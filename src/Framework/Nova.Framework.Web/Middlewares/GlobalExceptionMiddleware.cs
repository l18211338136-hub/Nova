using System.Net;
using System.Text.Json;
using MassTransit;
using FluentValidation;
using Nova.Contracts.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nova.Framework.Web.Responses;

namespace Nova.Framework.Web.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was canceled by the client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var statusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.StatusCode = statusCode;

        string message = exception.Message;

        // Unwrap MassTransit RequestFaultException if applicable
        if (exception is RequestFaultException faultException && faultException.Fault?.Exceptions?.Any() == true)
        {
            var fault = faultException.Fault.Exceptions.First();
            if (fault.ExceptionType == typeof(NovaValidationException).FullName || fault.ExceptionType == typeof(ValidationException).FullName)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                message = fault.Message;
            }
            else
            {
                message = fault.Message;
            }
            message = fault.Message;
        }
        else if (exception is ValidationException validationException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            message = string.Join(" ", validationException.Errors.Select(e => e.ErrorMessage));
        }
        else if (exception.InnerException != null)
        {
            message = exception.InnerException.Message;
        }

        var response = ApiResponse.Error(message, statusCode);
        
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}

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
        var (statusCode, message) = ResolveExceptionDetails(exception);

        context.Response.StatusCode = statusCode;
        var response = ApiResponse.Error(message, statusCode);
        
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        return context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string Message) ResolveExceptionDetails(Exception exception)
    {
        var current = exception;
        while (current != null)
        {
            if (current is NovaValidationException novaEx)
            {
                return ((int)HttpStatusCode.BadRequest, novaEx.Message);
            }
            if (current is ValidationException validationException)
            {
                return ((int)HttpStatusCode.BadRequest, string.Join(" ", validationException.Errors.Select(e => e.ErrorMessage)));
            }
            if (current is RequestFaultException faultException && faultException.Fault?.Exceptions?.Any() == true)
            {
                var fault = faultException.Fault.Exceptions.First();
                if (fault.ExceptionType == typeof(NovaValidationException).FullName ||
                    fault.ExceptionType == typeof(ValidationException).FullName ||
                    fault.ExceptionType?.EndsWith("ValidationException") == true)
                {
                    return ((int)HttpStatusCode.BadRequest, fault.Message);
                }
            }

            current = current.InnerException;
        }

        return ((int)HttpStatusCode.InternalServerError, "服务器内部错误，请稍后再试。");
    }
}

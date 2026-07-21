using FluentEmail.Core;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Commands;
using Nova.Contracts.DependencyInjection;
using Nova.Modules.Notification.Application;

namespace Nova.Modules.Notification.Infrastructure;

public class EmailNotificationService : IEmailService, ITransientDependency
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IFluentEmailFactory fluentEmailFactory, ILogger<EmailNotificationService> logger)
    {
        _fluentEmailFactory = fluentEmailFactory;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(SendEmailCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(command.To) || string.IsNullOrWhiteSpace(command.Subject) || string.IsNullOrWhiteSpace(command.Body))
            {
                _logger.LogWarning("SendEmailAsync failed: missing required arguments (To, Subject, or Body).");
                return false;
            }

            var email = _fluentEmailFactory.Create()
                .Subject(command.Subject)
                .Body(command.Body, isHtml: command.IsHtml);

            // Handle multiple recipients
            foreach (var to in command.To.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                email.To(to.Trim());
            }

            // Handle CC
            if (!string.IsNullOrWhiteSpace(command.Cc))
            {
                foreach (var cc in command.Cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    email.CC(cc.Trim());
                }
            }

            // Handle Attachments
            if (command.Attachments != null && command.Attachments.Any())
            {
                foreach (var attachmentPath in command.Attachments)
                {
                    var cleanPath = attachmentPath.Trim();
                    if (File.Exists(cleanPath))
                    {
                        email.AttachFromFilename(cleanPath);
                    }
                    else
                    {
                        _logger.LogWarning("Email attachment not found: {FilePath}", cleanPath);
                    }
                }
            }

            var response = await email.SendAsync(cancellationToken);

            if (response.Successful)
            {
                _logger.LogInformation("Email successfully sent to {To}", command.To);
                return true;
            }

            var errors = string.Join(", ", response.ErrorMessages);
            _logger.LogError("Failed to send email to {To}. Errors: {Errors}", command.To, errors);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while trying to send email to {To}", command.To);
            return false;
        }
    }
}

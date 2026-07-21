using MassTransit;
using Microsoft.Extensions.Logging;
using Nova.Contracts.Commands;

namespace Nova.Modules.Notification.Application.Consumers;

public class SendEmailCommandConsumer : IConsumer<SendEmailCommand>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailCommandConsumer> _logger;

    public SendEmailCommandConsumer(IEmailService emailService, ILogger<SendEmailCommandConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SendEmailCommand> context)
    {
        var command = context.Message;
        
        _logger.LogInformation("Received SendEmailCommand to: {To}, Subject: {Subject}", command.To, command.Subject);

        var result = await _emailService.SendEmailAsync(command, context.CancellationToken);

        if (!result)
        {
            _logger.LogWarning("Failed to send email to: {To}", command.To);
            // Depending on requirements, we could throw an exception to retry the message
            // throw new Exception("Failed to send email");
        }
        else
        {
            _logger.LogInformation("Successfully processed SendEmailCommand to: {To}", command.To);
        }
    }
}

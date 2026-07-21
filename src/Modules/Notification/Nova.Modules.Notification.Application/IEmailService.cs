using Nova.Contracts.Commands;

namespace Nova.Modules.Notification.Application;

public interface IEmailService
{
    Task<bool> SendEmailAsync(SendEmailCommand command, CancellationToken cancellationToken = default);
}

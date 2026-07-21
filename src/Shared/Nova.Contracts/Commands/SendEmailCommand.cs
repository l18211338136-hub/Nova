namespace Nova.Contracts.Commands;

public record SendEmailCommand(
    string To,
    string Subject,
    string Body,
    bool IsHtml = true,
    string? Cc = null,
    List<string>? Attachments = null
);

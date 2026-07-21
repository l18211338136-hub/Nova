namespace Nova.Modules.Notification.Infrastructure.Configuration;

public class SmtpOptions
{
    public const string Position = "Smtp";
    
    public string? Server { get; set; }
    public int Port { get; set; }
    public string? User { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; }
    public bool RequiresAuthentication { get; set; } = true;
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
}

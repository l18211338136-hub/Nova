using FluentEmail.MailKitSmtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nova.Modules.Notification.Application;
using Nova.Modules.Notification.Infrastructure.Configuration;

namespace Nova.Modules.Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IEmailService, EmailNotificationService>();

        var smtpOptions = new SmtpOptions();
        
        // 直接按照 SmtpClientOptions 的结构绑定配置
        configuration.GetSection(SmtpOptions.Position).Bind(smtpOptions);

        // 将最终的 SmtpOptions 注册为 Singleton（也可以使用 Configure<SmtpOptions> 结合 IOptions）
        services.AddSingleton(Options.Create(smtpOptions));

        if (!string.IsNullOrWhiteSpace(smtpOptions.Server) && !string.IsNullOrWhiteSpace(smtpOptions.User))
        {
            var senderOptions = new SmtpClientOptions
            {
                Server = smtpOptions.Server,
                Port = smtpOptions.Port,
                User = smtpOptions.User,
                Password = smtpOptions.Password,
                UseSsl = smtpOptions.UseSsl,
                RequiresAuthentication = smtpOptions.RequiresAuthentication
            };

            var defaultFrom = smtpOptions.FromAddress ?? smtpOptions.User;
            var defaultFromName = smtpOptions.FromName ?? "Nova Notification System";

            services.AddFluentEmail(defaultFrom, defaultFromName)
                    .AddMailKitSender(senderOptions);
        }

        return services;
    }
}

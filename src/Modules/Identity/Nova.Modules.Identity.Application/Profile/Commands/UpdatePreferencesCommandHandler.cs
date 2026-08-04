using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Common;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Profile.Commands;

public class UpdatePreferencesCommandHandler : IConsumer<UpdatePreferencesCommand>
{
    private static readonly string[] AllowedThemes = { "light", "dark", "system" };
    private static readonly string[] AllowedNotifyTypes = { "all", "mentions", "none" };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;

    public UpdatePreferencesCommandHandler(IServiceScopeFactory scopeFactory, NovaTenantDbContext tenantDb)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
    }

    public async Task Consume(ConsumeContext<UpdatePreferencesCommand> context)
    {
        var request = context.Message;

        if (request.CurrentUserId == Guid.Empty)
        {
            throw new NovaValidationException("未获取到当前登录用户信息");
        }

        if (request.Theme != null && !AllowedThemes.Contains(request.Theme, StringComparer.OrdinalIgnoreCase))
        {
            throw new NovaValidationException("不支持的主题取值");
        }

        if (request.NotifyType != null && !AllowedNotifyTypes.Contains(request.NotifyType, StringComparer.OrdinalIgnoreCase))
        {
            throw new NovaValidationException("不支持的通知范围取值");
        }

        if (!string.IsNullOrWhiteSpace(request.TimeZone) && !IsValidTimeZone(request.TimeZone))
        {
            throw new NovaValidationException("无法识别的时区标识");
        }

        var (scope, _) = await TenantScopeFactory.CreateAsync(
            _scopeFactory, _tenantDb, request.CurrentTenantId);

        using (scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<IIdentityDbContext>();

            var preference = await db.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == request.CurrentUserId);

            if (preference == null)
            {
                preference = UserPreference.CreateDefault(request.CurrentUserId);
                db.UserPreferences.Add(preference);
            }

            // null 表示本次不修改该项，因此逐项回退到当前值
            preference.UpdateAppearance(request.Theme, request.Font);
            preference.UpdateLocale(
                request.Language,
                request.TimeZone ?? preference.TimeZone);

            preference.UpdateNotifications(
                request.NotifyType,
                request.CommunicationEmails ?? preference.CommunicationEmails,
                request.MarketingEmails ?? preference.MarketingEmails,
                request.SocialEmails ?? preference.SocialEmails,
                request.MobileNotifications ?? preference.MobileNotifications);

            if (request.HiddenSidebarItems != null)
            {
                preference.UpdateHiddenSidebarItems(request.HiddenSidebarItems);
            }

            await db.SaveChangesAsync();

            await context.RespondAsync(new UpdatePreferencesResult { Success = true });
        }
    }

    private static bool IsValidTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}

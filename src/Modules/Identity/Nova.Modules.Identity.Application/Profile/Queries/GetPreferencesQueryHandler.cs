using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Common;
using Nova.Modules.Identity.Application.Database;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Profile.Queries;

public class GetPreferencesQueryHandler : IConsumer<GetPreferencesQuery>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;

    public GetPreferencesQueryHandler(IServiceScopeFactory scopeFactory, NovaTenantDbContext tenantDb)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
    }

    public async Task Consume(ConsumeContext<GetPreferencesQuery> context)
    {
        var request = context.Message;

        if (request.CurrentUserId == Guid.Empty)
        {
            throw new NovaValidationException("未获取到当前登录用户信息");
        }

        var (scope, _) = await TenantScopeFactory.CreateAsync(
            _scopeFactory, _tenantDb, request.CurrentTenantId);

        using (scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<IIdentityDbContext>();

            var preference = await db.UserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == request.CurrentUserId);

            // 尚未保存过偏好的用户返回内存默认值，不落库，避免读操作产生写入
            preference ??= UserPreference.CreateDefault(request.CurrentUserId);

            await context.RespondAsync(ToDto(preference));
        }
    }

    internal static UserPreferenceDto ToDto(UserPreference p) => new()
    {
        Theme = p.Theme,
        Font = p.Font,
        Language = p.Language,
        TimeZone = p.TimeZone,
        NotifyType = p.NotifyType,
        CommunicationEmails = p.CommunicationEmails,
        MarketingEmails = p.MarketingEmails,
        SocialEmails = p.SocialEmails,
        SecurityEmails = p.SecurityEmails,
        MobileNotifications = p.MobileNotifications,
        HiddenSidebarItems = p.GetHiddenSidebarItems().ToArray()
    };
}

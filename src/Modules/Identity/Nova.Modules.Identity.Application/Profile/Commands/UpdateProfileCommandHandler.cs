using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Common;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Profile.Commands;

public class UpdateProfileCommandHandler : IConsumer<UpdateProfileCommand>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;

    public UpdateProfileCommandHandler(IServiceScopeFactory scopeFactory, NovaTenantDbContext tenantDb)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
    }

    public async Task Consume(ConsumeContext<UpdateProfileCommand> context)
    {
        var request = context.Message;

        if (request.CurrentUserId == Guid.Empty)
        {
            throw new NovaValidationException("未获取到当前登录用户信息");
        }

        var (scope, tenantId) = await TenantScopeFactory.CreateAsync(
            _scopeFactory, _tenantDb, request.CurrentTenantId);

        using (scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(request.CurrentUserId.ToString());
            if (user == null)
            {
                throw new NovaValidationException("用户不存在");
            }

            var oldPhoneNumber = user.PhoneNumber;
            var newPhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            // 手机号在租户内需唯一，否则登录时无法定位到唯一用户
            if (!string.IsNullOrWhiteSpace(newPhoneNumber) &&
                !string.Equals(oldPhoneNumber, newPhoneNumber, StringComparison.Ordinal))
            {
                var occupied = await userManager.Users
                    .AnyAsync(u => u.PhoneNumber == newPhoneNumber && u.Id != user.Id);
                if (occupied)
                {
                    throw new NovaValidationException("该手机号已被其他用户绑定");
                }
            }

            user.UpdateProfile(request.NickName, request.Bio, request.AvatarUrl);
            user.PhoneNumber = newPhoneNumber;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new NovaValidationException($"更新资料失败: {errors}");
            }

            // 手机号变更时同步维护全局账号→租户映射，否则改号后无法用新号登录、旧号仍能定位租户
            if (!string.Equals(oldPhoneNumber, newPhoneNumber, StringComparison.Ordinal))
            {
                await SyncPhoneMappingAsync(oldPhoneNumber, newPhoneNumber, tenantId);
            }

            // 发布事件由 Storage 模块异步绑定头像附件
            if (request.AvatarFileId.HasValue)
            {
                await context.Publish(new Nova.Contracts.Storage.BindAttachmentEvent
                {
                    FileId = request.AvatarFileId.Value,
                    TargetType = "User",
                    TargetId = user.Id.ToString(),
                    AttachmentType = Nova.Contracts.Storage.AttachmentType.Avatar
                });
            }

            await context.RespondAsync(new UpdateProfileResult { Success = true });
        }
    }

    private async Task SyncPhoneMappingAsync(string? oldPhone, string? newPhone, string tenantId)
    {
        if (!string.IsNullOrWhiteSpace(oldPhone))
        {
            var stale = await _tenantDb.GlobalUserTenantMappings
                .Where(m => m.Account == oldPhone && m.TenantId == tenantId)
                .ToListAsync();
            if (stale.Count > 0)
            {
                _tenantDb.GlobalUserTenantMappings.RemoveRange(stale);
            }
        }

        if (!string.IsNullOrWhiteSpace(newPhone))
        {
            var exists = await _tenantDb.GlobalUserTenantMappings
                .AnyAsync(m => m.Account == newPhone && m.TenantId == tenantId);
            if (!exists)
            {
                _tenantDb.GlobalUserTenantMappings.Add(new GlobalUserTenantMapping
                {
                    Account = newPhone,
                    TenantId = tenantId
                });
            }
        }

        await _tenantDb.SaveChangesAsync();
    }
}

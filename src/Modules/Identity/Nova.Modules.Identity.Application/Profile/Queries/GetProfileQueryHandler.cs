using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Framework.MultiTenancy;
using Nova.Modules.Identity.Application.Common;
using Nova.Modules.Identity.Domain.Roles;
using Nova.Modules.Identity.Domain.Users;

namespace Nova.Modules.Identity.Application.Profile.Queries;

public class GetProfileQueryHandler : IConsumer<GetProfileQuery>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NovaTenantDbContext _tenantDb;

    public GetProfileQueryHandler(IServiceScopeFactory scopeFactory, NovaTenantDbContext tenantDb)
    {
        _scopeFactory = scopeFactory;
        _tenantDb = tenantDb;
    }

    public async Task Consume(ConsumeContext<GetProfileQuery> context)
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

            var roleNames = await userManager.GetRolesAsync(user);
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var roles = new List<ProfileRoleDto>(roleNames.Count);
            foreach (var roleName in roleNames)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                roles.Add(new ProfileRoleDto
                {
                    Name = roleName,
                    DisplayName = role?.DisplayName ?? roleName
                });
            }

            await context.RespondAsync(new ProfileDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                NickName = user.NickName,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Roles = roles.ToArray(),
                TenantId = tenantId,
                CreatedAt = user.CreatedAt
            });
        }
    }
}

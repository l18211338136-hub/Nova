using MassTransit;
using Microsoft.EntityFrameworkCore;
using Mapster;
using Nova.Contracts.CQRS;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Exceptions;
using Nova.Contracts.Security;
using Nova.Framework.Web.Responses;
using Nova.Modules.Organizations.Application.Database;
using Nova.Modules.Organizations.Application.DTOs;
using Nova.Modules.Organizations.Domain;
using System.ComponentModel;

namespace Nova.Modules.Organizations.Application.Organizations.Commands;

[ApiEndpoint("POST", "/api/organizations", typeof(ApiResponse<OrganizationDto>), "Organizations", Summary = "创建组织", RequireAuthorization = true)]
[RequirePermission("Organization.Orgs.Create")]
[Description("组织架构")]
public record CreateOrganizationCommand
{
    public string Name { get; init; } = default!;
    public Guid? ParentId { get; init; }
    public string? Code { get; init; }
    public string? Type { get; init; }
    public int Sort { get; init; }
    public Guid? LeaderUserId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Remarks { get; init; }
}

[ApiEndpoint("PUT", "/api/organizations/{Id}", typeof(ApiResponse<OrganizationDto>), "Organizations", Summary = "更新组织", RequireAuthorization = true)]
[RequirePermission("Organization.Orgs.Update")]
[Description("组织架构")]
public record UpdateOrganizationCommand
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public Guid? ParentId { get; init; }
    public string? Code { get; init; }
    public string? Type { get; init; }
    public int Sort { get; init; }
    public Guid? LeaderUserId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Remarks { get; init; }
    public bool IsEnabled { get; init; } = true;
}

[ApiEndpoint("DELETE", "/api/organizations/{Id}", typeof(ApiResponse<bool>), "Organizations", Summary = "删除组织", RequireAuthorization = true)]
[RequirePermission("Organization.Orgs.Delete")]
[Description("组织架构")]
public record DeleteOrganizationCommand
{
    public Guid Id { get; init; }
}

[ApiEndpoint("POST", "/api/organizations/{Id}/members", typeof(ApiResponse<bool>), "Organizations", Summary = "更新成员", RequireAuthorization = true)]
[RequirePermission("Organization.Members.Update")]
[Description("部门成员")]
public record UpdateOrganizationMembersCommand
{
    public Guid Id { get; init; }
    public List<OrganizationMemberDto> Members { get; init; } = new();
}

[ApiEndpoint("PUT", "/api/organizations/{Id}/permissions", typeof(ApiResponse<bool>), "Organizations", Summary = "保存权限", RequireAuthorization = true)]
[RequirePermission("Organization.Permissions.Save")]
[Description("部门权限")]
public record SaveOrganizationPermissionsCommand
{
    public Guid Id { get; init; }
    public int DataScope { get; init; } = 2;
    public string? AbacPoliciesJson { get; init; }
}

public class OrganizationCommandHandler :
    IConsumer<CreateOrganizationCommand>,
    IConsumer<UpdateOrganizationCommand>,
    IConsumer<DeleteOrganizationCommand>,
    IConsumer<UpdateOrganizationMembersCommand>,
    IConsumer<SaveOrganizationPermissionsCommand>,
    IScopedDependency
{
    private readonly IOrganizationDbContext _db;

    public OrganizationCommandHandler(IOrganizationDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<CreateOrganizationCommand> context)
    {
        var msg = context.Message;

        int level = 1;
        if (msg.ParentId.HasValue)
        {
            var parent = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == msg.ParentId.Value, context.CancellationToken);
            if (parent == null)
            {
                throw new NovaValidationException("上级机构不存在");
            }
            level = parent.Level + 1;
        }

        var entity = Organization.Create(
            msg.Name,
            msg.ParentId,
            msg.Code,
            msg.Type,
            level,
            msg.Sort,
            msg.LeaderUserId,
            msg.Phone,
            msg.Email,
            msg.Remarks);

        _db.Organizations.Add(entity);
        await _db.SaveChangesAsync(context.CancellationToken);

        var resultDto = entity.Adapt<OrganizationDto>();
        await context.RespondAsync(ApiResponse<OrganizationDto>.Success(resultDto));
    }

    public async Task Consume(ConsumeContext<UpdateOrganizationCommand> context)
    {
        var msg = context.Message;

        var entity = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == msg.Id, context.CancellationToken);
        if (entity == null)
        {
            throw new NovaValidationException("组织机构不存在");
        }

        if (msg.ParentId == msg.Id)
        {
            throw new NovaValidationException("不能将自身设为上级机构");
        }

        int level = 1;
        if (msg.ParentId.HasValue)
        {
            var parent = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == msg.ParentId.Value, context.CancellationToken);
            if (parent == null)
            {
                throw new NovaValidationException("上级机构不存在");
            }
            level = parent.Level + 1;
        }

        entity.Update(
            msg.Name,
            msg.ParentId,
            msg.Code,
            msg.Type,
            level,
            msg.Sort,
            msg.LeaderUserId,
            msg.Phone,
            msg.Email,
            msg.Remarks,
            msg.IsEnabled);

        await _db.SaveChangesAsync(context.CancellationToken);

        var resultDto = entity.Adapt<OrganizationDto>();
        await context.RespondAsync(ApiResponse<OrganizationDto>.Success(resultDto));
    }

    public async Task Consume(ConsumeContext<DeleteOrganizationCommand> context)
    {
        var msg = context.Message;

        var entity = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == msg.Id, context.CancellationToken);
        if (entity == null)
        {
            throw new NovaValidationException("组织机构不存在");
        }

        var hasChildren = await _db.Organizations.AnyAsync(o => o.ParentId == msg.Id, context.CancellationToken);
        if (hasChildren)
        {
            throw new NovaValidationException("该机构存在下级子机构，无法直接删除");
        }

        var hasMembers = await _db.UserOrganizations.AnyAsync(uo => uo.OrganizationId == msg.Id, context.CancellationToken);
        if (hasMembers)
        {
            throw new NovaValidationException("该机构下仍存在关联成员，请先清空成员后再试");
        }

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }

    public async Task Consume(ConsumeContext<UpdateOrganizationMembersCommand> context)
    {
        var msg = context.Message;

        var existing = await _db.UserOrganizations.Where(uo => uo.OrganizationId == msg.Id).ToListAsync(context.CancellationToken);
        _db.UserOrganizations.RemoveRange(existing);

        foreach (var m in msg.Members)
        {
            _db.UserOrganizations.Add(new UserOrganization
            {
                UserId = m.UserId,
                OrganizationId = msg.Id,
                IsPrimary = m.IsPrimary,
                JobTitle = m.JobTitle
            });
        }

        await _db.SaveChangesAsync(context.CancellationToken);
        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }

    public async Task Consume(ConsumeContext<SaveOrganizationPermissionsCommand> context)
    {
        var msg = context.Message;

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == msg.Id, context.CancellationToken);
        if (org == null)
        {
            throw new NovaValidationException("组织机构不存在");
        }

        org.DataScope = msg.DataScope;
        org.AbacPoliciesJson = msg.AbacPoliciesJson;

        await _db.SaveChangesAsync(context.CancellationToken);
        await context.RespondAsync(ApiResponse<bool>.Success(true));
    }
}

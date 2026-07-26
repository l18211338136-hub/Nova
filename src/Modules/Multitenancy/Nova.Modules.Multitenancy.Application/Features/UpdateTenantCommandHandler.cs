using System.Threading.Tasks;
using MassTransit;
using Nova.Modules.Multitenancy.Application.Services;

namespace Nova.Modules.Multitenancy.Application.Features;

public class UpdateTenantCommandHandler : IConsumer<UpdateTenantCommand>
{
    private readonly ITenantService _tenantService;

    public UpdateTenantCommandHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task Consume(ConsumeContext<UpdateTenantCommand> context)
    {
        var cmd = context.Message;
        await _tenantService.UpdateTenantAsync(
            cmd.Id,
            cmd.Name,
            cmd.ConnectionString,
            cmd.AdminEmail,
            cmd.Issuer,
            cmd.IsActive,
            cmd.ValidUpto,
            context.CancellationToken
        ).ConfigureAwait(false);

        await context.RespondAsync(new UpdateTenantResult(cmd.Id));
    }
}

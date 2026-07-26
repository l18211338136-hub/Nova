using System.Threading.Tasks;
using MassTransit;
using Nova.Modules.Multitenancy.Application.Services;

namespace Nova.Modules.Multitenancy.Application.Features;

public class DeleteTenantCommandHandler : IConsumer<DeleteTenantCommand>
{
    private readonly ITenantService _tenantService;

    public DeleteTenantCommandHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task Consume(ConsumeContext<DeleteTenantCommand> context)
    {
        await _tenantService.DeleteTenantAsync(context.Message.Id, context.CancellationToken).ConfigureAwait(false);
        await context.RespondAsync(new DeleteTenantResult(context.Message.Id));
    }
}

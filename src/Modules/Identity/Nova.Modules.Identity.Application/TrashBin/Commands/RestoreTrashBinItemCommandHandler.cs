using MassTransit;
using Nova.Contracts.TrashBin;

namespace Nova.Modules.Identity.Application.TrashBin.Commands;

public class RestoreTrashBinItemCommandHandler : IConsumer<RestoreTrashBinItemCommand>
{
    private readonly ITrashBinService _trashBinService;

    public RestoreTrashBinItemCommandHandler(ITrashBinService trashBinService)
    {
        _trashBinService = trashBinService;
    }

    public async Task Consume(ConsumeContext<RestoreTrashBinItemCommand> context)
    {
        var command = context.Message;
        var success = await _trashBinService.RestoreItemAsync(command.EntityType, command.Id, context.CancellationToken);
        await context.RespondAsync(new RestoreTrashBinItemResult { Success = success });
    }
}

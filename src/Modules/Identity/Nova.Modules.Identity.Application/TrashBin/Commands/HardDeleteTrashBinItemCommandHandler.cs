using MassTransit;
using Nova.Contracts.TrashBin;

namespace Nova.Modules.Identity.Application.TrashBin.Commands;

public class HardDeleteTrashBinItemCommandHandler : IConsumer<HardDeleteTrashBinItemCommand>
{
    private readonly ITrashBinService _trashBinService;

    public HardDeleteTrashBinItemCommandHandler(ITrashBinService trashBinService)
    {
        _trashBinService = trashBinService;
    }

    public async Task Consume(ConsumeContext<HardDeleteTrashBinItemCommand> context)
    {
        var command = context.Message;
        var success = await _trashBinService.HardDeleteItemAsync(command.EntityType, command.Id, context.CancellationToken);
        await context.RespondAsync(new HardDeleteTrashBinItemResult { Success = success });
    }
}

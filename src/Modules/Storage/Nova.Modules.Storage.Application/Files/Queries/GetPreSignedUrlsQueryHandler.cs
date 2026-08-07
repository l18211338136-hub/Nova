using MassTransit;
using Nova.Contracts.DependencyInjection;
using Nova.Contracts.Storage;
using Nova.Framework.Web.Responses;

namespace Nova.Modules.Storage.Application.Files.Queries;

public class GetPreSignedUrlsQueryHandler : IConsumer<GetPreSignedUrlsQuery>, IScopedDependency
{
    private readonly IStorageProvider _storageProvider;

    public GetPreSignedUrlsQueryHandler(IStorageProvider storageProvider)
    {
        _storageProvider = storageProvider;
    }

    public async Task Consume(ConsumeContext<GetPreSignedUrlsQuery> context)
    {
        var request = context.Message;
        if (request.Items == null || request.Items.Count == 0)
        {
            await context.RespondAsync(ApiResponse<List<PreSignedUrlResponseItem>>.Success(new List<PreSignedUrlResponseItem>()));
            return;
        }

        var expiresIn = TimeSpan.FromMinutes(request.ExpiresInMinutes > 0 ? request.ExpiresInMinutes : 15);
        var urls = await _storageProvider.GetPreSignedUploadUrlsAsync(request.Items, expiresIn, cancellationToken: context.CancellationToken);

        await context.RespondAsync(ApiResponse<List<PreSignedUrlResponseItem>>.Success(urls));
    }
}

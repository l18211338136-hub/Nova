using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Common;
using MassTransit;
using NSubstitute;
using Nova.Modules.Multitenancy.Application.Features;
using Nova.Modules.Multitenancy.Application.Services;
using Xunit;

namespace Nova.UnitTests.Handlers;

public class TenantCommandHandlersTests
{
    [Fact]
    public async Task CreateTenant_CallsService_And_EnqueuesInit_And_Responds()
    {
        var tenantService = Substitute.For<ITenantService>();
        tenantService.CreateTenantAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("generated-id");

        var jobClient = new FakeBackgroundJobClient();

        var handler = new CreateTenantCommandHandler(tenantService, jobClient);
        var cmd = new CreateTenantCommand("tid", "Tenant A", "conn", "admin@x.com", null, null);
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await handler.Consume(ctx);

        await tenantService.Received(1).CreateTenantAsync(
            "tid", "Tenant A", "conn", "admin@x.com", null, Arg.Any<CancellationToken>());
        Assert.Single(jobClient.EnqueuedJobs);
        var job = jobClient.EnqueuedJobs[0];
        Assert.Equal("MigrateTenantAsync", job.Method.Name);
        Assert.Contains("generated-id", job.Args.Select(a => a?.ToString() ?? string.Empty));
        await ctx.Received(1).RespondAsync(Arg.Any<CreateTenantResult>());
    }

    [Fact]
    public async Task UpdateTenant_CallsService_And_Responds()
    {
        var tenantService = Substitute.For<ITenantService>();
        var handler = new UpdateTenantCommandHandler(tenantService);
        var cmd = new UpdateTenantCommand("tid", "New", "conn", "a@x.com", null, true, System.DateTime.UtcNow.AddYears(1));
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await handler.Consume(ctx);

        await tenantService.Received(1).UpdateTenantAsync(
            "tid", "New", "conn", "a@x.com", null, true, Arg.Any<System.DateTime>(), Arg.Any<CancellationToken>());
        await ctx.Received(1).RespondAsync(Arg.Any<UpdateTenantResult>());
    }

    [Fact]
    public async Task DeleteTenant_CallsService_And_Responds()
    {
        var tenantService = Substitute.For<ITenantService>();
        var handler = new DeleteTenantCommandHandler(tenantService);
        var cmd = new DeleteTenantCommand("tid");
        var ctx = HandlerTestHarness.CreateConsumeContext(cmd);

        await handler.Consume(ctx);

        await tenantService.Received(1).DeleteTenantAsync("tid", Arg.Any<CancellationToken>());
        await ctx.Received(1).RespondAsync(Arg.Any<DeleteTenantResult>());
    }
}

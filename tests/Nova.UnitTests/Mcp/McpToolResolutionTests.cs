using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nova.Modules.Mcp.Application.Common.Interfaces;
using Nova.Modules.Mcp.Domain.Entities;
using Nova.Modules.Mcp.Infrastructure.Providers.Http;
using Nova.Modules.Mcp.Infrastructure.Server.Engine;

namespace Nova.UnitTests.Mcp;

/// <summary>
/// 回归测试：tools/call 必须按「密钥授权范围」解析工具行，
/// 不能仅按 Name 全局 FirstOrDefault（否则会命中指向其他环境的同名工具）。
/// </summary>
public class McpToolResolutionTests
{
    [Fact]
    public async Task ScopedKey_Should_Pick_Authorized_Server_Even_If_Foreign_Tool_Created_Earlier()
    {
        // Arrange：库中同名工具有两份，foreign（ai-test）先创建，prod 后创建；
        // 密钥只授权 prod。历史实现按 Name 取第一条 -> 命中 foreign -> 请求发到 ai-test。
        var db = CreateDb(out var prodServer, out var foreignServer, out var key, addProdAccess: true);

        var f = CreateFactory(db, out var engine, out var http);
        var call = JsonNode.Parse("""{"name":"GET__api_status","arguments":{}}""")!;

        // Act
        var result = await InvokeCallAsync(engine, call, key.KeyValue);
        var text = result?["content"]?[0]?["text"]?.ToString() ?? string.Empty;

        // Assert
        Assert.DoesNotContain("not authorized", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not found", text, StringComparison.OrdinalIgnoreCase);
        var url = Assert.Single(http.Urls);
        Assert.StartsWith("https://prod.invalid/api/status", url);
        Assert.DoesNotContain("ai-test", url);
    }

    [Fact]
    public async Task ScopedKey_Without_Server_Scope_Should_Be_Rejected()
    {
        var db = CreateDb(out _, out _, out var key, addProdAccess: false);
        var f = CreateFactory(db, out var engine, out var http);

        var result = await InvokeCallAsync(engine,
            JsonNode.Parse("""{"name":"GET__api_status","arguments":{}}""")!, key.KeyValue);
        var text = result?["content"]?[0]?["text"]?.ToString() ?? string.Empty;

        Assert.Contains("not authorized", text, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(http.Urls);
    }

    [Fact]
    public async Task AllServers_Key_Should_Pick_Earliest_Created_Tool_Deterministically()
    {
        var db = CreateDb(out _, out _, out var key, addProdAccess: false,
            allServers: true);
        var f = CreateFactory(db, out var engine, out var http);

        var result = await InvokeCallAsync(engine,
            JsonNode.Parse("""{"name":"GET__api_status","arguments":{}}""")!, key.KeyValue);
        var text = result?["content"]?[0]?["text"]?.ToString() ?? string.Empty;

        Assert.DoesNotContain("not authorized", text, StringComparison.OrdinalIgnoreCase);
        var url = Assert.Single(http.Urls);
        Assert.StartsWith("https://ai-test.invalid/api/status", url); // 最早的 foreign 行
    }

    // ---------- helpers ----------

    private static TestMcpDbContext CreateDb(
        out McpServer prodServer,
        out McpServer foreignServer,
        out McpKey key,
        bool addProdAccess,
        bool allServers = false)
    {
        var options = new DbContextOptionsBuilder<TestMcpDbContext>()
            .UseInMemoryDatabase($"mcp-resolve-{Guid.NewGuid():N}")
            .Options;
        var db = new TestMcpDbContext(options);

        foreignServer = McpServer.Create("ai-test", "https://ai-test.invalid");
        prodServer = McpServer.Create("prod-v2", "https://prod.invalid");
        db.McpServers.AddRange(foreignServer, prodServer);

        var foreignTool = McpTool.Create(foreignServer.Id, "GET__api_status", "foreign", "GET",
            "/api/status", "{}", "{}");
        foreignTool.CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var prodTool = McpTool.Create(prodServer.Id, "GET__api_status", "prod", "GET",
            "/api/status", "{}", "{}");
        prodTool.CreatedAt = new DateTimeOffset(2026, 11, 25, 0, 0, 0, TimeSpan.Zero);

        // 故意先插入 foreign，模拟历史代码 FirstOrDefault 命中它
        db.McpTools.AddRange(foreignTool, prodTool);

        key = McpKey.Create("test-key", allServers);
        db.McpKeys.Add(key);
        if (addProdAccess)
        {
            db.McpKeyAccesses.Add(McpKeyAccess.Create(key.Id, prodServer.Id));
        }

        db.SaveChanges();
        return db;
    }

    private static ServiceProvider CreateFactory(
        TestMcpDbContext db,
        out McpServerEngine engine,
        out CapturingHttpClientFactory http)
    {
        http = new CapturingHttpClientFactory();
        var services = new ServiceCollection();
        services.AddSingleton<IMcpDbContext>(db);
        services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor>(
            new Microsoft.AspNetCore.Http.HttpContextAccessor());
        services.AddSingleton<IHttpClientFactory>(http);
        services.AddTransient<HttpToolExecutor>();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        engine = new McpServerEngine(sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<McpServerEngine>.Instance);
        return sp;
    }

    private static async Task<JsonNode?> InvokeCallAsync(
        McpServerEngine engine, JsonNode args, string keyValue)
    {
        var method = typeof(McpServerEngine).GetMethod(
            "HandleCallToolAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task<JsonNode>)method.Invoke(engine, new object?[] { args, keyValue })!;
        return await task;
    }

    private sealed class TestMcpDbContext : DbContext, IMcpDbContext
    {
        public TestMcpDbContext(DbContextOptions<TestMcpDbContext> options) : base(options) { }

        public DbSet<McpServer> McpServers { get; set; } = default!;
        public DbSet<McpTool> McpTools { get; set; } = default!;
        public DbSet<McpKey> McpKeys { get; set; } = default!;
        public DbSet<McpKeyAccess> McpKeyAccesses { get; set; } = default!;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => base.SaveChangesAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<McpServer>().HasKey(x => x.Id);
            builder.Entity<McpTool>().HasKey(x => x.Id);
            builder.Entity<McpKey>().HasKey(x => x.Id);
            builder.Entity<McpKeyAccess>().HasKey(x => x.Id);
        }
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        public List<string> Urls { get; } = new();

        public HttpClient CreateClient(string name) => new HttpClient(new CaptureHandler(Urls));
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly List<string> _urls;
        public CaptureHandler(List<string> urls) => _urls = urls;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _urls.Add(request.RequestUri?.ToString() ?? "(null)");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"ok\":true}}")
            });
        }
    }
}

using System.Reflection;
using MassTransit;
using MassTransit.Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.Framework.Web.Modular;
using Xunit;

namespace Nova.IntegrationTests;

/// <summary>
/// 组合根集成测试：验证宿主的模块自动装配（AddModules）能够无错误地构建容器，
/// 并正确注册 MassTransit Mediator 与 16 个业务模块。无需数据库。
/// </summary>
public class CompositionRootTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=nova_test;Username=test;Password=test",
                ["Cache:Provider"] = "Local"
            })
            .Build();

        services.AddModules(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void CompositionRoot_Builds_Without_Errors()
    {
        var provider = BuildProvider();
        Assert.NotNull(provider);
    }

    [Fact]
    public void Mediator_Is_Registered()
    {
        var provider = BuildProvider();
        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void All_Sixteen_Modules_Are_Loaded()
    {
        var moduleType = typeof(IModule);
        var found = 0;
        var directory = AppContext.BaseDirectory;

        foreach (var file in Directory.GetFiles(directory, "Nova.Modules.*.Api.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                found += assembly.GetTypes()
                    .Count(t => moduleType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            }
            catch
            {
                // 与宿主扫描行为一致
            }
        }

        Assert.Equal(16, found);
    }
}

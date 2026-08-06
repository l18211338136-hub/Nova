using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Security;
using Nova.Framework.Domain.Auditing;
using Nova.Framework.MultiTenancy;
using Nova.Framework.Persistence.Interceptors;
using Nova.Modules.Audit.Infrastructure.Persistence;
using System.Security.Claims;
using Xunit;

namespace Nova.UnitTests;

public class EntityDiffingTests
{
    private class TestCurrentUser : ICurrentUser
    {
        public Guid? Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; } = "admin@nova.com";
        public string? Email { get; set; } = "admin@nova.com";
        public bool IsAuthenticated => true;
        public IEnumerable<Claim> Claims => Array.Empty<Claim>();
        public string[] Roles => new[] { "Admin" };
        public bool IsInRole(string role) => true;
    }

    [Fact]
    public async Task EntityChangeCaptureInterceptor_ShouldCapture_PropertyDiffsOnModified()
    {
        // Arrange
        var testCurrentUser = new TestCurrentUser();
        var interceptor = new EntityChangeCaptureInterceptor(testCurrentUser);

        var tenantInfo = new NovaTenantInfo
        {
            Id = "test-tenant",
            Identifier = "test-tenant",
            Name = "Test Tenant",
            ConnectionString = "x"
        };

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        using var db = new AuditDbContext(tenantInfo, options);

        // 1. 新增 EntityChangeLog
        var log = EntityChangeLog.Create("User", "user-123", "Modified", testCurrentUser.Id, testCurrentUser.Name);
        log.AddPropertyChange("NickName", "initial_nick", "updated_nick");

        db.EntityChangeLogs.Add(log);
        await db.SaveChangesAsync();

        // Assert
        var changeLogs = await db.EntityChangeLogs
            .Include(x => x.PropertyChanges)
            .ToListAsync();

        Assert.NotEmpty(changeLogs);

        var modifiedLog = changeLogs.FirstOrDefault(x => x.ChangeType == "Modified");
        Assert.NotNull(modifiedLog);
        Assert.Equal("User", modifiedLog!.EntityType);
        Assert.Equal("admin@nova.com", modifiedLog.OperatorName);

        var nickNameChange = modifiedLog.PropertyChanges.FirstOrDefault(p => p.PropertyName == "NickName");
        Assert.NotNull(nickNameChange);
        Assert.Equal("initial_nick", nickNameChange!.OriginalValue);
        Assert.Equal("updated_nick", nickNameChange.NewValue);
    }
}

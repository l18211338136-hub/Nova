using Microsoft.EntityFrameworkCore;
using Nova.Contracts.Security;
using Nova.Framework.Domain.Auditing;
using Nova.Framework.Persistence.Interceptors;
using NSubstitute;

namespace Nova.UnitTests;

public class AggregateRelationAuditingTests
{
    public class TestDbContext : DbContext
    {
        private readonly EntityChangeCaptureInterceptor _interceptor;

        public TestDbContext(DbContextOptions<TestDbContext> options, EntityChangeCaptureInterceptor interceptor)
            : base(options)
        {
            _interceptor = interceptor;
        }

        public DbSet<TestRoleClaim> RoleClaims { get; set; } = default!;
        public DbSet<TestUserRole> UserRoles { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_interceptor);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestUserRole>().HasKey(x => new { x.UserId, x.RoleId });
        }
    }

    public class TestRoleClaim
    {
        public int Id { get; set; }
        public string RoleId { get; set; } = default!;
        public string ClaimType { get; set; } = default!;
        public string ClaimValue { get; set; } = default!;
    }

    public class TestUserRole
    {
        public string UserId { get; set; } = default!;
        public string RoleId { get; set; } = default!;
    }

    [Fact]
    public async Task SavingChanges_ShouldCaptureRolePermissionChanges_BelongingToRole()
    {
        // Arrange
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(Guid.NewGuid());
        currentUser.Name.Returns("TestAdmin");

        var channel = Substitute.For<IEntityChangeChannel>();
        EntityChangeLog? capturedLog = null;
        channel.WriteAsync(Arg.Do<EntityChangeLog>(log => capturedLog = log), Arg.Any<CancellationToken>())
               .Returns(ValueTask.CompletedTask);

        var interceptor = new EntityChangeCaptureInterceptor(currentUser, channel);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var db = new TestDbContext(options, interceptor);

        var roleId = Guid.NewGuid().ToString();
        db.RoleClaims.Add(new TestRoleClaim
        {
            RoleId = roleId,
            ClaimType = "Permission",
            ClaimValue = "Users.Create"
        });

        // Act
        await db.SaveChangesAsync();

        // Assert
        Assert.NotNull(capturedLog);
        Assert.Equal("Role", capturedLog!.EntityType);
        Assert.Equal(roleId, capturedLog.EntityId);
        Assert.Equal("Modified", capturedLog.ChangeType);
        Assert.Single(capturedLog.PropertyChanges);

        var change = capturedLog.PropertyChanges.First();
        Assert.Equal("Permission", change.PropertyName);
        Assert.Equal("权限 (Permission)", change.PropertyDisplayName);
        Assert.Equal("Users.Create", change.NewValue);
        Assert.Null(change.OriginalValue);
    }
}

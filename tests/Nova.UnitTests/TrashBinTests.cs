using Microsoft.EntityFrameworkCore;
using Nova.Framework.Domain.Auditing;
using Nova.Framework.Persistence.TrashBin;

namespace Nova.UnitTests;

public class TestEntity : IFullAuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "TestItem";
    public bool IsDeleted { get; set; } = false;
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? Remarks => null;
    public int Sort => 0;
    public bool IsEnabled => true;
}

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}

public class TrashBinTests
{
    private DbContextOptions<TestDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetDeletedItemsAsync_ShouldReturnSoftDeletedEntities()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        var entity1 = new TestEntity { Name = "ActiveItem", IsDeleted = false };
        var entity2 = new TestEntity { Name = "DeletedItem", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow };
        context.TestEntities.AddRange(entity1, entity2);
        await context.SaveChangesAsync();

        var trashBinService = new TrashBinService(context);

        // Act
        var result = await trashBinService.GetDeletedItemsAsync();

        // Assert
        Assert.Equal(1, result.Total);
        Assert.Equal("DeletedItem", result.Items.First().DisplayName);
    }

    [Fact]
    public async Task RestoreItemAsync_ShouldUnsetSoftDeleteFlag()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        var entity = new TestEntity { Name = "DeletedItemToRestore", IsDeleted = true, DeletedAt = DateTimeOffset.UtcNow };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var trashBinService = new TrashBinService(context);

        // Act
        var success = await trashBinService.RestoreItemAsync(nameof(TestEntity), entity.Id);

        // Assert
        Assert.True(success);
        var restored = await context.TestEntities.FindAsync(entity.Id);
        Assert.NotNull(restored);
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedAt);
    }

    [Fact]
    public async Task HardDeleteItemAsync_ShouldPermanentlyRemoveEntity()
    {
        // Arrange
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        var entity = new TestEntity { Name = "ItemToHardDelete", IsDeleted = true };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        var trashBinService = new TrashBinService(context);

        // Act
        var success = await trashBinService.HardDeleteItemAsync(nameof(TestEntity), entity.Id);

        // Assert
        Assert.True(success);
        var dbEntity = await context.TestEntities.FindAsync(entity.Id);
        Assert.Null(dbEntity);
    }
}

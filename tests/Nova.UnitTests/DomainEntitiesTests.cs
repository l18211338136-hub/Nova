using Nova.Framework.Domain.Auditing;
using Nova.Framework.Domain.Entities;
using Xunit;

namespace Nova.UnitTests.Domain;

public class DomainEntitiesTests
{
    private class GuidEntity : Entity<Guid> { }
    private class IntEntity : Entity<int> { }
    private class AuditedEntity : FullAuditedEntity<Guid> { }

    [Fact]
    public void Guid_Entity_Auto_Generates_NonEmpty_Id()
    {
        var first = new GuidEntity();
        var second = new GuidEntity();

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Int_Entity_Keeps_Default_Key()
    {
        var entity = new IntEntity();
        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public void FullAuditedEntity_Has_Sensible_Defaults()
    {
        var entity = new AuditedEntity();

        Assert.True(entity.IsEnabled);
        Assert.False(entity.IsDeleted);
        Assert.Equal(0, entity.Sort);
        Assert.Equal(DateTimeOffset.UtcNow.Date, entity.CreatedAt.UtcDateTime.Date);
    }

    [Fact]
    public void FullAuditedEntity_Supports_Soft_Delete_Fields()
    {
        var entity = new AuditedEntity();

        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedBy = Guid.CreateVersion7();

        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.DeletedAt);
        Assert.NotNull(entity.DeletedBy);
    }
}

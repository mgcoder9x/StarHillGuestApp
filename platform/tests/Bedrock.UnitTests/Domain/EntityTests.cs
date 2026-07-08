using Bedrock.Domain.Entities;
using Bedrock.Domain.Events;
using Xunit;

namespace Bedrock.UnitTests.Domain;

public sealed class EntityTests
{
    private sealed class SampleEntity : Entity
    {
        public SampleEntity()
        {
        }

        public SampleEntity(Guid id) : base(id)
        {
        }

        public void DoSomething() => RaiseDomainEvent(new SampleDomainEvent());
    }

    private sealed class OtherEntity : Entity
    {
        public OtherEntity()
        {
        }

        public OtherEntity(Guid id) : base(id)
        {
        }
    }

    private sealed record SampleDomainEvent : IDomainEvent;

    [Fact]
    public void New_entity_should_get_nonempty_v7_id()
    {
        var entity = new SampleEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Ctor_with_empty_id_should_throw()
    {
        Assert.Throws<ArgumentException>(() => new SampleEntity(Guid.Empty));
    }

    [Fact]
    public void Entities_of_same_type_and_id_should_be_equal()
    {
        var id = Guid.CreateVersion7();

        var a = new SampleEntity(id);
        var b = new SampleEntity(id);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Entities_of_different_type_same_id_should_not_be_equal()
    {
        var id = Guid.CreateVersion7();

        Entity a = new SampleEntity(id);
        Entity b = new OtherEntity(id);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RaiseDomainEvent_should_collect_and_clear()
    {
        var entity = new SampleEntity();

        entity.DoSomething();
        Assert.Single(entity.DomainEvents);

        entity.ClearDomainEvents();
        Assert.Empty(entity.DomainEvents);
    }
}

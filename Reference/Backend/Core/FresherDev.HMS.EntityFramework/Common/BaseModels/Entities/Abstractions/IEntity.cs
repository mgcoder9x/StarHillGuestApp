namespace FresherDev.HMS.EntityFramework;

public interface IEntity
{
}

public interface IEntity<TKey> : IEntity
    where TKey : struct
{
    public TKey Id { get; set; }
}
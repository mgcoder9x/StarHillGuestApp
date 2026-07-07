using Microsoft.EntityFrameworkCore;

namespace FresherDev.HMS.EntityFramework;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    public DbSet<Address> Addresses { get; set; }

    public DbSet<Token> Tokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        AddQueryFilters(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        BeforeSaveChange();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void BeforeSaveChange()
    {
        foreach (var entity in ChangeTracker.Entries().Where(x => x.Entity is IAuditEntity<Guid> && x.State == EntityState.Added))
        {
            if (typeof(IAuditEntity).IsAssignableFrom(entity.Metadata.ClrType))
            {
                ((IAuditEntity)entity.Entity).CreatedTime = DateTimeOffset.UtcNow;
            }
        }

        foreach (var entity in ChangeTracker.Entries().Where(x => x.Entity is IAuditEntity && x.State == EntityState.Modified))
        {
            if (typeof(IAuditEntity).IsAssignableFrom(entity.Metadata.ClrType))
            {
                ((IAuditEntity)entity.Entity).UpdatedTime = DateTimeOffset.UtcNow;
            }
        }

        foreach (var entity in ChangeTracker.Entries().Where(x => x.Entity is ISoftDelete && x.State == EntityState.Deleted))
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entity.Metadata.ClrType))
            {
                var softDeleteEntity = (ISoftDelete)entity.Entity;
                entity.State = EntityState.Modified;
                softDeleteEntity.DeletedTime = DateTimeOffset.UtcNow;
                softDeleteEntity.IsDeleted = true;
            }
        }
    }

    private static void AddQueryFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // Other automated configurations left out
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSoftDeleteQueryFilter();
            }
        }
    }
}


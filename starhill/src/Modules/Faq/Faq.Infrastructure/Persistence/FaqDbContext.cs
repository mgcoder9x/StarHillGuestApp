using Bedrock.Application.Events;
using Bedrock.Application.Ports.Time;
using Bedrock.Application.Ports.Users;
using Bedrock.Infrastructure.Persistence;
using Faq.Domain;
using Microsoft.EntityFrameworkCore;

namespace Faq.Infrastructure.Persistence;

/// <summary>
/// DbContext module Faq — 1 DbContext + schema <c>faq</c> (data ownership F31). KHÔNG outbox (Faq chưa phát event).
/// Index/constraint đặt ở EF config. ResortId là Guid trần — KHÔNG FK chéo-schema; FK nội-schema faq
/// (Item→Category, Item→Item(parent), *Translation→cha) là hợp lệ. xmin (CP15) do PlatformDbContext tự map cho
/// entity <c>IHasConcurrencyToken</c> trên Npgsql.
/// </summary>
public sealed class FaqDbContext : PlatformDbContext
{
    public const string SchemaName = "faq";

    public FaqDbContext(
        DbContextOptions<FaqDbContext> options,
        IClock clock,
        ICurrentUser currentUser,
        IDomainEventDispatcher domainEventDispatcher)
        : base(options, clock, currentUser, domainEventDispatcher)
    {
    }

    public DbSet<FaqCategory> FaqCategories => Set<FaqCategory>();
    public DbSet<FaqCategoryTranslation> FaqCategoryTranslations => Set<FaqCategoryTranslation>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();
    public DbSet<FaqItemTranslation> FaqItemTranslations => Set<FaqItemTranslation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FaqDbContext).Assembly);
    }
}

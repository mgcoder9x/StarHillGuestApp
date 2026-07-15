using Faq.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Faq.Infrastructure.Persistence.Configurations;

// RowVersion (xmin) do PlatformDbContext.OnModelCreating tự map cho entity IHasConcurrencyToken trên Npgsql
// (CP15) — KHÔNG cấu hình lại ở đây. Id là UUIDv7 client-side → ValueGeneratedNever (mirror Rules/GuestAccess/Rooms).

/// <summary>
/// EF config <see cref="FaqCategory"/> — unique <c>(ResortId, Key)</c> (<c>ux_faq_category_key</c>): Key định danh
/// ổn định/resort. Index (resort, thứ tự) để guest/admin list theo SortOrder.
/// </summary>
public sealed class FaqCategoryConfiguration : IEntityTypeConfiguration<FaqCategory>
{
    public void Configure(EntityTypeBuilder<FaqCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("faq_category");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => new { x.ResortId, x.Key })
            .IsUnique()
            .HasDatabaseName("ux_faq_category_key");

        builder.HasIndex(x => new { x.ResortId, x.SortOrder }).HasDatabaseName("ix_faq_category_resort_order");
    }
}

/// <summary>EF config <see cref="FaqCategoryTranslation"/> — unique (category, lang); FK→FaqCategory Cascade.</summary>
public sealed class FaqCategoryTranslationConfiguration : IEntityTypeConfiguration<FaqCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<FaqCategoryTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("faq_category_translation");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Name).HasMaxLength(300);

        builder.HasIndex(x => new { x.FaqCategoryId, x.LanguageCode })
            .IsUnique()
            .HasDatabaseName("ux_faq_category_translation_lang");

        builder.HasOne<FaqCategory>()
            .WithMany()
            .HasForeignKey(x => x.FaqCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF config <see cref="FaqItem"/> — FK→FaqCategory <b>Restrict</b> (chặn xóa category còn item — backstop DB cho
/// invariant use case E-Faq.2); FK self <c>ParentId</c>→FaqItem <b>Restrict</b> (chặn xóa cha còn con). Index
/// (category, thứ tự) để list flow theo SortOrder.
/// </summary>
public sealed class FaqItemConfiguration : IEntityTypeConfiguration<FaqItem>
{
    public void Configure(EntityTypeBuilder<FaqItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("faq_item");
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.HasIndex(x => new { x.CategoryId, x.SortOrder }).HasDatabaseName("ix_faq_item_category_order");
        builder.HasIndex(x => x.ParentId).HasDatabaseName("ix_faq_item_parent");

        builder.HasOne<FaqCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FaqItem>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>EF config <see cref="FaqItemTranslation"/> — unique (item, lang); FK→FaqItem Cascade.</summary>
public sealed class FaqItemTranslationConfiguration : IEntityTypeConfiguration<FaqItemTranslation>
{
    public void Configure(EntityTypeBuilder<FaqItemTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("faq_item_translation");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Question).HasMaxLength(500);
        // AnswerHtmlSanitized: rich text → không giới hạn độ dài (text).

        builder.HasIndex(x => new { x.FaqItemId, x.LanguageCode })
            .IsUnique()
            .HasDatabaseName("ux_faq_item_translation_lang");

        builder.HasOne<FaqItem>()
            .WithMany()
            .HasForeignKey(x => x.FaqItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

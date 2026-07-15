using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rules.Domain;

namespace Rules.Infrastructure.Persistence.Configurations;

// RowVersion (xmin) do PlatformDbContext.OnModelCreating tự map cho entity IHasConcurrencyToken trên Npgsql
// (CP15) — KHÔNG cấu hình lại ở đây. Id là UUIDv7 client-side → ValueGeneratedNever (mirror GuestAccess/Rooms).

/// <summary>
/// EF config <see cref="RuleSet"/> (Draft). ĐÚNG MỘT bản nháp/resort — ràng buộc UNIQUE <c>ux_rule_set_resort</c>
/// (QR-AD-035): enforce bất biến ở DB (không TOCTOU), chống hai Draft mồ côi khi hai admin tạo section đầu tiên đua
/// nhau (Publish đọc "the Draft RuleSet" số ít — design §4). Cùng lớp cơ chế partial-unique publication (CP4).
/// </summary>
public sealed class RuleSetConfiguration : IEntityTypeConfiguration<RuleSet>
{
    public void Configure(EntityTypeBuilder<RuleSet> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_set");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.ResortId).IsUnique().HasDatabaseName("ux_rule_set_resort");
    }
}

/// <summary>EF config <see cref="RuleSection"/> (Draft) — FK→RuleSet Cascade (section thuộc set); index (set, thứ tự).</summary>
public sealed class RuleSectionConfiguration : IEntityTypeConfiguration<RuleSection>
{
    public void Configure(EntityTypeBuilder<RuleSection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_section");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => new { x.RuleSetId, x.SortOrder }).HasDatabaseName("ix_rule_section_set_order");

        builder.HasOne<RuleSet>()
            .WithMany()
            .HasForeignKey(x => x.RuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF config <see cref="RuleSectionTranslation"/> (Draft) — unique (section, lang); FK→RuleSection Cascade.</summary>
public sealed class RuleSectionTranslationConfiguration : IEntityTypeConfiguration<RuleSectionTranslation>
{
    public void Configure(EntityTypeBuilder<RuleSectionTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_section_translation");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(300);
        // BodyHtmlSanitized: rich text → không giới hạn độ dài (text/nvarchar(max)).

        builder.HasIndex(x => new { x.RuleSectionId, x.LanguageCode })
            .IsUnique()
            .HasDatabaseName("ux_rule_section_translation_lang");

        builder.HasOne<RuleSection>()
            .WithMany()
            .HasForeignKey(x => x.RuleSectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF config <see cref="RulePublication"/> (snapshot bất biến) — partial unique 1 IsCurrent/resort
/// (<c>ux_rule_publication_current</c>, filter <c>is_current</c>); index (resort, version) truy vết lịch sử.
/// </summary>
public sealed class RulePublicationConfiguration : IEntityTypeConfiguration<RulePublication>
{
    public void Configure(EntityTypeBuilder<RulePublication> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_publication");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ChangeNote).HasMaxLength(1000);

        builder.HasIndex(x => x.ResortId)
            .IsUnique()
            .HasDatabaseName("ux_rule_publication_current")
            .HasFilter("is_current");

        builder.HasIndex(x => new { x.ResortId, x.Version }).HasDatabaseName("ix_rule_publication_resort_version");
    }
}

/// <summary>EF config <see cref="RulePublicationSection"/> (đông cứng) — FK→RulePublication Cascade; index (pub, thứ tự).</summary>
public sealed class RulePublicationSectionConfiguration : IEntityTypeConfiguration<RulePublicationSection>
{
    public void Configure(EntityTypeBuilder<RulePublicationSection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_publication_section");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => new { x.RulePublicationId, x.SortOrder })
            .HasDatabaseName("ix_rule_publication_section_order");

        builder.HasOne<RulePublication>()
            .WithMany()
            .HasForeignKey(x => x.RulePublicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF config <see cref="RulePublicationSectionTranslation"/> — unique (pub-section, lang); FK Cascade.</summary>
public sealed class RulePublicationSectionTranslationConfiguration
    : IEntityTypeConfiguration<RulePublicationSectionTranslation>
{
    public void Configure(EntityTypeBuilder<RulePublicationSectionTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_publication_section_translation");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(300);

        builder.HasIndex(x => new { x.RulePublicationSectionId, x.LanguageCode })
            .IsUnique()
            .HasDatabaseName("ux_rule_pub_section_translation_lang");

        builder.HasOne<RulePublicationSection>()
            .WithMany()
            .HasForeignKey(x => x.RulePublicationSectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// EF config <see cref="RuleAcknowledgement"/> — unique <c>(GuestVisitId, RulePublicationId)</c> (ack idempotent/visit);
/// FK→RulePublication Restrict (không xóa publication đang được ack tham chiếu). RoomId/GuestSessionId/GuestVisitId
/// là Guid TRẦN (không FK chéo-schema rooms/guest_access — QR-AD-002/024).
/// </summary>
public sealed class RuleAcknowledgementConfiguration : IEntityTypeConfiguration<RuleAcknowledgement>
{
    public void Configure(EntityTypeBuilder<RuleAcknowledgement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("rule_acknowledgement");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.LanguageCode).IsRequired().HasMaxLength(16);

        builder.HasIndex(x => new { x.GuestVisitId, x.RulePublicationId })
            .IsUnique()
            .HasDatabaseName("ux_rule_ack_visit_publication");

        builder.HasOne<RulePublication>()
            .WithMany()
            .HasForeignKey(x => x.RulePublicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

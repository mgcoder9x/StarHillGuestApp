using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResortConfig.Domain;

namespace ResortConfig.Infrastructure.Persistence.Configurations;

/// <summary>EF config Resort — ràng buộc kỹ thuật (KHÔNG ở Domain). Port từ resort-qr.</summary>
public sealed class ResortConfiguration : IEntityTypeConfiguration<Resort>
{
    public void Configure(EntityTypeBuilder<Resort> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("resort");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Timezone).IsRequired().HasMaxLength(64);
        builder.Property(x => x.LogoUrl).HasMaxLength(512);
    }
}

/// <summary>EF config ResortSettings — 1-1 với Resort (unique FK) + CHECK ngưỡng vận hành.</summary>
public sealed class ResortSettingsConfiguration : IEntityTypeConfiguration<ResortSettings>
{
    public void Configure(EntityTypeBuilder<ResortSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.GuestWebBaseUrl).HasMaxLength(512);

        builder.HasIndex(x => x.ResortId).IsUnique().HasDatabaseName("ux_resort_settings_resort");

        builder.HasOne<Resort>()
            .WithMany()
            .HasForeignKey(x => x.ResortId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("resort_settings", t =>
        {
            t.HasCheckConstraint("ck_portal_window", "portal_window_minutes > 0");
            t.HasCheckConstraint("ck_idle_expiry", "visit_idle_expiry_hours > 0");
            t.HasCheckConstraint("ck_msg_len", "max_message_length > 0");
        });
    }
}

/// <summary>EF config ResortLanguage — unique (resort,code); partial unique IsDefault ở DbContext (Npgsql-aware).</summary>
public sealed class ResortLanguageConfiguration : IEntityTypeConfiguration<ResortLanguage>
{
    public void Configure(EntityTypeBuilder<ResortLanguage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("resort_language");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(16);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(64);

        builder.HasIndex(x => new { x.ResortId, x.Code }).IsUnique().HasDatabaseName("ux_lang_code");

        builder.HasOne<Resort>()
            .WithMany()
            .HasForeignKey(x => x.ResortId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

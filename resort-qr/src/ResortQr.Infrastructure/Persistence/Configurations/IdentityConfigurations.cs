using ResortQr.Domain.Identity;
using ResortQr.Domain.Resorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ResortQr.Infrastructure.Persistence.Configurations;

/// <summary>EF config AppUser — email unique; role enum→string.</summary>
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("app_user");
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(128);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_appuser_email");

        builder.HasOne<Resort>()
            .WithMany()
            .HasForeignKey(x => x.ResortId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

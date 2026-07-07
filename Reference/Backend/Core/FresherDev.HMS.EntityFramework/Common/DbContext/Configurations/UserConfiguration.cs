using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FresherDev.HMS.EntityFramework;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
       public void Configure(EntityTypeBuilder<User> builder)
       {
              builder.HasOne(x => x.Address)
                     .WithMany(x => x.Users)
                     .HasForeignKey(x => x.AddressId)
                     .HasConstraintName("FK_User_Address")
                     .OnDelete(DeleteBehavior.Cascade);

              builder.HasMany(x => x.Tokens)
                     .WithOne(x => x.User)
                     .HasForeignKey(x => x.UserId)
                     .HasConstraintName("FK_Token_User")
                     .OnDelete(DeleteBehavior.Cascade);
       }
}

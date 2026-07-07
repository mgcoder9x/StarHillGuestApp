using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FresherDev.HMS.EntityFramework;

[Table("AspNetPermissions")]
public class Permission : FullyEntity<Guid>
{
    [Required]
    [MaxLength(EntityLength.Normal)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(EntityLength.Normal)]
    public required string Discriminator { get; set; }

    public bool IsGranted { get; set; }

    public Guid? RoleId { get; set; }

    public Guid? UserId { get; set; }
}
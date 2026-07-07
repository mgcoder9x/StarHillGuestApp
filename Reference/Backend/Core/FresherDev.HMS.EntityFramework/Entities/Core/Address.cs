using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FresherDev.HMS.EntityFramework;

[Table("Addresses")]
public class Address : FullyEntity<Guid>
{
    [Required]
    [MaxLength(EntityLength.Normal)]
    public required string Name { get; set; }

    [MaxLength(EntityLength.Normal)]
    public string? Description { get; set; }

    public virtual IEnumerable<User>? Users { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FresherDev.HMS.EntityFramework;

[Table("AspNetUsers")]
public class User : FullyEntity<Guid>
{
    public const string MasterName = "admin";

    [Required]
    [MaxLength(EntityLength.Short)]
    public required string Username { get; set; }

    [Required]
    [MaxLength(EntityLength.Long)]
    public required string Password { get; set; }

    [MaxLength(EntityLength.Short)]
    public string? Email { get; set; }

    [MaxLength(EntityLength.Normal)]
    public string? FirstName { get; set; }

    [MaxLength(EntityLength.Normal)]
    public string? LastName { get; set; }

    [MaxLength(EntityLength.Short)]
    public string? Phone { get; set; }

    public Guid? AddressId { get; set; }

    public virtual Address? Address { get; set; }

    public virtual ICollection<Token>? Tokens { get; set; }
}
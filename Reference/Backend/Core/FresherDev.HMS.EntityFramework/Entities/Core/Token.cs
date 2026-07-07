using System.ComponentModel.DataAnnotations.Schema;

namespace FresherDev.HMS.EntityFramework;

[Table("Tokens")]
public class Token : Entity<Guid>
{
    public required string Value { get; set; }

    public string? Description { get; set; }

    public required Guid UserId { get; set; }

    public User? User { get; set; }
}

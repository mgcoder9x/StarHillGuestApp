namespace FresherDev.HMS.Core.Users;

public class UserModel
{
    public required Guid Id { get; set; }

    public required string Username { get; set; }

    public string? Email { get; set; }
}
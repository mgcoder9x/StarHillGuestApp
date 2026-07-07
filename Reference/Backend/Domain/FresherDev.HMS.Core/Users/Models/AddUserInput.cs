namespace FresherDev.HMS.Core.Users;

public class AddUserInput
{
    public required string Username { get; set; }

    public required string Password { get; set; }

    public string? Email { get; set; }
}
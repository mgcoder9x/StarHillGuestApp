namespace FresherDev.HMS.Core.Users;

public class UpdateUserInput
{
    public required Guid Id { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }
}
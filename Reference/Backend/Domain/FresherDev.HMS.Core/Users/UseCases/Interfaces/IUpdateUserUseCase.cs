namespace FresherDev.HMS.Core.Users;

public interface IUpdateUserUseCase
{
    Task UpdateUserAsync(string username, string lastName);
}

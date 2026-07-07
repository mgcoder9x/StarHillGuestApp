namespace FresherDev.HMS.Core.Users;

public interface IDeleteUserUseCase
{
    Task DeleteUserAsync(string username);

    Task DeleteAllUsersAsync();
}

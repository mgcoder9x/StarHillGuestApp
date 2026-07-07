namespace FresherDev.HMS.Core.Users;

public interface IAddUserUseCase
{
    Task AddUserAsync(string username);

    Task AddUserTransactionAsync();
}

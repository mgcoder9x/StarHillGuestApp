using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public partial class UserUseCase : IAddUserUseCase
{
    public async Task AddUserTransactionAsync()
    {
        var userRepository = this.GetGenericRepository<User>();
        var permissionRepository = this.GetGenericRepository<Permission>();

        using (var transaction = this.CreateTransaction())
        {
            await userRepository.AddAsync(new User()
            {
                Username = "luuquangict",
                Password = "password",
            });

            await permissionRepository.AddAsync(new Permission()
            {
                Discriminator = "Discry",
                Name = "Admin.AddUser",
            });

            await transaction.CommitAsync();
        }
    }

    public async Task AddUserAsync(string username)
    {
        var userRepository = this.GetGenericRepository<User>();
        await userRepository.AddAsync(new User()
        {
            Username = username,
            Password = "password",
        });
    }


}

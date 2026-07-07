using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public partial class UserUseCase : IDeleteUserUseCase
{
    public async Task DeleteAllUsersAsync()
    {
        var userRepository = this.GetGenericRepository<User>();
        var permissionRepository = this.GetGenericRepository<Permission>();
        var permis2 = this.GetRepository<PermissionRepository>();

        using (var transaction = this.CreateTransaction())
        {
            await userRepository.ExecuteDeleteAsync(x => true);
            await permissionRepository.ExecuteDeleteAsync(x => true);
            await transaction.CommitAsync();
        }
    }

    public async Task DeleteUserAsync(string username)
    {
        var userRepository = this.GetGenericRepository<User>();

        // var user = await userRepository.FindAsync(x => x.Username == username);
        // if (user == null)
        // {
        //     return NotFound("User not found: " + username);
        // }

        // await userRepository.DeleteAsync(user);

        await userRepository.ExecuteDeleteAsync(x => x.Username == username);
    }
}



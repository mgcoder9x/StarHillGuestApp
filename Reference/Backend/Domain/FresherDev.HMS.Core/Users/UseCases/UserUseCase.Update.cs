using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public partial class UserUseCase : IUpdateUserUseCase
{
    public async Task UpdateUserAsync(string username, string lastName)
    {
        var userRepository = this.GetGenericRepository<User>();

        var user = await userRepository.FindAsync(x => x.Username == username);
        if (user == null)
        {
            return;
        }

        user.LastName = lastName;
        await userRepository.UpdateAsync(user);
    }
}
using Microsoft.EntityFrameworkCore;

namespace FresherDev.HMS.EntityFramework;

public class UserRepository : BaseRepository<User, Guid>
{
    public UserRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<User>> GetUsersAsync(Guid userId)
    {
        return await this.AsQueryable()
                         .Where(x => x.Id == userId)
                         .ToListAsync();
    }

    public async Task<User> GetUserAsync(Guid userId)
    {
        //return await this.AsQueryable().Include(x => x.Address).ToListAsync();

        var user = await this.FindAsync(x => x.Id == userId, x => x.Address!, x => x.Tokens!);
        return user!;
    }
}
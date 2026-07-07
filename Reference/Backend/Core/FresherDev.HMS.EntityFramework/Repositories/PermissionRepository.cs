using Microsoft.EntityFrameworkCore;

namespace FresherDev.HMS.EntityFramework;

public class PermissionRepository : BaseRepository<Permission, Guid>
{
    public PermissionRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId)
    {
        return await this.AsQueryable()
                         .Where(x => x.UserId == userId)
                         .OrderBy(x => x.Name)
                         .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(Guid roleId)
    {
        return await this.AsQueryable()
                         .Where(x => x.RoleId == roleId)
                         .OrderBy(x => x.Name)
                         .ToListAsync();
    }
}

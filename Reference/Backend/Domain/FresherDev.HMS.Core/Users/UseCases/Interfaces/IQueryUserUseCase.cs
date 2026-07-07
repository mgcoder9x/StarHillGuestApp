using FresherDev.HMS.Common;
using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public interface IQueryUserUseCase
{
    Task<IEnumerable<UserModel>> GetUsersAsync();

    Task<IPaginationOutput<UserModel>> GetUserPaginationAsync(IPaginationInput input, QueryUserCriteria criteria);

    Task<IHttpResponse> GetUserAsync(Guid userId);
}
using AutoMapper;
using FresherDev.HMS.Common;
using FresherDev.HMS.Core.Shared;
using FresherDev.HMS.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FresherDev.HMS.Core.Users;

public partial class UserUseCase : BaseService, IQueryUserUseCase
{
    public UserUseCase(IUnitOfWork unitOfWork, IMapper mapper)
        : base(unitOfWork, mapper)
    {
    }

    public async Task<IEnumerable<UserModel>> GetUsersAsync()
    {
        var userRepository = this.GetGenericRepository<User>();

        var users = await userRepository.GetAsync(x => true, x => x.Address!, x => x.Tokens!);
        var userModels = Mapper.Map<IEnumerable<UserModel>>(users);

        return userModels;
    }

    public async Task<IHttpResponse> GetUserAsync(Guid userId)
    {
        var userRepository = this.GetGenericRepository<User>();
        var user = await userRepository.FindAsync(x => x.Id == userId, x => x.Address!, x => x.Tokens!);
        if (user == null)
        {
            return HttpResponse.NotFound();
        }

        var userModel = Mapper.Map<UserModel>(user);
        return HttpResponse<UserModel>.Ok(userModel);
    }

    public async Task<IPaginationOutput<UserModel>> GetUserPaginationAsync(IPaginationInput input, QueryUserCriteria criteria)
    {
        var userRepository = this.GetGenericRepository<User>();
        var userPagination = await userRepository
                            .AsQueryable()
                            .ApplyCriteria<User, UserCriteriaBuilder, QueryUserCriteria>(criteria)
                            .Include(x => x.Address)
                            .Include(x => x.Tokens)
                            .ToPaginationAsync(input);

        return MapPagination<User, UserModel>(userPagination);
    }
}
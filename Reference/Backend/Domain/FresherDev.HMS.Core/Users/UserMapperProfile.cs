using AutoMapper;
using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public class UserMapperProfile : Profile
{
    public UserMapperProfile()
    {
        CreateMap<AddUserInput, User>();

        CreateMap<UpdateUserInput, User>();

        CreateMap<User, UserModel>();
    }
}
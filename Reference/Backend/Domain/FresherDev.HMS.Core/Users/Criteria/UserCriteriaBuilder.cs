using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public class UserCriteriaBuilder : CriteriaBuilder<User, QueryUserCriteria>
{
    public UserCriteriaBuilder(QueryUserCriteria criteria) : base(criteria)
    {
    }

    public override void CreateModeling(QueryUserCriteria criteria)
    {

        if (!string.IsNullOrWhiteSpace(criteria.SearchString))
        {
            this.Or(x => x.Username.Contains(criteria.SearchString));
            this.Or(x => x.Email!.Contains(criteria.SearchString));
        }
    }
}
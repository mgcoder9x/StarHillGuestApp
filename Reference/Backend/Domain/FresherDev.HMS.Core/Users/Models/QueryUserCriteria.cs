using FresherDev.HMS.EntityFramework;

namespace FresherDev.HMS.Core.Users;

public class QueryUserCriteria : ICriteria
{
    public string? SearchString { get; set; }

    public int Level { get; set; }

    public int Type { get; set; }
}

public class QueryUserCriteria2 : ICriteria
{
    public string? SearchString { get; set; }

    public int Level { get; set; }

    public int Type { get; set; }
}
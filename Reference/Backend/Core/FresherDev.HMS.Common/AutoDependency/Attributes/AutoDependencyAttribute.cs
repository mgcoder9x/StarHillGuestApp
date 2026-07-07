namespace FresherDev.HMS.Common.AutoDependency;

public class AutoDependencyAttribute : Attribute
{
    public DependencyType Type { get; private set; }

    public AutoDependencyAttribute(DependencyType type = DependencyType.Transient)
    {
        Type = type;
    }
}

using FresherDev.HMS.Common;

namespace FresherDev.HMS.Infrastructure;

public class ForceLoadAssembly : ForceLoadAssemblyBase
{
    public static ForceLoadAssembly Instance { get; } = new ForceLoadAssembly();
}

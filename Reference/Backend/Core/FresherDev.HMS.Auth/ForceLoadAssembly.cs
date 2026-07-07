using FresherDev.HMS.Common;

namespace FresherDev.HMS.Auth;

public class ForceLoadAssembly : ForceLoadAssemblyBase
{
    public static ForceLoadAssembly Instance { get; } = new ForceLoadAssembly();
}
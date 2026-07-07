using FresherDev.HMS.Common;

namespace FresherDev.HMS.Domain;

public class ForceLoadAssembly : ForceLoadAssemblyBase
{
    public static ForceLoadAssembly Instance { get; } = new ForceLoadAssembly();
}

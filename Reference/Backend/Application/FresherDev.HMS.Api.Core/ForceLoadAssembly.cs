using FresherDev.HMS.Common;

namespace FresherDev.HMS.Api.Core;

public class ForceLoadAssembly : ForceLoadAssemblyBase
{
    public static ForceLoadAssembly Instance { get; } = new ForceLoadAssembly();
}
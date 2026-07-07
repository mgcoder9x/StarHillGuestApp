using FresherDev.HMS.Common;

namespace FresherDev.HMS.Api.Core.Shared;

public class ForceLoadAssembly : ForceLoadAssemblyBase
{
    public static ForceLoadAssembly Instance { get; } = new ForceLoadAssembly();
}
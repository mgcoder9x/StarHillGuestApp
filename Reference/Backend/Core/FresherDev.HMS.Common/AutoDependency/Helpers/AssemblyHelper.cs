using System.Reflection;

namespace FresherDev.HMS.Common.AutoDependency;

public class AssemblyHelper
{
    /// <summary>
    /// Get all types of current project
    /// </summary>    
    public static IEnumerable<Type> GetProjectTypes()
    {
        return GetProjectAssemblies()
            .SelectMany(x => x.GetTypes())
            .ToList();
    }

    /// <summary>
    /// Get all asesmblies of current project (exclude system assemblies)
    /// </summary>    
    private static IEnumerable<Assembly> GetProjectAssemblies()
    {
        var asesmblies = AppDomain.CurrentDomain
           .GetAssemblies()
           .OrderBy(x => x.FullName)
           .Where(x =>
           {

               if (string.IsNullOrEmpty(x.FullName))
               {
                   return false;
               }

               if (AutoConstants.IgnoreAssemblyNames.Any(ignore => x.FullName.StartsWith(ignore, StringComparison.OrdinalIgnoreCase)))
               {
                   return false;
               }

               return x.FullName.StartsWith(AutoConstants.AppNamespace, StringComparison.OrdinalIgnoreCase);
           })
           .OrderBy(x => x.FullName)
           .ToList();

        ConsoleHelper.Write("Project Assemblies", asesmblies.Select(x => x.FullName!));

        return asesmblies;
    }
}

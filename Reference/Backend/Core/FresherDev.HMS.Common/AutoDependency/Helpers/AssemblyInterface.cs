using System.Text.RegularExpressions;

namespace FresherDev.HMS.Common.AutoDependency;

internal class AssemblyInterface
{
    /// <summary>
    /// Lấy toàn bộ các Interface đúng định dạng
    /// </summary>
    public static ICollection<Type> GetValidInterfaces(IEnumerable<Type> projectTypes)
    {
        // Lấy ra toàn bộ các Interface trong assembly thỏa mãn các điều kiện:
        // - 1) Là interface        
        // - 2) Không gắn Attribute IgnoreAutoDependencyAttribute        
        // - Thỏa mãn hết trường hợp 1,2 và gắn AutoDependencyAttribute
        // - Thỏa mãn hết trường hợp 1,2 và có đúng định dạng

        var allInterfaces = projectTypes.Where(x => x.IsInterface).ToList();
        var ignoreInterfaces = allInterfaces.Where(x => x.HasAttributeNoInherit<IgnoreAutoDependencyAttribute>()).ToList();
        var autoInterfaces = allInterfaces.Where(x => x.HasAttributeNoInherit<AutoDependencyAttribute>()).ToList();

        var validInterfaces = allInterfaces.Where(x =>
        {
            // 2) Không gắn Attribute IgnoreAutoDependencyAttribute
            if (x.HasAttributeNoInherit<IgnoreAutoDependencyAttribute>())
            {
                return false;
            }

            // Nếu gắn AutoDependencyAttribute thì mặc định là true
            if (x.HasAttributeNoInherit<AutoDependencyAttribute>())
            {
                return true;
            }

            // Thỏa mãn pattern của 1 interface thì trả về true
            return ValidateInterface(x.Name);
        })
        .ToList();

        ConsoleHelper.Write("All Interfaces", allInterfaces.Select(x => x.FullName!));
        ConsoleHelper.Write("Ignore Interfaces", ignoreInterfaces.Select(x => x.FullName!));
        ConsoleHelper.Write("Auto Interfaces", autoInterfaces.Select(x => x.FullName!));

        ConsoleHelper.Write("Valid Interfaces", validInterfaces.Select(x => x.FullName!));

        return validInterfaces;
    }

    /// <summary>
    /// Validate các interface đúng với định dạng
    /// </summary>
    private static bool ValidateInterface(string name)
    {
        var regex = new Regex(AutoConstants.InterfacePattern);
        return regex.IsMatch(name);
    }
}

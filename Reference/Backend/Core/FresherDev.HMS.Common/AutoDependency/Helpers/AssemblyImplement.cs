namespace FresherDev.HMS.Common.AutoDependency;

internal static class AssemblyImplement
{
    /// <summary>
    /// Lấy ra toàn bộ các file đang được kế thừa từ <see cref="Type"/> hiện tại
    /// </summary>        
    public static ICollection<Type> GetImplementTypes(Type @interface, IEnumerable<Type> projectTypes)
    {
        return projectTypes.Where(type =>
            {
                if (!@interface.IsAssignableFrom(type))
                    return false;

                if (@interface.FullName == type.FullName)
                    return false;

                return true;
            })
            .ToList();
    }
}

using System.Reflection;

namespace FresherDev.HMS.Common;

public static class TypeExtensions
{
    /// <summary>
    /// Validate the <see cref="Type"/> has an attribute
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool HasAttribute<T>(this Type type)
       where T : Attribute
    {
        return type.GetCustomAttribute<T>(true) != null;
    }

    public static bool HasAttributeNoInherit<T>(this Type type)
       where T : Attribute
    {
        return type.GetCustomAttribute<T>(false) != null;
    }
}

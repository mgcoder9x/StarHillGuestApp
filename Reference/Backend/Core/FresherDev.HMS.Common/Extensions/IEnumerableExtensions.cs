namespace FresherDev.HMS.Common;

public static class IEnumerableExtensions
{
    public static bool SafeAny<T>(this IEnumerable<T> source)
    {
        if (source == null || source.Count() == 0)
        {
            return false;
        }

        return source.Any();
    }

    public static bool SafeAny<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null || source.Count() == 0)
        {
            return false;
        }

        return source.Any(predicate);
    }
}
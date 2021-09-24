using System.Collections.Generic;

namespace DSLKIT.Helpers
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Union<T>(this IEnumerable<T> e, T value)
        {
            foreach (var cur in e)
            {
                yield return cur;
            }

            yield return value;
        }

        public static IEnumerable<T> Union<T>(this T value, IEnumerable<T> e)
        {
            yield return value;

            foreach (var cur in e)
            {
                yield return cur;
            }
        }
    }
}
using System.Runtime.CompilerServices;

namespace Meander;

internal static class LinqExtensions
{
    private static IEnumerable<T> FromOne<T>(T value)
    {
        yield return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> dst, T value) => dst.Concat(FromOne(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyList<T> WithUpdatedElementAt<T>(this IReadOnlyList<T> src, int i, Func<T, T> updateFunc) => new List<T>(src) { [i] = updateFunc(src[i]) };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IReadOnlyList<T> WithUpdatedElementAt<T>(this IReadOnlyList<T> src, int i, Func<T, int, T> updateFunc) => new List<T>(src) { [i] = updateFunc(src[i], i) };

    public static IReadOnlyList<T> WithUpdatedElement<T>(this IReadOnlyList<T> src, Func<T, bool> match, Func<T, T> updateFunc)
    {
        var result = new List<T>(src.Count);
        for (int i = 0; i < src.Count; ++i)
        {
            var srcValue = src[i];
            result.Add(match(srcValue) ? updateFunc(srcValue) : srcValue);
        }

        return result;
    }

    public static IReadOnlyList<T> WithUpdatedElement<T>(this IReadOnlyList<T> src, Func<T, int, bool> match, Func<T, int, T> updateFunc)
    {
        var result = new List<T>(src.Count);
        for (int i = 0; i < src.Count; ++i)
        {
            var srcValue = src[i];
            result.Add(match(srcValue, i) ? updateFunc(srcValue, i) : srcValue);
        }

        return result;
    }
}

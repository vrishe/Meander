using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Meander;

internal static class DisposableExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static IDisposable PutOnRecord([NotNull] this IDisposable d, [NotNull] ICollection<IDisposable> record)
    {
        record.Add(d);
        return d;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisposeAll([NotNull] this IReadOnlyList<IDisposable> record)
    {
        for (int i = 0; i < record.Count; ++i)
        {
            record[i].Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisposeAll([NotNull] this IEnumerable<IDisposable> record)
    {
        foreach (var d in record)
        {
            d.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static ICollection<IDisposable> DisposeAll([NotNull] this ICollection<IDisposable> record)
    {
        foreach (var d in record)
        {
            d.Dispose();
        }

        return record;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static IList<IDisposable> DisposeAll([NotNull] this IList<IDisposable> record)
    {
        for (int i = 0; i < record.Count; ++i)
        {
            record[i].Dispose();
        }

        return record;
    }
}
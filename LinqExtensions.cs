using System;
using System.Collections.Generic;
using System.Linq;

namespace Permussion;

public static class LinqExtensions
{
    public static (IEnumerable<T> positiveItems, 
        IEnumerable<T> negativeItems,
        int positiveItemsCount,
        int negativeItemsCount) Predicategorize<T>(
        this IEnumerable<T> source,
        Predicate<T> predicate)
    {
        var positiveItems = System.Linq.Enumerable.Empty<T>();
        var negativeItems = System.Linq.Enumerable.Empty<T>();
        int positiveItemsCount = 0, negativeItemsCount = 0;
        foreach (var item in source)
            if (predicate(item))
            {
                positiveItems = positiveItems.Append(item);
                positiveItemsCount++;
            }
            else
            {
                negativeItems = negativeItems.Append(item);
                negativeItemsCount++;
            }

        return (positiveItems, negativeItems, positiveItemsCount, negativeItemsCount);
    }

    public static IEnumerable<T> DistinctWithHashSet<T>(this IEnumerable<T> source)
    {
        var hashSet = new HashSet<T>();
#pragma warning disable S3267
        foreach (var item in source)
#pragma warning restore S3267
            if (hashSet.Add(item))
                yield return item;
    }

    public static IEnumerable<short> DistinctWithShortHashSet(this IEnumerable<short> source)
    {
        var hashSet = new HashSet<short>();
#pragma warning disable S3267
        foreach (var item in source)
#pragma warning restore S3267
            if (hashSet.Add(item))
                yield return item;
    }
}
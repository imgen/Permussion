using System;
using System.Collections.Generic;
using System.Linq;

namespace Permussion;

public static class LinqExtensions
{
    public static (IEnumerable<T> positiveItems, IEnumerable<T> negativeItems) Predicategorize<T>(
        this IEnumerable<T> source,
        Predicate<T> predicate)
    {
        var positiveItems = Enumerable.Empty<T>();
        var negativeItems = Enumerable.Empty<T>();
        foreach (var item in source)
            if (predicate(item))
                positiveItems = positiveItems.Append(item);
            else
                negativeItems = negativeItems.Append(item);

        return (positiveItems, negativeItems);
    }
}
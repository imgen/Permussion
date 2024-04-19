﻿using System;
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
        var positiveItems = Enumerable.Empty<T>();
        var negativeItems = Enumerable.Empty<T>();
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
}
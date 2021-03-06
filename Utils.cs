using System.Collections.Generic;

namespace Permussion;

public class Utils
{
    public static int BinarySearch(short[] data, short item)
    {
        int min = 0;
        int max = data.Length - 1;
        do
        {
            int mid = (min + max) / 2;
            if (data[mid] == item)
                return mid;
            if (item > data[mid])
                min = mid + 1;
            else
                max = mid - 1;
        } while (min <= max);
        return -1;
    }

    public static int BinarySearch(LinkedList<short>[] data, short item)
    {
        int min = 0;
        int max = data.Length - 1;
        int mid = 0;
        do
        {
            mid = (min + max) / 2;
            var midValue = data[mid].First.Value;
            if (midValue == item)
                return mid;
            if (item > midValue)
                min = mid + 1;
            else
                max = mid - 1;
        } while (min <= max);
        return -mid;
    }
}
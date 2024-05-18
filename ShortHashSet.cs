using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Permussion;

public class ShortHashSet
{
    /// <summary>
    /// When constructing a hashset from an existing collection, it may contain duplicates,
    /// so this is used as the max acceptable excess ratio of capacity to count. Note that
    /// this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
    /// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
    /// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
    /// </summary>
    private const int StartOfFreeList = -3;

    private int[]? _buckets;
    private Entry[]? _entries;
    private ulong _fastModMultiplier;
    private int _count;
    private int _freeList;
    private int _freeCount;

    public ShortHashSet(int capacity = 0) => Initialize(capacity);

    /// <summary>
    /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
    /// greater than or equal to capacity.
    /// </summary>
    private void Initialize(int capacity)
    {
        var size = HashHelpers.GetPrime(capacity);
        var buckets = new int[size];
        var entries = new Entry[size];

        // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
        _freeList = -1;
        _buckets = buckets;
        _entries = entries;

        _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
    }

    /// <summary>Gets a reference to the specified hashcode's bucket, containing an index into <see cref="_entries"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucketRef(int hashCode)
    {
        var buckets = _buckets!;

        return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
    }

    public void Clear()
    {
        var count = _count;
        if (count > 0)
        {
            Array.Clear(_buckets!);
            _count = 0;
            _freeList = -1;
            _freeCount = 0;
            Array.Clear(_entries!, 0, count);
        }
    }
    public bool Add(short value)
    {
        var entries = _entries;
        Debug.Assert(entries != null, "expected entries to be non-null");

        int hashCode = value;

        ref var bucket = ref Unsafe.NullRef<int>();

        bucket = ref GetBucketRef(hashCode);
        var i = bucket - 1; // Value in _buckets is 1-based

        // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
        while (i >= 0)
        {
            ref var entry = ref entries[i];
            if (entry.Value == value)
                return false;
            i = entry.Next;
        }
        int index;
        if (_freeCount > 0)
        {
            index = _freeList;
            _freeCount--;
            _freeList = StartOfFreeList - entries[_freeList].Next;
        }
        else
        {
            var count = _count;
            if (count == entries.Length)
            {
                Resize();
                bucket = ref GetBucketRef(hashCode);
            }
            index = count;
            _count = count + 1;
            entries = _entries;
        }

        ref var entry2 = ref entries![index];
        entry2.Value = value;
        entry2.Next = bucket - 1; // Value in _buckets is 1-based
        bucket = index + 1;

        return true;
    }

    public int Count => _count - _freeCount;

    public void CopyTo(short[] array)
    {
        var arrayIndex = 0;
        var count = Count;

        var entries = _entries;
        for (var i = 0; i < _count && count != 0; i++)
        {
            ref var entry = ref entries![i];
            if (entry.Next >= -1)
            {
                array[arrayIndex++] = entry.Value;
                count--;
            }
        }
    }

    public short[] ToArray()
    {
        var array = new short[Count];
        CopyTo(array);
        return array;
    }

    private void Resize() => Resize(HashHelpers.ExpandPrime(_count));

    private void Resize(int newSize)
    {
        var entries = new Entry[newSize];

        var count = _count;
        Array.Copy(_entries!, entries, count);

        // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
        _buckets = new int[newSize];

        _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
        for (var i = 0; i < count; i++)
        {
            ref var entry = ref entries[i];
            if (entry.Next >= -1)
            {
                ref var bucket = ref GetBucketRef(entry.Value);
                entry.Next = bucket - 1; // Value in _buckets is 1-based
                bucket = i + 1;
            }
        }

        _entries = entries;
    }

    private struct Entry
    {
        /// <summary>
        /// 0-based index of next entry in chain: -1 means end of chain
        /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
        /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
        /// </summary>
        public int Next;
        public short Value;
    }
}

public static class HashHelpers
{
    public const uint HashCollisionThreshold = 100;

    // This is the maximum prime smaller than Array.MaxLength.
    public const int MaxPrimeArrayLength = 0x7FFFFFC3;

    public const int HashPrime = 101;

    // Table of prime numbers to use as hash table sizes.
    // A typical resize algorithm would pick the smallest prime number in this array
    // that is larger than twice the previous capacity.
    // Suppose our Hashtable currently has capacity x and enough elements are added
    // such that a resize needs to occur. Resizing first computes 2x then finds the
    // first prime in the table greater than 2x, i.e. if primes are ordered
    // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
    // Doubling is important for preserving the asymptotic complexity of the
    // hashtable operations such as add.  Having a prime guarantees that double
    // hashing does not lead to infinite loops.  IE, your hash function will be
    // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
    // We prefer the low computation costs of higher prime numbers over the increased
    // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
    internal static ReadOnlySpan<int> Primes =>
    [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];

    public static bool IsPrime(int candidate)
    {
        if ((candidate & 1) != 0)
        {
            int limit = (int)Math.Sqrt(candidate);
            for (int divisor = 3; divisor <= limit; divisor += 2)
            {
                if ((candidate % divisor) == 0)
                    return false;
            }
            return true;
        }
        return candidate == 2;
    }

    public static int GetPrime(int min)
    {
        foreach (int prime in Primes)
        {
            if (prime >= min)
                return prime;
        }

        // Outside of our predefined table. Compute the hard way.
        for (int i = (min | 1); i < int.MaxValue; i += 2)
        {
            if (IsPrime(i) && ((i - 1) % HashPrime != 0))
                return i;
        }
        return min;
    }

    // Returns size of hashtable to grow to.
    public static int ExpandPrime(int oldSize)
    {
        int newSize = 2 * oldSize;

        // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
        {
            Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
            return MaxPrimeArrayLength;
        }

        return GetPrime(newSize);
    }

    /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
    /// <remarks>This should only be used on 64-bit.</remarks>
    public static ulong GetFastModMultiplier(uint divisor) =>
        ulong.MaxValue / divisor + 1;

    /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
    /// <remarks>This should only be used on 64-bit.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FastMod(uint value, uint divisor, ulong multiplier)
    {
        // We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
        // which allows to avoid the long multiplication if the divisor is less than 2**31.
        Debug.Assert(divisor <= int.MaxValue);

        // This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
        // is faster than BigMul currently because we only need the high bits.
        uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

        Debug.Assert(highbits == value % divisor);
        return highbits;
    }
}


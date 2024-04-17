using System;
using System.Linq;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System.Threading;

namespace Permussion;

static class OneMsPermussioned
{
    public static (PermissionCheck[] permissionChecks, int count) CalculatePermissionChecksDistinct(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap,
        int maxPsId)
    {
        var permissionChecks = permissionSetMap.SelectMany(
            pair => pair.Value
                .SelectMany(x => permissionGroupOccurenceMap[x])
                .Distinct()
                .Select(x => new PermissionCheck(pair.Key, x))
        ).ToArray();

        return (permissionChecks, permissionChecks.Length);
    }

    public static (PermissionCheck[], int count)
        CalculatePermissionChecksUnion(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
            int maxPsId)
    {
        var permissionChecks = permissionSetMap.SelectMany(pair =>
        {
            var pgIds = pair.Value;
            return pgIds.Skip(1).Aggregate(
                permissionGroupOccurenceMap[pgIds[0]],
                (union, pgId) =>
                    union.Union(permissionGroupOccurenceMap[pgId]).ToArray()
            ).Select(x => new PermissionCheck(pair.Key, x));
        }).ToArray();

        return (permissionChecks, permissionChecks.Length);
    }

    public static (short[] psId1s, short[] psId2s, int count)
        CalculatePermissionChecks(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
            int maxPsId
        )
    {
        unchecked
        {
            var psIds = permissionSetMap.Keys.ToArray();
            int totalPermutationCount = 0;
            int psIdCount = psIds.Length;
            int i = psIdCount;
            while (--i >= 0)
            {
                var pgIds = permissionSetMap[psIds[i]];
                var j = pgIds.Length;
                while (--j >= 0)
                {
                    totalPermutationCount +=
                        permissionGroupOccurenceMap[pgIds[j]].Length;
                }
                totalPermutationCount -= pgIds.Length - 1;
            }
            var psId1s = new short[totalPermutationCount];
            var psId2s = new short[totalPermutationCount];
            int chunkSize = maxPsId + 1;
            var deduplicater = new short[psIdCount * chunkSize];
            var cache = new short[psIdCount * chunkSize];
            int startIndex = 0;
            Parallel.ForEach(
                psIds,
                (psId, _, psIndex) =>
                {
                    int permissionChecksCount = 0;
                    var pgIds = permissionSetMap[psId];
                    var pgCount = pgIds.Length;
                    int newStartIndex, oldStartIndex;
                    if (pgCount == 1)
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        permissionChecksCount = pgOccurences.Length;
                        newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        oldStartIndex = newStartIndex - permissionChecksCount;

                        Array.Fill(psId1s,
                            psId,
                            oldStartIndex,
                            permissionChecksCount);

                        Array.Copy(
                            pgOccurences,
                            0,
                            psId2s,
                            oldStartIndex,
                            permissionChecksCount
                        );
                        return;
                    }
                    int chunkStartIndex = (int)psIndex * chunkSize;

                    int i = -1;
                    while (++i < pgCount)
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[i]];
                        var j = pgOccurences.Length;
                        int hashIndex = 0;
                        while (--j >= 0)
                        {
                            var psId2 = pgOccurences[j];
                            hashIndex = chunkStartIndex + psId2;
                            if (deduplicater[hashIndex] == 0)
                            {
                                deduplicater[hashIndex] = 1;
                                cache[chunkStartIndex + permissionChecksCount++] = psId2;
                            }
                        }
                    }

                    newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                    oldStartIndex = newStartIndex - permissionChecksCount;

                    Array.Fill(psId1s,
                        psId,
                        oldStartIndex,
                        permissionChecksCount);

                    Array.Copy(
                        cache,
                        chunkStartIndex,
                        psId2s,
                        oldStartIndex,
                        permissionChecksCount
                    );
                }
            );
            return (psId1s, psId2s, startIndex);
        }
    }

    public static (short[] psId1s, short[] psId2s, int count)
        CalculatePermissionChecksFaster(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
            int maxPsId
        )
    {
        unchecked
        {
            var psIds = permissionSetMap.Keys.ToArray();
            int totalPermutationCount = 0;
            int psIdCount = psIds.Length;
            int i = psIdCount;
            while (--i >= 0)
            {
                var pgIds = permissionSetMap[psIds[i]];
                int pgCount = pgIds.Length;
                int maxOccurencePgCount = 0;
                int maxOccurencePgIndex = -1;

                var j = -1;
                while (++j < pgCount)
                {
                    int occurenceCount = permissionGroupOccurenceMap[pgIds[j]].Length;
                    totalPermutationCount += occurenceCount;
                    if (occurenceCount > maxOccurencePgCount)
                    {
                        maxOccurencePgCount = occurenceCount;
                        maxOccurencePgIndex = j;
                    }
                }

                if (maxOccurencePgIndex > 0)
                {
                    (pgIds[0], pgIds[maxOccurencePgIndex]) =
                        (pgIds[maxOccurencePgIndex], pgIds[0]);
                }
                totalPermutationCount -= pgIds.Length - 1;
            }

            var psId1s = new short[totalPermutationCount];
            var psId2s = new short[totalPermutationCount];
            int chunkSize = maxPsId + 1;
            var deduplicater = new short[psIdCount * chunkSize];
            var cache = new short[psIdCount * chunkSize];
            int startIndex = 0;
            Parallel.ForEach(
                psIds,
                (psId, _, psIndex) =>
                {
                    int permissionChecksCount = 0;
                    var pgIds = permissionSetMap[psId];
                    var pgCount = pgIds.Length;
                    int newStartIndex, oldStartIndex;
                    if (pgCount == 1)
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        permissionChecksCount = pgOccurences.Length;
                        newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        oldStartIndex = newStartIndex - permissionChecksCount;

                        Array.Fill(psId1s,
                            psId,
                            oldStartIndex,
                            permissionChecksCount);

                        Array.Copy(
                            pgOccurences,
                            0,
                            psId2s,
                            oldStartIndex,
                            permissionChecksCount
                        );
                        return;
                    }
                    int chunkStartIndex = (int)psIndex * chunkSize;

                    var firstPgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                    int firstPgOccurenceCount = firstPgOccurences.Length;
                    var j = -1;
                    while (++j < firstPgOccurenceCount)
                    {
                        deduplicater[chunkStartIndex + firstPgOccurences[j]] = 1;
                    }
                    permissionChecksCount += firstPgOccurences.Length;

                    int i = 0;
                    while (++i < pgCount)
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[i]];
                        j = pgOccurences.Length;
                        int hashIndex = 0;
                        while (--j >= 0)
                        {
                            var psId2 = pgOccurences[j];
                            hashIndex = chunkStartIndex + psId2;
                            if (deduplicater[hashIndex] == 0)
                            {
                                deduplicater[hashIndex] = 1;
                                cache[chunkStartIndex + permissionChecksCount++] = psId2;
                            }
                        }
                    }

                    newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                    oldStartIndex = newStartIndex - permissionChecksCount;

                    Array.Fill(psId1s,
                        psId,
                        oldStartIndex,
                        permissionChecksCount);

                    Array.Copy(
                        cache,
                        chunkStartIndex,
                        psId2s,
                        oldStartIndex,
                        permissionChecksCount
                    );
                }
            );
            return (psId1s, psId2s, startIndex);
        }
    }

    public static (short[] psId1s, short[] psId2s, int count)
        CalculatePermissionChecksAllParallel(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
            int maxPsId
        )
    {
        unchecked
        {
            var psIds = permissionSetMap.Keys.ToArray();
            int totalPermutationCount = 0;
            int psIdCount = psIds.Length;

            Parallel.ForEach(psIds, psId =>
            {
                var pgIds = permissionSetMap[psId];
                int pgCount = pgIds.Length;
                int maxOccurencePgCount = 0;
                int maxOccurencePgIndex = -1;

                var j = -1;
                int permutationCount = 0;
                while (++j < pgCount)
                {
                    int occurenceCount = permissionGroupOccurenceMap[pgIds[j]].Length;
                    permutationCount += occurenceCount;
                    if (occurenceCount > maxOccurencePgCount)
                    {
                        maxOccurencePgCount = occurenceCount;
                        maxOccurencePgIndex = j;
                    }
                }

                if (maxOccurencePgIndex > 0)
                {
                    (pgIds[0], pgIds[maxOccurencePgIndex]) =
                        (pgIds[maxOccurencePgIndex], pgIds[0]);
                }
                permutationCount -= pgIds.Length - 1;

                Interlocked.Add(ref totalPermutationCount, permutationCount);
            });

            var psId1s = new short[totalPermutationCount];
            var psId2s = new short[totalPermutationCount];
            int chunkSize = maxPsId + 1;
            var deduplicater = new short[psIdCount * chunkSize];
            var cache = new short[psIdCount * chunkSize];
            int startIndex = 0;
            Parallel.ForEach(
                psIds,
                (psId, _, psIndex) =>
                {
                    int permissionChecksCount = 0;
                    var pgIds = permissionSetMap[psId];
                    var pgCount = pgIds.Length;
                    int newStartIndex, oldStartIndex;
                    if (pgCount == 1)
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        permissionChecksCount = pgOccurences.Length;
                        newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        oldStartIndex = newStartIndex - permissionChecksCount;

                        Array.Fill(psId1s,
                            psId,
                            oldStartIndex,
                            permissionChecksCount);

                        Array.Copy(
                            pgOccurences,
                            0,
                            psId2s,
                            oldStartIndex,
                            permissionChecksCount
                        );
                        return;
                    }
                    int chunkStartIndex = (int)psIndex * chunkSize;

                    var firstPgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                    int firstPgOccurenceCount = firstPgOccurences.Length;
                    var j = -1;
                    while (++j < firstPgOccurenceCount)
                    {
                        deduplicater[chunkStartIndex + firstPgOccurences[j]] = 1;
                    }
                    permissionChecksCount += firstPgOccurences.Length;

                    int i = 0;
                    while (++i < pgCount)
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[i]];
                        j = pgOccurences.Length;
                        int hashIndex = 0;
                        while (--j >= 0)
                        {
                            var psId2 = pgOccurences[j];
                            hashIndex = chunkStartIndex + psId2;
                            if (deduplicater[hashIndex] == 0)
                            {
                                deduplicater[hashIndex] = 1;
                                cache[chunkStartIndex + permissionChecksCount++] = psId2;
                            }
                        }
                    }

                    newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                    oldStartIndex = newStartIndex - permissionChecksCount;

                    Array.Fill(psId1s,
                        psId,
                        oldStartIndex,
                        permissionChecksCount);

                    Array.Copy(
                        cache,
                        chunkStartIndex,
                        psId2s,
                        oldStartIndex,
                        permissionChecksCount
                    );
                }
            );
            return (psId1s, psId2s, startIndex);
        }
    }
}
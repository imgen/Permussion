using System;
using System.Linq;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System.Threading;
using System.Collections.Generic;

namespace Permussion
{
    class OneMsPermussioned
    {
        public static (PermissionCheck[] permissionChecks, int count) CalculatePermissionChecksDistinct(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
        {
            var permissionChecks = permissionSetMap.SelectMany(
                pair => pair.Value
                    .SelectMany(x => permissionGroupOccuranceMap[x])
                    .Distinct()
                    .Select(x => new PermissionCheck(pair.Key, x))
                ).ToArray();

            return (permissionChecks, permissionChecks.Length);
        }

        public static (PermissionCheck[], int count)
            CalculatePermissionChecksUnion(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
        {
            var permissionChecks = permissionSetMap.SelectMany(pair =>
            {
                var pgIds = pair.Value;
                return pgIds.Skip(1).Aggregate(
                        permissionGroupOccuranceMap[pgIds[0]],
                        (union, pgId) =>
                            union.Union(permissionGroupOccuranceMap[pgId]).ToArray()
                    ).Select(x => new PermissionCheck(pair.Key, x));
            }).ToArray();

            return (permissionChecks, permissionChecks.Length);
        }

        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecks(
                PermissionSetMap permissionSetMap,
                PermissionGroupOccuranceMap permissionGroupOccuranceMap,
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
                            permissionGroupOccuranceMap[pgIds[j]].Length;
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
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            permissionChecksCount = pgOccurances.Length;
                            newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                            oldStartIndex = newStartIndex - permissionChecksCount;

                            Array.Fill(psId1s,
                                psId,
                                oldStartIndex,
                                permissionChecksCount);

                            Array.Copy(
                                    pgOccurances,
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
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            var j = pgOccurances.Length;
                            int hashIndex = 0;
                            while (--j >= 0)
                            {
                                var psId2 = pgOccurances[j];
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
                PermissionGroupOccuranceMap permissionGroupOccuranceMap,
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
                    int maxOccurangePgCount = 0;
                    int maxOccurancePgIndex = -1;

                    var j = -1;
                    while (++j < pgCount)
                    {
                        int occuranceCount = permissionGroupOccuranceMap[pgIds[j]].Length;
                        totalPermutationCount += occuranceCount;
                        if (occuranceCount > maxOccurangePgCount)
                        {
                            maxOccurangePgCount = occuranceCount;
                            maxOccurancePgIndex = j;
                        }
                    }

                    if (maxOccurancePgIndex > 0)
                    {
                        (pgIds[0], pgIds[maxOccurancePgIndex]) =
                            (pgIds[maxOccurancePgIndex], pgIds[0]);
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
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            permissionChecksCount = pgOccurances.Length;
                            newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                            oldStartIndex = newStartIndex - permissionChecksCount;

                            Array.Fill(psId1s,
                                psId,
                                oldStartIndex,
                                permissionChecksCount);

                            Array.Copy(
                                    pgOccurances,
                                    0,
                                    psId2s,
                                    oldStartIndex,
                                    permissionChecksCount
                                );
                            return;
                        }
                        int chunkStartIndex = (int)psIndex * chunkSize;

                        var firstPgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        int firstPgOccuranceCount = firstPgOccurances.Length;
                        var j = -1;
                        while (++j < firstPgOccuranceCount)
                        {
                            deduplicater[chunkStartIndex + firstPgOccurances[j]] = 1;
                        }
                        permissionChecksCount += firstPgOccurances.Length;

                        int i = 0;
                        while (++i < pgCount)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            j = pgOccurances.Length;
                            int hashIndex = 0;
                            while (--j >= 0)
                            {
                                var psId2 = pgOccurances[j];
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
            CalculatePermissionChecksWithLinkedList(
                PermissionSetMap permissionSetMap,
                PermissionGroupOccuranceMap permissionGroupOccuranceMap,
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
                    int maxOccurangePgCount = 0;
                    int maxOccurancePgIndex = -1;

                    var j = -1;
                    while (++j < pgCount)
                    {
                        int occuranceCount = permissionGroupOccuranceMap[pgIds[j]].Length;
                        totalPermutationCount += occuranceCount;
                        if (occuranceCount > maxOccurangePgCount)
                        {
                            maxOccurangePgCount = occuranceCount;
                            maxOccurancePgIndex = j;
                        }
                    }

                    if (maxOccurancePgIndex > 0)
                    {
                        (pgIds[0], pgIds[maxOccurancePgIndex]) =
                            (pgIds[maxOccurancePgIndex], pgIds[0]);
                    }
                    totalPermutationCount -= pgIds.Length - 1;
                }

                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
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
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            permissionChecksCount = pgOccurances.Length;
                            newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                            oldStartIndex = newStartIndex - permissionChecksCount;

                            Array.Fill(psId1s,
                                psId,
                                oldStartIndex,
                                permissionChecksCount);

                            Array.Copy(
                                    pgOccurances,
                                    0,
                                    psId2s,
                                    oldStartIndex,
                                    permissionChecksCount
                                );
                            return;
                        }

                        var firstPgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        int firstPgOccuranceCount = firstPgOccurances.Length;
                        var buckets = new LinkedList<short>[firstPgOccuranceCount];
                        var j = -1;
                        while (++j < firstPgOccuranceCount)
                        {
                            buckets[j] = new LinkedList<short>();
                            buckets[j].AddFirst(firstPgOccurances[j]);
                        }
                        permissionChecksCount += firstPgOccurances.Length;

                        int i = 0;
                        LinkedListNode<short> previousNode;
                        while (++i < pgCount)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            j = pgOccurances.Length;

                        }

                        newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        oldStartIndex = newStartIndex - permissionChecksCount;

                        Array.Fill(psId1s,
                            psId,
                            oldStartIndex,
                            permissionChecksCount);
                    }
                );
                return (psId1s, psId2s, startIndex);
            }
        }

        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksAllParallel(
                PermissionSetMap permissionSetMap,
                PermissionGroupOccuranceMap permissionGroupOccuranceMap,
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
                    int maxOccurangePgCount = 0;
                    int maxOccurancePgIndex = -1;

                    var j = -1;
                    int permutationCount = 0;
                    while (++j < pgCount)
                    {
                        int occuranceCount = permissionGroupOccuranceMap[pgIds[j]].Length;
                        permutationCount += occuranceCount;
                        if (occuranceCount > maxOccurangePgCount)
                        {
                            maxOccurangePgCount = occuranceCount;
                            maxOccurancePgIndex = j;
                        }
                    }

                    if (maxOccurancePgIndex > 0)
                    {
                        (pgIds[0], pgIds[maxOccurancePgIndex]) =
                            (pgIds[maxOccurancePgIndex], pgIds[0]);
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
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            permissionChecksCount = pgOccurances.Length;
                            newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                            oldStartIndex = newStartIndex - permissionChecksCount;

                            Array.Fill(psId1s,
                                psId,
                                oldStartIndex,
                                permissionChecksCount);

                            Array.Copy(
                                    pgOccurances,
                                    0,
                                    psId2s,
                                    oldStartIndex,
                                    permissionChecksCount
                                );
                            return;
                        }
                        int chunkStartIndex = (int)psIndex * chunkSize;

                        var firstPgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        int firstPgOccuranceCount = firstPgOccurances.Length;
                        var j = -1;
                        while (++j < firstPgOccuranceCount)
                        {
                            deduplicater[chunkStartIndex + firstPgOccurances[j]] = 1;
                        }
                        permissionChecksCount += firstPgOccurances.Length;

                        int i = 0;
                        while (++i < pgCount)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            j = pgOccurances.Length;
                            int hashIndex = 0;
                            while (--j >= 0)
                            {
                                var psId2 = pgOccurances[j];
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
}

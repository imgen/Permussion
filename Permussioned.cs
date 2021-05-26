using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short,
    System.Collections.Generic.List<short>>;
using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;

namespace Permussion
{
    public static class Permussioned
    {
        public static (PermissionCheck[] permissionChecks, int count) CalculatePermissionChecks(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap)
        {
            var permissionChecks = permissionSetMap.SelectMany(
                pair => pair.Value
                    .SelectMany(x => permissionGroupOccuranceMap[x])
                    .Distinct()
                    .Select(x => new PermissionCheck(pair.Key, x))
                ).ToArray();

            return (permissionChecks, permissionChecks.Length);
        }

        public static (PermissionCheck[], int count) CalculatePermissionChecksV3(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap)
        {
            int totalPermutationCount = permissionSetMap
                .SelectMany(x => x.Value)
                .Sum(x => permissionGroupOccuranceMap[x].Count);
            var permissionChecks = new PermissionCheck[totalPermutationCount];
            int index = 0;
            foreach (var (psId, pgIds) in permissionSetMap)
            {
                foreach (var psId2 in
                    pgIds.SelectMany(x => permissionGroupOccuranceMap[x])
                    .Distinct())
                {
                    permissionChecks[index++] = new PermissionCheck(psId, psId2);
                }
            }

            return (permissionChecks, index);
        }

        public static (PermissionCheck[], int count) CalculatePermissionChecksFinally(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap)
        {
            var psIds = permissionSetMap.Keys.ToArray();
            int totalPermutationCount = permissionSetMap
                .SelectMany(x => x.Value)
                .Sum(x => permissionGroupOccuranceMap[x].Count);
            int index = -1;
            var permissionChecks = new PermissionCheck[totalPermutationCount];
            Parallel.ForEach(
                psIds,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 100
                },
                psId =>
                {
                    foreach (var psId2 in
                        permissionSetMap[psId].SelectMany(x => permissionGroupOccuranceMap[x])
                        .Distinct())
                    {
                        permissionChecks[Interlocked.Increment(ref index)] = new PermissionCheck(psId, psId2);
                    }
                }
            );

            return (permissionChecks, index + 1);
        }

        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksFinalTouch(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
        {
            unchecked
            {
                var psIds = permissionSetMap.Keys.ToArray();
                int totalPermutationCount = 0;
                int i = psIds.Length;
                while (--i >= 0)
                {
                    var pgIds = permissionSetMap[psIds[i]];
                    var j = pgIds.Count;
                    while (--j >= 0)
                    {
                        totalPermutationCount +=
                            permissionGroupOccuranceMap[pgIds[j]].Count;
                    }
                    totalPermutationCount -= pgIds.Count - 1;
                }
                int index = -1;
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
                Parallel.ForEach(
                    psIds,
                    psId =>
                    {
                        var hashSet = new HashSet<int>();
                        var pgIds = permissionSetMap[psId];
                        int i = pgIds.Count;
                        while (--i >= 0)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            int j = pgOccurances.Count;
                            while (--j >= 0)
                            {
                                short psId2 = pgOccurances[j];
                                if (hashSet.Contains(psId2) is false)
                                {
                                    hashSet.Add(psId2);
                                    var index2 = Interlocked.Increment(ref index);
                                    psId1s[index2] = psId;
                                    psId2s[index2] = psId2;
                                }
                            }
                        }
                    }
                );

                return (psId1s, psId2s, index + 1);
            }
        }

        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksFinalFinish(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
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
                    var j = pgIds.Count;
                    while (--j >= 0)
                    {
                        totalPermutationCount +=
                            permissionGroupOccuranceMap[pgIds[j]].Count;
                    }
                    totalPermutationCount -= pgIds.Count - 1;
                }
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
                int chunkSize = maxPsId + 1;
                var deduplicater = new short[psIdCount * chunkSize];
                int startIndex = 0;
                Parallel.ForEach(
                    psIds,
                    (psId, _, psIndex) =>
                    {
                        int chunkStartIndex = (int)psIndex * chunkSize;
                        var hashSet = new Span<short>(
                            deduplicater,
                            chunkStartIndex,
                            chunkSize
                        );
                        int permissionChecksCount = 0;
                        var pgIds = permissionSetMap[psId];
                        int i = -1;
                        while (++i < pgIds.Count)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            var j = pgOccurances.Count;
                            while (--j >= 0)
                            {
                                var psId2 = pgOccurances[j];
                                if (hashSet[psId2] == 0)
                                {
                                    hashSet[psId2] = 1;
                                    permissionChecksCount++;
                                }
                            }
                        }

                        int newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        var oldStartIndex = newStartIndex - permissionChecksCount;

                        short l = 1;
                        for (int k = 0; k < permissionChecksCount; k++)
                        {
                            psId1s[oldStartIndex + k] = psId;
                            while (hashSet[l] == 0)
                            {
                                l++;
                            }
                            hashSet[k] = l;
                            l++;
                        }

                        Array.Copy(
                                deduplicater,
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
            CalculatePermissionChecksFinalFinishUp(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
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
                    var j = pgIds.Count;
                    while (--j >= 0)
                    {
                        totalPermutationCount +=
                            permissionGroupOccuranceMap[pgIds[j]].Count;
                    }
                    totalPermutationCount -= pgIds.Count - 1;
                }
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
                int chunkSize = maxPsId + 1;
                var deduplicater = new short[psIdCount * chunkSize];
                var caches = new short[psIdCount * chunkSize];
                int startIndex = 0;
                Parallel.ForEach(
                    psIds,
                    (psId, _, psIndex) =>
                    {
                        int chunkStartIndex = (int)psIndex * chunkSize;
                        var hashSet = new Span<short>(
                            deduplicater,
                            chunkStartIndex,
                            chunkSize
                        );
                        var cache = new Span<short>(
                            caches,
                            chunkStartIndex,
                            chunkSize
                        );
                        int permissionChecksCount = 0;
                        var pgIds = permissionSetMap[psId];
                        int i = -1;
                        while (++i < pgIds.Count)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            var j = pgOccurances.Count;
                            while (--j >= 0)
                            {
                                var psId2 = pgOccurances[j];
                                if (hashSet[psId2] == 0)
                                {
                                    hashSet[psId2] = 1;
                                    cache[permissionChecksCount++] = psId2;
                                }
                            }
                        }

                        int newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        var oldStartIndex = newStartIndex - permissionChecksCount;

                        int k = -1;
                        while (++k < permissionChecksCount)
                        {
                            psId1s[oldStartIndex + k] = psId;
                        }

                        Array.Copy(
                                caches,
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
            CalculatePermissionChecksFinalFinishUpper(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
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
                    var j = pgIds.Count;
                    while (--j >= 0)
                    {
                        totalPermutationCount +=
                            permissionGroupOccuranceMap[pgIds[j]].Count;
                    }
                    totalPermutationCount -= pgIds.Count - 1;
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
                        int chunkStartIndex = (int)psIndex * chunkSize;

                        int permissionChecksCount = 0;
                        var pgIds = permissionSetMap[psId];
                        int i = -1;
                        while (++i < pgIds.Count)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            var j = pgOccurances.Count;
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

                        int newStartIndex = Interlocked.Add(ref startIndex, permissionChecksCount);
                        var oldStartIndex = newStartIndex - permissionChecksCount;

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
            CalculatePermissionChecksFinalFinishUpmost(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap,
            int maxPsId)
        {
            unchecked
            {
                var psIds = permissionSetMap.Keys.ToArray();
                int totalPermutationCount = 0;
                int psIdCount = psIds.Length;
                int i = -1;
                int[] startIndexes = new int[psIdCount + 1];
                var counts = new short[psIdCount];
                startIndexes[0] = 0;
                while (++i < psIdCount)
                {
                    var pgIds = permissionSetMap[psIds[i]];
                    var j = pgIds.Count;
                    while (--j >= 0)
                    {
                        totalPermutationCount +=
                            permissionGroupOccuranceMap[pgIds[j]].Count;
                    }
                    totalPermutationCount -= pgIds.Count - 1;
                    startIndexes[i + 1] = totalPermutationCount;
                }
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
                int chunkSize = maxPsId + 1;
                var deduplicater = new short[psIdCount * chunkSize];
                int totalPermissionCheckCount = 0;
                Parallel.ForEach(
                    psIds,
                    (psId, _, psIndexLong) =>
                    {
                        var psIndex = (int)psIndexLong;
                        int chunkStartIndex = psIndex * chunkSize;
                        var startIndex = startIndexes[psIndex];

                        short permissionChecksCount = 0;
                        var pgIds = permissionSetMap[psId];
                        int pgCount = pgIds.Count;

                        int i = -1;
                        while (++i < pgCount)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            var j = pgOccurances.Count;
                            int hashIndex = 0;
                            while (--j >= 0)
                            {
                                var psId2 = pgOccurances[j];
                                hashIndex = chunkStartIndex + psId2;
                                if (deduplicater[hashIndex] == 0)
                                {
                                    deduplicater[hashIndex] = 1;
                                    psId2s[startIndex + permissionChecksCount++] = psId2;
                                }
                            }
                        }

                        //Interlocked.Add(ref totalPermissionCheckCount, permissionChecksCount);
                        counts[psIndex] = permissionChecksCount;
                        Array.Fill(psId1s,
                            psId,
                            startIndex,
                            permissionChecksCount
                        );
                    }
                );

                i = -1;
                while (++i < psIdCount)
                {
                    totalPermissionCheckCount += counts[i];
                }

                //int frontChunkIndex = 0;
                //int backChunkIndex = psIdCount - 1;
                //int front = counts[0];
                //int back = startIndexes[backChunkIndex] + counts[backChunkIndex];

                //while(front < back)
                //{

                //}

                return (psId1s, psId2s, totalPermissionCheckCount);
            }
        }

        public static (PermissionCheck[] permissionChecks, int count) CalculatePermissionChecksFinal(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccuranceMap permissionGroupOccuranceMap)
        {
            var psIds = permissionSetMap.Keys.ToArray();
            int totalPermutationCount = 0;
            int psCount = psIds.Length;
            // startIndexes will indicate the start index of the chunk of memory
            // that exclusively belongs to a particular permission set while doing
            // parallel processing
            int[] startIndexes = new int[psCount + 1];
            startIndexes[0] = 0;
            int i = 0;
            for (; i < psCount; i++)
            {
                var pgIds = permissionSetMap[psIds[i]];
                totalPermutationCount += permissionSetMap[psIds[i]]
                    .Sum(x => permissionGroupOccuranceMap[x].Count) +
                    1 - pgIds.Count;
                startIndexes[i + 1] = totalPermutationCount;
            }
            int count = 0;
            var permissionChecks = new PermissionCheck[totalPermutationCount];
            Parallel.ForEach(
                psIds,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 100
                },
                (psId, _, index) =>
                {
                    var startIndex = startIndexes[index];
                    foreach (var psId2 in
                        permissionSetMap[psId].SelectMany(x => permissionGroupOccuranceMap[x])
                        .Distinct())
                    {
                        permissionChecks[startIndex++] = new PermissionCheck(psId, psId2);
                    }
                    Interlocked.Add(ref count, startIndex - startIndexes[index]);
                }
            );

            return (permissionChecks, count);
        }
    }

    public record PermissionSetGroup(
        short PermissionSetId,
        short PermissionGroupId,
        bool IsUserPermissionSet
    );

    public record PermissionCheck(short PermissionSetId1, short PermissionSetId2);
}

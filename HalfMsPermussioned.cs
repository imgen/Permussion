using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short, short[]>;

namespace Permussion
{
    public static class HalfMsPermussioned
    {
        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksParallel500us(
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
                    int pgCount = pgIds.Length;
                    int maxPossibleSubsetMatchCount =
                        permissionGroupOccuranceMap[pgIds[0]].Length;
                    int j = 0;
                    while(++j < pgCount)
                    {
                        var pgOccuranceCount = permissionGroupOccuranceMap[pgIds[j]].Length;
                        if (pgOccuranceCount < maxPossibleSubsetMatchCount)
                        {
                            maxPossibleSubsetMatchCount = pgOccuranceCount;
                        }
                    }
                    totalPermutationCount += maxPossibleSubsetMatchCount;
                }
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];

                int startIndex = 0;
                Parallel.ForEach(
                    psIds,
                    psId =>
                    {
                        var pgIds = permissionSetMap[psId];
                        int pgCount = pgIds.Length;
                        if (pgCount == 1)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            int newStartIndex = Interlocked.Add(ref startIndex, pgOccurancesCount);
                            var oldStartIndex = newStartIndex - pgOccurancesCount;
                            Array.Fill(psId1s, psId, oldStartIndex, pgOccurancesCount);
                            Array.Copy(pgOccurances, 0, psId2s, oldStartIndex, pgOccurancesCount);
                        }
                        else
                        {
                            var (prevIntersection, intersection) =
                                (new HashSet<short>(), new HashSet<short>());
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            int j = -1;
                            while (++j < pgOccurancesCount)
                            {
                                prevIntersection.Add(pgOccurances[j]);
                            }

                            int i = 0;
                            while (++i < pgCount)
                            {
                                pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                                pgOccurancesCount = pgOccurances.Length;
                                j = -1;
                                while (++j < pgOccurancesCount)
                                {
                                    short psId2 = pgOccurances[j];
                                    if (prevIntersection.Contains(psId2))
                                    {
                                        intersection.Add(psId2);
                                    }
                                }
                                if (i < pgCount - 1)
                                {
                                    prevIntersection.Clear();
                                    (prevIntersection, intersection) = (intersection, prevIntersection);
                                }
                            }

                            int matchCount = intersection.Count;

                            int newStartIndex = Interlocked.Add(ref startIndex, matchCount);
                            var oldStartIndex = newStartIndex - matchCount;
                            Array.Fill(psId1s, psId, oldStartIndex, matchCount);
                            i = 0;
                            foreach (var psId2 in intersection)
                            {
                                psId2s[oldStartIndex + i] = psId2;
                                i++;
                            }
                        }
                    }
                );

                return (psId1s, psId2s, startIndex);
            }
        }

        

        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksParallel500usSpanless(
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
                    int pgCount = pgIds.Length;
                    int maxPossibleSubsetMatchCount =
                        permissionGroupOccuranceMap[pgIds[0]].Length;
                    int j = 0;
                    while (++j < pgCount)
                    {
                        var pgOccuranceCount = permissionGroupOccuranceMap[pgIds[j]].Length;
                        if (pgOccuranceCount < maxPossibleSubsetMatchCount)
                        {
                            maxPossibleSubsetMatchCount = pgOccuranceCount;
                        }
                    }
                    totalPermutationCount += maxPossibleSubsetMatchCount;
                }
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];

                int chunkSize = maxPsId + 1;
                var deduplicater = new short[psIdCount * chunkSize * 2];

                int startIndex = 0;
                Parallel.ForEach(
                    psIds,
                    (psId, _, psIndex) =>
                    {
                        var pgIds = permissionSetMap[psId];
                        int pgCount = pgIds.Length;
                        if (pgCount == 1)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            int newStartIndex = Interlocked.Add(ref startIndex, pgOccurancesCount);
                            var oldStartIndex = newStartIndex - pgOccurancesCount;
                            Array.Fill(psId1s, psId, oldStartIndex, pgOccurancesCount);
                            Array.Copy(pgOccurances, 0, psId2s, oldStartIndex, pgOccurancesCount);
                        }
                        else
                        {
                            int chunkStartIndex = (int)psIndex * chunkSize * 2;
                            var prevIntersectionStartIndex = chunkStartIndex;
                            var intersectionStartIndex = chunkStartIndex + chunkSize;
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            int j = -1;
                            while (++j < pgOccurancesCount)
                            {
                                deduplicater[prevIntersectionStartIndex + pgOccurances[j]] = 1;
                            }

                            int i = 0;
                            int matchCount = 0;
                            while (++i < pgCount)
                            {
                                pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                                pgOccurancesCount = pgOccurances.Length;
                                j = -1;
                                while (++j < pgOccurancesCount)
                                {
                                    short psId2 = pgOccurances[j];
                                    if (deduplicater[prevIntersectionStartIndex + psId2] == 1)
                                    {
                                        if (i < pgCount - 1)
                                        {
                                            deduplicater[intersectionStartIndex + psId2] = 1;
                                        }
                                        else
                                        {
                                            deduplicater[intersectionStartIndex + matchCount++] = psId2;
                                        }
                                    }
                                }

                                if (i < pgCount - 1)
                                {
                                    Array.Fill<short>(deduplicater, 0, prevIntersectionStartIndex, chunkSize);
                                    var temp = prevIntersectionStartIndex;
                                    prevIntersectionStartIndex = intersectionStartIndex;
                                    intersectionStartIndex = temp;
                                }
                            }

                            int newStartIndex = Interlocked.Add(ref startIndex, matchCount);
                            var oldStartIndex = newStartIndex - matchCount;
                            Array.Fill(psId1s, psId, oldStartIndex, matchCount);
                            Array.Copy(deduplicater, intersectionStartIndex, psId2s, oldStartIndex, matchCount);
                        }
                    }
                );

                return (psId1s, psId2s, startIndex);
            }
        }

        public static (short[] psId1s, short[] psId2s, int count)
                CalculatePermissionChecksParallel500usSpanny(
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
                    int pgCount = pgIds.Length;
                    int maxPossibleSubsetMatchCount =
                        permissionGroupOccuranceMap[pgIds[0]].Length;
                    int j = 0;
                    while (++j < pgCount)
                    {
                        var pgOccuranceCount = permissionGroupOccuranceMap[pgIds[j]].Length;
                        if (pgOccuranceCount < maxPossibleSubsetMatchCount)
                        {
                            maxPossibleSubsetMatchCount = pgOccuranceCount;
                        }
                    }
                    totalPermutationCount += maxPossibleSubsetMatchCount;
                }
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];

                int chunkSize = maxPsId + 1;
                var deduplicater = new short[psIdCount * chunkSize * 2];

                int startIndex = 0;
                Parallel.ForEach(
                    psIds,
                    (psId, _, psIndex) =>
                    {
                        var pgIds = permissionSetMap[psId];
                        int pgCount = pgIds.Length;
                        if (pgCount == 1)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            int newStartIndex = Interlocked.Add(ref startIndex, pgOccurancesCount);
                            var oldStartIndex = newStartIndex - pgOccurancesCount;
                            Array.Fill(psId1s, psId, oldStartIndex, pgOccurancesCount);
                            Array.Copy(pgOccurances, 0, psId2s, oldStartIndex, pgOccurancesCount);
                        }
                        else
                        {
                            int chunkStartIndex = (int)psIndex * chunkSize * 2;
                            var prevIntersection = new Span<short>(deduplicater, chunkStartIndex, chunkSize);
                            var intersection = new Span<short>(deduplicater, chunkStartIndex + chunkSize, chunkSize);
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            int j = -1;
                            while (++j < pgOccurancesCount)
                            {
                                prevIntersection[pgOccurances[j]] = 1;
                            }

                            int i = 0;
                            while (++i < pgCount)
                            {
                                pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                                pgOccurancesCount = pgOccurances.Length;
                                j = -1;
                                while (++j < pgOccurancesCount)
                                {
                                    short psId2 = pgOccurances[j];
                                    if (prevIntersection[psId2] == 1)
                                    {
                                        intersection[psId2] = 1;
                                    }
                                }

                                prevIntersection.Clear();
                                var temp = prevIntersection;
                                prevIntersection = intersection;
                                intersection = temp;
                            }

                            var matches = prevIntersection
                                .ToArray().Where(x => x is not 0)
                                .ToArray();
                            int matchCount = matches.Length;

                            int newStartIndex = Interlocked.Add(ref startIndex, matchCount);
                            var oldStartIndex = newStartIndex - matchCount;
                            Array.Fill(psId1s, psId, oldStartIndex, matchCount);
                            Array.Copy(matches, 0, psId2s, oldStartIndex, matchCount);
                        }
                    }
                );

                return (psId1s, psId2s, startIndex);
            }
        }

    }
}

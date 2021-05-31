using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System.Globalization;

namespace Permussion
{
    public static class HalfMsPermussioned
    {
        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecks(
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
                            var prevIntersection = new HashSet<short>();
                            var intersection = new HashSet<short>();
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
                                psId2s[oldStartIndex + i++] = psId2;
                            }
                        }
                    }
                );

                return (psId1s, psId2s, startIndex);
            }
        }


        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksFaster(
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
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Length;
                            var intersection = new short[pgOccurancesCount];
                            Array.Copy(pgOccurances, intersection, pgOccurancesCount);

                            int i = 0;
                            int intersectionLength = pgOccurancesCount;
                            int matchCount = intersectionLength;
                            while (++i < pgCount)
                            {
                                pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                                int j = -1;
                                while (++j < intersectionLength)
                                {
                                    var psId2 = intersection[j];
                                    if (psId2 == 0)
                                    {
                                        continue;
                                    }
                                    if (Array.BinarySearch(pgOccurances, psId2) < 0)
                                    {
                                        intersection[j] = 0;
                                        matchCount--;
                                    }
                                }
                            }

                            int newStartIndex = Interlocked.Add(ref startIndex, matchCount);
                            var oldStartIndex = newStartIndex - matchCount;
                            Array.Fill(psId1s, psId, oldStartIndex, matchCount);
                            i = -1;
                            int k = 0;
                            while (++i < intersectionLength &&
                                k < matchCount)
                            {
                                var psId2 = intersection[i];
                                if (psId2 > 0)
                                {
                                    psId2s[oldStartIndex + k++] = psId2;
                                }
                            }
                        }
                    }
                );

                return (psId1s, psId2s, startIndex);
            }
        }

    }
}

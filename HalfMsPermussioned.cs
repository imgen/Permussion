using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short, short[]>;

namespace Permussion;

public static class HalfMsPermussioned
{
    public static (PermissionCheck[], int count)
        CalculatePermissionChecksLinq(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
            int maxPsId)
    {
        var permissionChecks = permissionSetMap.SelectMany(pair =>
        {
            var pgIds = pair.Value;
            return pgIds.Skip(1).Aggregate(
                permissionGroupOccurenceMap[pgIds[0]],
                (intersection, pgId) =>
                    intersection.Intersect(permissionGroupOccurenceMap[pgId]).ToArray()
            ).Select(x => new PermissionCheck(pair.Key, x));
        }).ToArray();

        return (permissionChecks, permissionChecks.Length);
    }

    public static (short[] psId1s, short[] psId2s, int count)
        CalculatePermissionChecks(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
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
                    permissionGroupOccurenceMap[pgIds[0]].Length;
                int j = 0;
                int minPgOccurenceCountIndex = 0;
                while (++j < pgCount)
                {
                    var pgOccurenceCount = permissionGroupOccurenceMap[pgIds[j]].Length;
                    if (pgOccurenceCount < maxPossibleSubsetMatchCount)
                    {
                        maxPossibleSubsetMatchCount = pgOccurenceCount;
                        minPgOccurenceCountIndex = j;
                    }
                }
                totalPermutationCount += maxPossibleSubsetMatchCount;
                if (minPgOccurenceCountIndex > 0)
                {
                    var temp = pgIds[0];
                    pgIds[0] = pgIds[minPgOccurenceCountIndex];
                    pgIds[minPgOccurenceCountIndex] = temp;
                }
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
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        var pgOccurencesCount = pgOccurences.Length;
                        int newStartIndex = Interlocked.Add(ref startIndex, pgOccurencesCount);
                        var oldStartIndex = newStartIndex - pgOccurencesCount;
                        Array.Fill(psId1s, psId, oldStartIndex, pgOccurencesCount);
                        Array.Copy(pgOccurences, 0, psId2s, oldStartIndex, pgOccurencesCount);
                    }
                    else
                    {
                        var prevIntersection = new HashSet<short>();
                        var intersection = new HashSet<short>();
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        var pgOccurencesCount = pgOccurences.Length;
                        int j = -1;
                        while (++j < pgOccurencesCount)
                        {
                            prevIntersection.Add(pgOccurences[j]);
                        }

                        int i = 0;
                        while (++i < pgCount)
                        {
                            pgOccurences = permissionGroupOccurenceMap[pgIds[i]];
                            pgOccurencesCount = pgOccurences.Length;
                            j = -1;
                            while (++j < pgOccurencesCount)
                            {
                                short psId2 = pgOccurences[j];
                                if (prevIntersection.Contains(psId2))
                                {
                                    intersection.Add(psId2);
                                }
                            }

                            if (intersection.Count == 1)
                            {
                                break;
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
            PermissionGroupOccurenceMap permissionGroupOccurenceMap,
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
                    permissionGroupOccurenceMap[pgIds[0]].Length;
                int j = 0;
                int minPgOccurenceCountIndex = 0;
                while (++j < pgCount)
                {
                    var pgOccurenceCount = permissionGroupOccurenceMap[pgIds[j]].Length;
                    if (pgOccurenceCount < maxPossibleSubsetMatchCount)
                    {
                        maxPossibleSubsetMatchCount = pgOccurenceCount;
                        minPgOccurenceCountIndex = j;
                    }
                }
                totalPermutationCount += maxPossibleSubsetMatchCount;
                if (minPgOccurenceCountIndex > 0)
                {
                    var temp = pgIds[0];
                    pgIds[0] = pgIds[minPgOccurenceCountIndex];
                    pgIds[minPgOccurenceCountIndex] = temp;
                }
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
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        var pgOccurencesCount = pgOccurences.Length;
                        int newStartIndex = Interlocked.Add(ref startIndex, pgOccurencesCount);
                        var oldStartIndex = newStartIndex - pgOccurencesCount;
                        Array.Fill(psId1s, psId, oldStartIndex, pgOccurencesCount);
                        Array.Copy(pgOccurences, 0, psId2s, oldStartIndex, pgOccurencesCount);
                    }
                    else
                    {
                        var pgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                        var pgOccurencesCount = pgOccurences.Length;
                        var intersection = new short[pgOccurencesCount];
                        Array.Copy(pgOccurences, intersection, pgOccurencesCount);

                        int i = 0;
                        int matchCount = pgOccurencesCount;
                        while (matchCount > 1 && ++i < pgCount)
                        {
                            pgOccurences = permissionGroupOccurenceMap[pgIds[i]];
                            int j = 0;
                            while (j < matchCount)
                            {
                                var psId2 = intersection[j];
                                if (Utils.BinarySearch(pgOccurences, psId2) < 0)
                                {
                                    intersection[j] = intersection[matchCount - 1];
                                    matchCount--;
                                }
                                else
                                {
                                    j++;
                                }
                            }
                        }

                        int newStartIndex = Interlocked.Add(ref startIndex, matchCount);
                        var oldStartIndex = newStartIndex - matchCount;
                        Array.Fill(psId1s, psId, oldStartIndex, matchCount);
                        Array.Copy(intersection, 0, psId2s, oldStartIndex, matchCount);
                    }
                }
            );

            return (psId1s, psId2s, startIndex);
        }
    }
}
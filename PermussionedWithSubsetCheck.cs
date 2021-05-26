using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System;

namespace Permussion
{
    public class PermussionedWithSubsetCheck
    {
        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksParallel(
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
                    int maxPossibleSubsetMatchCount = pgIds
                        .Select(x => permissionGroupOccuranceMap[x].Length)
                        .Min();
                    totalPermutationCount += maxPossibleSubsetMatchCount;
                }
                int index = -1;
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];

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
                            int j = -1;
                            while (++j < pgOccurancesCount)
                            {
                                short psId2 = pgOccurances[j];
                                var index2 = Interlocked.Increment(ref index);
                                psId1s[index2] = psId;
                                psId2s[index2] = psId2;
                            }
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

                                prevIntersection.Clear();
                                (prevIntersection, intersection) = (intersection, prevIntersection);
                            }

                            foreach (var psId2 in prevIntersection)
                            {
                                var index2 = Interlocked.Increment(ref index);
                                psId1s[index2] = psId;
                                psId2s[index2] = psId2;
                            }
                        }
                    }
                );

                return (psId1s, psId2s, index + 1);
            }
        }

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
                    int maxPossibleSubsetMatchCount = pgIds
                        .Select(x => permissionGroupOccuranceMap[x].Length)
                        .Min();
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

                                prevIntersection.Clear();
                                (prevIntersection, intersection) = (intersection, prevIntersection);
                            }

                            var matches = prevIntersection.ToArray();
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

        public static (short[] psId1s, short[] psId2s, int count)
            CalculatePermissionChecksParallel500usV2(
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
                    int maxPossibleSubsetMatchCount = pgIds
                        .Select(x => permissionGroupOccuranceMap[x].Length)
                        .Min();
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
                int psIdCount = psIds.Length;
                int i = -1;
                while (++i < psIdCount)
                {
                    var pgIds = permissionSetMap[psIds[i]];
                    int maxPossibleSubsetMatchCount = pgIds
                        .Select(x => permissionGroupOccuranceMap[x].Length)
                        .Min();
                    totalPermutationCount += maxPossibleSubsetMatchCount;
                }
                int index = 0;
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
                var (prevIntersection, intersection) =
                            (new HashSet<short>(), new HashSet<short>());

                foreach (var psId in psIds)
                {
                    var pgIds = permissionSetMap[psId];
                    int pgCount = pgIds.Length;
                    if (pgCount == 1)
                    {
                        var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        var pgOccurancesCount = pgOccurances.Length;
                        Array.Fill(psId1s, psId, index, pgOccurancesCount);
                        Array.Copy(pgOccurances, 0, psId2s, index, pgOccurancesCount);
                        index += pgOccurancesCount;
                    }
                    else
                    {

                        var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        var pgOccurancesCount = pgOccurances.Length;
                        int j = -1;
                        while (++j < pgOccurancesCount)
                        {
                            prevIntersection.Add(pgOccurances[j]);
                        }

                        i = 0;
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

                            prevIntersection.Clear();
                            (prevIntersection, intersection) = (intersection, prevIntersection);
                        }

                        var matches = prevIntersection.ToArray();
                        int matchCount = matches.Length;
                        Array.Fill(psId1s, psId, index, matchCount);
                        Array.Copy(matches, 0, psId2s, index, matchCount);
                        index += matchCount;

                        prevIntersection.Clear();
                    }
                }

                return (psId1s, psId2s, index);
            }
        }
    }
}

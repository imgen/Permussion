using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short,
    System.Collections.Generic.List<short>>;

namespace Permussion
{
    public class PermissionedWithSubsetCheck
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
                        .Select(x => permissionGroupOccuranceMap[x].Count)
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
                        int pgCount = pgIds.Count;
                        if (pgCount == 1)
                        {
                            var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                            var pgOccurancesCount = pgOccurances.Count;
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
                            var pgOccurancesCount = pgOccurances.Count;
                            int j = -1;
                            while (++j < pgOccurancesCount)
                            {
                                prevIntersection.Add(pgOccurances[j]);
                            }

                            int i = 0;
                            while (++i < pgCount)
                            {
                                pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                                pgOccurancesCount = pgOccurances.Count;
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
                        .Select(x => permissionGroupOccuranceMap[x].Count)
                        .Min();
                    totalPermutationCount += maxPossibleSubsetMatchCount;
                }
                int index = -1;
                var psId1s = new short[totalPermutationCount];
                var psId2s = new short[totalPermutationCount];
                var (prevIntersection, intersection) =
                            (new HashSet<short>(), new HashSet<short>());

                foreach (var psId in psIds)
                {
                    var pgIds = permissionSetMap[psId];
                    int pgCount = pgIds.Count;
                    if (pgCount == 1)
                    {
                        var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        var pgOccurancesCount = pgOccurances.Count;
                        int j = -1;
                        while (++j < pgOccurancesCount)
                        {
                            short psId2 = pgOccurances[j];
                            psId1s[++index] = psId;
                            psId2s[index] = psId2;
                        }
                    }
                    else
                    {

                        var pgOccurances = permissionGroupOccuranceMap[pgIds[0]];
                        var pgOccurancesCount = pgOccurances.Count;
                        int j = -1;
                        while (++j < pgOccurancesCount)
                        {
                            prevIntersection.Add(pgOccurances[j]);
                        }

                        i = 0;
                        while (++i < pgCount)
                        {
                            pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                            pgOccurancesCount = pgOccurances.Count;
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
                            psId1s[++index] = psId;
                            psId2s[index] = psId2;
                        }

                        prevIntersection.Clear();
                    }
                }

                return (psId1s, psId2s, index + 1);
            }
        }
    }
}

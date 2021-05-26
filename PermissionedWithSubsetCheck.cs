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
                            int j = pgOccurances.Count;
                            while (--j >= 0)
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
                            int j = pgOccurances.Count;
                            while (--j >= 0)
                            {
                                short psId2 = pgOccurances[j];
                                prevIntersection.Add(psId2);
                            }

                            int i = 0;
                            while (++i < pgCount)
                            {
                                pgOccurances = permissionGroupOccuranceMap[pgIds[i]];
                                j = pgOccurances.Count;
                                while (--j >= 0)
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

                            foreach (var psId2 in intersection)
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
    }
}

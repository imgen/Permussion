using System;
using System.Linq;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System.Threading;

namespace Permussion
{
    class OneMsPermussioned
    {
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
    }
}

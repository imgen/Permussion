using System;
using System.Linq;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System.Threading;
using System.Collections.Generic;

namespace Permussion;

public static class HalfMsPermissioned
{
    public static (short[] psId1s, short[] psId2s, int count)
        CalculatePermissionChecksWithLinkedList(
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

                    var firstPgOccurences = permissionGroupOccurenceMap[pgIds[0]];
                    int firstPgOccurenceCount = firstPgOccurences.Length;
                    var buckets = new LinkedList<short>[firstPgOccurenceCount];
                    var j = -1;
                    while (++j < firstPgOccurenceCount)
                    {
                        buckets[j] = new LinkedList<short>();
                        buckets[j].AddFirst(firstPgOccurences[j]);
                    }
                    permissionChecksCount += firstPgOccurences.Length;

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

}
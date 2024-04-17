using System;
using System.Linq;
using System.Threading.Tasks;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, short[]>;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short, short[]>;
using System.Threading;

namespace Permussion;

static class OneMsPermussioned
{

    public static PermissionCheck[]
        CalculatePermissionChecksUnion(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.SelectMany(pair =>
        {
            var pgIds = pair.Value;
            return pgIds.Skip(1).Aggregate(
                permissionGroupOccurenceMap[pgIds[0]],
                (union, pgId) =>
                    union.Union(permissionGroupOccurenceMap[pgId]).ToArray()
            ).Select(x => new PermissionCheck(pair.Key, x));
        }).ToArray();
}
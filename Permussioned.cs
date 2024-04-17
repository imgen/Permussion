using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short,
    System.Collections.Generic.List<short>>;
using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;

namespace Permussion;

public static class Permussioned
{
    public static PermissionCheck[] CalculatePermissionChecks(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        return permissionSetMap.SelectMany(
            pair => pair.Value
                .SelectMany(x => permissionGroupOccurenceMap[x])
                .Distinct()
                .Select(x => new PermissionCheck(pair.Key, x))
        ).ToArray();
    }
}

public record PermissionSetGroup(
    short PermissionSetId,
    short PermissionGroupId,
    bool IsUserPermissionSet
);

public record PermissionCheck(short PermissionSetId1, short PermissionSetId2);
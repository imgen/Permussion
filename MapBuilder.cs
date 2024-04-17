using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using PermissionGroupOccurrenceMap = System.Collections.Generic.Dictionary<short,
    System.Collections.Generic.List<short>>;

namespace Permussion;

public static class MapBuilder
{
    public static ValueTask<PermissionSetGroup[]> LoadPermissionSetGroups() =>
        JsonSerializer.DeserializeAsync<PermissionSetGroup[]>(
            File.OpenRead("PermissionSetGroups.json")
        );

    public static (
        PermissionSetMap UserPermissionSetMap,
        PermissionGroupOccurrenceMap PermissionGroupOccurenceMap)
        BuildPermissionMaps(PermissionSetGroup[] permissionSetGroups)
    {
        PermissionSetMap userPermissionSetMap = new();
        PermissionGroupOccurrenceMap permissionGroupOccurenceMap = new();
        int i = -1;
        int psgCount = permissionSetGroups.Length;
        while (++i < psgCount)
        {
            var (psId, pgId, isUserPermissionSet) = permissionSetGroups[i];
            if (userPermissionSetMap.TryGetValue(psId, out var pgIds))
            {
                pgIds.Add(pgId);
            }
            else if (isUserPermissionSet)
            {
                userPermissionSetMap[psId] = [pgId];
            }

            if (permissionGroupOccurenceMap.TryGetValue(pgId, out var psIds))
            {
                psIds.Add(psId);
            }
            else
            {
                permissionGroupOccurenceMap[pgId] = [psId];
            }
        }
            
        return (
            userPermissionSetMap,
            permissionGroupOccurenceMap
        );
    }
}

public record PermissionSetGroup(
    short PermissionSetId,
    short PermissionGroupId,
    bool IsUserPermissionSet
);
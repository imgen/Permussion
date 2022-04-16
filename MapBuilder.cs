using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using PermissionGroupOccurrenceMap = System.Collections.Generic.Dictionary<short,
    System.Collections.Generic.List<short>>;
using System.Collections.Generic;
using System.Linq;

namespace Permussion;

public static class MapBuilder
{
    public static ValueTask<PermissionSetGroup[]> LoadPermissionSetGroups()
    {
        return JsonSerializer.DeserializeAsync<PermissionSetGroup[]>(
            File.OpenRead("PermissionSetGroups.json")
        );
    }

    public static (
        PermissionSetMap UserPermissionSetMap,
        PermissionGroupOccurrenceMap PermissionGroupOccurenceMap,
        short MaxPermissionSetId)
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
                userPermissionSetMap[psId] = new() { pgId };
            }

            if (permissionGroupOccurenceMap.TryGetValue(pgId, out var psIds))
            {
                psIds.Add(psId);
            }
            else
            {
                permissionGroupOccurenceMap[pgId] = new() { psId };
            }
        }
            
        return (
            userPermissionSetMap,
            permissionGroupOccurenceMap,
            permissionSetGroups[^1].PermissionSetId
        );
    }

    static bool IsSorted<T>(List<T> list) => 
        list.OrderBy(x => x).SequenceEqual(list);
}
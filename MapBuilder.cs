using System.Threading.Tasks;
using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using PermissionGroupOccuranceMap = System.Collections.Generic.Dictionary<short,
    System.Collections.Generic.List<short>>;
using System.Text.Json;
using System.IO;

namespace Permussion
{
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
            PermissionGroupOccuranceMap PermissionGroupOccuranceMap,
            short MaxPermissionSetId)
            BuildPermissionMaps(PermissionSetGroup[] permissionSetGroups)
        {
            PermissionSetMap userPermissionSetMap = new();
            PermissionGroupOccuranceMap permissionGroupOccuranceMap = new();
            int i = -1;
            int psgCount = permissionSetGroups.Length;
            while (++i < psgCount)
            {
                var (psId, pgId, isUserPermissionSet) = permissionSetGroups[i];
                if (userPermissionSetMap.TryGetValue(psId, out var ps))
                {
                    ps.Add(pgId);
                }
                else if (isUserPermissionSet)
                {
                    userPermissionSetMap[psId] = new() { pgId };
                }

                if (permissionGroupOccuranceMap.TryGetValue(pgId, out var psIds))
                {
                    if (psIds.Contains(psId) is false)
                    {
                        psIds.Add(psId);
                    }
                }
                else
                {
                    permissionGroupOccuranceMap[pgId] = new() { psId };
                }
            }

            return (
                userPermissionSetMap,
                permissionGroupOccuranceMap,
                permissionSetGroups[permissionSetGroups.Length - 1].PermissionSetId
            );
        }
    }
}

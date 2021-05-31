using static Permussion.TinyProfiler;
using static Permussion.MapBuilder;
using System;
using Permussion;
using System.Linq;

var permissionSetGroups = await ProfileAsync(
    "Load permission set groups",
    LoadPermissionSetGroups
);
var (userPermissionSetMap, permissionGroupOccuranceMap, maxPsId) = Profile(
    "Build permission maps",
    () => BuildPermissionMaps(permissionSetGroups)
);

var userPermissionSetMapWithArray = userPermissionSetMap
    .ToDictionary(x => x.Key, x => x.Value.ToArray());
var permissionGroupOccuranceMapWithArray = permissionGroupOccuranceMap
    .ToDictionary(x => x.Key, x => x.Value.ToArray());
var bestTime = TimeSpan.FromDays(1);
int count = 0;
for (int i = 0; i < 100; i++)
{
    (_, _, count) = Profile(
        "Calculate user permission checks",
        () => OneMsPermussioned.CalculatePermissionChecksFaster(
            userPermissionSetMapWithArray,
            permissionGroupOccuranceMapWithArray,
            maxPsId
        ),
        (timeTaken, message) =>
        {
            bestTime = timeTaken < bestTime ? timeTaken : bestTime;
            Console.WriteLine(message);
        }
    );
}
Console.WriteLine($"Generated {count} user permission checks");
Console.WriteLine($"At least it will take {TinyProfiler.FormatTimeSpan(bestTime)} to calculate the user permission checks");
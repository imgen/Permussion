using static Permussion.TinyProfiler;
using static Permussion.MapBuilder;
using System;
using Permussion;
using System.Linq;

var permissionSetGroups = await ProfileAsync(
    "Load permission set groups",
    LoadPermissionSetGroups
);
var (userPermissionSetMap, permissionGroupOccurenceMap, maxPsId) = Profile(
    "Build permission maps",
    () => BuildPermissionMaps(permissionSetGroups)
);

var userPermissionSetMapWithArray = userPermissionSetMap
    .ToDictionary(x => x.Key, x => x.Value.ToArray());
var permissionGroupOccurenceMapWithArray = permissionGroupOccurenceMap
    .ToDictionary(x => x.Key, x => x.Value.ToArray());
var bestTime = TimeSpan.FromDays(1);
int count = 0;
short[] psId1s = null, psId2s;
for (int i = 0; i < 100; i++)
{
    (psId1s, psId2s, count) = Profile(
        "Calculate user permission checks",
        () => HalfMsPermussioned.CalculatePermissionChecksFaster(
            userPermissionSetMapWithArray,
            permissionGroupOccurenceMapWithArray,
            maxPsId
        ),
        (timeTaken, message) =>
        {
            bestTime = timeTaken < bestTime ? timeTaken : bestTime;
            Console.WriteLine(message);
        }
    );
}

bool doCountingOfMatches = true;
if (doCountingOfMatches)
{
    var counts = psId1s.Take(count).GroupBy(x => x)
        .Select(x => x.Count()).ToArray();
    var countOfMostMatches = counts.Max();
    var countOfLeastMatches = counts.Min();
    var averageMatchCount = counts.Average();
    Console.WriteLine($"The max count of matches is {countOfMostMatches}");
    Console.WriteLine($"The min count of matches is {countOfLeastMatches}");
    Console.WriteLine($"The average count of matches is {averageMatchCount}");
}
Console.WriteLine($"Generated {count} user permission checks");
Console.WriteLine($"At least it will take {TinyProfiler.FormatTimeSpan(bestTime)} to calculate the user permission checks");
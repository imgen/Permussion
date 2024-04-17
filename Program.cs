using static Permussion.TinyProfiler;
using static Permussion.MapBuilder;
using System;
using Permussion;
using System.Linq;

var permissionSetGroups = await ProfileAsync(
    "Load permission set groups",
    LoadPermissionSetGroups
);
var (userPermissionSetMap, permissionGroupOccurenceMap) = Profile(
    "Build permission maps",
    () => BuildPermissionMaps(permissionSetGroups)
);

var bestTime = TimeSpan.FromDays(1);
PermissionCheck[] permissionChecks = null;
for (var i = 0; i < 10_000; i++)
    permissionChecks = Profile(
        "Calculate user permission checks",
        () => Permussioned.CalculatePermissionChecksUnion(
            userPermissionSetMap,
            permissionGroupOccurenceMap
        ),
        (timeTaken, message) =>
        {
            bestTime = timeTaken < bestTime ? timeTaken : bestTime;
            Console.WriteLine(message);
        }
    );

var counts = permissionChecks!.GroupBy(x => x.PermissionSetId1)
        .Select(x => x.Count()).ToArray();
var countOfMostMatches = counts.Max();
var countOfLeastMatches = counts.Min();
var averageMatchCount = counts.Average();
Console.WriteLine($"The max count of matches is {countOfMostMatches}");
Console.WriteLine($"The min count of matches is {countOfLeastMatches}");
Console.WriteLine($"The average count of matches is {averageMatchCount}");

Console.WriteLine($"Generated {permissionChecks.Length} user permission checks");
Console.WriteLine($"At least it will take {TinyProfiler.FormatTimeSpan(bestTime)} to calculate the user permission checks");
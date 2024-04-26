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
(short[] PermissionSetIds1, short[] PermissionIds2)? permissionChecks = null;
for (var i = 0; i < 1000; i++)
    permissionChecks = Profile(
        "Calculate user permission checks",
        () => Permussioned.CalculatePermissionChecksDistinctLessParallelForTwoArrays(
            userPermissionSetMap,
            permissionGroupOccurenceMap
        ),
        (timeTaken, message) => bestTime = timeTaken < bestTime ? timeTaken : bestTime);

Console.WriteLine($"Generated {permissionChecks!.Value.PermissionSetIds1.Length} user permission checks");
Console.WriteLine($"At least it will take {TinyProfiler.FormatTimeSpan(bestTime)} to calculate the user permission checks");

var counts = permissionChecks.Value.PermissionSetIds1.GroupBy(x => x)
        .Select(x => x.Count()).ToArray();
var countOfMostMatches = counts.Max();
var countOfLeastMatches = counts.Min();
var averageMatchCount = counts.Average();
Console.WriteLine($"The max count of matches is {countOfMostMatches}");
Console.WriteLine($"The min count of matches is {countOfLeastMatches}");
Console.WriteLine($"The average count of matches is {averageMatchCount}");
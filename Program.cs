using static Permussion.TinyProfiler;
using static Permussion.MapBuilder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
IList<PermissionCheck>? permissionChecks = null;
for (var i = 0; i < 1000; i++)
    permissionChecks = Profile(
        "Calculate user permission checks",
        () => Permussioned.CalculatePermissionChecksDistinctLessParallelFor(
            userPermissionSetMap,
            permissionGroupOccurenceMap
        ),
        (timeTaken, message) =>
        {
            bestTime = timeTaken < bestTime ? timeTaken : bestTime;
            Console.WriteLine(message);
        }
    );

Console.WriteLine($"Generated {permissionChecks!.Count} user permission checks");
Console.WriteLine($"At least it will take {TinyProfiler.FormatTimeSpan(bestTime)} to calculate the user permission checks");

var counts = permissionChecks.GroupBy(x => x.PermissionSetId1)
        .Select(x => x.Count()).ToArray();
var countOfMostMatches = counts.Max();
var countOfLeastMatches = counts.Min();
var averageMatchCount = counts.Average();
Console.WriteLine($"The max count of matches is {countOfMostMatches}");
Console.WriteLine($"The min count of matches is {countOfLeastMatches}");
Console.WriteLine($"The average count of matches is {averageMatchCount}");
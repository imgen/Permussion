﻿using static Permussion.TinyProfiler;
using static Permussion.MapBuilder;
using System;
using Permussion;

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
short[] psId1s = null;
short[] psId2s = null;
int count = 0;
for (int i = 0; i < 100; i++)
{
    (psId1s, psId2s, count) = Profile(
        "Calculate user permission checks",
        () => OneMsPermussioned.CalculatePermissionChecksFinalFinishUpper(
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
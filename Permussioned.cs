﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using Pairs = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<short, System.Collections.Generic.List<short>>>;

namespace Permussion;

public static class Permussioned
{
    private const int ChunkCount = 64;

    private delegate IEnumerable<PermissionCheck> Generator(
        Pairs pairs,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap);

    private delegate IEnumerable<(short[] PermissionSetIds1, IList<short> PermissionSetIds2)> GeneratorWithTwoArrays(
        Pairs pairs,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap);

    private static IEnumerable<PermissionCheck> GenerateWithDistinct(
        this Pairs pairs,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap
    ) =>
        pairs.SelectMany(
            pair => pair.Value
                .SelectMany(x => permissionGroupOccurenceMap[x])
                .Distinct()
                .Select(x => new PermissionCheck(pair.Key, x))
        );

    private static IEnumerable<(short[] PermissionSetIds1, IList<short> PermissionSetIds2)> GenerateWithDistinctTwoArrays(
        this Pairs pairs,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap
    ) =>
        pairs.Select(
            pair =>
            {
                var permissionSetIdList = pair.Value;
                var capacity = permissionGroupOccurenceMap[permissionSetIdList[0]].Count;
                var maxOccurenceCount = capacity;
                var maxOccurenceCountIndex = 0;
                for (var i = 1; i < permissionSetIdList.Count; i++)
                {
                    var count = permissionGroupOccurenceMap[permissionSetIdList[i]].Count;
                    if (count > maxOccurenceCount)
                    {
                        maxOccurenceCount = count;
                        maxOccurenceCountIndex = i;
                    }

                    capacity += count;
                }
                var hash = new ShortHashSet(capacity);
                hash.AddAll(permissionGroupOccurenceMap[permissionSetIdList[maxOccurenceCountIndex]]);
                for (var i = 0; i < permissionSetIdList.Count; i++)
                {
                    if (i == maxOccurenceCountIndex)
                        continue;
                    var occurences = permissionGroupOccurenceMap[permissionSetIdList[i]];
                    for (var j = 0; j < occurences.Count; j++) 
                        hash.Add(occurences[j]);
                }

                var allOccurences = hash.ToArray();
                var permissionSetIds = Enumerable.Repeat(pair.Key, allOccurences.Length).ToArray();
                return (permissionSetIds, (IList<short>)allOccurences);
            }
        );

    private static IEnumerable<(short[] PermissionSetIds1, IList<short> PermissionSetIds2)> GenerateWithNoDistinctTwoArrays(
        this Pairs pairs,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap
    ) =>
        pairs.Select(
            pair =>
            {
                var occurences = permissionGroupOccurenceMap[pair.Value[0]];
                var permissionSetIds = Enumerable.Repeat(pair.Key, occurences.Count).ToArray();
                return (permissionSetIds, (IList<short>)occurences);
            }
        );

    private static IEnumerable<PermissionCheck> GenerateWithNoDistinct(
        this Pairs pairs,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap
    ) =>
        pairs.SelectMany(
            pair => permissionGroupOccurenceMap[pair.Value[0]]
                .Select(x => new PermissionCheck(pair.Key, x))
        );

    public static PermissionCheck[] CalculatePermissionChecksDistinct(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.GenerateWithDistinct(permissionGroupOccurenceMap).ToArray();

    public static PermissionCheck[] CalculatePermissionChecksDistinctLess(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        var (multipleItems, singleItems, _, _) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        return singleItems.GenerateWithNoDistinct(permissionGroupOccurenceMap)
            .Concat(multipleItems.GenerateWithDistinct(permissionGroupOccurenceMap)).ToArray();
    }

    public static PermissionCheck[] CalculatePermissionChecksDistinctParallel(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.Chunk(permissionSetMap.Count / ChunkCount).AsParallel()
            .SelectMany(
                permissionSetMapChunk =>
                    permissionSetMapChunk.GenerateWithDistinct(permissionGroupOccurenceMap)
            ).ToArray();

    public static PermissionCheck[] CalculatePermissionChecksDistinctLessParallel(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        var (multipleItems, singleItems, multipleItemsCount, singleItemsCount) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        
        return AddGenerator(singleItems, singleItemsCount, GenerateWithNoDistinct)
            .Concat(AddGenerator(multipleItems, multipleItemsCount, GenerateWithDistinct))
            .AsParallel()
            .SelectMany(chunkWithGenerator => 
                chunkWithGenerator.generator(chunkWithGenerator.pairs, permissionGroupOccurenceMap))
            .ToArray();

        IEnumerable<(Pairs pairs, Generator generator)> AddGenerator(Pairs pairs, int count, Generator generator) =>
            pairs.Chunk(count / ChunkCount)
                .Select<Pairs, (Pairs pairs, Generator generator)>(chunk => (chunk, generator));
    }

    public static PermissionCheck[] CalculatePermissionChecksDistinctParallelFor(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        var permissionCheckArrays = permissionSetMap.Chunk(permissionSetMap.Count / ChunkCount)
            .AsParallel()
            .Select(
                pairs => pairs.GenerateWithDistinct(permissionGroupOccurenceMap).ToArray()
            ).ToArray();

        var allPermissionChecks = new PermissionCheck[permissionCheckArrays.Sum(x => x.Length)];
        var copyIndex = 0;
        var permissionCheckArraysCount = permissionCheckArrays.Length;
        var copyIndices = new int[permissionCheckArraysCount];
        for (var i = 0; i < permissionCheckArraysCount; i++)
        {
            copyIndices[i] = copyIndex;
            copyIndex += permissionCheckArrays[i].Length;
        }

        Parallel.ForEach(permissionCheckArrays,
            (permissionChecks, _, index) => 
                Array.Copy(permissionChecks, 0, allPermissionChecks, copyIndices[index], permissionChecks.Length));

        return allPermissionChecks;
    }

    public static PermissionCheck[] CalculatePermissionChecksDistinctLessParallelFor(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        using var profileSession = new EmptyProfileSession("DistinctLessParallelFor");
        var (multipleItems, singleItems, multipleItemsCount, singleItemsCount) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        profileSession.Profile("Categorizing by count");
        var permissionCheckArrays = AddGenerator(singleItems, singleItemsCount, GenerateWithNoDistinct)
            .Concat(AddGenerator(multipleItems, multipleItemsCount, GenerateWithDistinct))
            .AsParallel()
            .Select(chunkWithGenerator =>
                chunkWithGenerator.generator(chunkWithGenerator.pairs, permissionGroupOccurenceMap).ToArray())
            .ToArray();

        profileSession.Profile("Generating array of arrays");

        var allPermissionChecks = new PermissionCheck[permissionCheckArrays.Sum(x => x.Length)];
        var copyIndex = 0;
        var permissionCheckArraysCount = permissionCheckArrays.Length;
        var copyIndices = new int[permissionCheckArraysCount];
        for (var i = 0; i < permissionCheckArraysCount; i++)
        {
            copyIndices[i] = copyIndex;
            copyIndex += permissionCheckArrays[i].Length;
        }

        profileSession.Profile("Generating copy indices");

        Parallel.ForEach(permissionCheckArrays,
            (permissionChecks, _, index) =>
                permissionChecks.CopyTo(allPermissionChecks, copyIndices[index]));

        profileSession.Profile("Flattening arrays");

        profileSession.Print();

        return allPermissionChecks;

        IEnumerable<(Pairs pairs, Generator generator)> AddGenerator(Pairs pairs, int count, Generator generator) =>
            pairs.Chunk(count / ChunkCount)
                .Select<Pairs, (Pairs pairs, Generator generator)>(chunk => (chunk, generator));
    }

    public static (short[] PermissionSetIds1, short[] PermissionIds2) CalculatePermissionChecksDistinctLessParallelForTwoArrays(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        //using var profileSession = new ProfileSession("DistinctLessParallelForTwoArrays");
        var (multipleItems, singleItems, multipleItemsCount, singleItemsCount) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        //profileSession.Profile("Categorizing by count");
        var permissionCheckArrays = AddGenerator(singleItems, singleItemsCount, GenerateWithNoDistinctTwoArrays)
            .Concat(AddGenerator(multipleItems, multipleItemsCount, GenerateWithDistinctTwoArrays))
            .AsParallel()
            .SelectMany(chunkWithGenerator =>
                chunkWithGenerator.generator(chunkWithGenerator.pairs, permissionGroupOccurenceMap).ToArray())
            .ToArray();

        //profileSession.Profile("Generating array of arrays");

        var permissionCheckCount = permissionCheckArrays.Sum(x => x.PermissionSetIds1.Length);
        var permissionSetIds1 = new short[permissionCheckCount];
        var permissionSetIds2 = new short[permissionCheckCount];
        var copyIndex = 0;
        var permissionCheckArraysCount = permissionCheckArrays.Length;
        var copyIndices = new int[permissionCheckArraysCount];
        for (var i = 0; i < permissionCheckArraysCount; i++)
        {
            copyIndices[i] = copyIndex;
            copyIndex += permissionCheckArrays[i].PermissionSetIds1.Length;
        }

        //profileSession.Profile("Generating copy indices");

        Parallel.ForEach(permissionCheckArrays,
            (pair, _, index) =>
            {
                pair.PermissionSetIds1.CopyTo(permissionSetIds1, copyIndices[index]);
                pair.PermissionSetIds2.CopyTo(permissionSetIds2, copyIndices[index]);
            });

        //profileSession.Profile("Flattening arrays");

        //profileSession.Print();

        return (permissionSetIds1, permissionSetIds2);

        IEnumerable<(Pairs pairs, GeneratorWithTwoArrays generator)> AddGenerator(Pairs pairs, int count, GeneratorWithTwoArrays generator) =>
            pairs.Chunk(count / ChunkCount)
                .Select<Pairs, (Pairs pairs, GeneratorWithTwoArrays generator)>(chunk => (chunk, generator));
    }

    public static PermissionCheck[] CalculatePermissionChecksDistinctLessParallelFor2(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        var (multipleItems, singleItems, multipleItemsCount, singleItemsCount) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        var permissionCheckList = singleItems
            .Chunk(singleItemsCount / ChunkCount)
            .AsParallel()
            .SelectMany(pairs => pairs.GenerateWithNoDistinct(permissionGroupOccurenceMap))
            .ToList();
        var permissionCheckArrays = multipleItems
            .Chunk(multipleItemsCount / ChunkCount)
            .AsParallel()
            .Select(pairs =>
                pairs.GenerateWithDistinct(permissionGroupOccurenceMap).ToArray())
            .ToArray();
        permissionCheckList.Capacity = permissionCheckList.Count + permissionCheckArrays.Sum(x => x.Length);
        var allPermissionChecks = permissionCheckList.GetInternalArray();
        var copyIndex = permissionCheckList.Count;
        var permissionCheckArraysCount = permissionCheckArrays.Length;
        var copyIndices = new int[permissionCheckArraysCount];
        for (var i = 0; i < permissionCheckArraysCount; i++)
        {
            copyIndices[i] = copyIndex;
            copyIndex += permissionCheckArrays[i].Length;
        }

        Parallel.ForEach(permissionCheckArrays,
            (permissionChecks, _, index) =>
                permissionChecks.CopyTo(allPermissionChecks, copyIndices[index]));

        return allPermissionChecks;
    }


    public static PermissionCheck[]
        CalculatePermissionChecksUnion(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.SelectMany(pair =>
        {
            var pgIds = pair.Value;
            var union = permissionGroupOccurenceMap[pgIds[0]];
            for (var i = 1; i < pgIds.Count; i++) 
                union = union.Union(permissionGroupOccurenceMap[pgIds[i]]).ToList();
            var permissionChecks = new PermissionCheck[union.Count];
            for (var i = 0; i < union.Count; i++)
            {
                permissionChecks[i] = new PermissionCheck(pair.Key, union[i]);
            }

            return permissionChecks;
        }).ToArray();

    public static PermissionCheck[]
        CalculatePermissionChecksUnionParallel(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.Chunk(permissionSetMap.Count / ChunkCount).AsParallel()
            .SelectMany(
                permissionSetMapChunk =>
                    permissionSetMapChunk.SelectMany(pair =>
                    {
                        var pgIds = pair.Value;
                        var union = permissionGroupOccurenceMap[pgIds[0]];
                        for (var i = 1; i < pgIds.Count; i++)
                            union = union.Union(permissionGroupOccurenceMap[pgIds[i]]).ToList();
                        var permissionChecks = new PermissionCheck[union.Count];
                        for (var i = 0; i < union.Count; i++)
                        {
                            permissionChecks[i] = new PermissionCheck(pair.Key, union[i]);
                        }

                        return permissionChecks;
                    })
            ).ToArray();

    /// <summary>
    /// This is different logic (without using PermissionGroup idea). The idea becomes from a Union to Intersection
    /// </summary>
    public static PermissionCheck[]
        CalculatePermissionChecksIntersection(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.SelectMany(pair =>
        {
            var pgIds = pair.Value;
            var intersection = permissionGroupOccurenceMap[pgIds[0]];
            for (var i = 1; i < pgIds.Count; i++)
                intersection = intersection.Intersect(permissionGroupOccurenceMap[pgIds[i]]).ToList();
            var permissionChecks = new PermissionCheck[intersection.Count];
            for (var i = 0; i < intersection.Count; i++)
            {
                permissionChecks[i] = new PermissionCheck(pair.Key, intersection[i]);
            }

            return permissionChecks;
        }).ToArray();

    /// <summary>
    /// This is different logic (without using PermissionGroup idea). The idea becomes from a Union to Intersection
    /// </summary>
    public static PermissionCheck[]
        CalculatePermissionChecksIntersectionParallel(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.Chunk(permissionSetMap.Count / ChunkCount).AsParallel()
            .SelectMany(
                permissionSetMapChunk =>
                    permissionSetMapChunk.SelectMany(pair =>
                    {
                        var pgIds = pair.Value;
                        var intersection = permissionGroupOccurenceMap[pgIds[0]];
                        for (var i = 1; i < pgIds.Count; i++)
                            intersection = intersection.Intersect(permissionGroupOccurenceMap[pgIds[i]]).ToList();
                        var permissionChecks = new PermissionCheck[intersection.Count];
                        for (var i = 0; i < intersection.Count; i++)
                        {
                            permissionChecks[i] = new PermissionCheck(pair.Key, intersection[i]);
                        }

                        return permissionChecks;
                    })
            ).ToArray();
}

public readonly struct PermissionCheck(short permissionSetId1, short permissionSetId2)
{
    public readonly short PermissionSetId1 = permissionSetId1;
    public readonly short PermissionSetId2 = permissionSetId2;
}
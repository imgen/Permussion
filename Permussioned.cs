using System;
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
        var permissionSetCount = permissionSetMap.Count;
        var permissionCheckArrays = permissionSetMap.AsParallel()
            .Select(
                pair => pair.Value
                .SelectMany(x => permissionGroupOccurenceMap[x])
                .Distinct()
                .Select(x => new PermissionCheck(pair.Key, x))
                .ToArray()
            ).ToArray();

        var totalSize = permissionCheckArrays.Sum(x => x.Length);

        var allPermissionChecks = new PermissionCheck[totalSize];
        var copyIndex = 0;
        var copyIndices = new int[permissionSetCount];
        for (var i = 0; i < permissionSetCount; i++)
        {
            copyIndices[i] = copyIndex;
            copyIndex += permissionCheckArrays[i].Length;
        }
        Parallel.ForEach(permissionCheckArrays,
            (permissionChecks, _, index) => 
                Array.Copy(permissionChecks, 0, allPermissionChecks, copyIndices[index], permissionChecks.Length));

        return allPermissionChecks;
    }

    public static PermissionCheck[]
        CalculatePermissionChecksUnion(
            PermissionSetMap permissionSetMap,
            PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.SelectMany(pair =>
        {
            var pgIds = pair.Value;
            return pgIds.Skip(1).Aggregate(
                permissionGroupOccurenceMap[pgIds[0]],
                (union, pgId) =>
                    union.Union(permissionGroupOccurenceMap[pgId]).ToList()
            ).Select(x => new PermissionCheck(pair.Key, x));
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
                        return pgIds.Skip(1).Aggregate(
                            permissionGroupOccurenceMap[pgIds[0]],
                            (union, pgId) =>
                                union.Union(permissionGroupOccurenceMap[pgId]).ToList()
                        ).Select(x => new PermissionCheck(pair.Key, x));
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
            return pgIds.Skip(1).Aggregate(
                permissionGroupOccurenceMap[pgIds[0]],
                (intersection, pgId) =>
                    intersection.Intersect(permissionGroupOccurenceMap[pgId]).ToList()
            ).Select(x => new PermissionCheck(pair.Key, x));
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
                        return pgIds.Skip(1).Aggregate(
                            permissionGroupOccurenceMap[pgIds[0]],
                            (intersection, pgId) =>
                                intersection.Intersect(permissionGroupOccurenceMap[pgId]).ToList()
                        ).Select(x => new PermissionCheck(pair.Key, x));
                    })
            ).ToArray();
}

public class PermissionCheck(short permissionSetId1, short permissionSetId2)
{
    public short PermissionSetId1 { get; } = permissionSetId1;
    public short PermissionSetId2 { get; } = permissionSetId2;
}
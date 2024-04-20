using System.Collections.Generic;
using System.Linq;
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
        var singleItemsChunkWithProcessor = singleItems
            .Chunk(singleItemsCount / ChunkCount)
            .Select<Pairs, (Pairs pairs, Generator generator)>(chunk => (chunk, GenerateWithNoDistinct));
        var multipleItemsChunkWithProcessor = multipleItems
            .Chunk(multipleItemsCount / ChunkCount)
            .Select<Pairs, (Pairs pairs, Generator generator)>(chunk => (chunk, GenerateWithDistinct));

        return singleItemsChunkWithProcessor.Concat(multipleItemsChunkWithProcessor)
            .AsParallel()
            .SelectMany(chunkWithGenerator => 
                chunkWithGenerator.generator(chunkWithGenerator.pairs, permissionGroupOccurenceMap))
            .ToArray();
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

public record PermissionCheck(short PermissionSetId1, short PermissionSetId2);
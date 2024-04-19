using System.Linq;
using PermissionGroupOccurenceMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;
using PermissionSetMap = System.Collections.Generic.Dictionary<short, System.Collections.Generic.List<short>>;

namespace Permussion;

public static class Permussioned
{
    private const int ChunkCount = 64;

    public static PermissionCheck[] CalculatePermissionChecksDistinct(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.SelectMany(
            pair => pair.Value
                .SelectMany(x => permissionGroupOccurenceMap[x])
                .Distinct()
                .Select(x => new PermissionCheck(pair.Key, x))
        ).ToArray();

    public static PermissionCheck[] CalculatePermissionChecksDistinctWithPredicategorize(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        var (multipleItems, singleItems) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        var singlePermissionChecks = singleItems.SelectMany(
            pair => permissionGroupOccurenceMap[pair.Value[0]]
                .Select(x => new PermissionCheck(pair.Key, x)
            )
        );
        var multiplePermissionChecks = multipleItems.SelectMany(
            pair => pair.Value
                .SelectMany(x => permissionGroupOccurenceMap[x])
                .Distinct()
                .Select(x => new PermissionCheck(pair.Key, x))
        );

        return singlePermissionChecks.Concat(multiplePermissionChecks).ToArray();
    }

    public static PermissionCheck[] CalculatePermissionChecksDistinctParallel(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap) =>
        permissionSetMap.Chunk(permissionSetMap.Count / ChunkCount).AsParallel()
            .SelectMany(
                permissionSetMapChunk =>
                    permissionSetMapChunk.SelectMany(
                        pair => pair.Value
                            .SelectMany(x => permissionGroupOccurenceMap[x])
                            .Distinct()
                            .Select(x => new PermissionCheck(pair.Key, x))
                    )
            ).ToArray();

    public static PermissionCheck[] CalculatePermissionChecksDistinctParallelWithPredicategorize(
        PermissionSetMap permissionSetMap,
        PermissionGroupOccurenceMap permissionGroupOccurenceMap)
    {
        var (multipleItems, singleItems) = permissionSetMap.Predicategorize(
            x => x.Value.Count > 1);
        var chunkSize = permissionSetMap.Count / ChunkCount;
        var singlePermissionChecks = singleItems.Chunk(chunkSize).AsParallel()
            .SelectMany(
                permissionSetMapChunk =>
                    permissionSetMapChunk.SelectMany(
                        pair => permissionGroupOccurenceMap[pair.Value[0]]
                            .Select(x => new PermissionCheck(pair.Key, x)
                    )
            )
        );
        var multiplePermissionChecks = multipleItems.Chunk(chunkSize).AsParallel()
            .SelectMany(
                permissionSetMapChunk =>
                    permissionSetMapChunk.SelectMany(
                        pair => pair.Value
                            .SelectMany(x => permissionGroupOccurenceMap[x])
                            .Distinct()
                            .Select(x => new PermissionCheck(pair.Key, x))
                    )
            );

        return singlePermissionChecks.Concat(multiplePermissionChecks).ToArray();
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
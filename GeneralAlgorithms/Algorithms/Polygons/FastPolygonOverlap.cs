namespace GeneralAlgorithms.Algorithms.Polygons;

using System.Collections.Generic;

using DataStructures.Common;
using DataStructures.Polygons;

/// <summary>
/// Computes polygon overlap by fast caching with int aliases.
/// </summary>
public class FastPolygonOverlap : PolygonOverlapBase<IntAlias<GridPolygon>>
{
    private readonly Dictionary<int, List<GridRectangle>> decompositions = new Dictionary<int, List<GridRectangle>>();
    private readonly GridPolygonPartitioning polygonPartitioning = new GridPolygonPartitioning();

    protected override List<GridRectangle> GetDecomposition(IntAlias<GridPolygon> polygon)
    {
        var alias = polygon.Alias;

        if (decompositions.ContainsKey(alias) == false)
        {
            decompositions[alias] = null;
        }

        var decomposition = decompositions[alias];

        if (decomposition == null)
        {
            decomposition = polygonPartitioning.GetPartitions(polygon.Value);
            decompositions[alias] = decomposition;
        }

        return decomposition;
    }

    protected override GridRectangle GetBoundingRectangle(IntAlias<GridPolygon> polygon)
    {
        return polygon.Value.BoundingRectangle;
    }
}
namespace GeneralAlgorithms.Algorithms.Polygons
{
	using System.Collections.Generic;
	using DataStructures.Polygons;
    using GeneralAlgorithms.DataStructures.Common;

    /// <summary>
    /// Computes polygon overlap by caching polygon partitions of polygons.
    /// See <see cref="FastPolygonOverlap"/> for a faster implementation.
    /// </summary>
    public class PolygonOverlap : PolygonOverlapBase<GridPolygon>
	{
        private readonly Dictionary<GridPolygon, List<GridRectangle>> decompositions = new Dictionary<GridPolygon, List<GridRectangle>>();
        private readonly GridPolygonPartitioning polygonPartitioning = new GridPolygonPartitioning();

		protected override List<GridRectangle> GetDecomposition(GridPolygon polygon)
		{
			if (decompositions.TryGetValue(polygon, out var p))
			{
				return p;
			}

			var ps = polygonPartitioning.GetPartitions(polygon);
			decompositions.Add(polygon, ps);

			return ps;
		}

		protected override GridRectangle GetBoundingRectangle(GridPolygon polygon)
		{
			return polygon.BoundingRectangle;
		}
    }
}
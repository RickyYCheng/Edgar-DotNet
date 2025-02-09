using System.Collections.ObjectModel;
using System.Diagnostics;

using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.Algorithms.Polygons;
using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.ConfigurationSpaces;
using MapGeneration.Core.Doors;
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var cspaceGen = new ConfigurationSpacesGenerator(
    new PolygonOverlap(),
    DoorHandler.DefaultHandler,
    new OrthogonalLineIntersection(),
    new GridPolygonUtils()
);

var shape1 = HoleToWeakPolygonContour(GridPolygon.GetRectangle(4, 3));
var door1 = new SpecificPositionsMode([
    new(new(0,0), new(0,1)),
    new(new(0,1), new(0,2)),
    new(new(0,2), new(0,3)),
    new(new(0,3), new(1,3)),
    new(new(1,3), new(2,3)),
    new(new(2,3), new(3,3)),
    new(new(3,3), new(4,3)),
    new(new(4,3), new(4,2)),
    new(new(4,2), new(4,1)),
    new(new(4,1), new(4,0)),
    new(new(4,0), new(3,0)),
    new(new(3,0), new(2,0)),
    new(new(2,0), new(1,0)),
    new(new(1,0), new(0,0)),
]);
var shape2 = GridPolygon.GetRectangle(4, 3);
var door2 = new OverlapMode(1, 0);

var cspace = cspaceGen.GetConfigurationSpace(shape2, door2, shape1, door1);

Console.WriteLine();

static GridPolygon HoleToWeakPolygonContour(GridPolygon hole)
{
    var aabb = hole.BoundingRectangle;
    ReadOnlyCollection<IntVector2> points = hole.GetPoints();

    var slicePoint = points.Where(e => e.X == aabb.A.X).Min();

    var idx = points.IndexOf(slicePoint);
    var result = new List<IntVector2>(points.Count + 6);
    CircularShiftRight(points, result, idx);

    result.Add(slicePoint);
    result.Reverse();

    result.Add(new(aabb.A.X - 1, slicePoint.Y));
    result.Add(new(aabb.A.X - 1, aabb.B.Y + 1));
    result.Add(new(aabb.B.X + 1, aabb.B.Y + 1));
    result.Add(new(aabb.B.X + 1, aabb.A.Y - 1));
    result.Add(new(slicePoint.X, aabb.A.Y - 1));

    return new(result);

    static void CircularShiftRight<T>(ReadOnlyCollection<T> arr, List<T> shifted, int n)
    {
        if (arr == null || arr.Count <= 0)
            return;

        int count = arr.Count;
        n %= count;

        if (n == 0)
        {
            shifted.AddRange(arr);
            return;
        }

        shifted.Clear();
        for (int i = n; i < count; i++)
            shifted.Add(arr[i]);
        for (int i = 0; i < n; i++)
            shifted.Add(arr[i]);
    }
}

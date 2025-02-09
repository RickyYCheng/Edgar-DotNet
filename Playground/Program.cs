using System.Collections.ObjectModel;
using System.Diagnostics;

using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var mapDescription = new MapDescription<string>();
mapDescription.AddRoom("A");
mapDescription.AddRoom("B");
mapDescription.AddRoom("C");
mapDescription.AddRoom("D");
mapDescription.AddRoom("E");
mapDescription.AddRoom("F");
mapDescription.AddPassage("A", "B");
mapDescription.AddPassage("B", "C");
mapDescription.AddPassage("C", "D");
mapDescription.AddPassage("D", "A");
mapDescription.AddPassage("D", "E");
mapDescription.AddPassage("E", "F");

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(1, 1),
  new OverlapMode(1, 0)
);

var corridorRoom = new RoomDescription(
    GridPolygon.GetRectangle(1, 1),
    new OverlapMode(1, 0)
);

mapDescription.AddCorridorShapes(corridorRoom);

mapDescription.AddRoomShapes("A", squareRoom);
mapDescription.AddRoomShapes("B", squareRoom);
mapDescription.AddRoomShapes("C", squareRoom);
mapDescription.AddRoomShapes("D", squareRoom);
mapDescription.AddRoomShapes("E", squareRoom);
mapDescription.AddRoomShapes("F", squareRoom);

var generator = 
    NodeConstraintArgs<string>
    .Boundary(10, 10)
    .WithSpecificNodeBoundary("A", 1, 1, new(9, 0))
    .WithBasic()
    .GetChainBasedGenerator();

var sw = Stopwatch.StartNew();
var layout = generator.GetLayouts(mapDescription, 1)[0];
sw.Stop();
Console.WriteLine(sw.ElapsedMilliseconds);

layout.ToPlot().SavePng("./result.png", 1000, 1000);

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

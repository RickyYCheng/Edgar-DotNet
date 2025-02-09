using System.Collections.ObjectModel;

using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var mapDescription = new MapDescription<int>();
mapDescription.AddRoom(0);
mapDescription.AddRoom(1);
mapDescription.AddPassage(0, 1);

var doorMode = new OverlapMode(1, 0);

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(2, 2),
  doorMode
);

var corridorRoom = new RoomDescription(
    GridPolygon.GetSquare(1),
    doorMode
);

mapDescription.AddRoomShapes(0, squareRoom);
mapDescription.AddRoomShapes(1, squareRoom);
mapDescription.AddCorridorShapes(corridorRoom);

var generator =
    NodeConstraintArgs<int>
    .Basic()
    .GetChainBasedGenerator([0, 1], true);

var layout = generator.GetLayouts(mapDescription, 1)[0];

var plot = layout.ToPlot();
plot.SavePng("./result.png", 1000, 1000);

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

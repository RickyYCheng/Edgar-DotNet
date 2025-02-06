using System.Diagnostics;

using GeneralAlgorithms.Algorithms.Common;
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

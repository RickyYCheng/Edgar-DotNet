using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var mapDescription = new MapDescription<string>();
mapDescription.AddRoom("Room A");
mapDescription.AddRoom("Room B");
mapDescription.AddPassage("Room A", "Room B");

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(1, 1),
  new OverlapMode(1, 0)
);

var corridorRoom = new RoomDescription(
    GridPolygon.GetSquare(1),
    new OverlapMode(1, 0)
);

mapDescription.AddRoomShapes("Room A", squareRoom);
mapDescription.AddRoomShapes("Room B", squareRoom);
mapDescription.AddCorridorShapes(corridorRoom);

var generator =
    NodeConstraintArgs<string>
    .Boundary(10, 10, new(0, 0), [
        ("Room A", new ([
            new(new (9, 0), new (10, 0)),
        ])),
    ])
    .WithBasic()
    .GetChainBasedGenerator(/*[0, 1], true*/);

var layout = generator.GetLayouts(mapDescription, 1)[0];

layout.ToPlot().SavePng("./result.png", 1000, 1000);

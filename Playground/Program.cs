using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var mapDescription = new MapDescription<string>();
mapDescription.AddRoom("Room A");
mapDescription.AddRoom("Room B");
mapDescription.AddRoom("Room C");
mapDescription.AddRoom("Room D");
mapDescription.AddPassage("Room A", "Room B");
mapDescription.AddPassage("Room B", "Room C");
mapDescription.AddPassage("Room C", "Room D");

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(3, 3),
  new OverlapMode(1, 1)
);

var corridorRoom = new RoomDescription(
    GridPolygon.GetSquare(1),
    new SpecificPositionsMode([
        new(new(0, 0), new(1, 0)),
        new(new(0, 1), new(1, 1)),
    ])
);

mapDescription.AddRoomShapes("Room A", squareRoom);
mapDescription.AddRoomShapes("Room B", squareRoom);
mapDescription.AddRoomShapes("Room C", squareRoom);
mapDescription.AddRoomShapes("Room D", squareRoom);
mapDescription.AddCorridorShapes(corridorRoom);

var generator =
    NodeConstraintArgs<string>
    .Boundary(20, 10, new(-10, 0), [
        ("Room A", new ([
            new(new (18, 0), new (19, 0)),
        ])),
    ])
    .WithBasic()
    .GetChainBasedGenerator(/*[0, 1], true*/);

var layout = generator.GetLayouts(mapDescription, 1)[0];

layout.ToPlot().SavePng("./result.png", 1000, 1000);

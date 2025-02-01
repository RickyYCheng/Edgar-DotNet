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
  GridPolygon.GetRectangle(3, 3),
  new OverlapMode(1, 1)
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
    .Boundary(10, 10, new(), [
        ("Room A", new ([
            //new(new (7, 0), new (8, 0)),
            new(new (8, 0), new (9, 0)),
        ])),
        //("Room B", new ([new(new (10, 5), new (10, 6))]))
    ])
    .WithBasic()
    .GetChainBasedGenerator([0, 1]);

var layout = generator.GetLayouts(mapDescription, 1)[0];

layout.ToPlot().SavePng("./result.png", 1000, 1000);

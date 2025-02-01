using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var mapDescription = new MapDescription<int>();
// Add rooms ( - you would normally use a for cycle)
mapDescription.AddRoom(0);
mapDescription.AddRoom(1);
//mapDescription.AddRoom(2);
//mapDescription.AddRoom(3);
// Add passages
mapDescription.AddPassage(0, 1);
//mapDescription.AddPassage(1, 2);
//mapDescription.AddPassage(2, 3);

var doorMode = new OverlapMode(1, 1);

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(3, 3),
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
    .Boundary(10, 10, new(), [
        (0, new ([new(new (8, 0), new (9, 0))])),
        //(1, new ([new(new (10, 5), new (10, 6))]))
    ])
    .WithBasic()
    .GetChainBasedGenerator([1]);

var layout = generator.GetLayouts(mapDescription, 1)[0];

layout.ToPlot().SavePng("./result.png", 1000, 1000);

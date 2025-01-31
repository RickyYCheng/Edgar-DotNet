using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
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

var doorMode = new OverlapMode(1, 0);

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(1, 2),
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
        (0, new ([new(new (9, 0), new (10, 0))])),
        (1, new ([new(new (10, 3), new (10, 4))]))
    ])
    .WithBasic()
    .GetChainBasedGenerator([1]);

var layouts = generator.GetLayouts(mapDescription, 1);

foreach (var layout in layouts)
{
    foreach (var position in layout.Rooms.Select(e => e.Position))
        Console.WriteLine(position);
}

Console.WriteLine();

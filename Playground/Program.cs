using GeneralAlgorithms.DataStructures.Common;
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

mapDescription.AddRoomShapes(0, squareRoom);
mapDescription.AddRoomShapes(1, squareRoom);

//var generator = LayoutGeneratorFactory.GetChainBasedGeneratorWithObstacle<int>(
//    GridPolygon.GetSquare(20),
//    new IntVector2(-10, -10)
//);

//var generator = LayoutGeneratorFactory.GetChainBasedGeneratorWithBoundary<int>(
//    20, 20, new IntVector2(-30, -30)
//);

var generator =
    NodeConstraintArgs<int>
    .Boundary(10, 10, new(), [
        (0, new SpecificPositionsMode([new(new IntVector2(9, 0), new IntVector2(10, 0))])),
        (1, new SpecificPositionsMode([new(new IntVector2(10, 2), new IntVector2(10, 3))]))
    ])
    .WithBasic()
    .GetChainBasedGenerator();

var layouts = generator.GetLayouts(mapDescription, 1);

foreach (var layout in layouts)
{
    foreach (var position in layout.Rooms.ToArray().Select(e => e.Position))
        Console.WriteLine(position);
}

Console.WriteLine();

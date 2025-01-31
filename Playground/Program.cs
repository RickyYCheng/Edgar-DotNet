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
  GridPolygon.GetSquare(2),
  doorMode
);

mapDescription.AddRoomShapes([squareRoom]);

//var generator = LayoutGeneratorFactory.GetChainBasedGeneratorWithObstacle<int>(
//    GridPolygon.GetSquare(20),
//    new IntVector2(-10, -10)
//);

//var generator = LayoutGeneratorFactory.GetChainBasedGeneratorWithBoundary<int>(
//    20, 20, new IntVector2(-30, -30)
//);

var generator =
    NodeConstraintArgs<int>
    .Boundary(20, new(-30))
    .WithBasic()
    .GetChainBasedGenerator();

var layouts = generator.GetLayouts(mapDescription, 10);

foreach (var layout in layouts)
{
    foreach(var room in layout.Rooms)
    {
        Console.WriteLine(room.Position);
    }
}

Console.WriteLine();

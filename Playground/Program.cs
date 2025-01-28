using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Polygons;
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Utils;

var mapDescription = new MapDescription<int>();
// Add rooms ( - you would normally use a for cycle)
mapDescription.AddRoom(0);
mapDescription.AddRoom(1);
mapDescription.AddRoom(2);
mapDescription.AddRoom(3);
// Add passages
mapDescription.AddPassage(0, 1);
mapDescription.AddPassage(0, 3);
mapDescription.AddPassage(1, 2);
mapDescription.AddPassage(2, 3);

var doorMode = new OverlapMode(1, 1);

var squareRoom = new RoomDescription(
  GridPolygon.GetSquare(8),
  doorMode
);
var rectangleRoom = new RoomDescription(
  GridPolygon.GetRectangle(6, 10),
  doorMode
);
mapDescription.AddRoomShapes([squareRoom, rectangleRoom]);

var generator = LayoutGeneratorFactory.GetChainBasedGeneratorWithObstacles<int>(
    [GridPolygon.GetRectangle(20, 500), GridPolygon.GetRectangle(20, 500)]
    , [new IntVector2(-20, 0), new IntVector2(20, 0)]
);
var layout = generator.GetLayouts(mapDescription, 1)[0];

_ = Console.ReadKey();

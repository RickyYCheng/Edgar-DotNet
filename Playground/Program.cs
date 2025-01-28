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
  GridPolygon.GetSquare(3),
  doorMode
);

mapDescription.AddRoomShapes([squareRoom]);

var generator = LayoutGeneratorFactory.GetChainBasedGeneratorWithObstacles<int>(
    GridPolygon.GetSquare(10), 
    new IntVector2(-5, -5)
);  
var layout = generator.GetLayouts(mapDescription, 1)[0];

Console.WriteLine("Finish!");
_ = Console.ReadKey();

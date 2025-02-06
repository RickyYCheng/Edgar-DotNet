using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.Algorithms.Polygons;
using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Configurations;
using MapGeneration.Core.Configurations.EnergyData;
using MapGeneration.Core.ConfigurationSpaces;
using MapGeneration.Core.Doors;
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Plot;
using MapGeneration.Utils;

var mapDescription = new MapDescription<string>();
mapDescription.AddRoom("Room A");
mapDescription.AddRoom("Room B");
mapDescription.AddRoom("Room C");
mapDescription.AddRoom("Room D");
mapDescription.AddRoom("Room E");
mapDescription.AddRoom("Room F");
mapDescription.AddPassage("Room A", "Room B");
mapDescription.AddPassage("Room B", "Room C");
mapDescription.AddPassage("Room C", "Room D");
mapDescription.AddPassage("Room D", "Room A");
mapDescription.AddPassage("Room D", "Room E");
mapDescription.AddPassage("Room E", "Room F");

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(1, 1),
  new OverlapMode(1, 0)
);

mapDescription.AddRoomShapes("Room A", squareRoom);
mapDescription.AddRoomShapes("Room B", squareRoom);
mapDescription.AddRoomShapes("Room C", squareRoom);
mapDescription.AddRoomShapes("Room D", squareRoom);
mapDescription.AddRoomShapes("Room E", squareRoom);
mapDescription.AddRoomShapes("Room F", squareRoom);

var generator = 
    NodeConstraintArgs<string>
    //.Boundary(10, 10)
    .SpecificNodeBoundary("Room A", 1, 1, new(7, 0))
    .WithBasic()
    .GetChainBasedGenerator();
// FIXME: perturb damping
var layout = generator.GetLayouts(mapDescription, 1)[0];

layout.ToPlot().SavePng("./result.png", 1000, 1000);

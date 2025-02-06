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
mapDescription.AddPassage("Room A", "Room B");

var squareRoom = new RoomDescription(
  GridPolygon.GetRectangle(1, 1),
  new OverlapMode(1, 0)
);

mapDescription.AddRoomShapes("Room A", squareRoom);
mapDescription.AddRoomShapes("Room B", squareRoom);

var generator = 
    NodeConstraintArgs<string>
    //.Boundary(10, 10)
    .SpecificNodeBoundary("Room A", 1, 1, new(9, 0))
    .WithBasic()
    .GetChainBasedGenerator();
// FIXME: perturb damping
var layout = generator.GetLayouts(mapDescription, 1)[0];

layout.ToPlot().SavePng("./result.png", 1000, 1000);

using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.Algorithms.Polygons;
using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.Configurations;
using MapGeneration.Core.Configurations.EnergyData;
using MapGeneration.Core.ConfigurationSpaces;
using MapGeneration.Core.Doors;
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.MapDescriptions;

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

var cspaceGen = new ConfigurationSpacesGenerator(
    new PolygonOverlap(), DoorHandler.DefaultHandler,
    new OrthogonalLineIntersection(), new GridPolygonUtils());

var cspaces = cspaceGen.Generate<string, Configuration<EnergyData>>(mapDescription);

var cspace = cspaces.GetConfigurationSpace(
    new(0, GridPolygon.GetRectangle(1, 1)), 
    new(0, GridPolygon.GetRectangle(1, 1))
);

Console.WriteLine();

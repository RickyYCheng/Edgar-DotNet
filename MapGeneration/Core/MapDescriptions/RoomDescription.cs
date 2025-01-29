﻿namespace MapGeneration.Core.MapDescriptions;

using GeneralAlgorithms.DataStructures.Polygons;

using Interfaces.Core.Doors;
using Interfaces.Core.MapDescriptions;

/// <summary>
/// Description of a room.
/// </summary>
public class RoomDescription : IRoomDescription
{
    public GridPolygon Shape { get; }

    public IDoorMode DoorsMode { get; }

    public RoomDescription(GridPolygon shape, IDoorMode doorsMode)
    {
        Shape = new GridPolygon(shape.GetPoints());
        DoorsMode = doorsMode;
    }
}
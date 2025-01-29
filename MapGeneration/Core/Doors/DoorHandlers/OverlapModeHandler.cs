﻿namespace MapGeneration.Core.Doors.DoorHandlers;

using System;
using System.Collections.Generic;

using DoorModes;

using GeneralAlgorithms.DataStructures.Polygons;

using Interfaces.Core.Doors;

/// <summary>
/// Generates door positions for <see cref="OverlapMode"/>.
/// </summary>
public class OverlapModeHandler : IDoorHandler
{
    /// <inheritdoc />
    public List<IDoorLine> GetDoorPositions(GridPolygon polygon, IDoorMode doorModeRaw)
    {
        if (!(doorModeRaw is OverlapMode doorMode))
            throw new InvalidOperationException("Invalid door mode supplied");

        var lines = new List<IDoorLine>();

        foreach (var line in polygon.GetLines())
        {
            if (line.Length < 2 * doorMode.CornerDistance + doorMode.DoorLength)
                continue;

            lines.Add(new DoorLine(line.Shrink(doorMode.CornerDistance, doorMode.CornerDistance + doorMode.DoorLength), doorMode.DoorLength));
        }

        return lines;
    }
}
namespace MapGeneration.Core.Constraints;
using System;
using System.Collections.Generic;

using GeneralAlgorithms.Algorithms.Polygons;
using MapGeneration.Interfaces.Core.Configuration;
using MapGeneration.Interfaces.Core.Configuration.EnergyData;
using MapGeneration.Interfaces.Core.Constraints;
using MapGeneration.Interfaces.Core.Layouts;
using MapGeneration.Core.ConfigurationSpaces;
using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.DataStructures.Common;
using System.Linq;

public class BoundaryConstraint<TLayout, TNode, TConfiguration, TEnergyData, TShapeContainer> : INodeConstraint<TLayout, TNode, TConfiguration, TEnergyData>
    where TLayout : ILayout<TNode, TConfiguration>
    where TConfiguration : IEnergyConfiguration<TShapeContainer, TEnergyData>
    where TEnergyData : INodeEnergyData, new()
{
    private readonly OrthogonalLineIntersection lineIntersection = new();
    private readonly IPolygonOverlap<TShapeContainer> polygonOverlap;
    private readonly float energySigma;
    private readonly TConfiguration boundary;
    private readonly Dictionary<TNode, TConfiguration> doorConfigurations;
    private readonly Dictionary<TNode, ConfigurationSpace> doorConfigurationSpaces;
    private readonly Dictionary<TNode, IntVector2> doorPositions;

    private bool IsBoundaryDoorAvailable => 
        doorConfigurations != null && doorConfigurations.Count > 0
        && doorConfigurationSpaces != null && doorConfigurationSpaces.Count > 0
        && doorPositions != null && doorPositions.Count > 0;

    public BoundaryConstraint(
        IPolygonOverlap<TShapeContainer> polygonOverlap,
        float averageSize,
        TConfiguration boundary,
        Dictionary<TNode, TConfiguration> doorConfigurations,
        Dictionary<TNode, ConfigurationSpace> doorConfigurationSpaces,
        Dictionary<TNode, IntVector2> doorPositions)
    {
        this.polygonOverlap = polygonOverlap;
        energySigma = 10 * averageSize;
        this.boundary = boundary;
        this.doorConfigurations = doorConfigurations;
        this.doorConfigurationSpaces = doorConfigurationSpaces;
        this.doorPositions = doorPositions;
    }

    /// <inheritdoc />
    public bool ComputeEnergyData(TLayout layout, TNode node, TConfiguration configuration, ref TEnergyData energyData)
    {
        int overlap = ComputeOverlap(configuration, boundary);
        int distance = overlap == 0 ? 0 : ComputeDistance(configuration, boundary);

        if(!IsNodeConnectedToBoundary(node, configuration))
            distance += ComputeDistanceToDoor(node, configuration);

        energyData.MoveDistance = distance;
        energyData.Overlap = overlap;
        energyData.Energy += ComputeEnergy(overlap, distance);
        return overlap == 0 && distance == 0;
    }

    /// <inheritdoc />
    public bool UpdateEnergyData(TLayout layout, TNode perturbedNode, TConfiguration oldConfiguration,
        TConfiguration newConfiguration, TNode node, TConfiguration configuration, ref TEnergyData energyData)
    {
        return ComputeEnergyData(layout, node, configuration, ref energyData);
    }

    /// <inheritdoc />
    public bool UpdateEnergyData(TLayout oldLayout, TLayout newLayout, TNode node, ref TEnergyData energyData)
    {
        newLayout.GetConfiguration(node, out var configuration);
        return ComputeEnergyData(newLayout, node, configuration, ref energyData);
    }

    private int ComputeOverlap(TConfiguration configuration, TConfiguration boundary)
    {
        var area = polygonOverlap.OverlapArea(configuration.ShapeContainer, configuration.Position, boundary.ShapeContainer, boundary.Position);
        var polyArea = polygonOverlap.GetArea(configuration.ShapeContainer);
        return polyArea - area;
    }

    private int ComputeDistance(TConfiguration configuration, TConfiguration boundary)
    {
        var counter = configuration.Shape.BoundingRectangle + configuration.Position;
        var hole = boundary.Shape.BoundingRectangle + boundary.Position;

        // 判断孔是否能够容纳轮廓
        if (hole.Width < counter.Width || hole.Height < counter.Height)
            return -1;

        // 计算x轴方向的移动距离
        int aX = hole.A.X;
        int bX = hole.B.X - counter.Width;
        int x0 = counter.A.X;
        int dx = Math.Max(aX - x0, 0) + Math.Max(x0 - bX, 0);

        // 计算y轴方向的移动距离
        int aY = hole.A.Y;
        int bY = hole.B.Y - counter.Height;
        int y0 = counter.A.Y;
        int dy = Math.Max(aY - y0, 0) + Math.Max(y0 - bY, 0);

        return dx + dy;
    }

    private float ComputeEnergy(int overlap, float distance)
        => (float)(Math.Exp(overlap / (energySigma * 625f) + distance / (energySigma * 50f)) - 1);

    private int ComputeDistanceToDoor(TNode node, TConfiguration roomConfig)
    {
        if (IsBoundaryDoorAvailable is false) return 0;
        if (doorConfigurations.ContainsKey(node) is false) return 0;

        return IntVector2.ManhattanDistance(doorPositions[node], roomConfig.Position);
    }

    private bool IsNodeConnectedToBoundary(TNode node, TConfiguration roomConfig)
    {
        if (IsBoundaryDoorAvailable is false) return true;
        if (doorConfigurations.ContainsKey(node) is false) return true;

        var doorConfig = doorConfigurations[node];
        var cspace = doorConfigurationSpaces[node];

        List<OrthogonalLine> lines1 = [new(roomConfig.Position, roomConfig.Position)];
        return lineIntersection.DoIntersect(cspace.Lines.Select(x => FastAddition(x, doorConfig.Position)), lines1);
    }
    private OrthogonalLine FastAddition(OrthogonalLine line, IntVector2 position)
    {
        return new OrthogonalLine(line.From + position, line.To + position);
    }
}
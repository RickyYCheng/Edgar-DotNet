﻿namespace MapGeneration.Core.Constraints
{
    using System;
    using System.Collections.Generic;
    using GeneralAlgorithms.Algorithms.Polygons;
    using GeneralAlgorithms.DataStructures.Common;
    using Interfaces.Core.Configuration;
    using Interfaces.Core.Configuration.EnergyData;
    using Interfaces.Core.Constraints;
    using Interfaces.Core.Layouts;
    using MapGeneration.Core.ConfigurationSpaces;
    using MapGeneration.Interfaces.Core.ConfigurationSpaces;

    public class ObstacleConstraint<TLayout, TNode, TConfiguration, TEnergyData, TShapeContainer> : INodeConstraint<TLayout, TNode, TConfiguration, TEnergyData>
        where TLayout : ILayout<TNode, TConfiguration>
        where TConfiguration : IEnergyConfiguration<TShapeContainer, TEnergyData>
        where TEnergyData : INodeEnergyData, new()
    {
        // IPolygonOverlap<TShapeContainer> might be IntAtlas<GridPolygon>
        private readonly IPolygonOverlap<TShapeContainer> polygonOverlap;
        private readonly float energySigma;
        private readonly IConfigurationSpaces<TNode, TShapeContainer, TConfiguration, ConfigurationSpace> configurationSpaces;
        private readonly TConfiguration obstacle;

        public ObstacleConstraint(IPolygonOverlap<TShapeContainer> polygonOverlap, float averageSize, IConfigurationSpaces<TNode, TShapeContainer, TConfiguration, ConfigurationSpace> configurationSpaces, TConfiguration obstacle)
        {
            this.polygonOverlap = polygonOverlap;
            energySigma = 10 * averageSize;
            this.configurationSpaces = configurationSpaces;
            this.obstacle = obstacle;
        }

        public bool ComputeEnergyData(TLayout layout, TNode node, TConfiguration configuration, ref TEnergyData energyData)
        {
            var overlap = 0;
            var distance = 0;
            var neighbours = new HashSet<TNode>(layout.Graph.GetNeighbours(node));

            foreach (var vertex in layout.Graph.Vertices)
            {
                if (vertex.Equals(node))
                    continue;

                if (!layout.GetConfiguration(vertex, out var c))
                    continue;

                var area = ComputeOverlap(configuration, c);

                if (area != 0)
                {
                    overlap += area;
                }
                else if (neighbours.Contains(vertex))
                {
                    if (!configurationSpaces.HaveValidPosition(configuration, c))
                    {
                        // TODO: this is not really accurate when there are more sophisticated door positions (as smaller distance is not always better)
                        distance += ComputeDistance(configuration, c);
                    }
                }
            }

            overlap += ComputeOverlap(configuration, obstacle);

            var energy = ComputeEnergy(overlap, distance);

            energyData.Overlap = overlap;
            energyData.MoveDistance = distance;
            energyData.Energy += energy;

            return overlap == 0 && distance == 0;
        }

        public bool UpdateEnergyData(TLayout layout, TNode perturbedNode, TConfiguration oldConfiguration,
            TConfiguration newConfiguration, TNode node, TConfiguration configuration, ref TEnergyData energyData)
        {
            var overlapOld = ComputeOverlap(configuration, oldConfiguration);
            var overlapNew = ComputeOverlap(configuration, newConfiguration);
            var overlapTotal = configuration.EnergyData.Overlap + (overlapNew - overlapOld);

            // MoveDistance should not be recomputed as it is used only when two nodes are neighbours which is not the case here
            var distanceTotal = configuration.EnergyData.MoveDistance;
            if (AreNeighbours(layout, perturbedNode, node))
            {
                // Distance is taken into account only when there is no overlap
                var distanceOld = overlapOld == 0 && !configurationSpaces.HaveValidPosition(oldConfiguration, configuration) ? ComputeDistance(configuration, oldConfiguration) : 0;
                var distanceNew = overlapNew == 0 && !configurationSpaces.HaveValidPosition(newConfiguration, configuration) ? ComputeDistance(configuration, newConfiguration) : 0;

                distanceTotal = configuration.EnergyData.MoveDistance + (distanceNew - distanceOld);
            }

            var obstacleOverlapOld = ComputeOverlap(obstacle, oldConfiguration);
            var obstacleOverlapNew = ComputeOverlap(obstacle, newConfiguration);
            overlapTotal += obstacleOverlapNew - obstacleOverlapOld;

            var newEnergy = ComputeEnergy(overlapTotal, distanceTotal);

            energyData.MoveDistance = distanceTotal;
            energyData.Overlap = overlapTotal;
            energyData.Energy += newEnergy;

            return overlapTotal == 0 && distanceTotal == 0;
        }

        public bool UpdateEnergyData(TLayout oldLayout, TLayout newLayout, TNode node, ref TEnergyData energyData)
        {
            oldLayout.GetConfiguration(node, out var oldConfiguration);
            var newOverlap = oldConfiguration.EnergyData.Overlap;
            var newDistance = oldConfiguration.EnergyData.MoveDistance;

            foreach (var vertex in oldLayout.Graph.Vertices)
            {
                if (vertex.Equals(node))
                    continue;

                if (!oldLayout.GetConfiguration(vertex, out var nodeConfiguration))
                    continue;

                newLayout.GetConfiguration(vertex, out var newNodeConfiguration);

                newOverlap += newNodeConfiguration.EnergyData.Overlap - nodeConfiguration.EnergyData.Overlap;
                newDistance += newNodeConfiguration.EnergyData.MoveDistance - nodeConfiguration.EnergyData.MoveDistance;

                var obstacleOverlapOld = ComputeOverlap(obstacle, oldConfiguration);
                var obstacleOverlapNew = ComputeOverlap(obstacle, newNodeConfiguration);
                newOverlap += obstacleOverlapNew - obstacleOverlapOld;
            }


            var newEnergy = ComputeEnergy(newOverlap, newDistance);

            energyData.MoveDistance = newDistance;
            energyData.Overlap = newOverlap;
            energyData.Energy += newEnergy;

            return newOverlap == 0 && newDistance == 0;
        }

        private int ComputeOverlap(TConfiguration configuration1, TConfiguration configuration2)
            => polygonOverlap.OverlapArea(configuration1.ShapeContainer, configuration1.Position, configuration2.ShapeContainer, configuration2.Position);

        private int ComputeDistance(TConfiguration configuration1, TConfiguration configuration2)
            => IntVector2.ManhattanDistance(
                configuration1.Shape.BoundingRectangle.Center + configuration1.Position,
                configuration2.Shape.BoundingRectangle.Center + configuration2.Position);

        private float ComputeEnergy(int overlap, float distance)
            => (float)(Math.Exp(overlap / (energySigma * 625)) * Math.Exp(distance / (energySigma * 50)) - 1);

        private bool AreNeighbours(TLayout layout, TNode node1, TNode node2)
            => layout.Graph.HasEdge(node1, node2);
    }
}
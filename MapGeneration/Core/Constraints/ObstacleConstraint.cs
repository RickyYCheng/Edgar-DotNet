namespace MapGeneration.Core.Constraints
{
    using System;
    using GeneralAlgorithms.Algorithms.Polygons;
    using GeneralAlgorithms.DataStructures.Common;
    using Interfaces.Core.Configuration;
    using Interfaces.Core.Configuration.EnergyData;
    using Interfaces.Core.Constraints;
    using Interfaces.Core.Layouts;

    public class ObstacleConstraint<TLayout, TNode, TConfiguration, TEnergyData, TShapeContainer> : INodeConstraint<TLayout, TNode, TConfiguration, TEnergyData>
        where TLayout : ILayout<TNode, TConfiguration>
        where TConfiguration : IEnergyConfiguration<TShapeContainer, TEnergyData>
        where TEnergyData : INodeEnergyData, new()
    {
        // IPolygonOverlap<TShapeContainer> might be IntAtlas<GridPolygon>
        private readonly IPolygonOverlap<TShapeContainer> polygonOverlap;
        private readonly float energySigma;
        private readonly TConfiguration obstacle;

        public ObstacleConstraint(IPolygonOverlap<TShapeContainer> polygonOverlap, float averageSize, TConfiguration obstacle)
        {
            this.polygonOverlap = polygonOverlap;
            energySigma = 10 * averageSize;
            this.obstacle = obstacle;
        }

        public bool ComputeEnergyData(TLayout layout, TNode node, TConfiguration configuration, ref TEnergyData energyData)
        {
            var overlap = ComputeOverlap(configuration, obstacle);
            var distance = overlap == 0 ? 0 : ComputeDistance(configuration, obstacle);

            var energy = ComputeEnergy(overlap, distance);

            energyData.Overlap = overlap;
            energyData.MoveDistance = distance;
            energyData.Energy += energy;

            return overlap == 0 && distance == 0;
        }

        public bool UpdateEnergyData(TLayout layout, TNode perturbedNode, TConfiguration oldConfiguration,
            TConfiguration newConfiguration, TNode node, TConfiguration configuration, ref TEnergyData energyData)
        {
            if (perturbedNode.Equals(node))
                return true;

            var overlapTotal = configuration.EnergyData.Overlap;
            var distanceTotal = configuration.EnergyData.MoveDistance;

            var obstacleOverlapOld = ComputeOverlap(oldConfiguration, obstacle);
            var obstacleDistanceOld = obstacleOverlapOld == 0 ? 0 : ComputeDistance(oldConfiguration, obstacle);
            var obstacleOverlapNew = ComputeOverlap(newConfiguration, obstacle);
            var obstacleDistanceNew = obstacleOverlapNew == 0 ? 0 : ComputeDistance(newConfiguration, obstacle);

            overlapTotal += (obstacleOverlapNew - obstacleOverlapOld);
            distanceTotal += (obstacleDistanceNew - obstacleDistanceOld);

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
    }
}
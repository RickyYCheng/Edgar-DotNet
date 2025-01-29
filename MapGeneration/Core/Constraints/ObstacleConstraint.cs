namespace MapGeneration.Core.Constraints
{
    using System;
    using GeneralAlgorithms.Algorithms.Polygons;
    using Interfaces.Core.Configuration;
    using Interfaces.Core.Configuration.EnergyData;
    using Interfaces.Core.Constraints;
    using Interfaces.Core.Layouts;
    using MapGeneration.Core.Layouts;

    public class ObstacleConstraint<TLayout, TNode, TConfiguration, TEnergyData, TShapeContainer> : INodeConstraint<TLayout, TNode, TConfiguration, TEnergyData>
        where TLayout : ILayout<TNode, TConfiguration>
        where TConfiguration : IEnergyConfiguration<TShapeContainer, TEnergyData>
        where TEnergyData : INodeEnergyData, new()
    {
        private readonly IPolygonOverlap<TShapeContainer> polygonOverlap;
        private readonly float energySigma;
        private readonly TConfiguration obstacle;

        public ObstacleConstraint(IPolygonOverlap<TShapeContainer> polygonOverlap, float averageSize, TConfiguration obstacle)
        {
            this.polygonOverlap = polygonOverlap;
            energySigma = 10 * averageSize;
            this.obstacle = obstacle;
        }

        /// <inheritdoc />
        public bool ComputeEnergyData(TLayout layout, TNode node, TConfiguration configuration, ref TEnergyData energyData)
        {
            var overlap = ComputeOverlap(configuration, obstacle);
            var distance = ComputeDistance(configuration, obstacle);

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

        private int ComputeOverlap(TConfiguration configuration, TConfiguration obstacle)
            => polygonOverlap.OverlapArea(configuration.ShapeContainer, configuration.Position, obstacle.ShapeContainer, obstacle.Position);

        private int ComputeDistance(TConfiguration configuration, TConfiguration obstacle)
        {
            var a = configuration.Shape.BoundingRectangle + configuration.Position;
            var b = obstacle.Shape.BoundingRectangle + obstacle.Position;

            // Compute the penetration depth along the X-axis
            int aWidth = a.B.X - a.A.X;
            int aCenterX = (a.A.X + a.B.X) / 2;
            int bWidth = b.B.X - b.A.X;
            int bCenterX = (b.B.X + b.A.X) / 2;
            int dx = Math.Abs(aCenterX - bCenterX);
            int xPenetration = (aWidth / 2 + bWidth / 2) - dx;

            // Compute the penetration depth along the Y-axis
            int aHeight = a.B.Y - a.A.Y;
            int aCenterY = (a.A.Y + a.B.Y) / 2;
            int bHeight = b.B.Y - b.A.Y;
            int bCenterY = (b.B.Y + b.A.Y) / 2;
            int dy = Math.Abs(aCenterY - bCenterY);
            int yPenetration = (aHeight / 2 + bHeight / 2) - dy;

            // If there is no overlap along either axis, return 0 (ReLU)
            if (xPenetration <= 0 || yPenetration <= 0)
                return 0;

            // Return the smaller penetration depth as the minimum separation distance
            return Math.Min(xPenetration, yPenetration);
        }

        private float ComputeEnergy(int overlap, float distance)
            => (float)(Math.Exp(overlap / (energySigma * 625)) * Math.Exp(distance / (energySigma * 50)) - 1);
    }
}
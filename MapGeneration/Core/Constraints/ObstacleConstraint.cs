namespace MapGeneration.Core.Constraints
{
    using System;
    using System.Collections.Generic;
    using ConfigurationSpaces;
    using GeneralAlgorithms.Algorithms.Polygons;
    using GeneralAlgorithms.DataStructures.Common;
    using Interfaces.Core.Configuration;
    using Interfaces.Core.Configuration.EnergyData;
    using Interfaces.Core.ConfigurationSpaces;
    using Interfaces.Core.Constraints;
    using Interfaces.Core.Layouts;
    using MapGeneration.Core.Configurations;

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
            var distance = ComputeDistance(configuration, obstacle);
            
            energyData.MoveDistance = distance;
            energyData.Overlap = overlap;
            energyData.Energy += ComputeEnergy(overlap, distance);
            return overlap == 0 && distance == 0;
        }

        // perturbed node 不包含在内
        public bool UpdateEnergyData(TLayout layout, TNode perturbedNode, TConfiguration oldConfiguration,
            TConfiguration newConfiguration, TNode node, TConfiguration configuration, ref TEnergyData energyData)
        {
            var overlap = ComputeOverlap(configuration, obstacle);
            var distance = ComputeDistance(configuration, obstacle);

            energyData.MoveDistance = distance;
            energyData.Overlap = overlap;
            energyData.Energy += ComputeEnergy(overlap, distance);
            return overlap == 0 && distance == 0;
        }

        // node = perturbed node
        public bool UpdateEnergyData(TLayout oldLayout, TLayout newLayout, TNode node, ref TEnergyData energyData)
        {
            newLayout.GetConfiguration(node, out var configuration);
            var overlap = ComputeOverlap(configuration, obstacle);
            var distance = ComputeDistance(configuration, obstacle);

            energyData.MoveDistance = distance;
            energyData.Overlap = overlap;
            energyData.Energy += ComputeEnergy(overlap, distance);
            return overlap == 0 && distance == 0;
        }

        private int ComputeOverlap(TConfiguration configuration1, TConfiguration configuration2)
            => polygonOverlap.OverlapArea(configuration1.ShapeContainer, configuration1.Position, configuration2.ShapeContainer, configuration2.Position);

        private int ComputeDistance(TConfiguration configuration1, TConfiguration configuration2)
        {
            var a = configuration1.Shape.BoundingRectangle + configuration1.Position;
            var b = configuration2.Shape.BoundingRectangle + configuration2.Position;

            // 计算X轴的穿透深度
            int aWidth = a.B.X - a.A.X;
            int aCenterX = (a.A.X + a.B.X) / 2;
            int bWidth = b.B.X - b.A.X;
            int bCenterX = (b.B.X + b.A.X) / 2;
            int dx = Math.Abs(aCenterX - bCenterX);
            int xPenetration = (aWidth / 2 + bWidth / 2) - dx;

            // 计算Y轴的穿透深度
            int aHeight = a.B.Y - a.A.Y;
            int aCenterY = (a.A.Y + a.B.Y) / 2;
            int bHeight = b.B.Y - b.A.Y;
            int bCenterY = (b.B.Y + b.A.Y) / 2;
            int dy = Math.Abs(aCenterY - bCenterY);
            int yPenetration = (aHeight / 2 + bHeight / 2) - dy;

            // 如果任一轴没有重叠，返回0
            if (xPenetration <= 0 || yPenetration <= 0)
                return 0;

            // 返回较小的穿透深度作为最小挤出距离
            return Math.Min(xPenetration, yPenetration);
        }

        private float ComputeEnergy(int overlap, float distance)
            => (float)(Math.Exp(overlap / (energySigma * 625)) * Math.Exp(distance / (energySigma * 50)) - 1);
    }
}
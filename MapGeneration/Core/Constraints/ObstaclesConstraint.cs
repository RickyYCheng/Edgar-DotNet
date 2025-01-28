using MapGeneration.Interfaces.Core.Configuration.EnergyData;
using MapGeneration.Interfaces.Core.Configuration;
using MapGeneration.Interfaces.Core.Constraints;
using MapGeneration.Interfaces.Core.Layouts;
using System;
using System.Linq;
using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.Algorithms.Polygons;

namespace MapGeneration.Core.Constraints
{
    public class ObstaclesConstraint<TLayout, TNode, TConfiguration, TShapeContainer, TLayoutEnergyData> : ILayoutConstraint<TLayout, TNode, TLayoutEnergyData>
        where TLayout : ILayout<TNode, TConfiguration>
        where TConfiguration : IConfiguration<TShapeContainer>
        where TLayoutEnergyData : IEnergyData
    {
        // IPolygonOverlap<TShapeContainer> might be IntAtlas<GridPolygon>
        private readonly IPolygonOverlap<TShapeContainer> polygonOverlap;
        private readonly TShapeContainer[] contours;
        private readonly IntVector2[] contourPositions;
        private readonly float energySigma;
        public ObstaclesConstraint(IPolygonOverlap<TShapeContainer> polygonOverlap, int averageSize, TShapeContainer[] contours, IntVector2[] contourPositions)
        {
            this.polygonOverlap = polygonOverlap;
            this.contours = contours;
            this.contourPositions = contourPositions;
            energySigma = 10 * averageSize;
        }

        public bool ComputeLayoutEnergyData(TLayout layout, ref TLayoutEnergyData energyData)
        {
            var configs = layout.GetAllConfigurations();
            var polygons = configs.Select(e => e.ShapeContainer).ToList();
            var polygonPositions = configs.Select(e => e.Position).ToList();

            var area = 0;
            var distance = 0;
            for (int i = 0; i < polygons.Count; i++)
            {
                var polygon = polygons[i];
                var polyPos = polygonPositions[i];
                for (int j = 0; j < contours.Length; j++)
                {
                    var contour = contours[j];
                    var contPos = contourPositions[j];

                    area += polygonOverlap.OverlapArea(polygon, polyPos, contour, contPos);
                    distance += polygonOverlap.GetDistance(polygon, polyPos, contour, contPos);
                }
            }

            if (area == 0) 
                return true;

            energyData.Energy += ComputeEnergy(area, distance);
            return false;
        }

        public bool UpdateLayoutEnergyData(TLayout oldLayout, TLayout newLayout, TNode node, ref TLayoutEnergyData energyData)
        {
            return ComputeLayoutEnergyData(newLayout, ref energyData);
        }

        // same with basic constraint
        private float ComputeEnergy(int overlap, float distance)
            => (float)(Math.Exp(overlap / (energySigma * 625)) * Math.Exp(distance / (energySigma * 50)) - 1);
    }
}

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
        public ObstaclesConstraint(IPolygonOverlap<TShapeContainer> polygonOverlap, TShapeContainer[] contours, IntVector2[] contourPositions)
        {
            this.polygonOverlap = polygonOverlap;
            this.contours = contours;
            this.contourPositions = contourPositions;
        }

        public bool ComputeLayoutEnergyData(TLayout layout, ref TLayoutEnergyData energyData)
        {
            var configs = layout.GetAllConfigurations();
            var polygons = configs.Select(e => e.ShapeContainer).ToList();
            var polygonPositions = configs.Select(e => e.Position).ToList();

            var area = 0;
            for (int i = 0; i < polygons.Count; i++)
            {
                var polygon = polygons[i];
                var polyPos = polygonPositions[i];
                for (int j = 0; j < contours.Length; j++)
                {
                    var contour = contours[j];
                    var contPos = contourPositions[j];

                    var intersection = polygonOverlap.OverlapArea(polygon, polyPos, contour, contPos);
                    area += intersection;
                }
            }

            if (area == 0) 
                return true;

            energyData.Energy += (float)(Math.Pow(Math.E, area) - 1);
            return false;
        }

        public bool UpdateLayoutEnergyData(TLayout oldLayout, TLayout newLayout, TNode node, ref TLayoutEnergyData energyData)
        {
            return ComputeLayoutEnergyData(newLayout, ref energyData);
        }
    }
}

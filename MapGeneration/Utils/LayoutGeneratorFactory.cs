namespace MapGeneration.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.ChainDecompositions;
    using Core.Configurations;
    using Core.Configurations.EnergyData;
    using Core.ConfigurationSpaces;
    using Core.Constraints;
    using Core.Doors;
    using Core.GeneratorPlanners;
    using Core.LayoutConverters;
    using Core.LayoutEvolvers;
    using Core.LayoutGenerators;
    using Core.LayoutOperations;
    using Core.Layouts;
    using Core.MapDescriptions;
    using GeneralAlgorithms.Algorithms.Common;
    using GeneralAlgorithms.Algorithms.Polygons;
    using GeneralAlgorithms.DataStructures.Common;
    using GeneralAlgorithms.DataStructures.Polygons;
    using Interfaces.Core.MapLayouts;

    public static class LayoutGeneratorFactory
    {
        /// <summary>
        /// Gets a basic layout generator that should not be used to generated layouts with corridors.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        /// <returns></returns>
        public static ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IMapLayout<TNode>> GetDefaultChainBasedGenerator<TNode>()
        {
            var layoutGenerator = new ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IMapLayout<TNode>>();

            var chainDecomposition = new BreadthFirstChainDecomposition<int>();
            var configurationSpacesGenerator = new ConfigurationSpacesGenerator(new PolygonOverlap(), DoorHandler.DefaultHandler, new OrthogonalLineIntersection(), new GridPolygonUtils());
            var generatorPlanner = new BasicGeneratorPlanner<Layout<Configuration<EnergyData>, BasicEnergyData>>();

            layoutGenerator.SetChainDecompositionCreator(mapDescription => chainDecomposition);
            layoutGenerator.SetConfigurationSpacesCreator(mapDescription => configurationSpacesGenerator.Generate<TNode, Configuration<EnergyData>>(mapDescription));
            layoutGenerator.SetInitialLayoutCreator(mapDescription => new Layout<Configuration<EnergyData>, BasicEnergyData>(mapDescription.GetGraph()));
            layoutGenerator.SetGeneratorPlannerCreator(mapDescription => generatorPlanner);
            layoutGenerator.SetLayoutConverterCreator((mapDescription, configurationSpaces) => new BasicLayoutConverter<Layout<Configuration<EnergyData>, BasicEnergyData>, TNode, Configuration<EnergyData>>(mapDescription, configurationSpaces, configurationSpacesGenerator.LastIntAliasMapping));
            layoutGenerator.SetLayoutEvolverCreator((mapDescription, layoutOperations) => new SimulatedAnnealingEvolver<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>>(layoutOperations));
            layoutGenerator.SetLayoutOperationsCreator((mapDescription, configurationSpaces) =>
            {
                var layoutOperations = new LayoutOperationsWithConstraints<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IntAlias<GridPolygon>, EnergyData, BasicEnergyData>(configurationSpaces, configurationSpaces.GetAverageSize());

                var averageSize = configurationSpaces.GetAverageSize();

                layoutOperations.AddNodeConstraint(new BasicConstraint<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, EnergyData, IntAlias<GridPolygon>>(
                    new FastPolygonOverlap(),
                    averageSize,
                    configurationSpaces
                ));

                return layoutOperations;
            });

            return layoutGenerator;
        }

        /// <summary>
        /// Gets a generator that can work with corridors.
        /// </summary>
        /// <param name="offsets"></param>
        /// <param name="canTouch">Whether rooms can touch. Perfomance is decreased when set to false.</param>
        /// <returns></returns>
        public static ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IMapLayout<TNode>> GetChainBasedGeneratorWithCorridors<TNode>(List<int> offsets, bool canTouch = false)
        {
            var layoutGenerator = new ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IMapLayout<TNode>>();

            var chainDecomposition = new BreadthFirstChainDecomposition<int>();
            var configurationSpacesGenerator = new ConfigurationSpacesGenerator(new PolygonOverlap(), DoorHandler.DefaultHandler, new OrthogonalLineIntersection(), new GridPolygonUtils());
            var generatorPlanner = new BasicGeneratorPlanner<Layout<Configuration<CorridorsData>, BasicEnergyData>>();

            layoutGenerator.SetChainDecompositionCreator(mapDescription => new CorridorsChainDecomposition<int>(mapDescription, chainDecomposition));
            layoutGenerator.SetConfigurationSpacesCreator(mapDescription => configurationSpacesGenerator.Generate<TNode, Configuration<CorridorsData>>(mapDescription));
            layoutGenerator.SetInitialLayoutCreator(mapDescription => new Layout<Configuration<CorridorsData>, BasicEnergyData>(mapDescription.GetGraph()));
            layoutGenerator.SetGeneratorPlannerCreator(mapDescription => generatorPlanner);
            layoutGenerator.SetLayoutConverterCreator((mapDescription, configurationSpaces) => new BasicLayoutConverter<Layout<Configuration<CorridorsData>, BasicEnergyData>, TNode, Configuration<CorridorsData>>(mapDescription, configurationSpaces, configurationSpacesGenerator.LastIntAliasMapping));
            layoutGenerator.SetLayoutEvolverCreator((mapDescription, layoutOperations) => new SimulatedAnnealingEvolver<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>>(layoutOperations));
            layoutGenerator.SetLayoutOperationsCreator((mapDescription, configurationSpaces) =>
            {
                var corridorConfigurationSpaces = configurationSpacesGenerator.Generate<TNode, Configuration<CorridorsData>>(mapDescription, offsets);
                var layoutOperations = new LayoutOperationsWithCorridors<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IntAlias<GridPolygon>, CorridorsData, BasicEnergyData>(configurationSpaces, mapDescription, corridorConfigurationSpaces, configurationSpaces.GetAverageSize());
                var polygonOverlap = new FastPolygonOverlap();

                var averageSize = configurationSpaces.GetAverageSize();

                layoutOperations.AddNodeConstraint(new BasicConstraint<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, CorridorsData, IntAlias<GridPolygon>>(
                    polygonOverlap,
                    averageSize,
                    configurationSpaces
                ));

                layoutOperations.AddNodeConstraint(new CorridorConstraints<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, CorridorsData, IntAlias<GridPolygon>>(
                    mapDescription,
                    averageSize,
                    corridorConfigurationSpaces
                ));

                if (!canTouch)
                {
                    layoutOperations.AddNodeConstraint(new TouchingConstraints<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, CorridorsData, IntAlias<GridPolygon>>(
                        mapDescription,
                        polygonOverlap
                    ));
                }

                return layoutOperations;
            });

            return layoutGenerator;
        }

        /// <summary>
        /// Creates a chain-based layout generator with obstacle constraints for a given set of obstacle polygons and their positions.
        /// This overload accepts raw <see cref="GridPolygon"/> contours and automatically wraps them with <see cref="IntAlias{T}"/> identifiers.
        /// </summary>
        /// <typeparam name="TNode">The type of nodes used in the map description and layout</typeparam>
        /// <param name="contours">Array of obstacle polygons defining forbidden areas in the layout</param>
        /// <param name="positions">Array of positions corresponding to the obstacle polygons</param>
        /// <returns>
        /// A configured chain-based generator that considers:
        /// - Chain decomposition of the space
        /// - Configuration spaces for valid placements
        /// - Obstacle collision constraints
        /// - Simulated annealing optimization
        /// </returns>
        /// <remarks>
        /// The generator uses a breadth-first chain decomposition and includes:
        /// 1. Polygon overlap detection for node constraints
        /// 2. Obstacle position constraints
        /// 3. Automatic ID assignment for obstacles via IntAlias wrapper
        /// </remarks>
        public static ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IMapLayout<TNode>> GetChainBasedGeneratorWithObstacles<TNode>(GridPolygon[] contours, IntVector2[] positions)
        {
            var layoutGenerator = new ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IMapLayout<TNode>>();

            var chainDecomposition = new BreadthFirstChainDecomposition<int>();
            var configurationSpacesGenerator = new ConfigurationSpacesGenerator(new PolygonOverlap(), DoorHandler.DefaultHandler, new OrthogonalLineIntersection(), new GridPolygonUtils());
            var generatorPlanner = new BasicGeneratorPlanner<Layout<Configuration<EnergyData>, BasicEnergyData>>();

            layoutGenerator.SetChainDecompositionCreator(mapDescription => chainDecomposition);
            layoutGenerator.SetConfigurationSpacesCreator(mapDescription => configurationSpacesGenerator.Generate<TNode, Configuration<EnergyData>>(mapDescription));
            layoutGenerator.SetInitialLayoutCreator(mapDescription => new Layout<Configuration<EnergyData>, BasicEnergyData>(mapDescription.GetGraph()));
            layoutGenerator.SetGeneratorPlannerCreator(mapDescription => generatorPlanner);
            layoutGenerator.SetLayoutConverterCreator((mapDescription, configurationSpaces) => new BasicLayoutConverter<Layout<Configuration<EnergyData>, BasicEnergyData>, TNode, Configuration<EnergyData>>(mapDescription, configurationSpaces, configurationSpacesGenerator.LastIntAliasMapping));
            layoutGenerator.SetLayoutEvolverCreator((mapDescription, layoutOperations) => new SimulatedAnnealingEvolver<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>>(layoutOperations));
            layoutGenerator.SetLayoutOperationsCreator((mapDescription, configurationSpaces) =>
            {
                var layoutOperations = new LayoutOperationsWithConstraints<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IntAlias<GridPolygon>, EnergyData, BasicEnergyData>(configurationSpaces, configurationSpaces.GetAverageSize());

                var averageSize = configurationSpaces.GetAverageSize();

                var polygonOverlap = new FastPolygonOverlap();
                layoutOperations.AddNodeConstraint(new BasicConstraint<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, EnergyData, IntAlias<GridPolygon>>(
                    polygonOverlap,
                    averageSize,
                    configurationSpaces
                ));

                layoutOperations.AddLayoutConstraint(new ObstaclesConstraint<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IntAlias<GridPolygon>, BasicEnergyData>(
                    polygonOverlap,
                    averageSize,
                    contours.Select((e, i) => new IntAlias<GridPolygon>(i, e)).ToArray(),
                    positions
                ));

                return layoutOperations;
            });

            return layoutGenerator;
        }
    }
}
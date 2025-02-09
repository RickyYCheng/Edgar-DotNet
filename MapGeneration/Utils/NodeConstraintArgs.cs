namespace MapGeneration.Utils;

using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.Algorithms.Polygons;
using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Polygons;

using MapGeneration.Core.ChainDecompositions;
using MapGeneration.Core.Configurations;
using MapGeneration.Core.Configurations.EnergyData;
using MapGeneration.Core.ConfigurationSpaces;
using MapGeneration.Core.Constraints;
using MapGeneration.Core.Doors;
using MapGeneration.Core.GeneratorPlanners;
using MapGeneration.Core.LayoutConverters;
using MapGeneration.Core.LayoutEvolvers;
using MapGeneration.Core.LayoutGenerators;
using MapGeneration.Core.LayoutOperations;
using MapGeneration.Core.Layouts;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Interfaces.Core.MapLayouts;

using System;
using System.Collections.Generic;

public abstract record class NodeConstraintArgs<TNode> 
{
    record class CompoundConstraintArgs : NodeConstraintArgs<TNode>
    {
        public List<NodeConstraintArgs<TNode>> Args { get; } = [];
        public NodeConstraintArgs<TNode> Add(NodeConstraintArgs<TNode> args)
        {
            if (args is CompoundConstraintArgs compound)
                Args.AddRange(compound.Args);
            else Args.Add(args);
            return this;
        }
    }
    record class BasicConstraintArgs : NodeConstraintArgs<TNode>;
    record class BoundaryConstraintArgs : NodeConstraintArgs<TNode>
    {
        public BoundaryConstraintArgs(int width, int height, IntVector2 position)
        {
            Bound = GridPolygon.GetRectangle(width, height) + position;
        }
        public GridPolygon Bound { get; }
    }
    
    public static NodeConstraintArgs<TNode> Basic() => new BasicConstraintArgs();
    public static NodeConstraintArgs<TNode> Boundary(int width, int height, IntVector2 position=default) => new BoundaryConstraintArgs(width, height, position);

    public NodeConstraintArgs<TNode> WithBasic() => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Basic());
    public NodeConstraintArgs<TNode> WithBoundary(int width, int height, IntVector2 position=default) => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Boundary(width, height, position));

    public ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IMapLayout<TNode>> GetChainBasedGenerator()
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
            var mapping = mapDescription.GetRoomsMapping();

            var layoutOperations = new LayoutOperationsWithConstraints<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IntAlias<GridPolygon>, EnergyData, BasicEnergyData>(configurationSpaces, configurationSpaces.GetAverageSize());
            var polygonOverlap = new FastPolygonOverlap();
            var averageSize = configurationSpaces.GetAverageSize();

            int counter = -1;
            var compound = (CompoundConstraintArgs)(this is CompoundConstraintArgs ? this : new CompoundConstraintArgs().Add(this));
            foreach (var arg in compound.Args)
            {
                if (arg is CompoundConstraintArgs)
                    throw new InvalidOperationException("Fatal error! CompoundConstraintArgs should not be in CompoundConstraintArgs!");
                else if (arg is BasicConstraintArgs _)
                {
                    layoutOperations.AddNodeConstraint(new BasicConstraint<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, EnergyData, IntAlias<GridPolygon>>(
                        polygonOverlap,
                        averageSize,
                        configurationSpaces
                    ));
                }
                else if (arg is BoundaryConstraintArgs bound)
                {
                    layoutOperations.AddNodeConstraint(new BoundaryConstraint<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, EnergyData, IntAlias<GridPolygon>>(
                        polygonOverlap,
                        averageSize,
                        new Configuration<EnergyData>(new IntAlias<GridPolygon>(counter--, bound.Bound), IntVector2.Zero, new EnergyData())
                    ));
                }
                else
                {
                    throw new InvalidOperationException("Unhandled type of NodeConstraintArgs. ");
                }
            }

            return layoutOperations;
        });

        return layoutGenerator;
    }
    public ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IMapLayout<TNode>> GetChainBasedGenerator(List<int> offsets, bool canTouch = false)
    {
        var layoutGenerator = new ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IMapLayout<TNode>>();

        var chainDecomposition = new BreadthFirstChainDecomposition<int>();
        var configurationSpacesGenerator = new ConfigurationSpacesGenerator(new PolygonOverlap(), DoorHandler.DefaultHandler, new OrthogonalLineIntersection(), new GridPolygonUtils());
        var generatorPlanner = new BasicGeneratorPlanner<Layout<Configuration<CorridorsData>, BasicEnergyData>>();

        layoutGenerator.OnInitMapDescription += mapDescription =>
        {
            if (offsets != null && offsets.Count > 0 && mapDescription.IsWithCorridors is false)
                mapDescription.SetWithCorridors(true, offsets);
        };
        layoutGenerator.SetChainDecompositionCreator(mapDescription => new CorridorsChainDecomposition<int>(mapDescription, chainDecomposition));
        layoutGenerator.SetConfigurationSpacesCreator(mapDescription => configurationSpacesGenerator.Generate<TNode, Configuration<CorridorsData>>(mapDescription));
        layoutGenerator.SetInitialLayoutCreator(mapDescription => new Layout<Configuration<CorridorsData>, BasicEnergyData>(mapDescription.GetGraph()));
        layoutGenerator.SetGeneratorPlannerCreator(mapDescription => generatorPlanner);
        layoutGenerator.SetLayoutConverterCreator((mapDescription, configurationSpaces) => new BasicLayoutConverter<Layout<Configuration<CorridorsData>, BasicEnergyData>, TNode, Configuration<CorridorsData>>(mapDescription, configurationSpaces, configurationSpacesGenerator.LastIntAliasMapping));
        layoutGenerator.SetLayoutEvolverCreator((mapDescription, layoutOperations) => new SimulatedAnnealingEvolver<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>>(layoutOperations));
        layoutGenerator.SetLayoutOperationsCreator((mapDescription, configurationSpaces) =>
        {
            var mapping = mapDescription.GetRoomsMapping();

            var corridorConfigurationSpaces = configurationSpacesGenerator.Generate<TNode, Configuration<CorridorsData>>(mapDescription, offsets);
            var layoutOperations = new LayoutOperationsWithCorridors<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IntAlias<GridPolygon>, CorridorsData, BasicEnergyData>(configurationSpaces, mapDescription, corridorConfigurationSpaces, configurationSpaces.GetAverageSize());
            var polygonOverlap = new FastPolygonOverlap();

            var averageSize = configurationSpaces.GetAverageSize();

            int counter = -1;
            var compound = (CompoundConstraintArgs)(this is CompoundConstraintArgs ? this : new CompoundConstraintArgs().Add(this));
            foreach (var arg in compound.Args)
            {
                if (arg is CompoundConstraintArgs)
                    throw new InvalidOperationException("Fatal error! CompoundConstraintArgs should not be in CompoundConstraintArgs!");
                else if (arg is BasicConstraintArgs _)
                {
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
                }
                else if (arg is BoundaryConstraintArgs bound)
                {
                    layoutOperations.AddNodeConstraint(new BoundaryConstraint<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, CorridorsData, IntAlias<GridPolygon>>(
                        polygonOverlap,
                        averageSize,
                        new Configuration<CorridorsData>(new IntAlias<GridPolygon>(counter--, bound.Bound), IntVector2.Zero, new CorridorsData())
                    ));
                }
                else
                {
                    throw new InvalidOperationException("Unhandled type of NodeConstraintArgs. ");
                }
            }

            return layoutOperations;
        });

        return layoutGenerator;
    }
}

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

public abstract record class NodeConstraintArgs
{
    record class CompoundConstraintArgs : NodeConstraintArgs
    {
        public List<NodeConstraintArgs> Args { get; } = new List<NodeConstraintArgs>();
        public NodeConstraintArgs Add(NodeConstraintArgs args)
        {
            if (args is CompoundConstraintArgs compound)
                Args.AddRange(compound.Args);
            else Args.Add(args);
            return this;
        }
    }
    record class BasicConstraintArgs : NodeConstraintArgs;
    record class BoundaryConstraintArgs : NodeConstraintArgs
    {
        public BoundaryConstraintArgs(int width, int height, IntVector2 position)
        {
            Width = width;
            Height = height;
            Position = position;
        }

        public int Width { get; }
        public int Height { get; }
        public IntVector2 Position { get; }
    }
    
    public static NodeConstraintArgs Basic() => new BasicConstraintArgs();
    public static NodeConstraintArgs Boundary(int width, int height, IntVector2 position) => new BoundaryConstraintArgs(width, height, position);
    public static NodeConstraintArgs Boundary(int squareSideLength, IntVector2 position) => new BoundaryConstraintArgs(squareSideLength, squareSideLength, position);

    public NodeConstraintArgs WithBasic() => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Basic());
    public NodeConstraintArgs WithBoundary(int width, int height, IntVector2 position) => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Boundary(width, height, position));
    public NodeConstraintArgs WithBoundary(int squareSideLength, IntVector2 position) => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Boundary(squareSideLength, squareSideLength, position));

    public ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, IMapLayout<TNode>> GetChainBasedGenerator<TNode>()
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
            var polygonOverlap = new FastPolygonOverlap();
            var averageSize = configurationSpaces.GetAverageSize();

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
                        new Configuration<EnergyData>(new IntAlias<GridPolygon>(-1, GridPolygon.GetRectangle(bound.Width, bound.Height)), bound.Position, new EnergyData())
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
    public ChainBasedGenerator<MapDescription<TNode>, Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, IMapLayout<TNode>> GetChainBasedGenerator<TNode>(List<int> offsets, bool canTouch)
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
                        new Configuration<CorridorsData>(new IntAlias<GridPolygon>(-1, GridPolygon.GetRectangle(bound.Width, bound.Height)), bound.Position, new CorridorsData())
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

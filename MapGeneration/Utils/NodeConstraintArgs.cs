﻿namespace MapGeneration.Utils;

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
using MapGeneration.Core.Doors.DoorModes;
using MapGeneration.Core.GeneratorPlanners;
using MapGeneration.Core.LayoutConverters;
using MapGeneration.Core.LayoutEvolvers;
using MapGeneration.Core.LayoutGenerators;
using MapGeneration.Core.LayoutOperations;
using MapGeneration.Core.Layouts;
using MapGeneration.Core.MapDescriptions;
using MapGeneration.Interfaces.Core.Configuration.EnergyData;
using MapGeneration.Interfaces.Core.MapLayouts;
using MapGeneration.Interfaces.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

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
        public BoundaryConstraintArgs(int width, int height, IntVector2 position, (TNode node, SpecificPositionsMode door)[] doors)
        {
            Bound = GridPolygon.GetRectangle(width, height) + position;
            Doors = doors.Select(e =>
            {
                e.door.DoTranslate(position);
                return e;
            }).ToArray();
        }

        public int Width { get; }
        public int Height { get; }
        public GridPolygon Bound { get; }
        public (TNode node, SpecificPositionsMode door)[] Doors { get; }
    }
    
    public static NodeConstraintArgs<TNode> Basic() => new BasicConstraintArgs();
    public static NodeConstraintArgs<TNode> Boundary(int width, int height, IntVector2 position = default, (TNode node, SpecificPositionsMode door)[] doors = null) => new BoundaryConstraintArgs(width, height, position, doors);

    public NodeConstraintArgs<TNode> WithBasic() => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Basic());
    public NodeConstraintArgs<TNode> WithBoundary(int width, int height, IntVector2 position = default, (TNode node, SpecificPositionsMode door)[] doors = null) => ((CompoundConstraintArgs)(this is CompoundConstraintArgs _comp ? _comp : new CompoundConstraintArgs().Add(this))).Add(Boundary(width, height, position, doors));

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
                    Dictionary<int, Configuration<EnergyData>> configurations = null;
                    Dictionary<int, ConfigurationSpace> cspaces = null;
                    
                    if (bound.Doors is not null)
                    {
                        configurations = GetConfigurations<EnergyData>(bound.Bound, mapDescription, bound.Doors);
                        cspaces = GetConfigurationSpaces(mapDescription, configurations, bound.Doors);
                    }

                    layoutOperations.AddNodeConstraint(new BoundaryConstraint<Layout<Configuration<EnergyData>, BasicEnergyData>, int, Configuration<EnergyData>, EnergyData, IntAlias<GridPolygon>>(
                        polygonOverlap,
                        averageSize,
                        new Configuration<EnergyData>(new IntAlias<GridPolygon>(-1, bound.Bound), IntVector2.Zero, new EnergyData()),
                        configurations,
                        cspaces
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
                    Dictionary<int, Configuration<CorridorsData>> configurations = null;
                    Dictionary<int, ConfigurationSpace> cspaces = null;

                    if (bound.Doors is not null)
                    {
                        configurations = GetConfigurations<CorridorsData>(bound.Bound, mapDescription, bound.Doors);
                        cspaces = GetConfigurationSpaces(mapDescription, configurations, bound.Doors);
                    }

                    layoutOperations.AddNodeConstraint(new BoundaryConstraint<Layout<Configuration<CorridorsData>, BasicEnergyData>, int, Configuration<CorridorsData>, CorridorsData, IntAlias<GridPolygon>>(
                        polygonOverlap,
                        averageSize,
                        new Configuration<CorridorsData>(new IntAlias<GridPolygon>(-1, bound.Bound), IntVector2.Zero, new CorridorsData()),
                        configurations,
                        cspaces
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
    
    GridPolygon[] GetOuterBlocks(GridPolygon polygon, IEnumerable<OrthogonalLine> doors)
    {
        var lineIntersection = new OrthogonalLineIntersection();

        var polylines = polygon.GetLines();

        List<int> mapping = []; // XXX: avoid multiple doors on same line. 
        var counter = 0;
        foreach (var polyline in polylines)
        {
            foreach (var doorline in doors)
            {
                if (lineIntersection.TryGetIntersection(polyline, doorline, out var intersection) && intersection.Length > 0)
                    mapping.Add(counter);
            }
            counter++;
        }
        var result = mapping.Select(id =>
        {
            var polyline = polylines[id];
            // BUG: polyline.Length cannot be 1. WTF?
            var shiftedPolyline = polyline + polyline.Length * polyline.Rotate(-90).GetDirectionVector();
            return new GridPolygon([polyline.To, polyline.From, shiftedPolyline.From, shiftedPolyline.To]);
        }).ToArray();
        return result;
    }
    Dictionary<int, Configuration<TEnergyData>> GetConfigurations<TEnergyData>(GridPolygon bound, MapDescription<TNode> mapDescription, (TNode node, SpecificPositionsMode door)[] doors)
        where TEnergyData : IEnergyData, ISmartCloneable<TEnergyData>
    {
        int counter = -1;
        var mapping = mapDescription.GetRoomsMapping();
        var result = new Dictionary<int, Configuration<TEnergyData>>(doors.Length);

        var blocks = GetOuterBlocks(bound, doors.SelectMany(e => e.door.DoorPositions));
        var count = 0;
        foreach ((TNode node, SpecificPositionsMode doorLine) in doors)
        {
            var polygon = blocks[count++];
            var configuration = new Configuration<TEnergyData>(
                new IntAlias<GridPolygon>(counter, polygon),
                IntVector2.Zero,
                default
            );
            result.Add(mapping[node], configuration);
            --counter;
        }
        return result;
    }
    Dictionary<int, ConfigurationSpace> GetConfigurationSpaces<TEnergyData>(MapDescription<TNode> mapDescription, Dictionary<int, Configuration<TEnergyData>> configurations, (TNode node, SpecificPositionsMode door)[] doors)
        where TEnergyData : IEnergyData, ISmartCloneable<TEnergyData>
    {
        var cspaceGen = new ConfigurationSpacesGenerator(new PolygonOverlap(), DoorHandler.DefaultHandler, new OrthogonalLineIntersection(), new GridPolygonUtils());

        var mapping = mapDescription.GetRoomsMapping();
        var roomsShapes = mapDescription.GetRoomShapesForNodes();

        var result = new Dictionary<int, ConfigurationSpace>(doors.Length);

        foreach ((TNode node, SpecificPositionsMode line) in doors)
        {
            var fixedShape = configurations[mapping[node]].Shape;
            var fixedDoor = line;

            var roomShapes = roomsShapes[mapping[node]];
            foreach (var shape in roomShapes)
            {
                var roomDescription = shape.RoomDescription;
                var roomShape = roomDescription.Shape;
                var roomDoor = roomDescription.DoorsMode;

                var cspace = cspaceGen.GetConfigurationSpace(roomShape, roomDoor, fixedShape, fixedDoor);
                result.Add(mapping[node], cspace);
            }
        }
        return result;
    }
}

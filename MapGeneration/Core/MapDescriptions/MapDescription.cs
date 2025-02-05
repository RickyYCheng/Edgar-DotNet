﻿namespace MapGeneration.Core.MapDescriptions;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using GeneralAlgorithms.Algorithms.Common;
using GeneralAlgorithms.DataStructures.Common;
using GeneralAlgorithms.DataStructures.Graphs;

using Interfaces.Core.MapDescriptions;

/// <summary>
/// Basic map description that supports corridors.
/// </summary>
/// <typeparam name="TNode"></typeparam>
public class MapDescription<TNode> : IMapDescription<int>
{
    // XXX: should all be corridor shapes after deleting AddRoomShapes no-node version. 
    protected readonly List<RoomContainer> RoomShapes = [];
    protected readonly List<RoomContainer> CorridorShapes = [];
    protected readonly List<List<RoomContainer>> RoomShapesForNodes = [];
    protected readonly List<Tuple<TNode, TNode>> Passages = [];
    protected readonly Dictionary<TNode, int> Rooms = [];
    protected List<Transformation> DefaultTransformations;
    protected bool IsLocked;

    public MapDescription()
    {
        DefaultTransformations = TransformationHelper.GetAllTransforamtion().ToList();
    }

    /// <inheritdoc />
    public bool IsWithCorridors { get; protected set; }

    /// <inheritdoc />
    public List<int> CorridorsOffsets { get; protected set; }

    #region Room shapes

    /// <summary>
    /// Add a shape that will be used for corridor rooms.
    /// </summary>
    /// <param name="shape">Shape to add.</param>
    /// <param name="transformations">Allowed transformation of the shape.</param>
    /// <param name="probability">The probability of choosing a given shape.</param>
    /// <param name="normalizeProbabilities">Whether probabilities should be normalized after rotating shapes.</param>
    public virtual void AddCorridorShapes(RoomDescription shape, ICollection<Transformation> transformations = null, double probability = 1, bool normalizeProbabilities = true)
    {
        ThrowIfLocked();

        if (probability <= 0)
            throw new ArgumentException("Probability should be greater than zero", nameof(probability));

        if (RoomShapes.Any(x => shape.Equals(x.RoomDescription)))
            throw new InvalidOperationException("Every RoomDescription can be added at most once");

        if (transformations != null && transformations.Count == 0)
            throw new ArgumentException("There must always be at least one transformation available (e.g. identity)");

        var transformationsList = transformations == null ? null : new List<Transformation>(transformations);

        CorridorShapes.Add(new RoomContainer(shape, transformationsList, probability, normalizeProbabilities));
    }

    /// <summary>
    /// Add shapes that will be used for corridor rooms.
    /// </summary>
    /// <param name="shapes">Room shapes to add.</param>
    /// <param name="transformations">Allowed transformation of the shapes.</param>
    /// <param name="probability">The probability of choosing any of given shapes.</param>
    /// <param name="normalizeProbabilities">Whether probabilities should be normalized after rotating shapes.</param>
    public virtual void AddCorridorShapes(List<RoomDescription> shapes, ICollection<Transformation> transformations = null, double probability = 1, bool normalizeProbabilities = true)
    {
        ThrowIfLocked();

        shapes.ForEach(x => AddCorridorShapes(x, transformations, probability, normalizeProbabilities));
    }

    /// <summary>
    /// Add shapes that will be used when choosing shapes for a given node.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="shapes">Room shapes to add.</param>
    /// <param name="transformations">Allowed transformation of the shapes.</param>
    /// <param name="probability">The probability of choosing any of given shapes.</param>
    /// <param name="normalizeProbabilities">Whether probabilities should be normalized after rotating shapes.</param>
    public virtual void AddRoomShapes(TNode node, List<RoomDescription> shapes, ICollection<Transformation> transformations = null, double probability = 1, bool normalizeProbabilities = true)
    {
        ThrowIfLocked();

        shapes.ForEach(x => AddRoomShapes(node, x, transformations, probability, normalizeProbabilities));
    }

    /// <summary>
    /// Add shape that will be used when choosing shapes for a given node.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="shape">Shape to add.</param>
    /// <param name="transformations">Allowed transformation of the shape.</param>
    /// <param name="probability">The probability of choosing a given shape.</param>
    /// <param name="normalizeProbabilities">Whether probabilities should be normalized after rotating shapes.</param>
    public virtual void AddRoomShapes(TNode node, RoomDescription shape, ICollection<Transformation> transformations = null, double probability = 1, bool normalizeProbabilities = true)
    {
        ThrowIfLocked();

        if (probability <= 0)
            throw new ArgumentException("Probability should be greater than zero", nameof(probability));

        if (!Rooms.TryGetValue(node, out var alias))
            throw new InvalidOperationException("Room must be first added to add shapes");

        var roomShapesForNode = RoomShapesForNodes[alias];

        if (roomShapesForNode == null)
        {
            roomShapesForNode = [];
            RoomShapesForNodes[alias] = roomShapesForNode;
        }

        if (roomShapesForNode.Any(x => shape.Equals(x.RoomDescription)))
            throw new InvalidOperationException("Every RoomDescription can be added at most once");

        if (transformations != null && transformations.Count == 0)
            throw new ArgumentException("There must always be at least one transformation available (e.g. identity)");

        var transformationsList = transformations == null ? null : new List<Transformation>(transformations);

        roomShapesForNode.Add(new RoomContainer(shape, transformationsList, probability, normalizeProbabilities));
    }

    #endregion

    #region Rooms and passages

    /// <summary>
    /// Adds a given node to the map description.
    /// </summary>
    /// <remarks>
    /// Any room must be added with this method before assignig shapes to it.
    /// </remarks>
    /// <param name="node"></param>
    public virtual void AddRoom(TNode node)
    {
        ThrowIfLocked();

        if (Rooms.ContainsKey(node))
            throw new InvalidOperationException("Given node was already added");

        Rooms.Add(node, Rooms.Count);
        RoomShapesForNodes.Add(null);
    }

    /// <summary>
    /// Adds a passage to the map description.
    /// </summary>
    /// <remarks>
    /// Both nodes must be already present in the map description when adding a passage.
    /// </remarks>
    /// <param name="node1"></param>
    /// <param name="node2"></param>
    public virtual void AddPassage(TNode node1, TNode node2)
    {
        ThrowIfLocked();

        var passage = Tuple.Create(node1, node2);

        if (Passages.Any(x => x.Equals(passage)))
            throw new InvalidOperationException("Given passage was already added");

        Passages.Add(passage);
    }

    #endregion

    /// <inheritdoc />
    public virtual IGraph<int> GetGraph()
    {
        IsLocked = true;
        var vertices = Enumerable.Range(0, Rooms.Count);
        var graph = new UndirectedAdjacencyListGraph<int>();
        var corridorCounter = Rooms.Count;

        foreach (var vertex in vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var passage in Passages)
        {
            if (IsWithCorridors)
            {
                graph.AddVertex(corridorCounter);
                graph.AddEdge(Rooms[passage.Item1], corridorCounter);
                graph.AddEdge(corridorCounter, Rooms[passage.Item2]);
                corridorCounter++;
            }
            else
            {
                graph.AddEdge(Rooms[passage.Item1], Rooms[passage.Item2]);
            }
        }

        return graph;
    }

    /// <summary>
    /// Sets whether the map description should be with corridors.
    /// </summary>
    /// <param name="enable">Whether corridors should be enabled.</param>
    /// <param name="offsets">How far away should non-corridor neighbours be from each other.</param>
    public virtual void SetWithCorridors(bool enable, List<int> offsets = null)
    {
        ThrowIfLocked();

        if (enable && (offsets == null || offsets.Count == 0))
            throw new ArgumentException("At least one offset must be set if corridors are enabled");

        IsWithCorridors = enable;
        CorridorsOffsets = offsets;
    }

    /// <inheritdoc />
    public virtual bool IsCorridorRoom(int room)
    {
        return room >= Rooms.Count;
    }

    /// <inheritdoc />
    public virtual IGraph<int> GetGraphWithoutCorrridors()
    {
        // TODO: keep dry
        IsLocked = true;
        var vertices = Enumerable.Range(0, Rooms.Count);
        var graph = new UndirectedAdjacencyListGraph<int>();

        foreach (var vertex in vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var passage in Passages)
        {
            graph.AddEdge(Rooms[passage.Item1], Rooms[passage.Item2]);
        }

        return graph;
    }

    /// <summary>
    /// Sets default room transformations.
    /// </summary>
    /// <remarks>
    /// These transformations are used if they are not overriden for individual nodes.
    /// </remarks>
    /// <param name="transformations"></param>
    public virtual void SetDefaultTransformations(ICollection<Transformation> transformations)
    {
        if (transformations == null)
            throw new ArgumentException("Default transformations must not be null");

        if (transformations.Count == 0)
            throw new ArgumentException("There must always be at least one transformation available (e.g. identity)");

        DefaultTransformations = new List<Transformation>(transformations);
    }

    /// <summary>
    /// Gets default room transformations.
    /// </summary>
    /// <returns></returns>
    public virtual ReadOnlyCollection<Transformation> GetDefaultTransformations()
    {
        return DefaultTransformations.AsReadOnly();
    }

    /// <summary>
    /// Gets room shapes for rooms without their own shapes.
    /// </summary>
    /// <returns></returns>
    public virtual ReadOnlyCollection<RoomContainer> GetRoomShapes()
    {
        return RoomShapes.AsReadOnly();
    }

    /// <summary>
    /// Gets room shapes for corridors.
    /// </summary>
    /// <returns></returns>
    public virtual ReadOnlyCollection<RoomContainer> GetCorridorShapes()
    {
        return CorridorShapes.AsReadOnly();
    }

    /// <summary>
    /// Gets room shapes for nodes.
    /// </summary>
    /// <remarks>
    /// The collection is null for nodes that do not have their own shapes.
    /// </remarks>
    /// <returns></returns>
    public virtual ReadOnlyCollection<ReadOnlyCollection<RoomContainer>> GetRoomShapesForNodes()
    {
        return RoomShapesForNodes.Select(x => x?.AsReadOnly()).ToList().AsReadOnly();
    }

    /// <summary>
    /// Returns the mapping from original vertices to ints.
    /// </summary>
    /// <returns></returns>
    public virtual TwoWayDictionary<TNode, int> GetRoomsMapping()
    {
        IsLocked = true;
        var mapping = new TwoWayDictionary<TNode, int>();

        foreach (var pair in Rooms)
        {
            mapping.Add(pair.Key, pair.Value);
        }

        return mapping;
    }

    protected virtual void ThrowIfLocked()
    {
        if (IsLocked)
            throw new InvalidOperationException("MapDescription is locked");
    }

    public class RoomContainer
    {
        public RoomDescription RoomDescription { get; }

        public double Probability { get; }

        public bool NormalizeProbabilities { get; }

        public List<Transformation> Transformations { get; }

        public RoomContainer(RoomDescription roomDescription, List<Transformation> transformations, double probability, bool normalizeProbabilities)
        {
            RoomDescription = roomDescription;
            Probability = probability;
            NormalizeProbabilities = normalizeProbabilities;
            Transformations = transformations;
        }
    }
}
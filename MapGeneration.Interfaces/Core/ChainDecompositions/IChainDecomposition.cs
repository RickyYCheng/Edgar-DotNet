﻿namespace MapGeneration.Interfaces.Core.ChainDecompositions;
using System.Collections.Generic;

using GeneralAlgorithms.DataStructures.Graphs;

/// <summary>
/// Represents an algorithm that can decompose graph into disjunt chains covering all vertices.
/// </summary>
/// <typeparam name="TNode"></typeparam>
public interface IChainDecomposition<TNode>
{
    List<List<TNode>> GetChains(IGraph<TNode> graph);
}
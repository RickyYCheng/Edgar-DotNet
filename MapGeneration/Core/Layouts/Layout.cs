﻿namespace MapGeneration.Core.Layouts;

using System.Collections.Generic;

using GeneralAlgorithms.DataStructures.Graphs;

using Interfaces.Core;
using Interfaces.Core.Layouts;
using Interfaces.Utils;

/// <inheritdoc cref="ILayout{TNode,TConfiguration}" />
/// <summary>
/// Basic layout implementation.
/// </summary>
public class Layout<TConfiguration, TLayoutEnergyData> : IEnergyLayout<int, TConfiguration, TLayoutEnergyData>, ISmartCloneable<Layout<TConfiguration, TLayoutEnergyData>>
    where TConfiguration : ISmartCloneable<TConfiguration>
    where TLayoutEnergyData : ISmartCloneable<TLayoutEnergyData>
{
    private readonly TConfiguration[] vertices;
    private readonly bool[] hasValue;

    /// <inheritdoc />
    public TLayoutEnergyData EnergyData { get; set; }

    /// <inheritdoc />
    public IGraph<int> Graph { get; }

    /// <summary>
    /// Construct a layout with a given graph and no configurations.
    /// </summary>
    /// <param name="graph"></param>
    public Layout(IGraph<int> graph)
    {
        Graph = graph;
        vertices = new TConfiguration[Graph.VerticesCount];
        hasValue = new bool[Graph.VerticesCount];
    }

    /// <inheritdoc />
    public bool GetConfiguration(int node, out TConfiguration configuration)
    {
        if (hasValue[node])
        {
            configuration = vertices[node];
            return true;
        }

        configuration = default(TConfiguration);
        return false;
    }

    /// <inheritdoc />
    public void SetConfiguration(int node, TConfiguration configuration)
    {
        vertices[node] = configuration;
        hasValue[node] = true;
    }

    /// <inheritdoc />
    public void RemoveConfiguration(int node)
    {
        vertices[node] = default(TConfiguration);
        hasValue[node] = false;
    }

    /// <inheritdoc />
    public IEnumerable<TConfiguration> GetAllConfigurations()
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            if (hasValue[i])
            {
                yield return vertices[i];
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Smart clones all configurations.
    /// Graph is not smart cloned.
    /// </summary>
    /// <returns></returns>
    public Layout<TConfiguration, TLayoutEnergyData> SmartClone()
    {
        var layout = new Layout<TConfiguration, TLayoutEnergyData>(Graph);

        for (var i = 0; i < vertices.Length; i++)
        {
            var configuration = vertices[i];

            if (hasValue[i])
            {
                layout.vertices[i] = configuration.SmartClone();
                layout.hasValue[i] = true;
            }
        }

        layout.EnergyData = EnergyData.SmartClone();

        return layout;
    }
}
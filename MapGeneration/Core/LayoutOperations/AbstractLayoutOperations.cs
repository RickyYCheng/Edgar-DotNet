﻿namespace MapGeneration.Core.LayoutOperations;

using System;
using System.Collections.Generic;
using System.Linq;

using ConfigurationSpaces;

using GeneralAlgorithms.DataStructures.Common;

using Interfaces.Core.Configuration;
using Interfaces.Core.ConfigurationSpaces;
using Interfaces.Core.LayoutOperations;
using Interfaces.Core.Layouts;
using Interfaces.Utils;

using Utils;

/// <inheritdoc cref="IChainBasedLayoutOperations{TLayout,TNode}" />
/// <summary>
/// Base class for layout operations.
/// </summary>
public abstract class AbstractLayoutOperations<TLayout, TNode, TConfiguration, TShapeContainer> : IChainBasedLayoutOperations<TLayout, TNode>, IRandomInjectable
    where TLayout : ILayout<TNode, TConfiguration>, ISmartCloneable<TLayout>
    where TConfiguration : IMutableConfiguration<TShapeContainer>, ISmartCloneable<TConfiguration>
{
    protected readonly IConfigurationSpaces<TNode, TShapeContainer, TConfiguration, ConfigurationSpace> ConfigurationSpaces;
    protected Random Random;
    protected float ShapePerturbChance = 0.4f;
    protected float DifferenceFromAverageScale = 0.4f;
    protected int AverageSize;

    protected Dictionary<TNode, TConfiguration> shiftingNodesWhenPerturb = [];

    protected AbstractLayoutOperations(IConfigurationSpaces<TNode, TShapeContainer, TConfiguration, ConfigurationSpace> configurationSpaces, int averageSize)
    {
        ConfigurationSpaces = configurationSpaces;
        AverageSize = averageSize;
    }

    /// <inheritdoc />
    public virtual void InjectRandomGenerator(Random random)
    {
        Random = random;
    }

    /// <inheritdoc />
    /// <summary>
    /// Perturbs a node by getting random shapes from configurations spaces until a
    /// different shape is found.
    /// </summary>
    /// <param name="layout"></param>
    /// <param name="node"></param>
    /// <param name="updateLayout"></param>
    public virtual void PerturbShape(TLayout layout, TNode node, bool updateLayout)
    {
        layout.GetConfiguration(node, out var configuration);

        // Return the current layout if a given node cannot be shape-perturbed
        if (!ConfigurationSpaces.CanPerturbShape(node))
            return;

        TShapeContainer shape;
        do
        {
            shape = ConfigurationSpaces.GetRandomShape(node);
        }
        while (ReferenceEquals(shape, configuration.Shape));

        var newConfiguration = configuration.SmartClone();
        newConfiguration.ShapeContainer = shape;

        if (updateLayout)
        {
            UpdateLayout(layout, node, newConfiguration);
            return;
        }

        layout.SetConfiguration(node, newConfiguration);
    }

    /// <inheritdoc />
    public virtual void PerturbShape(TLayout layout, IList<TNode> chain, bool updateLayout)
    {
        var canBePerturbed = chain.Where(x => ConfigurationSpaces.CanPerturbShape(x)).ToList();

        if (canBePerturbed.Count == 0)
            return;

        PerturbShape(layout, canBePerturbed.GetRandom(Random), updateLayout);
    }

    public void AddShiftingPerturbationNode(TNode node, TConfiguration configuration)
    {
        shiftingNodesWhenPerturb.Add(node, configuration);
    }

    protected void PerturbPositionByShifting(TLayout layout, TNode node, bool updateLayout)
    {
        if (!layout.GetConfiguration(node, out var mainConfiguration))
            throw new InvalidOperationException();

        var newConfiguration = mainConfiguration.SmartClone();
        var dir = ComputeDistance(newConfiguration, shiftingNodesWhenPerturb[node]);
        newConfiguration.Position += dir;

        if (updateLayout)
        {
            UpdateLayout(layout, node, newConfiguration);
            return;
        }

        layout.SetConfiguration(node, newConfiguration);

        static IntVector2 ComputeDistance(TConfiguration configuration, TConfiguration boundary)
        {
            var counter = configuration.Shape.BoundingRectangle + configuration.Position;
            var hole = boundary.Shape.BoundingRectangle + boundary.Position;

            // 计算x轴方向的移动距离
            int aX = hole.A.X;
            int bX = hole.B.X - counter.Width;
            int x0 = counter.A.X;

            int dx_neg = Math.Max(aX - x0, 0);    // 当前位置到左侧边界的过量部分
            int dx_pos = Math.Max(x0 - bX, 0);    // 当前位置到右侧边界的过量部分
            int moveX = dx_neg - dx_pos;          // 计算最终的x轴方向移动量（包含方向）

            // 计算y轴方向的移动距离
            int aY = hole.A.Y;
            int bY = hole.B.Y - counter.Height;
            int y0 = counter.A.Y;

            int dy_neg = Math.Max(aY - y0, 0);    // 当前位置到上侧边界的过量部分
            int dy_pos = Math.Max(y0 - bY, 0);    // 当前位置到下侧边界的过量部分
            int moveY = dy_neg - dy_pos;          // 计算最终的y轴方向移动量（包含方向）

            return new IntVector2(moveX, moveY);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Pertubs a position of a given node by getting a random point from a maximum
    /// intersection of configuration space of already laid out neighbours. TODO: is "laid out" ok?
    /// </summary>
    public virtual void PerturbPosition(TLayout layout, TNode node, bool updateLayout)
    {
        if (shiftingNodesWhenPerturb.ContainsKey(node))
        {
            PerturbPositionByShifting(layout, node, updateLayout);
            return;
        }

        var configurations = new List<TConfiguration>();

        foreach (var neighbour in layout.Graph.GetNeighbours(node))
        {
            if (layout.GetConfiguration(neighbour, out var configuration))
            {
                configurations.Add(configuration);
            }
        }

        if (!layout.GetConfiguration(node, out var mainConfiguration))
            throw new InvalidOperationException();

        var newPosition = ConfigurationSpaces.GetRandomIntersectionPoint(mainConfiguration, configurations, out var configurationsSatisfied);

        // If zero configurations were satisfied, that means that the current shape was not compatible
        // with any of its neighbours so we perturb shape instead.
        if (configurationsSatisfied == 0)
        {
            PerturbShape(layout, node, updateLayout);
            return;
        }

        var newConfiguration = mainConfiguration.SmartClone();
        newConfiguration.Position = newPosition;

        if (updateLayout)
        {
            UpdateLayout(layout, node, newConfiguration);
            return;
        }

        layout.SetConfiguration(node, newConfiguration);
    }

    /// <inheritdoc />
    public virtual void PerturbPosition(TLayout layout, IList<TNode> chain, bool updateLayout)
    {
        // TODO: check what would happen if only invalid nodes could be perturbed
        var canBePerturbed = chain.ToList();

        if (canBePerturbed.Count == 0)
            return;

        PerturbPosition(layout, canBePerturbed.GetRandom(Random), updateLayout);
    }

    /// <inheritdoc />
    /// <summary>
    /// Perturbs a given layout by first choosing whether to perturb a shape or a positions
    /// and than calling corresponding methods.
    /// </summary>
    public virtual void PerturbLayout(TLayout layout, IList<TNode> chain, bool updateLayout)
    {
        if (Random.NextDouble() <= ShapePerturbChance)
        {
            PerturbShape(layout, chain, updateLayout);
        }
        else
        {
            PerturbPosition(layout, chain, updateLayout);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Adds a given chain by greedily adding nodes one by one.
    /// </summary>
    public virtual void AddChain(TLayout layout, IList<TNode> chain, bool updateLayout)
    {
        foreach (var node in chain)
        {
            AddNodeGreedily(layout, node);
        }

        if (updateLayout)
        {
            UpdateLayout(layout);
        }
    }

    /// <inheritdoc />
    public virtual bool AreDifferentEnough(TLayout layout1, TLayout layout2)
    {
        return AreDifferentEnough(layout1, layout2, layout1.Graph.Vertices.ToList());
    }

    /// <inheritdoc />
    /// <summary>
    /// Checks if two layouts are different enough by comparing positions of corresponding nodes.
    /// </summary>
    public virtual bool AreDifferentEnough(TLayout layout1, TLayout layout2, IList<TNode> chain)
    {
        // TODO: make better
        var diff = 0d;

        foreach (var node in chain)
        {
            if (layout1.GetConfiguration(node, out var c1) && layout2.GetConfiguration(node, out var c2))
            {
                diff += (float)(Math.Pow(
                    5 * IntVector2.ManhattanDistance(c1.Shape.BoundingRectangle.Center + c1.Position,
                    c2.Shape.BoundingRectangle.Center + c2.Position) / (float)AverageSize, 2) * (ReferenceEquals(c1.Shape, c2.Shape) ? 1 : 4));
            }
        }

        diff /= (chain.Count());

        return DifferenceFromAverageScale * diff >= 1;
    }

    /// <inheritdoc />
    public abstract bool IsLayoutValid(TLayout layout);

    /// <inheritdoc />
    public abstract bool IsLayoutValid(TLayout layout, IList<TNode> chain);

    /// <inheritdoc />
    public abstract float GetEnergy(TLayout layout);

    /// <inheritdoc />
    public abstract void UpdateLayout(TLayout layout);

    /// <inheritdoc />
    public abstract void AddNodeGreedily(TLayout layout, TNode node);

    /// <summary>
    /// Updates energies after perturbing a given node.
    /// </summary>
    /// <remarks>
    /// This method is responsible for modifying the layout by setting the configuration
    /// to the perturbed node.
    /// </remarks>
    /// <param name="layout">Original layout.</param>
    /// <param name="perturbedNode">Node that was perturbed.</param>
    /// <param name="configuration">New configuration of the perturbed node.</param>
    protected abstract void UpdateLayout(TLayout layout, TNode perturbedNode, TConfiguration configuration);
}
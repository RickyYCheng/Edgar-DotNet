# COY Map Generation: A Recursive Map Generator Based on Chain Decomposition and Graph-Constrained Rooms Using the COY Algorithm

## Introduction

COY Map Generation is a map generator based on the COY algorithm, supporting recursive chain decomposition and graph-constrained room generation. It not only generates complex 2D maps but also implements boundary constraints, enabling partial generation and parallel generation based on it. This makes it particularly suitable for game development scenarios that require the generation of complex maps.

## COY Algorithm

COY is a joint name derived from the names of the following three main contributors:
- **Ma, Chongyang**
- **Ondřej Nepožitek**
- **Cheng, Yinghao**

### 1. [LevelSyn](https://github.com/chongyangma/LevelSyn) Phase

**Ma, Chongyang**'s thesis and repository reveal the original phase of the COY algorithm. The algorithm is divided into the following three steps:
1. **Configuration Space**: Configures the space for clockwise simple convex polygons.
2. **Recursive Divide and Conquer**: Recursively decomposes the graph into chains.
3. **Simulated Annealing**: Optimizes non-convex functions using simulated annealing.

### 2. [Edgar-DotNet](https://github.com/OndrejNepozitek/Edgar-DotNet) (v1.0) Phase

**Ondřej Nepožitek** transformed **Chongyang**'s algorithm into an Int32-based Grid coordinate system using Manhattan distance in his thesis, making it more suitable for TileMap systems. His main contributions include:
1. **C# Implementation**: Implemented **Chongyang**'s algorithm in C# using Int32 and Grid System.
2. **Mathematical Optimization**: Made some mathematical optimizations to speed up the generation process.
3. **Constraint System**: Configured a constraint system, implementing basic room constraints and corridor constraints as described in **Chongyang**'s thesis.

> **Note**: The Constraint feature is only available in Edgar-DotNet 1.0. The refactored 2.0 version seems to have encountered difficulties and has not been updated for a long time. **Ondřej** is now mainly working on the Edgar-Unity plugin. You can find his contact information on the respective project website.

### 3. COY Map Generation Phase

This phase has completed the transition from Level Generation to implementing recursive divide-and-conquer-based Map Generation. It includes the following parts:
1. **Fixed Constraint System**: Checked out **Ondřej**'s project to v1.0.6 and fixed a subtle but significant bug in the constraint system, allowing it to function correctly.
2. **Boundary Constraints**: Implemented boundary constraints with boundary doors based on the fixed constraint system. This was achieved by converting boundaries (counterclockwise hole polygons) into multiple clockwise contour polygons to complete the configuration space calculation for holes and contours. Although this "configuration space" is not mathematically correct (it should be a superset of the correct configuration space), the correct constraints were achieved through two-step constraints: boundary non-overlap constraints and boundary doors connection constraints.

> **The purpose of this structure is**:
> 1. The original generation did not include these constraints, so it could only generate independent Levels that were not connected to each other;
> 2. Based on my work, rooms within partial boundaries can be regenerated while maintaining connections between boundaries;
> 3. Users can even generate a bunch of large rooms and then independently and parallelly regenerate a bunch of small rooms within each large room.

This is why I refer to the previous work as Level Generation and this software as Map Generation.

## Quick Start

### Installation
1. Install the dll via nuget;
2. Install the dll or source code via paket;
3. Clone the source code.

### Sample Code
Check out the code in the playground project.

## Roadmap

### v1.0.7 Version
1. Visualize results in C# Interactive Notebook
2. Write more unit tests

### Future Plans
1. Godot Assert Lib 
2. Godot Assert Marketplace

# SierraEngine.NET

<h1 align="center">Grid Pathfinding 2D</h1>
<h6 align="center">By: <a href="https://nikichatv.com/Website/N-Studios.html">N-Studios</a></h6>

<br>
<p align="">A <strong>free-to-use</strong> open source project. Built using the <strong>A* pathfinding algorithm</strong>, on C#, it makes it incredibly easy to set up grid-based pathfinding in a Unity 2D workspace. You can have a look at or build on top of the scripts inside the /Scripts folder. A setup tutorial is listed below and the whole Examples.cs is described with comments on almost every line. Feel free to use in <strong>any kind of projects</strong>. Credit is not required but is appreciated.</p>

<br>

##  ⚙️  Setup

<p>At the very top of your code make sure to reference the AStar namespace by doing:</p>

```c#
using Ultimate.Algorithms.AStar;
```

<br>
<p>Add some variables to make the tweaking of the behavior of the pathfinding easy. Here's an example:</p>

```c#
public int startNodeIndex = 0; // The index of the start node in the grid.nodes list
public int endNodeIndex = 1; // The index of the end node in the grid.nodes list
public float nodeUnwalkablePercentange = 20f;     // Chance of a node being unwalkable
public Vector2Int size; // Size of the grid - X means width and Y means height
```

<br>
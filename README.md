# DijkstraFast

Fast Dijkstra shortest-path search for .NET. Nodes are plain integers, edges are stored in flat arrays, and the package has no dependencies.

```
dotnet add package DijkstraFast
```

It targets .NET Standard 2.0, so it runs on .NET Framework 4.6.1 and later, .NET Core 2.0 and later, and every modern .NET.

## Quick start

```csharp
using DijkstraFast;

// Nodes are numbered 0 to nodeCount - 1. Edges are directed.
var graph = new Graph(nodeCount: 4, new[]
{
    new Edge(0, 1, 1.0),
    new Edge(1, 2, 2.0),
    new Edge(0, 2, 5.0),
    new Edge(2, 3, 1.0),
});

PathResult path = graph.FindShortestPath(source: 0, target: 3);

path.Found;     // true
path.Distance;  // 4
path.Nodes;     // [0, 1, 2, 3]
```

When there is no path, `Found` is `false`, `Distance` is `double.PositiveInfinity` and `Nodes` is empty.

## Finding the nearest node that matches a condition

`FindNearest` stops at the first matching node it reaches. Use it for questions like "which depot is closest?" when you don't know the destination in advance.

```csharp
var depots = new HashSet<int> { 2, 3 };

PathResult nearest = graph.FindNearest(source: 0, isTarget: depots.Contains);

nearest.Target;    // 2
nearest.Distance;  // 3
nearest.Nodes;     // [0, 1, 2]
```

The source node is checked first, so it is returned if it matches.

## Paths from one node to every node

```csharp
ShortestPathTree tree = graph.FindShortestPathsFrom(source: 0);

tree.DistanceTo(3);    // 4
tree.PathTo(2).Nodes;  // [0, 1, 2]
tree.IsReachable(3);   // true
```

The search runs once. After that, `DistanceTo` is an array lookup and `PathTo` takes time proportional to the path's length.

## Paths into a node

`graph.Reverse()` returns the same graph with every edge flipped. Searching the reversed graph from a node finds the cheapest path from every other node to it. The paths come back in reverse order, from the destination to the start.

## Graphs you don't want to build up front

Implement `IGraph` to search a graph whose edges are computed on demand, such as a grid:

```csharp
// '#' is a wall. Each cell is a node: index = row * width + column.
sealed class Grid : IGraph
{
    private readonly string[] _rows;

    public Grid(params string[] rows)
    {
        _rows = rows;
    }

    private int Width => _rows[0].Length;

    public int NodeCount => _rows.Length * Width;

    public IEnumerable<Edge> GetOutgoingEdges(int node)
    {
        int row = node / Width, column = node % Width;

        foreach (var (r, c) in new[] { (row - 1, column), (row + 1, column), (row, column - 1), (row, column + 1) })
        {
            if (r >= 0 && r < _rows.Length && c >= 0 && c < Width && _rows[r][c] != '#')
            {
                yield return new Edge(node, r * Width + c, 1);
            }
        }
    }
}

var grid = new Grid(
    "..#.",
    "..#.",
    "....");

grid.FindShortestPath(source: 0, target: 3).Distance;  // 7, going around the wall
```

`Graph` is faster to search than a custom `IGraph`, so prefer it when you already have the full edge list.

## Rules and guarantees

- **Nodes** are numbered from `0` to `NodeCount - 1`.
- **Edges are directed.** For a two-way road, add an edge in each direction.
- **Costs must be zero or positive.** Dijkstra's algorithm gives wrong answers with negative costs, so `Edge` rejects them, and it rejects `NaN` too. An edge with a cost of `double.PositiveInfinity` is allowed but is never used.
- **Parallel edges and self-loops** are allowed. The search uses the cheapest edge.
- **Ties:** if several paths have the same lowest cost, any one of them may be returned.
- **Thread safety:** `Graph` is immutable, so many threads can search the same instance at once.
- **Cost:** a search takes O((V + E) log V) time and O(V) memory. `FindShortestPath` and `FindNearest` stop as soon as they reach a target, so a nearby target is found without exploring the whole graph.

## Building and testing

```
dotnet test
```

The tests run on .NET 10 and use xUnit and Shouldly.

## License

[MIT](LICENSE)

# DijkstraFast

Fast Dijkstra and A* shortest-path search for .NET. Nodes are plain integers, edges are stored in flat arrays, and the package has no dependencies.

```
dotnet add package DijkstraFast
```

It targets .NET Standard 2.0, so it runs on .NET Framework 4.6.1 and later, .NET Core 2.0 and later, and every modern .NET.

## Performance at a glance

One route between two random intersections on a synthetic road map with **1 million intersections**, on an Intel Core i9-13900K with .NET 10:

| Library and search | Time per 1M route | Allocated per 1M route | Graph in memory |
|---|---:|---:|---:|
| **DijkstraFast: A\*** | 46 ms | 19 MB | 62 MB |
| **DijkstraFast: Dijkstra** | 77 ms | 11.5 MB | 62 MB |
| QuikGraph 2.5.0: A* | 298 ms | 251 MB | 266 MB |
| QuikGraph 2.5.0: Dijkstra | 478 ms | 252 MB | 266 MB |
| Dijkstra.NET 1.2.1: Dijkstra | 617 ms | 475 MB | 514 MB |

More map sizes, how this is measured and the caveats are under [Performance](#performance).

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

## Faster searches with A*

If you can estimate how far each node is from the target, pass that estimate as a `heuristic`. The search then explores toward the target first and usually visits far fewer nodes.

```csharp
// On a grid where every move costs 1, the Manhattan distance never overestimates.
const int width = 4, target = 3;

double ManhattanToTarget(int node)
{
    return Math.Abs(node / width - target / width) + Math.Abs(node % width - target % width);
}

grid.FindShortestPath(source: 0, target: target, heuristic: ManhattanToTarget).Distance;  // 7
```

On a road map, use the straight-line distance to the target, in the same units as the edge costs.

The result is still the cheapest path as long as the estimate never exceeds the true remaining cost. If it can, there is no guarantee.

### Trading accuracy for speed

`heuristicWeight` multiplies the estimate. Above 1, the search heads for the target more aggressively and visits even fewer nodes. In exchange, the path it finds may cost up to `heuristicWeight` times as much as the cheapest one.

```csharp
// The path costs at most 1.5 times as much as the cheapest one: here, between 7 and 10.5.
grid.FindShortestPath(source: 0, target: target, heuristic: ManhattanToTarget, heuristicWeight: 1.5);
```

The heuristic is called at most once per node, so it can do real work, such as a great-circle distance calculation.

## Rules and guarantees

- **Nodes** are numbered from `0` to `NodeCount - 1`.
- **Edges are directed.** For a two-way road, add an edge in each direction.
- **Costs must be zero or positive.** Dijkstra's algorithm gives wrong answers with negative costs, so `Edge` rejects them, and it rejects `NaN` too. An edge with a cost of `double.PositiveInfinity` is allowed but is never used.
- **Parallel edges and self-loops** are allowed. The search uses the cheapest edge.
- **Ties:** if several paths have the same lowest cost, any one of them may be returned.
- **Thread safety:** `Graph` is immutable, so many threads can search the same instance at once.
- **Cost:** a search takes O((V + E) log V) time and O(V) memory. `FindShortestPath` and `FindNearest` stop as soon as they reach a target, so a nearby target is found without exploring the whole graph. A* with a good heuristic usually explores far less than that.

## Performance

The `benchmarks` project measures route searches with BenchmarkDotNet on synthetic road maps: intersections on a jittered grid, joined by two-way roads 0-30% longer than the straight line, with 5% of roads missing. Times are per route between two random intersections, on an Intel Core i9-13900K with .NET 10.

| Intersections | Directed edges | Dijkstra | A* | Weighted A* (1.08) | Hand-written, .NET `PriorityQueue` |
|---:|---:|---:|---:|---:|---:|
| 10,000 | 38 thousand | 375 μs | 176 μs | 160 μs | 415 μs |
| 100,000 | 378 thousand | 5.5 ms | 2.7 ms | 2.5 ms | 6.6 ms |
| 500,000 | 1.9 million | 29 ms | 14 ms | 12 ms | 39 ms |
| 1,000,000 | 3.8 million | 72 ms | 45 ms | 38 ms | 97 ms |
| 10,000,000 | 38 million | 1.0 s | 0.51 s | 0.45 s | 1.5 s |

- **Dijkstra, A\* and weighted A\*** are this library searching a `Graph`. A* uses the straight-line distance to the target as its heuristic, and takes 47-63% of Dijkstra's time.
- **The last column is not this library.** It is a hand-written Dijkstra using .NET's `PriorityQueue`, the kind you might write yourself, included for comparison ([TextbookDijkstra.cs](benchmarks/DijkstraFast.Benchmarks/Baselines/TextbookDijkstra.cs)). It takes 1.1 times as long as this library on the smallest map and 1.5 times as long on the largest.
- **A custom `IGraph`** is 1.3-1.6 times slower to search than a `Graph` with the same edges, measured on the two smaller maps.

For scale: Kyiv's streets in OpenStreetMap make a graph of about 170,000 nodes and 340,000 directed edges when every road point is a node, comparable to the 100,000-intersection map. The 10-million map is close in size to the road network of Western Europe used in routing research, which has 18 million nodes and 42 million edges. The whole USA has about 24 million nodes and 58 million edges.

On country-sized maps a route takes about half a second even with A*. That's fine for occasional queries, but real-time routing at that scale needs preprocessing techniques such as contraction hierarchies, which this library doesn't provide.

### Compared with other .NET libraries

The same routes searched with the two most-downloaded .NET libraries that offer Dijkstra: [QuikGraph](https://www.nuget.org/packages/QuikGraph) 2.5.0 and [Dijkstra.NET](https://www.nuget.org/packages/Dijkstra.NET) 1.2.1. Each is used through its standard graph type: QuikGraph's `AdjacencyGraph` with the road length stored on each edge, and Dijkstra.NET's `Graph.Simple.Graph`. Times are per route, and the figures in brackets show how much longer each search takes than DijkstraFast's Dijkstra.

| Intersections | DijkstraFast: Dijkstra | DijkstraFast: A* | QuikGraph: Dijkstra | QuikGraph: A* | QuikGraph: `ShortestPathsDijkstra` | Dijkstra.NET |
|---:|---:|---:|---:|---:|---:|---:|
| 10,000 | 373 μs | 180 μs | 2.6 ms (6.9×) | 1.4 ms (3.7×) | 4.0 ms (10.7×) | 3.0 ms (8.0×) |
| 100,000 | 5.6 ms | 3.0 ms | 36 ms (6.4×) | 18 ms (3.2×) | 56 ms (9.9×) | 43 ms (7.7×) |
| 1,000,000 | 77 ms | 46 ms | 478 ms (6.2×) | 298 ms (3.9×) | 716 ms (9.3×) | 617 ms (8.0×) |

Memory for the same maps. "Graph" is what the built graph keeps alive, and "per route" is what one Dijkstra search allocates:

| Intersections | DijkstraFast: graph | QuikGraph: graph | Dijkstra.NET: graph | DijkstraFast: per route | QuikGraph: per route | Dijkstra.NET: per route |
|---:|---:|---:|---:|---:|---:|---:|
| 10,000 | 0.6 MB | 2.7 MB | 5.3 MB | 0.12 MB | 2.3 MB | 3.9 MB |
| 100,000 | 6.2 MB | 27 MB | 52 MB | 1.2 MB | 25 MB | 45 MB |
| 1,000,000 | 62 MB | 266 MB | 514 MB | 11.5 MB | 252 MB | 475 MB |

- **Like for like,** DijkstraFast's Dijkstra is 6-7 times faster than QuikGraph's and about 8 times faster than Dijkstra.NET's, and its A* is 6-8 times faster than QuikGraph's A*.
- **Memory:** DijkstraFast's graph is about 4 times smaller than QuikGraph's and about 8 times smaller than Dijkstra.NET's. At 10 million intersections the three graphs take 0.6, 2.5 and 4.9 GB. Each search allocates 19-22 times less than QuikGraph's Dijkstra, and 32-41 times less than Dijkstra.NET.
- **How the libraries are called:** QuikGraph's Dijkstra and A* are stopped as soon as the target is reached, as DijkstraFast does. Its convenient one-call `ShortestPathsDijkstra` finds the paths to every node first, so it is slower still. Dijkstra.NET only accepts whole-number costs, so road lengths are multiplied by 10,000 and rounded.
- **Every library finds the shortest route;** the benchmark checks this before timing anything. The 10-million map is left out because the other libraries take 7-16 seconds per route there.

Both are general-purpose graph libraries with many features DijkstraFast doesn't have. These numbers only cover point-to-point routes on road-like graphs.

To run the benchmarks yourself, from `benchmarks/DijkstraFast.Benchmarks`:

```
dotnet run -c Release -- --anyCategories PointToPoint Search Graph
```

That covers maps of 10,000 and 100,000 intersections and takes about 10 minutes. The table above comes from the scaling set, which takes about 11 minutes and peaks at about 4 GB of memory:

```
dotnet run -c Release -- --anyCategories Scaling
```

The comparison with other libraries takes about 8 minutes:

```
dotnet run -c Release -- --anyCategories Libraries
```

BenchmarkDotNet measures only what a search allocates, so the graph sizes come from a separate report that builds each library's graph and measures how much the managed heap grows:

```
dotnet run -c Release -- --graph-memory
```

## Building and testing

```
dotnet test
```

The tests run on .NET 10 and use xUnit and Shouldly.

## License

[MIT](LICENSE)

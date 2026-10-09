using System;
using System.Linq;
using QuikGraph;
using DijkstraNetGraph = Dijkstra.NET.Graph.Simple.Graph;
using QuikEdge = QuikGraph.TaggedEdge<int, double>;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// How much memory each library's graph keeps alive once built, for the maps in <see cref="LibraryBenchmarks"/>.
    /// BenchmarkDotNet only measures what a benchmark allocates, so this is a separate report: dotnet run -c Release -- --graph-memory
    /// </summary>
    public static class GraphMemory
    {
        public static void Print()
        {
            Console.WriteLine("Managed heap growth from building each library's graph, in MB:");
            Console.WriteLine($"{"Intersections",14} {"Edges",12} {"DijkstraFast",14} {"QuikGraph",14} {"Dijkstra.NET",14}");

            foreach (var size in new[] { 10_000, 100_000, 1_000_000, 10_000_000 })
            {
                var network = new RoadNetwork(size);
                var nodeCount = network.NodeCount;
                var edges = network.Edges;

                var dijkstraFast = Measure(() =>
                {
                    return new Graph(nodeCount, edges);
                });

                var quikGraph = Measure(() =>
                {
                    var graph = new AdjacencyGraph<int, QuikEdge>();

                    graph.AddVertexRange(Enumerable.Range(0, nodeCount));
                    graph.AddEdgeRange(edges.Select(edge => new QuikEdge(edge.From, edge.To, edge.Cost)));

                    return graph;
                });

                // Allocated outside the measurement: the key table is part of this benchmark, not of Dijkstra.NET.
                var keys = new uint[nodeCount];

                var dijkstraNet = Measure(() =>
                {
                    var graph = new DijkstraNetGraph();

                    for (var node = 0; node < nodeCount; node++)
                    {
                        keys[node] = graph.AddNode();
                    }

                    foreach (var edge in edges)
                    {
                        graph.Connect(keys[edge.From], keys[edge.To], (int)Math.Round(edge.Cost * 10_000));
                    }

                    return graph;
                });

                Console.WriteLine($"{nodeCount,14:N0} {edges.Length,12:N0} {dijkstraFast,14:F1} {quikGraph,14:F1} {dijkstraNet,14:F1}");
            }
        }

        private static double Measure(Func<object> build)
        {
            var before = GC.GetTotalMemory(forceFullCollection: true);
            var graph = build();
            var after = GC.GetTotalMemory(forceFullCollection: true);

            GC.KeepAlive(graph);

            return (after - before) / (1024.0 * 1024.0);
        }
    }
}
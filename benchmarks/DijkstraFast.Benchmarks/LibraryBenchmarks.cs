using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.ShortestPath;
using DijkstraNetGraph = Dijkstra.NET.Graph.Simple.Graph;
using DijkstraNetSearch = Dijkstra.NET.ShortestPath.DijkstraExtensions;
using QuikEdge = QuikGraph.TaggedEdge<int, double>;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// The same routes searched with DijkstraFast and with two popular .NET graph libraries, QuikGraph and Dijkstra.NET,
    /// each used through its standard graph type. The routes are the same as in the other benchmarks for the sizes they share.
    /// Each invocation runs <see cref="RoutesPerInvoke"/> different routes; reported times are per route.
    /// </summary>
    /// <remarks>
    /// Run with <c>--anyCategories Libraries</c>.
    /// Maps of 10 million intersections are left out: the other libraries take 7-16 seconds per route there, so a run would take over an hour.
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("Libraries")]
    [Config(typeof(ScalingConfig))]
    public class LibraryBenchmarks
    {
        private const int RoutesPerInvoke = 16;

        // Dijkstra.NET only takes whole-number costs, so road lengths are scaled and rounded.
        private const double DijkstraNetScale = 10_000;

        private RoadNetwork _network = null!;
        private (int Source, int Target)[] _routes = null!;
        private AdjacencyGraph<int, QuikEdge> _quikGraph = null!;
        private DijkstraNetGraph _dijkstraNet = null!;
        private uint[] _dijkstraNetKeys = null!;
        private long _pathNodes;

        [Params(10_000, 100_000, 1_000_000)]
        public int NodeCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _network = new RoadNetwork(NodeCount);
            _routes = _network.RandomQueries(RoutesPerInvoke);

            _quikGraph = new AdjacencyGraph<int, QuikEdge>();
            _quikGraph.AddVertexRange(Enumerable.Range(0, _network.NodeCount));
            _quikGraph.AddEdgeRange(_network.Edges.Select(edge => new QuikEdge(edge.From, edge.To, edge.Cost)));

            _dijkstraNet = new DijkstraNetGraph();
            _dijkstraNetKeys = new uint[_network.NodeCount];

            for (var node = 0; node < _network.NodeCount; node++)
            {
                _dijkstraNetKeys[node] = _dijkstraNet.AddNode();
            }

            foreach (var edge in _network.Edges)
            {
                _dijkstraNet.Connect(_dijkstraNetKeys[edge.From], _dijkstraNetKeys[edge.To], (int)Math.Round(edge.Cost * DijkstraNetScale));
            }

            CheckTheLibrariesAgree();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = RoutesPerInvoke)]
        public double DijkstraFastDijkstra()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += _network.Graph.FindShortestPath(source, target).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double DijkstraFastAStar()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target)).Distance;
            }

            return total;
        }

        /// <summary>QuikGraph's Dijkstra, stopped once the target is reached, as DijkstraFast does.</summary>
        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double QuikGraphDijkstra()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += SearchQuikGraphDijkstra(source, target);
            }

            return total;
        }

        /// <summary>QuikGraph's A*, stopped once the target is reached.</summary>
        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double QuikGraphAStar()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += SearchQuikGraphAStar(source, target);
            }

            return total;
        }

        /// <summary>QuikGraph's documented one-call API, which finds the shortest paths to every node before returning one.</summary>
        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double QuikGraphShortestPathsDijkstra()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += SearchQuikGraphShortestPathsDijkstra(source, target);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double DijkstraNet()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += SearchDijkstraNet(source, target);
            }

            return total;
        }

        private double SearchQuikGraphDijkstra(int source, int target)
        {
            var algorithm = new DijkstraShortestPathAlgorithm<int, QuikEdge>(_quikGraph, edge => edge.Tag);

            return QuikGraphSearch(algorithm, handler => algorithm.FinishVertex += handler, source, target);
        }

        private double SearchQuikGraphAStar(int source, int target)
        {
            var algorithm = new AStarShortestPathAlgorithm<int, QuikEdge>(_quikGraph, edge => edge.Tag, node => _network.StraightLine(node, target));

            return QuikGraphSearch(algorithm, handler => algorithm.FinishVertex += handler, source, target);
        }

        // Records predecessors while the search runs and aborts it once the target has been finished.
        private static double QuikGraphSearch(
            ShortestPathAlgorithmBase<int, QuikEdge, IVertexListGraph<int, QuikEdge>> algorithm,
            Action<VertexAction<int>> subscribeFinishVertex,
            int source,
            int target)
        {
            var predecessors = new VertexPredecessorRecorderObserver<int, QuikEdge>();

            using (predecessors.Attach(algorithm))
            {
                subscribeFinishVertex(vertex =>
                {
                    if (vertex == target)
                    {
                        algorithm.Abort();
                    }
                });

                try
                {
                    algorithm.Compute(source);
                }
                catch (OperationCanceledException)
                {
                    // Depending on the version, QuikGraph may report an abort this way.
                }
            }

            return predecessors.TryGetPath(target, out var path) ? PathCost(path) : double.PositiveInfinity;
        }

        private double SearchQuikGraphShortestPathsDijkstra(int source, int target)
        {
            var tryGetPath = _quikGraph.ShortestPathsDijkstra(edge => edge.Tag, source);

            return tryGetPath(target, out var path) ? PathCost(path) : double.PositiveInfinity;
        }

        private double SearchDijkstraNet(int source, int target)
        {
            var result = DijkstraNetSearch.Dijkstra(_dijkstraNet, _dijkstraNetKeys[source], _dijkstraNetKeys[target]);

            if (!result.IsFounded)
            {
                return double.PositiveInfinity;
            }

            // Walk the path too, as the other libraries do when they return one.
            _pathNodes += result.GetPath().Count();

            return result.Distance / DijkstraNetScale;
        }

        private static double PathCost(IEnumerable<QuikEdge> path)
        {
            var total = 0.0;

            foreach (var edge in path)
            {
                total += edge.Tag;
            }

            return total;
        }

        // Every library must find the shortest route for the timing to mean anything. Dijkstra.NET's distances are
        // rounded to its whole-number costs. On the largest map only the first few routes are checked, to keep setup short.
        private void CheckTheLibrariesAgree()
        {
            var routesToCheck = NodeCount <= 100_000 ? _routes.Length : 2;

            for (var i = 0; i < routesToCheck; i++)
            {
                var (source, target) = _routes[i];
                var expected = _network.Graph.FindShortestPath(source, target).Distance;
                var tolerance = 1e-9 * Math.Max(1, expected);

                var results = new (string Name, double Distance, double Tolerance)[]
                {
                    ("DijkstraFast A*", _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target)).Distance, tolerance),
                    ("QuikGraph Dijkstra", SearchQuikGraphDijkstra(source, target), tolerance),
                    ("QuikGraph A*", SearchQuikGraphAStar(source, target), tolerance),
                    ("QuikGraph ShortestPathsDijkstra", SearchQuikGraphShortestPathsDijkstra(source, target), tolerance),
                    ("Dijkstra.NET", SearchDijkstraNet(source, target), 2e-4 * expected),
                };

                foreach (var (name, distance, allowed) in results)
                {
                    if (Math.Abs(distance - expected) > allowed)
                    {
                        throw new InvalidOperationException($"{name} disagrees for {source} -> {target}: {distance}, expected {expected}.");
                    }
                }
            }
        }
    }
}
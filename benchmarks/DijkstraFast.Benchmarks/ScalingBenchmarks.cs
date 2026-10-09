using System;
using BenchmarkDotNet.Attributes;
using DijkstraFast.Benchmarks.Baselines;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// How route time grows with the size of the road map, from a city to a country.
    /// The routes are the same as in <see cref="PointToPointBenchmarks"/> for the sizes both cover.
    /// Each invocation runs <see cref="RoutesPerInvoke"/> different routes; reported times are per route.
    /// </summary>
    /// <remarks>
    /// Run with <c>--anyCategories Scaling</c>. It takes about 11 minutes on an Intel Core i9-13900K.
    /// At 10 million intersections a benchmark process peaks at about 4 GB of memory.
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("Scaling")]
    [Config(typeof(ScalingConfig))]
    public class ScalingBenchmarks
    {
        private const int RoutesPerInvoke = 16;

        private RoadNetwork _network = null!;
        private (int Source, int Target)[] _routes = null!;
        private TextbookDijkstra _textbook = null!;

        [Params(10_000, 100_000, 500_000, 1_000_000, 10_000_000)]
        public int NodeCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _network = new RoadNetwork(NodeCount);
            _routes = _network.RandomQueries(RoutesPerInvoke);
            _textbook = new TextbookDijkstra(_network.NodeCount, _network.Edges);

            CheckTheVariantsAgree();
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = RoutesPerInvoke)]
        public double Dijkstra()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += _network.Graph.FindShortestPath(source, target).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double AStar()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target)).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double WeightedAStar()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target), 1 / 0.925).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = RoutesPerInvoke)]
        public double TextbookDijkstraWithBclPriorityQueue()
        {
            var total = 0.0;

            foreach (var (source, target) in _routes)
            {
                total += _textbook.FindShortestPath(source, target).Distance;
            }

            return total;
        }

        // Every variant must answer correctly for the timing to mean anything. On the largest maps a full
        // check would take minutes in each benchmark's process, so only the first few routes are checked there.
        private void CheckTheVariantsAgree()
        {
            var routesToCheck = NodeCount <= 100_000 ? _routes.Length : 2;

            for (var i = 0; i < routesToCheck; i++)
            {
                var (source, target) = _routes[i];
                var expected = _network.Graph.FindShortestPath(source, target).Distance;
                var aStar = _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target)).Distance;
                var weighted = _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target), 1 / 0.925).Distance;
                var textbook = _textbook.FindShortestPath(source, target).Distance;

                if (Math.Abs(aStar - expected) > 1e-9 || Math.Abs(textbook - expected) > 1e-9 || weighted > expected / 0.925 + 1e-9)
                {
                    throw new InvalidOperationException($"Variants disagree for {source} -> {target}: Dijkstra {expected}, A* {aStar}, weighted A* {weighted}, textbook {textbook}.");
                }
            }
        }
    }
}
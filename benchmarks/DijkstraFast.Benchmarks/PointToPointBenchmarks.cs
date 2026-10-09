using System;
using BenchmarkDotNet.Attributes;
using DijkstraFast.Benchmarks.Baselines;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// A route between two random intersections, the kind of query used to price a trip.
    /// Each invocation runs <see cref="QueriesPerInvoke"/> different routes; reported times are per route.
    /// </summary>
    [MemoryDiagnoser]
    [BenchmarkCategory("PointToPoint")]
    public class PointToPointBenchmarks
    {
        private const int QueriesPerInvoke = 16;

        private RoadNetwork _network = null!;
        private (int Source, int Target)[] _queries = null!;
        private ListGraph _listGraph = null!;
        private TextbookDijkstra _textbook = null!;

        [Params(10_000, 100_000)]
        public int NodeCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _network = new RoadNetwork(NodeCount);
            _queries = _network.RandomQueries(QueriesPerInvoke);
            _listGraph = new ListGraph(_network.NodeCount, _network.Edges);
            _textbook = new TextbookDijkstra(_network.NodeCount, _network.Edges);

            CheckTheVariantsAgree();
        }

        // Timing is only meaningful if every variant answers the same question correctly.
        private void CheckTheVariantsAgree()
        {
            foreach (var (source, target) in _queries)
            {
                var expected = _network.Graph.FindShortestPath(source, target).Distance;
                var aStar = _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target)).Distance;
                var weighted = _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target), 1 / 0.925).Distance;
                var custom = _listGraph.FindShortestPath(source, target).Distance;
                var textbook = _textbook.FindShortestPath(source, target).Distance;

                if (Math.Abs(aStar - expected) > 1e-9 || custom != expected || Math.Abs(textbook - expected) > 1e-9 || weighted > expected / 0.925 + 1e-9)
                {
                    throw new InvalidOperationException($"Variants disagree for {source} -> {target}: Dijkstra {expected}, A* {aStar}, weighted A* {weighted}, IGraph {custom}, textbook {textbook}.");
                }
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = QueriesPerInvoke)]
        public double Dijkstra()
        {
            var total = 0.0;

            foreach (var (source, target) in _queries)
            {
                total += _network.Graph.FindShortestPath(source, target).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = QueriesPerInvoke)]
        public double AStar()
        {
            var total = 0.0;

            foreach (var (source, target) in _queries)
            {
                total += _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target)).Distance;
            }

            return total;
        }

        /// <summary>Weight 1/0.925: routes at most ~8% longer than the shortest.</summary>
        [Benchmark(OperationsPerInvoke = QueriesPerInvoke)]
        public double WeightedAStar()
        {
            var total = 0.0;

            foreach (var (source, target) in _queries)
            {
                total += _network.Graph.FindShortestPath(source, target, node => _network.StraightLine(node, target), 1 / 0.925).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = QueriesPerInvoke)]
        public double DijkstraOnCustomIGraph()
        {
            var total = 0.0;

            foreach (var (source, target) in _queries)
            {
                total += _listGraph.FindShortestPath(source, target).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = QueriesPerInvoke)]
        public double TextbookDijkstraWithBclPriorityQueue()
        {
            var total = 0.0;

            foreach (var (source, target) in _queries)
            {
                total += _textbook.FindShortestPath(source, target).Distance;
            }

            return total;
        }
    }
}
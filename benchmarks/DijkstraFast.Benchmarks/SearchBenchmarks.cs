using System;
using BenchmarkDotNet.Attributes;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// Searches without a single fixed target: the nearest depot (1% of intersections are depots),
    /// and the cheapest routes from one intersection to every other.
    /// </summary>
    [MemoryDiagnoser]
    [BenchmarkCategory("Search")]
    public class SearchBenchmarks
    {
        private const int NearestQueriesPerInvoke = 16;
        private const int TreesPerInvoke = 4;

        private RoadNetwork _network = null!;
        private int[] _sources = null!;
        private Func<int, bool> _isDepot = null!;

        [Params(10_000, 100_000)]
        public int NodeCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _network = new RoadNetwork(NodeCount);

            var random = new Random(11);
            var depots = new bool[_network.NodeCount];

            for (var node = 0; node < depots.Length; node++)
            {
                depots[node] = random.NextDouble() < 0.01;
            }

            _isDepot = node => depots[node];
            _sources = new int[NearestQueriesPerInvoke];

            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i] = random.Next(_network.NodeCount);
            }
        }

        [Benchmark(OperationsPerInvoke = NearestQueriesPerInvoke)]
        public double FindNearest()
        {
            var total = 0.0;

            foreach (var source in _sources)
            {
                total += _network.Graph.FindNearest(source, _isDepot).Distance;
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = TreesPerInvoke)]
        public double FindShortestPathsFrom()
        {
            var total = 0.0;

            for (var i = 0; i < TreesPerInvoke; i++)
            {
                total += _network.Graph.FindShortestPathsFrom(_sources[i]).DistanceTo(0);
            }

            return total;
        }
    }
}
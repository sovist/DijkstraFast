using BenchmarkDotNet.Attributes;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// Building a <see cref="Graph"/> from a list of edges, and reversing it.
    /// The road network has about four edges per intersection.
    /// </summary>
    [MemoryDiagnoser]
    [BenchmarkCategory("Graph")]
    public class GraphBenchmarks
    {
        private RoadNetwork _network = null!;

        [Params(10_000, 100_000)]
        public int NodeCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _network = new RoadNetwork(NodeCount);
        }

        [Benchmark]
        public Graph Build()
        {
            return new Graph(_network.NodeCount, _network.Edges);
        }

        [Benchmark]
        public Graph Reverse()
        {
            return _network.Graph.Reverse();
        }
    }
}
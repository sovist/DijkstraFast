using System.Collections.Generic;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// A typical hand-written <see cref="IGraph"/>: one list of edges per node. Searching it goes through
    /// the general <see cref="IGraph"/> code path, which shows the cost of not using <see cref="Graph"/>.
    /// </summary>
    public sealed class ListGraph : IGraph
    {
        private readonly List<Edge>[] _edges;

        public ListGraph(int nodeCount, IEnumerable<Edge> edges)
        {
            _edges = new List<Edge>[nodeCount];

            for (var node = 0; node < nodeCount; node++)
            {
                _edges[node] = new List<Edge>();
            }

            foreach (var edge in edges)
            {
                _edges[edge.From].Add(edge);
            }
        }

        public int NodeCount => _edges.Length;

        public IEnumerable<Edge> GetOutgoingEdges(int node)
        {
            return _edges[node];
        }
    }
}
using System;

namespace DijkstraFast
{
    /// <summary>
    /// The cheapest paths from one source node to every node in a graph.
    /// </summary>
    /// <remarks>
    /// Returned by <see cref="Dijkstra.FindShortestPathsFrom"/>. Answering a query is cheap:
    /// <see cref="DistanceTo"/> is a lookup and <see cref="PathTo"/> is proportional to the path length.
    /// </remarks>
    public sealed class ShortestPathTree
    {
        private readonly double[] _distance;
        private readonly int[] _previous;

        internal ShortestPathTree(int source, double[] distance, int[] previous)
        {
            Source = source;
            _distance = distance;
            _previous = previous;
        }

        /// <summary>The node all paths start from.</summary>
        public int Source { get; }

        /// <summary>The number of nodes in the searched graph.</summary>
        public int NodeCount => _distance.Length;

        /// <summary>Whether <paramref name="node"/> can be reached from <see cref="Source"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the graph.</exception>
        public bool IsReachable(int node)
        {
            return !double.IsPositiveInfinity(DistanceTo(node));
        }

        /// <summary>
        /// The cost of the cheapest path from <see cref="Source"/> to <paramref name="node"/>,
        /// or <see cref="double.PositiveInfinity"/> if it cannot be reached.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the graph.</exception>
        public double DistanceTo(int node)
        {
            ValidateNode(node);

            return _distance[node];
        }

        /// <summary>The cheapest path from <see cref="Source"/> to <paramref name="node"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of the graph.</exception>
        public PathResult PathTo(int node)
        {
            if (!IsReachable(node))
            {
                return PathResult.NotFound;
            }

            return PathResult.Trace(Source, node, _distance, _previous);
        }

        private void ValidateNode(int node)
        {
            if ((uint)node >= (uint)_distance.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(node), node, $"Node must be in the range [0, {_distance.Length}).");
            }
        }
    }
}
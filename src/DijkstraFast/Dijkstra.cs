using System;

namespace DijkstraFast
{
    /// <summary>
    /// Dijkstra's shortest-path search over an <see cref="IGraph"/>.
    /// </summary>
    /// <remarks>
    /// Each search runs in O((V + E) log V) time and allocates O(V) memory, where V and E are the
    /// number of nodes and edges. Searches that have a target stop as soon as the target is reached,
    /// so a nearby target is found without exploring the whole graph.
    /// </remarks>
    public static class Dijkstra
    {
        /// <summary>
        /// Finds the cheapest path from <paramref name="source"/> to <paramref name="target"/>.
        /// </summary>
        /// <param name="graph">The graph to search.</param>
        /// <param name="source">The node to start from.</param>
        /// <param name="target">The node to reach.</param>
        /// <returns>The cheapest path, or a result with <see cref="PathResult.Found"/> set to false if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="graph"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> or <paramref name="target"/> is not a node of the graph.</exception>
        /// <exception cref="InvalidOperationException">A custom <see cref="IGraph"/> returned an invalid edge.</exception>
        public static PathResult FindShortestPath(this IGraph graph, int source, int target)
        {
            ValidateGraph(graph);
            ValidateNode(graph, source, nameof(source));
            ValidateNode(graph, target, nameof(target));

            return new Search(graph).FindPath(source, target, isTarget: null);
        }

        /// <summary>
        /// Finds the cheapest path from <paramref name="source"/> to the nearest node that satisfies
        /// <paramref name="isTarget"/>.
        /// </summary>
        /// <param name="graph">The graph to search.</param>
        /// <param name="source">The node to start from. It is checked first, so it is returned if it matches.</param>
        /// <param name="isTarget">
        /// Returns whether a node is an acceptable destination. Called at most once per node,
        /// in order of increasing distance from <paramref name="source"/>.
        /// </param>
        /// <returns>
        /// The cheapest path to the nearest matching node, which is available as <see cref="PathResult.Target"/>;
        /// or a result with <see cref="PathResult.Found"/> set to false if no reachable node matches.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="graph"/> or <paramref name="isTarget"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is not a node of the graph.</exception>
        /// <exception cref="InvalidOperationException">A custom <see cref="IGraph"/> returned an invalid edge.</exception>
        public static PathResult FindNearest(this IGraph graph, int source, Func<int, bool> isTarget)
        {
            ValidateGraph(graph);
            ValidateNode(graph, source, nameof(source));

            if (isTarget == null)
            {
                throw new ArgumentNullException(nameof(isTarget));
            }

            return new Search(graph).FindPath(source, target: -1, isTarget);
        }

        /// <summary>
        /// Finds the cheapest paths from <paramref name="source"/> to every node in the graph.
        /// </summary>
        /// <param name="graph">The graph to search.</param>
        /// <param name="source">The node to start from.</param>
        /// <returns>A tree that answers distance and path queries for any node.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="graph"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="source"/> is not a node of the graph.</exception>
        /// <exception cref="InvalidOperationException">A custom <see cref="IGraph"/> returned an invalid edge.</exception>
        public static ShortestPathTree FindShortestPathsFrom(this IGraph graph, int source)
        {
            ValidateGraph(graph);
            ValidateNode(graph, source, nameof(source));

            var search = new Search(graph);

            search.Run(source, target: -1, isTarget: null);

            return new ShortestPathTree(source, search.Distance, search.Previous);
        }

        private static void ValidateGraph(IGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (graph.NodeCount < 0)
            {
                throw new InvalidOperationException($"Graph reports a negative node count ({graph.NodeCount}).");
            }
        }

        private static void ValidateNode(IGraph graph, int node, string paramName)
        {
            if ((uint)node >= (uint)graph.NodeCount)
            {
                throw new ArgumentOutOfRangeException(paramName, node, $"Node must be in the range [0, {graph.NodeCount}).");
            }
        }

        /// <summary>
        /// The state of one search.
        /// Nodes enter the queue ordered by their distance from the source.
        /// </summary>
        private sealed class Search
        {
            private readonly IGraph _graph;
            private readonly Graph? _compact;
            private readonly MinHeap _heap = new MinHeap();

            public Search(IGraph graph)
            {
                _graph = graph;
                _compact = graph as Graph;

                var nodeCount = graph.NodeCount;

                Distance = new double[nodeCount];
                Previous = new int[nodeCount];

                for (var node = 0; node < nodeCount; node++)
                {
                    Distance[node] = double.PositiveInfinity;
                    Previous[node] = -1;
                }
            }

            public double[] Distance { get; }

            public int[] Previous { get; }

            public PathResult FindPath(int source, int target, Func<int, bool>? isTarget)
            {
                var reached = Run(source, target, isTarget);

                return reached < 0 ? PathResult.NotFound : PathResult.Trace(source, reached, Distance, Previous);
            }

            // Runs until a node equal to target (or satisfying isTarget) is taken from the queue, and returns
            // that node. Returns -1 once every reachable node has been taken without a match.
            public int Run(int source, int target, Func<int, bool>? isTarget)
            {
                Distance[source] = 0;

                _heap.Push(source, 0);

                while (_heap.TryPop(out var node, out var priority))
                {
                    // A node is queued again each time its distance improves; only the latest entry counts.
                    if (priority > Distance[node])
                    {
                        continue;
                    }

                    if (node == target || (isTarget != null && isTarget(node)))
                    {
                        return node;
                    }

                    if (_compact != null)
                    {
                        var edges = _compact.Edges;

                        for (int i = _compact.Offsets[node], end = _compact.Offsets[node + 1]; i < end; i++)
                        {
                            Relax(node, edges[i].To, edges[i].Cost);
                        }
                    }
                    else
                    {
                        var edges = _graph.GetOutgoingEdges(node) ?? throw new InvalidOperationException($"GetOutgoingEdges({node}) returned null.");

                        foreach (var edge in edges)
                        {
                            if (edge.From != node)
                            {
                                throw new InvalidOperationException($"GetOutgoingEdges({node}) returned edge {edge}, which does not start at node {node}.");
                            }

                            if ((uint)edge.To >= (uint)Distance.Length)
                            {
                                throw new InvalidOperationException($"GetOutgoingEdges({node}) returned edge {edge}, which leads outside the graph ({Distance.Length} node(s)).");
                            }

                            Relax(node, edge.To, edge.Cost);
                        }
                    }
                }

                return -1;
            }

            private void Relax(int from, int to, double cost)
            {
                var candidate = Distance[from] + cost;

                if (candidate < Distance[to])
                {
                    Distance[to] = candidate;
                    Previous[to] = from;

                    _heap.Push(to, candidate);
                }
            }
        }
    }
}
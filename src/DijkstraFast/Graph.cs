using System;
using System.Collections.Generic;
using System.Linq;

namespace DijkstraFast
{
    /// <summary>
    /// An immutable directed graph built from a list of edges.
    /// </summary>
    /// <remarks>
    /// Edges are stored contiguously per node, so searches touch only flat arrays.
    /// Instances are immutable and safe to search from multiple threads at once.
    /// </remarks>
    public sealed class Graph : IGraph
    {
        // Compressed sparse row layout: the edges leaving node n are
        // _edges[_offsets[n]] up to (but not including) _edges[_offsets[n + 1]].
        private readonly int[] _offsets;
        private readonly Edge[] _edges;

        /// <summary>
        /// Creates a graph with <paramref name="nodeCount"/> nodes and the given edges.
        /// </summary>
        /// <param name="nodeCount">The number of nodes. Nodes are numbered from 0 to <paramref name="nodeCount"/> - 1.</param>
        /// <param name="edges">
        /// The directed edges. Parallel edges and self-loops are allowed.
        /// The edges leaving each node keep the order in which they were given.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeCount"/> is negative.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="edges"/> is null.</exception>
        /// <exception cref="ArgumentException">An edge refers to a node outside the graph.</exception>
        public Graph(int nodeCount, IEnumerable<Edge> edges)
        {
            if (nodeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeCount), nodeCount, "Node count cannot be negative.");
            }

            if (edges == null)
            {
                throw new ArgumentNullException(nameof(edges));
            }

            var input = edges as Edge[] ?? edges.ToArray();

            // Count the edges leaving each node, then turn the counts into start offsets.
            var offsets = new int[nodeCount + 1];

            foreach (var edge in input)
            {
                if ((uint)edge.From >= (uint)nodeCount || (uint)edge.To >= (uint)nodeCount)
                {
                    throw new ArgumentException($"Edge {edge} refers to a node outside the graph, which has {nodeCount} node(s).", nameof(edges));
                }

                offsets[edge.From + 1]++;
            }

            for (var node = 0; node < nodeCount; node++)
            {
                offsets[node + 1] += offsets[node];
            }

            // Counting sort by source node. It is stable, so per-node order matches the input.
            var sorted = new Edge[input.Length];
            var cursor = (int[])offsets.Clone();

            foreach (var edge in input)
            {
                sorted[cursor[edge.From]++] = edge;
            }

            NodeCount = nodeCount;
            _offsets = offsets;
            _edges = sorted;
        }

        /// <inheritdoc />
        public int NodeCount { get; }

        /// <summary>The number of edges in the graph.</summary>
        public int EdgeCount => _edges.Length;

        /// <summary>
        /// Returns the edges leaving <paramref name="node"/>, in the order they were given.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="node"/> is not a node of this graph.</exception>
        public IReadOnlyList<Edge> GetOutgoingEdges(int node)
        {
            if ((uint)node >= (uint)NodeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(node), node, $"Node must be in the range [0, {NodeCount}).");
            }

            return new ArraySegment<Edge>(_edges, _offsets[node], _offsets[node + 1] - _offsets[node]);
        }

        IEnumerable<Edge> IGraph.GetOutgoingEdges(int node)
        {
            return GetOutgoingEdges(node);
        }

        /// <summary>
        /// Returns a copy of this graph with every edge pointing the other way.
        /// </summary>
        /// <remarks>
        /// Searching the reversed graph from a node finds the cheapest paths <em>to</em> that node.
        /// </remarks>
        public Graph Reverse()
        {
            return new Graph(NodeCount, _edges.Select(edge => new Edge(edge.To, edge.From, edge.Cost)));
        }

        internal int[] Offsets => _offsets;

        internal Edge[] Edges => _edges;
    }
}
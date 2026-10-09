using System;
using System.Collections.Generic;
using System.Globalization;

namespace DijkstraFast
{
    /// <summary>
    /// The outcome of a path search: either a path and its total cost, or "no path".
    /// </summary>
    public sealed class PathResult
    {
        private static readonly PathResult NotFoundResult = new PathResult(Array.Empty<int>(), double.PositiveInfinity);

        private readonly int[] _nodes;

        private PathResult(int[] nodes, double distance)
        {
            _nodes = nodes;
            Distance = distance;
        }

        /// <summary>Whether a path was found.</summary>
        public bool Found => _nodes.Length > 0;

        /// <summary>
        /// The nodes along the path, starting with the source and ending with the target.
        /// Contains just the source when the source is the target, and is empty when no path was found.
        /// </summary>
        public IReadOnlyList<int> Nodes => _nodes;

        /// <summary>
        /// The total cost of the path, or <see cref="double.PositiveInfinity"/> when no path was found.
        /// </summary>
        public double Distance { get; }

        /// <summary>
        /// The node the path ends at, or -1 when no path was found.
        /// Most useful with <see cref="Dijkstra.FindNearest"/>, where the target is not known in advance.
        /// </summary>
        public int Target => Found ? _nodes[_nodes.Length - 1] : -1;

        /// <inheritdoc />
        public override string ToString()
        {
            if (!Found)
            {
                return "No path";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0} (distance {1})", string.Join(" -> ", _nodes), Distance);
        }

        internal static PathResult NotFound => NotFoundResult;

        // Follows the predecessor links back from target to source.
        // Assumes target is reachable, so the links form a chain ending at source.
        internal static PathResult Trace(int source, int target, double[] distance, int[] previous)
        {
            var count = 1;

            for (var node = target; node != source; node = previous[node])
            {
                count++;
            }

            var nodes = new int[count];
            var current = target;

            for (var i = count - 1; i >= 0; i--)
            {
                nodes[i] = current;
                current = previous[current];
            }

            return new PathResult(nodes, distance[target]);
        }
    }
}
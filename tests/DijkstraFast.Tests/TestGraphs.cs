namespace DijkstraFast.Tests
{
    /// <summary>
    /// Wraps another graph so searches go through the general <see cref="IGraph"/> code path,
    /// and records which nodes the search expanded.
    /// </summary>
    internal sealed class RecordingGraph(IGraph inner) : IGraph
    {
        public List<int> Expanded { get; } = [];

        public int NodeCount => inner.NodeCount;

        public IEnumerable<Edge> GetOutgoingEdges(int node)
        {
            Expanded.Add(node);

            return inner.GetOutgoingEdges(node);
        }
    }

    /// <summary>
    /// A graph whose edges come from a delegate, used to simulate badly behaved implementations.
    /// </summary>
    internal sealed class DelegateGraph(int nodeCount, Func<int, IEnumerable<Edge>> getEdges) : IGraph
    {
        public int NodeCount => nodeCount;

        public IEnumerable<Edge> GetOutgoingEdges(int node)
        {
            return getEdges(node);
        }
    }

    /// <summary>
    /// The grid from the README. '#' is a wall; each cell is a node: index = row * width + column.
    /// </summary>
    internal sealed class Grid : IGraph
    {
        private readonly string[] _rows;

        public Grid(params string[] rows)
        {
            _rows = rows;
        }

        public int Width => _rows[0].Length;

        public int NodeCount => _rows.Length * Width;

        public IEnumerable<Edge> GetOutgoingEdges(int node)
        {
            int row = node / Width, column = node % Width;

            foreach (var (r, c) in new[] { (row - 1, column), (row + 1, column), (row, column - 1), (row, column + 1) })
            {
                if (r >= 0 && r < _rows.Length && c >= 0 && c < Width && _rows[r][c] != '#')
                {
                    yield return new Edge(node, r * Width + c, 1);
                }
            }
        }
    }

    internal static class TestGraphs
    {
        /// <summary>
        /// The graph from the README:
        /// <code>
        ///   0 --1--> 1 --2--> 2 --1--> 3
        ///   |                 ^
        ///   +--------5--------+
        /// </code>
        /// </summary>
        public static Graph Readme()
        {
            return new Graph(4,
            [
                new Edge(0, 1, 1.0),
                new Edge(1, 2, 2.0),
                new Edge(0, 2, 5.0),
                new Edge(2, 3, 1.0),
            ]);
        }

        /// <summary>A chain 0 -> 1 -> ... -> nodeCount - 1 where every edge costs 1.</summary>
        public static Graph Line(int nodeCount)
        {
            return new Graph(nodeCount, Enumerable.Range(0, nodeCount - 1).Select(i => new Edge(i, i + 1, 1)));
        }
    }
}
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

        /// <summary>The number of moves between two cells, ignoring walls. Never overestimates.</summary>
        public double Manhattan(int from, int to)
        {
            return Math.Abs(from / Width - to / Width) + Math.Abs(from % Width - to % Width);
        }
    }

    /// <summary>
    /// Random points in a 100 x 100 square joined by edges that cost at least 5% more than the
    /// straight-line distance, so the straight-line distance never overestimates the remaining cost.
    /// </summary>
    internal sealed class GeometricGraph
    {
        private readonly double[] _x;
        private readonly double[] _y;

        public GeometricGraph(int seed)
        {
            var random = new Random(seed);
            var nodeCount = random.Next(2, 60);

            _x = Enumerable.Range(0, nodeCount).Select(_ => random.NextDouble() * 100).ToArray();
            _y = Enumerable.Range(0, nodeCount).Select(_ => random.NextDouble() * 100).ToArray();

            Edges = Enumerable.Range(0, nodeCount)
                .SelectMany(from => Enumerable.Range(0, random.Next(1, 4)).Select(_ => (From: from, To: random.Next(nodeCount))))
                .Select(e => new Edge(e.From, e.To, StraightLine(e.From, e.To) * (1.05 + random.NextDouble() * 0.5)))
                .ToArray();

            Graph = new Graph(nodeCount, Edges);
        }

        public Graph Graph { get; }

        public Edge[] Edges { get; }

        public int NodeCount => Graph.NodeCount;

        public double StraightLine(int from, int to)
        {
            var dx = _x[from] - _x[to];
            var dy = _y[from] - _y[to];

            return Math.Sqrt(dx * dx + dy * dy);
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
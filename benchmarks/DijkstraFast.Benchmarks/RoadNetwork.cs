using System;
using System.Collections.Generic;

namespace DijkstraFast.Benchmarks
{
    /// <summary>
    /// A synthetic city road map. Intersections sit on a jittered square grid and are joined to their
    /// neighbours by two-way roads that are 0-30% longer than the straight line between them. About 5%
    /// of roads are missing, so routes need detours. Road lengths are never shorter than the straight
    /// line, so <see cref="StraightLine"/> is a heuristic that never overestimates.
    /// </summary>
    public sealed class RoadNetwork
    {
        private readonly double[] _x;
        private readonly double[] _y;

        public RoadNetwork(int nodeCount, int seed = 42)
        {
            var side = (int)Math.Round(Math.Sqrt(nodeCount));
            var random = new Random(seed);

            NodeCount = side * side;

            _x = new double[NodeCount];
            _y = new double[NodeCount];

            for (var node = 0; node < NodeCount; node++)
            {
                _x[node] = node % side + (random.NextDouble() - 0.5) * 0.6;
                _y[node] = node / side + (random.NextDouble() - 0.5) * 0.6;
            }

            var edges = new List<Edge>();

            for (var node = 0; node < NodeCount; node++)
            {
                var right = node % side < side - 1 ? node + 1 : -1;
                var down = node / side < side - 1 ? node + side : -1;

                foreach (var neighbour in new[] { right, down })
                {
                    if (neighbour < 0 || random.NextDouble() < 0.05)
                    {
                        continue;
                    }

                    var cost = StraightLine(node, neighbour) * (1 + random.NextDouble() * 0.3);

                    edges.Add(new Edge(node, neighbour, cost));
                    edges.Add(new Edge(neighbour, node, cost));
                }
            }

            Edges = edges.ToArray();
            Graph = new Graph(NodeCount, Edges);
        }

        public int NodeCount { get; }

        public Edge[] Edges { get; }

        public Graph Graph { get; }

        public double StraightLine(int from, int to)
        {
            var dx = _x[from] - _x[to];
            var dy = _y[from] - _y[to];

            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Random (source, target) pairs, the same for a given seed.</summary>
        public (int Source, int Target)[] RandomQueries(int count, int seed = 7)
        {
            var random = new Random(seed);
            var queries = new (int Source, int Target)[count];

            for (var i = 0; i < count; i++)
            {
                queries[i] = (random.Next(NodeCount), random.Next(NodeCount));
            }

            return queries;
        }
    }
}
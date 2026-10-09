using System;
using System.Collections.Generic;

namespace DijkstraFast.Benchmarks.Baselines
{
    /// <summary>
    /// Dijkstra written the straightforward way in modern .NET: a list of neighbours per node and the
    /// built-in <see cref="PriorityQueue{TElement,TPriority}"/>. It stops at the target and rebuilds the
    /// path, like <see cref="Dijkstra.FindShortestPath(IGraph,int,int)"/>, so the two do the same work.
    /// </summary>
    public sealed class TextbookDijkstra
    {
        private readonly List<(int To, double Cost)>[] _neighbours;

        public TextbookDijkstra(int nodeCount, IEnumerable<Edge> edges)
        {
            _neighbours = new List<(int To, double Cost)>[nodeCount];

            for (var node = 0; node < nodeCount; node++)
            {
                _neighbours[node] = new List<(int To, double Cost)>();
            }

            foreach (var edge in edges)
            {
                _neighbours[edge.From].Add((edge.To, edge.Cost));
            }
        }

        public (List<int> Path, double Distance) FindShortestPath(int source, int target)
        {
            var distance = new double[_neighbours.Length];
            var previous = new int[_neighbours.Length];
            var visited = new bool[_neighbours.Length];
            var queue = new PriorityQueue<int, double>();

            Array.Fill(distance, double.PositiveInfinity);
            Array.Fill(previous, -1);

            distance[source] = 0;
            queue.Enqueue(source, 0);

            while (queue.TryDequeue(out var node, out _))
            {
                if (visited[node])
                {
                    continue;
                }

                visited[node] = true;

                if (node == target)
                {
                    break;
                }

                foreach (var (to, cost) in _neighbours[node])
                {
                    var candidate = distance[node] + cost;

                    if (candidate < distance[to])
                    {
                        distance[to] = candidate;
                        previous[to] = node;

                        queue.Enqueue(to, candidate);
                    }
                }
            }

            var path = new List<int>();

            if (double.IsPositiveInfinity(distance[target]))
            {
                return (path, distance[target]);
            }

            for (var node = target; node != -1; node = previous[node])
            {
                path.Add(node);
            }

            path.Reverse();

            return (path, distance[target]);
        }
    }
}
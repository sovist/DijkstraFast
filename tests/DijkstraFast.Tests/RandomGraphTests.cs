namespace DijkstraFast.Tests
{
    /// <summary>
    /// Checks the search against a simple Bellman-Ford reference on many random graphs.
    /// Costs are multiples of 0.5, so every sum is exact and distances can be compared exactly.
    /// </summary>
    public class RandomGraphTests
    {
        public static IEnumerable<object[]> Seeds()
        {
            return Enumerable.Range(0, 300).Select(seed => new object[] { seed });
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Searches_ShouldMatchTheReference_When_TheGraphIsSmallAndRandom(int seed)
        {
            // Arrange
            var random = new Random(seed);
            var nodeCount = random.Next(1, 30);
            var edges = RandomEdges(random, nodeCount, edgeCount: random.Next(0, nodeCount * 4));
            var graph = new Graph(nodeCount, edges);
            var custom = new RecordingGraph(graph);

            for (var source = 0; source < nodeCount; source++)
            {
                var expected = ReferenceDistances(nodeCount, edges, source);
                var targets = Enumerable.Range(0, nodeCount).Where(_ => random.Next(4) == 0).ToHashSet();
                var nearestDistance = targets.Select(t => expected[t]).DefaultIfEmpty(double.PositiveInfinity).Min();

                // Act
                var tree = graph.FindShortestPathsFrom(source);
                var nearest = graph.FindNearest(source, targets.Contains);

                // Assert
                for (var target = 0; target < nodeCount; target++)
                {
                    tree.DistanceTo(target).ShouldBe(expected[target]);

                    ShouldBeCheapestPath(graph.FindShortestPath(source, target), edges, source, target, expected[target]);
                    ShouldBeCheapestPath(custom.FindShortestPath(source, target), edges, source, target, expected[target]);
                    ShouldBeCheapestPath(tree.PathTo(target), edges, source, target, expected[target]);
                }

                nearest.Distance.ShouldBe(nearestDistance);

                if (nearest.Found)
                {
                    targets.ShouldContain(nearest.Target);

                    ShouldBeCheapestPath(nearest, edges, source, nearest.Target, nearestDistance);
                }
            }
        }

        [Fact]
        public void FindShortestPathsFrom_ShouldMatchTheReference_When_TheGraphIsLargerAndRandom()
        {
            // Arrange
            var random = new Random(2026);
            const int nodeCount = 3000;
            var edges = RandomEdges(random, nodeCount, edgeCount: 12000);
            var graph = new Graph(nodeCount, edges);

            foreach (var source in new[] { 0, 1, 1500, 2999 })
            {
                var expected = ReferenceDistances(nodeCount, edges, source);

                // Act
                var tree = graph.FindShortestPathsFrom(source);

                // Assert
                Enumerable.Range(0, nodeCount).Select(tree.DistanceTo).ShouldBe(expected);
            }
        }

        private static void ShouldBeCheapestPath(PathResult path, Edge[] edges, int source, int target, double expectedDistance)
        {
            if (double.IsPositiveInfinity(expectedDistance))
            {
                path.ShouldBeNotFound();

                return;
            }

            path.Distance.ShouldBe(expectedDistance);
            path.ShouldBeValidPath(edges, source, target);
        }

        private static Edge[] RandomEdges(Random random, int nodeCount, int edgeCount)
        {
            return Enumerable.Range(0, edgeCount)
                .Select(_ => new Edge(random.Next(nodeCount), random.Next(nodeCount), random.Next(0, 21) / 2.0))
                .ToArray();
        }

        private static double[] ReferenceDistances(int nodeCount, Edge[] edges, int source)
        {
            var distance = Enumerable.Repeat(double.PositiveInfinity, nodeCount).ToArray();

            distance[source] = 0;

            for (var round = 0; round < nodeCount; round++)
            {
                var changed = false;

                foreach (var edge in edges)
                {
                    if (distance[edge.From] + edge.Cost < distance[edge.To])
                    {
                        distance[edge.To] = distance[edge.From] + edge.Cost;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            return distance;
        }
    }
}
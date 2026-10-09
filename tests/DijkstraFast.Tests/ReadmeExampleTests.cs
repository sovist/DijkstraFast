namespace DijkstraFast.Tests
{
    /// <summary>
    /// The examples from README.md, with each "// value" comment turned into an assertion.
    /// Keep the two in sync.
    /// </summary>
    public class ReadmeExampleTests
    {
        [Fact]
        public void QuickStart()
        {
            // Arrange
            var graph = BuildGraph();

            // Act
            PathResult path = graph.FindShortestPath(source: 0, target: 3);

            // Assert
            path.Found.ShouldBeTrue();
            path.Distance.ShouldBe(4);
            path.Nodes.ShouldBe([0, 1, 2, 3]);
        }

        [Fact]
        public void FindingTheNearestNodeThatMatchesACondition()
        {
            // Arrange
            var graph = BuildGraph();
            var depots = new HashSet<int> { 2, 3 };

            // Act
            PathResult nearest = graph.FindNearest(source: 0, isTarget: depots.Contains);

            // Assert
            nearest.Target.ShouldBe(2);
            nearest.Distance.ShouldBe(3);
            nearest.Nodes.ShouldBe([0, 1, 2]);
        }

        [Fact]
        public void PathsFromOneNodeToEveryNode()
        {
            // Arrange
            var graph = BuildGraph();

            // Act
            ShortestPathTree tree = graph.FindShortestPathsFrom(source: 0);

            // Assert
            tree.DistanceTo(3).ShouldBe(4);
            tree.PathTo(2).Nodes.ShouldBe([0, 1, 2]);
            tree.IsReachable(3).ShouldBeTrue();
        }

        [Fact]
        public void GraphsYouDontWantToBuildUpFront()
        {
            // Arrange
            var grid = BuildGrid();

            // Act
            var distance = grid.FindShortestPath(source: 0, target: 3).Distance;

            // Assert
            distance.ShouldBe(7);
        }

        [Fact]
        public void FasterSearchesWithAStar()
        {
            // Arrange
            var grid = BuildGrid();
            const int width = 4, target = 3;

            double ManhattanToTarget(int node)
            {
                return Math.Abs(node / width - target / width) + Math.Abs(node % width - target % width);
            }

            // Act
            var exact = grid.FindShortestPath(source: 0, target: target, heuristic: ManhattanToTarget);
            var quick = grid.FindShortestPath(source: 0, target: target, heuristic: ManhattanToTarget, heuristicWeight: 1.5);

            // Assert
            exact.Distance.ShouldBe(7);
            quick.Distance.ShouldBeInRange(7, 7 * 1.5);
        }

        private static Graph BuildGraph()
        {
            return new Graph(nodeCount: 4, new[]
            {
                new Edge(0, 1, 1.0),
                new Edge(1, 2, 2.0),
                new Edge(0, 2, 5.0),
                new Edge(2, 3, 1.0),
            });
        }

        private static Grid BuildGrid()
        {
            return new Grid(
                "..#.",
                "..#.",
                "....");
        }
    }
}
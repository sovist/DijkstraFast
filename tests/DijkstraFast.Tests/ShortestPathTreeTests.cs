namespace DijkstraFast.Tests
{
    public class ShortestPathTreeTests
    {
        [Fact]
        public void FindShortestPathsFrom_ShouldGiveTheDistanceAndPathToEveryNode()
        {
            // Act
            var tree = TestGraphs.Readme().FindShortestPathsFrom(0);

            // Assert
            tree.Source.ShouldBe(0);
            tree.NodeCount.ShouldBe(4);
            Enumerable.Range(0, 4).Select(tree.DistanceTo).ShouldBe([0, 1, 3, 4]);
            tree.PathTo(0).Nodes.ShouldBe([0]);
            tree.PathTo(1).Nodes.ShouldBe([0, 1]);
            tree.PathTo(2).Nodes.ShouldBe([0, 1, 2]);
            tree.PathTo(3).Nodes.ShouldBe([0, 1, 2, 3]);
        }

        [Fact]
        public void PathTo_ShouldMatchFindShortestPath_When_AskedForEveryNode()
        {
            // Arrange
            var graph = TestGraphs.Readme();

            // Act
            var tree = graph.FindShortestPathsFrom(1);

            // Assert
            for (var node = 0; node < graph.NodeCount; node++)
            {
                var direct = graph.FindShortestPath(1, node);
                var fromTree = tree.PathTo(node);

                fromTree.Found.ShouldBe(direct.Found);
                fromTree.Distance.ShouldBe(direct.Distance);
                fromTree.Nodes.ShouldBe(direct.Nodes);
            }
        }

        [Fact]
        public void IsReachable_ShouldBeFalse_When_NoPathLeadsToTheNode()
        {
            // Act
            var tree = TestGraphs.Readme().FindShortestPathsFrom(2);

            // Assert
            tree.IsReachable(0).ShouldBeFalse();
            tree.IsReachable(1).ShouldBeFalse();
            tree.IsReachable(2).ShouldBeTrue();
            tree.IsReachable(3).ShouldBeTrue();
            tree.DistanceTo(0).ShouldBe(double.PositiveInfinity);
            tree.PathTo(0).ShouldBeNotFound();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void Queries_ShouldThrow_When_TheNodeIsOutsideTheGraph(int node)
        {
            // Arrange
            var tree = TestGraphs.Readme().FindShortestPathsFrom(0);

            // Act
            Action distanceTo = () => tree.DistanceTo(node);
            Action isReachable = () => tree.IsReachable(node);
            Action pathTo = () => tree.PathTo(node);

            // Assert
            distanceTo.ShouldThrow<ArgumentOutOfRangeException>();
            isReachable.ShouldThrow<ArgumentOutOfRangeException>();
            pathTo.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FindShortestPathsFrom_ShouldThrow_When_TheSourceIsOutsideTheGraph()
        {
            // Act
            Action act = () => TestGraphs.Readme().FindShortestPathsFrom(-1);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("source");
        }
    }
}
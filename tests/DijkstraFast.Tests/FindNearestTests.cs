namespace DijkstraFast.Tests
{
    public class FindNearestTests
    {
        [Fact]
        public void FindNearest_ShouldReturnTheCheapestMatch_When_ACloserMatchByHopsCostsMore()
        {
            // Arrange: node 3 is one hop away but costs 10; node 4 is three hops away but costs 3.
            var graph = new Graph(5,
            [
                new Edge(0, 3, 10),
                new Edge(0, 1, 1),
                new Edge(1, 2, 1),
                new Edge(2, 4, 1),
            ]);

            // Act
            var nearest = graph.FindNearest(0, node => node >= 3);

            // Assert
            nearest.Found.ShouldBeTrue();
            nearest.Target.ShouldBe(4);
            nearest.Distance.ShouldBe(3);
            nearest.Nodes.ShouldBe([0, 1, 2, 4]);
        }

        [Fact]
        public void FindNearest_ShouldReturnTheSource_When_TheSourceMatches()
        {
            // Act
            var nearest = TestGraphs.Readme().FindNearest(1, _ => true);

            // Assert
            nearest.Target.ShouldBe(1);
            nearest.Distance.ShouldBe(0);
            nearest.Nodes.ShouldBe([1]);
        }

        [Fact]
        public void FindNearest_ShouldReportNotFound_When_NoReachableNodeMatches()
        {
            // Arrange: node 2 matches but cannot be reached from node 0.
            var graph = new Graph(3, [new Edge(0, 1, 1), new Edge(2, 0, 1)]);

            // Act
            var nearest = graph.FindNearest(0, node => node == 2);

            // Assert
            nearest.ShouldBeNotFound();
        }

        [Fact]
        public void FindNearest_ShouldCheckEachNodeOnceInOrderOfDistance()
        {
            // Arrange
            var graph = new Graph(5,
            [
                new Edge(0, 1, 4),
                new Edge(0, 2, 1),
                new Edge(2, 1, 1),
                new Edge(1, 3, 1),
                new Edge(2, 3, 5),
                new Edge(3, 4, 1),
            ]);
            var checkedNodes = new List<int>();

            // Act
            graph.FindNearest(0, node =>
            {
                checkedNodes.Add(node);

                return false;
            });

            // Assert: the distances from node 0 are 0, 1, 2, 3 and 4 for nodes 0, 2, 1, 3 and 4.
            checkedNodes.ShouldBe([0, 2, 1, 3, 4]);
        }

        [Fact]
        public void FindNearest_ShouldThrow_When_TheConditionIsNull()
        {
            // Act
            Action act = () => TestGraphs.Readme().FindNearest(0, null!);

            // Assert
            act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("isTarget");
        }

        [Fact]
        public void FindNearest_ShouldThrow_When_TheSourceIsOutsideTheGraph()
        {
            // Act
            Action act = () => TestGraphs.Readme().FindNearest(4, _ => true);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("source");
        }
    }
}
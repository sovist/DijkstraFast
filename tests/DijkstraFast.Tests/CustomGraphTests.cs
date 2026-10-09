namespace DijkstraFast.Tests;

public class CustomGraphTests
{
    [Fact]
    public void FindShortestPath_ShouldGoAroundWalls_When_SearchingAGrid()
    {
        // Arrange
        var grid = new Grid(
            "..#.",
            "..#.",
            "....");

        // Act
        var path = grid.FindShortestPath(0, 3);

        // Assert
        path.Distance.ShouldBe(7);
        path.Nodes[0].ShouldBe(0);
        path.Target.ShouldBe(3);
        path.Nodes.ShouldNotContain(2);
        path.Nodes.ShouldNotContain(6);
    }

    [Fact]
    public void FindShortestPath_ShouldMatchTheEquivalentGraph_When_SearchingThroughIGraph()
    {
        // Arrange
        var graph = TestGraphs.Readme();
        var custom = new RecordingGraph(graph);

        // Act & Assert
        for (var source = 0; source < graph.NodeCount; source++)
        {
            for (var target = 0; target < graph.NodeCount; target++)
            {
                var expected = graph.FindShortestPath(source, target);
                var actual = custom.FindShortestPath(source, target);

                actual.Distance.ShouldBe(expected.Distance);
                actual.Nodes.ShouldBe(expected.Nodes);
            }
        }
    }

    [Fact]
    public void FindShortestPath_ShouldThrow_When_AnEdgeDoesNotStartAtTheExpandedNode()
    {
        // Arrange
        var graph = new DelegateGraph(3, _ => [new Edge(2, 1, 1)]);

        // Act
        Action act = () => graph.FindShortestPath(0, 1);

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("does not start at node 0");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void FindShortestPath_ShouldThrow_When_AnEdgeLeadsOutsideTheGraph(int to)
    {
        // Arrange
        var graph = new DelegateGraph(3, node => [new Edge(node, to, 1)]);

        // Act
        Action act = () => graph.FindShortestPath(0, 1);

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("leads outside the graph");
    }

    [Fact]
    public void FindShortestPath_ShouldThrow_When_TheEdgeListIsNull()
    {
        // Arrange
        var graph = new DelegateGraph(3, _ => null!);

        // Act
        Action act = () => graph.FindShortestPath(0, 1);

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("returned null");
    }

    [Fact]
    public void FindShortestPathsFrom_ShouldThrow_When_TheNodeCountIsNegative()
    {
        // Arrange
        var graph = new DelegateGraph(-1, _ => []);

        // Act
        Action act = () => graph.FindShortestPathsFrom(0);

        // Assert
        act.ShouldThrow<InvalidOperationException>();
    }
}
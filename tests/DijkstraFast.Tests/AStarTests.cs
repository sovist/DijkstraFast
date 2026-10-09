namespace DijkstraFast.Tests;

public class AStarTests
{
    public static IEnumerable<object[]> Seeds()
    {
        return Enumerable.Range(0, 200).Select(seed => new object[] { seed });
    }

    public static IEnumerable<object[]> SeedsAndWeights()
    {
        double[] weights = [1.1, 1 / 0.925, 1.5, 3, 10];

        return Enumerable.Range(0, 100).SelectMany(seed => weights.Select(weight => new object[] { seed, weight }));
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void FindShortestPath_ShouldFindTheCheapestPath_When_TheHeuristicNeverOverestimates(int seed)
    {
        // Arrange
        var map = new GeometricGraph(seed);

        for (var target = 0; target < map.NodeCount; target++)
        {
            // Act
            var dijkstra = map.Graph.FindShortestPath(0, target);
            var aStar = map.Graph.FindShortestPath(0, target, node => map.StraightLine(node, target));

            // Assert
            if (!dijkstra.Found)
            {
                aStar.ShouldBeNotFound();

                continue;
            }

            aStar.Distance.ShouldBe(dijkstra.Distance, tolerance: 1e-9);
            aStar.ShouldBeValidPath(map.Edges, 0, target);
        }
    }

    [Theory]
    [MemberData(nameof(SeedsAndWeights))]
    public void FindShortestPath_ShouldStayWithinTheWeightTimesTheCheapestCost_When_TheHeuristicIsWeighted(int seed, double weight)
    {
        // Arrange
        var map = new GeometricGraph(seed);

        for (var target = 0; target < map.NodeCount; target++)
        {
            // Act
            var dijkstra = map.Graph.FindShortestPath(0, target);
            var weighted = map.Graph.FindShortestPath(0, target, node => map.StraightLine(node, target), weight);

            // Assert
            if (!dijkstra.Found)
            {
                weighted.ShouldBeNotFound();

                continue;
            }

            weighted.Distance.ShouldBeLessThanOrEqualTo(weight * dijkstra.Distance + 1e-9);
            weighted.ShouldBeValidPath(map.Edges, 0, target);
        }
    }

    [Fact]
    public void FindShortestPath_ShouldExpandFewerNodesThanDijkstra_When_GivenAHeuristic()
    {
        // Arrange: an open 40 x 40 grid, crossing it from left to right along the middle row.
        var grid = new Grid(Enumerable.Repeat(new string('.', 40), 40).ToArray());
        var source = 20 * grid.Width + 2;
        var target = 20 * grid.Width + 37;
        var dijkstraGraph = new RecordingGraph(grid);
        var aStarGraph = new RecordingGraph(grid);
        var weightedGraph = new RecordingGraph(grid);

        // Act
        var dijkstra = dijkstraGraph.FindShortestPath(source, target);
        var aStar = aStarGraph.FindShortestPath(source, target, node => grid.Manhattan(node, target));
        var weighted = weightedGraph.FindShortestPath(source, target, node => grid.Manhattan(node, target), heuristicWeight: 2);

        // Assert
        dijkstra.Distance.ShouldBe(35);
        aStar.Distance.ShouldBe(35);
        weighted.Distance.ShouldBe(35);

        aStarGraph.Expanded.Count.ShouldBeLessThan(dijkstraGraph.Expanded.Count / 10);
        weightedGraph.Expanded.Count.ShouldBeLessThanOrEqualTo(aStarGraph.Expanded.Count);
    }

    // Through node 1 costs 11 and through node 2 costs 6. A heavy weight makes the low estimate at node 1
    // dominate, so the search commits to the longer route; it is still within weight x the cheapest cost.
    [Fact]
    public void FindShortestPath_ShouldAcceptALongerPath_When_TheWeightLetsTheEstimateDominate()
    {
        // Arrange
        var graph = new Graph(4,
        [
            new Edge(0, 1, 1),
            new Edge(1, 3, 10),
            new Edge(0, 2, 5),
            new Edge(2, 3, 1),
        ]);
        double[] estimates = [0, 0, 1, 0];

        // Act
        var exact = graph.FindShortestPath(0, 3, node => estimates[node]);
        var weighted = graph.FindShortestPath(0, 3, node => estimates[node], heuristicWeight: 10);

        // Assert
        exact.Distance.ShouldBe(6);
        exact.Nodes.ShouldBe([0, 2, 3]);

        weighted.Distance.ShouldBe(11);
        weighted.Nodes.ShouldBe([0, 1, 3]);
    }

    // A search that never revisits a node it has expanded would take 0 -> 2 -> 3 -> 4 (cost 7) here,
    // because the estimate for node 1 is high enough to delay it until after node 3 has been expanded.
    [Fact]
    public void FindShortestPath_ShouldFindTheCheapestPath_When_TheHeuristicIsAdmissibleButInconsistent()
    {
        // Arrange
        var graph = new Graph(5,
        [
            new Edge(0, 1, 1),
            new Edge(1, 3, 1),
            new Edge(0, 2, 1),
            new Edge(2, 3, 3),
            new Edge(3, 4, 3),
        ]);
        double[] estimates = [0, 4, 0, 0, 0];

        // Act
        var path = graph.FindShortestPath(0, 4, node => estimates[node]);

        // Assert
        path.Distance.ShouldBe(5);
        path.Nodes.ShouldBe([0, 1, 3, 4]);
    }

    [Fact]
    public void FindShortestPath_ShouldCallTheHeuristicAtMostOncePerNode()
    {
        // Arrange
        var grid = new Grid(
            "..#.",
            "..#.",
            "....");
        var calls = new List<int>();

        // Act
        grid.FindShortestPath(0, 3, node =>
        {
            calls.Add(node);

            return grid.Manhattan(node, 3);
        });

        // Assert
        calls.ShouldNotBeEmpty();
        calls.ShouldBeUnique();
    }

    [Fact]
    public void FindShortestPath_ShouldReturnJustTheNode_When_SourceIsTarget()
    {
        // Act
        var path = TestGraphs.Readme().FindShortestPath(2, 2, _ => 0);

        // Assert
        path.Distance.ShouldBe(0);
        path.Nodes.ShouldBe([2]);
    }

    [Fact]
    public void FindShortestPath_ShouldReportNotFound_When_TheTargetIsUnreachable()
    {
        // Act
        var path = TestGraphs.Readme().FindShortestPath(3, 0, _ => 0);

        // Assert
        path.ShouldBeNotFound();
    }

    [Fact]
    public void FindShortestPath_ShouldThrow_When_TheHeuristicIsNull()
    {
        // Act
        Action act = () => TestGraphs.Readme().FindShortestPath(0, 3, heuristic: null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("heuristic");
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void FindShortestPath_ShouldThrow_When_TheWeightIsBelowOneOrNotFinite(double weight)
    {
        // Act
        Action act = () => TestGraphs.Readme().FindShortestPath(0, 3, _ => 0, weight);

        // Assert
        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("heuristicWeight");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void FindShortestPath_ShouldThrow_When_TheHeuristicReturnsANegativeNumberOrNaN(double estimate)
    {
        // Act
        Action act = () => TestGraphs.Readme().FindShortestPath(0, 3, _ => estimate);

        // Assert
        act.ShouldThrow<InvalidOperationException>().Message.ShouldContain("heuristic");
    }

    [Theory]
    [InlineData(-1, 0, "source")]
    [InlineData(0, 4, "target")]
    public void FindShortestPath_ShouldThrow_When_ANodeIsOutsideTheGraph(int source, int target, string paramName)
    {
        // Act
        Action act = () => TestGraphs.Readme().FindShortestPath(source, target, _ => 0);

        // Assert
        act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe(paramName);
    }
}
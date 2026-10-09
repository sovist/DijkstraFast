namespace DijkstraFast.Tests
{
    public class FindShortestPathTests
    {
        [Fact]
        public void FindShortestPath_ShouldPreferACheaperRouteWithMoreHops_When_TheDirectEdgeIsExpensive()
        {
            // Act
            var path = TestGraphs.Readme().FindShortestPath(0, 2);

            // Assert
            path.Found.ShouldBeTrue();
            path.Distance.ShouldBe(3);
            path.Nodes.ShouldBe([0, 1, 2]);
            path.Target.ShouldBe(2);
        }

        [Fact]
        public void FindShortestPath_ShouldTakeTheDirectEdge_When_ItIsCheaper()
        {
            // Arrange
            var graph = new Graph(3,
            [
                new Edge(0, 1, 2),
                new Edge(1, 2, 2),
                new Edge(0, 2, 3),
            ]);

            // Act
            var path = graph.FindShortestPath(0, 2);

            // Assert
            path.Distance.ShouldBe(3);
            path.Nodes.ShouldBe([0, 2]);
        }

        [Fact]
        public void FindShortestPath_ShouldReturnJustTheNodeWithZeroDistance_When_SourceIsTarget()
        {
            // Act
            var path = TestGraphs.Readme().FindShortestPath(2, 2);

            // Assert
            path.Found.ShouldBeTrue();
            path.Distance.ShouldBe(0);
            path.Nodes.ShouldBe([2]);
            path.Target.ShouldBe(2);
        }

        // An unreachable target must be reported as not found, never as a one-node path [target].
        [Fact]
        public void FindShortestPath_ShouldReportNotFound_When_TheTargetIsUnreachable()
        {
            // Arrange
            var graph = new Graph(4, [new Edge(0, 1, 1), new Edge(2, 3, 1)]);

            // Act
            var path = graph.FindShortestPath(0, 3);

            // Assert
            path.ShouldBeNotFound();
            path.ToString().ShouldBe("No path");
        }

        [Fact]
        public void FindShortestPath_ShouldFollowEdgesOnlyInTheirDirection()
        {
            // Arrange
            var graph = new Graph(2, [new Edge(0, 1, 1)]);

            // Act
            var forward = graph.FindShortestPath(0, 1);
            var backward = graph.FindShortestPath(1, 0);

            // Assert
            forward.Found.ShouldBeTrue();
            backward.ShouldBeNotFound();
        }

        [Fact]
        public void FindShortestPath_ShouldUseTheCheapestEdge_When_ThereAreParallelEdges()
        {
            // Arrange
            var graph = new Graph(2,
            [
                new Edge(0, 1, 5),
                new Edge(0, 1, 2),
                new Edge(0, 1, 9),
            ]);

            // Act
            var path = graph.FindShortestPath(0, 1);

            // Assert
            path.Distance.ShouldBe(2);
        }

        [Fact]
        public void FindShortestPath_ShouldIgnoreSelfLoops()
        {
            // Arrange
            var graph = new Graph(2,
            [
                new Edge(0, 0, 0),
                new Edge(0, 1, 1),
                new Edge(1, 1, 0),
            ]);

            // Act
            var path = graph.FindShortestPath(0, 1);

            // Assert
            path.Nodes.ShouldBe([0, 1]);
        }

        [Fact]
        public void FindShortestPath_ShouldTerminate_When_ThereAreZeroCostCycles()
        {
            // Arrange
            var graph = new Graph(4,
            [
                new Edge(0, 1, 0),
                new Edge(1, 2, 0),
                new Edge(2, 1, 0),
                new Edge(2, 0, 0),
                new Edge(2, 3, 1.5),
            ]);

            // Act
            var path = graph.FindShortestPath(0, 3);

            // Assert
            path.Distance.ShouldBe(1.5);
            path.Nodes.ShouldBe([0, 1, 2, 3]);
        }

        [Fact]
        public void FindShortestPath_ShouldNotUseAnEdge_When_ItsCostIsInfinite()
        {
            // Arrange
            var graph = new Graph(2, [new Edge(0, 1, double.PositiveInfinity)]);

            // Act
            var path = graph.FindShortestPath(0, 1);

            // Assert
            path.ShouldBeNotFound();
        }

        // Searches keep no state between calls, so an earlier search can't leak into a later one.
        [Fact]
        public void FindShortestPath_ShouldGiveIndependentResults_When_CalledRepeatedlyOnTheSameGraph()
        {
            // Arrange
            var graph = TestGraphs.Readme();

            // Act
            var first = graph.FindShortestPath(0, 3);
            var unreachable = graph.FindShortestPath(3, 0);
            var second = graph.FindShortestPath(0, 3);
            var other = graph.FindShortestPath(1, 3);

            // Assert
            second.Nodes.ShouldBe(first.Nodes);
            second.Distance.ShouldBe(first.Distance);
            unreachable.ShouldBeNotFound();
            other.Nodes.ShouldBe([1, 2, 3]);
        }

        [Fact]
        public void FindShortestPath_ShouldStop_When_TheTargetIsReached()
        {
            // Arrange
            var graph = new RecordingGraph(TestGraphs.Line(1000));

            // Act
            var path = graph.FindShortestPath(0, 2);

            // Assert
            path.Nodes.ShouldBe([0, 1, 2]);
            graph.Expanded.ShouldBe([0, 1]);
        }

        [Fact]
        public void FindShortestPath_ShouldFindThePath_When_ItRunsThroughAMillionNodes()
        {
            // Arrange
            const int nodeCount = 1_000_000;
            var graph = TestGraphs.Line(nodeCount);

            // Act
            var path = graph.FindShortestPath(0, nodeCount - 1);

            // Assert
            path.Distance.ShouldBe(nodeCount - 1);
            path.Nodes.Count.ShouldBe(nodeCount);
            path.Nodes[0].ShouldBe(0);
            path.Target.ShouldBe(nodeCount - 1);
        }

        [Fact]
        public void FindShortestPath_ShouldGiveCorrectResults_When_ManyThreadsSearchTheSameGraph()
        {
            // Arrange
            var random = new Random(12345);
            const int nodeCount = 500;
            var graph = new Graph(nodeCount, Enumerable.Range(0, 3000)
                .Select(_ => new Edge(random.Next(nodeCount), random.Next(nodeCount), random.Next(1, 100))));
            var queries = Enumerable.Range(0, 400).Select(_ => (Source: random.Next(nodeCount), Target: random.Next(nodeCount))).ToArray();
            var expected = queries.Select(q => graph.FindShortestPath(q.Source, q.Target).Distance).ToArray();
            var actual = new double[queries.Length];

            // Act
            Parallel.For(0, queries.Length, i => actual[i] = graph.FindShortestPath(queries[i].Source, queries[i].Target).Distance);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public void ToString_ShouldListThePathAndItsDistance()
        {
            // Act
            var text = TestGraphs.Readme().FindShortestPath(0, 3).ToString();

            // Assert
            text.ShouldBe("0 -> 1 -> 2 -> 3 (distance 4)");
        }

        [Fact]
        public void FindShortestPath_ShouldThrow_When_TheGraphIsNull()
        {
            // Act
            Action act = () => ((IGraph)null!).FindShortestPath(0, 0);

            // Assert
            act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("graph");
        }

        [Theory]
        [InlineData(-1, 0, "source")]
        [InlineData(4, 0, "source")]
        [InlineData(0, -1, "target")]
        [InlineData(0, 4, "target")]
        public void FindShortestPath_ShouldThrow_When_ANodeIsOutsideTheGraph(int source, int target, string paramName)
        {
            // Arrange
            var graph = TestGraphs.Readme();

            // Act
            Action act = () => graph.FindShortestPath(source, target);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe(paramName);
        }
    }
}
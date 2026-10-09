namespace DijkstraFast.Tests
{
    public class GraphTests
    {
        [Fact]
        public void Constructor_ShouldRecordNodeAndEdgeCounts()
        {
            // Act
            var graph = TestGraphs.Readme();

            // Assert
            graph.NodeCount.ShouldBe(4);
            graph.EdgeCount.ShouldBe(4);
        }

        [Fact]
        public void GetOutgoingEdges_ShouldReturnTheEdgesLeavingANodeInInputOrder()
        {
            // Arrange
            var graph = new Graph(3,
            [
                new Edge(1, 2, 7),
                new Edge(0, 2, 1),
                new Edge(1, 0, 3),
                new Edge(1, 1, 4),
            ]);

            // Act & Assert
            graph.GetOutgoingEdges(0).ShouldBe([new Edge(0, 2, 1)]);
            graph.GetOutgoingEdges(1).ShouldBe([new Edge(1, 2, 7), new Edge(1, 0, 3), new Edge(1, 1, 4)]);
            graph.GetOutgoingEdges(2).ShouldBeEmpty();
        }

        [Fact]
        public void Constructor_ShouldAllowAnEmptyGraph()
        {
            // Act
            var graph = new Graph(0, []);

            // Assert
            graph.NodeCount.ShouldBe(0);
            graph.EdgeCount.ShouldBe(0);
        }

        [Fact]
        public void Constructor_ShouldAcceptEdges_When_TheyComeFromALazySequence()
        {
            // Arrange
            static IEnumerable<Edge> Edges()
            {
                yield return new Edge(0, 1, 1);
                yield return new Edge(1, 2, 1);
            }

            // Act
            var graph = new Graph(3, Edges());

            // Assert
            graph.EdgeCount.ShouldBe(2);
            graph.FindShortestPath(0, 2).Distance.ShouldBe(2);
        }

        [Fact]
        public void Constructor_ShouldCopyTheEdges_When_TheInputArrayChangesLater()
        {
            // Arrange
            Edge[] edges = [new Edge(0, 1, 1)];
            var graph = new Graph(2, edges);

            // Act
            edges[0] = new Edge(1, 0, 1);

            // Assert
            graph.GetOutgoingEdges(0).ShouldBe([new Edge(0, 1, 1)]);
        }

        [Fact]
        public void Constructor_ShouldThrow_When_NodeCountIsNegative()
        {
            // Act
            Action act = () => new Graph(-1, []);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("nodeCount");
        }

        [Fact]
        public void Constructor_ShouldThrow_When_EdgesAreNull()
        {
            // Act
            Action act = () => new Graph(1, null!);

            // Assert
            act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("edges");
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(3, 0)]
        [InlineData(0, 3)]
        public void Constructor_ShouldThrow_When_AnEdgeRefersToANodeOutsideTheGraph(int from, int to)
        {
            // Act
            Action act = () => new Graph(3, [new Edge(from, to, 1)]);

            // Assert
            act.ShouldThrow<ArgumentException>().ParamName.ShouldBe("edges");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void GetOutgoingEdges_ShouldThrow_When_TheNodeIsOutsideTheGraph(int node)
        {
            // Arrange
            var graph = TestGraphs.Readme();

            // Act
            Action act = () => graph.GetOutgoingEdges(node);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("node");
        }

        [Fact]
        public void Reverse_ShouldFlipEveryEdgeAndKeepCosts()
        {
            // Act
            var reversed = TestGraphs.Readme().Reverse();

            // Assert
            reversed.NodeCount.ShouldBe(4);
            reversed.EdgeCount.ShouldBe(4);
            reversed.GetOutgoingEdges(0).ShouldBeEmpty();
            reversed.GetOutgoingEdges(1).ShouldBe([new Edge(1, 0, 1)]);
            reversed.GetOutgoingEdges(2).ShouldBe([new Edge(2, 1, 2), new Edge(2, 0, 5)], ignoreOrder: true);
            reversed.GetOutgoingEdges(3).ShouldBe([new Edge(3, 2, 1)]);
        }

        [Fact]
        public void Reverse_ShouldFindTheCheapestPathsIntoANode_When_Searched()
        {
            // Act
            var tree = TestGraphs.Readme().Reverse().FindShortestPathsFrom(3);

            // Assert
            tree.DistanceTo(0).ShouldBe(4);
            tree.PathTo(0).Nodes.ShouldBe([3, 2, 1, 0]);
        }
    }
}
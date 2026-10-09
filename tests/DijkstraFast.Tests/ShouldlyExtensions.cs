namespace DijkstraFast.Tests
{
    internal static class ShouldlyExtensions
    {
        /// <summary>
        /// Asserts that <paramref name="path"/> runs from <paramref name="source"/> to <paramref name="target"/>
        /// along existing edges, and that its <see cref="PathResult.Distance"/> is exactly the cost of those
        /// edges, taking the cheapest edge wherever there are parallel ones.
        /// </summary>
        public static void ShouldBeValidPath(this PathResult path, IReadOnlyList<Edge> edges, int source, int target)
        {
            path.Found.ShouldBeTrue();
            path.Nodes[0].ShouldBe(source);
            path.Target.ShouldBe(target);

            var total = 0.0;

            for (var i = 0; i + 1 < path.Nodes.Count; i++)
            {
                var from = path.Nodes[i];
                var to = path.Nodes[i + 1];
                var costs = edges.Where(e => e.From == from && e.To == to).Select(e => e.Cost).ToList();

                costs.ShouldNotBeEmpty($"The path uses {from} -> {to}, which is not an edge.");
                total += costs.Min();
            }

            path.Distance.ShouldBe(total);
        }

        /// <summary>Asserts that <paramref name="path"/> is the "no path" result.</summary>
        public static void ShouldBeNotFound(this PathResult path)
        {
            path.Found.ShouldBeFalse();
            path.Nodes.ShouldBeEmpty();
            path.Distance.ShouldBe(double.PositiveInfinity);
            path.Target.ShouldBe(-1);
        }
    }
}
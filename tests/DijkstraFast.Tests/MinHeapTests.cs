namespace DijkstraFast.Tests
{
    public class MinHeapTests
    {
        [Fact]
        public void TryPop_ShouldReturnFalse_When_TheHeapIsEmpty()
        {
            // Arrange
            var heap = new MinHeap();

            // Act
            var popped = heap.TryPop(out _, out _);

            // Assert
            popped.ShouldBeFalse();
            heap.Count.ShouldBe(0);
        }

        [Fact]
        public void TryPop_ShouldReturnEntriesInOrderOfPriority()
        {
            // Arrange
            var random = new Random(7);
            var heap = new MinHeap(capacity: 1);
            var priorities = Enumerable.Range(0, 1000).Select(_ => (double)random.Next(100)).ToArray();

            for (var node = 0; node < priorities.Length; node++)
            {
                heap.Push(node, priorities[node]);
            }

            // Act
            var popped = new List<double>();

            while (heap.TryPop(out var node, out var priority))
            {
                priority.ShouldBe(priorities[node]);

                popped.Add(priority);
            }

            // Assert
            popped.ShouldBe(priorities.OrderBy(p => p));
        }

        [Fact]
        public void TryPop_ShouldReturnTheSmallestEntry_When_PushesAndPopsAreInterleaved()
        {
            // Arrange
            var random = new Random(11);
            var heap = new MinHeap();
            var reference = new List<double>();

            // Act & Assert
            for (var step = 0; step < 5000; step++)
            {
                if (reference.Count == 0 || random.Next(3) > 0)
                {
                    double priority = random.Next(1000);

                    heap.Push(step, priority);
                    reference.Add(priority);
                }
                else
                {
                    heap.TryPop(out _, out var priority).ShouldBeTrue();
                    priority.ShouldBe(reference.Min());

                    reference.Remove(priority);
                }

                heap.Count.ShouldBe(reference.Count);
            }
        }
    }
}
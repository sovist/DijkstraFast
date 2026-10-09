using System.Globalization;

namespace DijkstraFast.Tests
{
    public class EdgeTests
    {
        [Fact]
        public void Constructor_ShouldStoreEndpointsAndCost()
        {
            // Act
            var edge = new Edge(1, 2, 2.5);

            // Assert
            edge.From.ShouldBe(1);
            edge.To.ShouldBe(2);
            edge.Cost.ShouldBe(2.5);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(double.MaxValue)]
        [InlineData(double.PositiveInfinity)]
        public void Constructor_ShouldAcceptCost_When_ItIsZeroOrPositive(double cost)
        {
            // Act
            var edge = new Edge(0, 1, cost);

            // Assert
            edge.Cost.ShouldBe(cost);
        }

        [Theory]
        [InlineData(-0.001)]
        [InlineData(-1.0)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.NaN)]
        public void Constructor_ShouldThrow_When_CostIsNegativeOrNaN(double cost)
        {
            // Act
            Action act = () => new Edge(0, 1, cost);

            // Assert
            act.ShouldThrow<ArgumentOutOfRangeException>().ParamName.ShouldBe("cost");
        }

        [Fact]
        public void Equals_ShouldBeTrue_When_EndpointsAndCostMatch()
        {
            // Arrange
            var a = new Edge(1, 2, 3);
            var b = new Edge(1, 2, 3);

            // Assert
            a.ShouldBe(b);
            (a == b).ShouldBeTrue();
            (a != b).ShouldBeFalse();
            a.Equals((object)b).ShouldBeTrue();
            a.GetHashCode().ShouldBe(b.GetHashCode());
        }

        [Theory]
        [InlineData(9, 2, 3.0)]
        [InlineData(1, 9, 3.0)]
        [InlineData(1, 2, 9.0)]
        public void Equals_ShouldBeFalse_When_AnyFieldDiffers(int from, int to, double cost)
        {
            // Arrange
            var a = new Edge(1, 2, 3);
            var b = new Edge(from, to, cost);

            // Assert
            a.ShouldNotBe(b);
            (a != b).ShouldBeTrue();
        }

        [Fact]
        public void Equals_ShouldBeFalse_When_ComparedWithNullOrAnotherType()
        {
            // Arrange
            var edge = new Edge(1, 2, 3);

            // Assert
            edge.Equals(null).ShouldBeFalse();
            edge.Equals("1 -> 2 (3)").ShouldBeFalse();
        }

        [Fact]
        public void ToString_ShouldUseInvariantCulture_When_CurrentCultureUsesDecimalComma()
        {
            // Arrange
            var original = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            try
            {
                // Act
                var text = new Edge(1, 2, 2.5).ToString();

                // Assert
                text.ShouldBe("1 -> 2 (2.5)");
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
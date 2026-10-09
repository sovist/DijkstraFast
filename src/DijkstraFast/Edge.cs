using System;
using System.Globalization;

namespace DijkstraFast
{
    /// <summary>
    /// A directed edge with a non-negative traversal cost.
    /// </summary>
    public readonly struct Edge : IEquatable<Edge>
    {
        /// <summary>
        /// Creates an edge leading from <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="from">The node the edge leaves.</param>
        /// <param name="to">The node the edge enters.</param>
        /// <param name="cost">The cost of traversing the edge. Must be zero or positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cost"/> is negative or NaN.</exception>
        public Edge(int from, int to, double cost)
        {
            // Written as !(cost >= 0) so that NaN is rejected too.
            if (!(cost >= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(cost), cost, "Edge cost must be zero or positive.");
            }

            From = from;
            To = to;
            Cost = cost;
        }

        /// <summary>The node the edge leaves.</summary>
        public int From { get; }

        /// <summary>The node the edge enters.</summary>
        public int To { get; }

        /// <summary>The cost of traversing the edge.</summary>
        public double Cost { get; }

        /// <inheritdoc />
        public bool Equals(Edge other)
        {
            return From == other.From && To == other.To && Cost.Equals(other.Cost);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is Edge other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = From;

                hash = (hash * 397) ^ To;
                hash = (hash * 397) ^ Cost.GetHashCode();

                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} -> {1} ({2})", From, To, Cost);
        }

        /// <summary>Returns whether two edges have the same endpoints and cost.</summary>
        public static bool operator ==(Edge left, Edge right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns whether two edges differ in endpoints or cost.</summary>
        public static bool operator !=(Edge left, Edge right)
        {
            return !left.Equals(right);
        }
    }
}
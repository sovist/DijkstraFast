using System.Collections.Generic;

namespace DijkstraFast
{
    /// <summary>
    /// A directed graph whose nodes are numbered from 0 to <see cref="NodeCount"/> - 1.
    /// </summary>
    /// <remarks>
    /// Implement this interface to search a graph you don't want to build up front, such as a grid
    /// or a graph whose edges are computed on demand. For a fixed list of edges, use <see cref="Graph"/>,
    /// which is faster to search.
    /// </remarks>
    public interface IGraph
    {
        /// <summary>The number of nodes in the graph.</summary>
        int NodeCount { get; }

        /// <summary>
        /// Returns the edges leaving <paramref name="node"/>.
        /// </summary>
        /// <param name="node">A node index between 0 and <see cref="NodeCount"/> - 1.</param>
        /// <returns>
        /// The outgoing edges. Each edge's <see cref="Edge.From"/> must equal <paramref name="node"/>,
        /// and each <see cref="Edge.To"/> must be a valid node index.
        /// </returns>
        IEnumerable<Edge> GetOutgoingEdges(int node);
    }
}
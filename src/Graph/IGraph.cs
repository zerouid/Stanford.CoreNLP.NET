using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Graph
{
	/// <author>Sonal Gupta</author>
	/// <?/>
	/// <?/>
	public interface IGraph<V, E>
	{
		/// <summary>Adds vertices (if not already in the graph) and the edge between them.</summary>
		/// <remarks>
		/// Adds vertices (if not already in the graph) and the edge between them.
		/// (If the graph is undirected, the choice of which vertex to call
		/// source and dest is arbitrary.)
		/// </remarks>
		/// <param name="source"/>
		/// <param name="dest"/>
		/// <param name="data"/>
		void Add(V source, V dest, E data);

		/// <summary>For adding a zero degree vertex</summary>
		/// <param name="v"/>
		bool AddVertex(V v);

		bool RemoveEdges(V source, V dest);

		bool RemoveEdge(V source, V dest, E data);

		/// <summary>remove a vertex (and its edges) from the graph.</summary>
		/// <param name="vertex"/>
		/// <returns>true if successfully removes the node</returns>
		bool RemoveVertex(V vertex);

		bool RemoveVertices(ICollection<V> vertices);

		int GetNumVertices();

		/// <summary>for undirected graph, it is just the edges from the node</summary>
		/// <param name="v"/>
		IList<E> GetOutgoingEdges(V v);

		/// <summary>for undirected graph, it is just the edges from the node</summary>
		/// <param name="v"/>
		IList<E> GetIncomingEdges(V v);

		int GetNumEdges();

		/// <summary>for undirected graph, it is just the neighbors</summary>
		/// <param name="vertex"/>
		ICollection<V> GetParents(V vertex);

		/// <summary>for undirected graph, it is just the neighbors</summary>
		/// <param name="vertex"/>
		ICollection<V> GetChildren(V vertex);

		ICollection<V> GetNeighbors(V v);

		/// <summary>clears the graph, removes all edges and nodes</summary>
		void Clear();

		bool ContainsVertex(V v);

		/// <summary>only checks if there is an edge from source to dest.</summary>
		/// <remarks>
		/// only checks if there is an edge from source to dest. To check if it is
		/// connected in either direction, use isNeighbor
		/// </remarks>
		/// <param name="source"/>
		/// <param name="dest"/>
		bool IsEdge(V source, V dest);

		bool IsNeighbor(V source, V dest);

		ICollection<V> GetAllVertices();

		IList<E> GetAllEdges();

		/// <summary>False if there are any vertices in the graph, true otherwise.</summary>
		/// <remarks>
		/// False if there are any vertices in the graph, true otherwise. Does not care
		/// about the number of edges.
		/// </remarks>
		bool IsEmpty();

		/// <summary>Deletes nodes with zero incoming and zero outgoing edges</summary>
		void RemoveZeroDegreeNodes();

		IList<E> GetEdges(V source, V dest);

		/// <summary>for undirected graph, it should just be the degree</summary>
		/// <param name="vertex"/>
		int GetInDegree(V vertex);

		int GetOutDegree(V vertex);

		IList<ICollection<V>> GetConnectedComponents();
	}
}

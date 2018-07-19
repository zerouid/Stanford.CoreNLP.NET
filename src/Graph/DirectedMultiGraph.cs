using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Graph
{
	/// <summary>Simple graph library; this is directed for now.</summary>
	/// <remarks>
	/// Simple graph library; this is directed for now. This class focuses on time
	/// efficiency rather than memory efficiency.
	/// </remarks>
	/// <author>sonalg</author>
	/// <author>John Bauer</author>
	/// <?/>
	/// <?/>
	[System.Serializable]
	public class DirectedMultiGraph<V, E> : IGraph<V, E>
	{
		internal readonly IDictionary<V, IDictionary<V, IList<E>>> outgoingEdges;

		internal readonly IDictionary<V, IDictionary<V, IList<E>>> incomingEdges;

		internal readonly MapFactory<V, IDictionary<V, IList<E>>> outerMapFactory;

		internal readonly MapFactory<V, IList<E>> innerMapFactory;

		public DirectedMultiGraph()
			: this(MapFactory.HashMapFactory<V, IDictionary<V, IList<E>>>(), MapFactory.HashMapFactory<V, IList<E>>())
		{
		}

		public DirectedMultiGraph(MapFactory<V, IDictionary<V, IList<E>>> outerMapFactory, MapFactory<V, IList<E>> innerMapFactory)
		{
			/* Serializable */
			this.outerMapFactory = outerMapFactory;
			this.innerMapFactory = innerMapFactory;
			this.outgoingEdges = outerMapFactory.NewMap();
			this.incomingEdges = outerMapFactory.NewMap();
		}

		/// <summary>Creates a copy of the given graph.</summary>
		/// <remarks>
		/// Creates a copy of the given graph. This will copy the entire data
		/// structure (this may be slow!), but will not copy any of the edge
		/// or vertex objects.
		/// </remarks>
		/// <param name="graph">The graph to copy into this object.</param>
		public DirectedMultiGraph(Edu.Stanford.Nlp.Graph.DirectedMultiGraph<V, E> graph)
			: this(graph.outerMapFactory, graph.innerMapFactory)
		{
			foreach (KeyValuePair<V, IDictionary<V, IList<E>>> map in graph.outgoingEdges)
			{
				IDictionary<V, IList<E>> edgesCopy = innerMapFactory.NewMap();
				foreach (KeyValuePair<V, IList<E>> entry in map.Value)
				{
					edgesCopy[entry.Key] = Generics.NewArrayList(entry.Value);
				}
				this.outgoingEdges[map.Key] = edgesCopy;
			}
			foreach (KeyValuePair<V, IDictionary<V, IList<E>>> map_1 in graph.incomingEdges)
			{
				IDictionary<V, IList<E>> edgesCopy = innerMapFactory.NewMap();
				foreach (KeyValuePair<V, IList<E>> entry in map_1.Value)
				{
					edgesCopy[entry.Key] = Generics.NewArrayList(entry.Value);
				}
				this.incomingEdges[map_1.Key] = edgesCopy;
			}
		}

		/// <summary>Be careful hashing these.</summary>
		/// <remarks>
		/// Be careful hashing these. They are mutable objects, and changing the object
		/// will throw off the hash code, messing up your hash table
		/// </remarks>
		public override int GetHashCode()
		{
			return outgoingEdges.GetHashCode();
		}

		public override bool Equals(object that)
		{
			if (that == this)
			{
				return true;
			}
			if (!(that is Edu.Stanford.Nlp.Graph.DirectedMultiGraph))
			{
				return false;
			}
			return outgoingEdges.Equals(((Edu.Stanford.Nlp.Graph.DirectedMultiGraph<object, object>)that).outgoingEdges);
		}

		/// <summary>For adding a zero degree vertex</summary>
		/// <param name="v"/>
		public virtual bool AddVertex(V v)
		{
			if (outgoingEdges.Contains(v))
			{
				return false;
			}
			outgoingEdges[v] = innerMapFactory.NewMap();
			incomingEdges[v] = innerMapFactory.NewMap();
			return true;
		}

		private IDictionary<V, IList<E>> GetOutgoingEdgesMap(V v)
		{
			IDictionary<V, IList<E>> map = outgoingEdges[v];
			if (map == null)
			{
				map = innerMapFactory.NewMap();
				outgoingEdges[v] = map;
				incomingEdges[v] = innerMapFactory.NewMap();
			}
			return map;
		}

		private IDictionary<V, IList<E>> GetIncomingEdgesMap(V v)
		{
			IDictionary<V, IList<E>> map = incomingEdges[v];
			if (map == null)
			{
				outgoingEdges[v] = innerMapFactory.NewMap();
				map = innerMapFactory.NewMap();
				incomingEdges[v] = map;
			}
			return map;
		}

		/// <summary>adds vertices (if not already in the graph) and the edge between them</summary>
		/// <param name="source"/>
		/// <param name="dest"/>
		/// <param name="data"/>
		public virtual void Add(V source, V dest, E data)
		{
			IDictionary<V, IList<E>> outgoingMap = GetOutgoingEdgesMap(source);
			IDictionary<V, IList<E>> incomingMap = GetIncomingEdgesMap(dest);
			IList<E> outgoingList = outgoingMap[dest];
			if (outgoingList == null)
			{
				outgoingList = new List<E>();
				outgoingMap[dest] = outgoingList;
			}
			IList<E> incomingList = incomingMap[source];
			if (incomingList == null)
			{
				incomingList = new List<E>();
				incomingMap[source] = incomingList;
			}
			outgoingList.Add(data);
			incomingList.Add(data);
		}

		public virtual bool RemoveEdges(V source, V dest)
		{
			if (!outgoingEdges.Contains(source))
			{
				return false;
			}
			if (!incomingEdges.Contains(dest))
			{
				return false;
			}
			if (!outgoingEdges[source].Contains(dest))
			{
				return false;
			}
			Sharpen.Collections.Remove(outgoingEdges[source], dest);
			Sharpen.Collections.Remove(incomingEdges[dest], source);
			return true;
		}

		public virtual bool RemoveEdge(V source, V dest, E data)
		{
			if (!outgoingEdges.Contains(source))
			{
				return false;
			}
			if (!incomingEdges.Contains(dest))
			{
				return false;
			}
			if (!outgoingEdges[source].Contains(dest))
			{
				return false;
			}
			bool foundOut = outgoingEdges.Contains(source) && outgoingEdges[source].Contains(dest) && outgoingEdges[source][dest].Remove(data);
			bool foundIn = incomingEdges.Contains(dest) && incomingEdges[dest].Contains(source) && incomingEdges[dest][source].Remove(data);
			if (foundOut && !foundIn)
			{
				throw new AssertionError("Edge found in outgoing but not incoming");
			}
			if (foundIn && !foundOut)
			{
				throw new AssertionError("Edge found in incoming but not outgoing");
			}
			// TODO: cut down the number of .get calls
			if (outgoingEdges.Contains(source) && (!outgoingEdges[source].Contains(dest) || outgoingEdges[source][dest].Count == 0))
			{
				Sharpen.Collections.Remove(outgoingEdges[source], dest);
			}
			if (incomingEdges.Contains(dest) && (!incomingEdges[dest].Contains(source) || incomingEdges[dest][source].Count == 0))
			{
				Sharpen.Collections.Remove(incomingEdges[dest], source);
			}
			return foundOut;
		}

		/// <summary>remove a vertex (and its edges) from the graph.</summary>
		/// <param name="vertex"/>
		/// <returns>true if successfully removes the node</returns>
		public virtual bool RemoveVertex(V vertex)
		{
			if (!outgoingEdges.Contains(vertex))
			{
				return false;
			}
			foreach (V other in outgoingEdges[vertex].Keys)
			{
				Sharpen.Collections.Remove(incomingEdges[other], vertex);
			}
			foreach (V other_1 in incomingEdges[vertex].Keys)
			{
				Sharpen.Collections.Remove(outgoingEdges[other_1], vertex);
			}
			Sharpen.Collections.Remove(outgoingEdges, vertex);
			Sharpen.Collections.Remove(incomingEdges, vertex);
			return true;
		}

		public virtual bool RemoveVertices(ICollection<V> vertices)
		{
			bool changed = false;
			foreach (V v in vertices)
			{
				if (RemoveVertex(v))
				{
					changed = true;
				}
			}
			return changed;
		}

		public virtual int GetNumVertices()
		{
			return outgoingEdges.Count;
		}

		public virtual IList<E> GetOutgoingEdges(V v)
		{
			if (!outgoingEdges.Contains(v))
			{
				//noinspection unchecked
				return Java.Util.Collections.EmptyList();
			}
			return CollectionUtils.Flatten(outgoingEdges[v].Values);
		}

		public virtual IList<E> GetIncomingEdges(V v)
		{
			if (!incomingEdges.Contains(v))
			{
				//noinspection unchecked
				return Java.Util.Collections.EmptyList();
			}
			return CollectionUtils.Flatten(incomingEdges[v].Values);
		}

		public virtual int GetNumEdges()
		{
			int count = 0;
			foreach (KeyValuePair<V, IDictionary<V, IList<E>>> sourceEntry in outgoingEdges)
			{
				foreach (KeyValuePair<V, IList<E>> destEntry in sourceEntry.Value)
				{
					count += destEntry.Value.Count;
				}
			}
			return count;
		}

		public virtual ICollection<V> GetParents(V vertex)
		{
			IDictionary<V, IList<E>> parentMap = incomingEdges[vertex];
			if (parentMap == null)
			{
				return null;
			}
			return Java.Util.Collections.UnmodifiableSet(parentMap.Keys);
		}

		public virtual ICollection<V> GetChildren(V vertex)
		{
			IDictionary<V, IList<E>> childMap = outgoingEdges[vertex];
			if (childMap == null)
			{
				return null;
			}
			return Java.Util.Collections.UnmodifiableSet(childMap.Keys);
		}

		/// <summary>Gets both parents and children nodes</summary>
		/// <param name="v"/>
		public virtual ICollection<V> GetNeighbors(V v)
		{
			// TODO: pity we have to copy the sets... is there a combination set?
			ICollection<V> children = GetChildren(v);
			ICollection<V> parents = GetParents(v);
			if (children == null && parents == null)
			{
				return null;
			}
			ICollection<V> neighbors = innerMapFactory.NewSet();
			Sharpen.Collections.AddAll(neighbors, children);
			Sharpen.Collections.AddAll(neighbors, parents);
			return neighbors;
		}

		/// <summary>clears the graph, removes all edges and nodes</summary>
		public virtual void Clear()
		{
			incomingEdges.Clear();
			outgoingEdges.Clear();
		}

		public virtual bool ContainsVertex(V v)
		{
			return outgoingEdges.Contains(v);
		}

		/// <summary>only checks if there is an edge from source to dest.</summary>
		/// <remarks>
		/// only checks if there is an edge from source to dest. To check if it is
		/// connected in either direction, use isNeighbor
		/// </remarks>
		/// <param name="source"/>
		/// <param name="dest"/>
		public virtual bool IsEdge(V source, V dest)
		{
			IDictionary<V, IList<E>> childrenMap = outgoingEdges[source];
			if (childrenMap == null || childrenMap.IsEmpty())
			{
				return false;
			}
			IList<E> edges = childrenMap[dest];
			if (edges == null || edges.IsEmpty())
			{
				return false;
			}
			return edges.Count > 0;
		}

		public virtual bool IsNeighbor(V source, V dest)
		{
			return IsEdge(source, dest) || IsEdge(dest, source);
		}

		public virtual ICollection<V> GetAllVertices()
		{
			return Java.Util.Collections.UnmodifiableSet(outgoingEdges.Keys);
		}

		public virtual IList<E> GetAllEdges()
		{
			IList<E> edges = new List<E>();
			foreach (IDictionary<V, IList<E>> e in outgoingEdges.Values)
			{
				foreach (IList<E> ee in e.Values)
				{
					Sharpen.Collections.AddAll(edges, ee);
				}
			}
			return edges;
		}

		/// <summary>False if there are any vertices in the graph, true otherwise.</summary>
		/// <remarks>
		/// False if there are any vertices in the graph, true otherwise. Does not care
		/// about the number of edges.
		/// </remarks>
		public virtual bool IsEmpty()
		{
			return outgoingEdges.IsEmpty();
		}

		/// <summary>Deletes nodes with zero incoming and zero outgoing edges</summary>
		public virtual void RemoveZeroDegreeNodes()
		{
			IList<V> toDelete = new List<V>();
			foreach (V vertex in outgoingEdges.Keys)
			{
				if (outgoingEdges[vertex].IsEmpty() && incomingEdges[vertex].IsEmpty())
				{
					toDelete.Add(vertex);
				}
			}
			foreach (V vertex_1 in toDelete)
			{
				Sharpen.Collections.Remove(outgoingEdges, vertex_1);
				Sharpen.Collections.Remove(incomingEdges, vertex_1);
			}
		}

		public virtual IList<E> GetEdges(V source, V dest)
		{
			IDictionary<V, IList<E>> childrenMap = outgoingEdges[source];
			if (childrenMap == null)
			{
				return Java.Util.Collections.EmptyList();
			}
			IList<E> edges = childrenMap[dest];
			if (edges == null)
			{
				return Java.Util.Collections.EmptyList();
			}
			return Java.Util.Collections.UnmodifiableList(edges);
		}

		/// <summary>direction insensitive (the paths can go "up" or through the parents)</summary>
		public virtual IList<V> GetShortestPath(V node1, V node2)
		{
			if (!outgoingEdges.Contains(node1) || !outgoingEdges.Contains(node2))
			{
				return null;
			}
			return GetShortestPath(node1, node2, false);
		}

		public virtual IList<E> GetShortestPathEdges(V node1, V node2)
		{
			return ConvertPath(GetShortestPath(node1, node2), false);
		}

		/// <summary>can specify the direction sensitivity</summary>
		/// <param name="node1"/>
		/// <param name="node2"/>
		/// <param name="directionSensitive">- whether the path can go through the parents</param>
		/// <returns>the list of nodes you get through to get there</returns>
		public virtual IList<V> GetShortestPath(V node1, V node2, bool directionSensitive)
		{
			if (!outgoingEdges.Contains(node1) || !outgoingEdges.Contains(node2))
			{
				return null;
			}
			return DijkstraShortestPath.GetShortestPath(this, node1, node2, directionSensitive);
		}

		public virtual IList<E> GetShortestPathEdges(V node1, V node2, bool directionSensitive)
		{
			return ConvertPath(GetShortestPath(node1, node2, directionSensitive), directionSensitive);
		}

		public virtual IList<E> ConvertPath(IList<V> nodes, bool directionSensitive)
		{
			if (nodes == null)
			{
				return null;
			}
			if (nodes.Count <= 1)
			{
				return Java.Util.Collections.EmptyList();
			}
			IList<E> path = new List<E>();
			IEnumerator<V> nodeIterator = nodes.GetEnumerator();
			V previous = nodeIterator.Current;
			while (nodeIterator.MoveNext())
			{
				V next = nodeIterator.Current;
				E connection = null;
				IList<E> edges = GetEdges(previous, next);
				if (edges.Count == 0 && !directionSensitive)
				{
					edges = GetEdges(next, previous);
				}
				if (edges.Count > 0)
				{
					connection = edges[0];
				}
				else
				{
					throw new ArgumentException("Path given with missing " + "edge connection");
				}
				path.Add(connection);
				previous = next;
			}
			return path;
		}

		public virtual int GetInDegree(V vertex)
		{
			if (!ContainsVertex(vertex))
			{
				return 0;
			}
			int result = 0;
			IDictionary<V, IList<E>> incoming = incomingEdges[vertex];
			foreach (IList<E> edges in incoming.Values)
			{
				result += edges.Count;
			}
			return result;
		}

		public virtual int GetOutDegree(V vertex)
		{
			int result = 0;
			IDictionary<V, IList<E>> outgoing = outgoingEdges[vertex];
			if (outgoing == null)
			{
				return 0;
			}
			foreach (IList<E> edges in outgoing.Values)
			{
				result += edges.Count;
			}
			return result;
		}

		public virtual IList<ICollection<V>> GetConnectedComponents()
		{
			return ConnectedComponents.GetConnectedComponents(this);
		}

		/// <summary>Deletes all duplicate edges.</summary>
		public virtual void DeleteDuplicateEdges()
		{
			foreach (V vertex in GetAllVertices())
			{
				foreach (V vertex2 in outgoingEdges[vertex].Keys)
				{
					IList<E> data = outgoingEdges[vertex][vertex2];
					ICollection<E> deduplicatedData = new TreeSet<E>(data);
					data.Clear();
					Sharpen.Collections.AddAll(data, deduplicatedData);
				}
				foreach (V vertex2_1 in incomingEdges[vertex].Keys)
				{
					IList<E> data = incomingEdges[vertex][vertex2_1];
					ICollection<E> deduplicatedData = new TreeSet<E>(data);
					data.Clear();
					Sharpen.Collections.AddAll(data, deduplicatedData);
				}
			}
		}

		public virtual IEnumerator<E> IncomingEdgeIterator(V vertex)
		{
			return new DirectedMultiGraph.EdgeIterator<V, E>(vertex, incomingEdges, outgoingEdges);
		}

		public virtual IEnumerable<E> IncomingEdgeIterable(V vertex)
		{
			return null;
		}

		public virtual IEnumerator<E> OutgoingEdgeIterator(V vertex)
		{
			return new DirectedMultiGraph.EdgeIterator<V, E>(vertex, outgoingEdges, incomingEdges);
		}

		public virtual IEnumerable<E> OutgoingEdgeIterable(V vertex)
		{
			return null;
		}

		public virtual IEnumerator<E> EdgeIterator()
		{
			return new DirectedMultiGraph.EdgeIterator<V, E>(this);
		}

		public virtual IEnumerable<E> EdgeIterable()
		{
			return null;
		}

		/// <summary>
		/// This class handles either iterating over a single vertex's
		/// connections or over all connections in a graph.
		/// </summary>
		internal class EdgeIterator<V, E> : IEnumerator<E>
		{
			private readonly IDictionary<V, IDictionary<V, IList<E>>> reverseEdges;

			/// <summary>when iterating over the whole graph, this iterates over nodes</summary>
			private IEnumerator<KeyValuePair<V, IDictionary<V, IList<E>>>> vertexIterator;

			/// <summary>for a given node, this iterates over its neighbors</summary>
			private IEnumerator<KeyValuePair<V, IList<E>>> connectionIterator;

			/// <summary>given the neighbor of a node, this iterates over all its connections</summary>
			private IEnumerator<E> edgeIterator;

			private V currentSource = null;

			private V currentTarget = null;

			private E currentEdge = null;

			private bool hasNext = true;

			public EdgeIterator(DirectedMultiGraph<V, E> graph)
			{
				vertexIterator = graph.outgoingEdges.GetEnumerator();
				reverseEdges = graph.incomingEdges;
			}

			public EdgeIterator(V startVertex, IDictionary<V, IDictionary<V, IList<E>>> source, IDictionary<V, IDictionary<V, IList<E>>> reverseEdges)
			{
				currentSource = startVertex;
				IDictionary<V, IList<E>> neighbors = source[startVertex];
				if (neighbors != null)
				{
					vertexIterator = null;
					connectionIterator = neighbors.GetEnumerator();
				}
				this.reverseEdges = reverseEdges;
			}

			public virtual bool MoveNext()
			{
				PrimeIterator();
				return hasNext;
			}

			public virtual E Current
			{
				get
				{
					if (!MoveNext())
					{
						throw new NoSuchElementException("Graph edge iterator exhausted.");
					}
					currentEdge = edgeIterator.Current;
					return currentEdge;
				}
			}

			private void PrimeIterator()
			{
				if (edgeIterator != null && edgeIterator.MoveNext())
				{
					hasNext = true;
				}
				else
				{
					// technically, we shouldn't need to put this here, but let's be safe
					if (connectionIterator != null && connectionIterator.MoveNext())
					{
						KeyValuePair<V, IList<E>> nextConnection = connectionIterator.Current;
						edgeIterator = nextConnection.Value.GetEnumerator();
						currentTarget = nextConnection.Key;
						PrimeIterator();
					}
					else
					{
						if (vertexIterator != null && vertexIterator.MoveNext())
						{
							KeyValuePair<V, IDictionary<V, IList<E>>> nextVertex = vertexIterator.Current;
							connectionIterator = nextVertex.Value.GetEnumerator();
							currentSource = nextVertex.Key;
							PrimeIterator();
						}
						else
						{
							hasNext = false;
						}
					}
				}
			}

			public virtual void Remove()
			{
				if (currentEdge != null)
				{
					reverseEdges[currentTarget][currentSource].Remove(currentEdge);
					edgeIterator.Remove();
					if (reverseEdges[currentTarget][currentSource] != null && reverseEdges[currentTarget][currentSource].Count == 0)
					{
						connectionIterator.Remove();
						Sharpen.Collections.Remove(reverseEdges[currentTarget], currentSource);
						// TODO: may not be necessary to set this to null
						edgeIterator = null;
					}
				}
			}
		}

		/// <summary>Topological sort of the graph.</summary>
		/// <remarks>
		/// Topological sort of the graph.
		/// <br />
		/// This method uses the depth-first search implementation of
		/// topological sort.
		/// Topological sorting only works if the graph is acyclic.
		/// </remarks>
		/// <returns>A sorted list of the vertices</returns>
		/// <exception cref="System.InvalidOperationException">if this graph is not a DAG</exception>
		public virtual IList<V> TopologicalSort()
		{
			IList<V> result = Generics.NewArrayList();
			ICollection<V> temporary = outerMapFactory.NewSet();
			ICollection<V> permanent = outerMapFactory.NewSet();
			foreach (V vertex in GetAllVertices())
			{
				if (!temporary.Contains(vertex))
				{
					TopologicalSortHelper(vertex, temporary, permanent, result);
				}
			}
			Java.Util.Collections.Reverse(result);
			return result;
		}

		private void TopologicalSortHelper(V vertex, ICollection<V> temporary, ICollection<V> permanent, IList<V> result)
		{
			temporary.Add(vertex);
			IDictionary<V, IList<E>> neighborMap = outgoingEdges[vertex];
			if (neighborMap != null)
			{
				foreach (V neighbor in neighborMap.Keys)
				{
					if (permanent.Contains(neighbor))
					{
						continue;
					}
					if (temporary.Contains(neighbor))
					{
						throw new InvalidOperationException("This graph has cycles. Topological sort not possible: " + this.ToString());
					}
					TopologicalSortHelper(neighbor, temporary, permanent, result);
				}
			}
			result.Add(vertex);
			permanent.Add(vertex);
		}

		/// <summary>Cast this multi-graph as a map from vertices, to the outgoing data along edges out of those vertices.</summary>
		/// <returns>A map representation of the graph.</returns>
		public virtual IDictionary<V, IList<E>> ToMap()
		{
			IDictionary<V, IList<E>> map = innerMapFactory.NewMap();
			foreach (V vertex in GetAllVertices())
			{
				map[vertex] = GetOutgoingEdges(vertex);
			}
			return map;
		}

		public override string ToString()
		{
			StringBuilder s = new StringBuilder();
			s.Append("{\n");
			s.Append("Vertices:\n");
			foreach (V vertex in outgoingEdges.Keys)
			{
				s.Append("  ").Append(vertex).Append('\n');
			}
			s.Append("Edges:\n");
			foreach (V source in outgoingEdges.Keys)
			{
				foreach (V dest in outgoingEdges[source].Keys)
				{
					foreach (E edge in outgoingEdges[source][dest])
					{
						s.Append("  ").Append(source).Append(" -> ").Append(dest).Append(" : ").Append(edge).Append('\n');
					}
				}
			}
			s.Append('}');
			return s.ToString();
		}

		private const long serialVersionUID = 609823567298345145L;
	}
}

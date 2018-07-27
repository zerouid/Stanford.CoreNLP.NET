using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Graph
{
	/// <summary>
	/// Finds connected components in the graph, currently uses inefficient list for
	/// variable 'verticesLeft'.
	/// </summary>
	/// <remarks>
	/// Finds connected components in the graph, currently uses inefficient list for
	/// variable 'verticesLeft'. It might give a problem for big graphs
	/// </remarks>
	/// <author>sonalg 08/08/11</author>
	public class ConnectedComponents<V, E>
	{
		public static IList<ICollection<V>> GetConnectedComponents<V, E>(IGraph<V, E> graph)
		{
			IList<ICollection<V>> ccs = new List<ICollection<V>>();
			LinkedList<V> todo = new LinkedList<V>();
			// TODO: why not a set?
			IList<V> verticesLeft = CollectionUtils.ToList(graph.GetAllVertices());
			while (verticesLeft.Count > 0)
			{
				todo.Add(verticesLeft[0]);
				verticesLeft.Remove(0);
				ccs.Add(Bfs(todo, graph, verticesLeft));
			}
			return ccs;
		}

		private static ICollection<V> Bfs<V, E>(LinkedList<V> todo, IGraph<V, E> graph, IList<V> verticesLeft)
		{
			ICollection<V> cc = Generics.NewHashSet();
			while (todo.Count > 0)
			{
				V node = todo.RemoveFirst();
				cc.Add(node);
				foreach (V neighbor in graph.GetNeighbors(node))
				{
					if (verticesLeft.Contains(neighbor))
					{
						cc.Add(neighbor);
						todo.Add(neighbor);
						verticesLeft.Remove(neighbor);
					}
				}
			}
			return cc;
		}
	}
}

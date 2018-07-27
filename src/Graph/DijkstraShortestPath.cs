using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Graph
{
	public class DijkstraShortestPath
	{
		private DijkstraShortestPath()
		{
		}

		// static method only
		public static IList<V> GetShortestPath<V, E>(IGraph<V, E> graph, V node1, V node2, bool directionSensitive)
		{
			if (node1.Equals(node2))
			{
				return Java.Util.Collections.SingletonList(node2);
			}
			ICollection<V> visited = Generics.NewHashSet();
			IDictionary<V, V> previous = Generics.NewHashMap();
			BinaryHeapPriorityQueue<V> unsettledNodes = new BinaryHeapPriorityQueue<V>();
			unsettledNodes.Add(node1, 0);
			while (unsettledNodes.Count > 0)
			{
				double distance = unsettledNodes.GetPriority();
				V u = unsettledNodes.RemoveFirst();
				visited.Add(u);
				if (u.Equals(node2))
				{
					break;
				}
				unsettledNodes.Remove(u);
				ICollection<V> candidates = ((directionSensitive) ? graph.GetChildren(u) : graph.GetNeighbors(u));
				foreach (V candidate in candidates)
				{
					double alt = distance - 1;
					// nodes not already present will have a priority of -inf
					if (alt > unsettledNodes.GetPriority(candidate) && !visited.Contains(candidate))
					{
						unsettledNodes.RelaxPriority(candidate, alt);
						previous[candidate] = u;
					}
				}
			}
			if (!previous.Contains(node2))
			{
				return null;
			}
			List<V> path = new List<V>();
			path.Add(node2);
			V n = node2;
			while (previous.Contains(n))
			{
				path.Add(previous[n]);
				n = previous[n];
			}
			Java.Util.Collections.Reverse(path);
			return path;
		}
	}
}

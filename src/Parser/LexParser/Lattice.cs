using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class Lattice : IEnumerable<LatticeEdge>
	{
		private const long serialVersionUID = 5135076134500512556L;

		private readonly IList<ParserConstraint> constraints;

		private readonly IList<LatticeEdge> edges;

		private readonly ICollection<int> nodes;

		private readonly IDictionary<int, IList<LatticeEdge>> edgeStartsAt;

		private int maxNode = -1;

		public Lattice()
		{
			edges = new List<LatticeEdge>();
			nodes = Generics.NewHashSet();
			constraints = new List<ParserConstraint>();
			edgeStartsAt = Generics.NewHashMap();
		}

		//TODO Do node normalization here
		public virtual void AddEdge(LatticeEdge e)
		{
			nodes.Add(e.start);
			nodes.Add(e.end);
			edges.Add(e);
			if (e.end > maxNode)
			{
				maxNode = e.end;
			}
			if (edgeStartsAt[e.start] == null)
			{
				IList<LatticeEdge> edges = new List<LatticeEdge>();
				edges.Add(e);
				edgeStartsAt[e.start] = edges;
			}
			else
			{
				edgeStartsAt[e.start].Add(e);
			}
		}

		public virtual void AddConstraint(ParserConstraint c)
		{
			constraints.Add(c);
		}

		public virtual int GetNumNodes()
		{
			return nodes.Count;
		}

		public virtual IList<ParserConstraint> GetConstraints()
		{
			return Java.Util.Collections.UnmodifiableList(constraints);
		}

		public virtual int GetNumEdges()
		{
			return edges.Count;
		}

		public virtual IList<LatticeEdge> GetEdgesOverSpan(int start, int end)
		{
			IList<LatticeEdge> allEdges = edgeStartsAt[start];
			IList<LatticeEdge> spanningEdges = new List<LatticeEdge>();
			if (allEdges != null)
			{
				foreach (LatticeEdge e in allEdges)
				{
					if (e.end == end)
					{
						spanningEdges.Add(e);
					}
				}
			}
			return spanningEdges;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(string.Format("[ Lattice: %d edges  %d nodes ]\n", edges.Count, nodes.Count));
			foreach (LatticeEdge e in edges)
			{
				sb.Append("  " + e.ToString() + "\n");
			}
			return sb.ToString();
		}

		public virtual void SetEdge(int id, LatticeEdge e)
		{
			edges.Set(id, e);
		}

		public virtual IEnumerator<LatticeEdge> GetEnumerator()
		{
			return edges.GetEnumerator();
		}

		public virtual void AddBoundary()
		{
			//Log prob of 0.0 since we have to take this transition
			LatticeEdge boundary = new LatticeEdge(LexiconConstants.Boundary, 0.0, maxNode, maxNode + 1);
			AddEdge(boundary);
		}
	}
}

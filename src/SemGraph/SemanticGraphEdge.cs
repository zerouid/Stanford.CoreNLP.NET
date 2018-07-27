using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;




namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>Represents an edge in the dependency graph.</summary>
	/// <remarks>Represents an edge in the dependency graph. Equal only if source, target, and relation are equal.</remarks>
	/// <author>Christopher Cox</author>
	/// <author>Teg Grenager</author>
	/// <seealso cref="SemanticGraph"/>
	[System.Serializable]
	public class SemanticGraphEdge : IComparable<Edu.Stanford.Nlp.Semgraph.SemanticGraphEdge>
	{
		public static bool printOnlyRelation = false;

		private GrammaticalRelation relation;

		private double weight;

		private bool isExtra;

		private readonly IndexedWord source;

		private readonly IndexedWord target;

		/// <param name="source">The source IndexedWord for this edge</param>
		/// <param name="target">The target IndexedWord for this edge</param>
		/// <param name="relation">The relation between the two words represented by this edge</param>
		/// <param name="weight">A score or weight to attach to the edge (not often used)</param>
		/// <param name="isExtra">Whether or not the dependency this edge represents was "extra"</param>
		public SemanticGraphEdge(IndexedWord source, IndexedWord target, GrammaticalRelation relation, double weight, bool isExtra)
		{
			// a hack for displaying SemanticGraph in JGraph.  Should be redone better.
			this.source = source;
			this.target = target;
			this.relation = relation;
			this.weight = weight;
			this.isExtra = isExtra;
		}

		public SemanticGraphEdge(Edu.Stanford.Nlp.Semgraph.SemanticGraphEdge e)
			: this(e.GetSource(), e.GetTarget(), e.GetRelation(), e.GetWeight(), e.IsExtra())
		{
		}

		public override string ToString()
		{
			if (!printOnlyRelation)
			{
				return GetSource() + " -> " + GetTarget() + " (" + GetRelation() + ")";
			}
			else
			{
				return GetRelation().ToString();
			}
		}

		public virtual GrammaticalRelation GetRelation()
		{
			return relation;
		}

		public virtual void SetRelation(GrammaticalRelation relation)
		{
			this.relation = relation;
		}

		public virtual IndexedWord GetSource()
		{
			return source;
		}

		public virtual IndexedWord GetGovernor()
		{
			return GetSource();
		}

		public virtual IndexedWord GetTarget()
		{
			return target;
		}

		public virtual IndexedWord GetDependent()
		{
			return GetTarget();
		}

		public virtual double GetWeight()
		{
			return weight;
		}

		public virtual void SetWeight(double weight)
		{
			this.weight = weight;
		}

		public virtual bool IsExtra()
		{
			return isExtra;
		}

		public virtual void SetIsExtra(bool isExtra)
		{
			this.isExtra = isExtra;
		}

		/// <returns>true if the edges are of the same relation type</returns>
		public virtual bool TypeEquals(Edu.Stanford.Nlp.Semgraph.SemanticGraphEdge e)
		{
			return (this.relation.Equals(e.relation));
		}

		private class SemanticGraphEdgeTargetComparator : IComparator<SemanticGraphEdge>
		{
			public virtual int Compare(SemanticGraphEdge o1, SemanticGraphEdge o2)
			{
				int targetVal = o1.GetTarget().CompareTo(o2.GetTarget());
				if (targetVal != 0)
				{
					return targetVal;
				}
				int sourceVal = o1.GetSource().CompareTo(o2.GetSource());
				if (sourceVal != 0)
				{
					return sourceVal;
				}
				return string.CompareOrdinal(o1.GetRelation().ToString(), o2.GetRelation().ToString());
			}
			// todo: cdm: surely we shouldn't have to do toString() now?
		}

		private static IComparator<SemanticGraphEdge> targetComparator = new SemanticGraphEdge.SemanticGraphEdgeTargetComparator();

		public static IComparator<SemanticGraphEdge> OrderByTargetComparator()
		{
			return targetComparator;
		}

		/// <summary>Compares SemanticGraphEdges.</summary>
		/// <remarks>
		/// Compares SemanticGraphEdges.
		/// Warning: compares on the sources, targets, and then the STRINGS of the relations.
		/// </remarks>
		/// <param name="other">Edge to compare to</param>
		/// <returns>Whether this is smaller, same, or larger</returns>
		public virtual int CompareTo(SemanticGraphEdge other)
		{
			int sourceVal = GetSource().CompareTo(other.GetSource());
			if (sourceVal != 0)
			{
				return sourceVal;
			}
			int targetVal = GetTarget().CompareTo(other.GetTarget());
			if (targetVal != 0)
			{
				return targetVal;
			}
			string thisRelation = GetRelation().ToString();
			string thatRelation = other.GetRelation().ToString();
			return string.CompareOrdinal(thisRelation, thatRelation);
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is SemanticGraphEdge))
			{
				return false;
			}
			SemanticGraphEdge semanticGraphEdge = (SemanticGraphEdge)o;
			if (relation != null)
			{
				bool retFlag = relation.Equals(semanticGraphEdge.relation);
				bool govMatch = GetGovernor().Equals(semanticGraphEdge.GetGovernor());
				bool depMatch = GetDependent().Equals(semanticGraphEdge.GetDependent());
				bool matched = retFlag && govMatch && depMatch;
				return matched;
			}
			//   if (relation != null ? !relation.equals(semanticGraphEdge.relation) : semanticGraphEdge.relation != null) return false;
			return base.Equals(o);
		}

		public override int GetHashCode()
		{
			int result;
			result = (relation != null ? relation.GetHashCode() : 0);
			result = 29 * result + (GetSource() != null ? GetSource().GetHashCode() : 0);
			result = 29 * result + (GetTarget() != null ? GetTarget().GetHashCode() : 0);
			return result;
		}

		private const long serialVersionUID = 2L;
	}
}

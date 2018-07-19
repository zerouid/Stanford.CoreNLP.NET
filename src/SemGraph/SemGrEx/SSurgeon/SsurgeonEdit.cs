using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	public abstract class SsurgeonEdit
	{
		private SsurgeonPattern owningPattern = null;

		/// <summary>
		/// Given a matching instance (via the SemgrexMatcher), performs an in-place
		/// modification on the given SemanticGraph.
		/// </summary>
		public abstract void Evaluate(SemanticGraph sg, SemgrexMatcher sm);

		public abstract string ToEditString();

		// This should be a parseable String representing the edit
		public override string ToString()
		{
			return ToEditString();
		}

		public virtual bool Equals(SsurgeonEdit tgt)
		{
			return this.ToString().Equals(tgt.ToString());
		}

		public virtual SsurgeonPattern GetOwningPattern()
		{
			return owningPattern;
		}

		public virtual void SetOwningPattern(SsurgeonPattern owningPattern)
		{
			this.owningPattern = owningPattern;
		}

		/// <summary>Used to retrieve the named node.</summary>
		/// <remarks>
		/// Used to retrieve the named node.  If not found in the SemgrexMatcher, check the
		/// owning pattern object, as this could've been a created node.
		/// </remarks>
		public virtual IndexedWord GetNamedNode(string nodeName, SemgrexMatcher sm)
		{
			IndexedWord ret = sm.GetNode(nodeName);
			if ((ret == null) && GetOwningPattern() != null)
			{
				return GetOwningPattern().GetNamedNode(nodeName);
			}
			return ret;
		}

		public virtual void AddNamedNode(IndexedWord newNode, string name)
		{
			GetOwningPattern().AddNamedNode(newNode, name);
		}
	}
}

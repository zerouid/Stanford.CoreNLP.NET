using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph.Semgrex;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon.Pred
{
	public abstract class NodeTest : ISsurgPred
	{
		private string matchName = null;

		// This is the named node match in the Semgrex matcher, used to identify node to apply test on
		public abstract string GetID();

		public abstract string GetDisplayName();

		public NodeTest()
		{
		}

		public NodeTest(string matchName)
		{
			this.matchName = matchName;
		}

		/// <exception cref="System.Exception"/>
		public virtual bool Test(SemgrexMatcher matcher)
		{
			return Evaluate(matcher.GetNode(matchName));
		}

		// This is the custom routine to implement
		/// <exception cref="System.Exception"/>
		protected internal abstract bool Evaluate(IndexedWord node);

		// Use this for debugging, and dual re-use of the code outside of Ssurgeon
		/// <exception cref="System.Exception"/>
		public virtual bool Test(IndexedWord node)
		{
			return Evaluate(node);
		}

		public override string ToString()
		{
			StringWriter buf = new StringWriter();
			buf.Write("(node-test :name ");
			buf.Write(GetDisplayName());
			buf.Write(" :id ");
			buf.Write(GetID());
			buf.Write(" :match-name ");
			buf.Write(matchName);
			buf.Write(")");
			return buf.ToString();
		}

		public virtual string GetMatchName()
		{
			return matchName;
		}
	}
}

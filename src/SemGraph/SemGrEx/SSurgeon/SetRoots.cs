using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	/// <summary>Forcibly sets the named nodes to be the new roots.</summary>
	/// <author>Eric Yeh</author>
	public class SetRoots : SsurgeonEdit
	{
		public const string Label = "setRoots";

		internal IList<string> newRootNames;

		public SetRoots(IList<string> newRootNames)
		{
			this.newRootNames = newRootNames;
		}

		public override void Evaluate(SemanticGraph sg, SemgrexMatcher sm)
		{
			IList<IndexedWord> newRoots = new List<IndexedWord>();
			foreach (string name in newRootNames)
			{
				newRoots.Add(GetNamedNode(name, sm));
			}
			sg.SetRoots(newRoots);
		}

		public override string ToEditString()
		{
			StringWriter buf = new StringWriter();
			buf.Write(Label);
			foreach (string name in newRootNames)
			{
				buf.Write("\t");
				buf.Write(name);
			}
			return buf.ToString();
		}

		public static void Main(string[] args)
		{
		}
		// TODO Auto-generated method stub
	}
}

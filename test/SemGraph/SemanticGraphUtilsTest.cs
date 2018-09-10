using System;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Semgraph
{
	/// <author>Sonal Gupta</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SemanticGraphUtilsTest
	{
		internal SemanticGraph graph;

		[NUnit.Framework.Test]
		public virtual void TestCreateSemgrexPattern()
		{
			try
			{
				SemanticGraph graph = SemanticGraph.ValueOf("[ate subj>Bill]");
				Func<IndexedWord, string> transformNode = null;
				string pat = SemanticGraphUtils.SemgrexFromGraphOrderedNodes(graph, null, null, transformNode);
				NUnit.Framework.Assert.AreEqual("{word: ate; tag: null; ner: null}=ate  >subj=E1 {word: bill; tag: null; ner: null}=Bill", pat.Trim());
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}

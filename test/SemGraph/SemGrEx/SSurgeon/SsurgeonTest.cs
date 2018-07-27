using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex.Ssurgeon
{
	[NUnit.Framework.TestFixture]
	public class SsurgeonTest
	{
		/// <summary>Simple test of an Ssurgeon edit script.</summary>
		/// <remarks>
		/// Simple test of an Ssurgeon edit script.  This instances a simple semantic graph,
		/// a semgrex pattern, and then the resulting actions over the named nodes in the
		/// semgrex match.
		/// </remarks>
		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void SimpleTest()
		{
			SemanticGraph sg = SemanticGraph.ValueOf("[mixed/VBN nsubj>[Joe/NNP appos>[bartender/NN det>the/DT]]  dobj>[drink/NN det>a/DT]]");
			SemgrexPattern semgrexPattern = SemgrexPattern.Compile("{}=a1 >appos=e1 {}=a2 <nsubj=e2 {}=a3");
			SsurgeonPattern pattern = new SsurgeonPattern(semgrexPattern);
			System.Console.Out.WriteLine("Start = " + sg.ToCompactString());
			// Find and snip the appos and root to nsubj links
			SsurgeonEdit apposSnip = new RemoveNamedEdge("e1", "a1", "a2");
			pattern.AddEdit(apposSnip);
			SsurgeonEdit nsubjSnip = new RemoveNamedEdge("e2", "a3", "a1");
			pattern.AddEdit(nsubjSnip);
			// Attach Joe to be the nsubj of bartender
			SsurgeonEdit reattachSubj = new AddEdge("a2", "a1", EnglishGrammaticalRelations.NominalSubject);
			pattern.AddEdit(reattachSubj);
			// Attach copula
			IndexedWord isNode = new IndexedWord();
			isNode.Set(typeof(CoreAnnotations.TextAnnotation), "is");
			isNode.Set(typeof(CoreAnnotations.LemmaAnnotation), "is");
			isNode.Set(typeof(CoreAnnotations.OriginalTextAnnotation), "is");
			isNode.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "VBN");
			SsurgeonEdit addCopula = new AddDep("a2", EnglishGrammaticalRelations.Copula, isNode);
			pattern.AddEdit(addCopula);
			// Destroy subgraph
			SsurgeonEdit destroySubgraph = new DeleteGraphFromNode("a3");
			pattern.AddEdit(destroySubgraph);
			// Process and output modified
			ICollection<SemanticGraph> newSgs = pattern.Execute(sg);
			foreach (SemanticGraph newSg in newSgs)
			{
				System.Console.Out.WriteLine("Modified = " + newSg.ToCompactString());
			}
			string firstGraphString = newSgs.GetEnumerator().Current.ToCompactString().Trim();
			NUnit.Framework.Assert.AreEqual(firstGraphString, "[bartender cop>is nsubj>Joe det>the]");
		}
	}
}

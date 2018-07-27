using System;
using System.Collections.Generic;
using System.IO;





namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Unit tests for the GrammaticalStructure family of classes.</summary>
	/// <author>dramage</author>
	/// <author>mcdm</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class GrammaticalStructureTest
	{
		/// <summary>Turn token string into HashSet to abstract over ordering</summary>
		private static HashSet<string> TokenSet(string tokenString)
		{
			Pattern tokenPattern = Pattern.Compile("(\\S+\\(\\S+-\\d+, \\S+-\\d+\\))");
			Matcher tpMatcher = tokenPattern.Matcher(tokenString);
			HashSet<string> tokenSet = new HashSet<string>();
			while (tpMatcher.Find())
			{
				tokenSet.Add(tpMatcher.Group());
			}
			return tokenSet;
		}

		private static HashSet<string> TokenSet(IList<TypedDependency> ds)
		{
			HashSet<string> tokenSet = new HashSet<string>();
			foreach (TypedDependency d in ds)
			{
				tokenSet.Add(d.ToString());
			}
			return tokenSet;
		}

		/// <summary>
		/// Tests that we can extract dependency relations correctly from
		/// some hard-coded trees.
		/// </summary>
		[NUnit.Framework.Test]
		public virtual void TestEnglishDependenciesByTree()
		{
			// the trees to test
			string[] testTrees = new string[] { "((S (NP (NNP Sam)) (VP (VBD died) (NP-TMP (NN today)))))", "(ROOT (S (NP (PRP I)) (VP (VBD saw) (NP (NP (DT the) (NN book)) (SBAR (WHNP (WDT which)) (S (NP (PRP you)) (VP (VBD bought)))))) (. .)))" };
			// the expected dependency answers (basic)
			string[] testAnswers = new string[] { "root(ROOT-0, died-2) nsubj(died-2, Sam-1) tmod(died-2, today-3)", "nsubj(saw-2, I-1) root(ROOT-0, saw-2) det(book-4, the-3) dobj(saw-2, book-4) dobj(bought-7, which-5) ref(book-4, which-5) dobj(bought-7, which-5) nsubj(bought-7, you-6) rcmod(book-4, bought-7)"
				 };
			// the expected dependency answers (collapsed dependencies)
			string[] testAnswersCollapsed = new string[] { "root(ROOT-0, died-2) nsubj(died-2, Sam-1) tmod(died-2, today-3)", "nsubj(saw-2, I-1) root(ROOT-0, saw-2) det(book-4, the-3) dobj(saw-2, book-4) dobj(bought-7, book-4) nsubj(bought-7, you-6) rcmod(book-4, bought-7)"
				 };
			// the expected dependency answers (conjunctions processed)
			string[] testAnswersCCProcessed = new string[] { "root(ROOT-0, died-2) nsubj(died-2, Sam-1) tmod(died-2, today-3)", "nsubj(saw-2, I-1) root(ROOT-0, saw-2) det(book-4, the-3) dobj(saw-2, book-4) dobj(bought-7, book-4) nsubj(bought-7, you-6) rcmod(book-4, bought-7)"
				 };
			for (int i = 0; i < testTrees.Length; i++)
			{
				string testTree = testTrees[i];
				string testAnswer = testAnswers[i];
				string testAnswerCollapsed = testAnswersCollapsed[i];
				string testAnswerCCProcessed = testAnswersCCProcessed[i];
				HashSet<string> testAnswerTokens = TokenSet(testAnswer);
				HashSet<string> testAnswerCollapsedTokens = TokenSet(testAnswerCollapsed);
				HashSet<string> testAnswerCCProcessedTokens = TokenSet(testAnswerCCProcessed);
				Tree tree;
				try
				{
					tree = new PennTreeReader(new StringReader(testTree), new LabeledScoredTreeFactory()).ReadTree();
				}
				catch (IOException e)
				{
					// these trees should all parse correctly
					throw new Exception(e);
				}
				GrammaticalStructure gs = new EnglishGrammaticalStructure(tree);
				NUnit.Framework.Assert.AreEqual("Unexpected basic dependencies for tree " + testTree, testAnswerTokens, TokenSet(gs.TypedDependencies(GrammaticalStructure.Extras.Maximal)));
				NUnit.Framework.Assert.AreEqual("Unexpected collapsed dependencies for tree " + testTree, testAnswerCollapsedTokens, TokenSet(gs.TypedDependenciesCollapsed(GrammaticalStructure.Extras.Maximal)));
				NUnit.Framework.Assert.AreEqual("Unexpected cc-processed dependencies for tree " + testTree, testAnswerCCProcessedTokens, TokenSet(gs.TypedDependenciesCCprocessed(GrammaticalStructure.Extras.Maximal)));
			}
		}
	}
}

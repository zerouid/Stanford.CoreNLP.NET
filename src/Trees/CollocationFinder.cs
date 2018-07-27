using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Finds WordNet collocations in parse trees.</summary>
	/// <remarks>
	/// Finds WordNet collocations in parse trees.  It can restructure
	/// collocations as single words, where the original words are joined by
	/// underscores.  You can test performance by using the "collocations" option
	/// to the TreePrint class.
	/// </remarks>
	/// <author>Chris Cox</author>
	/// <author>Eric Yeh</author>
	public class CollocationFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.CollocationFinder));

		private static bool Debug = false;

		private readonly Tree qTree;

		private readonly IHeadFinder hf;

		private readonly IList<CollocationFinder.Collocation> collocationCollector;

		private readonly IWordNetConnection wnConnect;

		/// <summary>
		/// Construct a new
		/// <c>CollocationFinder</c>
		/// over the
		/// <c>Tree</c>
		/// t.
		/// The default
		/// <see cref="IHeadFinder"/>
		/// is a
		/// <see cref="CollinsHeadFinder"/>
		/// .
		/// </summary>
		/// <param name="t">parse tree</param>
		/// <param name="w">wordnet connection</param>
		public CollocationFinder(Tree t, IWordNetConnection w)
			: this(t, w, new CollinsHeadFinder())
		{
		}

		/// <summary>
		/// Construct a new
		/// <c>CollocationFinder</c>
		/// over the
		/// <c>Tree</c>
		/// t.
		/// </summary>
		/// <param name="t">parse tree</param>
		/// <param name="w">wordnet connection</param>
		/// <param name="hf">
		/// 
		/// <see cref="IHeadFinder"/>
		/// to use
		/// </param>
		public CollocationFinder(Tree t, IWordNetConnection w, IHeadFinder hf)
		{
			CoordinationTransformer transformer = new CoordinationTransformer(hf);
			this.wnConnect = w;
			this.qTree = transformer.TransformTree(t);
			this.collocationCollector = Generics.NewArrayList();
			this.hf = hf;
			this.GetCollocationsList();
			if (Debug)
			{
				log.Info("Collected collocations: " + collocationCollector);
			}
		}

		/// <summary>Returns the "collocations included" parse tree.</summary>
		/// <returns>the mangled tree which applies collocations found in this object.</returns>
		public virtual Tree GetMangledTree()
		{
			return GetMangledTree(qTree);
		}

		private Tree GetMangledTree(Tree t)
		{
			CollocationFinder.Collocation matchingColl = null;
			foreach (Tree child in t.Children())
			{
				child = GetMangledTree(child);
			}
			//boolean additionalCollocationsExist = false;
			foreach (CollocationFinder.Collocation c in collocationCollector)
			{
				// if there are multiple collocations with the same parent node,
				// this will take the longer one
				if (t.Equals(c.parentNode))
				{
					if (matchingColl == null || (c.span.First() <= matchingColl.span.First() && c.span.Second() >= matchingColl.span.Second()))
					{
						matchingColl = c;
						if (Debug)
						{
							Runtime.err.WriteLine("Found matching collocation for tree:");
							t.PennPrint();
							Runtime.err.Write("  head label: " + c.headLabel);
							Runtime.err.WriteLine("; collocation string: " + c.collocationString);
							Runtime.err.WriteLine("  Constituents: " + c.indicesOfConstituentChildren);
						}
					}
				}
			}
			if (matchingColl == null)
			{
				return t;
			}
			else
			{
				if (Debug)
				{
					Runtime.err.WriteLine("Collapsing " + matchingColl);
				}
				Tree[] allChildren = t.Children();
				// get the earliest child in the collocation and store it as first child.
				// delete the rest.
				StringBuilder mutatedString = new StringBuilder(160);
				foreach (int i in matchingColl.indicesOfConstituentChildren)
				{
					string strToAppend = MergeLeavesIntoCollocatedString(allChildren[i]);
					mutatedString.Append(strToAppend);
					mutatedString.Append("_");
				}
				mutatedString = Sharpen.Runtime.DeleteCharAt(mutatedString, mutatedString.Length - 1);
				// Starting with the latest constituent, delete all the "pruned" children
				if (Debug)
				{
					Runtime.err.WriteLine("allChildren is: " + Arrays.ToString(allChildren));
				}
				for (int index = matchingColl.indicesOfConstituentChildren.Count - 1; index > 0; index--)
				{
					int thisConstituent = matchingColl.indicesOfConstituentChildren[index];
					allChildren = (Tree[])ArrayUtils.RemoveAt(allChildren, thisConstituent);
					if (Debug)
					{
						Runtime.err.WriteLine(" deleted " + thisConstituent + "; allChildren is: " + Arrays.ToString(allChildren));
					}
				}
				//name for the leaf string of our new collocation
				string newNodeString = mutatedString.ToString();
				int firstChildIndex = matchingColl.indicesOfConstituentChildren[0];
				//now we mutate the earliest constituent
				Tree newCollocationChild = allChildren[firstChildIndex];
				if (Debug)
				{
					Runtime.err.WriteLine("Manipulating: " + newCollocationChild);
				}
				newCollocationChild.SetValue(matchingColl.headLabel.Value());
				Tree newCollocationLeaf = newCollocationChild.TreeFactory().NewLeaf(newNodeString);
				newCollocationChild.SetChildren(Java.Util.Collections.SingletonList(newCollocationLeaf));
				if (Debug)
				{
					Runtime.err.WriteLine("  changed to: " + newCollocationChild);
				}
				allChildren[firstChildIndex] = newCollocationChild;
				t.SetChildren(allChildren);
				if (Debug)
				{
					Runtime.err.WriteLine("Restructured tree is:");
					t.PennPrint();
					Runtime.err.WriteLine();
				}
				return t;
			}
		}

		/// <summary>Traverses the parse tree to find WordNet collocations.</summary>
		private void GetCollocationsList()
		{
			GetCollocationsList(qTree);
		}

		/// <summary>Prints the collocations found in this <code>Tree</code> as strings.</summary>
		/// <remarks>
		/// Prints the collocations found in this <code>Tree</code> as strings.
		/// Each is followed by its boundary constituent indices in the original tree.
		/// <br />Example: <code> throw_up (2,3) </code>
		/// <br />         <code> came_up_with (7,9) </code>
		/// </remarks>
		public virtual void PrintCollocationStrings(PrintWriter pw)
		{
			//ArrayList<String> strs = new ArrayList<String>();
			foreach (CollocationFinder.Collocation c in collocationCollector)
			{
				string cs = c.collocationString;
				pw.Println(cs + " (" + (c.span.First() + 1) + "," + (c.span.Second() + 1) + ")");
			}
		}

		/// <summary>
		/// This method does the work of traversing the tree and writing collocations
		/// to the CollocationCollector (an internal data structure).
		/// </summary>
		/// <param name="t">Tree to get collocations from.</param>
		private void GetCollocationsList(Tree t)
		{
			int leftMostLeaf = Edu.Stanford.Nlp.Trees.Trees.LeftEdge(t, qTree);
			if (t.IsPreTerminal())
			{
				return;
			}
			IList<Tree> children = t.GetChildrenAsList();
			if (children.IsEmpty())
			{
				return;
			}
			//TODO: fix determineHead
			// - in phrases like "World Trade Organization 's" the head of the parent NP is "POS".
			// - this is problematic for the collocationFinder which assigns this head
			// as the POS for the collocation "World_Trade_Organization"!
			ILabel headLabel = hf.DetermineHead(t).Label();
			int leftSistersBuffer = 0;
			//measures the length of sisters in words when reading
			for (int i = 0; i < children.Count; i++)
			{
				List<int> childConstituents = new List<int>();
				childConstituents.Add(i);
				Tree subtree = children[i];
				int currWindowLength = 0;
				//measures the length in words of the current collocation.
				GetCollocationsList(subtree);
				//recursive call to get colls in subtrees.
				StringBuilder testString = new StringBuilder(160);
				testString.Append(TreeAsStemmedCollocation(subtree));
				testString.Append('_');
				int thisSubtreeLength = subtree.Yield().Count;
				currWindowLength += thisSubtreeLength;
				StringBuilder testStringNonStemmed = new StringBuilder(160);
				testStringNonStemmed.Append(TreeAsNonStemmedCollocation(subtree));
				testStringNonStemmed.Append('_');
				//for each subtree i, we iteratively append word yields of succeeding sister
				//subtrees j and check their wordnet entries.  if they exist we write them to
				//the global collocationCollector pair by the indices of the leftmost and
				//rightmost words in the collocation.
				for (int j = i + 1; j < children.Count; j++)
				{
					Tree sisterNode = children[j];
					childConstituents.Add(j);
					testString.Append(TreeAsStemmedCollocation(sisterNode));
					testStringNonStemmed.Append(TreeAsNonStemmedCollocation(sisterNode));
					currWindowLength += sisterNode.Yield().Count;
					if (Debug)
					{
					}
					//   err.println("Testing string w/ reported indices:" + testString.toString()
					//             + " (" +(leftMostLeaf+leftSistersBuffer)+","+(leftMostLeaf+leftSistersBuffer+currWindowLength-1)+")");
					//ignore collocations beginning with "the" or "a"
					if (StringUtils.LookingAt(testString.ToString(), "(?:[Tt]he|THE|[Aa][Nn]?)[ _]"))
					{
						if (false)
						{
							Runtime.err.WriteLine("CollocationFinder: Not collapsing the/a word: " + testString);
						}
					}
					else
					{
						if (WordNetContains(testString.ToString()))
						{
							Pair<int, int> c = new Pair<int, int>(leftMostLeaf + leftSistersBuffer, leftMostLeaf + leftSistersBuffer + currWindowLength - 1);
							List<int> childConstituentsClone = new List<int>(childConstituents);
							CollocationFinder.Collocation col = new CollocationFinder.Collocation(c, t, childConstituentsClone, testString.ToString(), headLabel);
							collocationCollector.Add(col);
							if (Debug)
							{
								Runtime.err.WriteLine("Found collocation in wordnet: " + testString);
								Runtime.err.WriteLine("  Span of collocation is: " + c + "; childConstituents is: " + c);
							}
						}
					}
					testString.Append('_');
					if (StringUtils.LookingAt(testStringNonStemmed.ToString(), "(?:[Tt]he|THE|[Aa][Nn]?)[ _]"))
					{
						if (false)
						{
							Runtime.err.WriteLine("CollocationFinder: Not collapsing the/a word: " + testStringNonStemmed);
						}
					}
					else
					{
						if (WordNetContains(testStringNonStemmed.ToString()))
						{
							Pair<int, int> c = new Pair<int, int>(leftMostLeaf + leftSistersBuffer, leftMostLeaf + leftSistersBuffer + currWindowLength - 1);
							List<int> childConstituentsClone = new List<int>(childConstituents);
							CollocationFinder.Collocation col = new CollocationFinder.Collocation(c, t, childConstituentsClone, testStringNonStemmed.ToString(), headLabel);
							collocationCollector.Add(col);
							if (Debug)
							{
								Runtime.err.WriteLine("Found collocation in wordnet: " + testStringNonStemmed);
								Runtime.err.WriteLine("  Span of collocation is: " + c + "; childConstituents is: " + c);
							}
						}
					}
					testStringNonStemmed.Append("_");
				}
				leftSistersBuffer += thisSubtreeLength;
			}
		}

		private static string TreeAsStemmedCollocation(Tree t)
		{
			IList<WordTag> list = GetStemmedWordTagsFromTree(t);
			// err.println(list.size());
			StringBuilder s = new StringBuilder(160);
			WordTag firstWord = list.Remove(0);
			s.Append(firstWord.Word());
			foreach (WordTag wt in list)
			{
				s.Append("_");
				s.Append(wt.Word());
			}
			//err.println("Expressing this as:"+s.toString());
			return s.ToString();
		}

		private static string TreeAsNonStemmedCollocation(Tree t)
		{
			IList<WordTag> list = GetNonStemmedWordTagsFromTree(t);
			StringBuilder s = new StringBuilder(160);
			WordTag firstWord = list.Remove(0);
			s.Append(firstWord.Word());
			foreach (WordTag wt in list)
			{
				s.Append('_');
				s.Append(wt.Word());
			}
			return s.ToString();
		}

		private static string MergeLeavesIntoCollocatedString(Tree t)
		{
			StringBuilder sb = new StringBuilder(160);
			List<TaggedWord> sent = t.TaggedYield();
			foreach (TaggedWord aSent in sent)
			{
				sb.Append(aSent.Word()).Append('_');
			}
			return sb.Substring(0, sb.Length - 1);
		}

		private static string MergeLeavesIntoCollocatedString(Tree[] trees)
		{
			StringBuilder sb = new StringBuilder(160);
			foreach (Tree t in trees)
			{
				List<TaggedWord> sent = t.TaggedYield();
				foreach (TaggedWord aSent in sent)
				{
					sb.Append(aSent.Word()).Append('_');
				}
			}
			return sb.Substring(0, sb.Length - 1);
		}

		/// <param name="t">a tree</param>
		/// <returns>
		/// the WordTags corresponding to the leaves of the tree,
		/// stemmed according to their POS tags in the tree.
		/// </returns>
		private static IList<WordTag> GetStemmedWordTagsFromTree(Tree t)
		{
			IList<WordTag> stemmedWordTags = Generics.NewArrayList();
			List<TaggedWord> s = t.TaggedYield();
			foreach (TaggedWord w in s)
			{
				WordTag wt = Morphology.StemStatic(w.Word(), w.Tag());
				stemmedWordTags.Add(wt);
			}
			return stemmedWordTags;
		}

		private static IList<WordTag> GetNonStemmedWordTagsFromTree(Tree t)
		{
			IList<WordTag> wordTags = Generics.NewArrayList();
			List<TaggedWord> s = t.TaggedYield();
			foreach (TaggedWord w in s)
			{
				WordTag wt = new WordTag(w.Word(), w.Tag());
				wordTags.Add(wt);
			}
			return wordTags;
		}

		/// <summary>Checks to see if WordNet contains the given word in its lexicon.</summary>
		/// <param name="s">Token</param>
		/// <returns>If the given token is in WordNet.</returns>
		private bool WordNetContains(string s)
		{
			return wnConnect.WordNetContains(s);
		}

		/// <summary>Holds information for one collocation.</summary>
		private class Collocation
		{
			internal readonly Pair<int, int> span;

			internal readonly Tree parentNode;

			internal readonly ILabel headLabel;

			internal readonly IList<int> indicesOfConstituentChildren;

			internal readonly string collocationString;

			private Collocation(Pair<int, int> span, Tree parentNode, List<int> indicesOfConstituentChildren, string collocationString, ILabel headLabel)
			{
				this.span = span;
				this.parentNode = parentNode;
				this.collocationString = collocationString;
				this.indicesOfConstituentChildren = indicesOfConstituentChildren;
				this.headLabel = headLabel;
			}

			public override string ToString()
			{
				return collocationString + indicesOfConstituentChildren + "/" + headLabel;
			}
		}
		// end static class Collocation
	}
}

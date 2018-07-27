using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A base class for a HeadFinder similar to the one described in
	/// Michael Collins' 1999 thesis.
	/// </summary>
	/// <remarks>
	/// A base class for a HeadFinder similar to the one described in
	/// Michael Collins' 1999 thesis.  For a given constituent we perform operations
	/// like (this is for "left" or "right":
	/// <pre>
	/// for categoryList in categoryLists
	/// for index = 1 to n [or n to 1 if R-&gt;L]
	/// for category in categoryList
	/// if category equals daughter[index] choose it.
	/// </pre>
	/// <p>
	/// with a final default that goes with the direction (L-&gt;R or R-&gt;L)
	/// For most constituents, there will be only one category in the list,
	/// the exception being, in Collins' original version, NP.
	/// </p>
	/// <p>
	/// It is up to the overriding base class to initialize the map
	/// from constituent type to categoryLists, "nonTerminalInfo",
	/// in its constructor.
	/// Entries are presumed to be of type String[][].  Each String[] is a list of
	/// categories, except for the first entry, which specifies direction of
	/// traversal and must be one of the following:
	/// </p>
	/// <ul>
	/// <li> "left" means search left-to-right by category and then by position
	/// <li> "leftdis" means search left-to-right by position and then by category
	/// <li> "right" means search right-to-left by category and then by position
	/// <li> "rightdis" means search right-to-left by position and then by category
	/// <li> "leftexcept" means to take the first thing from the left that isn't in the list
	/// <li> "rightexcept" means to take the first thing from the right that isn't on the list
	/// </ul>
	/// <p>
	/// Changes:
	/// </p>
	/// <ul>
	/// <li> 2002/10/28 -- Category label identity checking now uses the
	/// equals() method instead of ==, so not interning category labels
	/// shouldn't break things anymore.  (Roger Levy) <br />
	/// <li> 2003/02/10 -- Changed to use TreebankLanguagePack and to cut on
	/// characters that set off annotations, so this should work even if
	/// functional tags are still on nodes. <br />
	/// <li> 2004/03/30 -- Made abstract base class and subclasses for CollinsHeadFinder,
	/// ModCollinsHeadFinder, SemanticHeadFinder, ChineseHeadFinder
	/// (and trees.icegb.ICEGBHeadFinder, trees.international.negra.NegraHeadFinder,
	/// and movetrees.EnglishPennMaxProjectionHeadFinder)
	/// <li> 2011/01/13 -- Add support for categoriesToAvoid (which can be set to ensure that
	/// punctuation is not the head if there are other options)
	/// </ul>
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public abstract class AbstractCollinsHeadFinder : IHeadFinder, ICopulaHeadFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.AbstractCollinsHeadFinder));

		private static readonly bool Debug = Runtime.GetProperty("HeadFinder", null) != null;

		protected internal readonly ITreebankLanguagePack tlp;

		protected internal IDictionary<string, string[][]> nonTerminalInfo;

		/// <summary>Default direction if no rule is found for category (the head/parent).</summary>
		/// <remarks>
		/// Default direction if no rule is found for category (the head/parent).
		/// Subclasses can turn it on if they like.
		/// If they don't it is an error if no rule is defined for a category
		/// (null is returned).
		/// </remarks>
		protected internal string[] defaultRule;

		/// <summary>
		/// These are built automatically from categoriesToAvoid and used in a fairly
		/// different fashion from defaultRule (above).
		/// </summary>
		/// <remarks>
		/// These are built automatically from categoriesToAvoid and used in a fairly
		/// different fashion from defaultRule (above).  These are used for categories
		/// that do have defined rules but where none of them have matched.  Rather
		/// than picking the rightmost or leftmost child, we will use these to pick
		/// the the rightmost or leftmost child which isn't in categoriesToAvoid.
		/// </remarks>
		protected internal string[] defaultLeftRule;

		protected internal string[] defaultRightRule;

		/// <summary>Construct a HeadFinder.</summary>
		/// <remarks>
		/// Construct a HeadFinder.
		/// The TreebankLanguagePack is used to get basic categories. The remaining arguments
		/// set categories which, if it comes to last resort processing (i.e., none of
		/// the rules matched), will be avoided as heads. In last resort processing,
		/// it will attempt to match the leftmost or rightmost constituent not in this
		/// set but will fall back to the left or rightmost constituent if necessary.
		/// </remarks>
		/// <param name="tlp">TreebankLanguagePack used to determine basic category</param>
		/// <param name="categoriesToAvoid">Constituent types to avoid as head</param>
		protected internal AbstractCollinsHeadFinder(ITreebankLanguagePack tlp, params string[] categoriesToAvoid)
		{
			/* Serializable */
			// = null;
			this.tlp = tlp;
			// automatically build defaultLeftRule, defaultRightRule
			defaultLeftRule = new string[categoriesToAvoid.Length + 1];
			defaultRightRule = new string[categoriesToAvoid.Length + 1];
			if (categoriesToAvoid.Length > 0)
			{
				defaultLeftRule[0] = "leftexcept";
				defaultRightRule[0] = "rightexcept";
				System.Array.Copy(categoriesToAvoid, 0, defaultLeftRule, 1, categoriesToAvoid.Length);
				System.Array.Copy(categoriesToAvoid, 0, defaultRightRule, 1, categoriesToAvoid.Length);
			}
			else
			{
				defaultLeftRule[0] = "left";
				defaultRightRule[0] = "right";
			}
		}

		/// <summary>Generally will be false, except for SemanticHeadFinder</summary>
		public virtual bool MakesCopulaHead()
		{
			return false;
		}

		/// <summary>
		/// A way for subclasses for corpora with explicit head markings
		/// to return the explicitly marked head
		/// </summary>
		/// <param name="t">a tree to find the head of</param>
		/// <returns>the marked head-- null if no marked head</returns>
		protected internal virtual Tree FindMarkedHead(Tree t)
		{
			// to be overridden in subclasses for corpora
			//
			return null;
		}

		/// <summary>Determine which daughter of the current parse tree is the head.</summary>
		/// <param name="t">
		/// The parse tree to examine the daughters of.
		/// If this is a leaf, <code>null</code> is returned
		/// </param>
		/// <returns>The daughter parse tree that is the head of <code>t</code></returns>
		/// <seealso cref="Tree.PercolateHeads(IHeadFinder)">for a routine to call this and spread heads throughout a tree</seealso>
		public virtual Tree DetermineHead(Tree t)
		{
			return DetermineHead(t, null);
		}

		/// <summary>Determine which daughter of the current parse tree is the head.</summary>
		/// <param name="t">
		/// The parse tree to examine the daughters of.
		/// If this is a leaf, <code>null</code> is returned
		/// </param>
		/// <param name="parent">The parent of t</param>
		/// <returns>
		/// The daughter parse tree that is the head of <code>t</code>.
		/// Returns null for leaf nodes.
		/// </returns>
		/// <seealso cref="Tree.PercolateHeads(IHeadFinder)">for a routine to call this and spread heads throughout a tree</seealso>
		public virtual Tree DetermineHead(Tree t, Tree parent)
		{
			if (nonTerminalInfo == null)
			{
				throw new InvalidOperationException("Classes derived from AbstractCollinsHeadFinder must create and fill HashMap nonTerminalInfo.");
			}
			if (t == null || t.IsLeaf())
			{
				throw new ArgumentException("Can't return head of null or leaf Tree.");
			}
			if (Debug)
			{
				log.Info("determineHead for " + t.Value());
			}
			Tree[] kids = t.Children();
			Tree theHead;
			// first check if subclass found explicitly marked head
			if ((theHead = FindMarkedHead(t)) != null)
			{
				if (Debug)
				{
					log.Info("Find marked head method returned " + theHead.Label() + " as head of " + t.Label());
				}
				return theHead;
			}
			// if the node is a unary, then that kid must be the head
			// it used to special case preterminal and ROOT/TOP case
			// but that seemed bad (especially hardcoding string "ROOT")
			if (kids.Length == 1)
			{
				if (Debug)
				{
					log.Info("Only one child determines " + kids[0].Label() + " as head of " + t.Label());
				}
				return kids[0];
			}
			return DetermineNonTrivialHead(t, parent);
		}

		/// <summary>
		/// Called by determineHead and may be overridden in subclasses
		/// if special treatment is necessary for particular categories.
		/// </summary>
		/// <param name="t">The tre to determine the head daughter of</param>
		/// <param name="parent">The parent of t (or may be null)</param>
		/// <returns>The head daughter of t</returns>
		protected internal virtual Tree DetermineNonTrivialHead(Tree t, Tree parent)
		{
			Tree theHead = null;
			string motherCat = tlp.BasicCategory(t.Label().Value());
			if (motherCat.StartsWith("@"))
			{
				motherCat = Sharpen.Runtime.Substring(motherCat, 1);
			}
			if (Debug)
			{
				log.Info("Looking for head of " + t.Label() + "; value is |" + t.Label().Value() + "|, " + " baseCat is |" + motherCat + '|');
			}
			// We know we have nonterminals underneath
			// (a bit of a Penn Treebank assumption, but).
			// Look at label.
			// a total special case....
			// first look for POS tag at end
			// this appears to be redundant in the Collins case since the rule already would do that
			//    Tree lastDtr = t.lastChild();
			//    if (tlp.basicCategory(lastDtr.label().value()).equals("POS")) {
			//      theHead = lastDtr;
			//    } else {
			string[][] how = nonTerminalInfo[motherCat];
			Tree[] kids = t.Children();
			if (how == null)
			{
				if (Debug)
				{
					log.Info("Warning: No rule found for " + motherCat + " (first char: " + motherCat[0] + ')');
					log.Info("Known nonterms are: " + nonTerminalInfo.Keys);
				}
				if (defaultRule != null)
				{
					if (Debug)
					{
						log.Info("  Using defaultRule");
					}
					return TraverseLocate(kids, defaultRule, true);
				}
				else
				{
					// TreePrint because TreeGraphNode only prints the node number,
					// doesn't print the tree structure
					TreePrint printer = new TreePrint("penn");
					StringWriter buffer = new StringWriter();
					printer.PrintTree(t, new PrintWriter(buffer));
					// TODO: we could get really fancy and define our own
					// exception class to represent this
					throw new ArgumentException("No head rule defined for " + motherCat + " using " + this.GetType() + " in " + buffer.ToString());
				}
			}
			for (int i = 0; i < how.Length; i++)
			{
				bool lastResort = (i == how.Length - 1);
				theHead = TraverseLocate(kids, how[i], lastResort);
				if (theHead != null)
				{
					break;
				}
			}
			if (Debug)
			{
				log.Info("  Chose " + theHead.Label());
			}
			return theHead;
		}

		/// <summary>Attempt to locate head daughter tree from among daughters.</summary>
		/// <remarks>
		/// Attempt to locate head daughter tree from among daughters.
		/// Go through daughterTrees looking for things from or not in a set given by
		/// the contents of the array how, and if
		/// you do not find one, take leftmost or rightmost perhaps matching thing iff
		/// lastResort is true, otherwise return <code>null</code>.
		/// </remarks>
		protected internal virtual Tree TraverseLocate(Tree[] daughterTrees, string[] how, bool lastResort)
		{
			int headIdx;
			switch (how[0])
			{
				case "left":
				{
					headIdx = FindLeftHead(daughterTrees, how);
					break;
				}

				case "leftdis":
				{
					headIdx = FindLeftDisHead(daughterTrees, how);
					break;
				}

				case "leftexcept":
				{
					headIdx = FindLeftExceptHead(daughterTrees, how);
					break;
				}

				case "right":
				{
					headIdx = FindRightHead(daughterTrees, how);
					break;
				}

				case "rightdis":
				{
					headIdx = FindRightDisHead(daughterTrees, how);
					break;
				}

				case "rightexcept":
				{
					headIdx = FindRightExceptHead(daughterTrees, how);
					break;
				}

				default:
				{
					throw new InvalidOperationException("ERROR: invalid direction type " + how[0] + " to nonTerminalInfo map in AbstractCollinsHeadFinder.");
				}
			}
			// what happens if our rule didn't match anything
			if (headIdx < 0)
			{
				if (lastResort)
				{
					// use the default rule to try to match anything except categoriesToAvoid
					// if that doesn't match, we'll return the left or rightmost child (by
					// setting headIdx).  We want to be careful to ensure that postOperationFix
					// runs exactly once.
					string[] rule;
					if (how[0].StartsWith("left"))
					{
						headIdx = 0;
						rule = defaultLeftRule;
					}
					else
					{
						headIdx = daughterTrees.Length - 1;
						rule = defaultRightRule;
					}
					Tree child = TraverseLocate(daughterTrees, rule, false);
					if (child != null)
					{
						return child;
					}
					else
					{
						return daughterTrees[headIdx];
					}
				}
				else
				{
					// if we're not the last resort, we can return null to let the next rule try to match
					return null;
				}
			}
			headIdx = PostOperationFix(headIdx, daughterTrees);
			return daughterTrees[headIdx];
		}

		private int FindLeftHead(Tree[] daughterTrees, string[] how)
		{
			for (int i = 1; i < how.Length; i++)
			{
				for (int headIdx = 0; headIdx < daughterTrees.Length; headIdx++)
				{
					string childCat = tlp.BasicCategory(daughterTrees[headIdx].Label().Value());
					if (how[i].Equals(childCat))
					{
						return headIdx;
					}
				}
			}
			return -1;
		}

		private int FindLeftDisHead(Tree[] daughterTrees, string[] how)
		{
			for (int headIdx = 0; headIdx < daughterTrees.Length; headIdx++)
			{
				string childCat = tlp.BasicCategory(daughterTrees[headIdx].Label().Value());
				for (int i = 1; i < how.Length; i++)
				{
					if (how[i].Equals(childCat))
					{
						return headIdx;
					}
				}
			}
			return -1;
		}

		private int FindLeftExceptHead(Tree[] daughterTrees, string[] how)
		{
			for (int headIdx = 0; headIdx < daughterTrees.Length; headIdx++)
			{
				string childCat = tlp.BasicCategory(daughterTrees[headIdx].Label().Value());
				bool found = true;
				for (int i = 1; i < how.Length; i++)
				{
					if (how[i].Equals(childCat))
					{
						found = false;
					}
				}
				if (found)
				{
					return headIdx;
				}
			}
			return -1;
		}

		private int FindRightHead(Tree[] daughterTrees, string[] how)
		{
			for (int i = 1; i < how.Length; i++)
			{
				for (int headIdx = daughterTrees.Length - 1; headIdx >= 0; headIdx--)
				{
					string childCat = tlp.BasicCategory(daughterTrees[headIdx].Label().Value());
					if (how[i].Equals(childCat))
					{
						return headIdx;
					}
				}
			}
			return -1;
		}

		// from right, but search for any of the categories, not by category in turn
		private int FindRightDisHead(Tree[] daughterTrees, string[] how)
		{
			for (int headIdx = daughterTrees.Length - 1; headIdx >= 0; headIdx--)
			{
				string childCat = tlp.BasicCategory(daughterTrees[headIdx].Label().Value());
				for (int i = 1; i < how.Length; i++)
				{
					if (how[i].Equals(childCat))
					{
						return headIdx;
					}
				}
			}
			return -1;
		}

		private int FindRightExceptHead(Tree[] daughterTrees, string[] how)
		{
			for (int headIdx = daughterTrees.Length - 1; headIdx >= 0; headIdx--)
			{
				string childCat = tlp.BasicCategory(daughterTrees[headIdx].Label().Value());
				bool found = true;
				for (int i = 1; i < how.Length; i++)
				{
					if (how[i].Equals(childCat))
					{
						found = false;
					}
				}
				if (found)
				{
					return headIdx;
				}
			}
			return -1;
		}

		/// <summary>A way for subclasses to fix any heads under special conditions.</summary>
		/// <remarks>
		/// A way for subclasses to fix any heads under special conditions.
		/// The default does nothing.
		/// </remarks>
		/// <param name="headIdx">The index of the proposed head</param>
		/// <param name="daughterTrees">The array of daughter trees</param>
		/// <returns>The new headIndex</returns>
		protected internal virtual int PostOperationFix(int headIdx, Tree[] daughterTrees)
		{
			return headIdx;
		}

		private const long serialVersionUID = -6540278059442931087L;
	}
}

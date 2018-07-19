using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Dvparser
{
	/// <summary>
	/// This filter rejects Trees which have unary or binary productions
	/// which the given parser does not contain.
	/// </summary>
	/// <remarks>
	/// This filter rejects Trees which have unary or binary productions
	/// which the given parser does not contain.
	/// <br />
	/// One situation where this happens often is when grammar compaction
	/// is turned on; this can often result in a Tree where there is no
	/// BinaryRule which could explicitely create a particular node, but
	/// the Tree is still valid.  However, for various applications of the
	/// DVParser, this kind of Tree is useless.  A good way to eliminate
	/// most of this kind of tree is to make sure the parser is trained
	/// with <code>-compactGrammar 0</code>.
	/// </remarks>
	[System.Serializable]
	public class FilterConfusingRules : IPredicate<Tree>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Dvparser.FilterConfusingRules));

		internal readonly ICollection<string> unaryRules = new HashSet<string>();

		internal readonly TwoDimensionalSet<string, string> binaryRules = new TwoDimensionalSet<string, string>();

		internal const bool Debug = false;

		public FilterConfusingRules(LexicalizedParser parser)
		{
			BinaryGrammar binaryGrammar = parser.bg;
			UnaryGrammar unaryGrammar = parser.ug;
			Options op = parser.GetOp();
			IIndex<string> stateIndex = parser.stateIndex;
			foreach (UnaryRule unaryRule in unaryGrammar)
			{
				// only make one matrix for each parent state, and only use the
				// basic category for that      
				string childState = stateIndex.Get(unaryRule.child);
				string childBasic = op.Langpack().BasicCategory(childState);
				unaryRules.Add(childBasic);
			}
			foreach (BinaryRule binaryRule in binaryGrammar)
			{
				// only make one matrix for each parent state, and only use the
				// basic category for that
				string leftState = stateIndex.Get(binaryRule.leftChild);
				string leftBasic = op.Langpack().BasicCategory(leftState);
				string rightState = stateIndex.Get(binaryRule.rightChild);
				string rightBasic = op.Langpack().BasicCategory(rightState);
				binaryRules.Add(leftBasic, rightBasic);
			}
		}

		public virtual bool Test(Tree tree)
		{
			if (tree.IsLeaf() || tree.IsPreTerminal())
			{
				return true;
			}
			if (tree.Children().Length == 0 || tree.Children().Length > 2)
			{
				throw new AssertionError("Tree not binarized");
			}
			if (tree.Children().Length == 1)
			{
				if (!unaryRules.Contains(tree.Children()[0].Label().Value()))
				{
					return false;
				}
			}
			else
			{
				if (!binaryRules.Contains(tree.Children()[0].Label().Value(), tree.Children()[1].Label().Value()))
				{
					return false;
				}
			}
			foreach (Tree child in tree.Children())
			{
				if (!Test(child))
				{
					return false;
				}
			}
			return true;
		}
	}
}

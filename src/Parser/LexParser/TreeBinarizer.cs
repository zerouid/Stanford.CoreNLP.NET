using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Binarizes trees, typically in such a way that head-argument structure is respected.</summary>
	/// <remarks>
	/// Binarizes trees, typically in such a way that head-argument structure is respected.
	/// Looks only at the value of input tree nodes.
	/// Produces LabeledScoredTrees with CategoryWordTag labels.  The input trees have to have category, word, and tag
	/// present (as CategoryWordTag or CoreLabel labels)!
	/// Although the binarizer always respects heads, you can get left or right
	/// binarization by defining an appropriate HeadFinder.
	/// TODO: why not use CoreLabel if the input Tree used CoreLabel?
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Teg Grenager</author>
	/// <author>Christopher Manning</author>
	public class TreeBinarizer : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.TreeBinarizer));

		private const bool Debug = false;

		private IHeadFinder hf;

		private ITreeFactory tf;

		private ITreebankLanguagePack tlp;

		private bool insideFactor;

		private bool markovFactor;

		private int markovOrder;

		private bool useWrappingLabels;

		private double selectiveSplitThreshold;

		private bool markFinalStates;

		private bool unaryAtTop;

		private bool doSelectiveSplit;

		private ClassicCounter<string> stateCounter = new ClassicCounter<string>();

		private readonly bool simpleLabels;

		private readonly bool noRebinarization;

		// true: DT JJ NN -> DT "JJ NN", false: DT "DT"
		// = false;
		/// <summary>
		/// If this is set to true, then the binarizer will choose selectively whether or not to
		/// split states based on how many counts the states had in a previous run.
		/// </summary>
		/// <remarks>
		/// If this is set to true, then the binarizer will choose selectively whether or not to
		/// split states based on how many counts the states had in a previous run. These counts are
		/// stored in an internal counter, which will be added to when doSelectiveSplit is false.
		/// If passed false, this will initialize (clear) the counts.
		/// </remarks>
		/// <param name="doSelectiveSplit">Record this value and reset internal counter if false</param>
		public virtual void SetDoSelectiveSplit(bool doSelectiveSplit)
		{
			this.doSelectiveSplit = doSelectiveSplit;
			if (!doSelectiveSplit)
			{
				stateCounter = new ClassicCounter<string>();
			}
		}

		private static string Join(IList<Tree> treeList)
		{
			StringBuilder sb = new StringBuilder();
			for (IEnumerator<Tree> i = treeList.GetEnumerator(); i.MoveNext(); )
			{
				Tree t = i.Current;
				sb.Append(t.Label().Value());
				if (i.MoveNext())
				{
					sb.Append(' ');
				}
			}
			return sb.ToString();
		}

		private static void LocalTreeString(Tree t, StringBuilder sb, int level)
		{
			sb.Append('\n');
			for (int i = 0; i < level; i++)
			{
				sb.Append("  ");
			}
			sb.Append('(').Append(t.Label());
			if (level == 0 || IsSynthetic(t.Label().Value()))
			{
				// if it is synthetic, recurse
				for (int c = 0; c < t.NumChildren(); c++)
				{
					LocalTreeString(t.GetChild(c), sb, level + 1);
				}
			}
			sb.Append(')');
		}

		private static bool IsSynthetic(string label)
		{
			return label.IndexOf('@') > -1;
		}

		internal virtual Tree BinarizeLocalTree(Tree t, int headNum, TaggedWord head)
		{
			//System.out.println("Working on: "+headNum+" -- "+t.label());
			if (markovFactor)
			{
				string topCat = t.Label().Value();
				ILabel newLabel = new CategoryWordTag(topCat, head.Word(), head.Tag());
				t.SetLabel(newLabel);
				Tree t2;
				if (insideFactor)
				{
					t2 = MarkovInsideBinarizeLocalTreeNew(t, headNum, 0, t.NumChildren() - 1, true);
				}
				else
				{
					//          t2 = markovInsideBinarizeLocalTree(t, head, headNum, topCat, false);
					t2 = MarkovOutsideBinarizeLocalTree(t, head, headNum, topCat, new LinkedList<Tree>(), false);
				}
				return t2;
			}
			if (insideFactor)
			{
				return InsideBinarizeLocalTree(t, headNum, head, 0, 0);
			}
			return OutsideBinarizeLocalTree(t, t.Label().Value(), t.Label().Value(), headNum, head, 0, string.Empty, 0, string.Empty);
		}

		private Tree MarkovOutsideBinarizeLocalTree(Tree t, TaggedWord head, int headLoc, string topCat, LinkedList<Tree> ll, bool doneLeft)
		{
			string word = head.Word();
			string tag = head.Tag();
			IList<Tree> newChildren = new List<Tree>(2);
			// call with t, headNum, head, topCat, false
			if (headLoc == 0)
			{
				if (!doneLeft)
				{
					// insert a unary to separate the sides
					if (tlp.IsStartSymbol(topCat))
					{
						return MarkovOutsideBinarizeLocalTree(t, head, headLoc, topCat, new LinkedList<Tree>(), true);
					}
					string subLabelStr;
					if (simpleLabels)
					{
						subLabelStr = '@' + topCat;
					}
					else
					{
						string headStr = t.GetChild(headLoc).Label().Value();
						subLabelStr = '@' + topCat + ": " + headStr + " ]";
					}
					ILabel subLabel = new CategoryWordTag(subLabelStr, word, tag);
					Tree subTree = tf.NewTreeNode(subLabel, t.GetChildrenAsList());
					newChildren.Add(MarkovOutsideBinarizeLocalTree(subTree, head, headLoc, topCat, new LinkedList<Tree>(), true));
					return tf.NewTreeNode(t.Label(), newChildren);
				}
				int len = t.NumChildren();
				// len = 1
				if (len == 1)
				{
					return tf.NewTreeNode(t.Label(), Java.Util.Collections.SingletonList(t.GetChild(0)));
				}
				ll.AddFirst(t.GetChild(len - 1));
				if (ll.Count > markovOrder)
				{
					ll.RemoveLast();
				}
				// generate a right
				string subLabelStr_1;
				if (simpleLabels)
				{
					subLabelStr_1 = '@' + topCat;
				}
				else
				{
					string headStr = t.GetChild(headLoc).Label().Value();
					string rightStr = (len > markovOrder - 1 ? "... " : string.Empty) + Join(ll);
					subLabelStr_1 = '@' + topCat + ": " + headStr + ' ' + rightStr;
				}
				ILabel subLabel_1 = new CategoryWordTag(subLabelStr_1, word, tag);
				Tree subTree_1 = tf.NewTreeNode(subLabel_1, t.GetChildrenAsList().SubList(0, len - 1));
				newChildren.Add(MarkovOutsideBinarizeLocalTree(subTree_1, head, headLoc, topCat, ll, true));
				newChildren.Add(t.GetChild(len - 1));
				return tf.NewTreeNode(t.Label(), newChildren);
			}
			if (headLoc > 0)
			{
				ll.AddLast(t.GetChild(0));
				if (ll.Count > markovOrder)
				{
					ll.RemoveFirst();
				}
				// generate a left
				string subLabelStr;
				if (simpleLabels)
				{
					subLabelStr = '@' + topCat;
				}
				else
				{
					string headStr = t.GetChild(headLoc).Label().Value();
					string leftStr = Join(ll) + (headLoc > markovOrder - 1 ? " ..." : string.Empty);
					subLabelStr = '@' + topCat + ": " + leftStr + ' ' + headStr + " ]";
				}
				ILabel subLabel = new CategoryWordTag(subLabelStr, word, tag);
				Tree subTree = tf.NewTreeNode(subLabel, t.GetChildrenAsList().SubList(1, t.NumChildren()));
				newChildren.Add(t.GetChild(0));
				newChildren.Add(MarkovOutsideBinarizeLocalTree(subTree, head, headLoc - 1, topCat, ll, false));
				return tf.NewTreeNode(t.Label(), newChildren);
			}
			return t;
		}

		/// <summary>Uses tail recursion.</summary>
		/// <remarks>Uses tail recursion. The Tree t that is passed never changes, only the indices left and right do.</remarks>
		private Tree MarkovInsideBinarizeLocalTreeNew(Tree t, int headLoc, int left, int right, bool starting)
		{
			Tree result;
			Tree[] children = t.Children();
			if (starting)
			{
				// this local tree is a unary and doesn't need binarizing so just return it
				if (left == headLoc && right == headLoc)
				{
					return t;
				}
				// this local tree started off as a binary and the option to not
				// rebinarized such trees is set
				if (noRebinarization && children.Length == 2)
				{
					return t;
				}
				if (unaryAtTop)
				{
					// if we're doing grammar compaction, we add the unary at the top
					result = tf.NewTreeNode(t.Label(), Java.Util.Collections.SingletonList(MarkovInsideBinarizeLocalTreeNew(t, headLoc, left, right, false)));
					return result;
				}
			}
			// otherwise, we're going to make a new tree node
			IList<Tree> newChildren = null;
			// left then right top down, this means we generate right then left on the way up
			if (left == headLoc && right == headLoc)
			{
				// base case, we're done, just make a unary
				newChildren = Java.Util.Collections.SingletonList(children[headLoc]);
			}
			else
			{
				if (left < headLoc)
				{
					// generate a left if we can
					newChildren = new List<Tree>(2);
					newChildren.Add(children[left]);
					newChildren.Add(MarkovInsideBinarizeLocalTreeNew(t, headLoc, left + 1, right, false));
				}
				else
				{
					if (right > headLoc)
					{
						// generate a right if we can
						newChildren = new List<Tree>(2);
						newChildren.Add(MarkovInsideBinarizeLocalTreeNew(t, headLoc, left, right - 1, false));
						newChildren.Add(children[right]);
					}
					else
					{
						// this shouldn't happen, should have been caught above
						log.Info("Uh-oh, bad parameters passed to markovInsideBinarizeLocalTree");
					}
				}
			}
			// newChildren should be set up now with two children
			// make our new label
			ILabel label;
			if (starting)
			{
				label = t.Label();
			}
			else
			{
				label = MakeSyntheticLabel(t, left, right, headLoc, markovOrder);
			}
			if (doSelectiveSplit)
			{
				double stateCount = stateCounter.GetCount(label.Value());
				if (stateCount < selectiveSplitThreshold)
				{
					// too sparse, so
					if (starting && !unaryAtTop)
					{
						// if we're not compacting grammar, this is how we make sure the top state has the passive symbol
						label = t.Label();
					}
					else
					{
						label = MakeSyntheticLabel(t, left, right, headLoc, markovOrder - 1);
					}
				}
			}
			else
			{
				// lower order
				// otherwise, count up the states
				stateCounter.IncrementCount(label.Value(), 1.0);
			}
			// we only care about the category
			// finished making new label
			result = tf.NewTreeNode(label, newChildren);
			return result;
		}

		private ILabel MakeSyntheticLabel(Tree t, int left, int right, int headLoc, int markovOrder)
		{
			ILabel result;
			if (simpleLabels)
			{
				result = MakeSimpleSyntheticLabel(t);
			}
			else
			{
				if (useWrappingLabels)
				{
					result = MakeSyntheticLabel2(t, left, right, headLoc, markovOrder);
				}
				else
				{
					result = MakeSyntheticLabel1(t, left, right, headLoc, markovOrder);
				}
			}
			//      System.out.println("order " + markovOrder + " yielded " + result);
			return result;
		}

		/// <summary>Do nothing other than decorate the label with @</summary>
		private static ILabel MakeSimpleSyntheticLabel(Tree t)
		{
			string topCat = t.Label().Value();
			string labelStr = '@' + topCat;
			string word = ((IHasWord)t.Label()).Word();
			string tag = ((IHasTag)t.Label()).Tag();
			return new CategoryWordTag(labelStr, word, tag);
		}

		/// <summary>For a dotted rule VP^S -&gt; RB VP NP PP .</summary>
		/// <remarks>
		/// For a dotted rule VP^S -&gt; RB VP NP PP . where VP is the head
		/// makes label of the form: @VP^S| [ RB [VP] ... PP ]
		/// where the constituent after the @ is the passive that we are building
		/// and  the constituent in brackets is the head
		/// and the brackets on the left and right indicate whether or not there
		/// are more constituents to add on those sides.
		/// </remarks>
		private static ILabel MakeSyntheticLabel1(Tree t, int left, int right, int headLoc, int markovOrder)
		{
			string topCat = t.Label().Value();
			Tree[] children = t.Children();
			string leftString;
			if (left == 0)
			{
				leftString = "[ ";
			}
			else
			{
				leftString = " ";
			}
			string rightString;
			if (right == children.Length - 1)
			{
				rightString = " ]";
			}
			else
			{
				rightString = " ";
			}
			for (int i = 0; i < markovOrder; i++)
			{
				if (left < headLoc)
				{
					leftString = leftString + children[left].Label().Value() + ' ';
					left++;
				}
				else
				{
					if (right > headLoc)
					{
						rightString = ' ' + children[right].Label().Value() + rightString;
						right--;
					}
					else
					{
						break;
					}
				}
			}
			if (right > headLoc)
			{
				rightString = "..." + rightString;
			}
			if (left < headLoc)
			{
				leftString = leftString + "...";
			}
			string labelStr = '@' + topCat + "| " + leftString + '[' + t.GetChild(headLoc).Label().Value() + ']' + rightString;
			// the head in brackets
			string word = ((IHasWord)t.Label()).Word();
			string tag = ((IHasTag)t.Label()).Tag();
			return new CategoryWordTag(labelStr, word, tag);
		}

		/// <summary>for a dotted rule VP^S -&gt; RB VP NP PP .</summary>
		/// <remarks>
		/// for a dotted rule VP^S -&gt; RB VP NP PP . where VP is the head
		/// makes label of the form: @VP^S| VP_ ... PP&gt; RB[
		/// </remarks>
		private ILabel MakeSyntheticLabel2(Tree t, int left, int right, int headLoc, int markovOrder)
		{
			string topCat = t.Label().Value();
			Tree[] children = t.Children();
			string finalPiece;
			int i = 0;
			if (markFinalStates)
			{
				// figure out which one is final
				if (headLoc != 0 && left == 0)
				{
					// we are finishing on the left
					finalPiece = ' ' + children[left].Label().Value() + '[';
					left++;
					i++;
				}
				else
				{
					if (headLoc == 0 && right > headLoc && right == children.Length - 1)
					{
						// we are finishing on the right
						finalPiece = ' ' + children[right].Label().Value() + ']';
						right--;
						i++;
					}
					else
					{
						finalPiece = string.Empty;
					}
				}
			}
			else
			{
				finalPiece = string.Empty;
			}
			string middlePiece = string.Empty;
			for (; i < markovOrder; i++)
			{
				if (left < headLoc)
				{
					middlePiece = ' ' + children[left].Label().Value() + '<' + middlePiece;
					left++;
				}
				else
				{
					if (right > headLoc)
					{
						middlePiece = ' ' + children[right].Label().Value() + '>' + middlePiece;
						right--;
					}
					else
					{
						break;
					}
				}
			}
			if (right > headLoc || left < headLoc)
			{
				middlePiece = " ..." + middlePiece;
			}
			string headStr = t.GetChild(headLoc).Label().Value();
			string labelStr = '@' + topCat + "| " + headStr + '_' + middlePiece + finalPiece;
			// Was: Optimize memory allocation for this next line, since these are the String's that linger.
			// No longer necessary with Java 8+ Strings that don't share.
			// int leng = 1 + 2 + 1 + topCat.length() + headStr.length() + middlePiece.length() + finalPiece.length();
			// StringBuilder sb = new StringBuilder(leng);
			// sb.append('@').append(topCat).append("| ").append(headStr).append('_').append(middlePiece).append(finalPiece);
			// String labelStr = sb.toString();
			// log.info("makeSyntheticLabel2: " + labelStr);
			string word = ((IHasWord)t.Label()).Word();
			string tag = ((IHasTag)t.Label()).Tag();
			return new CategoryWordTag(labelStr, word, tag);
		}

		private Tree InsideBinarizeLocalTree(Tree t, int headNum, TaggedWord head, int leftProcessed, int rightProcessed)
		{
			string word = head.Word();
			string tag = head.Tag();
			IList<Tree> newChildren = new List<Tree>(2);
			// check done
			if (t.NumChildren() <= leftProcessed + rightProcessed + 2)
			{
				Tree leftChild = t.GetChild(leftProcessed);
				newChildren.Add(leftChild);
				if (t.NumChildren() == leftProcessed + rightProcessed + 1)
				{
					// unary ... so top level
					string finalCat = t.Label().Value();
					return tf.NewTreeNode(new CategoryWordTag(finalCat, word, tag), newChildren);
				}
				// binary
				Tree rightChild = t.GetChild(leftProcessed + 1);
				newChildren.Add(rightChild);
				string labelStr = t.Label().Value();
				if (leftProcessed != 0 || rightProcessed != 0)
				{
					labelStr = ("@ " + leftChild.Label().Value() + ' ' + rightChild.Label().Value());
				}
				return tf.NewTreeNode(new CategoryWordTag(labelStr, word, tag), newChildren);
			}
			if (headNum > leftProcessed)
			{
				// eat left word
				Tree leftChild = t.GetChild(leftProcessed);
				Tree rightChild = InsideBinarizeLocalTree(t, headNum, head, leftProcessed + 1, rightProcessed);
				newChildren.Add(leftChild);
				newChildren.Add(rightChild);
				string labelStr = ("@ " + leftChild.Label().Value() + ' ' + Sharpen.Runtime.Substring(rightChild.Label().Value(), 2));
				if (leftProcessed == 0 && rightProcessed == 0)
				{
					labelStr = t.Label().Value();
				}
				return tf.NewTreeNode(new CategoryWordTag(labelStr, word, tag), newChildren);
			}
			else
			{
				// eat right word
				Tree leftChild = InsideBinarizeLocalTree(t, headNum, head, leftProcessed, rightProcessed + 1);
				Tree rightChild = t.GetChild(t.NumChildren() - rightProcessed - 1);
				newChildren.Add(leftChild);
				newChildren.Add(rightChild);
				string labelStr = ("@ " + Sharpen.Runtime.Substring(leftChild.Label().Value(), 2) + ' ' + rightChild.Label().Value());
				if (leftProcessed == 0 && rightProcessed == 0)
				{
					labelStr = t.Label().Value();
				}
				return tf.NewTreeNode(new CategoryWordTag(labelStr, word, tag), newChildren);
			}
		}

		private Tree OutsideBinarizeLocalTree(Tree t, string labelStr, string finalCat, int headNum, TaggedWord head, int leftProcessed, string leftStr, int rightProcessed, string rightStr)
		{
			IList<Tree> newChildren = new List<Tree>(2);
			ILabel label = new CategoryWordTag(labelStr, head.Word(), head.Tag());
			// check if there are <=2 children already
			if (t.NumChildren() - leftProcessed - rightProcessed <= 2)
			{
				// done, return
				newChildren.Add(t.GetChild(leftProcessed));
				if (t.NumChildren() - leftProcessed - rightProcessed == 2)
				{
					newChildren.Add(t.GetChild(leftProcessed + 1));
				}
				return tf.NewTreeNode(label, newChildren);
			}
			if (headNum > leftProcessed)
			{
				// eat a left word
				Tree leftChild = t.GetChild(leftProcessed);
				string childLeftStr = leftStr + ' ' + leftChild.Label().Value();
				string childLabelStr;
				if (simpleLabels)
				{
					childLabelStr = '@' + finalCat;
				}
				else
				{
					childLabelStr = '@' + finalCat + " :" + childLeftStr + " ..." + rightStr;
				}
				Tree rightChild = OutsideBinarizeLocalTree(t, childLabelStr, finalCat, headNum, head, leftProcessed + 1, childLeftStr, rightProcessed, rightStr);
				newChildren.Add(leftChild);
				newChildren.Add(rightChild);
				return tf.NewTreeNode(label, newChildren);
			}
			else
			{
				// eat a right word
				Tree rightChild = t.GetChild(t.NumChildren() - rightProcessed - 1);
				string childRightStr = ' ' + rightChild.Label().Value() + rightStr;
				string childLabelStr;
				if (simpleLabels)
				{
					childLabelStr = '@' + finalCat;
				}
				else
				{
					childLabelStr = '@' + finalCat + " :" + leftStr + " ..." + childRightStr;
				}
				Tree leftChild = OutsideBinarizeLocalTree(t, childLabelStr, finalCat, headNum, head, leftProcessed, leftStr, rightProcessed + 1, childRightStr);
				newChildren.Add(leftChild);
				newChildren.Add(rightChild);
				return tf.NewTreeNode(label, newChildren);
			}
		}

		/// <summary>Binarizes the tree according to options set up in the constructor.</summary>
		/// <remarks>
		/// Binarizes the tree according to options set up in the constructor.
		/// Does the whole tree by calling itself recursively.
		/// </remarks>
		/// <param name="t">
		/// A tree to be binarized. The non-leaf nodes must already have
		/// CategoryWordTag labels, with heads percolated.
		/// </param>
		/// <returns>A binary tree.</returns>
		public virtual Tree TransformTree(Tree t)
		{
			// handle null
			if (t == null)
			{
				return null;
			}
			string cat = t.Label().Value();
			// handle words
			if (t.IsLeaf())
			{
				ILabel label = new Word(cat);
				//new CategoryWordTag(cat,cat,"");
				return tf.NewLeaf(label);
			}
			// handle tags
			if (t.IsPreTerminal())
			{
				Tree childResult = TransformTree(t.GetChild(0));
				string word = childResult.Value();
				// would be nicer if Word/CWT ??
				IList<Tree> newChildren = new List<Tree>(1);
				newChildren.Add(childResult);
				return tf.NewTreeNode(new CategoryWordTag(cat, word, cat), newChildren);
			}
			// handle categories
			Tree headChild = hf.DetermineHead(t);
			/*
			System.out.println("### finding head for:");
			t.pennPrint();
			System.out.println("### its head is:");
			headChild.pennPrint();
			*/
			if (headChild == null && !t.Label().Value().StartsWith(tlp.StartSymbol()))
			{
				log.Info("### No head found for:");
				t.PennPrint();
			}
			int headNum = -1;
			Tree[] kids = t.Children();
			IList<Tree> newChildren_1 = new List<Tree>(kids.Length);
			for (int childNum = 0; childNum < kids.Length; childNum++)
			{
				Tree child = kids[childNum];
				Tree childResult = TransformTree(child);
				// recursive call
				if (child == headChild)
				{
					headNum = childNum;
				}
				newChildren_1.Add(childResult);
			}
			Tree result;
			// XXXXX UPTO HERE!!!  ALMOST DONE!!!
			if (t.Label().Value().StartsWith(tlp.StartSymbol()))
			{
				// handle the ROOT tree properly
				/*
				//CategoryWordTag label = (CategoryWordTag) t.label();
				// binarize without the last kid and then add it back to the top tree
				Tree lastKid = (Tree)newChildren.remove(newChildren.size()-1);
				Tree tempTree = tf.newTreeNode(label, newChildren);
				tempTree = binarizeLocalTree(tempTree, headNum, result.head);
				newChildren = tempTree.getChildrenAsList();
				newChildren.add(lastKid); // add it back
				*/
				result = tf.NewTreeNode(t.Label(), newChildren_1);
			}
			else
			{
				// label shouldn't have changed
				//      CategoryWordTag headLabel = (CategoryWordTag) headChild.label();
				string word = ((IHasWord)headChild.Label()).Word();
				string tag = ((IHasTag)headChild.Label()).Tag();
				ILabel label = new CategoryWordTag(cat, word, tag);
				result = tf.NewTreeNode(label, newChildren_1);
				// cdm Mar 2005: invent a head so I don't have to rewrite all this
				// code, but with the removal of TreeHeadPair, some of the rest of
				// this should probably be rewritten too to not use this head variable
				TaggedWord head = new TaggedWord(word, tag);
				result = BinarizeLocalTree(result, headNum, head);
			}
			return result;
		}

		/// <summary>Builds a TreeBinarizer with all of the options set to simple values</summary>
		public static Edu.Stanford.Nlp.Parser.Lexparser.TreeBinarizer SimpleTreeBinarizer(IHeadFinder hf, ITreebankLanguagePack tlp)
		{
			return new Edu.Stanford.Nlp.Parser.Lexparser.TreeBinarizer(hf, tlp, false, false, 0, false, false, 0.0, false, true, true);
		}

		/// <summary>Build a custom binarizer for Trees.</summary>
		/// <param name="hf">the HeadFinder to use in binarization</param>
		/// <param name="tlp">the TreebankLanguagePack to use</param>
		/// <param name="insideFactor">whether to do inside markovization</param>
		/// <param name="markovFactor">whether to markovize the binary rules</param>
		/// <param name="markovOrder">the markov order to use; only relevant with markovFactor=true</param>
		/// <param name="useWrappingLabels">whether to use state names (labels) that allow wrapping from right to left</param>
		/// <param name="unaryAtTop">
		/// Whether to actually materialize the unary that rewrites
		/// a passive state to the active rule at the top of an original local
		/// tree.  This is used only when compaction is happening
		/// </param>
		/// <param name="selectiveSplitThreshold">if selective split is used, this will be the threshold used to decide which state splits to keep</param>
		/// <param name="markFinalStates">whether or not to make the state names (labels) of the final active states distinctive</param>
		/// <param name="noRebinarization">if true, a node which already has exactly two children is not altered</param>
		public TreeBinarizer(IHeadFinder hf, ITreebankLanguagePack tlp, bool insideFactor, bool markovFactor, int markovOrder, bool useWrappingLabels, bool unaryAtTop, double selectiveSplitThreshold, bool markFinalStates, bool simpleLabels, bool noRebinarization
			)
		{
			this.hf = hf;
			this.tlp = tlp;
			this.tf = new LabeledScoredTreeFactory(new CategoryWordTagFactory());
			this.insideFactor = insideFactor;
			this.markovFactor = markovFactor;
			this.markovOrder = markovOrder;
			this.useWrappingLabels = useWrappingLabels;
			this.unaryAtTop = unaryAtTop;
			this.selectiveSplitThreshold = selectiveSplitThreshold;
			this.markFinalStates = markFinalStates;
			this.simpleLabels = simpleLabels;
			this.noRebinarization = noRebinarization;
		}

		/// <summary>Lets you test out the TreeBinarizer on the command line.</summary>
		/// <remarks>
		/// Lets you test out the TreeBinarizer on the command line.
		/// This main method doesn't yet handle as many flags as one would like.
		/// But it does have:
		/// <ul>
		/// <li> -tlp TreebankLanguagePack
		/// <li>-tlpp TreebankLangParserParams
		/// <li>-insideFactor
		/// <li>-markovOrder
		/// </ul>
		/// </remarks>
		/// <param name="args">
		/// Command line arguments: flags as above, as above followed by
		/// treebankPath
		/// </param>
		public static void Main(string[] args)
		{
			ITreebankLangParserParams tlpp = null;
			// TreebankLangParserParams tlpp = new EnglishTreebankParserParams();
			// TreeReaderFactory trf = new LabeledScoredTreeReaderFactory();
			// Looks like it must build CategoryWordTagFactory!!
			ITreeReaderFactory trf = null;
			string fileExt = "mrg";
			IHeadFinder hf = new ModCollinsHeadFinder();
			ITreebankLanguagePack tlp = new PennTreebankLanguagePack();
			bool insideFactor = false;
			bool mf = false;
			int mo = 1;
			bool uwl = false;
			bool uat = false;
			double sst = 20.0;
			bool mfs = false;
			bool simpleLabels = false;
			bool noRebinarization = false;
			int i = 0;
			while (i < args.Length && args[i].StartsWith("-"))
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tlp") && i + 1 < args.Length)
				{
					try
					{
						tlp = (ITreebankLanguagePack)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
					}
					catch (Exception e)
					{
						log.Info("Couldn't instantiate: " + args[i + 1]);
						throw new Exception(e);
					}
					i++;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tlpp") && i + 1 < args.Length)
					{
						try
						{
							tlpp = (ITreebankLangParserParams)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
						}
						catch (Exception e)
						{
							log.Info("Couldn't instantiate: " + args[i + 1]);
							throw new Exception(e);
						}
						i++;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-insideFactor"))
						{
							insideFactor = true;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markovOrder") && i + 1 < args.Length)
							{
								i++;
								mo = System.Convert.ToInt32(args[i]);
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-simpleLabels"))
								{
									simpleLabels = true;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noRebinarization"))
									{
										noRebinarization = true;
									}
									else
									{
										log.Info("Unknown option:" + args[i]);
									}
								}
							}
						}
					}
				}
				i++;
			}
			if (i >= args.Length)
			{
				log.Info("usage: java TreeBinarizer [-tlpp class|-markovOrder int|...] treebankPath");
				System.Environment.Exit(0);
			}
			Treebank treebank;
			if (tlpp != null)
			{
				treebank = tlpp.MemoryTreebank();
				tlp = tlpp.TreebankLanguagePack();
				fileExt = tlp.TreebankFileExtension();
				hf = tlpp.HeadFinder();
			}
			else
			{
				treebank = new DiskTreebank(trf);
			}
			treebank.LoadPath(args[i], fileExt, true);
			ITreeTransformer tt = new Edu.Stanford.Nlp.Parser.Lexparser.TreeBinarizer(hf, tlp, insideFactor, mf, mo, uwl, uat, sst, mfs, simpleLabels, noRebinarization);
			foreach (Tree t in treebank)
			{
				Tree newT = tt.TransformTree(t);
				System.Console.Out.WriteLine("Original tree:");
				t.PennPrint();
				System.Console.Out.WriteLine("Binarized tree:");
				newT.PennPrint();
				System.Console.Out.WriteLine();
			}
		}
		// end main
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Performs non-language specific annotation of Trees.</summary>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	public class TreeAnnotator : ITreeTransformer
	{
		private ITreeFactory tf;

		private ITreebankLangParserParams tlpParams;

		private IHeadFinder hf;

		private TrainOptions trainOptions;

		public TreeAnnotator(IHeadFinder hf, ITreebankLangParserParams tlpp, Options op)
		{
			this.tlpParams = tlpp;
			this.hf = hf;
			this.tf = new LabeledScoredTreeFactory();
			this.trainOptions = op.trainOptions;
		}

		/// <summary>Do the category splitting of the tree passed in.</summary>
		/// <remarks>
		/// Do the category splitting of the tree passed in.
		/// This method defensively copies its argument, which is not changed.
		/// </remarks>
		/// <param name="t">
		/// The tree to be annotated.  This can be any tree with a
		/// <c>value()</c>
		/// stored in Labels.  The tree is assumed to have
		/// preterminals that are parts of speech.
		/// </param>
		/// <returns>
		/// The annotated version of the Tree (which is a completely
		/// separate Tree with new tree structure and new labels).  The
		/// non-leaf nodes of the tree will be CategoryWordTag objects.
		/// </returns>
		public virtual Tree TransformTree(Tree t)
		{
			// make a defensive copy which the helper method can then mangle
			Tree copy = t.DeepCopy(tf);
			if (trainOptions.markStrahler)
			{
				MarkStrahler(copy);
			}
			return TransformTreeHelper(copy, copy);
		}

		/// <summary>Do the category splitting of the tree passed in.</summary>
		/// <remarks>
		/// Do the category splitting of the tree passed in.
		/// This is initially called on the root node of a tree, and it recursively
		/// calls itself on children.  A depth first left-to-right traversal is
		/// done whereby a tree node's children are first transformed and then
		/// the parent is transformed.  At the time of calling, the original root
		/// always sits above the current node.  This routine can be assumed to,
		/// and does, change the tree passed in: it destructively modifies tree nodes,
		/// and makes new tree structure when it needs to.
		/// </remarks>
		/// <param name="t">The tree node to subcategorize.</param>
		/// <param name="root">
		/// The root of the tree.  It must contain
		/// <paramref name="t"/>
		/// or
		/// this code will throw a NullPointerException.
		/// </param>
		/// <returns>The annotated tree.</returns>
		private Tree TransformTreeHelper(Tree t, Tree root)
		{
			if (t == null)
			{
				// handle null
				return null;
			}
			if (t.IsLeaf())
			{
				//No need to change the label
				return t;
			}
			string cat = t.Label().Value();
			Tree parent;
			string parentStr;
			string grandParentStr;
			if (root == null || t.Equals(root))
			{
				parent = null;
				parentStr = string.Empty;
			}
			else
			{
				parent = t.Parent(root);
				parentStr = parent.Label().Value();
			}
			if (parent == null || parent.Equals(root))
			{
				grandParentStr = string.Empty;
			}
			else
			{
				grandParentStr = parent.Parent(root).Label().Value();
			}
			string baseParentStr = tlpParams.TreebankLanguagePack().BasicCategory(parentStr);
			string baseGrandParentStr = tlpParams.TreebankLanguagePack().BasicCategory(grandParentStr);
			//System.out.println(t.label().value() + " " + parentStr + " " + grandParentStr);
			if (t.IsPreTerminal())
			{
				// handle tags
				Tree childResult = TransformTreeHelper(t.Children()[0], null);
				// recurse
				string word = childResult.Value();
				// would be nicer if Word/CWT ??
				if (!trainOptions.noTagSplit)
				{
					if (trainOptions.tagPA)
					{
						string test = cat + "^" + baseParentStr;
						if (!trainOptions.tagSelectiveSplit || trainOptions.splitters.Contains(test))
						{
							cat = test;
						}
					}
					if (trainOptions.markUnaryTags && parent.NumChildren() == 1)
					{
						cat = cat + "^U";
					}
				}
				// otherwise, leave the tags alone!
				// Label label = new CategoryWordTag(cat, word, cat);
				ILabel label = t.Label().LabelFactory().NewLabel(t.Label());
				label.SetValue(cat);
				if (label is IHasCategory)
				{
					((IHasCategory)label).SetCategory(cat);
				}
				if (label is IHasWord)
				{
					((IHasWord)label).SetWord(word);
				}
				if (label is IHasTag)
				{
					((IHasTag)label).SetTag(cat);
				}
				t.SetLabel(label);
				t.SetChild(0, childResult);
				// just in case word is changed
				if (trainOptions.noTagSplit)
				{
					return t;
				}
				else
				{
					// language-specific transforms
					return tlpParams.TransformTree(t, root);
				}
			}
			// end isPreTerminal()
			// handle phrasal categories
			Tree[] kids = t.Children();
			for (int childNum = 0; childNum < kids.Length; childNum++)
			{
				Tree child = kids[childNum];
				Tree childResult = TransformTreeHelper(child, root);
				// recursive call
				t.SetChild(childNum, childResult);
			}
			Tree headChild = hf.DetermineHead(t);
			if (headChild == null || headChild.Label() == null)
			{
				throw new Exception("TreeAnnotator: null head found for tree [suggesting incomplete/wrong HeadFinder]:\n" + t);
			}
			ILabel headLabel = headChild.Label();
			if (!(headLabel is IHasWord))
			{
				throw new Exception("TreeAnnotator: Head label lacks a Word annotation!");
			}
			if (!(headLabel is IHasTag))
			{
				throw new Exception("TreeAnnotator: Head label lacks a Tag annotation!");
			}
			string word_1 = ((IHasWord)headLabel).Word();
			string tag = ((IHasTag)headLabel).Tag();
			// String baseTag = tlpParams.treebankLanguagePack().basicCategory(tag);
			string baseCat = tlpParams.TreebankLanguagePack().BasicCategory(cat);
			/* Sister annotation. Potential problem: if multiple sisters are
			* strong indicators for a single category's expansions.  This
			* happens concretely in the Chinese Treebank when NP (object)
			* has left sisters VV and AS.  Could lead to too much
			* sparseness.  The ideal solution would be to give the
			* splitting list an ordering, and take only the highest (~most
			* informative/reliable) sister annotation.
			*/
			if (trainOptions.sisterAnnotate && !trainOptions.smoothing && baseParentStr.Length > 0)
			{
				IList<string> leftSis = ListBasicCategories(SisterAnnotationStats.LeftSisterLabels(t, parent));
				IList<string> rightSis = ListBasicCategories(SisterAnnotationStats.RightSisterLabels(t, parent));
				IList<string> leftAnn = new List<string>();
				IList<string> rightAnn = new List<string>();
				foreach (string s in leftSis)
				{
					//s = baseCat+"=l="+tlpParams.treebankLanguagePack().basicCategory(s);
					leftAnn.Add(baseCat + "=l=" + tlpParams.TreebankLanguagePack().BasicCategory(s));
				}
				//System.out.println("left-annotated test string " + s);
				foreach (string s_1 in rightSis)
				{
					//s = baseCat+"=r="+tlpParams.treebankLanguagePack().basicCategory(s);
					rightAnn.Add(baseCat + "=r=" + tlpParams.TreebankLanguagePack().BasicCategory(s_1));
				}
				for (IEnumerator<string> j = rightAnn.GetEnumerator(); j.MoveNext(); )
				{
				}
				//System.out.println("new rightsis " + (String)j.next()); //debugging
				foreach (string annCat in trainOptions.sisterSplitters)
				{
					//System.out.println("annotated test string " + annCat);
					if (leftAnn.Contains(annCat) || rightAnn.Contains(annCat))
					{
						cat = cat + annCat.ReplaceAll("^" + baseCat, string.Empty);
						break;
					}
				}
			}
			if (trainOptions.Pa && !trainOptions.smoothing && baseParentStr.Length > 0)
			{
				string cat2 = baseCat + "^" + baseParentStr;
				if (!trainOptions.selectiveSplit || trainOptions.splitters.Contains(cat2))
				{
					cat = cat + "^" + baseParentStr;
				}
			}
			if (trainOptions.gPA && !trainOptions.smoothing && grandParentStr.Length > 0)
			{
				if (trainOptions.selectiveSplit)
				{
					string cat2 = baseCat + "^" + baseParentStr + "~" + baseGrandParentStr;
					if (cat.Contains("^") && trainOptions.splitters.Contains(cat2))
					{
						cat = cat + "~" + baseGrandParentStr;
					}
				}
				else
				{
					cat = cat + "~" + baseGrandParentStr;
				}
			}
			if (trainOptions.markUnary > 0)
			{
				if (trainOptions.markUnary == 1 && kids.Length == 1 && kids[0].Depth() >= 2)
				{
					cat = cat + "-U";
				}
				else
				{
					if (trainOptions.markUnary == 2 && parent != null && parent.NumChildren() == 1 && t.Depth() >= 2)
					{
						cat = cat + "-u";
					}
				}
			}
			if (trainOptions.rightRec && RightRec(t, baseCat))
			{
				cat = cat + "-R";
			}
			if (trainOptions.leftRec && LeftRec(t, baseCat))
			{
				cat = cat + "-L";
			}
			if (trainOptions.splitPrePreT && t.IsPrePreTerminal())
			{
				cat = cat + "-PPT";
			}
			//    Label label = new CategoryWordTag(cat, word, tag);
			ILabel label_1 = t.Label().LabelFactory().NewLabel(t.Label());
			label_1.SetValue(cat);
			if (label_1 is IHasCategory)
			{
				((IHasCategory)label_1).SetCategory(cat);
			}
			if (label_1 is IHasWord)
			{
				((IHasWord)label_1).SetWord(word_1);
			}
			if (label_1 is IHasTag)
			{
				((IHasTag)label_1).SetTag(tag);
			}
			t.SetLabel(label_1);
			return tlpParams.TransformTree(t, root);
		}

		private IList<string> ListBasicCategories(IList<string> l)
		{
			IList<string> l1 = new List<string>();
			foreach (string str in l)
			{
				l1.Add(tlpParams.TreebankLanguagePack().BasicCategory(str));
			}
			return l1;
		}

		private static bool RightRec(Tree t, string baseCat)
		{
			if (!baseCat.Equals("NP"))
			{
				//! baseCat.equals("S") &&
				return false;
			}
			while (!t.IsLeaf())
			{
				t = t.LastChild();
				string str = t.Label().Value();
				if (str.StartsWith(baseCat))
				{
					return true;
				}
			}
			return false;
		}

		private static bool LeftRec(Tree t, string baseCat)
		{
			while (!t.IsLeaf())
			{
				t = t.FirstChild();
				string str = t.Label().Value();
				if (str.StartsWith(baseCat))
				{
					return true;
				}
			}
			return false;
		}

		private static int MarkStrahler(Tree t)
		{
			if (t.IsLeaf())
			{
				// don't annotate the words at leaves!
				return 1;
			}
			else
			{
				string cat = t.Label().Value();
				int maxStrahler = -1;
				int maxMultiplicity = 0;
				for (int i = 0; i < t.NumChildren(); i++)
				{
					int strahler = MarkStrahler(t.GetChild(i));
					if (strahler > maxStrahler)
					{
						maxStrahler = strahler;
						maxMultiplicity = 1;
					}
					else
					{
						if (strahler == maxStrahler)
						{
							maxMultiplicity++;
						}
					}
				}
				if (maxMultiplicity > 1)
				{
					maxStrahler++;
				}
				// this is the one case where it grows
				cat = cat + '~' + maxStrahler;
				ILabel label = t.Label().LabelFactory().NewLabel(t.Label());
				label.SetValue(cat);
				t.SetLabel(label);
				return maxStrahler;
			}
		}
	}
}

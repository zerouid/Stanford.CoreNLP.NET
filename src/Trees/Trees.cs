using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Various static utilities for the <code>Tree</code> class.</summary>
	/// <author>Roger Levy</author>
	/// <author>Dan Klein</author>
	/// <author>Aria Haghighi (tree path methods)</author>
	public class Trees
	{
		private static readonly LabeledScoredTreeFactory defaultTreeFactory = new LabeledScoredTreeFactory();

		private Trees()
		{
		}

		/// <summary>
		/// Returns the positional index of the left edge of a tree <i>t</i>
		/// within a given root, as defined by the size of the yield of all
		/// material preceding <i>t</i>.
		/// </summary>
		public static int LeftEdge(Tree t, Tree root)
		{
			MutableInteger i = new MutableInteger(0);
			if (LeftEdge(t, root, i))
			{
				return i;
			}
			else
			{
				throw new Exception("Tree is not a descendant of root.");
			}
		}

		//      return -1;
		/// <summary>
		/// Returns the positional index of the left edge of a tree <i>t</i>
		/// within a given root, as defined by the size of the yield of all
		/// material preceding <i>t</i>.
		/// </summary>
		/// <remarks>
		/// Returns the positional index of the left edge of a tree <i>t</i>
		/// within a given root, as defined by the size of the yield of all
		/// material preceding <i>t</i>.
		/// This method returns -1 if no path is found, rather than exceptioning.
		/// </remarks>
		/// <seealso cref="LeftEdge(Tree, Tree)"/>
		public static int LeftEdgeUnsafe(Tree t, Tree root)
		{
			MutableInteger i = new MutableInteger(0);
			if (LeftEdge(t, root, i))
			{
				return i;
			}
			else
			{
				return -1;
			}
		}

		internal static bool LeftEdge(Tree t, Tree t1, MutableInteger i)
		{
			if (t == t1)
			{
				return true;
			}
			else
			{
				if (t1.IsLeaf())
				{
					int j = t1.Yield().Count;
					// so that empties don't add size
					i.Set(i + j);
					return false;
				}
				else
				{
					foreach (Tree kid in t1.Children())
					{
						if (LeftEdge(t, kid, i))
						{
							return true;
						}
					}
					return false;
				}
			}
		}

		/// <summary>
		/// Returns the positional index of the right edge of a tree
		/// <i>t</i> within a given root, as defined by the size of the yield
		/// of all material preceding <i>t</i> plus all the material
		/// contained in <i>t</i>.
		/// </summary>
		public static int RightEdge(Tree t, Tree root)
		{
			MutableInteger i = new MutableInteger(root.Yield().Count);
			if (RightEdge(t, root, i))
			{
				return i;
			}
			else
			{
				throw new Exception("Tree is not a descendant of root.");
			}
		}

		//      return root.yield().size() + 1;
		/// <summary>
		/// Returns the positional index of the right edge of a tree
		/// <i>t</i> within a given root, as defined by the size of the yield
		/// of all material preceding <i>t</i> plus all the material
		/// contained in <i>t</i>.
		/// </summary>
		/// <remarks>
		/// Returns the positional index of the right edge of a tree
		/// <i>t</i> within a given root, as defined by the size of the yield
		/// of all material preceding <i>t</i> plus all the material
		/// contained in <i>t</i>.
		/// This method returns root.yield().size() + 1 if no path is found, rather than exceptioning.
		/// </remarks>
		/// <seealso cref="RightEdge(Tree, Tree)"/>
		public static int RightEdgeUnsafe(Tree t, Tree root)
		{
			MutableInteger i = new MutableInteger(root.Yield().Count);
			if (RightEdge(t, root, i))
			{
				return i;
			}
			else
			{
				return root.Yield().Count + 1;
			}
		}

		internal static bool RightEdge(Tree t, Tree t1, MutableInteger i)
		{
			if (t == t1)
			{
				return true;
			}
			else
			{
				if (t1.IsLeaf())
				{
					int j = t1.Yield().Count;
					// so that empties don't add size
					i.Set(i - j);
					return false;
				}
				else
				{
					Tree[] kids = t1.Children();
					for (int j = kids.Length - 1; j >= 0; j--)
					{
						if (RightEdge(t, kids[j], i))
						{
							return true;
						}
					}
					return false;
				}
			}
		}

		/// <summary>
		/// Returns a lexicalized Tree whose Labels are CategoryWordTag
		/// instances, all corresponds to the input tree.
		/// </summary>
		public static Tree Lexicalize(Tree t, IHeadFinder hf)
		{
			IFunction<Tree, Tree> a = TreeFunctions.GetLabeledTreeToCategoryWordTagTreeFunction();
			Tree t1 = a.Apply(t);
			t1.PercolateHeads(hf);
			return t1;
		}

		/// <summary>returns the leaves in a Tree in the order that they're found.</summary>
		public static IList<Tree> Leaves(Tree t)
		{
			IList<Tree> l = new List<Tree>();
			Leaves(t, l);
			return l;
		}

		private static void Leaves(Tree t, IList<Tree> l)
		{
			if (t.IsLeaf())
			{
				l.Add(t);
			}
			else
			{
				foreach (Tree kid in t.Children())
				{
					Leaves(kid, l);
				}
			}
		}

		public static IList<Tree> PreTerminals(Tree t)
		{
			IList<Tree> l = new List<Tree>();
			PreTerminals(t, l);
			return l;
		}

		private static void PreTerminals(Tree t, IList<Tree> l)
		{
			if (t.IsPreTerminal())
			{
				l.Add(t);
			}
			else
			{
				foreach (Tree kid in t.Children())
				{
					PreTerminals(kid, l);
				}
			}
		}

		/// <summary>returns the labels of the leaves in a Tree in the order that they're found.</summary>
		public static IList<ILabel> LeafLabels(Tree t)
		{
			IList<ILabel> l = new List<ILabel>();
			LeafLabels(t, l);
			return l;
		}

		private static void LeafLabels(Tree t, IList<ILabel> l)
		{
			if (t.IsLeaf())
			{
				l.Add(t.Label());
			}
			else
			{
				foreach (Tree kid in t.Children())
				{
					LeafLabels(kid, l);
				}
			}
		}

		/// <summary>returns the labels of the leaves in a Tree, augmented with POS tags.</summary>
		/// <remarks>
		/// returns the labels of the leaves in a Tree, augmented with POS tags.  assumes that
		/// the labels are CoreLabels.
		/// </remarks>
		public static IList<CoreLabel> TaggedLeafLabels(Tree t)
		{
			IList<CoreLabel> l = new List<CoreLabel>();
			TaggedLeafLabels(t, l);
			return l;
		}

		private static void TaggedLeafLabels(Tree t, IList<CoreLabel> l)
		{
			if (t.IsPreTerminal())
			{
				CoreLabel fl = (CoreLabel)t.GetChild(0).Label();
				fl.Set(typeof(CoreAnnotations.TagLabelAnnotation), t.Label());
				l.Add(fl);
			}
			else
			{
				foreach (Tree kid in t.Children())
				{
					TaggedLeafLabels(kid, l);
				}
			}
		}

		/// <summary>
		/// Given a tree, set the tags on the leaf nodes if they are not
		/// already set.
		/// </summary>
		/// <remarks>
		/// Given a tree, set the tags on the leaf nodes if they are not
		/// already set.  Do this by using the preterminal's value as a tag.
		/// </remarks>
		public static void SetLeafTagsIfUnset(Tree tree)
		{
			if (tree.IsPreTerminal())
			{
				Tree leaf = tree.Children()[0];
				if (!(leaf.Label() is IHasTag))
				{
					return;
				}
				IHasTag label = (IHasTag)leaf.Label();
				if (label.Tag() == null)
				{
					label.SetTag(tree.Value());
				}
			}
			else
			{
				foreach (Tree child in tree.Children())
				{
					SetLeafTagsIfUnset(child);
				}
			}
		}

		/// <summary>Replace the labels of the leaves with the given leaves.</summary>
		public static void SetLeafLabels(Tree tree, IList<ILabel> labels)
		{
			IEnumerator<Tree> leafIterator = tree.GetLeaves().GetEnumerator();
			IEnumerator<ILabel> labelIterator = labels.GetEnumerator();
			while (leafIterator.MoveNext() && labelIterator.MoveNext())
			{
				Tree leaf = leafIterator.Current;
				ILabel label = labelIterator.Current;
				leaf.SetLabel(label);
			}
			//leafIterator.next().setLabel(labelIterator.next());
			if (leafIterator.MoveNext())
			{
				throw new ArgumentException("Tree had more leaves than the labels provided");
			}
			if (labelIterator.MoveNext())
			{
				throw new ArgumentException("More labels provided than tree had leaves");
			}
		}

		/// <summary>
		/// returns the maximal projection of <code>head</code> in
		/// <code>root</code> given a
		/// <see cref="IHeadFinder"/>
		/// </summary>
		public static Tree MaximalProjection(Tree head, Tree root, IHeadFinder hf)
		{
			Tree projection = head;
			if (projection == root)
			{
				return root;
			}
			Tree parent = projection.Parent(root);
			while (hf.DetermineHead(parent) == projection)
			{
				projection = parent;
				if (projection == root)
				{
					return root;
				}
				parent = projection.Parent(root);
			}
			return projection;
		}

		/* applies a TreeVisitor to all projections (including the node itself) of a node in a Tree.
		*  Does nothing if head is not in root.
		* @return the maximal projection of head in root.
		*/
		public static Tree ApplyToProjections(ITreeVisitor v, Tree head, Tree root, IHeadFinder hf)
		{
			Tree projection = head;
			Tree parent = projection.Parent(root);
			if (parent == null && projection != root)
			{
				return null;
			}
			v.VisitTree(projection);
			if (projection == root)
			{
				return root;
			}
			while (hf.DetermineHead(parent) == projection)
			{
				projection = parent;
				v.VisitTree(projection);
				if (projection == root)
				{
					return root;
				}
				parent = projection.Parent(root);
			}
			return projection;
		}

		/// <summary>gets the <code>n</code>th terminal in <code>tree</code>.</summary>
		/// <remarks>gets the <code>n</code>th terminal in <code>tree</code>.  The first terminal is number zero.</remarks>
		public static Tree GetTerminal(Tree tree, int n)
		{
			return GetTerminal(tree, new MutableInteger(0), n);
		}

		internal static Tree GetTerminal(Tree tree, MutableInteger i, int n)
		{
			if (i == n)
			{
				if (tree.IsLeaf())
				{
					return tree;
				}
				else
				{
					return GetTerminal(tree.Children()[0], i, n);
				}
			}
			else
			{
				if (tree.IsLeaf())
				{
					i.Set(i + tree.Yield().Count);
					return null;
				}
				else
				{
					foreach (Tree kid in tree.Children())
					{
						Tree result = GetTerminal(kid, i, n);
						if (result != null)
						{
							return result;
						}
					}
					return null;
				}
			}
		}

		/// <summary>gets the <code>n</code>th preterminal in <code>tree</code>.</summary>
		/// <remarks>gets the <code>n</code>th preterminal in <code>tree</code>.  The first terminal is number zero.</remarks>
		public static Tree GetPreTerminal(Tree tree, int n)
		{
			return GetPreTerminal(tree, new MutableInteger(0), n);
		}

		internal static Tree GetPreTerminal(Tree tree, MutableInteger i, int n)
		{
			if (i == n)
			{
				if (tree.IsPreTerminal())
				{
					return tree;
				}
				else
				{
					return GetPreTerminal(tree.Children()[0], i, n);
				}
			}
			else
			{
				if (tree.IsPreTerminal())
				{
					i.Set(i + tree.Yield().Count);
					return null;
				}
				else
				{
					foreach (Tree kid in tree.Children())
					{
						Tree result = GetPreTerminal(kid, i, n);
						if (result != null)
						{
							return result;
						}
					}
					return null;
				}
			}
		}

		/// <summary>returns the syntactic category of the tree as a list of the syntactic categories of the mother and the daughters</summary>
		public static IList<string> LocalTreeAsCatList(Tree t)
		{
			IList<string> l = new List<string>(t.Children().Length + 1);
			l.Add(t.Label().Value());
			for (int i = 0; i < t.Children().Length; i++)
			{
				l.Add(t.Children()[i].Label().Value());
			}
			return l;
		}

		/// <summary>Returns the index of <code>daughter</code> in <code>parent</code> by ==.</summary>
		/// <remarks>
		/// Returns the index of <code>daughter</code> in <code>parent</code> by ==.
		/// Returns -1 if <code>daughter</code> not found.
		/// </remarks>
		public static int ObjectEqualityIndexOf(Tree parent, Tree daughter)
		{
			for (int i = 0; i < parent.Children().Length; i++)
			{
				if (daughter == parent.Children()[i])
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns a String reporting what kinds of Tree and Label nodes this
		/// Tree contains.
		/// </summary>
		/// <param name="t">The tree to examine.</param>
		/// <returns>
		/// A human-readable String reporting what kinds of Tree and Label nodes this
		/// Tree contains.
		/// </returns>
		public static string ToStructureDebugString(Tree t)
		{
			string tCl = StringUtils.GetShortClassName(t);
			string tfCl = StringUtils.GetShortClassName(t.TreeFactory());
			string lCl = StringUtils.GetShortClassName(t.Label());
			string lfCl = StringUtils.GetShortClassName(t.Label().LabelFactory());
			ICollection<string> otherClasses = Generics.NewHashSet();
			string leafLabels = null;
			string tagLabels = null;
			string phraseLabels = null;
			string leaves = null;
			string nodes = null;
			foreach (Tree st in t)
			{
				string stCl = StringUtils.GetShortClassName(st);
				string stfCl = StringUtils.GetShortClassName(st.TreeFactory());
				string slCl = StringUtils.GetShortClassName(st.Label());
				string slfCl = StringUtils.GetShortClassName(st.Label().LabelFactory());
				if (!tCl.Equals(stCl))
				{
					otherClasses.Add(stCl);
				}
				if (!tfCl.Equals(stfCl))
				{
					otherClasses.Add(stfCl);
				}
				if (!lCl.Equals(slCl))
				{
					otherClasses.Add(slCl);
				}
				if (!lfCl.Equals(slfCl))
				{
					otherClasses.Add(slfCl);
				}
				if (st.IsPhrasal())
				{
					if (nodes == null)
					{
						nodes = stCl;
					}
					else
					{
						if (!nodes.Equals(stCl))
						{
							nodes = "mixed";
						}
					}
					if (phraseLabels == null)
					{
						phraseLabels = slCl;
					}
					else
					{
						if (!phraseLabels.Equals(slCl))
						{
							phraseLabels = "mixed";
						}
					}
				}
				else
				{
					if (st.IsPreTerminal())
					{
						if (nodes == null)
						{
							nodes = stCl;
						}
						else
						{
							if (!nodes.Equals(stCl))
							{
								nodes = "mixed";
							}
						}
						if (tagLabels == null)
						{
							tagLabels = StringUtils.GetShortClassName(slCl);
						}
						else
						{
							if (!tagLabels.Equals(slCl))
							{
								tagLabels = "mixed";
							}
						}
					}
					else
					{
						if (st.IsLeaf())
						{
							if (leaves == null)
							{
								leaves = stCl;
							}
							else
							{
								if (!leaves.Equals(stCl))
								{
									leaves = "mixed";
								}
							}
							if (leafLabels == null)
							{
								leafLabels = slCl;
							}
							else
							{
								if (!leafLabels.Equals(slCl))
								{
									leafLabels = "mixed";
								}
							}
						}
						else
						{
							throw new InvalidOperationException("Bad tree state: " + t);
						}
					}
				}
			}
			// end for Tree st : this
			StringBuilder sb = new StringBuilder();
			sb.Append("Tree with root of class ").Append(tCl).Append(" and factory ").Append(tfCl);
			sb.Append(" and root label class ").Append(lCl).Append(" and factory ").Append(lfCl);
			if (!otherClasses.IsEmpty())
			{
				sb.Append(" and the following classes also found within the tree: ").Append(otherClasses);
				return " with " + nodes + " interior nodes and " + leaves + " leaves, and " + phraseLabels + " phrase labels, " + tagLabels + " tag labels, and " + leafLabels + " leaf labels.";
			}
			else
			{
				sb.Append(" (and uniform use of these Tree and Label classes throughout the tree).");
			}
			return sb.ToString();
		}

		/// <summary>Turns a sentence into a flat phrasal tree.</summary>
		/// <remarks>
		/// Turns a sentence into a flat phrasal tree.
		/// The structure is S -&gt; tag*.  And then each tag goes to a word.
		/// The tag is either found from the label or made "WD".
		/// The tag and phrasal node have a StringLabel.
		/// </remarks>
		/// <param name="s">The Sentence to make the Tree from</param>
		/// <returns>The one phrasal level Tree</returns>
		public static Tree ToFlatTree(IList<IHasWord> s)
		{
			return ToFlatTree(s, new StringLabelFactory());
		}

		/// <summary>Turns a sentence into a flat phrasal tree.</summary>
		/// <remarks>
		/// Turns a sentence into a flat phrasal tree.
		/// The structure is S -&gt; tag*.  And then each tag goes to a word.
		/// The tag is either found from the label or made "WD".
		/// The tag and phrasal node have a StringLabel.
		/// </remarks>
		/// <param name="s">The Sentence to make the Tree from</param>
		/// <param name="lf">The LabelFactory with which to create the new Tree labels</param>
		/// <returns>The one phrasal level Tree</returns>
		public static Tree ToFlatTree<_T0>(IList<_T0> s, ILabelFactory lf)
			where _T0 : IHasWord
		{
			IList<Tree> daughters = new List<Tree>(s.Count);
			foreach (IHasWord word in s)
			{
				Tree wordNode = new LabeledScoredTreeNode(lf.NewLabel(word.Word()));
				if (word is TaggedWord)
				{
					TaggedWord taggedWord = (TaggedWord)word;
					wordNode = new LabeledScoredTreeNode(new StringLabel(taggedWord.Tag()), Java.Util.Collections.SingletonList(wordNode));
				}
				else
				{
					wordNode = new LabeledScoredTreeNode(lf.NewLabel("WD"), Java.Util.Collections.SingletonList(wordNode));
				}
				daughters.Add(wordNode);
			}
			return new LabeledScoredTreeNode(new StringLabel("S"), daughters);
		}

		public static string TreeToLatex(Tree t)
		{
			StringBuilder connections = new StringBuilder();
			StringBuilder hierarchy = new StringBuilder();
			TreeToLatexHelper(t, connections, hierarchy, 0, 1, 0);
			return "\\tree" + hierarchy + '\n' + connections + '\n';
		}

		private static int TreeToLatexHelper(Tree t, StringBuilder c, StringBuilder h, int n, int nextN, int indent)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < indent; i++)
			{
				sb.Append("  ");
			}
			h.Append('\n').Append(sb);
			h.Append("{\\").Append(t.IsLeaf() ? string.Empty : "n").Append("tnode{z").Append(n).Append("}{").Append(t.Label()).Append('}');
			if (!t.IsLeaf())
			{
				for (int k = 0; k < t.Children().Length; k++)
				{
					h.Append(", ");
					c.Append("\\nodeconnect{z").Append(n).Append("}{z").Append(nextN).Append("}\n");
					nextN = TreeToLatexHelper(t.Children()[k], c, h, nextN, nextN + 1, indent + 1);
				}
			}
			h.Append('}');
			return nextN;
		}

		public static string TreeToLatexEven(Tree t)
		{
			StringBuilder connections = new StringBuilder();
			StringBuilder hierarchy = new StringBuilder();
			int maxDepth = t.Depth();
			TreeToLatexEvenHelper(t, connections, hierarchy, 0, 1, 0, 0, maxDepth);
			return "\\tree" + hierarchy + '\n' + connections + '\n';
		}

		private static int TreeToLatexEvenHelper(Tree t, StringBuilder c, StringBuilder h, int n, int nextN, int indent, int curDepth, int maxDepth)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < indent; i++)
			{
				sb.Append("  ");
			}
			h.Append('\n').Append(sb);
			int tDepth = t.Depth();
			if (tDepth == 0 && tDepth + curDepth < maxDepth)
			{
				for (int pad = 0; pad < maxDepth - tDepth - curDepth; pad++)
				{
					h.Append("{\\ntnode{pad}{}, ");
				}
			}
			h.Append("{\\ntnode{z").Append(n).Append("}{").Append(t.Label()).Append('}');
			if (!t.IsLeaf())
			{
				for (int k = 0; k < t.Children().Length; k++)
				{
					h.Append(", ");
					c.Append("\\nodeconnect{z").Append(n).Append("}{z").Append(nextN).Append("}\n");
					nextN = TreeToLatexEvenHelper(t.Children()[k], c, h, nextN, nextN + 1, indent + 1, curDepth + 1, maxDepth);
				}
			}
			if (tDepth == 0 && tDepth + curDepth < maxDepth)
			{
				for (int pad = 0; pad < maxDepth - tDepth - curDepth; pad++)
				{
					h.Append('}');
				}
			}
			h.Append('}');
			return nextN;
		}

		internal static string TexTree(Tree t)
		{
			return TreeToLatex(t);
		}

		internal static string Escape(string s)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char c = s[i];
				if (c == '^')
				{
					sb.Append('\\');
				}
				sb.Append(c);
				if (c == '^')
				{
					sb.Append("{}");
				}
			}
			return sb.ToString();
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			int i = 0;
			while (i < args.Length)
			{
				Tree tree = Tree.ValueOf(args[i]);
				if (tree == null)
				{
					// maybe it was a filename
					tree = Tree.ValueOf(IOUtils.SlurpFile(args[i]));
				}
				if (tree != null)
				{
					System.Console.Out.WriteLine(Escape(TexTree(tree)));
				}
				i++;
			}
			if (i == 0)
			{
				Tree tree = (new PennTreeReader(new BufferedReader(new InputStreamReader(Runtime.@in)), new LabeledScoredTreeFactory(new StringLabelFactory()))).ReadTree();
				System.Console.Out.WriteLine(Escape(TexTree(tree)));
			}
		}

		public static Tree NormalizeTree(Tree tree, TreeNormalizer tn, ITreeFactory tf)
		{
			foreach (Tree node in tree)
			{
				if (node.IsLeaf())
				{
					node.Label().SetValue(tn.NormalizeTerminal(node.Label().Value()));
				}
				else
				{
					node.Label().SetValue(tn.NormalizeNonterminal(node.Label().Value()));
				}
			}
			return tn.NormalizeWholeTree(tree, tf);
		}

		/// <summary>Gets the <i>i</i>th leaf of a tree from the left.</summary>
		/// <remarks>
		/// Gets the <i>i</i>th leaf of a tree from the left.
		/// The leftmost leaf is numbered 0.
		/// </remarks>
		/// <returns>
		/// The <i>i</i><sup>th</sup> leaf as a Tree, or <code>null</code>
		/// if there is no such leaf.
		/// </returns>
		public static Tree GetLeaf(Tree tree, int i)
		{
			int count = -1;
			foreach (Tree next in tree)
			{
				if (next.IsLeaf())
				{
					count++;
				}
				if (count == i)
				{
					return next;
				}
			}
			return null;
		}

		/// <summary>Get lowest common ancestor of all the nodes in the list with the tree rooted at root</summary>
		public static Tree GetLowestCommonAncestor(IList<Tree> nodes, Tree root)
		{
			IList<IList<Tree>> paths = new List<IList<Tree>>();
			int min = int.MaxValue;
			foreach (Tree t in nodes)
			{
				IList<Tree> path = PathFromRoot(t, root);
				if (path == null)
				{
					return null;
				}
				min = Math.Min(min, path.Count);
				paths.Add(path);
			}
			Tree commonAncestor = null;
			for (int i = 0; i < min; ++i)
			{
				Tree ancestor = paths[0][i];
				bool quit = false;
				foreach (IList<Tree> path in paths)
				{
					if (!path[i].Equals(ancestor))
					{
						quit = true;
						break;
					}
				}
				if (quit)
				{
					break;
				}
				commonAncestor = ancestor;
			}
			return commonAncestor;
		}

		/// <summary>
		/// returns a list of categories that is the path from Tree from to Tree
		/// to within Tree root.
		/// </summary>
		/// <remarks>
		/// returns a list of categories that is the path from Tree from to Tree
		/// to within Tree root.  If either from or to is not in root,
		/// returns null.  Otherwise includes both from and to in the list.
		/// </remarks>
		public static IList<string> PathNodeToNode(Tree from, Tree to, Tree root)
		{
			IList<Tree> fromPath = PathFromRoot(from, root);
			//System.out.println(treeListToCatList(fromPath));
			if (fromPath == null)
			{
				return null;
			}
			IList<Tree> toPath = PathFromRoot(to, root);
			//System.out.println(treeListToCatList(toPath));
			if (toPath == null)
			{
				return null;
			}
			//System.out.println(treeListToCatList(fromPath));
			//System.out.println(treeListToCatList(toPath));
			int last = 0;
			int min = fromPath.Count <= toPath.Count ? fromPath.Count : toPath.Count;
			Tree lastNode = null;
			//     while((! (fromPath.isEmpty() || toPath.isEmpty())) &&  fromPath.get(0).equals(toPath.get(0))) {
			//       lastNode = (Tree) fromPath.remove(0);
			//       toPath.remove(0);
			//     }
			while (last < min && fromPath[last].Equals(toPath[last]))
			{
				lastNode = fromPath[last];
				last++;
			}
			//System.out.println(treeListToCatList(fromPath));
			//System.out.println(treeListToCatList(toPath));
			IList<string> totalPath = new List<string>();
			for (int i = fromPath.Count - 1; i >= last; i--)
			{
				Tree t = fromPath[i];
				totalPath.Add("up-" + t.Label().Value());
			}
			if (lastNode != null)
			{
				totalPath.Add("up-" + lastNode.Label().Value());
			}
			foreach (Tree t_1 in toPath)
			{
				totalPath.Add("down-" + t_1.Label().Value());
			}
			//     for(ListIterator i = fromPath.listIterator(fromPath.size()); i.hasPrevious(); ){
			//       Tree t = (Tree) i.previous();
			//       totalPath.add("up-" + t.label().value());
			//     }
			//     if(lastNode != null)
			//     totalPath.add("up-" + lastNode.label().value());
			//     for(ListIterator j = toPath.listIterator(); j.hasNext(); ){
			//       Tree t = (Tree) j.next();
			//       totalPath.add("down-" + t.label().value());
			//     }
			return totalPath;
		}

		/// <summary>returns list of tree nodes to root from t.</summary>
		/// <remarks>
		/// returns list of tree nodes to root from t.  Includes root and
		/// t. Returns null if tree not found dominated by root
		/// </remarks>
		public static IList<Tree> PathFromRoot(Tree t, Tree root)
		{
			if (t == root)
			{
				//if (t.equals(root)) {
				IList<Tree> l = new List<Tree>(1);
				l.Add(t);
				return l;
			}
			else
			{
				if (root == null)
				{
					return null;
				}
			}
			return root.DominationPath(t);
		}

		/// <summary>replaces all instances (by ==) of node with node1.</summary>
		/// <remarks>
		/// replaces all instances (by ==) of node with node1.  Doesn't affect
		/// the node t itself
		/// </remarks>
		public static void ReplaceNode(Tree node, Tree node1, Tree t)
		{
			if (t.IsLeaf())
			{
				return;
			}
			Tree[] kids = t.Children();
			IList<Tree> newKids = new List<Tree>(kids.Length);
			foreach (Tree kid in kids)
			{
				if (kid != node)
				{
					newKids.Add(kid);
					ReplaceNode(node, node1, kid);
				}
				else
				{
					newKids.Add(node1);
				}
			}
			t.SetChildren(newKids);
		}

		/// <summary>
		/// returns the node of a tree which represents the lowest common
		/// ancestor of nodes t1 and t2 dominated by root.
		/// </summary>
		/// <remarks>
		/// returns the node of a tree which represents the lowest common
		/// ancestor of nodes t1 and t2 dominated by root. If either t1 or
		/// or t2 is not dominated by root, returns null.
		/// </remarks>
		public static Tree GetLowestCommonAncestor(Tree t1, Tree t2, Tree root)
		{
			IList<Tree> t1Path = PathFromRoot(t1, root);
			IList<Tree> t2Path = PathFromRoot(t2, root);
			if (t1Path == null || t2Path == null)
			{
				return null;
			}
			int min = Math.Min(t1Path.Count, t2Path.Count);
			Tree commonAncestor = null;
			for (int i = 0; i < min && t1Path[i].Equals(t2Path[i]); ++i)
			{
				commonAncestor = t1Path[i];
			}
			return commonAncestor;
		}

		// todo [cdm 2015]: These next two methods duplicate the Tree.valueOf methods!
		/// <summary>Simple tree reading utility method.</summary>
		/// <remarks>Simple tree reading utility method.  Given a tree formatted as a PTB string, returns a Tree made by a specific TreeFactory.</remarks>
		public static Tree ReadTree(string ptbTreeString, ITreeFactory treeFactory)
		{
			try
			{
				PennTreeReader ptr = new PennTreeReader(new StringReader(ptbTreeString), treeFactory);
				return ptr.ReadTree();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>Simple tree reading utility method.</summary>
		/// <remarks>Simple tree reading utility method.  Given a tree formatted as a PTB string, returns a Tree made by the default TreeFactory (LabeledScoredTreeFactory)</remarks>
		public static Tree ReadTree(string str)
		{
			return ReadTree(str, defaultTreeFactory);
		}

		/// <summary>Outputs the labels on the trees, not just the words.</summary>
		public static void OutputTreeLabels(Tree tree)
		{
			OutputTreeLabels(tree, 0);
		}

		public static void OutputTreeLabels(Tree tree, int depth)
		{
			for (int i = 0; i < depth; ++i)
			{
				System.Console.Out.Write(" ");
			}
			System.Console.Out.WriteLine(tree.Label());
			foreach (Tree child in tree.Children())
			{
				OutputTreeLabels(child, depth + 1);
			}
		}

		/// <summary>Converts the tree labels to CoreLabels.</summary>
		/// <remarks>
		/// Converts the tree labels to CoreLabels.
		/// We need this because we store additional info in the CoreLabel, like token span.
		/// </remarks>
		/// <param name="tree"/>
		public static void ConvertToCoreLabels(Tree tree)
		{
			ILabel l = tree.Label();
			if (!(l is CoreLabel))
			{
				CoreLabel cl = new CoreLabel();
				cl.SetValue(l.Value());
				tree.SetLabel(cl);
			}
			foreach (Tree kid in tree.Children())
			{
				ConvertToCoreLabels(kid);
			}
		}

		/// <summary>
		/// Set the sentence index of all the leaves in the tree
		/// (only works on CoreLabel)
		/// </summary>
		public static void SetSentIndex(Tree tree, int sentIndex)
		{
			IList<ILabel> leaves = tree.Yield();
			foreach (ILabel leaf in leaves)
			{
				if (!(leaf is CoreLabel))
				{
					throw new ArgumentException("Only works on CoreLabel");
				}
				((CoreLabel)leaf).SetSentIndex(sentIndex);
			}
		}
	}
}

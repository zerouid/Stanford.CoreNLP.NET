using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Graph;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees.UD;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A
	/// <c>GrammaticalStructure</c>
	/// stores dependency relations between
	/// nodes in a tree.  A new
	/// <c>GrammaticalStructure</c>
	/// is constructed
	/// from an existing parse tree with the help of
	/// <see cref="GrammaticalRelation"/>
	/// 
	/// <c>GrammaticalRelation</c>
	/// }, which
	/// defines a hierarchy of grammatical relations, along with
	/// patterns for identifying them in parse trees.  The constructor for
	/// <c>GrammaticalStructure</c>
	/// uses these definitions to
	/// populate the new
	/// <c>GrammaticalStructure</c>
	/// with as many
	/// labeled grammatical relations as it can.  Once constructed, the new
	/// <c>GrammaticalStructure</c>
	/// can be printed in various
	/// formats, or interrogated using the interface methods in this
	/// class. Internally, this uses a representation via a
	/// <c>TreeGraphNode</c>
	/// ,
	/// that is, a tree with additional labeled
	/// arcs between nodes, for representing the grammatical relations in a
	/// parse tree.
	/// </summary>
	/// <author>Bill MacCartney</author>
	/// <author>Galen Andrew (refactoring English-specific stuff)</author>
	/// <author>Ilya Sherman (dependencies)</author>
	/// <author>Daniel Cer</author>
	/// <seealso cref="EnglishGrammaticalRelations"/>
	/// <seealso cref="GrammaticalRelation"/>
	/// <seealso cref="EnglishGrammaticalStructure"/>
	[System.Serializable]
	public abstract class GrammaticalStructure
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.GrammaticalStructure));

		private static readonly bool PrintDebugging = Runtime.GetProperty("GrammaticalStructure", null) != null;

		/// <summary>A specification for the types of extra edges to add to the dependency tree.</summary>
		/// <remarks>
		/// A specification for the types of extra edges to add to the dependency tree.
		/// If you're in doubt, use
		/// <see cref="None"/>
		/// .
		/// </remarks>
		[System.Serializable]
		public sealed class Extras
		{
			/// <summary>Don't include any additional edges.</summary>
			/// <remarks>
			/// Don't include any additional edges.
			/// Note: In older code (2014 and before) including extras was a boolean flag. This option is the equivalent of
			/// the
			/// <see langword="false"/>
			/// flag.
			/// </remarks>
			public static readonly GrammaticalStructure.Extras None = new GrammaticalStructure.Extras(false, false, false);

			/// <summary>Include only the extra reference edges, and save them as reference edges without collapsing.</summary>
			public static readonly GrammaticalStructure.Extras RefOnlyUncollapsed = new GrammaticalStructure.Extras(true, false, false);

			/// <summary>Include only the extra reference edges, but collapsing these edges to clone the edge type of the referent.</summary>
			/// <remarks>
			/// Include only the extra reference edges, but collapsing these edges to clone the edge type of the referent.
			/// So, for example, <i>My dog who eats sausage</i> may have a "ref" edge from <i>who</i> to <i>dog</i>
			/// that would be deleted and replaced with an "nsubj" edge from <i>eats</i> to <i>dog</i>.
			/// </remarks>
			public static readonly GrammaticalStructure.Extras RefOnlyCollapsed = new GrammaticalStructure.Extras(true, false, true);

			/// <summary>Add extra subjects only, not adding any of the other extra edge types.</summary>
			public static readonly GrammaticalStructure.Extras SubjOnly = new GrammaticalStructure.Extras(false, true, false);

			/// <seealso cref="SubjOnly"/>
			/// <seealso cref="RefOnlyUncollapsed"/>
			public static readonly GrammaticalStructure.Extras RefUncollapsedAndSubj = new GrammaticalStructure.Extras(true, true, false);

			/// <seealso cref="SubjOnly"/>
			/// <seealso cref="RefOnlyCollapsed"/>
			public static readonly GrammaticalStructure.Extras RefCollapsedAndSubj = new GrammaticalStructure.Extras(true, true, true);

			/// <summary>Do the maximal amount of extra processing.</summary>
			/// <remarks>
			/// Do the maximal amount of extra processing.
			/// Currently, this is equivalent to
			/// <see cref="RefCollapsedAndSubj"/>
			/// .
			/// Note: In older code (2014 and before) including extras was a boolean flag. This option is the equivalent of
			/// the
			/// <see langword="true"/>
			/// flag.
			/// </remarks>
			public static readonly GrammaticalStructure.Extras Maximal = new GrammaticalStructure.Extras(true, true, true);

			/// <summary>Add "ref" edges</summary>
			public readonly bool doRef;

			/// <summary>Add extra subject edges</summary>
			public readonly bool doSubj;

			/// <summary>collapse the "ref" edges</summary>
			public readonly bool collapseRef;

			/// <summary>Constructor.</summary>
			/// <remarks>Constructor. Nothing exciting here.</remarks>
			internal Extras(bool doRef, bool doSubj, bool collapseRef)
			{
				this.doRef = doRef;
				this.doSubj = doSubj;
				this.collapseRef = collapseRef;
			}
		}

		protected internal readonly IList<TypedDependency> typedDependencies;

		protected internal readonly IList<TypedDependency> allTypedDependencies;

		protected internal readonly IPredicate<string> puncFilter;

		protected internal readonly IPredicate<string> tagFilter;

		/// <summary>The root Tree node for this GrammaticalStructure.</summary>
		private readonly TreeGraphNode root;

		/// <summary>A map from arbitrary integer indices to nodes.</summary>
		private readonly IDictionary<int, TreeGraphNode> indexMap = Generics.NewHashMap();

		/// <summary>
		/// Create a new GrammaticalStructure, analyzing the parse tree and
		/// populate the GrammaticalStructure with as many labeled
		/// grammatical relation arcs as possible.
		/// </summary>
		/// <param name="t">A Tree to analyze</param>
		/// <param name="relations">A set of GrammaticalRelations to consider</param>
		/// <param name="relationsLock">Something needed to make this thread-safe when iterating over relations</param>
		/// <param name="transformer">
		/// A tree transformer to apply to the tree before converting (this argument
		/// may be null if no transformer is required)
		/// </param>
		/// <param name="hf">A HeadFinder for analysis</param>
		/// <param name="puncFilter">
		/// A Filter to reject punctuation. To delete punctuation
		/// dependencies, this filter should return false on
		/// punctuation word strings, and true otherwise.
		/// If punctuation dependencies should be kept, you
		/// should pass in a
		/// <c>Filters.&lt;String&gt;acceptFilter()</c>
		/// .
		/// </param>
		/// <param name="tagFilter">Appears to be unused (filters out tags??)</param>
		public GrammaticalStructure(Tree t, ICollection<GrammaticalRelation> relations, ILock relationsLock, ITreeTransformer transformer, IHeadFinder hf, IPredicate<string> puncFilter, IPredicate<string> tagFilter)
		{
			// end enum Extras
			TreeGraphNode treeGraph = new TreeGraphNode(t, (TreeGraphNode)null);
			// TODO: create the tree and reuse the leaf labels in one pass,
			// avoiding a wasteful copy of the labels.
			Edu.Stanford.Nlp.Trees.Trees.SetLeafLabels(treeGraph, t.Yield());
			Edu.Stanford.Nlp.Trees.Trees.SetLeafTagsIfUnset(treeGraph);
			if (transformer != null)
			{
				Tree transformed = transformer.TransformTree(treeGraph);
				if (!(transformed is TreeGraphNode))
				{
					throw new Exception("Transformer did not change TreeGraphNode into another TreeGraphNode: " + transformer);
				}
				this.root = (TreeGraphNode)transformed;
			}
			else
			{
				this.root = treeGraph;
			}
			IndexNodes(this.root);
			// add head word and tag to phrase nodes
			if (hf == null)
			{
				throw new AssertionError("Cannot use null HeadFinder");
			}
			root.PercolateHeads(hf);
			if (root.Value() == null)
			{
				root.SetValue("ROOT");
			}
			// todo: cdm: it doesn't seem like this line should be here
			// add dependencies, using heads
			this.puncFilter = puncFilter;
			this.tagFilter = tagFilter;
			// NoPunctFilter puncDepFilter = new NoPunctFilter(puncFilter);
			GrammaticalStructure.NoPunctTypedDependencyFilter puncTypedDepFilter = new GrammaticalStructure.NoPunctTypedDependencyFilter(puncFilter, tagFilter);
			DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> basicGraph = new DirectedMultiGraph<TreeGraphNode, GrammaticalRelation>();
			DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> completeGraph = new DirectedMultiGraph<TreeGraphNode, GrammaticalRelation>();
			// analyze the root (and its descendants, recursively)
			if (relationsLock != null)
			{
				relationsLock.Lock();
			}
			try
			{
				AnalyzeNode(root, root, relations, hf, puncFilter, tagFilter, basicGraph, completeGraph);
			}
			finally
			{
				if (relationsLock != null)
				{
					relationsLock.Unlock();
				}
			}
			AttachStrandedNodes(root, root, false, puncFilter, tagFilter, basicGraph);
			// add typed dependencies
			typedDependencies = GetDeps(puncTypedDepFilter, basicGraph);
			allTypedDependencies = Generics.NewArrayList(typedDependencies);
			GetExtraDeps(allTypedDependencies, puncTypedDepFilter, completeGraph);
		}

		/// <summary>
		/// Assign sequential integer indices (starting with 1) to all
		/// nodes of the subtree rooted at this
		/// <c>Tree</c>
		/// .  The leaves are indexed first,
		/// from left to right.  Then the internal nodes are indexed,
		/// using a pre-order tree traversal.
		/// </summary>
		private void IndexNodes(TreeGraphNode tree)
		{
			IndexNodes(tree, IndexLeaves(tree, 1));
		}

		/// <summary>
		/// Assign sequential integer indices to the leaves of the subtree
		/// rooted at this
		/// <c>TreeGraphNode</c>
		/// , beginning with
		/// <paramref name="startIndex"/>
		/// , and traversing the leaves from left
		/// to right. If node is already indexed, then it uses the existing index.
		/// </summary>
		/// <param name="startIndex">index for this node</param>
		/// <returns>the next index still unassigned</returns>
		private int IndexLeaves(TreeGraphNode tree, int startIndex)
		{
			if (tree.IsLeaf())
			{
				int oldIndex = tree.Index();
				if (oldIndex >= 0)
				{
					startIndex = oldIndex;
				}
				else
				{
					tree.SetIndex(startIndex);
				}
				AddNodeToIndexMap(startIndex, tree);
				startIndex++;
			}
			else
			{
				foreach (TreeGraphNode child in tree.children)
				{
					startIndex = IndexLeaves(child, startIndex);
				}
			}
			return startIndex;
		}

		/// <summary>
		/// Assign sequential integer indices to all nodes of the subtree
		/// rooted at this
		/// <c>TreeGraphNode</c>
		/// , beginning with
		/// <paramref name="startIndex"/>
		/// , and doing a pre-order tree traversal.
		/// Any node which already has an index will not be re-indexed
		/// &mdash; this is so that we can index the leaves first, and
		/// then index the rest.
		/// </summary>
		/// <param name="startIndex">index for this node</param>
		/// <returns>the next index still unassigned</returns>
		private int IndexNodes(TreeGraphNode tree, int startIndex)
		{
			if (tree.Index() < 0)
			{
				// if this node has no index
				AddNodeToIndexMap(startIndex, tree);
				tree.SetIndex(startIndex++);
			}
			if (!tree.IsLeaf())
			{
				foreach (TreeGraphNode child in tree.children)
				{
					startIndex = IndexNodes(child, startIndex);
				}
			}
			return startIndex;
		}

		/// <summary>
		/// Store a mapping from an arbitrary integer index to a node in
		/// this treegraph.
		/// </summary>
		/// <remarks>
		/// Store a mapping from an arbitrary integer index to a node in
		/// this treegraph.  Normally a client shouldn't need to use this,
		/// as the nodes are automatically indexed by the
		/// <c>TreeGraph</c>
		/// constructor.
		/// </remarks>
		/// <param name="index">the arbitrary integer index</param>
		/// <param name="node">
		/// the
		/// <c>TreeGraphNode</c>
		/// to be indexed
		/// </param>
		private void AddNodeToIndexMap(int index, TreeGraphNode node)
		{
			indexMap[int.Parse(index)] = node;
		}

		/// <summary>
		/// Return the node in the this treegraph corresponding to the
		/// specified integer index.
		/// </summary>
		/// <param name="index">the integer index of the node you want</param>
		/// <returns>
		/// the
		/// <c>TreeGraphNode</c>
		/// having the specified
		/// index (or
		/// <see langword="null"/>
		/// if such does not exist)
		/// </returns>
		private TreeGraphNode GetNodeByIndex(int index)
		{
			return indexMap[int.Parse(index)];
		}

		/// <summary>Return the root Tree of this GrammaticalStructure.</summary>
		/// <returns>the root Tree of this GrammaticalStructure</returns>
		public virtual TreeGraphNode Root()
		{
			return root;
		}

		private static void ThrowDepFormatException(string dep)
		{
			throw new Exception(string.Format("Dependencies should be for the format 'type(arg-idx, arg-idx)'. Could not parse '%s'", dep));
		}

		/// <summary>Create a grammatical structure from its string representation.</summary>
		/// <remarks>
		/// Create a grammatical structure from its string representation.
		/// Like buildCoNLLXGrammaticalStructure,
		/// this method fakes up the parts of the tree structure that are not
		/// used by the grammatical relation transformation operations.
		/// <i>Note:</i> Added by daniel cer
		/// </remarks>
		/// <param name="tokens"/>
		/// <param name="posTags"/>
		/// <param name="deps"/>
		public static GrammaticalStructure FromStringReps(IList<string> tokens, IList<string> posTags, IList<string> deps)
		{
			if (tokens.Count != posTags.Count)
			{
				throw new Exception(string.Format("tokens.size(): %d != pos.size(): %d%n", tokens.Count, posTags.Count));
			}
			IList<TreeGraphNode> tgWordNodes = new List<TreeGraphNode>(tokens.Count);
			IList<TreeGraphNode> tgPOSNodes = new List<TreeGraphNode>(tokens.Count);
			CoreLabel rootLabel = new CoreLabel();
			rootLabel.SetValue("ROOT");
			IList<IndexedWord> nodeWords = new List<IndexedWord>(tgPOSNodes.Count + 1);
			nodeWords.Add(new IndexedWord(rootLabel));
			UniversalSemanticHeadFinder headFinder = new UniversalSemanticHeadFinder();
			IEnumerator<string> posIter = posTags.GetEnumerator();
			foreach (string wordString in tokens)
			{
				string posString = posIter.Current;
				CoreLabel wordLabel = new CoreLabel();
				wordLabel.SetWord(wordString);
				wordLabel.SetValue(wordString);
				wordLabel.SetTag(posString);
				TreeGraphNode word = new TreeGraphNode(wordLabel);
				CoreLabel tagLabel = new CoreLabel();
				tagLabel.SetValue(posString);
				tagLabel.SetWord(posString);
				TreeGraphNode pos = new TreeGraphNode(tagLabel);
				tgWordNodes.Add(word);
				tgPOSNodes.Add(pos);
				TreeGraphNode[] childArr = new TreeGraphNode[] { word };
				pos.SetChildren(childArr);
				word.SetParent(pos);
				pos.PercolateHeads(headFinder);
				nodeWords.Add(new IndexedWord(wordLabel));
			}
			TreeGraphNode root = new TreeGraphNode(rootLabel);
			root.SetChildren(Sharpen.Collections.ToArray(tgPOSNodes, new TreeGraphNode[tgPOSNodes.Count]));
			root.SetIndex(0);
			// Build list of TypedDependencies
			IList<TypedDependency> tdeps = new List<TypedDependency>(deps.Count);
			foreach (string depString in deps)
			{
				int firstBracket = depString.IndexOf('(');
				if (firstBracket == -1)
				{
					ThrowDepFormatException(depString);
				}
				string type = Sharpen.Runtime.Substring(depString, 0, firstBracket);
				if (depString[depString.Length - 1] != ')')
				{
					ThrowDepFormatException(depString);
				}
				string args = Sharpen.Runtime.Substring(depString, firstBracket + 1, depString.Length - 1);
				int argSep = args.IndexOf(", ");
				if (argSep == -1)
				{
					ThrowDepFormatException(depString);
				}
				string parentArg = Sharpen.Runtime.Substring(args, 0, argSep);
				string childArg = Sharpen.Runtime.Substring(args, argSep + 2);
				int parentDash = parentArg.LastIndexOf('-');
				if (parentDash == -1)
				{
					ThrowDepFormatException(depString);
				}
				int childDash = childArg.LastIndexOf('-');
				if (childDash == -1)
				{
					ThrowDepFormatException(depString);
				}
				//System.err.printf("parentArg: %s%n", parentArg);
				int parentIdx = System.Convert.ToInt32(Sharpen.Runtime.Substring(parentArg, parentDash + 1).Replace("'", string.Empty));
				int childIdx = System.Convert.ToInt32(Sharpen.Runtime.Substring(childArg, childDash + 1).Replace("'", string.Empty));
				GrammaticalRelation grel = new GrammaticalRelation(Language.Any, type, null, GrammaticalRelation.Dependent);
				TypedDependency tdep = new TypedDependency(grel, nodeWords[parentIdx], nodeWords[childIdx]);
				tdeps.Add(tdep);
			}
			// TODO add some elegant way to construct language
			// appropriate GrammaticalStructures (e.g., English, Chinese, etc.)
			return new _GrammaticalStructure_410(tdeps, root);
		}

		private sealed class _GrammaticalStructure_410 : GrammaticalStructure
		{
			public _GrammaticalStructure_410(IList<TypedDependency> baseArg1, TreeGraphNode baseArg2)
				: base(baseArg1, baseArg2)
			{
				this.serialVersionUID = 1L;
			}

		}

		public GrammaticalStructure(IList<TypedDependency> projectiveDependencies, TreeGraphNode root)
		{
			this.root = root;
			IndexNodes(this.root);
			this.puncFilter = Filters.AcceptFilter();
			this.tagFilter = Filters.AcceptFilter();
			allTypedDependencies = typedDependencies = new List<TypedDependency>(projectiveDependencies);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Sharpen.Runtime.Substring(root.ToPrettyString(0), 1));
			sb.Append("Typed Dependencies:\n");
			sb.Append(typedDependencies);
			return sb.ToString();
		}

		private static void AttachStrandedNodes(TreeGraphNode t, TreeGraphNode root, bool attach, IPredicate<string> puncFilter, IPredicate<string> tagFilter, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> basicGraph)
		{
			if (t.IsLeaf())
			{
				return;
			}
			if (attach && puncFilter.Test(((CoreLabel)t.HeadWordNode().Label()).Value()) && tagFilter.Test(((CoreLabel)t.HeadWordNode().Label()).Tag()))
			{
				// make faster by first looking for links from parent
				// it is necessary to look for paths using all directions
				// because sometimes there are edges created from lower nodes to
				// nodes higher up
				TreeGraphNode parent = ((TreeGraphNode)t.Parent()).HighestNodeWithSameHead();
				if (!basicGraph.IsEdge(parent, t) && basicGraph.GetShortestPath(root, t, false) == null)
				{
					basicGraph.Add(parent, t, GrammaticalRelation.Dependent);
				}
			}
			foreach (TreeGraphNode kid in ((TreeGraphNode[])t.Children()))
			{
				AttachStrandedNodes(kid, root, (kid.HeadWordNode() != t.HeadWordNode()), puncFilter, tagFilter, basicGraph);
			}
		}

		// cdm dec 2009: I changed this to automatically fail on preterminal nodes, since they shouldn't match for GR parent patterns.  Should speed it up.
		private static void AnalyzeNode(TreeGraphNode t, TreeGraphNode root, ICollection<GrammaticalRelation> relations, IHeadFinder hf, IPredicate<string> puncFilter, IPredicate<string> tagFilter, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation
			> basicGraph, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> completeGraph)
		{
			if (t.IsPhrasal())
			{
				// don't do leaves or preterminals!
				TreeGraphNode tHigh = t.HighestNodeWithSameHead();
				foreach (GrammaticalRelation egr in relations)
				{
					if (egr.IsApplicable(t))
					{
						foreach (TreeGraphNode u in egr.GetRelatedNodes(t, root, hf))
						{
							TreeGraphNode uHigh = u.HighestNodeWithSameHead();
							if (uHigh == tHigh)
							{
								continue;
							}
							if (!puncFilter.Test(((CoreLabel)uHigh.HeadWordNode().Label()).Value()) || !tagFilter.Test(((CoreLabel)uHigh.HeadWordNode().Label()).Tag()))
							{
								continue;
							}
							completeGraph.Add(tHigh, uHigh, egr);
							// If there are two patterns that add dependencies, X --> Z and Y --> Z, and X dominates Y, then the dependency Y --> Z is not added to the basic graph to prevent unwanted duplication.
							// Similarly, if there is already a path from X --> Y, and an expression would trigger Y --> X somehow, we ignore that
							ICollection<TreeGraphNode> parents = basicGraph.GetParents(uHigh);
							if ((parents == null || parents.Count == 0 || parents.Contains(tHigh)) && basicGraph.GetShortestPath(uHigh, tHigh, true) == null)
							{
								// log.info("Adding " + egr.getShortName() + " from " + t + " to " + u + " tHigh=" + tHigh + "(" + tHigh.headWordNode() + ") uHigh=" + uHigh + "(" + uHigh.headWordNode() + ")");
								basicGraph.Add(tHigh, uHigh, egr);
							}
						}
					}
				}
				// now recurse into children
				foreach (TreeGraphNode kid in ((TreeGraphNode[])t.Children()))
				{
					AnalyzeNode(kid, root, relations, hf, puncFilter, tagFilter, basicGraph, completeGraph);
				}
			}
		}

		private void GetExtraDeps(IList<TypedDependency> deps, IPredicate<TypedDependency> puncTypedDepFilter, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> completeGraph)
		{
			GetExtras(deps);
			// adds stuff to basicDep based on the tregex patterns over the tree
			this.GetTreeDeps(deps, completeGraph, puncTypedDepFilter, ExtraTreeDepFilter());
			deps.Sort();
		}

		/// <summary>
		/// Helps the constructor build a list of typed dependencies using
		/// information from a
		/// <c>GrammaticalStructure</c>
		/// .
		/// </summary>
		private IList<TypedDependency> GetDeps(IPredicate<TypedDependency> puncTypedDepFilter, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> basicGraph)
		{
			IList<TypedDependency> basicDep = Generics.NewArrayList();
			foreach (TreeGraphNode gov in basicGraph.GetAllVertices())
			{
				foreach (TreeGraphNode dep in basicGraph.GetChildren(gov))
				{
					GrammaticalRelation reln = GetGrammaticalRelationCommonAncestor(((CoreLabel)gov.HeadWordNode().Label()), ((CoreLabel)gov.Label()), ((CoreLabel)dep.HeadWordNode().Label()), ((CoreLabel)dep.Label()), basicGraph.GetEdges(gov, dep));
					// log.info("  Gov: " + gov + " Dep: " + dep + " Reln: " + reln);
					basicDep.Add(new TypedDependency(reln, new IndexedWord(((CoreLabel)gov.HeadWordNode().Label())), new IndexedWord(((CoreLabel)dep.HeadWordNode().Label()))));
				}
			}
			// add the root
			TreeGraphNode dependencyRoot = new TreeGraphNode(new Word("ROOT"));
			dependencyRoot.SetIndex(0);
			TreeGraphNode rootDep = Root().HeadWordNode();
			if (rootDep == null)
			{
				IList<Tree> leaves = Edu.Stanford.Nlp.Trees.Trees.Leaves(Root());
				if (leaves.Count > 0)
				{
					Tree leaf = leaves[0];
					if (!(leaf is TreeGraphNode))
					{
						throw new AssertionError("Leaves should be TreeGraphNodes");
					}
					rootDep = (TreeGraphNode)leaf;
					if (rootDep.HeadWordNode() != null)
					{
						rootDep = rootDep.HeadWordNode();
					}
				}
			}
			if (rootDep != null)
			{
				TypedDependency rootTypedDep = new TypedDependency(GrammaticalRelation.Root, new IndexedWord(((CoreLabel)dependencyRoot.Label())), new IndexedWord(((CoreLabel)rootDep.Label())));
				if (puncTypedDepFilter.Test(rootTypedDep))
				{
					basicDep.Add(rootTypedDep);
				}
				else
				{
					// Root is a punctuation character
					/* Heuristic to find a root for the graph.
					* Make the first child of the current root the
					* new root and attach all other children to
					* the new root.
					*/
					IndexedWord root = rootTypedDep.Dep();
					IndexedWord newRoot = null;
					basicDep.Sort();
					foreach (TypedDependency td in basicDep)
					{
						if (td.Gov().Equals(root))
						{
							if (newRoot != null)
							{
								td.SetGov(newRoot);
							}
							else
							{
								td.SetGov(td.Gov());
								td.SetReln(GrammaticalRelation.Root);
								newRoot = td.Dep();
							}
						}
					}
				}
			}
			PostProcessDependencies(basicDep);
			basicDep.Sort();
			return basicDep;
		}

		/// <summary>
		/// Returns a Filter which checks dependencies for usefulness as
		/// extra tree-based dependencies.
		/// </summary>
		/// <remarks>
		/// Returns a Filter which checks dependencies for usefulness as
		/// extra tree-based dependencies.  By default, everything is
		/// accepted.  One example of how this can be useful is in the
		/// English dependencies, where the REL dependency is used as an
		/// intermediate and we do not want this to be added when we make a
		/// second pass over the trees for missing dependencies.
		/// </remarks>
		protected internal virtual IPredicate<TypedDependency> ExtraTreeDepFilter()
		{
			return Filters.AcceptFilter();
		}

		/// <summary>
		/// Post process the dependencies in whatever way this language
		/// requires.
		/// </summary>
		/// <remarks>
		/// Post process the dependencies in whatever way this language
		/// requires.  For example, English might replace "rel" dependencies
		/// with either dobj or pobj depending on the surrounding
		/// dependencies.
		/// </remarks>
		protected internal virtual void PostProcessDependencies(IList<TypedDependency> basicDep)
		{
		}

		// no post processing by default
		/// <summary>
		/// Get extra dependencies that do not depend on the tree structure,
		/// but rather only depend on the existing dependency structure.
		/// </summary>
		/// <remarks>
		/// Get extra dependencies that do not depend on the tree structure,
		/// but rather only depend on the existing dependency structure.
		/// For example, the English xsubj dependency can be extracted that way.
		/// </remarks>
		protected internal virtual void GetExtras(IList<TypedDependency> basicDep)
		{
		}

		// no extra dependencies by default
		/// <summary>
		/// Look through the tree t and adds to the List basicDep
		/// additional dependencies which aren't
		/// in the List but which satisfy the filter puncTypedDepFilter.
		/// </summary>
		/// <param name="deps">The list of dependencies which may be augmented</param>
		/// <param name="completeGraph">a graph of all the tree dependencies found earlier</param>
		/// <param name="puncTypedDepFilter">The filter that may skip punctuation dependencies</param>
		/// <param name="extraTreeDepFilter">Additional dependencies are added only if they pass this filter</param>
		protected internal virtual void GetTreeDeps(IList<TypedDependency> deps, DirectedMultiGraph<TreeGraphNode, GrammaticalRelation> completeGraph, IPredicate<TypedDependency> puncTypedDepFilter, IPredicate<TypedDependency> extraTreeDepFilter)
		{
			foreach (TreeGraphNode gov in completeGraph.GetAllVertices())
			{
				foreach (TreeGraphNode dep in completeGraph.GetChildren(gov))
				{
					foreach (GrammaticalRelation rel in RemoveGrammaticalRelationAncestors(completeGraph.GetEdges(gov, dep)))
					{
						TypedDependency newDep = new TypedDependency(rel, new IndexedWord(((CoreLabel)gov.HeadWordNode().Label())), new IndexedWord(((CoreLabel)dep.HeadWordNode().Label())));
						if (!deps.Contains(newDep) && puncTypedDepFilter.Test(newDep) && extraTreeDepFilter.Test(newDep))
						{
							newDep.SetExtra();
							deps.Add(newDep);
						}
					}
				}
			}
		}

		[System.Serializable]
		private class NoPunctFilter : IPredicate<IDependency<ILabel, ILabel, object>>
		{
			private IPredicate<string> npf;

			internal NoPunctFilter(IPredicate<string> f)
			{
				this.npf = f;
			}

			public virtual bool Test(IDependency<ILabel, ILabel, object> d)
			{
				if (d == null)
				{
					return false;
				}
				ILabel lab = d.Dependent();
				if (lab == null)
				{
					return false;
				}
				return npf.Test(lab.Value());
			}

			private const long serialVersionUID = -2319891944796663180L;
			// Automatically generated by Eclipse
		}

		[System.Serializable]
		private class NoPunctTypedDependencyFilter : IPredicate<TypedDependency>
		{
			private IPredicate<string> npf;

			private IPredicate<string> tf;

			internal NoPunctTypedDependencyFilter(IPredicate<string> f, IPredicate<string> tf)
			{
				// end static class NoPunctFilter
				this.npf = f;
				this.tf = tf;
			}

			public virtual bool Test(TypedDependency d)
			{
				if (d == null)
				{
					return false;
				}
				IndexedWord l = d.Dep();
				if (l == null)
				{
					return false;
				}
				return npf.Test(l.Value()) && tf.Test(l.Tag());
			}

			private const long serialVersionUID = -2872766864289207468L;
		}

		// end static class NoPunctTypedDependencyFilter
		/// <summary>
		/// Get GrammaticalRelation between gov and dep, and null if gov  is not the
		/// governor of dep.
		/// </summary>
		public virtual GrammaticalRelation GetGrammaticalRelation(int govIndex, int depIndex)
		{
			TreeGraphNode gov = GetNodeByIndex(govIndex);
			TreeGraphNode dep = GetNodeByIndex(depIndex);
			// TODO: this is pretty ugly
			return GetGrammaticalRelation(new IndexedWord(((CoreLabel)gov.Label())), new IndexedWord(((CoreLabel)dep.Label())));
		}

		/// <summary>
		/// Get GrammaticalRelation between gov and dep, and null if gov is not the
		/// governor of dep.
		/// </summary>
		public virtual GrammaticalRelation GetGrammaticalRelation(IndexedWord gov, IndexedWord dep)
		{
			IList<GrammaticalRelation> labels = Generics.NewArrayList();
			foreach (TypedDependency dependency in TypedDependencies(GrammaticalStructure.Extras.Maximal))
			{
				if (dependency.Gov().Equals(gov) && dependency.Dep().Equals(dep))
				{
					labels.Add(dependency.Reln());
				}
			}
			return GetGrammaticalRelationCommonAncestor(gov, gov, dep, dep, labels);
		}

		/// <summary>
		/// Returns the GrammaticalRelation which is the highest common
		/// ancestor of the list of relations passed in.
		/// </summary>
		/// <remarks>
		/// Returns the GrammaticalRelation which is the highest common
		/// ancestor of the list of relations passed in.  The Labels are
		/// passed in only for debugging reasons.  gov &amp; dep are the
		/// labels with the text, govH and depH can be higher labels in the
		/// tree which represent the category
		/// </remarks>
		private static GrammaticalRelation GetGrammaticalRelationCommonAncestor(IAbstractCoreLabel gov, IAbstractCoreLabel govH, IAbstractCoreLabel dep, IAbstractCoreLabel depH, IList<GrammaticalRelation> labels)
		{
			GrammaticalRelation reln = GrammaticalRelation.Dependent;
			IList<GrammaticalRelation> sortedLabels;
			if (labels.Count <= 1)
			{
				sortedLabels = labels;
			}
			else
			{
				sortedLabels = new List<GrammaticalRelation>(labels);
				sortedLabels.Sort(new GrammaticalStructure.NameComparator<GrammaticalRelation>());
			}
			// log.info(" gov " + govH + " dep " + depH + " arc labels: " + sortedLabels);
			foreach (GrammaticalRelation reln2 in sortedLabels)
			{
				if (reln.IsAncestor(reln2))
				{
					reln = reln2;
				}
				else
				{
					if (PrintDebugging && !reln2.IsAncestor(reln))
					{
						log.Info("@@@\t" + reln + "\t" + reln2 + "\t" + govH.Get(typeof(CoreAnnotations.ValueAnnotation)) + "\t" + depH.Get(typeof(CoreAnnotations.ValueAnnotation)));
					}
				}
			}
			if (PrintDebugging && reln.Equals(GrammaticalRelation.Dependent))
			{
				string topCat = govH.Get(typeof(CoreAnnotations.ValueAnnotation));
				string topTag = gov.Tag();
				string topWord = gov.Value();
				string botCat = depH.Get(typeof(CoreAnnotations.ValueAnnotation));
				string botTag = dep.Tag();
				string botWord = dep.Value();
				log.Info("### dep\t" + topCat + "\t" + topTag + "\t" + topWord + "\t" + botCat + "\t" + botTag + "\t" + botWord + "\t");
			}
			return reln;
		}

		private static IList<GrammaticalRelation> RemoveGrammaticalRelationAncestors(IList<GrammaticalRelation> original)
		{
			IList<GrammaticalRelation> filtered = Generics.NewArrayList();
			foreach (GrammaticalRelation reln in original)
			{
				bool descendantFound = false;
				for (int index = 0; index < filtered.Count; ++index)
				{
					GrammaticalRelation gr = filtered[index];
					//if the element in the list is an ancestor of the current
					//relation, remove it (we will replace it later)
					if (gr.IsAncestor(reln))
					{
						filtered.Remove(index);
						--index;
					}
					else
					{
						if (reln.IsAncestor(gr))
						{
							//if the relation is not an ancestor of an element in the
							//list, we add the relation
							descendantFound = true;
						}
					}
				}
				if (!descendantFound)
				{
					filtered.Add(reln);
				}
			}
			return filtered;
		}

		/// <summary>Returns the typed dependencies of this grammatical structure.</summary>
		/// <remarks>
		/// Returns the typed dependencies of this grammatical structure.  These
		/// are the basic word-level typed dependencies, where each word is dependent
		/// on one other thing, either a word or the starting ROOT, and the
		/// dependencies have a tree structure.  This corresponds to the
		/// command-line option "basicDependencies".
		/// </remarks>
		/// <returns>The typed dependencies of this grammatical structure</returns>
		public virtual ICollection<TypedDependency> TypedDependencies()
		{
			return TypedDependencies(GrammaticalStructure.Extras.None);
		}

		/// <summary>Returns all the typed dependencies of this grammatical structure.</summary>
		/// <remarks>
		/// Returns all the typed dependencies of this grammatical structure.
		/// These are like the basic (uncollapsed) dependencies, but may include
		/// extra arcs for control relationships, etc. This corresponds to the
		/// "nonCollapsed" option.
		/// </remarks>
		public virtual ICollection<TypedDependency> AllTypedDependencies()
		{
			return TypedDependencies(GrammaticalStructure.Extras.Maximal);
		}

		/// <summary>Returns the typed dependencies of this grammatical structure.</summary>
		/// <remarks>
		/// Returns the typed dependencies of this grammatical structure. These
		/// are non-collapsed dependencies (basic or nonCollapsed).
		/// </remarks>
		/// <param name="includeExtras">
		/// If true, the list of typed dependencies
		/// returned may include "extras", and does not follow a tree structure.
		/// </param>
		/// <returns>The typed dependencies of this grammatical structure</returns>
		public virtual IList<TypedDependency> TypedDependencies(GrammaticalStructure.Extras includeExtras)
		{
			// This copy has to be done because of the broken way
			// TypedDependency objects can be mutated by downstream methods
			// such as collapseDependencies.  Without the copy here it is
			// possible for two consecutive calls to
			// typedDependenciesCollapsed to get different results.  For
			// example, the English dependencies rename existing objects KILL
			// to note that they should be removed.
			IList<TypedDependency> source;
			if (includeExtras != GrammaticalStructure.Extras.None)
			{
				source = allTypedDependencies;
			}
			else
			{
				source = typedDependencies;
			}
			IList<TypedDependency> deps = new List<TypedDependency>(source);
			//TODO (sebschu): prevent correctDependencies from getting called multiple times
			CorrectDependencies(deps);
			return deps;
		}

		/// <seealso cref="TypedDependencies(Extras)"/>
		[Obsolete]
		public virtual IList<TypedDependency> TypedDependencies(bool includeExtras)
		{
			return TypedDependencies(includeExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None);
		}

		/// <summary>Get the typed dependencies after collapsing them.</summary>
		/// <remarks>
		/// Get the typed dependencies after collapsing them.
		/// Collapsing dependencies refers to turning certain function words
		/// such as prepositions and conjunctions into arcs, so they disappear from
		/// the set of nodes.
		/// There is no guarantee that the dependencies are a tree. While the
		/// dependencies are normally tree-like, the collapsing may introduce
		/// not only re-entrancies but even small cycles.
		/// </remarks>
		/// <returns>A set of collapsed dependencies</returns>
		public virtual ICollection<TypedDependency> TypedDependenciesCollapsed()
		{
			return TypedDependenciesCollapsed(GrammaticalStructure.Extras.None);
		}

		// todo [cdm 2012]: The semantics of this method is the opposite of the others.
		// The other no argument methods correspond to includeExtras being
		// true, but for this one it is false.  This should probably be made uniform.
		/// <summary>
		/// Get the typed dependencies after mostly collapsing them, but keep a tree
		/// structure.
		/// </summary>
		/// <remarks>
		/// Get the typed dependencies after mostly collapsing them, but keep a tree
		/// structure.  In order to do this, the code does:
		/// <ol>
		/// <li> no relative clause processing
		/// <li> no xsubj relations
		/// <li> no propagation of conjuncts
		/// </ol>
		/// This corresponds to the "tree" option.
		/// </remarks>
		/// <returns>collapsed dependencies keeping a tree structure</returns>
		public virtual ICollection<TypedDependency> TypedDependenciesCollapsedTree()
		{
			IList<TypedDependency> tdl = TypedDependencies(GrammaticalStructure.Extras.None);
			CollapseDependenciesTree(tdl);
			return tdl;
		}

		/// <summary>Get the typed dependencies after collapsing them.</summary>
		/// <remarks>
		/// Get the typed dependencies after collapsing them.
		/// The "collapsed" option corresponds to calling this method with argument
		/// <see langword="true"/>
		/// .
		/// </remarks>
		/// <param name="includeExtras">
		/// If true, the list of typed dependencies
		/// returned may include "extras", like controlling subjects
		/// </param>
		/// <returns>collapsed dependencies</returns>
		public virtual IList<TypedDependency> TypedDependenciesCollapsed(GrammaticalStructure.Extras includeExtras)
		{
			IList<TypedDependency> tdl = TypedDependencies(includeExtras);
			CollapseDependencies(tdl, false, includeExtras);
			return tdl;
		}

		/// <seealso cref="TypedDependenciesCollapsed(Extras)"/>
		[Obsolete]
		public virtual IList<TypedDependency> TypedDependenciesCollapsed(bool includeExtras)
		{
			return TypedDependenciesCollapsed(includeExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None);
		}

		/// <summary>
		/// Get the typed dependencies after collapsing them and processing eventual
		/// CC complements.
		/// </summary>
		/// <remarks>
		/// Get the typed dependencies after collapsing them and processing eventual
		/// CC complements.  The effect of this part is to distributed conjoined
		/// arguments across relations or conjoined predicates across their arguments.
		/// This is generally useful, and we generally recommend using the output of
		/// this method with the second argument being
		/// <see langword="true"/>
		/// .
		/// The "CCPropagated" option corresponds to calling this method with an
		/// argument of
		/// <see langword="true"/>
		/// .
		/// </remarks>
		/// <param name="includeExtras">
		/// If true, the list of typed dependencies
		/// returned may include "extras", such as controlled subject links.
		/// </param>
		/// <returns>collapsed dependencies with CC processed</returns>
		public virtual IList<TypedDependency> TypedDependenciesCCprocessed(GrammaticalStructure.Extras includeExtras)
		{
			IList<TypedDependency> tdl = TypedDependencies(includeExtras);
			CollapseDependencies(tdl, true, includeExtras);
			return tdl;
		}

		/// <seealso cref="TypedDependenciesCCprocessed(Extras)"/>
		[Obsolete]
		public virtual IList<TypedDependency> TypedDependenciesCCprocessed(bool includeExtras)
		{
			return TypedDependenciesCCprocessed(includeExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None);
		}

		public virtual IList<TypedDependency> TypedDependenciesEnhanced()
		{
			IList<TypedDependency> tdl = TypedDependencies(GrammaticalStructure.Extras.Maximal);
			AddEnhancements(tdl, UniversalEnglishGrammaticalStructure.EnhancedOptions);
			return tdl;
		}

		public virtual IList<TypedDependency> TypedDependenciesEnhancedPlusPlus()
		{
			IList<TypedDependency> tdl = TypedDependencies(GrammaticalStructure.Extras.Maximal);
			AddEnhancements(tdl, UniversalEnglishGrammaticalStructure.EnhancedPlusPlusOptions);
			return tdl;
		}

		/// <summary>
		/// Get a list of the typed dependencies, including extras like control
		/// dependencies, collapsing them and distributing relations across
		/// coordination.
		/// </summary>
		/// <remarks>
		/// Get a list of the typed dependencies, including extras like control
		/// dependencies, collapsing them and distributing relations across
		/// coordination.  This method is generally recommended for best
		/// representing the semantic and syntactic relations of a sentence. In
		/// general it returns a directed graph (i.e., the output may not be a tree
		/// and it may contain (small) cycles).
		/// The "CCPropagated" option corresponds to calling this method.
		/// </remarks>
		/// <returns>collapsed dependencies with CC processed</returns>
		public virtual IList<TypedDependency> TypedDependenciesCCprocessed()
		{
			return TypedDependenciesCCprocessed(GrammaticalStructure.Extras.Maximal);
		}

		/// <summary>
		/// Destructively modify the
		/// <c>Collection&lt;TypedDependency&gt;</c>
		/// to collapse
		/// language-dependent transitive dependencies.
		/// <p/>
		/// Default is no-op; to be over-ridden in subclasses.
		/// </summary>
		/// <param name="list">A list of dependencies to process for possible collapsing</param>
		/// <param name="CCprocess">apply CC process?</param>
		protected internal virtual void CollapseDependencies(IList<TypedDependency> list, bool CCprocess, GrammaticalStructure.Extras includeExtras)
		{
		}

		// do nothing as default operation
		/// <summary>Destructively applies different enhancements to the dependency graph.</summary>
		/// <remarks>
		/// Destructively applies different enhancements to the dependency graph.
		/// <p/>
		/// Default is no-op; to be over-ridden in subclasses.
		/// </remarks>
		/// <param name="list">A list of dependencies</param>
		/// <param name="options">Options that determine which enhancements are applied to the dependency graph.</param>
		protected internal virtual void AddEnhancements(IList<TypedDependency> list, EnhancementOptions options)
		{
		}

		// do nothing as default operation
		/// <summary>
		/// Destructively modify the
		/// <c>Collection&lt;TypedDependency&gt;</c>
		/// to collapse
		/// language-dependent transitive dependencies but keeping a tree structure.
		/// <p/>
		/// Default is no-op; to be over-ridden in subclasses.
		/// </summary>
		/// <param name="list">A list of dependencies to process for possible collapsing</param>
		protected internal virtual void CollapseDependenciesTree(IList<TypedDependency> list)
		{
		}

		// do nothing as default operation
		/// <summary>
		/// Destructively modify the
		/// <c>TypedDependencyGraph</c>
		/// to correct
		/// language-dependent dependencies. (e.g., nsubjpass in a relative clause)
		/// <p/>
		/// Default is no-op; to be over-ridden in subclasses.
		/// </summary>
		protected internal virtual void CorrectDependencies(IList<TypedDependency> list)
		{
		}

		// do nothing as default operation
		/// <summary>Checks if all the typeDependencies are connected</summary>
		/// <param name="list">a list of typedDependencies</param>
		/// <returns>true if the list represents a connected graph, false otherwise</returns>
		public static bool IsConnected(ICollection<TypedDependency> list)
		{
			return GetRoots(list).Count <= 1;
		}

		// there should be no more than one root to have a connected graph
		// there might be no root in the way we look when you have a relative clause
		// ex.: Apple is a society that sells computers
		// (the root "society" will also be the nsubj of "sells")
		/// <summary>Return a list of TypedDependencies which are not dependent on any node from the list.</summary>
		/// <param name="list">The list of TypedDependencies to check</param>
		/// <returns>A list of TypedDependencies which are not dependent on any node from the list</returns>
		public static ICollection<TypedDependency> GetRoots(ICollection<TypedDependency> list)
		{
			ICollection<TypedDependency> roots = new List<TypedDependency>();
			// need to see if more than one governor is not listed somewhere as a dependent
			// first take all the deps
			ICollection<IndexedWord> deps = Generics.NewHashSet();
			foreach (TypedDependency typedDep in list)
			{
				deps.Add(typedDep.Dep());
			}
			// go through the list and add typedDependency for which the gov is not a dep
			ICollection<IndexedWord> govs = Generics.NewHashSet();
			foreach (TypedDependency typedDep_1 in list)
			{
				IndexedWord gov = typedDep_1.Gov();
				if (!deps.Contains(gov) && !govs.Contains(gov))
				{
					roots.Add(typedDep_1);
				}
				govs.Add(gov);
			}
			return roots;
		}

		private const long serialVersionUID = 2286294455343892678L;

		private class NameComparator<X> : IComparator<X>
		{
			public virtual int Compare(X o1, X o2)
			{
				string n1 = o1.ToString();
				string n2 = o2.ToString();
				return string.CompareOrdinal(n1, n2);
			}
		}

		public const int CoNLLX_WordField = 1;

		public const int CoNLLX_POSField = 4;

		public const int CoNLLX_GovField = 6;

		public const int CoNLLX_RelnField = 7;

		public const int CoNLLX_FieldCount = 10;

		// Note that these field constants are 0-based whereas much documentation is 1-based
		/// <summary>
		/// Read in a file containing a CoNLL-X dependency treebank and return a
		/// corresponding list of GrammaticalStructures.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static IList<GrammaticalStructure> ReadCoNLLXGrammaticalStructureCollection(string fileName, IDictionary<string, GrammaticalRelation> shortNameToGRel, IGrammaticalStructureFromDependenciesFactory factory)
		{
			using (BufferedReader r = IOUtils.ReaderFromString(fileName))
			{
				LineNumberReader reader = new LineNumberReader(r);
				IList<GrammaticalStructure> gsList = new LinkedList<GrammaticalStructure>();
				IList<IList<string>> tokenFields = new List<IList<string>>();
				for (string inline = reader.ReadLine(); inline != null; inline = reader.ReadLine())
				{
					if (!inline.IsEmpty())
					{
						// read in a single sentence token by token
						IList<string> fields = Arrays.AsList(inline.Split("\t"));
						if (fields.Count != CoNLLX_FieldCount)
						{
							throw new Exception(string.Format("Error (line %d): 10 fields expected but %d are present", reader.GetLineNumber(), fields.Count));
						}
						tokenFields.Add(fields);
					}
					else
					{
						if (tokenFields.IsEmpty())
						{
							continue;
						}
						// skip excess empty lines
						gsList.Add(BuildCoNLLXGrammaticalStructure(tokenFields, shortNameToGRel, factory));
						tokenFields = new List<IList<string>>();
					}
				}
				return gsList;
			}
		}

		public static GrammaticalStructure BuildCoNLLXGrammaticalStructure(IList<IList<string>> tokenFields, IDictionary<string, GrammaticalRelation> shortNameToGRel, IGrammaticalStructureFromDependenciesFactory factory)
		{
			IList<IndexedWord> tgWords = new List<IndexedWord>(tokenFields.Count);
			IList<TreeGraphNode> tgPOSNodes = new List<TreeGraphNode>(tokenFields.Count);
			SemanticHeadFinder headFinder = new SemanticHeadFinder();
			// Construct TreeGraphNodes for words and POS tags
			foreach (IList<string> fields in tokenFields)
			{
				CoreLabel word = new CoreLabel();
				word.SetValue(fields[CoNLLX_WordField]);
				word.SetWord(fields[CoNLLX_WordField]);
				word.SetTag(fields[CoNLLX_POSField]);
				word.SetIndex(tgWords.Count + 1);
				CoreLabel pos = new CoreLabel();
				pos.SetTag(fields[CoNLLX_POSField]);
				pos.SetValue(fields[CoNLLX_POSField]);
				TreeGraphNode wordNode = new TreeGraphNode(word);
				TreeGraphNode posNode = new TreeGraphNode(pos);
				tgWords.Add(new IndexedWord(word));
				tgPOSNodes.Add(posNode);
				TreeGraphNode[] childArr = new TreeGraphNode[] { wordNode };
				posNode.SetChildren(childArr);
				wordNode.SetParent(posNode);
				posNode.PercolateHeads(headFinder);
			}
			// We fake up the parts of the tree structure that are not
			// actually used by the grammatical relation transformation
			// operations.
			//
			// That is, the constructed TreeGraphs consist of a flat tree,
			// without any phrase bracketing, but that does preserve the
			// parent child relationship between words and their POS tags.
			//
			// e.g. (ROOT (PRP I) (VBD hit) (DT the) (NN ball) (. .))
			TreeGraphNode root = new TreeGraphNode(new Word("ROOT-" + (tgPOSNodes.Count + 1)));
			root.SetChildren(Sharpen.Collections.ToArray(tgPOSNodes, new TreeGraphNode[tgPOSNodes.Count]));
			// Build list of TypedDependencies
			IList<TypedDependency> tdeps = new List<TypedDependency>(tgWords.Count);
			// Create a node outside the tree useful for root dependencies;
			// we want to keep those if they were stored in the conll file
			CoreLabel rootLabel = new CoreLabel();
			rootLabel.SetValue("ROOT");
			rootLabel.SetWord("ROOT");
			rootLabel.SetIndex(0);
			IndexedWord dependencyRoot = new IndexedWord(rootLabel);
			for (int i = 0; i < tgWords.Count; i++)
			{
				string parentIdStr = tokenFields[i][CoNLLX_GovField];
				if (StringUtils.IsNullOrEmpty(parentIdStr))
				{
					continue;
				}
				string grelString = tokenFields[i][CoNLLX_RelnField];
				if (grelString.Equals("null") || grelString.Equals("erased"))
				{
					continue;
				}
				GrammaticalRelation grel = shortNameToGRel[grelString.ToLower()];
				TypedDependency tdep;
				if (grel == null)
				{
					if (grelString.ToLower().Equals("root"))
					{
						tdep = new TypedDependency(GrammaticalRelation.Root, dependencyRoot, tgWords[i]);
					}
					else
					{
						throw new Exception("Unknown grammatical relation '" + grelString + "' fields: " + tokenFields[i] + "\nNode: " + tgWords[i] + '\n' + "Known Grammatical relations: [" + shortNameToGRel.Keys + ']');
					}
				}
				else
				{
					int parentId = System.Convert.ToInt32(parentIdStr) - 1;
					if (parentId >= tgWords.Count)
					{
						System.Console.Error.Printf("Warning: Invalid Parent Id %d Sentence Length: %d%n", parentId + 1, tgWords.Count);
						System.Console.Error.Printf("         Assigning to root (0)%n");
						parentId = -1;
					}
					tdep = new TypedDependency(grel, (parentId == -1 ? dependencyRoot : tgWords[parentId]), tgWords[i]);
				}
				tdeps.Add(tdep);
			}
			return factory.Build(tdeps, root);
		}

		public static void Main(string[] args)
		{
			/* Language-specific default properties. The default
			* options produce English Universal dependencies.
			* This should be overwritten in every subclass.
			*
			*/
			GrammaticalStructureConversionUtils.ConvertTrees(args, "en");
		}
	}
}

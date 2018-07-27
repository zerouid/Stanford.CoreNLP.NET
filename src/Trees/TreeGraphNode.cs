using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A
	/// <c>TreeGraphNode</c>
	/// is simply a
	/// <see cref="Tree"/>
	/// 
	/// <c>Tree</c>
	/// }
	/// with some additional functionality.  For example, the
	/// <c>parent()</c>
	/// method works without searching from the root.
	/// Labels are always assumed to be
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
	/// 
	/// <c>CoreLabel</c>
	/// }.
	/// This class makes the horrible mistake of changing the semantics of
	/// equals and hashCode to go back to "==" and System.identityHashCode,
	/// despite the semantics of the superclass's equality.
	/// </summary>
	/// <author>Bill MacCartney</author>
	[System.Serializable]
	public class TreeGraphNode : Tree, IHasParent
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.TreeGraphNode));

		/// <summary>Label for this node.</summary>
		private CoreLabel label;

		/// <summary>Parent of this node.</summary>
		protected internal Edu.Stanford.Nlp.Trees.TreeGraphNode parent;

		/// <summary>Children of this node.</summary>
		protected internal Edu.Stanford.Nlp.Trees.TreeGraphNode[] children = ZeroTgnChildren;

		/// <summary>For internal nodes, the head word of this subtree.</summary>
		private Edu.Stanford.Nlp.Trees.TreeGraphNode headWordNode;

		/// <summary>
		/// A leaf node should have a zero-length array for its
		/// children.
		/// </summary>
		/// <remarks>
		/// A leaf node should have a zero-length array for its
		/// children. For efficiency, subclasses can use this array as a
		/// return value for children() for leaf nodes if desired. Should
		/// this be public instead?
		/// </remarks>
		protected internal static readonly Edu.Stanford.Nlp.Trees.TreeGraphNode[] ZeroTgnChildren = new Edu.Stanford.Nlp.Trees.TreeGraphNode[0];

		private static readonly ILabelFactory mlf = CoreLabel.Factory();

		/// <summary>
		/// Create a new
		/// <c>TreeGraphNode</c>
		/// with the supplied
		/// label.
		/// </summary>
		/// <param name="label">the label for this node.</param>
		public TreeGraphNode(ILabel label)
		{
			// = null;
			this.label = (CoreLabel)mlf.NewLabel(label);
		}

		/// <summary>
		/// Create a new
		/// <c>TreeGraphNode</c>
		/// with the supplied
		/// label and list of child nodes.
		/// </summary>
		/// <param name="label">the label for this node.</param>
		/// <param name="children">
		/// the list of child
		/// <c>TreeGraphNode</c>
		/// s
		/// for this node.
		/// </param>
		public TreeGraphNode(ILabel label, IList<Tree> children)
			: this(label)
		{
			SetChildren(children);
		}

		/// <summary>
		/// Create a new
		/// <c>TreeGraphNode</c>
		/// having the same tree
		/// structure and label values as an existing tree (but no shared
		/// storage).  Operates recursively to construct an entire
		/// subtree.
		/// </summary>
		/// <param name="t">the tree to copy</param>
		/// <param name="parent">the parent node</param>
		protected internal TreeGraphNode(Tree t, Edu.Stanford.Nlp.Trees.TreeGraphNode parent)
		{
			this.parent = parent;
			Tree[] tKids = t.Children();
			int numKids = tKids.Length;
			children = new Edu.Stanford.Nlp.Trees.TreeGraphNode[numKids];
			for (int i = 0; i < numKids; i++)
			{
				children[i] = new Edu.Stanford.Nlp.Trees.TreeGraphNode(tKids[i], this);
				if (t.IsPreTerminal())
				{
					// add the tags to the leaves
					children[i].label.SetTag(t.Label().Value());
				}
			}
			this.label = (CoreLabel)mlf.NewLabel(t.Label());
		}

		/// <summary>
		/// Implements equality for
		/// <c>TreeGraphNode</c>
		/// s.  Unlike
		/// <c>Tree</c>
		/// s,
		/// <c>TreeGraphNode</c>
		/// s should be
		/// considered equal only if they are ==.  <i>Implementation note:</i>
		/// TODO: This should be changed via introducing a Tree interface with the current Tree and this class implementing it, since what is done here breaks the equals() contract.
		/// </summary>
		/// <param name="o">The object to compare with</param>
		/// <returns>Whether two things are equal</returns>
		public override bool Equals(object o)
		{
			return o == this;
		}

		public override int GetHashCode()
		{
			return Runtime.IdentityHashCode(this);
		}

		/// <summary>
		/// Returns the label associated with the current node, or null
		/// if there is no label.
		/// </summary>
		/// <returns>the label of the node</returns>
		public override ILabel Label()
		{
			return label;
		}

		public override void SetLabel(ILabel label)
		{
			if (label is CoreLabel)
			{
				this.SetLabel((CoreLabel)label);
			}
			else
			{
				this.SetLabel((CoreLabel)mlf.NewLabel(label));
			}
		}

		/// <summary>Sets the label associated with the current node.</summary>
		/// <param name="label">the new label to use.</param>
		public virtual void SetLabel(CoreLabel label)
		{
			this.label = label;
		}

		/// <summary>Get the index for the current node.</summary>
		public virtual int Index()
		{
			return label.Index();
		}

		/// <summary>Set the index for the current node.</summary>
		protected internal virtual void SetIndex(int index)
		{
			label.SetIndex(index);
		}

		/// <summary>Get the parent for the current node.</summary>
		public override Tree Parent()
		{
			return parent;
		}

		/// <summary>Set the parent for the current node.</summary>
		public virtual void SetParent(Edu.Stanford.Nlp.Trees.TreeGraphNode parent)
		{
			this.parent = parent;
		}

		/// <summary>Returns an array of the children of this node.</summary>
		public override Tree[] Children()
		{
			return children;
		}

		/// <summary>
		/// Sets the children of this
		/// <c>TreeGraphNode</c>
		/// .  If
		/// given
		/// <see langword="null"/>
		/// , this method sets
		/// the node's children to the canonical zero-length Tree[] array.
		/// </summary>
		/// <param name="children">an array of child trees</param>
		public override void SetChildren(Tree[] children)
		{
			if (children == null || children.Length == 0)
			{
				this.children = ZeroTgnChildren;
			}
			else
			{
				if (children is Edu.Stanford.Nlp.Trees.TreeGraphNode[])
				{
					this.children = (Edu.Stanford.Nlp.Trees.TreeGraphNode[])children;
					foreach (Edu.Stanford.Nlp.Trees.TreeGraphNode child in this.children)
					{
						child.SetParent(this);
					}
				}
				else
				{
					this.children = new Edu.Stanford.Nlp.Trees.TreeGraphNode[children.Length];
					for (int i = 0; i < children.Length; i++)
					{
						this.children[i] = (Edu.Stanford.Nlp.Trees.TreeGraphNode)children[i];
						this.children[i].SetParent(this);
					}
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override void SetChildren<_T0>(IList<_T0> childTreesList)
		{
			if (childTreesList == null || childTreesList.IsEmpty())
			{
				SetChildren(ZeroTgnChildren);
			}
			else
			{
				int leng = childTreesList.Count;
				Edu.Stanford.Nlp.Trees.TreeGraphNode[] childTrees = new Edu.Stanford.Nlp.Trees.TreeGraphNode[leng];
				Sharpen.Collections.ToArray(childTreesList, childTrees);
				SetChildren(childTrees);
			}
		}

		public override Tree SetChild(int i, Tree t)
		{
			if (!(t is Edu.Stanford.Nlp.Trees.TreeGraphNode))
			{
				throw new ArgumentException("Horrible error");
			}
			((Edu.Stanford.Nlp.Trees.TreeGraphNode)t).SetParent(this);
			return base.SetChild(i, t);
		}

		/// <summary>Adds a child in the ith location.</summary>
		/// <remarks>
		/// Adds a child in the ith location.  Does so without overwriting
		/// the parent pointers of the rest of the children, which might be
		/// relevant in case there are add and remove operations mixed
		/// together.
		/// </remarks>
		public override void AddChild(int i, Tree t)
		{
			if (!(t is Edu.Stanford.Nlp.Trees.TreeGraphNode))
			{
				throw new ArgumentException("Horrible error");
			}
			((Edu.Stanford.Nlp.Trees.TreeGraphNode)t).SetParent(this);
			Edu.Stanford.Nlp.Trees.TreeGraphNode[] kids = this.children;
			Edu.Stanford.Nlp.Trees.TreeGraphNode[] newKids = new Edu.Stanford.Nlp.Trees.TreeGraphNode[kids.Length + 1];
			if (i != 0)
			{
				System.Array.Copy(kids, 0, newKids, 0, i);
			}
			newKids[i] = (Edu.Stanford.Nlp.Trees.TreeGraphNode)t;
			if (i != kids.Length)
			{
				System.Array.Copy(kids, i, newKids, i + 1, kids.Length - i);
			}
			this.children = newKids;
		}

		/// <summary>Removes the ith child from the TreeGraphNode.</summary>
		/// <remarks>
		/// Removes the ith child from the TreeGraphNode.  Needs to override
		/// the parent removeChild so it can avoid setting the parent
		/// pointers on the remaining children.  This is useful if you want
		/// to add and remove children from one node to another node; this way,
		/// it won't matter what order you do the add and remove operations.
		/// </remarks>
		public override Tree RemoveChild(int i)
		{
			Edu.Stanford.Nlp.Trees.TreeGraphNode[] kids = ((Edu.Stanford.Nlp.Trees.TreeGraphNode[])Children());
			Edu.Stanford.Nlp.Trees.TreeGraphNode kid = kids[i];
			Edu.Stanford.Nlp.Trees.TreeGraphNode[] newKids = new Edu.Stanford.Nlp.Trees.TreeGraphNode[kids.Length - 1];
			for (int j = 0; j < newKids.Length; j++)
			{
				if (j < i)
				{
					newKids[j] = kids[j];
				}
				else
				{
					newKids[j] = kids[j + 1];
				}
			}
			this.children = newKids;
			return kid;
		}

		/// <summary>
		/// Uses the specified
		/// <see cref="IHeadFinder"/>
		/// 
		/// <c>HeadFinder</c>
		/// }
		/// to determine the heads for this node and all its descendants,
		/// and to store references to the head word node and head tag node
		/// in this node's
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// 
		/// <c>CoreLabel</c>
		/// } and the
		/// <c>CoreLabel</c>
		/// s of all its descendants.<p>
		/// <p/>
		/// Note that, in contrast to
		/// <see cref="Tree.PercolateHeads(IHeadFinder)"/>
		/// {
		/// <c>Tree.percolateHeads()</c>
		/// }, which assumes
		/// <see cref="Edu.Stanford.Nlp.Ling.CategoryWordTag"/>
		/// {
		/// <c>CategoryWordTag</c>
		/// } labels and therefore stores head
		/// words and head tags merely as
		/// <c>String</c>
		/// s, this
		/// method stores references to the actual nodes.  This mitigates
		/// potential problems in sentences which contain the same word
		/// more than once.
		/// </summary>
		/// <param name="hf">The headfinding algorithm to use</param>
		public override void PercolateHeads(IHeadFinder hf)
		{
			if (IsLeaf())
			{
				Edu.Stanford.Nlp.Trees.TreeGraphNode hwn = HeadWordNode();
				if (hwn == null)
				{
					SetHeadWordNode(this);
				}
			}
			else
			{
				foreach (Tree child in ((Edu.Stanford.Nlp.Trees.TreeGraphNode[])Children()))
				{
					child.PercolateHeads(hf);
				}
				Edu.Stanford.Nlp.Trees.TreeGraphNode head = SafeCast(hf.DetermineHead(this, parent));
				if (head != null)
				{
					Edu.Stanford.Nlp.Trees.TreeGraphNode hwn = head.HeadWordNode();
					if (hwn == null && head.IsLeaf())
					{
						// below us is a leaf
						SetHeadWordNode(head);
					}
					else
					{
						SetHeadWordNode(hwn);
					}
				}
				else
				{
					log.Info("Head is null: " + this);
				}
			}
		}

		/// <summary>
		/// Return the node containing the head word for this node (or
		/// <see langword="null"/>
		/// if none), as recorded in this node's
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// 
		/// <c>CoreLabel</c>
		/// }.  (In contrast to
		/// <see cref="Edu.Stanford.Nlp.Ling.CategoryWordTag"/>
		/// {
		/// <c>CategoryWordTag</c>
		/// }, we store head words and head
		/// tags as references to nodes, not merely as
		/// <c>String</c>
		/// s.)
		/// </summary>
		/// <returns>the node containing the head word for this node</returns>
		public virtual Edu.Stanford.Nlp.Trees.TreeGraphNode HeadWordNode()
		{
			return headWordNode;
		}

		/// <summary>
		/// Store the node containing the head word for this node by
		/// storing it in this node's
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// {
		/// <c>CoreLabel</c>
		/// }.  (In contrast to
		/// <see cref="Edu.Stanford.Nlp.Ling.CategoryWordTag"/>
		/// {
		/// <c>CategoryWordTag</c>
		/// }, we store head words and head
		/// tags as references to nodes, not merely as
		/// <c>String</c>
		/// s.)
		/// </summary>
		/// <param name="hwn">the node containing the head word for this node</param>
		private void SetHeadWordNode(Edu.Stanford.Nlp.Trees.TreeGraphNode hwn)
		{
			this.headWordNode = hwn;
		}

		/// <summary>
		/// Safely casts an
		/// <c>Object</c>
		/// to a
		/// <c>TreeGraphNode</c>
		/// if possible, else returns
		/// <see langword="null"/>
		/// .
		/// </summary>
		/// <param name="t">
		/// any
		/// <c>Object</c>
		/// </param>
		/// <returns>
		/// 
		/// <paramref name="t"/>
		/// if it is a
		/// <c>TreeGraphNode</c>
		/// ;
		/// <see langword="null"/>
		/// otherwise
		/// </returns>
		private static Edu.Stanford.Nlp.Trees.TreeGraphNode SafeCast(object t)
		{
			if (t == null || !(t is Edu.Stanford.Nlp.Trees.TreeGraphNode))
			{
				return null;
			}
			return (Edu.Stanford.Nlp.Trees.TreeGraphNode)t;
		}

		/// <summary>
		/// Checks the node's ancestors to find the highest ancestor with the
		/// same
		/// <c>headWordNode</c>
		/// as this node.
		/// </summary>
		public virtual Edu.Stanford.Nlp.Trees.TreeGraphNode HighestNodeWithSameHead()
		{
			Edu.Stanford.Nlp.Trees.TreeGraphNode node = this;
			while (true)
			{
				Edu.Stanford.Nlp.Trees.TreeGraphNode parent = SafeCast(((Edu.Stanford.Nlp.Trees.TreeGraphNode)node.Parent()));
				if (parent == null || parent.HeadWordNode() != node.HeadWordNode())
				{
					return node;
				}
				node = parent;
			}
		}

		private class TreeFactoryHolder
		{
			internal static readonly TreeGraphNodeFactory tgnf = new TreeGraphNodeFactory();

			private TreeFactoryHolder()
			{
			}
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Returns a
		/// <c>TreeFactory</c>
		/// that produces
		/// <c>TreeGraphNode</c>
		/// s.  The
		/// <c>Label</c>
		/// of
		/// <c>this</c>
		/// is examined, and providing it is not
		/// <see langword="null"/>
		/// , a
		/// <c>LabelFactory</c>
		/// which will
		/// produce that kind of
		/// <c>Label</c>
		/// is supplied to the
		/// <c>TreeFactory</c>
		/// .  If the
		/// <c>Label</c>
		/// is
		/// <see langword="null"/>
		/// , a
		/// <c>CoreLabel.factory()</c>
		/// will be used.  The factories
		/// returned on different calls are different: a new one is
		/// allocated each time.
		/// </summary>
		/// <returns>a factory to produce treegraphs</returns>
		public override ITreeFactory TreeFactory()
		{
			ILabelFactory lf;
			if (((CoreLabel)Label()) != null)
			{
				lf = ((CoreLabel)Label()).LabelFactory();
			}
			else
			{
				lf = CoreLabel.Factory();
			}
			return new TreeGraphNodeFactory(lf);
		}

		/// <summary>
		/// Return a
		/// <c>TreeFactory</c>
		/// that produces trees of type
		/// <c>TreeGraphNode</c>
		/// .  The factory returned is always
		/// the same one (a singleton).
		/// </summary>
		/// <returns>a factory to produce treegraphs</returns>
		public static ITreeFactory Factory()
		{
			return TreeGraphNode.TreeFactoryHolder.tgnf;
		}

		/// <summary>
		/// Return a
		/// <c>TreeFactory</c>
		/// that produces trees of type
		/// <c>TreeGraphNode</c>
		/// , with the
		/// <c>Label</c>
		/// made
		/// by the supplied
		/// <c>LabelFactory</c>
		/// .  The factory
		/// returned is a different one each time.
		/// </summary>
		/// <param name="lf">
		/// The
		/// <c>LabelFactory</c>
		/// to use
		/// </param>
		/// <returns>a factory to produce treegraphs</returns>
		public static ITreeFactory Factory(ILabelFactory lf)
		{
			return new TreeGraphNodeFactory(lf);
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of this node and
		/// its subtree with one node per line, indented according to
		/// <paramref name="indentLevel"/>
		/// .
		/// </summary>
		/// <param name="indentLevel">how many levels to indent (0 for root node)</param>
		/// <returns>
		/// 
		/// <c>String</c>
		/// representation of this subtree
		/// </returns>
		public virtual string ToPrettyString(int indentLevel)
		{
			StringBuilder buf = new StringBuilder("\n");
			for (int i = 0; i < indentLevel; i++)
			{
				buf.Append("  ");
			}
			if (children == null || children.Length == 0)
			{
				buf.Append(label.ToString(CoreLabel.OutputFormat.ValueIndexMap));
			}
			else
			{
				buf.Append('(').Append(label.ToString(CoreLabel.OutputFormat.ValueIndexMap));
				foreach (TreeGraphNode child in children)
				{
					buf.Append(' ').Append(child.ToPrettyString(indentLevel + 1));
				}
				buf.Append(')');
			}
			return buf.ToString();
		}

		/// <summary>
		/// Returns a
		/// <c>String</c>
		/// representation of this node and
		/// its subtree as a one-line parenthesized list.
		/// </summary>
		/// <returns>
		/// 
		/// <c>String</c>
		/// representation of this subtree
		/// </returns>
		public virtual string ToOneLineString()
		{
			StringBuilder buf = new StringBuilder();
			if (children == null || children.Length == 0)
			{
				buf.Append(label);
			}
			else
			{
				buf.Append('(').Append(label);
				foreach (TreeGraphNode child in children)
				{
					buf.Append(' ').Append(child.ToOneLineString());
				}
				buf.Append(')');
			}
			return buf.ToString();
		}

		public override string ToString()
		{
			return ToString(CoreLabel.DefaultFormat);
		}

		public virtual string ToString(CoreLabel.OutputFormat format)
		{
			return label.ToString(format);
		}

		/// <summary>Just for testing.</summary>
		public static void Main(string[] args)
		{
			try
			{
				ITreeReader tr = new PennTreeReader(new StringReader("(S (NP (NNP Sam)) (VP (VBD died) (NP (NN today))))"), new LabeledScoredTreeFactory());
				Tree t = tr.ReadTree();
				System.Console.Out.WriteLine(t);
				TreeGraphNode tgn = new TreeGraphNode(t, (TreeGraphNode)null);
				System.Console.Out.WriteLine(tgn.ToPrettyString(0));
				EnglishGrammaticalStructure gs = new EnglishGrammaticalStructure(tgn);
				System.Console.Out.WriteLine(tgn.ToPrettyString(0));
				tgn.PercolateHeads(new SemanticHeadFinder());
				System.Console.Out.WriteLine(tgn.ToPrettyString(0));
			}
			catch (Exception e)
			{
				log.Error("Horrible error: " + e);
				log.Error(e);
			}
		}

		private const long serialVersionUID = 5080098143617475328L;
	}
}

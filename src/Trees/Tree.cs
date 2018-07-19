using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// The abstract class
	/// <c>Tree</c>
	/// is used to collect all of the
	/// tree types, and acts as a generic extensible type.  This is the
	/// standard implementation of inheritance-based polymorphism.
	/// All
	/// <c>Tree</c>
	/// objects support accessors for their children (a
	/// <c>Tree[]</c>
	/// ), their label (a
	/// <c>Label</c>
	/// ), and their
	/// score (a
	/// <c>double</c>
	/// ).  However, different concrete
	/// implementations may or may not include the latter two, in which
	/// case a default value is returned.  The class Tree defines no data
	/// fields.  The two abstract methods that must be implemented are:
	/// <c>children()</c>
	/// , and
	/// <c>treeFactory()</c>
	/// .  Notes
	/// that
	/// <c>setChildren(Tree[])</c>
	/// is now an optional
	/// operation, whereas it was previously required to be
	/// implemented. There is now support for finding the parent of a
	/// tree.  This may be done by search from a tree root, or via a
	/// directly stored parent.  The
	/// <c>Tree</c>
	/// class now
	/// implements the
	/// <c>Collection</c>
	/// interface: in terms of
	/// this, each <i>node</i> of the tree is an element of the
	/// collection; hence one can explore the tree by using the methods of
	/// this interface.  A
	/// <c>Tree</c>
	/// is regarded as a read-only
	/// <c>Collection</c>
	/// (even though the
	/// <c>Tree</c>
	/// class
	/// has various methods that modify trees).  Moreover, the
	/// implementation is <i>not</i> thread-safe: no attempt is made to
	/// detect and report concurrent modifications.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Dan Klein</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - filled in types</author>
	[System.Serializable]
	public abstract class Tree : AbstractCollection<Edu.Stanford.Nlp.Trees.Tree>, ILabel, ILabeled, IScored
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tree));

		private const long serialVersionUID = 5441849457648722744L;

		/// <summary>
		/// A leaf node should have a zero-length array for its
		/// children.
		/// </summary>
		/// <remarks>
		/// A leaf node should have a zero-length array for its
		/// children. For efficiency, classes can use this array as a
		/// return value for children() for leaf nodes if desired.
		/// This can also be used elsewhere when you want an empty Tree array.
		/// </remarks>
		public static readonly Edu.Stanford.Nlp.Trees.Tree[] EmptyTreeArray = new Edu.Stanford.Nlp.Trees.Tree[0];

		public Tree()
		{
		}

		/// <summary>Says whether a node is a leaf.</summary>
		/// <remarks>
		/// Says whether a node is a leaf.  Can be used on an arbitrary
		/// <c>Tree</c>
		/// .  Being a leaf is defined as having no
		/// children.  This must be implemented as returning a zero-length
		/// Tree[] array for children().
		/// </remarks>
		/// <returns>true if this object is a leaf</returns>
		public virtual bool IsLeaf()
		{
			return NumChildren() == 0;
		}

		/// <summary>Says how many children a tree node has in its local tree.</summary>
		/// <remarks>
		/// Says how many children a tree node has in its local tree.
		/// Can be used on an arbitrary
		/// <c>Tree</c>
		/// .  Being a leaf is defined
		/// as having no children.
		/// </remarks>
		/// <returns>The number of direct children of the tree node</returns>
		public virtual int NumChildren()
		{
			return Children().Length;
		}

		/// <summary>Says whether the current node has only one child.</summary>
		/// <remarks>
		/// Says whether the current node has only one child.
		/// Can be used on an arbitrary
		/// <c>Tree</c>
		/// .
		/// </remarks>
		/// <returns>Whether the node heads a unary rewrite</returns>
		public virtual bool IsUnaryRewrite()
		{
			return NumChildren() == 1;
		}

		/// <summary>Return whether this node is a preterminal or not.</summary>
		/// <remarks>
		/// Return whether this node is a preterminal or not.  A preterminal is
		/// defined to be a node with one child which is itself a leaf.
		/// </remarks>
		/// <returns>true if the node is a preterminal; false otherwise</returns>
		public virtual bool IsPreTerminal()
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			return (kids.Length == 1) && (kids[0].IsLeaf());
		}

		/// <summary>Return whether all the children of this node are preterminals or not.</summary>
		/// <remarks>
		/// Return whether all the children of this node are preterminals or not.
		/// A preterminal is
		/// defined to be a node with one child which is itself a leaf.
		/// Considered false if the node has no children
		/// </remarks>
		/// <returns>true if the node is a prepreterminal; false otherwise</returns>
		public virtual bool IsPrePreTerminal()
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			if (kids.Length == 0)
			{
				return false;
			}
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				if (!kid.IsPreTerminal())
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Return whether this node is a phrasal node or not.</summary>
		/// <remarks>
		/// Return whether this node is a phrasal node or not.  A phrasal node
		/// is defined to be a node which is not a leaf or a preterminal.
		/// Worded positively, this means that it must have two or more children,
		/// or one child that is not a leaf.
		/// </remarks>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if the node is phrasal;
		/// <see langword="false"/>
		/// otherwise
		/// </returns>
		public virtual bool IsPhrasal()
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			return !(kids == null || kids.Length == 0 || (kids.Length == 1 && kids[0].IsLeaf()));
		}

		/// <summary>Implements equality for Tree's.</summary>
		/// <remarks>
		/// Implements equality for Tree's.  Two Tree objects are equal if they
		/// have equal
		/// <see cref="Value()"/>
		/// s, the same number of children, and their children
		/// are pairwise equal.
		/// </remarks>
		/// <param name="o">The object to compare with</param>
		/// <returns>Whether two things are equal</returns>
		public override bool Equals(object o)
		{
			if (o == this)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Trees.Tree))
			{
				return false;
			}
			Edu.Stanford.Nlp.Trees.Tree t = (Edu.Stanford.Nlp.Trees.Tree)o;
			string value1 = this.Value();
			string value2 = t.Value();
			if (value1 != null || value2 != null)
			{
				if (value1 == null || value2 == null || !value1.Equals(value2))
				{
					return false;
				}
			}
			Edu.Stanford.Nlp.Trees.Tree[] myKids = Children();
			Edu.Stanford.Nlp.Trees.Tree[] theirKids = t.Children();
			//if((myKids == null && (theirKids == null || theirKids.length != 0)) || (theirKids == null && myKids.length != 0) || (myKids.length != theirKids.length)){
			if (myKids.Length != theirKids.Length)
			{
				return false;
			}
			for (int i = 0; i < myKids.Length; i++)
			{
				if (!myKids[i].Equals(theirKids[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Implements a hashCode for Tree's.</summary>
		/// <remarks>
		/// Implements a hashCode for Tree's.  Two trees should have the same
		/// hashcode if they are equal, so we hash on the label value and
		/// the children's label values.
		/// </remarks>
		/// <returns>The hash code</returns>
		public override int GetHashCode()
		{
			string v = this.Value();
			int hc = (v == null) ? 1 : v.GetHashCode();
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			for (int i = 0; i < kids.Length; i++)
			{
				v = kids[i].Value();
				int hc2 = (v == null) ? i : v.GetHashCode();
				hc ^= (hc2 << i);
			}
			return hc;
		}

		/// <summary>
		/// Returns the position of a Tree in the children list, if present,
		/// or -1 if it is not present.
		/// </summary>
		/// <remarks>
		/// Returns the position of a Tree in the children list, if present,
		/// or -1 if it is not present.  Trees are checked for presence with
		/// object equality, ==.  Note that there are very few cases where an
		/// indexOf that used .equals() instead of == would be useful and
		/// correct.  In most cases, you want to figure out which child of
		/// the parent a known tree is, so looking for object equality will
		/// be faster and will avoid cases where you happen to have two
		/// subtrees that are exactly the same.
		/// </remarks>
		/// <param name="tree">The tree to look for in children list</param>
		/// <returns>Its index in the list or -1</returns>
		public virtual int ObjectIndexOf(Edu.Stanford.Nlp.Trees.Tree tree)
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			for (int i = 0; i < kids.Length; i++)
			{
				if (kids[i] == tree)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>Returns an array of children for the current node.</summary>
		/// <remarks>
		/// Returns an array of children for the current node.  If there
		/// are no children (if the node is a leaf), this must return a
		/// Tree[] array of length 0.  A null children() value for tree
		/// leaves was previously supported, but no longer is.
		/// A caller may assume that either
		/// <c>isLeaf()</c>
		/// returns
		/// true, or this node has a nonzero number of children.
		/// </remarks>
		/// <returns>The children of the node</returns>
		/// <seealso cref="GetChildrenAsList()"/>
		public abstract Edu.Stanford.Nlp.Trees.Tree[] Children();

		/// <summary>Returns a List of children for the current node.</summary>
		/// <remarks>
		/// Returns a List of children for the current node.  If there are no
		/// children, then a (non-null)
		/// <c>List&lt;Tree&gt;</c>
		/// of size 0 will
		/// be returned.  The list has new list structure but pointers to,
		/// not copies of the children.  That is, the returned list is mutable,
		/// and simply adding to or deleting items from it is safe, but beware
		/// changing the contents of the children.
		/// </remarks>
		/// <returns>The children of the node</returns>
		public virtual IList<Edu.Stanford.Nlp.Trees.Tree> GetChildrenAsList()
		{
			return new List<Edu.Stanford.Nlp.Trees.Tree>(Arrays.AsList(Children()));
		}

		/// <summary>
		/// Set the children of this node to be the children given in the
		/// array.
		/// </summary>
		/// <remarks>
		/// Set the children of this node to be the children given in the
		/// array.  This is an <b>optional</b> operation; by default it is
		/// unsupported.  Note for subclasses that if there are no
		/// children, the children() method must return a Tree[] array of
		/// length 0.  This class provides a
		/// <c>EMPTY_TREE_ARRAY</c>
		/// canonical zero-length Tree[] array
		/// to represent zero children, but it is <i>not</i> required that
		/// leaf nodes use this particular zero-length array to represent
		/// a leaf node.
		/// </remarks>
		/// <param name="children">
		/// The array of children, each a
		/// <c>Tree</c>
		/// </param>
		/// <seealso cref="SetChildren(System.Collections.Generic.IList{E})"/>
		public virtual void SetChildren(Edu.Stanford.Nlp.Trees.Tree[] children)
		{
			throw new NotSupportedException();
		}

		/// <summary>Set the children of this tree node to the given list.</summary>
		/// <remarks>
		/// Set the children of this tree node to the given list.  This
		/// method is implemented in the
		/// <c>Tree</c>
		/// class by
		/// converting the
		/// <c>List</c>
		/// into a tree array and calling
		/// the array-based method.  Subclasses which use a
		/// <c>List</c>
		/// -based representation of tree children should
		/// override this method.  This implementation allows the case
		/// that the
		/// <c>List</c>
		/// is
		/// <see langword="null"/>
		/// : it yields a
		/// node with no children (represented by a canonical zero-length
		/// children() array).
		/// </remarks>
		/// <param name="childTreesList">
		/// A list of trees to become children of the node.
		/// This method does not retain the List that you pass it (copying
		/// is done), but it will retain the individual children (they are
		/// not copied).
		/// </param>
		/// <seealso cref="SetChildren(Tree[])"/>
		public virtual void SetChildren<_T0>(IList<_T0> childTreesList)
			where _T0 : Edu.Stanford.Nlp.Trees.Tree
		{
			if (childTreesList == null || childTreesList.IsEmpty())
			{
				SetChildren(EmptyTreeArray);
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] childTrees = new Edu.Stanford.Nlp.Trees.Tree[childTreesList.Count];
				Sharpen.Collections.ToArray(childTreesList, childTrees);
				SetChildren(childTrees);
			}
		}

		/// <summary>
		/// Returns the label associated with the current node, or null
		/// if there is no label.
		/// </summary>
		/// <remarks>
		/// Returns the label associated with the current node, or null
		/// if there is no label.  The default implementation always
		/// returns
		/// <see langword="null"/>
		/// .
		/// </remarks>
		/// <returns>The label of the node</returns>
		public virtual ILabel Label()
		{
			return null;
		}

		/// <summary>Sets the label associated with the current node, if there is one.</summary>
		/// <remarks>
		/// Sets the label associated with the current node, if there is one.
		/// The default implementation ignores the label.
		/// </remarks>
		/// <param name="label">The label</param>
		public virtual void SetLabel(ILabel label)
		{
		}

		// a noop
		/// <summary>
		/// Returns the score associated with the current node, or NaN
		/// if there is no score.
		/// </summary>
		/// <remarks>
		/// Returns the score associated with the current node, or NaN
		/// if there is no score.  The default implementation returns NaN.
		/// </remarks>
		/// <returns>The score</returns>
		public virtual double Score()
		{
			return double.NaN;
		}

		/// <summary>Sets the score associated with the current node, if there is one.</summary>
		/// <param name="score">The score</param>
		public virtual void SetScore(double score)
		{
			throw new NotSupportedException("You must use a tree type that implements scoring in order call setScore()");
		}

		/// <summary>
		/// Returns the first child of a tree, or
		/// <see langword="null"/>
		/// if none.
		/// </summary>
		/// <returns>The first child</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree FirstChild()
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			if (kids.Length == 0)
			{
				return null;
			}
			return kids[0];
		}

		/// <summary>
		/// Returns the last child of a tree, or
		/// <see langword="null"/>
		/// if none.
		/// </summary>
		/// <returns>The last child</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree LastChild()
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			if (kids.Length == 0)
			{
				return null;
			}
			return kids[kids.Length - 1];
		}

		/// <summary>
		/// Return the highest node of the (perhaps trivial) unary chain that
		/// this node is part of.
		/// </summary>
		/// <remarks>
		/// Return the highest node of the (perhaps trivial) unary chain that
		/// this node is part of.
		/// In case this node is the only child of its parent, trace up the chain of
		/// unaries, and return the uppermost node of the chain (the node whose
		/// parent has multiple children, or the node that is the root of the tree).
		/// </remarks>
		/// <param name="root">The root of the tree that contains this subtree</param>
		/// <returns>
		/// The uppermost node of the unary chain, if this node is in a unary
		/// chain, or else the current node
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree UpperMostUnary(Edu.Stanford.Nlp.Trees.Tree root)
		{
			Edu.Stanford.Nlp.Trees.Tree parent = Parent(root);
			if (parent == null)
			{
				return this;
			}
			if (parent.NumChildren() > 1)
			{
				return this;
			}
			return parent.UpperMostUnary(root);
		}

		/// <summary>Assign a SpanAnnotation on each node of this tree.</summary>
		/// <remarks>
		/// Assign a SpanAnnotation on each node of this tree.
		/// The index starts at zero.
		/// </remarks>
		public virtual void SetSpans()
		{
			ConstituentsNodes(0);
		}

		/// <summary>Returns SpanAnnotation of this node, or null if annotation is not assigned.</summary>
		/// <remarks>
		/// Returns SpanAnnotation of this node, or null if annotation is not assigned.
		/// Use
		/// <c>setSpans()</c>
		/// to assign SpanAnnotations to a tree.
		/// </remarks>
		/// <returns>an IntPair: the SpanAnnotation of this node.</returns>
		public virtual IntPair GetSpan()
		{
			if (Label() is ICoreMap && ((ICoreMap)Label()).ContainsKey(typeof(CoreAnnotations.SpanAnnotation)))
			{
				return ((ICoreMap)Label()).Get(typeof(CoreAnnotations.SpanAnnotation));
			}
			return null;
		}

		/// <summary>Returns the Constituents generated by the parse tree.</summary>
		/// <remarks>
		/// Returns the Constituents generated by the parse tree. Constituents
		/// are computed with respect to whitespace (e.g., at the word level).
		/// </remarks>
		/// <returns>
		/// a Set of the constituents as constituents of
		/// type
		/// <c>Constituent</c>
		/// </returns>
		public virtual ICollection<Constituent> Constituents()
		{
			return Constituents(new SimpleConstituentFactory());
		}

		/// <summary>Returns the Constituents generated by the parse tree.</summary>
		/// <remarks>
		/// Returns the Constituents generated by the parse tree.
		/// The Constituents of a sentence include the preterminal categories
		/// but not the leaves.
		/// </remarks>
		/// <param name="cf">ConstituentFactory used to build the Constituent objects</param>
		/// <returns>
		/// a Set of the constituents as SimpleConstituent type
		/// (in the current implementation, a
		/// <c>HashSet</c>
		/// </returns>
		public virtual ICollection<Constituent> Constituents(IConstituentFactory cf)
		{
			return Constituents(cf, false);
		}

		/// <summary>Returns the Constituents generated by the parse tree.</summary>
		/// <remarks>
		/// Returns the Constituents generated by the parse tree.
		/// The Constituents of a sentence include the preterminal categories
		/// but not the leaves.
		/// </remarks>
		/// <param name="cf">ConstituentFactory used to build the Constituent objects</param>
		/// <param name="maxDepth">
		/// The maximum depth at which to add constituents,
		/// where 0 is the root level.  Negative maxDepth
		/// indicates no maximum.
		/// </param>
		/// <returns>
		/// a Set of the constituents as SimpleConstituent type
		/// (in the current implementation, a
		/// <c>HashSet</c>
		/// </returns>
		public virtual ICollection<Constituent> Constituents(IConstituentFactory cf, int maxDepth)
		{
			ICollection<Constituent> constituentsSet = Generics.NewHashSet();
			Constituents(constituentsSet, 0, cf, false, null, maxDepth, 0);
			return constituentsSet;
		}

		/// <summary>Returns the Constituents generated by the parse tree.</summary>
		/// <remarks>
		/// Returns the Constituents generated by the parse tree.
		/// The Constituents of a sentence include the preterminal categories
		/// but not the leaves.
		/// </remarks>
		/// <param name="cf">ConstituentFactory used to build the Constituent objects</param>
		/// <param name="charLevel">If true, compute bracketings irrespective of whitespace boundaries.</param>
		/// <returns>
		/// a Set of the constituents as SimpleConstituent type
		/// (in the current implementation, a
		/// <c>HashSet</c>
		/// </returns>
		public virtual ICollection<Constituent> Constituents(IConstituentFactory cf, bool charLevel)
		{
			ICollection<Constituent> constituentsSet = Generics.NewHashSet();
			Constituents(constituentsSet, 0, cf, charLevel, null, -1, 0);
			return constituentsSet;
		}

		public virtual ICollection<Constituent> Constituents(IConstituentFactory cf, bool charLevel, IPredicate<Edu.Stanford.Nlp.Trees.Tree> filter)
		{
			ICollection<Constituent> constituentsSet = Generics.NewHashSet();
			Constituents(constituentsSet, 0, cf, charLevel, filter, -1, 0);
			return constituentsSet;
		}

		/// <summary>
		/// Same as int constituents but just puts the span as an IntPair
		/// in the CoreLabel of the nodes.
		/// </summary>
		/// <param name="left">The left position to begin labeling from</param>
		/// <returns>The index of the right frontier of the constituent</returns>
		private int ConstituentsNodes(int left)
		{
			if (IsLeaf())
			{
				if (Label() is CoreLabel)
				{
					((CoreLabel)Label()).Set(typeof(CoreAnnotations.SpanAnnotation), new IntPair(left, left));
				}
				else
				{
					throw new NotSupportedException("Can only set spans on trees which use CoreLabel");
				}
				return (left + 1);
			}
			int position = left;
			// enumerate through daughter trees
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				position = kid.ConstituentsNodes(position);
			}
			//Parent span
			if (Label() is CoreLabel)
			{
				((CoreLabel)Label()).Set(typeof(CoreAnnotations.SpanAnnotation), new IntPair(left, position - 1));
			}
			else
			{
				throw new NotSupportedException("Can only set spans on trees which use CoreLabel");
			}
			return position;
		}

		/// <summary>
		/// Adds the constituents derived from
		/// <c>this</c>
		/// tree to
		/// the ordered
		/// <c>Constituent</c>
		/// 
		/// <c>Set</c>
		/// , beginning
		/// numbering from the second argument and returning the number of
		/// the right edge.  The reason for the return of the right frontier
		/// is in order to produce bracketings recursively by threading through
		/// the daughters of a given tree.
		/// </summary>
		/// <param name="constituentsSet">
		/// set of constituents to add results of bracketing
		/// this tree to
		/// </param>
		/// <param name="left">left position to begin labeling the bracketings with</param>
		/// <param name="cf">ConstituentFactory used to build the Constituent objects</param>
		/// <param name="charLevel">If true, compute constituents without respect to whitespace. Otherwise, preserve whitespace boundaries.</param>
		/// <param name="filter">A filter to use to decide whether or not to add a tree as a constituent.</param>
		/// <param name="maxDepth">The maximum depth at which to allow constituents.  Set to negative to indicate all depths allowed.</param>
		/// <param name="depth">The current depth</param>
		/// <returns>Index of right frontier of Constituent</returns>
		private int Constituents(ICollection<Constituent> constituentsSet, int left, IConstituentFactory cf, bool charLevel, IPredicate<Edu.Stanford.Nlp.Trees.Tree> filter, int maxDepth, int depth)
		{
			if (IsPreTerminal())
			{
				return left + ((charLevel) ? FirstChild().Value().Length : 1);
			}
			int position = left;
			// log.info("In bracketing trees left is " + left);
			// log.info("  label is " + label() +
			//                       "; num daughters: " + children().length);
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				position = kid.Constituents(constituentsSet, position, cf, charLevel, filter, maxDepth, depth + 1);
			}
			// log.info("  position went to " + position);
			if ((filter == null || filter.Test(this)) && (maxDepth < 0 || depth <= maxDepth))
			{
				//Compute span of entire tree at the end of recursion
				constituentsSet.Add(cf.NewConstituent(left, position - 1, Label(), Score()));
			}
			// log.info("  added " + label());
			return position;
		}

		/// <summary>Returns a new Tree that represents the local Tree at a certain node.</summary>
		/// <remarks>
		/// Returns a new Tree that represents the local Tree at a certain node.
		/// That is, it builds a new tree that copies the mother and daughter
		/// nodes (but not their Labels), as non-Leaf nodes,
		/// but zeroes out their children.
		/// </remarks>
		/// <returns>A local tree</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree LocalTree()
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			Edu.Stanford.Nlp.Trees.Tree[] newKids = new Edu.Stanford.Nlp.Trees.Tree[kids.Length];
			ITreeFactory tf = TreeFactory();
			for (int i = 0; i < n; i++)
			{
				newKids[i] = tf.NewTreeNode(kids[i].Label(), Arrays.AsList(EmptyTreeArray));
			}
			return tf.NewTreeNode(Label(), Arrays.AsList(newKids));
		}

		/// <summary>
		/// Returns a set of one level
		/// <c>Tree</c>
		/// s that ares the local trees
		/// of the tree.
		/// That is, it builds a new tree that copies the mother and daughter
		/// nodes (but not their Labels), for each phrasal node,
		/// but zeroes out their children.
		/// </summary>
		/// <returns>A set of local tree</returns>
		public virtual ICollection<Edu.Stanford.Nlp.Trees.Tree> LocalTrees()
		{
			ICollection<Edu.Stanford.Nlp.Trees.Tree> set = Generics.NewHashSet();
			foreach (Edu.Stanford.Nlp.Trees.Tree st in this)
			{
				if (st.IsPhrasal())
				{
					set.Add(st.LocalTree());
				}
			}
			return set;
		}

		/// <summary>
		/// Most instances of
		/// <c>Tree</c>
		/// will take a lot more than
		/// than the default
		/// <c>StringBuffer</c>
		/// size of 16 to print
		/// as an indented list of the whole tree, so we enlarge the default.
		/// </summary>
		private const int initialPrintStringBuilderSize = 500;

		/// <summary>
		/// Appends the printed form of a parse tree (as a bracketed String)
		/// to a
		/// <c>StringBuilder</c>
		/// .
		/// The implementation of this may be more efficient than for
		/// <c>toString()</c>
		/// on complex trees.
		/// </summary>
		/// <param name="sb">
		/// The
		/// <c>StringBuilder</c>
		/// to which the tree will be appended
		/// </param>
		/// <returns>
		/// Returns the
		/// <c>StringBuilder</c>
		/// passed in with extra stuff in it
		/// </returns>
		public virtual StringBuilder ToStringBuilder(StringBuilder sb)
		{
			return ToStringBuilder(sb, null);
		}

		/// <summary>
		/// Appends the printed form of a parse tree (as a bracketed String)
		/// to a
		/// <c>StringBuilder</c>
		/// .
		/// The implementation of this may be more efficient than for
		/// <c>toString()</c>
		/// on complex trees.
		/// </summary>
		/// <param name="sb">
		/// The
		/// <c>StringBuilder</c>
		/// to which the tree will be appended
		/// </param>
		/// <param name="labelFormatter">Formatting routine for how to print a Label</param>
		/// <returns>
		/// Returns the
		/// <c>StringBuilder</c>
		/// passed in with extra stuff in it
		/// </returns>
		public virtual StringBuilder ToStringBuilder(StringBuilder sb, IFunction<ILabel, string> labelFormatter)
		{
			if (IsLeaf())
			{
				if (Label() != null)
				{
					sb.Append(labelFormatter.Apply(Label()));
				}
				return sb;
			}
			else
			{
				sb.Append('(');
				if (Label() != null)
				{
					sb.Append(labelFormatter.Apply(Label()));
				}
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				if (kids != null)
				{
					foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
					{
						sb.Append(' ');
						kid.ToStringBuilder(sb, labelFormatter);
					}
				}
				return sb.Append(')');
			}
		}

		/// <summary>Converts parse tree to string in Penn Treebank format.</summary>
		/// <remarks>
		/// Converts parse tree to string in Penn Treebank format.
		/// Implementation note: Internally, the method gains
		/// efficiency by chaining use of a single
		/// <c>StringBuilder</c>
		/// through all the printing.
		/// </remarks>
		/// <returns>the tree as a bracketed list on one line</returns>
		public override string ToString()
		{
			return ToStringBuilder(new StringBuilder(Edu.Stanford.Nlp.Trees.Tree.initialPrintStringBuilderSize)).ToString();
		}

		private const int indentIncr = 2;

		private static string MakeIndentString(int indent)
		{
			StringBuilder sb = new StringBuilder(indent);
			for (int i = 0; i < indentIncr; i++)
			{
				sb.Append(' ');
			}
			return sb.ToString();
		}

		public virtual void PrintLocalTree()
		{
			PrintLocalTree(new PrintWriter(System.Console.Out, true));
		}

		/// <summary>Only prints the local tree structure, does not recurse</summary>
		public virtual void PrintLocalTree(PrintWriter pw)
		{
			pw.Print("(" + Label() + ' ');
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
			{
				pw.Print("(");
				pw.Print(kid.Label());
				pw.Print(") ");
			}
			pw.Println(")");
		}

		/// <summary>Indented list printing of a tree.</summary>
		/// <remarks>
		/// Indented list printing of a tree.  The tree is printed in an
		/// indented list notation, with node labels followed by node scores.
		/// </remarks>
		public virtual void IndentedListPrint()
		{
			IndentedListPrint(new PrintWriter(System.Console.Out, true), false);
		}

		/// <summary>Indented list printing of a tree.</summary>
		/// <remarks>
		/// Indented list printing of a tree.  The tree is printed in an
		/// indented list notation, with node labels followed by node scores.
		/// </remarks>
		/// <param name="pw">The PrintWriter to print the tree to</param>
		/// <param name="printScores">Whether to print the scores (log probs) of tree nodes</param>
		public virtual void IndentedListPrint(PrintWriter pw, bool printScores)
		{
			IndentedListPrint(string.Empty, MakeIndentString(indentIncr), pw, printScores);
		}

		/// <summary>Indented list printing of a tree.</summary>
		/// <remarks>
		/// Indented list printing of a tree.  The tree is printed in an
		/// indented list notation, with node labels followed by node scores.
		/// String parameters are used rather than integer levels for efficiency.
		/// </remarks>
		/// <param name="indent">
		/// The base
		/// <c>String</c>
		/// (normally just spaces)
		/// to print before each line of tree
		/// </param>
		/// <param name="pad">
		/// The additional
		/// <c>String</c>
		/// (normally just more
		/// spaces) to add when going to a deeper level of
		/// <c>Tree</c>
		/// .
		/// </param>
		/// <param name="pw">The PrintWriter to print the tree to</param>
		/// <param name="printScores">Whether to print the scores (log probs) of tree nodes</param>
		private void IndentedListPrint(string indent, string pad, PrintWriter pw, bool printScores)
		{
			StringBuilder sb = new StringBuilder(indent);
			ILabel label = Label();
			if (label != null)
			{
				sb.Append(label);
			}
			if (printScores)
			{
				sb.Append("  ");
				sb.Append(Score());
			}
			pw.Println(sb);
			Edu.Stanford.Nlp.Trees.Tree[] children = Children();
			string newIndent = indent + pad;
			foreach (Edu.Stanford.Nlp.Trees.Tree child in children)
			{
				child.IndentedListPrint(newIndent, pad, pw, printScores);
			}
		}

		/// <summary>Indented xml printing of a tree.</summary>
		/// <remarks>Indented xml printing of a tree.  The tree is printed in an indented xml notation.</remarks>
		public virtual void IndentedXMLPrint()
		{
			IndentedXMLPrint(new PrintWriter(System.Console.Out, true), false);
		}

		/// <summary>Indented xml printing of a tree.</summary>
		/// <remarks>
		/// Indented xml printing of a tree.  The tree is printed in an
		/// indented xml notation, with node labels followed by node scores.
		/// </remarks>
		/// <param name="pw">The PrintWriter to print the tree to</param>
		/// <param name="printScores">Whether to print the scores (log probs) of tree nodes</param>
		public virtual void IndentedXMLPrint(PrintWriter pw, bool printScores)
		{
			IndentedXMLPrint(string.Empty, MakeIndentString(indentIncr), pw, printScores);
		}

		/// <summary>Indented xml printing of a tree.</summary>
		/// <remarks>
		/// Indented xml printing of a tree.  The tree is printed in an
		/// indented xml notation, with node labels followed by node scores.
		/// String parameters are used rather than integer levels for efficiency.
		/// </remarks>
		/// <param name="indent">
		/// The base
		/// <c>String</c>
		/// (normally just spaces)
		/// to print before each line of tree
		/// </param>
		/// <param name="pad">
		/// The additional
		/// <c>String</c>
		/// (normally just more
		/// spaces) to add when going to a deeper level of
		/// <c>Tree</c>
		/// .
		/// </param>
		/// <param name="pw">The PrintWriter to print the tree to</param>
		/// <param name="printScores">Whether to print the scores (log probs) of tree nodes</param>
		private void IndentedXMLPrint(string indent, string pad, PrintWriter pw, bool printScores)
		{
			StringBuilder sb = new StringBuilder(indent);
			Edu.Stanford.Nlp.Trees.Tree[] children = Children();
			ILabel label = Label();
			if (label != null)
			{
				sb.Append('<');
				if (children.Length > 0)
				{
					sb.Append("node value=\"");
				}
				else
				{
					sb.Append("leaf value=\"");
				}
				sb.Append(XMLUtils.EscapeXML(SentenceUtils.WordToString(label, true)));
				sb.Append('"');
				if (printScores)
				{
					sb.Append(" score=");
					sb.Append(Score());
				}
				if (children.Length > 0)
				{
					sb.Append('>');
				}
				else
				{
					sb.Append("/>");
				}
			}
			else
			{
				if (children.Length > 0)
				{
					sb.Append("<node>");
				}
				else
				{
					sb.Append("<leaf/>");
				}
			}
			pw.Println(sb);
			if (children.Length > 0)
			{
				string newIndent = indent + pad;
				foreach (Edu.Stanford.Nlp.Trees.Tree child in children)
				{
					child.IndentedXMLPrint(newIndent, pad, pw, printScores);
				}
				pw.Println(indent + "</node>");
			}
		}

		private static void DisplayChildren(Edu.Stanford.Nlp.Trees.Tree[] trChildren, int indent, bool parentLabelNull, IFunction<ILabel, string> labelFormatter, PrintWriter pw)
		{
			bool firstSibling = true;
			bool leftSibIsPreTerm = true;
			// counts as true at beginning
			foreach (Edu.Stanford.Nlp.Trees.Tree currentTree in trChildren)
			{
				currentTree.Display(indent, parentLabelNull, firstSibling, leftSibIsPreTerm, false, labelFormatter, pw);
				leftSibIsPreTerm = currentTree.IsPreTerminal();
				// CC is a special case for English, but leave it in so we can exactly match PTB3 tree formatting
				if (currentTree.Value() != null && currentTree.Value().StartsWith("CC"))
				{
					leftSibIsPreTerm = false;
				}
				firstSibling = false;
			}
		}

		/// <summary>Returns the value of the node's label as a String.</summary>
		/// <remarks>
		/// Returns the value of the node's label as a String.  This is done by
		/// calling
		/// <c>toString()</c>
		/// on the value, if it exists. Otherwise,
		/// an empty string is returned.
		/// </remarks>
		/// <returns>The label of a tree node as a String</returns>
		public virtual string NodeString()
		{
			return (Value() == null) ? string.Empty : Value();
		}

		/// <summary>Display a node, implementing Penn Treebank style layout</summary>
		private void Display(int indent, bool parentLabelNull, bool firstSibling, bool leftSiblingPreTerminal, bool topLevel, IFunction<ILabel, string> labelFormatter, PrintWriter pw)
		{
			// the condition for staying on the same line in Penn Treebank
			bool suppressIndent = (parentLabelNull || (firstSibling && IsPreTerminal()) || (leftSiblingPreTerminal && IsPreTerminal() && (Label() == null || !Label().Value().StartsWith("CC"))));
			if (suppressIndent)
			{
				pw.Print(" ");
			}
			else
			{
				// pw.flush();
				if (!topLevel)
				{
					pw.Println();
				}
				for (int i = 0; i < indent; i++)
				{
					pw.Print("  ");
				}
			}
			// pw.flush();
			if (IsLeaf() || IsPreTerminal())
			{
				string terminalString = ToStringBuilder(new StringBuilder(), labelFormatter).ToString();
				pw.Print(terminalString);
				pw.Flush();
				return;
			}
			pw.Print("(");
			pw.Print(labelFormatter.Apply(Label()));
			// pw.flush();
			bool parentIsNull = Label() == null || Label().Value() == null;
			DisplayChildren(Children(), indent + 1, parentIsNull, labelFormatter, pw);
			pw.Print(")");
			pw.Flush();
		}

		/// <summary>Print the tree as done in Penn Treebank merged files.</summary>
		/// <remarks>
		/// Print the tree as done in Penn Treebank merged files.
		/// The formatting should be exactly the same, but we don't print the
		/// trailing whitespace found in Penn Treebank trees.
		/// The basic deviation from a bracketed indented tree is to in general
		/// collapse the printing of adjacent preterminals onto one line of
		/// tags and words.  Additional complexities are that conjunctions
		/// (tag CC) are not collapsed in this way, and that the unlabeled
		/// outer brackets are collapsed onto the same line as the next
		/// bracket down.
		/// </remarks>
		/// <param name="pw">
		/// The tree is printed to this
		/// <c>PrintWriter</c>
		/// </param>
		public virtual void PennPrint(PrintWriter pw)
		{
			PennPrint(pw, null);
		}

		public virtual void PennPrint(PrintWriter pw, IFunction<ILabel, string> labelFormatter)
		{
			Display(0, false, false, false, true, labelFormatter, pw);
			pw.Println();
			pw.Flush();
		}

		/// <summary>Print the tree as done in Penn Treebank merged files.</summary>
		/// <remarks>
		/// Print the tree as done in Penn Treebank merged files.
		/// The formatting should be exactly the same, but we don't print the
		/// trailing whitespace found in Penn Treebank trees.
		/// The basic deviation from a bracketed indented tree is to in general
		/// collapse the printing of adjacent preterminals onto one line of
		/// tags and words.  Additional complexities are that conjunctions
		/// (tag CC) are not collapsed in this way, and that the unlabeled
		/// outer brackets are collapsed onto the same line as the next
		/// bracket down.
		/// </remarks>
		/// <param name="ps">
		/// The tree is printed to this
		/// <c>PrintStream</c>
		/// </param>
		public virtual void PennPrint(TextWriter ps)
		{
			PennPrint(new PrintWriter(new OutputStreamWriter(ps), true));
		}

		public virtual void PennPrint(TextWriter ps, IFunction<ILabel, string> labelFormatter)
		{
			PennPrint(new PrintWriter(new OutputStreamWriter(ps), true), labelFormatter);
		}

		/// <summary>
		/// Calls
		/// <c>pennPrint()</c>
		/// and saves output to a String
		/// </summary>
		/// <returns>The indent S-expression representation of a Tree</returns>
		public virtual string PennString()
		{
			StringWriter sw = new StringWriter();
			PennPrint(new PrintWriter(sw));
			return sw.ToString();
		}

		/// <summary>Print the tree as done in Penn Treebank merged files.</summary>
		/// <remarks>
		/// Print the tree as done in Penn Treebank merged files.
		/// The formatting should be exactly the same, but we don't print the
		/// trailing whitespace found in Penn Treebank trees.
		/// The tree is printed to
		/// <c>System.out</c>
		/// . The basic deviation
		/// from a bracketed indented tree is to in general
		/// collapse the printing of adjacent preterminals onto one line of
		/// tags and words.  Additional complexities are that conjunctions
		/// (tag CC) are not collapsed in this way, and that the unlabeled
		/// outer brackets are collapsed onto the same line as the next
		/// bracket down.
		/// </remarks>
		public virtual void PennPrint()
		{
			PennPrint(System.Console.Out);
		}

		/// <summary>Finds the depth of the tree.</summary>
		/// <remarks>
		/// Finds the depth of the tree.  The depth is defined as the length
		/// of the longest path from this node to a leaf node.  Leaf nodes
		/// have depth zero.  POS tags have depth 1. Phrasal nodes have
		/// depth &gt;= 2.
		/// </remarks>
		/// <returns>the depth</returns>
		public virtual int Depth()
		{
			if (IsLeaf())
			{
				return 0;
			}
			int maxDepth = 0;
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				int curDepth = kid.Depth();
				if (curDepth > maxDepth)
				{
					maxDepth = curDepth;
				}
			}
			return maxDepth + 1;
		}

		/// <summary>Finds the distance from this node to the specified node.</summary>
		/// <remarks>
		/// Finds the distance from this node to the specified node.
		/// return -1 if this is not an ancestor of node.
		/// </remarks>
		/// <param name="node">A subtree contained in this tree</param>
		/// <returns>the depth</returns>
		public virtual int Depth(Edu.Stanford.Nlp.Trees.Tree node)
		{
			Edu.Stanford.Nlp.Trees.Tree p = node.Parent(this);
			if (this == node)
			{
				return 0;
			}
			if (p == null)
			{
				return -1;
			}
			int depth = 1;
			while (this != p)
			{
				p = p.Parent(this);
				depth++;
			}
			return depth;
		}

		/// <summary>Returns the tree leaf that is the head of the tree.</summary>
		/// <param name="hf">The head-finding algorithm to use</param>
		/// <param name="parent">The parent of this tree</param>
		/// <returns>
		/// The head tree leaf if any, else
		/// <see langword="null"/>
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree HeadTerminal(IHeadFinder hf, Edu.Stanford.Nlp.Trees.Tree parent)
		{
			if (IsLeaf())
			{
				return this;
			}
			Edu.Stanford.Nlp.Trees.Tree head = hf.DetermineHead(this, parent);
			if (head != null)
			{
				return head.HeadTerminal(hf, parent);
			}
			log.Info("Head is null: " + this);
			return null;
		}

		/// <summary>Returns the tree leaf that is the head of the tree.</summary>
		/// <param name="hf">The headfinding algorithm to use</param>
		/// <returns>
		/// The head tree leaf if any, else
		/// <see langword="null"/>
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree HeadTerminal(IHeadFinder hf)
		{
			return HeadTerminal(hf, null);
		}

		/// <summary>Returns the preterminal tree that is the head of the tree.</summary>
		/// <remarks>
		/// Returns the preterminal tree that is the head of the tree.
		/// See
		/// <see cref="IsPreTerminal()"/>
		/// for
		/// the definition of a preterminal node. Beware that some tree nodes may
		/// have no preterminal head.
		/// </remarks>
		/// <param name="hf">The headfinding algorithm to use</param>
		/// <returns>
		/// The head preterminal tree, if any, else
		/// <see langword="null"/>
		/// </returns>
		/// <exception cref="System.ArgumentException">if called on a leaf node</exception>
		public virtual Edu.Stanford.Nlp.Trees.Tree HeadPreTerminal(IHeadFinder hf)
		{
			if (IsPreTerminal())
			{
				return this;
			}
			else
			{
				if (IsLeaf())
				{
					throw new ArgumentException("Called headPreTerminal on a leaf: " + this);
				}
				else
				{
					Edu.Stanford.Nlp.Trees.Tree head = hf.DetermineHead(this);
					if (head != null)
					{
						return head.HeadPreTerminal(hf);
					}
					log.Info("Head preterminal is null: " + this);
					return null;
				}
			}
		}

		/// <summary>
		/// Finds the head words of each tree and assigns
		/// HeadWordLabelAnnotation on each node pointing to the correct
		/// CoreLabel.
		/// </summary>
		/// <remarks>
		/// Finds the head words of each tree and assigns
		/// HeadWordLabelAnnotation on each node pointing to the correct
		/// CoreLabel.  This relies on the nodes being CoreLabels, so it
		/// throws an IllegalArgumentException if this is ever not true.
		/// </remarks>
		public virtual void PercolateHeadAnnotations(IHeadFinder hf)
		{
			if (!(Label() is CoreLabel))
			{
				throw new ArgumentException("Expected CoreLabels in the trees");
			}
			CoreLabel nodeLabel = (CoreLabel)Label();
			if (IsLeaf())
			{
				return;
			}
			if (IsPreTerminal())
			{
				nodeLabel.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), (CoreLabel)Children()[0].Label());
				nodeLabel.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), nodeLabel);
				return;
			}
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
			{
				kid.PercolateHeadAnnotations(hf);
			}
			Edu.Stanford.Nlp.Trees.Tree head = hf.DetermineHead(this);
			if (head == null)
			{
				throw new ArgumentNullException("HeadFinder " + hf + " returned null for " + this);
			}
			else
			{
				if (head.IsLeaf())
				{
					nodeLabel.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), (CoreLabel)head.Label());
					nodeLabel.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), (CoreLabel)head.Parent(this).Label());
				}
				else
				{
					if (head.IsPreTerminal())
					{
						nodeLabel.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), (CoreLabel)head.Children()[0].Label());
						nodeLabel.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), (CoreLabel)head.Label());
					}
					else
					{
						if (!(head.Label() is CoreLabel))
						{
							throw new AssertionError("Horrible bug");
						}
						CoreLabel headLabel = (CoreLabel)head.Label();
						nodeLabel.Set(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation), headLabel.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)));
						nodeLabel.Set(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation), headLabel.Get(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation)));
					}
				}
			}
		}

		/// <summary>Finds the heads of the tree.</summary>
		/// <remarks>
		/// Finds the heads of the tree.  This code assumes that the label
		/// does store and return sensible values for the category, word, and tag.
		/// It will be a no-op otherwise.  The tree is modified.  The routine
		/// assumes the Tree has word leaves and tag preterminals, and copies
		/// their category to word and tag respectively, if they have a null
		/// value.
		/// </remarks>
		/// <param name="hf">The headfinding algorithm to use</param>
		public virtual void PercolateHeads(IHeadFinder hf)
		{
			ILabel nodeLabel = Label();
			if (IsLeaf())
			{
				// Sanity check: word() is usually set by the TreeReader.
				if (nodeLabel is IHasWord)
				{
					IHasWord w = (IHasWord)nodeLabel;
					if (w.Word() == null)
					{
						w.SetWord(nodeLabel.Value());
					}
				}
			}
			else
			{
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
				{
					kid.PercolateHeads(hf);
				}
				Edu.Stanford.Nlp.Trees.Tree head = hf.DetermineHead(this);
				if (head != null)
				{
					ILabel headLabel = head.Label();
					// Set the head tag.
					string headTag = (headLabel is IHasTag) ? ((IHasTag)headLabel).Tag() : null;
					if (headTag == null && head.IsLeaf())
					{
						// below us is a leaf
						headTag = nodeLabel.Value();
					}
					// Set the head word
					string headWord = (headLabel is IHasWord) ? ((IHasWord)headLabel).Word() : null;
					if (headWord == null && head.IsLeaf())
					{
						// below us is a leaf
						// this might be useful despite case for leaf above in
						// case the leaf label type doesn't support word()
						headWord = headLabel.Value();
					}
					// Set the head index
					int headIndex = (headLabel is IHasIndex) ? ((IHasIndex)headLabel).Index() : -1;
					if (nodeLabel is IHasWord)
					{
						((IHasWord)nodeLabel).SetWord(headWord);
					}
					if (nodeLabel is IHasTag)
					{
						((IHasTag)nodeLabel).SetTag(headTag);
					}
					if (nodeLabel is IHasIndex && headIndex >= 0)
					{
						((IHasIndex)nodeLabel).SetIndex(headIndex);
					}
				}
				else
				{
					log.Info("Head is null: " + this);
				}
			}
		}

		/// <summary>
		/// Return a Set of TaggedWord-TaggedWord dependencies, represented as
		/// Dependency objects, for the Tree.
		/// </summary>
		/// <remarks>
		/// Return a Set of TaggedWord-TaggedWord dependencies, represented as
		/// Dependency objects, for the Tree.  This will only give
		/// useful results if the internal tree node labels support HasWord and
		/// HasTag, and head percolation has already been done (see
		/// percolateHeads()).
		/// </remarks>
		/// <returns>Set of dependencies (each a Dependency)</returns>
		public virtual ICollection<IDependency<ILabel, ILabel, object>> Dependencies()
		{
			return Dependencies(Filters.AcceptFilter());
		}

		public virtual ICollection<IDependency<ILabel, ILabel, object>> Dependencies(IPredicate<IDependency<ILabel, ILabel, object>> f)
		{
			return Dependencies(f, true, true, false);
		}

		/// <summary>Convert a constituency label to a dependency label.</summary>
		/// <remarks>
		/// Convert a constituency label to a dependency label. Options are provided for selecting annotations
		/// to copy.
		/// </remarks>
		/// <param name="oldLabel"/>
		/// <param name="copyLabel"/>
		/// <param name="copyIndex"/>
		/// <param name="copyPosTag"/>
		private static ILabel MakeDependencyLabel(ILabel oldLabel, bool copyLabel, bool copyIndex, bool copyPosTag)
		{
			if (!copyLabel)
			{
				return oldLabel;
			}
			string wordForm = (oldLabel is IHasWord) ? ((IHasWord)oldLabel).Word() : oldLabel.Value();
			ILabel newLabel = oldLabel.LabelFactory().NewLabel(wordForm);
			if (newLabel is IHasWord)
			{
				((IHasWord)newLabel).SetWord(wordForm);
			}
			if (copyPosTag && newLabel is IHasTag && oldLabel is IHasTag)
			{
				string tag = ((IHasTag)oldLabel).Tag();
				((IHasTag)newLabel).SetTag(tag);
			}
			if (copyIndex && newLabel is IHasIndex && oldLabel is IHasIndex)
			{
				int index = ((IHasIndex)oldLabel).Index();
				((IHasIndex)newLabel).SetIndex(index);
			}
			return newLabel;
		}

		/// <summary>
		/// Return a set of TaggedWord-TaggedWord dependencies, represented as
		/// Dependency objects, for the Tree.
		/// </summary>
		/// <remarks>
		/// Return a set of TaggedWord-TaggedWord dependencies, represented as
		/// Dependency objects, for the Tree.  This will only give
		/// useful results if the internal tree node labels support HasWord and
		/// head percolation has already been done (see percolateHeads()).
		/// </remarks>
		/// <param name="f">
		/// Dependencies are excluded for which the Dependency is not
		/// accepted by the Filter
		/// </param>
		/// <returns>Set of dependencies (each a Dependency)</returns>
		public virtual ICollection<IDependency<ILabel, ILabel, object>> Dependencies(IPredicate<IDependency<ILabel, ILabel, object>> f, bool isConcrete, bool copyLabel, bool copyPosTag)
		{
			ICollection<IDependency<ILabel, ILabel, object>> deps = Generics.NewHashSet();
			foreach (Edu.Stanford.Nlp.Trees.Tree node in this)
			{
				// Skip leaves and unary re-writes
				if (node.IsLeaf() || node.Children().Length < 2)
				{
					continue;
				}
				// Create the head label (percolateHeads has already been executed)
				ILabel headLabel = MakeDependencyLabel(node.Label(), copyLabel, isConcrete, copyPosTag);
				string headWord = ((IHasWord)headLabel).Word();
				if (headWord == null)
				{
					headWord = headLabel.Value();
				}
				int headIndex = (isConcrete && (headLabel is IHasIndex)) ? ((IHasIndex)headLabel).Index() : -1;
				// every child with a different (or repeated) head is an argument
				bool seenHead = false;
				foreach (Edu.Stanford.Nlp.Trees.Tree child in node.Children())
				{
					ILabel depLabel = MakeDependencyLabel(child.Label(), copyLabel, isConcrete, copyPosTag);
					string depWord = ((IHasWord)depLabel).Word();
					if (depWord == null)
					{
						depWord = depLabel.Value();
					}
					int depIndex = (isConcrete && (depLabel is IHasIndex)) ? ((IHasIndex)depLabel).Index() : -1;
					if (!seenHead && headIndex == depIndex && headWord.Equals(depWord))
					{
						seenHead = true;
					}
					else
					{
						IDependency<ILabel, ILabel, object> dependency = (isConcrete && depIndex != headIndex) ? new UnnamedConcreteDependency(headLabel, depLabel) : new UnnamedDependency(headLabel, depLabel);
						if (f.Test(dependency))
						{
							deps.Add(dependency);
						}
					}
				}
			}
			return deps;
		}

		/// <summary>
		/// Return a set of Label-Label dependencies, represented as
		/// Dependency objects, for the Tree.
		/// </summary>
		/// <remarks>
		/// Return a set of Label-Label dependencies, represented as
		/// Dependency objects, for the Tree.  The Labels are the ones of the leaf
		/// nodes of the tree, without mucking with them.
		/// </remarks>
		/// <param name="f">
		/// Dependencies are excluded for which the Dependency is not
		/// accepted by the Filter
		/// </param>
		/// <param name="hf">
		/// The HeadFinder to use to identify the head of constituents.
		/// The code assumes
		/// that it can use
		/// <c>headPreTerminal(hf)</c>
		/// to find a
		/// tag and word to make a CoreLabel.
		/// </param>
		/// <returns>
		/// Set of dependencies (each a
		/// <c>Dependency</c>
		/// between two
		/// <c>CoreLabel</c>
		/// s, which each contain a tag(), word(),
		/// and value(), the last two of which are identical).
		/// </returns>
		public virtual ICollection<IDependency<ILabel, ILabel, object>> MapDependencies(IPredicate<IDependency<ILabel, ILabel, object>> f, IHeadFinder hf)
		{
			if (hf == null)
			{
				throw new ArgumentException("mapDependencies: need HeadFinder");
			}
			ICollection<IDependency<ILabel, ILabel, object>> deps = Generics.NewHashSet();
			foreach (Edu.Stanford.Nlp.Trees.Tree node in this)
			{
				if (node.IsLeaf() || node.Children().Length < 2)
				{
					continue;
				}
				// Label l = node.label();
				// log.info("doing kids of label: " + l);
				//Tree hwt = node.headPreTerminal(hf);
				Edu.Stanford.Nlp.Trees.Tree hwt = node.HeadTerminal(hf);
				// log.info("have hf, found head preterm: " + hwt);
				if (hwt == null)
				{
					throw new InvalidOperationException("mapDependencies: HeadFinder failed!");
				}
				foreach (Edu.Stanford.Nlp.Trees.Tree child in node.Children())
				{
					// Label dl = child.label();
					// Tree dwt = child.headPreTerminal(hf);
					Edu.Stanford.Nlp.Trees.Tree dwt = child.HeadTerminal(hf);
					if (dwt == null)
					{
						throw new InvalidOperationException("mapDependencies: HeadFinder failed!");
					}
					//log.info("kid is " + dl);
					//log.info("transformed to " + dml.toString("value{map}"));
					if (dwt != hwt)
					{
						IDependency<ILabel, ILabel, object> p = new UnnamedDependency(hwt.Label(), dwt.Label());
						if (f.Test(p))
						{
							deps.Add(p);
						}
					}
				}
			}
			return deps;
		}

		/// <summary>
		/// Return a set of Label-Label dependencies, represented as
		/// Dependency objects, for the Tree.
		/// </summary>
		/// <remarks>
		/// Return a set of Label-Label dependencies, represented as
		/// Dependency objects, for the Tree.  The Labels are the ones of the leaf
		/// nodes of the tree, without mucking with them. The head of the sentence is a
		/// dependent of a synthetic "root" label.
		/// </remarks>
		/// <param name="f">
		/// Dependencies are excluded for which the Dependency is not
		/// accepted by the Filter
		/// </param>
		/// <param name="hf">
		/// The HeadFinder to use to identify the head of constituents.
		/// The code assumes
		/// that it can use
		/// <c>headPreTerminal(hf)</c>
		/// to find a
		/// tag and word to make a CoreLabel.
		/// </param>
		/// <param name="rootName">Name of the root node.</param>
		/// <returns>
		/// Set of dependencies (each a
		/// <c>Dependency</c>
		/// between two
		/// <c>CoreLabel</c>
		/// s, which each contain a tag(), word(),
		/// and value(), the last two of which are identical).
		/// </returns>
		public virtual ICollection<IDependency<ILabel, ILabel, object>> MapDependencies(IPredicate<IDependency<ILabel, ILabel, object>> f, IHeadFinder hf, string rootName)
		{
			ICollection<IDependency<ILabel, ILabel, object>> deps = MapDependencies(f, hf);
			if (rootName != null)
			{
				ILabel hl = HeadTerminal(hf).Label();
				CoreLabel rl = new CoreLabel();
				rl.Set(typeof(CoreAnnotations.TextAnnotation), rootName);
				rl.Set(typeof(CoreAnnotations.IndexAnnotation), 0);
				deps.Add(new NamedDependency(rl, hl, rootName));
			}
			return deps;
		}

		/// <summary>Gets the yield of the tree.</summary>
		/// <remarks>
		/// Gets the yield of the tree.  The
		/// <c>Label</c>
		/// of all leaf nodes
		/// is returned
		/// as a list ordered by the natural left to right order of the
		/// leaves.  Null values, if any, are inserted into the list like any
		/// other value.
		/// </remarks>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's leaves.
		/// </returns>
		public virtual List<ILabel> Yield()
		{
			return Yield(new List<ILabel>());
		}

		/// <summary>Gets the yield of the tree.</summary>
		/// <remarks>
		/// Gets the yield of the tree.  The
		/// <c>Label</c>
		/// of all leaf nodes
		/// is returned
		/// as a list ordered by the natural left to right order of the
		/// leaves.  Null values, if any, are inserted into the list like any
		/// other value.
		/// <p><i>Implementation notes:</i> c. 2003: This has been rewritten to thread, so only one List
		/// is used. 2007: This method was duplicated to start to give type safety to Sentence.
		/// This method will now make a Word for any Leaf which does not itself implement HasWord, and
		/// put the Word into the Sentence, so the Sentence elements MUST implement HasWord.
		/// </remarks>
		/// <param name="y">
		/// The list in which the yield of the tree will be placed.
		/// Normally, this will be empty when the routine is called, but
		/// if not, the new yield is added to the end of the list.
		/// </param>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's leaves.
		/// </returns>
		public virtual List<ILabel> Yield(List<ILabel> y)
		{
			if (IsLeaf())
			{
				y.Add(Label());
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					kid.Yield(y);
				}
			}
			return y;
		}

		public virtual List<Word> YieldWords()
		{
			return YieldWords(new List<Word>());
		}

		public virtual List<Word> YieldWords(List<Word> y)
		{
			if (IsLeaf())
			{
				y.Add(new Word(Label()));
			}
			else
			{
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
				{
					kid.YieldWords(y);
				}
			}
			return y;
		}

		public virtual List<X> YieldHasWord<X>()
			where X : IHasWord
		{
			return YieldHasWord(new List<X>());
		}

		public virtual List<X> YieldHasWord<X>(List<X> y)
			where X : IHasWord
		{
			if (IsLeaf())
			{
				ILabel lab = Label();
				// cdm: this is new hacked in stuff in Mar 2007 so we can now have a
				// well-typed version of a Sentence, whose objects MUST implement HasWord
				//
				// wsg (Feb. 2010) - More hacks for trees with CoreLabels in which the type implements
				// HasWord but only the value field is populated. This can happen if legacy code uses
				// LabeledScoredTreeFactory but passes in a StringLabel to e.g. newLeaf().
				if (lab is IHasWord)
				{
					if (lab is CoreLabel)
					{
						CoreLabel cl = (CoreLabel)lab;
						if (cl.Word() == null)
						{
							cl.SetWord(cl.Value());
						}
						y.Add((X)cl);
					}
					else
					{
						y.Add((X)lab);
					}
				}
				else
				{
					y.Add((X)new Word(lab));
				}
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					kid.Yield(y);
				}
			}
			return y;
		}

		/// <summary>Gets the yield of the tree.</summary>
		/// <remarks>
		/// Gets the yield of the tree.  The
		/// <c>Label</c>
		/// of all leaf nodes
		/// is returned
		/// as a list ordered by the natural left to right order of the
		/// leaves.  Null values, if any, are inserted into the list like any
		/// other value.  This has been rewritten to thread, so only one List
		/// is used.
		/// </remarks>
		/// <param name="y">
		/// The list in which the yield of the tree will be placed.
		/// Normally, this will be empty when the routine is called, but
		/// if not, the new yield is added to the end of the list.
		/// </param>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's leaves.
		/// </returns>
		public virtual IList<T> Yield<T>(IList<T> y)
		{
			if (IsLeaf())
			{
				if (Label() is IHasWord)
				{
					IHasWord hw = (IHasWord)Label();
					hw.SetWord(Label().Value());
				}
				y.Add((T)Label());
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					kid.Yield(y);
				}
			}
			return y;
		}

		/// <summary>Gets the tagged yield of the tree.</summary>
		/// <remarks>
		/// Gets the tagged yield of the tree.
		/// The
		/// <c>Label</c>
		/// of all leaf nodes is returned
		/// as a list ordered by the natural left to right order of the
		/// leaves.  Null values, if any, are inserted into the list like any
		/// other value.
		/// </remarks>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's leaves.
		/// </returns>
		public virtual List<TaggedWord> TaggedYield()
		{
			return TaggedYield(new List<TaggedWord>());
		}

		public virtual IList<LabeledWord> LabeledYield()
		{
			return LabeledYield(new List<LabeledWord>());
		}

		/// <summary>
		/// Gets the tagged yield of the tree -- that is, get the preterminals
		/// as well as the terminals.
		/// </summary>
		/// <remarks>
		/// Gets the tagged yield of the tree -- that is, get the preterminals
		/// as well as the terminals.  The
		/// <c>Label</c>
		/// of all leaf nodes
		/// is returned
		/// as a list ordered by the natural left to right order of the
		/// leaves.  Null values, if any, are inserted into the list like any
		/// other value.  This has been rewritten to thread, so only one List
		/// is used.
		/// <p/>
		/// <i>Implementation note:</i> when we summon up enough courage, this
		/// method will be changed to take and return a
		/// <c>List&lt;W extends TaggedWord&gt;</c>
		/// .
		/// </remarks>
		/// <param name="ty">
		/// The list in which the tagged yield of the tree will be
		/// placed. Normally, this will be empty when the routine is called,
		/// but if not, the new yield is added to the end of the list.
		/// </param>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's leaves.
		/// </returns>
		public virtual X TaggedYield<X>(X ty)
			where X : IList<TaggedWord>
		{
			if (IsPreTerminal())
			{
				ty.Add(new TaggedWord(FirstChild().Label(), Label()));
			}
			else
			{
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
				{
					kid.TaggedYield(ty);
				}
			}
			return ty;
		}

		public virtual IList<LabeledWord> LabeledYield(IList<LabeledWord> ty)
		{
			if (IsPreTerminal())
			{
				ty.Add(new LabeledWord(FirstChild().Label(), Label()));
			}
			else
			{
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
				{
					kid.LabeledYield(ty);
				}
			}
			return ty;
		}

		/// <summary>
		/// Returns a
		/// <c>List&lt;CoreLabel&gt;</c>
		/// from the tree.
		/// These are a copy of the complete token representation
		/// that adds the tag as the tag and value.
		/// </summary>
		/// <returns>A tagged, labeled yield.</returns>
		public virtual IList<CoreLabel> TaggedLabeledYield()
		{
			IList<CoreLabel> ty = new List<CoreLabel>();
			TaggedLabeledYield(ty, 0);
			return ty;
		}

		private int TaggedLabeledYield(IList<CoreLabel> ty, int termIdx)
		{
			if (IsPreTerminal())
			{
				// usually this will fill in all the usual keys for a token
				CoreLabel taggedWord = new CoreLabel(FirstChild().Label());
				// but in case this just came from reading a tree that just has a value for words
				if (taggedWord.Word() == null)
				{
					taggedWord.SetWord(FirstChild().Value());
				}
				string tag = (Value() == null) ? string.Empty : Value();
				// set value and tag to the tag
				taggedWord.SetValue(tag);
				taggedWord.SetTag(tag);
				taggedWord.SetIndex(termIdx);
				ty.Add(taggedWord);
				return termIdx + 1;
			}
			else
			{
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in GetChildrenAsList())
				{
					termIdx = kid.TaggedLabeledYield(ty, termIdx);
				}
			}
			return termIdx;
		}

		/// <summary>Gets the preterminal yield (i.e., tags) of the tree.</summary>
		/// <remarks>
		/// Gets the preterminal yield (i.e., tags) of the tree.  All data in
		/// preterminal nodes is returned as a list ordered by the natural left to
		/// right order of the tree.  Null values, if any, are inserted into the
		/// list like any other value.  Pre-leaves are nodes of height 1.
		/// </remarks>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's pre-leaves.
		/// </returns>
		public virtual IList<ILabel> PreTerminalYield()
		{
			return PreTerminalYield(new List<ILabel>());
		}

		/// <summary>Gets the preterminal yield (i.e., tags) of the tree.</summary>
		/// <remarks>
		/// Gets the preterminal yield (i.e., tags) of the tree.  All data in
		/// preleaf nodes is returned as a list ordered by the natural left to
		/// right order of the tree.  Null values, if any, are inserted into the
		/// list like any other value.  Pre-leaves are nodes of height 1.
		/// </remarks>
		/// <param name="y">
		/// The list in which the preterminals of the tree will be
		/// placed. Normally, this will be empty when the routine is called,
		/// but if not, the new yield is added to the end of the list.
		/// </param>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the data in the tree's pre-leaves.
		/// </returns>
		public virtual IList<ILabel> PreTerminalYield(IList<ILabel> y)
		{
			if (IsPreTerminal())
			{
				y.Add(Label());
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					kid.PreTerminalYield(y);
				}
			}
			return y;
		}

		/// <summary>Gets the leaves of the tree.</summary>
		/// <remarks>
		/// Gets the leaves of the tree.  All leaves nodes are returned as a list
		/// ordered by the natural left to right order of the tree.  Null values,
		/// if any, are inserted into the list like any other value.
		/// </remarks>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the leaves.
		/// </returns>
		public virtual IList<T> GetLeaves<T>()
			where T : Edu.Stanford.Nlp.Trees.Tree
		{
			return GetLeaves(new List<T>());
		}

		/// <summary>Gets the leaves of the tree.</summary>
		/// <param name="list">
		/// The list in which the leaves of the tree will be
		/// placed. Normally, this will be empty when the routine is called,
		/// but if not, the new yield is added to the end of the list.
		/// </param>
		/// <returns>
		/// a
		/// <c>List</c>
		/// of the leaves.
		/// </returns>
		public virtual IList<T> GetLeaves<T>(IList<T> list)
			where T : Edu.Stanford.Nlp.Trees.Tree
		{
			if (IsLeaf())
			{
				list.Add((T)this);
			}
			else
			{
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in Children())
				{
					kid.GetLeaves(list);
				}
			}
			return list;
		}

		/// <summary>
		/// Get the set of all node and leaf
		/// <c>Label</c>
		/// s,
		/// null or otherwise, contained in the tree.
		/// </summary>
		/// <returns>
		/// the
		/// <c>Collection</c>
		/// (actually, Set) of all values
		/// in the tree.
		/// </returns>
		public virtual ICollection<ILabel> Labels()
		{
			ICollection<ILabel> n = Generics.NewHashSet();
			n.Add(Label());
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				Sharpen.Collections.AddAll(n, kid.Labels());
			}
			return n;
		}

		public virtual void SetLabels(ICollection<ILabel> c)
		{
			throw new NotSupportedException("Can't set Tree labels");
		}

		/// <summary>Return a flattened version of a tree.</summary>
		/// <remarks>
		/// Return a flattened version of a tree.  In many circumstances, this
		/// will just return the tree, but if the tree is something like a
		/// binarized version of a dependency grammar tree, then it will be
		/// flattened back to a dependency grammar tree representation.  Formally,
		/// a node will be removed from the tree when: it is not a terminal or
		/// preterminal, and its
		/// <c>label()</c>
		/// is
		/// <c>equal()</c>
		/// to
		/// the
		/// <c>label()</c>
		/// of its parent, and all its children will
		/// then be promoted to become children of the parent (in the same
		/// position in the sequence of daughters.
		/// </remarks>
		/// <returns>A flattened version of this tree.</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Flatten()
		{
			return Flatten(TreeFactory());
		}

		/// <summary>Return a flattened version of a tree.</summary>
		/// <remarks>
		/// Return a flattened version of a tree.  In many circumstances, this
		/// will just return the tree, but if the tree is something like a
		/// binarized version of a dependency grammar tree, then it will be
		/// flattened back to a dependency grammar tree representation.  Formally,
		/// a node will be removed from the tree when: it is not a terminal or
		/// preterminal, and its
		/// <c>label()</c>
		/// is
		/// <c>equal()</c>
		/// to
		/// the
		/// <c>label()</c>
		/// of its parent, and all its children will
		/// then be promoted to become children of the parent (in the same
		/// position in the sequence of daughters.
		/// Note: In the current implementation, the tree structure is mainly
		/// duplicated, but the links between preterminals and terminals aren't.
		/// </remarks>
		/// <param name="tf">TreeFactory used to create tree structure for flattened tree</param>
		/// <returns>A flattened version of this tree.</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Flatten(ITreeFactory tf)
		{
			if (IsLeaf() || IsPreTerminal())
			{
				return this;
			}
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			IList<Edu.Stanford.Nlp.Trees.Tree> newChildren = new List<Edu.Stanford.Nlp.Trees.Tree>(kids.Length);
			foreach (Edu.Stanford.Nlp.Trees.Tree child in kids)
			{
				if (child.IsLeaf() || child.IsPreTerminal())
				{
					newChildren.Add(child);
				}
				else
				{
					Edu.Stanford.Nlp.Trees.Tree newChild = child.Flatten(tf);
					if (Label().Equals(newChild.Label()))
					{
						Sharpen.Collections.AddAll(newChildren, newChild.GetChildrenAsList());
					}
					else
					{
						newChildren.Add(newChild);
					}
				}
			}
			return tf.NewTreeNode(Label(), newChildren);
		}

		/// <summary>
		/// Get the set of all subtrees inside the tree by returning a tree
		/// rooted at each node.
		/// </summary>
		/// <remarks>
		/// Get the set of all subtrees inside the tree by returning a tree
		/// rooted at each node.  These are <i>not</i> copies, but all share
		/// structure.  The tree is regarded as a subtree of itself.
		/// <i>Note:</i> If you only want to form this Set so that you can
		/// iterate over it, it is more efficient to simply use the Tree class's
		/// own
		/// <c>iterator()</c>
		/// method. This will iterate over the exact same
		/// elements (but perhaps/probably in a different order).
		/// </remarks>
		/// <returns>
		/// the
		/// <c>Set</c>
		/// of all subtrees in the tree.
		/// </returns>
		public virtual ICollection<Edu.Stanford.Nlp.Trees.Tree> SubTrees()
		{
			return SubTrees(Generics.NewHashSet());
		}

		/// <summary>
		/// Get the list of all subtrees inside the tree by returning a tree
		/// rooted at each node.
		/// </summary>
		/// <remarks>
		/// Get the list of all subtrees inside the tree by returning a tree
		/// rooted at each node.  These are <i>not</i> copies, but all share
		/// structure.  The tree is regarded as a subtree of itself.
		/// <i>Note:</i> If you only want to form this Collection so that you can
		/// iterate over it, it is more efficient to simply use the Tree class's
		/// own
		/// <c>iterator()</c>
		/// method. This will iterate over the exact same
		/// elements (but perhaps/probably in a different order).
		/// </remarks>
		/// <returns>
		/// the
		/// <c>List</c>
		/// of all subtrees in the tree.
		/// </returns>
		public virtual IList<Edu.Stanford.Nlp.Trees.Tree> SubTreeList()
		{
			return SubTrees(new List<Edu.Stanford.Nlp.Trees.Tree>());
		}

		/// <summary>
		/// Add the set of all subtrees inside a tree (including the tree itself)
		/// to the given
		/// <c>Collection</c>
		/// .
		/// <i>Note:</i> If you only want to form this Collection so that you can
		/// iterate over it, it is more efficient to simply use the Tree class's
		/// own
		/// <c>iterator()</c>
		/// method. This will iterate over the exact same
		/// elements (but perhaps/probably in a different order).
		/// </summary>
		/// <param name="n">A collection of nodes to which the subtrees will be added.</param>
		/// <returns>The collection parameter with the subtrees added.</returns>
		public virtual T SubTrees<T>(T n)
			where T : ICollection<Edu.Stanford.Nlp.Trees.Tree>
		{
			n.Add(this);
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				kid.SubTrees(n);
			}
			return n;
		}

		/// <summary>Makes a deep copy of not only the Tree structure but of the labels as well.</summary>
		/// <remarks>
		/// Makes a deep copy of not only the Tree structure but of the labels as well.
		/// Uses the TreeFactory of the root node given by treeFactory().
		/// Assumes that your labels give a non-null labelFactory().
		/// (Added by Aria Haghighi.)
		/// </remarks>
		/// <returns>A deep copy of the tree structure and its labels</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree DeepCopy()
		{
			return DeepCopy(TreeFactory());
		}

		/// <summary>Makes a deep copy of not only the Tree structure but of the labels as well.</summary>
		/// <remarks>
		/// Makes a deep copy of not only the Tree structure but of the labels as well.
		/// The new tree will have nodes made by the given TreeFactory.
		/// Each Label is copied using the labelFactory() returned
		/// by the corresponding node's label.
		/// It assumes that your labels give non-null labelFactory.
		/// (Added by Aria Haghighi.)
		/// </remarks>
		/// <param name="tf">
		/// The TreeFactory used to make all nodes in the copied
		/// tree structure
		/// </param>
		/// <returns>
		/// A Tree that is a deep copy of the tree structure and
		/// Labels of the original tree.
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree DeepCopy(ITreeFactory tf)
		{
			return DeepCopy(tf, Label().LabelFactory());
		}

		/// <summary>Makes a deep copy of not only the Tree structure but of the labels as well.</summary>
		/// <remarks>
		/// Makes a deep copy of not only the Tree structure but of the labels as well.
		/// Each tree is copied with the given TreeFactory.
		/// Each Label is copied using the given LabelFactory.
		/// That is, the tree and label factories can transform the nature of the
		/// data representation.
		/// </remarks>
		/// <param name="tf">
		/// The TreeFactory used to make all nodes in the copied
		/// tree structure
		/// </param>
		/// <param name="lf">
		/// The LabelFactory used to make all nodes in the copied
		/// tree structure
		/// </param>
		/// <returns>
		/// A Tree that is a deep copy of the tree structure and
		/// Labels of the original tree.
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree DeepCopy(ITreeFactory tf, ILabelFactory lf)
		{
			ILabel label = lf.NewLabel(Label());
			if (IsLeaf())
			{
				return tf.NewLeaf(label);
			}
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			// NB: The below list may not be of type Tree but TreeGraphNode, so we leave it untyped
			IList newKids = new ArrayList(kids.Length);
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				newKids.Add(kid.DeepCopy(tf, lf));
			}
			return tf.NewTreeNode(label, newKids);
		}

		/// <summary>Create a deep copy of the tree structure.</summary>
		/// <remarks>
		/// Create a deep copy of the tree structure.  The entire structure is
		/// recursively copied, but label data themselves are not cloned.
		/// The copy is built using a
		/// <c>TreeFactory</c>
		/// that will
		/// produce a
		/// <c>Tree</c>
		/// like the input one.
		/// </remarks>
		/// <returns>A deep copy of the tree structure (but not its labels).</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree TreeSkeletonCopy()
		{
			return TreeSkeletonCopy(TreeFactory());
		}

		/// <summary>Create a deep copy of the tree structure.</summary>
		/// <remarks>
		/// Create a deep copy of the tree structure.  The entire structure is
		/// recursively copied, but label data themselves are not cloned.
		/// By specifying an appropriate
		/// <c>TreeFactory</c>
		/// , this
		/// method can be used to change the type of a
		/// <c>Tree</c>
		/// .
		/// </remarks>
		/// <param name="tf">
		/// The
		/// <c>TreeFactory</c>
		/// to be used for creating
		/// the returned
		/// <c>Tree</c>
		/// </param>
		/// <returns>A deep copy of the tree structure (but not its labels).</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree TreeSkeletonCopy(ITreeFactory tf)
		{
			Edu.Stanford.Nlp.Trees.Tree t;
			if (IsLeaf())
			{
				t = tf.NewLeaf(Label());
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				IList<Edu.Stanford.Nlp.Trees.Tree> newKids = new List<Edu.Stanford.Nlp.Trees.Tree>(kids.Length);
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					newKids.Add(kid.TreeSkeletonCopy(tf));
				}
				t = tf.NewTreeNode(Label(), newKids);
			}
			return t;
		}

		/// <summary>Returns a deep copy of everything but the leaf labels.</summary>
		/// <remarks>
		/// Returns a deep copy of everything but the leaf labels.  The leaf
		/// labels are reused from the original tree.  This is useful for
		/// cases such as the dependency converter, which wants to finish
		/// with the same labels in the dependencies as the parse tree.
		/// </remarks>
		public virtual Edu.Stanford.Nlp.Trees.Tree TreeSkeletonConstituentCopy()
		{
			return TreeSkeletonConstituentCopy(TreeFactory(), Label().LabelFactory());
		}

		public virtual Edu.Stanford.Nlp.Trees.Tree TreeSkeletonConstituentCopy(ITreeFactory tf, ILabelFactory lf)
		{
			if (IsLeaf())
			{
				// Reuse the current label for a leaf.  This way, trees which
				// are based on tokens in a sentence can have the same tokens
				// even after a "deep copy".
				// TODO: the LabeledScoredTreeFactory copies the label for a new
				// leaf.  Perhaps we could add a newLeafNoCopy or something like
				// that for efficiency.
				Edu.Stanford.Nlp.Trees.Tree newLeaf = tf.NewLeaf(Label());
				newLeaf.SetLabel(Label());
				return newLeaf;
			}
			ILabel label = lf.NewLabel(Label());
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			IList<Edu.Stanford.Nlp.Trees.Tree> newKids = new List<Edu.Stanford.Nlp.Trees.Tree>(kids.Length);
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				newKids.Add(kid.TreeSkeletonConstituentCopy(tf, lf));
			}
			return tf.NewTreeNode(label, newKids);
		}

		/// <summary>Create a transformed Tree.</summary>
		/// <remarks>
		/// Create a transformed Tree.  The tree is traversed in a depth-first,
		/// left-to-right order, and the
		/// <c>TreeTransformer</c>
		/// is called
		/// on each node.  It returns some
		/// <c>Tree</c>
		/// .  The transformed
		/// tree has a new tree structure (i.e., a "deep copy" is done), but it
		/// will usually share its labels with the original tree.
		/// </remarks>
		/// <param name="transformer">The function that transforms tree nodes or subtrees</param>
		/// <returns>
		/// a transformation of this
		/// <c>Tree</c>
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Transform(ITreeTransformer transformer)
		{
			return Transform(transformer, TreeFactory());
		}

		/// <summary>Create a transformed Tree.</summary>
		/// <remarks>
		/// Create a transformed Tree.  The tree is traversed in a depth-first,
		/// left-to-right order, and the
		/// <c>TreeTransformer</c>
		/// is called
		/// on each node.  It returns some
		/// <c>Tree</c>
		/// .  The transformed
		/// tree has a new tree structure (i.e., a deep copy of the structure of the tree is done), but it
		/// will usually share its labels with the original tree.
		/// </remarks>
		/// <param name="transformer">The function that transforms tree nodes or subtrees</param>
		/// <param name="tf">
		/// The
		/// <c>TreeFactory</c>
		/// which will be used for creating
		/// new nodes for the returned
		/// <c>Tree</c>
		/// </param>
		/// <returns>
		/// a transformation of this
		/// <c>Tree</c>
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Transform(ITreeTransformer transformer, ITreeFactory tf)
		{
			Edu.Stanford.Nlp.Trees.Tree t;
			if (IsLeaf())
			{
				t = tf.NewLeaf(Label());
			}
			else
			{
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				IList<Edu.Stanford.Nlp.Trees.Tree> newKids = new List<Edu.Stanford.Nlp.Trees.Tree>(kids.Length);
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					newKids.Add(kid.Transform(transformer, tf));
				}
				t = tf.NewTreeNode(Label(), newKids);
			}
			return transformer.TransformTree(t);
		}

		/// <summary>
		/// Creates a (partial) deep copy of the tree, where all nodes that the
		/// filter does not accept are spliced out.
		/// </summary>
		/// <remarks>
		/// Creates a (partial) deep copy of the tree, where all nodes that the
		/// filter does not accept are spliced out.  If the result is not a tree
		/// (that is, it's a forest), an empty root node is generated.
		/// </remarks>
		/// <param name="nodeFilter">
		/// a Filter method which returns true to mean
		/// keep this node, false to mean delete it
		/// </param>
		/// <returns>a filtered copy of the tree</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree SpliceOut(IPredicate<Edu.Stanford.Nlp.Trees.Tree> nodeFilter)
		{
			return SpliceOut(nodeFilter, TreeFactory());
		}

		/// <summary>
		/// Creates a (partial) deep copy of the tree, where all nodes that the
		/// filter does not accept are spliced out.
		/// </summary>
		/// <remarks>
		/// Creates a (partial) deep copy of the tree, where all nodes that the
		/// filter does not accept are spliced out.  That is, the particular
		/// modes for which the
		/// <c>Filter</c>
		/// returns
		/// <see langword="false"/>
		/// are removed from the
		/// <c>Tree</c>
		/// , but those nodes' children
		/// are kept (assuming they pass the
		/// <c>Filter</c>
		/// , and they are
		/// added in the appropriate left-to-right ordering as new children of
		/// the parent node.  If the root node is deleted, so that the result
		/// would not be a tree (that is, it's a forest), an empty root node is
		/// generated.  If nothing is accepted,
		/// <see langword="null"/>
		/// is returned.
		/// </remarks>
		/// <param name="nodeFilter">
		/// a Filter method which returns true to mean
		/// keep this node, false to mean delete it
		/// </param>
		/// <param name="tf">
		/// A
		/// <c>TreeFactory</c>
		/// for making new trees. Used if
		/// the root node is deleted.
		/// </param>
		/// <returns>a filtered copy of the tree.</returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree SpliceOut(IPredicate<Edu.Stanford.Nlp.Trees.Tree> nodeFilter, ITreeFactory tf)
		{
			IList<Edu.Stanford.Nlp.Trees.Tree> l = SpliceOutHelper(nodeFilter, tf);
			if (l.IsEmpty())
			{
				return null;
			}
			else
			{
				if (l.Count == 1)
				{
					return l[0];
				}
			}
			// for a forest, make a new root
			return tf.NewTreeNode((ILabel)null, l);
		}

		private IList<Edu.Stanford.Nlp.Trees.Tree> SpliceOutHelper(IPredicate<Edu.Stanford.Nlp.Trees.Tree> nodeFilter, ITreeFactory tf)
		{
			// recurse over all children first
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			IList<Edu.Stanford.Nlp.Trees.Tree> l = new List<Edu.Stanford.Nlp.Trees.Tree>();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				Sharpen.Collections.AddAll(l, kid.SpliceOutHelper(nodeFilter, tf));
			}
			// check if this node is being spliced out
			if (nodeFilter.Test(this))
			{
				// no, so add our children and return
				Edu.Stanford.Nlp.Trees.Tree t;
				if (!l.IsEmpty())
				{
					t = tf.NewTreeNode(Label(), l);
				}
				else
				{
					t = tf.NewLeaf(Label());
				}
				l = new List<Edu.Stanford.Nlp.Trees.Tree>(1);
				l.Add(t);
				return l;
			}
			// we're out, so return our children
			return l;
		}

		/// <summary>
		/// Creates a deep copy of the tree, where all nodes that the filter
		/// does not accept and all children of such nodes are pruned.
		/// </summary>
		/// <remarks>
		/// Creates a deep copy of the tree, where all nodes that the filter
		/// does not accept and all children of such nodes are pruned.  If all
		/// of a node's children are pruned, that node is cut as well.
		/// A
		/// <c>Filter</c>
		/// can assume
		/// that it will not be called with a
		/// <see langword="null"/>
		/// argument.
		/// <p/>
		/// For example, the following code excises all PP nodes from a Tree: <br />
		/// <tt>
		/// Filter<Tree> f = new Filter<Tree> { <br />
		/// public boolean accept(Tree t) { <br />
		/// return ! t.label().value().equals("PP"); <br />
		/// } <br />
		/// }; <br />
		/// tree.prune(f);
		/// </tt> <br />
		/// If the root of the tree is pruned, null will be returned.
		/// </remarks>
		/// <param name="filter">the filter to be applied</param>
		/// <returns>
		/// a filtered copy of the tree, including the possibility of
		/// <see langword="null"/>
		/// if the root node of the tree is filtered
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Prune(IPredicate<Edu.Stanford.Nlp.Trees.Tree> filter)
		{
			return Prune(filter, TreeFactory());
		}

		/// <summary>
		/// Creates a deep copy of the tree, where all nodes that the filter
		/// does not accept and all children of such nodes are pruned.
		/// </summary>
		/// <remarks>
		/// Creates a deep copy of the tree, where all nodes that the filter
		/// does not accept and all children of such nodes are pruned.  If all
		/// of a node's children are pruned, that node is cut as well.
		/// A
		/// <c>Filter</c>
		/// can assume
		/// that it will not be called with a
		/// <see langword="null"/>
		/// argument.
		/// </remarks>
		/// <param name="filter">the filter to be applied</param>
		/// <param name="tf">the TreeFactory to be used to make new Tree nodes if needed</param>
		/// <returns>
		/// a filtered copy of the tree, including the possibility of
		/// <see langword="null"/>
		/// if the root node of the tree is filtered
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Prune(IPredicate<Edu.Stanford.Nlp.Trees.Tree> filter, ITreeFactory tf)
		{
			// is the current node to be pruned?
			if (!filter.Test(this))
			{
				return null;
			}
			// if not, recurse over all children
			IList<Edu.Stanford.Nlp.Trees.Tree> l = new List<Edu.Stanford.Nlp.Trees.Tree>();
			Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				Edu.Stanford.Nlp.Trees.Tree prunedChild = kid.Prune(filter, tf);
				if (prunedChild != null)
				{
					l.Add(prunedChild);
				}
			}
			// and check if this node has lost all its children
			if (l.IsEmpty() && !(kids.Length == 0))
			{
				return null;
			}
			// if we're still ok, copy the node
			if (IsLeaf())
			{
				return tf.NewLeaf(Label());
			}
			return tf.NewTreeNode(Label(), l);
		}

		/// <summary>
		/// Returns first child if this is unary and if the label at the current
		/// node is either "ROOT" or empty.
		/// </summary>
		/// <returns>
		/// The first child if this is unary and if the label at the current
		/// node is either "ROOT" or empty, else this
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree SkipRoot()
		{
			if (!IsUnaryRewrite())
			{
				return this;
			}
			string lab = Label().Value();
			return (lab == null || lab.IsEmpty() || "ROOT".Equals(lab)) ? FirstChild() : this;
		}

		/// <summary>
		/// Return a
		/// <c>TreeFactory</c>
		/// that produces trees of the
		/// appropriate type.
		/// </summary>
		/// <returns>A factory to produce Trees</returns>
		public abstract ITreeFactory TreeFactory();

		/// <summary>Return the parent of the tree node.</summary>
		/// <remarks>
		/// Return the parent of the tree node.  This routine may return
		/// <see langword="null"/>
		/// meaning simply that the implementation doesn't
		/// know how to determine the parent node, rather than there is no
		/// such node.
		/// </remarks>
		/// <returns>
		/// The parent
		/// <c>Tree</c>
		/// node or
		/// <see langword="null"/>
		/// </returns>
		/// <seealso cref="Parent(Tree)"/>
		public virtual Edu.Stanford.Nlp.Trees.Tree Parent()
		{
			throw new NotSupportedException();
		}

		/// <summary>Return the parent of the tree node.</summary>
		/// <remarks>
		/// Return the parent of the tree node.  This routine will traverse
		/// a tree (depth first) from the given
		/// <paramref name="root"/>
		/// , and will
		/// correctly find the parent, regardless of whether the concrete
		/// class stores parents.  It will only return
		/// <see langword="null"/>
		/// if this
		/// node is the
		/// <paramref name="root"/>
		/// node, or if this node is not
		/// contained within the tree rooted at
		/// <paramref name="root"/>
		/// .
		/// </remarks>
		/// <param name="root">The root node of the whole Tree</param>
		/// <returns>
		/// the parent
		/// <c>Tree</c>
		/// node if any;
		/// else
		/// <see langword="null"/>
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Parent(Edu.Stanford.Nlp.Trees.Tree root)
		{
			Edu.Stanford.Nlp.Trees.Tree[] kids = root.Children();
			return ParentHelper(root, kids, this);
		}

		private static Edu.Stanford.Nlp.Trees.Tree ParentHelper(Edu.Stanford.Nlp.Trees.Tree parent, Edu.Stanford.Nlp.Trees.Tree[] kids, Edu.Stanford.Nlp.Trees.Tree node)
		{
			foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
			{
				if (kid == node)
				{
					return parent;
				}
				Edu.Stanford.Nlp.Trees.Tree ret = node.Parent(kid);
				if (ret != null)
				{
					return ret;
				}
			}
			return null;
		}

		/// <summary>Returns the number of nodes the tree contains.</summary>
		/// <remarks>
		/// Returns the number of nodes the tree contains.  This method
		/// implements the
		/// <c>size()</c>
		/// function required by the
		/// <c>Collections</c>
		/// interface.  The size of the tree is the
		/// number of nodes it contains (of all types, including the leaf nodes
		/// and the root).
		/// </remarks>
		/// <returns>The size of the tree</returns>
		/// <seealso cref="Depth()"/>
		public override int Count
		{
			get
			{
				int size = 1;
				Edu.Stanford.Nlp.Trees.Tree[] kids = Children();
				foreach (Edu.Stanford.Nlp.Trees.Tree kid in kids)
				{
					size += kid.Count;
				}
				return size;
			}
		}

		/// <summary>
		/// Return the ancestor tree node
		/// <paramref name="height"/>
		/// nodes up from the current node.
		/// </summary>
		/// <param name="height">
		/// How many nodes up to go. A parameter of 0 means return
		/// this node, 1 means to return the parent node and so on.
		/// </param>
		/// <param name="root">The root node that this Tree is embedded under</param>
		/// <returns>
		/// The ancestor at height
		/// <paramref name="height"/>
		/// .  It returns null
		/// if it does not exist or the tree implementation does not keep track
		/// of parents
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Tree Ancestor(int height, Edu.Stanford.Nlp.Trees.Tree root)
		{
			if (height < 0)
			{
				throw new ArgumentException("ancestor: height cannot be negative");
			}
			if (height == 0)
			{
				return this;
			}
			Edu.Stanford.Nlp.Trees.Tree par = Parent(root);
			if (par == null)
			{
				return null;
			}
			return par.Ancestor(height - 1, root);
		}

		private class TreeIterator : IEnumerator<Tree>
		{
			private readonly IList<Tree> treeStack;

			protected internal TreeIterator(Tree t)
			{
				treeStack = new List<Tree>();
				treeStack.Add(t);
			}

			public virtual bool MoveNext()
			{
				return (!treeStack.IsEmpty());
			}

			public virtual Tree Current
			{
				get
				{
					int lastIndex = treeStack.Count - 1;
					if (lastIndex < 0)
					{
						throw new NoSuchElementException("TreeIterator exhausted");
					}
					Tree tr = treeStack.Remove(lastIndex);
					Tree[] kids = tr.Children();
					// so that we can efficiently use one List, we reverse them
					for (int i = kids.Length - 1; i >= 0; i--)
					{
						treeStack.Add(kids[i]);
					}
					return tr;
				}
			}

			/// <summary>Not supported</summary>
			public virtual void Remove()
			{
				throw new NotSupportedException();
			}

			public override string ToString()
			{
				return "TreeIterator";
			}
		}

		/// <summary>Returns an iterator over all the nodes of the tree.</summary>
		/// <remarks>
		/// Returns an iterator over all the nodes of the tree.  This method
		/// implements the
		/// <c>iterator()</c>
		/// method required by the
		/// <c>Collections</c>
		/// interface.  It does a preorder
		/// (children after node) traversal of the tree.  (A possible
		/// extension to the class at some point would be to allow different
		/// traversal orderings via variant iterators.)
		/// </remarks>
		/// <returns>An iterator over the nodes of the tree</returns>
		public override IEnumerator<Tree> GetEnumerator()
		{
			return new Tree.TreeIterator(this);
		}

		public virtual IList<Tree> PostOrderNodeList()
		{
			IList<Tree> nodes = new List<Tree>();
			PostOrderRecurse(this, nodes);
			return nodes;
		}

		private static void PostOrderRecurse(Tree t, IList<Tree> nodes)
		{
			foreach (Tree c in t.Children())
			{
				PostOrderRecurse(c, nodes);
			}
			nodes.Add(t);
		}

		public virtual IList<Tree> PreOrderNodeList()
		{
			IList<Tree> nodes = new List<Tree>();
			PreOrderRecurse(this, nodes);
			return nodes;
		}

		private static void PreOrderRecurse(Tree t, IList<Tree> nodes)
		{
			nodes.Add(t);
			foreach (Tree c in t.Children())
			{
				PreOrderRecurse(c, nodes);
			}
		}

		/// <summary>
		/// This gives you a tree from a String representation (as a
		/// bracketed Tree, of the kind produced by
		/// <c>toString()</c>
		/// ,
		/// <c>pennPrint()</c>
		/// , or as in the Penn Treebank).
		/// It's not the most efficient thing to do for heavy duty usage.
		/// The Tree returned is created by a
		/// LabeledScoredTreeReaderFactory. This means that "standard"
		/// normalizations (stripping functional categories, indices,
		/// empty nodes, and A-over-A nodes) will be done on it.
		/// </summary>
		/// <param name="str">The tree as a bracketed list in a String.</param>
		/// <returns>The Tree</returns>
		/// <exception cref="System.Exception">If Tree format is not valid</exception>
		public static Tree ValueOf(string str)
		{
			return ValueOf(str, new LabeledScoredTreeReaderFactory());
		}

		/// <summary>
		/// This gives you a tree from a String representation (as a
		/// bracketed Tree, of the kind produced by
		/// <c>toString()</c>
		/// ,
		/// <c>pennPrint()</c>
		/// , or as in the Penn Treebank.
		/// It's not the most efficient thing to do for heavy duty usage.
		/// </summary>
		/// <param name="str">The tree as a bracketed list in a String.</param>
		/// <param name="trf">The TreeFactory used to make the new Tree</param>
		/// <returns>The Tree</returns>
		/// <exception cref="System.Exception">If the Tree format is not valid</exception>
		public static Tree ValueOf(string str, ITreeReaderFactory trf)
		{
			try
			{
				return trf.NewTreeReader(new StringReader(str)).ReadTree();
			}
			catch (IOException ioe)
			{
				throw new Exception("Tree.valueOf() tree construction failed", ioe);
			}
		}

		/// <summary>Return the child at some daughter index.</summary>
		/// <remarks>
		/// Return the child at some daughter index.  The children are numbered
		/// starting with an index of 0.
		/// </remarks>
		/// <param name="i">The daughter index</param>
		/// <returns>The tree at that daughter index</returns>
		public virtual Tree GetChild(int i)
		{
			Tree[] kids = Children();
			return kids[i];
		}

		/// <summary>Destructively removes the child at some daughter index and returns it.</summary>
		/// <remarks>
		/// Destructively removes the child at some daughter index and returns it.
		/// Note
		/// that this method will throw an
		/// <see cref="System.IndexOutOfRangeException"/>
		/// if
		/// the daughter index is too big for the list of daughters.
		/// </remarks>
		/// <param name="i">The daughter index</param>
		/// <returns>The tree at that daughter index</returns>
		public virtual Tree RemoveChild(int i)
		{
			Tree[] kids = Children();
			Tree kid = kids[i];
			Tree[] newKids = new Tree[kids.Length - 1];
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
			SetChildren(newKids);
			return kid;
		}

		/// <summary>Adds the tree t at the index position among the daughters.</summary>
		/// <remarks>
		/// Adds the tree t at the index position among the daughters.  Note
		/// that this method will throw an
		/// <see cref="System.IndexOutOfRangeException"/>
		/// if
		/// the daughter index is too big for the list of daughters.
		/// </remarks>
		/// <param name="i">the index position at which to add the new daughter</param>
		/// <param name="t">the new daughter</param>
		public virtual void AddChild(int i, Tree t)
		{
			Tree[] kids = Children();
			Tree[] newKids = new Tree[kids.Length + 1];
			if (i != 0)
			{
				System.Array.Copy(kids, 0, newKids, 0, i);
			}
			newKids[i] = t;
			if (i != kids.Length)
			{
				System.Array.Copy(kids, i, newKids, i + 1, kids.Length - i);
			}
			SetChildren(newKids);
		}

		/// <summary>Adds the tree t at the last index position among the daughters.</summary>
		/// <param name="t">the new daughter</param>
		public virtual void AddChild(Tree t)
		{
			AddChild(Children().Length, t);
		}

		/// <summary>
		/// Replaces the
		/// <paramref name="i"/>
		/// th child of
		/// <c>this</c>
		/// with the tree t.
		/// Note
		/// that this method will throw an
		/// <see cref="System.IndexOutOfRangeException"/>
		/// if
		/// the child index is too big for the list of children.
		/// </summary>
		/// <param name="i">The index position at which to replace the child</param>
		/// <param name="t">The new child</param>
		/// <returns>The tree that was previously the ith d</returns>
		public virtual Tree SetChild(int i, Tree t)
		{
			Tree[] kids = Children();
			Tree old = kids[i];
			kids[i] = t;
			return old;
		}

		/// <summary>
		/// Returns true if
		/// <c>this</c>
		/// dominates the Tree passed in
		/// as an argument.  Object equality (==) rather than .equals() is used
		/// to determine domination.
		/// t.dominates(t) returns false.
		/// </summary>
		public virtual bool Dominates(Tree t)
		{
			IList<Tree> dominationPath = DominationPath(t);
			return dominationPath != null && dominationPath.Count > 1;
		}

		/// <summary>
		/// Returns the path of nodes leading down to a dominated node,
		/// including
		/// <c>this</c>
		/// and the dominated node itself.
		/// Returns null if t is not dominated by
		/// <c>this</c>
		/// .  Object
		/// equality (==) is the relevant criterion.
		/// t.dominationPath(t) returns null.
		/// </summary>
		public virtual IList<Tree> DominationPath(Tree t)
		{
			//Tree[] result = dominationPathHelper(t, 0);
			Tree[] result = DominationPath(t, 0);
			if (result == null)
			{
				return null;
			}
			return Arrays.AsList(result);
		}

		private Tree[] DominationPathHelper(Tree t, int depth)
		{
			Tree[] kids = Children();
			for (int i = kids.Length - 1; i >= 0; i--)
			{
				Tree t1 = kids[i];
				if (t1 == null)
				{
					return null;
				}
				Tree[] result;
				if ((result = t1.DominationPath(t, depth + 1)) != null)
				{
					result[depth] = this;
					return result;
				}
			}
			return null;
		}

		private Tree[] DominationPath(Tree t, int depth)
		{
			if (this == t)
			{
				Tree[] result = new Tree[depth + 1];
				result[depth] = this;
				return result;
			}
			return DominationPathHelper(t, depth);
		}

		/// <summary>
		/// Given nodes
		/// <paramref name="t1"/>
		/// and
		/// <paramref name="t2"/>
		/// which are
		/// dominated by this node, returns a list of all the nodes on the
		/// path from t1 to t2, inclusive, or null if none found.
		/// </summary>
		public virtual IList<Tree> PathNodeToNode(Tree t1, Tree t2)
		{
			if (!Contains(t1) || !Contains(t2))
			{
				return null;
			}
			if (t1 == t2)
			{
				return Java.Util.Collections.SingletonList(t1);
			}
			if (t1.Dominates(t2))
			{
				return t1.DominationPath(t2);
			}
			if (t2.Dominates(t1))
			{
				IList<Tree> path = t2.DominationPath(t1);
				Java.Util.Collections.Reverse(path);
				return path;
			}
			Tree joinNode = JoinNode(t1, t2);
			if (joinNode == null)
			{
				return null;
			}
			IList<Tree> t1DomPath = joinNode.DominationPath(t1);
			IList<Tree> t2DomPath = joinNode.DominationPath(t2);
			if (t1DomPath == null || t2DomPath == null)
			{
				return null;
			}
			List<Tree> path_1 = new List<Tree>(t1DomPath);
			Java.Util.Collections.Reverse(path_1);
			path_1.Remove(joinNode);
			Sharpen.Collections.AddAll(path_1, t2DomPath);
			return path_1;
		}

		/// <summary>
		/// Given nodes
		/// <paramref name="t1"/>
		/// and
		/// <paramref name="t2"/>
		/// which are
		/// dominated by this node, returns their "join node": the node
		/// <c>j</c>
		/// such that
		/// <c>j</c>
		/// dominates both
		/// <paramref name="t1"/>
		/// and
		/// <paramref name="t2"/>
		/// , and every other node which
		/// dominates both
		/// <paramref name="t1"/>
		/// and
		/// <paramref name="t2"/>
		/// dominates
		/// <c>j</c>
		/// .
		/// In the special case that t1 dominates t2, return t1, and vice versa.
		/// Return
		/// <see langword="null"/>
		/// if no such node can be found.
		/// </summary>
		public virtual Tree JoinNode(Tree t1, Tree t2)
		{
			if (!Contains(t1) || !Contains(t2))
			{
				return null;
			}
			if (this == t1 || this == t2)
			{
				return this;
			}
			Tree joinNode = null;
			IList<Tree> t1DomPath = DominationPath(t1);
			IList<Tree> t2DomPath = DominationPath(t2);
			if (t1DomPath == null || t2DomPath == null)
			{
				return null;
			}
			IEnumerator<Tree> it1 = t1DomPath.GetEnumerator();
			IEnumerator<Tree> it2 = t2DomPath.GetEnumerator();
			while (it1.MoveNext() && it2.MoveNext())
			{
				Tree n1 = it1.Current;
				Tree n2 = it2.Current;
				if (n1 != n2)
				{
					break;
				}
				joinNode = n1;
			}
			return joinNode;
		}

		/// <summary>
		/// Given nodes
		/// <paramref name="t1"/>
		/// and
		/// <paramref name="t2"/>
		/// which are
		/// dominated by this node, returns
		/// <see langword="true"/>
		/// iff
		/// <paramref name="t1"/>
		/// c-commands
		/// <paramref name="t2"/>
		/// .  (A node c-commands
		/// its sister(s) and any nodes below its sister(s).)
		/// </summary>
		public virtual bool CCommands(Tree t1, Tree t2)
		{
			IList<Tree> sibs = t1.Siblings(this);
			if (sibs == null)
			{
				return false;
			}
			foreach (Tree sib in sibs)
			{
				if (sib == t2 || sib.Contains(t2))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Returns the siblings of this Tree node.</summary>
		/// <remarks>
		/// Returns the siblings of this Tree node.  The siblings are all
		/// children of the parent of this node except this node.
		/// </remarks>
		/// <param name="root">The root within which this tree node is contained</param>
		/// <returns>
		/// The siblings as a list, an empty list if there are no siblings.
		/// The returned list is a modifiable new list structure, but contains
		/// the actual children.
		/// </returns>
		public virtual IList<Tree> Siblings(Tree root)
		{
			Tree parent = Parent(root);
			if (parent == null)
			{
				return null;
			}
			IList<Tree> siblings = parent.GetChildrenAsList();
			siblings.Remove(this);
			return siblings;
		}

		/// <summary>
		/// insert
		/// <paramref name="dtr"/>
		/// after
		/// <paramref name="position"/>
		/// existing
		/// daughters in
		/// <c>this</c>
		/// .
		/// </summary>
		public virtual void InsertDtr(Tree dtr, int position)
		{
			Tree[] kids = Children();
			if (position > kids.Length)
			{
				throw new ArgumentException("Can't insert tree after the " + position + "th daughter in " + this + "; only " + kids.Length + " daughters exist!");
			}
			Tree[] newKids = new Tree[kids.Length + 1];
			int i = 0;
			for (; i < position; i++)
			{
				newKids[i] = kids[i];
			}
			newKids[i] = dtr;
			for (; i < kids.Length; i++)
			{
				newKids[i + 1] = kids[i];
			}
			SetChildren(newKids);
		}

		// --- composition methods to implement Label interface
		public virtual string Value()
		{
			ILabel lab = Label();
			if (lab == null)
			{
				return null;
			}
			return lab.Value();
		}

		public virtual void SetValue(string value)
		{
			ILabel lab = Label();
			if (lab != null)
			{
				lab.SetValue(value);
			}
		}

		public virtual void SetFromString(string labelStr)
		{
			ILabel lab = Label();
			if (lab != null)
			{
				lab.SetFromString(labelStr);
			}
		}

		/// <summary>Returns a factory that makes labels of the same type as this one.</summary>
		/// <remarks>
		/// Returns a factory that makes labels of the same type as this one.
		/// May return
		/// <see langword="null"/>
		/// if no appropriate factory is known.
		/// </remarks>
		/// <returns>the LabelFactory for this kind of label</returns>
		public virtual ILabelFactory LabelFactory()
		{
			ILabel lab = Label();
			if (lab == null)
			{
				return null;
			}
			return lab.LabelFactory();
		}

		/// <summary>
		/// Returns the positional index of the left edge of  <i>node</i> within the tree,
		/// as measured by characters.
		/// </summary>
		/// <remarks>
		/// Returns the positional index of the left edge of  <i>node</i> within the tree,
		/// as measured by characters.  Returns -1 if <i>node is not found.</i>
		/// Note: These methods were written for internal evaluation routines. They are
		/// not the right methods to relate tree nodes to textual offsets. For these,
		/// look at the appropriate annotations on a CoreLabel (CharacterOffsetBeginAnnotation, etc.).
		/// </remarks>
		public virtual int LeftCharEdge(Tree node)
		{
			MutableInteger i = new MutableInteger(0);
			if (LeftCharEdge(node, i))
			{
				return i;
			}
			return -1;
		}

		private bool LeftCharEdge(Tree node, MutableInteger i)
		{
			if (this == node)
			{
				return true;
			}
			else
			{
				if (IsLeaf())
				{
					i.Set(i + Value().Length);
					return false;
				}
				else
				{
					foreach (Tree child in Children())
					{
						if (child.LeftCharEdge(node, i))
						{
							return true;
						}
					}
					return false;
				}
			}
		}

		/// <summary>
		/// Returns the positional index of the right edge of  <i>node</i> within the tree,
		/// as measured by characters.
		/// </summary>
		/// <remarks>
		/// Returns the positional index of the right edge of  <i>node</i> within the tree,
		/// as measured by characters. Returns -1 if <i>node is not found.</i>
		/// rightCharEdge returns the index of the rightmost character + 1, so that
		/// rightCharEdge(getLeaves().get(i)) == leftCharEdge(getLeaves().get(i+1))
		/// Note: These methods were written for internal evaluation routines. They are
		/// not the right methods to relate tree nodes to textual offsets. For these,
		/// look at the appropriate annotations on a CoreLabel (CharacterOffsetBeginAnnotation, etc.).
		/// </remarks>
		/// <param name="node">The subtree to look for in this Tree</param>
		/// <returns>The positional index of the right edge of node</returns>
		public virtual int RightCharEdge(Tree node)
		{
			IList<Tree> s = GetLeaves();
			int length = 0;
			foreach (Tree leaf in s)
			{
				length += leaf.Label().Value().Length;
			}
			MutableInteger i = new MutableInteger(length);
			if (RightCharEdge(node, i))
			{
				return i;
			}
			return -1;
		}

		private bool RightCharEdge(Tree node, MutableInteger i)
		{
			if (this == node)
			{
				return true;
			}
			else
			{
				if (IsLeaf())
				{
					i.Set(i - Label().Value().Length);
					return false;
				}
				else
				{
					for (int j = Children().Length - 1; j >= 0; j--)
					{
						if (Children()[j].RightCharEdge(node, i))
						{
							return true;
						}
					}
					return false;
				}
			}
		}

		/// <summary>
		/// Calculates the node's <i>number</i>, defined as the number of nodes traversed in a left-to-right, depth-first search of the
		/// tree starting at
		/// <paramref name="root"/>
		/// and ending at
		/// <c>this</c>
		/// .  Returns -1 if
		/// <paramref name="root"/>
		/// does not contain
		/// <c>this</c>
		/// .
		/// </summary>
		/// <param name="root">the root node of the relevant tree</param>
		/// <returns>
		/// the number of the current node, or -1 if
		/// <paramref name="root"/>
		/// does not contain
		/// <c>this</c>
		/// .
		/// </returns>
		public virtual int NodeNumber(Tree root)
		{
			MutableInteger i = new MutableInteger(1);
			if (NodeNumberHelper(root, i))
			{
				return i;
			}
			return -1;
		}

		private bool NodeNumberHelper(Tree t, MutableInteger i)
		{
			if (this == t)
			{
				return true;
			}
			i.IncValue(1);
			foreach (Tree kid in t.Children())
			{
				if (NodeNumberHelper(kid, i))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Fetches the
		/// <paramref name="i"/>
		/// th node in the tree, with node numbers defined
		/// as in
		/// <see cref="NodeNumber(Tree)"/>
		/// .
		/// </summary>
		/// <param name="i">the node number to fetch</param>
		/// <returns>
		/// the
		/// <paramref name="i"/>
		/// th node in the tree
		/// </returns>
		/// <exception cref="System.IndexOutOfRangeException">
		/// if
		/// <paramref name="i"/>
		/// is not between 1 and
		/// the number of nodes (inclusive) contained in
		/// <c>this</c>
		/// .
		/// </exception>
		public virtual Tree GetNodeNumber(int i)
		{
			return GetNodeNumberHelper(new MutableInteger(1), i);
		}

		private Tree GetNodeNumberHelper(MutableInteger i, int target)
		{
			int i1 = i;
			if (i1 == target)
			{
				return this;
			}
			if (i1 > target)
			{
				throw new IndexOutOfRangeException("Error -- tree does not contain " + i + " nodes.");
			}
			i.IncValue(1);
			foreach (Tree kid in Children())
			{
				Tree temp = kid.GetNodeNumberHelper(i, target);
				if (temp != null)
				{
					return temp;
				}
			}
			return null;
		}

		/// <summary>
		/// Assign sequential integer indices to the leaves of the tree
		/// rooted at this
		/// <c>Tree</c>
		/// , starting with 1.
		/// The leaves are traversed from left
		/// to right. If the node is already indexed, then it uses the existing index.
		/// This will only work if the leaves extend CoreMap.
		/// </summary>
		public virtual void IndexLeaves()
		{
			IndexLeaves(1, false);
		}

		/// <summary>Index the leaves, and optionally overwrite existing IndexAnnotations if they exist.</summary>
		/// <param name="overWrite">Whether to replace an existing index for a leaf.</param>
		public virtual void IndexLeaves(bool overWrite)
		{
			IndexLeaves(1, overWrite);
		}

		/// <summary>
		/// Assign sequential integer indices to the leaves of the subtree
		/// rooted at this
		/// <c>Tree</c>
		/// , beginning with
		/// <paramref name="startIndex"/>
		/// , and traversing the leaves from left
		/// to right. If node is already indexed, then it uses the existing index.
		/// This method only works if the labels of the tree implement
		/// CoreLabel!
		/// </summary>
		/// <param name="startIndex">index for this node</param>
		/// <param name="overWrite">Whether to replace an existing index for a leaf.</param>
		/// <returns>the next index still unassigned</returns>
		public virtual int IndexLeaves(int startIndex, bool overWrite)
		{
			if (IsLeaf())
			{
				/*CoreLabel afl = (CoreLabel) label();
				Integer oldIndex = afl.get(CoreAnnotations.IndexAnnotation.class);
				if (!overWrite && oldIndex != null && oldIndex >= 0) {
				startIndex = oldIndex;
				} else {
				afl.set(CoreAnnotations.IndexAnnotation.class, startIndex);
				}*/
				if (Label() is IHasIndex)
				{
					IHasIndex hi = (IHasIndex)Label();
					int oldIndex = hi.Index();
					if (!overWrite && oldIndex >= 0)
					{
						startIndex = oldIndex;
					}
					else
					{
						hi.SetIndex(startIndex);
					}
					startIndex++;
				}
			}
			else
			{
				foreach (Tree kid in Children())
				{
					startIndex = kid.IndexLeaves(startIndex, overWrite);
				}
			}
			return startIndex;
		}

		/// <summary>Percolates terminal indices through a dependency tree.</summary>
		/// <remarks>
		/// Percolates terminal indices through a dependency tree. The terminals should be indexed, e.g.,
		/// by calling indexLeaves() on the tree.
		/// <p>
		/// This method assumes CoreLabels!
		/// </remarks>
		public virtual void PercolateHeadIndices()
		{
			if (IsPreTerminal())
			{
				int nodeIndex = ((IHasIndex)FirstChild().Label()).Index();
				((IHasIndex)Label()).SetIndex(nodeIndex);
				return;
			}
			// Assign the head index to the first child that we encounter with a matching
			// surface form. Obviously a head can have the same surface form as its dependent,
			// and in this case the head index is ambiguous.
			string wordAnnotation = ((IHasWord)Label()).Word();
			if (wordAnnotation == null)
			{
				wordAnnotation = Value();
			}
			bool seenHead = false;
			foreach (Tree child in Children())
			{
				child.PercolateHeadIndices();
				string childWordAnnotation = ((IHasWord)child.Label()).Word();
				if (childWordAnnotation == null)
				{
					childWordAnnotation = child.Value();
				}
				if (!seenHead && wordAnnotation.Equals(childWordAnnotation))
				{
					seenHead = true;
					int nodeIndex = ((IHasIndex)child.Label()).Index();
					((IHasIndex)Label()).SetIndex(nodeIndex);
				}
			}
		}

		/// <summary>Index all spans (constituents) in the tree.</summary>
		/// <remarks>
		/// Index all spans (constituents) in the tree.
		/// For this, spans uses 0-based indexing and the span records the fencepost
		/// to the left of the first word and after the last word of the span.
		/// The spans are only recorded if the Tree has labels of a class which
		/// extends CoreMap.
		/// </remarks>
		public virtual void IndexSpans()
		{
			IndexSpans(0);
		}

		public virtual void IndexSpans(int startIndex)
		{
			IndexSpans(new MutableInteger(startIndex));
		}

		/// <summary>Assigns span indices (BeginIndexAnnotation and EndIndexAnnotation) to all nodes in a tree.</summary>
		/// <remarks>
		/// Assigns span indices (BeginIndexAnnotation and EndIndexAnnotation) to all nodes in a tree.
		/// The beginning index is equivalent to the IndexAnnotation of the first leaf in the constituent.
		/// The end index is equivalent to the first integer after the IndexAnnotation of the last leaf in the constituent.
		/// </remarks>
		/// <param name="startIndex">Begin indexing at this value</param>
		public virtual Pair<int, int> IndexSpans(MutableInteger startIndex)
		{
			int start = int.MaxValue;
			int end = int.MinValue;
			if (IsLeaf())
			{
				start = startIndex;
				end = startIndex + 1;
				startIndex.IncValue(1);
			}
			else
			{
				foreach (Tree kid in Children())
				{
					Pair<int, int> span = kid.IndexSpans(startIndex);
					if (span.first < start)
					{
						start = span.first;
					}
					if (span.second > end)
					{
						end = span.second;
					}
				}
			}
			ILabel label = Label();
			if (label is ICoreMap)
			{
				ICoreMap afl = (ICoreMap)Label();
				afl.Set(typeof(CoreAnnotations.BeginIndexAnnotation), start);
				afl.Set(typeof(CoreAnnotations.EndIndexAnnotation), end);
			}
			return new Pair<int, int>(start, end);
		}
	}
}

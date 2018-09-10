using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>An abstract base class for relations between tree nodes in tregex.</summary>
	/// <remarks>
	/// An abstract base class for relations between tree nodes in tregex. There are
	/// two types of subclasses: static anonymous singleton instantiations for
	/// relations that do not require arguments, and private subclasses for those
	/// with arguments. All invocations should be made through the static factory
	/// methods, which insure that there is only a single instance of each relation.
	/// Thus == can be used instead of .equals.
	/// <p/>
	/// If you want to add a new
	/// relation, you just have to fill in the definition of satisfies and
	/// searchNodeIterator. Also be careful to make the appropriate adjustments to
	/// getRelation and SIMPLE_RELATIONS. Finally, if you are using the TregexParser,
	/// you need to add the new relation symbol to the list of tokens.
	/// </remarks>
	/// <author>Galen Andrew</author>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	internal abstract class Relation
	{
		private const long serialVersionUID = -1564793674551362909L;

		private readonly string symbol;

		/// <summary>Whether this relationship is satisfied between two trees.</summary>
		/// <param name="t1">The tree that is the left operand.</param>
		/// <param name="t2">The tree that is the right operand.</param>
		/// <param name="root">The common root of t1 and t2</param>
		/// <returns>Whether this relationship is satisfied.</returns>
		internal abstract bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher);

		/// <summary>
		/// For a given node, returns an
		/// <see cref="System.Collections.IEnumerator{E}"/>
		/// over the nodes
		/// of the tree containing the node that satisfy the relation.
		/// </summary>
		/// <param name="t">A node in a Tree</param>
		/// <param name="matcher">The matcher that nodes have to satisfy</param>
		/// <returns>
		/// An Iterator over the nodes
		/// of the root tree that satisfy the relation.
		/// </returns>
		internal abstract IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher);

		private static readonly Pattern parentOfLastChild = Pattern.Compile("(<-|<`)");

		private static readonly Pattern lastChildOfParent = Pattern.Compile("(>-|>`)");

		/// <summary>Static factory method for all relations with no arguments.</summary>
		/// <remarks>
		/// Static factory method for all relations with no arguments. Includes:
		/// DOMINATES, DOMINATED_BY, PARENT_OF, CHILD_OF, PRECEDES,
		/// IMMEDIATELY_PRECEDES, HAS_LEFTMOST_DESCENDANT, HAS_RIGHTMOST_DESCENDANT,
		/// LEFTMOST_DESCENDANT_OF, RIGHTMOST_DESCENDANT_OF, SISTER_OF, LEFT_SISTER_OF,
		/// RIGHT_SISTER_OF, IMMEDIATE_LEFT_SISTER_OF, IMMEDIATE_RIGHT_SISTER_OF,
		/// HEADS, HEADED_BY, IMMEDIATELY_HEADS, IMMEDIATELY_HEADED_BY, ONLY_CHILD_OF,
		/// HAS_ONLY_CHILD, EQUALS
		/// </remarks>
		/// <param name="s">The String representation of the relation</param>
		/// <returns>The singleton static relation of the specified type</returns>
		/// <exception cref="ParseException">If bad relation s</exception>
		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		internal static Edu.Stanford.Nlp.Trees.Tregex.Relation GetRelation(string s, Func<string, string> basicCatFunction, IHeadFinder headFinder)
		{
			if (SimpleRelationsMap.Contains(s))
			{
				return SimpleRelationsMap[s];
			}
			// these are shorthands for relations with arguments
			if (s.Equals("<,"))
			{
				return GetRelation("<", "1", basicCatFunction, headFinder);
			}
			else
			{
				if (parentOfLastChild.Matcher(s).Matches())
				{
					return GetRelation("<", "-1", basicCatFunction, headFinder);
				}
				else
				{
					if (s.Equals(">,"))
					{
						return GetRelation(">", "1", basicCatFunction, headFinder);
					}
					else
					{
						if (lastChildOfParent.Matcher(s).Matches())
						{
							return GetRelation(">", "-1", basicCatFunction, headFinder);
						}
					}
				}
			}
			// finally try relations with headFinders
			Edu.Stanford.Nlp.Trees.Tregex.Relation r;
			switch (s)
			{
				case ">>#":
				{
					r = new Relation.Heads(headFinder);
					break;
				}

				case "<<#":
				{
					r = new Relation.HeadedBy(headFinder);
					break;
				}

				case ">#":
				{
					r = new Relation.ImmediatelyHeads(headFinder);
					break;
				}

				case "<#":
				{
					r = new Relation.ImmediatelyHeadedBy(headFinder);
					break;
				}

				default:
				{
					throw new ParseException("Unrecognized simple relation " + s);
				}
			}
			return Interner.GlobalIntern(r);
		}

		/// <summary>
		/// Static factory method for relations requiring an argument, including
		/// HAS_ITH_CHILD, ITH_CHILD_OF, UNBROKEN_CATEGORY_DOMINATES,
		/// UNBROKEN_CATEGORY_DOMINATED_BY.
		/// </summary>
		/// <param name="s">The String representation of the relation</param>
		/// <param name="arg">
		/// The argument to the relation, as a string; could be a node
		/// description or an integer
		/// </param>
		/// <returns>
		/// The singleton static relation of the specified type with the
		/// specified argument. Uses Interner to insure singleton-ity
		/// </returns>
		/// <exception cref="ParseException">If bad relation s</exception>
		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		internal static Edu.Stanford.Nlp.Trees.Tregex.Relation GetRelation(string s, string arg, Func<string, string> basicCatFunction, IHeadFinder headFinder)
		{
			if (arg == null)
			{
				return GetRelation(s, basicCatFunction, headFinder);
			}
			Edu.Stanford.Nlp.Trees.Tregex.Relation r;
			switch (s)
			{
				case "<":
				{
					r = new Relation.HasIthChild(System.Convert.ToInt32(arg));
					break;
				}

				case ">":
				{
					r = new Relation.IthChildOf(System.Convert.ToInt32(arg));
					break;
				}

				case "<+":
				{
					r = new Relation.UnbrokenCategoryDominates(arg, basicCatFunction);
					break;
				}

				case ">+":
				{
					r = new Relation.UnbrokenCategoryIsDominatedBy(arg, basicCatFunction);
					break;
				}

				case ".+":
				{
					r = new Relation.UnbrokenCategoryPrecedes(arg, basicCatFunction);
					break;
				}

				case ",+":
				{
					r = new Relation.UnbrokenCategoryFollows(arg, basicCatFunction);
					break;
				}

				default:
				{
					throw new ParseException("Unrecognized compound relation " + s + ' ' + arg);
				}
			}
			return Interner.GlobalIntern(r);
		}

		/// <summary>
		/// Produce a TregexPattern which represents the given MULTI_RELATION
		/// and its children
		/// </summary>
		/// <exception cref="Edu.Stanford.Nlp.Trees.Tregex.ParseException"/>
		internal static TregexPattern ConstructMultiRelation(string s, IList<DescriptionPattern> children, Func<string, string> basicCatFunction, IHeadFinder headFinder)
		{
			if (s.Equals("<..."))
			{
				IList<TregexPattern> newChildren = Generics.NewArrayList();
				for (int i = 0; i < children.Count; ++i)
				{
					Edu.Stanford.Nlp.Trees.Tregex.Relation rel = GetRelation("<", int.ToString(i + 1), basicCatFunction, headFinder);
					DescriptionPattern oldChild = children[i];
					TregexPattern newChild = new DescriptionPattern(rel, oldChild);
					newChildren.Add(newChild);
				}
				Edu.Stanford.Nlp.Trees.Tregex.Relation rel_1 = GetRelation("<", int.ToString(children.Count + 1), basicCatFunction, headFinder);
				TregexPattern noExtraChildren = new DescriptionPattern(rel_1, false, "__", null, false, basicCatFunction, Java.Util.Collections.EmptyList<Pair<int, string>>(), false, null);
				noExtraChildren.Negate();
				newChildren.Add(noExtraChildren);
				return new CoordinationPattern(newChildren, true);
			}
			else
			{
				throw new ParseException("Unknown multi relation " + s);
			}
		}

		private Relation(string symbol)
		{
			this.symbol = symbol;
		}

		public override string ToString()
		{
			return symbol;
		}

		/// <summary>
		/// This abstract Iterator implements a NULL iterator, but by subclassing and
		/// overriding advance and/or initialize, it is an efficient implementation.
		/// </summary>
		internal abstract class SearchNodeIterator : IEnumerator<Tree>
		{
			public SearchNodeIterator()
			{
				Initialize();
			}

			/// <summary>
			/// This is the next tree to be returned by the iterator, or null if there
			/// are no more items.
			/// </summary>
			internal Tree next;

			// = null;
			/// <summary>
			/// This method must insure that next points to first item, or null if there
			/// are no items.
			/// </summary>
			internal virtual void Initialize()
			{
				Advance();
			}

			/// <summary>
			/// This method must insure that next points to next item, or null if there
			/// are no more items.
			/// </summary>
			internal virtual void Advance()
			{
				next = null;
			}

			public virtual bool MoveNext()
			{
				return next != null;
			}

			public virtual Tree Current
			{
				get
				{
					if (next == null)
					{
						throw new NoSuchElementException();
					}
					Tree ret = next;
					Advance();
					return ret;
				}
			}

			public virtual void Remove()
			{
				throw new NotSupportedException("SearchNodeIterator does not support remove().");
			}
		}

		private sealed class _Relation_265 : Relation
		{
			public _Relation_265(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -8311913236233762612L;
			}

			// used in TregexParser
			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return t1 == t2;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_277(t);
			}

			private sealed class _SearchNodeIterator_277 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_277(Tree t)
				{
					this.t = t;
				}

				internal override void Initialize()
				{
					this.next = t;
				}

				private readonly Tree t;
			}
		}

		internal static readonly Relation Root = new _Relation_265("Root");

		private sealed class _Relation_286 : Relation
		{
			public _Relation_286(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 164629344977943816L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return t1 == t2;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return Java.Util.Collections.SingletonList(t).GetEnumerator();
			}
		}

		private static readonly Relation Equals = new _Relation_286("==");

		private sealed class _Relation_304 : Relation
		{
			public _Relation_304(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 3409941930361386114L;
			}

			/* this is a "dummy" relation that allows you to segment patterns. */
			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return true;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return matcher.GetRoot().GetEnumerator();
			}
		}

		private static readonly Relation PatternSplitter = new _Relation_304(":");

		private sealed class _Relation_320 : Relation
		{
			public _Relation_320(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -2580199434621268260L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return t1 != t2 && t1.Dominates(t2);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_332(t);
			}

			private sealed class _SearchNodeIterator_332 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_332(Tree t)
				{
					this.t = t;
				}

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.searchStack = new Stack<Tree>();
					for (int i = t.NumChildren() - 1; i >= 0; i--)
					{
						this.searchStack.Push(t.GetChild(i));
					}
					if (!this.searchStack.IsEmpty())
					{
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
						for (int i = this.next.NumChildren() - 1; i >= 0; i--)
						{
							this.searchStack.Push(this.next.GetChild(i));
						}
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation Dominates = new _Relation_320("<<");

		private sealed class _Relation_361 : Relation
		{
			public _Relation_361(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 6140614010121387690L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Relation.Dominates.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_373(matcher, t);
			}

			private sealed class _SearchNodeIterator_373 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_373(TregexMatcher matcher, Tree t)
				{
					this.matcher = matcher;
					this.t = t;
				}

				internal override void Initialize()
				{
					this.next = matcher.GetParent(t);
				}

				internal override void Advance()
				{
					this.next = matcher.GetParent(this.next);
				}

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}
		}

		private static readonly Relation DominatedBy = new _Relation_361(">>");

		private sealed class _Relation_387 : Relation
		{
			public _Relation_387(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 9140193735607580808L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				Tree[] kids = t1.Children();
				foreach (Tree kid in kids)
				{
					if (kid == t2)
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_405(t);
			}

			private sealed class _SearchNodeIterator_405 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_405(Tree t)
				{
					this.t = t;
				}

				internal int nextNum;

				// subtle bug warning here: if we use 
				//   int nextNum=0;
				// instead, we get the first daughter twice because the
				// assignment occurs after advance() has already been called
				// once by the constructor of SearchNodeIterator.
				internal override void Advance()
				{
					if (this.nextNum < t.NumChildren())
					{
						this.next = t.GetChild(this.nextNum);
						this.nextNum++;
					}
					else
					{
						this.next = null;
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation ParentOf = new _Relation_387("<");

		private sealed class _Relation_426 : Relation
		{
			public _Relation_426(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 8919710375433372537L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Relation.ParentOf.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_438(matcher, t);
			}

			private sealed class _SearchNodeIterator_438 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_438(TregexMatcher matcher, Tree t)
				{
					this.matcher = matcher;
					this.t = t;
				}

				internal override void Initialize()
				{
					this.next = matcher.GetParent(t);
				}

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}
		}

		private static readonly Relation ChildOf = new _Relation_426(">");

		private sealed class _Relation_447 : Relation
		{
			public _Relation_447(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -9065012389549976867L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Edu.Stanford.Nlp.Trees.Trees.RightEdge(t1, root) <= Edu.Stanford.Nlp.Trees.Trees.LeftEdge(t2, root);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_459(t, matcher);
			}

			private sealed class _SearchNodeIterator_459 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_459(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.searchStack = new Stack<Tree>();
					Tree current = t;
					Tree parent = matcher.GetParent(t);
					while (parent != null)
					{
						for (int i = parent.NumChildren() - 1; parent.GetChild(i) != current; i--)
						{
							this.searchStack.Push(parent.GetChild(i));
						}
						current = parent;
						parent = matcher.GetParent(parent);
					}
					this.Advance();
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
						for (int i = this.next.NumChildren() - 1; i >= 0; i--)
						{
							this.searchStack.Push(this.next.GetChild(i));
						}
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation Precedes = new _Relation_447("..");

		private sealed class _Relation_492 : Relation
		{
			public _Relation_492(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 3390147676937292768L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Edu.Stanford.Nlp.Trees.Trees.LeftEdge(t2, root) == Edu.Stanford.Nlp.Trees.Trees.RightEdge(t1, root);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_504(t, matcher);
			}

			private sealed class _SearchNodeIterator_504 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_504(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					Tree current;
					Tree parent = t;
					do
					{
						current = parent;
						parent = matcher.GetParent(parent);
						if (parent == null)
						{
							this.next = null;
							return;
						}
					}
					while (parent.LastChild() == current);
					for (int i = 1; i < n; i++)
					{
						if (parent.GetChild(i - 1) == current)
						{
							this.next = parent.GetChild(i);
							return;
						}
					}
				}

				internal override void Advance()
				{
					if (this.next.IsLeaf())
					{
						this.next = null;
					}
					else
					{
						this.next = this.next.FirstChild();
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation ImmediatelyPrecedes = new _Relation_492(".");

		private sealed class _Relation_538 : Relation
		{
			public _Relation_538(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -5948063114149496983L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Edu.Stanford.Nlp.Trees.Trees.RightEdge(t2, root) <= Edu.Stanford.Nlp.Trees.Trees.LeftEdge(t1, root);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_550(t, matcher);
			}

			private sealed class _SearchNodeIterator_550 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_550(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.searchStack = new Stack<Tree>();
					Tree current = t;
					Tree parent = matcher.GetParent(t);
					while (parent != null)
					{
						for (int i = 0; parent.GetChild(i) != current; i++)
						{
							this.searchStack.Push(parent.GetChild(i));
						}
						current = parent;
						parent = matcher.GetParent(parent);
					}
					this.Advance();
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
						for (int i = this.next.NumChildren() - 1; i >= 0; i--)
						{
							this.searchStack.Push(this.next.GetChild(i));
						}
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation Follows = new _Relation_538(",,");

		private sealed class _Relation_583 : Relation
		{
			public _Relation_583(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -2895075562891296830L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Edu.Stanford.Nlp.Trees.Trees.LeftEdge(t1, root) == Edu.Stanford.Nlp.Trees.Trees.RightEdge(t2, root);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_595(t, matcher);
			}

			private sealed class _SearchNodeIterator_595 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_595(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					Tree current;
					Tree parent = t;
					do
					{
						current = parent;
						parent = matcher.GetParent(parent);
						if (parent == null)
						{
							this.next = null;
							return;
						}
					}
					while (parent.FirstChild() == current);
					for (int i = 0; i < n; i++)
					{
						if (parent.GetChild(i + 1) == current)
						{
							this.next = parent.GetChild(i);
							return;
						}
					}
				}

				internal override void Advance()
				{
					if (this.next.IsLeaf())
					{
						this.next = null;
					}
					else
					{
						this.next = this.next.LastChild();
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation ImmediatelyFollows = new _Relation_583(",");

		private sealed class _Relation_629 : Relation
		{
			public _Relation_629(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -7352081789429366726L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1.IsLeaf())
				{
					return false;
				}
				else
				{
					return (t1.Children()[0] == t2) || this.Satisfies(t1.Children()[0], t2, root, matcher);
				}
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_645(t);
			}

			private sealed class _SearchNodeIterator_645 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_645(Tree t)
				{
					this.t = t;
				}

				internal override void Initialize()
				{
					this.next = t;
					this.Advance();
				}

				internal override void Advance()
				{
					if (this.next.IsLeaf())
					{
						this.next = null;
					}
					else
					{
						this.next = this.next.FirstChild();
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation HasLeftmostDescendant = new _Relation_629("<<,");

		private sealed class _Relation_664 : Relation
		{
			public _Relation_664(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -1405509785337859888L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1.IsLeaf())
				{
					return false;
				}
				else
				{
					Tree lastKid = t1.Children()[t1.Children().Length - 1];
					return (lastKid == t2) || this.Satisfies(lastKid, t2, root, matcher);
				}
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_681(t);
			}

			private sealed class _SearchNodeIterator_681 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_681(Tree t)
				{
					this.t = t;
				}

				internal override void Initialize()
				{
					this.next = t;
					this.Advance();
				}

				internal override void Advance()
				{
					if (this.next.IsLeaf())
					{
						this.next = null;
					}
					else
					{
						this.next = this.next.LastChild();
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation HasRightmostDescendant = new _Relation_664("<<-");

		private sealed class _Relation_700 : Relation
		{
			public _Relation_700(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 3103412865783190437L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Relation.HasLeftmostDescendant.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_712(t, matcher);
			}

			private sealed class _SearchNodeIterator_712 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_712(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					this.next = t;
					this.Advance();
				}

				internal override void Advance()
				{
					Tree last = this.next;
					this.next = matcher.GetParent(this.next);
					if (this.next != null && this.next.FirstChild() != last)
					{
						this.next = null;
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation LeftmostDescendantOf = new _Relation_700(">>,");

		private sealed class _Relation_731 : Relation
		{
			public _Relation_731(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -2000255467314675477L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Relation.HasRightmostDescendant.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_743(t, matcher);
			}

			private sealed class _SearchNodeIterator_743 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_743(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					this.next = t;
					this.Advance();
				}

				internal override void Advance()
				{
					Tree last = this.next;
					this.next = matcher.GetParent(this.next);
					if (this.next != null && this.next.LastChild() != last)
					{
						this.next = null;
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation RightmostDescendantOf = new _Relation_731(">>-");

		private sealed class _Relation_762 : Relation
		{
			public _Relation_762(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -3776688096782419004L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1 == t2 || t1 == root)
				{
					return false;
				}
				Tree parent = t1.Parent(root);
				return Relation.ParentOf.Satisfies(parent, t2, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_778(matcher, t);
			}

			private sealed class _SearchNodeIterator_778 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_778(TregexMatcher matcher, Tree t)
				{
					this.matcher = matcher;
					this.t = t;
				}

				internal Tree parent;

				internal int nextNum;

				internal override void Initialize()
				{
					this.parent = matcher.GetParent(t);
					if (this.parent != null)
					{
						this.nextNum = 0;
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (this.nextNum < this.parent.NumChildren())
					{
						this.next = this.parent.GetChild(this.nextNum++);
						if (this.next == t)
						{
							this.Advance();
						}
					}
					else
					{
						this.next = null;
					}
				}

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}
		}

		private static readonly Relation SisterOf = new _Relation_762("$");

		private sealed class _Relation_807 : Relation
		{
			public _Relation_807(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -4516161080140406862L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1 == t2 || t1 == root)
				{
					return false;
				}
				Tree parent = t1.Parent(root);
				Tree[] kids = parent.Children();
				for (int i = kids.Length - 1; i > 0; i--)
				{
					if (kids[i] == t1)
					{
						return false;
					}
					if (kids[i] == t2)
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_832(matcher, t);
			}

			private sealed class _SearchNodeIterator_832 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_832(TregexMatcher matcher, Tree t)
				{
					this.matcher = matcher;
					this.t = t;
				}

				internal Tree parent;

				internal int nextNum;

				internal override void Initialize()
				{
					this.parent = matcher.GetParent(t);
					if (this.parent != null)
					{
						this.nextNum = this.parent.NumChildren() - 1;
						this.Advance();
					}
				}

				internal override void Advance()
				{
					this.next = this.parent.GetChild(this.nextNum--);
					if (this.next == t)
					{
						this.next = null;
					}
				}

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}
		}

		private static readonly Relation LeftSisterOf = new _Relation_807("$++");

		private sealed class _Relation_857 : Relation
		{
			public _Relation_857(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -5880626025192328694L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Relation.LeftSisterOf.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_869(matcher, t);
			}

			private sealed class _SearchNodeIterator_869 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_869(TregexMatcher matcher, Tree t)
				{
					this.matcher = matcher;
					this.t = t;
				}

				internal Tree parent;

				internal int nextNum;

				internal override void Initialize()
				{
					this.parent = matcher.GetParent(t);
					if (this.parent != null)
					{
						this.nextNum = 0;
						this.Advance();
					}
				}

				internal override void Advance()
				{
					this.next = this.parent.GetChild(this.nextNum++);
					if (this.next == t)
					{
						this.next = null;
					}
				}

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}
		}

		private static readonly Relation RightSisterOf = new _Relation_857("$--");

		private sealed class _Relation_894 : Relation
		{
			public _Relation_894(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 7745237994722126917L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1 == t2 || t1 == root)
				{
					return false;
				}
				Tree[] sisters = t1.Parent(root).Children();
				for (int i = sisters.Length - 1; i > 0; i--)
				{
					if (sisters[i] == t1)
					{
						return false;
					}
					if (sisters[i] == t2)
					{
						return sisters[i - 1] == t1;
					}
				}
				return false;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_918(t, matcher);
			}

			private sealed class _SearchNodeIterator_918 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_918(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					if (t != matcher.GetRoot())
					{
						Tree parent = matcher.GetParent(t);
						int i = 0;
						while (parent.GetChild(i) != t)
						{
							i++;
						}
						if (i + 1 < parent.NumChildren())
						{
							this.next = parent.GetChild(i + 1);
						}
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation ImmediateLeftSisterOf = new _Relation_894("$+");

		private sealed class _Relation_936 : Relation
		{
			public _Relation_936(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -6555264189937531019L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return Relation.ImmediateLeftSisterOf.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_948(t, matcher);
			}

			private sealed class _SearchNodeIterator_948 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_948(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					if (t != matcher.GetRoot())
					{
						Tree parent = matcher.GetParent(t);
						int i = 0;
						while (parent.GetChild(i) != t)
						{
							i++;
						}
						if (i > 0)
						{
							this.next = parent.GetChild(i - 1);
						}
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation ImmediateRightSisterOf = new _Relation_936("$-");

		private sealed class _Relation_966 : Relation
		{
			public _Relation_966(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 1719812660770087879L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return t2.Children().Length == 1 && t2.FirstChild() == t1;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_978(t, matcher);
			}

			private sealed class _SearchNodeIterator_978 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_978(Tree t, TregexMatcher matcher)
				{
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					if (t != matcher.GetRoot())
					{
						this.next = matcher.GetParent(t);
						if (this.next.NumChildren() != 1)
						{
							this.next = null;
						}
					}
				}

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		private static readonly Relation OnlyChildOf = new _Relation_966(">:");

		private sealed class _Relation_992 : Relation
		{
			public _Relation_992(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -8776487500849294279L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return t1.Children().Length == 1 && t1.FirstChild() == t2;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1004(t);
			}

			private sealed class _SearchNodeIterator_1004 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1004(Tree t)
				{
					this.t = t;
				}

				internal override void Initialize()
				{
					if (!t.IsLeaf() && t.NumChildren() == 1)
					{
						this.next = t.FirstChild();
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation HasOnlyChild = new _Relation_992("<:");

		private sealed class _Relation_1015 : Relation
		{
			public _Relation_1015(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = -742912038636163403L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1.IsLeaf() || t1.Children().Length > 1)
				{
					return false;
				}
				Tree onlyDtr = t1.Children()[0];
				if (onlyDtr == t2)
				{
					return true;
				}
				else
				{
					return this.Satisfies(onlyDtr, t2, root, matcher);
				}
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1033(t);
			}

			private sealed class _SearchNodeIterator_1033 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1033(Tree t)
				{
					this.t = t;
				}

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.searchStack = new Stack<Tree>();
					if (!t.IsLeaf() && t.Children().Length == 1)
					{
						this.searchStack.Push(t.GetChild(0));
					}
					if (!this.searchStack.IsEmpty())
					{
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
						if (!this.next.IsLeaf() && this.next.Children().Length == 1)
						{
							this.searchStack.Push(this.next.GetChild(0));
						}
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation UnaryPathAncestorOf = new _Relation_1015("<<:");

		private sealed class _Relation_1060 : Relation
		{
			public _Relation_1060(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 4364021807752979404L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t2.IsLeaf() || t2.Children().Length > 1)
				{
					return false;
				}
				Tree onlyDtr = t2.Children()[0];
				if (onlyDtr == t1)
				{
					return true;
				}
				else
				{
					return this.Satisfies(t1, onlyDtr, root, matcher);
				}
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1078(matcher, t);
			}

			private sealed class _SearchNodeIterator_1078 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1078(TregexMatcher matcher, Tree t)
				{
					this.matcher = matcher;
					this.t = t;
				}

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.searchStack = new Stack<Tree>();
					Tree parent = matcher.GetParent(t);
					if (parent != null && !parent.IsLeaf() && parent.Children().Length == 1)
					{
						this.searchStack.Push(parent);
					}
					if (!this.searchStack.IsEmpty())
					{
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
						Tree parent = matcher.GetParent(this.next);
						if (parent != null && !parent.IsLeaf() && parent.Children().Length == 1)
						{
							this.searchStack.Push(parent);
						}
					}
				}

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}
		}

		private static readonly Relation UnaryPathDescendantOf = new _Relation_1060(">>:");

		private sealed class _Relation_1109 : Relation
		{
			public _Relation_1109(string baseArg1)
				: base(baseArg1)
			{
				this.serialVersionUID = 98745298745198245L;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t1 == t2)
				{
					return true;
				}
				return Relation.ParentOf.Satisfies(t1, t2, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1123(t);
			}

			private sealed class _SearchNodeIterator_1123 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1123(Tree t)
				{
					this.t = t;
				}

				internal int nextNum;

				internal bool usedParent;

				internal override void Advance()
				{
					if (!this.usedParent)
					{
						this.next = t;
						this.usedParent = true;
					}
					else
					{
						if (this.nextNum < t.NumChildren())
						{
							this.next = t.GetChild(this.nextNum);
							this.nextNum++;
						}
						else
						{
							this.next = null;
						}
					}
				}

				private readonly Tree t;
			}
		}

		private static readonly Relation ParentEquals = new _Relation_1109("<=");

		private static readonly Relation[] SimpleRelations = new Relation[] { Dominates, DominatedBy, ParentOf, ChildOf, Precedes, ImmediatelyPrecedes, Follows, ImmediatelyFollows, HasLeftmostDescendant, HasRightmostDescendant, LeftmostDescendantOf, 
			RightmostDescendantOf, SisterOf, LeftSisterOf, RightSisterOf, ImmediateLeftSisterOf, ImmediateRightSisterOf, OnlyChildOf, HasOnlyChild, Equals, PatternSplitter, UnaryPathAncestorOf, UnaryPathDescendantOf, ParentEquals };

		private static readonly IDictionary<string, Relation> SimpleRelationsMap = Generics.NewHashMap();

		static Relation()
		{
			foreach (Relation r in SimpleRelations)
			{
				SimpleRelationsMap[r.symbol] = r;
			}
			SimpleRelationsMap["<<`"] = HasRightmostDescendant;
			SimpleRelationsMap["<<,"] = HasLeftmostDescendant;
			SimpleRelationsMap[">>`"] = RightmostDescendantOf;
			SimpleRelationsMap[">>,"] = LeftmostDescendantOf;
			SimpleRelationsMap["$.."] = LeftSisterOf;
			SimpleRelationsMap["$,,"] = RightSisterOf;
			SimpleRelationsMap["$."] = ImmediateLeftSisterOf;
			SimpleRelationsMap["$,"] = ImmediateRightSisterOf;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Relation))
			{
				return false;
			}
			Relation relation = (Relation)o;
			return symbol.Equals(relation.symbol);
		}

		public override int GetHashCode()
		{
			return symbol.GetHashCode();
		}

		[System.Serializable]
		private class Heads : Relation
		{
			private const long serialVersionUID = 4681433462932265831L;

			internal readonly IHeadFinder hf;

			internal Heads(IHeadFinder hf)
				: base(">>#")
			{
				this.hf = hf;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (t2.IsLeaf())
				{
					return false;
				}
				else
				{
					if (t2.IsPreTerminal())
					{
						return (t2.FirstChild() == t1);
					}
					else
					{
						IHeadFinder headFinder = matcher.GetHeadFinder();
						if (headFinder == null)
						{
							headFinder = this.hf;
						}
						Tree head = headFinder.DetermineHead(t2);
						if (head == t1)
						{
							return true;
						}
						else
						{
							return Satisfies(t1, head, root, matcher);
						}
					}
				}
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1223(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_1223 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1223(Heads _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					this.next = t;
					this.Advance();
				}

				internal override void Advance()
				{
					IHeadFinder headFinder = matcher.GetHeadFinder();
					if (headFinder == null)
					{
						headFinder = this._enclosing.hf;
					}
					Tree last = this.next;
					this.next = matcher.GetParent(this.next);
					if (this.next != null && headFinder.DetermineHead(this.next) != last)
					{
						this.next = null;
					}
				}

				private readonly Heads _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.Heads))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Relation.Heads heads = (Relation.Heads)o;
				if (hf != null ? !hf.Equals(heads.hf) : heads.hf != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 29 * result + (hf != null ? hf.GetHashCode() : 0);
				return result;
			}
		}

		[System.Serializable]
		private class HeadedBy : Relation
		{
			private const long serialVersionUID = 2825997185749055693L;

			private readonly Relation.Heads heads;

			internal HeadedBy(IHeadFinder hf)
				: base("<<#")
			{
				this.heads = Interner.GlobalIntern(new Relation.Heads(hf));
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return heads.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1293(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_1293 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1293(HeadedBy _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					this.next = t;
					this.Advance();
				}

				internal override void Advance()
				{
					if (this.next.IsLeaf())
					{
						this.next = null;
					}
					else
					{
						if (matcher.GetHeadFinder() != null)
						{
							this.next = matcher.GetHeadFinder().DetermineHead(this.next);
						}
						else
						{
							this.next = this._enclosing.heads.hf.DetermineHead(this.next);
						}
					}
				}

				private readonly HeadedBy _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.HeadedBy))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Relation.HeadedBy headedBy = (Relation.HeadedBy)o;
				if (heads != null ? !heads.Equals(headedBy.heads) : headedBy.heads != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 29 * result + (heads != null ? heads.GetHashCode() : 0);
				return result;
			}
		}

		[System.Serializable]
		private class ImmediatelyHeads : Relation
		{
			private const long serialVersionUID = 2085410152913894987L;

			private readonly IHeadFinder hf;

			internal ImmediatelyHeads(IHeadFinder hf)
				: base(">#")
			{
				this.hf = hf;
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				if (matcher.GetHeadFinder() != null)
				{
					return matcher.GetHeadFinder().DetermineHead(t2) == t1;
				}
				else
				{
					return hf.DetermineHead(t2) == t1;
				}
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1370(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_1370 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1370(ImmediatelyHeads _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					if (t != matcher.GetRoot())
					{
						this.next = matcher.GetParent(t);
						IHeadFinder headFinder = matcher.GetHeadFinder() == null ? this._enclosing.hf : matcher.GetHeadFinder();
						if (headFinder.DetermineHead(this.next) != t)
						{
							this.next = null;
						}
					}
				}

				private readonly ImmediatelyHeads _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.ImmediatelyHeads))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Relation.ImmediatelyHeads immediatelyHeads = (Relation.ImmediatelyHeads)o;
				if (hf != null ? !hf.Equals(immediatelyHeads.hf) : immediatelyHeads.hf != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 29 * result + (hf != null ? hf.GetHashCode() : 0);
				return result;
			}
		}

		[System.Serializable]
		private class ImmediatelyHeadedBy : Relation
		{
			private const long serialVersionUID = 5910075663419780905L;

			private readonly Relation.ImmediatelyHeads immediatelyHeads;

			internal ImmediatelyHeadedBy(IHeadFinder hf)
				: base("<#")
			{
				this.immediatelyHeads = Interner.GlobalIntern(new Relation.ImmediatelyHeads(hf));
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return immediatelyHeads.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1434(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_1434 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1434(ImmediatelyHeadedBy _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					if (!t.IsLeaf())
					{
						if (matcher.GetHeadFinder() != null)
						{
							this.next = matcher.GetHeadFinder().DetermineHead(t);
						}
						else
						{
							this.next = this._enclosing.immediatelyHeads.hf.DetermineHead(t);
						}
					}
				}

				private readonly ImmediatelyHeadedBy _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.ImmediatelyHeadedBy))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Relation.ImmediatelyHeadedBy immediatelyHeadedBy = (Relation.ImmediatelyHeadedBy)o;
				if (immediatelyHeads != null ? !immediatelyHeads.Equals(immediatelyHeadedBy.immediatelyHeads) : immediatelyHeadedBy.immediatelyHeads != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 29 * result + (immediatelyHeads != null ? immediatelyHeads.GetHashCode() : 0);
				return result;
			}
		}

		[System.Serializable]
		private class IthChildOf : Relation
		{
			private const long serialVersionUID = -1463126827537879633L;

			private readonly int childNum;

			internal IthChildOf(int i)
				: base('>' + i.ToString())
			{
				if (i == 0)
				{
					throw new ArgumentException("Error -- no such thing as zeroth child!");
				}
				else
				{
					childNum = i;
				}
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				Tree[] kids = t2.Children();
				if (kids.Length < Math.Abs(childNum))
				{
					return false;
				}
				if (childNum > 0 && kids[childNum - 1] == t1)
				{
					return true;
				}
				if (childNum < 0 && kids[kids.Length + childNum] == t1)
				{
					return true;
				}
				return false;
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1515(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_1515 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1515(IthChildOf _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				internal override void Initialize()
				{
					if (t != matcher.GetRoot())
					{
						this.next = matcher.GetParent(t);
						if (this._enclosing.childNum > 0 && (this.next.NumChildren() < this._enclosing.childNum || this.next.GetChild(this._enclosing.childNum - 1) != t) || this._enclosing.childNum < 0 && (this.next.NumChildren() < -this._enclosing.childNum || this
							.next.GetChild(this.next.NumChildren() + this._enclosing.childNum) != t))
						{
							this.next = null;
						}
					}
				}

				private readonly IthChildOf _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.IthChildOf))
				{
					return false;
				}
				Relation.IthChildOf ithChildOf = (Relation.IthChildOf)o;
				if (childNum != ithChildOf.childNum)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				return childNum;
			}
		}

		[System.Serializable]
		private class HasIthChild : Relation
		{
			private const long serialVersionUID = 3546853729291582806L;

			private readonly Relation.IthChildOf ithChildOf;

			internal HasIthChild(int i)
				: base('<' + i.ToString())
			{
				ithChildOf = Interner.GlobalIntern(new Relation.IthChildOf(i));
			}

			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return ithChildOf.Satisfies(t2, t1, root, matcher);
			}

			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1579(this, t);
			}

			private sealed class _SearchNodeIterator_1579 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1579(HasIthChild _enclosing, Tree t)
				{
					this._enclosing = _enclosing;
					this.t = t;
				}

				internal override void Initialize()
				{
					int childNum = this._enclosing.ithChildOf.childNum;
					if (t.NumChildren() >= Math.Abs(childNum))
					{
						if (childNum > 0)
						{
							this.next = t.GetChild(childNum - 1);
						}
						else
						{
							this.next = t.GetChild(t.NumChildren() + childNum);
						}
					}
				}

				private readonly HasIthChild _enclosing;

				private readonly Tree t;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.HasIthChild))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Relation.HasIthChild hasIthChild = (Relation.HasIthChild)o;
				if (ithChildOf != null ? !ithChildOf.Equals(hasIthChild.ithChildOf) : hasIthChild.ithChildOf != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 29 * result + (ithChildOf != null ? ithChildOf.GetHashCode() : 0);
				return result;
			}
		}

		[System.Serializable]
		private class UnbrokenCategoryDominates : Relation
		{
			private const long serialVersionUID = -4174923168221859262L;

			private readonly Pattern pattern;

			private readonly bool negatedPattern;

			private readonly bool basicCat;

			private Func<string, string> basicCatFunction;

			/// <param name="arg">
			/// This may have a ! and then maybe a @ and then either an
			/// identifier or regex
			/// </param>
			internal UnbrokenCategoryDominates(string arg, Func<string, string> basicCatFunction)
				: base("<+(" + arg + ')')
			{
				if (arg.StartsWith("!"))
				{
					negatedPattern = true;
					arg = Sharpen.Runtime.Substring(arg, 1);
				}
				else
				{
					negatedPattern = false;
				}
				if (arg.StartsWith("@"))
				{
					basicCat = true;
					this.basicCatFunction = basicCatFunction;
					arg = Sharpen.Runtime.Substring(arg, 1);
				}
				else
				{
					basicCat = false;
				}
				if (arg.Matches("/.*/"))
				{
					pattern = Pattern.Compile(Sharpen.Runtime.Substring(arg, 1, arg.Length - 1));
				}
				else
				{
					if (arg.Matches("__"))
					{
						pattern = Pattern.Compile("^.*$");
					}
					else
					{
						pattern = Pattern.Compile("^(?:" + arg + ")$");
					}
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				foreach (Tree kid in t1.Children())
				{
					if (kid == t2)
					{
						return true;
					}
					else
					{
						if (PathMatchesNode(kid) && Satisfies(kid, t2, root, matcher))
						{
							return true;
						}
					}
				}
				return false;
			}

			private bool PathMatchesNode(Tree node)
			{
				string lab = node.Value();
				// added this code to not crash if null node, even though there probably should be null nodes in the tree
				if (lab == null)
				{
					// Say that a null label matches no positive pattern, but any negated patern
					return negatedPattern;
				}
				else
				{
					if (basicCat)
					{
						lab = basicCatFunction.Apply(lab);
					}
					Matcher m = pattern.Matcher(lab);
					return m.Find() != negatedPattern;
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1701(this, t);
			}

			private sealed class _SearchNodeIterator_1701 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1701(UnbrokenCategoryDominates _enclosing, Tree t)
				{
					this._enclosing = _enclosing;
					this.t = t;
				}

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.searchStack = new Stack<Tree>();
					for (int i = t.NumChildren() - 1; i >= 0; i--)
					{
						this.searchStack.Push(t.GetChild(i));
					}
					if (!this.searchStack.IsEmpty())
					{
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
						if (this._enclosing.PathMatchesNode(this.next))
						{
							for (int i = this.next.NumChildren() - 1; i >= 0; i--)
							{
								this.searchStack.Push(this.next.GetChild(i));
							}
						}
					}
				}

				private readonly UnbrokenCategoryDominates _enclosing;

				private readonly Tree t;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.UnbrokenCategoryDominates))
				{
					return false;
				}
				Relation.UnbrokenCategoryDominates unbrokenCategoryDominates = (Relation.UnbrokenCategoryDominates)o;
				if (negatedPattern != unbrokenCategoryDominates.negatedPattern)
				{
					return false;
				}
				if (!pattern.Equals(unbrokenCategoryDominates.pattern))
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result;
				result = pattern.GetHashCode();
				result = 29 * result + (negatedPattern ? 1 : 0);
				return result;
			}
		}

		[System.Serializable]
		private class UnbrokenCategoryIsDominatedBy : Relation
		{
			private const long serialVersionUID = 2867922828235355129L;

			private readonly Relation.UnbrokenCategoryDominates unbrokenCategoryDominates;

			internal UnbrokenCategoryIsDominatedBy(string arg, Func<string, string> basicCatFunction)
				: base(">+(" + arg + ')')
			{
				// end class UnbrokenCategoryDominates
				unbrokenCategoryDominates = Interner.GlobalIntern((new Relation.UnbrokenCategoryDominates(arg, basicCatFunction)));
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return unbrokenCategoryDominates.Satisfies(t2, t1, root, matcher);
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1786(this, matcher, t);
			}

			private sealed class _SearchNodeIterator_1786 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1786(UnbrokenCategoryIsDominatedBy _enclosing, TregexMatcher matcher, Tree t)
				{
					this._enclosing = _enclosing;
					this.matcher = matcher;
					this.t = t;
				}

				internal override void Initialize()
				{
					this.next = matcher.GetParent(t);
				}

				internal override void Advance()
				{
					if (this._enclosing.unbrokenCategoryDominates.PathMatchesNode(this.next))
					{
						this.next = matcher.GetParent(this.next);
					}
					else
					{
						this.next = null;
					}
				}

				private readonly UnbrokenCategoryIsDominatedBy _enclosing;

				private readonly TregexMatcher matcher;

				private readonly Tree t;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Relation.UnbrokenCategoryIsDominatedBy))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Relation.UnbrokenCategoryIsDominatedBy unbrokenCategoryIsDominatedBy = (Relation.UnbrokenCategoryIsDominatedBy)o;
				return unbrokenCategoryDominates.Equals(unbrokenCategoryIsDominatedBy.unbrokenCategoryDominates);
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 29 * result + unbrokenCategoryDominates.GetHashCode();
				return result;
			}
		}

		/// <summary>Note that this only works properly for context-free trees.</summary>
		/// <remarks>
		/// Note that this only works properly for context-free trees.
		/// Also, the use of initialize and advance is not very efficient just yet.  Finally, each node in the tree
		/// is added only once, even if there is more than one unbroken-category precedence path to it.
		/// </remarks>
		[System.Serializable]
		private class UnbrokenCategoryPrecedes : Relation
		{
			private const long serialVersionUID = 6866888667804306111L;

			private readonly Pattern pattern;

			private readonly bool negatedPattern;

			private readonly bool basicCat;

			private Func<string, string> basicCatFunction;

			/// <param name="arg">The pattern to match, perhaps preceded by ! and/or @</param>
			internal UnbrokenCategoryPrecedes(string arg, Func<string, string> basicCatFunction)
				: base(".+(" + arg + ')')
			{
				if (arg.StartsWith("!"))
				{
					negatedPattern = true;
					arg = Sharpen.Runtime.Substring(arg, 1);
				}
				else
				{
					negatedPattern = false;
				}
				if (arg.StartsWith("@"))
				{
					basicCat = true;
					this.basicCatFunction = basicCatFunction;
					// todo -- this was missing a this. which must be testable in a unit test!!! Make one
					arg = Sharpen.Runtime.Substring(arg, 1);
				}
				else
				{
					basicCat = false;
				}
				if (arg.Matches("/.*/"))
				{
					pattern = Pattern.Compile(Sharpen.Runtime.Substring(arg, 1, arg.Length - 1));
				}
				else
				{
					if (arg.Matches("__"))
					{
						pattern = Pattern.Compile("^.*$");
					}
					else
					{
						pattern = Pattern.Compile("^(?:" + arg + ")$");
					}
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return true;
			}

			// shouldn't have to do anything here.
			private bool PathMatchesNode(Tree node)
			{
				string lab = node.Value();
				// added this code to not crash if null node, even though there probably should be null nodes in the tree
				if (lab == null)
				{
					// Say that a null label matches no positive pattern, but any negated pattern
					return negatedPattern;
				}
				else
				{
					if (basicCat)
					{
						lab = basicCatFunction.Apply(lab);
					}
					Matcher m = pattern.Matcher(lab);
					return m.Find() != negatedPattern;
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_1897(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_1897 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_1897(UnbrokenCategoryPrecedes _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				private IdentityHashSet<Tree> nodesToSearch;

				private Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.nodesToSearch = new IdentityHashSet<Tree>();
					this.searchStack = new Stack<Tree>();
					this.InitializeHelper(this.searchStack, t, matcher.GetRoot());
					this.Advance();
				}

				private void InitializeHelper(Stack<Tree> stack, Tree node, Tree root)
				{
					if (node == root)
					{
						return;
					}
					Tree parent = matcher.GetParent(node);
					int i = parent.ObjectIndexOf(node);
					while (i == parent.Children().Length - 1 && parent != root)
					{
						node = parent;
						parent = matcher.GetParent(parent);
						i = parent.ObjectIndexOf(node);
					}
					Tree followingNode;
					if (i + 1 < parent.Children().Length)
					{
						followingNode = parent.Children()[i + 1];
					}
					else
					{
						followingNode = null;
					}
					while (followingNode != null)
					{
						//log.info("adding to stack node " + followingNode.toString());
						if (!this.nodesToSearch.Contains(followingNode))
						{
							stack.Add(followingNode);
							this.nodesToSearch.Add(followingNode);
						}
						if (this._enclosing.PathMatchesNode(followingNode))
						{
							this.InitializeHelper(stack, followingNode, root);
						}
						if (!followingNode.IsLeaf())
						{
							followingNode = followingNode.Children()[0];
						}
						else
						{
							followingNode = null;
						}
					}
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
					}
				}

				private readonly UnbrokenCategoryPrecedes _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}

		/// <summary>Note that this only works properly for context-free trees.</summary>
		/// <remarks>
		/// Note that this only works properly for context-free trees.
		/// Also, the use of initialize and advance is not very efficient just yet.  Finally, each node in the tree
		/// is added only once, even if there is more than one unbroken-category precedence path to it.
		/// </remarks>
		[System.Serializable]
		private class UnbrokenCategoryFollows : Relation
		{
			private const long serialVersionUID = -7890430001297866437L;

			private readonly Pattern pattern;

			private readonly bool negatedPattern;

			private readonly bool basicCat;

			private Func<string, string> basicCatFunction;

			/// <param name="arg">The pattern to match, perhaps preceded by ! and/or @</param>
			internal UnbrokenCategoryFollows(string arg, Func<string, string> basicCatFunction)
				: base(",+(" + arg + ')')
			{
				if (arg.StartsWith("!"))
				{
					negatedPattern = true;
					arg = Sharpen.Runtime.Substring(arg, 1);
				}
				else
				{
					negatedPattern = false;
				}
				if (arg.StartsWith("@"))
				{
					basicCat = true;
					this.basicCatFunction = basicCatFunction;
					arg = Sharpen.Runtime.Substring(arg, 1);
				}
				else
				{
					basicCat = false;
				}
				if (arg.Matches("/.*/"))
				{
					pattern = Pattern.Compile(Sharpen.Runtime.Substring(arg, 1, arg.Length - 1));
				}
				else
				{
					if (arg.Matches("__"))
					{
						pattern = Pattern.Compile("^.*$");
					}
					else
					{
						pattern = Pattern.Compile("^(?:" + arg + ")$");
					}
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override bool Satisfies(Tree t1, Tree t2, Tree root, TregexMatcher matcher)
			{
				return true;
			}

			// shouldn't have to do anything here.
			private bool PathMatchesNode(Tree node)
			{
				string lab = node.Value();
				// added this code to not crash if null node, even though there probably should be null nodes in the tree
				if (lab == null)
				{
					// Say that a null label matches no positive pattern, but any negated pattern
					return negatedPattern;
				}
				else
				{
					if (basicCat)
					{
						lab = basicCatFunction.Apply(lab);
					}
					Matcher m = pattern.Matcher(lab);
					return m.Find() != negatedPattern;
				}
			}

			/// <summary>
			/// <inheritDoc/>
			/// 
			/// </summary>
			internal override IEnumerator<Tree> SearchNodeIterator(Tree t, TregexMatcher matcher)
			{
				return new _SearchNodeIterator_2023(this, t, matcher);
			}

			private sealed class _SearchNodeIterator_2023 : Relation.SearchNodeIterator
			{
				public _SearchNodeIterator_2023(UnbrokenCategoryFollows _enclosing, Tree t, TregexMatcher matcher)
				{
					this._enclosing = _enclosing;
					this.t = t;
					this.matcher = matcher;
				}

				internal IdentityHashSet<Tree> nodesToSearch;

				internal Stack<Tree> searchStack;

				internal override void Initialize()
				{
					this.nodesToSearch = new IdentityHashSet<Tree>();
					this.searchStack = new Stack<Tree>();
					this.InitializeHelper(this.searchStack, t, matcher.GetRoot());
					this.Advance();
				}

				private void InitializeHelper(Stack<Tree> stack, Tree node, Tree root)
				{
					if (node == root)
					{
						return;
					}
					Tree parent = matcher.GetParent(node);
					int i = parent.ObjectIndexOf(node);
					while (i == 0 && parent != root)
					{
						node = parent;
						parent = matcher.GetParent(parent);
						i = parent.ObjectIndexOf(node);
					}
					Tree precedingNode;
					if (i > 0)
					{
						precedingNode = parent.Children()[i - 1];
					}
					else
					{
						precedingNode = null;
					}
					while (precedingNode != null)
					{
						//log.info("adding to stack node " + precedingNode.toString());
						if (!this.nodesToSearch.Contains(precedingNode))
						{
							stack.Add(precedingNode);
							this.nodesToSearch.Add(precedingNode);
						}
						if (this._enclosing.PathMatchesNode(precedingNode))
						{
							this.InitializeHelper(stack, precedingNode, root);
						}
						if (!precedingNode.IsLeaf())
						{
							precedingNode = precedingNode.Children()[0];
						}
						else
						{
							precedingNode = null;
						}
					}
				}

				internal override void Advance()
				{
					if (this.searchStack.IsEmpty())
					{
						this.next = null;
					}
					else
					{
						this.next = this.searchStack.Pop();
					}
				}

				private readonly UnbrokenCategoryFollows _enclosing;

				private readonly Tree t;

				private readonly TregexMatcher matcher;
			}
		}
	}
}

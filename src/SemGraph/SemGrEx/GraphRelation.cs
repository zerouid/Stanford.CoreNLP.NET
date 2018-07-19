using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>An abstract base class for relations between graph nodes in semgrex.</summary>
	/// <remarks>
	/// An abstract base class for relations between graph nodes in semgrex. There
	/// are two types of subclasses: static anonymous singleton instantiations for
	/// relations that do not require arguments, and private subclasses for those
	/// with arguments. All invocations should be made through the static factory
	/// methods.
	/// <p/>
	/// If you want to add a new relation, you just have to fill in the definition of
	/// <code>satisfies()</code> and <code>searchNodeIterator()</code>. Also be
	/// careful to make the appropriate adjustments to
	/// <code>getRelation()</code>. Finally, if you are using the SemgrexParser, you
	/// need to add the new relation symbol to the list of tokens. <p/>
	/// </remarks>
	/// <author>Chloe Kiddon</author>
	[System.Serializable]
	internal abstract class GraphRelation
	{
		internal readonly string symbol;

		internal readonly IPredicate<string> type;

		internal readonly string rawType;

		internal readonly string name;

		//"<" | ">" | ">>" | "<<" | "<#" | ">#" | ":" | "@">
		/// <summary>
		/// Returns <code>true</code> iff this <code>GraphRelation</code> holds between
		/// the given pair of nodes in the given semantic graph.
		/// </summary>
		internal abstract bool Satisfies(IndexedWord n1, IndexedWord n2, SemanticGraph sg);

		/// <summary>
		/// For a given node and its root, returns an
		/// <see cref="System.Collections.IEnumerator{E}"/>
		/// over the nodes
		/// of the semantic graph that satisfy the relation.
		/// </summary>
		internal abstract IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg);

		private GraphRelation(string symbol, string type, string name)
		{
			this.symbol = symbol;
			this.type = GetPattern(type);
			this.rawType = type;
			this.name = name;
		}

		private GraphRelation(string symbol, string type)
			: this(symbol, type, null)
		{
		}

		private GraphRelation(string symbol)
			: this(symbol, null)
		{
		}

		public override string ToString()
		{
			return symbol + ((rawType != null) ? rawType : string.Empty) + ((name != null) ? "=" + name : string.Empty);
		}

		public virtual IPredicate<string> GetPattern(string relnType)
		{
			if ((relnType == null) || (relnType.Equals(string.Empty)))
			{
				return Filters.AcceptFilter();
			}
			else
			{
				if (relnType.Matches("/.*/"))
				{
					return new RegexStringFilter(Sharpen.Runtime.Substring(relnType, 1, relnType.Length - 1));
				}
				else
				{
					// raw description
					return new ArrayStringFilter(ArrayStringFilter.Mode.Exact, relnType);
				}
			}
		}

		public virtual string GetName()
		{
			if (name == null || name == string.Empty)
			{
				return null;
			}
			return name;
		}

		[System.Serializable]
		internal class ALIGNMENT : GraphRelation
		{
			private Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment alignment;

			private bool hypToText;

			internal ALIGNMENT()
				: base("@", string.Empty)
			{
				// ALIGNMENT graph relation: "@" ==============================================
				hypToText = true;
			}

			internal virtual void SetAlignment(Edu.Stanford.Nlp.Semgraph.Semgrex.Alignment alignment, bool hypToText, GraphRelation.SearchNodeIterator itr)
			{
				this.alignment = alignment;
				this.hypToText = hypToText;
				//log.info("setting alignment");
				itr.Advance();
			}

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				if (alignment == null)
				{
					return false;
				}
				if (hypToText)
				{
					return (alignment.GetMap()[l1]).Equals(l2);
				}
				else
				{
					return (alignment.GetMap()[l2]).Equals(l1);
				}
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_124(this, node);
			}

			private sealed class _SearchNodeIterator_124 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_124(ALIGNMENT _enclosing, IndexedWord node)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.foundOnce = false;
				}

				internal bool foundOnce;

				internal int nextNum;

				// not really initialized until alignment is set
				internal override void Initialize()
				{
				}

				internal override void Advance()
				{
					if (this._enclosing.alignment == null)
					{
						return;
					}
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
					}
					//log.info("node: " + node.word());
					if (this._enclosing.hypToText)
					{
						if (!this.foundOnce)
						{
							this.next = this._enclosing.alignment.GetMap()[node];
							this.foundOnce = true;
						}
						else
						{
							//  if (next == null) log.info("no alignment"); else
							// log.info("other graph: " + next.word());
							this.next = null;
						}
					}
					else
					{
						//log.info("next: null");
						int num = 0;
						foreach (KeyValuePair<IndexedWord, IndexedWord> pair in this._enclosing.alignment.GetMap())
						{
							if (pair.Value.Equals(node))
							{
								if (this.nextNum == num)
								{
									this.next = pair.Key;
									this.nextNum++;
									//log.info("next: " + next.word());
									return;
								}
								num++;
							}
						}
						//log.info("backwards, next: null");
						this.next = null;
					}
				}

				private readonly ALIGNMENT _enclosing;

				private readonly IndexedWord node;
			}

			private const long serialVersionUID = -2936526066368043778L;
			// Generated automatically by Eclipse
		}

		private sealed class _GraphRelation_180 : GraphRelation
		{
			public _GraphRelation_180(string baseArg1, string baseArg2)
				: base(baseArg1, baseArg2)
			{
				this.serialVersionUID = 4710135995247390313L;
			}

			// ROOT graph relation: "Root" ================================================
			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				return l1 == l2;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_188(node);
			}

			private sealed class _SearchNodeIterator_188 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_188(IndexedWord node)
				{
					this.node = node;
				}

				internal override void Initialize()
				{
					this.next = node;
				}

				private readonly IndexedWord node;
			}

			private const long serialVersionUID;
		}

		internal static readonly GraphRelation Root = new _GraphRelation_180(string.Empty, string.Empty);

		private sealed class _GraphRelation_199 : GraphRelation
		{
			public _GraphRelation_199(string baseArg1, string baseArg2)
				: base(baseArg1, baseArg2)
			{
				this.serialVersionUID = 5259713498453659251L;
			}

			// automatically generated by Eclipse
			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				return true;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return sg.VertexSet().GetEnumerator();
			}

			private const long serialVersionUID;
		}

		internal static readonly GraphRelation Iterator = new _GraphRelation_199(":", string.Empty);

		private sealed class _GraphRelation_216 : GraphRelation
		{
			public _GraphRelation_216(string baseArg1, string baseArg2)
				: base(baseArg1, baseArg2)
			{
				this.serialVersionUID = -3088857488269777611L;
			}

			// automatically generated by Eclipse
			// ALIGNED_ROOT graph relation: "AlignRoot" ===================================
			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				return l1 == l2;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_224(node);
			}

			private sealed class _SearchNodeIterator_224 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_224(IndexedWord node)
				{
					this.node = node;
				}

				internal override void Initialize()
				{
					this.next = node;
				}

				private readonly IndexedWord node;
			}

			private const long serialVersionUID;
		}

		internal static readonly GraphRelation AlignedRoot = new _GraphRelation_216("AlignRoot", string.Empty);

		[System.Serializable]
		private class GOVERNER : GraphRelation
		{
			internal GOVERNER(string reln, string name)
				: base(">", reln, name)
			{
			}

			// automatically generated by Eclipse
			// GOVERNOR graph relation: ">" ===============================================
			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				IList<Pair<GrammaticalRelation, IndexedWord>> deps = sg.ChildPairs(l1);
				foreach (Pair<GrammaticalRelation, IndexedWord> dep in deps)
				{
					if (this.type.Test(dep.First().ToString()) && dep.Second().Equals(l2))
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_258(this, node, sg);
			}

			private sealed class _SearchNodeIterator_258 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_258(GOVERNER _enclosing, IndexedWord node, SemanticGraph sg)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.sg = sg;
				}

				internal IEnumerator<SemanticGraphEdge> iterator;

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					if (this.iterator == null)
					{
						this.iterator = sg.OutgoingEdgeIterator(node);
					}
					while (this.iterator.MoveNext())
					{
						SemanticGraphEdge edge = this.iterator.Current;
						this.relation = edge.GetRelation().ToString();
						if (!this._enclosing.type.Test(this.relation))
						{
							continue;
						}
						this.next = edge.GetTarget();
						return;
					}
					this.next = null;
				}

				private readonly GOVERNER _enclosing;

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}

			private const long serialVersionUID = -7003148918274183951L;
			// automatically generated by Eclipse
		}

		[System.Serializable]
		private class DEPENDENT : GraphRelation
		{
			internal DEPENDENT(string reln, string name)
				: base("<", reln, name)
			{
			}

			// DEPENDENT graph relation: "<" ===============================================
			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				if (l1.Equals(IndexedWord.NoWord) || l2.Equals(IndexedWord.NoWord))
				{
					return false;
				}
				IList<Pair<GrammaticalRelation, IndexedWord>> govs = sg.ParentPairs(l1);
				foreach (Pair<GrammaticalRelation, IndexedWord> gov in govs)
				{
					if (this.type.Test(gov.First().ToString()) && gov.Second().Equals(l2))
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_310(this, node, sg);
			}

			private sealed class _SearchNodeIterator_310 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_310(DEPENDENT _enclosing, IndexedWord node, SemanticGraph sg)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.sg = sg;
				}

				internal int nextNum;

				// subtle bug warning here: if we use int nextNum=0;
				// instead,
				// we get the first daughter twice because the assignment occurs after
				// advance() has already been
				// called once by the constructor of SearchNodeIterator.
				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					IList<Pair<GrammaticalRelation, IndexedWord>> govs = sg.ParentPairs(node);
					while (this.nextNum < govs.Count && !this._enclosing.type.Test(govs[this.nextNum].First().ToString()))
					{
						this.nextNum++;
					}
					if (this.nextNum < govs.Count)
					{
						this.next = govs[this.nextNum].Second();
						this.relation = govs[this.nextNum].First().ToString();
						this.nextNum++;
					}
					else
					{
						this.next = null;
					}
				}

				private readonly DEPENDENT _enclosing;

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}

			private const long serialVersionUID = -5115389883698108694L;
			// automatically generated by Eclipse
		}

		[System.Serializable]
		private class LIMITED_GRANDPARENT : GraphRelation
		{
			internal readonly int startDepth;

			internal readonly int endDepth;

			internal LIMITED_GRANDPARENT(string reln, string name, int startDepth, int endDepth)
				: base(startDepth + "," + endDepth + ">>", reln, name)
			{
				this.startDepth = startDepth;
				this.endDepth = endDepth;
			}

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				if (l1.Equals(IndexedWord.NoWord) || l2.Equals(IndexedWord.NoWord))
				{
					return false;
				}
				IList<ICollection<IndexedWord>> usedNodes = new List<ICollection<IndexedWord>>();
				for (int i = 0; i <= endDepth; ++i)
				{
					usedNodes.Add(Generics.NewIdentityHashSet<IndexedWord>());
				}
				return l1 != l2 && SatisfyHelper(l1, l2, sg, 0, usedNodes);
			}

			private bool SatisfyHelper(IndexedWord parent, IndexedWord l2, SemanticGraph sg, int depth, IList<ICollection<IndexedWord>> usedNodes)
			{
				IList<Pair<GrammaticalRelation, IndexedWord>> deps = sg.ChildPairs(parent);
				if (depth + 1 > endDepth)
				{
					return false;
				}
				if (depth + 1 >= startDepth)
				{
					foreach (Pair<GrammaticalRelation, IndexedWord> dep in deps)
					{
						if (this.type.Test(dep.First().ToString()) && dep.Second().Equals(l2))
						{
							return true;
						}
					}
				}
				usedNodes[depth].Add(parent);
				foreach (Pair<GrammaticalRelation, IndexedWord> dep_1 in deps)
				{
					if ((usedNodes.Count < depth + 1 || !usedNodes[depth + 1].Contains(dep_1.Second())) && SatisfyHelper(dep_1.Second(), l2, sg, depth + 1, usedNodes))
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_395(this, node, sg);
			}

			private sealed class _SearchNodeIterator_395 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_395(LIMITED_GRANDPARENT _enclosing, IndexedWord node, SemanticGraph sg)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.sg = sg;
				}

				internal IList<Stack<Pair<GrammaticalRelation, IndexedWord>>> searchStack;

				internal IList<ICollection<IndexedWord>> seenNodes;

				internal ICollection<IndexedWord> returnedNodes;

				internal int currentDepth;

				internal override void Initialize()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					this.searchStack = Generics.NewArrayList();
					for (int i = 0; i <= this._enclosing.endDepth; ++i)
					{
						this.searchStack.Add(new Stack<Pair<GrammaticalRelation, IndexedWord>>());
					}
					this.seenNodes = new List<ICollection<IndexedWord>>();
					for (int i_1 = 0; i_1 <= this._enclosing.endDepth; ++i_1)
					{
						this.seenNodes.Add(Generics.NewIdentityHashSet<IndexedWord>());
					}
					this.returnedNodes = Generics.NewIdentityHashSet();
					this.currentDepth = 1;
					IList<Pair<GrammaticalRelation, IndexedWord>> children = sg.ChildPairs(node);
					for (int i_2 = children.Count - 1; i_2 >= 0; i_2--)
					{
						this.searchStack[1].Push(children[i_2]);
					}
					if (!this.searchStack[1].IsEmpty())
					{
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					Pair<GrammaticalRelation, IndexedWord> nextPair;
					while (this.currentDepth <= this._enclosing.endDepth)
					{
						Stack<Pair<GrammaticalRelation, IndexedWord>> thisStack = this.searchStack[this.currentDepth];
						ICollection<IndexedWord> thisSeen = this.seenNodes[this.currentDepth];
						Stack<Pair<GrammaticalRelation, IndexedWord>> nextStack;
						ICollection<IndexedWord> nextSeen;
						if (this.currentDepth < this._enclosing.endDepth)
						{
							nextStack = this.searchStack[this.currentDepth + 1];
							nextSeen = this.seenNodes[this.currentDepth + 1];
						}
						else
						{
							nextStack = null;
							nextSeen = null;
						}
						while (!thisStack.IsEmpty())
						{
							nextPair = thisStack.Pop();
							if (thisSeen.Contains(nextPair.Second()))
							{
								continue;
							}
							thisSeen.Add(nextPair.Second());
							IList<Pair<GrammaticalRelation, IndexedWord>> children = sg.ChildPairs(nextPair.Second());
							for (int i = children.Count - 1; i >= 0; i--)
							{
								if (nextSeen != null && !nextSeen.Contains(children[i].Second()))
								{
									nextStack.Push(children[i]);
								}
							}
							if (this.currentDepth >= this._enclosing.startDepth && this._enclosing.type.Test(nextPair.First().ToString()) && !this.returnedNodes.Contains(nextPair.Second()))
							{
								this.next = nextPair.Second();
								this.relation = nextPair.First().ToString();
								this.returnedNodes.Add(nextPair.Second());
								return;
							}
						}
						// didn't see anything at this depth, move to the next depth
						++this.currentDepth;
					}
					// oh well, fell through with no results
					this.next = null;
				}

				private readonly LIMITED_GRANDPARENT _enclosing;

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}

			private const long serialVersionUID = 1L;
			// automatically generated by Eclipse
		}

		/// <summary>
		/// Factored out the common code from GRANDKID and GRANDPARENT
		/// <br />
		/// In general, the only differences are which ways to go on edges,
		/// so that is gotten through abstract methods
		/// </summary>
		[System.Serializable]
		private abstract class GRANDSOMETHING : GraphRelation
		{
			internal GRANDSOMETHING(string symbol, string reln, string name)
				: base(symbol, reln, name)
			{
			}

			internal abstract IList<Pair<GrammaticalRelation, IndexedWord>> GetNeighborPairs(SemanticGraph sg, IndexedWord node);

			internal abstract IEnumerator<SemanticGraphEdge> NeighborIterator(SemanticGraph sg, IndexedWord search);

			internal abstract IndexedWord FollowEdge(SemanticGraphEdge edge);

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				return l1 != l2 && SatisfyHelper(l1, l2, sg, Generics.NewIdentityHashSet<IndexedWord>());
			}

			private bool SatisfyHelper(IndexedWord node, IndexedWord l2, SemanticGraph sg, ICollection<IndexedWord> usedNodes)
			{
				IList<Pair<GrammaticalRelation, IndexedWord>> govs = GetNeighborPairs(sg, node);
				foreach (Pair<GrammaticalRelation, IndexedWord> gov in govs)
				{
					if (this.type.Test(gov.First().ToString()) && gov.Second().Equals(l2))
					{
						return true;
					}
				}
				usedNodes.Add(node);
				foreach (Pair<GrammaticalRelation, IndexedWord> gov_1 in govs)
				{
					if (!usedNodes.Contains(gov_1.Second()) && SatisfyHelper(gov_1.Second(), l2, sg, usedNodes))
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_524(this, node, sg);
			}

			private sealed class _SearchNodeIterator_524 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_524(GRANDSOMETHING _enclosing, IndexedWord node, SemanticGraph sg)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.sg = sg;
				}

				internal Stack<IndexedWord> searchStack;

				internal ICollection<IndexedWord> searchedNodes;

				internal ICollection<IndexedWord> matchedNodes;

				internal IEnumerator<SemanticGraphEdge> neighborIterator;

				internal override void Initialize()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					this.neighborIterator = null;
					this.searchedNodes = Generics.NewIdentityHashSet();
					this.matchedNodes = Generics.NewIdentityHashSet();
					this.searchStack = Generics.NewStack();
					this.searchStack.Push(node);
					this.Advance();
				}

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					while (!this.searchStack.IsEmpty())
					{
						if (this.neighborIterator == null || !this.neighborIterator.MoveNext())
						{
							IndexedWord search = this.searchStack.Pop();
							this.neighborIterator = this._enclosing.NeighborIterator(sg, search);
						}
						while (this.neighborIterator.MoveNext())
						{
							SemanticGraphEdge edge = this.neighborIterator.Current;
							IndexedWord otherEnd = this._enclosing.FollowEdge(edge);
							if (!this.searchedNodes.Contains(otherEnd))
							{
								this.searchStack.Push(otherEnd);
								this.searchedNodes.Add(otherEnd);
							}
							if (this._enclosing.type.Test(edge.GetRelation().ToString()) && !this.matchedNodes.Contains(otherEnd))
							{
								this.matchedNodes.Add(otherEnd);
								this.next = otherEnd;
								this.relation = edge.GetRelation().ToString();
								return;
							}
						}
					}
					// oh well, fell through with no results
					this.next = null;
				}

				private readonly GRANDSOMETHING _enclosing;

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}

			private const long serialVersionUID = 1L;
			// automatically generated by Eclipse
		}

		[System.Serializable]
		private class GRANDPARENT : GraphRelation.GRANDSOMETHING
		{
			internal GRANDPARENT(string reln, string name)
				: base(">>", reln, name)
			{
			}

			// GRANDPARENT graph relation: ">>" ===========================================
			internal override IList<Pair<GrammaticalRelation, IndexedWord>> GetNeighborPairs(SemanticGraph sg, IndexedWord node)
			{
				return sg.ChildPairs(node);
			}

			internal override IEnumerator<SemanticGraphEdge> NeighborIterator(SemanticGraph sg, IndexedWord search)
			{
				return sg.OutgoingEdgeIterator(search);
			}

			internal override IndexedWord FollowEdge(SemanticGraphEdge edge)
			{
				return edge.GetTarget();
			}

			private const long serialVersionUID = 1L;
			// automatically generated by Eclipse
		}

		[System.Serializable]
		private class GRANDKID : GraphRelation.GRANDSOMETHING
		{
			internal GRANDKID(string reln, string name)
				: base("<<", reln, name)
			{
			}

			// GRANDKID graph relation: "<<" ==============================================
			internal override IList<Pair<GrammaticalRelation, IndexedWord>> GetNeighborPairs(SemanticGraph sg, IndexedWord node)
			{
				return sg.ParentPairs(node);
			}

			internal override IEnumerator<SemanticGraphEdge> NeighborIterator(SemanticGraph sg, IndexedWord search)
			{
				return sg.IncomingEdgeIterator(search);
			}

			internal override IndexedWord FollowEdge(SemanticGraphEdge edge)
			{
				return edge.GetSource();
			}

			private const long serialVersionUID = 1L;
			// automatically generated by copying some other serialVersionUID
		}

		[System.Serializable]
		private class LIMITED_GRANDKID : GraphRelation
		{
			internal readonly int startDepth;

			internal readonly int endDepth;

			internal LIMITED_GRANDKID(string reln, string name, int startDepth, int endDepth)
				: base(startDepth + "," + endDepth + "<<", reln, name)
			{
				this.startDepth = startDepth;
				this.endDepth = endDepth;
			}

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				if (l1.Equals(IndexedWord.NoWord) || l2.Equals(IndexedWord.NoWord))
				{
					return false;
				}
				IList<ICollection<IndexedWord>> usedNodes = new List<ICollection<IndexedWord>>();
				for (int i = 0; i <= endDepth; ++i)
				{
					usedNodes.Add(Generics.NewIdentityHashSet<IndexedWord>());
				}
				return l1 != l2 && SatisfyHelper(l1, l2, sg, 0, usedNodes);
			}

			private bool SatisfyHelper(IndexedWord child, IndexedWord l2, SemanticGraph sg, int depth, IList<ICollection<IndexedWord>> usedNodes)
			{
				IList<Pair<GrammaticalRelation, IndexedWord>> deps = sg.ParentPairs(child);
				if (depth + 1 > endDepth)
				{
					return false;
				}
				if (depth + 1 >= startDepth)
				{
					foreach (Pair<GrammaticalRelation, IndexedWord> dep in deps)
					{
						if (this.type.Test(dep.First().ToString()) && dep.Second().Equals(l2))
						{
							return true;
						}
					}
				}
				usedNodes[depth].Add(child);
				foreach (Pair<GrammaticalRelation, IndexedWord> dep_1 in deps)
				{
					if ((usedNodes.Count < depth + 1 || !usedNodes[depth + 1].Contains(dep_1.Second())) && SatisfyHelper(dep_1.Second(), l2, sg, depth + 1, usedNodes))
					{
						return true;
					}
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_686(this, node, sg);
			}

			private sealed class _SearchNodeIterator_686 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_686(LIMITED_GRANDKID _enclosing, IndexedWord node, SemanticGraph sg)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.sg = sg;
				}

				internal IList<Stack<Pair<GrammaticalRelation, IndexedWord>>> searchStack;

				internal IList<ICollection<IndexedWord>> seenNodes;

				internal ICollection<IndexedWord> returnedNodes;

				internal int currentDepth;

				internal override void Initialize()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					this.searchStack = Generics.NewArrayList();
					for (int i = 0; i <= this._enclosing.endDepth; ++i)
					{
						this.searchStack.Add(new Stack<Pair<GrammaticalRelation, IndexedWord>>());
					}
					this.seenNodes = new List<ICollection<IndexedWord>>();
					for (int i_1 = 0; i_1 <= this._enclosing.endDepth; ++i_1)
					{
						this.seenNodes.Add(Generics.NewIdentityHashSet<IndexedWord>());
					}
					this.returnedNodes = Generics.NewIdentityHashSet();
					this.currentDepth = 1;
					IList<Pair<GrammaticalRelation, IndexedWord>> parents = sg.ParentPairs(node);
					for (int i_2 = parents.Count - 1; i_2 >= 0; i_2--)
					{
						this.searchStack[1].Push(parents[i_2]);
					}
					if (!this.searchStack[1].IsEmpty())
					{
						this.Advance();
					}
				}

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					Pair<GrammaticalRelation, IndexedWord> nextPair;
					while (this.currentDepth <= this._enclosing.endDepth)
					{
						Stack<Pair<GrammaticalRelation, IndexedWord>> thisStack = this.searchStack[this.currentDepth];
						ICollection<IndexedWord> thisSeen = this.seenNodes[this.currentDepth];
						Stack<Pair<GrammaticalRelation, IndexedWord>> nextStack;
						ICollection<IndexedWord> nextSeen;
						if (this.currentDepth < this._enclosing.endDepth)
						{
							nextStack = this.searchStack[this.currentDepth + 1];
							nextSeen = this.seenNodes[this.currentDepth + 1];
						}
						else
						{
							nextStack = null;
							nextSeen = null;
						}
						while (!thisStack.IsEmpty())
						{
							nextPair = thisStack.Pop();
							if (thisSeen.Contains(nextPair.Second()))
							{
								continue;
							}
							thisSeen.Add(nextPair.Second());
							IList<Pair<GrammaticalRelation, IndexedWord>> parents = sg.ParentPairs(nextPair.Second());
							for (int i = parents.Count - 1; i >= 0; i--)
							{
								if (nextSeen != null && !nextSeen.Contains(parents[i].Second()))
								{
									nextStack.Push(parents[i]);
								}
							}
							if (this.currentDepth >= this._enclosing.startDepth && this._enclosing.type.Test(nextPair.First().ToString()) && !this.returnedNodes.Contains(nextPair.Second()))
							{
								this.returnedNodes.Add(nextPair.Second());
								this.next = nextPair.Second();
								this.relation = nextPair.First().ToString();
								return;
							}
						}
						// didn't see anything at this depth, move to the next depth
						++this.currentDepth;
					}
					// oh well, fell through with no results
					this.next = null;
				}

				private readonly LIMITED_GRANDKID _enclosing;

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}

			private const long serialVersionUID = 1L;
			// automatically generated by Eclipse
		}

		[System.Serializable]
		private class EQUALS : GraphRelation
		{
			internal EQUALS(string reln, string name)
				: base("==", reln, name)
			{
			}

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				if (l1 == l2)
				{
					return true;
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_788(node);
			}

			private sealed class _SearchNodeIterator_788 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_788(IndexedWord node)
				{
					this.node = node;
				}

				internal bool alreadyIterated;

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					if (this.alreadyIterated)
					{
						this.next = null;
						return;
					}
					this.alreadyIterated = true;
					this.next = node;
					return;
				}

				private readonly IndexedWord node;
			}
		}

		[System.Serializable]
		private abstract class SIBLING_RELATION : GraphRelation
		{
			private const long serialVersionUID = 1L;

			internal SIBLING_RELATION(string symbol, string reln, string name)
				: base(symbol, reln, name)
			{
			}

			internal abstract bool SatisfiesOrder(IndexedWord l1, IndexedWord l2);

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				IndexedWord parent = sg.GetCommonAncestor(l1, l2);
				ICollection<IndexedWord> l1Parents = sg.GetParents(l1);
				if (parent != null && l1Parents.Contains(parent) && SatisfiesOrder(l1, l2))
				{
					return true;
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_833(this, node, sg);
			}

			private sealed class _SearchNodeIterator_833 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_833(SIBLING_RELATION _enclosing, IndexedWord node, SemanticGraph sg)
				{
					this._enclosing = _enclosing;
					this.node = node;
					this.sg = sg;
				}

				internal IEnumerator<IndexedWord> iterator;

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					if (this.iterator == null)
					{
						ICollection<IndexedWord> parents = sg.GetParents(node);
						ICollection<IndexedWord> neighbors = Generics.NewIdentityHashSet();
						foreach (IndexedWord parent in parents)
						{
							Sharpen.Collections.AddAll(neighbors, sg.GetChildren(parent));
						}
						this.iterator = neighbors.GetEnumerator();
					}
					while (this.iterator.MoveNext())
					{
						IndexedWord word = this.iterator.Current;
						if (!this._enclosing.SatisfiesOrder(node, word))
						{
							continue;
						}
						this.next = word;
						return;
					}
					this.next = null;
				}

				private readonly SIBLING_RELATION _enclosing;

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}
		}

		[System.Serializable]
		private class RIGHT_IMMEDIATE_SIBLING : GraphRelation.SIBLING_RELATION
		{
			internal RIGHT_IMMEDIATE_SIBLING(string reln, string name)
				: base("$+", reln, name)
			{
			}

			private const long serialVersionUID = 1L;

			internal override bool SatisfiesOrder(IndexedWord l1, IndexedWord l2)
			{
				return (l1.Index() == (l2.Index() - 1));
			}
		}

		[System.Serializable]
		private class LEFT_IMMEDIATE_SIBLING : GraphRelation.SIBLING_RELATION
		{
			internal LEFT_IMMEDIATE_SIBLING(string reln, string name)
				: base("$-", reln, name)
			{
			}

			private const long serialVersionUID = 1L;

			internal override bool SatisfiesOrder(IndexedWord l1, IndexedWord l2)
			{
				return (l1.Index() == (l2.Index() + 1));
			}
		}

		[System.Serializable]
		private class RIGHT_SIBLING : GraphRelation.SIBLING_RELATION
		{
			internal RIGHT_SIBLING(string reln, string name)
				: base("$++", reln, name)
			{
			}

			private const long serialVersionUID = 1L;

			internal override bool SatisfiesOrder(IndexedWord l1, IndexedWord l2)
			{
				return (l1.Index() < l2.Index());
			}
		}

		[System.Serializable]
		private class LEFT_SIBLING : GraphRelation.SIBLING_RELATION
		{
			internal LEFT_SIBLING(string reln, string name)
				: base("$--", reln, name)
			{
			}

			private const long serialVersionUID = 1L;

			internal override bool SatisfiesOrder(IndexedWord l1, IndexedWord l2)
			{
				return (l1.Index() > l2.Index());
			}
		}

		[System.Serializable]
		private class ADJACENT_NODE : GraphRelation
		{
			private const long serialVersionUID = 1L;

			internal ADJACENT_NODE(string reln, string name)
				: base(".", reln, name)
			{
			}

			internal override bool Satisfies(IndexedWord l1, IndexedWord l2, SemanticGraph sg)
			{
				if (l1.Index() == (l2.Index() - 1))
				{
					return true;
				}
				return false;
			}

			internal override IEnumerator<IndexedWord> SearchNodeIterator(IndexedWord node, SemanticGraph sg)
			{
				return new _SearchNodeIterator_939(node, sg);
			}

			private sealed class _SearchNodeIterator_939 : GraphRelation.SearchNodeIterator
			{
				public _SearchNodeIterator_939(IndexedWord node, SemanticGraph sg)
				{
					this.node = node;
					this.sg = sg;
				}

				internal IEnumerator<IndexedWord> iterator;

				internal override void Advance()
				{
					if (node.Equals(IndexedWord.NoWord))
					{
						this.next = null;
						return;
					}
					if (this.iterator == null)
					{
						this.iterator = sg.VertexSet().GetEnumerator();
					}
					while (this.iterator.MoveNext())
					{
						IndexedWord word = this.iterator.Current;
						if (node.Index() != (word.Index() - 1))
						{
							continue;
						}
						this.next = word;
						return;
					}
					this.next = null;
				}

				private readonly IndexedWord node;

				private readonly SemanticGraph sg;
			}
		}

		// ============================================================================
		public static bool IsKnownRelation(string reln)
		{
			return (reln.Equals(">") || reln.Equals("<") || reln.Equals(">>") || reln.Equals("<<") || reln.Equals("@") || reln.Equals("==") || reln.Equals("$+") || reln.Equals("$++") || reln.Equals("$-") || reln.Equals("$--") || reln.Equals("."));
		}

		/// <exception cref="Edu.Stanford.Nlp.Semgraph.Semgrex.ParseException"/>
		public static GraphRelation GetRelation(string reln, string type, string name)
		{
			if (reln == null && type == null)
			{
				return null;
			}
			if (!IsKnownRelation(reln))
			{
				throw new ParseException("Unknown relation " + reln);
			}
			switch (reln)
			{
				case ">":
				{
					return new GraphRelation.GOVERNER(type, name);
				}

				case "<":
				{
					return new GraphRelation.DEPENDENT(type, name);
				}

				case ">>":
				{
					return new GraphRelation.GRANDPARENT(type, name);
				}

				case "<<":
				{
					return new GraphRelation.GRANDKID(type, name);
				}

				case "==":
				{
					return new GraphRelation.EQUALS(type, name);
				}

				case "$+":
				{
					return new GraphRelation.RIGHT_IMMEDIATE_SIBLING(type, name);
				}

				case "$-":
				{
					return new GraphRelation.LEFT_IMMEDIATE_SIBLING(type, name);
				}

				case "$++":
				{
					return new GraphRelation.RIGHT_SIBLING(type, name);
				}

				case "$--":
				{
					return new GraphRelation.LEFT_SIBLING(type, name);
				}

				case ".":
				{
					return new GraphRelation.ADJACENT_NODE(type, name);
				}

				case "@":
				{
					return new GraphRelation.ALIGNMENT();
				}

				default:
				{
					//error
					throw new ParseException("Relation " + reln + " not handled by getRelation");
				}
			}
		}

		/// <exception cref="Edu.Stanford.Nlp.Semgraph.Semgrex.ParseException"/>
		public static GraphRelation GetRelation(string reln, string type, int num, string name)
		{
			if (reln == null && type == null)
			{
				return null;
			}
			if (reln.Equals(">>"))
			{
				return new GraphRelation.LIMITED_GRANDPARENT(type, name, num, num);
			}
			else
			{
				if (reln.Equals("<<"))
				{
					return new GraphRelation.LIMITED_GRANDKID(type, name, num, num);
				}
				else
				{
					if (IsKnownRelation(reln))
					{
						throw new ParseException("Relation " + reln + " does not use numeric arguments");
					}
					else
					{
						//error
						throw new ParseException("Unrecognized compound relation " + reln + " " + type);
					}
				}
			}
		}

		/// <exception cref="Edu.Stanford.Nlp.Semgraph.Semgrex.ParseException"/>
		public static GraphRelation GetRelation(string reln, string type, int num, int num2, string name)
		{
			if (reln == null && type == null)
			{
				return null;
			}
			if (reln.Equals(">>"))
			{
				return new GraphRelation.LIMITED_GRANDPARENT(type, name, num, num2);
			}
			else
			{
				if (reln.Equals("<<"))
				{
					return new GraphRelation.LIMITED_GRANDKID(type, name, num, num2);
				}
				else
				{
					if (IsKnownRelation(reln))
					{
						throw new ParseException("Relation " + reln + " does not use numeric arguments");
					}
					else
					{
						//error
						throw new ParseException("Unrecognized compound relation " + reln + " " + type);
					}
				}
			}
		}

		public override int GetHashCode()
		{
			return symbol.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is GraphRelation))
			{
				return false;
			}
			GraphRelation relation = (GraphRelation)o;
			if (!symbol.Equals(relation.symbol) || !type.Equals(relation.type))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// This abstract Iterator implements a NULL iterator, but by subclassing and
		/// overriding advance and/or initialize, it is an efficient implementation.
		/// </summary>
		internal abstract class SearchNodeIterator : IEnumerator<IndexedWord>
		{
			public SearchNodeIterator()
			{
				Initialize();
			}

			/// <summary>
			/// This is the next node to be returned by the iterator, or null if there
			/// are no more items.
			/// </summary>
			internal IndexedWord next = null;

			/// <summary>Current relation string for next;</summary>
			internal string relation = null;

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

			public virtual IndexedWord Current
			{
				get
				{
					if (next == null)
					{
						return null;
					}
					IndexedWord ret = next;
					Advance();
					return ret;
				}
			}

			internal virtual string GetReln()
			{
				return relation;
			}

			public virtual void Remove()
			{
				throw new NotSupportedException("SearchNodeIterator does not support remove().");
			}
		}

		private const long serialVersionUID = -9128973950911993056L;
		// Automatically generated by Eclipse
	}
}

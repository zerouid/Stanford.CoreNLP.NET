using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <author>Chloe Kiddon</author>
	[System.Serializable]
	public class CoordinationPattern : SemgrexPattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.CoordinationPattern));

		private const long serialVersionUID = -3122330899634961002L;

		private bool isConj;

		private bool isNodeCoord;

		private IList<SemgrexPattern> children;

		public CoordinationPattern(bool isNodeCoord, IList<SemgrexPattern> children, bool isConj)
		{
			/* if isConj is true, then it is an "AND" ; if it is false, it is an "OR".*/
			/* if isNodeCoord is true, then it is a node coordination conj; if it is false, then
			* 	it is a relation coordination conj. */
			if (children.Count < 2)
			{
				throw new Exception("Coordination node must have at least 2 children.");
			}
			this.children = children;
			this.isConj = isConj;
			this.isNodeCoord = isNodeCoord;
		}

		public virtual bool IsNodeCoord()
		{
			return isNodeCoord;
		}

		internal override void SetChild(SemgrexPattern child)
		{
			if (isNodeCoord)
			{
				foreach (object c in children)
				{
					if (c is NodePattern)
					{
						((NodePattern)c).SetChild(child);
					}
				}
			}
		}

		public virtual void AddRelnToNodeCoord(SemgrexPattern child)
		{
			if (isNodeCoord)
			{
				foreach (SemgrexPattern c in children)
				{
					IList<SemgrexPattern> newChildren = new List<SemgrexPattern>();
					Sharpen.Collections.AddAll(newChildren, c.GetChildren());
					newChildren.Add(child);
					c.SetChild(new Edu.Stanford.Nlp.Semgraph.Semgrex.CoordinationPattern(false, newChildren, true));
				}
			}
		}

		internal override IList<SemgrexPattern> GetChildren()
		{
			return children;
		}

		internal override string LocalString()
		{
			StringBuilder sb = new StringBuilder();
			if (IsNegated())
			{
				sb.Append('!');
			}
			if (IsOptional())
			{
				sb.Append('?');
			}
			sb.Append((isConj ? "and" : "or"));
			sb.Append(" ");
			sb.Append((isNodeCoord ? "node coordination" : "reln coordination"));
			return sb.ToString();
		}

		public override string ToString()
		{
			return ToString(true);
		}

		public override string ToString(bool hasPrecedence)
		{
			StringBuilder sb = new StringBuilder();
			if (isConj)
			{
				foreach (SemgrexPattern node in children)
				{
					sb.Append(node.ToString());
				}
			}
			else
			{
				sb.Append('[');
				for (IEnumerator<SemgrexPattern> iter = children.GetEnumerator(); iter.MoveNext(); )
				{
					SemgrexPattern node = iter.Current;
					sb.Append(node.ToString());
					if (iter.MoveNext())
					{
						sb.Append(" |");
					}
				}
				sb.Append(']');
			}
			return sb.ToString();
		}

		internal override SemgrexMatcher Matcher(SemanticGraph sg, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings variableStrings, bool ignoreCase)
		{
			return new CoordinationPattern.CoordinationMatcher(this, sg, null, null, true, node, namesToNodes, namesToRelations, variableStrings, ignoreCase);
		}

		internal override SemgrexMatcher Matcher(SemanticGraph sg, Alignment alignment, SemanticGraph sg_align, bool hypToText, IndexedWord node, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings
			 variableStrings, bool ignoreCase)
		{
			return new CoordinationPattern.CoordinationMatcher(this, sg, alignment, sg_align, hypToText, node, namesToNodes, namesToRelations, variableStrings, ignoreCase);
		}

		private class CoordinationMatcher : SemgrexMatcher
		{
			private SemgrexMatcher[] children;

			private readonly CoordinationPattern myNode;

			private int currChild;

			private readonly bool considerAll;

			private IndexedWord nextNodeMatch = null;

			public CoordinationMatcher(CoordinationPattern c, SemanticGraph sg, Alignment alignment, SemanticGraph sg_align, bool hypToText, IndexedWord n, IDictionary<string, IndexedWord> namesToNodes, IDictionary<string, string> namesToRelations, VariableStrings
				 variableStrings, bool ignoreCase)
				: base(sg, alignment, sg_align, hypToText, n, namesToNodes, namesToRelations, variableStrings)
			{
				// do all con/dis-juncts have to be considered to determine a match?
				// i.e. true if conj and not negated or disj and negated
				myNode = c;
				children = new SemgrexMatcher[myNode.children.Count];
				for (int i = 0; i < children.Length; i++)
				{
					SemgrexPattern node = myNode.children[i];
					children[i] = node.Matcher(sg, alignment, sg_align, hypToText, n, namesToNodes, namesToRelations, variableStrings, ignoreCase);
				}
				currChild = 0;
				considerAll = myNode.isConj ^ myNode.IsNegated();
			}

			internal override void ResetChildIter()
			{
				currChild = 0;
				foreach (SemgrexMatcher aChildren in children)
				{
					aChildren.ResetChildIter();
				}
				nextNodeMatch = null;
			}

			internal override void ResetChildIter(IndexedWord node)
			{
				// this.tree = node;
				currChild = 0;
				foreach (SemgrexMatcher aChildren in children)
				{
					aChildren.ResetChildIter(node);
				}
			}

			// find the next local match
			public override bool Matches()
			{
				// also known as "FUN WITH LOGIC"
				//log.info(myNode.toString());
				//log.info("consider all: " + considerAll);
				if (considerAll)
				{
					// these are the cases where all children must be considered to match
					if (currChild < 0)
					{
						// a past call to this node either got that it failed
						// matching or that it was a negative match that succeeded,
						// which we only want to accept once
						return myNode.IsOptional();
					}
					// we must have happily reached the end of a match the last
					// time we were here
					if (currChild == children.Length)
					{
						--currChild;
					}
					while (true)
					{
						if (myNode.IsNegated() != children[currChild].Matches())
						{
							// This node is set correctly.  Move on to the next node
							++currChild;
							if (currChild == children.Length)
							{
								// yay, all nodes matched.
								if (myNode.IsNegated())
								{
									// a negated node should only match once (before being reset)
									currChild = -1;
								}
								else
								{
									if (myNode.isNodeCoord)
									{
										nextNodeMatch = children[0].GetMatch();
									}
								}
								return true;
							}
						}
						else
						{
							// oops, this didn't work.
							children[currChild].ResetChildIter();
							// go backwards to see if we can continue matching from an
							// earlier location.
							// TODO: perhaps there should be a version where we only
							// care about new assignments to the root, or new
							// assigments to the root and variables, in which case we
							// could make use of getChangesVariables() to optimize how
							// many nodes we can skip past
							--currChild;
							if (currChild < 0)
							{
								return myNode.IsOptional();
							}
						}
					}
				}
				else
				{
					// these are the cases where a single child node can make you match
					for (; currChild < children.Length; currChild++)
					{
						//   	namesToNodes.putAll(namesToNodesOld);
						//    namesToRelations.putAll(namesToRelationsOld);
						if (myNode.IsNegated() != children[currChild].Matches())
						{
							// a negated node should only match once (before being reset)
							if (myNode.IsNegated())
							{
								currChild = children.Length;
							}
							if (myNode.isNodeCoord)
							{
								nextNodeMatch = children[currChild].GetMatch();
							}
							//    this.namesToNodes.putAll(children[currChild].namesToNodes);
							//   this.namesToRelations.putAll(children[currChild].namesToRelations);
							return true;
						}
						children[currChild].ResetChildIter();
					}
					if (myNode.IsNegated())
					{
						currChild = children.Length;
					}
					return myNode.IsOptional();
				}
			}

			public override IndexedWord GetMatch()
			{
				if (myNode.isNodeCoord && !myNode.IsNegated())
				{
					return nextNodeMatch;
				}
				else
				{
					throw new NotSupportedException();
				}
			}

			public override string ToString()
			{
				string ret = "coordinate matcher for: ";
				foreach (SemgrexMatcher child in children)
				{
					ret += child.ToString() + " ";
				}
				return ret;
			}
		}
	}
}

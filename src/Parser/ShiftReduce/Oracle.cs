using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>
	/// Given a possibly incorrect partial parse of a sentence, returns a
	/// transition which gives the parser the best chance of getting a good
	/// score.
	/// </summary>
	/// <remarks>
	/// Given a possibly incorrect partial parse of a sentence, returns a
	/// transition which gives the parser the best chance of getting a good
	/// score.  Ideally we would actually return the transition which gets
	/// the best possible score, but that is not guaranteed.
	/// <br />
	/// If the partial parse is in the correct state, though, this will
	/// return the gold transition.
	/// <br />
	/// TODO: make sure all of the return values respect the logic for
	/// which transitions are and aren't legal.
	/// </remarks>
	/// <author>John Bauer</author>
	internal class Oracle
	{
		internal IList<Tree> binarizedTrees;

		internal IList<IdentityHashMap<Tree, Tree>> parentMaps;

		internal IList<IList<Tree>> leafLists;

		internal bool compoundUnaries;

		internal ICollection<string> rootStates;

		internal Oracle(IList<Tree> binarizedTrees, bool compoundUnaries, ICollection<string> rootStates)
		{
			this.binarizedTrees = binarizedTrees;
			parentMaps = Generics.NewArrayList(binarizedTrees.Count);
			leafLists = Generics.NewArrayList();
			foreach (Tree tree in binarizedTrees)
			{
				parentMaps.Add(BuildParentMap(tree));
				leafLists.Add(Edu.Stanford.Nlp.Trees.Trees.Leaves(tree));
			}
			this.compoundUnaries = compoundUnaries;
		}

		internal static IdentityHashMap<Tree, Tree> BuildParentMap(Tree tree)
		{
			IdentityHashMap<Tree, Tree> map = Generics.NewIdentityHashMap();
			BuildParentMapHelper(tree, null, map);
			return map;
		}

		internal static void BuildParentMapHelper(Tree tree, Tree parent, IdentityHashMap<Tree, Tree> map)
		{
			if (parent != null)
			{
				map[tree] = parent;
			}
			if (!tree.IsLeaf())
			{
				foreach (Tree child in tree.Children())
				{
					BuildParentMapHelper(child, tree, map);
				}
			}
		}

		/// <summary>
		/// Returns an attempt at a "gold" transition given the current state
		/// while parsing a known gold tree.
		/// </summary>
		/// <remarks>
		/// Returns an attempt at a "gold" transition given the current state
		/// while parsing a known gold tree.
		/// Tree is passed in by index so the oracle can precompute various
		/// statistics about the tree.
		/// If we already finalized, then the correct transition is to idle.
		/// If the stack is empty, shift is the only possible answer.
		/// If the first item on the stack is a correct span, correctly
		/// labeled, and it has unaries transitions above it, then if we are
		/// not doing compound unaries, the next unary up is the correct
		/// answer.  If we are doing compound unaries, and the state does not
		/// already have a transition, then the correct answer is a compound
		/// unary transition to the top of the unary chain.
		/// If the first item is the entire tree, with no remaining unary
		/// transitions, then we need to finalize.
		/// If the first item is a correct span, with or without a correct
		/// label, and there are no unary transitions to be added, then we
		/// must look at the next parent.  If it has the same left side, then
		/// we return a shift transition.  If it has the same right side,
		/// then we look at the next subtree on the stack (which must exist).
		/// If it is also correct, then the transition is to combine the two
		/// subtrees with the correct label and side.
		/// TODO: suppose the correct label is not either child label and the
		/// children are binarized states?  We should see what the
		/// debinarizer does in that case.  Perhaps a post-processing step
		/// If the previous stack item is too small, then any binary reduce
		/// action is legal, with no gold transition.  TODO: can this be improved?
		/// If the previous stack item is too large, perhaps because of
		/// incorrectly attached PP/SBAR, for example, we still need to
		/// binary reduce.  TODO: is that correct?  TODO: we could look back
		/// further in the stack to find hints at a label that would work
		/// better, for example
		/// If the current item is an incorrect span, then look at the
		/// containing item.  If it has the same left side, shift.  If it has
		/// the same right side, binary reduce (producing an exact span if
		/// possible).  If neither edge is correct, then any of shift or
		/// binary reduce are acceptable, with no gold transition.  TODO: can
		/// this be improved?
		/// </remarks>
		internal virtual OracleTransition GoldTransition(int index, State state)
		{
			if (state.finished)
			{
				return new OracleTransition(new IdleTransition(), false, false, false);
			}
			if (state.stack.Size() == 0)
			{
				return new OracleTransition(new ShiftTransition(), false, false, false);
			}
			IDictionary<Tree, Tree> parents = parentMaps[index];
			Tree gold = binarizedTrees[index];
			IList<Tree> leaves = leafLists[index];
			Tree S0 = state.stack.Peek();
			Tree enclosingS0 = GetEnclosingTree(S0, parents, leaves);
			OracleTransition result = GetUnaryTransition(S0, enclosingS0, parents, compoundUnaries);
			if (result != null)
			{
				return result;
			}
			// TODO: we could interject that all trees must end with ROOT, for example
			if (state.tokenPosition >= state.sentence.Count && state.stack.Size() == 1)
			{
				return new OracleTransition(new FinalizeTransition(rootStates), false, false, false);
			}
			if (state.stack.Size() == 1)
			{
				return new OracleTransition(new ShiftTransition(), false, false, false);
			}
			if (SpansEqual(S0, enclosingS0))
			{
				Tree parent = parents[enclosingS0];
				// cannot be root
				while (SpansEqual(parent, enclosingS0))
				{
					// in case we had missed unary transitions
					enclosingS0 = parent;
					parent = parents[parent];
				}
				if (parent.Children()[0] == enclosingS0)
				{
					// S0 is the left child of the correct tree
					return new OracleTransition(new ShiftTransition(), false, false, false);
				}
				// was the second (right) child.  there must be something else on the stack...
				Tree S1 = state.stack.Pop().Peek();
				Tree enclosingS1 = GetEnclosingTree(S1, parents, leaves);
				if (SpansEqual(S1, enclosingS1))
				{
					// the two subtrees should be combined
					return new OracleTransition(new BinaryTransition(parent.Value(), ShiftReduceUtils.GetBinarySide(parent)), false, false, false);
				}
				return new OracleTransition(null, false, true, false);
			}
			if (ShiftReduceUtils.LeftIndex(S0) == ShiftReduceUtils.LeftIndex(enclosingS0))
			{
				return new OracleTransition(new ShiftTransition(), false, false, false);
			}
			if (ShiftReduceUtils.RightIndex(S0) == ShiftReduceUtils.RightIndex(enclosingS0))
			{
				Tree S1 = state.stack.Pop().Peek();
				Tree enclosingS1 = GetEnclosingTree(S1, parents, leaves);
				if (enclosingS0 == enclosingS1)
				{
					// BinaryTransition with enclosingS0's label, either side, but preferring LEFT
					return new OracleTransition(new BinaryTransition(enclosingS0.Value(), BinaryTransition.Side.Left), false, false, true);
				}
				// S1 is smaller than the next tree S0 is supposed to be part of,
				// so we must have a BinaryTransition
				if (ShiftReduceUtils.LeftIndex(S1) > ShiftReduceUtils.LeftIndex(enclosingS0))
				{
					return new OracleTransition(null, false, true, true);
				}
				// S1 is larger than the next tree.  This is the worst case
				return new OracleTransition(null, true, true, true);
			}
			// S0 doesn't match either endpoint of the enclosing tree
			return new OracleTransition(null, true, true, true);
		}

		internal static Tree GetEnclosingTree(Tree subtree, IDictionary<Tree, Tree> parents, IList<Tree> leaves)
		{
			// TODO: make this more efficient
			int left = ShiftReduceUtils.LeftIndex(subtree);
			int right = ShiftReduceUtils.RightIndex(subtree);
			Tree gold = leaves[left];
			while (ShiftReduceUtils.RightIndex(gold) < right)
			{
				gold = parents[gold];
			}
			if (gold.IsLeaf())
			{
				gold = parents[gold];
			}
			return gold;
		}

		internal static bool SpansEqual(Tree subtree, Tree goldSubtree)
		{
			return ((ShiftReduceUtils.LeftIndex(subtree) == ShiftReduceUtils.LeftIndex(goldSubtree)) && (ShiftReduceUtils.RightIndex(subtree) == ShiftReduceUtils.RightIndex(goldSubtree)));
		}

		internal static OracleTransition GetUnaryTransition(Tree S0, Tree enclosingS0, IDictionary<Tree, Tree> parents, bool compoundUnaries)
		{
			if (!SpansEqual(S0, enclosingS0))
			{
				return null;
			}
			Tree parent = parents[enclosingS0];
			if (parent == null || parent.Children().Length != 1)
			{
				return null;
			}
			// TODO: should we allow @ here?  Handle @ in some other way?
			string value = S0.Value();
			// TODO: go up the parent chain
			// What we want is:
			// If the top of the stack is part of the unary chain, then parent should point to that subtree's parent.
			// If the top of the stack is not part of the unary chain,
			if (!enclosingS0.Value().Equals(value))
			{
				while (true)
				{
					enclosingS0 = parent;
					parent = parents[enclosingS0];
					if (enclosingS0.Value().Equals(value))
					{
						// We found the current stack top in the unary sequence ...
						if (parent == null || parent.Children().Length > 1)
						{
							// ... however, it was the root or the top of the unary sequence
							return null;
						}
						else
						{
							// ... not root or top of unary sequence, so create unary transitions based on this top
							break;
						}
					}
					else
					{
						if (parent == null)
						{
							// We went to the root without finding a match.
							// Treat the root as the unary transition to be made
							// TODO: correct logic?
							parent = enclosingS0;
							break;
						}
						else
						{
							if (parent.Children().Length > 1)
							{
								// We went off the top of the unary chain without finding a match.
								// Transition to the top of the unary chain without any subnodes
								parent = enclosingS0;
								break;
							}
						}
					}
				}
			}
			if (compoundUnaries)
			{
				IList<string> labels = Generics.NewArrayList();
				while (parent != null && parent.Children().Length == 1)
				{
					labels.Add(parent.Value());
					parent = parents[parent];
				}
				Java.Util.Collections.Reverse(labels);
				return new OracleTransition(new CompoundUnaryTransition(labels, false), false, false, false);
			}
			else
			{
				return new OracleTransition(new UnaryTransition(parent.Value(), false), false, false, false);
			}
		}
	}
}

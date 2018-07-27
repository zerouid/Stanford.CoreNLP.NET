using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public abstract class BaseModel
	{
		internal readonly ShiftReduceOptions op;

		internal readonly IIndex<ITransition> transitionIndex;

		internal readonly ICollection<string> knownStates;

		internal readonly ICollection<string> rootStates;

		internal readonly ICollection<string> rootOnlyStates;

		public BaseModel(ShiftReduceOptions op, IIndex<ITransition> transitionIndex, ICollection<string> knownStates, ICollection<string> rootStates, ICollection<string> rootOnlyStates)
		{
			// This is shared with the owning ShiftReduceParser (for now, at least)
			// the set of goal categories of a reduce = the set of phrasal categories in a grammar
			this.transitionIndex = transitionIndex;
			this.op = op;
			this.knownStates = knownStates;
			this.rootStates = rootStates;
			this.rootOnlyStates = rootOnlyStates;
		}

		public BaseModel(Edu.Stanford.Nlp.Parser.Shiftreduce.BaseModel other)
		{
			this.op = other.op;
			this.transitionIndex = other.transitionIndex;
			this.knownStates = other.knownStates;
			this.rootStates = other.rootStates;
			this.rootOnlyStates = other.rootOnlyStates;
		}

		/// <summary>
		/// Returns a transition which might not even be part of the model,
		/// but will hopefully allow progress in an otherwise stuck parse
		/// TODO: perhaps we want to create an EmergencyTransition class
		/// which indicates that something has gone wrong
		/// </summary>
		public virtual ITransition FindEmergencyTransition(State state, IList<ParserConstraint> constraints)
		{
			if (state.stack.Size() == 0)
			{
				return null;
			}
			// See if there is a constraint whose boundaries match the end
			// points of the top node on the stack.  If so, we can apply a
			// UnaryTransition / CompoundUnaryTransition if that would solve
			// the constraint
			if (constraints != null)
			{
				Tree top = state.stack.Peek();
				foreach (ParserConstraint constraint in constraints)
				{
					if (ShiftReduceUtils.LeftIndex(top) != constraint.start || ShiftReduceUtils.RightIndex(top) != constraint.end - 1)
					{
						continue;
					}
					if (ShiftReduceUtils.ConstraintMatchesTreeTop(top, constraint))
					{
						continue;
					}
					// found an unmatched constraint that can be fixed with a unary transition
					// now we need to find a matching state for the transition
					foreach (string label in knownStates)
					{
						if (constraint.state.Matcher(label).Matches())
						{
							return ((op.compoundUnaries) ? new CompoundUnaryTransition(Java.Util.Collections.SingletonList(label), false) : new UnaryTransition(label, false));
						}
					}
				}
			}
			if (ShiftReduceUtils.IsTemporary(state.stack.Peek()) && (state.stack.Size() == 1 || ShiftReduceUtils.IsTemporary(state.stack.Pop().Peek())))
			{
				return ((op.compoundUnaries) ? new CompoundUnaryTransition(Java.Util.Collections.SingletonList(Sharpen.Runtime.Substring(state.stack.Peek().Value(), 1)), false) : new UnaryTransition(Sharpen.Runtime.Substring(state.stack.Peek().Value(), 1), 
					false));
			}
			if (state.stack.Size() == 1 && state.tokenPosition >= state.sentence.Count)
			{
				// either need to finalize or transition to a root state
				if (!rootStates.Contains(state.stack.Peek().Value()))
				{
					string root = rootStates.GetEnumerator().Current;
					return ((op.compoundUnaries) ? new CompoundUnaryTransition(Java.Util.Collections.SingletonList(root), false) : new UnaryTransition(root, false));
				}
			}
			if (state.stack.Size() == 1)
			{
				return null;
			}
			if (ShiftReduceUtils.IsTemporary(state.stack.Peek()))
			{
				return new BinaryTransition(Sharpen.Runtime.Substring(state.stack.Peek().Value(), 1), BinaryTransition.Side.Right);
			}
			if (ShiftReduceUtils.IsTemporary(state.stack.Pop().Peek()))
			{
				return new BinaryTransition(Sharpen.Runtime.Substring(state.stack.Pop().Peek().Value(), 1), BinaryTransition.Side.Left);
			}
			return null;
		}

		public abstract ICollection<ScoredObject<int>> FindHighestScoringTransitions(State state, bool requireLegal, int numTransitions, IList<ParserConstraint> constraints);

		/// <summary>Train a new model.</summary>
		/// <remarks>
		/// Train a new model.  This is the method to override for new models
		/// such that the ShiftReduceParser will fill in the model.  Given a
		/// collection of training trees and some other various information,
		/// this should train a new model.  The model is expected to already
		/// know about the possible transitions and which states are eligible
		/// to be root states via the BaseModel constructor.
		/// </remarks>
		/// <param name="serializedPath">Where serialized models go.  If the appropriate options are set, the method can use this to save intermediate models.</param>
		/// <param name="tagger">The tagger to use when evaluating devTreebank.  TODO: it would make more sense for ShiftReduceParser to retag the trees first</param>
		/// <param name="random">A random number generator to use for any random numbers.  Useful to make sure results can be reproduced.</param>
		/// <param name="binarizedTrainTrees">The treebank to train from.</param>
		/// <param name="transitionLists">binarizedTrainTrees converted into lists of transitions that will reproduce the same tree.</param>
		/// <param name="devTreebank">a set of trees which can be used for dev testing (assuming the user provided a dev treebank)</param>
		/// <param name="nThreads">how many threads the model can use for training</param>
		public abstract void TrainModel(string serializedPath, Edu.Stanford.Nlp.Tagger.Common.Tagger tagger, Random random, IList<Tree> binarizedTrainTrees, IList<IList<ITransition>> transitionLists, Treebank devTreebank, int nThreads);

		internal abstract ICollection<string> TagSet();

		private const long serialVersionUID = -175375535849840611L;
	}
}

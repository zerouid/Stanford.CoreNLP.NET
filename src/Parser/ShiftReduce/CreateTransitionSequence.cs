using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	public class CreateTransitionSequence
	{
		private CreateTransitionSequence()
		{
		}

		// static methods only.  
		// we could change this if we wanted to include options.
		public static IList<IList<ITransition>> CreateTransitionSequences(IList<Tree> binarizedTrees, bool compoundUnary, ICollection<string> rootStates, ICollection<string> rootOnlyStates)
		{
			IList<IList<ITransition>> transitionLists = Generics.NewArrayList();
			foreach (Tree tree in binarizedTrees)
			{
				IList<ITransition> transitions = CreateTransitionSequence(tree, compoundUnary, rootStates, rootOnlyStates);
				transitionLists.Add(transitions);
			}
			return transitionLists;
		}

		public static IList<ITransition> CreateTransitionSequence(Tree tree)
		{
			return CreateTransitionSequence(tree, true, Java.Util.Collections.Singleton("ROOT"), Java.Util.Collections.Singleton("ROOT"));
		}

		public static IList<ITransition> CreateTransitionSequence(Tree tree, bool compoundUnary, ICollection<string> rootStates, ICollection<string> rootOnlyStates)
		{
			IList<ITransition> transitions = Generics.NewArrayList();
			CreateTransitionSequenceHelper(transitions, tree, compoundUnary, rootOnlyStates);
			transitions.Add(new FinalizeTransition(rootStates));
			transitions.Add(new IdleTransition());
			return transitions;
		}

		private static void CreateTransitionSequenceHelper(IList<ITransition> transitions, Tree tree, bool compoundUnary, ICollection<string> rootOnlyStates)
		{
			if (tree.IsLeaf())
			{
			}
			else
			{
				// do nothing
				if (tree.IsPreTerminal())
				{
					transitions.Add(new ShiftTransition());
				}
				else
				{
					if (tree.Children().Length == 1)
					{
						bool isRoot = rootOnlyStates.Contains(tree.Label().Value());
						if (compoundUnary)
						{
							IList<string> labels = Generics.NewArrayList();
							while (tree.Children().Length == 1 && !tree.IsPreTerminal())
							{
								labels.Add(tree.Label().Value());
								tree = tree.Children()[0];
							}
							CreateTransitionSequenceHelper(transitions, tree, compoundUnary, rootOnlyStates);
							transitions.Add(new CompoundUnaryTransition(labels, isRoot));
						}
						else
						{
							CreateTransitionSequenceHelper(transitions, tree.Children()[0], compoundUnary, rootOnlyStates);
							transitions.Add(new UnaryTransition(tree.Label().Value(), isRoot));
						}
					}
					else
					{
						if (tree.Children().Length == 2)
						{
							CreateTransitionSequenceHelper(transitions, tree.Children()[0], compoundUnary, rootOnlyStates);
							CreateTransitionSequenceHelper(transitions, tree.Children()[1], compoundUnary, rootOnlyStates);
							// This is the tricky part... need to decide if the binary
							// transition is a left or right transition.  This is done by
							// looking at the existing heads of this node and its two
							// children.  The expectation is that the tree already has heads
							// assigned; otherwise, exception is thrown
							if (!(tree.Label() is CoreLabel) || !(tree.Children()[0].Label() is CoreLabel) || !(tree.Children()[1].Label() is CoreLabel))
							{
								throw new ArgumentException("Expected tree labels to be CoreLabel");
							}
							CoreLabel label = (CoreLabel)tree.Label();
							CoreLabel leftLabel = (CoreLabel)tree.Children()[0].Label();
							CoreLabel rightLabel = (CoreLabel)tree.Children()[1].Label();
							CoreLabel head = label.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation));
							CoreLabel leftHead = leftLabel.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation));
							CoreLabel rightHead = rightLabel.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation));
							if (head == null || leftHead == null || rightHead == null)
							{
								throw new ArgumentException("Expected tree labels to have their heads assigned");
							}
							if (head == leftHead)
							{
								transitions.Add(new BinaryTransition(tree.Label().Value(), BinaryTransition.Side.Left));
							}
							else
							{
								if (head == rightHead)
								{
									transitions.Add(new BinaryTransition(tree.Label().Value(), BinaryTransition.Side.Right));
								}
								else
								{
									throw new ArgumentException("Heads were incorrectly assigned: tree's head is not matched to either the right or left head");
								}
							}
						}
						else
						{
							throw new ArgumentException("Expected a binarized tree");
						}
					}
				}
			}
		}
	}
}

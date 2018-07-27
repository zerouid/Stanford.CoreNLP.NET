using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	internal class ShiftReduceUtils
	{
		private ShiftReduceUtils()
		{
		}

		// static utility methods
		internal static BinaryTransition.Side GetBinarySide(Tree tree)
		{
			if (tree.Children().Length != 2)
			{
				throw new AssertionError();
			}
			CoreLabel label = ErasureUtils.UncheckedCast(tree.Label());
			CoreLabel childLabel = ErasureUtils.UncheckedCast(tree.Children()[0].Label());
			if (label.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)) == childLabel.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation)))
			{
				return BinaryTransition.Side.Left;
			}
			else
			{
				return BinaryTransition.Side.Right;
			}
		}

		internal static bool IsTemporary(Tree tree)
		{
			string label = tree.Value();
			return label.StartsWith("@");
		}

		internal static bool IsEquivalentCategory(string l1, string l2)
		{
			if (l1.StartsWith("@"))
			{
				l1 = Sharpen.Runtime.Substring(l1, 1);
			}
			if (l2.StartsWith("@"))
			{
				l2 = Sharpen.Runtime.Substring(l2, 1);
			}
			return l1.Equals(l2);
		}

		/// <summary>Returns a 0-based index of the head of the tree.</summary>
		/// <remarks>Returns a 0-based index of the head of the tree.  Assumes the leaves had been indexed from 1</remarks>
		internal static int HeadIndex(Tree tree)
		{
			CoreLabel label = ErasureUtils.UncheckedCast(tree.Label());
			CoreLabel headLabel = label.Get(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation));
			return headLabel.Index() - 1;
		}

		/// <summary>Returns a 0-based index of the left leaf of the tree.</summary>
		/// <remarks>Returns a 0-based index of the left leaf of the tree.  Assumes the leaves had been indexed from 1</remarks>
		internal static int LeftIndex(Tree tree)
		{
			if (tree.IsLeaf())
			{
				CoreLabel label = ErasureUtils.UncheckedCast(tree.Label());
				return label.Index() - 1;
			}
			return LeftIndex(tree.Children()[0]);
		}

		/// <summary>Returns a 0-based index of the right leaf of the tree.</summary>
		/// <remarks>Returns a 0-based index of the right leaf of the tree.  Assumes the leaves had been indexed from 1</remarks>
		internal static int RightIndex(Tree tree)
		{
			if (tree.IsLeaf())
			{
				CoreLabel label = ErasureUtils.UncheckedCast(tree.Label());
				return label.Index() - 1;
			}
			return RightIndex(tree.Children()[tree.Children().Length - 1]);
		}

		internal static bool ConstraintMatchesTreeTop(Tree top, ParserConstraint constraint)
		{
			while (true)
			{
				if (constraint.state.Matcher(top.Value()).Matches())
				{
					return true;
				}
				else
				{
					if (top.Children().Length == 1)
					{
						top = top.Children()[0];
					}
					else
					{
						return false;
					}
				}
			}
		}

		/// <summary>
		/// Returns true iff the given
		/// <paramref name="state"/>
		/// is present on the
		/// <paramref name="agenda"/>
		/// </summary>
		internal static bool FindStateOnAgenda(ICollection<State> agenda, State state)
		{
			foreach (State other in agenda)
			{
				if (other.AreTransitionsEqual(state))
				{
					return true;
				}
			}
			return false;
		}
	}
}

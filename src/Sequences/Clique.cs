using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This class is meant to represent a clique in a (directed
	/// or undirected) linear-chain graphical model.
	/// </summary>
	/// <remarks>
	/// This class is meant to represent a clique in a (directed
	/// or undirected) linear-chain graphical model.  It encodes
	/// the relative indices that are included in a clique with
	/// respect to the current index (0).  For instance if you have a clique
	/// that is current label and two-ago label, then the relative
	/// indices clique would look like [-2, 0].  The relativeIndices[]
	/// array should be sorted.  Cliques are immutable.  Also, for two
	/// cliques, c1 and c2, (c1 == c2) iff c1.equals(c2).
	/// </remarks>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class Clique
	{
		private const long serialVersionUID = -8109637472035159453L;

		private readonly int[] relativeIndices;

		protected internal static readonly IDictionary<Clique.CliqueEqualityWrapper, Edu.Stanford.Nlp.Sequences.Clique> interner = Generics.NewHashMap();

		private class CliqueEqualityWrapper
		{
			private readonly Clique c;

			public CliqueEqualityWrapper(Clique c)
			{
				this.c = c;
			}

			public override bool Equals(object o)
			{
				if (!(o is Clique.CliqueEqualityWrapper))
				{
					return false;
				}
				Clique.CliqueEqualityWrapper otherC = (Clique.CliqueEqualityWrapper)o;
				if (otherC.c.relativeIndices.Length != c.relativeIndices.Length)
				{
					return false;
				}
				for (int i = 0; i < c.relativeIndices.Length; i++)
				{
					if (c.relativeIndices[i] != otherC.c.relativeIndices[i])
					{
						return false;
					}
				}
				return true;
			}

			public override int GetHashCode()
			{
				int h = 1;
				foreach (int i in c.relativeIndices)
				{
					h *= 17;
					h += i;
				}
				return h;
			}
		}

		// end static class CliqueEqualityWrapper
		private static Clique Intern(Clique c)
		{
			Clique.CliqueEqualityWrapper wrapper = new Clique.CliqueEqualityWrapper(c);
			Clique newC = interner[wrapper];
			if (newC == null)
			{
				interner[wrapper] = c;
				newC = c;
			}
			return newC;
		}

		private Clique(int[] relativeIndices)
		{
			this.relativeIndices = relativeIndices;
		}

		public static Clique ValueOf(int maxLeft, int maxRight)
		{
			int[] ri = new int[-maxLeft + maxRight + 1];
			int j = maxLeft;
			for (int i = 0; i < ri.Length; i++)
			{
				ri[i] = j++;
			}
			return ValueOfHelper(ri);
		}

		/// <summary>Make a clique over the provided relativeIndices.</summary>
		/// <remarks>
		/// Make a clique over the provided relativeIndices.
		/// relativeIndices should be sorted.
		/// </remarks>
		public static Clique ValueOf(int[] relativeIndices)
		{
			CheckSorted(relativeIndices);
			// copy the array so as to be safe
			return ValueOfHelper(ArrayUtils.Copy(relativeIndices));
		}

		public static Clique ValueOf(Clique c, int offset)
		{
			int[] ri = new int[c.relativeIndices.Length];
			for (int i = 0; i < ri.Length; i++)
			{
				ri[i] = c.relativeIndices[i] + offset;
			}
			return ValueOfHelper(ri);
		}

		/// <summary>
		/// This version assumes relativeIndices array no longer needs to
		/// be copied.
		/// </summary>
		/// <remarks>
		/// This version assumes relativeIndices array no longer needs to
		/// be copied. Further it is assumed that it has already been
		/// checked or assured by construction that relativeIndices
		/// is sorted.
		/// </remarks>
		private static Clique ValueOfHelper(int[] relativeIndices)
		{
			// if clique already exists, return that one
			Clique c = new Clique(relativeIndices);
			return Intern(c);
		}

		/// <summary>Parameter validity check.</summary>
		private static void CheckSorted(int[] sorted)
		{
			for (int i = 0; i < sorted.Length - 1; i++)
			{
				if (sorted[i] > sorted[i + 1])
				{
					throw new Exception("input must be sorted!");
				}
			}
		}

		/// <summary>
		/// Convenience method for finding the most far left
		/// relative index.
		/// </summary>
		public virtual int MaxLeft()
		{
			return relativeIndices[0];
		}

		/// <summary>
		/// Convenience method for finding the most far right
		/// relative index.
		/// </summary>
		public virtual int MaxRight()
		{
			return relativeIndices[relativeIndices.Length - 1];
		}

		/// <summary>The number of nodes in the clique.</summary>
		public virtual int Size()
		{
			return relativeIndices.Length;
		}

		/// <returns>the ith relativeIndex</returns>
		public virtual int RelativeIndex(int i)
		{
			return relativeIndices[i];
		}

		/// <summary>
		/// For a particular relative index, returns which element in
		/// the Clique it is.
		/// </summary>
		/// <remarks>
		/// For a particular relative index, returns which element in
		/// the Clique it is.  For instance, if you created a Clique
		/// c with relativeIndices [-2, -1, 0], then c.indexOfRelativeIndex(-1)
		/// will return 1.  If the relative index is not present, it
		/// will return -1.
		/// </remarks>
		public virtual int IndexOfRelativeIndex(int relativeIndex)
		{
			for (int i = 0; i < relativeIndices.Length; i++)
			{
				if (relativeIndices[i] == relativeIndex)
				{
					return i;
				}
			}
			return -1;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append('[');
			for (int i = 0; i < relativeIndices.Length; i++)
			{
				sb.Append(relativeIndices[i]);
				if (i != relativeIndices.Length - 1)
				{
					sb.Append(", ");
				}
			}
			sb.Append(']');
			return sb.ToString();
		}

		public virtual Clique LeftMessage()
		{
			int[] ri = new int[relativeIndices.Length - 1];
			System.Array.Copy(relativeIndices, 0, ri, 0, ri.Length);
			return ValueOfHelper(ri);
		}

		public virtual Clique RightMessage()
		{
			int[] ri = new int[relativeIndices.Length - 1];
			System.Array.Copy(relativeIndices, 1, ri, 0, ri.Length);
			return ValueOfHelper(ri);
		}

		public virtual Clique Shift(int shiftAmount)
		{
			if (shiftAmount == 0)
			{
				return this;
			}
			int[] ri = new int[relativeIndices.Length];
			for (int i = 0; i < ri.Length; i++)
			{
				ri[i] = relativeIndices[i] + shiftAmount;
			}
			return ValueOfHelper(ri);
		}

		private int hashCode = -1;

		public override int GetHashCode()
		{
			if (hashCode == -1)
			{
				hashCode = ToString().GetHashCode();
			}
			return hashCode;
		}

		protected internal virtual object ReadResolve()
		{
			return Intern(this);
		}
	}
}

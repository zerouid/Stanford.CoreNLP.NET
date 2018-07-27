using System;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	[System.Serializable]
	public class BinaryHeadFinder : IHeadFinder
	{
		private const long serialVersionUID = 4794072338791804184L;

		private readonly IHeadFinder fallbackHF;

		public BinaryHeadFinder()
			: this(null)
		{
		}

		public BinaryHeadFinder(IHeadFinder fallbackHF)
		{
			this.fallbackHF = fallbackHF;
		}

		/// <summary>Determine which daughter of the current parse tree is the head.</summary>
		/// <remarks>
		/// Determine which daughter of the current parse tree is the head.
		/// It assumes that the daughters already have had their heads
		/// determined. Another method has to do the tree walking.
		/// </remarks>
		/// <param name="t">The parse tree to examine the daughters of</param>
		/// <returns>
		/// The parse tree that is the head.  The convention has been
		/// that this returns <code>null</code> if no head is found.
		/// But maybe it should throw an exception?
		/// </returns>
		public virtual Tree DetermineHead(Tree t)
		{
			Tree result = DetermineBinaryHead(t);
			if (result == null && fallbackHF != null)
			{
				result = fallbackHF.DetermineHead(t);
			}
			if (result != null)
			{
				return result;
			}
			throw new InvalidOperationException("BinaryHeadFinder: unexpected tree: " + t);
		}

		public virtual Tree DetermineHead(Tree t, Tree parent)
		{
			Tree result = DetermineBinaryHead(t);
			if (result == null && fallbackHF != null)
			{
				result = fallbackHF.DetermineHead(t, parent);
			}
			if (result != null)
			{
				return result;
			}
			throw new InvalidOperationException("BinaryHeadFinder: unexpected tree: " + t);
		}

		private Tree DetermineBinaryHead(Tree t)
		{
			if (t.NumChildren() == 1)
			{
				return t.FirstChild();
			}
			else
			{
				string lval = t.FirstChild().Label().Value();
				if (lval != null && lval.StartsWith("@"))
				{
					return t.FirstChild();
				}
				else
				{
					string rval = t.LastChild().Label().Value();
					if (rval.StartsWith("@") || rval.Equals(LexiconConstants.BoundaryTag))
					{
						return t.LastChild();
					}
				}
			}
			return null;
		}
	}
}

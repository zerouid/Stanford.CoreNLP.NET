using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@nlp.stanford.edu)</author>
	internal class TsurgeonPatternRoot : TsurgeonPattern
	{
		public TsurgeonPatternRoot(TsurgeonPattern child)
			: this(new TsurgeonPattern[] { child })
		{
		}

		public TsurgeonPatternRoot(TsurgeonPattern[] children)
			: base("operations: ", children)
		{
			SetRoot(this);
		}

		internal bool coindexes = false;

		/// <summary>
		/// If one of the children is a CoindexNodes (or something else that
		/// wants coindexing), it can call this at the time of setRoot()
		/// </summary>
		internal virtual void SetCoindexes()
		{
			coindexes = true;
		}

		public override TsurgeonMatcher Matcher()
		{
			CoindexationGenerator coindexer = null;
			if (coindexes)
			{
				coindexer = new CoindexationGenerator();
			}
			return Matcher(Generics.NewHashMap<string, Tree>(), coindexer);
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new TsurgeonPatternRoot.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(TsurgeonPatternRoot _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			/// <summary>returns null if one of the surgeries eliminates the tree entirely.</summary>
			/// <remarks>
			/// returns null if one of the surgeries eliminates the tree entirely.  The
			/// operated-on tree is not to be trusted in this instance.
			/// </remarks>
			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				if (this.coindexer != null)
				{
					this.coindexer.SetLastIndex(tree);
				}
				foreach (TsurgeonMatcher child in this.childMatcher)
				{
					tree = child.Evaluate(tree, tregex);
					if (tree == null)
					{
						return null;
					}
				}
				return tree;
			}

			private readonly TsurgeonPatternRoot _enclosing;
		}
	}
}

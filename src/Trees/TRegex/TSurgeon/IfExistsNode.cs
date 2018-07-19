using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>
	/// Executes the give children only if the named Tregex node exists in
	/// the TregexMatcher at match time (allows for OR relations or
	/// optional relations)
	/// </summary>
	/// <author>John Bauer (horatio@gmail.com)</author>
	internal class IfExistsNode : TsurgeonPattern
	{
		internal readonly string name;

		internal readonly bool invert;

		public IfExistsNode(string name, bool invert, params TsurgeonPattern[] children)
			: base("if " + (invert ? "not " : string.Empty) + "exists " + name, children)
		{
			this.name = name;
			this.invert = invert;
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new IfExistsNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(IfExistsNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				if (this._enclosing.invert ^ (tregex.GetNode(this._enclosing.name) != null))
				{
					foreach (TsurgeonMatcher child in this.childMatcher)
					{
						child.Evaluate(tree, tregex);
					}
				}
				return tree;
			}

			private readonly IfExistsNode _enclosing;
		}
	}
}

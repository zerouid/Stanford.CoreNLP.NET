using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <author>Roger Levy (rog@stanford.edu)</author>
	public class FetchNode : TsurgeonPattern
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.FetchNode));

		public FetchNode(string nodeName)
			: base(nodeName, TsurgeonPattern.EmptyTsurgeonPatternArray)
		{
		}

		public override TsurgeonMatcher Matcher(IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
		{
			return new FetchNode.Matcher(this, newNodeNames, coindexer);
		}

		private class Matcher : TsurgeonMatcher
		{
			public Matcher(FetchNode _enclosing, IDictionary<string, Tree> newNodeNames, CoindexationGenerator coindexer)
				: base(this._enclosing, newNodeNames, coindexer)
			{
				this._enclosing = _enclosing;
			}

			public override Tree Evaluate(Tree tree, TregexMatcher tregex)
			{
				Tree result = this.newNodeNames[this._enclosing.label];
				if (result == null)
				{
					result = tregex.GetNode(this._enclosing.label);
				}
				if (result == null)
				{
					FetchNode.log.Info("Warning -- null node fetched by Tsurgeon operation for node: " + this + " (either no node labeled this, or the labeled node didn't match anything)");
				}
				return result;
			}

			private readonly FetchNode _enclosing;
		}
	}
}

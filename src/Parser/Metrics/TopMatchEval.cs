using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Measures accuracy by only considering the very top of the parse tree, eg where S, SINV, etc go</summary>
	/// <author>John Bauer</author>
	public class TopMatchEval : AbstractEval
	{
		private readonly IConstituentFactory cf;

		public TopMatchEval(string name, bool runningAverages)
			: base(name, runningAverages)
		{
			cf = new LabeledScoredConstituentFactory();
		}

		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			if (tree == null)
			{
				return Java.Util.Collections.EmptySet();
			}
			// The eval trees won't have a root level, instead starting with
			// the S/SINV/FRAG/whatever, so just eval at the top level
			ICollection<Constituent> result = tree.Constituents(cf, 0);
			return result;
		}
	}
}



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>Dan Klein</author>
	internal class NullGrammarProjection : IGrammarProjection
	{
		internal UnaryGrammar ug;

		internal BinaryGrammar bg;

		public virtual int Project(int state)
		{
			return state;
		}

		public virtual UnaryGrammar SourceUG()
		{
			return ug;
		}

		public virtual BinaryGrammar SourceBG()
		{
			return bg;
		}

		public virtual UnaryGrammar TargetUG()
		{
			return ug;
		}

		public virtual BinaryGrammar TargetBG()
		{
			return bg;
		}

		internal NullGrammarProjection(BinaryGrammar bg, UnaryGrammar ug)
		{
			this.ug = ug;
			this.bg = bg;
		}
	}
}

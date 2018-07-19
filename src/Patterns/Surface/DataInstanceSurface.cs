using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>Created by sonalg on 11/1/14.</summary>
	[System.Serializable]
	public class DataInstanceSurface : DataInstance
	{
		internal IList<CoreLabel> tokens;

		public DataInstanceSurface(IList<CoreLabel> toks)
		{
			this.tokens = toks;
		}

		public override IList<CoreLabel> GetTokens()
		{
			return tokens;
		}
	}
}

using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class GermanUnknownWordModelTrainer : BaseUnknownWordModelTrainer
	{
		protected internal override IUnknownWordModel BuildUWM()
		{
			IDictionary<string, float> unknownGT = null;
			if (useGT)
			{
				unknownGT = unknownGTTrainer.unknownGT;
			}
			return new GermanUnknownWordModel(op, lex, wordIndex, tagIndex, unSeenCounter, tagHash, unknownGT, seenEnd);
		}
	}
}

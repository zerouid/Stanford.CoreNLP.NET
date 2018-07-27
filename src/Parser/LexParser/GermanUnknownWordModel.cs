using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An unknown word model for German; relies on BaseUnknownWordModel plus number matching.</summary>
	/// <remarks>
	/// An unknown word model for German; relies on BaseUnknownWordModel plus number matching.
	/// An assumption of this model is that numbers (arabic digit sequences)
	/// are tagged CARD. This is correct for all of NEGRA/Tiger/TueBaDZ.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Greg Donaker (corrections and modeling improvements)</author>
	/// <author>Christopher Manning (generalized and improved what Greg did)</author>
	[System.Serializable]
	public class GermanUnknownWordModel : BaseUnknownWordModel
	{
		private const long serialVersionUID = 221L;

		private const string numberMatch = "[0-9]+(?:\\.[0-9]*)";

		public GermanUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter, IDictionary<ILabel, ClassicCounter<string>> tagHash, IDictionary<string, float> unknownGT
			, ICollection<string> seenEnd)
			: base(op, lex, wordIndex, tagIndex, unSeenCounter, tagHash, unknownGT, seenEnd)
		{
		}

		/// <summary>This constructor creates an UWM with empty data structures.</summary>
		/// <remarks>
		/// This constructor creates an UWM with empty data structures.  Only
		/// use if loading in the data separately, such as by reading in text
		/// lines containing the data.
		/// </remarks>
		public GermanUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(op, lex, wordIndex, tagIndex, new ClassicCounter<IntTaggedWord>(), Generics.NewHashMap<ILabel, ClassicCounter<string>>(), Generics.NewHashMap<string, float>(), Generics.NewHashSet<string>())
		{
		}

		/// <summary>
		/// Calculate the log-prob score of a particular TaggedWord in the
		/// unknown word model.
		/// </summary>
		/// <param name="itw">the tag-&gt;word production in IntTaggedWord form</param>
		/// <returns>The log-prob score of a particular TaggedWord.</returns>
		public override float Score(IntTaggedWord itw, string word)
		{
			string tag = itw.TagString(tagIndex);
			if (word.Matches(numberMatch))
			{
				//EncodingPrintWriter.out.println("Number match for " + word,encoding);
				if (tag.Equals("CARD"))
				{
					return 0.0f;
				}
				else
				{
					//EncodingPrintWriter.out.println("Unknown word estimate for " + word + " as " + tag + ": " + logProb,encoding); //debugging
					return float.NegativeInfinity;
				}
			}
			else
			{
				return base.Score(itw, word);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Stores, trains, and scores with an unknown word model.</summary>
	/// <remarks>
	/// Stores, trains, and scores with an unknown word model.  A couple
	/// of filters deterministically force rewrites for certain proper
	/// nouns, dates, and cardinal and ordinal numbers; when none of these
	/// filters are met, either the distribution of terminals with the same
	/// first character is used, or Good-Turing smoothing is used. Although
	/// this is developed for Chinese, the training and storage methods
	/// could be used cross-linguistically.
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class ChineseUnknownWordModel : BaseUnknownWordModel
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ChineseUnknownWordModel));

		private const string encoding = "GB18030";

		private readonly bool useUnicodeType;

		private const string numberMatch = ".*[0-9\uff10-\uff19\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341\u767e\u5343\u4e07\u4ebf\u96F6\u3007\u25cb\u25ef].*";

		private const string dateMatch = numberMatch + "[\u5e74\u6708\u65e5\u53f7]";

		private const string ordinalMatch = "\u7b2c.*";

		private const string properNameMatch = ".*[\u00b7\u0387\u2022\u2024\u2027\u2219\u22C5\u30FB].*";

		private readonly ICollection<string> seenFirst;

		public ChineseUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex, ClassicCounter<IntTaggedWord> unSeenCounter, IDictionary<ILabel, ClassicCounter<string>> tagHash, IDictionary<string, float> unknownGT
			, bool useGT, ICollection<string> seenFirst)
			: base(op, lex, wordIndex, tagIndex, unSeenCounter, tagHash, unknownGT, null)
		{
			// used only for debugging
			/* These strings are stored in ascii-type Unicode encoding.  To
			* edit them, either use the Unicode codes or use native2ascii or a
			* similar program to convert the file into a Chinese encoding, then
			* convert back. */
			// uses midDot characters as one clue of being proper name
			this.useFirst = !useGT;
			this.useGT = useGT;
			this.useUnicodeType = op.lexOptions.useUnicodeType;
			this.seenFirst = seenFirst;
		}

		/// <summary>This constructor creates an UWM with empty data structures.</summary>
		/// <remarks>
		/// This constructor creates an UWM with empty data structures.  Only
		/// use if loading in the data separately, such as by reading in text
		/// lines containing the data.
		/// TODO: would need to set useGT correctly if you saved a model with
		/// useGT and then wanted to recover it from text.
		/// </remarks>
		public ChineseUnknownWordModel(Options op, ILexicon lex, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: this(op, lex, wordIndex, tagIndex, new ClassicCounter<IntTaggedWord>(), Generics.NewHashMap<ILabel, ClassicCounter<string>>(), Generics.NewHashMap<string, float>(), false, Generics.NewHashSet<string>())
		{
		}

		public override float Score(IntTaggedWord itw, string word)
		{
			// Label tagL = itw.tagLabel();
			// String tag = tagL.value();
			string tag = itw.TagString(tagIndex);
			ILabel tagL = new Tag(tag);
			float logProb;
			if (word.Matches(dateMatch))
			{
				//EncodingPrintWriter.out.println("Date match for " + word,encoding);
				if (tag.Equals("NT"))
				{
					logProb = 0.0f;
				}
				else
				{
					logProb = float.NegativeInfinity;
				}
			}
			else
			{
				if (word.Matches(numberMatch))
				{
					//EncodingPrintWriter.out.println("Number match for " + word,encoding);
					if (tag.Equals("CD") && (!word.Matches(ordinalMatch)))
					{
						logProb = 0.0f;
					}
					else
					{
						if (tag.Equals("OD") && word.Matches(ordinalMatch))
						{
							logProb = 0.0f;
						}
						else
						{
							logProb = float.NegativeInfinity;
						}
					}
				}
				else
				{
					if (word.Matches(properNameMatch))
					{
						//EncodingPrintWriter.out.println("Proper name match for " + word,encoding);
						if (tag.Equals("NR"))
						{
							logProb = 0.0f;
						}
						else
						{
							logProb = float.NegativeInfinity;
						}
					}
					else
					{
						/* -------------
						// this didn't seem to work -- too categorical
						int type = Character.getType(word.charAt(0));
						// the below may not normalize probs over options, but is probably okay
						if (type == Character.START_PUNCTUATION) {
						if (tag.equals("PU-LPAREN") || tag.equals("PU-PAREN") ||
						tag.equals("PU-LQUOTE") || tag.equals("PU-QUOTE") ||
						tag.equals("PU")) {
						// if (VERBOSE) log.info("ChineseUWM: unknown L Punc");
						logProb = 0.0f;
						} else {
						logProb = Float.NEGATIVE_INFINITY;
						}
						} else if (type == Character.END_PUNCTUATION) {
						if (tag.equals("PU-RPAREN") || tag.equals("PU-PAREN") ||
						tag.equals("PU-RQUOTE") || tag.equals("PU-QUOTE") ||
						tag.equals("PU")) {
						// if (VERBOSE) log.info("ChineseUWM: unknown R Punc");
						logProb = 0.0f;
						} else {
						logProb = Float.NEGATIVE_INFINITY;
						}
						} else {
						if (tag.equals("PU-OTHER") || tag.equals("PU-ENDSENT") ||
						tag.equals("PU")) {
						// if (VERBOSE) log.info("ChineseUWM: unknown O Punc");
						logProb = 0.0f;
						} else {
						logProb = Float.NEGATIVE_INFINITY;
						}
						}
						------------- */
						if (useFirst)
						{
							string first = Sharpen.Runtime.Substring(word, 0, 1);
							if (useUnicodeType)
							{
								char ch = word[0];
								int type = char.GetType(ch);
								if (type != char.OtherLetter)
								{
									// standard Chinese characters are of type "OTHER_LETTER"!!
									first = int.ToString(type);
								}
							}
							if (!seenFirst.Contains(first))
							{
								if (useGT)
								{
									logProb = ScoreGT(tag);
									goto first_break;
								}
								else
								{
									first = unknown;
								}
							}
							/* get the Counter of terminal rewrites for the relevant tag */
							ClassicCounter<string> wordProbs = tagHash[tagL];
							/* if the proposed tag has never been seen before, issue a
							warning and return probability 0. */
							if (wordProbs == null)
							{
								logProb = float.NegativeInfinity;
							}
							else
							{
								if (wordProbs.ContainsKey(first))
								{
									logProb = (float)wordProbs.GetCount(first);
								}
								else
								{
									logProb = (float)wordProbs.GetCount(unknown);
								}
							}
						}
						else
						{
							if (useGT)
							{
								logProb = ScoreGT(tag);
							}
							else
							{
								logProb = float.NegativeInfinity;
							}
						}
first_break: ;
					}
				}
			}
			// should never get this!
			return logProb;
		}

		public static void Main(string[] args)
		{
			System.Console.Out.WriteLine("Testing unknown matching");
			string s = "\u5218\u00b7\u9769\u547d";
			if (s.Matches(properNameMatch))
			{
				System.Console.Out.WriteLine("hooray names!");
			}
			else
			{
				System.Console.Out.WriteLine("Uh-oh names!");
			}
			string s1 = "\uff13\uff10\uff10\uff10";
			if (s1.Matches(numberMatch))
			{
				System.Console.Out.WriteLine("hooray numbers!");
			}
			else
			{
				System.Console.Out.WriteLine("Uh-oh numbers!");
			}
			string s11 = "\u767e\u5206\u4e4b\u56db\u5341\u4e09\u70b9\u4e8c";
			if (s11.Matches(numberMatch))
			{
				System.Console.Out.WriteLine("hooray numbers!");
			}
			else
			{
				System.Console.Out.WriteLine("Uh-oh numbers!");
			}
			string s12 = "\u767e\u5206\u4e4b\u4e09\u5341\u516b\u70b9\u516d";
			if (s12.Matches(numberMatch))
			{
				System.Console.Out.WriteLine("hooray numbers!");
			}
			else
			{
				System.Console.Out.WriteLine("Uh-oh numbers!");
			}
			string s2 = "\u4e09\u6708";
			if (s2.Matches(dateMatch))
			{
				System.Console.Out.WriteLine("hooray dates!");
			}
			else
			{
				System.Console.Out.WriteLine("Uh-oh dates!");
			}
			System.Console.Out.WriteLine("Testing tagged word");
			ClassicCounter<TaggedWord> c = new ClassicCounter<TaggedWord>();
			TaggedWord tw1 = new TaggedWord("w", "t");
			c.IncrementCount(tw1);
			TaggedWord tw2 = new TaggedWord("w", "t2");
			System.Console.Out.WriteLine(c.ContainsKey(tw2));
			System.Console.Out.WriteLine(tw1.Equals(tw2));
			WordTag wt1 = ToWordTag(tw1);
			WordTag wt2 = ToWordTag(tw2);
			WordTag wt3 = new WordTag("w", "t2");
			System.Console.Out.WriteLine(wt1.Equals(wt2));
			System.Console.Out.WriteLine(wt2.Equals(wt3));
		}

		private static WordTag ToWordTag(TaggedWord tw)
		{
			return new WordTag(tw.Word(), tw.Tag());
		}

		private const long serialVersionUID = 221L;

		public override string GetSignature(string word, int loc)
		{
			throw new NotSupportedException();
		}
	}
}

using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Java.Lang;
using Java.Util.Concurrent;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <author>Pichuan Chang</author>
	/// <author>Michel Galley</author>
	/// <author>John Bauer</author>
	/// <author>KellenSunderland (public domain contribution)</author>
	public class ChineseStringUtils
	{
		private const bool Debug = false;

		private static readonly Pattern percentsPat = Pattern.Compile(ChineseUtils.White + "([\uff05%])" + ChineseUtils.White);

		private const string percentStr = ChineseUtils.Whiteplus + "([\uff05%])";

		private static readonly ChineseStringUtils.HKPostProcessor hkPostProcessor = new ChineseStringUtils.HKPostProcessor();

		private static readonly ChineseStringUtils.ASPostProcessor asPostProcessor = new ChineseStringUtils.ASPostProcessor();

		private static readonly ChineseStringUtils.BaseChinesePostProcessor basicPostsProcessor = new ChineseStringUtils.BaseChinesePostProcessor();

		private static readonly ChineseStringUtils.CTPPostProcessor ctpPostProcessor = new ChineseStringUtils.CTPPostProcessor();

		private static readonly ChineseStringUtils.PKPostProcessor pkPostProcessor = new ChineseStringUtils.PKPostProcessor();

		private ChineseStringUtils()
		{
		}

		// TODO: ChineseStringUtils and ChineseUtils should be put somewhere common
		// static methods
		public static bool IsLetterASCII(char c)
		{
			return c <= 127 && char.IsLetter(c);
		}

		public static string CombineSegmentedSentence(IList<CoreLabel> doc, SeqClassifierFlags flags)
		{
			// Hey all: Some of the code that was previously here for
			// whitespace normalization was a bit hackish as well as
			// obviously broken for some test cases. So...I went ahead and
			// re-wrote it.
			//
			// Also, putting everything into 'testContent', is a bit wasteful
			// memory wise. But, it's on my near-term todo list to
			// code something that's a bit more memory efficient.
			//
			// Finally, if these changes ended up breaking anything
			// just e-mail me (cerd@colorado.edu), and I'll try to fix it
			// asap  -cer (6/14/2006)
			/* Sun Oct  7 19:55:09 2007
			I'm actually not using "testContent" anymore.
			I think it's broken because the whole test file has been read over and over again,
			tand the testContentIdx has been set to 0 every time, while "doc" is moving
			line by line!!!!
			-pichuan
			*/
			int testContentIdx = 0;
			StringBuilder ans = new StringBuilder();
			// the actual output we will return
			StringBuilder unmod_ans = new StringBuilder();
			// this is the original output from the CoreLabel
			StringBuilder unmod_normed_ans = new StringBuilder();
			// this is the original output from the CoreLabel
			CoreLabel wi = null;
			for (IEnumerator<CoreLabel> wordIter = doc.GetEnumerator(); wordIter.MoveNext(); testContentIdx++)
			{
				CoreLabel pwi = wi;
				wi = wordIter.Current;
				bool originalWhiteSpace = "1".Equals(wi.Get(typeof(CoreAnnotations.SpaceBeforeAnnotation)));
				//  if the CRF says "START" (segmented), and it's not the first word..
				if (wi.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("1") && !("0".Equals(wi.Get(typeof(CoreAnnotations.PositionAnnotation)).ToString())))
				{
					// check if we need to preserve the "no space" between English
					// characters
					bool seg = true;
					// since it's in the "1" condition.. default is to seg
					if (flags.keepEnglishWhitespaces)
					{
						if (testContentIdx > 0)
						{
							char prevChar = pwi.Get(typeof(CoreAnnotations.OriginalCharAnnotation))[0];
							char currChar = wi.Get(typeof(CoreAnnotations.OriginalCharAnnotation))[0];
							if (IsLetterASCII(prevChar) && IsLetterASCII(currChar))
							{
								// keep the "non space" before wi
								if (!originalWhiteSpace)
								{
									seg = false;
								}
							}
						}
					}
					// if there was space and keepAllWhitespaces is true, restore it no matter what
					if (flags.keepAllWhitespaces && originalWhiteSpace)
					{
						seg = true;
					}
					if (seg)
					{
						if (originalWhiteSpace)
						{
							ans.Append('\u1924');
						}
						else
						{
							// a pretty Limbu character which is later changed to a space
							ans.Append(' ');
						}
					}
					unmod_ans.Append(' ');
					unmod_normed_ans.Append(' ');
				}
				else
				{
					bool seg = false;
					// since it's in the "0" condition.. default
					// Changed after conversation with Huihsin.
					//
					// Decided that all words consisting of English/ASCII characters
					// should be separated from the surrounding Chinese characters. -cer
					/* Sun Oct  7 22:14:46 2007 (pichuan)
					the comment above was from DanC.
					I changed the code but I think I'm doing the same thing here.
					*/
					if (testContentIdx > 0)
					{
						char prevChar = pwi.Get(typeof(CoreAnnotations.OriginalCharAnnotation))[0];
						char currChar = wi.Get(typeof(CoreAnnotations.OriginalCharAnnotation))[0];
						if ((prevChar < (char)128) != (currChar < (char)128))
						{
							if (ChineseUtils.IsNumber(prevChar) && ChineseUtils.IsNumber(currChar))
							{
							}
							else
							{
								// cdm: you would get here if you had an ASCII number next to a
								// Unihan range number.  Does that happen?  It presumably
								// shouldn't do any harm.... [cdm, oct 2007]
								if (flags.separateASCIIandRange)
								{
									seg = true;
								}
							}
						}
					}
					if (flags.keepEnglishWhitespaces)
					{
						if (testContentIdx > 0)
						{
							char prevChar = pwi.Get(typeof(CoreAnnotations.OriginalCharAnnotation))[0];
							char currChar = wi.Get(typeof(CoreAnnotations.OriginalCharAnnotation))[0];
							if (IsLetterASCII(prevChar) && IsLetterASCII(currChar) || IsLetterASCII(prevChar) && ChineseUtils.IsNumber(currChar) || ChineseUtils.IsNumber(prevChar) && IsLetterASCII(currChar))
							{
								// keep the "space" before wi
								if ("1".Equals(wi.Get(typeof(CoreAnnotations.SpaceBeforeAnnotation))))
								{
									seg = true;
								}
							}
						}
					}
					// if there was space and keepAllWhitespaces is true, restore it no matter what
					if (flags.keepAllWhitespaces)
					{
						if (!("0".Equals(wi.Get(typeof(CoreAnnotations.PositionAnnotation)).ToString())) && "1".Equals(wi.Get(typeof(CoreAnnotations.SpaceBeforeAnnotation))))
						{
							seg = true;
						}
					}
					if (seg)
					{
						if (originalWhiteSpace)
						{
							ans.Append('\u1924');
						}
						else
						{
							// a pretty Limbu character which is later changed to a space
							ans.Append(' ');
						}
					}
				}
				ans.Append(wi.Get(typeof(CoreAnnotations.OriginalCharAnnotation)));
				unmod_ans.Append(wi.Get(typeof(CoreAnnotations.OriginalCharAnnotation)));
				unmod_normed_ans.Append(wi.Get(typeof(CoreAnnotations.CharAnnotation)));
			}
			string ansStr = ans.ToString();
			if (flags.sighanPostProcessing)
			{
				if (!flags.keepAllWhitespaces)
				{
					// remove the Limbu char now, so it can be deleted in postprocessing
					ansStr = ansStr.ReplaceAll("\u1924", " ");
				}
				ansStr = PostProcessingAnswer(ansStr, flags);
			}
			// definitely remove the Limbu char if it survived till now
			ansStr = ansStr.ReplaceAll("\u1924", " ");
			return ansStr;
		}

		/// <summary>
		/// post process the answer to be output
		/// these post processing are not dependent on original input
		/// </summary>
		private static string PostProcessingAnswer(string ans, SeqClassifierFlags flags)
		{
			if (flags.useHk)
			{
				//logger.info("Using HK post processing.");
				return hkPostProcessor.PostProcessingAnswer(ans);
			}
			else
			{
				if (flags.useAs)
				{
					//logger.info("Using AS post processing.");
					return asPostProcessor.PostProcessingAnswer(ans);
				}
				else
				{
					if (flags.usePk)
					{
						//logger.info("Using PK post processing.");
						return pkPostProcessor.PostProcessingAnswer(ans, flags.keepAllWhitespaces);
					}
					else
					{
						if (flags.useMsr)
						{
							//logger.info("Using MSR post processing.");
							return basicPostsProcessor.PostProcessingAnswer(ans);
						}
						else
						{
							//logger.info("Using CTB post processing.");
							return ctpPostProcessor.PostProcessingAnswer(ans, flags.suppressMidDotPostprocessing);
						}
					}
				}
			}
		}

		internal class PKPostProcessor : ChineseStringUtils.BaseChinesePostProcessor
		{
			public override string PostProcessingAnswer(string ans)
			{
				return PostProcessingAnswer(ans, true);
			}

			public virtual string PostProcessingAnswer(string ans, bool keepAllWhitespaces)
			{
				ans = SeparatePuncs(ans);
				if (!keepAllWhitespaces)
				{
					/* Note!! All the "digits" are actually extracted/learned from the training data!!!!
					They are not real "digits" knowledge.
					See /u/nlp/data/chinese-segmenter/Sighan2005/dict/wordlist for the list we extracted
					*/
					string numPat = "[0-9\uff10-\uff19\uff0e\u00b7\u4e00\u5341\u767e]+";
					ans = ProcessColons(ans, numPat);
					ans = ProcessPercents(ans, numPat);
					ans = ProcessDots(ans, numPat);
					ans = ProcessCommas(ans);
					/* "\u2014\u2014\u2014" and "\u2026\u2026" should be together */
					string[] puncPatterns = new string[] { "\u2014" + ChineseUtils.White + "\u2014" + ChineseUtils.White + "\u2014", "\u2026" + ChineseUtils.White + "\u2026" };
					string[] correctPunc = new string[] { "\u2014\u2014\u2014", "\u2026\u2026" };
					for (int i = 0; i < puncPatterns.Length; i++)
					{
						Pattern p = patternMap.ComputeIfAbsent(ChineseUtils.White + puncPatterns[i] + ChineseUtils.White, null);
						Matcher m = p.Matcher(ans);
						ans = m.ReplaceAll(" " + correctPunc[i] + " ");
					}
				}
				ans = ans.Trim();
				return ans;
			}
		}

		internal class CTPPostProcessor : ChineseStringUtils.BaseChinesePostProcessor
		{
			public CTPPostProcessor()
			{
				puncs = new char[] { '\u3001', '\u3002', '\u3003', '\u3008', '\u3009', '\u300a', '\u300b', '\u300c', '\u300d', '\u300e', '\u300f', '\u3010', '\u3011', '\u3014', '\u3015', '\u0028', '\u0029', '\u0022', '\u003c', '\u003e' };
			}

			public override string PostProcessingAnswer(string ans)
			{
				return PostProcessingAnswer(ans, true);
			}

			public virtual string PostProcessingAnswer(string ans, bool suppressMidDotPostprocessing)
			{
				string numPat = "[0-9\uff10-\uff19]+";
				ans = SeparatePuncs(ans);
				if (!suppressMidDotPostprocessing)
				{
					ans = GluePunc('\u30fb', ans);
				}
				// this is a 'connector' - the katakana midDot char
				ans = ProcessColons(ans, numPat);
				ans = ProcessPercents(ans, numPat);
				ans = ProcessDots(ans, numPat);
				ans = ProcessCommas(ans);
				return ans.Trim();
			}
		}

		internal class ASPostProcessor : ChineseStringUtils.BaseChinesePostProcessor
		{
			public override string PostProcessingAnswer(string ans)
			{
				ans = SeparatePuncs(ans);
				/* Note!! All the "digits" are actually extracted/learned from the training data!!!!
				They are not real "digits" knowledge.
				See /u/nlp/data/chinese-segmenter/Sighan2005/dict/wordlist for the list we extracted
				*/
				string numPat = "[\uff10-\uff19\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341\u767e\u5343]+";
				ans = ProcessColons(ans, numPat);
				ans = ProcessPercents(ans, numPat);
				ans = ProcessDots(ans, numPat);
				ans = ProcessCommas(ans);
				return ans;
			}
		}

		internal class HKPostProcessor : ChineseStringUtils.BaseChinesePostProcessor
		{
			public HKPostProcessor()
			{
				puncs = new char[] { '\u3001', '\u3002', '\u3003', '\u3008', '\u3009', '\u300a', '\u300b', '\u300c', '\u300d', '\u300e', '\u300f', '\u3010', '\u3011', '\u3014', '\u3015', '\u2103' };
			}

			public override string PostProcessingAnswer(string ans)
			{
				ans = SeparatePuncs(ans);
				/* Note!! All the "digits" are actually extracted/learned from the training data!!!!
				They are not real "digits" knowledge.
				See /u/nlp/data/chinese-segmenter/Sighan2005/dict/wordlist for the list we extracted
				*/
				string numPat = "[0-9]+";
				ans = ProcessColons(ans, numPat);
				/* "\u2014\u2014\u2014" and "\u2026\u2026" should be together */
				string[] puncPatterns = new string[] { "\u2014" + ChineseUtils.White + "\u2014" + ChineseUtils.White + "\u2014", "\u2026" + ChineseUtils.White + "\u2026" };
				string[] correctPunc = new string[] { "\u2014\u2014\u2014", "\u2026\u2026" };
				for (int i = 0; i < puncPatterns.Length; i++)
				{
					Pattern p = patternMap.ComputeIfAbsent(ChineseUtils.White + puncPatterns[i] + ChineseUtils.White, null);
					Matcher m = p.Matcher(ans);
					ans = m.ReplaceAll(" " + correctPunc[i] + " ");
				}
				return ans.Trim();
			}
		}

		internal class BaseChinesePostProcessor
		{
			protected internal static readonly ConcurrentHashMap<string, Pattern> patternMap = new ConcurrentHashMap<string, Pattern>();

			protected internal char[] puncs;

			private Pattern[] colonsPat = null;

			private readonly char[] colons = new char[] { '\ufe55', ':', '\uff1a' };

			private Pattern percentsWhitePat;

			private Pattern[] colonsWhitePat = null;

			public BaseChinesePostProcessor()
			{
				// = null;
				puncs = new char[] { '\u3001', '\u3002', '\u3003', '\u3008', '\u3009', '\u300a', '\u300b', '\u300c', '\u300d', '\u300e', '\u300f', '\u3010', '\u3011', '\u3014', '\u3015' };
			}

			public virtual string PostProcessingAnswer(string ans)
			{
				return SeparatePuncs(ans);
			}

			/* make sure some punctuations will only appeared as one word (segmented from others). */
			/* These punctuations are derived directly from the training set. */
			internal virtual string SeparatePuncs(string ans)
			{
				Pattern[] puncsPat = CompilePunctuationPatterns();
				for (int i = 0; i < puncsPat.Length; i++)
				{
					Pattern p = puncsPat[i];
					char punc = puncs[i];
					Matcher m = p.Matcher(ans);
					ans = m.ReplaceAll(" " + punc + " ");
				}
				return ans.Trim();
			}

			private Pattern[] CompilePunctuationPatterns()
			{
				Pattern[] puncsPat = new Pattern[puncs.Length];
				for (int i = 0; i < puncs.Length; i++)
				{
					char punc = puncs[i];
					puncsPat[i] = patternMap.ComputeIfAbsent(GetEscapedPuncPattern(punc), null);
				}
				return puncsPat;
			}

			private static string GetEscapedPuncPattern(char punc)
			{
				string pattern;
				if (punc == '(' || punc == ')')
				{
					// escape
					pattern = ChineseUtils.White + "\\" + punc + ChineseUtils.White;
				}
				else
				{
					pattern = ChineseUtils.White + punc + ChineseUtils.White;
				}
				return pattern;
			}

			protected internal virtual string ProcessColons(string ans, string numPat)
			{
				/*
				':' 1. if "5:6" then put together
				2. if others, separate ':' and others
				*** Note!! All the "digits" are actually extracted/learned from the training data!!!!
				They are not real "digits" knowledge.
				*** See /u/nlp/data/chinese-segmenter/Sighan2005/dict/wordlist for the list we extracted.
				*/
				// first , just separate all ':'
				CompileColonPatterns();
				for (int i = 0; i < colons.Length; i++)
				{
					char colon = colons[i];
					Pattern p = colonsPat[i];
					Matcher m = p.Matcher(ans);
					ans = m.ReplaceAll(" " + colon + " ");
				}
				CompileColonsWhitePatterns(numPat);
				// second , combine "5:6" patterns
				for (int i_1 = 0; i_1 < colons.Length; i_1++)
				{
					char colon = colons[i_1];
					Pattern p = colonsWhitePat[i_1];
					Matcher m = p.Matcher(ans);
					while (m.Find())
					{
						ans = m.ReplaceAll("$1" + colon + "$2");
						m = p.Matcher(ans);
					}
				}
				ans = ans.Trim();
				return ans;
			}

			private void CompileColonsWhitePatterns(string numPat)
			{
				lock (this)
				{
					if (colonsWhitePat == null)
					{
						colonsWhitePat = new Pattern[colons.Length];
						for (int i = 0; i < colons.Length; i++)
						{
							char colon = colons[i];
							string pattern = "(" + numPat + ")" + ChineseUtils.Whiteplus + colon + ChineseUtils.Whiteplus + "(" + numPat + ")";
							colonsWhitePat[i] = patternMap.ComputeIfAbsent(pattern, null);
						}
					}
				}
			}

			private void CompileColonPatterns()
			{
				lock (this)
				{
					if (colonsPat == null)
					{
						colonsPat = new Pattern[colons.Length];
						for (int i = 0; i < colons.Length; i++)
						{
							char colon = colons[i];
							colonsPat[i] = patternMap.ComputeIfAbsent(ChineseUtils.White + colon + ChineseUtils.White, null);
						}
					}
				}
			}

			protected internal virtual string ProcessPercents(string ans, string numPat)
			{
				//  1. if "6%" then put together
				//  2. if others, separate '%' and others
				// logger.info("Process percents called!");
				// first , just separate all '%'
				Matcher m = percentsPat.Matcher(ans);
				ans = m.ReplaceAll(" $1 ");
				// second , combine "6%" patterns
				percentsWhitePat = patternMap.ComputeIfAbsent("(" + numPat + ")" + percentStr, null);
				Matcher m2 = percentsWhitePat.Matcher(ans);
				ans = m2.ReplaceAll("$1$2");
				ans = ans.Trim();
				return ans;
			}

			protected internal static string ProcessDots(string ans, string numPat)
			{
				/* all "\d\.\d" patterns */
				string dots = "[\ufe52\u2027\uff0e.]";
				Pattern p = patternMap.ComputeIfAbsent("(" + numPat + ")" + ChineseUtils.Whiteplus + "(" + dots + ")" + ChineseUtils.Whiteplus + "(" + numPat + ")", null);
				Matcher m = p.Matcher(ans);
				while (m.Find())
				{
					ans = m.ReplaceAll("$1$2$3");
					m = p.Matcher(ans);
				}
				p = patternMap.ComputeIfAbsent("(" + numPat + ")(" + dots + ")" + ChineseUtils.Whiteplus + "(" + numPat + ")", null);
				m = p.Matcher(ans);
				while (m.Find())
				{
					ans = m.ReplaceAll("$1$2$3");
					m = p.Matcher(ans);
				}
				p = patternMap.ComputeIfAbsent("(" + numPat + ")" + ChineseUtils.Whiteplus + "(" + dots + ")(" + numPat + ")", null);
				m = p.Matcher(ans);
				while (m.Find())
				{
					ans = m.ReplaceAll("$1$2$3");
					m = p.Matcher(ans);
				}
				ans = ans.Trim();
				return ans;
			}

			/// <summary>
			/// The one extant use of this method is to connect a U+30FB (Katakana midDot
			/// with preceding and following non-space characters (in CTB
			/// postprocessing).
			/// </summary>
			/// <remarks>
			/// The one extant use of this method is to connect a U+30FB (Katakana midDot
			/// with preceding and following non-space characters (in CTB
			/// postprocessing). I would hypothesize that if mid dot chars were correctly
			/// recognized in shape contexts, then this would be unnecessary [cdm 2007].
			/// Also, note that IBM GALE normalization seems to produce U+30FB and not
			/// U+00B7.
			/// </remarks>
			/// <param name="punc">character to be joined to surrounding chars</param>
			/// <param name="ans">Input string which may or may not contain punc</param>
			/// <returns>
			/// String with spaces removed between any instance of punc and
			/// surrounding chars.
			/// </returns>
			protected internal static string GluePunc(char punc, string ans)
			{
				Pattern p = patternMap.ComputeIfAbsent(ChineseUtils.White + punc, null);
				Matcher m = p.Matcher(ans);
				ans = m.ReplaceAll(punc.ToString());
				p = patternMap.ComputeIfAbsent(punc + ChineseUtils.White, null);
				m = p.Matcher(ans);
				ans = m.ReplaceAll(punc.ToString());
				ans = ans.Trim();
				return ans;
			}

			protected internal static string ProcessCommas(string ans)
			{
				string numPat = "[0-9\uff10-\uff19]";
				string nonNumPat = "[^0-9\uff10-\uff19]";
				/* all "\d\.\d" patterns */
				string commas = ",";
				ans = ans.ReplaceAll(",", " , ");
				ans = ans.ReplaceAll("  ", " ");
				Pattern p = patternMap.ComputeIfAbsent("(" + numPat + ")" + ChineseUtils.White + "(" + commas + ")" + ChineseUtils.White + "(" + numPat + "{3}" + nonNumPat + ")", null);
				Matcher m = p.Matcher(ans);
				if (m.Find())
				{
					ans = m.ReplaceAll("$1$2$3");
				}
				ans = ans.Trim();
				return ans;
			}
		}
	}
}

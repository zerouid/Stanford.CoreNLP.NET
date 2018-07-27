using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>DocumentReader for Chinese segmentation task.</summary>
	/// <remarks>
	/// DocumentReader for Chinese segmentation task. (Sighan bakeoff 2005)
	/// Reads in characters and labels them as 1 or 0 (word START or NONSTART).
	/// Note: maybe this can do less interning, since some is done in
	/// ObjectBankWrapper, but this also calls trim() as it works....
	/// </remarks>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Michel Galley (Viterbi search graph printing)</author>
	[System.Serializable]
	public class Sighan2005DocumentReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>, ILatticeWriter<CoreLabel, string, int>
	{
		private const long serialVersionUID = 3260295150250263237L;

		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Sighan2005DocumentReaderAndWriter));

		private const bool Debug = false;

		private const bool DebugMore = false;

		private static readonly Pattern dateChars = Pattern.Compile("[\u5E74\u6708\u65E5]");

		private static readonly Pattern dateCharsPlus = Pattern.Compile("[\u5E74\u6708\u65E5\u53f7]");

		private static readonly Pattern numberChars = Pattern.Compile("[0-9\uff10-\uff19" + "\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4E5D\u5341" + "\u96F6\u3007\u767E\u5343\u4E07\u4ebf\u5169\u25cb\u25ef\u3021-\u3029\u3038-\u303A]");

		private static readonly Pattern letterChars = Pattern.Compile("[A-Za-z\uFF21-\uFF3A\uFF41-\uFF5A]");

		private static readonly Pattern periodChars = Pattern.Compile("[\ufe52\u2027\uff0e.\u70B9]");

		private readonly Pattern separatingPuncChars = Pattern.Compile("[]!\"(),;:<=>?\\[\\\\`{|}~^\u3001-\u3003\u3008-\u3011\u3014-\u301F\u3030" + "\uff3d\uff01\uff02\uff08\uff09\uff0c\uff1b\uff1a\uff1c\uff1d\uff1e\uff1f" + "\uff3b\uff3c\uff40\uff5b\uff5c\uff5d\uff5e\uff3e]"
			);

		private readonly Pattern ambiguousPuncChars = Pattern.Compile("[-#$%&'*+/@_\uff0d\uff03\uff04\uff05\uff06\uff07\uff0a\uff0b\uff0f\uff20\uff3f]");

		private readonly Pattern midDotPattern = Pattern.Compile(ChineseUtils.MidDotRegexStr);

		private ChineseDocumentToSentenceProcessor cdtos;

		private ChineseDictionary cdict;

		private ChineseDictionary cdict2;

		private SeqClassifierFlags flags;

		private IIteratorFromReaderFactory<IList<CoreLabel>> factory;

		/* Serializable */
		// year, month, day chars.  Sometime try adding \u53f7 and see if it helps...
		// year, month, day chars.  Adding \u53F7 and seeing if it helps...
		// number chars (Chinese and Western).
		// You get U+25CB circle masquerading as zero in mt data - or even in Sighan 2003 ctb
		// add U+25EF for good measure (larger geometric circle)
		// A-Za-z, narrow and full width
		// two punctuation classes for Low and Ng style features.
		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		public virtual void Init(SeqClassifierFlags flags)
		{
			this.flags = flags;
			factory = LineIterator.GetFactory(new Sighan2005DocumentReaderAndWriter.CTBDocumentParser(this));
			// pichuan : flags.normalizationTable is null --> i believe this is replaced by some java class??
			// (Thu Apr 24 11:10:42 2008)
			cdtos = new ChineseDocumentToSentenceProcessor(flags.normalizationTable);
			if (flags.dictionary != null)
			{
				string[] dicts = flags.dictionary.Split(",");
				cdict = new ChineseDictionary(dicts, cdtos, flags.expandMidDot);
			}
			if (flags.serializedDictionary != null)
			{
				string dict = flags.serializedDictionary;
				cdict = new ChineseDictionary(dict, cdtos, flags.expandMidDot);
			}
			if (flags.dictionary2 != null)
			{
				string[] dicts2 = flags.dictionary2.Split(",");
				cdict2 = new ChineseDictionary(dicts2, cdtos, flags.expandMidDot);
			}
		}

		[System.Serializable]
		internal class CTBDocumentParser : IFunction<string, IList<CoreLabel>>
		{
			private const long serialVersionUID = 3260297180259462337L;

			private string defaultMap = "char=0,answer=1";

			public string[] map = StringUtils.MapStringToArray(this.defaultMap);

			public virtual IList<CoreLabel> Apply(string line)
			{
				if (line == null)
				{
					return null;
				}
				// logger.info("input: " + line);
				//Matcher tagMatcher = tagPattern.matcher(line);
				//line = tagMatcher.replaceAll("");
				line = line.Trim();
				IList<CoreLabel> lwi = new List<CoreLabel>();
				string origLine = line;
				line = this._enclosing.cdtos.Normalization(origLine);
				int origIndex = 0;
				int position = 0;
				StringBuilder nonspaceLineSB = new StringBuilder();
				for (int index = 0; index < len; index++)
				{
					char ch = line[index];
					CoreLabel wi = new CoreLabel();
					if (!char.IsWhiteSpace(ch) && !char.IsISOControl(ch))
					{
						string wordString = char.ToString(ch);
						wi.Set(typeof(CoreAnnotations.CharAnnotation), Sighan2005DocumentReaderAndWriter.Intern(wordString));
						nonspaceLineSB.Append(wordString);
						// non-breaking space is skipped as well
						while (char.IsWhiteSpace(origLine[origIndex]) || char.IsISOControl(origLine[origIndex]) || (origLine[origIndex] == '\u00A0'))
						{
							origIndex++;
						}
						wordString = char.ToString(origLine[origIndex]);
						wi.Set(typeof(CoreAnnotations.OriginalCharAnnotation), Sighan2005DocumentReaderAndWriter.Intern(wordString));
						// put in a word shape
						if (this._enclosing.flags.useShapeStrings)
						{
							wi.Set(typeof(CoreAnnotations.ShapeAnnotation), this._enclosing.ShapeOf(wordString));
						}
						if (this._enclosing.flags.useUnicodeType || this._enclosing.flags.useUnicodeType4gram || this._enclosing.flags.useUnicodeType5gram)
						{
							wi.Set(typeof(CoreAnnotations.UTypeAnnotation), char.GetType(ch));
						}
						if (this._enclosing.flags.useUnicodeBlock)
						{
							wi.Set(typeof(CoreAnnotations.UBlockAnnotation), Characters.UnicodeBlockStringOf(ch));
						}
						origIndex++;
						if (index == 0)
						{
							// first character of a sentence (a line)
							wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "1");
							wi.Set(typeof(CoreAnnotations.SpaceBeforeAnnotation), "1");
							wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "1");
						}
						else
						{
							if (char.IsWhiteSpace(line[index - 1]) || char.IsISOControl(line[index - 1]))
							{
								wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "1");
								wi.Set(typeof(CoreAnnotations.SpaceBeforeAnnotation), "1");
								wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "1");
							}
							else
							{
								wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "0");
								wi.Set(typeof(CoreAnnotations.SpaceBeforeAnnotation), "0");
								wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "0");
							}
						}
						wi.Set(typeof(CoreAnnotations.PositionAnnotation), Sighan2005DocumentReaderAndWriter.Intern((position).ToString()));
						position++;
						lwi.Add(wi);
					}
				}
				if (this._enclosing.flags.dictionary != null || this._enclosing.flags.serializedDictionary != null)
				{
					string nonspaceLine = nonspaceLineSB.ToString();
					Sighan2005DocumentReaderAndWriter.AddDictionaryFeatures(this._enclosing.cdict, typeof(CoreAnnotations.LBeginAnnotation), typeof(CoreAnnotations.LMiddleAnnotation), typeof(CoreAnnotations.LEndAnnotation), nonspaceLine, lwi);
				}
				if (this._enclosing.flags.dictionary2 != null)
				{
					string nonspaceLine = nonspaceLineSB.ToString();
					Sighan2005DocumentReaderAndWriter.AddDictionaryFeatures(this._enclosing.cdict2, typeof(CoreAnnotations.D2_LBeginAnnotation), typeof(CoreAnnotations.D2_LMiddleAnnotation), typeof(CoreAnnotations.D2_LEndAnnotation), nonspaceLine, lwi);
				}
				// logger.info("output: " + lwi.size());
				return lwi;
			}

			internal CTBDocumentParser(Sighan2005DocumentReaderAndWriter _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly Sighan2005DocumentReaderAndWriter _enclosing;
		}

		/// <summary>Calculates a character shape for Chinese.</summary>
		private string ShapeOf(string input)
		{
			string shape;
			if (flags.augmentedDateChars && Sighan2005DocumentReaderAndWriter.dateCharsPlus.Matcher(input).Matches())
			{
				shape = "D";
			}
			else
			{
				if (Sighan2005DocumentReaderAndWriter.dateChars.Matcher(input).Matches())
				{
					shape = "D";
				}
				else
				{
					if (Sighan2005DocumentReaderAndWriter.numberChars.Matcher(input).Matches())
					{
						shape = "N";
					}
					else
					{
						if (Sighan2005DocumentReaderAndWriter.letterChars.Matcher(input).Matches())
						{
							shape = "L";
						}
						else
						{
							if (Sighan2005DocumentReaderAndWriter.periodChars.Matcher(input).Matches())
							{
								shape = "P";
							}
							else
							{
								if (separatingPuncChars.Matcher(input).Matches())
								{
									shape = "S";
								}
								else
								{
									if (ambiguousPuncChars.Matcher(input).Matches())
									{
										shape = "A";
									}
									else
									{
										if (flags.useMidDotShape && midDotPattern.Matcher(input).Matches())
										{
											shape = "M";
										}
										else
										{
											shape = "C";
										}
									}
								}
							}
						}
					}
				}
			}
			return shape;
		}

		private static void AddDictionaryFeatures(ChineseDictionary dict, Type lbeginFieldName, Type lmiddleFieldName, Type lendFieldName, string nonspaceLine, IList<CoreLabel> lwi)
		{
			int lwiSize = lwi.Count;
			if (lwiSize != nonspaceLine.Length)
			{
				throw new Exception();
			}
			int[] lbegin = new int[lwiSize];
			int[] lmiddle = new int[lwiSize];
			int[] lend = new int[lwiSize];
			for (int i = 0; i < lwiSize; i++)
			{
				lbegin[i] = lmiddle[i] = lend[i] = 0;
			}
			for (int i_1 = 0; i_1 < lwiSize; i_1++)
			{
				for (int leng = ChineseDictionary.MaxLexiconLength; leng >= 1; leng--)
				{
					if (i_1 + leng - 1 < lwiSize)
					{
						if (dict.Contains(Sharpen.Runtime.Substring(nonspaceLine, i_1, i_1 + leng)))
						{
							// lbegin
							if (leng > lbegin[i_1])
							{
								lbegin[i_1] = leng;
							}
							// lmid
							int last = i_1 + leng - 1;
							if (leng == ChineseDictionary.MaxLexiconLength)
							{
								last += 1;
							}
							for (int mid = i_1 + 1; mid < last; mid++)
							{
								if (leng > lmiddle[mid])
								{
									lmiddle[mid] = leng;
								}
							}
							// lend
							if (leng < ChineseDictionary.MaxLexiconLength)
							{
								if (leng > lend[i_1 + leng - 1])
								{
									lend[i_1 + leng - 1] = leng;
								}
							}
						}
					}
				}
			}
			for (int i_2 = 0; i_2 < lwiSize; i_2++)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(lbegin[i_2]);
				if (lbegin[i_2] == ChineseDictionary.MaxLexiconLength)
				{
					sb.Append("+");
				}
				lwi[i_2].Set(lbeginFieldName, sb.ToString());
				sb = new StringBuilder();
				sb.Append(lmiddle[i_2]);
				if (lmiddle[i_2] == ChineseDictionary.MaxLexiconLength)
				{
					sb.Append("+");
				}
				lwi[i_2].Set(lmiddleFieldName, sb.ToString());
				sb = new StringBuilder();
				sb.Append(lend[i_2]);
				if (lend[i_2] == ChineseDictionary.MaxLexiconLength)
				{
					sb.Append("+");
				}
				lwi[i_2].Set(lendFieldName, sb.ToString());
			}
		}

		//logger.info(lwi.get(i));
		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter pw)
		{
			string ansStr = ChineseStringUtils.CombineSegmentedSentence(doc, flags);
			pw.Print(ansStr);
			pw.Println();
		}

		private static string Intern(string s)
		{
			return string.Intern(s.Trim());
		}

		public virtual void PrintLattice(DFSA<string, int> tagLattice, IList<CoreLabel> doc, PrintWriter @out)
		{
			CoreLabel[] docArray = Sharpen.Collections.ToArray(doc, new CoreLabel[doc.Count]);
			// Create answer lattice:
			MutableInteger nodeId = new MutableInteger(0);
			DFSA<string, int> answerLattice = new DFSA<string, int>(null);
			DFSAState<string, int> aInitState = new DFSAState<string, int>(nodeId, answerLattice);
			answerLattice.SetInitialState(aInitState);
			IDictionary<DFSAState<string, int>, DFSAState<string, int>> stateLinks = Generics.NewHashMap();
			// Convert binary lattice into word lattice:
			TagLatticeToAnswerLattice(tagLattice.InitialState(), aInitState, new StringBuilder(string.Empty), nodeId, 0, 0.0, stateLinks, answerLattice, docArray);
			try
			{
				answerLattice.PrintAttFsmFormat(@out);
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		/// <summary>
		/// Recursively builds an answer lattice (Chinese words) from a Viterbi search graph
		/// of binary predictions.
		/// </summary>
		/// <remarks>
		/// Recursively builds an answer lattice (Chinese words) from a Viterbi search graph
		/// of binary predictions. This function does a limited amount of post-processing:
		/// preserve white spaces of the input, and not segment between two latin characters or
		/// between two digits. Consequently, the probabilities of all paths in answerLattice
		/// may not sum to 1 (they do sum to 1 if no post processing applies).
		/// </remarks>
		/// <param name="tSource">Current node in Viterbi search graph.</param>
		/// <param name="aSource">Current node in answer lattice.</param>
		/// <param name="answer">Partial word starting at aSource.</param>
		/// <param name="nodeId">Currently unused node identifier for answer graph.</param>
		/// <param name="pos">Current position in docArray.</param>
		/// <param name="cost">Current cost of answer.</param>
		/// <param name="stateLinks">
		/// Maps nodes of the search graph to nodes in answer lattice
		/// (when paths of the search graph are recombined, paths of the answer lattice should be
		/// recombined as well, if at word boundary).
		/// </param>
		private void TagLatticeToAnswerLattice(DFSAState<string, int> tSource, DFSAState<string, int> aSource, StringBuilder answer, MutableInteger nodeId, int pos, double cost, IDictionary<DFSAState<string, int>, DFSAState<string, int>> stateLinks, 
			DFSA<string, int> answerLattice, CoreLabel[] docArray)
		{
			// Add "1" prediction after the end of the sentence, if applicable:
			if (tSource.IsAccepting() && tSource.ContinuingInputs().IsEmpty())
			{
				tSource.AddTransition(new DFSATransition<string, int>(string.Empty, tSource, new DFSAState<string, int>(-1, null), "1", string.Empty, 0));
			}
			// Get current label, character, and prediction:
			CoreLabel curLabel = (pos < docArray.Length) ? docArray[pos] : null;
			string curChr = null;
			string origSpace = null;
			if (curLabel != null)
			{
				curChr = curLabel.Get(typeof(CoreAnnotations.OriginalCharAnnotation));
				System.Diagnostics.Debug.Assert((curChr.Length == 1));
				origSpace = curLabel.Get(typeof(CoreAnnotations.SpaceBeforeAnnotation));
			}
			// Get set of successors in search graph:
			ICollection<string> inputs = tSource.ContinuingInputs();
			// Only keep most probable transition out of initial state:
			string answerConstraint = null;
			if (pos == 0)
			{
				double minCost = double.PositiveInfinity;
				// DFSATransition<String, Integer> bestTransition = null;
				foreach (string predictSpace in inputs)
				{
					DFSATransition<string, int> transition = tSource.Transition(predictSpace);
					double transitionCost = transition.Score();
					if (transitionCost < minCost)
					{
						if (predictSpace != null)
						{
							logger.Info(string.Format("mincost (%s): %e -> %e%n", predictSpace, minCost, transitionCost));
							minCost = transitionCost;
							answerConstraint = predictSpace;
						}
					}
				}
			}
			// Follow along each transition:
			foreach (string predictSpace_1 in inputs)
			{
				DFSATransition<string, int> transition = tSource.Transition(predictSpace_1);
				DFSAState<string, int> tDest = transition.Target();
				DFSAState<string, int> newASource = aSource;
				//logger.info(String.format("tsource=%s tdest=%s asource=%s pos=%d predictSpace=%s%n", tSource, tDest, newASource, pos, predictSpace));
				StringBuilder newAnswer = new StringBuilder(answer.ToString());
				int answerLen = newAnswer.Length;
				string prevChr = (answerLen > 0) ? newAnswer.Substring(answerLen - 1) : null;
				double newCost = cost;
				// Ignore paths starting with zero:
				if (answerConstraint != null && !answerConstraint.Equals(predictSpace_1))
				{
					logger.Info(string.Format("Skipping transition %s at pos 0.%n", predictSpace_1));
					continue;
				}
				// Ignore paths not consistent with input segmentation:
				if (flags.keepAllWhitespaces && "0".Equals(predictSpace_1) && "1".Equals(origSpace))
				{
					logger.Info(string.Format("Skipping non-boundary at pos %d, since space in the input.%n", pos));
					continue;
				}
				// Ignore paths adding segment boundaries between two latin characters, or between two digits:
				// (unless already present in original input)
				if ("1".Equals(predictSpace_1) && "0".Equals(origSpace) && prevChr != null && curChr != null)
				{
					char p = prevChr[0];
					char c = curChr[0];
					if (ChineseStringUtils.IsLetterASCII(p) && ChineseStringUtils.IsLetterASCII(c))
					{
						logger.Info(string.Format("Not hypothesizing a boundary at pos %d, since between two ASCII letters (%s and %s).%n", pos, prevChr, curChr));
						continue;
					}
					if (ChineseUtils.IsNumber(p) && ChineseUtils.IsNumber(c))
					{
						logger.Info(string.Format("Not hypothesizing a boundary at pos %d, since between two numeral characters (%s and %s).%n", pos, prevChr, curChr));
						continue;
					}
				}
				// If predictSpace==1, create a new transition in answer search graph:
				if ("1".Equals(predictSpace_1))
				{
					if (newAnswer.ToString().Length > 0)
					{
						// If answer destination node visited before, create a new edge and leave:
						if (stateLinks.Contains(tSource))
						{
							DFSAState<string, int> aDest = stateLinks[tSource];
							newASource.AddTransition(new DFSATransition<string, int>(string.Empty, newASource, aDest, newAnswer.ToString(), string.Empty, newCost));
							//logger.info(String.format("new transition: asource=%s adest=%s edge=%s%n", newASource, aDest, newAnswer));
							continue;
						}
						// If answer destination node not visited before, create it + new edge:
						nodeId.IncValue(1);
						DFSAState<string, int> aDest_1 = new DFSAState<string, int>(nodeId, answerLattice, 0.0);
						stateLinks[tSource] = aDest_1;
						newASource.AddTransition(new DFSATransition<string, int>(string.Empty, newASource, aDest_1, newAnswer.ToString(), string.Empty, newCost));
						//logger.info(String.format("new edge: adest=%s%n", newASource, aDest, newAnswer));
						//logger.info(String.format("new transition: asource=%s adest=%s edge=%s%n%n%n", newASource, aDest, newAnswer));
						// Reached an accepting state:
						if (tSource.IsAccepting())
						{
							aDest_1.SetAccepting(true);
							continue;
						}
						// Start new answer edge:
						newASource = aDest_1;
						newAnswer = new StringBuilder();
						newCost = 0.0;
					}
				}
				System.Diagnostics.Debug.Assert((curChr != null));
				newAnswer.Append(curChr);
				newCost += transition.Score();
				if (newCost < flags.searchGraphPrune || ChineseStringUtils.IsLetterASCII(curChr[0]))
				{
					TagLatticeToAnswerLattice(tDest, newASource, newAnswer, nodeId, pos + 1, newCost, stateLinks, answerLattice, docArray);
				}
			}
		}
	}
}

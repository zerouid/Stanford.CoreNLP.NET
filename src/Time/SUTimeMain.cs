using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Java.Util.Logging;
using Java.Util.Regex;
using Org.W3c.Dom;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Main program for testing SUTime.</summary>
	/// <remarks>
	/// Main program for testing SUTime.
	/// <br />
	/// Processing a text string:
	/// <pre>
	/// -in.type TEXT
	/// -date YYYY-MM-dd
	/// -i &lt;text&gt;
	/// -o &lt;output file&gt;
	/// </pre>
	/// Processing a text file:
	/// <pre>
	/// -in.type TEXTFILE
	/// -date YYYY-MM-dd
	/// -i input.txt
	/// -o &lt;output file&gt;
	/// </pre>
	/// Running on Timebank
	/// <pre>
	/// -in.type TIMEBANK_CSV
	/// -i timebank.csv
	/// -tempeval2.dct dct.txt
	/// -o &lt;output directory&gt;
	/// -eval &lt;evaluation script&gt;
	/// </pre>
	/// Evaluating on Tempeval2
	/// <pre>
	/// -in.type TEMPEVAL2
	/// -i &lt;directory with english data&gt;
	/// -o &lt;output directory&gt;
	/// -eval &lt;evaluation script&gt;
	/// -tempeval2.dct dct file (with document creation times)
	/// TEMPEVAL2 (download from http://timeml.org/site/timebank/timebank.html)
	/// Evaluation is token based.
	/// TRAINING (english):
	/// GUTIME:
	/// precision   0.88
	/// recall      0.71
	/// f1-measure  0.79
	/// accuracy    0.98
	/// attribute type       0.92
	/// attribute value      0.31   // LOW SCORE here is due to difference in format (no -,: in date)
	/// After fixing some formats for GUTIME:
	/// (GUTIME syntax is inconsistent at times (1991W 8WE, 19980212EV)
	/// attribute value      0.67
	/// SUTIME:
	/// Default: sutime.teRelHeurLevel=NONE, restrictToTimex3=false
	/// precision   0.873
	/// recall      0.897
	/// f1-measure  0.885
	/// accuracy    0.991
	/// P      R    F1
	/// attribute type       0.918 | 0.751 0.802 0.776
	/// attribute value      0.762 | 0.623 0.665 0.644
	/// P      R    F1
	/// mention attribute type       0.900 | 0.780 0.833 0.805
	/// mention attribute value      0.742 | 0.643 0.687 0.664
	/// sutime.teRelHeurLevel=MORE, restrictToTimex3=true
	/// precision   0.876
	/// recall      0.889
	/// f1-measure  0.882
	/// accuracy    0.991
	/// P      R    F1
	/// attribute type       0.918 | 0.744 0.798 0.770
	/// attribute value      0.776 | 0.629 0.675 0.651
	/// P      R    F1
	/// mention attribute type       0.901 | 0.780 0.836 0.807
	/// mention attribute value      0.750 | 0.649 0.696 0.672
	/// ------------------------------------------------------------------------------
	/// TEST (english):
	/// GUTIME:
	/// precision   0.89
	/// recall      0.79
	/// f1-measure  0.84
	/// accuracy    0.99
	/// attribute type       0.95
	/// attribute value      0.68
	/// SUTIME:
	/// Default: sutime.teRelHeurLevel=NONE, restrictToTimex3=false
	/// precision   0.878
	/// recall      0.963
	/// f1-measure  0.918
	/// accuracy    0.996
	/// P      R    F1
	/// attribute type       0.953 | 0.820 0.904 0.860
	/// attribute value      0.791 | 0.680 0.750 0.713
	/// P      R    F1
	/// mention attribute type       0.954 | 0.837 0.923 0.878
	/// mention attribute value      0.781 | 0.686 0.756 0.720
	/// sutime.teRelHeurLevel=MORE, restrictToTimex3=true
	/// precision   0.881
	/// recall      0.963
	/// f1-measure  0.920
	/// accuracy    0.995
	/// P      R    F1
	/// attribute type       0.959 | 0.821 0.910 0.863
	/// attribute value      0.818 | 0.699 0.776 0.736
	/// P      R    F1
	/// mention attribute type       0.961 | 0.844 0.936 0.888
	/// mention attribute value      0.803 | 0.705 0.782 0.742
	/// </pre>
	/// </remarks>
	/// <author>Angel Chang</author>
	public class SUTimeMain
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Time.SUTimeMain));

		protected internal static string Python = null;

		private SUTimeMain()
		{
		}

		private class EvalStats
		{
			internal PrecisionRecallStats prStats = new PrecisionRecallStats();

			internal PrecisionRecallStats valPrStats = new PrecisionRecallStats();

			internal PrecisionRecallStats estPrStats = new PrecisionRecallStats();
			// static class
			/*
			* Other Time corpora: (see also http://timeml.org/site/timebank/timebank.html)
			* LDC2006T08 TimeBank 1.2 (Uses TIMEX3)
			* LDC2005T07 ACE Time Normalization (TERN) 2004 English Training Data v 1.0 (Uses TIMEX2)
			*   GUTime achieved .85, .78, and .82 F-measure for timex2, text, and val fields
			* LDC2010T18 ACE Time Normalization (TERN) 2004 English Evaluation Data V1.0
			*/
			////////////////////////////////////////////////////////////////////////////////////////
			//    PrecisionRecallStats tokenPrStats = new PrecisionRecallStats();
		}

		private class TimebankTimex
		{
			internal string timexId;

			internal string timexVal;

			internal string timexOrigVal;

			internal string timexStr;

			internal int tid;

			private TimebankTimex(string timexId, string timexVal, string timexOrigVal, string timexStr)
			{
				this.timexId = timexId;
				this.timexVal = timexVal;
				this.timexOrigVal = timexOrigVal;
				this.timexStr = timexStr;
				if (timexId != null && timexId.Length > 0)
				{
					tid = System.Convert.ToInt32(timexId);
				}
			}
		}

		private class TimebankSent
		{
			internal bool initialized = false;

			internal string docId;

			internal string docFilename;

			internal string docPubDate;

			internal string sentId;

			internal string text;

			internal IList<SUTimeMain.TimebankTimex> timexes = new List<SUTimeMain.TimebankTimex>();

			internal IList<string> origItems = new List<string>();

			public virtual bool Add(string item)
			{
				string[] fields = item.Split("\\s*\\|\\s*", 9);
				string docId = fields[0];
				string docFilename = fields[1];
				string docPubDate = fields[2];
				string sentId = fields[3];
				string sent = fields[8];
				if (initialized)
				{
					// check compatibility;
					if (!docId.Equals(this.docId) || !sentId.Equals(this.sentId))
					{
						return false;
					}
				}
				else
				{
					this.docId = docId;
					this.docFilename = docFilename;
					this.docPubDate = docPubDate;
					this.sentId = sentId;
					this.text = sent;
					initialized = true;
				}
				origItems.Add(item);
				string timexId = fields[4];
				string timexVal = fields[5];
				string timexOrigVal = fields[6];
				string timexStr = fields[7];
				if (timexId != null && timexId.Length > 0)
				{
					timexes.Add(new SUTimeMain.TimebankTimex(timexId, timexVal, timexOrigVal, timexStr));
				}
				return true;
			}
		}

		//Overall: PrecisionRecallStats[tp=877,fp=199,fn=386,p=0.82  (877/1076),r=0.69  (877/1263),f1=0.75]
		//Value: PrecisionRecallStats[tp=229,fp=199,fn=1034,p=0.54  (229/428),r=0.18  (229/1263),f1=0.27]
		// Process one item from timebank CSV file
		private static void ProcessTimebankCsvSent(AnnotationPipeline pipeline, SUTimeMain.TimebankSent sent, PrintWriter pw, SUTimeMain.EvalStats evalStats)
		{
			if (sent != null)
			{
				sent.timexes.Sort(null);
				pw.Println();
				foreach (string item in sent.origItems)
				{
					pw.Println("PROC |" + item);
				}
				Annotation annotation = new Annotation(sent.text);
				annotation.Set(typeof(CoreAnnotations.DocDateAnnotation), sent.docPubDate);
				pipeline.Annotate(annotation);
				IList<ICoreMap> timexes = annotation.Get(typeof(TimeAnnotations.TimexAnnotations));
				int i = 0;
				foreach (ICoreMap t in timexes)
				{
					string[] newFields;
					if (sent.timexes.Count > i)
					{
						string res;
						SUTimeMain.TimebankTimex goldTimex = sent.timexes[i];
						Timex guessTimex = t.Get(typeof(TimeAnnotations.TimexAnnotation));
						string s1 = goldTimex.timexStr.ReplaceAll("\\s+", string.Empty);
						string s2 = guessTimex.Text().ReplaceAll("\\s+", string.Empty);
						if (s1.Equals(s2))
						{
							evalStats.estPrStats.IncrementTP();
							res = "OK";
						}
						else
						{
							evalStats.estPrStats.IncrementFP();
							evalStats.estPrStats.IncrementFN();
							res = "BAD";
						}
						newFields = new string[] { res, goldTimex.timexId, goldTimex.timexVal, goldTimex.timexOrigVal, goldTimex.timexStr, t.Get(typeof(TimeAnnotations.TimexAnnotation)).ToString() };
						i++;
					}
					else
					{
						newFields = new string[] { "NONE", t.Get(typeof(TimeAnnotations.TimexAnnotation)).ToString() };
						evalStats.estPrStats.IncrementFP();
					}
					pw.Println("GOT | " + StringUtils.Join(newFields, "|"));
				}
				for (; i < sent.timexes.Count; i++)
				{
					evalStats.estPrStats.IncrementFN();
				}
				i = 0;
				int lastIndex = 0;
				foreach (SUTimeMain.TimebankTimex goldTimex_1 in sent.timexes)
				{
					int index = sent.text.IndexOf(goldTimex_1.timexStr, lastIndex);
					int endIndex = index + goldTimex_1.timexStr.Length;
					bool found = false;
					for (; i < timexes.Count; i++)
					{
						ICoreMap t_1 = timexes[i];
						if (t_1.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) >= endIndex)
						{
							break;
						}
						else
						{
							if (t_1.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) >= index)
							{
								found = true;
								evalStats.prStats.IncrementTP();
								if (goldTimex_1.timexOrigVal.Equals(t_1.Get(typeof(TimeAnnotations.TimexAnnotation)).Value()))
								{
									evalStats.valPrStats.IncrementTP();
								}
								else
								{
									evalStats.valPrStats.IncrementFN();
								}
							}
							else
							{
								evalStats.prStats.IncrementFP();
								evalStats.valPrStats.IncrementFP();
							}
						}
					}
					if (!found)
					{
						evalStats.prStats.IncrementFN();
						evalStats.valPrStats.IncrementFN();
					}
					lastIndex = endIndex;
				}
				for (; i < timexes.Count; i++)
				{
					evalStats.prStats.IncrementFP();
					evalStats.valPrStats.IncrementFP();
				}
			}
		}

		// Process CSV file with just timebank sentences with time expressions
		/// <exception cref="System.IO.IOException"/>
		public static void ProcessTimebankCsv(AnnotationPipeline pipeline, string @in, string @out, string eval)
		{
			BufferedReader br = IOUtils.GetBufferedFileReader(@in);
			PrintWriter pw = (@out != null) ? IOUtils.GetPrintWriter(@out) : new PrintWriter(System.Console.Out);
			string line;
			//    boolean dataStarted = false;
			bool dataStarted = true;
			SUTimeMain.TimebankSent sent = new SUTimeMain.TimebankSent();
			string item = null;
			SUTimeMain.EvalStats evalStats = new SUTimeMain.EvalStats();
			line = br.ReadLine();
			// Skip first line
			while ((line = br.ReadLine()) != null)
			{
				if (line.Trim().Length == 0)
				{
					continue;
				}
				if (dataStarted)
				{
					if (line.Contains("|"))
					{
						if (item != null)
						{
							bool addOld = sent.Add(item);
							if (!addOld)
							{
								ProcessTimebankCsvSent(pipeline, sent, pw, evalStats);
								sent = new SUTimeMain.TimebankSent();
								sent.Add(item);
							}
						}
						item = line;
					}
					else
					{
						item += " " + line;
					}
				}
				else
				{
					if (line.Matches("#+ BEGIN DATA #+"))
					{
						dataStarted = true;
					}
				}
			}
			if (item != null)
			{
				bool addOld = sent.Add(item);
				if (!addOld)
				{
					ProcessTimebankCsvSent(pipeline, sent, pw, evalStats);
					sent = new SUTimeMain.TimebankSent();
					sent.Add(item);
				}
				ProcessTimebankCsvSent(pipeline, sent, pw, evalStats);
			}
			br.Close();
			if (@out != null)
			{
				pw.Close();
			}
			System.Console.Out.WriteLine("Estimate: " + evalStats.estPrStats.ToString(2));
			System.Console.Out.WriteLine("Overall: " + evalStats.prStats.ToString(2));
			System.Console.Out.WriteLine("Value: " + evalStats.valPrStats.ToString(2));
		}

		private static string JoinWordTags<_T0>(IList<_T0> l, string glue, int start, int end)
			where _T0 : ICoreMap
		{
			return StringUtils.Join(l, glue, null, start, end);
		}

		private static void ProcessTempEval2Doc(AnnotationPipeline pipeline, Annotation docAnnotation, IDictionary<string, IList<SUTimeMain.TimexAttributes>> timexMap, PrintWriter extPw, PrintWriter attrPw, PrintWriter debugPw, PrintWriter attrDebugPwGold
			, PrintWriter attrDebugPw)
		{
			pipeline.Annotate(docAnnotation);
			string docId = docAnnotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
			string docDate = docAnnotation.Get(typeof(CoreAnnotations.DocDateAnnotation));
			IList<ICoreMap> sents = docAnnotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (timexMap != null)
			{
				IList<SUTimeMain.TimexAttributes> golds = UpdateTimexText(timexMap, docAnnotation);
				if (attrDebugPwGold != null && golds != null)
				{
					foreach (SUTimeMain.TimexAttributes g in golds)
					{
						string[] newFields = new string[] { docId, docDate, g.sentIndex.ToString(), g.tokenStart.ToString(), g.tokenEnd.ToString(), g.type, g.value, g.text, g.context };
						/*g.tid, */
						attrDebugPwGold.Println(StringUtils.Join(newFields, "\t"));
					}
				}
			}
			if (attrDebugPw != null)
			{
				foreach (ICoreMap sent in sents)
				{
					IList<ICoreMap> timexes = sent.Get(typeof(TimeAnnotations.TimexAnnotations));
					if (timexes != null)
					{
						foreach (ICoreMap t in timexes)
						{
							Timex timex = t.Get(typeof(TimeAnnotations.TimexAnnotation));
							int sentIndex = sent.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
							int sentTokenStart = sent.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
							int tokenStart;
							int tokenEnd;
							if (t.ContainsKey(typeof(CoreAnnotations.TokenBeginAnnotation)))
							{
								tokenStart = t.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - sentTokenStart;
								tokenEnd = t.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - sentTokenStart;
							}
							else
							{
								ICoreMap cm = ChunkAnnotationUtils.GetAnnotatedChunkUsingCharOffsets(docAnnotation, t.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), t.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
								tokenStart = cm.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - sentTokenStart;
								tokenEnd = cm.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - sentTokenStart;
							}
							string context = JoinWordTags(sent.Get(typeof(CoreAnnotations.TokensAnnotation)), " ", tokenStart - 3, tokenEnd + 3);
							string[] newFields = new string[] { docId, docDate, sentIndex.ToString(), tokenStart.ToString(), tokenEnd.ToString(), timex.TimexType(), timex.Value(), timex.Text(), context };
							/*timex.tid(), */
							attrDebugPw.Println(StringUtils.Join(newFields, "\t"));
						}
					}
				}
			}
			if (debugPw != null)
			{
				IList<ICoreMap> timexes = docAnnotation.Get(typeof(TimeAnnotations.TimexAnnotations));
				foreach (ICoreMap t in timexes)
				{
					string[] newFields = new string[] { docId, docDate, t.Get(typeof(TimeAnnotations.TimexAnnotation)).ToString() };
					debugPw.Println("GOT | " + StringUtils.Join(newFields, "|"));
				}
			}
			if (extPw != null || attrPw != null)
			{
				foreach (ICoreMap sent in sents)
				{
					int sentTokenBegin = sent.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					foreach (ICoreMap t in sent.Get(typeof(TimeAnnotations.TimexAnnotations)))
					{
						Timex tmx = t.Get(typeof(TimeAnnotations.TimexAnnotation));
						IList<CoreLabel> tokens = t.Get(typeof(CoreAnnotations.TokensAnnotation));
						int tokenIndex = 0;
						if (tokens == null)
						{
							ICoreMap cm = ChunkAnnotationUtils.GetAnnotatedChunkUsingCharOffsets(docAnnotation, t.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), t.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
							tokens = cm.Get(typeof(CoreAnnotations.TokensAnnotation));
							tokenIndex = cm.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
						}
						else
						{
							tokenIndex = t.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
						}
						tokenIndex = tokenIndex - sentTokenBegin;
						string sentenceIndex = sent.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)).ToString();
						int tokenCount = 0;
						foreach (CoreLabel token in tokens)
						{
							string[] extFields = new string[] { docId, sentenceIndex, tokenIndex.ToString(), "timex3", tmx.Tid(), "1" };
							string extString = StringUtils.Join(extFields, "\t");
							if (extPw != null)
							{
								extPw.Println(extString);
							}
							if (attrPw != null)
							{
								/* && tokenCount == 0 */
								string[] attrFields = new string[] { "type", tmx.TimexType() };
								attrPw.Println(extString + "\t" + StringUtils.Join(attrFields, "\t"));
								if (tmx.Value() != null)
								{
									string val = tmx.Value();
									// Fix up expression values (needed for GUTime)
									if (useGUTime)
									{
										if ("TIME".Equals(tmx.TimexType()))
										{
											if (val.Matches("T\\d{4}"))
											{
												val = "T" + Sharpen.Runtime.Substring(val, 1, 3) + ":" + Sharpen.Runtime.Substring(val, 3, 5);
											}
										}
										else
										{
											if ("DATE".Equals(tmx.TimexType()))
											{
												if (val.Matches("\\d{8}T.*"))
												{
													val = Sharpen.Runtime.Substring(val, 0, 4) + "-" + Sharpen.Runtime.Substring(val, 4, 6) + "-" + Sharpen.Runtime.Substring(val, 6);
												}
												else
												{
													if (val.Matches("\\d{8}"))
													{
														val = Sharpen.Runtime.Substring(val, 0, 4) + "-" + Sharpen.Runtime.Substring(val, 4, 6) + "-" + Sharpen.Runtime.Substring(val, 6, 8);
													}
													else
													{
														if (val.Matches("\\d\\d\\d\\d.."))
														{
															val = Sharpen.Runtime.Substring(val, 0, 4) + "-" + Sharpen.Runtime.Substring(val, 4, 6);
														}
														else
														{
															if (val.Matches("[0-9X]{4}W[0-9X]{2}.*"))
															{
																if (val.Length > 7)
																{
																	val = Sharpen.Runtime.Substring(val, 0, 4) + "-" + Sharpen.Runtime.Substring(val, 4, 7) + "-" + Sharpen.Runtime.Substring(val, 7);
																}
																else
																{
																	val = Sharpen.Runtime.Substring(val, 0, 4) + "-" + Sharpen.Runtime.Substring(val, 4, 7);
																}
															}
														}
													}
												}
											}
										}
									}
									/*else {
									// SUTIME
									if ("DATE".equals(tmx.timexType())) {
									if (val.matches("\\d\\d\\dX")) {
									val = val.substring(0,3);  // Convert 199X to 199
									}
									}
									}   */
									attrFields[0] = "value";
									attrFields[1] = val;
									attrPw.Println(extString + "\t" + StringUtils.Join(attrFields, "\t"));
								}
							}
							tokenIndex++;
							tokenCount++;
						}
					}
				}
			}
		}

		private static CoreLabelTokenFactory tokenFactory = new CoreLabelTokenFactory();

		private static ICoreMap WordsToSentence(IList<string> sentWords)
		{
			string sentText = StringUtils.Join(sentWords, " ");
			Annotation sentence = new Annotation(sentText);
			IList<CoreLabel> tokens = new List<CoreLabel>(sentWords.Count);
			foreach (string text in sentWords)
			{
				CoreLabel token = tokenFactory.MakeToken();
				token.Set(typeof(CoreAnnotations.TextAnnotation), text);
				tokens.Add(token);
			}
			sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			return sentence;
		}

		public static Annotation SentencesToDocument(string documentID, string docDate, IList<ICoreMap> sentences)
		{
			string docText = ChunkAnnotationUtils.GetTokenText(sentences, typeof(CoreAnnotations.TextAnnotation));
			Annotation document = new Annotation(docText);
			document.Set(typeof(CoreAnnotations.DocIDAnnotation), documentID);
			document.Set(typeof(CoreAnnotations.DocDateAnnotation), docDate);
			document.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
			// Accumulate docTokens and label sentence with overall token begin/end, and sentence index annotations
			IList<CoreLabel> docTokens = new List<CoreLabel>();
			int sentenceIndex = 0;
			int tokenBegin = 0;
			foreach (ICoreMap sentenceAnnotation in sentences)
			{
				IList<CoreLabel> sentenceTokens = sentenceAnnotation.Get(typeof(CoreAnnotations.TokensAnnotation));
				Sharpen.Collections.AddAll(docTokens, sentenceTokens);
				int tokenEnd = tokenBegin + sentenceTokens.Count;
				sentenceAnnotation.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenBegin);
				sentenceAnnotation.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenEnd);
				sentenceAnnotation.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentenceIndex);
				sentenceIndex++;
				tokenBegin = tokenEnd;
			}
			document.Set(typeof(CoreAnnotations.TokensAnnotation), docTokens);
			// Put in character offsets
			int i = 0;
			foreach (CoreLabel token in docTokens)
			{
				string tokenText = token.Get(typeof(CoreAnnotations.TextAnnotation));
				token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), i);
				i += tokenText.Length;
				token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), i);
				i++;
			}
			// Skip space
			foreach (ICoreMap sentenceAnnotation_1 in sentences)
			{
				IList<CoreLabel> sentenceTokens = sentenceAnnotation_1.Get(typeof(CoreAnnotations.TokensAnnotation));
				sentenceAnnotation_1.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), sentenceTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
				sentenceAnnotation_1.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), sentenceTokens[sentenceTokens.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			}
			return document;
		}

		private class TimexAttributes
		{
			public string tid;

			public int sentIndex;

			public int tokenStart;

			public int tokenEnd;

			public string text;

			public string type;

			public string value;

			public string context;

			public TimexAttributes(string tid, int sentIndex, int tokenIndex)
			{
				this.tid = tid;
				this.sentIndex = sentIndex;
				this.tokenStart = tokenIndex;
				this.tokenEnd = tokenIndex + 1;
			}
		}

		private static SUTimeMain.TimexAttributes FindTimex(IDictionary<string, IList<SUTimeMain.TimexAttributes>> timexMap, string docId, string tid)
		{
			// Find entry
			IList<SUTimeMain.TimexAttributes> list = timexMap[docId];
			foreach (SUTimeMain.TimexAttributes timex in list)
			{
				if (timex.tid.Equals(tid))
				{
					return timex;
				}
			}
			return null;
		}

		private static IList<SUTimeMain.TimexAttributes> UpdateTimexText(IDictionary<string, IList<SUTimeMain.TimexAttributes>> timexMap, Annotation docAnnotation)
		{
			// Find entry
			string docId = docAnnotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
			IList<ICoreMap> sents = docAnnotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<SUTimeMain.TimexAttributes> list = timexMap[docId];
			if (list != null)
			{
				foreach (SUTimeMain.TimexAttributes timex in list)
				{
					ICoreMap sent = sents[timex.sentIndex];
					IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
					timex.text = StringUtils.JoinWords(tokens, " ", timex.tokenStart, timex.tokenEnd);
					timex.context = JoinWordTags(tokens, " ", timex.tokenStart - 3, timex.tokenEnd + 3);
				}
				/*        StringBuilder sb = new StringBuilder("");
				for (int i = timex.tokenStart; i < timex.tokenEnd; i++) {
				if (sb.length() > 0) { sb.append(" "); }
				sb.append(tokens.get(i).word());
				}
				timex.text = sb.toString();
				
				// Get context
				sb.setLength(0);
				int c1 = Math.max(0, timex.tokenStart - 3);
				int c2 = Math.min(tokens.size(), timex.tokenEnd + 3);
				for (int i = c1; i < c2; i++) {
				if (sb.length() > 0) { sb.append(" "); }
				sb.append(tokens.get(i).word());
				}
				timex.context = sb.toString();             */
				return list;
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"/>
		private static IDictionary<string, IList<SUTimeMain.TimexAttributes>> ReadTimexAttrExts(string extentsFile, string attrsFile)
		{
			IDictionary<string, IList<SUTimeMain.TimexAttributes>> timexMap = Generics.NewHashMap();
			BufferedReader extBr = IOUtils.GetBufferedFileReader(extentsFile);
			string line;
			string lastDocId = null;
			SUTimeMain.TimexAttributes lastTimex = null;
			while ((line = extBr.ReadLine()) != null)
			{
				if (line.Trim().IsEmpty())
				{
					continue;
				}
				// Simple tab delimited file
				string[] fields = line.Split("\t");
				string docName = fields[0];
				int sentNo = System.Convert.ToInt32(fields[1]);
				int tokenNo = System.Convert.ToInt32(fields[2]);
				string tid = fields[4];
				if (lastDocId != null && lastDocId.Equals(docName) && lastTimex != null && lastTimex.tid.Equals(tid))
				{
					// Expand previous
					System.Diagnostics.Debug.Assert((lastTimex.sentIndex == sentNo));
					lastTimex.tokenEnd = tokenNo + 1;
				}
				else
				{
					lastDocId = docName;
					lastTimex = new SUTimeMain.TimexAttributes(tid, sentNo, tokenNo);
					IList<SUTimeMain.TimexAttributes> list = timexMap[docName];
					if (list == null)
					{
						timexMap[docName] = list = new List<SUTimeMain.TimexAttributes>();
					}
					list.Add(lastTimex);
				}
			}
			extBr.Close();
			BufferedReader attrBr = IOUtils.GetBufferedFileReader(attrsFile);
			while ((line = attrBr.ReadLine()) != null)
			{
				if (line.Trim().Length == 0)
				{
					continue;
				}
				// Simple tab delimited file
				string[] fields = line.Split("\t");
				string docName = fields[0];
				int sentNo = System.Convert.ToInt32(fields[1]);
				int tokenNo = System.Convert.ToInt32(fields[2]);
				string tid = fields[4];
				string attrname = fields[6];
				string attrvalue = fields[7];
				// Find entry
				SUTimeMain.TimexAttributes timex = FindTimex(timexMap, docName, tid);
				System.Diagnostics.Debug.Assert((timex.sentIndex == sentNo));
				System.Diagnostics.Debug.Assert((timex.tokenStart <= tokenNo && timex.tokenEnd > tokenNo));
				switch (attrname)
				{
					case "type":
					{
						System.Diagnostics.Debug.Assert((timex.type == null || timex.type.Equals(attrvalue)));
						timex.type = attrvalue;
						break;
					}

					case "value":
					{
						System.Diagnostics.Debug.Assert((timex.value == null || timex.value.Equals(attrvalue)));
						timex.value = attrvalue;
						break;
					}

					default:
					{
						throw new Exception("Error processing " + attrsFile + ":" + "Unknown attribute " + attrname + ": from line " + line);
					}
				}
			}
			attrBr.Close();
			return timexMap;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ProcessTempEval2Tab(AnnotationPipeline pipeline, string @in, string @out, IDictionary<string, string> docDates)
		{
			IDictionary<string, IList<SUTimeMain.TimexAttributes>> timexMap = ReadTimexAttrExts(@in + "/timex-extents.tab", @in + "/timex-attributes.tab");
			BufferedReader br = IOUtils.GetBufferedFileReader(@in + "/base-segmentation.tab");
			PrintWriter debugPw = IOUtils.GetPrintWriter(@out + "/timex-debug.out");
			PrintWriter attrPw = IOUtils.GetPrintWriter(@out + "/timex-attrs.res.tab");
			PrintWriter extPw = IOUtils.GetPrintWriter(@out + "/timex-extents.res.tab");
			PrintWriter attrDebugPwGold = IOUtils.GetPrintWriter(@out + "/timex-attrs.debug.gold.tab");
			PrintWriter attrDebugPw = IOUtils.GetPrintWriter(@out + "/timex-attrs.debug.res.tab");
			string line;
			string curDocName = null;
			int curSentNo = -1;
			IList<string> tokens = null;
			IList<ICoreMap> sentences = null;
			while ((line = br.ReadLine()) != null)
			{
				if (line.Trim().Length == 0)
				{
					continue;
				}
				// Simple tab delimited file
				string[] fields = line.Split("\t");
				string docName = fields[0];
				int sentNo = System.Convert.ToInt32(fields[1]);
				//int tokenNo = Integer.parseInt(fields[2]);
				string tokenText = fields[3];
				// Create little annotation with sentences and tokens
				if (!docName.Equals(curDocName))
				{
					if (curDocName != null)
					{
						// Process document
						ICoreMap lastSentence = WordsToSentence(tokens);
						sentences.Add(lastSentence);
						Annotation docAnnotation = SentencesToDocument(curDocName, docDates[curDocName], sentences);
						ProcessTempEval2Doc(pipeline, docAnnotation, timexMap, extPw, attrPw, debugPw, attrDebugPwGold, attrDebugPw);
						curDocName = null;
					}
					// New doc
					tokens = new List<string>();
					sentences = new List<ICoreMap>();
				}
				else
				{
					if (curSentNo != sentNo)
					{
						ICoreMap lastSentence = WordsToSentence(tokens);
						sentences.Add(lastSentence);
						tokens = new List<string>();
					}
				}
				tokens.Add(tokenText);
				curDocName = docName;
				curSentNo = sentNo;
			}
			if (curDocName != null)
			{
				// Process document
				ICoreMap lastSentence = WordsToSentence(tokens);
				sentences.Add(lastSentence);
				Annotation docAnnotation = SentencesToDocument(curDocName, docDates[curDocName], sentences);
				ProcessTempEval2Doc(pipeline, docAnnotation, timexMap, extPw, attrPw, debugPw, attrDebugPwGold, attrDebugPw);
				curDocName = null;
			}
			br.Close();
			extPw.Close();
			attrPw.Close();
			debugPw.Close();
			attrDebugPwGold.Close();
			attrDebugPw.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Text.ParseException"/>
		public static void ProcessTempEval2(AnnotationPipeline pipeline, string @in, string @out, string eval, string dct)
		{
			IDictionary<string, string> docDates = (dct != null) ? IOUtils.ReadMap(dct) : IOUtils.ReadMap(@in + "/dct.txt");
			if (requiredDocDateFormat != null)
			{
				// convert from yyyyMMdd to requiredDocDateFormat
				DateFormat defaultFormatter = new SimpleDateFormat("yyyyMMdd");
				DateFormat requiredFormatter = new SimpleDateFormat(requiredDocDateFormat);
				foreach (KeyValuePair<string, string> docDateEntry in docDates)
				{
					DateTime date = defaultFormatter.Parse(docDateEntry.Value);
					docDates[docDateEntry.Key] = requiredFormatter.Format(date);
				}
			}
			ProcessTempEval2Tab(pipeline, @in, @out, docDates);
			if (eval != null)
			{
				IList<string> command = new List<string>();
				if (Python != null)
				{
					command.Add(Python);
				}
				command.Add(eval);
				command.Add(@in + "/base-segmentation.tab");
				command.Add(@in + "/timex-extents.tab");
				command.Add(@out + "/timex-extents.res.tab");
				command.Add(@in + "/timex-attributes.tab");
				command.Add(@out + "/timex-attrs.res.tab");
				ProcessBuilder pb = new ProcessBuilder(command);
				FileOutputStream evalFileOutput = new FileOutputStream(@out + "/scores.txt");
				TextWriter output = new OutputStreamWriter(new TeeStream(System.Console.Out, evalFileOutput));
				SystemUtils.Run(pb, output, null);
				evalFileOutput.Close();
			}
		}

		/// <exception cref="System.Exception"/>
		public static void ProcessTempEval3(AnnotationPipeline pipeline, string @in, string @out, string evalCmd)
		{
			// Process files
			File inFile = new File(@in);
			if (inFile.IsDirectory())
			{
				// input is a directory - process files in directory
				Pattern teinputPattern = Pattern.Compile("\\.(TE3input|tml)$");
				IEnumerable<File> files = IOUtils.IterFilesRecursive(inFile, teinputPattern);
				File outDir = new File(@out);
				outDir.Mkdirs();
				foreach (File file in files)
				{
					string inputFilename = file.GetAbsolutePath();
					string outputFilename = inputFilename.Replace(@in, @out).Replace(".TE3input", string.Empty);
					if (!Sharpen.Runtime.EqualsIgnoreCase(outputFilename, inputFilename))
					{
						//System.out.println(inputFilename + " => " + outputFilename);
						ProcessTempEval3File(pipeline, inputFilename, outputFilename);
					}
					else
					{
						log.Info("ABORTING: Input file and output is the same - " + inputFilename);
						System.Environment.Exit(-1);
					}
				}
			}
			else
			{
				// input is a file - process file
				ProcessTempEval3File(pipeline, @in, @out);
			}
			// Evaluate
			if (evalCmd != null)
			{
			}
		}

		// TODO: apply eval command
		/// <exception cref="System.Exception"/>
		public static void ProcessTempEval3File(AnnotationPipeline pipeline, string @in, string @out)
		{
			// Process one tempeval file
			IDocument doc = XMLUtils.ReadDocumentFromFile(@in);
			INode timemlNode = XMLUtils.GetNode(doc, "TimeML");
			INode docIdNode = XMLUtils.GetNode(timemlNode, "DOCID");
			INode dctNode = XMLUtils.GetNode(timemlNode, "DCT");
			INode dctTimexNode = XMLUtils.GetNode(dctNode, "TIMEX3");
			INode titleNode = XMLUtils.GetNode(timemlNode, "TITLE");
			INode extraInfoNode = XMLUtils.GetNode(timemlNode, "EXTRA_INFO");
			INode textNode = XMLUtils.GetNode(timemlNode, "TEXT");
			string date = XMLUtils.GetAttributeValue(dctTimexNode, "value");
			string text = textNode.GetTextContent();
			Annotation annotation = TextToAnnotation(pipeline, text, date);
			IElement annotatedTextElem = AnnotationToTmlTextElement(annotation);
			IDocument annotatedDoc = XMLUtils.CreateDocument();
			INode newTimemlNode = annotatedDoc.ImportNode(timemlNode, false);
			if (docIdNode != null)
			{
				newTimemlNode.AppendChild(annotatedDoc.ImportNode(docIdNode, true));
			}
			newTimemlNode.AppendChild(annotatedDoc.ImportNode(dctNode, true));
			if (titleNode != null)
			{
				newTimemlNode.AppendChild(annotatedDoc.ImportNode(titleNode, true));
			}
			if (extraInfoNode != null)
			{
				newTimemlNode.AppendChild(annotatedDoc.ImportNode(extraInfoNode, true));
			}
			newTimemlNode.AppendChild(annotatedDoc.AdoptNode(annotatedTextElem));
			annotatedDoc.AppendChild(newTimemlNode);
			PrintWriter pw = (@out != null) ? IOUtils.GetPrintWriter(@out) : new PrintWriter(System.Console.Out);
			string @string = XMLUtils.DocumentToString(annotatedDoc);
			pw.Println(@string);
			pw.Flush();
			if (@out != null)
			{
				pw.Close();
			}
		}

		private static string requiredDocDateFormat;

		private static bool useGUTime = false;

		/// <exception cref="System.Exception"/>
		public static AnnotationPipeline GetPipeline(Properties props, bool tokenize)
		{
			//    useGUTime = Boolean.parseBoolean(props.getProperty("gutime", "false"));
			AnnotationPipeline pipeline = new AnnotationPipeline();
			if (tokenize)
			{
				pipeline.AddAnnotator(new TokenizerAnnotator(false, "en"));
				pipeline.AddAnnotator(new WordsToSentencesAnnotator(false));
			}
			pipeline.AddAnnotator(new POSTaggerAnnotator(false));
			//    pipeline.addAnnotator(new NumberAnnotator(false));
			//    pipeline.addAnnotator(new QuantifiableEntityNormalizingAnnotator(false, false));
			string timeAnnotator = props.GetProperty("timeAnnotator", "sutime");
			switch (timeAnnotator)
			{
				case "gutime":
				{
					useGUTime = true;
					pipeline.AddAnnotator(new GUTimeAnnotator("gutime", props));
					break;
				}

				case "heideltime":
				{
					requiredDocDateFormat = "yyyy-MM-dd";
					pipeline.AddAnnotator(new HeidelTimeAnnotator("heideltime", props));
					break;
				}

				case "sutime":
				{
					pipeline.AddAnnotator(new TimeAnnotator("sutime", props));
					break;
				}

				default:
				{
					throw new ArgumentException("Unknown timeAnnotator: " + timeAnnotator);
				}
			}
			return pipeline;
		}

		internal enum InputType
		{
			Textfile,
			Text,
			TimebankCsv,
			Tempeval2,
			Tempeval3
		}

		/// <exception cref="System.IO.IOException"/>
		private static void ConfigLogger(string @out)
		{
			File outDir = new File(@out);
			if (!outDir.Exists())
			{
				outDir.Mkdirs();
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("handlers=java.util.logging.ConsoleHandler, java.util.logging.FileHandler\n");
			sb.Append(".level=SEVERE\n");
			sb.Append("edu.stanford.nlp.level=INFO\n");
			sb.Append("java.util.logging.ConsoleHandler.level=SEVERE\n");
			sb.Append("java.util.logging.FileHandler.formatter=java.util.logging.SimpleFormatter\n");
			sb.Append("java.util.logging.FileHandler.level=INFO\n");
			sb.Append("java.util.logging.FileHandler.pattern=" + @out + "/err.log" + "\n");
			LogManager.GetLogManager().ReadConfiguration(new ReaderInputStream(new StringReader(sb.ToString())));
		}

		private static IList<INode> CreateTimexNodes(string str, int charBeginOffset, IList<ICoreMap> timexAnns)
		{
			IList<ValuedInterval<ICoreMap, int>> timexList = new List<ValuedInterval<ICoreMap, int>>(timexAnns.Count);
			foreach (ICoreMap timexAnn in timexAnns)
			{
				timexList.Add(new ValuedInterval<ICoreMap, int>(timexAnn, MatchedExpression.CoremapToCharOffsetsIntervalFunc.Apply(timexAnn)));
			}
			timexList.Sort(HasIntervalConstants.ContainsFirstEndpointsComparator);
			return CreateTimexNodesPresorted(str, charBeginOffset, timexList);
		}

		private static IList<INode> CreateTimexNodesPresorted(string str, int charBeginOffset, IList<ValuedInterval<ICoreMap, int>> timexList)
		{
			if (charBeginOffset == null)
			{
				charBeginOffset = 0;
			}
			IList<INode> nodes = new List<INode>();
			int previousEnd = 0;
			IList<IElement> timexElems = new List<IElement>();
			IList<ValuedInterval<ICoreMap, int>> processed = new List<ValuedInterval<ICoreMap, int>>();
			CollectionValuedMap<int, ValuedInterval<ICoreMap, int>> unprocessed = new CollectionValuedMap<int, ValuedInterval<ICoreMap, int>>(CollectionFactory.ArrayListFactory<ValuedInterval<ICoreMap, int>>());
			foreach (ValuedInterval<ICoreMap, int> v in timexList)
			{
				ICoreMap timexAnn = v.GetValue();
				int begin = timexAnn.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) - charBeginOffset;
				int end = timexAnn.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) - charBeginOffset;
				if (begin >= previousEnd)
				{
					// Add text
					nodes.Add(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(str, previousEnd, begin)));
					// Add timex
					Timex timex = timexAnn.Get(typeof(TimeAnnotations.TimexAnnotation));
					IElement timexElem = timex.ToXmlElement();
					nodes.Add(timexElem);
					previousEnd = end;
					// For handling nested timexes
					processed.Add(v);
					timexElems.Add(timexElem);
				}
				else
				{
					unprocessed.Add(processed.Count - 1, v);
				}
			}
			if (previousEnd < str.Length)
			{
				nodes.Add(XMLUtils.CreateTextNode(Sharpen.Runtime.Substring(str, previousEnd)));
			}
			foreach (int i in unprocessed.Keys)
			{
				ValuedInterval<ICoreMap, int> v_1 = processed[i];
				string elemStr = v_1.GetValue().Get(typeof(CoreAnnotations.TextAnnotation));
				int charStart = v_1.GetValue().Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				IList<INode> innerElems = CreateTimexNodesPresorted(elemStr, charStart, (IList<ValuedInterval<ICoreMap, int>>)unprocessed[i]);
				IElement timexElem = timexElems[i];
				XMLUtils.RemoveChildren(timexElem);
				foreach (INode n in innerElems)
				{
					timexElem.AppendChild(n);
				}
			}
			return nodes;
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ProcessTextFile(AnnotationPipeline pipeline, string @in, string @out, string date)
		{
			string text = IOUtils.SlurpFile(@in);
			PrintWriter pw = (@out != null) ? IOUtils.GetPrintWriter(@out) : new PrintWriter(System.Console.Out);
			string @string = TextToAnnotatedXml(pipeline, text, date);
			pw.Println(@string);
			pw.Flush();
			if (@out != null)
			{
				pw.Close();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void ProcessText(AnnotationPipeline pipeline, string text, string @out, string date)
		{
			PrintWriter pw = (@out != null) ? IOUtils.GetPrintWriter(@out) : new PrintWriter(System.Console.Out);
			string @string = TextToAnnotatedXml(pipeline, text, date);
			pw.Println(@string);
			pw.Flush();
			if (@out != null)
			{
				pw.Close();
			}
		}

		public static string TextToAnnotatedXml(AnnotationPipeline pipeline, string text, string date)
		{
			Annotation annotation = TextToAnnotation(pipeline, text, date);
			IDocument xmlDoc = AnnotationToXmlDocument(annotation);
			return XMLUtils.DocumentToString(xmlDoc);
		}

		public static IElement AnnotationToTmlTextElement(Annotation annotation)
		{
			IList<ICoreMap> timexAnnsAll = annotation.Get(typeof(TimeAnnotations.TimexAnnotations));
			IElement textElem = XMLUtils.CreateElement("TEXT");
			IList<INode> timexNodes = CreateTimexNodes(annotation.Get(typeof(CoreAnnotations.TextAnnotation)), annotation.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), timexAnnsAll);
			foreach (INode node in timexNodes)
			{
				textElem.AppendChild(node);
			}
			return textElem;
		}

		public static IDocument AnnotationToXmlDocument(Annotation annotation)
		{
			IElement dateElem = XMLUtils.CreateElement("DATE");
			dateElem.SetTextContent(annotation.Get(typeof(CoreAnnotations.DocDateAnnotation)));
			IElement textElem = AnnotationToTmlTextElement(annotation);
			IElement docElem = XMLUtils.CreateElement("DOC");
			docElem.AppendChild(dateElem);
			docElem.AppendChild(textElem);
			// Create document and import elements into this document....
			IDocument doc = XMLUtils.CreateDocument();
			doc.AppendChild(doc.ImportNode(docElem, true));
			return doc;
		}

		public static Annotation TextToAnnotation(AnnotationPipeline pipeline, string text, string date)
		{
			Annotation annotation = new Annotation(text);
			annotation.Set(typeof(CoreAnnotations.DocDateAnnotation), date);
			pipeline.Annotate(annotation);
			return annotation;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			// Process arguments
			Properties props = StringUtils.ArgsToProperties(args);
			string @in = props.GetProperty("i");
			string date = props.GetProperty("date");
			string dct = props.GetProperty("tempeval2.dct");
			string @out = props.GetProperty("o");
			string inputTypeStr = props.GetProperty("in.type", SUTimeMain.InputType.Text.ToString());
			string eval = props.GetProperty("eval");
			Python = props.GetProperty("python", Python);
			SUTimeMain.InputType inputType = SUTimeMain.InputType.ValueOf(inputTypeStr);
			AnnotationPipeline pipeline;
			switch (inputType)
			{
				case SUTimeMain.InputType.Text:
				{
					pipeline = GetPipeline(props, true);
					ProcessText(pipeline, @in, @out, date);
					break;
				}

				case SUTimeMain.InputType.Textfile:
				{
					pipeline = GetPipeline(props, true);
					ProcessTextFile(pipeline, @in, @out, date);
					break;
				}

				case SUTimeMain.InputType.TimebankCsv:
				{
					ConfigLogger(@out);
					pipeline = GetPipeline(props, true);
					ProcessTimebankCsv(pipeline, @in, @out, eval);
					break;
				}

				case SUTimeMain.InputType.Tempeval2:
				{
					ConfigLogger(@out);
					pipeline = GetPipeline(props, false);
					ProcessTempEval2(pipeline, @in, @out, eval, dct);
					break;
				}

				case SUTimeMain.InputType.Tempeval3:
				{
					pipeline = GetPipeline(props, true);
					ProcessTempEval3(pipeline, @in, @out, eval);
					break;
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns.Surface
{
	/// <summary>
	/// CanNOT handle overlapping labeled text (that is one token cannot belong to
	/// multiple labels)! Note that there has to be spaces around the tags <label>
	/// and </label> for the reader to work correctly!
	/// </summary>
	/// <author>Sonal Gupta (sonalg@stanford.edu)</author>
	public class AnnotatedTextReader
	{
		public static IDictionary<string, DataInstance> ParseColumnFile(BufferedReader reader, ICollection<string> categoriesAllowed, IDictionary<string, Type> setClassForTheseLabels, bool setGoldClass, string sentIDprefix)
		{
			CoNLLDocumentReaderAndWriter conllreader = new CoNLLDocumentReaderAndWriter();
			Properties props = new Properties();
			SeqClassifierFlags flags = new SeqClassifierFlags(props);
			flags.entitySubclassification = "noprefix";
			flags.retainEntitySubclassification = false;
			conllreader.Init(flags);
			IEnumerator<IList<CoreLabel>> dociter = conllreader.GetIterator(reader);
			int num = -1;
			IDictionary<string, DataInstance> sents = new Dictionary<string, DataInstance>();
			while (dociter.MoveNext())
			{
				IList<CoreLabel> doc = dociter.Current;
				IList<string> words = new List<string>();
				IList<CoreLabel> sentcore = new List<CoreLabel>();
				int tokenindex = 0;
				foreach (CoreLabel l in doc)
				{
					if (l.Word().Equals(CoNLLDocumentReaderAndWriter.Boundary) || l.Word().Equals("-DOCSTART-"))
					{
						if (words.Count > 0)
						{
							num++;
							string docid = sentIDprefix + "-" + num.ToString();
							DataInstance sentInst = DataInstance.GetNewSurfaceInstance(sentcore);
							sents[docid] = sentInst;
							words = new List<string>();
							sentcore = new List<CoreLabel>();
							tokenindex = 0;
						}
						continue;
					}
					tokenindex++;
					words.Add(l.Word());
					l.Set(typeof(CoreAnnotations.IndexAnnotation), tokenindex);
					l.Set(typeof(CoreAnnotations.ValueAnnotation), l.Word());
					string label = l.Get(typeof(CoreAnnotations.AnswerAnnotation));
					System.Diagnostics.Debug.Assert(label != null, "label cannot be null");
					l.Set(typeof(CoreAnnotations.TextAnnotation), l.Word());
					l.Set(typeof(CoreAnnotations.OriginalTextAnnotation), l.Word());
					if (setGoldClass)
					{
						l.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), label);
					}
					if (setClassForTheseLabels != null && setClassForTheseLabels.Contains(label))
					{
						l.Set(setClassForTheseLabels[label], label);
					}
					sentcore.Add(l);
				}
				if (words.Count > 0)
				{
					num++;
					string docid = sentIDprefix + "-" + num.ToString();
					DataInstance sentInst = DataInstance.GetNewSurfaceInstance(sentcore);
					sents[docid] = sentInst;
				}
			}
			return sents;
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<ICoreMap> ParseFile(BufferedReader reader, ICollection<string> categoriesAllowed, IDictionary<string, Type> setClassForTheseLabels, bool setGoldClass, string sentIDprefix)
		{
			Pattern startingLabelToken = Pattern.Compile("<(" + StringUtils.Join(categoriesAllowed, "|") + ")>");
			Pattern endLabelToken = Pattern.Compile("</(" + StringUtils.Join(categoriesAllowed, "|") + ")>");
			string backgroundSymbol = "O";
			IList<ICoreMap> sentences = new List<ICoreMap>();
			int lineNum = -1;
			string l = null;
			while ((l = reader.ReadLine()) != null)
			{
				lineNum++;
				string[] t = l.Split("\t", 2);
				string id = null;
				string text = null;
				if (t.Length == 2)
				{
					id = t[0];
					text = t[1];
				}
				else
				{
					if (t.Length == 1)
					{
						text = t[0];
						id = lineNum.ToString();
					}
				}
				id = sentIDprefix + id;
				DocumentPreprocessor dp = new DocumentPreprocessor(new StringReader(text));
				PTBTokenizer.PTBTokenizerFactory<CoreLabel> tokenizerFactory = PTBTokenizer.PTBTokenizerFactory.NewCoreLabelTokenizerFactory("ptb3Escaping=false,normalizeParentheses=false,escapeForwardSlashAsterisk=false");
				dp.SetTokenizerFactory(tokenizerFactory);
				string label = backgroundSymbol;
				int sentNum = -1;
				foreach (IList<IHasWord> sentence in dp)
				{
					sentNum++;
					string sentStr = string.Empty;
					IList<CoreLabel> sent = new List<CoreLabel>();
					foreach (IHasWord tokw in sentence)
					{
						string tok = tokw.Word();
						Matcher startingMatcher = startingLabelToken.Matcher(tok);
						Matcher endMatcher = endLabelToken.Matcher(tok);
						if (startingMatcher.Matches())
						{
							//System.out.println("matched starting");
							label = startingMatcher.Group(1);
						}
						else
						{
							if (endMatcher.Matches())
							{
								//System.out.println("matched end");
								label = backgroundSymbol;
							}
							else
							{
								CoreLabel c = new CoreLabel();
								IList<string> toks = new List<string>();
								toks.Add(tok);
								foreach (string toksplit in toks)
								{
									sentStr += " " + toksplit;
									c.SetWord(toksplit);
									c.SetLemma(toksplit);
									c.SetValue(toksplit);
									c.Set(typeof(CoreAnnotations.TextAnnotation), toksplit);
									c.Set(typeof(CoreAnnotations.OriginalTextAnnotation), tok);
									if (setGoldClass)
									{
										c.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), label);
									}
									if (setClassForTheseLabels != null && setClassForTheseLabels.Contains(label))
									{
										c.Set(setClassForTheseLabels[label], label);
									}
									sent.Add(c);
								}
							}
						}
					}
					ICoreMap sentcm = new ArrayCoreMap();
					sentcm.Set(typeof(CoreAnnotations.TextAnnotation), sentStr.Trim());
					sentcm.Set(typeof(CoreAnnotations.TokensAnnotation), sent);
					sentcm.Set(typeof(CoreAnnotations.DocIDAnnotation), id + "-" + sentNum);
					sentences.Add(sentcm);
				}
			}
			return sentences;
		}
	}
}

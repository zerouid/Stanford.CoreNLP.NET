using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>DocumentReader for MUC format.</summary>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public class MUCDocumentReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(MUCDocumentReaderAndWriter));

		private const long serialVersionUID = -8334720781758500037L;

		private SeqClassifierFlags flags;

		private IIteratorFromReaderFactory<IList<CoreLabel>> factory;

		public virtual void Init(SeqClassifierFlags flags)
		{
			this.flags = flags;
			factory = XMLBeginEndIterator.GetFactory("DOC", new MUCDocumentReaderAndWriter.MUCDocumentParser(), true, true);
		}

		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		internal class MUCDocumentParser : IFunction<string, IList<CoreLabel>>
		{
			private static readonly Pattern sgml = Pattern.Compile("<([^>\\s]*)[^>]*>");

			private static readonly Pattern beginEntity = Pattern.Compile("<(ENAMEX|TIMEX|NUMEX) TYPE=\"([a-z]+)\"[^>]*>", Pattern.CaseInsensitive);

			private static readonly Pattern endEntity = Pattern.Compile("</(ENAMEX|TIMEX|NUMEX)>");

			public virtual IList<CoreLabel> Apply(string doc)
			{
				if (doc == null)
				{
					return null;
				}
				string section = string.Empty;
				string entity = "O";
				string entityClass = string.Empty;
				int pNum = 0;
				int sNum = 0;
				int wNum = 0;
				PTBTokenizer<CoreLabel> ptb = PTBTokenizer.NewPTBTokenizer(new BufferedReader(new StringReader(doc)), false, true);
				IList<CoreLabel> words = ptb.Tokenize();
				IList<CoreLabel> result = new List<CoreLabel>();
				CoreLabel prev = null;
				string prevString = string.Empty;
				foreach (CoreLabel word in words)
				{
					Matcher matcher = sgml.Matcher(word.Word());
					if (matcher.Matches())
					{
						string tag = matcher.Group(1);
						if (Sharpen.Runtime.EqualsIgnoreCase(word.Word(), "<p>"))
						{
							pNum++;
							sNum = 0;
							wNum = 0;
							if (prev != null)
							{
								string s = prev.Get(typeof(CoreAnnotations.AfterAnnotation));
								s += word.OriginalText() + word.After();
								prev.Set(typeof(CoreAnnotations.AfterAnnotation), s);
							}
							prevString += word.Before() + word.OriginalText();
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(word.Word(), "<s>"))
							{
								sNum++;
								wNum = 0;
								if (prev != null)
								{
									string s = prev.Get(typeof(CoreAnnotations.AfterAnnotation));
									s += word.OriginalText() + word.After();
									prev.Set(typeof(CoreAnnotations.AfterAnnotation), s);
								}
								prevString += word.Before() + word.OriginalText();
							}
							else
							{
								matcher = beginEntity.Matcher(word.Word());
								if (matcher.Matches())
								{
									entityClass = matcher.Group(1);
									entity = matcher.Group(2);
									if (prev != null)
									{
										string s = prev.Get(typeof(CoreAnnotations.AfterAnnotation));
										s += word.After();
										prev.Set(typeof(CoreAnnotations.AfterAnnotation), s);
									}
									prevString += word.Before();
								}
								else
								{
									matcher = endEntity.Matcher(word.Word());
									if (matcher.Matches())
									{
										entityClass = string.Empty;
										entity = "O";
										if (prev != null)
										{
											string s = prev.Get(typeof(CoreAnnotations.AfterAnnotation));
											s += word.After();
											prev.Set(typeof(CoreAnnotations.AfterAnnotation), s);
										}
										prevString += word.Before();
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(word.Word(), "<doc>"))
										{
											prevString += word.Before() + word.OriginalText();
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(word.Word(), "</doc>"))
											{
												string s = prev.Get(typeof(CoreAnnotations.AfterAnnotation));
												s += word.OriginalText();
												prev.Set(typeof(CoreAnnotations.AfterAnnotation), s);
											}
											else
											{
												section = tag.ToUpper();
												if (prev != null)
												{
													string s = prev.Get(typeof(CoreAnnotations.AfterAnnotation));
													s += word.OriginalText() + word.After();
													prev.Set(typeof(CoreAnnotations.AfterAnnotation), s);
												}
												prevString += word.Before() + word.OriginalText();
											}
										}
									}
								}
							}
						}
					}
					else
					{
						CoreLabel wi = new CoreLabel();
						wi.SetWord(word.Word());
						wi.Set(typeof(CoreAnnotations.OriginalTextAnnotation), word.OriginalText());
						wi.Set(typeof(CoreAnnotations.BeforeAnnotation), prevString + word.Before());
						wi.Set(typeof(CoreAnnotations.AfterAnnotation), word.After());
						wi.Set(typeof(CoreAnnotations.WordPositionAnnotation), string.Empty + wNum);
						wi.Set(typeof(CoreAnnotations.SentencePositionAnnotation), string.Empty + sNum);
						wi.Set(typeof(CoreAnnotations.ParaPositionAnnotation), string.Empty + pNum);
						wi.Set(typeof(CoreAnnotations.SectionAnnotation), section);
						wi.Set(typeof(CoreAnnotations.AnswerAnnotation), entity);
						wi.Set(typeof(CoreAnnotations.EntityClassAnnotation), entityClass);
						wNum++;
						prevString = string.Empty;
						result.Add(wi);
						prev = wi;
					}
				}
				//log.info(doc);
				//log.info(edu.stanford.nlp.util.StringUtils.join(result, "\n"));
				//System.exit(0);
				return result;
			}
		}

		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter pw)
		{
			string prevAnswer = "O";
			string prevClass = string.Empty;
			string afterLast = string.Empty;
			foreach (CoreLabel word in doc)
			{
				if (!prevAnswer.Equals("O") && !prevAnswer.Equals(word.Get(typeof(CoreAnnotations.AnswerAnnotation))))
				{
					pw.Print("</" + prevClass + ">");
					prevClass = string.Empty;
				}
				pw.Print(word.Get(typeof(CoreAnnotations.BeforeAnnotation)));
				if (!word.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("O") && !word.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(prevAnswer))
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(word.Get(typeof(CoreAnnotations.AnswerAnnotation)), "PERSON") || Sharpen.Runtime.EqualsIgnoreCase(word.Get(typeof(CoreAnnotations.AnswerAnnotation)), "ORGANIZATION") || Sharpen.Runtime.EqualsIgnoreCase(word
						.Get(typeof(CoreAnnotations.AnswerAnnotation)), "LOCATION"))
					{
						prevClass = "ENAMEX";
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(word.Get(typeof(CoreAnnotations.AnswerAnnotation)), "DATE") || Sharpen.Runtime.EqualsIgnoreCase(word.Get(typeof(CoreAnnotations.AnswerAnnotation)), "TIME"))
						{
							prevClass = "TIMEX";
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(word.Get(typeof(CoreAnnotations.AnswerAnnotation)), "PERCENT") || Sharpen.Runtime.EqualsIgnoreCase(word.Get(typeof(CoreAnnotations.AnswerAnnotation)), "MONEY"))
							{
								prevClass = "NUMEX";
							}
							else
							{
								log.Info("unknown type: " + word.Get(typeof(CoreAnnotations.AnswerAnnotation)));
								System.Environment.Exit(0);
							}
						}
					}
					pw.Print("<" + prevClass + " TYPE=\"" + word.Get(typeof(CoreAnnotations.AnswerAnnotation)) + "\">");
				}
				pw.Print(word.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
				afterLast = word.Get(typeof(CoreAnnotations.AfterAnnotation));
				prevAnswer = word.Get(typeof(CoreAnnotations.AnswerAnnotation));
			}
			if (!prevAnswer.Equals("O"))
			{
				pw.Print("</" + prevClass + ">");
				prevClass = string.Empty;
			}
			pw.Println(afterLast);
		}
	}
}

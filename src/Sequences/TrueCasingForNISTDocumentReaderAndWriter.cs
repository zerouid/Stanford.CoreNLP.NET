using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>A DocumentReaderAndWriter for truecasing documents.</summary>
	/// <remarks>
	/// A DocumentReaderAndWriter for truecasing documents.
	/// Adapted from Jenny's TrueCasingDocumentReaderAndWriter.java.
	/// </remarks>
	/// <author>Pi-Chuan Chang</author>
	[System.Serializable]
	public class TrueCasingForNISTDocumentReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TrueCasingForNISTDocumentReaderAndWriter));

		public const string ThreeClassesProperty = "3class";

		public static readonly bool ThreeClasses = bool.ParseBoolean(Runtime.GetProperty(ThreeClassesProperty, "false"));

		private const long serialVersionUID = -3000389291781534479L;

		private IIteratorFromReaderFactory<IList<CoreLabel>> factory;

		private bool verboseForTrueCasing = false;

		private static readonly Pattern alphabet = Pattern.Compile("[A-Za-z]+");

		// Note: This DocumentReaderAndWriter needs to be in core because it is
		// used in the truecasing Annotator (loaded by reflection).
		/// <summary>for test only</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Reader reader = new BufferedReader(new FileReader(args[0]));
			TrueCasingForNISTDocumentReaderAndWriter raw = new TrueCasingForNISTDocumentReaderAndWriter();
			raw.Init(null);
			for (IEnumerator<IList<CoreLabel>> it = raw.GetIterator(reader); it.MoveNext(); )
			{
				IList<CoreLabel> l = it.Current;
				foreach (CoreLabel cl in l)
				{
					System.Console.Out.WriteLine(cl);
				}
				System.Console.Out.WriteLine("========================================");
			}
		}

		public virtual void Init(SeqClassifierFlags flags)
		{
			verboseForTrueCasing = flags.verboseForTrueCasing;
			factory = LineIterator.GetFactory(new TrueCasingForNISTDocumentReaderAndWriter.LineToTrueCasesParser());
		}

		public static ISet knownWords = null;

		// todo
		public static bool Known(string s)
		{
			return knownWords.Contains(s.ToLower());
		}

		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter @out)
		{
			IList<string> sentence = new List<string>();
			int wrong = 0;
			foreach (CoreLabel wi in doc)
			{
				StringBuilder sb = new StringBuilder();
				if (!wi.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals(wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation))))
				{
					wrong++;
				}
				if (!ThreeClasses && wi.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("UPPER"))
				{
					sb.Append(wi.Word().ToUpper());
				}
				else
				{
					if (wi.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("LOWER"))
					{
						sb.Append(wi.Word().ToLower());
					}
					else
					{
						if (wi.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("INIT_UPPER"))
						{
							sb.Append(Sharpen.Runtime.Substring(wi.Word(), 0, 1).ToUpper()).Append(Sharpen.Runtime.Substring(wi.Word(), 1));
						}
						else
						{
							if (wi.Get(typeof(CoreAnnotations.AnswerAnnotation)).Equals("O"))
							{
								// in this case, if it contains a-z at all, then append "MIX" at the end
								sb.Append(wi.Word());
								Matcher alphaMatcher = alphabet.Matcher(wi.Word());
								if (alphaMatcher.Matches())
								{
									sb.Append("/MIX");
								}
							}
						}
					}
				}
				if (verboseForTrueCasing)
				{
					sb.Append("/GOLD-").Append(wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation))).Append("/GUESS-").Append(wi.Get(typeof(CoreAnnotations.AnswerAnnotation)));
				}
				sentence.Add(sb.ToString());
			}
			@out.Print(StringUtils.Join(sentence, " "));
			System.Console.Error.Printf("> wrong = %d ; total = %d%n", wrong, doc.Count);
			@out.Println();
		}

		public class LineToTrueCasesParser : IFunction<string, IList<CoreLabel>>
		{
			private static readonly Pattern allLower = Pattern.Compile("[^A-Z]*?[a-z]+[^A-Z]*?");

			private static readonly Pattern allUpper = Pattern.Compile("[^a-z]*?[A-Z]+[^a-z]*?");

			private static readonly Pattern startUpper = Pattern.Compile("[A-Z].*");

			public virtual IList<CoreLabel> Apply(string line)
			{
				IList<CoreLabel> doc = new List<CoreLabel>();
				int pos = 0;
				//line = line.replaceAll(" +"," ");
				//log.info("pichuan: processing line = "+line);
				string[] toks = line.Split(" ");
				foreach (string word in toks)
				{
					CoreLabel wi = new CoreLabel();
					Matcher lowerMatcher = allLower.Matcher(word);
					if (lowerMatcher.Matches())
					{
						wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "LOWER");
						wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "LOWER");
					}
					else
					{
						Matcher upperMatcher = allUpper.Matcher(word);
						if (!ThreeClasses && upperMatcher.Matches())
						{
							wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "UPPER");
							wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "UPPER");
						}
						else
						{
							Matcher startUpperMatcher = startUpper.Matcher(word);
							bool isINIT_UPPER;
							// = false;
							if (word.Length > 1)
							{
								string w2 = Sharpen.Runtime.Substring(word, 1);
								string lcw2 = w2.ToLower();
								isINIT_UPPER = w2.Equals(lcw2);
							}
							else
							{
								isINIT_UPPER = false;
							}
							if (startUpperMatcher.Matches() && isINIT_UPPER)
							{
								wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "INIT_UPPER");
								wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "INIT_UPPER");
							}
							else
							{
								wi.Set(typeof(CoreAnnotations.AnswerAnnotation), "O");
								wi.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), "O");
							}
						}
					}
					wi.SetWord(word.ToLower());
					wi.Set(typeof(CoreAnnotations.PositionAnnotation), pos.ToString());
					doc.Add(wi);
					pos++;
				}
				return doc;
			}
		}
	}
}

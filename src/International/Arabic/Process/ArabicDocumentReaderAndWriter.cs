using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>
	/// Reads newline delimited UTF-8 Arabic sentences with or without
	/// gold segmentation markers.
	/// </summary>
	/// <remarks>
	/// Reads newline delimited UTF-8 Arabic sentences with or without
	/// gold segmentation markers. When segmentation markers are present,
	/// this class may be used for
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicDocumentReaderAndWriter : IDocumentReaderAndWriter<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Process.ArabicDocumentReaderAndWriter));

		private const long serialVersionUID = 3667837672769424178L;

		private readonly IIteratorFromReaderFactory<IList<CoreLabel>> factory;

		private readonly ITokenizerFactory<CoreLabel> tf;

		private static readonly char DefaultSegMarker = '-';

		private readonly char segMarker;

		private const string tagDelimiter = "|||";

		private const string rewriteDelimiter = ">>>";

		private readonly bool inputHasTags;

		private readonly bool inputHasDomainLabels;

		private readonly string inputDomain;

		private readonly bool shouldStripRewrites;

		public class RewrittenArabicAnnotation : ICoreAnnotation<string>
		{
			// The segmentation marker used in the ATBv3 training data.
			// TODO(spenceg): Make this configurable.
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <param name="hasSegMarkers">if true, input has segmentation markers</param>
		public ArabicDocumentReaderAndWriter(bool hasSegMarkers)
			: this(hasSegMarkers, null)
		{
		}

		/// <param name="hasSegMarkers">if true, input has segmentation markers</param>
		/// <param name="tokFactory">a TokenizerFactory for the input</param>
		public ArabicDocumentReaderAndWriter(bool hasSegMarkers, ITokenizerFactory<CoreLabel> tokFactory)
			: this(hasSegMarkers, false, tokFactory)
		{
		}

		/// <param name="hasSegMarkers">if true, input has segmentation markers</param>
		/// <param name="hasTags">if true, input has morphological analyses separated by tagDelimiter.</param>
		/// <param name="tokFactory">a TokenizerFactory for the input</param>
		public ArabicDocumentReaderAndWriter(bool hasSegMarkers, bool hasTags, ITokenizerFactory<CoreLabel> tokFactory)
			: this(hasSegMarkers, hasTags, false, "123", tokFactory)
		{
		}

		/// <param name="hasSegMarkers">if true, input has segmentation markers</param>
		/// <param name="hasTags">if true, input has morphological analyses separated by tagDelimiter.</param>
		/// <param name="hasDomainLabels">
		/// if true, input has a whitespace-terminated domain at the beginning
		/// of each line of text
		/// </param>
		/// <param name="tokFactory">a TokenizerFactory for the input</param>
		public ArabicDocumentReaderAndWriter(bool hasSegMarkers, bool hasTags, bool hasDomainLabels, string domain, ITokenizerFactory<CoreLabel> tokFactory)
			: this(hasSegMarkers, hasTags, hasDomainLabels, domain, false, tokFactory)
		{
		}

		/// <param name="hasSegMarkers">if true, input has segmentation markers</param>
		/// <param name="hasTags">if true, input has morphological analyses separated by tagDelimiter.</param>
		/// <param name="hasDomainLabels">
		/// if true, input has a whitespace-terminated domain at the beginning
		/// of each line of text
		/// </param>
		/// <param name="stripRewrites">
		/// if true, erase orthographical rewrites from the gold labels (for
		/// comparison purposes)
		/// </param>
		/// <param name="tokFactory">a TokenizerFactory for the input</param>
		public ArabicDocumentReaderAndWriter(bool hasSegMarkers, bool hasTags, bool hasDomainLabels, string domain, bool stripRewrites, ITokenizerFactory<CoreLabel> tokFactory)
		{
			tf = tokFactory;
			inputHasTags = hasTags;
			inputHasDomainLabels = hasDomainLabels;
			inputDomain = domain;
			shouldStripRewrites = stripRewrites;
			segMarker = hasSegMarkers ? DefaultSegMarker : null;
			factory = LineIterator.GetFactory(new _ISerializableFunction_131(this));
		}

		private sealed class _ISerializableFunction_131 : ISerializableFunction<string, IList<CoreLabel>>
		{
			private const long serialVersionUID = 5243251505653686497L;

			public _ISerializableFunction_131(ArabicDocumentReaderAndWriter _enclosing)
			{
				this._enclosing = _enclosing;
				this.serialVersionUID = serialVersionUID;
			}

			public IList<CoreLabel> Apply(string @in)
			{
				IList<CoreLabel> tokenList;
				string lineDomain = string.Empty;
				if (this._enclosing.inputHasDomainLabels)
				{
					string[] domainAndData = @in.Split("\\s+", 2);
					if (domainAndData.Length < 2)
					{
						ArabicDocumentReaderAndWriter.log.Info("Missing domain label or text: ");
						ArabicDocumentReaderAndWriter.log.Info(@in);
					}
					else
					{
						lineDomain = domainAndData[0];
						@in = domainAndData[1];
					}
				}
				else
				{
					lineDomain = this._enclosing.inputDomain;
				}
				if (this._enclosing.inputHasTags)
				{
					string[] toks = @in.Split("\\s+");
					IList<CoreLabel> input = new List<CoreLabel>(toks.Length);
					string tagDelim = Pattern.Quote(ArabicDocumentReaderAndWriter.tagDelimiter);
					string rewDelim = Pattern.Quote(ArabicDocumentReaderAndWriter.rewriteDelimiter);
					foreach (string wordTag in toks)
					{
						string[] wordTagPair = wordTag.Split(tagDelim);
						System.Diagnostics.Debug.Assert(wordTagPair.Length == 2);
						string[] rewritePair = wordTagPair[0].Split(rewDelim);
						System.Diagnostics.Debug.Assert(rewritePair.Length == 1 || rewritePair.Length == 2);
						string raw = rewritePair[0];
						string rewritten = raw;
						if (rewritePair.Length == 2)
						{
							rewritten = rewritePair[1];
						}
						CoreLabel cl = new CoreLabel();
						if (this._enclosing.tf != null)
						{
							IList<CoreLabel> lexListRaw = this._enclosing.tf.GetTokenizer(new StringReader(raw)).Tokenize();
							IList<CoreLabel> lexListRewritten = this._enclosing.tf.GetTokenizer(new StringReader(rewritten)).Tokenize();
							if (lexListRewritten.Count != lexListRaw.Count)
							{
								System.Console.Error.Printf("%s: Different number of tokens in raw and rewritten: %s>>>%s%n", this.GetType().FullName, raw, rewritten);
								lexListRewritten = lexListRaw;
							}
							if (lexListRaw.IsEmpty())
							{
								continue;
							}
							else
							{
								if (lexListRaw.Count == 1)
								{
									raw = lexListRaw[0].Value();
									rewritten = lexListRewritten[0].Value();
								}
								else
								{
									if (lexListRaw.Count > 1)
									{
										string secondWord = lexListRaw[1].Value();
										if (secondWord.Equals(this._enclosing.segMarker.ToString()))
										{
											// Special case for the null marker in the vocalized section
											raw = lexListRaw[0].Value() + this._enclosing.segMarker;
											rewritten = lexListRewritten[0].Value() + this._enclosing.segMarker;
										}
										else
										{
											System.Console.Error.Printf("%s: Raw token generates multiple segments: %s%n", this.GetType().FullName, raw);
											raw = lexListRaw[0].Value();
											rewritten = lexListRewritten[0].Value();
										}
									}
								}
							}
						}
						cl.SetValue(raw);
						cl.SetWord(raw);
						cl.SetTag(wordTagPair[1]);
						cl.Set(typeof(CoreAnnotations.DomainAnnotation), lineDomain);
						cl.Set(typeof(ArabicDocumentReaderAndWriter.RewrittenArabicAnnotation), rewritten);
						input.Add(cl);
					}
					tokenList = IOBUtils.StringToIOB(input, this._enclosing.segMarker, true, this._enclosing.shouldStripRewrites);
				}
				else
				{
					if (this._enclosing.tf == null)
					{
						tokenList = IOBUtils.StringToIOB(@in, this._enclosing.segMarker);
					}
					else
					{
						IList<CoreLabel> line = this._enclosing.tf.GetTokenizer(new StringReader(@in)).Tokenize();
						tokenList = IOBUtils.StringToIOB(line, this._enclosing.segMarker, false);
					}
				}
				if (this._enclosing.inputHasDomainLabels && !this._enclosing.inputHasTags)
				{
					IOBUtils.LabelDomain(tokenList, lineDomain);
				}
				else
				{
					if (!this._enclosing.inputHasDomainLabels)
					{
						IOBUtils.LabelDomain(tokenList, this._enclosing.inputDomain);
					}
				}
				return tokenList;
			}

			private readonly ArabicDocumentReaderAndWriter _enclosing;
		}

		/// <summary>Required, but unused.</summary>
		public virtual void Init(SeqClassifierFlags flags)
		{
		}

		/// <summary>Iterate over an input document.</summary>
		public virtual IEnumerator<IList<CoreLabel>> GetIterator(Reader r)
		{
			return factory.GetIterator(r);
		}

		public virtual void PrintAnswers(IList<CoreLabel> doc, PrintWriter pw)
		{
			pw.Println("Answer\tGoldAnswer\tCharacter");
			foreach (CoreLabel word in doc)
			{
				pw.Printf("%s\t%s\t%s%n", word.Get(typeof(CoreAnnotations.AnswerAnnotation)), word.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)), word.Get(typeof(CoreAnnotations.CharAnnotation)));
			}
		}

		/// <summary>For debugging.</summary>
		/// <param name="args"/>
		/// <exception cref="System.IO.IOException"></exception>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s file > output%n", typeof(ArabicDocumentReaderAndWriter).FullName);
				System.Environment.Exit(-1);
			}
			string fileName = args[0];
			ITokenizerFactory<CoreLabel> tokFactory = ArabicTokenizer.AtbFactory();
			string atbVocOptions = "removeProMarker,removeMorphMarker";
			tokFactory.SetOptions(atbVocOptions);
			BufferedReader reader = IOUtils.ReaderFromString(fileName);
			for (string line; (line = reader.ReadLine()) != null; )
			{
				string[] toks = line.Split("\\s+");
				string delim = Pattern.Quote(tagDelimiter);
				bool isStart = true;
				foreach (string wordTag in toks)
				{
					string[] wordTagPair = wordTag.Split(delim);
					System.Diagnostics.Debug.Assert(wordTagPair.Length == 2);
					string word = wordTagPair[0];
					if (tokFactory != null)
					{
						IList<CoreLabel> lexList = tokFactory.GetTokenizer(new StringReader(word)).Tokenize();
						if (lexList.Count == 0)
						{
							continue;
						}
						else
						{
							if (lexList.Count == 1)
							{
								word = lexList[0].Value();
							}
							else
							{
								if (lexList.Count > 1)
								{
									string secondWord = lexList[1].Value();
									if (secondWord.Equals(DefaultSegMarker.ToString()))
									{
										// Special case for the null marker in the vocalized section
										word = lexList[0].Value() + DefaultSegMarker.ToString();
									}
									else
									{
										System.Console.Error.Printf("%s: Raw token generates multiple segments: %s%n", typeof(ArabicDocumentReaderAndWriter).FullName, word);
										word = lexList[0].Value();
									}
								}
							}
						}
					}
					if (!isStart)
					{
						System.Console.Out.Write(" ");
					}
					System.Console.Out.Write(word);
					isStart = false;
				}
				System.Console.Out.WriteLine();
			}
		}
		//    DocumentReaderAndWriter<CoreLabel> docReader = new ArabicDocumentReaderAndWriter(true,
		//        true,
		//        false,
		//        tokFactory);
		//    Iterator<List<CoreLabel>> itr = docReader.getIterator(new InputStreamReader(new FileInputStream(new File(fileName))));
		//    while(itr.hasNext()) {
		//      List<CoreLabel> line = itr.next();
		//      System.out.println(Sentence.listToString(line));
		//    }
	}
}

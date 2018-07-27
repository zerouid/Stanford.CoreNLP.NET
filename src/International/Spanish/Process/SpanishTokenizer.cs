using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.International.Spanish;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.International.Spanish.Process
{
	/// <summary>Tokenizer for raw Spanish text.</summary>
	/// <remarks>
	/// Tokenizer for raw Spanish text. This tokenization scheme is a derivative
	/// of PTB tokenization, but with extra rules for Spanish contractions and
	/// assimilations. It is based heavily on the FrenchTokenizer.
	/// <p>
	/// The tokenizer tokenizes according to the modified AnCora corpus tokenization
	/// standards, so the rules are a little different from PTB.
	/// </p>
	/// <p>
	/// A single instance of a Spanish Tokenizer is not thread safe, as it
	/// uses a non-threadsafe JFlex object to do the processing.  Multiple
	/// instances can be created safely, though.  A single instance of a
	/// SpanishTokenizerFactory is also not thread safe, as it keeps its
	/// options in a local variable.
	/// </p>
	/// </remarks>
	/// <author>Ishita Prasad</author>
	public class SpanishTokenizer<T> : AbstractTokenizer<T>
		where T : IHasWord
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Spanish.Process.SpanishTokenizer));

		private readonly SpanishLexer lexer;

		private readonly bool splitCompounds;

		private readonly bool splitVerbs;

		private readonly bool splitContractions;

		private readonly bool splitAny;

		private IList<CoreLabel> compoundBuffer;

		private SpanishVerbStripper verbStripper;

		public const string AncoraOptions = "ptb3Ellipsis=true,normalizeParentheses=true,ptb3Dashes=false,splitAll=true";

		/// <summary>Constructor.</summary>
		/// <param name="r"/>
		/// <param name="tf"/>
		/// <param name="lexerProperties"/>
		/// <param name="splitCompounds"/>
		public SpanishTokenizer(Reader r, ILexedTokenFactory<T> tf, Properties lexerProperties, bool splitCompounds, bool splitVerbs, bool splitContractions)
		{
			// The underlying JFlex lexer
			// Internal fields compound splitting
			// Produces the tokenization for parsing used by AnCora (fixed) */
			lexer = new SpanishLexer(r, tf, lexerProperties);
			this.splitCompounds = splitCompounds;
			this.splitVerbs = splitVerbs;
			this.splitContractions = splitContractions;
			this.splitAny = (splitCompounds || splitVerbs || splitContractions);
			if (splitAny)
			{
				compoundBuffer = Generics.NewArrayList(4);
			}
			if (splitVerbs)
			{
				verbStripper = SpanishVerbStripper.GetInstance();
			}
		}

		protected internal override T GetNext()
		{
			try
			{
				T nextToken;
				do
				{
					// initialized in do-while
					// Depending on the orthographic normalization options,
					// some tokens can be obliterated. In this case, keep iterating
					// until we see a non-zero length token.
					nextToken = (splitAny && !compoundBuffer.IsEmpty()) ? (T)compoundBuffer.Remove(0) : (T)lexer.Next();
				}
				while (nextToken != null && nextToken.Word().IsEmpty());
				// Check for compounds to split
				if (splitAny && nextToken is CoreLabel)
				{
					CoreLabel cl = (CoreLabel)nextToken;
					if (cl.ContainsKey(typeof(CoreAnnotations.ParentAnnotation)))
					{
						if (splitCompounds && cl.Get(typeof(CoreAnnotations.ParentAnnotation)).Equals(SpanishLexer.CompoundAnnotation))
						{
							nextToken = (T)ProcessCompound(cl);
						}
						else
						{
							if (splitVerbs && cl.Get(typeof(CoreAnnotations.ParentAnnotation)).Equals(SpanishLexer.VbPronAnnotation))
							{
								nextToken = (T)ProcessVerb(cl);
							}
							else
							{
								if (splitContractions && cl.Get(typeof(CoreAnnotations.ParentAnnotation)).Equals(SpanishLexer.ContrAnnotation))
								{
									nextToken = (T)ProcessContraction(cl);
								}
							}
						}
					}
				}
				return nextToken;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Copies the CoreLabel cl with the new word part</summary>
		private static CoreLabel CopyCoreLabel(CoreLabel cl, string part, int beginPosition, int endPosition)
		{
			CoreLabel newLabel = new CoreLabel(cl);
			newLabel.SetWord(part);
			newLabel.SetValue(part);
			newLabel.SetBeginPosition(beginPosition);
			newLabel.SetEndPosition(endPosition);
			newLabel.Set(typeof(CoreAnnotations.OriginalTextAnnotation), part);
			return newLabel;
		}

		private static CoreLabel CopyCoreLabel(CoreLabel cl, string part, int beginPosition)
		{
			return CopyCoreLabel(cl, part, beginPosition, beginPosition + part.Length);
		}

		/// <summary>
		/// Handles contractions like del and al, marked by the lexer
		/// del =&gt; de + l =&gt; de + el
		/// al =&gt; a + l =&gt; a + el
		/// con[mts]igo =&gt; con + [mts]i
		/// </summary>
		private CoreLabel ProcessContraction(CoreLabel cl)
		{
			cl.Remove(typeof(CoreAnnotations.ParentAnnotation));
			string word = cl.Word();
			string first;
			string second;
			int secondOffset = 0;
			int secondLength = 0;
			string lowered = word.ToLower();
			switch (lowered)
			{
				case "del":
				case "al":
				{
					first = Sharpen.Runtime.Substring(word, 0, lowered.Length - 1);
					char lastChar = word[lowered.Length - 1];
					if (char.IsLowerCase(lastChar))
					{
						second = "el";
					}
					else
					{
						second = "EL";
					}
					secondOffset = 1;
					secondLength = lowered.Length - 1;
					break;
				}

				case "conmigo":
				case "consigo":
				{
					first = Sharpen.Runtime.Substring(word, 0, 3);
					second = word[3] + "í";
					secondOffset = 3;
					secondLength = 4;
					break;
				}

				case "contigo":
				{
					first = Sharpen.Runtime.Substring(word, 0, 3);
					second = Sharpen.Runtime.Substring(word, 3, 5);
					secondOffset = 3;
					secondLength = 4;
					break;
				}

				default:
				{
					throw new ArgumentException("Invalid contraction provided to processContraction");
				}
			}
			int secondStart = cl.BeginPosition() + secondOffset;
			int secondEnd = secondStart + secondLength;
			compoundBuffer.Add(CopyCoreLabel(cl, second, secondStart, secondEnd));
			return CopyCoreLabel(cl, first, cl.BeginPosition(), secondStart);
		}

		/// <summary>
		/// Handles verbs with attached suffixes, marked by the lexer:
		/// Escribamosela =&gt; Escribamo + se + la =&gt; escribamos + se + la
		/// Sentaos =&gt; senta + os =&gt; sentad + os
		/// Damelo =&gt; da + me + lo
		/// </summary>
		private CoreLabel ProcessVerb(CoreLabel cl)
		{
			cl.Remove(typeof(CoreAnnotations.ParentAnnotation));
			SpanishVerbStripper.StrippedVerb stripped = verbStripper.SeparatePronouns(cl.Word());
			if (stripped == null)
			{
				return cl;
			}
			// Split the CoreLabel into separate labels, tracking changing begin + end
			// positions.
			int stemEnd = cl.BeginPosition() + stripped.GetOriginalStem().Length;
			int lengthRemoved = 0;
			foreach (string pronoun in stripped.GetPronouns())
			{
				int beginOffset = stemEnd + lengthRemoved;
				compoundBuffer.Add(CopyCoreLabel(cl, pronoun, beginOffset));
				lengthRemoved += pronoun.Length;
			}
			CoreLabel stem = CopyCoreLabel(cl, stripped.GetStem(), cl.BeginPosition(), stemEnd);
			stem.SetOriginalText(stripped.GetOriginalStem());
			return stem;
		}

		private static readonly Pattern pDash = Pattern.Compile("\\-");

		private static readonly Pattern pSpace = Pattern.Compile("\\s+");

		/// <summary>Splits a compound marked by the lexer.</summary>
		private CoreLabel ProcessCompound(CoreLabel cl)
		{
			cl.Remove(typeof(CoreAnnotations.ParentAnnotation));
			string[] parts = pSpace.Split(pDash.Matcher(cl.Word()).ReplaceAll(" - "));
			int lengthAccum = 0;
			foreach (string part in parts)
			{
				CoreLabel newLabel = new CoreLabel(cl);
				newLabel.SetWord(part);
				newLabel.SetValue(part);
				newLabel.SetBeginPosition(cl.BeginPosition() + lengthAccum);
				newLabel.SetEndPosition(cl.BeginPosition() + lengthAccum + part.Length);
				newLabel.Set(typeof(CoreAnnotations.OriginalTextAnnotation), part);
				compoundBuffer.Add(newLabel);
				lengthAccum += part.Length;
			}
			return compoundBuffer.Remove(0);
		}

		/// <summary>recommended factory method</summary>
		public static ITokenizerFactory<T> Factory<T>(ILexedTokenFactory<T> factory, string options)
			where T : IHasWord
		{
			return new SpanishTokenizer.SpanishTokenizerFactory<T>(factory, options);
		}

		public static ITokenizerFactory<T> Factory<T>(ILexedTokenFactory<T> factory)
			where T : IHasWord
		{
			return new SpanishTokenizer.SpanishTokenizerFactory<T>(factory, AncoraOptions);
		}

		/// <summary>A factory for Spanish tokenizer instances.</summary>
		/// <author>Spence Green</author>
		/// <?/>
		[System.Serializable]
		public class SpanishTokenizerFactory<T> : ITokenizerFactory<T>
			where T : IHasWord
		{
			private const long serialVersionUID = 946818805507187330L;

			protected internal readonly ILexedTokenFactory<T> factory;

			protected internal Properties lexerProperties = new Properties();

			protected internal bool splitCompoundOption = false;

			protected internal bool splitVerbOption = false;

			protected internal bool splitContractionOption = false;

			public static ITokenizerFactory<CoreLabel> NewCoreLabelTokenizerFactory()
			{
				return new SpanishTokenizer.SpanishTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory());
			}

			/// <summary>Constructs a new SpanishTokenizer that returns T objects and uses the options passed in.</summary>
			/// <param name="options">a String of options, separated by commas</param>
			/// <returns>A TokenizerFactory that returns the right token types</returns>
			/// <param name="factory">a factory for the token type that the tokenizer will return</param>
			public static SpanishTokenizer.SpanishTokenizerFactory<T> NewSpanishTokenizerFactory<T>(ILexedTokenFactory<T> factory, string options)
				where T : IHasWord
			{
				return new SpanishTokenizer.SpanishTokenizerFactory<T>(factory, options);
			}

			/// <summary>Make a factory for SpanishTokenizers, default options</summary>
			private SpanishTokenizerFactory(ILexedTokenFactory<T> factory)
			{
				// Constructors
				this.factory = factory;
			}

			/// <summary>Make a factory for SpanishTokenizers, options passed in</summary>
			private SpanishTokenizerFactory(ILexedTokenFactory<T> factory, string options)
			{
				this.factory = factory;
				SetOptions(options);
			}

			public virtual IEnumerator<T> GetIterator(Reader r)
			{
				return GetTokenizer(r);
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r)
			{
				return new SpanishTokenizer<T>(r, factory, lexerProperties, splitCompoundOption, splitVerbOption, splitContractionOption);
			}

			/// <summary>Set underlying tokenizer options.</summary>
			/// <param name="options">A comma-separated list of options</param>
			public virtual void SetOptions(string options)
			{
				if (options == null)
				{
					return;
				}
				string[] optionList = options.Split(",");
				foreach (string option in optionList)
				{
					string[] fields = option.Split("=");
					if (fields.Length == 1)
					{
						switch (fields[0])
						{
							case "splitAll":
							{
								splitCompoundOption = true;
								splitVerbOption = true;
								splitContractionOption = true;
								break;
							}

							case "splitCompounds":
							{
								splitCompoundOption = true;
								break;
							}

							case "splitVerbs":
							{
								splitVerbOption = true;
								break;
							}

							case "splitContractions":
							{
								splitContractionOption = true;
								break;
							}

							default:
							{
								lexerProperties.SetProperty(option, "true");
								break;
							}
						}
					}
					else
					{
						if (fields.Length == 2)
						{
							switch (fields[0])
							{
								case "splitAll":
								{
									splitCompoundOption = bool.ValueOf(fields[1]);
									splitVerbOption = bool.ValueOf(fields[1]);
									splitContractionOption = bool.ValueOf(fields[1]);
									break;
								}

								case "splitCompounds":
								{
									splitCompoundOption = bool.ValueOf(fields[1]);
									break;
								}

								case "splitVerbs":
								{
									splitVerbOption = bool.ValueOf(fields[1]);
									break;
								}

								case "splitContractions":
								{
									splitContractionOption = bool.ValueOf(fields[1]);
									break;
								}

								default:
								{
									lexerProperties.SetProperty(fields[0], fields[1]);
									break;
								}
							}
						}
						else
						{
							System.Console.Error.Printf("%s: Bad option %s%n", this.GetType().FullName, option);
						}
					}
				}
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r, string extraOptions)
			{
				SetOptions(extraOptions);
				return GetTokenizer(r);
			}
		}

		// end static class SpanishTokenizerFactory
		/// <summary>Returns a tokenizer with Ancora tokenization.</summary>
		public static ITokenizerFactory<CoreLabel> AncoraFactory()
		{
			ITokenizerFactory<CoreLabel> tf = SpanishTokenizer.SpanishTokenizerFactory.NewCoreLabelTokenizerFactory();
			tf.SetOptions(AncoraOptions);
			return tf;
		}

		/// <summary>a factory that vends CoreLabel tokens with default tokenization.</summary>
		public static ITokenizerFactory<CoreLabel> CoreLabelFactory()
		{
			return SpanishTokenizer.SpanishTokenizerFactory.NewCoreLabelTokenizerFactory();
		}

		public static ITokenizerFactory<CoreLabel> Factory()
		{
			return CoreLabelFactory();
		}

		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.LineSeparator();
			sb.Append(string.Format("Usage: java %s [OPTIONS] < file%n%n", typeof(SpanishTokenizer).FullName));
			sb.Append("Options:").Append(nl);
			sb.Append("   -help          : Print this message.").Append(nl);
			sb.Append("   -ancora        : Tokenization style of AnCora (fixed).").Append(nl);
			sb.Append("   -lowerCase     : Apply lowercasing.").Append(nl);
			sb.Append("   -encoding type : Encoding format.").Append(nl);
			sb.Append("   -options str   : Orthographic options (see SpanishLexer.java)").Append(nl);
			sb.Append("   -tokens        : Output tokens as line-separated instead of space-separated.").Append(nl);
			sb.Append("   -onePerLine    : Output tokens one per line.").Append(nl);
			return sb.ToString();
		}

		private static IDictionary<string, int> ArgOptionDefs()
		{
			IDictionary<string, int> argOptionDefs = Generics.NewHashMap();
			argOptionDefs["help"] = 0;
			argOptionDefs["ftb"] = 0;
			argOptionDefs["ancora"] = 0;
			argOptionDefs["lowerCase"] = 0;
			argOptionDefs["encoding"] = 1;
			argOptionDefs["options"] = 1;
			argOptionDefs["tokens"] = 0;
			return argOptionDefs;
		}

		/// <summary>A fast, rule-based tokenizer for Spanish based on AnCora.</summary>
		/// <remarks>
		/// A fast, rule-based tokenizer for Spanish based on AnCora.
		/// Performs punctuation splitting and light tokenization by default.
		/// <p>
		/// Currently, this tokenizer does not do line splitting. It assumes that the input
		/// file is delimited by the system line separator. The output will be equivalently
		/// delimited.
		/// </p>
		/// </remarks>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args, ArgOptionDefs());
			if (options.Contains("help"))
			{
				log.Info(Usage());
				return;
			}
			// Lexer options
			ITokenizerFactory<CoreLabel> tf = SpanishTokenizer.CoreLabelFactory();
			string orthoOptions = options.Contains("ancora") ? AncoraOptions : string.Empty;
			if (options.Contains("options"))
			{
				orthoOptions = orthoOptions.IsEmpty() ? options.GetProperty("options") : orthoOptions + ',' + options;
			}
			bool tokens = PropertiesUtils.GetBool(options, "tokens", false);
			if (!tokens)
			{
				orthoOptions = orthoOptions.IsEmpty() ? "tokenizeNLs" : orthoOptions + ",tokenizeNLs";
			}
			tf.SetOptions(orthoOptions);
			// Other options
			string encoding = options.GetProperty("encoding", "UTF-8");
			bool toLower = PropertiesUtils.GetBool(options, "lowerCase", false);
			Locale es = new Locale("es");
			bool onePerLine = PropertiesUtils.GetBool(options, "onePerLine", false);
			// Read the file from stdin
			int nLines = 0;
			int nTokens = 0;
			long startTime = Runtime.NanoTime();
			try
			{
				ITokenizer<CoreLabel> tokenizer = tf.GetTokenizer(new BufferedReader(new InputStreamReader(Runtime.@in, encoding)));
				BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(System.Console.Out, encoding));
				bool printSpace = false;
				while (tokenizer.MoveNext())
				{
					++nTokens;
					string word = tokenizer.Current.Word();
					if (word.Equals(SpanishLexer.NewlineToken))
					{
						++nLines;
						if (!onePerLine)
						{
							writer.NewLine();
							printSpace = false;
						}
					}
					else
					{
						string outputToken = toLower ? word.ToLower(es) : word;
						if (onePerLine)
						{
							writer.Write(outputToken);
							writer.NewLine();
						}
						else
						{
							if (printSpace)
							{
								writer.Write(" ");
							}
							writer.Write(outputToken);
							printSpace = true;
						}
					}
				}
			}
			catch (UnsupportedEncodingException e)
			{
				throw new RuntimeIOException("Bad character encoding", e);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			long elapsedTime = Runtime.NanoTime() - startTime;
			double linesPerSec = (double)nLines / (elapsedTime / 1e9);
			System.Console.Error.Printf("Done! Tokenized %d lines (%d tokens) at %.2f lines/sec%n", nLines, nTokens, linesPerSec);
		}
		// end main()
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.French.Process
{
	/// <summary>Tokenizer for raw French text.</summary>
	/// <remarks>
	/// Tokenizer for raw French text. This tokenization scheme is a derivative
	/// of PTB tokenization, but with extra rules for French elision and compounding.
	/// <p>
	/// The tokenizer implicitly inserts segmentation markers by not normalizing
	/// the apostrophe and hyphen. Detokenization can thus be performed by right-concatenating
	/// apostrophes and left-concatenating hyphens.
	/// <p>
	/// A single instance of an French Tokenizer is not thread safe, as it
	/// uses a non-threadsafe JFlex object to do the processing.  Multiple
	/// instances can be created safely, though.  A single instance of a
	/// FrenchTokenizerFactory is also not thread safe, as it keeps its
	/// options in a local variable.
	/// </remarks>
	/// <author>Spence Green</author>
	public class FrenchTokenizer<T> : AbstractTokenizer<T>
		where T : IHasWord
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.French.Process.FrenchTokenizer));

		private readonly FrenchLexer lexer;

		private readonly bool splitCompounds;

		private readonly bool splitContractions;

		private IList<CoreLabel> compoundBuffer;

		public const string FtbOptions = "ptb3Ellipsis=true,normalizeParentheses=true,ptb3Dashes=false," + "splitContractions=true,splitCompounds=true";

		/// <summary>Constructor.</summary>
		/// <param name="r"/>
		/// <param name="tf"/>
		/// <param name="lexerProperties"/>
		/// <param name="splitCompounds"/>
		public FrenchTokenizer(Reader r, ILexedTokenFactory<T> tf, Properties lexerProperties, bool splitCompounds, bool splitContractions)
		{
			// The underlying JFlex lexer
			// Internal fields compound splitting
			// Produces the tokenization for parsing used by Green, de Marneffe, and Manning (2011)
			lexer = new FrenchLexer(r, tf, lexerProperties);
			this.splitCompounds = splitCompounds;
			this.splitContractions = splitContractions;
			if (splitCompounds || splitContractions)
			{
				compoundBuffer = Generics.NewLinkedList();
			}
		}

		protected internal override T GetNext()
		{
			try
			{
				T nextToken = null;
				do
				{
					// Depending on the orthographic normalization options,
					// some tokens can be obliterated. In this case, keep iterating
					// until we see a non-zero length token.
					nextToken = ((splitContractions || splitCompounds) && compoundBuffer.Count > 0) ? (T)compoundBuffer.Remove(0) : (T)lexer.Next();
				}
				while (nextToken != null && nextToken.Word().Length == 0);
				// Check for compounds to split
				if (splitCompounds && nextToken is CoreLabel)
				{
					CoreLabel cl = (CoreLabel)nextToken;
					if (cl.ContainsKey(typeof(CoreAnnotations.ParentAnnotation)) && cl.Get(typeof(CoreAnnotations.ParentAnnotation)).Equals(FrenchLexer.CompoundAnnotation))
					{
						nextToken = (T)ProcessCompound(cl);
					}
				}
				// Check for contractions to split
				if (splitContractions && nextToken is CoreLabel)
				{
					CoreLabel cl = (CoreLabel)nextToken;
					if (cl.ContainsKey(typeof(CoreAnnotations.ParentAnnotation)) && cl.Get(typeof(CoreAnnotations.ParentAnnotation)).Equals(FrenchLexer.ContrAnnotation))
					{
						nextToken = (T)ProcessContraction(cl);
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

		/// <summary>Splits a compound marked by the lexer.</summary>
		private CoreLabel ProcessCompound(CoreLabel cl)
		{
			cl.Remove(typeof(CoreAnnotations.ParentAnnotation));
			string[] parts = cl.Word().ReplaceAll("-", " - ").Split("\\s+");
			foreach (string part in parts)
			{
				CoreLabel newLabel = new CoreLabel(cl);
				newLabel.SetWord(part);
				newLabel.SetValue(part);
				newLabel.Set(typeof(CoreAnnotations.OriginalTextAnnotation), part);
				compoundBuffer.Add(newLabel);
			}
			return compoundBuffer.Remove(0);
		}

		/// <summary>Splits a contraction marked by the lexer.</summary>
		/// <remarks>
		/// Splits a contraction marked by the lexer.
		/// au =&gt; a + u =&gt; à + le
		/// aux =&gt; a + ux =&gt; à + les
		/// des =&gt; de + s =&gt; de + les
		/// du =&gt; d + u =&gt; de + le
		/// </remarks>
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
				case "au":
				{
					first = "à";
					second = "le";
					secondOffset = 1;
					secondLength = 1;
					break;
				}

				case "aux":
				{
					first = "à";
					second = "les";
					secondOffset = 1;
					secondLength = 2;
					break;
				}

				case "du":
				{
					first = "de";
					second = "le";
					secondOffset = 1;
					secondLength = 1;
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

		/// <summary>A factory for French tokenizer instances.</summary>
		/// <author>Spence Green</author>
		/// <?/>
		[System.Serializable]
		public class FrenchTokenizerFactory<T> : ITokenizerFactory<T>
			where T : IHasWord
		{
			private const long serialVersionUID = 946818805507187330L;

			protected internal readonly ILexedTokenFactory<T> factory;

			protected internal Properties lexerProperties = new Properties();

			protected internal bool splitCompoundOption = false;

			protected internal bool splitContractionOption = true;

			public static ITokenizerFactory<CoreLabel> NewTokenizerFactory()
			{
				return new FrenchTokenizer.FrenchTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory());
			}

			/// <summary>todo [cdm 2013]: But we should change it to a method that can return any kind of Label and return CoreLabel here</summary>
			/// <param name="options">A String of options</param>
			/// <returns>A TokenizerFactory that returns Word objects</returns>
			public static ITokenizerFactory<Word> NewWordTokenizerFactory(string options)
			{
				return new FrenchTokenizer.FrenchTokenizerFactory<Word>(new WordTokenFactory(), options);
			}

			private FrenchTokenizerFactory(ILexedTokenFactory<T> factory)
			{
				this.factory = factory;
			}

			private FrenchTokenizerFactory(ILexedTokenFactory<T> factory, string options)
				: this(factory)
			{
				SetOptions(options);
			}

			public virtual IEnumerator<T> GetIterator(Reader r)
			{
				return GetTokenizer(r);
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r)
			{
				return new FrenchTokenizer<T>(r, factory, lexerProperties, splitCompoundOption, splitContractionOption);
			}

			/// <summary>Set underlying tokenizer options.</summary>
			/// <param name="options">A comma-separated list of options</param>
			public virtual void SetOptions(string options)
			{
				string[] optionList = options.Split(",");
				foreach (string option in optionList)
				{
					string[] fields = option.Split("=");
					if (fields.Length == 1)
					{
						if (fields[0].Equals("splitCompounds"))
						{
							splitCompoundOption = true;
						}
						else
						{
							lexerProperties.SetProperty(option, "true");
						}
					}
					else
					{
						if (fields.Length == 2)
						{
							if (fields[0].Equals("splitCompounds"))
							{
								splitCompoundOption = bool.ValueOf(fields[1]);
							}
							else
							{
								lexerProperties.SetProperty(fields[0], fields[1]);
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

		// end static class FrenchTokenizerFactory
		/// <summary>Returns a factory for FrenchTokenizer.</summary>
		/// <remarks>Returns a factory for FrenchTokenizer. THIS IS NEEDED FOR CREATION BY REFLECTION.</remarks>
		public static ITokenizerFactory<CoreLabel> Factory()
		{
			return FrenchTokenizer.FrenchTokenizerFactory.NewTokenizerFactory();
		}

		public static ITokenizerFactory<T> Factory<T>(ILexedTokenFactory<T> factory, string options)
			where T : IHasWord
		{
			return new FrenchTokenizer.FrenchTokenizerFactory<T>(factory, options);
		}

		/// <summary>
		/// Returns a factory for FrenchTokenizer that replicates the tokenization of
		/// Green, de Marneffe, and Manning (2011).
		/// </summary>
		public static ITokenizerFactory<CoreLabel> FtbFactory()
		{
			ITokenizerFactory<CoreLabel> tf = FrenchTokenizer.FrenchTokenizerFactory.NewTokenizerFactory();
			tf.SetOptions(FtbOptions);
			return tf;
		}

		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			sb.Append(string.Format("Usage: java %s [OPTIONS] < file%n%n", typeof(FrenchTokenizer).FullName));
			sb.Append("Options:").Append(nl);
			sb.Append("   -help          : Print this message.").Append(nl);
			sb.Append("   -ftb           : Tokenization for experiments in Green et al. (2011).").Append(nl);
			sb.Append("   -lowerCase     : Apply lowercasing.").Append(nl);
			sb.Append("   -encoding type : Encoding format.").Append(nl);
			sb.Append("   -options str   : Orthographic options (see FrenchLexer.java)").Append(nl);
			return sb.ToString();
		}

		private static IDictionary<string, int> ArgOptionDefs()
		{
			IDictionary<string, int> argOptionDefs = Generics.NewHashMap();
			argOptionDefs["help"] = 0;
			argOptionDefs["ftb"] = 0;
			argOptionDefs["lowerCase"] = 0;
			argOptionDefs["encoding"] = 1;
			argOptionDefs["options"] = 1;
			return argOptionDefs;
		}

		/// <summary>A fast, rule-based tokenizer for Modern Standard French.</summary>
		/// <remarks>
		/// A fast, rule-based tokenizer for Modern Standard French.
		/// Performs punctuation splitting and light tokenization by default.
		/// <p>
		/// Currently, this tokenizer does not do line splitting. It assumes that the input
		/// file is delimited by the system line separator. The output will be equivalently
		/// delimited.
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
			ITokenizerFactory<CoreLabel> tf = options.Contains("ftb") ? FrenchTokenizer.FtbFactory() : FrenchTokenizer.Factory();
			string orthoOptions = options.GetProperty("options", string.Empty);
			// When called from this main method, split on newline. No options for
			// more granular sentence splitting.
			orthoOptions = orthoOptions.IsEmpty() ? "tokenizeNLs" : orthoOptions + ",tokenizeNLs";
			tf.SetOptions(orthoOptions);
			// Other options
			string encoding = options.GetProperty("encoding", "UTF-8");
			bool toLower = PropertiesUtils.GetBool(options, "lowerCase", false);
			// Read the file from stdin
			int nLines = 0;
			int nTokens = 0;
			long startTime = Runtime.NanoTime();
			try
			{
				ITokenizer<CoreLabel> tokenizer = tf.GetTokenizer(new InputStreamReader(Runtime.@in, encoding));
				bool printSpace = false;
				while (tokenizer.MoveNext())
				{
					++nTokens;
					string word = tokenizer.Current.Word();
					if (word.Equals(FrenchLexer.NewlineToken))
					{
						++nLines;
						printSpace = false;
						System.Console.Out.WriteLine();
					}
					else
					{
						if (printSpace)
						{
							System.Console.Out.Write(" ");
						}
						string outputToken = toLower ? word.ToLower(Locale.French) : word;
						System.Console.Out.Write(outputToken);
						printSpace = true;
					}
				}
			}
			catch (UnsupportedEncodingException e)
			{
				log.Error(e);
			}
			long elapsedTime = Runtime.NanoTime() - startTime;
			double linesPerSec = (double)nLines / (elapsedTime / 1e9);
			System.Console.Error.Printf("Done! Tokenized %d lines (%d tokens) at %.2f lines/sec%n", nLines, nTokens, linesPerSec);
		}
		// end main()
	}
}

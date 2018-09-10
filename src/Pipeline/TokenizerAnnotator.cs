using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.French.Process;
using Edu.Stanford.Nlp.International.Spanish.Process;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will PTB tokenize the input.</summary>
	/// <remarks>
	/// This class will PTB tokenize the input.  It assumes that the original
	/// String is under the CoreAnnotations.TextAnnotation field
	/// and it will add the output from the
	/// InvertiblePTBTokenizer (
	/// <c>List&lt;CoreLabel&gt;</c>
	/// ) under
	/// CoreAnnotation.TokensAnnotation.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning</author>
	/// <author>Ishita Prasad</author>
	public class TokenizerAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.TokenizerAnnotator));

		/// <summary>Enum to identify the different TokenizerTypes.</summary>
		/// <remarks>
		/// Enum to identify the different TokenizerTypes. To add a new
		/// TokenizerType, add it to the list with a default options string
		/// and add a clause in getTokenizerType to identify it.
		/// </remarks>
		[System.Serializable]
		public sealed class TokenizerType
		{
			public static readonly TokenizerAnnotator.TokenizerType Unspecified = new TokenizerAnnotator.TokenizerType(null, null, "invertible,ptb3Escaping=true");

			public static readonly TokenizerAnnotator.TokenizerType Arabic = new TokenizerAnnotator.TokenizerType("ar", null, string.Empty);

			public static readonly TokenizerAnnotator.TokenizerType Chinese = new TokenizerAnnotator.TokenizerType("zh", null, string.Empty);

			public static readonly TokenizerAnnotator.TokenizerType Spanish = new TokenizerAnnotator.TokenizerType("es", "SpanishTokenizer", "invertible,ptb3Escaping=true,splitAll=true");

			public static readonly TokenizerAnnotator.TokenizerType English = new TokenizerAnnotator.TokenizerType("en", "PTBTokenizer", "invertible,ptb3Escaping=true");

			public static readonly TokenizerAnnotator.TokenizerType German = new TokenizerAnnotator.TokenizerType("de", null, "invertible,ptb3Escaping=true");

			public static readonly TokenizerAnnotator.TokenizerType French = new TokenizerAnnotator.TokenizerType("fr", "FrenchTokenizer", string.Empty);

			public static readonly TokenizerAnnotator.TokenizerType Whitespace = new TokenizerAnnotator.TokenizerType(null, "WhitespaceTokenizer", string.Empty);

			private readonly string abbreviation;

			private readonly string className;

			private readonly string defaultOptions;

			internal TokenizerType(string abbreviation, string className, string defaultOptions)
			{
				this.abbreviation = abbreviation;
				this.className = className;
				this.defaultOptions = defaultOptions;
			}

			public string GetDefaultOptions()
			{
				return TokenizerAnnotator.TokenizerType.defaultOptions;
			}

			private static readonly IDictionary<string, TokenizerAnnotator.TokenizerType> nameToTokenizerMap = InitializeNameMap();

			private static IDictionary<string, TokenizerAnnotator.TokenizerType> InitializeNameMap()
			{
				IDictionary<string, TokenizerAnnotator.TokenizerType> map = Generics.NewHashMap();
				foreach (TokenizerAnnotator.TokenizerType type in TokenizerAnnotator.TokenizerType.Values())
				{
					if (type.abbreviation != null)
					{
						map[type.abbreviation.ToUpper()] = type;
					}
					map[type.ToString().ToUpper()] = type;
				}
				return Java.Util.Collections.UnmodifiableMap(map);
			}

			private static readonly IDictionary<string, TokenizerAnnotator.TokenizerType> classToTokenizerMap = InitializeClassMap();

			private static IDictionary<string, TokenizerAnnotator.TokenizerType> InitializeClassMap()
			{
				IDictionary<string, TokenizerAnnotator.TokenizerType> map = Generics.NewHashMap();
				foreach (TokenizerAnnotator.TokenizerType type in TokenizerAnnotator.TokenizerType.Values())
				{
					if (type.className != null)
					{
						map[type.className.ToUpper()] = type;
					}
				}
				return Java.Util.Collections.UnmodifiableMap(map);
			}

			/// <summary>Get TokenizerType based on what's in the properties.</summary>
			/// <param name="props">Properties to find tokenizer options in</param>
			/// <returns>An element of the TokenizerType enum indicating the tokenizer to use</returns>
			public static TokenizerAnnotator.TokenizerType GetTokenizerType(Properties props)
			{
				string tokClass = props.GetProperty("tokenize.class", null);
				bool whitespace = bool.ValueOf(props.GetProperty("tokenize.whitespace", "false"));
				string language = props.GetProperty("tokenize.language", "en");
				if (whitespace)
				{
					return TokenizerAnnotator.TokenizerType.Whitespace;
				}
				if (tokClass != null)
				{
					TokenizerAnnotator.TokenizerType type = TokenizerAnnotator.TokenizerType.classToTokenizerMap[tokClass.ToUpper()];
					if (type == null)
					{
						throw new ArgumentException("TokenizerAnnotator: unknown tokenize.class property " + tokClass);
					}
					return type;
				}
				if (language != null)
				{
					TokenizerAnnotator.TokenizerType type = TokenizerAnnotator.TokenizerType.nameToTokenizerMap[language.ToUpper()];
					if (type == null)
					{
						throw new ArgumentException("TokenizerAnnotator: unknown tokenize.language property " + language);
					}
					return type;
				}
				return TokenizerAnnotator.TokenizerType.Unspecified;
			}
		}

		public const string EolProperty = "tokenize.keepeol";

		private readonly bool Verbose;

		private readonly ITokenizerFactory<CoreLabel> factory;

		/// <summary>new segmenter properties</summary>
		private readonly bool useSegmenter;

		private readonly IAnnotator segmenterAnnotator;

		/// <summary>Gives a non-verbose, English tokenizer.</summary>
		public TokenizerAnnotator()
			: this(false)
		{
		}

		// end enum TokenizerType
		// CONSTRUCTORS
		private static string ComputeExtraOptions(Properties properties)
		{
			string extraOptions = null;
			bool keepNewline = bool.ValueOf(properties.GetProperty(StanfordCoreNLP.NewlineSplitterProperty, "false"));
			// ssplit.eolonly
			string hasSsplit = properties.GetProperty("annotators");
			if (hasSsplit != null && hasSsplit.Contains(StanfordCoreNLP.StanfordSsplit))
			{
				// ssplit
				// Only possibly put in *NL* if not all one (the Boolean method treats null as false)
				if (!bool.Parse(properties.GetProperty("ssplit.isOneSentence")))
				{
					// Set to { NEVER, ALWAYS, TWO_CONSECUTIVE } based on  ssplit.newlineIsSentenceBreak
					string nlsbString = properties.GetProperty(StanfordCoreNLP.NewlineIsSentenceBreakProperty, StanfordCoreNLP.DefaultNewlineIsSentenceBreak);
					WordToSentenceProcessor.NewlineIsSentenceBreak nlsb = WordToSentenceProcessor.StringToNewlineIsSentenceBreak(nlsbString);
					if (nlsb != WordToSentenceProcessor.NewlineIsSentenceBreak.Never)
					{
						keepNewline = true;
					}
				}
			}
			if (keepNewline)
			{
				extraOptions = "tokenizeNLs,";
			}
			return extraOptions;
		}

		public TokenizerAnnotator(Properties properties)
			: this(false, properties, ComputeExtraOptions(properties))
		{
		}

		public TokenizerAnnotator(bool verbose)
			: this(verbose, TokenizerAnnotator.TokenizerType.English)
		{
		}

		public TokenizerAnnotator(string lang)
			: this(true, lang, null)
		{
		}

		public TokenizerAnnotator(bool verbose, TokenizerAnnotator.TokenizerType lang)
			: this(verbose, lang.ToString())
		{
		}

		public TokenizerAnnotator(bool verbose, string lang)
			: this(verbose, lang, null)
		{
		}

		public TokenizerAnnotator(bool verbose, string lang, string options)
			: this(verbose, lang == null ? null : PropertiesUtils.AsProperties("tokenize.language", lang), options)
		{
		}

		public TokenizerAnnotator(bool verbose, Properties props)
			: this(verbose, props, null)
		{
		}

		public TokenizerAnnotator(bool verbose, Properties props, string options)
		{
			if (props == null)
			{
				props = new Properties();
			}
			// check if segmenting must be done
			if (props.GetProperty("tokenize.language") != null && LanguageInfo.IsSegmenterLanguage(props.GetProperty("tokenize.language")))
			{
				useSegmenter = true;
				if (LanguageInfo.GetLanguageFromString(props.GetProperty("tokenize.language")) == LanguageInfo.HumanLanguage.Arabic)
				{
					segmenterAnnotator = new ArabicSegmenterAnnotator("segment", props);
				}
				else
				{
					if (LanguageInfo.GetLanguageFromString(props.GetProperty("tokenize.language")) == LanguageInfo.HumanLanguage.Chinese)
					{
						segmenterAnnotator = new ChineseSegmenterAnnotator("segment", props);
					}
					else
					{
						segmenterAnnotator = null;
						throw new Exception("No segmenter implemented for: " + LanguageInfo.GetLanguageFromString(props.GetProperty("tokenize.language")));
					}
				}
			}
			else
			{
				useSegmenter = false;
				segmenterAnnotator = null;
			}
			Verbose = PropertiesUtils.GetBool(props, "tokenize.verbose", verbose);
			TokenizerAnnotator.TokenizerType type = TokenizerAnnotator.TokenizerType.GetTokenizerType(props);
			factory = InitFactory(type, props, options);
		}

		/// <summary>
		/// initFactory returns the right type of TokenizerFactory based on the options in the properties file
		/// and the type.
		/// </summary>
		/// <remarks>
		/// initFactory returns the right type of TokenizerFactory based on the options in the properties file
		/// and the type. When adding a new Tokenizer, modify TokenizerType.getTokenizerType() to retrieve
		/// your tokenizer from the properties file, and then add a class is the switch structure here to
		/// instantiate the new Tokenizer type.
		/// </remarks>
		/// <param name="type">the TokenizerType</param>
		/// <param name="props">the properties file</param>
		/// <param name="extraOptions">extra things that should be passed into the tokenizer constructor</param>
		/// <exception cref="System.ArgumentException"/>
		private static ITokenizerFactory<CoreLabel> InitFactory(TokenizerAnnotator.TokenizerType type, Properties props, string extraOptions)
		{
			ITokenizerFactory<CoreLabel> factory;
			string options = props.GetProperty("tokenize.options", null);
			// set it to the equivalent of both extraOptions and options
			// TODO: maybe we should always have getDefaultOptions() and
			// expect the user to turn off default options.  That would
			// require all options to have negated options, but
			// currently there are some which don't have that
			if (options == null)
			{
				options = type.GetDefaultOptions();
			}
			if (extraOptions != null)
			{
				if (extraOptions.EndsWith(","))
				{
					options = extraOptions + options;
				}
				else
				{
					options = extraOptions + ',' + options;
				}
			}
			switch (type)
			{
				case TokenizerAnnotator.TokenizerType.Arabic:
				case TokenizerAnnotator.TokenizerType.Chinese:
				{
					factory = null;
					break;
				}

				case TokenizerAnnotator.TokenizerType.Spanish:
				{
					factory = SpanishTokenizer.Factory(new CoreLabelTokenFactory(), options);
					break;
				}

				case TokenizerAnnotator.TokenizerType.French:
				{
					factory = FrenchTokenizer.Factory(new CoreLabelTokenFactory(), options);
					break;
				}

				case TokenizerAnnotator.TokenizerType.Whitespace:
				{
					bool eolIsSignificant = bool.ValueOf(props.GetProperty(EolProperty, "false"));
					eolIsSignificant = eolIsSignificant || bool.ValueOf(props.GetProperty(StanfordCoreNLP.NewlineSplitterProperty, "false"));
					factory = new WhitespaceTokenizer.WhitespaceTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory(), eolIsSignificant);
					break;
				}

				case TokenizerAnnotator.TokenizerType.English:
				case TokenizerAnnotator.TokenizerType.German:
				{
					factory = PTBTokenizer.Factory(new CoreLabelTokenFactory(), options);
					break;
				}

				case TokenizerAnnotator.TokenizerType.Unspecified:
				{
					log.Info("No tokenizer type provided. Defaulting to PTBTokenizer.");
					factory = PTBTokenizer.Factory(new CoreLabelTokenFactory(), options);
					break;
				}

				default:
				{
					throw new ArgumentException("No valid tokenizer type provided.\n" + "Use -tokenize.language, -tokenize.class, or -tokenize.whitespace \n" + "to specify a tokenizer.");
				}
			}
			return factory;
		}

		/// <summary>Returns a thread-safe tokenizer</summary>
		public virtual ITokenizer<CoreLabel> GetTokenizer(Reader r)
		{
			return factory.GetTokenizer(r);
		}

		/// <summary>Helper method to set the TokenBeginAnnotation and TokenEndAnnotation of every token.</summary>
		private static void SetTokenBeginTokenEnd(IList<CoreLabel> tokensList)
		{
			int tokenIndex = 0;
			foreach (CoreLabel token in tokensList)
			{
				token.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenIndex);
				token.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenIndex + 1);
				tokenIndex++;
			}
		}

		/// <summary>set isNewline()</summary>
		private static void SetNewlineStatus(IList<CoreLabel> tokensList)
		{
			// label newlines
			foreach (CoreLabel token in tokensList)
			{
				if (token.Word().Equals(AbstractTokenizer.NewlineToken) && (token.EndPosition() - token.BeginPosition() == 1))
				{
					token.Set(typeof(CoreAnnotations.IsNewlineAnnotation), true);
				}
				else
				{
					token.Set(typeof(CoreAnnotations.IsNewlineAnnotation), false);
				}
			}
		}

		/// <summary>
		/// Does the actual work of splitting TextAnnotation into CoreLabels,
		/// which are then attached to the TokensAnnotation.
		/// </summary>
		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				log.Info("Tokenizing ... ");
			}
			// for Arabic and Chinese use a segmenter instead
			if (useSegmenter)
			{
				segmenterAnnotator.Annotate(annotation);
				// set indexes into document wide tokens list
				SetTokenBeginTokenEnd(annotation.Get(typeof(CoreAnnotations.TokensAnnotation)));
				SetNewlineStatus(annotation.Get(typeof(CoreAnnotations.TokensAnnotation)));
				return;
			}
			if (annotation.ContainsKey(typeof(CoreAnnotations.TextAnnotation)))
			{
				string text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
				Reader r = new StringReader(text);
				// don't wrap in BufferedReader.  It gives you nothing for in-memory String unless you need the readLine() method!
				IList<CoreLabel> tokens = GetTokenizer(r).Tokenize();
				// cdm 2010-05-15: This is now unnecessary, as it is done in CoreLabelTokenFactory
				// for (CoreLabel token: tokens) {
				// token.set(CoreAnnotations.TextAnnotation.class, token.get(CoreAnnotations.TextAnnotation.class));
				// }
				// label newlines
				SetNewlineStatus(tokens);
				// set indexes into document wide token list
				SetTokenBeginTokenEnd(tokens);
				// add tokens list to annotation
				annotation.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
				if (Verbose)
				{
					log.Info("done.");
					log.Info("Tokens: " + annotation.Get(typeof(CoreAnnotations.TokensAnnotation)));
				}
			}
			else
			{
				throw new Exception("Tokenizer unable to find text in annotation: " + annotation);
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.EmptySet();
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.BeforeAnnotation
				), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.PositionAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
				), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.IsNewlineAnnotation)));
		}
	}
}

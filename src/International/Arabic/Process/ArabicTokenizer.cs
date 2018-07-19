using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>Tokenizer for UTF-8 Arabic.</summary>
	/// <remarks>
	/// Tokenizer for UTF-8 Arabic. Buckwalter encoding is <i>not</i> supported.
	/// <p>
	/// A single instance of an Arabic Tokenizer is not thread safe, as it
	/// uses a non-threadsafe jflex object to do the processing.  Multiple
	/// instances can be created safely, though.  A single instance of a
	/// ArabicTokenizerFactory is also not thread safe, as it keeps its
	/// options in a local variable.
	/// <p>
	/// TODO(spenceg): Merge in rules from ibm tokenizer (v5).
	/// TODO(spenceg): Add XML escaping
	/// TODO(spenceg): When running from the command line, the tokenizer does not
	/// produce the correct number of newline-delimited lines for the ATB data
	/// sets.
	/// </remarks>
	/// <author>Spence Green</author>
	public class ArabicTokenizer<T> : AbstractTokenizer<T>
		where T : IHasWord
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Process.ArabicTokenizer));

		private readonly ArabicLexer lexer;

		private static readonly Properties atbOptions = new Properties();

		static ArabicTokenizer()
		{
			// The underlying JFlex lexer
			// Produces the normalization for parsing used in Green and Manning (2010)
			string optionsStr = "normArDigits,normArPunc,normAlif,removeDiacritics,removeTatweel,removeQuranChars";
			string[] optionToks = optionsStr.Split(",");
			foreach (string option in optionToks)
			{
				atbOptions.SetProperty(option, "true");
			}
		}

		public static Edu.Stanford.Nlp.International.Arabic.Process.ArabicTokenizer<CoreLabel> NewArabicTokenizer(Reader r, Properties lexerProperties)
		{
			return new Edu.Stanford.Nlp.International.Arabic.Process.ArabicTokenizer<CoreLabel>(r, new CoreLabelTokenFactory(), lexerProperties);
		}

		public ArabicTokenizer(Reader r, ILexedTokenFactory<T> tf, Properties lexerProperties)
		{
			lexer = new ArabicLexer(r, tf, lexerProperties);
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
					nextToken = (T)lexer.Next();
				}
				while (nextToken != null && nextToken.Word().Length == 0);
				return nextToken;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		[System.Serializable]
		public class ArabicTokenizerFactory<T> : ITokenizerFactory<T>
			where T : IHasWord
		{
			private const long serialVersionUID = 946818805507187330L;

			protected internal readonly ILexedTokenFactory<T> factory;

			protected internal Properties lexerProperties = new Properties();

			public static ITokenizerFactory<CoreLabel> NewTokenizerFactory()
			{
				return new ArabicTokenizer.ArabicTokenizerFactory<CoreLabel>(new CoreLabelTokenFactory());
			}

			private ArabicTokenizerFactory(ILexedTokenFactory<T> factory)
			{
				this.factory = factory;
			}

			public virtual IEnumerator<T> GetIterator(Reader r)
			{
				return GetTokenizer(r);
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r)
			{
				return new ArabicTokenizer<T>(r, factory, lexerProperties);
			}

			/// <summary>options: A comma-separated list of options</summary>
			public virtual void SetOptions(string options)
			{
				string[] optionList = options.Split(",");
				foreach (string option in optionList)
				{
					lexerProperties[option] = "true";
				}
			}

			public virtual ITokenizer<T> GetTokenizer(Reader r, string extraOptions)
			{
				SetOptions(extraOptions);
				return GetTokenizer(r);
			}
		}

		// end static class ArabicTokenizerFactory
		public static ITokenizerFactory<CoreLabel> Factory()
		{
			return ArabicTokenizer.ArabicTokenizerFactory.NewTokenizerFactory();
		}

		public static ITokenizerFactory<CoreLabel> AtbFactory()
		{
			ITokenizerFactory<CoreLabel> tf = ArabicTokenizer.ArabicTokenizerFactory.NewTokenizerFactory();
			foreach (string option in atbOptions.StringPropertyNames())
			{
				tf.SetOptions(option);
			}
			return tf;
		}

		/// <summary>A fast, rule-based tokenizer for Modern Standard Arabic (UTF-8 encoding).</summary>
		/// <remarks>
		/// A fast, rule-based tokenizer for Modern Standard Arabic (UTF-8 encoding).
		/// Performs punctuation splitting and light tokenization by default.
		/// Orthographic normalization options are available, and can be enabled with
		/// command line options.
		/// <p>
		/// Currently, this tokenizer does not do line splitting. It normalizes non-printing
		/// line separators across platforms and prints the system default line splitter
		/// to the output.
		/// <p>
		/// The following normalization options are provided:
		/// <ul>
		/// <li>
		/// <c>useUTF8Ellipsis</c>
		/// : Replaces sequences of three or more full stops with \u2026</li>
		/// <li>
		/// <c>normArDigits</c>
		/// : Convert Arabic digits to ASCII equivalents</li>
		/// <li>
		/// <c>normArPunc</c>
		/// : Convert Arabic punctuation to ASCII equivalents</li>
		/// <li>
		/// <c>normAlif</c>
		/// : Change all alif forms to bare alif</li>
		/// <li>
		/// <c>normYa</c>
		/// : Map ya to alif maqsura</li>
		/// <li>
		/// <c>removeDiacritics</c>
		/// : Strip all diacritics</li>
		/// <li>
		/// <c>removeTatweel</c>
		/// : Strip tatweel elongation character</li>
		/// <li>
		/// <c>removeQuranChars</c>
		/// : Remove diacritics that appear in the Quran</li>
		/// <li>
		/// <c>removeProMarker</c>
		/// : Remove the ATB null pronoun marker</li>
		/// <li>
		/// <c>removeSegMarker</c>
		/// : Remove the ATB clitic segmentation marker</li>
		/// <li>
		/// <c>removeMorphMarker</c>
		/// : Remove the ATB morpheme boundary markers</li>
		/// <li>
		/// <c>removeLengthening</c>
		/// : Replace all sequences of three or more identical (non-period) characters with one copy</li>
		/// <li>
		/// <c>atbEscaping</c>
		/// : Replace left/right parentheses with ATB escape characters</li>
		/// </ul>
		/// </remarks>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length > 0 && args[0].Contains("help"))
			{
				System.Console.Error.Printf("Usage: java %s [OPTIONS] < file%n", typeof(ArabicTokenizer).FullName);
				System.Console.Error.Printf("%nOptions:%n");
				log.Info("   -help : Print this message. See javadocs for all normalization options.");
				log.Info("   -atb  : Tokenization for the parsing experiments in Green and Manning (2010)");
				System.Environment.Exit(-1);
			}
			// Process normalization options
			Properties tokenizerOptions = StringUtils.ArgsToProperties(args);
			ITokenizerFactory<CoreLabel> tf = tokenizerOptions.Contains("atb") ? ArabicTokenizer.AtbFactory() : ArabicTokenizer.Factory();
			foreach (string option in tokenizerOptions.StringPropertyNames())
			{
				tf.SetOptions(option);
			}
			// Replace line separators with a token so that we can
			// count lines
			tf.SetOptions("tokenizeNLs");
			// Read the file
			int nLines = 0;
			int nTokens = 0;
			try
			{
				string encoding = "UTF-8";
				ITokenizer<CoreLabel> tokenizer = tf.GetTokenizer(new InputStreamReader(Runtime.@in, encoding));
				bool printSpace = false;
				while (tokenizer.MoveNext())
				{
					++nTokens;
					string word = tokenizer.Current.Word();
					if (word.Equals(ArabicLexer.NewlineToken))
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
						System.Console.Out.Write(word);
						printSpace = true;
					}
				}
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			System.Console.Error.Printf("Done! Tokenized %d lines (%d tokens)%n", nLines, nTokens);
		}
	}
}

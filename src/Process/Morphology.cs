using System;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// Morphology computes the base form of English words, by removing just
	/// inflections (not derivational morphology).
	/// </summary>
	/// <remarks>
	/// Morphology computes the base form of English words, by removing just
	/// inflections (not derivational morphology).  That is, it only does noun
	/// plurals, pronoun case, and verb endings, and not things like comparative adjectives
	/// or derived nominals.  It is based on a finite-state
	/// transducer implemented by John Carroll et al., written in flex and publicly
	/// available.
	/// See: http://www.informatics.susx.ac.uk/research/nlp/carroll/morph.html .
	/// There are several ways of invoking Morphology. One is by calling the static
	/// methods:
	/// <ul>
	/// <li> WordTag stemStatic(String word, String tag) </li>
	/// <li> WordTag stemStatic(WordTag wordTag) </li>
	/// </ul>
	/// If we have created a Morphology object already we can use the methods
	/// WordTag stem(String word, string tag) or WordTag stem(WordTag wordTag).
	/// <p>
	/// Another way of using Morphology is to run it on an input file by running
	/// <c>java Morphology filename</c>
	/// .  In this case, POS tags MUST be
	/// separated from words by an underscore ("_").
	/// <p>
	/// Note that a single instance of Morphology is not thread-safe, as
	/// the underlying lexer object is not built to be re-entrant.  One thing that
	/// you can do to get around this is build a new Morphology object for
	/// each thread or each set of calls to the Morphology.  For example, the
	/// MorphaAnnotator builds a Morphology for each document it annotates.
	/// The other approach is to use the synchronized methods in this class.
	/// The crucial lexer-accessing portion of all the static methods is synchronized
	/// (otherwise, their use tended to be threading bugs waiting to happen).
	/// If you want less synchronization, create your own Morphology objects.
	/// </remarks>
	/// <author>Kristina Toutanova (kristina@cs.stanford.edu)</author>
	/// <author>Christopher Manning</author>
	public class Morphology : IFunction
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.Morphology));

		private const bool Debug = false;

		private static Morpha staticLexer;

		private readonly Morpha lexer;

		public Morphology()
		{
			lexer = new Morpha(new InputStreamReader(Runtime.@in));
		}

		/// <summary>Process morphologically words from a Reader.</summary>
		/// <param name="in">The Reader to read from</param>
		public Morphology(Reader @in)
		{
			lexer = new Morpha(@in);
		}

		public Morphology(Reader @in, int flags)
		{
			lexer = new Morpha(@in);
			lexer.SetOptions(flags);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Word Next()
		{
			string nx = lexer.Next();
			if (nx == null)
			{
				return null;
			}
			else
			{
				return new Word(nx);
			}
		}

		public virtual Word Stem(Word w)
		{
			return new Word(Stem(w.Value()));
		}

		public virtual string Stem(string word)
		{
			try
			{
				lexer.Yyreset(new StringReader(word));
				lexer.Yybegin(Morpha.any);
				string wordRes = lexer.Next();
				return wordRes;
			}
			catch (IOException)
			{
				log.Warning("Morphology.stem() had error on word " + word);
				return word;
			}
		}

		public virtual string Lemma(string word, string tag)
		{
			return Lemmatize(word, tag, lexer, lexer.Option(1));
		}

		public virtual string Lemma(string word, string tag, bool lowercase)
		{
			return Lemmatize(word, tag, lexer, lowercase);
		}

		/// <summary>Adds the LemmaAnnotation to the given CoreLabel.</summary>
		public virtual void Stem(CoreLabel label)
		{
			Stem(label, typeof(CoreAnnotations.LemmaAnnotation));
		}

		/// <summary>
		/// Adds stem under annotation
		/// <paramref name="ann"/>
		/// to the given CoreLabel.
		/// Assumes that it has a TextAnnotation and PartOfSpeechAnnotation.
		/// </summary>
		public virtual void Stem(CoreLabel label, Type ann)
		{
			string lemma = Lemmatize(label.Word(), label.Tag(), lexer, lexer.Option(1));
			label.Set(ann, lemma);
		}

		/// <summary>
		/// Lemmatize the word, being sensitive to the tag, using the
		/// passed in lexer.
		/// </summary>
		/// <param name="lowercase">
		/// If this is true, words other than proper nouns will
		/// be changed to all lowercase.
		/// </param>
		private static string Lemmatize(string word, string tag, Morpha lexer, bool lowercase)
		{
			bool wordHasForbiddenChar = word.IndexOf('_') >= 0 || word.IndexOf(' ') >= 0 || word.IndexOf('\n') >= 0;
			string quotedWord = word;
			if (wordHasForbiddenChar)
			{
				// choose something unlikely. Classical Vedic!
				quotedWord = quotedWord.ReplaceAll("_", "\u1CF0");
				quotedWord = quotedWord.ReplaceAll(" ", "\u1CF1");
				quotedWord = quotedWord.ReplaceAll("\n", "\u1CF2");
			}
			string wordtag = quotedWord + '_' + tag;
			try
			{
				lexer.SetOption(1, lowercase);
				lexer.Yyreset(new StringReader(wordtag));
				lexer.Yybegin(Morpha.scan);
				string wordRes = lexer.Next();
				lexer.Next();
				// go past tag
				if (wordHasForbiddenChar)
				{
					wordRes = wordRes.ReplaceAll("\u1CF0", "_");
					wordRes = wordRes.ReplaceAll("\u1CF1", " ");
					wordRes = wordRes.ReplaceAll("\u1CF2", "\n");
				}
				return wordRes;
			}
			catch (IOException)
			{
				log.Warning("Morphology.stem() had error on word " + word + '/' + tag);
				return word;
			}
		}

		private static void InitStaticLexer()
		{
			lock (typeof(Morphology))
			{
				if (staticLexer == null)
				{
					staticLexer = new Morpha(new InputStreamReader(Runtime.@in));
				}
			}
		}

		/// <summary>Return a new WordTag which has the lemma as the value of word().</summary>
		/// <remarks>
		/// Return a new WordTag which has the lemma as the value of word().
		/// The default is to lowercase non-proper-nouns, unless options have
		/// been set.
		/// </remarks>
		public static WordTag StemStatic(string word, string tag)
		{
			lock (typeof(Morphology))
			{
				InitStaticLexer();
				return new WordTag(Lemmatize(word, tag, staticLexer, staticLexer.Option(1)), tag);
			}
		}

		/// <summary>Lemmatize the word, being sensitive to the tag.</summary>
		/// <remarks>
		/// Lemmatize the word, being sensitive to the tag.
		/// Words other than proper nouns will be changed to all lowercase.
		/// </remarks>
		/// <param name="word">The word to lemmatize</param>
		/// <param name="tag">What part of speech to assume for it.</param>
		/// <returns>The lemma for the word</returns>
		public static string LemmaStatic(string word, string tag)
		{
			lock (typeof(Morphology))
			{
				return LemmaStatic(word, tag, true);
			}
		}

		/// <summary>Lemmatize the word, being sensitive to the tag.</summary>
		/// <param name="word">The word to lemmatize</param>
		/// <param name="tag">What part of speech to assume for it.</param>
		/// <param name="lowercase">
		/// If this is true, words other than proper nouns will
		/// be changed to all lowercase.
		/// </param>
		/// <returns>The lemma for the word</returns>
		public static string LemmaStatic(string word, string tag, bool lowercase)
		{
			lock (typeof(Morphology))
			{
				InitStaticLexer();
				return Lemmatize(word, tag, staticLexer, lowercase);
			}
		}

		/// <summary>Return a new WordTag which has the lemma as the value of word().</summary>
		/// <remarks>
		/// Return a new WordTag which has the lemma as the value of word().
		/// The default is to lowercase non-proper-nouns, unless options have
		/// been set.
		/// </remarks>
		public static WordTag StemStatic(WordTag wT)
		{
			return StemStatic(wT.Word(), wT.Tag());
		}

		public virtual object Apply(object @in)
		{
			if (@in is WordTag)
			{
				WordTag wt = (WordTag)@in;
				string tag = wt.Tag();
				return new WordTag(Lemmatize(wt.Word(), tag, lexer, lexer.Option(1)), tag);
			}
			if (@in is Word)
			{
				return Stem((Word)@in);
			}
			return @in;
		}

		/// <summary>
		/// Lemmatize returning a
		/// <c>WordLemmaTag</c>
		/// .
		/// </summary>
		public virtual WordLemmaTag Lemmatize(WordTag wT)
		{
			string tag = wT.Tag();
			string word = wT.Word();
			string lemma = Lemma(word, tag);
			return new WordLemmaTag(word, lemma, tag);
		}

		public static WordLemmaTag LemmatizeStatic(WordTag wT)
		{
			string tag = wT.Tag();
			string word = wT.Word();
			string lemma = StemStatic(wT).Word();
			return new WordLemmaTag(word, lemma, tag);
		}

		/// <summary>Run the morphological analyzer.</summary>
		/// <remarks>
		/// Run the morphological analyzer.  Options are:
		/// <ul>
		/// <li>-rebuildVerbTable verbTableFile Convert a verb table from a text file
		/// (e.g., /u/nlp/data/morph/verbstem.list) to Java code contained in Morpha.flex .
		/// <li>-stem args ...  Stem each of the following arguments, which should either be
		/// in the form of just word or word_tag.
		/// <li> args ...  Each argument is a file and the contents of it are stemmed as
		/// space-separated tokens.    <i>Note:</i> If the tokens are tagged
		/// words, they must be in the format of whitespace separated word_tag pairs.
		/// </ul>
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				log.Info("java Morphology [-rebuildVerbTable file|-stem word+|file+]");
			}
			else
			{
				if (args.Length == 2 && args[0].Equals("-rebuildVerbTable"))
				{
					string verbs = IOUtils.SlurpFile(args[1]);
					string[] words = verbs.Split("\\s+");
					System.Console.Out.Write(" private static final String[] verbStems = { ");
					for (int i = 0; i < words.Length; i++)
					{
						System.Console.Out.Write('"' + words[i] + '"');
						if (i != words.Length - 1)
						{
							System.Console.Out.Write(", ");
							if (i % 5 == 0)
							{
								System.Console.Out.WriteLine();
								System.Console.Out.Write("    ");
							}
						}
					}
					System.Console.Out.WriteLine(" };");
				}
				else
				{
					if (args[0].Equals("-stem"))
					{
						for (int i = 1; i < args.Length; i++)
						{
							System.Console.Out.WriteLine(args[i] + " --> " + StemStatic(WordTag.ValueOf(args[i], "_")));
						}
					}
					else
					{
						int flags = 0;
						foreach (string arg in args)
						{
							if (arg[0] == '-')
							{
								try
								{
									flags = System.Convert.ToInt32(Sharpen.Runtime.Substring(arg, 1));
								}
								catch (NumberFormatException)
								{
									log.Info("Couldn't handle flag: " + arg + '\n');
								}
							}
							else
							{
								// ignore flag
								Edu.Stanford.Nlp.Process.Morphology morph = new Edu.Stanford.Nlp.Process.Morphology(new FileReader(arg), flags);
								for (Word next; (next = morph.Next()) != null; )
								{
									System.Console.Out.Write(next);
								}
							}
						}
					}
				}
			}
		}
	}
}

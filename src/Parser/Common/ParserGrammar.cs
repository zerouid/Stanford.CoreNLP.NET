using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Common
{
	/// <summary>An interface for the classes which store the data for a parser.</summary>
	/// <remarks>
	/// An interface for the classes which store the data for a parser.
	/// Objects which inherit this interface have a way to produce
	/// ParserQuery objects, have a general Options object, and return a
	/// list of Evals to perform on a parser.  This helps classes such as
	/// <see cref="Edu.Stanford.Nlp.Parser.Lexparser.EvaluateTreebank"/>
	/// analyze the performance of a parser.
	/// TODO: it would be nice to actually make this an interface again.
	/// Perhaps Java 8 will allow that
	/// </remarks>
	/// <author>John Bauer</author>
	public abstract class ParserGrammar : IFunction<IList<IHasWord>, Tree>
	{
		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(ParserGrammar));

		// TODO: it would be nice to move these to common, but that would
		// wreck all existing models
		public abstract IParserQuery ParserQuery();

		/// <summary>Parses the list of HasWord.</summary>
		/// <remarks>
		/// Parses the list of HasWord.  If the parse fails for some reason,
		/// an X tree is returned instead of barfing.
		/// </remarks>
		/// <param name="words">The input sentence (a List of words)</param>
		/// <returns>
		/// A Tree that is the parse tree for the sentence.  If the parser
		/// fails, a new Tree is synthesized which attaches all words to the
		/// root.
		/// </returns>
		public virtual Tree Apply<_T0>(IList<_T0> words)
			where _T0 : IHasWord
		{
			return Parse(words);
		}

		/// <summary>Tokenize the text using the parser's tokenizer</summary>
		public virtual IList<IHasWord> Tokenize(string sentence)
		{
			ITokenizerFactory<IHasWord> tf = TreebankLanguagePack().GetTokenizerFactory();
			ITokenizer<IHasWord> tokenizer = tf.GetTokenizer(new StringReader(sentence));
			IList<IHasWord> tokens = tokenizer.Tokenize();
			return tokens;
		}

		/// <summary>
		/// Will parse the text in <code>sentence</code> as if it represented
		/// a single sentence by first processing it with a tokenizer.
		/// </summary>
		public virtual Tree Parse(string sentence)
		{
			IList<IHasWord> tokens = Tokenize(sentence);
			if (GetOp().testOptions.preTag)
			{
				IFunction<IList<IHasWord>, IList<TaggedWord>> tagger = LoadTagger();
				tokens = tagger.Apply(tokens);
			}
			return Parse(tokens);
		}

		[System.NonSerialized]
		private IFunction<IList<IHasWord>, IList<TaggedWord>> tagger;

		[System.NonSerialized]
		private string taggerPath;

		public virtual IFunction<IList<IHasWord>, IList<TaggedWord>> LoadTagger()
		{
			Options op = GetOp();
			if (op.testOptions.preTag)
			{
				lock (this)
				{
					// TODO: rather coarse synchronization
					if (!op.testOptions.taggerSerializedFile.Equals(taggerPath))
					{
						taggerPath = op.testOptions.taggerSerializedFile;
						tagger = ReflectionLoading.LoadByReflection("edu.stanford.nlp.tagger.maxent.MaxentTagger", taggerPath);
					}
					return tagger;
				}
			}
			else
			{
				return null;
			}
		}

		public virtual IList<CoreLabel> Lemmatize(string sentence)
		{
			IList<IHasWord> tokens = Tokenize(sentence);
			return Lemmatize(tokens);
		}

		/// <summary>
		/// Only works on English, as it is hard coded for using the
		/// Morphology class, which is English-only
		/// </summary>
		public virtual IList<CoreLabel> Lemmatize<_T0>(IList<_T0> tokens)
			where _T0 : IHasWord
		{
			IList<TaggedWord> tagged;
			if (GetOp().testOptions.preTag)
			{
				IFunction<IList<IHasWord>, IList<TaggedWord>> tagger = LoadTagger();
				tagged = tagger.Apply(tokens);
			}
			else
			{
				Tree tree = Parse(tokens);
				tagged = tree.TaggedYield();
			}
			Morphology morpha = new Morphology();
			IList<CoreLabel> lemmas = Generics.NewArrayList();
			foreach (TaggedWord token in tagged)
			{
				CoreLabel label = new CoreLabel();
				label.SetWord(token.Word());
				label.SetTag(token.Tag());
				morpha.Stem(label);
				lemmas.Add(label);
			}
			return lemmas;
		}

		/// <summary>Parses the list of HasWord.</summary>
		/// <remarks>
		/// Parses the list of HasWord.  If the parse fails for some reason,
		/// an X tree is returned instead of barfing.
		/// </remarks>
		/// <param name="words">The input sentence (a List of words)</param>
		/// <returns>
		/// A Tree that is the parse tree for the sentence.  If the parser
		/// fails, a new Tree is synthesized which attaches all words to the
		/// root.
		/// </returns>
		public abstract Tree Parse<_T0>(IList<_T0> words)
			where _T0 : IHasWord;

		/// <summary>Returns a list of extra Eval objects to use when scoring the parser.</summary>
		public abstract IList<IEval> GetExtraEvals();

		/// <summary>
		/// Return a list of Eval-style objects which care about the whole
		/// ParserQuery, not just the finished tree
		/// </summary>
		public abstract IList<IParserQueryEval> GetParserQueryEvals();

		public abstract Options GetOp();

		public abstract ITreebankLangParserParams GetTLPParams();

		public abstract ITreebankLanguagePack TreebankLanguagePack();

		/// <summary>
		/// Returns a set of options which should be set by default when used
		/// in corenlp.
		/// </summary>
		/// <remarks>
		/// Returns a set of options which should be set by default when used
		/// in corenlp.  For example, the English PCFG/RNN models want
		/// -retainTmpSubcategories, and the ShiftReduceParser models may
		/// want -beamSize 4 depending on how they were trained.
		/// <br />
		/// TODO: right now completely hardcoded, should be settable as a training time option
		/// </remarks>
		public abstract string[] DefaultCoreNLPFlags();

		public abstract void SetOptionFlags(params string[] flags);

		/// <summary>The model requires text to be pretagged</summary>
		public abstract bool RequiresTags();

		public static ParserGrammar LoadModel(string path, params string[] extraFlags)
		{
			ParserGrammar parser;
			try
			{
				Timing timing = new Timing();
				parser = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(path);
				timing.Done(logger, "Loading parser from serialized file " + path);
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
			if (extraFlags.Length > 0)
			{
				parser.SetOptionFlags(extraFlags);
			}
			return parser;
		}
	}
}

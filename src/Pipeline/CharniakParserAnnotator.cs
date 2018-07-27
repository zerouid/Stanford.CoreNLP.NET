using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Charniak;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will add parse information to an Annotation from the BLLIP parser.</summary>
	/// <remarks>
	/// This class will add parse information to an Annotation from the BLLIP parser.
	/// It allows you to use the Charniak parser or Charniak and Johnson reranking parser
	/// along with any existing parser and reranking model.
	/// It assumes that the Annotation already contains the tokenized words
	/// as a
	/// <c>List&lt;List&lt;CoreLabel&gt;&gt;</c>
	/// under
	/// <c>CoreAnnotations.SentencesAnnotation.class</c>
	/// .
	/// If the words have POS tags, they will not be used.
	/// </remarks>
	/// <author>David McClosky</author>
	public class CharniakParserAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.CharniakParserAnnotator));

		private const bool BuildGraphs = true;

		private readonly IGrammaticalStructureFactory gsf = new EnglishGrammaticalStructureFactory();

		private readonly bool Verbose;

		private readonly CharniakParser parser;

		public CharniakParserAnnotator(string parserModel, string parserExecutable, bool verbose, int maxSentenceLength)
		{
			// TODO: make this an option?
			Verbose = verbose;
			parser = new CharniakParser(parserExecutable, parserModel);
			parser.SetMaxSentenceLength(maxSentenceLength);
		}

		public CharniakParserAnnotator()
		{
			Verbose = false;
			parser = new CharniakParser();
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				// parse a tree for each sentence
				foreach (ICoreMap sentence in annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					IList<CoreLabel> words = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					if (Verbose)
					{
						log.Info("Parsing: " + words);
					}
					int maxSentenceLength = parser.GetMaxSentenceLength();
					// generate the constituent tree
					Tree tree;
					// initialized below
					if (maxSentenceLength <= 0 || words.Count < maxSentenceLength)
					{
						tree = parser.GetBestParse(words);
					}
					else
					{
						tree = ParserUtils.XTree(words);
					}
					IList<Tree> trees = Generics.NewArrayList(1);
					trees.Add(tree);
					ParserAnnotatorUtils.FillInParseAnnotations(Verbose, BuildGraphs, gsf, sentence, trees, GrammaticalStructure.Extras.None);
				}
			}
			else
			{
				throw new Exception("unable to find sentences in: " + annotation);
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.SentencesAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(CoreAnnotations.CategoryAnnotation))));
		}
	}
}

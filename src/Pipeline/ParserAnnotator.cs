using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will add parse information to an Annotation.</summary>
	/// <remarks>
	/// This class will add parse information to an Annotation.
	/// It assumes that the Annotation already contains the tokenized words
	/// as a
	/// <c>List&lt;CoreLabel&gt;</c>
	/// in the TokensAnnotation under each
	/// particular CoreMap in the SentencesAnnotation.
	/// If the words have POS tags, they will be used.
	/// <br />
	/// Parse trees are added to each sentence's CoreMap (get with
	/// <c>CoreAnnotations.SentencesAnnotation</c>
	/// ) under
	/// <c>CoreAnnotations.TreeAnnotation</c>
	/// ).
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class ParserAnnotator : SentenceAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.ParserAnnotator));

		private readonly bool Verbose;

		private readonly bool BuildGraphs;

		private readonly ParserGrammar parser;

		private readonly IFunction<Tree, Tree> treeMap;

		/// <summary>Do not parse sentences larger than this sentence length</summary>
		private readonly int maxSentenceLength;

		/// <summary>Stop parsing if we exceed this time limit, in milliseconds.</summary>
		/// <remarks>
		/// Stop parsing if we exceed this time limit, in milliseconds.
		/// Use 0 for no limit.
		/// </remarks>
		private readonly long maxParseTime;

		private readonly int kBest;

		private readonly IGrammaticalStructureFactory gsf;

		private readonly int nThreads;

		private readonly bool saveBinaryTrees;

		/// <summary>Whether to include punctuation dependencies in the output.</summary>
		/// <remarks>Whether to include punctuation dependencies in the output. Starting in 2015, the default is true.</remarks>
		private readonly bool keepPunct;

		/// <summary>If true, don't re-annotate sentences that already have a tree annotation</summary>
		private readonly bool noSquash;

		private readonly GrammaticalStructure.Extras extraDependencies;

		public ParserAnnotator(bool verbose, int maxSent)
			: this(Runtime.GetProperty("parse.model", LexicalizedParser.DefaultParserLoc), verbose, maxSent, StringUtils.EmptyStringArray)
		{
		}

		public ParserAnnotator(string parserLoc, bool verbose, int maxSent, string[] flags)
			: this(LoadModel(parserLoc, verbose, flags), verbose, maxSent)
		{
		}

		public ParserAnnotator(ParserGrammar parser, bool verbose, int maxSent)
			: this(parser, verbose, maxSent, null)
		{
		}

		public ParserAnnotator(ParserGrammar parser, bool verbose, int maxSent, IFunction<Tree, Tree> treeMap)
		{
			this.Verbose = verbose;
			this.BuildGraphs = parser.GetTLPParams().SupportsBasicDependencies();
			this.parser = parser;
			this.maxSentenceLength = maxSent;
			this.treeMap = treeMap;
			this.maxParseTime = 0;
			this.kBest = 1;
			this.keepPunct = true;
			if (this.BuildGraphs)
			{
				ITreebankLanguagePack tlp = parser.GetTLPParams().TreebankLanguagePack();
				this.gsf = tlp.GrammaticalStructureFactory(tlp.PunctuationWordRejectFilter(), parser.GetTLPParams().TypedDependencyHeadFinder());
			}
			else
			{
				this.gsf = null;
			}
			this.nThreads = 1;
			this.saveBinaryTrees = false;
			this.noSquash = false;
			this.extraDependencies = GrammaticalStructure.Extras.None;
		}

		public ParserAnnotator(string annotatorName, Properties props)
		{
			string model = props.GetProperty(annotatorName + ".model", LexicalizedParser.DefaultParserLoc);
			if (model == null)
			{
				throw new ArgumentException("No model specified for Parser annotator " + annotatorName);
			}
			this.Verbose = PropertiesUtils.GetBool(props, annotatorName + ".debug", false);
			string[] flags = ConvertFlagsToArray(props.GetProperty(annotatorName + ".flags"));
			this.parser = LoadModel(model, Verbose, flags);
			this.maxSentenceLength = PropertiesUtils.GetInt(props, annotatorName + ".maxlen", -1);
			string treeMapClass = props.GetProperty(annotatorName + ".treemap");
			if (treeMapClass == null)
			{
				this.treeMap = null;
			}
			else
			{
				this.treeMap = ReflectionLoading.LoadByReflection(treeMapClass, props);
			}
			this.maxParseTime = PropertiesUtils.GetLong(props, annotatorName + ".maxtime", -1);
			this.kBest = PropertiesUtils.GetInt(props, annotatorName + ".kbest", 1);
			this.keepPunct = PropertiesUtils.GetBool(props, annotatorName + ".keepPunct", true);
			string buildGraphsProperty = annotatorName + ".buildgraphs";
			if (!this.parser.GetTLPParams().SupportsBasicDependencies())
			{
				if (PropertiesUtils.GetBool(props, buildGraphsProperty))
				{
					log.Info("WARNING: " + buildGraphsProperty + " set to true, but " + this.parser.GetTLPParams().GetType() + " does not support dependencies");
				}
				this.BuildGraphs = false;
			}
			else
			{
				this.BuildGraphs = PropertiesUtils.GetBool(props, buildGraphsProperty, true);
			}
			if (this.BuildGraphs)
			{
				bool generateOriginalDependencies = PropertiesUtils.GetBool(props, annotatorName + ".originalDependencies", false);
				parser.GetTLPParams().SetGenerateOriginalDependencies(generateOriginalDependencies);
				ITreebankLanguagePack tlp = parser.GetTLPParams().TreebankLanguagePack();
				IPredicate<string> punctFilter = this.keepPunct ? Filters.AcceptFilter() : tlp.PunctuationWordRejectFilter();
				this.gsf = tlp.GrammaticalStructureFactory(punctFilter, parser.GetTLPParams().TypedDependencyHeadFinder());
			}
			else
			{
				this.gsf = null;
			}
			this.nThreads = PropertiesUtils.GetInt(props, annotatorName + ".nthreads", PropertiesUtils.GetInt(props, "nthreads", 1));
			bool usesBinary = StanfordCoreNLP.UsesBinaryTrees(props);
			this.saveBinaryTrees = PropertiesUtils.GetBool(props, annotatorName + ".binaryTrees", usesBinary);
			this.noSquash = PropertiesUtils.GetBool(props, annotatorName + ".nosquash", false);
			this.extraDependencies = MetaClass.Cast(props.GetProperty(annotatorName + ".extradependencies", "NONE"), typeof(GrammaticalStructure.Extras));
		}

		public static string Signature(string annotatorName, Properties props)
		{
			StringBuilder os = new StringBuilder();
			os.Append(annotatorName + ".model:" + props.GetProperty(annotatorName + ".model", LexicalizedParser.DefaultParserLoc));
			os.Append(annotatorName + ".debug:" + props.GetProperty(annotatorName + ".debug", "false"));
			os.Append(annotatorName + ".flags:" + props.GetProperty(annotatorName + ".flags", string.Empty));
			os.Append(annotatorName + ".maxlen:" + props.GetProperty(annotatorName + ".maxlen", "-1"));
			os.Append(annotatorName + ".treemap:" + props.GetProperty(annotatorName + ".treemap", string.Empty));
			os.Append(annotatorName + ".maxtime:" + props.GetProperty(annotatorName + ".maxtime", "-1"));
			os.Append(annotatorName + ".originalDependencies:" + props.GetProperty(annotatorName + ".originalDependencies", "false"));
			os.Append(annotatorName + ".buildgraphs:" + props.GetProperty(annotatorName + ".buildgraphs", "true"));
			os.Append(annotatorName + ".nthreads:" + props.GetProperty(annotatorName + ".nthreads", props.GetProperty("nthreads", string.Empty)));
			os.Append(annotatorName + ".nosquash:" + props.GetProperty(annotatorName + ".nosquash", "false"));
			os.Append(annotatorName + ".keepPunct:" + props.GetProperty(annotatorName + ".keepPunct", "true"));
			os.Append(annotatorName + ".extradependencies:" + props.GetProperty(annotatorName + ".extradependencies", "NONE").ToLower());
			bool usesBinary = StanfordCoreNLP.UsesBinaryTrees(props);
			bool saveBinaryTrees = PropertiesUtils.GetBool(props, annotatorName + ".binaryTrees", usesBinary);
			os.Append(annotatorName + ".binaryTrees:" + saveBinaryTrees);
			return os.ToString();
		}

		private static string[] ConvertFlagsToArray(string parserFlags)
		{
			if (parserFlags == null || parserFlags.Trim().IsEmpty())
			{
				return StringUtils.EmptyStringArray;
			}
			else
			{
				return parserFlags.Trim().Split("\\s+");
			}
		}

		private static ParserGrammar LoadModel(string parserLoc, bool verbose, string[] flags)
		{
			if (verbose)
			{
				log.Info("Loading Parser Model [" + parserLoc + "] ...");
				log.Info("  Flags:");
				foreach (string flag in flags)
				{
					log.Info("  " + flag);
				}
				log.Info();
			}
			ParserGrammar result = ParserGrammar.LoadModel(parserLoc);
			result.SetOptionFlags(result.DefaultCoreNLPFlags());
			result.SetOptionFlags(flags);
			return result;
		}

		protected internal override int NThreads()
		{
			return nThreads;
		}

		protected internal override long MaxTime()
		{
			return maxParseTime;
		}

		protected internal override void DoOneSentence(Annotation annotation, ICoreMap sentence)
		{
			// If "noSquash" is set, don't re-annotate sentences which already have a tree annotation
			if (noSquash && sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation)) != null && !Sharpen.Runtime.EqualsIgnoreCase("X", sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation)).Label().Value()))
			{
				return;
			}
			IList<CoreLabel> words = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (Verbose)
			{
				log.Info("Parsing: " + words);
			}
			IList<Tree> trees = null;
			// generate the constituent tree
			if (maxSentenceLength <= 0 || words.Count <= maxSentenceLength)
			{
				try
				{
					IList<ParserConstraint> constraints = sentence.Get(typeof(ParserAnnotations.ConstraintAnnotation));
					trees = DoOneSentence(constraints, words);
				}
				catch (RuntimeInterruptedException)
				{
					if (Verbose)
					{
						log.Info("Took too long parsing: " + words);
					}
					trees = null;
				}
			}
			// tree == null may happen if the parser takes too long or if
			// the sentence is longer than the max length
			if (trees == null || trees.Count < 1)
			{
				DoOneFailedSentence(annotation, sentence);
			}
			else
			{
				FinishSentence(sentence, trees);
			}
		}

		protected internal override void DoOneFailedSentence(Annotation annotation, ICoreMap sentence)
		{
			IList<CoreLabel> words = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			Tree tree = ParserUtils.XTree(words);
			foreach (CoreLabel word in words)
			{
				if (word.Tag() == null)
				{
					word.SetTag("XX");
				}
			}
			IList<Tree> trees = Generics.NewArrayList(1);
			trees.Add(tree);
			FinishSentence(sentence, trees);
		}

		private void FinishSentence(ICoreMap sentence, IList<Tree> trees)
		{
			if (treeMap != null)
			{
				IList<Tree> mappedTrees = Generics.NewLinkedList();
				foreach (Tree tree in trees)
				{
					Tree mappedTree = treeMap.Apply(tree);
					mappedTrees.Add(mappedTree);
				}
				trees = mappedTrees;
			}
			ParserAnnotatorUtils.FillInParseAnnotations(Verbose, BuildGraphs, gsf, sentence, trees, extraDependencies);
			if (saveBinaryTrees)
			{
				TreeBinarizer binarizer = TreeBinarizer.SimpleTreeBinarizer(parser.GetTLPParams().HeadFinder(), parser.TreebankLanguagePack());
				Tree binarized = binarizer.TransformTree(trees[0]);
				Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(binarized);
				sentence.Set(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation), binarized);
			}
			// for some reason in some corner cases nodes aren't having sentenceIndex set
			// do a pass and make sure all nodes have sentenceIndex set
			SemanticGraph sg = sentence.Get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation));
			if (sg != null)
			{
				foreach (IndexedWord iw in sg.VertexSet())
				{
					if (iw.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)) == null && sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)) != null)
					{
						iw.SetSentIndex(sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
					}
				}
			}
		}

		private IList<Tree> DoOneSentence(IList<ParserConstraint> constraints, IList<CoreLabel> words)
		{
			IParserQuery pq = parser.ParserQuery();
			pq.SetConstraints(constraints);
			pq.Parse(words);
			IList<Tree> trees = Generics.NewLinkedList();
			try
			{
				// Use bestParse if kBest is set to 1.
				if (this.kBest == 1)
				{
					Tree t = pq.GetBestParse();
					if (t == null)
					{
						log.Warn("Parsing of sentence failed.  " + "Will ignore and continue: " + SentenceUtils.ListToString(words));
					}
					else
					{
						double score = pq.GetBestScore();
						t.SetScore(score % -10000.0);
						trees.Add(t);
					}
				}
				else
				{
					IList<ScoredObject<Tree>> scoredObjects = pq.GetKBestParses(this.kBest);
					if (scoredObjects == null || scoredObjects.Count < 1)
					{
						log.Warn("Parsing of sentence failed.  " + "Will ignore and continue: " + SentenceUtils.ListToString(words));
					}
					else
					{
						foreach (ScoredObject<Tree> so in scoredObjects)
						{
							// -10000 denotes unknown words
							Tree tree = so.Object();
							tree.SetScore(so.Score() % -10000.0);
							trees.Add(tree);
						}
					}
				}
			}
			catch (OutOfMemoryException e)
			{
				log.Error(e);
				// Beware that we can now get an OOM in logging, too.
				log.Warn("Parsing of sentence ran out of memory (length=" + words.Count + ").  " + "Will ignore and try to continue.");
			}
			catch (NoSuchParseException)
			{
				log.Warn("Parsing of sentence failed, possibly because of out of memory.  " + "Will ignore and continue: " + SentenceUtils.ListToString(words));
			}
			return trees;
		}

		public override ICollection<Type> Requires()
		{
			if (parser.RequiresTags())
			{
				return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
					), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation
					), typeof(CoreAnnotations.PartOfSpeechAnnotation))));
			}
			else
			{
				return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
					), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation
					))));
			}
		}

		public override ICollection<Type> RequirementsSatisfied()
		{
			if (this.BuildGraphs)
			{
				if (this.saveBinaryTrees)
				{
					return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(TreeCoreAnnotations.BinarizedTreeAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation
						), typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation
						), typeof(CoreAnnotations.BeginIndexAnnotation), typeof(CoreAnnotations.EndIndexAnnotation), typeof(CoreAnnotations.CategoryAnnotation))));
				}
				else
				{
					return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(
						SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation
						), typeof(CoreAnnotations.BeginIndexAnnotation), typeof(CoreAnnotations.EndIndexAnnotation), typeof(CoreAnnotations.CategoryAnnotation))));
				}
			}
			else
			{
				if (this.saveBinaryTrees)
				{
					return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(TreeCoreAnnotations.BinarizedTreeAnnotation), typeof(CoreAnnotations.CategoryAnnotation
						))));
				}
				else
				{
					return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(TreeCoreAnnotations.TreeAnnotation), typeof(CoreAnnotations.CategoryAnnotation))));
				}
			}
		}
	}
}

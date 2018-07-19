using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Machinereading.Common;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <author>Andrey Gusev</author>
	/// <author>Mihai</author>
	public class GenericDataSetReader
	{
		/// <summary>A logger for this class</summary>
		protected internal Logger logger;

		/// <summary>Finds the syntactic head of a syntactic constituent</summary>
		protected internal readonly IHeadFinder headFinder = new NoPunctuationHeadFinder();

		/// <summary>NL processor to use for sentence pre-processing</summary>
		protected internal StanfordCoreNLP processor;

		/// <summary>
		/// Additional NL processor that implements only syntactic parsing (needed for head detection)
		/// We need this processor to detect heads of predicted entities that cannot be matched to an existing constituent.
		/// </summary>
		/// <remarks>
		/// Additional NL processor that implements only syntactic parsing (needed for head detection)
		/// We need this processor to detect heads of predicted entities that cannot be matched to an existing constituent.
		/// This is created on demand, only when necessary
		/// </remarks>
		protected internal IAnnotator parserProcessor;

		/// <summary>If true, we perform syntactic analysis of the dataset sentences and annotations</summary>
		protected internal readonly bool preProcessSentences;

		/// <summary>If true, sets the head span to match the syntactic head of the extent.</summary>
		/// <remarks>
		/// If true, sets the head span to match the syntactic head of the extent.
		/// Otherwise, the head span is not modified.
		/// This is enabled for the NFL domain, where head spans are not given.
		/// </remarks>
		protected internal readonly bool calculateHeadSpan;

		/// <summary>If true, it regenerates the index spans for all tree nodes (useful for KBP)</summary>
		protected internal readonly bool forceGenerationOfIndexSpans;

		/// <summary>Only around for legacy results</summary>
		protected internal bool useNewHeadFinder = true;

		public GenericDataSetReader()
			: this(null, false, false, false)
		{
		}

		public GenericDataSetReader(StanfordCoreNLP processor, bool preProcessSentences, bool calculateHeadSpan, bool forceGenerationOfIndexSpans)
		{
			// import edu.stanford.nlp.util.logging.Redwood;
			// private static Redwood.RedwoodChannels log = Redwood.channels(GenericDataSetReader.class);
			this.logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.GenericDataSetReader).FullName);
			this.logger.SetLevel(Level.Severe);
			if (processor != null)
			{
				SetProcessor(processor);
			}
			parserProcessor = null;
			/* old parser options
			parser.setOptionFlags(new String[] {
			"-outputFormat", "penn,typedDependenciesCollapsed",
			"-maxLength", "100",
			"-retainTmpSubcategories"
			});
			*/
			this.preProcessSentences = preProcessSentences;
			this.calculateHeadSpan = calculateHeadSpan;
			this.forceGenerationOfIndexSpans = forceGenerationOfIndexSpans;
		}

		public virtual void SetProcessor(StanfordCoreNLP p)
		{
			this.processor = p;
		}

		public virtual void SetUseNewHeadFinder(bool useNewHeadFinder)
		{
			this.useNewHeadFinder = useNewHeadFinder;
		}

		public virtual IAnnotator GetParser()
		{
			if (parserProcessor == null)
			{
				parserProcessor = StanfordCoreNLP.GetExistingAnnotator("parse");
				System.Diagnostics.Debug.Assert((parserProcessor != null));
			}
			return parserProcessor;
		}

		public virtual void SetLoggerLevel(Level level)
		{
			logger.SetLevel(level);
		}

		public virtual Level GetLoggerLevel()
		{
			return logger.GetLevel();
		}

		/// <summary>Parses one file or directory with data from one domain</summary>
		/// <param name="path"/>
		/// <exception cref="System.IO.IOException"/>
		public Annotation Parse(string path)
		{
			Annotation retVal;
			// set below or exceptions
			try
			{
				//
				// this must return a dataset Annotation. each sentence in this dataset must contain:
				// - TokensAnnotation
				// - EntityMentionAnnotation
				// - RelationMentionAnnotation
				// - EventMentionAnnotation
				// the other annotations (parse, NER) are generated in preProcessSentences
				//
				retVal = this.Read(path);
			}
			catch (Exception ex)
			{
				IOException iox = new IOException(ex);
				throw iox;
			}
			if (preProcessSentences)
			{
				PreProcessSentences(retVal);
				if (MachineReadingProperties.trainUsePipelineNER)
				{
					logger.Severe("Changing NER tags using the CoreNLP pipeline.");
					ModifyUsingCoreNLPNER(retVal);
				}
			}
			return retVal;
		}

		private static void ModifyUsingCoreNLPNER(Annotation doc)
		{
			Properties ann = new Properties();
			ann.SetProperty("annotators", "pos, lemma, ner");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(ann, false);
			pipeline.Annotate(doc);
			foreach (ICoreMap sentence in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<EntityMention> entities = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
				if (entities != null)
				{
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					foreach (EntityMention en in entities)
					{
						//System.out.println("old ner tag for " + en.getExtentString() + " was " + en.getType());
						Span s = en.GetExtent();
						ICounter<string> allNertagforSpan = new ClassicCounter<string>();
						for (int i = s.Start(); i < s.End(); i++)
						{
							allNertagforSpan.IncrementCount(tokens[i].Ner());
						}
						string entityNertag = Counters.Argmax(allNertagforSpan);
						en.SetType(entityNertag);
					}
				}
			}
		}

		//System.out.println("new ner tag is " + entityNertag);
		/// <exception cref="System.Exception"/>
		public virtual Annotation Read(string path)
		{
			return null;
		}

		private static string SentenceToString(IList<CoreLabel> tokens)
		{
			StringBuilder os = new StringBuilder();
			//
			// Print text and tokens
			//
			if (tokens != null)
			{
				bool first = true;
				foreach (CoreLabel token in tokens)
				{
					if (!first)
					{
						os.Append(" ");
					}
					os.Append(token.Word());
					first = false;
				}
			}
			return os.ToString();
		}

		/// <summary>Find the index of the head of an entity.</summary>
		/// <param name="ent">The entity mention</param>
		/// <param name="tree">The Tree for the entire sentence in which it occurs.</param>
		/// <param name="tokens">The Sentence in which it occurs</param>
		/// <param name="setHeadSpan">Whether to set the head span in the entity mention.</param>
		/// <returns>The index of the entity head</returns>
		public virtual int AssignSyntacticHead(EntityMention ent, Tree tree, IList<CoreLabel> tokens, bool setHeadSpan)
		{
			if (ent.GetSyntacticHeadTokenPosition() != -1)
			{
				return ent.GetSyntacticHeadTokenPosition();
			}
			logger.Finest("Finding syntactic head for entity: " + ent + " in tree: " + tree.ToString());
			logger.Finest("Flat sentence is: " + tokens);
			Tree sh = null;
			try
			{
				sh = FindSyntacticHead(ent, tree, tokens);
			}
			catch (Exception e)
			{
				logger.Severe("WARNING: failed to parse sentence. Will continue with the right-most head heuristic: " + SentenceToString(tokens));
				Sharpen.Runtime.PrintStackTrace(e);
			}
			int headPos = ent.GetExtentTokenEnd() - 1;
			if (sh != null)
			{
				CoreLabel label = (CoreLabel)sh.Label();
				headPos = label.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
			}
			else
			{
				logger.Fine("WARNING: failed to find syntactic head for entity: " + ent + " in tree: " + tree);
				logger.Fine("Fallback strategy: will set head to last token in mention: " + tokens[headPos]);
			}
			ent.SetHeadTokenPosition(headPos);
			if (setHeadSpan)
			{
				// set the head span to match exactly the syntactic head
				// this is needed for some corpora where the head span is not given
				ent.SetHeadTokenSpan(new Span(headPos, headPos + 1));
			}
			return headPos;
		}

		/// <summary>Take a dataset Annotation, generate their parse trees and identify syntactic heads (and head spans, if necessary)</summary>
		public virtual void PreProcessSentences(Annotation dataset)
		{
			logger.Severe("GenericDataSetReader: Started pre-processing the corpus...");
			// run the processor, i.e., NER, parse etc.
			if (processor != null)
			{
				// we might already have syntactic annotation from offline files
				IList<ICoreMap> sentences = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
				if (sentences.Count > 0 && !sentences[0].ContainsKey(typeof(TreeCoreAnnotations.TreeAnnotation)))
				{
					logger.Info("Annotating dataset with " + processor);
					processor.Annotate(dataset);
				}
				else
				{
					logger.Info("Found existing syntactic annotations. Will not use the NLP processor.");
				}
			}
			/*
			List<CoreMap> sentences = dataset.get(CoreAnnotations.SentencesAnnotation.class);
			for(int i = 0; i < sentences.size(); i ++){
			CoreMap sent = sentences.get(i);
			List<CoreLabel> tokens = sent.get(CoreAnnotations.TokensAnnotation.class);
			logger.info("Tokens for sentence #" + i + ": " + tokens);
			logger.info("Parse tree for sentence #" + i + ": " + sent.get(TreeCoreAnnotations.TreeAnnotation.class).pennString());
			}
			*/
			IList<ICoreMap> sentences_1 = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			logger.Fine("Extracted " + sentences_1.Count + " sentences.");
			foreach (ICoreMap sentence in sentences_1)
			{
				IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				logger.Fine("Processing sentence " + tokens);
				Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				if (tree == null)
				{
					throw new Exception("ERROR: MR requires full syntactic analysis!");
				}
				// convert tree labels to CoreLabel if necessary
				// we need this because we store additional info in the CoreLabel, such as the spans of each tree
				ConvertToCoreLabels(tree);
				// store the tree spans, if not present already
				CoreLabel l = (CoreLabel)tree.Label();
				if (forceGenerationOfIndexSpans || (!l.ContainsKey(typeof(CoreAnnotations.BeginIndexAnnotation)) && !l.ContainsKey(typeof(CoreAnnotations.EndIndexAnnotation))))
				{
					tree.IndexSpans(0);
					logger.Fine("Index spans were generated.");
				}
				else
				{
					logger.Fine("Index spans were NOT generated.");
				}
				logger.Fine("Parse tree using CoreLabel:\n" + tree.PennString());
				//
				// now match all entity mentions against the syntactic tree
				//
				if (sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)) != null)
				{
					foreach (EntityMention ent in sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)))
					{
						logger.Fine("Finding head for entity: " + ent);
						int headPos = AssignSyntacticHead(ent, tree, tokens, calculateHeadSpan);
						logger.Fine("Syntactic head of mention \"" + ent + "\" is: " + tokens[headPos].Word());
						System.Diagnostics.Debug.Assert((ent.GetExtent() != null));
						System.Diagnostics.Debug.Assert((ent.GetHead() != null));
						System.Diagnostics.Debug.Assert((ent.GetSyntacticHeadTokenPosition() >= 0));
					}
				}
			}
			logger.Severe("GenericDataSetReader: Pre-processing complete.");
		}

		/// <summary>Converts the tree labels to CoreLabels.</summary>
		/// <remarks>
		/// Converts the tree labels to CoreLabels.
		/// We need this because we store additional info in the CoreLabel, like token span.
		/// </remarks>
		/// <param name="tree"/>
		public static void ConvertToCoreLabels(Tree tree)
		{
			ILabel l = tree.Label();
			if (!(l is CoreLabel))
			{
				CoreLabel cl = new CoreLabel();
				cl.SetValue(l.Value());
				tree.SetLabel(cl);
			}
			foreach (Tree kid in tree.Children())
			{
				ConvertToCoreLabels(kid);
			}
		}

		private static string PrintTree(Tree tree)
		{
			StringBuilder sb = new StringBuilder();
			return tree.ToStringBuilder(sb).ToString();
		}

		private Tree SafeHead(Tree top)
		{
			Tree head = top.HeadTerminal(headFinder);
			if (head != null)
			{
				return head;
			}
			// if no head found return the right-most leaf
			IList<Tree> leaves = top.GetLeaves();
			if (!leaves.IsEmpty())
			{
				return leaves[leaves.Count - 1];
			}
			// fallback: return top
			return top;
		}

		/// <summary>Finds the syntactic head of the given entity mention.</summary>
		/// <param name="ent">The entity mention</param>
		/// <param name="root">The Tree for the entire sentence in which it occurs.</param>
		/// <param name="tokens">The Sentence in which it occurs</param>
		/// <returns>
		/// The tree object corresponding to the head. This MUST be a child of root.
		/// It will be a leaf in the parse tree.
		/// </returns>
		public virtual Tree FindSyntacticHead(EntityMention ent, Tree root, IList<CoreLabel> tokens)
		{
			if (!useNewHeadFinder)
			{
				return OriginalFindSyntacticHead(ent, root, tokens);
			}
			logger.Fine("Searching for tree matching " + ent);
			Tree exactMatch = FindTreeWithSpan(root, ent.GetExtentTokenStart(), ent.GetExtentTokenEnd());
			//
			// found an exact match
			//
			if (exactMatch != null)
			{
				logger.Fine("Mention \"" + ent + "\" mapped to tree: " + PrintTree(exactMatch));
				return SafeHead(exactMatch);
			}
			// no exact match found
			// in this case, we parse the actual extent of the mention, embedded in a sentence
			// context, so as to make the parser work better :-)
			int approximateness = 0;
			IList<CoreLabel> extentTokens = new List<CoreLabel>();
			extentTokens.Add(InitCoreLabel("It"));
			extentTokens.Add(InitCoreLabel("was"));
			int AddedWords = 2;
			for (int i = ent.GetExtentTokenStart(); i < ent.GetExtentTokenEnd(); i++)
			{
				// Add everything except separated dashes! The separated dashes mess with the parser too badly.
				CoreLabel label = tokens[i];
				if (!"-".Equals(label.Word()))
				{
					extentTokens.Add(tokens[i]);
				}
				else
				{
					approximateness++;
				}
			}
			extentTokens.Add(InitCoreLabel("."));
			// constrain the parse to the part we're interested in.
			// Starting from ADDED_WORDS comes from skipping "It was".
			// -1 to exclude the period.
			// We now let it be any kind of nominal constituent, since there
			// are VP and S ones
			ParserConstraint constraint = new ParserConstraint(AddedWords, extentTokens.Count - 1, ".*");
			IList<ParserConstraint> constraints = Java.Util.Collections.SingletonList(constraint);
			Tree tree = Parse(extentTokens, constraints);
			logger.Fine("No exact match found. Local parse:\n" + tree.PennString());
			ConvertToCoreLabels(tree);
			tree.IndexSpans(ent.GetExtentTokenStart() - AddedWords);
			// remember it has ADDED_WORDS extra words at the beginning
			Tree subtree = FindPartialSpan(tree, ent.GetExtentTokenStart());
			Tree extentHead = SafeHead(subtree);
			logger.Fine("Head is: " + extentHead);
			System.Diagnostics.Debug.Assert((extentHead != null));
			// extentHead is a child in the local extent parse tree. we need to find the corresponding node in the main tree
			// Because we deleted dashes, it's index will be >= the index in the extent parse tree
			CoreLabel l = (CoreLabel)extentHead.Label();
			// Tree realHead = findTreeWithSpan(root, l.get(CoreAnnotations.BeginIndexAnnotation.class), l.get(CoreAnnotations.EndIndexAnnotation.class));
			Tree realHead = FunkyFindLeafWithApproximateSpan(root, l.Value(), l.Get(typeof(CoreAnnotations.BeginIndexAnnotation)), approximateness);
			if (realHead != null)
			{
				logger.Fine("Chosen head: " + realHead);
			}
			return realHead;
		}

		private Tree FindPartialSpan(Tree current, int start)
		{
			CoreLabel label = (CoreLabel)current.Label();
			int startIndex = label.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
			if (startIndex == start)
			{
				logger.Fine("findPartialSpan: Returning " + current);
				return current;
			}
			foreach (Tree kid in current.Children())
			{
				CoreLabel kidLabel = (CoreLabel)kid.Label();
				int kidStart = kidLabel.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
				int kidEnd = kidLabel.Get(typeof(CoreAnnotations.EndIndexAnnotation));
				// log.info("findPartialSpan: Examining " + kidLabel.value() + " from " + kidStart + " to " + kidEnd);
				if (kidStart <= start && kidEnd > start)
				{
					return FindPartialSpan(kid, start);
				}
			}
			throw new Exception("Shouldn't happen: " + start + " " + current);
		}

		private Tree FunkyFindLeafWithApproximateSpan(Tree root, string token, int index, int approximateness)
		{
			logger.Fine("Looking for " + token + " at pos " + index + " plus upto " + approximateness + " in tree: " + root.PennString());
			IList<Tree> leaves = root.GetLeaves();
			foreach (Tree leaf in leaves)
			{
				CoreLabel label = typeof(CoreLabel).Cast(leaf.Label());
				int ind = label.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
				// log.info("Token #" + ind + ": " + leaf.value());
				if (token.Equals(leaf.Value()) && ind >= index && ind <= index + approximateness)
				{
					return leaf;
				}
			}
			// this shouldn't happen
			// but it does happen (VERY RARELY) on some weird web text that includes SGML tags with spaces
			// TODO: does this mean that somehow tokenization is different for the parser? check this by throwing an Exception in KBP
			logger.Severe("GenericDataSetReader: WARNING: Failed to find head token");
			logger.Severe("  when looking for " + token + " at pos " + index + " plus upto " + approximateness + " in tree: " + root.PennString());
			return null;
		}

		/// <summary>
		/// This is the original version of
		/// <see cref="FindSyntacticHead(Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention, Edu.Stanford.Nlp.Trees.Tree, System.Collections.Generic.IList{E})"/>
		/// before Chris's modifications.
		/// There's no good reason to use it except for producing historical results.
		/// It Finds the syntactic head of the given entity mention.
		/// </summary>
		/// <param name="ent">The entity mention</param>
		/// <param name="root">The Tree for the entire sentence in which it occurs.</param>
		/// <param name="tokens">The Sentence in which it occurs</param>
		/// <returns>
		/// The tree object corresponding to the head. This MUST be a child of root.
		/// It will be a leaf in the parse tree.
		/// </returns>
		public virtual Tree OriginalFindSyntacticHead(EntityMention ent, Tree root, IList<CoreLabel> tokens)
		{
			logger.Fine("Searching for tree matching " + ent);
			Tree exactMatch = FindTreeWithSpan(root, ent.GetExtentTokenStart(), ent.GetExtentTokenEnd());
			//
			// found an exact match
			//
			if (exactMatch != null)
			{
				logger.Fine("Mention \"" + ent + "\" mapped to tree: " + PrintTree(exactMatch));
				return SafeHead(exactMatch);
			}
			//
			// no exact match found
			// in this case, we parse the actual extent of the mention
			//
			IList<CoreLabel> extentTokens = new List<CoreLabel>();
			for (int i = ent.GetExtentTokenStart(); i < ent.GetExtentTokenEnd(); i++)
			{
				extentTokens.Add(tokens[i]);
			}
			Tree tree = Parse(extentTokens);
			logger.Fine("No exact match found. Local parse:\n" + tree.PennString());
			ConvertToCoreLabels(tree);
			tree.IndexSpans(ent.GetExtentTokenStart());
			Tree extentHead = SafeHead(tree);
			System.Diagnostics.Debug.Assert((extentHead != null));
			// extentHead is a child in the local extent parse tree. we need to find the
			// corresponding node in the main tree
			CoreLabel l = (CoreLabel)extentHead.Label();
			Tree realHead = FindTreeWithSpan(root, l.Get(typeof(CoreAnnotations.BeginIndexAnnotation)), l.Get(typeof(CoreAnnotations.EndIndexAnnotation)));
			System.Diagnostics.Debug.Assert((realHead != null));
			return realHead;
		}

		private static CoreLabel InitCoreLabel(string token)
		{
			CoreLabel label = new CoreLabel();
			label.SetWord(token);
			label.SetValue(token);
			label.Set(typeof(CoreAnnotations.TextAnnotation), token);
			label.Set(typeof(CoreAnnotations.ValueAnnotation), token);
			return label;
		}

		protected internal virtual Tree ParseStrings(IList<string> tokens)
		{
			IList<CoreLabel> labels = new List<CoreLabel>();
			foreach (string t in tokens)
			{
				CoreLabel l = InitCoreLabel(t);
				labels.Add(l);
			}
			return Parse(labels);
		}

		protected internal virtual Tree Parse(IList<CoreLabel> tokens)
		{
			return Parse(tokens, null);
		}

		protected internal virtual Tree Parse(IList<CoreLabel> tokens, IList<ParserConstraint> constraints)
		{
			ICoreMap sent = new Annotation(string.Empty);
			sent.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			sent.Set(typeof(ParserAnnotations.ConstraintAnnotation), constraints);
			Annotation doc = new Annotation(string.Empty);
			IList<ICoreMap> sents = new List<ICoreMap>();
			sents.Add(sent);
			doc.Set(typeof(CoreAnnotations.SentencesAnnotation), sents);
			GetParser().Annotate(doc);
			sents = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sents[0].Get(typeof(TreeCoreAnnotations.TreeAnnotation));
		}

		/// <summary>Finds the tree with the given token span.</summary>
		/// <remarks>
		/// Finds the tree with the given token span.
		/// The tree must have CoreLabel labels and Tree.indexSpans must be called before this method.
		/// </remarks>
		/// <param name="tree">The tree to search in</param>
		/// <param name="start">The beginning index</param>
		/// <param name="end"/>
		/// <returns>A child of tree if match; otherwise null</returns>
		private static Tree FindTreeWithSpan(Tree tree, int start, int end)
		{
			CoreLabel l = (CoreLabel)tree.Label();
			if (l != null && l.ContainsKey(typeof(CoreAnnotations.BeginIndexAnnotation)) && l.ContainsKey(typeof(CoreAnnotations.EndIndexAnnotation)))
			{
				int myStart = l.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
				int myEnd = l.Get(typeof(CoreAnnotations.EndIndexAnnotation));
				if (start == myStart && end == myEnd)
				{
					// found perfect match
					return tree;
				}
				else
				{
					if (end < myStart)
					{
						return null;
					}
					else
					{
						if (start >= myEnd)
						{
							return null;
						}
					}
				}
			}
			// otherwise, check inside children - a match is possible
			foreach (Tree kid in tree.Children())
			{
				if (kid == null)
				{
					continue;
				}
				Tree ret = FindTreeWithSpan(kid, start, end);
				// found matching child
				if (ret != null)
				{
					return ret;
				}
			}
			// no match
			return null;
		}
	}
}

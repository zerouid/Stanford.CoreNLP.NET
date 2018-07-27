using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Dcoref
{
	public class RuleBasedCorefMentionFinder : ICorefMentionFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Dcoref.RuleBasedCorefMentionFinder));

		protected internal bool assignIds = true;

		private readonly IHeadFinder headFinder;

		protected internal IAnnotator parserProcessor;

		private readonly bool allowReparsing;

		public RuleBasedCorefMentionFinder()
			: this(Constants.AllowReparsing)
		{
		}

		public RuleBasedCorefMentionFinder(bool allowReparsing)
		{
			//  protected int maxID = -1;
			SieveCoreferenceSystem.logger.Fine("Using SEMANTIC HEAD FINDER!!!!!!!!!!!!!!!!!!!");
			this.headFinder = new SemanticHeadFinder();
			this.allowReparsing = allowReparsing;
		}

		/// <summary>When mention boundaries are given</summary>
		public virtual IList<IList<Mention>> FilterPredictedMentions(IList<IList<Mention>> allGoldMentions, Annotation doc, Dictionaries dict)
		{
			IList<IList<Mention>> predictedMentions = new List<IList<Mention>>();
			for (int i = 0; i < allGoldMentions.Count; i++)
			{
				ICoreMap s = doc.Get(typeof(CoreAnnotations.SentencesAnnotation))[i];
				IList<Mention> goldMentions = allGoldMentions[i];
				IList<Mention> mentions = new List<Mention>();
				predictedMentions.Add(mentions);
				Sharpen.Collections.AddAll(mentions, goldMentions);
				FindHead(s, mentions);
				// todo [cdm 2013]: This block seems to do nothing - the two sets are never used
				ICollection<IntPair> mentionSpanSet = Generics.NewHashSet();
				ICollection<IntPair> namedEntitySpanSet = Generics.NewHashSet();
				foreach (Mention m in mentions)
				{
					mentionSpanSet.Add(new IntPair(m.startIndex, m.endIndex));
					if (!m.headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals("O"))
					{
						namedEntitySpanSet.Add(new IntPair(m.startIndex, m.endIndex));
					}
				}
				SetBarePlural(mentions);
				RemoveSpuriousMentions(s, mentions, dict);
			}
			return predictedMentions;
		}

		/// <summary>Main method of mention detection.</summary>
		/// <remarks>
		/// Main method of mention detection.
		/// Extract all NP, PRP or NE, and filter out by manually written patterns.
		/// </remarks>
		public virtual IList<IList<Mention>> ExtractPredictedMentions(Annotation doc, int maxID, Dictionaries dict)
		{
			//    this.maxID = _maxID;
			IList<IList<Mention>> predictedMentions = new List<IList<Mention>>();
			foreach (ICoreMap s in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				IList<Mention> mentions = new List<Mention>();
				predictedMentions.Add(mentions);
				ICollection<IntPair> mentionSpanSet = Generics.NewHashSet();
				ICollection<IntPair> namedEntitySpanSet = Generics.NewHashSet();
				ExtractPremarkedEntityMentions(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractNamedEntityMentions(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractNPorPRP(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractEnumerations(s, mentions, mentionSpanSet, namedEntitySpanSet);
				FindHead(s, mentions);
				SetBarePlural(mentions);
				RemoveSpuriousMentions(s, mentions, dict);
			}
			// assign mention IDs
			if (assignIds)
			{
				AssignMentionIDs(predictedMentions, maxID);
			}
			return predictedMentions;
		}

		protected internal static void AssignMentionIDs(IList<IList<Mention>> predictedMentions, int maxID)
		{
			foreach (IList<Mention> mentions in predictedMentions)
			{
				foreach (Mention m in mentions)
				{
					m.mentionID = (++maxID);
				}
			}
		}

		protected internal static void SetBarePlural(IList<Mention> mentions)
		{
			foreach (Mention m in mentions)
			{
				string pos = m.headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				if (m.originalSpan.Count == 1 && pos.Equals("NNS"))
				{
					m.generic = true;
				}
			}
		}

		protected internal static void ExtractPremarkedEntityMentions(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph dependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			int beginIndex = -1;
			foreach (CoreLabel w in sent)
			{
				MultiTokenTag t = w.Get(typeof(CoreAnnotations.MentionTokenAnnotation));
				if (t != null)
				{
					// Part of a mention
					if (t.IsStart())
					{
						// Start of mention
						beginIndex = w.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					}
					if (t.IsEnd())
					{
						// end of mention
						int endIndex = w.Get(typeof(CoreAnnotations.IndexAnnotation));
						if (beginIndex >= 0)
						{
							IntPair mSpan = new IntPair(beginIndex, endIndex);
							int dummyMentionId = -1;
							Mention m = new Mention(dummyMentionId, beginIndex, endIndex, dependency, new List<CoreLabel>(sent.SubList(beginIndex, endIndex)));
							mentions.Add(m);
							mentionSpanSet.Add(mSpan);
							beginIndex = -1;
						}
						else
						{
							SieveCoreferenceSystem.logger.Warning("Start of marked mention not found in sentence: " + t + " at tokenIndex=" + (w.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1) + " for " + s.Get(typeof(CoreAnnotations.TextAnnotation)));
						}
					}
				}
			}
		}

		protected internal static void ExtractNamedEntityMentions(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph dependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			string preNE = "O";
			int beginIndex = -1;
			foreach (CoreLabel w in sent)
			{
				string nerString = w.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				if (!nerString.Equals(preNE))
				{
					int endIndex = w.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					if (!preNE.Matches("O|QUANTITY|CARDINAL|PERCENT|DATE|DURATION|TIME|SET"))
					{
						if (w.Get(typeof(CoreAnnotations.TextAnnotation)).Equals("'s"))
						{
							endIndex++;
						}
						IntPair mSpan = new IntPair(beginIndex, endIndex);
						// Need to check if beginIndex < endIndex because, for
						// example, there could be a 's mislabeled by the NER and
						// attached to the previous NER by the earlier heuristic
						if (beginIndex < endIndex && !mentionSpanSet.Contains(mSpan))
						{
							int dummyMentionId = -1;
							Mention m = new Mention(dummyMentionId, beginIndex, endIndex, dependency, new List<CoreLabel>(sent.SubList(beginIndex, endIndex)));
							mentions.Add(m);
							mentionSpanSet.Add(mSpan);
							namedEntitySpanSet.Add(mSpan);
						}
					}
					beginIndex = endIndex;
					preNE = nerString;
				}
			}
			// NE at the end of sentence
			if (!preNE.Matches("O|QUANTITY|CARDINAL|PERCENT|DATE|DURATION|TIME|SET"))
			{
				IntPair mSpan = new IntPair(beginIndex, sent.Count);
				if (!mentionSpanSet.Contains(mSpan))
				{
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, beginIndex, sent.Count, dependency, new List<CoreLabel>(sent.SubList(beginIndex, sent.Count)));
					mentions.Add(m);
					mentionSpanSet.Add(mSpan);
					namedEntitySpanSet.Add(mSpan);
				}
			}
		}

		private static readonly TregexPattern npOrPrpMentionPattern = TregexPattern.Compile("/^(?:NP|PRP)/");

		protected internal static void ExtractNPorPRP(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			tree.IndexLeaves();
			SemanticGraph dependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			TregexPattern tgrepPattern = npOrPrpMentionPattern;
			TregexMatcher matcher = tgrepPattern.Matcher(tree);
			while (matcher.Find())
			{
				Tree t = matcher.GetMatch();
				IList<Tree> mLeaves = t.GetLeaves();
				int beginIdx = ((CoreLabel)mLeaves[0].Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
				int endIdx = ((CoreLabel)mLeaves[mLeaves.Count - 1].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				if (",".Equals(sent[endIdx - 1].Word()))
				{
					endIdx--;
				}
				// try not to have span that ends with ,
				IntPair mSpan = new IntPair(beginIdx, endIdx);
				if (!mentionSpanSet.Contains(mSpan) && !InsideNE(mSpan, namedEntitySpanSet))
				{
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, beginIdx, endIdx, dependency, new List<CoreLabel>(sent.SubList(beginIdx, endIdx)), t);
					mentions.Add(m);
					mentionSpanSet.Add(mSpan);
				}
			}
		}

		/// <summary>Extract enumerations (A, B, and C)</summary>
		private static readonly TregexPattern enumerationsMentionPattern = TregexPattern.Compile("NP < (/^(?:NP|NNP|NML)/=m1 $.. (/^CC|,/ $.. /^(?:NP|NNP|NML)/=m2))");

		protected internal static void ExtractEnumerations(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			SemanticGraph dependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			TregexPattern tgrepPattern = enumerationsMentionPattern;
			TregexMatcher matcher = tgrepPattern.Matcher(tree);
			IDictionary<IntPair, Tree> spanToMentionSubTree = Generics.NewHashMap();
			while (matcher.Find())
			{
				matcher.GetMatch();
				Tree m1 = matcher.GetNode("m1");
				Tree m2 = matcher.GetNode("m2");
				IList<Tree> mLeaves = m1.GetLeaves();
				int beginIdx = ((CoreLabel)mLeaves[0].Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
				int endIdx = ((CoreLabel)mLeaves[mLeaves.Count - 1].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				spanToMentionSubTree[new IntPair(beginIdx, endIdx)] = m1;
				mLeaves = m2.GetLeaves();
				beginIdx = ((CoreLabel)mLeaves[0].Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
				endIdx = ((CoreLabel)mLeaves[mLeaves.Count - 1].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				spanToMentionSubTree[new IntPair(beginIdx, endIdx)] = m2;
			}
			foreach (IntPair mSpan in spanToMentionSubTree.Keys)
			{
				if (!mentionSpanSet.Contains(mSpan) && !InsideNE(mSpan, namedEntitySpanSet))
				{
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, mSpan.Get(0), mSpan.Get(1), dependency, new List<CoreLabel>(sent.SubList(mSpan.Get(0), mSpan.Get(1))), spanToMentionSubTree[mSpan]);
					mentions.Add(m);
					mentionSpanSet.Add(mSpan);
				}
			}
		}

		/// <summary>Check whether a mention is inside of a named entity</summary>
		private static bool InsideNE(IntPair mSpan, ICollection<IntPair> namedEntitySpanSet)
		{
			foreach (IntPair span in namedEntitySpanSet)
			{
				if (span.Get(0) <= mSpan.Get(0) && mSpan.Get(1) <= span.Get(1))
				{
					return true;
				}
			}
			return false;
		}

		protected internal virtual void FindHead(ICoreMap s, IList<Mention> mentions)
		{
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			tree.IndexSpans(0);
			foreach (Mention m in mentions)
			{
				Tree head = FindSyntacticHead(m, tree, sent);
				m.headIndex = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
				m.headWord = sent[m.headIndex];
				m.headString = m.headWord.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower(Locale.English);
				int start = m.headIndex - m.startIndex;
				if (start < 0 || start >= m.originalSpan.Count)
				{
					SieveCoreferenceSystem.logger.Warning("Invalid index for head " + start + "=" + m.headIndex + "-" + m.startIndex + ": originalSpan=[" + StringUtils.JoinWords(m.originalSpan, " ") + "], head=" + m.headWord);
					SieveCoreferenceSystem.logger.Warning("Setting head string to entire mention");
					m.headIndex = m.startIndex;
					m.headWord = m.originalSpan.Count > 0 ? m.originalSpan[0] : sent[m.startIndex];
					m.headString = m.originalSpan.ToString();
				}
			}
		}

		protected internal virtual Tree FindSyntacticHead(Mention m, Tree root, IList<CoreLabel> tokens)
		{
			// mention ends with 's
			int endIdx = m.endIndex;
			if (m.originalSpan.Count > 0)
			{
				string lastWord = m.originalSpan[m.originalSpan.Count - 1].Get(typeof(CoreAnnotations.TextAnnotation));
				if ((lastWord.Equals("'s") || lastWord.Equals("'")) && m.originalSpan.Count != 1)
				{
					endIdx--;
				}
			}
			Tree exactMatch = FindTreeWithSpan(root, m.startIndex, endIdx);
			//
			// found an exact match
			//
			if (exactMatch != null)
			{
				return SafeHead(exactMatch, endIdx);
			}
			// no exact match found
			// in this case, we parse the actual extent of the mention, embedded in a sentence
			// context, so as to make the parser work better :-)
			if (allowReparsing)
			{
				int approximateness = 0;
				IList<CoreLabel> extentTokens = new List<CoreLabel>();
				extentTokens.Add(InitCoreLabel("It"));
				extentTokens.Add(InitCoreLabel("was"));
				int AddedWords = 2;
				for (int i = m.startIndex; i < endIdx; i++)
				{
					// Add everything except separated dashes! The separated dashes mess with the parser too badly.
					CoreLabel label = tokens[i];
					if (!"-".Equals(label.Word()))
					{
						// necessary to copy tokens in case the parser does things like
						// put new indices on the tokens
						extentTokens.Add((CoreLabel)label.LabelFactory().NewLabel(label));
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
				ParserConstraint constraint = new ParserConstraint(AddedWords, extentTokens.Count - 1, Pattern.Compile(".*"));
				IList<ParserConstraint> constraints = Java.Util.Collections.SingletonList(constraint);
				Tree tree = Parse(extentTokens, constraints);
				ConvertToCoreLabels(tree);
				// now unnecessary, as parser uses CoreLabels?
				tree.IndexSpans(m.startIndex - AddedWords);
				// remember it has ADDED_WORDS extra words at the beginning
				Tree subtree = FindPartialSpan(tree, m.startIndex);
				// There was a possible problem that with a crazy parse, extentHead could be one of the added words, not a real word!
				// Now we make sure in findPartialSpan that it can't be before the real start, and in safeHead, we disallow something
				// passed the right end (that is, just that final period).
				Tree extentHead = SafeHead(subtree, endIdx);
				System.Diagnostics.Debug.Assert((extentHead != null));
				// extentHead is a child in the local extent parse tree. we need to find the corresponding node in the main tree
				// Because we deleted dashes, it's index will be >= the index in the extent parse tree
				CoreLabel l = (CoreLabel)extentHead.Label();
				Tree realHead = FunkyFindLeafWithApproximateSpan(root, l.Value(), l.Get(typeof(CoreAnnotations.BeginIndexAnnotation)), approximateness);
				System.Diagnostics.Debug.Assert((realHead != null));
				return realHead;
			}
			// If reparsing wasn't allowed, try to find a span in the tree
			// which happens to have the head
			Tree wordMatch = FindTreeWithSmallestSpan(root, m.startIndex, endIdx);
			if (wordMatch != null)
			{
				Tree head = SafeHead(wordMatch, endIdx);
				if (head != null)
				{
					int index = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					if (index >= m.startIndex && index < endIdx)
					{
						return head;
					}
				}
			}
			// If that didn't work, guess that it's the last word
			int lastNounIdx = endIdx - 1;
			for (int i_1 = m.startIndex; i_1 < m.endIndex; i_1++)
			{
				if (tokens[i_1].Tag().StartsWith("N"))
				{
					lastNounIdx = i_1;
				}
				else
				{
					if (tokens[i_1].Tag().StartsWith("W"))
					{
						break;
					}
				}
			}
			IList<Tree> leaves = root.GetLeaves();
			Tree endLeaf = leaves[lastNounIdx];
			return endLeaf;
		}

		/// <summary>Find the tree that covers the portion of interest.</summary>
		private static Tree FindPartialSpan(Tree root, int start)
		{
			CoreLabel label = (CoreLabel)root.Label();
			int startIndex = label.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
			if (startIndex == start)
			{
				return root;
			}
			foreach (Tree kid in root.Children())
			{
				CoreLabel kidLabel = (CoreLabel)kid.Label();
				int kidStart = kidLabel.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
				int kidEnd = kidLabel.Get(typeof(CoreAnnotations.EndIndexAnnotation));
				if (kidStart <= start && kidEnd > start)
				{
					return FindPartialSpan(kid, start);
				}
			}
			throw new Exception("Shouldn't happen: " + start + " " + root);
		}

		private static Tree FunkyFindLeafWithApproximateSpan(Tree root, string token, int index, int approximateness)
		{
			// log.info("Searching " + root + "\n  for " + token + " at position " + index + " (plus up to " + approximateness + ")");
			IList<Tree> leaves = root.GetLeaves();
			foreach (Tree leaf in leaves)
			{
				CoreLabel label = typeof(CoreLabel).Cast(leaf.Label());
				int indexInteger = label.Get(typeof(CoreAnnotations.IndexAnnotation));
				if (indexInteger == null)
				{
					continue;
				}
				int ind = indexInteger - 1;
				if (token.Equals(leaf.Value()) && ind >= index && ind <= index + approximateness)
				{
					return leaf;
				}
			}
			// this shouldn't happen
			//    throw new RuntimeException("RuleBasedCorefMentionFinder: ERROR: Failed to find head token");
			SieveCoreferenceSystem.logger.Warning("RuleBasedCorefMentionFinder: Failed to find head token:\n" + "Tree is: " + root + "\n" + "token = |" + token + "|" + index + "|, approx=" + approximateness);
			foreach (Tree leaf_1 in leaves)
			{
				if (token.Equals(leaf_1.Value()))
				{
					//log.info("Found something: returning " + leaf);
					return leaf_1;
				}
			}
			int fallback = Math.Max(0, leaves.Count - 2);
			SieveCoreferenceSystem.logger.Warning("RuleBasedCorefMentionFinder: Last resort: returning as head: " + leaves[fallback]);
			return leaves[fallback];
		}

		// last except for the added period.
		private static CoreLabel InitCoreLabel(string token)
		{
			CoreLabel label = new CoreLabel();
			label.Set(typeof(CoreAnnotations.TextAnnotation), token);
			label.Set(typeof(CoreAnnotations.ValueAnnotation), token);
			return label;
		}

		private Tree Parse(IList<CoreLabel> tokens)
		{
			return Parse(tokens, null);
		}

		private Tree Parse(IList<CoreLabel> tokens, IList<ParserConstraint> constraints)
		{
			ICoreMap sent = new Annotation(string.Empty);
			sent.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			sent.Set(typeof(ParserAnnotations.ConstraintAnnotation), constraints);
			Annotation doc = new Annotation(string.Empty);
			IList<ICoreMap> sents = new List<ICoreMap>(1);
			sents.Add(sent);
			doc.Set(typeof(CoreAnnotations.SentencesAnnotation), sents);
			GetParser().Annotate(doc);
			sents = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sents[0].Get(typeof(TreeCoreAnnotations.TreeAnnotation));
		}

		private IAnnotator GetParser()
		{
			if (parserProcessor == null)
			{
				IAnnotator parser = StanfordCoreNLP.GetExistingAnnotator("parse");
				if (parser == null)
				{
					Properties emptyProperties = new Properties();
					parser = new ParserAnnotator("coref.parse.md", emptyProperties);
				}
				if (parser == null)
				{
					// TODO: these assertions rule out the possibility of alternately named parse/pos annotators
					throw new AssertionError("Failed to get parser - this should not be possible");
				}
				if (parser.Requires().Contains(typeof(CoreAnnotations.PartOfSpeechAnnotation)))
				{
					IAnnotator tagger = StanfordCoreNLP.GetExistingAnnotator("pos");
					if (tagger == null)
					{
						throw new AssertionError("Parser required tagger, but failed to find the pos annotator");
					}
					IList<IAnnotator> annotators = Generics.NewArrayList();
					annotators.Add(tagger);
					annotators.Add(parser);
					parserProcessor = new AnnotationPipeline(annotators);
				}
				else
				{
					parserProcessor = parser;
				}
			}
			return parserProcessor;
		}

		// This probably isn't needed now; everything is always a core label. But no-op.
		private static void ConvertToCoreLabels(Tree tree)
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

		private Tree SafeHead(Tree top, int endIndex)
		{
			// The trees passed in do not have the CoordinationTransformer
			// applied, but that just means the SemanticHeadFinder results are
			// slightly worse.
			Tree head = top.HeadTerminal(headFinder);
			// One obscure failure case is that the added period becomes the head. Disallow this.
			if (head != null)
			{
				int headIndexInteger = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				if (headIndexInteger != null)
				{
					int headIndex = headIndexInteger - 1;
					if (headIndex < endIndex)
					{
						return head;
					}
				}
			}
			// if no head found return the right-most leaf
			IList<Tree> leaves = top.GetLeaves();
			int candidate = leaves.Count - 1;
			while (candidate >= 0)
			{
				head = leaves[candidate];
				int headIndexInteger = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				if (headIndexInteger != null)
				{
					int headIndex = headIndexInteger - 1;
					if (headIndex < endIndex)
					{
						return head;
					}
				}
				candidate--;
			}
			// fallback: return top
			return top;
		}

		internal static Tree FindTreeWithSmallestSpan(Tree tree, int start, int end)
		{
			IList<Tree> leaves = tree.GetLeaves();
			Tree startLeaf = leaves[start];
			Tree endLeaf = leaves[end - 1];
			return Edu.Stanford.Nlp.Trees.Trees.GetLowestCommonAncestor(Arrays.AsList(startLeaf, endLeaf), tree);
		}

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

		/// <summary>Filter out all spurious mentions</summary>
		protected internal static void RemoveSpuriousMentions(ICoreMap s, IList<Mention> mentions, Dictionaries dict)
		{
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			ICollection<Mention> remove = Generics.NewHashSet();
			foreach (Mention m in mentions)
			{
				string headPOS = m.headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				string headNE = m.headWord.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				// pleonastic it
				if (IsPleonastic(m, tree))
				{
					remove.Add(m);
				}
				// non word such as 'hmm'
				if (dict.nonWords.Contains(m.headString))
				{
					remove.Add(m);
				}
				// quantRule : not starts with 'any', 'all' etc
				if (m.originalSpan.Count > 0 && dict.quantifiers.Contains(m.originalSpan[0].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower(Locale.English)))
				{
					remove.Add(m);
				}
				// partitiveRule
				if (PartitiveRule(m, sent, dict))
				{
					remove.Add(m);
				}
				// bareNPRule
				if (headPOS.Equals("NN") && !dict.temporals.Contains(m.headString) && (m.originalSpan.Count == 1 || m.originalSpan[0].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("JJ")))
				{
					remove.Add(m);
				}
				// remove generic rule
				//  if(m.generic==true) remove.add(m);
				if (m.headString.Equals("%"))
				{
					remove.Add(m);
				}
				if (headNE.Equals("PERCENT") || headNE.Equals("MONEY"))
				{
					remove.Add(m);
				}
				// adjective form of nations
				if (dict.IsAdjectivalDemonym(m.SpanToString()))
				{
					remove.Add(m);
				}
				// stop list (e.g., U.S., there)
				if (InStopList(m))
				{
					remove.Add(m);
				}
			}
			// nested mention with shared headword (except apposition, enumeration): pick larger one
			foreach (Mention m1 in mentions)
			{
				foreach (Mention m2 in mentions)
				{
					if (m1 == m2 || remove.Contains(m1) || remove.Contains(m2))
					{
						continue;
					}
					if (m1.sentNum == m2.sentNum && m1.headWord == m2.headWord && m2.InsideIn(m1))
					{
						if (m2.endIndex < sent.Count && (sent[m2.endIndex].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals(",") || sent[m2.endIndex].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("CC")))
						{
							continue;
						}
						remove.Add(m2);
					}
				}
			}
			mentions.RemoveAll(remove);
		}

		private static bool InStopList(Mention m)
		{
			string mentionSpan = m.SpanToString().ToLower(Locale.English);
			if (mentionSpan.Equals("u.s.") || mentionSpan.Equals("u.k.") || mentionSpan.Equals("u.s.s.r"))
			{
				return true;
			}
			if (mentionSpan.Equals("there") || mentionSpan.StartsWith("etc.") || mentionSpan.Equals("ltd."))
			{
				return true;
			}
			if (mentionSpan.StartsWith("'s "))
			{
				return true;
			}
			if (mentionSpan.EndsWith("etc."))
			{
				return true;
			}
			return false;
		}

		private static bool PartitiveRule(Mention m, IList<CoreLabel> sent, Dictionaries dict)
		{
			return m.startIndex >= 2 && Sharpen.Runtime.EqualsIgnoreCase(sent[m.startIndex - 1].Get(typeof(CoreAnnotations.TextAnnotation)), "of") && dict.parts.Contains(sent[m.startIndex - 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower(Locale.English
				));
		}

		/// <summary>Check whether pleonastic 'it'.</summary>
		/// <remarks>Check whether pleonastic 'it'. E.g., It is possible that ...</remarks>
		private static readonly TregexPattern[] pleonasticPatterns = GetPleonasticPatterns();

		private static bool IsPleonastic(Mention m, Tree tree)
		{
			if (!Sharpen.Runtime.EqualsIgnoreCase(m.SpanToString(), "it"))
			{
				return false;
			}
			foreach (TregexPattern p in pleonasticPatterns)
			{
				if (CheckPleonastic(m, tree, p))
				{
					// SieveCoreferenceSystem.logger.fine("RuleBasedCorefMentionFinder: matched pleonastic pattern '" + p + "' for " + tree);
					return true;
				}
			}
			return false;
		}

		private static TregexPattern[] GetPleonasticPatterns()
		{
			string[] patterns = new string[] { "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@VP < (VBN $.. @S|SBAR))))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (ADJP $.. (/S|SBAR/))))"
				, "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (ADJP < (/S|SBAR/))))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (NP < /S|SBAR/)))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (NP $.. ADVP $.. /S|SBAR/)))"
				, "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (VP < (VBN $.. /S|SBAR/))))))", "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (ADJP $.. (/S|SBAR/))))))", "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (ADJP < (/S|SBAR/))))))"
				, "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (NP < /S|SBAR/)))))", "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (NP $.. ADVP $.. /S|SBAR/)))))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:seems|appears|means|follows)/) $.. /S|SBAR/))"
				, "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:turns|turned)/) $.. PRT $.. /S|SBAR/))" };
			// cdm 2013: I spent a while on these patterns. I fixed a syntax error in five patterns ($.. split with space), so it now shouldn't exception in checkPleonastic. This gave 0.02% on CoNLL11 dev
			// I tried some more precise patterns but they didn't help. Indeed, they tended to hurt vs. the higher recall patterns.
			//"NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (VP < (VBN $.. /S|SBAR/))))", // overmatches
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@VP < (VBN < expected|hoped $.. @SBAR))))",  // this one seems more accurate, but ...
			// in practice, go with this one (best results)
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@ADJP < (/^(?:JJ|VB)/ < /^(?i:(?:hard|tough|easi)(?:er|est)?|(?:im|un)?(?:possible|interesting|worthwhile|likely|surprising|certain)|disappointing|pointless|easy|fine|okay)$/) [ < @S|SBAR | $.. (@S|SBAR !< (IN !< for|For|FOR|that|That|THAT)) ] )))", // does worse than above 2 on CoNLL11 dev
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@NP $.. @ADVP $.. @SBAR)))", // cleft examples, generalized to not need ADVP; but gave worse CoNLL12 dev numbers....
			// these next 5 had buggy space in "$ ..", which I fixed
			// extraposed. OK 1/2 correct; need non-adverbial case
			// OK: 3/3 good matches on dev; but 3/4 wrong on WSJ
			// certain can be either but relatively likely pleonastic with it ... be
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (MD $.. (@VP < ((/^V.*/ < /^(?:be|become)/) $.. (@ADJP < (/^JJ/ < /^(?i:(?:hard|tough|easi)(?:er|est)?|(?:im|un)?(?:possible|interesting|worthwhile|likely|surprising|certain)|disappointing|pointless|easy|fine|okay))$/) [ < @S|SBAR | $.. (@S|SBAR !< (IN !< for|For|FOR|that|That|THAT)) ] )))))", // GOOD REPLACEMENT ; 2nd clause is for extraposed ones
			TregexPattern[] tgrepPatterns = new TregexPattern[patterns.Length];
			for (int i = 0; i < tgrepPatterns.Length; i++)
			{
				tgrepPatterns[i] = TregexPattern.Compile(patterns[i]);
			}
			return tgrepPatterns;
		}

		private static bool CheckPleonastic(Mention m, Tree tree, TregexPattern tgrepPattern)
		{
			try
			{
				TregexMatcher matcher = tgrepPattern.Matcher(tree);
				while (matcher.Find())
				{
					Tree np1 = matcher.GetNode("m1");
					if (((CoreLabel)np1.Label()).Get(typeof(CoreAnnotations.BeginIndexAnnotation)) + 1 == m.headWord.Get(typeof(CoreAnnotations.IndexAnnotation)))
					{
						return true;
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return false;
		}
	}
}

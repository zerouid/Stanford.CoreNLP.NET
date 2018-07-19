using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Coref.MD;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>A class for featurizing mention pairs and individual mentions.</summary>
	/// <author>Kevin Clark</author>
	public class FeatureExtractor
	{
		private const int MinWordCount = 20;

		private const int BinExact = 10;

		private const double BinExponent = 1.5;

		private static readonly IDictionary<int, string> SingletonFeatures = new Dictionary<int, string>();

		static FeatureExtractor()
		{
			SingletonFeatures[2] = "animacy";
			SingletonFeatures[3] = "person-coarse";
			SingletonFeatures[4] = "number";
			SingletonFeatures[5] = "position";
			SingletonFeatures[6] = "relation";
			SingletonFeatures[7] = "quantification";
			SingletonFeatures[8] = "modifiers";
			SingletonFeatures[9] = "negation";
			SingletonFeatures[10] = "modal";
			SingletonFeatures[11] = "attitude";
			SingletonFeatures[12] = "coordination";
		}

		private readonly Dictionaries dictionaries;

		private readonly ICollection<string> vocabulary;

		private readonly Compressor<string> compressor;

		private readonly bool useConstituencyParse;

		private readonly bool useDocSource;

		public FeatureExtractor(Properties props, Dictionaries dictionaries, Compressor<string> compressor)
			: this(props, dictionaries, compressor, StatisticalCorefTrainer.wordCountsFile)
		{
		}

		public FeatureExtractor(Properties props, Dictionaries dictionaries, Compressor<string> compressor, string wordCountsPath)
			: this(props, dictionaries, compressor, LoadVocabulary(wordCountsPath))
		{
		}

		public FeatureExtractor(Properties props, Dictionaries dictionaries, Compressor<string> compressor, ICollection<string> vocabulary)
		{
			this.dictionaries = dictionaries;
			this.compressor = compressor;
			this.vocabulary = vocabulary;
			this.useDocSource = CorefProperties.Conll(props);
			this.useConstituencyParse = CorefProperties.UseConstituencyParse(props);
		}

		private static ICollection<string> LoadVocabulary(string wordCountsPath)
		{
			ICollection<string> vocabulary = new HashSet<string>();
			try
			{
				ICounter<string> counts = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(wordCountsPath);
				foreach (KeyValuePair<string, double> e in counts.EntrySet())
				{
					if (e.Value > MinWordCount)
					{
						vocabulary.Add(e.Key);
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception("Error loading word counts", e);
			}
			return vocabulary;
		}

		public virtual DocumentExamples Extract(int id, Document document, IDictionary<Pair<int, int>, bool> labeledPairs)
		{
			return Extract(id, document, labeledPairs, compressor);
		}

		public virtual DocumentExamples Extract(int id, Document document, IDictionary<Pair<int, int>, bool> labeledPairs, Compressor<string> compressor)
		{
			IList<Mention> mentionsList = CorefUtils.GetSortedMentions(document);
			IDictionary<int, IList<Mention>> mentionsByHeadIndex = new Dictionary<int, IList<Mention>>();
			foreach (Mention m in mentionsList)
			{
				IList<Mention> withIndex = mentionsByHeadIndex[m.headIndex];
				if (withIndex == null)
				{
					withIndex = new List<Mention>();
					mentionsByHeadIndex[m.headIndex] = withIndex;
				}
				withIndex.Add(m);
			}
			IDictionary<int, Mention> mentions = document.predictedMentionsByID;
			IList<Example> examples = new List<Example>();
			ICollection<int> mentionsToExtract = new HashSet<int>();
			foreach (KeyValuePair<Pair<int, int>, bool> pair in labeledPairs)
			{
				Mention m1 = mentions[pair.Key.first];
				Mention m2 = mentions[pair.Key.second];
				mentionsToExtract.Add(m1.mentionID);
				mentionsToExtract.Add(m2.mentionID);
				CompressedFeatureVector features = compressor.Compress(GetFeatures(document, m1, m2));
				examples.Add(new Example(id, m1, m2, pair.Value ? 1.0 : 0.0, features));
			}
			IDictionary<int, CompressedFeatureVector> mentionFeatures = new Dictionary<int, CompressedFeatureVector>();
			foreach (int mentionID in mentionsToExtract)
			{
				mentionFeatures[mentionID] = compressor.Compress(GetFeatures(document, document.predictedMentionsByID[mentionID], mentionsByHeadIndex));
			}
			return new DocumentExamples(id, examples, mentionFeatures);
		}

		private ICounter<string> GetFeatures(Document doc, Mention m, IDictionary<int, IList<Mention>> mentionsByHeadIndex)
		{
			ICounter<string> features = new ClassicCounter<string>();
			// type features
			features.IncrementCount("mention-type=" + m.mentionType);
			features.IncrementCount("gender=" + m.gender);
			features.IncrementCount("person-fine=" + m.person);
			features.IncrementCount("head-ne-type=" + m.nerString);
			IList<string> singletonFeatures = m.GetSingletonFeatures(dictionaries);
			foreach (KeyValuePair<int, string> e in SingletonFeatures)
			{
				if (e.Key < singletonFeatures.Count)
				{
					features.IncrementCount(e.Value + "=" + singletonFeatures[e.Key]);
				}
			}
			// length and location features
			AddNumeric(features, "mention-length", m.SpanToString().Length);
			AddNumeric(features, "mention-words", m.originalSpan.Count);
			AddNumeric(features, "sentence-words", m.sentenceWords.Count);
			features.IncrementCount("sentence-words=" + Bin(m.sentenceWords.Count));
			features.IncrementCount("mention-position", m.mentionNum / (double)doc.predictedMentions.Count);
			features.IncrementCount("sentence-position", m.sentNum / (double)doc.numSentences);
			// lexical features
			CoreLabel firstWord = FirstWord(m);
			CoreLabel lastWord = LastWord(m);
			CoreLabel headWord = HeadWord(m);
			CoreLabel prevWord = PrevWord(m);
			CoreLabel nextWord = NextWord(m);
			CoreLabel prevprevWord = PrevprevWord(m);
			CoreLabel nextnextWord = NextnextWord(m);
			string headPOS = GetPOS(headWord);
			string firstPOS = GetPOS(firstWord);
			string lastPOS = GetPOS(lastWord);
			string prevPOS = GetPOS(prevWord);
			string nextPOS = GetPOS(nextWord);
			string prevprevPOS = GetPOS(prevprevWord);
			string nextnextPOS = GetPOS(nextnextWord);
			features.IncrementCount("first-word=" + WordIndicator(firstWord, firstPOS));
			features.IncrementCount("last-word=" + WordIndicator(lastWord, lastPOS));
			features.IncrementCount("head-word=" + WordIndicator(headWord, headPOS));
			features.IncrementCount("next-word=" + WordIndicator(nextWord, nextPOS));
			features.IncrementCount("prev-word=" + WordIndicator(prevWord, prevPOS));
			features.IncrementCount("next-bigram=" + WordIndicator(nextWord, nextnextWord, nextPOS + "_" + nextnextPOS));
			features.IncrementCount("prev-bigram=" + WordIndicator(prevprevWord, prevWord, prevprevPOS + "_" + prevPOS));
			features.IncrementCount("next-pos=" + nextPOS);
			features.IncrementCount("prev-pos=" + prevPOS);
			features.IncrementCount("first-pos=" + firstPOS);
			features.IncrementCount("last-pos=" + lastPOS);
			features.IncrementCount("next-pos-bigram=" + nextPOS + "_" + nextnextPOS);
			features.IncrementCount("prev-pos-bigram=" + prevprevPOS + "_" + prevPOS);
			AddDependencyFeatures(features, "parent", GetDependencyParent(m), true);
			AddFeature(features, "ends-with-head", m.headIndex == m.endIndex - 1);
			AddFeature(features, "is-generic", m.originalSpan.Count == 1 && firstPOS.Equals("NNS"));
			// syntax features
			IndexedWord w = m.headIndexedWord;
			string depPath = string.Empty;
			int depth = 0;
			while (w != null)
			{
				SemanticGraphEdge e_1 = GetDependencyParent(m, w);
				depth++;
				if (depth <= 3 && e_1 != null)
				{
					depPath += (depPath.IsEmpty() ? string.Empty : "_") + e_1.GetRelation().ToString();
					features.IncrementCount("dep-path=" + depPath);
					w = e_1.GetSource();
				}
				else
				{
					w = null;
				}
			}
			if (useConstituencyParse)
			{
				int fullEmbeddingLevel = HeadEmbeddingLevel(m.contextParseTree, m.headIndex);
				int mentionEmbeddingLevel = HeadEmbeddingLevel(m.mentionSubTree, m.headIndex - m.startIndex);
				if (fullEmbeddingLevel != -1 && mentionEmbeddingLevel != -1)
				{
					features.IncrementCount("mention-embedding-level=" + Bin(fullEmbeddingLevel - mentionEmbeddingLevel));
					features.IncrementCount("head-embedding-level=" + Bin(mentionEmbeddingLevel));
				}
				else
				{
					features.IncrementCount("undetermined-embedding-level");
				}
				features.IncrementCount("num-embedded-nps=" + Bin(NumEmbeddedNps(m.mentionSubTree)));
				string syntaxPath = string.Empty;
				Tree tree = m.contextParseTree;
				Tree head = tree.GetLeaves()[m.headIndex].Ancestor(1, tree);
				depth = 0;
				foreach (Tree node in tree.PathNodeToNode(head, tree))
				{
					syntaxPath += node.Value() + "-";
					features.IncrementCount("syntax-path=" + syntaxPath);
					depth++;
					if (depth >= 4 || node.Value().Equals("S"))
					{
						break;
					}
				}
			}
			// mention containment features
			AddFeature(features, "contained-in-other-mention", mentionsByHeadIndex[m.headIndex].Stream().AnyMatch(null));
			AddFeature(features, "contains-other-mention", mentionsByHeadIndex[m.headIndex].Stream().AnyMatch(null));
			// features from dcoref rules
			AddFeature(features, "bare-plural", m.originalSpan.Count == 1 && headPOS.Equals("NNS"));
			AddFeature(features, "quantifier-start", dictionaries.quantifiers.Contains(firstWord.Word().ToLower()));
			AddFeature(features, "negative-start", firstWord.Word().ToLower().Matches("none|no|nothing|not"));
			AddFeature(features, "partitive", RuleBasedCorefMentionFinder.PartitiveRule(m, m.sentenceWords, dictionaries));
			AddFeature(features, "adjectival-demonym", dictionaries.IsAdjectivalDemonym(m.SpanToString()));
			if (doc.docType != Document.DocType.Article && m.person == Dictionaries.Person.You && nextWord != null && Sharpen.Runtime.EqualsIgnoreCase(nextWord.Word(), "know"))
			{
				features.IncrementCount("generic-you");
			}
			return features;
		}

		private ICounter<string> GetFeatures(Document doc, Mention m1, Mention m2)
		{
			System.Diagnostics.Debug.Assert((m1.AppearEarlierThan(m2)));
			ICounter<string> features = new ClassicCounter<string>();
			// global features
			features.IncrementCount("bias");
			if (useDocSource)
			{
				features.IncrementCount("doc-type=" + doc.docType);
				if (doc.docInfo != null && doc.docInfo.Contains("DOC_ID"))
				{
					features.IncrementCount("doc-source=" + doc.docInfo["DOC_ID"].Split("/")[1]);
				}
			}
			// singleton feature conjunctions
			IList<string> singletonFeatures1 = m1.GetSingletonFeatures(dictionaries);
			IList<string> singletonFeatures2 = m2.GetSingletonFeatures(dictionaries);
			foreach (KeyValuePair<int, string> e in SingletonFeatures)
			{
				if (e.Key < singletonFeatures1.Count && e.Key < singletonFeatures2.Count)
				{
					features.IncrementCount(e.Value + "=" + singletonFeatures1[e.Key] + "_" + singletonFeatures2[e.Key]);
				}
			}
			SemanticGraphEdge p1 = GetDependencyParent(m1);
			SemanticGraphEdge p2 = GetDependencyParent(m2);
			features.IncrementCount("dep-relations=" + (p1 == null ? "null" : p1.GetRelation()) + "_" + (p2 == null ? "null" : p2.GetRelation()));
			features.IncrementCount("roles=" + GetRole(m1) + "_" + GetRole(m2));
			CoreLabel headCL1 = HeadWord(m1);
			CoreLabel headCL2 = HeadWord(m2);
			string headPOS1 = GetPOS(headCL1);
			string headPOS2 = GetPOS(headCL2);
			features.IncrementCount("head-pos-s=" + headPOS1 + "_" + headPOS2);
			features.IncrementCount("head-words=" + WordIndicator("h_" + headCL1.Word().ToLower() + "_" + headCL2.Word().ToLower(), headPOS1 + "_" + headPOS2));
			// agreement features
			AddFeature(features, "animacies-agree", m2.AnimaciesAgree(m1));
			AddFeature(features, "attributes-agree", m2.AttributesAgree(m1, dictionaries));
			AddFeature(features, "entity-types-agree", m2.EntityTypesAgree(m1, dictionaries));
			AddFeature(features, "numbers-agree", m2.NumbersAgree(m1));
			AddFeature(features, "genders-agree", m2.GendersAgree(m1));
			AddFeature(features, "ner-strings-equal", m1.nerString.Equals(m2.nerString));
			// string matching features
			AddFeature(features, "antecedent-head-in-anaphor", HeadContainedIn(m1, m2));
			AddFeature(features, "anaphor-head-in-antecedent", HeadContainedIn(m2, m1));
			if (m1.mentionType != Dictionaries.MentionType.Pronominal && m2.mentionType != Dictionaries.MentionType.Pronominal)
			{
				AddFeature(features, "antecedent-in-anaphor", m2.SpanToString().ToLower().Contains(m1.SpanToString().ToLower()));
				AddFeature(features, "anaphor-in-antecedent", m1.SpanToString().ToLower().Contains(m2.SpanToString().ToLower()));
				AddFeature(features, "heads-equal", Sharpen.Runtime.EqualsIgnoreCase(m1.headString, m2.headString));
				AddFeature(features, "heads-agree", m2.HeadsAgree(m1));
				AddFeature(features, "exact-match", m1.ToString().Trim().ToLower().Equals(m2.ToString().Trim().ToLower()));
				AddFeature(features, "partial-match", RelaxedStringMatch(m1, m2));
				double editDistance = StringUtils.EditDistance(m1.SpanToString(), m2.SpanToString()) / (double)(m1.SpanToString().Length + m2.SpanToString().Length);
				features.IncrementCount("edit-distance", editDistance);
				features.IncrementCount("edit-distance=" + ((int)(editDistance * 10) / 10.0));
				double headEditDistance = StringUtils.EditDistance(m1.headString, m2.headString) / (double)(m1.headString.Length + m2.headString.Length);
				features.IncrementCount("head-edit-distance", headEditDistance);
				features.IncrementCount("head-edit-distance=" + ((int)(headEditDistance * 10) / 10.0));
			}
			// distance features
			AddNumeric(features, "mention-distance", m2.mentionNum - m1.mentionNum);
			AddNumeric(features, "sentence-distance", m2.sentNum - m1.sentNum);
			if (m2.sentNum == m1.sentNum)
			{
				AddNumeric(features, "word-distance", m2.startIndex - m1.endIndex);
				if (m1.endIndex > m2.startIndex)
				{
					features.IncrementCount("spans-intersect");
				}
			}
			// setup for dcoref features
			ICollection<Mention> ms1 = new HashSet<Mention>();
			ms1.Add(m1);
			ICollection<Mention> ms2 = new HashSet<Mention>();
			ms2.Add(m2);
			Random r = new Random();
			CorefCluster c1 = new CorefCluster(20000 + r.NextInt(10000), ms1);
			CorefCluster c2 = new CorefCluster(10000 + r.NextInt(10000), ms2);
			string s2 = m2.LowercaseNormalizedSpanString();
			string s1 = m1.LowercaseNormalizedSpanString();
			// discourse dcoref features
			AddFeature(features, "mention-speaker-PER0", Sharpen.Runtime.EqualsIgnoreCase(m2.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation)), "PER0"));
			AddFeature(features, "antecedent-is-anaphor-speaker", CorefRules.AntecedentIsMentionSpeaker(doc, m2, m1, dictionaries));
			AddFeature(features, "same-speaker", CorefRules.EntitySameSpeaker(doc, m2, m1));
			AddFeature(features, "person-disagree-same-speaker", CorefRules.EntityPersonDisagree(doc, m2, m1, dictionaries) && CorefRules.EntitySameSpeaker(doc, m2, m1));
			AddFeature(features, "antecedent-matches-anaphor-speaker", CorefRules.AntecedentMatchesMentionSpeakerAnnotation(m2, m1, doc));
			AddFeature(features, "discourse-you-PER0", m2.person == Dictionaries.Person.You && doc.docType == Document.DocType.Article && m2.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation)).Equals("PER0"));
			AddFeature(features, "speaker-match-i-i", m2.number == Dictionaries.Number.Singular && dictionaries.firstPersonPronouns.Contains(s1) && m1.number == Dictionaries.Number.Singular && dictionaries.firstPersonPronouns.Contains(s2) && CorefRules.
				EntitySameSpeaker(doc, m2, m1));
			AddFeature(features, "speaker-match-speaker-i", m2.number == Dictionaries.Number.Singular && dictionaries.firstPersonPronouns.Contains(s2) && CorefRules.AntecedentIsMentionSpeaker(doc, m2, m1, dictionaries));
			AddFeature(features, "speaker-match-i-speaker", m1.number == Dictionaries.Number.Singular && dictionaries.firstPersonPronouns.Contains(s1) && CorefRules.AntecedentIsMentionSpeaker(doc, m1, m2, dictionaries));
			AddFeature(features, "speaker-match-you-you", dictionaries.secondPersonPronouns.Contains(s1) && dictionaries.secondPersonPronouns.Contains(s2) && CorefRules.EntitySameSpeaker(doc, m2, m1));
			AddFeature(features, "discourse-between-two-person", ((m2.person == Dictionaries.Person.I && m1.person == Dictionaries.Person.You || (m2.person == Dictionaries.Person.You && m1.person == Dictionaries.Person.I)) && (m2.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation
				)) - m1.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) == 1) && doc.docType == Document.DocType.Conversation));
			AddFeature(features, "incompatible-not-match", m1.person != Dictionaries.Person.I && m2.person != Dictionaries.Person.I && (CorefRules.AntecedentIsMentionSpeaker(doc, m1, m2, dictionaries) || CorefRules.AntecedentIsMentionSpeaker(doc, m2, m1
				, dictionaries)));
			int utteranceDist = Math.Abs(m1.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)) - m2.headWord.Get(typeof(CoreAnnotations.UtteranceAnnotation)));
			if (doc.docType != Document.DocType.Article && utteranceDist == 1 && !CorefRules.EntitySameSpeaker(doc, m2, m1))
			{
				AddFeature(features, "speaker-mismatch-i-i", m1.person == Dictionaries.Person.I && m2.person == Dictionaries.Person.I);
				AddFeature(features, "speaker-mismatch-you-you", m1.person == Dictionaries.Person.You && m2.person == Dictionaries.Person.You);
				AddFeature(features, "speaker-mismatch-we-we", m1.person == Dictionaries.Person.We && m2.person == Dictionaries.Person.We);
			}
			// other dcoref features
			string firstWord1 = FirstWord(m1).Word().ToLower();
			AddFeature(features, "indefinite-article-np", (m1.appositions == null && m1.predicateNominatives == null && (firstWord1.Equals("a") || firstWord1.Equals("an"))));
			AddFeature(features, "far-this", m2.LowercaseNormalizedSpanString().Equals("this") && Math.Abs(m2.sentNum - m1.sentNum) > 3);
			AddFeature(features, "per0-you-in-article", m2.person == Dictionaries.Person.You && doc.docType == Document.DocType.Article && m2.headWord.Get(typeof(CoreAnnotations.SpeakerAnnotation)).Equals("PER0"));
			AddFeature(features, "inside-in", m2.InsideIn(m1) || m1.InsideIn(m2));
			AddFeature(features, "indefinite-determiners", dictionaries.indefinitePronouns.Contains(m1.originalSpan[0].Lemma()) || dictionaries.indefinitePronouns.Contains(m2.originalSpan[0].Lemma()));
			AddFeature(features, "entity-attributes-agree", CorefRules.EntityAttributesAgree(c2, c1));
			AddFeature(features, "entity-token-distance", CorefRules.EntityTokenDistance(m2, m1));
			AddFeature(features, "i-within-i", CorefRules.EntityIWithinI(m2, m1, dictionaries));
			AddFeature(features, "exact-string-match", CorefRules.EntityExactStringMatch(c2, c1, dictionaries, doc.roleSet));
			AddFeature(features, "entity-relaxed-heads-agree", CorefRules.EntityRelaxedHeadsAgreeBetweenMentions(c2, c1, m2, m1));
			AddFeature(features, "is-acronym", CorefRules.EntityIsAcronym(doc, c2, c1));
			AddFeature(features, "demonym", m2.IsDemonym(m1, dictionaries));
			AddFeature(features, "incompatible-modifier", CorefRules.EntityHaveIncompatibleModifier(m2, m1));
			AddFeature(features, "head-lemma-match", m1.headWord.Lemma().Equals(m2.headWord.Lemma()));
			AddFeature(features, "words-included", CorefRules.EntityWordsIncluded(c2, c1, m2, m1));
			AddFeature(features, "extra-proper-noun", CorefRules.EntityHaveExtraProperNoun(m2, m1, new HashSet<string>()));
			AddFeature(features, "number-in-later-mentions", CorefRules.EntityNumberInLaterMention(m2, m1));
			AddFeature(features, "sentence-context-incompatible", CorefRules.SentenceContextIncompatible(m2, m1, dictionaries));
			// syntax features
			if (useConstituencyParse)
			{
				if (m1.sentNum == m2.sentNum)
				{
					int clauseCount = 0;
					Tree tree = m2.contextParseTree;
					Tree current = m2.mentionSubTree;
					while (true)
					{
						current = current.Ancestor(1, tree);
						if (current.Label().Value().StartsWith("S"))
						{
							clauseCount++;
						}
						if (current.Dominates(m1.mentionSubTree))
						{
							break;
						}
						if (current.Label().Value().Equals("ROOT") || current.Ancestor(1, tree) == null)
						{
							break;
						}
					}
					features.IncrementCount("clause-count", clauseCount);
					features.IncrementCount("clause-count=" + Bin(clauseCount));
				}
				if (RuleBasedCorefMentionFinder.IsPleonastic(m2, m2.contextParseTree) || RuleBasedCorefMentionFinder.IsPleonastic(m1, m1.contextParseTree))
				{
					features.IncrementCount("pleonastic-it");
				}
				if (MaximalNp(m1.mentionSubTree) == MaximalNp(m2.mentionSubTree))
				{
					features.IncrementCount("same-maximal-np");
				}
				bool m1Embedded = HeadEmbeddingLevel(m1.mentionSubTree, m1.headIndex - m1.startIndex) > 1;
				bool m2Embedded = HeadEmbeddingLevel(m2.mentionSubTree, m2.headIndex - m2.startIndex) > 1;
				features.IncrementCount("embedding=" + m1Embedded + "_" + m2Embedded);
			}
			return features;
		}

		private static void AddNumeric(ICounter<string> features, string key, int value)
		{
			features.IncrementCount(key + "=" + Bin(value));
			features.IncrementCount(key, value);
		}

		public static bool RelaxedStringMatch(Mention m1, Mention m2)
		{
			ICollection<string> propers = GetPropers(m1);
			propers.RetainAll(GetPropers(m2));
			return !propers.IsEmpty();
		}

		private static readonly ICollection<string> Propers = new HashSet<string>();

		static FeatureExtractor()
		{
			Propers.Add("NN");
			Propers.Add("NNS");
			Propers.Add("NNP");
			Propers.Add("NNPS");
		}

		private static ICollection<string> GetPropers(Mention m)
		{
			ICollection<string> propers = new HashSet<string>();
			for (int i = m.startIndex; i < m.endIndex; i++)
			{
				CoreLabel cl = m.sentenceWords[i];
				string Pos = cl.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				string word = cl.Word().ToLower();
				if (Propers.Contains(Pos))
				{
					propers.Add(word);
				}
			}
			return propers;
		}

		private static void AddFeature(ICounter<string> features, string name, bool value)
		{
			if (value)
			{
				features.IncrementCount(name);
			}
		}

		private static string Bin(int value)
		{
			return Bin(value, BinExact, BinExponent, int.MaxValue);
		}

		private static string Bin(int value, int binExact, double binExponent, int cap)
		{
			if (value < 0)
			{
				return "-" + Bin(-value);
			}
			if (value > cap)
			{
				return cap + "+";
			}
			string bin = value.ToString();
			if (value > binExact)
			{
				double start = Math.Pow(binExponent, (int)(Math.Log(value) / Math.Log(binExponent)));
				bin = (int)start + "-" + (int)(start * binExponent);
			}
			return bin;
		}

		private static string GetRole(Mention m)
		{
			if (m.isSubject)
			{
				return "subject";
			}
			else
			{
				if (m.isDirectObject)
				{
					return "direct-object";
				}
				else
				{
					if (m.isIndirectObject)
					{
						return "indirect-object";
					}
					else
					{
						if (m.isPrepositionObject)
						{
							return "preposition-object";
						}
					}
				}
			}
			return "unknown";
		}

		private static SemanticGraphEdge GetDependencyParent(Mention m)
		{
			return GetDependencyParent(m, m.headIndexedWord);
		}

		private static SemanticGraphEdge GetDependencyParent(Mention m, IndexedWord w)
		{
			IEnumerator<SemanticGraphEdge> iterator = m.enhancedDependency.IncomingEdgeIterator(w);
			return iterator.MoveNext() ? iterator.Current : null;
		}

		private void AddDependencyFeatures(ICounter<string> features, string prefix, SemanticGraphEdge e, bool addWord)
		{
			if (e == null)
			{
				features.IncrementCount("no-" + prefix);
				return;
			}
			IndexedWord parent = e.GetSource();
			string parentPOS = parent.Tag();
			string parentWord = parent.Word();
			string parentRelation = e.GetRelation().ToString();
			//String parentDir = e.getSource().beginPosition() < e.getTarget().beginPosition()
			//    ? "right" : "left";
			if (addWord)
			{
				features.IncrementCount(prefix + "-word=" + WordIndicator(parentWord, parentPOS));
			}
			features.IncrementCount(prefix + "-POS=" + parentPOS);
			features.IncrementCount(prefix + "-relation=" + parentRelation);
		}

		//features.incrementCount(prefix + "-direction=" + parentDir);
		public virtual Tree MaximalNp(Tree mentionSubTree)
		{
			Tree maximalSubtree = mentionSubTree;
			foreach (Tree subtree in mentionSubTree.PostOrderNodeList())
			{
				if (!subtree.IsLeaf() && !subtree.IsPreTerminal())
				{
					string label = ((CoreLabel)subtree.Label()).Get(typeof(CoreAnnotations.ValueAnnotation));
					if (label.Equals("NP"))
					{
						maximalSubtree = subtree;
					}
				}
			}
			return maximalSubtree;
		}

		private int NumEmbeddedNps(Tree mentionSubTree)
		{
			int embeddedNps = 0;
			foreach (Tree subtree in mentionSubTree.PostOrderNodeList())
			{
				if (!subtree.IsLeaf() && !subtree.IsPreTerminal())
				{
					string label = ((CoreLabel)subtree.Label()).Get(typeof(CoreAnnotations.ValueAnnotation));
					if (label.Equals("NP"))
					{
						embeddedNps++;
					}
				}
			}
			return embeddedNps;
		}

		private int HeadEmbeddingLevel(Tree tree, int headIndex)
		{
			int embeddingLevel = 0;
			try
			{
				Tree subtree = tree.GetLeaves()[headIndex];
				while (subtree != null)
				{
					string label = ((CoreLabel)subtree.Label()).Get(typeof(CoreAnnotations.ValueAnnotation));
					subtree = subtree.Ancestor(1, tree);
					if (label.Equals("NP"))
					{
						embeddingLevel++;
					}
				}
			}
			catch (Exception)
			{
				return -1;
			}
			return embeddingLevel;
		}

		private static bool HeadContainedIn(Mention m1, Mention m2)
		{
			string head = m1.headString;
			foreach (CoreLabel cl in m2.originalSpan)
			{
				if (head.Equals(cl.Word().ToLower()))
				{
					return true;
				}
			}
			return false;
		}

		private string WordIndicator(CoreLabel cl1, CoreLabel cl2, string Pos)
		{
			string w1 = cl1 == null ? "NONE" : cl1.Word().ToLower();
			string w2 = cl2 == null ? "NONE" : cl2.Word().ToLower();
			return WordIndicator(w1 + "_" + w2, Pos);
		}

		private string WordIndicator(CoreLabel cl, string Pos)
		{
			if (cl == null)
			{
				return "NONE";
			}
			return WordIndicator(cl.Word().ToLower(), Pos);
		}

		private string WordIndicator(string word, string Pos)
		{
			if (word == null)
			{
				return "NONE";
			}
			return vocabulary.Contains(word) ? word : Pos;
		}

		private static string GetPOS(CoreLabel cl)
		{
			return cl == null ? "NONE" : cl.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
		}

		private static CoreLabel FirstWord(Mention m)
		{
			return m.originalSpan[0];
		}

		private static CoreLabel HeadWord(Mention m)
		{
			return m.headWord;
		}

		private static CoreLabel LastWord(Mention m)
		{
			return m.originalSpan[m.originalSpan.Count - 1];
		}

		private static CoreLabel NextnextWord(Mention m)
		{
			return m.endIndex + 1 < m.sentenceWords.Count ? m.sentenceWords[m.endIndex + 1] : null;
		}

		private static CoreLabel NextWord(Mention m)
		{
			return m.endIndex < m.sentenceWords.Count ? m.sentenceWords[m.endIndex] : null;
		}

		private static CoreLabel PrevWord(Mention m)
		{
			return m.startIndex > 0 ? m.sentenceWords[m.startIndex - 1] : null;
		}

		private static CoreLabel PrevprevWord(Mention m)
		{
			return m.startIndex > 1 ? m.sentenceWords[m.startIndex - 2] : null;
		}
	}
}

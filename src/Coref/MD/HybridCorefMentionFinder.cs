using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.MD
{
	public class HybridCorefMentionFinder : CorefMentionFinder
	{
		public MentionDetectionClassifier mdClassifier = null;

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public HybridCorefMentionFinder(IHeadFinder headFinder, Properties props)
		{
			this.headFinder = headFinder;
			this.lang = CorefProperties.GetLanguage(props);
			mdClassifier = (CorefProperties.IsMentionDetectionTraining(props)) ? null : IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(CorefProperties.GetMentionDetectionModel(props));
		}

		public override IList<IList<Mention>> FindMentions(Annotation doc, Dictionaries dict, Properties props)
		{
			IList<IList<Mention>> predictedMentions = new List<IList<Mention>>();
			ICollection<string> neStrings = Generics.NewHashSet();
			IList<ICollection<IntPair>> mentionSpanSetList = Generics.NewArrayList();
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			//    boolean useNewMD = Boolean.parseBoolean(props.getProperty("useNewMD", "false"));
			// extract premarked mentions, NP/PRP, named entity, enumerations
			foreach (ICoreMap s in sentences)
			{
				IList<Mention> mentions = new List<Mention>();
				predictedMentions.Add(mentions);
				ICollection<IntPair> mentionSpanSet = Generics.NewHashSet();
				ICollection<IntPair> namedEntitySpanSet = Generics.NewHashSet();
				ExtractPremarkedEntityMentions(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractNamedEntityMentions(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractNPorPRP(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractEnumerations(s, mentions, mentionSpanSet, namedEntitySpanSet);
				AddNamedEntityStrings(s, neStrings, namedEntitySpanSet);
				mentionSpanSetList.Add(mentionSpanSet);
			}
			ExtractNamedEntityModifiers(sentences, mentionSpanSetList, predictedMentions, neStrings);
			// find head
			for (int i = 0; i < sentences.Count; i++)
			{
				FindHead(sentences[i], predictedMentions[i]);
			}
			// mention selection based on document-wise info
			RemoveSpuriousMentions(doc, predictedMentions, dict, CorefProperties.RemoveNestedMentions(props), lang);
			// if this is for MD training, skip classification
			if (!CorefProperties.IsMentionDetectionTraining(props))
			{
				mdClassifier.ClassifyMentions(predictedMentions, dict, props);
			}
			return predictedMentions;
		}

		protected internal static void ExtractNamedEntityMentions(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph basicDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			SemanticGraph enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			if (enhancedDependency == null)
			{
				enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			}
			string preNE = "O";
			int beginIndex = -1;
			foreach (CoreLabel w in sent)
			{
				string nerString = w.Ner();
				if (!nerString.Equals(preNE))
				{
					int endIndex = w.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					if (!preNE.Matches("O"))
					{
						if (w.Get(typeof(CoreAnnotations.TextAnnotation)).Equals("'s") && w.Tag().Equals("POS"))
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
							Mention m = new Mention(dummyMentionId, beginIndex, endIndex, sent, basicDependency, enhancedDependency, new List<CoreLabel>(sent.SubList(beginIndex, endIndex)));
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
			if (!preNE.Matches("O"))
			{
				IntPair mSpan = new IntPair(beginIndex, sent.Count);
				if (!mentionSpanSet.Contains(mSpan))
				{
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, beginIndex, sent.Count, sent, basicDependency, enhancedDependency, new List<CoreLabel>(sent.SubList(beginIndex, sent.Count)));
					mentions.Add(m);
					mentionSpanSet.Add(mSpan);
					namedEntitySpanSet.Add(mSpan);
				}
			}
		}

		private static void ExtractNPorPRP(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			tree.IndexLeaves();
			SemanticGraph basicDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			SemanticGraph enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			if (enhancedDependency == null)
			{
				enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			}
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
				//      if(!mentionSpanSet.contains(mSpan) && (!insideNE(mSpan, namedEntitySpanSet)) ) {
				if (!mentionSpanSet.Contains(mSpan) && (!InsideNE(mSpan, namedEntitySpanSet) || t.Value().StartsWith("PRP")))
				{
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, beginIdx, endIdx, sent, basicDependency, enhancedDependency, new List<CoreLabel>(sent.SubList(beginIdx, endIdx)), t);
					mentions.Add(m);
					mentionSpanSet.Add(mSpan);
					if (m.originalSpan.Count > 1)
					{
						bool isNE = true;
						foreach (CoreLabel cl in m.originalSpan)
						{
							if (!cl.Tag().StartsWith("NNP"))
							{
								isNE = false;
							}
						}
						if (isNE)
						{
							namedEntitySpanSet.Add(mSpan);
						}
					}
				}
			}
		}
	}
}

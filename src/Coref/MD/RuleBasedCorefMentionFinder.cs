using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
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
	public class RuleBasedCorefMentionFinder : CorefMentionFinder
	{
		public RuleBasedCorefMentionFinder(IHeadFinder headFinder, Properties props)
			: this(true, headFinder, CorefProperties.GetLanguage(props))
		{
		}

		public RuleBasedCorefMentionFinder(bool allowReparsing, IHeadFinder headFinder, Locale lang)
		{
			this.headFinder = headFinder;
			this.allowReparsing = allowReparsing;
			this.lang = lang;
		}

		/// <summary>When mention boundaries are given</summary>
		public virtual IList<IList<Mention>> FilterPredictedMentions(IList<IList<Mention>> allGoldMentions, Annotation doc, Dictionaries dict, Properties props)
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
			}
			RemoveSpuriousMentions(doc, predictedMentions, dict, CorefProperties.RemoveNestedMentions(props), lang);
			return predictedMentions;
		}

		/// <summary>Main method of mention detection.</summary>
		/// <remarks>
		/// Main method of mention detection.
		/// Extract all NP, PRP or NE, and filter out by manually written patterns.
		/// </remarks>
		public override IList<IList<Mention>> FindMentions(Annotation doc, Dictionaries dict, Properties props)
		{
			IList<IList<Mention>> predictedMentions = new List<IList<Mention>>();
			ICollection<string> neStrings = Generics.NewHashSet();
			IList<ICollection<IntPair>> mentionSpanSetList = Generics.NewArrayList();
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
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
			if (CorefProperties.LiberalMD(props))
			{
				ExtractNamedEntityModifiers(sentences, mentionSpanSetList, predictedMentions, neStrings);
			}
			// find head
			for (int i = 0; i < sz; i++)
			{
				FindHead(sentences[i], predictedMentions[i]);
				SetBarePlural(predictedMentions[i]);
			}
			// mention selection based on document-wise info
			if (lang == Locale.English && !CorefProperties.LiberalMD(props))
			{
				RemoveSpuriousMentionsEn(doc, predictedMentions, dict);
			}
			else
			{
				if (lang == Locale.Chinese)
				{
					if (CorefProperties.LiberalMD(props))
					{
						RemoveSpuriousMentionsZhSimple(doc, predictedMentions, dict);
					}
					else
					{
						RemoveSpuriousMentionsZh(doc, predictedMentions, dict, CorefProperties.RemoveNestedMentions(props));
					}
				}
			}
			return predictedMentions;
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

		public virtual void ExtractNPorPRP(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
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
				//if (",".equals(sent.get(endIdx-1).word())) { endIdx--; } // try not to have span that ends with ,
				IntPair mSpan = new IntPair(beginIdx, endIdx);
				if (!mentionSpanSet.Contains(mSpan) && (lang == Locale.Chinese || !InsideNE(mSpan, namedEntitySpanSet)))
				{
					//      if(!mentionSpanSet.contains(mSpan) && (!insideNE(mSpan, namedEntitySpanSet) || t.value().startsWith("PRP")) ) {
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, beginIdx, endIdx, sent, basicDependency, enhancedDependency, new List<CoreLabel>(sent.SubList(beginIdx, endIdx)), t);
					mentions.Add(m);
					mentionSpanSet.Add(mSpan);
				}
			}
		}

		//        if(m.originalSpan.size() > 1) {
		//          boolean isNE = true;
		//          for(CoreLabel cl : m.originalSpan) {
		//            if(!cl.tag().startsWith("NNP")) isNE = false;
		//          }
		//          if(isNE) {
		//            namedEntitySpanSet.add(mSpan);
		//          }
		//        }
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
					if (!preNE.Matches("O|QUANTITY|CARDINAL|PERCENT|DATE|DURATION|TIME|SET"))
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
			if (!preNE.Matches("O|QUANTITY|CARDINAL|PERCENT|DATE|DURATION|TIME|SET"))
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

		private static void RemoveSpuriousMentionsZhSimple(Annotation doc, IList<IList<Mention>> predictedMentions, Dictionaries dict)
		{
			for (int i = 0; i < predictedMentions.Count; i++)
			{
				IList<Mention> mentions = predictedMentions[i];
				ICollection<Mention> remove = Generics.NewHashSet();
				foreach (Mention m in mentions)
				{
					if (m.originalSpan.Count == 1 && m.headWord.Tag().Equals("CD"))
					{
						remove.Add(m);
					}
					if (m.SpanToString().Contains("ｑｕｏｔ"))
					{
						remove.Add(m);
					}
				}
				mentions.RemoveAll(remove);
			}
		}

		/// <summary>Filter out all spurious mentions</summary>
		protected internal override void RemoveSpuriousMentionsEn(Annotation doc, IList<IList<Mention>> predictedMentions, Dictionaries dict)
		{
			ICollection<string> standAlones = new HashSet<string>();
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < predictedMentions.Count; i++)
			{
				ICoreMap s = sentences[i];
				IList<Mention> mentions = predictedMentions[i];
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
					if (m.originalSpan.Count > 0)
					{
						string firstWord = m.originalSpan[0].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower(Locale.English);
						if (firstWord.Matches("none|no|nothing|not"))
						{
							remove.Add(m);
						}
					}
					//          if(dict.quantifiers.contains(firstWord)) remove.add(m);
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
					//          if(m.generic==true) remove.add(m);
					if (m.headString.Equals("%"))
					{
						remove.Add(m);
					}
					if (headNE.Equals("PERCENT") || headNE.Equals("MONEY"))
					{
						remove.Add(m);
					}
					// adjective form of nations
					// the [American] policy -> not mention
					// speak in [Japanese] -> mention
					// check if the mention is noun and the next word is not noun
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
		}
	}
}

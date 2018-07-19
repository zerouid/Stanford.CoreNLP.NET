using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.MD
{
	public class DependencyCorefMentionFinder : CorefMentionFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.MD.DependencyCorefMentionFinder));

		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.IO.IOException"/>
		public DependencyCorefMentionFinder(Properties props)
		{
			this.lang = CorefProperties.GetLanguage(props);
			mdClassifier = (CorefProperties.IsMentionDetectionTraining(props)) ? null : IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(CorefProperties.GetMentionDetectionModel(props));
		}

		public MentionDetectionClassifier mdClassifier = null;

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
			foreach (ICoreMap s in sentences)
			{
				IList<Mention> mentions = new List<Mention>();
				predictedMentions.Add(mentions);
				ICollection<IntPair> mentionSpanSet = Generics.NewHashSet();
				ICollection<IntPair> namedEntitySpanSet = Generics.NewHashSet();
				ExtractPremarkedEntityMentions(s, mentions, mentionSpanSet, namedEntitySpanSet);
				HybridCorefMentionFinder.ExtractNamedEntityMentions(s, mentions, mentionSpanSet, namedEntitySpanSet);
				ExtractNPorPRPFromDependency(s, mentions, mentionSpanSet, namedEntitySpanSet);
				AddNamedEntityStrings(s, neStrings, namedEntitySpanSet);
				mentionSpanSetList.Add(mentionSpanSet);
			}
			//    extractNamedEntityModifiers(sentences, mentionSpanSetList, predictedMentions, neStrings);
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

		private void ExtractNPorPRPFromDependency(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph basic = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			IList<IndexedWord> nounsOrPrp = basic.GetAllNodesByPartOfSpeechPattern("N.*|PRP.*|DT");
			// DT is for "this, these, etc"
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			foreach (IndexedWord w in nounsOrPrp)
			{
				SemanticGraphEdge edge = basic.GetEdge(basic.GetParent(w), w);
				GrammaticalRelation rel = null;
				string shortname = "root";
				// if edge is null, it's root
				if (edge != null)
				{
					rel = edge.GetRelation();
					shortname = rel.GetShortName();
				}
				// TODO: what to remove? remove more?
				if (shortname.Matches("det|compound"))
				{
					//        // for debug  ---------------
					//        Tree t = tree.getLeaves().get(w.index()-1);
					//        for(Tree p : tree.pathNodeToNode(t, tree)) {
					//          if(p.label().value().equals("NP")) {
					//            HeadFinder headFinder = new SemanticHeadFinder();
					//            Tree head = headFinder.determineHead(p);
					//            if(head == t.parent(tree)) {
					//              log.info();
					//            }
					//            break;
					//          }
					//        } // for debug -------------
					continue;
				}
				else
				{
					ExtractMentionForHeadword(w, basic, s, mentions, mentionSpanSet, namedEntitySpanSet);
				}
			}
		}

		private void ExtractMentionForHeadword(IndexedWord headword, SemanticGraph dep, ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph basic = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			SemanticGraph enhanced = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			if (enhanced == null)
			{
				enhanced = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			}
			// pronoun
			if (headword.Tag().StartsWith("PRP"))
			{
				ExtractPronounForHeadword(headword, dep, s, mentions, mentionSpanSet, namedEntitySpanSet);
				return;
			}
			// add NP mention
			IntPair npSpan = GetNPSpan(headword, dep, sent);
			int beginIdx = npSpan.Get(0);
			int endIdx = npSpan.Get(1) + 1;
			if (",".Equals(sent[endIdx - 1].Word()))
			{
				endIdx--;
			}
			// try not to have span that ends with ,
			if ("IN".Equals(sent[beginIdx].Tag()))
			{
				beginIdx++;
			}
			// try to remove first IN.
			AddMention(beginIdx, endIdx, headword, mentions, mentionSpanSet, namedEntitySpanSet, sent, basic, enhanced);
			//
			// extract the first element in conjunction (A and B -> extract A here "A and B", "B" will be extracted above)
			//
			// to make sure we find the first conjunction
			ICollection<IndexedWord> conjChildren = dep.GetChildrenWithReln(headword, UniversalEnglishGrammaticalRelations.Conjunct);
			if (conjChildren.Count > 0)
			{
				IndexedWord conjChild = dep.GetChildWithReln(headword, UniversalEnglishGrammaticalRelations.Conjunct);
				foreach (IndexedWord c in conjChildren)
				{
					if (c.Index() < conjChild.Index())
					{
						conjChild = c;
					}
				}
				IndexedWord left = SemanticGraphUtils.LeftMostChildVertice(conjChild, dep);
				for (int endIdxFirstElement = left.Index() - 1; endIdxFirstElement > beginIdx; endIdxFirstElement--)
				{
					if (!sent[endIdxFirstElement - 1].Tag().Matches("CC|,"))
					{
						if (headword.Index() - 1 < endIdxFirstElement)
						{
							AddMention(beginIdx, endIdxFirstElement, headword, mentions, mentionSpanSet, namedEntitySpanSet, sent, basic, enhanced);
						}
						break;
					}
				}
			}
		}

		/// <summary>
		/// return the left and right most node except copula relation (nsubj & cop) and some others (maybe discourse?)
		/// e.g., you are the person -&gt; return "the person"
		/// </summary>
		private IntPair GetNPSpan(IndexedWord headword, SemanticGraph dep, IList<CoreLabel> sent)
		{
			int headwordIdx = headword.Index() - 1;
			IList<IndexedWord> children = dep.GetChildList(headword);
			//    if(children.size()==0) return new IntPair(headwordIdx, headwordIdx);    // the headword is the only word
			// check if we have copula relation
			IndexedWord cop = dep.GetChildWithReln(headword, UniversalEnglishGrammaticalRelations.Copula);
			int startIdx = (cop == null) ? 0 : children.IndexOf(cop) + 1;
			// children which will be inside of NP
			IList<IndexedWord> insideNP = Generics.NewArrayList();
			for (int i = startIdx; i < children.Count; i++)
			{
				IndexedWord child = children[i];
				SemanticGraphEdge edge = dep.GetEdge(headword, child);
				if (edge.GetRelation().GetShortName().Matches("dep|discourse|punct"))
				{
					continue;
				}
				else
				{
					// skip
					insideNP.Add(child);
				}
			}
			if (insideNP.Count == 0)
			{
				return new IntPair(headwordIdx, headwordIdx);
			}
			// the headword is the only word
			Pair<IndexedWord, IndexedWord> firstChildLeftRight = SemanticGraphUtils.LeftRightMostChildVertices(insideNP[0], dep);
			Pair<IndexedWord, IndexedWord> lastChildLeftRight = SemanticGraphUtils.LeftRightMostChildVertices(insideNP[insideNP.Count - 1], dep);
			// headword can be first or last word
			int beginIdx = Math.Min(headwordIdx, firstChildLeftRight.first.Index() - 1);
			int endIdx = Math.Max(headwordIdx, lastChildLeftRight.second.Index() - 1);
			return new IntPair(beginIdx, endIdx);
		}

		private IntPair GetNPSpanOld(IndexedWord headword, SemanticGraph dep, IList<CoreLabel> sent)
		{
			IndexedWord cop = dep.GetChildWithReln(headword, UniversalEnglishGrammaticalRelations.Copula);
			Pair<IndexedWord, IndexedWord> leftRight = SemanticGraphUtils.LeftRightMostChildVertices(headword, dep);
			// headword can be first or last word
			int beginIdx = Math.Min(headword.Index() - 1, leftRight.first.Index() - 1);
			int endIdx = Math.Max(headword.Index() - 1, leftRight.second.Index() - 1);
			// no copula relation
			if (cop == null)
			{
				return new IntPair(beginIdx, endIdx);
			}
			// if we have copula relation
			IList<IndexedWord> children = dep.GetChildList(headword);
			int copIdx = children.IndexOf(cop);
			if (copIdx + 1 < children.Count)
			{
				beginIdx = Math.Min(headword.Index() - 1, SemanticGraphUtils.LeftMostChildVertice(children[copIdx + 1], dep).Index() - 1);
			}
			else
			{
				beginIdx = headword.Index() - 1;
			}
			return new IntPair(beginIdx, endIdx);
		}

		private void AddMention(int beginIdx, int endIdx, IndexedWord headword, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet, IList<CoreLabel> sent, SemanticGraph basic, SemanticGraph enhanced
			)
		{
			IntPair mSpan = new IntPair(beginIdx, endIdx);
			if (!mentionSpanSet.Contains(mSpan) && (!InsideNE(mSpan, namedEntitySpanSet)))
			{
				int dummyMentionId = -1;
				Mention m = new Mention(dummyMentionId, beginIdx, endIdx, sent, basic, enhanced, new List<CoreLabel>(sent.SubList(beginIdx, endIdx)));
				m.headIndex = headword.Index() - 1;
				m.headWord = sent[m.headIndex];
				m.headString = m.headWord.Word().ToLower(Locale.English);
				mentions.Add(m);
				mentionSpanSet.Add(mSpan);
			}
		}

		private void ExtractPronounForHeadword(IndexedWord headword, SemanticGraph dep, ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph basic = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			SemanticGraph enhanced = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			if (enhanced == null)
			{
				enhanced = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			}
			int beginIdx = headword.Index() - 1;
			int endIdx = headword.Index();
			// handle "you all", "they both" etc
			if (sent.Count > headword.Index() && sent[headword.Index()].Word().Matches("all|both"))
			{
				IndexedWord c = dep.GetNodeByIndex(headword.Index() + 1);
				SemanticGraphEdge edge = dep.GetEdge(headword, c);
				if (edge != null)
				{
					endIdx++;
				}
			}
			IntPair mSpan = new IntPair(beginIdx, endIdx);
			if (!mentionSpanSet.Contains(mSpan) && (!InsideNE(mSpan, namedEntitySpanSet)))
			{
				int dummyMentionId = -1;
				Mention m = new Mention(dummyMentionId, beginIdx, endIdx, sent, basic, enhanced, new List<CoreLabel>(sent.SubList(beginIdx, endIdx)));
				m.headIndex = headword.Index() - 1;
				m.headWord = sent[m.headIndex];
				m.headString = m.headWord.Word().ToLower(Locale.English);
				mentions.Add(m);
				mentionSpanSet.Add(mSpan);
			}
			// when pronoun is a part of conjunction (e.g., you and I)
			ICollection<IndexedWord> conjChildren = dep.GetChildrenWithReln(headword, UniversalEnglishGrammaticalRelations.Conjunct);
			if (conjChildren.Count > 0)
			{
				IntPair npSpan = GetNPSpan(headword, dep, sent);
				beginIdx = npSpan.Get(0);
				endIdx = npSpan.Get(1) + 1;
				if (",".Equals(sent[endIdx - 1].Word()))
				{
					endIdx--;
				}
				// try not to have span that ends with ,
				AddMention(beginIdx, endIdx, headword, mentions, mentionSpanSet, namedEntitySpanSet, sent, basic, enhanced);
			}
		}

		public static void FindHeadInDependency(ICoreMap s, IList<Mention> mentions)
		{
			foreach (Mention m in mentions)
			{
				FindHeadInDependency(s, m);
			}
		}

		public override void FindHead(ICoreMap s, IList<Mention> mentions)
		{
			foreach (Mention m in mentions)
			{
				FindHeadInDependency(s, m);
			}
		}

		// TODO: still errors in head finder
		public static void FindHeadInDependency(ICoreMap s, Mention m)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph basicDep = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			if (m.headWord == null)
			{
				// when there's punctuation, no node found in the dependency tree
				int curIdx;
				IndexedWord cur = null;
				for (curIdx = m.endIndex - 1; curIdx >= m.startIndex; curIdx--)
				{
					if ((cur = basicDep.GetNodeByIndexSafe(curIdx + 1)) != null)
					{
						break;
					}
				}
				if (cur == null)
				{
					curIdx = m.endIndex - 1;
				}
				while (cur != null)
				{
					IndexedWord p = basicDep.GetParent(cur);
					if (p == null || p.Index() - 1 < m.startIndex || p.Index() - 1 >= m.endIndex)
					{
						break;
					}
					curIdx = p.Index() - 1;
					cur = basicDep.GetNodeByIndexSafe(curIdx + 1);
				}
				//      for(IndexedWord p : basicDep.getPathToRoot(basicDep.getNodeByIndex(curIdx+1))) {
				//        if(p.index()-1 < m.startIndex || p.index()-1 >= m.endIndex) {
				//          break;
				//        }
				//        curIdx = p.index()-1;
				//      }
				m.headIndex = curIdx;
				m.headWord = sent[m.headIndex];
				m.headString = m.headWord.Word().ToLower(Locale.English);
			}
		}
	}
}

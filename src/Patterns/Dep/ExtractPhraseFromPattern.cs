using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Patterns.Dep
{
	public class ExtractPhraseFromPattern
	{
		public IList<string> cutoffRelations = new List<string>();

		public int maxDepth = int.MaxValue;

		public static IList<string> ignoreTags = Arrays.AsList("PRP", "PRP$", "CD", "DT", ".", "..", ",", "SYM");

		internal bool ignoreCommonTags = true;

		public static List<string> cutoffTags = new List<string>();

		public int maxPhraseLength = int.MaxValue;

		internal IDictionary<SemgrexPattern, IList<Pair<string, SemanticGraph>>> matchedGraphsForPattern = new Dictionary<SemgrexPattern, IList<Pair<string, SemanticGraph>>>();

		private static int Debug = 1;

		public ExtractPhraseFromPattern()
		{
		}

		public ExtractPhraseFromPattern(bool ignoreCommonTags, int maxPhraseLength)
		{
			//import org.jdom.Element;
			//import org.jdom.Namespace;
			//Namespace curNS;
			// 0 means none, 1 means partial, 2 means it shows sentences and their
			// techniques, app and focus, and 3 means full
			this.maxPhraseLength = maxPhraseLength;
			this.ignoreCommonTags = ignoreCommonTags;
		}

		//this.curNS = null;
		public virtual void SetMaxPhraseLength(int maxPhraseLength)
		{
			this.maxPhraseLength = maxPhraseLength;
		}

		//public ExtractPhraseFromPattern(Namespace curNS) {
		//  this.curNS = curNS;
		//}
		private bool CheckIfSatisfiedMaxDepth(SemanticGraph g, IndexedWord parent, IndexedWord child, IntPair depths)
		{
			if (depths.Get(0) == int.MaxValue)
			{
				return true;
			}
			if (parent.Equals(child))
			{
				return true;
			}
			bool foundInMaxDepth = false;
			foreach (IndexedWord c in g.GetChildren(parent))
			{
				if (c.Equals(child))
				{
					return true;
				}
			}
			depths.Set(1, depths.Get(1) + 1);
			if (depths.Get(1) >= depths.Get(0))
			{
				return false;
			}
			foreach (IndexedWord c_1 in g.GetChildren(parent))
			{
				foundInMaxDepth = CheckIfSatisfiedMaxDepth(g, c_1, child, depths);
				if (foundInMaxDepth == true)
				{
					return foundInMaxDepth;
				}
			}
			return false;
		}

		public virtual void ProcessSentenceForType(SemanticGraph g, IList<SemgrexPattern> typePatterns, IList<string> textTokens, ICollection<string> typePhrases, ICollection<IntPair> typeIndices, ICollection<IndexedWord> typeTriggerWords, bool findSubTrees
			, ICollection<ExtractedPhrase> extractedPhrases, bool lowercase)
		{
			foreach (SemgrexPattern pattern in typePatterns)
			{
				ICollection<IndexedWord> triggerWords = GetSemGrexPatternNodes(g, textTokens, typePhrases, typeIndices, pattern, findSubTrees, extractedPhrases, lowercase, null);
				foreach (IndexedWord w in triggerWords)
				{
					if (!typeTriggerWords.Contains(w))
					{
						typeTriggerWords.Add(w);
					}
				}
			}
		}

		// System.out.println("the string is " + StringUtils.join(focuss, ";"));
		/*
		* Given a SemanticGraph g and a SemgrexPattern pattern
		* And a bunch of other parameters,
		* run the pattern matcher (get SemgrexMatcher m)
		* Iterate through to get matching words/phrases
		*
		* Next, gets matchedGraphsForPattern.get(pattern),
		* a list of matched (String, semgraph) pairs
		* and adds the new graph and tokens if matched.
		*
		* I need to clarify what's going on with tokens.
		*/
		public virtual ICollection<IndexedWord> GetSemGrexPatternNodes(SemanticGraph g, IList<string> tokens, ICollection<string> outputNodes, ICollection<IntPair> outputIndices, SemgrexPattern pattern, bool findSubTrees, ICollection<ExtractedPhrase
			> extractedPhrases, bool lowercase, IPredicate<CoreLabel> acceptWord)
		{
			ICollection<IndexedWord> foundWordsParents = new HashSet<IndexedWord>();
			SemgrexMatcher m = pattern.Matcher(g, lowercase);
			while (m.Find())
			{
				IndexedWord w = m.GetNode("node");
				//System.out.println("found a match for " + pattern.pattern());
				IndexedWord parent = m.GetNode("parent");
				bool ifSatisfiedMaxDepth = CheckIfSatisfiedMaxDepth(g, parent, w, new IntPair(maxDepth, 0));
				if (ifSatisfiedMaxDepth == false)
				{
					continue;
				}
				if (Debug > 3)
				{
					IList<Pair<string, SemanticGraph>> matchedGraphs = matchedGraphsForPattern[pattern];
					if (matchedGraphs == null)
					{
						matchedGraphs = new List<Pair<string, SemanticGraph>>();
					}
					matchedGraphs.Add(new Pair<string, SemanticGraph>(StringUtils.Join(tokens, " "), g));
					//if (DEBUG >= 3)
					//  System.out.println("matched pattern is " + pattern);
					matchedGraphsForPattern[pattern] = matchedGraphs;
				}
				foundWordsParents.Add(parent);
				// String relationName = m.getRelnString("reln");
				// System.out.println("word is " + w.lemma() + " and " + w.tag());
				List<IndexedWord> seenNodes = new List<IndexedWord>();
				IList<string> cutoffrelations = new List<string>();
				//      if (elementStr.equalsIgnoreCase("technique"))
				//        cutoffrelations = cutoffRelationsForTech;
				//      if (elementStr.equalsIgnoreCase("app"))
				//        cutoffrelations = this.cuttoffRelationsForApp;
				//System.out.println("g is ");
				//g.prettyPrint();
				PrintSubGraph(g, w, cutoffrelations, tokens, outputNodes, outputIndices, seenNodes, new List<IndexedWord>(), findSubTrees, extractedPhrases, pattern, acceptWord);
			}
			return foundWordsParents;
		}

		//Here, the index (startIndex, endIndex) seems to be inclusive of the endIndex
		public virtual void PrintSubGraph(SemanticGraph g, IndexedWord w, IList<string> additionalCutOffRels, IList<string> textTokens, ICollection<string> listOfOutput, ICollection<IntPair> listOfOutputIndices, IList<IndexedWord> seenNodes, IList<IndexedWord
			> doNotAddThese, bool findSubTrees, ICollection<ExtractedPhrase> extractedPhrases, SemgrexPattern pattern, IPredicate<CoreLabel> acceptWord)
		{
			try
			{
				if (seenNodes.Contains(w))
				{
					return;
				}
				seenNodes.Add(w);
				if (doNotAddThese.Contains(w))
				{
					return;
				}
				IList<IndexedWord> andNodes = new List<IndexedWord>();
				DescendantsWithReln(g, w, "conj_and", new List<IndexedWord>(), andNodes);
				//System.out.println("and nodes are " + andNodes);
				foreach (IndexedWord w1 in andNodes)
				{
					PrintSubGraph(g, w1, additionalCutOffRels, textTokens, listOfOutput, listOfOutputIndices, seenNodes, doNotAddThese, findSubTrees, extractedPhrases, pattern, acceptWord);
				}
				Sharpen.Collections.AddAll(doNotAddThese, andNodes);
				IList<string> allCutOffRels = new List<string>();
				if (additionalCutOffRels != null)
				{
					Sharpen.Collections.AddAll(allCutOffRels, additionalCutOffRels);
				}
				Sharpen.Collections.AddAll(allCutOffRels, cutoffRelations);
				CollectionValuedMap<int, string> featPerToken = new CollectionValuedMap<int, string>();
				ICollection<string> feat = new List<string>();
				GetPatternsFromDataMultiClass.GetFeatures(g, w, true, feat, null);
				ICollection<IndexedWord> words = Descendants(g, w, allCutOffRels, doNotAddThese, ignoreCommonTags, acceptWord, featPerToken);
				// words.addAll(andNodes);
				// if (includeSiblings == true) {
				// for (IndexedWord ws : g.getSiblings(w)) {
				// if (additionalCutOffNodes == null
				// || !additionalCutOffNodes.contains(g.reln(g.getParent(w),
				// ws).getShortName()))
				// words.addAll(descendants(g, ws, additionalCutOffNodes, doNotAddThese));
				// }
				// }
				// if(afterand != null){
				// Set<IndexedWord> wordsAnd = descendants(g,afterand,
				// additionalCutOffNodes);
				// words.removeAll(wordsAnd);
				// printSubGraph(g,afterand, includeSiblings, additionalCutOffNodes);
				// }
				//System.out.println("words are " + words);
				if (words.Count > 0)
				{
					int min = int.MaxValue;
					int max = -1;
					foreach (IndexedWord word in words)
					{
						if (word.Index() < min)
						{
							min = word.Index();
						}
						if (word.Index() > max)
						{
							max = word.Index();
						}
					}
					IntPair indices;
					// Map<Integer, String> ph = new TreeMap<Integer, String>();
					// String phrase = "";
					// for (IndexedWord word : words) {
					// ph.put(word.index(), word.value());
					// }
					// phrase = StringUtils.join(ph.values(), " ");
					if ((max - min + 1) > maxPhraseLength)
					{
						max = min + maxPhraseLength - 1;
					}
					indices = new IntPair(min - 1, max - 1);
					string phrase = StringUtils.Join(textTokens.SubList(min - 1, max), " ");
					phrase = phrase.Trim();
					feat.Add("LENGTH-" + (max - min + 1));
					for (int i = min; i <= max; i++)
					{
						Sharpen.Collections.AddAll(feat, featPerToken[i]);
					}
					//System.out.println("phrase is " + phrase  + " index is " + indices + " and maxphraselength is " + maxPhraseLength + " and descendentset is " + words);
					ExtractedPhrase extractedPh = new ExtractedPhrase(min - 1, max - 1, pattern, phrase, Counters.AsCounter(feat));
					if (!listOfOutput.Contains(phrase) && !doNotAddThese.Contains(phrase))
					{
						//          if (sentElem != null) {
						//            Element node = new Element(elemString, curNS);
						//            node.addContent(phrase);
						//            sentElem.addContent(node);
						//          }
						listOfOutput.Add(phrase);
						if (!listOfOutputIndices.Contains(indices))
						{
							listOfOutputIndices.Add(indices);
							extractedPhrases.Add(extractedPh);
						}
						if (findSubTrees == true)
						{
							foreach (IndexedWord word_1 in words)
							{
								if (!seenNodes.Contains(word_1))
								{
									PrintSubGraph(g, word_1, additionalCutOffRels, textTokens, listOfOutput, listOfOutputIndices, seenNodes, doNotAddThese, findSubTrees, extractedPhrases, pattern, acceptWord);
								}
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <exception cref="System.Exception"/>
		public static ICollection<IndexedWord> Descendants(SemanticGraph g, IndexedWord vertex, IList<string> allCutOffRels, IList<IndexedWord> doNotAddThese, bool ignoreCommonTags, IPredicate<CoreLabel> acceptWord, CollectionValuedMap<int, string> 
			feat)
		{
			// Do a depth first search
			ICollection<IndexedWord> descendantSet = new HashSet<IndexedWord>();
			if (doNotAddThese != null && doNotAddThese.Contains(vertex))
			{
				return descendantSet;
			}
			if (!acceptWord.Test(vertex.BackingLabel()))
			{
				return descendantSet;
			}
			DescendantsHelper(g, vertex, descendantSet, allCutOffRels, doNotAddThese, new List<IndexedWord>(), ignoreCommonTags, acceptWord, feat);
			//    String descStr = "";
			//    for(IndexedWord descendant: descendantSet){
			//      descStr += descendant.word()+" ";
			//    }
			//    System.out.println(descStr);
			return descendantSet;
		}

		internal static bool CheckIfSatisfiesRelConstrains(SemanticGraph g, IndexedWord curr, IndexedWord child, IList<string> allCutOffRels, GrammaticalRelation rel)
		{
			string relName = rel.GetShortName();
			string relSpecificName = rel.ToString();
			string relFullName = rel.GetLongName();
			if (allCutOffRels != null)
			{
				foreach (string check in allCutOffRels)
				{
					if (relName.Matches(check) || (relSpecificName != null && relSpecificName.Matches(check)) || (relFullName != null && relFullName.Matches(check)))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <exception cref="System.Exception"/>
		private static void DescendantsHelper(SemanticGraph g, IndexedWord curr, ICollection<IndexedWord> descendantSet, IList<string> allCutOffRels, IList<IndexedWord> doNotAddThese, IList<IndexedWord> seenNodes, bool ignoreCommonTags, IPredicate<CoreLabel
			> acceptWord, CollectionValuedMap<int, string> feat)
		{
			if (seenNodes.Contains(curr))
			{
				return;
			}
			seenNodes.Add(curr);
			if (descendantSet.Contains(curr) || (doNotAddThese != null && doNotAddThese.Contains(curr)) || !acceptWord.Test(curr.BackingLabel()))
			{
				return;
			}
			if (!ignoreCommonTags || !ignoreTags.Contains(curr.Tag().Trim()))
			{
				descendantSet.Add(curr);
			}
			foreach (IndexedWord child in g.GetChildren(curr))
			{
				bool dontuse = false;
				if (doNotAddThese != null && doNotAddThese.Contains(child))
				{
					dontuse = true;
				}
				GrammaticalRelation rel = null;
				if (dontuse == false)
				{
					rel = g.Reln(curr, child);
					dontuse = CheckIfSatisfiesRelConstrains(g, curr, child, allCutOffRels, rel);
				}
				if (dontuse == false)
				{
					foreach (string cutOffTagRegex in cutoffTags)
					{
						if (child.Tag().Matches(cutOffTagRegex))
						{
							if (Debug >= 5)
							{
								System.Console.Out.WriteLine("ignored tag " + child + " because it satisfied " + cutOffTagRegex);
							}
							dontuse = true;
							break;
						}
					}
				}
				if (dontuse == false)
				{
					if (!feat.Contains(curr.Index()))
					{
						feat[curr.Index()] = new List<string>();
					}
					GetPatternsFromDataMultiClass.GetFeatures(g, curr, false, feat[curr.Index()], rel);
					//feat.add(curr.index(), "REL-" + rel.getShortName());
					DescendantsHelper(g, child, descendantSet, allCutOffRels, doNotAddThese, seenNodes, ignoreCommonTags, acceptWord, feat);
				}
			}
		}

		// get descendants that have this relation
		private void DescendantsWithReln(SemanticGraph g, IndexedWord w, string relation, IList<IndexedWord> seenNodes, IList<IndexedWord> descendantSet)
		{
			if (seenNodes.Contains(w))
			{
				return;
			}
			seenNodes.Add(w);
			if (descendantSet.Contains(w))
			{
				return;
			}
			if (ignoreCommonTags && ignoreTags.Contains(w.Tag().Trim()))
			{
				return;
			}
			foreach (IndexedWord child in g.GetChildren(w))
			{
				foreach (SemanticGraphEdge edge in g.GetAllEdges(w, child))
				{
					if (edge.GetRelation().ToString().Equals(relation))
					{
						descendantSet.Add(child);
					}
				}
				DescendantsWithReln(g, child, relation, seenNodes, descendantSet);
			}
		}

		/// <exception cref="System.Exception"/>
		public virtual void PrintMatchedGraphsForPattern(string filename, int maxGraphsPerPattern)
		{
			BufferedWriter w = new BufferedWriter(new FileWriter(filename));
			foreach (KeyValuePair<SemgrexPattern, IList<Pair<string, SemanticGraph>>> en in matchedGraphsForPattern)
			{
				w.Write("\n\nFor Pattern: " + en.Key.Pattern() + "\n");
				int num = 0;
				foreach (Pair<string, SemanticGraph> gEn in en.Value)
				{
					num++;
					if (num > maxGraphsPerPattern)
					{
						break;
					}
					w.Write(gEn.First() + "\n" + gEn.Second().ToFormattedString() + "\n\n");
				}
			}
			w.Close();
		}
	}
}

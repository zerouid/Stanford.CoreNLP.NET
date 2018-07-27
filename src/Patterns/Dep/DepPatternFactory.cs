using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Patterns.Dep
{
	/// <summary>Created by Sonal Gupta on 10/31/14.</summary>
	public class DepPatternFactory : PatternFactory
	{
		internal static string ignoreRels = string.Empty;

		internal static int upDepth = 2;

		internal static string allowedTagsForTrigger = ".*";

		internal static ICollection<Pattern> allowedTagPatternForTrigger = new HashSet<Pattern>();

		internal static ICollection<GrammaticalRelation> ignoreRelsSet = new HashSet<GrammaticalRelation>();

		public static void SetUp(Properties props)
		{
			ArgumentParser.FillOptions(typeof(DepPatternFactory), props);
			ArgumentParser.FillOptions(typeof(PatternFactory), props);
			foreach (string s in ignoreRels.Split("[,;]"))
			{
				ignoreRelsSet.Add(GrammaticalRelation.ValueOf(s));
			}
			foreach (string s_1 in allowedTagsForTrigger.Split("[,;]"))
			{
				allowedTagPatternForTrigger.Add(Pattern.Compile(s_1));
			}
		}

		public static IDictionary<int, ICollection<DepPattern>> GetPatternsAroundTokens(DataInstance sent, ICollection<CandidatePhrase> stopWords)
		{
			return GetPatternsForAllPhrases(sent, stopWords);
		}

		internal static IDictionary<int, ICollection<DepPattern>> GetPatternsForAllPhrases(DataInstance sent, ICollection<CandidatePhrase> commonWords)
		{
			SemanticGraph graph = ((DataInstanceDep)sent).GetGraph();
			IDictionary<int, ICollection<DepPattern>> pats4Sent = new Dictionary<int, ICollection<DepPattern>>();
			if (graph == null || graph.IsEmpty())
			{
				System.Console.Out.WriteLine("graph is empty or null!");
				return null;
			}
			ICollection<IndexedWord> allNodes;
			try
			{
				allNodes = graph.GetLeafVertices();
			}
			catch (ArgumentException)
			{
				return null;
			}
			foreach (IndexedWord w in allNodes)
			{
				//because index starts at 1!!!!
				pats4Sent[w.Index() - 1] = GetContext(w, graph, commonWords, sent);
			}
			return pats4Sent;
		}

		public static DepPattern PatternToDepPattern(Pair<IndexedWord, GrammaticalRelation> p, DataInstance sent)
		{
			Token token = new Token(PatternFactory.PatternType.Dep);
			CoreLabel backingLabel = sent.GetTokens()[p.First().Index() - 1];
			System.Diagnostics.Debug.Assert(backingLabel.ContainsKey(typeof(PatternsAnnotations.ProcessedTextAnnotation)), "the keyset are " + backingLabel.ToString(CoreLabel.OutputFormat.All));
			token.AddORRestriction(typeof(PatternsAnnotations.ProcessedTextAnnotation), backingLabel.Get(typeof(PatternsAnnotations.ProcessedTextAnnotation)));
			return new DepPattern(token, p.Second());
		}

		private static bool IfIgnoreRel(GrammaticalRelation rel)
		{
			if (ignoreRelsSet.Contains(rel))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		internal static ICollection<DepPattern> GetContext(IndexedWord w, SemanticGraph graph, ICollection<CandidatePhrase> stopWords, DataInstance sent)
		{
			ICollection<DepPattern> patterns = new HashSet<DepPattern>();
			IndexedWord node = w;
			int depth = 1;
			while (depth <= upDepth)
			{
				IndexedWord parent = graph.GetParent(node);
				if (parent == null)
				{
					break;
				}
				GrammaticalRelation rel = graph.Reln(parent, node);
				foreach (Pattern tagPattern in allowedTagPatternForTrigger)
				{
					if (tagPattern.Matcher(parent.Tag()).Matches())
					{
						if (!IfIgnoreRel(rel) && !stopWords.Contains(CandidatePhrase.CreateOrGet(parent.Word())) && parent.Word().Length > 1)
						{
							Pair<IndexedWord, GrammaticalRelation> pattern = new Pair<IndexedWord, GrammaticalRelation>(parent, rel);
							DepPattern patterndep = PatternToDepPattern(pattern, sent);
							if (depth <= upDepth)
							{
								patterns.Add(patterndep);
							}
						}
					}
				}
				//                    if (depth <= maxDepth) {
				//                      Counter<String> phrasesForPattern = phrasesForPatternForSent.get(patternStr);
				//                      if (phrasesForPattern == null)
				//                        phrasesForPattern = new ClassicCounter<String>();
				//                      phrasesForPattern.incrementCount(phrase);
				//                      phrasesForPatternForSent.put(patternStr, phrasesForPattern);
				//                    }
				//                    if (DEBUG >= 1)
				//                      System.out.println("for phrase " + phrase + " pattern is " + patternStr);
				node = parent;
				depth++;
			}
			return patterns;
		}

		public static ISet GetContext(DataInstance sent, int i, ICollection<CandidatePhrase> stopWords)
		{
			SemanticGraph graph = ((DataInstanceDep)sent).GetGraph();
			//nodes are indexed from 1 -- so wrong!!
			try
			{
				IndexedWord w = graph.GetNodeByIndex(i + 1);
				return GetContext(w, graph, stopWords, sent);
			}
			catch (ArgumentException)
			{
				return Java.Util.Collections.EmptySet();
			}
		}
	}
}

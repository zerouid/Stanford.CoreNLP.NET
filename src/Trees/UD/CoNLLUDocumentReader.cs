using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <summary>Reader for ConLL-U formatted dependency treebanks.</summary>
	/// <author>Sebastian Schuster</author>
	[System.Serializable]
	public class CoNLLUDocumentReader : IIteratorFromReaderFactory<SemanticGraph>
	{
		private const string CommentPos = "<COMMENT>";

		private const long serialVersionUID = -7340310509954331983L;

		private IIteratorFromReaderFactory<SemanticGraph> ifrf;

		public CoNLLUDocumentReader()
		{
			this.ifrf = DelimitRegExIterator.GetFactory("\n(\\s*\n)+", new CoNLLUDocumentReader.SentenceProcessor());
		}

		public virtual IEnumerator<SemanticGraph> GetIterator(Reader r)
		{
			return ifrf.GetIterator(r);
		}

		private static readonly IComparator<IndexedWord> byIndex = IComparer.NaturalOrder();

		/// <summary>Comparator for putting multiword tokens before regular tokens.</summary>
		private static readonly IComparator<IndexedWord> byType = null;

		private class SentenceProcessor : IFunction<string, SemanticGraph>
		{
			private int lineNumberCounter = 0;

			private static Pair<IndexedWord, GrammaticalRelation> GetGovAndReln(int govIdx, int copyCount, IndexedWord word, string relationName, IList<IndexedWord> sortedTokens)
			{
				IndexedWord gov;
				GrammaticalRelation reln;
				if (relationName.Equals("root"))
				{
					reln = GrammaticalRelation.Root;
				}
				else
				{
					reln = GrammaticalRelation.ValueOf(Language.UniversalEnglish, relationName);
				}
				if (govIdx == 0)
				{
					gov = new IndexedWord(word.DocID(), word.SentIndex(), 0);
					gov.SetValue("ROOT");
				}
				else
				{
					gov = CoNLLUDocumentReader.SentenceProcessor.GetToken(sortedTokens, govIdx, copyCount);
				}
				return Generics.NewPair(gov, reln);
			}

			private static IndexedWord GetToken(IList<IndexedWord> sortedTokens, int index)
			{
				return CoNLLUDocumentReader.SentenceProcessor.GetToken(sortedTokens, index, 0);
			}

			private static IndexedWord GetToken(IList<IndexedWord> sortedTokens, int index, int copyCount)
			{
				int tokenLength = sortedTokens.Count;
				for (int i = index - 1; i < tokenLength; i++)
				{
					IndexedWord token = sortedTokens[i];
					if (token.Index() == index && token.CopyCount() == copyCount)
					{
						return token;
					}
				}
				return null;
			}

			public virtual SemanticGraph Apply(string line)
			{
				if (line == null)
				{
					return null;
				}
				IFunction<string, IndexedWord> func = new CoNLLUDocumentReader.WordProcessor();
				ObjectBank<IndexedWord> words = ObjectBank.GetLineIterator(new StringReader(line), func);
				IList<IndexedWord> wordList = new List<IndexedWord>(words);
				IList<IndexedWord> sorted = new List<IndexedWord>(wordList.Count);
				IList<string> comments = new LinkedList<string>();
				/* Increase the line number in case there are comments before the actual sentence
				* and add them to the list of comments. */
				wordList.Stream().Filter(null).ForEach(null);
				wordList.Stream().Filter(null).Sorted(byIndex.ThenComparing(byType)).ForEach(null);
				IList<IndexedWord> sortedTokens = new List<IndexedWord>(wordList.Count);
				sorted.Stream().Filter(null).Filter(null).ForEach(null);
				sorted.Stream().Filter(null).Filter(null).ForEach(null);
				/* Construct a semantic graph. */
				IList<TypedDependency> deps = new List<TypedDependency>(sorted.Count);
				IntPair tokenSpan = null;
				string originalToken = null;
				foreach (IndexedWord word in sorted)
				{
					lineNumberCounter++;
					if (word.ContainsKey(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation)))
					{
						tokenSpan = word.Get(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation));
						originalToken = word.Word();
					}
					else
					{
						/* Deal with multiword tokens. */
						if (tokenSpan != null && tokenSpan.GetTarget() >= word.Index())
						{
							word.SetOriginalText(originalToken);
							word.Set(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation), tokenSpan);
						}
						else
						{
							tokenSpan = null;
							originalToken = null;
						}
						Dictionary<string, string> extraDeps = word.Get(typeof(CoreAnnotations.CoNLLUSecondaryDepsAnnotation));
						if (extraDeps.IsEmpty())
						{
							int govIdx = word.Get(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation));
							Pair<IndexedWord, GrammaticalRelation> govReln = GetGovAndReln(govIdx, 0, word, word.Get(typeof(CoreAnnotations.CoNLLDepTypeAnnotation)), sortedTokens);
							IndexedWord gov = govReln.First();
							GrammaticalRelation reln = govReln.Second();
							TypedDependency dep = new TypedDependency(reln, gov, word);
							word.Set(typeof(CoreAnnotations.LineNumberAnnotation), lineNumberCounter);
							deps.Add(dep);
						}
						else
						{
							foreach (string extraGovIdxStr in extraDeps.Keys)
							{
								if (extraGovIdxStr.Contains("."))
								{
									string[] indexParts = extraGovIdxStr.Split("\\.");
									int extraGovIdx = System.Convert.ToInt32(indexParts[0]);
									int copyCount = System.Convert.ToInt32(indexParts[1]);
									Pair<IndexedWord, GrammaticalRelation> govReln = GetGovAndReln(extraGovIdx, copyCount, word, extraDeps[extraGovIdxStr], sortedTokens);
									IndexedWord gov = govReln.First();
									GrammaticalRelation reln = govReln.Second();
									TypedDependency dep = new TypedDependency(reln, gov, word);
									dep.SetExtra();
									deps.Add(dep);
								}
								else
								{
									int extraGovIdx = System.Convert.ToInt32(extraGovIdxStr);
									int mainGovIdx = word.Get(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation)) != null ? word.Get(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation)) : -1;
									Pair<IndexedWord, GrammaticalRelation> govReln = GetGovAndReln(extraGovIdx, 0, word, extraDeps[extraGovIdxStr], sortedTokens);
									IndexedWord gov = govReln.First();
									GrammaticalRelation reln = govReln.Second();
									TypedDependency dep = new TypedDependency(reln, gov, word);
									if (extraGovIdx != mainGovIdx)
									{
										dep.SetExtra();
									}
									deps.Add(dep);
								}
							}
						}
					}
				}
				lineNumberCounter++;
				SemanticGraph sg = new SemanticGraph(deps);
				comments.ForEach(null);
				return sg;
			}
		}

		private class WordProcessor : IFunction<string, IndexedWord>
		{
			public virtual IndexedWord Apply(string line)
			{
				IndexedWord word = new IndexedWord();
				if (line.StartsWith("#"))
				{
					word.SetWord(line);
					word.SetTag(CommentPos);
					return word;
				}
				string[] bits = line.Split("\\s+");
				word.Set(typeof(CoreAnnotations.TextAnnotation), bits[1]);
				/* Check if it is a multiword token. */
				if (bits[0].Contains("-"))
				{
					string[] span = bits[0].Split("-");
					int start = System.Convert.ToInt32(span[0]);
					int end = System.Convert.ToInt32(span[1]);
					word.Set(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation), new IntPair(start, end));
					word.Set(typeof(CoreAnnotations.IndexAnnotation), start);
				}
				else
				{
					if (bits[0].Contains("."))
					{
						string[] indexParts = bits[0].Split("\\.");
						int index = System.Convert.ToInt32(indexParts[0]);
						int copyCount = System.Convert.ToInt32(indexParts[1]);
						word.Set(typeof(CoreAnnotations.IndexAnnotation), index);
						word.SetIndex(index);
						word.SetCopyCount(copyCount);
						word.SetValue(bits[1]);
						/* Parse features. */
						Dictionary<string, string> features = CoNLLUUtils.ParseFeatures(bits[5]);
						word.Set(typeof(CoreAnnotations.CoNLLUFeats), features);
						/* Parse extra dependencies. */
						Dictionary<string, string> extraDeps = CoNLLUUtils.ParseExtraDeps(bits[8]);
						word.Set(typeof(CoreAnnotations.CoNLLUSecondaryDepsAnnotation), extraDeps);
					}
					else
					{
						word.Set(typeof(CoreAnnotations.IndexAnnotation), System.Convert.ToInt32(bits[0]));
						word.Set(typeof(CoreAnnotations.LemmaAnnotation), bits[2]);
						word.Set(typeof(CoreAnnotations.CoarseTagAnnotation), bits[3]);
						word.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), bits[4]);
						word.Set(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation), System.Convert.ToInt32(bits[6]));
						word.Set(typeof(CoreAnnotations.CoNLLDepTypeAnnotation), bits[7]);
						word.Set(typeof(CoreAnnotations.CoNLLUMisc), bits[9]);
						word.SetIndex(System.Convert.ToInt32(bits[0]));
						word.SetValue(bits[1]);
						/* Parse features. */
						Dictionary<string, string> features = CoNLLUUtils.ParseFeatures(bits[5]);
						word.Set(typeof(CoreAnnotations.CoNLLUFeats), features);
						/* Parse extra dependencies. */
						Dictionary<string, string> extraDeps = CoNLLUUtils.ParseExtraDeps(bits[8]);
						word.Set(typeof(CoreAnnotations.CoNLLUSecondaryDepsAnnotation), extraDeps);
					}
				}
				return word;
			}
		}
		// end static class WordProcessor
	}
}

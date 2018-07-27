using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Neural;
using Edu.Stanford.Nlp.Semgraph;

namespace Edu.Stanford.Nlp.Coref.Neural
{
	/// <summary>Extracts word-embedding features from mentions.</summary>
	/// <author>Kevin Clark</author>
	public class EmbeddingExtractor
	{
		private readonly bool conll;

		private readonly Embedding staticWordEmbeddings;

		private readonly Embedding tunedWordEmbeddings;

		public EmbeddingExtractor(bool conll, Embedding staticWordEmbeddings, Embedding tunedWordEmbeddings)
		{
			this.conll = conll;
			this.staticWordEmbeddings = staticWordEmbeddings;
			this.tunedWordEmbeddings = tunedWordEmbeddings;
		}

		public virtual SimpleMatrix GetDocumentEmbedding(Document document)
		{
			if (!conll)
			{
				return new SimpleMatrix(staticWordEmbeddings.GetEmbeddingSize(), 1);
			}
			IList<CoreLabel> words = new List<CoreLabel>();
			ICollection<int> seenSentences = new HashSet<int>();
			foreach (Mention m in document.predictedMentionsByID.Values)
			{
				if (!seenSentences.Contains(m.sentNum))
				{
					seenSentences.Add(m.sentNum);
					Sharpen.Collections.AddAll(words, m.sentenceWords);
				}
			}
			return GetAverageEmbedding(words);
		}

		public virtual SimpleMatrix GetMentionEmbeddings(Mention m, SimpleMatrix docEmbedding)
		{
			IEnumerator<SemanticGraphEdge> depIterator = m.enhancedDependency.IncomingEdgeIterator(m.headIndexedWord);
			SemanticGraphEdge depRelation = depIterator.MoveNext() ? depIterator.Current : null;
			return NeuralUtils.Concatenate(GetAverageEmbedding(m.sentenceWords, m.startIndex, m.endIndex), GetAverageEmbedding(m.sentenceWords, m.startIndex - 5, m.startIndex), GetAverageEmbedding(m.sentenceWords, m.endIndex, m.endIndex + 5), GetAverageEmbedding
				(m.sentenceWords.SubList(0, m.sentenceWords.Count - 1)), docEmbedding, GetWordEmbedding(m.sentenceWords, m.headIndex), GetWordEmbedding(m.sentenceWords, m.startIndex), GetWordEmbedding(m.sentenceWords, m.endIndex - 1), GetWordEmbedding(m.sentenceWords
				, m.startIndex - 1), GetWordEmbedding(m.sentenceWords, m.endIndex), GetWordEmbedding(m.sentenceWords, m.startIndex - 2), GetWordEmbedding(m.sentenceWords, m.endIndex + 1), GetWordEmbedding(depRelation == null ? null : depRelation.GetSource(
				).Word()));
		}

		private SimpleMatrix GetAverageEmbedding(IList<CoreLabel> words)
		{
			SimpleMatrix emb = new SimpleMatrix(staticWordEmbeddings.GetEmbeddingSize(), 1);
			foreach (CoreLabel word in words)
			{
				emb = emb.Plus(GetStaticWordEmbedding(word.Word()));
			}
			return emb.Divide(Math.Max(1, words.Count));
		}

		private SimpleMatrix GetAverageEmbedding(IList<CoreLabel> sentence, int start, int end)
		{
			return GetAverageEmbedding(sentence.SubList(Math.Max(Math.Min(start, sentence.Count - 1), 0), Math.Max(Math.Min(end, sentence.Count - 1), 0)));
		}

		private SimpleMatrix GetWordEmbedding(IList<CoreLabel> sentence, int i)
		{
			return GetWordEmbedding(i < 0 || i >= sentence.Count ? null : sentence[i].Word());
		}

		public virtual SimpleMatrix GetWordEmbedding(string word)
		{
			word = NormalizeWord(word);
			return tunedWordEmbeddings.ContainsWord(word) ? tunedWordEmbeddings.Get(word) : staticWordEmbeddings.Get(word);
		}

		public virtual SimpleMatrix GetStaticWordEmbedding(string word)
		{
			return staticWordEmbeddings.Get(NormalizeWord(word));
		}

		private static string NormalizeWord(string w)
		{
			if (w == null)
			{
				return "<missing>";
			}
			else
			{
				if (w.Equals("/."))
				{
					return ".";
				}
				else
				{
					if (w.Equals("/?"))
					{
						return "?";
					}
					else
					{
						if (w.Equals("-LRB-"))
						{
							return "(";
						}
						else
						{
							if (w.Equals("-RRB-"))
							{
								return ")";
							}
							else
							{
								if (w.Equals("-LCB-"))
								{
									return "{";
								}
								else
								{
									if (w.Equals("-RCB-"))
									{
										return "}";
									}
									else
									{
										if (w.Equals("-LSB-"))
										{
											return "[";
										}
										else
										{
											if (w.Equals("-RSB-"))
											{
												return "]";
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return w.ReplaceAll("\\d", "0").ToLower();
		}
	}
}

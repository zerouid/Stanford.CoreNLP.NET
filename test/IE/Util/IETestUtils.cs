using System.Collections.Generic;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE.Util
{
	/// <summary>Factor out some commonly used code (e.g., make a tree from a CoNLL spec)</summary>
	/// <author>Gabor Angeli</author>
	public class IETestUtils
	{
		/// <summary>Create a dummy word, just with a given word at a given index.</summary>
		/// <remarks>
		/// Create a dummy word, just with a given word at a given index.
		/// Mostly useful for making semantic graphs.
		/// </remarks>
		public static CoreLabel MkWord(string gloss, int index)
		{
			CoreLabel w = new CoreLabel();
			w.SetWord(gloss);
			w.SetValue(gloss);
			if (index >= 0)
			{
				w.SetIndex(index);
			}
			return w;
		}

		/// <summary>Parse a CoNLL formatted string into a SemanticGraph.</summary>
		/// <remarks>
		/// Parse a CoNLL formatted string into a SemanticGraph.
		/// This is useful for tests so that you don't need to load the model (and are robust to
		/// model changes).
		/// </remarks>
		/// <param name="conll">The CoNLL format for the tree.</param>
		/// <returns>A semantic graph, as well as the flat tokens of the sentence.</returns>
		public static Pair<SemanticGraph, IList<CoreLabel>> ParseCoNLL(string conll)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			SemanticGraph tree = new SemanticGraph();
			foreach (string line in conll.Split("\n"))
			{
				if (line.Trim().Equals(string.Empty))
				{
					continue;
				}
				string[] fields = line.Trim().Split("\\s+");
				int index = System.Convert.ToInt32(fields[0]);
				string word = fields[1];
				CoreLabel label = MkWord(word, index);
				sentence.Add(label);
				if (fields[2].Equals("0"))
				{
					tree.AddRoot(new IndexedWord(label));
				}
				else
				{
					tree.AddVertex(new IndexedWord(label));
				}
				if (fields.Length > 4)
				{
					label.SetTag(fields[4]);
				}
				if (fields.Length > 5)
				{
					label.SetNER(fields[5]);
				}
				if (fields.Length > 6)
				{
					label.SetLemma(fields[6]);
				}
			}
			int i = 0;
			foreach (string line_1 in conll.Split("\n"))
			{
				if (line_1.Trim().Equals(string.Empty))
				{
					continue;
				}
				string[] fields = line_1.Trim().Split("\\s+");
				int parent = System.Convert.ToInt32(fields[2]);
				string reln = fields[3];
				if (parent > 0)
				{
					tree.AddEdge(new IndexedWord(sentence[parent - 1]), new IndexedWord(sentence[i]), new GrammaticalRelation(Language.UniversalEnglish, reln, null, null), 1.0, false);
				}
				i += 1;
			}
			return Pair.MakePair(tree, sentence);
		}

		/// <summary>Create a sentence (list of CoreLabels) from a given text.</summary>
		/// <remarks>
		/// Create a sentence (list of CoreLabels) from a given text.
		/// The resulting labels will have a word, lemma (guessed poorly), and
		/// a part of speech if one is specified on the input.
		/// </remarks>
		/// <param name="text">The text to parse.</param>
		/// <returns>A sentence corresponding to the text.</returns>
		public static IList<CoreLabel> ParseSentence(string text)
		{
			return Arrays.AsList(text.Split("\\s+")).Stream().Map(null).Collect(Collectors.ToList());
		}
	}
}

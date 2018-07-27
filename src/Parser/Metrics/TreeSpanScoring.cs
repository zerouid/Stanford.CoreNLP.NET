using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;




namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Provides a method for deciding how similar two trees are.</summary>
	/// <author>John Bauer</author>
	public class TreeSpanScoring
	{
		private TreeSpanScoring()
		{
		}

		// static only
		/// <summary>
		/// Counts how many spans are present in goldTree, including
		/// preterminals, but not present in guessTree, along with how many
		/// spans are present in guessTree and not goldTree.
		/// </summary>
		/// <remarks>
		/// Counts how many spans are present in goldTree, including
		/// preterminals, but not present in guessTree, along with how many
		/// spans are present in guessTree and not goldTree.  Each one counts
		/// as an error, meaning that something like a mislabeled span or
		/// preterminal counts as two errors.
		/// <br />
		/// Span labels are compared using the basicCategory() function
		/// from the passed in TreebankLanguagePack.
		/// </remarks>
		public static int CountSpanErrors(ITreebankLanguagePack tlp, Tree goldTree, Tree guessTree)
		{
			ICollection<Constituent> goldConstituents = goldTree.Constituents(LabeledConstituent.Factory());
			ICollection<Constituent> guessConstituents = guessTree.Constituents(LabeledConstituent.Factory());
			ICollection<Constituent> simpleGoldConstituents = SimplifyConstituents(tlp, goldConstituents);
			ICollection<Constituent> simpleGuessConstituents = SimplifyConstituents(tlp, guessConstituents);
			//System.out.println(simpleGoldConstituents);
			//System.out.println(simpleGuessConstituents);
			int errors = 0;
			foreach (Constituent gold in simpleGoldConstituents)
			{
				if (!simpleGuessConstituents.Contains(gold))
				{
					++errors;
				}
			}
			foreach (Constituent guess in simpleGuessConstituents)
			{
				if (!simpleGoldConstituents.Contains(guess))
				{
					++errors;
				}
			}
			// The spans returned by constituents() doesn't include the
			// preterminals, so we need to count those ourselves now
			IList<TaggedWord> goldWords = goldTree.TaggedYield();
			IList<TaggedWord> guessWords = guessTree.TaggedYield();
			int len = Math.Min(goldWords.Count, guessWords.Count);
			for (int i = 0; i < len; ++i)
			{
				string goldTag = tlp.BasicCategory(goldWords[i].Tag());
				string guessTag = tlp.BasicCategory(guessWords[i].Tag());
				if (!goldTag.Equals(guessTag))
				{
					// we count one error for each span that is present in the
					// gold and not in the guess, and one error for each span that
					// is present in the guess and not the gold, so this counts as
					// two errors
					errors += 2;
				}
			}
			return errors;
		}

		public static ICollection<Constituent> SimplifyConstituents(ITreebankLanguagePack tlp, ICollection<Constituent> constituents)
		{
			ICollection<Constituent> newConstituents = new HashSet<Constituent>();
			foreach (Constituent con in constituents)
			{
				if (!(con is LabeledConstituent))
				{
					throw new AssertionError("Unexpected constituent type " + con.GetType());
				}
				LabeledConstituent labeled = (LabeledConstituent)con;
				newConstituents.Add(new LabeledConstituent(labeled.Start(), labeled.End(), tlp.BasicCategory(labeled.Value())));
			}
			return newConstituents;
		}
	}
}

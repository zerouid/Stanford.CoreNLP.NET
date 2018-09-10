using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An interface for lexicons interfacing to lexparser.</summary>
	/// <remarks>
	/// An interface for lexicons interfacing to lexparser.  Its primary
	/// responsibility is to provide a conditional probability
	/// P(word|tag), which is fulfilled by the {#score} method.
	/// Inside the lexparser,
	/// Strings are interned and tags and words are usually represented as integers.
	/// </remarks>
	/// <author>Galen Andrew</author>
	public interface ILexicon
	{
		// if UNK were a word, counts would merge
		// boundary word -- assumed not a real word
		// boundary tag -- assumed not a real tag
		//void setUnknownWordModel(UnknownWordModel uwModel);
		//  double getCount(IntTaggedWord w);
		/// <summary>Checks whether a word is in the lexicon.</summary>
		/// <param name="word">The word as an int</param>
		/// <returns>Whether the word is in the lexicon</returns>
		bool IsKnown(int word);

		/// <summary>Checks whether a word is in the lexicon.</summary>
		/// <param name="word">The word as a String</param>
		/// <returns>Whether the word is in the lexicon</returns>
		bool IsKnown(string word);

		/// <summary>Return the Set of tags used by this tagger (available after training the tagger).</summary>
		/// <returns>The Set of tags used by this tagger</returns>
		ICollection<string> TagSet(Func<string, string> basicCategoryFunction);

		/// <summary>Get an iterator over all rules (pairs of (word, POS)) for this word.</summary>
		/// <param name="word">The word, represented as an integer in Index</param>
		/// <param name="loc">
		/// The position of the word in the sentence (counting from 0).
		/// <i>Implementation note: The BaseLexicon class doesn't
		/// actually make use of this position information.</i>
		/// </param>
		/// <param name="featureSpec">Additional word features like morphosyntactic information.</param>
		/// <returns>
		/// An Iterator over a List ofIntTaggedWords, which pair the word
		/// with possible taggings as integer pairs.  (Each can be
		/// thought of as a <code>tag -&gt; word<code> rule.)
		/// </returns>
		IEnumerator<IntTaggedWord> RuleIteratorByWord(int word, int loc, string featureSpec);

		/// <summary>
		/// Same thing, but with a string that needs to be translated by the
		/// lexicon's word index
		/// </summary>
		IEnumerator<IntTaggedWord> RuleIteratorByWord(string word, int loc, string featureSpec);

		/// <summary>Returns the number of rules (tag rewrites as word) in the Lexicon.</summary>
		/// <remarks>
		/// Returns the number of rules (tag rewrites as word) in the Lexicon.
		/// This method assumes that the lexicon has been initialized.
		/// </remarks>
		/// <returns>The number of rules (tag rewrites as word) in the Lexicon.</returns>
		int NumRules();

		/// <summary>Start training this lexicon on the expected number of trees.</summary>
		/// <remarks>
		/// Start training this lexicon on the expected number of trees.
		/// (Some UnknownWordModels use the number of trees to know when to
		/// start counting statistics.)
		/// </remarks>
		void InitializeTraining(double numTrees);

		/// <summary>Trains this lexicon on the Collection of trees.</summary>
		/// <remarks>
		/// Trains this lexicon on the Collection of trees.
		/// Can be called more than once with different collections of trees.
		/// </remarks>
		/// <param name="trees">Trees to train on</param>
		void Train(ICollection<Tree> trees);

		void Train(ICollection<Tree> trees, double weight);

		// WSGDEBUG
		// Binarizer converts everything to CategoryWordTag, so we lose additional
		// lexical annotations. RawTrees should be the same size as trees.
		void Train(ICollection<Tree> trees, ICollection<Tree> rawTrees);

		void Train(Tree tree, double weight);

		/// <summary>Not all subclasses support this particular method.</summary>
		/// <remarks>
		/// Not all subclasses support this particular method.  Those that
		/// don't will barf...
		/// </remarks>
		void Train(IList<TaggedWord> sentence, double weight);

		/// <summary>Not all subclasses support this particular method.</summary>
		/// <remarks>
		/// Not all subclasses support this particular method.  Those that
		/// don't will barf...
		/// </remarks>
		void Train(TaggedWord tw, int loc, double weight);

		/// <summary>
		/// If training on a per-word basis instead of on a per-tree basis,
		/// we will want to increment the tree count as this happens.
		/// </summary>
		void IncrementTreesRead(double weight);

		/// <summary>
		/// Sometimes we might have a sentence of tagged words which we would
		/// like to add to the lexicon, but they weren't part of a binarized,
		/// markovized, or otherwise annotated tree.
		/// </summary>
		void TrainUnannotated(IList<TaggedWord> sentence, double weight);

		/// <summary>Done collecting statistics for the lexicon.</summary>
		void FinishTraining();

		// void trainWithExpansion(Collection<TaggedWord> taggedWords);
		/// <summary>
		/// Get the score of this word with this tag (as an IntTaggedWord) at this
		/// loc.
		/// </summary>
		/// <remarks>
		/// Get the score of this word with this tag (as an IntTaggedWord) at this
		/// loc.
		/// (Presumably an estimate of P(word | tag).)
		/// </remarks>
		/// <param name="iTW">An IntTaggedWord pairing a word and POS tag</param>
		/// <param name="loc">
		/// The position in the sentence.  <i>In the default implementation
		/// this is used only for unknown words to change their
		/// probability distribution when sentence initial.</i>
		/// </param>
		/// <param name="word">
		/// The word itself; useful so we don't have to look it
		/// up in an index
		/// </param>
		/// <param name="featureSpec">TODO</param>
		/// <returns>A score, usually, log P(word|tag)</returns>
		float Score(IntTaggedWord iTW, int loc, string word, string featureSpec);

		/// <summary>Write the lexicon in human-readable format to the Writer.</summary>
		/// <remarks>
		/// Write the lexicon in human-readable format to the Writer.
		/// (An optional operation.)
		/// </remarks>
		/// <param name="w">The writer to output to</param>
		/// <exception cref="System.IO.IOException">If any I/O problem</exception>
		void WriteData(TextWriter w);

		/// <summary>
		/// Read the lexicon from the BufferedReader in the format written by
		/// writeData.
		/// </summary>
		/// <remarks>
		/// Read the lexicon from the BufferedReader in the format written by
		/// writeData.
		/// (An optional operation.)
		/// </remarks>
		/// <param name="in">The BufferedReader to read from</param>
		/// <exception cref="System.IO.IOException">If any I/O problem</exception>
		void ReadData(BufferedReader @in);

		IUnknownWordModel GetUnknownWordModel();

		// todo [cdm Sep 2013]: It seems like we could easily remove this from the interface
		void SetUnknownWordModel(IUnknownWordModel uwm);
	}

	public static class LexiconConstants
	{
		public const string UnknownWord = "UNK";

		public const string Boundary = ".$.";

		public const string BoundaryTag = ".$$.";
	}
}

using Edu.Stanford.Nlp.Stats;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class defines the runtime interface for unknown words
	/// in lexparser.
	/// </summary>
	/// <remarks>
	/// This class defines the runtime interface for unknown words
	/// in lexparser. See UnknownWordModelTrainer for how unknown
	/// word models are built from training data.
	/// </remarks>
	/// <author>Anna Rafferty</author>
	/// <author>Christopher Manning</author>
	public interface IUnknownWordModel
	{
		/// <summary>Get the level of equivalence classing for the model.</summary>
		/// <remarks>
		/// Get the level of equivalence classing for the model.
		/// One unknown word model may allow different options to be set; for example,
		/// several models of unknown words for a given language could be included in one
		/// class.  The unknown level can be queried with this method.
		/// </remarks>
		/// <returns>The current level of unknown word equivalence classing</returns>
		int GetUnknownLevel();

		/// <summary>Returns the lexicon used by this unknown word model.</summary>
		/// <remarks>
		/// Returns the lexicon used by this unknown word model. The
		/// lexicon is used to check information about words being seen/unseen.
		/// </remarks>
		/// <returns>The lexicon used by this unknown word model</returns>
		ILexicon GetLexicon();

		/// <summary>
		/// Get the score of this word with this tag (as an IntTaggedWord) at this
		/// location loc in a sentence.
		/// </summary>
		/// <remarks>
		/// Get the score of this word with this tag (as an IntTaggedWord) at this
		/// location loc in a sentence.
		/// (Presumably an estimate of P(word | tag), usually calculated as
		/// P(signature | tag).)
		/// Assumes the word is unknown.
		/// </remarks>
		/// <param name="iTW">An IntTaggedWord pairing a word and POS tag</param>
		/// <param name="loc">
		/// The position in the sentence.  <i>In the default implementation
		/// this is used only for unknown words to change their
		/// probability distribution when sentence initial.  Now,
		/// a negative value </i>
		/// </param>
		/// <param name="c_Tseen">Total count of this tag (on seen words) in training</param>
		/// <param name="total">Total count of word tokens in training</param>
		/// <param name="smooth">Weighting on prior P(T|U) in estimate</param>
		/// <param name="word">The word itself; useful so we don't look it up in the index</param>
		/// <returns>A double valued score, usually - log P(word|tag)</returns>
		float Score(IntTaggedWord iTW, int loc, double c_Tseen, double total, double smooth, string word);

		/// <summary>Calculate P(Tag|Signature) with Bayesian smoothing via just P(Tag|Unknown).</summary>
		double ScoreProbTagGivenWordSignature(IntTaggedWord iTW, int loc, double smooth, string word);

		/// <summary>
		/// This routine returns a String that is the "signature" of the class of a
		/// word.
		/// </summary>
		/// <remarks>
		/// This routine returns a String that is the "signature" of the class of a
		/// word. For, example, it might represent whether it is a number of ends in
		/// -s. The strings returned by convention match the pattern UNK or UNK-.* ,
		/// which is just assumed to not match any real word. Behavior depends on the
		/// unknownLevel (-uwm flag) passed in to the class.
		/// </remarks>
		/// <param name="word">The word to make a signature for</param>
		/// <param name="loc">
		/// Its position in the sentence (mainly so sentence-initial
		/// capitalized words can be treated differently)
		/// </param>
		/// <returns>A String that is its signature (equivalence class)</returns>
		string GetSignature(string word, int loc);

		/// <summary>Returns an unknown word signature as an integer index rather than as a String.</summary>
		int GetSignatureIndex(int wordIndex, int sentencePosition, string word);

		/// <summary>Adds the tagging with count to the data structures in this Lexicon.</summary>
		/// <param name="seen">Whether tagging is seen</param>
		/// <param name="itw">The tagging</param>
		/// <param name="count">Its weight</param>
		void AddTagging(bool seen, IntTaggedWord itw, double count);

		/// <summary>Returns a Counter from IntTaggedWord to how often they have been seen.</summary>
		ICounter<IntTaggedWord> UnSeenCounter();
	}
}

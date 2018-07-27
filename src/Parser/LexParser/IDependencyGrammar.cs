using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An interface for DependencyGrammars.</summary>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning</author>
	public interface IDependencyGrammar
	{
		/// <returns>
		/// The number of tags recognized in the reduced (projected) tag
		/// space used in the DependencyGrammar.
		/// </returns>
		int NumTagBins();

		/// <summary>
		/// Converts a tag (coded as an integer via a Numberer) from its
		/// representation in the full tag space to the reduced (projected) tag
		/// space used in the DependencyGrammar.
		/// </summary>
		/// <param name="tag">An int encoding a tag (in the "tags" Numberer)</param>
		/// <returns>An int representing the tag in the reduced binTag space</returns>
		int TagBin(int tag);

		/// <returns>
		/// The number of distance buckets (measured between the head and
		/// the nearest corner of the argument) used in calculating attachment
		/// probabilities by the DependencyGrammar
		/// </returns>
		int NumDistBins();

		/// <param name="distance">A distance in intervening words between head and arg</param>
		/// <returns>
		/// The distance bucket corresponding the the original distance
		/// (measured between the head and
		/// the nearest corner of the argument) used in calculating attachment
		/// probabilities by the DependencyGrammar.  Bucket numbers are small
		/// integers [0, ..., numDistBins - 1].
		/// </returns>
		short DistanceBin(int distance);

		/// <summary>Tune free parameters on these trees.</summary>
		/// <remarks>
		/// Tune free parameters on these trees.
		/// A substantive implementation is optional.
		/// </remarks>
		/// <param name="trees">A Collection of Trees for use as a tuning data set</param>
		void Tune(ICollection<Tree> trees);

		/// <summary>Score a IntDependency according to the grammar.</summary>
		/// <param name="dependency">The dependency object to be scored, in normal form.</param>
		/// <returns>
		/// The negative log probability given to the dependency by the
		/// grammar.  This may be Double.NEGATIVE_INFINITY for "impossible".
		/// </returns>
		double Score(IntDependency dependency);

		/// <summary>
		/// Score an IntDependency in the reduced tagBin space according to the
		/// grammar.
		/// </summary>
		/// <param name="dependency">
		/// The dependency object to be scored, where the tags in
		/// the dependency have already been mapped to a reduced space by a
		/// tagProjection function.
		/// </param>
		/// <returns>
		/// The negative log probability given to the dependency by the
		/// grammar.  This may be Double.NEGATIVE_INFINITY for "impossible".
		/// </returns>
		double ScoreTB(IntDependency dependency);

		/// <summary>
		/// Score a dependency according to the grammar, where the elements of the
		/// dependency are represented in separate paramters.
		/// </summary>
		/// <returns>
		/// The negative log probability given to the dependency by the
		/// grammar.  This may be Double.NEGATIVE_INFINITY for "impossible".
		/// </returns>
		double Score(int headWord, int headTag, int argWord, int argTag, bool leftHeaded, int dist);

		/// <summary>
		/// Score a dependency according to the grammar, where the elements of the
		/// dependency are represented in separate paramters.
		/// </summary>
		/// <remarks>
		/// Score a dependency according to the grammar, where the elements of the
		/// dependency are represented in separate paramters.  The tags in
		/// the dependency have already been mapped to a reduced space by a
		/// tagProjection function.
		/// </remarks>
		/// <returns>
		/// The negative log probability given to the dependency by the
		/// grammar.  This may be Double.NEGATIVE_INFINITY for "impossible".
		/// </returns>
		double ScoreTB(int headWord, int headTag, int argWord, int argTag, bool leftHeaded, int dist);

		/// <summary>Read from text grammar.</summary>
		/// <remarks>Read from text grammar.  Optional.</remarks>
		/// <exception cref="System.IO.IOException"/>
		void ReadData(BufferedReader @in);

		/// <summary>Write to text grammar.</summary>
		/// <remarks>Write to text grammar.  Optional.</remarks>
		/// <exception cref="System.IO.IOException"/>
		void WriteData(PrintWriter w);

		/// <summary>Set the Lexicon, which the DependencyGrammar may use in scoring P(w|t).</summary>
		void SetLexicon(ILexicon lexicon);
	}
}

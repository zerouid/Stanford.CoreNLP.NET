using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Contains language-specific methods commonly necessary to get a parser
	/// to parse an arbitrary treebank.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <version>03/05/2003</version>
	public interface ITreebankLangParserParams : ITreebankFactory
	{
		IHeadFinder HeadFinder();

		IHeadFinder TypedDependencyHeadFinder();

		/// <summary>Allows language specific processing (e.g., stemming) of head words.</summary>
		/// <param name="headWord">
		/// An
		/// <see cref="Edu.Stanford.Nlp.Ling.ILabel"/>
		/// that minimally implements the
		/// <see cref="Edu.Stanford.Nlp.Ling.IHasWord"/>
		/// and
		/// <see cref="Edu.Stanford.Nlp.Ling.IHasTag"/>
		/// interfaces.
		/// </param>
		/// <returns>
		/// A processed
		/// <see cref="Edu.Stanford.Nlp.Ling.ILabel"/>
		/// </returns>
		ILabel ProcessHeadWord(ILabel headWord);

		void SetInputEncoding(string encoding);

		void SetOutputEncoding(string encoding);

		/// <summary>If evalGFs = true, then the evaluation of parse trees will include evaluation on grammatical functions.</summary>
		/// <remarks>
		/// If evalGFs = true, then the evaluation of parse trees will include evaluation on grammatical functions.
		/// Otherwise, evaluation will strip the grammatical functions.
		/// </remarks>
		void SetEvaluateGrammaticalFunctions(bool evalGFs);

		/// <summary>Returns the output encoding being used.</summary>
		/// <returns>The output encoding being used.</returns>
		string GetOutputEncoding();

		/// <summary>Returns the input encoding being used.</summary>
		/// <returns>The input encoding being used.</returns>
		string GetInputEncoding();

		/// <summary>Returns a factory for reading in trees from the source you want.</summary>
		/// <remarks>
		/// Returns a factory for reading in trees from the source you want.  It's
		/// the responsibility of trf to deal properly with character-set encoding
		/// of the input.  It also is the responsibility of trf to properly
		/// normalize trees.
		/// </remarks>
		/// <returns>A factory that vends an appropriate TreeReader</returns>
		ITreeReaderFactory TreeReaderFactory();

		/// <summary>
		/// Vends a
		/// <see cref="ILexicon"/>
		/// object suitable to the particular language/treebank combination of interest.
		/// </summary>
		/// <param name="op">Options as to how the Lexicon behaves</param>
		/// <returns>A Lexicon, constructed based on the given option</returns>
		ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex);

		/// <summary>The tree transformer applied to trees prior to evaluation.</summary>
		/// <remarks>
		/// The tree transformer applied to trees prior to evaluation.
		/// For instance, it might delete punctuation nodes.  This method will
		/// be applied both to the parse output tree and to the gold
		/// tree.  The exact specification depends on "standard practice" for
		/// various treebanks.
		/// </remarks>
		/// <returns>
		/// A TreeTransformer that performs adjustments to trees to delete
		/// or equivalence class things not evaluated in the parser performance
		/// evaluation.
		/// </returns>
		ITreeTransformer Collinizer();

		/// <summary>the tree transformer used to produce trees for evaluation.</summary>
		/// <remarks>
		/// the tree transformer used to produce trees for evaluation.  Will
		/// be applied both to the parse output tree and to the gold
		/// tree. Should strip punctuation and maybe do some other
		/// things. The evalb version should strip some more stuff
		/// off. (finish this doc!)
		/// </remarks>
		ITreeTransformer CollinizerEvalb();

		/// <summary>returns a MemoryTreebank appropriate to the treebank source</summary>
		Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank();

		/// <summary>returns a DiskTreebank appropriate to the treebank source</summary>
		Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank();

		/// <summary>returns a MemoryTreebank appropriate to the testing treebank source</summary>
		Edu.Stanford.Nlp.Trees.MemoryTreebank TestMemoryTreebank();

		/// <summary>Required to extend TreebankFactory</summary>
		Edu.Stanford.Nlp.Trees.Treebank Treebank();

		/// <summary>
		/// returns a TreebankLanguagePack containing Treebank-specific (but
		/// not parser-specific) info such as what is punctuation, and also
		/// information about the structure of labels
		/// </summary>
		ITreebankLanguagePack TreebankLanguagePack();

		/// <summary>returns a PrintWriter used to print output.</summary>
		/// <remarks>
		/// returns a PrintWriter used to print output. It's the
		/// responsibility of the returned PrintWriter to deal properly with
		/// character encodings for the relevant treebank
		/// </remarks>
		PrintWriter Pw();

		/// <summary>
		/// returns a PrintWriter used to print output to the OutputStream
		/// o.
		/// </summary>
		/// <remarks>
		/// returns a PrintWriter used to print output to the OutputStream
		/// o. It's the responsibility of the returned PrintWriter to deal
		/// properly with character encodings for the relevant treebank
		/// </remarks>
		PrintWriter Pw(OutputStream o);

		/// <summary>Returns the splitting strings used for selective splits.</summary>
		/// <returns>
		/// An array containing ancestor-annotated Strings: categories
		/// should be split according to these ancestor annotations.
		/// </returns>
		string[] SisterSplitters();

		/// <summary>
		/// Returns a TreeTransformer appropriate to the Treebank which
		/// can be used to remove functional tags (such as "-TMP") from
		/// categories.
		/// </summary>
		ITreeTransformer SubcategoryStripper();

		/// <summary>
		/// This method does language-specific tree transformations such
		/// as annotating particular nodes with language-relevant features.
		/// </summary>
		/// <remarks>
		/// This method does language-specific tree transformations such
		/// as annotating particular nodes with language-relevant features.
		/// Such parameterizations should be inside the specific
		/// TreebankLangParserParams class.  This method is recursively
		/// applied to each node in the tree (depth first, left-to-right),
		/// so you shouldn't write this method to apply recursively to tree
		/// members.  This method is allowed to (and in some cases does)
		/// destructively change the input tree
		/// <paramref name="t"/>
		/// . It changes both
		/// labels and the tree shape.
		/// </remarks>
		/// <param name="t">
		/// The input tree (with non-language specific annotation already
		/// done, so you need to strip back to basic categories)
		/// </param>
		/// <param name="root">The root of the current tree (can be null for words)</param>
		/// <returns>
		/// The fully annotated tree node (with daughters still as you
		/// want them in the final result)
		/// </returns>
		Tree TransformTree(Tree t, Tree root);

		/// <summary>display language-specific settings</summary>
		void Display();

		/// <summary>Set a language-specific option according to command-line flags.</summary>
		/// <remarks>
		/// Set a language-specific option according to command-line flags.
		/// This routine should try to process the option starting at args[i] (which
		/// might potentially be several arguments long if it takes arguments).
		/// It should return the index after the last index it consumed in
		/// processing.  In particular, if it cannot process the current option,
		/// the return value should be i.
		/// </remarks>
		/// <param name="args">Array of command line arguments</param>
		/// <param name="i">Index in command line arguments to try to process as an option</param>
		/// <returns>
		/// The index of the item after arguments processed as part of this
		/// command line option.
		/// </returns>
		int SetOptionFlag(string[] args, int i);

		/// <summary>Return a default sentence of the language (for testing).</summary>
		/// <returns>A default sentence of the language</returns>
		IList<IHasWord> DefaultTestSentence();

		ITokenizerFactory<Tree> TreeTokenizerFactory();

		IExtractor<IDependencyGrammar> DependencyGrammarExtractor(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex);

		/// <summary>Give the parameters for smoothing in the MLEDependencyGrammar.</summary>
		/// <returns>an array of doubles with smooth_aT_hTWd, smooth_aTW_hTWd, smooth_stop, and interp</returns>
		double[] MLEDependencyGrammarSmoothingParams();

		/// <summary>Returns a language specific object for evaluating PP attachment</summary>
		/// <returns>
		/// An object that implements
		/// <see cref="Edu.Stanford.Nlp.Parser.Metrics.AbstractEval"/>
		/// </returns>
		AbstractEval PpAttachmentEval();

		/// <summary>
		/// Returns a function which reads the given filename and turns its
		/// content in a list of GrammaticalStructures.
		/// </summary>
		/// <remarks>
		/// Returns a function which reads the given filename and turns its
		/// content in a list of GrammaticalStructures.  Will throw
		/// UnsupportedOperationException if the language doesn't support
		/// dependencies or GrammaticalStructures.
		/// </remarks>
		IList<GrammaticalStructure> ReadGrammaticalStructureFromFile(string filename);

		/// <summary>Build a GrammaticalStructure from a Tree.</summary>
		/// <remarks>
		/// Build a GrammaticalStructure from a Tree.  Throws
		/// UnsupportedOperationException if the language doesn't support
		/// dependencies or GrammaticalStructures.
		/// </remarks>
		GrammaticalStructure GetGrammaticalStructure(Tree t, IPredicate<string> filter, IHeadFinder hf);

		/// <summary>
		/// Whether our code provides support for converting phrase structure
		/// (constituency) parses to (basic) dependency parses.
		/// </summary>
		/// <returns>Whether dependencies are supported for a language</returns>
		bool SupportsBasicDependencies();

		/// <summary>
		/// Set whether to generate original Stanford Dependencies or the newer
		/// Universal Dependencies.
		/// </summary>
		/// <param name="originalDependencies">Whether to generate SD</param>
		void SetGenerateOriginalDependencies(bool originalDependencies);

		/// <summary>
		/// Whether to generate original Stanford Dependencies or the newer
		/// Universal Dependencies.
		/// </summary>
		/// <returns>Whether to generate SD</returns>
		bool GenerateOriginalDependencies();

		/// <summary>When run inside StanfordCoreNLP, which flags should be used by default.</summary>
		/// <remarks>
		/// When run inside StanfordCoreNLP, which flags should be used by default.
		/// E.g., the current use is that for English, we want it to run with the
		/// option to retain "-TMP" functional tags but not to impose that on
		/// other languages.
		/// </remarks>
		string[] DefaultCoreNLPFlags();
	}
}

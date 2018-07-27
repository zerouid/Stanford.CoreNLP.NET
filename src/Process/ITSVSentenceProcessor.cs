using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// An interface for running an action (a callback function) on each line of a TSV file representing
	/// a collection of sentences in a corpus.
	/// </summary>
	/// <remarks>
	/// An interface for running an action (a callback function) on each line of a TSV file representing
	/// a collection of sentences in a corpus.
	/// This is a useful callback for processing a large batch of sentences; e.g., out of a Greenplum database.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public interface ITSVSentenceProcessor
	{
		/// <summary>A list of possible fields in the sentence table.</summary>
		public enum SentenceField
		{
			Id,
			DependenciesStanford,
			DependenciesExtras,
			DependenciesMalt,
			DependenciesMaltAlt1,
			DependenciesMaltAlt2,
			Words,
			Lemmas,
			PosTags,
			NerTags,
			DocId,
			SentenceIndex,
			CorpusId,
			DocCharBegin,
			DocCharEnd,
			Gloss
		}

		/// <summary>Process a given sentence.</summary>
		/// <param name="id">The sentence id (database id) of the sentence being processed.</param>
		/// <param name="doc">
		/// The single-sentence document to annotate. This contains:
		/// <ul>
		/// <li>Tokens</li>
		/// <li>A parse tree (Collapsed dependencies)</li>
		/// <li>POS Tags</li>
		/// <li>NER tags</li>
		/// <li>Lemmas</li>
		/// <li>DocID</li>
		/// <li>Sentence index</li>
		/// </ul>
		/// </param>
		void Process(long id, Annotation doc);

		/// <summary>Runs the given implementation of TSVSentenceProcessor, and then exits with the appropriate error code.</summary>
		/// <remarks>
		/// Runs the given implementation of TSVSentenceProcessor, and then exits with the appropriate error code.
		/// The error code is the number of exceptions encountered during processing.
		/// </remarks>
		/// <param name="in">The input stream to read examples off of.</param>
		/// <param name="debugStream">The stream to write debugging information to (e.g., stderr).</param>
		/// <param name="cleanup">
		/// A function to run after annotation is over, to clean up open files, etc.
		/// Takes as input the candidate error code, and returns a new error code to exit on.
		/// </param>
		/// <param name="sentenceTableSpec">
		/// The header of the sentence table fields being fed as input to this function.
		/// By default, this can be
		/// <see cref="DefaultSentenceTable"/>
		/// .
		/// </param>
		void RunAndExit(InputStream @in, TextWriter debugStream, IIntUnaryOperator cleanup, IList<TSVSentenceProcessor.SentenceField> sentenceTableSpec);

		// Parse line
		// Create Annotation
		// Process document
		// Debug
		// DONE
		/// <seealso cref="RunAndExit(Java.IO.InputStream, System.IO.TextWriter, Java.Util.Function.IIntUnaryOperator, System.Collections.Generic.IList{E})"/>
		void RunAndExit(InputStream @in, TextWriter debugStream, IIntUnaryOperator cleanup);
	}

	public static class TSVSentenceProcessorConstants
	{
		/// <summary>The list of fields actually in the sentence table being passed as a query to TSVSentenceProcessor.</summary>
		public const IList<TSVSentenceProcessor.SentenceField> DefaultSentenceTable = Java.Util.Collections.UnmodifiableList(Arrays.AsList(TSVSentenceProcessor.SentenceField.Id, TSVSentenceProcessor.SentenceField.DependenciesStanford, TSVSentenceProcessor.SentenceField
			.DependenciesExtras, TSVSentenceProcessor.SentenceField.DependenciesMalt, TSVSentenceProcessor.SentenceField.DependenciesMaltAlt1, TSVSentenceProcessor.SentenceField.DependenciesMaltAlt2, TSVSentenceProcessor.SentenceField.Words, TSVSentenceProcessor.SentenceField
			.Lemmas, TSVSentenceProcessor.SentenceField.PosTags, TSVSentenceProcessor.SentenceField.NerTags, TSVSentenceProcessor.SentenceField.DocId, TSVSentenceProcessor.SentenceField.SentenceIndex, TSVSentenceProcessor.SentenceField.CorpusId, TSVSentenceProcessor.SentenceField
			.DocCharBegin, TSVSentenceProcessor.SentenceField.DocCharEnd, TSVSentenceProcessor.SentenceField.Gloss));
	}
}

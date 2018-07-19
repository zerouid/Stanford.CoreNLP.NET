using Org.Apache.Lucene.Document;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>An easy way to access types of fields instead of setting variables up every time.</summary>
	/// <remarks>
	/// An easy way to access types of fields instead of setting variables up every time.
	/// Copied from KBPFieldType written by Angel.
	/// Created by sonalg on 10/14/14.
	/// </remarks>
	public class LuceneFieldType
	{
		public static readonly FieldType Analyzed = new FieldType();

		public static readonly FieldType AnalyzedNoPosition = new FieldType();

		public static readonly FieldType AnalyzedNotStored = new FieldType();

		public static readonly FieldType NotAnalyzed = new FieldType();

		public static readonly FieldType NotIndexed = new FieldType();

		static LuceneFieldType()
		{
			/* Indexed, tokenized, stored. */
			/* Indexed, tokenized, not stored. */
			/* Indexed, not tokenized, stored. */
			/* not Indexed, not tokenized, stored. */
			AnalyzedNotStored.SetIndexed(true);
			AnalyzedNotStored.SetTokenized(true);
			AnalyzedNotStored.SetStored(false);
			AnalyzedNotStored.SetStoreTermVectors(true);
			AnalyzedNotStored.SetStoreTermVectorPositions(true);
			AnalyzedNotStored.Freeze();
			Analyzed.SetIndexed(true);
			Analyzed.SetTokenized(true);
			Analyzed.SetStored(true);
			Analyzed.SetStoreTermVectors(true);
			Analyzed.SetStoreTermVectorPositions(true);
			Analyzed.Freeze();
			AnalyzedNoPosition.SetIndexed(true);
			AnalyzedNoPosition.SetTokenized(true);
			AnalyzedNoPosition.SetStoreTermVectors(true);
			AnalyzedNoPosition.SetStoreTermVectorPositions(false);
			AnalyzedNoPosition.Freeze();
			NotAnalyzed.SetIndexed(true);
			NotAnalyzed.SetTokenized(false);
			NotAnalyzed.SetStored(true);
			NotAnalyzed.SetStoreTermVectors(false);
			NotAnalyzed.SetStoreTermVectorPositions(false);
			NotAnalyzed.Freeze();
			NotIndexed.SetIndexed(false);
			NotIndexed.SetTokenized(false);
			NotIndexed.SetStored(true);
			NotIndexed.SetStoreTermVectors(false);
			NotIndexed.SetStoreTermVectorPositions(false);
			NotIndexed.Freeze();
		}
	}
}

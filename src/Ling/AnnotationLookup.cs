using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Provides a mapping between CoreAnnotation keys, which are classes, and a text String that names them,
	/// which is needed for things like text serializations and the Semgrex query language.
	/// </summary>
	/// <author>Anna Rafferty</author>
	public class AnnotationLookup
	{
		private AnnotationLookup()
		{
		}

		[System.Serializable]
		private sealed class KeyLookup
		{
			public static readonly AnnotationLookup.KeyLookup ValueKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ValueAnnotation), "value");

			public static readonly AnnotationLookup.KeyLookup TagKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.PartOfSpeechAnnotation), "tag");

			public static readonly AnnotationLookup.KeyLookup WordKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.TextAnnotation), "word");

			public static readonly AnnotationLookup.KeyLookup LemmaKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.LemmaAnnotation), "lemma");

			public static readonly AnnotationLookup.KeyLookup CategoryKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CategoryAnnotation), "cat");

			public static readonly AnnotationLookup.KeyLookup IndexKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.IndexAnnotation), "idx");

			public static readonly AnnotationLookup.KeyLookup ArgKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ArgumentAnnotation), "arg");

			public static readonly AnnotationLookup.KeyLookup MarkingKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.MarkingAnnotation), "mark");

			public static readonly AnnotationLookup.KeyLookup SemanticHeadWordKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SemanticHeadWordAnnotation), "shw");

			public static readonly AnnotationLookup.KeyLookup SemanticHeadPosKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SemanticHeadTagAnnotation), "shp");

			public static readonly AnnotationLookup.KeyLookup VerbSenseKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.VerbSenseAnnotation), "vs");

			public static readonly AnnotationLookup.KeyLookup CategoryFunctionalTagKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CategoryFunctionalTagAnnotation), "cft");

			public static readonly AnnotationLookup.KeyLookup NerKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.NamedEntityTagAnnotation), "ner");

			public static readonly AnnotationLookup.KeyLookup ShapeKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ShapeAnnotation), "shape");

			public static readonly AnnotationLookup.KeyLookup LeftTermKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.LeftTermAnnotation), "LEFT_TERM");

			public static readonly AnnotationLookup.KeyLookup ParentKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ParentAnnotation), "PARENT");

			public static readonly AnnotationLookup.KeyLookup SpanKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SpanAnnotation), "SPAN");

			public static readonly AnnotationLookup.KeyLookup BeforeKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.BeforeAnnotation), "before");

			public static readonly AnnotationLookup.KeyLookup AfterKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.AfterAnnotation), "after");

			public static readonly AnnotationLookup.KeyLookup CurrentKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.OriginalTextAnnotation), "current");

			public static readonly AnnotationLookup.KeyLookup AnswerKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.AnswerAnnotation), "answer");

			public static readonly AnnotationLookup.KeyLookup GOLDANSWER_Key = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.GoldAnswerAnnotation), "goldAnswer");

			public static readonly AnnotationLookup.KeyLookup FeaturesKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.FeaturesAnnotation), "features");

			public static readonly AnnotationLookup.KeyLookup InterpretationKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.InterpretationAnnotation), "interpretation");

			public static readonly AnnotationLookup.KeyLookup RoleKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.RoleAnnotation), "srl");

			public static readonly AnnotationLookup.KeyLookup GazetteerKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.GazetteerAnnotation), "gazetteer");

			public static readonly AnnotationLookup.KeyLookup StemKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.StemAnnotation), "stem");

			public static readonly AnnotationLookup.KeyLookup PolarityKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.PolarityAnnotation), "polarity");

			public static readonly AnnotationLookup.KeyLookup ChCharKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ChineseCharAnnotation), "char");

			public static readonly AnnotationLookup.KeyLookup ChOrigSegKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ChineseOrigSegAnnotation), "orig_seg");

			public static readonly AnnotationLookup.KeyLookup ChSegKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ChineseSegAnnotation), "seg");

			public static readonly AnnotationLookup.KeyLookup BeginPositionKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), "BEGIN_POS");

			public static readonly AnnotationLookup.KeyLookup EndPositionKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), "END_POS");

			public static readonly AnnotationLookup.KeyLookup DocidKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.DocIDAnnotation), "docID");

			public static readonly AnnotationLookup.KeyLookup SentindexKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SentenceIndexAnnotation), "sentIndex");

			public static readonly AnnotationLookup.KeyLookup IdfKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.IDFAnnotation), "idf");

			public static readonly AnnotationLookup.KeyLookup EndPositionKey2 = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), "endPosition");

			public static readonly AnnotationLookup.KeyLookup ChunkKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ChunkAnnotation), "chunk");

			public static readonly AnnotationLookup.KeyLookup NormalizedNerKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), "normalized");

			public static readonly AnnotationLookup.KeyLookup MorphoNumKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.MorphoNumAnnotation), "num");

			public static readonly AnnotationLookup.KeyLookup MorphoPersKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.MorphoPersAnnotation), "pers");

			public static readonly AnnotationLookup.KeyLookup MorphoGenKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.MorphoGenAnnotation), "gen");

			public static readonly AnnotationLookup.KeyLookup MorphoCaseKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.MorphoCaseAnnotation), "case");

			public static readonly AnnotationLookup.KeyLookup WordnetSynKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.WordnetSynAnnotation), "wordnetsyn");

			public static readonly AnnotationLookup.KeyLookup ProtoSynKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.ProtoAnnotation), "proto");

			public static readonly AnnotationLookup.KeyLookup DoctitleKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.DocTitleAnnotation), "doctitle");

			public static readonly AnnotationLookup.KeyLookup DoctypeKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.DocTypeAnnotation), "doctype");

			public static readonly AnnotationLookup.KeyLookup DocdateKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.DocDateAnnotation), "docdate");

			public static readonly AnnotationLookup.KeyLookup DocsourcetypeKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.DocSourceTypeAnnotation), "docsourcetype");

			public static readonly AnnotationLookup.KeyLookup LinkKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.LinkAnnotation), "link");

			public static readonly AnnotationLookup.KeyLookup SpeakerKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SpeakerAnnotation), "speaker");

			public static readonly AnnotationLookup.KeyLookup AuthorKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.AuthorAnnotation), "author");

			public static readonly AnnotationLookup.KeyLookup SectionKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SectionAnnotation), "section");

			public static readonly AnnotationLookup.KeyLookup SectionidKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SectionIDAnnotation), "sectionID");

			public static readonly AnnotationLookup.KeyLookup SectiondateKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.SectionDateAnnotation), "sectionDate");

			public static readonly AnnotationLookup.KeyLookup StackedNerKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.StackedNamedEntityTagAnnotation), "stackedNer");

			public static readonly AnnotationLookup.KeyLookup HeadwordKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.HeadWordStringAnnotation), "headword");

			public static readonly AnnotationLookup.KeyLookup GovernorKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.GovernorAnnotation), "governor");

			public static readonly AnnotationLookup.KeyLookup GazKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.GazAnnotation), "gaz");

			public static readonly AnnotationLookup.KeyLookup AbbrKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.AbbrAnnotation), "abbr");

			public static readonly AnnotationLookup.KeyLookup AbstrKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.AbstrAnnotation), "abstr");

			public static readonly AnnotationLookup.KeyLookup FreqKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.FreqAnnotation), "freq");

			public static readonly AnnotationLookup.KeyLookup WebKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.WebAnnotation), "web");

			public static readonly AnnotationLookup.KeyLookup PosTagKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.PartOfSpeechAnnotation), "pos");

			public static readonly AnnotationLookup.KeyLookup DeprelKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CoNLLDepTypeAnnotation), "deprel");

			public static readonly AnnotationLookup.KeyLookup HeadidxKey = new AnnotationLookup.KeyLookup(typeof(CoreAnnotations.CoNLLDepParentIndexAnnotation), "headidx");

			private readonly Type coreKey;

			private readonly string oldKey;

			internal KeyLookup(Type coreKey, string oldKey)
			{
				//PROJ_CAT_KEY(CoreAnnotations.ProjectedCategoryAnnotation.class, "pcat"),
				//HEAD_WORD_KEY("edu.stanford.nlp.ling.TreeCoreAnnotations.HeadWordAnnotation", "hw"),
				//HEAD_TAG_KEY("edu.stanford.nlp.ling.TreeCoreAnnotations.HeadTagAnnotation", "ht"),
				// effectively unused in 2016 (was in PropBank SRL)
				// Thang Sep13: for Genia NER
				// Also have "pos" for PartOfTag (POS is also the TAG_KEY - "tag", but "pos" makes more sense)
				// Still keep "tag" for POS tag so we don't break anything
				this.coreKey = coreKey;
				this.oldKey = oldKey;
			}

			/// <summary>This constructor allows us to use reflection for loading old class keys.</summary>
			/// <remarks>
			/// This constructor allows us to use reflection for loading old class keys.
			/// This is useful because we can then create distributions that do not have
			/// all of the classes required for all the old keys (such as trees package classes).
			/// </remarks>
			internal KeyLookup(string className, string oldKey)
			{
				Type keyClass;
				try
				{
					keyClass = Sharpen.Runtime.GetType(className);
				}
				catch (TypeLoadException)
				{
					CoreLabel.IGenericAnnotation<object> newKey = null;
					keyClass = newKey.GetType();
				}
				this.coreKey = ErasureUtils.UncheckedCast(keyClass);
				this.oldKey = oldKey;
			}
		}

		// end enum KeyLookup
		/// <summary>
		/// Returns a CoreAnnotation class key for the given string
		/// key if one exists; null otherwise.
		/// </summary>
		/// <param name="stringKey">String form of the key</param>
		/// <returns>
		/// A CoreLabel/CoreAnnotation key, or
		/// <see langword="null"/>
		/// if nothing matches
		/// </returns>
		public static Type ToCoreKey(string stringKey)
		{
			foreach (AnnotationLookup.KeyLookup lookup in AnnotationLookup.KeyLookup.Values())
			{
				if (lookup.oldKey.Equals(stringKey))
				{
					return lookup.coreKey;
				}
			}
			return null;
		}

		private static readonly IDictionary<Type, Type> valueCache = Generics.NewHashMap();

		/// <summary>Returns the runtime value type associated with the given key.</summary>
		/// <remarks>
		/// Returns the runtime value type associated with the given key.  Caches
		/// results in a private Map.
		/// </remarks>
		/// <param name="key">The annotation key (non-null)</param>
		/// <returns>The type of the value of that key (non-null)</returns>
		public static Type GetValueType(Type key)
		{
			Type type = valueCache[key];
			if (type == null)
			{
				try
				{
					type = System.Activator.CreateInstance(key).GetType();
				}
				catch (Exception e)
				{
					throw new Exception("Unexpected failure to instantiate - is your key class fancy?", e);
				}
				valueCache[(Type)key] = type;
			}
			return type;
		}
	}
}

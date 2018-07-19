using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// <p>
	/// Set of common annotations for
	/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
	/// s. The classes
	/// defined here are typesafe keys for getting and setting annotation
	/// values. These classes need not be instantiated outside of this
	/// class. e.g
	/// <see cref="TextAnnotation"/>
	/// .class serves as the key and a
	/// <c>String</c>
	/// serves as the value containing the
	/// corresponding word.
	/// </p>
	/// <p>
	/// New types of
	/// <see cref="ICoreAnnotation{V}"/>
	/// can be defined anywhere that is
	/// convenient in the source tree - they are just classes. This file exists to
	/// hold widely used "core" annotations and others inherited from the
	/// <see cref="ILabel"/>
	/// family. In general, most keys should be placed in this file as
	/// they may often be reused throughout the code. This architecture allows for
	/// flexibility, but in many ways it should be considered as equivalent to an
	/// enum in which everything should be defined
	/// </p>
	/// <p>
	/// The getType method required by CoreAnnotation must return the same class type
	/// as its value type parameter. It feels like one should be able to get away
	/// without that method, but because Java erases the generic type signature, that
	/// info disappears at runtime. See
	/// <see cref="ValueAnnotation"/>
	/// for an example.
	/// </p>
	/// </summary>
	/// <author>dramage</author>
	/// <author>rafferty</author>
	/// <author>bethard</author>
	public class CoreAnnotations
	{
		private CoreAnnotations()
		{
		}

		/// <summary>The CoreMap key identifying the annotation's text.</summary>
		/// <remarks>
		/// The CoreMap key identifying the annotation's text.
		/// Note that this key is intended to be used with many different kinds of
		/// annotations - documents, sentences and tokens all have their own text.
		/// </remarks>
		public class TextAnnotation : ICoreAnnotation<string>
		{
			// only static members
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting the lemma (morphological stem) of a token.</summary>
		/// <remarks>
		/// The CoreMap key for getting the lemma (morphological stem) of a token.
		/// This key is typically set on token annotations.
		/// TODO: merge with StemAnnotation?
		/// </remarks>
		public class LemmaAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting the Penn part of speech of a token.</summary>
		/// <remarks>
		/// The CoreMap key for getting the Penn part of speech of a token.
		/// This key is typically set on token annotations.
		/// </remarks>
		public class PartOfSpeechAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The CoreMap key for getting the token-level named entity tag (e.g., DATE,
		/// PERSON, etc.)
		/// This key is typically set on token annotations.
		/// </summary>
		public class NamedEntityTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting the coarse named entity tag (i.e.</summary>
		/// <remarks>The CoreMap key for getting the coarse named entity tag (i.e. LOCATION)</remarks>
		public class CoarseNamedEntityTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting the fine grained named entity tag (i.e.</summary>
		/// <remarks>The CoreMap key for getting the fine grained named entity tag (i.e. CITY)</remarks>
		public class FineGrainedNamedEntityTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The CoreMap key for getting the token-level named entity tag (e.g., DATE,
		/// PERSON, etc.) from a previous NER tagger.
		/// </summary>
		/// <remarks>
		/// The CoreMap key for getting the token-level named entity tag (e.g., DATE,
		/// PERSON, etc.) from a previous NER tagger. NERFeatureFactory is sensitive to
		/// this tag and will turn the annotations from the previous NER tagger into
		/// new features. This is currently used to implement one level of stacking --
		/// we may later change it to take a list as needed.
		/// This key is typically set on token annotations.
		/// </remarks>
		public class StackedNamedEntityTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The CoreMap key for getting the token-level true case annotation (e.g.,
		/// INIT_UPPER)
		/// This key is typically set on token annotations.
		/// </summary>
		public class TrueCaseAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key identifying the annotation's true-cased text.</summary>
		/// <remarks>
		/// The CoreMap key identifying the annotation's true-cased text.
		/// Note that this key is intended to be used with many different kinds of
		/// annotations - documents, sentences and tokens all have their own text.
		/// </remarks>
		public class TrueCaseTextAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting the tokens contained by an annotation.</summary>
		/// <remarks>
		/// The CoreMap key for getting the tokens contained by an annotation.
		/// This key should be set for any annotation that contains tokens. It can be
		/// done without much memory overhead using List.subList.
		/// </remarks>
		public class TokensAnnotation : ICoreAnnotation<IList<CoreLabel>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the tokens (can be words, phrases or anything that are of type CoreMap) contained by an annotation.</summary>
		/// <remarks>
		/// The CoreMap key for getting the tokens (can be words, phrases or anything that are of type CoreMap) contained by an annotation.
		/// This key should be set for any annotation that contains tokens (words, phrases etc). It can be
		/// done without much memory overhead using List.subList.
		/// </remarks>
		public class GenericTokensAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the sentences contained in an annotation.</summary>
		/// <remarks>
		/// The CoreMap key for getting the sentences contained in an annotation.
		/// The sentences are represented as a
		/// <c>List&lt;CoreMap&gt;</c>
		/// .
		/// Each sentence might typically have annotations such as
		/// <c>TextAnnotation</c>
		/// ,
		/// <c>TokensAnnotation</c>
		/// ,
		/// <c>SentenceIndexAnnotation</c>
		/// , and
		/// <c>BasicDependenciesAnnotation</c>
		/// .
		/// This key is typically set only on document annotations.
		/// </remarks>
		public class SentencesAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the quotations contained by an annotation.</summary>
		/// <remarks>
		/// The CoreMap key for getting the quotations contained by an annotation.
		/// This key is typically set only on document annotations.
		/// </remarks>
		public class QuotationsAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>The CoreMap key for getting the quotations contained by an annotation.</summary>
		/// <remarks>
		/// The CoreMap key for getting the quotations contained by an annotation.
		/// This key is typically set only on document annotations.
		/// </remarks>
		public class UnclosedQuotationsAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>Unique identifier within a document for a given quotation.</summary>
		public class QuotationIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>The index of the sentence that this annotation begins in.</summary>
		public class SentenceBeginAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>The index of the sentence that this annotation begins in.</summary>
		public class SentenceEndAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>The CoreMap key for getting the paragraphs contained by an annotation.</summary>
		/// <remarks>
		/// The CoreMap key for getting the paragraphs contained by an annotation.
		/// This key is typically set only on document annotations.
		/// </remarks>
		public class ParagraphsAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>The CoreMap key identifying the first token included in an annotation.</summary>
		/// <remarks>
		/// The CoreMap key identifying the first token included in an annotation. The
		/// token with index 0 is the first token in the document.
		/// This key should be set for any annotation that contains tokens.
		/// </remarks>
		public class TokenBeginAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>The CoreMap key identifying the last token after the end of an annotation.</summary>
		/// <remarks>
		/// The CoreMap key identifying the last token after the end of an annotation.
		/// The token with index 0 is the first token in the document.
		/// This key should be set for any annotation that contains tokens.
		/// </remarks>
		public class TokenEndAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// The CoreMap key identifying the date and time associated with an
		/// annotation.
		/// </summary>
		/// <remarks>
		/// The CoreMap key identifying the date and time associated with an
		/// annotation.
		/// This key is typically set on document annotations.
		/// </remarks>
		public class CalendarAnnotation : ICoreAnnotation<Calendar>
		{
			public virtual Type GetType()
			{
				return typeof(Calendar);
			}
		}

		/// <summary>
		/// This refers to the unique identifier for a "document", where document may
		/// vary based on your application.
		/// </summary>
		public class DocIDAnnotation : ICoreAnnotation<string>
		{
			/*
			* These are the keys hashed on by IndexedWord
			*/
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>This indexes a token number inside a sentence.</summary>
		/// <remarks>
		/// This indexes a token number inside a sentence.  Standardly, tokens are
		/// indexed within a sentence starting at 1 (not 0: we follow common parlance
		/// whereby we speak of the first word of a sentence).
		/// This is generally an individual word or feature index - it is local, and
		/// may not be uniquely identifying without other identifiers such as sentence
		/// and doc. However, if these are the same, the index annotation should be a
		/// unique identifier for differentiating objects.
		/// </remarks>
		public class IndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// This indexes the beginning of a span of words, e.g., a constituent in a
		/// tree.
		/// </summary>
		/// <remarks>
		/// This indexes the beginning of a span of words, e.g., a constituent in a
		/// tree. See
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree.IndexSpans(int)"/>
		/// .
		/// This annotation counts tokens.
		/// It standardly indexes from 1 (like IndexAnnotation).  The reasons for
		/// this are: (i) Talking about the first word of a sentence is kind of
		/// natural, and (ii) We use index 0 to refer to an imaginary root in
		/// dependency output.
		/// </remarks>
		public class BeginIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// This indexes the end of a span of words, e.g., a constituent in a
		/// tree.
		/// </summary>
		/// <remarks>
		/// This indexes the end of a span of words, e.g., a constituent in a
		/// tree.  See
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree.IndexSpans(int)"/>
		/// . This annotation
		/// counts tokens.  It standardly indexes from 1 (like IndexAnnotation).
		/// The end index is not a fencepost: its value is equal to the
		/// IndexAnnotation of the last word in the span.
		/// </remarks>
		public class EndIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// This indicates that starting at this token, the sentence should not be ended until
		/// we see a ForcedSentenceEndAnnotation.
		/// </summary>
		/// <remarks>
		/// This indicates that starting at this token, the sentence should not be ended until
		/// we see a ForcedSentenceEndAnnotation.  Used to force the ssplit annotator
		/// (eg the WordToSentenceProcessor) to keep tokens in the same sentence
		/// until ForcedSentenceEndAnnotation is seen.
		/// </remarks>
		public class ForcedSentenceUntilEndAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>This indicates the sentence should end at this token.</summary>
		/// <remarks>
		/// This indicates the sentence should end at this token.  Used to
		/// force the ssplit annotator (eg the WordToSentenceProcessor) to
		/// start a new sentence at the next token.
		/// </remarks>
		public class ForcedSentenceEndAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>Unique identifier within a document for a given sentence.</summary>
		public class SentenceIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// Line number for a sentence in a document delimited by newlines
		/// instead of punctuation.
		/// </summary>
		/// <remarks>
		/// Line number for a sentence in a document delimited by newlines
		/// instead of punctuation.  May skip numbers if there are blank
		/// lines not represented as sentences.  Indexed from 1 rather than 0.
		/// </remarks>
		public class LineNumberAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>Contains the "value" - an ill-defined string used widely in MapLabel.</summary>
		public class ValueAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class CategoryAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The exact original surface form of a token.</summary>
		/// <remarks>
		/// The exact original surface form of a token.  This is created in the
		/// invertible PTBTokenizer. The tokenizer may normalize the token form to
		/// match what appears in the PTB, but this key will hold the original characters.
		/// </remarks>
		public class OriginalTextAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Annotation for the whitespace characters appearing before this word.</summary>
		/// <remarks>
		/// Annotation for the whitespace characters appearing before this word. This
		/// can be filled in by an invertible tokenizer so that the original text string can be
		/// reconstructed.
		/// </remarks>
		public class BeforeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Annotation for the whitespace characters appear after this word.</summary>
		/// <remarks>
		/// Annotation for the whitespace characters appear after this word. This can
		/// be filled in by an invertible tokenizer so that the original text string can be
		/// reconstructed.
		/// Note: When running a tokenizer token-by-token, in general this field will only
		/// be filled in after the next token is read, so you need to be reading this field
		/// one behind. Be careful about this.
		/// </remarks>
		public class AfterAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>CoNLL dep parsing - coarser POS tags.</summary>
		public class CoarseTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>CoNLL dep parsing - the dependency type, such as SBJ or OBJ.</summary>
		/// <remarks>CoNLL dep parsing - the dependency type, such as SBJ or OBJ. This should be unified with CoNLLDepTypeAnnotation.</remarks>
		public class CoNLLDepAnnotation : ICoreAnnotation<ICoreMap>
		{
			public virtual Type GetType()
			{
				return typeof(ICoreMap);
			}
		}

		/// <summary>CoNLL SRL/dep parsing - whether the word is a predicate</summary>
		public class CoNLLPredicateAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>
		/// CoNLL SRL/dep parsing - map which, for the current word, specifies its
		/// specific role for each predicate
		/// </summary>
		public class CoNLLSRLAnnotation : ICoreAnnotation<IDictionary<int, string>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IDictionary));
			}
		}

		/// <summary>CoNLL dep parsing - the dependency type, such as SBJ or OBJ.</summary>
		/// <remarks>CoNLL dep parsing - the dependency type, such as SBJ or OBJ. This should be unified with CoNLLDepAnnotation.</remarks>
		public class CoNLLDepTypeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>CoNLL-U dep parsing - span of multiword tokens</summary>
		public class CoNLLUTokenSpanAnnotation : ICoreAnnotation<IntPair>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(Pair));
			}
		}

		/// <summary>CoNLL-U dep parsing - List of secondary dependencies</summary>
		public class CoNLLUSecondaryDepsAnnotation : ICoreAnnotation<Dictionary<string, string>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(Pair));
			}
		}

		/// <summary>CoNLL-U dep parsing - List of morphological features</summary>
		public class CoNLLUFeats : ICoreAnnotation<Dictionary<string, string>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(Hashtable));
			}
		}

		/// <summary>CoNLL-U dep parsing - Any other annotation</summary>
		public class CoNLLUMisc : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// CoNLL dep parsing - the index of the word which is the parent of this word
		/// in the dependency tree
		/// </summary>
		public class CoNLLDepParentIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>Inverse document frequency of the word this label represents</summary>
		public class IDFAnnotation : ICoreAnnotation<double>
		{
			public virtual Type GetType()
			{
				return typeof(double);
			}
		}

		/// <summary>The standard key for a propbank label which is of type Argument</summary>
		public class ArgumentAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Another key used for propbank - to signify core arg nodes or predicate
		/// nodes
		/// </summary>
		public class MarkingAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for Semantic Head Word which is a String</summary>
		public class SemanticHeadWordAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for Semantic Head Word POS which is a String</summary>
		public class SemanticHeadTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Probank key for the Verb sense given in the Propbank Annotation, should
		/// only be in the verbnode
		/// </summary>
		public class VerbSenseAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for storing category with functional tags.</summary>
		public class CategoryFunctionalTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// This is an NER ID annotation (in case the all caps parsing didn't work out
		/// for you...)
		/// </summary>
		public class NERIDAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The key for the normalized value of numeric named entities.</summary>
		public class NormalizedNamedEntityTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public enum SRL_ID
		{
			Arg,
			No,
			AllNo,
			Rel
		}

		/// <summary>
		/// The key for semantic role labels (Note: please add to this description if
		/// you use this key)
		/// </summary>
		public class SRLIDAnnotation : ICoreAnnotation<CoreAnnotations.SRL_ID>
		{
			public virtual Type GetType()
			{
				return typeof(CoreAnnotations.SRL_ID);
			}
		}

		/// <summary>
		/// The standard key for the "shape" of a word: a String representing the type
		/// of characters in a word, such as "Xx" for a capitalized word.
		/// </summary>
		/// <remarks>
		/// The standard key for the "shape" of a word: a String representing the type
		/// of characters in a word, such as "Xx" for a capitalized word. See
		/// <see cref="Edu.Stanford.Nlp.Process.WordShapeClassifier"/>
		/// for functions for
		/// making shape strings.
		/// </remarks>
		public class ShapeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The Standard key for storing the left terminal number relative to the root
		/// of the tree of the leftmost terminal dominated by the current node
		/// </summary>
		public class LeftTermAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>The standard key for the parent which is a String</summary>
		public class ParentAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class INAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for span which is an IntPair</summary>
		public class SpanAnnotation : ICoreAnnotation<IntPair>
		{
			public virtual Type GetType()
			{
				return typeof(IntPair);
			}
		}

		/// <summary>The standard key for the answer which is a String</summary>
		public class AnswerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for the answer which is a String</summary>
		public class PresetAnswerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for gold answer which is a String</summary>
		public class GoldAnswerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for the features which is a Collection</summary>
		public class FeaturesAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for the semantic interpretation</summary>
		public class InterpretationAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for the semantic role label of a phrase.</summary>
		public class RoleAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The standard key for the gazetteer information</summary>
		public class GazetteerAnnotation : ICoreAnnotation<IList<string>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>Morphological stem of the word this label represents</summary>
		public class StemAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PolarityAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MorphoNumAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MorphoPersAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MorphoGenAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MorphoCaseAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>For Chinese: character level information, segmentation.</summary>
		/// <remarks>
		/// For Chinese: character level information, segmentation. Used for representing
		/// a single character as a token.
		/// </remarks>
		public class ChineseCharAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>For Chinese: the segmentation info existing in the original text.</summary>
		public class ChineseOrigSegAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>For Chinese: the segmentation information from the segmenter.</summary>
		/// <remarks>
		/// For Chinese: the segmentation information from the segmenter.
		/// Either a "1" for a new word starting at this position or a "0".
		/// </remarks>
		public class ChineseSegAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Not sure exactly what this is, but it is different from
		/// ChineseSegAnnotation and seems to indicate if the text is segmented
		/// </summary>
		public class ChineseIsSegmentedAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>for Arabic: character level information, segmentation</summary>
		public class ArabicCharAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>For Arabic: the segmentation information from the segmenter.</summary>
		public class ArabicSegAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The CoreMap key identifying the offset of the first char of an
		/// annotation.
		/// </summary>
		/// <remarks>
		/// The CoreMap key identifying the offset of the first char of an
		/// annotation. The char with index 0 is the first char in the
		/// document.
		/// Note that these are currently measured in terms of UTF-16 char offsets, not codepoints,
		/// so that when non-BMP Unicode characters are present, such a character will add 2 to
		/// the position. On the other hand, these values will work with String#substring() and
		/// you can then calculate the number of codepoints in a substring.
		/// This key should be set for any annotation that represents a span of text.
		/// </remarks>
		public class CharacterOffsetBeginAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>
		/// The CoreMap key identifying the offset of the last character after the end
		/// of an annotation.
		/// </summary>
		/// <remarks>
		/// The CoreMap key identifying the offset of the last character after the end
		/// of an annotation. The character with index 0 is the first character in the
		/// document.
		/// Note that these are currently measured in terms of UTF-16 char offsets, not codepoints,
		/// so that when non-BMP Unicode characters are present, such a character will add 2 to
		/// the position. On the other hand, these values will work with String#substring() and
		/// you can then calculate the number of codepoints in a substring.
		/// This key should be set for any annotation that represents a span of text.
		/// </remarks>
		public class CharacterOffsetEndAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>Key for relative value of a word - used in RTE</summary>
		public class CostMagnificationAnnotation : ICoreAnnotation<double>
		{
			public virtual Type GetType()
			{
				return typeof(double);
			}
		}

		public class WordSenseAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SRLInstancesAnnotation : ICoreAnnotation<IList<IList<Pair<string, Pair>>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>
		/// Used by RTE to track number of text sentences, to determine when hyp
		/// sentences begin.
		/// </summary>
		public class NumTxtSentencesAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>Used in Trees</summary>
		public class TagLabelAnnotation : ICoreAnnotation<ILabel>
		{
			public virtual Type GetType()
			{
				return typeof(ILabel);
			}
		}

		/// <summary>
		/// Used in CRFClassifier stuff PositionAnnotation should possibly be an int -
		/// it's present as either an int or string depending on context CharAnnotation
		/// may be "CharacterAnnotation" - not sure
		/// </summary>
		public class DomainAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PositionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class CharAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Note: this is not a catchall "unknown" annotation but seems to have a
		/// specific meaning for sequence classifiers
		/// </summary>
		public class UnknownAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class IDAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Possibly this should be grouped with gazetteer annotation - original key
		/// was "gaz".
		/// </summary>
		public class GazAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PossibleAnswersAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class DistSimAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class AbbrAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class ChunkAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class GovernorAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class AbgeneAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class GeniaAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class AbstrAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class FreqAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class DictAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class WebAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class FemaleGazAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MaleGazAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LastGazAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>it really seems like this should have a different name or else be a boolean</summary>
		public class IsURLAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LinkAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MentionsAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>index into the list of entity mentions in a document</summary>
		public class EntityMentionIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(int));
			}
		}

		/// <summary>
		/// index into the list of entity mentions in a document for canonical entity mention
		/// ...this is primarily for linking entity mentions to their canonical entity mention
		/// </summary>
		public class CanonicalEntityMentionIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(int));
			}
		}

		/// <summary>mapping from coref mentions to corresponding ner derived entity mentions</summary>
		public class CorefMentionToEntityMentionMappingAnnotation : ICoreAnnotation<IDictionary<int, int>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IDictionary));
			}
		}

		/// <summary>mapping from ner derived entity mentions to coref mentions</summary>
		public class EntityMentionToCorefMentionMappingAnnotation : ICoreAnnotation<IDictionary<int, int>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IDictionary));
			}
		}

		public class EntityTypeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>it really seems like this should have a different name or else be a boolean</summary>
		public class IsDateRangeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PredictedAnswerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Seems like this could be consolidated with something else...</summary>
		public class OriginalCharAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class UTypeAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		public class EntityRuleAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Store a list of sections in the document</summary>
		public class SectionsAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>Store an index into a list of sections</summary>
		public class SectionIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(int));
			}
		}

		/// <summary>Store the beginning of the author mention for this section</summary>
		public class SectionAuthorCharacterOffsetBeginAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(int));
			}
		}

		/// <summary>Store the end of the author mention for this section</summary>
		public class SectionAuthorCharacterOffsetEndAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(int));
			}
		}

		/// <summary>Store the xml tag for the section as a CoreLabel</summary>
		public class SectionTagAnnotation : ICoreAnnotation<CoreLabel>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(CoreLabel));
			}
		}

		/// <summary>Store a list of CoreMaps representing quotes</summary>
		public class QuotesAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>Indicate whether a sentence is quoted</summary>
		public class QuotedAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>Section of a document</summary>
		public class SectionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Date for a section of a document</summary>
		public class SectionDateAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Id for a section of a document</summary>
		public class SectionIDAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Indicates that the token starts a new section and the attributes
		/// that should go into that section
		/// </summary>
		public class SectionStartAnnotation : ICoreAnnotation<ICoreMap>
		{
			public virtual Type GetType()
			{
				return typeof(ICoreMap);
			}
		}

		/// <summary>Indicates that the token end a section and the label of the section</summary>
		public class SectionEndAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class WordPositionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class ParaPositionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SentencePositionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SentenceIDAnnotation : ICoreAnnotation<string>
		{
			// Why do both this and sentenceposannotation exist? I don't know, but one
			// class
			// uses both so here they remain for now...
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class EntityClassAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class AnswerObjectAnnotation : ICoreAnnotation<object>
		{
			public virtual Type GetType()
			{
				return typeof(object);
			}
		}

		/// <summary>Used in Task3 Pascal system</summary>
		public class BestCliquesAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class BestFullAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LastTaggedAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Used in wsd.supwsd package</summary>
		public class LabelAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class NeighborsAnnotation : ICoreAnnotation<IList<Pair<WordLemmaTag, string>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public class ContextsAnnotation : ICoreAnnotation<IList<Pair<string, string>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public class DependentsAnnotation : ICoreAnnotation<IList<Pair<Triple<string, string, string>, string>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public class WordFormAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class TrueTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SubcategorizationAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class BagOfWordsAnnotation : ICoreAnnotation<IList<Pair<string, string>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>Used in srl.unsup</summary>
		public class HeightAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LengthAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Used in Gale2007ChineseSegmenter</summary>
		public class LBeginAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LMiddleAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LEndAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class D2_LBeginAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class D2_LMiddleAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class D2_LEndAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class UBlockAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Used in Chinese segmenters for whether there was space before a character.</summary>
		public class SpaceBeforeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The base version of the parser state, like NP or VBZ or ...</summary>
		public class StateAnnotation : ICoreAnnotation<CoreLabel>
		{
			/*
			* Used in parser.discrim
			*/
			public virtual Type GetType()
			{
				return typeof(CoreLabel);
			}
		}

		/// <summary>used in binarized trees to say the name of the most recent child</summary>
		public class PrevChildAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// used in binarized trees to specify the first child in the rule for which
		/// this node is the parent
		/// </summary>
		public class FirstChildAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>whether the node is the parent in a unary rule</summary>
		public class UnaryAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>annotation stolen from the lex parser</summary>
		public class DoAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>annotation stolen from the lex parser</summary>
		public class HaveAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>annotation stolen from the lex parser</summary>
		public class BeAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>annotation stolen from the lex parser</summary>
		public class NotAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>annotation stolen from the lex parser</summary>
		public class PercentAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>specifies the base state of the parent of this node in the parse tree</summary>
		public class GrandparentAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The key for storing a Head word as a string rather than a pointer (as in
		/// TreeCoreAnnotations.HeadWordAnnotation)
		/// </summary>
		public class HeadWordStringAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Used in nlp.coref</summary>
		public class MonthAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class DayAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class YearAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Used in propbank.srl</summary>
		public class PriorAnnotation : ICoreAnnotation<IDictionary<string, double>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IDictionary));
			}
		}

		public class SemanticWordAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SemanticTagAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class CovertIDAnnotation : ICoreAnnotation<IList<IntPair>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public class ArgDescendentAnnotation : ICoreAnnotation<Pair<string, double>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(Pair));
			}
		}

		/// <summary>Used in SimpleXMLAnnotator.</summary>
		/// <remarks>
		/// Used in SimpleXMLAnnotator. The value is an XML element name String for the
		/// innermost element in which this token was contained.
		/// </remarks>
		public class XmlElementAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Used in CleanXMLAnnotator.</summary>
		/// <remarks>
		/// Used in CleanXMLAnnotator.  The value is a list of XML element names indicating
		/// the XML tag the token was nested inside.
		/// </remarks>
		public class XmlContextAnnotation : ICoreAnnotation<IList<string>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>Used for Topic Assignments from LDA or its equivalent models.</summary>
		/// <remarks>
		/// Used for Topic Assignments from LDA or its equivalent models. The value is
		/// the topic ID assigned to the current token.
		/// </remarks>
		public class TopicAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class WordnetSynAnnotation : ICoreAnnotation<string>
		{
			// gets the synonymn of a word in the Wordnet (use a bit differently in sonalg's code)
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PhraseWordsTagAnnotation : ICoreAnnotation<string>
		{
			//to get words of the phrase
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PhraseWordsAnnotation : ICoreAnnotation<IList<string>>
		{
			//to get pos tag of the phrase i.e. root of the phrase tree in the parse tree
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public class ProtoAnnotation : ICoreAnnotation<string>
		{
			//to get prototype feature, see Haghighi Exemplar driven learning
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class CommonWordsAnnotation : ICoreAnnotation<string>
		{
			//which common words list does this word belong to
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class DocDateAnnotation : ICoreAnnotation<string>
		{
			// Document date
			// Needed by SUTime
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Document type
		/// What kind of document is it: story, multi-part article, listing, email, etc
		/// </summary>
		public class DocTypeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Document source type
		/// What kind of place did the document come from: newswire, discussion forum, web...
		/// </summary>
		public class DocSourceTypeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Document title
		/// What is the document title
		/// </summary>
		public class DocTitleAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Reference location for the document</summary>
		public class LocationAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// Author for the document
		/// (really should be a set of authors, but just have single string for simplicity)
		/// </summary>
		public class AuthorAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class NumericTypeAnnotation : ICoreAnnotation<string>
		{
			// Numeric annotations
			// Per token annotation indicating whether the token represents a NUMBER or ORDINAL
			// (twenty first => NUMBER ORDINAL)
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class NumericValueAnnotation : ICoreAnnotation<Number>
		{
			// Per token annotation indicating the numeric value of the token
			// (twenty first => 20 1)
			public virtual Type GetType()
			{
				return typeof(Number);
			}
		}

		public class NumericObjectAnnotation : ICoreAnnotation<object>
		{
			// Per token annotation indicating the numeric object associated with an annotation
			public virtual Type GetType()
			{
				return typeof(object);
			}
		}

		/// <summary>
		/// Annotation indicating whether the numeric phrase the token is part of
		/// represents a NUMBER or ORDINAL (twenty first
		/// <literal>=&gt;</literal>
		/// ORDINAL ORDINAL).
		/// </summary>
		public class NumericCompositeValueAnnotation : ICoreAnnotation<Number>
		{
			public virtual Type GetType()
			{
				return typeof(Number);
			}
		}

		/// <summary>
		/// Annotation indicating the numeric value of the phrase the token is part of
		/// (twenty first
		/// <literal>=&gt;</literal>
		/// 21 21 ).
		/// </summary>
		public class NumericCompositeTypeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>Annotation indicating the numeric object associated with an annotation.</summary>
		public class NumericCompositeObjectAnnotation : ICoreAnnotation<object>
		{
			public virtual Type GetType()
			{
				return typeof(object);
			}
		}

		public class NumerizedTokensAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>used in dcoref.</summary>
		/// <remarks>
		/// used in dcoref.
		/// to indicate that the it should use the discourse information annotated in the document
		/// </remarks>
		public class UseMarkedDiscourseAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		/// <summary>used in dcoref.</summary>
		/// <remarks>
		/// used in dcoref.
		/// to store discourse information. (marking
		/// <c>&lt;TURN&gt;</c>
		/// or quotation)
		/// </remarks>
		public class UtteranceAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>used in dcoref.</summary>
		/// <remarks>
		/// used in dcoref.
		/// to store speaker information.
		/// </remarks>
		public class SpeakerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>used in dcoref.</summary>
		/// <remarks>
		/// used in dcoref.
		/// to store paragraph information.
		/// </remarks>
		public class ParagraphAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>used in ParagraphAnnotator.</summary>
		/// <remarks>
		/// used in ParagraphAnnotator.
		/// to store paragraph information.
		/// </remarks>
		public class ParagraphIndexAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>used in dcoref.</summary>
		/// <remarks>
		/// used in dcoref.
		/// to store premarked entity mentions.
		/// </remarks>
		public class MentionTokenAnnotation : ICoreAnnotation<MultiTokenTag>
		{
			public virtual Type GetType()
			{
				return typeof(MultiTokenTag);
			}
		}

		/// <summary>used in incremental DAG parser</summary>
		public class LeftChildrenNodeAnnotation : ICoreAnnotation<ISortedSet<Pair<CoreLabel, string>>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(ISortedSet));
			}
		}

		/// <summary>Stores an exception associated with processing this document</summary>
		public class ExceptionAnnotation : ICoreAnnotation<Exception>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(Exception));
			}
		}

		/// <summary>The CoreMap key identifying the annotation's antecedent.</summary>
		/// <remarks>
		/// The CoreMap key identifying the annotation's antecedent.
		/// The intent of this annotation is to go with words that have been
		/// linked via coref to some other entity.  For example, if "dog" is
		/// corefed to "cirrus" in the sentence "Cirrus, a small dog, ate an
		/// entire pumpkin pie", then "dog" would have the
		/// AntecedentAnnotation "cirrus".
		/// This annotation is currently used ONLY in the KBP slot filling project.
		/// In that project, "cirrus" from the example above would also have an
		/// AntecedentAnnotation of "cirrus".
		/// Generally, you want to use the usual coref graph annotations
		/// </remarks>
		public class AntecedentAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LabelWeightAnnotation : ICoreAnnotation<double>
		{
			public virtual Type GetType()
			{
				return typeof(double);
			}
		}

		public class ColumnDataClassifierAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class LabelIDAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		/// <summary>An annotation for a sentence tagged with its KBP relation.</summary>
		/// <remarks>
		/// An annotation for a sentence tagged with its KBP relation.
		/// Attaches to a sentence.
		/// </remarks>
		/// <seealso cref="Edu.Stanford.Nlp.Pipeline.KBPAnnotator"/>
		public class KBPTriplesAnnotation : ICoreAnnotation<IList<RelationTriple>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		/// <summary>
		/// An annotation for the Wikipedia page (i.e., canonical name) associated with
		/// this token.
		/// </summary>
		/// <remarks>
		/// An annotation for the Wikipedia page (i.e., canonical name) associated with
		/// this token.
		/// This is the recommended annotation to use for entity linking that links to Wikipedia.
		/// Attaches to a token, as well as to a mention (see (@link MentionsAnnotation}).
		/// </remarks>
		/// <seealso cref="Edu.Stanford.Nlp.Pipeline.WikidictAnnotator"/>
		public class WikipediaEntityAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(string));
			}
		}

		/// <summary>
		/// The CoreMap key identifying the annotation's text, as formatted by the
		/// <see cref="Edu.Stanford.Nlp.Naturalli.QuestionToStatementTranslator"/>
		/// .
		/// This is attached to
		/// <see cref="CoreLabel"/>
		/// s.
		/// </summary>
		public class StatementTextAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key identifying an entity mention's potential gender.</summary>
		/// <remarks>
		/// The CoreMap key identifying an entity mention's potential gender.
		/// This is attached to
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// s.
		/// </remarks>
		public class GenderAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>
		/// The CoreLabel key identifying whether a token is a newline or not
		/// This is attached to
		/// <see cref="CoreLabel"/>
		/// s.
		/// </summary>
		public class IsNewlineAnnotation : ICoreAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}
	}
}

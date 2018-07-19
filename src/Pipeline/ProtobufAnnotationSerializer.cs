using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Neural.Rnn;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// <p>
	/// A serializer using Google's protocol buffer format.
	/// </summary>
	/// <remarks>
	/// <p>
	/// A serializer using Google's protocol buffer format.
	/// The files produced by this serializer, in addition to being language-independent,
	/// are a little over 10% the size and 4x faster to read+write versus the default Java serialization
	/// (see GenericAnnotationSerializer), when both files are compressed with gzip.
	/// </p>
	/// <p>
	/// Note that this handles only a subset of the possible annotations
	/// that can be attached to a sentence. Nonetheless, it is guaranteed to be
	/// lossless with the default set of named annotators you can create from a
	/// <see cref="StanfordCoreNLP"/>
	/// pipeline, with default properties defined for each annotator.
	/// Note that the serializer does not gzip automatically -- this must be done by passing in a GZipOutputStream
	/// and calling a GZipInputStream manually. For most Annotations, gzipping provides a notable decrease in size (~2.5x)
	/// due to most of the data being raw Strings.
	/// </p>
	/// <p>
	/// To allow lossy serialization, use
	/// <see cref="ProtobufAnnotationSerializer(bool)"/>
	/// .
	/// Otherwise, an exception is thrown if an unknown key appears in the annotation which would not be saved to th
	/// protocol buffer.
	/// If such keys exist, and are a part of the standard CoreNLP pipeline, please let us know!
	/// If you would like to serialize keys in addition to those serialized by default (e.g., you are attaching
	/// your own annotations), then you should do the following:
	/// </p>
	/// <ol>
	/// <li>
	/// Create a .proto file which extends one or more of Document, Sentence, or Token. Each of these have fields
	/// 100-255 left open for user extensions. An example of such an extension is:
	/// <pre>
	/// package edu.stanford.nlp.pipeline;
	/// option java_package = "com.example.my.awesome.nlp.app";
	/// option java_outer_classname = "MyAppProtos";
	/// import "CoreNLP.proto";
	/// extend Sentence {
	/// optional uint32 myNewField    = 101;
	/// }
	/// </pre>
	/// </li>
	/// <li>
	/// Compile your .proto file with protoc. For example (from CORENLP_HOME):
	/// <pre>
	/// protoc -I=src/edu/stanford/nlp/pipeline/:/path/to/folder/contining/your/proto/file --java_out=/path/to/output/src/folder/  /path/to/proto/file
	/// </pre>
	/// </li>
	/// <li>
	/// <p>
	/// Extend
	/// <see cref="ProtobufAnnotationSerializer"/>
	/// to serialize and deserialize your field.
	/// Generally, this entail overriding two functions -- one to write the proto and one to read it.
	/// In both cases, you usually want to call the superclass' implementation of the function, and add on to it
	/// from there.
	/// In our running example, adding a field to the
	/// <see cref="Sentence"/>
	/// proto, you would overwrite:
	/// </p>
	/// <ul>
	/// <li>
	/// <see cref="ToProtoBuilder(Edu.Stanford.Nlp.Util.ICoreMap, System.Collections.Generic.ICollection{E})"/>
	/// </li>
	/// <li>
	/// <see cref="FromProtoNoTokens(Sentence)"/>
	/// </li>
	/// </ul>
	/// <p>
	/// Note, importantly, that for the serializer to be able to check for lossless serialization, all annotations added
	/// to the proto must be registered as added by being removed from the set passed to
	/// <see cref="ToProtoBuilder(Edu.Stanford.Nlp.Util.ICoreMap, System.Collections.Generic.ICollection{E})"/>
	/// (and the analogous
	/// functions for documents and tokens).
	/// </p>
	/// <p>
	/// Lastly, the new annotations must be registered in the original .proto file; this can be achieved by including
	/// a static block in the overwritten class:
	/// </p>
	/// <pre>
	/// static {
	/// ExtensionRegistry registry = ExtensionRegistry.newInstance();
	/// registry.add(MyAppProtos.myNewField);
	/// CoreNLPProtos.registerAllExtensions(registry);
	/// }
	/// </pre>
	/// </li>
	/// </ol>
	/// TODOs
	/// <ul>
	/// <li>In CoreNLP, the leaves of a tree are == to the tokens in a sentence. This is not the case for a deserialized proto.</li>
	/// </ul>
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class ProtobufAnnotationSerializer : AnnotationSerializer
	{
		/// <summary>A global lock; necessary since dependency tree creation is not threadsafe</summary>
		private static readonly object globalLock = "I'm a lock :)";

		/// <summary>An exception to denote that the serialization would be lossy.</summary>
		/// <remarks>
		/// An exception to denote that the serialization would be lossy.
		/// This exception is thrown at serialization time.
		/// </remarks>
		/// <seealso cref="ProtobufAnnotationSerializer.enforceLosslessSerialization"/>
		/// <seealso cref="ProtobufAnnotationSerializer.ProtobufAnnotationSerializer(bool)"/>
		[System.Serializable]
		public class LossySerializationException : Exception
		{
			private const long serialVersionUID = 741506383659886245L;

			private LossySerializationException(string msg)
				: base(msg)
			{
			}
		}

		/// <summary>
		/// If true, serialization is guaranteed to be lossless or else a runtime exception is thrown
		/// at serialization time.
		/// </summary>
		public readonly bool enforceLosslessSerialization;

		/// <summary>Create a new Annotation serializer outputting to a protocol buffer format.</summary>
		/// <remarks>
		/// Create a new Annotation serializer outputting to a protocol buffer format.
		/// This is guaranteed to either be a lossless compression, or throw an exception at
		/// serialization time.
		/// </remarks>
		public ProtobufAnnotationSerializer()
			: this(true)
		{
		}

		/// <summary>Create a new Annotation serializer outputting to a protocol buffer format.</summary>
		/// <param name="enforceLosslessSerialization">
		/// If set to true, a
		/// <see cref="LossySerializationException"/>
		/// is thrown at serialization
		/// time if the serialization would be lossy. If set to false,
		/// these exceptions are ignored.
		/// </param>
		public ProtobufAnnotationSerializer(bool enforceLosslessSerialization)
		{
			this.enforceLosslessSerialization = enforceLosslessSerialization;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public override OutputStream Write(Annotation corpus, OutputStream os)
		{
			CoreNLPProtos.Document serialized = ToProto(corpus);
			serialized.WriteDelimitedTo(os);
			os.Flush();
			return os;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public override Pair<Annotation, InputStream> Read(InputStream @is)
		{
			CoreNLPProtos.Document doc = CoreNLPProtos.Document.ParseDelimitedFrom(@is);
			return Pair.MakePair(FromProto(doc), @is);
		}

		/// <summary>Read a single protocol buffer, which constitutes the entire stream.</summary>
		/// <remarks>
		/// Read a single protocol buffer, which constitutes the entire stream.
		/// This is in contrast to the default, where mutliple buffers may come out of the stream,
		/// and therefore each one is prepended by the length of the buffer to follow.
		/// </remarks>
		/// <param name="in">The file to read.</param>
		/// <returns>A parsed Annotation.</returns>
		/// <exception cref="System.IO.IOException">In case the stream cannot be read from.</exception>
		public virtual Annotation ReadUndelimited(File @in)
		{
			CoreNLPProtos.Document doc;
			try
			{
				using (FileInputStream delimited = new FileInputStream(@in))
				{
					doc = CoreNLPProtos.Document.ParseFrom(delimited);
				}
			}
			catch (Exception)
			{
				using (FileInputStream undelimited = new FileInputStream(@in))
				{
					doc = CoreNLPProtos.Document.ParseDelimitedFrom(undelimited);
				}
			}
			return FromProto(doc);
		}

		/// <summary>Get a particular key from a CoreMap, registering it as being retrieved.</summary>
		/// <param name="map">The CoreMap to retrieve the key from.</param>
		/// <param name="keysToRegister">A set of keys to remove this key from, representing to keys which should be retrieved by the serializer.</param>
		/// <param name="key">The key key to retrieve.</param>
		/// <?/>
		/// <returns>CoreMap.get(key)</returns>
		private static E GetAndRegister<E>(ICoreMap map, ICollection<Type> keysToRegister, Type key)
		{
			keysToRegister.Remove(key);
			return map.Get(key);
		}

		/// <summary>Create a CoreLabel proto from a CoreLabel instance.</summary>
		/// <remarks>
		/// Create a CoreLabel proto from a CoreLabel instance.
		/// This is not static, as it optionally throws an exception if the serialization is lossy.
		/// </remarks>
		/// <param name="coreLabel">The CoreLabel to convert</param>
		/// <returns>A protocol buffer message corresponding to this CoreLabel</returns>
		public virtual CoreNLPProtos.Token ToProto(CoreLabel coreLabel)
		{
			ICollection<Type> keysToSerialize = new HashSet<Type>(coreLabel.KeySetNotNull());
			CoreNLPProtos.Token.Builder builder = ToProtoBuilder(coreLabel, keysToSerialize);
			// Completeness check
			if (enforceLosslessSerialization && !keysToSerialize.IsEmpty())
			{
				throw new ProtobufAnnotationSerializer.LossySerializationException("Keys are not being serialized: " + StringUtils.Join(keysToSerialize));
			}
			return ((CoreNLPProtos.Token)builder.Build());
		}

		/// <summary>
		/// <p>
		/// The method to extend by subclasses of the Protobuf Annotator if custom additions are added to Tokens.
		/// </summary>
		/// <remarks>
		/// <p>
		/// The method to extend by subclasses of the Protobuf Annotator if custom additions are added to Tokens.
		/// In contrast to
		/// <see cref="ToProto(Edu.Stanford.Nlp.Ling.CoreLabel)"/>
		/// , this function
		/// returns a builder that can be extended.
		/// </p>
		/// </remarks>
		/// <param name="coreLabel">The sentence to save to a protocol buffer</param>
		/// <param name="keysToSerialize">
		/// A set tracking which keys have been saved. It's important to remove any keys added to the proto
		/// from this set, as the code tracks annotations to ensure lossless serialization
		/// </param>
		protected internal virtual CoreNLPProtos.Token.Builder ToProtoBuilder(CoreLabel coreLabel, ICollection<Type> keysToSerialize)
		{
			CoreNLPProtos.Token.Builder builder = CoreNLPProtos.Token.NewBuilder();
			ICollection<Type> keySet = coreLabel.KeySetNotNull();
			// Remove items serialized elsewhere from the required list
			keysToSerialize.Remove(typeof(CoreAnnotations.TextAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.SentenceIndexAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.DocIDAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.IndexAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.ParagraphAnnotation));
			// Remove items populated by number normalizer
			keysToSerialize.Remove(typeof(CoreAnnotations.NumericCompositeObjectAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.NumericCompositeTypeAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.NumericCompositeValueAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.NumericTypeAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.NumericValueAnnotation));
			// Remove items which were never supposed to be there in the first place
			keysToSerialize.Remove(typeof(CoreAnnotations.ForcedSentenceUntilEndAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.ForcedSentenceEndAnnotation));
			keysToSerialize.Remove(typeof(TreeCoreAnnotations.HeadWordLabelAnnotation));
			keysToSerialize.Remove(typeof(TreeCoreAnnotations.HeadTagLabelAnnotation));
			// Remove section info
			keysToSerialize.Remove(typeof(CoreAnnotations.SectionStartAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.SectionEndAnnotation));
			// Set the word (this may be null if the CoreLabel is storing a character (as in case of segmenter)
			if (coreLabel.Word() != null)
			{
				builder.SetWord(coreLabel.Word());
			}
			// Optional fields
			if (keySet.Contains(typeof(CoreAnnotations.PartOfSpeechAnnotation)))
			{
				builder.SetPos(coreLabel.Tag());
				keysToSerialize.Remove(typeof(CoreAnnotations.PartOfSpeechAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.ValueAnnotation)))
			{
				builder.SetValue(coreLabel.Value());
				keysToSerialize.Remove(typeof(CoreAnnotations.ValueAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CategoryAnnotation)))
			{
				builder.SetCategory(coreLabel.Category());
				keysToSerialize.Remove(typeof(CoreAnnotations.CategoryAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.BeforeAnnotation)))
			{
				builder.SetBefore(coreLabel.Before());
				keysToSerialize.Remove(typeof(CoreAnnotations.BeforeAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.AfterAnnotation)))
			{
				builder.SetAfter(coreLabel.After());
				keysToSerialize.Remove(typeof(CoreAnnotations.AfterAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.OriginalTextAnnotation)))
			{
				builder.SetOriginalText(coreLabel.OriginalText());
				keysToSerialize.Remove(typeof(CoreAnnotations.OriginalTextAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.NamedEntityTagAnnotation)))
			{
				builder.SetNer(coreLabel.Ner());
				keysToSerialize.Remove(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation)))
			{
				builder.SetCoarseNER(coreLabel.Get(typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation)));
				keysToSerialize.Remove(typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation)))
			{
				builder.SetFineGrainedNER(coreLabel.Get(typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation)));
				keysToSerialize.Remove(typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
			{
				builder.SetBeginChar(coreLabel.BeginPosition());
				keysToSerialize.Remove(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
			{
				builder.SetEndChar(coreLabel.EndPosition());
				keysToSerialize.Remove(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.LemmaAnnotation)))
			{
				builder.SetLemma(coreLabel.Lemma());
				keysToSerialize.Remove(typeof(CoreAnnotations.LemmaAnnotation));
			}
			if (keySet.Contains(typeof(CoreAnnotations.UtteranceAnnotation)))
			{
				builder.SetUtterance(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.UtteranceAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.SpeakerAnnotation)))
			{
				builder.SetSpeaker(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.SpeakerAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.BeginIndexAnnotation)))
			{
				builder.SetBeginIndex(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.BeginIndexAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.EndIndexAnnotation)))
			{
				builder.SetEndIndex(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.EndIndexAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.TokenBeginAnnotation)))
			{
				builder.SetTokenBeginIndex(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.TokenBeginAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.TokenEndAnnotation)))
			{
				builder.SetTokenEndIndex(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.TokenEndAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)))
			{
				builder.SetNormalizedNER(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)));
			}
			if (keySet.Contains(typeof(TimeAnnotations.TimexAnnotation)))
			{
				builder.SetTimexValue(ToProto(GetAndRegister(coreLabel, keysToSerialize, typeof(TimeAnnotations.TimexAnnotation))));
			}
			if (keySet.Contains(typeof(CoreAnnotations.AnswerAnnotation)))
			{
				builder.SetAnswer(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.AnswerAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.WikipediaEntityAnnotation)))
			{
				builder.SetWikipediaEntity(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.WikipediaEntityAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.IsNewlineAnnotation)))
			{
				builder.SetIsNewline(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.IsNewlineAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.XmlContextAnnotation)))
			{
				builder.SetHasXmlContext(true);
				builder.AddAllXmlContext(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.XmlContextAnnotation)));
			}
			else
			{
				builder.SetHasXmlContext(false);
			}
			// if there is section info for this token, store it
			if (keySet.Contains(typeof(CoreAnnotations.SectionStartAnnotation)))
			{
				ICoreMap sectionAnnotations = coreLabel.Get(typeof(CoreAnnotations.SectionStartAnnotation));
				// if there is a section name annotation, store it
				if (sectionAnnotations.Get(typeof(CoreAnnotations.SectionAnnotation)) != null)
				{
					builder.SetSectionName(sectionAnnotations.Get(typeof(CoreAnnotations.SectionAnnotation)));
				}
				// if there is a section author annotation, store it
				if (sectionAnnotations.Get(typeof(CoreAnnotations.AuthorAnnotation)) != null)
				{
					builder.SetSectionAuthor(sectionAnnotations.Get(typeof(CoreAnnotations.AuthorAnnotation)));
				}
				// if there is a section date annotation, store it
				if (sectionAnnotations.Get(typeof(CoreAnnotations.SectionDateAnnotation)) != null)
				{
					builder.SetSectionAuthor(sectionAnnotations.Get(typeof(CoreAnnotations.SectionDateAnnotation)));
				}
			}
			// store section end label
			if (keySet.Contains(typeof(CoreAnnotations.SectionEndAnnotation)))
			{
				builder.SetSectionEndLabel(coreLabel.Get(typeof(CoreAnnotations.SectionEndAnnotation)));
			}
			if (keySet.Contains(typeof(CorefCoreAnnotations.CorefClusterIdAnnotation)))
			{
				builder.SetCorefClusterID(GetAndRegister(coreLabel, keysToSerialize, typeof(CorefCoreAnnotations.CorefClusterIdAnnotation)));
			}
			if (keySet.Contains(typeof(NaturalLogicAnnotations.OperatorAnnotation)))
			{
				builder.SetOperator(ToProto(GetAndRegister(coreLabel, keysToSerialize, typeof(NaturalLogicAnnotations.OperatorAnnotation))));
			}
			if (keySet.Contains(typeof(NaturalLogicAnnotations.PolarityAnnotation)))
			{
				builder.SetPolarity(ToProto(GetAndRegister(coreLabel, keysToSerialize, typeof(NaturalLogicAnnotations.PolarityAnnotation))));
			}
			if (keySet.Contains(typeof(NaturalLogicAnnotations.PolarityDirectionAnnotation)))
			{
				builder.SetPolarityDir(GetAndRegister(coreLabel, keysToSerialize, typeof(NaturalLogicAnnotations.PolarityDirectionAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.SpanAnnotation)))
			{
				IntPair span = GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.SpanAnnotation));
				builder.SetSpan(((CoreNLPProtos.Span)CoreNLPProtos.Span.NewBuilder().SetBegin(span.GetSource()).SetEnd(span.GetTarget()).Build()));
			}
			if (keySet.Contains(typeof(SentimentCoreAnnotations.SentimentClass)))
			{
				builder.SetSentiment(GetAndRegister(coreLabel, keysToSerialize, typeof(SentimentCoreAnnotations.SentimentClass)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.QuotationIndexAnnotation)))
			{
				builder.SetQuotationIndex(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.QuotationIndexAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CoNLLUFeats)))
			{
				builder.SetConllUFeatures(ToMapStringStringProto(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.CoNLLUFeats))));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation)))
			{
				IntPair span = GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation));
				builder.SetConllUTokenSpan(((CoreNLPProtos.Span)CoreNLPProtos.Span.NewBuilder().SetBegin(span.GetSource()).SetEnd(span.GetTarget()).Build()));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CoNLLUMisc)))
			{
				builder.SetConllUMisc(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.CoNLLUMisc)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CoarseTagAnnotation)))
			{
				builder.SetCoarseTag(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.CoarseTagAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CoNLLUSecondaryDepsAnnotation)))
			{
				builder.SetConllUSecondaryDeps(ToMapStringStringProto(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.CoNLLUSecondaryDepsAnnotation))));
			}
			// Non-default annotators
			if (keySet.Contains(typeof(CoreAnnotations.GenderAnnotation)))
			{
				builder.SetGender(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.GenderAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.TrueCaseAnnotation)))
			{
				builder.SetTrueCase(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.TrueCaseAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.TrueCaseTextAnnotation)))
			{
				builder.SetTrueCaseText(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.TrueCaseTextAnnotation)));
			}
			// Chinese character related stuff
			if (keySet.Contains(typeof(CoreAnnotations.ChineseCharAnnotation)))
			{
				builder.SetChineseChar(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.ChineseCharAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.ChineseSegAnnotation)))
			{
				builder.SetChineseSeg(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.ChineseSegAnnotation)));
			}
			if (keySet.Contains(typeof(SegmenterCoreAnnotations.XMLCharAnnotation)))
			{
				builder.SetChineseXMLChar(GetAndRegister(coreLabel, keysToSerialize, typeof(SegmenterCoreAnnotations.XMLCharAnnotation)));
			}
			// French tokens potentially have ParentAnnotation
			if (keySet.Contains(typeof(CoreAnnotations.ParentAnnotation)))
			{
				builder.SetParent(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.ParentAnnotation)));
			}
			// indexes into document wide mention lists
			if (keySet.Contains(typeof(CoreAnnotations.EntityMentionIndexAnnotation)))
			{
				builder.SetEntityMentionIndex(GetAndRegister(coreLabel, keysToSerialize, typeof(CoreAnnotations.EntityMentionIndexAnnotation)));
			}
			// coref mentions that contain this token
			if (keySet.Contains(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation)))
			{
				foreach (int corefMentionIndex in coreLabel.Get(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation)))
				{
					builder.AddCorefMentionIndex(corefMentionIndex);
				}
				keysToSerialize.Remove(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation));
			}
			// Return
			return builder;
		}

		/// <summary>Create a protobuf builder, rather than a compiled protobuf.</summary>
		/// <remarks>
		/// Create a protobuf builder, rather than a compiled protobuf.
		/// Useful for, e.g., the simple CoreNLP interface.
		/// </remarks>
		/// <param name="sentence">The sentence to serialize.</param>
		/// <returns>A Sentence builder.</returns>
		public virtual CoreNLPProtos.Sentence.Builder ToProtoBuilder(ICoreMap sentence)
		{
			return ToProtoBuilder(sentence, Java.Util.Collections.EmptySet());
		}

		/// <summary>Create a Sentence proto from a CoreMap instance.</summary>
		/// <remarks>
		/// Create a Sentence proto from a CoreMap instance.
		/// This is not static, as it optionally throws an exception if the serialization is lossy.
		/// </remarks>
		/// <param name="sentence">
		/// The CoreMap to convert. Note that it should not be a CoreLabel or an Annotation,
		/// and should represent a sentence.
		/// </param>
		/// <returns>A protocol buffer message corresponding to this sentence</returns>
		/// <exception cref="System.ArgumentException">If the sentence is not a valid sentence (e.g., is a document or a word).</exception>
		public virtual CoreNLPProtos.Sentence ToProto(ICoreMap sentence)
		{
			ICollection<Type> keysToSerialize = new HashSet<Type>(sentence.KeySet());
			CoreNLPProtos.Sentence.Builder builder = ToProtoBuilder(sentence, keysToSerialize);
			// Completeness check
			if (enforceLosslessSerialization && !keysToSerialize.IsEmpty())
			{
				throw new ProtobufAnnotationSerializer.LossySerializationException("Keys are not being serialized: " + StringUtils.Join(keysToSerialize));
			}
			return ((CoreNLPProtos.Sentence)builder.Build());
		}

		/// <summary>
		/// <p>
		/// The method to extend by subclasses of the Protobuf Annotator if custom additions are added to Tokens.
		/// </summary>
		/// <remarks>
		/// <p>
		/// The method to extend by subclasses of the Protobuf Annotator if custom additions are added to Tokens.
		/// In contrast to
		/// <see cref="ToProto(Edu.Stanford.Nlp.Ling.CoreLabel)"/>
		/// , this function
		/// returns a builder that can be extended.
		/// </p>
		/// </remarks>
		/// <param name="sentence">The sentence to save to a protocol buffer</param>
		/// <param name="keysToSerialize">
		/// A set tracking which keys have been saved. It's important to remove any keys added to the proto
		/// from this set, as the code tracks annotations to ensure lossless serialization.
		/// </param>
		protected internal virtual CoreNLPProtos.Sentence.Builder ToProtoBuilder(ICoreMap sentence, ICollection<Type> keysToSerialize)
		{
			// Error checks
			if (sentence is CoreLabel)
			{
				throw new ArgumentException("CoreMap is actually a CoreLabel");
			}
			CoreNLPProtos.Sentence.Builder builder = CoreNLPProtos.Sentence.NewBuilder();
			// Remove items serialized elsewhere from the required list
			keysToSerialize.Remove(typeof(CoreAnnotations.TextAnnotation));
			keysToSerialize.Remove(typeof(CoreAnnotations.NumerizedTokensAnnotation));
			// Required fields
			builder.SetTokenOffsetBegin(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.TokenBeginAnnotation)));
			builder.SetTokenOffsetEnd(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.TokenEndAnnotation)));
			// Get key set of CoreMap
			ICollection<Type> keySet;
			if (sentence is ArrayCoreMap)
			{
				keySet = ((ArrayCoreMap)sentence).KeySetNotNull();
			}
			else
			{
				keySet = new IdentityHashSet<Type>(sentence.KeySet());
			}
			// Tokens
			if (sentence.ContainsKey(typeof(CoreAnnotations.TokensAnnotation)))
			{
				foreach (CoreLabel tok in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					builder.AddToken(ToProto(tok));
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.TokensAnnotation));
			}
			// Characters
			if (sentence.ContainsKey(typeof(SegmenterCoreAnnotations.CharactersAnnotation)))
			{
				foreach (CoreLabel c in sentence.Get(typeof(SegmenterCoreAnnotations.CharactersAnnotation)))
				{
					builder.AddCharacter(ToProto(c));
				}
				keysToSerialize.Remove(typeof(SegmenterCoreAnnotations.CharactersAnnotation));
			}
			// Optional fields
			if (keySet.Contains(typeof(CoreAnnotations.SentenceIndexAnnotation)))
			{
				builder.SetSentenceIndex(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.SentenceIndexAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.LineNumberAnnotation)))
			{
				builder.SetLineNumber(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.LineNumberAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
			{
				builder.SetCharacterOffsetBegin(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
			{
				builder.SetCharacterOffsetEnd(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			}
			if (keySet.Contains(typeof(TreeCoreAnnotations.TreeAnnotation)))
			{
				builder.SetParseTree(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(TreeCoreAnnotations.TreeAnnotation))));
			}
			if (keySet.Contains(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation)))
			{
				builder.SetBinarizedParseTree(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(TreeCoreAnnotations.BinarizedTreeAnnotation))));
			}
			if (keySet.Contains(typeof(TreeCoreAnnotations.KBestTreesAnnotation)))
			{
				foreach (Tree tree in sentence.Get(typeof(TreeCoreAnnotations.KBestTreesAnnotation)))
				{
					builder.AddKBestParseTrees(ToProto(tree));
					keysToSerialize.Remove(typeof(TreeCoreAnnotations.KBestTreesAnnotation));
				}
			}
			if (keySet.Contains(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree)))
			{
				builder.SetAnnotatedParseTree(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SentimentCoreAnnotations.SentimentAnnotatedTree))));
			}
			if (keySet.Contains(typeof(SentimentCoreAnnotations.SentimentClass)))
			{
				builder.SetSentiment(GetAndRegister(sentence, keysToSerialize, typeof(SentimentCoreAnnotations.SentimentClass)));
			}
			if (keySet.Contains(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)))
			{
				builder.SetBasicDependencies(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation))));
			}
			if (keySet.Contains(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation)))
			{
				builder.SetCollapsedDependencies(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation))));
			}
			if (keySet.Contains(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation)))
			{
				builder.SetCollapsedCCProcessedDependencies(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation))));
			}
			if (keySet.Contains(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation)))
			{
				builder.SetAlternativeDependencies(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation))));
			}
			if (keySet.Contains(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)))
			{
				builder.SetEnhancedDependencies(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation))));
			}
			if (keySet.Contains(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)))
			{
				builder.SetEnhancedPlusPlusDependencies(ToProto(GetAndRegister(sentence, keysToSerialize, typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))));
			}
			if (keySet.Contains(typeof(CoreAnnotations.TokensAnnotation)) && GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.TokensAnnotation)).Count > 0 && GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.TokensAnnotation
				))[0].ContainsKey(typeof(CoreAnnotations.ParagraphAnnotation)))
			{
				builder.SetParagraph(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.TokensAnnotation))[0].Get(typeof(CoreAnnotations.ParagraphAnnotation)));
			}
			if (keySet.Contains(typeof(CoreAnnotations.NumerizedTokensAnnotation)))
			{
				builder.SetHasNumerizedTokensAnnotation(true);
			}
			else
			{
				builder.SetHasNumerizedTokensAnnotation(false);
			}
			if (keySet.Contains(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation)))
			{
				foreach (SentenceFragment entailedSentence in GetAndRegister(sentence, keysToSerialize, typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation)))
				{
					builder.AddEntailedSentence(ToProto(entailedSentence));
				}
			}
			if (keySet.Contains(typeof(NaturalLogicAnnotations.EntailedClausesAnnotation)))
			{
				foreach (SentenceFragment entailedClause in GetAndRegister(sentence, keysToSerialize, typeof(NaturalLogicAnnotations.EntailedClausesAnnotation)))
				{
					builder.AddEntailedClause(ToProto(entailedClause));
				}
			}
			if (keySet.Contains(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation)))
			{
				builder.SetHasOpenieTriplesAnnotation(true);
				foreach (RelationTriple triple in GetAndRegister(sentence, keysToSerialize, typeof(NaturalLogicAnnotations.RelationTriplesAnnotation)))
				{
					builder.AddOpenieTriple(ToProto(triple));
				}
			}
			if (keySet.Contains(typeof(CoreAnnotations.KBPTriplesAnnotation)))
			{
				// mark that this sentence has kbp triples, potentially empty list
				builder.SetHasKBPTriplesAnnotation(true);
				// store each of the kbp triples
				foreach (RelationTriple triple in GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.KBPTriplesAnnotation)))
				{
					builder.AddKbpTriple(ToProto(triple));
				}
			}
			// Non-default annotators
			if (keySet.Contains(typeof(MachineReadingAnnotations.EntityMentionsAnnotation)))
			{
				builder.SetHasRelationAnnotations(true);
				foreach (EntityMention entity in GetAndRegister(sentence, keysToSerialize, typeof(MachineReadingAnnotations.EntityMentionsAnnotation)))
				{
					builder.AddEntity(ToProto(entity));
				}
			}
			else
			{
				builder.SetHasRelationAnnotations(false);
			}
			if (keySet.Contains(typeof(MachineReadingAnnotations.RelationMentionsAnnotation)))
			{
				if (!builder.GetHasRelationAnnotations())
				{
					throw new InvalidOperationException("Registered entity mentions without relation mentions");
				}
				foreach (RelationMention relation in GetAndRegister(sentence, keysToSerialize, typeof(MachineReadingAnnotations.RelationMentionsAnnotation)))
				{
					builder.AddRelation(ToProto(relation));
				}
			}
			// add each of the mentions in the List<Mentions> for this sentence
			if (keySet.Contains(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)))
			{
				builder.SetHasCorefMentionsAnnotation(true);
				foreach (Mention m in sentence.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)))
				{
					builder.AddMentionsForCoref(ToProto(m));
				}
				keysToSerialize.Remove(typeof(CorefCoreAnnotations.CorefMentionsAnnotation));
			}
			// Entity mentions
			if (keySet.Contains(typeof(CoreAnnotations.MentionsAnnotation)))
			{
				foreach (ICoreMap mention in sentence.Get(typeof(CoreAnnotations.MentionsAnnotation)))
				{
					builder.AddMentions(ToProtoMention(mention));
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.MentionsAnnotation));
				builder.SetHasEntityMentionsAnnotation(true);
			}
			else
			{
				builder.SetHasEntityMentionsAnnotation(false);
			}
			// add a sentence id if it exists
			if (keySet.Contains(typeof(CoreAnnotations.SentenceIDAnnotation)))
			{
				builder.SetSentenceID(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.SentenceIDAnnotation)));
			}
			// add section index
			if (keySet.Contains(typeof(CoreAnnotations.SectionIndexAnnotation)))
			{
				builder.SetSectionIndex(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.SectionIndexAnnotation)));
			}
			// add section date
			if (keySet.Contains(typeof(CoreAnnotations.SectionDateAnnotation)))
			{
				builder.SetSectionDate(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.SectionDateAnnotation)));
			}
			// add section name
			if (keySet.Contains(typeof(CoreAnnotations.SectionAnnotation)))
			{
				builder.SetSectionName(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.SectionAnnotation)));
			}
			// add section author
			if (keySet.Contains(typeof(CoreAnnotations.AuthorAnnotation)))
			{
				builder.SetSectionAuthor(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.AuthorAnnotation)));
			}
			// add doc id
			if (keySet.Contains(typeof(CoreAnnotations.DocIDAnnotation)))
			{
				builder.SetDocID(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.DocIDAnnotation)));
			}
			// add boolean flag if sentence is quoted
			if (keySet.Contains(typeof(CoreAnnotations.QuotedAnnotation)))
			{
				builder.SetSectionQuoted(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.QuotedAnnotation)));
			}
			// add chapter index if there is one
			if (keySet.Contains(typeof(ChapterAnnotator.ChapterAnnotation)))
			{
				builder.SetChapterIndex(GetAndRegister(sentence, keysToSerialize, typeof(ChapterAnnotator.ChapterAnnotation)));
			}
			// add paragraph index info
			if (keySet.Contains(typeof(CoreAnnotations.ParagraphIndexAnnotation)))
			{
				builder.SetParagraphIndex(GetAndRegister(sentence, keysToSerialize, typeof(CoreAnnotations.ParagraphIndexAnnotation)));
			}
			// Return
			return builder;
		}

		/// <summary>Create a Document proto from a CoreMap instance.</summary>
		/// <remarks>
		/// Create a Document proto from a CoreMap instance.
		/// This is not static, as it optionally throws an exception if the serialization is lossy.
		/// </remarks>
		/// <param name="doc">The Annotation to convert.</param>
		/// <returns>A protocol buffer message corresponding to this document</returns>
		public virtual CoreNLPProtos.Document ToProto(Annotation doc)
		{
			ICollection<Type> keysToSerialize = new HashSet<Type>(doc.KeySet());
			keysToSerialize.Remove(typeof(CoreAnnotations.TokensAnnotation));
			// note(gabor): tokens are saved in the sentence
			CoreNLPProtos.Document.Builder builder = ToProtoBuilder(doc, keysToSerialize);
			// Completeness Check
			if (enforceLosslessSerialization && !keysToSerialize.IsEmpty())
			{
				throw new ProtobufAnnotationSerializer.LossySerializationException("Keys are not being serialized: " + StringUtils.Join(keysToSerialize));
			}
			return ((CoreNLPProtos.Document)builder.Build());
		}

		/// <summary>Create a protobuf builder, rather than a compiled protobuf.</summary>
		/// <remarks>
		/// Create a protobuf builder, rather than a compiled protobuf.
		/// Useful for, e.g., the simple CoreNLP interface.
		/// </remarks>
		/// <param name="doc">The document to serialize.</param>
		/// <returns>A Document builder.</returns>
		public virtual CoreNLPProtos.Document.Builder ToProtoBuilder(Annotation doc)
		{
			return ToProtoBuilder(doc, Java.Util.Collections.EmptySet());
		}

		/// <summary>
		/// <p>
		/// The method to extend by subclasses of the Protobuf Annotator if custom additions are added to Tokens.
		/// </summary>
		/// <remarks>
		/// <p>
		/// The method to extend by subclasses of the Protobuf Annotator if custom additions are added to Tokens.
		/// In contrast to
		/// <see cref="ToProto(Edu.Stanford.Nlp.Ling.CoreLabel)"/>
		/// , this function
		/// returns a builder that can be extended.
		/// </p>
		/// </remarks>
		/// <param name="doc">The sentence to save to a protocol buffer</param>
		/// <param name="keysToSerialize">
		/// A set tracking which keys have been saved. It's important to remove any keys added to the proto
		/// from this set, as the code tracks annotations to ensure lossless serializationA set tracking which keys have been saved. It's important to remove any keys added to the proto
		/// from this set, as the code tracks annotations to ensure lossless serialization.
		/// </param>
		protected internal virtual CoreNLPProtos.Document.Builder ToProtoBuilder(Annotation doc, ICollection<Type> keysToSerialize)
		{
			CoreNLPProtos.Document.Builder builder = CoreNLPProtos.Document.NewBuilder();
			// Required fields
			builder.SetText(doc.Get(typeof(CoreAnnotations.TextAnnotation)));
			keysToSerialize.Remove(typeof(CoreAnnotations.TextAnnotation));
			// Check if we need to store xml info
			if (doc.ContainsKey(typeof(CoreAnnotations.SectionsAnnotation)))
			{
				builder.SetXmlDoc(true);
			}
			else
			{
				builder.SetXmlDoc(false);
			}
			// Optional fields
			if (doc.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				foreach (ICoreMap sentence in doc.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					builder.AddSentence(ToProto(sentence));
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.SentencesAnnotation));
			}
			else
			{
				if (doc.ContainsKey(typeof(CoreAnnotations.TokensAnnotation)))
				{
					foreach (CoreLabel token in doc.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						builder.AddSentencelessToken(ToProto(token));
					}
				}
			}
			if (doc.ContainsKey(typeof(CoreAnnotations.DocIDAnnotation)))
			{
				builder.SetDocID(doc.Get(typeof(CoreAnnotations.DocIDAnnotation)));
				keysToSerialize.Remove(typeof(CoreAnnotations.DocIDAnnotation));
			}
			if (doc.ContainsKey(typeof(CoreAnnotations.DocDateAnnotation)))
			{
				builder.SetDocDate(doc.Get(typeof(CoreAnnotations.DocDateAnnotation)));
				keysToSerialize.Remove(typeof(CoreAnnotations.DocDateAnnotation));
			}
			if (doc.ContainsKey(typeof(CoreAnnotations.CalendarAnnotation)))
			{
				builder.SetCalendar(doc.Get(typeof(CoreAnnotations.CalendarAnnotation)).ToInstant().ToEpochMilli());
				keysToSerialize.Remove(typeof(CoreAnnotations.CalendarAnnotation));
			}
			// add coref info
			if (doc.ContainsKey(typeof(CorefCoreAnnotations.CorefChainAnnotation)))
			{
				// mark that annotation has coref info
				builder.SetHasCorefAnnotation(true);
				foreach (KeyValuePair<int, CorefChain> chain in doc.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)))
				{
					builder.AddCorefChain(ToProto(chain.Value));
				}
				keysToSerialize.Remove(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			}
			else
			{
				builder.SetHasCorefAnnotation(false);
			}
			// add document level coref mentions info
			if (doc.ContainsKey(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)))
			{
				builder.SetHasCorefMentionAnnotation(true);
				foreach (Mention corefMention in doc.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)))
				{
					builder.AddMentionsForCoref(ToProto(corefMention));
				}
				keysToSerialize.Remove(typeof(CorefCoreAnnotations.CorefMentionsAnnotation));
			}
			else
			{
				builder.SetHasCorefMentionAnnotation(false);
			}
			// add quote information
			if (doc.ContainsKey(typeof(CoreAnnotations.QuotationsAnnotation)))
			{
				foreach (ICoreMap quote in doc.Get(typeof(CoreAnnotations.QuotationsAnnotation)))
				{
					builder.AddQuote(ToProtoQuote(quote));
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.QuotationsAnnotation));
			}
			// add document level entity mentions info
			if (doc.ContainsKey(typeof(CoreAnnotations.MentionsAnnotation)))
			{
				foreach (ICoreMap mention in doc.Get(typeof(CoreAnnotations.MentionsAnnotation)))
				{
					builder.AddMentions(ToProtoMention(mention));
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.MentionsAnnotation));
				builder.SetHasEntityMentionsAnnotation(true);
			}
			else
			{
				builder.SetHasEntityMentionsAnnotation(false);
			}
			// mappings between coref mentions and entity mentions
			if (doc.ContainsKey(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation)))
			{
				IDictionary<int, int> entityMentionToCorefMention = doc.Get(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation));
				int numEntityMentions = doc.Get(typeof(CoreAnnotations.MentionsAnnotation)).Count;
				for (int entityMentionIndex = 0; entityMentionIndex < numEntityMentions; entityMentionIndex++)
				{
					if (entityMentionToCorefMention.Keys.Contains(entityMentionIndex))
					{
						builder.AddEntityMentionToCorefMentionMappings(entityMentionToCorefMention[entityMentionIndex]);
					}
					else
					{
						// store a -1 if there is no coref mention corresponding to this entity mention
						builder.AddEntityMentionToCorefMentionMappings(-1);
					}
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation));
			}
			if (doc.ContainsKey(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation)))
			{
				IDictionary<int, int> corefMentionToEntityMention = doc.Get(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation));
				int numCorefMentions = doc.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation)).Count;
				for (int corefMentionIndex = 0; corefMentionIndex < numCorefMentions; corefMentionIndex++)
				{
					if (corefMentionToEntityMention.Keys.Contains(corefMentionIndex))
					{
						builder.AddCorefMentionToEntityMentionMappings(corefMentionToEntityMention[corefMentionIndex]);
					}
					else
					{
						// store a -1 if there is no coref mention corresponding to this entity mention
						builder.AddCorefMentionToEntityMentionMappings(-1);
					}
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation));
			}
			// add character info from segmenter
			if (doc.ContainsKey(typeof(SegmenterCoreAnnotations.CharactersAnnotation)))
			{
				foreach (CoreLabel c in doc.Get(typeof(SegmenterCoreAnnotations.CharactersAnnotation)))
				{
					builder.AddCharacter(ToProto(c));
				}
				keysToSerialize.Remove(typeof(SegmenterCoreAnnotations.CharactersAnnotation));
			}
			// add section info
			if (doc.ContainsKey(typeof(CoreAnnotations.SectionsAnnotation)))
			{
				foreach (ICoreMap section in doc.Get(typeof(CoreAnnotations.SectionsAnnotation)))
				{
					builder.AddSections(ToProtoSection(section));
				}
				keysToSerialize.Remove(typeof(CoreAnnotations.SectionsAnnotation));
			}
			// Return
			return builder;
		}

		/// <summary>Create a ParseTree proto from a Tree.</summary>
		/// <remarks>
		/// Create a ParseTree proto from a Tree. If the Tree is a scored tree, the scores will
		/// be preserved.
		/// </remarks>
		/// <param name="parseTree">The parse tree to convert.</param>
		/// <returns>A protocol buffer message corresponding to this tree.</returns>
		public virtual CoreNLPProtos.ParseTree ToProto(Tree parseTree)
		{
			CoreNLPProtos.ParseTree.Builder builder = CoreNLPProtos.ParseTree.NewBuilder();
			// Required fields
			foreach (Tree child in parseTree.Children())
			{
				builder.AddChild(ToProto(child));
			}
			// Optional fields
			IntPair span = parseTree.GetSpan();
			if (span != null)
			{
				builder.SetYieldBeginIndex(span.GetSource());
				builder.SetYieldEndIndex(span.GetTarget());
			}
			if (parseTree.Label() != null)
			{
				builder.SetValue(parseTree.Label().Value());
			}
			if (!double.IsNaN(parseTree.Score()))
			{
				builder.SetScore(parseTree.Score());
			}
			int sentiment;
			if (parseTree.Label() is ICoreMap && (sentiment = ((ICoreMap)parseTree.Label()).Get(typeof(RNNCoreAnnotations.PredictedClass))) != null)
			{
				builder.SetSentiment(CoreNLPProtos.Sentiment.ForNumber(sentiment));
			}
			// Return
			return ((CoreNLPProtos.ParseTree)builder.Build());
		}

		/// <summary>Create a compact representation of the semantic graph for this dependency parse.</summary>
		/// <param name="graph">The dependency graph to save.</param>
		/// <returns>A protocol buffer message corresponding to this parse.</returns>
		public static CoreNLPProtos.DependencyGraph ToProto(SemanticGraph graph)
		{
			CoreNLPProtos.DependencyGraph.Builder builder = CoreNLPProtos.DependencyGraph.NewBuilder();
			// Roots
			ICollection<int> rootSet = graph.GetRoots().Stream().Map(null).Collect(Collectors.ToCollection(null));
			// Nodes
			foreach (IndexedWord node in graph.VertexSet())
			{
				// Register node
				CoreNLPProtos.DependencyGraph.Node.Builder nodeBuilder = CoreNLPProtos.DependencyGraph.Node.NewBuilder().SetSentenceIndex(node.Get(typeof(CoreAnnotations.SentenceIndexAnnotation))).SetIndex(node.Index());
				if (node.CopyCount() > 0)
				{
					nodeBuilder.SetCopyAnnotation(node.CopyCount());
				}
				builder.AddNode(((CoreNLPProtos.DependencyGraph.Node)nodeBuilder.Build()));
				// Register root
				if (rootSet.Contains(node.Index()))
				{
					builder.AddRoot(node.Index());
				}
			}
			// Edges
			foreach (SemanticGraphEdge edge in graph.EdgeIterable())
			{
				// Set edge
				builder.AddEdge(CoreNLPProtos.DependencyGraph.Edge.NewBuilder().SetSource(edge.GetSource().Index()).SetTarget(edge.GetTarget().Index()).SetDep(edge.GetRelation().ToString()).SetIsExtra(edge.IsExtra()).SetSourceCopy(edge.GetSource().CopyCount
					()).SetTargetCopy(edge.GetTarget().CopyCount()).SetLanguage(ToProto(edge.GetRelation().GetLanguage())));
			}
			// Return
			return ((CoreNLPProtos.DependencyGraph)builder.Build());
		}

		/// <summary>Create a CorefChain protocol buffer from the given coref chain.</summary>
		/// <param name="chain">The coref chain to convert.</param>
		/// <returns>A protocol buffer message corresponding to this chain.</returns>
		public virtual CoreNLPProtos.CorefChain ToProto(CorefChain chain)
		{
			CoreNLPProtos.CorefChain.Builder builder = CoreNLPProtos.CorefChain.NewBuilder();
			// Set ID
			builder.SetChainID(chain.GetChainID());
			// Set mentions
			IDictionary<CorefChain.CorefMention, int> mentionToIndex = new IdentityHashMap<CorefChain.CorefMention, int>();
			foreach (KeyValuePair<IntPair, ICollection<CorefChain.CorefMention>> entry in chain.GetMentionMap())
			{
				foreach (CorefChain.CorefMention mention in entry.Value)
				{
					mentionToIndex[mention] = mentionToIndex.Count;
					builder.AddMention(CoreNLPProtos.CorefChain.CorefMention.NewBuilder().SetMentionID(mention.mentionID).SetMentionType(mention.mentionType.ToString()).SetNumber(mention.number.ToString()).SetGender(mention.gender.ToString()).SetAnimacy(mention
						.animacy.ToString()).SetBeginIndex(mention.startIndex - 1).SetEndIndex(mention.endIndex - 1).SetHeadIndex(mention.headIndex - 1).SetSentenceIndex(mention.sentNum - 1).SetPosition(mention.position.Get(1)));
				}
			}
			// Set representative mention
			builder.SetRepresentative(mentionToIndex[chain.GetRepresentativeMention()]);
			// Return
			return ((CoreNLPProtos.CorefChain)builder.Build());
		}

		/// <summary>Create a Section CoreMap protocol buffer from the given Section CoreMap</summary>
		/// <param name="section">The CoreMap representing the section to serialize to a proto.</param>
		/// <returns>The protocol buffer version of the section</returns>
		public virtual CoreNLPProtos.Section ToProtoSection(ICoreMap section)
		{
			CoreNLPProtos.Section.Builder builder = CoreNLPProtos.Section.NewBuilder();
			// Set char start
			builder.SetCharBegin(section.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
			// Set char end
			builder.SetCharEnd(section.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			// Set author
			if (section.Get(typeof(CoreAnnotations.AuthorAnnotation)) != null)
			{
				builder.SetAuthor(section.Get(typeof(CoreAnnotations.AuthorAnnotation)));
			}
			// Set date time
			if (section.Get(typeof(CoreAnnotations.SectionDateAnnotation)) != null)
			{
				builder.SetDatetime(section.Get(typeof(CoreAnnotations.SectionDateAnnotation)));
			}
			// add the sentence indexes for the sentences in this section
			foreach (ICoreMap sentence in section.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				int sentenceIndex = sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
				builder.AddSentenceIndexes(sentenceIndex);
			}
			// add the quotes
			foreach (ICoreMap quote in section.Get(typeof(CoreAnnotations.QuotesAnnotation)))
			{
				builder.AddQuotes(ToProtoQuote(quote));
			}
			// add author start character offset if present
			if (section.Get(typeof(CoreAnnotations.SectionAuthorCharacterOffsetBeginAnnotation)) != null)
			{
				builder.SetAuthorCharBegin(section.Get(typeof(CoreAnnotations.SectionAuthorCharacterOffsetBeginAnnotation)));
			}
			// add author end character offset if present
			if (section.Get(typeof(CoreAnnotations.SectionAuthorCharacterOffsetEndAnnotation)) != null)
			{
				builder.SetAuthorCharEnd(section.Get(typeof(CoreAnnotations.SectionAuthorCharacterOffsetEndAnnotation)));
			}
			// add original xml tag with all info for section
			builder.SetXmlTag(ToProto(section.Get(typeof(CoreAnnotations.SectionTagAnnotation))));
			return ((CoreNLPProtos.Section)builder.Build());
		}

		public virtual CoreNLPProtos.IndexedWord CreateIndexedWordProtoFromIW(IndexedWord iw)
		{
			CoreNLPProtos.IndexedWord.Builder builder = CoreNLPProtos.IndexedWord.NewBuilder();
			if (iw == null)
			{
				builder.SetSentenceNum(-1);
				builder.SetTokenIndex(-1);
			}
			else
			{
				builder.SetSentenceNum(iw.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)) - 1);
				builder.SetTokenIndex(iw.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1);
				builder.SetCopyCount(iw.CopyCount());
			}
			return ((CoreNLPProtos.IndexedWord)builder.Build());
		}

		public virtual CoreNLPProtos.IndexedWord CreateIndexedWordProtoFromCL(CoreLabel cl)
		{
			CoreNLPProtos.IndexedWord.Builder builder = CoreNLPProtos.IndexedWord.NewBuilder();
			if (cl == null)
			{
				builder.SetSentenceNum(-1);
				builder.SetTokenIndex(-1);
			}
			else
			{
				builder.SetSentenceNum(cl.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)) - 1);
				builder.SetTokenIndex(cl.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1);
			}
			return ((CoreNLPProtos.IndexedWord)builder.Build());
		}

		public virtual CoreNLPProtos.Mention ToProto(Mention mention)
		{
			// create the builder
			CoreNLPProtos.Mention.Builder builder = CoreNLPProtos.Mention.NewBuilder();
			// set enums
			if (mention.mentionType != null)
			{
				builder.SetMentionType(mention.mentionType.ToString());
			}
			if (mention.gender != null)
			{
				builder.SetGender(mention.gender.ToString());
			}
			if (mention.number != null)
			{
				builder.SetNumber(mention.number.ToString());
			}
			if (mention.animacy != null)
			{
				builder.SetAnimacy(mention.animacy.ToString());
			}
			if (mention.person != null)
			{
				builder.SetPerson(mention.person.ToString());
			}
			if (mention.headString != null)
			{
				builder.SetHeadString(mention.headString);
			}
			if (mention.nerString != null)
			{
				builder.SetNerString(mention.nerString);
			}
			builder.SetStartIndex(mention.startIndex);
			builder.SetEndIndex(mention.endIndex);
			builder.SetHeadIndex(mention.headIndex);
			builder.SetMentionID(mention.mentionID);
			builder.SetOriginalRef(mention.originalRef);
			builder.SetGoldCorefClusterID(mention.goldCorefClusterID);
			builder.SetCorefClusterID(mention.corefClusterID);
			builder.SetMentionNum(mention.mentionNum);
			builder.SetSentNum(mention.sentNum);
			builder.SetUtter(mention.utter);
			builder.SetParagraph(mention.paragraph);
			builder.SetIsSubject(mention.isSubject);
			builder.SetIsDirectObject(mention.isDirectObject);
			builder.SetIsIndirectObject(mention.isIndirectObject);
			builder.SetIsPrepositionObject(mention.isPrepositionObject);
			builder.SetHasTwin(mention.hasTwin);
			builder.SetGeneric(mention.generic);
			builder.SetIsSingleton(mention.isSingleton);
			// handle the two sets of Strings
			if (mention.dependents != null)
			{
				mention.dependents.ForEach(null);
			}
			if (mention.preprocessedTerms != null)
			{
				mention.preprocessedTerms.ForEach(null);
			}
			// set IndexedWords by storing (sentence number, token index) pairs
			builder.SetDependingVerb(CreateIndexedWordProtoFromIW(mention.dependingVerb));
			builder.SetHeadIndexedWord(CreateIndexedWordProtoFromIW(mention.headIndexedWord));
			builder.SetHeadWord(CreateIndexedWordProtoFromCL(mention.headWord));
			//CoreLabel headWord = (mention.headWord != null) ? mention.headWord : null;
			//builder.setHeadWord(createCoreLabelPositionProto(mention.headWord));
			// add positions for each CoreLabel in sentence
			if (mention.sentenceWords != null)
			{
				foreach (CoreLabel cl in mention.sentenceWords)
				{
					builder.AddSentenceWords(CreateIndexedWordProtoFromCL(cl));
				}
			}
			if (mention.originalSpan != null)
			{
				foreach (CoreLabel cl in mention.originalSpan)
				{
					builder.AddOriginalSpan(CreateIndexedWordProtoFromCL(cl));
				}
			}
			// flag if this Mention should get basicDependency, collapsedDependency, and contextParseTree or not
			builder.SetHasBasicDependency((mention.basicDependency != null));
			builder.SetHasEnhancedDepenedncy((mention.enhancedDependency != null));
			builder.SetHasContextParseTree((mention.contextParseTree != null));
			// handle the sets of Mentions, just store mentionID
			if (mention.appositions != null)
			{
				foreach (Mention m in mention.appositions)
				{
					builder.AddAppositions(m.mentionID);
				}
			}
			if (mention.predicateNominatives != null)
			{
				foreach (Mention m in mention.predicateNominatives)
				{
					builder.AddPredicateNominatives(m.mentionID);
				}
			}
			if (mention.relativePronouns != null)
			{
				foreach (Mention m in mention.relativePronouns)
				{
					builder.AddRelativePronouns(m.mentionID);
				}
			}
			if (mention.listMembers != null)
			{
				foreach (Mention m in mention.listMembers)
				{
					builder.AddListMembers(m.mentionID);
				}
			}
			if (mention.belongToLists != null)
			{
				foreach (Mention m in mention.belongToLists)
				{
					builder.AddBelongToLists(m.mentionID);
				}
			}
			if (mention.speakerInfo != null)
			{
				builder.SetSpeakerInfo(ToProto(mention.speakerInfo));
			}
			return ((CoreNLPProtos.Mention)builder.Build());
		}

		public virtual CoreNLPProtos.SpeakerInfo ToProto(SpeakerInfo speakerInfo)
		{
			CoreNLPProtos.SpeakerInfo.Builder builder = CoreNLPProtos.SpeakerInfo.NewBuilder();
			builder.SetSpeakerName(speakerInfo.GetSpeakerName());
			// mentionID's should be set by MentionAnnotator
			foreach (Mention m in speakerInfo.GetMentions())
			{
				builder.AddMentions(m.mentionID);
			}
			return ((CoreNLPProtos.SpeakerInfo)builder.Build());
		}

		/// <summary>Convert the given Timex object to a protocol buffer.</summary>
		/// <param name="timex">The timex to convert.</param>
		/// <returns>A protocol buffer corresponding to this Timex object.</returns>
		public virtual CoreNLPProtos.Timex ToProto(Timex timex)
		{
			CoreNLPProtos.Timex.Builder builder = CoreNLPProtos.Timex.NewBuilder();
			if (timex.Value() != null)
			{
				builder.SetValue(timex.Value());
			}
			if (timex.AltVal() != null)
			{
				builder.SetAltValue(timex.AltVal());
			}
			if (timex.Text() != null)
			{
				builder.SetText(timex.Text());
			}
			if (timex.TimexType() != null)
			{
				builder.SetType(timex.TimexType());
			}
			if (timex.Tid() != null)
			{
				builder.SetTid(timex.Tid());
			}
			if (timex.BeginPoint() >= 0)
			{
				builder.SetBeginPoint(timex.BeginPoint());
			}
			if (timex.EndPoint() >= 0)
			{
				builder.SetEndPoint(timex.EndPoint());
			}
			return ((CoreNLPProtos.Timex)builder.Build());
		}

		/// <summary>Serialize the given entity mention to the corresponding protocol buffer.</summary>
		/// <param name="ent">The entity mention to serialize.</param>
		/// <returns>A protocol buffer corresponding to the serialized entity mention.</returns>
		public virtual CoreNLPProtos.Entity ToProto(EntityMention ent)
		{
			CoreNLPProtos.Entity.Builder builder = CoreNLPProtos.Entity.NewBuilder();
			// From ExtractionObject
			if (ent.GetObjectId() != null)
			{
				builder.SetObjectID(ent.GetObjectId());
			}
			if (ent.GetExtent() != null)
			{
				builder.SetExtentStart(ent.GetExtent().Start()).SetExtentEnd(ent.GetExtent().End());
			}
			if (ent.GetType() != null)
			{
				builder.SetType(ent.GetType());
			}
			if (ent.GetSubType() != null)
			{
				builder.SetSubtype(ent.GetSubType());
			}
			// From Entity
			if (ent.GetHead() != null)
			{
				builder.SetHeadStart(ent.GetHead().Start());
				builder.SetHeadEnd(ent.GetHead().End());
			}
			if (ent.GetMentionType() != null)
			{
				builder.SetMentionType(ent.GetMentionType());
			}
			if (ent.GetNormalizedName() != null)
			{
				builder.SetNormalizedName(ent.GetNormalizedName());
			}
			if (ent.GetSyntacticHeadTokenPosition() >= 0)
			{
				builder.SetHeadTokenIndex(ent.GetSyntacticHeadTokenPosition());
			}
			if (ent.GetCorefID() != null)
			{
				builder.SetCorefID(ent.GetCorefID());
			}
			// Return
			return ((CoreNLPProtos.Entity)builder.Build());
		}

		/// <summary>Serialize the given relation mention to the corresponding protocol buffer.</summary>
		/// <param name="rel">The relation mention to serialize.</param>
		/// <returns>A protocol buffer corresponding to the serialized relation mention.</returns>
		public virtual CoreNLPProtos.Relation ToProto(RelationMention rel)
		{
			CoreNLPProtos.Relation.Builder builder = CoreNLPProtos.Relation.NewBuilder();
			// From ExtractionObject
			if (rel.GetObjectId() != null)
			{
				builder.SetObjectID(rel.GetObjectId());
			}
			if (rel.GetExtent() != null)
			{
				builder.SetExtentStart(rel.GetExtent().Start()).SetExtentEnd(rel.GetExtent().End());
			}
			if (rel.GetType() != null)
			{
				builder.SetType(rel.GetType());
			}
			if (rel.GetSubType() != null)
			{
				builder.SetSubtype(rel.GetSubType());
			}
			// From Relation
			if (rel.GetArgNames() != null)
			{
				rel.GetArgNames().ForEach(null);
			}
			if (rel.GetArgs() != null)
			{
				foreach (ExtractionObject arg in rel.GetArgs())
				{
					builder.AddArg(ToProto((EntityMention)arg));
				}
			}
			// Return
			return ((CoreNLPProtos.Relation)builder.Build());
		}

		/// <summary>Serialize a CoreNLP Language to a Protobuf Language.</summary>
		/// <param name="lang">The language to serialize.</param>
		/// <returns>The language in a Protobuf enum.</returns>
		public static CoreNLPProtos.Language ToProto(Language lang)
		{
			switch (lang)
			{
				case Language.Arabic:
				{
					return CoreNLPProtos.Language.Arabic;
				}

				case Language.Chinese:
				{
					return CoreNLPProtos.Language.Chinese;
				}

				case Language.UniversalChinese:
				{
					return CoreNLPProtos.Language.UniversalChinese;
				}

				case Language.English:
				{
					return CoreNLPProtos.Language.English;
				}

				case Language.UniversalEnglish:
				{
					return CoreNLPProtos.Language.UniversalEnglish;
				}

				case Language.German:
				{
					return CoreNLPProtos.Language.German;
				}

				case Language.French:
				{
					return CoreNLPProtos.Language.French;
				}

				case Language.Hebrew:
				{
					return CoreNLPProtos.Language.Hebrew;
				}

				case Language.Spanish:
				{
					return CoreNLPProtos.Language.Spanish;
				}

				case Language.Unknown:
				{
					return CoreNLPProtos.Language.Unknown;
				}

				case Language.Any:
				{
					return CoreNLPProtos.Language.Any;
				}

				default:
				{
					throw new InvalidOperationException("Unknown language: " + lang);
				}
			}
		}

		/// <summary>Return a Protobuf operator from an OperatorSpec (Natural Logic).</summary>
		public static CoreNLPProtos.Operator ToProto(OperatorSpec op)
		{
			return ((CoreNLPProtos.Operator)CoreNLPProtos.Operator.NewBuilder().SetName(op.instance.ToString()).SetQuantifierSpanBegin(op.quantifierBegin).SetQuantifierSpanEnd(op.quantifierEnd).SetSubjectSpanBegin(op.subjectBegin).SetSubjectSpanEnd(op.subjectEnd
				).SetObjectSpanBegin(op.objectBegin).SetObjectSpanEnd(op.objectEnd).Build());
		}

		/// <summary>Return a Protobuf polarity from a CoreNLP Polarity (Natural Logic).</summary>
		public static CoreNLPProtos.Polarity ToProto(Polarity pol)
		{
			return ((CoreNLPProtos.Polarity)CoreNLPProtos.Polarity.NewBuilder().SetProjectEquivalence(CoreNLPProtos.NaturalLogicRelation.ForNumber(pol.ProjectLexicalRelation(NaturalLogicRelation.Equivalent).fixedIndex)).SetProjectForwardEntailment(CoreNLPProtos.NaturalLogicRelation
				.ForNumber(pol.ProjectLexicalRelation(NaturalLogicRelation.ForwardEntailment).fixedIndex)).SetProjectReverseEntailment(CoreNLPProtos.NaturalLogicRelation.ForNumber(pol.ProjectLexicalRelation(NaturalLogicRelation.ReverseEntailment).fixedIndex
				)).SetProjectNegation(CoreNLPProtos.NaturalLogicRelation.ForNumber(pol.ProjectLexicalRelation(NaturalLogicRelation.Negation).fixedIndex)).SetProjectAlternation(CoreNLPProtos.NaturalLogicRelation.ForNumber(pol.ProjectLexicalRelation(NaturalLogicRelation
				.Alternation).fixedIndex)).SetProjectCover(CoreNLPProtos.NaturalLogicRelation.ForNumber(pol.ProjectLexicalRelation(NaturalLogicRelation.Cover).fixedIndex)).SetProjectIndependence(CoreNLPProtos.NaturalLogicRelation.ForNumber(pol.ProjectLexicalRelation
				(NaturalLogicRelation.Independence).fixedIndex)).Build());
		}

		/// <summary>Return a Protobuf RelationTriple from a RelationTriple.</summary>
		public static CoreNLPProtos.SentenceFragment ToProto(SentenceFragment fragment)
		{
			return ((CoreNLPProtos.SentenceFragment)CoreNLPProtos.SentenceFragment.NewBuilder().SetAssumedTruth(fragment.assumedTruth).SetScore(fragment.score).AddAllTokenIndex(fragment.words.Stream().Map(null).Collect(Collectors.ToList())).SetRoot(fragment
				.parseTree.GetFirstRoot().Index() - 1).Build());
		}

		/// <summary>Return a Protobuf RelationTriple from a RelationTriple.</summary>
		public static CoreNLPProtos.RelationTriple ToProto(RelationTriple triple)
		{
			CoreNLPProtos.RelationTriple.Builder builder = CoreNLPProtos.RelationTriple.NewBuilder().SetSubject(triple.SubjectGloss()).SetRelation(triple.RelationGloss()).SetObject(triple.ObjectGloss()).SetConfidence(triple.confidence).AddAllSubjectTokens
				(triple.subject.Stream().Map(null).Collect(Collectors.ToList())).AddAllRelationTokens(triple.relation.Count == 1 && triple.relation[0].Get(typeof(CoreAnnotations.IndexAnnotation)) == null ? Java.Util.Collections.EmptyList() : triple.relation
				.Stream().Map(null).Collect(Collectors.ToList())).AddAllObjectTokens(triple.@object.Stream().Map(null).Collect(Collectors.ToList()));
			// case: this is not a real relation token, but rather a placeholder relation
			Optional<SemanticGraph> treeOptional = triple.AsDependencyTree();
			treeOptional.IfPresent(null);
			return ((CoreNLPProtos.RelationTriple)builder.Build());
		}

		/// <summary>Serialize a Map (from Strings to Strings) to a proto.</summary>
		/// <param name="map">The map to serialize.</param>
		/// <returns>A proto representation of the map.</returns>
		public static CoreNLPProtos.MapStringString ToMapStringStringProto(IDictionary<string, string> map)
		{
			CoreNLPProtos.MapStringString.Builder proto = CoreNLPProtos.MapStringString.NewBuilder();
			foreach (KeyValuePair<string, string> entry in map)
			{
				proto.AddKey(entry.Key);
				proto.AddValue(entry.Value);
			}
			return ((CoreNLPProtos.MapStringString)proto.Build());
		}

		/// <summary>Serialize a Map (from Integers to Strings) to a proto.</summary>
		/// <param name="map">The map to serialize.</param>
		/// <returns>A proto representation of the map.</returns>
		public static CoreNLPProtos.MapIntString ToMapIntStringProto(IDictionary<int, string> map)
		{
			CoreNLPProtos.MapIntString.Builder proto = CoreNLPProtos.MapIntString.NewBuilder();
			foreach (KeyValuePair<int, string> entry in map)
			{
				proto.AddKey(entry.Key);
				proto.AddValue(entry.Value);
			}
			return ((CoreNLPProtos.MapIntString)proto.Build());
		}

		/// <summary>Convert a quote object to a protocol buffer.</summary>
		public static CoreNLPProtos.Quote ToProtoQuote(ICoreMap quote)
		{
			CoreNLPProtos.Quote.Builder builder = CoreNLPProtos.Quote.NewBuilder();
			if (quote.Get(typeof(CoreAnnotations.TextAnnotation)) != null)
			{
				builder.SetText(quote.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.DocIDAnnotation)) != null)
			{
				builder.SetDocid(quote.Get(typeof(CoreAnnotations.DocIDAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) != null)
			{
				builder.SetBegin(quote.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)) != null)
			{
				builder.SetEnd(quote.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation)) != null)
			{
				builder.SetSentenceBegin(quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.SentenceEndAnnotation)) != null)
			{
				builder.SetSentenceEnd(quote.Get(typeof(CoreAnnotations.SentenceEndAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) != null)
			{
				builder.SetTokenBegin(quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)) != null)
			{
				builder.SetTokenEnd(quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation)) != null)
			{
				builder.SetIndex(quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation)));
			}
			if (quote.Get(typeof(CoreAnnotations.AuthorAnnotation)) != null)
			{
				builder.SetAuthor(quote.Get(typeof(CoreAnnotations.AuthorAnnotation)));
			}
			// quote attribution info
			if (quote.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)) != null)
			{
				builder.SetAttributionDependencyGraph(ToProto(quote.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
			{
				builder.SetMention(quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation)) != null)
			{
				builder.SetMentionBegin(quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation)) != null)
			{
				builder.SetMentionEnd(quote.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation)) != null)
			{
				builder.SetMentionType(quote.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation)) != null)
			{
				builder.SetMentionSieve(quote.Get(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) != null)
			{
				builder.SetSpeaker(quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation)) != null)
			{
				builder.SetSpeakerSieve(quote.Get(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation)) != null)
			{
				builder.SetCanonicalMention(quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionBeginAnnotation)) != null)
			{
				builder.SetCanonicalMentionBegin(quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionBeginAnnotation)));
			}
			if (quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionEndAnnotation)) != null)
			{
				builder.SetCanonicalMentionEnd(quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionEndAnnotation)));
			}
			return ((CoreNLPProtos.Quote)builder.Build());
		}

		/// <summary>Convert a mention object to a protocol buffer.</summary>
		public virtual CoreNLPProtos.NERMention ToProtoMention(ICoreMap mention)
		{
			CoreNLPProtos.NERMention.Builder builder = CoreNLPProtos.NERMention.NewBuilder();
			if (mention.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)) != null)
			{
				builder.SetSentenceIndex(mention.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) != null)
			{
				builder.SetTokenStartInSentenceInclusive(mention.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.TokenEndAnnotation)) != null)
			{
				builder.SetTokenEndInSentenceExclusive(mention.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) != null)
			{
				builder.SetNer(mention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)) != null)
			{
				builder.SetNormalizedNER(mention.Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) != null)
			{
				builder.SetEntityType(mention.Get(typeof(CoreAnnotations.EntityTypeAnnotation)));
			}
			if (mention.Get(typeof(TimeAnnotations.TimexAnnotation)) != null)
			{
				builder.SetTimex(ToProto(mention.Get(typeof(TimeAnnotations.TimexAnnotation))));
			}
			if (mention.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)) != null)
			{
				builder.SetWikipediaEntity(mention.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.GenderAnnotation)) != null)
			{
				builder.SetGender(mention.Get(typeof(CoreAnnotations.GenderAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation)) != null)
			{
				builder.SetEntityMentionIndex(mention.Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation)) != null)
			{
				builder.SetCanonicalEntityMentionIndex(mention.Get(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation)));
			}
			if (mention.Get(typeof(CoreAnnotations.TextAnnotation)) != null)
			{
				builder.SetEntityMentionText(mention.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			return ((CoreNLPProtos.NERMention)builder.Build());
		}

		/// <summary>Create a CoreLabel from its serialized counterpart.</summary>
		/// <remarks>
		/// Create a CoreLabel from its serialized counterpart.
		/// Note that this is, by itself, a lossy operation. Fields like the docid (sentence index, etc.) are only known
		/// from the enclosing document, and are not tracked in the protobuf.
		/// </remarks>
		/// <param name="proto">The serialized protobuf to read the CoreLabel from.</param>
		/// <returns>A CoreLabel, missing the fields that are not stored in the CoreLabel protobuf.</returns>
		public virtual CoreLabel FromProto(CoreNLPProtos.Token proto)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			CoreLabel word = new CoreLabel();
			// Required fields
			word.SetWord(proto.GetWord());
			// Optional fields
			if (proto.HasPos())
			{
				word.SetTag(proto.GetPos());
			}
			if (proto.HasValue())
			{
				word.SetValue(proto.GetValue());
			}
			if (proto.HasCategory())
			{
				word.SetCategory(proto.GetCategory());
			}
			if (proto.HasBefore())
			{
				word.SetBefore(proto.GetBefore());
			}
			if (proto.HasAfter())
			{
				word.SetAfter(proto.GetAfter());
			}
			if (proto.HasOriginalText())
			{
				word.SetOriginalText(proto.GetOriginalText());
			}
			if (proto.HasNer())
			{
				word.SetNER(proto.GetNer());
			}
			if (proto.HasCoarseNER())
			{
				word.Set(typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation), proto.GetCoarseNER());
			}
			if (proto.HasFineGrainedNER())
			{
				word.Set(typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation), proto.GetFineGrainedNER());
			}
			if (proto.HasLemma())
			{
				word.SetLemma(proto.GetLemma());
			}
			if (proto.HasBeginChar())
			{
				word.SetBeginPosition(proto.GetBeginChar());
			}
			if (proto.HasEndChar())
			{
				word.SetEndPosition(proto.GetEndChar());
			}
			if (proto.HasSpeaker())
			{
				word.Set(typeof(CoreAnnotations.SpeakerAnnotation), proto.GetSpeaker());
			}
			if (proto.HasUtterance())
			{
				word.Set(typeof(CoreAnnotations.UtteranceAnnotation), proto.GetUtterance());
			}
			if (proto.HasBeginIndex())
			{
				word.Set(typeof(CoreAnnotations.BeginIndexAnnotation), proto.GetBeginIndex());
			}
			if (proto.HasEndIndex())
			{
				word.Set(typeof(CoreAnnotations.EndIndexAnnotation), proto.GetEndIndex());
			}
			if (proto.HasTokenBeginIndex())
			{
				word.Set(typeof(CoreAnnotations.TokenBeginAnnotation), proto.GetTokenBeginIndex());
			}
			if (proto.HasTokenEndIndex())
			{
				word.Set(typeof(CoreAnnotations.TokenEndAnnotation), proto.GetTokenEndIndex());
			}
			if (proto.HasNormalizedNER())
			{
				word.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), proto.GetNormalizedNER());
			}
			if (proto.HasTimexValue())
			{
				word.Set(typeof(TimeAnnotations.TimexAnnotation), FromProto(proto.GetTimexValue()));
			}
			if (proto.HasHasXmlContext() && proto.GetHasXmlContext())
			{
				word.Set(typeof(CoreAnnotations.XmlContextAnnotation), proto.GetXmlContextList());
			}
			if (proto.HasCorefClusterID())
			{
				word.Set(typeof(CorefCoreAnnotations.CorefClusterIdAnnotation), proto.GetCorefClusterID());
			}
			if (proto.HasAnswer())
			{
				word.Set(typeof(CoreAnnotations.AnswerAnnotation), proto.GetAnswer());
			}
			if (proto.HasOperator())
			{
				word.Set(typeof(NaturalLogicAnnotations.OperatorAnnotation), FromProto(proto.GetOperator()));
			}
			if (proto.HasPolarity())
			{
				word.Set(typeof(NaturalLogicAnnotations.PolarityAnnotation), FromProto(proto.GetPolarity()));
			}
			if (proto.HasPolarityDir())
			{
				word.Set(typeof(NaturalLogicAnnotations.PolarityDirectionAnnotation), proto.GetPolarityDir());
			}
			if (proto.HasSpan())
			{
				word.Set(typeof(CoreAnnotations.SpanAnnotation), new IntPair(proto.GetSpan().GetBegin(), proto.GetSpan().GetEnd()));
			}
			if (proto.HasSentiment())
			{
				word.Set(typeof(SentimentCoreAnnotations.SentimentClass), proto.GetSentiment());
			}
			if (proto.HasQuotationIndex())
			{
				word.Set(typeof(CoreAnnotations.QuotationIndexAnnotation), proto.GetQuotationIndex());
			}
			if (proto.HasConllUFeatures())
			{
				word.Set(typeof(CoreAnnotations.CoNLLUFeats), FromProto(proto.GetConllUFeatures()));
			}
			if (proto.HasConllUMisc())
			{
				word.Set(typeof(CoreAnnotations.CoNLLUMisc), proto.GetConllUMisc());
			}
			if (proto.HasCoarseTag())
			{
				word.Set(typeof(CoreAnnotations.CoarseTagAnnotation), proto.GetCoarseTag());
			}
			if (proto.HasConllUTokenSpan())
			{
				word.Set(typeof(CoreAnnotations.CoNLLUTokenSpanAnnotation), new IntPair(proto.GetConllUTokenSpan().GetBegin(), proto.GetSpan().GetEnd()));
			}
			if (proto.HasConllUSecondaryDeps())
			{
				word.Set(typeof(CoreAnnotations.CoNLLUSecondaryDepsAnnotation), FromProto(proto.GetConllUSecondaryDeps()));
			}
			if (proto.HasWikipediaEntity())
			{
				word.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), proto.GetWikipediaEntity());
			}
			if (proto.HasIsNewline())
			{
				word.Set(typeof(CoreAnnotations.IsNewlineAnnotation), proto.GetIsNewline());
			}
			// Chinese char info
			if (proto.HasChineseChar())
			{
				word.Set(typeof(CoreAnnotations.ChineseCharAnnotation), proto.GetChineseChar());
			}
			if (proto.HasChineseSeg())
			{
				word.Set(typeof(CoreAnnotations.ChineseSegAnnotation), proto.GetChineseSeg());
			}
			if (proto.HasChineseXMLChar())
			{
				word.Set(typeof(SegmenterCoreAnnotations.XMLCharAnnotation), proto.GetChineseXMLChar());
			}
			// Non-default annotators
			if (proto.HasGender())
			{
				word.Set(typeof(CoreAnnotations.GenderAnnotation), proto.GetGender());
			}
			if (proto.HasTrueCase())
			{
				word.Set(typeof(CoreAnnotations.TrueCaseAnnotation), proto.GetTrueCase());
			}
			if (proto.HasTrueCaseText())
			{
				word.Set(typeof(CoreAnnotations.TrueCaseTextAnnotation), proto.GetTrueCaseText());
			}
			// section stuff
			// handle section start info
			if (proto.HasSectionName() || proto.HasSectionAuthor() || proto.HasSectionDate())
			{
				ICoreMap sectionAnnotations = new ArrayCoreMap();
				if (proto.HasSectionName())
				{
					sectionAnnotations.Set(typeof(CoreAnnotations.SectionAnnotation), proto.GetSectionName());
				}
				if (proto.HasSectionDate())
				{
					sectionAnnotations.Set(typeof(CoreAnnotations.SectionDateAnnotation), proto.GetSectionDate());
				}
				if (proto.HasSectionAuthor())
				{
					sectionAnnotations.Set(typeof(CoreAnnotations.AuthorAnnotation), proto.GetSectionAuthor());
				}
				word.Set(typeof(CoreAnnotations.SectionStartAnnotation), sectionAnnotations);
			}
			// handle section end info
			if (proto.HasSectionEndLabel())
			{
				word.Set(typeof(CoreAnnotations.SectionEndAnnotation), proto.GetSectionEndLabel());
			}
			// get parents for French tokens
			if (proto.HasParent())
			{
				word.Set(typeof(CoreAnnotations.ParentAnnotation), proto.GetParent());
			}
			// mention info
			if (proto.HasEntityMentionIndex())
			{
				word.Set(typeof(CoreAnnotations.EntityMentionIndexAnnotation), proto.GetEntityMentionIndex());
			}
			if (proto.GetCorefMentionIndexList().Count != 0)
			{
			}
			//word.set(CorefMentionIndexesAnnotation.class, proto.getCorefMentionIndexList());
			// Return
			return word;
		}

		/// <summary>Create a CoreMap representing a sentence from this protocol buffer.</summary>
		/// <remarks>
		/// Create a CoreMap representing a sentence from this protocol buffer.
		/// This should not be used if you are reading a whole document, as it populates the tokens independent of the
		/// document tokens, which is not the behavior an
		/// <see cref="Annotation"/>
		/// expects.
		/// </remarks>
		/// <param name="proto">The protocol buffer to read from.</param>
		/// <returns>A CoreMap representing the sentence.</returns>
		[Obsolete]
		public virtual ICoreMap FromProto(CoreNLPProtos.Sentence proto)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			ICoreMap lossySentence = FromProtoNoTokens(proto);
			// Add tokens -- missing by default as they're populated as sublists of the document tokens
			IList<CoreLabel> tokens = proto.GetTokenList().Stream().Map(null).Collect(Collectors.ToList());
			lossySentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			// Add dependencies
			if (proto.HasBasicDependencies())
			{
				lossySentence.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), FromProto(proto.GetBasicDependencies(), tokens, null));
			}
			if (proto.HasCollapsedDependencies())
			{
				lossySentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), FromProto(proto.GetCollapsedDependencies(), tokens, null));
			}
			if (proto.HasCollapsedCCProcessedDependencies())
			{
				lossySentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), FromProto(proto.GetCollapsedCCProcessedDependencies(), tokens, null));
			}
			if (proto.HasAlternativeDependencies())
			{
				lossySentence.Set(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation), FromProto(proto.GetAlternativeDependencies(), tokens, null));
			}
			if (proto.HasEnhancedDependencies())
			{
				lossySentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), FromProto(proto.GetEnhancedDependencies(), tokens, null));
			}
			if (proto.HasEnhancedPlusPlusDependencies())
			{
				lossySentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), FromProto(proto.GetEnhancedPlusPlusDependencies(), tokens, null));
			}
			// Add entailed sentences
			if (proto.GetEntailedSentenceCount() > 0)
			{
				IList<SentenceFragment> entailedSentences = proto.GetEntailedSentenceList().Stream().Map(null).Collect(Collectors.ToList());
				lossySentence.Set(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation), entailedSentences);
			}
			// Add entailed clauses
			if (proto.GetEntailedClauseCount() > 0)
			{
				IList<SentenceFragment> entailedClauses = proto.GetEntailedClauseList().Stream().Map(null).Collect(Collectors.ToList());
				lossySentence.Set(typeof(NaturalLogicAnnotations.EntailedClausesAnnotation), entailedClauses);
			}
			// Add relation triples
			if (proto.GetOpenieTripleCount() > 0)
			{
				throw new InvalidOperationException("Cannot deserialize OpenIE triples with this method!");
			}
			if (proto.GetKbpTripleCount() > 0)
			{
				throw new InvalidOperationException("Cannot deserialize KBP triples with this method!");
			}
			// Add chinese characters
			if (proto.GetCharacterCount() > 0)
			{
				IList<CoreLabel> sentenceCharacters = proto.GetCharacterList().Stream().Map(null).Collect(Collectors.ToList());
				lossySentence.Set(typeof(SegmenterCoreAnnotations.CharactersAnnotation), sentenceCharacters);
			}
			// Add text -- missing by default as it's populated from the Document
			lossySentence.Set(typeof(CoreAnnotations.TextAnnotation), RecoverOriginalText(tokens, proto));
			// add section info
			if (proto.HasSectionName())
			{
				lossySentence.Set(typeof(CoreAnnotations.SectionAnnotation), proto.GetSectionName());
			}
			if (proto.HasSectionDate())
			{
				lossySentence.Set(typeof(CoreAnnotations.SectionDateAnnotation), proto.GetSectionDate());
			}
			if (proto.HasSectionAuthor())
			{
				lossySentence.Set(typeof(CoreAnnotations.AuthorAnnotation), proto.GetSectionAuthor());
			}
			if (proto.HasSectionIndex())
			{
				lossySentence.Set(typeof(CoreAnnotations.SectionIndexAnnotation), proto.GetSectionIndex());
			}
			// add quote info
			if (proto.HasChapterIndex())
			{
				lossySentence.Set(typeof(ChapterAnnotator.ChapterAnnotation), proto.GetChapterIndex());
			}
			if (proto.HasParagraphIndex())
			{
				lossySentence.Set(typeof(CoreAnnotations.ParagraphIndexAnnotation), proto.GetParagraphIndex());
			}
			// Return
			return lossySentence;
		}

		/// <summary>Create a CoreMap representing a sentence from this protocol buffer.</summary>
		/// <remarks>
		/// Create a CoreMap representing a sentence from this protocol buffer.
		/// Note that the sentence is very lossy -- most glaringly, the tokens are missing, awaiting a document
		/// to be filled in from.
		/// </remarks>
		/// <param name="proto">The serialized protobuf to read the sentence from.</param>
		/// <returns>A CoreMap, representing a sentence as stored in the protocol buffer (and therefore missing some fields)</returns>
		protected internal virtual ICoreMap FromProtoNoTokens(CoreNLPProtos.Sentence proto)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			ICoreMap sentence = new ArrayCoreMap();
			// Required fields
			sentence.Set(typeof(CoreAnnotations.TokenBeginAnnotation), proto.GetTokenOffsetBegin());
			sentence.Set(typeof(CoreAnnotations.TokenEndAnnotation), proto.GetTokenOffsetEnd());
			// Optional fields
			if (proto.HasSentenceIndex())
			{
				sentence.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), proto.GetSentenceIndex());
			}
			if (proto.HasCharacterOffsetBegin())
			{
				sentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), proto.GetCharacterOffsetBegin());
			}
			if (proto.HasCharacterOffsetEnd())
			{
				sentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), proto.GetCharacterOffsetEnd());
			}
			if (proto.HasParseTree())
			{
				sentence.Set(typeof(TreeCoreAnnotations.TreeAnnotation), FromProto(proto.GetParseTree()));
			}
			if (proto.HasBinarizedParseTree())
			{
				sentence.Set(typeof(TreeCoreAnnotations.BinarizedTreeAnnotation), FromProto(proto.GetBinarizedParseTree()));
			}
			if (proto.GetKBestParseTreesCount() > 0)
			{
				IList<Tree> trees = proto.GetKBestParseTreesList().Stream().Map(null).Collect(Collectors.ToCollection(null));
				sentence.Set(typeof(TreeCoreAnnotations.KBestTreesAnnotation), trees);
			}
			if (proto.HasAnnotatedParseTree())
			{
				sentence.Set(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree), FromProto(proto.GetAnnotatedParseTree()));
			}
			if (proto.HasSentiment())
			{
				sentence.Set(typeof(SentimentCoreAnnotations.SentimentClass), proto.GetSentiment());
			}
			// Non-default fields
			if (proto.HasHasRelationAnnotations() && proto.GetHasRelationAnnotations())
			{
				// set entities
				IList<EntityMention> entities = proto.GetEntityList().Stream().Map(null).Collect(Collectors.ToList());
				sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), entities);
				// set relations
				IList<RelationMention> relations = proto.GetRelationList().Stream().Map(null).Collect(Collectors.ToList());
				sentence.Set(typeof(MachineReadingAnnotations.RelationMentionsAnnotation), relations);
			}
			// add entity mentions for this sentence
			//List<CoreMap> mentions = proto.getMentionsList().stream().map(this::fromProto).collect(Collectors.toList());
			//sentence.set(CoreAnnotations.MentionsAnnotation.class, mentions);
			// if there are mentions for this sentence, add them to the annotation
			LoadSentenceMentions(proto, sentence);
			// add section info
			if (proto.HasSectionName())
			{
				sentence.Set(typeof(CoreAnnotations.SectionAnnotation), proto.GetSectionName());
			}
			if (proto.HasSectionDate())
			{
				sentence.Set(typeof(CoreAnnotations.SectionDateAnnotation), proto.GetSectionDate());
			}
			if (proto.HasSectionAuthor())
			{
				sentence.Set(typeof(CoreAnnotations.AuthorAnnotation), proto.GetSectionAuthor());
			}
			if (proto.HasSectionIndex())
			{
				sentence.Set(typeof(CoreAnnotations.SectionIndexAnnotation), proto.GetSectionIndex());
			}
			// add quoted info
			if (proto.HasSectionQuoted())
			{
				sentence.Set(typeof(CoreAnnotations.QuotedAnnotation), proto.GetSectionQuoted());
			}
			// add quote info
			if (proto.HasChapterIndex())
			{
				sentence.Set(typeof(ChapterAnnotator.ChapterAnnotation), proto.GetChapterIndex());
			}
			if (proto.HasParagraphIndex())
			{
				sentence.Set(typeof(CoreAnnotations.ParagraphIndexAnnotation), proto.GetParagraphIndex());
			}
			// Return
			return sentence;
		}

		protected internal virtual void LoadSentenceMentions(CoreNLPProtos.Sentence proto, ICoreMap sentence)
		{
			// add all Mentions for this sentence
			if (proto.GetHasCorefMentionsAnnotation())
			{
				sentence.Set(typeof(CorefCoreAnnotations.CorefMentionsAnnotation), new List<Mention>());
			}
			if (proto.GetMentionsForCorefList().Count != 0)
			{
				Dictionary<int, Mention> idToMention = new Dictionary<int, Mention>();
				IList<Mention> sentenceMentions = sentence.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation));
				// initial set up of all mentions
				foreach (CoreNLPProtos.Mention protoMention in proto.GetMentionsForCorefList())
				{
					Mention m = FromProtoNoTokens(protoMention);
					sentenceMentions.Add(m);
					idToMention[m.mentionID] = m;
				}
				// populate sets of Mentions for each Mention
				foreach (CoreNLPProtos.Mention protoMention_1 in proto.GetMentionsForCorefList())
				{
					Mention m = idToMention[protoMention_1.GetMentionID()];
					if (protoMention_1.GetAppositionsList().Count != 0)
					{
						m.appositions = new HashSet<Mention>();
						Sharpen.Collections.AddAll(m.appositions, protoMention_1.GetAppositionsList().Stream().Map(null).Collect(Collectors.ToList()));
					}
					if (protoMention_1.GetPredicateNominativesList().Count != 0)
					{
						m.predicateNominatives = new HashSet<Mention>();
						Sharpen.Collections.AddAll(m.predicateNominatives, protoMention_1.GetPredicateNominativesList().Stream().Map(null).Collect(Collectors.ToList()));
					}
					if (protoMention_1.GetRelativePronounsList().Count != 0)
					{
						m.relativePronouns = new HashSet<Mention>();
						Sharpen.Collections.AddAll(m.relativePronouns, protoMention_1.GetRelativePronounsList().Stream().Map(null).Collect(Collectors.ToList()));
					}
					if (protoMention_1.GetListMembersList().Count != 0)
					{
						m.listMembers = new HashSet<Mention>();
						Sharpen.Collections.AddAll(m.listMembers, protoMention_1.GetListMembersList().Stream().Map(null).Collect(Collectors.ToList()));
					}
					if (protoMention_1.GetBelongToListsList().Count != 0)
					{
						m.belongToLists = new HashSet<Mention>();
						Sharpen.Collections.AddAll(m.belongToLists, protoMention_1.GetBelongToListsList().Stream().Map(null).Collect(Collectors.ToList()));
					}
				}
			}
		}

		/// <summary>
		/// Returns a complete document, intended to mimic a document passes as input to
		/// <see cref="ToProto(Annotation)"/>
		/// as closely as possible.
		/// That is, most common fields are serialized, but there is not guarantee that custom additions
		/// will be saved and retrieved.
		/// </summary>
		/// <param name="proto">The protocol buffer to read the document from.</param>
		/// <returns>An Annotation corresponding to the read protobuf.</returns>
		public virtual Annotation FromProto(CoreNLPProtos.Document proto)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			// Set text
			Annotation ann = new Annotation(proto.GetText());
			// if there are characters, add characters
			if (proto.GetCharacterCount() > 0)
			{
				IList<CoreLabel> docChars = new List<CoreLabel>();
				foreach (CoreNLPProtos.Token c in proto.GetCharacterList())
				{
					docChars.Add(FromProto(c));
				}
				ann.Set(typeof(SegmenterCoreAnnotations.CharactersAnnotation), docChars);
			}
			bool hasCorefInfo = proto.GetHasCorefAnnotation();
			// Add tokens
			IList<CoreLabel> tokens = new List<CoreLabel>();
			if (proto.GetSentenceCount() > 0)
			{
				// Populate the tokens from the sentence
				foreach (CoreNLPProtos.Sentence sentence in proto.GetSentenceList())
				{
					// It's conceivable that the sentences are not contiguous -- pad this with nulls
					while (sentence.HasTokenOffsetBegin() && tokens.Count < sentence.GetTokenOffsetBegin())
					{
						tokens.Add(null);
					}
					// Read the sentence
					foreach (CoreNLPProtos.Token token in sentence.GetTokenList())
					{
						// make CoreLabel
						CoreLabel coreLabel = FromProto(token);
						// if there is coref info, set coref mention indexes info for this token
						if (hasCorefInfo)
						{
							coreLabel.Set(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation), new HashSet<int>());
							foreach (int corefMentionIndex in token.GetCorefMentionIndexList())
							{
								coreLabel.Get(typeof(CorefCoreAnnotations.CorefMentionIndexesAnnotation)).Add(corefMentionIndex);
							}
						}
						// Set docid
						if (proto.HasDocID())
						{
							coreLabel.SetDocID(proto.GetDocID());
						}
						if (token.HasTokenBeginIndex() && token.HasTokenEndIndex())
						{
							// This is usually true, if enough annotators are defined
							while (tokens.Count < sentence.GetTokenOffsetEnd())
							{
								tokens.Add(null);
							}
							for (int i = token.GetTokenBeginIndex(); i < token.GetTokenEndIndex(); ++i)
							{
								tokens.Set(token.GetTokenBeginIndex(), coreLabel);
							}
						}
						else
						{
							// Assume this token spans a single token, and just add it to the tokens list
							tokens.Add(coreLabel);
						}
					}
				}
			}
			else
			{
				if (proto.GetSentencelessTokenCount() > 0)
				{
					// Eek -- no sentences. Try to recover tokens directly
					if (proto.GetSentencelessTokenCount() > 0)
					{
						foreach (CoreNLPProtos.Token token in proto.GetSentencelessTokenList())
						{
							CoreLabel coreLabel = FromProto(token);
							// Set docid
							if (proto.HasDocID())
							{
								coreLabel.SetDocID(proto.GetDocID());
							}
							tokens.Add(coreLabel);
						}
					}
				}
			}
			if (!tokens.IsEmpty())
			{
				ann.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			}
			// add entity mentions
			if (proto.GetHasEntityMentionsAnnotation())
			{
				ann.Set(typeof(CoreAnnotations.MentionsAnnotation), new List<ICoreMap>());
			}
			// Add sentences
			IList<ICoreMap> sentences = new List<ICoreMap>(proto.GetSentenceCount());
			for (int sentIndex = 0; sentIndex < proto.GetSentenceCount(); ++sentIndex)
			{
				CoreNLPProtos.Sentence sentence = proto.GetSentence(sentIndex);
				ICoreMap map = FromProtoNoTokens(sentence);
				if (!tokens.IsEmpty() && sentence.HasTokenOffsetBegin() && sentence.HasTokenOffsetEnd() && map.Get(typeof(CoreAnnotations.TokensAnnotation)) == null)
				{
					// Set tokens for sentence
					int tokenBegin = sentence.GetTokenOffsetBegin();
					int tokenEnd = sentence.GetTokenOffsetEnd();
					System.Diagnostics.Debug.Assert(tokenBegin <= tokens.Count && tokenBegin <= tokenEnd);
					System.Diagnostics.Debug.Assert(tokenEnd <= tokens.Count);
					map.Set(typeof(CoreAnnotations.TokensAnnotation), tokens.SubList(tokenBegin, tokenEnd));
					// Set sentence index + token index + paragraph index
					for (int i = tokenBegin; i < tokenEnd; ++i)
					{
						CoreLabel token = tokens[i];
						if (token != null)
						{
							token.SetSentIndex(sentIndex);
							token.SetIndex(i - sentence.GetTokenOffsetBegin() + 1);
							if (sentence.HasParagraph())
							{
								token.Set(typeof(CoreAnnotations.ParagraphAnnotation), sentence.GetParagraph());
							}
						}
					}
					// Set text
					int characterBegin = sentence.GetCharacterOffsetBegin();
					int characterEnd = sentence.GetCharacterOffsetEnd();
					if (characterEnd <= proto.GetText().Length)
					{
						// The usual case -- get the text from the document text
						map.Set(typeof(CoreAnnotations.TextAnnotation), Sharpen.Runtime.Substring(proto.GetText(), characterBegin, characterEnd));
					}
					else
					{
						// The document text is wrong -- guess the text from the tokens
						map.Set(typeof(CoreAnnotations.TextAnnotation), RecoverOriginalText(tokens.SubList(tokenBegin, tokenEnd), sentence));
					}
					// add entity mentions for this sentence
					IList<ICoreMap> mentions = sentence.GetMentionsList().Stream().Map(null).Collect(Collectors.ToList());
					// add tokens to each entity mention
					foreach (ICoreMap entityMention in mentions)
					{
						IList<CoreLabel> entityMentionTokens = new List<CoreLabel>();
						for (int tokenIndex = entityMention.Get(typeof(CoreAnnotations.TokenBeginAnnotation)); tokenIndex < entityMention.Get(typeof(CoreAnnotations.TokenEndAnnotation)); tokenIndex++)
						{
							entityMentionTokens.Add(tokens[tokenIndex]);
						}
						int emCharOffsetBegin = entityMentionTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
						int emCharOffsetEnd = entityMentionTokens[entityMentionTokens.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
						// set character offsets
						entityMention.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), emCharOffsetBegin);
						entityMention.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), emCharOffsetEnd);
						entityMention.Set(typeof(CoreAnnotations.TokensAnnotation), entityMentionTokens);
					}
					if (sentence.GetHasEntityMentionsAnnotation())
					{
						map.Set(typeof(CoreAnnotations.MentionsAnnotation), mentions);
					}
					// add to document level list of entity mentions
					IList<ICoreMap> mentionsOnAnnotation = ann.Get(typeof(CoreAnnotations.MentionsAnnotation));
					if (mentionsOnAnnotation != null)
					{
						foreach (ICoreMap sentenceEM in mentions)
						{
							ann.Get(typeof(CoreAnnotations.MentionsAnnotation)).Add(sentenceEM);
						}
					}
				}
				// End iteration
				sentences.Add(map);
			}
			if (!sentences.IsEmpty())
			{
				ann.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
			}
			// Set DocID
			string docid = null;
			if (proto.HasDocID())
			{
				docid = proto.GetDocID();
				ann.Set(typeof(CoreAnnotations.DocIDAnnotation), docid);
			}
			// Set reference time
			if (proto.HasDocDate())
			{
				ann.Set(typeof(CoreAnnotations.DocDateAnnotation), proto.GetDocDate());
			}
			if (proto.HasCalendar())
			{
				GregorianCalendar calendar = new GregorianCalendar();
				calendar.SetTimeInMillis(proto.GetCalendar());
				ann.Set(typeof(CoreAnnotations.CalendarAnnotation), calendar);
			}
			// Set coref chain
			IDictionary<int, CorefChain> corefChains = new Dictionary<int, CorefChain>();
			foreach (CoreNLPProtos.CorefChain chainProto in proto.GetCorefChainList())
			{
				CorefChain chain = FromProto(chainProto, ann);
				corefChains[chain.GetChainID()] = chain;
			}
			if (proto.GetHasCorefAnnotation())
			{
				ann.Set(typeof(CorefCoreAnnotations.CorefChainAnnotation), corefChains);
			}
			// Set document coref mentions list ; this gets populated when sentences build CorefMentions below
			if (proto.GetHasCorefMentionAnnotation())
			{
				ann.Set(typeof(CorefCoreAnnotations.CorefMentionsAnnotation), new List<Mention>());
			}
			// hashes to access Mentions , later in this method need to add speakerInfo to Mention
			// so we need to create id -> Mention, CoreNLPProtos.Mention maps to do this, since SpeakerInfo could reference
			// any Mention in doc
			Dictionary<int, Mention> idToMention = new Dictionary<int, Mention>();
			Dictionary<int, CoreNLPProtos.Mention> idToProtoMention = new Dictionary<int, CoreNLPProtos.Mention>();
			// Set things in the sentence that need a document context.
			for (int sentenceIndex = 0; sentenceIndex < proto.GetSentenceCount(); ++sentenceIndex)
			{
				CoreNLPProtos.Sentence sentence = proto.GetSentenceList()[sentenceIndex];
				ICoreMap map = sentences[sentenceIndex];
				IList<CoreLabel> sentenceTokens = map.Get(typeof(CoreAnnotations.TokensAnnotation));
				// Set dependency graphs
				if (sentence.HasBasicDependencies())
				{
					map.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), FromProto(sentence.GetBasicDependencies(), sentenceTokens, docid));
				}
				if (sentence.HasCollapsedDependencies())
				{
					map.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), FromProto(sentence.GetCollapsedDependencies(), sentenceTokens, docid));
				}
				if (sentence.HasCollapsedCCProcessedDependencies())
				{
					map.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), FromProto(sentence.GetCollapsedCCProcessedDependencies(), sentenceTokens, docid));
				}
				if (sentence.HasAlternativeDependencies())
				{
					map.Set(typeof(SemanticGraphCoreAnnotations.AlternativeDependenciesAnnotation), FromProto(sentence.GetAlternativeDependencies(), sentenceTokens, docid));
				}
				if (sentence.HasEnhancedDependencies())
				{
					map.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), FromProto(sentence.GetEnhancedDependencies(), sentenceTokens, docid));
				}
				if (sentence.HasEnhancedPlusPlusDependencies())
				{
					map.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), FromProto(sentence.GetEnhancedPlusPlusDependencies(), sentenceTokens, docid));
				}
				// Set entailed sentences
				if (sentence.GetEntailedSentenceCount() > 0)
				{
					ICollection<SentenceFragment> entailedSentences = sentence.GetEntailedSentenceList().Stream().Map(null).Collect(Collectors.ToSet());
					map.Set(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation), entailedSentences);
				}
				if (sentence.GetEntailedClauseCount() > 0)
				{
					ICollection<SentenceFragment> entailedClauses = sentence.GetEntailedClauseList().Stream().Map(null).Collect(Collectors.ToSet());
					map.Set(typeof(NaturalLogicAnnotations.EntailedClausesAnnotation), entailedClauses);
				}
				// Set relation triples
				List<RelationTriple> triples = new List<RelationTriple>();
				if (sentence.GetHasOpenieTriplesAnnotation())
				{
					map.Set(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation), triples);
				}
				if (sentence.GetOpenieTripleCount() > 0)
				{
					foreach (CoreNLPProtos.RelationTriple triple in sentence.GetOpenieTripleList())
					{
						triples.Add(FromProto(triple, ann, sentenceIndex));
					}
					map.Set(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation), triples);
				}
				// Set kbp relation triples
				if (sentence.GetHasKBPTriplesAnnotation())
				{
					map.Set(typeof(CoreAnnotations.KBPTriplesAnnotation), new List<RelationTriple>());
				}
				if (sentence.GetKbpTripleCount() > 0)
				{
					foreach (CoreNLPProtos.RelationTriple kbpTriple in sentence.GetKbpTripleList())
					{
						map.Get(typeof(CoreAnnotations.KBPTriplesAnnotation)).Add(FromProto(kbpTriple, ann, sentenceIndex));
					}
				}
				// Redo some light annotation
				if (map.ContainsKey(typeof(CoreAnnotations.TokensAnnotation)) && (!sentence.HasHasNumerizedTokensAnnotation() || sentence.GetHasNumerizedTokensAnnotation()))
				{
					map.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), NumberNormalizer.FindAndMergeNumbers(map));
				}
				// add the CoreLabel and IndexedWord info to each mention
				// when Mentions are serialized, just storing the index in the sentence for CoreLabels and IndexedWords
				// this is the point where the de-serialized sentence has tokens
				int mentionInt = 0;
				foreach (CoreNLPProtos.Mention protoMention in sentence.GetMentionsForCorefList())
				{
					// get the mention
					Mention mentionToUpdate = map.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation))[mentionInt];
					// add to document level coref mention list
					IList<Mention> mentions = ann.Get(typeof(CorefCoreAnnotations.CorefMentionsAnnotation));
					if (mentions == null)
					{
						mentions = new List<Mention>();
						ann.Set(typeof(CorefCoreAnnotations.CorefMentionsAnnotation), mentions);
					}
					mentions.Add(mentionToUpdate);
					// store these in hash for more processing later in this method
					idToMention[mentionToUpdate.mentionID] = mentionToUpdate;
					idToProtoMention[mentionToUpdate.mentionID] = protoMention;
					// update the values
					int headIndexedWordIndex = protoMention.GetHeadIndexedWord().GetTokenIndex();
					if (headIndexedWordIndex >= 0)
					{
						mentionToUpdate.headIndexedWord = new IndexedWord(sentenceTokens[protoMention.GetHeadIndexedWord().GetTokenIndex()]);
						mentionToUpdate.headIndexedWord.SetCopyCount(protoMention.GetHeadIndexedWord().GetCopyCount());
					}
					int dependingVerbIndex = protoMention.GetDependingVerb().GetTokenIndex();
					if (dependingVerbIndex >= 0)
					{
						mentionToUpdate.dependingVerb = new IndexedWord(sentenceTokens[protoMention.GetDependingVerb().GetTokenIndex()]);
						mentionToUpdate.dependingVerb.SetCopyCount(protoMention.GetDependingVerb().GetCopyCount());
					}
					int headWordIndex = protoMention.GetHeadWord().GetTokenIndex();
					if (headWordIndex >= 0)
					{
						mentionToUpdate.headWord = sentenceTokens[protoMention.GetHeadWord().GetTokenIndex()];
					}
					mentionToUpdate.sentenceWords = new List<CoreLabel>();
					foreach (CoreNLPProtos.IndexedWord clp in protoMention.GetSentenceWordsList())
					{
						int ti = clp.GetTokenIndex();
						mentionToUpdate.sentenceWords.Add(sentenceTokens[ti]);
					}
					mentionToUpdate.originalSpan = new List<CoreLabel>();
					foreach (CoreNLPProtos.IndexedWord clp_1 in protoMention.GetOriginalSpanList())
					{
						int ti = clp_1.GetTokenIndex();
						mentionToUpdate.originalSpan.Add(sentenceTokens[ti]);
					}
					if (protoMention.GetHasBasicDependency())
					{
						mentionToUpdate.basicDependency = map.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
					}
					if (protoMention.GetHasEnhancedDepenedncy())
					{
						mentionToUpdate.enhancedDependency = map.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					}
					if (protoMention.GetHasContextParseTree())
					{
						mentionToUpdate.contextParseTree = map.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					}
					// move on to next mention
					mentionInt++;
				}
			}
			// set sections if this was an xmlDoc
			if (proto.HasXmlDoc() && proto.GetXmlDoc())
			{
				// this was an xml doc so set up a list of sections
				IList<ICoreMap> listOfSections = new List<ICoreMap>();
				ann.Set(typeof(CoreAnnotations.SectionsAnnotation), listOfSections);
				foreach (CoreNLPProtos.Section section in proto.GetSectionsList())
				{
					ICoreMap sectionCoreMap = FromProto(section, ann.Get(typeof(CoreAnnotations.SentencesAnnotation)));
					ann.Get(typeof(CoreAnnotations.SectionsAnnotation)).Add(sectionCoreMap);
				}
			}
			// Set quotes
			IList<ICoreMap> quotes = proto.GetQuoteList().Stream().Map(null).Collect(Collectors.ToList());
			if (!quotes.IsEmpty())
			{
				ann.Set(typeof(CoreAnnotations.QuotationsAnnotation), quotes);
				// add the tokens to the quote tokens list
				foreach (ICoreMap quote in quotes)
				{
					IList<CoreLabel> quoteTokens = new List<CoreLabel>();
					for (int quoteTokenIndex = quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)); quoteTokenIndex <= quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)); quoteTokenIndex++)
					{
						quoteTokens.Add(ann.Get(typeof(CoreAnnotations.TokensAnnotation))[quoteTokenIndex]);
					}
					quote.Set(typeof(CoreAnnotations.TokensAnnotation), quoteTokens);
				}
			}
			// Set NERmention
			//List<CoreMap> mentions = proto.getMentionsList().stream().map(this::fromProto).collect(Collectors.toList());
			//ann.set(MentionsAnnotation.class, mentions);
			// add SpeakerInfo stuff to Mentions, this requires knowing all mentions in the document
			// also add all the Set<Mention>
			foreach (int mentionID in idToMention.Keys)
			{
				// this is the Mention message corresponding to this Mention
				Mention mentionToUpdate = idToMention[mentionID];
				CoreNLPProtos.Mention correspondingProtoMention = idToProtoMention[mentionID];
				if (!correspondingProtoMention.HasSpeakerInfo())
				{
					// keep speakerInfo null for this Mention if it didn't store a speakerInfo
					// so just continue to next Mention
					continue;
				}
				// if we're here we know a speakerInfo was stored
				SpeakerInfo speakerInfo = FromProto(correspondingProtoMention.GetSpeakerInfo());
				// go through all ids stored for the speakerInfo in its mentions list, and get the Mention
				// Mentions are stored by MentionID , MentionID should be set by MentionAnnotator
				// MentionID is ID in document, 0, 1, 2, etc...
				foreach (int speakerInfoMentionID in correspondingProtoMention.GetSpeakerInfo().GetMentionsList())
				{
					speakerInfo.AddMention(idToMention[speakerInfoMentionID]);
				}
				// now the SpeakerInfo for this Mention should be fully restored
				mentionToUpdate.speakerInfo = speakerInfo;
			}
			// if there is coref info, add mappings from entity mentions and coref mentions
			if (hasCorefInfo)
			{
				// restore the entity mention to coref mention mappings
				// entity mentions without a corresponding coref mention have -1 in the serialized mapping
				ann.Set(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation), new Dictionary<int, int>());
				int entityMentionIndex = 0;
				foreach (int corefMentionForEntityMentionIndex in proto.GetEntityMentionToCorefMentionMappingsList())
				{
					if (corefMentionForEntityMentionIndex != -1)
					{
						ann.Get(typeof(CoreAnnotations.EntityMentionToCorefMentionMappingAnnotation))[entityMentionIndex] = corefMentionForEntityMentionIndex;
					}
					entityMentionIndex++;
				}
				// restore the coref mention to entity mention mappings
				// entity mentions without a corresponding coref mention have -1 in the serialized mapping
				ann.Set(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation), new Dictionary<int, int>());
				int corefMentionIndex = 0;
				foreach (int entityMentionForCorefMentionIndex in proto.GetCorefMentionToEntityMentionMappingsList())
				{
					if (entityMentionForCorefMentionIndex != -1)
					{
						ann.Get(typeof(CoreAnnotations.CorefMentionToEntityMentionMappingAnnotation))[corefMentionIndex] = entityMentionForCorefMentionIndex;
					}
					corefMentionIndex++;
				}
			}
			// Return
			return ann;
		}

		/// <summary>Retrieve a Tree object from a saved protobuf.</summary>
		/// <remarks>
		/// Retrieve a Tree object from a saved protobuf.
		/// This is not intended to be used on its own, but it is safe (lossless) to do so and therefore it is
		/// left visible.
		/// </remarks>
		/// <param name="proto">The serialized tree.</param>
		/// <returns>
		/// A Tree object corresponding to the saved tree. This will always be a
		/// <see cref="Edu.Stanford.Nlp.Trees.LabeledScoredTreeNode"/>
		/// .
		/// </returns>
		public virtual Tree FromProto(CoreNLPProtos.ParseTree proto)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			LabeledScoredTreeNode node = new LabeledScoredTreeNode();
			// Set label
			if (proto.HasValue())
			{
				CoreLabel value = new CoreLabel();
				value.SetCategory(proto.GetValue());
				value.SetValue(proto.GetValue());
				node.SetLabel(value);
				// Set span
				if (proto.HasYieldBeginIndex() && proto.HasYieldEndIndex())
				{
					IntPair span = new IntPair(proto.GetYieldBeginIndex(), proto.GetYieldEndIndex());
					value.Set(typeof(CoreAnnotations.SpanAnnotation), span);
				}
				// Set sentiment
				if (proto.HasSentiment())
				{
					value.Set(typeof(RNNCoreAnnotations.PredictedClass), proto.GetSentiment().GetNumber());
				}
			}
			// Set score
			if (proto.HasScore())
			{
				node.SetScore(proto.GetScore());
			}
			// Set children
			Tree[] children = new LabeledScoredTreeNode[proto.GetChildCount()];
			for (int i = 0; i < children.Length; ++i)
			{
				children[i] = FromProto(proto.GetChild(i));
			}
			node.SetChildren(children);
			// Return
			return node;
		}

		/// <summary>Return a CoreNLP language from a Protobuf language</summary>
		public static Language FromProto(CoreNLPProtos.Language lang)
		{
			switch (lang)
			{
				case CoreNLPProtos.Language.Arabic:
				{
					return Language.Arabic;
				}

				case CoreNLPProtos.Language.Chinese:
				{
					return Language.Chinese;
				}

				case CoreNLPProtos.Language.English:
				{
					return Language.English;
				}

				case CoreNLPProtos.Language.German:
				{
					return Language.German;
				}

				case CoreNLPProtos.Language.French:
				{
					return Language.French;
				}

				case CoreNLPProtos.Language.Hebrew:
				{
					return Language.Hebrew;
				}

				case CoreNLPProtos.Language.Spanish:
				{
					return Language.Spanish;
				}

				case CoreNLPProtos.Language.UniversalChinese:
				{
					return Language.UniversalChinese;
				}

				case CoreNLPProtos.Language.UniversalEnglish:
				{
					return Language.UniversalEnglish;
				}

				case CoreNLPProtos.Language.Unknown:
				{
					return Language.Unknown;
				}

				case CoreNLPProtos.Language.Any:
				{
					return Language.Any;
				}

				default:
				{
					throw new InvalidOperationException("Unknown language: " + lang);
				}
			}
		}

		/// <summary>Return a CoreNLP Operator (Natural Logic operator) from a Protobuf operator</summary>
		public static OperatorSpec FromProto(CoreNLPProtos.Operator @operator)
		{
			string opName = @operator.GetName().ToLower();
			Operator op = null;
			foreach (Operator candidate in Operator.Values())
			{
				if (candidate.ToString().ToLower().Equals(opName))
				{
					op = candidate;
					break;
				}
			}
			return new OperatorSpec(op, @operator.GetQuantifierSpanBegin(), @operator.GetQuantifierSpanEnd(), @operator.GetSubjectSpanBegin(), @operator.GetSubjectSpanEnd(), @operator.GetObjectSpanBegin(), @operator.GetObjectSpanEnd());
		}

		/// <summary>Return a CoreNLP Polarity (Natural Logic polarity) from a Protobuf operator</summary>
		public static Polarity FromProto(CoreNLPProtos.Polarity polarity)
		{
			byte[] projectionFn = new byte[7];
			projectionFn[0] = unchecked((byte)polarity.GetProjectEquivalence().GetNumber());
			projectionFn[1] = unchecked((byte)polarity.GetProjectForwardEntailment().GetNumber());
			projectionFn[2] = unchecked((byte)polarity.GetProjectReverseEntailment().GetNumber());
			projectionFn[3] = unchecked((byte)polarity.GetProjectNegation().GetNumber());
			projectionFn[4] = unchecked((byte)polarity.GetProjectAlternation().GetNumber());
			projectionFn[5] = unchecked((byte)polarity.GetProjectCover().GetNumber());
			projectionFn[6] = unchecked((byte)polarity.GetProjectIndependence().GetNumber());
			return new Polarity(projectionFn);
		}

		/// <summary>Deserialize a dependency tree, allowing for cross-sentence arcs.</summary>
		/// <remarks>
		/// Deserialize a dependency tree, allowing for cross-sentence arcs.
		/// This is primarily here for deserializing OpenIE triples.
		/// </remarks>
		/// <seealso cref="FromProto(DependencyGraph, System.Collections.Generic.IList{E}, string)"/>
		private static SemanticGraph FromProto(CoreNLPProtos.DependencyGraph proto, IList<CoreLabel> sentence, string docid, Optional<Annotation> document)
		{
			SemanticGraph graph = new SemanticGraph();
			// first construct the actual nodes; keep them indexed by their index
			// This block is optimized as one of the places which take noticeable time
			// in datum caching
			int min = int.MaxValue;
			int max = int.MinValue;
			foreach (CoreNLPProtos.DependencyGraph.Node @in in proto.GetNodeList())
			{
				min = @in.GetIndex() < min ? @in.GetIndex() : min;
				max = @in.GetIndex() > max ? @in.GetIndex() : max;
			}
			TwoDimensionalMap<int, int, IndexedWord> nodes = TwoDimensionalMap.HashMap();
			foreach (CoreNLPProtos.DependencyGraph.Node in_1 in proto.GetNodeList())
			{
				CoreLabel token;
				if (document.IsPresent())
				{
					token = document.Get().Get(typeof(CoreAnnotations.SentencesAnnotation))[in_1.GetSentenceIndex()].Get(typeof(CoreAnnotations.TokensAnnotation))[in_1.GetIndex() - 1];
				}
				else
				{
					// token index starts at 1!
					token = sentence[in_1.GetIndex() - 1];
				}
				// index starts at 1!
				IndexedWord word;
				if (in_1.HasCopyAnnotation() && in_1.GetCopyAnnotation() > 0)
				{
					// TODO: if we make a copy wrapper CoreLabel, use it here instead
					word = new IndexedWord(new CoreLabel(token));
					word.SetCopyCount(in_1.GetCopyAnnotation());
				}
				else
				{
					word = new IndexedWord(token);
				}
				// for backwards compatibility - new annotations should have
				// these fields set, but annotations older than August 2014 might not
				if (word.DocID() == null && docid != null)
				{
					word.SetDocID(docid);
				}
				if (word.SentIndex() < 0 && in_1.GetSentenceIndex() >= 0)
				{
					word.SetSentIndex(in_1.GetSentenceIndex());
				}
				if (word.Index() < 0 && in_1.GetIndex() >= 0)
				{
					word.SetIndex(in_1.GetIndex());
				}
				System.Diagnostics.Debug.Assert(in_1.GetIndex() == word.Index());
				nodes.Put(in_1.GetIndex(), in_1.GetCopyAnnotation(), word);
				graph.AddVertex(word);
			}
			// add all edges to the actual graph
			foreach (CoreNLPProtos.DependencyGraph.Edge ie in proto.GetEdgeList())
			{
				IndexedWord source = nodes.Get(ie.GetSource(), ie.GetSourceCopy());
				System.Diagnostics.Debug.Assert((source != null));
				IndexedWord target = nodes.Get(ie.GetTarget(), ie.GetTargetCopy());
				System.Diagnostics.Debug.Assert((target != null));
				lock (globalLock)
				{
					// this is not thread-safe: there are static fields in GrammaticalRelation
					System.Diagnostics.Debug.Assert(ie.HasDep());
					GrammaticalRelation rel = GrammaticalRelation.ValueOf(FromProto(ie.GetLanguage()), ie.GetDep());
					graph.AddEdge(source, target, rel, 1.0, ie.HasIsExtra() && ie.GetIsExtra());
				}
			}
			if (proto.GetRootCount() > 0)
			{
				ICollection<IndexedWord> roots = proto.GetRootList().Stream().Map(null).Collect(Collectors.ToList());
				graph.SetRoots(roots);
			}
			else
			{
				// Roots were not saved away
				// compute root nodes if non-empty
				if (!graph.IsEmpty())
				{
					graph.ResetRoots();
				}
			}
			return graph;
		}

		/// <summary>
		/// Voodoo magic to convert a serialized dependency graph into a
		/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
		/// .
		/// This method is intended to be called only from the
		/// <see cref="FromProto(Document)"/>
		/// method.
		/// </summary>
		/// <param name="proto">The serialized representation of the graph. This relies heavily on indexing into the original document.</param>
		/// <param name="sentence">
		/// The raw sentence that this graph was saved from must be provided, as it is not saved in the serialized
		/// representation.
		/// </param>
		/// <param name="docid">A docid must be supplied, as it is not saved by the serialized representation.</param>
		/// <returns>A semantic graph corresponding to the saved object, on the provided sentence.</returns>
		public static SemanticGraph FromProto(CoreNLPProtos.DependencyGraph proto, IList<CoreLabel> sentence, string docid)
		{
			return FromProto(proto, sentence, docid, Optional.Empty());
		}

		/// <summary>
		/// Return a
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// object from the serialized representation.
		/// This requires a sentence and a document so that
		/// (1) we have a docid for the dependency tree can be accurately rebuilt,
		/// and (2) we have references to the tokens to include in the relation triple.
		/// </summary>
		/// <param name="proto">The serialized relation triples.</param>
		/// <param name="doc">
		/// The document we are deserializing. This document should already
		/// have a docid annotation set, if there is one.
		/// </param>
		/// <param name="sentenceIndex">The index of the sentence this extraction should be attached to.</param>
		/// <returns>A relation triple as a Java object, corresponding to the seriaized proto.</returns>
		public static RelationTriple FromProto(CoreNLPProtos.RelationTriple proto, Annotation doc, int sentenceIndex)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			// Get the spans for the extraction
			IList<CoreLabel> subject = proto.GetSubjectTokensList().Stream().Map(null).Collect(Collectors.ToList());
			IList<CoreLabel> relation;
			if (proto.GetRelationTokensCount() == 0)
			{
				// If we don't have a real span for the relation, make a dummy word
				relation = Java.Util.Collections.SingletonList(new CoreLabel(new Word(proto.GetRelation())));
			}
			else
			{
				relation = proto.GetRelationTokensList().Stream().Map(null).Collect(Collectors.ToList());
			}
			IList<CoreLabel> @object = proto.GetObjectTokensList().Stream().Map(null).Collect(Collectors.ToList());
			// Create the extraction
			RelationTriple extraction;
			double confidence = proto.GetConfidence();
			if (proto.HasTree())
			{
				SemanticGraph tree = FromProto(proto.GetTree(), doc.Get(typeof(CoreAnnotations.SentencesAnnotation))[sentenceIndex].Get(typeof(CoreAnnotations.TokensAnnotation)), doc.Get(typeof(CoreAnnotations.DocIDAnnotation)), Optional.Of(doc));
				extraction = new RelationTriple.WithTree(subject, relation, @object, tree, confidence);
			}
			else
			{
				extraction = new RelationTriple(subject, relation, @object, confidence);
			}
			// Tweak the extraction
			if (proto.HasIstmod())
			{
				extraction.Istmod(proto.GetIstmod());
			}
			if (proto.HasPrefixBe())
			{
				extraction.IsPrefixBe(proto.GetPrefixBe());
			}
			if (proto.HasSuffixBe())
			{
				extraction.IsSuffixBe(proto.GetSuffixBe());
			}
			if (proto.HasSuffixOf())
			{
				extraction.IsSuffixOf(proto.GetSuffixOf());
			}
			// Return
			return extraction;
		}

		/// <summary>Returns a sentence fragment from a given protocol buffer, and an associated parse tree.</summary>
		/// <param name="fragment">The saved sentence fragment.</param>
		/// <param name="tree">The parse tree for the whole sentence.</param>
		/// <returns>
		/// A
		/// <see cref="Edu.Stanford.Nlp.Naturalli.SentenceFragment"/>
		/// object corresponding to the saved proto.
		/// </returns>
		public static SentenceFragment FromProto(CoreNLPProtos.SentenceFragment fragment, SemanticGraph tree)
		{
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			SemanticGraph fragmentTree = new SemanticGraph(tree);
			// Set the new root
			if (fragment.HasRoot())
			{
				fragmentTree.ResetRoots();
				fragmentTree.VertexSet().Stream().Filter(null).ForEach(null);
			}
			// Set the new vertices
			ICollection<int> keptIndices = new HashSet<int>(fragment.GetTokenIndexList());
			tree.VertexSet().Stream().Filter(null).ForEach(null);
			// Apparently this sometimes screws up the tree
			fragmentTree.VertexSet().Stream().Filter(null).ForEach(null);
			// Return the fragment
			//noinspection SimplifiableConditionalExpression
			return new SentenceFragment(fragmentTree, fragment.HasAssumedTruth() ? fragment.GetAssumedTruth() : true, false).ChangeScore(fragment.HasScore() ? fragment.GetScore() : 1.0);
		}

		/// <summary>Convert a serialized Map back into a Java Map.</summary>
		/// <param name="proto">The serialized map.</param>
		/// <returns>A Java Map corresponding to the serialized map.</returns>
		public static Dictionary<string, string> FromProto(CoreNLPProtos.MapStringString proto)
		{
			Dictionary<string, string> map = new Dictionary<string, string>();
			for (int i = 0; i < proto.GetKeyCount(); ++i)
			{
				map[proto.GetKey(i)] = proto.GetValue(i);
			}
			return map;
		}

		/// <summary>Convert a serialized Map back into a Java Map.</summary>
		/// <param name="proto">The serialized map.</param>
		/// <returns>A Java Map corresponding to the serialized map.</returns>
		public static Dictionary<int, string> FromProto(CoreNLPProtos.MapIntString proto)
		{
			Dictionary<int, string> map = new Dictionary<int, string>();
			for (int i = 0; i < proto.GetKeyCount(); ++i)
			{
				map[proto.GetKey(i)] = proto.GetValue(i);
			}
			return map;
		}

		/// <summary>Read a CorefChain from its serialized representation.</summary>
		/// <remarks>
		/// Read a CorefChain from its serialized representation.
		/// This is private due to the need for an additional partial document. Also, why on Earth are you trying to use
		/// this on its own anyways?
		/// </remarks>
		/// <param name="proto">The serialized representation of the coref chain, missing information on its mention span string.</param>
		/// <param name="partialDocument">
		/// A partial document, which must contain
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.SentencesAnnotation"/>
		/// and
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TokensAnnotation"/>
		/// in
		/// order to fill in the mention span strings.
		/// </param>
		/// <returns>A coreference chain.</returns>
		private CorefChain FromProto(CoreNLPProtos.CorefChain proto, Annotation partialDocument)
		{
			// Get chain ID
			int cid = proto.GetChainID();
			// Get mentions
			IDictionary<IntPair, ICollection<CorefChain.CorefMention>> mentions = new Dictionary<IntPair, ICollection<CorefChain.CorefMention>>();
			CorefChain.CorefMention representative = null;
			for (int i = 0; i < proto.GetMentionCount(); ++i)
			{
				if (Thread.Interrupted())
				{
					throw new RuntimeInterruptedException();
				}
				CoreNLPProtos.CorefChain.CorefMention mentionProto = proto.GetMention(i);
				// Create mention
				StringBuilder mentionSpan = new StringBuilder();
				IList<CoreLabel> sentenceTokens = partialDocument.Get(typeof(CoreAnnotations.SentencesAnnotation))[mentionProto.GetSentenceIndex()].Get(typeof(CoreAnnotations.TokensAnnotation));
				for (int k = mentionProto.GetBeginIndex(); k < mentionProto.GetEndIndex(); ++k)
				{
					mentionSpan.Append(" ").Append(sentenceTokens[k].Word());
				}
				// Set the coref cluster id for the token
				CorefChain.CorefMention mention = new CorefChain.CorefMention(Dictionaries.MentionType.ValueOf(mentionProto.GetMentionType()), Dictionaries.Number.ValueOf(mentionProto.GetNumber()), Dictionaries.Gender.ValueOf(mentionProto.GetGender()), Dictionaries.Animacy
					.ValueOf(mentionProto.GetAnimacy()), mentionProto.GetBeginIndex() + 1, mentionProto.GetEndIndex() + 1, mentionProto.GetHeadIndex() + 1, cid, mentionProto.GetMentionID(), mentionProto.GetSentenceIndex() + 1, new IntTuple(new int[] { mentionProto
					.GetSentenceIndex() + 1, mentionProto.GetPosition() }), mentionSpan.Substring(mentionSpan.Length > 0 ? 1 : 0));
				// Register mention
				IntPair key = new IntPair(mentionProto.GetSentenceIndex() - 1, mentionProto.GetHeadIndex() - 1);
				if (!mentions.Contains(key))
				{
					mentions[key] = new HashSet<CorefChain.CorefMention>();
				}
				mentions[key].Add(mention);
				// Check for representative
				if (proto.HasRepresentative() && i == proto.GetRepresentative())
				{
					representative = mention;
				}
			}
			// Return
			return new CorefChain(cid, mentions, representative);
		}

		private Mention FromProtoNoTokens(CoreNLPProtos.Mention protoMention)
		{
			Mention returnMention = new Mention();
			// set enums
			if (protoMention.GetMentionType() != null && !protoMention.GetMentionType().Equals(string.Empty))
			{
				returnMention.mentionType = Dictionaries.MentionType.ValueOf(protoMention.GetMentionType());
			}
			if (protoMention.GetNumber() != null && !protoMention.GetNumber().Equals(string.Empty))
			{
				returnMention.number = Dictionaries.Number.ValueOf(protoMention.GetNumber());
			}
			if (protoMention.GetGender() != null && !protoMention.GetGender().Equals(string.Empty))
			{
				returnMention.gender = Dictionaries.Gender.ValueOf(protoMention.GetGender());
			}
			if (protoMention.GetAnimacy() != null && !protoMention.GetAnimacy().Equals(string.Empty))
			{
				returnMention.animacy = Dictionaries.Animacy.ValueOf(protoMention.GetAnimacy());
			}
			if (protoMention.GetPerson() != null && !protoMention.GetPerson().Equals(string.Empty))
			{
				returnMention.person = Dictionaries.Person.ValueOf(protoMention.GetPerson());
			}
			// TO DO: if the original Mention had "" for this field it will be lost, should deal with this problem
			if (!protoMention.GetHeadString().Equals(string.Empty))
			{
				returnMention.headString = protoMention.GetHeadString();
			}
			// TO DO: if the original Mention had "" for this field it will be lost, should deal with this problem
			if (!protoMention.GetNerString().Equals(string.Empty))
			{
				returnMention.nerString = protoMention.GetNerString();
			}
			returnMention.startIndex = protoMention.GetStartIndex();
			returnMention.endIndex = protoMention.GetEndIndex();
			returnMention.headIndex = protoMention.GetHeadIndex();
			returnMention.mentionID = protoMention.GetMentionID();
			returnMention.originalRef = protoMention.GetOriginalRef();
			returnMention.goldCorefClusterID = protoMention.GetGoldCorefClusterID();
			returnMention.corefClusterID = protoMention.GetCorefClusterID();
			returnMention.mentionNum = protoMention.GetMentionNum();
			returnMention.sentNum = protoMention.GetSentNum();
			returnMention.utter = protoMention.GetUtter();
			returnMention.paragraph = protoMention.GetParagraph();
			returnMention.isSubject = protoMention.GetIsSubject();
			returnMention.isDirectObject = protoMention.GetIsDirectObject();
			returnMention.isIndirectObject = protoMention.GetIsIndirectObject();
			returnMention.isPrepositionObject = protoMention.GetIsPrepositionObject();
			returnMention.hasTwin = protoMention.GetHasTwin();
			returnMention.generic = protoMention.GetGeneric();
			returnMention.isSingleton = protoMention.GetIsSingleton();
			// handle the sets of Strings
			if (protoMention.GetDependentsCount() != 0)
			{
				returnMention.dependents = new HashSet<string>();
				Sharpen.Collections.AddAll(returnMention.dependents, protoMention.GetDependentsList());
			}
			if (protoMention.GetPreprocessedTermsCount() != 0)
			{
				returnMention.preprocessedTerms = new List<string>();
				Sharpen.Collections.AddAll(returnMention.preprocessedTerms, protoMention.GetPreprocessedTermsList());
			}
			return returnMention;
		}

		private SpeakerInfo FromProto(CoreNLPProtos.SpeakerInfo speakerInfo)
		{
			string speakerName = speakerInfo.GetSpeakerName();
			return new SpeakerInfo(speakerName);
		}

		/// <summary>Create an internal Timex object from the serialized protocol buffer.</summary>
		/// <param name="proto">The serialized protocol buffer to read from.</param>
		/// <returns>A timex, with as much information filled in as was gleaned from the protocol buffer.</returns>
		private Timex FromProto(CoreNLPProtos.Timex proto)
		{
			return new Timex(proto.HasType() ? proto.GetType() : null, proto.HasValue() ? proto.GetValue() : null, proto.HasAltValue() ? proto.GetAltValue() : null, proto.HasTid() ? proto.GetTid() : null, proto.HasText() ? proto.GetText() : null, proto.
				HasBeginPoint() ? proto.GetBeginPoint() : -1, proto.HasEndPoint() ? proto.GetEndPoint() : -1);
		}

		/// <summary>Read a entity mention from its serialized form.</summary>
		/// <remarks>
		/// Read a entity mention from its serialized form. Requires the containing sentence to be
		/// passed in along with the protocol buffer.
		/// </remarks>
		/// <param name="proto">The serialized entity mention.</param>
		/// <param name="sentence">The sentence this mention is attached to.</param>
		/// <returns>The entity mention corresponding to the serialized object.</returns>
		private EntityMention FromProto(CoreNLPProtos.Entity proto, ICoreMap sentence)
		{
			EntityMention rtn = new EntityMention(proto.HasObjectID() ? proto.GetObjectID() : null, sentence, proto.HasHeadStart() ? new Span(proto.GetHeadStart(), proto.GetHeadEnd()) : null, proto.HasHeadEnd() ? new Span(proto.GetExtentStart(), proto.GetExtentEnd
				()) : null, proto.HasType() ? proto.GetType() : null, proto.HasSubtype() ? proto.GetSubtype() : null, proto.HasMentionType() ? proto.GetMentionType() : null);
			if (proto.HasNormalizedName())
			{
				rtn.SetNormalizedName(proto.GetNormalizedName());
			}
			if (proto.HasHeadTokenIndex())
			{
				rtn.SetHeadTokenPosition(proto.GetHeadTokenIndex());
			}
			if (proto.HasCorefID())
			{
				rtn.SetCorefID(proto.GetCorefID());
			}
			return rtn;
		}

		/// <summary>Read a relation mention from its serialized form.</summary>
		/// <remarks>
		/// Read a relation mention from its serialized form. Requires the containing sentence to be
		/// passed in along with the protocol buffer.
		/// </remarks>
		/// <param name="proto">The serialized relation mention.</param>
		/// <param name="sentence">The sentence this mention is attached to.</param>
		/// <returns>The relation mention corresponding to the serialized object.</returns>
		private RelationMention FromProto(CoreNLPProtos.Relation proto, ICoreMap sentence)
		{
			IList<ExtractionObject> args = proto.GetArgList().Stream().Map(null).Collect(Collectors.ToList());
			RelationMention rtn = new RelationMention(proto.HasObjectID() ? proto.GetObjectID() : null, sentence, proto.HasExtentStart() ? new Span(proto.GetExtentStart(), proto.GetExtentEnd()) : null, proto.HasType() ? proto.GetType() : null, proto.HasSubtype
				() ? proto.GetSubtype() : null, args);
			if (proto.HasSignature())
			{
				rtn.SetSignature(proto.GetSignature());
			}
			if (proto.GetArgNameCount() > 0 || proto.GetArgCount() == 0)
			{
				rtn.SetArgNames(proto.GetArgNameList());
			}
			return rtn;
		}

		/// <summary>Convert a quote object to a protocol buffer.</summary>
		private static Annotation FromProto(CoreNLPProtos.Quote quote, IList<CoreLabel> tokens)
		{
			IList<CoreLabel> quotedTokens = null;
			// note[gabor]: This works, but apparently isn't the behavior of the quote annotator?
			//    if (quote.hasTokenBegin() && quote.hasTokenEnd() && quote.getTokenBegin() >= 0 && quote.getTokenEnd() >= 0) {
			//      quotedTokens = tokens.subList(quote.getTokenBegin(), quote.getTokenEnd());
			//    }
			Annotation ann = QuoteAnnotator.MakeQuote(quote.HasText() ? quote.GetText() : null, quote.HasBegin() ? quote.GetBegin() : -1, quote.HasEnd() ? quote.GetEnd() : -1, quotedTokens, quote.HasTokenBegin() ? quote.GetTokenBegin() : -1, quote.HasSentenceBegin
				() ? quote.GetSentenceBegin() : -1, quote.HasSentenceEnd() ? quote.GetSentenceEnd() : -1, quote.HasDocid() ? quote.GetDocid() : null);
			if (quote.HasIndex())
			{
				ann.Set(typeof(CoreAnnotations.QuotationIndexAnnotation), quote.GetIndex());
			}
			if (quote.HasTokenBegin())
			{
				ann.Set(typeof(CoreAnnotations.TokenBeginAnnotation), quote.GetTokenBegin());
			}
			if (quote.HasTokenEnd())
			{
				ann.Set(typeof(CoreAnnotations.TokenEndAnnotation), quote.GetTokenEnd());
			}
			if (quote.HasAuthor())
			{
				ann.Set(typeof(CoreAnnotations.AuthorAnnotation), quote.GetAuthor());
			}
			// quote attribution stuff
			if (quote.HasMention())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.MentionAnnotation), quote.GetMention());
			}
			if (quote.HasMentionBegin())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation), quote.GetMentionBegin());
			}
			if (quote.HasMentionEnd())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.MentionEndAnnotation), quote.GetMentionEnd());
			}
			if (quote.HasMentionType())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation), quote.GetMentionType());
			}
			if (quote.HasMentionSieve())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation), quote.GetMentionSieve());
			}
			if (quote.HasSpeaker())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.SpeakerAnnotation), quote.GetSpeaker());
			}
			if (quote.HasSpeakerSieve())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), quote.GetSpeakerSieve());
			}
			if (quote.HasCanonicalMention())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation), quote.GetCanonicalMention());
			}
			if (quote.HasCanonicalMentionBegin())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.CanonicalMentionBeginAnnotation), quote.GetCanonicalMentionBegin());
			}
			if (quote.HasCanonicalMentionEnd())
			{
				ann.Set(typeof(QuoteAttributionAnnotator.CanonicalMentionEndAnnotation), quote.GetCanonicalMentionEnd());
			}
			return ann;
		}

		/// <summary>Convert a quote object to a protocol buffer.</summary>
		private ICoreMap FromProto(CoreNLPProtos.NERMention mention)
		{
			ICoreMap map = new ArrayCoreMap(12);
			if (mention.HasSentenceIndex())
			{
				map.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), mention.GetSentenceIndex());
			}
			if (mention.HasTokenStartInSentenceInclusive())
			{
				map.Set(typeof(CoreAnnotations.TokenBeginAnnotation), mention.GetTokenStartInSentenceInclusive());
			}
			if (mention.HasTokenEndInSentenceExclusive())
			{
				map.Set(typeof(CoreAnnotations.TokenEndAnnotation), mention.GetTokenEndInSentenceExclusive());
			}
			if (mention.HasNer())
			{
				map.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), mention.GetNer());
			}
			if (mention.HasNormalizedNER())
			{
				map.Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), mention.GetNormalizedNER());
			}
			if (mention.HasEntityType())
			{
				map.Set(typeof(CoreAnnotations.EntityTypeAnnotation), mention.GetEntityType());
			}
			if (mention.HasTimex())
			{
				map.Set(typeof(TimeAnnotations.TimexAnnotation), FromProto(mention.GetTimex()));
			}
			if (mention.HasWikipediaEntity())
			{
				map.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), mention.GetWikipediaEntity());
			}
			if (mention.HasGender())
			{
				map.Set(typeof(CoreAnnotations.GenderAnnotation), mention.GetGender());
			}
			if (mention.HasEntityMentionIndex())
			{
				map.Set(typeof(CoreAnnotations.EntityMentionIndexAnnotation), mention.GetEntityMentionIndex());
			}
			if (mention.HasCanonicalEntityMentionIndex())
			{
				map.Set(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation), mention.GetCanonicalEntityMentionIndex());
			}
			if (mention.HasEntityMentionText())
			{
				map.Set(typeof(CoreAnnotations.TextAnnotation), mention.GetEntityMentionText());
			}
			return map;
		}

		/// <summary>Read a section coremap from its serialized form.</summary>
		/// <remarks>
		/// Read a section coremap from its serialized form. Requires the containing sentence to be
		/// passed in along with the protocol buffer.
		/// </remarks>
		/// <param name="section">The serialized section coremap</param>
		/// <returns>The relation mention corresponding to the serialized object.</returns>
		private ICoreMap FromProto(CoreNLPProtos.Section section, IList<ICoreMap> annotationSentences)
		{
			ICoreMap map = new ArrayCoreMap();
			map.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), section.GetCharBegin());
			map.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), section.GetCharEnd());
			if (section.HasAuthor())
			{
				map.Set(typeof(CoreAnnotations.AuthorAnnotation), section.GetAuthor());
			}
			if (section.HasDatetime())
			{
				map.Set(typeof(CoreAnnotations.SectionDateAnnotation), section.GetDatetime());
			}
			// go through the list of sentences and add them to this section's sentence list
			List<ICoreMap> sentencesList = new List<ICoreMap>();
			foreach (int sentenceIndex in section.GetSentenceIndexesList())
			{
				sentencesList.Add(annotationSentences[sentenceIndex]);
			}
			map.Set(typeof(CoreAnnotations.SentencesAnnotation), sentencesList);
			// go through the list of quotes and rebuild the quotes
			map.Set(typeof(CoreAnnotations.QuotesAnnotation), new List<ICoreMap>());
			foreach (CoreNLPProtos.Quote quote in section.GetQuotesList())
			{
				int quoteCharStart = quote.GetBegin();
				int quoteCharEnd = quote.GetEnd();
				string quoteAuthor = null;
				if (quote.HasAuthor())
				{
					quoteAuthor = quote.GetAuthor();
				}
				ICoreMap quoteCoreMap = new ArrayCoreMap();
				quoteCoreMap.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), quoteCharStart);
				quoteCoreMap.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), quoteCharEnd);
				quoteCoreMap.Set(typeof(CoreAnnotations.AuthorAnnotation), quoteAuthor);
				map.Get(typeof(CoreAnnotations.QuotesAnnotation)).Add(quoteCoreMap);
			}
			// if there is an author character start, add it
			if (section.HasAuthorCharBegin())
			{
				map.Set(typeof(CoreAnnotations.SectionAuthorCharacterOffsetBeginAnnotation), section.GetAuthorCharBegin());
			}
			if (section.HasAuthorCharEnd())
			{
				map.Set(typeof(CoreAnnotations.SectionAuthorCharacterOffsetEndAnnotation), section.GetAuthorCharEnd());
			}
			// add the original xml tag
			map.Set(typeof(CoreAnnotations.SectionTagAnnotation), FromProto(section.GetXmlTag()));
			return map;
		}

		/// <summary>
		/// Recover the
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TextAnnotation"/>
		/// field of a sentence
		/// from the tokens. This is useful if the text was not set in the protocol buffer, and therefore
		/// needs to be reconstructed from tokens.
		/// </summary>
		/// <param name="tokens">The list of tokens representing this sentence.</param>
		/// <returns>The original text of the sentence.</returns>
		protected internal virtual string RecoverOriginalText(IList<CoreLabel> tokens, CoreNLPProtos.Sentence sentence)
		{
			StringBuilder text = new StringBuilder();
			CoreLabel last = null;
			if (tokens.Count > 0)
			{
				CoreLabel token = tokens[0];
				if (token.OriginalText() != null)
				{
					text.Append(token.OriginalText());
				}
				else
				{
					text.Append(token.Word());
				}
				last = tokens[0];
			}
			for (int i = 1; i < tokens.Count; ++i)
			{
				CoreLabel token = tokens[i];
				if (token.Before() != null)
				{
					text.Append(token.Before());
					System.Diagnostics.Debug.Assert(last != null);
					int missingWhitespace = (token.BeginPosition() - last.EndPosition()) - token.Before().Length;
					while (missingWhitespace > 0)
					{
						text.Append(' ');
						missingWhitespace -= 1;
					}
				}
				if (token.OriginalText() != null)
				{
					text.Append(token.OriginalText());
				}
				else
				{
					text.Append(token.Word());
				}
				last = token;
			}
			return text.ToString();
		}
	}
}

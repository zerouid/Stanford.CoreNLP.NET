using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Simple
{
	/// <summary>A representation of a single Sentence.</summary>
	/// <remarks>
	/// A representation of a single Sentence.
	/// Although it is possible to create a sentence directly from text, it is advisable to
	/// create a document instead and operate on the document directly.
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class Sentence
	{
		/// <summary>A Properties object for creating a document from a single sentence.</summary>
		/// <remarks>
		/// A Properties object for creating a document from a single sentence. Used in the constructor
		/// <see cref="Sentence(string)"/>
		/// 
		/// </remarks>
		internal static Properties SingleSentenceDocument = PropertiesUtils.AsProperties("language", "english", "ssplit.isOneSentence", "true", "tokenize.class", "PTBTokenizer", "tokenize.language", "en", "mention.type", "dep", "coref.mode", "statistical"
			, "coref.md.type", "dep");

		/// <summary>A Properties object for creating a document from a single tokenized sentence.</summary>
		private static Properties SingleSentenceTokenizedDocument = PropertiesUtils.AsProperties("language", "english", "ssplit.isOneSentence", "true", "tokenize.class", "WhitespaceTokenizer", "tokenize.language", "en", "tokenize.whitespace", "true"
			, "mention.type", "dep", "coref.mode", "statistical", "coref.md.type", "dep");

		/// <summary>The protobuf representation of a Sentence.</summary>
		/// <remarks>
		/// The protobuf representation of a Sentence.
		/// Note that this does not necessarily have up to date token information.
		/// </remarks>
		private readonly CoreNLPProtos.Sentence.Builder impl;

		/// <summary>The protobuf representation of the tokens of a sentence.</summary>
		/// <remarks>The protobuf representation of the tokens of a sentence. This has up-to-date information on the tokens</remarks>
		private readonly IList<CoreNLPProtos.Token.Builder> tokensBuilders;

		/// <summary>The document this sentence is derived from</summary>
		public readonly Document document;

		/// <summary>The default properties to use for annotators.</summary>
		private readonly Properties defaultProps;

		/// <summary>The function to use to create a new document.</summary>
		/// <remarks>The function to use to create a new document. This is used for the cased() and caseless() functions.</remarks>
		private readonly IBiFunction<Properties, string, Document> docFn;

		/// <summary>Create a new sentence, using the specified properties as the default properties.</summary>
		/// <param name="doc">The document to link this sentence to.</param>
		/// <param name="props">The properties to use for tokenizing the sentence.</param>
		protected internal Sentence(Document doc, Properties props)
		{
			// Use the new coref
			// Use the new coref
			// redundant?
			// Set document
			this.document = doc;
			// Set sentence
			if (props.Contains("ssplit.isOneSentence"))
			{
				this.impl = this.document.Sentence(0, props).impl;
			}
			else
			{
				Properties modProps = new Properties(props);
				modProps.SetProperty("ssplit.isOneSentence", "true");
				this.impl = this.document.Sentence(0, modProps).impl;
			}
			// Set tokens
			this.tokensBuilders = document.Sentence(0).tokensBuilders;
			// Asserts
			System.Diagnostics.Debug.Assert((this.document.Sentence(0).impl == this.impl));
			System.Diagnostics.Debug.Assert((this.document.Sentence(0).tokensBuilders == this.tokensBuilders));
			// Set the default properties
			if (props == SingleSentenceTokenizedDocument)
			{
				this.defaultProps = SingleSentenceDocument;
			}
			else
			{
				// no longer care about tokenization
				this.defaultProps = props;
			}
			this.docFn = null;
		}

		/// <summary>Create a new sentence from some text, and some properties.</summary>
		/// <param name="text">The text of the sentence.</param>
		/// <param name="props">The properties to use for the annotators.</param>
		public Sentence(string text, Properties props)
			: this(new Document(props, text), props)
		{
		}

		/// <summary>Create a new sentence from the given text, assuming the entire text is just one sentence.</summary>
		/// <param name="text">The text of the sentence.</param>
		public Sentence(string text)
			: this(text, SingleSentenceDocument)
		{
		}

		/// <summary>The actual implementation of a tokenized sentence constructor</summary>
		protected internal Sentence(IFunction<string, Document> doc, IList<string> tokens, Properties props)
			: this(doc.Apply(StringUtils.Join(tokens.Stream().Map(null), " ")), props)
		{
			/* some random character */
			// Clean up whitespace
			for (int i = 0; i < impl.GetTokenCount(); ++i)
			{
				this.impl.GetTokenBuilder(i).SetWord(this.impl.GetTokenBuilder(i).GetWord().Replace('ߝ', ' '));
				this.impl.GetTokenBuilder(i).SetValue(this.impl.GetTokenBuilder(i).GetValue().Replace('ߝ', ' '));
				this.tokensBuilders[i].SetWord(this.tokensBuilders[i].GetWord().Replace('ߝ', ' '));
				this.tokensBuilders[i].SetValue(this.tokensBuilders[i].GetValue().Replace('ߝ', ' '));
			}
		}

		/// <summary>Create a new sentence from the given tokenized text, assuming the entire text is just one sentence.</summary>
		/// <remarks>
		/// Create a new sentence from the given tokenized text, assuming the entire text is just one sentence.
		/// WARNING: This method may in rare cases (mostly when tokens themselves have whitespace in them)
		/// produce strange results; it's a bit of a hack around the default tokenizer.
		/// </remarks>
		/// <param name="tokens">The text of the sentence.</param>
		public Sentence(IList<string> tokens)
			: this(null, tokens, SingleSentenceTokenizedDocument)
		{
		}

		/// <summary>Create a sentence from a saved protocol buffer.</summary>
		protected internal Sentence(IBiFunction<Properties, string, Document> docFn, CoreNLPProtos.Sentence proto, Properties props)
		{
			this.impl = ((CoreNLPProtos.Sentence.Builder)proto.ToBuilder());
			// Set tokens
			tokensBuilders = new List<CoreNLPProtos.Token.Builder>(this.impl.GetTokenCount());
			for (int i = 0; i < this.impl.GetTokenCount(); ++i)
			{
				tokensBuilders.Add(((CoreNLPProtos.Token.Builder)this.impl.GetToken(i).ToBuilder()));
			}
			// Initialize document
			this.document = docFn.Apply(props, proto.GetText());
			this.document.ForceSentences(Java.Util.Collections.SingletonList(this));
			// Asserts
			System.Diagnostics.Debug.Assert((this.document.Sentence(0).impl == this.impl));
			System.Diagnostics.Debug.Assert((this.document.Sentence(0).tokensBuilders == this.tokensBuilders));
			// Set default props
			this.defaultProps = props;
			this.docFn = docFn;
		}

		/// <summary>Create a sentence from a saved protocol buffer.</summary>
		public Sentence(CoreNLPProtos.Sentence proto)
			: this(null, proto, SingleSentenceDocument)
		{
		}

		/// <summary>Helper for creating a sentence from a document at a given index</summary>
		protected internal Sentence(Document doc, int sentenceIndex)
		{
			this.document = doc;
			this.impl = doc.Sentence(sentenceIndex).impl;
			// Set tokens
			this.tokensBuilders = doc.Sentence(sentenceIndex).tokensBuilders;
			// Asserts
			System.Diagnostics.Debug.Assert((this.document.Sentence(sentenceIndex).impl == this.impl));
			System.Diagnostics.Debug.Assert((this.document.Sentence(sentenceIndex).tokensBuilders == this.tokensBuilders));
			// Set default props
			this.defaultProps = Document.EmptyProps;
			this.docFn = doc.Sentence(sentenceIndex).docFn;
		}

		/// <summary>
		/// The canonical constructor of a sentence from a
		/// <see cref="Document"/>
		/// .
		/// </summary>
		/// <param name="doc">The document to link this sentence to.</param>
		/// <param name="proto">The sentence implementation to use for this sentence.</param>
		protected internal Sentence(Document doc, CoreNLPProtos.Sentence.Builder proto, Properties defaultProps)
		{
			this.document = doc;
			this.impl = proto;
			this.defaultProps = defaultProps;
			// Set tokens
			// This is the _only_ place we are allowed to construct tokens builders
			tokensBuilders = new List<CoreNLPProtos.Token.Builder>(this.impl.GetTokenCount());
			for (int i = 0; i < this.impl.GetTokenCount(); ++i)
			{
				tokensBuilders.Add(((CoreNLPProtos.Token.Builder)this.impl.GetToken(i).ToBuilder()));
			}
			this.docFn = null;
		}

		/// <summary>Also sets the the text of the sentence.</summary>
		/// <remarks>
		/// Also sets the the text of the sentence. Used by
		/// <see cref="Document"/>
		/// internally
		/// </remarks>
		/// <param name="doc">The document to link this sentence to.</param>
		/// <param name="proto">The sentence implementation to use for this sentence.</param>
		/// <param name="text">The text for the sentence</param>
		/// <param name="defaultProps">The default properties to use when annotating this sentence.</param>
		internal Sentence(Document doc, CoreNLPProtos.Sentence.Builder proto, string text, Properties defaultProps)
			: this(doc, proto, defaultProps)
		{
			this.impl.SetText(text);
		}

		/// <summary>Helper for creating a sentence from a document and a CoreMap representation</summary>
		protected internal Sentence(Document doc, ICoreMap sentence)
		{
			this.document = doc;
			System.Diagnostics.Debug.Assert(!doc.Sentences().IsEmpty());
			this.impl = doc.Sentence(0).impl;
			this.tokensBuilders = doc.Sentence(0).tokensBuilders;
			this.defaultProps = Document.EmptyProps;
			this.docFn = null;
		}

		/// <summary>Convert a CoreMap into a simple Sentence object.</summary>
		/// <remarks>
		/// Convert a CoreMap into a simple Sentence object.
		/// Note that this is a copy operation -- the implementing CoreMap will not be updated, and all of its
		/// contents are copied over to the protocol buffer format backing the
		/// <see cref="Sentence"/>
		/// object.
		/// </remarks>
		/// <param name="sentence">The CoreMap representation of the sentence.</param>
		public Sentence(ICoreMap sentence)
			: this(new Document(new _Annotation_247(sentence, sentence.Get(typeof(CoreAnnotations.TextAnnotation)))), sentence)
		{
		}

		private sealed class _Annotation_247 : Annotation
		{
			public _Annotation_247(ICoreMap sentence, string baseArg1)
				: base(baseArg1)
			{
				this.sentence = sentence;
				{
					this.Set(typeof(CoreAnnotations.SentencesAnnotation), Java.Util.Collections.SingletonList(sentence));
					if (sentence.ContainsKey(typeof(CoreAnnotations.DocIDAnnotation)))
					{
						this.Set(typeof(CoreAnnotations.DocIDAnnotation), sentence.Get(typeof(CoreAnnotations.DocIDAnnotation)));
					}
				}
			}

			private readonly ICoreMap sentence;
		}

		/// <summary>Convert a sentence fragment (i.e., entailed sentence) into a simple sentence object.</summary>
		/// <remarks>
		/// Convert a sentence fragment (i.e., entailed sentence) into a simple sentence object.
		/// Like
		/// <see cref="Sentence(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// , this copies the information in the fragment into the underlying
		/// protobuf backed format.
		/// </remarks>
		/// <param name="sentence">The sentence fragment to convert.</param>
		public Sentence(SentenceFragment sentence)
			: this(new _ArrayCoreMap_263(sentence, 32))
		{
		}

		private sealed class _ArrayCoreMap_263 : ArrayCoreMap
		{
			public _ArrayCoreMap_263(SentenceFragment sentence, int baseArg1)
				: base(baseArg1)
			{
				this.sentence = sentence;
				{
					this.Set(typeof(CoreAnnotations.TokensAnnotation), sentence.words);
					this.Set(typeof(CoreAnnotations.TextAnnotation), StringUtils.Join(sentence.words.Stream().Map(null), " "));
					if (sentence.words.IsEmpty())
					{
						this.Set(typeof(CoreAnnotations.TokenBeginAnnotation), 0);
						this.Set(typeof(CoreAnnotations.TokenEndAnnotation), 0);
					}
					else
					{
						this.Set(typeof(CoreAnnotations.TokenBeginAnnotation), sentence.words[0].Get(typeof(CoreAnnotations.IndexAnnotation)));
						this.Set(typeof(CoreAnnotations.TokenEndAnnotation), sentence.words[sentence.words.Count - 1].Get(typeof(CoreAnnotations.IndexAnnotation)) + 1);
					}
					this.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), sentence.parseTree);
					this.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), sentence.parseTree);
					this.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), sentence.parseTree);
				}
			}

			private readonly SentenceFragment sentence;
		}

		/// <summary>Make this sentence caseless.</summary>
		/// <remarks>
		/// Make this sentence caseless. That is, from now on, run the caseless models
		/// on the sentence by default rather than the standard CoreNLP models.
		/// </remarks>
		/// <returns>A new sentence with the default properties swapped out.</returns>
		public virtual Edu.Stanford.Nlp.Simple.Sentence Caseless()
		{
			return new Edu.Stanford.Nlp.Simple.Sentence(this.docFn, ((CoreNLPProtos.Sentence)impl.Build()), Document.CaselessProps);
		}

		/// <summary>Make this sentence case sensitive.</summary>
		/// <remarks>
		/// Make this sentence case sensitive.
		/// A sentence is case sensitive by default; this only has an effect if you have previously
		/// called
		/// <see cref="Caseless()"/>
		/// .
		/// </remarks>
		/// <returns>A new sentence with the default properties swapped out.</returns>
		public virtual Edu.Stanford.Nlp.Simple.Sentence Cased()
		{
			return new Edu.Stanford.Nlp.Simple.Sentence(this.docFn, ((CoreNLPProtos.Sentence)impl.Build()), Document.EmptyProps);
		}

		/// <summary>Serialize the given sentence (but not the associated document!) into a Protocol Buffer.</summary>
		/// <returns>The Protocol Buffer representing this sentence.</returns>
		public virtual CoreNLPProtos.Sentence Serialize()
		{
			lock (impl)
			{
				this.impl.ClearToken();
				foreach (CoreNLPProtos.Token.Builder token in this.tokensBuilders)
				{
					this.impl.AddToken(((CoreNLPProtos.Token)token.Build()));
				}
				return ((CoreNLPProtos.Sentence)impl.Build());
			}
		}

		/// <summary>Write this sentence to an output stream.</summary>
		/// <remarks>
		/// Write this sentence to an output stream.
		/// Internally, this stores the sentence as a protocol buffer, and saves that buffer to the output stream.
		/// This method does not close the stream after writing.
		/// </remarks>
		/// <param name="out">The output stream to write to. The stream is not closed after the method returns.</param>
		/// <exception cref="System.IO.IOException">Thrown from the underlying write() implementation.</exception>
		public virtual void Serialize(OutputStream @out)
		{
			Serialize().WriteDelimitedTo(@out);
			@out.Flush();
		}

		/// <summary>Read a sentence from an input stream.</summary>
		/// <remarks>
		/// Read a sentence from an input stream.
		/// This does not close the input stream.
		/// </remarks>
		/// <param name="in">The input stream to deserialize from.</param>
		/// <returns>The next sentence encoded in the input stream.</returns>
		/// <exception cref="System.IO.IOException">Thrown by the underlying parse() implementation.</exception>
		/// <seealso cref="Document.Serialize(Java.IO.OutputStream)"/>
		public static Edu.Stanford.Nlp.Simple.Sentence Deserialize(InputStream @in)
		{
			return new Edu.Stanford.Nlp.Simple.Sentence(CoreNLPProtos.Sentence.ParseDelimitedFrom(@in));
		}

		/// <summary>Return a class that can perform common algorithms on this sentence.</summary>
		public virtual SentenceAlgorithms Algorithms()
		{
			return new SentenceAlgorithms(this);
		}

		/// <summary>
		/// The raw text of the sentence, as input by, e.g.,
		/// <see cref="Sentence(string)"/>
		/// .
		/// </summary>
		public virtual string Text()
		{
			lock (impl)
			{
				return impl.GetText();
			}
		}

		//
		// SET AXIOMATICALLY
		//
		/// <summary>The index of the sentence within the document.</summary>
		public virtual int SentenceIndex()
		{
			lock (impl)
			{
				return impl.GetSentenceIndex();
			}
		}

		/// <summary>THe token offset of the sentence within the document.</summary>
		public virtual int SentenceTokenOffsetBegin()
		{
			lock (impl)
			{
				return impl.GetTokenOffsetBegin();
			}
		}

		/// <summary>The token offset of the end of this sentence within the document.</summary>
		public virtual int SentenceTokenOffsetEnd()
		{
			lock (impl)
			{
				return impl.GetTokenOffsetEnd();
			}
		}

		//
		// SET BY TOKENIZER
		//
		/// <summary>
		/// The words of the sentence, as per
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel.Word()"/>
		/// .
		/// </summary>
		public virtual IList<string> Words()
		{
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <summary>The word at the given index of the sentence.</summary>
		/// <remarks>The word at the given index of the sentence. @see Sentence#words()</remarks>
		public virtual string Word(int index)
		{
			return Words()[index];
		}

		/// <summary>
		/// The original (unprocessed) words of the sentence, as per
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel.OriginalText()"/>
		/// .
		/// </summary>
		public virtual IList<string> OriginalTexts()
		{
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <summary>The original word at the given index.</summary>
		/// <remarks>The original word at the given index. @see Sentence#originalTexts()</remarks>
		public virtual string OriginalText(int index)
		{
			return OriginalTexts()[index];
		}

		/// <summary>
		/// The character offset of each token in the sentence, as per
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel.BeginPosition()"/>
		/// .
		/// </summary>
		public virtual IList<int> CharacterOffsetBegin()
		{
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <summary>The character offset of the given index in the sentence.</summary>
		/// <remarks>The character offset of the given index in the sentence. @see Sentence#characterOffsetBegin().</remarks>
		public virtual int CharacterOffsetBegin(int index)
		{
			return CharacterOffsetBegin()[index];
		}

		/// <summary>
		/// The end character offset of each token in the sentence, as per
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel.EndPosition()"/>
		/// .
		/// </summary>
		public virtual IList<int> CharacterOffsetEnd()
		{
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <summary>The end character offset of the given index in the sentence.</summary>
		/// <remarks>The end character offset of the given index in the sentence. @see Sentence#characterOffsetEnd().</remarks>
		public virtual int CharacterOffsetEnd(int index)
		{
			return CharacterOffsetEnd()[index];
		}

		/// <summary>The whitespace before each token in the sentence.</summary>
		/// <remarks>
		/// The whitespace before each token in the sentence. This will match
		/// <see cref="After()"/>
		/// of the previous token.
		/// </remarks>
		public virtual IList<string> Before()
		{
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <summary>The whitespace before this token in the sentence.</summary>
		/// <remarks>
		/// The whitespace before this token in the sentence. This will match
		/// <see cref="After()"/>
		/// of the previous token.
		/// </remarks>
		public virtual string Before(int index)
		{
			return Before()[index];
		}

		/// <summary>The whitespace after each token in the sentence.</summary>
		/// <remarks>
		/// The whitespace after each token in the sentence. This will match
		/// <see cref="Before()"/>
		/// of the next token.
		/// </remarks>
		public virtual IList<string> After()
		{
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <summary>The whitespace after this token in the sentence.</summary>
		/// <remarks>
		/// The whitespace after this token in the sentence. This will match
		/// <see cref="Before()"/>
		/// of the next token.
		/// </remarks>
		public virtual string After(int index)
		{
			return After()[index];
		}

		/// <summary>The tokens in this sentence.</summary>
		/// <remarks>The tokens in this sentence. Each token class is just a helper for the methods in this class.</remarks>
		public virtual IList<Token> Tokens()
		{
			List<Token> tokens = new List<Token>(this.Length());
			for (int i = 0; i < Length(); ++i)
			{
				tokens.Add(new Token(this, i));
			}
			return tokens;
		}

		//
		// SET BY ANNOTATORS
		//
		/// <summary>The part of speech tags of the sentence.</summary>
		/// <param name="props">
		/// The properties to use for the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.POSTaggerAnnotator"/>
		/// .
		/// </param>
		/// <returns>A list of part of speech tags, one for each token in the sentence.</returns>
		public virtual IList<string> PosTags(Properties props)
		{
			document.RunPOS(props);
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <seealso cref="PosTags(Java.Util.Properties)"></seealso>
		public virtual IList<string> PosTags()
		{
			return PosTags(this.defaultProps);
		}

		/// <seealso cref="PosTags(Java.Util.Properties)"></seealso>
		public virtual string PosTag(int index)
		{
			return PosTags()[index];
		}

		/// <summary>The lemmas of the sentence.</summary>
		/// <param name="props">
		/// The properties to use for the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.MorphaAnnotator"/>
		/// .
		/// </param>
		/// <returns>A list of lemmatized words, one for each token in the sentence.</returns>
		public virtual IList<string> Lemmas(Properties props)
		{
			document.RunLemma(props);
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <seealso cref="Lemmas(Java.Util.Properties)"></seealso>
		public virtual IList<string> Lemmas()
		{
			return Lemmas(this.defaultProps);
		}

		/// <seealso cref="Lemmas(Java.Util.Properties)"></seealso>
		public virtual string Lemma(int index)
		{
			return Lemmas()[index];
		}

		/// <summary>The named entity tags of the sentence.</summary>
		/// <param name="props">
		/// The properties to use for the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.NERCombinerAnnotator"/>
		/// .
		/// </param>
		/// <returns>A list of named entity tags, one for each token in the sentence.</returns>
		public virtual IList<string> NerTags(Properties props)
		{
			document.RunNER(props);
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <seealso cref="NerTags(Java.Util.Properties)"></seealso>
		public virtual IList<string> NerTags()
		{
			return NerTags(this.defaultProps);
		}

		/// <summary>Run RegexNER over this sentence.</summary>
		/// <remarks>
		/// Run RegexNER over this sentence. Note that this is an in place operation, and simply
		/// updates the NER tags.
		/// Therefore, every time this function is called, it re-runs the annotator!
		/// </remarks>
		/// <param name="mappingFile">The regexner mapping file.</param>
		/// <param name="ignorecase">If true, run a caseless match on the regexner file.</param>
		public virtual void Regexner(string mappingFile, bool ignorecase)
		{
			Properties props = new Properties();
			foreach (object prop in this.defaultProps.Keys)
			{
				props.SetProperty(prop.ToString(), this.defaultProps.GetProperty(prop.ToString()));
			}
			props.SetProperty(AnnotatorConstants.StanfordRegexner + ".mapping", mappingFile);
			props.SetProperty(AnnotatorConstants.StanfordRegexner + ".ignorecase", bool.ToString(ignorecase));
			this.document.RunRegexner(props);
		}

		/// <seealso cref="NerTags(Java.Util.Properties)"></seealso>
		public virtual string NerTag(int index)
		{
			return NerTags()[index];
		}

		/// <summary>Get all mentions of the given NER tag, as a list of surface forms.</summary>
		/// <param name="nerTag">The ner tag to search for, case sensitive.</param>
		/// <returns>
		/// A list of surface forms of the entities of this tag. This is using the
		/// <see cref="Word(int)"/>
		/// function.
		/// </returns>
		public virtual IList<string> Mentions(string nerTag)
		{
			IList<string> mentionsOfTag = new List<string>();
			StringBuilder lastMention = new StringBuilder();
			string lastTag = "O";
			for (int i = 0; i < Length(); ++i)
			{
				string ner = NerTag(i);
				if (ner.Equals(nerTag) && !lastTag.Equals(nerTag))
				{
					// case: beginning of span
					lastMention.Append(Word(i)).Append(' ');
				}
				else
				{
					if (ner.Equals(nerTag) && lastTag.Equals(nerTag))
					{
						// case: in span
						lastMention.Append(Word(i)).Append(' ');
					}
					else
					{
						if (!ner.Equals(nerTag) && lastTag.Equals(nerTag))
						{
							// case: end of span
							if (lastMention.Length > 0)
							{
								mentionsOfTag.Add(lastMention.ToString().Trim());
							}
							lastMention.Length = 0;
						}
					}
				}
				lastTag = ner;
			}
			if (lastMention.Length > 0)
			{
				mentionsOfTag.Add(lastMention.ToString().Trim());
			}
			return mentionsOfTag;
		}

		/// <summary>Get all mentions of any NER tag, as a list of surface forms.</summary>
		/// <returns>
		/// A list of surface forms of the entities in this sentence. This is using the
		/// <see cref="Word(int)"/>
		/// function.
		/// </returns>
		public virtual IList<string> Mentions()
		{
			IList<string> mentionsOfTag = new List<string>();
			StringBuilder lastMention = new StringBuilder();
			string lastTag = "O";
			for (int i = 0; i < Length(); ++i)
			{
				string ner = NerTag(i);
				if (!ner.Equals("O") && !lastTag.Equals(ner))
				{
					// case: beginning of span
					if (lastMention.Length > 0)
					{
						mentionsOfTag.Add(lastMention.ToString().Trim());
					}
					lastMention.Length = 0;
					lastMention.Append(Word(i)).Append(' ');
				}
				else
				{
					if (!ner.Equals("O") && lastTag.Equals(ner))
					{
						// case: in span
						lastMention.Append(Word(i)).Append(' ');
					}
					else
					{
						if (ner.Equals("O") && !lastTag.Equals("O"))
						{
							// case: end of span
							if (lastMention.Length > 0)
							{
								mentionsOfTag.Add(lastMention.ToString().Trim());
							}
							lastMention.Length = 0;
						}
					}
				}
				lastTag = ner;
			}
			if (lastMention.Length > 0)
			{
				mentionsOfTag.Add(lastMention.ToString().Trim());
			}
			return mentionsOfTag;
		}

		/// <summary>Returns the constituency parse of this sentence.</summary>
		/// <param name="props">The properties to use in the parser annotator.</param>
		/// <returns>A parse tree object.</returns>
		public virtual Tree Parse(Properties props)
		{
			document.RunParse(props);
			lock (document.serializer)
			{
				return document.serializer.FromProto(impl.GetParseTree());
			}
		}

		/// <seealso cref="Parse(Java.Util.Properties)"></seealso>
		public virtual Tree Parse()
		{
			return Parse(this.defaultProps);
		}

		/// <summary>An internal helper to get the dependency tree of the given type.</summary>
		private CoreNLPProtos.DependencyGraph Dependencies(SemanticGraphFactory.Mode mode)
		{
			switch (mode)
			{
				case SemanticGraphFactory.Mode.Basic:
				{
					return impl.GetBasicDependencies();
				}

				case SemanticGraphFactory.Mode.Enhanced:
				{
					return impl.GetEnhancedDependencies();
				}

				case SemanticGraphFactory.Mode.EnhancedPlusPlus:
				{
					return impl.GetEnhancedPlusPlusDependencies();
				}

				default:
				{
					throw new ArgumentException("Unsupported dependency type: " + mode);
				}
			}
		}

		/// <summary>Returns the governor of the given index, according to the passed dependency type.</summary>
		/// <remarks>
		/// Returns the governor of the given index, according to the passed dependency type.
		/// The root has index -1.
		/// </remarks>
		/// <param name="props">The properties to use in the parser annotator.</param>
		/// <param name="index">
		/// The index of the dependent word ZERO INDEXED. That is, the first word of the sentence
		/// is index 0, not 1 as it would be in the
		/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
		/// framework.
		/// </param>
		/// <param name="mode">The type of dependency to use (e.g., basic, collapsed, collapsed cc processed).</param>
		/// <returns>The index of the governor, if one exists. A value of -1 indicates the root node.</returns>
		public virtual Optional<int> Governor(Properties props, int index, SemanticGraphFactory.Mode mode)
		{
			document.RunDepparse(props);
			foreach (CoreNLPProtos.DependencyGraph.Edge edge in Dependencies(mode).GetEdgeList())
			{
				if (edge.GetTarget() - 1 == index)
				{
					return Optional.Of(edge.GetSource() - 1);
				}
			}
			foreach (int root in impl.GetBasicDependencies().GetRootList())
			{
				if (index == root - 1)
				{
					return Optional.Of(-1);
				}
			}
			return Optional.Empty();
		}

		/// <seealso cref="Governor(Java.Util.Properties, int, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual Optional<int> Governor(Properties props, int index)
		{
			return Governor(props, index, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <seealso cref="Governor(Java.Util.Properties, int, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual Optional<int> Governor(int index, SemanticGraphFactory.Mode mode)
		{
			return Governor(this.defaultProps, index, mode);
		}

		/// <seealso cref="Governor(Java.Util.Properties, int)"></seealso>
		public virtual Optional<int> Governor(int index)
		{
			return Governor(this.defaultProps, index);
		}

		/// <summary>Returns the governors of a sentence, according to the passed dependency type.</summary>
		/// <remarks>
		/// Returns the governors of a sentence, according to the passed dependency type.
		/// The resulting list is of the same size as the original sentence, with each element being either
		/// the governor (index), or empty if the node has no known governor.
		/// The root has index -1.
		/// </remarks>
		/// <param name="props">The properties to use in the parser annotator.</param>
		/// <param name="mode">The type of dependency to use (e.g., basic, collapsed, collapsed cc processed).</param>
		/// <returns>A list of the (optional) governors of each token in the sentence.</returns>
		public virtual IList<Optional<int>> Governors(Properties props, SemanticGraphFactory.Mode mode)
		{
			document.RunDepparse(props);
			IList<Optional<int>> governors = new List<Optional<int>>(this.Length());
			for (int i = 0; i < this.Length(); ++i)
			{
				governors.Add(Optional.Empty());
			}
			foreach (CoreNLPProtos.DependencyGraph.Edge edge in Dependencies(mode).GetEdgeList())
			{
				governors.Set(edge.GetTarget() - 1, Optional.Of(edge.GetSource() - 1));
			}
			foreach (int root in impl.GetBasicDependencies().GetRootList())
			{
				governors.Set(root - 1, Optional.Of(-1));
			}
			return governors;
		}

		/// <seealso cref="Governors(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual IList<Optional<int>> Governors(Properties props)
		{
			return Governors(props, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <seealso cref="Governors(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual IList<Optional<int>> Governors(SemanticGraphFactory.Mode mode)
		{
			return Governors(this.defaultProps, mode);
		}

		/// <seealso cref="Governors(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual IList<Optional<int>> Governors()
		{
			return Governors(this.defaultProps, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <summary>Returns the incoming dependency label to a particular index, according to the Basic Dependencies.</summary>
		/// <param name="props">The properties to use in the parser annotator.</param>
		/// <param name="index">
		/// The index of the dependent word ZERO INDEXED. That is, the first word of the sentence
		/// is index 0, not 1 as it would be in the
		/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
		/// framework.
		/// </param>
		/// <param name="mode">The type of dependency to use (e.g., basic, collapsed, collapsed cc processed).</param>
		/// <returns>The incoming dependency label, if it exists.</returns>
		public virtual Optional<string> IncomingDependencyLabel(Properties props, int index, SemanticGraphFactory.Mode mode)
		{
			document.RunDepparse(props);
			foreach (CoreNLPProtos.DependencyGraph.Edge edge in Dependencies(mode).GetEdgeList())
			{
				if (edge.GetTarget() - 1 == index)
				{
					return Optional.Of(edge.GetDep());
				}
			}
			foreach (int root in impl.GetBasicDependencies().GetRootList())
			{
				if (index == root - 1)
				{
					return Optional.Of("root");
				}
			}
			return Optional.Empty();
		}

		/// <seealso cref="IncomingDependencyLabel(Java.Util.Properties, int, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual Optional<string> IncomingDependencyLabel(Properties props, int index)
		{
			return IncomingDependencyLabel(props, index, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <seealso cref="IncomingDependencyLabel(Java.Util.Properties, int, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual Optional<string> IncomingDependencyLabel(int index, SemanticGraphFactory.Mode mode)
		{
			return IncomingDependencyLabel(this.defaultProps, index, mode);
		}

		/// <seealso cref="IncomingDependencyLabel(Java.Util.Properties, int)"></seealso>
		public virtual Optional<string> IncomingDependencyLabel(int index)
		{
			return IncomingDependencyLabel(this.defaultProps, index);
		}

		/// <seealso cref="IncomingDependencyLabel(Java.Util.Properties, int)"></seealso>
		public virtual IList<Optional<string>> IncomingDependencyLabels(Properties props, SemanticGraphFactory.Mode mode)
		{
			document.RunDepparse(props);
			IList<Optional<string>> labels = new List<Optional<string>>(this.Length());
			for (int i = 0; i < this.Length(); ++i)
			{
				labels.Add(Optional.Empty());
			}
			foreach (CoreNLPProtos.DependencyGraph.Edge edge in Dependencies(mode).GetEdgeList())
			{
				labels.Set(edge.GetTarget() - 1, Optional.Of(edge.GetDep()));
			}
			foreach (int root in impl.GetBasicDependencies().GetRootList())
			{
				labels.Set(root - 1, Optional.Of("root"));
			}
			return labels;
		}

		/// <seealso cref="IncomingDependencyLabels(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual IList<Optional<string>> IncomingDependencyLabels(SemanticGraphFactory.Mode mode)
		{
			return IncomingDependencyLabels(this.defaultProps, mode);
		}

		/// <seealso cref="IncomingDependencyLabels(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual IList<Optional<string>> IncomingDependencyLabels(Properties props)
		{
			return IncomingDependencyLabels(props, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <seealso cref="IncomingDependencyLabels(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual IList<Optional<string>> IncomingDependencyLabels()
		{
			return IncomingDependencyLabels(this.defaultProps, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <summary>
		/// Returns the dependency graph of the sentence, as a raw
		/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
		/// object.
		/// Note that this method is slower than you may expect, as it has to convert the underlying protocol
		/// buffer back into a list of CoreLabels with which to populate the
		/// <see cref="Edu.Stanford.Nlp.Semgraph.SemanticGraph"/>
		/// .
		/// </summary>
		/// <param name="props">The properties to use for running the dependency parser annotator.</param>
		/// <param name="mode">The type of graph to return (e.g., basic, collapsed, etc).</param>
		/// <returns>The dependency graph of the sentence.</returns>
		public virtual SemanticGraph DependencyGraph(Properties props, SemanticGraphFactory.Mode mode)
		{
			document.RunDepparse(props);
			return ProtobufAnnotationSerializer.FromProto(Dependencies(mode), AsCoreLabels(), document.Docid().OrElse(null));
		}

		/// <seealso cref="DependencyGraph(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual SemanticGraph DependencyGraph(Properties props)
		{
			return DependencyGraph(props, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <seealso cref="DependencyGraph(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual SemanticGraph DependencyGraph()
		{
			return DependencyGraph(this.defaultProps, SemanticGraphFactory.Mode.Enhanced);
		}

		/// <seealso cref="DependencyGraph(Java.Util.Properties, Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory.Mode)"></seealso>
		public virtual SemanticGraph DependencyGraph(SemanticGraphFactory.Mode mode)
		{
			return DependencyGraph(this.defaultProps, mode);
		}

		/// <summary>The length of the sentence, in tokens</summary>
		public virtual int Length()
		{
			return impl.GetTokenCount();
		}

		/// <summary>Get a list of the (possible) Natural Logic operators on each node of the sentence.</summary>
		/// <remarks>
		/// Get a list of the (possible) Natural Logic operators on each node of the sentence.
		/// At each index, the list contains an operator spec if that index is the head word of an operator in the
		/// sentence.
		/// </remarks>
		/// <param name="props">The properties to pass to the natural logic annotator.</param>
		/// <returns>
		/// A list of Optionals, where each element corresponds to a token in the sentence, and the optional is nonempty
		/// if that index is an operator.
		/// </returns>
		public virtual IList<Optional<OperatorSpec>> Operators(Properties props)
		{
			document.RunNatlog(props);
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <seealso cref="Operators(Java.Util.Properties)"></seealso>
		public virtual IList<Optional<OperatorSpec>> Operators()
		{
			return Operators(this.defaultProps);
		}

		/// <seealso cref="Operators(Java.Util.Properties)"></seealso>
		public virtual Optional<OperatorSpec> OperatorAt(Properties props, int i)
		{
			return Operators(props)[i];
		}

		/// <seealso cref="Operators(Java.Util.Properties)"></seealso>
		public virtual Optional<OperatorSpec> OperatorAt(int i)
		{
			return Operators(this.defaultProps)[i];
		}

		/// <summary>Returns the list of non-empty Natural Logic operator specifications.</summary>
		/// <remarks>
		/// Returns the list of non-empty Natural Logic operator specifications.
		/// This amounts to the actual list of operators in the sentence.
		/// Note that the spans of the operators can be retrieved with
		/// <see cref="Edu.Stanford.Nlp.Naturalli.OperatorSpec.quantifierBegin"/>
		/// and
		/// <see cref="Edu.Stanford.Nlp.Naturalli.OperatorSpec.quantifierEnd"/>
		/// .
		/// </remarks>
		/// <param name="props">The properties to use for the natlog annotator.</param>
		/// <returns>A list of operators in the sentence.</returns>
		public virtual IList<OperatorSpec> OperatorsNonempty(Properties props)
		{
			return Operators(props).Stream().Filter(null).Map(null).Collect(Collectors.ToList());
		}

		/// <seealso cref="OperatorsNonempty(Java.Util.Properties)"></seealso>
		public virtual IList<OperatorSpec> OperatorsNonempty()
		{
			return OperatorsNonempty(this.defaultProps);
		}

		/// <summary>The Natural Logic notion of polarity for each token in a sentence.</summary>
		/// <param name="props">The properties to use for the natural logic annotator.</param>
		/// <returns>A list of Polarity objects, one for each token of the sentence.</returns>
		public virtual IList<Polarity> NatlogPolarities(Properties props)
		{
			document.RunNatlog(props);
			lock (impl)
			{
				return LazyList(tokensBuilders, null);
			}
		}

		/// <seealso cref="NatlogPolarities(Java.Util.Properties)"></seealso>
		public virtual IList<Polarity> NatlogPolarities()
		{
			return NatlogPolarities(this.defaultProps);
		}

		/// <summary>Get the polarity (the Natural Logic notion of polarity) for a given token in the sentence.</summary>
		/// <param name="props">The properties to use for the natural logic annotator.</param>
		/// <param name="index">The index to return the polarity of.</param>
		/// <returns>A list of Polarity objects, one for each token of the sentence.</returns>
		public virtual Polarity NatlogPolarity(Properties props, int index)
		{
			document.RunNatlog(props);
			lock (impl)
			{
				return ProtobufAnnotationSerializer.FromProto(tokensBuilders[index].GetPolarity());
			}
		}

		/// <seealso cref="NatlogPolarity(Java.Util.Properties, int)"></seealso>
		public virtual Polarity NatlogPolarity(int index)
		{
			return NatlogPolarity(this.defaultProps, index);
		}

		/// <summary>Get the OpenIE triples associated with this sentence.</summary>
		/// <remarks>
		/// Get the OpenIE triples associated with this sentence.
		/// Note that this function may be slower than you would expect, as it has to
		/// convert the underlying Protobuf representation back into
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s.
		/// </remarks>
		/// <param name="props">The properties to use for the OpenIE annotator.</param>
		/// <returns>
		/// A collection of
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// objects representing the OpenIE triples in the sentence.
		/// </returns>
		public virtual ICollection<RelationTriple> OpenieTriples(Properties props)
		{
			document.RunOpenie(props);
			lock (impl)
			{
				IList<CoreLabel> tokens = AsCoreLabels();
				Annotation doc = document.AsAnnotation();
				return impl.GetOpenieTripleList().Stream().Map(null).Collect(Collectors.ToList());
			}
		}

		/// <seealso>Sentence@openieTriples(Properties)</seealso>
		public virtual ICollection<RelationTriple> OpenieTriples()
		{
			return OpenieTriples(this.defaultProps);
		}

		/// <summary>Get a list of Open IE triples as flat (subject, relation, object, confidence) quadruples.</summary>
		/// <remarks>
		/// Get a list of Open IE triples as flat (subject, relation, object, confidence) quadruples.
		/// This is substantially faster than returning
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// objects, as it doesn't
		/// require converting the underlying representation into
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s; but, it also contains
		/// significantly less information about the sentence.
		/// </remarks>
		/// <seealso>Sentence@openieTriples(Properties)</seealso>
		public virtual ICollection<Quadruple<string, string, string, double>> Openie()
		{
			document.RunOpenie(this.defaultProps);
			return impl.GetOpenieTripleList().Stream().Filter(null).Map(null).Collect(Collectors.ToList());
		}

		/// <summary>Get the KBP triples associated with this sentence.</summary>
		/// <remarks>
		/// Get the KBP triples associated with this sentence.
		/// Note that this function may be slower than you would expect, as it has to
		/// convert the underlying Protobuf representation back into
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s.
		/// </remarks>
		/// <param name="props">The properties to use for the KBP annotator.</param>
		/// <returns>
		/// A collection of
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// objects representing the KBP triples in the sentence.
		/// </returns>
		public virtual ICollection<RelationTriple> KbpTriples(Properties props)
		{
			document.RunKBP(props);
			lock (impl)
			{
				IList<CoreLabel> tokens = AsCoreLabels();
				Annotation doc = document.AsAnnotation();
				return impl.GetKbpTripleList().Stream().Map(null).Collect(Collectors.ToList());
			}
		}

		/// <seealso>Sentence@kbpTriples(Properties)</seealso>
		public virtual ICollection<RelationTriple> KbpTriples()
		{
			return KbpTriples(this.defaultProps);
		}

		/// <summary>Get a list of KBP triples as flat (subject, relation, object, confidence) quadruples.</summary>
		/// <remarks>
		/// Get a list of KBP triples as flat (subject, relation, object, confidence) quadruples.
		/// This is substantially faster than returning
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// objects, as it doesn't
		/// require converting the underlying representation into
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// s; but, it also contains
		/// significantly less information about the sentence.
		/// </remarks>
		/// <seealso>Sentence@kbpTriples(Properties)</seealso>
		public virtual ICollection<Quadruple<string, string, string, double>> Kbp()
		{
			document.RunKBP(this.defaultProps);
			return impl.GetKbpTripleList().Stream().Filter(null).Map(null).Collect(Collectors.ToList());
		}

		/// <summary>The sentiment of this sentence (e.g., positive / negative).</summary>
		/// <returns>
		/// The
		/// <see cref="SentimentClass"/>
		/// of this sentence, as an enum value.
		/// </returns>
		public virtual SentimentClass Sentiment()
		{
			return Sentiment(this.defaultProps);
		}

		/// <summary>The sentiment of this sentence (e.g., positive / negative).</summary>
		/// <param name="props">The properties to pass to the sentiment classifier.</param>
		/// <returns>
		/// The
		/// <see cref="SentimentClass"/>
		/// of this sentence, as an enum value.
		/// </returns>
		public virtual SentimentClass Sentiment(Properties props)
		{
			document.RunSentiment(props);
			switch (impl.GetSentiment().ToLower())
			{
				case "very positive":
				{
					return SentimentClass.VeryPositive;
				}

				case "positive":
				{
					return SentimentClass.Positive;
				}

				case "negative":
				{
					return SentimentClass.Negative;
				}

				case "very negative":
				{
					return SentimentClass.VeryNegative;
				}

				case "neutral":
				{
					return SentimentClass.Neutral;
				}

				default:
				{
					throw new InvalidOperationException("Unknown sentiment class: " + impl.GetSentiment());
				}
			}
		}

		/// <summary>Get the coreference chain for just this sentence.</summary>
		/// <remarks>
		/// Get the coreference chain for just this sentence.
		/// Note that this method is actually fairly computationally expensive to call, as it constructs and prunes
		/// the coreference data structure for the entire document.
		/// </remarks>
		/// <returns>A coreference chain, but only for this sentence</returns>
		public virtual IDictionary<int, CorefChain> Coref()
		{
			// Get the raw coref structure
			IDictionary<int, CorefChain> allCorefs = document.Coref();
			// Delete coreference chains not in this sentence
			ICollection<int> toDeleteEntirely = new HashSet<int>();
			foreach (KeyValuePair<int, CorefChain> integerCorefChainEntry in allCorefs)
			{
				CorefChain chain = integerCorefChainEntry.Value;
				IList<CorefChain.CorefMention> mentions = new List<CorefChain.CorefMention>(chain.GetMentionsInTextualOrder());
				mentions.Stream().Filter(null).ForEach(null);
				if (chain.GetMentionsInTextualOrder().IsEmpty())
				{
					toDeleteEntirely.Add(integerCorefChainEntry.Key);
				}
			}
			// Clean up dangling empty chains
			toDeleteEntirely.ForEach(null);
			// Return
			return allCorefs;
		}

		//
		// Helpers for CoreNLP interoperability
		//
		/// <summary>Returns this sentence as a CoreNLP CoreMap object.</summary>
		/// <remarks>
		/// Returns this sentence as a CoreNLP CoreMap object.
		/// Note that, importantly, only the fields which have already been called will be populated in
		/// the CoreMap!
		/// Therefore, this method is generally NOT recommended.
		/// </remarks>
		/// <param name="functions">
		/// A list of functions to call before populating the CoreMap.
		/// For example, you can specify mySentence::posTags, and then posTags will
		/// be populated.
		/// </param>
		[SafeVarargs]
		public ICoreMap AsCoreMap(params IFunction<Edu.Stanford.Nlp.Simple.Sentence, object>[] functions)
		{
			foreach (IFunction<Edu.Stanford.Nlp.Simple.Sentence, object> function in functions)
			{
				function.Apply(this);
			}
			return this.document.AsAnnotation(true).Get(typeof(CoreAnnotations.SentencesAnnotation))[this.SentenceIndex()];
		}

		/// <summary>Returns this sentence as a list of CoreLabels representing the sentence.</summary>
		/// <remarks>
		/// Returns this sentence as a list of CoreLabels representing the sentence.
		/// Note that, importantly, only the fields which have already been called will be populated in
		/// the CoreMap!
		/// Therefore, this method is generally NOT recommended.
		/// </remarks>
		/// <param name="functions">
		/// A list of functions to call before populating the CoreMap.
		/// For example, you can specify mySentence::posTags, and then posTags will
		/// be populated.
		/// </param>
		[SafeVarargs]
		public IList<CoreLabel> AsCoreLabels(params IFunction<Edu.Stanford.Nlp.Simple.Sentence, object>[] functions)
		{
			foreach (IFunction<Edu.Stanford.Nlp.Simple.Sentence, object> function in functions)
			{
				function.Apply(this);
			}
			return AsCoreMap().Get(typeof(CoreAnnotations.TokensAnnotation));
		}

		//
		// HELPERS FROM DOCUMENT
		//
		/// <summary>A helper to get the raw Protobuf builder for a given token.</summary>
		/// <remarks>
		/// A helper to get the raw Protobuf builder for a given token.
		/// Primarily useful for cache checks.
		/// </remarks>
		/// <param name="i">The index of the token to retrieve.</param>
		/// <returns>A Protobuf builder for that token.</returns>
		public virtual CoreNLPProtos.Token.Builder RawToken(int i)
		{
			return tokensBuilders[i];
		}

		/// <summary>Get the backing protocol buffer for this sentence.</summary>
		/// <returns>The raw backing protocol buffer builder for this sentence.</returns>
		public virtual CoreNLPProtos.Sentence.Builder RawSentence()
		{
			return this.impl;
		}

		/// <summary>Update each token in the sentence with the given information.</summary>
		/// <param name="tokens">
		/// The CoreNLP tokens returned by the
		/// <see cref="Edu.Stanford.Nlp.Pipeline.IAnnotator"/>
		/// .
		/// </param>
		/// <param name="setter">The function to set a Protobuf object with the given field.</param>
		/// <param name="getter">
		/// The function to get the given field from the
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreLabel"/>
		/// .
		/// </param>
		/// <?/>
		protected internal virtual void UpdateTokens<E>(IList<CoreLabel> tokens, IConsumer<Pair<CoreNLPProtos.Token.Builder, E>> setter, IFunction<CoreLabel, E> getter)
		{
			lock (this.impl)
			{
				for (int i = 0; i < tokens.Count; ++i)
				{
					E value = getter.Apply(tokens[i]);
					if (value != null)
					{
						setter.Accept(Pair.MakePair(tokensBuilders[i], value));
					}
				}
			}
		}

		/// <summary>Update the parse tree for this sentence.</summary>
		/// <param name="parse">The parse tree to update.</param>
		/// <param name="binary">The binary parse tree to update.</param>
		protected internal virtual void UpdateParse(CoreNLPProtos.ParseTree parse, CoreNLPProtos.ParseTree binary)
		{
			lock (this.impl)
			{
				this.impl.SetParseTree(parse);
				if (binary != null)
				{
					this.impl.SetBinarizedParseTree(binary);
				}
			}
		}

		/// <summary>Update the dependencies of the sentence.</summary>
		/// <param name="basic">The basic dependencies to update.</param>
		/// <param name="enhanced">The enhanced dependencies to update.</param>
		/// <param name="enhancedPlusPlus">The enhanced plus plus dependencies to update.</param>
		protected internal virtual void UpdateDependencies(CoreNLPProtos.DependencyGraph basic, CoreNLPProtos.DependencyGraph enhanced, CoreNLPProtos.DependencyGraph enhancedPlusPlus)
		{
			lock (this.impl)
			{
				this.impl.SetBasicDependencies(basic);
				this.impl.SetEnhancedDependencies(enhanced);
				this.impl.SetEnhancedPlusPlusDependencies(enhancedPlusPlus);
			}
		}

		/// <summary>Update the Open IE relation triples for this sentence.</summary>
		/// <param name="triples">The stream of relation triples to add to the sentence.</param>
		protected internal virtual void UpdateOpenIE(IStream<CoreNLPProtos.RelationTriple> triples)
		{
			lock (this.impl)
			{
				triples.ForEach(null);
			}
		}

		/// <summary>Update the Open IE relation triples for this sentence.</summary>
		/// <param name="triples">The stream of relation triples to add to the sentence.</param>
		protected internal virtual void UpdateKBP(IStream<CoreNLPProtos.RelationTriple> triples)
		{
			lock (this.impl)
			{
				triples.ForEach(null);
			}
		}

		/// <summary>Update the Sentiment class for this sentence.</summary>
		/// <param name="sentiment">The sentiment of the sentence.</param>
		protected internal virtual void UpdateSentiment(string sentiment)
		{
			lock (this.impl)
			{
				this.impl.SetSentiment(sentiment);
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Edu.Stanford.Nlp.Simple.Sentence))
			{
				return false;
			}
			Edu.Stanford.Nlp.Simple.Sentence sentence = (Edu.Stanford.Nlp.Simple.Sentence)o;
			// Short circuit for fast equals check
			if (impl.HasText() && !impl.GetText().Equals(sentence.impl.GetText()))
			{
				return false;
			}
			if (this.tokensBuilders.Count != sentence.tokensBuilders.Count)
			{
				return false;
			}
			// Check the implementation of the sentence
			if (!((CoreNLPProtos.Sentence)impl.Build()).Equals(((CoreNLPProtos.Sentence)sentence.impl.Build())))
			{
				return false;
			}
			// Check each token
			for (int i = 0; i < sz; ++i)
			{
				if (!((CoreNLPProtos.Token)tokensBuilders[i].Build()).Equals(((CoreNLPProtos.Token)sentence.tokensBuilders[i].Build())))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override int GetHashCode()
		{
			if (this.impl.HasText())
			{
				return this.impl.GetText().GetHashCode() * 31 + this.tokensBuilders.Count;
			}
			else
			{
				return ((CoreNLPProtos.Sentence)impl.Build()).GetHashCode();
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override string ToString()
		{
			return impl.GetText();
		}

		/// <param name="start">- inclusive</param>
		/// <param name="end">- exclusive</param>
		/// <returns>- the text for the provided token span.</returns>
		public virtual string Substring(int start, int end)
		{
			StringBuilder sb = new StringBuilder();
			foreach (CoreLabel word in AsCoreLabels().SubList(start, end))
			{
				sb.Append(word.Word());
				sb.Append(word.After());
			}
			return sb.ToString();
		}

		private static IList<E> LazyList<E>(IList<CoreNLPProtos.Token.Builder> tokens, IFunction<CoreNLPProtos.Token.Builder, E> fn)
		{
			return new _AbstractList_1280(fn, tokens);
		}

		private sealed class _AbstractList_1280 : AbstractList<E>
		{
			public _AbstractList_1280(IFunction<CoreNLPProtos.Token.Builder, E> fn, IList<CoreNLPProtos.Token.Builder> tokens)
			{
				this.fn = fn;
				this.tokens = tokens;
			}

			public override E Get(int index)
			{
				return fn.Apply(tokens[index]);
			}

			public override int Count
			{
				get
				{
					return tokens.Count;
				}
			}

			private readonly IFunction<CoreNLPProtos.Token.Builder, E> fn;

			private readonly IList<CoreNLPProtos.Token.Builder> tokens;
		}

		/// <summary>Returns the sentence id of the sentence, if one was found</summary>
		public virtual Optional<string> Sentenceid()
		{
			lock (impl)
			{
				if (impl.HasSentenceID())
				{
					return Optional.Of(impl.GetSentenceID());
				}
				else
				{
					return Optional.Empty();
				}
			}
		}

		/// <summary>Apply a TokensRegex pattern to the sentence.</summary>
		/// <param name="pattern">The TokensRegex pattern to match against.</param>
		/// <returns>the matcher.</returns>
		public virtual bool Matches(TokenSequencePattern pattern)
		{
			return ((TokenSequenceMatcher)pattern.GetMatcher(AsCoreLabels())).Matches();
		}

		/// <summary>Apply a TokensRegex pattern to the sentence.</summary>
		/// <param name="pattern">The TokensRegex pattern to match against.</param>
		/// <returns>True if the tokensregex pattern matches.</returns>
		public virtual bool Matches(string pattern)
		{
			return Matches(TokenSequencePattern.Compile(pattern));
		}

		/// <summary>Apply a TokensRegex pattern to the sentence.</summary>
		/// <param name="pattern">The TokensRegex pattern to match against.</param>
		/// <param name="fn">The action to do on each match.</param>
		/// <returns>the list of matches, after run through the function.</returns>
		public virtual IList<T> Find<T>(TokenSequencePattern pattern, IFunction<TokenSequenceMatcher, T> fn)
		{
			TokenSequenceMatcher matcher = pattern.Matcher(AsCoreLabels());
			IList<T> lst = new List<T>();
			while (matcher.Find())
			{
				lst.Add(fn.Apply(matcher));
			}
			return lst;
		}

		public virtual IList<T> Find<T>(string pattern, IFunction<TokenSequenceMatcher, T> fn)
		{
			return Find(TokenSequencePattern.Compile(pattern), fn);
		}

		/// <summary>Apply a semgrex pattern to the sentence</summary>
		/// <param name="pattern">The Semgrex pattern to match against.</param>
		/// <param name="fn">The action to do on each match.</param>
		/// <returns>the list of matches, after run through the function.</returns>
		public virtual IList<T> Semgrex<T>(SemgrexPattern pattern, IFunction<SemgrexMatcher, T> fn)
		{
			SemgrexMatcher matcher = pattern.Matcher(DependencyGraph());
			IList<T> lst = new List<T>();
			while (matcher.FindNextMatchingNode())
			{
				lst.Add(fn.Apply(matcher));
			}
			return lst;
		}

		/// <summary>Apply a semgrex pattern to the sentence</summary>
		/// <param name="pattern">The Semgrex pattern to match against.</param>
		/// <param name="fn">The action to do on each match.</param>
		/// <returns>the list of matches, after run through the function.</returns>
		public virtual IList<T> Semgrex<T>(string pattern, IFunction<SemgrexMatcher, T> fn)
		{
			return Semgrex(SemgrexPattern.Compile(pattern), fn);
		}
	}
}

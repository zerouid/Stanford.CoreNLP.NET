// AbstractSequenceClassifier -- a framework for probabilistic sequence models.
// Copyright (c) 2002-2008 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/CRF-NER.html
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;










namespace Edu.Stanford.Nlp.IE
{
	/// <summary>This class provides common functionality for (probabilistic) sequence models.</summary>
	/// <remarks>
	/// This class provides common functionality for (probabilistic) sequence models.
	/// It is a superclass of our CMM and CRF sequence classifiers, and is even used
	/// in the (deterministic) NumberSequenceClassifier. See implementing classes for
	/// more information.
	/// An implementation must implement these 5 abstract methods: <br />
	/// <c>List&lt;IN&gt; classify(List&lt;IN&gt; document);</c>
	/// <br />
	/// <c>List&lt;IN&gt; classifyWithGlobalInformation(List&lt;IN&gt; tokenSequence, final CoreMap document, final CoreMap sentence);</c>
	/// <br />
	/// <c>void train(Collection&lt;List&lt;IN&gt;&gt; docs, DocumentReaderAndWriter&lt;IN&gt; readerAndWriter);</c>
	/// <br />
	/// <c>void serializeClassifier(String serializePath);</c>
	/// <br />
	/// <c>
	/// void loadClassifier(ObjectInputStream in, Properties props) throws IOException,
	/// ClassCastException, ClassNotFoundException;
	/// </c>
	/// <br />
	/// but a runtime (or rule-based) implementation can usefully implement just the first,
	/// and throw UnsupportedOperationException for the rest. Additionally, this method throws
	/// UnsupportedOperationException by default, but is implemented for some classifiers: <br />
	/// <c>Pair&lt;Counter&lt;Integer&gt;, TwoDimensionalCounter&lt;Integer,String&gt;&gt; printProbsDocument(List&lt;CoreLabel&gt; document);</c>
	/// <br />
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	/// <author>Dan Cer</author>
	/// <author>sonalg (made the class generic)</author>
	public abstract class AbstractSequenceClassifier<In> : Func<string, string>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.AbstractSequenceClassifier));

		public SeqClassifierFlags flags;

		public IIndex<string> classIndex;

		/// <summary>Support multiple feature factories (NERFeatureFactory, EmbeddingFeatureFactory) - Thang Sep 13, 2013.</summary>
		public IList<FeatureFactory<In>> featureFactories;

		protected internal IN pad;

		private ICoreTokenFactory<In> tokenFactory;

		public int windowSize;

		/// <summary>
		/// Different threads can add or query knownLCWords at the same time,
		/// so we need a concurrent data structure.
		/// </summary>
		/// <remarks>
		/// Different threads can add or query knownLCWords at the same time,
		/// so we need a concurrent data structure.  Created in reinit().
		/// </remarks>
		protected internal MaxSizeConcurrentHashSet<string> knownLCWords;

		/// <summary>This field can cache an allocated defaultReaderAndWriter.</summary>
		/// <remarks>
		/// This field can cache an allocated defaultReaderAndWriter. Never access this variable directly,
		/// as it is lazily allocated. Use the
		/// <see cref="AbstractSequenceClassifier{IN}.DefaultReaderAndWriter()"/>
		/// method.
		/// </remarks>
		private IDocumentReaderAndWriter<In> defaultReaderAndWriter;

		// = null;
		// = null;
		/// <summary>This is the DocumentReaderAndWriter used for reading training and testing files.</summary>
		/// <remarks>
		/// This is the DocumentReaderAndWriter used for reading training and testing files.
		/// It is the DocumentReaderAndWriter specified by the readerAndWriter flag and
		/// defaults to
		/// <c>edu.stanford.nlp.sequences.ColumnDocumentReaderAndWriter</c>
		/// which
		/// is suitable for reading CoNLL-style TSV files.
		/// </remarks>
		/// <returns>The default DocumentReaderAndWriter</returns>
		public virtual IDocumentReaderAndWriter<In> DefaultReaderAndWriter()
		{
			lock (this)
			{
				if (defaultReaderAndWriter == null)
				{
					defaultReaderAndWriter = MakeReaderAndWriter();
				}
				return defaultReaderAndWriter;
			}
		}

		/// <summary>This field can cache an allocated plainTextReaderAndWriter.</summary>
		/// <remarks>
		/// This field can cache an allocated plainTextReaderAndWriter. Never access this variable directly,
		/// as it is lazily allocated. Use the
		/// <see cref="AbstractSequenceClassifier{IN}.PlainTextReaderAndWriter()"/>
		/// method.
		/// </remarks>
		private IDocumentReaderAndWriter<In> plainTextReaderAndWriter;

		/// <summary>
		/// This is the default DocumentReaderAndWriter used for reading text files for runtime
		/// classification.
		/// </summary>
		/// <remarks>
		/// This is the default DocumentReaderAndWriter used for reading text files for runtime
		/// classification. It is the DocumentReaderAndWriter specified by the plainTextDocumentReaderAndWriter
		/// flag and defaults to
		/// <c>edu.stanford.nlp.sequences.PlainTextDocumentReaderAndWriter</c>
		/// which
		/// is suitable for reading plain text files, in languages with a Tokenizer available.
		/// This reader is now allocated lazily when required, since many times (such as when using
		/// AbstractSequenceClassifiers in StanfordCoreNLP, these DocumentReaderAndWriters are never used.
		/// Synchronized for safe lazy initialization.
		/// </remarks>
		/// <returns>The default plain text DocumentReaderAndWriter</returns>
		public virtual IDocumentReaderAndWriter<In> PlainTextReaderAndWriter()
		{
			lock (this)
			{
				if (plainTextReaderAndWriter == null)
				{
					if (flags.readerAndWriter != null && flags.readerAndWriter.Equals(flags.plainTextDocumentReaderAndWriter))
					{
						plainTextReaderAndWriter = DefaultReaderAndWriter();
					}
					else
					{
						plainTextReaderAndWriter = MakePlainTextReaderAndWriter();
					}
				}
				return plainTextReaderAndWriter;
			}
		}

		/// <summary>
		/// Construct a SeqClassifierFlags object based on the passed in properties,
		/// and then call the other constructor.
		/// </summary>
		/// <param name="props">See SeqClassifierFlags for known properties.</param>
		public AbstractSequenceClassifier(Properties props)
			: this(new SeqClassifierFlags(props))
		{
		}

		/// <summary>
		/// Initialize the featureFactory and other variables based on the passed in
		/// flags.
		/// </summary>
		/// <param name="flags">A specification of the AbstractSequenceClassifier to construct.</param>
		public AbstractSequenceClassifier(SeqClassifierFlags flags)
		{
			this.flags = flags;
			// Thang Sep13: allow for multiple feature factories.
			this.featureFactories = Generics.NewArrayList();
			if (flags.featureFactory != null)
			{
				FeatureFactory<In> factory = new MetaClass(flags.featureFactory).CreateInstance(flags.featureFactoryArgs);
				// for compatibility
				featureFactories.Add(factory);
			}
			if (flags.featureFactories != null)
			{
				for (int i = 0; i < flags.featureFactories.Length; i++)
				{
					FeatureFactory<In> indFeatureFactory = new MetaClass(flags.featureFactories[i]).CreateInstance(flags.featureFactoriesArgs[i]);
					this.featureFactories.Add(indFeatureFactory);
				}
			}
			if (flags.tokenFactory == null)
			{
				tokenFactory = (ICoreTokenFactory<In>)new CoreLabelTokenFactory();
			}
			else
			{
				this.tokenFactory = new MetaClass(flags.tokenFactory).CreateInstance(flags.tokenFactoryArgs);
			}
			pad = tokenFactory.MakeToken();
			windowSize = flags.maxLeft + 1;
			Reinit();
		}

		/// <summary>
		/// This method should be called after there have been changes to the flags
		/// (SeqClassifierFlags) variable, such as after deserializing a classifier.
		/// </summary>
		/// <remarks>
		/// This method should be called after there have been changes to the flags
		/// (SeqClassifierFlags) variable, such as after deserializing a classifier. It
		/// is called inside the loadClassifier methods. It assumes that the flags
		/// variable and the pad variable exist, but reinitializes things like the pad
		/// variable, featureFactory and readerAndWriter based on the flags.
		/// <p>
		/// <i>Implementation note:</i> At the moment this variable doesn't set
		/// windowSize or featureFactory, since they are being serialized separately in
		/// the file, but we should probably stop serializing them and just
		/// reinitialize them from the flags?
		/// </remarks>
		protected internal void Reinit()
		{
			pad.Set(typeof(CoreAnnotations.AnswerAnnotation), flags.backgroundSymbol);
			pad.Set(typeof(CoreAnnotations.GoldAnswerAnnotation), flags.backgroundSymbol);
			foreach (FeatureFactory featureFactory in featureFactories)
			{
				featureFactory.Init(flags);
			}
			defaultReaderAndWriter = null;
			plainTextReaderAndWriter = null;
			if (knownLCWords == null || knownLCWords.IsEmpty())
			{
				// reinit limits max (additional) size. We temporarily loosen this during training
				knownLCWords = new MaxSizeConcurrentHashSet<string>(flags.maxAdditionalKnownLCWords);
			}
			else
			{
				knownLCWords.SetMaxSize(knownLCWords.Count + flags.maxAdditionalKnownLCWords);
			}
		}

		public virtual ICollection<string> GetKnownLCWords()
		{
			return knownLCWords;
		}

		/// <summary>
		/// Makes a DocumentReaderAndWriter based on the flags the CRFClassifier
		/// was constructed with.
		/// </summary>
		/// <remarks>
		/// Makes a DocumentReaderAndWriter based on the flags the CRFClassifier
		/// was constructed with.  Will create an instance of the class specified in
		/// the property flags.readerAndWriter and
		/// initialize it with the CRFClassifier's flags.
		/// </remarks>
		/// <returns>The appropriate ReaderAndWriter for training/testing this classifier</returns>
		public virtual IDocumentReaderAndWriter<In> MakeReaderAndWriter()
		{
			IDocumentReaderAndWriter<In> readerAndWriter;
			try
			{
				readerAndWriter = ReflectionLoading.LoadByReflection(flags.readerAndWriter);
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("Error loading flags.readerAndWriter: '%s'", flags.readerAndWriter), e);
			}
			readerAndWriter.Init(flags);
			return readerAndWriter;
		}

		/// <summary>
		/// Makes a DocumentReaderAndWriter based on
		/// flags.plainTextReaderAndWriter.
		/// </summary>
		/// <remarks>
		/// Makes a DocumentReaderAndWriter based on
		/// flags.plainTextReaderAndWriter.  Useful for reading in
		/// untokenized text documents or reading plain text from the command
		/// line.  An example of a way to use this would be to return a
		/// edu.stanford.nlp.wordseg.Sighan2005DocumentReaderAndWriter for
		/// the Chinese Segmenter.
		/// </remarks>
		public virtual IDocumentReaderAndWriter<In> MakePlainTextReaderAndWriter()
		{
			string readerClassName = flags.plainTextDocumentReaderAndWriter;
			// We set this default here if needed because there may be models
			// which don't have the reader flag set
			if (readerClassName == null)
			{
				readerClassName = SeqClassifierFlags.DefaultPlainTextReader;
			}
			IDocumentReaderAndWriter<In> readerAndWriter;
			try
			{
				readerAndWriter = ReflectionLoading.LoadByReflection(readerClassName);
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("Error loading flags.plainTextDocumentReaderAndWriter: '%s'", flags.plainTextDocumentReaderAndWriter), e);
			}
			readerAndWriter.Init(flags);
			return readerAndWriter;
		}

		/// <summary>Returns the background class for the classifier.</summary>
		/// <returns>The background class name</returns>
		public virtual string BackgroundSymbol()
		{
			return flags.backgroundSymbol;
		}

		public virtual ICollection<string> Labels()
		{
			return Generics.NewHashSet(classIndex.ObjectsList());
		}

		/// <summary>Classify a List of IN.</summary>
		/// <remarks>
		/// Classify a List of IN. This method returns a new list of tokens, not
		/// the list of tokens passed in, and runs the new tokens through
		/// ObjectBankWrapper.  (Both these behaviors are different from that of the
		/// classify(List) method.
		/// </remarks>
		/// <param name="tokenSequence">The List of IN to be classified.</param>
		/// <returns>
		/// The classified List of IN, where the classifier output for
		/// each token is stored in its
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.AnswerAnnotation"/>
		/// field.
		/// </returns>
		public virtual IList<In> ClassifySentence<_T0>(IList<_T0> tokenSequence)
			where _T0 : IHasWord
		{
			IList<In> document = PreprocessTokens(tokenSequence);
			Classify(document);
			return document;
		}

		private IList<In> PreprocessTokens<_T0>(IList<_T0> tokenSequence)
			where _T0 : IHasWord
		{
			// log.info("knownLCWords.size is " + knownLCWords.size() + "; knownLCWords.maxSize is " + knownLCWords.getMaxSize() +
			//                   ", prior to NER for " + getClass().toString());
			IList<In> document = new List<In>();
			int i = 0;
			foreach (IHasWord word in tokenSequence)
			{
				IN wi;
				// initialized below
				if (word is ICoreMap)
				{
					// copy all annotations! some are required later in
					// AbstractSequenceClassifier.classifyWithInlineXML
					// wi = (IN) new ArrayCoreMap((ArrayCoreMap) word);
					wi = tokenFactory.MakeToken((IN)word);
				}
				else
				{
					wi = tokenFactory.MakeToken();
					wi.Set(typeof(CoreAnnotations.TextAnnotation), word.Word());
				}
				// wi.setWord(word.word());
				wi.Set(typeof(CoreAnnotations.PositionAnnotation), int.ToString(i));
				wi.Set(typeof(CoreAnnotations.AnswerAnnotation), BackgroundSymbol());
				document.Add(wi);
				i++;
			}
			// TODO get rid of ObjectBankWrapper
			ObjectBankWrapper<In> wrapper = new ObjectBankWrapper<In>(flags, null, knownLCWords);
			wrapper.ProcessDocument(document);
			// log.info("Size of knownLCWords is " + knownLCWords.size() + ", after NER for " + getClass().toString());
			return document;
		}

		/// <summary>Classify a List of IN using whatever additional information is passed in globalInfo.</summary>
		/// <remarks>
		/// Classify a List of IN using whatever additional information is passed in globalInfo.
		/// Used by SUTime (NumberSequenceClassifier), which requires the doc date to resolve relative dates.
		/// </remarks>
		/// <param name="tokenSequence">The List of IN to be classified.</param>
		/// <returns>
		/// The classified List of IN, where the classifier output for
		/// each token is stored in its "answer" field.
		/// </returns>
		public virtual IList<In> ClassifySentenceWithGlobalInformation<_T0>(IList<_T0> tokenSequence, ICoreMap doc, ICoreMap sentence)
			where _T0 : IHasWord
		{
			IList<In> document = PreprocessTokens(tokenSequence);
			ClassifyWithGlobalInformation(document, doc, sentence);
			return document;
		}

		public virtual ISequenceModel GetSequenceModel(IList<In> doc)
		{
			throw new NotSupportedException();
		}

		public virtual ISampler<IList<In>> GetSampler(IList<In> input)
		{
			return new _ISampler_352(this, input);
		}

		private sealed class _ISampler_352 : ISampler<IList<In>>
		{
			public _ISampler_352(AbstractSequenceClassifier<In> _enclosing, IList<In> input)
			{
				this._enclosing = _enclosing;
				this.input = input;
				this.model = this._enclosing.GetSequenceModel(input);
				this.sampler = new SequenceSampler();
			}

			internal ISequenceModel model;

			internal SequenceSampler sampler;

			public IList<In> DrawSample()
			{
				int[] sampleArray = this.sampler.BestSequence(this.model);
				IList<In> sample = new List<In>();
				int i = 0;
				foreach (IN word in input)
				{
					IN newWord = this._enclosing.tokenFactory.MakeToken(word);
					newWord.Set(typeof(CoreAnnotations.AnswerAnnotation), this._enclosing.classIndex.Get(sampleArray[i++]));
					sample.Add(newWord);
				}
				return sample;
			}

			private readonly AbstractSequenceClassifier<In> _enclosing;

			private readonly IList<In> input;
		}

		/// <summary>Takes a list of tokens and provides the K best sequence labelings of these tokens with their scores.</summary>
		/// <param name="doc">The List of tokens</param>
		/// <param name="answerField">The key for each token into which the label for the token will be written</param>
		/// <param name="k">The number of best sequence labelings to generate</param>
		/// <returns>
		/// A Counter where each key is a List of tokens with labels written in the answerField and its value
		/// is the score (conditional probability) assigned to this labeling of the sequence.
		/// </returns>
		public virtual ICounter<IList<In>> ClassifyKBest(IList<In> doc, Type answerField, int k)
		{
			if (doc.IsEmpty())
			{
				return new ClassicCounter<IList<In>>();
			}
			// TODO get rid of ObjectBankWrapper
			// i'm sorry that this is so hideous - JRF
			ObjectBankWrapper<In> obw = new ObjectBankWrapper<In>(flags, null, knownLCWords);
			doc = obw.ProcessDocument(doc);
			ISequenceModel model = GetSequenceModel(doc);
			KBestSequenceFinder tagInference = new KBestSequenceFinder();
			ICounter<int[]> bestSequences = tagInference.KBestSequences(model, k);
			ICounter<IList<In>> kBest = new ClassicCounter<IList<In>>();
			foreach (int[] seq in bestSequences.KeySet())
			{
				IList<In> kth = new List<In>();
				int pos = model.LeftWindow();
				foreach (IN fi in doc)
				{
					IN newFL = tokenFactory.MakeToken(fi);
					string guess = classIndex.Get(seq[pos]);
					fi.Remove(typeof(CoreAnnotations.AnswerAnnotation));
					// because fake answers will get
					// added during testing
					newFL.Set(answerField, guess);
					pos++;
					kth.Add(newFL);
				}
				kBest.SetCount(kth, bestSequences.GetCount(seq));
			}
			return kBest;
		}

		private DFSA<string, int> GetViterbiSearchGraph(IList<In> doc, Type answerField)
		{
			if (doc.IsEmpty())
			{
				return new DFSA<string, int>(null);
			}
			// TODO get rid of ObjectBankWrapper
			ObjectBankWrapper<In> obw = new ObjectBankWrapper<In>(flags, null, knownLCWords);
			doc = obw.ProcessDocument(doc);
			ISequenceModel model = GetSequenceModel(doc);
			return ViterbiSearchGraphBuilder.GetGraph(model, classIndex);
		}

		/// <summary>Classify the tokens in a String.</summary>
		/// <remarks>Classify the tokens in a String. Each sentence becomes a separate document.</remarks>
		/// <param name="str">
		/// A String with tokens in one or more sentences of text to be
		/// classified.
		/// </param>
		/// <returns>
		/// 
		/// <see cref="System.Collections.IList{E}"/>
		/// of classified sentences (each a List of something that
		/// extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// ).
		/// </returns>
		public virtual IList<IList<In>> Classify(string str)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromString(str, PlainTextReaderAndWriter());
			return ClassifyObjectBank(documents);
		}

		/// <summary>Classify the tokens in a String.</summary>
		/// <remarks>
		/// Classify the tokens in a String. Each sentence becomes a separate document.
		/// Doesn't override default readerAndWriter.
		/// </remarks>
		/// <param name="str">A String with tokens in one or more sentences of text to be classified.</param>
		/// <returns>
		/// 
		/// <see cref="System.Collections.IList{E}"/>
		/// of classified sentences (each a List of something that
		/// extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// ).
		/// </returns>
		public virtual IList<IList<In>> ClassifyRaw(string str, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromString(str, readerAndWriter);
			return ClassifyObjectBank(documents);
		}

		/// <summary>Classify the contents of a file.</summary>
		/// <param name="filename">Contains the sentence(s) to be classified.</param>
		/// <returns>
		/// 
		/// <see cref="System.Collections.IList{E}"/>
		/// of classified List of IN.
		/// </returns>
		public virtual IList<IList<In>> ClassifyFile(string filename)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFile(filename, PlainTextReaderAndWriter());
			return ClassifyObjectBank(documents);
		}

		/// <summary>Classify the tokens in an ObjectBank.</summary>
		/// <param name="documents">The documents in an ObjectBank to classify.</param>
		/// <returns>
		/// 
		/// <see cref="System.Collections.IList{E}"/>
		/// of classified sentences (each a List of something that
		/// extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// ).
		/// </returns>
		private IList<IList<In>> ClassifyObjectBank(ObjectBank<IList<In>> documents)
		{
			IList<IList<In>> result = new List<IList<In>>();
			foreach (IList<In> document in documents)
			{
				Classify(document);
				IList<In> sentence = new List<In>();
				foreach (IN wi in document)
				{
					// TaggedWord word = new TaggedWord(wi.word(), wi.answer());
					// sentence.add(word);
					sentence.Add(wi);
				}
				result.Add(sentence);
			}
			return result;
		}

		/// <summary>
		/// Maps a String input to an XML-formatted rendition of applying NER to the
		/// String.
		/// </summary>
		/// <remarks>
		/// Maps a String input to an XML-formatted rendition of applying NER to the
		/// String. Implements the Function interface. Calls
		/// classifyWithInlineXML(String) [q.v.].
		/// </remarks>
		public virtual string Apply(string @in)
		{
			return ClassifyWithInlineXML(@in);
		}

		/// <summary>
		/// Classify the contents of a
		/// <see cref="string"/>
		/// to one of several String
		/// representations that shows the classes. Plain text or XML input is expected
		/// and the
		/// <see cref="Edu.Stanford.Nlp.Sequences.PlainTextDocumentReaderAndWriter{IN}"/>
		/// is used. The classifier
		/// will tokenize the text and treat each sentence as a separate document. The
		/// output can be specified to be in a choice of three formats: slashTags
		/// (e.g., Bill/PERSON Smith/PERSON died/O ./O), inlineXML (e.g.,
		/// &lt;PERSON&gt;Bill Smith&lt;/PERSON&gt; went to
		/// &lt;LOCATION&gt;Paris&lt;/LOCATION&gt; .), or xml, for stand-off XML (e.g.,
		/// &lt;wi num="0" entity="PERSON"&gt;Sue&lt;/wi&gt; &lt;wi num="1"
		/// entity="O"&gt;shouted&lt;/wi&gt; ). There is also a binary choice as to
		/// whether the spacing between tokens of the original is preserved or whether
		/// the (tagged) tokens are printed with a single space (for inlineXML or
		/// slashTags) or a single newline (for xml) between each one.
		/// <p>
		/// <i>Fine points:</i> The slashTags and xml formats show tokens as
		/// transformed by any normalization processes inside the tokenizer, while
		/// inlineXML shows the tokens exactly as they appeared in the source text.
		/// When a period counts as both part of an abbreviation and as an end of
		/// sentence marker, it is included twice in the output String for slashTags or
		/// xml, but only once for inlineXML, where it is not counted as part of the
		/// abbreviation (or any named entity it is part of). For slashTags with
		/// preserveSpacing=true, there will be two successive periods such as "Jr.."
		/// The tokenized (preserveSpacing=false) output will have a space or a newline
		/// after the last token.
		/// </summary>
		/// <param name="sentences">
		/// The String to be classified. It will be tokenized and
		/// divided into documents according to (heuristically
		/// determined) sentence boundaries.
		/// </param>
		/// <param name="outputFormat">
		/// The format to put the output in: one of "slashTags", "xml",
		/// "inlineXML", "tsv", or "tabbedEntities"
		/// </param>
		/// <param name="preserveSpacing">
		/// Whether to preserve the input spacing between tokens, which may
		/// sometimes be none (true) or whether to tokenize the text and print
		/// it with one space between each token (false)
		/// </param>
		/// <returns>
		/// A
		/// <see cref="string"/>
		/// with annotated with classification information.
		/// </returns>
		public virtual string ClassifyToString(string sentences, string outputFormat, bool preserveSpacing)
		{
			PlainTextDocumentReaderAndWriter.OutputStyle outFormat = PlainTextDocumentReaderAndWriter.OutputStyle.FromShortName(outputFormat);
			IDocumentReaderAndWriter<In> textDocumentReaderAndWriter = PlainTextReaderAndWriter();
			ObjectBank<IList<In>> documents = MakeObjectBankFromString(sentences, textDocumentReaderAndWriter);
			StringBuilder sb = new StringBuilder();
			foreach (IList<In> doc in documents)
			{
				IList<In> docOutput = Classify(doc);
				if (textDocumentReaderAndWriter is PlainTextDocumentReaderAndWriter)
				{
					// TODO: implement this particular method and its options in the other documentReaderAndWriters
					sb.Append(((PlainTextDocumentReaderAndWriter<In>)textDocumentReaderAndWriter).GetAnswers(docOutput, outFormat, preserveSpacing));
				}
				else
				{
					StringWriter sw = new StringWriter();
					PrintWriter pw = new PrintWriter(sw);
					textDocumentReaderAndWriter.PrintAnswers(docOutput, pw);
					pw.Flush();
					sb.Append(sw);
					sb.Append('\n');
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Classify the contents of a
		/// <see cref="string"/>
		/// . Plain text or XML is expected
		/// and the
		/// <see cref="Edu.Stanford.Nlp.Sequences.PlainTextDocumentReaderAndWriter{IN}"/>
		/// is used by default.
		/// The classifier will treat each sentence as a separate document. The output can be
		/// specified to be in a choice of formats: Output is in inline XML format
		/// (e.g., &lt;PERSON&gt;Bill Smith&lt;/PERSON&gt; went to
		/// &lt;LOCATION&gt;Paris&lt;/LOCATION&gt; .)
		/// </summary>
		/// <param name="sentences">The string to be classified</param>
		/// <returns>
		/// A
		/// <see cref="string"/>
		/// with annotated with classification information.
		/// </returns>
		public virtual string ClassifyWithInlineXML(string sentences)
		{
			return ClassifyToString(sentences, "inlineXML", true);
		}

		/// <summary>Classify the contents of a String to a tagged word/class String.</summary>
		/// <remarks>
		/// Classify the contents of a String to a tagged word/class String. Plain text
		/// or XML input is expected and the
		/// <see cref="Edu.Stanford.Nlp.Sequences.PlainTextDocumentReaderAndWriter{IN}"/>
		/// is used by default.
		/// Output looks like: My/O name/O is/O Bill/PERSON Smith/PERSON ./O
		/// </remarks>
		/// <param name="sentences">The String to be classified</param>
		/// <returns>A String annotated with classification information.</returns>
		public virtual string ClassifyToString(string sentences)
		{
			return ClassifyToString(sentences, "slashTags", true);
		}

		/// <summary>
		/// Classify the contents of a
		/// <see cref="string"/>
		/// to classified character offset
		/// spans. Plain text or XML input text is expected and the
		/// <see cref="Edu.Stanford.Nlp.Sequences.PlainTextDocumentReaderAndWriter{IN}"/>
		/// is used by default.
		/// Output is a (possibly
		/// empty, but not
		/// <see langword="null"/>
		/// ) List of Triples. Each Triple is an entity
		/// name, followed by beginning and ending character offsets in the original
		/// String. Character offsets can be thought of as fenceposts between the
		/// characters, or, like certain methods in the Java String class, as character
		/// positions, numbered starting from 0, with the end index pointing to the
		/// position AFTER the entity ends. That is, end - start is the length of the
		/// entity in characters.
		/// <p>
		/// <i>Fine points:</i> Token offsets are true wrt the source text, even though
		/// the tokenizer may internally normalize certain tokens to String
		/// representations of different lengths (e.g., " becoming `` or ''). When a
		/// period counts as both part of an abbreviation and as an end of sentence
		/// marker, and that abbreviation is part of a named entity, the reported
		/// entity string excludes the period.
		/// </summary>
		/// <param name="sentences">The string to be classified</param>
		/// <returns>
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="Edu.Stanford.Nlp.Util.Triple{T1, T2, T3}"/>
		/// s, each of which gives an entity
		/// type and the beginning and ending character offsets.
		/// </returns>
		public virtual IList<Triple<string, int, int>> ClassifyToCharacterOffsets(string sentences)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromString(sentences, PlainTextReaderAndWriter());
			IList<Triple<string, int, int>> entities = new List<Triple<string, int, int>>();
			foreach (IList<In> doc in documents)
			{
				string prevEntityType = flags.backgroundSymbol;
				Triple<string, int, int> prevEntity = null;
				Classify(doc);
				foreach (IN fl in doc)
				{
					string guessedAnswer = fl.Get(typeof(CoreAnnotations.AnswerAnnotation));
					if (guessedAnswer.Equals(flags.backgroundSymbol))
					{
						if (prevEntity != null)
						{
							entities.Add(prevEntity);
							prevEntity = null;
						}
					}
					else
					{
						if (!guessedAnswer.Equals(prevEntityType))
						{
							if (prevEntity != null)
							{
								entities.Add(prevEntity);
							}
							prevEntity = new Triple<string, int, int>(guessedAnswer, fl.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), fl.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
						}
						else
						{
							System.Diagnostics.Debug.Assert(prevEntity != null);
							// if you read the code carefully, this
							// should always be true!
							prevEntity.SetThird(fl.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
						}
					}
					prevEntityType = guessedAnswer;
				}
				// include any entity at end of doc
				if (prevEntity != null)
				{
					entities.Add(prevEntity);
				}
			}
			return entities;
		}

		/// <summary>Have a word segmenter segment a String into a list of words.</summary>
		/// <remarks>
		/// Have a word segmenter segment a String into a list of words.
		/// ONLY USE IF YOU LOADED A CHINESE WORD SEGMENTER!!!!!
		/// </remarks>
		/// <param name="sentence">The string to be classified</param>
		/// <returns>List of words</returns>
		public virtual IList<string> SegmentString(string sentence)
		{
			// todo: This method is currently [2016] only called in a very small number of places:
			// the parser's jsp webapp, ChineseSegmenterAnnotator, and SegDemo.
			// Maybe we could eliminate it?
			// It also seems like it should be using the plainTextReaderAndWriter, not default?
			return SegmentString(sentence, DefaultReaderAndWriter());
		}

		public virtual IList<string> SegmentString(string sentence, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			ObjectBank<IList<In>> docs = MakeObjectBankFromString(sentence, readerAndWriter);
			StringWriter stringWriter = new StringWriter();
			PrintWriter stringPrintWriter = new PrintWriter(stringWriter);
			foreach (IList<In> doc in docs)
			{
				Classify(doc);
				readerAndWriter.PrintAnswers(doc, stringPrintWriter);
				stringPrintWriter.Println();
			}
			stringPrintWriter.Close();
			string segmented = stringWriter.ToString();
			return Arrays.AsList(segmented.Split("\\s"));
		}

		/*
		* Classify the contents of {@link SeqClassifierFlags scf.testFile}. The file
		* should be in the format expected based on {@link SeqClassifierFlags
		* scf.documentReader}.
		*
		* @return A {@link List} of {@link List}s of classified something that
		*         extends {@link CoreMap} where each {@link List} refers to a
		*         document/sentence.
		*/
		// public ObjectBank<List<In>> test() {
		// return test(flags.testFile);
		// }
		/// <summary>
		/// Classify a
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// .
		/// The classifications are added in place to the items of the document,
		/// which is also returned by this method.
		/// <i>Warning:</i> In many circumstances, you should not call this method directly.
		/// In particular, if you call this method directly, your document will not be preprocessed
		/// to add things like word distributional similarity class or word shape features that your
		/// classifier may rely on to work correctly. In such cases, you should call
		/// <see>#classifySentence(List<? extends HasWord>) classifySentence</see>
		/// instead.
		/// </summary>
		/// <param name="document">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// .
		/// </param>
		/// <returns>
		/// The same
		/// <see cref="System.Collections.IList{E}"/>
		/// , but with the elements annotated with their
		/// answers (stored under the
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.AnswerAnnotation"/>
		/// key). The answers will be the class labels defined by the CRF
		/// Classifier. They might be things like entity labels (in BIO
		/// notation or not) or something like "1" vs. "0" on whether to
		/// begin a new token here or not (in word segmentation).
		/// </returns>
		public abstract IList<In> Classify(IList<In> document);

		// todo [cdm 2017]: Check that our own NER code doesn't call this method wrongly anywhere.
		/// <summary>
		/// Classify a
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// using as
		/// additional information whatever is stored in the document and sentence.
		/// This is needed for SUTime (NumberSequenceClassifier), which requires
		/// the document date to resolve relative dates.
		/// </summary>
		/// <param name="tokenSequence">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// </param>
		/// <param name="document"/>
		/// <param name="sentence"/>
		/// <returns>Classified version of the input tokenSequence</returns>
		public abstract IList<In> ClassifyWithGlobalInformation(IList<In> tokenSequence, ICoreMap document, ICoreMap sentence);

		/// <summary>Classification is finished for the document.</summary>
		/// <remarks>
		/// Classification is finished for the document.
		/// Do any cleanup (if information was stored as part of the document for global classification)
		/// </remarks>
		/// <param name="document"/>
		public virtual void FinalizeClassification(ICoreMap document)
		{
		}

		/// <summary>Train the classifier based on values in flags.</summary>
		/// <remarks>
		/// Train the classifier based on values in flags. It will use the first of
		/// these variables that is defined: trainFiles (and baseTrainDir),
		/// trainFileList, trainFile.
		/// </remarks>
		public virtual void Train()
		{
			if (flags.trainFiles != null)
			{
				Train(flags.baseTrainDir, flags.trainFiles, DefaultReaderAndWriter());
			}
			else
			{
				if (flags.trainFileList != null)
				{
					string[] files = flags.trainFileList.Split(",");
					Train(files, DefaultReaderAndWriter());
				}
				else
				{
					Train(flags.trainFile, DefaultReaderAndWriter());
				}
			}
		}

		public virtual void Train(string filename)
		{
			Train(filename, DefaultReaderAndWriter());
		}

		public virtual void Train(string filename, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = true;
			Train(MakeObjectBankFromFile(filename, readerAndWriter), readerAndWriter);
		}

		public virtual void Train(string baseTrainDir, string trainFiles, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = true;
			Train(MakeObjectBankFromFiles(baseTrainDir, trainFiles, readerAndWriter), readerAndWriter);
		}

		public virtual void Train(string[] trainFileList, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = true;
			Train(MakeObjectBankFromFiles(trainFileList, readerAndWriter), readerAndWriter);
		}

		/// <summary>Trains a classifier from a Collection of sequences.</summary>
		/// <remarks>
		/// Trains a classifier from a Collection of sequences.
		/// Note that the Collection can be (and usually is) an ObjectBank.
		/// </remarks>
		/// <param name="docs">An ObjectBank or a collection of sequences of IN</param>
		public virtual void Train(ICollection<IList<In>> docs)
		{
			Train(docs, DefaultReaderAndWriter());
		}

		/// <summary>Trains a classifier from a Collection of sequences.</summary>
		/// <remarks>
		/// Trains a classifier from a Collection of sequences.
		/// Note that the Collection can be (and usually is) an ObjectBank.
		/// </remarks>
		/// <param name="docs">An ObjectBank or a collection of sequences of IN</param>
		/// <param name="readerAndWriter">A DocumentReaderAndWriter to use when loading test files</param>
		public abstract void Train(ICollection<IList<In>> docs, IDocumentReaderAndWriter<In> readerAndWriter);

		/// <summary>Reads a String into an ObjectBank object.</summary>
		/// <remarks>
		/// Reads a String into an ObjectBank object. NOTE: that the current
		/// implementation of ReaderIteratorFactory will first try to interpret each
		/// string as a filename, so this method will yield unwanted results if it
		/// applies to a string that is at the same time a filename. It prints out a
		/// warning, at least.
		/// </remarks>
		/// <param name="string">The String which will be the content of the ObjectBank</param>
		/// <returns>The ObjectBank</returns>
		public virtual ObjectBank<IList<In>> MakeObjectBankFromString(string @string, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			if (flags.announceObjectBankEntries)
			{
				log.Info("Reading data using " + readerAndWriter.GetType());
				if (flags.inputEncoding == null)
				{
					log.Info("Getting data from " + @string + " (default encoding)");
				}
				else
				{
					log.Info("Getting data from " + @string + " (" + flags.inputEncoding + " encoding)");
				}
			}
			// return new ObjectBank<List<In>>(new
			// ResettableReaderIteratorFactory(string), readerAndWriter);
			// TODO
			return new ObjectBankWrapper<In>(flags, new ObjectBank<IList<In>>(new ResettableReaderIteratorFactory(@string), readerAndWriter), knownLCWords);
		}

		public virtual ObjectBank<IList<In>> MakeObjectBankFromFile(string filename)
		{
			return MakeObjectBankFromFile(filename, DefaultReaderAndWriter());
		}

		public virtual ObjectBank<IList<In>> MakeObjectBankFromFile(string filename, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			string[] fileAsArray = new string[] { filename };
			return MakeObjectBankFromFiles(fileAsArray, readerAndWriter);
		}

		public virtual ObjectBank<IList<In>> MakeObjectBankFromFiles(string[] trainFileList, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			// try{
			ICollection<File> files = new List<File>();
			foreach (string trainFile in trainFileList)
			{
				File f = new File(trainFile);
				files.Add(f);
			}
			// System.err.printf("trainFileList contains %d file%s in encoding %s.%n", files.size(), files.size() == 1 ? "": "s", flags.inputEncoding);
			// TODO get rid of ObjectBankWrapper
			// return new ObjectBank<List<In>>(new
			// ResettableReaderIteratorFactory(files), readerAndWriter);
			return new ObjectBankWrapper<In>(flags, new ObjectBank<IList<In>>(new ResettableReaderIteratorFactory(files, flags.inputEncoding), readerAndWriter), knownLCWords);
		}

		// } catch (IOException e) {
		// throw new RuntimeException(e);
		// }
		public virtual ObjectBank<IList<In>> MakeObjectBankFromFiles(string baseDir, string filePattern, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			File path = new File(baseDir);
			IFileFilter filter = new RegExFileFilter(Pattern.Compile(filePattern));
			File[] origFiles = path.ListFiles(filter);
			ICollection<File> files = new List<File>();
			foreach (File file in origFiles)
			{
				if (file.IsFile())
				{
					if (flags.announceObjectBankEntries)
					{
						log.Info("Getting data from " + file + " (" + flags.inputEncoding + " encoding)");
					}
					files.Add(file);
				}
			}
			if (files.IsEmpty())
			{
				throw new Exception("No matching files: " + baseDir + '\t' + filePattern);
			}
			// return new ObjectBank<List<In>>(new
			// ResettableReaderIteratorFactory(files, flags.inputEncoding),
			// readerAndWriter);
			// TODO get rid of ObjectBankWrapper
			return new ObjectBankWrapper<In>(flags, new ObjectBank<IList<In>>(new ResettableReaderIteratorFactory(files, flags.inputEncoding), readerAndWriter), knownLCWords);
		}

		public virtual ObjectBank<IList<In>> MakeObjectBankFromFiles(ICollection<File> files, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			if (files.IsEmpty())
			{
				throw new Exception("Attempt to make ObjectBank with empty file list");
			}
			// return new ObjectBank<List<In>>(new
			// ResettableReaderIteratorFactory(files, flags.inputEncoding),
			// readerAndWriter);
			// TODO get rid of ObjectBankWrapper
			return new ObjectBankWrapper<In>(flags, new ObjectBank<IList<In>>(new ResettableReaderIteratorFactory(files, flags.inputEncoding), readerAndWriter), knownLCWords);
		}

		/// <summary>
		/// Set up an ObjectBank that will allow one to iterate over a collection of
		/// documents obtained from the passed in Reader.
		/// </summary>
		/// <remarks>
		/// Set up an ObjectBank that will allow one to iterate over a collection of
		/// documents obtained from the passed in Reader. Each document will be
		/// represented as a list of IN. If the ObjectBank iterator() is called until
		/// hasNext() returns false, then the Reader will be read till end of file, but
		/// no reading is done at the time of this call. Reading is done using the
		/// reading method specified in
		/// <c>flags.documentReader</c>
		/// , and for some
		/// reader choices, the column mapping given in
		/// <c>flags.map</c>
		/// .
		/// </remarks>
		/// <param name="in">
		/// Input data addNEWLCWords do we add new lowercase words from this
		/// data to the word shape classifier
		/// </param>
		/// <returns>The list of documents</returns>
		public virtual ObjectBank<IList<In>> MakeObjectBankFromReader(BufferedReader @in, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			if (flags.announceObjectBankEntries)
			{
				log.Info("Reading data using " + readerAndWriter.GetType());
			}
			// TODO get rid of ObjectBankWrapper
			// return new ObjectBank<List<In>>(new ResettableReaderIteratorFactory(in),
			// readerAndWriter);
			return new ObjectBankWrapper<In>(flags, new ObjectBank<IList<In>>(new ResettableReaderIteratorFactory(@in), readerAndWriter), knownLCWords);
		}

		/// <summary>
		/// Takes the file, reads it in, and prints out the likelihood of each possible
		/// label at each point.
		/// </summary>
		/// <param name="filename">The path to the specified file</param>
		public virtual void PrintProbs(string filename, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			// only for the OCR data does this matter
			// flags.ocrTrain = false;
			ObjectBank<IList<In>> docs = MakeObjectBankFromFile(filename, readerAndWriter);
			PrintProbsDocuments(docs);
		}

		/// <summary>
		/// Takes the files, reads them in, and prints out the likelihood of each possible
		/// label at each point.
		/// </summary>
		/// <param name="testFiles">A Collection of files</param>
		public virtual void PrintProbs(ICollection<File> testFiles, IDocumentReaderAndWriter<In> readerWriter)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFiles(testFiles, readerWriter);
			PrintProbsDocuments(documents);
		}

		/// <summary>
		/// Takes a
		/// <see cref="System.Collections.IList{E}"/>
		/// of documents and prints the likelihood of each
		/// possible label at each point.
		/// </summary>
		/// <param name="documents">
		/// A
		/// <see cref="System.Collections.IList{E}"/>
		/// of
		/// <see cref="System.Collections.IList{E}"/>
		/// of something that extends
		/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
		/// .
		/// </param>
		public virtual void PrintProbsDocuments(ObjectBank<IList<In>> documents)
		{
			ICounter<int> calibration = new ClassicCounter<int>();
			ICounter<int> correctByBin = new ClassicCounter<int>();
			TwoDimensionalCounter<int, string> calibratedTokens = new TwoDimensionalCounter<int, string>();
			foreach (IList<In> doc in documents)
			{
				Triple<ICounter<int>, ICounter<int>, TwoDimensionalCounter<int, string>> triple = PrintProbsDocument(doc);
				if (triple != null)
				{
					Counters.AddInPlace(calibration, triple.First());
					Counters.AddInPlace(correctByBin, triple.Second());
					calibratedTokens.AddAll(triple.Third());
				}
				System.Console.Out.WriteLine();
			}
			if (calibration.Size() > 0)
			{
				// we stored stuff, so print it out
				PrintWriter pw = new PrintWriter(System.Console.Error);
				OutputCalibrationInfo(pw, calibration, correctByBin, calibratedTokens);
				pw.Flush();
			}
		}

		private static void OutputCalibrationInfo(PrintWriter pw, ICounter<int> calibration, ICounter<int> correctByBin, TwoDimensionalCounter<int, string> calibratedTokens)
		{
			int numBins = 10;
			pw.Println();
			// in practice may well be in middle of line when called
			pw.Println("----------------------------------------");
			pw.Println("Probability distribution given to tokens (Counts for all class-token pairs; accuracy for this bin; examples are gold entity tokens in bin)");
			pw.Println("----------------------------------------");
			for (int i = 0; i < numBins; i++)
			{
				pw.Printf("[%.1f-%.1f%c: %.0f  %.2f%n", ((double)i) / numBins, ((double)(i + 1)) / numBins, i == (numBins - 1) ? ']' : ')', calibration.GetCount(i), correctByBin.GetCount(i) / calibration.GetCount(i));
			}
			pw.Println("----------------------------------------");
			for (int i_1 = 0; i_1 < numBins; i_1++)
			{
				pw.Printf("[%.1f-%.1f%c: %s%n", ((double)i_1) / numBins, ((double)(i_1 + 1)) / numBins, i_1 == (numBins - 1) ? ']' : ')', Counters.ToSortedString(calibratedTokens.GetCounter(i_1), 20, "%s=%.0f", ", ", "[%s]"));
			}
			pw.Println("----------------------------------------");
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyStdin()
		{
			ClassifyStdin(PlainTextReaderAndWriter());
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyStdin(IDocumentReaderAndWriter<In> readerWriter)
		{
			BufferedReader @is = IOUtils.ReaderFromStdin(flags.inputEncoding);
			for (string line; (line = @is.ReadLine()) != null; )
			{
				ICollection<IList<In>> documents = MakeObjectBankFromString(line, readerWriter);
				if (flags.keepEmptySentences && documents.IsEmpty())
				{
					documents = Java.Util.Collections.SingletonList<IList<In>>(Java.Util.Collections.EmptyList<In>());
				}
				ClassifyAndWriteAnswers(documents, readerWriter, false);
			}
		}

		public virtual Triple<ICounter<int>, ICounter<int>, TwoDimensionalCounter<int, string>> PrintProbsDocument(IList<In> document)
		{
			throw new NotSupportedException("Not implemented for this class.");
		}

		/// <summary>Does nothing by default.</summary>
		/// <remarks>Does nothing by default.  Subclasses can override if necessary.</remarks>
		public virtual void DumpFeatures(ICollection<IList<In>> documents)
		{
		}

		/// <summary>
		/// Load a text file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr).
		/// </summary>
		/// <remarks>
		/// Load a text file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr). This uses the value of flags.plainTextDocumentReaderAndWriter
		/// to determine how to read the textFile format. By default this gives
		/// edu.stanford.nlp.sequences.PlainTextDocumentReaderAndWriter.
		/// <i>Note:</i> This means that it works right for
		/// a plain textFile (and not a tab-separated columns test file).
		/// </remarks>
		/// <param name="textFile">The file to test on.</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyAndWriteAnswers(string textFile)
		{
			ClassifyAndWriteAnswers(textFile, PlainTextReaderAndWriter(), false);
		}

		/// <summary>
		/// Load a test file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr).
		/// </summary>
		/// <remarks>
		/// Load a test file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr). This uses the value of flags.documentReader
		/// to determine testFile format. By default, this means that it is set up to
		/// read a tab-separated columns test file
		/// </remarks>
		/// <param name="testFile">The file to test on.</param>
		/// <param name="outputScores">Whether to calculate and then log performance scores (P/R/F1)</param>
		/// <returns>A Triple of P/R/F1 if outputScores is true, else null</returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual Triple<double, double, double> ClassifyAndWriteAnswers(string testFile, bool outputScores)
		{
			return ClassifyAndWriteAnswers(testFile, DefaultReaderAndWriter(), outputScores);
		}

		/// <summary>
		/// Load a test file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr).
		/// </summary>
		/// <param name="testFile">The file to test on.</param>
		/// <param name="readerWriter">A reader and writer to use for the output</param>
		/// <param name="outputScores">Whether to calculate and then log performance scores (P/R/F1)</param>
		/// <returns>A Triple of P/R/F1 if outputScores is true, else null</returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual Triple<double, double, double> ClassifyAndWriteAnswers(string testFile, IDocumentReaderAndWriter<In> readerWriter, bool outputScores)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFile(testFile, readerWriter);
			return ClassifyAndWriteAnswers(documents, readerWriter, outputScores);
		}

		/// <summary>
		/// If the flag
		/// <c>outputEncoding</c>
		/// is defined, the output is written in that
		/// character encoding, otherwise in the system default character encoding.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual Triple<double, double, double> ClassifyAndWriteAnswers(string testFile, OutputStream outStream, IDocumentReaderAndWriter<In> readerWriter, bool outputScores)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFile(testFile, readerWriter);
			PrintWriter pw = IOUtils.EncodedOutputStreamPrintWriter(outStream, flags.outputEncoding, true);
			return ClassifyAndWriteAnswers(documents, pw, readerWriter, outputScores);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Triple<double, double, double> ClassifyAndWriteAnswers(string baseDir, string filePattern, IDocumentReaderAndWriter<In> readerWriter, bool outputScores)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFiles(baseDir, filePattern, readerWriter);
			return ClassifyAndWriteAnswers(documents, readerWriter, outputScores);
		}

		/// <summary>Run the classifier on a collection of text files.</summary>
		/// <remarks>
		/// Run the classifier on a collection of text files.
		/// Uses the plainTextReaderAndWriter to process them.
		/// </remarks>
		/// <param name="textFiles">A File Collection to process.</param>
		/// <exception cref="System.IO.IOException">For any IO error</exception>
		public virtual void ClassifyFilesAndWriteAnswers(ICollection<File> textFiles)
		{
			ClassifyFilesAndWriteAnswers(textFiles, PlainTextReaderAndWriter(), false);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyFilesAndWriteAnswers(ICollection<File> testFiles, IDocumentReaderAndWriter<In> readerWriter, bool outputScores)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFiles(testFiles, readerWriter);
			ClassifyAndWriteAnswers(documents, readerWriter, outputScores);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Triple<double, double, double> ClassifyAndWriteAnswers(ICollection<IList<In>> documents, IDocumentReaderAndWriter<In> readerWriter, bool outputScores)
		{
			return ClassifyAndWriteAnswers(documents, IOUtils.EncodedOutputStreamPrintWriter(System.Console.Out, flags.outputEncoding, true), readerWriter, outputScores);
		}

		/// <param name="documents"/>
		/// <param name="printWriter"/>
		/// <param name="readerWriter"/>
		/// <param name="outputScores">Whether to calculate and output the performance scores (P/R/F1) of the classifier</param>
		/// <returns>
		/// A Triple of overall P/R/F1, if outputScores is true, else
		/// <see langword="null"/>
		/// . The scores are done
		/// on a 0-100 scale like percentages.
		/// </returns>
		/// <exception cref="System.IO.IOException"/>
		public virtual Triple<double, double, double> ClassifyAndWriteAnswers(ICollection<IList<In>> documents, PrintWriter printWriter, IDocumentReaderAndWriter<In> readerWriter, bool outputScores)
		{
			if (flags.exportFeatures != null)
			{
				DumpFeatures(documents);
			}
			Timing timer = new Timing();
			ICounter<string> entityTP = new ClassicCounter<string>();
			ICounter<string> entityFP = new ClassicCounter<string>();
			ICounter<string> entityFN = new ClassicCounter<string>();
			bool resultsCounted = outputScores;
			int numWords = 0;
			int numDocs = 0;
			AtomicInteger threadCompletionCounter = new AtomicInteger(0);
			IThreadsafeProcessor<IList<In>, IList<In>> threadProcessor = new _IThreadsafeProcessor_1169(this, threadCompletionCounter);
			MulticoreWrapper<IList<In>, IList<In>> wrapper = null;
			if (flags.multiThreadClassifier != 0)
			{
				wrapper = new MulticoreWrapper<IList<In>, IList<In>>(flags.multiThreadClassifier, threadProcessor);
			}
			foreach (IList<In> doc in documents)
			{
				numWords += doc.Count;
				numDocs++;
				if (wrapper != null)
				{
					wrapper.Put(doc);
					while (wrapper.Peek())
					{
						IList<In> results = wrapper.Poll();
						WriteAnswers(results, printWriter, readerWriter);
						resultsCounted = resultsCounted && CountResults(results, entityTP, entityFP, entityFN);
					}
				}
				else
				{
					IList<In> results = threadProcessor.Process(doc);
					WriteAnswers(results, printWriter, readerWriter);
					resultsCounted = resultsCounted && CountResults(results, entityTP, entityFP, entityFN);
				}
			}
			if (wrapper != null)
			{
				wrapper.Join();
				while (wrapper.Peek())
				{
					IList<In> results = wrapper.Poll();
					WriteAnswers(results, printWriter, readerWriter);
					resultsCounted = resultsCounted && CountResults(results, entityTP, entityFP, entityFN);
				}
			}
			long millis = timer.Stop();
			double wordspersec = numWords / (((double)millis) / 1000);
			NumberFormat nf = new DecimalFormat("0.00");
			// easier way!
			log.Info(StringUtils.GetShortClassName(this) + " tagged " + numWords + " words in " + numDocs + " documents at " + nf.Format(wordspersec) + " words per second.");
			if (outputScores)
			{
				return PrintResults(entityTP, entityFP, entityFN);
			}
			else
			{
				return null;
			}
		}

		private sealed class _IThreadsafeProcessor_1169 : IThreadsafeProcessor<IList<In>, IList<In>>
		{
			public _IThreadsafeProcessor_1169(AbstractSequenceClassifier<In> _enclosing, AtomicInteger threadCompletionCounter)
			{
				this._enclosing = _enclosing;
				this.threadCompletionCounter = threadCompletionCounter;
			}

			public IList<In> Process(IList<In> doc)
			{
				doc = this._enclosing.Classify(doc);
				int completedNo = threadCompletionCounter.IncrementAndGet();
				if (this._enclosing.flags.verboseMode)
				{
					Edu.Stanford.Nlp.IE.AbstractSequenceClassifier.log.Info(completedNo + " examples completed");
				}
				return doc;
			}

			public IThreadsafeProcessor<IList<In>, IList<In>> NewInstance()
			{
				return this;
			}

			private readonly AbstractSequenceClassifier<In> _enclosing;

			private readonly AtomicInteger threadCompletionCounter;
		}

		/// <summary>
		/// Load a test file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr).
		/// </summary>
		/// <remarks>
		/// Load a test file, run the classifier on it, and then print the answers to
		/// stdout (with timing to stderr). This uses the value of flags.documentReader
		/// to determine testFile format.
		/// </remarks>
		/// <param name="testFile">The name of the file to test on.</param>
		/// <param name="k">How many best to print</param>
		/// <param name="readerAndWriter">Class to be used for printing answers</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyAndWriteAnswersKBest(string testFile, int k, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			ObjectBank<IList<In>> documents = MakeObjectBankFromFile(testFile, readerAndWriter);
			PrintWriter pw = IOUtils.EncodedOutputStreamPrintWriter(System.Console.Out, flags.outputEncoding, true);
			ClassifyAndWriteAnswersKBest(documents, k, pw, readerAndWriter);
			pw.Flush();
		}

		/// <summary>
		/// Run the classifier on the documents in an ObjectBank, and print the
		/// answers to a given PrintWriter (with timing to stderr).
		/// </summary>
		/// <remarks>
		/// Run the classifier on the documents in an ObjectBank, and print the
		/// answers to a given PrintWriter (with timing to stderr). The value of
		/// flags.documentReader is used to determine testFile format.
		/// </remarks>
		/// <param name="documents">The ObjectBank to test on.</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyAndWriteAnswersKBest(ObjectBank<IList<In>> documents, int k, PrintWriter printWriter, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			Timing timer = new Timing();
			int numWords = 0;
			int numSentences = 0;
			foreach (IList<In> doc in documents)
			{
				ICounter<IList<In>> kBest = ClassifyKBest(doc, typeof(CoreAnnotations.AnswerAnnotation), k);
				numWords += doc.Count;
				IList<IList<In>> sorted = Counters.ToSortedList(kBest);
				int n = 1;
				foreach (IList<In> l in sorted)
				{
					printWriter.Println("<sentence id=" + numSentences + " k=" + n + " logProb=" + kBest.GetCount(l) + " prob=" + Math.Exp(kBest.GetCount(l)) + '>');
					WriteAnswers(l, printWriter, readerAndWriter);
					printWriter.Println("</sentence>");
					n++;
				}
				numSentences++;
			}
			long millis = timer.Stop();
			double wordspersec = numWords / (((double)millis) / 1000);
			NumberFormat nf = new DecimalFormat("0.00");
			// easier way!
			log.Info(this.GetType().FullName + " tagged " + numWords + " words in " + numSentences + " documents at " + nf.Format(wordspersec) + " words per second.");
		}

		/// <summary>
		/// Load a test file, run the classifier on it, and then write a Viterbi search
		/// graph for each sequence.
		/// </summary>
		/// <param name="testFile">The file to test on.</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ClassifyAndWriteViterbiSearchGraph(string testFile, string searchGraphPrefix, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			Timing timer = new Timing();
			ObjectBank<IList<In>> documents = MakeObjectBankFromFile(testFile, readerAndWriter);
			int numWords = 0;
			int numSentences = 0;
			foreach (IList<In> doc in documents)
			{
				DFSA<string, int> tagLattice = GetViterbiSearchGraph(doc, typeof(CoreAnnotations.AnswerAnnotation));
				numWords += doc.Count;
				PrintWriter latticeWriter = new PrintWriter(new FileOutputStream(searchGraphPrefix + '.' + numSentences + ".wlattice"));
				PrintWriter vsgWriter = new PrintWriter(new FileOutputStream(searchGraphPrefix + '.' + numSentences + ".lattice"));
				if (readerAndWriter is ILatticeWriter)
				{
					((ILatticeWriter<IN, string, int>)readerAndWriter).PrintLattice(tagLattice, doc, latticeWriter);
				}
				tagLattice.PrintAttFsmFormat(vsgWriter);
				latticeWriter.Close();
				vsgWriter.Close();
				numSentences++;
			}
			long millis = timer.Stop();
			double wordspersec = numWords / (((double)millis) / 1000);
			NumberFormat nf = new DecimalFormat("0.00");
			// easier way!
			log.Info(this.GetType().FullName + " tagged " + numWords + " words in " + numSentences + " documents at " + nf.Format(wordspersec) + " words per second.");
		}

		/// <summary>
		/// Write the classifications of the Sequence classifier to a writer in a
		/// format determined by the DocumentReaderAndWriter used.
		/// </summary>
		/// <param name="doc">Documents to write out</param>
		/// <param name="printWriter">Writer to use for output</param>
		/// <exception cref="System.IO.IOException">If an IO problem</exception>
		public virtual void WriteAnswers(IList<In> doc, PrintWriter printWriter, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			if (flags.lowerNewgeneThreshold)
			{
				return;
			}
			if (flags.numRuns <= 1)
			{
				readerAndWriter.PrintAnswers(doc, printWriter);
				// out.println();
				printWriter.Flush();
			}
		}

		/// <summary>Count results using a method appropriate for the tag scheme being used.</summary>
		public virtual bool CountResults(IList<In> doc, ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN)
		{
			string bg = (flags.evaluateBackground ? null : flags.backgroundSymbol);
			if (flags.sighanPostProcessing)
			{
				// TODO: this is extremely indicative of being a Chinese Segmenter,
				// but it would still be better to have something more concrete
				return CountResultsSegmenter(doc, entityTP, entityFP, entityFN);
			}
			return IOBUtils.CountEntityResults(doc, entityTP, entityFP, entityFN, bg);
		}

		private const string CutLabel = "Cut";

		// TODO: could make this a parameter for the model
		public static bool CountResultsSegmenter<_T0>(IList<_T0> doc, ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN)
			where _T0 : ICoreMap
		{
			// count from 1 because each label represents cutting or
			// not cutting at a word, so we don't count the first word
			for (int i = 1; i < doc.Count; ++i)
			{
				ICoreMap word = doc[i];
				string gold = word.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
				string guess = word.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if (gold == null || guess == null)
				{
					return false;
				}
				if (gold.Equals("1") && guess.Equals("1"))
				{
					entityTP.IncrementCount(CutLabel, 1.0);
				}
				else
				{
					if (gold.Equals("0") && guess.Equals("1"))
					{
						entityFP.IncrementCount(CutLabel, 1.0);
					}
					else
					{
						if (gold.Equals("1") && guess.Equals("0"))
						{
							entityFN.IncrementCount(CutLabel, 1.0);
						}
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Given counters of true positives, false positives, and false
		/// negatives, prints out precision, recall, and f1 for each key.
		/// </summary>
		public static Triple<double, double, double> PrintResults(ICounter<string> entityTP, ICounter<string> entityFP, ICounter<string> entityFN)
		{
			ICollection<string> entities = new TreeSet<string>();
			Sharpen.Collections.AddAll(entities, entityTP.KeySet());
			Sharpen.Collections.AddAll(entities, entityFP.KeySet());
			Sharpen.Collections.AddAll(entities, entityFN.KeySet());
			log.Info("         Entity\tP\tR\tF1\tTP\tFP\tFN");
			foreach (string entity in entities)
			{
				double tp = entityTP.GetCount(entity);
				double fp = entityFP.GetCount(entity);
				double fn = entityFN.GetCount(entity);
				PrintPRLine(entity, tp, fp, fn);
			}
			double tp_1 = entityTP.TotalCount();
			double fp_1 = entityFP.TotalCount();
			double fn_1 = entityFN.TotalCount();
			return PrintPRLine("Totals", tp_1, fp_1, fn_1);
		}

		/// <summary>Print a line of precision, recall, and f1 scores, titled by entity.</summary>
		/// <returns>A Triple of the P/R/F, done on a 0-100 scale like percentages</returns>
		private static Triple<double, double, double> PrintPRLine(string entity, double tp, double fp, double fn)
		{
			double precision = (tp == 0.0 && fp == 0.0) ? 0.0 : tp / (tp + fp);
			double recall = (tp == 0.0 && fn == 0.0) ? 1.0 : tp / (tp + fn);
			double f1 = ((precision == 0.0 || recall == 0.0) ? 0.0 : 2.0 / (1.0 / precision + 1.0 / recall));
			log.Info(string.Format("%15s\t%.4f\t%.4f\t%.4f\t%.0f\t%.0f\t%.0f%n", entity, precision, recall, f1, tp, fp, fn));
			return new Triple<double, double, double>(precision * 100, recall * 100, f1 * 100);
		}

		/// <summary>Serialize a sequence classifier to a file on the given path.</summary>
		/// <param name="serializePath">The path/filename to write the classifier to.</param>
		public abstract void SerializeClassifier(string serializePath);

		/// <summary>Serialize a sequence classifier to an object output stream</summary>
		public abstract void SerializeClassifier(ObjectOutputStream oos);

		/// <summary>Loads a classifier from the given input stream.</summary>
		/// <remarks>
		/// Loads a classifier from the given input stream.
		/// Any exceptions are rethrown as unchecked exceptions.
		/// This method does not close the InputStream.
		/// </remarks>
		/// <param name="in">The InputStream to read from</param>
		public virtual void LoadClassifierNoExceptions(InputStream @in, Properties props)
		{
			// load the classifier
			try
			{
				LoadClassifier(@in, props);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			catch (TypeLoadException cnfe)
			{
				throw new Exception(cnfe);
			}
		}

		/// <summary>Load a classifier from the specified InputStream.</summary>
		/// <remarks>
		/// Load a classifier from the specified InputStream. No extra properties are
		/// supplied. This does not close the InputStream.
		/// </remarks>
		/// <param name="in">The InputStream to load the serialized classifier from</param>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public virtual void LoadClassifier(InputStream @in)
		{
			LoadClassifier(@in, null);
		}

		/// <summary>Load a classifier from the specified InputStream.</summary>
		/// <remarks>
		/// Load a classifier from the specified InputStream. The classifier is
		/// reinitialized from the flags serialized in the classifier. This does not
		/// close the InputStream.
		/// </remarks>
		/// <param name="in">The InputStream to load the serialized classifier from</param>
		/// <param name="props">
		/// This Properties object will be used to update the
		/// SeqClassifierFlags which are read from the serialized classifier
		/// </param>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public virtual void LoadClassifier(InputStream @in, Properties props)
		{
			LoadClassifier(new ObjectInputStream(@in), props);
		}

		/// <summary>Load a classifier from the specified input stream.</summary>
		/// <remarks>
		/// Load a classifier from the specified input stream. The classifier is
		/// reinitialized from the flags serialized in the classifier.
		/// </remarks>
		/// <param name="in">The InputStream to load the serialized classifier from</param>
		/// <param name="props">
		/// This Properties object will be used to update the
		/// SeqClassifierFlags which are read from the serialized classifier
		/// </param>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public abstract void LoadClassifier(ObjectInputStream @in, Properties props);

		/// <summary>Loads a classifier from the file specified by loadPath.</summary>
		/// <remarks>
		/// Loads a classifier from the file specified by loadPath. If loadPath ends in
		/// .gz, uses a GZIPInputStream, else uses a regular FileInputStream.
		/// </remarks>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void LoadClassifier(string loadPath)
		{
			LoadClassifier(loadPath, null);
		}

		/// <summary>Loads a classifier from the file, classpath resource, or URL specified by loadPath.</summary>
		/// <remarks>
		/// Loads a classifier from the file, classpath resource, or URL specified by loadPath. If loadPath ends in
		/// .gz, uses a GZIPInputStream.
		/// </remarks>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void LoadClassifier(string loadPath, Properties props)
		{
			using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(loadPath))
			{
				Timing t = new Timing();
				LoadClassifier(@is, props);
				t.Done(log, "Loading classifier from " + loadPath);
			}
		}

		public virtual void LoadClassifierNoExceptions(string loadPath)
		{
			LoadClassifierNoExceptions(loadPath, null);
		}

		public virtual void LoadClassifierNoExceptions(string loadPath, Properties props)
		{
			try
			{
				LoadClassifier(loadPath, props);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public virtual void LoadClassifier(File file)
		{
			LoadClassifier(file, null);
		}

		/// <summary>Loads a classifier from the file specified.</summary>
		/// <remarks>
		/// Loads a classifier from the file specified. If the file's name ends in .gz,
		/// uses a GZIPInputStream, else uses a regular FileInputStream. This method
		/// closes the File when done.
		/// </remarks>
		/// <param name="file">Loads a classifier from this file.</param>
		/// <param name="props">
		/// Properties in this object will be used to overwrite those
		/// specified in the serialized classifier
		/// </param>
		/// <exception cref="System.IO.IOException">If there are problems accessing the input stream</exception>
		/// <exception cref="System.InvalidCastException">If there are problems interpreting the serialized data</exception>
		/// <exception cref="System.TypeLoadException">If there are problems interpreting the serialized data</exception>
		public virtual void LoadClassifier(File file, Properties props)
		{
			Timing t = new Timing();
			BufferedInputStream bis;
			if (file.GetName().EndsWith(".gz"))
			{
				bis = new BufferedInputStream(new GZIPInputStream(new FileInputStream(file)));
			}
			else
			{
				bis = new BufferedInputStream(new FileInputStream(file));
			}
			try
			{
				LoadClassifier(bis, props);
				t.Done(log, "Loading classifier from " + file.GetAbsolutePath());
			}
			finally
			{
				bis.Close();
			}
		}

		public virtual void LoadClassifierNoExceptions(File file)
		{
			LoadClassifierNoExceptions(file, null);
		}

		public virtual void LoadClassifierNoExceptions(File file, Properties props)
		{
			try
			{
				LoadClassifier(file, props);
			}
			catch (Exception e)
			{
				log.Info("Error deserializing " + file.GetAbsolutePath());
				throw new Exception(e);
			}
		}

		[System.NonSerialized]
		private PrintWriter cliqueWriter;

		[System.NonSerialized]
		private int writtenNum;

		// = 0;
		/// <summary>Print the String features generated from a IN</summary>
		protected internal virtual void PrintFeatures(IN wi, ICollection<string> features)
		{
			if (flags.printFeatures == null || writtenNum >= flags.printFeaturesUpto)
			{
				return;
			}
			if (cliqueWriter == null)
			{
				cliqueWriter = IOUtils.GetPrintWriterOrDie("features-" + flags.printFeatures + ".txt");
				writtenNum = 0;
			}
			if (wi is CoreLabel)
			{
				cliqueWriter.Print(wi.Get(typeof(CoreAnnotations.TextAnnotation)) + ' ' + wi.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)) + ' ' + wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)) + '\t');
			}
			else
			{
				cliqueWriter.Print(wi.Get(typeof(CoreAnnotations.TextAnnotation)) + wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)) + '\t');
			}
			bool first = true;
			IList<string> featsList = new List<string>(features);
			featsList.Sort();
			foreach (string feat in featsList)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					cliqueWriter.Print(" ");
				}
				cliqueWriter.Print(feat);
			}
			cliqueWriter.Println();
			writtenNum++;
		}

		/// <summary>Print the String features generated from a token.</summary>
		protected internal virtual void PrintFeatureLists(IN wi, ICollection<IList<string>> features)
		{
			if (flags.printFeatures == null || writtenNum >= flags.printFeaturesUpto)
			{
				return;
			}
			PrintFeatureListsHelper(wi, features);
		}

		// Separating this method out lets printFeatureLists be inlined, which is good since it is usually a no-op.
		private void PrintFeatureListsHelper(IN wi, ICollection<IList<string>> features)
		{
			if (cliqueWriter == null)
			{
				cliqueWriter = IOUtils.GetPrintWriterOrDie("features-" + flags.printFeatures + ".txt");
				writtenNum = 0;
			}
			if (wi is CoreLabel)
			{
				cliqueWriter.Print(wi.Get(typeof(CoreAnnotations.TextAnnotation)) + ' ' + wi.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)) + ' ' + wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)) + '\t');
			}
			else
			{
				cliqueWriter.Print(wi.Get(typeof(CoreAnnotations.TextAnnotation)) + wi.Get(typeof(CoreAnnotations.GoldAnswerAnnotation)) + '\t');
			}
			bool first = true;
			foreach (IList<string> featList in features)
			{
				IList<string> sortedFeatList = new List<string>(featList);
				sortedFeatList.Sort();
				foreach (string feat in sortedFeatList)
				{
					if (first)
					{
						first = false;
					}
					else
					{
						cliqueWriter.Print(" ");
					}
					cliqueWriter.Print(feat);
				}
				cliqueWriter.Print("  ");
			}
			cliqueWriter.Println();
			writtenNum++;
		}

		public virtual int WindowSize()
		{
			return windowSize;
		}
	}
}

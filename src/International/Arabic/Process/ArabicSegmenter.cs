using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic.Process
{
	/// <summary>Arabic word segmentation model based on conditional random fields (CRF).</summary>
	/// <remarks>
	/// Arabic word segmentation model based on conditional random fields (CRF).
	/// This is a re-implementation (with extensions) of the model described in
	/// (Green and DeNero, 2012).
	/// This package includes a JFlex-based orthographic normalization package
	/// that runs on the input prior to processing by the CRF-based segmentation
	/// model. The normalization options are configurable, but must be consistent for
	/// both training and test data.
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicSegmenter : IWordSegmenter, IThreadsafeProcessor<string, string>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter));

		private const long serialVersionUID = -4791848633597417788L;

		private const string optTokenized = "tokenized";

		private const string optTokenizer = "orthoOptions";

		private const string optPrefix = "prefixMarker";

		private const string optSuffix = "suffixMarker";

		private const string optThreads = "nthreads";

		private const string optTedEval = "tedEval";

		private const string optFeatureFactory = "featureFactory";

		private const string defaultFeatureFactory = "edu.stanford.nlp.international.arabic.process.StartAndEndArabicSegmenterFeatureFactory";

		private const string localOnlyFeatureFactory = "edu.stanford.nlp.international.arabic.process.ArabicSegmenterFeatureFactory";

		private const string optWithDomains = "withDomains";

		private const string optDomain = "domain";

		private const string optNoRewrites = "noRewrites";

		private const string optLocalFeaturesOnly = "localFeaturesOnly";

		[System.NonSerialized]
		private CRFClassifier<CoreLabel> classifier;

		private readonly SeqClassifierFlags flags;

		private readonly ITokenizerFactory<CoreLabel> tf;

		private readonly string prefixMarker;

		private readonly string suffixMarker;

		private readonly bool isTokenized;

		private readonly string tokenizerOptions;

		private readonly string tedEvalPrefix;

		private readonly bool hasDomainLabels;

		private readonly string domain;

		private readonly bool noRewrites;

		/// <summary>Make an Arabic Segmenter.</summary>
		/// <param name="props">
		/// Options for how to tokenize. See the main method of
		/// <see cref="ArabicTokenizer{T}"/>
		/// for details
		/// </param>
		public ArabicSegmenter(Properties props)
		{
			/* Serializable */
			// SEGMENTER OPTIONS (can be set in the Properties object
			// passed to the constructor).
			// The input already been tokenized. Do not run the Arabic tokenizer.
			// Tokenizer options
			// Mark segmented prefixes with this String
			// Mark segmented suffixes with this String
			// Number of decoding threads
			// Write TedEval files
			// Use a custom feature factory
			// Training and evaluation files have domain labels
			// Training and evaluation text are all in the same domain (default:atb)
			// Ignore rewrites (training only, produces a model that then can be used to do
			// no-rewrite segmentation)
			// Use the original feature set which doesn't contain start-and-end "wrapper" features
			isTokenized = props.Contains(optTokenized);
			tokenizerOptions = props.GetProperty(optTokenizer, null);
			tedEvalPrefix = props.GetProperty(optTedEval, null);
			hasDomainLabels = props.Contains(optWithDomains);
			domain = props.GetProperty(optDomain, "atb");
			noRewrites = props.Contains(optNoRewrites);
			tf = GetTokenizerFactory();
			prefixMarker = props.GetProperty(optPrefix, string.Empty);
			suffixMarker = props.GetProperty(optSuffix, string.Empty);
			if (props.Contains(optLocalFeaturesOnly))
			{
				if (props.Contains(optFeatureFactory))
				{
					throw new Exception("Cannot use custom feature factory with localFeaturesOnly flag--" + "have your custom feature factory extend ArabicSegmenterFeatureFactory instead of " + "StartAndEndArabicSegmenterFeatureFactory and remove the localFeaturesOnly flag."
						);
				}
				props.SetProperty(optFeatureFactory, localOnlyFeatureFactory);
			}
			if (!props.Contains(optFeatureFactory))
			{
				props.SetProperty(optFeatureFactory, defaultFeatureFactory);
			}
			// Remove all command-line properties that are specific to ArabicSegmenter
			props.Remove(optTokenizer);
			props.Remove(optTokenized);
			props.Remove(optPrefix);
			props.Remove(optSuffix);
			props.Remove(optThreads);
			props.Remove(optTedEval);
			props.Remove(optWithDomains);
			props.Remove(optDomain);
			props.Remove(optNoRewrites);
			props.Remove(optLocalFeaturesOnly);
			flags = new SeqClassifierFlags(props);
			classifier = new CRFClassifier<CoreLabel>(flags);
		}

		/// <summary>Copy constructor.</summary>
		/// <param name="other"/>
		public ArabicSegmenter(Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter other)
		{
			isTokenized = other.isTokenized;
			tokenizerOptions = other.tokenizerOptions;
			prefixMarker = other.prefixMarker;
			suffixMarker = other.suffixMarker;
			tedEvalPrefix = other.tedEvalPrefix;
			hasDomainLabels = other.hasDomainLabels;
			domain = other.domain;
			noRewrites = other.noRewrites;
			flags = other.flags;
			// ArabicTokenizerFactory is *not* threadsafe. Make a new copy.
			tf = GetTokenizerFactory();
			// CRFClassifier is threadsafe, so return a reference.
			classifier = other.classifier;
		}

		/// <summary>Creates an ArabicTokenizer.</summary>
		/// <remarks>
		/// Creates an ArabicTokenizer. The default tokenizer
		/// is ArabicTokenizer.atbFactory(), which produces the
		/// same orthographic normalization as Green and Manning (2010).
		/// </remarks>
		/// <returns>A TokenizerFactory that produces each Arabic token as a CoreLabel</returns>
		private ITokenizerFactory<CoreLabel> GetTokenizerFactory()
		{
			ITokenizerFactory<CoreLabel> tokFactory = null;
			if (!isTokenized)
			{
				if (tokenizerOptions == null)
				{
					tokFactory = ArabicTokenizer.AtbFactory();
					string atbVocOptions = "removeProMarker,removeMorphMarker,removeLengthening";
					tokFactory.SetOptions(atbVocOptions);
				}
				else
				{
					if (tokenizerOptions.Contains("removeSegMarker"))
					{
						throw new Exception("Option 'removeSegMarker' cannot be used with ArabicSegmenter");
					}
					tokFactory = ArabicTokenizer.Factory();
					tokFactory.SetOptions(tokenizerOptions);
				}
				log.Info("Loaded ArabicTokenizer with options: " + tokenizerOptions);
			}
			return tokFactory;
		}

		public virtual void InitializeTraining(double numTrees)
		{
			throw new NotSupportedException("Training is not supported!");
		}

		public virtual void Train(ICollection<Tree> trees)
		{
			throw new NotSupportedException("Training is not supported!");
		}

		public virtual void Train(Tree tree)
		{
			throw new NotSupportedException("Training is not supported!");
		}

		public virtual void Train(IList<TaggedWord> sentence)
		{
			throw new NotSupportedException("Training is not supported!");
		}

		public virtual void FinishTraining()
		{
			throw new NotSupportedException("Training is not supported!");
		}

		public virtual string Process(string nextInput)
		{
			return SegmentString(nextInput);
		}

		public virtual IThreadsafeProcessor<string, string> NewInstance()
		{
			return new Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter(this);
		}

		public virtual IList<IHasWord> Segment(string line)
		{
			string segmentedString = SegmentString(line);
			return SentenceUtils.ToWordList(segmentedString.Split("\\s+"));
		}

		private IList<CoreLabel> SegmentStringToIOB(string line)
		{
			IList<CoreLabel> tokenList;
			if (tf == null)
			{
				// Whitespace tokenization.
				tokenList = IOBUtils.StringToIOB(line);
			}
			else
			{
				IList<CoreLabel> tokens = tf.GetTokenizer(new StringReader(line)).Tokenize();
				tokenList = IOBUtils.StringToIOB(tokens, null, false, tf, line);
			}
			IOBUtils.LabelDomain(tokenList, domain);
			tokenList = classifier.Classify(tokenList);
			return tokenList;
		}

		public virtual IList<CoreLabel> SegmentStringToTokenList(string line)
		{
			IList<CoreLabel> tokenList = CollectionUtils.MakeList();
			IList<CoreLabel> labeledSequence = SegmentStringToIOB(line);
			foreach (IntPair span in IOBUtils.TokenSpansForIOB(labeledSequence))
			{
				CoreLabel token = new CoreLabel();
				string text = IOBUtils.IOBToString(labeledSequence, prefixMarker, suffixMarker, span.GetSource(), span.GetTarget());
				token.SetWord(text);
				token.SetValue(text);
				token.Set(typeof(CoreAnnotations.TextAnnotation), text);
				token.Set(typeof(CoreAnnotations.ArabicSegAnnotation), "1");
				int start = labeledSequence[span.GetSource()].BeginPosition();
				int end = labeledSequence[span.GetTarget() - 1].EndPosition();
				token.SetOriginalText(Sharpen.Runtime.Substring(line, start, end));
				token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), start);
				token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), end);
				tokenList.Add(token);
			}
			return tokenList;
		}

		public virtual string SegmentString(string line)
		{
			IList<CoreLabel> labeledSequence = SegmentStringToIOB(line);
			string segmentedString = IOBUtils.IOBToString(labeledSequence, prefixMarker, suffixMarker);
			return segmentedString;
		}

		/// <summary>Segment all strings from an input.</summary>
		/// <param name="br">-- input stream to segment</param>
		/// <param name="pwOut">-- output stream to write the segmenter text</param>
		/// <returns>number of input characters segmented</returns>
		public virtual long Segment(BufferedReader br, PrintWriter pwOut)
		{
			long nSegmented = 0;
			try
			{
				for (string line; (line = br.ReadLine()) != null; )
				{
					nSegmented += line.Length;
					// Measure this quantity since it is quick to compute
					string segmentedLine = SegmentString(line);
					pwOut.Println(segmentedLine);
				}
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return nSegmented;
		}

		/// <summary>Train a segmenter from raw text.</summary>
		/// <remarks>Train a segmenter from raw text. Gold segmentation markers are required.</remarks>
		public virtual void Train()
		{
			bool hasSegmentationMarkers = true;
			bool hasTags = true;
			IDocumentReaderAndWriter<CoreLabel> docReader = new ArabicDocumentReaderAndWriter(hasSegmentationMarkers, hasTags, hasDomainLabels, domain, noRewrites, tf);
			ObjectBank<IList<CoreLabel>> lines = classifier.MakeObjectBankFromFile(flags.trainFile, docReader);
			classifier.Train(lines, docReader);
			log.Info("Finished training.");
		}

		/// <summary>
		/// Evaluate accuracy when the input is gold segmented text *with* segmentation
		/// markers and morphological analyses.
		/// </summary>
		/// <remarks>
		/// Evaluate accuracy when the input is gold segmented text *with* segmentation
		/// markers and morphological analyses. In other words, the evaluation file has the
		/// same format as the training data.
		/// </remarks>
		/// <param name="pwOut"/>
		private void Evaluate(PrintWriter pwOut)
		{
			log.Info("Starting evaluation...");
			bool hasSegmentationMarkers = true;
			bool hasTags = true;
			IDocumentReaderAndWriter<CoreLabel> docReader = new ArabicDocumentReaderAndWriter(hasSegmentationMarkers, hasTags, hasDomainLabels, domain, tf);
			ObjectBank<IList<CoreLabel>> lines = classifier.MakeObjectBankFromFile(flags.testFile, docReader);
			PrintWriter tedEvalGoldTree = null;
			PrintWriter tedEvalParseTree = null;
			PrintWriter tedEvalGoldSeg = null;
			PrintWriter tedEvalParseSeg = null;
			if (tedEvalPrefix != null)
			{
				try
				{
					tedEvalGoldTree = new PrintWriter(tedEvalPrefix + "_gold.ftree");
					tedEvalGoldSeg = new PrintWriter(tedEvalPrefix + "_gold.segmentation");
					tedEvalParseTree = new PrintWriter(tedEvalPrefix + "_parse.ftree");
					tedEvalParseSeg = new PrintWriter(tedEvalPrefix + "_parse.segmentation");
				}
				catch (FileNotFoundException e)
				{
					System.Console.Error.Printf("%s: %s%n", typeof(Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter).FullName, e.Message);
				}
			}
			ICounter<string> labelTotal = new ClassicCounter<string>();
			ICounter<string> labelCorrect = new ClassicCounter<string>();
			int total = 0;
			int correct = 0;
			foreach (IList<CoreLabel> line in lines)
			{
				string[] inputTokens = TedEvalSanitize(IOBUtils.IOBToString(line).ReplaceAll(":", "#pm#")).Split(" ");
				string[] goldTokens = TedEvalSanitize(IOBUtils.IOBToString(line, ":")).Split(" ");
				line = classifier.Classify(line);
				string[] parseTokens = TedEvalSanitize(IOBUtils.IOBToString(line, ":")).Split(" ");
				foreach (CoreLabel label in line)
				{
					// Do not evaluate labeling of whitespace
					string observation = label.Get(typeof(CoreAnnotations.CharAnnotation));
					if (!observation.Equals(IOBUtils.GetBoundaryCharacter()))
					{
						total++;
						string hypothesis = label.Get(typeof(CoreAnnotations.AnswerAnnotation));
						string reference = label.Get(typeof(CoreAnnotations.GoldAnswerAnnotation));
						labelTotal.IncrementCount(reference);
						if (hypothesis.Equals(reference))
						{
							correct++;
							labelCorrect.IncrementCount(reference);
						}
					}
				}
				if (tedEvalParseSeg != null)
				{
					tedEvalGoldTree.Printf("(root");
					tedEvalParseTree.Printf("(root");
					int safeLength = inputTokens.Length;
					if (inputTokens.Length != goldTokens.Length)
					{
						log.Info("In generating TEDEval files: Input and gold do not have the same number of tokens");
						log.Info("    (ignoring any extras)");
						log.Info("  input: " + Arrays.ToString(inputTokens));
						log.Info("  gold: " + Arrays.ToString(goldTokens));
						safeLength = Math.Min(inputTokens.Length, goldTokens.Length);
					}
					if (inputTokens.Length != parseTokens.Length)
					{
						log.Info("In generating TEDEval files: Input and parse do not have the same number of tokens");
						log.Info("    (ignoring any extras)");
						log.Info("  input: " + Arrays.ToString(inputTokens));
						log.Info("  parse: " + Arrays.ToString(parseTokens));
						safeLength = Math.Min(inputTokens.Length, parseTokens.Length);
					}
					for (int i = 0; i < safeLength; i++)
					{
						foreach (string segment in goldTokens[i].Split(":"))
						{
							tedEvalGoldTree.Printf(" (seg %s)", segment);
						}
						tedEvalGoldSeg.Printf("%s\t%s%n", inputTokens[i], goldTokens[i]);
						foreach (string segment_1 in parseTokens[i].Split(":"))
						{
							tedEvalParseTree.Printf(" (seg %s)", segment_1);
						}
						tedEvalParseSeg.Printf("%s\t%s%n", inputTokens[i], parseTokens[i]);
					}
					tedEvalGoldTree.Printf(")%n");
					tedEvalGoldSeg.Println();
					tedEvalParseTree.Printf(")%n");
					tedEvalParseSeg.Println();
				}
			}
			double accuracy = ((double)correct) / ((double)total);
			accuracy *= 100.0;
			pwOut.Println("EVALUATION RESULTS");
			pwOut.Printf("#datums:\t%d%n", total);
			pwOut.Printf("#correct:\t%d%n", correct);
			pwOut.Printf("accuracy:\t%.2f%n", accuracy);
			pwOut.Println("==================");
			// Output the per label accuracies
			pwOut.Println("PER LABEL ACCURACIES");
			foreach (string refLabel in labelTotal.KeySet())
			{
				double nTotal = labelTotal.GetCount(refLabel);
				double nCorrect = labelCorrect.GetCount(refLabel);
				double acc = (nCorrect / nTotal) * 100.0;
				pwOut.Printf(" %s\t%.2f%n", refLabel, acc);
			}
			if (tedEvalParseSeg != null)
			{
				tedEvalGoldTree.Close();
				tedEvalGoldSeg.Close();
				tedEvalParseTree.Close();
				tedEvalParseSeg.Close();
			}
		}

		private static string TedEvalSanitize(string str)
		{
			return str.ReplaceAll("\\(", "#lp#").ReplaceAll("\\)", "#rp#");
		}

		/// <summary>Evaluate P/R/F1 when the input is raw text.</summary>
		private static void EvaluateRawText(PrintWriter pwOut)
		{
			// TODO(spenceg): Evaluate raw input w.r.t. a reference that might have different numbers
			// of characters per sentence. Need to implement a monotonic sequence alignment algorithm
			// to align the two character strings.
			//    String gold = flags.answerFile;
			//    String rawFile = flags.testFile;
			throw new Exception("Not yet implemented!");
		}

		public virtual void SerializeSegmenter(string filename)
		{
			classifier.SerializeClassifier(filename);
		}

		public virtual void LoadSegmenter(string filename, Properties p)
		{
			try
			{
				classifier = CRFClassifier.GetClassifier(filename, p);
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Failed to load segmenter " + filename, e);
			}
		}

		public virtual void LoadSegmenter(string filename)
		{
			LoadSegmenter(filename, new Properties());
		}

		private static string Usage()
		{
			string nl = Runtime.LineSeparator();
			StringBuilder sb = new StringBuilder();
			sb.Append("Usage: java ").Append(typeof(Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter).FullName).Append(" OPTS < file_to_segment").Append(nl);
			sb.Append(nl).Append(" Options:").Append(nl);
			sb.Append("  -help                : Print this message.").Append(nl);
			sb.Append("  -orthoOptions str    : Comma-separated list of orthographic normalization options to pass to ArabicTokenizer.").Append(nl);
			sb.Append("  -tokenized           : Text is already tokenized. Do not run internal tokenizer.").Append(nl);
			sb.Append("  -trainFile file      : Gold segmented IOB training file.").Append(nl);
			sb.Append("  -testFile  file      : Gold segmented IOB evaluation file.").Append(nl);
			sb.Append("  -textFile  file      : Raw input file to be segmented.").Append(nl);
			sb.Append("  -loadClassifier file : Load serialized classifier from file.").Append(nl);
			sb.Append("  -prefixMarker char   : Mark segmented prefixes with specified character.").Append(nl);
			sb.Append("  -suffixMarker char   : Mark segmented suffixes with specified character.").Append(nl);
			sb.Append("  -nthreads num        : Number of threads  (default: 1)").Append(nl);
			sb.Append("  -tedEval prefix      : Output TedEval-compliant gold and parse files.").Append(nl);
			sb.Append("  -featureFactory cls  : Name of feature factory class  (default: ").Append(defaultFeatureFactory);
			sb.Append(")").Append(nl);
			sb.Append("  -withDomains         : Train file (if given) and eval file have domain labels.").Append(nl);
			sb.Append("  -domain dom          : Assume one domain for all data (default: 123)").Append(nl);
			sb.Append(nl).Append(" Otherwise, all flags correspond to those present in SeqClassifierFlags.java.").Append(nl);
			return sb.ToString();
		}

		private static IDictionary<string, int> OptionArgDefs()
		{
			IDictionary<string, int> optionArgDefs = Generics.NewHashMap();
			optionArgDefs["help"] = 0;
			optionArgDefs["orthoOptions"] = 1;
			optionArgDefs["tokenized"] = 0;
			optionArgDefs["trainFile"] = 1;
			optionArgDefs["testFile"] = 1;
			optionArgDefs["textFile"] = 1;
			optionArgDefs["loadClassifier"] = 1;
			optionArgDefs["prefixMarker"] = 1;
			optionArgDefs["suffixMarker"] = 1;
			optionArgDefs["nthreads"] = 1;
			optionArgDefs["tedEval"] = 1;
			optionArgDefs["featureFactory"] = 1;
			optionArgDefs["withDomains"] = 0;
			optionArgDefs["domain"] = 1;
			return optionArgDefs;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			// Strips off hyphens
			Properties options = StringUtils.ArgsToProperties(args, OptionArgDefs());
			if (options.Contains("help") || args.Length == 0)
			{
				log.Info(Usage());
				System.Environment.Exit(-1);
			}
			int nThreads = PropertiesUtils.GetInt(options, "nthreads", 1);
			Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter segmenter = GetSegmenter(options);
			// Decode either an evaluation file or raw text
			try
			{
				PrintWriter pwOut;
				if (segmenter.flags.outputEncoding != null)
				{
					OutputStreamWriter @out = new OutputStreamWriter(System.Console.Out, segmenter.flags.outputEncoding);
					pwOut = new PrintWriter(@out, true);
				}
				else
				{
					if (segmenter.flags.inputEncoding != null)
					{
						OutputStreamWriter @out = new OutputStreamWriter(System.Console.Out, segmenter.flags.inputEncoding);
						pwOut = new PrintWriter(@out, true);
					}
					else
					{
						pwOut = new PrintWriter(System.Console.Out, true);
					}
				}
				if (segmenter.flags.testFile != null)
				{
					if (segmenter.flags.answerFile == null)
					{
						segmenter.Evaluate(pwOut);
					}
					else
					{
						Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter.EvaluateRawText(pwOut);
					}
				}
				else
				{
					BufferedReader br = (segmenter.flags.textFile == null) ? IOUtils.ReaderFromStdin() : IOUtils.ReaderFromString(segmenter.flags.textFile, segmenter.flags.inputEncoding);
					double charsPerSec = Decode(segmenter, br, pwOut, nThreads);
					IOUtils.CloseIgnoringExceptions(br);
					System.Console.Error.Printf("Done! Processed input text at %.2f input characters/second%n", charsPerSec);
				}
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException)
			{
				System.Console.Error.Printf("%s: Could not open %s%n", typeof(Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter).FullName, segmenter.flags.textFile);
			}
		}

		/// <summary>Segment input and write to output stream.</summary>
		/// <param name="segmenter"/>
		/// <param name="br"/>
		/// <param name="pwOut"/>
		/// <param name="nThreads"/>
		/// <returns>input characters processed per second</returns>
		private static double Decode(Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter segmenter, BufferedReader br, PrintWriter pwOut, int nThreads)
		{
			System.Diagnostics.Debug.Assert(nThreads > 0);
			long nChars = 0;
			long startTime = Runtime.NanoTime();
			if (nThreads > 1)
			{
				MulticoreWrapper<string, string> wrapper = new MulticoreWrapper<string, string>(nThreads, segmenter);
				try
				{
					for (string line; (line = br.ReadLine()) != null; )
					{
						nChars += line.Length;
						wrapper.Put(line);
						while (wrapper.Peek())
						{
							pwOut.Println(wrapper.Poll());
						}
					}
					wrapper.Join();
					while (wrapper.Peek())
					{
						pwOut.Println(wrapper.Poll());
					}
				}
				catch (IOException e)
				{
					log.Warn(e);
				}
			}
			else
			{
				nChars = segmenter.Segment(br, pwOut);
			}
			long duration = Runtime.NanoTime() - startTime;
			double charsPerSec = (double)nChars / (duration / 1000000000.0);
			return charsPerSec;
		}

		/// <summary>Train a new segmenter or load an trained model from file.</summary>
		/// <remarks>
		/// Train a new segmenter or load an trained model from file.  First
		/// checks to see if there is a "model" or "loadClassifier" flag to
		/// load from, and if not tries to run training using the given
		/// options.
		/// </remarks>
		/// <param name="options">Properties to specify segmenter behavior</param>
		/// <returns>the trained or loaded model</returns>
		public static Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter GetSegmenter(Properties options)
		{
			Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter segmenter = new Edu.Stanford.Nlp.International.Arabic.Process.ArabicSegmenter(options);
			if (segmenter.flags.inputEncoding == null)
			{
				segmenter.flags.inputEncoding = Runtime.GetProperty("file.encoding");
			}
			// Load or train the classifier
			if (segmenter.flags.loadClassifier != null)
			{
				segmenter.LoadSegmenter(segmenter.flags.loadClassifier, options);
			}
			else
			{
				if (segmenter.flags.trainFile != null)
				{
					segmenter.Train();
					if (segmenter.flags.serializeTo != null)
					{
						segmenter.SerializeSegmenter(segmenter.flags.serializeTo);
						log.Info("Serialized segmenter to: " + segmenter.flags.serializeTo);
					}
				}
				else
				{
					log.Info("No training file or trained model specified!");
					log.Info(Usage());
					System.Environment.Exit(-1);
				}
			}
			return segmenter;
		}
	}
}

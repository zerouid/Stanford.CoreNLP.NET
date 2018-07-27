using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// Subclass of ClassifierCombiner that behaves like a NER, by copying
	/// the AnswerAnnotation labels to NERAnnotation.
	/// </summary>
	/// <remarks>
	/// Subclass of ClassifierCombiner that behaves like a NER, by copying
	/// the AnswerAnnotation labels to NERAnnotation. Also, it can run additional
	/// classifiers (NumberSequenceClassifier, QuantifiableEntityNormalizer, SUTime)
	/// to recognize numeric and date/time entities, depending on flag settings.
	/// </remarks>
	/// <author>Mihai Surdeanu</author>
	public class NERClassifierCombiner : ClassifierCombiner<CoreLabel>
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.NERClassifierCombiner));

		private readonly bool applyNumericClassifiers;

		public const bool ApplyNumericClassifiersDefault = true;

		public const string ApplyNumericClassifiersProperty = "ner.applyNumericClassifiers";

		private const string ApplyNumericClassifiersPropertyBase = "applyNumericClassifiers";

		public const string ApplyGazetteProperty = "ner.regex";

		public const bool ApplyGazetteDefault = false;

		private readonly NERClassifierCombiner.Language nerLanguage;

		public static readonly NERClassifierCombiner.Language NerLanguageDefault = NERClassifierCombiner.Language.English;

		public const string NerLanguageProperty = "ner.language";

		public const string NerLanguagePropertyBase = "language";

		public const string UsePresetNerProperty = "ner.usePresetNERTags";

		private readonly bool useSUTime;

		[System.Serializable]
		public sealed class Language
		{
			public static readonly NERClassifierCombiner.Language English = new NERClassifierCombiner.Language("English");

			public static readonly NERClassifierCombiner.Language Chinese = new NERClassifierCombiner.Language("Chinese");

			public string languageName;

			internal Language(string name)
			{
				this.languageName = name;
			}

			public static NERClassifierCombiner.Language FromString(string name, NERClassifierCombiner.Language defaultValue)
			{
				if (name != null)
				{
					foreach (NERClassifierCombiner.Language l in NERClassifierCombiner.Language.Values())
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(name, l.languageName))
						{
							return l;
						}
					}
				}
				return defaultValue;
			}
		}

		private readonly AbstractSequenceClassifier<CoreLabel> nsc;

		/// <summary>A mapping from single words to the NER tag that they should be.</summary>
		private readonly IDictionary<string, string> gazetteMapping;

		/// <exception cref="System.IO.IOException"/>
		public NERClassifierCombiner(Properties props)
			: base(props)
		{
			// todo [cdm 2015]: Could avoid constructing this if applyNumericClassifiers is false
			applyNumericClassifiers = PropertiesUtils.GetBool(props, ApplyNumericClassifiersProperty, ApplyNumericClassifiersDefault);
			nerLanguage = NERClassifierCombiner.Language.FromString(PropertiesUtils.GetString(props, NerLanguageProperty, null), NerLanguageDefault);
			useSUTime = PropertiesUtils.GetBool(props, NumberSequenceClassifier.UseSutimeProperty, NumberSequenceClassifier.UseSutimeDefault);
			nsc = new NumberSequenceClassifier(new Properties(), useSUTime, props);
			if (PropertiesUtils.GetBool(props, NERClassifierCombiner.ApplyGazetteProperty, NERClassifierCombiner.ApplyGazetteDefault))
			{
				this.gazetteMapping = ReadRegexnerGazette(DefaultPaths.DefaultNerGazetteMapping);
			}
			else
			{
				this.gazetteMapping = Java.Util.Collections.EmptyMap();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public NERClassifierCombiner(params string[] loadPaths)
			: this(ApplyNumericClassifiersDefault, NERClassifierCombiner.ApplyGazetteDefault, NumberSequenceClassifier.UseSutimeDefault, loadPaths)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public NERClassifierCombiner(bool applyNumericClassifiers, bool augmentRegexNER, bool useSUTime, params string[] loadPaths)
			: base(loadPaths)
		{
			this.applyNumericClassifiers = applyNumericClassifiers;
			this.nerLanguage = NerLanguageDefault;
			this.useSUTime = useSUTime;
			this.nsc = new NumberSequenceClassifier(useSUTime);
			if (augmentRegexNER)
			{
				this.gazetteMapping = ReadRegexnerGazette(DefaultPaths.DefaultNerGazetteMapping);
			}
			else
			{
				this.gazetteMapping = Java.Util.Collections.EmptyMap();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public NERClassifierCombiner(bool applyNumericClassifiers, NERClassifierCombiner.Language nerLanguage, bool useSUTime, bool augmentRegexNER, Properties nscProps, params string[] loadPaths)
			: base(nscProps, ClassifierCombiner.ExtractCombinationModeSafe(nscProps), loadPaths)
		{
			// NOTE: nscProps may contains sutime props which will not be recognized by the SeqClassifierFlags
			this.applyNumericClassifiers = applyNumericClassifiers;
			this.nerLanguage = nerLanguage;
			this.useSUTime = useSUTime;
			// check for which language to use for number sequence classifier
			if (nerLanguage == NERClassifierCombiner.Language.Chinese)
			{
				this.nsc = new ChineseNumberSequenceClassifier(new Properties(), useSUTime, nscProps);
			}
			else
			{
				this.nsc = new NumberSequenceClassifier(new Properties(), useSUTime, nscProps);
			}
			if (augmentRegexNER)
			{
				this.gazetteMapping = ReadRegexnerGazette(DefaultPaths.DefaultNerGazetteMapping);
			}
			else
			{
				this.gazetteMapping = Java.Util.Collections.EmptyMap();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[SafeVarargs]
		public NERClassifierCombiner(params AbstractSequenceClassifier<CoreLabel>[] classifiers)
			: this(ApplyNumericClassifiersDefault, NumberSequenceClassifier.UseSutimeDefault, NERClassifierCombiner.ApplyGazetteDefault, classifiers)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		[SafeVarargs]
		public NERClassifierCombiner(bool applyNumericClassifiers, bool useSUTime, bool augmentRegexNER, params AbstractSequenceClassifier<CoreLabel>[] classifiers)
			: base(classifiers)
		{
			this.applyNumericClassifiers = applyNumericClassifiers;
			this.nerLanguage = NerLanguageDefault;
			this.useSUTime = useSUTime;
			this.nsc = new NumberSequenceClassifier(useSUTime);
			if (augmentRegexNER)
			{
				this.gazetteMapping = ReadRegexnerGazette(DefaultPaths.DefaultNerGazetteMapping);
			}
			else
			{
				this.gazetteMapping = Java.Util.Collections.EmptyMap();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public NERClassifierCombiner(ObjectInputStream ois, Properties props)
			: base(ois, props)
		{
			// constructor which builds an NERClassifierCombiner from an ObjectInputStream
			// read the useSUTime from disk
			bool diskUseSUTime = ois.ReadBoolean();
			if (props.GetProperty("ner.useSUTime") != null)
			{
				this.useSUTime = bool.ParseBoolean(props.GetProperty("ner.useSUTime"));
			}
			else
			{
				this.useSUTime = diskUseSUTime;
			}
			// read the applyNumericClassifiers from disk
			bool diskApplyNumericClassifiers = ois.ReadBoolean();
			if (props.GetProperty("ner.applyNumericClassifiers") != null)
			{
				this.applyNumericClassifiers = bool.ParseBoolean(props.GetProperty("ner.applyNumericClassifiers"));
			}
			else
			{
				this.applyNumericClassifiers = diskApplyNumericClassifiers;
			}
			this.nerLanguage = NerLanguageDefault;
			// build the nsc, note that initProps should be set by ClassifierCombiner
			this.nsc = new NumberSequenceClassifier(new Properties(), useSUTime, props);
			if (PropertiesUtils.GetBool(props, NERClassifierCombiner.ApplyGazetteProperty, NERClassifierCombiner.ApplyGazetteDefault))
			{
				this.gazetteMapping = ReadRegexnerGazette(DefaultPaths.DefaultNerGazetteMapping);
			}
			else
			{
				this.gazetteMapping = Java.Util.Collections.EmptyMap();
			}
		}

		public static readonly ICollection<string> DefaultPassDownProperties = CollectionUtils.AsSet("encoding", "inputEncoding", "outputEncoding", "maxAdditionalKnownLCWords", "map", "ner.combinationMode", "ner.usePresetNERTags");

		/// <summary>
		/// This factory method is used to create the NERClassifierCombiner used in NERCombinerAnnotator
		/// (and, thence, in StanfordCoreNLP).
		/// </summary>
		/// <param name="name">
		/// A "x.y" format property name prefix (the "x" part). This is commonly null,
		/// and then "ner" is used.  If it is the empty string, then no property prefix is used.
		/// </param>
		/// <param name="properties">
		/// Various properties, including a list in "ner.model".
		/// The used ones start with name + "." or are in passDownProperties
		/// </param>
		/// <returns>An NERClassifierCombiner with the given properties</returns>
		public static NERClassifierCombiner CreateNERClassifierCombiner(string name, Properties properties)
		{
			return CreateNERClassifierCombiner(name, DefaultPassDownProperties, properties);
		}

		/// <summary>
		/// This factory method is used to create the NERClassifierCombiner used in NERCombinerAnnotator
		/// (and, thence, in StanfordCoreNLP).
		/// </summary>
		/// <param name="name">
		/// A "x.y" format property name prefix (the "x" part). This is commonly null,
		/// and then "ner" is used.  If it is the empty string, then no property prefix is used.
		/// </param>
		/// <param name="passDownProperties">
		/// Property names for which the property should be passed down
		/// to the NERClassifierCombiner. The default is not to pass down, but pass down is
		/// useful for things like charset encoding.
		/// </param>
		/// <param name="properties">
		/// Various properties, including a list in "ner.model".
		/// The used ones start with name + "." or are in passDownProperties
		/// </param>
		/// <returns>An NERClassifierCombiner with the given properties</returns>
		public static NERClassifierCombiner CreateNERClassifierCombiner(string name, ICollection<string> passDownProperties, Properties properties)
		{
			string prefix = (name == null) ? "ner." : name.IsEmpty() ? string.Empty : name + '.';
			string modelNames = properties.GetProperty(prefix + "model");
			if (modelNames == null)
			{
				modelNames = DefaultPaths.DefaultNerThreeclassModel + ',' + DefaultPaths.DefaultNerMucModel + ',' + DefaultPaths.DefaultNerConllModel;
			}
			// but modelNames can still be empty string is set explicitly to be empty!
			string[] models;
			if (!modelNames.IsEmpty())
			{
				models = modelNames.Split(",");
			}
			else
			{
				// Allow for no real NER model - can just use numeric classifiers or SUTime
				log.Info("WARNING: no NER models specified");
				models = StringUtils.EmptyStringArray;
			}
			NERClassifierCombiner nerCombiner;
			try
			{
				bool applyNumericClassifiers = PropertiesUtils.GetBool(properties, prefix + ApplyNumericClassifiersPropertyBase, ApplyNumericClassifiersDefault);
				bool useSUTime = PropertiesUtils.GetBool(properties, prefix + NumberSequenceClassifier.UseSutimePropertyBase, NumberSequenceClassifier.UseSutimeDefault);
				bool applyRegexner = PropertiesUtils.GetBool(properties, NERClassifierCombiner.ApplyGazetteProperty, NERClassifierCombiner.ApplyGazetteDefault);
				Properties combinerProperties;
				if (passDownProperties != null)
				{
					combinerProperties = PropertiesUtils.ExtractSelectedProperties(properties, passDownProperties);
					if (useSUTime)
					{
						// Make sure SUTime parameters are included
						Properties sutimeProps = PropertiesUtils.ExtractPrefixedProperties(properties, NumberSequenceClassifier.SutimeProperty + ".", true);
						PropertiesUtils.OverWriteProperties(combinerProperties, sutimeProps);
					}
				}
				else
				{
					// if passDownProperties is null, just pass everything through
					combinerProperties = properties;
				}
				//Properties combinerProperties = PropertiesUtils.extractSelectedProperties(properties, passDownProperties);
				NERClassifierCombiner.Language nerLanguage = NERClassifierCombiner.Language.FromString(properties.GetProperty(prefix + "language"), NERClassifierCombiner.Language.English);
				nerCombiner = new NERClassifierCombiner(applyNumericClassifiers, nerLanguage, useSUTime, applyRegexner, combinerProperties, models);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			return nerCombiner;
		}

		public virtual bool AppliesNumericClassifiers()
		{
			return applyNumericClassifiers;
		}

		public virtual bool UsesSUTime()
		{
			// if applyNumericClassifiers is false, SUTime isn't run regardless of setting of useSUTime
			return useSUTime && applyNumericClassifiers;
		}

		private static void CopyAnswerFieldsToNERField<Inn>(IList<INN> l)
			where Inn : ICoreMap
		{
			foreach (INN m in l)
			{
				m.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), m.Get(typeof(CoreAnnotations.AnswerAnnotation)));
			}
		}

		public override IList<CoreLabel> Classify(IList<CoreLabel> tokens)
		{
			return ClassifyWithGlobalInformation(tokens, null, null);
		}

		public override IList<CoreLabel> ClassifyWithGlobalInformation(IList<CoreLabel> tokens, ICoreMap document, ICoreMap sentence)
		{
			IList<CoreLabel> output = base.Classify(tokens);
			if (applyNumericClassifiers)
			{
				try
				{
					// recognizes additional MONEY, TIME, DATE, and NUMBER using a set of deterministic rules
					// note: some DATE and TIME entities are recognized by our statistical NER based on MUC
					// note: this includes SUTime
					// note: requires TextAnnotation, PartOfSpeechTagAnnotation, and AnswerAnnotation
					// note: this sets AnswerAnnotation!
					RecognizeNumberSequences(output, document, sentence);
				}
				catch (RuntimeInterruptedException e)
				{
					throw;
				}
				catch (Exception e)
				{
					log.Info("Ignored an exception in NumberSequenceClassifier: (result is that some numbers were not classified)");
					log.Info("Tokens: " + StringUtils.JoinWords(tokens, " "));
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
				// AnswerAnnotation -> NERAnnotation
				CopyAnswerFieldsToNERField(output);
				try
				{
					// normalizes numeric entities such as MONEY, TIME, DATE, or PERCENT
					// note: this uses and sets NamedEntityTagAnnotation!
					if (nerLanguage == NERClassifierCombiner.Language.Chinese)
					{
						// For chinese there is no support for SUTime by default
						// We need to hand in document and sentence for Chinese to handle DocDate; however, since English normalization
						// is handled by SUTime, and the information is passed in recognizeNumberSequences(), English only need output.
						ChineseQuantifiableEntityNormalizer.AddNormalizedQuantitiesToEntities(output, document, sentence);
					}
					else
					{
						QuantifiableEntityNormalizer.AddNormalizedQuantitiesToEntities(output, false, useSUTime);
					}
				}
				catch (Exception e)
				{
					log.Info("Ignored an exception in QuantifiableEntityNormalizer: (result is that entities were not normalized)");
					log.Info("Tokens: " + StringUtils.JoinWords(tokens, " "));
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
				catch (AssertionError e)
				{
					log.Info("Ignored an assertion in QuantifiableEntityNormalizer: (result is that entities were not normalized)");
					log.Info("Tokens: " + StringUtils.JoinWords(tokens, " "));
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
			}
			else
			{
				// AnswerAnnotation -> NERAnnotation
				CopyAnswerFieldsToNERField(output);
			}
			// Apply RegexNER annotations
			// cdm 2016: Used to say and do "// skip first token" but I couldn't understand why, so I removed that.
			foreach (CoreLabel token in tokens)
			{
				// System.out.println(token.toShorterString());
				if ((token.Tag() == null || token.Tag()[0] == 'N') && "O".Equals(token.Ner()) || "MISC".Equals(token.Ner()))
				{
					string target = gazetteMapping[token.OriginalText()];
					if (target != null)
					{
						token.SetNER(target);
					}
				}
			}
			// Return
			return output;
		}

		private void RecognizeNumberSequences(IList<CoreLabel> words, ICoreMap document, ICoreMap sentence)
		{
			// we need to copy here because NumberSequenceClassifier overwrites the AnswerAnnotation
			IList<CoreLabel> newWords = NumberSequenceClassifier.CopyTokens(words, sentence);
			nsc.ClassifyWithGlobalInformation(newWords, document, sentence);
			// copy AnswerAnnotation back. Do not overwrite!
			// also, copy all the additional annotations generated by SUTime and NumberNormalizer
			for (int i = 0; i < sz; i++)
			{
				CoreLabel origWord = words[i];
				CoreLabel newWord = newWords[i];
				// log.info(newWord.word() + " => " + newWord.get(CoreAnnotations.AnswerAnnotation.class) + " " + origWord.ner());
				string before = origWord.Get(typeof(CoreAnnotations.AnswerAnnotation));
				string newGuess = newWord.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if ((before == null || before.Equals(nsc.flags.backgroundSymbol) || before.Equals("MISC")) && !newGuess.Equals(nsc.flags.backgroundSymbol))
				{
					origWord.Set(typeof(CoreAnnotations.AnswerAnnotation), newGuess);
				}
				// transfer other annotations generated by SUTime or NumberNormalizer
				NumberSequenceClassifier.TransferAnnotations(newWord, origWord);
			}
		}

		public virtual void FinalizeAnnotation(Annotation annotation)
		{
			nsc.FinalizeClassification(annotation);
		}

		// write an NERClassifierCombiner to an ObjectOutputStream
		public override void SerializeClassifier(ObjectOutputStream oos)
		{
			try
			{
				// first write the ClassifierCombiner part to disk
				base.SerializeClassifier(oos);
				// write whether to use SUTime
				oos.WriteBoolean(useSUTime);
				// write whether to use NumericClassifiers
				oos.WriteBoolean(applyNumericClassifiers);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Static method for getting an NERClassifierCombiner from a string path.</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public static ClassifierCombiner GetClassifier(string loadPath, Properties props)
		{
			ObjectInputStream ois = IOUtils.ReadStreamFromString(loadPath);
			NERClassifierCombiner returnNCC = ((NERClassifierCombiner)GetClassifier(ois, props));
			IOUtils.CloseIgnoringExceptions(ois);
			return returnNCC;
		}

		// static method for getting an NERClassifierCombiner from an ObjectInputStream
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public static ClassifierCombiner GetClassifier(ObjectInputStream ois, Properties props)
		{
			return new NERClassifierCombiner(ois, props);
		}

		/// <summary>Method for displaying info about an NERClassifierCombiner.</summary>
		public static void ShowNCCInfo(NERClassifierCombiner ncc)
		{
			log.Info(string.Empty);
			log.Info("info for this NERClassifierCombiner: ");
			ClassifierCombiner.ShowCCInfo(ncc);
			log.Info("useSUTime: " + ncc.useSUTime);
			log.Info("applyNumericClassifier: " + ncc.applyNumericClassifiers);
			log.Info(string.Empty);
		}

		/// <summary>
		/// Read a gazette mapping in TokensRegex format from the given path
		/// The format is: 'case_sensitive_word \t target_ner_class' (additional info is ignored).
		/// </summary>
		/// <param name="mappingFile">The mapping file to read from, as a path either on the filesystem or in your classpath.</param>
		/// <returns>The mapping from word to NER tag.</returns>
		private static IDictionary<string, string> ReadRegexnerGazette(string mappingFile)
		{
			IDictionary<string, string> mapping = new Dictionary<string, string>();
			try
			{
				using (BufferedReader reader = IOUtils.ReaderFromString(mappingFile.Trim()))
				{
					foreach (string line in IOUtils.SlurpReader(reader).Split("\n"))
					{
						string[] fields = line.Split("\t");
						string key = fields[0];
						string target = fields[1];
						mapping[key] = target;
					}
				}
			}
			catch (IOException)
			{
				log.Warn("Could not read Regex mapping: " + mappingFile);
			}
			return Java.Util.Collections.UnmodifiableMap(mapping);
		}

		/// <summary>The main method.</summary>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(log, args);
			Properties props = StringUtils.ArgsToProperties(args);
			SeqClassifierFlags flags = new SeqClassifierFlags(props, false);
			// false for print probs as printed in next code block
			string loadPath = props.GetProperty("loadClassifier");
			NERClassifierCombiner ncc;
			if (loadPath != null)
			{
				// note that when loading a serialized classifier, the philosophy is override
				// any settings in props with those given in the commandline
				// so if you dumped it with useSUTime = false, and you say -useSUTime at
				// the commandline, the commandline takes precedence
				ncc = ((NERClassifierCombiner)GetClassifier(loadPath, props));
			}
			else
			{
				// pass null for passDownProperties to let all props go through
				ncc = CreateNERClassifierCombiner("ner", null, props);
			}
			// write the NERClassifierCombiner to the given path on disk
			string serializeTo = props.GetProperty("serializeTo");
			if (serializeTo != null)
			{
				ncc.SerializeClassifier(serializeTo);
			}
			string textFile = props.GetProperty("textFile");
			if (textFile != null)
			{
				ncc.ClassifyAndWriteAnswers(textFile);
			}
			// run on multiple textFiles , based off CRFClassifier code
			string textFiles = props.GetProperty("textFiles");
			if (textFiles != null)
			{
				IList<File> files = new List<File>();
				foreach (string filename in textFiles.Split(","))
				{
					files.Add(new File(filename));
				}
				ncc.ClassifyFilesAndWriteAnswers(files);
			}
			// options for run the NERClassifierCombiner on a testFile or testFiles
			string testFile = props.GetProperty("testFile");
			string testFiles = props.GetProperty("testFiles");
			string crfToExamine = props.GetProperty("crfToExamine");
			IDocumentReaderAndWriter<CoreLabel> readerAndWriter = ncc.DefaultReaderAndWriter();
			if (testFile != null || testFiles != null)
			{
				// check if there is not a crf specific request
				if (crfToExamine == null)
				{
					// in this case there is no crfToExamine
					if (testFile != null)
					{
						ncc.ClassifyAndWriteAnswers(testFile, readerAndWriter, true);
					}
					else
					{
						IList<File> files = Arrays.Stream(testFiles.Split(",")).Map(null).Collect(Collectors.ToList());
						ncc.ClassifyFilesAndWriteAnswers(files, ncc.DefaultReaderAndWriter(), true);
					}
				}
				else
				{
					ClassifierCombiner.ExamineCRF(ncc, crfToExamine, flags, testFile, testFiles, readerAndWriter);
				}
			}
			// option for showing info about the NERClassifierCombiner
			string showNCCInfo = props.GetProperty("showNCCInfo");
			if (showNCCInfo != null)
			{
				ShowNCCInfo(ncc);
			}
			// option for reading in from stdin
			if (flags.readStdin)
			{
				ncc.ClassifyStdin();
			}
		}
	}
}

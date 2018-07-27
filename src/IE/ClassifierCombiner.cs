using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IE.Ner;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// Merges the outputs of two or more AbstractSequenceClassifiers according to
	/// a simple precedence scheme: any given base classifier contributes only
	/// classifications of labels that do not exist in the base classifiers specified
	/// before, and that do not have any token overlap with labels assigned by
	/// higher priority classifiers.
	/// </summary>
	/// <remarks>
	/// Merges the outputs of two or more AbstractSequenceClassifiers according to
	/// a simple precedence scheme: any given base classifier contributes only
	/// classifications of labels that do not exist in the base classifiers specified
	/// before, and that do not have any token overlap with labels assigned by
	/// higher priority classifiers.
	/// <p>
	/// This is a pure AbstractSequenceClassifier, i.e., it sets the AnswerAnnotation label.
	/// If you work with NER classifiers, you should use NERClassifierCombiner. This class
	/// inherits from ClassifierCombiner, and takes care that all AnswerAnnotations are also
	/// copied to NERAnnotation.
	/// <p>
	/// You can specify up to 10 base classifiers using the -loadClassifier1 to -loadClassifier10
	/// properties. We also maintain the older usage when only two base classifiers were accepted,
	/// specified using -loadClassifier and -loadAuxClassifier.
	/// <p>
	/// ms 2009: removed all NER functionality (see NERClassifierCombiner), changed code so it
	/// accepts an arbitrary number of base classifiers, removed dead code.
	/// </remarks>
	/// <author>Chris Cox</author>
	/// <author>Mihai Surdeanu</author>
	public class ClassifierCombiner<In> : AbstractSequenceClassifier<In>
		where In : ICoreMap
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.ClassifierCombiner));

		private const bool Debug = false;

		private IList<AbstractSequenceClassifier<In>> baseClassifiers;

		/// <summary>NORMAL means that if one classifier uses PERSON, later classifiers can't also add PERSON, for example.</summary>
		/// <remarks>
		/// NORMAL means that if one classifier uses PERSON, later classifiers can't also add PERSON, for example. <br />
		/// HIGH_RECALL allows later models do set PERSON as long as it doesn't clobber existing annotations.
		/// </remarks>
		internal enum CombinationMode
		{
			Normal,
			HighRecall
		}

		private static readonly ClassifierCombiner.CombinationMode DefaultCombinationMode = ClassifierCombiner.CombinationMode.Normal;

		private const string CombinationModeProperty = "ner.combinationMode";

		private readonly ClassifierCombiner.CombinationMode combinationMode;

		private Properties initProps;

		private IList<string> initLoadPaths = new List<string>();

		/// <param name="p">
		/// Properties File that specifies
		/// <c>loadClassifier</c>
		/// and
		/// <c>loadAuxClassifier</c>
		/// properties or, alternatively,
		/// <c>loadClassifier[1-10]</c>
		/// properties.
		/// </param>
		/// <exception cref="Java.IO.FileNotFoundException">If classifier files not found</exception>
		/// <exception cref="System.IO.IOException"/>
		public ClassifierCombiner(Properties p)
			: base(p)
		{
			// keep track of properties used to initialize
			// keep track of paths used to load CRFs
			this.combinationMode = ExtractCombinationModeSafe(p);
			string loadPath1;
			string loadPath2;
			IList<string> paths = new List<string>();
			//
			// preferred configuration: specify up to 10 base classifiers using loadClassifier1 to loadClassifier10 properties
			//
			if ((loadPath1 = p.GetProperty("loadClassifier1")) != null && (loadPath2 = p.GetProperty("loadClassifier2")) != null)
			{
				paths.Add(loadPath1);
				paths.Add(loadPath2);
				for (int i = 3; i <= 10; i++)
				{
					string path;
					if ((path = p.GetProperty("loadClassifier" + i)) != null)
					{
						paths.Add(path);
					}
				}
				LoadClassifiers(p, paths);
			}
			else
			{
				//
				// second accepted setup (backward compatible): two classifier given in loadClassifier and loadAuxClassifier
				//
				if ((loadPath1 = p.GetProperty("loadClassifier")) != null && (loadPath2 = p.GetProperty("loadAuxClassifier")) != null)
				{
					paths.Add(loadPath1);
					paths.Add(loadPath2);
					LoadClassifiers(p, paths);
				}
				else
				{
					//
					// fall back strategy: use the two default paths on NLP machines
					//
					paths.Add(DefaultPaths.DefaultNerThreeclassModel);
					paths.Add(DefaultPaths.DefaultNerMucModel);
					LoadClassifiers(p, paths);
				}
			}
			this.initLoadPaths = new List<string>(paths);
			this.initProps = p;
		}

		/// <summary>
		/// Loads a series of base classifiers from the paths specified using the
		/// Properties specified.
		/// </summary>
		/// <param name="props">Properties for the classifier to use (encodings, output format, etc.)</param>
		/// <param name="combinationMode">How to handle multiple classifiers specifying the same entity type</param>
		/// <param name="loadPaths">Paths to the base classifiers</param>
		/// <exception cref="System.IO.IOException">If IO errors in loading classifier files</exception>
		public ClassifierCombiner(Properties props, ClassifierCombiner.CombinationMode combinationMode, params string[] loadPaths)
			: base(props)
		{
			this.combinationMode = combinationMode;
			IList<string> paths = new List<string>(Arrays.AsList(loadPaths));
			LoadClassifiers(props, paths);
			this.initLoadPaths = new List<string>(paths);
			this.initProps = props;
		}

		/// <summary>
		/// Loads a series of base classifiers from the paths specified using the
		/// Properties specified.
		/// </summary>
		/// <param name="combinationMode">How to handle multiple classifiers specifying the same entity type</param>
		/// <param name="loadPaths">Paths to the base classifiers</param>
		/// <exception cref="System.IO.IOException">If IO errors in loading classifier files</exception>
		public ClassifierCombiner(ClassifierCombiner.CombinationMode combinationMode, params string[] loadPaths)
			: this(new Properties(), combinationMode, loadPaths)
		{
		}

		/// <summary>Loads a series of base classifiers from the paths specified.</summary>
		/// <param name="loadPaths">Paths to the base classifiers</param>
		/// <exception cref="Java.IO.FileNotFoundException">If classifier files not found</exception>
		/// <exception cref="System.IO.IOException"/>
		public ClassifierCombiner(params string[] loadPaths)
			: this(DefaultCombinationMode, loadPaths)
		{
		}

		/// <summary>Combines a series of base classifiers.</summary>
		/// <param name="classifiers">The base classifiers</param>
		[SafeVarargs]
		public ClassifierCombiner(params AbstractSequenceClassifier<In>[] classifiers)
			: base(new Properties())
		{
			this.combinationMode = DefaultCombinationMode;
			baseClassifiers = new List<AbstractSequenceClassifier<In>>(Arrays.AsList(classifiers));
			flags.backgroundSymbol = baseClassifiers[0].flags.backgroundSymbol;
			this.initProps = new Properties();
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public ClassifierCombiner(ObjectInputStream ois, Properties props)
			: base(PropertiesUtils.OverWriteProperties((Properties)ois.ReadObject(), props))
		{
			// constructor for building a ClassifierCombiner from an ObjectInputStream
			// read the initial Properties out of the ObjectInputStream so you can properly start the AbstractSequenceClassifier
			// note now we load in props from command line and overwrite any that are given for command line
			// read another copy of initProps that I have helpfully included
			// TODO: probably set initProps in AbstractSequenceClassifier to avoid this writing twice thing, its hacky
			this.initProps = PropertiesUtils.OverWriteProperties((Properties)ois.ReadObject(), props);
			// read the initLoadPaths
			this.initLoadPaths = (List<string>)ois.ReadObject();
			// read the combinationMode from the serialized version
			string cm = (string)ois.ReadObject();
			// see if there is a commandline override for the combinationMode, else set newCM to the serialized version
			ClassifierCombiner.CombinationMode newCM;
			if (props.GetProperty("ner.combinationMode") != null)
			{
				// there is a possible commandline override, have to see if its valid
				try
				{
					// see if the commandline has a proper value
					newCM = ClassifierCombiner.CombinationMode.ValueOf(props.GetProperty("ner.combinationMode"));
				}
				catch (ArgumentException)
				{
					// the commandline override did not have a proper value, so just use the serialized version
					newCM = ClassifierCombiner.CombinationMode.ValueOf(cm);
				}
			}
			else
			{
				// there was no commandline override given, so just use the serialized version
				newCM = ClassifierCombiner.CombinationMode.ValueOf(cm);
			}
			this.combinationMode = newCM;
			// read in the base classifiers
			int numClassifiers = ois.ReadInt();
			// set up the list of base classifiers
			this.baseClassifiers = new List<AbstractSequenceClassifier<In>>();
			int i = 0;
			while (i < numClassifiers)
			{
				try
				{
					log.Info("loading CRF...");
					CRFClassifier<In> newCRF = ErasureUtils.UncheckedCast(CRFClassifier.GetClassifier(ois, props));
					baseClassifiers.Add(newCRF);
					i++;
				}
				catch (Exception)
				{
					try
					{
						log.Info("loading CMM...");
						CMMClassifier newCMM = ErasureUtils.UncheckedCast(CMMClassifier.GetClassifier(ois, props));
						baseClassifiers.Add(newCMM);
						i++;
					}
					catch (Exception ex)
					{
						throw new IOException("Couldn't load classifier!", ex);
					}
				}
			}
		}

		/// <summary>Either finds COMBINATION_MODE_PROPERTY or returns a default value.</summary>
		public static ClassifierCombiner.CombinationMode ExtractCombinationMode(Properties p)
		{
			string mode = p.GetProperty(CombinationModeProperty);
			if (mode == null)
			{
				return DefaultCombinationMode;
			}
			else
			{
				return ClassifierCombiner.CombinationMode.ValueOf(mode.ToUpper());
			}
		}

		/// <summary>
		/// Either finds COMBINATION_MODE_PROPERTY or returns a default
		/// value.
		/// </summary>
		/// <remarks>
		/// Either finds COMBINATION_MODE_PROPERTY or returns a default
		/// value.  If the value is not a legal value, a warning is printed.
		/// </remarks>
		public static ClassifierCombiner.CombinationMode ExtractCombinationModeSafe(Properties p)
		{
			try
			{
				return ExtractCombinationMode(p);
			}
			catch (ArgumentException)
			{
				log.Info("Illegal value of " + CombinationModeProperty + ": " + p.GetProperty(CombinationModeProperty));
				log.Info("  Legal values:");
				foreach (ClassifierCombiner.CombinationMode mode in ClassifierCombiner.CombinationMode.Values())
				{
					log.Info("  " + mode);
				}
				log.Info();
				return ClassifierCombiner.CombinationMode.Normal;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private void LoadClassifiers(Properties props, IList<string> paths)
		{
			baseClassifiers = new List<AbstractSequenceClassifier<In>>();
			if (PropertiesUtils.GetBool(props, "ner.usePresetNERTags", false))
			{
				AbstractSequenceClassifier<In> presetASC = new PresetSequenceClassifier(props);
				baseClassifiers.Add(presetASC);
			}
			foreach (string path in paths)
			{
				AbstractSequenceClassifier<In> cls = LoadClassifierFromPath(props, path);
				baseClassifiers.Add(cls);
			}
			if (baseClassifiers.Count > 0)
			{
				flags.backgroundSymbol = baseClassifiers[0].flags.backgroundSymbol;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static AbstractSequenceClassifier<INN> LoadClassifierFromPath<Inn>(Properties props, string path)
			where Inn : ICoreMap
		{
			//try loading as a CRFClassifier
			try
			{
				return ErasureUtils.UncheckedCast(CRFClassifier.GetClassifier(path, props));
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			//try loading as a CMMClassifier
			try
			{
				return ErasureUtils.UncheckedCast(CMMClassifier.GetClassifier(path));
			}
			catch (Exception e)
			{
				//fail
				//log.info("Couldn't load classifier from path :"+path);
				throw new IOException("Couldn't load classifier from " + path, e);
			}
		}

		public override ICollection<string> Labels()
		{
			ICollection<string> labs = Generics.NewHashSet();
			foreach (AbstractSequenceClassifier<ICoreMap> cls in baseClassifiers)
			{
				Sharpen.Collections.AddAll(labs, cls.Labels());
			}
			return labs;
		}

		/// <summary>
		/// Reads the Answer annotations in the given labellings (produced by the base models)
		/// and combines them using a priority ordering, i.e., for a given baseDocument all
		/// labellings seen before in the baseDocuments list have higher priority.
		/// </summary>
		/// <remarks>
		/// Reads the Answer annotations in the given labellings (produced by the base models)
		/// and combines them using a priority ordering, i.e., for a given baseDocument all
		/// labellings seen before in the baseDocuments list have higher priority.
		/// Writes the answer to AnswerAnnotation in the labeling at position 0
		/// (considered to be the main document).
		/// </remarks>
		/// <param name="baseDocuments">Results of all base AbstractSequenceClassifier models</param>
		/// <returns>
		/// A List of IN with the combined annotations.  (This is an
		/// updating of baseDocuments.get(0), not a new List.)
		/// </returns>
		private IList<In> MergeDocuments(IList<IList<In>> baseDocuments)
		{
			// we should only get here if there is something to merge
			System.Diagnostics.Debug.Assert((!baseClassifiers.IsEmpty() && !baseDocuments.IsEmpty()));
			// all base outputs MUST have the same length (we generated them internally!)
			for (int i = 1; i < baseDocuments.Count; i++)
			{
				System.Diagnostics.Debug.Assert((baseDocuments[0].Count == baseDocuments[i].Count));
			}
			string background = baseClassifiers[0].flags.backgroundSymbol;
			// baseLabels.get(i) points to the labels assigned by baseClassifiers.get(i)
			IList<ICollection<string>> baseLabels = new List<ICollection<string>>();
			ICollection<string> seenLabels = Generics.NewHashSet();
			foreach (AbstractSequenceClassifier<ICoreMap> baseClassifier in baseClassifiers)
			{
				ICollection<string> labs = baseClassifier.Labels();
				if (combinationMode != ClassifierCombiner.CombinationMode.HighRecall)
				{
					labs.RemoveAll(seenLabels);
				}
				else
				{
					labs.Remove(baseClassifier.flags.backgroundSymbol);
					labs.Remove(background);
				}
				Sharpen.Collections.AddAll(seenLabels, labs);
				baseLabels.Add(labs);
			}
			// incrementally merge each additional model with the main model (i.e., baseDocuments.get(0))
			// this keeps adding labels from the additional models to mainDocument
			// hence, when all is done, mainDocument contains the labels of all base models
			IList<In> mainDocument = baseDocuments[0];
			for (int i_1 = 1; i_1 < baseDocuments.Count; i_1++)
			{
				MergeTwoDocuments(mainDocument, baseDocuments[i_1], baseLabels[i_1], background);
			}
			return mainDocument;
		}

		/// <summary>
		/// This merges in labels from the auxDocument into the mainDocument when
		/// tokens have one of the labels in auxLabels, and the subsequence
		/// labeled with this auxLabel does not conflict with any non-background
		/// labelling in the mainDocument.
		/// </summary>
		internal static void MergeTwoDocuments<Inn>(IList<INN> mainDocument, IList<INN> auxDocument, ICollection<string> auxLabels, string background)
			where Inn : ICoreMap
		{
			bool insideAuxTag = false;
			bool auxTagValid = true;
			string prevAnswer = background;
			ICollection<INN> constituents = new List<INN>();
			IEnumerator<INN> auxIterator = auxDocument.ListIterator();
			foreach (INN wMain in mainDocument)
			{
				string mainAnswer = wMain.Get(typeof(CoreAnnotations.AnswerAnnotation));
				INN wAux = auxIterator.Current;
				string auxAnswer = wAux.Get(typeof(CoreAnnotations.AnswerAnnotation));
				bool insideMainTag = !mainAnswer.Equals(background);
				/* if the auxiliary classifier gave it one of the labels unique to
				auxClassifier, we might set the mainLabel to that. */
				if (auxLabels.Contains(auxAnswer))
				{
					if (!prevAnswer.Equals(auxAnswer) && !prevAnswer.Equals(background))
					{
						if (auxTagValid)
						{
							foreach (INN wi in constituents)
							{
								wi.Set(typeof(CoreAnnotations.AnswerAnnotation), prevAnswer);
							}
						}
						auxTagValid = true;
						constituents = new List<INN>();
					}
					insideAuxTag = true;
					if (insideMainTag)
					{
						auxTagValid = false;
					}
					prevAnswer = auxAnswer;
					constituents.Add(wMain);
				}
				else
				{
					if (insideAuxTag)
					{
						if (auxTagValid)
						{
							foreach (INN wi in constituents)
							{
								wi.Set(typeof(CoreAnnotations.AnswerAnnotation), prevAnswer);
							}
						}
						constituents = new List<INN>();
					}
					insideAuxTag = false;
					auxTagValid = true;
					prevAnswer = background;
				}
			}
			// deal with a sequence final auxLabel
			if (auxTagValid)
			{
				foreach (INN wi in constituents)
				{
					wi.Set(typeof(CoreAnnotations.AnswerAnnotation), prevAnswer);
				}
			}
		}

		/// <summary>
		/// Generates the AnswerAnnotation labels of the combined model for the given
		/// tokens, storing them in place in the tokens.
		/// </summary>
		/// <param name="tokens">A List of IN</param>
		/// <returns>The passed in parameters, which will have the AnswerAnnotation field added/overwritten</returns>
		public override IList<In> Classify(IList<In> tokens)
		{
			if (baseClassifiers.IsEmpty())
			{
				return tokens;
			}
			IList<IList<In>> baseOutputs = new List<IList<In>>();
			// the first base model works in place, modifying the original tokens
			IList<In> output = baseClassifiers[0].ClassifySentence(tokens);
			// classify(List<In>) is supposed to work in place, so add AnswerAnnotation to tokens!
			for (int i = 0; i < sz; i++)
			{
				tokens[i].Set(typeof(CoreAnnotations.AnswerAnnotation), output[i].Get(typeof(CoreAnnotations.AnswerAnnotation)));
			}
			baseOutputs.Add(tokens);
			for (int i_1 = 1; i_1 < sz; i_1++)
			{
				//List<CoreLabel> copy = deepCopy(tokens);
				// no need for deep copy: classifySentence creates a copy of the input anyway
				// List<CoreLabel> copy = tokens;
				output = baseClassifiers[i_1].ClassifySentence(tokens);
				baseOutputs.Add(output);
			}
			System.Diagnostics.Debug.Assert((baseOutputs.Count == baseClassifiers.Count));
			IList<In> finalAnswer = MergeDocuments(baseOutputs);
			return finalAnswer;
		}

		public override void Train(ICollection<IList<In>> docs, IDocumentReaderAndWriter<In> readerAndWriter)
		{
			throw new NotSupportedException();
		}

		// write a ClassifierCombiner to disk, this is based on CRFClassifier code
		public override void SerializeClassifier(string serializePath)
		{
			log.Info("Serializing classifier to " + serializePath + "...");
			ObjectOutputStream oos = null;
			try
			{
				oos = IOUtils.WriteStreamFromString(serializePath);
				SerializeClassifier(oos);
				log.Info("done.");
			}
			catch (Exception e)
			{
				throw new RuntimeIOException("Failed to save classifier", e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(oos);
			}
		}

		// method for writing a ClassifierCombiner to an ObjectOutputStream
		public override void SerializeClassifier(ObjectOutputStream oos)
		{
			try
			{
				// record the properties used to initialize
				oos.WriteObject(initProps);
				// this is a bit of a hack, but have to write this twice so you can get it again
				// after you initialize AbstractSequenceClassifier
				// basically when this is read from the ObjectInputStream, I read it once to call
				// super(props) and then I read it again so I can set this.initProps
				// TODO: probably should have AbstractSequenceClassifier store initProps to get rid of this double writing
				oos.WriteObject(initProps);
				// record the initial loadPaths
				oos.WriteObject(initLoadPaths);
				// record the combinationMode
				string combinationModeString = combinationMode.ToString();
				oos.WriteObject(combinationModeString);
				// get the number of classifiers to write to disk
				int numClassifiers = baseClassifiers.Count;
				oos.WriteInt(numClassifiers);
				// go through baseClassifiers and write each one to disk with CRFClassifier's serialize method
				log.Info(string.Empty);
				foreach (AbstractSequenceClassifier<In> asc in baseClassifiers)
				{
					//CRFClassifier crfc = (CRFClassifier) asc;
					//log.info("Serializing a base classifier...");
					asc.SerializeClassifier(oos);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public override void LoadClassifier(ObjectInputStream @in, Properties props)
		{
			throw new NotSupportedException();
		}

		public override IList<In> ClassifyWithGlobalInformation(IList<In> tokenSeq, ICoreMap doc, ICoreMap sent)
		{
			return Classify(tokenSeq);
		}

		// static method for getting a ClassifierCombiner from a string path
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		/// <exception cref="System.InvalidCastException"/>
		public static Edu.Stanford.Nlp.IE.ClassifierCombiner GetClassifier(string loadPath, Properties props)
		{
			ObjectInputStream ois = IOUtils.ReadStreamFromString(loadPath);
			Edu.Stanford.Nlp.IE.ClassifierCombiner returnCC = GetClassifier(ois, props);
			IOUtils.CloseIgnoringExceptions(ois);
			return returnCC;
		}

		// static method for getting a ClassifierCombiner from ObjectInputStream
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.InvalidCastException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static Edu.Stanford.Nlp.IE.ClassifierCombiner GetClassifier(ObjectInputStream ois, Properties props)
		{
			return new Edu.Stanford.Nlp.IE.ClassifierCombiner(ois, props);
		}

		// run a particular CRF of this ClassifierCombiner on a testFile
		// user can say -crfToExamine 0 to get 1st element or -crfToExamine /edu/stanford/models/muc7.crf.ser.gz
		// this does not currently support drill down on CMM's
		/// <exception cref="System.Exception"/>
		public static void ExamineCRF(Edu.Stanford.Nlp.IE.ClassifierCombiner cc, string crfNameOrIndex, SeqClassifierFlags flags, string testFile, string testFiles, IDocumentReaderAndWriter<CoreLabel> readerAndWriter)
		{
			CRFClassifier<CoreLabel> crf;
			// potential index into baseClassifiers
			int ci;
			// set ci with the following rules
			// 1. first see if ci is an index into baseClassifiers
			// 2. if its not an integer or wrong size, see if its a file name of a loadPath
			try
			{
				ci = System.Convert.ToInt32(crfNameOrIndex);
				if (ci < 0 || ci >= cc.baseClassifiers.Count)
				{
					// ci is not an int corresponding to an element in baseClassifiers, see if name of a crf loadPath
					ci = cc.initLoadPaths.IndexOf(crfNameOrIndex);
				}
			}
			catch (NumberFormatException)
			{
				// cannot interpret crfNameOrIndex as an integer, see if name of a crf loadPath
				ci = cc.initLoadPaths.IndexOf(crfNameOrIndex);
			}
			// if ci corresponds to an index in baseClassifiers, get the crf at that index, otherwise set crf to null
			if (ci >= 0 && ci < cc.baseClassifiers.Count)
			{
				// TODO: this will break if baseClassifiers contains something that is not a CRF
				crf = (CRFClassifier<CoreLabel>)cc.baseClassifiers[ci];
			}
			else
			{
				crf = null;
			}
			// if you can get a specific crf, generate the appropriate report, if null do nothing
			if (crf != null)
			{
				// if there is a crf and testFile was set , do the crf stuff for a single testFile
				if (testFile != null)
				{
					if (flags.searchGraphPrefix != null)
					{
						crf.ClassifyAndWriteViterbiSearchGraph(testFile, flags.searchGraphPrefix, crf.MakeReaderAndWriter());
					}
					else
					{
						if (flags.printFirstOrderProbs)
						{
							crf.PrintFirstOrderProbs(testFile, readerAndWriter);
						}
						else
						{
							if (flags.printFactorTable)
							{
								crf.PrintFactorTable(testFile, readerAndWriter);
							}
							else
							{
								if (flags.printProbs)
								{
									crf.PrintProbs(testFile, readerAndWriter);
								}
								else
								{
									if (flags.useKBest)
									{
										// TO DO: handle if user doesn't provide kBest
										int k = flags.kBest;
										crf.ClassifyAndWriteAnswersKBest(testFile, k, readerAndWriter);
									}
									else
									{
										if (flags.printLabelValue)
										{
											crf.PrintLabelInformation(testFile, readerAndWriter);
										}
										else
										{
											// no crf test flag provided
											log.Info("Warning: no crf test flag was provided, running classify and write answers");
											crf.ClassifyAndWriteAnswers(testFile, readerAndWriter, true);
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if (testFiles != null)
					{
						// if there is a crf and testFiles was set , do the crf stuff for testFiles
						// if testFile was set as well, testFile overrides
						IList<File> files = Arrays.Stream(testFiles.Split(",")).Map(null).Collect(Collectors.ToList());
						if (flags.printProbs)
						{
							// there is a crf and printProbs
							crf.PrintProbs(files, crf.DefaultReaderAndWriter());
						}
						else
						{
							log.Info("Warning: no crf test flag was provided, running classify files and write answers");
							crf.ClassifyFilesAndWriteAnswers(files, crf.DefaultReaderAndWriter(), true);
						}
					}
				}
			}
		}

		// show some info about a ClassifierCombiner
		public static void ShowCCInfo(Edu.Stanford.Nlp.IE.ClassifierCombiner cc)
		{
			log.Info(string.Empty);
			log.Info("classifiers used:");
			log.Info(string.Empty);
			if (cc.initLoadPaths.Count == cc.baseClassifiers.Count)
			{
				for (int i = 0; i < cc.initLoadPaths.Count; i++)
				{
					log.Info("baseClassifiers index " + i + " : " + cc.initLoadPaths[i]);
				}
			}
			else
			{
				for (int i = 0; i < cc.initLoadPaths.Count; i++)
				{
					log.Info("baseClassifiers index " + i);
				}
			}
			log.Info(string.Empty);
			log.Info("combinationMode: " + cc.combinationMode);
			log.Info(string.Empty);
		}

		/// <summary>Some basic testing of the ClassifierCombiner.</summary>
		/// <param name="args">Command-line arguments as properties: -loadClassifier1 serializedFile -loadClassifier2 serializedFile</param>
		/// <exception cref="System.Exception">If IO or serialization error loading classifiers</exception>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			Edu.Stanford.Nlp.IE.ClassifierCombiner ec = new Edu.Stanford.Nlp.IE.ClassifierCombiner(props);
			log.Info(ec.ClassifyToString("Marketing : Sony Hopes to Win Much Bigger Market For Wide Range of Small-Video Products --- By Andrew B. Cohen Staff Reporter of The Wall Street Journal"));
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.IE.Machinereading
{
	/// <summary>Uses parsed files to train classifier and test on data set.</summary>
	/// <author>Andrey Gusev</author>
	/// <author>Mason Smith</author>
	/// <author>David McClosky (mcclosky@stanford.edu)</author>
	[System.Serializable]
	public class BasicEntityExtractor : IExtractor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.BasicEntityExtractor));

		private const long serialVersionUID = -4011478706866593869L;

		private CRFClassifier<CoreLabel> classifier;

		private static readonly Type annotationForWord = typeof(CoreAnnotations.TextAnnotation);

		private const bool SaveConll2003 = false;

		protected internal string gazetteerLocation;

		protected internal ICollection<string> annotationsToSkip;

		protected internal bool useSubTypes;

		protected internal bool useBIO;

		protected internal EntityMentionFactory entityMentionFactory;

		public readonly Logger logger;

		protected internal bool useNERTags;

		public BasicEntityExtractor(string gazetteerLocation, bool useSubTypes, ICollection<string> annotationsToSkip, bool useBIO, EntityMentionFactory factory, bool useNERTags)
		{
			// non-final so we can do cross validation
			this.annotationsToSkip = annotationsToSkip;
			this.gazetteerLocation = gazetteerLocation;
			this.logger = Logger.GetLogger(typeof(Edu.Stanford.Nlp.IE.Machinereading.BasicEntityExtractor).FullName);
			this.useSubTypes = useSubTypes;
			this.useBIO = useBIO;
			this.entityMentionFactory = factory;
			this.useNERTags = useNERTags;
		}

		/// <summary>Annotate an ExtractionDataSet with entities.</summary>
		/// <remarks>
		/// Annotate an ExtractionDataSet with entities. This will modify the
		/// ExtractionDataSet in place.
		/// </remarks>
		/// <param name="doc">The dataset to label</param>
		public virtual void Annotate(Annotation doc)
		{
			// dump a file in CoNLL-2003 format
			// saveCoNLLFiles("/tmp/ace/test", doc, useSubTypes, useBIO);
			IList<ICoreMap> sents = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int sentCount = 1;
			foreach (ICoreMap sentence in sents)
			{
				if (useNERTags)
				{
					this.MakeAnnotationFromAllNERTags(sentence);
				}
				else
				{
					ExtractEntities(sentence, sentCount);
				}
				sentCount++;
			}
		}

		/*
		if(SAVE_CONLL_2003){
		try {
		saveCoNLLFiles("test_output/", doc, useSubTypes, useBIO);
		log.info("useBIO = " + useBIO);
		} catch (IOException e) {
		e.printStackTrace();
		System.exit(1);
		}
		}
		*/
		public virtual string GetEntityTypeForTag(string tag)
		{
			//need to be overridden by the extending class;
			return tag;
		}

		/// <summary>Label entities in an ExtractionSentence.</summary>
		/// <remarks>
		/// Label entities in an ExtractionSentence. Assumes the classifier has already
		/// been trained.
		/// </remarks>
		/// <param name="sentence">ExtractionSentence that we want to extract entities from</param>
		/// <returns>
		/// an ExtractionSentence with text content, tree and entities set.
		/// Relations will not be set.
		/// </returns>
		private ICoreMap ExtractEntities(ICoreMap sentence, int sentCount)
		{
			// don't add answer annotations
			IList<CoreLabel> testSentence = AnnotationUtils.SentenceEntityMentionsToCoreLabels(sentence, false, annotationsToSkip, null, useSubTypes, useBIO);
			// now label the sentence
			IList<CoreLabel> annotatedSentence = this.classifier.Classify(testSentence);
			logger.Finest("CLASSFIER OUTPUT: " + annotatedSentence);
			IList<EntityMention> extractedEntities = new List<EntityMention>();
			int i = 0;
			// variables which keep track of partially seen entities (i.e. we've seen
			// some but not all the words in them so far)
			string lastType = null;
			int startIndex = -1;
			//
			// note that labels may be in the BIO or just the IO format. we must handle both transparently
			//
			foreach (CoreLabel label in annotatedSentence)
			{
				string type = label.Get(typeof(CoreAnnotations.AnswerAnnotation));
				if (type.Equals(SeqClassifierFlags.DefaultBackgroundSymbol))
				{
					type = null;
				}
				// this is an entity end boundary followed by O
				if (type == null && lastType != null)
				{
					MakeEntityMention(sentence, startIndex, i, lastType, extractedEntities, sentCount);
					logger.Info("Found entity: " + extractedEntities[extractedEntities.Count - 1]);
					startIndex = -1;
				}
				else
				{
					// entity start preceded by an O
					if (lastType == null && type != null)
					{
						startIndex = i;
					}
					else
					{
						// entity end followed by another entity of different type
						if (lastType != null && type != null && (type.StartsWith("B-") || (lastType.StartsWith("I-") && type.StartsWith("I-") && !lastType.Equals(type)) || (NotBIO(lastType) && NotBIO(type) && !lastType.Equals(type))))
						{
							MakeEntityMention(sentence, startIndex, i, lastType, extractedEntities, sentCount);
							logger.Info("Found entity: " + extractedEntities[extractedEntities.Count - 1]);
							startIndex = i;
						}
					}
				}
				lastType = type;
				i++;
			}
			// replace the original annotation with the predicted entities
			sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), extractedEntities);
			logger.Finest("EXTRACTED ENTITIES: ");
			foreach (EntityMention e in extractedEntities)
			{
				logger.Finest("\t" + e);
			}
			PostprocessSentence(sentence, sentCount);
			return sentence;
		}

		/*
		* Called by extractEntities after extraction is done. Override this method if
		* there are some cleanups you want to implement.
		*/
		public virtual void PostprocessSentence(ICoreMap sentence, int sentCount)
		{
		}

		// nothing to do by default
		/// <summary>
		/// Converts NamedEntityTagAnnotation tags into
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention"/>
		/// s. This
		/// finds the longest sequence of NamedEntityTagAnnotation tags of the matching
		/// type.
		/// </summary>
		/// <param name="sentence">A sentence, ideally annotated with NamedEntityTagAnnotation</param>
		/// <param name="nerTag">The name of the NER tag to copy, e.g. "DATE".</param>
		/// <param name="entityType">
		/// The type of the
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention"/>
		/// objects created
		/// </param>
		public virtual void MakeAnnotationFromGivenNERTag(ICoreMap sentence, string nerTag, string entityType)
		{
			IList<CoreLabel> words = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<EntityMention> mentions = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			System.Diagnostics.Debug.Assert(words != null);
			System.Diagnostics.Debug.Assert(mentions != null);
			for (int start = 0; start < words.Count; start++)
			{
				int end;
				// find the first token after start that isn't of nerType
				for (end = start; end < words.Count; end++)
				{
					string ne = words[end].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					if (!ne.Equals(nerTag))
					{
						break;
					}
				}
				if (end > start)
				{
					// found a match!
					EntityMention m = entityMentionFactory.ConstructEntityMention(EntityMention.MakeUniqueId(), sentence, new Span(start, end), new Span(start, end), entityType, null, null);
					logger.Info("Created " + entityType + " entity mention: " + m);
					start = end - 1;
					mentions.Add(m);
				}
			}
			sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), mentions);
		}

		/// <summary>
		/// Converts NamedEntityTagAnnotation tags into
		/// <see cref="Edu.Stanford.Nlp.IE.Machinereading.Structure.EntityMention"/>
		/// s. This
		/// finds the longest sequence of NamedEntityTagAnnotation tags of the matching
		/// type.
		/// </summary>
		/// <param name="sentence">A sentence annotated with NamedEntityTagAnnotation</param>
		public virtual void MakeAnnotationFromAllNERTags(ICoreMap sentence)
		{
			IList<CoreLabel> words = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<EntityMention> mentions = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			System.Diagnostics.Debug.Assert(words != null);
			if (mentions == null)
			{
				this.logger.Info("mentions are null");
				mentions = new List<EntityMention>();
			}
			for (int start = 0; start < words.Count; start++)
			{
				int end;
				// find the first token after start that isn't of nerType
				string lastneTag = null;
				string ne = null;
				for (end = start; end < words.Count; end++)
				{
					ne = words[end].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					if (ne.Equals(SeqClassifierFlags.DefaultBackgroundSymbol) || (lastneTag != null && !ne.Equals(lastneTag)))
					{
						break;
					}
					lastneTag = ne;
				}
				if (end > start)
				{
					// found a match!
					string entityType = this.GetEntityTypeForTag(lastneTag);
					EntityMention m = entityMentionFactory.ConstructEntityMention(EntityMention.MakeUniqueId(), sentence, new Span(start, end), new Span(start, end), entityType, null, null);
					//TODO: changed entityType in the above sentence to nerTag - Sonal
					logger.Info("Created " + entityType + " entity mention: " + m);
					start = end - 1;
					mentions.Add(m);
				}
			}
			sentence.Set(typeof(MachineReadingAnnotations.EntityMentionsAnnotation), mentions);
		}

		private static bool NotBIO(string label)
		{
			return !(label.StartsWith("B-") || label.StartsWith("I-"));
		}

		public virtual void MakeEntityMention(ICoreMap sentence, int start, int end, string label, IList<EntityMention> entities, int sentCount)
		{
			System.Diagnostics.Debug.Assert((start >= 0));
			string identifier = MakeEntityMentionIdentifier(sentence, sentCount, entities.Count);
			EntityMention entity = MakeEntityMention(sentence, start, end, label, identifier);
			entities.Add(entity);
		}

		public static string MakeEntityMentionIdentifier(ICoreMap sentence, int sentCount, int entId)
		{
			string docid = sentence.Get(typeof(CoreAnnotations.DocIDAnnotation));
			if (docid == null)
			{
				docid = "EntityMention";
			}
			string identifier = docid + "-" + entId + "-" + sentCount;
			return identifier;
		}

		public virtual EntityMention MakeEntityMention(ICoreMap sentence, int start, int end, string label, string identifier)
		{
			Span span = new Span(start, end);
			string type = null;
			string subtype = null;
			if (!label.StartsWith("B-") && !label.StartsWith("I-"))
			{
				type = label;
				subtype = null;
			}
			else
			{
				// TODO: add support for subtypes! (needed at least in ACE)
				type = Sharpen.Runtime.Substring(label, 2);
				subtype = null;
			}
			// TODO: add support for subtypes! (needed at least in ACE)
			EntityMention entity = entityMentionFactory.ConstructEntityMention(identifier, sentence, span, span, type, subtype, null);
			ICounter<string> probs = new ClassicCounter<string>();
			probs.SetCount(entity.GetType(), 1.0);
			entity.SetTypeProbabilities(probs);
			return entity;
		}

		// TODO not called any more, but possibly useful as a reference
		/// <summary>
		/// This should be called after the classifier has been trained and
		/// parseAndTrain has been called to accumulate test set
		/// This will return precision,recall and F1 measure
		/// </summary>
		public virtual void RunTestSet(IList<IList<CoreLabel>> testSet)
		{
			ICounter<string> tp = new ClassicCounter<string>();
			ICounter<string> fp = new ClassicCounter<string>();
			ICounter<string> fn = new ClassicCounter<string>();
			ICounter<string> actual = new ClassicCounter<string>();
			foreach (IList<CoreLabel> labels in testSet)
			{
				IList<CoreLabel> unannotatedLabels = new List<CoreLabel>();
				// create a new label without answer annotation
				foreach (CoreLabel label in labels)
				{
					CoreLabel newLabel = new CoreLabel();
					newLabel.Set(annotationForWord, label.Get(annotationForWord));
					newLabel.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), label.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)));
					unannotatedLabels.Add(newLabel);
				}
				IList<CoreLabel> annotatedLabels = this.classifier.Classify(unannotatedLabels);
				int ind = 0;
				foreach (CoreLabel expectedLabel in labels)
				{
					CoreLabel annotatedLabel = annotatedLabels[ind];
					string answer = annotatedLabel.Get(typeof(CoreAnnotations.AnswerAnnotation));
					string expectedAnswer = expectedLabel.Get(typeof(CoreAnnotations.AnswerAnnotation));
					actual.IncrementCount(expectedAnswer);
					// match only non background symbols
					if (!SeqClassifierFlags.DefaultBackgroundSymbol.Equals(expectedAnswer) && expectedAnswer.Equals(answer))
					{
						// true positives
						tp.IncrementCount(answer);
						System.Console.Out.WriteLine("True Positive:" + annotatedLabel);
					}
					else
					{
						if (!SeqClassifierFlags.DefaultBackgroundSymbol.Equals(answer))
						{
							// false positives
							fp.IncrementCount(answer);
							System.Console.Out.WriteLine("False Positive:" + annotatedLabel);
						}
						else
						{
							if (!SeqClassifierFlags.DefaultBackgroundSymbol.Equals(expectedAnswer))
							{
								// false negatives
								fn.IncrementCount(expectedAnswer);
								System.Console.Out.WriteLine("False Negative:" + expectedLabel);
							}
						}
					}
					// else true negatives
					ind++;
				}
			}
			actual.Remove(SeqClassifierFlags.DefaultBackgroundSymbol);
		}

		// XXX not called any more -- maybe lose annotationsToSkip entirely?
		/// <param name="annotationsToSkip">The type of annotation to skip in assigning answer annotations</param>
		public virtual void SetAnnotationsToSkip(ICollection<string> annotationsToSkip)
		{
			this.annotationsToSkip = annotationsToSkip;
		}

		/*
		*  Model creation, saving, loading, and saving
		*/
		public virtual void Train(Annotation doc)
		{
			IList<IList<CoreLabel>> trainingSet = AnnotationUtils.EntityMentionsToCoreLabels(doc, annotationsToSkip, useSubTypes, useBIO);
			// dump a file in CoNLL-2003 format
			// saveCoNLLFiles("/tmp/ace/train/", doc, useSubTypes, useBIO);
			this.classifier = CreateClassifier();
			if (trainingSet.Count > 0)
			{
				this.classifier.Train(Java.Util.Collections.UnmodifiableCollection(trainingSet));
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void SaveCoNLLFiles(string dir, Annotation dataset, bool useSubTypes, bool alreadyBIO)
		{
			IList<ICoreMap> sentences = dataset.Get(typeof(CoreAnnotations.SentencesAnnotation));
			string docid = null;
			TextWriter os = null;
			foreach (ICoreMap sentence in sentences)
			{
				string myDocid = sentence.Get(typeof(CoreAnnotations.DocIDAnnotation));
				if (docid == null || !myDocid.Equals(docid))
				{
					if (os != null)
					{
						os.Close();
					}
					docid = myDocid;
					os = new TextWriter(new FileOutputStream(dir + File.separator + docid + ".conll"));
				}
				IList<CoreLabel> labeledSentence = AnnotationUtils.SentenceEntityMentionsToCoreLabels(sentence, true, null, null, useSubTypes, alreadyBIO);
				System.Diagnostics.Debug.Assert((labeledSentence != null));
				string prev = null;
				foreach (CoreLabel word in labeledSentence)
				{
					string w = word.Word().ReplaceAll("[ \t\n]+", "_");
					string t = word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
					string l = word.Get(typeof(CoreAnnotations.AnswerAnnotation));
					string nl = l;
					if (!alreadyBIO && !l.Equals("O"))
					{
						if (prev != null && l.Equals(prev))
						{
							nl = "I-" + l;
						}
						else
						{
							nl = "B-" + l;
						}
					}
					string line = w + " " + t + " " + nl;
					string[] toks = line.Split("[ \t\n]+");
					if (toks.Length != 3)
					{
						throw new Exception("INVALID LINE: \"" + line + "\"");
					}
					os.Printf("%s %s %s\n", w, t, nl);
					prev = l;
				}
				os.WriteLine();
			}
			if (os != null)
			{
				os.Close();
			}
		}

		public static void SaveCoNLL(TextWriter os, IList<IList<CoreLabel>> sentences, bool alreadyBIO)
		{
			os.WriteLine("-DOCSTART- -X- O\n");
			foreach (IList<CoreLabel> sent in sentences)
			{
				string prev = null;
				foreach (CoreLabel word in sent)
				{
					string w = word.Word().ReplaceAll("[ \t\n]+", "_");
					string t = word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
					string l = word.Get(typeof(CoreAnnotations.AnswerAnnotation));
					string nl = l;
					if (!alreadyBIO && !l.Equals("O"))
					{
						if (prev != null && l.Equals(prev))
						{
							nl = "I-" + l;
						}
						else
						{
							nl = "B-" + l;
						}
					}
					string line = w + " " + t + " " + nl;
					string[] toks = line.Split("[ \t\n]+");
					if (toks.Length != 3)
					{
						throw new Exception("INVALID LINE: \"" + line + "\"");
					}
					os.Printf("%s %s %s\n", w, t, nl);
					prev = l;
				}
				os.WriteLine();
			}
		}

		/*
		* Create the underlying classifier.
		*/
		private CRFClassifier<CoreLabel> CreateClassifier()
		{
			Properties props = new Properties();
			props.SetProperty("macro", "true");
			// use a generic CRF configuration
			props.SetProperty("useIfInteger", "true");
			props.SetProperty("featureFactory", "edu.stanford.nlp.ie.NERFeatureFactory");
			props.SetProperty("saveFeatureIndexToDisk", "false");
			if (this.gazetteerLocation != null)
			{
				log.Info("Using gazetteer: " + this.gazetteerLocation);
				props.SetProperty("gazette", this.gazetteerLocation);
				props.SetProperty("sloppyGazette", "true");
			}
			return new CRFClassifier<CoreLabel>(props);
		}

		/// <summary>Loads the model from disk.</summary>
		/// <param name="path">The location of model that was saved to disk</param>
		/// <exception cref="System.InvalidCastException">if model is the wrong format</exception>
		/// <exception cref="System.IO.IOException">
		/// if the model file doesn't exist or is otherwise
		/// unavailable/incomplete
		/// </exception>
		/// <exception cref="System.TypeLoadException">this would probably indicate a serious classpath problem</exception>
		public static Edu.Stanford.Nlp.IE.Machinereading.BasicEntityExtractor Load(string path, Type entityClassifier, bool preferDefaultGazetteer)
		{
			// load the additional arguments
			// try to load the extra file from the CLASSPATH first
			InputStream @is = typeof(Edu.Stanford.Nlp.IE.Machinereading.BasicEntityExtractor).GetClassLoader().GetResourceAsStream(path + ".extra");
			// if not found in the CLASSPATH, load from the file system
			if (@is == null)
			{
				@is = new FileInputStream(path + ".extra");
			}
			ObjectInputStream @in = new ObjectInputStream(@is);
			string gazetteerLocation = ErasureUtils.UncheckedCast<string>(@in.ReadObject());
			if (preferDefaultGazetteer)
			{
				gazetteerLocation = DefaultPaths.DefaultNflGazetteer;
			}
			ICollection<string> annotationsToSkip = ErasureUtils.UncheckedCast<ICollection<string>>(@in.ReadObject());
			bool useSubTypes = ErasureUtils.UncheckedCast<bool>(@in.ReadObject());
			bool useBIO = ErasureUtils.UncheckedCast<bool>(@in.ReadObject());
			@in.Close();
			@is.Close();
			Edu.Stanford.Nlp.IE.Machinereading.BasicEntityExtractor extractor = (Edu.Stanford.Nlp.IE.Machinereading.BasicEntityExtractor)MachineReading.MakeEntityExtractor(entityClassifier, gazetteerLocation);
			// load the CRF classifier (this works from any resource, e.g., classpath or file system)
			extractor.classifier = CRFClassifier.GetClassifier(path);
			// copy the extra arguments
			extractor.annotationsToSkip = annotationsToSkip;
			extractor.useSubTypes = useSubTypes;
			extractor.useBIO = useBIO;
			return extractor;
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void Save(string path)
		{
			// save the CRF
			this.classifier.SerializeClassifier(path);
			// save the additional arguments
			FileOutputStream fos = new FileOutputStream(path + ".extra");
			ObjectOutputStream @out = new ObjectOutputStream(fos);
			@out.WriteObject(this.gazetteerLocation);
			@out.WriteObject(this.annotationsToSkip);
			@out.WriteObject(this.useSubTypes);
			@out.WriteObject(this.useBIO);
			@out.Close();
		}

		/*
		* Other helper functions
		*/
		// TODO not called any more, but possibly useful as a reference
		/// <summary>for printing labeled sentence in less verbose manner</summary>
		/// <returns>string for printing</returns>
		public static string LabeledSentenceToString(IList<CoreLabel> labeledSentence, bool printNer)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[ ");
			foreach (CoreLabel label in labeledSentence)
			{
				string word = label.GetString(annotationForWord);
				string answer = label.GetString<CoreAnnotations.AnswerAnnotation>();
				string tag = label.GetString<CoreAnnotations.PartOfSpeechAnnotation>();
				sb.Append(word).Append("(").Append(tag);
				if (!SeqClassifierFlags.DefaultBackgroundSymbol.Equals(answer))
				{
					sb.Append(" ").Append(answer);
				}
				if (printNer)
				{
					sb.Append(" ner:").Append(label.Ner());
				}
				sb.Append(") ");
			}
			sb.Append("]");
			return sb.ToString();
		}

		public virtual void SetLoggerLevel(Level level)
		{
			logger.SetLevel(level);
		}
	}
}

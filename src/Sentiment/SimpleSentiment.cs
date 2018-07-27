using System;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Simple;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.Sentiment
{
	/// <summary>
	/// A simple sentiment classifier, inspired by Sida's Naive Bayes SVM
	/// paper.
	/// </summary>
	/// <remarks>
	/// A simple sentiment classifier, inspired by Sida's Naive Bayes SVM
	/// paper.
	/// The main goal of this class is to avoid the parse tree requirement of
	/// the RNN approach at:
	/// <see cref="SentimentPipeline"/>
	/// .
	/// </remarks>
	/// <author><a href="mailto:angeli@cs.stanford.edu">Gabor Angeli</a></author>
	public class SimpleSentiment
	{
		/// <summary>A logger for this class.</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Sentiment.SimpleSentiment));

		/// <summary>An appropriate pipeline object for featurizing training data</summary>
		private static Lazy<StanfordCoreNLP> pipeline = Lazy.Of(null);

		/// <summary>
		/// A single datum (presumably read from a training file) that encodes
		/// a sentence and an associated sentiment value.
		/// </summary>
		private class SentimentDatum
		{
			/// <summary>The sentence to classify.</summary>
			public readonly string sentence;

			/// <summary>The sentiment class of the sentence</summary>
			public readonly SentimentClass sentiment;

			/// <summary>The trivial constructor</summary>
			private SentimentDatum(string sentence, SentimentClass sentiment)
			{
				this.sentence = sentence;
				this.sentiment = sentiment;
			}

			/// <summary>Annotate this datum, and return it as a CoreMap.</summary>
			internal virtual ICoreMap AsCoreMap()
			{
				Annotation ann;
				if (string.Empty.Equals(sentence.Trim()))
				{
					switch (sentiment)
					{
						case SentimentClass.VeryPositive:
						{
							ann = new Annotation("cats are super awesome!");
							break;
						}

						case SentimentClass.Positive:
						{
							ann = new Annotation("cats are great");
							break;
						}

						case SentimentClass.Neutral:
						{
							ann = new Annotation("cats have tails");
							break;
						}

						case SentimentClass.Negative:
						{
							ann = new Annotation("cats suck");
							break;
						}

						case SentimentClass.VeryNegative:
						{
							ann = new Annotation("cats are literally the worst, I can't even.");
							break;
						}

						default:
						{
							throw new InvalidOperationException();
						}
					}
				}
				else
				{
					ann = new Annotation(sentence);
				}
				pipeline.Get().Annotate(ann);
				return ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[0];
			}
		}

		/// <summary>A simple regex for alpha words.</summary>
		/// <remarks>A simple regex for alpha words. That is, words matching [a-zA-Z]</remarks>
		private static readonly Pattern alpha = Pattern.Compile("[a-zA-Z]+");

		/// <summary>A simple regex for number tokens.</summary>
		/// <remarks>A simple regex for number tokens. That is, words matching [0-9]</remarks>
		private static readonly Pattern number = Pattern.Compile("[0-9]+");

		/// <summary>The underlying classifier we have trained to detect sentiment.</summary>
		private readonly IClassifier<SentimentClass, string> impl;

		/// <summary>Featurize a given sentence.</summary>
		/// <param name="sentence">The sentence to featurize.</param>
		/// <returns>A counter encoding the featurized sentence.</returns>
		private static ICounter<string> Featurize(ICoreMap sentence)
		{
			ClassicCounter<string> features = new ClassicCounter<string>();
			string lastLemma = "^";
			foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				string lemma = token.Lemma().ToLower();
				if (number.Matcher(lemma).Matches())
				{
					features.IncrementCount("**num**");
				}
				else
				{
					features.IncrementCount(lemma);
				}
				if (alpha.Matcher(lemma).Matches())
				{
					features.IncrementCount(lastLemma + "__" + lemma);
					lastLemma = lemma;
				}
			}
			features.IncrementCount(lastLemma + "__$");
			return features;
		}

		/// <summary>Create a new sentiment classifier object.</summary>
		/// <remarks>
		/// Create a new sentiment classifier object.
		/// This is really just a shallow wrapper around a classifier...
		/// </remarks>
		/// <param name="impl">The classifier doing the heavy lifting.</param>
		private SimpleSentiment(IClassifier<SentimentClass, string> impl)
		{
			this.impl = impl;
		}

		/// <summary>Get the sentiment of a sentence.</summary>
		/// <param name="sentence">
		/// The sentence as a core map.
		/// POS tags and Lemmas are a prerequisite.
		/// See
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.PartOfSpeechAnnotation"/>
		/// and
		/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.LemmaAnnotation"/>
		/// .
		/// </param>
		/// <returns>The sentiment class of this sentence.</returns>
		public virtual SentimentClass Classify(ICoreMap sentence)
		{
			ICounter<string> features = Featurize(sentence);
			RVFDatum<SentimentClass, string> datum = new RVFDatum<SentimentClass, string>(features);
			return impl.ClassOf(datum);
		}

		/// <seealso cref="Classify(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		public virtual SentimentClass Classify(string text)
		{
			Annotation ann = new Annotation(text);
			pipeline.Get().Annotate(ann);
			ICoreMap sentence = ann.Get(typeof(CoreAnnotations.SentencesAnnotation))[0];
			ICounter<string> features = Featurize(sentence);
			RVFDatum<SentimentClass, string> datum = new RVFDatum<SentimentClass, string>(features);
			return impl.ClassOf(datum);
		}

		/// <summary>Train a sentiment model from a set of data.</summary>
		/// <param name="data">The data to train the model from.</param>
		/// <param name="modelLocation">
		/// An optional location to save the model.
		/// Note that this stream will be closed in this method,
		/// and should not be written to thereafter.
		/// </param>
		/// <returns>A sentiment classifier, ready to use.</returns>
		public static SimpleSentiment Train(IStream<SimpleSentiment.SentimentDatum> data, Optional<OutputStream> modelLocation)
		{
			// Some useful variables configuring how we train
			bool useL1 = true;
			double sigma = 1.0;
			int featureCountThreshold = 5;
			// Featurize the data
			Redwood.Util.ForceTrack("Featurizing");
			RVFDataset<SentimentClass, string> dataset = new RVFDataset<SentimentClass, string>();
			AtomicInteger datasize = new AtomicInteger(0);
			ICounter<SentimentClass> distribution = new ClassicCounter<SentimentClass>();
			data.Unordered().Parallel().Map(null).ForEach(null);
			Redwood.Util.EndTrack("Featurizing");
			// Print label distribution
			Redwood.Util.StartTrack("Distribution");
			foreach (SentimentClass label in SentimentClass.Values())
			{
				Redwood.Util.Log(string.Format("%7d", (int)distribution.GetCount(label)) + "   " + label);
			}
			Redwood.Util.EndTrack("Distribution");
			// Train the classifier
			Redwood.Util.ForceTrack("Training");
			if (featureCountThreshold > 1)
			{
				dataset.ApplyFeatureCountThreshold(featureCountThreshold);
			}
			dataset.Randomize(42L);
			LinearClassifierFactory<SentimentClass, string> factory = new LinearClassifierFactory<SentimentClass, string>();
			factory.SetVerbose(true);
			try
			{
				factory.SetMinimizerCreator(null);
			}
			catch (Exception)
			{
			}
			factory.SetSigma(sigma);
			LinearClassifier<SentimentClass, string> classifier = factory.TrainClassifier(dataset);
			// Optionally save the model
			modelLocation.IfPresent(null);
			Redwood.Util.EndTrack("Training");
			// Evaluate the model
			Redwood.Util.ForceTrack("Evaluating");
			factory.SetVerbose(false);
			double sumAccuracy = 0.0;
			ICounter<SentimentClass> sumP = new ClassicCounter<SentimentClass>();
			ICounter<SentimentClass> sumR = new ClassicCounter<SentimentClass>();
			int numFolds = 4;
			for (int fold = 0; fold < numFolds; ++fold)
			{
				Pair<GeneralDataset<SentimentClass, string>, GeneralDataset<SentimentClass, string>> trainTest = dataset.SplitOutFold(fold, numFolds);
				LinearClassifier<SentimentClass, string> foldClassifier = factory.TrainClassifierWithInitialWeights(trainTest.first, classifier);
				// convex objective, so this should be OK
				sumAccuracy += foldClassifier.EvaluateAccuracy(trainTest.second);
				foreach (SentimentClass label_1 in SentimentClass.Values())
				{
					Pair<double, double> pr = foldClassifier.EvaluatePrecisionAndRecall(trainTest.second, label_1);
					sumP.IncrementCount(label_1, pr.first);
					sumP.IncrementCount(label_1, pr.second);
				}
			}
			DecimalFormat df = new DecimalFormat("0.000%");
			log.Info("----------");
			double aveAccuracy = sumAccuracy / ((double)numFolds);
			log.Info(string.Empty + numFolds + "-fold accuracy: " + df.Format(aveAccuracy));
			log.Info(string.Empty);
			foreach (SentimentClass label_2 in SentimentClass.Values())
			{
				double p = sumP.GetCount(label_2) / numFolds;
				double r = sumR.GetCount(label_2) / numFolds;
				log.Info(label_2 + " (P)  = " + df.Format(p));
				log.Info(label_2 + " (R)  = " + df.Format(r));
				log.Info(label_2 + " (F1) = " + df.Format(2 * p * r / (p + r)));
				log.Info(string.Empty);
			}
			log.Info("----------");
			Redwood.Util.EndTrack("Evaluating");
			// Return
			return new SimpleSentiment(classifier);
		}

		private static IStream<SimpleSentiment.SentimentDatum> Imdb(string path, SentimentClass label)
		{
			return StreamSupport.Stream(IOUtils.IterFilesRecursive(new File(path)).Spliterator(), true).Map(null);
		}

		private static IStream<SimpleSentiment.SentimentDatum> Stanford(string path)
		{
			return StreamSupport.Stream(IOUtils.ReadLines(path).Spliterator(), true).Map(null);
		}

		private static IStream<SimpleSentiment.SentimentDatum> Twitter(string path)
		{
			return StreamSupport.Stream(IOUtils.ReadLines(path).Spliterator(), true).Map(null);
		}

		/// <exception cref="System.IO.IOException"/>
		private static IStream<SimpleSentiment.SentimentDatum> Unlabelled(string path)
		{
			return StreamSupport.Stream(IOUtils.IterFilesRecursive(new File(path)).Spliterator(), true).FlatMap(null);
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			RedwoodConfiguration.Standard().Apply();
			Redwood.Util.StartTrack("main");
			// Read the data
			IStream<SimpleSentiment.SentimentDatum> data = IStream.Concat(IStream.Concat(IStream.Concat(Imdb("/users/gabor/tmp/aclImdb/train/pos", SentimentClass.Positive), Imdb("/users/gabor/tmp/aclImdb/train/neg", SentimentClass.Negative)), IStream.Concat
				(Imdb("/users/gabor/tmp/aclImdb/test/pos", SentimentClass.Positive), Imdb("/users/gabor/tmp/aclImdb/test/neg", SentimentClass.Negative))), IStream.Concat(IStream.Concat(Stanford("/users/gabor/tmp/train.tsv"), Stanford("/users/gabor/tmp/test.tsv"
				)), IStream.Concat(Twitter("/users/gabor/tmp/twitter.csv"), Unlabelled("/users/gabor/tmp/wikipedia"))));
			// Train the model
			OutputStream stream = IOUtils.GetFileOutputStream("/users/gabor/tmp/model.ser.gz");
			SimpleSentiment classifier = SimpleSentiment.Train(data, Optional.Of(stream));
			stream.Close();
			log.Info(classifier.Classify("I think life is great"));
			Redwood.Util.EndTrack("main");
		}
		// 85.8
	}
}

using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Pairwise mention-classification model.</summary>
	/// <author>Kevin Clark</author>
	public class PairwiseModel
	{
		public readonly string name;

		private readonly int trainingExamples;

		private readonly int epochs;

		protected internal readonly SimpleLinearClassifier classifier;

		private readonly double singletonRatio;

		private readonly string str;

		protected internal readonly MetaFeatureExtractor meta;

		public class Builder
		{
			private readonly string name;

			private readonly MetaFeatureExtractor meta;

			private readonly string source = StatisticalCorefTrainer.extractedFeaturesFile;

			private int trainingExamples = 100000000;

			private int epochs = 8;

			private SimpleLinearClassifier.ILoss loss = SimpleLinearClassifier.Log();

			private SimpleLinearClassifier.ILearningRateSchedule learningRateSchedule = SimpleLinearClassifier.AdaGrad(0.05, 30.0);

			private double regularizationStrength = 1e-7;

			private double singletonRatio = 0.3;

			private string modelFile = null;

			public Builder(string name, MetaFeatureExtractor meta)
			{
				// output in config file with reflection
				this.name = name;
				this.meta = meta;
			}

			public virtual PairwiseModel.Builder TrainingExamples(int trainingExamples)
			{
				this.trainingExamples = trainingExamples;
				return this;
			}

			public virtual PairwiseModel.Builder Epochs(int epochs)
			{
				this.epochs = epochs;
				return this;
			}

			public virtual PairwiseModel.Builder SingletonRatio(double singletonRatio)
			{
				this.singletonRatio = singletonRatio;
				return this;
			}

			public virtual PairwiseModel.Builder Loss(SimpleLinearClassifier.ILoss loss)
			{
				this.loss = loss;
				return this;
			}

			public virtual PairwiseModel.Builder RegularizationStrength(double regularizationStrength)
			{
				this.regularizationStrength = regularizationStrength;
				return this;
			}

			public virtual PairwiseModel.Builder LearningRateSchedule(SimpleLinearClassifier.ILearningRateSchedule learningRateSchedule)
			{
				this.learningRateSchedule = learningRateSchedule;
				return this;
			}

			public virtual PairwiseModel.Builder ModelPath(string modelFile)
			{
				this.modelFile = modelFile;
				return this;
			}

			public virtual PairwiseModel Build()
			{
				return new PairwiseModel(this);
			}
		}

		public static PairwiseModel.Builder NewBuilder(string name, MetaFeatureExtractor meta)
		{
			return new PairwiseModel.Builder(name, meta);
		}

		public PairwiseModel(PairwiseModel.Builder builder)
		{
			name = builder.name;
			meta = builder.meta;
			trainingExamples = builder.trainingExamples;
			epochs = builder.epochs;
			singletonRatio = builder.singletonRatio;
			classifier = new SimpleLinearClassifier(builder.loss, builder.learningRateSchedule, builder.regularizationStrength, builder.modelFile == null ? null : ((builder.modelFile.EndsWith(".ser") || builder.modelFile.EndsWith(".gz")) ? builder.modelFile
				 : StatisticalCorefTrainer.pairwiseModelsPath + builder.modelFile + "/model.ser"));
			str = StatisticalCorefTrainer.FieldValues(builder);
		}

		public virtual string GetDefaultOutputPath()
		{
			return StatisticalCorefTrainer.pairwiseModelsPath + name + "/";
		}

		public virtual SimpleLinearClassifier GetClassifier()
		{
			return classifier;
		}

		/// <exception cref="System.Exception"/>
		public virtual void WriteModel()
		{
			WriteModel(GetDefaultOutputPath());
		}

		/// <exception cref="System.Exception"/>
		public virtual void WriteModel(string outputPath)
		{
			File outDir = new File(outputPath);
			if (!outDir.Exists())
			{
				outDir.Mkdir();
			}
			using (PrintWriter writer = new PrintWriter(outputPath + "config", "UTF-8"))
			{
				writer.Print(str);
			}
			using (PrintWriter writer_1 = new PrintWriter(outputPath + "/weights", "UTF-8"))
			{
				classifier.PrintWeightVector(writer_1);
			}
			classifier.WriteWeights(outputPath + "/model.ser");
		}

		public virtual void Learn(Example example, IDictionary<int, CompressedFeatureVector> mentionFeatures, Compressor<string> compressor)
		{
			ICounter<string> features = meta.GetFeatures(example, mentionFeatures, compressor);
			classifier.Learn(features, example.label == 1.0 ? 1.0 : -1.0, 1.0);
		}

		public virtual void Learn(Example example, IDictionary<int, CompressedFeatureVector> mentionFeatures, Compressor<string> compressor, double weight)
		{
			ICounter<string> features = meta.GetFeatures(example, mentionFeatures, compressor);
			classifier.Learn(features, example.label == 1.0 ? 1.0 : -1.0, weight);
		}

		public virtual void Learn(Example correct, Example incorrect, IDictionary<int, CompressedFeatureVector> mentionFeatures, Compressor<string> compressor, double weight)
		{
			ICounter<string> cFeatures = null;
			ICounter<string> iFeatures = null;
			if (correct != null)
			{
				cFeatures = meta.GetFeatures(correct, mentionFeatures, compressor);
			}
			if (incorrect != null)
			{
				iFeatures = meta.GetFeatures(incorrect, mentionFeatures, compressor);
			}
			if (correct == null || incorrect == null)
			{
				if (singletonRatio != 0)
				{
					if (correct != null)
					{
						classifier.Learn(cFeatures, 1.0, weight * singletonRatio);
					}
					if (incorrect != null)
					{
						classifier.Learn(iFeatures, -1.0, weight * singletonRatio);
					}
				}
			}
			else
			{
				classifier.Learn(cFeatures, 1.0, weight);
				classifier.Learn(iFeatures, -1.0, weight);
			}
		}

		public virtual double Predict(Example example, IDictionary<int, CompressedFeatureVector> mentionFeatures, Compressor<string> compressor)
		{
			ICounter<string> features = meta.GetFeatures(example, mentionFeatures, compressor);
			return classifier.Label(features);
		}

		public virtual int GetNumTrainingExamples()
		{
			return trainingExamples;
		}

		public virtual int GetNumEpochs()
		{
			return epochs;
		}
	}
}

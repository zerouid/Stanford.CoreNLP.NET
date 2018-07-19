using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>
	/// A simple linear classifier trained by SGD with support for several different loss functions
	/// and learning rate schedules.
	/// </summary>
	/// <author>Kevin Clark</author>
	public class SimpleLinearClassifier
	{
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Coref.Statistical.SimpleLinearClassifier));

		private readonly SimpleLinearClassifier.ILoss defaultLoss;

		private readonly SimpleLinearClassifier.ILearningRateSchedule learningRateSchedule;

		private readonly double regularizationStrength;

		private readonly ICounter<string> weights;

		private readonly ICounter<string> accessTimes;

		private int examplesSeen;

		public SimpleLinearClassifier(SimpleLinearClassifier.ILoss loss, SimpleLinearClassifier.ILearningRateSchedule learningRateSchedule, double regularizationStrength)
			: this(loss, learningRateSchedule, regularizationStrength, null)
		{
		}

		public SimpleLinearClassifier(SimpleLinearClassifier.ILoss loss, SimpleLinearClassifier.ILearningRateSchedule learningRateSchedule, double regularizationStrength, string modelFile)
		{
			if (modelFile != null)
			{
				try
				{
					if (modelFile.EndsWith(".tab.gz"))
					{
						Timing.StartDoing("Reading " + modelFile);
						this.weights = Counters.DeserializeStringCounter(modelFile);
						Timing.EndDoing("Reading " + modelFile);
					}
					else
					{
						this.weights = IOUtils.ReadObjectAnnouncingTimingFromURLOrClasspathOrFileSystem(log, "Loading coref model", modelFile);
					}
				}
				catch (Exception e)
				{
					throw new Exception("Error leading weights from " + modelFile, e);
				}
			}
			else
			{
				this.weights = new ClassicCounter<string>();
			}
			this.defaultLoss = loss;
			this.regularizationStrength = regularizationStrength;
			this.learningRateSchedule = learningRateSchedule;
			accessTimes = new ClassicCounter<string>();
			examplesSeen = 0;
		}

		public virtual void Learn(ICounter<string> features, double label, double weight)
		{
			Learn(features, label, weight, defaultLoss);
		}

		public virtual void Learn(ICounter<string> features, double label, double weight, SimpleLinearClassifier.ILoss loss)
		{
			examplesSeen++;
			double dloss = loss.Derivative(label, WeightFeatureProduct(features));
			foreach (KeyValuePair<string, double> feature in features.EntrySet())
			{
				double dfeature = weight * (-dloss * feature.Value);
				if (dfeature != 0)
				{
					string featureName = feature.Key;
					learningRateSchedule.Update(featureName, dfeature);
					double lr = learningRateSchedule.GetLearningRate(featureName);
					double w = weights.GetCount(featureName);
					double dreg = weight * regularizationStrength * (examplesSeen - accessTimes.GetCount(featureName));
					double afterReg = (w - Math.Signum(w) * dreg * lr);
					weights.SetCount(featureName, (Math.Signum(afterReg) != Math.Signum(w) ? 0 : afterReg) + dfeature * lr);
					accessTimes.SetCount(featureName, examplesSeen);
				}
			}
		}

		public virtual double Label(ICounter<string> features)
		{
			return defaultLoss.Predict(WeightFeatureProduct(features));
		}

		public virtual double WeightFeatureProduct(ICounter<string> features)
		{
			double product = 0;
			foreach (KeyValuePair<string, double> feature in features.EntrySet())
			{
				product += feature.Value * weights.GetCount(feature.Key);
			}
			return product;
		}

		public virtual void SetWeight(string featureName, double weight)
		{
			weights.SetCount(featureName, weight);
		}

		public virtual SortedDictionary<string, double> GetWeightVector()
		{
			SortedDictionary<string, double> m = new SortedDictionary<string, double>(null);
			weights.EntrySet().Stream().ForEach(null);
			return m;
		}

		public virtual void PrintWeightVector()
		{
			PrintWeightVector(null);
		}

		public virtual void PrintWeightVector(PrintWriter writer)
		{
			SortedDictionary<string, double> sortedWeights = GetWeightVector();
			foreach (KeyValuePair<string, double> e in sortedWeights)
			{
				if (writer == null)
				{
					Redwood.Log("scoref.train", e.Key + " => " + e.Value);
				}
				else
				{
					writer.Println(e.Key + " => " + e.Value);
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public virtual void WriteWeights(string fname)
		{
			IOUtils.WriteObjectToFile(weights, fname);
		}

		public interface ILoss
		{
			// ---------- LOSS FUNCTIONS ----------
			double Predict(double product);

			double Derivative(double label, double product);
		}

		public static SimpleLinearClassifier.ILoss Log()
		{
			return new _ILoss_137();
		}

		private sealed class _ILoss_137 : SimpleLinearClassifier.ILoss
		{
			public _ILoss_137()
			{
			}

			public double Predict(double product)
			{
				return (1 - (1 / (1 + Math.Exp(product))));
			}

			public double Derivative(double label, double product)
			{
				return -label / (1 + Math.Exp(label * product));
			}

			public override string ToString()
			{
				return "log";
			}
		}

		public static SimpleLinearClassifier.ILoss QuadraticallySmoothedSVM(double gamma)
		{
			return new _ILoss_156(gamma);
		}

		private sealed class _ILoss_156 : SimpleLinearClassifier.ILoss
		{
			public _ILoss_156(double gamma)
			{
				this.gamma = gamma;
			}

			public double Predict(double product)
			{
				return product;
			}

			public double Derivative(double label, double product)
			{
				double mistake = label * product;
				return mistake >= 1 ? 0 : (mistake >= 1 - gamma ? (mistake - 1) * label / gamma : -label);
			}

			public override string ToString()
			{
				return string.Format("quadraticallySmoothed(%s)", gamma);
			}

			private readonly double gamma;
		}

		public static SimpleLinearClassifier.ILoss Hinge()
		{
			return QuadraticallySmoothedSVM(0);
		}

		public static SimpleLinearClassifier.ILoss MaxMargin(double h)
		{
			return new _ILoss_181(h);
		}

		private sealed class _ILoss_181 : SimpleLinearClassifier.ILoss
		{
			public _ILoss_181(double h)
			{
				this.h = h;
			}

			public double Predict(double product)
			{
				throw new NotSupportedException("Predict not implemented for max margin");
			}

			public double Derivative(double label, double product)
			{
				return product < -h ? 0 : 1;
			}

			public override string ToString()
			{
				return string.Format("max-margin(%s)", h);
			}

			private readonly double h;
		}

		public static SimpleLinearClassifier.ILoss Risk()
		{
			return new _ILoss_200();
		}

		private sealed class _ILoss_200 : SimpleLinearClassifier.ILoss
		{
			public _ILoss_200()
			{
			}

			public double Predict(double product)
			{
				return 1 / (1 + Math.Exp(product));
			}

			public double Derivative(double label, double product)
			{
				return -Math.Exp(product) / Math.Pow(1 + Math.Exp(product), 2);
			}

			public override string ToString()
			{
				return "risk";
			}
		}

		public interface ILearningRateSchedule
		{
			// ---------- LEARNING RATE SCHEDULES ----------
			void Update(string feature, double gradient);

			double GetLearningRate(string feature);
		}

		private abstract class CountBasedLearningRate : SimpleLinearClassifier.ILearningRateSchedule
		{
			private readonly ICounter<string> counter;

			public CountBasedLearningRate()
			{
				counter = new ClassicCounter<string>();
			}

			public virtual void Update(string feature, double gradient)
			{
				counter.IncrementCount(feature, GetCounterIncrement(gradient));
			}

			public virtual double GetLearningRate(string feature)
			{
				return GetLearningRate(counter.GetCount(feature));
			}

			public abstract double GetCounterIncrement(double gradient);

			public abstract double GetLearningRate(double count);
		}

		public static SimpleLinearClassifier.ILearningRateSchedule Constant(double eta)
		{
			return new _ILearningRateSchedule_247(eta);
		}

		private sealed class _ILearningRateSchedule_247 : SimpleLinearClassifier.ILearningRateSchedule
		{
			public _ILearningRateSchedule_247(double eta)
			{
				this.eta = eta;
			}

			public double GetLearningRate(string feature)
			{
				return eta;
			}

			public void Update(string feature, double gradient)
			{
			}

			public override string ToString()
			{
				return string.Format("constant(%s)", eta);
			}

			private readonly double eta;
		}

		public static SimpleLinearClassifier.ILearningRateSchedule InvScaling(double eta, double p)
		{
			return new _CountBasedLearningRate_264(eta, p);
		}

		private sealed class _CountBasedLearningRate_264 : SimpleLinearClassifier.CountBasedLearningRate
		{
			public _CountBasedLearningRate_264(double eta, double p)
			{
				this.eta = eta;
				this.p = p;
			}

			public override double GetCounterIncrement(double gradient)
			{
				return 1.0;
			}

			public override double GetLearningRate(double count)
			{
				return eta / Math.Pow(1 + count, p);
			}

			public override string ToString()
			{
				return string.Format("invScaling(%s, %s)", eta, p);
			}

			private readonly double eta;

			private readonly double p;
		}

		public static SimpleLinearClassifier.ILearningRateSchedule AdaGrad(double eta, double tau)
		{
			return new _CountBasedLearningRate_283(eta, tau);
		}

		private sealed class _CountBasedLearningRate_283 : SimpleLinearClassifier.CountBasedLearningRate
		{
			public _CountBasedLearningRate_283(double eta, double tau)
			{
				this.eta = eta;
				this.tau = tau;
			}

			public override double GetCounterIncrement(double gradient)
			{
				return gradient * gradient;
			}

			public override double GetLearningRate(double count)
			{
				return eta / (tau + Math.Sqrt(count));
			}

			public override string ToString()
			{
				return string.Format("adaGrad(%s, %s)", eta, tau);
			}

			private readonly double eta;

			private readonly double tau;
		}
	}
}

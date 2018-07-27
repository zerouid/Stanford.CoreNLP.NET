using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Utility classes for computing the B^3 and MUC coreference metrics</summary>
	/// <author>Kevin Clark</author>
	public class EvalUtils
	{
		public static double GetCombinedF1(double mucWeight, IList<IList<int>> gold, IList<Clusterer.Cluster> clusters, IDictionary<int, IList<int>> mentionToGold, IDictionary<int, Clusterer.Cluster> mentionToSystem)
		{
			EvalUtils.CombinedEvaluator combined = new EvalUtils.CombinedEvaluator(mucWeight);
			combined.Update(gold, clusters, mentionToGold, mentionToSystem);
			return combined.GetF1();
		}

		public static double F1(double pNum, double pDen, double rNum, double rDen)
		{
			double p = pNum == 0 ? 0 : pNum / pDen;
			double r = rNum == 0 ? 0 : rNum / rDen;
			return p == 0 ? 0 : 2 * p * r / (p + r);
		}

		public interface IEvaluator
		{
			void Update(IList<IList<int>> gold, IList<Clusterer.Cluster> clusters, IDictionary<int, IList<int>> mentionToGold, IDictionary<int, Clusterer.Cluster> mentionToSystem);

			double GetF1();
		}

		public class CombinedEvaluator : EvalUtils.IEvaluator
		{
			private readonly EvalUtils.B3Evaluator b3Evaluator;

			private readonly EvalUtils.MUCEvaluator mucEvaluator;

			private readonly double mucWeight;

			public CombinedEvaluator(double mucWeight)
			{
				b3Evaluator = new EvalUtils.B3Evaluator();
				mucEvaluator = new EvalUtils.MUCEvaluator();
				this.mucWeight = mucWeight;
			}

			public virtual void Update(IList<IList<int>> gold, IList<Clusterer.Cluster> clusters, IDictionary<int, IList<int>> mentionToGold, IDictionary<int, Clusterer.Cluster> mentionToSystem)
			{
				if (mucWeight != 1)
				{
					b3Evaluator.Update(gold, clusters, mentionToGold, mentionToSystem);
				}
				if (mucWeight != 0)
				{
					mucEvaluator.Update(gold, clusters, mentionToGold, mentionToSystem);
				}
			}

			public virtual double GetF1()
			{
				return (mucWeight == 0 ? 0 : mucWeight * mucEvaluator.GetF1()) + (mucWeight == 1 ? 0 : (1 - mucWeight) * b3Evaluator.GetF1());
			}
		}

		public abstract class AbstractEvaluator : EvalUtils.IEvaluator
		{
			public double pNum;

			public double pDen;

			public double rNum;

			public double rDen;

			public virtual void Update(IList<IList<int>> gold, IList<Clusterer.Cluster> clusters, IDictionary<int, IList<int>> mentionToGold, IDictionary<int, Clusterer.Cluster> mentionToSystem)
			{
				IList<IList<int>> clustersAsList = clusters.Stream().Map(null).Collect(Collectors.ToList());
				IDictionary<int, IList<int>> mentionToSystemLists = mentionToSystem.Stream().Collect(Collectors.ToMap(null, null));
				Pair<double, double> prec = GetScore(clustersAsList, mentionToGold);
				Pair<double, double> rec = GetScore(gold, mentionToSystemLists);
				pNum += prec.first;
				pDen += prec.second;
				rNum += rec.first;
				rDen += rec.second;
			}

			public virtual double GetF1()
			{
				return F1(pNum, pDen, rNum, rDen);
			}

			public virtual double GetRecall()
			{
				return pNum == 0 ? 0 : pNum / pDen;
			}

			public virtual double GetPrecision()
			{
				return rNum == 0 ? 0 : rNum / rDen;
			}

			public abstract Pair<double, double> GetScore(IList<IList<int>> clusters, IDictionary<int, IList<int>> mentionToGold);
		}

		public class B3Evaluator : EvalUtils.AbstractEvaluator
		{
			public override Pair<double, double> GetScore(IList<IList<int>> clusters, IDictionary<int, IList<int>> mentionToGold)
			{
				double num = 0;
				int dem = 0;
				foreach (IList<int> c in clusters)
				{
					if (c.Count == 1)
					{
						continue;
					}
					ICounter<IList<int>> goldCounts = new ClassicCounter<IList<int>>();
					double correct = 0;
					foreach (int m in c)
					{
						IList<int> goldCluster = mentionToGold[m];
						if (goldCluster != null)
						{
							goldCounts.IncrementCount(goldCluster);
						}
					}
					foreach (KeyValuePair<IList<int>, double> e in goldCounts.EntrySet())
					{
						if (e.Key.Count != 1)
						{
							correct += e.Value * e.Value;
						}
					}
					num += correct / c.Count;
					dem += c.Count;
				}
				return new Pair<double, double>(num, (double)dem);
			}
		}

		public class MUCEvaluator : EvalUtils.AbstractEvaluator
		{
			public override Pair<double, double> GetScore(IList<IList<int>> clusters, IDictionary<int, IList<int>> mentionToGold)
			{
				int tp = 0;
				int predictedPositive = 0;
				foreach (IList<int> c in clusters)
				{
					predictedPositive += c.Count - 1;
					tp += c.Count;
					ICollection<IList<int>> linked = new HashSet<IList<int>>();
					foreach (int m in c)
					{
						IList<int> g = mentionToGold[m];
						if (g == null)
						{
							tp -= 1;
						}
						else
						{
							linked.Add(g);
						}
					}
					tp -= linked.Count;
				}
				return new Pair<double, double>((double)tp, (double)predictedPositive);
			}
		}
	}
}

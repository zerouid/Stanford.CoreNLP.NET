using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>Computes labeled precision and recall (evalb) at the constituent category level.</summary>
	/// <author>Roger Levy</author>
	/// <author>Spence Green</author>
	public class EvalbByCat : AbstractEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.EvalbByCat));

		private readonly Evalb evalb;

		private Pattern pLabelFilter = null;

		private readonly ICounter<ILabel> precisions;

		private readonly ICounter<ILabel> recalls;

		private readonly ICounter<ILabel> f1s;

		private readonly ICounter<ILabel> precisions2;

		private readonly ICounter<ILabel> recalls2;

		private readonly ICounter<ILabel> pnums2;

		private readonly ICounter<ILabel> rnums2;

		public EvalbByCat(string str, bool runningAverages)
			: base(str, runningAverages)
		{
			// Only evaluate categories that match this regular expression
			evalb = new Evalb(str, false);
			precisions = new ClassicCounter<ILabel>();
			recalls = new ClassicCounter<ILabel>();
			f1s = new ClassicCounter<ILabel>();
			precisions2 = new ClassicCounter<ILabel>();
			recalls2 = new ClassicCounter<ILabel>();
			pnums2 = new ClassicCounter<ILabel>();
			rnums2 = new ClassicCounter<ILabel>();
		}

		public EvalbByCat(string str, bool runningAverages, string labelRegex)
			: this(str, runningAverages)
		{
			if (labelRegex != null)
			{
				pLabelFilter = Pattern.Compile(labelRegex.Trim());
			}
		}

		protected internal override ICollection<object> MakeObjects(Tree tree)
		{
			return ((ICollection<Constituent>)evalb.MakeObjects(tree));
		}

		private IDictionary<ILabel, ICollection<Constituent>> MakeObjectsByCat(Tree t)
		{
			IDictionary<ILabel, ICollection<Constituent>> objMap = Generics.NewHashMap();
			ICollection<Constituent> objSet = ((ICollection<Constituent>)MakeObjects(t));
			foreach (Constituent lc in objSet)
			{
				ILabel l = lc.Label();
				if (!objMap.Keys.Contains(l))
				{
					objMap[l] = Generics.NewHashSet<Constituent>();
				}
				objMap[l].Add(lc);
			}
			return objMap;
		}

		public override void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			if (gold == null || guess == null)
			{
				System.Console.Error.Printf("%s: Cannot compare against a null gold or guess tree!%n", this.GetType().FullName);
				return;
			}
			IDictionary<ILabel, ICollection<Constituent>> guessDeps = MakeObjectsByCat(guess);
			IDictionary<ILabel, ICollection<Constituent>> goldDeps = MakeObjectsByCat(gold);
			ICollection<ILabel> cats = Generics.NewHashSet(guessDeps.Keys);
			Sharpen.Collections.AddAll(cats, goldDeps.Keys);
			if (pw != null && runningAverages)
			{
				pw.Println("========================================");
				pw.Println("Labeled Bracketed Evaluation by Category");
				pw.Println("========================================");
			}
			++num;
			foreach (ILabel cat in cats)
			{
				ICollection<Constituent> thisGuessDeps = guessDeps.Contains(cat) ? guessDeps[cat] : Generics.NewHashSet<Constituent>();
				ICollection<Constituent> thisGoldDeps = goldDeps.Contains(cat) ? goldDeps[cat] : Generics.NewHashSet<Constituent>();
				double currentPrecision = Precision(thisGuessDeps, thisGoldDeps);
				double currentRecall = Precision(thisGoldDeps, thisGuessDeps);
				double currentF1 = (currentPrecision > 0.0 && currentRecall > 0.0 ? 2.0 / (1.0 / currentPrecision + 1.0 / currentRecall) : 0.0);
				precisions.IncrementCount(cat, currentPrecision);
				recalls.IncrementCount(cat, currentRecall);
				f1s.IncrementCount(cat, currentF1);
				precisions2.IncrementCount(cat, thisGuessDeps.Count * currentPrecision);
				pnums2.IncrementCount(cat, thisGuessDeps.Count);
				recalls2.IncrementCount(cat, thisGoldDeps.Count * currentRecall);
				rnums2.IncrementCount(cat, thisGoldDeps.Count);
				if (pw != null && runningAverages)
				{
					pw.Println(cat + "\tP: " + ((int)(currentPrecision * 10000)) / 100.0 + " (sent ave " + ((int)(precisions.GetCount(cat) * 10000 / num)) / 100.0 + ") (evalb " + ((int)(precisions2.GetCount(cat) * 10000 / pnums2.GetCount(cat))) / 100.0 + ")");
					pw.Println("\tR: " + ((int)(currentRecall * 10000)) / 100.0 + " (sent ave " + ((int)(recalls.GetCount(cat) * 10000 / num)) / 100.0 + ") (evalb " + ((int)(recalls2.GetCount(cat) * 10000 / rnums2.GetCount(cat))) / 100.0 + ")");
					double cF1 = 2.0 / (rnums2.GetCount(cat) / recalls2.GetCount(cat) + pnums2.GetCount(cat) / precisions2.GetCount(cat));
					string emit = str + " F1: " + ((int)(currentF1 * 10000)) / 100.0 + " (sent ave " + ((int)(10000 * f1s.GetCount(cat) / num)) / 100.0 + ", evalb " + ((int)(10000 * cF1)) / 100.0 + ")";
					pw.Println(emit);
				}
			}
			if (pw != null && runningAverages)
			{
				pw.Println("========================================");
			}
		}

		private ICollection<ILabel> GetEvalLabelSet(ICollection<ILabel> labelSet)
		{
			if (pLabelFilter == null)
			{
				return Generics.NewHashSet(precisions.KeySet());
			}
			else
			{
				ICollection<ILabel> evalSet = Generics.NewHashSet(precisions.KeySet().Count);
				foreach (ILabel label in labelSet)
				{
					if (pLabelFilter.Matcher(label.Value()).Matches())
					{
						evalSet.Add(label);
					}
				}
				return evalSet;
			}
		}

		public override void Display(bool verbose, PrintWriter pw)
		{
			if (precisions.KeySet().Count != recalls.KeySet().Count)
			{
				log.Error("Different counts for precisions and recalls!");
				return;
			}
			ICollection<ILabel> cats = GetEvalLabelSet(precisions.KeySet());
			Random rand = new Random();
			IDictionary<double, ILabel> f1Map = new SortedDictionary<double, ILabel>();
			foreach (ILabel cat in cats)
			{
				double pnum2 = pnums2.GetCount(cat);
				double rnum2 = rnums2.GetCount(cat);
				double prec = precisions2.GetCount(cat) / pnum2;
				double rec = recalls2.GetCount(cat) / rnum2;
				double f1 = 2.0 / (1.0 / prec + 1.0 / rec);
				if (f1.Equals(double.NaN))
				{
					f1 = -1.0;
				}
				if (f1Map.Contains(f1))
				{
					f1Map[f1 + (rand.NextDouble() / 1000.0)] = cat;
				}
				else
				{
					f1Map[f1] = cat;
				}
			}
			pw.Println("============================================================");
			pw.Println("Labeled Bracketed Evaluation by Category -- final statistics");
			pw.Println("============================================================");
			// Per category
			double catPrecisions = 0.0;
			double catPrecisionNums = 0.0;
			double catRecalls = 0.0;
			double catRecallNums = 0.0;
			foreach (ILabel cat_1 in f1Map.Values)
			{
				double pnum2 = pnums2.GetCount(cat_1);
				double rnum2 = rnums2.GetCount(cat_1);
				double prec = precisions2.GetCount(cat_1) / pnum2;
				prec *= 100.0;
				double rec = recalls2.GetCount(cat_1) / rnum2;
				rec *= 100.0;
				double f1 = 2.0 / (1.0 / prec + 1.0 / rec);
				catPrecisions += precisions2.GetCount(cat_1);
				catPrecisionNums += pnum2;
				catRecalls += recalls2.GetCount(cat_1);
				catRecallNums += rnum2;
				string Lp = pnum2 == 0.0 ? "N/A" : string.Format("%.2f", prec);
				string Lr = rnum2 == 0.0 ? "N/A" : string.Format("%.2f", rec);
				string F1 = (pnum2 == 0.0 || rnum2 == 0.0) ? "N/A" : string.Format("%.2f", f1);
				pw.Printf("%s\tLP: %s\tguessed: %d\tLR: %s\tgold: %d\t F1: %s%n", cat_1.Value(), Lp, (int)pnum2, Lr, (int)rnum2, F1);
			}
			pw.Println("============================================================");
			// Totals
			double prec_1 = catPrecisions / catPrecisionNums;
			double rec_1 = catRecalls / catRecallNums;
			double f1_1 = (2 * prec_1 * rec_1) / (prec_1 + rec_1);
			pw.Printf("Total\tLP: %.2f\tguessed: %d\tLR: %.2f\tgold: %d\t F1: %.2f%n", prec_1 * 100.0, (int)catPrecisionNums, rec_1 * 100.0, (int)catRecallNums, f1_1 * 100.0);
			pw.Println("============================================================");
		}
	}
}

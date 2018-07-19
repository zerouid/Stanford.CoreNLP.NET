using System.Collections.Generic;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Metrics
{
	/// <summary>A framework for Set-based precision/recall/F1 evaluation.</summary>
	/// <author>Dan Klein</author>
	public abstract class AbstractEval : IEval
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Metrics.AbstractEval));

		private const bool Debug = false;

		protected internal readonly string str;

		protected internal readonly bool runningAverages;

		private double precision = 0.0;

		private double recall = 0.0;

		private double f1 = 0.0;

		protected internal double num = 0.0;

		private double exact = 0.0;

		private double precision2 = 0.0;

		private double recall2 = 0.0;

		private double pnum2 = 0.0;

		private double rnum2 = 0.0;

		protected internal double curF1 = 0.0;

		public AbstractEval()
			: this(true)
		{
		}

		public AbstractEval(bool runningAverages)
			: this(string.Empty, runningAverages)
		{
		}

		public AbstractEval(string str)
			: this(str, true)
		{
		}

		public AbstractEval(string str, bool runningAverages)
		{
			this.str = str;
			this.runningAverages = runningAverages;
		}

		public virtual double GetSentAveF1()
		{
			return f1 / num;
		}

		public virtual double GetEvalbF1()
		{
			return 2.0 / (rnum2 / recall2 + pnum2 / precision2);
		}

		/// <summary>
		/// Return the evalb F1% from the last call to
		/// <see cref="Evaluate(Edu.Stanford.Nlp.Trees.Tree, Edu.Stanford.Nlp.Trees.Tree)"/>
		/// .
		/// </summary>
		/// <returns>The F1 percentage</returns>
		public virtual double GetLastF1()
		{
			return curF1 * 100.0;
		}

		/// <returns>
		/// The evalb (micro-averaged) F1 times 100 to make it
		/// a number between 0 and 100.
		/// </returns>
		public virtual double GetEvalbF1Percent()
		{
			return GetEvalbF1() * 100.0;
		}

		public virtual double GetExact()
		{
			return exact / num;
		}

		public virtual double GetExactPercent()
		{
			return GetExact() * 100.0;
		}

		public virtual int GetNum()
		{
			return (int)num;
		}

		// should be able to pass in a comparator!
		protected internal static double Precision<_T0, _T1>(ICollection<_T0> s1, ICollection<_T1> s2)
		{
			double n = 0.0;
			double p = 0.0;
			foreach (object o1 in s1)
			{
				if (s2.Contains(o1))
				{
					p += 1.0;
				}
				n += 1.0;
			}
			return (n > 0.0 ? p / n : 0.0);
		}

		protected internal abstract ICollection<object> MakeObjects(Tree tree);

		public virtual void Evaluate(Tree guess, Tree gold)
		{
			Evaluate(guess, gold, new PrintWriter(System.Console.Out, true));
		}

		/* Evaluates precision and recall by calling makeObjects() to make a
		* set of structures for guess Tree and gold Tree, and compares them
		* with each other.
		*/
		public virtual void Evaluate(Tree guess, Tree gold, PrintWriter pw)
		{
			Evaluate(guess, gold, pw, 1.0);
		}

		public virtual void Evaluate(Tree guess, Tree gold, PrintWriter pw, double weight)
		{
			ICollection<object> dep1 = MakeObjects(guess);
			ICollection<object> dep2 = MakeObjects(gold);
			double curPrecision = Precision(dep1, dep2);
			double curRecall = Precision(dep2, dep1);
			curF1 = (curPrecision > 0.0 && curRecall > 0.0 ? 2.0 / (1.0 / curPrecision + 1.0 / curRecall) : 0.0);
			precision += curPrecision * weight;
			recall += curRecall * weight;
			f1 += curF1 * weight;
			num += weight;
			precision2 += dep1.Count * curPrecision * weight;
			pnum2 += dep1.Count * weight;
			recall2 += dep2.Count * curRecall * weight;
			rnum2 += dep2.Count * weight;
			if (curF1 > 0.9999)
			{
				exact += 1.0;
			}
			if (pw != null)
			{
				pw.Print(" P: " + ((int)(curPrecision * 10000)) / 100.0);
				if (runningAverages)
				{
					pw.Println(" (sent ave " + ((int)(precision * 10000 / num)) / 100.0 + ") (evalb " + ((int)(precision2 * 10000 / pnum2)) / 100.0 + ")");
				}
				pw.Print(" R: " + ((int)(curRecall * 10000)) / 100.0);
				if (runningAverages)
				{
					pw.Print(" (sent ave " + ((int)(recall * 10000 / num)) / 100.0 + ") (evalb " + ((int)(recall2 * 10000 / rnum2)) / 100.0 + ")");
				}
				pw.Println();
				double cF1 = 2.0 / (rnum2 / recall2 + pnum2 / precision2);
				pw.Print(str + " F1: " + ((int)(curF1 * 10000)) / 100.0);
				if (runningAverages)
				{
					pw.Print(" (sent ave " + ((int)(10000 * f1 / num)) / 100.0 + ", evalb " + ((int)(10000 * cF1)) / 100.0 + ")   Exact: " + ((int)(10000 * exact / num)) / 100.0);
				}
				//      pw.println(" N: " + getNum());
				pw.Println(" N: " + num);
			}
		}

		/*
		Sentence s = guess.yield();
		for (Object obj : s) {
		if (curF1 < 0.7) {
		badwords.incrementCount(obj);
		} else {
		goodwords.incrementCount(obj);
		}
		}
		*/
		/*
		private Counter goodwords = new Counter();
		private Counter badwords = new Counter();
		
		public void printGoodBad() {
		System.out.println("Printing bad categories");
		for (Object key : Counters.keysAbove(badwords, 5.0)) {
		System.out.println("In badwords 5 times: " + key);
		double numb = badwords.getCount(key);
		double numg = goodwords.getCount(key);
		if (numb / (numb + numg) > 0.1) {
		System.out.println("Bad word!  " + key + " (" +
		(numb / (numb + numg)) + " bad)");
		// EncodingPrintWriter.out.println("Bad word!  " + key + " (" +
		//                 (numb / (numb + numg)) + " bad)",
		//                              "GB18030");
		}
		}
		}
		*/
		public virtual void Display(bool verbose)
		{
			Display(verbose, new PrintWriter(System.Console.Out, true));
		}

		public virtual void Display(bool verbose, PrintWriter pw)
		{
			double prec = precision2 / pnum2;
			//(num > 0.0 ? precision/num : 0.0);
			double rec = recall2 / rnum2;
			//(num > 0.0 ? recall/num : 0.0);
			double f = 2.0 / (1.0 / prec + 1.0 / rec);
			//(num > 0.0 ? f1/num : 0.0);
			//System.out.println(" Precision: "+((int)(10000.0*prec))/100.0);
			//System.out.println(" Recall:    "+((int)(10000.0*rec))/100.0);
			//System.out.println(" F1:        "+((int)(10000.0*f))/100.0);
			pw.Println(str + " summary evalb: LP: " + ((int)(10000.0 * prec)) / 100.0 + " LR: " + ((int)(10000.0 * rec)) / 100.0 + " F1: " + ((int)(10000.0 * f)) / 100.0 + " Exact: " + ((int)(10000.0 * exact / num)) / 100.0 + " N: " + GetNum());
		}

		public class RuleErrorEval : AbstractEval
		{
			private ClassicCounter<string> over = new ClassicCounter<string>();

			private ClassicCounter<string> under = new ClassicCounter<string>();

			/*
			double prec = (num > 0.0 ? precision/num : 0.0);
			double rec = (num > 0.0 ? recall/num : 0.0);
			double f = (num > 0.0 ? f1/num : 0.0);
			System.out.println(" Precision: "+prec);
			System.out.println(" Recall:    "+rec);
			System.out.println(" F1:        "+f);
			*/
			//private boolean verbose = false;
			protected internal static string Localize(Tree tree)
			{
				if (tree.IsLeaf())
				{
					return string.Empty;
				}
				StringBuilder sb = new StringBuilder();
				sb.Append(tree.Label());
				sb.Append(" ->");
				for (int i = 0; i < tree.Children().Length; i++)
				{
					sb.Append(' ');
					sb.Append(tree.Children()[i].Label());
				}
				return sb.ToString();
			}

			protected internal override ICollection<object> MakeObjects(Tree tree)
			{
				ICollection<string> localTrees = Generics.NewHashSet();
				foreach (Tree st in tree.SubTreeList())
				{
					localTrees.Add(Localize(st));
				}
				return localTrees;
			}

			public override void Evaluate(Tree t1, Tree t2, PrintWriter pw)
			{
				ICollection<string> s1 = ((ICollection<string>)MakeObjects(t1));
				ICollection<string> s2 = ((ICollection<string>)MakeObjects(t2));
				foreach (string o1 in s1)
				{
					if (!s2.Contains(o1))
					{
						over.IncrementCount(o1);
					}
				}
				foreach (string o2 in s2)
				{
					if (!s1.Contains(o2))
					{
						under.IncrementCount(o2);
					}
				}
			}

			private static void Display<T>(ClassicCounter<T> c, int num, PrintWriter pw)
			{
				IList<T> rules = new List<T>(c.KeySet());
				rules.Sort(Counters.ToComparatorDescending(c));
				int rSize = rules.Count;
				if (num > rSize)
				{
					num = rSize;
				}
				for (int i = 0; i < num; i++)
				{
					pw.Println(rules[i] + " " + c.GetCount(rules[i]));
				}
			}

			public override void Display(bool verbose, PrintWriter pw)
			{
				//this.verbose = verbose;
				pw.Println("Most frequently underproposed rules:");
				Display(under, (verbose ? 100 : 10), pw);
				pw.Println("Most frequently overproposed rules:");
				Display(over, (verbose ? 100 : 10), pw);
			}

			public RuleErrorEval(string str)
				: base(str)
			{
			}
		}

		/// <summary>This class counts which categories are over and underproposed in trees.</summary>
		public class CatErrorEval : AbstractEval
		{
			private ClassicCounter<string> over = new ClassicCounter<string>();

			private ClassicCounter<string> under = new ClassicCounter<string>();

			// end class RuleErrorEval
			/// <summary>Unused.</summary>
			/// <remarks>Unused. Fake satisfying the abstract class.</remarks>
			protected internal override ICollection<object> MakeObjects(Tree tree)
			{
				return null;
			}

			private static IList<string> MyMakeObjects(Tree tree)
			{
				IList<string> cats = new LinkedList<string>();
				foreach (Tree st in tree.SubTreeList())
				{
					cats.Add(st.Value());
				}
				return cats;
			}

			public override void Evaluate(Tree t1, Tree t2, PrintWriter pw)
			{
				IList<string> s1 = MyMakeObjects(t1);
				IList<string> s2 = MyMakeObjects(t2);
				IList<string> del2 = new LinkedList<string>(s2);
				// we delete out as we find them so we can score correctly a cat with
				// a certain cardinality in a tree.
				foreach (string o1 in s1)
				{
					if (!del2.Remove(o1))
					{
						over.IncrementCount(o1);
					}
				}
				foreach (string o2 in s2)
				{
					if (!s1.Remove(o2))
					{
						under.IncrementCount(o2);
					}
				}
			}

			private static void Display<T>(ClassicCounter<T> c, PrintWriter pw)
			{
				IList<T> cats = new List<T>(c.KeySet());
				cats.Sort(Counters.ToComparatorDescending(c));
				foreach (T ob in cats)
				{
					pw.Println(ob + " " + c.GetCount(ob));
				}
			}

			public override void Display(bool verbose, PrintWriter pw)
			{
				pw.Println("Most frequently underproposed categories:");
				Display(under, pw);
				pw.Println("Most frequently overproposed categories:");
				Display(over, pw);
			}

			public CatErrorEval(string str)
				: base(str)
			{
			}
		}

		/// <summary>This isn't really a kind of AbstractEval: we're sort of cheating here.</summary>
		public class ScoreEval : AbstractEval
		{
			internal double totScore = 0.0;

			internal double n = 0.0;

			internal NumberFormat nf = new DecimalFormat("0.000");

			// end class CatErrorEval
			protected internal override ICollection<object> MakeObjects(Tree tree)
			{
				return null;
			}

			public virtual void RecordScore(IKBestViterbiParser parser, PrintWriter pw)
			{
				double score = parser.GetBestScore();
				totScore += score;
				n++;
				if (pw != null)
				{
					pw.Print(str + " score: " + nf.Format(score));
					if (runningAverages)
					{
						pw.Print(" average score: " + nf.Format(totScore / n));
					}
					pw.Println();
				}
			}

			public override void Display(bool verbose, PrintWriter pw)
			{
				if (pw != null)
				{
					pw.Println(str + " total score: " + nf.Format(totScore) + " average score: " + ((n == 0.0) ? "N/A" : nf.Format(totScore / n)));
				}
			}

			public ScoreEval(string str, bool runningAverages)
				: base(str, runningAverages)
			{
			}
		}
		// end class DependencyEval
	}
}

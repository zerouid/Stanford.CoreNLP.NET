using System.Collections;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// See what sister annotation helps in treebank, based on support and
	/// KL divergence.
	/// </summary>
	/// <remarks>
	/// See what sister annotation helps in treebank, based on support and
	/// KL divergence.  Some code borrowing from ParentAnnotationStats.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <version>2003/02</version>
	public class SisterAnnotationStats : ITreeVisitor
	{
		public const bool DoTags = true;

		/// <summary>nodeRules is a HashMap -&gt; Counter: label-&gt;rewrite-&gt;count</summary>
		private IDictionary nodeRules = new Hashtable();

		/// <summary>
		/// leftRules and rightRules are HashMap -&gt; HashMap -&gt; Counter:
		/// label-&gt;sister_label-&gt;rewrite-&gt;count
		/// </summary>
		private IDictionary leftRules = new Hashtable();

		private IDictionary rightRules = new Hashtable();

		/// <summary>Minimum support * KL to be included in output and as feature</summary>
		public static readonly double[] Cutoffs = new double[] { 250.0, 500.0, 1000.0, 1500.0 };

		/// <summary>
		/// Minimum support of parent annotated node for grandparent to be
		/// studied.
		/// </summary>
		/// <remarks>
		/// Minimum support of parent annotated node for grandparent to be
		/// studied.  Just there to reduce runtime and printout size.
		/// </remarks>
		public const double Suppcutoff = 100.0;

		/// <summary>Does whatever one needs to do to a particular parse tree</summary>
		public virtual void VisitTree(Tree t)
		{
			Recurse(t, null);
		}

		/// <summary>p is parent</summary>
		public virtual void Recurse(Tree t, Tree p)
		{
			if (t.IsLeaf() || (t.IsPreTerminal() && (!DoTags)))
			{
				return;
			}
			if (!(p == null || t.Label().Value().Equals("ROOT")))
			{
				SisterCounters(t, p);
			}
			Tree[] kids = t.Children();
			foreach (Tree kid in kids)
			{
				Recurse(kid, t);
			}
		}

		/// <summary>string-value labels of left sisters; from inside to outside (right-left)</summary>
		public static IList<string> LeftSisterLabels(Tree t, Tree p)
		{
			IList<string> l = new List<string>();
			if (p == null)
			{
				return l;
			}
			Tree[] kids = p.Children();
			foreach (Tree kid in kids)
			{
				if (kid.Equals(t))
				{
					break;
				}
				else
				{
					l.Add(0, kid.Label().Value());
				}
			}
			return l;
		}

		/// <summary>string-value labels of right sisters; from inside to outside (left-right)</summary>
		public static IList<string> RightSisterLabels(Tree t, Tree p)
		{
			IList<string> l = new List<string>();
			if (p == null)
			{
				return l;
			}
			Tree[] kids = p.Children();
			for (int i = kids.Length - 1; i >= 0; i--)
			{
				if (kids[i].Equals(t))
				{
					break;
				}
				else
				{
					l.Add(kids[i].Label().Value());
				}
			}
			return l;
		}

		public static IList<string> KidLabels(Tree t)
		{
			Tree[] kids = t.Children();
			IList<string> l = new List<string>(kids.Length);
			foreach (Tree kid in kids)
			{
				l.Add(kid.Label().Value());
			}
			return l;
		}

		protected internal virtual void SisterCounters(Tree t, Tree p)
		{
			IList rewrite = KidLabels(t);
			IList left = LeftSisterLabels(t, p);
			IList right = RightSisterLabels(t, p);
			string label = t.Label().Value();
			if (!nodeRules.Contains(label))
			{
				nodeRules[label] = new ClassicCounter();
			}
			if (!rightRules.Contains(label))
			{
				rightRules[label] = new Hashtable();
			}
			if (!leftRules.Contains(label))
			{
				leftRules[label] = new Hashtable();
			}
			((ClassicCounter)nodeRules[label]).IncrementCount(rewrite);
			SideCounters(label, rewrite, left, leftRules);
			SideCounters(label, rewrite, right, rightRules);
		}

		protected internal virtual void SideCounters(string label, IList rewrite, IList sideSisters, IDictionary sideRules)
		{
			foreach (object sideSister in sideSisters)
			{
				string sis = (string)sideSister;
				if (!((IDictionary)sideRules[label]).Contains(sis))
				{
					((IDictionary)sideRules[label])[sis] = new ClassicCounter();
				}
				((ClassicCounter)((Hashtable)sideRules[label])[sis]).IncrementCount(rewrite);
			}
		}

		public virtual void PrintStats()
		{
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(2);
			// System.out.println("Node rules");
			// System.out.println(nodeRules);
			// System.out.println("Parent rules");
			// System.out.println(pRules);
			// System.out.println("Grandparent rules");
			// System.out.println(gPRules);
			// Store java code for selSplit
			StringBuilder[] javaSB = new StringBuilder[Cutoffs.Length];
			for (int i = 0; i < Cutoffs.Length; i++)
			{
				javaSB[i] = new StringBuilder("  private static String[] sisterSplit" + (i + 1) + " = new String[] {");
			}
			ArrayList topScores = new ArrayList();
			foreach (object o in nodeRules.Keys)
			{
				ArrayList answers = new ArrayList();
				string label = (string)o;
				ClassicCounter cntr = (ClassicCounter)nodeRules[label];
				double support = (cntr.TotalCount());
				System.Console.Out.WriteLine("Node " + label + " support is " + support);
				foreach (object o4 in ((Hashtable)leftRules[label]).Keys)
				{
					string sis = (string)o4;
					ClassicCounter cntr2 = (ClassicCounter)((Hashtable)leftRules[label])[sis];
					double support2 = (cntr2.TotalCount());
					/* alternative 1: use full distribution to calculate score */
					double kl = Counters.KlDivergence(cntr2, cntr);
					/* alternative 2: hold out test-context data to calculate score */
					/* this doesn't work because it can lead to zero-probability
					* data points hence infinite divergence */
					// 	Counter tempCounter = new Counter();
					// 	tempCounter.addCounter(cntr2);
					// 	for(Iterator i = tempCounter.seenSet().iterator(); i.hasNext();) {
					// 	  Object o = i.next();
					// 	  tempCounter.setCount(o,-1*tempCounter.countOf(o));
					// 	}
					// 	System.out.println(tempCounter); //debugging
					// 	tempCounter.addCounter(cntr);
					// 	System.out.println(tempCounter); //debugging
					// 	System.out.println(cntr);
					// 	double kl = cntr2.klDivergence(tempCounter);
					/* alternative 2 ends here */
					string annotatedLabel = label + "=l=" + sis;
					System.Console.Out.WriteLine("KL(" + annotatedLabel + "||" + label + ") = " + nf.Format(kl) + "\t" + "support(" + sis + ") = " + support2);
					answers.Add(new Pair(annotatedLabel, kl * support2));
					topScores.Add(new Pair(annotatedLabel, kl * support2));
				}
				foreach (object o3 in ((Hashtable)rightRules[label]).Keys)
				{
					string sis = (string)o3;
					ClassicCounter cntr2 = (ClassicCounter)((Hashtable)rightRules[label])[sis];
					double support2 = (cntr2.TotalCount());
					double kl = Counters.KlDivergence(cntr2, cntr);
					string annotatedLabel = label + "=r=" + sis;
					System.Console.Out.WriteLine("KL(" + annotatedLabel + "||" + label + ") = " + nf.Format(kl) + "\t" + "support(" + sis + ") = " + support2);
					answers.Add(new Pair(annotatedLabel, kl * support2));
					topScores.Add(new Pair(annotatedLabel, kl * support2));
				}
				// upto
				System.Console.Out.WriteLine("----");
				System.Console.Out.WriteLine("Sorted descending support * KL");
				answers.Sort(null);
				foreach (object answer in answers)
				{
					Pair p = (Pair)answer;
					double psd = ((double)p.Second());
					System.Console.Out.WriteLine(p.First() + ": " + nf.Format(psd));
					if (psd >= Cutoffs[0])
					{
						string annotatedLabel = (string)p.First();
						foreach (double Cutoff in Cutoffs)
						{
							if (psd >= Cutoff)
							{
							}
						}
					}
				}
				//javaSB[j].append("\"").append(annotatedLabel);
				//javaSB[j].append("\",");
				System.Console.Out.WriteLine();
			}
			topScores.Sort(null);
			string outString = "All enriched categories, sorted by score\n";
			foreach (object topScore in topScores)
			{
				Pair p = (Pair)topScore;
				double psd = ((double)p.Second());
				System.Console.Out.WriteLine(p.First() + ": " + nf.Format(psd));
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("  // Automatically generated by SisterAnnotationStats -- preferably don't edit");
			int k = Cutoffs.Length - 1;
			for (int j = 0; j < topScores.Count; j++)
			{
				Pair p = (Pair)topScores[j];
				double psd = ((double)p.Second());
				if (psd < Cutoffs[k])
				{
					if (k == 0)
					{
						break;
					}
					else
					{
						k--;
						j -= 1;
						// messy but should do it
						continue;
					}
				}
				javaSB[k].Append("\"").Append(p.First());
				javaSB[k].Append("\",");
			}
			for (int i_1 = 0; i_1 < Cutoffs.Length; i_1++)
			{
				int len = javaSB[i_1].Length;
				javaSB[i_1].Replace(len - 2, len, "};");
				System.Console.Out.WriteLine(javaSB[i_1]);
			}
			System.Console.Out.Write("  public static String[] sisterSplit = ");
			for (int i_2 = Cutoffs.Length; i_2 > 0; i_2--)
			{
				if (i_2 == 1)
				{
					System.Console.Out.Write("sisterSplit1");
				}
				else
				{
					System.Console.Out.Write("selectiveSisterSplit" + i_2 + " ? sisterSplit" + i_2 + " : (");
				}
			}
			// need to print extra one to close other things open
			for (int i_3 = Cutoffs.Length; i_3 >= 0; i_3--)
			{
				System.Console.Out.Write(")");
			}
			System.Console.Out.WriteLine(";");
		}

		/// <summary>
		/// Calculate sister annotation statistics suitable for doing
		/// selective sister splitting in the PCFGParser inside the
		/// FactoredParser.
		/// </summary>
		/// <param name="args">One argument: path to the Treebank</param>
		public static void Main(string[] args)
		{
			ClassicCounter<string> c = new ClassicCounter<string>();
			c.SetCount("A", 0);
			c.SetCount("B", 1);
			double d = Counters.KlDivergence(c, c);
			System.Console.Out.WriteLine("KL Divergence: " + d);
			string encoding = "UTF-8";
			if (args.Length > 1)
			{
				encoding = args[1];
			}
			if (args.Length < 1)
			{
				System.Console.Out.WriteLine("Usage: ParentAnnotationStats treebankPath");
			}
			else
			{
				SisterAnnotationStats pas = new SisterAnnotationStats();
				Treebank treebank = new DiskTreebank(null, encoding);
				treebank.LoadPath(args[0]);
				treebank.Apply(pas);
				pas.PrintStats();
			}
		}
	}
}

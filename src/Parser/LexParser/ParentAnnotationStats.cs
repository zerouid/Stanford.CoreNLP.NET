using System.Collections;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// See what parent annotation helps in treebank, based on support and
	/// KL divergence.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2003/01/04</version>
	public class ParentAnnotationStats : ITreeVisitor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats));

		private readonly ITreebankLanguagePack tlp;

		private ParentAnnotationStats(ITreebankLanguagePack tlp, bool doTags)
		{
			this.tlp = tlp;
			this.doTags = doTags;
		}

		private readonly bool doTags;

		private IDictionary<string, ClassicCounter<IList<string>>> nodeRules = Generics.NewHashMap();

		private IDictionary<IList<string>, ClassicCounter<IList<string>>> pRules = Generics.NewHashMap();

		private IDictionary<IList<string>, ClassicCounter<IList<string>>> gPRules = Generics.NewHashMap();

		private IDictionary<string, ClassicCounter<IList<string>>> tagNodeRules = Generics.NewHashMap();

		private IDictionary<IList<string>, ClassicCounter<IList<string>>> tagPRules = Generics.NewHashMap();

		private IDictionary<IList<string>, ClassicCounter<IList<string>>> tagGPRules = Generics.NewHashMap();

		/// <summary>Minimum support * KL to be included in output and as feature</summary>
		public static readonly double[] Cutoffs = new double[] { 100.0, 200.0, 500.0, 1000.0 };

		/// <summary>
		/// Minimum support of parent annotated node for grandparent to be
		/// studied.
		/// </summary>
		/// <remarks>
		/// Minimum support of parent annotated node for grandparent to be
		/// studied.  Just there to reduce runtime and printout size.
		/// </remarks>
		public const double Suppcutoff = 100.0;

		// corresponding ones for tags
		/// <summary>Does whatever one needs to do to a particular parse tree</summary>
		public virtual void VisitTree(Tree t)
		{
			ProcessTreeHelper("TOP", "TOP", t);
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

		public virtual void ProcessTreeHelper(string gP, string p, Tree t)
		{
			if (!t.IsLeaf() && (doTags || !t.IsPreTerminal()))
			{
				// stop at words/tags
				IDictionary<string, ClassicCounter<IList<string>>> nr;
				IDictionary<IList<string>, ClassicCounter<IList<string>>> pr;
				IDictionary<IList<string>, ClassicCounter<IList<string>>> gpr;
				if (t.IsPreTerminal())
				{
					nr = tagNodeRules;
					pr = tagPRules;
					gpr = tagGPRules;
				}
				else
				{
					nr = nodeRules;
					pr = pRules;
					gpr = gPRules;
				}
				string n = t.Label().Value();
				if (tlp != null)
				{
					p = tlp.BasicCategory(p);
					gP = tlp.BasicCategory(gP);
				}
				IList<string> kidn = KidLabels(t);
				ClassicCounter<IList<string>> cntr = nr[n];
				if (cntr == null)
				{
					cntr = new ClassicCounter<IList<string>>();
					nr[n] = cntr;
				}
				cntr.IncrementCount(kidn);
				IList<string> pairStr = new List<string>(2);
				pairStr.Add(n);
				pairStr.Add(p);
				cntr = pr[pairStr];
				if (cntr == null)
				{
					cntr = new ClassicCounter<IList<string>>();
					pr[pairStr] = cntr;
				}
				cntr.IncrementCount(kidn);
				IList<string> tripleStr = new List<string>(3);
				tripleStr.Add(n);
				tripleStr.Add(p);
				tripleStr.Add(gP);
				cntr = gpr[tripleStr];
				if (cntr == null)
				{
					cntr = new ClassicCounter<IList<string>>();
					gpr[tripleStr] = cntr;
				}
				cntr.IncrementCount(kidn);
				Tree[] kids = t.Children();
				foreach (Tree kid in kids)
				{
					ProcessTreeHelper(p, n, kid);
				}
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
				javaSB[i] = new StringBuilder("  private static String[] splitters" + (i + 1) + " = new String[] {");
			}
			ClassicCounter<IList<string>> allScores = new ClassicCounter<IList<string>>();
			// do value of parent
			foreach (string node in nodeRules.Keys)
			{
				List<Pair<IList<string>, double>> answers = Generics.NewArrayList();
				ClassicCounter<IList<string>> cntr = nodeRules[node];
				double support = (cntr.TotalCount());
				System.Console.Out.WriteLine("Node " + node + " support is " + support);
				foreach (IList<string> key in pRules.Keys)
				{
					if (key[0].Equals(node))
					{
						// only do it if they match
						ClassicCounter<IList<string>> cntr2 = pRules[key];
						double support2 = (cntr2.TotalCount());
						double kl = Counters.KlDivergence(cntr2, cntr);
						System.Console.Out.WriteLine("KL(" + key + "||" + node + ") = " + nf.Format(kl) + "\t" + "support(" + key + ") = " + support2);
						double score = kl * support2;
						answers.Add(new Pair<IList<string>, double>(key, score));
						allScores.SetCount(key, score);
					}
				}
				System.Console.Out.WriteLine("----");
				System.Console.Out.WriteLine("Sorted descending support * KL");
				answers.Sort(null);
				foreach (Pair<IList<string>, double> answer in answers)
				{
					Pair p = (Pair)answer;
					double psd = ((double)p.Second());
					System.Console.Out.WriteLine(p.First() + ": " + nf.Format(psd));
					if (psd >= Cutoffs[0])
					{
						IList lst = (IList)p.First();
						string nd = (string)lst[0];
						string par = (string)lst[1];
						for (int j = 0; j < Cutoffs.Length; j++)
						{
							if (psd >= Cutoffs[j])
							{
								javaSB[j].Append("\"").Append(nd).Append("^");
								javaSB[j].Append(par).Append("\", ");
							}
						}
					}
				}
				System.Console.Out.WriteLine();
			}
			/*
			// do value of parent with info gain -- yet to finish this
			for (Iterator it = nodeRules.entrySet().iterator(); it.hasNext(); ) {
			Map.Entry pair = (Map.Entry) it.next();
			String node = (String) pair.getKey();
			Counter cntr = (Counter) pair.getValue();
			double support = (cntr.totalCount());
			System.out.println("Node " + node + " support is " + support);
			ArrayList dtrs = new ArrayList();
			for (Iterator it2 = pRules.entrySet().iterator(); it2.hasNext();) {
			HashMap annotated = new HashMap();
			Map.Entry pair2 = (Map.Entry) it2.next();
			List node2 = (List) pair2.getKey();
			Counter cntr2 = (Counter) pair2.getValue();
			if (node2.get(0).equals(node)) {   // only do it if they match
			annotated.put(node2, cntr2);
			}
			}
			
			// upto
			
			List answers = new ArrayList();
			System.out.println("----");
			System.out.println("Sorted descending support * KL");
			Collections.sort(answers,
			new Comparator() {
			public int compare(Object o1, Object o2) {
			Pair p1 = (Pair) o1;
			Pair p2 = (Pair) o2;
			Double p12 = (Double) p1.second();
			Double p22 = (Double) p2.second();
			return p22.compareTo(p12);
			}
			});
			for (int i = 0, size = answers.size(); i < size; i++) {
			Pair p = (Pair) answers.get(i);
			double psd = ((Double) p.second()).doubleValue();
			System.out.println(p.first() + ": " + nf.format(psd));
			if (psd >= CUTOFFS[0]) {
			List lst = (List) p.first();
			String nd = (String) lst.get(0);
			String par = (String) lst.get(1);
			for (int j=0; j < CUTOFFS.length; j++) {
			if (psd >= CUTOFFS[j]) {
			javaSB[j].append("\"").append(nd).append("^");
			javaSB[j].append(par).append("\", ");
			}
			}
			}
			}
			System.out.println();
			}
			*/
			// do value of grandparent
			foreach (IList<string> node_1 in pRules.Keys)
			{
				List<Pair<IList<string>, double>> answers = Generics.NewArrayList();
				ClassicCounter<IList<string>> cntr = pRules[node_1];
				double support = (cntr.TotalCount());
				if (support < Suppcutoff)
				{
					continue;
				}
				System.Console.Out.WriteLine("Node " + node_1 + " support is " + support);
				foreach (IList<string> key in gPRules.Keys)
				{
					if (key[0].Equals(node_1[0]) && key[1].Equals(node_1[1]))
					{
						// only do it if they match
						ClassicCounter<IList<string>> cntr2 = gPRules[key];
						double support2 = (cntr2.TotalCount());
						double kl = Counters.KlDivergence(cntr2, cntr);
						System.Console.Out.WriteLine("KL(" + key + "||" + node_1 + ") = " + nf.Format(kl) + "\t" + "support(" + key + ") = " + support2);
						double score = kl * support2;
						answers.Add(Pair.MakePair(key, score));
						allScores.SetCount(key, score);
					}
				}
				System.Console.Out.WriteLine("----");
				System.Console.Out.WriteLine("Sorted descending support * KL");
				answers.Sort(null);
				foreach (Pair<IList<string>, double> answer in answers)
				{
					Pair p = (Pair)answer;
					double psd = ((double)p.Second());
					System.Console.Out.WriteLine(p.First() + ": " + nf.Format(psd));
					if (psd >= Cutoffs[0])
					{
						IList lst = (IList)p.First();
						string nd = (string)lst[0];
						string par = (string)lst[1];
						string gpar = (string)lst[2];
						for (int j = 0; j < Cutoffs.Length; j++)
						{
							if (psd >= Cutoffs[j])
							{
								javaSB[j].Append("\"").Append(nd).Append("^");
								javaSB[j].Append(par).Append("~");
								javaSB[j].Append(gpar).Append("\", ");
							}
						}
					}
				}
				System.Console.Out.WriteLine();
			}
			System.Console.Out.WriteLine();
			System.Console.Out.WriteLine("All scores:");
			IPriorityQueue<IList<string>> pq = Counters.ToPriorityQueue(allScores);
			while (!pq.IsEmpty())
			{
				IList<string> key = pq.GetFirst();
				double score = pq.GetPriority(key);
				pq.RemoveFirst();
				System.Console.Out.WriteLine(key + "\t" + score);
			}
			System.Console.Out.WriteLine("  // Automatically generated by ParentAnnotationStats -- preferably don't edit");
			for (int i_1 = 0; i_1 < Cutoffs.Length; i_1++)
			{
				int len = javaSB[i_1].Length;
				javaSB[i_1].Replace(len - 2, len, "};");
				System.Console.Out.WriteLine(javaSB[i_1]);
			}
			System.Console.Out.Write("  public static HashSet splitters = new HashSet(Arrays.asList(");
			for (int i_2 = Cutoffs.Length; i_2 > 0; i_2--)
			{
				if (i_2 == 1)
				{
					System.Console.Out.Write("splitters1");
				}
				else
				{
					System.Console.Out.Write("selectiveSplit" + i_2 + " ? splitters" + i_2 + " : (");
				}
			}
			// need to print extra one to close other things open
			for (int i_3 = Cutoffs.Length; i_3 >= 0; i_3--)
			{
				System.Console.Out.Write(")");
			}
			System.Console.Out.WriteLine(";");
		}

		private static void GetSplitters(double cutOff, IDictionary<string, ClassicCounter<IList<string>>> nr, IDictionary<IList<string>, ClassicCounter<IList<string>>> pr, IDictionary<IList<string>, ClassicCounter<IList<string>>> gpr, ICollection<string
			> splitters)
		{
			// do value of parent
			foreach (string node in nr.Keys)
			{
				IList<Pair<IList<string>, double>> answers = new List<Pair<IList<string>, double>>();
				ClassicCounter<IList<string>> cntr = nr[node];
				double support = (cntr.TotalCount());
				foreach (IList<string> key in pr.Keys)
				{
					if (key[0].Equals(node))
					{
						// only do it if they match
						ClassicCounter<IList<string>> cntr2 = pr[key];
						double support2 = cntr2.TotalCount();
						double kl = Counters.KlDivergence(cntr2, cntr);
						answers.Add(new Pair<IList<string>, double>(key, kl * support2));
					}
				}
				answers.Sort(null);
				foreach (Pair<IList<string>, double> p in answers)
				{
					double psd = p.Second();
					if (psd >= cutOff)
					{
						IList<string> lst = p.First();
						string nd = lst[0];
						string par = lst[1];
						string name = nd + "^" + par;
						splitters.Add(name);
					}
				}
			}
			/*
			// do value of parent with info gain -- yet to finish this
			for (Iterator it = nr.entrySet().iterator(); it.hasNext(); ) {
			Map.Entry pair = (Map.Entry) it.next();
			String node = (String) pair.getKey();
			Counter cntr = (Counter) pair.getValue();
			double support = (cntr.totalCount());
			ArrayList dtrs = new ArrayList();
			for (Iterator it2 = pr.entrySet().iterator(); it2.hasNext();) {
			HashMap annotated = new HashMap();
			Map.Entry pair2 = (Map.Entry) it2.next();
			List node2 = (List) pair2.getKey();
			Counter cntr2 = (Counter) pair2.getValue();
			if (node2.get(0).equals(node)) {   // only do it if they match
			annotated.put(node2, cntr2);
			}
			}
			
			// upto
			
			List answers = new ArrayList();
			Collections.sort(answers,
			new Comparator() {
			public int compare(Object o1, Object o2) {
			Pair p1 = (Pair) o1;
			Pair p2 = (Pair) o2;
			Double p12 = (Double) p1.second();
			Double p22 = (Double) p2.second();
			return p22.compareTo(p12);
			}
			});
			for (int i = 0, size = answers.size(); i < size; i++) {
			Pair p = (Pair) answers.get(i);
			double psd = ((Double) p.second()).doubleValue();
			if (psd >= cutOff) {
			List lst = (List) p.first();
			String nd = (String) lst.get(0);
			String par = (String) lst.get(1);
			String name = nd + "^" + par;
			splitters.add(name);
			}
			}
			}
			*/
			// do value of grandparent
			foreach (IList<string> node_1 in pr.Keys)
			{
				List<Pair<IList<string>, double>> answers = Generics.NewArrayList();
				ClassicCounter<IList<string>> cntr = pr[node_1];
				double support = (cntr.TotalCount());
				if (support < Suppcutoff)
				{
					continue;
				}
				foreach (IList<string> key in gpr.Keys)
				{
					if (key[0].Equals(node_1[0]) && key[1].Equals(node_1[1]))
					{
						// only do it if they match
						ClassicCounter<IList<string>> cntr2 = gpr[key];
						double support2 = (cntr2.TotalCount());
						double kl = Counters.KlDivergence(cntr2, cntr);
						answers.Add(new Pair<IList<string>, double>(key, kl * support2));
					}
				}
				answers.Sort(null);
				foreach (Pair<IList<string>, double> answer in answers)
				{
					Pair p = (Pair)answer;
					double psd = ((double)p.Second());
					if (psd >= cutOff)
					{
						IList lst = (IList)p.First();
						string nd = (string)lst[0];
						string par = (string)lst[1];
						string gpar = (string)lst[2];
						string name = nd + "^" + par + "~" + gpar;
						splitters.Add(name);
					}
				}
			}
		}

		/// <summary>
		/// Calculate parent annotation statistics suitable for doing
		/// selective parent splitting in the PCFGParser inside
		/// FactoredParser.
		/// </summary>
		/// <remarks>
		/// Calculate parent annotation statistics suitable for doing
		/// selective parent splitting in the PCFGParser inside
		/// FactoredParser.  <p>
		/// Usage: java edu.stanford.nlp.parser.lexparser.ParentAnnotationStats
		/// [-tags] treebankPath
		/// </remarks>
		/// <param name="args">One argument: path to the Treebank</param>
		public static void Main(string[] args)
		{
			bool doTags = false;
			if (args.Length < 1)
			{
				System.Console.Out.WriteLine("Usage: java edu.stanford.nlp.parser.lexparser.ParentAnnotationStats [-tags] treebankPath");
			}
			else
			{
				int i = 0;
				bool useCutOff = false;
				double cutOff = 0.0;
				while (args[i].StartsWith("-"))
				{
					if (args[i].Equals("-tags"))
					{
						doTags = true;
						i++;
					}
					else
					{
						if (args[i].Equals("-cutOff") && i + 1 < args.Length)
						{
							useCutOff = true;
							cutOff = double.ParseDouble(args[i + 1]);
							i += 2;
						}
						else
						{
							log.Info("Unknown option: " + args[i]);
							i++;
						}
					}
				}
				Treebank treebank = new DiskTreebank(null);
				treebank.LoadPath(args[i]);
				if (useCutOff)
				{
					ICollection<string> splitters = GetSplitCategories(treebank, doTags, 0, cutOff, cutOff, null);
					System.Console.Out.WriteLine(splitters);
				}
				else
				{
					Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats pas = new Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats(null, doTags);
					treebank.Apply(pas);
					pas.PrintStats();
				}
			}
		}

		/// <summary>Call this method to get a String array of categories to split on.</summary>
		/// <remarks>
		/// Call this method to get a String array of categories to split on.
		/// It calculates parent annotation statistics suitable for doing
		/// selective parent splitting in the PCFGParser inside
		/// FactoredParser.  <p>
		/// If tlp is non-null tlp.basicCategory() will be called on parent and
		/// grandparent nodes. <p>
		/// This version just defaults some parameters.
		/// <i>Implementation note:</i> This method is not designed for concurrent
		/// invocation: it uses static state variables.
		/// </remarks>
		public static ICollection<string> GetSplitCategories(Treebank t, double cutOff, ITreebankLanguagePack tlp)
		{
			return GetSplitCategories(t, true, 0, cutOff, cutOff, tlp);
		}

		/// <summary>Call this method to get a String array of categories to split on.</summary>
		/// <remarks>
		/// Call this method to get a String array of categories to split on.
		/// It calculates parent annotation statistics suitable for doing
		/// selective parent splitting in the PCFGParser inside
		/// FactoredParser.  <p>
		/// If tlp is non-null tlp.basicCategory() will be called on parent and
		/// grandparent nodes. <p>
		/// <i>Implementation note:</i> This method is not designed for concurrent
		/// invocation: it uses static state variables.
		/// </remarks>
		public static ICollection<string> GetSplitCategories(Treebank t, bool doTags, int algorithm, double phrasalCutOff, double tagCutOff, ITreebankLanguagePack tlp)
		{
			Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats pas = new Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats(tlp, doTags);
			t.Apply(pas);
			ICollection<string> splitters = Generics.NewHashSet();
			Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats.GetSplitters(phrasalCutOff, pas.nodeRules, pas.pRules, pas.gPRules, splitters);
			Edu.Stanford.Nlp.Parser.Lexparser.ParentAnnotationStats.GetSplitters(tagCutOff, pas.tagNodeRules, pas.tagPRules, pas.tagGPRules, splitters);
			return splitters;
		}

		/// <summary>
		/// This is hardwired to calculate the split categories from English
		/// Penn Treebank sections 2-21 with a default cutoff of 300 (as used
		/// in ACL03PCFG).
		/// </summary>
		/// <remarks>
		/// This is hardwired to calculate the split categories from English
		/// Penn Treebank sections 2-21 with a default cutoff of 300 (as used
		/// in ACL03PCFG).  It was added to upgrading of code in cases where no
		/// Treebank was available, and the pre-stored list was being used).
		/// </remarks>
		public static ICollection<string> GetEnglishSplitCategories(string treebankRoot)
		{
			ITreebankLangParserParams tlpParams = new EnglishTreebankParserParams();
			Treebank trees = tlpParams.MemoryTreebank();
			trees.LoadPath(treebankRoot, new NumberRangeFileFilter(200, 2199, true));
			return GetSplitCategories(trees, 300.0, tlpParams.TreebankLanguagePack());
		}
	}
}

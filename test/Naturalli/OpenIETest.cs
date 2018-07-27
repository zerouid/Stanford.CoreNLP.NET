using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;

using NUnit.Framework;


namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A test of the hard-coded clause splitting rules.</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class OpenIETest
	{
		protected internal virtual CoreLabel MkWord(string gloss, int index)
		{
			CoreLabel w = new CoreLabel();
			w.SetWord(gloss);
			w.SetValue(gloss);
			if (index >= 0)
			{
				w.SetIndex(index);
			}
			return w;
		}

		protected internal virtual ICollection<string> Clauses(string conll)
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			SemanticGraph tree = new SemanticGraph();
			foreach (string line in conll.Split("\n"))
			{
				if (line.Trim().Equals(string.Empty))
				{
					continue;
				}
				string[] fields = line.Trim().Split("\\s+");
				int index = System.Convert.ToInt32(fields[0]);
				string word = fields[1];
				CoreLabel label = MkWord(word, index);
				sentence.Add(label);
				if (fields[2].Equals("0"))
				{
					tree.AddRoot(new IndexedWord(label));
				}
				else
				{
					tree.AddVertex(new IndexedWord(label));
				}
				if (fields.Length > 4)
				{
					label.SetTag(fields[4]);
				}
				if (fields.Length > 5)
				{
					label.SetNER(fields[5]);
				}
				if (fields.Length > 6)
				{
					label.SetLemma(fields[6]);
				}
			}
			int i = 0;
			foreach (string line_1 in conll.Split("\n"))
			{
				if (line_1.Trim().Equals(string.Empty))
				{
					continue;
				}
				string[] fields = line_1.Trim().Split("\\s+");
				int parent = System.Convert.ToInt32(fields[2]);
				string reln = fields[3];
				if (parent > 0)
				{
					tree.AddEdge(new IndexedWord(sentence[parent - 1]), new IndexedWord(sentence[i]), new GrammaticalRelation(Language.English, reln, null, null), 1.0, false);
				}
				i += 1;
			}
			// Run extractor
			ClauseSplitterSearchProblem problem = new ClauseSplitterSearchProblem(tree, true);
			ICollection<string> clauses = new HashSet<string>();
			problem.Search(null, new LinearClassifier<ClauseSplitter.ClauseClassifierLabel, string>(new ClassicCounter<Pair<string, ClauseSplitter.ClauseClassifierLabel>>()), ClauseSplitterSearchProblem.HardSplits, null, 100000);
			return clauses;
		}

		[Test]
		public virtual void TestNoClauses()
		{
			NUnit.Framework.Assert.AreEqual(new _HashSet_95(), Clauses("1\tcats\t2\tnsubj\tNN\n" + "2\thave\t0\troot\tVB\n" + "3\ttails\t2\tdobj\tNN\n"));
		}

		private sealed class _HashSet_95 : HashSet<string>
		{
			public _HashSet_95()
			{
				{
					this.Add("cats have tails");
				}
			}
		}

		[Test]
		public virtual void TestXCompObj()
		{
			NUnit.Framework.Assert.AreEqual(new _HashSet_106(), Clauses("1\tI\t2\tnsubj\tPR\n" + "2\tpersuaded\t0\troot\tVBD\n" + "3\tFred\t2\tdobj\tNNP\n" + "4\tto\t5\taux\tTO\n" + "5\tleave\t2\txcomp\tVB\n" + "6\tthe\t7\tdet\tDT\n" + "7\troom\t5\tdobj\tNN\n"
				));
		}

		private sealed class _HashSet_106 : HashSet<string>
		{
			public _HashSet_106()
			{
				{
					this.Add("I persuaded Fred to leave the room");
					this.Add("Fred leave the room");
				}
			}
		}

		[Test]
		public virtual void TestXCompSubj()
		{
			NUnit.Framework.Assert.AreEqual(new _HashSet_122(), Clauses("1\tI\t3\tnsubjpass\tPR\n" + "2\twas\t3\tauxpass\tVB\n" + "3\tpersuaded\t0\troot\tVBD\n" + "4\tto\t5\taux\tTO\n" + "5\tleave\t3\txcomp\tVB\n" + "6\tthe\t7\tdet\tDT\n" + "7\troom\t5\tdobj\tNN\n"
				));
		}

		private sealed class _HashSet_122 : HashSet<string>
		{
			public _HashSet_122()
			{
				{
					this.Add("I was persuaded to leave the room");
					this.Add("I leave the room");
				}
			}
		}

		[Test]
		public virtual void TestCComp()
		{
			NUnit.Framework.Assert.AreEqual(new _HashSet_138(), Clauses("1\tI\t2\tnsubj\tPR\n" + "2\tsuggested\t0\troot\tVBD\n" + "3\tthat\t5\tmark\tIN\n" + "4\the\t5\tnsubj\tPR\n" + "5\tleave\t2\tccomp\tVB\n" + "6\tthe\t7\tdet\tDT\n" + "7\troom\t5\tdobj\tNN\n"
				));
		}

		private sealed class _HashSet_138 : HashSet<string>
		{
			public _HashSet_138()
			{
				{
					this.Add("I suggested that he leave the room");
					this.Add("he leave the room");
				}
			}
		}
	}
}

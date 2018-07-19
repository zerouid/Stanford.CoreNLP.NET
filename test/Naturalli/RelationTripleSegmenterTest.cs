using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// A test of various functions in
	/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
	/// .
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class RelationTripleSegmenterTest
	{
		protected internal virtual Optional<RelationTriple> MkExtraction(string conll)
		{
			return MkExtraction(conll, 0, false);
		}

		protected internal virtual Optional<RelationTriple> MkExtraction(string conll, bool allNominals)
		{
			return MkExtraction(conll, 0, allNominals);
		}

		protected internal virtual Optional<RelationTriple> MkExtraction(string conll, int listIndex)
		{
			return MkExtraction(conll, listIndex, false);
		}

		/// <summary>Parse a CoNLL formatted tree into a SemanticGraph object (along with a list of tokens).</summary>
		/// <param name="conll">The CoNLL formatted tree.</param>
		/// <returns>
		/// A pair of a SemanticGraph and a token list, corresponding to the parse of the sentence
		/// and to tokens in the sentence.
		/// </returns>
		protected internal virtual Pair<SemanticGraph, IList<CoreLabel>> MkTree(string conll)
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
				CoreLabel label = IETestUtils.MkWord(word, index);
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
					tree.AddEdge(new IndexedWord(sentence[parent - 1]), new IndexedWord(sentence[i]), new GrammaticalRelation(Language.UniversalEnglish, reln, null, null), 1.0, false);
				}
				i += 1;
			}
			return Pair.MakePair(tree, sentence);
		}

		/// <summary>
		/// Create a relation from a CoNLL format like:
		/// <pre>
		/// word_index  word  parent_index  incoming_relation
		/// </pre>
		/// </summary>
		protected internal virtual Optional<RelationTriple> MkExtraction(string conll, int listIndex, bool allNominals)
		{
			Pair<SemanticGraph, IList<CoreLabel>> info = MkTree(conll);
			SemanticGraph tree = info.first;
			IList<CoreLabel> sentence = info.second;
			// Run extractor
			Optional<RelationTriple> segmented = new RelationTripleSegmenter(allNominals).Segment(tree, Optional.Empty());
			if (segmented.IsPresent() && listIndex == 0)
			{
				return segmented;
			}
			IList<RelationTriple> extracted = new RelationTripleSegmenter(allNominals).Extract(tree, sentence);
			if (extracted.Count > listIndex)
			{
				return Optional.Of(extracted[listIndex - (segmented.IsPresent() ? 1 : 0)]);
			}
			return Optional.Empty();
		}

		protected internal virtual RelationTriple BlueCatsPlayWithYarnNoIndices()
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			sentence.Add(IETestUtils.MkWord("blue", -1));
			sentence.Add(IETestUtils.MkWord("cats", -1));
			sentence.Add(IETestUtils.MkWord("play", -1));
			sentence.Add(IETestUtils.MkWord("with", -1));
			sentence.Add(IETestUtils.MkWord("yarn", -1));
			return new RelationTriple(sentence.SubList(0, 2), sentence.SubList(2, 4), sentence.SubList(4, 5));
		}

		protected internal virtual RelationTriple BlueCatsPlayWithYarn()
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			sentence.Add(IETestUtils.MkWord("blue", 0));
			sentence.Add(IETestUtils.MkWord("cats", 1));
			sentence.Add(IETestUtils.MkWord("play", 2));
			sentence.Add(IETestUtils.MkWord("with", 3));
			sentence.Add(IETestUtils.MkWord("yarn", 4));
			return new RelationTriple(sentence.SubList(0, 2), sentence.SubList(2, 4), sentence.SubList(4, 5));
		}

		protected internal virtual RelationTriple YarnBlueCatsPlayWith()
		{
			IList<CoreLabel> sentence = new List<CoreLabel>();
			sentence.Add(IETestUtils.MkWord("yarn", 0));
			sentence.Add(IETestUtils.MkWord("blue", 1));
			sentence.Add(IETestUtils.MkWord("cats", 2));
			sentence.Add(IETestUtils.MkWord("play", 3));
			sentence.Add(IETestUtils.MkWord("with", 4));
			return new RelationTriple(sentence.SubList(1, 3), sentence.SubList(3, 5), sentence.SubList(0, 1));
		}

		[NUnit.Framework.Test]
		public virtual void TestToSentenceNoIndices()
		{
			NUnit.Framework.Assert.AreEqual(new _List_146(), BlueCatsPlayWithYarnNoIndices().AsSentence());
		}

		private sealed class _List_146 : List<CoreLabel>
		{
			public _List_146()
			{
				{
					this.Add(IETestUtils.MkWord("blue", -1));
					this.Add(IETestUtils.MkWord("cats", -1));
					this.Add(IETestUtils.MkWord("play", -1));
					this.Add(IETestUtils.MkWord("with", -1));
					this.Add(IETestUtils.MkWord("yarn", -1));
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestToSentenceInOrder()
		{
			NUnit.Framework.Assert.AreEqual(new _List_156(), BlueCatsPlayWithYarn().AsSentence());
		}

		private sealed class _List_156 : List<CoreLabel>
		{
			public _List_156()
			{
				{
					this.Add(IETestUtils.MkWord("blue", 0));
					this.Add(IETestUtils.MkWord("cats", 1));
					this.Add(IETestUtils.MkWord("play", 2));
					this.Add(IETestUtils.MkWord("with", 3));
					this.Add(IETestUtils.MkWord("yarn", 4));
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestToSentenceOutOfOrder()
		{
			NUnit.Framework.Assert.AreEqual(new _List_166(), YarnBlueCatsPlayWith().AsSentence());
		}

		private sealed class _List_166 : List<CoreLabel>
		{
			public _List_166()
			{
				{
					this.Add(IETestUtils.MkWord("yarn", 0));
					this.Add(IETestUtils.MkWord("blue", 1));
					this.Add(IETestUtils.MkWord("cats", 2));
					this.Add(IETestUtils.MkWord("play", 3));
					this.Add(IETestUtils.MkWord("with", 4));
				}
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestSameSemanticsForDifferentWordOrder()
		{
			NUnit.Framework.Assert.AreEqual(BlueCatsPlayWithYarn().ToString(), YarnBlueCatsPlayWith().ToString());
			NUnit.Framework.Assert.AreEqual("1.0\tblue cats\tplay with\tyarn", BlueCatsPlayWithYarn().ToString());
			NUnit.Framework.Assert.AreEqual("1.0\tblue cats\tplay with\tyarn", YarnBlueCatsPlayWith().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestGlosses()
		{
			NUnit.Framework.Assert.AreEqual("blue cats", BlueCatsPlayWithYarn().SubjectGloss());
			NUnit.Framework.Assert.AreEqual("play with", BlueCatsPlayWithYarn().RelationGloss());
			NUnit.Framework.Assert.AreEqual("yarn", BlueCatsPlayWithYarn().ObjectGloss());
		}

		[NUnit.Framework.Test]
		public virtual void TestBlueCatsPlayWithYarn()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tblue\t2\tamod\n" + "2\tcats\t3\tnsubj\n" + "3\tplay\t0\troot\n" + "4\twith\t5\tcase\n" + "5\tyarn\t3\tnmod:with\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tblue cats\tplay with\tyarn", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestBlueCatsPlayQuietlyWithYarn()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tblue\t2\tamod\n" + "2\tcats\t3\tnsubj\n" + "3\tplay\t0\troot\n" + "4\tquietly\t3\tadvmod\n" + "5\twith\t6\tcase\n" + "6\tyarn\t3\tnmod:with\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tblue cats\tplay quietly with\tyarn", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestCatsHaveTails()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tcats\t2\tnsubj\n" + "2\thave\t0\troot\n" + "3\ttails\t2\tdobj\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tcats\thave\ttails", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestrabbitsEatVegetables()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\trabbits\t2\tnsubj\n" + "2\teat\t0\troot\n" + "3\tvegetables\t2\tdobj\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\trabbits\teat\tvegetables", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestFishLikeToSwim()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tfish\t2\tnsubj\n" + "2\tlike\t0\troot\n" + "3\tto\t4\taux\n" + "4\tswim\t2\txcomp\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tfish\tlike\tto swim", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestFishLikeToSwimAlternateParse()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tfish\t2\tnsubj\n" + "2\tlike\t0\troot\n" + "3\tto\t4\tcase\n" + "4\tswim\t2\tnmod:to\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tfish\tlike to\tswim", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestMyCatsPlayWithYarn()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tmy\t2\tnmod:poss\n" + "2\tcats\t3\tnsubj\n" + "3\tplay\t0\troot\n" + "4\twith\t5\tcase\n" + "5\tyarn\t3\tnmod:with\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tmy cats\tplay with\tyarn", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestCatsAreCute()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tcats\t3\tnsubj\n" + "2\tare\t3\tcop\n" + "3\tcute\t0\troot\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tcats\tare\tcute", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestIAmInFlorida()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tI\t4\tnsubj\n" + "2\tam\t4\tcop\n" + "3\tin\t4\tcase\n" + "4\tFlorida\t0\troot\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tI\tam in\tFlorida", extraction.Get().ToString());
		}

		// not (I; am; Florida)
		[NUnit.Framework.Test]
		public virtual void TestWh()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\twhat\t3\tnsubj\tWP\n" + "2\tis\t3\tcop\tVBZ\n" + "3\tlove\t0\troot\tNN\n");
			NUnit.Framework.Assert.IsFalse("Extracted on WH word!", extraction.IsPresent());
		}

		[NUnit.Framework.Test]
		public virtual void TestPropagateCSubj()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\ttruffles\t2\tnsubj\n" + "2\tpicked\t4\tcsubj\n" + "3\tare\t4\tcop\n" + "4\ttasty\t0\troot\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\ttruffles picked\tare\ttasty", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestHeWasInaugurated()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\the\t3\tnsubjpass\n" + "2\twas\t3\tauxpass\n" + "3\tinaugurated\t0\troot\n" + "4\tas\t5\tcase\n" + "5\tpresident\t3\tnmod:as\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\the\twas inaugurated as\tpresident", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPPAttachment()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\the\t2\tnsubj\n" + "2\tserved\t0\troot\n" + "3\tas\t4\tcase\n" + "4\tpresident\t2\tnmod:as\n" + "5\tof\t8\tcase\n" + "6\tHarvard\t8\tcompound\n" + "7\tLaw\t8\tcompound\n" + "8\tReview\t4\tnmod:of\n"
				);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\the\tserved as\tpresident of Harvard Law Review", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPPAttachmentTwo()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\the\t4\tnsubj\n" + "2\twas\t4\tcop\n" + "3\tcommunity\t4\tcompound\n" + "4\torganizer\t0\troot\n" + "5\tin\t6\tcase\n" + "6\tChicago\t4\tnmod:in\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\the\twas\tcommunity organizer in Chicago", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestXComp()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tObama\t3\tnsubjpass\n" + "2\twas\t3\tauxpass\n" + "3\tnamed\t0\troot\n" + "4\t2009\t8\tnummod\n" + "5\tNobel\t8\tcompound\n" + "6\tPeace\t8\tcompound\n" + "7\tPrize\t8\tcompound\n" + "8\tLaureate\t3\txcomp\n"
				);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\twas named\t2009 Nobel Peace Prize Laureate", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPassiveNSubj()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tHRE\t3\tnsubjpass\n" + "2\twas\t3\tauxpass\n" + "3\tfounded\t0\troot\n" + "4\tin\t5\tcase\n" + "5\t1991\t3\tnmod:in\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tHRE\twas founded in\t1991", extraction.Get().ToString());
			extraction = MkExtraction("1\tfounded\t0\troot\n" + "2\tHRE\t1\tnsubjpass\n" + "3\tin\t4\tcase\n" + "4\t2003\t1\tnmod:in\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tHRE\tfounded in\t2003", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPossessive()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tUnicredit\t5\tnmod:poss\tNNP\tORGANIZATION\n" + "2\t's\t1\tcase\tPOS\tO\n" + "3\tBank\t5\tcompound\tNNP\tORGANIZATION\n" + "4\tAustria\t5\tcompound\tNNP\tORGANIZATION\n" + "5\tCreditanstalt\t0\troot\tNNP\tORGANIZATION\n"
				);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tUnicredit\thas\tBank Austria Creditanstalt", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPossessiveNoNER()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tIBM\t4\tnmod:poss\tNNP\tORGANIZATION\n" + "2\t's\t1\tcase\tPOS\tO\n" + "3\tresearch\t4\tcompound\tNN\tO\n" + "4\tgroup\t0\troot\tNN\tO\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tIBM\thas\tresearch group", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPossessiveWithObject()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tTim\t3\tnmod:poss\n" + "2\t's\t1\tcase\n" + "3\tfather\t0\troot\n" + "4\tTom\t3\tappos\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tTim\t's father is\tTom", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestApposInObject()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tNewspaper\t2\tnsubj\n" + "2\tpublished\t0\troot\n" + "3\tin\t4\tcase\n" + "4\tTucson\t2\tnmod:in\n" + "5\tArizona\t4\tappos\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tNewspaper\tpublished in\tArizona", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestApposAsSubj()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tDurin\t0\troot\tNNP\n" + "2\tson\t1\tappos\tNN\n" + "3\tof\t4\tcase\tIN\n" + "4\tThorin\t2\tnmod:of\tNNP\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tDurin\tson of\tThorin", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestReflexive()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tTom\t5\tnsubj\n" + "2\tand\t1\tcc\n" + "3\tJerry\t1\tconj:and\n" + "4\twere\t5\taux\n" + "5\tfighting\t0\troot\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tTom\tfighting\tJerry", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPassiveReflexive()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tTom\t5\tnsubjpass\n" + "2\tand\t1\tcc\n" + "3\tJerry\t1\tconj:and\n" + "4\twere\t5\tauxpass\n" + "5\tfighting\t0\troot\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tTom\tfighting\tJerry", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPossessiveInEntity()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tScania-Vabis\t2\tnsubj\n" + "2\testablished\t0\troot\n" + "3\tits\t6\tnmod:poss\n" + "4\tfirst\t6\tamod\n" + "5\tproduction\t6\tcompound\n" + "6\tplant\t2\tdobj\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tScania-Vabis\testablished\tits first production plant", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestOfWhich()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tof\t2\tcase\n" + "2\twhich\t5\tnmod:of\n" + "3\tBono\t5\tnsubj\n" + "4\tis\t5\tcop\n" + "5\tco-founder\t0\troot\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tBono\tis co-founder of\twhich", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestObjInRelation()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tScania-Vabis\t2\tnsubj\tNNP\tORGANIZATION\n" + "2\testablished\t0\troot\tVB\tO\n" + "3\tproduction\t4\tcompound\tNN\tO\n" + "4\tplant\t2\tdobj\tNN\tO\n" + "5\toutside\t6\tcase\tIN\tO\n" 
				+ "6\tSödertälje\t2\tnmod:outside\tNN\tO\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tScania-Vabis\testablished production plant outside\tSödertälje", extraction.Get().ToString());
			extraction = MkExtraction("1\tHun\t2\tcompound\tNNP\tPERSON\n" + "2\tSen\t3\tnsubj\tNNP\tPERSON\n" + "3\tplayed\t0\troot\tVBD\tO\n" + "4\tgolf\t3\tdobj\tNN\tO\n" + "5\twith\t6\tcase\tIN\tO\n" + "6\tShinawatra\t3\tnmod:with\tNNP\tPERSON\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tHun Sen\tplayed golf with\tShinawatra", extraction.Get().ToString());
			extraction = MkExtraction("1\tHun\t2\tcompound\tNNP\tPERSON\n" + "2\tSen\t3\tnsubj\tNNP\tPERSON\n" + "3\tplayed\t0\troot\tVBD\tO\n" + "4\tgolf\t3\tdobj\tNN\tO\n" + "5\tShinawatra\t3\tnmod:with\tNNP\tPERSON\n" + "6\tCambodia\t3\tdobj\tNNP\tLOCATION\n"
				);
			NUnit.Framework.Assert.IsFalse("Should not have found extraction for sentence! Incorrectly found: " + extraction.OrElse(null), extraction.IsPresent());
		}

		[NUnit.Framework.Test]
		public virtual void TestVBG()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tfoal\t3\tnsubj\n" + "2\tbe\t3\taux\n" + "3\tstanding\t0\troot\n" + "4\tnext\t3\tadvmod\t\n" + "5\tto\t6\tcase\n" + "6\thorse\t3\tnmod:to\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tfoal\tbe standing next to\thorse", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestVBGCollapsed()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tfoal\t3\tnsubj\n" + "2\tbe\t3\taux\n" + "3\tstanding\t0\troot\n" + "4\tnext\t6\tcase\t\n" + "5\tto\t4\tmwe\n" + "6\thorse\t3\tnmod:next_to\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tfoal\tbe standing next to\thorse", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestThereAreIn()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tthere\t2\texpl\n" + "2\tare\t0\troot\tVBP\tO\tbe\n" + "3\tdogs\t2\tnsubj\tNN\n" + "4\tin\t5\tcase\tNN\n" + "5\theaven\t3\tnmod:in\tNN\n", true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tdogs\tis in\theaven", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestThereAreWith()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tthere\t2\texpl\tEX\n" + "2\tare\t0\troot\tVBP\tO\tbe\n" + "3\tcats\t2\tnsubj\tNN\n" + "4\twith\t5\tcase\tIN\n" + "5\ttails\t3\tnmod:with\tNN\n", true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tcats\tis with\ttails", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestThereAreVBing()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tthere\t2\texpl\n" + "2\tare\t0\troot\tVBP\tO\tbe\n" + "3\tdogs\t2\tnsubj\n" + "4\tsitting\t3\tacl\n" + "5\tin\t6\tcase\tNN\n" + "6\theaven\t4\tnmod:in\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tdogs\tsitting in\theaven", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestDogsInheaven()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tdogs\t0\troot\tNN\n" + "2\tin\t3\tcase\tNN\n" + "3\theaven\t1\tnmod:in\tNN\n", true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tdogs\tis in\theaven", extraction.Get().ToString());
			extraction = MkExtraction("1\tdogs\t0\troot\tNN\n" + "2\tin\t3\tcase\tNN\n" + "3\theaven\t1\tnmod:of\tNN\n", true);
			NUnit.Framework.Assert.IsFalse(extraction.IsPresent());
		}

		[NUnit.Framework.Test]
		public virtual void TestAdvObject()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\thorses\t3\tnsubj\n" + "2\tare\t3\tcop\n" + "3\tgrazing\t0\troot\n" + "4\tpeacefully\t3\tadvmod\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\thorses\tare\tgrazing peacefully", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestAdvObjectPassive()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tthings\t3\tnsubjpass\n" + "2\tare\t3\tauxpass\n" + "3\tarranged\t0\troot\n" + "4\tneatly\t3\tadvmod\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tthings\tare\tarranged neatly", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestObamaBornInRegression()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tObama\t2\tnsubj\n" + "2\tBorn\t0\troot\n" + "3\tin\t4\tcase\n" + "4\tHonolulu\t2\tnmod:in\n" + "5\tHawaii\t4\tcompound\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\tBorn in\tHonolulu Hawaii", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestObamaPresidentOfRegression()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tObama\t3\tnsubj\n" + "2\tis\t3\tcop\n" + "3\tpresident\t0\troot\n" + "4\tof\t5\tcase\n" + "5\tUS\t3\tnmod:of\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\tis president of\tUS", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestObamaPresidentOfRegressionFull()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tObama\t6\tnsubj\n" + "2\tis\t6\tcop\n" + "3\t44th\t6\tamod\n" + "4\tand\t3\tcc\n" + "5\tcurrent\t3\tconj:and\n" + "6\tpresident\t0\troot\n" + "7\tof\t8\tcase\n" + "8\tUS\t6\tnmod:of\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\tis 44th and current president of\tUS", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestGeorgeBoydRegression()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tGeorge\t2\tcompound\n" + "2\tBoyd\t4\tnsubj\n" + "3\thas\t4\taux\n" + "4\tjoined\t0\troot\n" + "5\tNottingham\t6\tcompound\n" + "6\tForest\t4\tdobj\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tGeorge Boyd\thas joined\tNottingham Forest", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestUSPresidentObama1()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tUnited\t5\tcompound\tNNP\tORGANIZATION\n" + "2\tStates\t5\tcompound\tNNP\tORGANIZATION\n" + "3\tpresident\t5\tcompound\tNNP\tO\n" + "4\tBarack\t5\tcompound\tNNP\tPERSON\n" + "5\tObama\t0\troot\tNNP\tPERSON\n"
				);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tBarack Obama\tis president of\tUnited States", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestUSPresidentObama2()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tUnited\t5\tcompound\tNNP\tORGANIZATION\n" + "2\tStates\t5\tcompound\tNNP\tORGANIZATION\n" + "3\tpresident\t5\tcompound\tNNP\tTITLE\n" + "4\tBarack\t5\tcompound\tNNP\tPERSON\n" + "5\tObama\t0\troot\tNNP\tPERSON\n"
				);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tBarack Obama\tis president of\tUnited States", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestUSAllyBritain()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tUnited\t4\tcompound\tNNP\tLOCATION\n" + "2\tStates\t4\tcompound\tNNP\tLOCATION\n" + "3\tally\t4\tcompound\tNN\tO\n" + "4\tBritain\t0\troot\tNNP\tLOCATION\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tBritain\tis ally of\tUnited States", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestUSPresidentObama()
		{
			string conll = "1\tUnited\t2\tcompound\tNNP\tLOCATION\n" + "2\tStates\t4\tnmod:poss\tNNP\tLOCATION\n" + "3\t's\t2\tcase\tPOS\tO\n" + "4\tpresident\t0\troot\tNN\tO\n" + "5\tObama\t2\tappos\tNNP\tPERSON\n";
			Optional<RelationTriple> extraction = MkExtraction(conll, 0);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tUnited States\thas\tpresident", extraction.Get().ToString());
			extraction = MkExtraction(conll, 1);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\tis president of\tUnited States", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestUSsAllyBritain()
		{
			string conll = "1\tUnited\t2\tcompound\tNNP\tLOCATION\n" + "2\tStates\t4\tnmod:poss\tNNP\tLOCATION\n" + "3\t's\t2\tcase\tPOS\tO\n" + "4\tally\t0\troot\tNN\tO\n" + "5\tBritain\t2\tappos\tNNP\tPERSON\n";
			Optional<RelationTriple> extraction = MkExtraction(conll, 0);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tUnited States\thas\tally", extraction.Get().ToString());
			extraction = MkExtraction(conll, 1);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tBritain\tis ally of\tUnited States", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPresidentObama()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tPresident\t2\tcompound\tPOS\tTITLE\n" + "2\tObama\t0\troot\tNNP\tPERSON\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\tis\tPresident", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestAmericanActorChrisPratt()
		{
			string conll = "1\tAmerican\t4\tamod\tNN\tLOCATION\n" + "2\tactor\t4\tcompound\tNN\tTITLE\n" + "3\tChris\t4\tcompound\tNNP\tPERSON\n" + "4\tPratt\t0\troot\tNNP\tPERSON\n";
			Optional<RelationTriple> extraction = MkExtraction(conll, 0);
			NUnit.Framework.Assert.IsTrue("No first extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tChris Pratt\tis actor of\tAmerican", extraction.Get().ToString());
			extraction = MkExtraction(conll, 1);
			NUnit.Framework.Assert.IsTrue("No second extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tChris Pratt\tis\tAmerican", extraction.Get().ToString());
			extraction = MkExtraction(conll, 2);
			NUnit.Framework.Assert.IsTrue("No third extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tChris Pratt\tis\tactor", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestChrisManningOfStanford()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tChris\t2\tcompound\tNNP\tPERSON\n" + "2\tManning\t0\troot\tNNP\tPERSON\n" + "3\tof\t4\tcase\tIN\tO\n" + "4\tStanford\t2\tnmod:of\tNNP\tORGANIZATION\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tChris Manning\tis of\tStanford", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestChrisManningOfStanfordLong()
		{
			string conll = "1\tChris\t2\tcompound\tNNP\tPERSON\n" + "2\tManning\t5\tnsubj\tNNP\tPERSON\n" + "3\tof\t4\tcase\tIN\tO\n" + "4\tStanford\t2\tnmod:of\tNNP\tORGANIZATION\n" + "5\tvisited\t0\troot\tVBD\tO\n" + "6\tChina\t5\tdobj\tNNP\tLOCATION\n";
			Optional<RelationTriple> extraction = MkExtraction(conll);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tChris Manning\tis of\tStanford", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestChrisIsOfStanford()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tChris\t2\tcompound\tNNP\tPERSON\n" + "2\tManning\t0\troot\tNNP\tPERSON\n" + "3\tof\t4\tcase\tIN\tO\n" + "4\tStanford\t2\tnmod:of\tNNP\tORGANIZATION\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tChris Manning\tis of\tStanford", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestPPExtraction()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tObama\t0\troot\tNNP\tPERSON\n" + "2\tin\t3\tcase\tIN\tO\n" + "3\tTucson\t1\tnmod:in\tNNP\tLOCATION\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tObama\tis in\tTucson", extraction.Get().ToString());
			extraction = MkExtraction("1\tPietro\t2\tcompound\tNNP\tPERSON\n" + "2\tBadoglio\t0\troot\tNNP\tPERSON\n" + "3\tin\t5\tcase\tIN\tO\n" + "4\tsouthern\t5\tamod\tJJ\tO\n" + "5\tItaly\t2\tnmod:in\tNN\tLOCATION\n");
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tPietro Badoglio\tis in\tItaly", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestCommaDoesntOvergenerate()
		{
			Optional<RelationTriple> extraction = MkExtraction("1\tHonolulu\t3\tcompound\tNNP\tLOCATION\n" + "2\t,\t1\tpunct\t.\tO\n" + "3\tHawaii\t0\troot\tNNP\tLOCATION\n");
			NUnit.Framework.Assert.IsFalse("Found extraction when we shouldn't have! Extraction: " + (extraction.IsPresent() ? extraction.Get() : string.Empty), extraction.IsPresent());
			extraction = MkExtraction("1\tHonolulu\t3\tcompound\tNNP\tLOCATION\n" + "2\t,\t1\tpunct\t.\tO\n" + "3\tHawaii\t0\troot\tNNP\tLOCATION\n" + "4\t,\t3\tpunct\t.\tO\n");
			NUnit.Framework.Assert.IsFalse("Found extraction when we shouldn't have! Extraction: " + (extraction.IsPresent() ? extraction.Get() : string.Empty), extraction.IsPresent());
		}

		[NUnit.Framework.Test]
		public virtual void TestCompoundPossessive()
		{
			string conll = "1\tIBM\t4\tnmod:poss\tNNP\tORGANIZATION\n" + "2\t's\t1\tcase\tPOS\tO\n" + "3\tCEO\t4\tcompound\tNNP\tTITLE\n" + "4\tRometty\t0\troot\tNNP\tORGANIZATION\n";
			Optional<RelationTriple> extraction = MkExtraction(conll, 1);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tRometty\tis\tCEO", extraction.Get().ToString());
			extraction = MkExtraction(conll, 0);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tIBM\thas\tRometty", extraction.Get().ToString());
			extraction = MkExtraction(conll, 2);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tRometty\tis CEO of\tIBM", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestAllNominals()
		{
			string conll = "1\tfierce\t2\tamod\tJJ\n" + "2\tlions\t0\troot\tNN\n" + "3\tin\t4\tcase\tIN\n" + "4\tNarnia\t2\tnmod:in\tNNP\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, 0, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tfierce lions\tis in\tNarnia", extraction.Get().ToString());
			extraction = MkExtraction(conll, 1, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tlions\tis\tfierce", extraction.Get().ToString());
			// Negative case
			extraction = MkExtraction(conll, 0, false);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tfierce lions\tis in\tNarnia", extraction.Get().ToString());
			NUnit.Framework.Assert.IsFalse(MkExtraction(conll, 1, false).IsPresent());
		}

		[NUnit.Framework.Test]
		public virtual void TestAcl()
		{
			string conll = "1\tman\t0\troot\tNN\n" + "2\tsitting\t1\tacl\tVBG\n" + "3\tin\t4\tcase\tIN\n" + "4\ttree\t2\tnmod:in\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tman\tsitting in\ttree", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestAclWithAdverb()
		{
			string conll = "1\tman\t0\troot\tNN\n" + "2\tsitting\t1\tacl\tVBG\n" + "3\tvery\t2\tadvmod\tRB\n" + "4\tquietly\t2\tadvmod\tRB\n" + "5\tin\t6\tcase\tIN\n" + "6\ttree\t2\tnmod:in\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tman\tsitting very quietly in\ttree", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestAclNoPP()
		{
			string conll = "1\tman\t0\troot\tNN\n" + "2\triding\t1\tacl\tVBG\n" + "3\thorse\t2\tdobj\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tman\triding\thorse", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestAclWithPP()
		{
			string conll = "1\tweeds\t0\troot\tNN\n" + "2\tgrowing\t1\tacl\tVBG\n" + "3\taround\t4\tcase\tIN\n" + "4\tplant\t2\tnmod:around\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tweeds\tgrowing around\tplant", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestNmodTmod()
		{
			string conll = "1\tFriday\t3\tnmod:tmod\tNN\n" + "2\tI\t3\tnsubj\tPR\n" + "3\tmake\t0\troot\tVB\n" + "4\ttea\t3\tdobj\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tI\tmake tea at_time\tFriday", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestVPOnlyReplacedWith()
		{
			string conll = "1\treplaced\t0\tconj:and\tVBD\n" + "2\twith\t5\tcase\tIN\n" + "3\ta\t5\tdet\tDT\n" + "4\tdifferent\t5\tamod\tJJ\n" + "5\ttype\t1\tnmod:with\tNN\n" + "6\tof\t7\tcase\tIN\n" + "7\tfilter\t5\tnmod:of\tNN\n";
			// Positive case
			bool matches = false;
			SemanticGraph tree = MkTree(conll).first;
			foreach (SemgrexPattern candidate in new RelationTripleSegmenter().VpPatterns)
			{
				if (candidate.Matcher(tree).Matches())
				{
					matches = true;
				}
			}
			NUnit.Framework.Assert.IsTrue(matches);
		}

		[NUnit.Framework.Test]
		public virtual void TestThrowAway()
		{
			string conll = "1\tI\t2\tnsubj\tPRP\n" + "2\tthrow\t0\troot\tVB\n" + "3\taway\t2\tcompound:ptr\tRP\n" + "4\tmy\t5\tnmod:poss\tPRP$\n" + "5\tlaptop\t2\tdobj\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tI\tthrow away\tmy laptop", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestMassOfIron()
		{
			string conll = "1\tmass\t5\tnsubj\tNN\n" + "2\tof\t3\tcase\tIN\n" + "3\tiron\t1\tnmod:of\tNN\n" + "4\tis\t5\tcop\tVBZ\n" + "5\t55amu\t0\troot\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsTrue("No extraction for sentence!", extraction.IsPresent());
			NUnit.Framework.Assert.AreEqual("1.0\tiron\tmass of is\t55amu", extraction.Get().ToString());
		}

		[NUnit.Framework.Test]
		public virtual void TestStateOfTheUnion()
		{
			string conll = "1\tState\t5\tnsubj\tNNP\n" + "2\tof\t3\tcase\tIN\n" + "3\tUnion\t1\tnmod:of\tNNP\n" + "4\tis\t5\tcop\tVBZ\n" + "5\ttomorrow\t0\troot\tNN\n";
			// Positive case
			Optional<RelationTriple> extraction = MkExtraction(conll, true);
			NUnit.Framework.Assert.IsFalse("Extraction found when we shouldn't have: " + extraction, extraction.IsPresent());
		}
	}
}

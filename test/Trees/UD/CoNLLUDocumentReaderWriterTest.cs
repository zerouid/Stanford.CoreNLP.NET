using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <author>Sebastian Schuster</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CoNLLUDocumentReaderWriterTest
	{
		private static string MultiwordTestInput = "1     I         I      PRON    PRP   Case=Nom|Number=Sing|Person=1     2   nsubj   _   _\n" + "2-3   haven't   _      _       _     _                                 _   _   _   _\n" + "2     have      have   VERB    VBP    Number=Sing|Person=1|Tense=Pres   0   root   _   _\n"
			 + "3     not       not    PART    RB    Negative=Neg                      2   neg   _   _\n" + "4     a         a      DET     DT    Definite=Ind|PronType=Art         5   det   _   _\n" + "5     clue      clue   NOUN    NN    Number=Sing                       2   dobj   _   _\n"
			 + "6     .         .      PUNCT   .     _                                 2   punct   _   _\n\n";

		private static string CommentTestInput = "#comment line 1\n" + "#comment line 2\n" + "1     I         I      PRON    PRP   Case=Nom|Number=Sing|Person=1     2   nsubj   _   _\n" + "2     have      have   VERB    VBP    Number=Sing|Person=1|Tense=Pres   0   root   _   _\n"
			 + "3     not       not    PART    RB    Negative=Neg                      2   neg   _   _\n" + "4     a         a      DET     DT    Definite=Ind|PronType=Art         5   det   _   _\n" + "5     clue      clue   NOUN    NN    Number=Sing                       2   dobj   _   _\n"
			 + "6     .         .      PUNCT   .     _                                 2   punct   _   _\n\n";

		private static string ExtraDepsTestInput = "1     They       They       PRON    PRP    _    2   nsubj   2:nsubj|4:nsubj         _\n" + "2     buy        buy        VERB    VBP    _    0   root    0:root               _\n" + "3     and        and        CONJ    CC     _    2   cc      2:cc               _\n"
			 + "4     sell       sell       VERB    VBP    _    5   conj    5:conj               _\n" + "5     books      book       NOUN    NNS    _    2   dobj    2:dobj|4:dobj          _\n" + "6     ,          ,          PUNCT   ,      _    5   punct   5:punct               _\n"
			 + "7     newspapers newspaper  NOUN    NNS    _    5   conj    2:dobj|4:dobj|5:conj   _\n" + "8     and        and        CONJ    CC     _    5   cc      5:cc               _\n" + "9     magazines  magazine   NOUN    NNS    _    5   conj    2:dobj|4:dobj|5:conj   _\n"
			 + "10    .          .          PUNCT   .      _    2   punct   2:punct               _\n\n";

		private static string ExtraDepsTestEmptyNodeinput = "1     They       They       PRON    PRP    _    2   nsubj   2:nsubj|2.1:nsubj|2.2:nsubj         _\n" + "2     buy        buy        VERB    VBP    _    0   root    0:root               _\n"
			 + "2.1     buy        buy        VERB    VBP    _    _   _    2:conj:and               _\n" + "2.2     buy        buy        VERB    VBP    _    _   _    2:conj:and               _\n" + "3     books      book       NOUN    NNS    _    2   dobj    2:dobj          _\n"
			 + "4     ,          ,          PUNCT   ,      _    3   punct   3:punct               _\n" + "5     newspapers newspaper  NOUN    NNS    _    3   conj    2.1:dobj|3:conj   _\n" + "6     and        and        CONJ    CC     _    3   cc      3:cc               _\n"
			 + "7     magazines  magazine   NOUN    NNS    _    3   conj    2.2:dobj|3:conj   _\n" + "8    .          .          PUNCT   .      _    2   punct   2:punct               _\n\n";

		[NUnit.Framework.Test]
		public virtual void TestMultiWords()
		{
			CoNLLUDocumentReader reader = new CoNLLUDocumentReader();
			Reader stringReader = new StringReader(MultiwordTestInput);
			IEnumerator<SemanticGraph> it = reader.GetIterator(stringReader);
			SemanticGraph sg = it.Current;
			NUnit.Framework.Assert.IsNotNull(sg);
			NUnit.Framework.Assert.IsFalse("The input only contains one dependency tree.", it.MoveNext());
			NUnit.Framework.Assert.AreEqual("[have/VBP nsubj>I/PRP neg>not/RB dobj>[clue/NN det>a/DT] punct>./.]", sg.ToCompactString(true));
			foreach (IndexedWord iw in sg.VertexListSorted())
			{
				if (iw.Index() != 2 && iw.Index() != 3)
				{
					NUnit.Framework.Assert.AreEqual(string.Empty, iw.OriginalText());
				}
				else
				{
					NUnit.Framework.Assert.AreEqual("haven't", iw.OriginalText());
				}
			}
			NUnit.Framework.Assert.AreEqual(int.Parse(3), sg.GetNodeByIndex(2).Get(typeof(CoreAnnotations.LineNumberAnnotation)));
		}

		[NUnit.Framework.Test]
		public virtual void TestComment()
		{
			CoNLLUDocumentReader reader = new CoNLLUDocumentReader();
			Reader stringReader = new StringReader(CommentTestInput);
			IEnumerator<SemanticGraph> it = reader.GetIterator(stringReader);
			SemanticGraph sg = it.Current;
			NUnit.Framework.Assert.IsNotNull(sg);
			NUnit.Framework.Assert.IsFalse("The input only contains one dependency tree.", it.MoveNext());
			NUnit.Framework.Assert.AreEqual("[have/VBP nsubj>I/PRP neg>not/RB dobj>[clue/NN det>a/DT] punct>./.]", sg.ToCompactString(true));
			NUnit.Framework.Assert.AreEqual(int.Parse(3), sg.GetNodeByIndex(1).Get(typeof(CoreAnnotations.LineNumberAnnotation)));
			NUnit.Framework.Assert.AreEqual(2, sg.GetComments().Count);
			NUnit.Framework.Assert.AreEqual("#comment line 1", sg.GetComments()[0]);
		}

		/// <summary>Tests whether extra dependencies are correctly parsed.</summary>
		[NUnit.Framework.Test]
		public virtual void TestExtraDependencies()
		{
			CoNLLUDocumentReader reader = new CoNLLUDocumentReader();
			Reader stringReader = new StringReader(ExtraDepsTestInput);
			IEnumerator<SemanticGraph> it = reader.GetIterator(stringReader);
			SemanticGraph sg = it.Current;
			NUnit.Framework.Assert.IsNotNull(sg);
			NUnit.Framework.Assert.IsFalse("The input only contains one dependency tree.", it.MoveNext());
			NUnit.Framework.Assert.IsTrue(sg.ContainsEdge(sg.GetNodeByIndex(4), sg.GetNodeByIndex(1)));
			NUnit.Framework.Assert.IsTrue(sg.ContainsEdge(sg.GetNodeByIndex(2), sg.GetNodeByIndex(7)));
			NUnit.Framework.Assert.IsTrue(sg.ContainsEdge(sg.GetNodeByIndex(4), sg.GetNodeByIndex(7)));
		}

		/// <summary>
		/// Tests whether reading a Semantic Graph and printing it
		/// is equal to the original input.
		/// </summary>
		private void TestSingleReadAndWrite(string input)
		{
			string clean = input.ReplaceAll("[\\t ]+", "\t");
			CoNLLUDocumentReader reader = new CoNLLUDocumentReader();
			CoNLLUDocumentWriter writer = new CoNLLUDocumentWriter();
			Reader stringReader = new StringReader(clean);
			IEnumerator<SemanticGraph> it = reader.GetIterator(stringReader);
			SemanticGraph sg = it.Current;
			string output = writer.PrintSemanticGraph(sg);
			NUnit.Framework.Assert.AreEqual(clean, output);
		}

		[NUnit.Framework.Test]
		public virtual void TestReadingAndWriting()
		{
			TestSingleReadAndWrite(CommentTestInput);
			TestSingleReadAndWrite(ExtraDepsTestInput);
			TestSingleReadAndWrite(MultiwordTestInput);
			TestSingleReadAndWrite(ExtraDepsTestEmptyNodeinput);
		}
	}
}

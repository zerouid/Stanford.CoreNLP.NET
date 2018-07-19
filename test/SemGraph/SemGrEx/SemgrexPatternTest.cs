using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Java.IO;
using Java.Nio.Charset;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <author>Chloe Kiddon</author>
	/// <author>Sonal Gupta</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SemgrexPatternTest
	{
		/*
		* Test method for 'edu.stanford.nlp.semgraph.semgrex.SemgrexPattern.prettyPrint()'
		*/
		[NUnit.Framework.Test]
		public virtual void TestPrettyPrint()
		{
			// SemgrexPattern pat = SemgrexPattern.compile("{} >sub {} & ?>ss {w:w}");
			// SemgrexPattern pat =
			// SemgrexPattern.compile("({} </nsubj|agent/ {pos:/VB.*/}=hypVerb) @ ({#} </nsubj|agent/ {pos:/VB.*/}=txtVerb)");
			// SemgrexPattern pat =
			// SemgrexPattern.compile("({sentIndex:4} <=hypReln {sentIndex:2}=hypGov) @ ({} <=hypReln ({} >conj_and ({} @ {}=hypGov)))");
			SemgrexPattern pat = SemgrexPattern.Compile("({}=partnerOne [[<prep_to ({word:/married/} >nsubjpass {}=partnerTwo)] | [<nsubjpass ({word:married} >prep_to {}=partnerTwo)]]) @ ({} [[>/nn|appos/ {lemma:/wife|husband/} >poss ({}=txtPartner @ {}=partnerTwo)] | [<poss (({}=txtPartner @ {}=partnerTwo) >/appos|nn/ {lemma:/wife|husband/})]])"
				);
			// SemgrexPattern pat =
			// SemgrexPattern.compile("({pos:/VB.*/}=hVerb @ {pos:/VB.*/}=tVerb) >/nsubj|nsubjpass|dobj|iobj|prep.*/=hReln ({}=hWord @ ({}=tWord [ [ >/nsubj|nsubjpass|dobj|iobj|prep.*/=tReln {}=tVerb] | [ >appos ({} >/nsubj|nsubjpass|dobj|iobj|prep.*/=tReln {}=tVerb) ] | [ <appos ({} >/nsubj|nsubjpass|dobj|iobj|prep.*/=tReln {}=tVerb)] | ![> {}=tVerb]]))");
			// SemgrexPattern pat =
			// SemgrexPattern.compile("({}=partnerOne [[<prep_to ({word:married} >nsubjpass {}=partnerTwo)] | [<nsubjpass ({word:married} >prep_to {}=partnerTwo)]]) @ ({} [[>nn {lemma:/wife|husband/} >poss {}=txtPartner] | [<poss ({}=txtPartner >nn {lemma:/wife|husband/})]])");
			// @ ({} </nsubj|agent/ {pos:/VB.*/}=txtVerb)
			pat.PrettyPrint();
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestFind()
		{
			SemanticGraph h = SemanticGraph.ValueOf("[married/VBN nsubjpass>Hughes/NNP auxpass>was/VBD prep_to>Gracia/NNP]");
			SemanticGraph t = SemanticGraph.ValueOf("[loved/VBD\nnsubj>Hughes/NNP\ndobj>[wife/NN poss>his/PRP$ appos>Gracia/NNP]\nconj_and>[obsessed/JJ\ncop>was/VBD\nadvmod>absolutely/RB\nprep_with>[Elicia/NN poss>his/PRP$ amod>little/JJ compound>daughter/NN]]]"
				);
			string s = "(ROOT\n(S\n(NP (DT The) (NN chimney) (NNS sweeps))\n(VP (VBP do) (RB not)\n(VP (VB like)\n(S\n(VP (VBG working)\n(PP (IN on)\n(NP (DT an) (JJ empty) (NN stomach)))))))\n(. .)))";
			Tree tree = Tree.ValueOf(s);
			SemanticGraph sg = SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Collapsed, GrammaticalStructure.Extras.Maximal, null);
			SemgrexPattern pat = SemgrexPattern.Compile("{}=gov ![>det {}] & > {word:/^(?!not).*$/}=dep");
			sg.PrettyPrint();
			// SemgrexPattern pat =
			// SemgrexPattern.compile("{} [[<prep_to ({word:married} >nsubjpass {})] | [<nsubjpass ({word:married} >prep_to {})]]");
			pat.PrettyPrint();
			SemgrexMatcher mat = pat.Matcher(sg);
			while (mat.Find())
			{
				// String match = mat.getMatch().word();
				string gov = mat.GetNode("gov").Word();
				// String reln = mat.getRelnString("reln");
				string dep = mat.GetNode("dep").Word();
				// System.out.println(match);
				System.Console.Out.WriteLine(dep + ' ' + gov);
			}
			SemgrexPattern pat2 = SemgrexPattern.Compile("{} [[>/nn|appos/ ({lemma:/wife|husband|partner/} >/poss/ {}=txtPartner)] | [<poss ({}=txtPartner >/nn|appos/ {lemma:/wife|husband|partner/})]" + "| [<nsubj ({$} >> ({word:/wife|husband|partner/} >poss {word:/his|her/} >/nn|appos/ {}))]]"
				);
			SemgrexMatcher mat2 = pat2.Matcher(t);
			while (mat2.Find())
			{
				string match = mat2.GetMatch().Word();
				// String gov = mat.getNode("gov").word();
				// String reln = mat.getRelnString("reln");
				// String dep = mat.getNode("dep").word();
				System.Console.Out.WriteLine(match);
			}
			// System.out.println(dep + " " + gov);
			Dictionary<IndexedWord, IndexedWord> map = new Dictionary<IndexedWord, IndexedWord>();
			map[h.GetNodeByWordPattern("Hughes")] = t.GetNodeByWordPattern("Hughes");
			map[h.GetNodeByWordPattern("Gracia")] = t.GetNodeByWordPattern("Gracia");
			Alignment alignment = new Alignment(map, 0, string.Empty);
			SemgrexPattern fullPat = SemgrexPattern.Compile("({}=partnerOne [[<prep_to ({word:married} >nsubjpass {}=partnerTwo)] | [<nsubjpass ({word:married} >prep_to {}=partnerTwo)]]) @ ({} [[>/nn|appos/ ({lemma:/wife|husband|partner/} >/poss/ {}=txtPartner)] | [<poss ({}=txtPartner >/nn|appos/ {lemma:/wife|husband|partner/})]"
				 + "| [<nsubj ({$} >> ({word:/wife|husband|partner/} >poss {word:/his|her/} >/nn|appos/ {}=txtPartner))]])");
			fullPat.PrettyPrint();
			SemgrexMatcher fullMat = fullPat.Matcher(h, alignment, t);
			if (fullMat.Find())
			{
				System.Console.Out.WriteLine("woo: " + fullMat.GetMatch().Word());
				System.Console.Out.WriteLine(fullMat.GetNode("txtPartner"));
				System.Console.Out.WriteLine(fullMat.GetNode("partnerOne"));
				System.Console.Out.WriteLine(fullMat.GetNode("partnerTwo"));
			}
			else
			{
				System.Console.Out.WriteLine("boo");
			}
			SemgrexPattern pat3 = SemgrexPattern.Compile("({word:LIKE}=parent >>/aux.*/ {word:/do/}=node)");
			System.Console.Out.WriteLine("pattern is ");
			pat3.PrettyPrint();
			System.Console.Out.WriteLine("tree is ");
			sg.PrettyPrint();
			//checking if ignoring case or not
			SemgrexMatcher mat3 = pat3.Matcher(sg, true);
			if (mat3.Find())
			{
				string parent = mat3.GetNode("parent").Word();
				string node = mat3.GetNode("node").Word();
				System.Console.Out.WriteLine("Result: parent is " + parent + " and node is " + node);
				NUnit.Framework.Assert.AreEqual(parent, "like");
				NUnit.Framework.Assert.AreEqual(node, "do");
			}
			else
			{
				NUnit.Framework.Assert.Fail();
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestMacro()
		{
			SemanticGraph h = SemanticGraph.ValueOf("[married/VBN nsubjpass>Hughes/NNP auxpass>was/VBD nmod:to>Gracia/NNP]");
			string macro = "macro WORD = married";
			string pattern = "({word:${WORD}}=parent >>nsubjpass {}=node)";
			IList<SemgrexPattern> pats = SemgrexBatchParser.CompileStream(new ByteArrayInputStream(Sharpen.Runtime.GetBytesForString((macro + "\n" + pattern), StandardCharsets.Utf8)));
			SemgrexPattern pat3 = pats[0];
			bool ignoreCase = true;
			SemgrexMatcher mat3 = pat3.Matcher(h, ignoreCase);
			if (mat3.Find())
			{
				string parent = mat3.GetNode("parent").Word();
				string node = mat3.GetNode("node").Word();
				System.Console.Out.WriteLine("Result: parent is " + parent + " and node is " + node);
				NUnit.Framework.Assert.AreEqual(parent, "married");
				NUnit.Framework.Assert.AreEqual(node, "Hughes");
			}
			else
			{
				throw new Exception("failed!");
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestEnv()
		{
			SemanticGraph h = SemanticGraph.ValueOf("[married/VBN nsubjpass>Hughes/NNP auxpass>was/VBD nmod:to>Gracia/NNP]");
			h.GetFirstRoot().Set(typeof(PatternsAnnotations.PatternLabel1), "YES");
			//SemanticGraph t = SemanticGraph
			//  .valueOf("[loved/VBD\nnsubj:Hughes/NNP\ndobj:[wife/NN poss:his/PRP$ appos:Gracia/NNP]\nconj_and:[obsessed/JJ\ncop:was/VBD\nadvmod:absolutely/RB\nprep_with:[Elicia/NN poss:his/PRP$ amod:little/JJ nn:daughter/NN]]]");
			string macro = "macro WORD = married";
			Env env = new Env();
			env.Bind("pattern1", typeof(PatternsAnnotations.PatternLabel1));
			string pattern = "({pattern1:YES}=parent >>nsubjpass {}=node)";
			IList<SemgrexPattern> pats = SemgrexBatchParser.CompileStream(new ByteArrayInputStream(Sharpen.Runtime.GetBytesForString((macro + "\n" + pattern), StandardCharsets.Utf8)), env);
			SemgrexPattern pat3 = pats[0];
			bool ignoreCase = true;
			SemgrexMatcher mat3 = pat3.Matcher(h, ignoreCase);
			if (mat3.Find())
			{
				string parent = mat3.GetNode("parent").Word();
				string node = mat3.GetNode("node").Word();
				System.Console.Out.WriteLine("Result: parent is " + parent + " and node is " + node);
				NUnit.Framework.Assert.AreEqual(parent, "married");
				NUnit.Framework.Assert.AreEqual(node, "Hughes");
			}
			else
			{
				throw new Exception("failed!");
			}
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		[NUnit.Framework.Test]
		public virtual void TestSerialization()
		{
			SemgrexPattern pat3 = SemgrexPattern.Compile("({word:LIKE}=parent >>nn {word:/do/}=node)");
			File tempfile = File.CreateTempFile("temp", "file");
			tempfile.DeleteOnExit();
			IOUtils.WriteObjectToFile(pat3, tempfile);
			SemgrexPattern pat4 = IOUtils.ReadObjectFromFile(tempfile);
			NUnit.Framework.Assert.AreEqual(pat3, pat4);
		}

		[NUnit.Framework.Test]
		public virtual void TestSiblingPatterns()
		{
			SemanticGraph sg = SemanticGraph.ValueOf("[loved/VBD-2\nnsubj>Hughes/NNP-1\ndobj>[wife/NN-4 nmod:poss>his/PRP$-3 appos>Gracia/NNP-5]\nconj:and>[obsessed/JJ-9\ncop>was/VBD-7\nadvmod>absolutely/RB-8\nnmod:with>[Elicia/NN-14 nmod:poss>his/PRP$-11 amod>little/JJ-12 compound>daughter/NN-13]]]"
				);
			/* Test "." */
			SemgrexPattern pat1 = SemgrexPattern.Compile("{tag:NNP}=w1 . {tag:VBD}=w2");
			SemgrexMatcher matcher = pat1.Matcher(sg);
			if (matcher.Find())
			{
				string w1 = matcher.GetNode("w1").Word();
				string w2 = matcher.GetNode("w2").Word();
				NUnit.Framework.Assert.AreEqual("Hughes", w1);
				NUnit.Framework.Assert.AreEqual("loved", w2);
			}
			else
			{
				throw new Exception("failed!");
			}
			/* Test "$+" */
			SemgrexPattern pat2 = SemgrexPattern.Compile("{word:was}=w1 $+ {}=w2");
			matcher = pat2.Matcher(sg);
			if (matcher.Find())
			{
				string w1 = matcher.GetNode("w1").Word();
				string w2 = matcher.GetNode("w2").Word();
				NUnit.Framework.Assert.AreEqual("was", w1);
				NUnit.Framework.Assert.AreEqual("absolutely", w2);
			}
			else
			{
				throw new Exception("failed!");
			}
			/* Test "$-" */
			SemgrexPattern pat3 = SemgrexPattern.Compile("{word:absolutely}=w1 $- {}=w2");
			matcher = pat3.Matcher(sg);
			if (matcher.Find())
			{
				string w1 = matcher.GetNode("w1").Word();
				string w2 = matcher.GetNode("w2").Word();
				NUnit.Framework.Assert.AreEqual("absolutely", w1);
				NUnit.Framework.Assert.AreEqual("was", w2);
			}
			else
			{
				throw new Exception("failed!");
			}
			/* Test "$++" */
			SemgrexPattern pat4 = SemgrexPattern.Compile("{word:his}=w1 $++ {tag:NN}=w2");
			matcher = pat4.Matcher(sg);
			if (matcher.Find())
			{
				string w1 = matcher.GetNode("w1").Word();
				string w2 = matcher.GetNode("w2").Word();
				NUnit.Framework.Assert.AreEqual("his", w1);
				NUnit.Framework.Assert.AreEqual("daughter", w2);
			}
			else
			{
				throw new Exception("failed!");
			}
			/* Test "$--" */
			SemgrexPattern pat6 = SemgrexPattern.Compile("{word:daughter}=w1 $-- {tag:/PRP./}=w2");
			matcher = pat6.Matcher(sg);
			if (matcher.Find())
			{
				string w1 = matcher.GetNode("w1").Word();
				string w2 = matcher.GetNode("w2").Word();
				NUnit.Framework.Assert.AreEqual("daughter", w1);
				NUnit.Framework.Assert.AreEqual("his", w2);
			}
			else
			{
				throw new Exception("failed!");
			}
			/* Test for not matching. */
			SemgrexPattern pat5 = SemgrexPattern.Compile("{word:his}=w1 $-- {}=w2");
			matcher = pat5.Matcher(sg);
			if (matcher.Find())
			{
				throw new Exception("failed!");
			}
			/* Test for negation. */
			SemgrexPattern pat7 = SemgrexPattern.Compile("{word:his}=w1 !$-- {}");
			matcher = pat7.Matcher(sg);
			if (matcher.Find())
			{
				string w1 = matcher.GetNode("w1").Word();
				NUnit.Framework.Assert.AreEqual("his", w1);
			}
			else
			{
				throw new Exception("failed!");
			}
			SemgrexPattern pat8 = SemgrexPattern.Compile("{word:his}=w1 !$++ {}");
			matcher = pat8.Matcher(sg);
			if (matcher.Find())
			{
				throw new Exception("failed!");
			}
		}
	}
}

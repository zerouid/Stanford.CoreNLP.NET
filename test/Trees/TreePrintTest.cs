using System.IO;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Start testing various output mechanisms for TreePrint.</summary>
	/// <remarks>Start testing various output mechanisms for TreePrint.  So far, just one</remarks>
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TreePrintTest
	{
		[NUnit.Framework.Test]
		public virtual void TestConll2007()
		{
			Tree test = Tree.ValueOf("((S (NP (PRP It)) (VP (VBZ is) (RB not) (ADJP (JJ normal)) (SBAR (IN for) (S (NP (NNS dogs)) (VP (TO to) (VP (VB be) (VP (VBG vomiting)))))))))");
			string[] words = new string[] { "It", "is", "not", "normal", "for", "dogs", "to", "be", "vomiting" };
			string[] tags = new string[] { "PRP", "VBZ", "RB", "JJ", "IN", "NNS", "TO", "VB", "VBG" };
			TreePrint treePrint = new TreePrint("conll2007");
			StringWriter writer = new StringWriter();
			PrintWriter wrapped = new PrintWriter(writer);
			treePrint.PrintTree(test, wrapped);
			wrapped.Close();
			string @out = writer.ToString();
			string[] lines = @out.Trim().Split("\n");
			for (int i = 0; i < lines.Length; ++i)
			{
				string[] pieces = lines[i].Trim().Split("\\s+");
				int lineNum = int.Parse(pieces[0]);
				NUnit.Framework.Assert.AreEqual((i + 1), lineNum);
				NUnit.Framework.Assert.AreEqual(words[i], pieces[1]);
				NUnit.Framework.Assert.AreEqual(tags[i], pieces[3]);
				NUnit.Framework.Assert.AreEqual(tags[i], pieces[4]);
			}
		}
	}
}

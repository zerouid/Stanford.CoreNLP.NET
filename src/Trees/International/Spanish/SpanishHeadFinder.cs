using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <author>Jon Gauthier</author>
	[System.Serializable]
	public class SpanishHeadFinder : AbstractCollinsHeadFinder
	{
		private const long serialVersionUID = -841219428125220698L;

		private static readonly string[] allVerbs = new string[] { "vmip000", "vmii000", "vmif000", "vmis000", "vmic000", "vmsp000", "vmsi000", "vmm0000", "vmn0000", "vmg0000", "vmp0000", "vaip000", "vaii000", "vaif000", "vais000", "vaic000", "vasp000"
			, "vasi000", "vam0000", "van0000", "vag0000", "vap0000", "vsip000", "vsii000", "vsis000", "vsif000", "vsic000", "vssp000", "vssi000", "vsm0000", "vsn0000", "vsg0000", "vsp0000" };

		public SpanishHeadFinder()
			: this(new SpanishTreebankLanguagePack())
		{
		}

		public SpanishHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			nonTerminalInfo = Generics.NewHashMap();
			// "sentence"
			string[][] rootRules = new string[][] { new string[] { "right", "grup.verb", "s.a", "sn" }, new string[] { "left", "S" }, new string[] { "right", "sadv", "grup.adv", "neg", "interjeccio", "i", "sp", "grup.prep" }, InsertVerbs(new string[] { 
				"rightdis" }, new string[] { "nc0s000", "nc0p000", "nc00000", "np00000", "rg", "rn" }) };
			nonTerminalInfo[tlp.StartSymbol()] = rootRules;
			nonTerminalInfo["S"] = rootRules;
			nonTerminalInfo["sentence"] = rootRules;
			nonTerminalInfo["inc"] = rootRules;
			// adjectival phrases
			string[][] adjectivePhraseRules = new string[][] { new string[] { "leftdis", "grup.a", "s.a", "spec" } };
			nonTerminalInfo["s.a"] = adjectivePhraseRules;
			nonTerminalInfo["sa"] = adjectivePhraseRules;
			nonTerminalInfo["grup.a"] = new string[][] { new string[] { "rightdis", "aq0000", "ao0000" }, InsertVerbs(new string[] { "right" }, new string[] {  }), new string[] { "right", "rg", "rn" } };
			// adverbial phrases
			nonTerminalInfo["sadv"] = new string[][] { new string[] { "left", "grup.adv", "sadv" } };
			nonTerminalInfo["grup.adv"] = new string[][] { new string[] { "left", "conj" }, new string[] { "rightdis", "rg", "rn", "neg", "grup.adv" }, new string[] { "rightdis", "pr000000", "pi000000", "nc0s000", "nc0p000", "nc00000", "np00000" } };
			nonTerminalInfo["neg"] = new string[][] { new string[] { "leftdis", "rg", "rn" } };
			// noun phrases
			nonTerminalInfo["sn"] = new string[][] { new string[] { "leftdis", "nc0s000", "nc0p000", "nc00000" }, new string[] { "left", "grup.nom", "grup.w", "grup.z", "sn" }, new string[] { "leftdis", "spec" } };
			nonTerminalInfo["grup.nom"] = new string[][] { new string[] { "leftdis", "nc0s000", "nc0p000", "nc00000", "np00000", "w", "grup.w" }, new string[] { "leftdis", "pi000000", "pd000000" }, new string[] { "left", "grup.nom", "sp" }, new string[]
				 { "leftdis", "pn000000", "aq0000", "ao0000" }, new string[] { "left", "grup.a", "i", "grup.verb" }, new string[] { "leftdis", "grup.adv" } };
			// verb phrases
			nonTerminalInfo["grup.verb"] = new string[][] { InsertVerbs(new string[] { "left" }, new string[] {  }) };
			nonTerminalInfo["infinitiu"] = new string[][] { InsertVerbs(new string[] { "left" }, new string[] { "infinitiu" }) };
			nonTerminalInfo["gerundi"] = new string[][] { new string[] { "left", "vmg0000", "vag0000", "vsg0000", "gerundi" } };
			nonTerminalInfo["participi"] = new string[][] { new string[] { "left", "aq", "vmp0000", "vap0000", "vsp0000", "grup.a" } };
			// specifiers
			nonTerminalInfo["spec"] = new string[][] { new string[] { "left", "conj", "spec" }, new string[] { "leftdis", "da0000", "de0000", "di0000", "dd0000", "dp0000", "dn0000", "dt0000" }, new string[] { "leftdis", "z0", "grup.z" }, new string[] { 
				"left", "rg", "rn" }, new string[] { "leftdis", "pt000000", "pe000000", "pd000000", "pp000000", "pi000000", "pn000000", "pr000000" }, new string[] { "left", "grup.adv", "w" } };
			// entre A y B
			// etc.
			nonTerminalInfo["conj"] = new string[][] { new string[] { "leftdis", "cs", "cc" }, new string[] { "leftdis", "grup.cc", "grup.cs" }, new string[] { "left", "sp" } };
			nonTerminalInfo["interjeccio"] = new string[][] { new string[] { "leftdis", "i", "nc0s000", "nc0p000", "nc00000", "np00000", "pi000000" }, new string[] { "left", "interjeccio" } };
			nonTerminalInfo["relatiu"] = new string[][] { new string[] { "left", "pr000000" } };
			// prepositional phrases
			nonTerminalInfo["sp"] = new string[][] { new string[] { "left", "prep", "sp" } };
			nonTerminalInfo["prep"] = new string[][] { new string[] { "leftdis", "sp000", "prep", "grup.prep" } };
			// custom categories
			nonTerminalInfo["grup.cc"] = new string[][] { new string[] { "left", "cs" } };
			nonTerminalInfo["grup.cs"] = new string[][] { new string[] { "left", "cs" } };
			nonTerminalInfo["grup.prep"] = new string[][] { new string[] { "left", "prep", "grup.prep", "s" } };
			nonTerminalInfo["grup.pron"] = new string[][] { new string[] { "rightdis", "px000000" } };
			nonTerminalInfo["grup.w"] = new string[][] { new string[] { "right", "w" }, new string[] { "leftdis", "z0" }, new string[] { "left" } };
			nonTerminalInfo["grup.z"] = new string[][] { new string[] { "leftdis", "z0", "zu", "zp", "zd", "zm" }, new string[] { "right", "nc0s000", "nc0p000", "nc00000", "np00000" } };
		}

		/// <summary>
		/// Build a list of head rules containing all of the possible verb
		/// tags.
		/// </summary>
		/// <remarks>
		/// Build a list of head rules containing all of the possible verb
		/// tags. The verbs are inserted in between <tt>toLeft</tt> and
		/// <tt>toRight</tt>.
		/// </remarks>
		private string[] InsertVerbs(string[] toLeft, string[] toRight)
		{
			return ArrayUtils.Concatenate(toLeft, ArrayUtils.Concatenate(allVerbs, toRight));
		}

		/// <summary>Go through trees and determine their heads and print them.</summary>
		/// <remarks>
		/// Go through trees and determine their heads and print them.
		/// Just for debugging. <br />
		/// Usage: <code>
		/// java edu.stanford.nlp.trees.international.spanish.SpanishHeadFinder treebankFilePath
		/// </code>
		/// </remarks>
		/// <param name="args">The treebankFilePath</param>
		public static void Main(string[] args)
		{
			Treebank treebank = new DiskTreebank();
			CategoryWordTag.suppressTerminalDetails = true;
			treebank.LoadPath(args[0]);
			IHeadFinder chf = new Edu.Stanford.Nlp.Trees.International.Spanish.SpanishHeadFinder();
			treebank.Apply(new _ITreeVisitor_146(chf));
		}

		private sealed class _ITreeVisitor_146 : ITreeVisitor
		{
			public _ITreeVisitor_146(IHeadFinder chf)
			{
				this.chf = chf;
			}

			public void VisitTree(Tree pt)
			{
				// pt.percolateHeads(chf);
				//pt.pennPrint();
				Tree head = pt.HeadTerminal(chf);
			}

			private readonly IHeadFinder chf;
		}
		//System.out.println("======== " + head.label());
	}
}

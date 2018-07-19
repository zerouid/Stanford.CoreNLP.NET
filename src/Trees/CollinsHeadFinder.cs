using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>Implements the HeadFinder found in Michael Collins' 1999 thesis.</summary>
	/// <remarks>
	/// Implements the HeadFinder found in Michael Collins' 1999 thesis.
	/// Except: we've added a head rule for NX, which returns the leftmost item.
	/// No rule for the head of NX is found in any of the versions of
	/// Collins' head table that we have (did he perhaps use the NP rules
	/// for NX? -- no Bikel, CL, 2005 says it defaults to leftmost).
	/// These rules are suitable for the Penn Treebank.
	/// <p>
	/// May 2004: Added support for AUX and AUXG to the VP rules; these cause
	/// no interference in Penn Treebank parsing, but means that these rules
	/// also work for the BLLIP corpus (or Charniak parser output in general).
	/// Feb 2005: Fixes to coordination reheading so that punctuation cannot
	/// become head.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class CollinsHeadFinder : AbstractCollinsHeadFinder
	{
		private static readonly string[] EmptyStringArray = new string[] {  };

		public CollinsHeadFinder()
			: this(new PennTreebankLanguagePack())
		{
		}

		/// <summary>
		/// This constructor provides the traditional behavior, where there is
		/// no special avoidance of punctuation categories.
		/// </summary>
		/// <param name="tlp">TreebankLanguagePack used for basic category function</param>
		public CollinsHeadFinder(ITreebankLanguagePack tlp)
			: this(tlp, EmptyStringArray)
		{
		}

		public CollinsHeadFinder(ITreebankLanguagePack tlp, params string[] categoriesToAvoid)
			: base(tlp, categoriesToAvoid)
		{
			nonTerminalInfo = Generics.NewHashMap();
			// This version from Collins' diss (1999: 236-238)
			nonTerminalInfo["ADJP"] = new string[][] { new string[] { "left", "NNS", "QP", "NN", "$", "ADVP", "JJ", "VBN", "VBG", "ADJP", "JJR", "NP", "JJS", "DT", "FW", "RBR", "RBS", "SBAR", "RB" } };
			nonTerminalInfo["ADVP"] = new string[][] { new string[] { "right", "RB", "RBR", "RBS", "FW", "ADVP", "TO", "CD", "JJR", "JJ", "IN", "NP", "JJS", "NN" } };
			nonTerminalInfo["CONJP"] = new string[][] { new string[] { "right", "CC", "RB", "IN" } };
			nonTerminalInfo["FRAG"] = new string[][] { new string[] { "right" } };
			// crap
			nonTerminalInfo["INTJ"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["LST"] = new string[][] { new string[] { "right", "LS", ":" } };
			nonTerminalInfo["NAC"] = new string[][] { new string[] { "left", "NN", "NNS", "NNP", "NNPS", "NP", "NAC", "EX", "$", "CD", "QP", "PRP", "VBG", "JJ", "JJS", "JJR", "ADJP", "FW" } };
			nonTerminalInfo["NX"] = new string[][] { new string[] { "left" } };
			// crap
			nonTerminalInfo["PP"] = new string[][] { new string[] { "right", "IN", "TO", "VBG", "VBN", "RP", "FW" } };
			// should prefer JJ? (PP (JJ such) (IN as) (NP (NN crocidolite)))
			nonTerminalInfo["PRN"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["PRT"] = new string[][] { new string[] { "right", "RP" } };
			nonTerminalInfo["QP"] = new string[][] { new string[] { "left", "$", "IN", "NNS", "NN", "JJ", "RB", "DT", "CD", "NCD", "QP", "JJR", "JJS" } };
			nonTerminalInfo["RRC"] = new string[][] { new string[] { "right", "VP", "NP", "ADVP", "ADJP", "PP" } };
			nonTerminalInfo["S"] = new string[][] { new string[] { "left", "TO", "IN", "VP", "S", "SBAR", "ADJP", "UCP", "NP" } };
			nonTerminalInfo["SBAR"] = new string[][] { new string[] { "left", "WHNP", "WHPP", "WHADVP", "WHADJP", "IN", "DT", "S", "SQ", "SINV", "SBAR", "FRAG" } };
			nonTerminalInfo["SBARQ"] = new string[][] { new string[] { "left", "SQ", "S", "SINV", "SBARQ", "FRAG" } };
			nonTerminalInfo["SINV"] = new string[][] { new string[] { "left", "VBZ", "VBD", "VBP", "VB", "MD", "VP", "S", "SINV", "ADJP", "NP" } };
			nonTerminalInfo["SQ"] = new string[][] { new string[] { "left", "VBZ", "VBD", "VBP", "VB", "MD", "VP", "SQ" } };
			nonTerminalInfo["UCP"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["VP"] = new string[][] { new string[] { "left", "TO", "VBD", "VBN", "MD", "VBZ", "VB", "VBG", "VBP", "AUX", "AUXG", "VP", "ADJP", "NN", "NNS", "NP" } };
			nonTerminalInfo["WHADJP"] = new string[][] { new string[] { "left", "CC", "WRB", "JJ", "ADJP" } };
			nonTerminalInfo["WHADVP"] = new string[][] { new string[] { "right", "CC", "WRB" } };
			nonTerminalInfo["WHNP"] = new string[][] { new string[] { "left", "WDT", "WP", "WP$", "WHADJP", "WHPP", "WHNP" } };
			nonTerminalInfo["WHPP"] = new string[][] { new string[] { "right", "IN", "TO", "FW" } };
			nonTerminalInfo["X"] = new string[][] { new string[] { "right" } };
			// crap rule
			nonTerminalInfo["NP"] = new string[][] { new string[] { "rightdis", "NN", "NNP", "NNPS", "NNS", "NX", "POS", "JJR" }, new string[] { "left", "NP" }, new string[] { "rightdis", "$", "ADJP", "PRN" }, new string[] { "right", "CD" }, new string[
				] { "rightdis", "JJ", "JJS", "RB", "QP" } };
			nonTerminalInfo["TYPO"] = new string[][] { new string[] { "left" } };
			// another crap rule, for Brown (Roger)
			nonTerminalInfo["EDITED"] = new string[][] { new string[] { "left" } };
			// crap rule for Switchboard (if don't delete EDITED nodes)
			nonTerminalInfo["XS"] = new string[][] { new string[] { "right", "IN" } };
		}

		// rule for new structure in QP
		protected internal override int PostOperationFix(int headIdx, Tree[] daughterTrees)
		{
			if (headIdx >= 2)
			{
				string prevLab = tlp.BasicCategory(daughterTrees[headIdx - 1].Value());
				if (prevLab.Equals("CC") || prevLab.Equals("CONJP"))
				{
					int newHeadIdx = headIdx - 2;
					Tree t = daughterTrees[newHeadIdx];
					while (newHeadIdx >= 0 && t.IsPreTerminal() && tlp.IsPunctuationTag(t.Value()))
					{
						newHeadIdx--;
					}
					if (newHeadIdx >= 0)
					{
						headIdx = newHeadIdx;
					}
				}
			}
			return headIdx;
		}

		/// <summary>Go through trees and determine their heads and print them.</summary>
		/// <remarks>
		/// Go through trees and determine their heads and print them.
		/// Just for debuggin'. <br />
		/// Usage: <code>
		/// java edu.stanford.nlp.trees.CollinsHeadFinder treebankFilePath
		/// </code>
		/// </remarks>
		/// <param name="args">The treebankFilePath</param>
		public static void Main(string[] args)
		{
			Treebank treebank = new DiskTreebank();
			CategoryWordTag.suppressTerminalDetails = true;
			treebank.LoadPath(args[0]);
			IHeadFinder chf = new Edu.Stanford.Nlp.Trees.CollinsHeadFinder();
			treebank.Apply(null);
		}

		private const long serialVersionUID = -8747319554557223437L;
	}
}

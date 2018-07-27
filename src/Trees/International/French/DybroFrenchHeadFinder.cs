using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>Implements the head finding rules from Dybro-Johansen master's thesis.</summary>
	/// <author>mcdm</author>
	[System.Serializable]
	public class DybroFrenchHeadFinder : AbstractCollinsHeadFinder
	{
		private const long serialVersionUID = 8798606577201646967L;

		public DybroFrenchHeadFinder()
			: this(new FrenchTreebankLanguagePack())
		{
		}

		public DybroFrenchHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			//French POS:
			// A (adjective), ADV (adverb), C (conjunction and subordinating conjunction), CL (clitics),
			// CS (subordinating conjunction) but occurs only once!,
			// D (determiner), ET (foreign word), I (interjection), N (noun),
			// P (preposition), PREF (prefix), PRO (strong pronoun -- very confusing), V (verb), PUNC (punctuation)
			// There is also the expanded French CC tagset.
			// V, A, ADV, PRO, C, CL, N, D are all split into multiple tags.
			// http://www.linguist.univ-paris-diderot.fr/~mcandito/Publications/crabbecandi-taln2008-final.pdf
			// (perhaps you can find an English translation somewhere)
			nonTerminalInfo = Generics.NewHashMap();
			// "sentence"
			nonTerminalInfo[tlp.StartSymbol()] = new string[][] { new string[] { "right", "VN", "AP", "NP", "Srel", "VPpart", "AdP", "I", "Ssub", "VPinf", "PP" }, new string[] { "rightdis", "ADV", "ADVWH" }, new string[] { "right" } };
			nonTerminalInfo["SENT"] = new string[][] { new string[] { "right", "VN", "AP", "NP", "Srel", "VPpart", "AdP", "I", "Ssub", "VPinf", "PP" }, new string[] { "rightdis", "ADV", "ADVWH" }, new string[] { "right" } };
			// adjectival phrases
			nonTerminalInfo["AP"] = new string[][] { new string[] { "rightdis", "A", "ADJ", "ADJWH" }, new string[] { "right", "ET" }, new string[] { "rightdis", "V", "VIMP", "VINF", "VS", "VPP", "VPR" }, new string[] { "rightdis", "ADV", "ADVWH" } };
			// adverbial phrases
			nonTerminalInfo["AdP"] = new string[][] { new string[] { "rightdis", "ADV", "ADVWH" }, new string[] { "right" } };
			// coordinated phrases
			nonTerminalInfo["COORD"] = new string[][] { new string[] { "leftdis", "C", "CC", "CS" }, new string[] { "left" } };
			// noun phrases
			nonTerminalInfo["NP"] = new string[][] { new string[] { "leftdis", "N", "NPP", "NC", "PRO", "PROWH", "PROREL" }, new string[] { "left", "NP" }, new string[] { "leftdis", "A", "ADJ", "ADJWH" }, new string[] { "left", "AP", "I", "VPpart" }, new 
				string[] { "leftdis", "ADV", "ADVWH" }, new string[] { "left", "AdP", "ET" }, new string[] { "leftdis", "D", "DET", "DETWH" } };
			// prepositional phrases
			nonTerminalInfo["PP"] = new string[][] { new string[] { "left", "P" }, new string[] { "left" } };
			// verbal nucleus
			nonTerminalInfo["VN"] = new string[][] { new string[] { "right", "V", "VPinf" }, new string[] { "right" } };
			// infinitive clauses
			nonTerminalInfo["VPinf"] = new string[][] { new string[] { "left", "VN" }, new string[] { "leftdis", "V", "VIMP", "VINF", "VS", "VPP", "VPR" }, new string[] { "left" } };
			// nonfinite clauses
			nonTerminalInfo["VPpart"] = new string[][] { new string[] { "leftdis", "V", "VIMP", "VINF", "VS", "VPP", "VPR" }, new string[] { "left", "VN" }, new string[] { "left" } };
			// relative clauses
			nonTerminalInfo["Srel"] = new string[][] { new string[] { "right", "VN", "AP", "NP" }, new string[] { "right" } };
			// subordinate clauses
			nonTerminalInfo["Ssub"] = new string[][] { new string[] { "right", "VN", "AP", "NP", "PP", "VPinf", "Ssub", "VPpart" }, new string[] { "rightdis", "A", "ADJ", "ADJWH" }, new string[] { "rightdis", "ADV", "ADVWH" }, new string[] { "right" } };
			// parenthetical clauses
			nonTerminalInfo["Sint"] = new string[][] { new string[] { "right", "VN", "AP", "NP", "PP", "VPinf", "Ssub", "VPpart" }, new string[] { "rightdis", "A", "ADJ", "ADJWH" }, new string[] { "rightdis", "ADV", "ADVWH" }, new string[] { "right" } };
			// adverbes
			//nonTerminalInfo.put("ADV", new String[][] {{"left", "ADV", "PP", "P"}});
			// compound categories: start with MW: D, A, C, N, ADV, V, P, PRO, CL
			nonTerminalInfo["MWD"] = new string[][] { new string[] { "leftdis", "D", "DET", "DETWH" }, new string[] { "left" } };
			nonTerminalInfo["MWA"] = new string[][] { new string[] { "left", "P" }, new string[] { "leftdis", "N", "NPP", "NC" }, new string[] { "rightdis", "A", "ADJ", "ADJWH" }, new string[] { "right" } };
			nonTerminalInfo["MWC"] = new string[][] { new string[] { "leftdis", "C", "CC", "CS" }, new string[] { "left" } };
			nonTerminalInfo["MWN"] = new string[][] { new string[] { "rightdis", "N", "NPP", "NC" }, new string[] { "rightdis", "ET" }, new string[] { "right" } };
			nonTerminalInfo["MWV"] = new string[][] { new string[] { "leftdis", "V", "VIMP", "VINF", "VS", "VPP", "VPR" }, new string[] { "left" } };
			nonTerminalInfo["MWP"] = new string[][] { new string[] { "left", "P" }, new string[] { "leftdis", "ADV", "ADVWH" }, new string[] { "leftdis", "PRO", "PROWH", "PROREL" }, new string[] { "left" } };
			nonTerminalInfo["MWPRO"] = new string[][] { new string[] { "leftdis", "PRO", "PROWH", "PROREL" }, new string[] { "leftdis", "CL", "CLS", "CLR", "CLO" }, new string[] { "leftdis", "N", "NPP", "NC" }, new string[] { "leftdis", "A", "ADJ", "ADJWH"
				 }, new string[] { "left" } };
			nonTerminalInfo["MWCL"] = new string[][] { new string[] { "leftdis", "CL", "CLS", "CLR", "CLO" }, new string[] { "right" } };
			nonTerminalInfo["MWADV"] = new string[][] { new string[] { "left", "P" }, new string[] { "leftdis", "ADV", "ADVWH" }, new string[] { "left" } };
			nonTerminalInfo["MWI"] = new string[][] { new string[] { "leftdis", "N", "NPP", "NC" }, new string[] { "leftdis", "ADV", "ADVWH" }, new string[] { "left", "P" }, new string[] { "left" } };
			nonTerminalInfo["MWET"] = new string[][] { new string[] { "left", "ET" }, new string[] { "leftdis", "N", "NPP", "NC" }, new string[] { "left" } };
			//TODO: wsg2011: For phrasal nodes that lacked a label.
			nonTerminalInfo[FrenchXMLTreeReader.MissingPhrasal] = new string[][] { new string[] { "left" } };
		}

		/// <summary>Go through trees and determine their heads and print them.</summary>
		/// <remarks>
		/// Go through trees and determine their heads and print them.
		/// Just for debugging. <br />
		/// Usage: <code>
		/// java edu.stanford.nlp.trees.DybroFrenchHeadFinder treebankFilePath
		/// </code>
		/// </remarks>
		/// <param name="args">The treebankFilePath</param>
		public static void Main(string[] args)
		{
			Treebank treebank = new DiskTreebank();
			CategoryWordTag.suppressTerminalDetails = true;
			treebank.LoadPath(args[0]);
			IHeadFinder chf = new Edu.Stanford.Nlp.Trees.International.French.DybroFrenchHeadFinder();
			treebank.Apply(null);
		}
	}
}

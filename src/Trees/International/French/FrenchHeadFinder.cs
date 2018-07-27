using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>
	/// TODO wsg2010: Compare these head finding rules to those found in Arun Abishek's
	/// master's thesis.
	/// </summary>
	/// <author>mcdm</author>
	[System.Serializable]
	public class FrenchHeadFinder : AbstractCollinsHeadFinder
	{
		public FrenchHeadFinder()
			: this(new FrenchTreebankLanguagePack())
		{
		}

		public FrenchHeadFinder(FrenchTreebankLanguagePack tlp)
			: base(tlp)
		{
			//French POS:
			// A (adjective), ADV (adverb), C (conjunction and subordinating conjunction), CL (clitics),
			// CS (subordinating conjunction) but occurs only once!,
			// D (determiner), ET (foreign word), I (interjection), N (noun),
			// P (preposition), PREF (prefix), PRO (strong pronoun -- very confusing), V (verb), PUNC (punctuation)
			nonTerminalInfo = Generics.NewHashMap();
			// "sentence"
			nonTerminalInfo[tlp.StartSymbol()] = new string[][] { new string[] { "left", "VN", "NP" }, new string[] { "left" } };
			nonTerminalInfo["SENT"] = new string[][] { new string[] { "left", "VN", "NP" }, new string[] { "left" } };
			// adjectival phrases
			nonTerminalInfo["AP"] = new string[][] { new string[] { "left", "A", "V" }, new string[] { "rightdis", "N", "ET" }, new string[] { "left" } };
			// adverbial phrases
			nonTerminalInfo["AdP"] = new string[][] { new string[] { "right", "ADV" }, new string[] { "left", "N" }, new string[] { "right" } };
			// coordinated phrases
			nonTerminalInfo["COORD"] = new string[][] { new string[] { "leftdis", "C", "CC", "ADV", "PP", "P" }, new string[] { "left" } };
			// noun phrases
			nonTerminalInfo["NP"] = new string[][] { new string[] { "rightdis", "N", "PRO", "NP", "A" }, new string[] { "right", "ET" }, new string[] { "right" } };
			// prepositional phrases
			nonTerminalInfo["PP"] = new string[][] { new string[] { "left", "P", "PRO", "A", "NP", "V", "PP", "ADV" }, new string[] { "left" } };
			// verbal nucleus
			nonTerminalInfo["VN"] = new string[][] { new string[] { "right", "V", "VN" }, new string[] { "right" } };
			// infinitive clauses
			nonTerminalInfo["VPinf"] = new string[][] { new string[] { "left", "VN", "V" }, new string[] { "left" } };
			// nonfinite clauses
			nonTerminalInfo["VPpart"] = new string[][] { new string[] { "left", "VN", "V", "AP", "A", "AdP", "VPpart" }, new string[] { "left" } };
			// relative clauses
			nonTerminalInfo["Srel"] = new string[][] { new string[] { "left", "NP", "PRO", "PP", "C", "ADV" } };
			// subordinate clauses
			nonTerminalInfo["Ssub"] = new string[][] { new string[] { "left", "C", "PC", "ADV", "P", "PP" }, new string[] { "left" } };
			// parenthetical clauses
			nonTerminalInfo["Sint"] = new string[][] { new string[] { "left", "VN", "V", "NP", "Sint", "Ssub", "PP" }, new string[] { "left" } };
			// adverbes
			nonTerminalInfo["ADV"] = new string[][] { new string[] { "left", "ADV", "PP", "P" } };
			// compound categories: start with MW: D, A, C, N, ADV, V, P, PRO, CL
			nonTerminalInfo["MWD"] = new string[][] { new string[] { "left", "D" }, new string[] { "left" } };
			nonTerminalInfo["MWA"] = new string[][] { new string[] { "left", "P" }, new string[] { "left", "N" }, new string[] { "right", "A" }, new string[] { "right" } };
			nonTerminalInfo["MWC"] = new string[][] { new string[] { "left", "C", "CS" }, new string[] { "left" } };
			nonTerminalInfo["MWN"] = new string[][] { new string[] { "right", "N", "ET" }, new string[] { "right" } };
			nonTerminalInfo["MWV"] = new string[][] { new string[] { "left", "V" }, new string[] { "left" } };
			nonTerminalInfo["MWP"] = new string[][] { new string[] { "left", "P", "ADV", "PRO" }, new string[] { "left" } };
			nonTerminalInfo["MWPRO"] = new string[][] { new string[] { "left", "PRO", "CL", "N", "A" }, new string[] { "left" } };
			nonTerminalInfo["MWCL"] = new string[][] { new string[] { "left", "CL" }, new string[] { "right" } };
			nonTerminalInfo["MWADV"] = new string[][] { new string[] { "left", "P", "ADV" }, new string[] { "left" } };
			nonTerminalInfo["MWI"] = new string[][] { new string[] { "left", "N", "ADV", "P" }, new string[] { "left" } };
			nonTerminalInfo["MWET"] = new string[][] { new string[] { "left", "ET", "N" }, new string[] { "left" } };
			//TODO: wsg2011: For phrasal nodes that lacked a label.
			nonTerminalInfo[FrenchXMLTreeReader.MissingPhrasal] = new string[][] { new string[] { "left" } };
		}

		/// <summary>Go through trees and determine their heads and print them.</summary>
		/// <remarks>
		/// Go through trees and determine their heads and print them.
		/// Just for debugging. <br />
		/// Usage: <code>
		/// java edu.stanford.nlp.trees.FrenchHeadFinder treebankFilePath
		/// </code>
		/// </remarks>
		/// <param name="args">The treebankFilePath</param>
		public static void Main(string[] args)
		{
			Treebank treebank = new DiskTreebank();
			CategoryWordTag.suppressTerminalDetails = true;
			treebank.LoadPath(args[0]);
			IHeadFinder chf = new Edu.Stanford.Nlp.Trees.International.French.FrenchHeadFinder();
			treebank.Apply(null);
		}

		private const long serialVersionUID = 8747319554557223422L;
	}
}

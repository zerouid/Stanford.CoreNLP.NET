using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.International.French
{
	/// <summary>Head finding rules from Arun Abishek's master's thesis.</summary>
	/// <author>mcdm</author>
	[System.Serializable]
	public class AbishekFrenchHeadFinder : AbstractCollinsHeadFinder
	{
		private const long serialVersionUID = -7195627297254128427L;

		public AbishekFrenchHeadFinder()
			: this(new FrenchTreebankLanguagePack())
		{
		}

		public AbishekFrenchHeadFinder(FrenchTreebankLanguagePack tlp)
			: base(tlp)
		{
			//French POS:
			// A (adjective), ADV (adverb), C (conjunction and subordinating conjunction), CL (clitics),
			// CS (subordinating conjunction) but occurs only once!,
			// D (determiner), ET (foreign word), I (interjection), N (noun),
			// P (preposition), PREF (prefix), PRO (strong pronoun -- very confusing), V (verb), PUNC (punctuation)
			nonTerminalInfo = Generics.NewHashMap();
			// "sentence"
			nonTerminalInfo[tlp.StartSymbol()] = new string[][] { new string[] { "left", "VN", "V", "NP", "Srel", "Ssub", "Sint" } };
			nonTerminalInfo["SENT"] = new string[][] { new string[] { "left", "VN", "V", "NP", "Srel", "Ssub", "Sint" } };
			// adjectival phrases
			nonTerminalInfo["AP"] = new string[][] { new string[] { "right", "A", "N", "V" } };
			// adverbial phrases
			nonTerminalInfo["AdP"] = new string[][] { new string[] { "right", "ADV" }, new string[] { "left", "P", "D", "C" } };
			// coordinated phrases
			nonTerminalInfo["COORD"] = new string[][] { new string[] { "left", "C" }, new string[] { "right" } };
			// noun phrases
			nonTerminalInfo["NP"] = new string[][] { new string[] { "right", "N", "PRO", "A", "ADV" }, new string[] { "left", "NP" }, new string[] { "right" } };
			// prepositional phrases
			nonTerminalInfo["PP"] = new string[][] { new string[] { "right", "P", "CL", "A", "ADV", "V", "N" } };
			// verbal nucleus
			nonTerminalInfo["VN"] = new string[][] { new string[] { "right", "V" } };
			// infinitive clauses
			nonTerminalInfo["VPinf"] = new string[][] { new string[] { "left", "VN", "V" }, new string[] { "right" } };
			// nonfinite clauses
			nonTerminalInfo["VPpart"] = new string[][] { new string[] { "left", "VN", "V" }, new string[] { "right" } };
			// relative clauses
			nonTerminalInfo["Srel"] = new string[][] { new string[] { "left", "VN", "V" } };
			// subordinate clauses
			nonTerminalInfo["Ssub"] = new string[][] { new string[] { "left", "VN", "V" }, new string[] { "right" } };
			// parenthetical clauses
			nonTerminalInfo["Sint"] = new string[][] { new string[] { "left", "VN", "V" }, new string[] { "right" } };
			// adverbes
			//nonTerminalInfo.put("ADV", new String[][] {{"left", "ADV", "PP", "P"}});
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
			IHeadFinder chf = new Edu.Stanford.Nlp.Trees.International.French.AbishekFrenchHeadFinder();
			treebank.Apply(null);
		}
	}
}

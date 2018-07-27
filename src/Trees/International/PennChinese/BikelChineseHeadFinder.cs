using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>A headfinder implementing Dan Bikel's head rules.</summary>
	/// <remarks>
	/// A headfinder implementing Dan Bikel's head rules.
	/// March 2005: Updated to match the head-finding rules found in
	/// Bikel's thesis (2004).
	/// </remarks>
	/// <author>Galen Andrew</author>
	/// <author>Christopher Manning.</author>
	[System.Serializable]
	public class BikelChineseHeadFinder : AbstractCollinsHeadFinder
	{
		private const long serialVersionUID = -5445795668059315082L;

		public BikelChineseHeadFinder()
			: this(new ChineseTreebankLanguagePack())
		{
		}

		public BikelChineseHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			nonTerminalInfo = Generics.NewHashMap();
			// these are first-cut rules
			defaultRule = new string[] { "right" };
			// ROOT is not always unary for chinese -- PAIR is a special notation
			// that the Irish people use for non-unary ones....
			nonTerminalInfo["ROOT"] = new string[][] { new string[] { "left", "IP" } };
			nonTerminalInfo["PAIR"] = new string[][] { new string[] { "left", "IP" } };
			// Major syntactic categories
			nonTerminalInfo["ADJP"] = new string[][] { new string[] { "right", "ADJP", "JJ" }, new string[] { "right", "AD", "NN", "CS" } };
			nonTerminalInfo["ADVP"] = new string[][] { new string[] { "right", "ADVP", "AD" } };
			nonTerminalInfo["CLP"] = new string[][] { new string[] { "right", "CLP", "M" } };
			nonTerminalInfo["CP"] = new string[][] { new string[] { "right", "DEC", "SP" }, new string[] { "left", "ADVP", "CS" }, new string[] { "right", "CP", "IP" } };
			nonTerminalInfo["DNP"] = new string[][] { new string[] { "right", "DNP", "DEG" }, new string[] { "right", "DEC" } };
			nonTerminalInfo["DP"] = new string[][] { new string[] { "left", "DP", "DT" } };
			nonTerminalInfo["DVP"] = new string[][] { new string[] { "right", "DVP", "DEV" } };
			nonTerminalInfo["FRAG"] = new string[][] { new string[] { "right", "VV", "NR", "NN" } };
			nonTerminalInfo["INTJ"] = new string[][] { new string[] { "right", "INTJ", "IJ" } };
			nonTerminalInfo["IP"] = new string[][] { new string[] { "right", "IP", "VP" }, new string[] { "right", "VV" } };
			nonTerminalInfo["LCP"] = new string[][] { new string[] { "right", "LCP", "LC" } };
			nonTerminalInfo["LST"] = new string[][] { new string[] { "left", "LST", "CD", "OD" } };
			nonTerminalInfo["NP"] = new string[][] { new string[] { "right", "NP", "NN", "NT", "NR", "QP" } };
			nonTerminalInfo["PP"] = new string[][] { new string[] { "left", "PP", "P" } };
			nonTerminalInfo["PRN"] = new string[][] { new string[] { "right", "NP", "IP", "VP", "NT", "NR", "NN" } };
			nonTerminalInfo["QP"] = new string[][] { new string[] { "right", "QP", "CLP", "CD", "OD" } };
			nonTerminalInfo["UCP"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["VP"] = new string[][] { new string[] { "left", "VP", "VA", "VC", "VE", "VV", "BA", "LB", "VCD", "VSB", "VRD", "VNV", "VCP" } };
			nonTerminalInfo["VCD"] = new string[][] { new string[] { "right", "VCD", "VV", "VA", "VC", "VE" } };
			nonTerminalInfo["VCP"] = new string[][] { new string[] { "right", "VCP", "VV", "VA", "VC", "VE" } };
			nonTerminalInfo["VRD"] = new string[][] { new string[] { "right", "VRD", "VV", "VA", "VC", "VE" } };
			nonTerminalInfo["VSB"] = new string[][] { new string[] { "right", "VSB", "VV", "VA", "VC", "VE" } };
			nonTerminalInfo["VNV"] = new string[][] { new string[] { "right", "VNV", "VV", "VA", "VC", "VE" } };
			nonTerminalInfo["VPT"] = new string[][] { new string[] { "right", "VNV", "VV", "VA", "VC", "VE" } };
			// VNV typo for VPT? None of either in ctb4.
			nonTerminalInfo["WHNP"] = new string[][] { new string[] { "right", "WHNP", "NP", "NN", "NT", "NR", "QP" } };
			nonTerminalInfo["WHPP"] = new string[][] { new string[] { "left", "WHPP", "PP", "P" } };
			// some POS tags apparently sit where phrases are supposed to be
			nonTerminalInfo["CD"] = new string[][] { new string[] { "right", "CD" } };
			nonTerminalInfo["NN"] = new string[][] { new string[] { "right", "NN" } };
			nonTerminalInfo["NR"] = new string[][] { new string[] { "right", "NR" } };
			// parsing.  It shouldn't affect anything else because heads of preterminals are not
			// generally queried - GMA
			nonTerminalInfo["VV"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VA"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VC"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VE"] = new string[][] { new string[] { "left" } };
		}
	}
}

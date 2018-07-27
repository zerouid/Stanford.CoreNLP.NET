using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>HeadFinder for the Penn Chinese Treebank.</summary>
	/// <remarks>
	/// HeadFinder for the Penn Chinese Treebank.  Adapted from
	/// CollinsHeadFinder. This is the version used in Levy and Manning (2003).
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class ChineseHeadFinder : AbstractCollinsHeadFinder
	{
		/// <summary>If true, reverses the direction of search in VP and IP coordinations.</summary>
		/// <remarks>
		/// If true, reverses the direction of search in VP and IP coordinations.
		/// Works terribly .
		/// </remarks>
		private const bool coordSwitch = false;

		internal static readonly string[] leftExceptPunct = new string[] { "leftexcept", "PU" };

		internal static readonly string[] rightExceptPunct = new string[] { "rightexcept", "PU" };

		public ChineseHeadFinder()
			: this(new ChineseTreebankLanguagePack())
		{
		}

		public ChineseHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			nonTerminalInfo = Generics.NewHashMap();
			// these are first-cut rules
			string left = (coordSwitch ? "right" : "left");
			string right = (coordSwitch ? "left" : "right");
			string rightdis = "rightdis";
			defaultRule = new string[] { right };
			// ROOT is not always unary for chinese -- PAIR is a special notation
			// that the Irish people use for non-unary ones....
			nonTerminalInfo["ROOT"] = new string[][] { new string[] { left, "IP" } };
			nonTerminalInfo["PAIR"] = new string[][] { new string[] { left, "IP" } };
			// Major syntactic categories
			nonTerminalInfo["ADJP"] = new string[][] { new string[] { left, "JJ", "ADJP" } };
			// there is one ADJP unary rewrite to AD but otherwise all have JJ or ADJP
			nonTerminalInfo["ADVP"] = new string[][] { new string[] { left, "AD", "CS", "ADVP", "JJ" } };
			// CS is a subordinating conjunctor, and there are a couple of ADVP->JJ unary rewrites
			nonTerminalInfo["CLP"] = new string[][] { new string[] { right, "M", "CLP" } };
			//nonTerminalInfo.put("CP", new String[][] {{left, "WHNP","IP","CP","VP"}}); // this is complicated; see bracketing guide p. 34.  Actually, all WHNP are empty.  IP/CP seems to be the best semantic head; syntax would dictate DEC/ADVP. Using IP/CP/VP/M is INCREDIBLY bad for Dep parser - lose 3% absolute.
			nonTerminalInfo["CP"] = new string[][] { new string[] { right, "DEC", "WHNP", "WHPP" }, rightExceptPunct };
			// the (syntax-oriented) right-first head rule
			// nonTerminalInfo.put("CP", new String[][]{{right, "DEC", "ADVP", "CP", "IP", "VP", "M"}}); // the (syntax-oriented) right-first head rule
			nonTerminalInfo["DNP"] = new string[][] { new string[] { right, "DEG", "DEC" }, rightExceptPunct };
			// according to tgrep2, first preparation, all DNPs have a DEG daughter
			nonTerminalInfo["DP"] = new string[][] { new string[] { left, "DT", "DP" } };
			// there's one instance of DP adjunction
			nonTerminalInfo["DVP"] = new string[][] { new string[] { right, "DEV", "DEC" } };
			// DVP always has DEV under it
			nonTerminalInfo["FRAG"] = new string[][] { new string[] { right, "VV", "NN" }, rightExceptPunct };
			//FRAG seems only to be used for bits at the beginnings of articles: "Xinwenshe<DATE>" and "(wan)"
			nonTerminalInfo["INTJ"] = new string[][] { new string[] { right, "INTJ", "IJ", "SP" } };
			nonTerminalInfo["IP"] = new string[][] { new string[] { left, "VP", "IP" }, rightExceptPunct };
			// CDM July 2010 following email from Pi-Chuan changed preference to VP over IP: IP can be -SBJ, -OBJ, or -ADV, and shouldn't be head
			nonTerminalInfo["LCP"] = new string[][] { new string[] { right, "LC", "LCP" } };
			// there's a bit of LCP adjunction
			nonTerminalInfo["LST"] = new string[][] { new string[] { right, "CD", "PU" } };
			// covers all examples
			nonTerminalInfo["NP"] = new string[][] { new string[] { right, "NN", "NR", "NT", "NP", "PN", "CP" } };
			// Basic heads are NN/NR/NT/NP; PN is pronoun.  Some NPs are nominalized relative clauses without overt nominal material; these are NP->CP unary rewrites.  Finally, note that this doesn't give any special treatment of coordination.
			nonTerminalInfo["PP"] = new string[][] { new string[] { left, "P", "PP" } };
			// in the manual there's an example of VV heading PP but I couldn't find such an example with tgrep2
			// cdm 2006: PRN changed to not choose punctuation.  Helped parsing (if not significantly)
			// nonTerminalInfo.put("PRN", new String[][]{{left, "PU"}}); //presumably left/right doesn't matter
			nonTerminalInfo["PRN"] = new string[][] { new string[] { left, "NP", "VP", "IP", "QP", "PP", "ADJP", "CLP", "LCP" }, new string[] { rightdis, "NN", "NR", "NT", "FW" } };
			// cdm 2006: QP: add OD -- occurs some; occasionally NP, NT, M; parsing performance no-op
			nonTerminalInfo["QP"] = new string[][] { new string[] { right, "QP", "CLP", "CD", "OD", "NP", "NT", "M" } };
			// there's some QP adjunction
			// add OD?
			nonTerminalInfo["UCP"] = new string[][] { new string[] { left } };
			//an alternative would be "PU","CC"
			nonTerminalInfo["VP"] = new string[][] { new string[] { left, "VP", "VCD", "VPT", "VV", "VCP", "VA", "VC", "VE", "IP", "VSB", "VCP", "VRD", "VNV" }, leftExceptPunct };
			//note that ba and long bei introduce IP-OBJ small clauses; short bei introduces VP
			// add BA, LB, as needed
			// verb compounds
			nonTerminalInfo["VCD"] = new string[][] { new string[] { left, "VCD", "VV", "VA", "VC", "VE" } };
			// could easily be right instead
			nonTerminalInfo["VCP"] = new string[][] { new string[] { left, "VCD", "VV", "VA", "VC", "VE" } };
			// not much info from documentation
			nonTerminalInfo["VRD"] = new string[][] { new string[] { left, "VCD", "VRD", "VV", "VA", "VC", "VE" } };
			// definitely left
			nonTerminalInfo["VSB"] = new string[][] { new string[] { right, "VCD", "VSB", "VV", "VA", "VC", "VE" } };
			// definitely right, though some examples look questionably classified (na2lai2 zhi1fu4)
			nonTerminalInfo["VNV"] = new string[][] { new string[] { left, "VV", "VA", "VC", "VE" } };
			// left/right doesn't matter
			nonTerminalInfo["VPT"] = new string[][] { new string[] { left, "VV", "VA", "VC", "VE" } };
			// activity verb is to the left
			// some POS tags apparently sit where phrases are supposed to be
			nonTerminalInfo["CD"] = new string[][] { new string[] { right, "CD" } };
			nonTerminalInfo["NN"] = new string[][] { new string[] { right, "NN" } };
			nonTerminalInfo["NR"] = new string[][] { new string[] { right, "NR" } };
			// I'm adding these POS tags to do primitive morphology for character-level
			// parsing.  It shouldn't affect anything else because heads of preterminals are not
			// generally queried - GMA
			nonTerminalInfo["VV"] = new string[][] { new string[] { left } };
			nonTerminalInfo["VA"] = new string[][] { new string[] { left } };
			nonTerminalInfo["VC"] = new string[][] { new string[] { left } };
			nonTerminalInfo["VE"] = new string[][] { new string[] { left } };
			// new for ctb6.
			nonTerminalInfo["FLR"] = new string[][] { rightExceptPunct };
			// new for CTB9
			nonTerminalInfo["DFL"] = new string[][] { rightExceptPunct };
			nonTerminalInfo["EMO"] = new string[][] { leftExceptPunct };
			// left/right doesn't matter
			nonTerminalInfo["INC"] = new string[][] { leftExceptPunct };
			nonTerminalInfo["INTJ"] = new string[][] { leftExceptPunct };
			nonTerminalInfo["OTH"] = new string[][] { leftExceptPunct };
			nonTerminalInfo["SKIP"] = new string[][] { leftExceptPunct };
		}

		private const long serialVersionUID = 6143632784691159283L;
	}
}

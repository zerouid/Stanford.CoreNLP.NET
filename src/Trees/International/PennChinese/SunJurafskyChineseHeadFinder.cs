using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>A HeadFinder for Chinese based on rules described in Sun/Jurafsky NAACL 2004.</summary>
	/// <author>Galen Andrew</author>
	/// <version>Jul 12, 2004</version>
	[System.Serializable]
	public class SunJurafskyChineseHeadFinder : AbstractCollinsHeadFinder
	{
		private const long serialVersionUID = -7942375587642755210L;

		public SunJurafskyChineseHeadFinder()
			: this(new ChineseTreebankLanguagePack())
		{
		}

		public SunJurafskyChineseHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			defaultRule = new string[] { "right" };
			nonTerminalInfo = Generics.NewHashMap();
			nonTerminalInfo["ROOT"] = new string[][] { new string[] { "left", "IP" } };
			nonTerminalInfo["PAIR"] = new string[][] { new string[] { "left", "IP" } };
			nonTerminalInfo["ADJP"] = new string[][] { new string[] { "right", "ADJP", "JJ", "AD" } };
			nonTerminalInfo["ADVP"] = new string[][] { new string[] { "right", "ADVP", "AD", "CS", "JJ", "NP", "PP", "P", "VA", "VV" } };
			nonTerminalInfo["CLP"] = new string[][] { new string[] { "right", "CLP", "M", "NN", "NP" } };
			nonTerminalInfo["CP"] = new string[][] { new string[] { "right", "CP", "IP", "VP" } };
			nonTerminalInfo["DNP"] = new string[][] { new string[] { "right", "DEG", "DNP", "DEC", "QP" } };
			nonTerminalInfo["DP"] = new string[][] { new string[] { "left", "M", "DP", "DT", "OD" } };
			nonTerminalInfo["DVP"] = new string[][] { new string[] { "right", "DEV", "AD", "VP" } };
			nonTerminalInfo["IP"] = new string[][] { new string[] { "right", "VP", "IP", "NP" } };
			nonTerminalInfo["LCP"] = new string[][] { new string[] { "right", "LCP", "LC" } };
			nonTerminalInfo["LST"] = new string[][] { new string[] { "right", "CD", "NP", "QP" } };
			nonTerminalInfo["NP"] = new string[][] { new string[] { "right", "NP", "NN", "IP", "NR", "NT" } };
			nonTerminalInfo["PP"] = new string[][] { new string[] { "left", "P", "PP" } };
			nonTerminalInfo["PRN"] = new string[][] { new string[] { "left", "PU" } };
			nonTerminalInfo["QP"] = new string[][] { new string[] { "right", "QP", "CLP", "CD" } };
			nonTerminalInfo["UCP"] = new string[][] { new string[] { "left", "IP", "NP", "VP" } };
			nonTerminalInfo["VCD"] = new string[][] { new string[] { "left", "VV", "VA", "VE" } };
			nonTerminalInfo["VP"] = new string[][] { new string[] { "left", "VE", "VC", "VV", "VNV", "VPT", "VRD", "VSB", "VCD", "VP" } };
			nonTerminalInfo["VPT"] = new string[][] { new string[] { "left", "VA", "VV" } };
			nonTerminalInfo["VCP"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VNV"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VRD"] = new string[][] { new string[] { "left", "VV", "VA" } };
			nonTerminalInfo["VSB"] = new string[][] { new string[] { "right", "VV", "VE" } };
			nonTerminalInfo["FRAG"] = new string[][] { new string[] { "right", "VV", "NN" } };
			//FRAG seems only to be used for bits at the beginnings of articles: "Xinwenshe<DATE>" and "(wan)"
			// some POS tags apparently sit where phrases are supposed to be
			nonTerminalInfo["CD"] = new string[][] { new string[] { "right", "CD" } };
			nonTerminalInfo["NN"] = new string[][] { new string[] { "right", "NN" } };
			nonTerminalInfo["NR"] = new string[][] { new string[] { "right", "NR" } };
			// I'm adding these POS tags to do primitive morphology for character-level
			// parsing.  It shouldn't affect anything else because heads of preterminals are not
			// generally queried - GMA
			nonTerminalInfo["VV"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VA"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VC"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VE"] = new string[][] { new string[] { "left" } };
		}
		/* Yue Zhang and Stephen Clark 2008 based their rules on Sun/Jurafsky but changed a few things.
		Constituent Rules
		ADJP r ADJP JJ AD; r
		ADVP r ADVP AD CS JJ NP PP P VA VV; r
		CLP r CLP M NN NP; r
		CP r CP IP VP; r
		DNP r DEG DNP DEC QP; r
		DP r M; l DP DT OD; l
		DVP r DEV AD VP; r
		FRAG r VV NR NN NT; r
		IP r VP IP NP; r
		LCP r LCP LC; r
		LST r CD NP QP; r
		NP r NP NN IP NR NT; r
		NN r NP NN IP NR NT; r
		PP l P PP; l
		PRN l PU; l
		QP r QP CLP CD; r
		UCP l IP NP VP; l
		VCD l VV VA VE; l
		VP l VE VC VV VNV VPT VRD VSB
		VCD VP; l
		VPT l VA VV; l
		VRD l VVI VA; l
		VSB r VV VE; r
		default r
		*/
	}
}

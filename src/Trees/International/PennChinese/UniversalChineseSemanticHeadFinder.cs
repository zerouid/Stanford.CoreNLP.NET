using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>Implements a 'semantic head' variant of the the HeadFinder found in Chinese Head Finder.</summary>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Huihsin Tseng</author>
	/// <author>Percy Liang</author>
	[System.Serializable]
	public class UniversalChineseSemanticHeadFinder : ChineseHeadFinder
	{
		public UniversalChineseSemanticHeadFinder()
			: this(new ChineseTreebankLanguagePack())
		{
		}

		public UniversalChineseSemanticHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			RuleChanges();
		}

		/// <summary>Makes modifications of head finder rules to better fit with semantic notions of heads.</summary>
		private void RuleChanges()
		{
			// Note: removed VC and added NP; copula should not be the head.
			// todo [pengqi 2016]: prioritizing VP over VV works in most cases, but this actually interferes
			//   with xcomps(?) like
			// (VP (VV 继续)
			//     (VP (VC 是)
			//         (NP 重要 的 国际 新闻)
			//     )
			// )
			nonTerminalInfo["VP"] = new string[][] { new string[] { "left", "VP", "VCD", "VSB", "VPT", "VV", "VCP", "VA", "VE", "IP", "VRD", "VNV", "NP" }, leftExceptPunct };
			//nonTerminalInfo.put("CP", new String[][]{{"right", "CP", "IP", "VP"}, rightExceptPunct});
			nonTerminalInfo["CP"] = new string[][] { new string[] { "rightexcept", "DEC", "WHNP", "WHPP", "SP" }, rightExceptPunct };
			nonTerminalInfo["DVP"] = new string[][] { new string[] { "leftdis", "VP", "ADVP" } };
			nonTerminalInfo["LST"] = new string[][] { new string[] { "right", "CD", "NP", "QP", "PU" } };
			nonTerminalInfo["QP"] = new string[][] { new string[] { "right", "QP", "CD", "OD", "NP", "NT", "M", "CLP" } };
			// there's some QP adjunction
			nonTerminalInfo["PP"] = new string[][] { new string[] { "leftexcept", "P" } };
			// Preposition
			nonTerminalInfo["LCP"] = new string[][] { new string[] { "leftexcept", "LC" } };
			// Localizer
			nonTerminalInfo["DNP"] = new string[][] { new string[] { "rightexcept", "DEG", "DEC" } };
		}

		private const long serialVersionUID = 2L;
		// Associative
	}
}

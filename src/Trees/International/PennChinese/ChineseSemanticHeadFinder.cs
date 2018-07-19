using Edu.Stanford.Nlp.Trees;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>Implements a 'semantic head' variant of the the HeadFinder found in Chinese Head Finder</summary>
	/// <author>Pi-Chuan Chang</author>
	/// <author>Huihsin Tseng</author>
	[System.Serializable]
	public class ChineseSemanticHeadFinder : ChineseHeadFinder
	{
		public ChineseSemanticHeadFinder()
			: this(new ChineseTreebankLanguagePack())
		{
		}

		public ChineseSemanticHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			RuleChanges();
		}

		/// <summary>Makes modifications of head finder rules to better fit with semantic notions of heads.</summary>
		private void RuleChanges()
		{
			nonTerminalInfo["VP"] = new string[][] { new string[] { "left", "VP", "VCD", "VPT", "VV", "VCP", "VA", "VE", "VC", "IP", "VSB", "VCP", "VRD", "VNV" }, leftExceptPunct };
			nonTerminalInfo["CP"] = new string[][] { new string[] { "right", "CP", "IP", "VP" }, rightExceptPunct };
			nonTerminalInfo["DNP"] = new string[][] { new string[] { "leftdis", "NP" } };
			nonTerminalInfo["DVP"] = new string[][] { new string[] { "leftdis", "VP", "ADVP" } };
			nonTerminalInfo["LST"] = new string[][] { new string[] { "right", "CD", "NP", "QP", "PU" } };
		}

		private const long serialVersionUID = 2L;
	}
}

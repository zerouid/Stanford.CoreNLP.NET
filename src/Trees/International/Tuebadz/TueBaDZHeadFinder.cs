using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Trees.International.Tuebadz
{
	/// <summary>A HeadFinder for TueBa-D/Z.</summary>
	/// <remarks>
	/// A HeadFinder for TueBa-D/Z.  First version.
	/// <i>Notes:</i> EN_ADD seems to be replaced by ENADD in 2008 ACL German.
	/// Added as alternant by CDM.
	/// </remarks>
	/// <author>Roger Levy (rog@csli.stanford.edu)</author>
	[System.Serializable]
	public class TueBaDZHeadFinder : AbstractCollinsHeadFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Tuebadz.TueBaDZHeadFinder));

		private const long serialVersionUID = 1L;

		private const bool Debug = false;

		private readonly string left;

		private string right;

		private bool coordSwitch = false;

		public TueBaDZHeadFinder()
			: base(new TueBaDZLanguagePack())
		{
			string excluded = tlp.LabelAnnotationIntroducingCharacters().ToString();
			//    if(excluded.indexOf("-") >= 0) {
			excluded = "-" + excluded.ReplaceAll("-", string.Empty);
			// - can only appear at the beginning of a regex character class
			//    }
			headMarkedPattern = Pattern.Compile("^[^" + excluded + "]*:HD");
			headMarkedPattern2 = Pattern.Compile("^[^" + excluded + "]*-HD");
			nonTerminalInfo = Generics.NewHashMap();
			left = (coordSwitch ? "right" : "left");
			right = (coordSwitch ? "left" : "right");
			nonTerminalInfo["VROOT"] = new string[][] { new string[] { left, "SIMPX" }, new string[] { left, "NX" }, new string[] { left, "P" }, new string[] { left, "PX", "ADVX" }, new string[] { left, "EN", "EN_ADD", "ENADD" }, new string[] { left } };
			// we'll arbitrarily choose the leftmost.
			nonTerminalInfo["ROOT"] = new string[][] { new string[] { left, "SIMPX" }, new string[] { left, "NX" }, new string[] { left, "P" }, new string[] { left, "PX", "ADVX" }, new string[] { left, "EN", "EN_ADD", "ENADD" }, new string[] { left } };
			// we'll arbitrarily choose the leftmost.
			nonTerminalInfo["TOP"] = new string[][] { new string[] { left, "SIMPX" }, new string[] { left, "NX" }, new string[] { left, "P" }, new string[] { left, "PX", "ADVX" }, new string[] { left, "EN", "EN_ADD", "ENADD" }, new string[] { left } };
			// we'll arbitrarily choose the leftmost.  Using TOP now for ROOT
			nonTerminalInfo["PX"] = new string[][] { new string[] { left, "APPR", "APPRART", "PX" } };
			nonTerminalInfo["NX"] = new string[][] { new string[] { right, "NX" }, new string[] { right, "NE", "NN" }, new string[] { right, "EN", "EN_ADD", "ENADD", "FX" }, new string[] { right, "ADJX", "PIS", "ADVX" }, new string[] { right, "CARD", "TRUNC"
				 }, new string[] { right } };
			nonTerminalInfo["FX"] = new string[][] { new string[] { right, "FM", "FX" } };
			// junk rule for junk category :)
			nonTerminalInfo["ADJX"] = new string[][] { new string[] { right, "ADJX", "ADJA", "ADJD" }, new string[] { right } };
			nonTerminalInfo["ADVX"] = new string[][] { new string[] { right, "ADVX", "ADV" } };
			// what a nice category!
			nonTerminalInfo["DP"] = new string[][] { new string[] { left } };
			// no need for this really
			nonTerminalInfo["VXFIN"] = new string[][] { new string[] { left, "VXFIN" }, new string[] { right, "VVFIN" } };
			// not sure about left vs. right
			nonTerminalInfo["VXINF"] = new string[][] { new string[] { right, "VXINF" }, new string[] { right, "VVPP", "VVINF" } };
			// not sure about lef vs. right for this one either
			nonTerminalInfo["LV"] = new string[][] { new string[] { right } };
			// no need
			nonTerminalInfo["C"] = new string[][] { new string[] { right, "KOUS" }, new string[] { right, "NX" } };
			// I *think* right makes more sense for this.
			nonTerminalInfo["FKOORD"] = new string[][] { new string[] { left, "LK", "C" }, new string[] { right, "FKONJ", "MF", "VC" } };
			// This one is very tough right/left because it conjoins all sorts of fields together.  Not sure about the right solution
			nonTerminalInfo["KOORD"] = new string[][] { new string[] { left } };
			// no need.
			nonTerminalInfo["LK"] = new string[][] { new string[] { left } };
			// no need.
			// the one for MF is super-bad. MF does not designate a category
			// corresponding to headship. Really, something totally different
			// ought to be done for dependency.
			nonTerminalInfo["MF"] = new string[][] { new string[] { left } };
			nonTerminalInfo["MFE"] = new string[][] { new string[] { left } };
			// no need.
			// NF is pretty bad too, like MF. But it's not nearly so horrible.
			nonTerminalInfo["NF"] = new string[][] { new string[] { left } };
			nonTerminalInfo["PARORD"] = new string[][] { new string[] { left } };
			// no need.
			// not sure what's right here, but it's rare not to have a head marked.
			nonTerminalInfo["VC"] = new string[][] { new string[] { left, "VXINF" } };
			nonTerminalInfo["VF"] = new string[][] { new string[] { left, "NX", "ADJX", "PX", "ADVX", "EN", "SIMPX" } };
			// second dtrs are always punctuation.
			nonTerminalInfo["FKONJ"] = new string[][] { new string[] { left, "LK" }, new string[] { right, "VC" }, new string[] { left, "MF", "NF", "VF" } };
			// these are basically like clauses themselves...the problem is when there's no LK or VC :(
			nonTerminalInfo["DM"] = new string[][] { new string[] { left, "PTKANT" }, new string[] { left, "ITJ" }, new string[] { left, "KON", "FM" }, new string[] { left } };
			nonTerminalInfo["P"] = new string[][] { new string[] { left, "SIMPX" }, new string[] { left } };
			// ***NOTE*** that this is really the P-SIMPX category, but the - will make it stripped to P.
			nonTerminalInfo["PSIMPX"] = new string[][] { new string[] { left, "SIMPX" }, new string[] { left } };
			// ***NOTE*** that this is really the P-SIMPX category, but the - will make it stripped to P.
			nonTerminalInfo["R"] = new string[][] { new string[] { left, "C" }, new string[] { left, "R" }, new string[] { right, "VC" } };
			// ***NOTE*** this is really R-SIMPX.  Also: syntactic head here.  Except for the rare ones that have neither C nor R-SIMPX dtrs.
			nonTerminalInfo["RSIMPX"] = new string[][] { new string[] { left, "C" }, new string[] { left, "RSIMPX" }, new string[] { right, "VC" } };
			// ***NOTE*** this is really R-SIMPX.  Also: syntactic head here.  Except for the rare ones that have neither C nor R-SIMPX dtrs.
			nonTerminalInfo["SIMPX"] = new string[][] { new string[] { left, "LK" }, new string[] { right, "VC" }, new string[] { left, "SIMPX" }, new string[] { left, "C" }, new string[] { right, "FKOORD" }, new string[] { right, "MF" }, new string[] { 
				right } };
			//  syntactic (finite verb) head here.  Note that when there's no LK or VC,the interesting predication tends to be annotated as inside the MF
			nonTerminalInfo["EN"] = new string[][] { new string[] { left, "NX" } };
			// note that this node label starts as EN-ADD but the -ADD will get stripped off.
			nonTerminalInfo["EN_ADD"] = new string[][] { new string[] { left, "NX" }, new string[] { left, "VXINF" } };
			// just in case EN-ADD has been changed to EN_ADD
			nonTerminalInfo["ENADD"] = new string[][] { new string[] { left, "NX" }, new string[] { left, "VXINF" } };
		}

		private readonly Pattern headMarkedPattern;

		private readonly Pattern headMarkedPattern2;

		// just in case EN-ADD has been changed to EN_ADD
		/* Many TueBaDZ local trees have an explicitly marked head, as :HD or -HD.  (Almost!) all the time, there is only one :HD per local tree.  Use it if possible. */
		protected internal override Tree FindMarkedHead(Tree t)
		{
			Tree[] kids = t.Children();
			foreach (Tree kid in kids)
			{
				if (headMarkedPattern.Matcher(kid.Label().Value()).Find() || headMarkedPattern2.Matcher(kid.Label().Value()).Find())
				{
					//log.info("found manually-labeled head " + kids[i] + " for tree " + t);
					return kid;
				}
			}
			return null;
		}

		//Taken from AbstractTreebankLanguage pack b/c we have a slightly different definition of
		//basic category for head finding - we strip grammatical function tags.
		public virtual string BasicCategory(string category)
		{
			if (category == null)
			{
				return null;
			}
			return Sharpen.Runtime.Substring(category, 0, PostBasicCategoryIndex(category));
		}

		private int PostBasicCategoryIndex(string category)
		{
			bool sawAtZero = false;
			char seenAtZero = '\u0000';
			int i = 0;
			for (int leng = category.Length; i < leng; i++)
			{
				char ch = category[i];
				if (IsLabelAnnotationIntroducingCharacter(ch))
				{
					if (i == 0)
					{
						sawAtZero = true;
						seenAtZero = ch;
					}
					else
					{
						if (sawAtZero && ch == seenAtZero)
						{
							sawAtZero = false;
						}
						else
						{
							break;
						}
					}
				}
			}
			return i;
		}

		/// <summary>
		/// Say whether this character is an annotation introducing
		/// character.
		/// </summary>
		/// <param name="ch">The character to check</param>
		/// <returns>Whether it is an annotation introducing character</returns>
		public virtual bool IsLabelAnnotationIntroducingCharacter(char ch)
		{
			if (tlp.IsLabelAnnotationIntroducingCharacter(ch))
			{
				return true;
			}
			//for heads, there's one more char we want to check because we don't care about grammatical fns
			if (ch == '-')
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called by determineHead and may be overridden in subclasses
		/// if special treatment is necessary for particular categories.
		/// </summary>
		protected internal override Tree DetermineNonTrivialHead(Tree t, Tree parent)
		{
			Tree theHead = null;
			string motherCat = BasicCategory(t.Label().Value());
			// We know we have nonterminals underneath
			// (a bit of a Penn Treebank assumption, but).
			//   Look at label.
			string[][] how = nonTerminalInfo[motherCat];
			if (how == null)
			{
				if (defaultRule != null)
				{
					return TraverseLocate(t.Children(), defaultRule, true);
				}
				else
				{
					return null;
				}
			}
			for (int i = 0; i < how.Length; i++)
			{
				bool deflt = (i == how.Length - 1);
				theHead = TraverseLocate(t.Children(), how[i], deflt);
				if (theHead != null)
				{
					break;
				}
			}
			return theHead;
		}
	}
}

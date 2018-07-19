using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Negra
{
	/// <summary>HeadFinder for the Negra Treebank.</summary>
	/// <remarks>
	/// HeadFinder for the Negra Treebank.  Adapted from
	/// CollinsHeadFinder.
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class NegraHeadFinder : AbstractCollinsHeadFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Negra.NegraHeadFinder));

		private const long serialVersionUID = -7253035927065152766L;

		private const bool Debug = false;

		/// <summary>Vends a "semantic" NegraHeadFinder---one that disprefers modal/auxiliary verbs as the heads of S or VP.</summary>
		/// <returns>a NegraHeadFinder that uses a "semantic" head-finding rule for the S category.</returns>
		public static IHeadFinder NegraSemanticHeadFinder()
		{
			Edu.Stanford.Nlp.Trees.International.Negra.NegraHeadFinder result = new Edu.Stanford.Nlp.Trees.International.Negra.NegraHeadFinder();
			result.nonTerminalInfo["S"] = new string[][] { new string[] { result.right, "VVFIN", "VVIMP" }, new string[] { "right", "VP", "CVP" }, new string[] { "right", "VMFIN", "VAFIN", "VAIMP" }, new string[] { "right", "S", "CS" } };
			result.nonTerminalInfo["VP"] = new string[][] { new string[] { "right", "VVINF", "VVIZU", "VVPP" }, new string[] { result.right, "VZ", "VAINF", "VMINF", "VMPP", "VAPP", "PP" } };
			result.nonTerminalInfo["VZ"] = new string[][] { new string[] { result.right, "VVINF", "VAINF", "VMINF", "VVFIN", "VVIZU" } };
			// note that VZ < VVIZU is very rare, maybe shouldn't even exist.
			return result;
		}

		private bool coordSwitch = false;

		public NegraHeadFinder()
			: this(new NegraPennLanguagePack())
		{
		}

		internal string left;

		internal string right;

		public NegraHeadFinder(ITreebankLanguagePack tlp)
			: base(tlp)
		{
			nonTerminalInfo = Generics.NewHashMap();
			left = (coordSwitch ? "right" : "left");
			right = (coordSwitch ? "left" : "right");
			/* BEGIN ROGER TODO */
			//
			//    // some special rule for S
			//    if(motherCat.equals("S") && kids[0].label().value().equals("PRELS"))
			//return kids[0];
			//
			nonTerminalInfo["S"] = new string[][] { new string[] { left, "PRELS" } };
			/* END ROGER TODO */
			// these are first-cut rules
			// there are non-unary nodes I put in
			nonTerminalInfo["NUR"] = new string[][] { new string[] { left, "S" } };
			// root -- yuk
			nonTerminalInfo["ROOT"] = new string[][] { new string[] { left, "S", "CS", "VP", "CVP", "NP", "XY", "CNP", "DL", "AVP", "CAVP", "PN", "AP", "PP", "CO", "NN", "NE", "CPP", "CARD", "CH" } };
			// in case a user's treebank has TOP instead of ROOT or unlabeled
			nonTerminalInfo["TOP"] = new string[][] { new string[] { left, "S", "CS", "VP", "CVP", "NP", "XY", "CNP", "DL", "AVP", "CAVP", "PN", "AP", "PP", "CO", "NN", "NE", "CPP", "CARD", "CH" } };
			// Major syntactic categories -- in order appearing in negra.export
			nonTerminalInfo["NP"] = new string[][] { new string[] { right, "NN", "NE", "MPN", "NP", "CNP", "PN", "CAR" } };
			// Basic heads are NN/NE/NP; CNP is coordination; CAR is cardinal
			nonTerminalInfo["AP"] = new string[][] { new string[] { right, "ADJD", "ADJA", "CAP", "AA", "ADV" } };
			// there is one ADJP unary rewrite to AD but otherwise all have JJ or ADJP
			nonTerminalInfo["PP"] = new string[][] { new string[] { left, "KOKOM", "APPR", "PROAV" } };
			//nonTerminalInfo.put("S", new String[][] {{right, "S","CS","NP"}}); //Most of the time, S has its head explicitly marked.  CS is coordinated sentence.  I don't fully understand the rest of "non-headed" german sentences to say much.
			nonTerminalInfo["S"] = new string[][] { new string[] { right, "VMFIN", "VVFIN", "VAFIN", "VVIMP", "VAIMP" }, new string[] { "right", "VP", "CVP" }, new string[] { "right", "S", "CS" } };
			// let finite verbs (including imperatives) be head always.
			nonTerminalInfo["VP"] = new string[][] { new string[] { right, "VZ", "VAINF", "VMINF", "VVINF", "VVIZU", "VVPP", "VMPP", "VAPP", "PP" } };
			// VP usually has explicit head marking; there's lots of garbage here to sort out, though.
			nonTerminalInfo["VZ"] = new string[][] { new string[] { left, "PRTZU", "APPR", "PTKZU" } };
			// we could also try using the verb (on the right) instead of ZU as the head, maybe this would make more sense...
			nonTerminalInfo["CO"] = new string[][] { new string[] { left } };
			// this is an unlike coordination
			nonTerminalInfo["AVP"] = new string[][] { new string[] { right, "ADV", "AVP", "ADJD", "PROAV", "PP" } };
			nonTerminalInfo["AA"] = new string[][] { new string[] { right, "ADJD", "ADJA" } };
			// superlative adjective phrase with "am"; I'm using the adjective not the "am" marker
			nonTerminalInfo["CNP"] = new string[][] { new string[] { right, "NN", "NE", "MPN", "NP", "CNP", "PN", "CAR" } };
			nonTerminalInfo["CAP"] = new string[][] { new string[] { right, "ADJD", "ADJA", "CAP", "AA", "ADV" } };
			nonTerminalInfo["CPP"] = new string[][] { new string[] { right, "APPR", "PROAV", "PP", "CPP" } };
			nonTerminalInfo["CS"] = new string[][] { new string[] { right, "S", "CS" } };
			nonTerminalInfo["CVP"] = new string[][] { new string[] { right, "VP", "CVP" } };
			// covers all examples
			nonTerminalInfo["CVZ"] = new string[][] { new string[] { right, "VZ" } };
			// covers all examples
			nonTerminalInfo["CAVP"] = new string[][] { new string[] { right, "ADV", "AVP", "ADJD", "PWAV", "APPR", "PTKVZ" } };
			nonTerminalInfo["MPN"] = new string[][] { new string[] { right, "NE", "FM", "CARD" } };
			//presumably left/right doesn't matter
			nonTerminalInfo["NM"] = new string[][] { new string[] { right, "CARD", "NN" } };
			// covers all examples
			nonTerminalInfo["CAC"] = new string[][] { new string[] { right, "APPR", "AVP" } };
			//covers all examples
			nonTerminalInfo["CH"] = new string[][] { new string[] { right } };
			nonTerminalInfo["MTA"] = new string[][] { new string[] { right, "ADJA", "ADJD", "NN" } };
			nonTerminalInfo["CCP"] = new string[][] { new string[] { right, "AVP" } };
			nonTerminalInfo["DL"] = new string[][] { new string[] { left } };
			// don't understand this one yet
			nonTerminalInfo["ISU"] = new string[][] { new string[] { right } };
			// idioms, I think
			nonTerminalInfo["QL"] = new string[][] { new string[] { right } };
			// these are all complicated numerical expressions I think
			nonTerminalInfo["--"] = new string[][] { new string[] { right, "PP" } };
			// a garbage conjoined phrase appearing once
			// some POS tags apparently sit where phrases are supposed to be
			nonTerminalInfo["CD"] = new string[][] { new string[] { right, "CD" } };
			nonTerminalInfo["NN"] = new string[][] { new string[] { right, "NN" } };
			nonTerminalInfo["NR"] = new string[][] { new string[] { right, "NR" } };
		}

		/* Some Negra local trees have an explicitly marked head.  Use it if
		* possible. */
		protected internal virtual Tree FindMarkedHead(Tree[] kids)
		{
			foreach (Tree kid in kids)
			{
				if (kid.Label() is NegraLabel && ((NegraLabel)kid.Label()).GetEdge() != null && ((NegraLabel)kid.Label()).GetEdge().Equals("HD"))
				{
					//log.info("found manually-labeled head");
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
			char[] cutChars = tlp.LabelAnnotationIntroducingCharacters();
			foreach (char cutChar in cutChars)
			{
				if (ch == cutChar)
				{
					return true;
				}
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
			if (motherCat.StartsWith("@"))
			{
				motherCat = Sharpen.Runtime.Substring(motherCat, 1);
			}
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

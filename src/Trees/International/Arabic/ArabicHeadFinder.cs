using System;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Trees.International.Arabic
{
	/// <summary>
	/// Find the head of an Arabic tree, using the usual kind of heuristic
	/// head finding rules.
	/// </summary>
	/// <remarks>
	/// Find the head of an Arabic tree, using the usual kind of heuristic
	/// head finding rules.
	/// <p>
	/// <i>Implementation notes.</i>
	/// TO DO: make sure that -PRD marked elements are always chosen as heads.
	/// (Has this now been successfully done or not??)
	/// <p>
	/// Mona: I added the 8 new Nonterm for the merged DT with its following
	/// category as a rule the DT nonterm is right headed, the 8 new nonterm DTs
	/// are: DTCD, DTRB, DTRP, DTJJ, DTNN, DTNNS, DTNNP, DTNNPS.
	/// This was added Dec 7th, 2004.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Mona Diab</author>
	/// <author>Christopher Manning (added new stuff for ATBp3v3</author>
	[System.Serializable]
	public class ArabicHeadFinder : AbstractCollinsHeadFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Arabic.ArabicHeadFinder));

		private const long serialVersionUID = 6203368998430280740L;

		protected internal ArabicHeadFinder.TagSet tagSet;

		[System.Serializable]
		public sealed class TagSet
		{
			public static readonly ArabicHeadFinder.TagSet BiesCollapsed = new ArabicHeadFinder.TagSet();

			public static readonly ArabicHeadFinder.TagSet Original = new ArabicHeadFinder.TagSet();

			/* A work in progress. There may well be a better way to parameterize the HeadFinders via tagset. */
			// really there should be several here.
			// really there should be several here; major point is that the det part is ignored completely
			internal abstract string Prep();

			internal abstract string Noun();

			internal abstract string Adj();

			internal abstract string Det();

			internal abstract string DetPlusNoun();

			internal abstract ITreebankLanguagePack LangPack();

			internal static ArabicHeadFinder.TagSet TagSet(string str)
			{
				switch (str)
				{
					case "BIES_COLLAPSED":
					{
						return ArabicHeadFinder.TagSet.BiesCollapsed;
					}

					case "ORIGINAL":
					{
						return ArabicHeadFinder.TagSet.Original;
					}

					default:
					{
						throw new ArgumentException("Don't know anything about tagset " + str);
					}
				}
			}
		}

		public ArabicHeadFinder()
			: this(new ArabicTreebankLanguagePack())
		{
		}

		/// <summary>Construct an ArabicHeadFinder with a String parameter corresponding to the tagset in use.</summary>
		/// <param name="tagSet">Either "ORIGINAL" or "BIES_COLLAPSED"</param>
		public ArabicHeadFinder(string tagSet)
			: this(ArabicHeadFinder.TagSet.TagSet(tagSet))
		{
		}

		public ArabicHeadFinder(ArabicHeadFinder.TagSet tagSet)
			: this(tagSet.LangPack(), tagSet)
		{
		}

		public ArabicHeadFinder(ITreebankLanguagePack tlp)
			: this(tlp, ArabicHeadFinder.TagSet.BiesCollapsed)
		{
		}

		protected internal ArabicHeadFinder(ITreebankLanguagePack tlp, ArabicHeadFinder.TagSet tagSet)
			: base(tlp)
		{
			//this(new ArabicTreebankLanguagePack(), tagSet);
			this.tagSet = tagSet;
			//log.info("##testing: noun tag is " + tagSet.noun());
			nonTerminalInfo = Generics.NewHashMap();
			nonTerminalInfo["NX"] = new string[][] { new string[] { "left", "DT", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTJJ", "DTNOUN_QUANT", "NOUN_QUANT", "MWNP" } };
			nonTerminalInfo["ADJP"] = new string[][] { new string[] { "rightdis", tagSet.Adj(), "DTJJ", "ADJ_NUM", "DTADJ_NUM", "JJR", "DTJJR", "MWADJP" }, new string[] { "right", "ADJP", "VN", tagSet.Noun(), "MWNP", "NNP", "NNPS", "NNS", "DTNN", "DTNNS"
				, "DTNNP", "DTNNPS", "DTJJ", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "right", "RB", "MWADVP", "CD", "DTRB", "DTCD" }, new string[] { "right", "DT" } };
			// sometimes right, sometimes left headed??
			nonTerminalInfo["MWADJP"] = new string[][] { new string[] { "rightdis", tagSet.Adj(), "DTJJ", "ADJ_NUM", "DTADJ_NUM", "JJR", "DTJJR" }, new string[] { "right", tagSet.Noun(), "MWNP", "NNP", "NNPS", "NNS", "DTNN", "DTNNS", "DTNNP", "DTNNPS", 
				"DTJJ", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "right", "RB", "MWADVP", "CD", "DTRB", "DTCD" }, new string[] { "right", "DT" } };
			// sometimes right, sometimes left headed??
			nonTerminalInfo["ADVP"] = new string[][] { new string[] { "left", "WRB", "RB", "MWADVP", "ADVP", "WHADVP", "DTRB" }, new string[] { "left", "CD", "RP", tagSet.Noun(), "MWNP", "CC", "MWCONJP", tagSet.Adj(), "MWADJP", "DTJJ", "ADJ_NUM", "DTADJ_NUM"
				, "IN", "MWPP", "NP", "NNP", "NOFUNC", "DTRP", "DTNN", "DTNNP", "DTNNPS", "DTNNS", "DTJJ", "DTNOUN_QUANT", "NOUN_QUANT" } };
			// NNP is a gerund that they called an unknown (=NNP, believe it or not...)
			nonTerminalInfo["MWADVP"] = new string[][] { new string[] { "left", "WRB", "RB", "ADVP", "WHADVP", "DTRB" }, new string[] { "left", "CD", "RP", tagSet.Noun(), "MWNP", "CC", "MWCONJP", tagSet.Adj(), "MWADJP", "DTJJ", "ADJ_NUM", "DTADJ_NUM", "IN"
				, "MWPP", "NP", "NNP", "NOFUNC", "DTRP", "DTNN", "DTNNP", "DTNNPS", "DTNNS", "DTJJ", "DTNOUN_QUANT", "NOUN_QUANT" } };
			// NNP is a gerund that they called an unknown (=NNP, believe it or not...)
			nonTerminalInfo["CONJP"] = new string[][] { new string[] { "right", "IN", "RB", "MWADVP", tagSet.Noun(), "MWNP", "NNS", "NNP", "NNPS", "DTRB", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" } };
			nonTerminalInfo["MWCONJP"] = new string[][] { new string[] { "right", "IN", "RB", "MWADVP", tagSet.Noun(), "MWNP", "NNS", "NNP", "NNPS", "DTRB", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" } };
			nonTerminalInfo["FRAG"] = new string[][] { new string[] { "left", tagSet.Noun(), "MWNP", "NNPS", "NNP", "NNS", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "left", "VBP" } };
			nonTerminalInfo["MWFRAG"] = new string[][] { new string[] { "left", tagSet.Noun(), "MWNP", "NNPS", "NNP", "NNS", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "left", "VBP" } };
			nonTerminalInfo["INTJ"] = new string[][] { new string[] { "left", "RP", "UH", "DTRP" } };
			nonTerminalInfo["LST"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["NAC"] = new string[][] { new string[] { "left", "NP", "SBAR", "PP", "MWP", "ADJP", "S", "PRT", "UCP" }, new string[] { "left", "ADVP" } };
			// note: maybe CC, RB should be the heads?
			nonTerminalInfo["NP"] = new string[][] { new string[] { "left", tagSet.Noun(), "MWNP", tagSet.DetPlusNoun(), "NNS", "NNP", "NNPS", "NP", "PRP", "WHNP", "QP", "WP", "DTNNS", "DTNNPS", "DTNNP", "NOFUNC", "NO_FUNC", "DTNOUN_QUANT", "NOUN_QUANT"
				 }, new string[] { "left", tagSet.Adj(), "MWADJP", "DTJJ", "JJR", "DTJJR", "ADJ_NUM", "DTADJ_NUM" }, new string[] { "right", "CD", "DTCD" }, new string[] { "left", "PRP$" }, new string[] { "right", "DT" } };
			// should the JJ rule be left or right?
			nonTerminalInfo["MWNP"] = new string[][] { new string[] { "left", tagSet.Noun(), "MWNP", tagSet.DetPlusNoun(), "NNS", "NNP", "NNPS", "PRP", "QP", "WP", "DTNNS", "DTNNPS", "DTNNP", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "left", tagSet
				.Adj(), "MWADJP", "DTJJ", "JJR", "DTJJR", "ADJ_NUM", "DTADJ_NUM" }, new string[] { "right", "CD", "DTCD" }, new string[] { "left", "PRP$" }, new string[] { "right", "DT" } };
			// should the JJ rule be left or right?
			nonTerminalInfo["PP"] = new string[][] { new string[] { "left", tagSet.Prep(), "MWPP", "PP", "MWP", "PRT", "X" }, new string[] { "left", "NNP", "RP", tagSet.Noun(), "MWNP" }, new string[] { "left", "NP" } };
			// NN is for a mistaken "fy", and many wsT
			nonTerminalInfo["MWPP"] = new string[][] { new string[] { "left", tagSet.Prep(), "PP", "MWP", "PRT", "X" }, new string[] { "left", "NNP", "RP", tagSet.Noun(), "MWNP" }, new string[] { "left", "NP" } };
			// NN is for a mistaken "fy", and many wsT
			nonTerminalInfo["PRN"] = new string[][] { new string[] { "left", "NP" } };
			// don't get PUNC
			nonTerminalInfo["MWPRN"] = new string[][] { new string[] { "left", "IN" } };
			// don't get PUNC
			nonTerminalInfo["PRT"] = new string[][] { new string[] { "left", "RP", "PRT", "IN", "DTRP" } };
			nonTerminalInfo["QP"] = new string[][] { new string[] { "right", "CD", "DTCD", tagSet.Noun(), "MWNP", tagSet.Adj(), "MWADJP", "NNS", "NNP", "NNPS", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTJJ", "DTNOUN_QUANT", "NOUN_QUANT" } };
			nonTerminalInfo["S"] = new string[][] { new string[] { "left", "VP", "MWVP", "S" }, new string[] { "right", "PP", "MWP", "ADVP", "SBAR", "UCP", "ADJP" } };
			// really important to put in -PRD sensitivity here!
			nonTerminalInfo["MWS"] = new string[][] { new string[] { "left", "VP", "MWVP", "S" }, new string[] { "right", "PP", "MWP", "ADVP", "SBAR", "UCP", "ADJP" } };
			// really important to put in -PRD sensitivity here!
			nonTerminalInfo["SQ"] = new string[][] { new string[] { "left", "VP", "MWVP", "PP", "MWP" } };
			// to be principled, we need -PRD sensitivity here too.
			nonTerminalInfo["SBAR"] = new string[][] { new string[] { "left", "WHNP", "WHADVP", "WRB", "RP", "IN", "SBAR", "CC", "MWCONJP", "WP", "WHPP", "ADVP", "PRT", "RB", "MWADVP", "X", "DTRB", "DTRP" }, new string[] { "left", tagSet.Noun(), "MWNP", 
				"NNP", "NNS", "NNPS", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "left", "S" } };
			nonTerminalInfo["MWSBAR"] = new string[][] { new string[] { "left", "WHNP", "WHADVP", "WRB", "RP", "IN", "SBAR", "CC", "MWCONJP", "WP", "WHPP", "ADVP", "PRT", "RB", "MWADVP", "X", "DTRB", "DTRP" }, new string[] { "left", tagSet.Noun(), "MWNP"
				, "NNP", "NNS", "NNPS", "DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "left", "S" } };
			nonTerminalInfo["SBARQ"] = new string[][] { new string[] { "left", "WHNP", "WHADVP", "RP", "IN", "SBAR", "CC", "MWCONJP", "WP", "WHPP", "ADVP", "PRT", "RB", "MWADVP", "X" }, new string[] { "left", tagSet.Noun(), "MWNP", "NNP", "NNS", "NNPS", 
				"DTNN", "DTNNS", "DTNNP", "DTNNPS", "DTNOUN_QUANT", "NOUN_QUANT" }, new string[] { "left", "S" } };
			// copied from SBAR rule -- look more closely when there's time
			nonTerminalInfo["UCP"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo["VP"] = new string[][] { new string[] { "left", "VBD", "VBN", "VBP", "VBG", "DTVBG", "VN", "DTVN", "VP", "RB", "MWADVP", "X", "VB" }, new string[] { "left", "IN" }, new string[] { "left", "NNP", tagSet.Noun(), "MWNP", "DTNN", 
				"DTNNP", "DTNNPS", "DTNNS", "DTNOUN_QUANT", "NOUN_QUANT" } };
			// exclude RP because we don't want negation markers as heads -- no useful information?
			nonTerminalInfo["MWVP"] = new string[][] { new string[] { "left", "VBD", "VBN", "VBP", "VBG", "DTVBG", "VN", "DTVN", "VP", "MWVP", "RB", "MWADVP", "X", "VB" }, new string[] { "left", "IN" }, new string[] { "left", "NNP", tagSet.Noun(), "MWNP"
				, "DTNN", "DTNNP", "DTNNPS", "DTNNS", "DTNOUN_QUANT", "NOUN_QUANT" } };
			// exclude RP because we don't want negation markers as heads -- no useful information?
			//also, RB is used as gerunds
			nonTerminalInfo["WHADVP"] = new string[][] { new string[] { "left", "WRB", "WP" }, new string[] { "right", "CC", "MWCONJP" }, new string[] { "left", "IN" } };
			nonTerminalInfo["WHNP"] = new string[][] { new string[] { "right", "WP" } };
			nonTerminalInfo["WHPP"] = new string[][] { new string[] { "left", "IN", "MWPP", "RB", "MWADVP" } };
			nonTerminalInfo["X"] = new string[][] { new string[] { "left" } };
			//Added by Mona 12/7/04 for the newly created DT nonterm cat
			nonTerminalInfo["DTNN"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTNNS"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTNNP"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTNNPS"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTJJ"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTRP"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTRB"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTCD"] = new string[][] { new string[] { "right" } };
			nonTerminalInfo["DTIN"] = new string[][] { new string[] { "right" } };
			// stand-in dependency:
			nonTerminalInfo["EDITED"] = new string[][] { new string[] { "left" } };
			nonTerminalInfo[tlp.StartSymbol()] = new string[][] { new string[] { "left" } };
			// one stray SINV in the training set...garbage head rule here.
			nonTerminalInfo["SINV"] = new string[][] { new string[] { "left", "ADJP", "VP" } };
		}

		private readonly Pattern predPattern = Pattern.Compile(".*-PRD$");

		/// <summary>Predicatively marked elements in a sentence should be noted as heads</summary>
		protected internal override Tree FindMarkedHead(Tree t)
		{
			string cat = t.Value();
			if (cat.Equals("S"))
			{
				Tree[] kids = t.Children();
				foreach (Tree kid in kids)
				{
					if (predPattern.Matcher(kid.Value()).Matches())
					{
						return kid;
					}
				}
			}
			return null;
		}
	}
}

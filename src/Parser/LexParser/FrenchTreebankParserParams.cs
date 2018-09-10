using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.French;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>TreebankLangParserParams for the French Treebank corpus.</summary>
	/// <remarks>
	/// TreebankLangParserParams for the French Treebank corpus. This package assumes that the FTB
	/// has been transformed into PTB-format trees encoded in UTF-8. The "-xmlFormat" option can
	/// be used to read the raw FTB trees.
	/// </remarks>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class FrenchTreebankParserParams : TregexPoweredTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.FrenchTreebankParserParams));

		private const long serialVersionUID = -6976724734594763986L;

		private readonly StringBuilder optionsString;

		private IHeadFinder headFinder;

		private bool readPennFormat = true;

		private bool collinizerRetainsPunctuation = false;

		private TwoDimensionalCounter<string, string> mwCounter;

		private MorphoFeatureSpecification morphoSpec;

		private MorphoFeatureSpecification tagSpec;

		public FrenchTreebankParserParams()
			: base(new FrenchTreebankLanguagePack())
		{
			//The treebank is distributed in XML format.
			//Use -xmlFormat below to enable reading the raw files.
			//Controls the MW annotation feature
			// For adding the CC tagset as annotations.
			SetInputEncoding("UTF-8");
			optionsString = new StringBuilder();
			optionsString.Append("FrenchTreebankParserParams\n");
			InitializeAnnotationPatterns();
		}

		/// <summary>Features which should be enabled by default.</summary>
		protected internal override string[] BaselineAnnotationFeatures()
		{
			return new string[0];
		}

		/// <summary>Features to enable for the factored parser</summary>
		private static readonly string[] factoredFeatures = new string[] { "-tagPAFr", "-markInf", "-markPart", "-markVN", "-coord1", "-de2", "-markP1", "-MWAdvS", "-MWADVSel1", "-MWADVSel2", "-MWNSel1", "-MWNSel2", "-splitPUNC" };

		//MWE features...don't help overall parsing, but help MWE categories
		// New features for CL submission
		private void InitializeAnnotationPatterns()
		{
			// Incremental delta improvements are over the previous feature (dev set, <= 40)
			//
			// POS Splitting for verbs
			annotations["-markInf"] = new Pair("@V > (@VN > @VPinf)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-infinitive"));
			annotations["-markPart"] = new Pair("@V > (@VN > @VPpart)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-participle"));
			annotations["-markVN"] = new Pair("__ << @VN", new TregexPoweredTreebankParserParams.SimpleStringFunction("-withVN"));
			// +1.45 F1  (Helps MWEs significantly)
			annotations["-tagPAFr"] = new Pair("!@PUNC < (__ !< __) > __=parent", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "parent", true));
			// +.14 F1
			annotations["-coord1"] = new Pair("@COORD <2 __=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", true));
			// +.70 F1 -- de c-commands other stuff dominated by NP, PP, and COORD
			annotations["-de2"] = new Pair("@P < /^([Dd]es?|du|d')$/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-de2"));
			annotations["-de3"] = new Pair("@NP|PP|COORD >+(@NP|PP) (@PP <, (@P < /^([Dd]es?|du|d')$/))", new TregexPoweredTreebankParserParams.SimpleStringFunction("-de3"));
			// +.31 F1
			annotations["-markP1"] = new Pair("@P > (@PP > @NP)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-n"));
			//MWEs
			//(for MWADV 75.92 -> 77.16)
			annotations["-MWAdvS"] = new Pair("@MWADV > /S/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwadv-s"));
			annotations["-MWADVSel1"] = new Pair("@MWADV <1 @P <2 @N !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwadv1"));
			annotations["-MWADVSel2"] = new Pair("@MWADV <1 @P <2 @D <3 @N !<4 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwadv2"));
			annotations["-MWNSel1"] = new Pair("@MWN <1 @N <2 @A !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwn1"));
			annotations["-MWNSel2"] = new Pair("@MWN <1 @N <2 @P <3 @N !<4 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwn2"));
			annotations["-MWNSel3"] = new Pair("@MWN <1 @N <2 @- <3 @N !<4 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwn3"));
			annotations["-splitPUNC"] = new Pair("@PUNC < __=" + FrenchTreebankParserParams.AnnotatePunctuationFunction.key, new FrenchTreebankParserParams.AnnotatePunctuationFunction());
			// Mark MWE tags only
			annotations["-mweTag"] = new Pair("!@PUNC < (__ !< __) > /MW/=parent", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "parent", true));
			annotations["-sq"] = new Pair("@SENT << /\\?/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-Q"));
			//New phrasal splits
			annotations["-hasVP"] = new Pair("!@ROOT|SENT << /^VP/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasVP"));
			annotations["-hasVP2"] = new Pair("__ << /^VP/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasVP"));
			annotations["-npCOORD"] = new Pair("@NP < @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-coord"));
			annotations["-npVP"] = new Pair("@NP < /VP/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-vp"));
			//NPs
			annotations["-baseNP1"] = new Pair("@NP <1 @D <2 @N !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-np1"));
			annotations["-baseNP2"] = new Pair("@NP <1 @D <2 @MWN !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-np2"));
			annotations["-baseNP3"] = new Pair("@NP <1 @MWD <2 @N !<3 __ ", new TregexPoweredTreebankParserParams.SimpleStringFunction("-np3"));
			//MWEs
			annotations["-npMWN1"] = new Pair("@NP < (@MWN < @A)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwna"));
			annotations["-npMWN2"] = new Pair("@NP <1 @D <2 @MWN <3 @PP !<4 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwn2"));
			annotations["-npMWN3"] = new Pair("@NP <1 @D <2 (@MWN <1 @N <2 @A !<3 __) !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwn3"));
			annotations["-npMWN4"] = new Pair("@PP <, @P <2 (@NP <1 @D <2 (@MWN <1 @N <2 @A !<3 __) !<3 __) !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwn3"));
			//The whopper....
			annotations["-MWNSel"] = new Pair("@MWN", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 600, true));
			annotations["-MWADVSel"] = new Pair("@MWADV", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 500, true));
			annotations["-MWASel"] = new Pair("@MWA", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 100, true));
			annotations["-MWCSel"] = new Pair("@MWC", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 400, true));
			annotations["-MWDSel"] = new Pair("@MWD", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 100, true));
			annotations["-MWPSel"] = new Pair("@MWP", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 600, true));
			annotations["-MWPROSel"] = new Pair("@MWPRO", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 60, true));
			annotations["-MWVSel"] = new Pair("@MWV", new FrenchTreebankParserParams.AddPOSSequenceFunction(this, "-", 200, true));
			//MWN
			annotations["-mwn1"] = new Pair("@MWN <1 @N <2 @A !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-na"));
			annotations["-mwn2"] = new Pair("@MWN <1 @N <2 @P <3 @N !<4 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-npn"));
			annotations["-mwn3"] = new Pair("@MWN <1 @N <2 @- <3 @N !<4 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-n-n"));
			annotations["-mwn4"] = new Pair("@MWN <1 @N <2 @N !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-nn"));
			annotations["-mwn5"] = new Pair("@MWN <1 @D <2 @N !<3 __", new TregexPoweredTreebankParserParams.SimpleStringFunction("-dn"));
			//wh words
			annotations["-hasWH"] = new Pair("__ < /^(qui|quoi|comment|quel|quelle|quels|quelles|où|combien|que|pourquoi|quand)$/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-wh"));
			//POS splitting
			annotations["-markNNP2"] = new Pair("@N < /^[A-Z]/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-nnp"));
			annotations["-markD1"] = new Pair("@D > (__ > @PP)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-p"));
			annotations["-markD2"] = new Pair("@D > (__ > @NP)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-n"));
			annotations["-markD3"] = new Pair("@D > (__ > /^VP/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-v"));
			annotations["-markD4"] = new Pair("@D > (__ > /^S/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-s"));
			annotations["-markD5"] = new Pair("@D > (__ > @COORD)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-c"));
			//Appositives?
			annotations["-app1"] = new Pair("@NP < /[,]/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-app1"));
			annotations["-app2"] = new Pair("/[^,\\-:;\"]/ > (@NP < /^[,]$/) $,, /^[,]$/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-app2"));
			//COORD
			annotations["-coord2"] = new Pair("@COORD !< @C", new TregexPoweredTreebankParserParams.SimpleStringFunction("-nonC"));
			annotations["-hasCOORD"] = new Pair("__ < @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasCOORD"));
			annotations["-hasCOORDLS"] = new Pair("@SENT <, @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasCOORDLS"));
			annotations["-hasCOORDNonS"] = new Pair("__ < @COORD !<, @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasCOORDNonS"));
			// PP / VPInf
			annotations["-pp1"] = new Pair("@P < /^(du|des|au|aux)$/=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", false));
			annotations["-vpinf1"] = new Pair("@VPinf <, __=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", false));
			annotations["-vpinf2"] = new Pair("@VPinf <, __=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", true));
			// PP splitting (subsumed by the de2-3 features)
			annotations["-splitIN"] = new Pair("@PP <, (P < /^([Dd]e|[Dd]'|[Dd]es|[Dd]u|à|[Aa]u|[Aa]ux|[Ee]n|[Dd]ans|[Pp]ar|[Ss]ur|[Pp]our|[Aa]vec|[Ee]ntre)$/=word)", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", false, true)
				);
			annotations["-splitP"] = new Pair("@P < /^([Dd]e|[Dd]'|[Dd]es|[Dd]u|à|[Aa]u|[Aa]ux|[Ee]n|[Dd]ans|[Pp]ar|[Ss]ur|[Pp]our|[Aa]vec|[Ee]ntre)$/=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", false, true));
			//de features
			annotations["-hasde"] = new Pair("@NP|PP <+(@NP|PP) (P < de)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasDE"));
			annotations["-hasde2"] = new Pair("@PP < de", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasDE2"));
			//NPs
			annotations["-np1"] = new Pair("@NP < /^,$/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-np1"));
			annotations["-np2"] = new Pair("@NP <, (@D < le|la|les)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-np2"));
			annotations["-np3"] = new Pair("@D < le|la|les", new TregexPoweredTreebankParserParams.SimpleStringFunction("-def"));
			annotations["-baseNP"] = new Pair("@NP <, @D <- (@N , @D)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-baseNP"));
			// PP environment
			annotations["-markP2"] = new Pair("@P > (@PP > @AP)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-a"));
			annotations["-markP3"] = new Pair("@P > (@PP > @SENT|Ssub|VPinf|VPpart)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-v"));
			annotations["-markP4"] = new Pair("@P > (@PP > @Srel)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-r"));
			annotations["-markP5"] = new Pair("@P > (@PP > @COORD)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-c"));
			annotations["-markP6"] = new Pair("@P > @VPinf", new TregexPoweredTreebankParserParams.SimpleStringFunction("-b"));
			annotations["-markP7"] = new Pair("@P > @VPpart", new TregexPoweredTreebankParserParams.SimpleStringFunction("-b"));
			annotations["-markP8"] = new Pair("@P > /^MW|NP/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-internal"));
			annotations["-markP9"] = new Pair("@P > @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-c"));
			//MWEs
			annotations["-hasMWP"] = new Pair("!/S/ < @MWP", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwp"));
			annotations["-hasMWP2"] = new Pair("@PP < @MWP", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwp2"));
			annotations["-hasMWN2"] = new Pair("@PP <+(@NP) @MWN", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasMWN2"));
			annotations["-hasMWN3"] = new Pair("@NP < @MWN", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasMWN3"));
			annotations["-hasMWADV"] = new Pair("/^A/ < @MWADV", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasmwadv"));
			annotations["-hasC1"] = new Pair("__ < @MWC", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasc1"));
			annotations["-hasC2"] = new Pair("@MWC > /S/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasc2"));
			annotations["-hasC3"] = new Pair("@COORD < @MWC", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasc3"));
			annotations["-uMWN"] = new Pair("@NP <: @MWN", new TregexPoweredTreebankParserParams.SimpleStringFunction("-umwn"));
			//POS splitting
			annotations["-splitC"] = new Pair("@C < __=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", false));
			annotations["-splitD"] = new Pair("@D < /^[^\\d+]{1,4}$/=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word", false));
			annotations["-de1"] = new Pair("@D < /^([Dd]es?|du|d')$/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-de1"));
			annotations["-markNNP1"] = new Pair("@NP < (N < /^[A-Z]/) !< /^[^NA]/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-nnp"));
			//PP environment
			annotations["-markPP1"] = new Pair("@PP > @NP", new TregexPoweredTreebankParserParams.SimpleStringFunction("-n"));
			annotations["-markPP2"] = new Pair("@PP > @AP", new TregexPoweredTreebankParserParams.SimpleStringFunction("-a"));
			annotations["-markPP3"] = new Pair("@PP > @SENT|Ssub|VPinf|VPpart", new TregexPoweredTreebankParserParams.SimpleStringFunction("-v"));
			annotations["-markPP4"] = new Pair("@PP > @Srel", new TregexPoweredTreebankParserParams.SimpleStringFunction("-r"));
			annotations["-markPP5"] = new Pair("@PP > @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-c"));
			annotations["-dominateCC"] = new Pair("__ << @COORD", new TregexPoweredTreebankParserParams.SimpleStringFunction("-withCC"));
			annotations["-dominateIN"] = new Pair("__ << @PP", new TregexPoweredTreebankParserParams.SimpleStringFunction("-withPP"));
			//Klein and Manning style features
			annotations["-markContainsVP"] = new Pair("__ << /^VP/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasV"));
			annotations["-markContainsVP2"] = new Pair("__ << /^VP/=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-hasV-", "word", false));
			annotations["-markVNArgs"] = new Pair("@VN $+ __=word1", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word1", false));
			annotations["-markVNArgs2"] = new Pair("@VN > __=word1 $+ __=word2", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "word1", "word2", false));
			annotations["-markContainsMW"] = new Pair("__ << /^MW/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-hasMW"));
			annotations["-markContainsMW2"] = new Pair("__ << /^MW/=word", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-has-", "word", false));
			//MWE Sequence features
			annotations["-mwStart"] = new Pair("__ >, /^MW/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwStart"));
			annotations["-mwMiddle"] = new Pair("__ !>- /^MW/ !>, /^MW/ > /^MW/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwMid"));
			annotations["-mwMiddle2"] = new Pair("__ !>- /^MW/ !>, /^MW/ > /^MW/ , __=pos", new FrenchTreebankParserParams.AddRelativeNodeFunction(this, "-", "pos", true));
			annotations["-mwEnd"] = new Pair("__ >- /^MW/", new TregexPoweredTreebankParserParams.SimpleStringFunction("-mwEnd"));
			//AP Features
			annotations["-nonNAP"] = new Pair("@AP !$, @N|AP", new TregexPoweredTreebankParserParams.SimpleStringFunction("-nap"));
			//Phrasal splitting
			annotations["-markNPTMP"] = new Pair("@NP < (@N < /^(lundi|mardi|mercredi|jeudi|vendredi|samedi|dimanche|Lundi|Mardi|Mercredi|Jeudi|Vendredi|Samedi|Dimanche|janvier|février|mars|avril|mai|juin|juillet|août|septembre|octobre|novembre|décembre|Janvier|Février|Mars|Avril|Mai|Juin|Juillet|Août|Septembre|Octobre|Novembre|Décembre)$/)"
				, new TregexPoweredTreebankParserParams.SimpleStringFunction("-tmp"));
			//Singular
			annotations["-markSing1"] = new Pair("@NP < (D < /^(ce|cette|une|la|le|un|sa|son|ma|mon|ta|ton)$/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-sing"));
			annotations["-markSing2"] = new Pair("@AP < (A < (/[^sx]$/ !< __))", new TregexPoweredTreebankParserParams.SimpleStringFunction("-sing"));
			annotations["-markSing3"] = new Pair("@VPpart < (V < /(e|é)$/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-sing"));
			//Plural
			annotations["-markPl1"] = new Pair("@NP < (D < /s$/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-pl"));
			annotations["-markPl2"] = new Pair("@AP < (A < /[sx]$/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-pl"));
			annotations["-markPl3"] = new Pair("@VPpart < (V < /(es|és)$/)", new TregexPoweredTreebankParserParams.SimpleStringFunction("-pl"));
			CompileAnnotations(HeadFinder());
		}

		[System.Serializable]
		private class AnnotatePunctuationFunction : ISerializableFunction<TregexMatcher, string>
		{
			internal const string key = "term";

			public virtual string Apply(TregexMatcher m)
			{
				string punc = m.GetNode(key).Value();
				switch (punc)
				{
					case ".":
					{
						return "-fs";
					}

					case "?":
					{
						return "-quest";
					}

					case ",":
					{
						return "-comma";
					}

					case ":":
					case ";":
					{
						return "-colon";
					}
				}
				//      else if (punc.equals("-LRB-"))
				//        return "-lrb";
				//      else if (punc.equals("-RRB-"))
				//        return "-rrb";
				//      else if (punc.equals("-"))
				//        return "-dash";
				//      else if (quote.matcher(punc).matches())
				//        return "-quote";
				//      else if(punc.equals("/"))
				//        return "-slash";
				//      else if(punc.equals("%"))
				//        return "-perc";
				//      else if(punc.contains(".."))
				//        return "-ellipses";
				return string.Empty;
			}

			public override string ToString()
			{
				return "AnnotatePunctuationFunction";
			}

			private const long serialVersionUID = 1L;
		}

		/// <summary>
		/// Annotates all nodes that match the tregex query with annotationMark + key1
		/// Usually annotationMark = "-"
		/// Optionally, you can use a second key in the tregex expression.
		/// </summary>
		[System.Serializable]
		private class AddRelativeNodeFunction : ISerializableFunction<TregexMatcher, string>
		{
			private string annotationMark;

			private string key;

			private string key2;

			private bool doBasicCat = false;

			private bool toLower = false;

			public AddRelativeNodeFunction(FrenchTreebankParserParams _enclosing, string annotationMark, string key, bool basicCategory)
			{
				this._enclosing = _enclosing;
				this.annotationMark = annotationMark;
				this.key = key;
				this.key2 = null;
				this.doBasicCat = basicCategory;
			}

			public AddRelativeNodeFunction(FrenchTreebankParserParams _enclosing, string annotationMark, string key1, string key2, bool basicCategory)
				: this(annotationMark, key1, basicCategory)
			{
				this._enclosing = _enclosing;
				this.key2 = key2;
			}

			public AddRelativeNodeFunction(FrenchTreebankParserParams _enclosing, string annotationMark, string key1, bool basicCategory, bool toLower)
				: this(annotationMark, key1, basicCategory)
			{
				this._enclosing = _enclosing;
				this.toLower = toLower;
			}

			public virtual string Apply(TregexMatcher m)
			{
				string tag;
				if (this.key2 == null)
				{
					tag = this.annotationMark + ((this.doBasicCat) ? this._enclosing.tlp.BasicCategory(m.GetNode(this.key).Label().Value()) : m.GetNode(this.key).Label().Value());
				}
				else
				{
					string annot1 = (this.doBasicCat) ? this._enclosing.tlp.BasicCategory(m.GetNode(this.key).Label().Value()) : m.GetNode(this.key).Label().Value();
					string annot2 = (this.doBasicCat) ? this._enclosing.tlp.BasicCategory(m.GetNode(this.key2).Label().Value()) : m.GetNode(this.key2).Label().Value();
					tag = this.annotationMark + annot1 + this.annotationMark + annot2;
				}
				return (this.toLower) ? tag.ToLower() : tag;
			}

			public override string ToString()
			{
				if (this.key2 == null)
				{
					return "AddRelativeNodeFunction[" + this.annotationMark + ',' + this.key + ']';
				}
				else
				{
					return "AddRelativeNodeFunction[" + this.annotationMark + ',' + this.key + ',' + this.key2 + ']';
				}
			}

			private const long serialVersionUID = 1L;

			private readonly FrenchTreebankParserParams _enclosing;
		}

		[System.Serializable]
		private class AddPOSSequenceFunction : ISerializableFunction<TregexMatcher, string>
		{
			private readonly string annotationMark;

			private readonly bool doBasicCat;

			private readonly double cutoff;

			public AddPOSSequenceFunction(FrenchTreebankParserParams _enclosing, string annotationMark, int cutoff, bool basicCategory)
			{
				this._enclosing = _enclosing;
				this.annotationMark = annotationMark;
				this.doBasicCat = basicCategory;
				this.cutoff = cutoff;
			}

			public virtual string Apply(TregexMatcher m)
			{
				if (this._enclosing.mwCounter == null)
				{
					throw new Exception("Cannot enable POSSequence features without POS sequence map. Use option -frenchMWMap.");
				}
				Tree t = m.GetMatch();
				StringBuilder sb = new StringBuilder();
				foreach (Tree kid in t.Children())
				{
					if (!kid.IsPreTerminal())
					{
						throw new Exception("Not POS sequence for tree: " + t.ToString());
					}
					string tag = this.doBasicCat ? this._enclosing.tlp.BasicCategory(kid.Value()) : kid.Value();
					sb.Append(tag).Append(" ");
				}
				if (this._enclosing.mwCounter.GetCount(t.Value(), sb.ToString().Trim()) > this.cutoff)
				{
					return this.annotationMark + sb.ToString().ReplaceAll("\\s+", string.Empty).ToLower();
				}
				else
				{
					return string.Empty;
				}
			}

			public override string ToString()
			{
				return "AddPOSSequenceFunction[" + this.annotationMark + ',' + this.cutoff + ',' + this.doBasicCat + ']';
			}

			private const long serialVersionUID = 1L;

			private readonly FrenchTreebankParserParams _enclosing;
		}

		public override IHeadFinder HeadFinder()
		{
			if (headFinder == null)
			{
				headFinder = new DybroFrenchHeadFinder(TreebankLanguagePack());
			}
			//Superior for vanilla PCFG over Arun's headfinding rules
			return headFinder;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return HeadFinder();
		}

		private void SetHeadFinder(IHeadFinder hf)
		{
			if (hf == null)
			{
				throw new ArgumentException();
			}
			headFinder = hf;
			CompileAnnotations(hf);
		}

		/// <param name="op">Lexicon options</param>
		/// <returns>A Lexicon</returns>
		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (op.lexOptions.uwModelTrainer == null)
			{
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.FrenchUnknownWordModelTrainer";
			}
			if (morphoSpec != null)
			{
				return new FactoredLexicon(op, morphoSpec, wordIndex, tagIndex);
			}
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		public override string[] SisterSplitters()
		{
			return new string[0];
		}

		public override ITreeTransformer Collinizer()
		{
			return new TreeCollinizer(TreebankLanguagePack());
		}

		public override ITreeTransformer CollinizerEvalb()
		{
			return new TreeCollinizer(TreebankLanguagePack(), collinizerRetainsPunctuation, false);
		}

		public override DiskTreebank DiskTreebank()
		{
			return new DiskTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override MemoryTreebank MemoryTreebank()
		{
			return new MemoryTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return (readPennFormat) ? new FrenchTreeReaderFactory() : new FrenchXMLTreeReaderFactory(false);
		}

		public override IList<IHasWord> DefaultTestSentence()
		{
			string[] sent = new string[] { "Ceci", "est", "seulement", "un", "test", "." };
			return SentenceUtils.ToWordList(sent);
		}

		public override Tree TransformTree(Tree t, Tree root)
		{
			// Perform tregex-powered annotations
			t = base.TransformTree(t, root);
			string cat = t.Value();
			//Add morphosyntactic features if this is a POS tag
			if (t.IsPreTerminal() && tagSpec != null)
			{
				if (!(t.FirstChild().Label() is CoreLabel) || ((CoreLabel)t.FirstChild().Label()).OriginalText() == null)
				{
					throw new Exception(string.Format("%s: Term lacks morpho analysis: %s", this.GetType().FullName, t.ToString()));
				}
				string morphoStr = ((CoreLabel)t.FirstChild().Label()).OriginalText();
				Pair<string, string> lemmaMorph = MorphoFeatureSpecification.SplitMorphString(string.Empty, morphoStr);
				MorphoFeatures feats = tagSpec.StrToFeatures(lemmaMorph.Second());
				cat = feats.GetTag(cat);
			}
			//Update the label(s)
			t.SetValue(cat);
			if (t.IsPreTerminal() && t.Label() is IHasTag)
			{
				((IHasTag)t.Label()).SetTag(cat);
			}
			return t;
		}

		private void LoadMWMap(string filename)
		{
			mwCounter = new TwoDimensionalCounter<string, string>();
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(new File(filename)), "UTF-8"));
				int nLines = 0;
				for (string line; (line = br.ReadLine()) != null; nLines++)
				{
					string[] toks = line.Split("\t");
					System.Diagnostics.Debug.Assert(toks.Length == 3);
					mwCounter.SetCount(toks[0].Trim(), toks[1].Trim(), double.Parse(toks[2].Trim()));
				}
				br.Close();
				System.Console.Error.Printf("%s: Loaded %d lines from %s into MWE counter%n", this.GetType().FullName, nLines, filename);
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		/// <summary>Configures morpho-syntactic annotations for POS tags.</summary>
		/// <param name="activeFeats">
		/// A comma-separated list of feature values with names according
		/// to MorphoFeatureType.
		/// </param>
		private string SetupMorphoFeatures(string activeFeats)
		{
			string[] feats = activeFeats.Split(",");
			morphoSpec = tlp.MorphFeatureSpec();
			foreach (string feat in feats)
			{
				MorphoFeatureSpecification.MorphoFeatureType fType = MorphoFeatureSpecification.MorphoFeatureType.ValueOf(feat.Trim());
				morphoSpec.Activate(fType);
			}
			return morphoSpec.ToString();
		}

		public override void Display()
		{
			log.Info(optionsString.ToString());
		}

		public override int SetOptionFlag(string[] args, int i)
		{
			if (annotations.Contains(args[i]))
			{
				AddFeature(args[i]);
				i++;
			}
			else
			{
				if (args[i].Equals("-collinizerRetainsPunctuation"))
				{
					optionsString.Append("Collinizer retains punctuation.\n");
					collinizerRetainsPunctuation = true;
					i++;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-headFinder") && (i + 1 < args.Length))
					{
						try
						{
							IHeadFinder hf = (IHeadFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
							SetHeadFinder(hf);
							optionsString.Append("HeadFinder: " + args[i + 1] + "\n");
						}
						catch (Exception e)
						{
							log.Info(e);
							log.Info(this.GetType().FullName + ": Could not load head finder " + args[i + 1]);
						}
						i += 2;
					}
					else
					{
						if (args[i].Equals("-xmlFormat"))
						{
							optionsString.Append("Reading trees in XML format.\n");
							readPennFormat = false;
							SetInputEncoding(tlp.GetEncoding());
							i++;
						}
						else
						{
							if (args[i].Equals("-frenchFactored"))
							{
								foreach (string feature in factoredFeatures)
								{
									AddFeature(feature);
								}
								i++;
							}
							else
							{
								if (args[i].Equals("-frenchMWMap"))
								{
									LoadMWMap(args[i + 1]);
									i += 2;
								}
								else
								{
									if (args[i].Equals("-tsg"))
									{
										//wsg2011: These features should be removed for TSG extraction.
										//If they are retained, the resulting grammar seems to be too brittle....
										optionsString.Append("Removing baseline features: -markVN, -coord1");
										RemoveFeature("-markVN");
										optionsString.Append(" (removed -markVN)");
										RemoveFeature("-coord1");
										optionsString.Append(" (removed -coord1)\n");
										i++;
									}
									else
									{
										if (args[i].Equals("-factlex") && (i + 1 < args.Length))
										{
											string activeFeats = SetupMorphoFeatures(args[i + 1]);
											optionsString.Append("Factored Lexicon: active features: ").Append(activeFeats);
											// WSGDEBUG Maybe add -mweTag in place of -tagPAFr?
											RemoveFeature("-tagPAFr");
											optionsString.Append(" (removed -tagPAFr)\n");
											// Add -mweTag
											string[] option = new string[] { "-mweTag" };
											SetOptionFlag(option, 0);
											i += 2;
										}
										else
										{
											if (args[i].Equals("-noFeatures"))
											{
												foreach (string feature in annotations.Keys)
												{
													RemoveFeature(feature);
												}
												optionsString.Append("Removed all manual features.\n");
												i++;
											}
											else
											{
												if (args[i].Equals("-ccTagsetAnnotations"))
												{
													tagSpec = new FrenchMorphoFeatureSpecification();
													tagSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.Other);
													optionsString.Append("Adding CC tagset as POS state splits.\n");
													++i;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return i;
		}
	}
}

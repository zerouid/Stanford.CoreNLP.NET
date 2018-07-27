using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>A Chinese segmenter Feature Factory for the GALE project.</summary>
	/// <remarks>
	/// A Chinese segmenter Feature Factory for the GALE project.
	/// (Modified from the feature factory for Sighan Bakeoff 2005.)
	/// <p>
	/// c is Chinese character ("char").  c means current, n means next and p means previous.
	/// </p>
	/// <table>
	/// <tr>
	/// <th>Feature</th><th>Templates</th>
	/// </tr>
	/// <tr>
	/// <tr>
	/// <th></th><th>Current position clique</th>
	/// </tr>
	/// <tr>
	/// <td>useWord1</td><td>CONSTANT, cc, nc, pc, pc+cc, if (As|Msr|Pk|Hk) cc+nc, pc,nc </td>
	/// </tr>
	/// </table>
	/// </remarks>
	/// <author>Huihsin Tseng</author>
	/// <author>Pichuan Chang</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class Gale2007ChineseSegmenterFeatureFactory<In> : FeatureFactory<In>
		where In : CoreLabel
	{
		private const int Debug = 0;

		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Gale2007ChineseSegmenterFeatureFactory));

		[System.NonSerialized]
		private TagAffixDetector taDetector;

		[System.NonSerialized]
		private CorpusDictionary outDict;

		// = null;
		// = null;
		public override void Init(SeqClassifierFlags flags)
		{
			base.Init(flags);
		}

		private void CreateTADetector()
		{
			lock (this)
			{
				if (taDetector == null)
				{
					taDetector = new TagAffixDetector(flags);
				}
			}
		}

		private void CreateOutDict()
		{
			lock (this)
			{
				if (outDict == null)
				{
					logger.Info("reading " + flags.outDict2 + " as a seen lexicon");
					outDict = new CorpusDictionary(flags.outDict2);
				}
			}
		}

		/// <summary>Extracts all the features from the input data at a certain index.</summary>
		/// <param name="cInfo">The complete data set as a List of WordInfo</param>
		/// <param name="loc">The index at which to extract features.</param>
		public override ICollection<string> GetCliqueFeatures(PaddedList<In> cInfo, int loc, Clique clique)
		{
			ICollection<string> features = Generics.NewHashSet();
			if (clique == cliqueC)
			{
				AddAllInterningAndSuffixing(features, FeaturesC(cInfo, loc), "C");
			}
			else
			{
				if (clique == cliqueCpC)
				{
					AddAllInterningAndSuffixing(features, FeaturesCpC(cInfo, loc), "CpC");
					AddAllInterningAndSuffixing(features, FeaturesCnC(cInfo, loc - 1), "CnC");
				}
				else
				{
					if (clique == cliqueCpCp2C)
					{
						AddAllInterningAndSuffixing(features, FeaturesCpCp2C(cInfo, loc), "CpCp2C");
					}
					else
					{
						if (clique == cliqueCpCp2Cp3C)
						{
							AddAllInterningAndSuffixing(features, FeaturesCpCp2Cp3C(cInfo, loc), "CpCp2Cp3C");
						}
					}
				}
			}
			if (Debug > 0)
			{
				EncodingPrintWriter.Err.Println("For " + cInfo[loc] + ", features: " + features, "UTF-8");
			}
			return features;
		}

		private static readonly Pattern patE = Pattern.Compile("[a-z]");

		private static readonly Pattern patEC = Pattern.Compile("[A-Z]");

		private static string IsEnglish(string chp, string chc)
		{
			Matcher mp = patE.Matcher(chp);
			// previous char is [a-z]
			Matcher mc = patE.Matcher(chc);
			//  current char is [a-z]
			Matcher mpC = patEC.Matcher(chp);
			// previous char is [A-Z]
			Matcher mcC = patEC.Matcher(chc);
			//  current char is [A-Z]
			if (mp.Matches() && mcC.Matches())
			{
				return "BND";
			}
			else
			{
				// [a-z][A-Z]
				if (mp.Matches() && mc.Matches())
				{
					return "ENG";
				}
				else
				{
					// [a-z][a-z]
					if (mpC.Matches() && mcC.Matches())
					{
						return "BCC";
					}
					else
					{
						// [A-Z][A-Z]
						if (mp.Matches() && !mc.Matches() && !mcC.Matches())
						{
							return "e1";
						}
						else
						{
							// [a-z][^A-Za-z]
							if (mc.Matches() && !mp.Matches() && !mpC.Matches())
							{
								return "e2";
							}
							else
							{
								// [^A-Za-z][a-z]
								if (mpC.Matches() && !mc.Matches() && !mcC.Matches())
								{
									return "e3";
								}
								else
								{
									// [A-Z][^A-Za-z]
									if (mcC.Matches() && !mp.Matches() && !mpC.Matches())
									{
										return "e4";
									}
									else
									{
										// [^A-Za-z][A-Z]
										return string.Empty;
									}
								}
							}
						}
					}
				}
			}
		}

		private static readonly Pattern patP = Pattern.Compile("[-\u00b7.]");

		// end isEnglish
		// the pattern used to be [\u00b7\\-\\.] which AFAICS matched only . because - wasn't escaped. CDM Nov 2007
		private static string IsEngPU(string Ep)
		{
			Matcher mp = patP.Matcher(Ep);
			if (mp.Matches())
			{
				return "1:EngPU";
			}
			else
			{
				return string.Empty;
			}
		}

		//is EnglishPU
		private static void DictionaryFeaturesC(Type lbeginFieldName, Type lmiddleFieldName, Type lendFieldName, string dictSuffix, ICollection<string> features, CoreLabel p, CoreLabel c, CoreLabel c2)
		{
			string lbegin = c.GetString(lbeginFieldName);
			string lmiddle = c.GetString(lmiddleFieldName);
			string lend = c.GetString(lendFieldName);
			features.Add(lbegin + dictSuffix + "-lb");
			features.Add(lmiddle + dictSuffix + "-lm");
			features.Add(lend + dictSuffix + "-le");
			lbegin = p.GetString(lbeginFieldName);
			lmiddle = p.GetString(lmiddleFieldName);
			lend = p.GetString(lendFieldName);
			features.Add(lbegin + dictSuffix + "-plb");
			features.Add(lmiddle + dictSuffix + "-plm");
			features.Add(lend + dictSuffix + "-ple");
			lbegin = c2.GetString(lbeginFieldName);
			lmiddle = c2.GetString(lmiddleFieldName);
			lend = c2.GetString(lendFieldName);
			features.Add(lbegin + dictSuffix + "-c2lb");
			features.Add(lmiddle + dictSuffix + "-c2lm");
			features.Add(lend + dictSuffix + "-c2le");
		}

		protected internal virtual ICollection<string> FeaturesC<_T0>(PaddedList<_T0> cInfo, int loc)
			where _T0 : CoreLabel
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel c2 = cInfo[loc + 1];
			CoreLabel c3 = cInfo[loc + 2];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string charc = c.GetString<CoreAnnotations.CharAnnotation>();
			string charc2 = c2.GetString<CoreAnnotations.CharAnnotation>();
			string charc3 = c3.GetString<CoreAnnotations.CharAnnotation>();
			string charp = p.GetString<CoreAnnotations.CharAnnotation>();
			string charp2 = p2.GetString<CoreAnnotations.CharAnnotation>();
			string charp3 = p3.GetString<CoreAnnotations.CharAnnotation>();
			int cI = c.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypec = (cI != null ? cI.ToString() : string.Empty);
			int c2I = c2.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypec2 = (c2I != null ? c2I.ToString() : string.Empty);
			int c3I = c3.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypec3 = (c3I != null ? c3I.ToString() : string.Empty);
			int pI = p.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypep = (pI != null ? pI.ToString() : string.Empty);
			int p2I = p2.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypep2 = (p2I != null ? p2I.ToString() : string.Empty);
			/* N-gram features. N is upto 2. */
			if (flags.useWord1)
			{
				// features.add(charc +"c");
				// features.add(charc2+"c2");
				// features.add(charp +"p");
				// features.add(charp + charc  +"pc");
				// features.add(charc + charc2  +"cc2");
				// cdm: need hyphen so you can see which of charp or charc2 is null....
				// features.add(charp + "-" + charc2 + "pc2");
				features.Add(charc + "::c");
				features.Add(charc2 + "::c2");
				features.Add(charp + "::p");
				features.Add(charp2 + "::p2");
				// trying to restore the features that Huishin described in SIGHAN 2005 paper
				features.Add(charc + charc2 + "::cn");
				features.Add(charc + charc3 + "::cn2");
				features.Add(charp + charc + "::pc");
				features.Add(charp + charc2 + "::pn");
				features.Add(charp2 + charp + "::p2p");
				features.Add(charp2 + charc + "::p2c");
				features.Add(charc2 + charc + "::n2c");
			}
			if (flags.dictionary != null || flags.serializedDictionary != null)
			{
				DictionaryFeaturesC(typeof(CoreAnnotations.LBeginAnnotation), typeof(CoreAnnotations.LMiddleAnnotation), typeof(CoreAnnotations.LEndAnnotation), string.Empty, features, p, c, c2);
			}
			if (flags.dictionary2 != null)
			{
				DictionaryFeaturesC(typeof(CoreAnnotations.D2_LBeginAnnotation), typeof(CoreAnnotations.D2_LMiddleAnnotation), typeof(CoreAnnotations.D2_LEndAnnotation), "-D2-", features, p, c, c2);
			}
			if (flags.useFeaturesC4gram || flags.useFeaturesC5gram || flags.useFeaturesC6gram)
			{
				features.Add(charp2 + charp + "p2p");
				features.Add(charp2 + "p2");
			}
			if (flags.useFeaturesC5gram || flags.useFeaturesC6gram)
			{
				features.Add(charc3 + "c3");
				features.Add(charc2 + charc3 + "c2c3");
			}
			if (flags.useFeaturesC6gram)
			{
				features.Add(charp3 + "p3");
				features.Add(charp3 + charp2 + "p3p2");
			}
			if (flags.useUnicodeType || flags.useUnicodeType4gram || flags.useUnicodeType5gram)
			{
				features.Add(uTypep + "-" + uTypec + "-" + uTypec2 + "-uType3");
			}
			if (flags.useUnicodeType4gram || flags.useUnicodeType5gram)
			{
				features.Add(uTypep2 + "-" + uTypep + "-" + uTypec + "-" + uTypec2 + "-uType4");
			}
			if (flags.useUnicodeType5gram)
			{
				features.Add(uTypep2 + "-" + uTypep + "-" + uTypec + "-" + uTypec2 + "-" + uTypec3 + "-uType5");
			}
			if (flags.useUnicodeBlock)
			{
				features.Add(p.GetString<CoreAnnotations.UBlockAnnotation>() + "-" + c.GetString<CoreAnnotations.UBlockAnnotation>() + "-" + c2.GetString<CoreAnnotations.UBlockAnnotation>() + "-uBlock");
			}
			if (flags.useShapeStrings)
			{
				if (flags.useShapeStrings1)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + "ps");
					features.Add(c.GetString<CoreAnnotations.ShapeAnnotation>() + "cs");
					features.Add(c2.GetString<CoreAnnotations.ShapeAnnotation>() + "c2s");
				}
				if (flags.useShapeStrings3)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "pscsc2s");
				}
				if (flags.useShapeStrings4)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "p2spscsc2s");
				}
				if (flags.useShapeStrings5)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + c3.GetString<CoreAnnotations.ShapeAnnotation
						>() + "p2spscsc2sc3s");
				}
			}
			features.Add("cliqueC");
			return features;
		}

		private void DictionaryFeaturesCpC(Type lbeginFieldName, Type lmiddleFieldName, Type lendFieldName, string dictSuffix, ICollection<string> features, CoreLabel p2, CoreLabel p, CoreLabel c, CoreLabel c2)
		{
			string lbegin = c.GetString(lbeginFieldName);
			string lmiddle = c.GetString(lmiddleFieldName);
			string lend = c.GetString(lendFieldName);
			features.Add(lbegin + dictSuffix + "-lb");
			features.Add(lmiddle + dictSuffix + "-lm");
			features.Add(lend + dictSuffix + "-le");
			lbegin = p.GetString(lbeginFieldName);
			lmiddle = p.GetString(lmiddleFieldName);
			lend = p.Get(lendFieldName);
			features.Add(lbegin + dictSuffix + "-plb");
			features.Add(lmiddle + dictSuffix + "-plm");
			features.Add(lend + dictSuffix + "-ple");
			lbegin = c2.GetString(lbeginFieldName);
			lmiddle = c2.GetString(lmiddleFieldName);
			lend = c2.GetString(lendFieldName);
			features.Add(lbegin + dictSuffix + "-c2lb");
			features.Add(lmiddle + dictSuffix + "-c2lm");
			features.Add(lend + dictSuffix + "-c2le");
			if (flags.useDictionaryConjunctions)
			{
				string p2Lend = p2.GetString(lendFieldName);
				string pLend = p.GetString(lendFieldName);
				string pLbegin = p.GetString(lbeginFieldName);
				string cLbegin = c.GetString(lbeginFieldName);
				string cLmiddle = c.GetString(lmiddleFieldName);
				if (flags.useDictionaryConjunctions3)
				{
					features.Add(pLend + cLbegin + cLmiddle + dictSuffix + "-pcLconj1");
				}
				features.Add(p2Lend + pLend + cLbegin + cLmiddle + dictSuffix + "-p2pcLconj1");
				features.Add(p2Lend + pLend + pLbegin + cLbegin + cLmiddle + dictSuffix + "-p2pcLconj2");
			}
		}

		protected internal virtual ICollection<string> FeaturesCpC<_T0>(PaddedList<_T0> cInfo, int loc)
			where _T0 : CoreLabel
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel c2 = cInfo[loc + 1];
			CoreLabel c3 = cInfo[loc + 2];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string charc = c.GetString<CoreAnnotations.CharAnnotation>();
			string charc2 = c2.GetString<CoreAnnotations.CharAnnotation>();
			string charc3 = c3.GetString<CoreAnnotations.CharAnnotation>();
			string charp = p.GetString<CoreAnnotations.CharAnnotation>();
			string charp2 = p2.GetString<CoreAnnotations.CharAnnotation>();
			string charp3 = p3.GetString<CoreAnnotations.CharAnnotation>();
			int cI = c.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypec = (cI != null ? cI.ToString() : string.Empty);
			int c2I = c2.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypec2 = (c2I != null ? c2I.ToString() : string.Empty);
			int c3I = c3.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypec3 = (c3I != null ? c3I.ToString() : string.Empty);
			int pI = p.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypep = (pI != null ? pI.ToString() : string.Empty);
			int p2I = p2.Get(typeof(CoreAnnotations.UTypeAnnotation));
			string uTypep2 = (p2I != null ? p2I.ToString() : string.Empty);
			if (flags.dictionary != null || flags.serializedDictionary != null)
			{
				DictionaryFeaturesCpC(typeof(CoreAnnotations.LBeginAnnotation), typeof(CoreAnnotations.LMiddleAnnotation), typeof(CoreAnnotations.LEndAnnotation), string.Empty, features, p2, p, c, c2);
			}
			if (flags.dictionary2 != null)
			{
				DictionaryFeaturesCpC(typeof(CoreAnnotations.D2_LBeginAnnotation), typeof(CoreAnnotations.D2_LMiddleAnnotation), typeof(CoreAnnotations.D2_LEndAnnotation), "-D2-", features, p2, p, c, c2);
			}
			/*
			* N-gram features. N is upto 2.
			*/
			if (flags.useWord2)
			{
				// features.add(charc +"c");
				// features.add(charc2+"c2");
				// features.add(charp +"p");
				// features.add(charp + charc  +"pc");
				// features.add(charc + charc2  +"cc2");
				// // cdm: need hyphen so you can see which of charp or charc2 is null....
				// features.add(charp + "-" + charc2 + "pc2");
				features.Add(charc + "::c");
				features.Add(charc2 + "::c1");
				features.Add(charp + "::p");
				features.Add(charp2 + "::p2");
				// trying to restore the features that Huihsin described in SIGHAN 2005 paper
				features.Add(charc + charc2 + "::cn");
				// (*)
				features.Add(charp + charc + "::pc");
				features.Add(charp + charc2 + "::pn");
				features.Add(charp2 + charp + "::p2p");
				features.Add(charp2 + charc + "::p2c");
				features.Add(charc2 + charc + "::n2c");
			}
			// todo: this is messed up: Same as one above at (*); should be cn2 = charc + charc3 + "::cn2"
			if (flags.useFeaturesCpC4gram || flags.useFeaturesCpC5gram || flags.useFeaturesCpC6gram)
			{
				// todo: Both these features duplicate ones already in useWord2
				features.Add(charp2 + charp + "p2p");
				features.Add(charp2 + "p2");
			}
			if (flags.useFeaturesCpC5gram || flags.useFeaturesCpC6gram)
			{
				features.Add(charc3 + "c3");
				features.Add(charc2 + charc3 + "c2c3");
			}
			if (flags.useFeaturesCpC6gram)
			{
				features.Add(charp3 + "p3");
				features.Add(charp3 + charp2 + "p3p2");
			}
			if (flags.useGoodForNamesCpC)
			{
				// these 2 features should be distinctively good at biasing from
				// picking up a Chinese family name in the p2 or p3 positions:
				// familyName X X startWord AND familyName X startWord
				// But actually they seem to have negative value.
				features.Add(charp2 + "p2");
				features.Add(charp3 + "p3");
			}
			if (flags.useUnicodeType || flags.useUnicodeType4gram || flags.useUnicodeType5gram)
			{
				features.Add(uTypep + "-" + uTypec + "-" + uTypec2 + "-uType3");
			}
			if (flags.useUnicodeType4gram || flags.useUnicodeType5gram)
			{
				features.Add(uTypep2 + "-" + uTypep + "-" + uTypec + "-" + uTypec2 + "-uType4");
			}
			if (flags.useUnicodeType5gram)
			{
				features.Add(uTypep2 + "-" + uTypep + "-" + uTypec + "-" + uTypec2 + "-" + uTypec3 + "-uType5");
			}
			if (flags.useWordUTypeConjunctions2)
			{
				features.Add(uTypep + charc + "putcc");
				features.Add(charp + uTypec + "pccut");
			}
			if (flags.useWordUTypeConjunctions3)
			{
				features.Add(uTypep2 + uTypep + charc + "p2utputcc");
				features.Add(uTypep + charc + uTypec2 + "putccc2ut");
				features.Add(charc + uTypec2 + uTypec3 + "ccc2utc3ut");
			}
			if (flags.useUnicodeBlock)
			{
				features.Add(p.GetString<CoreAnnotations.UBlockAnnotation>() + "-" + c.GetString<CoreAnnotations.UBlockAnnotation>() + "-" + c2.GetString<CoreAnnotations.UBlockAnnotation>() + "-uBlock");
			}
			if (flags.useShapeStrings)
			{
				if (flags.useShapeStrings1)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + "ps");
					features.Add(c.GetString<CoreAnnotations.ShapeAnnotation>() + "cs");
					features.Add(c2.GetString<CoreAnnotations.ShapeAnnotation>() + "c2s");
				}
				if (flags.useShapeStrings3)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "pscsc2s");
				}
				if (flags.useShapeStrings4)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "p2spscsc2s");
				}
				if (flags.useShapeStrings5)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + c3.GetString<CoreAnnotations.ShapeAnnotation
						>() + "p2spscsc2sc3s");
				}
				if (flags.useWordShapeConjunctions2)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + charc + "pscc");
					features.Add(charp + c.GetString<CoreAnnotations.ShapeAnnotation>() + "pccs");
				}
				if (flags.useWordShapeConjunctions3)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + charc + "p2spscc");
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + charc + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "psccc2s");
					features.Add(charc + c2.GetString<CoreAnnotations.ShapeAnnotation>() + c3.GetString<CoreAnnotations.ShapeAnnotation>() + "ccc2sc3s");
				}
			}
			/*
			Radical N-gram features. N is upto 4.
			Smoothing method of N-gram, because there are too many characters in Chinese.
			(It works better than N-gram when they are used individually. less sparse)
			*/
			char rcharc;
			char rcharc2;
			char rcharp;
			char rcharp2;
			if (charc.Length == 0)
			{
				rcharc = 'n';
			}
			else
			{
				rcharc = RadicalMap.GetRadical(charc[0]);
			}
			if (charc2.Length == 0)
			{
				rcharc2 = 'n';
			}
			else
			{
				rcharc2 = RadicalMap.GetRadical(charc2[0]);
			}
			if (charp.Length == 0)
			{
				rcharp = 'n';
			}
			else
			{
				rcharp = RadicalMap.GetRadical(charp[0]);
			}
			if (charp2.Length == 0)
			{
				rcharp2 = 'n';
			}
			else
			{
				rcharp2 = RadicalMap.GetRadical(charp2[0]);
			}
			if (flags.useRad2)
			{
				features.Add(rcharc + "rc");
				features.Add(rcharc2 + "rc2");
				features.Add(rcharp + "rp");
				features.Add(rcharp + rcharc + "rprc");
				features.Add(rcharc + rcharc2 + "rcrc2");
				features.Add(rcharp + rcharc + rcharc2 + "rprcrc2");
			}
			if (flags.useRad2b)
			{
				features.Add(rcharc + "rc");
				features.Add(rcharc2 + "rc2");
				features.Add(rcharp + "rp");
				features.Add(rcharp + rcharc + "rprc");
				features.Add(rcharc + rcharc2 + "rcrc2");
				features.Add(rcharp2 + rcharp + "rp2rp");
			}
			/* Non-word dictionary: SEEN bi-gram marked as non-word.
			* This is frickin' useful.  I hadn't realized.  CDM Oct 2007.
			*/
			if (flags.useDict2)
			{
				NonDict2 nd = new NonDict2(flags);
				features.Add(nd.CheckDic(charp + charc, flags) + "nondict");
			}
			if (flags.useOutDict2)
			{
				if (outDict == null)
				{
					CreateOutDict();
				}
				features.Add(outDict.GetW(charp + charc) + "outdict");
				// -1 0
				features.Add(outDict.GetW(charc + charc2) + "outdict");
				// 0 1
				features.Add(outDict.GetW(charp2 + charp) + "outdict");
				// -2 -1
				features.Add(outDict.GetW(charp2 + charp + charc) + "outdict");
				// -2 -1 0
				features.Add(outDict.GetW(charp3 + charp2 + charp) + "outdict");
				// -3 -2 -1
				features.Add(outDict.GetW(charp + charc + charc2) + "outdict");
				// -1 0 1
				features.Add(outDict.GetW(charc + charc2 + charc3) + "outdict");
				// 0 1 2
				features.Add(outDict.GetW(charp + charc + charc2 + charc3) + "outdict");
			}
			// -1 0 1 2
			/*
			(CTB/ASBC/HK/PK/MSR) POS information of each characters.
			If a character falls into some function categories,
			it is very likely there is a boundary.
			A lot of Chinese function words belong to single characters.
			This feature is also good for numbers and punctuations.
			DE* are grouped into DE.
			*/
			if (flags.useCTBChar2 || flags.useASBCChar2 || flags.useHKChar2 || flags.usePKChar2 || flags.useMSRChar2)
			{
				string[] tagsets;
				// the "useChPos" now only works for CTB and PK
				if (flags.useChPos)
				{
					if (flags.useCTBChar2)
					{
						tagsets = new string[] { "AD", "AS", "BA", "CC", "CD", "CS", "DE", "DT", "ETC", "IJ", "JJ", "LB", "LC", "M", "NN", "NR", "NT", "OD", "P", "PN", "PU", "SB", "SP", "VA", "VC", "VE", "VV" };
					}
					else
					{
						if (flags.usePKChar2)
						{
							//tagsets = new String[]{"r", "j", "t", "a", "nz", "l", "vn", "i", "m", "ns", "nr", "v", "n", "q", "Ng", "b", "d", "nt"};
							tagsets = new string[] { "2", "3", "4" };
						}
						else
						{
							throw new Exception("only support settings for CTB and PK now.");
						}
					}
				}
				else
				{
					//logger.info("Using Derived features");
					tagsets = new string[] { "2", "3", "4" };
				}
				if (taDetector == null)
				{
					CreateTADetector();
				}
				foreach (string tag in tagsets)
				{
					features.Add(taDetector.CheckDic(tag + "p", charp) + taDetector.CheckDic(tag + "i", charp) + taDetector.CheckDic(tag + "s", charc) + taDetector.CheckInDic(charp) + taDetector.CheckInDic(charc) + tag + "prep-sufc");
				}
			}
			//features.add("|ctbchar2");
			/*
			In error analysis, we found English words and numbers are often separated.
			Rule 1: isNumber feature: check if the current and previous char is a number.
			Rule 2: Disambiguation of time point and time duration.
			Rule 3: isEnglish feature: check if the current and previous character is an english letter.
			Rule 4: English name feature: check if the current char is a conjunct pu for English first and last name, since there is no space between two names.
			Most of PUs are a good indicator for word boundary, but - and .  is a strong indicator that there is no boundry within a previous , a follow char and it.
			*/
			if (flags.useRule2)
			{
				/* Reduplication features */
				// previous character == current character
				if (charp.Equals(charc))
				{
					features.Add("11-R2");
				}
				// previous character == next character
				if (charp.Equals(charc2))
				{
					features.Add("22-R2");
				}
				// current character == next next character
				// fire only when usePk and useHk are both false.
				// Notice: this should be (almost) the same as the "22" feature, but we keep it for now.
				if (!flags.usePk && !flags.useHk)
				{
					if (charc.Equals(charc2))
					{
						features.Add("33-R2");
					}
				}
				char cur1 = ' ';
				char cur2 = ' ';
				char cur = ' ';
				char pre = ' ';
				// actually their length must be either 0 or 1
				if (charc2.Length > 0)
				{
					cur1 = charc2[0];
				}
				if (charc3.Length > 0)
				{
					cur2 = charc3[0];
				}
				if (charc.Length > 0)
				{
					cur = charc[0];
				}
				if (charp.Length > 0)
				{
					pre = charp[0];
				}
				string prer = rcharp.ToString();
				// the radical of previous character
				Pattern E = Pattern.Compile("[a-zA-Z]");
				Pattern N = Pattern.Compile("[0-9]");
				Matcher m = E.Matcher(charp);
				Matcher ce = E.Matcher(charc);
				Matcher pe = E.Matcher(charp2);
				Matcher cn = N.Matcher(charc);
				Matcher pn = N.Matcher(charp2);
				// if current and previous characters are numbers...
				if (cur >= '0' && cur <= '9' && pre >= '0' && pre <= '9')
				{
					if (cur == '9' && pre == '1' && cur1 == '9' && cur2 >= '0' && cur2 <= '9')
					{
						//199x
						features.Add("YR-R2");
					}
					else
					{
						features.Add("2N-R2");
					}
				}
				else
				{
					// if current and previous characters are not both numbers
					// but previous char is a number
					// i.e. patterns like "1N" , "2A", etc
					if (pre >= '0' && pre <= '9')
					{
						features.Add("1N-R2");
					}
					else
					{
						// if previous character is an English character
						if (m.Matches())
						{
							features.Add("E-R2");
						}
						else
						{
							// if the previous character contains no radical (and it exist)
							if (prer.Equals(".") && charp.Length == 1)
							{
								if (ce.Matches())
								{
									features.Add("PU+E-R2");
								}
								if (pe.Matches())
								{
									features.Add("E+PU-R2");
								}
								if (cn.Matches())
								{
									features.Add("PU+N-R2");
								}
								if (pn.Matches())
								{
									features.Add("N+PU-R2");
								}
								features.Add("PU-R2");
							}
						}
					}
				}
				string engType = IsEnglish(charp, charc);
				string engPU = IsEngPU(charp);
				if (!engType.Equals(string.Empty))
				{
					features.Add(engType);
				}
				if (!engPU.Equals(string.Empty) && !engType.Equals(string.Empty))
				{
					StringBuilder sb = new StringBuilder();
					sb.Append(engPU).Append(engType).Append("R2");
					features.Add(sb.ToString());
				}
			}
			//end of use rule
			// features using "Character.getType" information!
			string origS = c.GetString<CoreAnnotations.OriginalCharAnnotation>();
			char origC = ' ';
			if (origS.Length > 0)
			{
				origC = origS[0];
			}
			int type = char.GetType(origC);
			switch (type)
			{
				case char.UppercaseLetter:
				case char.LowercaseLetter:
				{
					// A-Z and full-width A-Z
					// a-z and full-width a-z
					features.Add("CHARTYPE-LETTER");
					break;
				}

				case char.DecimalDigitNumber:
				{
					features.Add("CHARTYPE-DECIMAL_DIGIT_NUMBER");
					break;
				}

				case char.OtherLetter:
				{
					// mostly chinese chars
					features.Add("CHARTYPE-OTHER_LETTER");
					break;
				}

				default:
				{
					// other types
					features.Add("CHARTYPE-MISC");
					break;
				}
			}
			features.Add("cliqueCpC");
			return features;
		}

		// end featuresCpC
		/// <summary>
		/// For a CRF, this shouldn't be necessary, since the features duplicate
		/// those from CpC, but Huihsin found some valuable, presumably becuase
		/// it modified the regularization a bit.
		/// </summary>
		/// <param name="cInfo">The list of characters</param>
		/// <param name="loc">Position of c in list</param>
		/// <returns>Collection of String features (sparse set of boolean features</returns>
		protected internal virtual ICollection<string> FeaturesCnC<_T0>(PaddedList<_T0> cInfo, int loc)
			where _T0 : CoreLabel
		{
			ICollection<string> features = new List<string>();
			if (flags.useWordn)
			{
				CoreLabel c = cInfo[loc];
				CoreLabel c2 = cInfo[loc + 1];
				CoreLabel p = cInfo[loc - 1];
				CoreLabel p2 = cInfo[loc - 2];
				string charc = c.GetString<CoreAnnotations.CharAnnotation>();
				string charc2 = c2.GetString<CoreAnnotations.CharAnnotation>();
				string charp = p.GetString<CoreAnnotations.CharAnnotation>();
				string charp2 = p2.GetString<CoreAnnotations.CharAnnotation>();
				features.Add(charc + "c");
				features.Add(charc2 + "c2");
				features.Add(charp + "p");
				features.Add(charp2 + "p2");
				features.Add(charp2 + charp + "p2p");
				features.Add(charp + charc + "pc");
				features.Add(charc + charc2 + "cc2");
				features.Add(charp + "-" + charc2 + "pc2");
				features.Add("cliqueCnC");
			}
			return features;
		}

		//end of CnC
		/// <summary>Second order clique features</summary>
		/// <param name="cInfo">The list of characters</param>
		/// <param name="loc">Position of c in list</param>
		/// <returns>Collection of String features (sparse set of boolean features</returns>
		protected internal virtual ICollection<string> FeaturesCpCp2C<_T0>(PaddedList<_T0> cInfo, int loc)
			where _T0 : CoreLabel
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel c2 = cInfo[loc + 1];
			CoreLabel c3 = cInfo[loc + 2];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string charc = c.GetString<CoreAnnotations.CharAnnotation>();
			string charc2 = c2.GetString<CoreAnnotations.CharAnnotation>();
			string charc3 = c3.GetString<CoreAnnotations.CharAnnotation>();
			string charp = p.GetString<CoreAnnotations.CharAnnotation>();
			string charp2 = p2.GetString<CoreAnnotations.CharAnnotation>();
			string charp3 = p3.GetString<CoreAnnotations.CharAnnotation>();
			// N-gram features. N is up to 3
			if (flags.useWord3)
			{
				features.Add(charc + "::c");
				features.Add(charc2 + "::n");
				features.Add(charp + "::p");
				features.Add(charp2 + "::p2");
				// trying to restore the features that Huihsin described in SIGHAN 2005 paper
				features.Add(charc + charc2 + "::cn");
				features.Add(charc + charc2 + charc3 + "::cnn2");
				features.Add(charp + charc + "::pc");
				features.Add(charp + charc2 + "::pn");
				features.Add(charp2 + charp + "::p2p");
				features.Add(charp3 + charp2 + charp + "::p3p2p");
				features.Add(charp2 + charc + "::p2c");
				features.Add(charc + charc3 + "::cn2");
			}
			if (flags.useShapeStrings)
			{
				if (flags.useShapeStrings1)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + "ps");
					features.Add(c.GetString<CoreAnnotations.ShapeAnnotation>() + "cs");
					features.Add(c2.GetString<CoreAnnotations.ShapeAnnotation>() + "c2s");
				}
				if (flags.useShapeStrings3)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "pscsc2s");
				}
				if (flags.useShapeStrings4)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "p2spscsc2s");
				}
				if (flags.useShapeStrings5)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + c.GetString<CoreAnnotations.ShapeAnnotation>() + c2.GetString<CoreAnnotations.ShapeAnnotation>() + c3.GetString<CoreAnnotations.ShapeAnnotation
						>() + "p2spscsc2sc3s");
				}
				if (flags.useWordShapeConjunctions2)
				{
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + charc + "pscc");
					features.Add(charp + c.GetString<CoreAnnotations.ShapeAnnotation>() + "pccs");
				}
				if (flags.useWordShapeConjunctions3)
				{
					features.Add(p2.GetString<CoreAnnotations.ShapeAnnotation>() + p.GetString<CoreAnnotations.ShapeAnnotation>() + charc + "p2spscc");
					features.Add(p.GetString<CoreAnnotations.ShapeAnnotation>() + charc + c2.GetString<CoreAnnotations.ShapeAnnotation>() + "psccc2s");
					features.Add(charc + c2.GetString<CoreAnnotations.ShapeAnnotation>() + c3.GetString<CoreAnnotations.ShapeAnnotation>() + "ccc2sc3s");
				}
			}
			/*
			Radical N-gram features. N is upto 4.
			Smoothing method of N-gram, because there are too many characters in Chinese.
			(It works better than N-gram when they are used individually. less sparse)
			*/
			char rcharc;
			char rcharc2;
			char rcharp;
			char rcharp2;
			if (charc.Length == 0)
			{
				rcharc = 'n';
			}
			else
			{
				rcharc = RadicalMap.GetRadical(charc[0]);
			}
			if (charc2.Length == 0)
			{
				rcharc2 = 'n';
			}
			else
			{
				rcharc2 = RadicalMap.GetRadical(charc2[0]);
			}
			if (charp.Length == 0)
			{
				rcharp = 'n';
			}
			else
			{
				rcharp = RadicalMap.GetRadical(charp[0]);
			}
			if (charp2.Length == 0)
			{
				rcharp2 = 'n';
			}
			else
			{
				rcharp2 = RadicalMap.GetRadical(charp2[0]);
			}
			if (flags.useRad2)
			{
				features.Add(rcharc + "rc");
				features.Add(rcharc2 + "rc2");
				features.Add(rcharp + "rp");
				features.Add(rcharp + rcharc + "rprc");
				features.Add(rcharc + rcharc2 + "rcrc2");
				features.Add(rcharp + rcharc + rcharc2 + "rprcrc2");
			}
			if (flags.useRad2b)
			{
				features.Add(rcharc + "rc");
				features.Add(rcharc2 + "rc2");
				features.Add(rcharp + "rp");
				features.Add(rcharp + rcharc + "rprc");
				features.Add(rcharc + rcharc2 + "rcrc2");
				features.Add(rcharp2 + rcharp + "rp2rp");
			}
			features.Add("cliqueCpCp2C");
			return features;
		}

		// end featuresCpCp2C
		protected internal virtual ICollection<string> FeaturesCpCp2Cp3C<_T0>(PaddedList<_T0> cInfo, int loc)
			where _T0 : CoreLabel
		{
			ICollection<string> features = new List<string>();
			if (flags.use4Clique && flags.maxLeft >= 3)
			{
				CoreLabel c = cInfo[loc];
				CoreLabel c2 = cInfo[loc + 1];
				CoreLabel p = cInfo[loc - 1];
				CoreLabel p2 = cInfo[loc - 2];
				CoreLabel p3 = cInfo[loc - 3];
				string charc = c.GetString<CoreAnnotations.CharAnnotation>();
				string charp = p.GetString<CoreAnnotations.CharAnnotation>();
				string charp2 = p2.GetString<CoreAnnotations.CharAnnotation>();
				string charp3 = p3.GetString<CoreAnnotations.CharAnnotation>();
				int cI = c.Get(typeof(CoreAnnotations.UTypeAnnotation));
				string uTypec = (cI != null ? cI.ToString() : string.Empty);
				int c2I = c2.Get(typeof(CoreAnnotations.UTypeAnnotation));
				string uTypec2 = (c2I != null ? c2I.ToString() : string.Empty);
				int pI = p.Get(typeof(CoreAnnotations.UTypeAnnotation));
				string uTypep = (pI != null ? pI.ToString() : string.Empty);
				int p2I = p2.Get(typeof(CoreAnnotations.UTypeAnnotation));
				string uTypep2 = (p2I != null ? p2I.ToString() : string.Empty);
				int p3I = p3.Get(typeof(CoreAnnotations.UTypeAnnotation));
				string uTypep3 = (p3I != null ? p3I.ToString() : string.Empty);
				if (flags.useLongSequences)
				{
					features.Add(charp3 + charp2 + charp + charc + "p3p2pc");
				}
				if (flags.useUnicodeType4gram || flags.useUnicodeType5gram)
				{
					features.Add(uTypep3 + "-" + uTypep2 + "-" + uTypep + "-" + uTypec + "-uType4");
				}
				if (flags.useUnicodeType5gram)
				{
					features.Add(uTypep3 + "-" + uTypep2 + "-" + uTypep + "-" + uTypec + "-" + uTypec2 + "-uType5");
				}
				features.Add("cliqueCpCp2Cp3C");
			}
			return features;
		}

		private const long serialVersionUID = 8197648719208850960L;
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>A Chinese segmenter Feature Factory for GALE project.</summary>
	/// <remarks>
	/// A Chinese segmenter Feature Factory for GALE project. (modified from Sighan Bakeoff 2005.)
	/// This is supposed to have all the good closed-track features from Sighan bakeoff 2005,
	/// and some other "open-track" features
	/// This will also be used to do a character-based chunking!
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
	[System.Serializable]
	public class ChineseSegmenterFeatureFactory<In> : FeatureFactory<In>
		where In : CoreLabel
	{
		private const long serialVersionUID = 3387166382968763350L;

		private static TagAffixDetector taDetector = null;

		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(ChineseSegmenterFeatureFactory));

		public override void Init(SeqClassifierFlags flags)
		{
			base.Init(flags);
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
			}
			// else if (clique == cliqueCpCp2C) {
			//   addAllInterningAndSuffixing(features, featuresCpCp2C(cInfo, loc), "CpCp2C");
			// } else if (clique == cliqueCpCp2Cp3C) {
			//   addAllInterningAndSuffixing(features, featuresCpCp2Cp3C(cInfo, loc), "CpCp2Cp3C");
			// } else if (clique == cliqueCpCp2Cp3Cp4C) {
			//   addAllInterningAndSuffixing(features, featuresCpCp2Cp3Cp4C(cInfo, loc), "CpCp2Cp3Cp4C");
			// } else if (clique == cliqueCpCp2Cp3Cp4Cp5C) {
			//   addAllInterningAndSuffixing(features, featuresCpCp2Cp3Cp4Cp5C(cInfo, loc), "CpCp2Cp3Cp4Cp5C");
			// }
			return features;
		}

		private static Pattern patE = Pattern.Compile("[a-z]");

		private static Pattern patEC = Pattern.Compile("[A-Z]");

		private static string IsEnglish(string Ep, string Ec)
		{
			string chp = Ep;
			string chc = Ec;
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

		private static Pattern patP = Pattern.Compile("[\u00b7\\-\\.]");

		//is English
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
		public virtual ICollection<string> FeaturesC(PaddedList<In> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel c1 = cInfo[loc + 1];
			CoreLabel c2 = cInfo[loc + 2];
			CoreLabel c3 = cInfo[loc + 3];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			string charc1 = c1.Get(typeof(CoreAnnotations.CharAnnotation));
			string charc2 = c2.Get(typeof(CoreAnnotations.CharAnnotation));
			string charc3 = c3.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp2 = p2.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp3 = p3.Get(typeof(CoreAnnotations.CharAnnotation));
			if (flags.useWord1)
			{
				// features.add(charc +"c");
				// features.add(charc1+"c1");
				// features.add(charp +"p");
				// features.add(charp +charc  +"pc");
				// if(flags.useAs || flags.useMsr || flags.usePk || flags.useHk){ //msr, as
				//   features.add(charc +charc1 +"cc1");
				//   features.add(charp + charc1 +"pc1");
				// }
				features.Add(charc + "::c");
				features.Add(charc1 + "::c1");
				features.Add(charp + "::p");
				features.Add(charp2 + "::p2");
				// trying to restore the features that Huishin described in SIGHAN 2005 paper
				features.Add(charc + charc1 + "::cn");
				features.Add(charp + charc + "::pc");
				features.Add(charp + charc1 + "::pn");
				features.Add(charp2 + charp + "::p2p");
				features.Add(charp2 + charc + "::p2c");
				features.Add(charc2 + charc + "::n2c");
				features.Add("|word1");
			}
			return features;
		}

		private static CorpusDictionary outDict = null;

		public virtual ICollection<string> FeaturesCpC(PaddedList<In> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel c1 = cInfo[loc + 1];
			CoreLabel c2 = cInfo[loc + 2];
			CoreLabel c3 = cInfo[loc + 3];
			CoreLabel p = cInfo[loc - 1];
			CoreLabel p2 = cInfo[loc - 2];
			CoreLabel p3 = cInfo[loc - 3];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charc == null)
			{
				charc = string.Empty;
			}
			string charc1 = c1.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charc1 == null)
			{
				charc1 = string.Empty;
			}
			string charc2 = c2.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charc2 == null)
			{
				charc2 = string.Empty;
			}
			string charc3 = c3.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charc3 == null)
			{
				charc3 = string.Empty;
			}
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charp == null)
			{
				charp = string.Empty;
			}
			string charp2 = p2.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charp2 == null)
			{
				charp2 = string.Empty;
			}
			string charp3 = p3.Get(typeof(CoreAnnotations.CharAnnotation));
			if (charp3 == null)
			{
				charp3 = string.Empty;
			}
			/*
			* N-gram features. N is upto 2.
			*/
			if (flags.useWord2)
			{
				// features.add(charc +"c");
				// features.add(charc1+"c1");
				// features.add(charp +"p");
				// features.add(charp +charc  +"pc");
				// if( flags.useMsr ){
				//   features.add(charc +charc1 +"cc1");
				//   features.add(charp + charc1 +"pc1");
				// }
				features.Add(charc + "::c");
				features.Add(charc1 + "::c1");
				features.Add(charp + "::p");
				features.Add(charp2 + "::p2");
				// trying to restore the features that Huishin described in SIGHAN 2005 paper
				features.Add(charc + charc1 + "::cn");
				features.Add(charp + charc + "::pc");
				features.Add(charp + charc1 + "::pn");
				features.Add(charp2 + charp + "::p2p");
				features.Add(charp2 + charc + "::p2c");
				features.Add(charc2 + charc + "::n2c");
				features.Add("|word2");
			}
			/*
			Radical N-gram features. N is upto 4.
			Smoothing method of N-gram, because there are too many characters in Chinese.
			(It works better than N-gram when they are used individually. less sparse)
			*/
			char rcharc;
			char rcharc1;
			char rcharc2;
			char rcharc3;
			char rcharp;
			char rcharp1;
			char rcharp2;
			char rcharp3;
			if (charc.Length == 0)
			{
				rcharc = 'n';
			}
			else
			{
				rcharc = RadicalMap.GetRadical(charc[0]);
			}
			if (charc1.Length == 0)
			{
				rcharc1 = 'n';
			}
			else
			{
				rcharc1 = RadicalMap.GetRadical(charc1[0]);
			}
			if (charc2.Length == 0)
			{
				rcharc2 = 'n';
			}
			else
			{
				rcharc2 = RadicalMap.GetRadical(charc2[0]);
			}
			if (charc3.Length == 0)
			{
				rcharc3 = 'n';
			}
			else
			{
				rcharc3 = RadicalMap.GetRadical(charc3[0]);
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
			if (charp3.Length == 0)
			{
				rcharp3 = 'n';
			}
			else
			{
				rcharp3 = RadicalMap.GetRadical(charp3[0]);
			}
			if (flags.useRad2)
			{
				features.Add(rcharc + "rc");
				features.Add(rcharc1 + "rc1");
				features.Add(rcharp + "rp");
				features.Add(rcharp + rcharc + "rpc");
				features.Add(rcharc + rcharc1 + "rcc1");
				features.Add(rcharp + rcharc + rcharc1 + "rpcc1");
				features.Add("|rad2");
			}
			/* non-word dictionary:SEEM bi-gram marked as non-word */
			if (flags.useDict2)
			{
				NonDict2 nd = new NonDict2(flags);
				features.Add(nd.CheckDic(charp + charc, flags) + "nondict");
				features.Add("|useDict2");
			}
			if (flags.useOutDict2)
			{
				if (outDict == null)
				{
					logger.Info("reading " + flags.outDict2 + " as a seen lexicon");
					outDict = new CorpusDictionary(flags.outDict2, true);
				}
				features.Add(outDict.GetW(charp + charc) + "outdict");
				// -1 0
				features.Add(outDict.GetW(charc + charc1) + "outdict");
				// 0 1
				features.Add(outDict.GetW(charp2 + charp) + "outdict");
				// -2 -1
				features.Add(outDict.GetW(charp2 + charp + charc) + "outdict");
				// -2 -1 0
				features.Add(outDict.GetW(charp3 + charp2 + charp) + "outdict");
				// -3 -2 -1
				features.Add(outDict.GetW(charp + charc + charc1) + "outdict");
				// -1 0 1
				features.Add(outDict.GetW(charc + charc1 + charc2) + "outdict");
				// 0 1 2
				features.Add(outDict.GetW(charp + charc + charc1 + charc2) + "outdict");
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
					taDetector = new TagAffixDetector(flags);
				}
				foreach (string tagset in tagsets)
				{
					features.Add(taDetector.CheckDic(tagset + "p", charp) + taDetector.CheckDic(tagset + "i", charp) + taDetector.CheckDic(tagset + "s", charc) + taDetector.CheckInDic(charp) + taDetector.CheckInDic(charc) + tagset + "prep-sufc");
				}
			}
			// features.add("|ctbchar2");  // Added a constant feature several times!!
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
					features.Add("11");
				}
				// previous character == next character
				if (charp.Equals(charc1))
				{
					features.Add("22");
				}
				// current character == next next character
				// fire only when usePk and useHk are both false.
				// Notice: this should be (almost) the same as the "22" feature, but we keep it for now.
				if (!flags.usePk && !flags.useHk)
				{
					if (charc.Equals(charc2))
					{
						features.Add("33");
					}
				}
				char cur1 = ' ';
				char cur2 = ' ';
				char cur = ' ';
				char pre = ' ';
				// actually their length must be either 0 or 1
				if (charc1.Length > 0)
				{
					cur1 = charc1[0];
				}
				if (charc2.Length > 0)
				{
					cur2 = charc2[0];
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
						features.Add("YR");
					}
					else
					{
						features.Add("2N");
					}
				}
				else
				{
					// if current and previous characters are not both numbers
					// but previous char is a number
					// i.e. patterns like "1N" , "2A", etc
					if (pre >= '0' && pre <= '9')
					{
						features.Add("1N");
					}
					else
					{
						// if previous character is an English character
						if (m.Matches())
						{
							features.Add("E");
						}
						else
						{
							// if the previous character contains no radical (and it exist)
							if (prer.Equals(".") && charp.Length == 1)
							{
								// fire only when usePk and useHk are both false. Not sure why. -pichuan
								if (!flags.useHk && !flags.usePk)
								{
									if (ce.Matches())
									{
										features.Add("PU+E");
									}
									if (pe.Matches())
									{
										features.Add("E+PU");
									}
									if (cn.Matches())
									{
										features.Add("PU+N");
									}
									if (pn.Matches())
									{
										features.Add("N+PU");
									}
								}
								features.Add("PU");
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
					features.Add(engPU + engType);
				}
			}
			//end of use rule
			// features using "Character.getType" information!
			string origS = c.Get(typeof(CoreAnnotations.OriginalCharAnnotation));
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
			return features;
		}

		public virtual ICollection<string> FeaturesCnC(PaddedList<In> cInfo, int loc)
		{
			ICollection<string> features = new List<string>();
			CoreLabel c = cInfo[loc];
			CoreLabel c1 = cInfo[loc + 1];
			CoreLabel p = cInfo[loc - 1];
			string charc = c.Get(typeof(CoreAnnotations.CharAnnotation));
			string charc1 = c1.Get(typeof(CoreAnnotations.CharAnnotation));
			string charp = p.Get(typeof(CoreAnnotations.CharAnnotation));
			if (flags.useWordn)
			{
				features.Add(charc + "c");
				features.Add(charc1 + "c1");
				features.Add(charp + "p");
				features.Add(charp + charc + "pc");
				if (flags.useAs || flags.useMsr || flags.usePk || flags.useHk)
				{
					features.Add(charc + charc1 + "cc1");
					features.Add(charp + charc1 + "pc1");
				}
				features.Add("|wordn");
			}
			return features;
		}
		//end of CnC
		//end of Class
	}
}

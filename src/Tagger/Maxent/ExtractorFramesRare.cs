// ExtractorFramesRare -- StanfordMaxEnt, A Maximum Entropy Toolkit
// Copyright (c) 2002-2008 The Board of Trustees of
// Leland Stanford Junior University. All rights reserved.
//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//For more information, bug reports, fixes, contact:
//Christopher Manning
//Dept of Computer Science, Gates 1A
//Stanford CA 94305-9010
//USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
//http://www-nlp.stanford.edu/software/tagger.shtml
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.French;
using Edu.Stanford.Nlp.International.Spanish;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// This class contains feature extractors for the MaxentTagger that are only
	/// applied to rare (low frequency/unknown) words.
	/// </summary>
	/// <remarks>
	/// This class contains feature extractors for the MaxentTagger that are only
	/// applied to rare (low frequency/unknown) words.
	/// The following options are supported:
	/// <table>
	/// <tr><td>Name</td><td>Args</td><td>Effect</td></tr>
	/// <tr><td>wordshapes</td><td>left, right</td>
	/// <td>Word shape features, e.g., transform Foo5 into Xxx#
	/// (not exactly like that, but that general idea).
	/// Creates individual features for each word left ... right.
	/// If just one argument wordshapes(-2) is given, then end is taken as 0.
	/// If left is not less than or equal to right, no features are made.
	/// Fairly English-specific.</td></tr>
	/// <tr><td>unicodeshapes</td><td>left, right</td>
	/// <td>Same thing, but works for unicode characters generally.</td></tr>
	/// <tr><td>unicodeshapeconjunction</td><td>left, right</td>
	/// <td>Instead of individual word shape features, combines several
	/// word shapes into one feature.</td></tr>
	/// <tr><td>suffix</td><td>length, position</td>
	/// <td>Features for suffixes of the word position.  One feature for
	/// each suffix of length 1 ... length.</td></tr>
	/// <tr><td>prefix</td><td>length, position</td>
	/// <td>Features for prefixes of the word position.  One feature for
	/// each prefix of length 1 ... length.</td></tr>
	/// <tr><td>prefixsuffix</td><td>length</td>
	/// <td>Features for concatenated prefix and suffix.  One feature for
	/// each of length 1 ... length.</td></tr>
	/// <tr><td>capitalizationsuffix</td><td>length</td>
	/// <td>Current word only.  Combines character suffixes up to size length with a
	/// binary value for whether the word contains any capital letters.</td></tr>
	/// <tr><td>distsim</td><td>filename, left, right</td>
	/// <td>Individual features for each position left ... right.
	/// Compares that word with the dictionary in filename.</td></tr>
	/// <tr><td>distsimconjunction</td><td>filename, left, right</td>
	/// <td>A concatenation of distsim features from left ... right.</td></tr>
	/// </table>
	/// Also available are the macros "naacl2003unknowns",
	/// "lnaacl2003unknowns", and "naacl2003conjunctions".
	/// naacl2003unknowns and lnaacl2003unknowns include suffix extractors
	/// and extractors for specific word shape features, such as containing
	/// or not containing a digit.
	/// <br />
	/// The macro "frenchunknowns" is a macro for five extractors specific
	/// to French, which test the end of the word to see if it matches
	/// common suffixes for various POS classes and plural words.  Adding
	/// this experiment did not improve accuracy over the regular
	/// naacl2003unknowns extractor macro, though.
	/// <br />
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <author>Christopher Manning</author>
	/// <author>Michel Galley</author>
	/// <version>2.0</version>
	public class ExtractorFramesRare
	{
		/// <summary>Last 1-4 characters of word</summary>
		private static readonly Extractor cWordSuff1 = new ExtractorWordSuff(1, 0);

		private static readonly Extractor cWordSuff2 = new ExtractorWordSuff(2, 0);

		private static readonly Extractor cWordSuff3 = new ExtractorWordSuff(3, 0);

		private static readonly Extractor cWordSuff4 = new ExtractorWordSuff(4, 0);

		/// <summary>"1" iff word contains 1 or more upper case characters (somewhere)</summary>
		private static readonly Extractor cWordUppCase = new ExtractorUCase();

		/// <summary>"1" iff word contains 1 or more digit characters (somewhere)</summary>
		private static readonly Extractor cWordNumber = new ExtractorCNumber();

		/// <summary>"1" iff word contains 1 or more dash characters (somewhere)</summary>
		private static readonly Extractor cWordDash = new ExtractorDash();

		/// <summary>"1" if token has no lower case letters</summary>
		private static readonly Extractor cNoLower = new ExtractorAllCap();

		/// <summary>"1" if token has only upper case letters</summary>
		private static readonly Extractor cAllCapitalized = new ExtractorAllCapitalized();

		/// <summary>"1" if capitalized and one of following 3 words is Inc., Co., or Corp.</summary>
		private static readonly Extractor cCompany = new CompanyNameDetector();

		/// <summary>
		/// "1" if capitalized and one of following 3 words is Inc., Co.,
		/// Corp., or similar words
		/// </summary>
		private static readonly Extractor cCaselessCompany = new CaselessCompanyNameDetector();

		/// <summary>"1" if word contains letter, digit, and dash, in any position and case</summary>
		private static readonly Extractor cLetterDigitDash = new ExtractorLetterDigitDash();

		/// <summary>"1" if word contains uppercase letter, digit, and dash</summary>
		private static readonly Extractor cUpperDigitDash = new ExtractorUpperDigitDash();

		/// <summary>Distance to lowercase word.</summary>
		/// <remarks>Distance to lowercase word.  Used by another extractor....</remarks>
		private static readonly Extractor cCapDist = new ExtractorCapDistLC();

		private static readonly Extractor[] eFrames_motley_naacl2003 = new Extractor[] { cWordUppCase, cWordNumber, cWordDash, cNoLower, cLetterDigitDash, cCompany, cAllCapitalized, cUpperDigitDash };

		private static readonly Extractor[] eFrames_motley_naacl2003_left = new Extractor[] { cWordUppCase, cWordNumber, cWordDash, cNoLower, cLetterDigitDash, cAllCapitalized, cUpperDigitDash };

		private static readonly Extractor[] eFrames_motley_caseless_naacl2003 = new Extractor[] { cWordNumber, cWordDash, cLetterDigitDash, cCaselessCompany };

		/// <summary>Whether it has a typical French noun suffix.</summary>
		private static readonly ExtractorFrenchNounSuffix cWordFrenchNounSuffix = new ExtractorFrenchNounSuffix();

		/// <summary>Whether it has a typical French adverb suffix.</summary>
		private static readonly ExtractorFrenchAdvSuffix cWordFrenchAdvSuffix = new ExtractorFrenchAdvSuffix();

		/// <summary>Whether it has a typical French verb suffix.</summary>
		private static readonly ExtractorFrenchVerbSuffix cWordFrenchVerbSuffix = new ExtractorFrenchVerbSuffix();

		/// <summary>Whether it has a typical French adjective suffix.</summary>
		private static readonly ExtractorFrenchAdjSuffix cWordFrenchAdjSuffix = new ExtractorFrenchAdjSuffix();

		/// <summary>Whether it has a typical French plural suffix.</summary>
		private static readonly ExtractorFrenchPluralSuffix cWordFrenchPluralSuffix = new ExtractorFrenchPluralSuffix();

		private static readonly Extractor[] french_unknown_extractors = new Extractor[] { cWordFrenchNounSuffix, cWordFrenchAdvSuffix, cWordFrenchVerbSuffix, cWordFrenchAdjSuffix, cWordFrenchPluralSuffix };

		/// <summary>Extracts Spanish gender patterns.</summary>
		private static readonly ExtractorSpanishGender cWordSpanishGender = new ExtractorSpanishGender();

		/// <summary>Matches conditional-tense verb suffixes.</summary>
		private static readonly ExtractorSpanishConditionalSuffix cWordSpanishConditionalSuffix = new ExtractorSpanishConditionalSuffix();

		/// <summary>Matches imperfect-tense verb suffixes (-er, -ir verbs).</summary>
		private static readonly ExtractorSpanishImperfectErIrSuffix cWordSpanishImperfectErIrSuffix = new ExtractorSpanishImperfectErIrSuffix();

		private static readonly Extractor[] spanish_unknown_extractors = new Extractor[] { cWordSpanishGender, cWordSpanishConditionalSuffix, cWordSpanishImperfectErIrSuffix };

		private ExtractorFramesRare()
		{
		}

		/// <summary>
		/// Adds a few specific extractors needed by both "naacl2003unknowns"
		/// and "lnaacl2003unknowns".
		/// </summary>
		private static void GetNaaclExtractors(List<Extractor> extrs)
		{
			extrs.Add(new ExtractorStartSentenceCap());
			extrs.Add(new ExtractorMidSentenceCapC());
			extrs.Add(new ExtractorMidSentenceCap());
			for (int i = 1; i <= 10; i++)
			{
				extrs.Add(new ExtractorWordSuff(i, 0));
			}
			for (int i_1 = 1; i_1 <= 10; i_1++)
			{
				extrs.Add(new ExtractorWordPref(i_1, 0));
			}
		}

		/// <summary>
		/// Adds a few specific extractors needed by "naacl2003unknowns" in a
		/// caseless form.
		/// </summary>
		private static void GetCaselessNaaclExtractors(List<Extractor> extrs)
		{
			for (int i = 1; i <= 10; i++)
			{
				extrs.Add(new ExtractorWordSuff(i, 0));
			}
			for (int i_1 = 1; i_1 <= 10; i_1++)
			{
				extrs.Add(new ExtractorWordPref(i_1, 0));
			}
		}

		/// <summary>Get an array of rare word feature Extractor identified by a name.</summary>
		/// <remarks>
		/// Get an array of rare word feature Extractor identified by a name.
		/// Note: Names used here must also be known in getExtractorFrames, so we
		/// can appropriately add error messages.  So if you add a keyword here,
		/// add it there as one to be ignored, too. (In the next iteration, this
		/// class and ExtractorFrames should probably just be combined).
		/// </remarks>
		/// <param name="identifier">Describes a set of extractors for rare word features</param>
		/// <returns>A set of extractors for rare word features</returns>
		protected internal static Extractor[] GetExtractorFramesRare(string identifier, TTags ttags)
		{
			List<Extractor> extrs = new List<Extractor>();
			IList<string> args = StringUtils.ValueSplit(identifier, "[a-zA-Z0-9]*(?:\\([^)]*\\))?", "\\s*,\\s*");
			foreach (string arg in args)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase("naacl2003unknowns", arg))
				{
					Sharpen.Collections.AddAll(extrs, Arrays.AsList(eFrames_motley_naacl2003));
					GetNaaclExtractors(extrs);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(("lnaacl2003unknowns"), arg))
					{
						Sharpen.Collections.AddAll(extrs, Arrays.AsList(eFrames_motley_naacl2003_left));
						GetNaaclExtractors(extrs);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase("caselessnaacl2003unknowns", arg))
						{
							Sharpen.Collections.AddAll(extrs, Arrays.AsList(eFrames_motley_caseless_naacl2003));
							GetCaselessNaaclExtractors(extrs);
						}
						else
						{
							// TODO: test this next one
							if (Sharpen.Runtime.EqualsIgnoreCase("naacl2003conjunctions", arg))
							{
								Sharpen.Collections.AddAll(extrs, Arrays.AsList(Naacl2003Conjunctions()));
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase("frenchunknowns", arg))
								{
									Sharpen.Collections.AddAll(extrs, Arrays.AsList(french_unknown_extractors));
								}
								else
								{
									if (arg.StartsWith("wordshapes("))
									{
										int lWindow = Extractor.GetParenthesizedNum(arg, 1);
										int rWindow = Extractor.GetParenthesizedNum(arg, 2);
										string wsc = Extractor.GetParenthesizedArg(arg, 3);
										if (wsc == null)
										{
											wsc = "chris2";
										}
										for (int i = lWindow; i <= rWindow; i++)
										{
											extrs.Add(new ExtractorWordShapeClassifier(i, wsc));
										}
									}
									else
									{
										if (arg.StartsWith("wordshapeconjunction("))
										{
											int lWindow = Extractor.GetParenthesizedNum(arg, 1);
											int rWindow = Extractor.GetParenthesizedNum(arg, 2);
											string wsc = Extractor.GetParenthesizedArg(arg, 3);
											if (wsc == null)
											{
												wsc = "chris2";
											}
											for (int i = lWindow; i <= rWindow; i++)
											{
												extrs.Add(new ExtractorWordShapeConjunction(lWindow, rWindow, wsc));
											}
										}
										else
										{
											if (arg.StartsWith("unicodeshapes("))
											{
												int lWindow = Extractor.GetParenthesizedNum(arg, 1);
												int rWindow = Extractor.GetParenthesizedNum(arg, 2);
												for (int i = lWindow; i <= rWindow; i++)
												{
													extrs.Add(new ExtractorWordShapeClassifier(i, "chris4"));
												}
											}
											else
											{
												if (arg.StartsWith("unicodeshapeconjunction("))
												{
													int lWindow = Extractor.GetParenthesizedNum(arg, 1);
													int rWindow = Extractor.GetParenthesizedNum(arg, 2);
													extrs.Add(new ExtractorWordShapeConjunction(lWindow, rWindow, "chris4"));
												}
												else
												{
													if (arg.StartsWith("chinesedictionaryfeatures("))
													{
														throw new Exception("These features are no longer supported." + "  The paths and data files associated " + "with this material are out of date, and " + "the classes used are not thread-safe.  " + "Those problems would need to be fixed " + "to use this feature."
															);
													}
													else
													{
														//String path = Extractor.getParenthesizedArg(arg, 1);
														//// Default nlp location for these features is: /u/nlp/data/pos-tagger/dictionary
														//int lWindow = Extractor.getParenthesizedNum(arg, 2);
														//int rWindow = Extractor.getParenthesizedNum(arg, 3);
														//// First set up the dictionary prefix for the Chinese dictionaries
														//ASBCDict.setPathPrefix(path);
														//for (int i = lWindow; i <= rWindow; i++) {
														//  extrs.addAll(Arrays.asList(ctbPreFeatures(i)));
														//  extrs.addAll(Arrays.asList(ctbSufFeatures(i)));
														//  extrs.addAll(Arrays.asList(ctbUnkDictFeatures(i)));
														//  extrs.addAll(Arrays.asList(asbcUnkFeatures(i)));
														//}
														// No longer add prefix suffix features, now that you can more flexibly add them separately.
														// } else if ("generic".equalsIgnoreCase(arg)) {
														//   // does prefix and suffix up to 6 grams
														//   for (int i = 1; i <= 6; i++) {
														//     extrs.add(new ExtractorCWordSuff(i));
														//     extrs.add(new ExtractorCWordPref(i));
														//   }
														if (Sharpen.Runtime.EqualsIgnoreCase(arg, "motleyUnknown"))
														{
															// This is naacl2003unknown minus prefix and suffix features.
															Sharpen.Collections.AddAll(extrs, Arrays.AsList(eFrames_motley_naacl2003));
														}
														else
														{
															if (arg.StartsWith("suffix("))
															{
																int max = Extractor.GetParenthesizedNum(arg, 1);
																// will conveniently be 0 if not specified
																int position = Extractor.GetParenthesizedNum(arg, 2);
																for (int i = 1; i <= max; i++)
																{
																	extrs.Add(new ExtractorWordSuff(i, position));
																}
															}
															else
															{
																if (arg.StartsWith("prefix("))
																{
																	int max = Extractor.GetParenthesizedNum(arg, 1);
																	// will conveniently be 0 if not specified
																	int position = Extractor.GetParenthesizedNum(arg, 2);
																	for (int i = 1; i <= max; i++)
																	{
																		extrs.Add(new ExtractorWordPref(i, position));
																	}
																}
																else
																{
																	if (arg.StartsWith("prefixsuffix("))
																	{
																		int max = Extractor.GetParenthesizedNum(arg, 1);
																		for (int i = 1; i <= max; i++)
																		{
																			extrs.Add(new ExtractorsConjunction(new ExtractorWordPref(i, 0), new ExtractorWordSuff(i, 0)));
																		}
																	}
																	else
																	{
																		if (arg.StartsWith("capitalizationsuffix("))
																		{
																			int max = Extractor.GetParenthesizedNum(arg, 1);
																			for (int i = 1; i <= max; i++)
																			{
																				extrs.Add(new ExtractorsConjunction(cWordUppCase, new ExtractorWordSuff(i, 0)));
																			}
																		}
																		else
																		{
																			if (arg.StartsWith("distsim("))
																			{
																				string path = Extractor.GetParenthesizedArg(arg, 1);
																				// traditional nlp filesystem location is: /u/nlp/data/pos_tags_are_useless/egw.bnc.200.pruned
																				int lWindow = Extractor.GetParenthesizedNum(arg, 2);
																				int rWindow = Extractor.GetParenthesizedNum(arg, 3);
																				for (int i = lWindow; i <= rWindow; i++)
																				{
																					extrs.Add(new ExtractorDistsim(path, i));
																				}
																			}
																			else
																			{
																				if (arg.StartsWith("distsimconjunction("))
																				{
																					string path = Extractor.GetParenthesizedArg(arg, 1);
																					int lWindow = Extractor.GetParenthesizedNum(arg, 2);
																					int rWindow = Extractor.GetParenthesizedNum(arg, 3);
																					extrs.Add(new ExtractorDistsimConjunction(path, lWindow, rWindow));
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(arg, "lctagfeatures"))
																					{
																						Sharpen.Collections.AddAll(extrs, Arrays.AsList(LcTagFeatures(ttags)));
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
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return Sharpen.Collections.ToArray(extrs, new Extractor[extrs.Count]);
		}

		/// <summary>This provides the conjunction of various features as rare words features.</summary>
		/// <returns>An array of feature conjunctions</returns>
		private static Extractor[] Naacl2003Conjunctions()
		{
			Extractor[] newW = new Extractor[24];
			//add them manually ....
			newW[0] = new ExtractorsConjunction(cWordUppCase, cWordSuff1);
			newW[1] = new ExtractorsConjunction(cWordUppCase, cWordSuff2);
			newW[2] = new ExtractorsConjunction(cWordUppCase, cWordSuff3);
			newW[3] = new ExtractorsConjunction(cWordUppCase, cWordSuff4);
			newW[4] = new ExtractorsConjunction(cNoLower, cWordSuff1);
			newW[5] = new ExtractorsConjunction(cNoLower, cWordSuff2);
			newW[6] = new ExtractorsConjunction(cNoLower, cWordSuff3);
			newW[7] = new ExtractorsConjunction(cNoLower, cWordSuff4);
			Extractor cMidSentence = new ExtractorMidSentenceCap();
			newW[8] = new ExtractorsConjunction(cMidSentence, cWordSuff1);
			newW[9] = new ExtractorsConjunction(cMidSentence, cWordSuff2);
			newW[10] = new ExtractorsConjunction(cMidSentence, cWordSuff3);
			newW[11] = new ExtractorsConjunction(cMidSentence, cWordSuff4);
			Extractor cWordStartUCase = new ExtractorStartSentenceCap();
			newW[12] = new ExtractorsConjunction(cWordStartUCase, cWordSuff1);
			newW[13] = new ExtractorsConjunction(cWordStartUCase, cWordSuff2);
			newW[14] = new ExtractorsConjunction(cWordStartUCase, cWordSuff3);
			newW[15] = new ExtractorsConjunction(cWordStartUCase, cWordSuff4);
			Extractor cWordMidUCase = new ExtractorMidSentenceCapC();
			newW[16] = new ExtractorsConjunction(cWordMidUCase, cWordSuff1);
			newW[17] = new ExtractorsConjunction(cWordMidUCase, cWordSuff2);
			newW[18] = new ExtractorsConjunction(cWordMidUCase, cWordSuff3);
			newW[19] = new ExtractorsConjunction(cWordMidUCase, cWordSuff4);
			newW[20] = new ExtractorsConjunction(cCapDist, cWordSuff1);
			newW[21] = new ExtractorsConjunction(cCapDist, cWordSuff2);
			newW[22] = new ExtractorsConjunction(cCapDist, cWordSuff3);
			newW[23] = new ExtractorsConjunction(cCapDist, cWordSuff4);
			return newW;
		}

		private static Extractor[] LcTagFeatures(TTags ttags)
		{
			Extractor[] newE = new Extractor[ttags.GetSize()];
			for (int i = 0; i < ttags.GetSize(); i++)
			{
				string tag = ttags.GetTag(i);
				newE[i] = new ExtractorCapLCSeen(tag);
			}
			return newE;
		}

		/* private ExtractorFramesRare() {
		// this is now a statics only class!
		} */
		/*
		ArrayList<Extractor> v = new ArrayList<Extractor>();
		GlobalHolder.ySize = GlobalHolder.tags.getSize();
		for (int i = 1; i < 5; i++) {
		for (int y = 0; y < GlobalHolder.tags.getSize(); y++) {
		if (!GlobalHolder.tags.isClosed(GlobalHolder.tags.getTag(y))) {
		ExtractorMorpho extr = new ExtractorMorpho(i, y);
		v.add(extr);
		}// if open
		}
		}// for i
		
		for (int y = 0; y < GlobalHolder.ySize; y++) {
		for (int y1 = 0; y1 < GlobalHolder.ySize; y1++) {
		if (!GlobalHolder.tags.isClosed(GlobalHolder.tags.getTag(y)) && (!GlobalHolder.tags.isClosed(GlobalHolder.tags.getTag(y)))) {
		ExtractorMorpho extr = new ExtractorMorpho(5, y, y1);
		v.add(extr);
		}// if open
		}
		}
		int vSize = v.size();
		Extractor[] eFramestemp = new Extractor[eFrames.length + vSize];
		System.arraycopy(eFrames, 0, eFramestemp, 0, eFrames.length);
		for (int i = 0; i < vSize; i++) {
		eFramestemp[i + eFrames.length] = v.get(i);
		}
		eFrames = eFramestemp;
		*/
		private static Extractor[] CtbPreFeatures(int n)
		{
			string[] tagsets = new string[] { "AD", "AS", "BA", "CC", "CD", "CS", "DEC", "DEG", "DER", "DEV", "DT", "ETC", "FW", "IJ", "JJ", "LB", "LC", "M", "MSP", "NN", "NP", "NR", "NT", "OD", "P", "PN", "PU", "SB", "SP", "VA", "VC", "VE", "VV" };
			Extractor[] newW = new Extractor[tagsets.Length];
			for (int k = 0; k < tagsets.Length; k++)
			{
				newW[k] = new CtbPreDetector(tagsets[k], n);
			}
			return newW;
		}

		// end ctbPreFeatures
		private static Extractor[] CtbSufFeatures(int n)
		{
			string[] tagsets = new string[] { "AD", "AS", "BA", "CC", "CD", "CS", "DEC", "DEG", "DER", "DEV", "DT", "ETC", "FW", "IJ", "JJ", "LB", "LC", "M", "MSP", "NN", "NP", "NR", "NT", "OD", "P", "PN", "PU", "SB", "SP", "VA", "VC", "VE", "VV" };
			Extractor[] newW = new Extractor[tagsets.Length];
			for (int k = 0; k < tagsets.Length; k++)
			{
				newW[k] = new CtbSufDetector(tagsets[k], n);
			}
			return newW;
		}

		// end ctbSuffFeatures
		/*
		public static Extractor[] asbcPreFeatures(int n) {
		String[] tagsets = {"A", "Caa", "Cab", "Cba", "Cbb", "D", "DE", "DK", "Da", "Dd", "De", "Des", "Dfa", "Dfb", "Di", "Dk", "FW", "I", " Na", "Nb", " Nc", "Ncb", "Ncd", " Nd", "Neaq", "Nep", "Neqa", "Neqb", "Nes", "Neu", "Nf", "Ng", "Nh", "P", "PU", "SHI", "T", "VA", "VAC", "VB", "VC", "VCL", "VD", "VE", "VF", "VG", "VH", "VHC", "VI", "VJ", "VK", "VL", "V_2" };
		Extractor[] newW=new Extractor[tagsets.length];
		for(int k=0;k<tagsets.length;k++){
		newW[k] = new ASBCPreDetector(tagsets[k], n);
		}
		return newW;
		}
		
		public static Extractor[] asbcSufFeatures(int n) {
		String[] tagsets = {"A", "Caa", "Cab", "Cba", "Cbb", "D", "DE", "DK", "Da", "Dd", "De", "Des", "Dfa", "Dfb", "Di", "Dk", "FW", "I", " Na", "Nb", " Nc", "Ncb", "Ncd", " Nd", "Neaq", "Nep", "Neqa", "Neqb", "Nes", "Neu", "Nf", "Ng", "Nh", "P", "PU", "SHI", "T", "VA", "VAC", "VB", "VC", "VCL", "VD", "VE", "VF", "VG", "VH", "VHC", "VI", "VJ", "VK", "VL", "V_2"  };
		Extractor[] newW=new Extractor[tagsets.length];
		for(int k=0;k<tagsets.length;k++){
		newW[k] = new ASBCSufDetector(tagsets[k], n);
		}
		return newW;
		}
		*/
		private static Extractor[] AsbcUnkFeatures(int n)
		{
			string[] tagsets = new string[] { "A", "Caa", "Cab", "Cba", "Cbb", "D", "DE", "DK", "Da", "Dd", "De", "Des", "Dfa", "Dfb", "Di", "Dk", "FW", "I", " Na", "Nb", " Nc", "Ncb", "Ncd", " Nd", "Neaq", "Nep", "Neqa", "Neqb", "Nes", "Neu", "Nf", "Ng"
				, "Nh", "P", "PU", "SHI", "T", "VA", "VAC", "VB", "VC", "VCL", "VD", "VE", "VF", "VG", "VH", "VHC", "VI", "VJ", "VK", "VL", "V_2" };
			Extractor[] newW = new Extractor[tagsets.Length];
			for (int k = 0; k < tagsets.Length; k++)
			{
				newW[k] = new ASBCunkDetector(tagsets[k], n);
			}
			return newW;
		}

		private static Extractor[] CtbUnkDictFeatures(int n)
		{
			string[] tagsets = new string[] { "A", "Caa", "Cab", "Cba", "Cbb", "D", "DE", "DK", "Da", "Dd", "De", "Des", "Dfa", "Dfb", "Di", "Dk", "FW", "I", " Na", "Nb", " Nc", "Ncb", "Ncd", " Nd", "Neaq", "Nep", "Neqa", "Neqb", "Nes", "Neu", "Nf", "Ng"
				, "Nh", "P", "PU", "SHI", "T", "VA", "VAC", "VB", "VC", "VCL", "VD", "VE", "VF", "VG", "VH", "VHC", "VI", "VJ", "VK", "VL", "V_2" };
			Extractor[] newW = new Extractor[tagsets.Length];
			for (int k = 0; k < tagsets.Length; k++)
			{
				newW[k] = new CTBunkDictDetector(tagsets[k], n);
			}
			return newW;
		}
	}

	/// <summary>Superclass for rare word feature frames.</summary>
	/// <remarks>
	/// Superclass for rare word feature frames.  Provides some common functions.
	/// Designed to be extended.
	/// </remarks>
	[System.Serializable]
	internal class RareExtractor : Extractor
	{
		internal const string naTag = "NA";

		internal RareExtractor()
			: base()
		{
		}

		internal RareExtractor(int position)
			: base(position, false)
		{
		}

		// end class ExtractorFramesRare
		internal static bool StartsUpperCase(string s)
		{
			if (s == null || s.Length == 0)
			{
				return false;
			}
			char ch = s[0];
			return char.IsUpperCase(ch);
		}

		/// <summary>
		/// A string is lowercase if it starts with a lowercase letter
		/// such as one from a to z.
		/// </summary>
		/// <remarks>
		/// A string is lowercase if it starts with a lowercase letter
		/// such as one from a to z.
		/// Should we include numbers?
		/// </remarks>
		/// <param name="s">The String to check</param>
		/// <returns>If its first character is lower case</returns>
		protected internal static bool StartsLowerCase(string s)
		{
			if (s == null)
			{
				return false;
			}
			char ch = s[0];
			return char.IsLowerCase(ch);
		}

		protected internal static bool ContainsDash(string s)
		{
			return s != null && s.IndexOf('-') >= 0;
		}

		protected internal static bool ContainsNumber(string s)
		{
			if (s == null)
			{
				return false;
			}
			for (int i = 0; i < len; i++)
			{
				if (char.IsDigit(s[i]))
				{
					return true;
				}
			}
			return false;
		}

		protected internal static bool ContainsLetter(string s)
		{
			if (s == null)
			{
				return false;
			}
			for (int i = 0; i < len; i++)
			{
				if (char.IsLetter(s[i]))
				{
					return true;
				}
			}
			return false;
		}

		protected internal static bool ContainsUpperCase(string s)
		{
			if (s == null)
			{
				return false;
			}
			for (int i = 0; i < len; i++)
			{
				if (char.IsUpperCase(s[i]))
				{
					return true;
				}
			}
			return false;
		}

		protected internal static bool AllUpperCase(string s)
		{
			if (s == null)
			{
				return false;
			}
			for (int i = 0; i < len; i++)
			{
				if (!char.IsUpperCase(s[i]))
				{
					return false;
				}
			}
			return true;
		}

		internal static bool NoneLowerCase(string s)
		{
			if (s == null)
			{
				return false;
			}
			for (int i = 0; i < len; i++)
			{
				if (char.IsLowerCase(s[i]))
				{
					return false;
				}
			}
			return true;
		}

		private const long serialVersionUID = -7682607870855426599L;
	}

	/// <summary>English-specific crude company name NER.</summary>
	[System.Serializable]
	internal class CompanyNameDetector : RareExtractor
	{
		internal const int CompanyNameWindow = 3;

		internal readonly ICollection<string> companyNameEnds;

		public CompanyNameDetector()
		{
			// end class RareExtractor
			companyNameEnds = Generics.NewHashSet();
			companyNameEnds.Add("Company");
			companyNameEnds.Add("COMPANY");
			companyNameEnds.Add("Co.");
			companyNameEnds.Add("Co");
			// at end of sentence in PTB
			companyNameEnds.Add("Cos.");
			companyNameEnds.Add("CO.");
			companyNameEnds.Add("COS.");
			companyNameEnds.Add("Corporation");
			companyNameEnds.Add("CORPORATION");
			companyNameEnds.Add("Corp.");
			companyNameEnds.Add("Corp");
			// at end of sentence in PTB
			companyNameEnds.Add("CORP.");
			companyNameEnds.Add("Incorporated");
			companyNameEnds.Add("INCORPORATED");
			companyNameEnds.Add("Inc.");
			companyNameEnds.Add("Inc");
			// at end of sentence in PTB
			companyNameEnds.Add("INC.");
			companyNameEnds.Add("Association");
			companyNameEnds.Add("ASSOCIATION");
			companyNameEnds.Add("Assn");
			companyNameEnds.Add("ASSN");
			companyNameEnds.Add("Limited");
			companyNameEnds.Add("LIMITED");
			companyNameEnds.Add("Ltd.");
			companyNameEnds.Add("LTD.");
			companyNameEnds.Add("L.P.");
		}

		// companyNameEnds.add("PLC"); // Other thing added at same time.
		private bool CompanyNameEnd(string s)
		{
			return companyNameEnds.Contains(s);
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (!StartsUpperCase(s))
			{
				return "0";
			}
			for (int i = 0; i <= CompanyNameWindow; i++)
			{
				string s1 = pH.GetWord(h, i);
				if (CompanyNameEnd(s1))
				{
					return "1";
				}
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 21L;
	}

	[System.Serializable]
	internal class CaselessCompanyNameDetector : RareExtractor
	{
		private readonly ICollection<string> companyNameEnds;

		public CaselessCompanyNameDetector()
		{
			// end class CompanyNameDetector
			companyNameEnds = Generics.NewHashSet();
			CompanyNameDetector cased = new CompanyNameDetector();
			foreach (string name in cased.companyNameEnds)
			{
				companyNameEnds.Add(name.ToLower());
			}
		}

		private bool CompanyNameEnd(string s)
		{
			return companyNameEnds.Contains(s);
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			for (int i = 0; i <= CompanyNameDetector.CompanyNameWindow; i++)
			{
				string s1 = pH.GetWord(h, i);
				if (CompanyNameEnd(s1))
				{
					return "1";
				}
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 21L;
	}

	[System.Serializable]
	internal class ExtractorUCase : RareExtractor
	{
		public ExtractorUCase()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (ContainsUpperCase(s))
			{
				return "1";
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 22L;
	}

	[System.Serializable]
	internal class ExtractorLetterDigitDash : RareExtractor
	{
		public ExtractorLetterDigitDash()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (ContainsLetter(s) && ContainsDash(s) && ContainsNumber(s))
			{
				return "1";
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 23;
	}

	[System.Serializable]
	internal class ExtractorUpperDigitDash : RareExtractor
	{
		public ExtractorUpperDigitDash()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (ContainsUpperCase(s) && ContainsDash(s) && ContainsNumber(s))
			{
				return "1";
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 33L;
	}

	/// <summary>This requires the 3 character classes in order.</summary>
	/// <remarks>This requires the 3 character classes in order.  This was worse than ExtractorLetterDigitDash (Oct 2009)</remarks>
	[System.Serializable]
	internal class ExtractorLetterDashDigit : RareExtractor
	{
		public ExtractorLetterDashDigit()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (s == null)
			{
				return "0";
			}
			bool seenLetter = false;
			bool seenDash = false;
			bool seenNumber = false;
			for (int i = 0; i < len; i++)
			{
				char ch = s[i];
				if (char.IsLetter(ch))
				{
					seenLetter = true;
				}
				else
				{
					if (seenLetter && ch == '-')
					{
						seenDash = true;
					}
					else
					{
						if (seenDash && char.IsDigit(ch))
						{
							seenNumber = true;
							break;
						}
					}
				}
			}
			if (seenNumber)
			{
				return "1";
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 33L;
	}

	/// <summary>
	/// creates features which are true if the current word is all caps
	/// and the distance to the first lowercase word to the left is dist
	/// the distance is 1 for adjacent, 2 for one across, 3 for ...
	/// </summary>
	/// <remarks>
	/// creates features which are true if the current word is all caps
	/// and the distance to the first lowercase word to the left is dist
	/// the distance is 1 for adjacent, 2 for one across, 3 for ... and so on.
	/// infinity if no capitalized word (we hit the start of sentence or '')
	/// </remarks>
	[System.Serializable]
	internal class ExtractorCapDistLC : RareExtractor
	{
		internal bool verbose = false;

		public ExtractorCapDistLC()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string word = pH.GetWord(h, 0);
			string ret;
			if (!StartsUpperCase(word))
			{
				if (verbose)
				{
					System.Console.Out.WriteLine("did not apply because not start with upper case");
				}
				return "0";
			}
			if (AllUpperCase(word))
			{
				ret = "all:";
			}
			else
			{
				ret = "start";
			}
			//now find the distance
			int current = -1;
			int distance = 1;
			while (true)
			{
				string prevWord = pH.GetWord(h, current);
				if (StartsLowerCase(prevWord))
				{
					if (verbose)
					{
						System.Console.Out.WriteLine("returning " + (ret + current) + "for " + word + ' ' + prevWord);
					}
					return ret + distance;
				}
				if (prevWord.Equals(naTag) || prevWord.Equals("``"))
				{
					if (verbose)
					{
						System.Console.Out.WriteLine("returning " + ret + "infinity for " + word + ' ' + prevWord);
					}
					return ret + "infinity";
				}
				current--;
				distance++;
			}
		}

		public override bool IsDynamic()
		{
			return false;
		}

		public override bool IsLocal()
		{
			return false;
		}

		private const long serialVersionUID = 34L;
	}

	/// <summary>
	/// This feature applies when the word is capitalized
	/// and the previous lower case is infinity
	/// and the lower cased version of it has occured 2 or more times with tag t
	/// false if the word was not seen.
	/// </summary>
	/// <remarks>
	/// This feature applies when the word is capitalized
	/// and the previous lower case is infinity
	/// and the lower cased version of it has occured 2 or more times with tag t
	/// false if the word was not seen.
	/// create features only for tags that are the same as the tag t
	/// </remarks>
	[System.Serializable]
	internal class ExtractorCapLCSeen : RareExtractor
	{
		internal readonly string tag;

		internal int cutoff = 1;

		private readonly Extractor cCapDist = new ExtractorCapDistLC();

		[System.NonSerialized]
		private Dictionary dict;

		internal ExtractorCapLCSeen(string tag)
		{
			this.tag = tag;
		}

		protected internal override void SetGlobalHolder(MaxentTagger tagger)
		{
			this.dict = tagger.dict;
		}

		public override bool Precondition(string tag1)
		{
			return tag.Equals(tag1);
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string res = cCapDist.Extract(h, pH);
			if (res.Equals("0"))
			{
				return res;
			}
			//otherwise it is capitalized
			string word = pH.GetWord(h, 0);
			if (dict.GetCount(word, tag) > cutoff)
			{
				return res + tag;
			}
			else
			{
				return "0";
			}
		}

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 35L;
	}

	/// <summary>"1" if not first word of sentence and _some_ letter is uppercase</summary>
	[System.Serializable]
	internal class ExtractorMidSentenceCap : RareExtractor
	{
		public ExtractorMidSentenceCap()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string prevTag = pH.GetTag(h, -1);
			if (prevTag == null)
			{
				return "0";
			}
			if (prevTag.Equals(naTag))
			{
				return "0";
			}
			string s = pH.GetWord(h, 0);
			if (ContainsUpperCase(s))
			{
				return "1";
			}
			return "0";
		}

		private const long serialVersionUID = 24L;

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return true;
		}
	}

	/// <summary>
	/// "0" if not 1st word of sentence or not upper case, or lowercased version
	/// not in dictionary.
	/// </summary>
	/// <remarks>
	/// "0" if not 1st word of sentence or not upper case, or lowercased version
	/// not in dictionary.  Else first tag of word lowercased.
	/// </remarks>
	[System.Serializable]
	internal class ExtractorStartSentenceCap : RareExtractor
	{
		[System.NonSerialized]
		private Dictionary dict;

		public ExtractorStartSentenceCap()
		{
		}

		protected internal override void SetGlobalHolder(MaxentTagger tagger)
		{
			this.dict = tagger.dict;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string prevTag = pH.GetTag(h, -1);
			if (prevTag == null)
			{
				return zeroSt;
			}
			if (!prevTag.Equals(naTag))
			{
				return zeroSt;
			}
			string s = pH.GetWord(h, 0);
			if (StartsUpperCase(s))
			{
				string s1 = s.ToLower();
				if (dict.IsUnknown(s1))
				{
					return zeroSt;
				}
				return dict.GetFirstTag(s1);
			}
			return zeroSt;
		}

		private const long serialVersionUID = 25L;

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return true;
		}
	}

	/// <summary>
	/// "0" if first word of sentence or not first letter uppercase or if
	/// lowercase version isn't in dictionary.
	/// </summary>
	/// <remarks>
	/// "0" if first word of sentence or not first letter uppercase or if
	/// lowercase version isn't in dictionary.  Otherwise first tag of lowercase
	/// equivalent.
	/// </remarks>
	[System.Serializable]
	internal class ExtractorMidSentenceCapC : RareExtractor
	{
		[System.NonSerialized]
		private Dictionary dict;

		public ExtractorMidSentenceCapC()
		{
		}

		protected internal override void SetGlobalHolder(MaxentTagger tagger)
		{
			this.dict = tagger.dict;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string prevTag = pH.GetTag(h, -1);
			if (prevTag == null)
			{
				return zeroSt;
			}
			if (prevTag.Equals(naTag))
			{
				return zeroSt;
			}
			string s = pH.GetWord(h, 0);
			if (StartsUpperCase(s))
			{
				string s1 = s.ToLower();
				if (dict.IsUnknown(s1))
				{
					return zeroSt;
				}
				return dict.GetFirstTag(s1);
			}
			return zeroSt;
		}

		private const long serialVersionUID = 26L;

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return true;
		}
	}

	[System.Serializable]
	internal class ExtractorCapC : RareExtractor
	{
		[System.NonSerialized]
		private Dictionary dict;

		public ExtractorCapC()
		{
		}

		protected internal override void SetGlobalHolder(MaxentTagger tagger)
		{
			this.dict = tagger.dict;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (StartsUpperCase(s))
			{
				string s1 = s.ToLower();
				if (dict.IsUnknown(s1))
				{
					return zeroSt;
				}
				return dict.GetFirstTag(s1);
			}
			return zeroSt;
		}

		private const long serialVersionUID = 26L;

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorAllCap : RareExtractor
	{
		public ExtractorAllCap()
		{
		}

		// TODO: the next time we have to rebuild the tagger files anyway, we
		// should change this class's name to something like
		// "ExtractorNoLowercase" to distinguish it from
		// ExtractorAllCapitalized
		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (NoneLowerCase(s))
			{
				return "1";
			}
			return "0";
		}

		private const long serialVersionUID = 27L;

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorAllCapitalized : RareExtractor
	{
		public ExtractorAllCapitalized()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (AllUpperCase(s))
			{
				return "1";
			}
			return "0";
		}

		private const long serialVersionUID = 32L;

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorCNumber : RareExtractor
	{
		public ExtractorCNumber()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (ContainsNumber(s))
			{
				return "1";
			}
			return "0";
		}

		private const long serialVersionUID = 28L;

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorDash : RareExtractor
	{
		public ExtractorDash()
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (ContainsDash(s))
			{
				return "1";
			}
			return "0";
		}

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		private const long serialVersionUID = 29L;
	}

	[System.Serializable]
	internal class ExtractorWordSuff : RareExtractor
	{
		private readonly int num;

		private readonly int position;

		internal ExtractorWordSuff(int num, int position)
		{
			// todo [cdm 2013]: position field in this class could be deleted and use super's position. But will break
			this.num = num;
			this.position = position;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			// String word = TestSentence.toNice(pH.getWord(h, 0));
			string word = pH.GetWord(h, position);
			if (word.Length < num)
			{
				return "######";
			}
			return Sharpen.Runtime.Substring(word, word.Length - num);
		}

		private const long serialVersionUID = 724767436530L;

		public override string ToString()
		{
			return StringUtils.GetShortClassName(this) + "(len" + num + ",w" + position + ")";
		}

		public override bool IsLocal()
		{
			return (position == 0);
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorWordPref : RareExtractor
	{
		private readonly int num;

		private readonly int position;

		internal ExtractorWordPref(int num, int position)
		{
			// todo [cdm 2013]: position field in this class could be deleted and use super's position. But will break
			this.num = num;
			this.position = position;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			// String word = TestSentence.toNice(pH.getWord(h, 0));
			string word = pH.GetWord(h, position);
			if (word.Length < num)
			{
				return "######";
			}
			else
			{
				return Sharpen.Runtime.Substring(word, 0, num);
			}
		}

		private const long serialVersionUID = 724767436531L;

		public override string ToString()
		{
			return StringUtils.GetShortClassName(this) + "(len" + num + ",w" + position + ")";
		}

		public override bool IsLocal()
		{
			return (position == 0);
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorsConjunction : RareExtractor
	{
		private readonly Extractor extractor1;

		private readonly Extractor extractor2;

		internal volatile bool isLocal;

		internal volatile bool isDynamic;

		internal ExtractorsConjunction(Extractor e1, Extractor e2)
		{
			// end class ExtractorWordPref
			extractor1 = e1;
			extractor2 = e2;
			isLocal = e1.IsLocal() && e2.IsLocal();
			isDynamic = e1.IsDynamic() || e2.IsDynamic();
		}

		protected internal override void SetGlobalHolder(MaxentTagger tagger)
		{
			extractor1.SetGlobalHolder(tagger);
			extractor2.SetGlobalHolder(tagger);
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string ex1 = extractor1.Extract(h, pH);
			if (ex1.Equals(zeroSt))
			{
				return zeroSt;
			}
			string ex2 = extractor2.Extract(h, pH);
			if (ex2.Equals(zeroSt))
			{
				return zeroSt;
			}
			return ex1 + ':' + ex2;
		}

		private const long serialVersionUID = 36L;

		public override bool IsLocal()
		{
			return isLocal;
		}

		public override bool IsDynamic()
		{
			return isDynamic;
		}

		public override string ToString()
		{
			return StringUtils.GetShortClassName(this) + '(' + extractor1 + ',' + extractor2 + ')';
		}
	}

	[System.Serializable]
	internal class PluralAcronymDetector : RareExtractor
	{
		public PluralAcronymDetector()
		{
		}

		private static bool PluralAcronym(string s)
		{
			int len = s.Length;
			len--;
			if (s[len] != 's')
			{
				return false;
			}
			for (int i = 0; i < len; i++)
			{
				if (!char.IsUpperCase(s[i]))
				{
					return false;
				}
			}
			return true;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = pH.GetWord(h, 0);
			if (PluralAcronym(s))
			{
				return "1";
			}
			return "0";
		}

		private const long serialVersionUID = 33L;

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class CtbPreDetector : RareExtractor
	{
		private string t1;

		internal CtbPreDetector(string t2, int n2)
			: base(n2)
		{
			t1 = t2;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = TestSentence.ToNice(pH.GetWord(h, position));
			if (!s.Equals(string.Empty) && CtbDict.GetTagPre(t1, Sharpen.Runtime.Substring(s, 0, 1)).Equals("1"))
			{
				return "1:" + t1;
			}
			return "0:" + t1;
		}

		private const long serialVersionUID = 43L;

		public override string ToString()
		{
			return base.ToString() + " tag=" + t1;
		}

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class CtbSufDetector : RareExtractor
	{
		private string t1;

		internal CtbSufDetector(string t2, int n2)
			: base(n2)
		{
			// end class ctbPreDetector
			t1 = t2;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = TestSentence.ToNice(pH.GetWord(h, position));
			if (!s.Equals(string.Empty) && CtbDict.GetTagSuf(t1, Sharpen.Runtime.Substring(s, s.Length - 1, s.Length)).Equals("1"))
			{
				return "1:" + t1;
			}
			return "0:" + t1;
		}

		private const long serialVersionUID = 44L;

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}

		public override string ToString()
		{
			return base.ToString() + " tag=" + t1;
		}
	}

	[System.Serializable]
	internal class ASBCunkDetector : RareExtractor
	{
		private string t1;

		private int n1;

		internal ASBCunkDetector(string t2, int n2)
		{
			// end class ctbPreDetector
			/*
			class ASBCPreDetector extends RareExtractor {
			private String t1;
			private int n1;
			public ASBCPreDetector(String t2, int n2) {
			t1=t2;
			n1=n2;
			}
			
			@Override
			String extract(History h, PairsHolder pH) {
			String s=TestSentence.toNice(pH.get(h,n1,false));
			
			if(!s.equals("") && ASBCDict.getTagPre(t1, s.substring(0, 1)).equals("1"))
			return "1:"+t1;
			return "0:"+t1;
			}
			private static final long serialVersionUID = 53L;
			} // end class ASBCPreDetector
			
			class ASBCSufDetector extends RareExtractor {
			private String t1;
			private int n1;
			public ASBCSufDetector(String t2, int n2) {
			t1=t2;
			n1=n2;
			}
			
			@Override
			String extract(History h, PairsHolder pH) {
			String s=TestSentence.toNice(pH.get(h,n1,false));
			if (!s.equals("") && ASBCDict.getTagSuf(t1, s.substring(s.length()-1, s.length())).equals("1"))
			return "1:"+t1;
			return "0:"+t1;
			}
			private static final long serialVersionUID = 54L;
			} // end class ASBCPreDetector
			*/
			t1 = t2;
			n1 = n2;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = TestSentence.ToNice(pH.GetWord(h, n1));
			if (ASBCunkDict.GetTag(t1, s).Equals("1"))
			{
				return "1:" + t1;
			}
			return "0:" + t1;
		}

		private const long serialVersionUID = 57L;

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class CTBunkDictDetector : RareExtractor
	{
		private string t1;

		private int n1;

		internal CTBunkDictDetector(string t2, int n2)
		{
			// end class ASBCunkDetector
			t1 = t2;
			n1 = n2;
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = TestSentence.ToNice(pH.GetWord(h, n1));
			if (CTBunkDict.GetTag(t1, s).Equals("1"))
			{
				return "1:" + t1;
			}
			return "0:" + t1;
		}

		private const long serialVersionUID = 80L;

		public override bool IsLocal()
		{
			return false;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal abstract class CWordBooleanExtractor : RareExtractor
	{
		// end class CTBunkDictDetector
		internal override string Extract(History h, PairsHolder pH)
		{
			string cword = pH.GetWord(h, 0);
			return ExtractFeature(cword) ? "1" : "0";
		}

		internal abstract bool ExtractFeature(string cword);

		public override bool IsLocal()
		{
			return true;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	[System.Serializable]
	internal class ExtractorFrenchNounSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = 848772358776880060L;

		internal override bool ExtractFeature(string cword)
		{
			return FrenchUnknownWordSignatures.HasNounSuffix(cword);
		}
	}

	[System.Serializable]
	internal class ExtractorFrenchAdvSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = 9141591417435848689L;

		internal override bool ExtractFeature(string cword)
		{
			return FrenchUnknownWordSignatures.HasAdvSuffix(cword);
		}
	}

	[System.Serializable]
	internal class ExtractorFrenchVerbSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = -1762307766086637191L;

		internal override bool ExtractFeature(string cword)
		{
			return FrenchUnknownWordSignatures.HasVerbSuffix(cword);
		}
	}

	[System.Serializable]
	internal class ExtractorFrenchAdjSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = -5838046941039275411L;

		internal override bool ExtractFeature(string cword)
		{
			return FrenchUnknownWordSignatures.HasAdjSuffix(cword);
		}
	}

	[System.Serializable]
	internal class ExtractorFrenchPluralSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = 1139695807527192176L;

		internal override bool ExtractFeature(string cword)
		{
			return FrenchUnknownWordSignatures.HasPossiblePlural(cword);
		}
	}

	[System.Serializable]
	internal class ExtractorSpanishGender : RareExtractor
	{
		private const long serialVersionUID = -7359312929174070404L;

		internal override string Extract(History h, PairsHolder pH)
		{
			string cword = pH.GetWord(h, 0);
			if (SpanishUnknownWordSignatures.HasMasculineSuffix(cword))
			{
				return "m";
			}
			else
			{
				if (SpanishUnknownWordSignatures.HasFeminineSuffix(cword))
				{
					return "f";
				}
				else
				{
					return string.Empty;
				}
			}
		}
	}

	[System.Serializable]
	internal class ExtractorSpanishConditionalSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = 4383251116043848632L;

		internal override bool ExtractFeature(string cword)
		{
			return SpanishUnknownWordSignatures.HasConditionalSuffix(cword);
		}
	}

	[System.Serializable]
	internal class ExtractorSpanishImperfectErIrSuffix : CWordBooleanExtractor
	{
		private const long serialVersionUID = -5804047931816433075L;

		internal override bool ExtractFeature(string cword)
		{
			return SpanishUnknownWordSignatures.HasImperfectErIrSuffix(cword);
		}
	}
}

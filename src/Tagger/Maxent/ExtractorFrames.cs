//ExtractorFrames -- StanfordMaxEnt, A Maximum Entropy Toolkit
//Copyright (c) 2002-2011 Leland Stanford Junior University
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
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// This class contains the basic feature extractors used for all words and
	/// tag sequences (and interaction terms) for the MaxentTagger, but not the
	/// feature extractors explicitly targeting generalization for rare or unknown
	/// words.
	/// </summary>
	/// <remarks>
	/// This class contains the basic feature extractors used for all words and
	/// tag sequences (and interaction terms) for the MaxentTagger, but not the
	/// feature extractors explicitly targeting generalization for rare or unknown
	/// words.
	/// The following options are supported:
	/// <table>
	/// <tr><td>Name</td><td>Args</td><td>Effect</td></tr>
	/// <tr><td>words</td><td>begin, end</td>
	/// <td>Individual features for words begin ... end.
	/// If just one argument words(-2) is given, then end is taken as 0. If
	/// begin is not less than or equal to end, no features are made.</td></tr>
	/// <tr><td>tags</td><td>begin, end</td>
	/// <td>Individual features for tags begin ... end</td></tr>
	/// <tr><td>biword</td><td>w1, w2</td>
	/// <td>One feature for the pair of words w1, w2</td></tr>
	/// <tr><td>biwords</td><td>begin, end</td>
	/// <td>One feature for each sequential pair of words
	/// from begin to end</td></tr>
	/// <tr><td>twoTags</td><td>t1, t2</td>
	/// <td>One feature for the pair of tags t1, t2</td></tr>
	/// <tr><td>lowercasewords</td><td>begin, end</td>
	/// <td>One feature for each word begin ... end, lowercased</td></tr>
	/// <tr><td>order</td><td>left, right</td>
	/// <td>A feature for tags left through 0 and a feature for
	/// tags 0 through right.  Lower order left and right features are
	/// also added.
	/// This gets very expensive for higher order terms.</td></tr>
	/// <tr><td>wordTag</td><td>w, t</td>
	/// <td>A feature combining word w and tag t.</td></tr>
	/// <tr><td>wordTwoTags</td><td>w, t1, t2</td>
	/// <td>A feature combining word w and tags t1, t2.</td></tr>
	/// <tr><td>threeTags</td><td>t1, t2, t3</td>
	/// <td>A feature combining tags t1, t2, t3.</td></tr>
	/// <tr><td>vbn</td><td>length</td>
	/// <td>A feature that looks at the left length words for something that
	/// appears to be a VBN (in English) without looking at the actual tags.
	/// It is zeroeth order, as it does not look at the tag predictions.
	/// It also is never used, since it doesn't seem to help.</td></tr>
	/// <tr><td>allwordshapes</td><td>left, right</td>
	/// <td>Word shape features, eg transform Foo5 into Xxx#
	/// (not exactly like that, but that general idea).
	/// Creates individual features for each word left ... right.
	/// Compare with the feature "wordshapes" in ExtractorFramesRare,
	/// which is only applied to rare words. Fairly English-specific.
	/// Slightly increases accuracy.</td></tr>
	/// <tr><td>allunicodeshapes</td><td>left, right</td>
	/// <td>Same thing, but works for unicode characters more generally.</td></tr>
	/// <tr><td>allunicodeshapeconjunction</td><td>left, right</td>
	/// <td>Instead of individual word shape features, combines several
	/// word shapes into one feature.</td></tr>
	/// </table>
	/// See
	/// <see cref="ExtractorFramesRare"/>
	/// for more options.
	/// <br />
	/// There are also macro features:
	/// <br />
	/// left3words = words(-1,1),order(2) <br />
	/// left5words = words(-2,2),order(2) <br />
	/// generic = words(-1,1),order(2),biwords(-1,0),wordTag(0,-1) <br />
	/// bidirectional5words =
	/// words(-2,2),order(-2,2),twoTags(-1,1),
	/// wordTag(0,-1),wordTag(0,1),biwords(-1,1) <br />
	/// bidirectional =
	/// words(-1,1),order(-2,2),twoTags(-1,1),
	/// wordTag(0,-1),wordTag(0,1),biwords(-1,1) <br />
	/// german = some random stuff <br />
	/// sighan2005 = some other random stuff <br />
	/// The left3words architectures are faster, but slightly less
	/// accurate, than the bidirectional architectures.
	/// 'naacl2003unknowns' was our traditional set of unknown word
	/// features, but you can now specify features more flexibility via the
	/// various other supported keywords.
	/// <br />
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <author>Michel Galley</author>
	/// <version>1.0</version>
	public class ExtractorFrames
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.ExtractorFrames));

		internal static readonly Extractor cWord = new Extractor(0, false);

		private static readonly Extractor prevWord = new Extractor(-1, false);

		private static readonly Extractor prevTag = new Extractor(-1, true);

		private static readonly Extractor prevTagWord = new ExtractorFrames.ExtractorWordTag(0, -1);

		private static readonly Extractor prevWord2 = new Extractor(-2, false);

		private static readonly Extractor prevTwoTag = new Extractor(-2, true);

		private static readonly Extractor nextWord = new Extractor(1, false);

		private static readonly Extractor nextWord2 = new Extractor(2, false);

		private static readonly Extractor nextTag = new Extractor(1, true);

		private static readonly Extractor[] eFrames_sighan2005 = new Extractor[] { cWord, prevWord, prevWord2, nextWord, nextWord2, prevTag, prevTwoTag, new ExtractorFrames.ExtractorContinuousTagConjunction(-2) };

		private static readonly Extractor[] eFrames_german = new Extractor[] { cWord, prevWord, nextWord, nextTag, prevTag, new ExtractorFrames.ExtractorContinuousTagConjunction(-2), prevTagWord, new ExtractorFrames.ExtractorTwoWords(-1, 0) };

		/// <summary>This class is not meant to be instantiated.</summary>
		private ExtractorFrames()
		{
		}

		// all features are implicitly conjoined with the current tag
		// prev tag and current word!
		// features for 2005 SIGHAN tagger
		// features for a german-language bidirectional tagger
		protected internal static Extractor[] GetExtractorFrames(string arch)
		{
			// handle some traditional macro options
			// left3words: a simple trigram CMM tagger (similar to the baseline EMNLP 2000 tagger)
			// left5words: a simple trigram CMM tagger, like left3words, with 5 word context
			// generic: our standard multilingual CMM baseline
			arch = arch.ReplaceAll("left3words", "words(-1,1),order(2)");
			arch = arch.ReplaceAll("left5words", "words(-2,2),order(2)");
			arch = arch.ReplaceAll("generic", "words(-1,1),order(2),biwords(-1,0),wordTag(0,-1)");
			arch = arch.ReplaceAll("bidirectional5words", "words(-2,2),order(-2,2),twoTags(-1,1),wordTag(0,-1),wordTag(0,1),biwords(-1,1)");
			arch = arch.ReplaceAll("bidirectional", "words(-1,1),order(-2,2),twoTags(-1,1),wordTag(0,-1),wordTag(0,1),biwords(-1,1)");
			List<Extractor> extrs = new List<Extractor>();
			IList<string> args = StringUtils.ValueSplit(arch, "[a-zA-Z0-9]*(?:\\([^)]*\\))?", "\\s*,\\s*");
			foreach (string arg in args)
			{
				if (arg.Equals("sighan2005"))
				{
					Sharpen.Collections.AddAll(extrs, Arrays.AsList(eFrames_sighan2005));
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(arg, "german"))
					{
						Sharpen.Collections.AddAll(extrs, Arrays.AsList(eFrames_german));
					}
					else
					{
						if (arg.StartsWith("words("))
						{
							// non-sequence features with just a certain number of words to the
							// left and right; e.g., words(-2,2) or words(-2,-1)
							int lWindow = Extractor.GetParenthesizedNum(arg, 1);
							int rWindow = Extractor.GetParenthesizedNum(arg, 2);
							for (int i = lWindow; i <= rWindow; i++)
							{
								extrs.Add(new Extractor(i, false));
							}
						}
						else
						{
							if (arg.StartsWith("tags("))
							{
								// non-sequence features with just a certain number of words to the
								// left and right; e.g., tags(-2,2) or tags(-2,-1)
								int lWindow = Extractor.GetParenthesizedNum(arg, 1);
								int rWindow = Extractor.GetParenthesizedNum(arg, 2);
								for (int i = lWindow; i <= rWindow; i++)
								{
									extrs.Add(new Extractor(i, true));
								}
							}
							else
							{
								if (arg.StartsWith("biwords("))
								{
									// non-sequence features of word pairs.
									// biwords(-2,1) would give you 3 extractors for w-2w-1, w-1,w0, w0w1
									int lWindow = Extractor.GetParenthesizedNum(arg, 1);
									int rWindow = Extractor.GetParenthesizedNum(arg, 2);
									for (int i = lWindow; i < rWindow; i++)
									{
										extrs.Add(new ExtractorFrames.ExtractorTwoWords(i));
									}
								}
								else
								{
									if (arg.StartsWith("biword("))
									{
										// non-sequence feature of a word pair.
										// biwords(-2,1) would give you 1 extractor for w-2, w+1
										int left = Extractor.GetParenthesizedNum(arg, 1);
										int right = Extractor.GetParenthesizedNum(arg, 2);
										extrs.Add(new ExtractorFrames.ExtractorTwoWords(left, right));
									}
									else
									{
										if (arg.StartsWith("twoTags("))
										{
											// non-sequence feature of a tag pair.
											// twoTags(-2,1) would give you 1 extractor for t-2, t+1
											int left = Extractor.GetParenthesizedNum(arg, 1);
											int right = Extractor.GetParenthesizedNum(arg, 2);
											extrs.Add(new ExtractorFrames.ExtractorTwoTags(left, right));
										}
										else
										{
											if (arg.StartsWith("lowercasewords("))
											{
												// non-sequence features with just a certain number of lowercase words
												// to the left and right
												int lWindow = Extractor.GetParenthesizedNum(arg, 1);
												int rWindow = Extractor.GetParenthesizedNum(arg, 2);
												for (int i = lWindow; i <= rWindow; i++)
												{
													extrs.Add(new ExtractorFrames.ExtractorWordLowerCase(i));
												}
											}
											else
											{
												if (arg.StartsWith("order("))
												{
													// anything like order(2), order(-4), order(0,3), or
													// order(-2,1) are okay.
													int leftOrder = Extractor.GetParenthesizedNum(arg, 1);
													int rightOrder = Extractor.GetParenthesizedNum(arg, 2);
													if (leftOrder > 0)
													{
														leftOrder = -leftOrder;
													}
													if (rightOrder < 0)
													{
														throw new ArgumentException("Right order must be non-negative, not " + rightOrder);
													}
													// cdm 2009: We only add successively higher order tag k-grams
													// ending adjacent to t0.  Adding lower order features at a distance
													// appears not to help (Dec 2009). But they can now be added with tags().
													for (int idx = leftOrder; idx <= rightOrder; idx++)
													{
														if (idx == 0)
														{
														}
														else
														{
															// do nothing
															if (idx == -1 || idx == 1)
															{
																extrs.Add(new Extractor(idx, true));
															}
															else
															{
																extrs.Add(new ExtractorFrames.ExtractorContinuousTagConjunction(idx));
															}
														}
													}
												}
												else
												{
													if (arg.StartsWith("wordTag("))
													{
														// sequence feature of a word and a tag: wordTag(-1,1)
														int posW = Extractor.GetParenthesizedNum(arg, 1);
														int posT = Extractor.GetParenthesizedNum(arg, 2);
														extrs.Add(new ExtractorFrames.ExtractorWordTag(posW, posT));
													}
													else
													{
														if (arg.StartsWith("wordTwoTags("))
														{
															int word = Extractor.GetParenthesizedNum(arg, 1);
															int tag1 = Extractor.GetParenthesizedNum(arg, 2);
															int tag2 = Extractor.GetParenthesizedNum(arg, 3);
															extrs.Add(new ExtractorFrames.ExtractorWordTwoTags(word, tag1, tag2));
														}
														else
														{
															if (arg.StartsWith("threeTags("))
															{
																int pos1 = Extractor.GetParenthesizedNum(arg, 1);
																int pos2 = Extractor.GetParenthesizedNum(arg, 2);
																int pos3 = Extractor.GetParenthesizedNum(arg, 3);
																extrs.Add(new ExtractorFrames.ExtractorThreeTags(pos1, pos2, pos3));
															}
															else
															{
																if (arg.StartsWith("vbn("))
																{
																	int order = Extractor.GetParenthesizedNum(arg, 1);
																	extrs.Add(new ExtractorVerbalVBNZero(order));
																}
																else
																{
																	if (arg.StartsWith("allwordshapes("))
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
																		if (arg.StartsWith("allwordshapeconjunction("))
																		{
																			int lWindow = Extractor.GetParenthesizedNum(arg, 1);
																			int rWindow = Extractor.GetParenthesizedNum(arg, 2);
																			string wsc = Extractor.GetParenthesizedArg(arg, 3);
																			if (wsc == null)
																			{
																				wsc = "chris2";
																			}
																			extrs.Add(new ExtractorWordShapeConjunction(lWindow, rWindow, wsc));
																		}
																		else
																		{
																			if (arg.StartsWith("allunicodeshapes("))
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
																				if (arg.StartsWith("allunicodeshapeconjunction("))
																				{
																					int lWindow = Extractor.GetParenthesizedNum(arg, 1);
																					int rWindow = Extractor.GetParenthesizedNum(arg, 2);
																					extrs.Add(new ExtractorWordShapeConjunction(lWindow, rWindow, "chris4"));
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(arg, "spanishauxiliaries"))
																					{
																						extrs.Add(new ExtractorSpanishAuxiliaryTag());
																						extrs.Add(new ExtractorSpanishSemiauxiliaryTag());
																					}
																					else
																					{
																						if (Sharpen.Runtime.EqualsIgnoreCase(arg, "naacl2003unknowns") || Sharpen.Runtime.EqualsIgnoreCase(arg, "lnaacl2003unknowns") || Sharpen.Runtime.EqualsIgnoreCase(arg, "caselessnaacl2003unknowns") || Sharpen.Runtime.EqualsIgnoreCase(arg, "naacl2003conjunctions"
																							) || Sharpen.Runtime.EqualsIgnoreCase(arg, "frenchunknowns") || Sharpen.Runtime.EqualsIgnoreCase(arg, "spanishunknowns") || arg.StartsWith("wordshapes(") || arg.StartsWith("wordshapeconjunction(") || Sharpen.Runtime.EqualsIgnoreCase(arg, "motleyUnknown"
																							) || arg.StartsWith("suffix(") || arg.StartsWith("prefix(") || arg.StartsWith("prefixsuffix") || arg.StartsWith("capitalizationsuffix(") || arg.StartsWith("distsim(") || arg.StartsWith("distsimconjunction(") || Sharpen.Runtime.EqualsIgnoreCase
																							(arg, "lctagfeatures") || arg.StartsWith("unicodeshapes(") || arg.StartsWith("chinesedictionaryfeatures(") || arg.StartsWith("unicodeshapeconjunction("))
																						{
																						}
																						else
																						{
																							// okay; known unknown keyword
																							log.Info("Unrecognized ExtractorFrames identifier (ignored): " + arg);
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
			}
			// end for
			return Sharpen.Collections.ToArray(extrs, new Extractor[extrs.Count]);
		}

		/// <summary>This extractor extracts a word and tag in conjunction.</summary>
		[System.Serializable]
		internal class ExtractorWordTag : Extractor
		{
			private const long serialVersionUID = 3L;

			private readonly int wordPosition;

			public ExtractorWordTag(int posW, int posT)
				: base(posT, true)
			{
				wordPosition = posW;
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				return pH.GetTag(h, position) + '!' + pH.GetWord(h, wordPosition);
			}

			public override string ToString()
			{
				return (GetType().FullName + "(w" + wordPosition + ",t" + position + ')');
			}
		}

		/// <summary>The word in lower-cased version.</summary>
		/// <remarks>
		/// The word in lower-cased version.
		/// Always uses Locale.ENGLISH.
		/// </remarks>
		[System.Serializable]
		internal class ExtractorWordLowerCase : Extractor
		{
			private const long serialVersionUID = -7847524200422095441L;

			public ExtractorWordLowerCase(int position)
				: base(position, false)
			{
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				return pH.GetWord(h, position).ToLower(Locale.English);
			}
		}

		/// <summary>The current word if it is capitalized, zero otherwise.</summary>
		/// <remarks>
		/// The current word if it is capitalized, zero otherwise.
		/// Always uses Locale.ENGLISH.
		/// </remarks>
		[System.Serializable]
		internal class ExtractorCWordCapCase : Extractor
		{
			private const long serialVersionUID = -2393096135964969744L;

			internal override string Extract(History h, PairsHolder pH)
			{
				string cw = pH.GetWord(h, 0);
				string lk = cw.ToLower(Locale.English);
				if (lk.Equals(cw))
				{
					return zeroSt;
				}
				return cw;
			}

			public override bool IsLocal()
			{
				return true;
			}

			public override bool IsDynamic()
			{
				return false;
			}
		}

		/// <summary>This extractor extracts two words in conjunction.</summary>
		/// <remarks>
		/// This extractor extracts two words in conjunction.
		/// The one argument constructor gives you leftPosition and
		/// leftPosition+1, but with the two argument constructor,
		/// they can be any pair of word positions.
		/// </remarks>
		[System.Serializable]
		internal class ExtractorTwoWords : Extractor
		{
			private const long serialVersionUID = -1034112287022504917L;

			private readonly int leftPosition;

			private readonly int rightPosition;

			public ExtractorTwoWords(int leftPosition)
				: this(leftPosition, leftPosition + 1)
			{
			}

			public ExtractorTwoWords(int position1, int position2)
				: base(0, false)
			{
				if (position1 > position2)
				{
					leftPosition = position1;
					rightPosition = position2;
				}
				else
				{
					leftPosition = position2;
					rightPosition = position1;
				}
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				// I ran a bunch of timing tests that seem to indicate it is
				// cheaper to simply add string + char + string than use a
				// StringBuilder or go through the StringBuildMemoizer -horatio
				return pH.GetWord(h, leftPosition) + '!' + pH.GetWord(h, rightPosition);
			}

			public override bool IsLocal()
			{
				return false;
			}

			// isDynamic --> false, but no need to override
			public override string ToString()
			{
				return (GetType().FullName + "(w" + leftPosition + ",w" + rightPosition + ')');
			}
		}

		/// <summary>This extractor extracts two tags in conjunction.</summary>
		/// <remarks>
		/// This extractor extracts two tags in conjunction.
		/// The one argument constructor gives you leftPosition and
		/// leftPosition+1, but with the two argument constructor,
		/// they can be any pair of tag positions.
		/// </remarks>
		[System.Serializable]
		internal class ExtractorTwoTags : Extractor
		{
			private const long serialVersionUID = -7342144764725605134L;

			private readonly int leftPosition;

			private readonly int rightPosition;

			private readonly int leftContext;

			private readonly int rightContext;

			public ExtractorTwoTags(int position1, int position2)
			{
				leftPosition = Math.Min(position1, position2);
				rightPosition = Math.Max(position1, position2);
				leftContext = -Math.Min(leftPosition, 0);
				rightContext = Math.Max(rightPosition, 0);
			}

			public override int RightContext()
			{
				return rightContext;
			}

			public override int LeftContext()
			{
				return leftContext;
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				// I ran a bunch of timing tests that seem to indicate it is
				// cheaper to simply add string + char + string than use a
				// StringBuilder or go through the StringBuildMemoizer -horatio
				return pH.GetTag(h, leftPosition) + '!' + pH.GetTag(h, rightPosition);
			}

			public override bool IsLocal()
			{
				return false;
			}

			public override bool IsDynamic()
			{
				return true;
			}

			public override string ToString()
			{
				return (GetType().FullName + "(t" + leftPosition + ",t" + rightPosition + ')');
			}
		}

		/// <summary>This extractor extracts two words and a tag in conjunction.</summary>
		[System.Serializable]
		internal class ExtractorTwoWordsTag : Extractor
		{
			private const long serialVersionUID = 277004119652781188L;

			private readonly int leftWord;

			private readonly int rightWord;

			private readonly int tag;

			private readonly int rightContext;

			private readonly int leftContext;

			public ExtractorTwoWordsTag(int leftWord, int rightWord, int tag)
			{
				this.leftWord = Math.Min(leftWord, rightWord);
				this.rightWord = Math.Max(leftWord, rightWord);
				this.tag = tag;
				this.rightContext = Math.Max(tag, 0);
				this.leftContext = -Math.Min(tag, 0);
			}

			public override int RightContext()
			{
				return rightContext;
			}

			public override int LeftContext()
			{
				return leftContext;
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				return (pH.GetWord(h, leftWord) + '!' + pH.GetTag(h, tag) + '!' + pH.GetWord(h, rightWord));
			}

			public override bool IsLocal()
			{
				return false;
			}

			public override bool IsDynamic()
			{
				return true;
			}

			public override string ToString()
			{
				return (GetType().FullName + "(w" + leftWord + ",t" + tag + ",w" + rightWord + ')');
			}
		}

		/// <summary>This extractor extracts several contiguous tags only on one side of position 0.</summary>
		/// <remarks>
		/// This extractor extracts several contiguous tags only on one side of position 0.
		/// E.g., use constructor argument -3 for an order 3 predictor on the left.
		/// isLocal=false, isDynamic=true (through super call)
		/// </remarks>
		[System.Serializable]
		internal class ExtractorContinuousTagConjunction : Extractor
		{
			private const long serialVersionUID = 3;

			public ExtractorContinuousTagConjunction(int maxPosition)
				: base(maxPosition, true)
			{
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				StringBuilder sb = new StringBuilder();
				if (position < 0)
				{
					for (int idx = position; idx < 0; idx++)
					{
						if (idx != position)
						{
							sb.Append('!');
						}
						sb.Append(pH.GetTag(h, idx));
					}
				}
				else
				{
					for (int idx = position; idx > 0; idx--)
					{
						if (idx != position)
						{
							sb.Append('!');
						}
						sb.Append(pH.GetTag(h, idx));
					}
				}
				return sb.ToString();
			}
		}

		/// <summary>This extractor extracts three tags.</summary>
		[System.Serializable]
		internal class ExtractorThreeTags : Extractor
		{
			private const long serialVersionUID = 8563584394721620568L;

			private int position1;

			private int position2;

			private int position3;

			public ExtractorThreeTags(int position1, int position2, int position3)
			{
				// bubblesort them!
				int x;
				if (position1 > position2)
				{
					x = position2;
					position2 = position1;
					position1 = x;
				}
				if (position2 > position3)
				{
					x = position3;
					position3 = position2;
					position2 = x;
				}
				if (position1 > position2)
				{
					x = position2;
					position2 = position1;
					position1 = x;
				}
				this.position1 = position1;
				this.position2 = position2;
				this.position3 = position3;
			}

			public override int RightContext()
			{
				if (position3 > 0)
				{
					return position3;
				}
				else
				{
					return 0;
				}
			}

			public override int LeftContext()
			{
				if (position1 < 0)
				{
					return -position1;
				}
				else
				{
					return 0;
				}
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				return pH.GetTag(h, position1) + '!' + pH.GetTag(h, position2) + '!' + pH.GetTag(h, position3);
			}

			public override bool IsLocal()
			{
				return false;
			}

			public override bool IsDynamic()
			{
				return true;
			}

			public override string ToString()
			{
				return (GetType().FullName + "(t" + position1 + ",t" + position2 + ",t" + position3 + ')');
			}
		}

		/// <summary>This extractor extracts two tags and the a word in conjunction.</summary>
		[System.Serializable]
		internal class ExtractorWordTwoTags : Extractor
		{
			private const long serialVersionUID = -4942654091455804176L;

			private int position1;

			private int position2;

			private int word;

			public ExtractorWordTwoTags(int word, int position1, int position2)
			{
				// We sort so that position1 <= position2 and then rely on that.
				if (position1 < position2)
				{
					this.position1 = position1;
					this.position2 = position1;
				}
				else
				{
					this.position1 = position2;
					this.position2 = position1;
				}
				this.word = word;
			}

			public override int LeftContext()
			{
				if (position1 < 0)
				{
					return -position1;
				}
				else
				{
					return 0;
				}
			}

			public override int RightContext()
			{
				if (position2 > 0)
				{
					return position2;
				}
				else
				{
					return 0;
				}
			}

			internal override string Extract(History h, PairsHolder pH)
			{
				return pH.GetTag(h, position1) + '!' + pH.GetWord(h, word) + '!' + pH.GetTag(h, position2);
			}

			public override bool IsLocal()
			{
				return false;
			}

			public override bool IsDynamic()
			{
				return true;
			}

			public override string ToString()
			{
				return (GetType().FullName + "(t" + position1 + ",t" + position2 + ",w" + word + ')');
			}
		}
	}

	[System.Serializable]
	internal class ExtractorWordShapeClassifier : Extractor
	{
		private readonly int wordShaper;

		private readonly string name;

		internal ExtractorWordShapeClassifier(int position, string wsc)
			: base(position, false)
		{
			// end class ExtractorFrames
			// This cache speeds things up a little bit.  I used
			// -Xrunhprof:cpu=samples,interval=1 when using the "distsim" tagger
			// on the training set to measure roughly how much time was spent in
			// this method.  I concluded that with the cache, 1.24% of the time
			// is spent here, and without the cache, 1.26% of the time is spent
			// here.  This is a very small savings, which would be even smaller
			// if we make the cache thread safe.  It turns out that, as written,
			// the cache is not thread safe for various reasons.  In particular,
			// it assumes only one wordshape classifier is ever used, which
			// might not be true even with just one tagger, and has an even
			// higher chance of not being true if there are multiple taggers.
			// Furthermore, access to the cache should really be synchronized
			// regardless.  The easiest solution is to comment out the cache and
			// note that if you want to bring it back, make it a map from wsc to
			// cache rather than just a single cache.  -- horatio
			//private static final Map<String, String> shapes =
			//  Generics.newHashMap();
			// --- should be:
			//private static final Map<String, Map<String, String>> ...
			wordShaper = WordShapeClassifier.LookupShaper(wsc);
			name = "ExtractorWordShapeClassifier(" + position + ',' + wsc + ')';
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string s = base.Extract(h, pH);
			string shape = WordShapeClassifier.WordShape(s, wordShaper);
			return shape;
		}

		private const long serialVersionUID = 101L;

		public override string ToString()
		{
			return name;
		}

		public override bool IsLocal()
		{
			return position == 0;
		}

		public override bool IsDynamic()
		{
			return false;
		}
	}

	/// <summary>This extractor extracts a conjunction of word shapes.</summary>
	[System.Serializable]
	internal class ExtractorWordShapeConjunction : Extractor
	{
		private const long serialVersionUID = -49L;

		private readonly int wordShaper;

		private readonly int left;

		private readonly int right;

		private readonly string name;

		internal ExtractorWordShapeConjunction(int left, int right, string wsc)
			: base()
		{
			this.left = left;
			this.right = right;
			wordShaper = WordShapeClassifier.LookupShaper(wsc);
			name = "ExtractorWordShapeConjunction(" + left + ',' + right + ',' + wsc + ')';
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			StringBuilder sb = new StringBuilder();
			for (int j = left; j <= right; j++)
			{
				string s = pH.GetWord(h, j);
				sb.Append(WordShapeClassifier.WordShape(s, wordShaper));
				if (j < right)
				{
					sb.Append('|');
				}
			}
			return sb.ToString();
		}

		public override string ToString()
		{
			return name;
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

	/// <summary>
	/// Extracts a boolean indicating whether the given word is preceded by
	/// an auxiliary verb.
	/// </summary>
	[System.Serializable]
	internal class ExtractorSpanishAuxiliaryTag : Extractor
	{
		private const long serialVersionUID = -3352770856914897103L;

		public ExtractorSpanishAuxiliaryTag()
			: base(-1, true)
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string tag = base.Extract(h, pH);
			return tag.StartsWith("va") ? "1" : "0";
		}

		public override string ToString()
		{
			return "ExtractorSpanishAuxiliaryTag";
		}
	}

	/// <summary>
	/// Extracts a boolean indicating whether the given word is preceded by
	/// a semi-auxiliary verb.
	/// </summary>
	[System.Serializable]
	internal class ExtractorSpanishSemiauxiliaryTag : Extractor
	{
		private const long serialVersionUID = -164942945521643734L;

		public ExtractorSpanishSemiauxiliaryTag()
			: base(-1, true)
		{
		}

		internal override string Extract(History h, PairsHolder pH)
		{
			string tag = base.Extract(h, pH);
			return tag.StartsWith("vs") ? "1" : "0";
		}

		public override string ToString()
		{
			return "ExtractorSpanishSemiauxiliaryTag";
		}
	}
}

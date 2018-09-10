using System;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// This class contains options to the parser which MUST be the SAME at
	/// both training and testing (parsing) time in order for the parser to
	/// work properly.
	/// </summary>
	/// <remarks>
	/// This class contains options to the parser which MUST be the SAME at
	/// both training and testing (parsing) time in order for the parser to
	/// work properly.  It also contains an object which stores the options
	/// used by the parser at training time and an object which contains
	/// default options for test use.
	/// </remarks>
	/// <author>Dan Klein</author>
	/// <author>Christopher Manning</author>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class Options
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.Options));

		public Options()
			: this(new EnglishTreebankParserParams())
		{
			trainOptions = NewTrainOptions();
			testOptions = NewTestOptions();
		}

		public Options(ITreebankLangParserParams tlpParams)
		{
			trainOptions = NewTrainOptions();
			testOptions = NewTestOptions();
			this.tlpParams = tlpParams;
		}

		/// <summary>
		/// Set options based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set options based on a String array in the style of
		/// commandline flags. This method goes through the array until it ends,
		/// processing options, as for
		/// <see cref="SetOption(string[], int)"/>
		/// .
		/// </remarks>
		/// <param name="flags">
		/// Array of options (or as a varargs list of arguments).
		/// The options passed in should
		/// be specified like command-line arguments, including with an initial
		/// minus sign  for example,
		/// {"-outputFormat", "typedDependencies", "-maxLength", "70"}
		/// </param>
		/// <exception cref="System.ArgumentException">If an unknown flag is passed in</exception>
		public virtual void SetOptions(params string[] flags)
		{
			SetOptions(flags, 0, flags.Length);
		}

		/// <summary>
		/// Set options based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set options based on a String array in the style of
		/// commandline flags. This method goes through the array until it ends,
		/// processing options, as for
		/// <see cref="SetOption(string[], int)"/>
		/// .
		/// </remarks>
		/// <param name="flags">
		/// Array of options.  The options passed in should
		/// be specified like command-line arguments, including with an initial
		/// minus sign  for example,
		/// {"-outputFormat", "typedDependencies", "-maxLength", "70"}
		/// </param>
		/// <param name="startIndex">The index in the array to begin processing options at</param>
		/// <param name="endIndexPlusOne">
		/// A number one greater than the last array index at
		/// which options should be processed
		/// </param>
		/// <exception cref="System.ArgumentException">If an unknown flag is passed in</exception>
		public virtual void SetOptions(string[] flags, int startIndex, int endIndexPlusOne)
		{
			for (int i = startIndex; i < endIndexPlusOne; )
			{
				i = SetOption(flags, i);
			}
		}

		/// <summary>
		/// Set options based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set options based on a String array in the style of
		/// commandline flags. This method goes through the array until it ends,
		/// processing options, as for
		/// <see cref="SetOption(string[], int)"/>
		/// .
		/// </remarks>
		/// <param name="flags">
		/// Array of options (or as a varargs list of arguments).
		/// The options passed in should
		/// be specified like command-line arguments, including with an initial
		/// minus sign  for example,
		/// {"-outputFormat", "typedDependencies", "-maxLength", "70"}
		/// </param>
		/// <exception cref="System.ArgumentException">If an unknown flag is passed in</exception>
		public virtual void SetOptionsOrWarn(params string[] flags)
		{
			SetOptionsOrWarn(flags, 0, flags.Length);
		}

		/// <summary>
		/// Set options based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set options based on a String array in the style of
		/// commandline flags. This method goes through the array until it ends,
		/// processing options, as for
		/// <see cref="SetOption(string[], int)"/>
		/// .
		/// </remarks>
		/// <param name="flags">
		/// Array of options.  The options passed in should
		/// be specified like command-line arguments, including with an initial
		/// minus sign  for example,
		/// {"-outputFormat", "typedDependencies", "-maxLength", "70"}
		/// </param>
		/// <param name="startIndex">The index in the array to begin processing options at</param>
		/// <param name="endIndexPlusOne">
		/// A number one greater than the last array index at
		/// which options should be processed
		/// </param>
		/// <exception cref="System.ArgumentException">If an unknown flag is passed in</exception>
		public virtual void SetOptionsOrWarn(string[] flags, int startIndex, int endIndexPlusOne)
		{
			for (int i = startIndex; i < endIndexPlusOne; )
			{
				i = SetOptionOrWarn(flags, i);
			}
		}

		/// <summary>
		/// Set an option based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set an option based on a String array in the style of
		/// commandline flags. The option may
		/// be either one known by the Options object, or one recognized by the
		/// TreebankLangParserParams which has already been set up inside the Options
		/// object, and then the option is set in the language-particular
		/// TreebankLangParserParams.
		/// Note that despite this method being an instance method, many flags
		/// are actually set as static class variables in the Train and Test
		/// classes (this should be fixed some day).
		/// Some options (there are many others; see the source code):
		/// <ul>
		/// <li> <code>-maxLength n</code> set the maximum length sentence to parse (inclusively)
		/// <li> <code>-printTT</code> print the training trees in raw, annotated, and annotated+binarized form.  Useful for debugging and other miscellany.
		/// <li> <code>-printAnnotated filename</code> use only in conjunction with -printTT.  Redirects printing of annotated training trees to <code>filename</code>.
		/// <li> <code>-forceTags</code> when the parser is tested against a set of gold standard trees, use the tagged yield, instead of just the yield, as input.
		/// </ul>
		/// </remarks>
		/// <param name="flags">An array of options arguments, command-line style.  E.g. {"-maxLength", "50"}.</param>
		/// <param name="i">The index in flags to start at when processing an option</param>
		/// <returns>
		/// The index in flags of the position after the last element used in
		/// processing this option. If the current array position cannot be processed as a valid
		/// option, then a warning message is printed to stderr and the return value is <code>i+1</code>
		/// </returns>
		public virtual int SetOptionOrWarn(string[] flags, int i)
		{
			int j = SetOptionFlag(flags, i);
			if (j == i)
			{
				j = tlpParams.SetOptionFlag(flags, i);
			}
			if (j == i)
			{
				log.Info("WARNING! lexparser.Options: Unknown option ignored: " + flags[i]);
				j++;
			}
			return j;
		}

		/// <summary>
		/// Set an option based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set an option based on a String array in the style of
		/// commandline flags. The option may
		/// be either one known by the Options object, or one recognized by the
		/// TreebankLangParserParams which has already been set up inside the Options
		/// object, and then the option is set in the language-particular
		/// TreebankLangParserParams.
		/// Note that despite this method being an instance method, many flags
		/// are actually set as static class variables in the Train and Test
		/// classes (this should be fixed some day).
		/// Some options (there are many others; see the source code):
		/// <ul>
		/// <li> <code>-maxLength n</code> set the maximum length sentence to parse (inclusively)
		/// <li> <code>-printTT</code> print the training trees in raw, annotated, and annotated+binarized form.  Useful for debugging and other miscellany.
		/// <li> <code>-printAnnotated filename</code> use only in conjunction with -printTT.  Redirects printing of annotated training trees to <code>filename</code>.
		/// <li> <code>-forceTags</code> when the parser is tested against a set of gold standard trees, use the tagged yield, instead of just the yield, as input.
		/// </ul>
		/// </remarks>
		/// <param name="flags">An array of options arguments, command-line style.  E.g. {"-maxLength", "50"}.</param>
		/// <param name="i">The index in flags to start at when processing an option</param>
		/// <returns>
		/// The index in flags of the position after the last element used in
		/// processing this option.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// If the current array position cannot be
		/// processed as a valid option
		/// </exception>
		public virtual int SetOption(string[] flags, int i)
		{
			int j = SetOptionFlag(flags, i);
			if (j == i)
			{
				j = tlpParams.SetOptionFlag(flags, i);
			}
			if (j == i)
			{
				throw new ArgumentException("Unknown option: " + flags[i]);
			}
			return j;
		}

		/// <summary>
		/// Set an option in this object, based on a String array in the style of
		/// commandline flags.
		/// </summary>
		/// <remarks>
		/// Set an option in this object, based on a String array in the style of
		/// commandline flags.  The option is only processed with respect to
		/// options directly known by the Options object.
		/// Some options (there are many others; see the source code):
		/// <ul>
		/// <li> <code>-maxLength n</code> set the maximum length sentence to parse (inclusively)
		/// <li> <code>-printTT</code> print the training trees in raw, annotated, and annotated+binarized form.  Useful for debugging and other miscellany.
		/// <li> <code>-printAnnotated filename</code> use only in conjunction with -printTT.  Redirects printing of annotated training trees to <code>filename</code>.
		/// <li> <code>-forceTags</code> when the parser is tested against a set of gold standard trees, use the tagged yield, instead of just the yield, as input.
		/// </ul>
		/// </remarks>
		/// <param name="args">An array of options arguments, command-line style.  E.g. {"-maxLength", "50"}.</param>
		/// <param name="i">The index in args to start at when processing an option</param>
		/// <returns>
		/// The index in args of the position after the last element used in
		/// processing this option, or the value i unchanged if a valid option couldn't
		/// be processed starting at position i.
		/// </returns>
		protected internal virtual int SetOptionFlag(string[] args, int i)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-PCFG"))
			{
				doDep = false;
				doPCFG = true;
				i++;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dep"))
				{
					doDep = true;
					doPCFG = false;
					i++;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-factored"))
					{
						doDep = true;
						doPCFG = true;
						testOptions.useFastFactored = false;
						i++;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-fastFactored"))
						{
							doDep = true;
							doPCFG = true;
							testOptions.useFastFactored = true;
							i++;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noRecoveryTagging"))
							{
								testOptions.noRecoveryTagging = true;
								i++;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useLexiconToScoreDependencyPwGt"))
								{
									testOptions.useLexiconToScoreDependencyPwGt = true;
									i++;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useSmoothTagProjection"))
									{
										useSmoothTagProjection = true;
										i++;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useUnigramWordSmoothing"))
										{
											useUnigramWordSmoothing = true;
											i++;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useNonProjectiveDependencyParser"))
											{
												testOptions.useNonProjectiveDependencyParser = true;
												i++;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxLength") && (i + 1 < args.Length))
												{
													testOptions.maxLength = System.Convert.ToInt32(args[i + 1]);
													i += 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-MAX_ITEMS") && (i + 1 < args.Length))
													{
														testOptions.MaxItems = System.Convert.ToInt32(args[i + 1]);
														i += 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainLength") && (i + 1 < args.Length))
														{
															// train on only short sentences
															trainOptions.trainLengthLimit = System.Convert.ToInt32(args[i + 1]);
															i += 2;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-lengthNormalization"))
															{
																testOptions.lengthNormalization = true;
																i++;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-iterativeCKY"))
																{
																	testOptions.iterativeCKY = true;
																	i++;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-vMarkov") && (i + 1 < args.Length))
																	{
																		int order = System.Convert.ToInt32(args[i + 1]);
																		if (order <= 1)
																		{
																			trainOptions.Pa = false;
																			trainOptions.gPA = false;
																		}
																		else
																		{
																			if (order == 2)
																			{
																				trainOptions.Pa = true;
																				trainOptions.gPA = false;
																			}
																			else
																			{
																				if (order >= 3)
																				{
																					trainOptions.Pa = true;
																					trainOptions.gPA = true;
																				}
																			}
																		}
																		i += 2;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-vSelSplitCutOff") && (i + 1 < args.Length))
																		{
																			trainOptions.selectiveSplitCutOff = double.Parse(args[i + 1]);
																			trainOptions.selectiveSplit = trainOptions.selectiveSplitCutOff > 0.0;
																			i += 2;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-vSelPostSplitCutOff") && (i + 1 < args.Length))
																			{
																				trainOptions.selectivePostSplitCutOff = double.Parse(args[i + 1]);
																				trainOptions.selectivePostSplit = trainOptions.selectivePostSplitCutOff > 0.0;
																				i += 2;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-deleteSplitters") && (i + 1 < args.Length))
																				{
																					string[] toDel = args[i + 1].Split(" *, *");
																					trainOptions.deleteSplitters = Generics.NewHashSet(Arrays.AsList(toDel));
																					i += 2;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-postSplitWithBaseCategory"))
																					{
																						trainOptions.postSplitWithBaseCategory = true;
																						i += 1;
																					}
																					else
																					{
																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-vPostMarkov") && (i + 1 < args.Length))
																						{
																							int order = System.Convert.ToInt32(args[i + 1]);
																							if (order <= 1)
																							{
																								trainOptions.postPA = false;
																								trainOptions.postGPA = false;
																							}
																							else
																							{
																								if (order == 2)
																								{
																									trainOptions.postPA = true;
																									trainOptions.postGPA = false;
																								}
																								else
																								{
																									if (order >= 3)
																									{
																										trainOptions.postPA = true;
																										trainOptions.postGPA = true;
																									}
																								}
																							}
																							i += 2;
																						}
																						else
																						{
																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-hMarkov") && (i + 1 < args.Length))
																							{
																								int order = System.Convert.ToInt32(args[i + 1]);
																								if (order >= 0)
																								{
																									trainOptions.markovOrder = order;
																									trainOptions.markovFactor = true;
																								}
																								else
																								{
																									trainOptions.markovFactor = false;
																								}
																								i += 2;
																							}
																							else
																							{
																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-distanceBins") && (i + 1 < args.Length))
																								{
																									int numBins = System.Convert.ToInt32(args[i + 1]);
																									if (numBins <= 1)
																									{
																										distance = false;
																									}
																									else
																									{
																										if (numBins == 4)
																										{
																											distance = true;
																											coarseDistance = true;
																										}
																										else
																										{
																											if (numBins == 5)
																											{
																												distance = true;
																												coarseDistance = false;
																											}
																											else
																											{
																												throw new ArgumentException("Invalid value for -distanceBin: " + args[i + 1]);
																											}
																										}
																									}
																									i += 2;
																								}
																								else
																								{
																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noStop"))
																									{
																										genStop = false;
																										i++;
																									}
																									else
																									{
																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nonDirectional"))
																										{
																											directional = false;
																											i++;
																										}
																										else
																										{
																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-depWeight") && (i + 1 < args.Length))
																											{
																												testOptions.depWeight = double.Parse(args[i + 1]);
																												i += 2;
																											}
																											else
																											{
																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printPCFGkBest") && (i + 1 < args.Length))
																												{
																													testOptions.printPCFGkBest = System.Convert.ToInt32(args[i + 1]);
																													i += 2;
																												}
																												else
																												{
																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-evalPCFGkBest") && (i + 1 < args.Length))
																													{
																														testOptions.evalPCFGkBest = System.Convert.ToInt32(args[i + 1]);
																														i += 2;
																													}
																													else
																													{
																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printFactoredKGood") && (i + 1 < args.Length))
																														{
																															testOptions.printFactoredKGood = System.Convert.ToInt32(args[i + 1]);
																															i += 2;
																														}
																														else
																														{
																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-smoothTagsThresh") && (i + 1 < args.Length))
																															{
																																lexOptions.smoothInUnknownsThreshold = System.Convert.ToInt32(args[i + 1]);
																																i += 2;
																															}
																															else
																															{
																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unseenSmooth") && (i + 1 < args.Length))
																																{
																																	testOptions.unseenSmooth = double.Parse(args[i + 1]);
																																	i += 2;
																																}
																																else
																																{
																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-fractionBeforeUnseenCounting") && (i + 1 < args.Length))
																																	{
																																		trainOptions.fractionBeforeUnseenCounting = double.Parse(args[i + 1]);
																																		i += 2;
																																	}
																																	else
																																	{
																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-hSelSplitThresh") && (i + 1 < args.Length))
																																		{
																																			trainOptions.HselCut = System.Convert.ToInt32(args[i + 1]);
																																			trainOptions.hSelSplit = trainOptions.HselCut > 0;
																																			i += 2;
																																		}
																																		else
																																		{
																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nohSelSplit"))
																																			{
																																				trainOptions.hSelSplit = false;
																																				i += 1;
																																			}
																																			else
																																			{
																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tagPA"))
																																				{
																																					trainOptions.tagPA = true;
																																					i += 1;
																																				}
																																				else
																																				{
																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noTagPA"))
																																					{
																																						trainOptions.tagPA = false;
																																						i += 1;
																																					}
																																					else
																																					{
																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tagSelSplitCutOff") && (i + 1 < args.Length))
																																						{
																																							trainOptions.tagSelectiveSplitCutOff = double.Parse(args[i + 1]);
																																							trainOptions.tagSelectiveSplit = trainOptions.tagSelectiveSplitCutOff > 0.0;
																																							i += 2;
																																						}
																																						else
																																						{
																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tagSelPostSplitCutOff") && (i + 1 < args.Length))
																																							{
																																								trainOptions.tagSelectivePostSplitCutOff = double.Parse(args[i + 1]);
																																								trainOptions.tagSelectivePostSplit = trainOptions.tagSelectivePostSplitCutOff > 0.0;
																																								i += 2;
																																							}
																																							else
																																							{
																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noTagSplit"))
																																								{
																																									trainOptions.noTagSplit = true;
																																									i += 1;
																																								}
																																								else
																																								{
																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-uwm") && (i + 1 < args.Length))
																																									{
																																										lexOptions.useUnknownWordSignatures = System.Convert.ToInt32(args[i + 1]);
																																										i += 2;
																																									}
																																									else
																																									{
																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownSuffixSize") && (i + 1 < args.Length))
																																										{
																																											lexOptions.unknownSuffixSize = System.Convert.ToInt32(args[i + 1]);
																																											i += 2;
																																										}
																																										else
																																										{
																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownPrefixSize") && (i + 1 < args.Length))
																																											{
																																												lexOptions.unknownPrefixSize = System.Convert.ToInt32(args[i + 1]);
																																												i += 2;
																																											}
																																											else
																																											{
																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-uwModelTrainer") && (i + 1 < args.Length))
																																												{
																																													lexOptions.uwModelTrainer = args[i + 1];
																																													i += 2;
																																												}
																																												else
																																												{
																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-openClassThreshold") && (i + 1 < args.Length))
																																													{
																																														trainOptions.openClassTypesThreshold = System.Convert.ToInt32(args[i + 1]);
																																														i += 2;
																																													}
																																													else
																																													{
																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unary") && i + 1 < args.Length)
																																														{
																																															trainOptions.markUnary = System.Convert.ToInt32(args[i + 1]);
																																															i += 2;
																																														}
																																														else
																																														{
																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unaryTags"))
																																															{
																																																trainOptions.markUnaryTags = true;
																																																i += 1;
																																															}
																																															else
																																															{
																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-mutate"))
																																																{
																																																	lexOptions.smartMutation = true;
																																																	i += 1;
																																																}
																																																else
																																																{
																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useUnicodeType"))
																																																	{
																																																		lexOptions.useUnicodeType = true;
																																																		i += 1;
																																																	}
																																																	else
																																																	{
																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-rightRec"))
																																																		{
																																																			trainOptions.rightRec = true;
																																																			i += 1;
																																																		}
																																																		else
																																																		{
																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noRightRec"))
																																																			{
																																																				trainOptions.rightRec = false;
																																																				i += 1;
																																																			}
																																																			else
																																																			{
																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-preTag"))
																																																				{
																																																					testOptions.preTag = true;
																																																					i += 1;
																																																				}
																																																				else
																																																				{
																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-forceTags"))
																																																					{
																																																						testOptions.forceTags = true;
																																																						i += 1;
																																																					}
																																																					else
																																																					{
																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-taggerSerializedFile"))
																																																						{
																																																							testOptions.taggerSerializedFile = args[i + 1];
																																																							i += 2;
																																																						}
																																																						else
																																																						{
																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-forceTagBeginnings"))
																																																							{
																																																								testOptions.forceTagBeginnings = true;
																																																								i += 1;
																																																							}
																																																							else
																																																							{
																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noFunctionalForcing"))
																																																								{
																																																									testOptions.noFunctionalForcing = true;
																																																									i += 1;
																																																								}
																																																								else
																																																								{
																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-scTags"))
																																																									{
																																																										dcTags = false;
																																																										i += 1;
																																																									}
																																																									else
																																																									{
																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dcTags"))
																																																										{
																																																											dcTags = true;
																																																											i += 1;
																																																										}
																																																										else
																																																										{
																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-basicCategoryTagsInDependencyGrammar"))
																																																											{
																																																												trainOptions.basicCategoryTagsInDependencyGrammar = true;
																																																												i += 1;
																																																											}
																																																											else
																																																											{
																																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-evalb"))
																																																												{
																																																													testOptions.evalb = true;
																																																													i += 1;
																																																												}
																																																												else
																																																												{
																																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-v") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-verbose"))
																																																													{
																																																														testOptions.verbose = true;
																																																														i += 1;
																																																													}
																																																													else
																																																													{
																																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputFilesDirectory") && i + 1 < args.Length)
																																																														{
																																																															testOptions.outputFilesDirectory = args[i + 1];
																																																															i += 2;
																																																														}
																																																														else
																																																														{
																																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputFilesExtension") && i + 1 < args.Length)
																																																															{
																																																																testOptions.outputFilesExtension = args[i + 1];
																																																																i += 2;
																																																															}
																																																															else
																																																															{
																																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputFilesPrefix") && i + 1 < args.Length)
																																																																{
																																																																	testOptions.outputFilesPrefix = args[i + 1];
																																																																	i += 2;
																																																																}
																																																																else
																																																																{
																																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputkBestEquivocation") && i + 1 < args.Length)
																																																																	{
																																																																		testOptions.outputkBestEquivocation = args[i + 1];
																																																																		i += 2;
																																																																	}
																																																																	else
																																																																	{
																																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-writeOutputFiles"))
																																																																		{
																																																																			testOptions.writeOutputFiles = true;
																																																																			i += 1;
																																																																		}
																																																																		else
																																																																		{
																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printAllBestParses"))
																																																																			{
																																																																				testOptions.printAllBestParses = true;
																																																																				i += 1;
																																																																			}
																																																																			else
																																																																			{
																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputTreeFormat") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputFormat"))
																																																																				{
																																																																					testOptions.outputFormat = args[i + 1];
																																																																					i += 2;
																																																																				}
																																																																				else
																																																																				{
																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputTreeFormatOptions") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputFormatOptions"))
																																																																					{
																																																																						testOptions.outputFormatOptions = args[i + 1];
																																																																						i += 2;
																																																																					}
																																																																					else
																																																																					{
																																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-addMissingFinalPunctuation"))
																																																																						{
																																																																							testOptions.addMissingFinalPunctuation = true;
																																																																							i += 1;
																																																																						}
																																																																						else
																																																																						{
																																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-flexiTag"))
																																																																							{
																																																																								lexOptions.flexiTag = true;
																																																																								i += 1;
																																																																							}
																																																																							else
																																																																							{
																																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-lexiTag"))
																																																																								{
																																																																									lexOptions.flexiTag = false;
																																																																									i += 1;
																																																																								}
																																																																								else
																																																																								{
																																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useSignatureForKnownSmoothing"))
																																																																									{
																																																																										lexOptions.useSignatureForKnownSmoothing = true;
																																																																										i += 1;
																																																																									}
																																																																									else
																																																																									{
																																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-wordClassesFile"))
																																																																										{
																																																																											lexOptions.wordClassesFile = args[i + 1];
																																																																											i += 2;
																																																																										}
																																																																										else
																																																																										{
																																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-compactGrammar"))
																																																																											{
																																																																												trainOptions.compactGrammar = System.Convert.ToInt32(args[i + 1]);
																																																																												i += 2;
																																																																											}
																																																																											else
																																																																											{
																																																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markFinalStates"))
																																																																												{
																																																																													trainOptions.markFinalStates = Sharpen.Runtime.EqualsIgnoreCase(args[i + 1], "true");
																																																																													i += 2;
																																																																												}
																																																																												else
																																																																												{
																																																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-leftToRight"))
																																																																													{
																																																																														trainOptions.leftToRight = args[i + 1].Equals("true");
																																																																														i += 2;
																																																																													}
																																																																													else
																																																																													{
																																																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-cnf"))
																																																																														{
																																																																															forceCNF = true;
																																																																															i += 1;
																																																																														}
																																																																														else
																																																																														{
																																																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-smoothRules"))
																																																																															{
																																																																																trainOptions.ruleSmoothing = true;
																																																																																trainOptions.ruleSmoothingAlpha = double.ValueOf(args[i + 1]);
																																																																																i += 2;
																																																																															}
																																																																															else
																																																																															{
																																																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nodePrune") && i + 1 < args.Length)
																																																																																{
																																																																																	nodePrune = Sharpen.Runtime.EqualsIgnoreCase(args[i + 1], "true");
																																																																																	i += 2;
																																																																																}
																																																																																else
																																																																																{
																																																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noDoRecovery"))
																																																																																	{
																																																																																		testOptions.doRecovery = false;
																																																																																		i += 1;
																																																																																	}
																																																																																	else
																																																																																	{
																																																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-acl03chinese"))
																																																																																		{
																																																																																			trainOptions.markovOrder = 1;
																																																																																			trainOptions.markovFactor = true;
																																																																																		}
																																																																																		else
																																																																																		{
																																																																																			// no increment
																																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-wordFunction"))
																																																																																			{
																																																																																				wordFunction = ReflectionLoading.LoadByReflection(args[i + 1]);
																																																																																				i += 2;
																																																																																			}
																																																																																			else
																																																																																			{
																																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-acl03pcfg"))
																																																																																				{
																																																																																					doDep = false;
																																																																																					doPCFG = true;
																																																																																					// lexOptions.smoothInUnknownsThreshold = 30;
																																																																																					trainOptions.markUnary = 1;
																																																																																					trainOptions.Pa = true;
																																																																																					trainOptions.gPA = false;
																																																																																					trainOptions.tagPA = true;
																																																																																					trainOptions.tagSelectiveSplit = false;
																																																																																					trainOptions.rightRec = true;
																																																																																					trainOptions.selectiveSplit = true;
																																																																																					trainOptions.selectiveSplitCutOff = 400.0;
																																																																																					trainOptions.markovFactor = true;
																																																																																					trainOptions.markovOrder = 2;
																																																																																					trainOptions.hSelSplit = true;
																																																																																					lexOptions.useUnknownWordSignatures = 2;
																																																																																					lexOptions.flexiTag = true;
																																																																																					// DAN: Tag double-counting is BAD for PCFG-only parsing
																																																																																					dcTags = false;
																																																																																				}
																																																																																				else
																																																																																				{
																																																																																					// don't increment i so it gets language specific stuff as well
																																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-jenny"))
																																																																																					{
																																																																																						doDep = false;
																																																																																						doPCFG = true;
																																																																																						// lexOptions.smoothInUnknownsThreshold = 30;
																																																																																						trainOptions.markUnary = 1;
																																																																																						trainOptions.Pa = false;
																																																																																						trainOptions.gPA = false;
																																																																																						trainOptions.tagPA = false;
																																																																																						trainOptions.tagSelectiveSplit = false;
																																																																																						trainOptions.rightRec = true;
																																																																																						trainOptions.selectiveSplit = false;
																																																																																						//      trainOptions.selectiveSplitCutOff = 400.0;
																																																																																						trainOptions.markovFactor = false;
																																																																																						//      trainOptions.markovOrder = 2;
																																																																																						trainOptions.hSelSplit = false;
																																																																																						lexOptions.useUnknownWordSignatures = 2;
																																																																																						lexOptions.flexiTag = true;
																																																																																						// DAN: Tag double-counting is BAD for PCFG-only parsing
																																																																																						dcTags = false;
																																																																																					}
																																																																																					else
																																																																																					{
																																																																																						// don't increment i so it gets language specific stuff as well
																																																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-goodPCFG"))
																																																																																						{
																																																																																							doDep = false;
																																																																																							doPCFG = true;
																																																																																							// op.lexOptions.smoothInUnknownsThreshold = 30;
																																																																																							trainOptions.markUnary = 1;
																																																																																							trainOptions.Pa = true;
																																																																																							trainOptions.gPA = false;
																																																																																							trainOptions.tagPA = true;
																																																																																							trainOptions.tagSelectiveSplit = false;
																																																																																							trainOptions.rightRec = true;
																																																																																							trainOptions.selectiveSplit = true;
																																																																																							trainOptions.selectiveSplitCutOff = 400.0;
																																																																																							trainOptions.markovFactor = true;
																																																																																							trainOptions.markovOrder = 2;
																																																																																							trainOptions.hSelSplit = true;
																																																																																							lexOptions.useUnknownWordSignatures = 2;
																																																																																							lexOptions.flexiTag = true;
																																																																																							// DAN: Tag double-counting is BAD for PCFG-only parsing
																																																																																							dcTags = false;
																																																																																							string[] delSplit = new string[] { "-deleteSplitters", "VP^NP,VP^VP,VP^SINV,VP^SQ" };
																																																																																							if (this.SetOptionFlag(delSplit, 0) != 2)
																																																																																							{
																																																																																								log.Info("Error processing deleteSplitters");
																																																																																							}
																																																																																						}
																																																																																						else
																																																																																						{
																																																																																							// don't increment i so it gets language specific stuff as well
																																																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-linguisticPCFG"))
																																																																																							{
																																																																																								doDep = false;
																																																																																								doPCFG = true;
																																																																																								// op.lexOptions.smoothInUnknownsThreshold = 30;
																																																																																								trainOptions.markUnary = 1;
																																																																																								trainOptions.Pa = true;
																																																																																								trainOptions.gPA = false;
																																																																																								trainOptions.tagPA = true;
																																																																																								// on at the moment, but iffy
																																																																																								trainOptions.tagSelectiveSplit = false;
																																																																																								trainOptions.rightRec = false;
																																																																																								// not for linguistic
																																																																																								trainOptions.selectiveSplit = true;
																																																																																								trainOptions.selectiveSplitCutOff = 400.0;
																																																																																								trainOptions.markovFactor = true;
																																																																																								trainOptions.markovOrder = 2;
																																																																																								trainOptions.hSelSplit = true;
																																																																																								lexOptions.useUnknownWordSignatures = 5;
																																																																																								// different from acl03pcfg
																																																																																								lexOptions.flexiTag = false;
																																																																																								// different from acl03pcfg
																																																																																								// DAN: Tag double-counting is BAD for PCFG-only parsing
																																																																																								dcTags = false;
																																																																																							}
																																																																																							else
																																																																																							{
																																																																																								// don't increment i so it gets language specific stuff as well
																																																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-ijcai03"))
																																																																																								{
																																																																																									doDep = true;
																																																																																									doPCFG = true;
																																																																																									trainOptions.markUnary = 0;
																																																																																									trainOptions.Pa = true;
																																																																																									trainOptions.gPA = false;
																																																																																									trainOptions.tagPA = false;
																																																																																									trainOptions.tagSelectiveSplit = false;
																																																																																									trainOptions.rightRec = false;
																																																																																									trainOptions.selectiveSplit = true;
																																																																																									trainOptions.selectiveSplitCutOff = 300.0;
																																																																																									trainOptions.markovFactor = true;
																																																																																									trainOptions.markovOrder = 2;
																																																																																									trainOptions.hSelSplit = true;
																																																																																									trainOptions.compactGrammar = 0;
																																																																																									/// cdm: May 2005 compacting bad for factored?
																																																																																									lexOptions.useUnknownWordSignatures = 2;
																																																																																									lexOptions.flexiTag = false;
																																																																																									dcTags = true;
																																																																																								}
																																																																																								else
																																																																																								{
																																																																																									// op.nodePrune = true;  // cdm: May 2005: this doesn't help
																																																																																									// don't increment i so it gets language specific stuff as well
																																																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-goodFactored"))
																																																																																									{
																																																																																										doDep = true;
																																																																																										doPCFG = true;
																																																																																										trainOptions.markUnary = 0;
																																																																																										trainOptions.Pa = true;
																																																																																										trainOptions.gPA = false;
																																																																																										trainOptions.tagPA = false;
																																																																																										trainOptions.tagSelectiveSplit = false;
																																																																																										trainOptions.rightRec = false;
																																																																																										trainOptions.selectiveSplit = true;
																																																																																										trainOptions.selectiveSplitCutOff = 300.0;
																																																																																										trainOptions.markovFactor = true;
																																																																																										trainOptions.markovOrder = 2;
																																																																																										trainOptions.hSelSplit = true;
																																																																																										trainOptions.compactGrammar = 0;
																																																																																										/// cdm: May 2005 compacting bad for factored?
																																																																																										lexOptions.useUnknownWordSignatures = 5;
																																																																																										// different from ijcai03
																																																																																										lexOptions.flexiTag = false;
																																																																																										dcTags = true;
																																																																																									}
																																																																																									else
																																																																																									{
																																																																																										// op.nodePrune = true;  // cdm: May 2005: this doesn't help
																																																																																										// don't increment i so it gets language specific stuff as well
																																																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseFactored"))
																																																																																										{
																																																																																											// Single counting tag->word rewrite is also much better for Chinese
																																																																																											// Factored.  Bracketing F1 goes up about 0.7%.
																																																																																											dcTags = false;
																																																																																											lexOptions.useUnicodeType = true;
																																																																																											trainOptions.markovOrder = 2;
																																																																																											trainOptions.hSelSplit = true;
																																																																																											trainOptions.markovFactor = true;
																																																																																											trainOptions.HselCut = 50;
																																																																																										}
																																																																																										else
																																																																																										{
																																																																																											// trainOptions.openClassTypesThreshold=1;  // so can get unseen punctuation
																																																																																											// trainOptions.fractionBeforeUnseenCounting=0.0;  // so can get unseen punctuation
																																																																																											// don't increment i so it gets language specific stuff as well
																																																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-arabicFactored"))
																																																																																											{
																																																																																												doDep = true;
																																																																																												doPCFG = true;
																																																																																												dcTags = false;
																																																																																												// "false" seems to help Arabic about 0.1% F1
																																																																																												trainOptions.markovFactor = true;
																																																																																												trainOptions.markovOrder = 2;
																																																																																												trainOptions.hSelSplit = true;
																																																																																												trainOptions.HselCut = 75;
																																																																																												// 75 bit better than 50, 100 a bit worse
																																																																																												trainOptions.Pa = true;
																																																																																												trainOptions.gPA = false;
																																																																																												trainOptions.selectiveSplit = true;
																																																																																												trainOptions.selectiveSplitCutOff = 300.0;
																																																																																												trainOptions.markUnary = 1;
																																																																																												// Helps PCFG and marginally factLB
																																																																																												// trainOptions.compactGrammar = 0;  // Doesn't seem to help or only 0.05% F1
																																																																																												lexOptions.useUnknownWordSignatures = 9;
																																																																																												lexOptions.unknownPrefixSize = 1;
																																																																																												lexOptions.unknownSuffixSize = 1;
																																																																																												testOptions.MaxItems = 500000;
																																																																																											}
																																																																																											else
																																																																																											{
																																																																																												// Arabic sentences are long enough that this helps a fraction
																																																																																												// don't increment i so it gets language specific stuff as well
																																																																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-frenchFactored"))
																																																																																												{
																																																																																													doDep = true;
																																																																																													doPCFG = true;
																																																																																													dcTags = false;
																																																																																													//wsg2011: Setting to false improves F1 by 0.5%
																																																																																													trainOptions.markovFactor = true;
																																																																																													trainOptions.markovOrder = 2;
																																																																																													trainOptions.hSelSplit = true;
																																																																																													trainOptions.HselCut = 75;
																																																																																													trainOptions.Pa = true;
																																																																																													trainOptions.gPA = false;
																																																																																													trainOptions.selectiveSplit = true;
																																																																																													trainOptions.selectiveSplitCutOff = 300.0;
																																																																																													trainOptions.markUnary = 0;
																																																																																													//Unary rule marking bad for french..setting to 0 gives +0.3 F1
																																																																																													lexOptions.useUnknownWordSignatures = 1;
																																																																																													lexOptions.unknownPrefixSize = 1;
																																																																																													lexOptions.unknownSuffixSize = 2;
																																																																																												}
																																																																																												else
																																																																																												{
																																																																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chinesePCFG"))
																																																																																													{
																																																																																														trainOptions.markovOrder = 2;
																																																																																														trainOptions.markovFactor = true;
																																																																																														trainOptions.HselCut = 5;
																																																																																														trainOptions.Pa = true;
																																																																																														trainOptions.gPA = true;
																																																																																														trainOptions.selectiveSplit = false;
																																																																																														doDep = false;
																																																																																														doPCFG = true;
																																																																																														// Single counting tag->word rewrite is also much better for Chinese PCFG
																																																																																														// Bracketing F1 is up about 2% and tag accuracy about 1% (exact by 6%)
																																																																																														dcTags = false;
																																																																																													}
																																																																																													else
																																																																																													{
																																																																																														// no increment
																																																																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printTT") && (i + 1 < args.Length))
																																																																																														{
																																																																																															trainOptions.printTreeTransformations = System.Convert.ToInt32(args[i + 1]);
																																																																																															i += 2;
																																																																																														}
																																																																																														else
																																																																																														{
																																																																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printAnnotatedRuleCounts"))
																																																																																															{
																																																																																																trainOptions.printAnnotatedRuleCounts = true;
																																																																																																i++;
																																																																																															}
																																																																																															else
																																																																																															{
																																																																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printAnnotatedStateCounts"))
																																																																																																{
																																																																																																	trainOptions.printAnnotatedStateCounts = true;
																																																																																																	i++;
																																																																																																}
																																																																																																else
																																																																																																{
																																																																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printAnnotated") && (i + 1 < args.Length))
																																																																																																	{
																																																																																																		try
																																																																																																		{
																																																																																																			trainOptions.printAnnotatedPW = tlpParams.Pw(new FileOutputStream(args[i + 1]));
																																																																																																		}
																																																																																																		catch (IOException)
																																																																																																		{
																																																																																																			trainOptions.printAnnotatedPW = null;
																																																																																																		}
																																																																																																		i += 2;
																																																																																																	}
																																																																																																	else
																																																																																																	{
																																																																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printBinarized") && (i + 1 < args.Length))
																																																																																																		{
																																																																																																			try
																																																																																																			{
																																																																																																				trainOptions.printBinarizedPW = tlpParams.Pw(new FileOutputStream(args[i + 1]));
																																																																																																			}
																																																																																																			catch (IOException)
																																																																																																			{
																																																																																																				trainOptions.printBinarizedPW = null;
																																																																																																			}
																																																																																																			i += 2;
																																																																																																		}
																																																																																																		else
																																																																																																		{
																																																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-printStates"))
																																																																																																			{
																																																																																																				trainOptions.printStates = true;
																																																																																																				i++;
																																																																																																			}
																																																																																																			else
																																																																																																			{
																																																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-preTransformer") && (i + 1 < args.Length))
																																																																																																				{
																																																																																																					string[] classes = args[i + 1].Split(",");
																																																																																																					i += 2;
																																																																																																					if (classes.Length == 1)
																																																																																																					{
																																																																																																						trainOptions.preTransformer = ReflectionLoading.LoadByReflection(classes[0], this);
																																																																																																					}
																																																																																																					else
																																																																																																					{
																																																																																																						if (classes.Length > 1)
																																																																																																						{
																																																																																																							CompositeTreeTransformer composite = new CompositeTreeTransformer();
																																																																																																							trainOptions.preTransformer = composite;
																																																																																																							foreach (string clazz in classes)
																																																																																																							{
																																																																																																								ITreeTransformer transformer = ReflectionLoading.LoadByReflection(clazz, this);
																																																																																																								composite.AddTransformer(transformer);
																																																																																																							}
																																																																																																						}
																																																																																																					}
																																																																																																				}
																																																																																																				else
																																																																																																				{
																																																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-taggedFiles") && (i + 1 < args.Length))
																																																																																																					{
																																																																																																						trainOptions.taggedFiles = args[i + 1];
																																																																																																						i += 2;
																																																																																																					}
																																																																																																					else
																																																																																																					{
																																																																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-predictSplits"))
																																																																																																						{
																																																																																																							// This is an experimental (and still in development)
																																																																																																							// reimplementation of Berkeley's state splitting grammar.
																																																																																																							trainOptions.predictSplits = true;
																																																																																																							trainOptions.compactGrammar = 0;
																																																																																																							i++;
																																																																																																						}
																																																																																																						else
																																																																																																						{
																																																																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitCount"))
																																																																																																							{
																																																																																																								trainOptions.splitCount = System.Convert.ToInt32(args[i + 1]);
																																																																																																								i += 2;
																																																																																																							}
																																																																																																							else
																																																																																																							{
																																																																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitRecombineRate"))
																																																																																																								{
																																																																																																									trainOptions.splitRecombineRate = double.Parse(args[i + 1]);
																																																																																																									i += 2;
																																																																																																								}
																																																																																																								else
																																																																																																								{
																																																																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainingThreads") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nThreads"))
																																																																																																									{
																																																																																																										trainOptions.trainingThreads = System.Convert.ToInt32(args[i + 1]);
																																																																																																										testOptions.testingThreads = System.Convert.ToInt32(args[i + 1]);
																																																																																																										i += 2;
																																																																																																									}
																																																																																																									else
																																																																																																									{
																																																																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-testingThreads"))
																																																																																																										{
																																																																																																											testOptions.testingThreads = System.Convert.ToInt32(args[i + 1]);
																																																																																																											i += 2;
																																																																																																										}
																																																																																																										else
																																																																																																										{
																																																																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-evals"))
																																																																																																											{
																																																																																																												testOptions.evals = StringUtils.StringToProperties(args[i + 1], testOptions.evals);
																																																																																																												i += 2;
																																																																																																											}
																																																																																																											else
																																																																																																											{
																																																																																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-fastFactoredCandidateMultiplier"))
																																																																																																												{
																																																																																																													testOptions.fastFactoredCandidateMultiplier = System.Convert.ToInt32(args[i + 1]);
																																																																																																													i += 2;
																																																																																																												}
																																																																																																												else
																																																																																																												{
																																																																																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-fastFactoredCandidateAddend"))
																																																																																																													{
																																																																																																														testOptions.fastFactoredCandidateAddend = System.Convert.ToInt32(args[i + 1]);
																																																																																																														i += 2;
																																																																																																													}
																																																																																																													else
																																																																																																													{
																																																																																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-quietEvaluation"))
																																																																																																														{
																																																																																																															testOptions.quietEvaluation = true;
																																																																																																															i += 1;
																																																																																																														}
																																																																																																														else
																																																																																																														{
																																																																																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noquietEvaluation"))
																																																																																																															{
																																																																																																																testOptions.quietEvaluation = false;
																																																																																																																i += 1;
																																																																																																															}
																																																																																																															else
																																																																																																															{
																																																																																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-simpleBinarizedLabels"))
																																																																																																																{
																																																																																																																	trainOptions.simpleBinarizedLabels = true;
																																																																																																																	i += 1;
																																																																																																																}
																																																																																																																else
																																																																																																																{
																																																																																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noRebinarization"))
																																																																																																																	{
																																																																																																																		trainOptions.noRebinarization = true;
																																																																																																																		i += 1;
																																																																																																																	}
																																																																																																																	else
																																																																																																																	{
																																																																																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dvKBest"))
																																																																																																																		{
																																																																																																																			trainOptions.dvKBest = System.Convert.ToInt32(args[i + 1]);
																																																																																																																			rerankerKBest = trainOptions.dvKBest;
																																																																																																																			i += 2;
																																																																																																																		}
																																																																																																																		else
																																																																																																																		{
																																																																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-regCost"))
																																																																																																																			{
																																																																																																																				trainOptions.regCost = double.Parse(args[i + 1]);
																																																																																																																				i += 2;
																																																																																																																			}
																																																																																																																			else
																																																																																																																			{
																																																																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dvIterations") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainingIterations"))
																																																																																																																				{
																																																																																																																					trainOptions.trainingIterations = System.Convert.ToInt32(args[i + 1]);
																																																																																																																					i += 2;
																																																																																																																				}
																																																																																																																				else
																																																																																																																				{
																																																																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-stalledIterationLimit"))
																																																																																																																					{
																																																																																																																						trainOptions.stalledIterationLimit = System.Convert.ToInt32(args[i + 1]);
																																																																																																																						i += 2;
																																																																																																																					}
																																																																																																																					else
																																																																																																																					{
																																																																																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dvBatchSize") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-batchSize"))
																																																																																																																						{
																																																																																																																							trainOptions.batchSize = System.Convert.ToInt32(args[i + 1]);
																																																																																																																							i += 2;
																																																																																																																						}
																																																																																																																						else
																																																																																																																						{
																																																																																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-qnIterationsPerBatch"))
																																																																																																																							{
																																																																																																																								trainOptions.qnIterationsPerBatch = System.Convert.ToInt32(args[i + 1]);
																																																																																																																								i += 2;
																																																																																																																							}
																																																																																																																							else
																																																																																																																							{
																																																																																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-qnEstimates"))
																																																																																																																								{
																																																																																																																									trainOptions.qnEstimates = System.Convert.ToInt32(args[i + 1]);
																																																																																																																									i += 2;
																																																																																																																								}
																																																																																																																								else
																																																																																																																								{
																																																																																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-qnTolerance"))
																																																																																																																									{
																																																																																																																										trainOptions.qnTolerance = double.Parse(args[i + 1]);
																																																																																																																										i += 2;
																																																																																																																									}
																																																																																																																									else
																																																																																																																									{
																																																																																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-debugOutputFrequency"))
																																																																																																																										{
																																																																																																																											trainOptions.debugOutputFrequency = System.Convert.ToInt32(args[i + 1]);
																																																																																																																											i += 2;
																																																																																																																										}
																																																																																																																										else
																																																																																																																										{
																																																																																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxTrainTimeSeconds"))
																																																																																																																											{
																																																																																																																												trainOptions.maxTrainTimeSeconds = System.Convert.ToInt32(args[i + 1]);
																																																																																																																												i += 2;
																																																																																																																											}
																																																																																																																											else
																																																																																																																											{
																																																																																																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dvSeed") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-randomSeed"))
																																																																																																																												{
																																																																																																																													trainOptions.randomSeed = long.Parse(args[i + 1]);
																																																																																																																													i += 2;
																																																																																																																												}
																																																																																																																												else
																																																																																																																												{
																																																																																																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-wordVectorFile"))
																																																																																																																													{
																																																																																																																														lexOptions.wordVectorFile = args[i + 1];
																																																																																																																														i += 2;
																																																																																																																													}
																																																																																																																													else
																																																																																																																													{
																																																																																																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-numHid"))
																																																																																																																														{
																																																																																																																															lexOptions.numHid = System.Convert.ToInt32(args[i + 1]);
																																																																																																																															i += 2;
																																																																																																																														}
																																																																																																																														else
																																																																																																																														{
																																																																																																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-learningRate"))
																																																																																																																															{
																																																																																																																																trainOptions.learningRate = double.Parse(args[i + 1]);
																																																																																																																																i += 2;
																																																																																																																															}
																																																																																																																															else
																																																																																																																															{
																																																																																																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-deltaMargin"))
																																																																																																																																{
																																																																																																																																	trainOptions.deltaMargin = double.Parse(args[i + 1]);
																																																																																																																																	i += 2;
																																																																																																																																}
																																																																																																																																else
																																																																																																																																{
																																																																																																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownNumberVector"))
																																																																																																																																	{
																																																																																																																																		trainOptions.unknownNumberVector = true;
																																																																																																																																		i += 1;
																																																																																																																																	}
																																																																																																																																	else
																																																																																																																																	{
																																																																																																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUnknownNumberVector"))
																																																																																																																																		{
																																																																																																																																			trainOptions.unknownNumberVector = false;
																																																																																																																																			i += 1;
																																																																																																																																		}
																																																																																																																																		else
																																																																																																																																		{
																																																																																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownDashedWordVectors"))
																																																																																																																																			{
																																																																																																																																				trainOptions.unknownDashedWordVectors = true;
																																																																																																																																				i += 1;
																																																																																																																																			}
																																																																																																																																			else
																																																																																																																																			{
																																																																																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUnknownDashedWordVectors"))
																																																																																																																																				{
																																																																																																																																					trainOptions.unknownDashedWordVectors = false;
																																																																																																																																					i += 1;
																																																																																																																																				}
																																																																																																																																				else
																																																																																																																																				{
																																																																																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownCapsVector"))
																																																																																																																																					{
																																																																																																																																						trainOptions.unknownCapsVector = true;
																																																																																																																																						i += 1;
																																																																																																																																					}
																																																																																																																																					else
																																																																																																																																					{
																																																																																																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUnknownCapsVector"))
																																																																																																																																						{
																																																																																																																																							trainOptions.unknownCapsVector = false;
																																																																																																																																							i += 1;
																																																																																																																																						}
																																																																																																																																						else
																																																																																																																																						{
																																																																																																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownChineseYearVector"))
																																																																																																																																							{
																																																																																																																																								trainOptions.unknownChineseYearVector = true;
																																																																																																																																								i += 1;
																																																																																																																																							}
																																																																																																																																							else
																																																																																																																																							{
																																																																																																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUnknownChineseYearVector"))
																																																																																																																																								{
																																																																																																																																									trainOptions.unknownChineseYearVector = false;
																																																																																																																																									i += 1;
																																																																																																																																								}
																																																																																																																																								else
																																																																																																																																								{
																																																																																																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownChineseNumberVector"))
																																																																																																																																									{
																																																																																																																																										trainOptions.unknownChineseNumberVector = true;
																																																																																																																																										i += 1;
																																																																																																																																									}
																																																																																																																																									else
																																																																																																																																									{
																																																																																																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUnknownChineseNumberVector"))
																																																																																																																																										{
																																																																																																																																											trainOptions.unknownChineseNumberVector = false;
																																																																																																																																											i += 1;
																																																																																																																																										}
																																																																																																																																										else
																																																																																																																																										{
																																																																																																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unknownChinesePercentVector"))
																																																																																																																																											{
																																																																																																																																												trainOptions.unknownChinesePercentVector = true;
																																																																																																																																												i += 1;
																																																																																																																																											}
																																																																																																																																											else
																																																																																																																																											{
																																																																																																																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUnknownChinesePercentVector"))
																																																																																																																																												{
																																																																																																																																													trainOptions.unknownChinesePercentVector = false;
																																																																																																																																													i += 1;
																																																																																																																																												}
																																																																																																																																												else
																																																																																																																																												{
																																																																																																																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dvSimplifiedModel"))
																																																																																																																																													{
																																																																																																																																														trainOptions.dvSimplifiedModel = true;
																																																																																																																																														i += 1;
																																																																																																																																													}
																																																																																																																																													else
																																																																																																																																													{
																																																																																																																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-scalingForInit"))
																																																																																																																																														{
																																																																																																																																															trainOptions.scalingForInit = double.Parse(args[i + 1]);
																																																																																																																																															i += 2;
																																																																																																																																														}
																																																																																																																																														else
																																																																																																																																														{
																																																																																																																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-rerankerKBest"))
																																																																																																																																															{
																																																																																																																																																rerankerKBest = System.Convert.ToInt32(args[i + 1]);
																																																																																																																																																i += 2;
																																																																																																																																															}
																																																																																																																																															else
																																																																																																																																															{
																																																																																																																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-baseParserWeight"))
																																																																																																																																																{
																																																																																																																																																	baseParserWeight = double.Parse(args[i + 1]);
																																																																																																																																																	i += 2;
																																																																																																																																																}
																																																																																																																																																else
																																																																																																																																																{
																																																																																																																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unkWord"))
																																																																																																																																																	{
																																																																																																																																																		trainOptions.unkWord = args[i + 1];
																																																																																																																																																		i += 2;
																																																																																																																																																	}
																																																																																																																																																	else
																																																																																																																																																	{
																																																																																																																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-lowercaseWordVectors"))
																																																																																																																																																		{
																																																																																																																																																			trainOptions.lowercaseWordVectors = true;
																																																																																																																																																			i += 1;
																																																																																																																																																		}
																																																																																																																																																		else
																																																																																																																																																		{
																																																																																																																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noLowercaseWordVectors"))
																																																																																																																																																			{
																																																																																																																																																				trainOptions.lowercaseWordVectors = false;
																																																																																																																																																				i += 1;
																																																																																																																																																			}
																																																																																																																																																			else
																																																																																																																																																			{
																																																																																																																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-transformMatrixType"))
																																																																																																																																																				{
																																																																																																																																																					trainOptions.transformMatrixType = TrainOptions.TransformMatrixType.ValueOf(args[i + 1]);
																																																																																																																																																					i += 2;
																																																																																																																																																				}
																																																																																																																																																				else
																																																																																																																																																				{
																																																																																																																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useContextWords"))
																																																																																																																																																					{
																																																																																																																																																						trainOptions.useContextWords = true;
																																																																																																																																																						i += 1;
																																																																																																																																																					}
																																																																																																																																																					else
																																																																																																																																																					{
																																																																																																																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noUseContextWords"))
																																																																																																																																																						{
																																																																																																																																																							trainOptions.useContextWords = false;
																																																																																																																																																							i += 1;
																																																																																																																																																						}
																																																																																																																																																						else
																																																																																																																																																						{
																																																																																																																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainWordVectors"))
																																																																																																																																																							{
																																																																																																																																																								trainOptions.trainWordVectors = true;
																																																																																																																																																								i += 1;
																																																																																																																																																							}
																																																																																																																																																							else
																																																																																																																																																							{
																																																																																																																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noTrainWordVectors"))
																																																																																																																																																								{
																																																																																																																																																									trainOptions.trainWordVectors = false;
																																																																																																																																																									i += 1;
																																																																																																																																																								}
																																																																																																																																																								else
																																																																																																																																																								{
																																																																																																																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markStrahler"))
																																																																																																																																																									{
																																																																																																																																																										trainOptions.markStrahler = true;
																																																																																																																																																										i += 1;
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
			return i;
		}

		[System.Serializable]
		public class LexOptions
		{
			/// <summary>Whether to use suffix and capitalization information for unknowns.</summary>
			/// <remarks>
			/// Whether to use suffix and capitalization information for unknowns.
			/// Within the BaseLexicon model options have the following meaning:
			/// 0 means a single unknown token.  1 uses suffix, and capitalization.
			/// 2 uses a variant (richer) form of signature.  Good.
			/// Use this one.  Using the richer signatures in versions 3 or 4 seems
			/// to have very marginal or no positive value.
			/// 3 uses a richer form of signature that mimics the NER word type
			/// patterns.  4 is a variant of 2.  5 is another with more English
			/// specific morphology (good for English unknowns!).
			/// 6-9 are options for Arabic.  9 codes some patterns for numbers and
			/// derivational morphology, but also supports unknownPrefixSize and
			/// unknownSuffixSize.
			/// For German, 0 means a single unknown token, and non-zero means to use
			/// capitalization of first letter and a suffix of length
			/// unknownSuffixSize.
			/// </remarks>
			public int useUnknownWordSignatures = 0;

			/// <summary>
			/// RS: file for Turian's word vectors
			/// The default value is an example of size 25 word vectors on the nlp machines
			/// </summary>
			public const string DefaultWordVectorFile = "/u/scr/nlp/deeplearning/datasets/turian/embeddings-scaled.EMBEDDING_SIZE=25.txt";

			public string wordVectorFile = DefaultWordVectorFile;

			/// <summary>Number of hidden units in the word vectors.</summary>
			/// <remarks>
			/// Number of hidden units in the word vectors.  As setting of 0
			/// will make it try to extract the size from the data file.
			/// </remarks>
			public int numHid = 0;

			/// <summary>Words more common than this are tagged with MLE P(t|w).</summary>
			/// <remarks>
			/// Words more common than this are tagged with MLE P(t|w). Default 100. The
			/// smoothing is sufficiently slight that changing this has little effect.
			/// But set this to 0 to be able to use the parser as a vanilla PCFG with
			/// no smoothing (not as a practical parser but for exposition or debugging).
			/// </remarks>
			public int smoothInUnknownsThreshold = 100;

			/// <summary>Smarter smoothing for rare words.</summary>
			public bool smartMutation = false;

			/// <summary>Make use of unicode code point types in smoothing.</summary>
			public bool useUnicodeType = false;

			/// <summary>
			/// For certain Lexicons, a certain number of word-final letters are
			/// used to subclassify the unknown token.
			/// </summary>
			/// <remarks>
			/// For certain Lexicons, a certain number of word-final letters are
			/// used to subclassify the unknown token. This gives the number of
			/// letters.
			/// </remarks>
			public int unknownSuffixSize = 1;

			/// <summary>
			/// For certain Lexicons, a certain number of word-initial letters are
			/// used to subclassify the unknown token.
			/// </summary>
			/// <remarks>
			/// For certain Lexicons, a certain number of word-initial letters are
			/// used to subclassify the unknown token. This gives the number of
			/// letters.
			/// </remarks>
			public int unknownPrefixSize = 1;

			/// <summary>Model for unknown words that the lexicon should use.</summary>
			/// <remarks>
			/// Model for unknown words that the lexicon should use.  This is the
			/// name of a class.
			/// </remarks>
			public string uwModelTrainer;

			public bool flexiTag = false;

			/// <summary>
			/// Whether to use signature rather than just being unknown as prior in
			/// known word smoothing.
			/// </summary>
			/// <remarks>
			/// Whether to use signature rather than just being unknown as prior in
			/// known word smoothing.  Currently only works if turned on for English.
			/// </remarks>
			public bool useSignatureForKnownSmoothing;

			/// <summary>
			/// A file of word class data which may be used for smoothing,
			/// normally instead of hand-specified signatures.
			/// </summary>
			public string wordClassesFile;

			private const long serialVersionUID = 2805351374506855632L;

			private static readonly string[] @params = new string[] { "useUnknownWordSignatures", "smoothInUnknownsThreshold", "smartMutation", "useUnicodeType", "unknownSuffixSize", "unknownPrefixSize", "flexiTag", "useSignatureForKnownSmoothing", "wordClassesFile"
				 };

			// = null;
			/* If this option is false, then all words that were seen in the training
			* data (even once) are constrained to only have seen tags.  That is,
			* mle is used for the lexicon.
			* If this option is true, then if a word has been seen more than
			* smoothInUnknownsThreshold, then it will still only get tags with which
			* it has been seen, but rarer words will get all tags for which the
			* unknown word model (or smart mutation) does not give a score of -Inf.
			* This will normally be all open class tags.
			* If floodTags is invoked by the parser, all other tags will also be
			* given a minimal non-zero, non-infinite probability.
			*/
			public override string ToString()
			{
				return @params[0] + " " + useUnknownWordSignatures + "\n" + @params[1] + " " + smoothInUnknownsThreshold + "\n" + @params[2] + " " + smartMutation + "\n" + @params[3] + " " + useUnicodeType + "\n" + @params[4] + " " + unknownSuffixSize + "\n"
					 + @params[5] + " " + unknownPrefixSize + "\n" + @params[6] + " " + flexiTag + "\n" + @params[7] + " " + useSignatureForKnownSmoothing + "\n" + @params[8] + " " + wordClassesFile + "\n";
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual void ReadData(BufferedReader @in)
			{
				for (int i = 0; i < @params.Length; i++)
				{
					string line = @in.ReadLine();
					int idx = line.IndexOf(' ');
					string key = Sharpen.Runtime.Substring(line, 0, idx);
					string value = Sharpen.Runtime.Substring(line, idx + 1);
					if (!Sharpen.Runtime.EqualsIgnoreCase(key, @params[i]))
					{
						log.Info("Yikes!!! Expected " + @params[i] + " got " + key);
					}
					switch (i)
					{
						case 0:
						{
							useUnknownWordSignatures = System.Convert.ToInt32(value);
							break;
						}

						case 1:
						{
							smoothInUnknownsThreshold = System.Convert.ToInt32(value);
							break;
						}

						case 2:
						{
							smartMutation = bool.Parse(value);
							break;
						}

						case 3:
						{
							useUnicodeType = bool.Parse(value);
							break;
						}

						case 4:
						{
							unknownSuffixSize = System.Convert.ToInt32(value);
							break;
						}

						case 5:
						{
							unknownPrefixSize = System.Convert.ToInt32(value);
							break;
						}

						case 6:
						{
							flexiTag = bool.Parse(value);
							break;
						}

						case 7:
						{
							useSignatureForKnownSmoothing = bool.Parse(value);
							break;
						}

						case 8:
						{
							wordClassesFile = value;
							break;
						}
					}
				}
			}
		}

		public Options.LexOptions lexOptions = new Options.LexOptions();

		/// <summary>The treebank-specific parser parameters  to use.</summary>
		public ITreebankLangParserParams tlpParams;

		// end class LexOptions
		/// <returns>
		/// The treebank language pack for the treebank the parser
		/// is trained on.
		/// </returns>
		public virtual ITreebankLanguagePack Langpack()
		{
			return tlpParams.TreebankLanguagePack();
		}

		/// <summary>
		/// Forces parsing with strictly CNF grammar -- unary chains are converted
		/// to XP&amp;YP symbols and back
		/// </summary>
		public bool forceCNF = false;

		/// <summary>Do a PCFG parse of the sentence.</summary>
		/// <remarks>
		/// Do a PCFG parse of the sentence.  If both variables are on,
		/// also do a combined parse of the sentence.
		/// </remarks>
		public bool doPCFG = true;

		/// <summary>Do a dependency parse of the sentence.</summary>
		public bool doDep = true;

		/// <summary>if true, any child can be the head (seems rather bad!)</summary>
		public bool freeDependencies = false;

		/// <summary>Whether dependency grammar considers left/right direction.</summary>
		/// <remarks>Whether dependency grammar considers left/right direction. Good.</remarks>
		public bool directional = true;

		public bool genStop = true;

		public bool useSmoothTagProjection = false;

		public bool useUnigramWordSmoothing = false;

		/// <summary>Use distance bins in the dependency calculations</summary>
		public bool distance = true;

		/// <summary>Use coarser distance (4 bins) in dependency calculations</summary>
		public bool coarseDistance = false;

		/// <summary>"double count" tags rewrites as word in PCFG and Dep parser.</summary>
		/// <remarks>
		/// "double count" tags rewrites as word in PCFG and Dep parser.  Good for
		/// combined parsing only (it used to not kick in for PCFG parsing).  This
		/// option is only used at Test time, but it is now in Options, so the
		/// correct choice for a grammar is recorded by a serialized parser.
		/// You should turn this off for a vanilla PCFG parser.
		/// </remarks>
		public bool dcTags = true;

		/// <summary>
		/// If true, inside the factored parser, remove any node from the final
		/// chosen tree which improves the PCFG score.
		/// </summary>
		/// <remarks>
		/// If true, inside the factored parser, remove any node from the final
		/// chosen tree which improves the PCFG score. This was added as the
		/// dependency factor tends to encourage 'deep' trees.
		/// </remarks>
		public bool nodePrune = false;

		public TrainOptions trainOptions;

		/// <summary>Separated out so subclasses of Options can override</summary>
		public virtual TrainOptions NewTrainOptions()
		{
			return new TrainOptions();
		}

		/// <summary>Note that the TestOptions is transient.</summary>
		/// <remarks>
		/// Note that the TestOptions is transient.  This means that whatever
		/// options get set at creation time are forgotten when the parser is
		/// serialized.  If you want an option to be remembered when the
		/// parser is reloaded, put it in either TrainOptions or in this
		/// class itself.
		/// </remarks>
		[System.NonSerialized]
		public TestOptions testOptions;

		/// <summary>Separated out so subclasses of Options can override</summary>
		public virtual TestOptions NewTestOptions()
		{
			return new TestOptions();
		}

		/// <summary>
		/// A function that maps words used in training and testing to new
		/// words.
		/// </summary>
		/// <remarks>
		/// A function that maps words used in training and testing to new
		/// words.  For example, it could be a function to lowercase text,
		/// such as edu.stanford.nlp.util.LowercaseFunction (which makes the
		/// parser case insensitive).  This function is applied in
		/// LexicalizedParserQuery.parse and in the training methods which
		/// build a new parser.
		/// </remarks>
		public Func<string, string> wordFunction = null;

		/// <summary>
		/// If the parser has a reranker, it looks at this many trees when
		/// building the reranked list.
		/// </summary>
		public int rerankerKBest = 100;

		/// <summary>
		/// If reranking sentences, we can use the score from the original
		/// parser as well.
		/// </summary>
		/// <remarks>
		/// If reranking sentences, we can use the score from the original
		/// parser as well.  This tells us how much weight to give that score.
		/// </remarks>
		public double baseParserWeight = 0.0;

		/// <summary>
		/// Making the TestOptions transient means it won't even be
		/// constructed when you deserialize an Options, so we need to
		/// construct it on our own when deserializing
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			testOptions = NewTestOptions();
		}

		public virtual void Display()
		{
			//    try {
			log.Info("Options parameters:");
			WriteData(new PrintWriter(System.Console.Error));
		}

		/*    } catch (IOException e) {
		e.printStackTrace();
		}*/
		public virtual void WriteData(TextWriter w)
		{
			//throws IOException {
			PrintWriter @out = new PrintWriter(w);
			StringBuilder sb = new StringBuilder();
			sb.Append(lexOptions.ToString());
			sb.Append("parserParams ").Append(tlpParams.GetType().FullName).Append("\n");
			sb.Append("forceCNF ").Append(forceCNF).Append("\n");
			sb.Append("doPCFG ").Append(doPCFG).Append("\n");
			sb.Append("doDep ").Append(doDep).Append("\n");
			sb.Append("freeDependencies ").Append(freeDependencies).Append("\n");
			sb.Append("directional ").Append(directional).Append("\n");
			sb.Append("genStop ").Append(genStop).Append("\n");
			sb.Append("distance ").Append(distance).Append("\n");
			sb.Append("coarseDistance ").Append(coarseDistance).Append("\n");
			sb.Append("dcTags ").Append(dcTags).Append("\n");
			sb.Append("nPrune ").Append(nodePrune).Append("\n");
			@out.Print(sb.ToString());
			@out.Flush();
		}

		/// <summary>Populates data in this Options from the character stream.</summary>
		/// <param name="in">The Reader</param>
		/// <exception cref="System.IO.IOException">If there is a problem reading data</exception>
		public virtual void ReadData(BufferedReader @in)
		{
			string line;
			string value;
			// skip old variables if still present
			lexOptions.ReadData(@in);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			try
			{
				tlpParams = (ITreebankLangParserParams)System.Activator.CreateInstance(Sharpen.Runtime.GetType(value));
			}
			catch (Exception e)
			{
				IOException ioe = new IOException("Problem instantiating parserParams: " + line);
				ioe.InitCause(e);
				throw ioe;
			}
			line = @in.ReadLine();
			// ensure backwards compatibility
			if (line.Matches("^forceCNF.*"))
			{
				value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
				forceCNF = bool.Parse(value);
				line = @in.ReadLine();
			}
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			doPCFG = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			doDep = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			freeDependencies = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			directional = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			genStop = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			distance = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			coarseDistance = bool.Parse(value);
			line = @in.ReadLine();
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			dcTags = bool.Parse(value);
			line = @in.ReadLine();
			if (!line.Matches("^nPrune.*"))
			{
				throw new Exception("Expected nPrune, found: " + line);
			}
			value = Sharpen.Runtime.Substring(line, line.IndexOf(' ') + 1);
			nodePrune = bool.Parse(value);
			line = @in.ReadLine();
			// get rid of last line
			if (line.Length != 0)
			{
				throw new Exception("Expected blank line, found: " + line);
			}
		}

		private const long serialVersionUID = 4L;
	}
}

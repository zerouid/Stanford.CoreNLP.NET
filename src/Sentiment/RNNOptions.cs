using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Sentiment
{
	[System.Serializable]
	public class RNNOptions
	{
		/// <summary>The random seed the random number generator is initialized with.</summary>
		public int randomSeed = new Random().NextInt();

		/// <summary>Filename for the word vectors</summary>
		public string wordVectors;

		/// <summary>In the wordVectors file, what word represents unknown?</summary>
		public string unkWord = "UNK";

		/// <summary>
		/// By default, initialize random word vectors instead of reading
		/// from a file
		/// </summary>
		public bool randomWordVectors = true;

		/// <summary>Size of vectors to use.</summary>
		/// <remarks>
		/// Size of vectors to use.  Must be at most the size of the vectors
		/// in the word vector file.  If a smaller size is specified, vectors
		/// will be truncated.
		/// </remarks>
		public int numHid = 25;

		/// <summary>Number of classes to build the RNN for</summary>
		public int numClasses = 5;

		public bool lowercaseWordVectors = false;

		public bool useTensors = true;

		public ITreebankLanguagePack langpack = new PennTreebankLanguagePack();

		/// <summary>No syntactic untying - use the same matrix/tensor for all categories.</summary>
		/// <remarks>
		/// No syntactic untying - use the same matrix/tensor for all categories.
		/// This results in all nodes getting the same matrix (and tensor,
		/// where applicable)
		/// </remarks>
		public bool simplifiedModel = true;

		/// <summary>
		/// If this option is true, then the binary and unary classification
		/// matrices are combined.
		/// </summary>
		/// <remarks>
		/// If this option is true, then the binary and unary classification
		/// matrices are combined.  Only makes sense if simplifiedModel is true.
		/// If combineClassification is set to true, simplifiedModel will
		/// also be set to true.  If simplifiedModel is set to false, this
		/// will be set to false.
		/// </remarks>
		public bool combineClassification = true;

		public RNNTrainOptions trainOptions = new RNNTrainOptions();

		public static readonly string[] DefaultClassNames = new string[] { "Very negative", "Negative", "Neutral", "Positive", "Very positive" };

		public static readonly string[] BinaryDefaultClassNames = new string[] { "Negative", "Positive" };

		public string[] classNames = DefaultClassNames;

		public static readonly int[][] ApproximateEquivalenceClasses = new int[][] { new int[] { 0, 1 }, new int[] { 3, 4 } };

		public static readonly int[][] BinaryApproximateEquivalenceClasses = new int[][] { new int[] { 0 }, new int[] { 1 } };

		/// <summary>
		/// The following option represents classes which can be treated as
		/// equivalent when scoring.
		/// </summary>
		/// <remarks>
		/// The following option represents classes which can be treated as
		/// equivalent when scoring.  There will be two separate scorings,
		/// one with equivalence used and one without.  Default is set for
		/// the sentiment project.
		/// </remarks>
		public int[][] equivalenceClasses = ApproximateEquivalenceClasses;

		public static readonly string[] DefaultEquivalenceClassNames = new string[] { "Negative", "Positive" };

		public string[] equivalenceClassNames = DefaultEquivalenceClassNames;

		public RNNTestOptions testOptions = new RNNTestOptions();

		// TODO [2014]: This should really be a long
		// TODO: add an option to set this to some other language pack
		// almost an owl
		// TODO: we can remove this if we reserialize all the models
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		private void ReadObject(ObjectInputStream @in)
		{
			@in.DefaultReadObject();
			if (testOptions == null)
			{
				testOptions = new RNNTestOptions();
			}
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("GENERAL OPTIONS\n");
			result.Append("randomSeed=" + randomSeed + "\n");
			result.Append("wordVectors=" + wordVectors + "\n");
			result.Append("unkWord=" + unkWord + "\n");
			result.Append("randomWordVectors=" + randomWordVectors + "\n");
			result.Append("numHid=" + numHid + "\n");
			result.Append("numClasses=" + numClasses + "\n");
			result.Append("lowercaseWordVectors=" + lowercaseWordVectors + "\n");
			result.Append("useTensors=" + useTensors + "\n");
			result.Append("simplifiedModel=" + simplifiedModel + "\n");
			result.Append("combineClassification=" + combineClassification + "\n");
			result.Append("classNames=" + StringUtils.Join(classNames, ",") + "\n");
			result.Append("equivalenceClasses=");
			if (equivalenceClasses != null)
			{
				for (int i = 0; i < equivalenceClasses.Length; ++i)
				{
					if (i > 0)
					{
						result.Append(";");
					}
					for (int j = 0; j < equivalenceClasses[i].Length; ++j)
					{
						if (j > 0)
						{
							result.Append(",");
						}
						result.Append(equivalenceClasses[i][j]);
					}
				}
			}
			result.Append("\n");
			result.Append("equivalenceClassNames=");
			if (equivalenceClassNames != null)
			{
				result.Append(StringUtils.Join(equivalenceClassNames, ","));
			}
			result.Append("\n");
			result.Append(trainOptions.ToString());
			result.Append(testOptions.ToString());
			return result.ToString();
		}

		public virtual int SetOption(string[] args, int argIndex)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-randomSeed"))
			{
				randomSeed = System.Convert.ToInt32(args[argIndex + 1]);
				return argIndex + 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-wordVectors"))
				{
					wordVectors = args[argIndex + 1];
					return argIndex + 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-unkWord"))
					{
						unkWord = args[argIndex + 1];
						return argIndex + 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-numHid"))
						{
							numHid = System.Convert.ToInt32(args[argIndex + 1]);
							return argIndex + 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-numClasses"))
							{
								numClasses = System.Convert.ToInt32(args[argIndex + 1]);
								return argIndex + 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-lowercaseWordVectors"))
								{
									lowercaseWordVectors = true;
									return argIndex + 1;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nolowercaseWordVectors"))
									{
										lowercaseWordVectors = false;
										return argIndex + 1;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-randomWordVectors"))
										{
											randomWordVectors = true;
											return argIndex + 1;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-norandomWordVectors"))
											{
												randomWordVectors = false;
												return argIndex + 1;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-simplifiedModel"))
												{
													simplifiedModel = true;
													return argIndex + 1;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nosimplifiedModel"))
													{
														simplifiedModel = false;
														combineClassification = false;
														return argIndex + 1;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-combineClassification"))
														{
															combineClassification = true;
															simplifiedModel = true;
															return argIndex + 1;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nocombineClassification"))
															{
																combineClassification = false;
																return argIndex + 1;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-useTensors"))
																{
																	useTensors = true;
																	return argIndex + 1;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nouseTensors"))
																	{
																		useTensors = false;
																		return argIndex + 1;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-classNames"))
																		{
																			classNames = args[argIndex + 1].Split(",");
																			return argIndex + 2;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-equivalenceClasses"))
																			{
																				if (args[argIndex + 1].Trim().Length == 0)
																				{
																					equivalenceClasses = null;
																					return argIndex + 2;
																				}
																				string[] pieces = args[argIndex + 1].Split(";");
																				equivalenceClasses = new int[pieces.Length][];
																				for (int i = 0; i < pieces.Length; ++i)
																				{
																					string[] values = pieces[i].Split(",");
																					equivalenceClasses[i] = new int[values.Length];
																					for (int j = 0; j < values.Length; ++j)
																					{
																						equivalenceClasses[i][j] = int.Parse(values[j]);
																					}
																				}
																				return argIndex + 2;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-equivalenceClassNames"))
																				{
																					if (args[argIndex + 1].Trim().Length > 0)
																					{
																						equivalenceClassNames = args[argIndex + 1].Split(",");
																					}
																					else
																					{
																						equivalenceClassNames = null;
																					}
																					return argIndex + 2;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-binaryModel"))
																					{
																						// macro option
																						numClasses = 2;
																						classNames = BinaryDefaultClassNames;
																						// TODO: should we just make this null?
																						equivalenceClasses = BinaryApproximateEquivalenceClasses;
																						trainOptions.SetOption(args, argIndex);
																						// in case the trainOptions use binaryModel as well
																						return argIndex + 1;
																					}
																					else
																					{
																						int newIndex = trainOptions.SetOption(args, argIndex);
																						if (newIndex == argIndex)
																						{
																							newIndex = testOptions.SetOption(args, argIndex);
																						}
																						return newIndex;
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

		private const long serialVersionUID = 1;
	}
}

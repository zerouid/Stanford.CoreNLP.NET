


namespace Edu.Stanford.Nlp.Sentiment
{
	[System.Serializable]
	public class RNNTrainOptions
	{
		public int batchSize = 27;

		/// <summary>Number of times through all the trees</summary>
		public int epochs = 400;

		public int debugOutputEpochs = 8;

		public int maxTrainTimeSeconds = 60 * 60 * 24;

		public double learningRate = 0.01;

		public double scalingForInit = 1.0;

		private double[] classWeights = null;

		/// <summary>
		/// The classWeights can be passed in as a comma separated list of
		/// weights using the -classWeights flag.
		/// </summary>
		/// <remarks>
		/// The classWeights can be passed in as a comma separated list of
		/// weights using the -classWeights flag.  If the classWeights are
		/// not specified, the value is assumed to be 1.0.  classWeights only
		/// apply at train time; we do not weight the classes at all during
		/// test time.
		/// </remarks>
		public virtual double GetClassWeight(int i)
		{
			if (classWeights == null)
			{
				return 1.0;
			}
			return classWeights[i];
		}

		/// <summary>Regularization cost for the transform matrix</summary>
		public double regTransformMatrix = 0.001;

		/// <summary>Regularization cost for the classification matrices</summary>
		public double regClassification = 0.0001;

		/// <summary>Regularization cost for the word vectors</summary>
		public double regWordVector = 0.0001;

		/// <summary>The value to set the learning rate for each parameter when initializing adagrad.</summary>
		public double initialAdagradWeight = 0.0;

		/// <summary>How many epochs between resets of the adagrad learning rates.</summary>
		/// <remarks>
		/// How many epochs between resets of the adagrad learning rates.
		/// Set to 0 to never reset.
		/// </remarks>
		public int adagradResetFrequency = 1;

		/// <summary>Regularization cost for the transform tensor</summary>
		public double regTransformTensor = 0.001;

		/// <summary>Shuffle matrices when training.</summary>
		/// <remarks>
		/// Shuffle matrices when training.  Usually should be true.  Set to
		/// false to compare training across different implementations, such
		/// as with the original Matlab version
		/// </remarks>
		public bool shuffleMatrices = true;

		/// <summary>
		/// If set, the initial matrices are logged to this location as a single file
		/// using SentimentModel.toString()
		/// </summary>
		public string initialMatrixLogPath = null;

		public int nThreads = 1;

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			result.Append("TRAIN OPTIONS\n");
			result.Append("batchSize=" + batchSize + "\n");
			result.Append("epochs=" + epochs + "\n");
			result.Append("debugOutputEpochs=" + debugOutputEpochs + "\n");
			result.Append("maxTrainTimeSeconds=" + maxTrainTimeSeconds + "\n");
			result.Append("learningRate=" + learningRate + "\n");
			result.Append("scalingForInit=" + scalingForInit + "\n");
			if (classWeights == null)
			{
				result.Append("classWeights=null\n");
			}
			else
			{
				result.Append("classWeights=");
				result.Append(classWeights[0]);
				for (int i = 1; i < classWeights.Length; ++i)
				{
					result.Append("," + classWeights[i]);
				}
				result.Append("\n");
			}
			result.Append("regTransformMatrix=" + regTransformMatrix + "\n");
			result.Append("regTransformTensor=" + regTransformTensor + "\n");
			result.Append("regClassification=" + regClassification + "\n");
			result.Append("regWordVector=" + regWordVector + "\n");
			result.Append("initialAdagradWeight=" + initialAdagradWeight + "\n");
			result.Append("adagradResetFrequency=" + adagradResetFrequency + "\n");
			result.Append("shuffleMatrices=" + shuffleMatrices + "\n");
			result.Append("initialMatrixLogPath=" + initialMatrixLogPath + "\n");
			result.Append("nThreads=" + nThreads + "\n");
			return result.ToString();
		}

		public virtual int SetOption(string[] args, int argIndex)
		{
			if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-batchSize"))
			{
				batchSize = System.Convert.ToInt32(args[argIndex + 1]);
				return argIndex + 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-epochs"))
				{
					epochs = System.Convert.ToInt32(args[argIndex + 1]);
					return argIndex + 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-debugOutputEpochs"))
					{
						debugOutputEpochs = System.Convert.ToInt32(args[argIndex + 1]);
						return argIndex + 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-maxTrainTimeSeconds"))
						{
							maxTrainTimeSeconds = System.Convert.ToInt32(args[argIndex + 1]);
							return argIndex + 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-learningRate"))
							{
								learningRate = double.ParseDouble(args[argIndex + 1]);
								return argIndex + 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-scalingForInit"))
								{
									scalingForInit = double.ParseDouble(args[argIndex + 1]);
									return argIndex + 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-regTransformMatrix"))
									{
										regTransformMatrix = double.ParseDouble(args[argIndex + 1]);
										return argIndex + 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-regTransformTensor"))
										{
											regTransformTensor = double.ParseDouble(args[argIndex + 1]);
											return argIndex + 2;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-regClassification"))
											{
												regClassification = double.ParseDouble(args[argIndex + 1]);
												return argIndex + 2;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-regWordVector"))
												{
													regWordVector = double.ParseDouble(args[argIndex + 1]);
													return argIndex + 2;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-initialAdagradWeight"))
													{
														initialAdagradWeight = double.ParseDouble(args[argIndex + 1]);
														return argIndex + 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-adagradResetFrequency"))
														{
															adagradResetFrequency = System.Convert.ToInt32(args[argIndex + 1]);
															return argIndex + 2;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-classWeights"))
															{
																string classWeightString = args[argIndex + 1];
																string[] pieces = classWeightString.Split(",");
																classWeights = new double[pieces.Length];
																for (int i = 0; i < pieces.Length; ++i)
																{
																	classWeights[i] = double.ParseDouble(pieces[i]);
																}
																return argIndex + 2;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-shuffleMatrices"))
																{
																	shuffleMatrices = true;
																	return argIndex + 1;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-noShuffleMatrices"))
																	{
																		shuffleMatrices = false;
																		return argIndex + 1;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-initialMatrixLogPath"))
																		{
																			initialMatrixLogPath = args[argIndex + 1];
																			return argIndex + 2;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-nThreads") || Sharpen.Runtime.EqualsIgnoreCase(args[argIndex], "-numThreads"))
																			{
																				nThreads = System.Convert.ToInt32(args[argIndex + 1]);
																				return argIndex + 2;
																			}
																			else
																			{
																				return argIndex;
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

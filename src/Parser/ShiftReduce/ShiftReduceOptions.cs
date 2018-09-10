using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	[System.Serializable]
	public class ShiftReduceOptions : Options
	{
		public override Edu.Stanford.Nlp.Parser.Lexparser.TrainOptions NewTrainOptions()
		{
			return new ShiftReduceTrainOptions();
		}

		public override Edu.Stanford.Nlp.Parser.Lexparser.TestOptions NewTestOptions()
		{
			return new ShiftReduceTestOptions();
		}

		internal virtual ShiftReduceTrainOptions TrainOptions()
		{
			return ErasureUtils.UncheckedCast(trainOptions);
		}

		internal virtual ShiftReduceTestOptions TestOptions()
		{
			return ErasureUtils.UncheckedCast(testOptions);
		}

		public bool compoundUnaries = true;

		public string featureFactoryClass = "edu.stanford.nlp.parser.shiftreduce.BasicFeatureFactory";

		protected internal override int SetOptionFlag(string[] args, int i)
		{
			int j = base.SetOptionFlag(args, i);
			if (i != j)
			{
				return j;
			}
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-beamSize"))
			{
				TestOptions().beamSize = System.Convert.ToInt32(args[i + 1]);
				i += 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainBeamSize"))
				{
					TrainOptions().beamSize = System.Convert.ToInt32(args[i + 1]);
					i += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-compoundUnaries"))
					{
						compoundUnaries = true;
						i++;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nocompoundUnaries"))
						{
							compoundUnaries = false;
							i++;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-featureFactory"))
							{
								featureFactoryClass = args[i + 1];
								i += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-averagedModels"))
								{
									TrainOptions().averagedModels = System.Convert.ToInt32(args[i + 1]);
									i += 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-cvAveragedModels"))
									{
										TrainOptions().cvAveragedModels = true;
										i++;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noCVAveragedModels"))
										{
											TrainOptions().cvAveragedModels = false;
											i++;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-retrainAfterCutoff"))
											{
												TrainOptions().retrainAfterCutoff = true;
												i++;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noRetrainAfterCutoff"))
												{
													TrainOptions().retrainAfterCutoff = false;
													i++;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainingMethod"))
													{
														TrainOptions().trainingMethod = ShiftReduceTrainOptions.TrainingMethod.ValueOf(args[i + 1].ToUpper());
														if (TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.Beam || TrainOptions().trainingMethod == ShiftReduceTrainOptions.TrainingMethod.ReorderBeam)
														{
															if (TrainOptions().beamSize <= 0)
															{
																TrainOptions().beamSize = ShiftReduceTrainOptions.DefaultBeamSize;
															}
															if (TestOptions().beamSize <= 0)
															{
																TestOptions().beamSize = TrainOptions().beamSize;
															}
														}
														i += 2;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-featureFrequencyCutoff"))
														{
															TrainOptions().featureFrequencyCutoff = System.Convert.ToInt32(args[i + 1]);
															i += 2;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-saveIntermediateModels"))
															{
																TrainOptions().saveIntermediateModels = true;
																i++;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nosaveIntermediateModels"))
																{
																	TrainOptions().saveIntermediateModels = false;
																	i++;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-oracleShiftToBinary"))
																	{
																		TrainOptions().oracleShiftToBinary = true;
																		i++;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-oracleBinaryToShift"))
																		{
																			TrainOptions().oracleBinaryToShift = true;
																			i++;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-recordBinarized"))
																			{
																				TestOptions().recordBinarized = args[i + 1];
																				i += 2;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-recordDebinarized"))
																				{
																					TestOptions().recordDebinarized = args[i + 1];
																					i += 2;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-decayLearningRate"))
																					{
																						TrainOptions().decayLearningRate = double.Parse(args[i + 1]);
																						i += 2;
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

		private const long serialVersionUID = 1L;
	}
}

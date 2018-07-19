using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.International.French
{
	/// <summary>
	/// If MorphoFeatureType.OTHER is active, then the "CC tagset" is produced (see Tbl.2
	/// of (Crabbe and Candito, 2008).
	/// </summary>
	/// <remarks>
	/// If MorphoFeatureType.OTHER is active, then the "CC tagset" is produced (see Tbl.2
	/// of (Crabbe and Candito, 2008). Additional support exists for GEN, NUM, and PER, which
	/// are (mostly) marked in the FTB annotation.
	/// <p>
	/// The actual CC tag is placed in the altTag field of the MorphoFeatures object.
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class FrenchMorphoFeatureSpecification : MorphoFeatureSpecification
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(FrenchMorphoFeatureSpecification));

		private const long serialVersionUID = -58379347760106784L;

		public static readonly string[] genVals = new string[] { "M", "F" };

		public static readonly string[] numVals = new string[] { "SG", "PL" };

		public static readonly string[] perVals = new string[] { "1", "2", "3" };

		public override IList<string> GetValues(MorphoFeatureSpecification.MorphoFeatureType feat)
		{
			if (feat == MorphoFeatureSpecification.MorphoFeatureType.Gen)
			{
				return Arrays.AsList(genVals);
			}
			else
			{
				if (feat == MorphoFeatureSpecification.MorphoFeatureType.Num)
				{
					return Arrays.AsList(numVals);
				}
				else
				{
					if (feat == MorphoFeatureSpecification.MorphoFeatureType.Per)
					{
						return Arrays.AsList(perVals);
					}
					else
					{
						throw new ArgumentException("French does not support feature type: " + feat.ToString());
					}
				}
			}
		}

		public override MorphoFeatures StrToFeatures(string spec)
		{
			MorphoFeatures feats = new MorphoFeatures();
			//Usually this is the boundary symbol
			if (spec == null || spec.Equals(string.Empty))
			{
				return feats;
			}
			bool isOtherActive = IsActive(MorphoFeatureSpecification.MorphoFeatureType.Other);
			if (spec.StartsWith("ADV"))
			{
				feats.SetAltTag("ADV");
				if (spec.Contains("int"))
				{
					if (isOtherActive)
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "advint");
					}
					feats.SetAltTag("ADVWH");
				}
			}
			else
			{
				if (spec.StartsWith("A"))
				{
					feats.SetAltTag("ADJ");
					if (spec.Contains("int"))
					{
						if (isOtherActive)
						{
							feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "adjint");
						}
						feats.SetAltTag("ADJWH");
					}
					AddPhiFeatures(feats, spec);
				}
				else
				{
					if (spec.Equals("CC") || spec.Equals("C-C"))
					{
						if (isOtherActive)
						{
							feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Cc");
						}
						feats.SetAltTag("CC");
					}
					else
					{
						if (spec.Equals("CS") || spec.Equals("C-S"))
						{
							if (isOtherActive)
							{
								feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Cs");
							}
							feats.SetAltTag("CS");
						}
						else
						{
							if (spec.StartsWith("CL"))
							{
								feats.SetAltTag("CL");
								if (spec.Contains("suj") || spec.Equals("CL-S-3fp"))
								{
									//"CL-S-3fp" is equivalent to suj
									if (isOtherActive)
									{
										feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Sbj");
									}
									feats.SetAltTag("CLS");
								}
								else
								{
									if (spec.Contains("obj"))
									{
										if (isOtherActive)
										{
											feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Obj");
										}
										feats.SetAltTag("CLO");
									}
									else
									{
										if (spec.Contains("refl"))
										{
											if (isOtherActive)
											{
												feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Rfl");
											}
											feats.SetAltTag("CLR");
										}
									}
								}
								AddPhiFeatures(feats, spec);
							}
							else
							{
								if (spec.StartsWith("D"))
								{
									feats.SetAltTag("DET");
									if (spec.Contains("int"))
									{
										if (isOtherActive)
										{
											feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "dint");
										}
										feats.SetAltTag("DETWH");
									}
									AddPhiFeatures(feats, spec);
								}
								else
								{
									if (spec.StartsWith("N"))
									{
										feats.SetAltTag("N");
										//TODO These are usually N-card...make these CD?
										if (spec.Contains("P"))
										{
											if (isOtherActive)
											{
												feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Np");
											}
											feats.SetAltTag("NPP");
										}
										else
										{
											if (spec.Contains("C"))
											{
												if (isOtherActive)
												{
													feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Nc");
												}
												feats.SetAltTag("NC");
											}
										}
										AddPhiFeatures(feats, spec);
									}
									else
									{
										if (spec.StartsWith("PRO"))
										{
											feats.SetAltTag("PRO");
											if (spec.Contains("int"))
											{
												if (isOtherActive)
												{
													feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Ni");
												}
												feats.SetAltTag("PROWH");
											}
											else
											{
												if (spec.Contains("rel"))
												{
													if (isOtherActive)
													{
														feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Nr");
													}
													feats.SetAltTag("PROREL");
												}
											}
											AddPhiFeatures(feats, spec);
										}
										else
										{
											if (spec.StartsWith("V"))
											{
												feats.SetAltTag("V");
												if (spec.Contains("Y"))
												{
													if (isOtherActive)
													{
														feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Vp");
													}
													feats.SetAltTag("VIMP");
												}
												else
												{
													if (spec.Contains("W"))
													{
														if (isOtherActive)
														{
															feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Vf");
														}
														feats.SetAltTag("VINF");
													}
													else
													{
														if (spec.Contains("S") || spec.Contains("T"))
														{
															if (isOtherActive)
															{
																feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Vs");
															}
															feats.SetAltTag("VS");
														}
														else
														{
															if (spec.Contains("K"))
															{
																if (isOtherActive)
																{
																	feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Vp");
																}
																feats.SetAltTag("VPP");
															}
															else
															{
																if (spec.Contains("G"))
																{
																	if (isOtherActive)
																	{
																		feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Other, "Vr");
																	}
																	feats.SetAltTag("VPR");
																}
															}
														}
													}
												}
												AddPhiFeatures(feats, spec);
											}
											else
											{
												if (spec.Equals("P") || spec.Equals("I"))
												{
													feats.SetAltTag(spec);
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
			//    else {
			//      log.info("Could not map spec: " + spec);
			//    }
			return feats;
		}

		private void AddPhiFeatures(MorphoFeatures feats, string spec)
		{
			string[] toks = spec.Split("\\-+");
			string morphStr;
			if (toks.Length == 3 && toks[0].Equals("PRO") && toks[2].Equals("neg"))
			{
				morphStr = toks[1];
			}
			else
			{
				morphStr = toks[toks.Length - 1];
			}
			//wsg2011: The analyses have mixed casing....
			morphStr = morphStr.ToLower();
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Gen))
			{
				if (morphStr.Contains("m"))
				{
					feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Gen, genVals[0]);
				}
				else
				{
					if (morphStr.Contains("f"))
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Gen, genVals[1]);
					}
				}
			}
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Per))
			{
				if (morphStr.Contains("1"))
				{
					feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Per, perVals[0]);
				}
				else
				{
					if (morphStr.Contains("2"))
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Per, perVals[1]);
					}
					else
					{
						if (morphStr.Contains("3"))
						{
							feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Per, perVals[2]);
						}
					}
				}
			}
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Num))
			{
				if (morphStr.Contains("s"))
				{
					feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Num, numVals[0]);
				}
				else
				{
					if (morphStr.Contains("p"))
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Num, numVals[1]);
					}
				}
			}
		}

		/// <summary>For debugging</summary>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.Error.Printf("Usage: java %s file%n", typeof(FrenchMorphoFeatureSpecification).FullName);
				System.Environment.Exit(-1);
			}
			try
			{
				BufferedReader br = new BufferedReader(new FileReader(args[0]));
				MorphoFeatureSpecification mfs = new FrenchMorphoFeatureSpecification();
				//Activate all features for debugging
				mfs.Activate(MorphoFeatureSpecification.MorphoFeatureType.Gen);
				mfs.Activate(MorphoFeatureSpecification.MorphoFeatureType.Num);
				mfs.Activate(MorphoFeatureSpecification.MorphoFeatureType.Per);
				for (string line; (line = br.ReadLine()) != null; )
				{
					MorphoFeatures feats = mfs.StrToFeatures(line);
					System.Console.Out.Printf("%s\t%s%n", line.Trim(), feats.ToString());
				}
				br.Close();
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
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Arabic
{
	/// <summary>Extracts morphosyntactic features from BAMA/SAMA analyses.</summary>
	/// <remarks>
	/// Extracts morphosyntactic features from BAMA/SAMA analyses. Compatible with both the
	/// long tags in the ATB and the output of MADA.
	/// </remarks>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicMorphoFeatureSpecification : MorphoFeatureSpecification
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(ArabicMorphoFeatureSpecification));

		private const long serialVersionUID = 4448045447200922076L;

		private static readonly string[] defVals = new string[] { "I", "D" };

		private static readonly string[] caseVals = new string[] { "NOM", "ACC", "GEN" };

		private static readonly string[] genVals = new string[] { "M", "F" };

		private static readonly string[] numVals = new string[] { "SG", "DU", "PL" };

		private static readonly string[] perVals = new string[] { "1", "2", "3" };

		private static readonly string[] possVals = new string[] { "POSS" };

		private static readonly string[] voiceVals = new string[] { "ACT", "PASS" };

		private static readonly string[] moodVals = new string[] { "I", "S", "J" };

		private static readonly string[] tenseVals = new string[] { "PAST", "PRES", "IMP" };

		private static readonly Pattern pFeatureTuple = Pattern.Compile("(\\d\\p{Upper}\\p{Upper}?)");

		private static readonly Pattern pDemPronounFeatures = Pattern.Compile("DEM_PRON(.+)");

		private static readonly Pattern pVerbMood = Pattern.Compile("MOOD|SUBJ");

		private static readonly Pattern pMood = Pattern.Compile("_MOOD:([ISJ])");

		private static readonly Pattern pVerbTenseMarker = Pattern.Compile("IV|PV|CV");

		private static readonly Pattern pNounNoMorph = Pattern.Compile("PROP|QUANT");

		// Standard feature tuple (e.g., "3MS", "1P", etc.)
		// Demonstrative pronouns do not have number
		//Verbal patterns
		public override IList<string> GetValues(MorphoFeatureSpecification.MorphoFeatureType feat)
		{
			if (feat == MorphoFeatureSpecification.MorphoFeatureType.Def)
			{
				return Arrays.AsList(defVals);
			}
			else
			{
				if (feat == MorphoFeatureSpecification.MorphoFeatureType.Case)
				{
					throw new Exception(this.GetType().FullName + ": Case is presently unsupported!");
				}
				else
				{
					//      return Arrays.asList(caseVals);
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
								if (feat == MorphoFeatureSpecification.MorphoFeatureType.Poss)
								{
									return Arrays.AsList(possVals);
								}
								else
								{
									if (feat == MorphoFeatureSpecification.MorphoFeatureType.Voice)
									{
										return Arrays.AsList(voiceVals);
									}
									else
									{
										if (feat == MorphoFeatureSpecification.MorphoFeatureType.Mood)
										{
											return Arrays.AsList(moodVals);
										}
										else
										{
											if (feat == MorphoFeatureSpecification.MorphoFeatureType.Tense)
											{
												return Arrays.AsList(tenseVals);
											}
											else
											{
												throw new ArgumentException("Arabic does not support feature type: " + feat.ToString());
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

		/// <summary>Hand-written rules to convert SAMA analyses to feature structures.</summary>
		public override MorphoFeatures StrToFeatures(string spec)
		{
			MorphoFeatures features = new ArabicMorphoFeatureSpecification.ArabicMorphoFeatures();
			// Check for the boundary symbol
			if (spec == null || spec.Equals(string.Empty))
			{
				return features;
			}
			//Possessiveness
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Poss) && spec.Contains("POSS"))
			{
				features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Poss, possVals[0]);
			}
			//Nominals and pronominals. Mona ignores Pronominals in ERTS, but they seem to help...
			// NSUFF -- declinable nominals
			// VSUFF -- enclitic pronominals
			// PRON -- ordinary pronominals
			if (spec.Contains("NSUFF") || spec.Contains("NOUN") || spec.Contains("ADJ"))
			{
				// Nominal phi feature indicators are different than the indicators
				// that we process with processInflectionalFeatures()
				if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Ngen))
				{
					if (spec.Contains("FEM"))
					{
						features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Ngen, genVals[1]);
					}
					else
					{
						if (spec.Contains("MASC") || !pNounNoMorph.Matcher(spec).Find())
						{
							features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Ngen, genVals[0]);
						}
					}
				}
				// WSGDEBUG -- Number for nominals only
				if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Nnum))
				{
					if (spec.Contains("DU"))
					{
						features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Nnum, numVals[1]);
					}
					else
					{
						if (spec.Contains("PL"))
						{
							features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Nnum, numVals[2]);
						}
						else
						{
							if (!pNounNoMorph.Matcher(spec).Find())
							{
								// (spec.contains("SG"))
								features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Nnum, numVals[0]);
							}
						}
					}
				}
				//Definiteness
				if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Def))
				{
					if (spec.Contains("DET"))
					{
						features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Def, defVals[1]);
					}
					else
					{
						if (!pNounNoMorph.Matcher(spec).Find())
						{
							features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Def, defVals[0]);
						}
					}
				}
				// Proper nouns (probably a stupid feature)
				if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Prop))
				{
					if (spec.Contains("PROP"))
					{
						features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Prop, string.Empty);
					}
				}
			}
			else
			{
				if (spec.Contains("PRON") || (spec.Contains("VSUFF_DO") && !pVerbMood.Matcher(spec).Find()))
				{
					if (spec.Contains("DEM_PRON"))
					{
						features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Def, defVals[0]);
						Matcher m = pDemPronounFeatures.Matcher(spec);
						if (m.Find())
						{
							spec = m.Group(1);
							ProcessInflectionalFeaturesHelper(features, spec);
						}
					}
					else
					{
						ProcessInflectionalFeatures(features, spec);
					}
				}
				else
				{
					// Verbs (marked for tense)
					if (pVerbTenseMarker.Matcher(spec).Find())
					{
						// Tense feature
						if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Tense))
						{
							if (spec.Contains("PV"))
							{
								features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Tense, tenseVals[0]);
							}
							else
							{
								if (spec.Contains("IV"))
								{
									features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Tense, tenseVals[1]);
								}
								else
								{
									if (spec.Contains("CV"))
									{
										features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Tense, tenseVals[2]);
									}
								}
							}
						}
						// Inflectional features
						ProcessInflectionalFeatures(features, spec);
						if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Mood))
						{
							Matcher moodMatcher = pMood.Matcher(spec);
							if (moodMatcher.Find())
							{
								string moodStr = moodMatcher.Group(1);
								switch (moodStr)
								{
									case "I":
									{
										features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Mood, moodVals[0]);
										break;
									}

									case "S":
									{
										features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Mood, moodVals[1]);
										break;
									}

									case "J":
									{
										features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Mood, moodVals[2]);
										break;
									}
								}
							}
						}
						if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Voice))
						{
							if (spec.Contains("PASS"))
							{
								features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Voice, voiceVals[1]);
							}
							else
							{
								features.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Voice, voiceVals[0]);
							}
						}
					}
				}
			}
			return features;
		}

		/// <summary>Extract features from a standard phi feature specification.</summary>
		/// <param name="feats"/>
		/// <param name="spec"/>
		private void ProcessInflectionalFeatures(MorphoFeatures feats, string spec)
		{
			// Extract the feature tuple
			Matcher m = pFeatureTuple.Matcher(spec);
			if (m.Find())
			{
				spec = m.Group(1);
				ProcessInflectionalFeaturesHelper(feats, spec);
			}
		}

		private void ProcessInflectionalFeaturesHelper(MorphoFeatures feats, string spec)
		{
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Gen))
			{
				if (spec.Contains("M"))
				{
					feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Gen, genVals[0]);
				}
				else
				{
					if (spec.Contains("F"))
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Gen, genVals[1]);
					}
				}
			}
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Num))
			{
				if (spec.EndsWith("S"))
				{
					feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Num, numVals[0]);
				}
				else
				{
					if (spec.EndsWith("D"))
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Num, numVals[1]);
					}
					else
					{
						if (spec.EndsWith("P"))
						{
							feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Num, numVals[2]);
						}
					}
				}
			}
			if (IsActive(MorphoFeatureSpecification.MorphoFeatureType.Per))
			{
				if (spec.Contains("1"))
				{
					feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Per, perVals[0]);
				}
				else
				{
					if (spec.Contains("2"))
					{
						feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Per, perVals[1]);
					}
					else
					{
						if (spec.Contains("3"))
						{
							feats.AddFeature(MorphoFeatureSpecification.MorphoFeatureType.Per, perVals[2]);
						}
					}
				}
			}
		}

		/// <summary>Converts features specifications to labels for tagging</summary>
		/// <author>Spence Green</author>
		[System.Serializable]
		public class ArabicMorphoFeatures : MorphoFeatures
		{
			private const long serialVersionUID = -4611776415583633186L;

			public override MorphoFeatures FromTagString(string str)
			{
				string[] feats = str.Split("\\-");
				MorphoFeatures mFeats = new ArabicMorphoFeatureSpecification.ArabicMorphoFeatures();
				// First element is the base POS
				//      String baseTag = feats[0];
				for (int i = 1; i < feats.Length; i++)
				{
					string[] keyValue = feats[i].Split(KeyValDelim);
					if (keyValue.Length != 2)
					{
						continue;
					}
					MorphoFeatureSpecification.MorphoFeatureType fName = MorphoFeatureSpecification.MorphoFeatureType.ValueOf(keyValue[0].Trim());
					mFeats.AddFeature(fName, keyValue[1].Trim());
				}
				return mFeats;
			}

			public override string GetTag(string basePartOfSpeech)
			{
				StringBuilder sb = new StringBuilder(basePartOfSpeech);
				// Iterate over feature list so that features are added in the same order
				// for every feature spec.
				foreach (MorphoFeatureSpecification.MorphoFeatureType feat in MorphoFeatureSpecification.MorphoFeatureType.Values())
				{
					if (HasFeature(feat))
					{
						sb.Append(string.Format("-%s:%s", feat, fSpec[feat]));
					}
				}
				return sb.ToString();
			}
		}

		/// <summary>For debugging.</summary>
		/// <remarks>
		/// For debugging. Converts a set of long tags (BAMA analyses as in the ATB) to their morpho
		/// feature specification. The input file should have one long tag per line.
		/// </remarks>
		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				System.Console.Error.Printf("Usage: java %s filename feats%n", typeof(ArabicMorphoFeatureSpecification).FullName);
				System.Environment.Exit(-1);
			}
			MorphoFeatureSpecification fSpec = new ArabicMorphoFeatureSpecification();
			string[] feats = args[1].Split(",");
			foreach (string feat in feats)
			{
				MorphoFeatureSpecification.MorphoFeatureType fType = MorphoFeatureSpecification.MorphoFeatureType.ValueOf(feat);
				fSpec.Activate(fType);
			}
			File fName = new File(args[0]);
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(fName)));
				int nLine = 0;
				for (string line; (line = br.ReadLine()) != null; nLine++)
				{
					MorphoFeatures mFeats = fSpec.StrToFeatures(line.Trim());
					System.Console.Out.Printf("%s\t%s%n", line.Trim(), mFeats.ToString());
				}
				br.Close();
				System.Console.Out.Printf("%nRead %d lines%n", nLine);
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

using System.Collections.Generic;
using System.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <summary>Utility functions for reading and writing CoNLL-U files.</summary>
	/// <author>Sebastian Schuster</author>
	public class CoNLLUUtils
	{
		/// <summary>
		/// Parses the value of the feature column in a CoNLL-U file
		/// and returns them in a HashMap with the feature names as keys
		/// and the feature values as values.
		/// </summary>
		/// <param name="featureString"/>
		/// <returns>A HashMap<String,String> with the feature values.</returns>
		public static Dictionary<string, string> ParseFeatures(string featureString)
		{
			Dictionary<string, string> features = new Dictionary<string, string>();
			if (!featureString.Equals("_"))
			{
				string[] featValPairs = featureString.Split("\\|");
				foreach (string p in featValPairs)
				{
					string[] featValPair = p.Split("=");
					features[featValPair[0]] = featValPair[1];
				}
			}
			return features;
		}

		/// <summary>
		/// Converts a feature HashMap to a feature string to be used
		/// in a CoNLL-U file.
		/// </summary>
		/// <returns>The feature string.</returns>
		public static string ToFeatureString(Dictionary<string, string> features)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			if (features != null)
			{
				IList<string> sortedKeys = new List<string>(features.Keys);
				sortedKeys.Sort(new CoNLLUUtils.FeatureNameComparator());
				foreach (string key in sortedKeys)
				{
					if (!first)
					{
						sb.Append("|");
					}
					else
					{
						first = false;
					}
					sb.Append(key).Append("=").Append(features[key]);
				}
			}
			/* Empty feature list. */
			if (first)
			{
				sb.Append("_");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Parses the value of the extra dependencies column in a CoNLL-U file
		/// and returns them in a HashMap with the governor indices as keys
		/// and the relation names as values.
		/// </summary>
		/// <param name="extraDepsString"/>
		/// <returns>A HashMap<Integer,String> with the additional dependencies.</returns>
		public static Dictionary<string, string> ParseExtraDeps(string extraDepsString)
		{
			Dictionary<string, string> extraDeps = new Dictionary<string, string>();
			if (!extraDepsString.Equals("_"))
			{
				string[] extraDepParts = extraDepsString.Split("\\|");
				foreach (string extraDepString in extraDepParts)
				{
					int sepPos = extraDepString.IndexOf(":");
					string reln = Sharpen.Runtime.Substring(extraDepString, sepPos + 1);
					string gov = Sharpen.Runtime.Substring(extraDepString, 0, sepPos);
					extraDeps[gov] = reln;
				}
			}
			return extraDeps;
		}

		/// <summary>
		/// Converts an extra dependencies hash map to a string to be used
		/// in a CoNLL-U file.
		/// </summary>
		/// <param name="extraDeps"/>
		/// <returns>The extra dependencies string.</returns>
		public static string ToExtraDepsString(Dictionary<string, string> extraDeps)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			if (extraDeps != null)
			{
				IList<string> sortedKeys = new List<string>(extraDeps.Keys);
				sortedKeys.Sort();
				foreach (string key in sortedKeys)
				{
					if (!first)
					{
						sb.Append("|");
					}
					else
					{
						first = false;
					}
					sb.Append(key).Append(":").Append(extraDeps[key]);
				}
			}
			/* Empty feature list. */
			if (first)
			{
				sb.Append("_");
			}
			return sb.ToString();
		}

		public class FeatureNameComparator : IComparator<string>
		{
			public virtual int Compare(string featureName1, string featureName2)
			{
				return string.CompareOrdinal(featureName1.ToLower(), featureName2.ToLower());
			}
		}
	}
}

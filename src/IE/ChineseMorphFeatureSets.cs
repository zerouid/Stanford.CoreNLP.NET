using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.IE
{
	/// <summary>A class for holding Chinese morphological features used for word segmentation and POS tagging.</summary>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseMorphFeatureSets
	{
		private const long serialVersionUID = -1055526945031459198L;

		private IIndex<string> featIndex = new HashIndex<string>();

		private IDictionary<string, ICollection<char>> singletonFeatures = Generics.NewHashMap();

		private IDictionary<string, Pair<ICollection<char>, ICollection<char>>> affixFeatures = Generics.NewHashMap();

		public virtual IDictionary<string, ICollection<char>> GetSingletonFeatures()
		{
			return singletonFeatures;
		}

		public virtual IDictionary<string, Pair<ICollection<char>, ICollection<char>>> GetAffixFeatures()
		{
			return affixFeatures;
		}

		public ChineseMorphFeatureSets(string featureDir)
		{
			try
			{
				File dir = new File(featureDir);
				File[] files = dir.ListFiles(null);
				foreach (File file in files)
				{
					GetFeatures(file);
				}
			}
			catch (IOException e)
			{
				throw new Exception("Error creating ChineseMaxentLexicon" + e);
			}
		}

		private enum FeatType
		{
			Prefix,
			Suffix,
			Singleton
		}

		/// <exception cref="System.IO.IOException"/>
		private void GetFeatures(File file)
		{
			BufferedReader @in = new BufferedReader(new InputStreamReader(new FileInputStream(file), "GB18030"));
			string filename = file.GetName();
			string singleFeatName = filename;
			if (singleFeatName.IndexOf('.') >= 0)
			{
				singleFeatName = Sharpen.Runtime.Substring(singleFeatName, 0, filename.LastIndexOf('.'));
			}
			ChineseMorphFeatureSets.FeatType featType = null;
			foreach (ChineseMorphFeatureSets.FeatType ft in ChineseMorphFeatureSets.FeatType.Values())
			{
				if (filename.Contains(ft.ToString().ToLower()))
				{
					featType = ft;
					singleFeatName = Sharpen.Runtime.Substring(singleFeatName, 0, filename.IndexOf(ft.ToString().ToLower()));
					if (singleFeatName.EndsWith("_"))
					{
						singleFeatName = Sharpen.Runtime.Substring(singleFeatName, 0, singleFeatName.LastIndexOf('_'));
					}
					break;
				}
			}
			featIndex.Add(singleFeatName);
			string singleFeatIndexString = int.ToString(featIndex.IndexOf(singleFeatName));
			ICollection<char> featureSet = Generics.NewHashSet();
			string line;
			Pattern typedDoubleFeatPattern = Pattern.Compile("([A-Za-z]+)\\s+(.)\\s+(.)\\s*");
			Pattern typedSingleFeatPattern = Pattern.Compile("([A-Za-z]+)\\s+(.)\\s*");
			Pattern singleFeatPattern = Pattern.Compile("(.)(?:\\s+[0-9]+)?\\s*");
			while ((line = @in.ReadLine()) != null)
			{
				if (line.Length == 0)
				{
					continue;
				}
				if (featType == null)
				{
					Matcher typedDoubleFeatMatcher = typedDoubleFeatPattern.Matcher(line);
					if (typedDoubleFeatMatcher.Matches())
					{
						string featName = typedDoubleFeatMatcher.Group(1);
						featIndex.Add(featName);
						string featIndexString = int.ToString(featIndex.IndexOf(featName));
						string prefixChar = typedDoubleFeatMatcher.Group(2);
						AddTypedFeature(featIndexString, prefixChar[0], true);
						string suffixChar = typedDoubleFeatMatcher.Group(3);
						AddTypedFeature(featIndexString, suffixChar[0], false);
						continue;
					}
				}
				Matcher typedSingleFeatMatcher = typedSingleFeatPattern.Matcher(line);
				if (typedSingleFeatMatcher.Matches())
				{
					string featName = typedSingleFeatMatcher.Group(1);
					featIndex.Add(featName);
					string featIndexString = int.ToString(featIndex.IndexOf(featName));
					string charString = typedSingleFeatMatcher.Group(2);
					switch (featType)
					{
						case ChineseMorphFeatureSets.FeatType.Prefix:
						{
							AddTypedFeature(featIndexString, charString[0], true);
							break;
						}

						case ChineseMorphFeatureSets.FeatType.Suffix:
						{
							AddTypedFeature(featIndexString, charString[0], false);
							break;
						}

						case ChineseMorphFeatureSets.FeatType.Singleton:
						{
							throw new Exception("ERROR: typed SINGLETON feature.");
						}
					}
					continue;
				}
				Matcher singleFeatMatcher = singleFeatPattern.Matcher(line);
				if (singleFeatMatcher.Matches())
				{
					string charString = singleFeatMatcher.Group();
					featureSet.Add(charString[0]);
					continue;
				}
				if (line.StartsWith("prefix") || line.StartsWith("suffix"))
				{
					if (featureSet.Count > 0)
					{
						Pair<ICollection<char>, ICollection<char>> p = affixFeatures[singleFeatIndexString];
						if (p == null)
						{
							affixFeatures[singleFeatIndexString] = p = new Pair<ICollection<char>, ICollection<char>>();
						}
						if (featType == ChineseMorphFeatureSets.FeatType.Prefix)
						{
							p.SetFirst(featureSet);
						}
						else
						{
							p.SetSecond(featureSet);
						}
						featureSet = Generics.NewHashSet();
					}
					featType = ChineseMorphFeatureSets.FeatType.Prefix;
					if (line.StartsWith("prefix"))
					{
						featType = ChineseMorphFeatureSets.FeatType.Prefix;
					}
					else
					{
						if (line.StartsWith("suffix"))
						{
							featType = ChineseMorphFeatureSets.FeatType.Suffix;
						}
					}
				}
			}
			if (featureSet.Count > 0)
			{
				if (featType == ChineseMorphFeatureSets.FeatType.Singleton)
				{
					singletonFeatures[singleFeatIndexString] = featureSet;
				}
				else
				{
					Pair<ICollection<char>, ICollection<char>> p = affixFeatures[singleFeatIndexString];
					if (p == null)
					{
						affixFeatures[singleFeatIndexString] = p = new Pair<ICollection<char>, ICollection<char>>();
					}
					if (featType == ChineseMorphFeatureSets.FeatType.Prefix)
					{
						p.SetFirst(featureSet);
					}
					else
					{
						p.SetSecond(featureSet);
					}
				}
			}
		}

		private void AddTypedFeature(string featName, char featChar, bool isPrefix)
		{
			Pair<ICollection<char>, ICollection<char>> p = affixFeatures[featName];
			if (p == null)
			{
				affixFeatures[featName] = p = new Pair<ICollection<char>, ICollection<char>>();
			}
			ICollection<char> feature;
			if (isPrefix)
			{
				feature = p.First();
				if (feature == null)
				{
					p.SetFirst(feature = Generics.NewHashSet());
				}
			}
			else
			{
				feature = p.Second();
				if (feature == null)
				{
					p.SetSecond(feature = Generics.NewHashSet());
				}
			}
			feature.Add(featChar);
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>Created by sonalg on 10/27/14.</summary>
	public class PatternFactory
	{
		/// <summary>allow to match stop words before a target term.</summary>
		/// <remarks>
		/// allow to match stop words before a target term. This is to match something
		/// like "I am on some X" if the pattern is "I am on X"
		/// </remarks>
		public static bool useStopWordsBeforeTerm = false;

		/// <summary>Add NER restriction to the target phrase in the patterns</summary>
		public static bool useTargetNERRestriction = false;

		public static bool useNER = true;

		/// <summary>Can just write a number (if same for all labels) or "Label1,2;Label2,3;...."</summary>
		public static string numWordsCompound = "2";

		public static IDictionary<string, int> numWordsCompoundMapped = new Dictionary<string, int>();

		public static int numWordsCompoundMax = 2;

		/// <summary>Use lemma instead of words for the context tokens</summary>
		public static bool useLemmaContextTokens = true;

		public static IList<string> fillerWords = Arrays.AsList("a", "an", "the", "`", "``", "'", "''");

		/// <summary>by default doesn't ignore anything.</summary>
		/// <remarks>by default doesn't ignore anything. What phrases to ignore.</remarks>
		public static Pattern ignoreWordRegex = Pattern.Compile("a^");

		public static void SetUp(Properties props, PatternFactory.PatternType patternType, ICollection<string> labels)
		{
			ArgumentParser.FillOptions(typeof(PatternFactory), props);
			numWordsCompoundMax = 0;
			if (numWordsCompound.Contains(",") || numWordsCompound.Contains(";"))
			{
				string[] toks = numWordsCompound.Split(";");
				foreach (string t in toks)
				{
					string[] toks2 = t.Split(",");
					int numWords = System.Convert.ToInt32(toks2[1]);
					numWordsCompoundMapped[toks2[0]] = numWords;
					if (numWords > numWordsCompoundMax)
					{
						numWordsCompoundMax = numWords;
					}
				}
			}
			else
			{
				numWordsCompoundMax = System.Convert.ToInt32(numWordsCompound);
				foreach (string label in labels)
				{
					numWordsCompoundMapped[label] = System.Convert.ToInt32(numWordsCompound);
				}
			}
			if (patternType.Equals(PatternFactory.PatternType.Surface))
			{
				SurfacePatternFactory.SetUp(props);
			}
			else
			{
				if (patternType.Equals(PatternFactory.PatternType.Dep))
				{
					DepPatternFactory.SetUp(props);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		public enum PatternType
		{
			Surface,
			Dep
		}

		public static bool DoNotUse(string word, ICollection<CandidatePhrase> stopWords)
		{
			return stopWords.Contains(CandidatePhrase.CreateOrGet(word.ToLower())) || ignoreWordRegex.Matcher(word).Matches();
		}

		public static IDictionary<int, ISet> GetPatternsAroundTokens(PatternFactory.PatternType patternType, DataInstance sent, ICollection<CandidatePhrase> stopWords)
		{
			if (patternType.Equals(PatternFactory.PatternType.Surface))
			{
				return SurfacePatternFactory.GetPatternsAroundTokens(sent, stopWords);
			}
			else
			{
				if (patternType.Equals(PatternFactory.PatternType.Dep))
				{
					return (IDictionary)DepPatternFactory.GetPatternsAroundTokens(sent, stopWords);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>An abstract class to represent a Pattern.</summary>
	/// <remarks>
	/// An abstract class to represent a Pattern. Currently only surface patterns are implemented.
	/// In future, dependency patterns shd be implemented too.
	/// </remarks>
	/// <Author>Sonal Gupta @sonalg.</Author>
	[System.Serializable]
	public abstract class Pattern
	{
		public PatternFactory.PatternType type;

		public Pattern(PatternFactory.PatternType type)
		{
			this.type = type;
		}

		public static bool SameGenre(PatternFactory.PatternType patternClass, Edu.Stanford.Nlp.Patterns.Pattern p1, Edu.Stanford.Nlp.Patterns.Pattern p2)
		{
			if (patternClass.Equals(PatternFactory.PatternType.Surface))
			{
				return SurfacePattern.SameGenre((SurfacePattern)p1, (SurfacePattern)p2);
			}
			else
			{
				if (patternClass.Equals(PatternFactory.PatternType.Dep))
				{
					return DepPattern.SameGenre((DepPattern)p1, (DepPattern)p2);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		public abstract CollectionValuedMap<string, string> GetRelevantWords();

		public static bool Subsumes(PatternFactory.PatternType patternClass, Edu.Stanford.Nlp.Patterns.Pattern pat, Edu.Stanford.Nlp.Patterns.Pattern p)
		{
			if (patternClass.Equals(PatternFactory.PatternType.Surface))
			{
				return SurfacePattern.Subsumes((SurfacePattern)pat, (SurfacePattern)p);
			}
			else
			{
				if (patternClass.Equals(PatternFactory.PatternType.Dep))
				{
					return DepPattern.Subsumes((DepPattern)pat, (DepPattern)p);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}

		public abstract int EqualContext(Edu.Stanford.Nlp.Patterns.Pattern p);

		public abstract string ToStringSimple();

		/// <summary>Get set of patterns around this token.</summary>
		public static ISet GetContext(PatternFactory.PatternType patternClass, DataInstance sent, int i, ICollection<CandidatePhrase> stopWords)
		{
			if (patternClass.Equals(PatternFactory.PatternType.Surface))
			{
				return SurfacePatternFactory.GetContext(sent.GetTokens(), i, stopWords);
			}
			else
			{
				return DepPatternFactory.GetContext(sent, i, stopWords);
			}
		}

		public abstract string ToString(IList<string> notAllowedClasses);

		protected internal static void GetRelevantWordsBase(Token[] t, CollectionValuedMap<string, string> relWords)
		{
			if (t != null)
			{
				foreach (Token s in t)
				{
					IDictionary<string, string> str = s.ClassORRestrictionsAsString();
					if (str != null)
					{
						relWords.AddAll(str);
					}
				}
			}
		}

		protected internal static void GetRelevantWordsBase(Token t, CollectionValuedMap<string, string> relWords)
		{
			if (t != null)
			{
				IDictionary<string, string> str = t.ClassORRestrictionsAsString();
				if (str != null)
				{
					relWords.AddAll(str);
				}
			}
		}
	}
}

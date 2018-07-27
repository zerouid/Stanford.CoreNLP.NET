using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Pattern for matching a CoreMap</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class CoreMapNodePattern : ComplexNodePattern<ICoreMap, Type>
	{
		private static IBiFunction<ICoreMap, Type, object> CreateGetter()
		{
			return new _IBiFunction_18();
		}

		private sealed class _IBiFunction_18 : IBiFunction<ICoreMap, Type, object>
		{
			public _IBiFunction_18()
			{
			}

			public object Apply(ICoreMap m, Type k)
			{
				return m.Get(k);
			}
		}

		public CoreMapNodePattern(IList<Pair<Type, NodePattern>> annotationPatterns)
			: base(CreateGetter(), annotationPatterns)
		{
		}

		public CoreMapNodePattern(params Pair<Type, NodePattern>[] annotationPatterns)
			: base(CreateGetter(), annotationPatterns)
		{
		}

		public CoreMapNodePattern(Type key, NodePattern pattern)
			: this(Pair.MakePair(key, pattern))
		{
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern ValueOf(string textAnnotationPattern)
		{
			return ValueOf(null, textAnnotationPattern);
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern ValueOf(string textAnnotationPattern, int flags)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern p = new Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern(new List<Pair<Type, NodePattern>>(1));
			p.Add(typeof(CoreAnnotations.TextAnnotation), NewStringRegexPattern(textAnnotationPattern, flags));
			return p;
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern ValueOf(Env env, string textAnnotationPattern)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern p = new Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern(new List<Pair<Type, NodePattern>>(1));
			p.Add(typeof(CoreAnnotations.TextAnnotation), NewStringRegexPattern(textAnnotationPattern, (env != null) ? env.defaultStringPatternFlags : 0));
			return p;
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern ValueOf(Pattern textAnnotationPattern)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern p = new Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern(new List<Pair<Type, NodePattern>>(1));
			p.Add(typeof(CoreAnnotations.TextAnnotation), new ComplexNodePattern.StringAnnotationRegexPattern(textAnnotationPattern));
			return p;
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern ValueOf(IDictionary<string, string> attributes)
		{
			return ValueOf(null, attributes);
		}

		public static Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern ValueOf(Env env, IDictionary<string, string> attributes)
		{
			Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern p = new Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapNodePattern(new List<Pair<Type, NodePattern>>(attributes.Count));
			p.Populate(env, attributes, null);
			return p;
		}

		public class AttributesEqualMatchChecker<K> : SequencePattern.INodesMatchChecker<ICoreMap>
		{
			internal ICollection<Type> keys;

			public AttributesEqualMatchChecker(params Type[] keys)
			{
				this.keys = CollectionUtils.AsSet(keys);
			}

			public virtual bool Matches(ICoreMap o1, ICoreMap o2)
			{
				foreach (Type key in keys)
				{
					object v1 = o1.Get(key);
					object v2 = o2.Get(key);
					if (v1 != null)
					{
						if (!v1.Equals(v2))
						{
							return false;
						}
					}
					else
					{
						if (v2 != null)
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		public static readonly CoreMapNodePattern.AttributesEqualMatchChecker TextAttrEqualChecker = new CoreMapNodePattern.AttributesEqualMatchChecker(typeof(CoreAnnotations.TextAnnotation));
	}
}

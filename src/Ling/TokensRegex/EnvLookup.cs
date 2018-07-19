using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Pipeline;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Provides lookup functions using an Env</summary>
	/// <author>Angel Chang</author>
	public class EnvLookup
	{
		private EnvLookup()
		{
		}

		// static methods
		// TODO: For additional keys, read map of name to Class from file???
		public static Type LookupAnnotationKey(Env env, string name)
		{
			if (env != null)
			{
				object obj = env.Get(name);
				if (obj != null)
				{
					if (obj is Type)
					{
						return (Type)obj;
					}
					else
					{
						if (obj is IValue)
						{
							obj = ((IValue)obj).Get();
							if (obj is Type)
							{
								return (Type)obj;
							}
						}
					}
				}
			}
			return AnnotationLookup.ToCoreKey(name);
		}

		public static Type LookupAnnotationKeyWithClassname(Env env, string name)
		{
			Type annotationKey = LookupAnnotationKey(env, name);
			if (annotationKey == null)
			{
				try
				{
					Type clazz = Sharpen.Runtime.GetType(name);
					return clazz;
				}
				catch (TypeLoadException)
				{
				}
				return null;
			}
			else
			{
				return annotationKey;
			}
		}

		public static IDictionary<Type, CoreMapAttributeAggregator> GetDefaultTokensAggregators(Env env)
		{
			if (env != null)
			{
				IDictionary<Type, CoreMapAttributeAggregator> obj = env.GetDefaultTokensAggregators();
				if (obj != null)
				{
					return obj;
				}
			}
			return CoreMapAttributeAggregator.DefaultNumericTokensAggregators;
		}

		public static CoreMapAggregator GetDefaultTokensAggregator(Env env)
		{
			if (env != null)
			{
				CoreMapAggregator obj = env.GetDefaultTokensAggregator();
				if (obj != null)
				{
					return obj;
				}
			}
			return CoreMapAggregator.DefaultNumericTokensAggregator;
		}

		public static IList<Type> GetDefaultTokensResultAnnotationKey(Env env)
		{
			if (env != null)
			{
				IList<Type> obj = env.GetDefaultTokensResultAnnotationKey();
				if (obj != null)
				{
					return obj;
				}
			}
			return null;
		}

		public static IList<Type> GetDefaultResultAnnotationKey(Env env)
		{
			if (env != null)
			{
				IList<Type> obj = env.GetDefaultResultAnnotationKey();
				if (obj != null)
				{
					return obj;
				}
			}
			return null;
		}

		public static IFunction<MatchedExpression, object> GetDefaultResultAnnotationExtractor(Env env)
		{
			if (env != null)
			{
				IFunction<MatchedExpression, object> obj = env.GetDefaultResultsAnnotationExtractor();
				if (obj != null)
				{
					return obj;
				}
			}
			return null;
		}

		public static Type GetDefaultNestedResultsAnnotationKey(Env env)
		{
			if (env != null)
			{
				Type obj = env.GetDefaultNestedResultsAnnotationKey();
				if (obj != null)
				{
					return obj;
				}
			}
			return null;
		}

		public static Type GetDefaultTextAnnotationKey(Env env)
		{
			if (env != null)
			{
				Type obj = env.GetDefaultTextAnnotationKey();
				if (obj != null)
				{
					return obj;
				}
			}
			return typeof(CoreAnnotations.TextAnnotation);
		}

		public static Type GetDefaultTokensAnnotationKey(Env env)
		{
			if (env != null)
			{
				Type obj = env.GetDefaultTokensAnnotationKey();
				if (obj != null)
				{
					return obj;
				}
			}
			return typeof(CoreAnnotations.TokensAnnotation);
		}
	}
}

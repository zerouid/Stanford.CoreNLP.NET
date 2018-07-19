using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Functions for aggregating token attributes.</summary>
	/// <author>Angel Chang</author>
	public abstract class CoreMapAttributeAggregator
	{
		public static IDictionary<Type, CoreMapAttributeAggregator> GetDefaultAggregators()
		{
			return DefaultAggregators;
		}

		public static CoreMapAttributeAggregator GetAggregator(string str)
		{
			return AggregatorLookup[str];
		}

		public abstract object Aggregate<_T0>(Type key, IList<_T0> @in)
			where _T0 : ICoreMap;

		private sealed class _CoreMapAttributeAggregator_31 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_31()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null)
					{
						return obj;
					}
				}
				return null;
			}
		}

		public static readonly CoreMapAttributeAggregator FirstNonNil = new _CoreMapAttributeAggregator_31();

		private sealed class _CoreMapAttributeAggregator_44 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_44()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					return obj;
				}
				return null;
			}
		}

		public static readonly CoreMapAttributeAggregator First = new _CoreMapAttributeAggregator_44();

		private sealed class _CoreMapAttributeAggregator_55 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_55()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				for (int i = @in.Count - 1; i >= 0; i--)
				{
					ICoreMap cm = @in[i];
					object obj = cm.Get(key);
					if (obj != null)
					{
						return obj;
					}
				}
				return null;
			}
		}

		public static readonly CoreMapAttributeAggregator LastNonNil = new _CoreMapAttributeAggregator_55();

		private sealed class _CoreMapAttributeAggregator_69 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_69()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				for (int i = @in.Count - 1; i >= 0; i--)
				{
					ICoreMap cm = @in[i];
					return cm.Get(key);
				}
				return null;
			}
		}

		public static readonly CoreMapAttributeAggregator Last = new _CoreMapAttributeAggregator_69();

		public sealed class ConcatListAggregator<T> : CoreMapAttributeAggregator
		{
			public ConcatListAggregator()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				IList<T> res = new List<T>();
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null)
					{
						if (obj is IList)
						{
							Sharpen.Collections.AddAll(res, (IList<T>)obj);
						}
					}
				}
				return res;
			}
		}

		public sealed class ConcatCoreMapListAggregator<T> : CoreMapAttributeAggregator
			where T : ICoreMap
		{
			internal bool concatSelf = false;

			public ConcatCoreMapListAggregator()
			{
			}

			public ConcatCoreMapListAggregator(bool concatSelf)
			{
				this.concatSelf = concatSelf;
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				IList<T> res = new List<T>();
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					bool added = false;
					if (obj != null)
					{
						if (obj is IList)
						{
							Sharpen.Collections.AddAll(res, (IList<T>)obj);
							added = true;
						}
					}
					if (!added && concatSelf)
					{
						res.Add((T)cm);
					}
				}
				return res;
			}
		}

		public static readonly CoreMapAttributeAggregator.ConcatCoreMapListAggregator<CoreLabel> ConcatTokens = new CoreMapAttributeAggregator.ConcatCoreMapListAggregator<CoreLabel>(true);

		public static readonly CoreMapAttributeAggregator.ConcatCoreMapListAggregator<ICoreMap> ConcatCoremap = new CoreMapAttributeAggregator.ConcatCoreMapListAggregator<ICoreMap>(true);

		public sealed class ConcatAggregator : CoreMapAttributeAggregator
		{
			internal string delimiter;

			public ConcatAggregator(string delimiter)
			{
				this.delimiter = delimiter;
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				StringBuilder sb = new StringBuilder();
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null)
					{
						if (sb.Length > 0)
						{
							sb.Append(delimiter);
						}
						sb.Append(obj);
					}
				}
				return sb.ToString();
			}
		}

		public sealed class ConcatTextAggregator : CoreMapAttributeAggregator
		{
			internal string delimiter;

			public ConcatTextAggregator(string delimiter)
			{
				this.delimiter = delimiter;
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				string text = ChunkAnnotationUtils.GetTokenText(@in, key);
				return text;
			}
		}

		public static readonly CoreMapAttributeAggregator Concat = new CoreMapAttributeAggregator.ConcatAggregator(" ");

		public static readonly CoreMapAttributeAggregator ConcatText = new CoreMapAttributeAggregator.ConcatTextAggregator(" ");

		private sealed class _CoreMapAttributeAggregator_165 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_165()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				return @in.Count;
			}
		}

		public static readonly CoreMapAttributeAggregator Count = new _CoreMapAttributeAggregator_165();

		private sealed class _CoreMapAttributeAggregator_170 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_170()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				double sum = 0;
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null)
					{
						if (obj is Number)
						{
							sum += ((Number)obj);
						}
						else
						{
							if (obj is string)
							{
								sum += double.ParseDouble((string)obj);
							}
							else
							{
								throw new Exception("Cannot sum attribute " + key + ", object of type: " + obj.GetType());
							}
						}
					}
				}
				return sum;
			}
		}

		public static readonly CoreMapAttributeAggregator Sum = new _CoreMapAttributeAggregator_170();

		private sealed class _CoreMapAttributeAggregator_189 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_189()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				IComparable min = null;
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null)
					{
						if (obj is IComparable)
						{
							IComparable c = (IComparable)obj;
							if (min == null)
							{
								min = c;
							}
							else
							{
								if (c.CompareTo(min) < 0)
								{
									min = c;
								}
							}
						}
						else
						{
							throw new Exception("Cannot get min of attribute " + key + ", object of type: " + obj.GetType());
						}
					}
				}
				return min;
			}
		}

		public static readonly CoreMapAttributeAggregator Min = new _CoreMapAttributeAggregator_189();

		private sealed class _CoreMapAttributeAggregator_211 : CoreMapAttributeAggregator
		{
			public _CoreMapAttributeAggregator_211()
			{
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				IComparable max = null;
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null)
					{
						if (obj is IComparable)
						{
							IComparable c = (IComparable)obj;
							if (max == null)
							{
								max = c;
							}
							else
							{
								if (c.CompareTo(max) > 0)
								{
									max = c;
								}
							}
						}
						else
						{
							throw new Exception("Cannot get max of attribute " + key + ", object of type: " + obj.GetType());
						}
					}
				}
				return max;
			}
		}

		public static readonly CoreMapAttributeAggregator Max = new _CoreMapAttributeAggregator_211();

		public sealed class MostFreqAggregator : CoreMapAttributeAggregator
		{
			internal ICollection<object> ignoreSet;

			public MostFreqAggregator()
			{
			}

			public MostFreqAggregator(ICollection<object> set)
			{
				ignoreSet = set;
			}

			public override object Aggregate<_T0>(Type key, IList<_T0> @in)
			{
				if (@in == null)
				{
					return null;
				}
				IntCounter<object> counter = new IntCounter<object>();
				foreach (ICoreMap cm in @in)
				{
					object obj = cm.Get(key);
					if (obj != null && (ignoreSet == null || !ignoreSet.Contains(obj)))
					{
						counter.IncrementCount(obj);
					}
				}
				if (counter.Size() > 0)
				{
					return counter.Argmax();
				}
				else
				{
					return null;
				}
			}
		}

		public static readonly CoreMapAttributeAggregator MostFreq = new CoreMapAttributeAggregator.MostFreqAggregator();

		private static readonly IDictionary<string, CoreMapAttributeAggregator> AggregatorLookup = Generics.NewHashMap();

		static CoreMapAttributeAggregator()
		{
			AggregatorLookup["FIRST"] = First;
			AggregatorLookup["FIRST_NON_NIL"] = FirstNonNil;
			AggregatorLookup["LAST"] = Last;
			AggregatorLookup["LAST_NON_NIL"] = LastNonNil;
			AggregatorLookup["MIN"] = Min;
			AggregatorLookup["MAX"] = Max;
			AggregatorLookup["COUNT"] = Count;
			AggregatorLookup["SUM"] = Sum;
			AggregatorLookup["CONCAT"] = Concat;
			AggregatorLookup["CONCAT_TEXT"] = ConcatText;
			AggregatorLookup["CONCAT_TOKENS"] = ConcatTokens;
			AggregatorLookup["MOST_FREQ"] = MostFreq;
		}

		public static readonly IDictionary<Type, CoreMapAttributeAggregator> DefaultAggregators;

		public static readonly IDictionary<Type, CoreMapAttributeAggregator> DefaultNumericAggregators;

		public static readonly IDictionary<Type, CoreMapAttributeAggregator> DefaultNumericTokensAggregators;

		static CoreMapAttributeAggregator()
		{
			IDictionary<Type, CoreMapAttributeAggregator> defaultAggr = new ArrayMap<Type, CoreMapAttributeAggregator>();
			defaultAggr[typeof(CoreAnnotations.TextAnnotation)] = CoreMapAttributeAggregator.ConcatText;
			defaultAggr[typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)] = CoreMapAttributeAggregator.First;
			defaultAggr[typeof(CoreAnnotations.CharacterOffsetEndAnnotation)] = CoreMapAttributeAggregator.Last;
			defaultAggr[typeof(CoreAnnotations.TokenBeginAnnotation)] = CoreMapAttributeAggregator.First;
			defaultAggr[typeof(CoreAnnotations.TokenEndAnnotation)] = CoreMapAttributeAggregator.Last;
			defaultAggr[typeof(CoreAnnotations.TokensAnnotation)] = CoreMapAttributeAggregator.ConcatTokens;
			defaultAggr[typeof(CoreAnnotations.BeforeAnnotation)] = CoreMapAttributeAggregator.First;
			defaultAggr[typeof(CoreAnnotations.AfterAnnotation)] = CoreMapAttributeAggregator.Last;
			DefaultAggregators = Java.Util.Collections.UnmodifiableMap(defaultAggr);
			IDictionary<Type, CoreMapAttributeAggregator> defaultNumericAggr = new ArrayMap<Type, CoreMapAttributeAggregator>(DefaultAggregators);
			defaultNumericAggr[typeof(CoreAnnotations.NumericCompositeTypeAnnotation)] = CoreMapAttributeAggregator.FirstNonNil;
			defaultNumericAggr[typeof(CoreAnnotations.NumericCompositeValueAnnotation)] = CoreMapAttributeAggregator.FirstNonNil;
			defaultNumericAggr[typeof(CoreAnnotations.NamedEntityTagAnnotation)] = CoreMapAttributeAggregator.FirstNonNil;
			defaultNumericAggr[typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation)] = CoreMapAttributeAggregator.FirstNonNil;
			DefaultNumericAggregators = Java.Util.Collections.UnmodifiableMap(defaultNumericAggr);
			IDictionary<Type, CoreMapAttributeAggregator> defaultNumericTokensAggr = new ArrayMap<Type, CoreMapAttributeAggregator>(DefaultNumericAggregators);
			defaultNumericTokensAggr[typeof(CoreAnnotations.NumerizedTokensAnnotation)] = CoreMapAttributeAggregator.ConcatCoremap;
			DefaultNumericTokensAggregators = Java.Util.Collections.UnmodifiableMap(defaultNumericTokensAggr);
		}
	}
}

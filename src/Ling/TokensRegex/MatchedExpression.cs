using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Matched Expression represents a chunk of text that was matched from an original segment of text.</summary>
	/// <author>Angel Chang</author>
	public class MatchedExpression
	{
		/// <summary>Text representing the matched expression</summary>
		protected internal string text;

		/// <summary>Character offsets (relative to original text).</summary>
		/// <remarks>
		/// Character offsets (relative to original text).
		/// TODO: Fix up
		/// If matched using regular text patterns,
		/// the character offsets are with respect to the annotation (usually sentence)
		/// from which the text was matched against
		/// If matched using tokens, the character offsets are with respect to the overall document
		/// </remarks>
		protected internal Interval<int> charOffsets;

		/// <summary>Token offsets (relative to original text tokenization)</summary>
		protected internal Interval<int> tokenOffsets;

		/// <summary>Chunk offsets (relative to chunking on top of original text)</summary>
		protected internal Interval<int> chunkOffsets;

		protected internal ICoreMap annotation;

		/// <summary>Function indicating how to extract an value from annotation built from this expression</summary>
		protected internal object context;

		protected internal MatchedExpression.SingleAnnotationExtractor extractFunc;

		public IValue value;

		internal double priority;

		internal double weight;

		internal int order;

		/// <summary>Function that takes a CoreMap, applies an extraction function to it, to get a value.</summary>
		/// <remarks>
		/// Function that takes a CoreMap, applies an extraction function to it, to get a value.
		/// Also contains information on how to construct a final annotation.
		/// </remarks>
		public class SingleAnnotationExtractor : IFunction<ICoreMap, IValue>
		{
			public string name;

			public double priority;

			public double weight;

			public Type tokensAnnotationField = typeof(CoreAnnotations.TokensAnnotation);

			public IList<Type> tokensResultAnnotationField;

			public IList<Type> resultAnnotationField;

			public Type resultNestedAnnotationField;

			public bool includeNested = false;

			public IFunction<ICoreMap, IValue> valueExtractor;

			public IFunction<MatchedExpression, IValue> expressionToValue;

			public IFunction<MatchedExpression, object> resultAnnotationExtractor;

			public CoreMapAggregator tokensAggregator;

			// TODO: Should we keep some context from the source so we can perform more complex evaluation?
			// Some context to help to extract value from annotation
			//protected Map<String,String> attributes;
			// Used to disambiguate matched expressions
			// Priority/Order in which this rule should be applied with respect to others
			// Weight given to the rule (how likely is this rule to fire)
			//    public Class annotationField;  // Annotation field to apply rule over: text or tokens or numerizedtokens
			// Tokens or numerizedtokens
			// Annotation field to put new annotation
			// Annotation field for child/nested annotations
			public virtual IValue Apply(ICoreMap @in)
			{
				return valueExtractor.Apply(@in);
			}

			private static void SetAnnotations(ICoreMap cm, IList<Type> annotationKeys, object obj)
			{
				if (annotationKeys.Count > 1 && obj is IList)
				{
					// List of annotationKeys, obj also list, we should try to match the objects to annotationKeys
					IList list = (IList)obj;
					int n = Math.Min(list.Count, annotationKeys.Count);
					for (int i = 0; i < n; i++)
					{
						object v = list[i];
						Type key = annotationKeys[i];
						if (key == null)
						{
							throw new Exception("Invalid null annotation key");
						}
						if (v is IValue)
						{
							cm.Set(key, ((IValue)v).Get());
						}
						else
						{
							cm.Set(key, v);
						}
					}
				}
				else
				{
					// Only a single object, set all annotationKeys to that obj
					foreach (Type key in annotationKeys)
					{
						if (key == null)
						{
							throw new Exception("Invalid null annotation key");
						}
						cm.Set(key, obj);
					}
				}
			}

			public virtual void Annotate<_T0>(MatchedExpression matchedExpression, IList<_T0> nested)
				where _T0 : ICoreMap
			{
				if (resultNestedAnnotationField != null)
				{
					matchedExpression.annotation.Set(resultNestedAnnotationField, nested);
				}
				// NOTE: for now value must be extracted after nested annotation is in place...
				Annotate(matchedExpression);
			}

			public virtual void Annotate(MatchedExpression matchedExpression)
			{
				IValue ev = null;
				if (expressionToValue != null)
				{
					ev = expressionToValue.Apply(matchedExpression);
				}
				matchedExpression.value = (ev != null) ? ev : valueExtractor.Apply(matchedExpression.annotation);
				if (resultAnnotationField != null)
				{
					if (resultAnnotationExtractor != null)
					{
						object result = resultAnnotationExtractor.Apply(matchedExpression);
						SetAnnotations(matchedExpression.annotation, resultAnnotationField, result);
					}
					else
					{
						// TODO: Should default result be the matchedExpression, value, object???
						//matchedExpression.annotation.set(resultAnnotationField, matchedExpression);
						IValue v = matchedExpression.GetValue();
						SetAnnotations(matchedExpression.annotation, resultAnnotationField, (v != null) ? v.Get() : null);
					}
				}
				if (tokensResultAnnotationField != null)
				{
					IList<ICoreMap> tokens = (IList<ICoreMap>)matchedExpression.annotation.Get(tokensAnnotationField);
					if (resultAnnotationExtractor != null)
					{
						object result = resultAnnotationExtractor.Apply(matchedExpression);
						foreach (ICoreMap cm in tokens)
						{
							SetAnnotations(cm, tokensResultAnnotationField, result);
						}
					}
					else
					{
						// TODO: Should default result be the matchedExpression, value, object???
						//matchedExpression.annotation.set(resultAnnotationField, matchedExpression);
						IValue v = matchedExpression.GetValue();
						foreach (ICoreMap cm in tokens)
						{
							SetAnnotations(cm, tokensResultAnnotationField, (v != null) ? v.Get() : null);
						}
					}
				}
			}

			public virtual MatchedExpression CreateMatchedExpression(Interval<int> charOffsets, Interval<int> tokenOffsets)
			{
				return new MatchedExpression(charOffsets, tokenOffsets, this, priority, weight);
			}
		}

		public MatchedExpression(MatchedExpression me)
		{
			// end static class SingleAnnotationExtractor
			this.annotation = me.annotation;
			this.extractFunc = me.extractFunc;
			this.text = me.text;
			this.value = me.value;
			//this.attributes = me.attributes;
			this.priority = me.priority;
			this.weight = me.weight;
			this.order = me.order;
			this.charOffsets = me.charOffsets;
			this.tokenOffsets = me.tokenOffsets;
			this.chunkOffsets = me.tokenOffsets;
		}

		public MatchedExpression(Interval<int> charOffsets, Interval<int> tokenOffsets, MatchedExpression.SingleAnnotationExtractor extractFunc, double priority, double weight)
		{
			this.charOffsets = charOffsets;
			this.tokenOffsets = tokenOffsets;
			this.chunkOffsets = tokenOffsets;
			this.extractFunc = extractFunc;
			this.priority = priority;
			this.weight = weight;
		}

		public virtual bool ExtractAnnotation(Env env, ICoreMap sourceAnnotation)
		{
			return ExtractAnnotation(sourceAnnotation, extractFunc.tokensAggregator);
		}

		private bool ExtractAnnotation(ICoreMap sourceAnnotation, CoreMapAggregator aggregator)
		{
			Type tokensAnnotationKey = extractFunc.tokensAnnotationField;
			if (chunkOffsets != null)
			{
				annotation = aggregator.Merge((IList<ICoreMap>)sourceAnnotation.Get(tokensAnnotationKey), chunkOffsets.GetBegin(), chunkOffsets.GetEnd());
				if (sourceAnnotation.ContainsKey(typeof(CoreAnnotations.TextAnnotation)))
				{
					ChunkAnnotationUtils.AnnotateChunkText(annotation, sourceAnnotation);
				}
				if (tokenOffsets != null)
				{
					if (annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) == null)
					{
						annotation.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenOffsets.GetBegin());
					}
					if (annotation.Get(typeof(CoreAnnotations.TokenEndAnnotation)) == null)
					{
						annotation.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenOffsets.GetEnd());
					}
				}
				charOffsets = Interval.ToInterval(annotation.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), annotation.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
				tokenOffsets = Interval.ToInterval(annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), annotation.Get(typeof(CoreAnnotations.TokenEndAnnotation)), Interval.IntervalOpenEnd);
			}
			else
			{
				int baseCharOffset = sourceAnnotation.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				if (baseCharOffset == null)
				{
					baseCharOffset = 0;
				}
				chunkOffsets = ChunkAnnotationUtils.GetChunkOffsetsUsingCharOffsets((IList<ICoreMap>)sourceAnnotation.Get(tokensAnnotationKey), charOffsets.GetBegin() + baseCharOffset, charOffsets.GetEnd() + baseCharOffset);
				ICoreMap annotation2 = aggregator.Merge((IList<ICoreMap>)sourceAnnotation.Get(tokensAnnotationKey), chunkOffsets.GetBegin(), chunkOffsets.GetEnd());
				annotation = ChunkAnnotationUtils.GetAnnotatedChunkUsingCharOffsets(sourceAnnotation, charOffsets.GetBegin(), charOffsets.GetEnd());
				tokenOffsets = Interval.ToInterval(annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), annotation.Get(typeof(CoreAnnotations.TokenEndAnnotation)), Interval.IntervalOpenEnd);
				annotation.Set(tokensAnnotationKey, annotation2.Get(tokensAnnotationKey));
			}
			text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			extractFunc.Annotate(this, (IList<ICoreMap>)annotation.Get(tokensAnnotationKey));
			return true;
		}

		public virtual bool ExtractAnnotation<_T0>(Env env, IList<_T0> source)
			where _T0 : ICoreMap
		{
			return ExtractAnnotation(source, CoreMapAggregator.GetDefaultAggregator());
		}

		protected internal virtual bool ExtractAnnotation<_T0>(IList<_T0> source, CoreMapAggregator aggregator)
			where _T0 : ICoreMap
		{
			annotation = aggregator.Merge(source, chunkOffsets.GetBegin(), chunkOffsets.GetEnd());
			charOffsets = Interval.ToInterval(annotation.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), annotation.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)), Interval.IntervalOpenEnd);
			tokenOffsets = Interval.ToInterval(annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), annotation.Get(typeof(CoreAnnotations.TokenEndAnnotation)), Interval.IntervalOpenEnd);
			text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			extractFunc.Annotate(this, source.SubList(chunkOffsets.GetBegin(), chunkOffsets.GetEnd()));
			return true;
		}

		public virtual Interval<int> GetCharOffsets()
		{
			return charOffsets;
		}

		public virtual Interval<int> GetTokenOffsets()
		{
			return tokenOffsets;
		}

		public virtual Interval<int> GetChunkOffsets()
		{
			return chunkOffsets;
		}

		/* public Map<String, String> getAttributes() {
		return attributes;
		}*/
		public virtual double GetPriority()
		{
			return priority;
		}

		public virtual double GetWeight()
		{
			return weight;
		}

		public virtual int GetOrder()
		{
			return order;
		}

		public virtual bool IsIncludeNested()
		{
			return extractFunc.includeNested;
		}

		public virtual void SetIncludeNested(bool includeNested)
		{
			extractFunc.includeNested = includeNested;
		}

		public virtual string GetText()
		{
			return text;
		}

		public virtual ICoreMap GetAnnotation()
		{
			return annotation;
		}

		public virtual IValue GetValue()
		{
			return value;
		}

		public override string ToString()
		{
			return text;
		}

		public static IList<ICoreMap> ReplaceMerged<_T0, _T1>(IList<_T0> list, IList<_T1> matchedExprs)
			where _T0 : ICoreMap
			where _T1 : MatchedExpression
		{
			if (matchedExprs == null)
			{
				return list;
			}
			matchedExprs.Sort(ExprTokenOffsetComparator);
			IList<ICoreMap> merged = new List<ICoreMap>(list.Count);
			// Approximate size
			int last = 0;
			foreach (MatchedExpression expr in matchedExprs)
			{
				int start = expr.chunkOffsets.First();
				int end = expr.chunkOffsets.Second();
				if (start >= last)
				{
					Sharpen.Collections.AddAll(merged, list.SubList(last, start));
					ICoreMap m = expr.GetAnnotation();
					merged.Add(m);
					last = end;
				}
			}
			// Add rest of elements
			if (last < list.Count)
			{
				Sharpen.Collections.AddAll(merged, list.SubList(last, list.Count));
			}
			return merged;
		}

		public static IList<ICoreMap> ReplaceMergedUsingTokenOffsets<_T0, _T1>(IList<_T0> list, IList<_T1> matchedExprs)
			where _T0 : ICoreMap
			where _T1 : MatchedExpression
		{
			if (matchedExprs == null)
			{
				return list;
			}
			IDictionary<int, int> tokenBeginToListIndexMap = new Dictionary<int, int>();
			//Generics.newHashMap();
			IDictionary<int, int> tokenEndToListIndexMap = new Dictionary<int, int>();
			//Generics.newHashMap();
			for (int i = 0; i < list.Count; i++)
			{
				ICoreMap cm = list[i];
				if (cm.ContainsKey(typeof(CoreAnnotations.TokenBeginAnnotation)) && cm.ContainsKey(typeof(CoreAnnotations.TokenEndAnnotation)))
				{
					tokenBeginToListIndexMap[cm.Get(typeof(CoreAnnotations.TokenBeginAnnotation))] = i;
					tokenEndToListIndexMap[cm.Get(typeof(CoreAnnotations.TokenEndAnnotation))] = i + 1;
				}
				else
				{
					tokenBeginToListIndexMap[i] = i;
					tokenEndToListIndexMap[i + 1] = i + 1;
				}
			}
			matchedExprs.Sort(ExprTokenOffsetComparator);
			IList<ICoreMap> merged = new List<ICoreMap>(list.Count);
			// Approximate size
			int last = 0;
			foreach (MatchedExpression expr in matchedExprs)
			{
				int start = expr.tokenOffsets.First();
				int end = expr.tokenOffsets.Second();
				int istart = tokenBeginToListIndexMap[start];
				int iend = tokenEndToListIndexMap[end];
				if (istart != null && iend != null)
				{
					if (istart >= last)
					{
						Sharpen.Collections.AddAll(merged, list.SubList(last, istart));
						ICoreMap m = expr.GetAnnotation();
						merged.Add(m);
						last = iend;
					}
				}
			}
			// Add rest of elements
			if (last < list.Count)
			{
				Sharpen.Collections.AddAll(merged, list.SubList(last, list.Count));
			}
			return merged;
		}

		public static IList<T> RemoveNullValues<T>(IList<T> chunks)
			where T : MatchedExpression
		{
			IList<T> okayChunks = new List<T>(chunks.Count);
			foreach (T chunk in chunks)
			{
				IValue v = chunk.value;
				if (v == null || v.Get() == null)
				{
				}
				else
				{
					//skip
					okayChunks.Add(chunk);
				}
			}
			return okayChunks;
		}

		public static IList<T> RemoveNested<T>(IList<T> chunks)
			where T : MatchedExpression
		{
			if (chunks.Count > 1)
			{
				for (int i = 0; i < sz; i++)
				{
					chunks[i].order = i;
				}
				return IntervalTree.GetNonNested(chunks, ExprToTokenOffsetsIntervalFunc, ExprLengthPriorityComparator);
			}
			else
			{
				return chunks;
			}
		}

		public static IList<T> RemoveOverlapping<T>(IList<T> chunks)
			where T : MatchedExpression
		{
			if (chunks.Count > 1)
			{
				for (int i = 0; i < sz; i++)
				{
					chunks[i].order = i;
				}
				return IntervalTree.GetNonOverlapping(chunks, ExprToTokenOffsetsIntervalFunc, ExprPriorityLengthComparator);
			}
			else
			{
				return chunks;
			}
		}

		public static T GetBestMatched<T>(IList<T> matches, IToDoubleFunction<MatchedExpression> scorer)
			where T : MatchedExpression
		{
			if (matches == null || matches.IsEmpty())
			{
				return null;
			}
			T best = null;
			double bestScore = double.NegativeInfinity;
			foreach (T m in matches)
			{
				double s = scorer.ApplyAsDouble(m);
				if (best == null || s > bestScore)
				{
					best = m;
					bestScore = s;
				}
			}
			return best;
		}

		public static readonly IFunction<ICoreMap, Interval<int>> CoremapToTokenOffsetsIntervalFunc = null;

		public static readonly IFunction<ICoreMap, Interval<int>> CoremapToCharOffsetsIntervalFunc = null;

		public static readonly IFunction<MatchedExpression, Interval<int>> ExprToTokenOffsetsIntervalFunc = null;

		public static readonly IComparator<MatchedExpression> ExprPriorityComparator = null;

		public static readonly IComparator<MatchedExpression> ExprOrderComparator = null;

		public static readonly IComparator<MatchedExpression> ExprLengthComparator = null;

		public static readonly IComparator<MatchedExpression> ExprTokenOffsetComparator = null;

		public static readonly IComparator<MatchedExpression> ExprTokenOffsetsNestedFirstComparator = null;

		public static readonly IComparator<MatchedExpression> ExprPriorityLengthComparator = Comparators.Chain(ExprPriorityComparator, ExprLengthComparator, ExprOrderComparator, ExprTokenOffsetComparator);

		public static readonly IComparator<MatchedExpression> ExprLengthPriorityComparator = Comparators.Chain(ExprLengthComparator, ExprPriorityComparator, ExprOrderComparator, ExprTokenOffsetComparator);

		public static readonly IToDoubleFunction<MatchedExpression> ExprWeightScorer = null;
		// Compares two matched expressions.
		// Use to order matched expressions by:
		//    length (longest first), then whether it has value or not (has value first),
		// Returns -1 if e1 is longer than e2, 1 if e2 is longer
		// If e1 and e2 are the same length:
		//    Returns -1 if e1 has value, but e2 doesn't (1 if e2 has value, but e1 doesn't)
		//    Otherwise, both e1 and e2 has value or no value
		// Compares two matched expressions.
		// Use to order matched expressions by:
		//   score
		//    length (longest first), then whether it has value or not (has value first),
		//    original order
		//    and then beginning token offset (smaller offset first)
	}
}

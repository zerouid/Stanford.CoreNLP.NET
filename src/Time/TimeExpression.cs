using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Time Expression.</summary>
	/// <author>Angel Chang</author>
	public class TimeExpression : MatchedExpression
	{
		/// <summary>The CoreMap key for storing a SUTime.TimeIndex (for looking up Timex Id).</summary>
		public class TimeIndexAnnotation : ICoreAnnotation<SUTime.TimeIndex>
		{
			public virtual Type GetType()
			{
				return typeof(SUTime.TimeIndex);
			}
		}

		/// <summary>The CoreMap key for storing a TimeExpression annotation.</summary>
		public class Annotation : ICoreAnnotation<TimeExpression>
		{
			// todo [cdm 2016]: Rename this class!
			public virtual Type GetType()
			{
				return typeof(TimeExpression);
			}
		}

		/// <summary>The CoreMap key for storing a nested annotations.</summary>
		public class ChildrenAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}

		internal SUTime.Temporal origTemporal;

		public TimeExpression(MatchedExpression expr)
			: base(expr)
		{
		}

		public TimeExpression(Interval<int> charOffsets, Interval<int> tokenOffsets, Func<ICoreMap, SUTime.Temporal> temporalFunc, double priority, double weight)
			: base(charOffsets, tokenOffsets, GetSingleAnnotationExtractor(temporalFunc), priority, weight)
		{
		}

		protected internal static readonly Func<MatchedExpression, TimeExpression> TimeExpressionConverter = null;

		//int tid;     // Time ID
		// todo [2013]: never read. Can delete? (Set in TimeExpressionExtractorImpl)
		//int anchorTimeId = -1;
		private static MatchedExpression.SingleAnnotationExtractor GetSingleAnnotationExtractor(Func<ICoreMap, SUTime.Temporal> temporalFunc)
		{
			MatchedExpression.SingleAnnotationExtractor extractFunc = new MatchedExpression.SingleAnnotationExtractor();
			extractFunc.valueExtractor = null;
			extractFunc.tokensAnnotationField = typeof(CoreAnnotations.NumerizedTokensAnnotation);
			extractFunc.resultAnnotationField = Java.Util.Collections.SingletonList((Type)typeof(TimeExpression.Annotation));
			extractFunc.resultNestedAnnotationField = typeof(TimeExpression.ChildrenAnnotation);
			extractFunc.resultAnnotationExtractor = TimeExpressionConverter;
			extractFunc.tokensAggregator = CoreMapAggregator.DefaultNumericTokensAggregator;
			return extractFunc;
		}

		public virtual bool AddMod()
		{
			SUTime.Temporal t = GetTemporal();
			if (t != null)
			{
				if (t != SUTime.TimeNoneOk)
				{
					SetTemporal(t);
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		public override bool ExtractAnnotation(Env env, ICoreMap sourceAnnotation)
		{
			bool okay = base.ExtractAnnotation(env, sourceAnnotation);
			//super.extractAnnotation(sourceAnnotation, CoreAnnotations.NumerizedTokensAnnotation.class,
			//CoreMapAttributeAggregator.DEFAULT_NUMERIC_TOKENS_AGGREGATORS,
			//TimeExpression.Annotation.class, TimeExpression.ChildrenAnnotation.class);
			if (okay)
			{
				return AddMod();
			}
			else
			{
				return false;
			}
		}

		public override bool ExtractAnnotation<_T0>(Env env, IList<_T0> source)
		{
			bool okay = base.ExtractAnnotation(env, source);
			//super.extractAnnotation(source, CoreMapAttributeAggregator.getDefaultAggregators(),
			//TimeExpression.Annotation.class, TimeExpression.ChildrenAnnotation.class);
			if (okay)
			{
				return AddMod();
			}
			else
			{
				return false;
			}
		}

		/* public int getTid() {
		return tid;
		}*/
		public virtual SUTime.Temporal GetTemporal()
		{
			if (value != null && value.Get() is SUTime.Temporal)
			{
				return (SUTime.Temporal)value.Get();
			}
			return null;
		}

		public virtual void SetTemporal(SUTime.Temporal temporal)
		{
			this.value = new Expressions.PrimitiveValue<SUTime.Temporal>("Temporal", temporal);
		}
		/*  public String toString()
		{
		return text;
		} */
		/*  public Timex getTimex(SUTime.TimeIndex timeIndex) {
		Timex timex = temporal.getTimex(timeIndex);
		timex.text = text;
		timex.xml = timex
		assert(timex.tid == tid);
		} */
	}
}

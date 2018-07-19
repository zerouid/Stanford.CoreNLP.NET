using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>
	/// Provides generic mechanism to convert natural language into temporal representation
	/// by reading patterns/rules specifying mapping between text and temporal objects
	/// from file.
	/// </summary>
	/// <author>Angel Chang</author>
	public class GenericTimeExpressionPatterns : ITimeExpressionPatterns
	{
		internal Env env;

		internal Options options;

		public GenericTimeExpressionPatterns(Options options)
		{
			this.options = options;
			InitEnv();
			if (options.binders != null)
			{
				foreach (Env.IBinder binder in options.binders)
				{
					binder.Bind(env);
				}
			}
		}

		public virtual CoreMapExpressionExtractor CreateExtractor()
		{
			IList<string> filenames = StringUtils.Split(options.grammarFilename, "\\s*[,;]\\s*");
			return CoreMapExpressionExtractor.CreateExtractorFromFiles(env, filenames);
		}

		[System.Serializable]
		private class TimexTypeMatchNodePattern : NodePattern<TimeExpression>
		{
			internal SUTime.TimexType type;

			public TimexTypeMatchNodePattern(SUTime.TimexType type)
			{
				this.type = type;
			}

			public override bool Match(TimeExpression te)
			{
				if (te != null)
				{
					SUTime.Temporal t = te.GetTemporal();
					if (t != null)
					{
						return type.Equals(t.GetTimexType());
					}
				}
				return false;
			}
		}

		[System.Serializable]
		private class MatchedExpressionValueTypeMatchNodePattern : NodePattern<MatchedExpression>
		{
			internal string valueType;

			public MatchedExpressionValueTypeMatchNodePattern(string valueType)
			{
				this.valueType = valueType;
			}

			public override bool Match(MatchedExpression me)
			{
				IValue v = (me != null) ? me.GetValue() : null;
				if (v != null)
				{
					return (valueType.Equals(v.GetType()));
				}
				return false;
			}
		}

		private void InitEnv()
		{
			env = TokenSequencePattern.GetNewEnv();
			env.SetDefaultResultsAnnotationExtractor(TimeExpression.TimeExpressionConverter);
			env.SetDefaultTokensAnnotationKey(typeof(CoreAnnotations.NumerizedTokensAnnotation));
			env.SetDefaultResultAnnotationKey(typeof(TimeExpression.Annotation));
			env.SetDefaultNestedResultsAnnotationKey(typeof(TimeExpression.ChildrenAnnotation));
			env.SetDefaultTokensAggregators(CoreMapAttributeAggregator.DefaultNumericTokensAggregators);
			env.Bind("nested", typeof(TimeExpression.ChildrenAnnotation));
			env.Bind("time", new TimeFormatter.TimePatternExtractRuleCreator());
			// Do case insensitive matching
			env.SetDefaultStringPatternFlags(Pattern.CaseInsensitive | Pattern.UnicodeCase);
			env.Bind("options", options);
			env.Bind("TIME_REF", SUTime.TimeRef);
			env.Bind("TIME_REF_UNKNOWN", SUTime.TimeRefUnknown);
			env.Bind("TIME_UNKNOWN", SUTime.TimeUnknown);
			env.Bind("TIME_NONE", SUTime.TimeNone);
			env.Bind("ERA_AD", SUTime.EraAd);
			env.Bind("ERA_BC", SUTime.EraBc);
			env.Bind("ERA_UNKNOWN", SUTime.EraUnknown);
			env.Bind("HALFDAY_AM", SUTime.HalfdayAm);
			env.Bind("HALFDAY_PM", SUTime.HalfdayPm);
			env.Bind("HALFDAY_UNKNOWN", SUTime.HalfdayUnknown);
			env.Bind("RESOLVE_TO_THIS", SUTime.ResolveToThis);
			env.Bind("RESOLVE_TO_PAST", SUTime.ResolveToPast);
			env.Bind("RESOLVE_TO_FUTURE", SUTime.ResolveToFuture);
			env.Bind("RESOLVE_TO_CLOSEST", SUTime.ResolveToClosest);
			env.Bind("numcomptype", typeof(CoreAnnotations.NumericCompositeTypeAnnotation));
			env.Bind("numcompvalue", typeof(CoreAnnotations.NumericCompositeValueAnnotation));
			env.Bind("temporal", typeof(TimeExpression.Annotation));
			//    env.bind("tags", SequenceMatchRules.Tags.TagsAnnotation.class);
			env.Bind("::IS_TIMEX_DATE", new GenericTimeExpressionPatterns.TimexTypeMatchNodePattern(SUTime.TimexType.Date));
			env.Bind("::IS_TIMEX_DURATION", new GenericTimeExpressionPatterns.TimexTypeMatchNodePattern(SUTime.TimexType.Duration));
			env.Bind("::IS_TIMEX_TIME", new GenericTimeExpressionPatterns.TimexTypeMatchNodePattern(SUTime.TimexType.Time));
			env.Bind("::IS_TIMEX_SET", new GenericTimeExpressionPatterns.TimexTypeMatchNodePattern(SUTime.TimexType.Set));
			env.Bind("::IS_TIME_UNIT", new GenericTimeExpressionPatterns.MatchedExpressionValueTypeMatchNodePattern("TIMEUNIT"));
			env.Bind("::MONTH", new GenericTimeExpressionPatterns.MatchedExpressionValueTypeMatchNodePattern("MONTH_OF_YEAR"));
			env.Bind("::DAYOFWEEK", new GenericTimeExpressionPatterns.MatchedExpressionValueTypeMatchNodePattern("DAY_OF_WEEK"));
			// BINDINGS for parsing from file!!!!!!!
			foreach (SUTime.TemporalOp t in SUTime.TemporalOp.Values())
			{
				env.Bind(t.ToString(), new Expressions.PrimitiveValue<SUTime.TemporalOp>("TemporalOp", t));
			}
			foreach (SUTime.TimeUnit t_1 in SUTime.TimeUnit.Values())
			{
				if (!t_1.Equals(SUTime.TimeUnit.Unknown))
				{
					//env.bind(t.name(), new SequenceMatchRules.PrimitiveValue<SUTime.Temporal>("DURATION", t.getDuration(), "TIMEUNIT"));
					env.Bind(t_1.ToString(), new Expressions.PrimitiveValue<SUTime.Temporal>("TIMEUNIT", t_1.GetDuration()));
				}
			}
			foreach (SUTime.StandardTemporalType t_2 in SUTime.StandardTemporalType.Values())
			{
				env.Bind(t_2.ToString(), new Expressions.PrimitiveValue<SUTime.StandardTemporalType>("TemporalType", t_2));
			}
			env.Bind("Duration", new Expressions.PrimitiveValue<IValueFunction>(Expressions.TypeFunction, new _NamedValueFunction_124("Duration")));
			// New so we get different time ids
			// TODO: Check args
			// TODO: Handle Strings...
			// TODO: This should already be in durations....
			//String durationUnitString = (durationUnitTokens != null)? durationUnitTokens.get(0).get(CoreAnnotations.TextAnnotation.class):null;
			//SUTime.Duration durationUnit = getDuration(durationUnitString);
			// TODO: Handle inexactness
			// Create duration range...
			// Add begin and end times
			env.Bind("DayOfWeek", new Expressions.PrimitiveValue<IValueFunction>(Expressions.TypeFunction, new _NamedValueFunction_212("DayOfWeek")));
			env.Bind("MonthOfYear", new Expressions.PrimitiveValue<IValueFunction>(Expressions.TypeFunction, new _NamedValueFunction_235("MonthOfYear")));
			env.Bind("MakePeriodicTemporalSet", new Expressions.PrimitiveValue<IValueFunction>(Expressions.TypeFunction, new _NamedValueFunction_258("MakePeriodicTemporalSet")));
			// First argument is the temporal acting as the base of the periodic set
			// Second argument is the quantifier (string)
			// Third argument is the multiple (how much to scale the natural period)
			/*"P1X"*/
			env.Bind("TemporalCompose", new Expressions.PrimitiveValue<IValueFunction>(Expressions.TypeFunction, new _NamedValueFunction_328("TemporalCompose")));
		}

		private sealed class _NamedValueFunction_124 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_124(string baseArg1)
				: base(baseArg1)
			{
			}

			private SUTime.Temporal AddEndPoints(SUTime.Duration d, SUTime.Time beginTime, SUTime.Time endTime)
			{
				SUTime.Temporal t = d;
				if (d != null && (beginTime != null || endTime != null))
				{
					SUTime.Time b = beginTime;
					SUTime.Time e = endTime;
					if (b == SUTime.TimeRefUnknown)
					{
						b = new SUTime.RefTime("UNKNOWN");
					}
					else
					{
						if (b == SUTime.TimeUnknown)
						{
							b = new SUTime.SimpleTime("UNKNOWN");
						}
					}
					if (e == SUTime.TimeRefUnknown)
					{
						e = new SUTime.RefTime("UNKNOWN");
					}
					else
					{
						if (e == SUTime.TimeUnknown)
						{
							e = new SUTime.SimpleTime("UNKNOWN");
						}
					}
					t = new SUTime.Range(b, e, d);
				}
				return t;
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count == 2)
				{
					SUTime.Duration d = (SUTime.Duration)@in[0].Get();
					if (@in[1].Get() is Number)
					{
						int m = ((Number)@in[1].Get());
						return new Expressions.PrimitiveValue("DURATION", d.MultiplyBy(m));
					}
					else
					{
						if (@in[1].Get() is string)
						{
							Number n = System.Convert.ToInt32((string)@in[1].Get());
							if (n != null)
							{
								return new Expressions.PrimitiveValue("DURATION", d.MultiplyBy(n));
							}
							else
							{
								return null;
							}
						}
						else
						{
							throw new ArgumentException("Invalid arguments to " + this.name);
						}
					}
				}
				else
				{
					if (@in.Count == 5 || @in.Count == 3)
					{
						IList<ICoreMap> durationStartTokens = (IList<ICoreMap>)@in[0].Get();
						Number durationStartVal = (durationStartTokens != null) ? durationStartTokens[0].Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation)) : null;
						IList<ICoreMap> durationEndTokens = (IList<ICoreMap>)@in[1].Get();
						Number durationEndVal = (durationEndTokens != null) ? durationEndTokens[0].Get(typeof(CoreAnnotations.NumericCompositeValueAnnotation)) : null;
						IList<ICoreMap> durationUnitTokens = (IList<ICoreMap>)@in[2].Get();
						TimeExpression te = (durationUnitTokens != null) ? durationUnitTokens[0].Get(typeof(TimeExpression.Annotation)) : null;
						SUTime.Duration durationUnit = (SUTime.Duration)te.GetTemporal();
						SUTime.Duration durationStart = (durationStartVal != null) ? durationUnit.MultiplyBy(durationStartVal) : null;
						SUTime.Duration durationEnd = (durationEndVal != null) ? durationUnit.MultiplyBy(durationEndVal) : null;
						SUTime.Duration duration = durationStart;
						if (duration == null)
						{
							if (durationEnd != null)
							{
								duration = durationEnd;
							}
							else
							{
								duration = new SUTime.InexactDuration(durationUnit);
							}
						}
						else
						{
							if (durationEnd != null)
							{
								duration = new SUTime.DurationRange(durationStart, durationEnd);
							}
						}
						SUTime.Time beginTime = (@in.Count > 3) ? (SUTime.Time)@in[3].Get() : null;
						SUTime.Time endTime = (@in.Count > 4) ? (SUTime.Time)@in[4].Get() : null;
						SUTime.Temporal temporal = this.AddEndPoints(duration, beginTime, endTime);
						if (temporal is SUTime.Range)
						{
							return new Expressions.PrimitiveValue("RANGE", temporal);
						}
						else
						{
							return new Expressions.PrimitiveValue("DURATION", temporal);
						}
					}
					else
					{
						throw new ArgumentException("Invalid number of arguments to " + this.name);
					}
				}
			}
		}

		private sealed class _NamedValueFunction_212 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_212(string baseArg1)
				: base(baseArg1)
			{
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is Number))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count == 1)
				{
					return new Expressions.PrimitiveValue(SUTime.StandardTemporalType.DayOfWeek.ToString(), SUTime.StandardTemporalType.DayOfWeek.CreateTemporal(((Number)@in[0].Get())));
				}
				else
				{
					throw new ArgumentException("Invalid number of arguments to " + this.name);
				}
			}
		}

		private sealed class _NamedValueFunction_235 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_235(string baseArg1)
				: base(baseArg1)
			{
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is Number))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count == 1)
				{
					return new Expressions.PrimitiveValue(SUTime.StandardTemporalType.MonthOfYear.ToString(), SUTime.StandardTemporalType.MonthOfYear.CreateTemporal(((Number)@in[0].Get())));
				}
				else
				{
					throw new ArgumentException("Invalid number of arguments to " + this.name);
				}
			}
		}

		private sealed class _NamedValueFunction_258 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_258(string baseArg1)
				: base(baseArg1)
			{
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 3)
				{
					return false;
				}
				if (@in[0] == null || (!(@in[0].Get() is SUTime.Temporal) && !(@in[0].Get() is TimeExpression)))
				{
					return false;
				}
				if (@in[1] == null || (!(@in[1].Get() is string) && !(@in[1].Get() is IList)))
				{
					return false;
				}
				if (@in[2] == null || !(@in[2].Get() is Number))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count >= 1)
				{
					SUTime.Temporal temporal = null;
					object t = @in[0].Get();
					if (t is SUTime.Temporal)
					{
						temporal = (SUTime.Temporal)@in[0].Get();
					}
					else
					{
						if (t is TimeExpression)
						{
							temporal = ((TimeExpression)t).GetTemporal();
						}
						else
						{
							throw new ArgumentException("Type mismatch on arg0: Cannot apply " + this + " to " + @in);
						}
					}
					string quant = null;
					int scale = 1;
					if (@in.Count >= 2 && @in[1] != null)
					{
						object arg1 = @in[1].Get();
						if (arg1 is string)
						{
							quant = (string)arg1;
						}
						else
						{
							if (arg1 is IList)
							{
								IList<ICoreMap> cms = (IList<ICoreMap>)arg1;
								quant = ChunkAnnotationUtils.GetTokenText(cms, typeof(CoreAnnotations.TextAnnotation));
								if (quant != null)
								{
									quant = quant.ToLower();
								}
							}
							else
							{
								throw new ArgumentException("Type mismatch on arg1: Cannot apply " + this + " to " + @in);
							}
						}
					}
					if (@in.Count >= 3 && @in[2] != null)
					{
						Number arg2 = (Number)@in[2].Get();
						if (arg2 != null)
						{
							scale = arg2;
						}
					}
					SUTime.Duration period = temporal.GetPeriod();
					if (period != null && scale != 1)
					{
						period = period.MultiplyBy(scale);
					}
					return new Expressions.PrimitiveValue("PeriodicTemporalSet", new SUTime.PeriodicTemporalSet(temporal, period, quant, null));
				}
				else
				{
					throw new ArgumentException("Invalid number of arguments to " + this.name);
				}
			}
		}

		private sealed class _NamedValueFunction_328 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_328(string baseArg1)
				: base(baseArg1)
			{
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is SUTime.TemporalOp))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count > 1)
				{
					SUTime.TemporalOp op = (SUTime.TemporalOp)@in[0].Get();
					bool allTemporalArgs = true;
					object[] args = new object[@in.Count - 1];
					for (int i = 0; i < args.Length; i++)
					{
						IValue v = @in[i + 1];
						if (v != null)
						{
							args[i] = v.Get();
							if (args[i] is MatchedExpression)
							{
								IValue v2 = ((MatchedExpression)args[i]).GetValue();
								args[i] = (v2 != null) ? v2.Get() : null;
							}
							if (args[i] != null && !(args[i] is SUTime.Temporal))
							{
								allTemporalArgs = false;
							}
						}
					}
					if (allTemporalArgs)
					{
						SUTime.Temporal[] temporalArgs = new SUTime.Temporal[args.Length];
						for (int i_1 = 0; i_1 < args.Length; i_1++)
						{
							temporalArgs[i_1] = (SUTime.Temporal)args[i_1];
						}
						return new Expressions.PrimitiveValue(null, op.Apply(temporalArgs));
					}
					else
					{
						return new Expressions.PrimitiveValue(null, op.Apply(args));
					}
				}
				else
				{
					throw new ArgumentException("Invalid number of arguments to " + this.name);
				}
			}
		}

		public virtual int DetermineRelFlags(ICoreMap annotation, TimeExpression te)
		{
			int flags = 0;
			bool flagsSet = false;
			if (te.value.GetTags() != null)
			{
				IValue v = te.value.GetTags().GetTag("resolveTo");
				if (v != null && v.Get() is Number)
				{
					flags = ((Number)v.Get());
					flagsSet = true;
				}
			}
			if (!flagsSet)
			{
				if (te.GetTemporal() is SUTime.PartialTime)
				{
					flags = SUTime.ResolveToClosest;
				}
			}
			return flags;
		}
	}
}

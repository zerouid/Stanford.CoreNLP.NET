using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;







namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>ValueFunctions supported by tokensregex.</summary>
	/// <author>Angel Chang</author>
	public class ValueFunctions
	{
		private ValueFunctions()
		{
		}

		// static methods
		protected internal static object LookupFunctionObject(Env env, string name)
		{
			if (env != null)
			{
				object obj = env.Get(name);
				if (obj != null)
				{
					return obj;
				}
			}
			return registeredFunctions[name];
		}

		public abstract class NamedValueFunction : IValueFunction
		{
			protected internal string name;

			protected internal string signature;

			public NamedValueFunction(string name)
			{
				this.name = name;
			}

			public virtual string GetDescription()
			{
				return string.Empty;
			}

			public virtual string GetParamDesc()
			{
				return "...";
			}

			protected internal static string GetParamDesc(string type, int nargs)
			{
				if (nargs < 0)
				{
					return type + "...";
				}
				else
				{
					if (nargs <= 3)
					{
						string[] tmp = new string[nargs];
						Arrays.Fill(tmp, type);
						return StringUtils.Join(tmp, ",");
					}
					else
					{
						return type + '[' + nargs + ']';
					}
				}
			}

			protected internal static string GetTypeName(Type c)
			{
				return c.GetCanonicalName();
			}

			public override string ToString()
			{
				if (signature == null)
				{
					signature = name + '(' + GetParamDesc() + ')';
				}
				return signature;
			}

			public abstract IValue Apply(Env arg1, IList<IValue> arg2);

			public abstract bool CheckArgs(IList<IValue> arg1);
		}

		public class ParamInfo
		{
			public readonly string name;

			public readonly string typeName;

			public readonly Type className;

			public readonly bool nullable;

			public ParamInfo(string name, string typeName, Type className, bool nullable)
			{
				// end static class NamedValueFunction
				this.name = name;
				this.typeName = typeName;
				this.className = className;
				this.nullable = nullable;
			}
		}

		public abstract class TypeCheckedFunction : ValueFunctions.NamedValueFunction
		{
			internal IList<ValueFunctions.ParamInfo> paramInfos;

			internal int nargs;

			public TypeCheckedFunction(string name, IList<ValueFunctions.ParamInfo> paramInfos)
				: base(name)
			{
				this.paramInfos = paramInfos;
				nargs = (paramInfos != null) ? paramInfos.Count : 0;
			}

			public TypeCheckedFunction(string name, params ValueFunctions.ParamInfo[] paramInfos)
				: base(name)
			{
				this.paramInfos = Arrays.AsList(paramInfos);
				nargs = paramInfos.Length;
			}

			public override string GetParamDesc()
			{
				StringBuilder sb = new StringBuilder();
				foreach (ValueFunctions.ParamInfo p in paramInfos)
				{
					if (sb.Length > 0)
					{
						sb.Append(", ");
					}
					if (p.typeName != null)
					{
						sb.Append(p.typeName);
					}
					else
					{
						sb.Append(GetTypeName(p.className));
					}
				}
				return sb.ToString();
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != nargs)
				{
					return false;
				}
				for (int i = 0; i < @in.Count; i++)
				{
					IValue v = @in[i];
					ValueFunctions.ParamInfo p = paramInfos[i];
					if (v == null)
					{
						if (!p.nullable)
						{
							return false;
						}
					}
					else
					{
						if (p.typeName != null && !p.typeName.Equals(v.GetType()))
						{
							return false;
						}
						if (v.Get() != null)
						{
							if (p.className != null && !(p.className.IsAssignableFrom(v.Get().GetType())))
							{
								return false;
							}
						}
					}
				}
				return true;
			}
		}

		public abstract class NumericFunction : ValueFunctions.NamedValueFunction
		{
			protected internal string resultTypeName = Expressions.TypeNumber;

			protected internal int nargs = 2;

			protected internal NumericFunction(string name, int nargs)
				: base(name)
			{
				this.nargs = nargs;
			}

			protected internal NumericFunction(string name, int nargs, string resultTypeName)
				: base(name)
			{
				this.resultTypeName = resultTypeName;
				this.nargs = nargs;
			}

			public override string GetParamDesc()
			{
				return GetParamDesc(Expressions.TypeNumber, nargs);
			}

			public abstract Number Compute(params Number[] ns);

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (nargs > 0 && @in.Count != nargs)
				{
					return false;
				}
				foreach (IValue v in @in)
				{
					if (v == null || !(v.Get() is Number))
					{
						return false;
					}
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (nargs > 0 && @in.Count != nargs)
				{
					throw new ArgumentException(nargs + " arguments expected, got " + @in.Count);
				}
				Number[] numbers = new Number[@in.Count];
				for (int i = 0; i < @in.Count; i++)
				{
					numbers[i] = (Number)@in[i].Get();
				}
				Number res = Compute(numbers);
				return new Expressions.PrimitiveValue(resultTypeName, res);
			}
		}

		private sealed class _NumericFunction_195 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_195(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					return @in[0] + @in[1];
				}
				else
				{
					return @in[0] + @in[1];
				}
			}
		}

		public static readonly IValueFunction AddFunction = new _NumericFunction_195("ADD", 2);

		private sealed class _NumericFunction_206 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_206(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					return @in[0] - @in[1];
				}
				else
				{
					return @in[0] - @in[1];
				}
			}
		}

		public static readonly IValueFunction SubtractFunction = new _NumericFunction_206("SUBTRACT", 2);

		private sealed class _NumericFunction_217 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_217(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					return @in[0] * @in[1];
				}
				else
				{
					return @in[0] * @in[1];
				}
			}
		}

		public static readonly IValueFunction MultiplyFunction = new _NumericFunction_217("MULTIPLY", 2);

		private sealed class _NumericFunction_228 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_228(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					if (@in[0] % @in[1] == 0)
					{
						return @in[0] / @in[1];
					}
					else
					{
						return @in[0] / @in[1];
					}
				}
				else
				{
					return @in[0] / @in[1];
				}
			}
		}

		public static readonly IValueFunction DivideFunction = new _NumericFunction_228("DIVIDE", 2);

		private sealed class _NumericFunction_241 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_241(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					return @in[0] % @in[1];
				}
				else
				{
					return @in[0] % @in[1];
				}
			}
		}

		public static readonly IValueFunction ModFunction = new _NumericFunction_241("MOD", 2);

		private sealed class _NumericFunction_252 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_252(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					return Math.Max(@in[0], @in[1]);
				}
				else
				{
					return Math.Max(@in[0], @in[1]);
				}
			}
		}

		public static readonly IValueFunction MaxFunction = new _NumericFunction_252("MAX", 2);

		private sealed class _NumericFunction_263 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_263(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]) && ValueFunctions.IsInteger(@in[1]))
				{
					return Math.Min(@in[0], @in[1]);
				}
				else
				{
					return Math.Min(@in[0], @in[1]);
				}
			}
		}

		public static readonly IValueFunction MinFunction = new _NumericFunction_263("MIN", 2);

		private sealed class _NumericFunction_274 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_274(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				return Math.Pow(@in[0], @in[1]);
			}
		}

		public static readonly IValueFunction PowFunction = new _NumericFunction_274("POW", 2);

		private sealed class _NumericFunction_281 : ValueFunctions.NumericFunction
		{
			public _NumericFunction_281(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override Number Compute(params Number[] @in)
			{
				if (ValueFunctions.IsInteger(@in[0]))
				{
					return -@in[0];
				}
				else
				{
					return -@in[0];
				}
			}
		}

		public static readonly IValueFunction NegateFunction = new _NumericFunction_281("NEGATE", 1);

		public abstract class BooleanFunction : ValueFunctions.NamedValueFunction
		{
			protected internal string resultTypeName = Expressions.TypeBoolean;

			protected internal int nargs = 2;

			protected internal BooleanFunction(string name, int nargs)
				: base(name)
			{
				this.nargs = nargs;
			}

			protected internal BooleanFunction(string name, int nargs, string resultTypeName)
				: base(name)
			{
				this.resultTypeName = resultTypeName;
				this.nargs = nargs;
			}

			public abstract bool Compute(params bool[] ns);

			public override string GetParamDesc()
			{
				return GetParamDesc(Expressions.TypeBoolean, nargs);
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (nargs > 0 && @in.Count != nargs)
				{
					return false;
				}
				foreach (IValue v in @in)
				{
					if (v == null || !(v.Get() is bool))
					{
						return false;
					}
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (nargs > 0 && @in.Count != nargs)
				{
					throw new ArgumentException(nargs + " arguments expected, got " + @in.Count);
				}
				bool[] bools = new bool[@in.Count];
				for (int i = 0; i < @in.Count; i++)
				{
					bools[i] = (bool)@in[i].Get();
				}
				bool res = Compute(bools);
				return new Expressions.PrimitiveValue(resultTypeName, res);
			}
		}

		private sealed class _BooleanFunction_341 : ValueFunctions.BooleanFunction
		{
			public _BooleanFunction_341(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override bool Compute(params bool[] @in)
			{
				foreach (bool b in @in)
				{
					if (!b)
					{
						return false;
					}
				}
				return true;
			}
		}

		public static readonly IValueFunction AndFunction = new _BooleanFunction_341("AND", -1);

		private sealed class _BooleanFunction_351 : ValueFunctions.BooleanFunction
		{
			public _BooleanFunction_351(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override bool Compute(params bool[] @in)
			{
				foreach (bool b in @in)
				{
					if (b)
					{
						return true;
					}
				}
				return false;
			}
		}

		public static readonly IValueFunction OrFunction = new _BooleanFunction_351("OR", -1);

		private sealed class _BooleanFunction_361 : ValueFunctions.BooleanFunction
		{
			public _BooleanFunction_361(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override bool Compute(params bool[] @in)
			{
				bool res = !@in[0];
				return res;
			}
		}

		public static readonly IValueFunction NotFunction = new _BooleanFunction_361("NOT", 1);

		private static string Join(object[] args, string glue)
		{
			string res = null;
			if (args.Length == 1)
			{
				// Only one element - check if it is a list or array and do join on that
				if (args[0] is IEnumerable)
				{
					res = StringUtils.Join((IEnumerable)args[0], glue);
				}
				else
				{
					res = StringUtils.Join(args, glue);
				}
			}
			else
			{
				res = StringUtils.Join(args, glue);
			}
			return res;
		}

		public abstract class StringFunction : ValueFunctions.NamedValueFunction
		{
			protected internal string resultTypeName = Expressions.TypeString;

			protected internal int nargs = 2;

			protected internal StringFunction(string name, int nargs)
				: base(name)
			{
				this.nargs = nargs;
			}

			protected internal StringFunction(string name, int nargs, string resultTypeName)
				: base(name)
			{
				this.resultTypeName = resultTypeName;
				this.nargs = nargs;
			}

			public abstract string Compute(params string[] strs);

			public override string GetParamDesc()
			{
				return GetParamDesc(Expressions.TypeString, nargs);
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (nargs > 0 && @in.Count != nargs)
				{
					return false;
				}
				foreach (IValue v in @in)
				{
					if (v == null)
					{
						/*|| !(v.get() instanceof String) */
						return false;
					}
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (nargs > 0 && @in.Count != nargs)
				{
					throw new ArgumentException(nargs + " arguments expected, got " + @in.Count);
				}
				string[] strs = new string[@in.Count];
				for (int i = 0; i < @in.Count; i++)
				{
					if (@in[i].Get() is string)
					{
						strs[i] = (string)@in[i].Get();
					}
					else
					{
						if (@in[i].Get() != null)
						{
							strs[i] = @in[i].Get().ToString();
						}
						else
						{
							strs[i] = null;
						}
					}
				}
				string res = Compute(strs);
				return new Expressions.PrimitiveValue(resultTypeName, res);
			}
		}

		private sealed class _StringFunction_439 : ValueFunctions.StringFunction
		{
			public _StringFunction_439(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override string Compute(params string[] @in)
			{
				return ValueFunctions.Join(@in, string.Empty);
			}
		}

		public static readonly IValueFunction ConcatFunction = new _StringFunction_439("CONCAT", -1);

		private sealed class _StringFunction_446 : ValueFunctions.StringFunction
		{
			public _StringFunction_446(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override string Compute(params string[] @in)
			{
				return @in[0].ToUpper();
			}
		}

		public static readonly IValueFunction UppercaseFunction = new _StringFunction_446("UPPERCASE", 1);

		private sealed class _StringFunction_453 : ValueFunctions.StringFunction
		{
			public _StringFunction_453(string baseArg1, int baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			public override string Compute(params string[] @in)
			{
				return @in[0].ToLower();
			}
		}

		public static readonly IValueFunction LowercaseFunction = new _StringFunction_453("LOWERCASE", 1);

		private sealed class _NamedValueFunction_460 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_460(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "...";
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 1)
				{
					return false;
				}
				if (@in.Count > 1 && (@in[0] == null || !(@in[0].Get() is string)))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count > 1)
				{
					string format = (string)@in[0].Get();
					object[] args = new object[@in.Count - 1];
					for (int i = 1; i < @in.Count; i++)
					{
						args[i - 1] = @in[i].Get();
					}
					string res = string.Format(format, args);
					System.Console.Out.Write(res);
				}
				else
				{
					System.Console.Out.Write(@in[0]);
				}
				return null;
			}
		}

		public static readonly IValueFunction PrintFunction = new _NamedValueFunction_460("PRINT");

		private sealed class _NamedValueFunction_494 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_494(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return Expressions.TypeString + ",...";
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				string format = (string)@in[0].Get();
				object[] args = new object[@in.Count - 1];
				for (int i = 1; i < @in.Count; i++)
				{
					args[i - 1] = @in[i].Get();
				}
				string res = string.Format(format, args);
				return new Expressions.PrimitiveValue(Expressions.TypeString, res);
			}
		}

		public static readonly IValueFunction FormatFunction = new _NamedValueFunction_494("FORMAT");

		private sealed class _NamedValueFunction_523 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_523(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "String glue,...";
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				string glue = (string)@in[0].Get();
				object[] args = new object[@in.Count - 1];
				for (int i = 1; i < @in.Count; i++)
				{
					args[i - 1] = @in[i].Get();
				}
				string res = ValueFunctions.Join(args, glue);
				return new Expressions.PrimitiveValue(Expressions.TypeString, res);
			}
		}

		public static readonly IValueFunction JoinFunction = new _NamedValueFunction_523("JOIN");

		private sealed class _NamedValueFunction_552 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_552(string baseArg1)
				: base(baseArg1)
			{
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is IList))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IList list = (IList)@in[0].Get();
				string[] args = new string[list.Count];
				for (int i = 0; i < list.Count; i++)
				{
					args[i] = list[i].ToString();
				}
				MultiWordStringMatcher matcher = new MultiWordStringMatcher("EXCTWS");
				string regex = matcher.GetRegex(args);
				return new Expressions.PrimitiveValue(Expressions.TypeRegex, regex);
			}
		}

		public static readonly IValueFunction CreateRegexFunction = new _NamedValueFunction_552("CREATE_REGEX");

		private static readonly ValueFunctions.ParamInfo ParamInfoValueFunction = new ValueFunctions.ParamInfo("FUNCTION", Expressions.TypeFunction, typeof(IValueFunction), false);

		private static readonly ValueFunctions.ParamInfo ParamInfoList = new ValueFunctions.ParamInfo("LIST", null, typeof(IList), true);

		private sealed class _TypeCheckedFunction_580 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_580(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is list of elements to apply function to
			// Second argument is function to apply
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null)
				{
					return null;
				}
				IList list = (IList)@in[0].Get();
				IValueFunction func = (IValueFunction)@in[1].Get();
				IList<IValue> res = new List<IValue>(list.Count);
				foreach (object elem in list)
				{
					IList<IValue> args = new List<IValue>(1);
					args.Add(Expressions.CreateValue(Expressions.TypeList, elem));
					res.Add(func.Apply(env, args));
				}
				return new Expressions.PrimitiveValue<IList<IValue>>(Expressions.TypeList, res);
			}
		}

		public static readonly IValueFunction MapValuesFunction = new _TypeCheckedFunction_580("MAP_VALUES", ParamInfoList);

		private static readonly ValueFunctions.ParamInfo ParamInfoFunction = new ValueFunctions.ParamInfo("FUNCTION", Expressions.TypeFunction, typeof(Func), false);

		private sealed class _TypeCheckedFunction_599 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_599(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is list of elements to apply function to
			// Second argument is function to apply
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null)
				{
					return null;
				}
				IList list = (IList)@in[0].Get();
				Func func = (Func)@in[1].Get();
				IList<object> res = new List<object>(list.Count);
				foreach (object elem in list)
				{
					res.Add(func.Apply(elem));
				}
				return new Expressions.PrimitiveValue<IList<object>>(null, res);
			}
		}

		public static readonly IValueFunction MapFunction = new _TypeCheckedFunction_599("MAP", ParamInfoList);

		private static readonly ValueFunctions.ParamInfo ParamInfoTokenRegex = new ValueFunctions.ParamInfo("TOKEN_REGEX", Expressions.TypeTokenRegex, typeof(TokenSequencePattern), false);

		private static readonly ValueFunctions.ParamInfo ParamInfoTokenList = new ValueFunctions.ParamInfo("TOKEN_LIST", null, typeof(IList), true);

		private static readonly ValueFunctions.ParamInfo ParamInfoTokenListReplace = new ValueFunctions.ParamInfo("TOKEN_LIST_REPLACEMENT", null, typeof(IList), true);

		private sealed class _TypeCheckedFunction_621 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_621(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is list of tokens to match
			// Second argument is pattern to match
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return Expressions.False;
				}
				IList<ICoreMap> cms = (IList<ICoreMap>)@in[0].Get();
				TokenSequencePattern pattern = (TokenSequencePattern)@in[1].Get();
				TokenSequenceMatcher matcher = ((TokenSequenceMatcher)pattern.GetMatcher(cms));
				bool matches = matcher.Matches();
				return (matches) ? Expressions.True : Expressions.False;
			}
		}

		public static readonly IValueFunction TokensMatchFunction = new _TypeCheckedFunction_621("TOKENS_MATCH", ParamInfoTokenList);

		private sealed class _TypeCheckedFunction_637 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_637(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is list of tokens to match
			// Second argument is pattern to match
			// Third argument is replacement tokens
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return Expressions.False;
				}
				IList<ICoreMap> cms = (IList<ICoreMap>)@in[0].Get();
				IList<ICoreMap> replacement = (IList<ICoreMap>)@in[2].Get();
				TokenSequencePattern pattern = (TokenSequencePattern)@in[1].Get();
				TokenSequenceMatcher matcher = ((TokenSequenceMatcher)pattern.GetMatcher(cms));
				IList<ICoreMap> replaced = matcher.ReplaceAll(replacement);
				return new Expressions.PrimitiveValue(Expressions.TypeTokens, replaced);
			}
		}

		public static readonly IValueFunction TokensReplaceFunction = new _TypeCheckedFunction_637("TOKENS_REPLACE", ParamInfoTokenList);

		private static readonly ValueFunctions.ParamInfo ParamInfoStringRegex = new ValueFunctions.ParamInfo("REGEX", Expressions.TypeRegex, null, false);

		private static readonly ValueFunctions.ParamInfo ParamInfoString = new ValueFunctions.ParamInfo("STRING", null, typeof(string), true);

		private static readonly ValueFunctions.ParamInfo ParamInfoStringReplace = new ValueFunctions.ParamInfo("STRING_REPLACEMENT", null, typeof(string), true);

		private sealed class _TypeCheckedFunction_658 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_658(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is string to match
			// Second argument is pattern to match
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return Expressions.False;
				}
				string str = (string)@in[0].Get();
				string regex = (string)@in[1].Get();
				Pattern pattern = env.GetStringPattern(regex);
				Matcher matcher = pattern.Matcher(str);
				bool matches = matcher.Matches();
				return (matches) ? Expressions.True : Expressions.False;
			}
		}

		public static readonly IValueFunction StringMatchFunction = new _TypeCheckedFunction_658("STRING_MATCH", ParamInfoString);

		private sealed class _TypeCheckedFunction_675 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_675(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is string to match
			// Second argument is pattern to match
			// Third argument is replacement string
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return Expressions.False;
				}
				string str = (string)@in[0].Get();
				string replacement = (string)@in[2].Get();
				string regex = (string)@in[1].Get();
				Pattern pattern = env.GetStringPattern(regex);
				Matcher matcher = pattern.Matcher(str);
				string replaced = matcher.ReplaceAll(replacement);
				return new Expressions.PrimitiveValue(Expressions.TypeString, replaced);
			}
		}

		public static readonly IValueFunction StringReplaceFunction = new _TypeCheckedFunction_675("STRING_REPLACE", ParamInfoString);

		private static readonly CoreLabelTokenFactory CorelabelFactory = new CoreLabelTokenFactory();

		private static readonly ValueFunctions.ParamInfo ParamInfoToken = new ValueFunctions.ParamInfo("TOKEN", null, typeof(ICoreMap), false);

		private sealed class _TypeCheckedFunction_697 : ValueFunctions.TypeCheckedFunction
		{
			public _TypeCheckedFunction_697(string baseArg1, ValueFunctions.ParamInfo[] baseArg2)
				: base(baseArg1, baseArg2)
			{
			}

			// First argument is token to split
			// Second argument is pattern to split on
			public override IValue Apply(Env env, IList<IValue> @in)
			{
				ICoreMap cm = (ICoreMap)@in[0].Get();
				string regex = (string)@in[1].Get();
				bool includeMatchedAsTokens = (bool)@in[2].Get();
				Pattern pattern = env.GetStringPattern(regex);
				IList<CoreLabel> res = ChunkAnnotationUtils.SplitCoreMap(pattern, includeMatchedAsTokens, cm, ValueFunctions.CorelabelFactory);
				return new Expressions.PrimitiveValue(Expressions.TypeTokens, res);
			}
		}

		public static readonly IValueFunction TokenStringSplitFunction = new _TypeCheckedFunction_697("TOKEN_STRING_SPLIT", ParamInfoToken);

		public static bool IsInteger(Number n)
		{
			return (n is long || n is int || n is short);
		}

		public static readonly ValueFunctions.NumericComparator NumberComparator = new ValueFunctions.NumericComparator();

		public class NumericComparator : IComparator<Number>
		{
			public virtual int Compare(Number o1, Number o2)
			{
				if (IsInteger(o1) && IsInteger(o2))
				{
					return long.Compare(o1, o2);
				}
				else
				{
					return double.Compare(o1, o2);
				}
			}
		}

		public class ComparableComparator<T> : IComparator<T>
			where T : IComparable<T>
		{
			public virtual int Compare(T o1, T o2)
			{
				return o1.CompareTo(o2);
			}
		}

		public enum CompareType
		{
			Gt,
			Lt,
			Ge,
			Le,
			Eq,
			Ne
		}

		public class CompareFunction<T> : ValueFunctions.NamedValueFunction
		{
			internal IComparator<T> comparator;

			internal ValueFunctions.CompareType compType;

			internal Type clazz;

			public CompareFunction(string name, IComparator<T> comparator, ValueFunctions.CompareType compType, Type clazz)
				: base(name)
			{
				this.comparator = comparator;
				this.compType = compType;
				this.clazz = clazz;
			}

			public override string GetParamDesc()
			{
				return "(" + GetTypeName(clazz) + "," + GetTypeName(clazz) + ")";
			}

			public virtual bool Compare(T o1, T o2)
			{
				int res = comparator.Compare(o1, o2);
				switch (compType)
				{
					case ValueFunctions.CompareType.Gt:
					{
						return res > 0;
					}

					case ValueFunctions.CompareType.Lt:
					{
						return res < 0;
					}

					case ValueFunctions.CompareType.Ge:
					{
						return res >= 0;
					}

					case ValueFunctions.CompareType.Le:
					{
						return res <= 0;
					}

					case ValueFunctions.CompareType.Eq:
					{
						return res == 0;
					}

					case ValueFunctions.CompareType.Ne:
					{
						return res != 0;
					}

					default:
					{
						throw new NotSupportedException("Unknown compType: " + compType);
					}
				}
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					return false;
				}
				if (clazz != null)
				{
					if (@in[0] == null || @in[0].Get() == null || !(clazz.IsAssignableFrom(@in[0].Get().GetType())))
					{
						return false;
					}
					if (@in[1] == null || @in[1].Get() == null || !(clazz.IsAssignableFrom(@in[1].Get().GetType())))
					{
						return false;
					}
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					throw new ArgumentException("2 arguments expected, got " + @in.Count);
				}
				if (@in[0] == null || @in[1] == null || @in[0].Get() == null || @in[1].Get() == null)
				{
					return null;
				}
				// Can't compare...
				bool res = Compare((T)@in[0].Get(), (T)@in[1].Get());
				return (res) ? Expressions.True : Expressions.False;
			}
		}

		private sealed class _NamedValueFunction_797 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_797(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "Object,Object";
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				return @in.Count == 2;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					throw new ArgumentException("2 arguments expected, got " + @in.Count);
				}
				bool res = false;
				if (@in[0] == null || @in[1] == null)
				{
					res = (@in[0] == @in[1]);
				}
				else
				{
					if (@in[0].Get() == null || @in[1].Get() == null)
					{
						res = (@in[0].Get() == @in[1].Get());
					}
					else
					{
						res = @in[0].Get().Equals(@in[1].Get());
					}
				}
				return (res) ? Expressions.False : Expressions.True;
			}
		}

		public static readonly IValueFunction NotEqualsFunction = new _NamedValueFunction_797("EQUALS");

		private sealed class _NamedValueFunction_826 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_826(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "Object,Object";
			}

			public override bool CheckArgs(IList<IValue> @in)
			{
				return @in.Count == 2;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					throw new ArgumentException("2 arguments expected, got " + @in.Count);
				}
				bool res = false;
				if (@in[0] == null || @in[1] == null)
				{
					res = (@in[0] == @in[1]);
				}
				else
				{
					if (@in[0].Get() == null || @in[1].Get() == null)
					{
						res = (@in[0].Get() == @in[1].Get());
					}
					else
					{
						res = @in[0].Get().Equals(@in[1].Get());
					}
				}
				return (res) ? Expressions.True : Expressions.False;
			}
		}

		public static readonly IValueFunction EqualsFunction = new _NamedValueFunction_826("EQUALS");

		private sealed class _NamedValueFunction_854 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_854(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "CoreMap coremap,String fieldName|Class field,[Object value]";
			}

			// First argument is what (CoreMap) to get annotation for
			// Second argument is field (Class or String) to get annotation for
			// Third argument (optional) is annotation value to set
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || (!(@in[0].Get() is ICoreMap) && !(@in[0].Get() is IList)))
				{
					return false;
				}
				if (@in[1] == null || (!(@in[1].Get() is Type) && !(@in[1].Get() is string)))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue cmv = @in[0];
				object field = @in[1].Get();
				Type annotationFieldClass = null;
				if (field is string)
				{
					annotationFieldClass = EnvLookup.LookupAnnotationKey(env, (string)field);
					if (annotationFieldClass == null)
					{
						throw new ArgumentException("Cannot get annotation field " + field);
					}
				}
				else
				{
					if (field is Type)
					{
						annotationFieldClass = (Type)field;
					}
					else
					{
						throw new ArgumentException("Type mismatch on arg1: Cannot apply " + this + " to " + @in);
					}
				}
				if (cmv.Get() is ICoreMap)
				{
					ICoreMap cm = (ICoreMap)cmv.Get();
					if (@in.Count >= 3)
					{
						IValue v = @in[2];
						object annotationObject = (v != null) ? v.Get() : null;
						cm.Set(annotationFieldClass, annotationObject);
					}
					object obj = cm.Get(annotationFieldClass);
					return Expressions.CreateValue(annotationFieldClass.FullName, obj);
				}
				else
				{
					if (cmv.Get() is IList)
					{
						IList<ICoreMap> cmList = (IList<ICoreMap>)cmv.Get();
						if (@in.Count >= 3)
						{
							IValue v = @in[2];
							object annotationObject = (v != null) ? v.Get() : null;
							foreach (ICoreMap cm in cmList)
							{
								cm.Set(annotationFieldClass, annotationObject);
							}
						}
						IList<object> list = new List<object>();
						IValue res = new Expressions.PrimitiveValue(Expressions.TypeList, list);
						foreach (ICoreMap cm_1 in cmList)
						{
							list.Add(cm_1.Get(annotationFieldClass));
						}
						return res;
					}
					else
					{
						throw new ArgumentException("Type mismatch on arg0: Cannot apply " + this + " to " + @in);
					}
				}
			}
		}

		public static readonly IValueFunction AnnotationFunction = new _NamedValueFunction_854("ANNOTATION_VALUE");

		private sealed class _NamedValueFunction_923 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_923(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "CoreMap or List<CoreMap>,String tag";
			}

			// First argument is what (CoreMap or List<CoreMap>) to tag
			// Second argument is tag
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for NULL
				if (@in[0] == null || (!(@in[0].Get() is ICoreMap) && !(@in[0].Get() is IList)))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public IValue GetTag(ICoreMap cm, string tag)
			{
				Tags tags = cm.Get(typeof(Tags.TagsAnnotation));
				return (tags != null) ? tags.GetTag(tag) : null;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				IValue v = @in[0];
				IValue res = null;
				string tag = (string)@in[1].Get();
				if (v.Get() is ICoreMap)
				{
					res = this.GetTag((ICoreMap)v.Get(), tag);
				}
				else
				{
					if (v.Get() is IList)
					{
						IList<ICoreMap> cmList = (IList<ICoreMap>)v.Get();
						IList<IValue> list = new List<IValue>();
						res = new Expressions.PrimitiveValue(Expressions.TypeList, list);
						foreach (ICoreMap cm in cmList)
						{
							list.Add(this.GetTag(cm, tag));
						}
					}
					else
					{
						throw new ArgumentException("Type mismatch on arg0: Cannot apply " + this + " to " + @in);
					}
				}
				return res;
			}
		}

		public static readonly IValueFunction GetAnnotationTagFunction = new _NamedValueFunction_923("GET_ANNOTATION_TAG");

		private sealed class _NamedValueFunction_975 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_975(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "CoreMap or List<CoreMap>,String tag,[Object value]";
			}

			// First argument is what (CoreMap or List<CoreMap>) to tag
			// Second argument is tag
			// Third argument is tag value
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || (!(@in[0].Get() is ICoreMap) && !(@in[0].Get() is IList)))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public void SetTag(ICoreMap cm, string tag, IValue tagValue)
			{
				Tags tags = cm.Get(typeof(Tags.TagsAnnotation));
				if (tags == null)
				{
					cm.Set(typeof(Tags.TagsAnnotation), tags = new Tags());
				}
				tags.SetTag(tag, tagValue);
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue v = @in[0];
				string tag = (string)@in[1].Get();
				IValue tagValue = (@in.Count >= 3) ? @in[2] : null;
				if (v.Get() is ICoreMap)
				{
					this.SetTag((ICoreMap)v.Get(), tag, tagValue);
				}
				else
				{
					if (v.Get() is IList)
					{
						IList<ICoreMap> cmList = (IList<ICoreMap>)v.Get();
						foreach (ICoreMap cm in cmList)
						{
							this.SetTag(cm, tag, tagValue);
						}
					}
					else
					{
						throw new ArgumentException("Type mismatch on arg0: Cannot apply " + this + " to " + @in);
					}
				}
				return v;
			}
		}

		public static readonly IValueFunction SetAnnotationTagFunction = new _NamedValueFunction_975("SET_ANNOTATION_TAG");

		private sealed class _NamedValueFunction_1027 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1027(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "CoreMap or List<CoreMap>,String tag";
			}

			// First argument is what (CoreMap) to tag
			// Second argument is tag
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					return false;
				}
				if (@in[0] == null || (!(@in[0].Get() is ICoreMap) && !(@in[0].Get() is IList)))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public void RemoveTag(ICoreMap cm, string tag)
			{
				Tags tags = cm.Get(typeof(Tags.TagsAnnotation));
				if (tags != null)
				{
					tags.RemoveTag(tag);
				}
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue v = @in[0];
				string tag = (string)@in[1].Get();
				if (v.Get() is ICoreMap)
				{
					this.RemoveTag((ICoreMap)v.Get(), tag);
				}
				else
				{
					if (v.Get() is IList)
					{
						IList<ICoreMap> cmList = (IList<ICoreMap>)v.Get();
						foreach (ICoreMap cm in cmList)
						{
							this.RemoveTag(cm, tag);
						}
					}
					else
					{
						throw new ArgumentException("Type mismatch on arg0: Cannot apply " + this + " to " + @in);
					}
				}
				return v;
			}
		}

		public static readonly IValueFunction RemoveAnnotationTagFunction = new _NamedValueFunction_1027("REMOVE_ANNOTATION_TAG");

		private sealed class _NamedValueFunction_1075 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1075(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return ValueFunctions.NamedValueFunction.GetTypeName(typeof(Tags)) + " tags,String field,[Object value]";
			}

			// First argument is tags object
			// Second argument is tag
			// Third argument (optional) is tag value
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is Tags))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue v = @in[0];
				Tags tags = (Tags)v.Get();
				string tag = (string)@in[1].Get();
				if (@in.Count >= 3)
				{
					IValue tagValue = @in[2];
					tags.SetTag(tag, tagValue);
				}
				return tags.GetTag(tag);
			}
		}

		public static readonly IValueFunction TagsValueFunction = new _NamedValueFunction_1075("TAGS_VALUE");

		private sealed class _NamedValueFunction_1110 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1110(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "Value,String tag,[Object value]";
			}

			// First argument is what to tag
			// Second argument is tag
			// Third argument is tag value
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null)
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue v = @in[0];
				Tags tags = v.GetTags();
				if (tags == null)
				{
					v.SetTags(tags = new Tags());
				}
				string tag = (string)@in[1].Get();
				IValue tagValue = (@in.Count >= 3) ? @in[2] : null;
				tags.SetTag(tag, tagValue);
				return v;
			}
		}

		public static readonly IValueFunction SetValueTagFunction = new _NamedValueFunction_1110("VALUE_TAG");

		private sealed class _NamedValueFunction_1146 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1146(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "Value,String tag";
			}

			// First argument is what to tag
			// Second argument is tag
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					return false;
				}
				if (@in[0] == null)
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue v = @in[0];
				Tags tags = v.GetTags();
				string tag = (string)@in[1].Get();
				return (tags != null) ? tags.GetTag(tag) : null;
			}
		}

		public static readonly IValueFunction GetValueTagFunction = new _NamedValueFunction_1146("GET_VALUE_TAG");

		private sealed class _NamedValueFunction_1177 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1177(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "Value,String tag";
			}

			// First argument is what to tag
			// Second argument is tag
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2)
				{
					return false;
				}
				if (@in[0] == null)
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue v = @in[0];
				Tags tags = v.GetTags();
				if (tags == null)
				{
					v.SetTags(tags = new Tags());
				}
				string tag = (string)@in[1].Get();
				tags.RemoveTag(tag);
				return v;
			}
		}

		public static readonly IValueFunction RemoveValueTagFunction = new _NamedValueFunction_1177("REMOVE_VALUE_TAG");

		private sealed class _NamedValueFunction_1211 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1211(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return Expressions.TypeComposite + " obj,String field,[Object value]";
			}

			// First argument is composite value
			// Second argument is field to select
			// Third argument (optional) is value to set composite value field to
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for null
				if (@in[0] == null || !(@in[0] is Expressions.CompositeValue))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				// Allow for null
				Expressions.CompositeValue v = (Expressions.CompositeValue)@in[0];
				string fieldName = (string)@in[1].Get();
				if (@in.Count >= 3)
				{
					v.Set(fieldName, @in[2]);
				}
				return v.GetValue(fieldName);
			}
		}

		public static readonly IValueFunction CompositeValueFunction = new _NamedValueFunction_1211("COMPOSITE_VALUE");

		private sealed class _NamedValueFunction_1246 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1246(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return Expressions.TypeComposite;
			}

			// First argument is composite value
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 1)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for null
				if (@in[0] == null || !(@in[0] is Expressions.CompositeValue))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				// Allow for null
				Expressions.CompositeValue v = (Expressions.CompositeValue)@in[0];
				IList<string> res = new List<string>(v.GetAttributes());
				return Expressions.CreateValue(Expressions.TypeList, res);
			}
		}

		public static readonly IValueFunction CompositeKeysFunction = new _NamedValueFunction_1246("COMPOSITE_KEYS");

		private sealed class _NamedValueFunction_1273 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1273(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "Object obj,String fieldName,[Object value]";
			}

			// First argument is object
			// Second argument is field to select
			// Third argument (optional) is value to assign to object field
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for null
				if (@in[0] == null || !(@in[0] is object))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				// Allow for null
				IValue v = @in[0];
				string fieldName = (string)@in[1].Get();
				try
				{
					object obj = v.Get();
					FieldInfo f = obj.GetType().GetField(fieldName);
					if (@in.Count >= 3)
					{
						IValue fieldValue = @in[2];
						if (fieldValue == null)
						{
							f.SetValue(obj, null);
						}
						else
						{
							if (f.GetType().IsAssignableFrom(typeof(IValue)))
							{
								f.SetValue(obj, fieldValue);
							}
							else
							{
								if (fieldValue.Get() == null)
								{
									f.SetValue(obj, null);
								}
								else
								{
									if (f.GetType().IsAssignableFrom(typeof(IList)))
									{
										if (fieldValue.Get() is IList)
										{
											IList list = (IList)fieldValue.Get();
											IType[] fieldParamTypes = ((IParameterizedType)f.GetGenericType()).GetActualTypeArguments();
											if (fieldParamTypes[0] is IValue)
											{
												IList<IValue> list2 = new List<IValue>(list.Count);
												foreach (object elem in list)
												{
													list2.Add(Expressions.AsValue(env, elem));
												}
												f.SetValue(obj, list2);
											}
											else
											{
												IList list2 = new ArrayList(list.Count);
												foreach (object elem in list)
												{
													if (elem is IValue)
													{
														list2.Add(((IValue)elem).Get());
													}
													else
													{
														list2.Add(elem);
													}
												}
												f.SetValue(obj, list2);
											}
										}
										else
										{
											f.SetValue(obj, Arrays.AsList(fieldValue.Get()));
										}
									}
									else
									{
										f.SetValue(obj, fieldValue.Get());
									}
								}
							}
						}
					}
					return Expressions.CreateValue(null, f.GetValue(obj));
				}
				catch (ReflectiveOperationException ex)
				{
					throw new Exception("Cannot get field " + fieldName + " from " + v, ex);
				}
			}
		}

		public static readonly IValueFunction ObjectFieldFunction = new _NamedValueFunction_1273("OBJECT_FIELD");

		private sealed class _NamedValueFunction_1350 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1350(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "List list,int index,[Object value]";
			}

			// First argument is List
			// Second argument is index of element to select
			// Third argument (optional) is value to assign list element
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for null
				if (@in[0] == null || !(@in[0].Get() is IList))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is int))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				// Allow for null
				IList list = (IList)@in[0].Get();
				int index = (int)@in[1].Get();
				if (index < 0)
				{
					index = list.Count + index;
				}
				if (index >= list.Count || index < 0)
				{
					// index out of bounds
					return null;
				}
				if (@in.Count >= 3)
				{
					IValue fieldValue = @in[2];
					if (fieldValue != null)
					{
						list.Set(index, fieldValue.Get());
					}
					else
					{
						list.Set(index, null);
					}
				}
				object obj = list[index];
				return Expressions.AsValue(env, obj);
			}
		}

		public static readonly IValueFunction ListValueFunction = new _NamedValueFunction_1350("LIST_VALUE");

		private sealed class _NamedValueFunction_1399 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1399(string baseArg1)
				: base(baseArg1)
			{
			}

			//      return Expressions.PrimitiveValue.create(null, obj);
			public override string GetParamDesc()
			{
				return "Map map,Object key,[Object value]";
			}

			// First argument is Map
			// Second argument is key of element to select
			// Third argument (optional) is value to assign to element
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 2 && @in.Count != 3)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for null
				if (@in[0] == null || !(@in[0].Get() is IDictionary))
				{
					return false;
				}
				if (@in[1] == null || !(@in[1].Get() is object))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				// Allow for null
				IDictionary map = (IDictionary)@in[0].Get();
				object key = @in[1].Get();
				if (@in.Count >= 3)
				{
					IValue fieldValue = @in[2];
					if (fieldValue != null)
					{
						map[key] = fieldValue.Get();
					}
					else
					{
						Sharpen.Collections.Remove(map, key);
					}
				}
				object obj = map[key];
				if (@in.Count == 2 && obj == null && key is string)
				{
					Type annotationFieldClass = null;
					annotationFieldClass = EnvLookup.LookupAnnotationKey(env, (string)key);
					if (annotationFieldClass != null)
					{
						obj = map[annotationFieldClass];
					}
				}
				return Expressions.AsValue(env, obj);
			}
		}

		public static readonly IValueFunction MapValueFunction = new _NamedValueFunction_1399("MAP_VALUE");

		private sealed class _NamedValueFunction_1448 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1448(string baseArg1)
				: base(baseArg1)
			{
			}

			//      return Expressions.PrimitiveValue.create(null, obj);
			public override string GetParamDesc()
			{
				return "Map";
			}

			// First argument is Map
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count != 1)
				{
					return false;
				}
				if (@in[0] == null || @in[0].Get() == null)
				{
					return true;
				}
				// Allow for null
				if (@in[0] == null || !(@in[0].Get() is IDictionary))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				if (@in[0] == null || @in[0].Get() == null)
				{
					return null;
				}
				// Allow for null
				IDictionary map = (IDictionary)@in[0].Get();
				IList<object> res = new ArrayList(map.Keys);
				return Expressions.CreateValue(Expressions.TypeList, res);
			}
		}

		public static readonly IValueFunction MapKeysFunction = new _NamedValueFunction_1448("MAP_KEYS");

		private sealed class _NamedValueFunction_1475 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1475(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "ValueFunction func,Object initialValue,...";
			}

			// First argument is function to apply
			// Second argument is initial value
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 2)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is IValueFunction))
				{
					return false;
				}
				if (@in[1] == null)
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValueFunction func = (IValueFunction)@in[0].Get();
				IValue res = @in[1];
				IList<IValue> args = new List<IValue>(2);
				for (int i = 2; i < @in.Count; i++)
				{
					args.Set(0, res);
					args.Set(1, @in[i]);
					res = func.Apply(env, args);
				}
				return res;
			}
		}

		public static readonly IValueFunction AggregateFunction = new _NamedValueFunction_1475("AGGREGATE");

		private sealed class _NamedValueFunction_1510 : ValueFunctions.NamedValueFunction
		{
			public _NamedValueFunction_1510(string baseArg1)
				: base(baseArg1)
			{
			}

			public override string GetParamDesc()
			{
				return "ValueFunction func or String funcname,...";
			}

			// First argument is function to apply
			public override bool CheckArgs(IList<IValue> @in)
			{
				if (@in.Count < 1)
				{
					return false;
				}
				if (@in[0] == null || !(@in[0].Get() is IValueFunction || @in[0].Get() is string))
				{
					return false;
				}
				return true;
			}

			public override IValue Apply(Env env, IList<IValue> @in)
			{
				IValue res;
				IList<IValue> args = new List<IValue>(@in.Count - 1);
				for (int i = 1; i < @in.Count; i++)
				{
					args.Add(@in[i]);
				}
				if (@in[0].Get() is IValueFunction)
				{
					IValueFunction func = (IValueFunction)@in[0].Get();
					res = func.Apply(env, args);
				}
				else
				{
					if (@in[0].Get() is string)
					{
						Expressions.FunctionCallExpression func = new Expressions.FunctionCallExpression((string)@in[0].Get(), args);
						res = func.Evaluate(env);
					}
					else
					{
						throw new ArgumentException("Type mismatch on arg0: Cannot apply " + this + " to " + @in);
					}
				}
				return res;
			}
		}

		public static readonly IValueFunction CallFunction = new _NamedValueFunction_1510("CALL");

		internal static readonly CollectionValuedMap<string, IValueFunction> registeredFunctions = new CollectionValuedMap<string, IValueFunction>(MapFactory.LinkedHashMapFactory<string, ICollection<IValueFunction>>(), CollectionFactory.ArrayListFactory
			<IValueFunction>(), false);

		static ValueFunctions()
		{
			registeredFunctions.Add("Add", AddFunction);
			registeredFunctions.Add("Subtract", SubtractFunction);
			registeredFunctions.Add("Multiply", MultiplyFunction);
			registeredFunctions.Add("Divide", DivideFunction);
			registeredFunctions.Add("Mod", ModFunction);
			registeredFunctions.Add("Min", MinFunction);
			registeredFunctions.Add("Max", MaxFunction);
			registeredFunctions.Add("Pow", PowFunction);
			registeredFunctions.Add("Negate", NegateFunction);
			registeredFunctions.Add("And", AndFunction);
			registeredFunctions.Add("Or", OrFunction);
			registeredFunctions.Add("Not", NotFunction);
			registeredFunctions.Add("Format", FormatFunction);
			registeredFunctions.Add("Concat", ConcatFunction);
			registeredFunctions.Add("Join", JoinFunction);
			registeredFunctions.Add("Lowercase", LowercaseFunction);
			registeredFunctions.Add("Uppercase", UppercaseFunction);
			registeredFunctions.Add("Map", MapValuesFunction);
			registeredFunctions.Add("Map", MapFunction);
			registeredFunctions.Add("Match", TokensMatchFunction);
			registeredFunctions.Add("Match", StringMatchFunction);
			registeredFunctions.Add("Replace", TokensReplaceFunction);
			registeredFunctions.Add("Replace", StringReplaceFunction);
			registeredFunctions.Add("GE", new ValueFunctions.CompareFunction<Number>("GE", NumberComparator, ValueFunctions.CompareType.Ge, typeof(Number)));
			registeredFunctions.Add("GT", new ValueFunctions.CompareFunction<Number>("GT", NumberComparator, ValueFunctions.CompareType.Gt, typeof(Number)));
			registeredFunctions.Add("LE", new ValueFunctions.CompareFunction<Number>("LE", NumberComparator, ValueFunctions.CompareType.Le, typeof(Number)));
			registeredFunctions.Add("LT", new ValueFunctions.CompareFunction<Number>("LT", NumberComparator, ValueFunctions.CompareType.Lt, typeof(Number)));
			registeredFunctions.Add("EQ", new ValueFunctions.CompareFunction<Number>("EQ", NumberComparator, ValueFunctions.CompareType.Eq, typeof(Number)));
			registeredFunctions.Add("NE", new ValueFunctions.CompareFunction<Number>("NE", NumberComparator, ValueFunctions.CompareType.Ne, typeof(Number)));
			registeredFunctions.Add("EQ", EqualsFunction);
			registeredFunctions.Add("NE", NotEqualsFunction);
			registeredFunctions.Add("VTag", SetValueTagFunction);
			registeredFunctions.Add("GetVTag", GetValueTagFunction);
			registeredFunctions.Add("RemoveVTag", RemoveValueTagFunction);
			registeredFunctions.Add("Tag", SetAnnotationTagFunction);
			registeredFunctions.Add("GetTag", GetAnnotationTagFunction);
			registeredFunctions.Add("RemoveTag", RemoveAnnotationTagFunction);
			registeredFunctions.Add("Split", TokenStringSplitFunction);
			registeredFunctions.Add("Annotate", AnnotationFunction);
			registeredFunctions.Add("Aggregate", AggregateFunction);
			registeredFunctions.Add("Call", CallFunction);
			registeredFunctions.Add("CreateRegex", CreateRegexFunction);
			registeredFunctions.Add("Select", CompositeValueFunction);
			registeredFunctions.Add("Select", MapValueFunction);
			registeredFunctions.Add("Select", TagsValueFunction);
			registeredFunctions.Add("Select", AnnotationFunction);
			registeredFunctions.Add("Select", ObjectFieldFunction);
			registeredFunctions.Add("ListSelect", ListValueFunction);
			registeredFunctions.Add("Keys", MapKeysFunction);
			registeredFunctions.Add("Keys", CompositeKeysFunction);
			registeredFunctions.Add("Set", TagsValueFunction);
			registeredFunctions.Add("Set", CompositeValueFunction);
			registeredFunctions.Add("Set", MapValueFunction);
			registeredFunctions.Add("Set", AnnotationFunction);
			registeredFunctions.Add("Set", ObjectFieldFunction);
			registeredFunctions.Add("Set", ListValueFunction);
			registeredFunctions.Add("Get", TagsValueFunction);
			registeredFunctions.Add("Get", CompositeValueFunction);
			registeredFunctions.Add("Get", MapValueFunction);
			registeredFunctions.Add("Get", AnnotationFunction);
			registeredFunctions.Add("Get", ObjectFieldFunction);
			registeredFunctions.Add("Get", ListValueFunction);
			// For debugging
			registeredFunctions.Add("Print", PrintFunction);
		}

		public static void Main(string[] args)
		{
			// Dumps the registered functions
			foreach (KeyValuePair<string, ICollection<IValueFunction>> entry in registeredFunctions)
			{
				foreach (IValueFunction vf in entry.Value)
				{
					System.Console.Out.WriteLine(entry.Key + ": " + vf);
				}
			}
		}
	}
}

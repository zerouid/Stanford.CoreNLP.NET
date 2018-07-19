using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>
	/// Various implementations of the Expression interface, which is
	/// used for specifying an "action" or "result" in TokensRegex extraction rules.
	/// </summary>
	/// <remarks>
	/// Various implementations of the Expression interface, which is
	/// used for specifying an "action" or "result" in TokensRegex extraction rules.
	/// Expressions are made up of identifiers, literals (numbers, strings "I'm a string", TRUE, FALSE),
	/// function calls ( FUNC(args) ).
	/// <p>
	/// After a pattern has been matched, we can access the capture groups using one of the following methods:
	/// <p>
	/// <table>
	/// <tr><th>Field</th><th>Description</th></tr>
	/// <tr><th colspan="2">Accessing captured groups as list of tokens</th></tr>
	/// <tr><td>$n</td><td>Capture group (as list of tokens) corresponding to the variable
	/// <c>$n</c>
	/// .
	/// If
	/// <c>n</c>
	/// is a integer, then the n-th captured group.  Capture group 0 is the entire matched expression.
	/// Otherwise, if
	/// <c>n</c>
	/// is a string, then the captured group with name
	/// <c>n</c>
	/// .</td></tr>
	/// <tr><td>$n[i]</td><td>The i-th token of the captured group
	/// <c>$n</c>
	/// .
	/// Use negative indices to count from the end of the list (e.g. -1 is the last token).</td></tr>
	/// <tr><td>$n[i].key</td><td>The value of annotation
	/// <c>key</c>
	/// of the i-th token of the captured group
	/// <c>$n</c>
	/// .</td></tr>
	/// <tr><th colspan="2">Accessing captured groups as MatchedGroupInfo</th></tr>
	/// <tr><td>$$n</td><td>Capture group (as MatchedGroupInfo) corresponding to the variable
	/// <c>$n</c>
	/// .
	/// Use to get the associated value of the group and any embedded capture groups.
	/// If
	/// <c>n</c>
	/// is a integer, then the n-th captured group.  Capture group 0 is the entire matched expression.
	/// Otherwise, if
	/// <c>n</c>
	/// is a string, then the captured group with name
	/// <c>n</c>
	/// .</td></tr>
	/// <tr><td>$$n.text</td><td>Text of the capture group
	/// <c>n</c>
	/// .</td></tr>
	/// <tr><td>$$n.nodes</td><td>Tokens of the capture group
	/// <c>n</c>
	/// (this is equivalent to
	/// <c>$n</c>
	/// ).</td></tr>
	/// <tr><td>$$n.value</td><td>Value associated with capture group
	/// <c>n</c>
	/// .</td></tr>
	/// <tr><td>$$n.matchResults</td><td>Additional match results associated with capture group
	/// <c>n</c>
	/// .
	/// Use to get embedded capture groups.  For instance, when the TokensRegex
	/// <c>/(\d\d)-(\d\d)/</c>
	/// is matched
	/// against the sentence "the score was 10-12",
	/// <c>$$0.text</c>
	/// will be "10-12" and
	/// <c>$$0.matchResults[0].word.group(1)</c>
	/// will be "10".</td></tr>
	/// </table>
	/// <p>
	/// The following functions are supported:
	/// <table>
	/// <tr><th>Function</th><th>Description</th></tr>
	/// <tr><td>
	/// <c>Annotate(CoreMap, field, value)</c>
	/// </td><td>Annotates the CoreMap with specified field=value</td></tr>
	/// <tr><td>
	/// <c>Aggregate(function, initialValue,...)</c>
	/// </td><td>Aggregates values using function (like fold)</td></tr>
	/// <tr><td>
	/// <c>Split(CoreMap, delimRegex, includeMatched)</c>
	/// </td><td>Split one CoreMap into smaller coremaps using the specified delimRegex on the text of the CoreMap.
	/// If includeMatched is true, pieces that matches the delimRegex are included in the final list of CoreMaps</td></tr>
	/// <tr><th colspan="2">Tagging functions</th></tr>
	/// <tr><td>
	/// <c>Tag(CoreMap or List&lt;CoreMap&gt;, tag, value)&lt;br&gt;VTag(Value,tag,value)</c>
	/// </td><td>Sets a temporary tag on the CoreMap(s) or Value</td></tr>
	/// <tr><td>
	/// <c>GetTag(CoreMap or List&lt;CoreMap&gt;, tag)&lt;br&gt;GetVTag(Value,tag)</c>
	/// </td><td>Returns the temporary tag on the CoreMap(s) or Value</td></tr>
	/// <tr><td>
	/// <c>RemoveTag(CoreMap or List&lt;CoreMap&gt;, tag)&lt;br&gt;RemoveVTag(Value,tag)</c>
	/// </td><td>Removes the temporary tag on the CoreMap(s) or Value</td></tr>
	/// <tr><th colspan="2">Regex functions</th></tr>
	/// <tr><td>
	/// <c>Match(List&lt;CoreMap&gt;, tokensregex)&lt;br&gt;Match(String,regex)</c>
	/// </td><td>Returns whether the tokens or text matched</td></tr>
	/// <tr><td>
	/// <c>Replace(List&lt;CoreMap&gt;, tokensregex, replacement)&lt;br&gt;Match(String,regex,replacement)</c>
	/// </td><td>Replaces the matched tokens or text</td></tr>
	/// <tr><td>
	/// <c>CreateRegex(List&lt;String&gt;)</c>
	/// </td><td>Creates one big string regular expression that matches any of the strings in the list</td></tr>
	/// <tr><th colspan="2">Accessor functions</th></tr>
	/// <tr><td>
	/// <c>Map(list,function)</c>
	/// </td><td>Returns a new list that is the result of applying the function on every element of the List</td></tr>
	/// <tr><td>
	/// <c>Keys(map)</c>
	/// </td><td>Returns list of keys for the given map</td></tr>
	/// <tr><td>&lt;
	/// <c>Set(object or map, fieldname, value)</c>
	/// <br />
	/// <c>Set(list,index,value)</c>
	/// }</td><td>Set the field to the specified value</td></tr>
	/// <tr><td>
	/// <c>Get(object or map, fieldname) or object.fieldname &lt;br&gt;Get(list,index) or list[index]</c>
	/// </td><td>Returns the value of the specified field</td></tr>
	/// <tr><th colspan="2">String functions</th></tr>
	/// <tr><td>
	/// <c>Format(format,arg1,arg2,...)</c>
	/// </td><td>Returns formatted string</td></tr>
	/// <tr><td>
	/// <c>Concat(str1,str2,...)</c>
	/// </td><td>Returns strings concatenated together</td></tr>
	/// <tr><td>
	/// <c>Join(glue,str1,str2,...)</c>
	/// </td><td>Returns strings concatenated together with glue in the middle</td></tr>
	/// <tr><td>
	/// <c>Lowercase(str)</c>
	/// </td><td>Returns the lowercase form of the string</td></tr>
	/// <tr><td>
	/// <c>Uppercase(str)</c>
	/// </td><td>Returns the uppercase form of the string</td></tr>
	/// <tr><th colspan="2">Numeric functions</th></tr>
	/// <tr><td>
	/// <c>Subtract(X,Y)</c>
	/// </td><td>Returns
	/// <c>X-Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Add(X,Y)</c>
	/// </td><td>Returns
	/// <c>X+Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Subtract(X,Y)</c>
	/// </td><td>Returns
	/// <c>X-Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Multiply(X,Y)</c>
	/// </td><td>Returns
	/// <c>X*Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Divide(X,Y)</c>
	/// </td><td>Returns
	/// <c>X/Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Mod(X,Y)</c>
	/// </td><td>Returns
	/// <c>X%Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Negate(X)</c>
	/// </td><td>Returns
	/// <c>-X</c>
	/// </td></tr>
	/// <tr><th colspan="2">Boolean functions</th></tr>
	/// <tr><td>
	/// <c>And(X,Y)</c>
	/// </td><td>Returns
	/// <c>X&&Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Or(X,Y)</c>
	/// </td><td>Returns
	/// <c>X||Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>Not(X)</c>
	/// </td><td>Returns
	/// <c>!X</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>GE(X,Y) or X &gt;= Y</c>
	/// </td><td>Returns
	/// <c>X &gt;= Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>GT(X,Y) or X &gt; Y</c>
	/// </td><td>Returns
	/// <c>X &gt; Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>LE(X,Y) or X &lt;= Y</c>
	/// </td><td>Returns
	/// <c>X &lt;= Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>LT(X,Y) or X &lt; Y</c>
	/// </td><td>Returns
	/// <c>X &lt; Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>EQ(X,Y) or X == Y</c>
	/// </td><td>Returns
	/// <c>X == Y</c>
	/// </td></tr>
	/// <tr><td>
	/// <c>NE(X,Y) or X != Y</c>
	/// </td><td>Returns
	/// <c>X != Y</c>
	/// </td></tr>
	/// </table>
	/// </remarks>
	/// <author>Angel Chang</author>
	public class Expressions
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions));

		/// <summary>VAR - Variable</summary>
		public const string TypeVar = "VAR";

		/// <summary>
		/// FUNCTION - (input)
		/// <literal>=&gt;</literal>
		/// (output) where input is a list of Values, and output is a single Value
		/// </summary>
		public const string TypeFunction = "FUNCTION";

		/// <summary>REGEX - Regular expression pattern (for tokens or string)</summary>
		public const string TypeRegex = "REGEX";

		public const string TypeStringRegex = "STRING_REGEX";

		public const string TypeTokenRegex = "TOKEN_REGEX";

		/// <summary>REGEXMATCHVAR - Variable that refers to variable resulting from a regex match or used in a regex match (starts with $)</summary>
		public const string TypeRegexmatchvar = "REGEXMATCHVAR";

		/// <summary>STRING - String</summary>
		public const string TypeString = "STRING";

		/// <summary>NUMBER - Numeric value (can be integer or real)</summary>
		public const string TypeNumber = "NUMBER";

		/// <summary>COMPOSITE - Composite value with field names and field values</summary>
		public const string TypeComposite = "COMPOSITE";

		/// <summary>LIST - List</summary>
		public const string TypeList = "LIST";

		public const string TypeSet = "SET";

		public const string TypeAnnotationKey = "ANNOKEY";

		/// <summary>CLASS - Maps to a Java class</summary>
		public const string TypeClass = "CLASS";

		public const string TypeTokens = "TOKENS";

		public const string TypeBoolean = "BOOLEAN";

		public const string VarSelf = "_";

		public static readonly IValue<bool> True = new Expressions.PrimitiveValue<bool>(Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions.TypeBoolean, true);

		public static readonly IValue<bool> False = new Expressions.PrimitiveValue<bool>(Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions.TypeBoolean, false);

		public static readonly IValue Nil = new Expressions.PrimitiveValue("NIL", null);

		private Expressions()
		{
		}

		// static methods and classes
		public static bool ConvertValueToBoolean(IValue v, bool keepNull)
		{
			bool res = null;
			if (v != null)
			{
				object obj = v.Get();
				if (obj != null)
				{
					if (obj is bool)
					{
						res = ((bool)obj);
					}
					else
					{
						if (obj is int)
						{
							res = (((int)obj) != 0);
						}
						else
						{
							res = true;
						}
					}
					return res;
				}
			}
			return (keepNull) ? res : false;
		}

		public static IValue<bool> ConvertValueToBooleanValue(IValue v, bool keepNull)
		{
			if (v != null)
			{
				object obj = v.Get();
				if (obj is bool)
				{
					return (IValue<bool>)v;
				}
				else
				{
					return new Expressions.PrimitiveValue<bool>(Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions.TypeBoolean, ConvertValueToBoolean(v, keepNull));
				}
			}
			else
			{
				return keepNull ? null : False;
			}
		}

		public static C AsObject<C>(Env env, object v)
		{
			if (v is IExpression)
			{
				return (C)((IExpression)v).Evaluate(env).Get();
			}
			else
			{
				return (C)v;
			}
		}

		public static IExpression AsExpression(Env env, object v)
		{
			if (v is IExpression)
			{
				return (IExpression)v;
			}
			else
			{
				return CreateValue(null, v);
			}
		}

		public static IValue AsValue(Env env, object v)
		{
			if (v is IValue)
			{
				return (IValue)v;
			}
			else
			{
				return CreateValue(null, v);
			}
		}

		public static IValue CreateValue<T>(string typename, T value, params string[] tags)
		{
			if (value is IValue)
			{
				return (IValue)value;
			}
			else
			{
				if (typename == null && value != null)
				{
					// TODO: Check for simpler typename provided by value
					typename = value.GetType().FullName;
				}
				return new Expressions.PrimitiveValue<T>(typename, value, tags);
			}
		}

		/// <summary>An expression that is a wrapper around another expression.</summary>
		public abstract class WrappedExpression : IExpression
		{
			protected internal IExpression expr;

			public virtual Tags GetTags()
			{
				return expr.GetTags();
			}

			public virtual void SetTags(Tags tags)
			{
				expr.SetTags(tags);
			}

			public virtual string GetType()
			{
				return expr.GetType();
			}

			public virtual IExpression Simplify(Env env)
			{
				return expr.Simplify(env);
			}

			public virtual bool HasValue()
			{
				return expr.HasValue();
			}

			public virtual IValue Evaluate(Env env, params object[] args)
			{
				return expr.Evaluate(env, args);
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.WrappedExpression))
				{
					return false;
				}
				Expressions.WrappedExpression that = (Expressions.WrappedExpression)o;
				if (expr != null ? !expr.Equals(that.expr) : that.expr != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				return expr != null ? expr.GetHashCode() : 0;
			}
		}

		/// <summary>An expression with a typename and tags.</summary>
		[System.Serializable]
		public abstract class TypedExpression : IExpression
		{
			internal string typename;

			internal Tags tags;

			public TypedExpression(string typename, params string[] tags)
			{
				this.typename = typename;
				if (tags != null)
				{
					this.tags = new Tags(tags);
				}
			}

			public virtual Tags GetTags()
			{
				return tags;
			}

			public virtual void SetTags(Tags tags)
			{
				this.tags = tags;
			}

			public virtual string GetType()
			{
				return typename;
			}

			public virtual IExpression Simplify(Env env)
			{
				return this;
			}

			public virtual bool HasValue()
			{
				return false;
			}

			private const long serialVersionUID = 2;

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.TypedExpression))
				{
					return false;
				}
				Expressions.TypedExpression that = (Expressions.TypedExpression)o;
				if (tags != null ? !tags.Equals(that.tags) : that.tags != null)
				{
					return false;
				}
				if (typename != null ? !typename.Equals(that.typename) : that.typename != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = typename != null ? typename.GetHashCode() : 0;
				result = 31 * result + (tags != null ? tags.GetHashCode() : 0);
				return result;
			}

			public abstract IValue Evaluate(Env arg1, object[] arg2);
		}

		/// <summary>A simple implementation of an expression that is represented by a java object of type T</summary>
		/// <?/>
		[System.Serializable]
		public abstract class SimpleExpression<T> : Expressions.TypedExpression
		{
			internal T value;

			protected internal SimpleExpression(string typename, T value, params string[] tags)
				: base(typename, tags)
			{
				this.value = value;
			}

			public virtual T Get()
			{
				return value;
			}

			public override string ToString()
			{
				return GetType() + "(" + value + ")";
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.SimpleExpression))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Expressions.SimpleExpression that = (Expressions.SimpleExpression)o;
				if (value != null ? !value.Equals(that.value) : that.value != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 31 * result + (value != null ? value.GetHashCode() : 0);
				return result;
			}
		}

		/// <summary>
		/// A simple implementation of an expression that is represented by a java object of type T
		/// and which also has a cached Value stored with it
		/// </summary>
		/// <?/>
		[System.Serializable]
		public class SimpleCachedExpression<T> : Expressions.SimpleExpression<T>
		{
			internal IValue evaluated;

			internal bool disableCaching = false;

			protected internal SimpleCachedExpression(string typename, T value, params string[] tags)
				: base(typename, value, tags)
			{
			}

			protected internal virtual IValue DoEvaluation(Env env, params object[] args)
			{
				throw new NotSupportedException("Cannot evaluate type: " + typename);
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				if (args != null)
				{
					return DoEvaluation(env, args);
				}
				if (evaluated == null || disableCaching)
				{
					evaluated = DoEvaluation(env, args);
				}
				return evaluated;
			}

			public override bool HasValue()
			{
				return (evaluated != null);
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.SimpleCachedExpression))
				{
					return false;
				}
				Expressions.SimpleCachedExpression that = (Expressions.SimpleCachedExpression)o;
				if (disableCaching != that.disableCaching)
				{
					return false;
				}
				if (evaluated != null ? !evaluated.Equals(that.evaluated) : that.evaluated != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = evaluated != null ? evaluated.GetHashCode() : 0;
				result = 31 * result + (disableCaching ? 1 : 0);
				return result;
			}
		}

		/// <summary>Simple implementation of Value backed by a java object of type T</summary>
		/// <?/>
		[System.Serializable]
		public class SimpleValue<T> : Expressions.TypedExpression, IValue<T>
		{
			internal T value;

			protected internal SimpleValue(string typename, T value, params string[] tags)
				: base(typename, tags)
			{
				this.value = value;
			}

			public virtual T Get()
			{
				return value;
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				return this;
			}

			public override string ToString()
			{
				return GetType() + "(" + value + ")";
			}

			public override bool HasValue()
			{
				return true;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.SimpleValue))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Expressions.SimpleValue that = (Expressions.SimpleValue)o;
				if (value != null ? !value.Equals(that.value) : that.value != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 31 * result + (value != null ? value.GetHashCode() : 0);
				return result;
			}
		}

		/// <summary>A string that represents a regular expression</summary>
		[System.Serializable]
		public class RegexValue : Expressions.SimpleValue<string>
		{
			public RegexValue(string regex, params string[] tags)
				: base(TypeRegex, regex, tags)
			{
			}
		}

		/// <summary>A variable assignment with the name of the variable, and the expression to assign to that variable</summary>
		[System.Serializable]
		public class VarAssignmentExpression : Expressions.TypedExpression
		{
			internal readonly string varName;

			internal readonly IExpression valueExpr;

			internal readonly bool bindAsValue;

			public VarAssignmentExpression(string varName, IExpression valueExpr, bool bindAsValue)
				: base("VAR_ASSIGNMENT")
			{
				this.varName = varName;
				this.valueExpr = valueExpr;
				this.bindAsValue = bindAsValue;
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				IValue value = valueExpr.Evaluate(env, args);
				if (args != null)
				{
					if (args.Length == 1 && args[0] is ICoreMap)
					{
						ICoreMap cm = (ICoreMap)args[0];
						Type annotationKey = EnvLookup.LookupAnnotationKey(env, varName);
						if (annotationKey != null)
						{
							cm.Set(annotationKey, (value != null) ? value.Get() : null);
							return value;
						}
					}
				}
				if (bindAsValue)
				{
					env.Bind(varName, value);
				}
				else
				{
					env.Bind(varName, (value != null) ? value.Get() : null);
					if (TypeRegex == value.GetType())
					{
						try
						{
							object vobj = value.Get();
							if (vobj is string)
							{
								env.BindStringRegex(varName, (string)vobj);
							}
							else
							{
								if (vobj is Pattern)
								{
									env.BindStringRegex(varName, ((Pattern)vobj).Pattern());
								}
							}
						}
						catch (Exception)
						{
						}
					}
				}
				return value;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.VarAssignmentExpression))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Expressions.VarAssignmentExpression that = (Expressions.VarAssignmentExpression)o;
				if (bindAsValue != that.bindAsValue)
				{
					return false;
				}
				if (valueExpr != null ? !valueExpr.Equals(that.valueExpr) : that.valueExpr != null)
				{
					return false;
				}
				if (varName != null ? !varName.Equals(that.varName) : that.varName != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 31 * result + (varName != null ? varName.GetHashCode() : 0);
				result = 31 * result + (valueExpr != null ? valueExpr.GetHashCode() : 0);
				result = 31 * result + (bindAsValue ? 1 : 0);
				return result;
			}
		}

		/// <summary>A variable, which can be assigned any expression.</summary>
		/// <remarks>
		/// A variable, which can be assigned any expression.
		/// When evaluated, the value of the variable is retrieved from the
		/// environment, evaluated, and returned.
		/// </remarks>
		[System.Serializable]
		public class VarExpression : Expressions.SimpleExpression<string>, IAssignableExpression
		{
			public VarExpression(string varname, params string[] tags)
				: base(TypeVar, varname, tags)
			{
			}

			// end class VarAssignmentExpression
			public override IValue Evaluate(Env env, params object[] args)
			{
				IExpression exp = null;
				string varName = value;
				if (args != null)
				{
					if (args.Length == 1 && args[0] is ICoreMap)
					{
						ICoreMap cm = (ICoreMap)args[0];
						if (VarSelf.Equals(varName))
						{
							return CreateValue(varName, cm);
						}
						Type annotationKey = EnvLookup.LookupAnnotationKey(env, varName);
						if (annotationKey != null)
						{
							return CreateValue(varName, cm.Get(annotationKey));
						}
					}
				}
				if (VarSelf.Equals(varName))
				{
					return CreateValue(varName, env.Peek(varName));
				}
				object obj = env.Get(varName);
				if (obj != null)
				{
					exp = AsExpression(env, obj);
				}
				IValue v = exp != null ? exp.Evaluate(env, args) : null;
				if (v == null)
				{
					log.Info("Unknown variable: " + varName);
				}
				return v;
			}

			public virtual IExpression Assign(IExpression expr)
			{
				return new Expressions.VarAssignmentExpression(value, expr, true);
			}
		}

		/// <summary>A variable that represents a regular expression match result.</summary>
		/// <remarks>
		/// A variable that represents a regular expression match result.
		/// The match result is identified either by the group id (Integer) or
		/// the group name (String).
		/// When evaluated, one argument (the MatchResult or SequenceMatchResult) must be supplied.
		/// Depending on the match result supplied, the returned value
		/// is either a String (for MatchResult) or a list of tokens (for SequenceMatchResult).
		/// </remarks>
		private static readonly Pattern DigitsPattern = Pattern.Compile("\\d+");

		[System.Serializable]
		public class RegexMatchVarExpression : Expressions.SimpleExpression, IAssignableExpression
		{
			public RegexMatchVarExpression(string groupname, params string[] tags)
				: base(TypeRegexmatchvar, groupname, tags)
			{
			}

			public RegexMatchVarExpression(int groupid, params string[] tags)
				: base(TypeRegexmatchvar, groupid, tags)
			{
			}

			public static Expressions.RegexMatchVarExpression ValueOf(string group)
			{
				if (DigitsPattern.Matcher(group).Matches())
				{
					int n = int.Parse(group);
					return new Expressions.RegexMatchVarExpression(n);
				}
				else
				{
					return new Expressions.RegexMatchVarExpression(group);
				}
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				if (args != null && args.Length > 0)
				{
					if (args[0] is ISequenceMatchResult)
					{
						ISequenceMatchResult mr = (ISequenceMatchResult)args[0];
						object v = Get();
						if (v is string)
						{
							// TODO: depending if TYPE_STRING, use string version...
							return new Expressions.PrimitiveValue<IList>(TypeTokens, mr.GroupNodes((string)v));
						}
						else
						{
							if (v is int)
							{
								return new Expressions.PrimitiveValue<IList>(TypeTokens, mr.GroupNodes((int)v));
							}
							else
							{
								throw new NotSupportedException("String match result must be referred to by group id");
							}
						}
					}
					else
					{
						if (args[0] is IMatchResult)
						{
							IMatchResult mr = (IMatchResult)args[0];
							object v = Get();
							if (v is int)
							{
								string str = mr.Group((int)Get());
								return new Expressions.PrimitiveValue<string>(TypeString, str);
							}
							else
							{
								throw new NotSupportedException("String match result must be referred to by group id");
							}
						}
					}
				}
				return null;
			}

			public virtual IExpression Assign(IExpression expr)
			{
				return new Expressions.VarAssignmentExpression(value.ToString(), expr, false);
			}
		}

		[System.Serializable]
		public class RegexMatchResultVarExpression : Expressions.SimpleExpression
		{
			public RegexMatchResultVarExpression(string groupname, params string[] tags)
				: base(TypeRegexmatchvar, groupname, tags)
			{
			}

			public RegexMatchResultVarExpression(int groupid, params string[] tags)
				: base(TypeRegexmatchvar, groupid, tags)
			{
			}

			public static Expressions.RegexMatchResultVarExpression ValueOf(string group)
			{
				if (DigitsPattern.Matcher(group).Matches())
				{
					int n = int.Parse(group);
					return new Expressions.RegexMatchResultVarExpression(n);
				}
				else
				{
					return new Expressions.RegexMatchResultVarExpression(group);
				}
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				if (args != null && args.Length > 0)
				{
					if (args[0] is ISequenceMatchResult)
					{
						ISequenceMatchResult mr = (ISequenceMatchResult)args[0];
						object v = Get();
						if (v is string)
						{
							return new Expressions.PrimitiveValue("MATCHED_GROUP_INFO", mr.GroupInfo((string)v));
						}
						else
						{
							if (v is int)
							{
								return new Expressions.PrimitiveValue("MATCHED_GROUP_INFO", mr.GroupInfo((int)v));
							}
							else
							{
								throw new NotSupportedException("String match result must be referred to by group id");
							}
						}
					}
				}
				return null;
			}
		}

		/// <summary>A function call that can be assigned a value.</summary>
		[System.Serializable]
		public class AssignableFunctionCallExpression : Expressions.FunctionCallExpression, IAssignableExpression
		{
			public AssignableFunctionCallExpression(string function, IList<IExpression> @params, params string[] tags)
				: base(function, @params, tags)
			{
			}

			public virtual IExpression Assign(IExpression expr)
			{
				IList<IExpression> newParams = new List<IExpression>(@params);
				newParams.Add(expr);
				IExpression res = new Expressions.FunctionCallExpression(function, newParams);
				res.SetTags(tags);
				return res;
			}
		}

		[System.Serializable]
		public class IndexedExpression : Expressions.AssignableFunctionCallExpression
		{
			public IndexedExpression(IExpression expr, int index)
				: base("ListSelect", Arrays.AsList(expr, new Expressions.PrimitiveValue("Integer", index)))
			{
			}
		}

		[System.Serializable]
		public class FieldExpression : Expressions.AssignableFunctionCallExpression
		{
			public FieldExpression(IExpression expr, string field)
				: base("Select", Arrays.AsList(expr, new Expressions.PrimitiveValue(TypeString, field)))
			{
			}

			public FieldExpression(IExpression expr, IExpression field)
				: base("Select", Arrays.AsList(expr, field))
			{
			}
		}

		[System.Serializable]
		public class OrExpression : Expressions.FunctionCallExpression
		{
			public OrExpression(IList<IExpression> children)
				: base("Or", children)
			{
			}
		}

		[System.Serializable]
		public class AndExpression : Expressions.FunctionCallExpression
		{
			public AndExpression(IList<IExpression> children)
				: base("And", children)
			{
			}
		}

		[System.Serializable]
		public class NotExpression : Expressions.FunctionCallExpression
		{
			public NotExpression(IExpression expr)
				: base("Not", Arrays.AsList(expr))
			{
			}
		}

		[System.Serializable]
		public class IfExpression : Expressions.TypedExpression
		{
			internal IExpression condExpr;

			internal IExpression trueExpr;

			internal IExpression falseExpr;

			public IfExpression(IExpression cond, IExpression vt, IExpression vf)
				: base("If")
			{
				this.condExpr = cond;
				this.trueExpr = vt;
				this.falseExpr = vf;
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				IValue condValue = condExpr.Evaluate(env, args);
				bool cond = (bool)condValue.Get();
				if (cond)
				{
					return trueExpr.Evaluate(env, args);
				}
				else
				{
					return falseExpr.Evaluate(env, args);
				}
			}
		}

		public class CaseExpression : Expressions.WrappedExpression
		{
			public CaseExpression(IList<Pair<IExpression, IExpression>> conds, IExpression elseExpr)
			{
				if (conds.Count == 0)
				{
					throw new ArgumentException("No conditions!");
				}
				else
				{
					expr = elseExpr;
					for (int i = conds.Count - 1; i >= 0; i--)
					{
						Pair<IExpression, IExpression> p = conds[i];
						expr = new Expressions.IfExpression(p.First(), p.Second(), expr);
					}
				}
			}
		}

		public class ConditionalExpression : Expressions.WrappedExpression
		{
			public ConditionalExpression(IExpression expr)
			{
				this.expr = expr;
			}

			public ConditionalExpression(string op, IExpression expr1, IExpression expr2)
			{
				switch (op)
				{
					case ">=":
					{
						expr = new Expressions.FunctionCallExpression("GE", Arrays.AsList(expr1, expr2));
						break;
					}

					case "<=":
					{
						expr = new Expressions.FunctionCallExpression("LE", Arrays.AsList(expr1, expr2));
						break;
					}

					case ">":
					{
						expr = new Expressions.FunctionCallExpression("GT", Arrays.AsList(expr1, expr2));
						break;
					}

					case "<":
					{
						expr = new Expressions.FunctionCallExpression("LT", Arrays.AsList(expr1, expr2));
						break;
					}

					case "==":
					{
						expr = new Expressions.FunctionCallExpression("EQ", Arrays.AsList(expr1, expr2));
						break;
					}

					case "!=":
					{
						expr = new Expressions.FunctionCallExpression("NE", Arrays.AsList(expr1, expr2));
						break;
					}

					case "=~":
					{
						expr = new Expressions.FunctionCallExpression("Match", Arrays.AsList(expr1, expr2));
						break;
					}

					case "!~":
					{
						expr = new Expressions.NotExpression(new Expressions.FunctionCallExpression("Match", Arrays.AsList(expr1, expr2)));
						break;
					}
				}
			}

			public override string GetType()
			{
				return Expressions.TypeBoolean;
			}

			public override IExpression Simplify(Env env)
			{
				return this;
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				IValue v = expr.Evaluate(env, args);
				return ConvertValueToBooleanValue(v, false);
			}
		}

		[System.Serializable]
		public class ListExpression : Expressions.TypedExpression
		{
			internal IList<IExpression> exprs;

			public ListExpression(string typename, params string[] tags)
				: base(typename, tags)
			{
				this.exprs = new List<IExpression>();
			}

			public ListExpression(string typename, IList<IExpression> exprs, params string[] tags)
				: base(typename, tags)
			{
				this.exprs = new List<IExpression>(exprs);
			}

			public virtual void AddAll(IList<IExpression> exprs)
			{
				if (exprs != null)
				{
					Sharpen.Collections.AddAll(this.exprs, exprs);
				}
			}

			public virtual void Add(IExpression expr)
			{
				this.exprs.Add(expr);
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				IList<IValue> values = new List<IValue>(exprs.Count);
				foreach (IExpression s in exprs)
				{
					values.Add(s.Evaluate(env, args));
				}
				return new Expressions.PrimitiveValue<IList<IValue>>(typename, values);
			}
		}

		private static bool IsArgTypesCompatible(Type[] paramTypes, Type[] targetParamTypes)
		{
			bool compatible = true;
			if (targetParamTypes.Length == paramTypes.Length)
			{
				for (int i = 0; i < targetParamTypes.Length; i++)
				{
					if (targetParamTypes[i].IsPrimitive)
					{
						compatible = false;
						if (paramTypes[i] != null)
						{
							try
							{
								Type type = (Type)paramTypes[i].GetField("TYPE").GetValue(null);
								if (type.Equals(targetParamTypes[i]))
								{
									compatible = true;
								}
							}
							catch (ReflectiveOperationException)
							{
							}
						}
						if (!compatible)
						{
							break;
						}
					}
					else
					{
						if (paramTypes[i] != null && !targetParamTypes[i].IsAssignableFrom(paramTypes[i]))
						{
							compatible = false;
							break;
						}
					}
				}
			}
			else
			{
				compatible = false;
			}
			return compatible;
		}

		protected internal static readonly string Newline = Runtime.GetProperty("line.separator");

		[System.Serializable]
		public class FunctionCallExpression : Expressions.TypedExpression
		{
			internal readonly string function;

			internal readonly IList<IExpression> @params;

			public FunctionCallExpression(string function, IList<IExpression> @params, params string[] tags)
				: base(TypeFunction, tags)
			{
				this.function = function;
				this.@params = @params;
			}

			public override string ToString()
			{
				return function + '(' + StringUtils.Join(@params, ", ") + ')';
			}

			public override IExpression Simplify(Env env)
			{
				bool paramsAllHasValue = true;
				IList<IExpression> simplifiedParams = new List<IExpression>(@params.Count);
				foreach (IExpression param in @params)
				{
					IExpression simplified = param.Simplify(env);
					simplifiedParams.Add(simplified);
					if (!(simplified.HasValue()))
					{
						paramsAllHasValue = false;
					}
				}
				IExpression res = new Expressions.FunctionCallExpression(function, simplifiedParams);
				if (paramsAllHasValue)
				{
					return res.Evaluate(env);
				}
				else
				{
					return res;
				}
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				object funcValue = ValueFunctions.LookupFunctionObject(env, function);
				if (funcValue == null)
				{
					throw new Exception("Unknown function " + function);
				}
				if (funcValue is IValue)
				{
					funcValue = ((IValue)funcValue).Evaluate(env, args).Get();
				}
				if (funcValue is IValueFunction)
				{
					IValueFunction f = (IValueFunction)funcValue;
					IList<IValue> evaled = new List<IValue>();
					foreach (IExpression param in @params)
					{
						evaled.Add(param.Evaluate(env, args));
					}
					return f.Apply(env, evaled);
				}
				else
				{
					if (funcValue is ICollection)
					{
						IList<IValue> evaled = new List<IValue>();
						foreach (IExpression param in @params)
						{
							evaled.Add(param.Evaluate(env, args));
						}
						ICollection<IValueFunction> fs = (ICollection<IValueFunction>)funcValue;
						foreach (IValueFunction f in fs)
						{
							if (f.CheckArgs(evaled))
							{
								return f.Apply(env, evaled);
							}
						}
						StringBuilder sb = new StringBuilder();
						sb.Append("Cannot find function matching args: " + function + Newline);
						sb.Append("Args are: " + StringUtils.Join(evaled, ",") + Newline);
						if (fs.Count > 0)
						{
							sb.Append("Options are:\n" + StringUtils.Join(fs, Newline));
						}
						else
						{
							sb.Append("No options");
						}
						throw new Exception(sb.ToString());
					}
					else
					{
						if (funcValue is Type)
						{
							Type c = (Type)funcValue;
							IList<IValue> evaled = new List<IValue>();
							foreach (IExpression param in @params)
							{
								evaled.Add(param.Evaluate(env, args));
							}
							Type[] paramTypes = new Type[@params.Count];
							object[] objs = new object[@params.Count];
							bool paramsNotNull = true;
							for (int i = 0; i < @params.Count; i++)
							{
								IValue v = evaled[i];
								if (v != null)
								{
									objs[i] = v.Get();
									if (objs[i] != null)
									{
										paramTypes[i] = objs[i].GetType();
									}
									else
									{
										paramTypes[i] = null;
										paramsNotNull = false;
									}
								}
								else
								{
									objs[i] = null;
									paramTypes[i] = null;
									paramsNotNull = false;
								}
							}
							//throw new RuntimeException("Missing evaluated value for " + params.get(i));
							if (paramsNotNull)
							{
								object obj = MetaClass.Create(c).CreateInstance(objs);
								if (obj != null)
								{
									return new Expressions.PrimitiveValue<object>(function, obj);
								}
							}
							try
							{
								ConstructorInfo constructor = null;
								try
								{
									constructor = c.GetConstructor(paramTypes);
								}
								catch (MissingMethodException ex)
								{
									ConstructorInfo[] constructors = c.GetConstructors();
									foreach (ConstructorInfo cons in constructors)
									{
										Type[] consParamTypes = ((Type[])cons.GetParameterTypes());
										bool compatible = IsArgTypesCompatible(paramTypes, consParamTypes);
										if (compatible)
										{
											constructor = cons;
											break;
										}
									}
									if (constructor == null)
									{
										throw new Exception("Cannot instantiate " + c, ex);
									}
								}
								object obj = constructor.NewInstance(objs);
								return new Expressions.PrimitiveValue<object>(function, obj);
							}
							catch (ReflectiveOperationException ex)
							{
								throw new Exception("Cannot instantiate " + c, ex);
							}
						}
						else
						{
							throw new NotSupportedException("Unsupported function value " + funcValue);
						}
					}
				}
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.FunctionCallExpression))
				{
					return false;
				}
				Expressions.FunctionCallExpression that = (Expressions.FunctionCallExpression)o;
				if (function != null ? !function.Equals(that.function) : that.function != null)
				{
					return false;
				}
				if (@params != null ? !@params.Equals(that.@params) : that.@params != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = function != null ? function.GetHashCode() : 0;
				result = 31 * result + (@params != null ? @params.GetHashCode() : 0);
				return result;
			}
		}

		[System.Serializable]
		public class MethodCallExpression : Expressions.TypedExpression
		{
			internal string function;

			private readonly IExpression @object;

			internal IList<IExpression> @params;

			public MethodCallExpression(string function, IExpression @object, IList<IExpression> @params, params string[] tags)
				: base(TypeFunction, tags)
			{
				this.function = function;
				this.@object = @object;
				this.@params = @params;
			}

			public override string ToString()
			{
				return @object + "." + function + '(' + StringUtils.Join(@params, ", ") + ')';
			}

			public override IExpression Simplify(Env env)
			{
				bool paramsAllHasValue = true;
				IList<IExpression> simplifiedParams = new List<IExpression>(@params.Count);
				foreach (IExpression param in @params)
				{
					IExpression simplified = param.Simplify(env);
					simplifiedParams.Add(simplified);
					if (!(simplified.HasValue()))
					{
						paramsAllHasValue = false;
					}
				}
				IExpression simplifiedObject = @object.Simplify(env);
				IExpression res = new Expressions.MethodCallExpression(function, simplifiedObject, simplifiedParams);
				if (paramsAllHasValue && @object.HasValue())
				{
					return res.Evaluate(env);
				}
				else
				{
					return res;
				}
			}

			public override IValue Evaluate(Env env, params object[] args)
			{
				IValue evaledObj = @object.Evaluate(env, args);
				if (evaledObj == null || evaledObj.Get() == null)
				{
					return null;
				}
				object mainObj = evaledObj.Get();
				Type c = mainObj.GetType();
				IList<IValue> evaled = new List<IValue>();
				foreach (IExpression param in @params)
				{
					evaled.Add(param.Evaluate(env, args));
				}
				Type[] paramTypes = new Type[@params.Count];
				object[] objs = new object[@params.Count];
				for (int i = 0; i < @params.Count; i++)
				{
					IValue v = evaled[i];
					if (v != null)
					{
						objs[i] = v.Get();
						if (objs[i] != null)
						{
							paramTypes[i] = objs[i].GetType();
						}
						else
						{
							paramTypes[i] = null;
						}
					}
					else
					{
						objs[i] = null;
						paramTypes[i] = null;
					}
				}
				//throw new RuntimeException("Missing evaluated value for " + params.get(i));
				MethodInfo method = null;
				try
				{
					method = c.GetMethod(function, paramTypes);
				}
				catch (MissingMethodException ex)
				{
					MethodInfo[] methods = c.GetMethods();
					foreach (MethodInfo m in methods)
					{
						if (m.Name.Equals(function))
						{
							Type[] mParamTypes = m.GetParameterTypes();
							if (mParamTypes.Length == paramTypes.Length)
							{
								bool compatible = IsArgTypesCompatible(paramTypes, mParamTypes);
								if (compatible)
								{
									method = m;
									break;
								}
							}
						}
					}
					if (method == null)
					{
						throw new Exception("Cannot find method " + function + " on object of class " + c, ex);
					}
				}
				try
				{
					object res = method.Invoke(mainObj, objs);
					return new Expressions.PrimitiveValue<object>(function, res);
				}
				catch (ReflectiveOperationException ex)
				{
					throw new Exception("Cannot evaluate method " + function + " on object " + mainObj, ex);
				}
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is Expressions.MethodCallExpression))
				{
					return false;
				}
				if (!base.Equals(o))
				{
					return false;
				}
				Expressions.MethodCallExpression that = (Expressions.MethodCallExpression)o;
				if (function != null ? !function.Equals(that.function) : that.function != null)
				{
					return false;
				}
				if (@object != null ? !@object.Equals(that.@object) : that.@object != null)
				{
					return false;
				}
				if (@params != null ? !@params.Equals(that.@params) : that.@params != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = base.GetHashCode();
				result = 31 * result + (function != null ? function.GetHashCode() : 0);
				result = 31 * result + (@object != null ? @object.GetHashCode() : 0);
				result = 31 * result + (@params != null ? @params.GetHashCode() : 0);
				return result;
			}
		}

		/// <summary>Primitive value that is directly represented by a Java object of type T</summary>
		[System.Serializable]
		public class PrimitiveValue<T> : Expressions.SimpleValue<T>
		{
			public PrimitiveValue(string typename, T value, params string[] tags)
				: base(typename, value, tags)
			{
			}
		}

		/// <summary>A composite value with field names and values for each field</summary>
		[System.Serializable]
		public class CompositeValue : Expressions.SimpleCachedExpression<IDictionary<string, IExpression>>, IValue<IDictionary<string, IExpression>>
		{
			public CompositeValue(params string[] tags)
				: base(TypeComposite, new Dictionary<string, IExpression>(), tags)
			{
			}

			public CompositeValue(IDictionary<string, IExpression> m, bool isEvaluated, params string[] tags)
				: base(TypeComposite, m, tags)
			{
				//Generics.<String,Expression>newHashMap()
				if (isEvaluated)
				{
					evaluated = this;
					disableCaching = !CheckValue();
				}
			}

			private bool CheckValue()
			{
				bool ok = true;
				foreach (string key in value.Keys)
				{
					IExpression expr = value[key];
					if (expr != null && !expr.HasValue())
					{
						ok = false;
					}
				}
				return ok;
			}

			public virtual ICollection<string> GetAttributes()
			{
				return value.Keys;
			}

			public virtual IExpression GetExpression(string attr)
			{
				return value[attr];
			}

			public virtual IValue GetValue(string attr)
			{
				IExpression expr = value[attr];
				if (expr == null)
				{
					return null;
				}
				if (expr is IValue)
				{
					return (IValue)expr;
				}
				throw new NotSupportedException("Expression was not evaluated....");
			}

			public virtual T Get<T>(string attr)
			{
				IExpression expr = value[attr];
				if (expr == null)
				{
					return null;
				}
				if (expr is IValue)
				{
					return ((IValue<T>)expr).Get();
				}
				throw new NotSupportedException("Expression was not evaluated....");
			}

			public virtual void Set(string attr, object obj)
			{
				if (obj is IExpression)
				{
					value[attr] = (IExpression)obj;
				}
				else
				{
					value[attr] = CreateValue(null, obj);
				}
				evaluated = null;
			}

			private static object ToCompatibleObject(FieldInfo f, object value)
			{
				if (value == null)
				{
					return value;
				}
				if (!f.DeclaringType.IsAssignableFrom(value.GetType()))
				{
					if (typeof(Number).IsAssignableFrom(value.GetType()))
					{
						Number number = (Number)value;
						if (f.GetType().IsAssignableFrom(typeof(double)))
						{
							return number;
						}
						else
						{
							if (f.GetType().IsAssignableFrom(typeof(float)))
							{
								return number;
							}
							else
							{
								if (f.GetType().IsAssignableFrom(typeof(long)))
								{
									return number;
								}
								else
								{
									if (f.GetType().IsAssignableFrom(typeof(int)))
									{
										return number;
									}
								}
							}
						}
					}
				}
				return value;
			}

			private static IValue AttemptTypeConversion(Expressions.CompositeValue cv, Env env, params object[] args)
			{
				IExpression typeFieldExpr = cv.value["type"];
				if (typeFieldExpr != null)
				{
					// Automatically convert types ....
					IValue typeValue = typeFieldExpr.Evaluate(env, args);
					if (typeFieldExpr is Expressions.VarExpression)
					{
						Expressions.VarExpression varExpr = (Expressions.VarExpression)typeFieldExpr;
						// The name of the variable is used to indicate the "type" of object
						string typeName = varExpr.Get();
						if (typeValue != null)
						{
							// Check if variable points to a class
							// If so, then try to instantiate a new instance of the class
							if (TypeClass.Equals(typeValue.GetType()))
							{
								// Variable maps to a java class
								Type c = (Type)typeValue.Get();
								try
								{
									object obj = System.Activator.CreateInstance(c);
									// for any field other than the "type", set the value of the field
									//   of the created object to the specified value
									foreach (string s in cv.value.Keys)
									{
										if (!"type".Equals(s))
										{
											IValue v = cv.value[s].Evaluate(env, args);
											try
											{
												FieldInfo f = c.GetField(s);
												object objVal = ToCompatibleObject(f, v.Get());
												f.SetValue(obj, objVal);
											}
											catch (NoSuchFieldException ex)
											{
												throw new Exception("Unknown field " + s + " for type " + typeName + ", trying to set to " + v, ex);
											}
											catch (ArgumentException ex)
											{
												throw new Exception("Incompatible type " + s + " for type " + typeName + ", trying to set to " + v, ex);
											}
										}
									}
									return new Expressions.PrimitiveValue<object>(typeName, obj);
								}
								catch (ReflectiveOperationException ex)
								{
									throw new Exception("Cannot instantiate " + c, ex);
								}
							}
							else
							{
								if (typeValue.Get() != null)
								{
									// When evaluated, variable does not explicitly map to "CLASS"
									// See if we can convert this CompositeValue into appropriate object
									// by calling "create(CompositeValue cv)"
									Type c = typeValue.Get().GetType();
									try
									{
										MethodInfo m = c.GetMethod("create", typeof(Expressions.CompositeValue));
										Expressions.CompositeValue evaluatedCv = cv.EvaluateNoTypeConversion(env, args);
										try
										{
											return new Expressions.PrimitiveValue<object>(typeName, m.Invoke(typeValue.Get(), evaluatedCv));
										}
										catch (ReflectiveOperationException ex)
										{
											throw new Exception("Cannot instantiate " + c, ex);
										}
									}
									catch (MissingMethodException)
									{
									}
								}
							}
						}
					}
					else
					{
						if (typeValue != null && typeValue.Get() is string)
						{
							string typeName = (string)typeValue.Get();
							// Predefined types:
							IExpression valueField = cv.value["value"];
							IValue value = valueField.Evaluate(env, args);
							switch (typeName)
							{
								case TypeAnnotationKey:
								{
									string className = (string)value.Get();
									try
									{
										return new Expressions.PrimitiveValue<Type>(TypeAnnotationKey, Sharpen.Runtime.GetType(className));
									}
									catch (TypeLoadException ex)
									{
										throw new Exception("Unknown class " + className, ex);
									}
									goto case TypeClass;
								}

								case TypeClass:
								{
									string className = (string)value.Get();
									try
									{
										return new Expressions.PrimitiveValue<Type>(TypeClass, Sharpen.Runtime.GetType(className));
									}
									catch (TypeLoadException ex)
									{
										throw new Exception("Unknown class " + className, ex);
									}
									goto case TypeString;
								}

								case TypeString:
								{
									return new Expressions.PrimitiveValue<string>(TypeString, (string)value.Get());
								}

								case TypeRegex:
								{
									return new Expressions.RegexValue((string)value.Get());
								}

								case TypeNumber:
								{
									/* } else if (TYPE_TOKEN_REGEX.equals(type)) {
									return new PrimitiveValue<TokenSequencePattern>(TYPE_TOKEN_REGEX, (TokenSequencePattern) value.get()); */
									if (value.Get() is Number)
									{
										return new Expressions.PrimitiveValue<Number>(TypeNumber, (Number)value.Get());
									}
									else
									{
										if (value.Get() is string)
										{
											string str = (string)value.Get();
											if (str.Contains("."))
											{
												return new Expressions.PrimitiveValue<Number>(TypeNumber, double.ValueOf(str));
											}
											else
											{
												return new Expressions.PrimitiveValue<Number>(TypeNumber, long.ValueOf(str));
											}
										}
										else
										{
											throw new ArgumentException("Invalid value " + value + " for type " + typeName);
										}
									}
									goto default;
								}

								default:
								{
									// TODO: support other types
									return new Expressions.PrimitiveValue(typeName, value.Get());
								}
							}
						}
					}
				}
				//throw new UnsupportedOperationException("Cannot convert type " + typeName);
				return null;
			}

			public virtual Expressions.CompositeValue SimplifyNoTypeConversion(Env env, params object[] args)
			{
				IDictionary<string, IExpression> m = value;
				IDictionary<string, IExpression> res = new Dictionary<string, IExpression>(m.Count);
				//Generics.newHashMap (m.size());
				foreach (KeyValuePair<string, IExpression> stringExpressionEntry in m)
				{
					res[stringExpressionEntry.Key] = stringExpressionEntry.Value.Simplify(env);
				}
				return new Expressions.CompositeValue(res, true);
			}

			private Expressions.CompositeValue EvaluateNoTypeConversion(Env env, params object[] args)
			{
				IDictionary<string, IExpression> m = value;
				IDictionary<string, IExpression> res = new Dictionary<string, IExpression>(m.Count);
				//Generics.newHashMap (m.size());
				foreach (KeyValuePair<string, IExpression> stringExpressionEntry in m)
				{
					res[stringExpressionEntry.Key] = stringExpressionEntry.Value.Evaluate(env, args);
				}
				return new Expressions.CompositeValue(res, true);
			}

			protected internal override IValue DoEvaluation(Env env, params object[] args)
			{
				IValue v = AttemptTypeConversion(this, env, args);
				if (v != null)
				{
					return v;
				}
				IDictionary<string, IExpression> m = value;
				IDictionary<string, IExpression> res = new Dictionary<string, IExpression>(m.Count);
				//Generics.newHashMap (m.size());
				foreach (KeyValuePair<string, IExpression> stringExpressionEntry in m)
				{
					res[stringExpressionEntry.Key] = stringExpressionEntry.Value.Evaluate(env, args);
				}
				disableCaching = !CheckValue();
				return new Expressions.CompositeValue(res, true);
			}
		}
		// end static class CompositeValue
	}
}

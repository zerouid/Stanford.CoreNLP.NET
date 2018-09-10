using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Util;






namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Rules for matching sequences using regular expressions.</summary>
	/// <remarks>
	/// Rules for matching sequences using regular expressions.
	/// <p>
	/// There are 2 types of rules:
	/// <ol>
	/// <li><b>Assignment rules</b> which assign a value to a variable for later use.
	/// </li>
	/// <li><b>Extraction rules</b> which specifies how regular expression patterns are to be matched against text,
	/// which matched text expressions are to extracted, and what value to assign to the matched expression.</li>
	/// </ol>
	/// NOTE:
	/// <c>#</c>
	/// or
	/// <c>//</c>
	/// can be used to indicates one-line comments.
	/// <p>
	/// <b>Assignment Rules</b> are used to assign values to variables.
	/// The basic format is:
	/// <c>variable = value</c>
	/// .
	/// <p>
	/// <em>Variable Names</em>:
	/// <ul>
	/// <li>Variable names should follow the pattern [A-Za-z_][A-Za-z0-9_]*</li>
	/// <li>Variable names for use in regular expressions (to be expanded later) must start with
	/// <c>$</c>
	/// </li>
	/// </ul>
	/// <p>
	/// <em>Value Types</em>:
	/// <table>
	/// <tr><th>Type</th><th>Format</th><th>Example</th><th>Description</th></tr>
	/// <tr><td>
	/// <c>BOOLEAN</c>
	/// </td><td>
	/// <c>TRUE | FALSE</c>
	/// </td><td>
	/// <c>TRUE</c>
	/// </td><td></td></tr>
	/// <tr><td>
	/// <c>STRING</c>
	/// </td><td>
	/// <c>"..."</c>
	/// </td><td>
	/// <c>"red"</c>
	/// </td><td></td></tr>
	/// <tr><td>
	/// <c>INTEGER</c>
	/// </td><td>
	/// <c>[+-]\d+</c>
	/// </td><td>
	/// <c>1500</c>
	/// </td><td></td></tr>
	/// <tr><td>
	/// <c>LONG</c>
	/// </td><td>
	/// <c>[+-]\d+L</c>
	/// </td><td>
	/// <c>1500000000000L</c>
	/// </td><td></td></tr>
	/// <tr><td>
	/// <c>DOUBLE</c>
	/// </td><td>
	/// <c>[+-]\d*\.\d+</c>
	/// </td><td>
	/// <c>6.98</c>
	/// </td><td></td></tr>
	/// <tr><td>
	/// <c>REGEX</c>
	/// </td><td>
	/// <c>/.../</c>
	/// </td><td>
	/// <c>/[Aa]pril/</c>
	/// </td>
	/// <td>String regular expression
	/// <see cref="Java.Util.Regex.Pattern"/>
	/// </td></tr>
	/// <tr><td>
	/// <c>TOKENS_REGEX</c>
	/// </td><td>
	/// <c>( [...] [...] ... )</c>
	/// </td><td>
	/// <c>( /up/ /to/ /4/ /months/ )</c>
	/// </td>
	/// <td>Tokens regular expression
	/// <see cref="TokenSequencePattern"/>
	/// </td></tr>
	/// <tr><td>
	/// <c>LIST</c>
	/// </td><td>
	/// <c>( [item1] , [item2], ... )</c>
	/// </td><td>
	/// <c>("red", "blue", "yellow" )</c>
	/// </td>
	/// <td></td></tr>
	/// </table>
	/// <p>
	/// Some typical uses and examples for assignment rules include:
	/// <ol>
	/// <li>Assignment of value to variables for use in later rules</li>
	/// <li>Binding of text key to annotation key (as
	/// <c>Class</c>
	/// ).
	/// <pre>
	/// tokens = { type: "CLASS", value: "edu.stanford.nlp.ling.CoreAnnotations$TokensAnnotation" }
	/// </pre>
	/// </li>
	/// <li>Defining regular expressions macros to be embedded in other regular expressions
	/// <pre>
	/// $SEASON = "/spring|summer|fall|autumn|winter/"
	/// $NUM = ( [ { numcomptype:NUMBER } ] )
	/// </pre>
	/// </li>
	/// <li>Setting default environment variables.
	/// Rules are applied with respect to an environment (
	/// <see cref="Env"/>
	/// ), which can be accessed using the variable
	/// <c>ENV</c>
	/// .
	/// Members of the Environment can be set as needed.
	/// <pre>
	/// # Set default parameters to be used when reading rules
	/// ENV.defaults["ruleType"] = "tokens"
	/// # Set default string pattern flags (to case-insensitive)
	/// ENV.defaultStringPatternFlags = 2
	/// # Specifies that the result should go into the
	/// <c>tokens</c>
	/// key (as defined above).
	/// ENV.defaultResultAnnotationKey = tokens
	/// </pre>
	/// </li>
	/// <li>Defining options</li>
	/// </ol>
	/// <p>
	/// Predefined values are:
	/// <table>
	/// <tr><th>Variable</th><th>Type</th><th>Description</th></tr>
	/// <tr><td>
	/// <c>ENV</c>
	/// </td><td>
	/// <see cref="Env"/>
	/// </td><td>The environment with respect to which the rules are applied.</td></tr>
	/// <tr><td>
	/// <c>TRUE</c>
	/// </td><td>
	/// <c>BOOLEAN</c>
	/// </td><td>The
	/// <c>Boolean</c>
	/// value
	/// <see langword="true"/>
	/// .</td></tr>
	/// <tr><td>
	/// <c>FALSE</c>
	/// </td><td>
	/// <c>BOOLEAN</c>
	/// </td><td>The
	/// <c>Boolean</c>
	/// value
	/// <see langword="false"/>
	/// .</td></tr>
	/// <tr><td>
	/// <c>NIL</c>
	/// </td><td>
	/// <c/>
	/// </td><td>The
	/// <see langword="null"/>
	/// value.</td></tr>
	/// <tr><td>
	/// <c>tags</c>
	/// </td><td>
	/// <c>Class</c>
	/// </td><td>The annotation key
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.Types.Tags.TagsAnnotation"/>
	/// .</td></tr>
	/// </table>
	/// <p>
	/// <b>Extraction Rules</b> specifies how regular expression patterns are to be matched against text.
	/// See
	/// <see cref="CoreMapExpressionExtractor{T}"/>
	/// for more information on the types of the rules, and in what sequence the rules are applied.
	/// A basic rule can be specified using the following template:
	/// <pre>
	/// {
	/// # Type of the rule
	/// ruleType: "tokens" | "text" | "composite" | "filter",
	/// # Pattern to match against
	/// pattern: ( &lt;TokenSequencePattern&gt; ) | /&lt;TextPattern&gt;/,
	/// # Resulting value to go into the resulting annotation
	/// result: ...
	/// # More fields following...
	/// }
	/// </pre>
	/// Example:
	/// <pre>
	/// {
	/// ruleType: "tokens",
	/// pattern: ( /one/ ),
	/// result: 1
	/// }
	/// </pre>
	/// <p>
	/// Extraction rule fields (most fields are optional):
	/// <table>
	/// <tr><th>Field</th><th>Values</th><th>Example</th><th>Description</th></tr>
	/// <tr><td>
	/// <c>ruleType</c>
	/// </td><td>
	/// <c>"tokens" | "text" | "composite" | "filter"</c>
	/// </td>
	/// <td>
	/// <c>tokens</c>
	/// </td><td>Type of the rule (required).</td></tr>
	/// <tr><td>
	/// <c>pattern</c>
	/// </td><td>
	/// <c>&lt;Token Sequence Pattern&gt; = (...) | &lt;Text Pattern&gt; = /.../</c>
	/// </td>
	/// <td>
	/// <c>( /winter/ /of/ $YEAR )</c>
	/// </td><td>Pattern to match against.
	/// See
	/// <see cref="TokenSequencePattern"/>
	/// and
	/// <see cref="Java.Util.Regex.Pattern"/>
	/// for
	/// how to specify patterns over tokens and strings (required).</td></tr>
	/// <tr><td>
	/// <c>action</c>
	/// </td><td>
	/// <c>&lt;Action List&gt; = (...)</c>
	/// </td>
	/// <td>
	/// <c>( Annotate($0, ner, "DATE") )</c>
	/// </td><td>List of actions to apply when the pattern is triggered.
	/// Each action is a
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions">TokensRegex Expression</see>
	/// </td></tr>
	/// <tr><td>
	/// <c>result</c>
	/// </td><td>
	/// <c>&lt;Expression&gt;</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Resulting value to go into the resulting annotation.  See
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions"/>
	/// for how to specify the result.</td></tr>
	/// <tr><td>
	/// <c>name</c>
	/// </td><td>
	/// <c>STRING</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Name to identify the extraction rule.</td></tr>
	/// <tr><td>
	/// <c>stage</c>
	/// </td><td>
	/// <c>INTEGER</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Stage at which the rule is to be applied.  Rules are grouped in stages, which are applied from lowest to highest.</td></tr>
	/// <tr><td>
	/// <c>active</c>
	/// </td><td>
	/// <c>Boolean</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Whether this rule is enabled (active) or not (default true).</td></tr>
	/// <tr><td>
	/// <c>priority</c>
	/// </td><td>
	/// <c>DOUBLE</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Priority of rule.  Within a stage, matches from higher priority rules are preferred.</td></tr>
	/// <tr><td>
	/// <c>weight</c>
	/// </td><td>
	/// <c>DOUBLE</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Weight of rule (not currently used).</td></tr>
	/// <tr><td>
	/// <c>over</c>
	/// </td><td>
	/// <c>CLASS</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Annotation field to check pattern against.</td></tr>
	/// <tr><td>
	/// <c>matchFindType</c>
	/// </td><td>
	/// <c>FIND_NONOVERLAPPING | FIND_ALL</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Whether to find all matched expression or just the nonoverlapping ones (default
	/// <c>FIND_NONOVERLAPPING</c>
	/// ).</td></tr>
	/// <tr><td>
	/// <c>matchWithResults</c>
	/// </td><td>
	/// <c>Boolean</c>
	/// </td>
	/// <td>
	/// <c/>
	/// </td><td>Whether results of the matches should be returned (default false).
	/// Set to true to access captured groups of embedded regular expressions.</td></tr>
	/// <tr><td>
	/// <c>matchedExpressionGroup</c>
	/// </td><td>
	/// <c>Integer</c>
	/// </td>
	/// <td>
	/// <c>2</c>
	/// </td><td>What group should be treated as the matched expression group (default 0).</td></tr>
	/// </table>
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <seealso cref="CoreMapExpressionExtractor{T}"/>
	/// <seealso cref="TokenSequencePattern"/>
	public class SequenceMatchRules
	{
		private SequenceMatchRules()
		{
		}

		/// <summary>A sequence match rule.</summary>
		public interface IRule
		{
			// static class with inner classes
		}

		/// <summary>Rule that specifies what value to assign to a variable.</summary>
		public class AssignmentRule : SequenceMatchRules.IRule
		{
			internal readonly IExpression expr;

			public AssignmentRule(IAssignableExpression varExpr, IExpression value)
			{
				expr = varExpr.Assign(value);
			}

			public virtual void Evaluate(Env env)
			{
				expr.Evaluate(env);
			}
		}

		/// <summary>Rule that specifies how to extract sequence of MatchedExpression from an annotation (CoreMap).</summary>
		/// <?/>
		[System.Serializable]
		public class AnnotationExtractRule<S, T> : SequenceMatchRules.IRule, SequenceMatchRules.IExtractRule<S, T>, IPredicate<T>
			where T : MatchedExpression
		{
			private const long serialVersionUID = -2148125332223720424L;

			/// <summary>Name of the rule</summary>
			public string name;

			/// <summary>Stage in which this rule should be applied with respect to others</summary>
			public int stage = 1;

			/// <summary>Priority in which this rule should be applied with respect to others</summary>
			public double priority;

			/// <summary>Weight given to the rule (how likely is this rule to fire)</summary>
			public double weight;

			/// <summary>Annotation field to apply rule over: text or tokens or numerizedtokens</summary>
			public Type annotationField;

			public Type tokensAnnotationField;

			/// <summary>Annotation field(s) on individual tokens to put new annotation</summary>
			public IList<Type> tokensResultAnnotationField;

			/// <summary>Annotation field(s) to put new annotation</summary>
			public IList<Type> resultAnnotationField;

			/// <summary>Annotation field for child/nested annotations</summary>
			public Type resultNestedAnnotationField;

			public SequenceMatcher.FindType matchFindType;

			/// <summary>Which group to take as the matched expression - default is 0</summary>
			public int matchedExpressionGroup;

			public bool matchWithResults;

			/// <summary>Type of rule to apply: token string match, pattern string match</summary>
			public string ruleType;

			public bool isComposite;

			public bool includeNested = true;

			public bool active = true;

			/// <summary>Actual rule performing the extraction (converting annotation to MatchedExpression)</summary>
			public SequenceMatchRules.IExtractRule<S, T> extractRule;

			public IPredicate<T> filterRule;

			/// <summary>Pattern - the type of which is dependent on the rule type</summary>
			public object pattern;

			public IExpression result;

			// TODO: Combine ruleType and isComposite
			// TODO: Get parameter from somewhere....
			public virtual void Update(Env env, IDictionary<string, object> attributes)
			{
				foreach (KeyValuePair<string, object> stringObjectEntry in attributes)
				{
					string key = stringObjectEntry.Key;
					object obj = stringObjectEntry.Value;
					switch (key)
					{
						case "name":
						{
							name = (string)Expressions.AsObject(env, obj);
							break;
						}

						case "priority":
						{
							priority = ((Number)Expressions.AsObject(env, obj));
							break;
						}

						case "stage":
						{
							stage = ((Number)Expressions.AsObject(env, obj));
							break;
						}

						case "weight":
						{
							weight = ((Number)Expressions.AsObject(env, obj));
							break;
						}

						case "over":
						{
							object annoKey = Expressions.AsObject(env, obj);
							if (annoKey is Type)
							{
								annotationField = (Type)annoKey;
							}
							else
							{
								if (annoKey is string)
								{
									annotationField = EnvLookup.LookupAnnotationKeyWithClassname(env, (string)annoKey);
								}
								else
								{
									if (annotationField == null)
									{
										annotationField = typeof(ICoreMap);
									}
									else
									{
										throw new ArgumentException("Invalid annotation key " + annoKey);
									}
								}
							}
							break;
						}

						case "active":
						{
							active = (bool)Expressions.AsObject(env, obj);
							break;
						}

						case "ruleType":
						{
							ruleType = (string)Expressions.AsObject(env, obj);
							break;
						}

						case "matchFindType":
						{
							matchFindType = SequenceMatcher.FindType.ValueOf((string)Expressions.AsObject(env, obj));
							break;
						}

						case "matchWithResults":
						{
							matchWithResults = ((bool)Expressions.AsObject(env, obj));
							break;
						}

						case "matchedExpressionGroup":
						{
							matchedExpressionGroup = ((Number)Expressions.AsObject(env, obj));
							break;
						}
					}
				}
			}

			public virtual bool Extract(S @in, IList<T> @out)
			{
				return extractRule.Extract(@in, @out);
			}

			public virtual bool Test(T obj)
			{
				return filterRule.Test(obj);
			}

			public virtual bool IsMostlyCompatible(SequenceMatchRules.AnnotationExtractRule<S, T> aer)
			{
				// TODO: Check tokensResultAnnotationField, resultAnnotationField, resultNestedAnnotationField?
				return (stage == aer.stage && Objects.Equals(annotationField, aer.annotationField) && Objects.Equals(tokensAnnotationField, aer.tokensAnnotationField) && matchedExpressionGroup == 0 && aer.matchedExpressionGroup == 0 && matchWithResults == aer
					.matchWithResults && Objects.Equals(ruleType, aer.ruleType) && isComposite == aer.isComposite && active == aer.active && Objects.Equals(result, aer.result));
			}

			public virtual bool HasTokensRegexPattern()
			{
				return pattern != null && pattern is TokenSequencePattern;
			}

			public override string ToString()
			{
				return GetType().GetSimpleName() + '[' + pattern.ToString() + ']';
			}
		}

		// end static class AnnotationExtractRule
		public static SequenceMatchRules.AssignmentRule CreateAssignmentRule(Env env, IAssignableExpression var, IExpression result)
		{
			SequenceMatchRules.AssignmentRule ar = new SequenceMatchRules.AssignmentRule(var, result);
			ar.Evaluate(env);
			return ar;
		}

		public static SequenceMatchRules.IRule CreateRule(Env env, Expressions.CompositeValue cv)
		{
			IDictionary<string, object> attributes;
			cv = cv.SimplifyNoTypeConversion(env);
			attributes = new Dictionary<string, object>();
			//Generics.newHashMap();
			foreach (string s in cv.GetAttributes())
			{
				attributes[s] = cv.GetExpression(s);
			}
			return CreateExtractionRule(env, attributes);
		}

		protected internal static SequenceMatchRules.AnnotationExtractRule CreateExtractionRule(Env env, IDictionary<string, object> attributes)
		{
			string ruleType = (string)Expressions.AsObject(env, attributes["ruleType"]);
			if (ruleType == null && env != null)
			{
				ruleType = (string)env.GetDefaults()["ruleType"];
			}
			SequenceMatchRules.AnnotationExtractRuleCreator ruleCreator = LookupExtractRuleCreator(env, ruleType);
			if (ruleCreator != null)
			{
				return ruleCreator.Create(env, attributes);
			}
			else
			{
				throw new ArgumentException("Unknown rule type: " + ruleType);
			}
		}

		public static SequenceMatchRules.AnnotationExtractRule CreateExtractionRule(Env env, string ruleType, object pattern, IExpression result)
		{
			if (ruleType == null && env != null)
			{
				ruleType = (string)env.GetDefaults()["ruleType"];
			}
			SequenceMatchRules.AnnotationExtractRuleCreator ruleCreator = LookupExtractRuleCreator(env, ruleType);
			if (ruleCreator != null)
			{
				IDictionary<string, object> attributes = new Dictionary<string, object>();
				//Generics.newHashMap();
				attributes["ruleType"] = ruleType;
				attributes["pattern"] = pattern;
				attributes["result"] = result;
				return ruleCreator.Create(env, attributes);
			}
			else
			{
				throw new ArgumentException("Unknown rule type: " + ruleType);
			}
		}

		public const string CompositeRuleType = "composite";

		public const string TokenPatternRuleType = "tokens";

		public const string TextPatternRuleType = "text";

		public const string FilterRuleType = "filter";

		public static readonly SequenceMatchRules.TokenPatternExtractRuleCreator TokenPatternExtractRuleCreator = new SequenceMatchRules.TokenPatternExtractRuleCreator();

		public static readonly SequenceMatchRules.CompositeExtractRuleCreator CompositeExtractRuleCreator = new SequenceMatchRules.CompositeExtractRuleCreator();

		public static readonly SequenceMatchRules.TextPatternExtractRuleCreator TextPatternExtractRuleCreator = new SequenceMatchRules.TextPatternExtractRuleCreator();

		public static readonly SequenceMatchRules.MultiTokenPatternExtractRuleCreator MultiTokenPatternExtractRuleCreator = new SequenceMatchRules.MultiTokenPatternExtractRuleCreator();

		public static readonly SequenceMatchRules.AnnotationExtractRuleCreator DefaultExtractRuleCreator = TokenPatternExtractRuleCreator;

		private static readonly IDictionary<string, SequenceMatchRules.AnnotationExtractRuleCreator> registeredRuleTypes = new Dictionary<string, SequenceMatchRules.AnnotationExtractRuleCreator>();

		static SequenceMatchRules()
		{
			//Generics.newHashMap();
			registeredRuleTypes[TokenPatternRuleType] = TokenPatternExtractRuleCreator;
			registeredRuleTypes[CompositeRuleType] = CompositeExtractRuleCreator;
			registeredRuleTypes[TextPatternRuleType] = TextPatternExtractRuleCreator;
			registeredRuleTypes[FilterRuleType] = TokenPatternExtractRuleCreator;
		}

		private static SequenceMatchRules.AnnotationExtractRuleCreator LookupExtractRuleCreator(Env env, string ruleType)
		{
			if (env != null)
			{
				object obj = env.Get(ruleType);
				if (obj != null && obj is SequenceMatchRules.AnnotationExtractRuleCreator)
				{
					return (SequenceMatchRules.AnnotationExtractRuleCreator)obj;
				}
			}
			if (ruleType == null)
			{
				return DefaultExtractRuleCreator;
			}
			else
			{
				return registeredRuleTypes[ruleType];
			}
		}

		public static SequenceMatchRules.AnnotationExtractRule CreateTokenPatternRule(Env env, SequencePattern.PatternExpr expr, IExpression result)
		{
			return TokenPatternExtractRuleCreator.Create(env, expr, result);
		}

		public static SequenceMatchRules.AnnotationExtractRule CreateTextPatternRule(Env env, string expr, IExpression result)
		{
			return TextPatternExtractRuleCreator.Create(env, expr, result);
		}

		public static SequenceMatchRules.AnnotationExtractRule CreateMultiTokenPatternRule(Env env, SequenceMatchRules.AnnotationExtractRule template, IList<TokenSequencePattern> patterns)
		{
			return SequenceMatchRules.MultiTokenPatternExtractRuleCreator.Create(env, template, patterns);
		}

		public class AnnotationExtractRuleCreator
		{
			public virtual SequenceMatchRules.AnnotationExtractRule Create(Env env)
			{
				SequenceMatchRules.AnnotationExtractRule r = new SequenceMatchRules.AnnotationExtractRule();
				r.resultAnnotationField = EnvLookup.GetDefaultResultAnnotationKey(env);
				r.resultNestedAnnotationField = EnvLookup.GetDefaultNestedResultsAnnotationKey(env);
				r.tokensAnnotationField = EnvLookup.GetDefaultTokensAnnotationKey(env);
				r.tokensResultAnnotationField = EnvLookup.GetDefaultTokensResultAnnotationKey(env);
				if (env != null)
				{
					r.Update(env, env.GetDefaults());
				}
				return r;
			}

			public virtual SequenceMatchRules.AnnotationExtractRule Create(Env env, IDictionary<string, object> attributes)
			{
				// Get default annotation extract rule from env
				SequenceMatchRules.AnnotationExtractRule r = Create(env);
				if (attributes != null)
				{
					r.Update(env, attributes);
				}
				return r;
			}
		}

		public static MatchedExpression.SingleAnnotationExtractor CreateAnnotationExtractor(Env env, SequenceMatchRules.AnnotationExtractRule r)
		{
			MatchedExpression.SingleAnnotationExtractor extractor = new MatchedExpression.SingleAnnotationExtractor();
			extractor.name = r.name;
			extractor.tokensAnnotationField = r.tokensAnnotationField;
			extractor.tokensResultAnnotationField = r.tokensResultAnnotationField;
			extractor.resultAnnotationField = r.resultAnnotationField;
			extractor.resultNestedAnnotationField = r.resultNestedAnnotationField;
			extractor.priority = r.priority;
			extractor.weight = r.weight;
			extractor.includeNested = r.includeNested;
			extractor.resultAnnotationExtractor = EnvLookup.GetDefaultResultAnnotationExtractor(env);
			extractor.tokensAggregator = EnvLookup.GetDefaultTokensAggregator(env);
			return extractor;
		}

		public class CompositeExtractRuleCreator : SequenceMatchRules.AnnotationExtractRuleCreator
		{
			protected internal static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, SequencePattern.PatternExpr expr, IExpression action, IExpression result)
			{
				TokenSequencePattern pattern = ((TokenSequencePattern)TokenSequencePattern.Compile(expr));
				UpdateExtractRule(r, env, pattern, action, result);
			}

			protected internal static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, TokenSequencePattern pattern, IExpression action, IExpression result)
			{
				MatchedExpression.SingleAnnotationExtractor annotationExtractor = CreateAnnotationExtractor(env, r);
				SequenceMatchRules.SequenceMatchResultExtractor<ICoreMap> valueExtractor = new SequenceMatchRules.SequenceMatchResultExtractor<ICoreMap>(env, action, result);
				SequenceMatchRules.SequencePatternExtractRule<ICoreMap, IValue> valueExtractRule = new SequenceMatchRules.SequencePatternExtractRule<ICoreMap, IValue>(pattern, valueExtractor, r.matchFindType, r.matchWithResults);
				SequenceMatchRules.SequenceMatchedExpressionExtractor exprExtractor = new SequenceMatchRules.SequenceMatchedExpressionExtractor(annotationExtractor, r.matchedExpressionGroup);
				SequenceMatchRules.SequencePatternExtractRule<ICoreMap, MatchedExpression> exprExtractRule = new SequenceMatchRules.SequencePatternExtractRule<ICoreMap, MatchedExpression>(pattern, exprExtractor, r.matchFindType, r.matchWithResults);
				annotationExtractor.expressionToValue = null;
				annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapFunctionApplier<IList<ICoreMap>, IValue>(env, r.annotationField, valueExtractRule);
				r.extractRule = exprExtractRule;
				r.filterRule = new SequenceMatchRules.AnnotationMatchedFilter(annotationExtractor);
				r.pattern = pattern;
				r.result = result;
				pattern.weight = r.weight;
				pattern.priority = r.priority;
			}

			protected internal virtual SequenceMatchRules.AnnotationExtractRule Create(Env env, SequencePattern.PatternExpr expr, IExpression result)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, null);
				r.isComposite = true;
				if (r.annotationField == null)
				{
					r.annotationField = r.resultNestedAnnotationField;
				}
				if (r.annotationField == null)
				{
					throw new ArgumentException("Error creating composite rule: no annotation field");
				}
				r.ruleType = TokenPatternRuleType;
				UpdateExtractRule(r, env, expr, null, result);
				return r;
			}

			public override SequenceMatchRules.AnnotationExtractRule Create(Env env, IDictionary<string, object> attributes)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, attributes);
				r.isComposite = true;
				if (r.annotationField == null)
				{
					r.annotationField = r.resultNestedAnnotationField;
				}
				if (r.annotationField == null)
				{
					throw new ArgumentException("Error creating composite rule: no annotation field");
				}
				if (r.ruleType == null)
				{
					r.ruleType = TokenPatternRuleType;
				}
				//SequencePattern.PatternExpr expr = (SequencePattern.PatternExpr) attributes.get("pattern");
				TokenSequencePattern expr = (TokenSequencePattern)Expressions.AsObject(env, attributes["pattern"]);
				IExpression action = Expressions.AsExpression(env, attributes["action"]);
				IExpression result = Expressions.AsExpression(env, attributes["result"]);
				UpdateExtractRule(r, env, expr, action, result);
				return r;
			}
		}

		public class TokenPatternExtractRuleCreator : SequenceMatchRules.AnnotationExtractRuleCreator
		{
			protected internal static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, SequencePattern.PatternExpr expr, IExpression action, IExpression result)
			{
				TokenSequencePattern pattern = ((TokenSequencePattern)TokenSequencePattern.Compile(expr));
				UpdateExtractRule(r, env, pattern, action, result);
			}

			protected internal static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, TokenSequencePattern pattern, IExpression action, IExpression result)
			{
				MatchedExpression.SingleAnnotationExtractor annotationExtractor = CreateAnnotationExtractor(env, r);
				SequenceMatchRules.SequenceMatchResultExtractor<ICoreMap> valueExtractor = new SequenceMatchRules.SequenceMatchResultExtractor<ICoreMap>(env, action, result);
				SequenceMatchRules.SequencePatternExtractRule<ICoreMap, IValue> valueExtractRule = new SequenceMatchRules.SequencePatternExtractRule<ICoreMap, IValue>(pattern, valueExtractor, r.matchFindType, r.matchWithResults);
				SequenceMatchRules.SequenceMatchedExpressionExtractor exprExtractor = new SequenceMatchRules.SequenceMatchedExpressionExtractor(annotationExtractor, r.matchedExpressionGroup);
				SequenceMatchRules.SequencePatternExtractRule<ICoreMap, MatchedExpression> exprExtractRule = new SequenceMatchRules.SequencePatternExtractRule<ICoreMap, MatchedExpression>(pattern, exprExtractor, r.matchFindType, r.matchWithResults);
				annotationExtractor.expressionToValue = null;
				if (r.annotationField != null && r.annotationField != typeof(ICoreMap))
				{
					annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapFunctionApplier<IList<ICoreMap>, IValue>(env, r.annotationField, valueExtractRule);
					r.extractRule = new SequenceMatchRules.CoreMapExtractRule<IList<ICoreMap>, MatchedExpression>(env, r.annotationField, exprExtractRule);
				}
				else
				{
					annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapToListFunctionApplier<IValue>(env, valueExtractRule);
					r.extractRule = new SequenceMatchRules.CoreMapToListExtractRule<MatchedExpression>(exprExtractRule);
				}
				r.filterRule = new SequenceMatchRules.AnnotationMatchedFilter(annotationExtractor);
				r.pattern = pattern;
				r.result = result;
				pattern.weight = r.weight;
				pattern.priority = r.priority;
			}

			protected internal virtual SequenceMatchRules.AnnotationExtractRule Create(Env env, SequencePattern.PatternExpr expr, IExpression result)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, null);
				if (r.annotationField == null)
				{
					r.annotationField = r.tokensAnnotationField;
				}
				r.ruleType = TokenPatternRuleType;
				UpdateExtractRule(r, env, expr, null, result);
				return r;
			}

			public override SequenceMatchRules.AnnotationExtractRule Create(Env env, IDictionary<string, object> attributes)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, attributes);
				if (r.annotationField == null)
				{
					r.annotationField = r.tokensAnnotationField;
				}
				if (r.ruleType == null)
				{
					r.ruleType = TokenPatternRuleType;
				}
				//SequencePattern.PatternExpr expr = (SequencePattern.PatternExpr) attributes.get("pattern");
				TokenSequencePattern expr = (TokenSequencePattern)Expressions.AsObject(env, attributes["pattern"]);
				IExpression action = Expressions.AsExpression(env, attributes["action"]);
				IExpression result = Expressions.AsExpression(env, attributes["result"]);
				UpdateExtractRule(r, env, expr, action, result);
				return r;
			}
		}

		public class MultiTokenPatternExtractRuleCreator : SequenceMatchRules.AnnotationExtractRuleCreator
		{
			protected internal static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, MultiPatternMatcher<ICoreMap> pattern, IExpression action, IExpression result)
			{
				MatchedExpression.SingleAnnotationExtractor annotationExtractor = CreateAnnotationExtractor(env, r);
				SequenceMatchRules.SequenceMatchResultExtractor<ICoreMap> valueExtractor = new SequenceMatchRules.SequenceMatchResultExtractor<ICoreMap>(env, action, result);
				SequenceMatchRules.MultiSequencePatternExtractRule<ICoreMap, IValue> valueExtractRule = new SequenceMatchRules.MultiSequencePatternExtractRule<ICoreMap, IValue>(pattern, valueExtractor);
				SequenceMatchRules.SequenceMatchedExpressionExtractor exprExtractor = new SequenceMatchRules.SequenceMatchedExpressionExtractor(annotationExtractor, r.matchedExpressionGroup);
				SequenceMatchRules.MultiSequencePatternExtractRule<ICoreMap, MatchedExpression> exprExtractRule = new SequenceMatchRules.MultiSequencePatternExtractRule<ICoreMap, MatchedExpression>(pattern, exprExtractor);
				annotationExtractor.expressionToValue = null;
				if (r.annotationField != null && r.annotationField != typeof(ICoreMap))
				{
					annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapFunctionApplier<IList<ICoreMap>, IValue>(env, r.annotationField, valueExtractRule);
					r.extractRule = new SequenceMatchRules.CoreMapExtractRule<IList<ICoreMap>, MatchedExpression>(env, r.annotationField, exprExtractRule);
				}
				else
				{
					annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapToListFunctionApplier<IValue>(env, valueExtractRule);
					r.extractRule = new SequenceMatchRules.CoreMapToListExtractRule<MatchedExpression>(exprExtractRule);
				}
				r.filterRule = new SequenceMatchRules.AnnotationMatchedFilter(annotationExtractor);
				r.pattern = pattern;
				r.result = result;
			}

			protected internal static SequenceMatchRules.AnnotationExtractRule Create(Env env, SequenceMatchRules.AnnotationExtractRule aerTemplate, IList<TokenSequencePattern> patterns)
			{
				SequenceMatchRules.AnnotationExtractRule r = new SequenceMatchRules.AnnotationExtractRule();
				r.stage = aerTemplate.stage;
				r.active = aerTemplate.active;
				r.priority = double.NaN;
				// Priority from patterns?
				r.weight = double.NaN;
				// weight from patterns?
				r.annotationField = aerTemplate.annotationField;
				r.tokensAnnotationField = aerTemplate.tokensAnnotationField;
				r.tokensResultAnnotationField = aerTemplate.tokensResultAnnotationField;
				r.resultAnnotationField = aerTemplate.resultAnnotationField;
				r.resultNestedAnnotationField = aerTemplate.resultNestedAnnotationField;
				r.matchFindType = aerTemplate.matchFindType;
				r.matchedExpressionGroup = aerTemplate.matchedExpressionGroup;
				r.matchWithResults = aerTemplate.matchWithResults;
				r.ruleType = aerTemplate.ruleType;
				r.isComposite = aerTemplate.isComposite;
				r.includeNested = aerTemplate.includeNested;
				r.active = aerTemplate.active;
				r.result = aerTemplate.result;
				if (r.annotationField == null)
				{
					r.annotationField = r.tokensAnnotationField;
				}
				r.ruleType = TokenPatternRuleType;
				MultiPatternMatcher<ICoreMap> multiPatternMatcher = TokenSequencePattern.GetMultiPatternMatcher(patterns);
				multiPatternMatcher.SetMatchWithResult(r.matchWithResults);
				UpdateExtractRule(r, env, multiPatternMatcher, null, r.result);
				return r;
			}

			public override SequenceMatchRules.AnnotationExtractRule Create(Env env, IDictionary<string, object> attributes)
			{
				throw new NotSupportedException();
			}
		}

		public class TextPatternExtractRuleCreator : SequenceMatchRules.AnnotationExtractRuleCreator
		{
			protected internal static void UpdateExtractRule(SequenceMatchRules.AnnotationExtractRule r, Env env, string expr, IExpression action, IExpression result)
			{
				MatchedExpression.SingleAnnotationExtractor annotationExtractor = CreateAnnotationExtractor(env, r);
				Pattern pattern = env.GetStringPattern(expr);
				SequenceMatchRules.StringMatchResultExtractor valueExtractor = new SequenceMatchRules.StringMatchResultExtractor(env, action, result);
				SequenceMatchRules.StringPatternExtractRule<IValue> valueExtractRule = new SequenceMatchRules.StringPatternExtractRule<IValue>(pattern, valueExtractor);
				SequenceMatchRules.StringMatchedExpressionExtractor exprExtractor = new SequenceMatchRules.StringMatchedExpressionExtractor(annotationExtractor, r.matchedExpressionGroup);
				SequenceMatchRules.StringPatternExtractRule<MatchedExpression> exprExtractRule = new SequenceMatchRules.StringPatternExtractRule<MatchedExpression>(pattern, exprExtractor);
				annotationExtractor.valueExtractor = new SequenceMatchRules.CoreMapFunctionApplier<string, IValue>(env, r.annotationField, valueExtractRule);
				r.extractRule = new SequenceMatchRules.CoreMapExtractRule<string, MatchedExpression>(env, r.annotationField, exprExtractRule);
				r.filterRule = new SequenceMatchRules.AnnotationMatchedFilter(annotationExtractor);
				r.pattern = pattern;
				r.result = result;
			}

			protected internal virtual SequenceMatchRules.AnnotationExtractRule Create(Env env, string expr, IExpression result)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, null);
				if (r.annotationField == null)
				{
					r.annotationField = EnvLookup.GetDefaultTextAnnotationKey(env);
				}
				r.ruleType = TextPatternRuleType;
				UpdateExtractRule(r, env, expr, null, result);
				return r;
			}

			public override SequenceMatchRules.AnnotationExtractRule Create(Env env, IDictionary<string, object> attributes)
			{
				SequenceMatchRules.AnnotationExtractRule r = base.Create(env, attributes);
				if (r.annotationField == null)
				{
					r.annotationField = EnvLookup.GetDefaultTextAnnotationKey(env);
				}
				if (r.ruleType == null)
				{
					r.ruleType = TextPatternRuleType;
				}
				string expr = (string)Expressions.AsObject(env, attributes["pattern"]);
				IExpression action = Expressions.AsExpression(env, attributes["action"]);
				IExpression result = Expressions.AsExpression(env, attributes["result"]);
				UpdateExtractRule(r, env, expr, action, result);
				return r;
			}
		}

		[System.Serializable]
		public class AnnotationMatchedFilter : IPredicate<MatchedExpression>
		{
			private const long serialVersionUID = -2085736376364259354L;

			internal readonly MatchedExpression.SingleAnnotationExtractor extractor;

			public AnnotationMatchedFilter(MatchedExpression.SingleAnnotationExtractor extractor)
			{
				this.extractor = extractor;
			}

			public virtual bool Test(MatchedExpression me)
			{
				ICoreMap cm = me.GetAnnotation();
				IValue v = extractor.Apply(cm);
				if (v != null)
				{
					if (v.Get() == null)
					{
						return true;
					}
					else
					{
						extractor.Annotate(me);
						return false;
					}
				}
				else
				{
					//return v.get() == null;
					return false;
				}
			}
		}

		public class StringMatchResultExtractor : Func<IMatchResult, IValue>
		{
			internal readonly Env env;

			internal readonly IExpression action;

			internal readonly IExpression result;

			public StringMatchResultExtractor(Env env, IExpression action, IExpression result)
			{
				this.env = env;
				this.action = action;
				this.result = result;
			}

			public StringMatchResultExtractor(Env env, IExpression result)
				: this(env, null, result)
			{
			}

			public virtual IValue Apply(IMatchResult matchResult)
			{
				IValue v = null;
				if (action != null)
				{
					action.Evaluate(env, matchResult);
				}
				if (result != null)
				{
					v = result.Evaluate(env, matchResult);
				}
				return v;
			}
		}

		public class SequenceMatchResultExtractor<T> : Func<ISequenceMatchResult<T>, IValue>
		{
			internal readonly Env env;

			internal readonly IExpression action;

			internal readonly IExpression result;

			public SequenceMatchResultExtractor(Env env, IExpression action, IExpression result)
			{
				this.env = env;
				this.action = action;
				this.result = result;
			}

			public SequenceMatchResultExtractor(Env env, IExpression result)
				: this(env, null, result)
			{
			}

			public virtual IValue Apply(ISequenceMatchResult<T> matchResult)
			{
				IValue v = null;
				if (action != null)
				{
					action.Evaluate(env, matchResult);
				}
				if (result != null)
				{
					v = result.Evaluate(env, matchResult);
				}
				return v;
			}
		}

		/// <summary>Interface for a rule that extracts a list of matched items from an input.</summary>
		/// <?/>
		/// <?/>
		public interface IExtractRule<I, O>
		{
			bool Extract(I @in, IList<O> @out);
		}

		/// <summary>Extraction rule that filters the input before passing it on to the next extractor.</summary>
		/// <?/>
		/// <?/>
		public class FilterExtractRule<I, O> : SequenceMatchRules.IExtractRule<I, O>
		{
			internal readonly IPredicate<I> filter;

			internal readonly SequenceMatchRules.IExtractRule<I, O> rule;

			public FilterExtractRule(IPredicate<I> filter, SequenceMatchRules.IExtractRule<I, O> rule)
			{
				this.filter = filter;
				this.rule = rule;
			}

			[SafeVarargs]
			public FilterExtractRule(IPredicate<I> filter, params SequenceMatchRules.IExtractRule<I, O>[] rules)
			{
				this.filter = filter;
				this.rule = new SequenceMatchRules.ListExtractRule<I, O>(rules);
			}

			public virtual bool Extract(I @in, IList<O> @out)
			{
				if (filter.Test(@in))
				{
					return rule.Extract(@in, @out);
				}
				else
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Extraction rule that applies a list of rules in sequence and aggregates
		/// all matches found.
		/// </summary>
		/// <?/>
		/// <?/>
		public class ListExtractRule<I, O> : SequenceMatchRules.IExtractRule<I, O>
		{
			internal readonly IList<SequenceMatchRules.IExtractRule<I, O>> rules;

			public ListExtractRule(ICollection<SequenceMatchRules.IExtractRule<I, O>> rules)
			{
				this.rules = new List<SequenceMatchRules.IExtractRule<I, O>>(rules);
			}

			[SafeVarargs]
			public ListExtractRule(params SequenceMatchRules.IExtractRule<I, O>[] rules)
			{
				this.rules = new List<SequenceMatchRules.IExtractRule<I, O>>(rules.Length);
				Java.Util.Collections.AddAll(this.rules, rules);
			}

			public virtual bool Extract(I @in, IList<O> @out)
			{
				bool extracted = false;
				foreach (SequenceMatchRules.IExtractRule<I, O> rule in rules)
				{
					if (rule.Extract(@in, @out))
					{
						extracted = true;
					}
				}
				return extracted;
			}

			[SafeVarargs]
			public void AddRules(params SequenceMatchRules.IExtractRule<I, O>[] rules)
			{
				Java.Util.Collections.AddAll(this.rules, rules);
			}

			public virtual void AddRules(ICollection<SequenceMatchRules.IExtractRule<I, O>> rules)
			{
				Sharpen.Collections.AddAll(this.rules, rules);
			}

			public virtual string RuleList()
			{
				IList<string> names = new List<string>();
				foreach (SequenceMatchRules.IExtractRule rule in rules)
				{
					if (rule is SequenceMatchRules.AnnotationExtractRule)
					{
						SequenceMatchRules.AnnotationExtractRule aer = (SequenceMatchRules.AnnotationExtractRule)rule;
						string ruleString;
						// initialized below
						if (aer.pattern != null)
						{
							ruleString = aer.pattern.ToString();
						}
						else
						{
							if (aer.extractRule != null)
							{
								ruleString = aer.extractRule.ToString();
							}
							else
							{
								if (aer.filterRule != null)
								{
									ruleString = aer.filterRule.ToString();
								}
								else
								{
									ruleString = aer.ToString();
								}
							}
						}
						names.Add(ruleString);
					}
					else
					{
						names.Add(rule.GetType().FullName);
					}
				}
				return names.ToString();
			}

			public override string ToString()
			{
				return "ListExtractRule[" + RuleList() + ']';
			}
		}

		/// <summary>Extraction rule to apply a extraction rule on a particular CoreMap field.</summary>
		/// <remarks>
		/// Extraction rule to apply a extraction rule on a particular CoreMap field.
		/// Input is of type CoreMap, output is templated type O.
		/// </remarks>
		/// <?/>
		/// <?/>
		public class CoreMapExtractRule<T, O> : SequenceMatchRules.IExtractRule<ICoreMap, O>
		{
			internal readonly Env env;

			internal readonly Type annotationField;

			internal readonly SequenceMatchRules.IExtractRule<T, O> extractRule;

			public CoreMapExtractRule(Env env, Type annotationField, SequenceMatchRules.IExtractRule<T, O> extractRule)
			{
				this.annotationField = annotationField;
				this.extractRule = extractRule;
				this.env = env;
			}

			public virtual bool Extract(ICoreMap cm, IList<O> @out)
			{
				env.Push(Expressions.VarSelf, cm);
				try
				{
					T field = (T)cm.Get(annotationField);
					return extractRule.Extract(field, @out);
				}
				finally
				{
					env.Pop(Expressions.VarSelf);
				}
			}
		}

		/// <summary>Extraction rule that treats a single CoreMap as a list/sequence of CoreMaps.</summary>
		/// <remarks>
		/// Extraction rule that treats a single CoreMap as a list/sequence of CoreMaps.
		/// (A convenience class, for use with BasicSequenceExtractRule.)
		/// Input is of type CoreMap, output is templated type O.
		/// </remarks>
		/// <?/>
		public class CoreMapToListExtractRule<O> : SequenceMatchRules.IExtractRule<ICoreMap, O>
		{
			internal readonly SequenceMatchRules.IExtractRule<IList<ICoreMap>, O> extractRule;

			public CoreMapToListExtractRule(SequenceMatchRules.IExtractRule<IList<ICoreMap>, O> extractRule)
			{
				this.extractRule = extractRule;
			}

			public virtual bool Extract(ICoreMap cm, IList<O> @out)
			{
				return extractRule.Extract(Arrays.AsList(cm), @out);
			}
		}

		/// <summary>Extraction rule.</summary>
		/// <remarks>
		/// Extraction rule.
		/// Input is of type CoreMap, output is MatchedExpression.
		/// </remarks>
		public class BasicSequenceExtractRule : SequenceMatchRules.IExtractRule<IList<ICoreMap>, MatchedExpression>
		{
			internal readonly MatchedExpression.SingleAnnotationExtractor extractor;

			public BasicSequenceExtractRule(MatchedExpression.SingleAnnotationExtractor extractor)
			{
				this.extractor = extractor;
			}

			public virtual bool Extract<_T0>(IList<_T0> seq, IList<MatchedExpression> @out)
				where _T0 : ICoreMap
			{
				bool extracted = false;
				for (int i = 0; i < seq.Count; i++)
				{
					ICoreMap t = seq[i];
					IValue v = extractor.Apply(t);
					if (v != null)
					{
						MatchedExpression te = extractor.CreateMatchedExpression(Interval.ToInterval(i, i + 1, Interval.IntervalOpenEnd), null);
						@out.Add(te);
						extracted = true;
					}
				}
				return extracted;
			}
		}

		public class SequencePatternExtractRule<T, O> : SequenceMatchRules.IExtractRule<IList<T>, O>, Func<IList<T>, O>
		{
			internal readonly SequencePattern<T> pattern;

			internal readonly Func<ISequenceMatchResult<T>, O> extractor;

			internal readonly SequenceMatcher.FindType findType;

			internal readonly bool matchWithResult;

			public SequencePatternExtractRule(Env env, string regex, Func<ISequenceMatchResult<T>, O> extractor)
				: this(SequencePattern.Compile(env, regex), extractor)
			{
			}

			public SequencePatternExtractRule(SequencePattern<T> p, Func<ISequenceMatchResult<T>, O> extractor)
				: this(p, extractor, null, false)
			{
			}

			public SequencePatternExtractRule(SequencePattern<T> p, Func<ISequenceMatchResult<T>, O> extractor, SequenceMatcher.FindType findType, bool matchWithResult)
			{
				this.extractor = extractor;
				this.pattern = p;
				this.findType = findType;
				this.matchWithResult = matchWithResult;
			}

			public virtual bool Extract<_T0>(IList<_T0> seq, IList<O> @out)
				where _T0 : T
			{
				if (seq == null)
				{
					return false;
				}
				bool extracted = false;
				SequenceMatcher<T> m = pattern.GetMatcher(seq);
				if (findType != null)
				{
					m.SetFindType(findType);
				}
				m.SetMatchWithResult(matchWithResult);
				while (m.Find())
				{
					@out.Add(extractor.Apply(m));
					extracted = true;
				}
				// System.err.println("SequencePattern " + pattern + " of type " + pattern.getClass() + " matched on " + extracted);
				return extracted;
			}

			public virtual O Apply<_T0>(IList<_T0> seq)
				where _T0 : T
			{
				if (seq == null)
				{
					return null;
				}
				SequenceMatcher<T> m = pattern.GetMatcher(seq);
				m.SetMatchWithResult(matchWithResult);
				if (m.Matches())
				{
					return extractor.Apply(m);
				}
				else
				{
					return null;
				}
			}
		}

		public class MultiSequencePatternExtractRule<T, O> : SequenceMatchRules.IExtractRule<IList<T>, O>, Func<IList<T>, O>
		{
			internal readonly MultiPatternMatcher<T> matcher;

			internal readonly Func<ISequenceMatchResult<T>, O> extractor;

			public MultiSequencePatternExtractRule(MultiPatternMatcher<T> matcher, Func<ISequenceMatchResult<T>, O> extractor)
			{
				// end static class SequencePatternExtractRule
				this.extractor = extractor;
				this.matcher = matcher;
			}

			public virtual bool Extract<_T0>(IList<_T0> seq, IList<O> @out)
				where _T0 : T
			{
				if (seq == null)
				{
					return false;
				}
				bool extracted = false;
				IList<ISequenceMatchResult<T>> matched = matcher.FindNonOverlappingMaxScore(seq);
				foreach (ISequenceMatchResult<T> m in matched)
				{
					@out.Add(extractor.Apply(m));
					extracted = true;
				}
				return extracted;
			}

			public virtual O Apply<_T0>(IList<_T0> seq)
				where _T0 : T
			{
				if (seq == null)
				{
					return null;
				}
				IList<ISequenceMatchResult<T>> matched = matcher.FindNonOverlappingMaxScore(seq);
				if (!matched.IsEmpty())
				{
					return extractor.Apply(matched[0]);
				}
				else
				{
					return null;
				}
			}
		}

		public class StringPatternExtractRule<O> : SequenceMatchRules.IExtractRule<string, O>, Func<string, O>
		{
			private readonly Pattern pattern;

			private readonly Func<IMatchResult, O> extractor;

			public StringPatternExtractRule(Pattern pattern, Func<IMatchResult, O> extractor)
			{
				this.pattern = pattern;
				this.extractor = extractor;
			}

			public StringPatternExtractRule(Env env, string regex, Func<IMatchResult, O> extractor)
				: this(env, regex, extractor, false)
			{
			}

			public StringPatternExtractRule(string regex, Func<IMatchResult, O> extractor)
				: this(null, regex, extractor, false)
			{
			}

			public StringPatternExtractRule(Env env, string regex, Func<IMatchResult, O> extractor, bool addWordBoundaries)
			{
				this.extractor = extractor;
				if (addWordBoundaries)
				{
					regex = "\\b(?:" + regex + ")\\b";
				}
				if (env != null)
				{
					pattern = env.GetStringPattern(regex);
				}
				else
				{
					pattern = Pattern.Compile(regex);
				}
			}

			public virtual bool Extract(string str, IList<O> @out)
			{
				if (str == null)
				{
					return false;
				}
				bool extracted = false;
				Matcher m = pattern.Matcher(str);
				while (m.Find())
				{
					@out.Add(extractor.Apply(m));
					// System.err.println("StringPatternExtractRule: " + pattern + " extracted " + out.get(out.size() - 1)); // XXXX
					extracted = true;
				}
				return extracted;
			}

			public virtual O Apply(string str)
			{
				if (str == null)
				{
					return null;
				}
				Matcher m = pattern.Matcher(str);
				if (m.Matches())
				{
					return extractor.Apply(m);
				}
				else
				{
					return null;
				}
			}
		}

		public class StringMatchedExpressionExtractor : Func<IMatchResult, MatchedExpression>
		{
			internal readonly MatchedExpression.SingleAnnotationExtractor extractor;

			internal readonly int group;

			public StringMatchedExpressionExtractor(MatchedExpression.SingleAnnotationExtractor extractor, int group)
			{
				// end static class StringPatternExtractRule
				this.extractor = extractor;
				this.group = group;
			}

			public virtual MatchedExpression Apply(IMatchResult matched)
			{
				MatchedExpression te = extractor.CreateMatchedExpression(Interval.ToInterval(matched.Start(group), matched.End(group), Interval.IntervalOpenEnd), null);
				return te;
			}
		}

		public class SequenceMatchedExpressionExtractor : Func<ISequenceMatchResult<ICoreMap>, MatchedExpression>
		{
			internal readonly MatchedExpression.SingleAnnotationExtractor extractor;

			internal readonly int group;

			public SequenceMatchedExpressionExtractor(MatchedExpression.SingleAnnotationExtractor extractor, int group)
			{
				this.extractor = extractor;
				this.group = group;
			}

			public virtual MatchedExpression Apply(ISequenceMatchResult<ICoreMap> matched)
			{
				MatchedExpression te = extractor.CreateMatchedExpression(null, Interval.ToInterval(matched.Start(group), matched.End(group), Interval.IntervalOpenEnd));
				if (double.IsNaN(te.priority))
				{
					te.priority = matched.Priority();
				}
				if (double.IsNaN(te.weight))
				{
					te.weight = matched.Score();
				}
				if (this.group != 0)
				{
					// Save context so value evaluation can happen
					te.context = matched.ToBasicSequenceMatchResult();
				}
				return te;
			}
		}

		public class CoreMapFunctionApplier<T, O> : Func<ICoreMap, O>
		{
			internal readonly Env env;

			internal readonly Type annotationField;

			internal readonly Func<T, O> func;

			public CoreMapFunctionApplier(Env env, Type annotationField, Func<T, O> func)
			{
				this.annotationField = annotationField;
				if (annotationField == null)
				{
					throw new ArgumentException("Annotation field cannot be null");
				}
				this.func = func;
				this.env = env;
			}

			public virtual O Apply(ICoreMap cm)
			{
				if (env != null)
				{
					env.Push(Expressions.VarSelf, cm);
				}
				try
				{
					T field = (T)cm.Get(annotationField);
					return func.Apply(field);
				}
				finally
				{
					if (env != null)
					{
						env.Pop(Expressions.VarSelf);
					}
				}
			}
		}

		public class CoreMapToListFunctionApplier<O> : Func<ICoreMap, O>
		{
			internal readonly Env env;

			internal readonly Func<IList<ICoreMap>, O> func;

			public CoreMapToListFunctionApplier(Env env, Func<IList<ICoreMap>, O> func)
			{
				this.func = func;
				this.env = env;
			}

			public virtual O Apply(ICoreMap cm)
			{
				if (env != null)
				{
					env.Push(Expressions.VarSelf, cm);
				}
				try
				{
					return func.Apply(Java.Util.Collections.SingletonList(cm));
				}
				finally
				{
					if (env != null)
					{
						env.Pop(Expressions.VarSelf);
					}
				}
			}
		}
		// end static class CoreMapToListFunctionApplier
	}
}

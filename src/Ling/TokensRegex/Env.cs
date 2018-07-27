using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;






namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Holds environment variables to be used for compiling string into a pattern.</summary>
	/// <remarks>
	/// Holds environment variables to be used for compiling string into a pattern.
	/// Use
	/// <see cref="EnvLookup"/>
	/// to perform actual lookup (it will provide reasonable defaults).
	/// <p>
	/// Some of the types of variables to bind are:
	/// </p>
	/// <ul>
	/// <li>
	/// <c>SequencePattern</c>
	/// (compiled pattern)</li>
	/// <li>
	/// <c>PatternExpr</c>
	/// (sequence pattern expression - precompiled)</li>
	/// <li>
	/// <c>NodePattern</c>
	/// (pattern for matching one element)</li>
	/// <li>
	/// <c>Class</c>
	/// (binding of CoreMap attribute to java Class)</li>
	/// </ul>
	/// </remarks>
	/// <author>Angel Chang</author>
	public class Env
	{
		/// <summary>Parser that converts a string into a SequencePattern.</summary>
		/// <seealso cref="Edu.Stanford.Nlp.Ling.Tokensregex.Parser.TokenSequenceParser"/>
		internal SequencePattern.IParser parser;

		/// <summary>Mapping of variable names to their values</summary>
		private IDictionary<string, object> variables = new Dictionary<string, object>();

		/// <summary>Mapping of per thread temporary variables to their values.</summary>
		private ThreadLocal<IDictionary<string, object>> threadLocalVariables = new ThreadLocal<IDictionary<string, object>>();

		/// <summary>
		/// Mapping of variables that can be expanded in a regular expression for strings,
		/// to their regular expressions.
		/// </summary>
		/// <remarks>
		/// Mapping of variables that can be expanded in a regular expression for strings,
		/// to their regular expressions.
		/// The variable name must start with "$" and include only the alphanumeric characters
		/// (it should follow the pattern
		/// <c>$[A-Za-z0-9_]+</c>
		/// ).
		/// Each variable is mapped to a pair, consisting of the
		/// <c>Pattern</c>
		/// representing
		/// the name of the variable to be replaced, and a
		/// <c>String</c>
		/// representing the
		/// regular expression (escaped) that is used to replace the name of the variable.
		/// </remarks>
		private IDictionary<string, Pair<Pattern, string>> stringRegexVariables = new Dictionary<string, Pair<Pattern, string>>();

		/// <summary>
		/// Default parameters (used when reading in rules for
		/// <see cref="SequenceMatchRules"/>
		/// .
		/// </summary>
		public IDictionary<string, object> defaults = new Dictionary<string, object>();

		/// <summary>Default flags to use for string regular expressions match</summary>
		/// <seealso cref="Java.Util.Regex.Pattern.Compile(string, int)"/>
		public int defaultStringPatternFlags = 0;

		/// <summary>Default flags to use for string literal match</summary>
		/// <seealso cref="NodePattern{T}.CaseInsensitive"/>
		public int defaultStringMatchFlags = 0;

		public Type sequenceMatchResultExtractor;

		public Type stringMatchResultExtractor;

		/// <summary>Annotation key to use to getting tokens (default is CoreAnnotations.TokensAnnotation.class)</summary>
		public Type defaultTokensAnnotationKey;

		/// <summary>Annotation key to use to getting text (default is CoreAnnotations.TextAnnotation.class)</summary>
		public Type defaultTextAnnotationKey;

		/// <summary>List of keys indicating the per-token annotations (default is null).</summary>
		/// <remarks>
		/// List of keys indicating the per-token annotations (default is null).
		/// If specified, each token will be annotated with the extracted results from the
		/// <see cref="defaultResultsAnnotationExtractor"/>
		/// .
		/// If null, then individual tokens that are matched are not annotated.
		/// </remarks>
		public IList<Type> defaultTokensResultAnnotationKey;

		/// <summary>List of keys indicating what fields should be annotated for the aggregated CoreMap.</summary>
		/// <remarks>
		/// List of keys indicating what fields should be annotated for the aggregated CoreMap.
		/// If specified, the aggregated CoreMap is annotated with the extracted results from the
		/// <see cref="defaultResultsAnnotationExtractor"/>
		/// .
		/// If null, then the aggregated CoreMap is not annotated.
		/// </remarks>
		public IList<Type> defaultResultAnnotationKey;

		/// <summary>Annotation key to use during composite phase for storing matched sequences and to match against.</summary>
		public Type defaultNestedResultsAnnotationKey;

		/// <summary>How should the tokens be aggregated when collapsing a sequence of tokens into one CoreMap</summary>
		public IDictionary<Type, CoreMapAttributeAggregator> defaultTokensAggregators;

		private CoreMapAggregator defaultTokensAggregator;

		/// <summary>Whether we should merge and output CoreLabels or not.</summary>
		public bool aggregateToTokens;

		/// <summary>How annotations are extracted from the MatchedExpression.</summary>
		/// <remarks>
		/// How annotations are extracted from the MatchedExpression.
		/// If the result type is a List and more than one annotation key is specified,
		/// then the result is paired with the annotation key.
		/// Example: If annotation key is [ner,normalized] and result is [CITY,San Francisco]
		/// then the final CoreMap will have ner=CITY, normalized=San Francisco.
		/// Otherwise, the result is treated as one object (all keys will be assigned that value).
		/// </remarks>
		internal IFunction<MatchedExpression, object> defaultResultsAnnotationExtractor;

		/// <summary>Interface for performing custom binding of values to the environment</summary>
		public interface IBinder
		{
			// Various of the public variables in this class are instantiated by reflection from TokensRegex rules
			//Generics.newHashMap();
			//Generics.newHashMap();
			//Generics.newHashMap();
			void Init(string prefix, Properties props);

			void Bind(Env env);
		}

		public Env(SequencePattern.IParser p)
		{
			this.parser = p;
		}

		public virtual void InitDefaultBindings()
		{
			Bind("FALSE", Expressions.False);
			Bind("TRUE", Expressions.True);
			Bind("NIL", Expressions.Nil);
			Bind("ENV", this);
			Bind("tags", typeof(Tags.TagsAnnotation));
		}

		public virtual IDictionary<string, object> GetDefaults()
		{
			return defaults;
		}

		public virtual void SetDefaults(IDictionary<string, object> defaults)
		{
			this.defaults = defaults;
		}

		public virtual IDictionary<Type, CoreMapAttributeAggregator> GetDefaultTokensAggregators()
		{
			return defaultTokensAggregators;
		}

		public virtual void SetDefaultTokensAggregators(IDictionary<Type, CoreMapAttributeAggregator> defaultTokensAggregators)
		{
			this.defaultTokensAggregators = defaultTokensAggregators;
		}

		public virtual CoreMapAggregator GetDefaultTokensAggregator()
		{
			if (defaultTokensAggregator == null && (defaultTokensAggregators != null || aggregateToTokens))
			{
				CoreLabelTokenFactory tokenFactory = (aggregateToTokens) ? new CoreLabelTokenFactory() : null;
				IDictionary<Type, CoreMapAttributeAggregator> aggregators = defaultTokensAggregators;
				if (aggregators == null)
				{
					aggregators = CoreMapAttributeAggregator.DefaultNumericTokensAggregators;
				}
				defaultTokensAggregator = CoreMapAggregator.GetAggregator(aggregators, null, tokenFactory);
			}
			return defaultTokensAggregator;
		}

		public virtual Type GetDefaultTextAnnotationKey()
		{
			return defaultTextAnnotationKey;
		}

		public virtual void SetDefaultTextAnnotationKey(Type defaultTextAnnotationKey)
		{
			this.defaultTextAnnotationKey = defaultTextAnnotationKey;
		}

		public virtual Type GetDefaultTokensAnnotationKey()
		{
			return defaultTokensAnnotationKey;
		}

		public virtual void SetDefaultTokensAnnotationKey(Type defaultTokensAnnotationKey)
		{
			this.defaultTokensAnnotationKey = defaultTokensAnnotationKey;
		}

		public virtual IList<Type> GetDefaultTokensResultAnnotationKey()
		{
			return defaultTokensResultAnnotationKey;
		}

		public virtual void SetDefaultTokensResultAnnotationKey(params Type[] defaultTokensResultAnnotationKey)
		{
			this.defaultTokensResultAnnotationKey = Arrays.AsList(defaultTokensResultAnnotationKey);
		}

		public virtual void SetDefaultTokensResultAnnotationKey(IList<Type> defaultTokensResultAnnotationKey)
		{
			this.defaultTokensResultAnnotationKey = defaultTokensResultAnnotationKey;
		}

		public virtual IList<Type> GetDefaultResultAnnotationKey()
		{
			return defaultResultAnnotationKey;
		}

		public virtual void SetDefaultResultAnnotationKey(params Type[] defaultResultAnnotationKey)
		{
			this.defaultResultAnnotationKey = Arrays.AsList(defaultResultAnnotationKey);
		}

		public virtual void SetDefaultResultAnnotationKey(IList<Type> defaultResultAnnotationKey)
		{
			this.defaultResultAnnotationKey = defaultResultAnnotationKey;
		}

		public virtual Type GetDefaultNestedResultsAnnotationKey()
		{
			return defaultNestedResultsAnnotationKey;
		}

		public virtual void SetDefaultNestedResultsAnnotationKey(Type defaultNestedResultsAnnotationKey)
		{
			this.defaultNestedResultsAnnotationKey = defaultNestedResultsAnnotationKey;
		}

		public virtual IFunction<MatchedExpression, object> GetDefaultResultsAnnotationExtractor()
		{
			return defaultResultsAnnotationExtractor;
		}

		public virtual void SetDefaultResultsAnnotationExtractor(IFunction<MatchedExpression, object> defaultResultsAnnotationExtractor)
		{
			this.defaultResultsAnnotationExtractor = defaultResultsAnnotationExtractor;
		}

		public virtual Type GetSequenceMatchResultExtractor()
		{
			return sequenceMatchResultExtractor;
		}

		public virtual void SetSequenceMatchResultExtractor(Type sequenceMatchResultExtractor)
		{
			this.sequenceMatchResultExtractor = sequenceMatchResultExtractor;
		}

		public virtual Type GetStringMatchResultExtractor()
		{
			return stringMatchResultExtractor;
		}

		public virtual void SetStringMatchResultExtractor(Type stringMatchResultExtractor)
		{
			this.stringMatchResultExtractor = stringMatchResultExtractor;
		}

		public virtual IDictionary<string, object> GetVariables()
		{
			return variables;
		}

		public virtual void SetVariables(IDictionary<string, object> variables)
		{
			this.variables = variables;
		}

		public virtual void ClearVariables()
		{
			this.variables.Clear();
		}

		public virtual int GetDefaultStringPatternFlags()
		{
			return defaultStringPatternFlags;
		}

		public virtual void SetDefaultStringPatternFlags(int defaultStringPatternFlags)
		{
			this.defaultStringPatternFlags = defaultStringPatternFlags;
		}

		public virtual int GetDefaultStringMatchFlags()
		{
			return defaultStringMatchFlags;
		}

		public virtual void SetDefaultStringMatchFlags(int defaultStringMatchFlags)
		{
			this.defaultStringMatchFlags = defaultStringMatchFlags;
		}

		private static readonly Pattern StringRegexVarNamePattern = Pattern.Compile("\\$[A-Za-z0-9_]+");

		public virtual void BindStringRegex(string var, string regex)
		{
			// Enforce requirements on variable names ($alphanumeric_)
			if (!StringRegexVarNamePattern.Matcher(var).Matches())
			{
				throw new ArgumentException("StringRegex binding error: Invalid variable name " + var);
			}
			Pattern varPattern = Pattern.Compile(Pattern.Quote(var));
			string replace = Matcher.QuoteReplacement(regex);
			stringRegexVariables[var] = new Pair<Pattern, string>(varPattern, replace);
		}

		public virtual string ExpandStringRegex(string regex)
		{
			// Replace all variables in regex
			string expanded = regex;
			foreach (KeyValuePair<string, Pair<Pattern, string>> stringPairEntry in stringRegexVariables)
			{
				Pair<Pattern, string> p = stringPairEntry.Value;
				expanded = p.First().Matcher(expanded).ReplaceAll(p.Second());
			}
			return expanded;
		}

		public virtual Pattern GetStringPattern(string regex)
		{
			string expanded = ExpandStringRegex(regex);
			return Pattern.Compile(expanded, defaultStringPatternFlags);
		}

		public virtual void Bind(string name, object obj)
		{
			if (obj != null)
			{
				variables[name] = obj;
			}
			else
			{
				Sharpen.Collections.Remove(variables, name);
			}
		}

		public virtual void Bind(string name, SequencePattern pattern)
		{
			Bind(name, pattern.GetPatternExpr());
		}

		public virtual void Unbind(string name)
		{
			Bind(name, null);
		}

		public virtual NodePattern GetNodePattern(string name)
		{
			object obj = variables[name];
			if (obj != null)
			{
				if (obj is SequencePattern)
				{
					SequencePattern seqPattern = (SequencePattern)obj;
					if (seqPattern.GetPatternExpr() is SequencePattern.NodePatternExpr)
					{
						return ((SequencePattern.NodePatternExpr)seqPattern.GetPatternExpr()).nodePattern;
					}
					else
					{
						throw new Exception("Invalid node pattern class: " + seqPattern.GetPatternExpr().GetType() + " for variable " + name);
					}
				}
				else
				{
					if (obj is SequencePattern.NodePatternExpr)
					{
						SequencePattern.NodePatternExpr pe = (SequencePattern.NodePatternExpr)obj;
						return pe.nodePattern;
					}
					else
					{
						if (obj is NodePattern)
						{
							return (NodePattern)obj;
						}
						else
						{
							if (obj is string)
							{
								try
								{
									SequencePattern.NodePatternExpr pe = (SequencePattern.NodePatternExpr)parser.ParseNode(this, (string)obj);
									return pe.nodePattern;
								}
								catch (Exception pex)
								{
									throw new Exception("Error parsing " + obj + " to node pattern", pex);
								}
							}
							else
							{
								throw new Exception("Invalid node pattern variable class: " + obj.GetType() + " for variable " + name);
							}
						}
					}
				}
			}
			return null;
		}

		public virtual SequencePattern.PatternExpr GetSequencePatternExpr(string name, bool copy)
		{
			object obj = variables[name];
			if (obj != null)
			{
				if (obj is SequencePattern)
				{
					SequencePattern seqPattern = (SequencePattern)obj;
					return seqPattern.GetPatternExpr();
				}
				else
				{
					if (obj is SequencePattern.PatternExpr)
					{
						SequencePattern.PatternExpr pe = (SequencePattern.PatternExpr)obj;
						return (copy) ? pe.Copy() : pe;
					}
					else
					{
						if (obj is NodePattern)
						{
							return new SequencePattern.NodePatternExpr((NodePattern)obj);
						}
						else
						{
							if (obj is string)
							{
								try
								{
									return parser.ParseSequence(this, (string)obj);
								}
								catch (Exception pex)
								{
									throw new Exception("Error parsing " + obj + " to sequence pattern", pex);
								}
							}
							else
							{
								throw new Exception("Invalid sequence pattern variable class: " + obj.GetType());
							}
						}
					}
				}
			}
			return null;
		}

		public virtual object Get(string name)
		{
			return variables[name];
		}

		// Functions for storing temporary thread specific variables
		//  that are used when running tokensregex
		public virtual void Push(string name, object value)
		{
			IDictionary<string, object> vars = threadLocalVariables.Get();
			if (vars == null)
			{
				threadLocalVariables.Set(vars = new Dictionary<string, object>());
			}
			//Generics.newHashMap());
			Stack<object> stack = (Stack<object>)vars[name];
			if (stack == null)
			{
				vars[name] = stack = new Stack<object>();
			}
			stack.Push(value);
		}

		public virtual object Pop(string name)
		{
			IDictionary<string, object> vars = threadLocalVariables.Get();
			if (vars == null)
			{
				return null;
			}
			Stack<object> stack = (Stack<object>)vars[name];
			if (stack == null || stack.IsEmpty())
			{
				return null;
			}
			else
			{
				return stack.Pop();
			}
		}

		public virtual object Peek(string name)
		{
			IDictionary<string, object> vars = threadLocalVariables.Get();
			if (vars == null)
			{
				return null;
			}
			Stack<object> stack = (Stack<object>)vars[name];
			if (stack == null || stack.IsEmpty())
			{
				return null;
			}
			else
			{
				return stack.Peek();
			}
		}
	}
}

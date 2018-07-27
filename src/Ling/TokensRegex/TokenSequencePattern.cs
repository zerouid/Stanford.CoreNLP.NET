using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling.Tokensregex.Parser;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>
	/// Token Sequence Pattern for regular expressions over sequences of tokens (each represented as a
	/// <c>CoreMap</c>
	/// ).
	/// Sequences over tokens can be matched like strings.
	/// <p>
	/// To use:
	/// </p>
	/// <pre>
	/// <c>
	/// TokenSequencePattern p = TokenSequencePattern.compile("....");
	/// TokenSequenceMatcher m = p.getMatcher(tokens);
	/// while (m.find()) ....
	/// </c>
	/// </pre>
	/// <p>
	/// Supports the following:
	/// <ul>
	/// <li>Concatenation:
	/// <c>X Y</c>
	/// </li>
	/// <li>Or:
	/// <c>X | Y</c>
	/// </li>
	/// <li>And:
	/// <c>X & Y</c>
	/// </li>
	/// <li>Groups:
	/// <ul>
	/// <li>capturing:
	/// <c>(X)</c>
	/// (with numeric group id)</li>
	/// <li>capturing:
	/// <c>(?$var X)</c>
	/// (with group name "$var")</li>
	/// <li>noncapturing:
	/// <c>(?:X)</c>
	/// </li>
	/// </ul>
	/// Capturing groups can be retrieved with group id or group variable, as matched string
	/// (
	/// <c>m.group()</c>
	/// ) or list of tokens (
	/// <c>m.groupNodes()</c>
	/// ).
	/// <ul>
	/// <li>To retrieve group using id:
	/// <c>m.group(id)</c>
	/// or
	/// <c>m.groupNodes(id)</c>
	/// <br /> NOTE: Capturing groups are indexed from left to right, starting at one.  Group zero is the entire matched sequence.
	/// </li>
	/// <li>To retrieve group using bound variable name:
	/// <c>m.group("$var")</c>
	/// or
	/// <c>m.groupNodes("$var")</c>
	/// </li>
	/// </ul>
	/// See
	/// <see cref="ISequenceMatchResult{T}"/>
	/// for more accessor functions to retrieve matches.
	/// </li>
	/// <li>Greedy Quantifiers:
	/// <c/>
	/// X+, X?, X*, X{n,m}, X{n}, X{n,}}</li>
	/// <li>Reluctant Quantifiers:
	/// <c/>
	/// X+?, X??, X*?, X{n,m}?, X{n}?, X{n,}?}</li>
	/// <li>Back references:
	/// <c>\captureid</c>
	/// </li>
	/// <li>Value binding for groups:
	/// <c>[pattern] =&gt; [value]</c>
	/// .
	/// Value for matched expression can be accessed using
	/// <c>m.groupValue()</c>
	/// <br />Example:
	/// <c>( one =&gt; 1 | two =&gt; 2 | three =&gt; 3 | ...)</c>
	/// </li>
	/// </ul>
	/// <p>
	/// Individual tokens are marked by
	/// <c>"[" TOKEN_EXPR "]"</c>
	/// <br />Possible
	/// <c>TOKEN_EXPR</c>
	/// :
	/// </p>
	/// <ul>
	/// <li> All specified token attributes match:
	/// <br /> For Strings:
	/// <c/>
	/// { lemma:/.../; tag:"NNP" } } = attributes that need to all match.
	/// If only one attribute, the {} can be dropped.
	/// <br /> See
	/// <see cref="Edu.Stanford.Nlp.Ling.AnnotationLookup">AnnotationLookup</see>
	/// for a list of predefined token attribute names.
	/// <br /> Additional attributes can be bound using the environment (see below).
	/// <br /> NOTE:
	/// <c>/.../</c>
	/// used for regular expressions,
	/// <c>"..."</c>
	/// for exact string matches
	/// <br /> For Numbers:
	/// <c/>
	/// { word&gt;=2 }}
	/// <br /> NOTE: Relation can be
	/// <c>"&gt;=", "&lt;=", "&gt;", "&lt;",</c>
	/// or
	/// <c>"=="</c>
	/// <br /> Others:
	/// <c/>
	/// { word::IS_NUM } , { word::IS_NIL } } or
	/// <c/>
	/// { word::NOT_EXISTS }, { word::NOT_NIL } } or
	/// <c/>
	/// { word::EXISTS } }
	/// </li>
	/// <li>Short hand for just word/text match:
	/// <c>/.../</c>
	/// or
	/// <c>"..."</c>
	/// </li>
	/// <li>
	/// Negation:
	/// <c/>
	/// !{...} }
	/// </li>
	/// <li>
	/// Conjunction or Disjunction:
	/// <c/>
	/// {...} & {...} }   or
	/// <c/>
	/// {...} | {...} }
	/// </li>
	/// </ul>
	/// <p>
	/// Special tokens:
	/// Any token:
	/// <c>[]</c>
	/// </p>
	/// <p>
	/// String pattern match across multiple tokens:
	/// <c/>
	/// (?m){min,max} /pattern/}
	/// </p>
	/// <p>
	/// Special expressions: indicated by double braces:
	/// <c/>
	/// {{ expr }}}
	/// <br /> See
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.Types.Expressions"/>
	/// for syntax.
	/// </p>
	/// <p>
	/// Binding of variables for use in compiling patterns:
	/// </p>
	/// <ol>
	/// <li> Use
	/// <c>Env env = TokenSequencePattern.getNewEnv()</c>
	/// to create a new environment for binding </li>
	/// <li> Bind string to attribute key (Class) lookup:
	/// <c>env.bind("numtype", CoreAnnotations.NumericTypeAnnotation.class);</c>
	/// </li>
	/// <li> Bind patterns / strings for compiling patterns
	/// <pre>
	/// <c>
	/// // Bind string for later compilation using: compile("/it/ /was/ $RELDAY");
	/// env.bind("$RELDAY", "/today|yesterday|tomorrow|tonight|tonite/");
	/// // Bind pre-compiled patter for later compilation using: compile("/it/ /was/ $RELDAY");
	/// env.bind("$RELDAY", TokenSequencePattern.compile(env, "/today|yesterday|tomorrow|tonight|tonite/"));
	/// </c>
	/// </pre>
	/// </li>
	/// <li> Bind custom node pattern functions (currently no arguments are supported)
	/// <pre>
	/// <c>
	/// // Bind node pattern so we can do patterns like: compile("... temporal::IS_TIMEX_DATE ...");
	/// //   (TimexTypeMatchNodePattern is a NodePattern that implements some custom logic)
	/// env.bind("::IS_TIMEX_DATE", new TimexTypeMatchNodePattern(SUTime.TimexType.DATE));
	/// </c>
	/// </pre>
	/// </li>
	/// </ol>
	/// <p>
	/// Actions (partially implemented)
	/// </p>
	/// <ul>
	/// <li>
	/// <c>pattern ==&gt; action</c>
	/// </li>
	/// <li> Supported action:
	/// <c/>
	/// &annotate( { ner="DATE" } ) } </li>
	/// <li> Not applied automatically, associated with a pattern.</li>
	/// <li> To apply, call
	/// <c>pattern.getAction().apply(match, groupid)</c>
	/// </li>
	/// </ul>
	/// </summary>
	/// <author>Angel Chang</author>
	/// <seealso cref="TokenSequenceMatcher"/>
	[System.Serializable]
	public class TokenSequencePattern : SequencePattern<ICoreMap>
	{
		private const long serialVersionUID = -4760710834202406916L;

		public static readonly Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern AnyNodePattern = ((Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern)Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern.Compile(AnyNodePatternExpr));

		private static readonly Env DefaultEnv = GetNewEnv();

		public TokenSequencePattern(string patternStr, SequencePattern.PatternExpr nodeSequencePattern)
			: base(patternStr, nodeSequencePattern)
		{
		}

		public TokenSequencePattern(string patternStr, SequencePattern.PatternExpr nodeSequencePattern, ISequenceMatchAction<ICoreMap> action)
			: base(patternStr, nodeSequencePattern, action)
		{
		}

		public static Env GetNewEnv()
		{
			Env env = new Env(new TokenSequenceParser());
			env.InitDefaultBindings();
			return env;
		}

		/// <summary>
		/// Compiles a regular expression over tokens into a TokenSequencePattern
		/// using the default environment.
		/// </summary>
		/// <param name="string">Regular expression to be compiled</param>
		/// <returns>Compiled TokenSequencePattern</returns>
		public static Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern Compile(string @string)
		{
			return ((Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern)Compile(DefaultEnv, @string));
		}

		/// <summary>
		/// Compiles a regular expression over tokens into a TokenSequencePattern
		/// using the specified environment.
		/// </summary>
		/// <param name="env">Environment to use</param>
		/// <param name="string">Regular expression to be compiled</param>
		/// <returns>Compiled TokenSequencePattern</returns>
		public static SequencePattern<T> Compile(Env env, string @string)
		{
			try
			{
				//      SequencePattern.PatternExpr nodeSequencePattern = TokenSequenceParser.parseSequence(env, string);
				//      return new TokenSequencePattern(string, nodeSequencePattern);
				// TODO: Check token sequence parser?
				Pair<SequencePattern.PatternExpr, ISequenceMatchAction<ICoreMap>> p = env.parser.ParseSequenceWithAction(env, @string);
				return new Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern(@string, p.First(), p.Second());
			}
			catch (Exception ex)
			{
				throw new Exception("Error when parsing " + @string, ex);
			}
		}

		/// <summary>
		/// Compiles a sequence of regular expressions into a TokenSequencePattern
		/// using the default environment.
		/// </summary>
		/// <param name="strings">List of regular expression to be compiled</param>
		/// <returns>Compiled TokenSequencePattern</returns>
		public static Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern Compile(params string[] strings)
		{
			return Compile(DefaultEnv, strings);
		}

		/// <summary>
		/// Compiles a sequence of regular expressions into a TokenSequencePattern
		/// using the specified environment.
		/// </summary>
		/// <param name="env">Environment to use</param>
		/// <param name="strings">List of regular expression to be compiled</param>
		/// <returns>Compiled TokenSequencePattern</returns>
		public static Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern Compile(Env env, params string[] strings)
		{
			try
			{
				IList<SequencePattern.PatternExpr> patterns = new List<SequencePattern.PatternExpr>();
				foreach (string @string in strings)
				{
					// TODO: Check token sequence parser?
					SequencePattern.PatternExpr pattern = env.parser.ParseSequence(env, @string);
					patterns.Add(pattern);
				}
				SequencePattern.PatternExpr nodeSequencePattern = new SequencePattern.SequencePatternExpr(patterns);
				return new Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern(StringUtils.Join(strings), nodeSequencePattern);
			}
			catch (Exception ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>Compiles a PatternExpr into a TokenSequencePattern.</summary>
		/// <param name="nodeSequencePattern">A sequence pattern expression (before translation into a NFA)</param>
		/// <returns>Compiled TokenSequencePattern</returns>
		protected internal static SequencePattern<T> Compile(SequencePattern.PatternExpr nodeSequencePattern)
		{
			return new Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern(null, nodeSequencePattern);
		}

		/// <summary>
		/// Returns a TokenSequenceMatcher that can be used to match this pattern
		/// against the specified list of tokens.
		/// </summary>
		/// <param name="tokens">List of tokens to match against</param>
		/// <returns>TokenSequenceMatcher</returns>
		public override SequenceMatcher<ICoreMap> GetMatcher<_T0>(IList<_T0> tokens)
		{
			return new TokenSequenceMatcher(this, tokens);
		}

		/// <summary>
		/// Returns a TokenSequenceMatcher that can be used to match this pattern
		/// against the specified list of tokens.
		/// </summary>
		/// <param name="tokens">List of tokens to match against</param>
		/// <returns>TokenSequenceMatcher</returns>
		public virtual TokenSequenceMatcher Matcher<_T0>(IList<_T0> tokens)
			where _T0 : ICoreMap
		{
			return ((TokenSequenceMatcher)GetMatcher(tokens));
		}

		/// <summary>Returns a String representation of the TokenSequencePattern.</summary>
		/// <returns>A String representation of the TokenSequencePattern</returns>
		public override string ToString()
		{
			return this.Pattern();
		}

		/// <summary>Create a multi-pattern matcher for matching across multiple TokensRegex patterns.</summary>
		/// <param name="patterns">Collection of input patterns</param>
		/// <returns>A MultiPatternMatcher</returns>
		public static MultiPatternMatcher<ICoreMap> GetMultiPatternMatcher(ICollection<Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern> patterns)
		{
			return new MultiPatternMatcher<ICoreMap>(new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns)), patterns);
		}

		/// <summary>Create a multi-pattern matcher for matching across multiple TokensRegex patterns.</summary>
		/// <param name="patterns">Input patterns</param>
		/// <returns>A MultiPatternMatcher</returns>
		public static MultiPatternMatcher<ICoreMap> GetMultiPatternMatcher(params Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern[] patterns)
		{
			return new MultiPatternMatcher<ICoreMap>(new MultiPatternMatcher.BasicSequencePatternTrigger<ICoreMap>(new CoreMapNodePatternTrigger(patterns)), patterns);
		}

		/// <summary>Create a multi-pattern matcher for matching across multiple TokensRegex patterns from Strings.</summary>
		/// <param name="patterns">Input patterns in String format</param>
		/// <returns>A MultiPatternMatcher</returns>
		public static MultiPatternMatcher<ICoreMap> GetMultiPatternMatcher(params string[] patterns)
		{
			IList<Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern> tokenSequencePatterns = Arrays.Stream(patterns).Map(null).Collect(Collectors.ToList());
			return Edu.Stanford.Nlp.Ling.Tokensregex.TokenSequencePattern.GetMultiPatternMatcher(tokenSequencePatterns);
		}
	}
}

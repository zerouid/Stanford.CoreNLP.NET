using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Uses TokensRegex patterns to annotate tokens.</summary>
	/// <remarks>
	/// Uses TokensRegex patterns to annotate tokens.
	/// <p>
	/// Configuration:
	/// <ul>
	/// <li>
	/// <c>rules</c>
	/// - Name of file containing extraction rules
	/// (see
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapExpressionExtractor{T}"/>
	/// and
	/// <see cref="Edu.Stanford.Nlp.Ling.Tokensregex.SequenceMatchRules"/>
	/// </li>
	/// </ul>
	/// Other options (can be set in rules file using
	/// <c>options.xxx = ...</c>
	/// )
	/// <ul>
	/// <li>
	/// <c>setTokenOffsets</c>
	/// - whether to explicit set the token offsets of individual tokens (needed to token sequence matches to work)</li>
	/// <li>
	/// <c>extractWithTokens</c>
	/// - whether to return unmatched tokens as well</li>
	/// <li>
	/// <c>flatten</c>
	/// - whether to flatten matched expressions into individual tokens</li>
	/// <li>
	/// <c>matchedExpressionsAnnotationKey</c>
	/// - Annotation key where matched expressions are stored as a list</li>
	/// </ul>
	/// </p>
	/// <p>Multiple
	/// <c>TokensRegexAnnotator</c>
	/// can be configured using the same properties file by specifying
	/// difference prefix for the
	/// <c>TokensRegexAnnotator</c>
	/// </p>
	/// </remarks>
	/// <author>Angel Chang</author>
	public class TokensRegexAnnotator : IAnnotator
	{
		private readonly Env env;

		private readonly CoreMapExpressionExtractor<MatchedExpression> extractor;

		private readonly TokensRegexAnnotator.Options options = new TokensRegexAnnotator.Options();

		private readonly bool verbose;

		public class Options
		{
			public Type matchedExpressionsAnnotationKey;

			public bool setTokenOffsets;

			public bool extractWithTokens;

			public bool flatten;
			// Make public so can be accessed and set via reflection
		}

		public TokensRegexAnnotator(params string[] files)
		{
			env = TokenSequencePattern.GetNewEnv();
			extractor = CoreMapExpressionExtractor.CreateExtractorFromFiles(env, files);
			verbose = false;
		}

		public TokensRegexAnnotator(string name, Properties props)
		{
			string prefix = (name == null) ? string.Empty : name + '.';
			string[] files = PropertiesUtils.GetStringArray(props, prefix + "rules");
			env = TokenSequencePattern.GetNewEnv();
			env.Bind("options", options);
			if (PropertiesUtils.GetBool(props, prefix + "caseInsensitive"))
			{
				System.Console.Error.WriteLine("using case insensitive!");
				env.SetDefaultStringMatchFlags(NodePattern.CaseInsensitive | Pattern.UnicodeCase);
				env.SetDefaultStringPatternFlags(Pattern.CaseInsensitive | Pattern.UnicodeCase);
			}
			if (files.Length != 0)
			{
				extractor = CoreMapExpressionExtractor.CreateExtractorFromFiles(env, files);
			}
			else
			{
				extractor = null;
			}
			verbose = PropertiesUtils.GetBool(props, prefix + "verbose", false);
			options.setTokenOffsets = PropertiesUtils.GetBool(props, prefix + "setTokenOffsets", options.setTokenOffsets);
			options.extractWithTokens = PropertiesUtils.GetBool(props, prefix + "extractWithTokens", options.extractWithTokens);
			options.flatten = PropertiesUtils.GetBool(props, prefix + "flatten", options.flatten);
			string matchedExpressionsAnnotationKeyName = props.GetProperty(prefix + "matchedExpressionsAnnotationKey");
			if (matchedExpressionsAnnotationKeyName != null)
			{
				options.matchedExpressionsAnnotationKey = EnvLookup.LookupAnnotationKeyWithClassname(env, matchedExpressionsAnnotationKeyName);
				if (options.matchedExpressionsAnnotationKey == null)
				{
					string propName = prefix + "matchedExpressionsAnnotationKey";
					throw new Exception("Cannot determine annotation key for " + propName + '=' + matchedExpressionsAnnotationKeyName);
				}
			}
		}

		public TokensRegexAnnotator(Properties props)
			: this(null, props)
		{
		}

		private static void AddTokenOffsets(ICoreMap annotation)
		{
			// We are going to mark the token begin and token end for each token
			int startTokenOffset = annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (startTokenOffset == null)
			{
				startTokenOffset = 0;
			}
			//set token offsets
			int i = 0;
			foreach (ICoreMap c in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				//set token begin
				c.Set(typeof(CoreAnnotations.TokenBeginAnnotation), i + startTokenOffset);
				i++;
				//set token end
				c.Set(typeof(CoreAnnotations.TokenEndAnnotation), i + startTokenOffset);
			}
		}

		private IList<ICoreMap> Extract(ICoreMap annotation)
		{
			IList<ICoreMap> cms;
			if (options.extractWithTokens)
			{
				cms = extractor.ExtractCoreMapsMergedWithTokens(annotation);
			}
			else
			{
				cms = extractor.ExtractCoreMaps(annotation);
			}
			if (options.flatten)
			{
				return extractor.Flatten(cms);
			}
			else
			{
				return cms;
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (verbose)
			{
				Redwood.Log(Redwood.Dbg, "Adding TokensRegexAnnotator annotation...");
			}
			if (options.setTokenOffsets)
			{
				AddTokenOffsets(annotation);
			}
			// just do nothing if no extractor is specified
			if (extractor != null)
			{
				IList<ICoreMap> allMatched;
				if (annotation.ContainsKey(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					allMatched = new List<ICoreMap>();
					IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
					foreach (ICoreMap sentence in sentences)
					{
						IList<ICoreMap> matched = Extract(sentence);
						if (matched != null && options.matchedExpressionsAnnotationKey != null)
						{
							Sharpen.Collections.AddAll(allMatched, matched);
							sentence.Set(options.matchedExpressionsAnnotationKey, matched);
							foreach (ICoreMap cm in matched)
							{
								cm.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
							}
						}
					}
				}
				else
				{
					allMatched = Extract(annotation);
				}
				if (options.matchedExpressionsAnnotationKey != null)
				{
					annotation.Set(options.matchedExpressionsAnnotationKey, allMatched);
				}
			}
			if (verbose)
			{
				Redwood.Log(Redwood.Dbg, "done.");
			}
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.TokensAnnotation));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			// TODO: not sure what goes here
			return Java.Util.Collections.EmptySet();
		}
	}
}

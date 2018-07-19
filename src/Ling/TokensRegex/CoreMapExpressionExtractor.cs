using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex.Parser;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Represents a list of assignment and extraction rules over sequence patterns.</summary>
	/// <remarks>
	/// Represents a list of assignment and extraction rules over sequence patterns.
	/// See
	/// <see cref="SequenceMatchRules"/>
	/// for the syntax of rules.
	/// <p>
	/// Assignment rules are used to assign a value to a variable for later use in
	/// extraction rules or for expansions in patterns.
	/// <p>
	/// Extraction rules are used to extract text/tokens matching regular expressions.
	/// Extraction rules are grouped into stages, with each stage consisting of the following:
	/// <ol>
	/// <li>Matching of rules over <b>text</b> and <b>tokens</b>.  These rules are applied directly on the <b>text</b> and <b>tokens</b> fields of the
	/// <c>CoreMap</c>
	/// .</li>
	/// <li>Matching of <b>composite</b> rules.  Matched expression are merged, and composite rules
	/// are applied recursively until no more changes to the matched expressions are detected.</li>
	/// <li><b>Filtering</b> of an invalid expression.  In the final phase, a final filtering stage filters out invalid expressions.</li>
	/// </ol>
	/// The different stages are numbered and are applied in numeric order.
	/// </remarks>
	/// <author>Angel Chang</author>
	/// <seealso cref="SequenceMatchRules"/>
	public class CoreMapExpressionExtractor<T>
		where T : MatchedExpression
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Ling.Tokensregex.CoreMapExpressionExtractor));

		private static bool verbose = false;

		private readonly Env env;

		private bool keepTags = false;

		private bool collapseExtractionRules = false;

		private readonly Type tokensAnnotationKey;

		private readonly IDictionary<int, CoreMapExpressionExtractor.Stage<T>> stages;

		/// <summary>Describes one stage of extraction.</summary>
		/// <?/>
		public class Stage<T>
		{
			/// <summary>Whether to clear matched expressions from previous stages or not</summary>
			internal bool clearMatched = false;

			/// <summary>
			/// Limit the number of iterations for which the composite rules are applied
			/// (prevents badly formed rules from iterating forever)
			/// </summary>
			internal int limitIters = 50;

			/// <summary>Stage id (stages are applied in numeric order from low to high)</summary>
			internal int stageId;

			/// <summary>Rules to extract matched  expressions directly from tokens</summary>
			internal SequenceMatchRules.IExtractRule<ICoreMap, T> basicExtractRule;

			/// <summary>Rules to extract composite expressions (grouped in stages)</summary>
			internal SequenceMatchRules.IExtractRule<IList<ICoreMap>, T> compositeExtractRule;

			/// <summary>Filtering rule</summary>
			internal IPredicate<T> filterRule;

			// TODO: Remove templating of MatchedExpressions<?>  (keep for now until TimeExpression rules can be decoupled)
			/* Keeps temporary tags created by extractor */
			/* Collapses extraction rules - use with care */
			private static SequenceMatchRules.IExtractRule<I, O> AddRule<I, O>(SequenceMatchRules.IExtractRule<I, O> origRule, SequenceMatchRules.IExtractRule<I, O> rule)
			{
				SequenceMatchRules.ListExtractRule<I, O> r;
				if (origRule is SequenceMatchRules.ListExtractRule)
				{
					r = (SequenceMatchRules.ListExtractRule<I, O>)origRule;
				}
				else
				{
					r = new SequenceMatchRules.ListExtractRule<I, O>();
					if (origRule != null)
					{
						r.AddRules(origRule);
					}
				}
				r.AddRules(rule);
				return r;
			}

			private void AddCompositeRule(SequenceMatchRules.IExtractRule<IList<ICoreMap>, T> rule)
			{
				compositeExtractRule = AddRule(compositeExtractRule, rule);
			}

			private void AddBasicRule(SequenceMatchRules.IExtractRule<ICoreMap, T> rule)
			{
				basicExtractRule = AddRule(basicExtractRule, rule);
			}

			private void AddFilterRule(IPredicate<T> rule)
			{
				Filters.DisjFilter<T> r;
				if (filterRule is Filters.DisjFilter)
				{
					r = (Filters.DisjFilter<T>)filterRule;
					r.AddFilter(rule);
				}
				else
				{
					if (filterRule == null)
					{
						r = new Filters.DisjFilter<T>(rule);
					}
					else
					{
						r = new Filters.DisjFilter<T>(filterRule, rule);
					}
					filterRule = r;
				}
			}
		}

		/// <summary>Creates an empty instance with no rules.</summary>
		public CoreMapExpressionExtractor()
			: this(null)
		{
		}

		/// <summary>Creates a default instance with the specified environment.</summary>
		/// <remarks>
		/// Creates a default instance with the specified environment.
		/// (use the default tokens annotation key as specified in the environment)
		/// </remarks>
		/// <param name="env">Environment to use for binding variables and applying rules</param>
		public CoreMapExpressionExtractor(Env env)
		{
			this.stages = new Dictionary<int, CoreMapExpressionExtractor.Stage<T>>();
			//Generics.newHashMap();
			this.env = env;
			this.tokensAnnotationKey = EnvLookup.GetDefaultTokensAnnotationKey(env);
			this.collapseExtractionRules = false;
			if (env != null)
			{
				this.collapseExtractionRules = Objects.Equals((bool)env.Get("collapseExtractionRules"), true);
				if (env.Get("verbose") != null)
				{
					verbose = (env.Get("verbose") != null) && Objects.Equals((bool)env.Get("verbose"), true);
				}
			}
		}

		/// <summary>Creates an instance with the specified environment and list of rules</summary>
		/// <param name="env">Environment to use for binding variables and applying rules</param>
		/// <param name="rules">List of rules for this extractor</param>
		public CoreMapExpressionExtractor(Env env, IList<SequenceMatchRules.IRule> rules)
			: this(env)
		{
			AppendRules(rules);
		}

		/// <summary>Add specified rules to this extractor.</summary>
		/// <param name="rules"/>
		public virtual void AppendRules(IList<SequenceMatchRules.IRule> rules)
		{
			if (verbose)
			{
				log.Info("Read " + rules.Count + " rules");
			}
			// Put rules into stages
			if (collapseExtractionRules)
			{
				rules = Collapse(rules);
				if (verbose)
				{
					log.Info("Collapsing into " + rules.Count + " rules");
				}
			}
			foreach (SequenceMatchRules.IRule r in rules)
			{
				if (r is SequenceMatchRules.AssignmentRule)
				{
					// Nothing to do
					// Assignments are added to environment as they are parsed
					((SequenceMatchRules.AssignmentRule)r).Evaluate(env);
				}
				else
				{
					if (r is SequenceMatchRules.AnnotationExtractRule)
					{
						SequenceMatchRules.AnnotationExtractRule aer = (SequenceMatchRules.AnnotationExtractRule)r;
						CoreMapExpressionExtractor.Stage<T> stage = stages[aer.stage];
						if (stage == null)
						{
							stages[aer.stage] = stage = new CoreMapExpressionExtractor.Stage<T>();
							stage.stageId = aer.stage;
							bool clearMatched = (bool)env.GetDefaults()["stage.clearMatched"];
							if (clearMatched != null)
							{
								stage.clearMatched = clearMatched;
							}
							int limitIters = (int)env.GetDefaults()["stage.limitIters"];
							if (limitIters != null)
							{
								stage.limitIters = limitIters;
							}
						}
						if (aer.active)
						{
							if (SequenceMatchRules.FilterRuleType.Equals(aer.ruleType))
							{
								stage.AddFilterRule(aer);
							}
							else
							{
								if (aer.isComposite)
								{
									//            if (SequenceMatchRules.COMPOSITE_RULE_TYPE.equals(aer.ruleType)) {
									stage.AddCompositeRule(aer);
								}
								else
								{
									stage.AddBasicRule(aer);
								}
							}
						}
						else
						{
							log.Debug("Ignoring inactive rule: " + aer.name);
						}
					}
				}
			}
		}

		// used to be INFO but annoyed Chris/users
		private SequenceMatchRules.AnnotationExtractRule CreateMergedRule(SequenceMatchRules.AnnotationExtractRule aerTemplate, IList<TokenSequencePattern> patterns)
		{
			return SequenceMatchRules.CreateMultiTokenPatternRule(env, aerTemplate, patterns);
		}

		private IList<SequenceMatchRules.IRule> Collapse(IList<SequenceMatchRules.IRule> rules)
		{
			IList<SequenceMatchRules.IRule> collapsed = new List<SequenceMatchRules.IRule>();
			IList<TokenSequencePattern> patterns = null;
			SequenceMatchRules.AnnotationExtractRule aerTemplate = null;
			foreach (SequenceMatchRules.IRule rule in rules)
			{
				bool ruleHandled = false;
				if (rule is SequenceMatchRules.AnnotationExtractRule)
				{
					SequenceMatchRules.AnnotationExtractRule aer = (SequenceMatchRules.AnnotationExtractRule)rule;
					if (aer.HasTokensRegexPattern())
					{
						if (aerTemplate == null || aerTemplate.IsMostlyCompatible(aer))
						{
							if (aerTemplate == null)
							{
								aerTemplate = aer;
							}
							if (patterns == null)
							{
								patterns = new List<TokenSequencePattern>();
							}
							patterns.Add((TokenSequencePattern)aer.pattern);
							ruleHandled = true;
						}
					}
				}
				// Did we handle this rule?
				if (!ruleHandled)
				{
					if (aerTemplate != null)
					{
						SequenceMatchRules.AnnotationExtractRule merged = CreateMergedRule(aerTemplate, patterns);
						collapsed.Add(merged);
						aerTemplate = null;
						patterns = null;
					}
					collapsed.Add(rule);
				}
			}
			if (aerTemplate != null)
			{
				SequenceMatchRules.AnnotationExtractRule merged = CreateMergedRule(aerTemplate, patterns);
				collapsed.Add(merged);
			}
			return collapsed;
		}

		public virtual Env GetEnv()
		{
			return env;
		}

		public virtual void SetExtractRules(SequenceMatchRules.IExtractRule<ICoreMap, T> basicExtractRule, SequenceMatchRules.IExtractRule<IList<ICoreMap>, T> compositeExtractRule, IPredicate<T> filterRule)
		{
			CoreMapExpressionExtractor.Stage<T> stage = new CoreMapExpressionExtractor.Stage<T>();
			stage.basicExtractRule = basicExtractRule;
			stage.compositeExtractRule = compositeExtractRule;
			stage.filterRule = filterRule;
			this.stages.Clear();
			this.stages[1] = stage;
		}

		/// <summary>Creates an extractor using the specified environment, and reading the rules from the given filenames.</summary>
		/// <param name="env"/>
		/// <param name="filenames"/>
		/// <exception cref="System.Exception"/>
		public static CoreMapExpressionExtractor<M> CreateExtractorFromFiles<M>(Env env, params string[] filenames)
			where M : MatchedExpression
		{
			return CreateExtractorFromFiles(env, Arrays.AsList(filenames));
		}

		/// <summary>Creates an extractor using the specified environment, and reading the rules from the given filenames.</summary>
		/// <param name="env"/>
		/// <param name="filenames"/>
		/// <exception cref="System.Exception"/>
		public static CoreMapExpressionExtractor<M> CreateExtractorFromFiles<M>(Env env, IList<string> filenames)
			where M : MatchedExpression
		{
			CoreMapExpressionExtractor<M> extractor = new CoreMapExpressionExtractor<M>(env);
			foreach (string filename in filenames)
			{
				try
				{
					using (BufferedReader br = IOUtils.ReaderFromString(filename))
					{
						if (verbose)
						{
							log.Info("Reading TokensRegex rules from " + filename);
						}
						TokenSequenceParser parser = new TokenSequenceParser();
						parser.UpdateExpressionExtractor(extractor, br);
					}
				}
				catch (Exception ex)
				{
					throw new Exception("Error parsing file: " + filename, ex);
				}
			}
			return extractor;
		}

		/// <summary>Creates an extractor using the specified environment, and reading the rules from the given filename.</summary>
		/// <param name="env"/>
		/// <param name="filename"/>
		/// <exception cref="System.Exception"/>
		public static CoreMapExpressionExtractor CreateExtractorFromFile(Env env, string filename)
		{
			return CreateExtractorFromFiles(env, Java.Util.Collections.SingletonList(filename));
		}

		/// <summary>Creates an extractor using the specified environment, and reading the rules from the given string</summary>
		/// <param name="env"/>
		/// <param name="str"/>
		/// <exception cref="System.IO.IOException">, ParseException</exception>
		/// <exception cref="Edu.Stanford.Nlp.Ling.Tokensregex.Parser.ParseException"/>
		/// <exception cref="Edu.Stanford.Nlp.Ling.Tokensregex.Parser.TokenSequenceParseException"/>
		public static CoreMapExpressionExtractor CreateExtractorFromString(Env env, string str)
		{
			TokenSequenceParser parser = new TokenSequenceParser();
			CoreMapExpressionExtractor extractor = parser.GetExpressionExtractor(env, new StringReader(str));
			return extractor;
		}

		public virtual IValue GetValue(string varname)
		{
			IExpression expr = (IExpression)env.Get(varname);
			if (expr != null)
			{
				return expr.Evaluate(env);
			}
			else
			{
				throw new Exception("Unable get expression for variable " + varname);
			}
		}

		private IList<ICoreMap> ExtractCoreMapsToList(IList<ICoreMap> res, ICoreMap annotation)
		{
			IList<T> exprs = ExtractExpressions(annotation);
			foreach (T expr in exprs)
			{
				res.Add(expr.GetAnnotation());
			}
			return res;
		}

		/// <summary>Returns list of coremaps that matches the specified rules.</summary>
		/// <param name="annotation"/>
		public virtual IList<ICoreMap> ExtractCoreMaps(ICoreMap annotation)
		{
			IList<ICoreMap> res = new List<ICoreMap>();
			return ExtractCoreMapsToList(res, annotation);
		}

		/// <summary>Returns list of merged tokens and original tokens.</summary>
		/// <param name="annotation"/>
		public virtual IList<ICoreMap> ExtractCoreMapsMergedWithTokens(ICoreMap annotation)
		{
			IList<ICoreMap> res = ExtractCoreMaps(annotation);
			int startTokenOffset = annotation.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
			if (startTokenOffset == null)
			{
				startTokenOffset = 0;
			}
			int startTokenOffsetFinal = startTokenOffset;
			IList<ICoreMap> merged = CollectionUtils.MergeListWithSortedMatchedPreAggregated(annotation.Get(tokensAnnotationKey), res, null);
			return merged;
		}

		public virtual IList<ICoreMap> Flatten(IList<ICoreMap> cms)
		{
			return Flatten(cms, tokensAnnotationKey);
		}

		private static IList<ICoreMap> Flatten(IList<ICoreMap> cms, Type key)
		{
			IList<ICoreMap> res = new List<ICoreMap>();
			foreach (ICoreMap cm in cms)
			{
				if (cm.Get(key) != null)
				{
					Sharpen.Collections.AddAll(res, (IList<ICoreMap>)cm.Get(key));
				}
				else
				{
					res.Add(cm);
				}
			}
			return res;
		}

		private void CleanupTags(ICollection objs, IDictionary<object, bool> cleaned)
		{
			foreach (object obj in objs)
			{
				if (!cleaned.Contains(obj))
				{
					cleaned[obj] = false;
					if (obj is ICoreMap)
					{
						CleanupTags((ICoreMap)obj, cleaned);
					}
					else
					{
						if (obj is ICollection)
						{
							CleanupTags((ICollection)obj, cleaned);
						}
					}
					cleaned[obj] = true;
				}
			}
		}

		private void CleanupTags(ICoreMap cm)
		{
			CleanupTags(cm, new IdentityHashMap<object, bool>());
		}

		private void CleanupTags(ICoreMap cm, IDictionary<object, bool> cleaned)
		{
			cm.Remove(typeof(Tags.TagsAnnotation));
			foreach (Type key in cm.KeySet())
			{
				object obj = cm.Get(key);
				if (!cleaned.Contains(obj))
				{
					cleaned[obj] = false;
					if (obj is ICoreMap)
					{
						CleanupTags((ICoreMap)obj, cleaned);
					}
					else
					{
						if (obj is ICollection)
						{
							CleanupTags((ICollection)obj, cleaned);
						}
					}
					cleaned[obj] = true;
				}
			}
		}

		private Pair<IList<ICoreMap>, IList<T>> ApplyCompositeRule<_T0>(SequenceMatchRules.IExtractRule<IList<ICoreMap>, T> compositeExtractRule, IList<_T0> merged, IList<T> matchedExpressions, int limit)
			where _T0 : ICoreMap
		{
			// Apply higher order rules
			bool done = false;
			// Limit of number of times rules are applied just in case
			int maxIters = limit;
			int iters = 0;
			while (!done)
			{
				IList<T> newExprs = new List<T>();
				bool extracted = compositeExtractRule.Extract(merged, newExprs);
				if (verbose && extracted)
				{
					log.Info("applyCompositeRule() extracting with " + compositeExtractRule + " from " + merged + " gives " + newExprs);
				}
				if (extracted)
				{
					AnnotateExpressions(merged, newExprs);
					newExprs = MatchedExpression.RemoveNullValues(newExprs);
					if (!newExprs.IsEmpty())
					{
						newExprs = MatchedExpression.RemoveNested(newExprs);
						newExprs = MatchedExpression.RemoveOverlapping(newExprs);
						merged = MatchedExpression.ReplaceMerged(merged, newExprs);
						// Favor newly matched expressions over older ones
						Sharpen.Collections.AddAll(newExprs, matchedExpressions);
						matchedExpressions = MatchedExpression.RemoveNested(newExprs);
						matchedExpressions = MatchedExpression.RemoveOverlapping(matchedExpressions);
					}
					else
					{
						extracted = false;
					}
				}
				done = !extracted;
				iters++;
				if (maxIters > 0 && iters >= maxIters)
				{
					if (verbose)
					{
						log.Warn("Aborting application of composite rules: Maximum iteration " + maxIters + " reached");
					}
					break;
				}
			}
			return new Pair<IList<ICoreMap>, IList<T>>(merged, matchedExpressions);
		}

		private class CompositeMatchState<T>
		{
			internal IList<ICoreMap> merged;

			internal IList<T> matched;

			internal int iters;

			private CompositeMatchState(IList<ICoreMap> merged, IList<T> matched, int iters)
			{
				this.merged = merged;
				this.matched = matched;
				this.iters = iters;
			}
		}

		public virtual IList<T> ExtractExpressions(ICoreMap annotation)
		{
			// Extract potential expressions
			IList<T> matchedExpressions = new List<T>();
			IList<int> stageIds = new List<int>(stages.Keys);
			stageIds.Sort();
			foreach (int stageId in stageIds)
			{
				CoreMapExpressionExtractor.Stage<T> stage = stages[stageId];
				SequenceMatchRules.IExtractRule<ICoreMap, T> basicExtractRule = stage.basicExtractRule;
				if (stage.clearMatched)
				{
					matchedExpressions.Clear();
				}
				if (basicExtractRule != null)
				{
					basicExtractRule.Extract(annotation, matchedExpressions);
					if (verbose && matchedExpressions != null)
					{
						log.Info("extractExpressions() extracting with " + basicExtractRule + " from " + annotation + " gives " + matchedExpressions);
					}
					AnnotateExpressions(annotation, matchedExpressions);
					matchedExpressions = MatchedExpression.RemoveNullValues(matchedExpressions);
					matchedExpressions = MatchedExpression.RemoveNested(matchedExpressions);
					matchedExpressions = MatchedExpression.RemoveOverlapping(matchedExpressions);
				}
				IList<ICoreMap> merged = MatchedExpression.ReplaceMergedUsingTokenOffsets(annotation.Get(tokensAnnotationKey), matchedExpressions);
				SequenceMatchRules.IExtractRule<IList<ICoreMap>, T> compositeExtractRule = stage.compositeExtractRule;
				if (compositeExtractRule != null)
				{
					Pair<IList<ICoreMap>, IList<T>> p = ApplyCompositeRule(compositeExtractRule, merged, matchedExpressions, stage.limitIters);
					merged = p.First();
					matchedExpressions = p.Second();
				}
				matchedExpressions = FilterInvalidExpressions(stage.filterRule, matchedExpressions);
			}
			matchedExpressions.Sort(MatchedExpression.ExprTokenOffsetsNestedFirstComparator);
			if (!keepTags)
			{
				CleanupTags(annotation);
			}
			return matchedExpressions;
		}

		private void AnnotateExpressions(ICoreMap annotation, IList<T> expressions)
		{
			// TODO: Logging can be excessive
			IList<T> toDiscard = new List<T>();
			foreach (T te in expressions)
			{
				// Add attributes and all
				if (te.annotation == null)
				{
					try
					{
						bool extractOkay = te.ExtractAnnotation(env, annotation);
						if (verbose && extractOkay)
						{
							log.Info("annotateExpressions() matched " + te + " from " + annotation);
						}
						if (!extractOkay)
						{
							// Things didn't turn out so well
							toDiscard.Add(te);
							log.Warn("Error extracting annotation from " + te);
						}
					}
					catch (Exception ex)
					{
						/*+ ", " + te.getExtractErrorMessage() */
						if (verbose)
						{
							log.Warn("Error extracting annotation from " + te);
							log.Warn(ex);
						}
					}
				}
			}
			expressions.RemoveAll(toDiscard);
		}

		private void AnnotateExpressions<_T0>(IList<_T0> chunks, IList<T> expressions)
			where _T0 : ICoreMap
		{
			// TODO: Logging can be excessive
			IList<T> toDiscard = new List<T>();
			foreach (T te in expressions)
			{
				// Add attributes and all
				try
				{
					bool extractOkay = te.ExtractAnnotation(env, chunks);
					if (verbose && extractOkay)
					{
						log.Info("annotateExpressions() matched " + te + " from " + chunks);
					}
					if (!extractOkay)
					{
						// Things didn't turn out so well
						toDiscard.Add(te);
						log.Warn("Error extracting annotation from " + te);
					}
				}
				catch (Exception ex)
				{
					/*+ ", " + te.getExtractErrorMessage() */
					if (verbose)
					{
						log.Warn("Error extracting annotation from " + te);
						log.Warn(ex);
					}
				}
			}
			expressions.RemoveAll(toDiscard);
		}

		private IList<T> FilterInvalidExpressions(IPredicate<T> filterRule, IList<T> expressions)
		{
			if (filterRule == null)
			{
				return expressions;
			}
			if (expressions.IsEmpty())
			{
				return expressions;
			}
			int nfiltered = 0;
			IList<T> kept = new List<T>(expressions.Count);
			// Approximate size
			foreach (T expr in expressions)
			{
				if (!filterRule.Test(expr))
				{
					kept.Add(expr);
				}
				else
				{
					nfiltered++;
				}
			}
			//        logger.warning("Filtering out " + expr.getText());
			if (nfiltered > 0 && verbose)
			{
				log.Debug("Filtered " + nfiltered);
			}
			return kept;
		}

		/// <summary>Keeps the temporary tags on the sentence after extraction has finished.</summary>
		/// <remarks>
		/// Keeps the temporary tags on the sentence after extraction has finished.
		/// This can have potentially unexpected results if you run the same sentence through multiple extractors;
		/// but, it makes the extraction process 20+% faster.
		/// </remarks>
		/// <returns>This object</returns>
		public virtual CoreMapExpressionExtractor KeepTemporaryTags()
		{
			this.keepTags = true;
			return this;
		}

		public static void SetVerbose(bool v)
		{
			verbose = v;
		}
	}
}

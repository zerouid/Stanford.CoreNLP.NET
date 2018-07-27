using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Extracts time expressions.</summary>
	/// <author>Angel Chang</author>
	public class TimeExpressionExtractorImpl : ITimeExpressionExtractor
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Time.TimeExpressionExtractorImpl));

		private ITimeExpressionPatterns timexPatterns;

		private CoreMapExpressionExtractor expressionExtractor;

		private Options options;

		public TimeExpressionExtractorImpl()
		{
			// Patterns for extracting time expressions
			// Options
			Init(new Options());
		}

		public TimeExpressionExtractorImpl(string name, Properties props)
		{
			Init(name, props);
		}

		public virtual void Init(string name, Properties props)
		{
			Init(new Options(name, props));
		}

		public virtual void Init(Options options)
		{
			this.options = options;
			// NumberNormalizer.setVerbose(options.verbose); // cdm 2016: Try omitting this: Don't we want to see errors?
			CoreMapExpressionExtractor.SetVerbose(options.verbose);
			if (options.grammarFilename == null)
			{
				options.grammarFilename = Options.DefaultGrammarFiles;
				logger.Warning("Time rules file is not specified: using default rules at " + options.grammarFilename);
			}
			logger.Info("Using following SUTime rules: " + options.grammarFilename);
			timexPatterns = new GenericTimeExpressionPatterns(options);
			this.expressionExtractor = timexPatterns.CreateExtractor();
		}

		public virtual IList<ICoreMap> ExtractTimeExpressionCoreMaps(ICoreMap annotation, ICoreMap docAnnotation)
		{
			SUTime.TimeIndex timeIndex;
			// initialized immediately below
			string docDate = null;
			if (docAnnotation != null)
			{
				timeIndex = docAnnotation.Get(typeof(TimeExpression.TimeIndexAnnotation));
				if (timeIndex == null)
				{
					docAnnotation.Set(typeof(TimeExpression.TimeIndexAnnotation), timeIndex = new SUTime.TimeIndex());
				}
				// default look for the sentence's forum post date
				// if it doesn't have one, back off to the document date
				if (annotation.Get(typeof(CoreAnnotations.SectionDateAnnotation)) != null)
				{
					docDate = annotation.Get(typeof(CoreAnnotations.SectionDateAnnotation));
				}
				else
				{
					docDate = docAnnotation.Get(typeof(CoreAnnotations.DocDateAnnotation));
				}
				if (docDate == null)
				{
					Calendar cal = docAnnotation.Get(typeof(CoreAnnotations.CalendarAnnotation));
					if (cal == null)
					{
						if (options.verbose)
						{
							logger.Warn("WARNING: No document date specified");
						}
					}
					else
					{
						SimpleDateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd:hh:mm:ss");
						docDate = dateFormat.Format(cal.GetTime());
					}
				}
			}
			else
			{
				timeIndex = new SUTime.TimeIndex();
			}
			if (StringUtils.IsNullOrEmpty(docDate))
			{
				docDate = null;
			}
			if (timeIndex.docDate == null && docDate != null)
			{
				try
				{
					// TODO: have more robust parsing of document date?  docDate may not have century....
					// TODO: if docDate didn't change, we can cache the parsing of the docDate and not repeat it for every sentence
					timeIndex.docDate = SUTime.ParseDateTime(docDate, true);
				}
				catch (Exception e)
				{
					throw new Exception("Could not parse date string: [" + docDate + "]", e);
				}
			}
			string sectionDate = annotation.Get(typeof(CoreAnnotations.SectionDateAnnotation));
			string refDate = (sectionDate != null) ? sectionDate : docDate;
			return ExtractTimeExpressionCoreMaps(annotation, refDate, timeIndex);
		}

		public virtual IList<ICoreMap> ExtractTimeExpressionCoreMaps(ICoreMap annotation, string docDate)
		{
			SUTime.TimeIndex timeIndex = new SUTime.TimeIndex();
			return ExtractTimeExpressionCoreMaps(annotation, docDate, timeIndex);
		}

		public virtual IList<ICoreMap> ExtractTimeExpressionCoreMaps(ICoreMap annotation, string docDate, SUTime.TimeIndex timeIndex)
		{
			IList<TimeExpression> timeExpressions = ExtractTimeExpressions(annotation, docDate, timeIndex);
			return ToCoreMaps(annotation, timeExpressions, timeIndex);
		}

		~TimeExpressionExtractorImpl()
		{
			docAnnotation.Remove(typeof(TimeExpression.TimeIndexAnnotation));
		}

		private IList<ICoreMap> ToCoreMaps(ICoreMap annotation, IList<TimeExpression> timeExpressions, SUTime.TimeIndex timeIndex)
		{
			if (timeExpressions == null)
			{
				return null;
			}
			IList<ICoreMap> coreMaps = new List<ICoreMap>(timeExpressions.Count);
			foreach (TimeExpression te in timeExpressions)
			{
				ICoreMap cm = te.GetAnnotation();
				SUTime.Temporal temporal = te.GetTemporal();
				if (temporal != null)
				{
					string origText = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
					string text = cm.Get(typeof(CoreAnnotations.TextAnnotation));
					if (origText != null)
					{
						// Make sure the text is from original (and not from concatenated tokens)
						ChunkAnnotationUtils.AnnotateChunkText(cm, annotation);
						text = cm.Get(typeof(CoreAnnotations.TextAnnotation));
					}
					IDictionary<string, string> timexAttributes;
					try
					{
						timexAttributes = temporal.GetTimexAttributes(timeIndex);
						if (options.includeRange)
						{
							SUTime.Temporal rangeTemporal = temporal.GetRange();
							if (rangeTemporal != null)
							{
								timexAttributes["range"] = rangeTemporal.ToString();
							}
						}
					}
					catch (Exception e)
					{
						if (options.verbose)
						{
							logger.Warn("Failed to get attributes from " + text + ", timeIndex " + timeIndex);
							logger.Warn(e);
						}
						continue;
					}
					Timex timex;
					try
					{
						timex = Timex.FromMap(text, timexAttributes);
					}
					catch (Exception e)
					{
						if (options.verbose)
						{
							logger.Warn("Failed to process timex " + text + " with attributes " + timexAttributes);
							logger.Warn(e);
						}
						continue;
					}
					System.Diagnostics.Debug.Assert(timex != null);
					// Timex.fromMap never returns null and if it exceptions, we've already done a continue
					cm.Set(typeof(TimeAnnotations.TimexAnnotation), timex);
					coreMaps.Add(cm);
				}
			}
			return coreMaps;
		}

		public virtual IList<TimeExpression> ExtractTimeExpressions(ICoreMap annotation, string refDateStr, SUTime.TimeIndex timeIndex)
		{
			SUTime.Time refDate = null;
			if (refDateStr != null)
			{
				try
				{
					// TODO: have more robust parsing of document date?  docDate may not have century....
					// TODO: if docDate didn't change, we can cache the parsing of the docDate and not repeat it for every sentence
					refDate = SUTime.ParseDateTime(refDateStr, true);
				}
				catch (Exception e)
				{
					throw new Exception("Could not parse date string: [" + refDateStr + "]", e);
				}
			}
			return ExtractTimeExpressions(annotation, refDate, timeIndex);
		}

		public virtual IList<TimeExpression> ExtractTimeExpressions(ICoreMap annotation, SUTime.Time refDate, SUTime.TimeIndex timeIndex)
		{
			if (!annotation.ContainsKey(typeof(CoreAnnotations.NumerizedTokensAnnotation)))
			{
				try
				{
					IList<ICoreMap> mergedNumbers = NumberNormalizer.FindAndMergeNumbers(annotation);
					annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), mergedNumbers);
				}
				catch (NumberFormatException e)
				{
					logger.Warn("Caught bad number: " + e.Message);
					annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), new List<ICoreMap>());
				}
			}
			IList<MatchedExpression> matchedExpressions = expressionExtractor.ExtractExpressions(annotation);
			IList<TimeExpression> timeExpressions = new List<TimeExpression>(matchedExpressions.Count);
			foreach (MatchedExpression expr in matchedExpressions)
			{
				// Make sure we have the correct type (instead of just MatchedExpression)
				//timeExpressions.add(TimeExpression.TimeExpressionConverter.apply(expr));
				// TODO: Fix the extraction pipeline so it creates TimeExpression instead of MatchedExpressions
				// For now, grab the time expression from the annotation (this is good, so we don't have duplicate copies)
				TimeExpression annoTe = expr.GetAnnotation().Get(typeof(TimeExpression.Annotation));
				if (annoTe != null)
				{
					timeExpressions.Add(annoTe);
				}
			}
			// We cache the document date in the timeIndex
			if (timeIndex.docDate == null)
			{
				if (refDate != null)
				{
					timeIndex.docDate = refDate;
				}
				else
				{
					if (options.searchForDocDate)
					{
						// there was no document date but option was set to look for document date
						timeIndex.docDate = FindReferenceDate(timeExpressions);
					}
				}
			}
			// Didn't have a reference date - try using cached doc date
			if (refDate == null)
			{
				refDate = timeIndex.docDate;
			}
			// Some resolving is done even if refDate null...
			ResolveTimeExpressions(annotation, timeExpressions, refDate);
			if (options.restrictToTimex3)
			{
				// Keep only TIMEX3 compatible timeExpressions
				IList<TimeExpression> kept = new List<TimeExpression>(timeExpressions.Count);
				foreach (TimeExpression te in timeExpressions)
				{
					if (te.GetTemporal() != null && te.GetTemporal().GetTimexValue() != null)
					{
						kept.Add(te);
					}
					else
					{
						IList<ICoreMap> children = te.GetAnnotation().Get(typeof(TimeExpression.ChildrenAnnotation));
						if (children != null)
						{
							foreach (ICoreMap child in children)
							{
								TimeExpression childTe = child.Get(typeof(TimeExpression.Annotation));
								if (childTe != null)
								{
									ResolveTimeExpression(annotation, childTe, refDate);
									if (childTe.GetTemporal() != null && childTe.GetTemporal().GetTimexValue() != null)
									{
										kept.Add(childTe);
									}
								}
							}
						}
					}
				}
				timeExpressions = kept;
			}
			// Add back nested time expressions for ranges....
			// For now only one level of nesting...
			if (options.includeNested)
			{
				IList<TimeExpression> nestedTimeExpressions = new List<TimeExpression>();
				foreach (TimeExpression te in timeExpressions)
				{
					if (te.IsIncludeNested())
					{
						IList<ICoreMap> children = te.GetAnnotation().Get(typeof(TimeExpression.ChildrenAnnotation));
						if (children != null)
						{
							foreach (ICoreMap child in children)
							{
								TimeExpression childTe = child.Get(typeof(TimeExpression.Annotation));
								if (childTe != null)
								{
									nestedTimeExpressions.Add(childTe);
								}
							}
						}
					}
				}
				ResolveTimeExpressions(annotation, nestedTimeExpressions, refDate);
				Sharpen.Collections.AddAll(timeExpressions, nestedTimeExpressions);
			}
			timeExpressions.Sort(MatchedExpression.ExprTokenOffsetsNestedFirstComparator);
			// Some resolving is done even if refDate null...
			ResolveTimeExpressions(annotation, timeExpressions, refDate);
			return timeExpressions;
		}

		private void ResolveTimeExpression(ICoreMap annotation, TimeExpression te, SUTime.Time docDate)
		{
			SUTime.Temporal temporal = te.GetTemporal();
			if (temporal != null)
			{
				// TODO: use correct time for anchor
				try
				{
					int flags = timexPatterns.DetermineRelFlags(annotation, te);
					//int flags = 0;
					SUTime.Temporal grounded = temporal.Resolve(docDate, flags);
					if (grounded == null)
					{
						logger.Debug("Error resolving " + temporal + ", using docDate=" + docDate);
					}
					if (grounded != temporal)
					{
						te.origTemporal = temporal;
						te.SetTemporal(grounded);
					}
				}
				catch (Exception ex)
				{
					if (options.verbose)
					{
						logger.Warn("Error resolving " + temporal, ex);
						logger.Warn(ex);
					}
				}
			}
		}

		private void ResolveTimeExpressions(ICoreMap annotation, IList<TimeExpression> timeExpressions, SUTime.Time docDate)
		{
			foreach (TimeExpression te in timeExpressions)
			{
				ResolveTimeExpression(annotation, te, docDate);
			}
		}

		private static SUTime.Time FindReferenceDate(IList<TimeExpression> timeExpressions)
		{
			// Find first full date in this annotation with year, month, and day
			foreach (TimeExpression te in timeExpressions)
			{
				SUTime.Temporal t = te.GetTemporal();
				if (t is SUTime.Time)
				{
					if (t.IsGrounded())
					{
						return t.GetTime();
					}
					else
					{
						if (t is SUTime.PartialTime)
						{
							if (JodaTimeUtils.HasYYYYMMDD(t.GetTime().GetJodaTimePartial()))
							{
								return t.GetTime();
							}
							else
							{
								if (JodaTimeUtils.HasYYMMDD(t.GetTime().GetJodaTimePartial()))
								{
									return t.GetTime().Resolve(SUTime.GetCurrentTime()).GetTime();
								}
							}
						}
					}
				}
			}
			return null;
		}
	}
}

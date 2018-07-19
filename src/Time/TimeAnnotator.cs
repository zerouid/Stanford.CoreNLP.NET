using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>
	/// Annotate temporal expressions in text with
	/// <see cref="SUTime"/>
	/// .
	/// The expressions recognized by SUTime are loosely based on GUTIME.
	/// After annotation, the
	/// <see cref="TimexAnnotations"/>
	/// annotation
	/// will be populated with a
	/// <c>List&lt;CoreMap&gt;</c>
	/// , each of which
	/// will represent one temporal expression.
	/// If a reference time is set (via
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.DocDateAnnotation"/>
	/// ),
	/// then temporal expressions are resolved with respect to the document date.  You set it on an
	/// Annotation as follows:
	/// <blockquote>
	/// <c>annotation.set(CoreAnnotations.DocDateAnnotation.class, "2013-07-14");</c>
	/// </blockquote>
	/// <p>
	/// <br />
	/// <b>Input annotations</b>
	/// <table border="1">
	/// <tr>
	/// <th>Annotation</th>
	/// <th>Type</th>
	/// <th>Description</th>
	/// <th>Required?</th>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.DocDateAnnotation"/>
	/// </td>
	/// <td>
	/// <c>String</c>
	/// </td>
	/// <td>If present, then the string is interpreted as a date/time and
	/// used as the reference document date with respect to which other
	/// temporal expressions are resolved</td>
	/// <td>Optional</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.SentencesAnnotation"/>
	/// </td>
	/// <td>
	/// <c>List&lt;CoreMap&gt;</c>
	/// </td>
	/// <td>If present, time expressions will be extracted from each sentence
	/// and each sentence will be annotated individually.</td>
	/// <td>Optional (good to have)</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TokensAnnotation"/>
	/// </td>
	/// <td>
	/// <c>List&lt;CoreLabel&gt;</c>
	/// </td>
	/// <td>Tokens (for each sentence or for entire annotation if no sentences)</td>
	/// <td>Required</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TextAnnotation"/>
	/// </td>
	/// <td>
	/// <c>String</c>
	/// </td>
	/// <td>Text (for each sentence or for entire annotation if no sentences)</td>
	/// <td>Optional</td>
	/// </tr>
	/// <tr><td colspan="4"><center><b>Per token annotations</b></center></td></tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TextAnnotation"/>
	/// </td>
	/// <td>
	/// <c>String</c>
	/// </td>
	/// <td>Token text (normalized)</td>
	/// <td>Required</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.OriginalTextAnnotation"/>
	/// </td>
	/// <td>
	/// <c>String</c>
	/// </td>
	/// <td>Token text (original)</td>
	/// <td>Required</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetBeginAnnotation"/>
	/// </td>
	/// <td>
	/// <c>Integer</c>
	/// </td>
	/// <td>The index of the first character of this token
	/// (0-based wrt to TextAnnotation of the annotation containing the TokensAnnotation).</td>
	/// <td>Required</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetEndAnnotation"/>
	/// </td>
	/// <td>
	/// <c>Integer</c>
	/// </td>
	/// <td>The index of the first character after this token
	/// (0-based wrt to TextAnnotation of the annotation containing the TokensAnnotation).</td>
	/// <td>Required</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.PartOfSpeechAnnotation"/>
	/// </td>
	/// <td>
	/// <c>String</c>
	/// </td>
	/// <td>Token part of speech</td>
	/// <td>Optional</td>
	/// </tr>
	/// </table>
	/// <p>
	/// <br />
	/// <b>Output annotations</b>
	/// <table border="1">
	/// <tr>
	/// <th>Annotation</th>
	/// <th>Type</th>
	/// <th>Description</th>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="TimexAnnotations"/>
	/// </td>
	/// <td>
	/// <c>List&lt;CoreMap&gt;</c>
	/// </td>
	/// <td>List of temporal expressions (on the entire annotation and also for each sentence)</td>
	/// </tr>
	/// <tr><td colspan="3"><center><b>Per each temporal expression</b></center></td></tr>
	/// <tr>
	/// <td>
	/// <see cref="TimexAnnotation"/>
	/// </td>
	/// <td>
	/// <see cref="Timex"/>
	/// </td>
	/// <td>Timex object with TIMEX3 XML attributes, use for exporting TIMEX3 information</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Annotation"/>
	/// </td>
	/// <td>
	/// <see cref="TimeExpression"/>
	/// </td>
	/// <td>TimeExpression object.  Use
	/// <c>getTemporal()</c>
	/// to get internal temporal representation.</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="ChildrenAnnotation"/>
	/// </td>
	/// <td>
	/// <c>List&lt;CoreMap&gt;</c>
	/// </td>
	/// <td>List of chunks forming this time expression (inner chunks can be tokens, nested time expressions,
	/// numeric expressions, etc)</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TextAnnotation"/>
	/// </td>
	/// <td>
	/// <c>String</c>
	/// </td>
	/// <td>Text of this time expression</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TokensAnnotation"/>
	/// </td>
	/// <td>
	/// <c>List&lt;CoreLabel&gt;</c>
	/// </td>
	/// <td>Tokens that make up this time expression</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetBeginAnnotation"/>
	/// </td>
	/// <td>
	/// <c>Integer</c>
	/// </td>
	/// <td>The index of the first character of this token (0-based).</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.CharacterOffsetEndAnnotation"/>
	/// </td>
	/// <td>
	/// <c>Integer</c>
	/// </td>
	/// <td>The index of the first character after this token (0-based).</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TokenBeginAnnotation"/>
	/// </td>
	/// <td>
	/// <c>Integer</c>
	/// </td>
	/// <td>The index of the first token of this time expression (0-based).</td>
	/// </tr>
	/// <tr>
	/// <td>
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations.TokenEndAnnotation"/>
	/// </td>
	/// <td>
	/// <c>Integer</c>
	/// </td>
	/// <td>The index of the first token after this time expression (0-based).</td>
	/// </tr>
	/// </table>
	/// </summary>
	public class TimeAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Time.TimeAnnotator));

		private readonly TimeExpressionExtractorImpl timexExtractor;

		private readonly bool quiet;

		public TimeAnnotator()
			: this(false)
		{
		}

		public TimeAnnotator(bool quiet)
		{
			timexExtractor = new TimeExpressionExtractorImpl();
			this.quiet = quiet;
		}

		public TimeAnnotator(string name, Properties props)
			: this(name, props, false)
		{
		}

		public TimeAnnotator(string name, Properties props, bool quiet)
		{
			timexExtractor = new TimeExpressionExtractorImpl(name, props);
			this.quiet = quiet;
		}

		public virtual void Annotate(Annotation annotation)
		{
			SUTime.TimeIndex timeIndex = new SUTime.TimeIndex();
			string docDate = annotation.Get(typeof(CoreAnnotations.DocDateAnnotation));
			if (docDate == null)
			{
				Calendar cal = annotation.Get(typeof(CoreAnnotations.CalendarAnnotation));
				if (cal == null)
				{
					if (!quiet)
					{
						log.Warn("No document date specified");
					}
				}
				else
				{
					SimpleDateFormat dateFormat = new SimpleDateFormat("yyyy-MM-dd:hh:mm:ss");
					docDate = dateFormat.Format(cal.GetTime());
				}
			}
			IList<ICoreMap> allTimeExpressions;
			// initialized below = null;
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences != null)
			{
				allTimeExpressions = new List<ICoreMap>();
				IList<ICoreMap> allNumerics = new List<ICoreMap>();
				foreach (ICoreMap sentence in sentences)
				{
					// make sure that token character offsets align with the actual sentence text
					// They may not align due to token normalizations, such as "(" to "-LRB-".
					ICoreMap alignedSentence = NumberSequenceClassifier.AlignSentence(sentence);
					// uncomment the next line for verbose dumping of tokens....
					// log.info("SENTENCE: " + ((ArrayCoreMap) sentence).toShorterString());
					IList<ICoreMap> timeExpressions = timexExtractor.ExtractTimeExpressionCoreMaps(alignedSentence, docDate, timeIndex);
					if (timeExpressions != null)
					{
						Sharpen.Collections.AddAll(allTimeExpressions, timeExpressions);
						sentence.Set(typeof(TimeAnnotations.TimexAnnotations), timeExpressions);
						foreach (ICoreMap timeExpression in timeExpressions)
						{
							timeExpression.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentence.Get(typeof(CoreAnnotations.SentenceIndexAnnotation)));
						}
					}
					IList<ICoreMap> numbers = alignedSentence.Get(typeof(CoreAnnotations.NumerizedTokensAnnotation));
					if (numbers != null)
					{
						sentence.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), numbers);
						Sharpen.Collections.AddAll(allNumerics, numbers);
					}
				}
				annotation.Set(typeof(CoreAnnotations.NumerizedTokensAnnotation), allNumerics);
			}
			else
			{
				allTimeExpressions = AnnotateSingleSentence(annotation, docDate, timeIndex);
			}
			annotation.Set(typeof(TimeAnnotations.TimexAnnotations), allTimeExpressions);
		}

		/// <summary>Helper method for people not working from a complete Annotation.</summary>
		/// <returns>A list of CoreMap.  Each CoreMap represents a detected temporal expression.</returns>
		public virtual IList<ICoreMap> AnnotateSingleSentence(ICoreMap sentence, string docDate, SUTime.TimeIndex timeIndex)
		{
			ICoreMap annotationCopy = NumberSequenceClassifier.AlignSentence(sentence);
			if (docDate != null && docDate.IsEmpty())
			{
				docDate = null;
			}
			return timexExtractor.ExtractTimeExpressionCoreMaps(annotationCopy, docDate, timeIndex);
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.TokensAnnotation));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(TimeAnnotations.TimexAnnotations));
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <summary>Simple wrapper around SUTime for parsing lots of strings outside of Annotation objects.</summary>
	/// <remarks>
	/// Simple wrapper around SUTime for parsing lots of strings outside of Annotation objects.
	/// Note that this class sets up its own small, static (i.e., global shared) annotation pipeline,
	/// which will always use the default English annotators, and which requires using a POS Tagger.
	/// </remarks>
	/// <author>David McClosky</author>
	public class SUTimeSimpleParser
	{
		private SUTimeSimpleParser()
		{
		}

		/// <summary>Indicates that any exception occurred inside the TimeAnnotator.</summary>
		/// <remarks>Indicates that any exception occurred inside the TimeAnnotator.  This should only be caused by bugs in SUTime.</remarks>
		[System.Serializable]
		public class SUTimeParsingError : Exception
		{
			private const long serialVersionUID = 1L;

			public readonly string timeExpression;

			public SUTimeParsingError(string timeExpression)
			{
				// static methods
				this.timeExpression = timeExpression;
			}

			public override string GetLocalizedMessage()
			{
				return "Error while parsing '" + timeExpression + '\'';
			}
		}

		private static readonly AnnotationPipeline pipeline;

		private static readonly IDictionary<string, SUTime.Temporal> cache;

		public static int calls;

		public static int misses;

		static SUTimeSimpleParser()
		{
			// = 0;
			// = 0;
			pipeline = MakeNumericPipeline();
			cache = Generics.NewHashMap();
		}

		private static AnnotationPipeline MakeNumericPipeline()
		{
			AnnotationPipeline pipeline = new AnnotationPipeline();
			pipeline.AddAnnotator(new TokenizerAnnotator(false, "en"));
			pipeline.AddAnnotator(new WordsToSentencesAnnotator(false));
			pipeline.AddAnnotator(new POSTaggerAnnotator(false));
			pipeline.AddAnnotator(new TimeAnnotator(true));
			return pipeline;
		}

		public static SUTime.Temporal ParseOrNull(string str)
		{
			Annotation doc = new Annotation(str);
			pipeline.Annotate(doc);
			if (doc.Get(typeof(CoreAnnotations.SentencesAnnotation)) == null)
			{
				return null;
			}
			if (doc.Get(typeof(CoreAnnotations.SentencesAnnotation)).IsEmpty())
			{
				return null;
			}
			IList<ICoreMap> timexAnnotations = doc.Get(typeof(TimeAnnotations.TimexAnnotations));
			if (timexAnnotations.Count > 1)
			{
				return null;
			}
			else
			{
				if (timexAnnotations.IsEmpty())
				{
					return null;
				}
			}
			ICoreMap timex = timexAnnotations[0];
			if (timex.Get(typeof(TimeExpression.Annotation)) == null)
			{
				return null;
			}
			else
			{
				return timex.Get(typeof(TimeExpression.Annotation)).GetTemporal();
			}
		}

		/// <summary>Parse a string with SUTime.</summary>
		/// <exception cref="SUTimeParsingError">if anything goes wrong</exception>
		/// <exception cref="Edu.Stanford.Nlp.Time.SUTimeSimpleParser.SUTimeParsingError"/>
		public static SUTime.Temporal Parse(string str)
		{
			try
			{
				Annotation doc = new Annotation(str);
				pipeline.Annotate(doc);
				System.Diagnostics.Debug.Assert(doc.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null);
				System.Diagnostics.Debug.Assert(!doc.Get(typeof(CoreAnnotations.SentencesAnnotation)).IsEmpty());
				IList<ICoreMap> timexAnnotations = doc.Get(typeof(TimeAnnotations.TimexAnnotations));
				if (timexAnnotations.Count > 1)
				{
					throw new Exception("Too many timexes for '" + str + '\'');
				}
				ICoreMap timex = timexAnnotations[0];
				return timex.Get(typeof(TimeExpression.Annotation)).GetTemporal();
			}
			catch (Exception e)
			{
				SUTimeSimpleParser.SUTimeParsingError parsingError = new SUTimeSimpleParser.SUTimeParsingError(str);
				parsingError.InitCause(e);
				throw parsingError;
			}
		}

		/// <summary>Cached wrapper of parse method.</summary>
		/// <exception cref="Edu.Stanford.Nlp.Time.SUTimeSimpleParser.SUTimeParsingError"/>
		public static SUTime.Temporal ParseUsingCache(string str)
		{
			calls++;
			if (!cache.Contains(str))
			{
				misses++;
				cache[str] = Parse(str);
			}
			return cache[str];
		}
	}
}

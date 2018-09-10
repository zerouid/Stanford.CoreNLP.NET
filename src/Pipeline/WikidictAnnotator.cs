using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An annotator for entity linking to Wikipedia pages via the Wikidict.</summary>
	/// <author>Gabor Angeli</author>
	public class WikidictAnnotator : SentenceAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.WikidictAnnotator));

		/// <summary>A pattern for simple numbers</summary>
		private static readonly Pattern NumberPattern = Pattern.Compile("[0-9.]+");

		private int threads = 1;

		private string wikidictPath = DefaultPaths.DefaultWikidictTsv;

		private double threshold = 0.0;

		private bool wikidictCaseless = false;

		/// <summary>The actual Wikidict dictionary.</summary>
		private readonly IDictionary<string, string> dictionary = new Dictionary<string, string>(21000000);

		/// <summary>Create a new WikiDict annotator, with the given name and properties.</summary>
		public WikidictAnnotator(string name, Properties properties)
		{
			// it's gonna be large no matter what
			ArgumentParser.FillOptions(this, name, properties);
			long startTime = Runtime.CurrentTimeMillis();
			log.Info("Reading Wikidict from " + wikidictPath);
			try
			{
				int i = 0;
				string[] fields = new string[3];
				foreach (string line in IOUtils.ReadLines(wikidictPath, "UTF-8"))
				{
					if (line[0] == '\t')
					{
						continue;
					}
					StringUtils.SplitOnChar(fields, line, '\t');
					if (i % 1000000 == 0)
					{
						log.Info("Loaded " + i + " entries from Wikidict [" + SystemUtils.GetMemoryInUse() + "MB memory used; " + Redwood.FormatTimeDifference(Runtime.CurrentTimeMillis() - startTime) + " elapsed]");
					}
					// Check that the read entry is above the score threshold
					if (threshold > 0.0)
					{
						double score = double.Parse(fields[2]);
						if (score < threshold)
						{
							continue;
						}
					}
					string surfaceForm = fields[0];
					if (wikidictCaseless)
					{
						surfaceForm = surfaceForm.ToLower();
					}
					string link = string.Intern(fields[1]);
					// intern, as most entities have multiple surface forms
					// Add the entry
					dictionary[surfaceForm] = link;
					i += 1;
				}
				log.Info("Done reading Wikidict (" + dictionary.Count + " links read; " + Redwood.FormatTimeDifference(Runtime.CurrentTimeMillis() - startTime) + " elapsed)");
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
		}

		/// <seealso cref="WikidictAnnotator(string, Java.Util.Properties)"></seealso>
		public WikidictAnnotator(Properties properties)
			: this(AnnotatorConstants.StanfordLink, properties)
		{
		}

		/// <summary>Try to normalize timex values to the form they would appear in the knowledge base.</summary>
		/// <param name="timex">The timex value to normalize.</param>
		/// <returns>The normalized timex value (e.g., dates have the time of day removed, etc.)</returns>
		public static string NormalizeTimex(string timex)
		{
			if (timex.Contains("T") && !"PRESENT".Equals(timex))
			{
				return Sharpen.Runtime.Substring(timex, 0, timex.IndexOf("T"));
			}
			else
			{
				return timex;
			}
		}

		/// <summary>Link the given mention, if possible.</summary>
		/// <param name="mention">
		/// The mention to link, as given by
		/// <see cref="EntityMentionsAnnotator"/>
		/// </param>
		/// <returns>The Wikidict entry for the given mention, or the normalized timex / numeric value -- as appropriate.</returns>
		public virtual Optional<string> Link(ICoreMap mention)
		{
			string surfaceForm = mention.Get(typeof(CoreAnnotations.OriginalTextAnnotation)) == null ? mention.Get(typeof(CoreAnnotations.TextAnnotation)) : mention.Get(typeof(CoreAnnotations.OriginalTextAnnotation));
			// set up key for wikidict ; if caseless use lower case version of surface form
			string mentionSurfaceFormKey;
			if (wikidictCaseless)
			{
				mentionSurfaceFormKey = surfaceForm.ToLower();
			}
			else
			{
				mentionSurfaceFormKey = surfaceForm;
			}
			// get ner
			string ner = mention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
			if (ner != null && (Sharpen.Runtime.EqualsIgnoreCase(KBPRelationExtractor.NERTag.Date.name, ner) || Sharpen.Runtime.EqualsIgnoreCase("TIME", ner) || Sharpen.Runtime.EqualsIgnoreCase("SET", ner)) && mention.Get(typeof(TimeAnnotations.TimexAnnotation
				)) != null && mention.Get(typeof(TimeAnnotations.TimexAnnotation)).Value() != null)
			{
				// Case: normalize dates
				Timex timex = mention.Get(typeof(TimeAnnotations.TimexAnnotation));
				if (timex.Value() != null && !timex.Value().Equals("PRESENT") && !timex.Value().Equals("PRESENT_REF") && !timex.Value().Equals("PAST") && !timex.Value().Equals("PAST_REF") && !timex.Value().Equals("FUTURE") && !timex.Value().Equals("FUTURE_REF"
					))
				{
					return Optional.Of(NormalizeTimex(timex.Value()));
				}
				else
				{
					return Optional.Empty();
				}
			}
			else
			{
				if (ner != null && Sharpen.Runtime.EqualsIgnoreCase("ORDINAL", ner) && mention.Get(typeof(CoreAnnotations.NumericValueAnnotation)) != null)
				{
					// Case: normalize ordinals
					Number numericValue = mention.Get(typeof(CoreAnnotations.NumericValueAnnotation));
					return Optional.Of(numericValue.ToString());
				}
				else
				{
					if (NumberPattern.Matcher(surfaceForm).Matches())
					{
						// Case: keep numbers as is
						return Optional.Of(surfaceForm);
					}
					else
					{
						if (ner != null && !"O".Equals(ner) && dictionary.Contains(mentionSurfaceFormKey))
						{
							// Case: link with Wikidict
							return Optional.Of(dictionary[mentionSurfaceFormKey]);
						}
						else
						{
							// Else: keep the surface form as is
							return Optional.Empty();
						}
					}
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override int NThreads()
		{
			return threads;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override long MaxTime()
		{
			return -1L;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override void DoOneSentence(Annotation annotation, ICoreMap sentence)
		{
			foreach (CoreLabel token in sentence.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				token.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), "O");
			}
			foreach (ICoreMap mention in sentence.Get(typeof(CoreAnnotations.MentionsAnnotation)))
			{
				Optional<string> canonicalName = Link(mention);
				if (canonicalName.IsPresent())
				{
					mention.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), canonicalName.Get());
					foreach (CoreLabel token_1 in mention.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						token_1.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), canonicalName.Get());
					}
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override void DoOneFailedSentence(Annotation annotation, ICoreMap sentence)
		{
		}

		/* do nothing */
		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.WikipediaEntityAnnotation));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override ICollection<Type> Requires()
		{
			ICollection<Type> requirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation), typeof(
				CoreAnnotations.MentionsAnnotation)));
			return Java.Util.Collections.UnmodifiableSet(requirements);
		}

		/// <summary>A debugging method to try entity linking sentences from the console.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			props.SetProperty("annotators", "tokenize,ssplit,pos,lemma,ner,entitymentions,entitylink");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			IOUtils.Console("sentence> ", null);
		}
	}
}

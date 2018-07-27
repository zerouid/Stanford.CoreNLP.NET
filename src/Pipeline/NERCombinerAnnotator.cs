using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IE.Regexp;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex.Types;
using Edu.Stanford.Nlp.Time;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will add NER information to an Annotation using a combination of NER models.</summary>
	/// <remarks>
	/// This class will add NER information to an Annotation using a combination of NER models.
	/// It assumes that the Annotation already contains the tokenized words in sentences
	/// under
	/// <c>CoreAnnotations.SentencesAnnotation.class</c>
	/// as
	/// <c>List&lt;? extends CoreLabel&gt;</c>
	/// } or a
	/// <c>List&lt;List&lt;? extends CoreLabel&gt;&gt;</c>
	/// under
	/// <c>Annotation.WORDS_KEY</c>
	/// and adds NER information to each CoreLabel,
	/// in the
	/// <c>CoreLabel.NER_KEY</c>
	/// field.  It uses
	/// the NERClassifierCombiner class in the ie package.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	/// <author>Mihai Surdeanu (modified it to work with the new NERClassifierCombiner)</author>
	public class NERCombinerAnnotator : SentenceAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.NERCombinerAnnotator));

		private readonly NERClassifierCombiner ner;

		private readonly bool Verbose;

		private bool usePresentDateForDocDate = false;

		private string providedDocDate = string.Empty;

		private readonly long maxTime;

		private readonly int nThreads;

		private readonly int maxSentenceLength;

		private LanguageInfo.HumanLanguage language = LanguageInfo.HumanLanguage.English;

		/// <summary>Spanish NER enhancements</summary>
		private static readonly IDictionary<string, string> spanishToEnglishTag = new Dictionary<string, string>();

		static NERCombinerAnnotator()
		{
			spanishToEnglishTag["PERS"] = "PERSON";
			spanishToEnglishTag["ORG"] = "ORGANIZATION";
			spanishToEnglishTag["LUG"] = "LOCATION";
			spanishToEnglishTag["OTROS"] = "MISC";
		}

		private const string spanishNumberRegexRules = "edu/stanford/nlp/models/kbp/spanish/gazetteers/kbp_regexner_number_sp.tag";

		private TokensRegexNERAnnotator spanishNumberAnnotator;

		/// <summary>fine grained ner</summary>
		private bool applyFineGrained = true;

		private TokensRegexNERAnnotator fineGrainedNERAnnotator;

		/// <summary>additional rules ner - add your own additional regexner rules after fine grained phase</summary>
		private bool applyAdditionalRules = true;

		private TokensRegexNERAnnotator additionalRulesNERAnnotator;

		/// <summary>entity mentions</summary>
		private bool buildEntityMentions = true;

		private EntityMentionsAnnotator entityMentionsAnnotator;

		/// <exception cref="System.IO.IOException"/>
		public NERCombinerAnnotator(Properties properties)
		{
			IList<string> models = new List<string>();
			string modelNames = properties.GetProperty("ner.model");
			if (modelNames == null)
			{
				modelNames = DefaultPaths.DefaultNerThreeclassModel + ',' + DefaultPaths.DefaultNerMucModel + ',' + DefaultPaths.DefaultNerConllModel;
			}
			if (!modelNames.IsEmpty())
			{
				Sharpen.Collections.AddAll(models, Arrays.AsList(modelNames.Split(",")));
			}
			if (models.IsEmpty())
			{
				// Allow for no real NER model - can just use numeric classifiers or SUTime.
				// Have to unset ner.model, so unlikely that people got here by accident.
				log.Info("WARNING: no NER models specified");
			}
			bool applyNumericClassifiers = PropertiesUtils.GetBool(properties, NERClassifierCombiner.ApplyNumericClassifiersProperty, NERClassifierCombiner.ApplyNumericClassifiersDefault);
			bool applyRegexner = PropertiesUtils.GetBool(properties, NERClassifierCombiner.ApplyGazetteProperty, NERClassifierCombiner.ApplyGazetteDefault);
			bool useSUTime = PropertiesUtils.GetBool(properties, NumberSequenceClassifier.UseSutimeProperty, NumberSequenceClassifier.UseSutimeDefault);
			// option for setting doc date to be the present during each annotation
			usePresentDateForDocDate = PropertiesUtils.GetBool(properties, "ner." + "usePresentDateForDocDate", false);
			// option for setting doc date from a provided string
			providedDocDate = PropertiesUtils.GetString(properties, "ner." + "providedDocDate", string.Empty);
			Pattern p = Pattern.Compile("[0-9]{4}\\-[0-9]{2}\\-[0-9]{2}");
			Matcher m = p.Matcher(providedDocDate);
			if (!m.Matches())
			{
				providedDocDate = string.Empty;
			}
			NERClassifierCombiner.Language nerLanguage = NERClassifierCombiner.Language.FromString(PropertiesUtils.GetString(properties, NERClassifierCombiner.NerLanguageProperty, null), NERClassifierCombiner.NerLanguageDefault);
			bool verbose = PropertiesUtils.GetBool(properties, "ner." + "verbose", false);
			string[] loadPaths = Sharpen.Collections.ToArray(models, new string[models.Count]);
			Properties combinerProperties = PropertiesUtils.ExtractSelectedProperties(properties, NERClassifierCombiner.DefaultPassDownProperties);
			if (useSUTime)
			{
				// Make sure SUTime parameters are included
				Properties sutimeProps = PropertiesUtils.ExtractPrefixedProperties(properties, NumberSequenceClassifier.SutimeProperty + '.', true);
				PropertiesUtils.OverWriteProperties(combinerProperties, sutimeProps);
			}
			NERClassifierCombiner nerCombiner = new NERClassifierCombiner(applyNumericClassifiers, nerLanguage, useSUTime, applyRegexner, combinerProperties, loadPaths);
			this.nThreads = PropertiesUtils.GetInt(properties, "ner.nthreads", PropertiesUtils.GetInt(properties, "nthreads", 1));
			this.maxTime = PropertiesUtils.GetLong(properties, "ner.maxtime", 0);
			this.maxSentenceLength = PropertiesUtils.GetInt(properties, "ner.maxlen", int.MaxValue);
			this.language = LanguageInfo.GetLanguageFromString(PropertiesUtils.GetString(properties, "ner.language", "en"));
			// in case of Spanish, use the Spanish number regexner annotator
			if (language.Equals(LanguageInfo.HumanLanguage.Spanish))
			{
				Properties spanishNumberRegexNerProperties = new Properties();
				spanishNumberRegexNerProperties["spanish.number.regexner.mapping"] = spanishNumberRegexRules;
				spanishNumberRegexNerProperties["spanish.number.regexner.validpospattern"] = "^(NUM).*";
				spanishNumberRegexNerProperties["spanish.number.regexner.ignorecase"] = "true";
				spanishNumberAnnotator = new TokensRegexNERAnnotator("spanish.number.regexner", spanishNumberRegexNerProperties);
			}
			// set up fine grained ner
			SetUpFineGrainedNER(properties);
			// set up additional rules ner
			SetUpAdditionalRulesNER(properties);
			// set up entity mentions
			SetUpEntityMentionBuilding(properties);
			Verbose = verbose;
			this.ner = nerCombiner;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public NERCombinerAnnotator()
			: this(true)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public NERCombinerAnnotator(bool verbose)
			: this(new NERClassifierCombiner(new Properties()), verbose)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public NERCombinerAnnotator(bool verbose, params string[] classifiers)
			: this(new NERClassifierCombiner(classifiers), verbose)
		{
		}

		public NERCombinerAnnotator(NERClassifierCombiner ner, bool verbose)
			: this(ner, verbose, 1, 0, int.MaxValue)
		{
		}

		public NERCombinerAnnotator(NERClassifierCombiner ner, bool verbose, int nThreads, long maxTime)
			: this(ner, verbose, nThreads, maxTime, int.MaxValue)
		{
		}

		public NERCombinerAnnotator(NERClassifierCombiner ner, bool verbose, int nThreads, long maxTime, int maxSentenceLength)
			: this(ner, verbose, nThreads, maxTime, maxSentenceLength, true, true)
		{
		}

		public NERCombinerAnnotator(NERClassifierCombiner ner, bool verbose, int nThreads, long maxTime, int maxSentenceLength, bool fineGrained, bool entityMentions)
		{
			Verbose = verbose;
			this.ner = ner;
			this.maxTime = maxTime;
			this.nThreads = nThreads;
			this.maxSentenceLength = maxSentenceLength;
			Properties nerProperties = new Properties();
			nerProperties.SetProperty("ner.applyFineGrained", bool.ToString(fineGrained));
			nerProperties.SetProperty("ner.buildEntityMentions", bool.ToString(entityMentions));
			SetUpAdditionalRulesNER(nerProperties);
			SetUpFineGrainedNER(nerProperties);
			SetUpEntityMentionBuilding(nerProperties);
		}

		public virtual void SetUpFineGrainedNER(Properties properties)
		{
			// set up fine grained ner
			this.applyFineGrained = PropertiesUtils.GetBool(properties, "ner.applyFineGrained", true);
			if (this.applyFineGrained)
			{
				string fineGrainedPrefix = "ner.fine.regexner";
				Properties fineGrainedProps = PropertiesUtils.ExtractPrefixedProperties(properties, fineGrainedPrefix + ".", true);
				// explicity set fine grained ner default here
				if (!fineGrainedProps.Contains("ner.fine.regexner.mapping"))
				{
					fineGrainedProps["ner.fine.regexner.mapping"] = DefaultPaths.DefaultKbpTokensregexNerSettings;
				}
				// build the fine grained ner TokensRegexNERAnnotator
				fineGrainedNERAnnotator = new TokensRegexNERAnnotator(fineGrainedPrefix, fineGrainedProps);
			}
		}

		public virtual void SetUpAdditionalRulesNER(Properties properties)
		{
			this.applyAdditionalRules = (!properties.GetProperty("ner.additional.regexner.mapping", string.Empty).Equals(string.Empty));
			if (this.applyAdditionalRules)
			{
				string additionalRulesPrefix = "ner.additional.regexner";
				Properties additionalRulesProps = PropertiesUtils.ExtractPrefixedProperties(properties, additionalRulesPrefix + ".", true);
				// build the additional rules ner TokensRegexNERAnnotator
				additionalRulesNERAnnotator = new TokensRegexNERAnnotator(additionalRulesPrefix, additionalRulesProps);
			}
		}

		public virtual void SetUpEntityMentionBuilding(Properties properties)
		{
			this.buildEntityMentions = PropertiesUtils.GetBool(properties, "ner.buildEntityMentions", true);
			if (this.buildEntityMentions)
			{
				string entityMentionsPrefix = "ner.entitymentions";
				Properties entityMentionsProps = PropertiesUtils.ExtractPrefixedProperties(properties, entityMentionsPrefix + ".", true);
				// pass language info to the entity mention annotator
				entityMentionsProps.SetProperty("ner.entitymentions.language", language.ToString());
				entityMentionsAnnotator = new EntityMentionsAnnotator(entityMentionsPrefix, entityMentionsProps);
			}
		}

		protected internal override int NThreads()
		{
			return nThreads;
		}

		protected internal override long MaxTime()
		{
			return maxTime;
		}

		public override void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				log.Info("Adding NER Combiner annotation ... ");
			}
			// if ner.usePresentDateForDocDate is set, use the present date as the doc date
			if (usePresentDateForDocDate)
			{
				string currentDate = new SimpleDateFormat("yyyy-MM-dd").Format(Calendar.GetInstance().GetTime());
				annotation.Set(typeof(CoreAnnotations.DocDateAnnotation), currentDate);
			}
			// use provided doc date if applicable
			if (!providedDocDate.Equals(string.Empty))
			{
				annotation.Set(typeof(CoreAnnotations.DocDateAnnotation), providedDocDate);
			}
			base.Annotate(annotation);
			this.ner.FinalizeAnnotation(annotation);
			if (Verbose)
			{
				log.Info("done.");
			}
			// if Spanish, run the regexner with Spanish number rules
			if (LanguageInfo.HumanLanguage.Spanish.Equals(language))
			{
				spanishNumberAnnotator.Annotate(annotation);
			}
			// perform safety clean up
			// MONEY and NUMBER ner tagged items should not have Timex values
			foreach (CoreLabel token in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				if (token.Ner().Equals("MONEY") || token.Ner().Equals("NUMBER"))
				{
					token.Remove(typeof(TimeAnnotations.TimexAnnotation));
				}
			}
			// if fine grained ner is requested, run that
			if (this.applyFineGrained || this.applyAdditionalRules)
			{
				// run the fine grained NER
				if (this.applyFineGrained)
				{
					fineGrainedNERAnnotator.Annotate(annotation);
				}
				// run the custom rules specified
				if (this.applyAdditionalRules)
				{
					additionalRulesNERAnnotator.Annotate(annotation);
				}
				// set the FineGrainedNamedEntityTagAnnotation.class
				foreach (CoreLabel token_1 in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					string fineGrainedTag = token_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					token_1.Set(typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation), fineGrainedTag);
				}
			}
			// if entity mentions should be built, run that
			if (this.buildEntityMentions)
			{
				entityMentionsAnnotator.Annotate(annotation);
			}
		}

		/// <summary>convert Spanish tag content of older models</summary>
		public virtual string SpanishToEnglishTag(string spanishTag)
		{
			if (spanishToEnglishTag.Contains(spanishTag))
			{
				return spanishToEnglishTag[spanishTag];
			}
			else
			{
				return spanishTag;
			}
		}

		protected internal override void DoOneSentence(Annotation annotation, ICoreMap sentence)
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<CoreLabel> output;
			// only used if try assignment works.
			if (tokens.Count <= this.maxSentenceLength)
			{
				try
				{
					output = this.ner.ClassifySentenceWithGlobalInformation(tokens, annotation, sentence);
				}
				catch (RuntimeInterruptedException)
				{
					// If we get interrupted, set the NER labels to the background
					// symbol if they are not already set, then exit.
					output = null;
				}
			}
			else
			{
				output = null;
			}
			if (output == null)
			{
				DoOneFailedSentence(annotation, sentence);
			}
			else
			{
				for (int i = 0; i < sz; ++i)
				{
					// add the named entity tag to each token
					string neTag = output[i].Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					string normNeTag = output[i].Get(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation));
					if (language.Equals(LanguageInfo.HumanLanguage.Spanish))
					{
						neTag = SpanishToEnglishTag(neTag);
						normNeTag = SpanishToEnglishTag(normNeTag);
					}
					tokens[i].SetNER(neTag);
					tokens[i].Set(typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation), neTag);
					if (normNeTag != null)
					{
						tokens[i].Set(typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), normNeTag);
					}
					NumberSequenceClassifier.TransferAnnotations(output[i], tokens[i]);
				}
				if (Verbose)
				{
					bool first = true;
					StringBuilder sb = new StringBuilder("NERCombinerAnnotator output: [");
					foreach (CoreLabel w in tokens)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							sb.Append(", ");
						}
						sb.Append(w.ToShorterString("Text", "NamedEntityTag", "NormalizedNamedEntityTag"));
					}
					sb.Append(']');
					log.Info(sb);
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override void DoOneFailedSentence(Annotation annotation, ICoreMap sentence)
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			foreach (CoreLabel token in tokens)
			{
				// add the background named entity tag to each token if it doesn't have an NER tag.
				if (token.Ner() == null)
				{
					token.SetNER(this.ner.BackgroundSymbol());
				}
			}
		}

		public override ICollection<Type> Requires()
		{
			// TODO: we could check the models to see which ones use lemmas
			// and which ones use pos tags
			if (ner.UsesSUTime() || ner.AppliesNumericClassifiers())
			{
				return Java.Util.Collections.UnmodifiableSet(new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation
					), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.LemmaAnnotation), typeof(CoreAnnotations.BeforeAnnotation), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation
					), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation), typeof(CoreAnnotations.IsNewlineAnnotation))));
			}
			else
			{
				return Java.Util.Collections.UnmodifiableSet(new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation
					), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.BeforeAnnotation), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.IndexAnnotation
					), typeof(CoreAnnotations.OriginalTextAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation), typeof(CoreAnnotations.IsNewlineAnnotation))));
			}
		}

		public override ICollection<Type> RequirementsSatisfied()
		{
			HashSet<Type> nerRequirementsSatisfied = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.NamedEntityTagAnnotation), typeof(CoreAnnotations.NormalizedNamedEntityTagAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(TimeExpression.Annotation
				), typeof(TimeExpression.TimeIndexAnnotation), typeof(CoreAnnotations.DistSimAnnotation), typeof(CoreAnnotations.NumericCompositeTypeAnnotation), typeof(TimeAnnotations.TimexAnnotation), typeof(CoreAnnotations.NumericValueAnnotation), typeof(
				TimeExpression.ChildrenAnnotation), typeof(CoreAnnotations.NumericTypeAnnotation), typeof(CoreAnnotations.ShapeAnnotation), typeof(Tags.TagsAnnotation), typeof(CoreAnnotations.NumerizedTokensAnnotation), typeof(CoreAnnotations.AnswerAnnotation
				), typeof(CoreAnnotations.NumericCompositeValueAnnotation), typeof(CoreAnnotations.CoarseNamedEntityTagAnnotation), typeof(CoreAnnotations.FineGrainedNamedEntityTagAnnotation)));
			if (this.buildEntityMentions)
			{
				nerRequirementsSatisfied.Add(typeof(CoreAnnotations.MentionsAnnotation));
				nerRequirementsSatisfied.Add(typeof(CoreAnnotations.EntityTypeAnnotation));
				nerRequirementsSatisfied.Add(typeof(CoreAnnotations.EntityMentionIndexAnnotation));
			}
			return nerRequirementsSatisfied;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Paragraphs;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves;
using Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An annotator uses attributes quotes in a text to their speakers.</summary>
	/// <remarks>
	/// An annotator uses attributes quotes in a text to their speakers. It uses a two-stage process that first links quotes
	/// to mentions and then mentions to speakers. Each stage consists in a series of sieves that each try to make
	/// predictions on the quote or mentions that have not been linked by previous sieves.
	/// The annotator will add the following annotations to each QuotationAnnotation:
	/// <ul>
	/// <li>MentionAnnotation : the text of the mention</li>
	/// <li>MentionBeginAnnotation : the beginning token index of the mention</li>
	/// <li>MentionEndAnnotation : the end token index of the mention</li>
	/// <li>MentionTypeAnnotation : the type of mention (pronoun, name, or animate noun)</li>
	/// <li>MentionSieveAnnotation : the sieve that made the mention prediction</li>
	/// <li>SpeakerAnnotation : the name of the speaker</li>
	/// <li>SpeakerSieveAnnotation : the name of the sieve that made the speaker prediction</li>
	/// </ul>
	/// The annotator has the following options:
	/// <ul>
	/// <li>quoteattribution.charactersPath (required): path to file containing the character names, aliases,
	/// and gender information.</li>
	/// <li>quoteattribution.booknlpCoref (required): path to tokens file generated from
	/// <a href="https://github.com/dbamman/book-nlp">book-nlp</a> containing coref information.</li>
	/// <li>quoteattribution.QMSieves: list of sieves to use in the quote to mention linking phase
	/// (default=tri,dep,onename,voc,paraend,conv,sup,loose). More information about the sieves can be found at our
	/// <a href="stanfordnlp.github.io/CoreNLP/quoteattribution.html">website</a>. </li>
	/// <li>quoteattribution.MSSieves: list of sieves to use in the mention to speaker linking phase
	/// (default=det,top).</li>
	/// <li>quoteattribution.model: path to trained model file.</li>
	/// <li>quoteattribution.familyWordsFile: path to file with family words list.</li>
	/// <li>quoteattribution.animacyWordsFile: path to file with animacy words list.</li>
	/// <li>quoteattribution.genderNamesFile: path to file with names list with gender information.</li>
	/// </ul>
	/// </remarks>
	/// <author>Grace Muzny, Michael Fang</author>
	public class QuoteAttributionAnnotator : IAnnotator
	{
		public class MentionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MentionBeginAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		public class MentionEndAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		public class MentionTypeAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MentionSieveAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SpeakerAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SpeakerSieveAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class CanonicalMentionAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class CanonicalMentionBeginAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		public class CanonicalMentionEndAnnotation : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(QuoteAttributionAnnotator));

		public const string DefaultQmsieves = "tri,dep,onename,voc,paraend,conv,sup,loose";

		public const string DefaultMssieves = "det,top";

		public const string DefaultModelPath = "edu/stanford/nlp/models/quoteattribution/quoteattribution_model.ser";

		public static string FamilyWordList = "edu/stanford/nlp/models/quoteattribution/family_words.txt";

		public static string AnimacyWordList = "edu/stanford/nlp/models/quoteattribution/animate.unigrams.txt";

		public static string GenderWordList = "edu/stanford/nlp/models/quoteattribution/gender_filtered.txt";

		public static string CorefPath = string.Empty;

		public static string ModelPath = "edu/stanford/nlp/models/quoteattribution/quoteattribution_model.ser";

		public static string CharactersFile = string.Empty;

		public bool buildCharacterMapPerAnnotation = false;

		public bool useCoref = true;

		public bool Verbose = false;

		private ICollection<string> animacyList;

		private ICollection<string> familyRelations;

		private IDictionary<string, Person.Gender> genderMap;

		private IDictionary<string, IList<Person>> characterMap;

		private string qmSieveList;

		private string msSieveList;

		public QuoteAttributionAnnotator(Properties props)
		{
			// settings
			// these paths go in the props file
			// fields
			Verbose = PropertiesUtils.GetBool(props, "verbose", false);
			Timing timer = null;
			CorefPath = props.GetProperty("booknlpCoref", null);
			if (CorefPath == null && Verbose)
			{
				log.Err("Warning: no coreference map!");
			}
			ModelPath = props.GetProperty("modelPath", DefaultModelPath);
			CharactersFile = props.GetProperty("charactersPath", null);
			if (CharactersFile == null && Verbose)
			{
				log.Err("Warning: no characters file!");
			}
			qmSieveList = props.GetProperty("QMSieves", DefaultQmsieves);
			msSieveList = props.GetProperty("MSSieves", DefaultMssieves);
			if (Verbose)
			{
				timer = new Timing();
				log.Info("Loading QuoteAttribution coref [" + CorefPath + "]...");
				log.Info("Loading QuoteAttribution characters [" + CharactersFile + "]...");
			}
			// loading all our word lists
			FamilyWordList = props.GetProperty("familyWordsFile", FamilyWordList);
			AnimacyWordList = props.GetProperty("animacyWordsFile", AnimacyWordList);
			GenderWordList = props.GetProperty("genderNamesFile", GenderWordList);
			familyRelations = QuoteAttributionUtils.ReadFamilyRelations(FamilyWordList);
			genderMap = QuoteAttributionUtils.ReadGenderedNounList(GenderWordList);
			animacyList = QuoteAttributionUtils.ReadAnimacyList(AnimacyWordList);
			if (characterMap != null)
			{
				characterMap = QuoteAttributionUtils.ReadPersonMap(CharactersFile);
			}
			else
			{
				buildCharacterMapPerAnnotation = true;
			}
			// use Stanford CoreNLP coref to map mentions to canonical mentions
			useCoref = PropertiesUtils.GetBool(props, "useCoref", useCoref);
			if (Verbose)
			{
				timer.Stop("done.");
			}
		}

		/// <summary>if no character list is provided, produce a list of person names from entity mentions annotation</summary>
		public virtual void EntityMentionsToCharacterMap(Annotation annotation)
		{
			characterMap = new Dictionary<string, IList<Person>>();
			foreach (ICoreMap entityMention in annotation.Get(typeof(CoreAnnotations.MentionsAnnotation)))
			{
				string entityMentionString = entityMention.ToString();
				if (entityMention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals("PERSON"))
				{
					Person newPerson = new Person(entityMentionString, "UNK", new ArrayList());
					IList<Person> newPersonList = new List<Person>();
					newPersonList.Add(newPerson);
					characterMap[entityMentionString] = newPersonList;
				}
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			bool perDocumentCharacterMap = false;
			if (buildCharacterMapPerAnnotation)
			{
				if (annotation.ContainsKey(typeof(CoreAnnotations.MentionsAnnotation)))
				{
					EntityMentionsToCharacterMap(annotation);
				}
			}
			// 0. pre-preprocess the text with paragraph annotations
			// TODO: maybe move this out, definitely make it so that you can set paragraph breaks
			Properties propsPara = new Properties();
			propsPara.SetProperty("paragraphBreak", "one");
			ParagraphAnnotator pa = new ParagraphAnnotator(propsPara, false);
			pa.Annotate(annotation);
			// 1. preprocess the text
			// a) setup coref
			IDictionary<int, string> pronounCorefMap = QuoteAttributionUtils.SetupCoref(CorefPath, characterMap, annotation);
			//annotate chapter numbers in sentences. Useful for denoting chapter boundaries
			new ChapterAnnotator().Annotate(annotation);
			// to incorporate sentences across paragraphs
			QuoteAttributionUtils.AddEnhancedSentences(annotation);
			//annotate depparse of quote-removed sentences
			QuoteAttributionUtils.AnnotateForDependencyParse(annotation);
			Annotation preprocessed = annotation;
			// 2. Quote->Mention annotation
			IDictionary<string, QMSieve> qmSieves = GetQMMapping(preprocessed, pronounCorefMap);
			foreach (string sieveName in qmSieveList.Split(","))
			{
				qmSieves[sieveName].DoQuoteToMention(preprocessed);
			}
			// 3. Mention->Speaker annotation
			IDictionary<string, MSSieve> msSieves = GetMSMapping(preprocessed, pronounCorefMap);
			foreach (string sieveName_1 in msSieveList.Split(","))
			{
				msSieves[sieveName_1].DoMentionToSpeaker(preprocessed);
			}
			// see if any speaker's could be matched to a canonical entity mention
			foreach (ICoreMap quote in QuoteAnnotator.GatherQuotes(annotation))
			{
				int firstSpeakerTokenIndex = quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation));
				if (firstSpeakerTokenIndex != null)
				{
					CoreLabel firstSpeakerToken = annotation.Get(typeof(CoreAnnotations.TokensAnnotation))[firstSpeakerTokenIndex];
					int entityMentionIndex = firstSpeakerToken.Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation));
					if (entityMentionIndex != null)
					{
						// set speaker string
						ICoreMap entityMention = annotation.Get(typeof(CoreAnnotations.MentionsAnnotation))[entityMentionIndex];
						int canonicalEntityMentionIndex = entityMention.Get(typeof(CoreAnnotations.CanonicalEntityMentionIndexAnnotation));
						if (canonicalEntityMentionIndex != null)
						{
							ICoreMap canonicalEntityMention = annotation.Get(typeof(CoreAnnotations.MentionsAnnotation))[canonicalEntityMentionIndex];
							// add canonical entity mention info to quote
							quote.Set(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation), canonicalEntityMention.Get(typeof(CoreAnnotations.TextAnnotation)));
							// set first and last tokens of canonical entity mention
							IList<CoreLabel> canonicalEntityMentionTokens = canonicalEntityMention.Get(typeof(CoreAnnotations.TokensAnnotation));
							CoreLabel canonicalEntityMentionFirstToken = canonicalEntityMentionTokens[0];
							CoreLabel canonicalEntityMentionLastToken = canonicalEntityMentionTokens[canonicalEntityMentionTokens.Count - 1];
							quote.Set(typeof(QuoteAttributionAnnotator.CanonicalMentionBeginAnnotation), canonicalEntityMentionFirstToken.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
							quote.Set(typeof(QuoteAttributionAnnotator.CanonicalMentionEndAnnotation), canonicalEntityMentionLastToken.Get(typeof(CoreAnnotations.TokenBeginAnnotation)));
						}
					}
				}
			}
		}

		private IDictionary<string, QMSieve> GetQMMapping(Annotation doc, IDictionary<int, string> pronounCorefMap)
		{
			IDictionary<string, QMSieve> map = new Dictionary<string, QMSieve>();
			map["tri"] = new TrigramSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["dep"] = new DependencyParseSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["onename"] = new OneNameSentenceSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["voc"] = new VocativeSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["paraend"] = new ParagraphEndQuoteClosestSieve(doc, characterMap, pronounCorefMap, animacyList);
			SupervisedSieve ss = new SupervisedSieve(doc, characterMap, pronounCorefMap, animacyList);
			ss.LoadModel(ModelPath);
			map["sup"] = ss;
			map["conv"] = new ConversationalSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["loose"] = new LooseConversationalSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["closest"] = new ClosestMentionSieve(doc, characterMap, pronounCorefMap, animacyList);
			return map;
		}

		private IDictionary<string, MSSieve> GetMSMapping(Annotation doc, IDictionary<int, string> pronounCorefMap)
		{
			IDictionary<string, MSSieve> map = new Dictionary<string, MSSieve>();
			map["det"] = new DeterministicSpeakerSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["loose"] = new LooseConversationalSpeakerSieve(doc, characterMap, pronounCorefMap, animacyList);
			map["top"] = new BaselineTopSpeakerSieve(doc, characterMap, pronounCorefMap, animacyList, genderMap, familyRelations);
			map["maj"] = new MajoritySpeakerSieve(doc, characterMap, pronounCorefMap, animacyList);
			return map;
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(QuoteAttributionAnnotator.MentionAnnotation), typeof(QuoteAttributionAnnotator.MentionBeginAnnotation), typeof(QuoteAttributionAnnotator.MentionEndAnnotation), typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation
				), typeof(QuoteAttributionAnnotator.CanonicalMentionBeginAnnotation), typeof(QuoteAttributionAnnotator.CanonicalMentionEndAnnotation), typeof(QuoteAttributionAnnotator.MentionTypeAnnotation), typeof(QuoteAttributionAnnotator.MentionSieveAnnotation
				), typeof(QuoteAttributionAnnotator.SpeakerAnnotation), typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), typeof(CoreAnnotations.ParagraphIndexAnnotation)));
		}

		public virtual ICollection<Type> Requires()
		{
			ICollection<Type> quoteAttributionRequirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation
				), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.LemmaAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation), typeof(CoreAnnotations.MentionsAnnotation), 
				typeof(CoreAnnotations.BeforeAnnotation), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
				)));
			if (useCoref)
			{
				quoteAttributionRequirements.Add(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			}
			return quoteAttributionRequirements;
		}
	}
}

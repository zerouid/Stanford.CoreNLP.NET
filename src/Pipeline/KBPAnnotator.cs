using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Simple;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>An annotator which takes as input sentences, and produces KBP relation annotations.</summary>
	/// <author>Gabor Angeli</author>
	public class KBPAnnotator : IAnnotator
	{
		private string NotProvided = "none";

		private Properties kbpProperties;

		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.KBPAnnotator));

		private string model = DefaultPaths.DefaultKbpClassifier;

		private string semgrexdir = DefaultPaths.DefaultKbpSemgrexDir;

		private string tokensregexdir = DefaultPaths.DefaultKbpTokensregexDir;

		private bool Verbose = false;

		private LanguageInfo.HumanLanguage kbpLanguage;

		/// <summary>The extractor implementation.</summary>
		public readonly IKBPRelationExtractor extractor;

		/// <summary>A serializer to convert to the Simple CoreNLP representation.</summary>
		private readonly ProtobufAnnotationSerializer serializer = new ProtobufAnnotationSerializer(false);

		/// <summary>A basic rule-based system for Spanish coreference</summary>
		private KBPBasicSpanishCorefSystem spanishCorefSystem;

		/// <summary>maximum length sentence to run on</summary>
		private readonly int maxLength;

		/// <summary>pattern matchers for processing coref mentions</summary>
		internal TokenSequencePattern titlePersonPattern = TokenSequencePattern.Compile("[pos:JJ & ner:O]? [ner: TITLE]+ ([ner: PERSON]+)");

		/// <summary>map for converting KBP relation names to latest names</summary>
		private Dictionary<string, string> relationNameConversionMap;

		/// <summary>Create a new KBP annotator from the given properties.</summary>
		/// <param name="props">The properties to use when creating this extractor.</param>
		public KBPAnnotator(string name, Properties props)
		{
			//@ArgumentParser.Option(name="kbp.language", gloss="language for kbp")
			//private String language = "english";
			/*
			* A TokensRegexNER annotator for the special KBP NER types (case-sensitive).
			*/
			//private final TokensRegexNERAnnotator casedNER;
			/*
			* A TokensRegexNER annotator for the special KBP NER types (case insensitive).
			*/
			//private final TokensRegexNERAnnotator caselessNER;
			// Parse standard properties
			ArgumentParser.FillOptions(this, name, props);
			//Locale kbpLanguage =
			//(language.toLowerCase().equals("zh") || language.toLowerCase().equals("chinese")) ?
			//Locale.CHINESE : Locale.ENGLISH ;
			kbpProperties = props;
			try
			{
				List<IKBPRelationExtractor> extractors = new List<IKBPRelationExtractor>();
				// add tokensregex rules
				if (!tokensregexdir.Equals(NotProvided))
				{
					extractors.Add(new KBPTokensregexExtractor(tokensregexdir, Verbose));
				}
				// add semgrex rules
				if (!semgrexdir.Equals(NotProvided))
				{
					extractors.Add(new KBPSemgrexExtractor(semgrexdir, Verbose));
				}
				// attempt to add statistical model
				if (!model.Equals(NotProvided))
				{
					log.Info("Loading KBP classifier from: " + model);
					object @object = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(model);
					IKBPRelationExtractor statisticalExtractor;
					if (@object is LinearClassifier)
					{
						//noinspection unchecked
						statisticalExtractor = new KBPStatisticalExtractor((IClassifier<string, string>)@object);
					}
					else
					{
						if (@object is KBPStatisticalExtractor)
						{
							statisticalExtractor = (KBPStatisticalExtractor)@object;
						}
						else
						{
							throw new InvalidCastException(@object.GetType() + " cannot be cast into a " + typeof(KBPStatisticalExtractor));
						}
					}
					extractors.Add(statisticalExtractor);
				}
				// build extractor
				this.extractor = new KBPEnsembleExtractor(Sharpen.Collections.ToArray(extractors, new IKBPRelationExtractor[extractors.Count]));
				// set maximum length of sentence to operate on
				maxLength = System.Convert.ToInt32(props.GetProperty("kbp.maxlen", "-1"));
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
			// set up map for converting between older and new KBP relation names
			relationNameConversionMap = new Dictionary<string, string>();
			relationNameConversionMap["org:dissolved"] = "org:date_dissolved";
			relationNameConversionMap["org:founded"] = "org:date_founded";
			relationNameConversionMap["org:number_of_employees/members"] = "org:number_of_employees_members";
			relationNameConversionMap["org:political/religious_affiliation"] = "org:political_religious_affiliation";
			relationNameConversionMap["org:top_members/employees"] = "org:top_members_employees";
			relationNameConversionMap["per:member_of"] = "per:employee_or_member_of";
			relationNameConversionMap["per:employee_of"] = "per:employee_or_member_of";
			relationNameConversionMap["per:stateorprovinces_of_residence"] = "per:statesorprovinces_of_residence";
			// set up KBP language
			kbpLanguage = LanguageInfo.GetLanguageFromString(props.GetProperty("kbp.language", "en"));
			// build the Spanish coref system if necessary
			if (LanguageInfo.HumanLanguage.Spanish.Equals(kbpLanguage))
			{
				spanishCorefSystem = new KBPBasicSpanishCorefSystem();
			}
		}

		/// <seealso cref="KBPAnnotator(string, Java.Util.Properties)"></seealso>
		public KBPAnnotator(Properties properties)
			: this(AnnotatorConstants.StanfordKbp, properties)
		{
		}

		/// <summary>Augment the coreferent mention map with acronym matches.</summary>
		private static void AcronymMatch(IList<ICoreMap> mentions, IDictionary<ICoreMap, ICollection<ICoreMap>> mentionsMap)
		{
			int ticks = 0;
			// Get all the candidate antecedents
			IDictionary<IList<string>, ICoreMap> textToMention = new Dictionary<IList<string>, ICoreMap>();
			foreach (ICoreMap mention in mentions)
			{
				string nerTag = mention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				if (nerTag != null && (nerTag.Equals(KBPRelationExtractor.NERTag.Organization.name) || nerTag.Equals(KBPRelationExtractor.NERTag.Location.name)))
				{
					IList<string> tokens = mention.Get(typeof(CoreAnnotations.TokensAnnotation)).Stream().Map(null).Collect(Collectors.ToList());
					if (tokens.Count > 1)
					{
						textToMention[tokens] = mention;
					}
				}
			}
			// Look for candidate acronyms
			foreach (ICoreMap acronym in mentions)
			{
				string nerTag = acronym.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				if (nerTag != null && (nerTag.Equals(KBPRelationExtractor.NERTag.Organization.name) || nerTag.Equals(KBPRelationExtractor.NERTag.Location.name)))
				{
					string text = acronym.Get(typeof(CoreAnnotations.TextAnnotation));
					if (!text.Contains(" "))
					{
						// Candidate acronym
						ICollection<ICoreMap> acronymCluster = mentionsMap[acronym];
						if (acronymCluster == null)
						{
							acronymCluster = new LinkedHashSet<ICoreMap>();
							acronymCluster.Add(acronym);
						}
						// Try to match it to an antecedent
						foreach (KeyValuePair<IList<string>, ICoreMap> entry in textToMention)
						{
							// Time out if we take too long in this loop.
							ticks += 1;
							if (ticks > 1000)
							{
								return;
							}
							// Check if the pair is an acronym
							if (AcronymMatcher.IsAcronym(text, entry.Key))
							{
								// Case: found a coreferent pair
								ICoreMap coreferent = entry.Value;
								ICollection<ICoreMap> coreferentCluster = mentionsMap[coreferent];
								if (coreferentCluster == null)
								{
									coreferentCluster = new LinkedHashSet<ICoreMap>();
									coreferentCluster.Add(coreferent);
								}
								// Create a new coreference cluster
								ICollection<ICoreMap> newCluster = new LinkedHashSet<ICoreMap>();
								Sharpen.Collections.AddAll(newCluster, acronymCluster);
								Sharpen.Collections.AddAll(newCluster, coreferentCluster);
								// Set the new cluster
								foreach (ICoreMap key in newCluster)
								{
									mentionsMap[key] = newCluster;
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Helper method to find best kbp mention in a coref chain
		/// This is defined as longest kbp mention or null if
		/// the coref chain does not contain a kbp mention
		/// </summary>
		/// <param name="ann">the annotation</param>
		/// <param name="corefChain">CorefChain containing potential KBP mentions to search through</param>
		/// <param name="kbpMentions">HashMap mapping character offsets to KBP mentions</param>
		/// <returns>
		/// a list of kbp mentions (or null) for each coref mention in this coref chain, and the index of "best"
		/// kbp mention, which in this case is the longest kbp mention
		/// </returns>
		public virtual Pair<IList<ICoreMap>, ICoreMap> CorefChainToKBPMentions(CorefChain corefChain, Annotation ann, Dictionary<Pair<int, int>, ICoreMap> kbpMentions)
		{
			// map coref mentions into kbp mentions (possibly null if no corresponding kbp mention)
			IList<ICoreMap> annSentences = ann.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// create a list of kbp mentions in this coref chain, possibly all null
			//System.err.println("---");
			//System.err.println("KBP mentions for coref chain");
			IList<ICoreMap> kbpMentionsForCorefChain = corefChain.GetMentionsInTextualOrder().Stream().Map(null).Collect(Collectors.ToList());
			// if a best KBP mention can't be found, handle special cases
			// look for a PERSON kbp mention in TITLE+ (PERSON+)
			//if (kbpMentionFound != null)
			//System.err.println(kbpMentionFound.get(CoreAnnotations.TextAnnotation.class));
			// map kbp mentions to the lengths of their text
			IList<int> kbpMentionLengths = kbpMentionsForCorefChain.Stream().Map(null).Collect(Collectors.ToList());
			int bestIndex = kbpMentionLengths.IndexOf(kbpMentionLengths.Stream().Reduce(0, null));
			// return the first occurrence of the kbp mention with max length (possibly null)
			return new Pair(kbpMentionsForCorefChain, kbpMentionsForCorefChain[bestIndex]);
		}

		/// <summary>Convert between older naming convention and current for relation names</summary>
		/// <param name="relationName">the original relation name.</param>
		/// <returns>the converted relation name</returns>
		private string ConvertRelationNameToLatest(string relationName)
		{
			if (relationNameConversionMap.Contains(relationName))
			{
				return relationNameConversionMap[relationName];
			}
			else
			{
				return relationName;
			}
		}

		/// <summary>Returns whether the given token counts as a valid pronominal mention for KBP.</summary>
		/// <remarks>
		/// Returns whether the given token counts as a valid pronominal mention for KBP.
		/// This method (at present) works for either Chinese or English.
		/// </remarks>
		/// <param name="word">The token to classify.</param>
		/// <returns>true if this token is a pronoun that KBP should recognize.</returns>
		private static bool KbpIsPronominalMention(CoreLabel word)
		{
			return WordLists.IsKbpPronominalMention(word.Word());
		}

		/// <summary>Annotate this document for KBP relations.</summary>
		/// <param name="annotation">The document to annotate.</param>
		public virtual void Annotate(Annotation annotation)
		{
			// get a list of sentences for this annotation
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// Create simple document
			Document doc = new Document(kbpProperties, serializer.ToProto(annotation));
			// Get the mentions in the document
			IList<ICoreMap> mentions = new List<ICoreMap>();
			foreach (ICoreMap sentence in sentences)
			{
				Sharpen.Collections.AddAll(mentions, sentence.Get(typeof(CoreAnnotations.MentionsAnnotation)));
			}
			// Compute coreferent clusters
			// (map an index to a KBP mention)
			IDictionary<Pair<int, int>, ICoreMap> mentionByStartIndex = new Dictionary<Pair<int, int>, ICoreMap>();
			foreach (ICoreMap mention in mentions)
			{
				foreach (CoreLabel token in mention.Get(typeof(CoreAnnotations.TokensAnnotation)))
				{
					mentionByStartIndex[Pair.MakePair(token.SentIndex(), token.Index())] = mention;
				}
			}
			// (collect coreferent KBP mentions)
			IDictionary<ICoreMap, ICollection<ICoreMap>> mentionsMap = new Dictionary<ICoreMap, ICollection<ICoreMap>>();
			// map from canonical mention -> other mentions
			if (annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)) != null)
			{
				foreach (KeyValuePair<int, CorefChain> chain in annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)))
				{
					ICoreMap firstMention = null;
					foreach (CorefChain.CorefMention mention_1 in chain.Value.GetMentionsInTextualOrder())
					{
						ICoreMap kbpMention = null;
						for (int i = mention_1.startIndex; i < mention_1.endIndex; ++i)
						{
							if (mentionByStartIndex.Contains(Pair.MakePair(mention_1.sentNum - 1, i)))
							{
								kbpMention = mentionByStartIndex[Pair.MakePair(mention_1.sentNum - 1, i)];
								break;
							}
						}
						if (firstMention == null)
						{
							firstMention = kbpMention;
						}
						if (kbpMention != null)
						{
							if (!mentionsMap.Contains(firstMention))
							{
								mentionsMap[firstMention] = new LinkedHashSet<ICoreMap>();
							}
							mentionsMap[firstMention].Add(kbpMention);
						}
					}
				}
			}
			// (coreference acronyms)
			AcronymMatch(mentions, mentionsMap);
			// (ensure valid NER tag for canonical mention)
			foreach (ICoreMap key in new HashSet<ICoreMap>(mentionsMap.Keys))
			{
				if (key.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) == null)
				{
					ICoreMap newKey = null;
					foreach (ICoreMap candidate in mentionsMap[key])
					{
						if (candidate.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)) != null)
						{
							newKey = candidate;
							break;
						}
					}
					if (newKey != null)
					{
						mentionsMap[newKey] = Sharpen.Collections.Remove(mentionsMap, key);
					}
					else
					{
						Sharpen.Collections.Remove(mentionsMap, key);
					}
				}
			}
			// case: no mention in this chain has an NER tag.
			// Propagate Entity Link
			foreach (KeyValuePair<ICoreMap, ICollection<ICoreMap>> entry in mentionsMap)
			{
				string entityLink = entry.Key.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation));
				if (entityLink != null)
				{
					foreach (ICoreMap mention_1 in entry.Value)
					{
						foreach (CoreLabel token in mention_1.Get(typeof(CoreAnnotations.TokensAnnotation)))
						{
							token.Set(typeof(CoreAnnotations.WikipediaEntityAnnotation), entityLink);
						}
					}
				}
			}
			// create a mapping of char offset pairs to KBPMention
			Dictionary<Pair<int, int>, ICoreMap> charOffsetToKBPMention = new Dictionary<Pair<int, int>, ICoreMap>();
			foreach (ICoreMap mention_2 in mentions)
			{
				int nerMentionCharBegin = mention_2.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int nerMentionCharEnd = mention_2.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				charOffsetToKBPMention[new Pair<int, int>(nerMentionCharBegin, nerMentionCharEnd)] = mention_2;
			}
			// Create a canonical mention map
			IDictionary<ICoreMap, ICoreMap> mentionToCanonicalMention;
			if (kbpLanguage.Equals(LanguageInfo.HumanLanguage.Spanish))
			{
				mentionToCanonicalMention = spanishCorefSystem.CanonicalMentionMapFromEntityMentions(mentions);
				if (Verbose)
				{
					log.Info("---");
					log.Info("basic spanish coref results");
					foreach (ICoreMap originalMention in mentionToCanonicalMention.Keys)
					{
						if (!originalMention.Equals(mentionToCanonicalMention[originalMention]))
						{
							log.Info("mapped: " + originalMention + " to: " + mentionToCanonicalMention[originalMention]);
						}
					}
				}
			}
			else
			{
				mentionToCanonicalMention = new Dictionary<ICoreMap, ICoreMap>();
			}
			// check if there is coref info
			ICollection<KeyValuePair<int, CorefChain>> corefChains;
			if (annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation)) != null && !kbpLanguage.Equals(LanguageInfo.HumanLanguage.Spanish))
			{
				corefChains = annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			}
			else
			{
				corefChains = new HashSet<KeyValuePair<int, CorefChain>>();
			}
			foreach (KeyValuePair<int, CorefChain> indexCorefChainPair in corefChains)
			{
				CorefChain corefChain = indexCorefChainPair.Value;
				Pair<IList<ICoreMap>, ICoreMap> corefChainKBPMentionsAndBestIndex = CorefChainToKBPMentions(corefChain, annotation, charOffsetToKBPMention);
				IList<ICoreMap> corefChainKBPMentions = corefChainKBPMentionsAndBestIndex.First();
				ICoreMap bestKBPMentionForChain = corefChainKBPMentionsAndBestIndex.Second();
				if (bestKBPMentionForChain != null)
				{
					foreach (ICoreMap kbpMention in corefChainKBPMentions)
					{
						if (kbpMention != null)
						{
							//System.err.println("---");
							// ad hoc filters ; assume acceptable unless a filter blocks it
							bool acceptableLink = true;
							// block people matches without a token overlap, exempting pronominal to non-pronominal
							// good: Ashton --> Catherine Ashton
							// good: she --> Catherine Ashton
							// bad: Morsi --> Catherine Ashton
							string kbpMentionNERTag = kbpMention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
							string bestKBPMentionForChainNERTag = bestKBPMentionForChain.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
							if (kbpMentionNERTag != null && bestKBPMentionForChainNERTag != null && kbpMentionNERTag.Equals("PERSON") && bestKBPMentionForChainNERTag.Equals("PERSON") && !KbpIsPronominalMention(kbpMention.Get(typeof(CoreAnnotations.TokensAnnotation))[0]
								) && !KbpIsPronominalMention(bestKBPMentionForChain.Get(typeof(CoreAnnotations.TokensAnnotation))[0]))
							{
								//System.err.println("testing PERSON to PERSON coref link");
								bool tokenMatchFound = false;
								foreach (CoreLabel kbpToken in kbpMention.Get(typeof(CoreAnnotations.TokensAnnotation)))
								{
									foreach (CoreLabel bestKBPToken in bestKBPMentionForChain.Get(typeof(CoreAnnotations.TokensAnnotation)))
									{
										if (kbpToken.Word().ToLower().Equals(bestKBPToken.Word().ToLower()))
										{
											tokenMatchFound = true;
											break;
										}
									}
									if (tokenMatchFound)
									{
										break;
									}
								}
								if (!tokenMatchFound)
								{
									acceptableLink = false;
								}
							}
							// check the coref link passed the filters
							if (acceptableLink)
							{
								mentionToCanonicalMention[kbpMention] = bestKBPMentionForChain;
							}
						}
					}
				}
			}
			//System.err.println("kbp mention: " + kbpMention.get(CoreAnnotations.TextAnnotation.class));
			//System.err.println("coref mention: " + bestKBPMentionForChain.get(CoreAnnotations.TextAnnotation.class));
			// (add missing mentions)
			mentions.Stream().Filter(null).ForEach(null);
			// handle acronym coreference
			Dictionary<string, IList<ICoreMap>> acronymClusters = new Dictionary<string, IList<ICoreMap>>();
			Dictionary<string, IList<ICoreMap>> acronymInstances = new Dictionary<string, IList<ICoreMap>>();
			foreach (ICoreMap acronymMention in mentionToCanonicalMention.Keys)
			{
				string acronymNERTag = acronymMention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
				if ((acronymMention == mentionToCanonicalMention[acronymMention]) && acronymNERTag != null && (acronymNERTag.Equals(KBPRelationExtractor.NERTag.Organization.name) || acronymNERTag.Equals(KBPRelationExtractor.NERTag.Location.name)))
				{
					string acronymText = acronymMention.Get(typeof(CoreAnnotations.TextAnnotation));
					IList<ICoreMap> coreferentMentions = new List<ICoreMap>();
					// define acronyms as not containing spaces (e.g. ACLU)
					if (!acronymText.Contains(" "))
					{
						int numCoreferentsChecked = 0;
						foreach (ICoreMap coreferentMention in mentions)
						{
							// only check first 1000
							if (numCoreferentsChecked > 1000)
							{
								break;
							}
							// don't check a mention against itself
							if (acronymMention == coreferentMention)
							{
								continue;
							}
							// don't check other mentions without " "
							string coreferentText = coreferentMention.Get(typeof(CoreAnnotations.TextAnnotation));
							if (!coreferentText.Contains(" "))
							{
								continue;
							}
							numCoreferentsChecked++;
							IList<string> coreferentTokenStrings = coreferentMention.Get(typeof(CoreAnnotations.TokensAnnotation)).Stream().Map(null).Collect(Collectors.ToList());
							// when an acronym match is found:
							// store every mention (that isn't ACLU) that matches with ACLU in acronymClusters
							// store every instance of "ACLU" in acronymInstances
							// afterwards find the best mention in acronymClusters, and match it to every mention in acronymInstances
							if (AcronymMatcher.IsAcronym(acronymText, coreferentTokenStrings))
							{
								if (!acronymClusters.Contains(acronymText))
								{
									acronymClusters[acronymText] = new List<ICoreMap>();
								}
								if (!acronymInstances.Contains(acronymText))
								{
									acronymInstances[acronymText] = new List<ICoreMap>();
								}
								acronymClusters[acronymText].Add(coreferentMention);
								acronymInstances[acronymText].Add(acronymMention);
							}
						}
					}
				}
			}
			// process each acronym (e.g. ACLU)
			foreach (string acronymText_1 in acronymInstances.Keys)
			{
				// find longest ORG or null
				ICoreMap bestORG = null;
				foreach (ICoreMap coreferentMention in acronymClusters[acronymText_1])
				{
					if (!coreferentMention.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals(KBPRelationExtractor.NERTag.Organization.name))
					{
						continue;
					}
					if (bestORG == null)
					{
						bestORG = coreferentMention;
					}
					else
					{
						if (coreferentMention.Get(typeof(CoreAnnotations.TextAnnotation)).Length > bestORG.Get(typeof(CoreAnnotations.TextAnnotation)).Length)
						{
							bestORG = coreferentMention;
						}
					}
				}
				// find longest LOC or null
				ICoreMap bestLOC = null;
				foreach (ICoreMap coreferentMention_1 in acronymClusters[acronymText_1])
				{
					if (!coreferentMention_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)).Equals(KBPRelationExtractor.NERTag.Location.name))
					{
						continue;
					}
					if (bestLOC == null)
					{
						bestLOC = coreferentMention_1;
					}
					else
					{
						if (coreferentMention_1.Get(typeof(CoreAnnotations.TextAnnotation)).Length > bestLOC.Get(typeof(CoreAnnotations.TextAnnotation)).Length)
						{
							bestLOC = coreferentMention_1;
						}
					}
				}
				// link ACLU to "American Civil Liberties Union" ; make sure NER types match
				foreach (ICoreMap acronymMention_1 in acronymInstances[acronymText_1])
				{
					string mentionType = acronymMention_1.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation));
					if (mentionType.Equals(KBPRelationExtractor.NERTag.Organization.name) && bestORG != null)
					{
						mentionToCanonicalMention[acronymMention_1] = bestORG;
					}
					if (mentionType.Equals(KBPRelationExtractor.NERTag.Location.name) && bestLOC != null)
					{
						mentionToCanonicalMention[acronymMention_1] = bestLOC;
					}
				}
			}
			// Cluster mentions by sentence
			IList<ICoreMap>[] mentionsBySentence = new IList[annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)).Count];
			for (int i_1 = 0; i_1 < mentionsBySentence.Length; ++i_1)
			{
				mentionsBySentence[i_1] = new List<ICoreMap>();
			}
			foreach (ICoreMap mention_3 in mentionToCanonicalMention.Keys)
			{
				mentionsBySentence[mention_3.Get(typeof(CoreAnnotations.SentenceIndexAnnotation))].Add(mention_3);
			}
			// Classify
			for (int sentenceI = 0; sentenceI < mentionsBySentence.Length; ++sentenceI)
			{
				Dictionary<string, RelationTriple> relationStringsToTriples = new Dictionary<string, RelationTriple>();
				IList<RelationTriple> finalTriplesList = new List<RelationTriple>();
				// the annotations
				IList<ICoreMap> candidates = mentionsBySentence[sentenceI];
				// determine sentence length
				int sentenceLength = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[sentenceI].Get(typeof(CoreAnnotations.TokensAnnotation)).Count;
				// check if sentence is too long, if it's too long don't run kbp
				if (maxLength != -1 && sentenceLength > maxLength)
				{
					// set the triples annotation to an empty list of RelationTriples
					annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[sentenceI].Set(typeof(CoreAnnotations.KBPTriplesAnnotation), finalTriplesList);
					// continue to next sentence
					continue;
				}
				// sentence isn't too long, so continue processing this sentence
				for (int subjI = 0; subjI < candidates.Count; ++subjI)
				{
					ICoreMap subj = candidates[subjI];
					int subjBegin = subj.Get(typeof(CoreAnnotations.TokensAnnotation))[0].Index() - 1;
					int subjEnd = subj.Get(typeof(CoreAnnotations.TokensAnnotation))[subj.Get(typeof(CoreAnnotations.TokensAnnotation)).Count - 1].Index();
					Optional<KBPRelationExtractor.NERTag> subjNER = KBPRelationExtractor.NERTag.FromString(subj.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)));
					if (subjNER.IsPresent())
					{
						for (int objI = 0; objI < candidates.Count; ++objI)
						{
							if (subjI == objI)
							{
								continue;
							}
							if (Thread.Interrupted())
							{
								throw new RuntimeInterruptedException();
							}
							ICoreMap obj = candidates[objI];
							int objBegin = obj.Get(typeof(CoreAnnotations.TokensAnnotation))[0].Index() - 1;
							int objEnd = obj.Get(typeof(CoreAnnotations.TokensAnnotation))[obj.Get(typeof(CoreAnnotations.TokensAnnotation)).Count - 1].Index();
							Optional<KBPRelationExtractor.NERTag> objNER = KBPRelationExtractor.NERTag.FromString(obj.Get(typeof(CoreAnnotations.NamedEntityTagAnnotation)));
							if (objNER.IsPresent() && KBPRelationExtractor.RelationType.PlausiblyHasRelation(subjNER.Get(), objNER.Get()))
							{
								// type check
								KBPRelationExtractor.KBPInput input = new KBPRelationExtractor.KBPInput(new Span(subjBegin, subjEnd), new Span(objBegin, objEnd), subjNER.Get(), objNER.Get(), doc.Sentence(sentenceI));
								//  -- BEGIN Classify
								Pair<string, double> prediction = extractor.Classify(input);
								//  -- END Classify
								// Handle the classifier output
								if (!KBPStatisticalExtractor.NoRelation.Equals(prediction.first))
								{
									RelationTriple triple = new RelationTriple.WithLink(subj.Get(typeof(CoreAnnotations.TokensAnnotation)), mentionToCanonicalMention[subj].Get(typeof(CoreAnnotations.TokensAnnotation)), Java.Util.Collections.SingletonList(new CoreLabel(new Word
										(ConvertRelationNameToLatest(prediction.first)))), obj.Get(typeof(CoreAnnotations.TokensAnnotation)), mentionToCanonicalMention[obj].Get(typeof(CoreAnnotations.TokensAnnotation)), prediction.second, sentences[sentenceI].Get(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation
										)), subj.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)), obj.Get(typeof(CoreAnnotations.WikipediaEntityAnnotation)));
									string tripleString = triple.SubjectGloss() + "\t" + triple.RelationGloss() + "\t" + triple.ObjectGloss();
									// ad hoc checks for problems
									bool acceptableTriple = true;
									if (triple.ObjectGloss().Equals(triple.SubjectGloss()) && triple.RelationGloss().EndsWith("alternate_names"))
									{
										acceptableTriple = false;
									}
									// only add this triple if it has the highest confidence ; this process generates duplicates with
									// different confidence scores, so we want to filter out the lower confidence versions
									if (acceptableTriple && !relationStringsToTriples.Contains(tripleString))
									{
										relationStringsToTriples[tripleString] = triple;
									}
									else
									{
										if (acceptableTriple && triple.confidence > relationStringsToTriples[tripleString].confidence)
										{
											relationStringsToTriples[tripleString] = triple;
										}
									}
								}
							}
						}
					}
				}
				finalTriplesList = new ArrayList(relationStringsToTriples.Values);
				// Set triples
				annotation.Get(typeof(CoreAnnotations.SentencesAnnotation))[sentenceI].Set(typeof(CoreAnnotations.KBPTriplesAnnotation), finalTriplesList);
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<Type> RequirementsSatisfied()
		{
			ICollection<Type> requirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.MentionsAnnotation), typeof(CoreAnnotations.KBPTriplesAnnotation)));
			return Java.Util.Collections.UnmodifiableSet(requirements);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<Type> Requires()
		{
			ICollection<Type> requirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation
				), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.LemmaAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation
				), typeof(CoreAnnotations.OriginalTextAnnotation)));
			return Java.Util.Collections.UnmodifiableSet(requirements);
		}

		/// <summary>A debugging method to try relation extraction from the console.</summary>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			props.SetProperty("annotators", "tokenize,ssplit,pos,lemma,ner,regexner,parse,mention,coref,kbp");
			props.SetProperty("regexner.mapping", "ignorecase=true,validpospattern=^(NN|JJ).*,edu/stanford/nlp/models/kbp/regexner_caseless.tab;edu/stanford/nlp/models/kbp/regexner_cased.tab");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			IOUtils.Console("sentence> ", null);
		}
	}
}

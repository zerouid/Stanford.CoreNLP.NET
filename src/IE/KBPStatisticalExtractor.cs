using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Simple;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;








namespace Edu.Stanford.Nlp.IE
{
	/// <summary>A relation extractor to work with Victor's new KBP data.</summary>
	[System.Serializable]
	public class KBPStatisticalExtractor : IKBPRelationExtractor
	{
		private const long serialVersionUID = 1L;

		public static File TrainFile = new File("train.conll");

		public static File TestFile = new File("test.conll");

		public static string ModelFile = DefaultPaths.DefaultKbpClassifier;

		public static Optional<string> Predictions = Optional.Empty();

		private enum MinimizerType
		{
			Qn,
			Sgd,
			Hybrid,
			L1
		}

		private static KBPStatisticalExtractor.MinimizerType minimizer = KBPStatisticalExtractor.MinimizerType.L1;

		private static int FeatureThreshold = 0;

		private static double Sigma = 1.0;

		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.KBPStatisticalExtractor));

		private sealed class _HashSet_65 : HashSet<string>
		{
			public _HashSet_65()
			{
				{
					this.Add("executive");
					this.Add("chairman");
					this.Add("president");
					this.Add("chief");
					this.Add("head");
					this.Add("general");
					this.Add("ceo");
					this.Add("officer");
					this.Add("founder");
					this.Add("found");
					this.Add("leader");
					this.Add("vice");
					this.Add("king");
					this.Add("prince");
					this.Add("manager");
					this.Add("host");
					this.Add("minister");
					this.Add("adviser");
					this.Add("boss");
					this.Add("chair");
					this.Add("ambassador");
					this.Add("shareholder");
					this.Add("star");
					this.Add("governor");
					this.Add("investor");
					this.Add("representative");
					this.Add("dean");
					this.Add("commissioner");
					this.Add("deputy");
					this.Add("commander");
					this.Add("scientist");
					this.Add("midfielder");
					this.Add("speaker");
					this.Add("researcher");
					this.Add("editor");
					this.Add("chancellor");
					this.Add("fellow");
					this.Add("leadership");
					this.Add("diplomat");
					this.Add("attorney");
					this.Add("associate");
					this.Add("striker");
					this.Add("pilot");
					this.Add("captain");
					this.Add("banker");
					this.Add("mayer");
					this.Add("premier");
					this.Add("producer");
					this.Add("architect");
					this.Add("designer");
					this.Add("major");
					this.Add("advisor");
					this.Add("presidency");
					this.Add("senator");
					this.Add("specialist");
					this.Add("faculty");
					this.Add("monitor");
					this.Add("chairwoman");
					this.Add("mayor");
					this.Add("columnist");
					this.Add("mediator");
					this.Add("prosecutor");
					this.Add("entrepreneur");
					this.Add("creator");
					this.Add("superstar");
					this.Add("commentator");
					this.Add("principal");
					this.Add("operative");
					this.Add("businessman");
					this.Add("peacekeeper");
					this.Add("investigator");
					this.Add("coordinator");
					this.Add("knight");
					this.Add("lawmaker");
					this.Add("justice");
					this.Add("publisher");
					this.Add("playmaker");
					this.Add("moderator");
					this.Add("negotiator");
				}
			}
		}

		/// <summary>A list of triggers for top employees.</summary>
		private static readonly ICollection<string> TopEmployeeTriggers = Java.Util.Collections.UnmodifiableSet(new _HashSet_65());

		/// <summary>
		/// <p>
		/// Often, features fall naturally into <i>feature templates</i> and their associated value.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Often, features fall naturally into <i>feature templates</i> and their associated value.
		/// For example, unigram features have a feature template of unigram, and a feature value of the word
		/// in question.
		/// </p>
		/// <p>
		/// This method is a convenience convention for defining these feature template / value pairs.
		/// The advantage of using the method is that it allows for easily finding the feature template for a
		/// given feature value; thus, you can do feature selection post-hoc on the String features by splitting
		/// out certain feature templates.
		/// </p>
		/// <p>
		/// Note that spaces in the feature value are also replaced with a special character, mostly out of
		/// paranoia.
		/// </p>
		/// </remarks>
		/// <param name="features">The feature counter we are updating.</param>
		/// <param name="featureTemplate">The feature template to add a value to.</param>
		/// <param name="featureValue">
		/// The value of the feature template. This is joined with the template, so it
		/// need only be unique within the template.
		/// </param>
		private static void Indicator(ICounter<string> features, string featureTemplate, string featureValue)
		{
			features.IncrementCount(featureTemplate + "ℵ" + featureValue.Replace(' ', 'ˑ'));
		}

		/// <summary>Get information from the span between the two mentions.</summary>
		/// <remarks>
		/// Get information from the span between the two mentions.
		/// Canonically, get the words in this span.
		/// For instance, for "Obama was born in Hawaii", this would return a list
		/// "was born in" if the selector is <code>CoreLabel::token</code>;
		/// or "be bear in" if the selector is <code>CoreLabel::lemma</code>.
		/// </remarks>
		/// <param name="input">The featurizer input.</param>
		/// <param name="selector">The field to compute for each element in the span. A good default is <code></code>CoreLabel::word</code> or <code></code>CoreLabel::token</code></param>
		/// <?/>
		/// <returns>A list of elements between the two mentions.</returns>
		private static IList<E> SpanBetweenMentions<E>(KBPRelationExtractor.KBPInput input, Func<CoreLabel, E> selector)
		{
			IList<CoreLabel> sentence = input.sentence.AsCoreLabels(null, null);
			Span subjSpan = input.subjectSpan;
			Span objSpan = input.objectSpan;
			// Corner cases
			if (Span.Overlaps(subjSpan, objSpan))
			{
				return Java.Util.Collections.EmptyList;
			}
			// Get the range between the subject and object
			int begin = subjSpan.End();
			int end = objSpan.Start();
			if (begin > end)
			{
				begin = objSpan.End();
				end = subjSpan.Start();
			}
			if (begin > end)
			{
				throw new ArgumentException("Gabor sucks at logic and he should feel bad about it: " + subjSpan + " and " + objSpan);
			}
			else
			{
				if (begin == end)
				{
					return Java.Util.Collections.EmptyList;
				}
			}
			// Compute the return value
			IList<E> rtn = new List<E>();
			for (int i = begin; i < end; ++i)
			{
				rtn.Add(selector.Apply(sentence[i]));
			}
			return rtn;
		}

		/// <summary>
		/// <p>
		/// Span features often only make sense if the subject and object are positioned at the correct ends of the span.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Span features often only make sense if the subject and object are positioned at the correct ends of the span.
		/// For example, "x is the son of y" and "y is the son of x" have the same span feature, but mean different things
		/// depending on where x and y are.
		/// </p>
		/// <p>
		/// This is a simple helper to position a dummy subject and object token appropriately.
		/// </p>
		/// </remarks>
		/// <param name="input">The featurizer input.</param>
		/// <param name="feature">The span feature to augment.</param>
		/// <returns>The augmented feature.</returns>
		private static string WithMentionsPositioned(KBPRelationExtractor.KBPInput input, string feature)
		{
			if (input.subjectSpan.IsBefore(input.objectSpan))
			{
				return "+__SUBJ__ " + feature + " __OBJ__";
			}
			else
			{
				return "__OBJ__ " + feature + " __SUBJ__";
			}
		}

		private static void DenseFeatures(KBPRelationExtractor.KBPInput input, Sentence sentence, ClassicCounter<string> feats)
		{
			bool subjBeforeObj = input.subjectSpan.IsBefore(input.objectSpan);
			// Type signature
			Indicator(feats, "type_signature", input.subjectType + "," + input.objectType);
			// Relative position
			Indicator(feats, "subj_before_obj", subjBeforeObj ? "y" : "n");
		}

		private static void SurfaceFeatures(KBPRelationExtractor.KBPInput input, Sentence simpleSentence, ClassicCounter<string> feats)
		{
			IList<string> lemmaSpan = SpanBetweenMentions(input, null);
			IList<string> nerSpan = SpanBetweenMentions(input, null);
			IList<string> posSpan = SpanBetweenMentions(input, null);
			// Unigram features of the sentence
			IList<CoreLabel> tokens = input.sentence.AsCoreLabels(null, null);
			foreach (CoreLabel token in tokens)
			{
				Indicator(feats, "sentence_unigram", token.Lemma());
			}
			// Full lemma span ( -0.3 F1 )
			//    if (lemmaSpan.size() <= 5) {
			//      indicator(feats, "full_lemma_span", withMentionsPositioned(input, StringUtils.join(lemmaSpan, " ")));
			//    }
			// Lemma n-grams
			string lastLemma = "_^_";
			foreach (string lemma in lemmaSpan)
			{
				Indicator(feats, "lemma_bigram", WithMentionsPositioned(input, lastLemma + " " + lemma));
				Indicator(feats, "lemma_unigram", WithMentionsPositioned(input, lemma));
				lastLemma = lemma;
			}
			Indicator(feats, "lemma_bigram", WithMentionsPositioned(input, lastLemma + " _$_"));
			// NER + lemma bi-grams
			for (int i = 0; i < lemmaSpan.Count - 1; ++i)
			{
				if (!"O".Equals(nerSpan[i]) && "O".Equals(nerSpan[i + 1]) && "IN".Equals(posSpan[i + 1]))
				{
					Indicator(feats, "ner/lemma_bigram", WithMentionsPositioned(input, nerSpan[i] + " " + lemmaSpan[i + 1]));
				}
				if (!"O".Equals(nerSpan[i + 1]) && "O".Equals(nerSpan[i]) && "IN".Equals(posSpan[i]))
				{
					Indicator(feats, "ner/lemma_bigram", WithMentionsPositioned(input, lemmaSpan[i] + " " + nerSpan[i + 1]));
				}
			}
			// Distance between mentions
			string distanceBucket = ">10";
			if (lemmaSpan.Count == 0)
			{
				distanceBucket = "0";
			}
			else
			{
				if (lemmaSpan.Count <= 3)
				{
					distanceBucket = "<=3";
				}
				else
				{
					if (lemmaSpan.Count <= 5)
					{
						distanceBucket = "<=5";
					}
					else
					{
						if (lemmaSpan.Count <= 10)
						{
							distanceBucket = "<=10";
						}
						else
						{
							if (lemmaSpan.Count <= 15)
							{
								distanceBucket = "<=15";
							}
						}
					}
				}
			}
			Indicator(feats, "distance_between_entities_bucket", distanceBucket);
			// Punctuation features
			int numCommasInSpan = 0;
			int numQuotesInSpan = 0;
			int parenParity = 0;
			foreach (string lemma_1 in lemmaSpan)
			{
				if (lemma_1.Equals(","))
				{
					numCommasInSpan += 1;
				}
				if (lemma_1.Equals("\"") || lemma_1.Equals("``") || lemma_1.Equals("''"))
				{
					numQuotesInSpan += 1;
				}
				if (lemma_1.Equals("(") || lemma_1.Equals("-LRB-"))
				{
					parenParity += 1;
				}
				if (lemma_1.Equals(")") || lemma_1.Equals("-RRB-"))
				{
					parenParity -= 1;
				}
			}
			Indicator(feats, "comma_parity", numCommasInSpan % 2 == 0 ? "even" : "odd");
			Indicator(feats, "quote_parity", numQuotesInSpan % 2 == 0 ? "even" : "odd");
			Indicator(feats, "paren_parity", string.Empty + parenParity);
			// Is broken by entity
			ICollection<string> intercedingNERTags = nerSpan.Stream().Filter(null).Collect(Collectors.ToSet());
			if (!intercedingNERTags.IsEmpty())
			{
				Indicator(feats, "has_interceding_ner", "t");
			}
			foreach (string ner in intercedingNERTags)
			{
				Indicator(feats, "interceding_ner", ner);
			}
			// Left and right context
			IList<CoreLabel> sentence = input.sentence.AsCoreLabels(null);
			if (input.subjectSpan.Start() == 0)
			{
				Indicator(feats, "subj_left", "^");
			}
			else
			{
				Indicator(feats, "subj_left", sentence[input.subjectSpan.Start() - 1].Lemma());
			}
			if (input.subjectSpan.End() == sentence.Count)
			{
				Indicator(feats, "subj_right", "$");
			}
			else
			{
				Indicator(feats, "subj_right", sentence[input.subjectSpan.End()].Lemma());
			}
			if (input.objectSpan.Start() == 0)
			{
				Indicator(feats, "obj_left", "^");
			}
			else
			{
				Indicator(feats, "obj_left", sentence[input.objectSpan.Start() - 1].Lemma());
			}
			if (input.objectSpan.End() == sentence.Count)
			{
				Indicator(feats, "obj_right", "$");
			}
			else
			{
				Indicator(feats, "obj_right", sentence[input.objectSpan.End()].Lemma());
			}
			// Skip-word patterns
			if (lemmaSpan.Count == 1 && input.subjectSpan.IsBefore(input.objectSpan))
			{
				string left = input.subjectSpan.Start() == 0 ? "^" : sentence[input.subjectSpan.Start() - 1].Lemma();
				Indicator(feats, "X<subj>Y<obj>", left + "_" + lemmaSpan[0]);
			}
		}

		private static void DependencyFeatures(KBPRelationExtractor.KBPInput input, Sentence sentence, ClassicCounter<string> feats)
		{
			int subjectHead = sentence.Algorithms().HeadOfSpan(input.subjectSpan);
			int objectHead = sentence.Algorithms().HeadOfSpan(input.objectSpan);
			//    indicator(feats, "subject_head", sentence.lemma(subjectHead));
			//    indicator(feats, "object_head", sentence.lemma(objectHead));
			if (input.objectType.isRegexNERType)
			{
				Indicator(feats, "object_head", sentence.Lemma(objectHead));
			}
			// Get the dependency path
			IList<string> depparsePath = sentence.Algorithms().DependencyPathBetween(subjectHead, objectHead, Optional.Of(null));
			// Chop out appos edges
			if (depparsePath.Count > 3)
			{
				IList<int> apposChunks = new List<int>();
				for (int i = 1; i < depparsePath.Count - 1; ++i)
				{
					if ("-appos->".Equals(depparsePath[i]))
					{
						if (i != 1)
						{
							apposChunks.Add(i - 1);
						}
						apposChunks.Add(i);
					}
					else
					{
						if ("<-appos-".Equals(depparsePath[i]))
						{
							if (i < depparsePath.Count - 1)
							{
								apposChunks.Add(i + 1);
							}
							apposChunks.Add(i);
						}
					}
				}
				apposChunks.Sort();
				for (int i_1 = apposChunks.Count - 1; i_1 >= 0; --i_1)
				{
					depparsePath.Remove(i_1);
				}
			}
			// Dependency path distance buckets
			string distanceBucket = ">10";
			if (depparsePath.Count == 3)
			{
				distanceBucket = "<=3";
			}
			else
			{
				if (depparsePath.Count <= 5)
				{
					distanceBucket = "<=5";
				}
				else
				{
					if (depparsePath.Count <= 7)
					{
						distanceBucket = "<=7";
					}
					else
					{
						if (depparsePath.Count <= 9)
						{
							distanceBucket = "<=9";
						}
						else
						{
							if (depparsePath.Count <= 13)
							{
								distanceBucket = "<=13";
							}
							else
							{
								if (depparsePath.Count <= 17)
								{
									distanceBucket = "<=17";
								}
							}
						}
					}
				}
			}
			Indicator(feats, "parse_distance_between_entities_bucket", distanceBucket);
			// Add the path features
			if (depparsePath.Count > 2 && depparsePath.Count <= 7)
			{
				//      indicator(feats, "deppath", StringUtils.join(depparsePath.subList(1, depparsePath.size() - 1), ""));
				//      indicator(feats, "deppath_unlex", StringUtils.join(depparsePath.subList(1, depparsePath.size() - 1).stream().filter(x -> x.startsWith("-") || x.startsWith("<")), ""));
				Indicator(feats, "deppath_w/tag", sentence.PosTag(subjectHead) + StringUtils.Join(depparsePath.SubList(1, depparsePath.Count - 1), string.Empty) + sentence.PosTag(objectHead));
				Indicator(feats, "deppath_w/ner", input.subjectType + StringUtils.Join(depparsePath.SubList(1, depparsePath.Count - 1), string.Empty) + input.objectType);
			}
			// Add the edge features
			//noinspection Convert2streamapi
			foreach (string node in depparsePath)
			{
				if (!node.StartsWith("-") && !node.StartsWith("<-"))
				{
					Indicator(feats, "deppath_word", node);
				}
			}
			for (int i_2 = 0; i_2 < depparsePath.Count - 1; ++i_2)
			{
				Indicator(feats, "deppath_edge", depparsePath[i_2] + depparsePath[i_2 + 1]);
			}
			for (int i_3 = 0; i_3 < depparsePath.Count - 2; ++i_3)
			{
				Indicator(feats, "deppath_chunk", depparsePath[i_3] + depparsePath[i_3 + 1] + depparsePath[i_3 + 2]);
			}
		}

		private static void RelationSpecificFeatures(KBPRelationExtractor.KBPInput input, Sentence sentence, ClassicCounter<string> feats)
		{
			if (input.objectType.Equals(KBPRelationExtractor.NERTag.Number))
			{
				// Bucket the object value if it is a number
				// This is to prevent things like "age:9000" and to soft penalize "age:one"
				// The following features are extracted:
				//   1. Whether the object parses as a number (should always be true)
				//   2. Whether the object is an integer
				//   3. If the object is an integer, around what value is it (bucketed around common age values)
				//   4. Was the number spelled out, or written as a numeric number
				try
				{
					Number number = NumberNormalizer.WordToNumber(input.GetObjectText());
					if (number != null)
					{
						Indicator(feats, "obj_parsed_as_num", "t");
						if (number.Equals(number))
						{
							Indicator(feats, "obj_isint", "t");
							int numAsInt = number;
							string bucket = "<0";
							if (numAsInt == 0)
							{
								bucket = "0";
							}
							else
							{
								if (numAsInt == 1)
								{
									bucket = "1";
								}
								else
								{
									if (numAsInt < 5)
									{
										bucket = "<5";
									}
									else
									{
										if (numAsInt < 18)
										{
											bucket = "<18";
										}
										else
										{
											if (numAsInt < 25)
											{
												bucket = "<25";
											}
											else
											{
												if (numAsInt < 50)
												{
													bucket = "<50";
												}
												else
												{
													if (numAsInt < 80)
													{
														bucket = "<80";
													}
													else
													{
														if (numAsInt < 125)
														{
															bucket = "<125";
														}
														else
														{
															if (numAsInt >= 100)
															{
																bucket = ">125";
															}
														}
													}
												}
											}
										}
									}
								}
							}
							Indicator(feats, "obj_number_bucket", bucket);
						}
						else
						{
							Indicator(feats, "obj_isint", "f");
						}
						if (Sharpen.Runtime.EqualsIgnoreCase(input.GetObjectText().Replace(",", string.Empty), number.ToString()))
						{
							Indicator(feats, "obj_spelledout_num", "f");
						}
						else
						{
							Indicator(feats, "obj_spelledout_num", "t");
						}
					}
					else
					{
						Indicator(feats, "obj_parsed_as_num", "f");
					}
				}
				catch (NumberFormatException)
				{
					Indicator(feats, "obj_parsed_as_num", "f");
				}
				// Special case dashes and the String "one"
				if (input.GetObjectText().Contains("-"))
				{
					Indicator(feats, "obj_num_has_dash", "t");
				}
				else
				{
					Indicator(feats, "obj_num_has_dash", "f");
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(input.GetObjectText(), "one"))
				{
					Indicator(feats, "obj_num_is_one", "t");
				}
				else
				{
					Indicator(feats, "obj_num_is_one", "f");
				}
			}
			if ((input.subjectType == KBPRelationExtractor.NERTag.Person && input.objectType.Equals(KBPRelationExtractor.NERTag.Organization)) || (input.subjectType == KBPRelationExtractor.NERTag.Organization && input.objectType.Equals(KBPRelationExtractor.NERTag
				.Person)))
			{
				// Try to capture some denser features for employee_of
				// These are:
				//   1. Whether a TITLE tag occurs either before, after, or inside the relation span
				//   2. Whether a top employee trigger occurs either before, after, or inside the relation span
				Span relationSpan = Span.Union(input.subjectSpan, input.objectSpan);
				// (triggers before span)
				for (int i = Math.Max(0, relationSpan.Start() - 5); i < relationSpan.Start(); ++i)
				{
					if ("TITLE".Equals(sentence.NerTag(i)))
					{
						Indicator(feats, "title_before", "t");
					}
					if (TopEmployeeTriggers.Contains(sentence.Word(i).ToLower()))
					{
						Indicator(feats, "top_employee_trigger_before", "t");
					}
				}
				// (triggers after span)
				for (int i_1 = relationSpan.End(); i_1 < Math.Min(sentence.Length(), relationSpan.End()); ++i_1)
				{
					if ("TITLE".Equals(sentence.NerTag(i_1)))
					{
						Indicator(feats, "title_after", "t");
					}
					if (TopEmployeeTriggers.Contains(sentence.Word(i_1).ToLower()))
					{
						Indicator(feats, "top_employee_trigger_after", "t");
					}
				}
				// (triggers inside span)
				foreach (int i_2 in relationSpan)
				{
					if ("TITLE".Equals(sentence.NerTag(i_2)))
					{
						Indicator(feats, "title_inside", "t");
					}
					if (TopEmployeeTriggers.Contains(sentence.Word(i_2).ToLower()))
					{
						Indicator(feats, "top_employee_trigger_inside", "t");
					}
				}
			}
		}

		public static ICounter<string> Features(KBPRelationExtractor.KBPInput input)
		{
			// Get useful variables
			ClassicCounter<string> feats = new ClassicCounter<string>();
			if (Span.Overlaps(input.subjectSpan, input.objectSpan) || input.subjectSpan.Size() == 0 || input.objectSpan.Size() == 0)
			{
				return new ClassicCounter<string>();
			}
			// Actually featurize
			DenseFeatures(input, input.sentence, feats);
			SurfaceFeatures(input, input.sentence, feats);
			DependencyFeatures(input, input.sentence, feats);
			RelationSpecificFeatures(input, input.sentence, feats);
			return feats;
		}

		/// <summary>Create a classifier factory</summary>
		/// <?/>
		/// <returns>A factory to minimize a classifier against.</returns>
		private static LinearClassifierFactory<L, string> InitFactory<L>(double sigma)
		{
			LinearClassifierFactory<L, string> factory = new LinearClassifierFactory<L, string>();
			IFactory<IMinimizer<IDiffFunction>> minimizerFactory;
			switch (minimizer)
			{
				case KBPStatisticalExtractor.MinimizerType.Qn:
				{
					minimizerFactory = null;
					break;
				}

				case KBPStatisticalExtractor.MinimizerType.Sgd:
				{
					minimizerFactory = null;
					break;
				}

				case KBPStatisticalExtractor.MinimizerType.Hybrid:
				{
					factory.UseHybridMinimizerWithInPlaceSGD(100, 1000, sigma);
					minimizerFactory = null;
					break;
				}

				case KBPStatisticalExtractor.MinimizerType.L1:
				{
					minimizerFactory = null;
					break;
				}

				default:
				{
					throw new InvalidOperationException("Unknown minimizer: " + minimizer);
				}
			}
			factory.SetMinimizerCreator(minimizerFactory);
			return factory;
		}

		/// <summary>Train a multinomial classifier off of the provided dataset.</summary>
		/// <param name="dataset">The dataset to train the classifier off of.</param>
		/// <returns>A classifier.</returns>
		public static IClassifier<string, string> TrainMultinomialClassifier(GeneralDataset<string, string> dataset, int featureThreshold, double sigma)
		{
			// Set up the dataset and factory
			log.Info("Applying feature threshold (" + featureThreshold + ")...");
			dataset.ApplyFeatureCountThreshold(featureThreshold);
			log.Info("Randomizing dataset...");
			dataset.Randomize(42l);
			log.Info("Creating factory...");
			LinearClassifierFactory<string, string> factory = InitFactory(sigma);
			// Train the final classifier
			log.Info("BEGIN training");
			LinearClassifier<string, string> classifier = factory.TrainClassifier(dataset);
			log.Info("END training");
			// Debug
			KBPRelationExtractor.Accuracy trainAccuracy = new KBPRelationExtractor.Accuracy();
			foreach (IDatum<string, string> datum in dataset)
			{
				string guess = classifier.ClassOf(datum);
				trainAccuracy.Predict(Java.Util.Collections.Singleton(guess), Java.Util.Collections.Singleton(datum.Label()));
			}
			log.Info("Training accuracy:");
			log.Info(trainAccuracy.ToString());
			log.Info(string.Empty);
			// Return the classifier
			return classifier;
		}

		/// <summary>The implementing classifier of this extractor.</summary>
		public readonly IClassifier<string, string> classifier;

		/// <summary>Create a new KBP relation extractor, from the given implementing classifier.</summary>
		/// <param name="classifier">The implementing classifier.</param>
		public KBPStatisticalExtractor(IClassifier<string, string> classifier)
		{
			this.classifier = classifier;
		}

		/// <summary>
		/// Score the given input, returning both the classification decision and the
		/// probability of that decision.
		/// </summary>
		/// <remarks>
		/// Score the given input, returning both the classification decision and the
		/// probability of that decision.
		/// Note that this method will not return a relation which does not type check.
		/// </remarks>
		/// <param name="input">The input to classify.</param>
		/// <returns>A pair with the relation we classified into, along with its confidence.</returns>
		public virtual Pair<string, double> Classify(KBPRelationExtractor.KBPInput input)
		{
			RVFDatum<string, string> datum = new RVFDatum<string, string>(Features(input));
			ICounter<string> scores = classifier.ScoresOf(datum);
			Counters.ExpInPlace(scores);
			Counters.Normalize(scores);
			string best = Counters.Argmax(scores);
			// While it doesn't type check, continue going down the list.
			// NO_RELATION is always an option somewhere in there, so safe to keep going...
			while (!KBPRelationExtractorConstants.NoRelation.Equals(best) && scores.Size() > 1 && (!KBPRelationExtractor.RelationType.FromString(best).Get().validNamedEntityLabels.Contains(input.objectType) || KBPRelationExtractor.RelationType.FromString
				(best).Get().entityType != input.subjectType))
			{
				scores.Remove(best);
				Counters.Normalize(scores);
				best = Counters.Argmax(scores);
			}
			return Pair.MakePair(best, scores.GetCount(best));
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			RedwoodConfiguration.Standard().Apply();
			// Disable SLF4J crap.
			ArgumentParser.FillOptions(typeof(Edu.Stanford.Nlp.IE.KBPStatisticalExtractor), args);
			// Fill command-line options
			// Load the test (or dev) data
			Redwood.Util.ForceTrack("Test data");
			IList<Pair<KBPRelationExtractor.KBPInput, string>> testExamples = IKBPRelationExtractor.ReadDataset(TestFile);
			log.Info("Read " + testExamples.Count + " examples");
			Redwood.Util.EndTrack("Test data");
			// If we can't find an existing model, train one
			if (!IOUtils.ExistsInClasspathOrFileSystem(ModelFile))
			{
				Redwood.Util.ForceTrack("Training data");
				IList<Pair<KBPRelationExtractor.KBPInput, string>> trainExamples = IKBPRelationExtractor.ReadDataset(TrainFile);
				log.Info("Read " + trainExamples.Count + " examples");
				log.Info(string.Empty + trainExamples.Stream().Map(null).Filter(null).Count() + " are " + KBPRelationExtractorConstants.NoRelation);
				Redwood.Util.EndTrack("Training data");
				// Featurize + create the dataset
				Redwood.Util.ForceTrack("Creating dataset");
				RVFDataset<string, string> dataset = new RVFDataset<string, string>();
				AtomicInteger i = new AtomicInteger(0);
				long beginTime = Runtime.CurrentTimeMillis();
				trainExamples.Stream().Parallel().ForEach(null);
				// This takes a while per example
				trainExamples.Clear();
				// Free up some memory
				Redwood.Util.EndTrack("Creating dataset");
				// Train the classifier
				log.Info("Training classifier:");
				IClassifier<string, string> classifier = TrainMultinomialClassifier(dataset, FeatureThreshold, Sigma);
				dataset.Clear();
				// Free up some memory
				// Save the classifier
				IOUtils.WriteObjectToFile(new Edu.Stanford.Nlp.IE.KBPStatisticalExtractor(classifier), ModelFile);
			}
			// Read either a newly-trained or pre-trained model
			object model = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(ModelFile);
			Edu.Stanford.Nlp.IE.KBPStatisticalExtractor classifier_1;
			if (model is IClassifier)
			{
				//noinspection unchecked
				classifier_1 = new Edu.Stanford.Nlp.IE.KBPStatisticalExtractor((IClassifier<string, string>)model);
			}
			else
			{
				classifier_1 = ((Edu.Stanford.Nlp.IE.KBPStatisticalExtractor)model);
			}
			// Evaluate the model
			classifier_1.ComputeAccuracy(testExamples.Stream(), Predictions.Map(null));
		}
	}
}

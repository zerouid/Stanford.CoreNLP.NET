using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.International.Spanish;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Spanish;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish.Pipeline
{
	/// <summary>
	/// Clean up an AnCora treebank which has been processed to expand multi-word
	/// tokens into separate leaves.
	/// </summary>
	/// <remarks>
	/// Clean up an AnCora treebank which has been processed to expand multi-word
	/// tokens into separate leaves. (This prior splitting task is performed by
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishTreeNormalizer"/>
	/// through the
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishXMLTreeReader"/>
	/// class).
	/// </remarks>
	/// <author>Jon Gauthier</author>
	/// <author>Spence Green (original French version)</author>
	public sealed class MultiWordPreprocessor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(MultiWordPreprocessor));

		private static int nMissingPOS;

		private static int nMissingPhrasal;

		private static int nFixedPOS;

		private static int nFixedPhrasal;

		/// <summary>
		/// If a multiword token has a part-of-speech tag matching a key of
		/// this map, the constituent heading the split expression should
		/// have a label with the value corresponding to said key.
		/// </summary>
		/// <remarks>
		/// If a multiword token has a part-of-speech tag matching a key of
		/// this map, the constituent heading the split expression should
		/// have a label with the value corresponding to said key.
		/// e.g., since `(rg, grup.adv)` is in this map, we will eventually
		/// convert
		/// (rg cerca_de)
		/// to
		/// (grup.adv (rg cerca) (sp000 de))
		/// </remarks>
		private static readonly IDictionary<string, string> phrasalCategoryMap = new Dictionary<string, string>();

		static MultiWordPreprocessor()
		{
			phrasalCategoryMap["ao0000"] = "grup.a";
			phrasalCategoryMap["aq0000"] = "grup.a";
			phrasalCategoryMap["aqo000"] = "grup.a";
			phrasalCategoryMap["da0000"] = "spec";
			phrasalCategoryMap["di0000"] = "sn";
			phrasalCategoryMap["dn0000"] = "spec";
			phrasalCategoryMap["dt0000"] = "spec";
			phrasalCategoryMap["i"] = "interjeccio";
			phrasalCategoryMap["i00"] = "interjeccio";
			phrasalCategoryMap["rg"] = "grup.adv";
			phrasalCategoryMap["rn"] = "grup.adv";
			// no sólo
			phrasalCategoryMap["vaip000"] = "grup.verb";
			phrasalCategoryMap["vmg0000"] = "grup.verb";
			phrasalCategoryMap["vmic000"] = "grup.verb";
			phrasalCategoryMap["vmii000"] = "grup.verb";
			phrasalCategoryMap["vmif000"] = "grup.verb";
			phrasalCategoryMap["vmip000"] = "grup.verb";
			phrasalCategoryMap["vmis000"] = "grup.verb";
			phrasalCategoryMap["vmm0000"] = "grup.verb";
			phrasalCategoryMap["vmn0000"] = "grup.verb";
			phrasalCategoryMap["vmp0000"] = "grup.verb";
			phrasalCategoryMap["vmsi000"] = "grup.verb";
			phrasalCategoryMap["vmsp000"] = "grup.verb";
			phrasalCategoryMap["zm"] = "grup.nom";
			// New groups (not from AnCora specification)
			phrasalCategoryMap["cc"] = "grup.cc";
			phrasalCategoryMap["cs"] = "grup.cs";
			phrasalCategoryMap["pn000000"] = "grup.nom";
			phrasalCategoryMap["pi000000"] = "grup.pron";
			phrasalCategoryMap["pr000000"] = "grup.pron";
			phrasalCategoryMap["pt000000"] = "grup.pron";
			phrasalCategoryMap["px000000"] = "grup.pron";
			phrasalCategoryMap["sp000"] = "grup.prep";
			phrasalCategoryMap["w"] = "grup.w";
			phrasalCategoryMap["z"] = "grup.z";
			phrasalCategoryMap["z0"] = "grup.z";
			phrasalCategoryMap["zp"] = "grup.z";
			phrasalCategoryMap["zu"] = "grup.z";
		}

		private class ManualUWModel
		{
			private static readonly IDictionary<string, string> posMap = new Dictionary<string, string>();

			static ManualUWModel()
			{
				// i.e., "metros cúbicos"
				posMap["cúbico"] = "aq0000";
				posMap["cúbicos"] = "aq0000";
				posMap["diagonal"] = "aq0000";
				posMap["diestro"] = "aq0000";
				posMap["llevados"] = "aq0000";
				// llevados a cabo
				posMap["llevadas"] = "aq0000";
				// llevadas a cabo
				posMap["menudo"] = "aq0000";
				posMap["obstante"] = "aq0000";
				posMap["rapadas"] = "aq0000";
				// cabezas rapadas
				posMap["rasa"] = "aq0000";
				posMap["súbito"] = "aq0000";
				posMap["temática"] = "aq0000";
				posMap["tuya"] = "px000000";
				// foreign words
				posMap["alter"] = "nc0s000";
				posMap["ego"] = "nc0s000";
				posMap["Jet"] = "nc0s000";
				posMap["lag"] = "nc0s000";
				posMap["line"] = "nc0s000";
				posMap["lord"] = "nc0s000";
				posMap["model"] = "nc0s000";
				posMap["mortem"] = "nc0s000";
				// post-mortem
				posMap["pater"] = "nc0s000";
				// pater familias
				posMap["pipe"] = "nc0s000";
				posMap["play"] = "nc0s000";
				posMap["pollastre"] = "nc0s000";
				posMap["post"] = "nc0s000";
				posMap["power"] = "nc0s000";
				posMap["priori"] = "nc0s000";
				posMap["rock"] = "nc0s000";
				posMap["roll"] = "nc0s000";
				posMap["salubritatis"] = "nc0s000";
				posMap["savoir"] = "nc0s000";
				posMap["service"] = "nc0s000";
				posMap["status"] = "nc0s000";
				posMap["stem"] = "nc0s000";
				posMap["street"] = "nc0s000";
				posMap["task"] = "nc0s000";
				posMap["trio"] = "nc0s000";
				posMap["zigzag"] = "nc0s000";
				// foreign words (invariable)
				posMap["mass"] = "nc0n000";
				posMap["media"] = "nc0n000";
				// foreign words (plural)
				posMap["options"] = "nc0p000";
				// compound words, other invariables
				posMap["regañadientes"] = "nc0n000";
				posMap["sabiendas"] = "nc0n000";
				// a sabiendas (de)
				// common gender
				posMap["virgen"] = "nc0s000";
				posMap["merced"] = "ncfs000";
				posMap["miel"] = "ncfs000";
				posMap["torera"] = "ncfs000";
				posMap["ultranza"] = "ncfs000";
				posMap["vísperas"] = "ncfs000";
				posMap["acecho"] = "ncms000";
				posMap["alzamiento"] = "ncms000";
				posMap["bordo"] = "ncms000";
				posMap["cápita"] = "ncms000";
				posMap["ciento"] = "ncms000";
				posMap["cuño"] = "ncms000";
				posMap["pairo"] = "ncms000";
				posMap["pese"] = "ncms000";
				// pese a
				posMap["pique"] = "ncms000";
				posMap["pos"] = "ncms000";
				posMap["postre"] = "ncms000";
				posMap["pro"] = "ncms000";
				posMap["ralentí"] = "ncms000";
				posMap["ras"] = "ncms000";
				posMap["rebato"] = "ncms000";
				posMap["torno"] = "ncms000";
				posMap["través"] = "ncms000";
				posMap["creces"] = "ncfp000";
				posMap["cuestas"] = "ncfp000";
				posMap["oídas"] = "ncfp000";
				posMap["tientas"] = "ncfp000";
				posMap["trizas"] = "ncfp000";
				posMap["veras"] = "ncfp000";
				posMap["abuelos"] = "ncmp000";
				posMap["ambages"] = "ncmp000";
				posMap["modos"] = "ncmp000";
				posMap["pedazos"] = "ncmp000";
				posMap["A"] = "sps00";
				posMap["amén"] = "rg";
				// amén de
				posMap["Bailando"] = "vmg0000";
				posMap["Soñando"] = "vmg0000";
				posMap["Teniendo"] = "vmg0000";
				posMap["echaremos"] = "vmif000";
				posMap["formaba"] = "vmii000";
				posMap["Formabas"] = "vmii000";
				posMap["Forman"] = "vmip000";
				posMap["perece"] = "vmip000";
				posMap["PONE"] = "vmip000";
				posMap["suicídate"] = "vmm0000";
				posMap["tardar"] = "vmn0000";
				posMap["seiscientas"] = "z0";
				posMap["trescientas"] = "z0";
				posMap["cc"] = "zu";
				posMap["km"] = "zu";
				posMap["kms"] = "zu";
			}

			private static int nUnknownWordTypes = posMap.Count;

			private static readonly Pattern digit = Pattern.Compile("\\d+");

			private static readonly Pattern participle = Pattern.Compile("[ai]d[oa]$");

			/// <summary>
			/// Names which would be mistakenly marked as function words by
			/// unigram tagger (and which never appear as function words in
			/// multi-word tokens)
			/// </summary>
			private static readonly ICollection<string> actuallyNames = new HashSet<string>(Arrays.AsList("Avenida", "Contra", "Gracias", "in", "Mercado", "Jesús", "Salvo", "Van"));

			private static readonly Pattern otherNamePattern = Pattern.Compile("\\b(Al\\w+|A[^l]\\w*|[B-Z]\\w+)");

			private static readonly Pattern otherNamePattern2 = Pattern.Compile("\\b(A\\w+|[B-Z]\\w+)");

			private static readonly Pattern pPronounDeterminers = Pattern.Compile("(tod|otr|un)[oa]s?");

			// interjection
			// preposition; only appears in corpus as "in extremis" (preposition)
			// interjection
			// verb
			// Name-looking word that isn't "Al"
			// Name-looking word that isn't "A"
			// Determiners which may also appear as pronouns
			public static string GetOverrideTag(string word, string containingPhrase)
			{
				if (containingPhrase == null)
				{
					return null;
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(word, "este") && !containingPhrase.StartsWith(word))
				{
					return "np00000";
				}
				else
				{
					if (word.Equals("contra") && (containingPhrase.StartsWith("en contra") || containingPhrase.StartsWith("En contra")))
					{
						return "nc0s000";
					}
					else
					{
						if (word.Equals("total") && containingPhrase.StartsWith("ese"))
						{
							return "nc0s000";
						}
						else
						{
							if (word.Equals("DEL"))
							{
								// Uses of "Del" in corpus are proper nouns, but uses of "DEL" are
								// prepositions.. convenient for our purposes
								return "sp000";
							}
							else
							{
								if (word.Equals("sí") && containingPhrase.Contains("por sí") || containingPhrase.Contains("fuera de sí"))
								{
									return "pp000000";
								}
								else
								{
									if (pPronounDeterminers.Matcher(word).Matches() && containingPhrase.EndsWith(word))
									{
										// Determiners tailing a phrase are pronouns: "sobre todo," "al otro", etc.
										return "pi000000";
									}
									else
									{
										if (word.Equals("cuando") && containingPhrase.EndsWith(word))
										{
											return "pi000000";
										}
										else
										{
											if ((Sharpen.Runtime.EqualsIgnoreCase(word, "contra") && containingPhrase.EndsWith(word)))
											{
												return "nc0s000";
											}
											else
											{
												if (word.Equals("salvo") && containingPhrase.EndsWith("salvo"))
												{
													return "aq0000";
												}
												else
												{
													if (word.Equals("mira") && containingPhrase.EndsWith(word))
													{
														return "nc0s000";
													}
													else
													{
														if (word.Equals("pro") && containingPhrase.StartsWith("en pro"))
														{
															return "nc0s000";
														}
														else
														{
															if (word.Equals("espera") && containingPhrase.EndsWith("espera de"))
															{
																return "nc0s000";
															}
															else
															{
																if (word.Equals("Paso") && containingPhrase.Equals("El Paso"))
																{
																	return "np00000";
																}
																else
																{
																	if (word.Equals("medio") && (containingPhrase.EndsWith("medio de") || containingPhrase.EndsWith("ambiente") || containingPhrase.EndsWith("por medio") || containingPhrase.Contains("por medio") || containingPhrase.EndsWith("medio")))
																	{
																		return "nc0s000";
																	}
																	else
																	{
																		if (word.Equals("Medio") && containingPhrase.Contains("Ambiente"))
																		{
																			return "nc0s000";
																		}
																		else
																		{
																			if (word.Equals("Medio") && containingPhrase.Equals("Oriente Medio"))
																			{
																				return "aq0000";
																			}
																			else
																			{
																				if (word.Equals("media") && containingPhrase.Equals("mass media"))
																				{
																					return "nc0n000";
																				}
																				else
																				{
																					if (word.Equals("cuenta"))
																					{
																						// tomar en cuenta, darse cuenta de, ...
																						return "nc0s000";
																					}
																					else
																					{
																						if (word.Equals("h") && containingPhrase.StartsWith("km"))
																						{
																							return "zu";
																						}
																						else
																						{
																							if (word.Equals("A") && (containingPhrase.Contains("-") || containingPhrase.Contains(",") || otherNamePattern2.Matcher(containingPhrase).Find() || containingPhrase.Equals("terminal A")))
																							{
																								return "np00000";
																							}
																							else
																							{
																								if (word.Equals("forma") && containingPhrase.StartsWith("forma parte"))
																								{
																									return "vmip000";
																								}
																								else
																								{
																									if (word.Equals("Sin") && containingPhrase.Contains("Jaime"))
																									{
																										return "np00000";
																									}
																									else
																									{
																										if (word.Equals("di") && containingPhrase.Contains("di cuenta"))
																										{
																											return "vmis000";
																										}
																										else
																										{
																											if (word.Equals("demos") && containingPhrase.Contains("demos cuenta"))
																											{
																												return "vmsp000";
																											}
																											else
																											{
																												if ((word.Equals("van") || word.Equals("den")) && containingPhrase.Contains("van den"))
																												{
																													return "np00000";
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				if (word.Equals("Al"))
				{
					// "Al" is sometimes a part of name phrases: Arabic names, Al Gore, etc.
					// Mark it a noun if its containing phrase has some other capitalized word
					if (otherNamePattern.Matcher(containingPhrase).Find())
					{
						return "np00000";
					}
					else
					{
						return "sp000";
					}
				}
				if (actuallyNames.Contains(word))
				{
					return "np00000";
				}
				if (word.Equals("sino") && containingPhrase.EndsWith(word))
				{
					return "nc0s000";
				}
				else
				{
					if (word.Equals("mañana") || word.Equals("paso") || word.Equals("monta") || word.Equals("deriva") || word.Equals("visto"))
					{
						return "nc0s000";
					}
					else
					{
						if (word.Equals("frente") && containingPhrase.StartsWith("al frente"))
						{
							return "nc0s000";
						}
					}
				}
				return null;
			}

			/// <summary>
			/// Match phrases for which unknown words should be assumed to be
			/// common nouns
			/// - a trancas y barrancas
			/// - en vez de, en pos de
			/// - sin embargo
			/// - merced a
			/// - pese a que
			/// </summary>
			private static readonly Pattern commonPattern = Pattern.Compile("^al? |^en .+ de$|sin | al?$| que$", Pattern.CaseInsensitive);

			public static string GetTag(string word, string containingPhrase)
			{
				// Exact matches
				if (word.Equals("%"))
				{
					return "ft";
				}
				else
				{
					if (word.Equals("+"))
					{
						return "fz";
					}
					else
					{
						if (word.Equals("&") || word.Equals("@"))
						{
							return "f0";
						}
					}
				}
				if (digit.Matcher(word).Find())
				{
					return "z0";
				}
				else
				{
					if (posMap.Contains(word))
					{
						return posMap[word];
					}
				}
				// Fallbacks
				if (participle.Matcher(word).Find())
				{
					return "aq0000";
				}
				// One last hint: is the phrase one which we have designated to
				// contain mostly common nouns?
				if (commonPattern.Matcher(word).Matches())
				{
					return "ncms000";
				}
				// Now make an educated guess.
				//log.info("No POS tag for " + word);
				return "np00000";
			}
		}

		/// <summary>Source training data for a unigram tagger from the given tree.</summary>
		public static void UpdateTagger(TwoDimensionalCounter<string, string> tagger, Tree t)
		{
			IList<CoreLabel> yield = t.TaggedLabeledYield();
			foreach (CoreLabel cl in yield)
			{
				if (cl.Tag().Equals(SpanishTreeNormalizer.MwTag))
				{
					continue;
				}
				tagger.IncrementCount(cl.Word(), cl.Tag());
			}
		}

		public static void TraverseAndFix(Tree t, Tree parent, TwoDimensionalCounter<string, string> unigramTagger, bool retainNER)
		{
			if (t.IsPreTerminal())
			{
				if (t.Value().Equals(SpanishTreeNormalizer.MwTag))
				{
					nMissingPOS++;
					string pos = InferPOS(t, parent, unigramTagger);
					if (pos != null)
					{
						t.SetValue(pos);
						nFixedPOS++;
					}
				}
				return;
			}
			foreach (Tree kid in t.Children())
			{
				TraverseAndFix(kid, t, unigramTagger, retainNER);
			}
			// Post-order visit
			if (t.Value().StartsWith(SpanishTreeNormalizer.MwPhraseTag))
			{
				nMissingPhrasal++;
				string phrasalCat = InferPhrasalCategory(t, retainNER);
				if (phrasalCat != null)
				{
					t.SetValue(phrasalCat);
					nFixedPhrasal++;
				}
			}
		}

		/// <summary>Get a string representation of the immediate phrase which contains the given node.</summary>
		private static string GetContainingPhrase(Tree t, Tree parent)
		{
			if (parent == null)
			{
				return null;
			}
			IList<ILabel> phraseYield = parent.Yield();
			StringBuilder containingPhrase = new StringBuilder();
			foreach (ILabel l in phraseYield)
			{
				containingPhrase.Append(l.Value()).Append(" ");
			}
			return Sharpen.Runtime.Substring(containingPhrase.ToString(), 0, containingPhrase.Length - 1);
		}

		private static readonly SpanishVerbStripper verbStripper = SpanishVerbStripper.GetInstance();

		/// <summary>
		/// Attempt to infer the part of speech of the given preterminal node, which
		/// was created during the expansion of a multi-word token.
		/// </summary>
		private static string InferPOS(Tree t, Tree parent, TwoDimensionalCounter<string, string> unigramTagger)
		{
			string word = t.FirstChild().Value();
			string containingPhraseStr = GetContainingPhrase(t, parent);
			// Overrides: let the manual POS model handle a few special cases first
			string overrideTag = MultiWordPreprocessor.ManualUWModel.GetOverrideTag(word, containingPhraseStr);
			if (overrideTag != null)
			{
				return overrideTag;
			}
			ICollection<string> unigramTaggerKeys = unigramTagger.FirstKeySet();
			// Try treating this word as a verb and stripping any clitic
			// pronouns. If the stripped version exists in the unigram
			// tagger, then stick with the verb hypothesis
			SpanishVerbStripper.StrippedVerb strippedVerb = verbStripper.SeparatePronouns(word);
			if (strippedVerb != null && unigramTaggerKeys.Contains(strippedVerb.GetStem()))
			{
				string pos = Counters.Argmax(unigramTagger.GetCounter(strippedVerb.GetStem()));
				if (pos.StartsWith("v"))
				{
					return pos;
				}
			}
			if (unigramTagger.FirstKeySet().Contains(word))
			{
				return Counters.Argmax(unigramTagger.GetCounter(word), new MultiWordPreprocessor.POSTieBreaker());
			}
			return MultiWordPreprocessor.ManualUWModel.GetTag(word, containingPhraseStr);
		}

		/// <summary>Resolves "ties" between candidate part-of-speech tags encountered by the unigram tagger.</summary>
		private class POSTieBreaker : IComparator<string>
		{
			public virtual int Compare(string o1, string o2)
			{
				bool firstIsNoun = o1.StartsWith("n");
				bool secondIsNoun = o2.StartsWith("n");
				// Prefer nouns over everything
				if (firstIsNoun && !secondIsNoun)
				{
					return -1;
				}
				else
				{
					if (secondIsNoun && !firstIsNoun)
					{
						return 1;
					}
				}
				// No other policies at the moment
				return 0;
			}
		}

		/// <summary>
		/// Attempt to infer the phrasal category of the given node, which
		/// heads words which were expanded from a multi-word token.
		/// </summary>
		private static string InferPhrasalCategory(Tree t, bool retainNER)
		{
			string phraseValue = t.Value();
			// Retrieve the part-of-speech assigned to the original multi-word
			// token
			string originalPos = Sharpen.Runtime.Substring(phraseValue, phraseValue.LastIndexOf('_') + 1);
			if (phrasalCategoryMap.Contains(originalPos))
			{
				return phrasalCategoryMap[originalPos];
			}
			else
			{
				if (originalPos.Length > 0 && originalPos[0] == 'n')
				{
					// TODO may lead to some funky trees if a child somehow gets an
					// incorrect tag -- e.g. we may have a `grup.nom` head a `vmis000`
					if (!retainNER)
					{
						return "grup.nom";
					}
					char nerTag = phraseValue[phraseValue.Length - 1];
					switch (nerTag)
					{
						case 'l':
						{
							return "grup.nom.lug";
						}

						case 'o':
						{
							return "grup.nom.org";
						}

						case 'p':
						{
							return "grup.nom.pers";
						}

						case '0':
						{
							return "grup.nom.otros";
						}

						default:
						{
							return "grup.nom";
						}
					}
				}
			}
			// Fallback: try to infer based on part-of-speech sequence formed by
			// constituents
			StringBuilder sb = new StringBuilder();
			foreach (Tree kid in t.Children())
			{
				sb.Append(kid.Value()).Append(" ");
			}
			string posSequence = sb.ToString().Trim();
			log.Info("No phrasal cat for: " + posSequence + " (original POS of MWE: " + originalPos + ")");
			// Give up.
			return null;
		}

		private static void ResolveDummyTags(File treeFile, TwoDimensionalCounter<string, string> unigramTagger, bool retainNER, TreeNormalizer tn)
		{
			ITreeFactory tf = new LabeledScoredTreeFactory();
			MultiWordTreeExpander expander = new MultiWordTreeExpander();
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8"));
				ITreeReaderFactory trf = new SpanishTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				PrintWriter pw = new PrintWriter(new TextWriter(new FileOutputStream(new File(treeFile + ".fixed")), false, "UTF-8"));
				int nTrees = 0;
				for (Tree t; (t = tr.ReadTree()) != null; nTrees++)
				{
					TraverseAndFix(t, null, unigramTagger, retainNER);
					// Now "decompress" further the expanded trees formed by
					// multiword token splitting
					t = expander.ExpandPhrases(t, tn, tf);
					if (tn != null)
					{
						t = tn.NormalizeWholeTree(t, tf);
					}
					pw.Println(t.ToString());
				}
				pw.Close();
				tr.Close();
				System.Console.Out.WriteLine("Processed " + nTrees + " trees");
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}

		private static string Usage()
		{
			StringBuilder sb = new StringBuilder();
			string nl = Runtime.GetProperty("line.separator");
			sb.Append(string.Format("Usage: java %s [OPTIONS] treebank-file%n", typeof(MultiWordPreprocessor).FullName));
			sb.Append("Options:").Append(nl);
			sb.Append("   -help: Print this message").Append(nl);
			sb.Append("   -ner: Retain NER information in tree constituents (pre-pre-terminal nodes)").Append(nl);
			sb.Append("   -normalize {true, false}: Run the Spanish tree normalizer (non-aggressive) on the output of the main routine (true by default)").Append(nl);
			return sb.ToString();
		}

		private static readonly IDictionary<string, int> argOptionDefs;

		static MultiWordPreprocessor()
		{
			argOptionDefs = Generics.NewHashMap();
			argOptionDefs["help"] = 0;
			argOptionDefs["ner"] = 0;
			argOptionDefs["normalize"] = 1;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			Properties options = StringUtils.ArgsToProperties(args, argOptionDefs);
			if (!options.Contains(string.Empty) || options.Contains("help"))
			{
				log.Info(Usage());
				return;
			}
			bool retainNER = PropertiesUtils.GetBool(options, "ner", false);
			bool normalize = PropertiesUtils.GetBool(options, "normalize", true);
			File treeFile = new File(options.GetProperty(string.Empty));
			TwoDimensionalCounter<string, string> labelTerm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> termLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> labelPreterm = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> pretermLabel = new TwoDimensionalCounter<string, string>();
			TwoDimensionalCounter<string, string> unigramTagger = new TwoDimensionalCounter<string, string>();
			try
			{
				BufferedReader br = new BufferedReader(new InputStreamReader(new FileInputStream(treeFile), "UTF-8"));
				ITreeReaderFactory trf = new SpanishTreeReaderFactory();
				ITreeReader tr = trf.NewTreeReader(br);
				for (Tree t; (t = tr.ReadTree()) != null; )
				{
					UpdateTagger(unigramTagger, t);
				}
				tr.Close();
				//Closes the underlying reader
				System.Console.Out.WriteLine("Resolving DUMMY tags");
				ResolveDummyTags(treeFile, unigramTagger, retainNER, normalize ? new SpanishTreeNormalizer(true, false, false) : null);
				System.Console.Out.WriteLine("#Unknown Word Types: " + MultiWordPreprocessor.ManualUWModel.nUnknownWordTypes);
				System.Console.Out.WriteLine(string.Format("#Missing POS: %d (fixed: %d, %.2f%%)", nMissingPOS, nFixedPOS, (double)nFixedPOS / nMissingPOS * 100));
				System.Console.Out.WriteLine(string.Format("#Missing Phrasal: %d (fixed: %d, %.2f%%)", nMissingPhrasal, nFixedPhrasal, (double)nFixedPhrasal / nMissingPhrasal * 100));
				System.Console.Out.WriteLine("Done!");
			}
			catch (UnsupportedEncodingException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (FileNotFoundException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
		}
	}
}

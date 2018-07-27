using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;




namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>
	/// This class takes a
	/// <see cref="SentenceFragment"/>
	/// and converts it to a conventional
	/// OpenIE triple, as materialized in the
	/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
	/// class.
	/// </summary>
	/// <author>Gabor Angeli</author>
	public class RelationTripleSegmenter
	{
		private readonly bool allowNominalsWithoutNER;

		private sealed class _List_31 : List<SemgrexPattern>
		{
			public _List_31()
			{
				{
					// { blue cats play [quietly] with yarn,
					//   Jill blew kisses at Jack,
					//   cats are standing next to dogs }
					this.Add(SemgrexPattern.Compile("{$}=verb ?>/cop|aux(pass)?/ {}=be >/.subj(pass)?/ {}=subject >/(nmod|acl|advcl):.*/=prepEdge ( {}=object ?>appos {} = appos ?>case {}=prep) ?>dobj {pos:/N.*/}=relObj"));
					// { cats are cute,
					//   horses are grazing peacefully }
					this.Add(SemgrexPattern.Compile("{$}=object >/.subj(pass)?/ {}=subject >/cop|aux(pass)?/ {}=verb ?>case {}=prep"));
					// { fish like to swim }
					this.Add(SemgrexPattern.Compile("{$}=verb >/.subj(pass)?/ {}=subject >xcomp ( {}=object ?>appos {}=appos )"));
					// { cats have tails }
					this.Add(SemgrexPattern.Compile("{$}=verb ?>/aux(pass)?/ {}=be >/.subj(pass)?/ {}=subject >/[di]obj|xcomp/ ( {}=object ?>appos {}=appos )"));
					// { Tom and Jerry were fighting }
					this.Add(SemgrexPattern.Compile("{$}=verb >/nsubj(pass)?/ ( {}=subject >/conj:and/=subjIgnored {}=object )"));
					// { mass of iron is 55amu }
					this.Add(SemgrexPattern.Compile("{pos:/NNS?/}=object >cop {}=relappend1 >/nsubj(pass)?/ ( {}=verb >/nmod:of/ ( {pos:/NNS?/}=subject >case {}=relappend0 ) )"));
				}
			}
		}

		/// <summary>A list of patterns to match relation extractions against</summary>
		public readonly IList<SemgrexPattern> VerbPatterns = Java.Util.Collections.UnmodifiableList(new _List_31());

		private sealed class _List_56 : List<SemgrexPattern>
		{
			public _List_56()
			{
				{
					foreach (SemgrexPattern pattern in this._enclosing.VerbPatterns)
					{
						string fullPattern = pattern.Pattern();
						string vpPattern = fullPattern.Replace(">/.subj(pass)?/ {}=subject", string.Empty).Replace("$", "pos:/V.*/");
						// drop the subject
						// but, force the root to be on a verb
						this.Add(SemgrexPattern.Compile(vpPattern));
					}
				}
			}
		}

		/// <summary>
		/// <p>
		/// A set of derivative patterns from
		/// <see cref="VerbPatterns"/>
		/// that ignore the subject
		/// arc. This is useful primarily for creating a training set for the clause splitter which emulates the
		/// behavior of the relation triple segmenter component.
		/// </p>
		/// </summary>
		public readonly IList<SemgrexPattern> VpPatterns;

		private sealed class _List_69 : List<TokenSequencePattern>
		{
			public _List_69()
			{
				{
					// { NER nominal_verb NER,
					//   United States president Obama }
					this.Add(TokenSequencePattern.Compile("(?$object [ner:/PERSON|ORGANIZATION|LOCATION+/]+ ) (?$beof_comp [ {tag:/NN.*/} & !{ner:/PERSON|ORGANIZATION|LOCATION/} ]+ ) (?$subject [ner:/PERSON|ORGANIZATION|LOCATION/]+ )"));
					// { NER 's nominal_verb NER,
					//   America 's president , Obama }
					this.Add(TokenSequencePattern.Compile("(?$object [ner:/PERSON|ORGANIZATION|LOCATION+/]+ ) /'s/ (?$beof_comp [ {tag:/NN.*/} & !{ner:/PERSON|ORGANIZATION|LOCATION/} ]+ ) /,/? (?$subject [ner:/PERSON|ORGANIZATION|LOCATION/]+ )"));
					// { NER , NER ,,
					//   Obama, 28, ...,
					//   Obama (28) ...}
					this.Add(TokenSequencePattern.Compile("(?$subject [ner:/PERSON|ORGANIZATION|LOCATION/]+ ) /,/ (?$object [ner:/NUMBER|DURATION|PERSON|ORGANIZATION/]+ ) /,/"));
					this.Add(TokenSequencePattern.Compile("(?$subject [ner:/PERSON|ORGANIZATION|LOCATION/]+ ) /\\(/ (?$object [ner:/NUMBER|DURATION|PERSON|ORGANIZATION/]+ ) /\\)/"));
				}
			}
		}

		/// <summary>A set of nominal patterns, that don't require being in a coherent clause, but do require NER information.</summary>
		public readonly IList<TokenSequencePattern> NounTokenPatterns = Java.Util.Collections.UnmodifiableList(new _List_69());

		/// <summary>A set of nominal patterns using dependencies, that don't require being in a coherent clause, but do require NER information.</summary>
		private readonly IList<SemgrexPattern> NounDependencyPatterns;

		/// <summary>Create a new relation triple segmenter.</summary>
		/// <param name="allowNominalsWithoutNER">
		/// If true, extract all nominal relations and not just those which are warranted based on
		/// named entity tags. For most practical applications, this greatly over-produces trivial triples.
		/// </param>
		public RelationTripleSegmenter(bool allowNominalsWithoutNER)
		{
			VpPatterns = Java.Util.Collections.UnmodifiableList(new _List_56(this));
			this.allowNominalsWithoutNER = allowNominalsWithoutNER;
			NounDependencyPatterns = Java.Util.Collections.UnmodifiableList(new _List_97(allowNominalsWithoutNER));
		}

		private sealed class _List_97 : List<SemgrexPattern>
		{
			public _List_97(bool allowNominalsWithoutNER)
			{
				this.allowNominalsWithoutNER = allowNominalsWithoutNER;
				{
					// { Durin, son of Thorin }
					this.Add(SemgrexPattern.Compile("{tag:/N.*/}=subject >appos ( {}=relation >/nmod:.*/=relaux {}=object)"));
					// { Thorin's son, Durin }
					this.Add(SemgrexPattern.Compile("{}=relation >/nmod:.*/=relaux {}=subject >appos {}=object"));
					// { Stanford's Chris Manning  }
					this.Add(SemgrexPattern.Compile("{tag:/N.*/}=object >/nmod:poss/=relaux ( {}=subject >case {} )"));
					// { Chris Manning of Stanford,
					//   [There are] cats with tails,
					if (allowNominalsWithoutNER)
					{
						this.Add(SemgrexPattern.Compile("{tag:/N.*/}=subject >/nmod:(?!poss).*/=relaux {}=object"));
					}
					else
					{
						this.Add(SemgrexPattern.Compile("{ner:/PERSON|ORGANIZATION|LOCATION/}=subject >/nmod:(?!poss).*/=relaux {ner:/..+/}=object"));
						this.Add(SemgrexPattern.Compile("{tag:/N.*/}=subject >/nmod:(in|with)/=relaux {}=object"));
					}
					//  { President Obama }
					if (allowNominalsWithoutNER)
					{
						this.Add(SemgrexPattern.Compile("{tag:/N.*/}=subject >/amod/=arc {}=object"));
					}
					else
					{
						this.Add(SemgrexPattern.Compile("{ner:/PERSON|ORGANIZATION|LOCATION/}=subject >/amod|compound/=arc {ner:/..+/}=object"));
					}
				}
			}

			private readonly bool allowNominalsWithoutNER;
		}

		/// <seealso cref="RelationTripleSegmenter(bool)"/>
		public RelationTripleSegmenter()
			: this(false)
		{
			VpPatterns = Java.Util.Collections.UnmodifiableList(new _List_56(this));
		}

		/// <summary>Extract the nominal patterns from this sentence.</summary>
		/// <seealso cref="NounTokenPatterns"/>
		/// <seealso cref="NounDependencyPatterns"/>
		/// <param name="parse">The parse tree of the sentence to annotate.</param>
		/// <param name="tokens">The tokens of the sentence to annotate.</param>
		/// <returns>
		/// A list of
		/// <see cref="Edu.Stanford.Nlp.IE.Util.RelationTriple"/>
		/// s. Note that these do not have an associated tree with them.
		/// </returns>
		public virtual IList<RelationTriple> Extract(SemanticGraph parse, IList<CoreLabel> tokens)
		{
			IList<RelationTriple> extractions = new List<RelationTriple>();
			ICollection<Triple<Span, string, Span>> alreadyExtracted = new HashSet<Triple<Span, string, Span>>();
			//
			// Run Token Patterns
			//
			foreach (TokenSequencePattern tokenPattern in NounTokenPatterns)
			{
				TokenSequenceMatcher tokenMatcher = tokenPattern.Matcher(tokens);
				while (tokenMatcher.Find())
				{
					bool missingPrefixBe;
					bool missingSuffixOf = false;
					// Create subject
					IList<ICoreMap> subject = tokenMatcher.GroupNodes("$subject");
					Span subjectSpan = Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(tokens, Span.FromValues(((CoreLabel)subject[0]).Index() - 1, ((CoreLabel)subject[subject.Count - 1]).Index()));
					IList<CoreLabel> subjectTokens = new List<CoreLabel>();
					foreach (int i in subjectSpan)
					{
						subjectTokens.Add(tokens[i]);
					}
					// Create object
					IList<ICoreMap> @object = tokenMatcher.GroupNodes("$object");
					Span objectSpan = Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(tokens, Span.FromValues(((CoreLabel)@object[0]).Index() - 1, ((CoreLabel)@object[@object.Count - 1]).Index()));
					if (Span.Overlaps(subjectSpan, objectSpan))
					{
						continue;
					}
					IList<CoreLabel> objectTokens = new List<CoreLabel>();
					foreach (int i_1 in objectSpan)
					{
						objectTokens.Add(tokens[i_1]);
					}
					// Create relation
					if (subjectTokens.Count > 0 && objectTokens.Count > 0)
					{
						IList<CoreLabel> relationTokens = new List<CoreLabel>();
						// (add the 'be')
						missingPrefixBe = true;
						// (add a complement to the 'be')
						IList<ICoreMap> beofComp = tokenMatcher.GroupNodes("$beof_comp");
						if (beofComp != null)
						{
							// (add the complement
							foreach (ICoreMap token in beofComp)
							{
								if (token is CoreLabel)
								{
									relationTokens.Add((CoreLabel)token);
								}
								else
								{
									relationTokens.Add(new CoreLabel(token));
								}
							}
							// (add the 'of')
							missingSuffixOf = true;
						}
						// Add extraction
						string relationGloss = StringUtils.Join(relationTokens.Stream().Map(null), " ");
						if (!alreadyExtracted.Contains(Triple.MakeTriple(subjectSpan, relationGloss, objectSpan)))
						{
							RelationTriple extraction = new RelationTriple(subjectTokens, relationTokens, objectTokens);
							//noinspection ConstantConditions
							extraction.IsPrefixBe(missingPrefixBe);
							extraction.IsSuffixOf(missingSuffixOf);
							extractions.Add(extraction);
							alreadyExtracted.Add(Triple.MakeTriple(subjectSpan, relationGloss, objectSpan));
						}
					}
				}
				//
				// Run Semgrex Matches
				//
				foreach (SemgrexPattern semgrex in NounDependencyPatterns)
				{
					SemgrexMatcher matcher = semgrex.Matcher(parse);
					while (matcher.Find())
					{
						bool missingPrefixBe = false;
						bool missingSuffixBe = false;
						bool istmod = false;
						// Get relaux if applicable
						string relaux = matcher.GetRelnString("relaux");
						string ignoredArc = relaux;
						if (ignoredArc == null)
						{
							ignoredArc = matcher.GetRelnString("arc");
						}
						// Create subject
						IndexedWord subject = matcher.GetNode("subject");
						IList<IndexedWord> subjectTokens = new List<IndexedWord>();
						Span subjectSpan;
						if (subject.Ner() != null && !"O".Equals(subject.Ner()))
						{
							subjectSpan = Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(tokens, Span.FromValues(subject.Index() - 1, subject.Index()));
							foreach (int i in subjectSpan)
							{
								subjectTokens.Add(new IndexedWord(tokens[i]));
							}
						}
						else
						{
							subjectTokens = GetValidChunk(parse, subject, ValidSubjectArcs, Optional.OfNullable(ignoredArc), true).OrElse(Java.Util.Collections.SingletonList(subject));
							subjectSpan = Edu.Stanford.Nlp.Naturalli.Util.TokensToSpan(subjectTokens);
						}
						// Create object
						IndexedWord @object = matcher.GetNode("object");
						IList<IndexedWord> objectTokens = new List<IndexedWord>();
						Span objectSpan;
						if (@object.Ner() != null && !"O".Equals(@object.Ner()))
						{
							objectSpan = Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(tokens, Span.FromValues(@object.Index() - 1, @object.Index()));
							foreach (int i in objectSpan)
							{
								objectTokens.Add(new IndexedWord(tokens[i]));
							}
						}
						else
						{
							objectTokens = GetValidChunk(parse, @object, ValidObjectArcs, Optional.OfNullable(ignoredArc), true).OrElse(Java.Util.Collections.SingletonList(@object));
							objectSpan = Edu.Stanford.Nlp.Naturalli.Util.TokensToSpan(objectTokens);
						}
						// Check that the pair is valid
						if (Span.Overlaps(subjectSpan, objectSpan))
						{
							continue;
						}
						// We extracted an identity
						if (subjectSpan.End() == objectSpan.Start() - 1 && (tokens[subjectSpan.End()].Word().Matches("[\\.,:;\\('\"]") || "CC".Equals(tokens[subjectSpan.End()].Tag())))
						{
							continue;
						}
						// We're straddling a clause
						if (objectSpan.End() == subjectSpan.Start() - 1 && (tokens[objectSpan.End()].Word().Matches("[\\.,:;\\('\"]") || "CC".Equals(tokens[objectSpan.End()].Tag())))
						{
							continue;
						}
						// We're straddling a clause
						// Get any prepositional edges
						string expected = relaux == null ? string.Empty : Sharpen.Runtime.Substring(relaux, relaux.IndexOf(":") + 1).Replace("_", " ");
						IndexedWord prepWord = null;
						// (these usually come from the object)
						bool prepositionIsPrefix = false;
						foreach (SemanticGraphEdge edge in parse.OutgoingEdgeIterable(@object))
						{
							if (edge.GetRelation().ToString().Equals("case"))
							{
								prepWord = edge.GetDependent();
							}
						}
						// (...but sometimes from the subject)
						if (prepWord == null)
						{
							foreach (SemanticGraphEdge edge_1 in parse.OutgoingEdgeIterable(subject))
							{
								if (edge_1.GetRelation().ToString().Equals("case"))
								{
									prepositionIsPrefix = true;
									prepWord = edge_1.GetDependent();
								}
							}
						}
						IList<IndexedWord> prepChunk = Java.Util.Collections.EmptyList;
						if (prepWord != null && !expected.Equals("tmod"))
						{
							Optional<IList<IndexedWord>> optionalPrepChunk = GetValidChunk(parse, prepWord, Java.Util.Collections.Singleton("mwe"), Optional.Empty(), true);
							if (!optionalPrepChunk.IsPresent())
							{
								continue;
							}
							prepChunk = optionalPrepChunk.Get();
							prepChunk.Sort(null);
						}
						// ascending sort
						// Get the relation
						if (subjectTokens.Count > 0 && objectTokens.Count > 0)
						{
							LinkedList<IndexedWord> relationTokens = new LinkedList<IndexedWord>();
							IndexedWord relNode = matcher.GetNode("relation");
							if (relNode != null)
							{
								// Case: we have a grounded relation span
								// (add the relation)
								relationTokens.Add(relNode);
								// (add any prepositional case markings)
								if (prepositionIsPrefix)
								{
									missingSuffixBe = true;
									// We're almost certainly missing a suffix 'be'
									for (int i = prepChunk.Count - 1; i >= 0; --i)
									{
										relationTokens.AddFirst(prepChunk[i]);
									}
								}
								else
								{
									Sharpen.Collections.AddAll(relationTokens, prepChunk);
								}
								if (Sharpen.Runtime.EqualsIgnoreCase(expected, "tmod"))
								{
									istmod = true;
								}
							}
							else
							{
								// Case: we have a hallucinated relation span
								// (mark it as missing a preceding 'be'
								if (!expected.Equals("poss"))
								{
									missingPrefixBe = true;
								}
								// (add any prepositional case markings)
								if (prepositionIsPrefix)
								{
									for (int i = prepChunk.Count - 1; i >= 0; --i)
									{
										relationTokens.AddFirst(prepChunk[i]);
									}
								}
								else
								{
									Sharpen.Collections.AddAll(relationTokens, prepChunk);
								}
								if (Sharpen.Runtime.EqualsIgnoreCase(expected, "tmod"))
								{
									istmod = true;
								}
								// (some fine-tuning)
								if (allowNominalsWithoutNER && "of".Equals(expected))
								{
									continue;
								}
							}
							// prohibit things like "conductor of electricity" -> "conductor; be of; electricity"
							// Add extraction
							string relationGloss = StringUtils.Join(relationTokens.Stream().Map(null), " ");
							if (!alreadyExtracted.Contains(Triple.MakeTriple(subjectSpan, relationGloss, objectSpan)))
							{
								RelationTriple extraction = new RelationTriple(subjectTokens.Stream().Map(null).Collect(Collectors.ToList()), relationTokens.Stream().Map(null).Collect(Collectors.ToList()), objectTokens.Stream().Map(null).Collect(Collectors.ToList()));
								extraction.Istmod(istmod);
								extraction.IsPrefixBe(missingPrefixBe);
								extraction.IsSuffixBe(missingSuffixBe);
								extractions.Add(extraction);
								alreadyExtracted.Add(Triple.MakeTriple(subjectSpan, relationGloss, objectSpan));
							}
						}
					}
				}
			}
			//
			// Filter downward polarity extractions
			//
			IEnumerator<RelationTriple> iter = extractions.GetEnumerator();
			while (iter.MoveNext())
			{
				RelationTriple term = iter.Current;
				bool shouldRemove = true;
				foreach (CoreLabel token in term)
				{
					if (token.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation)) == null || !token.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation)).IsDownwards())
					{
						shouldRemove = false;
					}
				}
				if (shouldRemove)
				{
					iter.Remove();
				}
			}
			// Don't extract things in downward polarity contexts.
			// Return
			return extractions;
		}

		private sealed class _HashSet_386 : HashSet<string>
		{
			public _HashSet_386()
			{
				{
					//  /**
					//   * A counter keeping track of how many times a given pattern has matched. This allows us to learn to iterate
					//   * over patterns in the optimal order; this is just an efficiency tweak (but an effective one!).
					//   */
					//  private final Counter<SemgrexPattern> VERB_PATTERN_HITS = new ClassicCounter<>();
					this.Add("amod");
					this.Add("compound");
					this.Add("aux");
					this.Add("nummod");
					this.Add("nmod:poss");
					this.Add("nmod:tmod");
					this.Add("expl");
					this.Add("nsubj");
					this.Add("case");
				}
			}
		}

		/// <summary>A set of valid arcs denoting a subject entity we are interested in</summary>
		public readonly ICollection<string> ValidSubjectArcs = Java.Util.Collections.UnmodifiableSet(new _HashSet_386());

		private sealed class _HashSet_392 : HashSet<string>
		{
			public _HashSet_392()
			{
				{
					this.Add("amod");
					this.Add("compound");
					this.Add("aux");
					this.Add("nummod");
					this.Add("nmod");
					this.Add("nsubj");
					this.Add("nmod:*");
					this.Add("nmod:poss");
					this.Add("nmod:tmod");
					this.Add("conj:and");
					this.Add("advmod");
					this.Add("acl");
					this.Add("case");
				}
			}
		}

		/// <summary>A set of valid arcs denoting an object entity we are interested in</summary>
		public readonly ICollection<string> ValidObjectArcs = Java.Util.Collections.UnmodifiableSet(new _HashSet_392());

		private sealed class _HashSet_399 : HashSet<string>
		{
			public _HashSet_399()
			{
				{
					// add("advcl"); // Born in Hawaii, Obama is a US citizen; citizen -advcl-> Born.
					this.Add("amod");
					this.Add("advmod");
					this.Add("conj");
					this.Add("cc");
					this.Add("conj:and");
					this.Add("conj:or");
					this.Add("auxpass");
					this.Add("compound:*");
				}
			}
		}

		/// <summary>A set of valid arcs denoting an adverbial modifier we are interested in</summary>
		public readonly ICollection<string> ValidAdverbArcs = Java.Util.Collections.UnmodifiableSet(new _HashSet_399());

		/// <seealso cref="GetValidSubjectChunk(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord, Java.Util.Optional{T})"/>
		/// <seealso cref="GetValidObjectChunk(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord, Java.Util.Optional{T})"/>
		/// <seealso cref="GetValidAdverbChunk(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord, Java.Util.Optional{T})"/>
		protected internal virtual Optional<IList<IndexedWord>> GetValidChunk(SemanticGraph parse, IndexedWord originalRoot, ICollection<string> validArcs, Optional<string> ignoredArc, bool allowExtraArcs)
		{
			IPriorityQueue<IndexedWord> chunk = new FixedPrioritiesPriorityQueue<IndexedWord>();
			ICollection<double> seenIndices = new HashSet<double>();
			IQueue<IndexedWord> fringe = new LinkedList<IndexedWord>();
			IndexedWord root = originalRoot;
			fringe.Add(root);
			bool isCopula = false;
			IndexedWord primaryCase = null;
			foreach (SemanticGraphEdge edge in parse.OutgoingEdgeIterable(originalRoot))
			{
				string shortName = edge.GetRelation().GetShortName();
				if (shortName.Equals("cop") || shortName.Equals("auxpass"))
				{
					isCopula = true;
				}
				if (shortName.Equals("case"))
				{
					primaryCase = edge.GetDependent();
				}
			}
			while (!fringe.IsEmpty())
			{
				root = fringe.Poll();
				chunk.Add(root, -root.PseudoPosition());
				// Sanity check to prevent infinite loops
				if (seenIndices.Contains(root.PseudoPosition()))
				{
					// TODO(gabor) Indicates a cycle in the tree!
					return Optional.Empty();
				}
				seenIndices.Add(root.PseudoPosition());
				// Check outgoing edges
				bool hasConj = false;
				bool hasCC = false;
				foreach (SemanticGraphEdge edge_1 in parse.GetOutEdgesSorted(root))
				{
					string shortName = edge_1.GetRelation().GetShortName();
					string name = edge_1.GetRelation().ToString();
					if (shortName.StartsWith("conj"))
					{
						hasConj = true;
					}
					if (shortName.Equals("cc"))
					{
						hasCC = true;
					}
					//noinspection StatementWithEmptyBody
					if (isCopula && (shortName.Equals("cop") || shortName.Contains("subj") || shortName.Equals("auxpass")))
					{
					}
					else
					{
						// noop; ignore nsubj, cop for extractions with copula
						if (edge_1.GetDependent() == primaryCase)
						{
						}
						else
						{
							// noop: ignore case edge
							if (ignoredArc.IsPresent() && (ignoredArc.Get().Equals(name) || (ignoredArc.Get().StartsWith("conj") && name.Equals("cc"))))
							{
							}
							else
							{
								// noop; ignore explicitly requested noop arc, or "CC" if the noop arc is a conj:*
								if (!validArcs.Contains(edge_1.GetRelation().GetShortName()) && !validArcs.Contains(edge_1.GetRelation().GetShortName().ReplaceAll(":.*", ":*")))
								{
									if (!allowExtraArcs)
									{
										return Optional.Empty();
									}
								}
								else
								{
									// noop: just some dangling arc
									fringe.Add(edge_1.GetDependent());
								}
							}
						}
					}
				}
				// Ensure that we don't have a conj without a cc, or vice versa
				if (bool.LogicalXor(hasConj, hasCC))
				{
					return Optional.Empty();
				}
			}
			return Optional.Of(chunk.ToSortedList());
		}

		/// <seealso cref="GetValidChunk(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord, System.Collections.Generic.ICollection{E}, Java.Util.Optional{T}, bool)"/>
		protected internal virtual Optional<IList<IndexedWord>> GetValidChunk(SemanticGraph parse, IndexedWord originalRoot, ICollection<string> validArcs, Optional<string> ignoredArc)
		{
			return GetValidChunk(parse, originalRoot, validArcs, ignoredArc, false);
		}

		/// <summary>Get the yield of a given subtree, if it is a valid subject.</summary>
		/// <remarks>
		/// Get the yield of a given subtree, if it is a valid subject.
		/// Otherwise, return
		/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
		/// }.
		/// </remarks>
		/// <param name="parse">The parse tree we are extracting a subtree from.</param>
		/// <param name="root">The root of the subtree.</param>
		/// <param name="noopArc">An optional edge type to ignore in gathering the chunk.</param>
		/// <returns>If this subtree is a valid entity, we return its yield. Otherwise, we return empty.</returns>
		protected internal virtual Optional<IList<IndexedWord>> GetValidSubjectChunk(SemanticGraph parse, IndexedWord root, Optional<string> noopArc)
		{
			return GetValidChunk(parse, root, ValidSubjectArcs, noopArc);
		}

		/// <summary>Get the yield of a given subtree, if it is a valid object.</summary>
		/// <remarks>
		/// Get the yield of a given subtree, if it is a valid object.
		/// Otherwise, return
		/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
		/// }.
		/// </remarks>
		/// <param name="parse">The parse tree we are extracting a subtree from.</param>
		/// <param name="root">The root of the subtree.</param>
		/// <param name="noopArc">An optional edge type to ignore in gathering the chunk.</param>
		/// <returns>If this subtree is a valid entity, we return its yield. Otherwise, we return empty.</returns>
		protected internal virtual Optional<IList<IndexedWord>> GetValidObjectChunk(SemanticGraph parse, IndexedWord root, Optional<string> noopArc)
		{
			return GetValidChunk(parse, root, ValidObjectArcs, noopArc);
		}

		/// <summary>Get the yield of a given subtree, if it is a adverb chunk.</summary>
		/// <remarks>
		/// Get the yield of a given subtree, if it is a adverb chunk.
		/// Otherwise, return
		/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
		/// }.
		/// </remarks>
		/// <param name="parse">The parse tree we are extracting a subtree from.</param>
		/// <param name="root">The root of the subtree.</param>
		/// <param name="noopArc">An optional edge type to ignore in gathering the chunk.</param>
		/// <returns>If this subtree is a valid adverb, we return its yield. Otherwise, we return empty.</returns>
		protected internal virtual Optional<IList<IndexedWord>> GetValidAdverbChunk(SemanticGraph parse, IndexedWord root, Optional<string> noopArc)
		{
			return GetValidChunk(parse, root, ValidAdverbArcs, noopArc);
		}

		/// <summary>
		/// <p>
		/// Try to segment this sentence as a relation triple.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Try to segment this sentence as a relation triple.
		/// This sentence must already match one of a few strict patterns for a valid OpenIE extraction.
		/// If it does not, then no relation triple is created.
		/// That is, this is <b>not</b> a relation extractor; it is just a utility to segment what is already a
		/// (subject, relation, object) triple into these three parts.
		/// </p>
		/// <p>
		/// This method will only run the verb-centric patterns
		/// </p>
		/// </remarks>
		/// <param name="parse">The sentence to process, as a dependency tree.</param>
		/// <param name="confidence">An optional confidence to pass on to the relation triple.</param>
		/// <param name="consumeAll">if true, force the entire parse to be consumed by the pattern.</param>
		/// <returns>A relation triple, if this sentence matches one of the patterns of a valid relation triple.</returns>
		private Optional<RelationTriple> SegmentVerb(SemanticGraph parse, Optional<double> confidence, bool consumeAll)
		{
			// Run pattern loop
			foreach (SemgrexPattern pattern in VerbPatterns)
			{
				// For every candidate pattern...
				SemgrexMatcher m = pattern.Matcher(parse);
				if (m.Matches())
				{
					// ... see if it matches the sentence
					if ("nmod:poss".Equals(m.GetRelnString("prepEdge")))
					{
						goto PATTERN_LOOP_continue;
					}
					// nmod:poss is not a preposition!
					int numKnownDependents = 2;
					// subject and object, at minimum
					bool istmod = false;
					// this is a tmod relation
					// Object
					IndexedWord @object = m.GetNode("appos");
					if (@object == null)
					{
						@object = m.GetNode("object");
					}
					if (@object != null && @object.Tag() != null && @object.Tag().StartsWith("W"))
					{
						continue;
					}
					// don't extract WH arguments
					System.Diagnostics.Debug.Assert(@object != null);
					// Verb
					IPriorityQueue<IndexedWord> verbChunk = new FixedPrioritiesPriorityQueue<IndexedWord>();
					IndexedWord verb = m.GetNode("verb");
					IList<IndexedWord> adverbs = new List<IndexedWord>();
					Optional<string> subjNoopArc = Optional.Empty();
					Optional<string> objNoopArc = Optional.Empty();
					System.Diagnostics.Debug.Assert(verb != null);
					// Case: a standard extraction with a main verb
					IndexedWord relObj = m.GetNode("relObj");
					foreach (SemanticGraphEdge edge in parse.OutgoingEdgeIterable(verb))
					{
						if ("advmod".Equals(edge.GetRelation().ToString()) || "amod".Equals(edge.GetRelation().ToString()) || "compound:*".Equals(edge.GetRelation().ToString().ReplaceAll(":.*", ":*")))
						{
							// Add adverb modifiers
							string tag = edge.GetDependent().BackingLabel().Tag();
							if (tag == null || (!tag.StartsWith("W") && !Sharpen.Runtime.EqualsIgnoreCase(edge.GetDependent().BackingLabel().Word(), "then")))
							{
								// prohibit advmods like "where"
								adverbs.Add(edge.GetDependent());
							}
						}
						else
						{
							if (edge.GetDependent().Equals(relObj))
							{
								// Add additional object to the relation
								Optional<IList<IndexedWord>> relObjSpan = GetValidChunk(parse, relObj, Java.Util.Collections.Singleton("compound"), Optional.Empty());
								if (!relObjSpan.IsPresent())
								{
									goto PATTERN_LOOP_continue;
								}
								else
								{
									foreach (IndexedWord token in relObjSpan.Get())
									{
										verbChunk.Add(token, -token.PseudoPosition());
									}
									numKnownDependents += 1;
								}
							}
						}
					}
					verbChunk.Add(verb, -verb.PseudoPosition());
					// Prepositions
					IndexedWord prep = m.GetNode("prep");
					string prepEdge = m.GetRelnString("prepEdge");
					if (prep != null)
					{
						// (get the preposition chunk)
						Optional<IList<IndexedWord>> chunk = GetValidChunk(parse, prep, Java.Util.Collections.Singleton("mwe"), Optional.Empty(), true);
						// (continue if no chunk found)
						if (!chunk.IsPresent())
						{
							goto PATTERN_LOOP_continue;
						}
						// Probably something like a conj w/o a cc
						// (add the preposition)
						foreach (IndexedWord word in chunk.Get())
						{
							verbChunk.Add(word, int.MinValue / 2 - word.PseudoPosition());
						}
					}
					// (handle special prepositions)
					if (prepEdge != null)
					{
						string prepStringFromEdge = Sharpen.Runtime.Substring(prepEdge, prepEdge.IndexOf(":") + 1).Replace("_", " ");
						if ("tmod".Equals(prepStringFromEdge))
						{
							istmod = true;
						}
					}
					// Auxilliary "be"
					IndexedWord be = m.GetNode("be");
					if (be != null)
					{
						verbChunk.Add(be, -be.PseudoPosition());
						numKnownDependents += 1;
					}
					// (adverbs have to be well-formed)
					if (!adverbs.IsEmpty())
					{
						ICollection<IndexedWord> adverbialModifiers = new HashSet<IndexedWord>();
						foreach (IndexedWord adv in adverbs)
						{
							Optional<IList<IndexedWord>> adverbChunk = GetValidAdverbChunk(parse, adv, Optional.Empty());
							if (adverbChunk.IsPresent())
							{
								Sharpen.Collections.AddAll(adverbialModifiers, adverbChunk.Get().Stream().Collect(Collectors.ToList()));
							}
							else
							{
								goto PATTERN_LOOP_continue;
							}
							// Invalid adverbial phrase
							numKnownDependents += 1;
						}
						foreach (IndexedWord adverbToken in adverbialModifiers)
						{
							verbChunk.Add(adverbToken, -adverbToken.PseudoPosition());
						}
					}
					// (check for additional edges)
					if (consumeAll && parse.OutDegree(verb) > numKnownDependents)
					{
						//noinspection UnnecessaryLabelOnContinueStatement
						goto PATTERN_LOOP_continue;
					}
					// Too many outgoing edges; we didn't consume them all.
					IList<IndexedWord> relation = verbChunk.ToSortedList();
					int appendI = 0;
					IndexedWord relAppend = m.GetNode("relappend" + appendI);
					while (relAppend != null)
					{
						relation.Add(relAppend);
						appendI += 1;
						relAppend = m.GetNode("relappend" + appendI);
					}
					// Last chance to register ignored edges
					if (!subjNoopArc.IsPresent())
					{
						subjNoopArc = Optional.OfNullable(m.GetRelnString("subjIgnored"));
						if (!subjNoopArc.IsPresent())
						{
							subjNoopArc = Optional.OfNullable(m.GetRelnString("prepEdge"));
						}
					}
					// For some strange "there are" cases
					if (!objNoopArc.IsPresent())
					{
						objNoopArc = Optional.OfNullable(m.GetRelnString("objIgnored"));
					}
					// Find the subject
					// By default, this is just the subject node; but, occasionally we want to follow a
					// csubj clause to find the real subject.
					IndexedWord subject = m.GetNode("subject");
					if (subject != null && subject.Tag() != null && subject.Tag().StartsWith("W"))
					{
						continue;
					}
					// don't extract WH subjects
					// Subject+Object
					Optional<IList<IndexedWord>> subjectSpan = GetValidSubjectChunk(parse, subject, subjNoopArc);
					Optional<IList<IndexedWord>> objectSpan = GetValidObjectChunk(parse, @object, objNoopArc);
					// Create relation
					if (subjectSpan.IsPresent() && objectSpan.IsPresent() && CollectionUtils.Intersection(new HashSet<IndexedWord>(subjectSpan.Get()), new HashSet<IndexedWord>(objectSpan.Get())).IsEmpty())
					{
						// ... and has a valid subject+object
						// Success! Found a valid extraction.
						RelationTriple.WithTree extraction = new RelationTriple.WithTree(subjectSpan.Get().Stream().Map(null).Collect(Collectors.ToList()), relation.Stream().Map(null).Collect(Collectors.ToList()), objectSpan.Get().Stream().Map(null).Collect(Collectors
							.ToList()), parse, confidence.OrElse(1.0));
						extraction.Istmod(istmod);
						return Optional.Of(extraction);
					}
				}
			}
PATTERN_LOOP_break: ;
			// Failed to match any pattern; return failure
			return Optional.Empty();
		}

		/// <summary>
		/// Same as
		/// <see cref="SegmentVerb(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Java.Util.Optional{T}, bool)"/>
		/// , but with ACL clauses.
		/// This is a bit out of the ordinary, logic-wise, so it sits in its own function.
		/// </summary>
		private Optional<RelationTriple> SegmentACL(SemanticGraph parse, Optional<double> confidence, bool consumeAll)
		{
			IndexedWord subject = parse.GetFirstRoot();
			Optional<IList<IndexedWord>> subjectSpan = GetValidSubjectChunk(parse, subject, Optional.Of("acl"));
			if (subjectSpan.IsPresent())
			{
				// found a valid subject
				foreach (SemanticGraphEdge edgeFromSubj in parse.OutgoingEdgeIterable(subject))
				{
					if ("acl".Equals(edgeFromSubj.GetRelation().ToString()))
					{
						// found a valid relation
						IndexedWord relation = edgeFromSubj.GetDependent();
						IList<IndexedWord> relationSpan = new List<IndexedWord>();
						relationSpan.Add(relation);
						IList<IndexedWord> objectSpan = new List<IndexedWord>();
						IList<IndexedWord> ppSpan = new List<IndexedWord>();
						Optional<IndexedWord> pp = Optional.Empty();
						// Get other arguments
						foreach (SemanticGraphEdge edgeFromRel in parse.OutgoingEdgeIterable(relation))
						{
							string rel = edgeFromRel.GetRelation().ToString();
							// Collect adverbs
							if ("advmod".Equals(rel))
							{
								Optional<IList<IndexedWord>> advSpan = GetValidAdverbChunk(parse, edgeFromRel.GetDependent(), Optional.Empty());
								if (!advSpan.IsPresent())
								{
									return Optional.Empty();
								}
								// bad adverb span!
								Sharpen.Collections.AddAll(relationSpan, advSpan.Get());
							}
							else
							{
								// Collect object
								if (rel.EndsWith("obj"))
								{
									if (!objectSpan.IsEmpty())
									{
										return Optional.Empty();
									}
									// duplicate objects!
									Optional<IList<IndexedWord>> maybeObjSpan = GetValidObjectChunk(parse, edgeFromRel.GetDependent(), Optional.Empty());
									if (!maybeObjSpan.IsPresent())
									{
										return Optional.Empty();
									}
									// bad object span!
									Sharpen.Collections.AddAll(objectSpan, maybeObjSpan.Get());
								}
								else
								{
									// Collect pp
									if (rel.StartsWith("nmod:"))
									{
										if (!ppSpan.IsEmpty())
										{
											return Optional.Empty();
										}
										// duplicate objects!
										Optional<IList<IndexedWord>> maybePPSpan = GetValidObjectChunk(parse, edgeFromRel.GetDependent(), Optional.Of("case"));
										if (!maybePPSpan.IsPresent())
										{
											return Optional.Empty();
										}
										// bad object span!
										Sharpen.Collections.AddAll(ppSpan, maybePPSpan.Get());
										// Add the actual preposition, if we can find it
										foreach (SemanticGraphEdge edge in parse.OutgoingEdgeIterable(edgeFromRel.GetDependent()))
										{
											if ("case".Equals(edge.GetRelation().ToString()))
											{
												pp = Optional.Of(edge.GetDependent());
											}
										}
									}
									else
									{
										if (consumeAll)
										{
											return Optional.Empty();
										}
									}
								}
							}
						}
						// bad edge out of the relation
						// Construct a triple
						// (canonicalize the triple to be subject; relation; object, folding in the PP)
						if (!ppSpan.IsEmpty() && !objectSpan.IsEmpty())
						{
							Sharpen.Collections.AddAll(relationSpan, objectSpan);
							objectSpan = ppSpan;
						}
						else
						{
							if (!ppSpan.IsEmpty())
							{
								objectSpan = ppSpan;
							}
						}
						// (last error checks -- shouldn't ever fire)
						if (!subjectSpan.IsPresent() || subjectSpan.Get().IsEmpty() || relationSpan.IsEmpty() || objectSpan.IsEmpty())
						{
							return Optional.Empty();
						}
						// (sort the relation span)
						relationSpan.Sort(null);
						// (add in the PP node, if it exists)
						if (pp.IsPresent())
						{
							relationSpan.Add(pp.Get());
						}
						// (success!)
						RelationTriple.WithTree extraction = new RelationTriple.WithTree(subjectSpan.Get().Stream().Map(null).Collect(Collectors.ToList()), relationSpan.Stream().Map(null).Collect(Collectors.ToList()), objectSpan.Stream().Map(null).Collect(Collectors
							.ToList()), parse, confidence.OrElse(1.0));
						return Optional.Of(extraction);
					}
				}
			}
			// Nothing found; return
			return Optional.Empty();
		}

		/// <summary>
		/// <p>
		/// Try to segment this sentence as a relation triple.
		/// </summary>
		/// <remarks>
		/// <p>
		/// Try to segment this sentence as a relation triple.
		/// This sentence must already match one of a few strict patterns for a valid OpenIE extraction.
		/// If it does not, then no relation triple is created.
		/// That is, this is <b>not</b> a relation extractor; it is just a utility to segment what is already a
		/// (subject, relation, object) triple into these three parts.
		/// </p>
		/// <p>
		/// This method will attempt to use both the verb-centric patterns and the ACL-centric patterns.
		/// </p>
		/// </remarks>
		/// <param name="parse">The sentence to process, as a dependency tree.</param>
		/// <param name="confidence">An optional confidence to pass on to the relation triple.</param>
		/// <param name="consumeAll">if true, force the entire parse to be consumed by the pattern.</param>
		/// <returns>A relation triple, if this sentence matches one of the patterns of a valid relation triple.</returns>
		public virtual Optional<RelationTriple> Segment(SemanticGraph parse, Optional<double> confidence, bool consumeAll)
		{
			// Copy and clean the tree
			parse = new SemanticGraph(parse);
			// Special case "there is <something>". Arguably this is a job for the clause splitter, but the <something> is
			// sometimes not _really_ its own clause
			IndexedWord root = parse.GetFirstRoot();
			if ((root.Lemma() != null && Sharpen.Runtime.EqualsIgnoreCase(root.Lemma(), "be")) || (root.Lemma() == null && (Sharpen.Runtime.EqualsIgnoreCase("is", root.Word()) || Sharpen.Runtime.EqualsIgnoreCase("are", root.Word()) || Sharpen.Runtime.EqualsIgnoreCase
				("were", root.Word()) || Sharpen.Runtime.EqualsIgnoreCase("be", root.Word()))))
			{
				// Check for the "there is" construction
				bool foundThere = false;
				bool tooMayArcs = false;
				// an indicator for there being too much nonsense hanging off of the root
				Optional<SemanticGraphEdge> newRoot = Optional.Empty();
				foreach (SemanticGraphEdge edge in parse.OutgoingEdgeIterable(root))
				{
					if (edge.GetRelation().ToString().Equals("expl") && Sharpen.Runtime.EqualsIgnoreCase(edge.GetDependent().Word(), "there"))
					{
						foundThere = true;
					}
					else
					{
						if (edge.GetRelation().ToString().Equals("nsubj"))
						{
							newRoot = Optional.Of(edge);
						}
						else
						{
							tooMayArcs = true;
						}
					}
				}
				// Split off "there is")
				if (foundThere && newRoot.IsPresent() && !tooMayArcs)
				{
					ClauseSplitterSearchProblem.SplitToChildOfEdge(parse, newRoot.Get());
				}
			}
			// Run the patterns
			Optional<RelationTriple> extraction = SegmentVerb(parse, confidence, consumeAll);
			if (!extraction.IsPresent())
			{
				extraction = SegmentACL(parse, confidence, consumeAll);
			}
			//
			// Remove downward polarity extractions
			//
			if (extraction.IsPresent())
			{
				bool shouldRemove = true;
				foreach (CoreLabel token in extraction.Get())
				{
					if (token.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation)) == null || !token.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation)).IsDownwards())
					{
						shouldRemove = false;
					}
				}
				if (shouldRemove)
				{
					return Optional.Empty();
				}
			}
			// Return
			return extraction;
		}

		/// <summary>Segment the given parse tree, forcing all nodes to be consumed.</summary>
		/// <seealso cref="Segment(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Java.Util.Optional{T})"/>
		public virtual Optional<RelationTriple> Segment(SemanticGraph parse, Optional<double> confidence)
		{
			return Segment(parse, confidence, true);
		}
	}
}

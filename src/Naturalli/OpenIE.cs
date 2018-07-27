using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;







namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>An OpenIE system based on valid Natural Logic deletions of a sentence.</summary>
	/// <remarks>
	/// An OpenIE system based on valid Natural Logic deletions of a sentence.
	/// The system is described in:
	/// "Leveraging Linguistic Structure For Open Domain Information Extraction." Gabor Angeli, Melvin Johnson Premkumar, Christopher Manning. ACL 2015.
	/// The paper can be found at <a href="http://nlp.stanford.edu/pubs/2015angeli-openie.pdf">http://nlp.stanford.edu/pubs/2015angeli-openie.pdf</a>.
	/// Documentation on the system can be found on
	/// <a href="https://nlp.stanford.edu/software/openie.html">the project homepage</a>,
	/// or the <a href="http://stanfordnlp.github.io/CoreNLP/openie.html">CoreNLP annotator documentation page</a>.
	/// The simplest invocation of the system would be something like:
	/// <c>java -mx1g -cp stanford-openie.jar:stanford-openie-models.jar edu.stanford.nlp.naturalli.OpenIE</c>
	/// Note that this class serves both as an entry point for the OpenIE system, but also as a CoreNLP annotator
	/// which can be plugged into the CoreNLP pipeline (or any other annotation pipeline).
	/// </remarks>
	/// <seealso cref="Annotate(Edu.Stanford.Nlp.Pipeline.Annotation)"/>
	/// <seealso cref="Main(string[])"/>
	/// <author>Gabor Angeli</author>
	public class OpenIE : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Naturalli.OpenIE));

		private enum OutputFormat
		{
			Reverb,
			Ollie,
			Default,
			QaSrl
		}

		/// <summary>A pattern for rewriting "NN_1 is a JJ NN_2" --&gt; NN_1 is JJ"</summary>
		private static readonly SemgrexPattern adjectivePattern = SemgrexPattern.Compile("{}=obj >nsubj {}=subj >cop {}=be >det {word:/an?/} >amod {}=adj ?>/prep_.*/=prep {}=pobj");

		private static OpenIE.OutputFormat Format = OpenIE.OutputFormat.Default;

		private static File Filelist = null;

		private static TextWriter Output = System.Console.Out;

		private string splitterModel = DefaultPaths.DefaultOpenieClauseSearcher;

		private bool noModel = false;

		private double splitterThreshold = 0.1;

		private bool splitterDisable = false;

		private int entailmentsPerSentence = 1000;

		private bool ignoreAffinity = false;

		private string affinityModels = DefaultPaths.DefaultNaturalliAffinities;

		private double affinityProbabilityCap = 1.0 / 3.0;

		private bool consumeAll = true;

		private bool allNominals = false;

		private bool resolveCoref = false;

		private bool stripEntailments = false;

		/// <summary>The natural logic weights loaded from the models file.</summary>
		/// <remarks>
		/// The natural logic weights loaded from the models file.
		/// This is primarily the prepositional attachment statistics.
		/// </remarks>
		private readonly NaturalLogicWeights weights;

		/// <summary>The clause splitter model, if one is to be used.</summary>
		/// <remarks>
		/// The clause splitter model, if one is to be used.
		/// This component splits a sentence into a set of entailed clauses, but does not yet
		/// maximally shorten them.
		/// This is the implementation of stage 1 of the OpenIE pipeline.
		/// </remarks>
		public readonly Optional<IClauseSplitter> clauseSplitter;

		/// <summary>The forward entailer model, running a search from clauses to maximally shortened clauses.</summary>
		/// <remarks>
		/// The forward entailer model, running a search from clauses to maximally shortened clauses.
		/// This is the implementation of stage 2 of the OpenIE pipeline.
		/// </remarks>
		public readonly ForwardEntailer forwardEntailer;

		/// <summary>
		/// The relation triple segmenter, which converts a maximally shortened clause into an OpenIE
		/// extraction triple.
		/// </summary>
		/// <remarks>
		/// The relation triple segmenter, which converts a maximally shortened clause into an OpenIE
		/// extraction triple.
		/// This is the implementation of stage 3 of the OpenIE pipeline.
		/// </remarks>
		public RelationTripleSegmenter segmenter;

		/// <summary>Create a new OpenIE system, with default properties</summary>
		public OpenIE()
			: this(new Properties())
		{
		}

		/// <summary>Create a ne OpenIE system, based on the given properties.</summary>
		/// <param name="props">The properties to parametrize the system with.</param>
		public OpenIE(Properties props)
		{
			//
			// TODO(gabor): handle things like "One example of chemical energy is that found in the food that we eat ."
			//
			//
			// Static Options (for running standalone)
			//
			//
			// Annotator Options (for running in the pipeline)
			//
			// Fill the properties
			ArgumentParser.FillOptions(this, props);
			Properties withoutOpenIEPrefix = new Properties();
			foreach (string key in props.StringPropertyNames())
			{
				withoutOpenIEPrefix.SetProperty(key.Replace("openie.", string.Empty), props.GetProperty(key));
			}
			ArgumentParser.FillOptions(this, withoutOpenIEPrefix);
			// Create the clause splitter
			try
			{
				if (splitterDisable)
				{
					clauseSplitter = Optional.Empty();
				}
				else
				{
					if (noModel)
					{
						log.Info("Not loading a splitter model");
						clauseSplitter = Optional.Of(null);
					}
					else
					{
						clauseSplitter = Optional.Of(IClauseSplitter.Load(splitterModel));
					}
				}
			}
			catch (IOException e)
			{
				//throw new RuntimeIOException("Could not load clause splitter model at " + splitterModel + ": " + e.getClass() + ": " + e.getMessage());
				throw new RuntimeIOException("Could not load clause splitter model at " + splitterModel, e);
			}
			// Create the forward entailer
			try
			{
				this.weights = ignoreAffinity ? new NaturalLogicWeights(affinityProbabilityCap) : new NaturalLogicWeights(affinityModels, affinityProbabilityCap);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("Could not load affinity model at " + affinityModels + ": " + e.Message);
			}
			forwardEntailer = new ForwardEntailer(entailmentsPerSentence, weights);
			// Create the relation segmenter
			segmenter = new RelationTripleSegmenter(allNominals);
		}

		/// <summary>Find the clauses in a sentence, where the sentence is expressed as a dependency tree.</summary>
		/// <param name="tree">The dependency tree representation of the sentence.</param>
		/// <param name="assumedTruth">
		/// The assumed truth of the sentence. This is almost always true, unless you are
		/// doing some more nuanced reasoning.
		/// </param>
		/// <returns>A set of clauses extracted from the sentence. This includes the original sentence.</returns>
		public virtual IList<SentenceFragment> ClausesInSentence(SemanticGraph tree, bool assumedTruth)
		{
			if (clauseSplitter.IsPresent())
			{
				return clauseSplitter.Get().Apply(tree, assumedTruth).TopClauses(splitterThreshold, 32);
			}
			else
			{
				return Java.Util.Collections.EmptyList();
			}
		}

		/// <summary>Find the clauses in a sentence.</summary>
		/// <remarks>
		/// Find the clauses in a sentence.
		/// This runs the clause splitting component of the OpenIE system only.
		/// </remarks>
		/// <seealso cref="ClausesInSentence(Edu.Stanford.Nlp.Semgraph.SemanticGraph, bool)"/>
		/// <param name="sentence">The raw sentence to extract clauses from.</param>
		/// <returns>A set of clauses extracted from the sentence. This includes the original sentence.</returns>
		public virtual IList<SentenceFragment> ClausesInSentence(ICoreMap sentence)
		{
			return ClausesInSentence(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)), true);
		}

		/// <summary>Returns all of the entailed shortened clauses (as per natural logic) from the given clause.</summary>
		/// <remarks>
		/// Returns all of the entailed shortened clauses (as per natural logic) from the given clause.
		/// This runs the forward entailment component of the OpenIE system only.
		/// It is usually chained together with the clause splitting component:
		/// <see cref="ClausesInSentence(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// .
		/// </remarks>
		/// <param name="clause">The premise clause, as a sentence fragment in itself.</param>
		/// <returns>A list of entailed clauses.</returns>
		public virtual IList<SentenceFragment> EntailmentsFromClause(SentenceFragment clause)
		{
			if (clause.parseTree.IsEmpty())
			{
				return Java.Util.Collections.EmptyList();
			}
			else
			{
				// Get the forward entailments
				IList<SentenceFragment> list = new List<SentenceFragment>();
				if (entailmentsPerSentence > 0)
				{
					Sharpen.Collections.AddAll(list, forwardEntailer.Apply(clause.parseTree, true).Search().Stream().Map(null).Collect(Collectors.ToList()));
				}
				list.Add(clause);
				// A special case for adjective entailments
				IList<SentenceFragment> adjFragments = new List<SentenceFragment>();
				SemgrexMatcher matcher = adjectivePattern.Matcher(clause.parseTree);
				while (matcher.Find())
				{
					// (get nodes)
					IndexedWord subj = matcher.GetNode("subj");
					IndexedWord be = matcher.GetNode("be");
					IndexedWord adj = matcher.GetNode("adj");
					IndexedWord obj = matcher.GetNode("obj");
					IndexedWord pobj = matcher.GetNode("pobj");
					string prep = matcher.GetRelnString("prep");
					// (if the adjective, or any earlier adjective, is privative, then all bets are off)
					foreach (SemanticGraphEdge edge in clause.parseTree.OutgoingEdgeIterable(obj))
					{
						if ("amod".Equals(edge.GetRelation().ToString()) && edge.GetDependent().Index() <= adj.Index() && Edu.Stanford.Nlp.Naturalli.Util.PrivativeAdjectives.Contains(edge.GetDependent().Word().ToLower()))
						{
							goto OUTER_continue;
						}
					}
					// (create the core tree)
					SemanticGraph tree = new SemanticGraph();
					tree.AddRoot(adj);
					tree.AddVertex(subj);
					tree.AddVertex(be);
					tree.AddEdge(adj, be, GrammaticalRelation.ValueOf(Language.English, "cop"), double.NegativeInfinity, false);
					tree.AddEdge(adj, subj, GrammaticalRelation.ValueOf(Language.English, "nsubj"), double.NegativeInfinity, false);
					// (add pp attachment, if it existed)
					if (pobj != null)
					{
						System.Diagnostics.Debug.Assert(prep != null);
						tree.AddEdge(adj, pobj, GrammaticalRelation.ValueOf(Language.English, prep), double.NegativeInfinity, false);
					}
					// (check for monotonicity)
					if (adj.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation)).IsUpwards() && be.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation)).IsUpwards())
					{
						// (add tree)
						adjFragments.Add(new SentenceFragment(tree, clause.assumedTruth, false));
					}
OUTER_continue: ;
				}
OUTER_break: ;
				Sharpen.Collections.AddAll(list, adjFragments);
				return list;
			}
		}

		/// <summary>
		/// Returns all the maximally shortened entailed fragments (as per natural logic)
		/// from the given collection of clauses.
		/// </summary>
		/// <param name="clauses">The clauses to shorten further.</param>
		/// <returns>A set of sentence fragments corresponding to the maximally shortened entailed clauses.</returns>
		public virtual ICollection<SentenceFragment> EntailmentsFromClauses(ICollection<SentenceFragment> clauses)
		{
			ICollection<SentenceFragment> entailments = new HashSet<SentenceFragment>();
			foreach (SentenceFragment clause in clauses)
			{
				Sharpen.Collections.AddAll(entailments, EntailmentsFromClause(clause));
			}
			return entailments;
		}

		/// <summary>Returns the possible relation triple in this sentence fragment.</summary>
		/// <seealso cref="RelationInFragment(SentenceFragment, Edu.Stanford.Nlp.Util.ICoreMap)"/>
		public virtual Optional<RelationTriple> RelationInFragment(SentenceFragment fragment)
		{
			return segmenter.Segment(fragment.parseTree, Optional.Of(fragment.score), consumeAll);
		}

		/// <summary>Returns the possible relation triple in this set of sentence fragments.</summary>
		/// <seealso cref="RelationsInFragments(System.Collections.Generic.ICollection{E}, Edu.Stanford.Nlp.Util.ICoreMap)"/>
		public virtual IList<RelationTriple> RelationsInFragments(ICollection<SentenceFragment> fragments)
		{
			return fragments.Stream().Map(null).Filter(null).Map(null).Collect(Collectors.ToList());
		}

		/// <summary>Returns the possible relation triple in this sentence fragment.</summary>
		/// <param name="fragment">The sentence fragment to try to extract relations from.</param>
		/// <param name="sentence">The containing sentence for the fragment.</param>
		/// <returns>
		/// A relation triple if we could find one; otherwise,
		/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
		/// .
		/// </returns>
		private Optional<RelationTriple> RelationInFragment(SentenceFragment fragment, ICoreMap sentence)
		{
			return segmenter.Segment(fragment.parseTree, Optional.Of(fragment.score), consumeAll);
		}

		/// <summary>Returns a list of OpenIE relations from the given set of sentence fragments.</summary>
		/// <param name="fragments">The sentence fragments to extract relations from.</param>
		/// <param name="sentence">The containing sentence that these fragments were extracted from.</param>
		/// <returns>A list of OpenIE triples, corresponding to all the triples that could be extracted from the given fragments.</returns>
		private IList<RelationTriple> RelationsInFragments(ICollection<SentenceFragment> fragments, ICoreMap sentence)
		{
			return fragments.Stream().Map(null).Filter(null).Map(null).Collect(Collectors.ToList());
		}

		/// <summary>Extract the relations in this clause.</summary>
		/// <seealso cref="EntailmentsFromClause(SentenceFragment)"/>
		/// <seealso cref="RelationsInFragments(System.Collections.Generic.ICollection{E})"/>
		public virtual IList<RelationTriple> RelationsInClause(SentenceFragment clause)
		{
			return RelationsInFragments(EntailmentsFromClause(clause));
		}

		/// <summary>Extract the relations in this sentence.</summary>
		/// <seealso cref="ClausesInSentence(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// <seealso cref="EntailmentsFromClause(SentenceFragment)"/>
		/// <seealso cref="RelationsInFragments(System.Collections.Generic.ICollection{E})"/>
		public virtual IList<RelationTriple> RelationsInSentence(ICoreMap sentence)
		{
			return RelationsInFragments(EntailmentsFromClauses(ClausesInSentence(sentence)));
		}

		/// <summary>Create a copy of the passed parse tree, canonicalizing pronominal nodes with their canonical mention.</summary>
		/// <remarks>
		/// Create a copy of the passed parse tree, canonicalizing pronominal nodes with their canonical mention.
		/// Canonical mentions are tied together with the <i>compound</i> dependency arc; otherwise, the structure of
		/// the tree remains unchanged.
		/// </remarks>
		/// <param name="parse">The original dependency parse of the sentence.</param>
		/// <param name="canonicalMentionMap">The map from tokens to their canonical mentions.</param>
		/// <returns>A <b>copy</b> of the passed parse tree, with pronouns replaces with their canonical mention.</returns>
		private static SemanticGraph CanonicalizeCoref(SemanticGraph parse, IDictionary<CoreLabel, IList<CoreLabel>> canonicalMentionMap)
		{
			parse = new SemanticGraph(parse);
			foreach (IndexedWord node in new HashSet<IndexedWord>(parse.VertexSet()))
			{
				// copy the vertex set to prevent ConcurrentModificationExceptions
				if (node.Tag() != null && node.Tag().StartsWith("PRP"))
				{
					IList<CoreLabel> canonicalMention = canonicalMentionMap[node.BackingLabel()];
					if (canonicalMention != null)
					{
						// Case: this node is a preposition with a valid antecedent.
						// 1. Save the attaching edges
						IList<SemanticGraphEdge> incomingEdges = parse.IncomingEdgeList(node);
						IList<SemanticGraphEdge> outgoingEdges = parse.OutgoingEdgeList(node);
						// 2. Remove the node
						parse.RemoveVertex(node);
						// 3. Add the new head word
						IndexedWord headWord = new IndexedWord(canonicalMention[canonicalMention.Count - 1]);
						headWord.SetPseudoPosition(node.PseudoPosition());
						parse.AddVertex(headWord);
						foreach (SemanticGraphEdge edge in incomingEdges)
						{
							parse.AddEdge(edge.GetGovernor(), headWord, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
						}
						foreach (SemanticGraphEdge edge_1 in outgoingEdges)
						{
							parse.AddEdge(headWord, edge_1.GetDependent(), edge_1.GetRelation(), edge_1.GetWeight(), edge_1.IsExtra());
						}
						// 4. Add other words
						double pseudoPosition = headWord.PseudoPosition() - 1e-3;
						for (int i = canonicalMention.Count - 2; i >= 0; --i)
						{
							// Create the node
							IndexedWord dependent = new IndexedWord(canonicalMention[i]);
							// Set its pseudo position appropriately
							dependent.SetPseudoPosition(pseudoPosition);
							pseudoPosition -= 1e-3;
							// Add the node to the graph
							parse.AddVertex(dependent);
							parse.AddEdge(headWord, dependent, UniversalEnglishGrammaticalRelations.CompoundModifier, 1.0, false);
						}
					}
				}
			}
			return parse;
		}

		/// <summary>Annotate a single sentence.</summary>
		/// <remarks>
		/// Annotate a single sentence.
		/// This annotator will, in particular, set the
		/// <see cref="EntailedSentencesAnnotation"/>
		/// and
		/// <see cref="RelationTriplesAnnotation"/>
		/// annotations.
		/// </remarks>
		public virtual void AnnotateSentence(ICoreMap sentence, IDictionary<CoreLabel, IList<CoreLabel>> canonicalMentionMap)
		{
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (tokens.Count < 2)
			{
				// Short sentence. Skip annotating it.
				sentence.Set(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation), Java.Util.Collections.EmptyList());
				if (!stripEntailments)
				{
					sentence.Set(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation), Java.Util.Collections.EmptySet());
				}
			}
			else
			{
				// Get the dependency tree
				SemanticGraph parse = sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation));
				if (parse == null)
				{
					parse = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
				}
				if (parse == null)
				{
					throw new InvalidOperationException("Cannot run OpenIE without a parse tree!");
				}
				// Clean the tree
				parse = new SemanticGraph(parse);
				Edu.Stanford.Nlp.Naturalli.Util.CleanTree(parse);
				// Resolve Coreference
				SemanticGraph canonicalizedParse = parse;
				if (resolveCoref && !canonicalMentionMap.IsEmpty())
				{
					canonicalizedParse = CanonicalizeCoref(parse, canonicalMentionMap);
				}
				// Run OpenIE
				// (clauses)
				IList<SentenceFragment> clauses = ClausesInSentence(canonicalizedParse, true);
				// note: uses coref-canonicalized parse
				// (entailment)
				ICollection<SentenceFragment> fragments = EntailmentsFromClauses(clauses);
				// (segment)
				IList<RelationTriple> extractions = segmenter.Extract(parse, tokens);
				// note: uses non-coref-canonicalized parse!
				Sharpen.Collections.AddAll(extractions, RelationsInFragments(fragments, sentence));
				// Set the annotations
				sentence.Set(typeof(NaturalLogicAnnotations.EntailedClausesAnnotation), new HashSet<SentenceFragment>(clauses));
				sentence.Set(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation), fragments);
				sentence.Set(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation), new List<RelationTriple>(new HashSet<RelationTriple>(extractions)));
				// uniq the extractions
				if (stripEntailments)
				{
					sentence.Remove(typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation));
				}
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// This annotator will, in particular, set the
		/// <see cref="EntailedSentencesAnnotation"/>
		/// and
		/// <see cref="RelationTriplesAnnotation"/>
		/// annotations.
		/// </summary>
		public virtual void Annotate(Annotation annotation)
		{
			// Accumulate Coref data
			IDictionary<int, CorefChain> corefChains;
			IDictionary<CoreLabel, IList<CoreLabel>> canonicalMentionMap = new IdentityHashMap<CoreLabel, IList<CoreLabel>>();
			if (resolveCoref && (corefChains = annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation))) != null)
			{
				foreach (CorefChain chain in corefChains.Values)
				{
					// Make sure it's a real chain and not a singleton
					if (chain.GetMentionsInTextualOrder().Count < 2)
					{
						continue;
					}
					// Metadata
					IList<CoreLabel> canonicalMention = null;
					double canonicalMentionScore = double.NegativeInfinity;
					ICollection<CoreLabel> tokensToMark = new HashSet<CoreLabel>();
					IList<CorefChain.CorefMention> mentions = chain.GetMentionsInTextualOrder();
					// Iterate over mentions
					for (int i = 0; i < mentions.Count; ++i)
					{
						// Get some data on this mention
						Pair<IList<CoreLabel>, double> info = GrokCorefMention(annotation, mentions[i]);
						// Figure out if it should be the canonical mention
						double score = info.second + ((double)i) / ((double)mentions.Count) + (mentions[i] == chain.GetRepresentativeMention() ? 1.0 : 0.0);
						if (canonicalMention == null || score > canonicalMentionScore)
						{
							canonicalMention = info.first;
							canonicalMentionScore = score;
						}
						// Register the participating tokens
						if (info.first.Count == 1)
						{
							// Only mark single-node tokens!
							Sharpen.Collections.AddAll(tokensToMark, info.first);
						}
					}
					// Mark the tokens as coreferent
					System.Diagnostics.Debug.Assert(canonicalMention != null);
					foreach (CoreLabel token in tokensToMark)
					{
						IList<CoreLabel> existingMention = canonicalMentionMap[token];
						if (existingMention == null || existingMention.IsEmpty() || "O".Equals(existingMention[0].Ner()))
						{
							// Don't clobber existing good mentions
							canonicalMentionMap[token] = canonicalMention;
						}
					}
				}
			}
			// Annotate each sentence
			annotation.Get(typeof(CoreAnnotations.SentencesAnnotation)).ForEach(null);
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation), typeof(NaturalLogicAnnotations.EntailedSentencesAnnotation))));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public virtual ICollection<Type> Requires()
		{
			ICollection<Type> requirements = new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation
				), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.LemmaAnnotation), typeof(NaturalLogicAnnotations.PolarityAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)));
			//CoreAnnotations.OriginalTextAnnotation.class
			if (resolveCoref)
			{
				requirements.Add(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			}
			return Java.Util.Collections.UnmodifiableSet(requirements);
		}

		/// <summary>A utility to get useful information out of a CorefMention.</summary>
		/// <remarks>
		/// A utility to get useful information out of a CorefMention. In particular, it returns the CoreLabels which are
		/// associated with this mention, and it returns a score for how much we think this mention should be the canonical
		/// mention.
		/// </remarks>
		/// <param name="doc">The document this mention is referenced into.</param>
		/// <param name="mention">The mention itself.</param>
		/// <returns>A pair of the tokens in the mention, and a score for how much we like this mention as the canonical mention.</returns>
		private static Pair<IList<CoreLabel>, double> GrokCorefMention(Annotation doc, CorefChain.CorefMention mention)
		{
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.SentencesAnnotation))[mention.sentNum - 1].Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<CoreLabel> mentionAsTokens = tokens.SubList(mention.startIndex - 1, mention.endIndex - 1);
			// Try to assess this mention's NER type
			ICounter<string> nerVotes = new ClassicCounter<string>();
			mentionAsTokens.Stream().Filter(null).ForEach(null);
			string ner = Counters.Argmax(nerVotes, null);
			double nerCount = nerVotes.GetCount(ner);
			double nerScore = nerCount * nerCount / ((double)mentionAsTokens.Count);
			// Return
			return Pair.MakePair(mentionAsTokens, nerScore);
		}

		/// <summary>
		/// Prints an OpenIE triple to a String, according to the output format requested in
		/// the annotator.
		/// </summary>
		/// <param name="extraction">The triple to write.</param>
		/// <param name="docid">The document ID (for the ReVerb format)</param>
		/// <param name="sentence">The sentence the triple was extracted from (for the ReVerb format)</param>
		/// <returns>A String representation of the triple.</returns>
		public static string TripleToString(RelationTriple extraction, string docid, ICoreMap sentence)
		{
			switch (Format)
			{
				case OpenIE.OutputFormat.Reverb:
				{
					return extraction.ToReverbString(docid, sentence);
				}

				case OpenIE.OutputFormat.Ollie:
				{
					return extraction.ConfidenceGloss() + ": (" + extraction.SubjectGloss() + "; " + extraction.RelationGloss() + "; " + extraction.ObjectGloss() + ')';
				}

				case OpenIE.OutputFormat.Default:
				{
					return extraction.ToString();
				}

				case OpenIE.OutputFormat.QaSrl:
				{
					return extraction.ToQaSrlString(sentence);
				}

				default:
				{
					throw new InvalidOperationException("Format is not implemented: " + Format);
				}
			}
		}

		/// <summary>Process a single file or line of standard in.</summary>
		/// <param name="pipeline">The annotation pipeline to run the lines of the input through.</param>
		/// <param name="docid">The docid of the document we are extracting.</param>
		/// <param name="document">the document to annotate.</param>
		private static void ProcessDocument(AnnotationPipeline pipeline, string docid, string document)
		{
			// Error checks
			if (document.Trim().IsEmpty())
			{
				return;
			}
			// Annotate the document
			Annotation ann = new Annotation(document);
			pipeline.Annotate(ann);
			// Get the extractions
			bool empty = true;
			lock (Output)
			{
				foreach (ICoreMap sentence in ann.Get(typeof(CoreAnnotations.SentencesAnnotation)))
				{
					foreach (RelationTriple extraction in sentence.Get(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation)))
					{
						// Print the extractions
						Output.WriteLine(TripleToString(extraction, docid, sentence));
						empty = false;
					}
				}
			}
			if (empty)
			{
				log.Info("No extractions in: " + ("stdin".Equals(docid) ? document : docid));
			}
		}

		/// <summary>An entry method for annotating standard in with OpenIE extractions.</summary>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			// Parse the arguments
			Properties props = StringUtils.ArgsToProperties(args, new _Dictionary_667());
			ArgumentParser.FillOptions(new Type[] { typeof(Edu.Stanford.Nlp.Naturalli.OpenIE), typeof(ArgumentParser) }, props);
			AtomicInteger exceptionCount = new AtomicInteger(0);
			IExecutorService exec = Executors.NewFixedThreadPool(ArgumentParser.threads);
			// Parse the files to process
			string[] filesToProcess;
			if (Filelist != null)
			{
				filesToProcess = IOUtils.LinesFromFile(Filelist.GetPath()).Stream().Map(null).Map(null).Map(null).ToArray(null);
			}
			else
			{
				if (!string.Empty.Equals(props.GetProperty(string.Empty, string.Empty)))
				{
					filesToProcess = props.GetProperty(string.Empty, string.Empty).Split("\\s+");
				}
				else
				{
					filesToProcess = new string[0];
				}
			}
			// Tweak the arguments
			if (string.Empty.Equals(props.GetProperty("annotators", string.Empty)))
			{
				if (!Sharpen.Runtime.EqualsIgnoreCase("false", props.GetProperty("resolve_coref", props.GetProperty("openie.resolve_coref", "false"))))
				{
					props.SetProperty("coref.md.type", "dep");
					// so we don't need the `parse` annotator
					props.SetProperty("coref.mode", "statistical");
					// explicitly ask for scoref
					props.SetProperty("annotators", "tokenize,ssplit,pos,lemma,depparse,ner,mention,coref,natlog,openie");
				}
				else
				{
					props.SetProperty("annotators", "tokenize,ssplit,pos,lemma,depparse,natlog,openie");
				}
			}
			if (string.Empty.Equals(props.GetProperty("depparse.extradependencies", string.Empty)))
			{
				props.SetProperty("depparse.extradependencies", "ref_only_uncollapsed");
			}
			if (string.Empty.Equals(props.GetProperty("parse.extradependencies", string.Empty)))
			{
				props.SetProperty("parse.extradependencies", "ref_only_uncollapsed");
			}
			if (string.Empty.Equals(props.GetProperty("tokenize.class", string.Empty)))
			{
				props.SetProperty("tokenize.class", "PTBTokenizer");
			}
			if (string.Empty.Equals(props.GetProperty("tokenize.language", string.Empty)))
			{
				props.SetProperty("tokenize.language", "en");
			}
			// Tweak properties for console mode.
			// In particular, in this mode we can assume every line of standard in is a new sentence.
			if (filesToProcess.Length == 0 && string.Empty.Equals(props.GetProperty("ssplit.isOneSentence", string.Empty)))
			{
				props.SetProperty("ssplit.isOneSentence", "true");
			}
			// Some error checks on the arguments
			if (!props.GetProperty("annotators").ToLower().Contains("openie"))
			{
				log.Error("If you specify custom annotators, you must at least include 'openie'");
				System.Environment.Exit(1);
			}
			// Copy properties that are missing the 'openie' prefix
			new HashSet<object>(props.Keys).Stream().Filter(null).ForEach(null);
			// Create the pipeline
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			// Run OpenIE
			if (filesToProcess.Length == 0)
			{
				// Running from stdin; one document per line.
				log.Info("Processing from stdin. Enter one sentence per line.");
				Scanner scanner = new Scanner(Runtime.@in);
				string line;
				try
				{
					line = scanner.NextLine();
				}
				catch (NoSuchElementException)
				{
					log.Info("No lines found on standard in");
					return;
				}
				while (line != null)
				{
					ProcessDocument(pipeline, "stdin", line);
					try
					{
						line = scanner.NextLine();
					}
					catch (NoSuchElementException)
					{
						return;
					}
				}
			}
			else
			{
				// Running from file parameters.
				// Make sure we can read all the files in the queue.
				// This will prevent a nasty surprise 10 hours into a running job...
				foreach (string file in filesToProcess)
				{
					if (!new File(file).Exists() || !new File(file).CanRead())
					{
						log.Error("Cannot read file (or file does not exist: '" + file + '\'');
					}
				}
				// Actually process the files.
				foreach (string file_1 in filesToProcess)
				{
					log.Info("Processing file: " + file_1);
					if (ArgumentParser.threads > 1)
					{
						// Multi-threaded: submit a job to run
						string fileToSubmit = file_1;
						exec.Submit(null);
					}
					else
					{
						// Single-threaded: just run the job
						ProcessDocument(pipeline, file_1, IOUtils.SlurpFile(new File(file_1)));
					}
				}
			}
			// Exit
			exec.Shutdown();
			log.Info("All files have been queued; awaiting termination...");
			exec.AwaitTermination(long.MaxValue, TimeUnit.Seconds);
			log.Info("DONE processing files. " + exceptionCount.Get() + " exceptions encountered.");
			System.Environment.Exit(exceptionCount.Get());
		}

		private sealed class _Dictionary_667 : Dictionary<string, int>
		{
			public _Dictionary_667()
			{
				{
					this["openie.resolve_coref"] = 0;
					this["resolve_coref"] = 0;
					this["openie.splitter.nomodel"] = 0;
					this["splitter.nomodel"] = 0;
					this["openie.splitter.disable"] = 0;
					this["splitter.disable"] = 0;
					this["openie.ignore_affinity"] = 0;
					this["splitter.ignore_affinity"] = 0;
					this["openie.triple.strict"] = 0;
					this["splitter.triple.strict"] = 0;
					this["openie.triple.all_nominals"] = 0;
					this["splitter.triple.all_nominals"] = 0;
				}
			}
		}
	}
}

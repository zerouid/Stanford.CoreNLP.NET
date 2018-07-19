using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.International;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A search problem for finding clauses in a sentence.</summary>
	/// <remarks>
	/// A search problem for finding clauses in a sentence.
	/// <p>
	/// For usage at test time, load a model from
	/// <see cref="IClauseSplitter.Load(string)"/>
	/// , and then take the top clauses of a given tree
	/// with
	/// <see cref="TopClauses(double, int)"/>
	/// , yielding a list of
	/// <see cref="SentenceFragment"/>
	/// s.
	/// <p>
	/// <pre>
	/// <c>
	/// ClauseSearcher searcher = ClauseSearcher.factory("/model/path/");
	/// List&lt;SentenceFragment&gt; sentences = searcher.topClauses(threshold);
	/// </c>
	/// </pre>
	/// <p>
	/// For training, see
	/// <see cref="IClauseSplitter.Train(Java.Util.Stream.IStream{T}, Java.IO.File, Java.IO.File)"/>
	/// .
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class ClauseSplitterSearchProblem
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Naturalli.ClauseSplitterSearchProblem));

		private sealed class _List_57 : List<string>
		{
			public _List_57()
			{
				{
					this.Add("simple");
				}
			}
		}

		private sealed class _List_60 : List<string>
		{
			public _List_60()
			{
				{
					this.Add("simple");
				}
			}
		}

		private sealed class _List_63 : List<string>
		{
			public _List_63()
			{
				{
					this.Add("clone_dobj");
					this.Add("clone_nsubj");
					this.Add("simple");
				}
			}
		}

		private sealed class _List_68 : List<string>
		{
			public _List_68()
			{
				{
					this.Add("clone_nsubj");
					this.Add("simple");
				}
			}
		}

		private sealed class _List_72 : List<string>
		{
			public _List_72()
			{
				{
					this.Add("clone_dobj");
					this.Add("simple");
				}
			}
		}

		private sealed class _List_76 : List<string>
		{
			public _List_76()
			{
				{
					this.Add("clone_nsubj");
					this.Add("simple");
				}
			}
		}

		private sealed class _List_80 : List<string>
		{
			public _List_80()
			{
				{
					this.Add("clone_nsubj");
					this.Add("simple");
				}
			}
		}

		private sealed class _List_84 : List<string>
		{
			public _List_84()
			{
				{
					this.Add("clone_nsubj");
					this.Add("clone_dobj");
					this.Add("simple");
				}
			}
		}

		private sealed class _List_89 : List<string>
		{
			public _List_89()
			{
				{
					// no doubt (-> that cats have tails <-)
					this.Add("simple");
				}
			}
		}

		private sealed class _List_92 : List<string>
		{
			public _List_92()
			{
				{
					// no doubt (-> that cats have tails <-)
					this.Add("simple");
				}
			}
		}

		private sealed class _Dictionary_56 : Dictionary<string, IList<string>>
		{
			public _Dictionary_56()
			{
				{
					this["comp"] = new _List_57();
					this["ccomp"] = new _List_60();
					this["xcomp"] = new _List_63();
					this["vmod"] = new _List_68();
					this["csubj"] = new _List_72();
					this["advcl"] = new _List_76();
					this["advcl:*"] = new _List_80();
					this["conj:*"] = new _List_84();
					this["acl:relcl"] = new _List_89();
					this["parataxis"] = new _List_92();
				}
			}
		}

		/// <summary>A specification for clause splits we _always_ want to do.</summary>
		/// <remarks>
		/// A specification for clause splits we _always_ want to do. The format is a map from the edge label we are splitting, to
		/// the preference for the type of split we should do. The most preferred is at the front of the list, and then it backs off
		/// to the less and less preferred split types.
		/// </remarks>
		protected internal static readonly IDictionary<string, IList<string>> HardSplits = Java.Util.Collections.UnmodifiableMap(new _Dictionary_56());

		private sealed class _HashSet_100 : HashSet<string>
		{
			public _HashSet_100()
			{
				{
					this.Add("report");
					this.Add("say");
					this.Add("told");
					this.Add("claim");
					this.Add("assert");
					this.Add("think");
					this.Add("believe");
					this.Add("suppose");
				}
			}
		}

		/// <summary>A set of words which indicate that the complement clause is not factual, or at least not necessarily factual.</summary>
		protected internal static readonly ICollection<string> IndirectSpeechLemmas = Java.Util.Collections.UnmodifiableSet(new _HashSet_100());

		/// <summary>The tree to search over.</summary>
		public readonly SemanticGraph tree;

		/// <summary>The assumed truth of the original clause.</summary>
		public readonly bool assumedTruth;

		/// <summary>The length of the sentence, as determined from the tree.</summary>
		public readonly int sentenceLength;

		/// <summary>A mapping from a word to the extra edges that come out of it.</summary>
		private readonly IDictionary<IndexedWord, ICollection<SemanticGraphEdge>> extraEdgesByGovernor = new Dictionary<IndexedWord, ICollection<SemanticGraphEdge>>();

		/// <summary>A mapping from a word to the extra edges that to into it.</summary>
		private readonly IDictionary<IndexedWord, ICollection<SemanticGraphEdge>> extraEdgesByDependent = new Dictionary<IndexedWord, ICollection<SemanticGraphEdge>>();

		/// <summary>The classifier for whether a particular dependency edge defines a clause boundary.</summary>
		private readonly Optional<IClassifier<ClauseSplitter.ClauseClassifierLabel, string>> isClauseClassifier;

		/// <summary>
		/// An optional featurizer to use with the clause classifier (
		/// <see cref="isClauseClassifier"/>
		/// ).
		/// If that classifier is defined, this should be as well.
		/// </summary>
		private readonly Optional<IFunction<Triple<ClauseSplitterSearchProblem.State, ClauseSplitterSearchProblem.IAction, ClauseSplitterSearchProblem.State>, ICounter<string>>> featurizer;

		/// <summary>A mapping from edges in the tree, to an index.</summary>
		private readonly IIndex<SemanticGraphEdge> edgeToIndex = new HashIndex<SemanticGraphEdge>(null, null);

		/// <summary>A search state.</summary>
		public class State
		{
			public readonly SemanticGraphEdge edge;

			public readonly int edgeIndex;

			public readonly SemanticGraphEdge subjectOrNull;

			public readonly int distanceFromSubj;

			public readonly SemanticGraphEdge objectOrNull;

			public readonly IConsumer<SemanticGraph> thunk;

			public bool isDone;

			public State(ClauseSplitterSearchProblem _enclosing, SemanticGraphEdge edge, SemanticGraphEdge subjectOrNull, int distanceFromSubj, SemanticGraphEdge objectOrNull, IConsumer<SemanticGraph> thunk, bool isDone)
			{
				this._enclosing = _enclosing;
				// It's lying -- type inference times out with a diamond
				this.edge = edge;
				this.edgeIndex = this._enclosing.edgeToIndex.IndexOf(edge);
				this.subjectOrNull = subjectOrNull;
				this.distanceFromSubj = distanceFromSubj;
				this.objectOrNull = objectOrNull;
				this.thunk = thunk;
				this.isDone = isDone;
			}

			public State(ClauseSplitterSearchProblem _enclosing, ClauseSplitterSearchProblem.State source, bool isDone)
			{
				this._enclosing = _enclosing;
				this.edge = source.edge;
				this.edgeIndex = this._enclosing.edgeToIndex.IndexOf(this.edge);
				this.subjectOrNull = source.subjectOrNull;
				this.distanceFromSubj = source.distanceFromSubj;
				this.objectOrNull = source.objectOrNull;
				this.thunk = source.thunk;
				this.isDone = isDone;
			}

			public virtual SemanticGraph OriginalTree()
			{
				return this._enclosing.tree;
			}

			public virtual ClauseSplitterSearchProblem.State WithIsDone(ClauseSplitter.ClauseClassifierLabel argmax)
			{
				if (argmax == ClauseSplitter.ClauseClassifierLabel.ClauseSplit)
				{
					this.isDone = true;
				}
				else
				{
					if (argmax == ClauseSplitter.ClauseClassifierLabel.ClauseInterm)
					{
						this.isDone = false;
					}
					else
					{
						throw new InvalidOperationException("Invalid classifier label for isDone: " + argmax);
					}
				}
				return this;
			}

			private readonly ClauseSplitterSearchProblem _enclosing;
		}

		/// <summary>An action being taken; that is, the type of clause splitting going on.</summary>
		public interface IAction
		{
			/// <summary>The name of this action.</summary>
			string Signature();

			/// <summary>A check to make sure this is actually a valid action to take, in the context of the given tree.</summary>
			/// <param name="originalTree">The _original_ tree we are searching over. This is before any clauses are split off.</param>
			/// <param name="edge">The edge that we are traversing with this clause.</param>
			/// <returns>True if this is a valid action.</returns>
			bool PrerequisitesMet(SemanticGraph originalTree, SemanticGraphEdge edge);

			/// <summary>Apply this action to the given state.</summary>
			/// <param name="tree">The original tree we are applying the action to.</param>
			/// <param name="source">The source state we are mutating from.</param>
			/// <param name="outgoingEdge">The edge we are splitting off as a clause.</param>
			/// <param name="subjectOrNull">The subject of the parent tree, if there is one.</param>
			/// <param name="ppOrNull">The preposition attachment of the parent tree, if there is one.</param>
			/// <returns>
			/// A new state, or
			/// <see cref="Java.Util.Optional{T}.Empty{T}()"/>
			/// if this action was not successful.
			/// </returns>
			Optional<ClauseSplitterSearchProblem.State> ApplyTo(SemanticGraph tree, ClauseSplitterSearchProblem.State source, SemanticGraphEdge outgoingEdge, SemanticGraphEdge subjectOrNull, SemanticGraphEdge ppOrNull);
		}

		/// <summary>The options used for training the clause searcher.</summary>
		public class TrainingOptions
		{
			public double negativeSubsampleRatio = 1.00;

			public float positiveDatumWeight = 100.0f;

			public float unknownDatumWeight = 1.0f;

			public float clauseSplitWeight = 1.0f;

			public float clauseIntermWeight = 2.0f;

			public int seed = 42;

			public Type classifierFactory = (Type)((object)typeof(LinearClassifierFactory));
		}

		/// <summary>Mostly just an alias, but make sure our featurizer is serializable!</summary>
		public interface IFeaturizer : IFunction<Triple<ClauseSplitterSearchProblem.State, ClauseSplitterSearchProblem.IAction, ClauseSplitterSearchProblem.State>, ICounter<string>>
		{
			bool IsSimpleSplit(ICounter<string> feats);
		}

		/// <summary>
		/// Create a searcher manually, suppling a dependency tree, an optional classifier for when to split clauses,
		/// and a featurizer for that classifier.
		/// </summary>
		/// <remarks>
		/// Create a searcher manually, suppling a dependency tree, an optional classifier for when to split clauses,
		/// and a featurizer for that classifier.
		/// You almost certainly want to use
		/// <see cref="IClauseSplitter.Load(string)"/>
		/// instead of this
		/// constructor.
		/// </remarks>
		/// <param name="tree">The dependency tree to search over.</param>
		/// <param name="assumedTruth">The assumed truth of the tree (relevant for natural logic inference). If in doubt, pass in true.</param>
		/// <param name="isClauseClassifier">The classifier for whether a given dependency arc should be a new clause. If this is not given, all arcs are treated as clause separators.</param>
		/// <param name="featurizer">
		/// The featurizer for the classifier. If no featurizer is given, one should be given in
		/// <see cref="Search(Java.Util.Function.IPredicate{T}, Edu.Stanford.Nlp.Classify.IClassifier{L, F}, System.Collections.Generic.IDictionary{K, V}, Java.Util.Function.IFunction{T, R}, int)"/>
		/// , or else the classifier will be useless.
		/// </param>
		/// <seealso cref="IClauseSplitter.Load(string)"/>
		protected internal ClauseSplitterSearchProblem(SemanticGraph tree, bool assumedTruth, Optional<IClassifier<ClauseSplitter.ClauseClassifierLabel, string>> isClauseClassifier, Optional<IFunction<Triple<ClauseSplitterSearchProblem.State, ClauseSplitterSearchProblem.IAction
			, ClauseSplitterSearchProblem.State>, ICounter<string>>> featurizer)
		{
			this.tree = new SemanticGraph(tree);
			this.assumedTruth = assumedTruth;
			this.isClauseClassifier = isClauseClassifier;
			this.featurizer = featurizer;
			// Index edges
			this.tree.EdgeIterable().ForEach(null);
			// Get length
			IList<IndexedWord> sortedVertices = tree.VertexListSorted();
			sentenceLength = sortedVertices[sortedVertices.Count - 1].Index();
			// Register extra edges
			foreach (IndexedWord vertex in sortedVertices)
			{
				extraEdgesByGovernor[vertex] = new List<SemanticGraphEdge>();
				extraEdgesByDependent[vertex] = new List<SemanticGraphEdge>();
			}
			IList<SemanticGraphEdge> extraEdges = Edu.Stanford.Nlp.Naturalli.Util.CleanTree(this.tree);
			System.Diagnostics.Debug.Assert(Edu.Stanford.Nlp.Naturalli.Util.IsTree(this.tree));
			foreach (SemanticGraphEdge edge in extraEdges)
			{
				extraEdgesByGovernor[edge.GetGovernor()].Add(edge);
				extraEdgesByDependent[edge.GetDependent()].Add(edge);
			}
		}

		/// <summary>Create a clause searcher which searches naively through every possible subtree as a clause.</summary>
		/// <remarks>
		/// Create a clause searcher which searches naively through every possible subtree as a clause.
		/// For an end-user, this is almost certainly not what you want.
		/// However, it is very useful for training time.
		/// </remarks>
		/// <param name="tree">The dependency tree to search over.</param>
		/// <param name="assumedTruth">The truth of the premise. Almost always True.</param>
		public ClauseSplitterSearchProblem(SemanticGraph tree, bool assumedTruth)
			: this(tree, assumedTruth, Optional.Empty(), Optional.Empty())
		{
		}

		/// <summary>The basic method for splitting off a clause of a tree.</summary>
		/// <remarks>
		/// The basic method for splitting off a clause of a tree.
		/// This modifies the tree in place.
		/// </remarks>
		/// <param name="tree">The tree to split a clause from.</param>
		/// <param name="toKeep">The edge representing the clause to keep.</param>
		internal static void SplitToChildOfEdge(SemanticGraph tree, SemanticGraphEdge toKeep)
		{
			IQueue<IndexedWord> fringe = new LinkedList<IndexedWord>();
			IList<IndexedWord> nodesToRemove = new List<IndexedWord>();
			// Find nodes to remove
			// (from the root)
			foreach (IndexedWord root in tree.GetRoots())
			{
				nodesToRemove.Add(root);
				foreach (SemanticGraphEdge @out in tree.OutgoingEdgeIterable(root))
				{
					if (!@out.Equals(toKeep))
					{
						fringe.Add(@out.GetDependent());
					}
				}
			}
			// (recursively)
			while (!fringe.IsEmpty())
			{
				IndexedWord node = fringe.Poll();
				nodesToRemove.Add(node);
				foreach (SemanticGraphEdge @out in tree.OutgoingEdgeIterable(node))
				{
					if (!@out.Equals(toKeep))
					{
						fringe.Add(@out.GetDependent());
					}
				}
			}
			// Remove nodes
			nodesToRemove.ForEach(null);
			// Set new root
			tree.SetRoot(toKeep.GetDependent());
		}

		/// <summary>The basic method for splitting off a clause of a tree.</summary>
		/// <remarks>
		/// The basic method for splitting off a clause of a tree.
		/// This modifies the tree in place.
		/// This method addtionally follows ref edges.
		/// </remarks>
		/// <param name="tree">The tree to split a clause from.</param>
		/// <param name="toKeep">The edge representing the clause to keep.</param>
		private void SimpleClause(SemanticGraph tree, SemanticGraphEdge toKeep)
		{
			SplitToChildOfEdge(tree, toKeep);
			// Follow 'ref' edges
			IDictionary<IndexedWord, IndexedWord> refReplaceMap = new Dictionary<IndexedWord, IndexedWord>();
			// (find replacements)
			foreach (IndexedWord vertex in tree.VertexSet())
			{
				foreach (SemanticGraphEdge edge in extraEdgesByDependent[vertex])
				{
					if ("ref".Equals(edge.GetRelation().ToString()) && !tree.ContainsVertex(edge.GetGovernor()))
					{
						// it's a ref edge...
						// ...that doesn't already exist in the tree.
						refReplaceMap[vertex] = edge.GetGovernor();
					}
				}
			}
			// (do replacements)
			foreach (KeyValuePair<IndexedWord, IndexedWord> entry in refReplaceMap)
			{
				IEnumerator<SemanticGraphEdge> iter = tree.IncomingEdgeIterator(entry.Key);
				if (!iter.MoveNext())
				{
					continue;
				}
				SemanticGraphEdge incomingEdge = iter.Current;
				IndexedWord governor = incomingEdge.GetGovernor();
				tree.RemoveVertex(entry.Key);
				AddSubtree(tree, governor, incomingEdge.GetRelation().ToString(), this.tree, entry.Value, this.tree.IncomingEdgeList(tree.GetFirstRoot()));
			}
		}

		/// <summary>A helper to add a single word to a given dependency tree</summary>
		/// <param name="toModify">The tree to add the word to.</param>
		/// <param name="root">The root of the tree where we should be adding the word.</param>
		/// <param name="rel">The relation to add the word with.</param>
		/// <param name="coreLabel">The word to add.</param>
		private static void AddWord(SemanticGraph toModify, IndexedWord root, string rel, CoreLabel coreLabel)
		{
			IndexedWord dependent = new IndexedWord(coreLabel);
			toModify.AddVertex(dependent);
			toModify.AddEdge(root, dependent, GrammaticalRelation.ValueOf(Language.English, rel), double.NegativeInfinity, false);
		}

		/// <summary>A helper to add an entire subtree to a given dependency tree.</summary>
		/// <param name="toModify">The tree to add the subtree to.</param>
		/// <param name="root">The root of the tree where we should be adding the subtree.</param>
		/// <param name="rel">The relation to add the subtree with.</param>
		/// <param name="originalTree">
		/// The orignal tree (i.e.,
		/// <see cref="tree"/>
		/// ).
		/// </param>
		/// <param name="subject">The root of the clause to add.</param>
		/// <param name="ignoredEdges">The edges to ignore adding when adding this subtree.</param>
		private static void AddSubtree(SemanticGraph toModify, IndexedWord root, string rel, SemanticGraph originalTree, IndexedWord subject, ICollection<SemanticGraphEdge> ignoredEdges)
		{
			if (toModify.ContainsVertex(subject))
			{
				return;
			}
			// This subtree already exists.
			IQueue<IndexedWord> fringe = new LinkedList<IndexedWord>();
			ICollection<IndexedWord> wordsToAdd = new List<IndexedWord>();
			ICollection<SemanticGraphEdge> edgesToAdd = new List<SemanticGraphEdge>();
			// Search for subtree to add
			foreach (SemanticGraphEdge edge in originalTree.OutgoingEdgeIterable(subject))
			{
				if (!ignoredEdges.Contains(edge))
				{
					if (toModify.ContainsVertex(edge.GetDependent()))
					{
						// Case: we're adding a subtree that's not disjoint from toModify. This is bad news.
						return;
					}
					edgesToAdd.Add(edge);
					fringe.Add(edge.GetDependent());
				}
			}
			while (!fringe.IsEmpty())
			{
				IndexedWord node = fringe.Poll();
				wordsToAdd.Add(node);
				foreach (SemanticGraphEdge edge_1 in originalTree.OutgoingEdgeIterable(node))
				{
					if (!ignoredEdges.Contains(edge_1))
					{
						if (toModify.ContainsVertex(edge_1.GetDependent()))
						{
							// Case: we're adding a subtree that's not disjoint from toModify. This is bad news.
							return;
						}
						edgesToAdd.Add(edge_1);
						fringe.Add(edge_1.GetDependent());
					}
				}
			}
			// Add subtree
			// (add subject)
			toModify.AddVertex(subject);
			toModify.AddEdge(root, subject, GrammaticalRelation.ValueOf(Language.English, rel), double.NegativeInfinity, false);
			// (add nodes)
			wordsToAdd.ForEach(null);
			// (add edges)
			foreach (SemanticGraphEdge edge_2 in edgesToAdd)
			{
				System.Diagnostics.Debug.Assert(!toModify.IncomingEdgeIterator(edge_2.GetDependent()).MoveNext());
				toModify.AddEdge(edge_2.GetGovernor(), edge_2.GetDependent(), edge_2.GetRelation(), edge_2.GetWeight(), edge_2.IsExtra());
			}
		}

		/// <summary>Strips aux and mark edges when we are splitting into a clause.</summary>
		/// <param name="toModify">The tree we are stripping the edges from.</param>
		private static void StripAuxMark(SemanticGraph toModify)
		{
			IList<SemanticGraphEdge> toClean = new List<SemanticGraphEdge>();
			foreach (SemanticGraphEdge edge in toModify.OutgoingEdgeIterable(toModify.GetFirstRoot()))
			{
				string rel = edge.GetRelation().ToString();
				if (("aux".Equals(rel) || "mark".Equals(rel)) && !toModify.OutgoingEdgeIterator(edge.GetDependent()).MoveNext())
				{
					toClean.Add(edge);
				}
			}
			foreach (SemanticGraphEdge edge_1 in toClean)
			{
				toModify.RemoveEdge(edge_1);
				toModify.RemoveVertex(edge_1.GetDependent());
			}
		}

		/// <summary>Create a mock node, to be added to the dependency tree but which is not part of the original sentence.</summary>
		/// <param name="toCopy">The CoreLabel to copy from initially.</param>
		/// <param name="word">The new word to add.</param>
		/// <param name="Pos">The new part of speech to add.</param>
		/// <returns>A CoreLabel copying most fields from toCopy, but with a new word and POS tag (as well as a new index).</returns>
		private CoreLabel MockNode(CoreLabel toCopy, string word, string Pos)
		{
			CoreLabel mock = new CoreLabel(toCopy);
			mock.SetWord(word);
			mock.SetLemma(word);
			mock.SetValue(word);
			mock.SetNER("O");
			mock.SetTag(Pos);
			mock.SetIndex(sentenceLength + 5);
			return mock;
		}

		/// <summary>
		/// Get the top few clauses from this searcher, cutting off at the given minimum
		/// probability.
		/// </summary>
		/// <param name="thresholdProbability">The threshold under which to stop returning clauses. This should be between 0 and 1.</param>
		/// <param name="maxClauses">A hard limit on the number of clauses to return.</param>
		/// <returns>
		/// The resulting
		/// <see cref="SentenceFragment"/>
		/// objects, representing the top clauses of the sentence.
		/// </returns>
		public virtual IList<SentenceFragment> TopClauses(double thresholdProbability, int maxClauses)
		{
			IList<SentenceFragment> results = new List<SentenceFragment>();
			Search(null);
			return results;
		}

		/// <summary>Search, using the default weights / featurizer.</summary>
		/// <remarks>
		/// Search, using the default weights / featurizer. This is the most common entry method for the raw search,
		/// though
		/// <see cref="TopClauses(double, int)"/>
		/// may be a more convenient method for
		/// an end user.
		/// </remarks>
		/// <param name="candidateFragments">The callback function for results. The return value defines whether to continue searching.</param>
		public virtual void Search(IPredicate<Triple<double, IList<ICounter<string>>, ISupplier<SentenceFragment>>> candidateFragments)
		{
			if (!isClauseClassifier.IsPresent())
			{
				Search(candidateFragments, new LinearClassifier<ClauseSplitter.ClauseClassifierLabel, string>(new ClassicCounter<Pair<string, ClauseSplitter.ClauseClassifierLabel>>()), HardSplits, this.featurizer.OrElse(DefaultFeaturizer), 1000);
			}
			else
			{
				if (!(isClauseClassifier.Get() is LinearClassifier))
				{
					throw new ArgumentException("For now, only linear classifiers are supported");
				}
				Search(candidateFragments, isClauseClassifier.Get(), HardSplits, this.featurizer.Get(), 1000);
			}
		}

		/// <summary>Search from the root of the tree.</summary>
		/// <remarks>
		/// Search from the root of the tree.
		/// This function also defines the default action space to use during search.
		/// This is NOT recommended to be used at test time.
		/// </remarks>
		/// <seealso cref="Search(Java.Util.Function.IPredicate{T})"/>
		/// <param name="candidateFragments">The callback function.</param>
		/// <param name="classifier">The classifier for whether an arc should be on the path to a clause split, a clause split itself, or neither.</param>
		/// <param name="featurizer">The featurizer to use during search, to be dot producted with the weights.</param>
		public virtual void Search(IPredicate<Triple<double, IList<ICounter<string>>, ISupplier<SentenceFragment>>> candidateFragments, IClassifier<ClauseSplitter.ClauseClassifierLabel, string> classifier, IDictionary<string, IList<string>> hardCodedSplits
			, IFunction<Triple<ClauseSplitterSearchProblem.State, ClauseSplitterSearchProblem.IAction, ClauseSplitterSearchProblem.State>, ICounter<string>> featurizer, int maxTicks)
		{
			// The output specs
			// The learning specs
			ICollection<ClauseSplitterSearchProblem.IAction> actionSpace = new List<ClauseSplitterSearchProblem.IAction>();
			// SIMPLE SPLIT
			actionSpace.Add(new _IAction_564());
			// CLONE ROOT
			actionSpace.Add(new _IAction_596());
			// Only valid if there's a single nontrivial outgoing edge from a node. Otherwise it's a whole can of worms.
			// what?
			//              addWord(toModify, outgoingEdge.getDependent(), "auxpass", mockNode(outgoingEdge.getDependent().backingLabel(), "is", "VBZ"));
			// COPY SUBJECT
			actionSpace.Add(new _IAction_643());
			// Don't split into anything but verbs or nouns
			// COPY OBJECT
			actionSpace.Add(new _IAction_685());
			// Don't split into anything but verbs or nouns
			// Split the clause
			// Attach the new subject
			// Strip bits we don't want
			foreach (IndexedWord root in tree.GetRoots())
			{
				Search(root, candidateFragments, classifier, hardCodedSplits, featurizer, actionSpace, maxTicks);
			}
		}

		private sealed class _IAction_564 : ClauseSplitterSearchProblem.IAction
		{
			public _IAction_564()
			{
			}

			public string Signature()
			{
				return "simple";
			}

			public bool PrerequisitesMet(SemanticGraph originalTree, SemanticGraphEdge edge)
			{
				char tag = edge.GetDependent().Tag()[0];
				return !(tag != 'V' && tag != 'N' && tag != 'J' && tag != 'P' && tag != 'D');
			}

			public Optional<ClauseSplitterSearchProblem.State> ApplyTo(SemanticGraph tree, ClauseSplitterSearchProblem.State source, SemanticGraphEdge outgoingEdge, SemanticGraphEdge subjectOrNull, SemanticGraphEdge objectOrNull)
			{
				return Optional.Of(new ClauseSplitterSearchProblem.State(this, outgoingEdge, subjectOrNull == null ? source.subjectOrNull : subjectOrNull, subjectOrNull == null ? (source.distanceFromSubj + 1) : 0, objectOrNull == null ? source.objectOrNull : 
					objectOrNull, source.thunk.AndThen(null), false));
			}
		}

		private sealed class _IAction_596 : ClauseSplitterSearchProblem.IAction
		{
			public _IAction_596()
			{
			}

			public string Signature()
			{
				return "clone_root_as_nsubjpass";
			}

			public bool PrerequisitesMet(SemanticGraph originalTree, SemanticGraphEdge edge)
			{
				IEnumerator<SemanticGraphEdge> iter = originalTree.OutgoingEdgeIterable(edge.GetGovernor()).GetEnumerator();
				if (!iter.MoveNext())
				{
					return false;
				}
				bool nontrivialEdge = false;
				while (iter.MoveNext())
				{
					SemanticGraphEdge outEdge = iter.Current;
					switch (outEdge.GetRelation().ToString())
					{
						case "nn":
						case "amod":
						{
							break;
						}

						default:
						{
							if (nontrivialEdge)
							{
								return false;
							}
							nontrivialEdge = true;
							break;
						}
					}
				}
				return true;
			}

			public Optional<ClauseSplitterSearchProblem.State> ApplyTo(SemanticGraph tree, ClauseSplitterSearchProblem.State source, SemanticGraphEdge outgoingEdge, SemanticGraphEdge subjectOrNull, SemanticGraphEdge objectOrNull)
			{
				return Optional.Of(new ClauseSplitterSearchProblem.State(this, outgoingEdge, subjectOrNull == null ? source.subjectOrNull : subjectOrNull, subjectOrNull == null ? (source.distanceFromSubj + 1) : 0, objectOrNull == null ? source.objectOrNull : 
					objectOrNull, source.thunk.AndThen(null), true));
			}
		}

		private sealed class _IAction_643 : ClauseSplitterSearchProblem.IAction
		{
			public _IAction_643()
			{
			}

			public string Signature()
			{
				return "clone_nsubj";
			}

			public bool PrerequisitesMet(SemanticGraph originalTree, SemanticGraphEdge edge)
			{
				char tag = edge.GetDependent().Tag()[0];
				if (tag != 'V' && tag != 'N')
				{
					return false;
				}
				foreach (SemanticGraphEdge grandchild in originalTree.OutgoingEdgeIterable(edge.GetDependent()))
				{
					if (grandchild.GetRelation().ToString().Contains("subj"))
					{
						return false;
					}
				}
				return true;
			}

			public Optional<ClauseSplitterSearchProblem.State> ApplyTo(SemanticGraph tree, ClauseSplitterSearchProblem.State source, SemanticGraphEdge outgoingEdge, SemanticGraphEdge subjectOrNull, SemanticGraphEdge objectOrNull)
			{
				if (subjectOrNull != null && !outgoingEdge.Equals(subjectOrNull))
				{
					return Optional.Of(new ClauseSplitterSearchProblem.State(this, outgoingEdge, subjectOrNull, 0, objectOrNull == null ? source.objectOrNull : objectOrNull, source.thunk.AndThen(null), false));
				}
				else
				{
					return Optional.Empty();
				}
			}
		}

		private sealed class _IAction_685 : ClauseSplitterSearchProblem.IAction
		{
			public _IAction_685()
			{
			}

			public string Signature()
			{
				return "clone_dobj";
			}

			public bool PrerequisitesMet(SemanticGraph originalTree, SemanticGraphEdge edge)
			{
				char tag = edge.GetDependent().Tag()[0];
				if (tag != 'V' && tag != 'N')
				{
					return false;
				}
				foreach (SemanticGraphEdge grandchild in originalTree.OutgoingEdgeIterable(edge.GetDependent()))
				{
					if (grandchild.GetRelation().ToString().Contains("subj"))
					{
						return false;
					}
				}
				return true;
			}

			public Optional<ClauseSplitterSearchProblem.State> ApplyTo(SemanticGraph tree, ClauseSplitterSearchProblem.State source, SemanticGraphEdge outgoingEdge, SemanticGraphEdge subjectOrNull, SemanticGraphEdge objectOrNull)
			{
				if (objectOrNull != null && !outgoingEdge.Equals(objectOrNull))
				{
					return Optional.Of(new ClauseSplitterSearchProblem.State(this, outgoingEdge, subjectOrNull == null ? source.subjectOrNull : subjectOrNull, subjectOrNull == null ? (source.distanceFromSubj + 1) : 0, objectOrNull, source.thunk.AndThen(null), false
						));
				}
				else
				{
					return Optional.Empty();
				}
			}
		}

		/// <summary>Re-order the action space based on the specified order of names.</summary>
		private static ICollection<ClauseSplitterSearchProblem.IAction> OrderActions(ICollection<ClauseSplitterSearchProblem.IAction> actionSpace, IList<string> order)
		{
			IList<ClauseSplitterSearchProblem.IAction> tmp = new List<ClauseSplitterSearchProblem.IAction>(actionSpace);
			IList<ClauseSplitterSearchProblem.IAction> @out = new List<ClauseSplitterSearchProblem.IAction>();
			foreach (string key in order)
			{
				IEnumerator<ClauseSplitterSearchProblem.IAction> iter = tmp.GetEnumerator();
				while (iter.MoveNext())
				{
					ClauseSplitterSearchProblem.IAction a = iter.Current;
					if (a.Signature().Equals(key))
					{
						@out.Add(a);
						iter.Remove();
					}
				}
			}
			Sharpen.Collections.AddAll(@out, tmp);
			return @out;
		}

		/// <summary>The core implementation of the search.</summary>
		/// <param name="root">The root word to search from. Traditionally, this is the root of the sentence.</param>
		/// <param name="candidateFragments">
		/// The callback for the resulting sentence fragments.
		/// This is a predicate of a triple of values.
		/// The return value of the predicate determines whether we should continue searching.
		/// The triple is a triple of
		/// <ol>
		/// <li>The log probability of the sentence fragment, according to the featurizer and the weights</li>
		/// <li>The features along the path to this fragment. The last element of this is the features from the most recent step.</li>
		/// <li>The sentence fragment. Because it is relatively expensive to compute the resulting tree, this is returned as a lazy
		/// <see cref="Java.Util.Function.ISupplier{T}"/>
		/// .</li>
		/// </ol>
		/// </param>
		/// <param name="classifier">The classifier for whether an arc should be on the path to a clause split, a clause split itself, or neither.</param>
		/// <param name="featurizer">The featurizer to use. Make sure this matches the weights!</param>
		/// <param name="actionSpace">The action space we are allowed to take. Each action defines a means of splitting a clause on a dependency boundary.</param>
		protected internal virtual void Search(IndexedWord root, IPredicate<Triple<double, IList<ICounter<string>>, ISupplier<SentenceFragment>>> candidateFragments, IClassifier<ClauseSplitter.ClauseClassifierLabel, string> classifier, IDictionary<string
			, IList<string>> hardCodedSplits, IFunction<Triple<ClauseSplitterSearchProblem.State, ClauseSplitterSearchProblem.IAction, ClauseSplitterSearchProblem.State>, ICounter<string>> featurizer, ICollection<ClauseSplitterSearchProblem.IAction> actionSpace
			, int maxTicks)
		{
			// The root to search from
			// The output specs
			// The learning specs
			// (the fringe)
			IPriorityQueue<Pair<ClauseSplitterSearchProblem.State, IList<ICounter<string>>>> fringe = new FixedPrioritiesPriorityQueue<Pair<ClauseSplitterSearchProblem.State, IList<ICounter<string>>>>();
			// (avoid duplicate work)
			ICollection<IndexedWord> seenWords = new HashSet<IndexedWord>();
			ClauseSplitterSearchProblem.State firstState = new ClauseSplitterSearchProblem.State(this, null, null, -9000, null, null, true);
			// First state is implicitly "done"
			fringe.Add(Pair.MakePair(firstState, new List<ICounter<string>>(0)), -0.0);
			int ticks = 0;
			while (!fringe.IsEmpty())
			{
				if (++ticks > maxTicks)
				{
					//        log.info("WARNING! Timed out on search with " + ticks + " ticks");
					return;
				}
				// Useful variables
				double logProbSoFar = fringe.GetPriority();
				System.Diagnostics.Debug.Assert(logProbSoFar <= 0.0);
				Pair<ClauseSplitterSearchProblem.State, IList<ICounter<string>>> lastStatePair = fringe.RemoveFirst();
				ClauseSplitterSearchProblem.State lastState = lastStatePair.first;
				IList<ICounter<string>> featuresSoFar = lastStatePair.second;
				IndexedWord rootWord = lastState.edge == null ? root : lastState.edge.GetDependent();
				// Register thunk
				if (lastState.isDone)
				{
					if (!candidateFragments.Test(Triple.MakeTriple(logProbSoFar, featuresSoFar, null)))
					{
						// Add the extra edges back in, if they don't break the tree-ness of the extraction
						// what a strange thing to have happen...
						//noinspection unchecked
						break;
					}
				}
				// Find relevant auxiliary terms
				SemanticGraphEdge subjOrNull = null;
				SemanticGraphEdge objOrNull = null;
				foreach (SemanticGraphEdge auxEdge in tree.OutgoingEdgeIterable(rootWord))
				{
					string relString = auxEdge.GetRelation().ToString();
					if (relString.Contains("obj"))
					{
						objOrNull = auxEdge;
					}
					else
					{
						if (relString.Contains("subj"))
						{
							subjOrNull = auxEdge;
						}
					}
				}
				// Iterate over children
				// For each outgoing edge...
				foreach (SemanticGraphEdge outgoingEdge in tree.OutgoingEdgeIterable(rootWord))
				{
					// Prohibit indirect speech verbs from splitting off clauses
					// (e.g., 'said', 'think')
					// This fires if the governor is an indirect speech verb, and the outgoing edge is a ccomp
					if (outgoingEdge.GetRelation().ToString().Equals("ccomp") && ((outgoingEdge.GetGovernor().Lemma() != null && IndirectSpeechLemmas.Contains(outgoingEdge.GetGovernor().Lemma())) || IndirectSpeechLemmas.Contains(outgoingEdge.GetGovernor().Word(
						))))
					{
						continue;
					}
					// Get some variables
					string outgoingEdgeRelation = outgoingEdge.GetRelation().ToString();
					IList<string> forcedArcOrder = hardCodedSplits[outgoingEdgeRelation];
					if (forcedArcOrder == null && outgoingEdgeRelation.Contains(":"))
					{
						forcedArcOrder = hardCodedSplits[Sharpen.Runtime.Substring(outgoingEdgeRelation, 0, outgoingEdgeRelation.IndexOf(':')) + ":*"];
					}
					bool doneForcedArc = false;
					// For each action...
					foreach (ClauseSplitterSearchProblem.IAction action in (forcedArcOrder == null ? actionSpace : OrderActions(actionSpace, forcedArcOrder)))
					{
						// Check the prerequisite
						if (!action.PrerequisitesMet(tree, outgoingEdge))
						{
							continue;
						}
						if (forcedArcOrder != null && doneForcedArc)
						{
							break;
						}
						// 1. Compute the child state
						Optional<ClauseSplitterSearchProblem.State> candidate = action.ApplyTo(tree, lastState, outgoingEdge, subjOrNull, objOrNull);
						if (candidate.IsPresent())
						{
							double logProbability;
							ClauseSplitter.ClauseClassifierLabel bestLabel;
							ICounter<string> features = featurizer.Apply(Triple.MakeTriple(lastState, action, candidate.Get()));
							if (forcedArcOrder != null && !doneForcedArc)
							{
								logProbability = 0.0;
								bestLabel = ClauseSplitter.ClauseClassifierLabel.ClauseSplit;
								doneForcedArc = true;
							}
							else
							{
								if (features.ContainsKey("__undocumented_junit_no_classifier"))
								{
									logProbability = double.NegativeInfinity;
									bestLabel = ClauseSplitter.ClauseClassifierLabel.ClauseInterm;
								}
								else
								{
									ICounter<ClauseSplitter.ClauseClassifierLabel> scores = classifier.ScoresOf(new RVFDatum<ClauseSplitter.ClauseClassifierLabel, string>(features));
									if (scores.Size() > 0)
									{
										Counters.LogNormalizeInPlace(scores);
									}
									string rel = outgoingEdge.GetRelation().ToString();
									if ("nsubj".Equals(rel) || "dobj".Equals(rel))
									{
										scores.Remove(ClauseSplitter.ClauseClassifierLabel.NotAClause);
									}
									// Always at least yield on nsubj and dobj
									logProbability = Counters.Max(scores, double.NegativeInfinity);
									bestLabel = Counters.Argmax(scores, null, ClauseSplitter.ClauseClassifierLabel.ClauseSplit);
								}
							}
							if (bestLabel != ClauseSplitter.ClauseClassifierLabel.NotAClause)
							{
								Pair<ClauseSplitterSearchProblem.State, IList<ICounter<string>>> childState = Pair.MakePair(candidate.Get().WithIsDone(bestLabel), new _List_897(features, featuresSoFar));
								// 2. Register the child state
								if (!seenWords.Contains(childState.first.edge.GetDependent()))
								{
									//            log.info("  pushing " + action.signature() + " with " + argmax.first.edge);
									fringe.Add(childState, logProbability);
								}
							}
						}
					}
				}
				seenWords.Add(rootWord);
			}
		}

		private sealed class _List_897 : List<ICounter<string>>
		{
			public _List_897(ICounter<string> features, ICollection<ICounter<string>> baseArg1)
				: base(baseArg1)
			{
				this.features = features;
				{
					this.Add(features);
				}
			}

			private readonly ICounter<string> features;
		}

		private sealed class _IFeaturizer_920 : ClauseSplitterSearchProblem.IFeaturizer
		{
			public _IFeaturizer_920()
			{
				this.serialVersionUID = 4145523451314579506L;
			}

			private const long serialVersionUID;

			//    log.info("Search finished in " + ticks + " ticks and " + classifierEvals + " classifier evaluations.");
			public bool IsSimpleSplit(ICounter<string> feats)
			{
				foreach (string key in feats.KeySet())
				{
					if (key.StartsWith("simple&"))
					{
						return true;
					}
				}
				return false;
			}

			public ICounter<string> Apply(Triple<ClauseSplitterSearchProblem.State, ClauseSplitterSearchProblem.IAction, ClauseSplitterSearchProblem.State> triple)
			{
				// Variables
				ClauseSplitterSearchProblem.State from = triple.first;
				ClauseSplitterSearchProblem.IAction action = triple.second;
				ClauseSplitterSearchProblem.State to = triple.third;
				string signature = action.Signature();
				string edgeRelTaken = to.edge == null ? "root" : to.edge.GetRelation().ToString();
				string edgeRelShort = to.edge == null ? "root" : to.edge.GetRelation().GetShortName();
				if (edgeRelShort.Contains("_"))
				{
					edgeRelShort = Sharpen.Runtime.Substring(edgeRelShort, 0, edgeRelShort.IndexOf('_'));
				}
				// -- Featurize --
				// Variables to aggregate
				bool parentHasSubj = false;
				bool parentHasObj = false;
				bool childHasSubj = false;
				bool childHasObj = false;
				ICounter<string> feats = new ClassicCounter<string>();
				// 1. edge taken
				feats.IncrementCount(signature + "&edge:" + edgeRelTaken);
				feats.IncrementCount(signature + "&edge_type:" + edgeRelShort);
				// 2. last edge taken
				if (from.edge == null)
				{
					System.Diagnostics.Debug.Assert(to.edge == null || to.OriginalTree().GetRoots().Contains(to.edge.GetGovernor()));
					feats.IncrementCount(signature + "&at_root");
					feats.IncrementCount(signature + "&at_root&root_pos:" + to.OriginalTree().GetFirstRoot().Tag());
				}
				else
				{
					feats.IncrementCount(signature + "&not_root");
					string lastRelShort = from.edge.GetRelation().GetShortName();
					if (lastRelShort.Contains("_"))
					{
						lastRelShort = Sharpen.Runtime.Substring(lastRelShort, 0, lastRelShort.IndexOf('_'));
					}
					feats.IncrementCount(signature + "&last_edge:" + lastRelShort);
				}
				if (to.edge != null)
				{
					// 3. other edges at parent
					foreach (SemanticGraphEdge parentNeighbor in from.OriginalTree().OutgoingEdgeIterable(to.edge.GetGovernor()))
					{
						if (parentNeighbor != to.edge)
						{
							string parentNeighborRel = parentNeighbor.GetRelation().ToString();
							if (parentNeighborRel.Contains("subj"))
							{
								parentHasSubj = true;
							}
							if (parentNeighborRel.Contains("obj"))
							{
								parentHasObj = true;
							}
							// (add feature)
							feats.IncrementCount(signature + "&parent_neighbor:" + parentNeighborRel);
							feats.IncrementCount(signature + "&edge_type:" + edgeRelShort + "&parent_neighbor:" + parentNeighborRel);
						}
					}
					// 4. Other edges at child
					int childNeighborCount = 0;
					foreach (SemanticGraphEdge childNeighbor in from.OriginalTree().OutgoingEdgeIterable(to.edge.GetDependent()))
					{
						string childNeighborRel = childNeighbor.GetRelation().ToString();
						if (childNeighborRel.Contains("subj"))
						{
							childHasSubj = true;
						}
						if (childNeighborRel.Contains("obj"))
						{
							childHasObj = true;
						}
						childNeighborCount += 1;
						// (add feature)
						feats.IncrementCount(signature + "&child_neighbor:" + childNeighborRel);
						feats.IncrementCount(signature + "&edge_type:" + edgeRelShort + "&child_neighbor:" + childNeighborRel);
					}
					// 4.1 Number of other edges at child
					feats.IncrementCount(signature + "&child_neighbor_count:" + (childNeighborCount < 3 ? childNeighborCount : ">2"));
					feats.IncrementCount(signature + "&edge_type:" + edgeRelShort + "&child_neighbor_count:" + (childNeighborCount < 3 ? childNeighborCount : ">2"));
					// 5. Subject/Object stats
					feats.IncrementCount(signature + "&parent_neighbor_subj:" + parentHasSubj);
					feats.IncrementCount(signature + "&parent_neighbor_obj:" + parentHasObj);
					feats.IncrementCount(signature + "&child_neighbor_subj:" + childHasSubj);
					feats.IncrementCount(signature + "&child_neighbor_obj:" + childHasObj);
					// 6. POS tag info
					feats.IncrementCount(signature + "&parent_pos:" + to.edge.GetGovernor().Tag());
					feats.IncrementCount(signature + "&child_pos:" + to.edge.GetDependent().Tag());
					feats.IncrementCount(signature + "&pos_signature:" + to.edge.GetGovernor().Tag() + "_" + to.edge.GetDependent().Tag());
					feats.IncrementCount(signature + "&edge_type:" + edgeRelShort + "&pos_signature:" + to.edge.GetGovernor().Tag() + "_" + to.edge.GetDependent().Tag());
				}
				return feats;
			}
		}

		/// <summary>The default featurizer to use during training.</summary>
		public static readonly ClauseSplitterSearchProblem.IFeaturizer DefaultFeaturizer = new _IFeaturizer_920();
	}
}

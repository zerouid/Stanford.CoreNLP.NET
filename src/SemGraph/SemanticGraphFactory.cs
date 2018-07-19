using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph
{
	/// <summary>
	/// Refactoring of static makers of SemanticGraphs in order to simplify
	/// the SemanticGraph class.
	/// </summary>
	/// <author>rafferty</author>
	public class SemanticGraphFactory
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.SemanticGraphFactory));

		private SemanticGraphFactory()
		{
		}

		private const bool IncludePunctuationDependencies = false;

		public enum Mode
		{
			CollapsedTree,
			Collapsed,
			Ccprocessed,
			Basic,
			Enhanced,
			EnhancedPlusPlus
		}

		// just static factory methods
		/// <summary>Produces an Uncollapsed (basic) SemanticGraph.</summary>
		public static SemanticGraph GenerateUncollapsedDependencies(Tree tree)
		{
			return MakeFromTree(tree, SemanticGraphFactory.Mode.Basic, GrammaticalStructure.Extras.None);
		}

		/// <summary>Produces a Collapsed SemanticGraph.</summary>
		[System.ObsoleteAttribute(@"Use GenerateEnhancedDependencies(Edu.Stanford.Nlp.Trees.Tree) orGenerateEnhancedPlusPlusDependencies(Edu.Stanford.Nlp.Trees.Tree) instead.")]
		public static SemanticGraph GenerateCollapsedDependencies(Tree tree)
		{
			return MakeFromTree(tree, SemanticGraphFactory.Mode.Collapsed, GrammaticalStructure.Extras.None);
		}

		/// <summary>Produces a CCProcessed SemanticGraph.</summary>
		[System.ObsoleteAttribute(@"Use GenerateEnhancedDependencies(Edu.Stanford.Nlp.Trees.Tree) orGenerateEnhancedPlusPlusDependencies(Edu.Stanford.Nlp.Trees.Tree) instead.")]
		public static SemanticGraph GenerateCCProcessedDependencies(Tree tree)
		{
			return MakeFromTree(tree, SemanticGraphFactory.Mode.Ccprocessed, GrammaticalStructure.Extras.None);
		}

		/// <summary>Produces an enhanced dependencies SemanticGraph.</summary>
		public static SemanticGraph GenerateEnhancedDependencies(Tree tree)
		{
			return MakeFromTree(tree, SemanticGraphFactory.Mode.Enhanced, GrammaticalStructure.Extras.None);
		}

		/// <summary>Produces an enhanced++ dependencies SemanticGraph.</summary>
		public static SemanticGraph GenerateEnhancedPlusPlusDependencies(Tree tree)
		{
			return MakeFromTree(tree, SemanticGraphFactory.Mode.EnhancedPlusPlus, GrammaticalStructure.Extras.None);
		}

		/// <summary>Produces an Uncollapsed (basic) SemanticGraph.</summary>
		public static SemanticGraph GenerateUncollapsedDependencies(GrammaticalStructure gs)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Basic, GrammaticalStructure.Extras.None, null);
		}

		/// <summary>Produces a Collapsed SemanticGraph.</summary>
		[System.ObsoleteAttribute(@"Use GenerateEnhancedDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) orGenerateEnhancedPlusPlusDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) instead.")]
		public static SemanticGraph GenerateCollapsedDependencies(GrammaticalStructure gs)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Collapsed, GrammaticalStructure.Extras.None, null);
		}

		/// <summary>Produces a CCProcessed SemanticGraph.</summary>
		[System.ObsoleteAttribute(@"Use GenerateEnhancedDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) orGenerateEnhancedPlusPlusDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) instead.")]
		public static SemanticGraph GenerateCCProcessedDependencies(GrammaticalStructure gs)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Ccprocessed, GrammaticalStructure.Extras.None, null);
		}

		/// <summary>Produces an enhanced dependencies SemanticGraph.</summary>
		public static SemanticGraph GenerateEnhancedDependencies(GrammaticalStructure gs)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Enhanced, GrammaticalStructure.Extras.None, null);
		}

		/// <summary>Produces an enhanced++ dependencies SemanticGraph.</summary>
		public static SemanticGraph GenerateEnhancedPlusPlusDependencies(GrammaticalStructure gs)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.EnhancedPlusPlus, GrammaticalStructure.Extras.None, null);
		}

		/// <summary>Produces an Uncollapsed (basic) SemanticGraph.</summary>
		/// <remarks>
		/// Produces an Uncollapsed (basic) SemanticGraph.
		/// The extras parameter has no effect if gs is an instance of
		/// <see cref="Edu.Stanford.Nlp.Trees.UniversalEnglishGrammaticalStructure"/>
		/// .
		/// </remarks>
		[System.ObsoleteAttribute(@"Use GenerateUncollapsedDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) instead.")]
		public static SemanticGraph GenerateUncollapsedDependencies(GrammaticalStructure gs, GrammaticalStructure.Extras extras)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Basic, extras, null);
		}

		/// <summary>Produces a Collapsed SemanticGraph with optional extras.</summary>
		[System.ObsoleteAttribute(@"Use GenerateEnhancedDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) orGenerateEnhancedPlusPlusDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) instead.")]
		public static SemanticGraph GenerateCollapsedDependencies(GrammaticalStructure gs, GrammaticalStructure.Extras extras)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Collapsed, extras, null);
		}

		/// <summary>Produces a CCProcessed SemanticGraph with optional extras.</summary>
		[System.ObsoleteAttribute(@"Use GenerateEnhancedDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) orGenerateEnhancedPlusPlusDependencies(Edu.Stanford.Nlp.Trees.GrammaticalStructure) instead.")]
		public static SemanticGraph GenerateCCProcessedDependencies(GrammaticalStructure gs, GrammaticalStructure.Extras extras)
		{
			return MakeFromTree(gs, SemanticGraphFactory.Mode.Ccprocessed, extras, null);
		}

		public static SemanticGraph MakeFromTree(Tree tree, SemanticGraphFactory.Mode mode, GrammaticalStructure.Extras includeExtras, IPredicate<TypedDependency> filter, bool originalDependencies)
		{
			return MakeFromTree(tree, mode, includeExtras, filter, originalDependencies, IncludePunctuationDependencies);
		}

		/// <summary>
		/// Returns a new
		/// <c>SemanticGraph</c>
		/// constructed from a given
		/// <see cref="Edu.Stanford.Nlp.Trees.Tree"/>
		/// with given options.
		/// This factory method is intended to replace a profusion of highly similar
		/// factory methods, such as
		/// <c>typedDependencies()</c>
		/// ,
		/// <c>typedDependenciesCollapsed()</c>
		/// ,
		/// <c>allTypedDependencies()</c>
		/// ,
		/// <c>allTypedDependenciesCollapsed()</c>
		/// , etc.
		/// For a fuller explanation of the meaning of the boolean arguments, see
		/// <see cref="Edu.Stanford.Nlp.Trees.GrammaticalStructure"/>
		/// .
		/// </summary>
		/// <param name="tree">A tree representing a phrase structure parse</param>
		/// <param name="includeExtras">
		/// Whether to include extra dependencies, which may
		/// result in a non-tree
		/// </param>
		/// <param name="filter">A filter to exclude certain dependencies; ignored if null</param>
		/// <param name="originalDependencies">
		/// generate original Stanford dependencies instead of new
		/// Universal Dependencies
		/// </param>
		/// <returns>A SemanticGraph</returns>
		public static SemanticGraph MakeFromTree(Tree tree, SemanticGraphFactory.Mode mode, GrammaticalStructure.Extras includeExtras, IPredicate<TypedDependency> filter, bool originalDependencies, bool includePunctuationDependencies)
		{
			GrammaticalStructure gs;
			if (originalDependencies)
			{
				IPredicate<string> wordFilt;
				if (includePunctuationDependencies)
				{
					wordFilt = Filters.AcceptFilter();
				}
				else
				{
					wordFilt = new PennTreebankLanguagePack().PunctuationWordRejectFilter();
				}
				gs = new EnglishGrammaticalStructure(tree, wordFilt, new SemanticHeadFinder(true));
			}
			else
			{
				IPredicate<string> tagFilt;
				if (includePunctuationDependencies)
				{
					tagFilt = Filters.AcceptFilter();
				}
				else
				{
					tagFilt = new PennTreebankLanguagePack().PunctuationTagRejectFilter();
				}
				gs = new UniversalEnglishGrammaticalStructure(tree, tagFilt, new UniversalSemanticHeadFinder(true));
			}
			return MakeFromTree(gs, mode, includeExtras, filter);
		}

		// TODO: these booleans would be more readable as enums similar to Mode.
		// Then the arguments would make more sense
		public static SemanticGraph MakeFromTree(GrammaticalStructure gs, SemanticGraphFactory.Mode mode, GrammaticalStructure.Extras includeExtras, IPredicate<TypedDependency> filter)
		{
			ICollection<TypedDependency> deps;
			switch (mode)
			{
				case SemanticGraphFactory.Mode.Enhanced:
				{
					deps = gs.TypedDependenciesEnhanced();
					break;
				}

				case SemanticGraphFactory.Mode.EnhancedPlusPlus:
				{
					deps = gs.TypedDependenciesEnhancedPlusPlus();
					break;
				}

				case SemanticGraphFactory.Mode.CollapsedTree:
				{
					deps = gs.TypedDependenciesCollapsedTree();
					break;
				}

				case SemanticGraphFactory.Mode.Collapsed:
				{
					deps = gs.TypedDependenciesCollapsed(includeExtras);
					break;
				}

				case SemanticGraphFactory.Mode.Ccprocessed:
				{
					deps = gs.TypedDependenciesCCprocessed(includeExtras);
					break;
				}

				case SemanticGraphFactory.Mode.Basic:
				{
					deps = gs.TypedDependencies(includeExtras);
					break;
				}

				default:
				{
					throw new ArgumentException("Unknown mode " + mode);
				}
			}
			if (filter != null)
			{
				IList<TypedDependency> depsFiltered = Generics.NewArrayList();
				foreach (TypedDependency td in deps)
				{
					if (filter.Test(td))
					{
						depsFiltered.Add(td);
					}
				}
				deps = depsFiltered;
			}
			// there used to be an if clause that filtered out the case of empty
			// dependencies. However, I could not understand (or replicate) the error
			// it alluded to, and it led to empty dependency graphs for very short fragments,
			// which meant they were ignored by the RTE system. Changed. (pado)
			// See also the SemanticGraph constructor.
			//log.info(deps.toString());
			return new SemanticGraph(deps);
		}

		[Obsolete]
		public static SemanticGraph MakeFromTree(GrammaticalStructure tree, SemanticGraphFactory.Mode mode, bool includeExtras, IPredicate<TypedDependency> filter)
		{
			return MakeFromTree(tree, mode, includeExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None, filter);
		}

		public static SemanticGraph MakeFromTree(GrammaticalStructure structure)
		{
			return MakeFromTree(structure, SemanticGraphFactory.Mode.Basic, GrammaticalStructure.Extras.None, null);
		}

		public static SemanticGraph MakeFromTree(Tree tree, SemanticGraphFactory.Mode mode, GrammaticalStructure.Extras includeExtras, IPredicate<TypedDependency> filter)
		{
			return MakeFromTree(tree, mode, includeExtras, filter, false);
		}

		[Obsolete]
		public static SemanticGraph MakeFromTree(Tree tree, SemanticGraphFactory.Mode mode, bool includeExtras, IPredicate<TypedDependency> filter)
		{
			return MakeFromTree(tree, mode, includeExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None, filter, false);
		}

		public static SemanticGraph MakeFromTree(Tree tree, SemanticGraphFactory.Mode mode, GrammaticalStructure.Extras includeExtras)
		{
			return MakeFromTree(tree, mode, includeExtras, null, false);
		}

		/// <seealso cref="MakeFromTree(Edu.Stanford.Nlp.Trees.Tree, Mode, Edu.Stanford.Nlp.Trees.GrammaticalStructure.Extras)"/>
		[Obsolete]
		public static SemanticGraph MakeFromTree(Tree tree, SemanticGraphFactory.Mode mode, bool includeExtras)
		{
			return MakeFromTree(tree, mode, includeExtras ? GrammaticalStructure.Extras.Maximal : GrammaticalStructure.Extras.None);
		}

		/// <summary>Given a list of edges, attempts to create and return a rooted SemanticGraph.</summary>
		/// <remarks>
		/// Given a list of edges, attempts to create and return a rooted SemanticGraph.
		/// TODO: throw Exceptions, or flag warnings on conditions for concern (no root, etc)
		/// </remarks>
		public static SemanticGraph MakeFromEdges(IEnumerable<SemanticGraphEdge> edges)
		{
			// Identify the root(s) of this graph
			SemanticGraph sg = new SemanticGraph();
			ICollection<IndexedWord> vertices = GetVerticesFromEdgeSet(edges);
			foreach (IndexedWord vertex in vertices)
			{
				sg.AddVertex(vertex);
			}
			foreach (SemanticGraphEdge edge in edges)
			{
				sg.AddEdge(edge.GetSource(), edge.GetTarget(), edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			}
			sg.ResetRoots();
			return sg;
		}

		/// <summary>Given an iterable set of edges, returns the set of  vertices covered by these edges.</summary>
		/// <remarks>
		/// Given an iterable set of edges, returns the set of  vertices covered by these edges.
		/// Note: CDM changed the return of this from a List to a Set in 2011. This seemed more
		/// sensible.  Hopefully it doesn't break anything....
		/// </remarks>
		private static ICollection<IndexedWord> GetVerticesFromEdgeSet(IEnumerable<SemanticGraphEdge> edges)
		{
			ICollection<IndexedWord> retSet = Generics.NewHashSet();
			foreach (SemanticGraphEdge edge in edges)
			{
				retSet.Add(edge.GetGovernor());
				retSet.Add(edge.GetDependent());
			}
			return retSet;
		}

		/// <summary>
		/// Given a set of vertices, and the source graph they are drawn from, create a path composed
		/// of the minimum paths between the vertices.
		/// </summary>
		/// <remarks>
		/// Given a set of vertices, and the source graph they are drawn from, create a path composed
		/// of the minimum paths between the vertices.  i.e. this is a simple brain-dead attempt at getting
		/// something approximating a minimum spanning graph.
		/// NOTE: the hope is the vertices will already be contiguous, but facilities are added just in case for
		/// adding additional nodes.
		/// </remarks>
		public static SemanticGraph MakeFromVertices(SemanticGraph sg, ICollection<IndexedWord> nodes)
		{
			IList<SemanticGraphEdge> edgesToAdd = new List<SemanticGraphEdge>();
			IList<IndexedWord> nodesToAdd = new List<IndexedWord>(nodes);
			foreach (IndexedWord nodeA in nodes)
			{
				foreach (IndexedWord nodeB in nodes)
				{
					if (nodeA != nodeB)
					{
						IList<SemanticGraphEdge> edges = sg.GetShortestDirectedPathEdges(nodeA, nodeB);
						if (edges != null)
						{
							Sharpen.Collections.AddAll(edgesToAdd, edges);
							foreach (SemanticGraphEdge edge in edges)
							{
								IndexedWord gov = edge.GetGovernor();
								IndexedWord dep = edge.GetDependent();
								if (gov != null && !nodesToAdd.Contains(gov))
								{
									nodesToAdd.Add(gov);
								}
								if (dep != null && !nodesToAdd.Contains(dep))
								{
									nodesToAdd.Add(dep);
								}
							}
						}
					}
				}
			}
			SemanticGraph retSg = new SemanticGraph();
			foreach (IndexedWord node in nodesToAdd)
			{
				retSg.AddVertex(node);
			}
			foreach (SemanticGraphEdge edge_1 in edgesToAdd)
			{
				retSg.AddEdge(edge_1.GetGovernor(), edge_1.GetDependent(), edge_1.GetRelation(), edge_1.GetWeight(), edge_1.IsExtra());
			}
			retSg.ResetRoots();
			return retSg;
		}

		/// <summary>This creates a new graph based off the given, but uses the existing nodes objects.</summary>
		public static SemanticGraph DuplicateKeepNodes(SemanticGraph sg)
		{
			SemanticGraph retSg = new SemanticGraph();
			foreach (IndexedWord node in sg.VertexSet())
			{
				retSg.AddVertex(node);
			}
			retSg.SetRoots(sg.GetRoots());
			foreach (SemanticGraphEdge edge in sg.EdgeIterable())
			{
				retSg.AddEdge(edge.GetGovernor(), edge.GetDependent(), edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
			}
			return retSg;
		}

		/// <summary>
		/// Given a list of graphs, constructs a new graph combined from the
		/// collection of graphs.
		/// </summary>
		/// <remarks>
		/// Given a list of graphs, constructs a new graph combined from the
		/// collection of graphs.  Original vertices are used, edges are
		/// copied.  Graphs are ordered by the sentence index and index of
		/// the original vertices.  Intent is to create a "mega graph"
		/// similar to the graphs used in the RTE problem.
		/// <br />
		/// This method only works if the indexed words have different
		/// sentence ids, as otherwise the maps used will confuse several of
		/// the IndexedWords.
		/// </remarks>
		public static SemanticGraph MakeFromGraphs(ICollection<SemanticGraph> sgList)
		{
			SemanticGraph sg = new SemanticGraph();
			ICollection<IndexedWord> newRoots = Generics.NewHashSet();
			foreach (SemanticGraph currSg in sgList)
			{
				Sharpen.Collections.AddAll(newRoots, currSg.GetRoots());
				foreach (IndexedWord currVertex in currSg.VertexSet())
				{
					sg.AddVertex(currVertex);
				}
				foreach (SemanticGraphEdge currEdge in currSg.EdgeIterable())
				{
					sg.AddEdge(currEdge.GetGovernor(), currEdge.GetDependent(), currEdge.GetRelation(), currEdge.GetWeight(), currEdge.IsExtra());
				}
			}
			sg.SetRoots(newRoots);
			return sg;
		}

		/// <summary>
		/// Like makeFromGraphs, but it makes a deep copy of the graphs and
		/// renumbers the index words.
		/// </summary>
		/// <remarks>
		/// Like makeFromGraphs, but it makes a deep copy of the graphs and
		/// renumbers the index words.
		/// <br />
		/// <paramref name="lengths"/>
		/// must be a vector containing the number of
		/// tokens in each sentence.  This is used to reindex the tokens.
		/// </remarks>
		public static SemanticGraph DeepCopyFromGraphs(IList<SemanticGraph> graphs, IList<int> lengths)
		{
			SemanticGraph newGraph = new SemanticGraph();
			IDictionary<int, IndexedWord> newWords = Generics.NewHashMap();
			IList<IndexedWord> newRoots = new List<IndexedWord>();
			int vertexOffset = 0;
			for (int i = 0; i < graphs.Count; ++i)
			{
				SemanticGraph graph = graphs[i];
				foreach (IndexedWord vertex in graph.VertexSet())
				{
					IndexedWord newVertex = new IndexedWord(vertex);
					newVertex.SetIndex(vertex.Index() + vertexOffset);
					newGraph.AddVertex(newVertex);
					newWords[newVertex.Index()] = newVertex;
				}
				foreach (SemanticGraphEdge edge in graph.EdgeIterable())
				{
					IndexedWord gov = newWords[edge.GetGovernor().Index() + vertexOffset];
					IndexedWord dep = newWords[edge.GetDependent().Index() + vertexOffset];
					if (gov == null || dep == null)
					{
						throw new AssertionError("Counting problem (or broken edge)");
					}
					newGraph.AddEdge(gov, dep, edge.GetRelation(), edge.GetWeight(), edge.IsExtra());
				}
				foreach (IndexedWord root in graph.GetRoots())
				{
					newRoots.Add(newWords[root.Index() + vertexOffset]);
				}
				vertexOffset += lengths[i];
			}
			newGraph.SetRoots(newRoots);
			return newGraph;
		}
	}
}

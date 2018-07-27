using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Pipeline
{
	/// <author>David McClosky</author>
	public class ParserAnnotatorUtils
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.ParserAnnotatorUtils));

		private ParserAnnotatorUtils()
		{
		}

		// static methods
		/// <summary>
		/// Put the tree in the CoreMap for the sentence, also add any
		/// dependency graphs to the sentence, and fill in missing tag annotations.
		/// </summary>
		/// <remarks>
		/// Put the tree in the CoreMap for the sentence, also add any
		/// dependency graphs to the sentence, and fill in missing tag annotations.
		/// Thread safety note: nothing special is done to ensure the thread
		/// safety of the GrammaticalStructureFactory.  However, both the
		/// EnglishGrammaticalStructureFactory and the
		/// ChineseGrammaticalStructureFactory are thread safe.
		/// </remarks>
		public static void FillInParseAnnotations(bool verbose, bool buildGraphs, IGrammaticalStructureFactory gsf, ICoreMap sentence, IList<Tree> trees, GrammaticalStructure.Extras extras)
		{
			bool first = true;
			foreach (Tree tree in trees)
			{
				// make sure all tree nodes are CoreLabels
				// TODO: why isn't this always true? something fishy is going on
				Edu.Stanford.Nlp.Trees.Trees.ConvertToCoreLabels(tree);
				// index nodes, i.e., add start and end token positions to all nodes
				// this is needed by other annotators down stream, e.g., the NFLAnnotator
				tree.IndexSpans(0);
				if (first)
				{
					sentence.Set(typeof(TreeCoreAnnotations.TreeAnnotation), tree);
					if (verbose)
					{
						log.Info("Tree is:");
						tree.PennPrint(System.Console.Error);
					}
					SetMissingTags(sentence, tree);
					if (buildGraphs)
					{
						// generate the dependency graph
						// unfortunately, it is necessary to make the
						// GrammaticalStructure three times, as the dependency
						// conversion changes the given data structure
						SemanticGraph deps = SemanticGraphFactory.GenerateCollapsedDependencies(gsf.NewGrammaticalStructure(tree), extras);
						SemanticGraph uncollapsedDeps = SemanticGraphFactory.GenerateUncollapsedDependencies(gsf.NewGrammaticalStructure(tree), extras);
						SemanticGraph ccDeps = SemanticGraphFactory.GenerateCCProcessedDependencies(gsf.NewGrammaticalStructure(tree), extras);
						SemanticGraph enhancedDeps = SemanticGraphFactory.GenerateEnhancedDependencies(gsf.NewGrammaticalStructure(tree));
						SemanticGraph enhancedPlusPlusDeps = SemanticGraphFactory.GenerateEnhancedPlusPlusDependencies(gsf.NewGrammaticalStructure(tree));
						if (verbose)
						{
							log.Info("SDs:");
							log.Info(deps.ToString(SemanticGraph.OutputFormat.List));
						}
						sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation), deps);
						sentence.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), uncollapsedDeps);
						sentence.Set(typeof(SemanticGraphCoreAnnotations.CollapsedCCProcessedDependenciesAnnotation), ccDeps);
						sentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), enhancedDeps);
						sentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation), enhancedPlusPlusDeps);
					}
					first = false;
				}
			}
			if (trees.Count > 1)
			{
				sentence.Set(typeof(TreeCoreAnnotations.KBestTreesAnnotation), trees);
			}
		}

		/// <summary>
		/// Set the tags of the original tokens and the leaves if they
		/// aren't already set.
		/// </summary>
		private static void SetMissingTags(ICoreMap sentence, Tree tree)
		{
			IList<TaggedWord> taggedWords = null;
			IList<ILabel> leaves = null;
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i = 0; i < size; ++i)
			{
				CoreLabel token = tokens[i];
				if (token.Tag() == null)
				{
					if (taggedWords == null)
					{
						taggedWords = tree.TaggedYield();
					}
					if (leaves == null)
					{
						leaves = tree.Yield();
					}
					token.SetTag(taggedWords[i].Tag());
					ILabel leaf = leaves[i];
					if (leaf is IHasTag)
					{
						((IHasTag)leaf).SetTag(taggedWords[i].Tag());
					}
				}
			}
		}
	}
}

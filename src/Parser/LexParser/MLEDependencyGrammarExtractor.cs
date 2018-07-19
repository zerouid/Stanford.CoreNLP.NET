using System.Collections.Generic;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Gathers statistics on tree dependencies and then passes them to an
	/// MLEDependencyGrammar for dependency grammar construction.
	/// </summary>
	/// <author>Dan Klein</author>
	public class MLEDependencyGrammarExtractor : AbstractTreeExtractor<IDependencyGrammar>
	{
		protected internal readonly IIndex<string> wordIndex;

		protected internal readonly IIndex<string> tagIndex;

		/// <summary>This is where all dependencies are stored (using full tag space).</summary>
		protected internal ClassicCounter<IntDependency> dependencyCounter = new ClassicCounter<IntDependency>();

		protected internal ITreebankLangParserParams tlpParams;

		/// <summary>Whether left and right is distinguished.</summary>
		protected internal bool directional;

		/// <summary>Whether dependent distance from head is distinguished.</summary>
		protected internal bool useDistance;

		/// <summary>Whether dependent distance is distinguished more coarsely.</summary>
		protected internal bool useCoarseDistance;

		/// <summary>Whether basic category tags are in the dependency grammar.</summary>
		protected internal readonly bool basicCategoryTagsInDependencyGrammar;

		public MLEDependencyGrammarExtractor(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
			: base(op)
		{
			//private Set dependencies = new HashSet();
			this.wordIndex = wordIndex;
			this.tagIndex = tagIndex;
			tlpParams = op.tlpParams;
			directional = op.directional;
			useDistance = op.distance;
			useCoarseDistance = op.coarseDistance;
			basicCategoryTagsInDependencyGrammar = op.trainOptions.basicCategoryTagsInDependencyGrammar;
		}

		protected internal override void TallyRoot(Tree lt, double weight)
		{
			// this list is in full (not reduced) tag space
			IList<IntDependency> deps = MLEDependencyGrammar.TreeToDependencyList(lt, wordIndex, tagIndex);
			foreach (IntDependency dependency in deps)
			{
				dependencyCounter.IncrementCount(dependency, weight);
			}
		}

		public override IDependencyGrammar FormResult()
		{
			wordIndex.AddToIndex(LexiconConstants.UnknownWord);
			MLEDependencyGrammar dg = new MLEDependencyGrammar(tlpParams, directional, useDistance, useCoarseDistance, basicCategoryTagsInDependencyGrammar, op, wordIndex, tagIndex);
			foreach (IntDependency dependency in dependencyCounter.KeySet())
			{
				dg.AddRule(dependency, dependencyCounter.GetCount(dependency));
			}
			return dg;
		}
	}
}

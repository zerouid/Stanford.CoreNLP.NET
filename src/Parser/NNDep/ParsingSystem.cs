using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Nndep
{
	/// <summary>Defines a transition-based parsing framework for dependency parsing.</summary>
	/// <author>Danqi Chen</author>
	public abstract class ParsingSystem
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Nndep.ParsingSystem));

		/// <summary>Defines language-specific settings for this parsing instance.</summary>
		private readonly ITreebankLanguagePack tlp;

		/// <summary>Dependency label used between root of sentence and ROOT node.</summary>
		protected internal readonly string rootLabel;

		protected internal readonly IList<string> labels;

		protected internal readonly IList<string> transitions;

		/// <summary>
		/// Determine whether the given transition is legal for this
		/// configuration.
		/// </summary>
		/// <param name="c">Parsing configuration</param>
		/// <param name="t">Transition string</param>
		/// <returns>
		/// Whether the given transition is legal in this
		/// configuration
		/// </returns>
		public abstract bool CanApply(Configuration c, string t);

		/// <summary>
		/// Apply the given transition to the given configuration, modifying
		/// the configuration's state in place.
		/// </summary>
		public abstract void Apply(Configuration c, string t);

		/// <summary>
		/// Provide a static-oracle recommendation for the next parsing step
		/// to take.
		/// </summary>
		/// <param name="c">Current parser configuration</param>
		/// <param name="dTree">Gold tree which parser needs to reach</param>
		/// <returns>Transition string</returns>
		public abstract string GetOracle(Configuration c, DependencyTree dTree);

		/// <summary>
		/// Determine whether applying the given transition in the given
		/// configuration tree will leave in us a state in which we can reach
		/// the gold tree.
		/// </summary>
		/// <remarks>
		/// Determine whether applying the given transition in the given
		/// configuration tree will leave in us a state in which we can reach
		/// the gold tree. (Useful for building a dynamic oracle.)
		/// </remarks>
		internal abstract bool IsOracle(Configuration c, string t, DependencyTree dTree);

		/// <summary>Build an initial parser configuration from the given sentence.</summary>
		public abstract Configuration InitialConfiguration(ICoreMap sentence);

		/// <summary>
		/// Determine if the given configuration corresponds to a parser which
		/// has completed its parse.
		/// </summary>
		internal abstract bool IsTerminal(Configuration c);

		/// <summary>Return the number of transitions.</summary>
		public virtual int NumTransitions()
		{
			return transitions.Count;
		}

		/// <param name="tlp">
		/// TreebankLanguagePack describing the language being
		/// parsed
		/// </param>
		/// <param name="labels">
		/// A list of possible dependency relation labels, with
		/// the ROOT relation label as the first element
		/// </param>
		public ParsingSystem(ITreebankLanguagePack tlp, IList<string> labels, IList<string> transitions, bool verbose)
		{
			// TODO pass labels as Map<String, GrammaticalRelation>; use GrammaticalRelation throughout
			this.tlp = tlp;
			this.labels = new List<string>(labels);
			//NOTE: assume that the first element of labels is rootLabel
			rootLabel = labels[0];
			this.transitions = transitions;
			if (verbose)
			{
				log.Info(Config.Separator);
				log.Info("#Transitions: " + NumTransitions());
				log.Info("#Labels: " + labels.Count);
				log.Info("ROOTLABEL: " + rootLabel);
			}
		}

		public virtual int GetTransitionID(string s)
		{
			int numTrans = NumTransitions();
			for (int k = 0; k < numTrans; ++k)
			{
				if (transitions[k].Equals(s))
				{
					return k;
				}
			}
			return -1;
		}

		private ICollection<string> GetPunctuationTags()
		{
			if (tlp is PennTreebankLanguagePack)
			{
				// Hack for English: match punctuation tags used in Danqi's paper
				return new HashSet<string>(Arrays.AsList("''", ",", ".", ":", "``", "-LRB-", "-RRB-"));
			}
			else
			{
				return CollectionUtils.AsSet(tlp.PunctuationTags());
			}
		}

		/// <summary>
		/// Evaluate performance on a list of sentences, predicted parses,
		/// and gold parses.
		/// </summary>
		/// <returns>A map from metric name to metric value</returns>
		public virtual IDictionary<string, double> Evaluate(IList<ICoreMap> sentences, IList<DependencyTree> trees, IList<DependencyTree> goldTrees)
		{
			IDictionary<string, double> result = new Dictionary<string, double>();
			// We'll skip words which are punctuation. Retrieve tags indicating
			// punctuation in this treebank.
			ICollection<string> punctuationTags = GetPunctuationTags();
			if (trees.Count != goldTrees.Count)
			{
				log.Error("Incorrect number of trees.");
				return null;
			}
			int correctArcs = 0;
			int correctArcsNoPunc = 0;
			int correctHeads = 0;
			int correctHeadsNoPunc = 0;
			int correctTrees = 0;
			int correctTreesNoPunc = 0;
			int correctRoot = 0;
			int sumArcs = 0;
			int sumArcsNoPunc = 0;
			for (int i = 0; i < trees.Count; ++i)
			{
				IList<CoreLabel> tokens = sentences[i].Get(typeof(CoreAnnotations.TokensAnnotation));
				if (trees[i].n != goldTrees[i].n)
				{
					log.Error("Tree " + (i + 1) + ": incorrect number of nodes.");
					return null;
				}
				if (!trees[i].IsTree())
				{
					log.Error("Tree " + (i + 1) + ": illegal.");
					return null;
				}
				int nCorrectHead = 0;
				int nCorrectHeadNoPunc = 0;
				int nNoPunc = 0;
				for (int j = 1; j <= trees[i].n; ++j)
				{
					if (trees[i].GetHead(j) == goldTrees[i].GetHead(j))
					{
						++correctHeads;
						++nCorrectHead;
						if (trees[i].GetLabel(j).Equals(goldTrees[i].GetLabel(j)))
						{
							++correctArcs;
						}
					}
					++sumArcs;
					string tag = tokens[j - 1].Tag();
					if (!punctuationTags.Contains(tag))
					{
						++sumArcsNoPunc;
						++nNoPunc;
						if (trees[i].GetHead(j) == goldTrees[i].GetHead(j))
						{
							++correctHeadsNoPunc;
							++nCorrectHeadNoPunc;
							if (trees[i].GetLabel(j).Equals(goldTrees[i].GetLabel(j)))
							{
								++correctArcsNoPunc;
							}
						}
					}
				}
				if (nCorrectHead == trees[i].n)
				{
					++correctTrees;
				}
				if (nCorrectHeadNoPunc == nNoPunc)
				{
					++correctTreesNoPunc;
				}
				if (trees[i].GetRoot() == goldTrees[i].GetRoot())
				{
					++correctRoot;
				}
			}
			result["UAS"] = correctHeads * 100.0 / sumArcs;
			result["UASnoPunc"] = correctHeadsNoPunc * 100.0 / sumArcsNoPunc;
			result["LAS"] = correctArcs * 100.0 / sumArcs;
			result["LASnoPunc"] = correctArcsNoPunc * 100.0 / sumArcsNoPunc;
			result["UEM"] = correctTrees * 100.0 / trees.Count;
			result["UEMnoPunc"] = correctTreesNoPunc * 100.0 / trees.Count;
			result["ROOT"] = correctRoot * 100.0 / trees.Count;
			return result;
		}

		public virtual double GetUAS(IList<ICoreMap> sentences, IList<DependencyTree> trees, IList<DependencyTree> goldTrees)
		{
			IDictionary<string, double> result = Evaluate(sentences, trees, goldTrees);
			return result == null || !result.Contains("UAS") ? -1.0 : result["UAS"];
		}

		public virtual double GetUASnoPunc(IList<ICoreMap> sentences, IList<DependencyTree> trees, IList<DependencyTree> goldTrees)
		{
			IDictionary<string, double> result = Evaluate(sentences, trees, goldTrees);
			return result == null || !result.Contains("UASnoPunc") ? -1.0 : result["UASnoPunc"];
		}
	}
}

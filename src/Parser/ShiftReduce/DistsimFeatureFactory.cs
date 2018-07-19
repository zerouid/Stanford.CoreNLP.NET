using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Featurizes words based only on their distributional similarity classes.</summary>
	/// <remarks>
	/// Featurizes words based only on their distributional similarity classes.
	/// Borrows the Distsim class from the tagger.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class DistsimFeatureFactory : FeatureFactory
	{
		private readonly Distsim distsim;

		internal DistsimFeatureFactory()
		{
			throw new NotSupportedException("Illegal construction of DistsimFeatureFactory.  It must be created with a path to a cluster file");
		}

		internal DistsimFeatureFactory(string path)
		{
			distsim = Distsim.InitLexicon(path);
		}

		public virtual void AddDistsimFeatures(IList<string> features, CoreLabel label, string featureName)
		{
			if (label == null)
			{
				return;
			}
			string word = GetFeatureFromCoreLabel(label, FeatureFactory.FeatureComponent.Headword);
			string tag = GetFeatureFromCoreLabel(label, FeatureFactory.FeatureComponent.Headtag);
			string cluster = distsim.GetMapping(word);
			features.Add(featureName + "dis-" + cluster);
			features.Add(featureName + "disT-" + cluster + "-" + tag);
		}

		public override IList<string> Featurize(State state, IList<string> features)
		{
			CoreLabel s0Label = GetStackLabel(state.stack, 0);
			// current top of stack
			CoreLabel s1Label = GetStackLabel(state.stack, 1);
			// one previous
			CoreLabel q0Label = GetQueueLabel(state.sentence, state.tokenPosition, 0);
			// current location in queue
			AddDistsimFeatures(features, s0Label, "S0");
			AddDistsimFeatures(features, s1Label, "S1");
			AddDistsimFeatures(features, q0Label, "Q0");
			return features;
		}

		private const long serialVersionUID = -396152777907151063L;
	}
}

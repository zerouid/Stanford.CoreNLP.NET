using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>Represents all coreference examples for a particular document.</summary>
	/// <remarks>
	/// Represents all coreference examples for a particular document. Individual mention features are
	/// stored separately from pairwise features to save memory.
	/// </remarks>
	/// <author>Kevin Clark</author>
	[System.Serializable]
	public class DocumentExamples
	{
		private const long serialVersionUID = -2474306699767791493L;

		public readonly int id;

		public IList<Example> examples;

		public readonly IDictionary<int, CompressedFeatureVector> mentionFeatures;

		public DocumentExamples(int id, IList<Example> examples, IDictionary<int, CompressedFeatureVector> mentionFeatures)
		{
			this.id = id;
			this.examples = examples;
			this.mentionFeatures = mentionFeatures;
		}
	}
}

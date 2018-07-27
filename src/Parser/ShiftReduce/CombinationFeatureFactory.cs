using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Parser.Shiftreduce
{
	/// <summary>Combines multiple feature factories into one feature factory</summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class CombinationFeatureFactory : FeatureFactory
	{
		internal FeatureFactory[] factories;

		public CombinationFeatureFactory(FeatureFactory[] factories)
		{
			this.factories = factories;
		}

		public override IList<string> Featurize(State state, IList<string> features)
		{
			foreach (FeatureFactory factory in factories)
			{
				factory.Featurize(state, features);
			}
			return features;
		}

		private const long serialVersionUID = 1;
	}
}

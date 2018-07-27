using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>Feature factory for SimpleTagger of Mallet.</summary>
	/// <author>Michel Galley</author>
	[System.Serializable]
	public class MalletFeatureFactory<In> : FeatureFactory<In>
		where In : CoreLabel
	{
		private const long serialVersionUID = -5586998916869425417L;

		public override ICollection<string> GetCliqueFeatures(PaddedList<In> info, int position, Clique clique)
		{
			IList<string> features = new List<string>(Arrays.AsList(info[position].Word().Split(" ")));
			return features;
		}
	}
}

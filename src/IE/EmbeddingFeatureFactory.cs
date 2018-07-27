using System.Collections;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE
{
	/// <summary>For features generated from word embeddings.</summary>
	/// <author>Thang Luong, created on Sep 11, 2013: minor enhancements.</author>
	/// <author>Mengqiu Wang: original developer.</author>
	[System.Serializable]
	public class EmbeddingFeatureFactory : FeatureFactory
	{
		/* (non-Javadoc)
		* @see edu.stanford.nlp.sequences.FeatureFactory#getCliqueFeatures(edu.stanford.nlp.util.PaddedList, int, edu.stanford.nlp.sequences.Clique)
		*/
		public override ICollection GetCliqueFeatures(PaddedList info, int position, Clique clique)
		{
			// TODO Auto-generated method stub
			return null;
		}
	}
}

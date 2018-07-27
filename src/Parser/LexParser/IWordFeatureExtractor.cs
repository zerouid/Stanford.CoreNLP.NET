using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>An interface for getting features out of words for a feature-based lexicon.</summary>
	/// <author>Galen Andrew</author>
	public interface IWordFeatureExtractor
	{
		void SetFeatureLevel(int level);

		ICollection<string> MakeFeatures(string word);

		void ApplyFeatureCountThreshold(ICollection<string> data, int thresh);
	}
}

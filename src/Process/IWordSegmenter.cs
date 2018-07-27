using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;


namespace Edu.Stanford.Nlp.Process
{
	/// <summary>
	/// An interface for segmenting strings into words
	/// (in unwordsegmented languages).
	/// </summary>
	/// <author>Galen Andrew</author>
	public interface IWordSegmenter
	{
		void InitializeTraining(double numTrees);

		void Train(ICollection<Tree> trees);

		void Train(Tree trees);

		void Train(IList<TaggedWord> sentence);

		void FinishTraining();

		void LoadSegmenter(string filename);

		IList<IHasWord> Segment(string s);
	}
}

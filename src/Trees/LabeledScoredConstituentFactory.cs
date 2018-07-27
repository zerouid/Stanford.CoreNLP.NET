using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>LabeledScoredConstituentFactory</code> acts as a factory for
	/// creating objects of class <code>LabeledScoredConstituent</code>.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class LabeledScoredConstituentFactory : IConstituentFactory
	{
		public virtual Constituent NewConstituent(int start, int end)
		{
			return new LabeledScoredConstituent(start, end);
		}

		public virtual Constituent NewConstituent(int start, int end, ILabel label, double score)
		{
			return new LabeledScoredConstituent(start, end, label, score);
		}
	}
}

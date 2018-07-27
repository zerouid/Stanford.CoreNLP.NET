using Edu.Stanford.Nlp.Stats;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An EqualityChecker for WordCatConstituents.</summary>
	/// <remarks>
	/// An EqualityChecker for WordCatConstituents.
	/// Words only have to have the correct span
	/// while tags (word/POS) and cats (labeled brackets) must have correct span
	/// and label.
	/// </remarks>
	/// <author>Galen Andrew</author>
	public class WordCatEqualityChecker : EquivalenceClassEval.IEqualityChecker
	{
		public virtual bool AreEqual(object o, object o2)
		{
			WordCatConstituent span = (WordCatConstituent)o;
			WordCatConstituent span2 = (WordCatConstituent)o2;
			if (span.type != span2.type)
			{
				return false;
			}
			else
			{
				if (span.Start() != span2.Start() || span.End() != span2.End())
				{
					return false;
				}
				else
				{
					if (span.type != WordCatConstituent.wordType && !span.Value().Equals(span2.Value()))
					{
						return false;
					}
					else
					{
						return true;
					}
				}
			}
		}
	}
}

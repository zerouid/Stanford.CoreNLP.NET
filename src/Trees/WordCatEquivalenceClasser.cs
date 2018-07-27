using Edu.Stanford.Nlp.Stats;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An EquivalenceClasser for WordCatConstituents.</summary>
	/// <remarks>
	/// An EquivalenceClasser for WordCatConstituents.  WCCs are equivalent iff
	/// they are of the same type (word, cat, tag).
	/// </remarks>
	/// <author>Galen Andrew</author>
	public class WordCatEquivalenceClasser : IEquivalenceClasser
	{
		public virtual object EquivalenceClass(object o)
		{
			WordCatConstituent lb = (WordCatConstituent)o;
			return lb.type;
		}
	}
}

using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>ConstituentFactory</code> acts as a factory for creating objects
	/// of class <code>Constituent</code>, or some descendent class.
	/// </summary>
	/// <remarks>
	/// A <code>ConstituentFactory</code> acts as a factory for creating objects
	/// of class <code>Constituent</code>, or some descendent class.
	/// An interface.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class SimpleConstituentFactory : IConstituentFactory
	{
		public virtual Constituent NewConstituent(int start, int end)
		{
			return new SimpleConstituent(start, end);
		}

		public virtual Constituent NewConstituent(int start, int end, ILabel label, double score)
		{
			return new SimpleConstituent(start, end);
		}
	}
}

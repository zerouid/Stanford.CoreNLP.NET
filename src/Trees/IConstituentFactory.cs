using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>ConstituentFactory</code> is a factory for creating objects
	/// of class <code>Constituent</code>, or some descendent class.
	/// </summary>
	/// <remarks>
	/// A <code>ConstituentFactory</code> is a factory for creating objects
	/// of class <code>Constituent</code>, or some descendent class.
	/// An interface.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IConstituentFactory
	{
		/// <summary>Build a constituent with this start and end.</summary>
		/// <param name="start">Start position</param>
		/// <param name="end">End position</param>
		Constituent NewConstituent(int start, int end);

		/// <summary>Build a constituent with this start and end.</summary>
		/// <param name="start">Start position</param>
		/// <param name="end">End position</param>
		/// <param name="label">Label</param>
		/// <param name="score">Score</param>
		Constituent NewConstituent(int start, int end, ILabel label, double score);
	}
}

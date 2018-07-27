

namespace Edu.Stanford.Nlp.Optimization
{
	/// <summary>Indicates that a function has a method for supplying an intitial value.</summary>
	/// <author><a href="mailto:klein@cs.stanford.edu">Dan Klein</a></author>
	/// <version>1.0</version>
	/// <since>1.0</since>
	public interface IHasInitial
	{
		/// <summary>Returns the intitial point in the domain (but not necessarily a feasible one).</summary>
		/// <returns>a domain point</returns>
		double[] Initial();
	}
}

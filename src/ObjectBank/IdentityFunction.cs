


namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>An Identity function that returns its argument.</summary>
	/// <author>Jenny Finkel</author>
	public class IdentityFunction<X> : Func<X, X>
	{
		/// <param name="o">The Object to be returned</param>
		/// <returns>o</returns>
		public virtual X Apply(X o)
		{
			return o;
		}
	}
}

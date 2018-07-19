using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>An individual dependency between a governor and a dependent.</summary>
	/// <remarks>
	/// An individual dependency between a governor and a dependent.
	/// The governor and dependent are represented as a Label.
	/// For example, these can be a
	/// Word or a WordTag.  If one wishes the dependencies to preserve positions
	/// in a sentence, then each can be a LabeledConstituent or CoreLabel.
	/// Dependencies support an Object naming the dependency type.  This may be
	/// null.  Dependencies have factories.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public interface IDependency<G, D, N>
		where G : ILabel
		where D : ILabel
	{
		/// <summary>Describes the governor (regent/head) of the dependency relation.</summary>
		/// <returns>The governor of this dependency</returns>
		G Governor();

		/// <summary>
		/// Describes the dependent (argument/modifier) of
		/// the dependency relation.
		/// </summary>
		/// <returns>the dependent of this dependency</returns>
		D Dependent();

		/// <summary>Names the type of dependency (subject, instrument, ...).</summary>
		/// <remarks>
		/// Names the type of dependency (subject, instrument, ...).
		/// This might be a String in the simplest case, but can provide for
		/// arbitrary object types.
		/// </remarks>
		/// <returns>the name for this dependency type</returns>
		N Name();

		/// <summary>Are two dependencies equal if you ignore the dependency name.</summary>
		/// <param name="o">The thing to compare against ignoring name</param>
		/// <returns>true iff the head and dependent are the same.</returns>
		bool EqualsIgnoreName(object o);

		/// <summary>Provide different printing options via a String keyword.</summary>
		/// <remarks>
		/// Provide different printing options via a String keyword.
		/// The main recognized option currently is "xml".  Otherwise the
		/// default toString() is used.
		/// </remarks>
		/// <param name="format">A format string, either "xml" or you get the default</param>
		/// <returns>A String representation of the dependency</returns>
		string ToString(string format);

		/// <summary>Provide a factory for this kind of dependency</summary>
		/// <returns>A DependencyFactory</returns>
		IDependencyFactory DependencyFactory();
	}
}



namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>Interface for Objects which can be described by their features.</summary>
	/// <remarks>
	/// Interface for Objects which can be described by their features.
	/// An Object is described by a Datum as a List of categorical features.
	/// (For features which have numeric values, see
	/// <see cref="RVFDatum{L, F}"/>
	/// .
	/// These objects can also be Serialized (for insertion into a file database).
	/// </remarks>
	/// <author>Sepandar Kamvar (sdkamvar@stanford.edu)</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	/// <?/>
	/// <?/>
	public interface IDatum<L, F> : IFeaturizable<F>, ILabeled<L>
	{
	}
}

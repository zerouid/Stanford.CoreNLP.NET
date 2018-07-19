using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>Interface for Objects that can be described by their features.</summary>
	/// <author>
	/// Sepandar Kamvar (sdkamvar@stanford.edu)
	/// Type-safety added by Sarah Spikes (sdspikes@cs.stanford.edu)
	/// </author>
	public interface IFeaturizable<F>
	{
		/// <summary>returns Object as a Collection of its features</summary>
		ICollection<F> AsFeatures();
	}
}

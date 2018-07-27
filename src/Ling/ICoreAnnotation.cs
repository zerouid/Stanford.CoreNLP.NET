using System;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// The base class for any annotation that can be marked on a
	/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
	/// ,
	/// parameterized by the type of the value associated with the annotation.
	/// Subclasses of this class are the keys in the
	/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
	/// , so they are
	/// instantiated only by utility methods in
	/// <see cref="CoreAnnotations"/>
	/// .
	/// </summary>
	/// <author>dramage</author>
	/// <author>rafferty</author>
	public interface ICoreAnnotation<V> : TypesafeMap.IKey<V>
	{
		/// <summary>Returns the type associated with this annotation.</summary>
		/// <remarks>
		/// Returns the type associated with this annotation.  This method must
		/// return the same class type as its value type parameter.  It feels like
		/// one should be able to get away without this method, but because Java
		/// erases the generic type signature, that info disappears at runtime.
		/// </remarks>
		Type GetType();
	}
}

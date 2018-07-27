


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A pointer to an object, to get around not being able to access non-final
	/// variables within an anonymous function.
	/// </summary>
	/// <author>Gabor Angeli</author>
	[System.Serializable]
	public class Pointer<T>
	{
		/// <summary>The serial version uid to ensure stable serialization.</summary>
		private const long serialVersionUID = 1L;

		/// <summary>The value the pointer is set to, if it is set.</summary>
		private T impl;

		/// <summary>Create a pointer pointing nowhere.</summary>
		public Pointer()
		{
			this.impl = null;
		}

		/// <summary>Create a pointer pointing at the given object.</summary>
		/// <param name="impl">The object the pointer is pointing at.</param>
		public Pointer(T impl)
		{
			this.impl = impl;
		}

		/// <summary>Dereference the pointer.</summary>
		/// <remarks>
		/// Dereference the pointer.
		/// If the pointer is pointing somewhere, the
		/// <linkplain>
		/// Optional
		/// optional
		/// </linkplain>
		/// will be set.
		/// Otherwise, the optional will be
		/// <linkplain>
		/// Optional#empty()
		/// empty
		/// </linkplain>
		/// .
		/// </remarks>
		public virtual Optional<T> Dereference()
		{
			return Optional.OfNullable(impl);
		}

		/// <summary>Set the pointer.</summary>
		/// <param name="impl">The value to set the pointer to. If this is null, the pointer is unset.</param>
		public virtual void Set(T impl)
		{
			this.impl = impl;
		}

		/// <summary>Set the pointer to a possible value.</summary>
		/// <param name="impl">
		/// The value to set the pointer to. If this is
		/// <linkplain>
		/// Optional#empty
		/// empty
		/// </linkplain>
		/// , the pointer is unset.
		/// </param>
		public virtual void Set(Optional<T> impl)
		{
			this.impl = impl.OrElse(null);
		}
	}
}

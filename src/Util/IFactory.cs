

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>A generified factory class which creates instances of a particular type.</summary>
	/// <author>dramage</author>
	public interface IFactory<T>
	{
		/// <summary>Creates and returns a new instance of the given type.</summary>
		/// <returns>A new instance of the type T</returns>
		T Create();
	}
}

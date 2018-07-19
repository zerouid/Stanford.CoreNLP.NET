using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Disjoint set interface.</summary>
	/// <author>Dan Klein</author>
	/// <version>4/17/01</version>
	public interface IDisjointSet<T>
	{
		T Find(T o);

		void Union(T a, T b);
	}
}

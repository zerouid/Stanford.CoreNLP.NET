using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Only to be implemented by Tree subclasses that actualy keep their
	/// parent pointers.
	/// </summary>
	/// <remarks>
	/// Only to be implemented by Tree subclasses that actualy keep their
	/// parent pointers.  For example, the base Tree class should
	/// <b>not</b> implement this, but TreeGraphNode should.
	/// </remarks>
	/// <author>John Bauer</author>
	public interface IHasParent
	{
		Tree Parent();
	}
}

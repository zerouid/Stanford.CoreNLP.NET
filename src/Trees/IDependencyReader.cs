using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	public interface IDependencyReader
	{
		/// <exception cref="System.IO.IOException"/>
		IList<GrammaticalStructure> ReadDependencies(string fileName);
	}
}

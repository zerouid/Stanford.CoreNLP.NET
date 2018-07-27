using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Trees
{
	public interface IDependencyReader
	{
		/// <exception cref="System.IO.IOException"/>
		IList<GrammaticalStructure> ReadDependencies(string fileName);
	}
}

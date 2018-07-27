using System.Collections.Generic;
using Edu.Stanford.Nlp.Trees;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <author>grenager</author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) (Templatization)</author>
	public interface IExtractor<T>
	{
		T Extract(ICollection<Tree> trees);

		T Extract(IEnumerator<Tree> iterator, IFunction<Tree, Tree> f);
	}
}

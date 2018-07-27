using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Tagger.IO
{
	public interface ITaggedFileReader : IEnumerable<IList<TaggedWord>>, IEnumerator<IList<TaggedWord>>
	{
		string Filename();
	}
}

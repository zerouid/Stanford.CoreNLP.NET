using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Tagger.Common
{
	/// <summary>
	/// This module includes constants that are the same for all taggers,
	/// as opposed to being part of their configurations.
	/// </summary>
	/// <remarks>
	/// This module includes constants that are the same for all taggers,
	/// as opposed to being part of their configurations.
	/// Also, can be used as an interface if you don't want to necessarily
	/// include the MaxentTagger code, such as in public releases which
	/// don't include that code.
	/// </remarks>
	/// <author>John Bauer</author>
	public abstract class Tagger : IFunction<IList<IHasWord>, IList<TaggedWord>>
	{
		public const string EosTag = ".$$.";

		public const string EosWord = ".$.";

		public abstract IList<TaggedWord> Apply<_T0>(IList<_T0> @in)
			where _T0 : IHasWord;

		public static Edu.Stanford.Nlp.Tagger.Common.Tagger LoadModel(string path)
		{
			// TODO: we can avoid ReflectionLoading if we instead use the
			// serialization mechanism in MaxentTagger.  Similar to ParserGrammar
			return ReflectionLoading.LoadByReflection("edu.stanford.nlp.tagger.maxent.MaxentTagger", path);
		}
	}
}

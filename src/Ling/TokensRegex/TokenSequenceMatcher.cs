using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Token Sequence Matcher for regular expressions over sequences of tokens.</summary>
	/// <author>Angel Chang</author>
	public class TokenSequenceMatcher : CoreMapSequenceMatcher<ICoreMap>
	{
		public TokenSequenceMatcher(SequencePattern<ICoreMap> pattern, IList<ICoreMap> tokens)
			: base(pattern, tokens)
		{
			/* protected static Function<List<? extends CoreLabel>, String> CORELABEL_LIST_TO_STRING_CONVERTER =
			new Function<List<? extends CoreLabel>, String>() {
			public String apply(List<? extends CoreLabel> in) {
			return (in != null)? ChunkAnnotationUtils.getTokenText(in, CoreAnnotations.TextAnnotation.class): null;
			}
			};     */
			//   this.nodesToStringConverter = CORELABEL_LIST_TO_STRING_CONVERTER;
			this.nodesToStringConverter = CoremapListToStringConverter;
		}
	}
}

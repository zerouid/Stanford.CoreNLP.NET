using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	public class SegmenterCoreAnnotations
	{
		private SegmenterCoreAnnotations()
		{
		}

		public class CharactersAnnotation : ICoreAnnotation<IList<CoreLabel>>
		{
			// only static members
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}

		public class XMLCharAnnotation : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}
	}
}

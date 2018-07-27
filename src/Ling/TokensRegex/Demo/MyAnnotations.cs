using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Demo
{
	/// <summary>Example annotations</summary>
	/// <author>Angel Chang</author>
	public class MyAnnotations
	{
		public class MyTokensAnnotation : ICoreAnnotation<IList<ICoreMap>>
		{
			// My custom tokens
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}

		public class MyTypeAnnotation : ICoreAnnotation<string>
		{
			// My custom type
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(string));
			}
		}

		public class MyValueAnnotation : ICoreAnnotation<string>
		{
			// My custom value
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(string));
			}
		}
	}
}

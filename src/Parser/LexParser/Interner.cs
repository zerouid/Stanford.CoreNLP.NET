using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>(Someday this should be removed, but at present lexparser needs it)</summary>
	/// <author>Dan Klein</author>
	internal class Interner<E>
	{
		private IDictionary<E, E> oToO = Generics.NewHashMap();

		public virtual E Intern(E o)
		{
			E i = oToO[o];
			if (i == null)
			{
				i = o;
				oToO[o] = o;
			}
			return i;
		}
	}
}

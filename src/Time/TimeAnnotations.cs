using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Time
{
	/// <summary>
	/// Set of common annotations for
	/// <see cref="Edu.Stanford.Nlp.Util.ICoreMap"/>
	/// s
	/// that require classes from the time package.  See
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// for more information.  This class exists
	/// so that
	/// <see cref="Edu.Stanford.Nlp.Ling.CoreAnnotations"/>
	/// need not depend on timex classes,
	/// which in particular pull in the xom.jar package.
	/// </summary>
	/// <author>John Bauer</author>
	public class TimeAnnotations
	{
		/// <summary>The CoreMap key for storing a Timex annotation</summary>
		public class TimexAnnotation : ICoreAnnotation<Timex>
		{
			public virtual Type GetType()
			{
				return typeof(Timex);
			}
		}

		/// <summary>The CoreMap key for storing all Timex annotations in a document.</summary>
		public class TimexAnnotations : ICoreAnnotation<IList<ICoreMap>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast(typeof(IList));
			}
		}
	}
}

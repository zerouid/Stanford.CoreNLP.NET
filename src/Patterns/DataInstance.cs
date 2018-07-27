using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Patterns.Dep;
using Edu.Stanford.Nlp.Patterns.Surface;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Patterns
{
	/// <summary>
	/// It's a list of Corelabels for SurfacePattern, Dependency parse for DepPattern etc
	/// Created by sonalg on 11/1/14.
	/// </summary>
	[System.Serializable]
	public abstract class DataInstance
	{
		public abstract IList<CoreLabel> GetTokens();

		public static DataInstance GetNewSurfaceInstance(IList<CoreLabel> tokens)
		{
			return new DataInstanceSurface(tokens);
		}

		public static DataInstance GetNewInstance(PatternFactory.PatternType type, ICoreMap s)
		{
			if (type.Equals(PatternFactory.PatternType.Surface))
			{
				return new DataInstanceSurface(s.Get(typeof(CoreAnnotations.TokensAnnotation)));
			}
			else
			{
				if (type.Equals(PatternFactory.PatternType.Dep))
				{
					return new DataInstanceDep(s);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}
	}
}

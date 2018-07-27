using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Patterns
{
	[System.Serializable]
	public class PatternsAnnotations
	{
		private const long serialVersionUID = 1L;

		public class ProcessedTextAnnotation : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class MatchedPattern : CoreLabel.IGenericAnnotation<bool>
		{
			public virtual Type GetType()
			{
				return typeof(bool);
			}
		}

		public class MatchedPatterns : CoreLabel.IGenericAnnotation<ICollection<Pattern>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(ISet));
			}
		}

		/// <summary>All matched phrases - can be from multiple labels</summary>
		public class MatchedPhrases : CoreLabel.IGenericAnnotation<CollectionValuedMap<string, CandidatePhrase>>
		{
			public virtual Type GetType()
			{
				Type claz = (Type)typeof(IDictionary);
				return claz;
			}
		}

		/// <summary>For each label, what was the longest phrase that matched.</summary>
		/// <remarks>For each label, what was the longest phrase that matched. If none, then the map doesn't have the label key</remarks>
		public class LongestMatchedPhraseForEachLabel : CoreLabel.IGenericAnnotation<IDictionary<string, CandidatePhrase>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IDictionary));
			}
		}

		public class PatternLabel1 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel2 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel3 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel4 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel5 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel6 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel7 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel8 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel9 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternLabel10 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class SeedLabeledOrNot : CoreLabel.IGenericAnnotation<IDictionary<Type, bool>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IDictionary));
			}
		}

		public class OtherSemanticLabel : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class Features : CoreLabel.IGenericAnnotation<ICollection<string>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(ISet));
			}
		}

		public class PatternHumanLabel1 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel2 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel3 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel4 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel5 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel6 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel7 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel8 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel9 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		public class PatternHumanLabel10 : CoreLabel.IGenericAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}
	}
}

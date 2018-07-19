using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Common
{
	/// <summary>Parse time options for the Stanford lexicalized parser.</summary>
	/// <remarks>
	/// Parse time options for the Stanford lexicalized parser.  For
	/// example, you can set a ConstraintAnnotation and the parser
	/// annotator will extract that annotation and apply the constraints
	/// when parsing.
	/// </remarks>
	public class ParserAnnotations
	{
		private ParserAnnotations()
		{
		}

		/// <summary>
		/// This CoreMap key represents a regular expression which the parser
		/// will try to match when assigning tags.
		/// </summary>
		/// <remarks>
		/// This CoreMap key represents a regular expression which the parser
		/// will try to match when assigning tags.
		/// This key is typically set on token annotations.
		/// </remarks>
		public class CandidatePartOfSpeechAnnotation : ICoreAnnotation<string>
		{
			// only static members
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>The CoreMap key for getting a list of constraints to apply when parsing.</summary>
		public class ConstraintAnnotation : ICoreAnnotation<IList<ParserConstraint>>
		{
			public virtual Type GetType()
			{
				return ErasureUtils.UncheckedCast<Type>(typeof(IList));
			}
		}
	}
}

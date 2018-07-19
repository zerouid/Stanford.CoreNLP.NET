using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>A mapping from verbs to their different tenses.</summary>
	/// <remarks>
	/// A mapping from verbs to their different tenses.
	/// This is English-only, for now.
	/// </remarks>
	/// <author><a href="mailto:angeli@cs.stanford.edu">Gabor Angeli</a></author>
	[System.Serializable]
	public sealed class VerbTense
	{
		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense Infinitive = new Edu.Stanford.Nlp.Naturalli.VerbTense(0);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense SingularPresentFirstPerson = new Edu.Stanford.Nlp.Naturalli.VerbTense(1);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense SingularPresentSecondPerson = new Edu.Stanford.Nlp.Naturalli.VerbTense(2);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense SingularPresentThirdPerson = new Edu.Stanford.Nlp.Naturalli.VerbTense(3);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense PresentPlural = new Edu.Stanford.Nlp.Naturalli.VerbTense(4);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense PresentParticiple = new Edu.Stanford.Nlp.Naturalli.VerbTense(5);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense SingularPastFirstPerson = new Edu.Stanford.Nlp.Naturalli.VerbTense(6);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense SingularPastSecondPerson = new Edu.Stanford.Nlp.Naturalli.VerbTense(7);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense SingularPastThirdPerson = new Edu.Stanford.Nlp.Naturalli.VerbTense(8);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense PastPlural = new Edu.Stanford.Nlp.Naturalli.VerbTense(9);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense Past = new Edu.Stanford.Nlp.Naturalli.VerbTense(10);

		public static readonly Edu.Stanford.Nlp.Naturalli.VerbTense PastParticiple = new Edu.Stanford.Nlp.Naturalli.VerbTense(11);

		/// <summary>The data for common verb conjugations.</summary>
		private static readonly Lazy<IDictionary<string, string[]>> EnglishTenses = Lazy.Of(null);

		/// <summary>The column of the file for the tense.</summary>
		private readonly int column;

		internal VerbTense(int column)
		{
			this.column = column;
		}

		/// <summary>Get the correct verb tense for the verb tense's features.</summary>
		/// <param name="past">If true, this is a past-tense verb.</param>
		/// <param name="plural">If true, this is a plural verb.</param>
		/// <param name="participle">If true, this is a participle verb.</param>
		/// <param name="person">1st, 2nd, or 3rd person: corresponds to 1,2, or 3 for this argument.</param>
		/// <returns>The verb tense corresponding to this information</returns>
		public static Edu.Stanford.Nlp.Naturalli.VerbTense Of(bool past, bool plural, bool participle, int person)
		{
			if (past)
			{
				if (plural)
				{
					return Edu.Stanford.Nlp.Naturalli.VerbTense.PastPlural;
				}
				if (participle)
				{
					return Edu.Stanford.Nlp.Naturalli.VerbTense.PastParticiple;
				}
				switch (person)
				{
					case 1:
					{
						return Edu.Stanford.Nlp.Naturalli.VerbTense.SingularPastFirstPerson;
					}

					case 2:
					{
						return Edu.Stanford.Nlp.Naturalli.VerbTense.SingularPastSecondPerson;
					}

					case 3:
					default:
					{
						return Edu.Stanford.Nlp.Naturalli.VerbTense.SingularPastThirdPerson;
					}
				}
			}
			else
			{
				if (plural)
				{
					return Edu.Stanford.Nlp.Naturalli.VerbTense.PresentPlural;
				}
				if (participle)
				{
					return Edu.Stanford.Nlp.Naturalli.VerbTense.PresentParticiple;
				}
				switch (person)
				{
					case 1:
					{
						return Edu.Stanford.Nlp.Naturalli.VerbTense.SingularPresentFirstPerson;
					}

					case 2:
					{
						return Edu.Stanford.Nlp.Naturalli.VerbTense.SingularPresentSecondPerson;
					}

					case 3:
					default:
					{
						return Edu.Stanford.Nlp.Naturalli.VerbTense.SingularPresentThirdPerson;
					}
				}
			}
		}

		/// <summary>Apply this tense to the given verb.</summary>
		/// <param name="lemma">The verb to conjugate.</param>
		/// <param name="negated">If true, this verb is negated.</param>
		/// <returns>The conjugated verb.</returns>
		public string ConjugateEnglish(string lemma, bool negated)
		{
			string[] data = Edu.Stanford.Nlp.Naturalli.VerbTense.EnglishTenses.Get()[lemma];
			if (data != null)
			{
				string conjugated = data[negated ? Edu.Stanford.Nlp.Naturalli.VerbTense.column + 12 : Edu.Stanford.Nlp.Naturalli.VerbTense.column];
				if (!string.Empty.Equals(conjugated))
				{
					// case: we found a match
					return conjugated;
				}
				else
				{
					if (negated)
					{
						// case: try the unnegated form
						conjugated = data[Edu.Stanford.Nlp.Naturalli.VerbTense.column];
						if (!string.Empty.Equals(conjugated))
						{
							return conjugated;
						}
					}
				}
				// case: tense not explicit in map
				if (Edu.Stanford.Nlp.Naturalli.VerbTense.column >= 0 && Edu.Stanford.Nlp.Naturalli.VerbTense.column < 6)
				{
					conjugated = data[Edu.Stanford.Nlp.Naturalli.VerbTense.Infinitive.column];
				}
				else
				{
					conjugated = data[Edu.Stanford.Nlp.Naturalli.VerbTense.Past.column];
				}
				if (!string.Empty.Equals(conjugated))
				{
					return conjugated;
				}
				else
				{
					return lemma;
				}
			}
			else
			{
				// case: word not in dictionary
				return lemma;
			}
		}

		/// <seealso cref="ConjugateEnglish(string, bool)"/>
		public string ConjugateEnglish(string lemma)
		{
			return ConjugateEnglish(lemma, false);
		}

		/// <seealso cref="ConjugateEnglish(string, bool)"/>
		public string ConjugateEnglish(CoreLabel token, bool negated)
		{
			return ConjugateEnglish(Optional.OfNullable(token.Lemma()).OrElse(token.Word()), negated);
		}

		/// <seealso cref="ConjugateEnglish(string, bool)"/>
		public string ConjugateEnglish(CoreLabel token)
		{
			return ConjugateEnglish(Optional.OfNullable(token.Lemma()).OrElse(token.Word()), false);
		}
	}
}

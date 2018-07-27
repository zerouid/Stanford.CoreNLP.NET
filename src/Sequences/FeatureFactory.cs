using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Sequences
{
	/// <summary>
	/// This is the abstract class that all feature factories must
	/// subclass.
	/// </summary>
	/// <remarks>
	/// This is the abstract class that all feature factories must
	/// subclass.  It also defines most of the basic
	/// <see cref="Clique"/>
	/// s
	/// that you would want to make features over.  It contains a
	/// convenient method, getCliques(maxLeft, maxRight) which will give
	/// you all the cliques within the specified limits.
	/// </remarks>
	/// <?/>
	/// <author>Jenny Finkel</author>
	[System.Serializable]
	public abstract class FeatureFactory<In>
	{
		private const long serialVersionUID = 7249250071983091694L;

		protected internal SeqClassifierFlags flags;

		public FeatureFactory()
		{
		}

		public virtual void Init(SeqClassifierFlags flags)
		{
			this.flags = flags;
		}

		public static readonly Clique cliqueC = Clique.ValueOf(new int[] { 0 });

		public static readonly Clique cliqueCpC = Clique.ValueOf(new int[] { -1, 0 });

		public static readonly Clique cliqueCp2C = Clique.ValueOf(new int[] { -2, 0 });

		public static readonly Clique cliqueCp3C = Clique.ValueOf(new int[] { -3, 0 });

		public static readonly Clique cliqueCp4C = Clique.ValueOf(new int[] { -4, 0 });

		public static readonly Clique cliqueCp5C = Clique.ValueOf(new int[] { -5, 0 });

		public static readonly Clique cliqueCpCp2C = Clique.ValueOf(new int[] { -2, -1, 0 });

		public static readonly Clique cliqueCpCp2Cp3C = Clique.ValueOf(new int[] { -3, -2, -1, 0 });

		public static readonly Clique cliqueCpCp2Cp3Cp4C = Clique.ValueOf(new int[] { -4, -3, -2, -1, 0 });

		public static readonly Clique cliqueCpCp2Cp3Cp4Cp5C = Clique.ValueOf(new int[] { -5, -4, -3, -2, -1, 0 });

		public static readonly Clique cliqueCnC = Clique.ValueOf(new int[] { 0, 1 });

		public static readonly Clique cliqueCpCnC = Clique.ValueOf(new int[] { -1, 0, 1 });

		public static readonly IList<Clique> knownCliques = Arrays.AsList(cliqueC, cliqueCpC, cliqueCp2C, cliqueCp3C, cliqueCp4C, cliqueCp5C, cliqueCpCp2C, cliqueCpCp2Cp3C, cliqueCpCp2Cp3Cp4C, cliqueCpCp2Cp3Cp4Cp5C, cliqueCnC, cliqueCpCnC);

		public virtual IList<Clique> GetCliques()
		{
			return GetCliques(flags.maxLeft, flags.maxRight);
		}

		public static IList<Clique> GetCliques(int maxLeft, int maxRight)
		{
			IList<Clique> cliques = new List<Clique>();
			foreach (Clique c in knownCliques)
			{
				if (-c.MaxLeft() <= maxLeft && c.MaxRight() <= maxRight)
				{
					cliques.Add(c);
				}
			}
			return cliques;
		}

		/// <summary>
		/// This method returns a
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of the features
		/// calculated for the word at the specified position in info (the list of
		/// words) for the specified
		/// <see cref="Clique"/>
		/// .
		/// It should return the actual String features, <b>NOT</b> wrapped in any
		/// other object, as the wrapping
		/// will be done automatically.
		/// Because it takes a
		/// <see cref="Edu.Stanford.Nlp.Util.PaddedList{E}"/>
		/// you don't
		/// need to worry about indices which are outside of the list.
		/// </summary>
		/// <param name="info">A PaddedList of the feature-value pairs</param>
		/// <param name="position">The current position to extract features at</param>
		/// <param name="clique">
		/// The particular clique for which to extract features. It
		/// should be a member of the knownCliques list.
		/// </param>
		/// <returns>
		/// A
		/// <see cref="System.Collections.ICollection{E}"/>
		/// of the features
		/// calculated for the word at the specified position in info.
		/// </returns>
		public abstract ICollection<string> GetCliqueFeatures(PaddedList<In> info, int position, Clique clique);

		/// <summary>
		/// Makes more complete feature names out of partial feature names, by
		/// adding a suffix to the String feature name, adding results to an
		/// accumulator
		/// </summary>
		/// <param name="accumulator">The output features are added here</param>
		/// <param name="addend">The base set of features</param>
		/// <param name="suffix">The suffix added to each feature in the addend set</param>
		protected internal virtual void AddAllInterningAndSuffixing(ICollection<string> accumulator, ICollection<string> addend, string suffix)
		{
			bool nonNullSuffix = suffix != null && !suffix.IsEmpty();
			if (nonNullSuffix)
			{
				suffix = '|' + suffix;
			}
			// boolean intern2 = flags.intern2;
			foreach (string feat in addend)
			{
				if (nonNullSuffix)
				{
					feat = feat.Concat(suffix);
				}
				// if (intern2) {
				//   feat = feat.intern();
				// }
				accumulator.Add(feat);
			}
		}

		/// <summary>Convenience methods for subclasses which use CoreLabel.</summary>
		/// <remarks>
		/// Convenience methods for subclasses which use CoreLabel.  Gets the
		/// word after applying any wordFunction present in the
		/// SeqClassifierFlags.
		/// </remarks>
		/// <param name="label">A CoreLabel</param>
		/// <returns>
		/// The TextAnnotation of the label, perhaps after passing it through
		/// a function (flags.wordFunction)
		/// </returns>
		protected internal virtual string GetWord(CoreLabel label)
		{
			string word = label.GetString<CoreAnnotations.TextAnnotation>();
			if (flags.wordFunction != null)
			{
				word = flags.wordFunction.Apply(word);
			}
			return word;
		}
	}
}

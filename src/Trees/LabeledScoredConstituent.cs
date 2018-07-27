using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>LabeledScoredConstituent</code> object defines an edge in a graph
	/// with a label and a score.
	/// </summary>
	/// <author>Christopher Manning</author>
	public class LabeledScoredConstituent : LabeledConstituent
	{
		private double score;

		/// <summary>Create an empty <code>LabeledScoredConstituent</code> object.</summary>
		public LabeledScoredConstituent()
		{
		}

		/// <summary>
		/// Create a <code>LabeledScoredConstituent</code> object with given
		/// values.
		/// </summary>
		/// <param name="start">start node of edge</param>
		/// <param name="end">end node of edge</param>
		public LabeledScoredConstituent(int start, int end)
			: base(start, end)
		{
		}

		/// <summary>
		/// Create a <code>LabeledScoredConstituent</code> object with given
		/// values.
		/// </summary>
		/// <param name="start">start node of edge</param>
		/// <param name="end">end node of edge</param>
		public LabeledScoredConstituent(int start, int end, ILabel label, double score)
			: base(start, end, label)
		{
			// implicitly super();
			this.score = score;
		}

		/// <summary>
		/// Returns the score associated with the current node, or Nan
		/// if there is no score
		/// </summary>
		/// <returns>the score</returns>
		public override double Score()
		{
			return score;
		}

		/// <summary>Sets the score associated with the current node, if there is one</summary>
		public override void SetScore(double score)
		{
			this.score = score;
		}

		/// <summary>
		/// A <code>LabeledScoredConstituentLabelFactory</code> object makes a
		/// <code>LabeledScoredConstituent</code> with a <code>StringLabel</code>
		/// label (or of the type of label passed in for the final constructor).
		/// </summary>
		private class LabeledScoredConstituentLabelFactory : ILabelFactory
		{
			/// <summary>Make a new <code>LabeledScoredConstituent</code>.</summary>
			/// <param name="labelStr">A string</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabel(string labelStr)
			{
				return new LabeledScoredConstituent(0, 0, new StringLabel(labelStr), 0.0);
			}

			/// <summary>Make a new <code>LabeledScoredConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <param name="options">The options are ignored.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabel(string labelStr, int options)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Make a new <code>LabeledScoredConstituent</code>.</summary>
			/// <param name="labelStr">A string that</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabelFromString(string labelStr)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Create a new <code>LabeledScoredConstituent</code>.</summary>
			/// <param name="oldLabel">A <code>Label</code>.</param>
			/// <returns>A new <code>LabeledScoredConstituent</code></returns>
			public virtual ILabel NewLabel(ILabel oldLabel)
			{
				return new LabeledScoredConstituent(0, 0, oldLabel, 0.0);
			}
		}

		private class LabelFactoryHolder
		{
			internal static readonly ILabelFactory lf = new LabeledScoredConstituent.LabeledScoredConstituentLabelFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>Return a factory for this kind of label.</summary>
		/// <remarks>
		/// Return a factory for this kind of label.
		/// The factory returned is always the same one (a singleton)
		/// </remarks>
		/// <returns>the label factory</returns>
		public override ILabelFactory LabelFactory()
		{
			return LabeledScoredConstituent.LabelFactoryHolder.lf;
		}

		private class ConstituentFactoryHolder
		{
			private static readonly IConstituentFactory cf = new LabeledScoredConstituentFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public override IConstituentFactory ConstituentFactory()
		{
			return LabeledScoredConstituent.ConstituentFactoryHolder.cf;
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public static IConstituentFactory Factory()
		{
			return LabeledScoredConstituent.ConstituentFactoryHolder.cf;
		}
	}
}

using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>LabeledConstituent</code> object represents a single bracketing in
	/// a derivation, including start and end points and <code>Label</code>
	/// information, but excluding probabilistic information.
	/// </summary>
	/// <remarks>
	/// A <code>LabeledConstituent</code> object represents a single bracketing in
	/// a derivation, including start and end points and <code>Label</code>
	/// information, but excluding probabilistic information.  It is used
	/// to represent the basic information that is accumulated in exploring parses.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2002/06/01</version>
	public class LabeledConstituent : SimpleConstituent
	{
		/// <summary>The Label.</summary>
		private ILabel label;

		/// <summary>Create an empty <code>LabeledConstituent</code> object.</summary>
		public LabeledConstituent()
		{
		}

		/// <summary>
		/// Create a <code>LabeledConstituent</code> object with given
		/// values.
		/// </summary>
		/// <param name="start">Start node of edge</param>
		/// <param name="end">End node of edge</param>
		public LabeledConstituent(int start, int end)
			: base(start, end)
		{
		}

		/// <summary>Create a <code>LabeledConstituent</code> object with given values.</summary>
		/// <param name="start">Start node of edge</param>
		/// <param name="end">End node of edge</param>
		/// <param name="label">The label of the <code>Constituent</code></param>
		public LabeledConstituent(int start, int end, ILabel label)
			: base(start, end)
		{
			/* implements Label */
			// implicitly super();
			this.label = label;
		}

		/// <summary>Create a <code>LabeledConstituent</code> object with given values.</summary>
		/// <param name="start">Start node of edge</param>
		/// <param name="end">End node of edge</param>
		/// <param name="stringValue">The name of the <code>Constituent</code></param>
		public LabeledConstituent(int start, int end, string stringValue)
			: base(start, end)
		{
			this.label = new StringLabel(stringValue);
		}

		public override ILabel Label()
		{
			return label;
		}

		public override void SetLabel(ILabel label)
		{
			this.label = label;
		}

		public override void SetFromString(string labelStr)
		{
			this.label = new StringLabel(labelStr);
		}

		/// <summary>
		/// A <code>LabeledConstituentLabelFactory</code> object makes a
		/// <code>StringLabel</code> <code>LabeledScoredConstituent</code>.
		/// </summary>
		private class LabeledConstituentLabelFactory : ILabelFactory
		{
			/// <summary>Make a new <code>LabeledConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabel(string labelStr)
			{
				return new LabeledConstituent(0, 0, new StringLabel(labelStr));
			}

			/// <summary>Make a new <code>LabeledConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <param name="options">The options are ignored.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabel(string labelStr, int options)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Make a new <code>LabeledConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabelFromString(string labelStr)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Create a new <code>LabeledConstituent</code>.</summary>
			/// <param name="oldLabel">A <code>Label</code>.</param>
			/// <returns>A new <code>LabeledConstituent</code></returns>
			public virtual ILabel NewLabel(ILabel oldLabel)
			{
				return new LabeledConstituent(0, 0, oldLabel);
			}
		}

		private class LabelFactoryHolder
		{
			internal static readonly ILabelFactory lf = new LabeledConstituent.LabeledConstituentLabelFactory();
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
			return LabeledConstituent.LabelFactoryHolder.lf;
		}

		private class ConstituentFactoryHolder
		{
			/// <summary>
			/// A <code>LabeledConstituentFactory</code> acts as a factory for
			/// creating objects of class <code>LabeledConstituent</code>.
			/// </summary>
			private class LabeledConstituentFactory : IConstituentFactory
			{
				// extra class guarantees correct lazy loading (Bloch p.194)
				public virtual Constituent NewConstituent(int start, int end)
				{
					return new LabeledConstituent(start, end);
				}

				public virtual Constituent NewConstituent(int start, int end, ILabel label, double score)
				{
					return new LabeledConstituent(start, end, label);
				}
			}

			internal static readonly IConstituentFactory cf = new LabeledConstituent.ConstituentFactoryHolder.LabeledConstituentFactory();
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public override IConstituentFactory ConstituentFactory()
		{
			return LabeledConstituent.ConstituentFactoryHolder.cf;
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public static IConstituentFactory Factory()
		{
			return LabeledConstituent.ConstituentFactoryHolder.cf;
		}
	}
}

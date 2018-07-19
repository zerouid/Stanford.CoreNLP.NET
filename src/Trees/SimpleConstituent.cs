using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A <code>SimpleConstituent</code> object defines a generic edge in a graph.</summary>
	/// <remarks>
	/// A <code>SimpleConstituent</code> object defines a generic edge in a graph.
	/// The <code>SimpleConstituent</code> records only the endpoints of the
	/// <code>Constituent</code>, as two integers.
	/// It doesn't label the edges.
	/// (It doesn't implement equals() since this actually decreases
	/// performance on a non-final class (requires dynamic resolution of which
	/// to call).)
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class SimpleConstituent : Constituent
	{
		/// <summary>Left node of edge.</summary>
		private int start;

		/// <summary>End node of edge.</summary>
		private int end;

		/// <summary>Create an empty <code>SimpleConstituent</code> object.</summary>
		public SimpleConstituent()
		{
		}

		/// <summary>Create a <code>SimpleConstituent</code> object with given values.</summary>
		/// <param name="start">start node of edge</param>
		/// <param name="end">end node of edge</param>
		public SimpleConstituent(int start, int end)
		{
			// implicitly super();
			this.start = start;
			this.end = end;
		}

		/// <summary>access start node.</summary>
		public override int Start()
		{
			return start;
		}

		/// <summary>set start node.</summary>
		public override void SetStart(int start)
		{
			this.start = start;
		}

		/// <summary>access end node.</summary>
		public override int End()
		{
			return end;
		}

		/// <summary>set end node.</summary>
		public override void SetEnd(int end)
		{
			this.end = end;
		}

		/// <summary>
		/// A <code>SimpleConstituentLabelFactory</code> object makes a
		/// <code>StringLabel</code> <code>LabeledScoredConstituent</code>.
		/// </summary>
		private class SimpleConstituentLabelFactory : ILabelFactory
		{
			/// <summary>Make a new <code>SimpleConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabel(string labelStr)
			{
				return new SimpleConstituent(0, 0);
			}

			/// <summary>Make a new <code>SimpleConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <param name="options">The options are ignored.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabel(string labelStr, int options)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Make a new <code>SimpleConstituent</code>.</summary>
			/// <param name="labelStr">A string.</param>
			/// <returns>The created label</returns>
			public virtual ILabel NewLabelFromString(string labelStr)
			{
				return NewLabel(labelStr);
			}

			/// <summary>Create a new <code>SimpleConstituent</code>.</summary>
			/// <param name="oldLabel">A <code>Label</code>.</param>
			/// <returns>A new <code>SimpleConstituent</code></returns>
			public virtual ILabel NewLabel(ILabel oldLabel)
			{
				return new SimpleConstituent(0, 0);
			}
		}

		private class LabelFactoryHolder
		{
			internal static readonly ILabelFactory lf = new SimpleConstituent.SimpleConstituentLabelFactory();
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
			return SimpleConstituent.LabelFactoryHolder.lf;
		}

		private class ConstituentFactoryHolder
		{
			/// <summary>
			/// A <code>SimpleConstituentFactory</code> acts as a factory for
			/// creating objects of class <code>SimpleConstituent</code>.
			/// </summary>
			private class SimpleConstituentFactory : IConstituentFactory
			{
				// extra class guarantees correct lazy loading (Bloch p.194)
				public virtual Constituent NewConstituent(int start, int end)
				{
					return new SimpleConstituent(start, end);
				}

				public virtual Constituent NewConstituent(int start, int end, ILabel label, double score)
				{
					return new SimpleConstituent(start, end);
				}
			}

			internal static readonly IConstituentFactory cf = new SimpleConstituent.ConstituentFactoryHolder.SimpleConstituentFactory();
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public virtual IConstituentFactory ConstituentFactory()
		{
			return SimpleConstituent.ConstituentFactoryHolder.cf;
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public static IConstituentFactory Factory()
		{
			return SimpleConstituent.ConstituentFactoryHolder.cf;
		}
	}
}

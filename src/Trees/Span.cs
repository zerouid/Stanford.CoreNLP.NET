using Edu.Stanford.Nlp.Ling;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>A <code>Span</code> is an optimized <code>SimpleConstituent</code> object.</summary>
	/// <remarks>
	/// A <code>Span</code> is an optimized <code>SimpleConstituent</code> object.
	/// It provides exactly the same functionality as a SimpleConstituent, but
	/// by being final, and with its own implementation of Span equality,
	/// it runs faster, so as to placate Dan Klein.  (With JDK1.3 client, it still
	/// doesn't run as fast as an implementation outside of the SimpleConstituent
	/// hierarchy, but with JDK1.3 server, it does!  And both versions are
	/// several times faster with -server than -client, so that should be used.)
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <version>2001/01/08</version>
	public sealed class Span : SimpleConstituent
	{
		/// <summary>Create an empty <code>Span</code> object.</summary>
		public Span()
		{
		}

		/// <summary>Create a <code>Span</code> object with given values.</summary>
		/// <param name="start">start node of edge</param>
		/// <param name="end">end node of edge</param>
		public Span(int start, int end)
			: base(start, end)
		{
		}

		// implicitly super();
		/// <summary>
		/// An overloading for efficiency for when you know that you're comparing
		/// with a Span.
		/// </summary>
		/// <param name="sp">the span to compare against</param>
		/// <returns>whether they have the same start and end</returns>
		/// <seealso cref="Constituent.Equals(object)"/>
		public bool Equals(Edu.Stanford.Nlp.Trees.Span sp)
		{
			return Start() == sp.Start() && End() == sp.End();
		}

		private class ConstituentFactoryHolder
		{
			private ConstituentFactoryHolder()
			{
			}

			/// <summary>
			/// A <code>SpanFactory</code> acts as a factory for creating objects
			/// of class <code>Span</code>.
			/// </summary>
			/// <remarks>
			/// A <code>SpanFactory</code> acts as a factory for creating objects
			/// of class <code>Span</code>.
			/// An interface.
			/// </remarks>
			private class SpanFactory : IConstituentFactory
			{
				// extra class guarantees correct lazy loading (Bloch p.194)
				// static holder class
				public virtual Constituent NewConstituent(int start, int end)
				{
					return new Span(start, end);
				}

				public virtual Constituent NewConstituent(int start, int end, ILabel label, double score)
				{
					return new Span(start, end);
				}
			}

			private static readonly IConstituentFactory cf = new Span.ConstituentFactoryHolder.SpanFactory();
		}

		// end static class ConstituentFactoryHolder
		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public override IConstituentFactory ConstituentFactory()
		{
			return Span.ConstituentFactoryHolder.cf;
		}

		/// <summary>Return a factory for this kind of constituent.</summary>
		/// <remarks>
		/// Return a factory for this kind of constituent.
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The constituent factory</returns>
		public static IConstituentFactory Factory()
		{
			return Span.ConstituentFactoryHolder.cf;
		}
	}
}

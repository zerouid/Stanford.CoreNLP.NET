using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Utility code for
	/// <see cref="Java.Util.Function.IFunction{T, R}"/>
	/// .
	/// </summary>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	/// <author>javanlp</author>
	public class Functions
	{
		private Functions()
		{
		}

		private class ComposedFunction<T1, T2, T3> : IFunction<T1, T3>
		{
			internal IFunction<T2, T3> g;

			internal IFunction<T1, T2> f;

			public ComposedFunction(IFunction<T2, T3> g, IFunction<T1, T2> f)
			{
				this.g = g;
				this.f = f;
			}

			public virtual T3 Apply(T1 t1)
			{
				return (g.Apply(f.Apply(t1)));
			}
		}

		/// <summary>
		/// Returns the
		/// <see cref="Java.Util.Function.IFunction{T, R}"/>
		/// <tt>g o f</tt>.
		/// </summary>
		/// <returns>g o f</returns>
		public static IFunction<T1, T3> Compose<T1, T2, T3, _T3>(IFunction<T1, T2> f, IFunction<_T3> g)
		{
			// Type system is stupid
			return new Functions.ComposedFunction(f, g);
		}

		public static IFunction<T, T> IdentityFunction<T>()
		{
			return null;
		}

		private class InvertedBijection<T1, T2> : IBijectiveFunction<T2, T1>
		{
			internal InvertedBijection(IBijectiveFunction<T1, T2> f)
			{
				this.f = f;
			}

			private readonly IBijectiveFunction<T1, T2> f;

			public virtual T1 Apply(T2 @in)
			{
				return f.Unapply(@in);
			}

			public virtual T2 Unapply(T1 @in)
			{
				return f.Apply(@in);
			}
		}

		public static IBijectiveFunction<T2, T1> Invert<T1, T2>(IBijectiveFunction<T1, T2> f)
		{
			if (f is Functions.InvertedBijection)
			{
				return ((Functions.InvertedBijection<T2, T1>)f).f;
			}
			return new Functions.InvertedBijection<T1, T2>(f);
		}
	}
}

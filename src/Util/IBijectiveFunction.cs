


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// A
	/// <see cref="Java.Util.Function.IFunction{T, R}"/>
	/// that is invertible, and so has the unapply method.
	/// </summary>
	/// <author>David Hall</author>
	public interface IBijectiveFunction<T1, T2> : IFunction<T1, T2>
	{
		T1 Unapply(T2 @in);
	}
}

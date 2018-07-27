



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>HasInterval interface</summary>
	/// <author>Angel Chang</author>
	public interface IHasInterval<E>
		where E : IComparable<E>
	{
		/// <summary>Returns the interval</summary>
		/// <returns>interval</returns>
		Interval<E> GetInterval();
	}

	public static class HasIntervalConstants
	{
		public const IComparator<IHasInterval<int>> LengthGtComparator = null;

		public const IComparator<IHasInterval<int>> LengthLtComparator = null;

		public const IComparator<IHasInterval> EndpointsComparator = null;

		public const IComparator<IHasInterval> NestedFirstEndpointsComparator = null;

		public const IComparator<IHasInterval> ContainsFirstEndpointsComparator = null;

		public const IComparator<IHasInterval<int>> LengthEndpointsComparator = Comparators.Chain(HasIntervalConstants.LengthGtComparator, HasIntervalConstants.EndpointsComparator);
	}
}

using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>Basic support for filtering records via LogFilter objects.</summary>
	/// <remarks>Basic support for filtering records via LogFilter objects.  Can be used in both conjunctive and disjunctive mode.</remarks>
	/// <author>David McClosky</author>
	public class FilterHandler : BooleanLogRecordHandler
	{
		private IList<ILogFilter> filters;

		private bool disjunctiveMode;

		public FilterHandler(IList<ILogFilter> filters, bool disjunctiveMode)
		{
			this.filters = filters;
			this.disjunctiveMode = disjunctiveMode;
		}

		public override bool PropagateRecord(Redwood.Record record)
		{
			foreach (ILogFilter filter in filters)
			{
				bool match = filter.Matches(record);
				if (match && disjunctiveMode)
				{
					return true;
				}
				if (!match && !disjunctiveMode)
				{
					return false;
				}
			}
			return !disjunctiveMode;
		}
	}
}

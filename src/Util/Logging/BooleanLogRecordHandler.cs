using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A log message handler designed for filtering.</summary>
	/// <remarks>A log message handler designed for filtering.  This is a convenience class to handle the common case of filtering messages.</remarks>
	/// <author>David McClosky</author>
	public abstract class BooleanLogRecordHandler : LogRecordHandler
	{
		/// <summary>For BooleanLogRecordHandler, you should leave this alone and implement propagateRecord instead.</summary>
		public override IList<Redwood.Record> Handle(Redwood.Record record)
		{
			bool keep = PropagateRecord(record);
			if (keep)
			{
				List<Redwood.Record> records = new List<Redwood.Record>();
				records.Add(record);
				return records;
			}
			else
			{
				return LogRecordHandler.Empty;
			}
		}

		/// <summary>Given a record, return true if it should be propagated to later handlers.</summary>
		public abstract bool PropagateRecord(Redwood.Record record);
	}
}

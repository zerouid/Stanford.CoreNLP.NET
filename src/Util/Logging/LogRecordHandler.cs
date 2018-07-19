using System.Collections.Generic;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A log message handler.</summary>
	/// <remarks>
	/// A log message handler. This can take the role of a filter, which blocks future handlers from
	/// receiving the message, or as an entity that produces a side effect based on the message
	/// (e.g. printing it to console or a file).
	/// All log Records pass through an ordered list of Handler objects; all operations done on a log
	/// Record are done by some handler or another.
	/// When writing filters, you should see
	/// <see cref="BooleanLogRecordHandler"/>
	/// instead which allows for a
	/// simpler interface.
	/// </remarks>
	/// <seealso cref="BooleanLogRecordHandler"/>
	public abstract class LogRecordHandler
	{
		/// <summary>An empty list to serve as the FALSE token for filters</summary>
		public static readonly IList<Redwood.Record> Empty = Java.Util.Collections.EmptyList();

		/// <summary>Handle a log Record, either as a filter or by producing a side effect.</summary>
		/// <param name="record">The log record to handle</param>
		/// <returns>a (possibly empty) list of records to be sent on in the pipeline</returns>
		public abstract IList<Redwood.Record> Handle(Redwood.Record record);

		/// <summary>Signal the start of a track, i.e.</summary>
		/// <remarks>Signal the start of a track, i.e. that we have descended a level deeper.</remarks>
		/// <param name="signal">
		/// A record corresponding to the information in the track header.
		/// The depth in this object is the old log depth.
		/// </param>
		/// <returns>
		/// A list of records to pass down the pipeline, not including the startTrack() signal.
		/// The returned records are passed to handle(), not startTrack(),
		/// and are sent before the startTrack() signal.
		/// </returns>
		public virtual IList<Redwood.Record> SignalStartTrack(Redwood.Record signal)
		{
			return Empty;
		}

		/// <summary>Signal the end of a track, i.e.</summary>
		/// <remarks>Signal the end of a track, i.e. that we have popped up to a higher level.</remarks>
		/// <param name="newDepth">The new depth; that is, the current depth - 1.</param>
		/// <returns>
		/// A list of records to pass down the pipeline.
		/// The returned records are passed to handle(), not endTrack().
		/// and are sent before the startTrack() signal.
		/// </returns>
		public virtual IList<Redwood.Record> SignalEndTrack(int newDepth, long timeEnded)
		{
			return Empty;
		}

		public virtual IList<Redwood.Record> SignalShutdown()
		{
			return Empty;
		}
	}
}

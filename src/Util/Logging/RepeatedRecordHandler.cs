using System;
using System.Collections.Generic;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>Filters repeated messages and replaces them with the number of times they were logged.</summary>
	/// <author>David McClosky,</author>
	/// <author>Gabor Angeli (angeli at cs.stanford): approximate record equality, repeated tracks squashed</author>
	public class RepeatedRecordHandler : LogRecordHandler
	{
		private readonly Stack<RepeatedRecordHandler.RepeatedRecordInfo> stack = new Stack<RepeatedRecordHandler.RepeatedRecordInfo>();

		internal RepeatedRecordHandler.RepeatedRecordInfo current = new RepeatedRecordHandler.RepeatedRecordInfo();

		private readonly RepeatedRecordHandler.IRepeatSemantics repeatSemantics;

		/// <summary>
		/// Create a new repeated log message handler, using the given semantics for what
		/// constitutes a repeated record.
		/// </summary>
		/// <param name="repeatSemantics">The semantics for what constitutes a repeated record</param>
		public RepeatedRecordHandler(RepeatedRecordHandler.IRepeatSemantics repeatSemantics)
		{
			this.repeatSemantics = repeatSemantics;
		}

		private void Flush(RepeatedRecordHandler.RepeatedRecordInfo info, IList<Redwood.Record> willReturn)
		{
			//(suppress all printing)
			if (info.suppressRecord)
			{
				return;
			}
			//(get time)
			int repeatedRecordCount = info.timesSeen - info.timesPrinted;
			if (repeatedRecordCount > 0)
			{
				//(send message record)
				//((add force tag))
				object[] newTags = new object[info.lastRecord.Channels().Length + 1];
				System.Array.Copy(info.lastRecord.Channels(), 0, newTags, 1, info.lastRecord.Channels().Length);
				newTags[0] = Redwood.Force;
				//((create record))
				Redwood.Record newRecord = new Redwood.Record(repeatSemantics.Message(repeatedRecordCount), newTags, info.lastRecord.depth, info.lastRecord.timesstamp);
				//((pass record))
				willReturn.Add(newRecord);
				info.timesSeen = 0;
				info.timesPrinted = 0;
			}
		}

		private void FlushParents(IList<Redwood.Record> willReturn)
		{
			Stack<RepeatedRecordHandler.RepeatedRecordInfo> reverseStack = new Stack<RepeatedRecordHandler.RepeatedRecordInfo>();
			while (!stack.IsEmpty())
			{
				reverseStack.Push(stack.Pop());
			}
			while (!reverseStack.IsEmpty())
			{
				RepeatedRecordHandler.RepeatedRecordInfo info = reverseStack.Pop();
				info.timesSeen -= 1;
				Flush(info, willReturn);
				stack.Push(info);
			}
		}

		private bool RecordVerdict(Redwood.Record r, bool isRepeat, bool shouldPrint, IList<Redwood.Record> willReturn)
		{
			if (r.Force())
			{
				FlushParents(willReturn);
				if (isRepeat)
				{
					Flush(current, willReturn);
				}
				//if not repeat, will flush below
				shouldPrint = true;
			}
			if (!isRepeat)
			{
				Flush(current, willReturn);
				current.lastRecord = r;
			}
			if (shouldPrint)
			{
				current.timeOfLastPrintedRecord = r.timesstamp;
				current.timesPrinted += 1;
			}
			current.timesSeen += 1;
			current.somethingPrinted = true;
			//(return)
			return shouldPrint;
		}

		private bool InternalHandle(Redwood.Record record, IList<Redwood.Record> willReturn)
		{
			// We are passing the record through a number of filters,
			// ordered by priority, to determine whether or not
			// to continue passing it on
			//--Special Cases
			//--Regular Cases
			//(ckeck squashing)
			if (this.current.suppressRecord)
			{
				return RecordVerdict(record, false, false, willReturn);
			}
			//arg 2 is irrelevant here
			//(check first record printed)
			if (this.current.lastRecord == null)
			{
				return RecordVerdict(record, false, true, willReturn);
			}
			//(check equality)
			if (this.repeatSemantics.Equals(current.lastRecord, record))
			{
				//(check time)
				long currentTime = record.timesstamp;
				if (currentTime - this.current.timeOfLastPrintedRecord > this.repeatSemantics.MaxWaitTimeInMillis())
				{
					return RecordVerdict(record, true, true, willReturn);
				}
				//(check num printed)
				if (this.current.timesSeen < this.repeatSemantics.NumToForcePrint())
				{
					return RecordVerdict(record, true, true, willReturn);
				}
				else
				{
					return RecordVerdict(record, true, false, willReturn);
				}
			}
			else
			{
				//(different record)
				return RecordVerdict(record, false, true, willReturn);
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> Handle(Redwood.Record record)
		{
			IList<Redwood.Record> willReturn = new List<Redwood.Record>();
			if (InternalHandle(record, willReturn))
			{
				willReturn.Add(record);
			}
			return willReturn;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalStartTrack(Redwood.Record signal)
		{
			//(handle record)
			IList<Redwood.Record> willReturn = new List<Redwood.Record>();
			bool isPrinting = InternalHandle(signal, willReturn);
			//(adjust state for track)
			if (!signal.Force())
			{
				if (isPrinting)
				{
					current.trackCountPending = RepeatedRecordHandler.PendingType.Printing;
					current.timesPrinted -= 1;
				}
				else
				{
					current.trackCountPending = RepeatedRecordHandler.PendingType.Seen;
				}
				current.timesSeen -= 1;
			}
			//(push stack)
			stack.Push(current);
			current = new RepeatedRecordHandler.RepeatedRecordInfo();
			if (!isPrinting)
			{
				current.suppressRecord = true;
			}
			return willReturn;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalEndTrack(int newDepth, long timeEnded)
		{
			IList<Redwood.Record> willReturn = new List<Redwood.Record>();
			//(get state info)
			bool trackWasNonempty = current.somethingPrinted;
			//(flush)
			Flush(current, willReturn);
			current = stack.Pop();
			//(update seen counts)
			if (trackWasNonempty)
			{
				if (current.trackCountPending == RepeatedRecordHandler.PendingType.Printing)
				{
					//((track was in fact printed))
					current.timesPrinted += 1;
				}
				if (current.trackCountPending != RepeatedRecordHandler.PendingType.None)
				{
					//((track was in fact seen))
					current.timesSeen += 1;
				}
				//((track is nonempty))
				current.somethingPrinted = true;
			}
			//(update this track)
			current.trackCountPending = RepeatedRecordHandler.PendingType.None;
			return willReturn;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override IList<Redwood.Record> SignalShutdown()
		{
			IList<Redwood.Record> willReturn = new List<Redwood.Record>();
			Flush(current, willReturn);
			return willReturn;
		}

		private enum PendingType
		{
			None,
			Printing,
			Seen
		}

		private class RepeatedRecordInfo
		{
			private Redwood.Record lastRecord = null;

			private int timesSeen = 0;

			private int timesPrinted = 0;

			private long timeOfLastPrintedRecord = 0L;

			private bool suppressRecord = false;

			private bool somethingPrinted = false;

			private RepeatedRecordHandler.PendingType trackCountPending = RepeatedRecordHandler.PendingType.None;
		}

		/// <summary>Determines the semantics of what constitutes a repeated record</summary>
		public interface IRepeatSemantics
		{
			bool Equals(Redwood.Record lastRecord, Redwood.Record newRecord);

			long MaxWaitTimeInMillis();

			int NumToForcePrint();

			string Message(int linesOmitted);
		}

		/// <summary>
		/// Judges two records to be equal if they come from the same place,
		/// and begin with the same string, modulo numbers
		/// </summary>
		public class ApproximateRepeatSemantics : RepeatedRecordHandler.IRepeatSemantics
		{
			private static bool SameMessage(string last, string current)
			{
				string lastNoNumbers = last.ReplaceAll("[0-9\\.\\-]+", "#");
				string currentNoNumbers = current.ReplaceAll("[0-9\\.\\-]+", "#");
				return lastNoNumbers.StartsWith(Sharpen.Runtime.Substring(currentNoNumbers, 0, Math.Min(7, currentNoNumbers.Length)));
			}

			public virtual bool Equals(Redwood.Record lastRecord, Redwood.Record record)
			{
				return Arrays.Equals(record.Channels(), lastRecord.Channels()) && SameMessage(lastRecord.content == null ? "null" : lastRecord.content.ToString(), record.content == null ? "null" : record.content.ToString());
			}

			public virtual long MaxWaitTimeInMillis()
			{
				return 1000;
			}

			public virtual int NumToForcePrint()
			{
				return 3;
			}

			public virtual string Message(int linesOmitted)
			{
				return "... " + linesOmitted + " similar messages";
			}
		}

		public static readonly RepeatedRecordHandler.ApproximateRepeatSemantics Approximate = new RepeatedRecordHandler.ApproximateRepeatSemantics();

		/// <summary>
		/// Judges two records to be equal if they are from the same place,
		/// and have the same message
		/// </summary>
		public class ExactRepeatSemantics : RepeatedRecordHandler.IRepeatSemantics
		{
			public virtual bool Equals(Redwood.Record lastRecord, Redwood.Record record)
			{
				return Arrays.Equals(record.Channels(), lastRecord.Channels()) && ((record.content == null && lastRecord.content == null) || (record.content != null && record.content.Equals(lastRecord.content)));
			}

			public virtual long MaxWaitTimeInMillis()
			{
				return long.MaxValue;
			}

			public virtual int NumToForcePrint()
			{
				return 1;
			}

			public virtual string Message(int linesOmitted)
			{
				return "(last message repeated " + linesOmitted + " times)";
			}
		}

		public static readonly RepeatedRecordHandler.ExactRepeatSemantics Exact = new RepeatedRecordHandler.ExactRepeatSemantics();
	}
}

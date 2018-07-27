using System.Collections.Generic;


namespace Edu.Stanford.Nlp.Util.Logging
{
	public class RerouteChannel : LogRecordHandler
	{
		private object oldChannelName;

		private object newChannelName;

		public RerouteChannel(object oldChannelName, object newChannelName)
		{
			this.oldChannelName = oldChannelName;
			this.newChannelName = newChannelName;
		}

		public override IList<Redwood.Record> Handle(Redwood.Record record)
		{
			IList<Redwood.Record> results = new List<Redwood.Record>();
			object[] channels = record.Channels();
			for (int i = 0; i < channels.Length; i++)
			{
				object channel = channels[i];
				if (oldChannelName.Equals(channel))
				{
					// make a new version of the Record with a different channel name
					channels[i] = newChannelName;
					Redwood.Record reroutedRecord = new Redwood.Record(record.content, channels, record.depth, record.timesstamp);
					results.Add(reroutedRecord);
					return results;
				}
			}
			// didn't find any matching records, so just return the original one
			results.Add(record);
			return results;
		}
	}
}

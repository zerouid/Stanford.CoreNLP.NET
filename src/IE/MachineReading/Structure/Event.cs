using System.Collections.Generic;


namespace Edu.Stanford.Nlp.IE.Machinereading.Structure
{
	/// <summary>Event holds a map from event to event mentions.</summary>
	/// <remarks>
	/// Event holds a map from event to event mentions. Assumes a single
	/// dataset.
	/// </remarks>
	public class Event
	{
		private IDictionary<string, IList<EventMention>> eventToEventMentions = new Dictionary<string, IList<EventMention>>();

		public virtual void AddEntity(string @event, EventMention em)
		{
			IList<EventMention> mentions = this.eventToEventMentions[@event];
			if (mentions == null)
			{
				mentions = new List<EventMention>();
				this.eventToEventMentions[@event] = mentions;
			}
			mentions.Add(em);
		}

		public virtual IList<EventMention> GetEventMentions(string @event)
		{
			IList<EventMention> retVal = this.eventToEventMentions[@event];
			return retVal != null ? retVal : Java.Util.Collections.EmptyList<EventMention>();
		}
	}
}

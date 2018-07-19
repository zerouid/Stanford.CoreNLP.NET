using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>Simple interface to determine if a Record matches a set of criteria.</summary>
	/// <remarks>
	/// Simple interface to determine if a Record matches a set of criteria.
	/// Inner classes provide some common filtering operations.  Other simple
	/// and generate purpose LogFilters should be added here as well.
	/// </remarks>
	/// <author>David McClosky</author>
	public interface ILogFilter
	{
		bool Matches(Redwood.Record message);

		public class HasChannel : ILogFilter
		{
			private object matchingChannel;

			public HasChannel(object message)
			{
				this.matchingChannel = message;
			}

			public virtual bool Matches(Redwood.Record record)
			{
				foreach (object tag in record.Channels())
				{
					if (tag.Equals(matchingChannel))
					{
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>Propagate records containing certain substrings.</summary>
		/// <remarks>
		/// Propagate records containing certain substrings.  Note that this
		/// doesn't require Records to have String messages since it will call
		/// toString() on them anyway.
		/// </remarks>
		public class ContainsMessage : ILogFilter
		{
			private string substring;

			public ContainsMessage(string message)
			{
				this.substring = message;
			}

			public virtual bool Matches(Redwood.Record record)
			{
				string content = record.content.ToString();
				return content.Contains(this.substring);
			}
		}

		/// <summary>Propagate records when Records match a specific message exactly (equals() is used for comparisons)</summary>
		public class MatchesMessage : ILogFilter
		{
			private object message;

			public MatchesMessage(object message)
			{
				this.message = message;
			}

			public virtual bool Matches(Redwood.Record record)
			{
				return record.content.Equals(message);
			}
		}
	}

	public static class LogFilterConstants
	{
	}
}

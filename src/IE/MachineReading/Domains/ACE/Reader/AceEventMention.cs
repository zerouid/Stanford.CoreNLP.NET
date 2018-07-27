using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>Stores one ACE event mention</summary>
	public class AceEventMention : AceMention
	{
		/// <summary>Maps argument roles to argument mentions</summary>
		private IDictionary<string, AceEventMentionArgument> mRolesToArguments;

		/// <summary>the parent event</summary>
		private AceEvent mParent;

		/// <summary>anchor text for this event</summary>
		private AceCharSeq mAnchor;

		public AceEventMention(string id, AceCharSeq extent, AceCharSeq anchor)
			: base(id, extent)
		{
			mRolesToArguments = Generics.NewHashMap();
			this.mAnchor = anchor;
		}

		public override string ToString()
		{
			return "AceEventMention [mAnchor=" + mAnchor + ", mParent=" + mParent + ", mRolesToArguments=" + mRolesToArguments + ", mExtent=" + mExtent + ", mId=" + mId + "]";
		}

		public virtual ICollection<AceEventMentionArgument> GetArgs()
		{
			return mRolesToArguments.Values;
		}

		public virtual ICollection<string> GetRoles()
		{
			return mRolesToArguments.Keys;
		}

		public virtual AceEntityMention GetArg(string role)
		{
			return mRolesToArguments[role].GetContent();
		}

		public virtual void AddArg(AceEntityMention em, string role)
		{
			mRolesToArguments[role] = new AceEventMentionArgument(role, em);
		}

		public virtual void SetParent(AceEvent e)
		{
			mParent = e;
		}

		public virtual AceEvent GetParent()
		{
			return mParent;
		}

		public virtual void SetAnchor(AceCharSeq anchor)
		{
			mAnchor = anchor;
		}

		public virtual AceCharSeq GetAnchor()
		{
			return mAnchor;
		}

		// TODO disabled until we tie in sentence boundaries
		// public int getSentence(AceDocument doc) {
		// return doc.getToken(getArg(0).getHead().getTokenStart()).getSentence();
		// }
		/// <summary>
		/// Returns the smallest start of all argument heads (or the beginning of the
		/// mention's extent if there are no arguments)
		/// </summary>
		public virtual int GetMinTokenStart()
		{
			ICollection<AceEventMentionArgument> args = GetArgs();
			int earliestTokenStart = -1;
			foreach (AceEventMentionArgument arg in args)
			{
				int tokenStart = arg.GetContent().GetHead().GetTokenStart();
				if (earliestTokenStart == -1)
				{
					earliestTokenStart = tokenStart;
				}
				else
				{
					earliestTokenStart = Math.Min(earliestTokenStart, tokenStart);
				}
			}
			// this will happen when we have no arguments
			if (earliestTokenStart == -1)
			{
				return mExtent.GetTokenStart();
			}
			return earliestTokenStart;
		}

		/// <summary>
		/// Returns the largest start of all argument heads (or the beginning of the
		/// mention's extent if there are no arguments)
		/// </summary>
		public virtual int GetMaxTokenEnd()
		{
			ICollection<AceEventMentionArgument> args = GetArgs();
			int latestTokenStart = -1;
			foreach (AceEventMentionArgument arg in args)
			{
				int tokenStart = arg.GetContent().GetHead().GetTokenStart();
				if (latestTokenStart == -1)
				{
					latestTokenStart = tokenStart;
				}
				else
				{
					latestTokenStart = Math.Max(latestTokenStart, tokenStart);
				}
			}
			// this will happen when we have no arguments
			if (latestTokenStart == -1)
			{
				return mExtent.GetTokenStart();
			}
			return latestTokenStart;
		}
		// TODO: toXml method
	}
}

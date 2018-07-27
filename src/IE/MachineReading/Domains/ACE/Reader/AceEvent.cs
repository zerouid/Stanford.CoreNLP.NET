using System.Collections.Generic;


namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	/// <summary>Stores one ACE event</summary>
	public class AceEvent : AceElement
	{
		private string mType;

		private string mSubtype;

		private string mModality;

		private string mPolarity;

		private string mGenericity;

		private string mTense;

		/// <summary>The list of mentions for this event</summary>
		private IList<AceEventMention> mMentions;

		public const string NilLabel = "nil";

		public AceEvent(string id, string type, string subtype, string modality, string polarity, string genericity, string tense)
			: base(id)
		{
			mType = type;
			mSubtype = subtype;
			mModality = modality;
			mPolarity = polarity;
			mGenericity = genericity;
			mTense = tense;
			mMentions = new List<AceEventMention>();
		}

		public virtual void AddMention(AceEventMention m)
		{
			mMentions.Add(m);
			m.SetParent(this);
		}

		public virtual AceEventMention GetMention(int which)
		{
			return mMentions[which];
		}

		public virtual int GetMentionCount()
		{
			return mMentions.Count;
		}

		public virtual string GetType()
		{
			return mType;
		}

		public virtual void SetType(string s)
		{
			mType = s;
		}

		public virtual string GetSubtype()
		{
			return mSubtype;
		}

		public virtual void SetSubtype(string s)
		{
			mSubtype = s;
		}

		public virtual string GetModality()
		{
			return mModality;
		}

		public virtual void SetModality(string modality)
		{
			this.mModality = modality;
		}

		public virtual string GetmPolarity()
		{
			return mPolarity;
		}

		public virtual void SetmPolarity(string mPolarity)
		{
			this.mPolarity = mPolarity;
		}

		public virtual string GetGenericity()
		{
			return mGenericity;
		}

		public virtual void SetGenericity(string genericity)
		{
			this.mGenericity = genericity;
		}

		public virtual string GetTense()
		{
			return mTense;
		}

		public virtual void SetTense(string tense)
		{
			this.mTense = tense;
		}
		// TODO: didn't implement toXml
	}
}

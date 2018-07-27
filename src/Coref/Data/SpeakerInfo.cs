using System.Collections.Generic;
using Edu.Stanford.Nlp.Math;




namespace Edu.Stanford.Nlp.Coref.Data
{
	/// <summary>Information about a speaker</summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class SpeakerInfo
	{
		private const long serialVersionUID = 7776098967746458031L;

		private readonly string speakerId;

		private string speakerName;

		private string[] speakerNameStrings;

		private string speakerDesc;

		private readonly ICollection<Mention> mentions = new LinkedHashSet<Mention>();

		private readonly bool speakerIdIsNumber;

		private readonly bool speakerIdIsAutoDetermined;

		private static readonly Pattern DefaultSpeakerPattern = Pattern.Compile("PER\\d+");

		public static readonly Pattern WhitespacePattern = Pattern.Compile("\\s+|_+");

		public SpeakerInfo(string speakerName)
		{
			// tokenized speaker name
			// Mentions that corresponds to the speaker...
			//  private Mention originalMention;            // the mention used when creating this SpeakerInfo
			// speaker id is a number (probably mention id)
			// speaker id was auto determined by system
			//  private Mention mainMention;
			// TODO: keep track of speaker utterances?
			this.speakerId = speakerName;
			int commaPos = speakerName.IndexOf(',');
			if (commaPos > 0)
			{
				// drop everything after the ,
				this.speakerName = Sharpen.Runtime.Substring(speakerName, 0, commaPos);
				if (commaPos < speakerName.Length)
				{
					speakerDesc = Sharpen.Runtime.Substring(speakerName, commaPos + 1);
					speakerDesc = speakerDesc.Trim();
					if (speakerDesc.IsEmpty())
					{
						speakerDesc = null;
					}
				}
			}
			else
			{
				this.speakerName = speakerName;
			}
			this.speakerNameStrings = WhitespacePattern.Split(this.speakerName);
			speakerIdIsNumber = NumberMatchingRegex.IsDecimalInteger(speakerId);
			speakerIdIsAutoDetermined = DefaultSpeakerPattern.Matcher(speakerId).Matches();
		}

		public virtual bool HasRealSpeakerName()
		{
			return mentions.Count > 0 || !(speakerIdIsAutoDetermined || speakerIdIsNumber);
		}

		public virtual string GetSpeakerName()
		{
			return speakerName;
		}

		public virtual string GetSpeakerDesc()
		{
			return speakerDesc;
		}

		public virtual string[] GetSpeakerNameStrings()
		{
			return speakerNameStrings;
		}

		public virtual ICollection<Mention> GetMentions()
		{
			return mentions;
		}

		public virtual bool ContainsMention(Mention m)
		{
			return mentions.Contains(m);
		}

		public virtual void AddMention(Mention m)
		{
			if (mentions.IsEmpty() && m.mentionType == Dictionaries.MentionType.Proper)
			{
				// check if mention name is probably better indicator of the speaker
				string mentionName = m.SpanToString();
				if (speakerIdIsNumber || speakerIdIsAutoDetermined)
				{
					string nerName = m.NerName();
					speakerName = (nerName != null) ? nerName : mentionName;
					speakerNameStrings = WhitespacePattern.Split(speakerName);
				}
			}
			mentions.Add(m);
		}

		public virtual int GetCorefClusterId()
		{
			int corefClusterId = -1;
			// Coref cluster id that corresponds to this speaker
			foreach (Mention m in mentions)
			{
				if (m.corefClusterID >= 0)
				{
					corefClusterId = m.corefClusterID;
					break;
				}
			}
			return corefClusterId;
		}

		public override string ToString()
		{
			return speakerId;
		}
	}
}

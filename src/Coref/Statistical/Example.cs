using Edu.Stanford.Nlp.Coref.Data;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Statistical
{
	/// <summary>A representation of a mention-pair for training coreference models.</summary>
	/// <author>Kevin Clark</author>
	[System.Serializable]
	public class Example
	{
		private const long serialVersionUID = 1104263558466004590L;

		public readonly int docId;

		public readonly double label;

		public readonly CompressedFeatureVector pairwiseFeatures;

		public readonly int mentionId1;

		public readonly int mentionId2;

		public readonly Dictionaries.MentionType mentionType1;

		public readonly Dictionaries.MentionType mentionType2;

		public Example(int docId, Mention m1, Mention m2, double label, CompressedFeatureVector pairwiseFeatures)
		{
			this.docId = docId;
			this.label = label;
			this.pairwiseFeatures = pairwiseFeatures;
			this.mentionId1 = m1.mentionID;
			this.mentionId2 = m2.mentionID;
			this.mentionType1 = m1.mentionType;
			this.mentionType2 = m2.mentionType;
		}

		public Example(Edu.Stanford.Nlp.Coref.Statistical.Example pair, bool isPositive)
		{
			this.docId = pair.docId;
			this.label = isPositive ? 1 : 0;
			this.pairwiseFeatures = null;
			this.mentionId1 = -1;
			this.mentionId2 = pair.mentionId2;
			this.mentionType1 = null;
			this.mentionType2 = pair.mentionType2;
		}

		public virtual bool IsNewLink()
		{
			return pairwiseFeatures == null;
		}
	}
}

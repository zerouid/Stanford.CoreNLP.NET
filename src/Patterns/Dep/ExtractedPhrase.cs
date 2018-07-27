using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Patterns.Dep
{
	[System.Serializable]
	public class ExtractedPhrase
	{
		private const long serialVersionUID = 1L;

		internal int startIndex;

		internal int endIndex;

		internal SemgrexPattern pattern;

		internal string value;

		internal double confidence = 1;

		internal string articleId = null;

		internal int sentId = null;

		internal ICounter<string> features;

		public ExtractedPhrase(int startIndex, int endIndex, SemgrexPattern pattern, string value)
			: this(startIndex, endIndex, pattern, value, 1.0, null, null, null)
		{
		}

		public ExtractedPhrase(int startIndex, int endIndex, SemgrexPattern pattern, string value, ICounter<string> features)
			: this(startIndex, endIndex, pattern, value, 1.0, null, null, features)
		{
		}

		public ExtractedPhrase(int startIndex, int endIndex, SemgrexPattern pattern, string value, double weight, string articleId, int sentId)
			: this(startIndex, endIndex, pattern, value, weight, articleId, sentId, null)
		{
		}

		public ExtractedPhrase(int startIndex, int endIndex, SemgrexPattern pattern, string value, double weight, string articleId, int sentId, ICounter<string> features)
		{
			this.startIndex = startIndex;
			this.endIndex = endIndex;
			this.pattern = pattern;
			this.value = value;
			this.confidence = weight;
			this.articleId = articleId;
			this.sentId = sentId;
			this.features = features;
		}

		public ExtractedPhrase(int startIndex, int endIndex, string value)
			: this(startIndex, endIndex, null, value)
		{
		}

		// public ExtractedPhrase(int startIndex, int endIndex) {
		// this(startIndex, endIndex, null, null);
		// }
		// public ExtractedPhrase(int startIndex, int endIndex, SemgrexPattern
		// pattern) {
		// this(startIndex, endIndex, pattern, null);
		// }
		internal virtual int GetStartIndex()
		{
			return this.startIndex;
		}

		internal virtual int GetEndIndex()
		{
			return this.endIndex;
		}

		public virtual IntPair GetIndices()
		{
			return new IntPair(startIndex, endIndex);
		}

		public virtual string GetValue()
		{
			return this.value;
		}

		public virtual SemgrexPattern GetPattern()
		{
			return this.pattern;
		}

		internal virtual void SetPattern(SemgrexPattern pattern)
		{
			this.pattern = pattern;
		}

		internal virtual void SetConfidence(double weight)
		{
			this.confidence = weight;
		}

		public override bool Equals(object o)
		{
			if (!(o is Edu.Stanford.Nlp.Patterns.Dep.ExtractedPhrase))
			{
				return false;
			}
			Edu.Stanford.Nlp.Patterns.Dep.ExtractedPhrase p = (Edu.Stanford.Nlp.Patterns.Dep.ExtractedPhrase)o;
			if (p.startIndex == this.startIndex && p.endIndex == this.endIndex && (this.value.Equals(p.value)))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return this.startIndex * 31 + this.endIndex + this.value.GetHashCode();
		}

		public virtual ICounter<string> GetFeatures()
		{
			return this.features;
		}

		public override string ToString()
		{
			return this.value + "(" + startIndex + "," + endIndex + "," + features + ")";
		}
	}
}

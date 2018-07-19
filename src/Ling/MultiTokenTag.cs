using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// Represents a tag for a multi token expression
	/// Can be used to annotate individual tokens without
	/// having nested annotations
	/// </summary>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class MultiTokenTag
	{
		private const long serialVersionUID = 1;

		public MultiTokenTag.Tag tag;

		public int index;

		[System.Serializable]
		public class Tag
		{
			private const long serialVersionUID = 1;

			public string name;

			public string tag;

			public int length;

			public Tag(string name, string tag, int length)
			{
				// total length of expression
				this.name = name;
				this.tag = tag;
				this.length = length;
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				MultiTokenTag.Tag tag1 = (MultiTokenTag.Tag)o;
				if (length != tag1.length)
				{
					return false;
				}
				if (!name.Equals(tag1.name))
				{
					return false;
				}
				if (!tag.Equals(tag1.tag))
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int result = name.GetHashCode();
				result = 31 * result + tag.GetHashCode();
				result = 31 * result + length;
				return result;
			}
		}

		public MultiTokenTag(MultiTokenTag.Tag tag, int index)
		{
			this.tag = tag;
			this.index = index;
		}

		public virtual bool IsStart()
		{
			return index == 0;
		}

		public virtual bool IsEnd()
		{
			return index == tag.length - 1;
		}

		public override string ToString()
		{
			return tag.name + "/" + tag.tag + "(" + index + "/" + tag.length + ")";
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (o == null || GetType() != o.GetType())
			{
				return false;
			}
			MultiTokenTag that = (MultiTokenTag)o;
			if (index != that.index)
			{
				return false;
			}
			if (!tag.Equals(that.tag))
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int result = tag.GetHashCode();
			result = 31 * result + index;
			return result;
		}
	}
}

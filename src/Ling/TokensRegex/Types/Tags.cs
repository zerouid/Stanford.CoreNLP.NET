using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Ling.Tokensregex.Types
{
	/// <summary>Tags that can be added to values or annotations</summary>
	[System.Serializable]
	public class Tags
	{
		public class TagsAnnotation : ICoreAnnotation<Tags>
		{
			public virtual Type GetType()
			{
				return typeof(Tags);
			}
		}

		internal IDictionary<string, IValue> tags;

		public Tags(params string[] tags)
		{
			if (tags != null)
			{
				this.tags = new Dictionary<string, IValue>();
				// Generics.newHashMap();
				foreach (string tag in tags)
				{
					this.tags[tag] = null;
				}
			}
		}

		public virtual ICollection<string> GetTags()
		{
			return tags.Keys;
		}

		public virtual bool HasTag(string tag)
		{
			return (tags != null) ? tags.Contains(tag) : false;
		}

		public virtual void SetTag(string tag, IValue v)
		{
			if (tags == null)
			{
				tags = new Dictionary<string, IValue>(1);
			}
			//Generics.newHashMap(1);
			tags[tag] = v;
		}

		public virtual void AddTag(string tag, IValue v)
		{
			if (tags == null)
			{
				tags = new Dictionary<string, IValue>(1);
			}
			//Generics.newHashMap(1);
			// Adds v as a tag into a list of tags...
			IList<IValue> tagList = null;
			if (tags.Contains(tag))
			{
				IValue oldValue = tags[tag];
				if (Expressions.TypeList.Equals(oldValue.GetType()))
				{
					tagList = ErasureUtils.UncheckedCast(oldValue.Get());
				}
				else
				{
					// Put the oldValue into a new array
					tagList = new List<IValue>();
					tagList.Add(oldValue);
					tags[tag] = Expressions.CreateValue(Expressions.TypeList, tagList);
				}
			}
			else
			{
				tagList = new List<IValue>();
				tags[tag] = Expressions.CreateValue(Expressions.TypeList, tagList);
			}
			tagList.Add(v);
		}

		public virtual void RemoveTag(string tag)
		{
			if (tags != null)
			{
				Sharpen.Collections.Remove(tags, tag);
			}
		}

		public virtual IValue GetTag(string tag)
		{
			return (tags != null) ? tags[tag] : null;
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Tags))
			{
				return false;
			}
			Tags tags1 = (Tags)o;
			if (tags != null ? !tags.Equals(tags1.tags) : tags1.tags != null)
			{
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return tags != null ? tags.GetHashCode() : 0;
		}

		private const long serialVersionUID = 2;
	}
}

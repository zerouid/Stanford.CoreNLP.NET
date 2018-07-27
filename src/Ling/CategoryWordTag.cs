using System;


namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>CategoryWordTag</code> object acts as a complex Label
	/// which contains a category, a head word, and a tag.
	/// </summary>
	/// <remarks>
	/// A <code>CategoryWordTag</code> object acts as a complex Label
	/// which contains a category, a head word, and a tag.
	/// The category label is the primary <code>value</code>
	/// </remarks>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class CategoryWordTag : StringLabel, IHasCategory, IHasWord, IHasTag
	{
		private const long serialVersionUID = -745085381666943254L;

		protected internal string word;

		protected internal string tag;

		/// <summary>
		/// If this is false, the tag and word are never printed in toString()
		/// calls.
		/// </summary>
		public static bool printWordTag = true;

		/// <summary>
		/// If set to true, when a terminal or preterminal has as its category
		/// something that is also the word or tag value, the latter are
		/// suppressed.
		/// </summary>
		public static bool suppressTerminalDetails;

		public CategoryWordTag()
			: base()
		{
		}

		/// <summary>This one argument constructor sets just the value.</summary>
		/// <param name="label">the string that will become the category/value</param>
		public CategoryWordTag(string label)
			: base(label)
		{
		}

		public CategoryWordTag(string category, string word, string tag)
			: base(category)
		{
			// = false;
			this.word = word;
			this.tag = tag;
		}

		/// <summary>Creates a new CategoryWordTag label from an existing label.</summary>
		/// <remarks>
		/// Creates a new CategoryWordTag label from an existing label.
		/// The oldLabel value() -- i.e., category -- is used for the new label.
		/// The tag and word
		/// are initialized iff the current label implements HasTag and HasWord
		/// respectively.
		/// </remarks>
		/// <param name="oldLabel">The label to use as a basis of this Label</param>
		public CategoryWordTag(ILabel oldLabel)
			: base(oldLabel)
		{
			if (oldLabel is IHasTag)
			{
				this.tag = ((IHasTag)oldLabel).Tag();
			}
			if (oldLabel is IHasWord)
			{
				this.word = ((IHasWord)oldLabel).Word();
			}
		}

		public virtual string Category()
		{
			return Value();
		}

		public virtual void SetCategory(string category)
		{
			SetValue(category);
		}

		public virtual string Word()
		{
			return word;
		}

		public virtual void SetWord(string word)
		{
			this.word = word;
		}

		public virtual string Tag()
		{
			return tag;
		}

		public virtual void SetTag(string tag)
		{
			this.tag = tag;
		}

		public virtual void SetCategoryWordTag(string category, string word, string tag)
		{
			SetCategory(category);
			SetWord(word);
			SetTag(tag);
		}

		/// <summary>Returns a <code>String</code> representation of the label.</summary>
		/// <remarks>
		/// Returns a <code>String</code> representation of the label.
		/// This attempts to be somewhat clever in choosing to print or
		/// suppress null components and the details of words or categories
		/// depending on the setting of <code>printWordTag</code> and
		/// <code>suppressTerminalDetails</code>.
		/// </remarks>
		/// <returns>The label as a string</returns>
		public override string ToString()
		{
			if (Category() != null)
			{
				if ((Word() == null || Tag() == null) || !printWordTag || (suppressTerminalDetails && (Word().Equals(Category()) || Tag().Equals(Category()))))
				{
					return Category();
				}
				else
				{
					return Category() + "[" + Word() + "/" + Tag() + "]";
				}
			}
			else
			{
				if (Tag() == null)
				{
					return Word();
				}
				else
				{
					return Word() + "/" + Tag();
				}
			}
		}

		/// <summary>Returns a <code>String</code> representation of the label.</summary>
		/// <remarks>
		/// Returns a <code>String</code> representation of the label.
		/// If the argument String is "full" then all components of the label
		/// are returned, and otherwise the normal toString() is returned.
		/// </remarks>
		/// <returns>The label as a string</returns>
		public virtual string ToString(string mode)
		{
			if ("full".Equals(mode))
			{
				return Category() + "[" + Word() + "/" + Tag() + "]";
			}
			return ToString();
		}

		/// <summary>Set everything by reversing a toString operation.</summary>
		/// <remarks>
		/// Set everything by reversing a toString operation.
		/// This should be added at some point.
		/// </remarks>
		public override void SetFromString(string labelStr)
		{
			throw new NotSupportedException();
		}

		private class LabelFactoryHolder
		{
			private LabelFactoryHolder()
			{
			}

			private static readonly ILabelFactory lf = new CategoryWordTagFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a factory for this kind of label
		/// (i.e., <code>CategoryWordTag</code>).
		/// </summary>
		/// <remarks>
		/// Return a factory for this kind of label
		/// (i.e., <code>CategoryWordTag</code>).
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The label factory</returns>
		public override ILabelFactory LabelFactory()
		{
			return CategoryWordTag.LabelFactoryHolder.lf;
		}

		/// <summary>Return a factory for this kind of label</summary>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return CategoryWordTag.LabelFactoryHolder.lf;
		}
	}
}

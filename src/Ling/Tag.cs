using Sharpen;

namespace Edu.Stanford.Nlp.Ling
{
	/// <summary>
	/// A <code>Tag</code> object acts as a Label by containing a
	/// <code>String</code> that is a part-of-speech tag.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2003/02/15 (implements TagFactory correctly now)</version>
	[System.Serializable]
	public class Tag : StringLabel, IHasTag
	{
		private const long serialVersionUID = 1143434026005416755L;

		/// <summary>Constructs a Tag object.</summary>
		public Tag()
			: base()
		{
		}

		/// <summary>Constructs a Tag object.</summary>
		/// <param name="tag">The tag name</param>
		public Tag(string tag)
			: base(tag)
		{
		}

		/// <summary>
		/// Creates a new tag whose tag value is the value of any
		/// class that supports the <code>Label</code> interface.
		/// </summary>
		/// <param name="lab">The label to be used as the basis of the new Tag</param>
		public Tag(ILabel lab)
			: base(lab)
		{
		}

		public virtual string Tag()
		{
			return Value();
		}

		public virtual void SetTag(string tag)
		{
			SetValue(tag);
		}

		/// <summary>
		/// A <code>TagFactory</code> acts as a factory for creating objects
		/// of class <code>Tag</code>
		/// </summary>
		private class TagFactory : ILabelFactory
		{
			public TagFactory()
			{
			}

			/// <summary>
			/// Create a new <code>Tag</code>, where the label is formed
			/// from the <code>String</code> passed in.
			/// </summary>
			/// <param name="cat">The cat that will go into the <code>Tag</code></param>
			public virtual ILabel NewLabel(string cat)
			{
				return new Tag(cat);
			}

			/// <summary>
			/// Create a new <code>Tag</code>, where the label is formed
			/// from the <code>String</code> passed in.
			/// </summary>
			/// <param name="cat">The cat that will go into the <code>Tag</code></param>
			/// <param name="options">is ignored by a TagFactory</param>
			public virtual ILabel NewLabel(string cat, int options)
			{
				return new Tag(cat);
			}

			/// <summary>
			/// Create a new <code>Tag</code>, where the label is formed
			/// from the <code>String</code> passed in.
			/// </summary>
			/// <param name="cat">The cat that will go into the <code>Tag</code></param>
			public virtual ILabel NewLabelFromString(string cat)
			{
				return new Tag(cat);
			}

			/// <summary>
			/// Create a new <code>Tag Label</code>, where the label is
			/// formed from
			/// the <code>Label</code> object passed in.
			/// </summary>
			/// <remarks>
			/// Create a new <code>Tag Label</code>, where the label is
			/// formed from
			/// the <code>Label</code> object passed in.  Depending on what fields
			/// each label has, other things will be <code>null</code>.
			/// </remarks>
			/// <param name="oldLabel">The Label that the new label is being created from</param>
			/// <returns>a new label of a particular type</returns>
			public virtual ILabel NewLabel(ILabel oldLabel)
			{
				return new Tag(oldLabel);
			}
		}

		private class LabelFactoryHolder
		{
			private static readonly ILabelFactory lf = new Tag.TagFactory();
			// extra class guarantees correct lazy loading (Bloch p.194)
		}

		/// <summary>
		/// Return a factory for this kind of label
		/// (i.e., <code>Tag</code>).
		/// </summary>
		/// <remarks>
		/// Return a factory for this kind of label
		/// (i.e., <code>Tag</code>).
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The label factory</returns>
		public override ILabelFactory LabelFactory()
		{
			return Tag.LabelFactoryHolder.lf;
		}

		/// <summary>
		/// Return a factory for this kind of label
		/// (i.e., <code>Tag</code>).
		/// </summary>
		/// <remarks>
		/// Return a factory for this kind of label
		/// (i.e., <code>Tag</code>).
		/// The factory returned is always the same one (a singleton).
		/// </remarks>
		/// <returns>The label factory</returns>
		public static ILabelFactory Factory()
		{
			return Tag.LabelFactoryHolder.lf;
		}
	}
}

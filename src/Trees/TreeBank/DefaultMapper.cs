


namespace Edu.Stanford.Nlp.Trees.Treebank
{
	/// <author>Spence Green</author>
	public class DefaultMapper : IMapper
	{
		public virtual bool CanChangeEncoding(string parent, string child)
		{
			return true;
		}

		public virtual string Map(string parent, string element)
		{
			return element;
		}

		public virtual void Setup(File path, params string[] options)
		{
		}
	}
}

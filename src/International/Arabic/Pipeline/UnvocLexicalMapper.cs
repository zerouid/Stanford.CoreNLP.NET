


namespace Edu.Stanford.Nlp.International.Arabic.Pipeline
{
	/// <summary>Returns</summary>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class UnvocLexicalMapper : DefaultLexicalMapper
	{
		private const long serialVersionUID = -8702531532523913125L;

		private static readonly Pattern decoration = Pattern.Compile("\\+|\\[.*\\]$");

		public override string Map(string parent, string element)
		{
			string cleanElement = decoration.Matcher(element).ReplaceAll(string.Empty);
			if (cleanElement.Equals(string.Empty))
			{
				return element;
			}
			return cleanElement;
		}
	}
}



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Creates an annotation from text</summary>
	/// <author>Angel Chang</author>
	public class TextAnnotationCreator : AbstractTextAnnotationCreator
	{
		/// <exception cref="System.IO.IOException"/>
		public override Annotation CreateFromText(string text)
		{
			return new Annotation(text);
		}
	}
}

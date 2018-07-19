using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Creates a annotation from an input source</summary>
	/// <author>Angel Chang</author>
	public interface IAnnotationCreator
	{
		/// <exception cref="System.IO.IOException"/>
		Annotation CreateFromText(string text);

		/// <exception cref="System.IO.IOException"/>
		Annotation CreateFromFile(string filename);

		/// <exception cref="System.IO.IOException"/>
		Annotation CreateFromFile(File file);

		/// <exception cref="System.IO.IOException"/>
		Annotation Create(InputStream stream);

		/// <exception cref="System.IO.IOException"/>
		Annotation Create(InputStream stream, string encoding);

		/// <exception cref="System.IO.IOException"/>
		Annotation Create(Reader reader);
	}
}

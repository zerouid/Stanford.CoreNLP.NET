using Edu.Stanford.Nlp.IO;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// Creates a stub implementation for creating annotation from
	/// various input sources using InputStream as the main input source
	/// </summary>
	/// <author>Angel Chang</author>
	public abstract class AbstractInputStreamAnnotationCreator : IAnnotationCreator
	{
		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation CreateFromText(string text)
		{
			return Create(new StringReader(text));
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation CreateFromFile(string filename)
		{
			InputStream stream = new BufferedInputStream(new FileInputStream(filename));
			Annotation anno = Create(stream);
			IOUtils.CloseIgnoringExceptions(stream);
			return anno;
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation CreateFromFile(File file)
		{
			return CreateFromFile(file.GetAbsolutePath());
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation Create(InputStream stream)
		{
			return Create(stream, "UTF-8");
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation Create(Reader reader)
		{
			// TODO: Is this okay?  If we are using this class, maybe we want byte-level stuff
			//  not character level
			return Create(new ReaderInputStream(reader));
		}

		public abstract Annotation Create(InputStream arg1, string arg2);
	}
}

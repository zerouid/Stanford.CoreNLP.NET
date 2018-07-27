using Edu.Stanford.Nlp.IO;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// Creates a stub implementation for creating annotation from
	/// various input sources using String as the main input source
	/// </summary>
	/// <author>Angel Chang</author>
	public abstract class AbstractTextAnnotationCreator : IAnnotationCreator
	{
		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation CreateFromFile(string filename)
		{
			Reader r = IOUtils.GetBufferedFileReader(filename);
			Annotation anno = Create(r);
			IOUtils.CloseIgnoringExceptions(r);
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
			return Create(new InputStreamReader(stream));
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation Create(InputStream stream, string encoding)
		{
			return Create(new InputStreamReader(stream, encoding));
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual Annotation Create(Reader reader)
		{
			string text = IOUtils.SlurpReader(reader);
			return CreateFromText(text);
		}

		public abstract Annotation CreateFromText(string arg1);
	}
}

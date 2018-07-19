using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// This is a convenience class which works almost exactly like
	/// <code>FileReader</code>
	/// but allows for the specification of input encoding.
	/// </summary>
	/// <author>Alex Kleeman</author>
	public class EncodingFileReader : InputStreamReader
	{
		private const string DefaultEncoding = "UTF-8";

		/// <summary>
		/// Creates a new <tt>EncodingFileReader</tt>, given the name of the
		/// file to read from.
		/// </summary>
		/// <param name="fileName">the name of the file to read from</param>
		/// <exception>
		/// java.io.FileNotFoundException
		/// if the named file does not
		/// exist, is a directory rather than a regular file,
		/// or for some other reason cannot be opened for
		/// reading.
		/// </exception>
		/// <exception>
		/// java.io.UnsupportedEncodingException
		/// if the encoding does not exist.
		/// </exception>
		/// <exception cref="Java.IO.UnsupportedEncodingException"/>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public EncodingFileReader(string fileName)
			: base(new FileInputStream(fileName), DefaultEncoding)
		{
		}

		/// <summary>
		/// Creates a new <tt>EncodingFileReader</tt>, given the name of the
		/// file to read from and an encoding
		/// </summary>
		/// <param name="fileName">the name of the file to read from</param>
		/// <param name="encoding"><tt>String</tt> specifying the encoding to be used</param>
		/// <exception>
		/// java.io.UnsupportedEncodingException
		/// if the encoding does not exist.
		/// </exception>
		/// <exception>
		/// java.io.FileNotFoundException
		/// if the named file does not exist,
		/// is a directory rather than a regular file,
		/// or for some other reason cannot be opened for
		/// reading.
		/// </exception>
		/// <exception cref="Java.IO.UnsupportedEncodingException"/>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public EncodingFileReader(string fileName, string encoding)
			: base(new FileInputStream(fileName), encoding == null ? DefaultEncoding : encoding)
		{
		}

		/// <summary>
		/// Creates a new <tt>EncodingFileReader</tt>, given the <tt>File</tt>
		/// to read from, and using default of utf-8.
		/// </summary>
		/// <param name="file">the <tt>File</tt> to read from</param>
		/// <exception>
		/// FileNotFoundException
		/// if the file does not exist,
		/// is a directory rather than a regular file,
		/// or for some other reason cannot be opened for
		/// reading.
		/// </exception>
		/// <exception>
		/// java.io.UnsupportedEncodingException
		/// if the encoding does not exist.
		/// </exception>
		/// <exception cref="Java.IO.UnsupportedEncodingException"/>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public EncodingFileReader(File file)
			: base(new FileInputStream(file), DefaultEncoding)
		{
		}

		/// <summary>
		/// Creates a new <tt>FileReader</tt>, given the <tt>File</tt>
		/// to read from and encoding.
		/// </summary>
		/// <param name="file">the <tt>File</tt> to read from</param>
		/// <param name="encoding"><tt>String</tt> specifying the encoding to be used</param>
		/// <exception>
		/// FileNotFoundException
		/// if the file does not exist,
		/// is a directory rather than a regular file,
		/// or for some other reason cannot be opened for
		/// reading.
		/// </exception>
		/// <exception>
		/// java.io.UnsupportedEncodingException
		/// if the encoding does not exist.
		/// </exception>
		/// <exception cref="Java.IO.UnsupportedEncodingException"/>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public EncodingFileReader(File file, string encoding)
			: base(new FileInputStream(file), encoding == null ? DefaultEncoding : encoding)
		{
		}

		/// <summary>
		/// Creates a new <tt>FileReader</tt>, given the
		/// <tt>FileDescriptor</tt> to read from.
		/// </summary>
		/// <param name="fd">the FileDescriptor to read from</param>
		public EncodingFileReader(FileDescriptor fd)
			: base(new FileInputStream(fd))
		{
		}
	}
}

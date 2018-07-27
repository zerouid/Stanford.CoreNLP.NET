using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.GZip;
using Microsoft.Extensions.Configuration;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Helper Class for various I/O related things.</summary>
	/// <author>Kayur Patel</author>
	/// <author>Teg Grenager</author>
	/// <author>Christopher Manning</author>
	public class IOUtils
	{
		private const int SlurpBufferSize = 16384;

		public static readonly string eolChar = Environment.NewLine;

		public const string defaultEncoding = "utf-8";

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.IO.IOUtils));

		private IOUtils()
		{
		}

		// todo: Inline
		// A class of static methods
		/// <summary>Write object to a file with the specified name.</summary>
		/// <remarks>Write object to a file with the specified name.  The file is silently gzipped if the filename ends with .gz.</remarks>
		/// <param name="o">Object to be written to file</param>
		/// <param name="filename">Name of the temp file</param>
		/// <exception cref="System.IO.IOException">If can't write file.</exception>
		/// <returns>FileInfo containing the object</returns>
		public static FileInfo WriteObjectToFile(object o, string filename)
		{
			return WriteObjectToFile(o, new FileInfo(filename));
		}

		/// <summary>Write an object to a specified FileInfo.</summary>
		/// <remarks>Write an object to a specified FileInfo.  The file is silently gzipped if the filename ends with .gz.</remarks>
		/// <param name="o">Object to be written to file</param>
		/// <param name="file">The temp FileInfo</param>
		/// <exception cref="System.IO.IOException">If FileInfo cannot be written</exception>
		/// <returns>FileInfo containing the object</returns>
		public static FileInfo WriteObjectToFile(object o, FileInfo file)
		{
			return WriteObjectToFile(o, file, false);
		}

		/// <summary>Write an object to a specified FileInfo.</summary>
		/// <remarks>Write an object to a specified FileInfo. The file is silently gzipped if the filename ends with .gz.</remarks>
		/// <param name="o">Object to be written to file</param>
		/// <param name="file">The temp FileInfo</param>
		/// <param name="append">If true, append to this file instead of overwriting it</param>
		/// <exception cref="System.IO.IOException">If FileInfo cannot be written</exception>
		/// <returns>FileInfo containing the object</returns>
		public static FileInfo WriteObjectToFile(object o, FileInfo file, bool append)
		{
			// file.createNewFile(); // cdm may 2005: does nothing needed
			Stream os = file.Open(append ? FileMode.Append : FileMode.Open, FileAccess.Write);
			if (file.Name.EndsWith(".gz"))
			{
				os = new GZipOutputStream(os);
			}
			using(os = new BufferedStream(os))
			{
				os.WriteObject(o);
			}
			return file;
		}

		/// <summary>Write object to a file with the specified name.</summary>
		/// <param name="o">Object to be written to file</param>
		/// <param name="filename">Name of the temp file</param>
		/// <returns>FileInfo containing the object, or null if an exception was caught</returns>
		public static FileInfo WriteObjectToFileNoExceptions(object o, string filename)
		{
			FileInfo file = null;
			Stream oos = null;
			try
			{
				file = new FileInfo(filename);
				// file.createNewFile(); // cdm may 2005: does nothing needed
				using(oos = new GZipOutputStream(file.OpenWrite()))
				{
					oos.WriteObject(o);
				}
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
			}
			finally
			{
				CloseIgnoringExceptions(oos);
			}
			return file;
		}

		/// <summary>Write object to temp file which is destroyed when the program exits.</summary>
		/// <param name="o">Object to be written to file</param>
		/// <param name="filename">Name of the temp file</param>
		/// <exception cref="System.IO.IOException">If file cannot be written</exception>
		/// <returns>FileInfo containing the object</returns>
		public static FileInfo WriteObjectToTempFile(object o, string filename)
		{
			filename = Path.GetTempFileName() + ".tmp";
			FileInfo file = new FileInfo(filename);
			//file.DeleteOnExit();
			WriteObjectToFile(o, file);
			return file;
		}

		/// <summary>Write object to a temp file and ignore exceptions.</summary>
		/// <param name="o">Object to be written to file</param>
		/// <param name="filename">Name of the temp file</param>
		/// <returns>FileInfo containing the object</returns>
		public static FileInfo WriteObjectToTempFileNoExceptions(object o, string filename)
		{
			try
			{
				return WriteObjectToTempFile(o, filename);
			}
			catch (Exception e)
			{
				logger.Error("Error writing object to file " + filename);
				logger.Err(ThrowableToStackTrace(e));
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		private static Stream GetBufferedOutputStream(string path)
		{
			Stream os = new BufferedStream(File.OpenWrite(path));
			if (path.EndsWith(".gz"))
			{
				os = new GZipOutputStream(os);
			}
			return os;
		}

		//++ todo [cdm, Aug 2012]: Do we need the below methods? They're kind of weird in unnecessarily bypassing using a Writer.
		/// <summary>Writes a string to a file.</summary>
		/// <param name="contents">The string to write</param>
		/// <param name="path">The file path</param>
		/// <param name="encoding">The encoding to encode in</param>
		/// <exception cref="System.IO.IOException">In case of failure</exception>
		public static void WriteStringToFile(string contents, string path, string encoding)
		{
			using(StreamWriter writer = new StreamWriter(GetBufferedOutputStream(path),Encoding.GetEncoding(encoding)))
			{
				writer.Write(contents);
			}
		}

		/// <summary>Writes a string to a file, squashing exceptions</summary>
		/// <param name="contents">The string to write</param>
		/// <param name="path">The file path</param>
		/// <param name="encoding">The encoding to encode in</param>
		public static void WriteStringToFileNoExceptions(string contents, string path, string encoding)
		{
			Stream os = null;
			try
			{
				if (path.EndsWith(".gz"))
				{
					os = new GZipOutputStream(File.OpenWrite(path));
				}
				else
				{
					os = new BufferedStream(File.OpenWrite(path));
				}
				using(var writer = new StreamWriter(os, Encoding.GetEncoding(encoding)) ) 
				{
					writer.Write(contents);
				}
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
			}
			finally
			{
				CloseIgnoringExceptions(os);
			}
		}

		/// <summary>Writes a string to a temporary file.</summary>
		/// <param name="contents">The string to write</param>
		/// <param name="path">The file path</param>
		/// <param name="encoding">The encoding to encode in</param>
		/// <exception cref="System.IO.IOException">In case of failure</exception>
		/// <returns>The FileInfo written to</returns>
		public static FileInfo WriteStringToTempFile(string contents, string path, string encoding)
		{
			Stream os;
			FileInfo tmp = new FileInfo(Path.GetTempFileName() + ".tmp");
			if (path.EndsWith(".gz"))
			{
				os = new GZipOutputStream(tmp.OpenWrite());
			}
			else
			{
				os = new BufferedStream(tmp.OpenWrite());
			}

			using(var writer = new StreamWriter(os, Encoding.GetEncoding(encoding)) ) 
			{
				writer.Write(contents);
			}
			return tmp;
		}

		/// <summary>Writes a string to a temporary file, as UTF-8</summary>
		/// <param name="contents">The string to write</param>
		/// <param name="path">The file path</param>
		/// <exception cref="System.IO.IOException">In case of failure</exception>
		public static void WriteStringToTempFile(string contents, string path)
		{
			WriteStringToTempFile(contents, path, "UTF-8");
		}

		/// <summary>Writes a string to a temporary file, squashing exceptions</summary>
		/// <param name="contents">The string to write</param>
		/// <param name="path">The file path</param>
		/// <param name="encoding">The encoding to encode in</param>
		/// <returns>The FileInfo that was written to</returns>
		public static FileInfo WriteStringToTempFileNoExceptions(string contents, string path, string encoding)
		{
			Stream os = null;
			FileInfo tmp = null;
			try
			{
				tmp = new FileInfo(Path.GetTempFileName() + ".tmp");
				if (path.EndsWith(".gz"))
				{
					os = new GZipOutputStream(tmp.OpenWrite());
				}
				else
				{
					os = new BufferedStream(tmp.OpenWrite());
				}
				using(var writer = new StreamWriter(os, Encoding.GetEncoding(encoding)) ) 
				{
					writer.Write(contents);
				}
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
			}
			finally
			{
				CloseIgnoringExceptions(os);
			}
			return tmp;
		}

		/// <summary>Writes a string to a temporary file with UTF-8 encoding, squashing exceptions</summary>
		/// <param name="contents">The string to write</param>
		/// <param name="path">The file path</param>
		public static void WriteStringToTempFileNoExceptions(string contents, string path)
		{
			WriteStringToTempFileNoExceptions(contents, path, "UTF-8");
		}

		//-- todo [cdm, Aug 2012]: Do we need the below methods? They're kind of weird in unnecessarily bypassing using a Writer.
		// todo [cdm, Sep 2013]: Can we remove this next method and its friends? (Weird in silently gzipping, overlaps other functionality.)
		/// <summary>Read an object from a stored file.</summary>
		/// <remarks>Read an object from a stored file. It is silently ungzipped, regardless of name.</remarks>
		/// <param name="file">The file pointing to the object to be retrieved</param>
		/// <exception cref="System.IO.IOException">If file cannot be read</exception>
		/// <exception cref="System.TypeLoadException">If reading serialized object fails</exception>
		/// <returns>The object read from the file.</returns>
		public static T ReadObjectFromFile<T>(FileInfo file)
		{
			try
			{
				using (Stream ois = new BufferedStream(new GZipInputStream(file.OpenRead())))
				{
					object o = ois.ReadObject();
					return (T)o;
				}
			}
			catch (SharpZipBaseException)
			{
				using (Stream ois_1 = new BufferedStream(file.OpenRead()))
				{
					object o = ois_1.ReadObject();
					return (T)o;
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static Stream GetDataInputStream(string filenameUrlOrClassPath)
		{
			return GetInputStreamFromURLOrClasspathOrFileSystem(filenameUrlOrClassPath);
		}

		/// <exception cref="System.IO.IOException"/>
		public static Stream GetDataOutputStream(string filename)
		{
			return GetBufferedOutputStream((filename));
		}

		/// <summary>Read an object from a stored file.</summary>
		/// <remarks>
		/// Read an object from a stored file.  The file can be anything obtained
		/// via a URL, the filesystem, or the classpath (eg in a jar file).
		/// </remarks>
		/// <param name="filename">The file pointing to the object to be retrieved</param>
		/// <exception cref="System.IO.IOException">If file cannot be read</exception>
		/// <exception cref="System.TypeLoadException">If reading serialized object fails</exception>
		/// <returns>The object read from the file.</returns>
		public static T ReadObjectFromURLOrClasspathOrFileSystem<T>(string filename)
		{
			using (Stream ois = GetInputStreamFromURLOrClasspathOrFileSystem(filename))
			{
				object o = ois.ReadObject();
				return (T)o;
			}
		}

		public static T ReadObjectAnnouncingTimingFromURLOrClasspathOrFileSystem<T>(Redwood.RedwoodChannels log, string msg, string path)
		{
			T obj;
			try
			{
				Timing timing = new Timing();
				obj = Edu.Stanford.Nlp.IO.IOUtils.ReadObjectFromURLOrClasspathOrFileSystem<T>(path);
				log.Info(msg + ' ' + path + " ... done [" + timing.ToSecondsString() + " sec].");
			}
			catch (Exception e)
			{
				throw new RuntimeIOException(e);
			}
			return obj;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static T ReadObjectFromObjectStream<T>(Stream ois)
		{
			object o = ois.ReadObject();
			return (T)o;
		}

		/// <summary>Read an object from a stored file.</summary>
		/// <param name="filename">The filename of the object to be retrieved</param>
		/// <exception cref="System.IO.IOException">If file cannot be read</exception>
		/// <exception cref="System.TypeLoadException">If reading serialized object fails</exception>
		/// <returns>The object read from the file.</returns>
		public static T ReadObjectFromFile<T>(string filename)
		{
			return ReadObjectFromFile<T>(new FileInfo(filename));
		}

		/// <summary>Read an object from a stored file without throwing exceptions.</summary>
		/// <param name="file">The file pointing to the object to be retrieved</param>
		/// <returns>The object read from the file, or null if an exception occurred.</returns>
		public static T ReadObjectFromFileNoExceptions<T>(FileInfo file)
		{
			object o = null;
			try
			{
				using(Stream ois = new BufferedStream(new GZipInputStream(file.OpenRead())))
				{
					o = ois.ReadObject();
				}
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
			}
			return (T)o;
		}

		/// <exception cref="System.IO.IOException"/>
		public static int LineCount(string textFileOrUrl)
		{
			using (var r = ReaderFromString(textFileOrUrl))
			{
				int numLines = 0;
				while (r.ReadLine() != null)
				{
					numLines++;
				}
				return numLines;
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static Stream WriteStreamFromString(string serializePath)
		{
			Stream oos;
			if (serializePath.EndsWith(".gz"))
			{
				oos = new BufferedStream(new GZipOutputStream(File.OpenWrite(serializePath)));
			}
			else
			{
				oos = new BufferedStream(File.OpenWrite(serializePath));
			}
			return oos;
		}

		/// <summary>Returns an ObjectInputStream reading from any of a URL, a CLASSPATH resource, or a file.</summary>
		/// <remarks>
		/// Returns an ObjectInputStream reading from any of a URL, a CLASSPATH resource, or a file.
		/// The CLASSPATH takes priority over the file system.
		/// This stream is buffered and, if necessary, gunzipped.
		/// </remarks>
		/// <param name="filenameOrUrl">The String specifying the URL/resource/file to load</param>
		/// <returns>An ObjectInputStream for loading a resource</returns>
		/// <exception cref="RuntimeIOException">On any IO error</exception>
		/// <exception cref="System.ArgumentNullException">Input parameter is null</exception>
		/// <exception cref="System.IO.IOException"/>
		public static Stream ReadStreamFromString(string filenameOrUrl)
		{
			return GetInputStreamFromURLOrClasspathOrFileSystem(filenameOrUrl);
		}

		/// <summary>Locates this file either in the CLASSPATH or in the file system.</summary>
		/// <remarks>
		/// Locates this file either in the CLASSPATH or in the file system. The CLASSPATH takes priority.
		/// Note that this method uses the ClassLoader methods, so that classpath resources must be specified as
		/// absolute resource paths without a leading "/".
		/// </remarks>
		/// <param name="name">The file or resource name</param>
		/// <exception cref="Java.IO.FileNotFoundException">If the file does not exist</exception>
		/// <returns>The InputStream of name, or null if not found</returns>
		private static Stream FindStreamInClasspathOrFileSystem(string name)
		{
			// ms 10-04-2010:
			// - even though this may look like a regular file, it may be a path inside a jar in the CLASSPATH
			// - check for this first. This takes precedence over the file system.
			Assembly asm = typeof(Edu.Stanford.Nlp.IO.IOUtils).Assembly;
			Stream @is = asm.GetManifestResourceStream(name);
			// windows FileInfo.separator is \, but getting resources only works with /
			if (@is == null)
			{
				@is = asm.GetManifestResourceStream(name.Replace("\\\\", "/"));
				// Classpath doesn't like double slashes (e.g., /home/user//foo.txt)
				if (@is == null)
				{
					@is = asm.GetManifestResourceStream(Regex.Replace(name.Replace("\\\\", "/"),"/+", "/"));
				}
			}
			// if not found in the CLASSPATH, load from the file system
			if (@is == null)
			{
				@is = File.OpenRead(name);
			}
			return @is;
		}

		/// <summary>Check if this path exists either in the classpath or on the filesystem.</summary>
		/// <param name="name">The file or resource name.</param>
		/// <returns>
		/// true if a call to
		/// <see cref="GetBufferedReaderFromClasspathOrFileSystem(string)"/>
		/// would return a valid stream.
		/// </returns>
		public static bool ExistsInClasspathOrFileSystem(string name)
		{
			Assembly asm = typeof(Edu.Stanford.Nlp.IO.IOUtils).Assembly;
			Stream @is = asm.GetManifestResourceStream(name);
			if (@is == null)
			{
				@is = asm.GetManifestResourceStream(name.Replace("\\\\", "/"));
				if (@is == null)
				{
					@is = asm.GetManifestResourceStream(Regex.Replace(name.Replace("\\\\", "/"),"/+", "/"));
				}
			}
			return @is != null || File.Exists(name);
		}

		/// <summary>
		/// Locates this file either using the given URL, or in the CLASSPATH, or in the file system
		/// The CLASSPATH takes priority over the file system!
		/// This stream is buffered and gunzipped (if necessary).
		/// </summary>
		/// <param name="textFileOrUrl">The String specifying the URL/resource/file to load</param>
		/// <returns>An InputStream for loading a resource</returns>
		/// <exception cref="System.IO.IOException">On any IO error</exception>
		/// <exception cref="System.ArgumentNullException">Input parameter is null</exception>
		public static Stream GetInputStreamFromURLOrClasspathOrFileSystem(string textFileOrUrl)
		{
			Stream @in;
			if (textFileOrUrl == null)
			{
				throw new ArgumentNullException("Attempt to open file with null name");
			}
			else
			{
				if (Regex.IsMatch(textFileOrUrl,"^https?://.*"))
				{
					WebRequest wr = WebRequest.Create(textFileOrUrl);
					@in = wr.GetResponse().GetResponseStream();
				}
				else
				{
					try
					{
						@in = FindStreamInClasspathOrFileSystem(textFileOrUrl);
					}
					catch (FileNotFoundException)
					{
						try
						{
							// Maybe this happens to be some other format of URL?
							WebRequest wr = WebRequest.Create(textFileOrUrl);
							@in = wr.GetResponse().GetResponseStream();
						}
						catch (IOException)
						{
							// Don't make the original exception a cause, since it is usually bogus
							throw new IOException("Unable to open \"" + textFileOrUrl + "\" as " + "class path, filename or URL");
						}
					}
				}
			}
			// , e2);
			// If it is a GZIP stream then ungzip it
			if (textFileOrUrl.EndsWith(".gz"))
			{
				try
				{
					@in = new GZipInputStream(@in);
				}
				catch (Exception e)
				{
					throw new RuntimeIOException("Resource or file looks like a gzip file, but is not: " + textFileOrUrl, e);
				}
			}
			// buffer this stream.  even gzip streams benefit from buffering,
			// such as for the shift reduce parser [cdm 2016: I think this is only because default buffer is small; see below]
			@in = new BufferedStream(@in);
			return @in;
		}

		// todo [cdm 2015]: I think GZIPInputStream has its own buffer and so we don't need to buffer in that case.
		// todo: Though it's default size is 512 bytes so need to make 8K in constructor. Or else buffering outside gzip is faster
		// todo: final InputStream is = new GZIPInputStream( new FileInputStream( file ), 65536 );
		/// <summary>Quietly opens a FileInfo.</summary>
		/// <remarks>
		/// Quietly opens a FileInfo. If the file ends with a ".gz" extension,
		/// automatically opens a GZIPInputStream to wrap the constructed
		/// FileInputStream.
		/// </remarks>
		/// <exception cref="Edu.Stanford.Nlp.IO.RuntimeIOException"/>
		public static Stream InputStreamFromFile(FileInfo file)
		{
			try
			{
				Stream @is = new BufferedStream(file.OpenRead());
				if (file.Name.EndsWith(".gz"))
				{
					@is = new GZipInputStream(@is);
				}
				return @is;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Open a BufferedReader to a FileInfo.</summary>
		/// <remarks>
		/// Open a BufferedReader to a FileInfo. If the file's getName() ends in .gz,
		/// it is interpreted as a gzipped file (and uncompressed). The file is then
		/// interpreted as a utf-8 text file.
		/// </remarks>
		/// <param name="file">What to read from</param>
		/// <returns>The BufferedReader</returns>
		/// <exception cref="RuntimeIOException">If there is an I/O problem</exception>
		public static StreamReader ReaderFromFile(FileInfo file)
		{
			Stream @is = null;
			try
			{
				@is = InputStreamFromFile(file);
				return new StreamReader(@is, Encoding.UTF8);
			}
			catch (IOException ioe)
			{
				Edu.Stanford.Nlp.IO.IOUtils.CloseIgnoringExceptions(@is);
				throw new RuntimeIOException(ioe);
			}
		}

		// todo [cdm 2014]: get rid of this method, using other methods. This will change the semantics to null meaning UTF-8, but that seems better in 2015.
		/// <summary>Open a BufferedReader to a FileInfo.</summary>
		/// <remarks>
		/// Open a BufferedReader to a FileInfo. If the file's getName() ends in .gz,
		/// it is interpreted as a gzipped file (and uncompressed). The file is then
		/// turned into a BufferedReader with the given encoding.
		/// If the encoding passed in is null, then the system default encoding is used.
		/// </remarks>
		/// <param name="file">What to read from</param>
		/// <param name="encoding">What charset to use. A null String is interpreted as platform default encoding</param>
		/// <returns>The BufferedReader</returns>
		/// <exception cref="RuntimeIOException">If there is an I/O problem</exception>
		public static StreamReader ReaderFromFile(FileInfo file, string encoding)
		{
			Stream @is = null;
			try
			{
				@is = InputStreamFromFile(file);
				if (encoding == null)
				{
					return new StreamReader(@is);
				}
				else
				{
					return new StreamReader(@is, Encoding.GetEncoding(encoding));
				}
			}
			catch (IOException ioe)
			{
				Edu.Stanford.Nlp.IO.IOUtils.CloseIgnoringExceptions(@is);
				throw new RuntimeIOException(ioe);
			}
		}

		/// <summary>Open a BufferedReader on stdin.</summary>
		/// <remarks>Open a BufferedReader on stdin. Use the user's default encoding.</remarks>
		/// <returns>The BufferedReader</returns>
		public static StreamReader ReaderFromStdin()
		{
			return new StreamReader(System.Console.OpenStandardInput());
		}

		/// <summary>Open a BufferedReader on stdin.</summary>
		/// <remarks>Open a BufferedReader on stdin. Use the specified character encoding.</remarks>
		/// <param name="encoding">
		/// CharSet encoding. Maybe be null, in which case the
		/// platform default encoding is used
		/// </param>
		/// <returns>The BufferedReader</returns>
		/// <exception cref="System.IO.IOException">If there is an I/O problem</exception>
		public static StreamReader ReaderFromStdin(string encoding)
		{
			if (encoding == null)
			{
				return new StreamReader(System.Console.OpenStandardInput());
			}
			return new StreamReader(System.Console.OpenStandardInput(), Encoding.GetEncoding(encoding));
		}

		// TODO [cdm 2015]: Should we rename these methods. Sort of misleading: They really read files, resources, etc. specified by a String
		/// <summary>Open a BufferedReader to a file, class path entry or URL specified by a String name.</summary>
		/// <remarks>
		/// Open a BufferedReader to a file, class path entry or URL specified by a String name.
		/// If the String starts with https?://, then it is first tried as a URL. It
		/// is next tried as a resource on the CLASSPATH, and then it is tried
		/// as a local file. Finally, it is then tried again in case it is some network-available
		/// file accessible by URL. If the String ends in .gz, it
		/// is interpreted as a gzipped file (and uncompressed). The file is then
		/// interpreted as a utf-8 text file.
		/// Note that this method uses the ClassLoader methods, so that classpath resources must be specified as
		/// absolute resource paths without a leading "/".
		/// </remarks>
		/// <param name="textFileOrUrl">What to read from</param>
		/// <returns>The BufferedReader</returns>
		/// <exception cref="System.IO.IOException">If there is an I/O problem</exception>
		public static StreamReader ReaderFromString(string textFileOrUrl)
		{
			return new StreamReader(GetInputStreamFromURLOrClasspathOrFileSystem(textFileOrUrl), Encoding.UTF8);
		}

		/// <summary>Open a BufferedReader to a file or URL specified by a String name.</summary>
		/// <remarks>
		/// Open a BufferedReader to a file or URL specified by a String name. If the
		/// String starts with https?://, then it is first tried as a URL, otherwise it
		/// is next tried as a resource on the CLASSPATH, and then finally it is tried
		/// as a local file or other network-available file . If the String ends in .gz, it
		/// is interpreted as a gzipped file (and uncompressed), else it is interpreted as
		/// a regular text file in the given encoding.
		/// If the encoding passed in is null, then the system default encoding is used.
		/// </remarks>
		/// <param name="textFileOrUrl">What to read from</param>
		/// <param name="encoding">
		/// CharSet encoding. Maybe be null, in which case the
		/// platform default encoding is used
		/// </param>
		/// <returns>The BufferedReader</returns>
		/// <exception cref="System.IO.IOException">If there is an I/O problem</exception>
		public static StreamReader ReaderFromString(string textFileOrUrl, string encoding)
		{
			Stream @is = GetInputStreamFromURLOrClasspathOrFileSystem(textFileOrUrl);
			if (encoding == null)
			{
				return new StreamReader(@is);
			}
			return new StreamReader(@is, Encoding.GetEncoding(encoding));
		}

		/// <summary>Returns an Iterable of the lines in the file.</summary>
		/// <remarks>
		/// Returns an Iterable of the lines in the file.
		/// The file reader will be closed when the iterator is exhausted. IO errors
		/// will throw an (unchecked) RuntimeIOException
		/// </remarks>
		/// <param name="path">The file whose lines are to be read.</param>
		/// <returns>An Iterable containing the lines from the file.</returns>
		public static IEnumerable<string> ReadLines(string path)
		{
			return ReadLines(path, null);
		}

		/// <summary>Returns an Iterable of the lines in the file.</summary>
		/// <remarks>
		/// Returns an Iterable of the lines in the file.
		/// The file reader will be closed when the iterator is exhausted. IO errors
		/// will throw an (unchecked) RuntimeIOException
		/// </remarks>
		/// <param name="path">The file whose lines are to be read.</param>
		/// <param name="encoding">The encoding to use when reading lines.</param>
		/// <returns>An Iterable containing the lines from the file.</returns>
		public static IEnumerable<string> ReadLines(string path, string encoding)
		{
			return new IOUtils.GetLinesIterable(path, null, encoding);
		}

		/// <summary>Returns an Iterable of the lines in the file.</summary>
		/// <remarks>
		/// Returns an Iterable of the lines in the file.
		/// The file reader will be closed when the iterator is exhausted.
		/// </remarks>
		/// <param name="file">The file whose lines are to be read.</param>
		/// <returns>An Iterable containing the lines from the file.</returns>
		public static IEnumerable<string> ReadLines(FileInfo file)
		{
			return ReadLines(file, null, null);
		}

		/// <summary>Returns an Iterable of the lines in the file.</summary>
		/// <remarks>
		/// Returns an Iterable of the lines in the file.
		/// The file reader will be closed when the iterator is exhausted.
		/// </remarks>
		/// <param name="file">The file whose lines are to be read.</param>
		/// <param name="fileInputStreamWrapper">
		/// The class to wrap the InputStream with, e.g. GZIPInputStream. Note
		/// that the class must have a constructor that accepts an
		/// InputStream.
		/// </param>
		/// <returns>An Iterable containing the lines from the file.</returns>
		public static IEnumerable<string> ReadLines(FileInfo file, Type fileInputStreamWrapper)
		{
			return ReadLines(file, fileInputStreamWrapper, null);
		}

		/// <summary>
		/// Returns an Iterable of the lines in the file, wrapping the generated
		/// FileInputStream with an instance of the supplied class.
		/// </summary>
		/// <remarks>
		/// Returns an Iterable of the lines in the file, wrapping the generated
		/// FileInputStream with an instance of the supplied class. IO errors will
		/// throw an (unchecked) RuntimeIOException
		/// </remarks>
		/// <param name="file">The file whose lines are to be read.</param>
		/// <param name="fileInputStreamWrapper">
		/// The class to wrap the InputStream with, e.g. GZIPInputStream. Note
		/// that the class must have a constructor that accepts an
		/// InputStream.
		/// </param>
		/// <param name="encoding">The encoding to use when reading lines.</param>
		/// <returns>An Iterable containing the lines from the file.</returns>
		public static IEnumerable<string> ReadLines(FileInfo file, Type fileInputStreamWrapper, string encoding)
		{
			return new IOUtils.GetLinesIterable(file, fileInputStreamWrapper, encoding);
		}

		internal class GetLinesIterable : IEnumerable<string>
		{
			internal readonly FileInfo file;

			internal readonly string path;

			internal readonly Type fileInputStreamWrapper;

			internal readonly string encoding;

			internal GetLinesIterable(FileInfo file, Type fileInputStreamWrapper, string encoding)
			{
				// TODO: better programming style would be to make this two
				// separate classes, but we don't expect to make more versions of
				// this class anyway
				this.file = file;
				this.path = null;
				this.fileInputStreamWrapper = fileInputStreamWrapper;
				this.encoding = encoding;
			}

			internal GetLinesIterable(string path, Type fileInputStreamWrapper, string encoding)
			{
				this.file = null;
				this.path = path;
				this.fileInputStreamWrapper = fileInputStreamWrapper;
				this.encoding = encoding;
			}

			/// <exception cref="System.IO.IOException"/>
			private Stream GetStream()
			{
				if (file != null)
				{
					return InputStreamFromFile(file);
				}
				else
				{
					if (path != null)
					{
						return GetInputStreamFromURLOrClasspathOrFileSystem(path);
					}
					else
					{
						throw new Exception("No known path to read");
					}
				}
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			public virtual IEnumerator<string> GetEnumerator()
			{
				return new _IEnumerator_758(this);
			}

			private sealed class _IEnumerator_758 : IEnumerator<string>
			{
				public _IEnumerator_758(GetLinesIterable _enclosing)
				{
					this._enclosing = _enclosing;
					this.reader = this.GetReader();
					this.line = this.GetLine();
					this.readerOpen = true;
				}

				protected internal readonly StreamReader reader;

				protected internal string line;

				private bool readerOpen;

				public bool MoveNext()
				{
					return this.line != null;
				}

				object IEnumerator.Current
				{
					get { return this.Current; }
				}
				public string Current
				{
					get
					{
						string nextLine = this.line;
						if (nextLine == null)
						{
							throw new InvalidOperationException();
						}
						this.line = this.GetLine();
						return nextLine;
					}
				}
				
				public void Reset()
				{
					throw new NotSupportedException();
				}

				protected internal string GetLine()
				{
					try
					{
						string result = this.reader.ReadLine();
						if (result == null)
						{
							this.readerOpen = false;
							this.reader.Close();
						}
						return result;
					}
					catch (IOException e)
					{
						throw new RuntimeIOException(e);
					}
				}

				protected internal StreamReader GetReader()
				{
					try
					{
						Stream stream = this._enclosing.GetStream();
						if (this._enclosing.fileInputStreamWrapper != null)
						{
							stream = (Stream)this._enclosing.fileInputStreamWrapper.GetConstructor(new [] { typeof(Stream) }).Invoke(new [] { stream });
						}
						if (this._enclosing.encoding == null)
						{
							return new StreamReader(stream);
						}
						else
						{
							return new StreamReader(stream, Encoding.GetEncoding(this._enclosing.encoding));
						}
					}
					catch (Exception e)
					{
						throw new RuntimeIOException(e);
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly GetLinesIterable _enclosing;

                #region IDisposable Support
                private bool disposedValue = false; // To detect redundant calls

                void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
							if (this.readerOpen)
							{
								IOUtils.logger.Warn("Forgot to close FileIterable -- closing from finalize()");
								this.reader.Dispose();
							}
                        }
                        disposedValue = true;
                    }
                }

                // This code added to correctly implement the disposable pattern.
                public void Dispose()
                {
                    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                    Dispose(true);
                }
                #endregion
            }
		}

		// end static class GetLinesIterable
		/// <summary>Given a reader, returns the lines from the reader as an Iterable.</summary>
		/// <param name="r">input reader</param>
		/// <param name="includeEol">whether to keep eol-characters in the returned strings</param>
		/// <returns>iterable of lines (as strings)</returns>
		public static IEnumerable<string> GetLineIterable(StreamReader r, bool includeEol)
		{
			if (includeEol)
			{
				return new IOUtils.EolPreservingLineReaderIterable(r);
			}
			else
			{
				return new IOUtils.LineReaderIterable(r);
			}
		}

		public static IEnumerable<string> GetLineIterable(StreamReader r, int bufferSize, bool includeEol)
		{
			if (includeEol)
			{
				return new IOUtils.EolPreservingLineReaderIterable(r, bufferSize);
			}
			else
			{
				return new IOUtils.LineReaderIterable(r);
			}
		}

		/// <summary>
		/// Line iterator that uses BufferedReader.readLine()
		/// EOL-characters are automatically discarded and not included in the strings returns
		/// </summary>
		private sealed class LineReaderIterable : IEnumerable<string>
		{
			private readonly StreamReader reader;

			public LineReaderIterable(StreamReader reader)
			{
				this.reader = reader;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			public IEnumerator<string> GetEnumerator()
			{
				return new _IEnumerator_864(this);
			}

			private sealed class _IEnumerator_864 : IEnumerator<string>
			{
				public _IEnumerator_864(LineReaderIterable _enclosing)
				{
					this._enclosing = _enclosing;
					this.next = this.GetNext();
				}

				private string next;

				private string GetNext()
				{
					try
					{
						return this._enclosing.reader.ReadLine();
					}
					catch (IOException ex)
					{
						throw new RuntimeIOException(ex);
					}
				}

				public bool MoveNext()
				{
					return this.next != null;
				}

				object IEnumerator.Current 
				{
					get { return this.Current; }
				}
				public string Current
				{
					get
					{
						string nextLine = this.next;
						if (nextLine == null)
						{
							throw new InvalidOperationException();
						}
						this.next = this.GetNext();
						return nextLine;
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

                public void Reset()
                {
                    throw new NotSupportedException();
                }

                private readonly LineReaderIterable _enclosing;

                #region IDisposable Support
                private bool disposedValue = false; // To detect redundant calls

                void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            // TODO: dispose managed state (managed objects).
                        }
                        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                        // TODO: set large fields to null.

                        disposedValue = true;
                    }
                }

                // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
                // ~_IEnumerator_864() {
                //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                //   Dispose(false);
                // }

                // This code added to correctly implement the disposable pattern.
                public void Dispose()
                {
                    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                    Dispose(true);
                    // TODO: uncomment the following line if the finalizer is overridden above.
                    // GC.SuppressFinalize(this);
                }
                #endregion
            }
		}

		/// <summary>Line iterator that preserves the eol-character exactly as read from reader.</summary>
		/// <remarks>
		/// Line iterator that preserves the eol-character exactly as read from reader.
		/// Line endings are: \r\n,\n,\r
		/// Lines returns by this iterator will include the eol-characters
		/// </remarks>
		private sealed class EolPreservingLineReaderIterable : IEnumerable<string>
		{
			private readonly StreamReader reader;

			private readonly int bufferSize;

			public EolPreservingLineReaderIterable(StreamReader reader)
				: this(reader, SlurpBufferSize)
			{
			}

			public EolPreservingLineReaderIterable(StreamReader reader, int bufferSize)
			{
				this.reader = reader;
				this.bufferSize = bufferSize;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			public IEnumerator<string> GetEnumerator()
			{
				return new _IEnumerator_921(this);
			}

			private sealed class _IEnumerator_921 : IEnumerator<string>
			{
				public _IEnumerator_921(EolPreservingLineReaderIterable _enclosing)
				{
					this._enclosing = _enclosing;
					this.done = false;
					this.sb = new StringBuilder(80);
					this.charBuffer = new char[this._enclosing.bufferSize];
					this.charBufferPos = -1;
					this.charsInBuffer = 0;
					this.lastWasLF = false;
				}

				private string next;

				private bool done;

				private StringBuilder sb;

				private char[] charBuffer;

				private int charBufferPos;

				private int charsInBuffer;

				internal bool lastWasLF;

				private string GetNext()
				{
					try
					{
						while (true)
						{
							if (this.charBufferPos < 0)
							{
								this.charsInBuffer = this._enclosing.reader.Read(this.charBuffer, 0, this.charBuffer.Length);
								if (this.charsInBuffer < 0)
								{
									// No more!!!
									if (this.sb.Length > 0)
									{
										string line = this.sb.ToString();
										// resets the buffer
										this.sb.Length = 0;
										return line;
									}
									else
									{
										return null;
									}
								}
								this.charBufferPos = 0;
							}
							bool eolReached = this.CopyUntilEol();
							if (eolReached)
							{
								// eol reached
								string line = this.sb.ToString();
								// resets the buffer
								this.sb.Length = 0;
								return line;
							}
						}
					}
					catch (IOException ex)
					{
						throw new RuntimeIOException(ex);
					}
				}

				private bool CopyUntilEol()
				{
					for (int i = this.charBufferPos; i < this.charsInBuffer; i++)
					{
						if (this.charBuffer[i] == '\n')
						{
							// line end
							// copy into our string builder
							this.sb.Append(this.charBuffer, this.charBufferPos, i - this.charBufferPos + 1);
							// advance character buffer pos
							this.charBufferPos = i + 1;
							this.lastWasLF = false;
							return true;
						}
						else
						{
							// end of line reached
							if (this.lastWasLF)
							{
								// not a '\n' here - still need to terminate line (but don't include current character)
								if (i > this.charBufferPos)
								{
									this.sb.Append(this.charBuffer, this.charBufferPos, i - this.charBufferPos);
									// advance character buffer pos
									this.charBufferPos = i;
									this.lastWasLF = false;
									return true;
								}
							}
						}
						// end of line reached
						this.lastWasLF = (this.charBuffer[i] == '\r');
					}
					this.sb.Append(this.charBuffer, this.charBufferPos, this.charsInBuffer - this.charBufferPos);
					// reset character buffer pos
					this.charBufferPos = -1;
					return false;
				}

				public bool MoveNext()
				{
					if (this.done)
					{
						return false;
					}
					if (this.next == null)
					{
						this.next = this.GetNext();
					}
					if (this.next == null)
					{
						this.done = true;
					}
					return !this.done;
				}

				object IEnumerator.Current
				{
					get { return this.Current; }
				}
				public string Current
				{
					get
					{
						if (!this.MoveNext())
						{
							throw new InvalidOperationException();
						}
						string res = this.next;
						this.next = null;
						return res;
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

                public void Reset()
                {
                    throw new NotSupportedException();
                }

                private readonly EolPreservingLineReaderIterable _enclosing;

                #region IDisposable Support
                private bool disposedValue = false; // To detect redundant calls

                void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            // TODO: dispose managed state (managed objects).
                        }

                        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                        // TODO: set large fields to null.

                        disposedValue = true;
                    }
                }

                // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
                // ~_IEnumerator_921() {
                //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                //   Dispose(false);
                // }

                // This code added to correctly implement the disposable pattern.
                public void Dispose()
                {
                    // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                    Dispose(true);
                    // TODO: uncomment the following line if the finalizer is overridden above.
                    // GC.SuppressFinalize(this);
                }
                #endregion
            }
			// end iterator()
		}

		// end static class EolPreservingLineReaderIterable
		/// <summary>
		/// Provides an implementation of closing a file for use in a finally block so
		/// you can correctly close a file without even more exception handling stuff.
		/// </summary>
		/// <remarks>
		/// Provides an implementation of closing a file for use in a finally block so
		/// you can correctly close a file without even more exception handling stuff.
		/// From a suggestion in a talk by Josh Bloch. Calling close() will flush().
		/// </remarks>
		/// <param name="c">The IO resource to close (e.g., a Stream/Reader)</param>
		public static void CloseIgnoringExceptions(IDisposable c)
		{
			if (c != null)
			{
				try
				{
					c.Dispose();
				}
				catch (IOException)
				{
				}
			}
		}

		// ignore
		/// <summary>Iterate over all the files in the directory, recursively.</summary>
		/// <param name="dir">The root directory.</param>
		/// <returns>All files within the directory.</returns>
		public static IEnumerable<FileInfo> IterFilesRecursive(FileInfo dir)
		{
			return IterFilesRecursive(dir, (Regex)null);
		}

		/// <summary>Iterate over all the files in the directory, recursively.</summary>
		/// <param name="dir">The root directory.</param>
		/// <param name="ext">A string that must be at the end of all files (e.g. ".txt")</param>
		/// <returns>All files within the directory ending in the given extension.</returns>
		public static IEnumerable<FileInfo> IterFilesRecursive(FileInfo dir, string ext)
		{
			return IterFilesRecursive(dir, new Regex(Regex.Escape(ext) + "$"));
		}

		/// <summary>Iterate over all the files in the directory, recursively.</summary>
		/// <param name="dir">The root directory.</param>
		/// <param name="pattern">
		/// A regular expression that the file path must match. This uses
		/// Matcher.find(), so use ^ and $ to specify endpoints.
		/// </param>
		/// <returns>All files within the directory.</returns>
		public static IEnumerable<FileInfo> IterFilesRecursive(FileInfo dir, Regex pattern)
		{
			return new _IEnumerable_1073(dir, pattern);
		}

		private sealed class _IEnumerable_1073 : IEnumerable<FileInfo>
		{
			public _IEnumerable_1073(FileInfo dir, Regex pattern)
			{
				this.dir = dir;
				this.pattern = pattern;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			public IEnumerator<FileInfo> GetEnumerator()
			{
				return new _AbstractIterator_1075(dir, pattern);
			}

			private sealed class _AbstractIterator_1075 : AbstractIterator<FileInfo>
			{
				public _AbstractIterator_1075(FileInfo dir, Regex pattern)
				{
					this.dir = dir;
					this.pattern = pattern;
					this.files = new Queue<FileInfo>(new [] { dir });
					this.file = this.FindNext();
				}

				private readonly Queue<FileInfo> files;

				private FileInfo file;

				public override bool MoveNext()
				{
					return this.file != null;
				}

				public override FileInfo Current
				{
					get
					{
						FileInfo result = this.file;
						if (result == null)
						{
							throw new InvalidOperationException();
						}
						this.file = this.FindNext();
						return result;
					}
				}

				private FileInfo FindNext()
				{
					FileInfo next = null;
					while (this.files.Count > 0 && next == null)
					{
						next = this.files.Dequeue();
						if (next.Attributes == FileAttributes.Directory)
						{
							foreach(var f in Directory.EnumerateFiles(next.FullName).Select(_ => new FileInfo(_)))
								this.files.Enqueue(f);
							next = null;
						}
						else
						{
							if (pattern != null)
							{
								if (!pattern.IsMatch(next.FullName))
								{
									next = null;
								}
							}
						}
					}
					return next;
				}

				private readonly FileInfo dir;

				private readonly Regex pattern;
			}

			private readonly FileInfo dir;

			private readonly Regex pattern;
		}

		/// <summary>Returns all the text in the given FileInfo as a single String.</summary>
		/// <remarks>
		/// Returns all the text in the given FileInfo as a single String.
		/// If the file's name ends in .gz, it is assumed to be gzipped and is silently uncompressed.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpFile(FileInfo file)
		{
			return SlurpFile(file, null);
		}

		/// <summary>Returns all the text in the given FileInfo as a single String.</summary>
		/// <remarks>
		/// Returns all the text in the given FileInfo as a single String.
		/// If the file's name ends in .gz, it is assumed to be gzipped and is silently uncompressed.
		/// </remarks>
		/// <param name="file">The file to read from</param>
		/// <param name="encoding">
		/// The character encoding to assume.  This may be null, and
		/// the platform default character encoding is used.
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpFile(FileInfo file, string encoding)
		{
			return IOUtils.SlurpReader(IOUtils.EncodedInputStreamReader(InputStreamFromFile(file), encoding));
		}

		/// <summary>Returns all the text in the given FileInfo as a single String.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpGZippedFile(string filename)
		{
			StreamReader r = EncodedInputStreamReader(new GZipInputStream(File.OpenRead(filename)), null);
			return IOUtils.SlurpReader(r);
		}

		/// <summary>Returns all the text in the given FileInfo as a single String.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpGZippedFile(FileInfo file)
		{
			StreamReader r = EncodedInputStreamReader(new GZipInputStream(file.OpenRead()), null);
			return IOUtils.SlurpReader(r);
		}

		/// <summary>Returns all the text in the given file with the given encoding.</summary>
		/// <remarks>
		/// Returns all the text in the given file with the given encoding.
		/// The string may be empty, if the file is empty.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpFile(string filename, string encoding)
		{
			StreamReader r = ReaderFromString(filename, encoding);
			return IOUtils.SlurpReader(r);
		}

		/// <summary>
		/// Returns all the text in the given file with the given
		/// encoding.
		/// </summary>
		/// <remarks>
		/// Returns all the text in the given file with the given
		/// encoding. If the file cannot be read (non-existent, etc.), then
		/// the method throws an unchecked RuntimeIOException.  If the caller
		/// is willing to tolerate missing files, they should catch that
		/// exception.
		/// </remarks>
		public static string SlurpFileNoExceptions(string filename, string encoding)
		{
			try
			{
				return SlurpFile(filename, encoding);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("slurpFile IO problem", e);
			}
		}

		/// <summary>Returns all the text in the given file</summary>
		/// <returns>The text in the file.</returns>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpFile(string filename)
		{
			return SlurpFile(filename, defaultEncoding);
		}

		/// <summary>Returns all the text at the given URL.</summary>
		public static string SlurpURLNoExceptions(Uri u, string encoding)
		{
			try
			{
				return IOUtils.SlurpURL(u, encoding);
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
				return null;
			}
		}

		/// <summary>Returns all the text at the given URL.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpURL(Uri u, string encoding)
		{
			using(var client = new WebClient())
			{
				client.Encoding = Encoding.GetEncoding(encoding);
				return client.DownloadString(u);
			}
			// string lineSeparator = Environment.NewLine;
			// URLConnection uc = u.OpenConnection();
			// uc.SetReadTimeout(30000);
			// InputStream @is;
			// try
			// {
			// 	@is = uc.GetInputStream();
			// }
			// catch (SocketTimeoutException e)
			// {
			// 	logger.Error("Socket time out; returning empty string.");
			// 	logger.Err(ThrowableToStackTrace(e));
			// 	return string.Empty;
			// }
			// using (BufferedReader br = new BufferedReader(new InputStreamReader(@is, encoding)))
			// {
			// 	StringBuilder buff = new StringBuilder(SlurpBufferSize);
			// 	// make biggish
			// 	for (string temp; (temp = br.ReadLine()) != null; )
			// 	{
			// 		buff.Append(temp);
			// 		buff.Append(lineSeparator);
			// 	}
			// 	return buff.ToString();
			// }
		}

		// public static string GetUrlEncoding(URLConnection connection)
		// {
		// 	string contentType = connection.GetContentType();
		// 	string[] values = contentType.Split(";");
		// 	string charset = defaultEncoding;
		// 	// might or might not be right....
		// 	foreach (string value in values)
		// 	{
		// 		value = value.Trim();
		// 		if (value.ToLower(Locale.English).StartsWith("charset="))
		// 		{
		// 			charset = Sharpen.Runtime.Substring(value, "charset=".Length);
		// 		}
		// 	}
		// 	return charset;
		// }

		/// <summary>Returns all the text at the given URL.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpURL(Uri u)
		{
			using(var client = new WebClient())
			{
				return client.DownloadString(u);
			}

			// URLConnection uc = u.OpenConnection();
			// string encoding = GetUrlEncoding(uc);
			// InputStream @is = uc.GetInputStream();
			// using (BufferedReader br = new BufferedReader(new InputStreamReader(@is, encoding)))
			// {
			// 	StringBuilder buff = new StringBuilder(SlurpBufferSize);
			// 	// make biggish
			// 	string lineSeparator = Runtime.LineSeparator();
			// 	for (string temp; (temp = br.ReadLine()) != null; )
			// 	{
			// 		buff.Append(temp);
			// 		buff.Append(lineSeparator);
			// 	}
			// 	return buff.ToString();
			// }
		}

		/// <summary>Returns all the text at the given URL.</summary>
		public static string SlurpURLNoExceptions(Uri u)
		{
			try
			{
				return SlurpURL(u);
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
				return null;
			}
		}

		/// <summary>Returns all the text at the given URL.</summary>
		/// <exception cref="System.Exception"/>
		public static string SlurpURL(string path)
		{
			return SlurpURL(new Uri(path));
		}

		/// <summary>Returns all the text at the given URL.</summary>
		/// <remarks>
		/// Returns all the text at the given URL. If the file cannot be read
		/// (non-existent, etc.), then and only then the method returns
		/// <see langword="null"/>
		/// .
		/// </remarks>
		public static string SlurpURLNoExceptions(string path)
		{
			try
			{
				return SlurpURL(path);
			}
			catch (Exception e)
			{
				logger.Err(ThrowableToStackTrace(e));
				return null;
			}
		}

		/// <summary>
		/// Returns all the text in the given file with the given
		/// encoding.
		/// </summary>
		/// <remarks>
		/// Returns all the text in the given file with the given
		/// encoding. If the file cannot be read (non-existent, etc.), then
		/// the method throws an unchecked RuntimeIOException.  If the caller
		/// is willing to tolerate missing files, they should catch that
		/// exception.
		/// </remarks>
		public static string SlurpFileNoExceptions(FileInfo file)
		{
			try
			{
				return IOUtils.SlurpReader(EncodedInputStreamReader(file.OpenRead(), null));
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>
		/// Returns all the text in the given file with the given
		/// encoding.
		/// </summary>
		/// <remarks>
		/// Returns all the text in the given file with the given
		/// encoding. If the file cannot be read (non-existent, etc.), then
		/// the method throws an unchecked RuntimeIOException.  If the caller
		/// is willing to tolerate missing files, they should catch that
		/// exception.
		/// </remarks>
		public static string SlurpFileNoExceptions(string filename)
		{
			try
			{
				return SlurpFile(filename);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		/// <summary>Returns all the text from the given Reader.</summary>
		/// <remarks>
		/// Returns all the text from the given Reader.
		/// Closes the Reader when done.
		/// </remarks>
		/// <returns>The text in the file.</returns>
		public static string SlurpReader(StreamReader reader)
		{
			using(reader) 
			{
				return reader.ReadToEnd();
			}
			// StringBuilder buff = new StringBuilder();
			// try
			// {
			// 	using (BufferedReader r = new BufferedReader(reader))
			// 	{
			// 		char[] chars = new char[SlurpBufferSize];
			// 		while (true)
			// 		{
			// 			int amountRead = r.Read(chars, 0, SlurpBufferSize);
			// 			if (amountRead < 0)
			// 			{
			// 				break;
			// 			}
			// 			buff.Append(chars, 0, amountRead);
			// 		}
			// 	}
			// }
			// catch (Exception e)
			// {
			// 	throw new RuntimeIOException("slurpReader IO problem", e);
			// }
			// return buff.ToString();
		}

		/// <summary>Read the contents of an input stream, decoding it according to the given character encoding.</summary>
		/// <param name="input">The input stream to read from</param>
		/// <returns>The String representation of that input stream</returns>
		/// <exception cref="System.IO.IOException"/>
		public static string SlurpInputStream(Stream input, string encoding)
		{
			return SlurpReader(EncodedInputStreamReader(input, encoding));
		}

		/// <summary>Send all bytes from the input stream to the output stream.</summary>
		/// <param name="input">The input bytes.</param>
		/// <param name="output">Where the bytes should be written.</param>
		/// <exception cref="System.IO.IOException"/>
		public static void WriteStreamToStream(Stream input, Stream output)
		{
			input.CopyTo(output);
			// byte[] buffer = new byte[4096];
			// while (true)
			// {
			// 	int len = input.Read(buffer);
			// 	if (len == -1)
			// 	{
			// 		break;
			// 	}
			// 	output.Write(buffer, 0, len);
			// }
		}

		/// <summary>Read in a CSV formatted file with a header row.</summary>
		/// <param name="path">- path to CSV file</param>
		/// <param name="quoteChar">- character for enclosing strings, defaults to "</param>
		/// <param name="escapeChar">- character for escaping quotes appearing in quoted strings; defaults to " (i.e. "" is used for " inside quotes, consistent with Excel)</param>
		/// <returns>a list of maps representing the rows of the csv. The maps' keys are the header strings and their values are the row contents</returns>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public static IList<IDictionary<string, string>> ReadCSVWithHeader(string path, char quoteChar, char escapeChar)
		{
			string[] labels = null;
			IList<IDictionary<string, string>> rows = new List<IDictionary<string, string>>();
			foreach (string line in IOUtils.ReadLines(path))
			{
				// logger.info("Splitting "+line);
				if (labels == null)
				{
					labels = StringUtils.SplitOnCharWithQuoting(line, ',', '"', escapeChar);
				}
				else
				{
					string[] cells = StringUtils.SplitOnCharWithQuoting(line, ',', quoteChar, escapeChar);
					System.Diagnostics.Debug.Assert((cells.Length == labels.Length));
					IDictionary<string, string> cellMap = new Dictionary<string,string>();
					for (int i = 0; i < labels.Length; i++)
					{
						cellMap.Add(labels[i], cells[i]);
					}
					rows.Add(cellMap);
				}
			}
			return rows;
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<IDictionary<string, string>> ReadCSVWithHeader(string path)
		{
			return ReadCSVWithHeader(path, '"', '"');
		}

		/// <summary>Read a CSV file character by character.</summary>
		/// <remarks>
		/// Read a CSV file character by character. Allows for multi-line CSV files (in quotes), but
		/// is less flexible and likely slower than readCSVWithHeader()
		/// </remarks>
		/// <param name="csvContents">The char[] array corresponding to the contents of the file</param>
		/// <param name="numColumns">The number of columns in the file (for verification, primarily)</param>
		/// <returns>A list of lines in the file</returns>
		public static LinkedList<string[]> ReadCSVStrictly(char[] csvContents, int numColumns)
		{
			//--Variables
			StringBuilder[] buffer = new StringBuilder[numColumns];
			buffer[0] = new StringBuilder();
			LinkedList<string[]> lines = new LinkedList<string[]>();
			//--State
			bool inQuotes = false;
			bool nextIsEscaped = false;
			int columnI = 0;
			//--Read
			for (int offset = 0; offset < csvContents.Length; offset++)
			{
				if (nextIsEscaped)
				{
					buffer[columnI].Append(csvContents[offset]);
					nextIsEscaped = false;
				}
				else
				{
					switch (csvContents[offset])
					{
						case '"':
						{
							//(case: quotes)
							inQuotes = !inQuotes;
							break;
						}

						case ',':
						{
							//(case: field separator)
							if (inQuotes)
							{
								buffer[columnI].Append(',');
							}
							else
							{
								columnI += 1;
								if (columnI >= numColumns)
								{
									throw new ArgumentException("Too many columns: " + columnI + "/" + numColumns + " (offset: " + offset + ")");
								}
								buffer[columnI] = new StringBuilder();
							}
							break;
						}

						case '\n':
						{
							//(case: newline)
							if (inQuotes)
							{
								buffer[columnI].Append('\n');
							}
							else
							{
								//((error checks))
								if (columnI != numColumns - 1)
								{
									throw new ArgumentException("Too few columns: " + columnI + "/" + numColumns + " (offset: " + offset + ")");
								}
								//((create line))
								string[] rtn = new string[buffer.Length];
								for (int i = 0; i < buffer.Length; i++)
								{
									rtn[i] = buffer[i].ToString();
								}
								lines.AddLast(rtn);
								//((update state))
								columnI = 0;
								buffer[columnI] = new StringBuilder();
							}
							break;
						}

						case '\\':
						{
							nextIsEscaped = true;
							break;
						}

						default:
						{
							buffer[columnI].Append(csvContents[offset]);
							break;
						}
					}
				}
			}
			//--Return
			return lines;
		}

		/// <exception cref="System.IO.IOException"/>
		public static LinkedList<string[]> ReadCSVStrictly(string filename, int numColumns)
		{
			return ReadCSVStrictly(SlurpFile(filename).ToCharArray(), numColumns);
		}

		/// <summary>Get a input file stream (automatically gunzip/bunzip2 depending on file extension)</summary>
		/// <param name="filename">Name of file to open</param>
		/// <returns>Input stream that can be used to read from the file</returns>
		/// <exception cref="System.IO.IOException">if there are exceptions opening the file</exception>
		public static Stream GetFileInputStream(string filename)
		{
			Stream @in = File.OpenRead(filename);
			if (filename.EndsWith(".gz"))
			{
				@in = new GZipInputStream(@in);
			}
			else
			{
				if (filename.EndsWith(".bz2"))
				{
					//in = new CBZip2InputStream(in);
					@in = GetBZip2PipedInputStream(filename);
				}
			}
			return @in;
		}

		/// <summary>Get a output file stream (automatically gzip/bzip2 depending on file extension)</summary>
		/// <param name="filename">Name of file to open</param>
		/// <returns>Output stream that can be used to write to the file</returns>
		/// <exception cref="System.IO.IOException">if there are exceptions opening the file</exception>
		public static Stream GetFileOutputStream(string filename)
		{
			Stream @out = File.OpenWrite(filename);
			if (filename.EndsWith(".gz"))
			{
				@out = new GZipOutputStream(@out);
			}
			else
			{
				if (filename.EndsWith(".bz2"))
				{
					//out = new CBZip2OutputStream(out);
					@out = GetBZip2PipedOutputStream(filename);
				}
			}
			return @out;
		}

		/// <exception cref="System.IO.IOException"/>
		public static Stream GetFileOutputStream(string filename, bool append)
		{
			Stream @out = File.Open(filename, append ? FileMode.Append : FileMode.Open, FileAccess.Write);
			if (filename.EndsWith(".gz"))
			{
				@out = new GZipOutputStream(@out);
			}
			else
			{
				if (filename.EndsWith(".bz2"))
				{
					//out = new CBZip2OutputStream(out);
					@out = GetBZip2PipedOutputStream(filename);
				}
			}
			return @out;
		}

		/// <exception cref="System.IO.IOException"/>
		[System.ObsoleteAttribute(@"Just call readerFromString(filename)")]
		public static StreamReader GetBufferedFileReader(string filename)
		{
			return ReaderFromString(filename, defaultEncoding);
		}

		/// <exception cref="System.IO.IOException"/>
		[System.ObsoleteAttribute(@"Just call readerFromString(filename)")]
		public static StreamReader GetBufferedReaderFromClasspathOrFileSystem(string filename)
		{
			return ReaderFromString(filename, defaultEncoding);
		}

		/// <exception cref="System.IO.IOException"/>
		public static StreamWriter GetPrintWriter(FileInfo textFile)
		{
			return GetPrintWriter(textFile, null);
		}

		/// <exception cref="System.IO.IOException"/>
		public static StreamWriter GetPrintWriter(FileInfo textFile, string encoding)
		{
			if (encoding == null)
			{
				encoding = defaultEncoding;
			}
			return new StreamWriter(textFile.OpenWrite(), Encoding.GetEncoding(encoding)) { AutoFlush = true };
		}

		/// <exception cref="System.IO.IOException"/>
		public static StreamWriter GetPrintWriter(string filename)
		{
			return GetPrintWriter(filename, defaultEncoding);
		}

		public static StreamWriter GetPrintWriterIgnoringExceptions(string filename)
		{
			try
			{
				return GetPrintWriter(filename, defaultEncoding);
			}
			catch (IOException)
			{
				return null;
			}
		}

		public static StreamWriter GetPrintWriterOrDie(string filename)
		{
			try
			{
				return GetPrintWriter(filename, defaultEncoding);
			}
			catch (IOException ioe)
			{
				throw new RuntimeIOException(ioe);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static StreamWriter GetPrintWriter(string filename, string encoding)
		{
			Stream @out = GetFileOutputStream(filename);
			if (encoding == null)
			{
				encoding = defaultEncoding;
			}
			return new StreamWriter(@out, Encoding.GetEncoding(encoding), 8192) { AutoFlush = true };
		}

		/// <exception cref="System.IO.IOException"/>
		public static Stream GetBZip2PipedInputStream(string filename)
		{

			return new BZip2InputStream(File.OpenRead(filename));
		}

		/// <exception cref="System.IO.IOException"/>
		public static Stream GetBZip2PipedOutputStream(string filename)
		{
			return new BZip2OutputStream(File.OpenWrite(filename));
		}

		private static readonly Regex tab = new Regex("\t", RegexOptions.Compiled);

		/// <summary>Read column as set</summary>
		/// <param name="infile">- filename</param>
		/// <param name="field">index of field to read</param>
		/// <returns>a set of the entries in column field</returns>
		/// <exception cref="System.IO.IOException"/>
		public static ICollection<string> ReadColumnSet(string infile, int field)
		{
			StreamReader br = IOUtils.GetBufferedFileReader(infile);
			ICollection<string> set = new HashSet<string>();
			for (string line; (line = br.ReadLine()) != null; )
			{
				line = line.Trim();
				if (line.Length > 0)
				{
					if (field < 0)
					{
						set.Add(line);
					}
					else
					{
						string[] fields = tab.Split(line);
						if (field < fields.Length)
						{
							set.Add(fields[field]);
						}
					}
				}
			}
			br.Close();
			return set;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Lang.InstantiationException"/>
		/// <exception cref="System.MemberAccessException"/>
		/// <exception cref="Java.Lang.NoSuchFieldException"/>
		/// <exception cref="System.MissingMethodException"/>
		/// <exception cref="System.Reflection.TargetInvocationException"/>
		public static IList<C> ReadObjectFromColumns<C>(Type objClass, string filename, string[] fieldNames, string delimiter)
		{
			Regex delimiterPattern = new Regex(delimiter, RegexOptions.Compiled);
			IList<C> list = new List<C>();
			StreamReader br = IOUtils.GetBufferedFileReader(filename);
			for (string line; (line = br.ReadLine()) != null; )
			{
				line = line.Trim();
				if (line.Length > 0)
				{
					C item = StringUtils.ColumnStringToObject<C>(objClass, line, delimiterPattern, fieldNames);
					list.Add(item);
				}
			}
			br.Close();
			return list;
		}

		/// <exception cref="System.IO.IOException"/>
		public static IDictionary<string, string> ReadMap(string filename)
		{
			IDictionary<string, string> map = new Dictionary<string, string>();
			StreamReader br = IOUtils.GetBufferedFileReader(filename);
			for (string line; (line = br.ReadLine()) != null; )
			{
				string[] fields = tab.Split(line, 2);
				map[fields[0]] = fields[1];
			}
			br.Close();
			return map;
		}

		/// <summary>Returns the contents of a file as a single string.</summary>
		/// <remarks>
		/// Returns the contents of a file as a single string.  The string may be
		/// empty, if the file is empty.  If there is an IOException, it is caught
		/// and null is returned.
		/// </remarks>
		public static string StringFromFile(string filename)
		{
			return StringFromFile(filename, defaultEncoding);
		}

		/// <summary>Returns the contents of a file as a single string.</summary>
		/// <remarks>
		/// Returns the contents of a file as a single string.  The string may be
		/// empty, if the file is empty.  If there is an IOException, it is caught
		/// and null is returned.  Encoding can also be specified.
		/// </remarks>
		public static string StringFromFile(string filename, string encoding)
		{
			// todo: This is same as slurpFile (!)
			try
			{
				return File.ReadAllText(filename, Encoding.GetEncoding(encoding));
				// StringBuilder sb = new StringBuilder();
				// BufferedReader @in = new BufferedReader(new EncodingFileReader(filename, encoding));
				// string line;
				// while ((line = @in.ReadLine()) != null)
				// {
				// 	sb.Append(line);
				// 	sb.Append(eolChar);
				// }
				// @in.Close();
				// return sb.ToString();
			}
			catch (IOException e)
			{
				logger.Err(ThrowableToStackTrace(e));
				return null;
			}
		}

		/// <summary>Returns the contents of a file as a list of strings.</summary>
		/// <remarks>
		/// Returns the contents of a file as a list of strings.  The list may be
		/// empty, if the file is empty.  If there is an IOException, it is caught
		/// and null is returned.
		/// </remarks>
		public static IList<string> LinesFromFile(string filename)
		{
			return LinesFromFile(filename, defaultEncoding);
		}

		/// <summary>Returns the contents of a file as a list of strings.</summary>
		/// <remarks>
		/// Returns the contents of a file as a list of strings.  The list may be
		/// empty, if the file is empty.  If there is an IOException, it is caught
		/// and null is returned. Encoding can also be specified
		/// </remarks>
		public static IList<string> LinesFromFile(string filename, string encoding)
		{
			return LinesFromFile(filename, encoding, false);
		}

		public static IList<string> LinesFromFile(string filename, string encoding, bool ignoreHeader)
		{
			try
			{
				IList<string> lines = new List<string>();
				StreamReader @in = ReaderFromString(filename, encoding);
				string line;
				int i = 0;
				while ((line = @in.ReadLine()) != null)
				{
					i++;
					if (ignoreHeader && i == 1)
					{
						continue;
					}
					lines.Add(line);
				}
				@in.Close();
				return lines;
			}
			catch (IOException e)
			{
				logger.Err(ThrowableToStackTrace(e));
				return null;
			}
		}

		/// <summary>
		/// A JavaNLP specific convenience routine for obtaining the current
		/// scratch directory for the machine you're currently running on.
		/// </summary>
		public static FileInfo GetJNLPLocalScratch(IConfiguration properties)
		{
			try
			{
				string machineName = Dns.GetHostName();
				string username = properties["user.name"];
				return new FileInfo("/" + machineName + "/scr1/" + username);
			}
			catch (Exception)
			{
				return new FileInfo("./scr/");
			}
		}

		// default scratch
		/// <summary>Given a filepath, makes sure a directory exists there.</summary>
		/// <remarks>
		/// Given a filepath, makes sure a directory exists there.  If not, creates and returns it.
		/// Same as ENSURE-DIRECTORY in CL.
		/// </remarks>
		/// <param name="tgtDir">The directory that you wish to ensure exists</param>
		/// <exception cref="System.IO.IOException">If directory can't be created, is an existing file, or for other reasons</exception>
		public static FileSystemInfo EnsureDir(FileSystemInfo tgtDir)
		{
			if (tgtDir.Exists)
			{
				if (tgtDir.Attributes == FileAttributes.Directory)
				{
					return tgtDir;
				}
				else
				{
					throw new IOException("Could not create directory " + tgtDir.FullName + ", as a file already exists at that path.");
				}
			}
			else
			{
				// if (!tgtDir.Create())
				// {
				// 	throw new IOException("Could not create directory " + tgtDir.FullName);
				// }
				return Directory.CreateDirectory(tgtDir.FullName);
			}
		}

		/// <summary>Given a filepath, delete all files in the directory recursively</summary>
		/// <param name="dir">Directory from which to delete files</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if the deletion is successful,
		/// <see langword="false"/>
		/// otherwise
		/// </returns>
		public static bool DeleteDirRecursively(FileSystemInfo dir)
		{
			try
			{
				Directory.Delete(dir.FullName, true);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static string GetExtension(string fileName)
		{
			if (!fileName.Contains("."))
			{
				return null;
			}
			int idx = fileName.LastIndexOf('.');
			return fileName.Substring(idx + 1);
		}

		/// <summary>Create a Reader with an explicit encoding around an InputStream.</summary>
		/// <remarks>
		/// Create a Reader with an explicit encoding around an InputStream.
		/// This static method will treat null as meaning to use the platform default,
		/// unlike the Java library methods that disallow a null encoding.
		/// </remarks>
		/// <param name="stream">An InputStream</param>
		/// <param name="encoding">A charset encoding</param>
		/// <returns>A Reader</returns>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public static StreamReader EncodedInputStreamReader(Stream stream, string encoding)
		{
			// InputStreamReader doesn't allow encoding to be null;
			if (encoding == null)
			{
				return new StreamReader(stream);
			}
			else
			{
				return new StreamReader(stream, Encoding.GetEncoding(encoding));
			}
		}

		/// <summary>Create a Reader with an explicit encoding around an InputStream.</summary>
		/// <remarks>
		/// Create a Reader with an explicit encoding around an InputStream.
		/// This static method will treat null as meaning to use the platform default,
		/// unlike the Java library methods that disallow a null encoding.
		/// </remarks>
		/// <param name="stream">An InputStream</param>
		/// <param name="encoding">A charset encoding</param>
		/// <returns>A Reader</returns>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public static TextWriter EncodedOutputStreamWriter(Stream stream, string encoding)
		{
			// OutputStreamWriter doesn't allow encoding to be null;
			if (encoding == null)
			{
				return new StreamWriter(stream);
			}
			else
			{
				return new StreamWriter(stream, Encoding.GetEncoding(encoding));
			}
		}

		/// <summary>Create a Reader with an explicit encoding around an InputStream.</summary>
		/// <remarks>
		/// Create a Reader with an explicit encoding around an InputStream.
		/// This static method will treat null as meaning to use the platform default,
		/// unlike the Java library methods that disallow a null encoding.
		/// </remarks>
		/// <param name="stream">An InputStream</param>
		/// <param name="encoding">A charset encoding</param>
		/// <param name="autoFlush">Whether to make an autoflushing Writer</param>
		/// <returns>A Reader</returns>
		/// <exception cref="System.IO.IOException">If any IO problem</exception>
		public static StreamWriter EncodedOutputStreamPrintWriter(Stream stream, string encoding, bool autoFlush)
		{
			// PrintWriter doesn't allow encoding to be null; or to have charset and flush
			if (encoding == null)
			{
				return new StreamWriter(stream) { AutoFlush = true };
			}
			else
			{
				return new StreamWriter(stream, Encoding.GetEncoding(encoding)) { AutoFlush = true };
			}
		}

		/// <summary>
		/// A raw file copy function -- this is not public since no error checks are made as to the
		/// consistency of the file being copied.
		/// </summary>
		/// <remarks>
		/// A raw file copy function -- this is not public since no error checks are made as to the
		/// consistency of the file being copied. Use instead:
		/// </remarks>
		/// <seealso cref="Cp(Java.IO.FileInfo, Java.IO.FileInfo, bool)"/>
		/// <param name="source">The source file. This is guaranteed to exist, and is guaranteed to be a file.</param>
		/// <param name="target">The target file.</param>
		/// <exception cref="System.IO.IOException">Throws an exception if the copy fails.</exception>
		private static void CopyFile(FileInfo source, FileInfo target)
		{
			using(var src = source.OpenRead())
			using(var dest = target.OpenWrite())
			{
				src.CopyTo(dest);
			}
			// FileChannel sourceChannel = new FileInputStream(source).GetChannel();
			// FileChannel targetChannel = new FileOutputStream(target).GetChannel();
			// // allow for the case that it doesn't all transfer in one go (though it probably does for a file cp)
			// long pos = 0;
			// long toCopy = sourceChannel.Size();
			// while (toCopy > 0)
			// {
			// 	long bytes = sourceChannel.TransferTo(pos, toCopy, targetChannel);
			// 	pos += bytes;
			// 	toCopy -= bytes;
			// }
			// sourceChannel.Close();
			// targetChannel.Close();
		}

		/// <summary><p>An implementation of cp, as close to the Unix command as possible.</summary>
		/// <remarks>
		/// <p>An implementation of cp, as close to the Unix command as possible.
		/// Both directories and files are valid for either the source or the target;
		/// if the target exists, the semantics of Unix cp are [intended to be] obeyed.</p>
		/// </remarks>
		/// <param name="source">The source file or directory.</param>
		/// <param name="target">The target to write this file or directory to.</param>
		/// <param name="recursive">If true, recursively copy directory contents</param>
		/// <exception cref="System.IO.IOException">
		/// If either the copy fails (standard IO Exception), or the command is invalid
		/// (e.g., copying a directory without the recursive flag)
		/// </exception>
		public static void Cp(FileInfo source, FileInfo target, bool recursive)
		{
			// Error checks
			if (source.Attributes == FileAttributes.Directory && !recursive)
			{
				// cp a b -- a is a directory
				throw new IOException("cp: omitting directory: " + source);
			}
			if (!target.Directory.Exists)
			{
				// cp a b/c/d/e -- b/c/d doesn't exist
				throw new IOException("cp: cannot copy to directory: " + recursive + " (parent doesn't exist)");
			}
			if (target.Directory.Attributes != FileAttributes.Directory)
			{
				// cp a b/c/d/e -- b/c/d is a regular file
				throw new IOException("cp: cannot copy to directory: " + recursive + " (parent isn't a directory)");
			}
			// Get true target
			FileInfo trueTarget;
			if (target.Exists && target.Attributes == FileAttributes.Directory)
			{
				trueTarget = new FileInfo(Path.Combine(target.FullName, Path.GetFileName(source.FullName)));
			}
			else
			{
				trueTarget = target;
			}
			// Copy
			if (source.Attributes != FileAttributes.Directory)
			{
				// Case: copying a file
				CopyFile(source, trueTarget);
			}
			else
			{
				if (source.Attributes == FileAttributes.Directory)
				{
					// Case: copying a directory
					FileInfo[] children = Directory.GetFiles(source.FullName).Select(_=> new FileInfo(_)).ToArray();
					if (children == null)
					{
						throw new IOException("cp: could not list files in source: " + source);
					}
					if (target.Exists)
					{
						// Case: cp -r a b -- b exists
						if (target.Attributes != FileAttributes.Directory)
						{
							// cp -r a b -- b is a regular file
							throw new IOException("cp: cannot copy directory into regular file: " + target);
						}
						if (trueTarget.Exists && trueTarget.Attributes != FileAttributes.Directory)
						{
							// cp -r a b -- b/a is not a directory
							throw new IOException("cp: overwriting a file with a directory: " + trueTarget);
						}
						if (!trueTarget.Exists)
						{
							try 
							{ 
								Directory.CreateDirectory(trueTarget.FullName);
							}
							catch
							{
								// cp -r a b -- b/a cannot be created
								throw new IOException("cp: could not create directory: " + trueTarget);
							}
						}
					}
					else
					{
						// Case: cp -r a b -- b does not exist
						System.Diagnostics.Debug.Assert(trueTarget == target);
						try 
						{ 
							Directory.CreateDirectory(trueTarget.FullName);
						}
						catch
						{
							// cp -r a b -- cannot create b as a directory
							throw new IOException("cp: could not create target directory: " + trueTarget);
						}
					}
					// Actually do the copy
					foreach (FileInfo child in children)
					{
						FileInfo childTarget = new FileInfo(Path.Combine(trueTarget.FullName, Path.GetFileName(child.FullName)));
						Cp(child, childTarget, recursive);
					}
				}
				else
				{
					throw new IOException("cp: unknown file type: " + source);
				}
			}
		}

		/// <seealso cref="Cp(Java.IO.FileInfo, Java.IO.FileInfo, bool)"/>
		/// <exception cref="System.IO.IOException"/>
		public static void Cp(FileInfo source, FileInfo target)
		{
			Cp(source, target, false);
		}

		/// <summary>A Java implementation of the Unix tail functionality.</summary>
		/// <remarks>
		/// A Java implementation of the Unix tail functionality.
		/// That is, read the last n lines of the input file f.
		/// </remarks>
		/// <param name="f">The file to read the last n lines from</param>
		/// <param name="n">The number of lines to read from the end of the file.</param>
		/// <param name="encoding">The encoding to read the file in.</param>
		/// <returns>The read lines, one String per line.</returns>
		/// <exception cref="System.IO.IOException">if the file could not be read.</exception>
		public static string[] Tail(FileInfo f, int n, string encoding)
		{
			if (n == 0)
			{
				return new string[0];
			}
			// Variables
			Stream raf = f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			int linesRead = 0;
			IList<byte> bytes = new List<byte>();
			IList<string> linesReversed = new List<string>();
			// Seek to end of file
			// Read backwards
			for (long seek = 0; seek <= raf.Length; seek++)
			{
				// Seek back
				raf.Seek(-seek, SeekOrigin.End);
				// Read the next character
				byte c = (byte)raf.ReadByte();
				if (c == '\n')
				{
					// If it's a newline, handle adding the line
					byte[] str = new byte[bytes.Count];
					for (int i = 0; i < str.Length; ++i)
					{
						str[i] = bytes[str.Length - i - 1];
					}
					linesReversed.Add(Encoding.GetEncoding(encoding).GetString(str));
					bytes = new List<byte>();
					linesRead += 1;
					if (linesRead == n)
					{
						break;
					}
				}
				else
				{
					// Else, register the character for later
					bytes.Add(c);
				}
			}
			// Add any remaining lines
			if (linesRead < n && bytes.Count > 0)
			{
				byte[] str = new byte[bytes.Count];
				for (int i = 0; i < str.Length; ++i)
				{
					str[i] = bytes[str.Length - i - 1];
				}
				linesReversed.Add(Encoding.GetEncoding(encoding).GetString(str));
			}
			// Create output
			string[] rtn = new string[linesReversed.Count];
			for (int i_1 = 0; i_1 < rtn.Length; ++i_1)
			{
				rtn[i_1] = linesReversed[rtn.Length - i_1 - 1];
			}
			raf.Close();
			return rtn;
		}

		/// <seealso cref="Tail(Java.IO.FileInfo, int, string)"></seealso>
		/// <exception cref="System.IO.IOException"/>
		public static string[] Tail(FileInfo f, int n)
		{
			return Tail(f, n, "utf-8");
		}

		private sealed class _HashSet_2010 : HashSet<string>
		{
			public _HashSet_2010()
			{
				{
					this.Add("/");
					this.Add("/u");
					this.Add("/u/");
					this.Add("/u/nlp");
					this.Add("/u/nlp/");
					this.Add("/u/nlp/data");
					this.Add("/u/nlp/data/");
					this.Add("/scr");
					this.Add("/scr/");
					this.Add("/u/scr/nlp/data");
					this.Add("/u/scr/nlp/data/");
				}
			}
		}

		/// <summary>Bare minimum sanity checks</summary>
		private static ICollection<string> blacklistedPathsToRemove = new _HashSet_2010();

		/// <summary>Delete this file; or, if it is a directory, delete this directory and all its contents.</summary>
		/// <remarks>
		/// Delete this file; or, if it is a directory, delete this directory and all its contents.
		/// This is a somewhat dangerous function to call from code, and so a few safety features have been
		/// implemented (though you should not rely on these!):
		/// <ul>
		/// <li>Certain directories are prohibited from being removed.</li>
		/// <li>More than 100 files cannot be removed with this function.</li>
		/// <li>More than 10GB cannot be removed with this function.</li>
		/// </ul>
		/// </remarks>
		/// <param name="file">The file or directory to delete.</param>
		public static void DeleteRecursively(FileInfo file)
		{
			// Sanity checks
			if (blacklistedPathsToRemove.Contains(file.FullName))
			{
				throw new ArgumentException("You're trying to delete " + file + "! I _really_ don't think you want to do that...");
			}
			int count = 0;
			long size = 0;
			foreach (FileInfo f in IterFilesRecursive(file))
			{
				count += 1;
				size += f.Length;
			}
			if (count > 100)
			{
				throw new ArgumentException("Deleting more than 100 files; you should do this manually");
			}
			if (size > 10000000000L)
			{
				// 10 GB
				throw new ArgumentException("Deleting more than 10GB; you should do this manually");
			}
			// Do delete
			if (file.Attributes == FileAttributes.Directory)
			{
				FileInfo[] children = Directory.GetFiles(file.FullName).Select(_ => new FileInfo(_)).ToArray();
				if (children != null)
				{
					foreach (FileInfo child in children)
					{
						DeleteRecursively(child);
					}
				}
			}
			//noinspection ResultOfMethodCallIgnored
			file.Delete();
		}

		/// <summary>Start a simple console.</summary>
		/// <remarks>
		/// Start a simple console. Read lines from stdin, and pass each line to the callback.
		/// Returns on typing "exit" or "quit".
		/// </remarks>
		/// <param name="callback">The function to run for every line of input.</param>
		/// <exception cref="System.IO.IOException">Thrown from the underlying input stream.</exception>
		public static void Console(string prompt, Action<string> callback)
		{
			StreamReader reader = new StreamReader(System.Console.OpenStandardInput());
			string line;
			System.Console.Out.Write(prompt);
			while ((line = reader.ReadLine()) != null)
			{
				switch (line.ToLower())
				{
					case "":
					{
						break;
					}

					case "exit":
					case "quit":
					case "q":
					{
						return;
					}

					default:
					{
						callback(line);
						break;
					}
				}
				System.Console.Out.Write(prompt);
			}
		}

		/// <summary>Create a prompt, and read a single line of response.</summary>
		/// <param name="prompt">An optional prompt to show the user.</param>
		/// <exception cref="System.IO.IOException">Throw from the underlying reader.</exception>
		public static string PromptUserInput(string prompt)
		{
			StreamReader reader = new StreamReader(System.Console.OpenStandardInput());
			System.Console.Out.Write(prompt ?? "> ");
			return reader.ReadLine();
		}

		/// <seealso cref="Console(string, Java.Util.Function.IConsumer{T})"></seealso>
		/// <exception cref="System.IO.IOException"/>
		public static void Console(Action<string> callback)
		{
			Console("> ", callback);
		}

		public static string ThrowableToStackTrace(Exception t)
		{
			return t.ToString();
			// StringBuilder sb = new StringBuilder();
			// sb.Append(t).Append(eolChar);
			// foreach (StackTraceElement e in t.GetStackTrace())
			// {
			// 	sb.Append("\t at ").Append(e).Append(eolChar);
			// }
			// return sb.ToString();
		}
	}
}

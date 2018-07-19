using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// A convenience IO class with print and println statements to
	/// standard output and standard error allowing encoding in an
	/// arbitrary character set.
	/// </summary>
	/// <remarks>
	/// A convenience IO class with print and println statements to
	/// standard output and standard error allowing encoding in an
	/// arbitrary character set.  It also provides methods which use UTF-8
	/// always, overriding the system default charset.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	public class EncodingPrintWriter
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IO.EncodingPrintWriter));

		private const string DefaultEncoding = "UTF-8";

		private static PrintWriter cachedErrWriter;

		private static string cachedErrEncoding = string.Empty;

		private static PrintWriter cachedOutWriter;

		private static string cachedOutEncoding = string.Empty;

		private EncodingPrintWriter()
		{
		}

		/// <summary>Print methods wrapped around System.err</summary>
		public class Err
		{
			private Err()
			{
			}

			// uninstantiable
			// uninstantiable
			private static void SetupErrWriter(string encoding)
			{
				if (encoding == null)
				{
					encoding = DefaultEncoding;
				}
				if (cachedErrWriter == null || !cachedErrEncoding.Equals(encoding))
				{
					try
					{
						cachedErrWriter = new PrintWriter(new OutputStreamWriter(System.Console.Error, encoding), true);
						cachedErrEncoding = encoding;
					}
					catch (UnsupportedEncodingException e)
					{
						log.Info("Error " + e + "Printing as default encoding.");
						cachedErrWriter = new PrintWriter(new OutputStreamWriter(System.Console.Error), true);
						cachedErrEncoding = string.Empty;
					}
				}
			}

			public static void Println(string o, string encoding)
			{
				SetupErrWriter(encoding);
				cachedErrWriter.Println(o);
			}

			public static void Print(string o, string encoding)
			{
				SetupErrWriter(encoding);
				cachedErrWriter.Print(o);
				cachedErrWriter.Flush();
			}

			public static void Println(string o)
			{
				Println(o, null);
			}

			public static void Print(string o)
			{
				Print(o, null);
			}
		}

		/// <summary>Print methods wrapped around System.out</summary>
		public class Out
		{
			private Out()
			{
			}

			// end static class err
			// uninstantiable
			private static void SetupOutWriter(string encoding)
			{
				if (encoding == null)
				{
					encoding = DefaultEncoding;
				}
				if (cachedOutWriter == null || !cachedOutEncoding.Equals(encoding))
				{
					try
					{
						cachedOutWriter = new PrintWriter(new OutputStreamWriter(System.Console.Out, encoding), true);
						cachedOutEncoding = encoding;
					}
					catch (UnsupportedEncodingException e)
					{
						log.Info("Error " + e + "Printing as default encoding.");
						cachedOutWriter = new PrintWriter(new OutputStreamWriter(System.Console.Out), true);
						cachedOutEncoding = string.Empty;
					}
				}
			}

			public static void Println(string o, string encoding)
			{
				SetupOutWriter(encoding);
				cachedOutWriter.Println(o);
			}

			public static void Print(string o, string encoding)
			{
				SetupOutWriter(encoding);
				cachedOutWriter.Print(o);
				cachedOutWriter.Flush();
			}

			/// <summary>
			/// Print the argument plus a NEWLINE in UTF-8, regardless of
			/// the platform default.
			/// </summary>
			/// <param name="o">String to print</param>
			public static void Println(string o)
			{
				Println(o, null);
			}

			public static void Print(string o)
			{
				Print(o, null);
			}
		}
		// end static class out
	}
}

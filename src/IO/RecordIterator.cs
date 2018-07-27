using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// For reading files or input streams which are structured as records and fields
	/// (rows and columns).
	/// </summary>
	/// <remarks>
	/// For reading files or input streams which are structured as records and fields
	/// (rows and columns).  Each time you call <code>next()</code>, you get back the
	/// next record as a list of strings.  You can specify the field delimiter (as a
	/// regular expression), how many fields to expect, and whether to filter lines
	/// containing the wrong number of fields.
	/// The iterator may be empty, if the file is empty.  If there is an
	/// <code>IOException</code> when <code>next()</code> is called, it is
	/// caught silently, and <code>null</code> is returned (!).
	/// </remarks>
	/// <author>Bill MacCartney</author>
	public class RecordIterator : IEnumerator<IList<string>>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IO.RecordIterator));

		private static string Whitespace = "\\s+";

		private BufferedReader reader;

		private int fields;

		private bool filter;

		private string delim = Whitespace;

		private IList<string> nextResult;

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified <code>Reader</code>.
		/// </summary>
		/// <param name="reader">the reader to read from</param>
		/// <param name="fields">how many fields to expect in each record</param>
		/// <param name="filter">whether to filter lines containing wrong number of fields</param>
		/// <param name="delim">a regexp on which to split lines into fields (default whitespace)</param>
		public RecordIterator(Reader reader, int fields, bool filter, string delim)
		{
			// -1 means infer from first line of input
			// factory methods -------------------------------------------------------
			this.reader = new BufferedReader(reader);
			this.fields = fields;
			this.filter = filter;
			this.delim = delim;
			if (delim == null)
			{
				this.delim = Whitespace;
			}
			Advance();
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.
		/// </summary>
		/// <param name="filename">the file to read from</param>
		/// <param name="fields">how many fields to expect in each record</param>
		/// <param name="filter">whether to filter lines containing wrong number of fields</param>
		/// <param name="delim">a regexp on which to split lines into fields (default whitespace)</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public RecordIterator(string filename, int fields, bool filter, string delim)
			: this(new FileReader(filename), fields, filter, delim)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified <code>InputStream</code>.
		/// </summary>
		/// <param name="in">the <code>InputStream</code> to read from</param>
		/// <param name="fields">how many fields to expect in each record</param>
		/// <param name="filter">whether to filter lines containing wrong number of fields</param>
		/// <param name="delim">a regexp on which to split lines into fields (default whitespace)</param>
		public RecordIterator(InputStream @in, int fields, bool filter, string delim)
			: this(new InputStreamReader(@in), fields, filter, delim)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.
		/// </summary>
		/// <remarks>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.  The default whitespace
		/// delimiter is used.
		/// </remarks>
		/// <param name="filename">the file to read from</param>
		/// <param name="fields">how many fields to expect in each record</param>
		/// <param name="filter">whether to filter lines containing wrong number of fields</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public RecordIterator(string filename, int fields, bool filter)
			: this(filename, fields, filter, Whitespace)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.
		/// </summary>
		/// <remarks>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.  The default whitespace
		/// delimiter is used.  The first line is used to determine how many
		/// fields per record to expect.
		/// </remarks>
		/// <param name="filename">the file to read from</param>
		/// <param name="filter">whether to filter lines containing wrong number of fields</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public RecordIterator(string filename, bool filter)
			: this(filename, -1, filter, Whitespace)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.
		/// </summary>
		/// <remarks>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.  The default whitespace
		/// delimiter is used.  Lines which contain other than <code>fields</code>
		/// fields are filtered.
		/// </remarks>
		/// <param name="filename">the file to read from</param>
		/// <param name="fields">how many fields to expect in each record</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public RecordIterator(string filename, int fields)
			: this(filename, fields, true, Whitespace)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.
		/// </summary>
		/// <remarks>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.  No lines are filtered.
		/// </remarks>
		/// <param name="filename">the file to read from</param>
		/// <param name="delim">a regexp on which to split lines into fields (default whitespace)</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public RecordIterator(string filename, string delim)
			: this(filename, 0, false, delim)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.
		/// </summary>
		/// <remarks>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified file.  The default whitespace
		/// delimiter is used.  No lines are filtered.
		/// </remarks>
		/// <param name="filename">the file to read from</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public RecordIterator(string filename)
			: this(filename, 0, false, Whitespace)
		{
		}

		/// <summary>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified <code>InputStream</code>.
		/// </summary>
		/// <remarks>
		/// Returns an <code>Iterator</code> over records (lists of strings)
		/// corresponding to lines in the specified <code>InputStream</code>.  The
		/// default whitespace delimiter is used.  No lines are filtered.
		/// </remarks>
		/// <param name="in">the stream to read from</param>
		public RecordIterator(InputStream @in)
			: this(@in, 0, false, Whitespace)
		{
		}

		// iterator methods ------------------------------------------------------
		public virtual bool MoveNext()
		{
			return (nextResult != null);
		}

		public virtual IList<string> Current
		{
			get
			{
				IList<string> result = nextResult;
				Advance();
				return result;
			}
		}

		public virtual void Remove()
		{
			throw new NotSupportedException();
		}

		// convenience methods ---------------------------------------------------
		/// <summary>
		/// A static convenience method that returns the first line of the
		/// specified file as list of strings, using the specified regexp as
		/// delimiter.
		/// </summary>
		/// <param name="filename">the file to read from</param>
		/// <param name="delim">a regexp on which to split lines into fields (default whitespace)</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static IList<string> FirstRecord(string filename, string delim)
		{
			Edu.Stanford.Nlp.IO.RecordIterator it = new Edu.Stanford.Nlp.IO.RecordIterator(filename, delim);
			if (!it.MoveNext())
			{
				return null;
			}
			return it.Current;
		}

		/// <summary>
		/// A static convenience method that returns the first line of the
		/// specified file as list of strings, using the default whitespace
		/// delimiter.
		/// </summary>
		/// <param name="filename">the file to read from</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static IList<string> FirstRecord(string filename)
		{
			return FirstRecord(filename, Whitespace);
		}

		/// <summary>
		/// A static convenience method that tells you how many fields are in the
		/// first line of the specified file, using the specified regexp as
		/// delimiter.
		/// </summary>
		/// <param name="filename">the file to read from</param>
		/// <param name="delim">a regexp on which to split lines into fields (default whitespace)</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static int DetermineNumFields(string filename, string delim)
		{
			IList<string> fields = FirstRecord(filename, delim);
			if (fields == null)
			{
				return -1;
			}
			else
			{
				return fields.Count;
			}
		}

		/// <summary>
		/// A static convenience method that tells you how many fields are in the
		/// first line of the specified file, using the default whitespace
		/// delimiter.
		/// </summary>
		/// <param name="filename">the file to read from</param>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static int DetermineNumFields(string filename)
		{
			return DetermineNumFields(filename, Whitespace);
		}

		// private methods -------------------------------------------------------
		private void Advance()
		{
			nextResult = null;
			while (true)
			{
				// 2 exits in body of loop
				string line = null;
				try
				{
					line = reader.ReadLine();
				}
				catch (IOException)
				{
				}
				// could block if reader is not ready
				// swallow it, yikes!
				if (line == null)
				{
					return;
				}
				// end of input: nextResult remains null
				string[] tokens = line.Split(delim);
				if (fields < 0)
				{
					fields = tokens.Length;
				}
				// remember number of fields in first line
				if (filter && (tokens.Length != fields || (tokens.Length == 1 && tokens[0].Equals(string.Empty))))
				{
					// wrong number of fields
					// it's a blank line
					continue;
				}
				// skip this line
				nextResult = new List<string>();
				foreach (string token in tokens)
				{
					nextResult.Add(token);
				}
				return;
			}
		}

		// this line will be our next result
		// -----------------------------------------------------------------------
		/// <summary>Just for testing.</summary>
		/// <remarks>
		/// Just for testing.  Reads from the file named on the command line, or from
		/// stdin, and echoes the records it reads to stdout.
		/// </remarks>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.IO.RecordIterator it = null;
			if (args.Length > 0)
			{
				it = new Edu.Stanford.Nlp.IO.RecordIterator(args[0]);
			}
			else
			{
				it = new Edu.Stanford.Nlp.IO.RecordIterator(Runtime.@in);
				log.Info("[Reading from stdin...]");
			}
			while (it != null && it.MoveNext())
			{
				IList<string> record = it.Current;
				foreach (string field in record)
				{
					System.Console.Out.Printf("[%-10s]", field);
				}
				System.Console.Out.WriteLine();
			}
		}
	}
}

/*
* Copyright (c) 2002-2004, Martian Software, Inc.
* This file is made available under the LGPL as described in the accompanying
* LICENSE.TXT file.
*/
using System.Collections;
using System.Text;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// <p>A utility class to parse a command line contained in a single String into
	/// an array of argument tokens, much as the JVM (or more accurately, your
	/// operating system) does before calling your programs' <code>public static
	/// void main(String[] args)</code>
	/// methods.</p>
	/// <p>This class has been developed to parse the command line in the same way
	/// that MS Windows 2000 does.
	/// </summary>
	/// <remarks>
	/// <p>A utility class to parse a command line contained in a single String into
	/// an array of argument tokens, much as the JVM (or more accurately, your
	/// operating system) does before calling your programs' <code>public static
	/// void main(String[] args)</code>
	/// methods.</p>
	/// <p>This class has been developed to parse the command line in the same way
	/// that MS Windows 2000 does.  Arguments containing spaces should be enclosed
	/// in quotes. Quotes that should be in the argument string should be escaped
	/// with a preceding backslash ('\') character.  Backslash characters that
	/// should be in the argument string should also be escaped with a preceding
	/// backslash character.</p>
	/// Whenever <code>JSAP.parse(String)</code> is called, the specified String is
	/// tokenized by this class, then forwarded to <code>JSAP.parse(String[])</code>
	/// for further processing.
	/// </remarks>
	/// <author><a href="http://www.martiansoftware.com/contact.html">Marty Lamb</a></author>
	public class CommandLineTokenizer
	{
		/// <summary>Hide the constructor.</summary>
		private CommandLineTokenizer()
		{
		}

		/// <summary>Goofy internal utility to avoid duplicated code.</summary>
		/// <remarks>
		/// Goofy internal utility to avoid duplicated code.  If the specified
		/// StringBuffer is not empty, its contents are appended to the resulting
		/// array (temporarily stored in the specified ArrayList).  The StringBuffer
		/// is then emptied in order to begin storing the next argument.
		/// </remarks>
		/// <param name="resultBuffer">
		/// the List temporarily storing the resulting
		/// argument array.
		/// </param>
		/// <param name="buf">the StringBuffer storing the current argument.</param>
		private static void AppendToBuffer(IList resultBuffer, StringBuilder buf)
		{
			if (buf.Length > 0)
			{
				resultBuffer.Add(buf.ToString());
				buf.Length = 0;
			}
		}

		/// <summary>Parses the specified command line into an array of individual arguments.</summary>
		/// <remarks>
		/// Parses the specified command line into an array of individual arguments.
		/// Arguments containing spaces should be enclosed in quotes.
		/// Quotes that should be in the argument string should be escaped with a
		/// preceding backslash ('\') character.  Backslash characters that should
		/// be in the argument string should also be escaped with a preceding
		/// backslash character.
		/// </remarks>
		/// <param name="commandLine">the command line to parse</param>
		/// <returns>an argument array representing the specified command line.</returns>
		public static string[] Tokenize(string commandLine)
		{
			IList resultBuffer = new ArrayList();
			if (commandLine != null)
			{
				int z = commandLine.Length;
				bool insideQuotes = false;
				StringBuilder buf = new StringBuilder();
				for (int i = 0; i < z; ++i)
				{
					char c = commandLine[i];
					if (c == '"')
					{
						AppendToBuffer(resultBuffer, buf);
						insideQuotes = !insideQuotes;
					}
					else
					{
						if (c == '\\')
						{
							if ((z > i + 1) && ((commandLine[i + 1] == '"') || (commandLine[i + 1] == '\\')))
							{
								buf.Append(commandLine[i + 1]);
								++i;
							}
							else
							{
								buf.Append("\\");
							}
						}
						else
						{
							if (insideQuotes)
							{
								buf.Append(c);
							}
							else
							{
								if (char.IsWhiteSpace(c))
								{
									AppendToBuffer(resultBuffer, buf);
								}
								else
								{
									buf.Append(c);
								}
							}
						}
					}
				}
				AppendToBuffer(resultBuffer, buf);
			}
			string[] result = new string[resultBuffer.Count];
			return ((string[])Sharpen.Collections.ToArray(resultBuffer, result));
		}
	}
}

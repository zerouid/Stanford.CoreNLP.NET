using System;
using System.Collections.Generic;
using System.IO;
using Java.IO;
using Java.Lang;
using Java.Text;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Useful methods for running shell commands, getting the process ID, checking
	/// memory usage, etc.
	/// </summary>
	/// <author>Bill MacCartney</author>
	/// <author>
	/// Steven Bethard (
	/// <see cref="Run(Java.Lang.ProcessBuilder)"/>
	/// )
	/// </author>
	public class SystemUtils
	{
		private SystemUtils()
		{
		}

		/// <summary>Runtime exception thrown by execute.</summary>
		[System.Serializable]
		public class ProcessException : Exception
		{
			private const long serialVersionUID = 1L;

			public ProcessException(string @string)
				: base(@string)
			{
			}

			public ProcessException(Exception cause)
				: base(cause)
			{
			}
			// static methods
		}

		/// <summary>Start the process defined by the ProcessBuilder, and run until complete.</summary>
		/// <remarks>
		/// Start the process defined by the ProcessBuilder, and run until complete.
		/// Process output and errors will be written to System.out and System.err,
		/// respectively.
		/// </remarks>
		/// <param name="builder">The ProcessBuilder defining the process to run.</param>
		public static void Run(ProcessBuilder builder)
		{
			Run(builder, null, null);
		}

		/// <summary>Start the process defined by the ProcessBuilder, and run until complete.</summary>
		/// <param name="builder">The ProcessBuilder defining the process to run.</param>
		/// <param name="output">
		/// Where the process output should be written. If null, the
		/// process output will be written to System.out.
		/// </param>
		/// <param name="error">
		/// Where the process error output should be written. If null,
		/// the process error output will written to System.err.
		/// </param>
		public static void Run(ProcessBuilder builder, TextWriter output, TextWriter error)
		{
			try
			{
				Process process = builder.Start();
				Consume(process, output, error);
				int result = process.WaitFor();
				if (result != 0)
				{
					string msg = "process %s exited with value %d";
					throw new SystemUtils.ProcessException(string.Format(msg, builder.Command(), result));
				}
			}
			catch (Exception e)
			{
				throw new SystemUtils.ProcessException(e);
			}
		}

		/// <summary>Helper method that consumes the output and error streams of a process.</summary>
		/// <remarks>
		/// Helper method that consumes the output and error streams of a process.
		/// This should avoid deadlocks where, e.g. the process won't complete because
		/// it is waiting for output to be read from stdout or stderr.
		/// </remarks>
		/// <param name="process">A running process.</param>
		/// <param name="outputWriter">Where to write output. If null, System.out is used.</param>
		/// <param name="errorWriter">Where to write error output. If null, System.err is used.</param>
		/// <exception cref="System.Exception"/>
		private static void Consume(Process process, TextWriter outputWriter, TextWriter errorWriter)
		{
			if (outputWriter == null)
			{
				outputWriter = new OutputStreamWriter(System.Console.Out);
			}
			if (errorWriter == null)
			{
				errorWriter = new OutputStreamWriter(System.Console.Error);
			}
			SystemUtils.WriterThread outputThread = new SystemUtils.WriterThread(process.GetInputStream(), outputWriter);
			SystemUtils.WriterThread errorThread = new SystemUtils.WriterThread(process.GetErrorStream(), errorWriter);
			outputThread.Start();
			errorThread.Start();
			outputThread.Join();
			errorThread.Join();
		}

		/// <summary>Thread that reads from an Reader and writes to a Writer.</summary>
		/// <remarks>
		/// Thread that reads from an Reader and writes to a Writer.
		/// Used as a helper for
		/// <see cref="#consume"/>
		/// to avoid deadlocks.
		/// </remarks>
		private class WriterThread : Thread
		{
			private Reader reader;

			private TextWriter writer;

			public WriterThread(InputStream inputStream, TextWriter writer)
			{
				this.reader = new InputStreamReader(inputStream);
				this.writer = writer;
			}

			public override void Run()
			{
				char[] buffer = new char[4096];
				while (true)
				{
					try
					{
						int read = this.reader.Read(buffer);
						if (read == -1)
						{
							break;
						}
						this.writer.Write(buffer, 0, read);
						this.writer.Flush();
					}
					catch (IOException e)
					{
						throw new SystemUtils.ProcessException(e);
					}
					Thread.Yield();
				}
			}
		}

		/// <summary>Helper class that acts as a output stream to a process</summary>
		public class ProcessOutputStream : OutputStream
		{
			private Process process;

			private Thread outWriterThread;

			private Thread errWriterThread;

			/// <exception cref="System.IO.IOException"/>
			public ProcessOutputStream(string[] cmd)
				: this(new ProcessBuilder(cmd), new PrintWriter(System.Console.Out), new PrintWriter(System.Console.Error))
			{
			}

			/// <exception cref="System.IO.IOException"/>
			public ProcessOutputStream(string[] cmd, TextWriter writer)
				: this(new ProcessBuilder(cmd), writer, writer)
			{
			}

			/// <exception cref="System.IO.IOException"/>
			public ProcessOutputStream(string[] cmd, TextWriter output, TextWriter error)
				: this(new ProcessBuilder(cmd), output, error)
			{
			}

			/// <exception cref="System.IO.IOException"/>
			public ProcessOutputStream(ProcessBuilder builder, TextWriter output, TextWriter error)
			{
				this.process = builder.Start();
				errWriterThread = new StreamGobbler(process.GetErrorStream(), error);
				outWriterThread = new StreamGobbler(process.GetInputStream(), output);
				errWriterThread.Start();
				outWriterThread.Start();
			}

			/// <exception cref="System.IO.IOException"/>
			public override void Flush()
			{
				process.GetOutputStream().Flush();
			}

			/// <exception cref="System.IO.IOException"/>
			public override void Write(int b)
			{
				process.GetOutputStream().Write(b);
			}

			/// <exception cref="System.IO.IOException"/>
			public override void Close()
			{
				process.GetOutputStream().Close();
				try
				{
					errWriterThread.Join();
					outWriterThread.Join();
					process.WaitFor();
				}
				catch (Exception e)
				{
					throw new SystemUtils.ProcessException(e);
				}
			}
		}

		// end static class StaticOutputStream
		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// array.  If there is any regular output or error
		/// output, it is appended to the given
		/// <c>StringBuilder</c>
		/// s.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string[] cmd, StringBuilder outputLines, StringBuilder errorLines)
		{
			RunShellCommand(cmd, null, outputLines, errorLines);
		}

		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// array.  If there is any regular output or error
		/// output, it is appended to the given
		/// <c>StringBuilder</c>
		/// s.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string[] cmd, File dir, StringBuilder outputLines, StringBuilder errorLines)
		{
			Process p = Runtime.GetRuntime().Exec(cmd, null, dir);
			if (outputLines != null)
			{
				using (BufferedReader @in = new BufferedReader(new InputStreamReader(p.GetInputStream())))
				{
					for (string line; (line = @in.ReadLine()) != null; )
					{
						outputLines.Append(line).Append("\n");
					}
				}
			}
			if (errorLines != null)
			{
				using (BufferedReader err = new BufferedReader(new InputStreamReader(p.GetErrorStream())))
				{
					for (string line; (line = err.ReadLine()) != null; )
					{
						errorLines.Append(line).Append("\n");
					}
				}
			}
		}

		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// .  If there is any regular output or error output,
		/// it is appended to the given
		/// <c>StringBuilder</c>
		/// s.
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string cmd, StringBuilder outputLines, StringBuilder errorLines)
		{
			RunShellCommand(new string[] { cmd }, outputLines, errorLines);
		}

		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// array.  If there is any regular output, it is
		/// appended to the given
		/// <c>StringBuilder</c>
		/// .  If there is any error
		/// output, it is swallowed (!).
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string[] cmd, StringBuilder outputLines)
		{
			RunShellCommand(cmd, outputLines, null);
		}

		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// .  If there is any regular output, it is appended
		/// to the given
		/// <c>StringBuilder</c>
		/// .  If there is any error output, it
		/// is swallowed (!).
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string cmd, StringBuilder outputLines)
		{
			RunShellCommand(new string[] { cmd }, outputLines, null);
		}

		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// array.  If there is any output, it is swallowed (!).
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string[] cmd)
		{
			RunShellCommand(cmd, null, null);
		}

		/// <summary>
		/// Runs the shell command which is specified, along with its arguments, in the
		/// given
		/// <c>String</c>
		/// .  If there is any output, it is swallowed (!).
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void RunShellCommand(string cmd)
		{
			RunShellCommand(new string[] { cmd }, null, null);
		}

		/// <summary>Returns the process ID, via an awful hack.</summary>
		/// <exception cref="System.IO.IOException"/>
		public static int GetPID()
		{
			// note that we ask Perl for "ppid" -- process ID of parent -- that's us
			string[] cmd = new string[] { "perl", "-e", "print getppid() . \"\\n\";" };
			StringBuilder @out = new StringBuilder();
			RunShellCommand(cmd, @out);
			return System.Convert.ToInt32(@out.ToString());
		}

		/// <summary>Returns the process ID, via an awful hack, or else -1.</summary>
		public static int GetPIDNoExceptions()
		{
			try
			{
				return SystemUtils.GetPID();
			}
			catch (IOException)
			{
				return -1;
			}
		}

		/// <summary>Returns the number of megabytes (MB) of memory in use.</summary>
		public static int GetMemoryInUse()
		{
			Runtime runtime = Runtime.GetRuntime();
			long mb = 1024 * 1024;
			long total = runtime.TotalMemory();
			long free = runtime.FreeMemory();
			return (int)((total - free) / mb);
		}

		/// <summary>Returns the string value of the stack trace for the given Throwable.</summary>
		public static string GetStackTraceString(Exception t)
		{
			ByteArrayOutputStream bs = new ByteArrayOutputStream();
			Sharpen.Runtime.PrintStackTrace(t, new TextWriter(bs));
			return bs.ToString();
		}

		// ----------------------------------------------------------------------------
		/// <summary>
		/// Returns a String representing the current date and time in the given
		/// format.
		/// </summary>
		/// <seealso><a href="http://java.sun.com/j2se/1.5.0/docs/api/java/text/SimpleDateFormat.html">SimpleDateFormat</a></seealso>
		public static string GetTimestampString(string fmt)
		{
			return (new SimpleDateFormat(fmt)).Format(new DateTime());
		}

		/// <summary>
		/// Returns a String representing the current date and time in the format
		/// "20071022-140522".
		/// </summary>
		public static string GetTimestampString()
		{
			return GetTimestampString("yyyyMMdd-HHmmss");
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			StringBuilder @out = new StringBuilder();
			RunShellCommand("date", @out);
			System.Console.Out.WriteLine("The date is " + @out);
			int pid = GetPID();
			System.Console.Out.WriteLine("The PID is " + pid);
			System.Console.Out.WriteLine("The memory in use is " + GetMemoryInUse() + "MB");
			IList<string> foo = new List<string>();
			for (int i = 0; i < 5000000; i++)
			{
				foo.Add("0123456789");
			}
			System.Console.Out.WriteLine("The memory in use is " + GetMemoryInUse() + "MB");
			foo = null;
			Runtime.Gc();
			System.Console.Out.WriteLine("The memory in use is " + GetMemoryInUse() + "MB");
		}
	}
}

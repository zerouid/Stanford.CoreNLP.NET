using System;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Opens a outputstream for writing into a bzip2 file by piping into the bzip2 command.</summary>
	/// <remarks>
	/// Opens a outputstream for writing into a bzip2 file by piping into the bzip2 command.
	/// Output from bzip2 command is written into the specified file.
	/// </remarks>
	/// <author>Angel Chang</author>
	public class BZip2PipedOutputStream : OutputStream
	{
		private string filename;

		private Process process;

		private ByteStreamGobbler outGobbler;

		private StreamGobbler errGobbler;

		private PrintWriter errWriter;

		/// <exception cref="System.IO.IOException"/>
		public BZip2PipedOutputStream(string filename)
			: this(filename, System.Console.Error)
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public BZip2PipedOutputStream(string filename, OutputStream err)
		{
			string bzip2 = Runtime.GetProperty("bzip2", "bzip2");
			string cmd = bzip2;
			// + " > " + filename;
			//log.info("getBZip2PipedOutputStream: Running command: "+cmd);
			ProcessBuilder pb = new ProcessBuilder();
			pb.Command(cmd);
			this.process = pb.Start();
			this.filename = filename;
			OutputStream outStream = new FileOutputStream(filename);
			errWriter = new PrintWriter(new BufferedWriter(new OutputStreamWriter(err)));
			outGobbler = new ByteStreamGobbler("Output stream gobbler: " + cmd + " " + filename, process.GetInputStream(), outStream);
			errGobbler = new StreamGobbler(process.GetErrorStream(), errWriter);
			outGobbler.Start();
			errGobbler.Start();
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
				outGobbler.Join();
				errGobbler.Join();
				outGobbler.GetOutputStream().Close();
				process.WaitFor();
			}
			catch (Exception ex)
			{
				throw new RuntimeInterruptedException(ex);
			}
		}
		//log.info("getBZip2PipedOutputStream: Closed. ");
	}
}

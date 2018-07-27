using System;




namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Stream Gobbler that read and write bytes
	/// (can be used to gobble byte based stdout from a process.exec into a file)
	/// </summary>
	/// <author>Angel Chang</author>
	public class ByteStreamGobbler : Thread
	{
		internal InputStream inStream;

		internal OutputStream outStream;

		internal int bufferSize = 4096;

		public ByteStreamGobbler(InputStream @is, OutputStream @out)
		{
			this.inStream = new BufferedInputStream(@is);
			this.outStream = new BufferedOutputStream(@out);
		}

		public ByteStreamGobbler(string name, InputStream @is, OutputStream @out)
			: base(name)
		{
			this.inStream = new BufferedInputStream(@is);
			this.outStream = new BufferedOutputStream(@out);
		}

		public ByteStreamGobbler(string name, InputStream @is, OutputStream @out, int bufferSize)
			: base(name)
		{
			this.inStream = new BufferedInputStream(@is);
			this.outStream = new BufferedOutputStream(@out);
			if (bufferSize <= 0)
			{
				throw new ArgumentException("Invalid buffer size " + bufferSize + ": must be larger than 0");
			}
			this.bufferSize = bufferSize;
		}

		public virtual InputStream GetInputStream()
		{
			return inStream;
		}

		public virtual OutputStream GetOutputStream()
		{
			return outStream;
		}

		public override void Run()
		{
			try
			{
				byte[] b = new byte[bufferSize];
				int bytesRead;
				while ((bytesRead = inStream.Read(b)) >= 0)
				{
					if (bytesRead > 0)
					{
						outStream.Write(b, 0, bytesRead);
					}
				}
				inStream.Close();
			}
			catch (Exception ex)
			{
				System.Console.Out.WriteLine("Problem reading stream :" + inStream.GetType().GetCanonicalName() + " " + ex);
				Sharpen.Runtime.PrintStackTrace(ex);
			}
		}
	}
}

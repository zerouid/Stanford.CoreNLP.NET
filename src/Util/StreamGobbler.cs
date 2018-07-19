using System;
using System.IO;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Reads the output of a process started by Process.exec()
	/// Adapted from:
	/// http://www.velocityreviews.com/forums/t130884-process-runtimeexec-causes-subprocess-hang.html
	/// </summary>
	/// <author>pado</author>
	public class StreamGobbler : Thread
	{
		internal InputStream @is;

		internal TextWriter outputFileHandle;

		internal bool shouldRun = true;

		public StreamGobbler(InputStream @is, TextWriter outputFileHandle)
		{
			this.@is = @is;
			this.outputFileHandle = outputFileHandle;
			this.SetDaemon(true);
		}

		public virtual void Kill()
		{
			this.shouldRun = false;
		}

		public override void Run()
		{
			try
			{
				InputStreamReader isr = new InputStreamReader(@is);
				BufferedReader br = new BufferedReader(isr);
				string s = null;
				//noinspection ConstantConditions
				while (s == null && shouldRun)
				{
					while ((s = br.ReadLine()) != null)
					{
						outputFileHandle.Write(s);
						outputFileHandle.Write("\n");
					}
					Thread.Sleep(1000);
				}
				isr.Close();
				br.Close();
				outputFileHandle.Flush();
			}
			catch (Exception ex)
			{
				System.Console.Out.WriteLine("Problem reading stream :" + @is.GetType().GetCanonicalName() + " " + ex);
				Sharpen.Runtime.PrintStackTrace(ex);
			}
		}
	}
}

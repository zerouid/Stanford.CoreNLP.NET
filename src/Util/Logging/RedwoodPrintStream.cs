using System;
using System.IO;




namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>A PrintStream that writes to Redwood logs.</summary>
	/// <remarks>
	/// A PrintStream that writes to Redwood logs.
	/// The primary use of this class is to override System.out and System.err.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	/// <author>Kevin Reschke (kreschke at cs.stanford)</author>
	public class RedwoodPrintStream : TextWriter
	{
		private readonly Redwood.Flag tag;

		private readonly TextWriter realStream;

		private StringBuilder buffer = new StringBuilder();

		private bool checkForThrowable = false;

		public RedwoodPrintStream(Redwood.Flag tag, TextWriter realStream)
			: base(realStream)
		{
			this.tag = tag;
			this.realStream = realStream;
		}

		private void Log(object message)
		{
			lock (this)
			{
				if (buffer.Length > 0)
				{
					LogB(message);
					LogB("\n");
				}
				else
				{
					if (tag != null)
					{
						Redwood.Log(tag, message);
					}
					else
					{
						Redwood.Log(message);
					}
				}
			}
		}

		private void Logf(string format, object[] args)
		{
			lock (this)
			{
				if (tag != null)
				{
					Redwood.Channels(tag).Logf(format, args);
				}
				else
				{
					Redwood.Logf(format, args);
				}
			}
		}

		private void LogB(object message)
		{
			lock (this)
			{
				char[] str = message.ToString().ToCharArray();
				foreach (char c in str)
				{
					if (c == '\n')
					{
						string msg = buffer.ToString();
						if (tag != null)
						{
							Redwood.Log(tag, msg);
						}
						else
						{
							Redwood.Log(msg);
						}
						buffer = new StringBuilder();
					}
					else
					{
						buffer.Append(c.ToString());
					}
				}
			}
		}

		public override void Flush()
		{
			realStream.Flush();
		}

		public override void Close()
		{
		}

		public override bool CheckError()
		{
			return false;
		}

		protected override void SetError()
		{
		}

		protected override void ClearError()
		{
		}

		public override void Write(bool b)
		{
			LogB(b);
		}

		public override void Write(char c)
		{
			LogB(c);
		}

		public override void Write(int i)
		{
			LogB(i);
		}

		public override void Write(long l)
		{
			LogB(l);
		}

		public override void Write(float f)
		{
			LogB(f);
		}

		public override void Write(double d)
		{
			LogB(d);
		}

		public override void Write(char[] chars)
		{
			LogB(new string(chars));
		}

		public override void Write(string s)
		{
			LogB(s);
		}

		public override void Write(object o)
		{
			LogB(o);
		}

		public override void WriteLine(bool b)
		{
			Log(b);
		}

		public override void WriteLine(char c)
		{
			Log(c);
		}

		public override void WriteLine(int i)
		{
			Log(i);
		}

		public override void WriteLine(long l)
		{
			Log(l);
		}

		public override void WriteLine(float f)
		{
			Log(f);
		}

		public override void WriteLine(double d)
		{
			Log(d);
		}

		public override void WriteLine(char[] chars)
		{
			Log(new string(chars));
		}

		public override void WriteLine(string s)
		{
			if (checkForThrowable)
			{
				//(check if from throwable)
				bool fromThrowable = false;
				foreach (StackTraceElement e in Thread.CurrentThread().GetStackTrace())
				{
					if (e.GetClassName().Equals(typeof(Exception).FullName))
					{
						fromThrowable = true;
					}
				}
				//(handle message appropriately)
				if (fromThrowable)
				{
					realStream.WriteLine(s);
				}
				else
				{
					Log(s);
					checkForThrowable = false;
				}
			}
			else
			{
				Log(s);
			}
		}

		public override void WriteLine(object o)
		{
			if (o is Exception)
			{
				realStream.WriteLine(o);
				Flush();
				checkForThrowable = true;
			}
			else
			{
				Log(o);
			}
		}

		public override void WriteLine()
		{
			Log(string.Empty);
		}

		public override TextWriter Printf(string s, params object[] objects)
		{
			Logf(s, objects);
			return this;
		}

		public override TextWriter Printf(Locale locale, string s, params object[] objects)
		{
			Logf(s, objects);
			return this;
		}

		public override TextWriter Format(string s, params object[] objects)
		{
			Logf(s, objects);
			return this;
		}

		public override TextWriter Format(Locale locale, string s, params object[] objects)
		{
			Logf(s, objects);
			return this;
		}

		public override TextWriter Append(ICharSequence charSequence)
		{
			LogB(charSequence);
			return this;
		}

		public override TextWriter Append(ICharSequence charSequence, int i, int i1)
		{
			LogB(charSequence.SubSequence(i, i1));
			return this;
		}

		public override TextWriter Append(char c)
		{
			LogB(c);
			return this;
		}
	}
}

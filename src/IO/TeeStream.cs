


namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// This class splits the calls to an OutputStream into two different
	/// streams.
	/// </summary>
	/// <author>John Bauer</author>
	public class TeeStream : OutputStream, ICloseable, IFlushable
	{
		public TeeStream(OutputStream s1, OutputStream s2)
		{
			this.s1 = s1;
			this.s2 = s2;
		}

		internal OutputStream s1;

		internal OutputStream s2;

		/// <exception cref="System.IO.IOException"/>
		public override void Close()
		{
			s1.Close();
			s2.Close();
		}

		/// <exception cref="System.IO.IOException"/>
		public override void Flush()
		{
			s1.Flush();
			s2.Flush();
		}

		/// <exception cref="System.IO.IOException"/>
		public override void Write(byte[] b)
		{
			s1.Write(b);
			s2.Write(b);
		}

		/// <exception cref="System.IO.IOException"/>
		public override void Write(byte[] b, int off, int len)
		{
			s1.Write(b, off, len);
			s2.Write(b, off, len);
		}

		/// <exception cref="System.IO.IOException"/>
		public override void Write(int b)
		{
			s1.Write(b);
			s2.Write(b);
		}
	}
}

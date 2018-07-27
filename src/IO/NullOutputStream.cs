


namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// An OutputStream which throws away all output instead of outputting anything
	/// <br />
	/// Taken from http://stackoverflow.com/questions/2127979
	/// </summary>
	/// <author>John Bauer</author>
	public class NullOutputStream : OutputStream
	{
		/// <exception cref="System.IO.IOException"/>
		public override void Write(int i)
		{
		}

		// do nothing
		public override void Write(byte[] b, int off, int len)
		{
		}

		// still do nothing
		public override void Write(byte[] b)
		{
		}

		// this doesn't do anything either
		public override void Flush()
		{
		}
		// write all buffered text.  
		// just kidding, it actually does nothing
	}
}

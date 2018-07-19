using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// An
	/// <c>OutputStream</c>
	/// that can be turned into a
	/// <c>String</c>
	/// .
	/// </summary>
	/// <author>Bill MacCartney</author>
	public class StringOutputStream : OutputStream
	{
		private readonly StringBuilder sb = new StringBuilder();

		public StringOutputStream()
		{
		}

		public virtual void Clear()
		{
			lock (this)
			{
				sb.Length = 0;
			}
		}

		public override void Write(int i)
		{
			lock (this)
			{
				sb.Append((char)i);
			}
		}

		public override string ToString()
		{
			lock (this)
			{
				return sb.ToString();
			}
		}
	}
}

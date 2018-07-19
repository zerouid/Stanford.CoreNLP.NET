using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Common
{
	[System.Serializable]
	public class NoSuchParseException : NoSuchElementException
	{
		private const long serialVersionUID = 2;

		public NoSuchParseException()
			: base()
		{
		}

		public NoSuchParseException(string error)
			: base(error)
		{
		}
	}
}

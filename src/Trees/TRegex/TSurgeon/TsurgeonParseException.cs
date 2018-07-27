using System;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>
	/// A runtime exception that indicates something went wrong parsing a
	/// Tsurgeon expression.
	/// </summary>
	/// <remarks>
	/// A runtime exception that indicates something went wrong parsing a
	/// Tsurgeon expression.  The purpose is to make those exceptions
	/// unchecked exceptions, as there are only a few circumstances in
	/// which one could recover.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class TsurgeonParseException : Exception
	{
		private const long serialVersionUID = -4417368416943652737L;

		public TsurgeonParseException(string message)
			: base(message)
		{
		}

		public TsurgeonParseException(string message, Exception cause)
			: base(message, cause)
		{
		}
	}
}

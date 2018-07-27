using System;


namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>
	/// A runtime exception that indicates something went wrong parsing a
	/// tregex expression.
	/// </summary>
	/// <remarks>
	/// A runtime exception that indicates something went wrong parsing a
	/// tregex expression.  The purpose is to make those exceptions
	/// unchecked exceptions, as there are only a few circumstances in
	/// which one could recover.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class TregexParseException : Exception
	{
		public TregexParseException(string message, Exception cause)
			: base(message, cause)
		{
		}
	}
}

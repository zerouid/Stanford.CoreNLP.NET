using System;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>
	/// A runtime exception that indicates something went wrong parsing a
	/// semgrex expression.
	/// </summary>
	/// <remarks>
	/// A runtime exception that indicates something went wrong parsing a
	/// semgrex expression.  The purpose is to make those exceptions
	/// unchecked exceptions, as there are only a few circumstances in
	/// which one could recover.
	/// </remarks>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class SemgrexParseException : Exception
	{
		public SemgrexParseException(string message, Exception cause)
			: base(message, cause)
		{
		}
	}
}

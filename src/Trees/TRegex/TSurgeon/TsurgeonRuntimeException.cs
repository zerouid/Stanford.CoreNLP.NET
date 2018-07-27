using System;


namespace Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon
{
	/// <summary>Something has gone wrong internally in Tsurgeon</summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class TsurgeonRuntimeException : Exception
	{
		private const long serialVersionUID = 1;

		/// <summary>Creates a new exception with a message.</summary>
		/// <param name="message">the message for the exception</param>
		public TsurgeonRuntimeException(string message)
			: base(message)
		{
		}
	}
}

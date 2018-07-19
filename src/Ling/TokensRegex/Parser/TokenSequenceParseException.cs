using System;
using Sharpen;

namespace Edu.Stanford.Nlp.Ling.Tokensregex.Parser
{
	/// <summary>Created by sonalg on 2/5/15.</summary>
	[System.Serializable]
	public class TokenSequenceParseException : Exception
	{
		public TokenSequenceParseException()
			: base()
		{
		}

		public TokenSequenceParseException(string msg)
			: base(msg)
		{
		}

		public TokenSequenceParseException(string message, Exception throwable)
			: base(message, throwable)
		{
		}
	}
}

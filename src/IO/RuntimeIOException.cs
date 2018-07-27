using System;


namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// An unchecked version of
	/// <see cref="System.IO.IOException"/>
	/// . Thrown by
	/// <see cref="Edu.Stanford.Nlp.Process.ITokenizer{T}"/>
	/// implementing classes,
	/// among other things.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	[System.Serializable]
	public class RuntimeIOException : Exception
	{
		private const long serialVersionUID = -8572218999165094626L;

		/// <summary>Creates a new exception.</summary>
		public RuntimeIOException()
		{
		}

		/// <summary>Creates a new exception with a message.</summary>
		/// <param name="message">the message for the exception</param>
		public RuntimeIOException(string message)
			: base(message)
		{
		}

		/// <summary>Creates a new exception with an embedded cause.</summary>
		/// <param name="cause">The cause for the exception</param>
		public RuntimeIOException(Exception cause)
			: base(cause)
		{
		}

		/// <summary>Creates a new exception with a message and an embedded cause.</summary>
		/// <param name="message">the message for the exception</param>
		/// <param name="cause">The cause for the exception</param>
		public RuntimeIOException(string message, Exception cause)
			: base(message, cause)
		{
		}
	}
}

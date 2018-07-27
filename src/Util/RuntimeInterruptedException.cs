using System;


namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// An unchecked version of
	/// <see cref="System.Exception"/>
	/// . Thrown by
	/// classes that pay attention to if they were interrupted, such as the LexicalizedParser.
	/// </summary>
	/// <author>John Bauer</author>
	[System.Serializable]
	public class RuntimeInterruptedException : Exception
	{
		public RuntimeInterruptedException()
			: base()
		{
		}

		public RuntimeInterruptedException(Exception e)
			: base(e)
		{
		}
	}
}

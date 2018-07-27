using System.IO;



namespace Edu.Stanford.Nlp.IO
{
	/// <summary>Shorthand class for opening an output file for human-readable output.</summary>
	/// <remarks>
	/// Shorthand class for opening an output file for human-readable output.
	/// com:bruceeckel:tools:PrintFile.java
	/// </remarks>
	public class PrintFile : TextWriter
	{
		/// <exception cref="System.IO.IOException"/>
		public PrintFile(string filename)
			: base(new BufferedOutputStream(new FileOutputStream(filename)))
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public PrintFile(File file)
			: this(file.GetPath())
		{
		}
	}
}

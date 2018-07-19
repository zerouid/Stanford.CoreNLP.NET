using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>ANSI supported styles (rather, a subset of).</summary>
	/// <remarks>
	/// ANSI supported styles (rather, a subset of).
	/// These values are mirrored in Redwood.Util.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	[System.Serializable]
	public sealed class Style
	{
		public static readonly Edu.Stanford.Nlp.Util.Logging.Style None = new Edu.Stanford.Nlp.Util.Logging.Style(string.Empty);

		public static readonly Edu.Stanford.Nlp.Util.Logging.Style Bold = new Edu.Stanford.Nlp.Util.Logging.Style("\x21[1m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Style Dim = new Edu.Stanford.Nlp.Util.Logging.Style("\x21[2m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Style Italic = new Edu.Stanford.Nlp.Util.Logging.Style("\x21[3m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Style Underline = new Edu.Stanford.Nlp.Util.Logging.Style("\x21[4m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Style Blink = new Edu.Stanford.Nlp.Util.Logging.Style("\x21[5m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Style CrossOut = new Edu.Stanford.Nlp.Util.Logging.Style("\x21[9m");

		public readonly string ansiCode;

		internal Style(string ansiCode)
		{
			this.ansiCode = ansiCode;
		}

		public string Apply(string toColor)
		{
			StringBuilder b = new StringBuilder();
			if (Redwood.supportsAnsi)
			{
				b.Append(Edu.Stanford.Nlp.Util.Logging.Style.ansiCode);
			}
			b.Append(toColor);
			if (Redwood.supportsAnsi)
			{
				b.Append("\x21[0m");
			}
			return b.ToString();
		}
	}
}




using System.Text;

namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>ANSI supported colors.</summary>
	/// <remarks>
	/// ANSI supported colors.
	/// These values are mirrored in Redwood.Util.
	/// </remarks>
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	[System.Serializable]
	public sealed class Color
	{
		public static readonly Edu.Stanford.Nlp.Util.Logging.Color None = new Edu.Stanford.Nlp.Util.Logging.Color(string.Empty);

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Black = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[30m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color White = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[37m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Red = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[31m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Green = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[32m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Yellow = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[33m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Blue = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[34m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Magenta = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[35m");

		public static readonly Edu.Stanford.Nlp.Util.Logging.Color Cyan = new Edu.Stanford.Nlp.Util.Logging.Color("\x21[36m");

		public readonly string ansiCode;

		internal Color(string ansiCode)
		{
			//note: NONE BLACK and WHITE must be first three (for random colors in OutputHandler to work)
			this.ansiCode = ansiCode;
		}

		public string Apply(string toColor)
		{
			StringBuilder b = new StringBuilder();
			if (Redwood.supportsAnsi)
			{
				b.Append(ansiCode);
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

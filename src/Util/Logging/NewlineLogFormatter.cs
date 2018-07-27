


namespace Edu.Stanford.Nlp.Util.Logging
{
	/// <summary>Simply format and put a newline after each log message.</summary>
	/// <author>Heeyoung Lee</author>
	public class NewlineLogFormatter : Formatter
	{
		public override string Format(LogRecord rec)
		{
			return FormatMessage(rec) + '\n';
		}
	}
}

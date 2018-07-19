using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>
	/// Simple utility class: reads the environment variable in
	/// ENV_VARIABLE and provides a method that converts strings which
	/// start with that environment variable to file paths.
	/// </summary>
	/// <remarks>
	/// Simple utility class: reads the environment variable in
	/// ENV_VARIABLE and provides a method that converts strings which
	/// start with that environment variable to file paths.  For example,
	/// you can send it
	/// "$NLP_DATA_HOME/data/pos-tagger/wsj3t0-18-left3words"
	/// and it will convert that to
	/// "/u/nlp/data/pos-tagger/wsj3t0-18-left3words"
	/// unless you have set $NLP_DATA_HOME to something else.
	/// <br />
	/// The only environment variable expanded is that defined by
	/// ENV_VARIABLE, and the only place in the string it is expanded is at
	/// the start of the string.
	/// </remarks>
	/// <author>John Bauer</author>
	public class DataFilePaths
	{
		private DataFilePaths()
		{
		}

		private const string NlpDataVariable = "NLP_DATA_HOME";

		private const string NlpDataVariablePrefix = '$' + NlpDataVariable;

		private static readonly string NlpDataHome = ((Runtime.Getenv(NlpDataVariable) != null) ? Runtime.Getenv(NlpDataVariable) : "/u/nlp");

		private const string JavanlpVariable = "JAVANLP_HOME";

		private const string JavanlpVariablePrefix = '$' + JavanlpVariable;

		private static readonly string JavanlpHome = ((Runtime.Getenv(JavanlpVariable) != null) ? Runtime.Getenv(JavanlpVariable) : ".");

		public static string Convert(string path)
		{
			if (path.StartsWith(NlpDataVariablePrefix))
			{
				return NlpDataHome + Sharpen.Runtime.Substring(path, NlpDataVariablePrefix.Length);
			}
			if (path.StartsWith(JavanlpVariablePrefix))
			{
				return JavanlpHome + Sharpen.Runtime.Substring(path, JavanlpVariablePrefix.Length);
			}
			return path;
		}
	}
}

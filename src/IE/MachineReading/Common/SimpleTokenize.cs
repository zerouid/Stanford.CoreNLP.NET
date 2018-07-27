using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Machinereading.Common
{
	/// <summary>Simple string tokenization</summary>
	public class SimpleTokenize
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(SimpleTokenize));

		/// <summary>Basic string tokenization, skipping over white spaces</summary>
		public static List<string> Tokenize(string line)
		{
			List<string> tokens = new List<string>();
			StringTokenizer tokenizer = new StringTokenizer(line);
			while (tokenizer.MoveNext())
			{
				tokens.Add(tokenizer.NextToken());
			}
			return tokens;
		}

		/// <summary>Basic string tokenization, skipping over white spaces</summary>
		public static List<string> Tokenize(string line, string separators)
		{
			List<string> tokens = new List<string>();
			StringTokenizer tokenizer = new StringTokenizer(line, separators);
			while (tokenizer.MoveNext())
			{
				tokens.Add(tokenizer.NextToken());
			}
			return tokens;
		}

		/// <summary>Finds the first non-whitespace character starting at start</summary>
		private static int FindNonWhitespace(string s, int start)
		{
			for (; start < s.Length; start++)
			{
				if (char.IsWhiteSpace(s[start]) == false)
				{
					return start;
				}
			}
			return -1;
		}

		private static int FindWhitespace(string s, int start)
		{
			for (; start < s.Length; start++)
			{
				if (char.IsWhiteSpace(s[start]))
				{
					return start;
				}
			}
			return -1;
		}

		/// <summary>Replaces all occurences of \" with "</summary>
		private static string NormalizeQuotes(string str)
		{
			StringBuilder buffer = new StringBuilder();
			for (int i = 0; i < str.Length; i++)
			{
				// do not include \ if followed by "
				if (str[i] == '\\' && i < str.Length - 1 && str[i + 1] == '\"')
				{
					continue;
				}
				else
				{
					buffer.Append(str[i]);
				}
			}
			return buffer.ToString();
		}

		/// <summary>
		/// String tokenization, considering everything within quotes as 1 token
		/// Regular quotes inside tokens MUST be preceded by \
		/// </summary>
		public static List<string> TokenizeWithQuotes(string line)
		{
			List<string> tokens = new List<string>();
			int position = 0;
			while ((position = FindNonWhitespace(line, position)) != -1)
			{
				int end = -1;
				// found quoted token (not preceded by \)
				if (line[position] == '\"' && (position == 0 || line[position - 1] != '\\'))
				{
					// find the first quote not preceded by \
					int current = position;
					for (; ; )
					{
						// found end of string first
						if ((end = line.IndexOf('\"', current + 1)) == -1)
						{
							end = line.Length;
							break;
						}
						else
						{
							// found a quote
							if (line[end - 1] != '\\')
							{
								// valid quote
								end++;
								break;
							}
							else
							{
								// quote preceded by \
								current = end;
							}
						}
					}
					// do not include the quotes in the token
					tokens.Add(NormalizeQuotes(Sharpen.Runtime.Substring(line, position + 1, end - 1)));
				}
				else
				{
					// regular token
					if ((end = FindWhitespace(line, position + 1)) == -1)
					{
						end = line.Length;
					}
					tokens.Add(new string(Sharpen.Runtime.Substring(line, position, end)));
				}
				position = end;
			}
			return tokens;
		}

		/// <summary>
		/// Constructs a valid quote-surrounded token All inside quotes are preceded by
		/// \
		/// </summary>
		public static string Quotify(string str)
		{
			StringBuilder buffer = new StringBuilder();
			buffer.Append('\"');
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] == '\"')
				{
					buffer.Append('\\');
				}
				buffer.Append(str[i]);
			}
			buffer.Append('\"');
			return buffer.ToString();
		}

		/// <summary>Implements a simple test</summary>
		public static void Main(string[] argv)
		{
			string @in = "T \"Athens \\\"the beautiful\\\"\" \"Athens\" \"\" \"Greece\"";
			log.Info("Input: " + @in);
			log.Info(TokenizeWithQuotes(@in));
		}
	}
}

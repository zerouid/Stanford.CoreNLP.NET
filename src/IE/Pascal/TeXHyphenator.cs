using System;
using System.Collections;
using System.IO;
using System.Text;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.IE.Pascal
{
	/// <summary>Hyphenates words according to the TeX algorithm.</summary>
	/// <author>Jamie Nicolson (nicolson@cs.stanford.edu)</author>
	public class TeXHyphenator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(TeXHyphenator));

		private class Node
		{
			internal Hashtable children = new Hashtable();

			internal int[] pattern = null;
		}

		/// <summary>Loads the default hyphenation rules in DefaultTeXHyphenator.</summary>
		public virtual void LoadDefault()
		{
			try
			{
				Load(new BufferedReader(new StringReader(DefaultTeXHyphenData.hyphenData)));
			}
			catch (IOException e)
			{
				// shouldn't happen
				throw new Exception(e);
			}
		}

		/// <summary>Loads custom hyphenation rules.</summary>
		/// <remarks>
		/// Loads custom hyphenation rules. You probably want to use
		/// loadDefault() instead.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public virtual void Load(BufferedReader input)
		{
			string line;
			while ((line = input.ReadLine()) != null)
			{
				if (StringUtils.Matches(line, "\\s*(%.*)?"))
				{
					// comment or blank line
					log.Info("Skipping: " + line);
					continue;
				}
				char[] linechars = line.ToCharArray();
				int[] pattern = new int[linechars.Length];
				char[] chars = new char[linechars.Length];
				int c = 0;
				foreach (char linechar in linechars)
				{
					if (char.IsDigit(linechar))
					{
						pattern[c] = char.Digit(linechar, 10);
					}
					else
					{
						chars[c++] = linechar;
					}
				}
				char[] shortchars = new char[c];
				int[] shortpattern = new int[c + 1];
				System.Array.Copy(chars, 0, shortchars, 0, c);
				System.Array.Copy(pattern, 0, shortpattern, 0, c + 1);
				InsertHyphPattern(shortchars, shortpattern);
			}
		}

		private TeXHyphenator.Node head = new TeXHyphenator.Node();

		public static string ToString(int[] i)
		{
			StringBuilder sb = new StringBuilder();
			foreach (int anI in i)
			{
				sb.Append(anI);
			}
			return sb.ToString();
		}

		private void InsertHyphPattern(char[] chars, int[] pattern)
		{
			// find target node, building as we go
			TeXHyphenator.Node cur = head;
			foreach (char aChar in chars)
			{
				char curchar = aChar;
				TeXHyphenator.Node next = (TeXHyphenator.Node)cur.children[curchar];
				if (next == null)
				{
					next = new TeXHyphenator.Node();
					cur.children[curchar] = next;
				}
				cur = next;
			}
			System.Diagnostics.Debug.Assert((cur.pattern == null));
			cur.pattern = pattern;
		}

		private IList GetMatchingPatterns(char[] chars, int startingIdx)
		{
			TeXHyphenator.Node cur = head;
			ArrayList matchingPatterns = new ArrayList();
			if (cur.pattern != null)
			{
				matchingPatterns.Add(cur.pattern);
			}
			for (int c = startingIdx; cur != null && c < chars.Length; ++c)
			{
				char curchar = chars[c];
				TeXHyphenator.Node next = (TeXHyphenator.Node)cur.children[curchar];
				cur = next;
				if (cur != null && cur.pattern != null)
				{
					matchingPatterns.Add(cur.pattern);
				}
			}
			return matchingPatterns;
		}

		private void LabelWordBreakPoints(char[] phrase, int start, int end, bool[] breakPoints)
		{
			char[] word = new char[end - start + 2];
			System.Array.Copy(phrase, start, word, 1, end - start);
			word[0] = '.';
			word[word.Length - 1] = '.';
			// breakScore[i] is the score for breaking before word[i]
			int[] breakScore = new int[word.Length + 1];
			for (int c = 0; c < word.Length; ++c)
			{
				IList patterns = GetMatchingPatterns(word, c);
				IEnumerator iter = patterns.GetEnumerator();
				while (iter.MoveNext())
				{
					int[] pattern = (int[])iter.Current;
					for (int i = 0; i < pattern.Length; ++i)
					{
						if (breakScore[c + i] < pattern[i])
						{
							breakScore[c + i] = pattern[i];
						}
					}
				}
			}
			breakPoints[start] = true;
			for (int i_1 = start + 1; i_1 < end; i_1++)
			{
				// remember that breakPoints is offset by one because we introduced
				// the leading "."
				breakPoints[i_1 - 1] |= (breakScore[i_1 - start] % 2 == 1);
			}
		}

		/// <param name="lcphrase">Some English text in lowercase.</param>
		/// <returns>
		/// An array of booleans, one per character of the input,
		/// indicating whether it would be OK to insert a hyphen before that
		/// character.
		/// </returns>
		public virtual bool[] FindBreakPoints(char[] lcphrase)
		{
			bool[] breakPoints = new bool[lcphrase.Length];
			bool inWord = false;
			int wordStart = 0;
			int c = 0;
			for (; c < lcphrase.Length; ++c)
			{
				if (!inWord && char.IsLetter(lcphrase[c]))
				{
					wordStart = c;
					inWord = true;
				}
				else
				{
					if (inWord && !char.IsLetter(lcphrase[c]))
					{
						inWord = false;
						LabelWordBreakPoints(lcphrase, wordStart, c, breakPoints);
					}
				}
			}
			if (inWord)
			{
				LabelWordBreakPoints(lcphrase, wordStart, c, breakPoints);
			}
			return breakPoints;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			TeXHyphenator hyphenator = new TeXHyphenator();
			hyphenator.LoadDefault();
			foreach (string arg in args)
			{
				char[] chars = arg.ToLower().ToCharArray();
				bool[] breakPoints = hyphenator.FindBreakPoints(chars);
				System.Console.Out.WriteLine(arg);
				StringBuilder sb = new StringBuilder();
				foreach (bool breakPoint in breakPoints)
				{
					if (breakPoint)
					{
						sb.Append("^");
					}
					else
					{
						sb.Append("-");
					}
				}
				System.Console.Out.WriteLine(sb.ToString());
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.Tregex
{
	/// <summary>This defines how to use macros from a file in Tregex.</summary>
	/// <remarks>
	/// This defines how to use macros from a file in Tregex.  Macro files
	/// are expected to be lines of macros, one per line, with the original
	/// and the replacement separated by tabs.  Blank lines and lines
	/// starting with # are ignored.
	/// </remarks>
	/// <author>John Bauer</author>
	public class Macros
	{
		private Macros()
		{
		}

		// static methods only
		public static IList<Pair<string, string>> ReadMacros(string filename)
		{
			return ReadMacros(filename, "utf-8");
		}

		public static IList<Pair<string, string>> ReadMacros(string filename, string encoding)
		{
			try
			{
				BufferedReader bin = new BufferedReader(new InputStreamReader(new FileInputStream(filename), encoding));
				return ReadMacros(bin);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static IList<Pair<string, string>> ReadMacros(BufferedReader bin)
		{
			try
			{
				IList<Pair<string, string>> macros = new List<Pair<string, string>>();
				string line;
				int lineNumber = 0;
				while ((line = bin.ReadLine()) != null)
				{
					++lineNumber;
					string trimmed = line.Trim();
					if (trimmed.Equals(string.Empty) || trimmed[0] == '#')
					{
						continue;
					}
					string[] pieces = line.Split("\t", 2);
					if (pieces.Length < 2)
					{
						throw new ArgumentException("Expected lines of the format " + "original (tab) replacement.  " + "Line number " + lineNumber + " does not match.");
					}
					macros.Add(new Pair<string, string>(pieces[0], pieces[1]));
				}
				return macros;
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public static void AddAllMacros(TregexPatternCompiler compiler, string filename, string encoding)
		{
			if (filename == null || filename.Equals(string.Empty))
			{
				return;
			}
			foreach (Pair<string, string> macro in ReadMacros(filename, encoding))
			{
				compiler.AddMacro(macro.First(), macro.Second());
			}
		}

		public static void AddAllMacros(TregexPatternCompiler compiler, BufferedReader br)
		{
			foreach (Pair<string, string> macro in ReadMacros(br))
			{
				compiler.AddMacro(macro.First(), macro.Second());
			}
		}
	}
}

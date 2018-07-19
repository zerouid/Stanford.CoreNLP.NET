using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <summary>Parses a batch of SemgrexPatterns from a stream.</summary>
	/// <remarks>
	/// Parses a batch of SemgrexPatterns from a stream.
	/// Each SemgrexPattern must be defined in a single line.
	/// This includes a preprocessor that supports macros, defined as: "macro NAME = VALUE" and used as ${NAME}
	/// For example:
	/// # lines starting with the pound sign are skipped
	/// macro JOB = president|ceo|star
	/// {}=entity &gt;appos ({lemma:/${JOB}/} &gt;nn {ner:ORGANIZATION}=slot)
	/// </remarks>
	public class SemgrexBatchParser
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Semgraph.Semgrex.SemgrexBatchParser));

		/// <summary>Maximum stream size in characters</summary>
		private const int MaxStreamSize = 1024 * 1024;

		public static bool Verbose = false;

		private SemgrexBatchParser()
		{
		}

		// static methods class
		/// <exception cref="System.IO.IOException"/>
		public static IList<SemgrexPattern> CompileStream(InputStream @is)
		{
			return CompileStream(@is, null);
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<SemgrexPattern> CompileStream(InputStream @is, Env env)
		{
			BufferedReader reader = new BufferedReader(new InputStreamReader(@is));
			reader.Mark(MaxStreamSize);
			IDictionary<string, string> macros = Preprocess(reader);
			reader.Reset();
			return Parse(reader, macros, env);
		}

		/// <exception cref="System.IO.IOException"/>
		private static IList<SemgrexPattern> Parse(BufferedReader reader, IDictionary<string, string> macros, Env env)
		{
			IList<SemgrexPattern> patterns = new List<SemgrexPattern>();
			for (string line; (line = reader.ReadLine()) != null; )
			{
				line = line.Trim();
				if (line.IsEmpty() || line.StartsWith("#"))
				{
					continue;
				}
				if (line.StartsWith("macro "))
				{
					continue;
				}
				line = ReplaceMacros(line, macros);
				SemgrexPattern pattern = SemgrexPattern.Compile(line, env);
				patterns.Add(pattern);
			}
			return patterns;
		}

		private static readonly Pattern MacroNamePattern = Pattern.Compile("\\$\\{[a-z0-9]+\\}", Pattern.CaseInsensitive);

		private static string ReplaceMacros(string line, IDictionary<string, string> macros)
		{
			StringBuilder @out = new StringBuilder();
			Matcher matcher = MacroNamePattern.Matcher(line);
			int offset = 0;
			while (matcher.Find(offset))
			{
				int start = matcher.Start();
				int end = matcher.End();
				string name = Sharpen.Runtime.Substring(line, start + 2, end - 1);
				string value = macros[name];
				if (value == null)
				{
					throw new Exception("ERROR: Unknown macro \"" + name + "\"!");
				}
				if (start > offset)
				{
					@out.Append(Sharpen.Runtime.Substring(line, offset, start));
				}
				@out.Append(value);
				offset = end;
			}
			if (offset < line.Length)
			{
				@out.Append(Sharpen.Runtime.Substring(line, offset));
			}
			string postProcessed = @out.ToString();
			if (!postProcessed.Equals(line) && Verbose)
			{
				log.Info("Line \"" + line + "\" changed to \"" + postProcessed + '"');
			}
			return postProcessed;
		}

		/// <exception cref="System.IO.IOException"/>
		private static IDictionary<string, string> Preprocess(BufferedReader reader)
		{
			IDictionary<string, string> macros = Generics.NewHashMap();
			for (string line; (line = reader.ReadLine()) != null; )
			{
				line = line.Trim();
				if (line.StartsWith("macro "))
				{
					Pair<string, string> macro = ExtractMacro(line);
					macros[macro.First()] = macro.Second();
				}
			}
			return macros;
		}

		private static Pair<string, string> ExtractMacro(string line)
		{
			System.Diagnostics.Debug.Assert((line.StartsWith("macro")));
			int equalPosition = line.IndexOf('=');
			if (equalPosition < 0)
			{
				throw new Exception("ERROR: Invalid syntax in macro line: \"" + line + "\"!");
			}
			string name = Sharpen.Runtime.Substring(line, 5, equalPosition).Trim();
			if (name.IsEmpty())
			{
				throw new Exception("ERROR: Invalid syntax in macro line: \"" + line + "\"!");
			}
			string value = Sharpen.Runtime.Substring(line, equalPosition + 1).Trim();
			if (value.IsEmpty())
			{
				throw new Exception("ERROR: Invalid syntax in macro line: \"" + line + "\"!");
			}
			return new Pair<string, string>(name, value);
		}
	}
}

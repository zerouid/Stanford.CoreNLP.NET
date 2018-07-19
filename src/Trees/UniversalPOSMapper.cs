using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Helper class to perform a context-sensitive mapping of POS
	/// tags in a tree to universal POS tags.
	/// </summary>
	/// <author>Sebastian Schuster</author>
	public class UniversalPOSMapper
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.UniversalPOSMapper));

		public const string DefaultTsurgeonFile = "edu/stanford/nlp/models/upos/ENUniversalPOS.tsurgeon";

		private static bool loaded;

		private static IList<Pair<TregexPattern, TsurgeonPattern>> operations;

		private UniversalPOSMapper()
		{
		}

		// = false;
		// = null;
		// static methods
		public static void Load()
		{
			Load(DefaultTsurgeonFile);
		}

		public static void Load(string filename)
		{
			try
			{
				operations = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.GetOperationsFromFile(filename, "UTF-8", new TregexPatternCompiler());
			}
			catch (IOException)
			{
				log.Error(string.Format("%s: Warning - could not load Tsurgeon file from %s.%n", typeof(Edu.Stanford.Nlp.Trees.UniversalPOSMapper).GetSimpleName(), filename));
			}
			loaded = true;
		}

		public static Tree MapTree(Tree t)
		{
			if (!loaded)
			{
				Load();
			}
			if (operations == null)
			{
				return t;
			}
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(operations, t.DeepCopy());
		}
	}
}

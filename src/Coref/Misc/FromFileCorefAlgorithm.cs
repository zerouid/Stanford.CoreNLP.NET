using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.Misc
{
	/// <summary>Class for loading coreference links from a file and then performing them on CoNLL data.</summary>
	/// <remarks>
	/// Class for loading coreference links from a file and then performing them on CoNLL data.
	/// Each line of the file should contain a document id followed by a tab followed by a
	/// space-separated list of pairs of mention ids, separated by commas, to be merged
	/// (e.g., 0\t2,3 2,5 4,9).
	/// </remarks>
	/// <author>Kevin Clark</author>
	public class FromFileCorefAlgorithm : ICorefAlgorithm
	{
		private readonly IDictionary<int, IList<Pair<int, int>>> toMerge = new Dictionary<int, IList<Pair<int, int>>>();

		private int currentDocId = 0;

		public FromFileCorefAlgorithm(string savedLinkPath)
		{
			try
			{
				using (BufferedReader br = new BufferedReader(new FileReader(savedLinkPath)))
				{
					br.Lines().ForEach(null);
				}
			}
			catch (IOException e)
			{
				throw new Exception("Error reading saved links", e);
			}
		}

		public virtual void RunCoref(Document document)
		{
			if (toMerge.Contains(currentDocId))
			{
				foreach (Pair<int, int> pair in toMerge[currentDocId])
				{
					CorefUtils.MergeCoreferenceClusters(pair, document);
				}
			}
			currentDocId += 1;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(new string[] { "-props", args[0] });
			new CorefSystem(new DocumentMaker(props, new Dictionaries(props)), new Edu.Stanford.Nlp.Coref.Misc.FromFileCorefAlgorithm(args[1]), true, false).RunOnConll(props);
		}
	}
}

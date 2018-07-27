using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// This utility looks for a given sentence in a file or directory of
	/// tree files.
	/// </summary>
	/// <remarks>
	/// This utility looks for a given sentence in a file or directory of
	/// tree files.  Options that can be specified are a tag separator used
	/// on the sentence, the encoding of the file, and a regex to limit the
	/// files looked for in subdirectorys.  For example, if you specify
	/// -fileRegex ".*parse", then only filenames that end in "parse" will
	/// be considered.
	/// <br />
	/// The first non-option argument given will be the sentence searched
	/// for.  The other arguments are paths in which to look for the
	/// sentence.
	/// </remarks>
	/// <author>John Bauer</author>
	public class FindTreebankTree
	{
		public static void Main(string[] args)
		{
			// Args specified with -tagSeparator, -encoding, etc are assigned
			// to the appropriate option.  Otherwise, the first arg found is
			// the sentence to look for, and all other args are paths in which
			// to look for that sentence.
			string needle = string.Empty;
			string tagSeparator = "_";
			string encoding = "utf-8";
			string fileRegex = string.Empty;
			IList<string> paths = new List<string>();
			for (int i = 0; i < args.Length; ++i)
			{
				if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tagSeparator") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--tagSeparator")) && i + 1 < args.Length)
				{
					tagSeparator = args[i + 1];
					++i;
				}
				else
				{
					if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-encoding") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--encoding")) && i + 1 < args.Length)
					{
						encoding = args[i + 1];
						++i;
					}
					else
					{
						if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-fileRegex") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--fileRegex")) && i + 1 < args.Length)
						{
							fileRegex = args[i + 1];
							++i;
						}
						else
						{
							if (needle.Equals(string.Empty))
							{
								needle = args[i].Trim();
							}
							else
							{
								paths.Add(args[i]);
							}
						}
					}
				}
			}
			ITreeReaderFactory trf = new LabeledScoredTreeReaderFactory();
			// If the user specified a regex, here we make a filter using that
			// regex.  We just use an anonymous class for the filter
			IFileFilter filter = null;
			if (!fileRegex.Equals(string.Empty))
			{
				Pattern filePattern = Pattern.Compile(fileRegex);
				filter = null;
			}
			foreach (string path in paths)
			{
				// Start a new treebank with the given path, encoding, filter, etc
				DiskTreebank treebank = new DiskTreebank(trf, encoding);
				treebank.LoadPath(path, filter);
				IEnumerator<Tree> treeIterator = treebank.GetEnumerator();
				int treeCount = 0;
				string currentFile = string.Empty;
				while (treeIterator.MoveNext())
				{
					// the treebank might be a directory, not a single file, so
					// keep track of which file we are currently looking at
					if (!currentFile.Equals(treebank.GetCurrentFilename()))
					{
						currentFile = treebank.GetCurrentFilename();
						treeCount = 0;
					}
					++treeCount;
					Tree tree = treeIterator.Current;
					IList<TaggedWord> sentence = tree.TaggedYield();
					bool found = false;
					// The tree can match in one of three ways: tagged, untagged,
					// or untagged and unsegmented (which is useful for Chinese,
					// for example)
					string haystack = SentenceUtils.ListToString(sentence, true);
					found = needle.Equals(haystack);
					haystack = haystack.ReplaceAll(" ", string.Empty);
					found = found || needle.Equals(haystack);
					haystack = SentenceUtils.ListToString(sentence, false, tagSeparator);
					found = found || needle.Equals(haystack);
					if (found)
					{
						System.Console.Out.WriteLine("needle found in " + currentFile + " tree " + treeCount);
					}
				}
			}
		}
	}
}

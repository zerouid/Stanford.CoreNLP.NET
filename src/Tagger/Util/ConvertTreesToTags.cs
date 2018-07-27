using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.IO;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Tagger.Util
{
	/// <summary>
	/// A short utility program that dumps out trees from multiple files
	/// into one file of tagged text.
	/// </summary>
	/// <remarks>
	/// A short utility program that dumps out trees from multiple files
	/// into one file of tagged text.  Useful for combining many parse tree
	/// training files into one tagger training file, since the tagger
	/// doesn't have convenient ways of reading in an entire directory.
	/// <p>
	/// There are a few command line arguments available:
	/// <table>
	/// <tr>
	/// <td> -output &lt;filename&gt; </td>
	/// <td> File to output the data to </td>
	/// </tr>
	/// <tr>
	/// <td> -tagSeparator &lt;separator&gt; </td>
	/// <td> Separator to use between word and tag </td>
	/// </tr>
	/// <tr>
	/// <td> -treeRange &lt;range&gt; </td>
	/// <td> If tree files have numbers, they will be filtered out if not
	/// in this range.  Can be null. </td>
	/// </tr>
	/// <tr>
	/// <td> -inputEncoding &lt;encoding&gt; </td>
	/// <td> Encoding to use when reading tree files </td>
	/// </tr>
	/// <tr>
	/// <td> -outputEncoding &lt;encoding&gt; </td>
	/// <td> Encoding to use when writing tags </td>
	/// </tr>
	/// <tr>
	/// <td> -treeFilter &lt;classname&gt; </td>
	/// <td> A Filter&lt;Tree&gt; to load by reflection which eliminates
	/// trees from the data read </td>
	/// </tr>
	/// <tr>
	/// <td> -noTags </td>
	/// <td> If present, will only output the words, no tags at all
	/// </tr>
	/// <tr>
	/// <td> -noSpaces </td>
	/// <td> If present, words will be concatenated together </td>
	/// </tr>
	/// </table>
	/// All other arguments will be treated as filenames to read.
	/// </remarks>
	/// <author>John Bauer</author>
	public class ConvertTreesToTags
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Util.ConvertTreesToTags));

		private ConvertTreesToTags()
		{
		}

		// main method only
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			string outputFilename = string.Empty;
			string tagSeparator = string.Empty;
			string treeRange = string.Empty;
			string inputEncoding = "UTF-8";
			string outputEncoding = "UTF-8";
			string treeFilter = string.Empty;
			bool noTags = false;
			bool noSpaces = false;
			IList<string> inputFilenames = new List<string>();
			for (int i = 0; i < args.Length; ++i)
			{
				if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-output") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--output")) && (i + 1 < args.Length))
				{
					outputFilename = args[i + 1];
					i++;
				}
				else
				{
					if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tagSeparator") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--tagSeparator")) && (i + 1 < args.Length))
					{
						tagSeparator = args[i + 1];
						i++;
					}
					else
					{
						if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-treeRange") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--treeRange")) && (i + 1 < args.Length))
						{
							treeRange = args[i + 1];
							i++;
						}
						else
						{
							if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-inputEncoding") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--inputEncoding")) && (i + 1 < args.Length))
							{
								inputEncoding = args[i + 1];
								i++;
							}
							else
							{
								if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-outputEncoding") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--outputEncoding")) && (i + 1 < args.Length))
								{
									outputEncoding = args[i + 1];
									i++;
								}
								else
								{
									if ((Sharpen.Runtime.EqualsIgnoreCase(args[i], "-treeFilter") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--treeFilter")) && (i + 1 < args.Length))
									{
										treeFilter = args[i + 1];
										i++;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noTags") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--noTags"))
										{
											noTags = true;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noSpaces") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "--noSpaces"))
											{
												noSpaces = true;
											}
											else
											{
												inputFilenames.Add(args[i]);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			if (outputFilename.Equals(string.Empty))
			{
				log.Info("Must specify an output filename, -output");
				System.Environment.Exit(2);
			}
			if (inputFilenames.Count == 0)
			{
				log.Info("Must specify one or more input filenames");
				System.Environment.Exit(2);
			}
			FileOutputStream fos = new FileOutputStream(outputFilename);
			OutputStreamWriter osw = new OutputStreamWriter(fos, outputEncoding);
			BufferedWriter bout = new BufferedWriter(osw);
			Properties props = new Properties();
			foreach (string filename in inputFilenames)
			{
				string description = TaggedFileRecord.Format + "=" + TaggedFileRecord.Format.Trees + "," + filename;
				if (!treeRange.IsEmpty())
				{
					description = TaggedFileRecord.TreeRange + "=" + treeRange + "," + description;
				}
				if (!treeFilter.IsEmpty())
				{
					description = TaggedFileRecord.TreeFilter + "=" + treeFilter + "," + description;
				}
				description = TaggedFileRecord.Encoding + "=" + inputEncoding + "," + description;
				TaggedFileRecord record = TaggedFileRecord.CreateRecord(props, description);
				foreach (IList<TaggedWord> sentence in record.Reader())
				{
					string output = SentenceUtils.ListToString(sentence, noTags, tagSeparator);
					if (noSpaces)
					{
						output = output.ReplaceAll(" ", string.Empty);
					}
					bout.Write(output);
					bout.NewLine();
				}
			}
			bout.Flush();
			bout.Close();
			osw.Close();
			fos.Close();
		}
	}
}

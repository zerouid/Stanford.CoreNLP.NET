using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>Check if a bigram exists in bakeoff corpora.</summary>
	/// <remarks>
	/// Check if a bigram exists in bakeoff corpora.
	/// The dictionaries that this class reads have to be in UTF-8.
	/// </remarks>
	/// <author>Huihsin Tseng</author>
	/// <author>Pichuan Chang</author>
	public class CorpusDictionary
	{
		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Wordseg.CorpusDictionary));

		private ICollection<string> oneWord;

		/// <summary>Load a dictionary of words.</summary>
		/// <param name="filename">A file of words, one per line. It must be in UTF-8.</param>
		public CorpusDictionary(string filename)
			: this(filename, false)
		{
		}

		public CorpusDictionary(string filename, bool normalize)
		{
			// = null;
			if (oneWord == null)
			{
				oneWord = ReadDict(filename, normalize);
			}
		}

		public virtual ICollection<string> GetTable()
		{
			return oneWord;
		}

		private static ICollection<string> ReadDict(string filename, bool normalize)
		{
			ICollection<string> word = Generics.NewHashSet();
			logger.Info("Loading " + (normalize ? "normalized" : "unnormalized") + " dictionary from " + filename);
			try
			{
				using (InputStream @is = IOUtils.GetInputStreamFromURLOrClasspathOrFileSystem(filename))
				{
					BufferedReader wordDetectorReader = new BufferedReader(new InputStreamReader(@is, "UTF-8"));
					int i = 0;
					for (string wordDetectorLine; (wordDetectorLine = wordDetectorReader.ReadLine()) != null; )
					{
						i++;
						//String[] fields = wordDetectorLine.split("	");
						//logger.debug("DEBUG: "+filename+" "+wordDetectorLine);
						int origLeng = wordDetectorLine.Length;
						wordDetectorLine = wordDetectorLine.Trim();
						int newLeng = wordDetectorLine.Length;
						if (newLeng != origLeng)
						{
							EncodingPrintWriter.Err.Println("Line " + i + " of " + filename + " has leading/trailing whitespace: |" + wordDetectorLine + "|", "UTF-8");
						}
						if (newLeng == 0)
						{
							EncodingPrintWriter.Err.Println("Line " + i + " of " + filename + " is empty", "UTF-8");
						}
						else
						{
							if (normalize)
							{
								wordDetectorLine = ChineseUtils.Normalize(wordDetectorLine, ChineseUtils.Ascii, ChineseUtils.Ascii, ChineseUtils.Normalize);
							}
							word.Add(wordDetectorLine);
						}
					}
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			return word;
		}

		public virtual bool Contains(string word)
		{
			return GetTable().Contains(word);
		}

		public virtual string GetW(string a1)
		{
			if (Contains(a1))
			{
				return "1";
			}
			return "0";
		}
	}
}

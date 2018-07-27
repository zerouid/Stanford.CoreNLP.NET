using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Wordseg.Demo
{
	/// <summary>
	/// This is a very simple demo of calling the Chinese Word Segmenter
	/// programmatically.
	/// </summary>
	/// <remarks>
	/// This is a very simple demo of calling the Chinese Word Segmenter
	/// programmatically.  It assumes an input file in UTF8.
	/// <p/>
	/// <code>
	/// Usage: java -mx1g -cp seg.jar SegDemo fileName
	/// </code>
	/// This will run correctly in the distribution home directory.  To
	/// run in general, the properties for where to find dictionaries or
	/// normalizations have to be set.
	/// </remarks>
	/// <author>Christopher Manning</author>
	public class SegDemo
	{
		private static readonly string basedir = Runtime.GetProperty("SegDemo", "data");

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Runtime.SetOut(new TextWriter(System.Console.Out, true, "utf-8"));
			Properties props = new Properties();
			props.SetProperty("sighanCorporaDict", basedir);
			// props.setProperty("NormalizationTable", "data/norm.simp.utf8");
			// props.setProperty("normTableEncoding", "UTF-8");
			// below is needed because CTBSegDocumentIteratorFactory accesses it
			props.SetProperty("serDictionary", basedir + "/dict-chris6.ser.gz");
			if (args.Length > 0)
			{
				props.SetProperty("testFile", args[0]);
			}
			props.SetProperty("inputEncoding", "UTF-8");
			props.SetProperty("sighanPostProcessing", "true");
			CRFClassifier<CoreLabel> segmenter = new CRFClassifier<CoreLabel>(props);
			segmenter.LoadClassifierNoExceptions(basedir + "/ctb.gz", props);
			foreach (string filename in args)
			{
				segmenter.ClassifyAndWriteAnswers(filename);
			}
			string sample = "我住在美国。";
			IList<string> segmented = segmenter.SegmentString(sample);
			System.Console.Out.WriteLine(segmented);
		}
	}
}

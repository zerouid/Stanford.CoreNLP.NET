using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.IO;
using Edu.Stanford.Nlp.Tagger.Maxent;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.Util
{
	/// <summary>
	/// Takes a tagger data file of any format readable by the tagger and
	/// outputs a new file containing tagged sentences which are prefixes
	/// of the original data.
	/// </summary>
	/// <remarks>
	/// Takes a tagger data file of any format readable by the tagger and
	/// outputs a new file containing tagged sentences which are prefixes
	/// of the original data.  The prefixes are of random length.  If the
	/// -fullSentence parameter is true, the original sentence is output
	/// after each prefix.
	/// <br />
	/// Input is taken from the tagger file described in "input".  Output
	/// goes to stdout.
	/// </remarks>
	/// <author>John Bauer</author>
	public class MakePrefixFile
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(MakePrefixFile));

		public static void Main(string[] args)
		{
			Properties config = StringUtils.ArgsToProperties(args);
			log.Info(config);
			bool fullSentence = PropertiesUtils.GetBool(config, "fullSentence", false);
			Random random = new Random();
			string tagSeparator = config.GetProperty("tagSeparator", TaggerConfig.TagSeparator);
			TaggedFileRecord record = TaggedFileRecord.CreateRecord(config, config.GetProperty("input"));
			foreach (IList<TaggedWord> sentence in record.Reader())
			{
				int len = random.NextInt(sentence.Count) + 1;
				System.Console.Out.WriteLine(SentenceUtils.ListToString(sentence.SubList(0, len), false, tagSeparator));
				if (fullSentence)
				{
					System.Console.Out.WriteLine(SentenceUtils.ListToString(sentence, false, tagSeparator));
				}
			}
		}
	}
}

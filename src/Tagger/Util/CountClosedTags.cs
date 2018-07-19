using System.Collections.Generic;
using System.IO;
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
	/// Implements Chris's heuristic for when a closed tag class can be
	/// treated as a closed tag.
	/// </summary>
	/// <remarks>
	/// Implements Chris's heuristic for when a closed tag class can be
	/// treated as a closed tag.  You count how many different words in the
	/// class you see in the first X% of the training data, then make sure
	/// you don't see any new words in the rest of the training or test data.
	/// <br />
	/// This handles tagged training/test data in any format handled by the
	/// tagger (@see edu.stanford.nlp.tagger.maxent.MaxentTagger).  Files
	/// are specified as a comma-separated list via the flag
	/// -TRAIN_FILE_PROPERTY or -TEST_FILE_PROPERTY.  Closed tags are
	/// specified as a space separated list using the flag
	/// -CLOSED_TAGS_PROPERTY.
	/// <br />
	/// CountClosedTags then reads each training file to count how many
	/// lines are in it.  First, it reads the first
	/// -TRAINING_RATIO_PROPERTY fraction of the lines and keeps track of
	/// which words show up for each closed tag.  Next, it reads the rest
	/// of the training file and keeps track of which words show up in the
	/// rest of the data that didn't show up in the rest of the training
	/// data.  Finally, it reads all of the test files, once again tracking
	/// the words that didn't show up in the training data.
	/// <br />
	/// CountClosedTags then outputs the number of unique words that showed
	/// up in the TRAINING_RATIO_PROPERTY training data and the total
	/// number of unique words for each tag.  If the -PRINT_WORDS_PROPERTY
	/// flag is set to true, it also prints out the sets of observed words.
	/// <br />
	/// </remarks>
	/// <author>John Bauer</author>
	public class CountClosedTags
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Util.CountClosedTags));

		/// <summary>Which tags to look for</summary>
		internal ICollection<string> closedTags;

		/// <summary>Words seen in the first trainingRatio fraction of the trainFiles</summary>
		internal IDictionary<string, ICollection<string>> trainingWords = Generics.NewHashMap();

		/// <summary>Words seen in either trainFiles or testFiles</summary>
		internal IDictionary<string, ICollection<string>> allWords = Generics.NewHashMap();

		internal const double DefaultTrainingRatio = 2.0 / 3.0;

		/// <summary>How much of each training file to count for trainingWords</summary>
		internal readonly double trainingRatio;

		/// <summary>Whether or not the final output should print the words</summary>
		internal readonly bool printWords;

		/// <summary>Tag separator...</summary>
		private const string tagSeparator = "_";

		private CountClosedTags(Properties props)
		{
			// intended to be a standalone program, not a class
			string tagList = props.GetProperty(ClosedTagsProperty);
			if (tagList != null)
			{
				closedTags = new TreeSet<string>();
				string[] pieces = tagList.Split("\\s+");
				Java.Util.Collections.AddAll(closedTags, pieces);
			}
			else
			{
				closedTags = null;
			}
			if (props.Contains(TrainingRatioProperty))
			{
				trainingRatio = double.ValueOf(props.GetProperty(TrainingRatioProperty));
			}
			else
			{
				trainingRatio = DefaultTrainingRatio;
			}
			printWords = bool.ValueOf(props.GetProperty(PrintWordsProperty, "false"));
		}

		/// <summary>Count how many sentences there are in filename</summary>
		/// <exception cref="System.IO.IOException"/>
		private static int CountSentences(TaggedFileRecord file)
		{
			int count = 0;
			foreach (IList<TaggedWord> line in file.Reader())
			{
				++count;
			}
			return count;
		}

		/// <summary>
		/// Given a line, split it into tagged words and add each word to
		/// the given tagWordMap
		/// </summary>
		internal virtual void AddTaggedWords(IList<TaggedWord> line, IDictionary<string, ICollection<string>> tagWordMap)
		{
			foreach (TaggedWord taggedWord in line)
			{
				string word = taggedWord.Word();
				string tag = taggedWord.Tag();
				if (closedTags == null || closedTags.Contains(tag))
				{
					if (!tagWordMap.Contains(tag))
					{
						tagWordMap[tag] = new TreeSet<string>();
					}
					tagWordMap[tag].Add(word);
				}
			}
		}

		/// <summary>
		/// Count trainingRatio of the sentences for both trainingWords and
		/// allWords, and count the rest for just allWords
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		internal virtual void CountTrainingTags(TaggedFileRecord file)
		{
			int sentences = CountSentences(file);
			int trainSentences = (int)(sentences * trainingRatio);
			ITaggedFileReader reader = file.Reader();
			IList<TaggedWord> line;
			for (int i = 0; i < trainSentences && reader.MoveNext(); ++i)
			{
				line = reader.Current;
				AddTaggedWords(line, trainingWords);
				AddTaggedWords(line, allWords);
			}
			while (reader.MoveNext())
			{
				line = reader.Current;
				AddTaggedWords(line, allWords);
			}
		}

		/// <summary>Count all the words in the given file for just allWords</summary>
		/// <exception cref="System.IO.IOException"/>
		internal virtual void CountTestTags(TaggedFileRecord file)
		{
			foreach (IList<TaggedWord> line in file.Reader())
			{
				AddTaggedWords(line, allWords);
			}
		}

		/// <summary>Print out the results found</summary>
		internal virtual void Report()
		{
			IList<string> successfulTags = new List<string>();
			ICollection<string> tags = new TreeSet<string>();
			Sharpen.Collections.AddAll(tags, allWords.Keys);
			Sharpen.Collections.AddAll(tags, trainingWords.Keys);
			if (closedTags != null)
			{
				Sharpen.Collections.AddAll(tags, closedTags);
			}
			foreach (string tag in tags)
			{
				int numTraining = (trainingWords.Contains(tag) ? trainingWords[tag].Count : 0);
				int numTotal = (allWords.Contains(tag) ? allWords[tag].Count : 0);
				if (numTraining == numTotal && numTraining > 0)
				{
					successfulTags.Add(tag);
				}
				System.Console.Out.WriteLine(tag + " " + numTraining + " " + numTotal);
				if (printWords)
				{
					ICollection<string> trainingSet = trainingWords[tag];
					if (trainingSet == null)
					{
						trainingSet = Java.Util.Collections.EmptySet();
					}
					ICollection<string> allSet = allWords[tag];
					foreach (string word in trainingSet)
					{
						System.Console.Out.Write(" " + word);
					}
					if (trainingSet.Count < allSet.Count)
					{
						System.Console.Out.WriteLine();
						System.Console.Out.Write(" *");
						foreach (string word_1 in allWords[tag])
						{
							if (!trainingSet.Contains(word_1))
							{
								System.Console.Out.Write(" " + word_1);
							}
						}
					}
					System.Console.Out.WriteLine();
				}
			}
			System.Console.Out.WriteLine(successfulTags);
		}

		public const string TestFileProperty = "testFile";

		public const string TrainFileProperty = "trainFile";

		public const string ClosedTagsProperty = "closedTags";

		public const string TrainingRatioProperty = "trainingRatio";

		public const string PrintWordsProperty = "printWords";

		private static readonly ICollection<string> knownArgs = Generics.NewHashSet(Arrays.AsList(TestFileProperty, TrainFileProperty, ClosedTagsProperty, TrainingRatioProperty, PrintWordsProperty, TaggerConfig.EncodingProperty, TaggerConfig.TagSeparatorProperty
			));

		private static void Help(string error)
		{
			if (error != null && !error.Equals(string.Empty))
			{
				log.Info(error);
			}
			System.Environment.Exit(2);
		}

		private static void CheckArgs(Properties props)
		{
			if (!props.Contains(TrainFileProperty))
			{
				Help("No " + TrainFileProperty + " specified");
			}
			foreach (string arg in props.StringPropertyNames())
			{
				if (!knownArgs.Contains(arg))
				{
					Help("Unknown arg " + arg);
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			Runtime.SetOut(new TextWriter(System.Console.Out, true, "UTF-8"));
			Runtime.SetErr(new TextWriter(System.Console.Error, true, "UTF-8"));
			Properties config = StringUtils.ArgsToProperties(args);
			CheckArgs(config);
			Edu.Stanford.Nlp.Tagger.Util.CountClosedTags cct = new Edu.Stanford.Nlp.Tagger.Util.CountClosedTags(config);
			string trainFiles = config.GetProperty(TrainFileProperty);
			string testFiles = config.GetProperty(TestFileProperty);
			IList<TaggedFileRecord> files = TaggedFileRecord.CreateRecords(config, trainFiles);
			foreach (TaggedFileRecord file in files)
			{
				cct.CountTrainingTags(file);
			}
			if (testFiles != null)
			{
				files = TaggedFileRecord.CreateRecords(config, testFiles);
				foreach (TaggedFileRecord file_1 in files)
				{
					cct.CountTestTags(file_1);
				}
			}
			cct.Report();
		}
	}
}

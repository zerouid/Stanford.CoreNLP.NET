using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Tagger.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>
	/// Tags data and can handle either data with gold-standard tags (computing
	/// performance statistics) or unlabeled data.
	/// </summary>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class TestClassifier
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.TestClassifier));

		private readonly TaggedFileRecord fileRecord;

		private int numRight;

		private int numWrong;

		private int unknownWords;

		private int numWrongUnknown;

		private int numCorrectSentences;

		private int numSentences;

		private ConfusionMatrix<string> confusionMatrix;

		private bool writeUnknDict;

		private bool writeWords;

		private bool writeTopWords;

		private bool writeConfusionMatrix;

		internal MaxentTagger maxentTagger;

		internal TaggerConfig config;

		internal string saveRoot;

		/// <exception cref="System.IO.IOException"/>
		public TestClassifier(MaxentTagger maxentTagger)
			: this(maxentTagger, maxentTagger.config.GetFile())
		{
		}

		/// <exception cref="System.IO.IOException"/>
		public TestClassifier(MaxentTagger maxentTagger, string testFile)
		{
			// TODO: can we break this class up in some way?  Perhaps we can
			// spread some functionality into TestSentence and some into MaxentTagger
			// TODO: at the very least, it doesn't seem to make sense to make it
			// an object with state, rather than just some static methods
			// TODO: only one boolean here instead of 4?  They all use the same
			// debug status
			this.maxentTagger = maxentTagger;
			this.config = maxentTagger.config;
			SetDebug(config.GetDebug());
			fileRecord = TaggedFileRecord.CreateRecord(config, testFile);
			saveRoot = config.GetDebugPrefix();
			if (saveRoot == null || saveRoot.Equals(string.Empty))
			{
				saveRoot = fileRecord.Filename();
			}
			Test();
			if (writeConfusionMatrix)
			{
				PrintFile pf = new PrintFile(saveRoot + ".confusion");
				pf.Write(confusionMatrix.ToString());
				pf.Close();
			}
		}

		private void ProcessResults(TestSentence testS, PrintFile wordsFile, PrintFile unknDictFile, PrintFile topWordsFile, bool verboseResults)
		{
			numSentences++;
			testS.WriteTagsAndErrors(testS.finalTags, unknDictFile, verboseResults);
			if (writeUnknDict)
			{
				testS.PrintUnknown(numSentences, unknDictFile);
			}
			if (writeTopWords)
			{
				testS.PrintTop(topWordsFile);
			}
			testS.UpdateConfusionMatrix(testS.finalTags, confusionMatrix);
			numWrong = numWrong + testS.numWrong;
			numRight = numRight + testS.numRight;
			unknownWords = unknownWords + testS.numUnknown;
			numWrongUnknown = numWrongUnknown + testS.numWrongUnknown;
			if (testS.numWrong == 0)
			{
				numCorrectSentences++;
			}
			if (verboseResults)
			{
				log.Info("Sentence number: " + numSentences + "; length " + (testS.size - 1) + "; correct: " + testS.numRight + "; wrong: " + testS.numWrong + "; unknown wrong: " + testS.numWrongUnknown);
				log.Info("  Total tags correct: " + numRight + "; wrong: " + numWrong + "; unknown wrong: " + numWrongUnknown);
			}
		}

		/// <summary>Test on a file containing correct tags already.</summary>
		/// <remarks>
		/// Test on a file containing correct tags already. when init'ing from trees
		/// TODO: Add the ability to have a second transformer to transform output back; possibly combine this method
		/// with method below
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		private void Test()
		{
			numSentences = 0;
			confusionMatrix = new ConfusionMatrix<string>();
			PrintFile pf = null;
			PrintFile pf1 = null;
			PrintFile pf3 = null;
			if (writeWords)
			{
				pf = new PrintFile(saveRoot + ".words");
			}
			if (writeUnknDict)
			{
				pf1 = new PrintFile(saveRoot + ".un.dict");
			}
			if (writeTopWords)
			{
				pf3 = new PrintFile(saveRoot + ".words.top");
			}
			bool verboseResults = config.GetVerboseResults();
			if (config.GetNThreads() != 1)
			{
				MulticoreWrapper<IList<TaggedWord>, TestSentence> wrapper = new MulticoreWrapper<IList<TaggedWord>, TestSentence>(config.GetNThreads(), new TestClassifier.TestSentenceProcessor(maxentTagger));
				foreach (IList<TaggedWord> taggedSentence in fileRecord.Reader())
				{
					wrapper.Put(taggedSentence);
					while (wrapper.Peek())
					{
						ProcessResults(wrapper.Poll(), pf, pf1, pf3, verboseResults);
					}
				}
				wrapper.Join();
				while (wrapper.Peek())
				{
					ProcessResults(wrapper.Poll(), pf, pf1, pf3, verboseResults);
				}
			}
			else
			{
				foreach (IList<TaggedWord> taggedSentence in fileRecord.Reader())
				{
					TestSentence testS = new TestSentence(maxentTagger);
					testS.SetCorrectTags(taggedSentence);
					testS.TagSentence(taggedSentence, false);
					ProcessResults(testS, pf, pf1, pf3, verboseResults);
				}
			}
			if (pf != null)
			{
				pf.Close();
			}
			if (pf1 != null)
			{
				pf1.Close();
			}
			if (pf3 != null)
			{
				pf3.Close();
			}
		}

		internal virtual string ResultsString(MaxentTagger maxentTagger)
		{
			StringBuilder output = new StringBuilder();
			output.Append(string.Format("Model %s has xSize=%d, ySize=%d, and numFeatures=%d.%n", maxentTagger.config.GetModel(), maxentTagger.xSize, maxentTagger.ySize, maxentTagger.GetLambdaSolve().lambda.Length));
			output.Append(string.Format("Results on %d sentences and %d words, of which %d were unknown.%n", numSentences, numRight + numWrong, unknownWords));
			output.Append(string.Format("Total sentences right: %d (%f%%); wrong: %d (%f%%).%n", numCorrectSentences, numCorrectSentences * 100.0 / numSentences, numSentences - numCorrectSentences, (numSentences - numCorrectSentences) * 100.0 / (numSentences
				)));
			output.Append(string.Format("Total tags right: %d (%f%%); wrong: %d (%f%%).%n", numRight, numRight * 100.0 / (numRight + numWrong), numWrong, numWrong * 100.0 / (numRight + numWrong)));
			if (unknownWords > 0)
			{
				output.Append(string.Format("Unknown words right: %d (%f%%); wrong: %d (%f%%).%n", (unknownWords - numWrongUnknown), 100.0 - (numWrongUnknown * 100.0 / unknownWords), numWrongUnknown, numWrongUnknown * 100.0 / unknownWords));
			}
			return output.ToString();
		}

		internal virtual void PrintModelAndAccuracy(MaxentTagger maxentTagger)
		{
			// print the output all at once so that multiple threads don't clobber each other's output
			log.Info(ResultsString(maxentTagger));
		}

		internal virtual int GetNumWords()
		{
			return numRight + numWrong;
		}

		internal virtual void SetDebug(bool status)
		{
			writeUnknDict = status;
			writeWords = status;
			writeTopWords = status;
			writeConfusionMatrix = status;
		}

		internal class TestSentenceProcessor : IThreadsafeProcessor<IList<TaggedWord>, TestSentence>
		{
			internal MaxentTagger maxentTagger;

			public TestSentenceProcessor(MaxentTagger maxentTagger)
			{
				this.maxentTagger = maxentTagger;
			}

			public virtual TestSentence Process(IList<TaggedWord> taggedSentence)
			{
				TestSentence testS = new TestSentence(maxentTagger);
				testS.SetCorrectTags(taggedSentence);
				testS.TagSentence(taggedSentence, false);
				return testS;
			}

			public virtual IThreadsafeProcessor<IList<TaggedWord>, TestSentence> NewInstance()
			{
				// MaxentTagger is threadsafe
				return this;
			}
		}
	}
}

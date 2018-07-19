using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// Calculates phrase based precision and recall (similar to conlleval)
	/// Handles various encodings such as IO, IOB, IOE, BILOU, SBEIO, []
	/// Usage: java edu.stanford.nlp.stats.MultiClassChunkEvalStats [options] &lt; filename <br />
	/// -r - Do raw token based evaluation <br />
	/// -d delimiter - Specifies delimiter to use (instead of tab) <br />
	/// -b boundary - Boundary token (default is -X- ) <br />
	/// -t defaultTag - Default tag to use if tag is not prefixed (i.e.
	/// </summary>
	/// <remarks>
	/// Calculates phrase based precision and recall (similar to conlleval)
	/// Handles various encodings such as IO, IOB, IOE, BILOU, SBEIO, []
	/// Usage: java edu.stanford.nlp.stats.MultiClassChunkEvalStats [options] &lt; filename <br />
	/// -r - Do raw token based evaluation <br />
	/// -d delimiter - Specifies delimiter to use (instead of tab) <br />
	/// -b boundary - Boundary token (default is -X- ) <br />
	/// -t defaultTag - Default tag to use if tag is not prefixed (i.e. is not X-xxx ) <br />
	/// -ignoreProvidedTag - Discards the provided tag (i.e. if label is X-xxx, just use xxx for evaluation)
	/// </remarks>
	/// <author>Angel Chang</author>
	public class MultiClassChunkEvalStats : MultiClassPrecisionRecallExtendedStats.MultiClassStringLabelStats
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Stats.MultiClassChunkEvalStats));

		private bool inCorrect = false;

		private LabeledChunkIdentifier.LabelTagType prevCorrect = null;

		private LabeledChunkIdentifier.LabelTagType prevGuess = null;

		private LabeledChunkIdentifier chunker;

		private bool useLabel = false;

		public MultiClassChunkEvalStats(IClassifier<string, F> classifier, GeneralDataset<string, F> data, string negLabel)
			: base(classifier, data, negLabel)
		{
			chunker = new LabeledChunkIdentifier();
			chunker.SetNegLabel(negLabel);
		}

		public MultiClassChunkEvalStats(string negLabel)
			: base(negLabel)
		{
			chunker = new LabeledChunkIdentifier();
			chunker.SetNegLabel(negLabel);
		}

		public MultiClassChunkEvalStats(IIndex<string> dataLabelIndex, string negLabel)
			: base(dataLabelIndex, negLabel)
		{
			chunker = new LabeledChunkIdentifier();
			chunker.SetNegLabel(negLabel);
		}

		public virtual LabeledChunkIdentifier GetChunker()
		{
			return chunker;
		}

		public override void ClearCounts()
		{
			base.ClearCounts();
			inCorrect = false;
			prevCorrect = null;
			prevGuess = null;
		}

		protected internal override void FinalizeCounts()
		{
			MarkBoundary();
			base.FinalizeCounts();
		}

		private string GetTypeLabel(LabeledChunkIdentifier.LabelTagType tagType)
		{
			if (useLabel)
			{
				return tagType.label;
			}
			else
			{
				return tagType.type;
			}
		}

		protected internal override void MarkBoundary()
		{
			if (inCorrect)
			{
				inCorrect = false;
				correctGuesses.IncrementCount(GetTypeLabel(prevCorrect));
			}
			prevGuess = null;
			prevCorrect = null;
		}

		protected internal override void AddGuess(string guess, string trueLabel, bool addUnknownLabels)
		{
			LabeledChunkIdentifier.LabelTagType guessTagType = chunker.GetTagType(guess);
			LabeledChunkIdentifier.LabelTagType correctTagType = chunker.GetTagType(trueLabel);
			AddGuess(guessTagType, correctTagType, addUnknownLabels);
		}

		protected internal virtual void AddGuess(LabeledChunkIdentifier.LabelTagType guess, LabeledChunkIdentifier.LabelTagType correct, bool addUnknownLabels)
		{
			if (addUnknownLabels)
			{
				if (labelIndex == null)
				{
					labelIndex = new HashIndex<string>();
				}
				labelIndex.Add(GetTypeLabel(guess));
				labelIndex.Add(GetTypeLabel(correct));
			}
			if (inCorrect)
			{
				bool prevCorrectEnded = LabeledChunkIdentifier.IsEndOfChunk(prevCorrect, correct);
				bool prevGuessEnded = LabeledChunkIdentifier.IsEndOfChunk(prevGuess, guess);
				if (prevCorrectEnded && prevGuessEnded && prevGuess.TypeMatches(prevCorrect))
				{
					inCorrect = false;
					correctGuesses.IncrementCount(GetTypeLabel(prevCorrect));
				}
				else
				{
					if (prevCorrectEnded != prevGuessEnded || !guess.TypeMatches(correct))
					{
						inCorrect = false;
					}
				}
			}
			bool correctStarted = LabeledChunkIdentifier.IsStartOfChunk(prevCorrect, correct);
			bool guessStarted = LabeledChunkIdentifier.IsStartOfChunk(prevGuess, guess);
			if (correctStarted && guessStarted && guess.TypeMatches(correct))
			{
				inCorrect = true;
			}
			if (correctStarted)
			{
				foundCorrect.IncrementCount(GetTypeLabel(correct));
			}
			if (guessStarted)
			{
				foundGuessed.IncrementCount(GetTypeLabel(guess));
			}
			if (chunker.IsIgnoreProvidedTag())
			{
				if (guess.TypeMatches(correct))
				{
					tokensCorrect++;
				}
			}
			else
			{
				if (guess.label.Equals(correct.label))
				{
					tokensCorrect++;
				}
			}
			tokensCount++;
			prevGuess = guess;
			prevCorrect = correct;
		}

		// Returns string precision recall in ConllEval format
		public override string GetConllEvalString()
		{
			return GetConllEvalString(true);
		}

		public static void Main(string[] args)
		{
			StringUtils.LogInvocationString(log, args);
			Properties props = StringUtils.ArgsToProperties(args);
			string boundary = props.GetProperty("b", "-X-");
			string delimiter = props.GetProperty("d", "\t");
			string defaultPosTag = props.GetProperty("t", "I");
			bool raw = bool.ValueOf(props.GetProperty("r", "false"));
			bool ignoreProvidedTag = bool.ValueOf(props.GetProperty("ignoreProvidedTag", "false"));
			string format = props.GetProperty("format", "conll");
			string filename = props.GetProperty("i");
			string backgroundLabel = props.GetProperty("k", "O");
			try
			{
				MultiClassPrecisionRecallExtendedStats stats;
				if (raw)
				{
					stats = new MultiClassPrecisionRecallExtendedStats.MultiClassStringLabelStats(backgroundLabel);
				}
				else
				{
					Edu.Stanford.Nlp.Stats.MultiClassChunkEvalStats mstats = new Edu.Stanford.Nlp.Stats.MultiClassChunkEvalStats(backgroundLabel);
					mstats.GetChunker().SetDefaultPosTag(defaultPosTag);
					mstats.GetChunker().SetIgnoreProvidedTag(ignoreProvidedTag);
					stats = mstats;
				}
				if (filename != null)
				{
					stats.Score(filename, delimiter, boundary);
				}
				else
				{
					stats.Score(new BufferedReader(new InputStreamReader(Runtime.@in)), delimiter, boundary);
				}
				if (Sharpen.Runtime.EqualsIgnoreCase("conll", format))
				{
					System.Console.Out.WriteLine(stats.GetConllEvalString());
				}
				else
				{
					System.Console.Out.WriteLine(stats.GetDescription(6));
				}
			}
			catch (IOException ex)
			{
				log.Info("Error processing file: " + ex.ToString());
				Sharpen.Runtime.PrintStackTrace(ex, System.Console.Error);
			}
		}
	}
}

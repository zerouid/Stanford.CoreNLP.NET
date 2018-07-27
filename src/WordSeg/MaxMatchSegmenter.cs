using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Fsm;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Sequences;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>Lexicon-based segmenter.</summary>
	/// <remarks>
	/// Lexicon-based segmenter. Uses dynamic programming to find a word
	/// segmentation that satisfies the following two preferences:
	/// (1) minimize the number of out-of-vocabulary (OOV) words;
	/// (2) if there are multiple segmentations with the same number
	/// of OOV words, then select the one that minimizes the number
	/// of segments. Note that
	/// <see cref="Edu.Stanford.Nlp.Parser.Lexparser.MaxMatchSegmenter"/>
	/// contains a greedy version of this algorithm.
	/// Note that the output segmentation may need to postprocessing for the segmentation
	/// of non-Chinese characters (e.g., punctuation, foreign names).
	/// </remarks>
	/// <author>Michel Galley</author>
	[System.Serializable]
	public class MaxMatchSegmenter : IWordSegmenter
	{
		private const bool Debug = false;

		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(MaxMatchSegmenter));

		private readonly ICollection<string> words = Generics.NewHashSet();

		private int len = -1;

		private int edgesNb = 0;

		private const int maxLength = 10;

		private IList<DFSAState<Word, int>> states;

		private DFSA<Word, int> lattice = null;

		public enum MatchHeuristic
		{
			Minwords,
			Maxwords,
			Maxlen
		}

		private static readonly Pattern chineseStartChars = Pattern.Compile("^[\u4E00-\u9FFF]");

		private static readonly Pattern chineseEndChars = Pattern.Compile("[\u4E00-\u9FFF]$");

		private static readonly Pattern chineseChars = Pattern.Compile("[\u4E00-\u9FFF]");

		private static readonly Pattern excludeChars = Pattern.Compile("[0-9\uff10-\uff19" + "\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4E5D\u5341" + "\u96F6\u3007\u767E\u5343\u4E07\u4ebf\u5169\u25cb\u25ef\u3021-\u3029\u3038-\u303A" + "-#$%&'*+/@_\uff0d\uff03\uff04\uff05\uff06\uff07\uff0a\uff0b\uff0f\uff20\uff3f]"
			);

		public virtual void InitializeTraining(double numTrees)
		{
		}

		public virtual void Train(ICollection<Tree> trees)
		{
			foreach (Tree tree in trees)
			{
				Train(tree);
			}
		}

		public virtual void Train(Tree tree)
		{
			Train(tree.TaggedYield());
		}

		public virtual void Train(IList<TaggedWord> sentence)
		{
			foreach (TaggedWord word in sentence)
			{
				if (word.Word().Length <= maxLength)
				{
					AddStringToLexicon(word.Word());
				}
			}
		}

		public virtual void FinishTraining()
		{
		}

		public virtual void LoadSegmenter(string filename)
		{
			AddLexicon(filename);
		}

		public virtual IList<IHasWord> Segment(string s)
		{
			BuildSegmentationLattice(s);
			List<Word> sent = MaxMatchSegmentation();
			PrintlnErr("raw output: " + SentenceUtils.ListToString(sent));
			List<Word> postProcessedSent = PostProcessSentence(sent);
			PrintlnErr("processed output: " + SentenceUtils.ListToString(postProcessedSent));
			ChineseStringUtils.CTPPostProcessor postProcessor = new ChineseStringUtils.CTPPostProcessor();
			string postSentString = postProcessor.PostProcessingAnswer(postProcessedSent.ToString(), false);
			PrintlnErr("Sighan2005 output: " + postSentString);
			string[] postSentArray = postSentString.Split("\\s+");
			List<Word> postSent = new List<Word>();
			foreach (string w in postSentArray)
			{
				postSent.Add(new Word(w));
			}
			return new List<IHasWord>(postSent);
		}

		/// <summary>Add a word to the lexicon, unless it contains some non-Chinese character.</summary>
		private void AddStringToLexicon(string str)
		{
			if (str.Equals(string.Empty))
			{
				logger.Warn("WARNING: blank line in lexicon");
			}
			else
			{
				if (str.Contains(" "))
				{
					logger.Warn("WARNING: word with space in lexicon");
				}
				else
				{
					if (ExcludeChar(str))
					{
						PrintlnErr("skipping word: " + str);
						return;
					}
					// printlnErr("adding word: "+str);
					words.Add(str);
				}
			}
		}

		/// <summary>Read lexicon from a one-column text file.</summary>
		private void AddLexicon(string filename)
		{
			try
			{
				BufferedReader lexiconReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), "UTF-8"));
				string lexiconLine;
				while ((lexiconLine = lexiconReader.ReadLine()) != null)
				{
					AddStringToLexicon(lexiconLine);
				}
			}
			catch (FileNotFoundException)
			{
				logger.Error("Lexicon not found: " + filename);
				System.Environment.Exit(-1);
			}
			catch (IOException e)
			{
				logger.Error("IO error while reading: " + filename, e);
				throw new Exception(e);
			}
		}

		/// <summary>
		/// Builds a lattice of all possible segmentations using only words
		/// present in the lexicon.
		/// </summary>
		/// <remarks>
		/// Builds a lattice of all possible segmentations using only words
		/// present in the lexicon. This function must be run prior to
		/// running maxMatchSegmentation.
		/// </remarks>
		private void BuildSegmentationLattice(string s)
		{
			edgesNb = 0;
			len = s.Length;
			// Initialize word lattice:
			states = new List<DFSAState<Word, int>>();
			lattice = new DFSA<Word, int>("wordLattice");
			for (int i = 0; i <= s.Length; ++i)
			{
				states.Add(new DFSAState<Word, int>(i, lattice));
			}
			// Set start and accepting state:
			lattice.SetInitialState(states[0]);
			states[len].SetAccepting(true);
			// Find all instances of lexicon words in input string:
			for (int start = 0; start < len; ++start)
			{
				for (int end = len; end > start; --end)
				{
					string str = Sharpen.Runtime.Substring(s, start, end);
					System.Diagnostics.Debug.Assert((str.Length > 0));
					bool isOneChar = (start + 1 == end);
					bool isInDict = words.Contains(str);
					if (isInDict || isOneChar)
					{
						double cost = isInDict ? 1 : 100;
						DFSATransition<Word, int> trans = new DFSATransition<Word, int>(null, states[start], states[end], new Word(str), null, cost);
						//logger.info("start="+start+" end="+end+" word="+str);
						states[start].AddTransition(trans);
						++edgesNb;
					}
				}
			}
		}

		/// <summary>Returns the lexicon-based segmentation that minimizes the number of words.</summary>
		/// <returns>Segmented sentence.</returns>
		public virtual List<Word> MaxMatchSegmentation()
		{
			return SegmentWords(MaxMatchSegmenter.MatchHeuristic.Minwords);
		}

		/// <summary>Returns the lexicon-based segmentation following heuristic h.</summary>
		/// <remarks>
		/// Returns the lexicon-based segmentation following heuristic h.
		/// Note that buildSegmentationLattice must be run first.
		/// Two heuristics are currently available -- MINWORDS and MAXWORDS --
		/// to respectively minimize and maximize the number of segment
		/// (where each segment is a lexicon word, if possible).
		/// </remarks>
		/// <param name="h">Heuristic to use for segmentation.</param>
		/// <returns>Segmented sentence.</returns>
		/// <exception cref="System.NotSupportedException"/>
		/// <seealso cref="BuildSegmentationLattice(string)"/>
		public virtual List<Word> SegmentWords(MaxMatchSegmenter.MatchHeuristic h)
		{
			if (lattice == null || len < 0)
			{
				throw new NotSupportedException("segmentWords must be run first");
			}
			IList<Word> segmentedWords = new List<Word>();
			// Init dynamic programming:
			double[] costs = new double[len + 1];
			IList<DFSATransition<Word, int>> bptrs = new List<DFSATransition<Word, int>>();
			for (int i = 0; i < len + 1; ++i)
			{
				bptrs.Add(null);
			}
			costs[0] = 0.0;
			for (int i_1 = 1; i_1 <= len; ++i_1)
			{
				costs[i_1] = double.MaxValue;
			}
			// DP:
			for (int start = 0; start < len; ++start)
			{
				DFSAState<Word, int> fromState = states[start];
				ICollection<DFSATransition<Word, int>> trs = fromState.Transitions();
				foreach (DFSATransition<Word, int> tr in trs)
				{
					DFSAState<Word, int> toState = tr.GetTarget();
					double lcost = tr.Score();
					int end = toState.StateID();
					//logger.debug("start="+start+" end="+end+" word="+tr.getInput());
					if (h == MaxMatchSegmenter.MatchHeuristic.Minwords)
					{
						// Minimize number of words:
						if (costs[start] + 1 < costs[end])
						{
							costs[end] = costs[start] + lcost;
							bptrs.Set(end, tr);
						}
					}
					else
					{
						//logger.debug("start="+start+" end="+end+" word="+tr.getInput());
						if (h == MaxMatchSegmenter.MatchHeuristic.Maxwords)
						{
							// Maximze number of words:
							if (costs[start] + 1 < costs[end])
							{
								costs[end] = costs[start] - lcost;
								bptrs.Set(end, tr);
							}
						}
						else
						{
							throw new NotSupportedException("unimplemented heuristic");
						}
					}
				}
			}
			// Extract min-cost path:
			int i_2 = len;
			while (i_2 > 0)
			{
				DFSATransition<Word, int> tr = bptrs[i_2];
				DFSAState<Word, int> fromState = tr.GetSource();
				Word word = tr.GetInput();
				if (!word.Word().Equals(" "))
				{
					segmentedWords.Add(0, word);
				}
				i_2 = fromState.StateID();
			}
			// Print lattice density ([1,+inf[) : if equal to 1, it means
			// there is only one segmentation using words of the lexicon.
			return new List<Word>(segmentedWords);
		}

		/// <summary>Returns a lexicon-based segmentation.</summary>
		/// <remarks>
		/// Returns a lexicon-based segmentation. At each position x in the input string,
		/// it attempts to find largest value y, so that [x,y] is part of the lexicon.
		/// Then, it tried to match more input from position y+1. This greedy algorithm
		/// (taken from edu.stanford.nlp.lexparser.MaxMatchSegmenter) has no theoretical
		/// guarantee, and it would be wise to use segmentWords instead.
		/// </remarks>
		/// <param name="s">Input (unsegmented) string.</param>
		/// <returns>Segmented sentence.</returns>
		public virtual List<Word> GreedilySegmentWords(string s)
		{
			IList<Word> segmentedWords = new List<Word>();
			int length = s.Length;
			int start = 0;
			while (start < length)
			{
				int end = Math.Min(length, start + maxLength);
				while (end > start + 1)
				{
					string nextWord = Sharpen.Runtime.Substring(s, start, end);
					if (words.Contains(nextWord))
					{
						segmentedWords.Add(new Word(nextWord));
						break;
					}
					end--;
				}
				if (end == start + 1)
				{
					// character does not start any word in our dictionary
					segmentedWords.Add(new Word(new string(new char[] { s[start] })));
					start++;
				}
				else
				{
					start = end;
				}
			}
			return new List<Word>(segmentedWords);
		}

		public static void Main(string[] args)
		{
			Properties props = StringUtils.ArgsToProperties(args);
			// logger.debug(props.toString());
			SeqClassifierFlags flags = new SeqClassifierFlags(props);
			MaxMatchSegmenter seg = new MaxMatchSegmenter();
			string lexiconFile = props.GetProperty("lexicon");
			if (lexiconFile != null)
			{
				seg.AddLexicon(lexiconFile);
			}
			else
			{
				logger.Error("Error: no lexicon file!");
				System.Environment.Exit(1);
			}
			Sighan2005DocumentReaderAndWriter sighanRW = new Sighan2005DocumentReaderAndWriter();
			sighanRW.Init(flags);
			BufferedReader br = new BufferedReader(new InputStreamReader(Runtime.@in));
			PrintWriter stdoutW = new PrintWriter(System.Console.Out);
			int lineNb = 0;
			for (; ; )
			{
				++lineNb;
				logger.Info("line: " + lineNb);
				try
				{
					string line = br.ReadLine();
					if (line == null)
					{
						break;
					}
					string outputLine = null;
					if (props.GetProperty("greedy") != null)
					{
						List<Word> sentence = seg.GreedilySegmentWords(line);
						outputLine = SentenceUtils.ListToString(sentence);
					}
					else
					{
						if (props.GetProperty("maxwords") != null)
						{
							seg.BuildSegmentationLattice(line);
							outputLine = SentenceUtils.ListToString(seg.SegmentWords(MaxMatchSegmenter.MatchHeuristic.Maxwords));
						}
						else
						{
							seg.BuildSegmentationLattice(line);
							outputLine = SentenceUtils.ListToString(seg.MaxMatchSegmentation());
						}
					}
					StringReader strR = new StringReader(outputLine);
					IEnumerator<IList<CoreLabel>> itr = sighanRW.GetIterator(strR);
					while (itr.MoveNext())
					{
						sighanRW.PrintAnswers(itr.Current, stdoutW);
					}
				}
				catch (IOException)
				{
					// System.out.println(outputLine);
					break;
				}
			}
			stdoutW.Flush();
		}

		private static void PrintlnErr(string s)
		{
			EncodingPrintWriter.Err.Println(s, "UTF-8");
		}

		private static List<Word> PostProcessSentence(List<Word> sent)
		{
			List<Word> newSent = new List<Word>();
			foreach (Word word in sent)
			{
				if (newSent.Count > 0)
				{
					string prevWord = newSent[newSent.Count - 1].ToString();
					string curWord = word.ToString();
					string prevChar = Sharpen.Runtime.Substring(prevWord, prevWord.Length - 1);
					string curChar = Sharpen.Runtime.Substring(curWord, 0, 1);
					if (!IsChinese(prevChar) && !IsChinese(curChar))
					{
						Word mergedWord = new Word(prevWord + curWord);
						newSent.Set(newSent.Count - 1, mergedWord);
						//printlnErr("merged: "+mergedWord);
						//printlnErr("merged: "+mergedWord+" from: "+prevWord+" and: "+curWord);
						continue;
					}
				}
				newSent.Add(word);
			}
			return new List<Word>(newSent);
		}

		private static bool StartsWithChinese(string str)
		{
			return chineseStartChars.Matcher(str).Matches();
		}

		private static bool EndsWithChinese(string str)
		{
			return chineseEndChars.Matcher(str).Matches();
		}

		private static bool IsChinese(string str)
		{
			return chineseChars.Matcher(str).Matches();
		}

		private static bool ExcludeChar(string str)
		{
			return excludeChars.Matcher(str).Matches();
		}

		private const long serialVersionUID = 8263734344886904724L;
	}
}

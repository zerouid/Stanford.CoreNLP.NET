using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class HTKLatticeReader
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.HTKLatticeReader));

		public readonly bool Debug;

		public readonly bool Prettyprint;

		public const bool Usesum = true;

		public const bool Usemax = false;

		private readonly bool mergeType;

		public const string Silence = "<SIL>";

		private int numStates;

		private IList<HTKLatticeReader.LatticeWord> latticeWords;

		private int[] nodeTimes;

		private List<HTKLatticeReader.LatticeWord>[] wordsAtTime;

		private List<HTKLatticeReader.LatticeWord>[] wordsStartAt;

		private List<HTKLatticeReader.LatticeWord>[] wordsEndAt;

		/// <exception cref="System.Exception"/>
		private void ReadInput(BufferedReader @in)
		{
			// GET RID OF COMMENT LINES
			string line = @in.ReadLine();
			while (line.Trim().StartsWith("#"))
			{
				line = @in.ReadLine();
			}
			// READ LATTICE
			latticeWords = new List<HTKLatticeReader.LatticeWord>();
			Pattern wordLinePattern = Pattern.Compile("(\\d+)\\s+(\\d+)\\s+lm=(-?\\d+\\.\\d+),am=(-?\\d+\\.\\d+)\\s+([^( ]+)(?:\\((\\d+)\\))?.*");
			Matcher wordLineMatcher = wordLinePattern.Matcher(line);
			while (wordLineMatcher.Matches())
			{
				int startNode = System.Convert.ToInt32(wordLineMatcher.Group(1)) - 1;
				int endNode = System.Convert.ToInt32(wordLineMatcher.Group(2)) - 1;
				double lm = double.ParseDouble(wordLineMatcher.Group(3));
				double am = double.ParseDouble(wordLineMatcher.Group(4));
				string word = wordLineMatcher.Group(5).ToLower();
				string pronun = wordLineMatcher.Group(6);
				if (Sharpen.Runtime.EqualsIgnoreCase(word, "<s>"))
				{
					line = @in.ReadLine();
					wordLineMatcher = wordLinePattern.Matcher(line);
					continue;
				}
				if (Sharpen.Runtime.EqualsIgnoreCase(word, "</s>"))
				{
					word = LexiconConstants.Boundary;
				}
				int pronunciation;
				if (pronun == null)
				{
					pronunciation = 0;
				}
				else
				{
					pronunciation = System.Convert.ToInt32(pronun);
				}
				HTKLatticeReader.LatticeWord lw = new HTKLatticeReader.LatticeWord(word, startNode, endNode, lm, am, pronunciation, mergeType);
				if (Debug)
				{
					log.Info(lw);
				}
				latticeWords.Add(lw);
				line = @in.ReadLine();
				wordLineMatcher = wordLinePattern.Matcher(line);
			}
			// GET NUMBER OF NODES
			numStates = System.Convert.ToInt32(line.Trim());
			if (Debug)
			{
				log.Info(numStates);
			}
			// READ NODE TIMES
			nodeTimes = new int[numStates];
			Pattern nodeTimePattern = Pattern.Compile("(\\d+)\\s+t=(\\d+)\\s*");
			Matcher nodeTimeMatcher;
			for (int i = 0; i < numStates; i++)
			{
				nodeTimeMatcher = nodeTimePattern.Matcher(@in.ReadLine());
				if (!nodeTimeMatcher.Matches())
				{
					log.Info("Input File Error");
					System.Environment.Exit(1);
				}
				// assert ((Integer.parseInt(nodeTimeMatcher.group(1))-1) == i) ;
				nodeTimes[i] = System.Convert.ToInt32(nodeTimeMatcher.Group(2));
				if (Debug)
				{
					log.Info(i + "\tt=" + nodeTimes[i]);
				}
			}
		}

		private void MergeSimultaneousNodes()
		{
			int[] indexMap = new int[nodeTimes.Length];
			indexMap[0] = 0;
			int prevNode = 0;
			int prevTime = nodeTimes[0];
			if (Debug)
			{
				log.Info(0 + " (" + nodeTimes[0] + ")" + "-->" + 0 + " (" + nodeTimes[0] + ") ++");
			}
			for (int i = 1; i < nodeTimes.Length; i++)
			{
				if (prevTime == nodeTimes[i])
				{
					indexMap[i] = prevNode;
					if (Debug)
					{
						log.Info(i + " (" + nodeTimes[i] + ")" + "-->" + prevNode + " (" + nodeTimes[prevNode] + ") **");
					}
				}
				else
				{
					indexMap[i] = prevNode = i;
					prevTime = nodeTimes[i];
					if (Debug)
					{
						log.Info(i + " (" + nodeTimes[i] + ")" + "-->" + prevNode + " (" + nodeTimes[prevNode] + ") ++");
					}
				}
			}
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				lw.startNode = indexMap[lw.startNode];
				lw.endNode = indexMap[lw.endNode];
				if (Debug)
				{
					log.Info(lw);
				}
			}
		}

		private void RemoveEmptyNodes()
		{
			int[] indexMap = new int[numStates];
			int j = 0;
			for (int i = 0; i < numStates; i++)
			{
				indexMap[i] = j;
				if (wordsStartAt[i].Count != 0 || wordsEndAt[i].Count != 0)
				{
					j++;
				}
			}
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				wordsStartAt[lw.startNode].Remove(lw);
				wordsEndAt[lw.endNode].Remove(lw);
				for (int i_1 = lw.startNode; i_1 < lw.endNode; i_1++)
				{
					wordsAtTime[i_1].Remove(lw);
				}
				lw.startNode = indexMap[lw.startNode];
				lw.endNode = indexMap[lw.endNode];
				wordsStartAt[lw.startNode].Add(lw);
				wordsEndAt[lw.endNode].Add(lw);
				for (int i_2 = lw.startNode; i_2 < lw.endNode; i_2++)
				{
					wordsAtTime[i_2].Add(lw);
				}
			}
			numStates = j;
			List<HTKLatticeReader.LatticeWord>[] tmp = wordsAtTime;
			wordsAtTime = new ArrayList[numStates];
			System.Array.Copy(tmp, 0, wordsAtTime, 0, numStates);
			tmp = wordsStartAt;
			wordsStartAt = new ArrayList[numStates];
			System.Array.Copy(tmp, 0, wordsStartAt, 0, numStates);
			tmp = wordsEndAt;
			wordsEndAt = new ArrayList[numStates];
			System.Array.Copy(tmp, 0, wordsEndAt, 0, numStates);
		}

		private void BuildWordTimeArrays()
		{
			BuildWordsAtTime();
			BuildWordsStartAt();
			BuildWordsEndAt();
		}

		private void BuildWordsAtTime()
		{
			wordsAtTime = new ArrayList[numStates];
			for (int i = 0; i < wordsAtTime.Length; i++)
			{
				wordsAtTime[i] = new List<HTKLatticeReader.LatticeWord>();
			}
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				for (int j = lw.startNode; j <= lw.endNode; j++)
				{
					wordsAtTime[j].Add(lw);
				}
			}
		}

		private void BuildWordsStartAt()
		{
			wordsStartAt = new ArrayList[numStates];
			for (int i = 0; i < wordsStartAt.Length; i++)
			{
				wordsStartAt[i] = new List<HTKLatticeReader.LatticeWord>();
			}
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				wordsStartAt[lw.startNode].Add(lw);
			}
		}

		private void BuildWordsEndAt()
		{
			wordsEndAt = new ArrayList[numStates];
			for (int i = 0; i < wordsEndAt.Length; i++)
			{
				wordsEndAt[i] = new List<HTKLatticeReader.LatticeWord>();
			}
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				wordsEndAt[lw.endNode].Add(lw);
			}
		}

		private void RemoveRedundency()
		{
			bool changed = true;
			while (changed)
			{
				changed = false;
				foreach (List<HTKLatticeReader.LatticeWord> aWordsAtTime in wordsAtTime)
				{
					if (aWordsAtTime.Count < 2)
					{
						continue;
					}
					for (int j = 0; j < aWordsAtTime.Count - 1; j++)
					{
						HTKLatticeReader.LatticeWord w1 = aWordsAtTime[j];
						for (int k = j + 1; k < aWordsAtTime.Count; k++)
						{
							HTKLatticeReader.LatticeWord w2 = aWordsAtTime[k];
							if (Sharpen.Runtime.EqualsIgnoreCase(w1.word, w2.word))
							{
								if (RemoveRedundentPair(w1, w2))
								{
									//int numMerged = mergeDuplicates();
									//if (DEBUG) { log.info("merged " + numMerged + " identical entries."); }
									changed = true;
									//printWords();
									//j--;
									goto INNER_continue;
								}
							}
						}
INNER_continue: ;
					}
INNER_break: ;
				}
			}
		}

		//return;
		private bool RemoveRedundentPair(HTKLatticeReader.LatticeWord w1, HTKLatticeReader.LatticeWord w2)
		{
			if (Debug)
			{
				log.Info("trying to remove:");
				log.Info(w1);
				log.Info(w2);
			}
			int w1Start = w1.startNode;
			int w2Start = w2.startNode;
			int w1End = w1.endNode;
			int w2End = w2.endNode;
			// we must pick new start and end times that are legal
			int newStart;
			int oldStart;
			if (w1Start < w2Start)
			{
				newStart = w2Start;
				oldStart = w1Start;
			}
			else
			{
				newStart = w1Start;
				oldStart = w2Start;
			}
			int newEnd;
			int oldEnd;
			if (w1End < w2End)
			{
				newEnd = w1End;
				oldEnd = w2End;
			}
			else
			{
				newEnd = w2End;
				oldEnd = w1End;
			}
			// check legality (illegality not guarenteed)
			foreach (HTKLatticeReader.LatticeWord lw in wordsStartAt[oldStart])
			{
				if (lw.endNode < newStart || ((lw.endNode == newStart) && (lw.endNode != lw.startNode)))
				{
					if (Debug)
					{
						log.Info("failed");
					}
					return false;
				}
			}
			foreach (HTKLatticeReader.LatticeWord lw_1 in wordsEndAt[oldEnd])
			{
				if (lw_1.startNode > newEnd || ((lw_1.startNode == newEnd) && (lw_1.endNode != lw_1.startNode)))
				{
					if (Debug)
					{
						log.Info("failed");
					}
					return false;
				}
			}
			// change start/end times of adjacent entries
			ChangeStartTimes(wordsStartAt[oldEnd], newEnd);
			ChangeEndTimes(wordsEndAt[oldStart], newStart);
			// change start/end times of words adjacent to adjacent entries
			ChangeStartTimes(wordsStartAt[oldStart], newStart);
			ChangeEndTimes(wordsEndAt[oldEnd], newEnd);
			if (Debug)
			{
				log.Info("succeeded");
			}
			return true;
		}

		private void ChangeStartTimes(IList<HTKLatticeReader.LatticeWord> words, int newStartTime)
		{
			List<HTKLatticeReader.LatticeWord> toRemove = new List<HTKLatticeReader.LatticeWord>();
			foreach (HTKLatticeReader.LatticeWord lw in words)
			{
				latticeWords.Remove(lw);
				int oldStartTime = lw.startNode;
				lw.startNode = newStartTime;
				if (latticeWords.Contains(lw))
				{
					if (Debug)
					{
						log.Info("duplicate found");
					}
					HTKLatticeReader.LatticeWord twin = latticeWords[latticeWords.IndexOf(lw)];
					// assert (twin != lw) ;
					lw.startNode = oldStartTime;
					twin.Merge(lw);
					//wordsStartAt[lw.startNode].remove(lw);
					toRemove.Add(lw);
					wordsEndAt[lw.endNode].Remove(lw);
					for (int i = lw.startNode; i <= lw.endNode; i++)
					{
						wordsAtTime[i].Remove(lw);
					}
				}
				else
				{
					if (oldStartTime < newStartTime)
					{
						for (int i = oldStartTime; i < newStartTime; i++)
						{
							wordsAtTime[i].Remove(lw);
						}
					}
					else
					{
						for (int i = newStartTime; i < oldStartTime; i++)
						{
							wordsAtTime[i].Add(lw);
						}
					}
					latticeWords.Add(lw);
					if (oldStartTime != newStartTime)
					{
						//wordsStartAt[oldStartTime].remove(lw);
						toRemove.Add(lw);
						wordsStartAt[newStartTime].Add(lw);
					}
				}
			}
			words.RemoveAll(toRemove);
		}

		private void ChangeEndTimes(IList<HTKLatticeReader.LatticeWord> words, int newEndTime)
		{
			List<HTKLatticeReader.LatticeWord> toRemove = new List<HTKLatticeReader.LatticeWord>();
			foreach (HTKLatticeReader.LatticeWord lw in words)
			{
				latticeWords.Remove(lw);
				int oldEndTime = lw.endNode;
				lw.endNode = newEndTime;
				if (latticeWords.Contains(lw))
				{
					if (Debug)
					{
						log.Info("duplicate found");
					}
					HTKLatticeReader.LatticeWord twin = latticeWords[latticeWords.IndexOf(lw)];
					// assert (twin != lw) ;
					lw.endNode = oldEndTime;
					twin.Merge(lw);
					wordsStartAt[lw.startNode].Remove(lw);
					//wordsEndAt[lw.endNode].remove(lw);
					toRemove.Add(lw);
					for (int i = lw.startNode; i <= lw.endNode; i++)
					{
						wordsAtTime[i].Remove(lw);
					}
				}
				else
				{
					if (oldEndTime > newEndTime)
					{
						for (int i = newEndTime + 1; i <= oldEndTime; i++)
						{
							wordsAtTime[i].Remove(lw);
						}
					}
					else
					{
						for (int i = oldEndTime + 1; i <= newEndTime; i++)
						{
							wordsAtTime[i].Add(lw);
						}
					}
					latticeWords.Add(lw);
					if (oldEndTime != newEndTime)
					{
						//wordsEndAt[oldEndTime].remove(lw);
						toRemove.Add(lw);
						wordsEndAt[newEndTime].Add(lw);
					}
				}
			}
			words.RemoveAll(toRemove);
		}

		private void RemoveSilence()
		{
			List<HTKLatticeReader.LatticeWord> silences = new List<HTKLatticeReader.LatticeWord>();
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(lw.word, Silence))
				{
					silences.Add(lw);
				}
			}
			foreach (HTKLatticeReader.LatticeWord lw_1 in silences)
			{
				//if (lw.endNode == numStates) {
				ChangeEndTimes(wordsEndAt[lw_1.startNode], lw_1.endNode);
			}
			//} else {
			//changeStartTimes(wordsStartAt[lw.endNode], lw.startNode);
			//}
			silences.Clear();
			foreach (HTKLatticeReader.LatticeWord lw_2 in latticeWords)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(lw_2.word, Silence))
				{
					silences.Add(lw_2);
				}
			}
			foreach (HTKLatticeReader.LatticeWord lw_3 in silences)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(lw_3.word, Silence))
				{
					latticeWords.Remove(lw_3);
					wordsStartAt[lw_3.startNode].Remove(lw_3);
					wordsEndAt[lw_3.endNode].Remove(lw_3);
					for (int j = lw_3.startNode; j <= lw_3.endNode; j++)
					{
						wordsAtTime[j].Remove(lw_3);
					}
				}
			}
		}

		private int MergeDuplicates()
		{
			int numMerged = 0;
			for (int i = 0; i < latticeWords.Count - 1; i++)
			{
				HTKLatticeReader.LatticeWord first = latticeWords[i];
				for (int j = i + 1; j < latticeWords.Count; j++)
				{
					HTKLatticeReader.LatticeWord second = latticeWords[j];
					if (first.Equals(second))
					{
						if (Debug)
						{
							log.Info("removed duplicate");
						}
						first.Merge(second);
						latticeWords.Remove(j);
						wordsStartAt[second.startNode].Remove(second);
						wordsEndAt[second.endNode].Remove(second);
						for (int k = second.startNode; k <= second.endNode; k++)
						{
							wordsAtTime[k].Remove(second);
						}
						numMerged++;
						j--;
					}
				}
			}
			return numMerged;
		}

		public virtual void PrintWords()
		{
			latticeWords.Sort();
			System.Console.Out.WriteLine("Words: ");
			foreach (HTKLatticeReader.LatticeWord lw in latticeWords)
			{
				System.Console.Out.WriteLine(lw);
			}
		}

		private double GetProb(HTKLatticeReader.LatticeWord lw)
		{
			return lw.am * 100.0 + lw.lm;
		}

		//     private LatticeWord[][] nBest(int n) {
		//     }
		public virtual void ProcessLattice()
		{
			// log.info(1);
			BuildWordTimeArrays();
			//log.info(2);
			RemoveSilence();
			//log.info(3);
			MergeDuplicates();
			//log.info(4);
			RemoveRedundency();
			//log.info(5);
			RemoveEmptyNodes();
			//log.info(6);
			if (Prettyprint)
			{
				PrintWords();
			}
		}

		/// <exception cref="System.Exception"/>
		public HTKLatticeReader(string filename)
			: this(filename, Usesum, false, false)
		{
		}

		/// <exception cref="System.Exception"/>
		public HTKLatticeReader(string filename, bool mergeType)
			: this(filename, mergeType, false, false)
		{
		}

		/// <exception cref="System.Exception"/>
		public HTKLatticeReader(string filename, bool mergeType, bool debug, bool prettyPrint)
		{
			this.Debug = debug;
			this.Prettyprint = prettyPrint;
			this.mergeType = mergeType;
			using (BufferedReader @in = IOUtils.ReaderFromString(filename))
			{
				//log.info(-1);
				ReadInput(@in);
				//log.info(0);
				if (Prettyprint)
				{
					PrintWords();
				}
				ProcessLattice();
			}
		}

		public virtual IList<HTKLatticeReader.LatticeWord> GetLatticeWords()
		{
			return latticeWords;
		}

		public virtual int GetNumStates()
		{
			return numStates;
		}

		public virtual IList<HTKLatticeReader.LatticeWord> GetWordsOverSpan(int a, int b)
		{
			List<HTKLatticeReader.LatticeWord> words = new List<HTKLatticeReader.LatticeWord>();
			foreach (HTKLatticeReader.LatticeWord lw in wordsStartAt[a])
			{
				if (lw.endNode == b)
				{
					words.Add(lw);
				}
			}
			return words;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			bool mergeType = Usesum;
			bool prettyPrint = true;
			bool debug = false;
			string parseGram = null;
			string filename = args[0];
			for (int i = 1; i < args.Length; i++)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-debug"))
				{
					debug = true;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useMax"))
					{
						mergeType = Usemax;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-useSum"))
						{
							mergeType = Usesum;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-noPrettyPrint"))
							{
								prettyPrint = false;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-parser"))
								{
									parseGram = args[++i];
								}
								else
								{
									log.Info("unrecognized flag: " + args[i]);
									log.Info("usage: java LatticeReader <file> [ -debug ] [ -useMax ] [ -useSum ] [ -noPrettyPrint ] [ -parser parserFile ]");
									System.Environment.Exit(0);
								}
							}
						}
					}
				}
			}
			Edu.Stanford.Nlp.Parser.Lexparser.HTKLatticeReader lr = new Edu.Stanford.Nlp.Parser.Lexparser.HTKLatticeReader(filename, mergeType, debug, prettyPrint);
			if (parseGram != null)
			{
				Options op = new Options();
				// TODO: these options all get clobbered by the Options object
				// stored in the LexicalizedParser (unless it's a text file?)
				op.doDep = false;
				op.testOptions.maxLength = 80;
				op.testOptions.maxSpanForTags = 80;
				LexicalizedParser lp = LexicalizedParser.LoadModel(parseGram, op);
				// TODO: somehow merge this into ParserQuery instead of being
				// LexicalizedParserQuery specific
				LexicalizedParserQuery pq = lp.LexicalizedParserQuery();
				pq.Parse(lr);
				Tree t = pq.GetBestParse();
				t.PennPrint();
			}
		}

		public class LatticeWord : IComparable<HTKLatticeReader.LatticeWord>
		{
			public string word;

			public int startNode;

			public int endNode;

			public double lm;

			public double am;

			public int pronunciation;

			public readonly bool mergeType;

			public LatticeWord(string word, int startNode, int endNode, double lm, double am, int pronunciation, bool mergeType)
			{
				//lr.processLattice();
				this.word = word;
				this.startNode = startNode;
				this.endNode = endNode;
				this.lm = lm;
				this.am = am;
				this.pronunciation = pronunciation;
				this.mergeType = mergeType;
			}

			public virtual void Merge(HTKLatticeReader.LatticeWord lw)
			{
				if (mergeType == Usemax)
				{
					am = Math.Max(am, lw.am);
					lw.am = am;
				}
				else
				{
					if (mergeType == Usesum)
					{
						double tmp = lw.am;
						lw.am += am;
						am += tmp;
					}
				}
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(startNode).Append("\t");
				sb.Append(endNode).Append("\t");
				sb.Append("lm=").Append(lm).Append(",");
				sb.Append("am=").Append(am).Append("\t");
				sb.Append(word);
				//.append("(").append(pronunciation).append(")");
				return sb.ToString();
			}

			public override bool Equals(object o)
			{
				if (!(o is HTKLatticeReader.LatticeWord))
				{
					return false;
				}
				HTKLatticeReader.LatticeWord other = (HTKLatticeReader.LatticeWord)o;
				if (!Sharpen.Runtime.EqualsIgnoreCase(word, other.word))
				{
					return false;
				}
				if (startNode != other.startNode)
				{
					return false;
				}
				if (endNode != other.endNode)
				{
					return false;
				}
				//if (pronunciation != other.pronunciation) { return false; }
				return true;
			}

			public virtual int CompareTo(HTKLatticeReader.LatticeWord other)
			{
				if (startNode < other.startNode)
				{
					return -1;
				}
				else
				{
					if (startNode > other.startNode)
					{
						return 1;
					}
				}
				if (endNode < other.endNode)
				{
					return -1;
				}
				else
				{
					if (endNode > other.endNode)
					{
						return 1;
					}
				}
				return 0;
			}
		}
	}
}

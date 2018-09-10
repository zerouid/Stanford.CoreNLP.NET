using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Charniak
{
	/// <summary>Utility routines for printing/reading scored parses for the Charniak Parser.</summary>
	/// <author>Angel Chang</author>
	public class CharniakScoredParsesReaderWriter
	{
		private static readonly Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Charniak.CharniakScoredParsesReaderWriter));

		private static readonly Pattern wsDelimiter = Pattern.Compile("\\s+");

		private CharniakScoredParsesReaderWriter()
		{
		}

		// static methods
		/// <summary>
		/// Reads scored parses from the charniak parser
		/// File format of the scored parses:
		/// <pre>
		/// <c>
		/// &lt;# of parses&gt;\t&lt;sentenceid&gt;
		/// &lt;score&gt;
		/// &lt;parse&gt;
		/// &lt;score&gt;
		/// &lt;parse&gt;
		/// ...
		/// </c>
		/// </pre>
		/// </summary>
		/// <param name="filename">- File to read parses from</param>
		/// <returns>iterable with list of scored parse trees</returns>
		public static IEnumerable<IList<ScoredObject<Tree>>> ReadScoredTrees(string filename)
		{
			try
			{
				CharniakScoredParsesReaderWriter.ScoredParsesIterator iter = new CharniakScoredParsesReaderWriter.ScoredParsesIterator(filename);
				return new IterableIterator<IList<ScoredObject<Tree>>>(iter);
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>Reads scored parses from the charniak parser</summary>
		/// <param name="inputDesc">- Description of input used in log messages</param>
		/// <param name="br">- input reader</param>
		/// <returns>iterable with list of scored parse trees</returns>
		public static IEnumerable<IList<ScoredObject<Tree>>> ReadScoredTrees(string inputDesc, BufferedReader br)
		{
			CharniakScoredParsesReaderWriter.ScoredParsesIterator iter = new CharniakScoredParsesReaderWriter.ScoredParsesIterator(inputDesc, br);
			return new IterableIterator<IList<ScoredObject<Tree>>>(iter);
		}

		/// <summary>
		/// Convert string representing scored parses (in the charniak parser output format)
		/// to list of scored parse trees
		/// </summary>
		/// <param name="parseStr"/>
		/// <returns>list of scored parse trees</returns>
		public static IList<ScoredObject<Tree>> StringToParses(string parseStr)
		{
			try
			{
				BufferedReader br = new BufferedReader(new StringReader(parseStr));
				IEnumerable<IList<ScoredObject<Tree>>> trees = ReadScoredTrees(string.Empty, br);
				IList<ScoredObject<Tree>> res = null;
				IEnumerator<IList<ScoredObject<Tree>>> iter = trees.GetEnumerator();
				if (iter != null && iter.MoveNext())
				{
					res = iter.Current;
				}
				br.Close();
				return res;
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>
		/// Convert list of scored parse trees to string representing scored parses
		/// (in the charniak parser output format).
		/// </summary>
		/// <param name="parses">- list of scored parse trees</param>
		/// <returns>string representing scored parses</returns>
		public static string ParsesToString(IList<ScoredObject<Tree>> parses)
		{
			if (parses == null)
			{
				return null;
			}
			StringOutputStream os = new StringOutputStream();
			PrintWriter pw = new PrintWriter(os);
			PrintScoredTrees(pw, 0, parses);
			pw.Close();
			return os.ToString();
		}

		/// <summary>Print scored parse trees in format used by charniak parser</summary>
		/// <param name="trees">- trees to output</param>
		/// <param name="filename">- file to output to</param>
		public static void PrintScoredTrees(IEnumerable<IList<ScoredObject<Tree>>> trees, string filename)
		{
			try
			{
				PrintWriter pw = IOUtils.GetPrintWriter(filename);
				int i = 0;
				foreach (IList<ScoredObject<Tree>> treeList in trees)
				{
					PrintScoredTrees(pw, i, treeList);
					i++;
				}
				pw.Close();
			}
			catch (IOException ex)
			{
				throw new Exception(ex);
			}
		}

		/// <summary>Print scored parse trees for one sentence in format used by Charniak parser.</summary>
		/// <param name="pw">- printwriter</param>
		/// <param name="id">- sentence id</param>
		/// <param name="trees">- trees to output</param>
		public static void PrintScoredTrees(PrintWriter pw, int id, IList<ScoredObject<Tree>> trees)
		{
			pw.Println(trees.Count + "\t" + id);
			foreach (ScoredObject<Tree> scoredTree in trees)
			{
				pw.Println(scoredTree.Score());
				pw.Println(scoredTree.Object());
			}
		}

		private class ScoredParsesIterator : AbstractIterator<IList<ScoredObject<Tree>>>
		{
			internal string inputDesc;

			internal BufferedReader br;

			internal IList<ScoredObject<Tree>> next;

			internal Timing timing;

			internal int processed = 0;

			internal bool done = false;

			internal bool closeBufferNeeded = true;

			internal bool expectConsecutiveSentenceIds = true;

			internal int lastSentenceId = -1;

			/// <exception cref="System.IO.IOException"/>
			private ScoredParsesIterator(string filename)
				: this(filename, IOUtils.GetBufferedFileReader(filename))
			{
			}

			private ScoredParsesIterator(string inputDesc, BufferedReader br)
			{
				this.inputDesc = inputDesc;
				this.br = br;
				logger.Info("Reading cached parses from " + inputDesc);
				timing = new Timing();
				timing.Start();
				next = GetNext();
				done = next == null;
			}

			private IList<ScoredObject<Tree>> GetNext()
			{
				try
				{
					string line;
					int parsesExpected = 0;
					int sentenceId = lastSentenceId;
					ScoredObject<Tree> curParse = null;
					double score = null;
					IList<ScoredObject<Tree>> curParses = null;
					while ((line = br.ReadLine()) != null)
					{
						line = line.Trim();
						if (!line.IsEmpty())
						{
							if (parsesExpected == 0)
							{
								// Finished processing parses
								string[] fields = wsDelimiter.Split(line, 2);
								parsesExpected = System.Convert.ToInt32(fields[0]);
								sentenceId = System.Convert.ToInt32(fields[1]);
								if (expectConsecutiveSentenceIds)
								{
									if (sentenceId != lastSentenceId + 1)
									{
										if (lastSentenceId < sentenceId)
										{
											StringBuilder sb = new StringBuilder("Missing sentences");
											for (int i = lastSentenceId + 1; i < sentenceId; i++)
											{
												sb.Append(' ').Append(i);
											}
											logger.Warning(sb.ToString());
										}
										else
										{
											logger.Warning("sentenceIds are not increasing (last=" + lastSentenceId + ", curr=" + sentenceId + ")");
										}
									}
								}
								lastSentenceId = sentenceId;
								curParses = new List<ScoredObject<Tree>>(parsesExpected);
							}
							else
							{
								if (score == null)
								{
									// read score
									score = double.Parse(wsDelimiter.Split(line, 2)[0]);
								}
								else
								{
									// Reading a parse
									curParse = new ScoredObject<Tree>(Edu.Stanford.Nlp.Trees.Trees.ReadTree(line), score);
									curParses.Add(curParse);
									curParse = null;
									score = null;
									parsesExpected--;
									if (parsesExpected == 0)
									{
										return curParses;
									}
								}
							}
						}
					}
				}
				catch (IOException ex)
				{
					throw new Exception(ex);
				}
				return null;
			}

			public override bool MoveNext()
			{
				return !done;
			}

			public override IList<ScoredObject<Tree>> Current
			{
				get
				{
					if (!done)
					{
						IList<ScoredObject<Tree>> cur = next;
						next = GetNext();
						processed++;
						if (next == null)
						{
							logger.Info("Read " + processed + " trees, from " + inputDesc + " in " + timing.ToSecondsString() + " secs");
							done = true;
							if (closeBufferNeeded)
							{
								try
								{
									br.Close();
								}
								catch (IOException ex)
								{
									logger.Warn(ex);
								}
							}
						}
						return cur;
					}
					else
					{
						throw new NoSuchElementException("No more elements from " + inputDesc);
					}
				}
			}
		}
		// end static class ScoredParsesIterator
	}
}

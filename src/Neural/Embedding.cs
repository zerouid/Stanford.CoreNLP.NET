using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;

using Org.Ejml.Simple;


namespace Edu.Stanford.Nlp.Neural
{
	/// <author>Minh-Thang Luong <lmthang@stanford.edu></author>
	/// <author>John Bauer</author>
	/// <author>Richard Socher</author>
	/// <author>Kevin Clark</author>
	[System.Serializable]
	public class Embedding
	{
		private const long serialVersionUID = 4925779982530239054L;

		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Neural.Embedding));

		private IDictionary<string, SimpleMatrix> wordVectors;

		private int embeddingSize;

		internal const string StartWord = "*START*";

		internal const string EndWord = "*END*";

		internal const string UnknownWord = "*UNK*";

		internal const string UnknownNumber = "*NUM*";

		internal const string UnknownCaps = "*CAPS*";

		internal const string UnknownChineseYear = "*ZH_YEAR*";

		internal const string UnknownChineseNumber = "*ZH_NUM*";

		internal const string UnknownChinesePercent = "*ZH_PERCENT*";

		internal static readonly Pattern NumberPattern = Pattern.Compile("-?[0-9][-0-9,.:]*");

		internal static readonly Pattern CapsPattern = Pattern.Compile("[a-zA-Z]*[A-Z][a-zA-Z]*");

		internal static readonly Pattern ChineseYearPattern = Pattern.Compile("[〇零一二三四五六七八九０１２３４５６７８９]{4}+年");

		internal static readonly Pattern ChineseNumberPattern = Pattern.Compile("(?:[〇０零一二三四五六七八九０１２３４５６７８９十百万千亿]+[点多]?)+");

		internal static readonly Pattern ChinesePercentPattern = Pattern.Compile("百分之[〇０零一二三四五六七八九０１２３４５６７８９十点]+");

		/// <summary>Some word vectors are trained with DG representing number.</summary>
		/// <remarks>
		/// Some word vectors are trained with DG representing number.
		/// We mix all of those into the unknown number vectors.
		/// </remarks>
		internal static readonly Pattern DgPattern = Pattern.Compile(".*DG.*");

		public Embedding(IDictionary<string, SimpleMatrix> wordVectors)
		{
			this.wordVectors = wordVectors;
			this.embeddingSize = GetEmbeddingSize(wordVectors);
		}

		public Embedding(string wordVectorFile)
			: this(wordVectorFile, 0)
		{
		}

		public Embedding(string wordVectorFile, int embeddingSize)
		{
			this.wordVectors = Generics.NewHashMap();
			this.embeddingSize = embeddingSize;
			LoadWordVectors(wordVectorFile);
		}

		public Embedding(string wordFile, string vectorFile)
			: this(wordFile, vectorFile, 0)
		{
		}

		public Embedding(string wordFile, string vectorFile, int embeddingSize)
		{
			this.wordVectors = Generics.NewHashMap();
			this.embeddingSize = embeddingSize;
			LoadWordVectors(wordFile, vectorFile);
		}

		/// <summary>This method reads a file of raw word vectors, with a given expected size, and returns a map of word to vector.</summary>
		/// <remarks>
		/// This method reads a file of raw word vectors, with a given expected size, and returns a map of word to vector.
		/// <br />
		/// The file should be in the format <br />
		/// <c>WORD X1 X2 X3 ...</c>
		/// <br />
		/// If vectors in the file are smaller than expectedSize, an
		/// exception is thrown.  If vectors are larger, the vectors are
		/// truncated and a warning is printed.
		/// </remarks>
		private void LoadWordVectors(string wordVectorFile)
		{
			log.Info("# Loading embedding ...\n  word vector file = " + wordVectorFile);
			bool warned = false;
			int numWords = 0;
			foreach (string line in IOUtils.ReadLines(wordVectorFile, "utf-8"))
			{
				string[] lineSplit = line.Split("\\s+");
				string word = lineSplit[0];
				// check for unknown token
				if (word.Equals("UNKNOWN") || word.Equals("UUUNKKK") || word.Equals("UNK") || word.Equals("*UNKNOWN*") || word.Equals("<unk>"))
				{
					word = UnknownWord;
				}
				// check for start token
				if (word.Equals("<s>"))
				{
					word = StartWord;
				}
				// check for end token
				if (word.Equals("</s>"))
				{
					word = EndWord;
				}
				int dimOfWords = lineSplit.Length - 1;
				if (embeddingSize <= 0)
				{
					embeddingSize = dimOfWords;
					log.Info("  detected embedding size = " + dimOfWords);
				}
				// the first entry is the word itself
				// the other entries will all be entries in the word vector
				if (dimOfWords > embeddingSize)
				{
					if (!warned)
					{
						warned = true;
						log.Info("WARNING: Dimensionality of numHid parameter and word vectors do not match, deleting word vector dimensions to fit!");
					}
					dimOfWords = embeddingSize;
				}
				else
				{
					if (dimOfWords < embeddingSize)
					{
						throw new Exception("Word vectors file has dimension too small for requested numHid of " + embeddingSize);
					}
				}
				double[][] vec = new double[dimOfWords][];
				for (int i = 1; i <= dimOfWords; i++)
				{
					vec[i - 1][0] = double.ParseDouble(lineSplit[i]);
				}
				SimpleMatrix vector = new SimpleMatrix(vec);
				wordVectors[word] = vector;
				numWords++;
			}
			log.Info("  num words = " + numWords);
		}

		/// <summary>
		/// This method takes as input two files: wordFile (one word per line) and a raw word vector file
		/// with a given expected size, and returns a map of word to vector.
		/// </summary>
		/// <remarks>
		/// This method takes as input two files: wordFile (one word per line) and a raw word vector file
		/// with a given expected size, and returns a map of word to vector.
		/// <p>
		/// The word vector file should be in the format <br />
		/// <c>X1 X2 X3 ...</c>
		/// <br />
		/// If vectors in the file are smaller than expectedSize, an
		/// exception is thrown.  If vectors are larger, the vectors are
		/// truncated and a warning is printed.
		/// </remarks>
		private void LoadWordVectors(string wordFile, string vectorFile)
		{
			log.Info("# Loading embedding ...\n  word file = " + wordFile + "\n  vector file = " + vectorFile);
			bool warned = false;
			int numWords = 0;
			IEnumerator<string> wordIterator = IOUtils.ReadLines(wordFile, "utf-8").GetEnumerator();
			foreach (string line in IOUtils.ReadLines(vectorFile, "utf-8"))
			{
				string[] lineSplit = line.Split("\\s+");
				string word = wordIterator.Current;
				// check for unknown token
				// FIXME cut and paste code
				if (word.Equals("UNKNOWN") || word.Equals("UUUNKKK") || word.Equals("UNK") || word.Equals("*UNKNOWN*") || word.Equals("<unk>"))
				{
					word = UnknownWord;
				}
				// check for start token
				if (word.Equals("<s>"))
				{
					word = StartWord;
				}
				// check for end token
				if (word.Equals("</s>"))
				{
					word = EndWord;
				}
				int dimOfWords = lineSplit.Length;
				if (embeddingSize <= 0)
				{
					embeddingSize = dimOfWords;
					log.Info("  detected embedding size = " + dimOfWords);
				}
				// the first entry is the word itself
				// the other entries will all be entries in the word vector
				if (dimOfWords > embeddingSize)
				{
					if (!warned)
					{
						warned = true;
						log.Info("WARNING: Dimensionality of numHid parameter and word vectors do not match, deleting word vector dimensions to fit!");
					}
					dimOfWords = embeddingSize;
				}
				else
				{
					if (dimOfWords < embeddingSize)
					{
						throw new Exception("Word vectors file has dimension too small for requested numHid of " + embeddingSize);
					}
				}
				double[][] vec = new double[dimOfWords][];
				for (int i = 0; i < dimOfWords; i++)
				{
					vec[i][0] = double.ParseDouble(lineSplit[i]);
				}
				SimpleMatrix vector = new SimpleMatrix(vec);
				wordVectors[word] = vector;
				numWords++;
			}
			log.Info("  num words = " + numWords);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void WriteToFile(string filename)
		{
			IOUtils.WriteObjectToFile(wordVectors, filename);
		}

		/* -- Getters and Setters -- */
		public virtual int Size()
		{
			return wordVectors.Count;
		}

		public virtual ICollection<SimpleMatrix> Values()
		{
			return wordVectors.Values;
		}

		public virtual ICollection<string> KeySet()
		{
			return wordVectors.Keys;
		}

		public virtual ICollection<KeyValuePair<string, SimpleMatrix>> EntrySet()
		{
			return wordVectors;
		}

		public virtual SimpleMatrix Get(string word)
		{
			if (wordVectors.Contains(word))
			{
				return wordVectors[word];
			}
			else
			{
				return wordVectors[UnknownWord];
			}
		}

		public virtual bool ContainsWord(string word)
		{
			return wordVectors.Contains(word);
		}

		public virtual SimpleMatrix GetStartWordVector()
		{
			return wordVectors[StartWord];
		}

		public virtual SimpleMatrix GetEndWordVector()
		{
			return wordVectors[EndWord];
		}

		public virtual SimpleMatrix GetUnknownWordVector()
		{
			return wordVectors[UnknownWord];
		}

		public virtual IDictionary<string, SimpleMatrix> GetWordVectors()
		{
			return wordVectors;
		}

		public virtual int GetEmbeddingSize()
		{
			return embeddingSize;
		}

		public virtual void SetWordVectors(IDictionary<string, SimpleMatrix> wordVectors)
		{
			this.wordVectors = wordVectors;
			this.embeddingSize = GetEmbeddingSize(wordVectors);
		}

		private static int GetEmbeddingSize(IDictionary<string, SimpleMatrix> wordVectors)
		{
			if (!wordVectors.Contains(UnknownWord))
			{
				// find if there's any other unk string
				string unkStr = string.Empty;
				if (wordVectors.Contains("UNK"))
				{
					unkStr = "UNK";
				}
				if (wordVectors.Contains("UUUNKKK"))
				{
					unkStr = "UUUNKKK";
				}
				if (wordVectors.Contains("UNKNOWN"))
				{
					unkStr = "UNKNOWN";
				}
				if (wordVectors.Contains("*UNKNOWN*"))
				{
					unkStr = "*UNKNOWN*";
				}
				if (wordVectors.Contains("<unk>"))
				{
					unkStr = "<unk>";
				}
				// set UNKNOWN_WORD
				if (!unkStr.IsEmpty())
				{
					wordVectors[UnknownWord] = wordVectors[unkStr];
				}
				else
				{
					throw new Exception("! wordVectors used to initialize Embedding doesn't contain any recognized form of " + UnknownWord);
				}
			}
			return wordVectors[UnknownWord].GetNumElements();
		}
	}
}

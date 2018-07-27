using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IE.Machinereading.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class AceToken
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.AceToken));

		/// <summary>
		/// The actual token bytes
		/// Normally we work with mWord (see below), but mLiteral is needed when
		/// we need to check if a sequence of tokens exists in a gazetteer
		/// </summary>
		private string mLiteral;

		/// <summary>The index of the literal in the WORDS hash</summary>
		private int mWord;

		/// <summary>Case of mWord</summary>
		private int mCase;

		/// <summary>Suffixes of mWord</summary>
		private int[] mSuffixes;

		private int mLemma;

		private int mPos;

		private int mChunk;

		private int mNerc;

		private Span mByteOffset;

		/// <summary>Raw byte offset in the SGM doc</summary>
		private Span mRawByteOffset;

		private int mSentence;

		/// <summary>Entity class from Massi</summary>
		private string mMassiClass;

		/// <summary>Entity label from the BBN corpus</summary>
		private string mMassiBbn;

		/// <summary>WordNet super-senses detected by Massi</summary>
		private string mMassiWnss;

		/// <summary>Dictionary for all words in the corpus</summary>
		public static readonly StringDictionary Words;

		/// <summary>Dictionary for all lemmas in the corpus</summary>
		public static readonly StringDictionary Lemmas;

		/// <summary>Dictionary for all other strings in the corpus</summary>
		public static readonly StringDictionary Others;

		/// <summary>Map of all proximity classes</summary>
		public static readonly IDictionary<int, List<int>> ProxClasses;

		/// <summary>How many elements per proximity class</summary>
		private const int ProximityClassSize = 5;

		/// <summary>The location gazetteer</summary>
		private static IDictionary<string, string> LocGaz = null;

		/// <summary>The person first name dictionary</summary>
		private static IDictionary<string, string> FirstGaz = null;

		/// <summary>The person last name dictionary</summary>
		private static IDictionary<string, string> LastGaz = null;

		/// <summary>List of trigger words</summary>
		private static IDictionary<string, string> TriggerGaz = null;

		private static readonly Pattern SgmlPattern;

		static AceToken()
		{
			Words = new StringDictionary("words");
			Lemmas = new StringDictionary("lemmas");
			Others = new StringDictionary("others");
			Words.SetMode(true);
			Lemmas.SetMode(true);
			Others.SetMode(true);
			ProxClasses = Generics.NewHashMap();
			SgmlPattern = Pattern.Compile("<[^<>]+>");
		}

		/// <exception cref="Java.IO.FileNotFoundException"/>
		/// <exception cref="System.IO.IOException"/>
		public static void LoadGazetteers(string dataPath)
		{
			log.Info("Loading location gazetteer... ");
			LocGaz = Generics.NewHashMap();
			LoadDictionary(LocGaz, dataPath + File.separator + "world_small.gaz.nonambiguous");
			log.Info("done.");
			log.Info("Loading first-name gazetteer... ");
			FirstGaz = Generics.NewHashMap();
			LoadDictionary(FirstGaz, dataPath + File.separator + "per_first.gaz");
			log.Info("done.");
			log.Info("Loading last-name gazetteer... ");
			LastGaz = Generics.NewHashMap();
			LoadDictionary(LastGaz, dataPath + File.separator + "per_last.gaz");
			log.Info("done.");
			log.Info("Loading trigger-word gazetteer... ");
			TriggerGaz = Generics.NewHashMap();
			LoadDictionary(TriggerGaz, dataPath + File.separator + "triggers.gaz");
			log.Info("done.");
		}

		/// <summary>Loads one dictionary from disk</summary>
		/// <exception cref="Java.IO.FileNotFoundException"/>
		/// <exception cref="System.IO.IOException"/>
		private static void LoadDictionary(IDictionary<string, string> dict, string file)
		{
			BufferedReader @in = new BufferedReader(new FileReader(file));
			string line;
			while ((line = @in.ReadLine()) != null)
			{
				List<string> tokens = SimpleTokenize.Tokenize(line);
				if (tokens.Count > 0)
				{
					string lower = tokens[0].ToLower();
					if (tokens.Count == 1)
					{
						dict[lower] = "true";
					}
					else
					{
						dict[lower] = tokens[1];
					}
				}
			}
		}

		public static bool IsLocation(string lower)
		{
			return Exists(LocGaz, lower);
		}

		public static bool IsFirstName(string lower)
		{
			return Exists(FirstGaz, lower);
		}

		public static bool IsLastName(string lower)
		{
			return Exists(LastGaz, lower);
		}

		public static string IsTriggerWord(string lower)
		{
			return TriggerGaz[lower];
		}

		/// <summary>Verifies if the given string exists in the given dictionary</summary>
		public static bool Exists(IDictionary<string, string> dict, string elem)
		{
			if (dict[elem] != null)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Loads all proximity classes from the hard disk The WORDS map must be
		/// created before!
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public static void LoadProximityClasses(string proxFileName)
		{
			log.Info("Loading proximity classes...");
			BufferedReader @in = null;
			try
			{
				@in = new BufferedReader(new FileReader(proxFileName));
			}
			catch (IOException)
			{
				log.Info("Warning: no proximity database found.");
				return;
			}
			string line;
			while ((line = @in.ReadLine()) != null)
			{
				List<string> tokens = SimpleTokenize.Tokenize(line);
				if (tokens.Count > 0)
				{
					int key = Words.Get(tokens[0]);
					List<int> value = new List<int>();
					for (int i = 0; i < tokens.Count && i < ProximityClassSize; i++)
					{
						int word = Words.Get(tokens[i]);
						value.Add(word);
					}
					ProxClasses[key] = value;
				}
			}
			@in.Close();
			log.Info("Finished loading proximity classes.");
		}

		public virtual string GetLiteral()
		{
			return mLiteral;
		}

		public virtual int GetWord()
		{
			return mWord;
		}

		public virtual int GetCase()
		{
			return mCase;
		}

		public virtual int[] GetSuffixes()
		{
			return mSuffixes;
		}

		public virtual int GetLemma()
		{
			return mLemma;
		}

		public virtual int GetPos()
		{
			return mPos;
		}

		public virtual int GetChunk()
		{
			return mChunk;
		}

		public virtual int GetNerc()
		{
			return mNerc;
		}

		public virtual Span GetByteOffset()
		{
			return mByteOffset;
		}

		public virtual int GetByteStart()
		{
			return mByteOffset.Start();
		}

		public virtual int GetByteEnd()
		{
			return mByteOffset.End();
		}

		public virtual int GetSentence()
		{
			return mSentence;
		}

		public virtual Span GetRawByteOffset()
		{
			return mRawByteOffset;
		}

		public virtual int GetRawByteStart()
		{
			return mRawByteOffset.Start();
		}

		public virtual int GetRawByteEnd()
		{
			return mRawByteOffset.End();
		}

		public virtual void SetMassiClass(string i)
		{
			mMassiClass = i;
		}

		public virtual string GetMassiClass()
		{
			return mMassiClass;
		}

		public virtual void SetMassiBbn(string i)
		{
			mMassiBbn = i;
		}

		public virtual string GetMassiBbn()
		{
			return mMassiBbn;
		}

		public virtual void SetMassiWnss(string i)
		{
			mMassiWnss = i;
		}

		public virtual string GetMassiWnss()
		{
			return mMassiWnss;
		}

		public static bool IsSgml(string s)
		{
			Matcher match = SgmlPattern.Matcher(s);
			return match.Find(0);
		}

		public static string RemoveSpaces(string s)
		{
			if (s == null)
			{
				return s;
			}
			return s.ReplaceAll(" ", "_");
		}

		public const int CaseOther = 0;

		public const int CaseAllcaps = 1;

		public const int CaseAllcapsordots = 2;

		public const int CaseCapini = 3;

		public const int CaseIncap = 4;

		public const int CaseAlldigits = 5;

		public const int CaseAlldigitsordots = 6;

		private static int DetectCase(string word)
		{
			//
			// is the word all caps? (e.g. IBM)
			//
			bool isAllCaps = true;
			for (int i = 0; i < word.Length; i++)
			{
				if (!char.IsUpperCase(word[i]))
				{
					isAllCaps = false;
					break;
				}
			}
			if (isAllCaps)
			{
				return CaseAllcaps;
			}
			//
			// is the word all caps or dots?(e.g. I.B.M.)
			//
			bool isAllCapsOrDots = true;
			if (char.IsUpperCase(word[0]))
			{
				for (int i_1 = 0; i_1 < word.Length; i_1++)
				{
					if (!char.IsUpperCase(word[i_1]) && word[i_1] != '.')
					{
						isAllCapsOrDots = false;
						break;
					}
				}
			}
			else
			{
				isAllCapsOrDots = false;
			}
			if (isAllCapsOrDots)
			{
				return CaseAllcapsordots;
			}
			//
			// does the word start with a cap?(e.g. Tuesday)
			//
			bool isInitialCap = false;
			if (char.IsUpperCase(word[0]))
			{
				isInitialCap = true;
			}
			if (isInitialCap)
			{
				return CaseCapini;
			}
			//
			// does the word contain a capitalized letter?
			//
			bool isInCap = false;
			for (int i_2 = 1; i_2 < word.Length; i_2++)
			{
				if (char.IsUpperCase(word[i_2]))
				{
					isInCap = true;
					break;
				}
			}
			if (isInCap)
			{
				return CaseIncap;
			}
			//
			// is the word all digits? (e.g. 123)
			//
			bool isAllDigits = false;
			for (int i_3 = 0; i_3 < word.Length; i_3++)
			{
				if (!char.IsDigit(word[i_3]))
				{
					isAllDigits = false;
					break;
				}
			}
			if (isAllDigits)
			{
				return CaseAlldigits;
			}
			//
			// is the word all digits or . or ,? (e.g. 1.3)
			//
			bool isAllDigitsOrDots = true;
			if (char.IsDigit(word[0]))
			{
				for (int i_1 = 0; i_1 < word.Length; i_1++)
				{
					if (!char.IsDigit(word[i_1]) && word[i_1] != '.' && word[i_1] != ',')
					{
						isAllDigitsOrDots = false;
						break;
					}
				}
			}
			else
			{
				isAllDigitsOrDots = false;
			}
			if (isAllDigitsOrDots)
			{
				return CaseAlldigitsordots;
			}
			return CaseOther;
		}

		private static int[] ExtractSuffixes(string word)
		{
			string lower = word.ToLower();
			List<int> suffixes = new List<int>();
			for (int i = 2; i <= 4; i++)
			{
				if (lower.Length >= i)
				{
					try
					{
						string suf = Sharpen.Runtime.Substring(lower, lower.Length - i);
						suffixes.Add(Words.Get(suf));
					}
					catch (Exception)
					{
					}
				}
				else
				{
					// unknown suffix
					break;
				}
			}
			int[] sufs = new int[suffixes.Count];
			for (int i_1 = 0; i_1 < suffixes.Count; i_1++)
			{
				sufs[i_1] = suffixes[i_1];
			}
			return sufs;
		}

		/// <summary>Constructs an AceToken from a tokenized line generated by Tokey</summary>
		public AceToken(string word, string lemma, string pos, string chunk, string nerc, string start, string end, int sentence)
		{
			mLiteral = word;
			if (word == null)
			{
				mWord = -1;
				mCase = -1;
				mSuffixes = null;
			}
			else
			{
				mWord = Words.Get(RemoveSpaces(word), false);
				mCase = DetectCase(word);
				mSuffixes = ExtractSuffixes(word);
			}
			if (lemma == null)
			{
				mLemma = -1;
			}
			else
			{
				mLemma = Lemmas.Get(RemoveSpaces(lemma), false);
			}
			if (pos == null)
			{
				mPos = -1;
			}
			else
			{
				mPos = Others.Get(pos, false);
			}
			if (chunk == null)
			{
				mChunk = -1;
			}
			else
			{
				mChunk = Others.Get(chunk, false);
			}
			if (nerc == null)
			{
				mNerc = -1;
			}
			else
			{
				mNerc = Others.Get(nerc, false);
			}
			if (start != null && end != null)
			{
				mByteOffset = new Span(System.Convert.ToInt32(start), System.Convert.ToInt32(end));
				mRawByteOffset = new Span(System.Convert.ToInt32(start), System.Convert.ToInt32(end));
			}
			mSentence = sentence;
			mMassiClass = string.Empty;
			mMassiBbn = string.Empty;
			mMassiWnss = string.Empty;
		}

		/// <summary>
		/// Recomputes start/end phrase positions by removing SGML tag strings This is
		/// required because ACE annotations skip over SGML tags when computing
		/// positions in stream, hence annotations do not match with our preprocessing
		/// positions, which count everything
		/// </summary>
		public virtual int AdjustPhrasePositions(int offsetToSubtract, string word)
		{
			if (IsSgml(word))
			{
				// offsetToSubtract += word.length();
				// the token length may be different than (end - start)!
				// i.e. QUOTE_PREVIOUSPOST is cleaned in Tokey!
				offsetToSubtract += mByteOffset.End() - mByteOffset.Start();
				mByteOffset.SetStart(-1);
				mByteOffset.SetEnd(-1);
			}
			else
			{
				mByteOffset.SetStart(mByteOffset.Start() - offsetToSubtract);
				mByteOffset.SetEnd(mByteOffset.End() - offsetToSubtract);
			}
			return offsetToSubtract;
		}

		/// <summary>Pretty display</summary>
		public virtual string Display()
		{
			if (mByteOffset != null)
			{
				return "['" + Words.Get(mWord) + "', " + Others.Get(mPos) + ", " + mByteOffset.Start() + ", " + mByteOffset.End() + "]";
			}
			return "['" + Words.Get(mWord) + "', " + Others.Get(mPos) + "]";
		}

		public override string ToString()
		{
			return Display();
		}
	}
}

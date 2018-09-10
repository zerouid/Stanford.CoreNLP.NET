using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Ling.Tokensregex
{
	/// <summary>Table used to lookup multi-word phrases.</summary>
	/// <remarks>
	/// Table used to lookup multi-word phrases.
	/// This class provides functions for looking up all instances of known phrases in a document in an efficient manner.
	/// Phrases can be added to the phrase table using
	/// <ul>
	/// <li>readPhrases</li>
	/// <li>readPhrasesWithTagScores</li>
	/// <li>addPhrase</li>
	/// </ul>
	/// You can lookup phrases in the table using
	/// <ul>
	/// <li>get</li>
	/// <li>lookup</li>
	/// </ul>
	/// You can find phrases occurring in a piece of text using
	/// <ul>
	/// <li>findAllMatches</li>
	/// <li>findNonOverlappingPhrases</li>
	/// </ul>
	/// </remarks>
	/// <author>Angel Chang</author>
	[System.Serializable]
	public class PhraseTable
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Ling.Tokensregex.PhraseTable));

		private const string PhraseEnd = string.Empty;

		private const long serialVersionUID = 1L;

		internal IDictionary<string, object> rootTree;

		public bool normalize = true;

		public bool caseInsensitive = false;

		public bool ignorePunctuation = false;

		public bool ignorePunctuationTokens = true;

		public IAnnotator tokenizer;

		internal int nPhrases = 0;

		internal int nStrings = 0;

		[System.NonSerialized]
		internal CacheMap<string, string> normalizedCache = new CacheMap<string, string>(5000);

		public PhraseTable()
		{
		}

		public PhraseTable(int initSize)
		{
			// tokenizing annotator
			rootTree = new Dictionary<string, object>(initSize);
		}

		public PhraseTable(bool normalize, bool caseInsensitive, bool ignorePunctuation)
		{
			this.normalize = normalize;
			this.caseInsensitive = caseInsensitive;
			this.ignorePunctuation = ignorePunctuation;
		}

		public virtual bool IsEmpty()
		{
			return (nPhrases == 0);
		}

		public virtual bool ContainsKey(object key)
		{
			return Get(key) != null;
		}

		public virtual PhraseTable.Phrase Get(object key)
		{
			if (key is string)
			{
				return Lookup((string)key);
			}
			else
			{
				if (key is PhraseTable.IWordList)
				{
					return Lookup((PhraseTable.IWordList)key);
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>Clears this table</summary>
		public virtual void Clear()
		{
			rootTree = null;
			nPhrases = 0;
			nStrings = 0;
		}

		public virtual void SetNormalizationCacheSize(int cacheSize)
		{
			CacheMap<string, string> newNormalizedCache = new CacheMap<string, string>(cacheSize);
			newNormalizedCache.PutAll(normalizedCache);
			normalizedCache = newNormalizedCache;
		}

		/// <summary>Input functions to read in phrases to the table</summary>
		private static readonly Pattern tabPattern = Pattern.Compile("\t");

		/// <summary>Read in phrases from a file (assumed to be tab delimited)</summary>
		/// <param name="filename">- Name of file</param>
		/// <param name="checkTag">
		/// - Indicates if there is a tag column (assumed to be 2nd column)
		/// If false, treats entire line as the phrase
		/// </param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrases(string filename, bool checkTag)
		{
			ReadPhrases(filename, checkTag, tabPattern);
		}

		/// <summary>Read in phrases from a file.</summary>
		/// <remarks>Read in phrases from a file.  Column delimiters are matched using regex</remarks>
		/// <param name="filename">- Name of file</param>
		/// <param name="checkTag">
		/// - Indicates if there is a tag column (assumed to be 2nd column)
		/// If false, treats entire line as the phrase
		/// </param>
		/// <param name="delimiterRegex">- Regex for identifying column delimiter</param>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrases(string filename, bool checkTag, string delimiterRegex)
		{
			ReadPhrases(filename, checkTag, Pattern.Compile(delimiterRegex));
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrases(string filename, bool checkTag, Pattern delimiterPattern)
		{
			Timing timer = new Timing();
			timer.Doing("Reading phrases: " + filename);
			BufferedReader br = IOUtils.GetBufferedFileReader(filename);
			string line;
			while ((line = br.ReadLine()) != null)
			{
				if (checkTag)
				{
					string[] columns = delimiterPattern.Split(line, 2);
					if (columns.Length == 1)
					{
						AddPhrase(columns[0]);
					}
					else
					{
						AddPhrase(columns[0], columns[1]);
					}
				}
				else
				{
					AddPhrase(line);
				}
			}
			br.Close();
			timer.Done();
		}

		/// <summary>Read in phrases where there is each pattern has a score of being associated with a certain tag.</summary>
		/// <remarks>
		/// Read in phrases where there is each pattern has a score of being associated with a certain tag.
		/// The file format is assumed to be
		/// phrase\ttag1 count\ttag2 count...
		/// where the phrases and tags are delimited by tabs, and each tag and count is delimited by whitespaces
		/// </remarks>
		/// <param name="filename"/>
		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrasesWithTagScores(string filename)
		{
			ReadPhrasesWithTagScores(filename, tabPattern, whitespacePattern);
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrasesWithTagScores(string filename, string fieldDelimiterRegex, string countDelimiterRegex)
		{
			ReadPhrasesWithTagScores(filename, Pattern.Compile(fieldDelimiterRegex), Pattern.Compile(countDelimiterRegex));
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrasesWithTagScores(string filename, Pattern fieldDelimiterPattern, Pattern countDelimiterPattern)
		{
			Timing timer = new Timing();
			timer.Doing("Reading phrases: " + filename);
			BufferedReader br = IOUtils.GetBufferedFileReader(filename);
			string line;
			int lineno = 0;
			while ((line = br.ReadLine()) != null)
			{
				string[] columns = fieldDelimiterPattern.Split(line);
				string phrase = columns[0];
				// Pick map factory to use depending on number of tags we have
				MapFactory<string, MutableDouble> mapFactory = (columns.Length < 20) ? MapFactory.ArrayMapFactory<string, MutableDouble>() : MapFactory.LinkedHashMapFactory<string, MutableDouble>();
				ICounter<string> counts = new ClassicCounter<string>(mapFactory);
				for (int i = 1; i < columns.Length; i++)
				{
					string[] tagCount = countDelimiterPattern.Split(columns[i], 2);
					if (tagCount.Length == 2)
					{
						try
						{
							counts.SetCount(tagCount[0], double.Parse(tagCount[1]));
						}
						catch (NumberFormatException ex)
						{
							throw new Exception("Error processing field " + i + ": '" + columns[i] + "' from (" + filename + ":" + lineno + "): " + line, ex);
						}
					}
					else
					{
						throw new Exception("Error processing field " + i + ": '" + columns[i] + "' from + (" + filename + ":" + lineno + "): " + line);
					}
				}
				AddPhrase(phrase, null, counts);
				lineno++;
			}
			br.Close();
			timer.Done();
		}

		/// <exception cref="System.IO.IOException"/>
		public virtual void ReadPhrases(string filename, int phraseColIndex, int tagColIndex)
		{
			if (phraseColIndex < 0)
			{
				throw new ArgumentException("Invalid phraseColIndex " + phraseColIndex);
			}
			Timing timer = new Timing();
			timer.Doing("Reading phrases: " + filename);
			BufferedReader br = IOUtils.GetBufferedFileReader(filename);
			string line;
			while ((line = br.ReadLine()) != null)
			{
				string[] columns = tabPattern.Split(line);
				string phrase = columns[phraseColIndex];
				string tag = (tagColIndex >= 0) ? columns[tagColIndex] : null;
				AddPhrase(phrase, tag);
			}
			br.Close();
			timer.Done();
		}

		public static PhraseTable.Phrase GetLongestPhrase(IList<PhraseTable.Phrase> phrases)
		{
			PhraseTable.Phrase longest = null;
			foreach (PhraseTable.Phrase phrase in phrases)
			{
				if (longest == null || phrase.IsLonger(longest))
				{
					longest = phrase;
				}
			}
			return longest;
		}

		public virtual string[] SplitText(string phraseText)
		{
			string[] words;
			if (tokenizer != null)
			{
				Annotation annotation = new Annotation(phraseText);
				tokenizer.Annotate(annotation);
				IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
				words = new string[tokens.Count];
				for (int i = 0; i < tokens.Count; i++)
				{
					words[i] = tokens[i].Word();
				}
			}
			else
			{
				phraseText = possPattern.Matcher(phraseText).ReplaceAll(" 's$1");
				words = delimPattern.Split(phraseText);
			}
			return words;
		}

		public virtual PhraseTable.IWordList ToWordList(string phraseText)
		{
			string[] words = SplitText(phraseText);
			return new PhraseTable.StringList(words);
		}

		public virtual PhraseTable.IWordList ToNormalizedWordList(string phraseText)
		{
			string[] words = SplitText(phraseText);
			IList<string> list = new List<string>(words.Length);
			foreach (string word in words)
			{
				word = GetNormalizedForm(word);
				if (word.Length > 0)
				{
					list.Add(word);
				}
			}
			return new PhraseTable.StringList(list);
		}

		public virtual void AddPhrases(ICollection<string> phraseTexts)
		{
			foreach (string phraseText in phraseTexts)
			{
				AddPhrase(phraseText, null);
			}
		}

		public virtual void AddPhrases(IDictionary<string, string> taggedPhraseTexts)
		{
			foreach (string phraseText in taggedPhraseTexts.Keys)
			{
				AddPhrase(phraseText, taggedPhraseTexts[phraseText]);
			}
		}

		public virtual bool AddPhrase(string phraseText)
		{
			return AddPhrase(phraseText, null);
		}

		public virtual bool AddPhrase(string phraseText, string tag)
		{
			return AddPhrase(phraseText, tag, null);
		}

		public virtual bool AddPhrase(string phraseText, string tag, object phraseData)
		{
			PhraseTable.IWordList wordList = ToNormalizedWordList(phraseText);
			return AddPhrase(phraseText, tag, wordList, phraseData);
		}

		public virtual bool AddPhrase(IList<string> tokens)
		{
			return AddPhrase(tokens, null);
		}

		public virtual bool AddPhrase(IList<string> tokens, string tag)
		{
			return AddPhrase(tokens, tag, null);
		}

		public virtual bool AddPhrase(IList<string> tokens, string tag, object phraseData)
		{
			PhraseTable.IWordList wordList = new PhraseTable.StringList(tokens);
			return AddPhrase(StringUtils.Join(tokens, " "), tag, wordList, phraseData);
		}

		private int MaxListSize = 20;

		private bool AddPhrase(string phraseText, string tag, PhraseTable.IWordList wordList, object phraseData)
		{
			lock (this)
			{
				if (rootTree == null)
				{
					rootTree = new Dictionary<string, object>();
				}
				return AddPhrase(rootTree, phraseText, tag, wordList, phraseData, 0);
			}
		}

		private void AddPhrase(IDictionary<string, object> tree, PhraseTable.Phrase phrase, int wordIndex)
		{
			lock (this)
			{
				string word = (phrase.wordList.Size() <= wordIndex) ? PhraseEnd : phrase.wordList.GetWord(wordIndex);
				object node = tree[word];
				if (node == null)
				{
					tree[word] = phrase;
				}
				else
				{
					if (node is PhraseTable.Phrase)
					{
						// create list with this phrase and other and put it here
						IList list = new ArrayList(2);
						list.Add(phrase);
						list.Add(node);
						tree[word] = list;
					}
					else
					{
						if (node is IDictionary)
						{
							AddPhrase((IDictionary<string, object>)node, phrase, wordIndex + 1);
						}
						else
						{
							if (node is IList)
							{
								((IList)node).Add(phrase);
							}
							else
							{
								throw new Exception("Unexpected class " + node.GetType() + " while adding word " + wordIndex + "(" + word + ") in phrase " + phrase.GetText());
							}
						}
					}
				}
			}
		}

		private bool AddPhrase(IDictionary<string, object> tree, string phraseText, string tag, PhraseTable.IWordList wordList, object phraseData, int wordIndex)
		{
			lock (this)
			{
				// Find place to insert this item
				bool phraseAdded = false;
				// True if this phrase was successfully added to the phrase table
				bool newPhraseAdded = false;
				// True if the phrase was a new phrase
				bool oldPhraseNewFormAdded = false;
				// True if the phrase already exists, and this was new form added to old phrase
				for (int i = wordIndex; i < wordList.Size(); i++)
				{
					string word = Interner.GlobalIntern(wordList.GetWord(i));
					object node = tree[word];
					if (node == null)
					{
						// insert here
						PhraseTable.Phrase phrase = new PhraseTable.Phrase(wordList, phraseText, tag, phraseData);
						tree[word] = phrase;
						phraseAdded = true;
						newPhraseAdded = true;
					}
					else
					{
						if (node is PhraseTable.Phrase)
						{
							// check rest of the phrase matches
							PhraseTable.Phrase oldphrase = (PhraseTable.Phrase)node;
							int matchedTokenEnd = CheckWordListMatch(oldphrase, wordList, 0, wordList.Size(), i + 1, true);
							if (matchedTokenEnd >= 0)
							{
								oldPhraseNewFormAdded = oldphrase.AddForm(phraseText);
							}
							else
							{
								// create list with this phrase and other and put it here
								PhraseTable.Phrase newphrase = new PhraseTable.Phrase(wordList, phraseText, tag, phraseData);
								IList list = new ArrayList(2);
								list.Add(oldphrase);
								list.Add(newphrase);
								tree[word] = list;
								newPhraseAdded = true;
							}
							phraseAdded = true;
						}
						else
						{
							if (node is IDictionary)
							{
								tree = (IDictionary<string, object>)node;
							}
							else
							{
								if (node is IList)
								{
									// Search through list for matches to word (at this point, the table is small, so no Map)
									IList lookupList = (IList)node;
									int nMaps = 0;
									foreach (object obj in lookupList)
									{
										if (obj is PhraseTable.Phrase)
										{
											// check rest of the phrase matches
											PhraseTable.Phrase oldphrase = (PhraseTable.Phrase)obj;
											int matchedTokenEnd = CheckWordListMatch(oldphrase, wordList, 0, wordList.Size(), i, true);
											if (matchedTokenEnd >= 0)
											{
												oldPhraseNewFormAdded = oldphrase.AddForm(phraseText);
												phraseAdded = true;
												break;
											}
										}
										else
										{
											if (obj is IDictionary)
											{
												if (nMaps == 1)
												{
													throw new Exception("More than one map in list while adding word " + i + "(" + word + ") in phrase " + phraseText);
												}
												tree = (IDictionary<string, object>)obj;
												nMaps++;
											}
											else
											{
												throw new Exception("Unexpected class in list " + obj.GetType() + " while adding word " + i + "(" + word + ") in phrase " + phraseText);
											}
										}
									}
									if (!phraseAdded && nMaps == 0)
									{
										// add to list
										PhraseTable.Phrase newphrase = new PhraseTable.Phrase(wordList, phraseText, tag, phraseData);
										lookupList.Add(newphrase);
										newPhraseAdded = true;
										phraseAdded = true;
										if (lookupList.Count > MaxListSize)
										{
											// convert lookupList (should consist only of phrases) to map
											IDictionary newMap = new Dictionary<string, object>(lookupList.Count);
											foreach (object obj_1 in lookupList)
											{
												if (obj_1 is PhraseTable.Phrase)
												{
													PhraseTable.Phrase oldphrase = (PhraseTable.Phrase)obj_1;
													AddPhrase(newMap, oldphrase, i + 1);
												}
												else
												{
													throw new Exception("Unexpected class in list " + obj_1.GetType() + " while converting list to map");
												}
											}
											tree[word] = newMap;
										}
									}
								}
								else
								{
									throw new Exception("Unexpected class in list " + node.GetType() + " while adding word " + i + "(" + word + ") in phrase " + phraseText);
								}
							}
						}
					}
					if (phraseAdded)
					{
						break;
					}
				}
				if (!phraseAdded)
				{
					if (wordList.Size() == 0)
					{
						log.Warn(phraseText + " not added");
					}
					else
					{
						PhraseTable.Phrase oldphrase = (PhraseTable.Phrase)tree[PhraseEnd];
						if (oldphrase != null)
						{
							int matchedTokenEnd = CheckWordListMatch(oldphrase, wordList, 0, wordList.Size(), wordList.Size(), true);
							if (matchedTokenEnd >= 0)
							{
								oldPhraseNewFormAdded = oldphrase.AddForm(phraseText);
							}
							else
							{
								// create list with this phrase and other and put it here
								PhraseTable.Phrase newphrase = new PhraseTable.Phrase(wordList, phraseText, tag, phraseData);
								IList list = new ArrayList(2);
								list.Add(oldphrase);
								list.Add(newphrase);
								tree[PhraseEnd] = list;
								newPhraseAdded = true;
							}
						}
						else
						{
							PhraseTable.Phrase newphrase = new PhraseTable.Phrase(wordList, phraseText, tag, phraseData);
							tree[PhraseEnd] = newphrase;
							newPhraseAdded = true;
						}
					}
				}
				if (newPhraseAdded)
				{
					nPhrases++;
					nStrings++;
				}
				else
				{
					nStrings++;
				}
				return (newPhraseAdded || oldPhraseNewFormAdded);
			}
		}

		public virtual string GetNormalizedForm(string word)
		{
			string normalized = normalizedCache[word];
			if (normalized == null)
			{
				normalized = CreateNormalizedForm(word);
				lock (this)
				{
					normalizedCache[word] = normalized;
				}
			}
			return normalized;
		}

		private static readonly Pattern punctWhitespacePattern = Pattern.Compile("\\s*(\\p{Punct})\\s*");

		private static readonly Pattern whitespacePattern = Pattern.Compile("\\s+");

		private static readonly Pattern delimPattern = Pattern.Compile("[\\s_-]+");

		private static readonly Pattern possPattern = Pattern.Compile("'s(\\s+|$)");

		private string CreateNormalizedForm(string word)
		{
			if (normalize)
			{
				word = StringUtils.Normalize(word);
			}
			if (caseInsensitive)
			{
				word = word.ToLower();
			}
			if (ignorePunctuation)
			{
				word = punctWhitespacePattern.Matcher(word).ReplaceAll(string.Empty);
			}
			else
			{
				if (ignorePunctuationTokens)
				{
					if (punctWhitespacePattern.Matcher(word).Matches())
					{
						word = string.Empty;
					}
				}
			}
			word = whitespacePattern.Matcher(word).ReplaceAll(string.Empty);
			return word;
		}

		public virtual PhraseTable.Phrase Lookup(string phrase)
		{
			return Lookup(ToWordList(phrase));
		}

		public virtual PhraseTable.Phrase LookupNormalized(string phrase)
		{
			return Lookup(ToNormalizedWordList(phrase));
		}

		public virtual PhraseTable.Phrase Lookup(PhraseTable.IWordList wordList)
		{
			if (wordList == null || rootTree == null)
			{
				return null;
			}
			IDictionary<string, object> tree = rootTree;
			for (int i = 0; i < wordList.Size(); i++)
			{
				string word = wordList.GetWord(i);
				object node = tree[word];
				if (node == null)
				{
					return null;
				}
				else
				{
					if (node is PhraseTable.Phrase)
					{
						PhraseTable.Phrase phrase = (PhraseTable.Phrase)node;
						int matchedTokenEnd = CheckWordListMatch(phrase, wordList, 0, wordList.Size(), i, true);
						if (matchedTokenEnd >= 0)
						{
							return phrase;
						}
					}
					else
					{
						if (node is IDictionary)
						{
							tree = (IDictionary<string, object>)node;
						}
						else
						{
							if (node is IList)
							{
								// Search through list for matches to word (at this point, the table is small, so no Map)
								IList lookupList = (IList)node;
								int nMaps = 0;
								foreach (object obj in lookupList)
								{
									if (obj is PhraseTable.Phrase)
									{
										// check rest of the phrase matches
										PhraseTable.Phrase phrase = (PhraseTable.Phrase)obj;
										int matchedTokenEnd = CheckWordListMatch(phrase, wordList, 0, wordList.Size(), i, true);
										if (matchedTokenEnd >= 0)
										{
											return phrase;
										}
									}
									else
									{
										if (obj is IDictionary)
										{
											if (nMaps == 1)
											{
												throw new Exception("More than one map in list while looking up word " + i + "(" + word + ") in phrase " + wordList.ToString());
											}
											tree = (IDictionary<string, object>)obj;
											nMaps++;
										}
										else
										{
											throw new Exception("Unexpected class in list " + obj.GetType() + " while looking up word " + i + "(" + word + ") in phrase " + wordList.ToString());
										}
									}
								}
								if (nMaps == 0)
								{
									return null;
								}
							}
							else
							{
								throw new Exception("Unexpected class in list " + node.GetType() + " while looking up word " + i + "(" + word + ") in phrase " + wordList.ToString());
							}
						}
					}
				}
			}
			PhraseTable.Phrase phrase_1 = (PhraseTable.Phrase)tree[PhraseEnd];
			if (phrase_1 != null)
			{
				int matchedTokenEnd = CheckWordListMatch(phrase_1, wordList, 0, wordList.Size(), wordList.Size(), true);
				return (matchedTokenEnd >= 0) ? phrase_1 : null;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Given a segment of text, returns list of spans (PhraseMatch) that corresponds
		/// to a phrase in the table
		/// </summary>
		/// <param name="text">Input text to search over</param>
		/// <returns>List of all matched spans</returns>
		public virtual IList<PhraseTable.PhraseMatch> FindAllMatches(string text)
		{
			PhraseTable.IWordList tokens = ToNormalizedWordList(text);
			return FindAllMatches(tokens, 0, tokens.Size(), false);
		}

		/// <summary>
		/// Given a list of tokens, returns list of spans (PhraseMatch) that corresponds
		/// to a phrase in the table
		/// </summary>
		/// <param name="tokens">List of tokens to search over</param>
		/// <returns>List of all matched spans</returns>
		public virtual IList<PhraseTable.PhraseMatch> FindAllMatches(PhraseTable.IWordList tokens)
		{
			return FindAllMatches(tokens, 0, tokens.Size(), true);
		}

		/// <summary>
		/// Given a segment of text, returns list of spans (PhraseMatch) that corresponds
		/// to a phrase in the table (filtered by the list of acceptable phrase)
		/// </summary>
		/// <param name="acceptablePhrases">- What phrases to look for (need to be subset of phrases already in table)</param>
		/// <param name="text">Input text to search over</param>
		/// <returns>List of all matched spans</returns>
		public virtual IList<PhraseTable.PhraseMatch> FindAllMatches(IList<PhraseTable.Phrase> acceptablePhrases, string text)
		{
			PhraseTable.IWordList tokens = ToNormalizedWordList(text);
			return FindAllMatches(acceptablePhrases, tokens, 0, tokens.Size(), false);
		}

		/// <summary>
		/// Given a list of tokens, returns list of spans (PhraseMatch) that corresponds
		/// to a phrase in the table (filtered by the list of acceptable phrase)
		/// </summary>
		/// <param name="acceptablePhrases">- What phrases to look for (need to be subset of phrases already in table)</param>
		/// <param name="tokens">List of tokens to search over</param>
		/// <returns>List of all matched spans</returns>
		public virtual IList<PhraseTable.PhraseMatch> FindAllMatches(IList<PhraseTable.Phrase> acceptablePhrases, PhraseTable.IWordList tokens)
		{
			return FindAllMatches(acceptablePhrases, tokens, 0, tokens.Size(), true);
		}

		public virtual IList<PhraseTable.PhraseMatch> FindAllMatches(PhraseTable.IWordList tokens, int tokenStart, int tokenEnd, bool needNormalization)
		{
			return FindMatches(null, tokens, tokenStart, tokenEnd, needNormalization, true, false);
		}

		/* find all */
		/* don't need to match end exactly */
		public virtual IList<PhraseTable.PhraseMatch> FindAllMatches(IList<PhraseTable.Phrase> acceptablePhrases, PhraseTable.IWordList tokens, int tokenStart, int tokenEnd, bool needNormalization)
		{
			return FindMatches(acceptablePhrases, tokens, tokenStart, tokenEnd, needNormalization, true, false);
		}

		/* find all */
		/* don't need to match end exactly */
		public virtual IList<PhraseTable.PhraseMatch> FindMatches(string text)
		{
			PhraseTable.IWordList tokens = ToNormalizedWordList(text);
			return FindMatches(tokens, 0, tokens.Size(), false);
		}

		public virtual IList<PhraseTable.PhraseMatch> FindMatches(PhraseTable.IWordList tokens)
		{
			return FindMatches(tokens, 0, tokens.Size(), true);
		}

		public virtual IList<PhraseTable.PhraseMatch> FindMatches(PhraseTable.IWordList tokens, int tokenStart, int tokenEnd, bool needNormalization)
		{
			return FindMatches(null, tokens, tokenStart, tokenEnd, needNormalization, false, false);
		}

		/* don't need to find all */
		/* don't need to match end exactly */
		public virtual IList<PhraseTable.PhraseMatch> FindMatches(string text, int tokenStart, int tokenEnd, bool needNormalization)
		{
			PhraseTable.IWordList tokens = ToNormalizedWordList(text);
			return FindMatches(tokens, tokenStart, tokenEnd, false);
		}

		protected internal virtual int CheckWordListMatch(PhraseTable.Phrase phrase, PhraseTable.IWordList tokens, int tokenStart, int tokenEnd, int checkStart, bool matchEnd)
		{
			if (checkStart < tokenStart)
			{
				return -1;
			}
			int i;
			int phraseSize = phrase.wordList.Size();
			for (i = checkStart; i < tokenEnd && i - tokenStart < phraseSize; i++)
			{
				string word = tokens.GetWord(i);
				string phraseWord = phrase.wordList.GetWord(i - tokenStart);
				if (!phraseWord.Equals(word))
				{
					return -1;
				}
			}
			if (i - tokenStart == phraseSize)
			{
				// All tokens in phrase has been matched!
				if (matchEnd)
				{
					return (i == tokenEnd) ? i : -1;
				}
				else
				{
					return i;
				}
			}
			else
			{
				return -1;
			}
		}

		public virtual IList<PhraseTable.PhraseMatch> FindNonOverlappingPhrases(IList<PhraseTable.PhraseMatch> phraseMatches)
		{
			if (phraseMatches.Count > 1)
			{
				return IntervalTree.GetNonOverlapping(phraseMatches, PhrasematchLengthEndpointsComparator);
			}
			else
			{
				return phraseMatches;
			}
		}

		protected internal virtual IList<PhraseTable.PhraseMatch> FindMatches(ICollection<PhraseTable.Phrase> acceptablePhrases, PhraseTable.IWordList tokens, int tokenStart, int tokenEnd, bool needNormalization, bool findAll, bool matchEnd)
		{
			if (needNormalization)
			{
				System.Diagnostics.Debug.Assert((tokenStart >= 0));
				System.Diagnostics.Debug.Assert((tokenEnd > tokenStart));
				int n = tokenEnd - tokenStart;
				IList<string> normalized = new List<string>(n);
				int[] tokenIndexMap = new int[n + 1];
				int j = 0;
				int last = 0;
				for (int i = tokenStart; i < tokenEnd; i++)
				{
					string word = tokens.GetWord(i);
					word = GetNormalizedForm(word);
					if (word.Length != 0)
					{
						normalized.Add(word);
						tokenIndexMap[j] = i;
						last = i;
						j++;
					}
				}
				tokenIndexMap[j] = Math.Min(last + 1, tokenEnd);
				IList<PhraseTable.PhraseMatch> matched = FindMatchesNormalized(acceptablePhrases, new PhraseTable.StringList(normalized), 0, normalized.Count, findAll, matchEnd);
				foreach (PhraseTable.PhraseMatch pm in matched)
				{
					System.Diagnostics.Debug.Assert((pm.tokenBegin >= 0));
					System.Diagnostics.Debug.Assert((pm.tokenEnd >= pm.tokenBegin));
					System.Diagnostics.Debug.Assert((pm.tokenEnd <= normalized.Count));
					if (pm.tokenEnd > 0 && pm.tokenEnd > pm.tokenBegin)
					{
						pm.tokenEnd = tokenIndexMap[pm.tokenEnd - 1] + 1;
					}
					else
					{
						pm.tokenEnd = tokenIndexMap[pm.tokenEnd];
					}
					pm.tokenBegin = tokenIndexMap[pm.tokenBegin];
					System.Diagnostics.Debug.Assert((pm.tokenBegin >= 0));
					System.Diagnostics.Debug.Assert((pm.tokenEnd >= pm.tokenBegin));
				}
				return matched;
			}
			else
			{
				return FindMatchesNormalized(acceptablePhrases, tokens, tokenStart, tokenEnd, findAll, matchEnd);
			}
		}

		protected internal virtual IList<PhraseTable.PhraseMatch> FindMatchesNormalized(ICollection<PhraseTable.Phrase> acceptablePhrases, PhraseTable.IWordList tokens, int tokenStart, int tokenEnd, bool findAll, bool matchEnd)
		{
			IList<PhraseTable.PhraseMatch> matched = new List<PhraseTable.PhraseMatch>();
			Stack<PhraseTable.StackEntry> todoStack = new Stack<PhraseTable.StackEntry>();
			todoStack.Push(new PhraseTable.StackEntry(rootTree, tokenStart, tokenStart, tokenEnd, findAll ? tokenStart + 1 : -1));
			while (!todoStack.IsEmpty())
			{
				PhraseTable.StackEntry cur = todoStack.Pop();
				IDictionary<string, object> tree = cur.tree;
				for (int i = cur.tokenNext; i <= cur.tokenEnd; i++)
				{
					if (tree.Contains(PhraseEnd))
					{
						PhraseTable.Phrase phrase = (PhraseTable.Phrase)tree[PhraseEnd];
						if (acceptablePhrases == null || acceptablePhrases.Contains(phrase))
						{
							int matchedTokenEnd = CheckWordListMatch(phrase, tokens, cur.tokenStart, cur.tokenEnd, i, matchEnd);
							if (matchedTokenEnd >= 0)
							{
								matched.Add(new PhraseTable.PhraseMatch(phrase, cur.tokenStart, matchedTokenEnd));
							}
						}
					}
					if (i == cur.tokenEnd)
					{
						break;
					}
					string word = tokens.GetWord(i);
					object node = tree[word];
					if (node == null)
					{
						break;
					}
					else
					{
						if (node is PhraseTable.Phrase)
						{
							// check rest of the phrase matches
							PhraseTable.Phrase phrase = (PhraseTable.Phrase)node;
							if (acceptablePhrases == null || acceptablePhrases.Contains(phrase))
							{
								int matchedTokenEnd = CheckWordListMatch(phrase, tokens, cur.tokenStart, cur.tokenEnd, i + 1, matchEnd);
								if (matchedTokenEnd >= 0)
								{
									matched.Add(new PhraseTable.PhraseMatch(phrase, cur.tokenStart, matchedTokenEnd));
								}
							}
							break;
						}
						else
						{
							if (node is IDictionary)
							{
								tree = (IDictionary<string, object>)node;
							}
							else
							{
								if (node is IList)
								{
									// Search through list for matches to word (at this point, the table is small, so no Map)
									IList lookupList = (IList)node;
									foreach (object obj in lookupList)
									{
										if (obj is PhraseTable.Phrase)
										{
											// check rest of the phrase matches
											PhraseTable.Phrase phrase = (PhraseTable.Phrase)obj;
											if (acceptablePhrases == null || acceptablePhrases.Contains(phrase))
											{
												int matchedTokenEnd = CheckWordListMatch(phrase, tokens, cur.tokenStart, cur.tokenEnd, i + 1, matchEnd);
												if (matchedTokenEnd >= 0)
												{
													matched.Add(new PhraseTable.PhraseMatch(phrase, cur.tokenStart, matchedTokenEnd));
												}
											}
										}
										else
										{
											if (obj is IDictionary)
											{
												todoStack.Push(new PhraseTable.StackEntry((IDictionary<string, object>)obj, cur.tokenStart, i + 1, cur.tokenEnd, -1));
											}
											else
											{
												throw new Exception("Unexpected class in list " + obj.GetType() + " while looking up " + word);
											}
										}
									}
									break;
								}
								else
								{
									throw new Exception("Unexpected class " + node.GetType() + " while looking up " + word);
								}
							}
						}
					}
				}
				if (cur.continueAt >= 0)
				{
					int newStart = (cur.continueAt > cur.tokenStart) ? cur.continueAt : cur.tokenStart + 1;
					if (newStart < cur.tokenEnd)
					{
						todoStack.Push(new PhraseTable.StackEntry(cur.tree, newStart, newStart, cur.tokenEnd, newStart + 1));
					}
				}
			}
			return matched;
		}

		public virtual IEnumerator<PhraseTable.Phrase> Iterator()
		{
			return new PhraseTable.PhraseTableIterator(this);
		}

		private class PhraseTableIterator : AbstractIterator<PhraseTable.Phrase>
		{
			private PhraseTable phraseTable;

			private Stack<IEnumerator<object>> iteratorStack = new Stack<IEnumerator<object>>();

			private PhraseTable.Phrase next = null;

			public PhraseTableIterator(PhraseTable phraseTable)
			{
				this.phraseTable = phraseTable;
				this.iteratorStack.Push(this.phraseTable.rootTree.Values.GetEnumerator());
				this.next = GetNext();
			}

			private PhraseTable.Phrase GetNext()
			{
				while (!iteratorStack.IsEmpty())
				{
					IEnumerator<object> iter = iteratorStack.Peek();
					if (iter.MoveNext())
					{
						object obj = iter.Current;
						if (obj is PhraseTable.Phrase)
						{
							return (PhraseTable.Phrase)obj;
						}
						else
						{
							if (obj is IDictionary)
							{
								iteratorStack.Push(((IDictionary)obj).Values.GetEnumerator());
							}
							else
							{
								if (obj is IList)
								{
									iteratorStack.Push(((IList)obj).GetEnumerator());
								}
								else
								{
									throw new Exception("Unexpected class in phrase table " + obj.GetType());
								}
							}
						}
					}
					else
					{
						iteratorStack.Pop();
					}
				}
				return null;
			}

			public override bool MoveNext()
			{
				return next != null;
			}

			public override PhraseTable.Phrase Current
			{
				get
				{
					PhraseTable.Phrase res = next;
					next = GetNext();
					return res;
				}
			}
		}

		private class StackEntry
		{
			internal IDictionary<string, object> tree;

			internal int tokenStart;

			internal int tokenNext;

			internal int tokenEnd;

			internal int continueAt;

			private StackEntry(IDictionary<string, object> tree, int tokenStart, int tokenNext, int tokenEnd, int continueAt)
			{
				this.tree = tree;
				this.tokenStart = tokenStart;
				this.tokenNext = tokenNext;
				this.tokenEnd = tokenEnd;
				this.continueAt = continueAt;
			}
		}

		/// <summary>A phrase is a multiword expression</summary>
		public class Phrase
		{
			/// <summary>List of words in this phrase</summary>
			internal PhraseTable.IWordList wordList;

			internal string text;

			internal string tag;

			internal object data;

			private ICollection<string> alternateForms;

			public Phrase(PhraseTable.IWordList wordList, string text, string tag, object data)
			{
				// additional data associated with the phrase
				// Alternate forms that can be used for lookup elsewhere
				this.wordList = wordList;
				this.text = text;
				this.tag = tag;
				this.data = data;
			}

			public virtual bool IsLonger(PhraseTable.Phrase phrase)
			{
				return (this.GetWordList().Size() > phrase.GetWordList().Size() || (this.GetWordList().Size() == phrase.GetWordList().Size() && this.GetText().Length > phrase.GetText().Length));
			}

			public virtual bool AddForm(string form)
			{
				if (alternateForms == null)
				{
					alternateForms = new HashSet<string>(4);
					alternateForms.Add(text);
				}
				return alternateForms.Add(form);
			}

			public virtual PhraseTable.IWordList GetWordList()
			{
				return wordList;
			}

			public virtual string GetText()
			{
				return text;
			}

			public virtual string GetTag()
			{
				return tag;
			}

			public virtual object GetData()
			{
				return data;
			}

			public virtual ICollection<string> GetAlternateForms()
			{
				if (alternateForms == null)
				{
					IList<string> forms = new ArrayList(1);
					forms.Add(text);
					return forms;
				}
				return alternateForms;
			}

			public override string ToString()
			{
				return text;
			}
		}

		public static readonly IComparator<PhraseTable.PhraseMatch> PhrasematchLengthEndpointsComparator = Comparators.Chain(HasIntervalConstants.LengthGtComparator, HasIntervalConstants.EndpointsComparator);

		/// <summary>Represents a matched phrase</summary>
		public class PhraseMatch : IHasInterval<int>
		{
			internal PhraseTable.Phrase phrase;

			internal int tokenBegin;

			internal int tokenEnd;

			[System.NonSerialized]
			internal Interval<int> span;

			public PhraseMatch(PhraseTable.Phrase phrase, int tokenBegin, int tokenEnd)
			{
				this.phrase = phrase;
				this.tokenBegin = tokenBegin;
				this.tokenEnd = tokenEnd;
			}

			public virtual PhraseTable.Phrase GetPhrase()
			{
				return phrase;
			}

			public virtual int GetTokenBegin()
			{
				return tokenBegin;
			}

			public virtual int GetTokenEnd()
			{
				return tokenEnd;
			}

			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(phrase);
				sb.Append(" at (").Append(tokenBegin);
				sb.Append(",").Append(tokenEnd).Append(")");
				return sb.ToString();
			}

			public virtual Interval<int> GetInterval()
			{
				if (span == null)
				{
					span = Interval.ToInterval(tokenBegin, tokenEnd, Interval.IntervalOpenEnd);
				}
				return span;
			}
		}

		public static string ToString(PhraseTable.IWordList wordList)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < wordList.Size(); i++)
			{
				if (sb.Length > 0)
				{
					sb.Append(" ");
				}
				sb.Append(wordList.GetWord(i));
			}
			return sb.ToString();
		}

		public interface IWordList
		{
			string GetWord(int i);

			int Size();
		}

		public class TokenList : PhraseTable.IWordList
		{
			private IList<ICoreMap> tokens;

			private Type textKey = typeof(CoreAnnotations.TextAnnotation);

			public TokenList(IList<CoreLabel> tokens)
			{
				this.tokens = tokens;
			}

			public TokenList(IList<ICoreMap> tokens, Type key)
			{
				this.tokens = tokens;
				this.textKey = key;
			}

			public virtual string GetWord(int i)
			{
				return (string)tokens[i].Get(textKey);
			}

			public virtual int Size()
			{
				return tokens.Count;
			}

			public override string ToString()
			{
				return PhraseTable.ToString(this);
			}
		}

		public class StringList : PhraseTable.IWordList
		{
			private IList<string> words;

			public StringList(IList<string> words)
			{
				this.words = words;
			}

			public StringList(string[] wordsArray)
			{
				this.words = Arrays.AsList(wordsArray);
			}

			public virtual string GetWord(int i)
			{
				return words[i];
			}

			public virtual int Size()
			{
				return words.Count;
			}

			public override string ToString()
			{
				return PhraseTable.ToString(this);
			}
		}

		public class PhraseStringCollection : ICollection<string>
		{
			internal PhraseTable phraseTable;

			internal bool useNormalizedLookup;

			public PhraseStringCollection(PhraseTable phraseTable, bool useNormalizedLookup)
			{
				/*  public static class PhraseCollection implements Collection<Phrase>
				{
				
				} */
				this.phraseTable = phraseTable;
				this.useNormalizedLookup = useNormalizedLookup;
			}

			public virtual int Count
			{
				get
				{
					return phraseTable.nStrings;
				}
			}

			public virtual bool IsEmpty()
			{
				return phraseTable.nStrings == 0;
			}

			public virtual bool Contains(object o)
			{
				if (o is string)
				{
					if (useNormalizedLookup)
					{
						return (phraseTable.LookupNormalized((string)o) != null);
					}
					else
					{
						return (phraseTable.Lookup((string)o) != null);
					}
				}
				else
				{
					return false;
				}
			}

			public virtual IEnumerator<string> GetEnumerator()
			{
				throw new NotSupportedException("iterator is not supported for PhraseTable.PhraseStringCollection");
			}

			//      return new FunctionApplyingIterator( phraseTable.iterator(), new Function<Phrase,String>() {
			//        @Override
			//        public String apply(Phrase in) {
			//          return in.getText();
			//        }
			//      });
			public virtual object[] ToArray()
			{
				throw new NotSupportedException("toArray is not supported for PhraseTable.PhraseStringCollection");
			}

			public virtual T[] ToArray<T>(T[] a)
			{
				throw new NotSupportedException("toArray is not supported for PhraseTable.PhraseStringCollection");
			}

			public virtual bool Add(string s)
			{
				return phraseTable.AddPhrase(s);
			}

			public virtual bool Remove(object o)
			{
				throw new NotSupportedException("Remove is not supported for PhraseTable.PhraseStringCollection");
			}

			public virtual bool ContainsAll<_T0>(ICollection<_T0> c)
			{
				foreach (object o in c)
				{
					if (!Contains(o))
					{
						return false;
					}
				}
				return true;
			}

			public virtual bool AddAll<_T0>(ICollection<_T0> c)
				where _T0 : string
			{
				bool modified = false;
				foreach (string s in c)
				{
					if (Add(s))
					{
						modified = true;
					}
				}
				return modified;
			}

			public virtual bool RemoveAll<_T0>(ICollection<_T0> c)
			{
				bool modified = false;
				foreach (object o in c)
				{
					if (Remove(o))
					{
						modified = true;
					}
				}
				return modified;
			}

			public virtual bool RetainAll<_T0>(ICollection<_T0> c)
			{
				throw new NotSupportedException("retainAll is not supported for PhraseTable.PhraseStringCollection");
			}

			public virtual void Clear()
			{
				phraseTable.Clear();
			}
		}
	}
}

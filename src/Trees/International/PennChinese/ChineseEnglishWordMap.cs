// ChineseEnglishWordMap -- a mapping from Chinese to English words.
// Copyright (c) 2002, 2003, 2004 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// Map is taken from CEDict Chinese-English Lexicon.  Future versions
// will support multiple Lexicons.
//
// http://www.mandarintools.com/cedict.html
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//    Support/Questions: java-nlp-user@lists.stanford.edu
//    Licensing: java-nlp-support@lists.stanford.edu
using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Pennchinese
{
	/// <summary>A class for mapping Chinese words to English.</summary>
	/// <remarks>A class for mapping Chinese words to English.  Uses CEDict free Lexicon.</remarks>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseEnglishWordMap
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.International.Pennchinese.ChineseEnglishWordMap));

		private const long serialVersionUID = 7655332268578049993L;

		private IDictionary<string, ICollection<string>> map = Generics.NewHashMap(10000);

		private const string defaultPattern = "[^ ]+ ([^ ]+)[^/]+/(.+)/";

		private const string defaultDelimiter = "[/;]";

		private const string defaultCharset = "UTF-8";

		private static readonly string[] punctuations = new string[] { "\uff08.*?\uff09", "\\(.*?\\)", "<.*?>", "[\u2033\u20dd\u25cb\u25ef\u2039\u2329\u27e8\u203a\u232a\u27e9\u00ab\u27ea\u00bb\u27eb\u2308\u230b\u27e6\u27e7\u3030\uff5e\u201c\u2036\u201d\u2033\u2307\u301c\u3012\u29c4\u300a\u300b\u3000]"
			, "^to " };

		private const bool Debug = false;

		private bool normalized = false;

		/// <summary>SingletonHolder is loaded on the first execution of getInstance().</summary>
		private class SingletonHolder
		{
			private SingletonHolder()
			{
			}

			private static readonly ChineseEnglishWordMap Instance = new ChineseEnglishWordMap();
			// large dictionary!
		}

		/// <summary>A method for getting a singleton instance of this class.</summary>
		/// <remarks>
		/// A method for getting a singleton instance of this class.
		/// In general, you should use this method rather than the constructor,
		/// since each instance of the class is a large data file in memory.
		/// </remarks>
		/// <returns>An instance of ChineseEnglishWordMap</returns>
		public static ChineseEnglishWordMap GetInstance()
		{
			return ChineseEnglishWordMap.SingletonHolder.Instance;
		}

		/// <summary>Does the word exist in the dictionary?</summary>
		/// <param name="key">The word in Chinese</param>
		/// <returns>Whether it is in the dictionary</returns>
		public virtual bool ContainsKey(string key)
		{
			key = key.ToLower();
			key = key.Trim();
			return map.Contains(key);
		}

		/// <param name="key">a Chinese word</param>
		/// <returns>the English translation (null if not in dictionary)</returns>
		public virtual ICollection<string> GetAllTranslations(string key)
		{
			key = key.ToLower();
			key = key.Trim();
			return map[key];
		}

		/// <param name="key">a Chinese word</param>
		/// <returns>the English translations as an array (null if not in dictionary)</returns>
		public virtual string GetFirstTranslation(string key)
		{
			key = key.ToLower();
			key = key.Trim();
			ICollection<string> strings = map[key];
			if (strings == null)
			{
				return null;
			}
			else
			{
				return strings.GetEnumerator().Current;
			}
		}

		public virtual void ReadCEDict(string dictPath)
		{
			ReadCEDict(dictPath, defaultPattern, defaultDelimiter, defaultCharset);
		}

		private string Normalize(string t)
		{
			string origT;
			if (!this.normalized)
			{
				return t;
			}
			foreach (string punc in punctuations)
			{
				t = t.ReplaceAll(punc, string.Empty);
			}
			t = t.Trim();
			if (Debug && !origT.Equals(t))
			{
				log.Info("orig=" + origT);
				log.Info("norm=" + t);
			}
			return t;
		}

		private ICollection<string> Normalize(ICollection<string> trans)
		{
			if (!this.normalized)
			{
				return trans;
			}
			ICollection<string> set = Generics.NewHashSet();
			foreach (string t in trans)
			{
				t = Normalize(t);
				if (!t.Equals(string.Empty))
				{
					set.Add(t);
				}
			}
			return set;
		}

		public virtual void ReadCEDict(string dictPath, string pattern, string delimiter, string charset)
		{
			try
			{
				BufferedReader infile = new BufferedReader(new InputStreamReader(new FileInputStream(dictPath), charset));
				Pattern p = Pattern.Compile(pattern);
				for (string line = infile.ReadLine(); line != null; line = infile.ReadLine())
				{
					Matcher m = p.Matcher(line);
					if (m.Matches())
					{
						string word = (m.Group(1)).ToLower();
						word = word.Trim();
						// don't want leading or trailing spaces
						string transGroup = m.Group(2);
						string[] trans = transGroup.Split(delimiter);
						// TODO: strip out punctuations from translation
						if (map.Contains(word))
						{
							ICollection<string> oldtrans = map[word];
							foreach (string t in trans)
							{
								t = Normalize(t);
								if (!t.Equals(string.Empty))
								{
									if (!oldtrans.Contains(t))
									{
										oldtrans.Add(t);
									}
								}
							}
						}
						else
						{
							ICollection<string> transList = new LinkedHashSet<string>(Arrays.AsList(trans));
							string normW = Normalize(word);
							ICollection<string> normSet = Normalize(transList);
							if (!normW.Equals(string.Empty) && normSet.Count > 0)
							{
								map[normW] = normSet;
							}
						}
					}
				}
				infile.Close();
			}
			catch (IOException e)
			{
				throw new Exception("IOException reading CEDict from file " + dictPath, e);
			}
		}

		/// <summary>Make a ChineseEnglishWordMap with a default CEDict path.</summary>
		/// <remarks>
		/// Make a ChineseEnglishWordMap with a default CEDict path.
		/// It looks for the file "cedict_ts.u8" in the working directory, for the
		/// value of the CEDICT environment variable, and in a Stanford NLP Group
		/// specific place.  It throws an exception if a dictionary cannot be found.
		/// </remarks>
		public ChineseEnglishWordMap()
		{
			string path = CEDict.Path();
			ReadCEDict(path);
		}

		/// <summary>Make a ChineseEnglishWordMap</summary>
		/// <param name="dictPath">the path/filename of the CEDict</param>
		public ChineseEnglishWordMap(string dictPath)
		{
			ReadCEDict(dictPath);
		}

		/// <summary>Make a ChineseEnglishWordMap</summary>
		/// <param name="dictPath">the path/filename of the CEDict</param>
		/// <param name="normalized">whether the entries in dictionary are normalized or not</param>
		public ChineseEnglishWordMap(string dictPath, bool normalized)
		{
			this.normalized = normalized;
			ReadCEDict(dictPath);
		}

		public ChineseEnglishWordMap(string dictPath, string pattern, string delimiter, string charset)
		{
			ReadCEDict(dictPath, pattern, delimiter, charset);
		}

		public ChineseEnglishWordMap(string dictPath, string pattern, string delimiter, string charset, bool normalized)
		{
			this.normalized = normalized;
			ReadCEDict(dictPath, pattern, delimiter, charset);
		}

		private static bool IsDigits(string @in)
		{
			for (int i = 0; i < len; i++)
			{
				if (!char.IsDigit(@in[i]))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Returns a reversed map of the current map.</summary>
		/// <returns>A reversed map of the current map.</returns>
		public virtual IDictionary<string, ICollection<string>> GetReverseMap()
		{
			ICollection<KeyValuePair<string, ICollection<string>>> entries = map;
			IDictionary<string, ICollection<string>> rMap = Generics.NewHashMap(entries.Count);
			foreach (KeyValuePair<string, ICollection<string>> me in entries)
			{
				string k = me.Key;
				ICollection<string> transList = me.Value;
				foreach (string trans in transList)
				{
					ICollection<string> entry = rMap[trans];
					if (entry == null)
					{
						// reduce default size as most will be small
						ICollection<string> toAdd = new LinkedHashSet<string>(6);
						toAdd.Add(k);
						rMap[trans] = toAdd;
					}
					else
					{
						entry.Add(k);
					}
				}
			}
			return rMap;
		}

		/// <summary>Add all of the mappings from the specified map to the current map.</summary>
		public virtual int AddMap(IDictionary<string, ICollection<string>> addM)
		{
			int newTrans = 0;
			foreach (KeyValuePair<string, ICollection<string>> me in addM)
			{
				string k = me.Key;
				ICollection<string> addList = me.Value;
				ICollection<string> origList = map[k];
				if (origList == null)
				{
					map[k] = new LinkedHashSet<string>(addList);
					ICollection<string> newList = map[k];
					if (newList != null && newList.Count != 0)
					{
						newTrans += addList.Count;
					}
				}
				else
				{
					foreach (string toAdd in addList)
					{
						if (!(origList.Contains(toAdd)))
						{
							origList.Add(toAdd);
							newTrans++;
						}
					}
				}
			}
			return newTrans;
		}

		public override string ToString()
		{
			return map.ToString();
		}

		public virtual int Size()
		{
			return map.Count;
		}

		/// <summary>
		/// The main method reads (segmented, whitespace delimited) words from a file
		/// and prints them with their English translation(s).
		/// </summary>
		/// <remarks>
		/// The main method reads (segmented, whitespace delimited) words from a file
		/// and prints them with their English translation(s).
		/// The path and filename of the CEDict Lexicon can be supplied via the
		/// "-dictPath" flag; otherwise the default filename "cedict_ts.u8" in the
		/// current directory is checked.
		/// By default, only the first translation is printed.  If the "-all" flag
		/// is given, all translations are printed.
		/// The input and output encoding can be specified using the "-encoding" flag.
		/// Otherwise UTF-8 is assumed.
		/// </remarks>
		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			IDictionary<string, int> flagsToNumArgs = Generics.NewHashMap();
			flagsToNumArgs["-dictPath"] = 1;
			flagsToNumArgs["-encoding"] = 1;
			IDictionary<string, string[]> argMap = StringUtils.ArgsToMap(args, flagsToNumArgs);
			string[] otherArgs = argMap[null];
			if (otherArgs.Length < 1)
			{
				log.Info("usage: ChineseEnglishWordMap [-all] [-dictPath path] [-encoding enc_string] inputFile");
				System.Environment.Exit(1);
			}
			string filename = otherArgs[0];
			bool allTranslations = argMap.Contains("-all");
			string charset = defaultCharset;
			if (argMap.Contains("-encoding"))
			{
				charset = argMap["-encoding"][0];
			}
			BufferedReader r = new BufferedReader(new InputStreamReader(new FileInputStream(filename), charset));
			ITreebankLanguagePack tlp = new ChineseTreebankLanguagePack();
			string[] dpString = argMap["-dictPath"];
			ChineseEnglishWordMap cewm = (dpString == null) ? new ChineseEnglishWordMap() : new ChineseEnglishWordMap(dpString[0]);
			int totalWords = 0;
			int coveredWords = 0;
			PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, charset), true);
			for (string line = r.ReadLine(); line != null; line = r.ReadLine())
			{
				string[] words = line.Split("\\s", 1000);
				foreach (string word in words)
				{
					totalWords++;
					if (word.Length == 0)
					{
						continue;
					}
					pw.Print(StringUtils.Pad(word + ':', 8));
					if (tlp.IsPunctuationWord(word))
					{
						totalWords--;
						pw.Print(word);
					}
					else
					{
						if (IsDigits(word))
						{
							pw.Print(word + " [NUMBER]");
						}
						else
						{
							if (cewm.ContainsKey(word))
							{
								coveredWords++;
								if (allTranslations)
								{
									IList<string> trans = new List<string>(cewm.GetAllTranslations(word));
									foreach (string s in trans)
									{
										pw.Print((trans.IndexOf(s) > 0 ? "|" : string.Empty) + s);
									}
								}
								else
								{
									pw.Print(cewm.GetFirstTranslation(word));
								}
							}
							else
							{
								pw.Print("[UNK]");
							}
						}
					}
					pw.Println();
				}
				pw.Println();
			}
			r.Close();
			log.Info("Finished translating " + totalWords + " words (");
			log.Info(coveredWords + " were in dictionary).");
		}
	}
}

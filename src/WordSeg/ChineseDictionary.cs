using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <summary>
	/// This class provides a main method that loads various dictionaries, and
	/// saves them in a serialized version, and runtime compiles them into a word list used as a feature in the segmenter.
	/// </summary>
	/// <remarks>
	/// This class provides a main method that loads various dictionaries, and
	/// saves them in a serialized version, and runtime compiles them into a word list used as a feature in the segmenter.
	/// The features are added in the method
	/// <see cref="Sighan2005DocumentReaderAndWriter.AddDictionaryFeatures(ChineseDictionary, System.Type{T}, System.Type{T}, System.Type{T}, string, System.Collections.Generic.IList{E})"/>
	/// .
	/// </remarks>
	/// <author>Pi-Chuan Chang</author>
	public class ChineseDictionary
	{
		private const bool Debug = false;

		public const int MaxLexiconLength = 6;

		private static Redwood.RedwoodChannels logger = Redwood.Channels(typeof(Edu.Stanford.Nlp.Wordseg.ChineseDictionary));

		private readonly ICollection<string>[] words_ = new HashSet[MaxLexiconLength + 1];

		private readonly ChineseDocumentToSentenceProcessor cdtos_;

		// todo [2017]: This should be redone sometime to not have such a hardcoded upper limit.
		// = null;
		private void SerializeDictionary(string serializePath)
		{
			logger.Info("Serializing dictionaries to " + serializePath + " ... ");
			try
			{
				ObjectOutputStream oos = IOUtils.WriteStreamFromString(serializePath);
				//oos.writeObject(MAX_LEXICON_LENGTH);
				oos.WriteObject(words_);
				//oos.writeObject(cdtos_);
				oos.Close();
				logger.Info("done.");
			}
			catch (Exception e)
			{
				logger.Error("Failed", e);
				throw new RuntimeIOException(e);
			}
		}

		private static ICollection<string>[] LoadDictionary(string serializePath)
		{
			ICollection<string>[] dict = new HashSet[MaxLexiconLength + 1];
			for (int i = 0; i <= MaxLexiconLength; i++)
			{
				dict[i] = Generics.NewHashSet();
			}
			// logger.info("loading dictionaries from " + serializePath + "...");
			try
			{
				// once we read MAX_LEXICON_LENGTH and cdtos as well
				// now these files only store one object we care about
				//ChineseDictionary.MAX_LEXICON_LENGTH = (int) ois.readObject();
				dict = IOUtils.ReadObjectFromURLOrClasspathOrFileSystem(serializePath);
			}
			catch (Exception e)
			{
				logger.Error("Failed to load Chinese dictionary " + serializePath, e);
				throw new Exception(e);
			}
			return dict;
		}

		public ChineseDictionary(string dict)
			: this(new string[] { dict })
		{
		}

		public ChineseDictionary(string[] dicts)
			: this(dicts, null)
		{
		}

		public ChineseDictionary(string[] dicts, ChineseDocumentToSentenceProcessor cdtos)
			: this(dicts, cdtos, false)
		{
		}

		/// <summary>
		/// The first argument can be one file path, or multiple files separated by
		/// commas.
		/// </summary>
		public ChineseDictionary(string serDicts, ChineseDocumentToSentenceProcessor cdtos, bool expandMidDot)
			: this(serDicts.Split(","), cdtos, expandMidDot)
		{
		}

		public ChineseDictionary(string[] dicts, ChineseDocumentToSentenceProcessor cdtos, bool expandMidDot)
		{
			logger.Info(string.Format("Loading Chinese dictionaries from %d file%s:%n", dicts.Length, (dicts.Length == 1) ? string.Empty : "s"));
			foreach (string dict in dicts)
			{
				logger.Info("  " + dict);
			}
			for (int i = 0; i <= MaxLexiconLength; i++)
			{
				words_[i] = Generics.NewHashSet();
			}
			this.cdtos_ = cdtos;
			foreach (string dict_1 in dicts)
			{
				if (dict_1.EndsWith("ser.gz"))
				{
					// TODO: the way this is written does not work if we allow dictionaries to have different settings of MAX_LEXICON_LENGTH
					ICollection<string>[] dictwords = LoadDictionary(dict_1);
					for (int i_1 = 0; i_1 <= MaxLexiconLength; i_1++)
					{
						Sharpen.Collections.AddAll(words_[i_1], dictwords[i_1]);
						dictwords[i_1] = null;
					}
				}
				else
				{
					AddDict(dict_1, expandMidDot);
				}
			}
			int total = 0;
			for (int i_2 = 0; i_2 <= MaxLexiconLength; i_2++)
			{
				total += words_[i_2].Count;
			}
			logger.Info(string.Format("Done. Unique words in ChineseDictionary is: %d.%n", total));
		}

		private static readonly Pattern midDot = Pattern.Compile(ChineseUtils.MidDotRegexStr);

		private void AddDict(string dict, bool expandMidDot)
		{
			string content = IOUtils.SlurpFileNoExceptions(dict, "utf-8");
			string[] lines = content.Split("\n");
			logger.Info("  " + dict + ": " + lines.Length + " entries");
			foreach (string line in lines)
			{
				line = line.Trim();
				// normalize any midDot
				if (expandMidDot)
				{
					// normalize down middot chars
					line = line.ReplaceAll(ChineseUtils.MidDotRegexStr, "\u00B7");
				}
				AddOneDict(line);
				if (expandMidDot && midDot.Matcher(line).Find())
				{
					line = line.ReplaceAll(ChineseUtils.MidDotRegexStr, string.Empty);
					AddOneDict(line);
				}
			}
		}

		private void AddOneDict(string item)
		{
			int length = item.Length;
			if (length == 0)
			{
			}
			else
			{
				// Do nothing for empty items
				if (length <= MaxLexiconLength - 1)
				{
					if (cdtos_ != null)
					{
						item = cdtos_.Normalization(item);
					}
					words_[length].Add(item);
				}
				else
				{
					// insist on new String as it may save memory
					string subItem = new string(Sharpen.Runtime.Substring(item, 0, MaxLexiconLength));
					if (cdtos_ != null)
					{
						subItem = cdtos_.Normalization(subItem);
					}
					// length=MAX_LEXICON_LENGTH and MAX_LEXICON_LENGTH+
					words_[MaxLexiconLength].Add(subItem);
				}
			}
		}

		public virtual bool Contains(string word)
		{
			int length = word.Length;
			if (length <= MaxLexiconLength - 1)
			{
				return words_[length].Contains(word);
			}
			else
			{
				length = MaxLexiconLength;
				return words_[length].Contains(Sharpen.Runtime.Substring(word, 0, 6));
			}
		}

		public static void Main(string[] args)
		{
			string inputDicts = "/u/nlp/data/chinese-dictionaries/plain/ne_wikipedia-utf8.txt,/u/nlp/data/chinese-dictionaries/plain/newsexplorer_entities_utf8.txt,/u/nlp/data/chinese-dictionaries/plain/Ch-name-list-utf8.txt,/u/nlp/data/chinese-dictionaries/plain/wikilex-20070908-zh-en.txt,/u/nlp/data/chinese-dictionaries/plain/adso-1.25-050405-monolingual-clean.utf8.txt,/u/nlp/data/chinese-dictionaries/plain/lexicon_108k_normalized.txt,/u/nlp/data/chinese-dictionaries/plain/lexicon_mandarintools_normalized.txt,/u/nlp/data/chinese-dictionaries/plain/harbin-ChineseNames_utf8.txt,/u/nlp/data/chinese-dictionaries/plain/lexicon_HowNet_normalized.txt";
			string output = "/u/nlp/data/gale/segtool/stanford-seg/classifiers/dict-chris6.ser.gz";
			IDictionary<string, int> flagMap = Generics.NewHashMap();
			flagMap["-inputDicts"] = 1;
			flagMap["-output"] = 1;
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, flagMap);
			// args = argsMap.get(null);
			if (argsMap.Keys.Contains("-inputDicts"))
			{
				inputDicts = argsMap["-inputDicts"][0];
			}
			if (argsMap.Keys.Contains("-output"))
			{
				output = argsMap["-output"][0];
			}
			string[] dicts = inputDicts.Split(",");
			ChineseDocumentToSentenceProcessor cdtos = new ChineseDocumentToSentenceProcessor(null);
			bool expandMidDot = true;
			Edu.Stanford.Nlp.Wordseg.ChineseDictionary dict = new Edu.Stanford.Nlp.Wordseg.ChineseDictionary(dicts, cdtos, expandMidDot);
			dict.SerializeDictionary(output);
		}
		/*
		//ChineseDictionary dict = new ChineseDictionary(args[0]);
		for (int i = 0; i <= MAX_LEXICON_LENGTH; i++) {
		logger.info("Length: " + i+": "+dict.words[i].size());
		}
		for (int i = 0; i <= MAX_LEXICON_LENGTH; i++) {
		logger.info("Length: " + i+": "+dict.words[i].size());
		if (dict.words[i].size() < 1000) {
		for (String word : dict.words[i]) {
		EncodingPrintWriter.err.println(word, "UTF-8");
		}
		}
		}
		for  (int i = 1; i < args.length; i++) {
		logger.info(args[i] + " " + Boolean.valueOf(dict.contains(args[i])).toString());
		}
		*/
	}
}

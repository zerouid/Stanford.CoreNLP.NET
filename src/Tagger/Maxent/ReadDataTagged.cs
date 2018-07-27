/*
* Title:        StanfordMaxEnt<p>
* Description:  A Maximum Entropy Toolkit<p>
* Copyright:    Copyright (c) Trustees of Leland Stanford Junior University<p>
*/
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Tagger.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;


namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	/// <summary>Reads tagged data from a file and creates a dictionary.</summary>
	/// <remarks>
	/// Reads tagged data from a file and creates a dictionary.
	/// The tagged data has to be whitespace-separated items, with the word and
	/// tag split off by a delimiter character, which is found as the last instance
	/// of the delimiter character in the item.
	/// </remarks>
	/// <author>Kristina Toutanova</author>
	/// <version>1.0</version>
	public class ReadDataTagged
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Tagger.Maxent.ReadDataTagged));

		private readonly List<DataWordTag> v = new List<DataWordTag>();

		private int numElements = 0;

		private int totalSentences = 0;

		private int totalWords = 0;

		private readonly PairsHolder pairs;

		private readonly MaxentTagger maxentTagger;

		protected internal ReadDataTagged(TaggerConfig config, MaxentTagger maxentTagger, PairsHolder pairs)
		{
			//TODO: make a class DataHolder that holds the dict, tags, pairs, etc, for tagger and pass it around
			this.maxentTagger = maxentTagger;
			this.pairs = pairs;
			IList<TaggedFileRecord> fileRecords = TaggedFileRecord.CreateRecords(config, config.GetFile());
			IDictionary<string, IntCounter<string>> wordTagCounts = Generics.NewHashMap();
			foreach (TaggedFileRecord record in fileRecords)
			{
				LoadFile(record.Reader(), wordTagCounts);
			}
			// By counting the words and then filling the Dictionary, we can
			// make it so there are no calls that mutate the Dictionary or its
			// TagCount objects later
			maxentTagger.dict.FillWordTagCounts(wordTagCounts);
		}

		/// <summary>Frees the memory that is stored in this object by dropping the word-tag data.</summary>
		internal virtual void Release()
		{
			v.Clear();
		}

		internal virtual DataWordTag Get(int index)
		{
			return v[index];
		}

		private void LoadFile(ITaggedFileReader reader, IDictionary<string, IntCounter<string>> wordTagCounts)
		{
			log.Info("Loading tagged words from " + reader.Filename());
			List<string> words = new List<string>();
			List<string> tags = new List<string>();
			int numSentences = 0;
			int numWords = 0;
			int maxLen = int.MinValue;
			int minLen = int.MaxValue;
			foreach (IList<TaggedWord> sentence in reader)
			{
				if (maxentTagger.wordFunction != null)
				{
					IList<TaggedWord> newSentence = new List<TaggedWord>(sentence.Count);
					foreach (TaggedWord word in sentence)
					{
						TaggedWord newWord = new TaggedWord(maxentTagger.wordFunction.Apply(word.Word()), word.Tag());
						newSentence.Add(newWord);
					}
					sentence = newSentence;
				}
				foreach (TaggedWord tw in sentence)
				{
					if (tw != null)
					{
						words.Add(tw.Word());
						tags.Add(tw.Tag());
						if (!maxentTagger.tagTokens.Contains(tw.Tag()))
						{
							maxentTagger.tagTokens[tw.Tag()] = Generics.NewHashSet<string>();
						}
						maxentTagger.tagTokens[tw.Tag()].Add(tw.Word());
					}
				}
				maxLen = (sentence.Count > maxLen ? sentence.Count : maxLen);
				minLen = (sentence.Count < minLen ? sentence.Count : minLen);
				words.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosWord);
				tags.Add(Edu.Stanford.Nlp.Tagger.Common.Tagger.EosTag);
				numElements = numElements + sentence.Count + 1;
				// iterate over the words in the sentence
				for (int i = 0; i < sentence.Count + 1; i++)
				{
					History h = new History(totalWords + totalSentences, totalWords + totalSentences + sentence.Count, totalWords + totalSentences + i, pairs, maxentTagger.extractors);
					string tag = tags[i];
					string word = words[i];
					pairs.Add(new WordTag(word, tag));
					int y = maxentTagger.AddTag(tag);
					DataWordTag dat = new DataWordTag(h, y, tag);
					v.Add(dat);
					IntCounter<string> tagCounts = wordTagCounts[word];
					if (tagCounts == null)
					{
						tagCounts = new IntCounter<string>();
						wordTagCounts[word] = tagCounts;
					}
					tagCounts.IncrementCount(tag, 1);
				}
				totalSentences++;
				totalWords += sentence.Count;
				numSentences++;
				numWords += sentence.Count;
				words.Clear();
				tags.Clear();
				if ((numSentences % 100000) == 0)
				{
					log.Info("Read " + numSentences + " sentences, min " + minLen + " words, max " + maxLen + " words ... [still reading]");
				}
			}
			log.Info("Read " + numWords + " words from " + reader.Filename() + " [done].");
			log.Info("Read " + numSentences + " sentences, min " + minLen + " words, max " + maxLen + " words.");
		}

		/// <summary>
		/// Returns the number of tokens in the data read, which is the number of words
		/// plus one end sentence token per sentence.
		/// </summary>
		/// <returns>The number of tokens in the data</returns>
		public virtual int GetSize()
		{
			return numElements;
		}
	}
}

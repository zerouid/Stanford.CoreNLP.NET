using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Simple stoplist class.</summary>
	/// <author>Sepandar Kamvar</author>
	public class StopList
	{
		private ICollection<Word> wordSet;

		public StopList()
		{
			/*
			*     Constructs a stoplist with very few stopwords.
			*/
			wordSet = Generics.NewHashSet();
			AddGenericWords();
		}

		/// <summary>Constructs a new stoplist from the contents of a file.</summary>
		/// <remarks>
		/// Constructs a new stoplist from the contents of a file. It is
		/// assumed that the file contains stopwords, one on a line.
		/// The stopwords need not be in any order.
		/// </remarks>
		public StopList(File list)
		{
			wordSet = Generics.NewHashSet();
			try
			{
				BufferedReader reader = new BufferedReader(new FileReader(list));
				while (reader.Ready())
				{
					wordSet.Add(new Word(reader.ReadLine()));
				}
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
		}

		//e.printStackTrace(System.err);
		//addGenericWords();
		/// <summary>Adds some extremely common words to the stoplist.</summary>
		private void AddGenericWords()
		{
			string[] genericWords = new string[] { "a", "an", "the", "and", "or", "but", "nor" };
			for (int i = 1; i < 7; i++)
			{
				wordSet.Add(new Word(genericWords[i]));
			}
		}

		/// <summary>Returns true if the word is in the stoplist.</summary>
		public virtual bool Contains(Word word)
		{
			return wordSet.Contains(word);
		}

		/// <summary>Returns true if the word is in the stoplist.</summary>
		public virtual bool Contains(string word)
		{
			return wordSet.Contains(new Word(word));
		}
	}
}

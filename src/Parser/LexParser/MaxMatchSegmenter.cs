using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>A word-segmentation scheme using the max-match algorithm.</summary>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class MaxMatchSegmenter : IWordSegmenter
	{
		private readonly ICollection<string> words = Generics.NewHashSet();

		private const int maxLength = 10;

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
					words.Add(word.Word());
				}
			}
		}

		public virtual void FinishTraining()
		{
		}

		public virtual void LoadSegmenter(string filename)
		{
			throw new NotSupportedException();
		}

		public virtual IList<IHasWord> Segment(string s)
		{
			IList<Word> segmentedWords = new List<Word>();
			for (int start = 0; start < length; )
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
					// handle non-BMP characters
					if (s.CodePointAt(start) >= unchecked((int)(0x10000)))
					{
						segmentedWords.Add(new Word(new string(Sharpen.Runtime.Substring(s, start, start + 2))));
						start += 2;
					}
					else
					{
						segmentedWords.Add(new Word(new string(Sharpen.Runtime.Substring(s, start, start + 1))));
						start++;
					}
				}
				else
				{
					start = end;
				}
			}
			return new List<IHasWord>(segmentedWords);
		}

		private const long serialVersionUID = 8260792244886911724L;
	}
}

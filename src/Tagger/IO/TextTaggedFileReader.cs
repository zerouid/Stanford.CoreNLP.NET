using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.IO
{
	public class TextTaggedFileReader : ITaggedFileReader
	{
		internal readonly BufferedReader reader;

		internal readonly string tagSeparator;

		internal readonly string filename;

		internal int numSentences = 0;

		internal IList<TaggedWord> next;

		public TextTaggedFileReader(TaggedFileRecord record)
		{
			filename = record.file;
			try
			{
				reader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), record.encoding));
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			tagSeparator = record.tagSeparator;
			PrimeNext();
		}

		public virtual IEnumerator<IList<TaggedWord>> GetEnumerator()
		{
			return this;
		}

		public virtual string Filename()
		{
			return filename;
		}

		public virtual bool MoveNext()
		{
			return next != null;
		}

		public virtual IList<TaggedWord> Current
		{
			get
			{
				if (next == null)
				{
					throw new NoSuchElementException();
				}
				IList<TaggedWord> thisIteration = next;
				PrimeNext();
				return thisIteration;
			}
		}

		internal virtual void PrimeNext()
		{
			string line;
			try
			{
				line = reader.ReadLine();
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			if (line == null)
			{
				next = null;
				return;
			}
			++numSentences;
			next = new List<TaggedWord>();
			StringTokenizer st = new StringTokenizer(line);
			//loop over words in a single sentence
			while (st.HasMoreTokens())
			{
				string token = st.NextToken();
				int indexUnd = token.LastIndexOf(tagSeparator);
				if (indexUnd < 0)
				{
					throw new ArgumentException("Data format error: can't find delimiter \"" + tagSeparator + "\" in word \"" + token + "\" (line " + (numSentences + 1) + " of " + filename + ')');
				}
				string word = string.Intern(Sharpen.Runtime.Substring(token, 0, indexUnd));
				string tag = string.Intern(Sharpen.Runtime.Substring(token, indexUnd + 1));
				next.Add(new TaggedWord(word, tag));
			}
		}

		public virtual void Remove()
		{
			throw new NotSupportedException();
		}
	}
}

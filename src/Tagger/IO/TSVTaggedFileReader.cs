using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.IO
{
	public class TSVTaggedFileReader : ITaggedFileReader
	{
		internal readonly BufferedReader reader;

		internal readonly string filename;

		internal readonly int wordColumn;

		internal readonly int tagColumn;

		internal IList<TaggedWord> next = null;

		internal int linesRead = 0;

		internal const int DefaultWordColumn = 0;

		internal const int DefaultTagColumn = 1;

		public TSVTaggedFileReader(TaggedFileRecord record)
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
			wordColumn = ((record.wordColumn == null) ? DefaultWordColumn : record.wordColumn);
			tagColumn = ((record.tagColumn == null) ? DefaultTagColumn : record.tagColumn);
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
			// eat all blank lines until we hit the next block of text
			string line = string.Empty;
			while (line.Trim().Equals(string.Empty))
			{
				try
				{
					line = reader.ReadLine();
					++linesRead;
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
			}
			// we hit something with text, so now we read one line at a time
			// until we hit the next blank line.  the next blank line (or EOF)
			// ends the sentence.
			next = new List<TaggedWord>();
			while (line != null && !line.Trim().Equals(string.Empty))
			{
				string[] pieces = line.Split("\t");
				if (pieces.Length <= wordColumn || pieces.Length <= tagColumn)
				{
					throw new ArgumentException("File " + filename + " line #" + linesRead + " too short");
				}
				string word = pieces[wordColumn];
				string tag = pieces[tagColumn];
				next.Add(new TaggedWord(word, tag));
				try
				{
					line = reader.ReadLine();
					++linesRead;
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}
		}

		public virtual void Remove()
		{
			throw new NotSupportedException();
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.IO
{
	public class TreeTaggedFileReader : ITaggedFileReader
	{
		private readonly Treebank treebank;

		private readonly string filename;

		private readonly ITreeReaderFactory trf;

		private readonly ITreeTransformer transformer;

		private readonly TreeNormalizer normalizer;

		private readonly IPredicate<Tree> treeFilter;

		private readonly IEnumerator<Tree> treeIterator;

		private Tree next = null;

		public TreeTaggedFileReader(TaggedFileRecord record)
		{
			// int numSentences = 0;
			filename = record.file;
			trf = record.trf == null ? new LabeledScoredTreeReaderFactory() : record.trf;
			transformer = record.treeTransformer;
			normalizer = record.treeNormalizer;
			treeFilter = record.treeFilter;
			treebank = new DiskTreebank(trf, record.encoding);
			if (record.treeRange != null)
			{
				treebank.LoadPath(filename, record.treeRange);
			}
			else
			{
				treebank.LoadPath(filename);
			}
			treeIterator = treebank.GetEnumerator();
			FindNext();
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
					throw new NoSuchElementException("Iterator exhausted.");
				}
				Tree t = next;
				if (normalizer != null)
				{
					t = normalizer.NormalizeWholeTree(t, t.TreeFactory());
				}
				if (transformer != null)
				{
					t = t.Transform(transformer);
				}
				FindNext();
				return t.TaggedYield();
			}
		}

		/// <summary>Skips ahead in the iterator to the next non-filtered tree.</summary>
		private void FindNext()
		{
			while (treeIterator.MoveNext())
			{
				next = treeIterator.Current;
				if (treeFilter == null || treeFilter.Test(next))
				{
					return;
				}
			}
			next = null;
		}

		public virtual void Remove()
		{
			throw new NotSupportedException();
		}
	}
}

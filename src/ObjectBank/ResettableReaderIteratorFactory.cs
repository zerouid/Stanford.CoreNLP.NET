using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>Vends ReaderIterators which can always be rewound.</summary>
	/// <remarks>
	/// Vends ReaderIterators which can always be rewound.
	/// Java's Readers cannot be reset, but this ReaderIteratorFactory allows resetting.
	/// It the input types are anything other than Readers, then it resets them in
	/// the obvious way.  If the input is a Reader, then it's contents are saved
	/// to a tmp file (which is destroyed when the VM exits) which is then resettable.
	/// </remarks>
	/// <author>Jenny Finkel</author>
	public class ResettableReaderIteratorFactory : ReaderIteratorFactory
	{
		/// <summary>
		/// Constructs a ResettableReaderIteratorFactory from the input sources
		/// contained in the Collection.
		/// </summary>
		/// <remarks>
		/// Constructs a ResettableReaderIteratorFactory from the input sources
		/// contained in the Collection.  The Collection should contain
		/// Objects of type File, String, URL and Reader.  See class
		/// description for details.
		/// </remarks>
		/// <param name="c">Collection of input sources.</param>
		public ResettableReaderIteratorFactory(ICollection<object> c)
			: base(c)
		{
		}

		public ResettableReaderIteratorFactory(ICollection<object> c, string encoding)
			: base(c, encoding)
		{
		}

		/// <summary>
		/// Convenience constructor to construct a ResettableReaderIteratorFactory
		/// from a single input source.
		/// </summary>
		/// <remarks>
		/// Convenience constructor to construct a ResettableReaderIteratorFactory
		/// from a single input source. The Object should be of type File,
		/// String, URL or Reader.  See the class description for details.
		/// </remarks>
		/// <param name="o">An input source that can be converted into a Reader</param>
		public ResettableReaderIteratorFactory(object o)
			: base(o)
		{
		}

		/// <summary>
		/// Convenience constructor to construct a ResettableReaderIteratorFactory
		/// from a single input source.
		/// </summary>
		/// <remarks>
		/// Convenience constructor to construct a ResettableReaderIteratorFactory
		/// from a single input source. The Object should be of type File,
		/// String, URL or Reader.  See the class description for details.
		/// </remarks>
		/// <param name="o">An input source that can be converted into a Reader</param>
		/// <param name="encoding">The character encoding of a File or URL</param>
		public ResettableReaderIteratorFactory(object o, string encoding)
			: base(o, encoding)
		{
		}

		/// <summary>
		/// Constructs a ResettableReaderIteratorFactory with no initial
		/// input sources.
		/// </summary>
		public ResettableReaderIteratorFactory()
			: base()
		{
		}

		/// <summary>Returns an Iterator over the input sources in the underlying Collection.</summary>
		/// <returns>an Iterator over the input sources in the underlying Collection.</returns>
		public override IEnumerator<Reader> GetEnumerator()
		{
			ICollection<object> newCollection = new List<object>();
			foreach (object o in c)
			{
				if (o is Reader)
				{
					string name = o.ToString() + ".tmp";
					File tmpFile;
					try
					{
						tmpFile = File.CreateTempFile(name, string.Empty);
					}
					catch (Exception e)
					{
						throw new RuntimeIOException(e);
					}
					tmpFile.DeleteOnExit();
					StringUtils.PrintToFile(tmpFile, IOUtils.SlurpReader((Reader)o), false, false, enc);
					newCollection.Add(tmpFile);
				}
				else
				{
					newCollection.Add(o);
				}
			}
			c = newCollection;
			return new ReaderIteratorFactory.ReaderIterator(this);
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;





namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>
	/// A ReaderIteratorFactory provides a means of getting an Iterator
	/// which returns java.util.Readers over a Collection of input
	/// sources.
	/// </summary>
	/// <remarks>
	/// A ReaderIteratorFactory provides a means of getting an Iterator
	/// which returns java.util.Readers over a Collection of input
	/// sources.  Currently supported input sources are: Files, Strings,
	/// URLs and Readers.  A ReaderIteratorFactory may take a Collection
	/// on construction and new sources may be added either individually
	/// (via the add(Object) method) or as a Collection (via the
	/// addAll(Collection method).  The implementation automatically
	/// determines the type of input and produces a java.util.Reader
	/// accordingly.  If you wish to add support for a new kind of input,
	/// refer the the setNextObject() method of the nested class
	/// ReaderIterator.
	/// <p>
	/// The Readers returned by this class are not closed by the class when you
	/// move to the next element (nor at any other time). So, if you want the
	/// files closed, then the caller needs to close them.  The caller can only
	/// do this if they pass in Readers.  Otherwise, this class should probably
	/// close them but currently doesn't.
	/// <p>
	/// TODO: Have this class close the files that it opens.
	/// </remarks>
	/// <author><A HREF="mailto:jrfinkel@stanford.edu">Jenny Finkel</A></author>
	/// <version>1.0</version>
	public class ReaderIteratorFactory : IEnumerable<Reader>
	{
		/// <summary>
		/// Constructs a ReaderIteratorFactory from the input sources
		/// contained in the Collection.
		/// </summary>
		/// <remarks>
		/// Constructs a ReaderIteratorFactory from the input sources
		/// contained in the Collection.  The Collection should contain
		/// Objects of type File, String, URL and/or Reader.  See class
		/// description for details.
		/// </remarks>
		/// <param name="c">Collection of input sources.</param>
		public ReaderIteratorFactory(ICollection<object> c)
			: this()
		{
			//TODO: does this always store the same kind of thing in a given instance,
			//or do you want to allow having some Files, some Strings, etc.?
			Sharpen.Collections.AddAll(this.c, c);
		}

		public ReaderIteratorFactory(ICollection<object> c, string encoding)
			: this()
		{
			this.enc = encoding;
			Sharpen.Collections.AddAll(this.c, c);
		}

		/// <summary>
		/// Convenience constructor to construct a ReaderIteratorFactory from a single
		/// input source.
		/// </summary>
		/// <remarks>
		/// Convenience constructor to construct a ReaderIteratorFactory from a single
		/// input source. The Object should be of type File, String, URL and Reader.  See class
		/// description for details.
		/// </remarks>
		/// <param name="o">an input source that can be converted into a Reader</param>
		public ReaderIteratorFactory(object o)
			: this(Java.Util.Collections.Singleton(o))
		{
		}

		public ReaderIteratorFactory(object o, string encoding)
			: this(Java.Util.Collections.Singleton(o), encoding)
		{
		}

		public ReaderIteratorFactory()
		{
			c = new List<object>();
		}

		/// <summary>The underlying Collection of input sources.</summary>
		/// <remarks>
		/// The underlying Collection of input sources.  Currently supported
		/// input sources are: Files, Strings, URLs and Readers.   The
		/// implementation automatically determines the type of input and
		/// produces a java.util.Reader accordingly.
		/// </remarks>
		protected internal ICollection<object> c;

		/// <summary>The encoding for file input.</summary>
		/// <remarks>
		/// The encoding for file input.  This is defaulted to "utf-8"
		/// only applies when c is of type <code> File </code>.
		/// </remarks>
		protected internal string enc = "UTF-8";

		/// <summary>Returns an Iterator over the input sources in the underlying Collection.</summary>
		/// <returns>an Iterator over the input sources in the underlying Collection.</returns>
		public virtual IEnumerator<Reader> GetEnumerator()
		{
			return new ReaderIteratorFactory.ReaderIterator(this);
		}

		/// <summary>Adds an Object to the underlying Collection of  input sources.</summary>
		/// <param name="o">Input source to be added to the underlying Collection.</param>
		public virtual bool Add(object o)
		{
			return this.c.Add(o);
		}

		/// <summary>Removes an Object from the underlying Collection of  input sources.</summary>
		/// <param name="o">Input source to be removed from the underlying Collection.</param>
		public virtual bool Remove(object o)
		{
			return this.c.Remove(o);
		}

		/// <summary>
		/// Adds all Objects in Collection c to the underlying Collection of
		/// input sources.
		/// </summary>
		/// <param name="c">Collection of input sources to be added to the underlying Collection.</param>
		public virtual bool AddAll<_T0>(ICollection<_T0> c)
		{
			return Sharpen.Collections.AddAll(this.c, c);
		}

		/// <summary>
		/// Removes all Objects in Collection c from the underlying Collection of
		/// input sources.
		/// </summary>
		/// <param name="c">Collection of input sources to be removed from the underlying Collection.</param>
		public virtual bool RemoveAll<_T0>(ICollection<_T0> c)
		{
			return this.c.RemoveAll(c);
		}

		/// <summary>
		/// Removes all Objects from the underlying Collection of input sources
		/// except those in Collection c
		/// </summary>
		/// <param name="c">Collection of input sources to be retained in the underlying Collection.</param>
		public virtual bool RetainAll<_T0>(ICollection<_T0> c)
		{
			return this.c.RetainAll(c);
		}

		/// <summary>Iterator which contains BufferedReaders.</summary>
		internal class ReaderIterator : AbstractIterator<Reader>
		{
			private IEnumerator<object> iter;

			private Reader nextObject;

			/// <summary>Sole constructor.</summary>
			public ReaderIterator(ReaderIteratorFactory _enclosing)
			{
				this._enclosing = _enclosing;
				this.iter = this._enclosing.c.GetEnumerator();
				this.SetNextObject();
			}

			/// <summary>
			/// sets nextObject to a BufferedReader for the next input source,
			/// or null of there is no next input source.
			/// </summary>
			private void SetNextObject()
			{
				if (!this.iter.MoveNext())
				{
					this.nextObject = null;
					this.iter = null;
					return;
				}
				object o = this.iter.Current;
				try
				{
					if (o is File)
					{
						File file = (File)o;
						if (file.IsDirectory())
						{
							List<object> l = new List<object>();
							Sharpen.Collections.AddAll(l, Arrays.AsList(file.ListFiles()));
							while (this.iter.MoveNext())
							{
								l.Add(this.iter.Current);
							}
							this.iter = l.GetEnumerator();
							file = (File)this.iter.Current;
						}
						this.nextObject = IOUtils.ReaderFromFile(file, this._enclosing.enc);
					}
					else
					{
						if (o is string)
						{
							//           File file = new File((String)o);
							//           if (file.exists()) {
							//             if (file.isDirectory()) {
							//               ArrayList l = new ArrayList();
							//               l.addAll(Arrays.asList(file.listFiles()));
							//               while (iter.hasNext()) {
							//                 l.add(iter.next());
							//               }
							//               iter = l.iterator();
							//               file = (File) iter.next();
							//             }
							//             if (((String)o).endsWith(".gz")) {
							//               BufferedReader tmp = new BufferedReader(new InputStreamReader(new GZIPInputStream(new FileInputStream(file)), enc));
							//               nextObject = tmp;
							//             } else {
							//               nextObject = new BufferedReader(new EncodingFileReader(file, enc));
							//             }
							//           } else {
							this.nextObject = new BufferedReader(new StringReader((string)o));
						}
						else
						{
							//          }
							if (o is URL)
							{
								// todo: add encoding specification to this as well? -akleeman
								this.nextObject = new BufferedReader(new InputStreamReader(((URL)o).OpenStream()));
							}
							else
							{
								if (o is Reader)
								{
									this.nextObject = new BufferedReader((Reader)o);
								}
								else
								{
									throw new Exception("don't know how to get Reader from class " + o.GetType() + " of object " + o);
								}
							}
						}
					}
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}

			/// <returns>true if there is another (valid) input source to read from</returns>
			public override bool MoveNext()
			{
				return this.nextObject != null;
			}

			/// <summary>Returns nextObject and then sets nextObject to the next input source.</summary>
			/// <returns>BufferedReader for next input source.</returns>
			public override Reader Current
			{
				get
				{
					if (this.nextObject == null)
					{
						throw new NoSuchElementException();
					}
					Reader tmp = this.nextObject;
					this.SetNextObject();
					return tmp;
				}
			}

			private readonly ReaderIteratorFactory _enclosing;
		}
	}
}

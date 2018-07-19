using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Objectbank
{
	/// <summary>
	/// The ObjectBank class is designed to make it easy to change the format/source
	/// of data read in by other classes and to standardize how data is read in
	/// javaNLP classes.
	/// </summary>
	/// <remarks>
	/// The ObjectBank class is designed to make it easy to change the format/source
	/// of data read in by other classes and to standardize how data is read in
	/// javaNLP classes.
	/// This should make reuse of existing code (by non-authors of the code)
	/// easier because one has to just create a new ObjectBank which knows where to
	/// look for the data and how to turn it into Objects, and then use the new
	/// ObjectBank in the class.  This will also make it easier to reuse code for
	/// reading in the same data.
	/// <p>
	/// An ObjectBank is a Collection of Objects.  These objects are taken
	/// from input sources and then tokenized and parsed into the desired
	/// kind of Object.  An ObjectBank requires a ReaderIteratorFactory and a
	/// IteratorFromReaderFactory.  The ReaderIteratorFactory is used to get
	/// an Iterator over java.util.Readers which contain representations of
	/// the Objects.  A ReaderIteratorFactory resembles a collection that
	/// takes input sources and dispenses Iterators over java.util.Readers
	/// of those sources.  A IteratorFromReaderFactory is used to turn a single
	/// java.io.Reader into an Iterator over Objects.  The
	/// IteratorFromReaderFactory splits the contents of the java.util.Reader
	/// into Strings and then parses them into appropriate Objects.
	/// <h3>Example Usages:</h3>
	/// The general case is covered below, but the most common thing people
	/// <i>actually</i> want to do is read lines from a file.  There are special
	/// methods to make this easy!  You use the
	/// <c>getLineIterator</c>
	/// method.
	/// In its simplest use, it returns an
	/// <c>ObjectBank&lt;String&gt;</c>
	/// , which is a subclass of
	/// <c>Collection&lt;String&gt;</c>
	/// .  So, statements like these work:
	/// <pre><code>
	/// for (String str : ObjectBank.getLineIterator(filename) {
	/// System.out.println(str);
	/// }
	/// String[] strings = ObjectBank.getLineIterator(filename).toArray(new String[0]); <br /><br />
	/// String[] strings = ObjectBank.getLineIterator(filename, "GB18030").toArray(new String[0]);
	/// </code></pre>
	/// More complex uses of getLineIterator let you interpret each line of a file
	/// as an object of arbitrary type via a transformer Function.
	/// <p>
	/// For more general uses with existing classes, you first construct a collection of sources, then a class that
	/// will make the objects of interest from instances of those sources, and then set up an ObjectBank that can
	/// vend those objects:
	/// <pre>
	/// <c/>
	/// ReaderIteratorFactory rif = new ReaderIteratorFactory(Arrays.asList(new String[] { "file1", "file2", "file3" }));
	/// IteratorFromReaderFactory<Mention> corefIFRF = new MUCCorefIteratorFromReaderFactory(true);
	/// for (Mention m : new ObjectBank(rif, corefIFRF))
	/// ...
	/// }
	/// }</pre>
	/// As an example of the general power of this class, suppose you have
	/// a collection of files in the directory /u/nlp/data/gre/questions.  Each file
	/// contains several Puzzle documents which look like:
	/// <pre>
	/// &lt;puzzle&gt;
	/// &lt;preamble&gt; some text &lt;/preamble&gt;
	/// &lt;question&gt; some intro text
	/// &lt;answer&gt; answer1 &lt;/answer&gt;
	/// &lt;answer&gt; answer2 &lt;/answer&gt;
	/// &lt;answer&gt; answer3 &lt;/answer&gt;
	/// &lt;answer&gt; answer4 &lt;/answer&gt;
	/// &lt;/question&gt;
	/// &lt;question&gt; another question
	/// &lt;answer&gt; answer1 &lt;/answer&gt;
	/// &lt;answer&gt; answer2 &lt;/answer&gt;
	/// &lt;answer&gt; answer3 &lt;/answer&gt;
	/// &lt;answer&gt; answer4 &lt;/answer&gt;
	/// &lt;/question&gt;
	/// &lt;/puzzle&gt;
	/// </pre>
	/// First you need to build a ReaderIteratorFactory which will provide java.io.Readers
	/// over all the files in your directory:
	/// <pre>
	/// <c>
	/// Collection c = new FileSequentialCollection("/u/nlp/data/gre/questions/", "", false);
	/// ReaderIteratorFactory rif = new ReaderIteratorFactory(c);
	/// </c>
	/// </pre>
	/// Next you need to make an IteratorFromReaderFactory which will take the
	/// java.io.Readers vended by the ReaderIteratorFactory, split them up into
	/// documents (Strings) and
	/// then convert the Strings into Objects.  In this case we want to keep everything
	/// between each set of &lt;puzzle&gt; &lt;/puzzle&gt; tags so we would use a BeginEndTokenizerFactory.
	/// You would also need to write a class which extends Function and whose apply method
	/// converts the String between the &lt;puzzle&gt; &lt;/puzzle&gt; tags into Puzzle objects.
	/// <pre>
	/// <c>
	/// public class PuzzleParser implements Function
	/// public Object apply (Object o)
	/// String s = (String)o;
	/// ...
	/// Puzzle p = new Puzzle(...);
	/// ...
	/// return p;
	/// </c>
	/// }
	/// }</pre>
	/// Now to build the IteratorFromReaderFactory:
	/// <p>
	/// <c>IteratorFromReaderFactory rtif = new BeginEndTokenizerFactory("&lt;puzzle&gt;", "&lt;/puzzle&gt;", new PuzzleParser());</c>
	/// <p>
	/// Now, to create your ObjectBank you just give it the ReaderIteratorFactory and
	/// IteratorFromReaderFactory that you just created:
	/// <p>
	/// <c>ObjectBank puzzles = new ObjectBank(rif, rtif);</c>
	/// <p>
	/// Now, if you get a new set of puzzles that are located elsewhere and formatted differently
	/// you create a new ObjectBank for reading them in and use that ObjectBank instead with only
	/// trivial changes (or possible none at all if the ObjectBank is read in on a constructor)
	/// to your code.  Or even better, if someone else wants to use your code to evaluate their puzzles,
	/// which are  located elsewhere and formatted differently, they already know what they have to do
	/// to make your code work for them.
	/// </remarks>
	/// <author>Jenny Finkel <a href="mailto:jrfinkel@cs.stanford.edu">jrfinkel@stanford.edu</a></author>
	/// <author>Sarah Spikes (sdspikes@cs.stanford.edu) - cleanup and filling in types</author>
	[System.Serializable]
	public class ObjectBank<E> : ICollection<E>
	{
		/// <summary>
		/// This creates a new ObjectBank with the given ReaderIteratorFactory
		/// and ObjectIteratorFactory.
		/// </summary>
		/// <param name="rif">
		/// The
		/// <see cref="ReaderIteratorFactory"/>
		/// from which to get Readers
		/// </param>
		/// <param name="ifrf">
		/// The
		/// <see cref="IIteratorFromReaderFactory{T}"/>
		/// which turns java.io.Readers
		/// into Iterators of Objects
		/// </param>
		public ObjectBank(ReaderIteratorFactory rif, IIteratorFromReaderFactory<E> ifrf)
		{
			this.rif = rif;
			this.ifrf = ifrf;
		}

		protected internal ReaderIteratorFactory rif;

		protected internal IIteratorFromReaderFactory<E> ifrf;

		private IList<E> contents;

		// = null;
		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<string> GetLineIterator(string filename)
		{
			return GetLineIterator(new File(filename));
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<X> GetLineIterator<X>(string filename, IFunction<string, X> op)
		{
			return GetLineIterator(new File(filename), op);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<string> GetLineIterator(string filename, string encoding)
		{
			return GetLineIterator(new File(filename), encoding);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<string> GetLineIterator(Reader reader)
		{
			return GetLineIterator(reader, new IdentityFunction<string>());
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<X> GetLineIterator<X>(Reader reader, IFunction<string, X> op)
		{
			ReaderIteratorFactory rif = new ReaderIteratorFactory(reader);
			IIteratorFromReaderFactory<X> ifrf = LineIterator.GetFactory(op);
			return new Edu.Stanford.Nlp.Objectbank.ObjectBank<X>(rif, ifrf);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<string> GetLineIterator(File file)
		{
			return GetLineIterator(Java.Util.Collections.Singleton(file), new IdentityFunction<string>());
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<X> GetLineIterator<X>(File file, IFunction<string, X> op)
		{
			return GetLineIterator(Java.Util.Collections.Singleton(file), op);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<string> GetLineIterator(File file, string encoding)
		{
			return GetLineIterator(file, new IdentityFunction<string>(), encoding);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<X> GetLineIterator<X>(File file, IFunction<string, X> op, string encoding)
		{
			ReaderIteratorFactory rif = new ReaderIteratorFactory(file, encoding);
			IIteratorFromReaderFactory<X> ifrf = LineIterator.GetFactory(op);
			return new Edu.Stanford.Nlp.Objectbank.ObjectBank<X>(rif, ifrf);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<X> GetLineIterator<X, _T1>(ICollection<_T1> filesStringsAndReaders, IFunction<string, X> op)
		{
			ReaderIteratorFactory rif = new ReaderIteratorFactory(filesStringsAndReaders);
			IIteratorFromReaderFactory<X> ifrf = LineIterator.GetFactory(op);
			return new Edu.Stanford.Nlp.Objectbank.ObjectBank<X>(rif, ifrf);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<string> GetLineIterator<_T0>(ICollection<_T0> filesStringsAndReaders, string encoding)
		{
			return GetLineIterator(filesStringsAndReaders, new IdentityFunction<string>(), encoding);
		}

		public static Edu.Stanford.Nlp.Objectbank.ObjectBank<X> GetLineIterator<X, _T1>(ICollection<_T1> filesStringsAndReaders, IFunction<string, X> op, string encoding)
		{
			ReaderIteratorFactory rif = new ReaderIteratorFactory(filesStringsAndReaders, encoding);
			IIteratorFromReaderFactory<X> ifrf = LineIterator.GetFactory(op);
			return new Edu.Stanford.Nlp.Objectbank.ObjectBank<X>(rif, ifrf);
		}

		/// <summary>This is handy for having getLineIterator return a collection of files for feeding into another ObjectBank.</summary>
		public class PathToFileFunction : IFunction<string, File>
		{
			public virtual File Apply(string str)
			{
				return new File(str);
			}
		}

		public virtual IEnumerator<E> GetEnumerator()
		{
			// basically concatenates Iterator's made from
			// each java.io.Reader.
			if (keepInMemory)
			{
				if (contents == null)
				{
					contents = new List<E>();
					IEnumerator<E> iter = new ObjectBank.OBIterator(this);
					while (iter.MoveNext())
					{
						contents.Add(iter.Current);
					}
				}
				return contents.GetEnumerator();
			}
			return new ObjectBank.OBIterator(this);
		}

		private bool keepInMemory;

		// = false;
		/// <summary>
		/// Tells the ObjectBank to store all of
		/// its contents in memory so that it doesn't
		/// have to be recomputed each time you iterate
		/// through it.
		/// </summary>
		/// <remarks>
		/// Tells the ObjectBank to store all of
		/// its contents in memory so that it doesn't
		/// have to be recomputed each time you iterate
		/// through it.  This is useful when the data
		/// is small enough that it can be kept in
		/// memory, but reading/processing it
		/// is expensive/slow.  Defaults to false.
		/// </remarks>
		/// <param name="keep">Whether to keep contents in memory</param>
		public virtual void KeepInMemory(bool keep)
		{
			keepInMemory = keep;
		}

		/// <summary>
		/// If you are keeping the contents in memory,
		/// this will clear the memory, and they will be
		/// recomputed the next time iterator() is
		/// called.
		/// </summary>
		public virtual void ClearMemory()
		{
			contents = null;
		}

		public virtual bool IsEmpty()
		{
			return !GetEnumerator().MoveNext();
		}

		/// <summary>Can be slow.</summary>
		/// <remarks>Can be slow.  Usage not recommended.</remarks>
		public virtual bool Contains(object o)
		{
			foreach (E e in this)
			{
				if (e == o)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Can be slow.</summary>
		/// <remarks>Can be slow.  Usage not recommended.</remarks>
		public virtual bool ContainsAll<_T0>(ICollection<_T0> c)
		{
			foreach (object obj in c)
			{
				if (!Contains(obj))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Can be slow.</summary>
		/// <remarks>Can be slow.  Usage not recommended.</remarks>
		public virtual int Count
		{
			get
			{
				IEnumerator<E> iter = GetEnumerator();
				int size = 0;
				while (iter.MoveNext())
				{
					size++;
					iter.Current;
				}
				return size;
			}
		}

		public virtual void Clear()
		{
			rif = new ReaderIteratorFactory();
		}

		public virtual object[] ToArray()
		{
			IEnumerator<E> iter = GetEnumerator();
			List<object> al = new List<object>();
			while (iter.MoveNext())
			{
				al.Add(iter.Current);
			}
			return Sharpen.Collections.ToArray(al);
		}

		/// <summary>Can be slow.</summary>
		/// <remarks>Can be slow.  Usage not recommended.</remarks>
		public virtual T[] ToArray<T>(T[] o)
		{
			IEnumerator<E> iter = GetEnumerator();
			List<E> al = new List<E>();
			while (iter.MoveNext())
			{
				al.Add(iter.Current);
			}
			return Sharpen.Collections.ToArray(al, o);
		}

		/// <summary>Unsupported Operation.</summary>
		/// <remarks>
		/// Unsupported Operation.  If you wish to add a new data source,
		/// do so in the underlying ReaderIteratorFactory
		/// </remarks>
		public virtual bool Add(E o)
		{
			throw new NotSupportedException();
		}

		/// <summary>Unsupported Operation.</summary>
		/// <remarks>
		/// Unsupported Operation.  If you wish to remove a data source,
		/// do so in the underlying ReaderIteratorFactory
		/// </remarks>
		public virtual bool Remove(object o)
		{
			throw new NotSupportedException();
		}

		/// <summary>Unsupported Operation.</summary>
		/// <remarks>
		/// Unsupported Operation.  If you wish to add new data sources,
		/// do so in the underlying ReaderIteratorFactory
		/// </remarks>
		public virtual bool AddAll<_T0>(ICollection<_T0> c)
			where _T0 : E
		{
			throw new NotSupportedException();
		}

		/// <summary>Unsupported Operation.</summary>
		/// <remarks>
		/// Unsupported Operation.  If you wish to remove data sources,
		/// remove, do so in the underlying ReaderIteratorFactory.
		/// </remarks>
		public virtual bool RemoveAll<_T0>(ICollection<_T0> c)
		{
			throw new NotSupportedException();
		}

		/// <summary>Unsupported Operation.</summary>
		/// <remarks>
		/// Unsupported Operation.  If you wish to retain only certain data
		/// sources, do so in the underlying ReaderIteratorFactory.
		/// </remarks>
		public virtual bool RetainAll<_T0>(ICollection<_T0> c)
		{
			throw new NotSupportedException();
		}

		/// <summary>Iterator of Objects.</summary>
		internal class OBIterator : AbstractIterator<E>
		{
			private readonly IEnumerator<Reader> readerIterator;

			private IEnumerator<E> tok;

			private E nextObject;

			private Reader currReader;

			public OBIterator(ObjectBank<E> _enclosing)
			{
				this._enclosing = _enclosing;
				// = null;
				this.readerIterator = this._enclosing.rif.GetEnumerator();
				this.SetNextObject();
			}

			private void SetNextObject()
			{
				if (this.tok != null && this.tok.MoveNext())
				{
					this.nextObject = this.tok.Current;
				}
				else
				{
					this.SetNextObjectHelper();
				}
			}

			private void SetNextObjectHelper()
			{
				while (true)
				{
					try
					{
						if (this.currReader != null)
						{
							this.currReader.Close();
						}
					}
					catch (IOException e)
					{
						throw new Exception(e);
					}
					if (this.readerIterator.MoveNext())
					{
						this.currReader = this.readerIterator.Current;
						this.tok = this._enclosing.ifrf.GetIterator(this.currReader);
					}
					else
					{
						this.nextObject = null;
						return;
					}
					if (this.tok.MoveNext())
					{
						this.nextObject = this.tok.Current;
						return;
					}
				}
			}

			public override bool MoveNext()
			{
				return this.nextObject != null;
			}

			public override E Current
			{
				get
				{
					if (this.nextObject == null)
					{
						throw new NoSuchElementException();
					}
					E tmp = this.nextObject;
					this.SetNextObject();
					return tmp;
				}
			}

			private readonly ObjectBank<E> _enclosing;
		}

		private const long serialVersionUID = -4030295596701541770L;
		// end class OBIterator
	}
}

using System;
using System.Collections.Generic;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	/// <summary>
	/// A
	/// <c>FileSequentialCollection</c>
	/// maintains a read-only
	/// collection of
	/// <c>Files</c>
	/// .  (It's a list, but we don't
	/// make it a List or else one needs an iterator that can go backwards.)
	/// It is built from a Collection of paths, or just from a single path.
	/// Optionally one can also provide a
	/// <c>FileFilter</c>
	/// which is
	/// applied over the files in a recursive traversal, or else
	/// an extension and whether to do recursive traversal, which are used to
	/// construct a filter.
	/// Note that the Collection argument constructor will behave 'normally'
	/// iff none of the Collection elements are directories.  If they are
	/// directories they will be recursed and files in them added.  To get the
	/// behavior of putting just directories in the collection one needs to
	/// use the constructor
	/// <c>FileSequentialCollection(c, failFilt, true)</c>
	/// ,
	/// where
	/// <c>failFilt</c>
	/// is a user-supplied
	/// <c>FileFilter</c>
	/// that accepts no files.
	/// The
	/// <c>FileSequentialCollection</c>
	/// builds from these
	/// constructor arguments a collection of
	/// <c>Files</c>
	/// , which can be
	/// iterated over, etc.  This class does runtime expansion of paths.
	/// That is, it is optimized for iteration and not for random access.
	/// It is also an unmodifiable Collection.
	/// The class provides some additional constructors beyond the two recommended
	/// by the Collections package, to allow specifying a
	/// <c>FileFilter</c>
	/// and similar options.  Nevertheless, so as to avoid overburdening the
	/// the API, not every possibly useful constructor has been provided where
	/// these can be easily synthesized using standard Collections package
	/// facilities.  Useful idioms to know are:
	/// <ul>
	/// <li>To make a
	/// <c>FileSequentialCollection</c>
	/// from an array of
	/// <c>Files</c>
	/// or
	/// <c>Strings</c>
	/// 
	/// <c>arr</c>
	/// :<br />
	/// <c>FileSequentialCollection fcollect = new FileSequentialCollection(Arrays.asList(arr));</c>
	/// </li>
	/// <li>To make a
	/// <c>FileSequentialCollection</c>
	/// from a single
	/// <c>File</c>
	/// or
	/// <c>String</c>
	/// fi:<br />
	/// <c>
	/// FileSequentialCollection fcollect =
	/// new FileSequentialCollection(Collections.singletonList(fi));
	/// </c>
	/// </li>
	/// </ul>
	/// This class will throw an
	/// <c>IllegalArgumentException</c>
	/// if there
	/// are things that are not existing Files or String paths to existing files
	/// in the input collection (from the Iterator).
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>1.0, August 2002</version>
	public class FileSequentialCollection : AbstractCollection<File>
	{
		/// <summary>Stores the input collection over which we work.</summary>
		/// <remarks>
		/// Stores the input collection over which we work.  This is
		/// commonly a brief summary of a full set of files.
		/// </remarks>
		private readonly ICollection<object> coll;

		/// <summary>A filter for files to match.</summary>
		private readonly IFileFilter filt;

		private readonly bool includeDirs;

		/// <summary>
		/// Creates an empty
		/// <c>FileSequentialCollection</c>
		/// , with no Files
		/// in it.  Since a
		/// <c>FileSequentialCollection</c>
		/// is not
		/// modifiable, this is
		/// largely useless (except if you want an empty one).
		/// </summary>
		public FileSequentialCollection()
			: this((ICollection<object>)null)
		{
		}

		/// <summary>
		/// Creates a
		/// <c>FileSequentialCollection</c>
		/// from the passed in
		/// <c>Collection</c>
		/// .  The constructor iterates through the
		/// collection.  For each element, if it is a
		/// <c>File</c>
		/// or
		/// <c>String</c>
		/// , then this file path is traversed for addition
		/// to the collection.  If the argument is of some other type, an
		/// <c>IllegalArgumentException</c>
		/// is thrown.
		/// For each
		/// <c>File</c>
		/// or
		/// <c>String</c>
		/// , if they
		/// do not correspond to directories, then they are added to the
		/// collection; if they do, they are recursively explored and all
		/// non-directories within them are added to the collection.
		/// </summary>
		/// <param name="c">
		/// The collection to build the
		/// <c>FileSequentialCollection</c>
		/// from
		/// </param>
		public FileSequentialCollection(ICollection<object> c)
			: this(c, null)
		{
		}

		/// <summary>
		/// Creates a
		/// <c>FileSequentialCollection</c>
		/// from the passed in
		/// <c>File</c>
		/// path.  If the
		/// <c>File</c>
		/// does not correspond to a directory, then it is added to the
		/// collection; if it does, it is explored.  Files
		/// that match the extension, and files in subfolders that match, if
		/// appropriate, are added to the collection.
		/// This is an additional convenience constructor.
		/// </summary>
		/// <param name="path">file or directory to load from</param>
		/// <param name="suffix">suffix (normally "File extension") of files to load</param>
		/// <param name="recursively">true means descend into subdirectories as well</param>
		public FileSequentialCollection(File path, string suffix, bool recursively)
			: this(Java.Util.Collections.SingletonList(path), suffix, recursively)
		{
		}

		/// <summary>
		/// Creates a
		/// <c>FileSequentialCollection</c>
		/// from the passed in
		/// <c>Collection</c>
		/// .  The constructor iterates through the
		/// collection.  For each element, if it is a
		/// <c>File</c>
		/// , then the
		/// <c>File</c>
		/// is added to the collection, if it is a
		/// <c>String</c>
		/// , then a
		/// <c>File</c>
		/// corresponding to this
		/// <c>String</c>
		/// as a file path is added to the collection, and
		/// if the argument is of some other type, an
		/// <c>IllegalArgumentException</c>
		/// is thrown.  For the files
		/// thus specified, they are included in the collection only if they
		/// match an extension filter as specified by the other arguments.
		/// </summary>
		/// <param name="c">Collection of files or directories as Files or Strings</param>
		/// <param name="suffix">suffix (normally "File extension") of files to load</param>
		/// <param name="recursively">true means descend into subdirectories as well</param>
		public FileSequentialCollection(ICollection<object> c, string suffix, bool recursively)
			: this(c, new ExtensionFileFilter(suffix, recursively), false)
		{
		}

		/// <summary>
		/// Creates a
		/// <c>FileSequentialCollection</c>
		/// from the passed in
		/// <c>Collection</c>
		/// .  The constructor iterates through the
		/// collection.  For each element, if it is a
		/// <c>File</c>
		/// or
		/// <c>String</c>
		/// then these file paths are processed as
		/// explained below.
		/// If the argument is of some other type, an
		/// <c>IllegalArgumentException</c>
		/// is thrown.  For the files
		/// specified, if they are not directories, they are included in the
		/// collection.  If they are directories, files inside them are
		/// included iff they match the
		/// <c>FileFilter</c>
		/// .  This will
		/// include recursive directory descent iff the
		/// <c>FileFilter</c>
		/// accepts directories.
		/// If the path is a directory then only
		/// files within the directory (perhaps recursively) that satisfy the
		/// filter are processed.  If the
		/// <c>path</c>
		/// is a file, then
		/// that file is processed regardless of whether it satisfies the
		/// filter.  (This semantics was adopted, since otherwise there was no
		/// easy way to go through all the files in a directory without
		/// descending recursively via the specification of a
		/// <c>FileFilter</c>
		/// .)
		/// </summary>
		/// <param name="c">The collection of file or directory to load from</param>
		/// <param name="filt">
		/// A FileFilter of files to load.  This may be
		/// <see langword="null"/>
		/// , in which case all files are accepted.
		/// </param>
		public FileSequentialCollection(ICollection<object> c, IFileFilter filt)
			: this(c, filt, false)
		{
		}

		public FileSequentialCollection(string filename, IFileFilter filt)
			: this(Java.Util.Collections.SingletonList(filename), filt)
		{
		}

		public FileSequentialCollection(string filename)
			: this(filename, null)
		{
		}

		/// <summary>
		/// Creates a
		/// <c>FileSequentialCollection</c>
		/// from the passed in
		/// <c>Collection</c>
		/// .  The constructor iterates through the
		/// collection.  For each element, if it is a
		/// <c>File</c>
		/// or
		/// <c>String</c>
		/// then these file paths are processed as
		/// explained below.
		/// If the argument is of some other type, an
		/// <c>IllegalArgumentException</c>
		/// is thrown.  For the files
		/// specified, if they are not directories, they are included in the
		/// collection.  If they are directories, files inside them are
		/// included iff they match the
		/// <c>FileFilter</c>
		/// .  This will
		/// include recursive directory descent iff the
		/// <c>FileFilter</c>
		/// accepts directories.
		/// If the path is a directory then only
		/// files within the directory (perhaps recursively) that satisfy the
		/// filter are processed.  If the
		/// <c>path</c>
		/// is a file, then
		/// that file is processed regardless of whether it satisfies the
		/// filter.  (This semantics was adopted, since otherwise there was no
		/// easy way to go through all the files in a directory without
		/// descending recursively via the specification of a
		/// <c>FileFilter</c>
		/// .)
		/// </summary>
		/// <param name="c">
		/// The collection of file or directory to load from.  An
		/// argument of
		/// <see langword="null"/>
		/// is interpreted like an
		/// empty collection.
		/// </param>
		/// <param name="filt">
		/// A FileFilter of files to load.  This may be
		/// <see langword="null"/>
		/// , in which case all files are accepted
		/// </param>
		/// <param name="includeDirs">Whether to include directory names in the file list</param>
		public FileSequentialCollection(ICollection<object> c, IFileFilter filt, bool includeDirs)
			: base()
		{
			// store the arguments.  They are expanded by the iterator
			if (c == null)
			{
				coll = new List<object>();
			}
			else
			{
				coll = c;
			}
			this.filt = filt;
			this.includeDirs = includeDirs;
		}

		/// <summary>Returns the size of the FileSequentialCollection.</summary>
		/// <returns>size How many files are in the collection</returns>
		public override int Count
		{
			get
			{
				int counter = 0;
				foreach (File f in this)
				{
					counter++;
				}
				return counter;
			}
		}

		/// <summary>Return an Iterator over files in the collection.</summary>
		/// <remarks>
		/// Return an Iterator over files in the collection.
		/// This version lazily works its way down directories.
		/// </remarks>
		public override IEnumerator<File> GetEnumerator()
		{
			return new FileSequentialCollection.FileSequentialCollectionIterator(this);
		}

		/// <summary>This is the iterator that gets returned</summary>
		private sealed class FileSequentialCollectionIterator : IEnumerator<File>
		{
			private object[] roots;

			private int rootsIndex;

			private Stack<object> fileArrayStack;

			private Stack<int> fileArrayStackIndices;

			private File next;

			public FileSequentialCollectionIterator(FileSequentialCollection _enclosing)
			{
				this._enclosing = _enclosing;
				// current state is a rootsIterator, a position in a recursion
				// under a directory listing, and a pointer in the current
				// directory.
				// these may be of type File or String
				// these next two simulate a list of pairs, but I was too lazy to
				// make an extra class
				// log.info("Coll is " + coll);
				this.roots = Sharpen.Collections.ToArray(this._enclosing.coll);
				this.rootsIndex = 0;
				this.fileArrayStack = new Stack<object>();
				this.fileArrayStackIndices = new Stack<int>();
				if (this.roots.Length > 0)
				{
					this.fileArrayStack.Add(this.roots[this.rootsIndex]);
					this.fileArrayStackIndices.Push(int.Parse(0));
				}
				this.next = this.PrimeNextFile();
			}

			public bool MoveNext()
			{
				return this.next != null;
			}

			/// <summary>Returns the next element in the iteration.</summary>
			public File Current
			{
				get
				{
					if (this.next == null)
					{
						throw new NoSuchElementException("FileSequentialCollection exhausted");
					}
					File ret = this.next;
					this.next = this.PrimeNextFile();
					return ret;
				}
			}

			/// <summary>Not supported</summary>
			public void Remove()
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Returns the next file to be accessed, or
			/// <see langword="null"/>
			/// if
			/// there are none left.  This is all quite hairy to write as an
			/// iterator....
			/// </summary>
			/// <returns>The next file</returns>
			private File PrimeNextFile()
			{
				while (this.rootsIndex < this.roots.Length)
				{
					while (!this.fileArrayStack.Empty())
					{
						// log.info("fileArrayStack: " + fileArrayStack);
						object obj = this.fileArrayStack.Peek();
						if (obj is File[])
						{
							// log.info("Got a File[]");
							File[] files = (File[])obj;
							int index = this.fileArrayStackIndices.Pop();
							int ind = index;
							if (ind < files.Length)
							{
								index = int.Parse(ind + 1);
								this.fileArrayStackIndices.Push(index);
								this.fileArrayStack.Push(files[ind]);
							}
							else
							{
								// loop around to process this new file
								// this directory is finished and we pop up
								this.fileArrayStack.Pop();
							}
						}
						else
						{
							// take it off the stack: tail recursion optimization
							this.fileArrayStack.Pop();
							if (obj is string)
							{
								obj = new File((string)obj);
							}
							if (!(obj is File))
							{
								throw new ArgumentException("Collection elements must be Files or Strings");
							}
							File path = (File)obj;
							if (path.IsDirectory())
							{
								// log.info("Got directory " + path);
								// if path is a directory, look into it
								File[] directoryListing = path.ListFiles(this._enclosing.filt);
								if (directoryListing == null)
								{
									throw new ArgumentException("Directory access problem for: " + path);
								}
								// log.info("  with " +
								//	    directoryListing.length + " files in it.");
								if (this._enclosing.includeDirs)
								{
									// log.info("Include dir as answer");
									if (directoryListing.Length > 0)
									{
										this.fileArrayStack.Push(directoryListing);
										this.fileArrayStackIndices.Push(int.Parse(0));
									}
									return path;
								}
								else
								{
									// we don't include the dir, so we'll push
									// the directory and loop around again ...
									if (directoryListing.Length > 0)
									{
										this.fileArrayStack.Push(directoryListing);
										this.fileArrayStackIndices.Push(int.Parse(0));
									}
								}
							}
							else
							{
								// otherwise there was nothing in the
								// directory; we will pop back up
								// it's just a fixed file
								// log.info("Got a plain file " + path);
								if (!path.Exists())
								{
									throw new ArgumentException("File doesn't exist: " + path);
								}
								return path;
							}
						}
					}
					// go through loop again. we've pushed or popped as needed
					// finished this root entry; go on to the next
					this.rootsIndex++;
					if (this.rootsIndex < this.roots.Length)
					{
						this.fileArrayStack.Add(this.roots[this.rootsIndex]);
						this.fileArrayStackIndices.Push(int.Parse(0));
					}
				}
				// finished everything
				return null;
			}

			private readonly FileSequentialCollection _enclosing;
		}

		/// <summary>
		/// This is simply a debugging aid that tests the functionality of
		/// the class.
		/// </summary>
		/// <remarks>
		/// This is simply a debugging aid that tests the functionality of
		/// the class.  The supplied arguments are put in a
		/// <c>Collection</c>
		/// , and passed to the
		/// <c>FileSequentialCollection</c>
		/// constructor.
		/// An iterator is then used to print the names of all the files
		/// (but not directories) in the collection.
		/// </remarks>
		/// <param name="args">A list of file paths</param>
		public static void Main(string[] args)
		{
			FileSequentialCollection fcollect = new FileSequentialCollection(Arrays.AsList(args));
			foreach (File fi in fcollect)
			{
				System.Console.Out.WriteLine(fi);
			}
			// test the other constructors
			System.Console.Out.WriteLine("Above was Collection constructor");
			System.Console.Out.WriteLine("Empty constructor");
			FileSequentialCollection fcollect2 = new FileSequentialCollection();
			foreach (File fi_1 in fcollect2)
			{
				System.Console.Out.WriteLine(fi_1);
			}
			System.Console.Out.WriteLine("File String(mrg) boolean(true) constructor");
			FileSequentialCollection fcollect3 = new FileSequentialCollection(new File(args[0]), "mrg", true);
			foreach (File fi_2 in fcollect3)
			{
				System.Console.Out.WriteLine(fi_2);
			}
			System.Console.Out.WriteLine("Collection String(mrg) boolean constructor");
			FileSequentialCollection fcollect4 = new FileSequentialCollection(Arrays.AsList(args), "mrg", true);
			foreach (File fi_3 in fcollect4)
			{
				System.Console.Out.WriteLine(fi_3);
			}
			System.Console.Out.WriteLine("Testing number range file filter");
			FileSequentialCollection fcollect5 = new FileSequentialCollection(Arrays.AsList(args), new NumberRangeFileFilter(320, 410, true));
			foreach (File fi_4 in fcollect5)
			{
				System.Console.Out.WriteLine(fi_4);
			}
			System.Console.Out.WriteLine("Testing null filter but include dirs");
			FileSequentialCollection fcollect6 = new FileSequentialCollection(Arrays.AsList(args), (IFileFilter)null, true);
			foreach (File fi_5 in fcollect6)
			{
				System.Console.Out.WriteLine(fi_5);
			}
		}
	}
}

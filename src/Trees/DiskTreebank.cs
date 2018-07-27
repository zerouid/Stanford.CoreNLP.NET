using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>DiskTreebank</code> is a <code>Collection</code> of
	/// <code>Tree</code>s.
	/// </summary>
	/// <remarks>
	/// A <code>DiskTreebank</code> is a <code>Collection</code> of
	/// <code>Tree</code>s.
	/// A <code>DiskTreebank</code> object stores merely the information to
	/// get at a corpus of trees that is stored on disk.  Access is usually
	/// via apply()'ing a TreeVisitor to each Tree in the Treebank or by using
	/// an iterator() to get an iteration over the Trees.
	/// <p/>
	/// If the root Label of the Tree objects built by the TreeReader
	/// implements HasIndex, then the filename and index of the tree in
	/// a corpus will be inserted as they are read in.
	/// </remarks>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	public sealed class DiskTreebank : Treebank
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.DiskTreebank));

		private static bool PrintFilenames = false;

		private readonly IList<File> filePaths = new List<File>();

		private readonly IList<IFileFilter> fileFilters = new List<IFileFilter>();

		private string currentFilename;

		/// <summary>Create a new DiskTreebank.</summary>
		/// <remarks>Create a new DiskTreebank. The trees are made with a <code>LabeledScoredTreeReaderFactory</code>.</remarks>
		public DiskTreebank()
			: this(new LabeledScoredTreeReaderFactory())
		{
		}

		/// <summary>Create a new treebank, set the encoding for file access.</summary>
		/// <param name="encoding">The charset encoding to use for treebank file decoding</param>
		public DiskTreebank(string encoding)
			: this(new LabeledScoredTreeReaderFactory(), encoding)
		{
		}

		/// <summary>Create a new DiskTreebank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		public DiskTreebank(ITreeReaderFactory trf)
			: base(trf)
		{
		}

		/// <summary>Create a new DiskTreebank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		/// <param name="encoding">The charset encoding to use for treebank file decoding</param>
		public DiskTreebank(ITreeReaderFactory trf, string encoding)
			: base(trf, encoding)
		{
		}

		/// <summary>Create a new Treebank.</summary>
		/// <remarks>Create a new Treebank. The trees are made with a <code>LabeledScoredTreeReaderFactory</code>.</remarks>
		/// <param name="initialCapacity">
		/// The initial size of the underlying Collection.
		/// For a <code>DiskTreebank</code>, this parameter is ignored.
		/// </param>
		public DiskTreebank(int initialCapacity)
			: this(initialCapacity, new LabeledScoredTreeReaderFactory())
		{
		}

		/// <summary>Create a new Treebank.</summary>
		/// <param name="initialCapacity">
		/// The initial size of the underlying Collection,
		/// For a <code>DiskTreebank</code>, this parameter is ignored.
		/// </param>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		public DiskTreebank(int initialCapacity, ITreeReaderFactory trf)
			: this(trf)
		{
		}

		/*
		* Absolute path of the file currently being read.
		*/
		// = null;
		/// <summary>Empty a <code>Treebank</code>.</summary>
		public override void Clear()
		{
			filePaths.Clear();
			fileFilters.Clear();
		}

		/// <summary>Load trees from given directory.</summary>
		/// <remarks>
		/// Load trees from given directory.  This version just records
		/// the paths to be processed, and actually processes them at apply time.
		/// </remarks>
		/// <param name="path">file or directory to load from</param>
		/// <param name="filt">a FilenameFilter of files to load</param>
		public override void LoadPath(File path, IFileFilter filt)
		{
			if (path.Exists())
			{
				filePaths.Add(path);
				fileFilters.Add(filt);
			}
			else
			{
				System.Console.Error.Printf("%s: File/path %s does not exist. Skipping.%n", this.GetType().FullName, path.GetPath());
			}
		}

		/// <summary>Applies the TreeVisitor to to all trees in the Treebank.</summary>
		/// <param name="tp">A class that can process trees.</param>
		public override void Apply(ITreeVisitor tp)
		{
			foreach (Tree t in this)
			{
				tp.VisitTree(t);
			}
		}

		/// <summary>Returns the absolute path of the file currently being read.</summary>
		public string GetCurrentFilename()
		{
			return currentFilename;
		}

		public IList<File> GetCurrentPaths()
		{
			return Java.Util.Collections.UnmodifiableList(filePaths);
		}

		public void PrintFileNames()
		{
			PrintFilenames = true;
		}

		private class DiskTreebankIterator : IEnumerator<Tree>
		{
			private ITreeReader tr = null;

			private Tree storedTree = null;

			private readonly IList<File> localPathList;

			private readonly IList<IFileFilter> localFilterList;

			private int fileListPtr = 0;

			private File currentFile;

			private int curLineId = 1;

			private IList<File> curFileList;

			private IEnumerator<File> curPathIter;

			private DiskTreebankIterator(DiskTreebank _enclosing)
			{
				this._enclosing = _enclosing;
				// null means iterator is exhausted (or not yet constructed)
				//Create local copies so that calls to loadPath() in the parent class
				//don't cause exceptions i.e., this iterator is valid over the state of DiskTreebank
				//when the iterator is created.
				this.localPathList = new List<File>(this._enclosing.filePaths);
				this.localFilterList = new List<IFileFilter>(this._enclosing.fileFilters);
				if (this.PrimeNextPath() && this.PrimeNextFile())
				{
					this.storedTree = this.PrimeNextTree();
				}
			}

			//In the case of a recursive file filter, performs a BFS through the directory structure.
			private bool PrimeNextPath()
			{
				while (this.fileListPtr < this.localPathList.Count && this.fileListPtr < this.localFilterList.Count)
				{
					File nextPath = this.localPathList[this.fileListPtr];
					IFileFilter nextFilter = this.localFilterList[this.fileListPtr];
					this.fileListPtr++;
					IList<File> pathListing = ((nextPath.IsDirectory()) ? Arrays.AsList(nextPath.ListFiles(nextFilter)) : Java.Util.Collections.SingletonList(nextPath));
					if (pathListing != null)
					{
						if (pathListing.Count > 1)
						{
							pathListing.Sort();
						}
						this.curFileList = new List<File>();
						foreach (File path in pathListing)
						{
							if (path.IsDirectory())
							{
								this.localPathList.Add(path);
								this.localFilterList.Add(nextFilter);
							}
							else
							{
								this.curFileList.Add(path);
							}
						}
						if (this.curFileList.Count != 0)
						{
							this.curPathIter = this.curFileList.GetEnumerator();
							return true;
						}
					}
				}
				return false;
			}

			private bool PrimeNextFile()
			{
				try
				{
					if (this.curPathIter.MoveNext() || (this.PrimeNextPath() && this.curPathIter.MoveNext()))
					{
						this.currentFile = this.curPathIter.Current;
						this._enclosing.currentFilename = this.currentFile.GetAbsolutePath();
						if (DiskTreebank.PrintFilenames)
						{
							DiskTreebank.log.Info(this.currentFile);
						}
						if (this.tr != null)
						{
							this.tr.Close();
						}
						this.tr = this._enclosing.TreeReaderFactory().NewTreeReader(IOUtils.ReaderFromFile(this.currentFile, this._enclosing.Encoding()));
						this.curLineId = 1;
						return true;
					}
				}
				catch (UnsupportedEncodingException e)
				{
					System.Console.Error.Printf("%s: Filesystem does not support encoding:%n%s%n", this.GetType().FullName, e.ToString());
					throw new Exception(e);
				}
				catch (FileNotFoundException e)
				{
					System.Console.Error.Printf("%s: File does not exist:%n%s%n", this.GetType().FullName, e.ToString());
					throw new Exception(e);
				}
				catch (IOException e)
				{
					System.Console.Error.Printf("%s: Unable to close open tree reader:%n%s%n", this.GetType().FullName, this.currentFile.GetPath());
					throw new Exception(e);
				}
				return false;
			}

			private Tree PrimeNextTree()
			{
				Tree t = null;
				try
				{
					t = this.tr.ReadTree();
					if (t == null && this.PrimeNextFile())
					{
						//Current file is exhausted
						t = this.tr.ReadTree();
					}
					//Associate this tree with a file and line number
					if (t != null && t.Label() != null && t.Label() is IHasIndex)
					{
						IHasIndex lab = (IHasIndex)t.Label();
						lab.SetSentIndex(this.curLineId++);
						lab.SetDocID(this.currentFile.GetName());
					}
				}
				catch (IOException e)
				{
					System.Console.Error.Printf("%s: Error reading from file %s:%n%s%n", this.GetType().FullName, this.currentFile.GetPath(), e.ToString());
					throw new Exception(e);
				}
				return t;
			}

			/// <summary>Returns true if the iteration has more elements.</summary>
			public virtual bool MoveNext()
			{
				return this.storedTree != null;
			}

			/// <summary>Returns the next element in the iteration.</summary>
			public virtual Tree Current
			{
				get
				{
					if (this.storedTree == null)
					{
						throw new NoSuchElementException();
					}
					Tree ret = this.storedTree;
					this.storedTree = this.PrimeNextTree();
					return ret;
				}
			}

			/// <summary>Not supported</summary>
			public virtual void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly DiskTreebank _enclosing;
		}

		/// <summary>Return an Iterator over Trees in the Treebank.</summary>
		/// <remarks>
		/// Return an Iterator over Trees in the Treebank.  This is implemented
		/// by building per-file MemoryTreebanks for the files in the
		/// DiskTreebank.  As such, it isn't as efficient as using
		/// <code>apply()</code>.
		/// </remarks>
		public override IEnumerator<Tree> GetEnumerator()
		{
			return new DiskTreebank.DiskTreebankIterator(this);
		}
	}
}

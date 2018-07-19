using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A <code>MemoryTreebank</code> object stores a corpus of examples with
	/// given tree structures in memory (as a <code>List</code>).
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <version>2004/09/01</version>
	public sealed class MemoryTreebank : Treebank, IFileProcessor, IList<Tree>
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.MemoryTreebank));

		private const bool PrintFilenames = false;

		/// <summary>The collection of parse trees.</summary>
		private readonly IList<Tree> parseTrees;

		/// <summary>Create a new tree bank.</summary>
		/// <remarks>
		/// Create a new tree bank.
		/// The trees are made with a <code>LabeledScoredTreeReaderFactory</code>.
		/// <p/>
		/// <i>Compatibility note: Until Sep 2004, this used to create a Treebank
		/// with a SimpleTreeReaderFactory, but this was changed as the old
		/// default wasn't very useful, especially to naive users. This one now
		/// uses a LabledScoredTreeReaderFactory with a no-op TreeNormalizer.</i>
		/// </remarks>
		public MemoryTreebank()
			: this(new LabeledScoredTreeReaderFactory(new TreeNormalizer()))
		{
		}

		/// <summary>Create a new tree bank, using a specific TreeNormalizer.</summary>
		/// <remarks>
		/// Create a new tree bank, using a specific TreeNormalizer.
		/// The trees are made with a <code>LabeledScoredTreeReaderFactory</code>.
		/// <p/>
		/// <i>Compatibility note: Until Sep 2004, this used to create a Treebank
		/// with a SimpleTreeReaderFactory, but this was changed as the old
		/// default wasn't very useful, especially to naive users.</i>
		/// </remarks>
		public MemoryTreebank(TreeNormalizer tm)
			: this(new LabeledScoredTreeReaderFactory(tm))
		{
		}

		/// <summary>Create a new tree bank, set the encoding for file access</summary>
		/// <param name="encoding">the encoding to use for file access.</param>
		public MemoryTreebank(string encoding)
			: this(new LabeledScoredTreeReaderFactory(), encoding)
		{
		}

		/// <summary>Create a new tree bank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		public MemoryTreebank(ITreeReaderFactory trf)
			: base(trf)
		{
			//  private static final boolean BROKEN_NFS = true;
			parseTrees = new List<Tree>();
		}

		/// <summary>Create a new tree bank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		/// <param name="encoding">the encoding to use for file access.</param>
		public MemoryTreebank(ITreeReaderFactory trf, string encoding)
			: base(trf, encoding)
		{
			parseTrees = new List<Tree>();
		}

		/// <summary>Create a new tree bank.</summary>
		/// <remarks>
		/// Create a new tree bank.  The list of trees passed in is simply placed
		/// in the Treebank.  It is not copied.
		/// </remarks>
		/// <param name="trees">The trees to put in the Treebank.</param>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		/// <param name="encoding">the encoding to use for file access.</param>
		public MemoryTreebank(IList<Tree> trees, ITreeReaderFactory trf, string encoding)
			: base(trf, encoding)
		{
			parseTrees = trees;
		}

		/// <summary>Create a new Treebank.</summary>
		/// <param name="initialCapacity">
		/// The initial size of the underlying Collection,
		/// (if a Collection-based storage mechanism is being provided)
		/// </param>
		public MemoryTreebank(int initialCapacity)
			: this(initialCapacity, new LabeledScoredTreeReaderFactory(new TreeNormalizer()))
		{
		}

		/// <summary>Create a new tree bank.</summary>
		/// <param name="initialCapacity">The initial size of the underlying Collection</param>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <code>TreeReader</code>
		/// </param>
		public MemoryTreebank(int initialCapacity, ITreeReaderFactory trf)
			: base(initialCapacity, trf)
		{
			parseTrees = new List<Tree>(initialCapacity);
		}

		/// <summary>Empty a <code>Treebank</code>.</summary>
		public override void Clear()
		{
			parseTrees.Clear();
		}

		/// <summary>Load trees from given directory.</summary>
		/// <param name="path">file or directory to load from</param>
		/// <param name="filt">a FilenameFilter of files to load</param>
		public override void LoadPath(File path, IFileFilter filt)
		{
			FilePathProcessor.ProcessPath(path, filt, this);
		}

		public void LoadPath(string path, IFileFilter filt, string srlFile)
		{
			ReadSRLFile(srlFile);
			FilePathProcessor.ProcessPath(new File(path), filt, this);
			srlMap = null;
		}

		private IDictionary<string, CollectionValuedMap<int, string>> srlMap = null;

		private void ReadSRLFile(string srlFile)
		{
			srlMap = Generics.NewHashMap();
			foreach (string line in ObjectBank.GetLineIterator(new File(srlFile)))
			{
				string[] bits = line.Split("\\s+", 3);
				string filename = bits[0];
				int treeNum = System.Convert.ToInt32(bits[1]);
				string info = bits[2];
				CollectionValuedMap<int, string> cvm = srlMap[filename];
				if (cvm == null)
				{
					cvm = new CollectionValuedMap<int, string>();
					srlMap[filename] = cvm;
				}
				cvm.Add(treeNum, info);
			}
		}

		/// <summary>Load a collection of parse trees from the file of given name.</summary>
		/// <remarks>
		/// Load a collection of parse trees from the file of given name.
		/// Each tree may optionally be encased in parens to allow for Penn
		/// Treebank style trees.
		/// This methods implements the <code>FileProcessor</code> interface.
		/// </remarks>
		/// <param name="file">file to load a tree from</param>
		public void ProcessFile(File file)
		{
			ITreeReader tr = null;
			// SRL stuff
			CollectionValuedMap<int, string> srlMap = null;
			if (this.srlMap != null)
			{
				// there must be a better way ...
				string filename = file.GetAbsolutePath();
				foreach (string suffix in this.srlMap.Keys)
				{
					if (filename.EndsWith(suffix))
					{
						srlMap = this.srlMap[suffix];
						break;
					}
				}
				if (srlMap == null)
				{
					log.Info("could not find SRL entries for file: " + file);
				}
			}
			try
			{
				// maybe print file name to stdout to get some feedback
				// could throw an IO exception if can't open for reading
				tr = TreeReaderFactory().NewTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(file), Encoding())));
				int sentIndex = 0;
				Tree pt;
				while ((pt = tr.ReadTree()) != null)
				{
					if (pt.Label() is IHasIndex)
					{
						// so we can trace where this tree came from
						IHasIndex hi = (IHasIndex)pt.Label();
						hi.SetDocID(file.GetName());
						hi.SetSentIndex(sentIndex);
					}
					if (srlMap == null)
					{
						parseTrees.Add(pt);
					}
					else
					{
						ICollection<string> srls = srlMap[sentIndex];
						//           pt.pennPrint();
						//           log.info(srls);
						parseTrees.Add(pt);
						if (srls.IsEmpty())
						{
						}
						else
						{
							//            parseTrees.add(pt);
							foreach (string srl in srls)
							{
								//              Tree t = pt.deepCopy();
								string[] bits = srl.Split("\\s+");
								int verbIndex = System.Convert.ToInt32(bits[0]);
								string lemma = bits[2].Split("\\.")[0];
								//              Tree verb = Trees.getTerminal(t, verbIndex);
								Tree verb = Edu.Stanford.Nlp.Trees.Trees.GetTerminal(pt, verbIndex);
								//              ((CoreLabel)verb.label()).set(SRLIDAnnotation.class, SRL_ID.REL);
								((CoreLabel)verb.Label()).Set(typeof(CoreAnnotations.CoNLLPredicateAnnotation), true);
								for (int i = 4; i < bits.Length; i++)
								{
									string arg = bits[i];
									string[] bits1;
									if (arg.IndexOf("ARGM") >= 0)
									{
										bits1 = arg.Split("-");
									}
									else
									{
										bits1 = arg.Split("-");
									}
									string locs = bits1[0];
									string argType = bits1[1];
									if (argType.Equals("rel"))
									{
										continue;
									}
									foreach (string loc in locs.Split("[*,]"))
									{
										bits1 = loc.Split(":");
										int term = System.Convert.ToInt32(bits1[0]);
										int height = System.Convert.ToInt32(bits1[1]);
										//                  Tree t1 = Trees.getPreTerminal(t, term);
										Tree t1 = Edu.Stanford.Nlp.Trees.Trees.GetPreTerminal(pt, term);
										for (int j = 0; j < height; j++)
										{
											//                    t1 = t1.parent(t);
											t1 = t1.Parent(pt);
										}
										IDictionary<int, string> roleMap = ((CoreLabel)t1.Label()).Get(typeof(CoreAnnotations.CoNLLSRLAnnotation));
										if (roleMap == null)
										{
											roleMap = Generics.NewHashMap();
											((CoreLabel)t1.Label()).Set(typeof(CoreAnnotations.CoNLLSRLAnnotation), roleMap);
										}
										roleMap[verbIndex] = argType;
									}
								}
							}
						}
					}
					//                  ((CoreLabel)t1.label()).set(SRLIDAnnotation.class, SRL_ID.ARG);
					//               for (Tree t1 : t) {
					//                 if (t1.isLeaf()) { continue; }
					//                 CoreLabel fl = (CoreLabel)t1.label();
					//                 if (fl.value() == null) { continue; }
					//                 if (!fl.has(SRLIDAnnotation.class)) {
					//                   boolean allNone = true;
					//                   for (Tree t2 : t1) {
					//                     SRL_ID s = ((CoreLabel)t2.label()).get(SRLIDAnnotation.class);
					//                     if (s == SRL_ID.ARG || s == SRL_ID.REL) {
					//                       allNone = false;
					//                       break;
					//                     }
					//                   }
					//                   if (allNone) {
					//                     fl.set(SRLIDAnnotation.class, SRL_ID.ALL_NO);
					//                   } else {
					//                     fl.set(SRLIDAnnotation.class, SRL_ID.NO);
					//                   }
					//                 }
					//               }
					//              parseTrees.add(t);
					sentIndex++;
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException("MemoryTreebank.processFile IOException in file " + file, e);
			}
			finally
			{
				IOUtils.CloseIgnoringExceptions(tr);
			}
		}

		/// <summary>Load a collection of parse trees from a Reader.</summary>
		/// <remarks>
		/// Load a collection of parse trees from a Reader.
		/// Each tree may optionally be encased in parens to allow for Penn
		/// Treebank style trees.
		/// </remarks>
		/// <param name="r">
		/// The reader to read trees from.  (If you want it buffered,
		/// you should already have buffered it!)
		/// </param>
		public void Load(Reader r)
		{
			Load(r, null);
		}

		/// <summary>Load a collection of parse trees from a Reader.</summary>
		/// <remarks>
		/// Load a collection of parse trees from a Reader.
		/// Each tree may optionally be encased in parens to allow for Penn
		/// Treebank style trees.
		/// </remarks>
		/// <param name="r">
		/// The reader to read trees from.  (If you want it buffered,
		/// you should already have buffered it!)
		/// </param>
		/// <param name="id">
		/// An ID for where these files come from (arbitrary, but
		/// something like a filename.  Can be <code>null</code> for none.
		/// </param>
		public void Load(Reader r, string id)
		{
			try
			{
				// could throw an IO exception?
				ITreeReader tr = TreeReaderFactory().NewTreeReader(r);
				int sentIndex = 0;
				for (Tree pt; (pt = tr.ReadTree()) != null; )
				{
					if (pt.Label() is IHasIndex)
					{
						// so we can trace where this tree came from
						IHasIndex hi = (IHasIndex)pt.Label();
						if (id != null)
						{
							hi.SetDocID(id);
						}
						hi.SetSentIndex(sentIndex);
					}
					parseTrees.Add(pt);
					sentIndex++;
				}
			}
			catch (IOException e)
			{
				log.Info("load IO Exception: " + e);
			}
		}

		/// <summary>Get a tree by index from the Treebank.</summary>
		/// <remarks>
		/// Get a tree by index from the Treebank.
		/// This operation isn't in the <code>Treebank</code> feature set, and
		/// so is only available with a <code>MemoryTreebank</code>, but is
		/// useful in allowing the latter to be used as a <code>List</code>.
		/// </remarks>
		/// <param name="i">The integer (counting from 0) index of the tree</param>
		/// <returns>A tree</returns>
		public Tree Get(int i)
		{
			return parseTrees[i];
		}

		/// <summary>Apply the TreeVisitor tp to all trees in the Treebank.</summary>
		/// <param name="tp">A class that implements the TreeVisitor interface</param>
		public override void Apply(ITreeVisitor tp)
		{
			foreach (Tree parseTree in parseTrees)
			{
				tp.VisitTree(parseTree);
			}
		}

		// or could do as Iterator but slower
		// Iterator iter = parseTrees.iterator();
		// while (iter.hasNext()) {
		//    tp.visitTree((Tree) iter.next());
		// }
		/// <summary>Return an Iterator over Trees in the Treebank.</summary>
		/// <returns>The iterator</returns>
		public override IEnumerator<Tree> GetEnumerator()
		{
			return parseTrees.GetEnumerator();
		}

		/// <summary>Returns the size of the Treebank.</summary>
		/// <remarks>
		/// Returns the size of the Treebank.
		/// Provides a more efficient implementation than the one for a
		/// generic <code>Treebank</code>
		/// </remarks>
		/// <returns>the number of trees in the Treebank</returns>
		public override int Count
		{
			get
			{
				return parseTrees.Count;
			}
		}

		// Extra stuff to implement List interface
		public void Add(int index, Tree element)
		{
			parseTrees.Add(index, element);
		}

		public override bool Add(Tree element)
		{
			return parseTrees.Add(element);
		}

		public bool AddAll<_T0>(int index, ICollection<_T0> c)
			where _T0 : Tree
		{
			return parseTrees.AddAll(index, c);
		}

		public int IndexOf(object o)
		{
			return parseTrees.IndexOf(o);
		}

		public int LastIndexOf(object o)
		{
			return parseTrees.LastIndexOf(o);
		}

		public Tree Remove(int index)
		{
			return parseTrees.Remove(index);
		}

		public Tree Set(int index, Tree element)
		{
			return parseTrees.Set(index, element);
		}

		public IListIterator<Tree> ListIterator()
		{
			return parseTrees.ListIterator();
		}

		public IListIterator<Tree> ListIterator(int index)
		{
			return parseTrees.ListIterator(index);
		}

		public IList<Tree> SubList(int fromIndex, int toIndex)
		{
			return parseTrees.SubList(fromIndex, toIndex);
		}

		/// <summary>
		/// Return a MemoryTreebank where each
		/// Tree in the current treebank has been transformed using the
		/// TreeTransformer.
		/// </summary>
		/// <remarks>
		/// Return a MemoryTreebank where each
		/// Tree in the current treebank has been transformed using the
		/// TreeTransformer.  This Treebank is unchanged (assuming that the
		/// TreeTransformer correctly doesn't change input Trees).
		/// </remarks>
		/// <param name="treeTrans">The TreeTransformer to use</param>
		public override Treebank Transform(ITreeTransformer treeTrans)
		{
			Treebank mtb = new Edu.Stanford.Nlp.Trees.MemoryTreebank(Count, TreeReaderFactory());
			foreach (Tree t in this)
			{
				mtb.Add(treeTrans.TransformTree(t));
			}
			return mtb;
		}

		/// <summary>Loads treebank grammar from first argument and prints it.</summary>
		/// <remarks>
		/// Loads treebank grammar from first argument and prints it.
		/// Just a demonstration of functionality. <br />
		/// <code>usage: java MemoryTreebank treebankFilesPath</code>
		/// </remarks>
		/// <param name="args">array of command-line arguments</param>
		public static void Main(string[] args)
		{
			Timing.StartTime();
			Treebank treebank = new Edu.Stanford.Nlp.Trees.MemoryTreebank(null);
			treebank.LoadPath(args[0]);
			Timing.EndTime();
			System.Console.Out.WriteLine(treebank);
		}
	}
}

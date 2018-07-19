using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A
	/// <c>Treebank</c>
	/// object provides access to a corpus of examples with
	/// given tree structures.
	/// This class now implements the Collection interface. However, it may offer
	/// less than the full power of the Collection interface: some Treebanks are
	/// read only, and so may throw the UnsupportedOperationException.
	/// </summary>
	/// <author>Christopher Manning</author>
	/// <author>Roger Levy (added encoding variable and method)</author>
	public abstract class Treebank : AbstractCollection<Tree>
	{
		/// <summary>
		/// Stores the
		/// <c>TreeReaderFactory</c>
		/// that will be used to
		/// create a
		/// <c>TreeReader</c>
		/// to process a file of trees.
		/// </summary>
		private ITreeReaderFactory trf;

		/// <summary>Stores the charset encoding of the Treebank on disk.</summary>
		private string encoding = TreebankLanguagePackConstants.DefaultEncoding;

		public const string DefaultTreeFileSuffix = "mrg";

		/// <summary>Create a new Treebank (using a LabeledScoredTreeReaderFactory).</summary>
		public Treebank()
			: this(new LabeledScoredTreeReaderFactory())
		{
		}

		/// <summary>Create a new Treebank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <c>TreeReader</c>
		/// </param>
		public Treebank(ITreeReaderFactory trf)
		{
			this.trf = trf;
		}

		/// <summary>Create a new Treebank.</summary>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <c>TreeReader</c>
		/// </param>
		/// <param name="encoding">The charset encoding to use for treebank file decoding</param>
		public Treebank(ITreeReaderFactory trf, string encoding)
		{
			this.trf = trf;
			this.encoding = encoding;
		}

		/// <summary>Create a new Treebank.</summary>
		/// <param name="initialCapacity">
		/// The initial size of the underlying Collection,
		/// (if a Collection-based storage mechanism is being provided)
		/// </param>
		public Treebank(int initialCapacity)
			: this(initialCapacity, new LabeledScoredTreeReaderFactory())
		{
		}

		/// <summary>Create a new Treebank.</summary>
		/// <param name="initialCapacity">
		/// The initial size of the underlying Collection,
		/// (if a Collection-based storage mechanism is being provided)
		/// </param>
		/// <param name="trf">
		/// the factory class to be called to create a new
		/// <c>TreeReader</c>
		/// </param>
		public Treebank(int initialCapacity, ITreeReaderFactory trf)
		{
			this.trf = trf;
		}

		/// <summary>
		/// Get the
		/// <c>TreeReaderFactory</c>
		/// for a
		/// <c>Treebank</c>
		/// --
		/// this method is provided in order to make the
		/// <c>TreeReaderFactory</c>
		/// available to subclasses.
		/// </summary>
		/// <returns>The TreeReaderFactory</returns>
		protected internal virtual ITreeReaderFactory TreeReaderFactory()
		{
			return trf;
		}

		/// <summary>Returns the encoding in use for treebank file bytestream access.</summary>
		/// <returns>The encoding in use for treebank file bytestream access.</returns>
		public virtual string Encoding()
		{
			return encoding;
		}

		/// <summary>
		/// Empty a
		/// <c>Treebank</c>
		/// .
		/// </summary>
		public abstract override void Clear();

		/// <summary>Load a sequence of trees from given directory and its subdirectories.</summary>
		/// <remarks>
		/// Load a sequence of trees from given directory and its subdirectories.
		/// Trees should reside in files with the suffix "mrg".
		/// Or: load a single file with the given pathName (including extension)
		/// </remarks>
		/// <param name="pathName">file or directory name</param>
		public virtual void LoadPath(string pathName)
		{
			LoadPath(new File(pathName));
		}

		/// <summary>Load a sequence of trees from given file or directory and its subdirectories.</summary>
		/// <remarks>
		/// Load a sequence of trees from given file or directory and its subdirectories.
		/// Either this loads from a directory (tree) and
		/// trees must reside in files with the suffix "mrg" (this is an English
		/// Penn Treebank holdover!),
		/// or it loads a single file with the given path (including extension)
		/// </remarks>
		/// <param name="path">File specification</param>
		public virtual void LoadPath(File path)
		{
			LoadPath(path, DefaultTreeFileSuffix, true);
		}

		/// <summary>Load trees from given directory.</summary>
		/// <param name="pathName">File or directory name</param>
		/// <param name="suffix">
		/// Extension of files to load: If
		/// <paramref name="pathName"/>
		/// is a directory, then, if this is
		/// non-
		/// <see langword="null"/>
		/// , all and only files ending in "." followed
		/// by this extension will be loaded; if it is
		/// <see langword="null"/>
		/// ,
		/// all files in directories will be loaded.  If
		/// <paramref name="pathName"/>
		/// is not a directory, this parameter is ignored.
		/// </param>
		/// <param name="recursively">descend into subdirectories as well</param>
		public virtual void LoadPath(string pathName, string suffix, bool recursively)
		{
			LoadPath(new File(pathName), new ExtensionFileFilter(suffix, recursively));
		}

		/// <summary>Load trees from given directory.</summary>
		/// <param name="path">file or directory to load from</param>
		/// <param name="suffix">suffix of files to load</param>
		/// <param name="recursively">descend into subdirectories as well</param>
		public virtual void LoadPath(File path, string suffix, bool recursively)
		{
			LoadPath(path, new ExtensionFileFilter(suffix, recursively));
		}

		/// <summary>
		/// Load a sequence of trees from given directory and its subdirectories
		/// which match the file filter.
		/// </summary>
		/// <remarks>
		/// Load a sequence of trees from given directory and its subdirectories
		/// which match the file filter.
		/// Or: load a single file with the given pathName (including extension)
		/// </remarks>
		/// <param name="pathName">file or directory name</param>
		/// <param name="filt">A filter used to determine which files match</param>
		public virtual void LoadPath(string pathName, IFileFilter filt)
		{
			LoadPath(new File(pathName), filt);
		}

		/// <summary>Load trees from given path specification.</summary>
		/// <param name="path">file or directory to load from</param>
		/// <param name="filt">a FilenameFilter of files to load</param>
		public abstract void LoadPath(File path, IFileFilter filt);

		/// <summary>Apply a TreeVisitor to each tree in the Treebank.</summary>
		/// <remarks>
		/// Apply a TreeVisitor to each tree in the Treebank.
		/// For all current implementations of Treebank, this is the fastest
		/// way to traverse all the trees in the Treebank.
		/// </remarks>
		/// <param name="tp">The TreeVisitor to be applied</param>
		public abstract void Apply(ITreeVisitor tp);

		/// <summary>
		/// Return a Treebank (actually a TransformingTreebank) where each
		/// Tree in the current treebank has been transformed using the
		/// TreeTransformer.
		/// </summary>
		/// <remarks>
		/// Return a Treebank (actually a TransformingTreebank) where each
		/// Tree in the current treebank has been transformed using the
		/// TreeTransformer.  The argument Treebank is unchanged (assuming
		/// that the TreeTransformer correctly doesn't change input Trees).
		/// </remarks>
		/// <param name="treeTrans">The TreeTransformer to use</param>
		/// <returns>
		/// A Treebank (actually a TransformingTreebank) where each
		/// Tree in the current treebank has been transformed using the
		/// TreeTransformer.
		/// </returns>
		public virtual Edu.Stanford.Nlp.Trees.Treebank Transform(ITreeTransformer treeTrans)
		{
			return new TransformingTreebank(this, treeTrans);
		}

		/// <summary>Return the whole treebank as a series of big bracketed lists.</summary>
		/// <remarks>
		/// Return the whole treebank as a series of big bracketed lists.
		/// Calling this is a really bad idea if your treebank is large.
		/// </remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			Apply(null);
			return sb.ToString();
		}

		private sealed class CounterTreeProcessor : ITreeVisitor
		{
			internal int i;

			// = 0;
			public void VisitTree(Tree t)
			{
				i++;
			}

			public int Total()
			{
				return i;
			}
		}

		/// <summary>Returns the size of the Treebank.</summary>
		/// <returns>size How many trees are in the treebank</returns>
		public override int Count
		{
			get
			{
				Treebank.CounterTreeProcessor counter = new Treebank.CounterTreeProcessor();
				Apply(counter);
				return counter.Total();
			}
		}

		/// <summary>
		/// Divide a Treebank into 3, by taking every 9th sentence for the dev
		/// set and every 10th for the test set.
		/// </summary>
		/// <remarks>
		/// Divide a Treebank into 3, by taking every 9th sentence for the dev
		/// set and every 10th for the test set.  Penn people do this.
		/// </remarks>
		public virtual void Decimate(TextWriter trainW, TextWriter devW, TextWriter testW)
		{
			PrintWriter trainPW = new PrintWriter(trainW, true);
			PrintWriter devPW = new PrintWriter(devW, true);
			PrintWriter testPW = new PrintWriter(testW, true);
			int i = 0;
			foreach (Tree t in this)
			{
				if (i == 8)
				{
					t.PennPrint(devPW);
				}
				else
				{
					if (i == 9)
					{
						t.PennPrint(testPW);
					}
					else
					{
						t.PennPrint(trainPW);
					}
				}
				i = (i + 1) % 10;
			}
		}

		/// <summary>
		/// Return various statistics about the treebank (number of sentences,
		/// words, tag set, etc.).
		/// </summary>
		/// <returns>
		/// A String with various statistics about the treebank (number of
		/// sentences, words, tag set, etc.).
		/// </returns>
		public virtual string TextualSummary()
		{
			return TextualSummary(null);
		}

		/// <summary>
		/// Return various statistics about the treebank (number of sentences,
		/// words, tag set, etc.).
		/// </summary>
		/// <param name="tlp">
		/// The TreebankLanguagePack used to determine punctuation and an
		/// appropriate character encoding
		/// </param>
		/// <returns>A big string for human consumption describing the treebank</returns>
		public virtual string TextualSummary(ITreebankLanguagePack tlp)
		{
			int numTrees = 0;
			int numTreesLE40 = 0;
			int numNonUnaryRoots = 0;
			Tree nonUnaryEg = null;
			ClassicCounter<Tree> nonUnaries = new ClassicCounter<Tree>();
			ClassicCounter<string> roots = new ClassicCounter<string>();
			ClassicCounter<string> starts = new ClassicCounter<string>();
			ClassicCounter<string> puncts = new ClassicCounter<string>();
			int numUnenclosedLeaves = 0;
			int numLeaves = 0;
			int numNonPhrasal = 0;
			int numPreTerminalWithMultipleChildren = 0;
			int numWords = 0;
			int numTags = 0;
			int shortestSentence = int.MaxValue;
			int longestSentence = 0;
			int numNullLabel = 0;
			ICollection<string> words = Generics.NewHashSet();
			ClassicCounter<string> tags = new ClassicCounter<string>();
			ClassicCounter<string> cats = new ClassicCounter<string>();
			Tree leafEg = null;
			Tree preTerminalMultipleChildrenEg = null;
			Tree nullLabelEg = null;
			Tree rootRewritesAsTaggedWordEg = null;
			foreach (Tree t in this)
			{
				roots.IncrementCount(t.Value());
				numTrees++;
				int leng = t.Yield().Count;
				if (leng <= 40)
				{
					numTreesLE40++;
				}
				if (leng < shortestSentence)
				{
					shortestSentence = leng;
				}
				if (leng > longestSentence)
				{
					longestSentence = leng;
				}
				if (t.NumChildren() > 1)
				{
					if (numNonUnaryRoots == 0)
					{
						nonUnaryEg = t;
					}
					if (numNonUnaryRoots < 100)
					{
						nonUnaries.IncrementCount(t.LocalTree());
					}
					numNonUnaryRoots++;
				}
				else
				{
					if (t.IsLeaf())
					{
						numUnenclosedLeaves++;
					}
					else
					{
						Tree t2 = t.FirstChild();
						if (t2.IsLeaf())
						{
							numLeaves++;
							leafEg = t;
						}
						else
						{
							if (t2.IsPreTerminal())
							{
								if (numNonPhrasal == 0)
								{
									rootRewritesAsTaggedWordEg = t;
								}
								numNonPhrasal++;
							}
						}
						starts.IncrementCount(t2.Value());
					}
				}
				foreach (Tree subtree in t)
				{
					ILabel lab = subtree.Label();
					if (lab == null || lab.Value() == null || lab.Value().IsEmpty())
					{
						if (numNullLabel == 0)
						{
							nullLabelEg = subtree;
						}
						numNullLabel++;
						if (lab == null)
						{
							subtree.SetLabel(new StringLabel(string.Empty));
						}
						else
						{
							if (lab.Value() == null)
							{
								subtree.Label().SetValue(string.Empty);
							}
						}
					}
					if (subtree.IsLeaf())
					{
						numWords++;
						words.Add(subtree.Value());
					}
					else
					{
						if (subtree.IsPreTerminal())
						{
							numTags++;
							tags.IncrementCount(subtree.Value());
							if (tlp != null && tlp.IsPunctuationTag(subtree.Value()))
							{
								puncts.IncrementCount(subtree.FirstChild().Value());
							}
						}
						else
						{
							if (subtree.IsPhrasal())
							{
								bool hasLeafChild = false;
								foreach (Tree kt in subtree.Children())
								{
									if (kt.IsLeaf())
									{
										hasLeafChild = true;
									}
								}
								if (hasLeafChild)
								{
									numPreTerminalWithMultipleChildren++;
									if (preTerminalMultipleChildrenEg == null)
									{
										preTerminalMultipleChildrenEg = subtree;
									}
								}
								cats.IncrementCount(subtree.Value());
							}
							else
							{
								throw new InvalidOperationException("Treebank: Bad tree in treebank!: " + subtree);
							}
						}
					}
				}
			}
			StringWriter sw = new StringWriter(2000);
			PrintWriter pw = new PrintWriter(sw);
			NumberFormat nf = NumberFormat.GetNumberInstance();
			nf.SetMaximumFractionDigits(0);
			pw.Println("Treebank has " + numTrees + " trees (" + numTreesLE40 + " of length <= 40) and " + numWords + " words (tokens)");
			if (numTrees > 0)
			{
				if (numTags != numWords)
				{
					pw.Println("  Warning! numTags differs and is " + numTags);
				}
				if (roots.Size() == 1)
				{
					string root = (string)Sharpen.Collections.ToArray(roots.KeySet())[0];
					pw.Println("  The root category is: " + root);
				}
				else
				{
					pw.Println("  Warning! " + roots.Size() + " different roots in treebank: " + Counters.ToString(roots, nf));
				}
				if (numNonUnaryRoots > 0)
				{
					pw.Print("  Warning! " + numNonUnaryRoots + " trees without unary initial rewrite.  ");
					if (numNonUnaryRoots > 100)
					{
						pw.Print("First 100 ");
					}
					pw.Println("Rewrites: " + Counters.ToString(nonUnaries, nf));
					pw.Println("    Example: " + nonUnaryEg);
				}
				if (numUnenclosedLeaves > 0 || numLeaves > 0 || numNonPhrasal > 0)
				{
					pw.Println("  Warning! Non-phrasal trees: " + numUnenclosedLeaves + " bare leaves; " + numLeaves + " root rewrites as leaf; and " + numNonPhrasal + " root rewrites as tagged word");
					if (numLeaves > 0)
					{
						pw.Println("  Example bad root rewrites as leaf: " + leafEg);
					}
					if (numNonPhrasal > 0)
					{
						pw.Println("  Example bad root rewrites as tagged word: " + rootRewritesAsTaggedWordEg);
					}
				}
				if (numNullLabel > 0)
				{
					pw.Println("  Warning!  " + numNullLabel + " tree nodes with null or empty string labels, e.g.:");
					pw.Println("    " + nullLabelEg);
				}
				if (numPreTerminalWithMultipleChildren > 0)
				{
					pw.Println("  Warning! " + numPreTerminalWithMultipleChildren + " preterminal nodes with multiple children.");
					pw.Println("    Example: " + preTerminalMultipleChildrenEg);
				}
				pw.Println("  Sentences range from " + shortestSentence + " to " + longestSentence + " words, with an average length of " + (((numWords * 100) / numTrees) / 100.0) + " words.");
				pw.Println("  " + cats.Size() + " phrasal category types, " + tags.Size() + " tag types, and " + words.Count + " word types");
				string[] empties = new string[] { "*", "0", "*T*", "*RNR*", "*U*", "*?*", "*EXP*", "*ICH*", "*NOT*", "*PPA*", "*OP*", "*pro*", "*PRO*" };
				// What a dopey choice using 0 as an empty element name!!
				// The problem with the below is that words aren't turned into a basic
				// category, but empties commonly are indexed....  Would need to look
				// for them with a suffix of -[0-9]+
				ICollection<string> knownEmpties = Generics.NewHashSet(Arrays.AsList(empties));
				ICollection<string> emptiesIntersection = Sets.Intersection(words, knownEmpties);
				if (!emptiesIntersection.IsEmpty())
				{
					pw.Println("  Caution! " + emptiesIntersection.Count + " word types are known empty elements: " + emptiesIntersection);
				}
				ICollection<string> joint = Sets.Intersection(cats.KeySet(), tags.KeySet());
				if (!joint.IsEmpty())
				{
					pw.Println("  Warning! " + joint.Count + " items are tags and categories: " + joint);
				}
				foreach (string cat in cats.KeySet())
				{
					if (cat != null && cat.Contains("@"))
					{
						pw.Println("  Warning!!  Stanford Parser does not work with categories containing '@' like: " + cat);
						break;
					}
				}
				foreach (string cat_1 in tags.KeySet())
				{
					if (cat_1 != null && cat_1.Contains("@"))
					{
						pw.Println("  Warning!!  Stanford Parser does not work with tags containing '@' like: " + cat_1);
						break;
					}
				}
				pw.Println("    Cats: " + Counters.ToString(cats, nf));
				pw.Println("    Tags: " + Counters.ToString(tags, nf));
				pw.Println("    " + starts.Size() + " start categories: " + Counters.ToString(starts, nf));
				if (!puncts.IsEmpty())
				{
					pw.Println("    Puncts: " + Counters.ToString(puncts, nf));
				}
			}
			return sw.ToString();
		}

		/// <summary>This operation isn't supported for a Treebank.</summary>
		/// <remarks>This operation isn't supported for a Treebank.  Tell them immediately.</remarks>
		public override bool Remove(object o)
		{
			throw new NotSupportedException("Treebank is read-only");
		}
	}
}

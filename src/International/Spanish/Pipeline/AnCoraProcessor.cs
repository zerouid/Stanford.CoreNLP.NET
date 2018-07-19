using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Spanish;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Concurrent;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.International.Spanish.Pipeline
{
	/// <summary>
	/// A tool which accepts raw AnCora-3.0 Spanish XML files and produces
	/// normalized / pre-processed PTB-style treebanks for use with CoreNLP
	/// tools.
	/// </summary>
	/// <remarks>
	/// A tool which accepts raw AnCora-3.0 Spanish XML files and produces
	/// normalized / pre-processed PTB-style treebanks for use with CoreNLP
	/// tools.
	/// This is a substitute for an awkward and complicated string of
	/// command-line invocations. The produced corpus is the standard
	/// treebank which has been used to train the CoreNLP Spanish models.
	/// The preprocessing steps performed here include:
	/// - Expansion and automatic tagging of multi-word tokens (see
	/// <see cref="MultiWordPreprocessor"/>
	/// ,
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishTreeNormalizer.NormalizeForMultiWord(Edu.Stanford.Nlp.Trees.Tree, Edu.Stanford.Nlp.Trees.ITreeFactory)"/>
	/// - Heuristic parsing of expanded multi-word tokens (see
	/// <see cref="MultiWordTreeExpander"/>
	/// - Splitting of elided forms (<em>al</em>, <em>del</em>,
	/// <em>conmigo</em>, etc.) and clitic pronouns from verb forms (see
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishTreeNormalizer.ExpandElisions(Edu.Stanford.Nlp.Trees.Tree)"/>
	/// ,
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishTreeNormalizer.ExpandCliticPronouns(Edu.Stanford.Nlp.Trees.Tree)"/>
	/// - Miscellaneous cleanup of parse trees, spelling fixes, parsing
	/// error corrections (see
	/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishTreeNormalizer"/>
	/// )
	/// Apart from raw corpus data, this processor depends upon unigram
	/// part-of-speech tag data. If not provided explicitly to the
	/// processor, the data will be collected from the given files. (You can
	/// pre-compute POS data from AnCora XML using
	/// <see cref="AnCoraPOSStats"/>
	/// .)
	/// For invocation options, execute the class with no arguments.
	/// </remarks>
	/// <author>Jon Gauthier</author>
	public class AnCoraProcessor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.International.Spanish.Pipeline.AnCoraProcessor));

		private readonly IList<File> inputFiles;

		private readonly Properties options;

		private readonly TwoDimensionalCounter<string, string> unigramTagger;

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public AnCoraProcessor(IList<File> inputFiles, Properties options)
		{
			this.inputFiles = inputFiles;
			this.options = options;
			if (options.Contains("unigramTagger"))
			{
				ObjectInputStream ois = new ObjectInputStream(new FileInputStream(options.GetProperty("unigramTagger")));
				unigramTagger = (TwoDimensionalCounter<string, string>)ois.ReadObject();
			}
			else
			{
				unigramTagger = new TwoDimensionalCounter<string, string>();
			}
		}

		/// <exception cref="System.Exception"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		public virtual IList<Tree> Process()
		{
			// Each of the following subroutines are multithreaded; there is a bottleneck between the
			// method calls
			IList<Tree> trees = LoadTrees();
			trees = FixMultiWordTokens(trees);
			return trees;
		}

		/// <summary>
		/// Use
		/// <see cref="Edu.Stanford.Nlp.Trees.International.Spanish.SpanishXMLTreeReader"/>
		/// to load the trees from the provided files,
		/// and begin collecting some statistics to be used in later MWE cleanup.
		/// NB: Much of the important cleanup happens implicitly here; the XML tree reader triggers the
		/// tree normalization routine.
		/// </summary>
		/// <exception cref="System.Exception"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		private IList<Tree> LoadTrees()
		{
			bool ner = PropertiesUtils.GetBool(options, "ner", false);
			string encoding = new SpanishTreebankLanguagePack().GetEncoding();
			SpanishXMLTreeReaderFactory trf = new SpanishXMLTreeReaderFactory(true, true, ner, false);
			IList<Tree> trees = new List<Tree>();
			foreach (File file in inputFiles)
			{
				Pair<TwoDimensionalCounter<string, string>, IList<Tree>> ret = ProcessTreeFile(file, trf, encoding);
				Counters.AddInPlace(unigramTagger, ret.First());
				Sharpen.Collections.AddAll(trees, ret.Second());
			}
			return trees;
		}

		/// <summary>Processes a single file containing AnCora XML trees.</summary>
		/// <remarks>
		/// Processes a single file containing AnCora XML trees. Returns MWE statistics for the trees in
		/// the file and the actual parsed trees.
		/// </remarks>
		private static Pair<TwoDimensionalCounter<string, string>, IList<Tree>> ProcessTreeFile(File file, SpanishXMLTreeReaderFactory trf, string encoding)
		{
			TwoDimensionalCounter<string, string> tagger = new TwoDimensionalCounter<string, string>();
			try
			{
				Reader @in = new BufferedReader(new InputStreamReader(new FileInputStream(file), encoding));
				ITreeReader tr = trf.NewTreeReader(file.GetPath(), @in);
				IList<Tree> trees = new List<Tree>();
				Tree t;
				Tree splitPoint;
				while ((t = tr.ReadTree()) != null)
				{
					do
					{
						// We may need to split the current tree into multiple parts.
						// (If not, a call to `split` with a `null` split-point is a
						// no-op
						splitPoint = FindSplitPoint(t);
						Pair<Tree, Tree> split = Split(t, splitPoint);
						Tree toAdd = split.First();
						t = split.Second();
						trees.Add(toAdd);
						UpdateTagger(tagger, toAdd);
					}
					while (splitPoint != null);
				}
				tr.Close();
				return new Pair<TwoDimensionalCounter<string, string>, IList<Tree>>(tagger, trees);
			}
			catch (IOException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				return null;
			}
		}

		private static void UpdateTagger(TwoDimensionalCounter<string, string> tagger, Tree t)
		{
			IList<CoreLabel> yield = t.TaggedLabeledYield();
			foreach (CoreLabel label in yield)
			{
				if (label.Tag().Equals(SpanishTreeNormalizer.MwTag))
				{
					continue;
				}
				tagger.IncrementCount(label.Word(), label.Tag());
			}
		}

		private static TreeNormalizer splittingNormalizer = new SpanishSplitTreeNormalizer();

		private static ITreeFactory splittingTreeFactory = new LabeledScoredTreeFactory();

		/// <summary>
		/// Split the given tree based on a split point such that the
		/// terminals leading up to the split point are in the left returned
		/// tree and those following the split point are in the left returned
		/// tree.
		/// </summary>
		/// <remarks>
		/// Split the given tree based on a split point such that the
		/// terminals leading up to the split point are in the left returned
		/// tree and those following the split point are in the left returned
		/// tree.
		/// AnCora contains a nontrivial amount of trees with multiple
		/// sentences in them. This method is used to break apart these
		/// sentences into separate trees.
		/// </remarks>
		/// <param name="t">
		/// Tree from which to extract a subtree. This may be
		/// modified during processing.
		/// </param>
		/// <param name="splitPoint">
		/// Point up to which to extract. If
		/// <see langword="null"/>
		/// ,
		/// <paramref name="t"/>
		/// is returned unchanged in the place of
		/// the right tree.
		/// </param>
		/// <returns>
		/// A pair where the left tree contains every terminal leading
		/// up to and including
		/// <paramref name="splitPoint"/>
		/// and the right tree
		/// contains every terminal following
		/// <paramref name="splitPoint"/>
		/// .
		/// Both trees may be normalized before return.
		/// </returns>
		internal static Pair<Tree, Tree> Split(Tree t, Tree splitPoint)
		{
			if (splitPoint == null)
			{
				return new Pair<Tree, Tree>(t, null);
			}
			Tree left = t.Prune(new AnCoraProcessor.LeftOfFilter(splitPoint, t));
			Tree right = t.Prune(new AnCoraProcessor.RightOfExclusiveFilter(splitPoint, t));
			left = splittingNormalizer.NormalizeWholeTree(left, splittingTreeFactory);
			right = splittingNormalizer.NormalizeWholeTree(right, splittingTreeFactory);
			return new Pair<Tree, Tree>(left, right);
		}

		/// <summary>
		/// Accepts any tree node to the left of the provided node (or the
		/// provided node itself).
		/// </summary>
		[System.Serializable]
		private class LeftOfFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = -5146948439247427344L;

			private Tree reference;

			private Tree root;

			/// <param name="reference">
			/// Node to which nodes provided to this filter
			/// should be compared
			/// </param>
			/// <param name="root">
			/// Root of the tree which contains the reference node
			/// and all nodes which may be provided to the filter
			/// </param>
			private LeftOfFilter(Tree reference, Tree root)
			{
				this.reference = reference;
				this.root = root;
			}

			public virtual bool Test(Tree obj)
			{
				if (obj == reference || obj.Dominates(reference) || reference.Dominates(obj))
				{
					return true;
				}
				Tree rightmostDescendant = GetRightmostDescendant(obj);
				return Edu.Stanford.Nlp.Trees.Trees.RightEdge(rightmostDescendant, root) <= Edu.Stanford.Nlp.Trees.Trees.LeftEdge(reference, root);
			}

			private Tree GetRightmostDescendant(Tree t)
			{
				if (t.IsLeaf())
				{
					return t;
				}
				else
				{
					return GetRightmostDescendant(t.Children()[t.Children().Length - 1]);
				}
			}
		}

		/// <summary>Accepts any tree node to the right of the provided node.</summary>
		[System.Serializable]
		private class RightOfExclusiveFilter : IPredicate<Tree>
		{
			private const long serialVersionUID = 8283161954004080591L;

			private Tree root;

			private Tree firstToKeep;

			/// <param name="reference">
			/// Node to which nodes provided to this filter
			/// should be compared
			/// </param>
			/// <param name="root">
			/// Root of the tree which contains the reference node
			/// and all nodes which may be provided to the filter
			/// </param>
			private RightOfExclusiveFilter(Tree reference, Tree root)
			{
				// This should be the leftmost terminal node of the filtered tree
				this.root = root;
				firstToKeep = GetFollowingTerminal(reference, root);
			}

			public virtual bool Test(Tree obj)
			{
				if (obj.Dominates(firstToKeep))
				{
					return true;
				}
				Tree leftmostDescendant = GetLeftmostDescendant(obj);
				return Edu.Stanford.Nlp.Trees.Trees.RightEdge(leftmostDescendant, root) > Edu.Stanford.Nlp.Trees.Trees.LeftEdge(firstToKeep, root);
			}

			/// <summary>Get the terminal node which immediately follows the given node.</summary>
			private Tree GetFollowingTerminal(Tree terminal, Tree root)
			{
				Tree sibling = GetRightSiblingOrRightAncestor(terminal, root);
				if (sibling == null)
				{
					return null;
				}
				return GetLeftmostDescendant(sibling);
			}

			/// <summary>
			/// Get the right sibling of the given node, or some node which is
			/// the right sibling of an ancestor of the given node.
			/// </summary>
			/// <remarks>
			/// Get the right sibling of the given node, or some node which is
			/// the right sibling of an ancestor of the given node.
			/// If no such node can be found, this method returns
			/// <see langword="null"/>
			/// .
			/// </remarks>
			private Tree GetRightSiblingOrRightAncestor(Tree t, Tree root)
			{
				Tree parent = t.Parent(root);
				if (parent == null)
				{
					return null;
				}
				int idxWithinParent = parent.ObjectIndexOf(t);
				if (idxWithinParent < parent.NumChildren() - 1)
				{
					// Easy case: just return the immediate right sibling
					return parent.GetChild(idxWithinParent + 1);
				}
				return GetRightSiblingOrRightAncestor(parent, root);
			}

			private Tree GetLeftmostDescendant(Tree t)
			{
				if (t.IsLeaf())
				{
					return t;
				}
				else
				{
					return GetLeftmostDescendant(t.Children()[0]);
				}
			}
		}

		/// <summary>
		/// Matches a point in the AnCora corpus which is the delimiter
		/// between two sentences.
		/// </summary>
		/// <seealso>
		/// 
		/// <see cref="Split(Edu.Stanford.Nlp.Trees.Tree, Edu.Stanford.Nlp.Trees.Tree)"/>
		/// </seealso>
		private static readonly TregexPattern pSplitPoint = TregexPattern.Compile("fp $+ /^[^f]/ > S|sentence");

		/// <summary>
		/// Find the next point (preterminal) at which the given tree should
		/// be split.
		/// </summary>
		/// <param name="t"/>
		/// <returns>
		/// The endpoint of a subtree which should be extracted, or
		/// <see langword="null"/>
		/// if there are no subtrees which need to be
		/// extracted.
		/// </returns>
		internal static Tree FindSplitPoint(Tree t)
		{
			TregexMatcher m = pSplitPoint.Matcher(t);
			if (m.Find())
			{
				return m.GetMatch();
			}
			return null;
		}

		private class MultiWordProcessor : IThreadsafeProcessor<ICollection<Tree>, ICollection<Tree>>
		{
			private readonly TreeNormalizer tn;

			private readonly IFactory<TreeNormalizer> tnf;

			private readonly ITreeFactory tf;

			private readonly bool ner;

			public MultiWordProcessor(AnCoraProcessor _enclosing, IFactory<TreeNormalizer> tnf, ITreeFactory tf, bool ner)
			{
				this._enclosing = _enclosing;
				// NB: TreeNormalizer is not thread-safe, and so we need to accept + store a
				// TreeNormalizer factory instead
				this.tnf = tnf;
				this.tn = tnf.Create();
				this.tf = tf;
				this.ner = ner;
			}

			public virtual ICollection<Tree> Process(ICollection<Tree> coll)
			{
				IList<Tree> ret = new List<Tree>();
				// Apparently TsurgeonPatterns are not thread safe
				MultiWordTreeExpander expander = new MultiWordTreeExpander();
				foreach (Tree t in coll)
				{
					// Begin with basic POS / phrasal category inference
					MultiWordPreprocessor.TraverseAndFix(t, null, this._enclosing.unigramTagger, this.ner);
					// Now "decompress" further the expanded trees formed by multiword token splitting
					t = expander.ExpandPhrases(t, this.tn, this.tf);
					t = this.tn.NormalizeWholeTree(t, this.tf);
					ret.Add(t);
				}
				return ret;
			}

			public virtual IThreadsafeProcessor<ICollection<Tree>, ICollection<Tree>> NewInstance()
			{
				return new AnCoraProcessor.MultiWordProcessor(this, this.tnf, this.tf, this.ner);
			}

			private readonly AnCoraProcessor _enclosing;
		}

		/// <summary>
		/// Fix tree structure, phrasal categories and part-of-speech labels in newly expanded
		/// multi-word tokens.
		/// </summary>
		/// <exception cref="System.Exception"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		private IList<Tree> FixMultiWordTokens(IList<Tree> trees)
		{
			bool ner = PropertiesUtils.GetBool(options, "ner", false);
			// Shared resources
			IFactory<TreeNormalizer> tnf = new _IFactory_389();
			ITreeFactory tf = new LabeledScoredTreeFactory();
			IThreadsafeProcessor<ICollection<Tree>, ICollection<Tree>> processor = new AnCoraProcessor.MultiWordProcessor(this, tnf, tf, ner);
			int availableProcessors = Runtime.GetRuntime().AvailableProcessors();
			MulticoreWrapper<ICollection<Tree>, ICollection<Tree>> wrapper = new MulticoreWrapper<ICollection<Tree>, ICollection<Tree>>(availableProcessors, processor, false);
			// Chunk our work so that parallelization is actually worth it
			int numChunks = availableProcessors * 20;
			IList<IList<Tree>> chunked = CollectionUtils.PartitionIntoFolds(trees, numChunks);
			IList<Tree> ret = new List<Tree>();
			foreach (ICollection<Tree> coll in chunked)
			{
				wrapper.Put(coll);
				while (wrapper.Peek())
				{
					Sharpen.Collections.AddAll(ret, wrapper.Poll());
				}
			}
			wrapper.Join();
			while (wrapper.Peek())
			{
				Sharpen.Collections.AddAll(ret, wrapper.Poll());
			}
			return ret;
		}

		private sealed class _IFactory_389 : IFactory<TreeNormalizer>
		{
			public _IFactory_389()
			{
			}

			public TreeNormalizer Create()
			{
				return new SpanishTreeNormalizer(true, false, false);
			}
		}

		private static readonly string usage = string.Format("Usage: java %s [OPTIONS] file(s)%n%n", typeof(AnCoraProcessor).FullName) + "Options:\n" + "    -unigramTagger <tagger_path>: Path to a serialized `TwoDimensionalCounter` which\n" + "        should be used for unigram tagging in multi-word token expansion. If this option\n"
			 + "        is not provided, a unigram tagger will be built from the provided corpus data.\n" + "        (This option is useful if you are processing splits of the corpus separately but\n" + "        want each step to benefit from a complete tagger.)\n"
			 + "    -ner: Add NER-specific information to trees\n";

		private static readonly IDictionary<string, int> argOptionDefs = new Dictionary<string, int>();

		static AnCoraProcessor()
		{
			argOptionDefs["unigramTagger"] = 1;
			argOptionDefs["ner"] = 0;
		}

		/// <exception cref="System.Exception"/>
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="Java.Util.Concurrent.ExecutionException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				log.Info(usage);
			}
			Properties options = StringUtils.ArgsToProperties(args, argOptionDefs);
			string[] remainingArgs = options.GetProperty(string.Empty).Split(" ");
			IList<File> fileList = new List<File>();
			foreach (string arg in remainingArgs)
			{
				fileList.Add(new File(arg));
			}
			AnCoraProcessor processor = new AnCoraProcessor(fileList, options);
			IList<Tree> trees = processor.Process();
			foreach (Tree t in trees)
			{
				System.Console.Out.WriteLine(t);
			}
		}
	}
}

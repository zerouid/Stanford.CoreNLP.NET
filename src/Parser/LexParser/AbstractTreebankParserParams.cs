using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Metrics;
using Edu.Stanford.Nlp.Parser.Tools;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;




namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// An abstract class providing a common method base from which to
	/// complete a
	/// <c>TreebankLangParserParams</c>
	/// implementing class.
	/// <p/>
	/// With some extending classes you'll want to have access to special
	/// attributes of the corresponding TreebankLanguagePack while taking
	/// advantage of this class's code for making the TreebankLanguagePack
	/// accessible.  A good way to do this is to pass a new instance of the
	/// appropriate TreebankLanguagePack into this class's constructor,
	/// then get it back later on by casting a call to
	/// treebankLanguagePack().  See ChineseTreebankParserParams for an
	/// example.
	/// </summary>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public abstract class AbstractTreebankParserParams : ITreebankLangParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.AbstractTreebankParserParams));

		/// <summary>
		/// If true, then evaluation is over grammatical functions as well as the labels
		/// If false, then grammatical functions are stripped for evaluation.
		/// </summary>
		/// <remarks>
		/// If true, then evaluation is over grammatical functions as well as the labels
		/// If false, then grammatical functions are stripped for evaluation.  This really
		/// only makes sense if you've trained with grammatical functions but want to evaluate without them.
		/// </remarks>
		protected internal bool evalGF = true;

		/// <summary>
		/// The job of this class is to remove subcategorizations from
		/// tag and category nodes, so as to put a tree in a suitable
		/// state for evaluation.
		/// </summary>
		/// <remarks>
		/// The job of this class is to remove subcategorizations from
		/// tag and category nodes, so as to put a tree in a suitable
		/// state for evaluation.  Providing the TreebankLanguagePack
		/// is defined correctly, this should work for any language.
		/// </remarks>
		protected internal class SubcategoryStripper : ITreeTransformer
		{
			protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

			public virtual Tree TransformTree(Tree tree)
			{
				ILabel lab = tree.Label();
				if (tree.IsLeaf())
				{
					Tree leaf = this.tf.NewLeaf(lab);
					leaf.SetScore(tree.Score());
					return leaf;
				}
				string s = lab.Value();
				s = this._enclosing.TreebankLanguagePack().BasicCategory(s);
				int numKids = tree.NumChildren();
				IList<Tree> children = new List<Tree>(numKids);
				for (int cNum = 0; cNum < numKids; cNum++)
				{
					Tree child = tree.GetChild(cNum);
					Tree newChild = this.TransformTree(child);
					// cdm 2007: for just subcategory stripping, null shouldn't happen
					// if (newChild != null) {
					children.Add(newChild);
				}
				// }
				// if (children.isEmpty()) {
				//   return null;
				// }
				CategoryWordTag newLabel = new CategoryWordTag(lab);
				newLabel.SetCategory(s);
				if (lab is IHasTag)
				{
					string tag = ((IHasTag)lab).Tag();
					tag = this._enclosing.TreebankLanguagePack().BasicCategory(tag);
					newLabel.SetTag(tag);
				}
				Tree node = this.tf.NewTreeNode(newLabel, children);
				node.SetScore(tree.Score());
				return node;
			}

			internal SubcategoryStripper(AbstractTreebankParserParams _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly AbstractTreebankParserParams _enclosing;
		}

		/// <summary>
		/// The job of this class is to remove subcategorizations from
		/// tag and category nodes, so as to put a tree in a suitable
		/// state for evaluation.
		/// </summary>
		/// <remarks>
		/// The job of this class is to remove subcategorizations from
		/// tag and category nodes, so as to put a tree in a suitable
		/// state for evaluation.  Providing the TreebankLanguagePack
		/// is defined correctly, this should work for any language.
		/// Very simililar to subcategory stripper, but strips grammatical
		/// functions as well.
		/// </remarks>
		protected internal class RemoveGFSubcategoryStripper : ITreeTransformer
		{
			protected internal ITreeFactory tf = new LabeledScoredTreeFactory();

			// end class SubcategoryStripper
			public virtual Tree TransformTree(Tree tree)
			{
				ILabel lab = tree.Label();
				if (tree.IsLeaf())
				{
					Tree leaf = this.tf.NewLeaf(lab);
					leaf.SetScore(tree.Score());
					return leaf;
				}
				string s = lab.Value();
				s = this._enclosing.TreebankLanguagePack().BasicCategory(s);
				s = this._enclosing.TreebankLanguagePack().StripGF(s);
				int numKids = tree.NumChildren();
				IList<Tree> children = new List<Tree>(numKids);
				for (int cNum = 0; cNum < numKids; cNum++)
				{
					Tree child = tree.GetChild(cNum);
					Tree newChild = this.TransformTree(child);
					children.Add(newChild);
				}
				CategoryWordTag newLabel = new CategoryWordTag(lab);
				newLabel.SetCategory(s);
				if (lab is IHasTag)
				{
					string tag = ((IHasTag)lab).Tag();
					tag = this._enclosing.TreebankLanguagePack().BasicCategory(tag);
					tag = this._enclosing.TreebankLanguagePack().StripGF(tag);
					newLabel.SetTag(tag);
				}
				Tree node = this.tf.NewTreeNode(newLabel, children);
				node.SetScore(tree.Score());
				return node;
			}

			internal RemoveGFSubcategoryStripper(AbstractTreebankParserParams _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly AbstractTreebankParserParams _enclosing;
		}

		protected internal string inputEncoding;

		protected internal string outputEncoding;

		protected internal ITreebankLanguagePack tlp;

		protected internal bool generateOriginalDependencies;

		/// <summary>Stores the passed-in TreebankLanguagePack and sets up charset encodings.</summary>
		/// <param name="tlp">The treebank language pack to use</param>
		protected internal AbstractTreebankParserParams(ITreebankLanguagePack tlp)
		{
			// end class RemoveGFSubcategoryStripper
			this.tlp = tlp;
			inputEncoding = tlp.GetEncoding();
			outputEncoding = tlp.GetEncoding();
			generateOriginalDependencies = false;
		}

		public virtual ILabel ProcessHeadWord(ILabel headWord)
		{
			return headWord;
		}

		/// <summary>Sets whether to consider grammatical functions in evaluation</summary>
		public virtual void SetEvaluateGrammaticalFunctions(bool evalGFs)
		{
			this.evalGF = evalGFs;
		}

		/// <summary>Sets the input encoding.</summary>
		public virtual void SetInputEncoding(string encoding)
		{
			inputEncoding = encoding;
		}

		/// <summary>Sets the output encoding.</summary>
		public virtual void SetOutputEncoding(string encoding)
		{
			outputEncoding = encoding;
		}

		/// <summary>Returns the output encoding being used.</summary>
		public virtual string GetOutputEncoding()
		{
			return outputEncoding;
		}

		/// <summary>Returns the input encoding being used.</summary>
		public virtual string GetInputEncoding()
		{
			return inputEncoding;
		}

		/// <summary>Returns a language specific object for evaluating PP attachment</summary>
		/// <returns>
		/// An object that implements
		/// <see cref="Edu.Stanford.Nlp.Parser.Metrics.AbstractEval"/>
		/// </returns>
		public virtual AbstractEval PpAttachmentEval()
		{
			return null;
		}

		/// <summary>returns a MemoryTreebank appropriate to the treebank source</summary>
		public abstract MemoryTreebank MemoryTreebank();

		/// <summary>returns a DiskTreebank appropriate to the treebank source</summary>
		public abstract DiskTreebank DiskTreebank();

		/// <summary>
		/// You can often return the same thing for testMemoryTreebank as
		/// for memoryTreebank
		/// </summary>
		public virtual MemoryTreebank TestMemoryTreebank()
		{
			return MemoryTreebank();
		}

		/// <summary>Implemented as required by TreebankFactory.</summary>
		/// <remarks>Implemented as required by TreebankFactory. Use diskTreebank() instead.</remarks>
		public virtual Treebank Treebank()
		{
			return DiskTreebank();
		}

		/// <summary>The PrintWriter used to print output.</summary>
		/// <remarks>
		/// The PrintWriter used to print output. It's the responsibility of
		/// pw to deal properly with character encodings for the relevant
		/// treebank.
		/// </remarks>
		public virtual PrintWriter Pw()
		{
			return Pw(System.Console.Out);
		}

		/// <summary>The PrintWriter used to print output.</summary>
		/// <remarks>
		/// The PrintWriter used to print output. It's the responsibility of
		/// pw to deal properly with character encodings for the relevant
		/// treebank.
		/// </remarks>
		public virtual PrintWriter Pw(OutputStream o)
		{
			string encoding = outputEncoding;
			if (!Java.Nio.Charset.Charset.IsSupported(encoding))
			{
				log.Info("Warning: desired encoding " + encoding + " not accepted. ");
				log.Info("Using UTF-8 to construct PrintWriter");
				encoding = "UTF-8";
			}
			//log.info("TreebankParserParams.pw(): encoding is " + encoding);
			try
			{
				return new PrintWriter(new OutputStreamWriter(o, encoding), true);
			}
			catch (UnsupportedEncodingException e)
			{
				log.Info("Warning: desired encoding " + outputEncoding + " not accepted. " + e);
				try
				{
					return new PrintWriter(new OutputStreamWriter(o, "UTF-8"), true);
				}
				catch (UnsupportedEncodingException e1)
				{
					log.Info("Something is really wrong.  Your system doesn't even support UTF-8!" + e1);
					return new PrintWriter(o, true);
				}
			}
		}

		/// <summary>Returns an appropriate treebankLanguagePack</summary>
		public virtual ITreebankLanguagePack TreebankLanguagePack()
		{
			return tlp;
		}

		/// <summary>The HeadFinder to use for your treebank.</summary>
		public abstract IHeadFinder HeadFinder();

		/// <summary>The HeadFinder to use when extracting typed dependencies.</summary>
		public abstract IHeadFinder TypedDependencyHeadFinder();

		public virtual ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		/// <summary>Give the parameters for smoothing in the MLEDependencyGrammar.</summary>
		/// <remarks>
		/// Give the parameters for smoothing in the MLEDependencyGrammar.
		/// Defaults are the ones previously hard coded into MLEDependencyGrammar.
		/// </remarks>
		/// <returns>an array of doubles with smooth_aT_hTWd, smooth_aTW_hTWd, smooth_stop, and interp</returns>
		public virtual double[] MLEDependencyGrammarSmoothingParams()
		{
			return new double[] { 16.0, 16.0, 4.0, 0.6 };
		}

		/// <summary>
		/// Takes a Tree and a collinizer and returns a Collection of labeled
		/// <see cref="Edu.Stanford.Nlp.Trees.Constituent"/>
		/// s for PARSEVAL.
		/// </summary>
		/// <param name="t">The tree to extract constituents from</param>
		/// <param name="collinizer">
		/// The TreeTransformer used to normalize the tree for
		/// evaluation
		/// </param>
		/// <returns>The bag of Constituents for PARSEVAL.</returns>
		public static ICollection<Constituent> ParsevalObjectify(Tree t, ITreeTransformer collinizer)
		{
			return ParsevalObjectify(t, collinizer, true);
		}

		/// <summary>
		/// Takes a Tree and a collinizer and returns a Collection of
		/// <see cref="Edu.Stanford.Nlp.Trees.Constituent"/>
		/// s for
		/// PARSEVAL evaluation.  Some notes on this particular parseval:
		/// <ul>
		/// <li> It is character-based, which allows it to be used on segmentation/parsing combination evaluation.
		/// <li> whether it gives you labeled or unlabeled bracketings depends on the value of the
		/// <paramref name="labelConstituents"/>
		/// parameter
		/// </ul>
		/// (Note that I haven't checked this rigorously yet with the PARSEVAL definition
		/// -- Roger.)
		/// </summary>
		public static ICollection<Constituent> ParsevalObjectify(Tree t, ITreeTransformer collinizer, bool labelConstituents)
		{
			ICollection<Constituent> spans = new List<Constituent>();
			Tree t1 = collinizer.TransformTree(t);
			if (t1 == null)
			{
				return spans;
			}
			foreach (Tree node in t1)
			{
				if (node.IsLeaf() || node.IsPreTerminal() || (node != t1 && node.Parent(t1) == null))
				{
					continue;
				}
				int leftEdge = t1.LeftCharEdge(node);
				int rightEdge = t1.RightCharEdge(node);
				if (labelConstituents)
				{
					spans.Add(new LabeledConstituent(leftEdge, rightEdge, node.Label()));
				}
				else
				{
					spans.Add(new SimpleConstituent(leftEdge, rightEdge));
				}
			}
			return spans;
		}

		/// <summary>Returns a collection of untyped word-word dependencies for the tree.</summary>
		public static ICollection<IList<string>> UntypedDependencyObjectify(Tree t, IHeadFinder hf, ITreeTransformer collinizer)
		{
			return DependencyObjectify(t, hf, collinizer, new AbstractTreebankParserParams.UntypedDependencyTyper(hf));
		}

		/// <summary>Returns a collection of unordered (but directed!) untyped word-word dependencies for the tree.</summary>
		public static ICollection<IList<string>> UnorderedUntypedDependencyObjectify(Tree t, IHeadFinder hf, ITreeTransformer collinizer)
		{
			return DependencyObjectify(t, hf, collinizer, new AbstractTreebankParserParams.UnorderedUntypedDependencyTyper(hf));
		}

		/// <summary>Returns a collection of word-word dependencies typed by mother, head, daughter node syntactic categories.</summary>
		public static ICollection<IList<string>> TypedDependencyObjectify(Tree t, IHeadFinder hf, ITreeTransformer collinizer)
		{
			return DependencyObjectify(t, hf, collinizer, new AbstractTreebankParserParams.TypedDependencyTyper(hf));
		}

		/// <summary>Returns a collection of unordered (but directed!) typed word-word dependencies for the tree.</summary>
		public static ICollection<IList<string>> UnorderedTypedDependencyObjectify(Tree t, IHeadFinder hf, ITreeTransformer collinizer)
		{
			return DependencyObjectify(t, hf, collinizer, new AbstractTreebankParserParams.UnorderedTypedDependencyTyper(hf));
		}

		/// <summary>
		/// Returns the set of dependencies in a tree, according to some
		/// <see cref="Edu.Stanford.Nlp.Trees.IDependencyTyper{T}"/>
		/// .
		/// </summary>
		public static ICollection<E> DependencyObjectify<E>(Tree t, IHeadFinder hf, ITreeTransformer collinizer, IDependencyTyper<E> typer)
		{
			ICollection<E> deps = new List<E>();
			Tree t1 = collinizer.TransformTree(t);
			if (t1 == null)
			{
				return deps;
			}
			DependencyObjectifyHelper(t1, t1, hf, deps, typer);
			return deps;
		}

		private static void DependencyObjectifyHelper<E>(Tree t, Tree root, IHeadFinder hf, ICollection<E> c, IDependencyTyper<E> typer)
		{
			if (t.IsLeaf() || t.IsPreTerminal())
			{
				return;
			}
			Tree headDtr = hf.DetermineHead(t);
			foreach (Tree child in t.Children())
			{
				DependencyObjectifyHelper(child, root, hf, c, typer);
				if (child != headDtr)
				{
					c.Add(typer.MakeDependency(headDtr, child, root));
				}
			}
		}

		private class UntypedDependencyTyper : IDependencyTyper<IList<string>>
		{
			internal IHeadFinder hf;

			public UntypedDependencyTyper(IHeadFinder hf)
			{
				this.hf = hf;
			}

			public virtual IList<string> MakeDependency(Tree head, Tree dep, Tree root)
			{
				IList<string> result = new List<string>(3);
				Tree headTerm = head.HeadTerminal(hf);
				Tree depTerm = dep.HeadTerminal(hf);
				bool headLeft = root.LeftCharEdge(headTerm) < root.LeftCharEdge(depTerm);
				result.Add(headTerm.Value());
				result.Add(depTerm.Value());
				if (headLeft)
				{
					result.Add(leftHeaded);
				}
				else
				{
					result.Add(rightHeaded);
				}
				return result;
			}
		}

		private class UnorderedUntypedDependencyTyper : IDependencyTyper<IList<string>>
		{
			internal IHeadFinder hf;

			public UnorderedUntypedDependencyTyper(IHeadFinder hf)
			{
				this.hf = hf;
			}

			public virtual IList<string> MakeDependency(Tree head, Tree dep, Tree root)
			{
				IList<string> result = new List<string>(3);
				Tree headTerm = head.HeadTerminal(hf);
				Tree depTerm = dep.HeadTerminal(hf);
				result.Add(headTerm.Value());
				result.Add(depTerm.Value());
				return result;
			}
		}

		private const string leftHeaded = "leftHeaded";

		private const string rightHeaded = "rightHeaded";

		private class TypedDependencyTyper : IDependencyTyper<IList<string>>
		{
			internal IHeadFinder hf;

			public TypedDependencyTyper(IHeadFinder hf)
			{
				this.hf = hf;
			}

			public virtual IList<string> MakeDependency(Tree head, Tree dep, Tree root)
			{
				IList<string> result = new List<string>(6);
				Tree headTerm = head.HeadTerminal(hf);
				Tree depTerm = dep.HeadTerminal(hf);
				bool headLeft = root.LeftCharEdge(headTerm) < root.LeftCharEdge(depTerm);
				result.Add(headTerm.Value());
				result.Add(depTerm.Value());
				result.Add(head.Parent(root).Value());
				result.Add(head.Value());
				result.Add(dep.Value());
				if (headLeft)
				{
					result.Add(leftHeaded);
				}
				else
				{
					result.Add(rightHeaded);
				}
				return result;
			}
		}

		private class UnorderedTypedDependencyTyper : IDependencyTyper<IList<string>>
		{
			internal IHeadFinder hf;

			public UnorderedTypedDependencyTyper(IHeadFinder hf)
			{
				this.hf = hf;
			}

			public virtual IList<string> MakeDependency(Tree head, Tree dep, Tree root)
			{
				IList<string> result = new List<string>(6);
				Tree headTerm = head.HeadTerminal(hf);
				Tree depTerm = dep.HeadTerminal(hf);
				result.Add(headTerm.Value());
				result.Add(depTerm.Value());
				result.Add(head.Parent(root).Value());
				result.Add(head.Value());
				result.Add(dep.Value());
				return result;
			}
		}

		/// <summary>
		/// Returns an EquivalenceClasser that classes typed dependencies
		/// by the syntactic categories of mother, head and daughter,
		/// plus direction.
		/// </summary>
		/// <returns>An Equivalence class for typed dependencies</returns>
		public static IEquivalenceClasser<IList<string>, string> TypedDependencyClasser()
		{
			return null;
		}

		/// <summary>the tree transformer used to produce trees for evaluation.</summary>
		/// <remarks>
		/// the tree transformer used to produce trees for evaluation.  Will
		/// be applied both to the parse output tree and to the gold
		/// tree. Should strip punctuation and maybe do some other things.
		/// </remarks>
		public abstract ITreeTransformer Collinizer();

		/// <summary>the tree transformer used to produce trees for evaluation.</summary>
		/// <remarks>
		/// the tree transformer used to produce trees for evaluation.  Will
		/// be applied both to the parse output tree and to the gold
		/// tree. Should strip punctuation and maybe do some other
		/// things. The evalb version should strip some more stuff
		/// off. (finish this doc!)
		/// </remarks>
		public abstract ITreeTransformer CollinizerEvalb();

		/// <summary>Returns the splitting strings used for selective splits.</summary>
		/// <returns>
		/// An array containing ancestor-annotated Strings: categories
		/// should be split according to these ancestor annotations.
		/// </returns>
		public abstract string[] SisterSplitters();

		/// <summary>
		/// Returns a TreeTransformer appropriate to the Treebank which
		/// can be used to remove functional tags (such as "-TMP") from
		/// categories.
		/// </summary>
		/// <remarks>
		/// Returns a TreeTransformer appropriate to the Treebank which
		/// can be used to remove functional tags (such as "-TMP") from
		/// categories. Removes GFs if evalGF = false; if GFs were not used
		/// in training, results are equivalent.
		/// </remarks>
		public virtual ITreeTransformer SubcategoryStripper()
		{
			if (evalGF)
			{
				return new AbstractTreebankParserParams.SubcategoryStripper(this);
			}
			return new AbstractTreebankParserParams.RemoveGFSubcategoryStripper(this);
		}

		/// <summary>
		/// This method does language-specific tree transformations such
		/// as annotating particular nodes with language-relevant features.
		/// </summary>
		/// <remarks>
		/// This method does language-specific tree transformations such
		/// as annotating particular nodes with language-relevant features.
		/// Such parameterizations should be inside the specific
		/// TreebankLangParserParams class.  This method is recursively
		/// applied to each node in the tree (depth first, left-to-right),
		/// so you shouldn't write this method to apply recursively to tree
		/// members.  This method is allowed to (and in some cases does)
		/// destructively change the input tree
		/// <paramref name="t"/>
		/// . It changes both
		/// labels and the tree shape.
		/// </remarks>
		/// <param name="t">
		/// The input tree (with non-language specific annotation already
		/// done, so you need to strip back to basic categories)
		/// </param>
		/// <param name="root">The root of the current tree (can be null for words)</param>
		/// <returns>
		/// The fully annotated tree node (with daughters still as you
		/// want them in the final result)
		/// </returns>
		public abstract Tree TransformTree(Tree t, Tree root);

		/// <summary>Display (write to stderr) language-specific settings.</summary>
		public abstract void Display();

		/// <summary>Set language-specific options according to flags.</summary>
		/// <remarks>
		/// Set language-specific options according to flags.
		/// This routine should process the option starting in args[i] (which
		/// might potentially be several arguments long if it takes arguments).
		/// It should return the index after the last index it consumed in
		/// processing.  In particular, if it cannot process the current option,
		/// the return value should be i.
		/// <p>
		/// Generic options are processed separately by
		/// <see cref="Options.SetOption(string[], int)"/>
		/// ,
		/// and implementations of this method do not have to worry about them.
		/// The Options class handles routing options.
		/// TreebankParserParams that extend this class should call super when
		/// overriding this method.
		/// </remarks>
		public virtual int SetOptionFlag(string[] args, int i)
		{
			return i;
		}

		private const long serialVersionUID = 4299501909017975915L;

		public virtual ITokenizerFactory<Tree> TreeTokenizerFactory()
		{
			return new TreeTokenizerFactory(TreeReaderFactory());
		}

		public virtual IExtractor<IDependencyGrammar> DependencyGrammarExtractor(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			return new MLEDependencyGrammarExtractor(op, wordIndex, tagIndex);
		}

		public virtual bool IsEvalGF()
		{
			return evalGF;
		}

		public virtual void SetEvalGF(bool evalGF)
		{
			this.evalGF = evalGF;
		}

		/// <summary>Annotation function for mapping punctuation to PTB-style equivalence classes.</summary>
		/// <author>Spence Green</author>
		[System.Serializable]
		protected internal class AnnotatePunctuationFunction : ISerializableFunction<TregexMatcher, string>
		{
			private readonly string key;

			private readonly string annotationMark;

			public AnnotatePunctuationFunction(string annotationMark, string key)
			{
				this.key = key;
				this.annotationMark = annotationMark;
			}

			public virtual string Apply(TregexMatcher m)
			{
				string punc = m.GetNode(key).Value();
				string punctClass = PunctEquivalenceClasser.GetPunctClass(punc);
				return punctClass.Equals(string.Empty) ? string.Empty : annotationMark + punctClass;
			}

			public override string ToString()
			{
				return "AnnotatePunctuationFunction";
			}

			private const long serialVersionUID = 1L;
		}

		public virtual IList<GrammaticalStructure> ReadGrammaticalStructureFromFile(string filename)
		{
			throw new NotSupportedException("This language does not support GrammaticalStructures or dependencies");
		}

		public virtual GrammaticalStructure GetGrammaticalStructure(Tree t, IPredicate<string> filter, IHeadFinder hf)
		{
			throw new NotSupportedException("This language does not support GrammaticalStructures or dependencies");
		}

		/// <summary>By default, parsers are assumed to not support dependencies.</summary>
		/// <remarks>
		/// By default, parsers are assumed to not support dependencies.
		/// Only English and Chinese do at present.
		/// </remarks>
		public virtual bool SupportsBasicDependencies()
		{
			return false;
		}

		/// <summary>
		/// For languages that have implementations of the
		/// original Stanford dependencies and Universal
		/// dependencies, this parameter is used to decide which
		/// implementation should be used.
		/// </summary>
		public virtual void SetGenerateOriginalDependencies(bool originalDependencies)
		{
			this.generateOriginalDependencies = originalDependencies;
			if (this.tlp != null)
			{
				this.tlp.SetGenerateOriginalDependencies(originalDependencies);
			}
		}

		public virtual bool GenerateOriginalDependencies()
		{
			return this.generateOriginalDependencies;
		}

		private static readonly string[] EmptyArgs = new string[0];

		public virtual string[] DefaultCoreNLPFlags()
		{
			return EmptyArgs;
		}

		public abstract IList<IHasWord> DefaultTestSentence();

		public abstract ITreeReaderFactory TreeReaderFactory();
	}
}

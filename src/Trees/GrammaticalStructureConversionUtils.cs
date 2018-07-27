using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Lexparser;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Contains several utility methods to convert constituency trees to
	/// dependency trees.
	/// </summary>
	/// <remarks>
	/// Contains several utility methods to convert constituency trees to
	/// dependency trees.
	/// Used by
	/// <see cref="GrammaticalStructure.Main(string[])"/>
	/// .
	/// </remarks>
	public class GrammaticalStructureConversionUtils
	{
		public const string DefaultParserFile = "edu/stanford/nlp/models/lexparser/englishPCFG.ser.gz";

		private GrammaticalStructureConversionUtils()
		{
		}

		// static methods
		/// <summary>
		/// Print typed dependencies in either the Stanford dependency representation
		/// or in the conllx format.
		/// </summary>
		/// <param name="deps">Typed dependencies to print</param>
		/// <param name="tree">
		/// Tree corresponding to typed dependencies (only necessary if conllx
		/// == true)
		/// </param>
		/// <param name="conllx">If true use conllx format, otherwise use Stanford representation</param>
		/// <param name="extraSep">
		/// If true, in the Stanford representation, the extra dependencies
		/// (which do not preserve the tree structure) are printed after the
		/// basic dependencies
		/// </param>
		/// <param name="convertToUPOS">
		/// If true convert the POS tags to universal POS tags and output
		/// them along the original POS tags.
		/// </param>
		public static void PrintDependencies(GrammaticalStructure gs, ICollection<TypedDependency> deps, Tree tree, bool conllx, bool extraSep, bool convertToUPOS)
		{
			System.Console.Out.WriteLine(DependenciesToString(gs, deps, tree, conllx, extraSep, convertToUPOS));
		}

		/// <summary>
		/// Calls dependenciesToCoNLLXString with the basic dependencies
		/// from a grammatical structure.
		/// </summary>
		/// <remarks>
		/// Calls dependenciesToCoNLLXString with the basic dependencies
		/// from a grammatical structure.
		/// (see
		/// <see cref="DependenciesToCoNLLXString(System.Collections.Generic.ICollection{E}, Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// )
		/// </remarks>
		public static string DependenciesToCoNLLXString(GrammaticalStructure gs, ICoreMap sentence)
		{
			return DependenciesToCoNLLXString(gs.TypedDependencies(), sentence);
		}

		/// <summary>Returns a dependency tree in CoNNL-X format.</summary>
		/// <remarks>
		/// Returns a dependency tree in CoNNL-X format.
		/// It requires a CoreMap for the sentence with a TokensAnnotation.
		/// Each token has to contain a word and a POS tag.
		/// </remarks>
		/// <param name="deps">The list of TypedDependency relations.</param>
		/// <param name="sentence">The corresponding CoreMap for the sentence.</param>
		/// <returns>Dependency tree in CoNLL-X format.</returns>
		public static string DependenciesToCoNLLXString(ICollection<TypedDependency> deps, ICoreMap sentence)
		{
			StringBuilder bf = new StringBuilder();
			Dictionary<int, TypedDependency> indexedDeps = new Dictionary<int, TypedDependency>(deps.Count);
			foreach (TypedDependency dep in deps)
			{
				indexedDeps[dep.Dep().Index()] = dep;
			}
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (tokens == null)
			{
				throw new Exception("dependenciesToCoNLLXString: CoreMap does not have required TokensAnnotation.");
			}
			int idx = 1;
			foreach (CoreLabel token in tokens)
			{
				string word = token.Value();
				string pos = token.Tag();
				string cPos = (token.Get(typeof(CoreAnnotations.CoarseTagAnnotation)) != null) ? token.Get(typeof(CoreAnnotations.CoarseTagAnnotation)) : pos;
				string lemma = token.Lemma() != null ? token.Lemma() : "_";
				int gov = indexedDeps.Contains(idx) ? indexedDeps[idx].Gov().Index() : 0;
				string reln = indexedDeps.Contains(idx) ? indexedDeps[idx].Reln().ToString() : "erased";
				string @out = string.Format("%d\t%s\t%s\t%s\t%s\t_\t%d\t%s\t_\t_\n", idx, word, lemma, cPos, pos, gov, reln);
				bf.Append(@out);
				idx++;
			}
			return bf.ToString();
		}

		public static string DependenciesToString(GrammaticalStructure gs, ICollection<TypedDependency> deps, Tree tree, bool conllx, bool extraSep, bool convertToUPOS)
		{
			StringBuilder bf = new StringBuilder();
			IDictionary<int, int> indexToPos = Generics.NewHashMap();
			indexToPos[0] = 0;
			// to deal with the special node "ROOT"
			IList<Tree> gsLeaves = gs.Root().GetLeaves();
			for (int i = 0; i < gsLeaves.Count; i++)
			{
				TreeGraphNode leaf = (TreeGraphNode)gsLeaves[i];
				indexToPos[((CoreLabel)leaf.Label()).Index()] = i + 1;
			}
			if (conllx)
			{
				IList<Tree> leaves = tree.GetLeaves();
				IList<ILabel> uposLabels;
				// = null; // initialized below
				if (convertToUPOS)
				{
					Tree uposTree = UniversalPOSMapper.MapTree(tree);
					uposLabels = uposTree.PreTerminalYield();
				}
				else
				{
					uposLabels = tree.PreTerminalYield();
				}
				int index = 0;
				ICoreMap sentence = new CoreLabel();
				IList<CoreLabel> tokens = new List<CoreLabel>(leaves.Count);
				foreach (Tree leaf in leaves)
				{
					index++;
					if (!indexToPos.Contains(index))
					{
						continue;
					}
					CoreLabel token = new CoreLabel();
					token.SetIndex(index);
					token.SetValue(leaf.Value());
					token.SetWord(leaf.Value());
					token.SetTag(leaf.Parent(tree).Value());
					token.Set(typeof(CoreAnnotations.CoarseTagAnnotation), uposLabels[index - 1].Value());
					tokens.Add(token);
				}
				sentence.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
				bf.Append(DependenciesToCoNLLXString(deps, sentence));
			}
			else
			{
				if (extraSep)
				{
					IList<TypedDependency> extraDeps = new List<TypedDependency>();
					foreach (TypedDependency dep in deps)
					{
						if (dep.Extra())
						{
							extraDeps.Add(dep);
						}
						else
						{
							bf.Append(ToStringIndex(dep, indexToPos));
							bf.Append('\n');
						}
					}
					// now we print the separator for extra dependencies, and print these if
					// there are some
					if (!extraDeps.IsEmpty())
					{
						bf.Append("======\n");
						foreach (TypedDependency dep_1 in extraDeps)
						{
							bf.Append(ToStringIndex(dep_1, indexToPos));
							bf.Append('\n');
						}
					}
				}
				else
				{
					foreach (TypedDependency dep in deps)
					{
						bf.Append(ToStringIndex(dep, indexToPos));
						bf.Append('\n');
					}
				}
			}
			return bf.ToString();
		}

		private static string ToStringIndex(TypedDependency td, IDictionary<int, int> indexToPos)
		{
			IndexedWord gov = td.Gov();
			IndexedWord dep = td.Dep();
			return td.Reln() + "(" + gov.Value() + "-" + indexToPos[gov.Index()] + gov.ToPrimes() + ", " + dep.Value() + "-" + indexToPos[dep.Index()] + dep.ToPrimes() + ")";
		}

		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.GrammaticalStructureConversionUtils));

		private static string[] ParseClassConstructArgs(string namePlusArgs)
		{
			string[] args = StringUtils.EmptyStringArray;
			string name = namePlusArgs;
			if (namePlusArgs.Matches(".*\\([^)]*\\)$"))
			{
				string argStr = namePlusArgs.ReplaceFirst("^.*\\(([^)]*)\\)$", "$1");
				args = argStr.Split(",");
				name = namePlusArgs.ReplaceFirst("\\([^)]*\\)$", string.Empty);
			}
			string[] tokens = new string[1 + args.Length];
			tokens[0] = name;
			System.Array.Copy(args, 0, tokens, 1, args.Length);
			return tokens;
		}

		private static IDependencyReader LoadAlternateDependencyReader(string altDepReaderName)
		{
			Type altDepReaderClass = null;
			string[] toks = ParseClassConstructArgs(altDepReaderName);
			altDepReaderName = toks[0];
			string[] depReaderArgs = new string[toks.Length - 1];
			System.Array.Copy(toks, 1, depReaderArgs, 0, toks.Length - 1);
			try
			{
				Type cl = Sharpen.Runtime.GetType(altDepReaderName);
				altDepReaderClass = cl.AsSubclass<IDependencyReader>();
			}
			catch (TypeLoadException)
			{
			}
			// have a second go below
			if (altDepReaderClass == null)
			{
				try
				{
					Type cl = Sharpen.Runtime.GetType("edu.stanford.nlp.trees." + altDepReaderName);
					altDepReaderClass = cl.AsSubclass<IDependencyReader>();
				}
				catch (TypeLoadException)
				{
				}
			}
			//
			if (altDepReaderClass == null)
			{
				log.Info("Can't load dependency reader " + altDepReaderName + " or edu.stanford.nlp.trees." + altDepReaderName);
				return null;
			}
			IDependencyReader altDepReader;
			// initialized below
			if (depReaderArgs.Length == 0)
			{
				try
				{
					altDepReader = System.Activator.CreateInstance(altDepReaderClass);
				}
				catch (InstantiationException e)
				{
					throw new Exception(e);
				}
				catch (MemberAccessException)
				{
					log.Info("No argument constructor to " + altDepReaderName + " is not public");
					return null;
				}
			}
			else
			{
				try
				{
					altDepReader = altDepReaderClass.GetConstructor(typeof(string[])).NewInstance((object)depReaderArgs);
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
				catch (InstantiationException e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
					return null;
				}
				catch (MemberAccessException)
				{
					log.Info(depReaderArgs.Length + " argument constructor to " + altDepReaderName + " is not public.");
					return null;
				}
				catch (MissingMethodException)
				{
					log.Info("String arguments constructor to " + altDepReaderName + " does not exist.");
					return null;
				}
			}
			return altDepReader;
		}

		private static IDependencyPrinter LoadAlternateDependencyPrinter(string altDepPrinterName)
		{
			Type altDepPrinterClass = null;
			string[] toks = ParseClassConstructArgs(altDepPrinterName);
			altDepPrinterName = toks[0];
			string[] depPrintArgs = new string[toks.Length - 1];
			System.Array.Copy(toks, 1, depPrintArgs, 0, toks.Length - 1);
			try
			{
				Type cl = Sharpen.Runtime.GetType(altDepPrinterName);
				altDepPrinterClass = cl.AsSubclass<IDependencyPrinter>();
			}
			catch (TypeLoadException)
			{
			}
			//
			if (altDepPrinterClass == null)
			{
				try
				{
					Type cl = Sharpen.Runtime.GetType("edu.stanford.nlp.trees." + altDepPrinterName);
					altDepPrinterClass = cl.AsSubclass<IDependencyPrinter>();
				}
				catch (TypeLoadException)
				{
				}
			}
			//
			if (altDepPrinterClass == null)
			{
				System.Console.Error.Printf("Unable to load alternative printer %s or %s. Is your classpath set correctly?\n", altDepPrinterName, "edu.stanford.nlp.trees." + altDepPrinterName);
				return null;
			}
			try
			{
				IDependencyPrinter depPrinter;
				if (depPrintArgs.Length == 0)
				{
					depPrinter = System.Activator.CreateInstance(altDepPrinterClass);
				}
				else
				{
					depPrinter = altDepPrinterClass.GetConstructor(typeof(string[])).NewInstance((object)depPrintArgs);
				}
				return depPrinter;
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				return null;
			}
			catch (MissingMethodException)
			{
				if (depPrintArgs.Length == 0)
				{
					System.Console.Error.Printf("Can't find no-argument constructor %s().%n", altDepPrinterName);
				}
				else
				{
					System.Console.Error.Printf("Can't find constructor %s(%s).%n", altDepPrinterName, Arrays.ToString(depPrintArgs));
				}
				return null;
			}
		}

		private static IFunction<IList<IHasWord>, Tree> LoadParser(string parserFile, string parserOptions, bool makeCopulaHead)
		{
			if (parserFile == null || parserFile.IsEmpty())
			{
				parserFile = DefaultParserFile;
				if (parserOptions == null)
				{
					parserOptions = "-retainTmpSubcategories";
				}
			}
			if (parserOptions == null)
			{
				parserOptions = string.Empty;
			}
			if (makeCopulaHead)
			{
				parserOptions = "-makeCopulaHead " + parserOptions;
			}
			parserOptions = parserOptions.Trim();
			// Load parser by reflection, so that this class doesn't require parser
			// for runtime use
			// LexicalizedParser lp = LexicalizedParser.loadModel(parserFile);
			// For example, the tregex package uses TreePrint, which uses
			// GrammaticalStructure, which would then import the
			// LexicalizedParser.  The tagger can read trees, which means it
			// would depend on tregex and therefore depend on the parser.
			IFunction<IList<IHasWord>, Tree> lp;
			try
			{
				Type[] classes = new Type[] { typeof(string), typeof(string[]) };
				MethodInfo method = Sharpen.Runtime.GetType("edu.stanford.nlp.parser.lexparser.LexicalizedParser").GetMethod("loadModel", classes);
				string[] opts = StringUtils.EmptyStringArray;
				if (!parserOptions.IsEmpty())
				{
					opts = parserOptions.Split(" +");
				}
				lp = (IFunction<IList<IHasWord>, Tree>)method.Invoke(null, parserFile, opts);
			}
			catch (Exception cnfe)
			{
				throw new Exception(cnfe);
			}
			return lp;
		}

		/// <summary>
		/// Allow a collection of trees, that is a Treebank, appear to be a collection
		/// of GrammaticalStructures.
		/// </summary>
		/// <author>danielcer</author>
		private class TreeBankGrammaticalStructureWrapper : IEnumerable<GrammaticalStructure>
		{
			private readonly IEnumerable<Tree> trees;

			private readonly bool keepPunct;

			private readonly ITreebankLangParserParams @params;

			private readonly IDictionary<GrammaticalStructure, Tree> origTrees = new WeakHashMap<GrammaticalStructure, Tree>();

			public TreeBankGrammaticalStructureWrapper(IEnumerable<Tree> wrappedTrees, bool keepPunct, ITreebankLangParserParams @params)
			{
				trees = wrappedTrees;
				this.keepPunct = keepPunct;
				this.@params = @params;
			}

			public virtual IEnumerator<GrammaticalStructure> GetEnumerator()
			{
				return new GrammaticalStructureConversionUtils.TreeBankGrammaticalStructureWrapper.GsIterator(this);
			}

			public virtual Tree GetOriginalTree(GrammaticalStructure gs)
			{
				return origTrees[gs];
			}

			private class GsIterator : IEnumerator<GrammaticalStructure>
			{
				private readonly IEnumerator<Tree> tbIterator = this._enclosing.trees.GetEnumerator();

				private readonly IPredicate<string> puncFilter;

				private readonly IHeadFinder hf;

				private GrammaticalStructure next;

				public GsIterator(TreeBankGrammaticalStructureWrapper _enclosing)
				{
					this._enclosing = _enclosing;
					if (this._enclosing.keepPunct)
					{
						this.puncFilter = Filters.AcceptFilter();
					}
					else
					{
						if (this._enclosing.@params.GenerateOriginalDependencies())
						{
							this.puncFilter = this._enclosing.@params.TreebankLanguagePack().PunctuationWordRejectFilter();
						}
						else
						{
							this.puncFilter = this._enclosing.@params.TreebankLanguagePack().PunctuationTagRejectFilter();
						}
					}
					this.hf = this._enclosing.@params.TypedDependencyHeadFinder();
					this.PrimeGs();
				}

				private void PrimeGs()
				{
					GrammaticalStructure gs = null;
					while (gs == null && this.tbIterator.MoveNext())
					{
						Tree t = this.tbIterator.Current;
						// log.info("GsIterator: Next tree is");
						// log.info(t);
						if (t == null)
						{
							continue;
						}
						try
						{
							gs = this._enclosing.@params.GetGrammaticalStructure(t, this.puncFilter, this.hf);
							this._enclosing.origTrees[gs] = t;
							this.next = gs;
							// log.info("GsIterator: Next tree is");
							// log.info(t);
							return;
						}
						catch (ArgumentNullException npe)
						{
							GrammaticalStructureConversionUtils.log.Info("Bung tree caused below dump. Continuing....");
							GrammaticalStructureConversionUtils.log.Info(t);
							Sharpen.Runtime.PrintStackTrace(npe);
						}
					}
					this.next = null;
				}

				public virtual bool MoveNext()
				{
					return this.next != null;
				}

				public virtual GrammaticalStructure Current
				{
					get
					{
						GrammaticalStructure ret = this.next;
						if (ret == null)
						{
							throw new NoSuchElementException();
						}
						this.PrimeGs();
						return ret;
					}
				}

				public virtual void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly TreeBankGrammaticalStructureWrapper _enclosing;
			}
		}

		/// <summary>Enum to identify the different TokenizerTypes.</summary>
		/// <remarks>
		/// Enum to identify the different TokenizerTypes. To add a new
		/// TokenizerType, add it to the list with a default options string
		/// and add a clause in getTokenizerType to identify it.
		/// </remarks>
		[System.Serializable]
		public sealed class ConverterOptions
		{
			public static readonly GrammaticalStructureConversionUtils.ConverterOptions UniversalEnglish = new GrammaticalStructureConversionUtils.ConverterOptions("en", new NPTmpRetainingTreeNormalizer(0, false, 1, false), "edu.stanford.nlp.parser.lexparser.EnglishTreebankParserParams"
				, false, true);

			public static readonly GrammaticalStructureConversionUtils.ConverterOptions UniversalChinese = new GrammaticalStructureConversionUtils.ConverterOptions("zh", new CTBErrorCorrectingTreeNormalizer(false, false, false, false), "edu.stanford.nlp.parser.lexparser.ChineseTreebankParserParams"
				, false, false);

			public static readonly GrammaticalStructureConversionUtils.ConverterOptions English = new GrammaticalStructureConversionUtils.ConverterOptions("en-sd", new NPTmpRetainingTreeNormalizer(0, false, 1, false), "edu.stanford.nlp.parser.lexparser.EnglishTreebankParserParams"
				, true, true);

			public static readonly GrammaticalStructureConversionUtils.ConverterOptions Chinese = new GrammaticalStructureConversionUtils.ConverterOptions("zh-sd", new CTBErrorCorrectingTreeNormalizer(false, false, false, false), "edu.stanford.nlp.parser.lexparser.ChineseTreebankParserParams"
				, true, false);

			public readonly string abbreviation;

			public readonly TreeNormalizer treeNormalizer;

			public readonly string tlPPClassName;

			public readonly bool stanfordDependencies;

			public readonly bool convertToUPOS;

			internal ConverterOptions(string abbreviation, TreeNormalizer treeNormalizer, string tlPPClassName, bool stanfordDependencies, bool convertToUPOS)
			{
				// end static class TreebankGrammaticalStructureWrapper
				/* Conversion to UPOS is currently only supported for English. */
				this.abbreviation = abbreviation;
				this.treeNormalizer = treeNormalizer;
				this.tlPPClassName = tlPPClassName;
				/* Generate old Stanford Dependencies instead of UD, when set to true. */
				this.stanfordDependencies = stanfordDependencies;
				this.convertToUPOS = convertToUPOS;
			}

			private static readonly IDictionary<string, GrammaticalStructureConversionUtils.ConverterOptions> nameToTokenizerMap = InitializeNameMap();

			private static IDictionary<string, GrammaticalStructureConversionUtils.ConverterOptions> InitializeNameMap()
			{
				IDictionary<string, GrammaticalStructureConversionUtils.ConverterOptions> map = Generics.NewHashMap();
				foreach (GrammaticalStructureConversionUtils.ConverterOptions opts in GrammaticalStructureConversionUtils.ConverterOptions.Values())
				{
					if (opts.abbreviation != null)
					{
						map[opts.abbreviation.ToUpper()] = opts;
					}
					map[opts.ToString().ToUpper()] = opts;
				}
				return Java.Util.Collections.UnmodifiableMap(map);
			}

			public static GrammaticalStructureConversionUtils.ConverterOptions GetConverterOptions(string language)
			{
				if (language == null)
				{
					return GrammaticalStructureConversionUtils.ConverterOptions.nameToTokenizerMap["EN"];
				}
				GrammaticalStructureConversionUtils.ConverterOptions opts = GrammaticalStructureConversionUtils.ConverterOptions.nameToTokenizerMap[language.ToUpper()];
				return opts != null ? opts : GrammaticalStructureConversionUtils.ConverterOptions.nameToTokenizerMap["EN"];
			}
		}

		/// <summary>Given sentences or trees, output the typed dependencies.</summary>
		/// <remarks>
		/// Given sentences or trees, output the typed dependencies.
		/// <p>
		/// By default, the method outputs the collapsed typed dependencies with
		/// processing of conjuncts. The input can be given as plain text (one sentence
		/// by line) using the option -sentFile, or as trees using the option
		/// -treeFile. For -sentFile, the input has to be strictly one sentence per
		/// line. You can specify where to find a parser with -parserFile
		/// serializedParserPath. See LexicalizedParser for more flexible processing of
		/// text files (including with Stanford Dependencies output). The above options
		/// assume a file as input. You can also feed trees (only) via stdin by using
		/// the option -filter.  If one does not specify a -parserFile, one
		/// can specify which language pack to use with -tLPP, This option
		/// specifies a class which determines which GrammaticalStructure to
		/// use, which HeadFinder to use, etc.  It will default to
		/// edu.stanford.nlp.parser.lexparser.EnglishTreebankParserParams,
		/// but any TreebankLangParserParams can be specified.
		/// <p>
		/// If no method of producing trees is given other than to use the
		/// LexicalizedParser, but no parser is specified, a default parser
		/// is used, the English parser.  You can specify options to load
		/// with the parser using the -parserOpts flag.  If the default
		/// parser is used, and no options are provided, the option
		/// -retainTmpSubcategories is used.
		/// <p>
		/// The following options can be used to specify the types of dependencies
		/// wanted: </p>
		/// <ul>
		/// <li> -collapsed collapsed dependencies
		/// <li> -basic non-collapsed dependencies that preserve a tree structure
		/// <li> -nonCollapsed non-collapsed dependencies that do not preserve a tree
		/// structure (the basic dependencies plus the extra ones)
		/// <li> -CCprocessed
		/// collapsed dependencies and conjunctions processed (dependencies are added
		/// for each conjunct) -- this is the default if no options are passed
		/// <li> -collapsedTree collapsed dependencies retaining a tree structure
		/// <li> -makeCopulaHead Contrary to the approach argued for in the SD papers,
		/// nevertheless make the verb 'to be' the head, not the predicate noun, adjective,
		/// etc. (However, when the verb 'to be' is used as an auxiliary verb, the main
		/// verb is still treated as the head.)
		/// <li> -originalDependencies generate the dependencies using the original converter
		/// instead of the Universal Dependencies converter.
		/// </ul>
		/// <p>
		/// The
		/// <c>-conllx</c>
		/// option will output the dependencies in the CoNLL format,
		/// instead of in the standard Stanford format (relation(governor,dependent))
		/// and will retain punctuation by default.
		/// When used in the "collapsed" format, words such as prepositions, conjunctions
		/// which get collapsed into the grammatical relations and are not part of the
		/// sentence per se anymore will be annotated with "erased" as grammatical relation
		/// and attached to the fake "ROOT" node with index 0.
		/// <p/><p>
		/// There is also an option to retain dependencies involving punctuation:
		/// <c>-keepPunct</c>
		/// </p><p>
		/// The
		/// <c>-extraSep</c>
		/// option used with -nonCollapsed will print the basic
		/// dependencies first, then a separator ======, and then the extra
		/// dependencies that do not preserve the tree structure. The -test option is
		/// used for debugging: it prints the grammatical structure, as well as the
		/// basic, collapsed and CCprocessed dependencies. It also checks the
		/// connectivity of the collapsed dependencies. If the collapsed dependencies
		/// list doesn't constitute a connected graph, it prints the possible offending
		/// nodes (one of them is the real root of the graph).
		/// </p><p>
		/// Using the -conllxFile, you can pass a file containing Stanford dependencies
		/// in the CoNLL format (e.g., the basic dependencies), and obtain another
		/// representation using one of the representation options.
		/// </p><p>
		/// Usage: <br />
		/// <code>java edu.stanford.nlp.trees.GrammaticalStructure [-treeFile FILE | -sentFile FILE | -conllxFile FILE | -filter] <br />
		/// [-collapsed -basic -CCprocessed -test -generateOriginalDependencies]</code>
		/// </remarks>
		/// <param name="args">Command-line arguments, as above</param>
		public static void ConvertTrees(string[] args, string defaultLang)
		{
			/* Use a tree normalizer that removes all empty nodes.
			This prevents wrong indexing of the nodes in the dependency relations. */
			IEnumerable<GrammaticalStructure> gsBank = null;
			Properties props = StringUtils.ArgsToProperties(args);
			string language = props.GetProperty("language", defaultLang);
			GrammaticalStructureConversionUtils.ConverterOptions opts = GrammaticalStructureConversionUtils.ConverterOptions.GetConverterOptions(language);
			MemoryTreebank tb = new MemoryTreebank(opts.treeNormalizer);
			IEnumerable<Tree> trees = tb;
			string encoding = props.GetProperty("encoding", "utf-8");
			try
			{
				Runtime.SetOut(new TextWriter(System.Console.Out, true, encoding));
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			string treeFileName = props.GetProperty("treeFile");
			string sentFileName = props.GetProperty("sentFile");
			string conllXFileName = props.GetProperty("conllxFile");
			string altDepPrinterName = props.GetProperty("altprinter");
			string altDepReaderName = props.GetProperty("altreader");
			string altDepReaderFilename = props.GetProperty("altreaderfile");
			string filter = props.GetProperty("filter");
			bool makeCopulaHead = props.GetProperty("makeCopulaHead") != null;
			bool generateOriginalDependencies = props.GetProperty("originalDependencies") != null || opts.stanfordDependencies;
			// TODO: if a parser is specified, load this from the parser
			// instead of ever loading it from this way
			string tLPP = props.GetProperty("tLPP", opts.tlPPClassName);
			ITreebankLangParserParams @params = ReflectionLoading.LoadByReflection(tLPP);
			@params.SetGenerateOriginalDependencies(generateOriginalDependencies);
			if (makeCopulaHead)
			{
				// TODO: generalize and allow for more options
				string[] options = new string[] { "-makeCopulaHead" };
				@params.SetOptionFlag(options, 0);
			}
			if (sentFileName == null && (altDepReaderName == null || altDepReaderFilename == null) && treeFileName == null && conllXFileName == null && filter == null)
			{
				try
				{
					System.Console.Error.Printf("Usage: java %s%n", typeof(GrammaticalStructure).GetCanonicalName());
					System.Console.Error.WriteLine("Options:");
					System.Console.Error.WriteLine("  Dependency representation:");
					System.Console.Error.WriteLine("    -basic:\t\tGenerate basic dependencies.");
					System.Console.Error.WriteLine("    -enhanced:\t\tGenerate enhanced dependencies, currently only implemented for English UD.");
					System.Console.Error.WriteLine("    -enhanced++:\tGenerate enhanced++ dependencies (default), currently only implemented for English UD.");
					System.Console.Error.WriteLine("    -collapsed:\t\tGenerate collapsed dependencies, deprecated.");
					System.Console.Error.WriteLine("    -CCprocessed:\tGenerate CC-processed dependencies, deprecated.");
					System.Console.Error.WriteLine("    -collapsedTree:\tGenerate collapsed-tree dependencies, deprecated.");
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.WriteLine("  Input:");
					System.Console.Error.WriteLine("    -treeFile <FILE>:\tConvert from constituency trees in <FILE>");
					System.Console.Error.WriteLine("    -sentFile <FILE>:\tParse and convert sentences from <FILE>. Only implemented for English.");
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.WriteLine("  Output:");
					System.Console.Error.WriteLine("    -conllx:\t\tOutput dependencies in CoNLL format.");
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.WriteLine("  Language:");
					System.Console.Error.WriteLine("    -language [en|zh|en-sd|zh-sd]:\t (Universal English Dependencies, Universal Chinese Dependencies, English Stanford Dependencies, Chinese Stanford Dependencies)");
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.WriteLine(string.Empty);
					System.Console.Error.WriteLine("Example:");
					ITreeReader tr = new PennTreeReader(new StringReader("((S (NP (NNP Sam)) (VP (VBD died) (NP-TMP (NN today)))))"));
					tb.Add(tr.ReadTree());
				}
				catch (Exception e)
				{
					log.Info("Horrible error: " + e);
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
			else
			{
				if (altDepReaderName != null && altDepReaderFilename != null)
				{
					IDependencyReader altDepReader = LoadAlternateDependencyReader(altDepReaderName);
					try
					{
						gsBank = altDepReader.ReadDependencies(altDepReaderFilename);
					}
					catch (IOException)
					{
						log.Info("Error reading " + altDepReaderFilename);
						return;
					}
				}
				else
				{
					if (treeFileName != null)
					{
						tb.LoadPath(treeFileName);
					}
					else
					{
						if (filter != null)
						{
							tb.Load(IOUtils.ReaderFromStdin());
						}
						else
						{
							if (conllXFileName != null)
							{
								try
								{
									gsBank = @params.ReadGrammaticalStructureFromFile(conllXFileName);
								}
								catch (RuntimeIOException)
								{
									log.Info("Error reading " + conllXFileName);
									return;
								}
							}
							else
							{
								string parserFile = props.GetProperty("parserFile");
								string parserOpts = props.GetProperty("parserOpts");
								bool tokenized = props.GetProperty("tokenized") != null;
								IFunction<IList<IHasWord>, Tree> lp = LoadParser(parserFile, parserOpts, makeCopulaHead);
								trees = new GrammaticalStructureConversionUtils.LazyLoadTreesByParsing(sentFileName, encoding, tokenized, lp);
								// Instead of getting this directly from the LP, use reflection
								// so that a package which uses GrammaticalStructure doesn't
								// necessarily have to use LexicalizedParser
								try
								{
									MethodInfo method = lp.GetType().GetMethod("getTLPParams");
									@params = (ITreebankLangParserParams)method.Invoke(lp);
									@params.SetGenerateOriginalDependencies(generateOriginalDependencies);
								}
								catch (Exception cnfe)
								{
									throw new Exception(cnfe);
								}
							}
						}
					}
				}
			}
			// treats the output according to the options passed
			bool basic = props.GetProperty("basic") != null;
			bool collapsed = props.GetProperty("collapsed") != null;
			bool CCprocessed = props.GetProperty("CCprocessed") != null;
			bool collapsedTree = props.GetProperty("collapsedTree") != null;
			bool nonCollapsed = props.GetProperty("nonCollapsed") != null;
			bool extraSep = props.GetProperty("extraSep") != null;
			bool parseTree = props.GetProperty("parseTree") != null;
			bool test = props.GetProperty("test") != null;
			bool keepPunct = true;
			//always keep punctuation marks
			bool conllx = props.GetProperty("conllx") != null;
			// todo: Support checkConnected on more options (including basic)
			bool checkConnected = props.GetProperty("checkConnected") != null;
			bool portray = props.GetProperty("portray") != null;
			bool enhanced = props.GetProperty("enhanced") != null;
			bool enhancedPlusPlus = props.GetProperty("enhanced++") != null;
			// If requested load alternative printer
			IDependencyPrinter altDepPrinter = null;
			if (altDepPrinterName != null)
			{
				altDepPrinter = LoadAlternateDependencyPrinter(altDepPrinterName);
			}
			// log.info("First tree in tb is");
			// log.info(((MemoryTreebank) tb).get(0));
			MethodInfo m = null;
			if (test)
			{
				// see if we can use SemanticGraph(Factory) to check for being a DAG
				// Do this by reflection to avoid this becoming a dependency when we distribute the parser
				try
				{
					Type sgf = Sharpen.Runtime.GetType("edu.stanford.nlp.semgraph.SemanticGraphFactory");
					m = Sharpen.Runtime.GetDeclaredMethod(sgf, "makeFromTree", typeof(GrammaticalStructure), typeof(SemanticGraphFactory.Mode), typeof(GrammaticalStructure.Extras), typeof(IPredicate));
				}
				catch (Exception)
				{
					log.Info("Test cannot check for cycles in tree format (classes not available)");
				}
			}
			if (gsBank == null)
			{
				gsBank = new GrammaticalStructureConversionUtils.TreeBankGrammaticalStructureWrapper(trees, keepPunct, @params);
			}
			foreach (GrammaticalStructure gs in gsBank)
			{
				Tree tree;
				if (gsBank is GrammaticalStructureConversionUtils.TreeBankGrammaticalStructureWrapper)
				{
					// log.info("Using TreeBankGrammaticalStructureWrapper branch");
					tree = ((GrammaticalStructureConversionUtils.TreeBankGrammaticalStructureWrapper)gsBank).GetOriginalTree(gs);
				}
				else
				{
					// log.info("Tree is: ");
					// log.info(t);
					// log.info("Using gs.root() branch");
					tree = gs.Root();
				}
				// recover tree
				// log.info("Tree from gs is");
				// log.info(t);
				if (test)
				{
					// print the grammatical structure, the basic, collapsed and CCprocessed
					System.Console.Out.WriteLine("============= parse tree =======================");
					tree.PennPrint();
					System.Console.Out.WriteLine();
					System.Console.Out.WriteLine("------------- GrammaticalStructure -------------");
					System.Console.Out.WriteLine(gs);
					bool allConnected = true;
					bool connected;
					ICollection<TypedDependency> bungRoots = null;
					System.Console.Out.WriteLine("------------- basic dependencies ---------------");
					IList<TypedDependency> gsb = gs.TypedDependencies(GrammaticalStructure.Extras.None);
					System.Console.Out.WriteLine(StringUtils.Join(gsb, "\n"));
					connected = GrammaticalStructure.IsConnected(gsb);
					if (!connected && bungRoots == null)
					{
						bungRoots = GrammaticalStructure.GetRoots(gsb);
					}
					allConnected = connected && allConnected;
					System.Console.Out.WriteLine("------------- non-collapsed dependencies (basic + extra) ---------------");
					IList<TypedDependency> gse = gs.TypedDependencies(GrammaticalStructure.Extras.Maximal);
					System.Console.Out.WriteLine(StringUtils.Join(gse, "\n"));
					connected = GrammaticalStructure.IsConnected(gse);
					if (!connected && bungRoots == null)
					{
						bungRoots = GrammaticalStructure.GetRoots(gse);
					}
					allConnected = connected && allConnected;
					System.Console.Out.WriteLine("------------- collapsed dependencies -----------");
					System.Console.Out.WriteLine(StringUtils.Join(gs.TypedDependenciesCollapsed(GrammaticalStructure.Extras.Maximal), "\n"));
					System.Console.Out.WriteLine("------------- collapsed dependencies tree -----------");
					System.Console.Out.WriteLine(StringUtils.Join(gs.TypedDependenciesCollapsedTree(), "\n"));
					System.Console.Out.WriteLine("------------- CCprocessed dependencies --------");
					IList<TypedDependency> gscc = gs.TypedDependenciesCollapsed(GrammaticalStructure.Extras.Maximal);
					System.Console.Out.WriteLine(StringUtils.Join(gscc, "\n"));
					System.Console.Out.WriteLine("-----------------------------------------------");
					// connectivity tests
					connected = GrammaticalStructure.IsConnected(gscc);
					if (!connected && bungRoots == null)
					{
						bungRoots = GrammaticalStructure.GetRoots(gscc);
					}
					allConnected = connected && allConnected;
					if (allConnected)
					{
						System.Console.Out.WriteLine("dependencies form connected graphs.");
					}
					else
					{
						System.Console.Out.WriteLine("dependency graph NOT connected! possible offending nodes: " + bungRoots);
					}
					// test for collapsed dependencies being a tree:
					// make sure at least it doesn't contain cycles (i.e., is a DAG)
					// Do this by reflection so parser doesn't need SemanticGraph and its
					// libraries
					if (m != null)
					{
						try
						{
							// the first arg is null because it's a static method....
							object semGraph = m.Invoke(null, gs, SemanticGraphFactory.Mode.Ccprocessed, GrammaticalStructure.Extras.Maximal, null);
							Type sg = Sharpen.Runtime.GetType("edu.stanford.nlp.semgraph.SemanticGraph");
							MethodInfo mDag = Sharpen.Runtime.GetDeclaredMethod(sg, "isDag");
							bool isDag = (bool)mDag.Invoke(semGraph);
							System.Console.Out.WriteLine("tree dependencies form a DAG: " + isDag);
						}
						catch (Exception e)
						{
							Sharpen.Runtime.PrintStackTrace(e);
						}
					}
				}
				else
				{
					// end of "test" output
					if (parseTree)
					{
						System.Console.Out.WriteLine("============= parse tree =======================");
						tree.PennPrint();
						System.Console.Out.WriteLine();
					}
					if (basic)
					{
						if (collapsed || CCprocessed || collapsedTree || nonCollapsed || enhanced || enhancedPlusPlus)
						{
							System.Console.Out.WriteLine("------------- basic dependencies ---------------");
						}
						if (altDepPrinter == null)
						{
							PrintDependencies(gs, gs.TypedDependencies(GrammaticalStructure.Extras.None), tree, conllx, false, opts.convertToUPOS);
						}
						else
						{
							System.Console.Out.WriteLine(altDepPrinter.DependenciesToString(gs, gs.TypedDependencies(GrammaticalStructure.Extras.None), tree));
						}
					}
					if (nonCollapsed)
					{
						if (basic || CCprocessed || collapsed || collapsedTree)
						{
							System.Console.Out.WriteLine("----------- non-collapsed dependencies (basic + extra) -----------");
						}
						PrintDependencies(gs, gs.AllTypedDependencies(), tree, conllx, extraSep, opts.convertToUPOS);
					}
					if (collapsed)
					{
						if (basic || CCprocessed || collapsedTree || nonCollapsed)
						{
							System.Console.Out.WriteLine("----------- collapsed dependencies -----------");
						}
						PrintDependencies(gs, gs.TypedDependenciesCollapsed(GrammaticalStructure.Extras.Maximal), tree, conllx, false, opts.convertToUPOS);
					}
					if (CCprocessed)
					{
						if (basic || collapsed || collapsedTree || nonCollapsed)
						{
							System.Console.Out.WriteLine("---------- CCprocessed dependencies ----------");
						}
						IList<TypedDependency> deps = gs.TypedDependenciesCCprocessed(GrammaticalStructure.Extras.Maximal);
						if (checkConnected)
						{
							if (!GrammaticalStructure.IsConnected(deps))
							{
								log.Info("Graph is not connected for:");
								log.Info(tree);
								log.Info("possible offending nodes: " + GrammaticalStructure.GetRoots(deps));
							}
						}
						PrintDependencies(gs, deps, tree, conllx, false, opts.convertToUPOS);
					}
					if (collapsedTree)
					{
						if (basic || CCprocessed || collapsed || nonCollapsed)
						{
							System.Console.Out.WriteLine("----------- collapsed dependencies tree -----------");
						}
						PrintDependencies(gs, gs.TypedDependenciesCollapsedTree(), tree, conllx, false, opts.convertToUPOS);
					}
					if (enhanced)
					{
						if (basic || enhancedPlusPlus)
						{
							System.Console.Out.WriteLine("----------- enhanced dependencies tree -----------");
						}
						PrintDependencies(gs, gs.TypedDependenciesEnhanced(), tree, conllx, false, opts.convertToUPOS);
					}
					if (enhancedPlusPlus)
					{
						if (basic || enhanced)
						{
							System.Console.Out.WriteLine("----------- enhanced++ dependencies tree -----------");
						}
						PrintDependencies(gs, gs.TypedDependenciesEnhancedPlusPlus(), tree, conllx, false, opts.convertToUPOS);
					}
					// default use: enhanced++ for UD, CCprocessed for SD (to parallel what happens within the parser)
					if (!basic && !collapsed && !CCprocessed && !collapsedTree && !nonCollapsed && !enhanced && !enhancedPlusPlus)
					{
						// System.out.println("----------- CCprocessed dependencies -----------");
						if (generateOriginalDependencies)
						{
							PrintDependencies(gs, gs.TypedDependenciesCCprocessed(GrammaticalStructure.Extras.Maximal), tree, conllx, false, opts.convertToUPOS);
						}
						else
						{
							PrintDependencies(gs, gs.TypedDependenciesEnhancedPlusPlus(), tree, conllx, false, opts.convertToUPOS);
						}
					}
				}
				if (portray)
				{
					try
					{
						// put up a window showing it
						Type sgu = Sharpen.Runtime.GetType("edu.stanford.nlp.rte.gui.SemanticGraphVisualization");
						MethodInfo mRender = Sharpen.Runtime.GetDeclaredMethod(sgu, "render", typeof(GrammaticalStructure), typeof(string));
						// the first arg is null because it's a static method....
						mRender.Invoke(null, gs, "Collapsed, CC processed deps");
					}
					catch (Exception e)
					{
						throw new Exception("Couldn't use swing to portray semantic graph", e);
					}
				}
			}
		}

		internal class LazyLoadTreesByParsing : IEnumerable<Tree>
		{
			internal readonly Reader reader;

			internal readonly string filename;

			internal readonly bool tokenized;

			internal readonly string encoding;

			internal readonly IFunction<IList<IHasWord>, Tree> lp;

			public LazyLoadTreesByParsing(string filename, string encoding, bool tokenized, IFunction<IList<IHasWord>, Tree> lp)
			{
				// end for
				// end convertTrees
				// todo [cdm 2013]: Take this out and make it a trees class: TreeIterableByParsing
				this.filename = filename;
				this.encoding = encoding;
				this.reader = null;
				this.tokenized = tokenized;
				this.lp = lp;
			}

			public virtual IEnumerator<Tree> GetEnumerator()
			{
				BufferedReader iReader;
				if (reader != null)
				{
					iReader = new BufferedReader(reader);
				}
				else
				{
					try
					{
						iReader = new BufferedReader(new InputStreamReader(new FileInputStream(filename), encoding));
					}
					catch (IOException e)
					{
						throw new Exception(e);
					}
				}
				return new _IEnumerator_944(this, iReader);
			}

			private sealed class _IEnumerator_944 : IEnumerator<Tree>
			{
				public _IEnumerator_944(LazyLoadTreesByParsing _enclosing, BufferedReader iReader)
				{
					this._enclosing = _enclosing;
					this.iReader = iReader;
				}

				internal string line;

				// = null;
				public bool MoveNext()
				{
					if (this.line != null)
					{
						return true;
					}
					else
					{
						try
						{
							this.line = iReader.ReadLine();
						}
						catch (IOException e)
						{
							throw new Exception(e);
						}
						if (this.line == null)
						{
							try
							{
								if (this._enclosing.reader == null)
								{
									iReader.Close();
								}
							}
							catch (Exception e)
							{
								throw new Exception(e);
							}
							return false;
						}
						return true;
					}
				}

				public Tree Current
				{
					get
					{
						if (this.line == null)
						{
							throw new NoSuchElementException();
						}
						Reader lineReader = new StringReader(this.line);
						this.line = null;
						IList<Word> words;
						if (this._enclosing.tokenized)
						{
							words = WhitespaceTokenizer.NewWordWhitespaceTokenizer(lineReader).Tokenize();
						}
						else
						{
							words = PTBTokenizer.NewPTBTokenizer(lineReader).Tokenize();
						}
						if (!words.IsEmpty())
						{
							// the parser throws an exception if told to parse an empty sentence.
							Tree parseTree = this._enclosing.lp.Apply(words);
							return parseTree;
						}
						else
						{
							return new SimpleTree();
						}
					}
				}

				public void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly LazyLoadTreesByParsing _enclosing;

				private readonly BufferedReader iReader;
			}
		}
		// end static class LazyLoadTreesByParsing
	}
}

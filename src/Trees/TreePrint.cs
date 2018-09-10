using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// A class for customizing the print method(s) for a
	/// <c>edu.stanford.nlp.trees.Tree</c>
	/// as the output of the
	/// parser.  This class supports printing in multiple ways and altering
	/// behavior via properties specified at construction.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	/// <author>Galen Andrew</author>
	public class TreePrint
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.TreePrint));

		public const string rootLabelOnlyFormat = "rootSymbolOnly";

		public const string headMark = "=H";

		/// <summary>The legal output tree formats.</summary>
		public static readonly string[] outputTreeFormats = new string[] { "penn", "oneline", rootLabelOnlyFormat, "words", "wordsAndTags", "dependencies", "typedDependencies", "typedDependenciesCollapsed", "latexTree", "xmlTree", "collocations", "semanticGraph"
			, "conllStyleDependencies", "conll2007" };

		private readonly Properties formats;

		private readonly Properties options;

		private readonly bool markHeadNodes;

		private readonly bool lexicalize;

		private readonly bool removeEmpty;

		private readonly bool ptb2text;

		private readonly bool transChinese;

		private readonly bool basicDependencies;

		private readonly bool collapsedDependencies;

		private readonly bool nonCollapsedDependencies;

		private readonly bool nonCollapsedDependenciesSeparated;

		private readonly bool CCPropagatedDependencies;

		private readonly bool treeDependencies;

		private readonly bool includeTags;

		private readonly IHeadFinder hf;

		private readonly ITreebankLanguagePack tlp;

		private readonly WordStemmer stemmer;

		private readonly IPredicate<IDependency<ILabel, ILabel, object>> dependencyFilter;

		private readonly IPredicate<IDependency<ILabel, ILabel, object>> dependencyWordFilter;

		private readonly IGrammaticalStructureFactory gsf;

		/// <summary>Pool use of one WordNetConnection.</summary>
		/// <remarks>
		/// Pool use of one WordNetConnection.  I don't really know if
		/// Dan Bikel's WordNet code is thread safe, but it definitely doesn't
		/// close its files, and too much of our code makes TreePrint objects and
		/// then drops them on the floor, and so we run out of file handles.
		/// That is, if this variable isn't static, code crashes.
		/// Maybe we should change this code to use jwnl(x)?
		/// CDM July 2006.
		/// </remarks>
		private static IWordNetConnection wnc;

		/// <summary>
		/// This PrintWriter is used iff the user doesn't pass one in to a
		/// call to printTree().
		/// </summary>
		/// <remarks>
		/// This PrintWriter is used iff the user doesn't pass one in to a
		/// call to printTree().  It prints to System.out.
		/// </remarks>
		private readonly PrintWriter pw = new PrintWriter(System.Console.Out, true);

		/// <summary>Construct a new TreePrint that will print the given formats.</summary>
		/// <remarks>
		/// Construct a new TreePrint that will print the given formats.
		/// Warning! This is the anglocentric constructor.
		/// It will work correctly only for English.
		/// </remarks>
		/// <param name="formats">The formats to print the tree in.</param>
		public TreePrint(string formats)
			: this(formats, string.Empty, new PennTreebankLanguagePack())
		{
		}

		/// <summary>Make a TreePrint instance with no options specified.</summary>
		public TreePrint(string formats, ITreebankLanguagePack tlp)
			: this(formats, string.Empty, tlp)
		{
		}

		/// <summary>Make a TreePrint instance.</summary>
		/// <remarks>Make a TreePrint instance. This one uses the default tlp headFinder.</remarks>
		public TreePrint(string formats, string options, ITreebankLanguagePack tlp)
			: this(formats, options, tlp, tlp.HeadFinder(), tlp.TypedDependencyHeadFinder())
		{
		}

		/// <summary>Make a TreePrint instance.</summary>
		/// <param name="formatString">
		/// A comma separated list of ways to print each Tree.
		/// For instance, "penn" or "words,typedDependencies".
		/// Known formats are: oneline, penn, latexTree, xmlTree, words,
		/// wordsAndTags, rootSymbolOnly, dependencies,
		/// typedDependencies, typedDependenciesCollapsed,
		/// collocations, semanticGraph, conllStyleDependencies,
		/// conll2007.  The last two are both tab-separated values
		/// formats.  The latter has a lot more columns filled with
		/// underscores. All of them print a blank line after
		/// the output except for oneline.  oneline is also not
		/// meaningful in XML output (it is ignored: use penn instead).
		/// (Use of typedDependenciesCollapsed is deprecated.  It
		/// works but we recommend instead selecting a type of
		/// dependencies using the optionsString argument.  Note in
		/// particular that typedDependenciesCollapsed does not do
		/// CC propagation, which we generally recommend.)
		/// </param>
		/// <param name="optionsString">
		/// Options that additionally specify how trees are to
		/// be printed (for instance, whether stemming should be done).
		/// Known options are:
		/// <c>
		/// stem, lexicalize, markHeadNodes,
		/// xml, removeTopBracket, transChinese,
		/// includePunctuationDependencies, basicDependencies, treeDependencies,
		/// CCPropagatedDependencies, collapsedDependencies, nonCollapsedDependencies,
		/// nonCollapsedDependenciesSeparated, includeTags
		/// </c>
		/// .
		/// </param>
		/// <param name="tlp">
		/// The TreebankLanguagePack used to do things like delete
		/// or ignore punctuation in output
		/// </param>
		/// <param name="hf">The HeadFinder used in printing output</param>
		public TreePrint(string formatString, string optionsString, ITreebankLanguagePack tlp, IHeadFinder hf, IHeadFinder typedDependencyHF)
		{
			// TODO: Add support for makeCopulaHead as an outputFormatOption here.
			// = false;
			// = false;
			// = false;
			formats = StringUtils.StringToProperties(formatString);
			options = StringUtils.StringToProperties(optionsString);
			IList<string> okOutputs = Arrays.AsList(outputTreeFormats);
			foreach (object formObj in formats.Keys)
			{
				string format = (string)formObj;
				if (!okOutputs.Contains(format))
				{
					throw new Exception("Error: output tree format " + format + " not supported. Known formats are: " + okOutputs);
				}
			}
			this.hf = hf;
			this.tlp = tlp;
			bool includePunctuationDependencies;
			includePunctuationDependencies = PropertyToBoolean(this.options, "includePunctuationDependencies");
			bool generateOriginalDependencies = tlp.GenerateOriginalDependencies();
			IPredicate<string> puncFilter;
			if (includePunctuationDependencies)
			{
				dependencyFilter = Filters.AcceptFilter();
				dependencyWordFilter = Filters.AcceptFilter();
				puncFilter = Filters.AcceptFilter();
			}
			else
			{
				dependencyFilter = new Dependencies.DependentPuncTagRejectFilter<ILabel, ILabel, object>(tlp.PunctuationTagRejectFilter());
				dependencyWordFilter = new Dependencies.DependentPuncWordRejectFilter<ILabel, ILabel, object>(tlp.PunctuationWordRejectFilter());
				//Universal dependencies filter punction by tags
				puncFilter = generateOriginalDependencies ? tlp.PunctuationWordRejectFilter() : tlp.PunctuationTagRejectFilter();
			}
			if (PropertyToBoolean(this.options, "stem"))
			{
				stemmer = new WordStemmer();
			}
			else
			{
				stemmer = null;
			}
			if (formats.Contains("typedDependenciesCollapsed") || formats.Contains("typedDependencies") || (formats.Contains("conll2007") && tlp.SupportsGrammaticalStructures()))
			{
				gsf = tlp.GrammaticalStructureFactory(puncFilter, typedDependencyHF);
			}
			else
			{
				gsf = null;
			}
			lexicalize = PropertyToBoolean(this.options, "lexicalize");
			markHeadNodes = PropertyToBoolean(this.options, "markHeadNodes");
			transChinese = PropertyToBoolean(this.options, "transChinese");
			ptb2text = PropertyToBoolean(this.options, "ptb2text");
			removeEmpty = PropertyToBoolean(this.options, "noempty") || ptb2text;
			basicDependencies = PropertyToBoolean(this.options, "basicDependencies");
			collapsedDependencies = PropertyToBoolean(this.options, "collapsedDependencies");
			nonCollapsedDependencies = PropertyToBoolean(this.options, "nonCollapsedDependencies");
			nonCollapsedDependenciesSeparated = PropertyToBoolean(this.options, "nonCollapsedDependenciesSeparated");
			treeDependencies = PropertyToBoolean(this.options, "treeDependencies");
			includeTags = PropertyToBoolean(this.options, "includeTags");
			// if no option format for the dependencies is specified, CCPropagated is the default
			if (!basicDependencies && !collapsedDependencies && !nonCollapsedDependencies && !nonCollapsedDependenciesSeparated && !treeDependencies)
			{
				CCPropagatedDependencies = true;
			}
			else
			{
				CCPropagatedDependencies = PropertyToBoolean(this.options, "CCPropagatedDependencies");
			}
		}

		private static bool PropertyToBoolean(Properties prop, string key)
		{
			return bool.Parse(prop.GetProperty(key));
		}

		/// <summary>Prints the tree to the default PrintWriter.</summary>
		/// <param name="t">The tree to display</param>
		public virtual void PrintTree(Tree t)
		{
			PrintTree(t, pw);
		}

		/// <summary>Prints the tree, with an empty ID.</summary>
		/// <param name="t">The tree to display</param>
		/// <param name="pw">The PrintWriter to print it to</param>
		public virtual void PrintTree(Tree t, PrintWriter pw)
		{
			PrintTree(t, string.Empty, pw);
		}

		/// <summary>Prints the tree according to the options specified for this instance.</summary>
		/// <remarks>
		/// Prints the tree according to the options specified for this instance.
		/// If the tree
		/// <paramref name="t"/>
		/// is
		/// <see langword="null"/>
		/// , then the code prints
		/// a line indicating a skipped tree.  Under the XML option this is
		/// an
		/// <c>s</c>
		/// element with the
		/// <c>skipped</c>
		/// attribute having
		/// value
		/// <see langword="true"/>
		/// , and, otherwise, it is the token
		/// <c>SENTENCE_SKIPPED_OR_UNPARSABLE</c>
		/// .
		/// </remarks>
		/// <param name="t">The tree to display</param>
		/// <param name="id">A name for this sentence</param>
		/// <param name="pw">Where to display the tree</param>
		public virtual void PrintTree(Tree t, string id, PrintWriter pw)
		{
			bool inXml = PropertyToBoolean(options, "xml");
			if (t == null)
			{
				// Parsing didn't succeed.
				if (inXml)
				{
					pw.Print("<s");
					if (!StringUtils.IsNullOrEmpty(id))
					{
						pw.Print(" id=\"" + XMLUtils.EscapeXML(id) + '\"');
					}
					pw.Println(" skipped=\"true\"/>");
					pw.Println();
				}
				else
				{
					pw.Println("SENTENCE_SKIPPED_OR_UNPARSABLE");
				}
			}
			else
			{
				if (inXml)
				{
					pw.Print("<s");
					if (!StringUtils.IsNullOrEmpty(id))
					{
						pw.Print(" id=\"" + XMLUtils.EscapeXML(id) + '\"');
					}
					pw.Println(">");
				}
				PrintTreeInternal(t, pw, inXml);
				if (inXml)
				{
					pw.Println("</s>");
					pw.Println();
				}
			}
		}

		/// <summary>Prints the trees according to the options specified for this instance.</summary>
		/// <remarks>
		/// Prints the trees according to the options specified for this instance.
		/// If the tree
		/// <c>t</c>
		/// is
		/// <see langword="null"/>
		/// , then the code prints
		/// a line indicating a skipped tree.  Under the XML option this is
		/// an
		/// <c>s</c>
		/// element with the
		/// <c>skipped</c>
		/// attribute having
		/// value
		/// <see langword="true"/>
		/// , and, otherwise, it is the token
		/// <c>SENTENCE_SKIPPED_OR_UNPARSABLE</c>
		/// .
		/// </remarks>
		/// <param name="trees">The list of trees to display</param>
		/// <param name="id">A name for this sentence</param>
		/// <param name="pw">Where to dislay the tree</param>
		public virtual void PrintTrees(IList<ScoredObject<Tree>> trees, string id, PrintWriter pw)
		{
			bool inXml = PropertyToBoolean(options, "xml");
			int ii = 0;
			// incremented before used, so first tree is numbered 1
			foreach (ScoredObject<Tree> tp in trees)
			{
				ii++;
				Tree t = tp.Object();
				double score = tp.Score();
				if (t == null)
				{
					// Parsing didn't succeed.
					if (inXml)
					{
						pw.Print("<s");
						if (!StringUtils.IsNullOrEmpty(id))
						{
							pw.Print(" id=\"" + XMLUtils.EscapeXML(id) + '\"');
						}
						pw.Print(" n=\"");
						pw.Print(ii);
						pw.Print('\"');
						pw.Print(" score=\"" + score + '\"');
						pw.Println(" skipped=\"true\"/>");
						pw.Println();
					}
					else
					{
						pw.Println("SENTENCE_SKIPPED_OR_UNPARSABLE Parse #" + ii + " with score " + score);
					}
				}
				else
				{
					if (inXml)
					{
						pw.Print("<s");
						if (!StringUtils.IsNullOrEmpty(id))
						{
							pw.Print(" id=\"");
							pw.Print(XMLUtils.EscapeXML(id));
							pw.Print('\"');
						}
						pw.Print(" n=\"");
						pw.Print(ii);
						pw.Print('\"');
						pw.Print(" score=\"");
						pw.Print(score);
						pw.Print('\"');
						pw.Println(">");
					}
					else
					{
						pw.Print("# Parse ");
						pw.Print(ii);
						pw.Print(" with score ");
						pw.Println(score);
					}
					PrintTreeInternal(t, pw, inXml);
					if (inXml)
					{
						pw.Println("</s>");
						pw.Println();
					}
				}
			}
		}

		/// <summary>Print the internal part of a tree having already identified it.</summary>
		/// <remarks>
		/// Print the internal part of a tree having already identified it.
		/// The ID and outer XML element is printed wrapping this method, but none
		/// of the internal content.
		/// </remarks>
		/// <param name="t">The tree to print. Now known to be non-null</param>
		/// <param name="pw">Where to print it to</param>
		/// <param name="inXml">Whether to use XML style printing</param>
		private void PrintTreeInternal(Tree t, PrintWriter pw, bool inXml)
		{
			Tree outputTree = t;
			if (formats.Contains("conll2007") || removeEmpty)
			{
				outputTree = outputTree.Prune(new BobChrisTreeNormalizer.EmptyFilter());
			}
			if (formats.Contains("words"))
			{
				if (inXml)
				{
					List<ILabel> sentUnstemmed = outputTree.Yield();
					pw.Println("  <words>");
					int i = 1;
					foreach (ILabel w in sentUnstemmed)
					{
						pw.Println("    <word ind=\"" + i + "\">" + XMLUtils.EscapeXML(w.Value()) + "</word>");
						i++;
					}
					pw.Println("  </words>");
				}
				else
				{
					string sent = SentenceUtils.ListToString(outputTree.Yield(), false);
					if (ptb2text)
					{
						pw.Println(PTBTokenizer.Ptb2Text(sent));
					}
					else
					{
						pw.Println(sent);
						pw.Println();
					}
				}
			}
			if (PropertyToBoolean(options, "removeTopBracket"))
			{
				string s = outputTree.Label().Value();
				if (tlp.IsStartSymbol(s))
				{
					if (outputTree.IsUnaryRewrite())
					{
						outputTree = outputTree.FirstChild();
					}
					else
					{
						// It's not quite clear what to do if the tree isn't unary at the top
						// but we then don't strip the ROOT symbol, since that seems closer
						// than losing part of the tree altogether....
						log.Info("TreePrint: can't remove top bracket: not unary");
					}
				}
			}
			// Note that TreePrint is also called on dependency trees that have
			// a word as the root node, and so we don't error if there isn't
			// the root symbol at the top; rather we silently assume that this
			// is a dependency tree!!
			if (stemmer != null)
			{
				stemmer.VisitTree(outputTree);
			}
			if (lexicalize)
			{
				outputTree = Edu.Stanford.Nlp.Trees.Trees.Lexicalize(outputTree, hf);
				Func<Tree, Tree> a = TreeFunctions.GetLabeledToDescriptiveCoreLabelTreeFunction();
				outputTree = a.Apply(outputTree);
			}
			if (formats.Contains("collocations"))
			{
				outputTree = GetCollocationProcessedTree(outputTree, hf);
			}
			if (!lexicalize)
			{
				// delexicalize the output tree
				Func<Tree, Tree> a = TreeFunctions.GetLabeledTreeToStringLabeledTreeFunction();
				outputTree = a.Apply(outputTree);
			}
			Tree outputPSTree = outputTree;
			// variant with head-marking, translations
			if (markHeadNodes)
			{
				outputPSTree = MarkHeadNodes(outputPSTree);
			}
			if (transChinese)
			{
				ITreeTransformer tt = null;
				outputPSTree = tt.TransformTree(outputPSTree);
			}
			if (PropertyToBoolean(options, "xml"))
			{
				if (formats.Contains("wordsAndTags"))
				{
					List<TaggedWord> sent = outputTree.TaggedYield();
					pw.Println("  <words pos=\"true\">");
					int i = 1;
					foreach (TaggedWord tw in sent)
					{
						pw.Println("    <word ind=\"" + i + "\" pos=\"" + XMLUtils.EscapeXML(tw.Tag()) + "\">" + XMLUtils.EscapeXML(tw.Word()) + "</word>");
						i++;
					}
					pw.Println("  </words>");
				}
				if (formats.Contains("penn"))
				{
					pw.Println("  <tree style=\"penn\">");
					StringWriter sw = new StringWriter();
					PrintWriter psw = new PrintWriter(sw);
					outputPSTree.PennPrint(psw);
					pw.Print(XMLUtils.EscapeXML(sw.ToString()));
					pw.Println("  </tree>");
				}
				if (formats.Contains("latexTree"))
				{
					pw.Println("    <tree style=\"latexTrees\">");
					pw.Println(".[");
					StringWriter sw = new StringWriter();
					PrintWriter psw = new PrintWriter(sw);
					outputTree.IndentedListPrint(psw, false);
					pw.Print(XMLUtils.EscapeXML(sw.ToString()));
					pw.Println(".]");
					pw.Println("  </tree>");
				}
				if (formats.Contains("xmlTree"))
				{
					pw.Println("<tree style=\"xml\">");
					outputTree.IndentedXMLPrint(pw, false);
					pw.Println("</tree>");
				}
				if (formats.Contains("dependencies"))
				{
					Tree indexedTree = outputTree.DeepCopy(outputTree.TreeFactory(), CoreLabel.Factory());
					indexedTree.IndexLeaves();
					ICollection<IDependency<ILabel, ILabel, object>> depsSet = indexedTree.MapDependencies(dependencyWordFilter, hf);
					IList<IDependency<ILabel, ILabel, object>> sortedDeps = new List<IDependency<ILabel, ILabel, object>>(depsSet);
					sortedDeps.Sort(Dependencies.DependencyIndexComparator());
					pw.Println("<dependencies style=\"untyped\">");
					foreach (IDependency<ILabel, ILabel, object> d in sortedDeps)
					{
						pw.Println(d.ToString("xml"));
					}
					pw.Println("</dependencies>");
				}
				if (formats.Contains("conll2007") || formats.Contains("conllStyleDependencies"))
				{
					log.Info("The \"conll2007\" and \"conllStyleDependencies\" formats are ignored in xml.");
				}
				if (formats.Contains("typedDependencies"))
				{
					GrammaticalStructure gs = gsf.NewGrammaticalStructure(outputTree);
					if (basicDependencies)
					{
						Print(gs.TypedDependencies(), "xml", includeTags, pw);
					}
					if (nonCollapsedDependencies || nonCollapsedDependenciesSeparated)
					{
						Print(gs.AllTypedDependencies(), "xml", includeTags, pw);
					}
					if (collapsedDependencies)
					{
						Print(gs.TypedDependenciesCollapsed(GrammaticalStructure.Extras.Maximal), "xml", includeTags, pw);
					}
					if (CCPropagatedDependencies)
					{
						Print(gs.TypedDependenciesCCprocessed(), "xml", includeTags, pw);
					}
					if (treeDependencies)
					{
						Print(gs.TypedDependenciesCollapsedTree(), "xml", includeTags, pw);
					}
				}
				if (formats.Contains("typedDependenciesCollapsed"))
				{
					GrammaticalStructure gs = gsf.NewGrammaticalStructure(outputTree);
					Print(gs.TypedDependenciesCCprocessed(), "xml", includeTags, pw);
				}
			}
			else
			{
				// This makes parser require jgrapht.  Bad.
				// if (formats.containsKey("semanticGraph")) {
				//  SemanticGraph sg = SemanticGraph.makeFromTree(outputTree, true, false, false, null);
				//  pw.println(sg.toFormattedString());
				// }
				// non-XML printing
				if (formats.Contains("wordsAndTags"))
				{
					pw.Println(SentenceUtils.ListToString(outputTree.TaggedYield(), false));
					pw.Println();
				}
				if (formats.Contains("oneline"))
				{
					pw.Println(outputPSTree);
				}
				if (formats.Contains("penn"))
				{
					outputPSTree.PennPrint(pw);
					pw.Println();
				}
				if (formats.Contains(rootLabelOnlyFormat))
				{
					pw.Println(outputTree.Label().Value());
				}
				if (formats.Contains("latexTree"))
				{
					pw.Println(".[");
					outputTree.IndentedListPrint(pw, false);
					pw.Println(".]");
				}
				if (formats.Contains("xmlTree"))
				{
					outputTree.IndentedXMLPrint(pw, false);
				}
				if (formats.Contains("dependencies"))
				{
					Tree indexedTree = outputTree.DeepCopy(outputTree.TreeFactory());
					indexedTree.IndexLeaves();
					IList<IDependency<ILabel, ILabel, object>> sortedDeps = GetSortedDeps(indexedTree, dependencyWordFilter);
					foreach (IDependency<ILabel, ILabel, object> d in sortedDeps)
					{
						pw.Println(d.ToString("predicate"));
					}
					pw.Println();
				}
				if (formats.Contains("conll2007"))
				{
					// CoNLL-X 2007 format: http://ilk.uvt.nl/conll/#dataformat
					// wsg: This code should be retained (and not subsumed into EnglishGrammaticalStructure) so
					//      that dependencies for other languages can be printed.
					// wsg2011: This code currently ignores the dependency label since the present implementation
					//          of mapDependencies() returns UnnamedDependency objects.
					// TODO: if there is a GrammaticalStructureFactory available, use that instead of mapDependencies
					Tree it = outputTree.DeepCopy(outputTree.TreeFactory(), CoreLabel.Factory());
					it.IndexLeaves();
					IList<CoreLabel> tagged = it.TaggedLabeledYield();
					IList<IDependency<ILabel, ILabel, object>> sortedDeps = GetSortedDeps(it, Filters.AcceptFilter());
					foreach (IDependency<ILabel, ILabel, object> d in sortedDeps)
					{
						if (!dependencyFilter.Test(d))
						{
							continue;
						}
						if (!(d.Dependent() is IHasIndex) || !(d.Governor() is IHasIndex))
						{
							throw new ArgumentException("Expected labels to have indices");
						}
						IHasIndex dep = (IHasIndex)d.Dependent();
						IHasIndex gov = (IHasIndex)d.Governor();
						int depi = dep.Index();
						int govi = gov.Index();
						CoreLabel w = tagged[depi - 1];
						// Used for both course and fine POS tag fields
						string tag = PTBTokenizer.PtbToken2Text(w.Tag());
						string word = PTBTokenizer.PtbToken2Text(w.Word());
						string lemma = "_";
						string feats = "_";
						string pHead = "_";
						string pDepRel = "_";
						string depRel;
						if (d.Name() != null)
						{
							depRel = d.Name().ToString();
						}
						else
						{
							depRel = (govi == 0) ? "ROOT" : "NULL";
						}
						// The 2007 format has 10 fields
						pw.Printf("%d\t%s\t%s\t%s\t%s\t%s\t%d\t%s\t%s\t%s%n", depi, word, lemma, tag, tag, feats, govi, depRel, pHead, pDepRel);
					}
					pw.Println();
				}
				if (formats.Contains("conllStyleDependencies"))
				{
					// TODO: Rewrite this to output StanfordDependencies using EnglishGrammaticalStructure code
					BobChrisTreeNormalizer tn = new BobChrisTreeNormalizer();
					Tree indexedTree = outputTree.DeepCopy(outputTree.TreeFactory(), CoreLabel.Factory());
					// TODO: Can the below for-loop be deleted now?  (Now that the HeadFinder knows about NML.)
					foreach (Tree node in indexedTree)
					{
						if (node.Label().Value().StartsWith("NML"))
						{
							node.Label().SetValue("NP");
						}
					}
					indexedTree = tn.NormalizeWholeTree(indexedTree, outputTree.TreeFactory());
					indexedTree.IndexLeaves();
					ICollection<IDependency<ILabel, ILabel, object>> depsSet = null;
					bool failed = false;
					try
					{
						depsSet = indexedTree.MapDependencies(dependencyFilter, hf);
					}
					catch (Exception)
					{
						failed = true;
					}
					if (failed)
					{
						log.Info("failed: ");
						log.Info(t);
						log.Info();
					}
					else
					{
						IDictionary<int, int> deps = Generics.NewHashMap();
						foreach (IDependency<ILabel, ILabel, object> dep in depsSet)
						{
							CoreLabel child = (CoreLabel)dep.Dependent();
							CoreLabel parent = (CoreLabel)dep.Governor();
							int childIndex = child.Get(typeof(CoreAnnotations.IndexAnnotation));
							int parentIndex = parent.Get(typeof(CoreAnnotations.IndexAnnotation));
							//            log.info(childIndex+"\t"+parentIndex);
							deps[childIndex] = parentIndex;
						}
						bool foundRoot = false;
						int index = 1;
						foreach (Tree node_1 in indexedTree.GetLeaves())
						{
							string word = node_1.Label().Value();
							string tag = node_1.Parent(indexedTree).Label().Value();
							int parent = 0;
							if (deps.Contains(index))
							{
								parent = deps[index];
							}
							else
							{
								if (foundRoot)
								{
									throw new Exception();
								}
								foundRoot = true;
							}
							pw.Println(index + '\t' + word + '\t' + tag + '\t' + parent);
							index++;
						}
						pw.Println();
					}
				}
				if (formats.Contains("typedDependencies"))
				{
					GrammaticalStructure gs = gsf.NewGrammaticalStructure(outputTree);
					if (basicDependencies)
					{
						Print(gs.TypedDependencies(), includeTags, pw);
					}
					if (nonCollapsedDependencies)
					{
						Print(gs.AllTypedDependencies(), includeTags, pw);
					}
					if (nonCollapsedDependenciesSeparated)
					{
						Print(gs.AllTypedDependencies(), "separator", includeTags, pw);
					}
					if (collapsedDependencies)
					{
						Print(gs.TypedDependenciesCollapsed(GrammaticalStructure.Extras.Maximal), includeTags, pw);
					}
					if (CCPropagatedDependencies)
					{
						Print(gs.TypedDependenciesCCprocessed(), includeTags, pw);
					}
					if (treeDependencies)
					{
						Print(gs.TypedDependenciesCollapsedTree(), includeTags, pw);
					}
				}
				if (formats.Contains("typedDependenciesCollapsed"))
				{
					GrammaticalStructure gs = gsf.NewGrammaticalStructure(outputTree);
					Print(gs.TypedDependenciesCCprocessed(), includeTags, pw);
				}
			}
			// This makes parser require jgrapht.  Bad
			// if (formats.containsKey("semanticGraph")) {
			//  SemanticGraph sg = SemanticGraph.makeFromTree(outputTree, true, false, false, null);
			//  pw.println(sg.toFormattedString());
			// }
			// flush to make sure we see all output
			pw.Flush();
		}

		private IList<IDependency<ILabel, ILabel, object>> GetSortedDeps(Tree tree, IPredicate<IDependency<ILabel, ILabel, object>> filter)
		{
			if (gsf != null)
			{
				GrammaticalStructure gs = gsf.NewGrammaticalStructure(tree);
				ICollection<TypedDependency> deps = gs.TypedDependencies(GrammaticalStructure.Extras.None);
				IList<IDependency<ILabel, ILabel, object>> sortedDeps = new List<IDependency<ILabel, ILabel, object>>();
				foreach (TypedDependency dep in deps)
				{
					sortedDeps.Add(new NamedDependency(dep.Gov(), dep.Dep(), dep.Reln().ToString()));
				}
				sortedDeps.Sort(Dependencies.DependencyIndexComparator());
				return sortedDeps;
			}
			else
			{
				ICollection<IDependency<ILabel, ILabel, object>> depsSet = tree.MapDependencies(filter, hf, "root");
				IList<IDependency<ILabel, ILabel, object>> sortedDeps = new List<IDependency<ILabel, ILabel, object>>(depsSet);
				sortedDeps.Sort(Dependencies.DependencyIndexComparator());
				return sortedDeps;
			}
		}

		/// <summary>
		/// For the input tree, collapse any collocations in it that exist in
		/// WordNet and are contiguous in the tree into a single node.
		/// </summary>
		/// <remarks>
		/// For the input tree, collapse any collocations in it that exist in
		/// WordNet and are contiguous in the tree into a single node.
		/// A single static Wordnet connection is used by all instances of this
		/// class.  Reflection to check that a Wordnet connection exists.  Otherwise
		/// we print an error and do nothing.
		/// </remarks>
		/// <param name="tree">The input tree.  NOTE: This tree is mangled by this method</param>
		/// <param name="hf">The head finder to use</param>
		/// <returns>The collocation collapsed tree</returns>
		private static Tree GetCollocationProcessedTree(Tree tree, IHeadFinder hf)
		{
			lock (typeof(TreePrint))
			{
				if (wnc == null)
				{
					try
					{
						Type cl = Sharpen.Runtime.GetType("edu.stanford.nlp.trees.WordNetInstance");
						wnc = (IWordNetConnection)System.Activator.CreateInstance(cl);
					}
					catch (Exception e)
					{
						log.Info("Couldn't open WordNet Connection.  Aborting collocation detection.");
						log.Info(e);
						wnc = null;
					}
				}
				if (wnc != null)
				{
					CollocationFinder cf = new CollocationFinder(tree, wnc, hf);
					tree = cf.GetMangledTree();
				}
				else
				{
					log.Error("WordNetConnection unavailable for collocations.");
				}
				return tree;
			}
		}

		public virtual void PrintHeader(PrintWriter pw, string charset)
		{
			if (PropertyToBoolean(options, "xml"))
			{
				pw.Println("<?xml version=\"1.0\" encoding=\"" + charset + "\"?>");
				pw.Println("<corpus>");
			}
		}

		public virtual void PrintFooter(PrintWriter pw)
		{
			if (PropertyToBoolean(options, "xml"))
			{
				pw.Println("</corpus>");
			}
		}

		public virtual Tree MarkHeadNodes(Tree t)
		{
			return MarkHeadNodes(t, null);
		}

		private Tree MarkHeadNodes(Tree t, Tree head)
		{
			if (t.IsLeaf())
			{
				return t;
			}
			// don't worry about head-marking leaves
			ILabel newLabel;
			if (t == head)
			{
				newLabel = HeadMark(t.Label());
			}
			else
			{
				newLabel = t.Label();
			}
			Tree newHead = hf.DetermineHead(t);
			return t.TreeFactory().NewTreeNode(newLabel, Arrays.AsList(HeadMarkChildren(t, newHead)));
		}

		private static ILabel HeadMark(ILabel l)
		{
			ILabel l1 = l.LabelFactory().NewLabel(l);
			l1.SetValue(l1.Value() + headMark);
			return l1;
		}

		private Tree[] HeadMarkChildren(Tree t, Tree head)
		{
			Tree[] kids = t.Children();
			Tree[] newKids = new Tree[kids.Length];
			for (int i = 0; i < n; i++)
			{
				newKids[i] = MarkHeadNodes(kids[i], head);
			}
			return newKids;
		}

		/// <summary>This provides a simple main method for calling TreePrint.</summary>
		/// <remarks>
		/// This provides a simple main method for calling TreePrint.
		/// Flags supported are:
		/// <ol>
		/// <li> -format format (like -outputFormat of parser, default "penn")
		/// <li> -options options (like -outputFormatOptions of parser, default "")
		/// <li> -tLP class (the TreebankLanguagePack, default "edu.stanford.nlp.tree.PennTreebankLanguagePack")
		/// <li> -hf class (the HeadFinder, default, the one in the class specified by -tLP)
		/// <li> -useTLPTreeReader (use the treeReaderFactory() inside
		/// the -tLP class; otherwise a PennTreeReader with no normalization is used)
		/// </ol>
		/// The single argument should be a file containing Trees in the format that is either
		/// Penn Treebank s-expressions or as specified by -useTLPTreeReader and the -tLP class,
		/// or if there is no such argument, trees are read from stdin and the program runs as a
		/// filter.
		/// </remarks>
		/// <param name="args">Command line arguments, as above.</param>
		public static void Main(string[] args)
		{
			string format = "penn";
			string options = string.Empty;
			string tlpName = "edu.stanford.nlp.trees.PennTreebankLanguagePack";
			string hfName = null;
			IDictionary<string, int> flagMap = Generics.NewHashMap();
			flagMap["-format"] = 1;
			flagMap["-options"] = 1;
			flagMap["-tLP"] = 1;
			flagMap["-hf"] = 1;
			IDictionary<string, string[]> argsMap = StringUtils.ArgsToMap(args, flagMap);
			args = argsMap[null];
			if (argsMap.Keys.Contains("-format"))
			{
				format = argsMap["-format"][0];
			}
			if (argsMap.Keys.Contains("-options"))
			{
				options = argsMap["-options"][0];
			}
			if (argsMap.Keys.Contains("-tLP"))
			{
				tlpName = argsMap["-tLP"][0];
			}
			if (argsMap.Keys.Contains("-hf"))
			{
				hfName = argsMap["-hf"][0];
			}
			ITreebankLanguagePack tlp;
			try
			{
				tlp = (ITreebankLanguagePack)System.Activator.CreateInstance(Sharpen.Runtime.GetType(tlpName));
			}
			catch (Exception e)
			{
				log.Warning(e);
				return;
			}
			IHeadFinder hf;
			if (hfName != null)
			{
				try
				{
					hf = (IHeadFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(hfName));
				}
				catch (Exception e)
				{
					log.Warning(e);
					return;
				}
			}
			else
			{
				hf = tlp.HeadFinder();
			}
			Edu.Stanford.Nlp.Trees.TreePrint print = new Edu.Stanford.Nlp.Trees.TreePrint(format, options, tlp, (hf == null) ? tlp.HeadFinder() : hf, tlp.TypedDependencyHeadFinder());
			IEnumerator<Tree> i;
			// initialized below
			if (args.Length > 0)
			{
				Treebank trees;
				// initialized below
				ITreeReaderFactory trf;
				if (argsMap.Keys.Contains("-useTLPTreeReader"))
				{
					trf = tlp.TreeReaderFactory();
				}
				else
				{
					trf = null;
				}
				trees = new DiskTreebank(trf);
				trees.LoadPath(args[0]);
				i = trees.GetEnumerator();
			}
			else
			{
				i = tlp.TreeTokenizerFactory().GetTokenizer(new BufferedReader(new InputStreamReader(Runtime.@in)));
			}
			while (i.MoveNext())
			{
				print.PrintTree(i.Current);
			}
		}

		/// <summary>
		/// NO OUTSIDE USE
		/// Returns a String representation of the result of this set of
		/// typed dependencies in a user-specified format.
		/// </summary>
		/// <remarks>
		/// NO OUTSIDE USE
		/// Returns a String representation of the result of this set of
		/// typed dependencies in a user-specified format.
		/// Currently, three formats are supported:
		/// <dl>
		/// <dt>"plain"</dt>
		/// <dd>(Default.)  Formats the dependencies as logical relations,
		/// as exemplified by the following:
		/// <pre>
		/// nsubj(died-1, Sam-0)
		/// tmod(died-1, today-2)
		/// </pre>
		/// </dd>
		/// <dt>"readable"</dt>
		/// <dd>Formats the dependencies as a table with columns
		/// <c>dependent</c>
		/// ,
		/// <c>relation</c>
		/// , and
		/// <c>governor</c>
		/// , as exemplified by the following:
		/// <pre>
		/// Sam-0               nsubj               died-1
		/// today-2             tmod                died-1
		/// </pre>
		/// </dd>
		/// <dt>"xml"</dt>
		/// <dd>Formats the dependencies as XML, as exemplified by the following:
		/// <pre>
		/// &lt;dependencies&gt;
		/// &lt;dep type="nsubj"&gt;
		/// &lt;governor idx="1"&gt;died&lt;/governor&gt;
		/// &lt;dependent idx="0"&gt;Sam&lt;/dependent&gt;
		/// &lt;/dep&gt;
		/// &lt;dep type="tmod"&gt;
		/// &lt;governor idx="1"&gt;died&lt;/governor&gt;
		/// &lt;dependent idx="2"&gt;today&lt;/dependent&gt;
		/// &lt;/dep&gt;
		/// &lt;/dependencies&gt;
		/// </pre>
		/// </dd>
		/// </dl>
		/// </remarks>
		/// <param name="dependencies">The TypedDependencies to print</param>
		/// <param name="format">
		/// a
		/// <c>String</c>
		/// specifying the desired format
		/// </param>
		/// <returns>
		/// a
		/// <c>String</c>
		/// representation of the typed
		/// dependencies in this
		/// <c>GrammaticalStructure</c>
		/// </returns>
		private static string ToString(ICollection<TypedDependency> dependencies, string format, bool includeTags)
		{
			if (format != null && format.Equals("xml"))
			{
				return ToXMLString(dependencies, includeTags);
			}
			else
			{
				if (format != null && format.Equals("readable"))
				{
					return ToReadableString(dependencies);
				}
				else
				{
					if (format != null && format.Equals("separator"))
					{
						return ToString(dependencies, true, includeTags);
					}
					else
					{
						return ToString(dependencies, false, includeTags);
					}
				}
			}
		}

		/// <summary>
		/// NO OUTSIDE USE
		/// Returns a String representation of this set of typed dependencies
		/// as exemplified by the following:
		/// <pre>
		/// tmod(died-6, today-9)
		/// nsubj(died-6, Sam-3)
		/// </pre>
		/// </summary>
		/// <param name="dependencies">The TypedDependencies to print</param>
		/// <param name="extraSep">boolean indicating whether the extra dependencies have to be printed separately, after the basic ones</param>
		/// <returns>
		/// a
		/// <c>String</c>
		/// representation of this set of
		/// typed dependencies
		/// </returns>
		private static string ToString(ICollection<TypedDependency> dependencies, bool extraSep, bool includeTags)
		{
			CoreLabel.OutputFormat labelFormat = (includeTags) ? CoreLabel.OutputFormat.ValueTagIndex : CoreLabel.OutputFormat.ValueIndex;
			StringBuilder buf = new StringBuilder();
			if (extraSep)
			{
				IList<TypedDependency> extraDeps = new List<TypedDependency>();
				foreach (TypedDependency td in dependencies)
				{
					if (td.Extra())
					{
						extraDeps.Add(td);
					}
					else
					{
						buf.Append(td.ToString(labelFormat)).Append('\n');
					}
				}
				// now we print the separator for extra dependencies, and print these if there are some
				if (!extraDeps.IsEmpty())
				{
					buf.Append("======\n");
					foreach (TypedDependency td_1 in extraDeps)
					{
						buf.Append(td_1.ToString(labelFormat)).Append('\n');
					}
				}
			}
			else
			{
				foreach (TypedDependency td in dependencies)
				{
					buf.Append(td.ToString(labelFormat)).Append('\n');
				}
			}
			return buf.ToString();
		}

		// NO OUTSIDE USE
		private static string ToReadableString(ICollection<TypedDependency> dependencies)
		{
			StringBuilder buf = new StringBuilder();
			buf.Append(string.Format("%-20s%-20s%-20s%n", "dep", "reln", "gov"));
			buf.Append(string.Format("%-20s%-20s%-20s%n", "---", "----", "---"));
			foreach (TypedDependency td in dependencies)
			{
				buf.Append(string.Format("%-20s%-20s%-20s%n", td.Dep(), td.Reln(), td.Gov()));
			}
			return buf.ToString();
		}

		// NO OUTSIDE USE
		private static string ToXMLString(ICollection<TypedDependency> dependencies, bool includeTags)
		{
			StringBuilder buf = new StringBuilder("<dependencies style=\"typed\">\n");
			foreach (TypedDependency td in dependencies)
			{
				string reln = td.Reln().ToString();
				string gov = td.Gov().Value();
				string govTag = td.Gov().Tag();
				int govIdx = td.Gov().Index();
				string dep = td.Dep().Value();
				string depTag = td.Dep().Tag();
				int depIdx = td.Dep().Index();
				bool extra = td.Extra();
				// add an attribute if the node is a copy
				// (this happens in collapsing when different prepositions are conjuncts)
				string govCopy = string.Empty;
				int copyGov = td.Gov().CopyCount();
				if (copyGov > 0)
				{
					govCopy = " copy=\"" + copyGov + '\"';
				}
				string depCopy = string.Empty;
				int copyDep = td.Dep().CopyCount();
				if (copyDep > 0)
				{
					depCopy = " copy=\"" + copyDep + '\"';
				}
				string govTagAttribute = (includeTags && govTag != null) ? " tag=\"" + govTag + "\"" : string.Empty;
				string depTagAttribute = (includeTags && depTag != null) ? " tag=\"" + depTag + "\"" : string.Empty;
				// add an attribute if the typed dependency is an extra relation (do not preserve the tree structure)
				string extraAttr = string.Empty;
				if (extra)
				{
					extraAttr = " extra=\"yes\"";
				}
				buf.Append("  <dep type=\"").Append(XMLUtils.EscapeXML(reln)).Append('\"').Append(extraAttr).Append(">\n");
				buf.Append("    <governor idx=\"").Append(govIdx).Append('\"').Append(govCopy).Append(govTagAttribute).Append('>').Append(XMLUtils.EscapeXML(gov)).Append("</governor>\n");
				buf.Append("    <dependent idx=\"").Append(depIdx).Append('\"').Append(depCopy).Append(depTagAttribute).Append('>').Append(XMLUtils.EscapeXML(dep)).Append("</dependent>\n");
				buf.Append("  </dep>\n");
			}
			buf.Append("</dependencies>");
			return buf.ToString();
		}

		/// <summary>
		/// USED BY TREEPRINT AND WSD.SUPWSD.PREPROCESS
		/// Prints this set of typed dependencies to the specified
		/// <c>PrintWriter</c>
		/// .
		/// </summary>
		/// <param name="dependencies">The collection of TypedDependency to print</param>
		/// <param name="pw">Where to print them</param>
		public static void Print(ICollection<TypedDependency> dependencies, bool includeTags, PrintWriter pw)
		{
			pw.Println(ToString(dependencies, false, includeTags));
		}

		/// <summary>
		/// USED BY TREEPRINT
		/// Prints this set of typed dependencies to the specified
		/// <c>PrintWriter</c>
		/// in the specified format.
		/// </summary>
		/// <param name="dependencies">The collection of TypedDependency to print</param>
		/// <param name="format">"xml" or "readable" or other</param>
		/// <param name="pw">Where to print them</param>
		public static void Print(ICollection<TypedDependency> dependencies, string format, bool includeTags, PrintWriter pw)
		{
			pw.Println(ToString(dependencies, format, includeTags));
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Arabic;
using Edu.Stanford.Nlp.International.Morph;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Arabic;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;






namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// A
	/// <see cref="ITreebankLangParserParams"/>
	/// implementing class for
	/// the Penn Arabic Treebank.  The baseline feature set works with either
	/// UTF-8 or Buckwalter input, although the behavior of some unused features depends
	/// on the input encoding.
	/// </summary>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	/// <author>Spence Green</author>
	[System.Serializable]
	public class ArabicTreebankParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ArabicTreebankParserParams));

		private const long serialVersionUID = 8853426784197984653L;

		private readonly StringBuilder optionsString;

		private bool retainNPTmp = false;

		private bool retainNPSbj = false;

		private bool retainPRD = false;

		private bool retainPPClr = false;

		private bool changeNoLabels = false;

		private bool collinizerRetainsPunctuation = false;

		private bool discardX = false;

		private IHeadFinder headFinder;

		private readonly IDictionary<string, Pair<TregexPattern, IFunction<TregexMatcher, string>>> annotationPatterns;

		private readonly IList<Pair<TregexPattern, IFunction<TregexMatcher, string>>> activeAnnotations;

		private static readonly string[] EmptyStringArray = new string[0];

		private MorphoFeatureSpecification morphoSpec = null;

		public ArabicTreebankParserParams()
			: base(new ArabicTreebankLanguagePack())
		{
			{
				//Initialize the headFinder here
				//NOTE (WSG): This method is called by main() to load the test treebank
				//NOTE (WSG): This method is called to load the training treebank
				//Normalize by default
				// Recursively process children depth-first
				// Make the new parent label
				//NOTE (WSG): This is applied to both the best parse by getBestParse()
				//and to the gold eval tree by testOnTreebank()
				// WSGDEBUG -- Annotate POS tags with nominal (grammatical) gender
				//Add manual state splits
				// WSGDEBUG
				//Add morphosyntactic features if this is a POS tag
				//Update the label(s)
				baselineFeatures.Add("-markNounNPargTakers");
				baselineFeatures.Add("-genitiveMark");
				baselineFeatures.Add("-splitPUNC");
				baselineFeatures.Add("-markContainsVerb");
				baselineFeatures.Add("-markStrictBaseNP");
				baselineFeatures.Add("-markOneLevelIdafa");
				baselineFeatures.Add("-splitIN");
				baselineFeatures.Add("-markMasdarVP");
				baselineFeatures.Add("-containsSVO");
				baselineFeatures.Add("-splitCC");
				baselineFeatures.Add("-markFem");
				// Added for MWE experiments
				baselineFeatures.Add("-mwe");
				baselineFeatures.Add("-mweContainsVerb");
			}
			optionsString = new StringBuilder();
			optionsString.Append("ArabicTreebankParserParams\n");
			annotationPatterns = Generics.NewHashMap();
			activeAnnotations = new List<Pair<TregexPattern, IFunction<TregexMatcher, string>>>();
			headFinder = HeadFinder();
			InitializeAnnotationPatterns();
		}

		/// <summary>
		/// Creates an
		/// <see cref="Edu.Stanford.Nlp.Trees.International.Arabic.ArabicTreeReaderFactory"/>
		/// with parameters set
		/// via options passed in from the command line.
		/// </summary>
		/// <returns>
		/// An
		/// <see cref="Edu.Stanford.Nlp.Trees.International.Arabic.ArabicTreeReaderFactory"/>
		/// </returns>
		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new ArabicTreeReaderFactory(retainNPTmp, retainPRD, changeNoLabels, discardX, retainNPSbj, false, retainPPClr);
		}

		public override Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.MemoryTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.DiskTreebank(TreeReaderFactory(), inputEncoding);
		}

		public override IHeadFinder HeadFinder()
		{
			if (headFinder == null)
			{
				headFinder = new ArabicHeadFinder(TreebankLanguagePack());
			}
			return headFinder;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return HeadFinder();
		}

		/// <summary>Returns a lexicon for Arabic.</summary>
		/// <remarks>Returns a lexicon for Arabic.  At the moment this is just a BaseLexicon.</remarks>
		/// <param name="op">Lexicon options</param>
		/// <returns>A Lexicon</returns>
		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (op.lexOptions.uwModelTrainer == null)
			{
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.ArabicUnknownWordModelTrainer";
			}
			if (morphoSpec != null)
			{
				return new FactoredLexicon(op, morphoSpec, wordIndex, tagIndex);
			}
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		/// <summary>Return a default sentence for the language (for testing).</summary>
		/// <remarks>
		/// Return a default sentence for the language (for testing).
		/// The example is in UTF-8.
		/// </remarks>
		public override IList<IHasWord> DefaultTestSentence()
		{
			string[] sent = new string[] { "هو", "استنكر", "الحكومة", "يوم", "امس", "." };
			return SentenceUtils.ToWordList(sent);
		}

		protected internal class ArabicSubcategoryStripper : ITreeTransformer
		{
			protected internal readonly ITreeFactory tf = new LabeledScoredTreeFactory();

			public virtual Tree TransformTree(Tree tree)
			{
				ILabel lab = tree.Label();
				string s = lab.Value();
				if (tree.IsLeaf())
				{
					Tree leaf = this.tf.NewLeaf(lab);
					leaf.SetScore(tree.Score());
					return leaf;
				}
				else
				{
					if (tree.IsPhrasal())
					{
						if (this._enclosing.retainNPTmp && s.StartsWith("NP-TMP"))
						{
							s = "NP-TMP";
						}
						else
						{
							if (this._enclosing.retainNPSbj && s.StartsWith("NP-SBJ"))
							{
								s = "NP-SBJ";
							}
							else
							{
								if (this._enclosing.retainPRD && s.Matches("VB[^P].*PRD.*"))
								{
									s = this._enclosing.tlp.BasicCategory(s);
									s += "-PRD";
								}
								else
								{
									s = this._enclosing.tlp.BasicCategory(s);
								}
							}
						}
					}
					else
					{
						if (tree.IsPreTerminal())
						{
							s = this._enclosing.tlp.BasicCategory(s);
						}
						else
						{
							System.Console.Error.Printf("Encountered a non-leaf/phrasal/pre-terminal node %s\n", s);
							s = this._enclosing.tlp.BasicCategory(s);
						}
					}
				}
				IList<Tree> children = new List<Tree>(tree.NumChildren());
				foreach (Tree child in tree.GetChildrenAsList())
				{
					Tree newChild = this.TransformTree(child);
					children.Add(newChild);
				}
				Tree node = this.tf.NewTreeNode(lab, children);
				node.SetValue(s);
				node.SetScore(tree.Score());
				if (node.Label() is IHasTag)
				{
					((IHasTag)node.Label()).SetTag(s);
				}
				return node;
			}

			internal ArabicSubcategoryStripper(ArabicTreebankParserParams _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly ArabicTreebankParserParams _enclosing;
		}

		/// <summary>
		/// Returns a TreeTransformer that retains categories
		/// according to the following options supported by setOptionFlag:
		/// <p>
		/// <code>-retainNPTmp</code> Retain temporal NP marking on NPs.
		/// </summary>
		/// <remarks>
		/// Returns a TreeTransformer that retains categories
		/// according to the following options supported by setOptionFlag:
		/// <p>
		/// <code>-retainNPTmp</code> Retain temporal NP marking on NPs.
		/// <code>-retainNPSbj</code> Retain NP subject function tags
		/// <code>-markPRDverbs</code> Retain PRD verbs.
		/// </p>
		/// </remarks>
		public override ITreeTransformer SubcategoryStripper()
		{
			return new ArabicTreebankParserParams.ArabicSubcategoryStripper(this);
		}

		/// <summary>The collinizer eliminates punctuation</summary>
		public override ITreeTransformer Collinizer()
		{
			return new TreeCollinizer(tlp, !collinizerRetainsPunctuation, false);
		}

		/// <summary>Stand-in collinizer does nothing to the tree.</summary>
		public override ITreeTransformer CollinizerEvalb()
		{
			return Collinizer();
		}

		public override string[] SisterSplitters()
		{
			return EmptyStringArray;
		}

		private static readonly MorphoFeatureSpecification tagSpec = new ArabicMorphoFeatureSpecification();

		static ArabicTreebankParserParams()
		{
			tagSpec.Activate(MorphoFeatureSpecification.MorphoFeatureType.Ngen);
		}

		public override Tree TransformTree(Tree t, Tree root)
		{
			string baseCat = t.Value();
			StringBuilder newCategory = new StringBuilder();
			foreach (Pair<TregexPattern, IFunction<TregexMatcher, string>> e in activeAnnotations)
			{
				TregexMatcher m = e.First().Matcher(root);
				if (m.MatchesAt(t))
				{
					newCategory.Append(e.Second().Apply(m));
				}
			}
			if (t.IsPreTerminal() && tagSpec != null)
			{
				if (!(t.FirstChild().Label() is CoreLabel) || ((CoreLabel)t.FirstChild().Label()).OriginalText() == null)
				{
					throw new Exception(string.Format("%s: Term lacks morpho analysis: %s", this.GetType().FullName, t.ToString()));
				}
				string morphoStr = ((CoreLabel)t.FirstChild().Label()).OriginalText();
				MorphoFeatures feats = tagSpec.StrToFeatures(morphoStr);
				baseCat = feats.GetTag(baseCat);
			}
			string newCat = baseCat + newCategory.ToString();
			t.SetValue(newCat);
			if (t.IsPreTerminal() && t.Label() is IHasTag)
			{
				((IHasTag)t.Label()).SetTag(newCat);
			}
			return t;
		}

		/// <summary>These are the annotations included when the user selects the -arabicFactored option.</summary>
		private readonly IList<string> baselineFeatures = new List<string>();

		private readonly IList<string> additionalFeatures = new List<string>();

		private void InitializeAnnotationPatterns()
		{
			//This doesn't/can't really pick out genitives, but just any NP following an NN head.
			//wsg2011: In particular, it doesn't select NP complements of PPs, which are also genitive.
			string genitiveNodeTregexString = "@NP > @NP $- /^N/";
			TregexPatternCompiler tregexPatternCompiler = new TregexPatternCompiler(HeadFinder());
			try
			{
				// ******************
				// Baseline features
				// ******************
				annotationPatterns["-genitiveMark"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile(genitiveNodeTregexString), new ArabicTreebankParserParams.SimpleStringFunction("-genitive"));
				annotationPatterns["-markStrictBaseNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP !< (__ < (__ < __))"), new ArabicTreebankParserParams.SimpleStringFunction("-base"));
				// NP with no phrasal node in it
				annotationPatterns["-markOneLevelIdafa"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < (@NP < (__ < __)) !< (/^[^N]/ < (__ < __)) !< (__ < (__ < (__ < __)))"), new ArabicTreebankParserParams.SimpleStringFunction
					("-idafa1"));
				annotationPatterns["-markNounNPargTakers"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NN|NNS|NNP|NNPS|DTNN|DTNNS|DTNNP|DTNNPS ># (@NP < @NP)"), new ArabicTreebankParserParams.SimpleStringFunction
					("-NounNParg"));
				annotationPatterns["-markContainsVerb"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << (/^[CIP]?V/ < (__ !< __))"), new ArabicTreebankParserParams.SimpleStringFunction("-withV"));
				annotationPatterns["-splitIN"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@IN < __=word"), new ArabicTreebankParserParams.AddRelativeNodeFunction("-", "word", false));
				annotationPatterns["-splitPUNC"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@PUNC < __=" + ArabicTreebankParserParams.AnnotatePunctuationFunction2.key), new ArabicTreebankParserParams.AnnotatePunctuationFunction2
					());
				annotationPatterns["-markMasdarVP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@VP|MWVP < /VBG|VN/"), new ArabicTreebankParserParams.SimpleStringFunction("-masdar"));
				annotationPatterns["-containsSVO"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << (@S < (@NP . @VP|MWVP))"), new ArabicTreebankParserParams.SimpleStringFunction("-hasSVO"));
				annotationPatterns["-splitCC"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@CC|CONJ . __=term , __"), new ArabicTreebankParserParams.AddEquivalencedConjNode("-", "term"));
				annotationPatterns["-markFem"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < /ة$/"), new ArabicTreebankParserParams.SimpleStringFunction("-fem"));
				// Added for MWE experiments
				annotationPatterns["-mwe"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ > /MW/=tag"), new ArabicTreebankParserParams.AddRelativeNodeFunction("-", "tag", true));
				annotationPatterns["-mweContainsVerb"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << @MWVP"), new ArabicTreebankParserParams.SimpleStringFunction("-withV"));
				//This version, which uses the PTB equivalence classing, results in slightly lower labeled F1
				//than the splitPUNC feature above, which was included in the COLING2010 evaluation
				annotationPatterns["-splitPUNC2"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@PUNC < __=punc"), new AbstractTreebankParserParams.AnnotatePunctuationFunction("-", "punc"));
				// Label each POS with its parent
				annotationPatterns["-tagPAar"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("!@PUNC < (__ !< __) > __=parent"), new ArabicTreebankParserParams.AddRelativeNodeFunction("-", "parent", true));
				//Didn't work
				annotationPatterns["-splitCC1"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@CC|CONJ < __=term"), new ArabicTreebankParserParams.AddRelativeNodeRegexFunction("-", "term", "-*([^-].*)"));
				annotationPatterns["-splitCC2"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@CC . __=term , __"), new ArabicTreebankParserParams.AddRelativeNodeFunction("-", "term", true));
				annotationPatterns["-idafaJJ1"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP <, (@NN $+ @NP) <+(@NP) @ADJP"), new ArabicTreebankParserParams.SimpleStringFunction("-idafaJJ"));
				annotationPatterns["-idafaJJ2"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP <, (@NN $+ @NP) <+(@NP) @ADJP !<< @SBAR"), new ArabicTreebankParserParams.SimpleStringFunction("-idafaJJ"));
				annotationPatterns["-properBaseNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP !<< @NP < /NNP/ !< @PUNC|CD"), new ArabicTreebankParserParams.SimpleStringFunction("-prop"));
				annotationPatterns["-interrog"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << هل|ماذا|لماذا|اين|متى"), new ArabicTreebankParserParams.SimpleStringFunction("-inter"));
				annotationPatterns["-splitPseudo"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NN < مع|بعد|بين"), new ArabicTreebankParserParams.SimpleStringFunction("-pseudo"));
				annotationPatterns["-nPseudo"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < (@NN < مع|بعد|بين)"), new ArabicTreebankParserParams.SimpleStringFunction("-npseudo"));
				annotationPatterns["-pseudoArg"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < @NP $, (@NN < مع|بعد|بين)"), new ArabicTreebankParserParams.SimpleStringFunction("-pseudoArg"));
				annotationPatterns["-eqL1"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < (@S !< @VP|S)"), new ArabicTreebankParserParams.SimpleStringFunction("-haseq"));
				annotationPatterns["-eqL1L2"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < (__ < (@S !< @VP|S)) | < (@S !< @VP|S)"), new ArabicTreebankParserParams.SimpleStringFunction("-haseq"));
				annotationPatterns["-fullQuote"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < ((@PUNC < \") $ (@PUNC < \"))"), new ArabicTreebankParserParams.SimpleStringFunction("-fq"));
				annotationPatterns["-brokeQuote"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < ((@PUNC < \") !$ (@PUNC < \"))"), new ArabicTreebankParserParams.SimpleStringFunction("-bq"));
				annotationPatterns["-splitVP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@VP <# __=term1"), new ArabicTreebankParserParams.AddRelativeNodeFunction("-", "term1", true));
				annotationPatterns["-markFemP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP|ADJP < (__ < /ة$/)"), new ArabicTreebankParserParams.SimpleStringFunction("-femP"));
				annotationPatterns["-embedSBAR"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP|PP <+(@NP|PP) @SBAR"), new ArabicTreebankParserParams.SimpleStringFunction("-embedSBAR"));
				annotationPatterns["-complexVP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << (@VP < (@NP $ @NP)) > __"), new ArabicTreebankParserParams.SimpleStringFunction("-complexVP"));
				annotationPatterns["-containsJJ"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP <+(@NP) /JJ/"), new ArabicTreebankParserParams.SimpleStringFunction("-hasJJ"));
				annotationPatterns["-markMasdarVP2"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << @VN|VBG"), new ArabicTreebankParserParams.SimpleStringFunction("-masdar"));
				annotationPatterns["-coordNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP|ADJP <+(@NP|ADJP) (@CC|PUNC $- __ $+ __)"), new ArabicTreebankParserParams.SimpleStringFunction("-coordNP"));
				annotationPatterns["-coordWa"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << (@CC , __ < و-)"), new ArabicTreebankParserParams.SimpleStringFunction("-coordWA"));
				annotationPatterns["-NPhasADJP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP <+(@NP) @ADJP"), new ArabicTreebankParserParams.SimpleStringFunction("-NPhasADJP"));
				annotationPatterns["-NPADJP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < @ADJP"), new ArabicTreebankParserParams.SimpleStringFunction("-npadj"));
				annotationPatterns["-NPJJ"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < /JJ/"), new ArabicTreebankParserParams.SimpleStringFunction("-npjj"));
				annotationPatterns["-NPCC"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP <+(@NP) @CC"), new ArabicTreebankParserParams.SimpleStringFunction("-npcc"));
				annotationPatterns["-NPCD"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < @CD"), new ArabicTreebankParserParams.SimpleStringFunction("-npcd"));
				annotationPatterns["-NPNNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < /NNP/"), new ArabicTreebankParserParams.SimpleStringFunction("-npnnp"));
				annotationPatterns["-SVO"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@S < (@NP . @VP)"), new ArabicTreebankParserParams.SimpleStringFunction("-svo"));
				annotationPatterns["-containsSBAR"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << @SBAR"), new ArabicTreebankParserParams.SimpleStringFunction("-hasSBAR"));
				//WSGDEBUG - Template
				//annotationPatterns.put("", new Pair<TregexPattern,Function<TregexMatcher,String>>(tregexPatternCompiler.compile(""), new SimpleStringFunction("")));
				// ************
				// Old and unused features (in various states of repair)
				// *************
				annotationPatterns["-markGappedVP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile("@VP > @VP $- __ $ /^(?:CC|CONJ)/ !< /^V/"), new ArabicTreebankParserParams.SimpleStringFunction("-gappedVP"));
				annotationPatterns["-markGappedVPConjoiners"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile("/^(?:CC|CONJ)/ $ (@VP > @VP $- __ !< /^V/)"), new ArabicTreebankParserParams.SimpleStringFunction("-gappedVP"));
				annotationPatterns["-markGenitiveParent"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile("@NP < (" + genitiveNodeTregexString + ')'), new ArabicTreebankParserParams.SimpleStringFunction("-genitiveParent"));
				// maSdr: this pattern is just a heuristic classification, which matches on
				// various common maSdr pattterns, but probably also matches on a lot of other
				// stuff.  It marks NPs with possible maSdr.
				// Roger's old pattern:
				annotationPatterns["-maSdrMark"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("/^N/ <<# (/^[t\\u062a].+[y\\u064a].$/ > @NN|NOUN|DTNN)"), new ArabicTreebankParserParams.SimpleStringFunction("-maSdr"
					));
				// chris' attempt
				annotationPatterns["-maSdrMark2"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("/^N/ <<# (/^(?:[t\\u062a].+[y\\u064a].|<.{3,}|A.{3,})$/ > @NN|NOUN|DTNN)"), new ArabicTreebankParserParams.SimpleStringFunction
					("-maSdr"));
				annotationPatterns["-maSdrMark3"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("/^N/ <<# (/^(?:[t\\u062a<A].{3,})$/ > @NN|NOUN|DTNN)"), new ArabicTreebankParserParams.SimpleStringFunction("-maSdr"
					));
				annotationPatterns["-maSdrMark4"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("/^N/ <<# (/^(?:[t\\u062a<A].{3,})$/ > (@NN|NOUN|DTNN > (@NP < @NP)))"), new ArabicTreebankParserParams.SimpleStringFunction
					("-maSdr"));
				annotationPatterns["-maSdrMark5"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("/^N/ <<# (__ > (@NN|NOUN|DTNN > (@NP < @NP)))"), new ArabicTreebankParserParams.SimpleStringFunction("-maSdr"));
				annotationPatterns["-mjjMark"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@JJ|DTJJ < /^m/ $+ @PP ># @ADJP "), new ArabicTreebankParserParams.SimpleStringFunction("-mjj"));
				//annotationPatterns.put(markPRDverbString,new Pair<TregexPattern,Function<TregexMatcher,String>>(TregexPattern.compile("/^V[^P]/ > VP $ /-PRD$/"),new SimpleStringFunction("-PRDverb"))); // don't need this pattern anymore, the functionality has been moved to ArabicTreeNormalizer
				// PUNC is PUNC in either raw or Bies POS encoding
				annotationPatterns["-markNPwithSdescendant"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ !< @S << @S [ >> @NP | == @NP ]"), new ArabicTreebankParserParams.SimpleStringFunction("-inNPdominatesS"
					));
				annotationPatterns["-markRightRecursiveNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ <<- @NP [>>- @NP | == @NP]"), new ArabicTreebankParserParams.SimpleStringFunction("-rrNP"));
				annotationPatterns["-markBaseNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP !< @NP !< @VP !< @SBAR !< @ADJP !< @ADVP !< @S !< @QP !< @UCP !< @PP"), new ArabicTreebankParserParams.SimpleStringFunction
					("-base"));
				// allow only a single level of idafa as Base NP; this version works!
				annotationPatterns["-markBaseNPplusIdafa"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP !< (/^[^N]/ < (__ < __)) !< (__ < (__ < (__ < __)))"), new ArabicTreebankParserParams.SimpleStringFunction
					("-base"));
				annotationPatterns["-markTwoLevelIdafa"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < (@NP < (@NP < (__ < __)) !< (/^[^N]/ < (__ < __))) !< (/^[^N]/ < (__ < __)) !< (__ < (__ < (__ < (__ < __))))"
					), new ArabicTreebankParserParams.SimpleStringFunction("-idafa2"));
				annotationPatterns["-markDefiniteIdafa"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < (/^(?:NN|NOUN)/ !$,, /^[^AP]/) <+(/^NP/) (@NP < /^DT/)"), new ArabicTreebankParserParams.SimpleStringFunction
					("-defIdafa"));
				annotationPatterns["-markDefiniteIdafa1"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < (/^(?:NN|NOUN)/ !$,, /^[^AP]/) < (@NP < /^DT/) !< (/^[^N]/ < (__ < __)) !< (__ < (__ < (__ < __)))"), 
					new ArabicTreebankParserParams.SimpleStringFunction("-defIdafa1"));
				annotationPatterns["-markContainsSBAR"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ << @SBAR"), new ArabicTreebankParserParams.SimpleStringFunction("-withSBAR"));
				annotationPatterns["-markPhrasalNodesDominatedBySBAR"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < (__ < __) >> @SBAR"), new ArabicTreebankParserParams.SimpleStringFunction("-domBySBAR"));
				annotationPatterns["-markCoordinateNPs"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < @CC|CONJ"), new ArabicTreebankParserParams.SimpleStringFunction("-coord"));
				//annotationPatterns.put("-markCopularVerbTags",new Pair<TregexPattern,Function<TregexMatcher,String>>(tregexPatternCompiler.compile("/^V/ < " + copularVerbForms),new SimpleStringFunction("-copular")));
				//annotationPatterns.put("-markSBARVerbTags",new Pair<TregexPattern,Function<TregexMatcher,String>>(tregexPatternCompiler.compile("/^V/ < " + sbarVerbForms),new SimpleStringFunction("-SBARverb")));
				annotationPatterns["-markNounAdjVPheads"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NN|NNS|NNP|NNPS|JJ|DTJJ|DTNN|DTNNS|DTNNP|DTNNPS ># @VP"), new ArabicTreebankParserParams.SimpleStringFunction
					("-VHead"));
				// a better version of the below might only mark clitic pronouns, but
				// since most pronouns are clitics, let's try this first....
				annotationPatterns["-markPronominalNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP < @PRP"), new ArabicTreebankParserParams.SimpleStringFunction("-PRP"));
				// try doing coordination parallelism -- there's a lot of that in Arabic (usually the same, sometimes different CC)
				annotationPatterns["-markMultiCC"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < (@CC $.. @CC)"), new ArabicTreebankParserParams.SimpleStringFunction("-multiCC"));
				// this unfortunately didn't seem helpful for capturing CC parallelism; should try again
				annotationPatterns["-markHasCCdaughter"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < @CC"), new ArabicTreebankParserParams.SimpleStringFunction("-CCdtr"));
				annotationPatterns["-markAcronymNP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@NP !<  (__ < (__ < __)) < (/^NN/ < /^.$/ $ (/^NN/ < /^.$/)) !< (__ < /../)"), new ArabicTreebankParserParams.SimpleStringFunction
					("-acro"));
				annotationPatterns["-markAcronymNN"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("/^NN/ < /^.$/ $ (/^NN/ < /^.$/) > (@NP !<  (__ < (__ < __)) !< (__ < /../))"), new ArabicTreebankParserParams.SimpleStringFunction
					("-acro"));
				//PP Specific patterns
				annotationPatterns["-markPPwithPPdescendant"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ !< @PP << @PP [ >> @PP | == @PP ]"), new ArabicTreebankParserParams.SimpleStringFunction("-inPPdominatesPP"
					));
				annotationPatterns["-gpAnnotatePrepositions"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile("/^(?:IN|PREP)$/ > (__ > __=gp)"), new ArabicTreebankParserParams.AddRelativeNodeFunction("^^", "gp", false));
				annotationPatterns["-gpEquivalencePrepositions"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile("/^(?:IN|PREP)$/ > (@PP >+(/^PP/) __=gp)"), new ArabicTreebankParserParams.AddEquivalencedNodeFunction("^^", "gp"
					));
				annotationPatterns["-gpEquivalencePrepositionsVar"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(TregexPattern.Compile("/^(?:IN|PREP)$/ > (@PP >+(/^PP/) __=gp)"), new ArabicTreebankParserParams.AddEquivalencedNodeFunctionVar("^^"
					, "gp"));
				annotationPatterns["-markPPParent"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@PP=max !< @PP"), new ArabicTreebankParserParams.AddRelativeNodeRegexFunction("^^", "max", "^(\\w)"));
				annotationPatterns["-whPP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@PP <- (@SBAR <, /^WH/)"), new ArabicTreebankParserParams.SimpleStringFunction("-whPP"));
				//    annotationPatterns.put("-markTmpPP", new Pair<TregexPattern,Function<TregexMatcher,String>>(tregexPatternCompiler.compile("@PP !<+(__) @PP"),new LexicalCategoryFunction("-TMP",temporalNouns)));
				annotationPatterns["-deflateMin"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("__ < (__ < من)"), new ArabicTreebankParserParams.SimpleStringFunction("-min"));
				annotationPatterns["-v2MarkovIN"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@IN > (@__=p1 > @__=p2)"), new ArabicTreebankParserParams.AddRelativeNodeFunction("^", "p1", "p2", false));
				annotationPatterns["-pleonasticMin"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@PP <, (IN < من) > @S"), new ArabicTreebankParserParams.SimpleStringFunction("-pleo"));
				annotationPatterns["-v2MarkovPP"] = new Pair<TregexPattern, IFunction<TregexMatcher, string>>(tregexPatternCompiler.Compile("@PP > (@__=p1 > @__=p2)"), new ArabicTreebankParserParams.AddRelativeNodeFunction("^", "p1", "p2", false));
			}
			catch (TregexParseException e)
			{
				int nth = annotationPatterns.Count + 1;
				string nthStr = (nth == 1) ? "1st" : ((nth == 2) ? "2nd" : nth + "th");
				log.Info("Parse exception on " + nthStr + " annotation pattern initialization:" + e);
				throw;
			}
		}

		[System.Serializable]
		private class SimpleStringFunction : ISerializableFunction<TregexMatcher, string>
		{
			public SimpleStringFunction(string result)
			{
				this.result = result;
			}

			private string result;

			public virtual string Apply(TregexMatcher tregexMatcher)
			{
				return result;
			}

			public override string ToString()
			{
				return "SimpleStringFunction[" + result + ']';
			}

			private const long serialVersionUID = 1L;
		}

		[System.Serializable]
		private class AddRelativeNodeFunction : ISerializableFunction<TregexMatcher, string>
		{
			private string annotationMark;

			private string key;

			private string key2;

			private bool doBasicCat = false;

			private static readonly ITreebankLanguagePack tlp = new ArabicTreebankLanguagePack();

			public AddRelativeNodeFunction(string annotationMark, string key, bool basicCategory)
			{
				this.annotationMark = annotationMark;
				this.key = key;
				this.key2 = null;
				doBasicCat = basicCategory;
			}

			public AddRelativeNodeFunction(string annotationMark, string key1, string key2, bool basicCategory)
				: this(annotationMark, key1, basicCategory)
			{
				this.key2 = key2;
			}

			public virtual string Apply(TregexMatcher m)
			{
				if (key2 == null)
				{
					return annotationMark + ((doBasicCat) ? tlp.BasicCategory(m.GetNode(key).Label().Value()) : m.GetNode(key).Label().Value());
				}
				else
				{
					string annot1 = (doBasicCat) ? tlp.BasicCategory(m.GetNode(key).Label().Value()) : m.GetNode(key).Label().Value();
					string annot2 = (doBasicCat) ? tlp.BasicCategory(m.GetNode(key2).Label().Value()) : m.GetNode(key2).Label().Value();
					return annotationMark + annot1 + annotationMark + annot2;
				}
			}

			public override string ToString()
			{
				if (key2 == null)
				{
					return "AddRelativeNodeFunction[" + annotationMark + ',' + key + ']';
				}
				else
				{
					return "AddRelativeNodeFunction[" + annotationMark + ',' + key + ',' + key2 + ']';
				}
			}

			private const long serialVersionUID = 1L;
		}

		[System.Serializable]
		private class AddRelativeNodeRegexFunction : ISerializableFunction<TregexMatcher, string>
		{
			private string annotationMark;

			private string key;

			private Pattern pattern;

			private string key2 = null;

			private Pattern pattern2;

			public AddRelativeNodeRegexFunction(string annotationMark, string key, string regex)
			{
				this.annotationMark = annotationMark;
				this.key = key;
				try
				{
					this.pattern = Pattern.Compile(regex);
				}
				catch (PatternSyntaxException pse)
				{
					log.Info("Bad pattern: " + regex);
					pattern = null;
					throw new ArgumentException(pse);
				}
			}

			public virtual string Apply(TregexMatcher m)
			{
				string val = m.GetNode(key).Label().Value();
				if (pattern != null)
				{
					Matcher mat = pattern.Matcher(val);
					if (mat.Find())
					{
						val = mat.Group(1);
					}
				}
				if (key2 != null && pattern2 != null)
				{
					string val2 = m.GetNode(key2).Label().Value();
					Matcher mat2 = pattern2.Matcher(val2);
					if (mat2.Find())
					{
						val = val + annotationMark + mat2.Group(1);
					}
					else
					{
						val = val + annotationMark + val2;
					}
				}
				return annotationMark + val;
			}

			public override string ToString()
			{
				return "AddRelativeNodeRegexFunction[" + annotationMark + ',' + key + ',' + pattern + ']';
			}

			private const long serialVersionUID = 1L;
		}

		/// <summary>This one only distinguishes VP, S and Other (mainly nominal) contexts.</summary>
		/// <remarks>
		/// This one only distinguishes VP, S and Other (mainly nominal) contexts.
		/// These seem the crucial distinctions for Arabic true prepositions,
		/// based on raw counts in data.
		/// </remarks>
		[System.Serializable]
		private class AddEquivalencedNodeFunction : ISerializableFunction<TregexMatcher, string>
		{
			private string annotationMark;

			private string key;

			public AddEquivalencedNodeFunction(string annotationMark, string key)
			{
				this.annotationMark = annotationMark;
				this.key = key;
			}

			public virtual string Apply(TregexMatcher m)
			{
				string node = m.GetNode(key).Label().Value();
				if (node.StartsWith("S"))
				{
					return annotationMark + 'S';
				}
				else
				{
					if (node.StartsWith("V"))
					{
						return annotationMark + 'V';
					}
					else
					{
						return string.Empty;
					}
				}
			}

			public override string ToString()
			{
				return "AddEquivalencedNodeFunction[" + annotationMark + ',' + key + ']';
			}

			private const long serialVersionUID = 1L;
		}

		/// <summary>This one only distinguishes VP, S*, A* versus other (mainly nominal) contexts.</summary>
		[System.Serializable]
		private class AddEquivalencedNodeFunctionVar : ISerializableFunction<TregexMatcher, string>
		{
			private string annotationMark;

			private string key;

			public AddEquivalencedNodeFunctionVar(string annotationMark, string key)
			{
				this.annotationMark = annotationMark;
				this.key = key;
			}

			public virtual string Apply(TregexMatcher m)
			{
				string node = m.GetNode(key).Label().Value();
				// We also tried if (node.startsWith("V")) [var2] and if (node.startsWith("V") || node.startsWith("S")) [var3]. Both seemed markedly worse than the basic function or this var form (which seems a bit better than the basic equiv option).
				if (node.StartsWith("S") || node.StartsWith("V") || node.StartsWith("A"))
				{
					return annotationMark + "VSA";
				}
				else
				{
					return string.Empty;
				}
			}

			public override string ToString()
			{
				return "AddEquivalencedNodeFunctionVar[" + annotationMark + ',' + key + ']';
			}

			private const long serialVersionUID = 1L;
		}

		[System.Serializable]
		private class AnnotatePunctuationFunction2 : ISerializableFunction<TregexMatcher, string>
		{
			internal const string key = "term";

			private static readonly Pattern quote = Pattern.Compile("^\"$");

			public virtual string Apply(TregexMatcher m)
			{
				string punc = m.GetNode(key).Value();
				if (punc.Equals("."))
				{
					return "-fs";
				}
				else
				{
					if (punc.Equals("?"))
					{
						return "-quest";
					}
					else
					{
						if (punc.Equals(","))
						{
							return "-comma";
						}
						else
						{
							if (punc.Equals(":") || punc.Equals(";"))
							{
								return "-colon";
							}
							else
							{
								if (punc.Equals("-LRB-"))
								{
									return "-lrb";
								}
								else
								{
									if (punc.Equals("-RRB-"))
									{
										return "-rrb";
									}
									else
									{
										if (punc.Equals("-PLUS-"))
										{
											return "-plus";
										}
										else
										{
											if (punc.Equals("-"))
											{
												return "-dash";
											}
											else
											{
												if (quote.Matcher(punc).Matches())
												{
													return "-quote";
												}
											}
										}
									}
								}
							}
						}
					}
				}
				//      else if(punc.equals("/"))
				//        return "-slash";
				//      else if(punc.equals("%"))
				//        return "-perc";
				//      else if(punc.contains(".."))
				//        return "-ellipses";
				return string.Empty;
			}

			public override string ToString()
			{
				return "AnnotatePunctuationFunction2";
			}

			private const long serialVersionUID = 1L;
		}

		[System.Serializable]
		private class AddEquivalencedConjNode : ISerializableFunction<TregexMatcher, string>
		{
			private string annotationMark;

			private string key;

			private const string nnTags = "DTNN DTNNP DTNNPS DTNNS NN NNP NNS NNPS";

			private static readonly ICollection<string> nnTagClass = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(nnTags.Split("\\s+"))));

			private const string jjTags = "ADJ_NUM DTJJ DTJJR JJ JJR";

			private static readonly ICollection<string> jjTagClass = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(jjTags.Split("\\s+"))));

			private const string vbTags = "VBD VBP";

			private static readonly ICollection<string> vbTagClass = Java.Util.Collections.UnmodifiableSet(Generics.NewHashSet(Arrays.AsList(vbTags.Split("\\s+"))));

			private static readonly ITreebankLanguagePack tlp = new ArabicTreebankLanguagePack();

			public AddEquivalencedConjNode(string annotationMark, string key)
			{
				this.annotationMark = annotationMark;
				this.key = key;
			}

			public virtual string Apply(TregexMatcher m)
			{
				string node = m.GetNode(key).Value();
				string eqClass = tlp.BasicCategory(node);
				if (nnTagClass.Contains(eqClass))
				{
					eqClass = "noun";
				}
				else
				{
					if (jjTagClass.Contains(eqClass))
					{
						eqClass = "adj";
					}
					else
					{
						if (vbTagClass.Contains(eqClass))
						{
							eqClass = "vb";
						}
					}
				}
				return annotationMark + eqClass;
			}

			public override string ToString()
			{
				return "AddEquivalencedConjNode[" + annotationMark + ',' + key + ']';
			}

			private const long serialVersionUID = 1L;
		}

		/// <summary>Reconfigures active features after a change in the default headfinder.</summary>
		/// <param name="hf"/>
		private void SetHeadFinder(IHeadFinder hf)
		{
			if (hf == null)
			{
				throw new ArgumentException();
			}
			headFinder = hf;
			// Need to re-initialize all patterns due to the new headFinder
			InitializeAnnotationPatterns();
			activeAnnotations.Clear();
			foreach (string key in baselineFeatures)
			{
				Pair<TregexPattern, IFunction<TregexMatcher, string>> p = annotationPatterns[key];
				activeAnnotations.Add(p);
			}
			foreach (string key_1 in additionalFeatures)
			{
				Pair<TregexPattern, IFunction<TregexMatcher, string>> p = annotationPatterns[key_1];
				activeAnnotations.Add(p);
			}
		}

		/// <summary>Configures morpho-syntactic annotations for POS tags.</summary>
		/// <param name="activeFeats">
		/// A comma-separated list of feature values with names according
		/// to MorphoFeatureType.
		/// </param>
		private string SetupMorphoFeatures(string activeFeats)
		{
			string[] feats = activeFeats.Split(",");
			morphoSpec = tlp.MorphFeatureSpec();
			foreach (string feat in feats)
			{
				MorphoFeatureSpecification.MorphoFeatureType fType = MorphoFeatureSpecification.MorphoFeatureType.ValueOf(feat.Trim());
				morphoSpec.Activate(fType);
			}
			return morphoSpec.ToString();
		}

		private void RemoveBaselineFeature(string featName)
		{
			if (baselineFeatures.Contains(featName))
			{
				baselineFeatures.Remove(featName);
				Pair<TregexPattern, IFunction<TregexMatcher, string>> p = annotationPatterns[featName];
				activeAnnotations.Remove(p);
			}
		}

		public override void Display()
		{
			log.Info(optionsString.ToString());
		}

		/// <summary>
		/// Some options for setOptionFlag:
		/// <p>
		/// <code>-retainNPTmp</code> Retain temporal NP marking on NPs.
		/// </summary>
		/// <remarks>
		/// Some options for setOptionFlag:
		/// <p>
		/// <code>-retainNPTmp</code> Retain temporal NP marking on NPs.
		/// <code>-retainNPSbj</code> Retain NP subject function tags
		/// <code>-markGappedVP</code> marked gapped VPs.
		/// <code>-collinizerRetainsPunctuation</code> does what it says.
		/// </p>
		/// </remarks>
		/// <param name="args">flag arguments (usually from commmand line</param>
		/// <param name="i">index at which to begin argument processing</param>
		/// <returns>Index in args array after the last processed index for option</returns>
		public override int SetOptionFlag(string[] args, int i)
		{
			//log.info("Setting option flag: "  + args[i]);
			//lang. specific options
			bool didSomething = false;
			if (annotationPatterns.Keys.Contains(args[i]))
			{
				if (!baselineFeatures.Contains(args[i]))
				{
					additionalFeatures.Add(args[i]);
				}
				Pair<TregexPattern, IFunction<TregexMatcher, string>> p = annotationPatterns[args[i]];
				activeAnnotations.Add(p);
				optionsString.Append("Option " + args[i] + " added annotation pattern " + p.First() + " with annotation " + p.Second() + '\n');
				didSomething = true;
			}
			else
			{
				if (args[i].Equals("-retainNPTmp"))
				{
					optionsString.Append("Retaining NP-TMP marking.\n");
					retainNPTmp = true;
					didSomething = true;
				}
				else
				{
					if (args[i].Equals("-retainNPSbj"))
					{
						optionsString.Append("Retaining NP-SBJ dash tag.\n");
						retainNPSbj = true;
						didSomething = true;
					}
					else
					{
						if (args[i].Equals("-retainPPClr"))
						{
							optionsString.Append("Retaining PP-CLR dash tag.\n");
							retainPPClr = true;
							didSomething = true;
						}
						else
						{
							if (args[i].Equals("-discardX"))
							{
								optionsString.Append("Discarding X trees.\n");
								discardX = true;
								didSomething = true;
							}
							else
							{
								if (args[i].Equals("-changeNoLabels"))
								{
									optionsString.Append("Change no labels.\n");
									changeNoLabels = true;
									didSomething = true;
								}
								else
								{
									if (args[i].Equals("-markPRDverbs"))
									{
										optionsString.Append("Mark PRD.\n");
										retainPRD = true;
										didSomething = true;
									}
									else
									{
										if (args[i].Equals("-collinizerRetainsPunctuation"))
										{
											optionsString.Append("Collinizer retains punctuation.\n");
											collinizerRetainsPunctuation = true;
											didSomething = true;
										}
										else
										{
											if (args[i].Equals("-arabicFactored"))
											{
												foreach (string annotation in baselineFeatures)
												{
													string[] a = new string[] { annotation };
													SetOptionFlag(a, 0);
												}
												didSomething = true;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-headFinder") && (i + 1 < args.Length))
												{
													try
													{
														IHeadFinder hf = (IHeadFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
														SetHeadFinder(hf);
														optionsString.Append("HeadFinder: " + args[i + 1] + "\n");
													}
													catch (Exception e)
													{
														log.Info(e);
														log.Info(this.GetType().FullName + ": Could not load head finder " + args[i + 1]);
													}
													i++;
													didSomething = true;
												}
												else
												{
													if (args[i].Equals("-factlex") && (i + 1 < args.Length))
													{
														string activeFeats = SetupMorphoFeatures(args[++i]);
														optionsString.Append("Factored Lexicon: active features: ").Append(activeFeats);
														//
														//      removeBaselineFeature("-markFem");
														//      optionsString.append(" (removed -markFem)\n");
														didSomething = true;
													}
													else
													{
														if (args[i].Equals("-noFeatures"))
														{
															activeAnnotations.Clear();
															optionsString.Append("Removed all manual features.\n");
															didSomething = true;
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			//wsg2010: The segmenter does not work, but keep this to remember how it was instantiated.
			//    else if (args[i].equals("-arabicTokenizerModel")) {
			//      String modelFile = args[i+1];
			//      try {
			//        WordSegmenter aSeg = (WordSegmenter) Class.forName("edu.stanford.nlp.wordseg.ArabicSegmenter").newInstance();
			//        aSeg.loadSegmenter(modelFile);
			//        System.out.println("aSeg=" + aSeg);
			//        TokenizerFactory<Word> aTF = WordSegmentingTokenizer.factory(aSeg);
			//        ((ArabicTreebankLanguagePack) treebankLanguagePack()).setTokenizerFactory(aTF);
			//      } catch (RuntimeIOException ex) {
			//        log.info("Couldn't load ArabicSegmenter " + modelFile);
			//        ex.printStackTrace();
			//      } catch (Exception e) {
			//        log.info("Couldn't instantiate segmenter: edu.stanford.nlp.wordseg.ArabicSegmenter");
			//        e.printStackTrace();
			//      }
			//      i++; // 2 args
			//      didSomething = true;
			//    }
			if (didSomething)
			{
				i++;
			}
			return i;
		}

		/// <param name="args"/>
		public static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Environment.Exit(-1);
			}
			ArabicTreebankParserParams tlpp = new ArabicTreebankParserParams();
			string[] options = new string[] { "-arabicFactored" };
			tlpp.SetOptionFlag(options, 0);
			DiskTreebank tb = tlpp.DiskTreebank();
			tb.LoadPath(args[0], "txt", false);
			foreach (Tree t in tb)
			{
				foreach (Tree subtree in t)
				{
					tlpp.TransformTree(subtree, t);
				}
				System.Console.Out.WriteLine(t.ToString());
			}
		}
	}
}

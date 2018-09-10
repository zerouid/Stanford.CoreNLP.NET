using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>Parameter file for parsing the Penn Chinese Treebank.</summary>
	/// <remarks>
	/// Parameter file for parsing the Penn Chinese Treebank.  Includes
	/// category enrichments specific to the Penn Chinese Treebank.
	/// </remarks>
	/// <author>Roger Levy</author>
	/// <author>Christopher Manning</author>
	/// <author>Galen Andrew</author>
	[System.Serializable]
	public class ChineseTreebankParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.ChineseTreebankParserParams));

		/// <summary>
		/// The variable ctlp stores the same thing as the tlp variable in
		/// AbstractTreebankParserParams, but pre-cast to be a
		/// ChineseTreebankLanguagePack.
		/// </summary>
		/// <remarks>
		/// The variable ctlp stores the same thing as the tlp variable in
		/// AbstractTreebankParserParams, but pre-cast to be a
		/// ChineseTreebankLanguagePack.
		/// todo [cdm 2013]: Just change to method that casts
		/// </remarks>
		private ChineseTreebankLanguagePack ctlp;

		public bool charTags = false;

		public bool useCharacterBasedLexicon = false;

		public bool useMaxentLexicon = false;

		public bool useMaxentDepGrammar = false;

		public bool segment = false;

		public bool segmentMarkov = false;

		public bool sunJurafskyHeadFinder = false;

		public bool bikelHeadFinder = false;

		public bool discardFrags = false;

		public bool useSimilarWordMap = false;

		public string segmenterClass = null;

		private ILexicon lex;

		private IWordSegmenter segmenter;

		private IHeadFinder headFinder = null;

		private static void PrintlnErr(string s)
		{
			EncodingPrintWriter.Err.Println(s, ChineseTreebankLanguagePack.Encoding);
		}

		public ChineseTreebankParserParams()
			: base(new ChineseTreebankLanguagePack())
		{
			ctlp = (ChineseTreebankLanguagePack)base.TreebankLanguagePack();
		}

		/// <summary>Returns a ChineseHeadFinder</summary>
		public override IHeadFinder HeadFinder()
		{
			if (headFinder == null)
			{
				if (sunJurafskyHeadFinder)
				{
					return new SunJurafskyChineseHeadFinder();
				}
				else
				{
					if (bikelHeadFinder)
					{
						return new BikelChineseHeadFinder();
					}
					else
					{
						return new ChineseHeadFinder();
					}
				}
			}
			else
			{
				return headFinder;
			}
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			if (this.GenerateOriginalDependencies())
			{
				return new ChineseSemanticHeadFinder();
			}
			else
			{
				return new UniversalChineseSemanticHeadFinder();
			}
		}

		/// <summary>Returns a ChineseLexicon</summary>
		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (useCharacterBasedLexicon)
			{
				return lex = new ChineseCharacterBasedLexicon(this, wordIndex, tagIndex);
			}
			// } else if (useMaxentLexicon) {
			// return lex = new ChineseMaxentLexicon();
			if (op.lexOptions.uwModelTrainer == null)
			{
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.ChineseUnknownWordModelTrainer";
			}
			if (segmenterClass != null)
			{
				try
				{
					segmenter = ReflectionLoading.LoadByReflection(segmenterClass, this, wordIndex, tagIndex);
				}
				catch (ReflectionLoading.ReflectionLoadingException)
				{
					segmenter = ReflectionLoading.LoadByReflection(segmenterClass);
				}
			}
			ChineseLexicon clex = new ChineseLexicon(op, this, wordIndex, tagIndex);
			if (segmenter != null)
			{
				lex = new ChineseLexiconAndWordSegmenter(clex, segmenter);
				ctlp.SetTokenizerFactory(WordSegmentingTokenizer.Factory(segmenter));
			}
			else
			{
				lex = clex;
			}
			return lex;
		}

		public override double[] MLEDependencyGrammarSmoothingParams()
		{
			return new double[] { 5.8, 17.7, 6.5, 0.4 };
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			TreeNormalizer tn = new CTBErrorCorrectingTreeNormalizer(splitNPTMP, splitPPTMP, splitXPTMP, charTags);
			return new CTBTreeReaderFactory(tn, discardFrags);
		}

		/// <summary>
		/// Uses a DiskTreebank with a CHTBTokenizer and a
		/// BobChrisTreeNormalizer.
		/// </summary>
		public override Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank()
		{
			string encoding = inputEncoding;
			if (!Java.Nio.Charset.Charset.IsSupported(encoding))
			{
				PrintlnErr("Warning: desired encoding " + encoding + " not accepted. ");
				PrintlnErr("Using UTF-8 to construct DiskTreebank");
				encoding = "UTF-8";
			}
			return new Edu.Stanford.Nlp.Trees.DiskTreebank(TreeReaderFactory(), encoding);
		}

		/// <summary>
		/// Uses a MemoryTreebank with a CHTBTokenizer and a
		/// BobChrisTreeNormalizer
		/// </summary>
		public override Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank()
		{
			string encoding = inputEncoding;
			if (!Java.Nio.Charset.Charset.IsSupported(encoding))
			{
				System.Console.Out.WriteLine("Warning: desired encoding " + encoding + " not accepted. ");
				System.Console.Out.WriteLine("Using UTF-8 to construct MemoryTreebank");
				encoding = "UTF-8";
			}
			return new Edu.Stanford.Nlp.Trees.MemoryTreebank(TreeReaderFactory(), encoding);
		}

		/// <summary>Returns a ChineseCollinizer</summary>
		public override ITreeTransformer Collinizer()
		{
			return new ChineseCollinizer(ctlp);
		}

		/// <summary>Returns a ChineseCollinizer that doesn't delete punctuation</summary>
		public override ITreeTransformer CollinizerEvalb()
		{
			return new ChineseCollinizer(ctlp, false);
		}

		//   /** Returns a <code>ChineseTreebankLanguagePack</code> */
		//   public TreebankLanguagePack treebankLanguagePack() {
		//     return new ChineseTreebankLanguagePack();
		//   }
		/* --------- not used now
		// Automatically generated by ParentAnnotationStats -- preferably don't edit
		private static final String[] splitters1 = new String[] {"VA^VCD", "NP^NP", "NP^VP", "NP^IP", "NP^DNP", "NP^PP", "NP^LCP", "NP^PRN", "NP^QP", "PP^IP", "PP^NP", "NN^FRAG", "NN^NP", "NT^FRAG", "NT^NP", "NR^FRAG", "NR^NP", "VV^FRAG", "VV^VRD", "VV^VCD", "VV^VP", "VV^VSB", "VP^VP", "VP^IP", "VP^DVP", "IP^ROOT", "IP^IP", "IP^CP", "IP^VP", "IP^PP", "IP^NP", "IP^LCP", "CP^IP", "QP^NP", "QP^PP", "QP^VP", "ADVP^CP", "CC^VP", "CC^NP", "CC^IP", "CC^QP", "PU^NP", "PU^FRAG", "PU^IP", "PU^VP", "PU^PRN", "PU^QP", "PU^LST", "NP^DNP~QP", "NT^NP~NP", "NT^NP~VP", "NT^NP~IP", "NT^NP~LCP", "NT^NP~PP", "NT^NP~PRN", "NT^NP~QP", "NT^NP~DNP", "NP^NP~VP", "NP^NP~NP", "NP^NP~IP", "NP^NP~PP", "NP^NP~DNP", "NP^NP~LCP", "NN^NP~VP", "NN^NP~IP", "NN^NP~NP", "NN^NP~PP", "NN^NP~DNP", "NN^NP~LCP", "NN^NP~UCP", "NN^NP~QP", "NN^NP~PRN", "M^CLP~DP", "M^CLP~QP", "M^CLP~NP", "M^CLP~CLP", "CD^QP~VP", "CD^QP~NP", "CD^QP~QP", "CD^QP~LCP", "CD^QP~PP", "CD^QP~DNP", "CD^QP~DP", "CD^QP~IP", "IP^IP~IP", "IP^IP~ROOT", "IP^IP~VP", "LC^LCP~PP", "LC^LCP~IP", "NP^VP~IP", "NP^VP~VP", "AD^ADVP~IP", "AD^ADVP~QP", "AD^ADVP~VP", "AD^ADVP~NP", "AD^ADVP~PP", "AD^ADVP~ADVP", "NP^IP~ROOT", "NP^IP~IP", "NP^IP~CP", "NP^IP~VP", "DT^DP~PP", "P^PP~IP", "P^PP~NP", "P^PP~VP", "P^PP~DNP", "VV^VP~IP", "VV^VP~VP", "PU^IP~IP", "PU^IP~VP", "PU^IP~ROOT", "PU^IP~CP", "JJ^ADJP~DNP", "JJ^ADJP~ADJP", "NR^NP~IP", "NR^NP~NP", "NR^NP~PP", "NR^NP~VP", "NR^NP~DNP", "NR^NP~LCP", "NR^NP~PRN", "NP^PP~NP", "NP^PP~IP", "NP^PP~DNP", "VA^VP~VP", "VA^VP~IP", "VA^VP~DVP", "VP^VP~VP", "VP^VP~IP", "VP^VP~DVP", "VP^IP~ROOT", "VP^IP~CP", "VP^IP~IP", "VP^IP~VP", "VP^IP~PP", "VP^IP~LCP", "VP^IP~NP", "PN^NP~NP", "PN^NP~IP", "PN^NP~PP"};
		private static final String[] splitters2 = new String[] {"VA^VCD", "NP^NP", "NP^VP", "NP^IP", "NP^DNP", "NP^PP", "NP^LCP", "NN^FRAG", "NN^NP", "NT^FRAG", "NT^NP", "NR^FRAG", "NR^NP", "VV^FRAG", "VV^VRD", "VV^VCD", "VV^VP", "VV^VSB", "VP^VP", "VP^IP", "VP^DVP", "IP^ROOT", "IP^IP", "IP^CP", "IP^VP", "IP^PP", "CP^IP", "ADVP^CP", "CC^VP", "CC^NP", "PU^NP", "PU^FRAG", "PU^IP", "PU^VP", "PU^PRN", "NT^NP~NP", "NT^NP~VP", "NT^NP~IP", "NT^NP~LCP", "NT^NP~PP", "NP^NP~VP", "NP^NP~NP", "NP^NP~IP", "NP^NP~PP", "NP^NP~DNP", "NN^NP~VP", "NN^NP~IP", "NN^NP~NP", "NN^NP~PP", "NN^NP~DNP", "NN^NP~LCP", "NN^NP~UCP", "NN^NP~QP", "NN^NP~PRN", "M^CLP~DP", "CD^QP~VP", "CD^QP~NP", "CD^QP~QP", "CD^QP~LCP", "CD^QP~PP", "CD^QP~DNP", "CD^QP~DP", "LC^LCP~PP", "NP^VP~IP", "NP^VP~VP", "AD^ADVP~IP", "AD^ADVP~QP", "AD^ADVP~VP", "AD^ADVP~NP", "NP^IP~ROOT", "NP^IP~IP", "NP^IP~CP", "NP^IP~VP", "P^PP~IP", "P^PP~NP", "P^PP~VP", "P^PP~DNP", "VV^VP~IP", "VV^VP~VP", "PU^IP~IP", "PU^IP~VP", "PU^IP~ROOT", "PU^IP~CP", "JJ^ADJP~DNP", "NR^NP~IP", "NR^NP~NP", "NR^NP~PP", "NR^NP~VP", "NR^NP~DNP", "NR^NP~LCP", "NP^PP~NP", "VA^VP~VP", "VA^VP~IP", "VP^VP~VP", "VP^IP~ROOT", "VP^IP~CP", "VP^IP~IP", "VP^IP~VP", "VP^IP~PP", "VP^IP~LCP", "VP^IP~NP", "PN^NP~NP"};
		private static final String[] splitters3 = new String[] {"NP^NP", "NP^VP", "NP^IP", "NP^DNP", "NP^PP", "NP^LCP", "NN^FRAG", "NN^NP", "NT^FRAG", "NR^FRAG", "NR^NP", "VV^FRAG", "VV^VRD", "VV^VCD", "VV^VP", "VV^VSB", "VP^VP", "VP^IP", "IP^ROOT", "IP^IP", "IP^CP", "IP^VP", "PU^NP", "PU^FRAG", "PU^IP", "PU^VP", "PU^PRN", "NP^NP~VP", "NN^NP~VP", "NN^NP~IP", "NN^NP~NP", "NN^NP~PP", "NN^NP~DNP", "NN^NP~LCP", "M^CLP~DP", "CD^QP~VP", "CD^QP~NP", "CD^QP~QP", "AD^ADVP~IP", "AD^ADVP~QP", "AD^ADVP~VP", "P^PP~IP", "VV^VP~IP", "VV^VP~VP", "PU^IP~IP", "PU^IP~VP", "NR^NP~IP", "NR^NP~NP", "NR^NP~PP", "NR^NP~VP", "VP^VP~VP", "VP^IP~ROOT", "VP^IP~CP", "VP^IP~IP", "VP^IP~VP"};
		private static final String[] splitters4 = new String[] {"NP^NP", "NP^VP", "NP^IP", "NN^FRAG", "NT^FRAG", "NR^FRAG", "VV^FRAG", "VV^VRD", "VV^VCD", "VP^VP", "VP^IP", "IP^ROOT", "IP^IP", "IP^CP", "IP^VP", "PU^NP", "PU^FRAG", "PU^IP", "PU^VP", "NN^NP~VP", "NN^NP~IP", "NN^NP~NP", "NN^NP~PP", "NN^NP~DNP", "NN^NP~LCP", "CD^QP~VP", "CD^QP~NP", "AD^ADVP~IP", "VV^VP~IP", "VV^VP~VP", "NR^NP~IP", "VP^IP~ROOT", "VP^IP~CP"};
		// these ones were built by hand.
		// one can't tag split under FRAG or everything breaks, because of those
		// big flat FRAGs....
		private static final String[] splitters5 = new String[] {"NN^FRAG", "NT^FRAG", "NR^FRAG", "VV^FRAG", "VV^VCD", "VV^VRD", "NP^NP", "VP^VP", "IP^ROOT", "IP^IP", "PU^NP", "PU^FRAG", "P^PP~VP", "P^PP~IP"};
		private static final String[] splitters6 = new String[] {"VV^VCD", "VV^VRD", "NP^NP", "VP^VP", "IP^ROOT", "IP^IP", "PU^NP", "P^PP~VP", "P^PP~IP"};
		private static final String[] splitters7 = new String[] {"NP^NP", "VP^VP", "IP^ROOT", "IP^IP", "PU^NP", "P^PP~VP", "P^PP~IP"};
		private static final String[] splitters8 = new String[] {"IP^ROOT", "IP^IP", "PU^NP", "P^PP~VP", "P^PP~IP"};
		private static final String[] splitters9 = new String[] {"VV^VCD", "VV^VRD", "NP^NP", "VP^VP", "IP^ROOT", "IP^IP", "P^PP~VP", "P^PP~IP"};
		private static final String[] splitters10 = new String[] {"NP^NP", "VP^VP", "IP^ROOT", "IP^IP", "P^PP~VP", "P^PP~IP"};
		
		
		public String[] splitters() {
		switch (selectiveSplitLevel) {
		case 1:
		return splitters1;
		case 2:
		return splitters2;
		case 3:
		return splitters3;
		case 4:
		return splitters4;
		case 5:
		return splitters5;
		case 6:
		return splitters6;
		case 7:
		return splitters7;
		case 8:
		return splitters8;
		case 9:
		return splitters9;
		case 10:
		return splitters10;
		default:
		return new String[0];
		}
		}
		------------------ */
		public override string[] SisterSplitters()
		{
			return StringUtils.EmptyStringArray;
		}

		/// <summary>
		/// transformTree does all language-specific tree
		/// transformations.
		/// </summary>
		/// <remarks>
		/// transformTree does all language-specific tree
		/// transformations. Any parameterizations should be inside the
		/// specific TreebankLangParserParams class.
		/// </remarks>
		public override Tree TransformTree(Tree t, Tree root)
		{
			if (t == null || t.IsLeaf())
			{
				return t;
			}
			string parentStr;
			string grandParentStr;
			Tree parent;
			Tree grandParent;
			if (root == null || t.Equals(root))
			{
				parent = null;
				parentStr = string.Empty;
			}
			else
			{
				parent = t.Parent(root);
				parentStr = parent.Label().Value();
			}
			if (parent == null || parent.Equals(root))
			{
				grandParent = null;
				grandParentStr = string.Empty;
			}
			else
			{
				grandParent = parent.Parent(root);
				grandParentStr = grandParent.Label().Value();
			}
			string baseParentStr = ctlp.BasicCategory(parentStr);
			string baseGrandParentStr = ctlp.BasicCategory(grandParentStr);
			CoreLabel lab = (CoreLabel)t.Label();
			string word = lab.Word();
			string tag = lab.Tag();
			string baseTag = ctlp.BasicCategory(tag);
			string category = lab.Value();
			string baseCategory = ctlp.BasicCategory(category);
			if (t.IsPreTerminal())
			{
				// it's a POS tag
				IList<string> leftAunts = ListBasicCategories(SisterAnnotationStats.LeftSisterLabels(parent, grandParent));
				IList<string> rightAunts = ListBasicCategories(SisterAnnotationStats.RightSisterLabels(parent, grandParent));
				// Chinese-specific punctuation splits
				if (chineseSplitPunct && baseTag.Equals("PU"))
				{
					if (ChineseTreebankLanguagePack.ChineseDouHaoAcceptFilter().Test(word))
					{
						tag = tag + "-DOU";
					}
					else
					{
						// System.out.println("Punct: Split dou hao"); // debugging
						if (ChineseTreebankLanguagePack.ChineseCommaAcceptFilter().Test(word))
						{
							tag = tag + "-COMMA";
						}
						else
						{
							// System.out.println("Punct: Split comma"); // debugging
							if (ChineseTreebankLanguagePack.ChineseColonAcceptFilter().Test(word))
							{
								tag = tag + "-COLON";
							}
							else
							{
								// System.out.println("Punct: Split colon"); // debugging
								if (ChineseTreebankLanguagePack.ChineseQuoteMarkAcceptFilter().Test(word))
								{
									if (chineseSplitPunctLR)
									{
										if (ChineseTreebankLanguagePack.ChineseLeftQuoteMarkAcceptFilter().Test(word))
										{
											tag += "-LQUOTE";
										}
										else
										{
											tag += "-RQUOTE";
										}
									}
									else
									{
										tag = tag + "-QUOTE";
									}
								}
								else
								{
									// System.out.println("Punct: Split quote"); // debugging
									if (ChineseTreebankLanguagePack.ChineseEndSentenceAcceptFilter().Test(word))
									{
										tag = tag + "-ENDSENT";
									}
									else
									{
										// System.out.println("Punct: Split end sent"); // debugging
										if (ChineseTreebankLanguagePack.ChineseParenthesisAcceptFilter().Test(word))
										{
											if (chineseSplitPunctLR)
											{
												if (ChineseTreebankLanguagePack.ChineseLeftParenthesisAcceptFilter().Test(word))
												{
													tag += "-LPAREN";
												}
												else
												{
													tag += "-RPAREN";
												}
											}
											else
											{
												tag += "-PAREN";
											}
										}
										else
										{
											//printlnErr("Just used -PAREN annotation");
											//printlnErr(word);
											//throw new RuntimeException();
											// System.out.println("Punct: Split paren"); // debugging
											if (ChineseTreebankLanguagePack.ChineseDashAcceptFilter().Test(word))
											{
												tag = tag + "-DASH";
											}
											else
											{
												// System.out.println("Punct: Split dash"); // debugging
												if (ChineseTreebankLanguagePack.ChineseOtherAcceptFilter().Test(word))
												{
													tag = tag + "-OTHER";
												}
												else
												{
													PrintlnErr("Unknown punct (you should add it to CTLP): " + tag + " |" + word + "|");
												}
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					if (chineseSplitDouHao)
					{
						// only split DouHao
						if (ChineseTreebankLanguagePack.ChineseDouHaoAcceptFilter().Test(word) && baseTag.Equals("PU"))
						{
							tag = tag + "-DOU";
						}
					}
				}
				// Chinese-specific POS tag splits (non-punctuation)
				if (tagWordSize)
				{
					int l = word.Length;
					tag += "-" + l + "CHARS";
				}
				if (mergeNNVV && baseTag.Equals("NN"))
				{
					tag = "VV";
				}
				if ((chineseSelectiveTagPA || chineseVerySelectiveTagPA) && (baseTag.Equals("CC") || baseTag.Equals("P")))
				{
					tag += "-" + baseParentStr;
				}
				if (chineseSelectiveTagPA && (baseTag.Equals("VV")))
				{
					tag += "-" + baseParentStr;
				}
				if (markMultiNtag && tag.StartsWith("N"))
				{
					for (int i = 0; i < parent.NumChildren(); i++)
					{
						if (parent.Children()[i].Label().Value().StartsWith("N") && parent.Children()[i] != t)
						{
							tag += "=N";
						}
					}
				}
				//System.out.println("Found multi=N rewrite");
				if (markVVsisterIP && baseTag.Equals("VV"))
				{
					bool seenIP = false;
					for (int i = 0; i < parent.NumChildren(); i++)
					{
						if (parent.Children()[i].Label().Value().StartsWith("IP"))
						{
							seenIP = true;
						}
					}
					if (seenIP)
					{
						tag += "-IP";
					}
				}
				//System.out.println("Found VV with IP sister"); // testing
				if (markPsisterIP && baseTag.Equals("P"))
				{
					bool seenIP = false;
					for (int i = 0; i < parent.NumChildren(); i++)
					{
						if (parent.Children()[i].Label().Value().StartsWith("IP"))
						{
							seenIP = true;
						}
					}
					if (seenIP)
					{
						tag += "-IP";
					}
				}
				if (markADgrandchildOfIP && baseTag.Equals("AD") && baseGrandParentStr.Equals("IP"))
				{
					tag += "~IP";
				}
				//System.out.println("Found AD with IP grandparent"); // testing
				if (gpaAD && baseTag.Equals("AD"))
				{
					tag += "~" + baseGrandParentStr;
				}
				//System.out.println("Found AD with grandparent " + grandParentStr); // testing
				if (markPostverbalP && leftAunts.Contains("VV") && baseTag.Equals("P"))
				{
					//System.out.println("Found post-verbal P");
					tag += "^=lVV";
				}
				// end Chinese-specific tag splits
				ILabel label = new CategoryWordTag(tag, word, tag);
				t.SetLabel(label);
			}
			else
			{
				// it's a phrasal category
				Tree[] kids = t.Children();
				// Chinese-specific category splits
				IList<string> leftSis = ListBasicCategories(SisterAnnotationStats.LeftSisterLabels(t, parent));
				IList<string> rightSis = ListBasicCategories(SisterAnnotationStats.RightSisterLabels(t, parent));
				if (paRootDtr && baseParentStr.Equals("ROOT"))
				{
					category += "^ROOT";
				}
				if (markIPsisterBA && baseCategory.Equals("IP"))
				{
					if (leftSis.Contains("BA"))
					{
						category += "=BA";
					}
				}
				//System.out.println("Found IP sister of BA");
				if (dominatesV && HasV(t.PreTerminalYield()))
				{
					// mark categories containing a verb
					category += "-v";
				}
				if (markIPsisterVVorP && baseCategory.Equals("IP"))
				{
					// todo: cdm: is just looking for "P" here selective enough??
					if (leftSis.Contains("VV") || leftSis.Contains("P"))
					{
						category += "=VVP";
					}
				}
				if (markIPsisDEC && baseCategory.Equals("IP"))
				{
					if (rightSis.Contains("DEC"))
					{
						category += "=DEC";
					}
				}
				//System.out.println("Found prenominal IP");
				if (baseCategory.Equals("VP"))
				{
					// cdm 2008: this used to just check that it startsWith("VP"), but
					// I think that was bad because it also matched VPT verb compounds
					if (chineseSplitVP == 3)
					{
						bool hasCC = false;
						bool hasPU = false;
						bool hasLexV = false;
						foreach (Tree kid in kids)
						{
							if (kid.Label().Value().StartsWith("CC"))
							{
								hasCC = true;
							}
							else
							{
								if (kid.Label().Value().StartsWith("PU"))
								{
									hasPU = true;
								}
								else
								{
									if (StringUtils.LookingAt(kid.Label().Value(), "(V[ACEV]|VCD|VCP|VNV|VPT|VRD|VSB)"))
									{
										hasLexV = true;
									}
								}
							}
						}
						if (hasCC || (hasPU && !hasLexV))
						{
							category += "-CRD";
						}
						else
						{
							//System.out.println("Found coordinate VP"); // testing
							if (hasLexV)
							{
								category += "-COMP";
							}
							else
							{
								//System.out.println("Found complementing VP"); // testing
								category += "-ADJT";
							}
						}
					}
					else
					{
						//System.out.println("Found adjoining VP"); // testing
						if (chineseSplitVP >= 1)
						{
							bool hasBA = false;
							foreach (Tree kid in kids)
							{
								if (kid.Label().Value().StartsWith("BA"))
								{
									hasBA = true;
								}
								else
								{
									if (chineseSplitVP == 2 && tlp.BasicCategory(kid.Label().Value()).Equals("VP"))
									{
										foreach (Tree kidkid in kid.Children())
										{
											if (kidkid.Label().Value().StartsWith("BA"))
											{
												hasBA = true;
											}
										}
									}
								}
							}
							if (hasBA)
							{
								category += "-BA";
							}
						}
					}
				}
				if (markVPadjunct && baseParentStr.Equals("VP"))
				{
					// cdm 2008: This used to use startsWith("VP") but changed to baseCat
					Tree[] sisters = parent.Children();
					bool hasVPsister = false;
					bool hasCC = false;
					bool hasPU = false;
					bool hasLexV = false;
					foreach (Tree sister in sisters)
					{
						if (tlp.BasicCategory(sister.Label().Value()).Equals("VP"))
						{
							hasVPsister = true;
						}
						if (sister.Label().Value().StartsWith("CC"))
						{
							hasCC = true;
						}
						if (sister.Label().Value().StartsWith("PU"))
						{
							hasPU = true;
						}
						if (StringUtils.LookingAt(sister.Label().Value(), "(V[ACEV]|VCD|VCP|VNV|VPT|VRD|VSB)"))
						{
							hasLexV = true;
						}
					}
					if (hasVPsister && !(hasCC || hasPU || hasLexV))
					{
						category += "-VPADJ";
					}
				}
				//System.out.println("Found adjunct of VP"); // testing
				if (markNPmodNP && baseCategory.Equals("NP") && baseParentStr.Equals("NP"))
				{
					if (rightSis.Contains("NP"))
					{
						category += "=MODIFIERNP";
					}
				}
				//System.out.println("Found NP modifier of NP"); // testing
				if (markModifiedNP && baseCategory.Equals("NP") && baseParentStr.Equals("NP"))
				{
					if (rightSis.IsEmpty() && (leftSis.Contains("ADJP") || leftSis.Contains("NP") || leftSis.Contains("DNP") || leftSis.Contains("QP") || leftSis.Contains("CP") || leftSis.Contains("PP")))
					{
						category += "=MODIFIEDNP";
					}
				}
				//System.out.println("Found modified NP"); // testing
				if (markNPconj && baseCategory.Equals("NP") && baseParentStr.Equals("NP"))
				{
					if (rightSis.Contains("CC") || rightSis.Contains("PU") || leftSis.Contains("CC") || leftSis.Contains("PU"))
					{
						category += "=CONJ";
					}
				}
				//System.out.println("Found NP conjunct"); // testing
				if (markIPconj && baseCategory.Equals("IP") && baseParentStr.Equals("IP"))
				{
					Tree[] sisters = parent.Children();
					bool hasCommaSis = false;
					bool hasIPSis = false;
					foreach (Tree sister in sisters)
					{
						if (ctlp.BasicCategory(sister.Label().Value()).Equals("PU") && ChineseTreebankLanguagePack.ChineseCommaAcceptFilter().Test(sister.Children()[0].Label().ToString()))
						{
							hasCommaSis = true;
						}
						//System.out.println("Found CommaSis"); // testing
						if (ctlp.BasicCategory(sister.Label().Value()).Equals("IP") && sister != t)
						{
							hasIPSis = true;
						}
					}
					if (hasCommaSis && hasIPSis)
					{
						category += "-CONJ";
					}
				}
				//System.out.println("Found IP conjunct"); // testing
				if (unaryIP && baseCategory.Equals("IP") && t.NumChildren() == 1)
				{
					category += "-U";
				}
				//System.out.println("Found unary IP"); //testing
				if (unaryCP && baseCategory.Equals("CP") && t.NumChildren() == 1)
				{
					category += "-U";
				}
				//System.out.println("Found unary CP"); //testing
				if (splitBaseNP && baseCategory.Equals("NP"))
				{
					if (t.IsPrePreTerminal())
					{
						category = category + "-B";
					}
				}
				//if (Test.verbose) printlnErr(baseCategory + " " + leftSis.toString()); //debugging
				if (markPostverbalPP && leftSis.Contains("VV") && baseCategory.Equals("PP"))
				{
					//System.out.println("Found post-verbal PP");
					category += "=lVV";
				}
				if ((markADgrandchildOfIP || gpaAD) && ListBasicCategories(SisterAnnotationStats.KidLabels(t)).Contains("AD"))
				{
					category += "^ADVP";
				}
				if (markCC)
				{
					// was: for (int i = 0; i < kids.length; i++) {
					// This second version takes an idea from Collins: don't count
					// marginal conjunctions which don't conjoin 2 things.
					for (int i = 1; i < kids.Length - 1; i++)
					{
						string cat2 = kids[i].Label().Value();
						if (cat2.StartsWith("CC"))
						{
							category += "-CC";
						}
					}
				}
				ILabel label = new CategoryWordTag(category, word, tag);
				t.SetLabel(label);
			}
			return t;
		}

		/// <summary>
		/// Chinese: Split the dou hao (a punctuation mark separating
		/// members of a list) from other punctuation.
		/// </summary>
		/// <remarks>
		/// Chinese: Split the dou hao (a punctuation mark separating
		/// members of a list) from other punctuation.  Good but included below.
		/// </remarks>
		public bool chineseSplitDouHao = false;

		/// <summary>
		/// Chinese: split Chinese punctuation several ways, along the lines
		/// of English punctuation plus another category for the dou hao.
		/// </summary>
		/// <remarks>
		/// Chinese: split Chinese punctuation several ways, along the lines
		/// of English punctuation plus another category for the dou hao.  Good.
		/// </remarks>
		public bool chineseSplitPunct = true;

		/// <summary>
		/// Chinese: split left right/paren quote (if chineseSplitPunct is also
		/// true.
		/// </summary>
		/// <remarks>
		/// Chinese: split left right/paren quote (if chineseSplitPunct is also
		/// true.  Only very marginal gains, but seems positive.
		/// </remarks>
		public bool chineseSplitPunctLR = false;

		/// <summary>
		/// Chinese: mark VVs that are sister of IP (communication &amp;
		/// small-clause-taking verbs).
		/// </summary>
		/// <remarks>
		/// Chinese: mark VVs that are sister of IP (communication &amp;
		/// small-clause-taking verbs).  Good: give 0.5%
		/// </remarks>
		public bool markVVsisterIP = true;

		/// <summary>Chinese: mark P's that are sister of IP.</summary>
		/// <remarks>Chinese: mark P's that are sister of IP.  Negative effect</remarks>
		public bool markPsisterIP = true;

		/// <summary>Chinese: mark IP's that are sister of VV or P.</summary>
		/// <remarks>
		/// Chinese: mark IP's that are sister of VV or P.  These rarely
		/// have punctuation. Small positive effect.
		/// </remarks>
		public bool markIPsisterVVorP = true;

		/// <summary>Chinese: mark ADs that are grandchild of IP.</summary>
		public bool markADgrandchildOfIP = false;

		/// <summary>Grandparent annotate all AD.</summary>
		/// <remarks>Grandparent annotate all AD.  Seems slightly negative.</remarks>
		public bool gpaAD = true;

		public bool chineseVerySelectiveTagPA = false;

		public bool chineseSelectiveTagPA = false;

		/// <summary>Chinese: mark IPs that are sister of BA.</summary>
		/// <remarks>
		/// Chinese: mark IPs that are sister of BA.  These always have
		/// overt NP.  Very slightly positive.
		/// </remarks>
		public bool markIPsisterBA = true;

		/// <summary>
		/// Chinese: mark phrases that are adjuncts of VP (these tend to be
		/// locatives/temporals, and have a specific distribution).
		/// </summary>
		/// <remarks>
		/// Chinese: mark phrases that are adjuncts of VP (these tend to be
		/// locatives/temporals, and have a specific distribution).
		/// Necessary even with chineseSplitVP==3 and parent annotation because
		/// parent annotation happens with unsplit parent categories.
		/// Slightly positive.
		/// </remarks>
		public bool markVPadjunct = true;

		/// <summary>Chinese: mark NP modifiers of NPs.</summary>
		/// <remarks>Chinese: mark NP modifiers of NPs. Quite positive (0.5%)</remarks>
		public bool markNPmodNP = true;

		/// <summary>
		/// Chinese: mark left-modified NPs (rightmost NPs with a left-side
		/// mod).
		/// </summary>
		/// <remarks>
		/// Chinese: mark left-modified NPs (rightmost NPs with a left-side
		/// mod).  Slightly positive.
		/// </remarks>
		public bool markModifiedNP = true;

		/// <summary>Chinese: mark NPs that are conjuncts.</summary>
		/// <remarks>Chinese: mark NPs that are conjuncts.  Negative on small set.</remarks>
		public bool markNPconj = true;

		/// <summary>
		/// Chinese: mark nominal tags that are part of multi-nominal
		/// rewrites.
		/// </summary>
		/// <remarks>
		/// Chinese: mark nominal tags that are part of multi-nominal
		/// rewrites.  Doesn't seem any good.
		/// </remarks>
		public bool markMultiNtag = false;

		/// <summary>Chinese: mark IPs that are part of prenominal modifiers.</summary>
		/// <remarks>Chinese: mark IPs that are part of prenominal modifiers. Negative.</remarks>
		public bool markIPsisDEC = true;

		/// <summary>Chinese: mark IPs that are conjuncts.</summary>
		/// <remarks>
		/// Chinese: mark IPs that are conjuncts.  Or those that have
		/// (adjuncts or subjects)
		/// </remarks>
		public bool markIPconj = false;

		public bool markIPadjsubj = false;

		/// <summary>Chinese VP splitting.</summary>
		/// <remarks>
		/// Chinese VP splitting.  0 = none;
		/// 1 = mark with -BA a VP that directly dominates a BA;
		/// 2 = mark with -BA a VP that directly dominates a BA or a VP that
		/// directly dominates a BA
		/// 3 = split VPs into VP-COMP, VP-CRD, VP-ADJ.  (Negative value.)
		/// </remarks>
		public int chineseSplitVP = 3;

		/// <summary>Chinese: merge NN and VV.</summary>
		/// <remarks>Chinese: merge NN and VV.  A lark.</remarks>
		public bool mergeNNVV = false;

		/// <summary>Chinese: unary category marking</summary>
		public bool unaryIP = false;

		public bool unaryCP = false;

		/// <summary>Chinese: parent annotate daughter of root.</summary>
		/// <remarks>
		/// Chinese: parent annotate daughter of root.  Meant only for
		/// selectivesplit=false.
		/// </remarks>
		public bool paRootDtr = false;

		/// <summary>
		/// Chinese: mark P with a left aunt VV, and PP with a left sister
		/// VV.
		/// </summary>
		/// <remarks>
		/// Chinese: mark P with a left aunt VV, and PP with a left sister
		/// VV.  Note that it's necessary to mark both to thread the
		/// context-marking.  Used to identify post-verbal P's, which are
		/// rare.
		/// </remarks>
		public bool markPostverbalP = false;

		public bool markPostverbalPP = false;

		/// <summary>Mark base NPs.</summary>
		/// <remarks>Mark base NPs.  Good.</remarks>
		public bool splitBaseNP = false;

		/// <summary>Annotate tags for number of characters contained.</summary>
		public bool tagWordSize = false;

		/// <summary>Mark phrases which are conjunctions.</summary>
		/// <remarks>
		/// Mark phrases which are conjunctions.
		/// Appears negative, even with 200K words training data.
		/// </remarks>
		public bool markCC = false;

		/// <summary>
		/// Whether to retain the -TMP functional tag on various phrasal
		/// categories.
		/// </summary>
		/// <remarks>
		/// Whether to retain the -TMP functional tag on various phrasal
		/// categories.  On 80K words training, minutely helpful; on 200K
		/// words, best option gives 0.6%.  Doing
		/// splitNPTMP and splitPPTMP (but not splitXPTMP) is best.
		/// </remarks>
		public bool splitNPTMP = false;

		public bool splitPPTMP = false;

		public bool splitXPTMP = false;

		/// <summary>Verbal distance -- mark whether symbol dominates a verb (V*).</summary>
		/// <remarks>
		/// Verbal distance -- mark whether symbol dominates a verb (V*).
		/// Seems bad for Chinese.
		/// </remarks>
		public bool dominatesV = false;

		/// <summary>Parameters specific for creating a ChineseLexicon</summary>
		public const bool DefaultUseGoodTurningUnknownWordModel = false;

		public bool useGoodTuringUnknownWordModel = DefaultUseGoodTurningUnknownWordModel;

		public bool useCharBasedUnknownWordModel = false;

		/// <summary>Parameters for a ChineseCharacterBasedLexicon</summary>
		public double lengthPenalty = 5.0;

		public bool useUnknownCharacterModel = true;

		/// <summary>
		/// penaltyType should be set as follows:
		/// 0: no length penalty
		/// 1: quadratic length penalty
		/// 2: penalty for continuation chars only
		/// TODO: make this an enum
		/// </summary>
		public int penaltyType = 0;

		// using tagPA on Chinese 100k is negative.
		// XXXX upto in testing
		// true
		// Not used now
		// /** How selectively to split. */
		// public int selectiveSplitLevel = 1;
		public override void Display()
		{
			string chineseParams = "Using ChineseTreebankParserParams" + " chineseSplitDouHao=" + chineseSplitDouHao + " chineseSplitPunct=" + chineseSplitPunct + " chineseSplitPunctLR=" + chineseSplitPunctLR + " markVVsisterIP=" + markVVsisterIP + " markVPadjunct="
				 + markVPadjunct + " chineseSplitVP=" + chineseSplitVP + " mergeNNVV=" + mergeNNVV + " unaryIP=" + unaryIP + " unaryCP=" + unaryCP + " paRootDtr=" + paRootDtr + " markPsisterIP=" + markPsisterIP + " markIPsisterVVorP=" + markIPsisterVVorP +
				 " markADgrandchildOfIP=" + markADgrandchildOfIP + " gpaAD=" + gpaAD + " markIPsisterBA=" + markIPsisterBA + " markNPmodNP=" + markNPmodNP + " markNPconj=" + markNPconj + " markMultiNtag=" + markMultiNtag + " markIPsisDEC=" + markIPsisDEC +
				 " markIPconj=" + markIPconj + " markIPadjsubj=" + markIPadjsubj + " markPostverbalP=" + markPostverbalP + " markPostverbalPP=" + markPostverbalPP + " baseNP=" + splitBaseNP + " headFinder=" + (sunJurafskyHeadFinder ? "sunJurafsky" : (bikelHeadFinder
				 ? "bikel" : "levy")) + " discardFrags=" + discardFrags + " dominatesV=" + dominatesV;
			//      + " selSplitLevel=" + selectiveSplitLevel
			PrintlnErr(chineseParams);
		}

		private IList<string> ListBasicCategories(IList<string> l)
		{
			IList<string> l1 = new List<string>();
			foreach (string s in l)
			{
				l1.Add(ctlp.BasicCategory(s));
			}
			return l1;
		}

		// TODO: Rewrite this as general matching predicate
		private static bool HasV(IList tags)
		{
			foreach (object tag in tags)
			{
				string str = tag.ToString();
				if (str.StartsWith("V"))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>Set language-specific options according to flags.</summary>
		/// <remarks>
		/// Set language-specific options according to flags.
		/// This routine should process the option starting in args[i] (which
		/// might potentially be several arguments long if it takes arguments).
		/// It should return the index after the last index it consumed in
		/// processing.  In particular, if it cannot process the current option,
		/// the return value should be i.
		/// </remarks>
		public override int SetOptionFlag(string[] args, int i)
		{
			// [CDM 2008: there are no generic options!] first, see if it's a generic option
			// int j = super.setOptionFlag(args, i);
			// if(i != j) return j;
			//lang. specific options
			// if (args[i].equalsIgnoreCase("-vSelSplitLevel") &&
			//            (i+1 < args.length)) {
			//   selectiveSplitLevel = Integer.parseInt(args[i+1]);
			//   i+=2;
			// } else
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-paRootDtr"))
			{
				paRootDtr = true;
				i += 1;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unaryIP"))
				{
					unaryIP = true;
					i += 1;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-unaryCP"))
					{
						unaryCP = true;
						i += 1;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markPostverbalP"))
						{
							markPostverbalP = true;
							i += 1;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markPostverbalPP"))
							{
								markPostverbalPP = true;
								i += 1;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-baseNP"))
								{
									splitBaseNP = true;
									i += 1;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markVVsisterIP"))
									{
										markVVsisterIP = true;
										i += 1;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markPsisterIP"))
										{
											markPsisterIP = true;
											i += 1;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markIPsisterVVorP"))
											{
												markIPsisterVVorP = true;
												i += 1;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markIPsisterBA"))
												{
													markIPsisterBA = true;
													i += 1;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-dominatesV"))
													{
														dominatesV = true;
														i += 1;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-gpaAD"))
														{
															gpaAD = true;
															i += 1;
														}
														else
														{
															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markVPadjunct"))
															{
																markVPadjunct = bool.ValueOf(args[i + 1]);
																i += 2;
															}
															else
															{
																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markNPmodNP"))
																{
																	markNPmodNP = true;
																	i += 1;
																}
																else
																{
																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markModifiedNP"))
																	{
																		markModifiedNP = true;
																		i += 1;
																	}
																	else
																	{
																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nomarkModifiedNP"))
																		{
																			markModifiedNP = false;
																			i += 1;
																		}
																		else
																		{
																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markNPconj"))
																			{
																				markNPconj = true;
																				i += 1;
																			}
																			else
																			{
																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nomarkNPconj"))
																				{
																					markNPconj = false;
																					i += 1;
																				}
																				else
																				{
																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseSplitPunct"))
																					{
																						chineseSplitPunct = true;
																						i += 1;
																					}
																					else
																					{
																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseSplitPunctLR"))
																						{
																							chineseSplitPunct = true;
																							chineseSplitPunctLR = true;
																							i += 1;
																						}
																						else
																						{
																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseSelectiveTagPA"))
																							{
																								chineseSelectiveTagPA = true;
																								i += 1;
																							}
																							else
																							{
																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseVerySelectiveTagPA"))
																								{
																									chineseVerySelectiveTagPA = true;
																									i += 1;
																								}
																								else
																								{
																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markIPsisDEC"))
																									{
																										markIPsisDEC = true;
																										i += 1;
																									}
																									else
																									{
																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseSplitVP"))
																										{
																											chineseSplitVP = System.Convert.ToInt32(args[i + 1]);
																											i += 2;
																										}
																										else
																										{
																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tagWordSize"))
																											{
																												tagWordSize = true;
																												i += 1;
																											}
																											else
																											{
																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-vanilla"))
																												{
																													chineseSplitDouHao = false;
																													chineseSplitPunct = false;
																													chineseSplitPunctLR = false;
																													markVVsisterIP = false;
																													markPsisterIP = false;
																													markIPsisterVVorP = false;
																													markADgrandchildOfIP = false;
																													gpaAD = false;
																													markIPsisterBA = false;
																													markVPadjunct = false;
																													markNPmodNP = false;
																													markModifiedNP = false;
																													markNPconj = false;
																													markMultiNtag = false;
																													markIPsisDEC = false;
																													markIPconj = false;
																													markIPadjsubj = false;
																													chineseSplitVP = 0;
																													mergeNNVV = false;
																													unaryIP = false;
																													unaryCP = false;
																													paRootDtr = false;
																													markPostverbalP = false;
																													markPostverbalPP = false;
																													splitBaseNP = false;
																													// selectiveSplitLevel = 0;
																													i += 1;
																												}
																												else
																												{
																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-acl03chinese"))
																													{
																														chineseSplitDouHao = false;
																														chineseSplitPunct = true;
																														chineseSplitPunctLR = true;
																														markVVsisterIP = true;
																														markPsisterIP = true;
																														markIPsisterVVorP = true;
																														markADgrandchildOfIP = false;
																														gpaAD = true;
																														markIPsisterBA = false;
																														markVPadjunct = true;
																														markNPmodNP = true;
																														markModifiedNP = true;
																														markNPconj = true;
																														markMultiNtag = false;
																														markIPsisDEC = true;
																														markIPconj = false;
																														markIPadjsubj = false;
																														chineseSplitVP = 3;
																														mergeNNVV = false;
																														unaryIP = true;
																														unaryCP = true;
																														paRootDtr = true;
																														markPostverbalP = false;
																														markPostverbalPP = false;
																														splitBaseNP = false;
																														// selectiveSplitLevel = 0;
																														i += 1;
																													}
																													else
																													{
																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chineseFactored"))
																														{
																															chineseSplitDouHao = false;
																															chineseSplitPunct = true;
																															chineseSplitPunctLR = true;
																															markVVsisterIP = true;
																															markPsisterIP = true;
																															markIPsisterVVorP = true;
																															markADgrandchildOfIP = false;
																															gpaAD = true;
																															markIPsisterBA = true;
																															markVPadjunct = true;
																															markNPmodNP = true;
																															markModifiedNP = true;
																															markNPconj = true;
																															markMultiNtag = false;
																															markIPsisDEC = true;
																															markIPconj = false;
																															markIPadjsubj = false;
																															chineseSplitVP = 3;
																															mergeNNVV = false;
																															unaryIP = true;
																															unaryCP = true;
																															paRootDtr = true;
																															markPostverbalP = false;
																															markPostverbalPP = false;
																															splitBaseNP = false;
																															// selectiveSplitLevel = 0;
																															chineseVerySelectiveTagPA = true;
																															i += 1;
																														}
																														else
																														{
																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-chinesePCFG"))
																															{
																																chineseSplitDouHao = false;
																																chineseSplitPunct = true;
																																chineseSplitPunctLR = true;
																																markVVsisterIP = true;
																																markPsisterIP = false;
																																markIPsisterVVorP = true;
																																markADgrandchildOfIP = false;
																																gpaAD = false;
																																markIPsisterBA = true;
																																markVPadjunct = true;
																																markNPmodNP = true;
																																markModifiedNP = true;
																																markNPconj = false;
																																markMultiNtag = false;
																																markIPsisDEC = false;
																																markIPconj = false;
																																markIPadjsubj = false;
																																chineseSplitVP = 0;
																																mergeNNVV = false;
																																unaryIP = false;
																																unaryCP = false;
																																paRootDtr = false;
																																markPostverbalP = false;
																																markPostverbalPP = false;
																																splitBaseNP = false;
																																// selectiveSplitLevel = 0;
																																chineseVerySelectiveTagPA = true;
																																i += 1;
																															}
																															else
																															{
																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-sunHead"))
																																{
																																	sunJurafskyHeadFinder = true;
																																	i++;
																																}
																																else
																																{
																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-bikelHead"))
																																	{
																																		bikelHeadFinder = true;
																																		i++;
																																	}
																																	else
																																	{
																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-discardFrags"))
																																		{
																																			discardFrags = true;
																																			i++;
																																		}
																																		else
																																		{
																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-charLex"))
																																			{
																																				useCharacterBasedLexicon = true;
																																				i++;
																																			}
																																			else
																																			{
																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-charUnk"))
																																				{
																																					useCharBasedUnknownWordModel = true;
																																					i++;
																																				}
																																				else
																																				{
																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-rad"))
																																					{
																																						useUnknownCharacterModel = true;
																																						i++;
																																					}
																																					else
																																					{
																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-lengthPenalty") && (i + 1 < args.Length))
																																						{
																																							lengthPenalty = double.Parse(args[i + 1]);
																																							i += 2;
																																						}
																																						else
																																						{
																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-penaltyType") && (i + 1 < args.Length))
																																							{
																																								penaltyType = System.Convert.ToInt32(args[i + 1]);
																																								i += 2;
																																							}
																																							else
																																							{
																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-gtUnknown"))
																																								{
																																									useGoodTuringUnknownWordModel = true;
																																									i++;
																																								}
																																								else
																																								{
																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxentUnk"))
																																									{
																																										// useMaxentUnknownWordModel = true;
																																										i++;
																																									}
																																									else
																																									{
																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-tuneSigma"))
																																										{
																																											// ChineseMaxentLexicon.tuneSigma = true;
																																											i++;
																																										}
																																										else
																																										{
																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-trainCountThresh") && (i + 1 < args.Length))
																																											{
																																												// ChineseMaxentLexicon.trainCountThreshold = Integer.parseInt(args[i + 1]);
																																												i += 2;
																																											}
																																											else
																																											{
																																												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markCC"))
																																												{
																																													markCC = true;
																																													i++;
																																												}
																																												else
																																												{
																																													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-segmentMarkov") || Sharpen.Runtime.EqualsIgnoreCase(args[i], "-segmentWords"))
																																													{
																																														segment = true;
																																														segmentMarkov = true;
																																														segmenterClass = "edu.stanford.nlp.parser.lexparser.ChineseMarkovWordSegmenter";
																																														i++;
																																													}
																																													else
																																													{
																																														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-segmentMaxMatch"))
																																														{
																																															segment = true;
																																															segmentMarkov = false;
																																															segmenterClass = "edu.stanford.nlp.parser.lexparser.MaxMatchSegmenter";
																																															i++;
																																														}
																																														else
																																														{
																																															if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-segmentDPMaxMatch"))
																																															{
																																																segment = true;
																																																segmentMarkov = false;
																																																segmenterClass = "edu.stanford.nlp.wordseg.MaxMatchSegmenter";
																																																i++;
																																															}
																																															else
																																															{
																																																if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxentLex"))
																																																{
																																																	// useMaxentLexicon = true;
																																																	i++;
																																																}
																																																else
																																																{
																																																	if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-fixUnkFunctionWords"))
																																																	{
																																																		// ChineseMaxentLexicon.fixUnkFunctionWords = true;
																																																		i++;
																																																	}
																																																	else
																																																	{
																																																		if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-similarWordSmoothing"))
																																																		{
																																																			useSimilarWordMap = true;
																																																			i++;
																																																		}
																																																		else
																																																		{
																																																			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxentLexSeenTagsOnly"))
																																																			{
																																																				// useMaxentLexicon = true;
																																																				// ChineseMaxentLexicon.seenTagsOnly = true;
																																																				i++;
																																																			}
																																																			else
																																																			{
																																																				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxentLexFeatLevel") && (i + 1 < args.Length))
																																																				{
																																																					// ChineseMaxentLexicon.featureLevel = Integer.parseInt(args[i + 1]);
																																																					i += 2;
																																																				}
																																																				else
																																																				{
																																																					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxentDepGrammarFeatLevel") && (i + 1 < args.Length))
																																																					{
																																																						depGramFeatureLevel = System.Convert.ToInt32(args[i + 1]);
																																																						i += 2;
																																																					}
																																																					else
																																																					{
																																																						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-maxentDepGrammar"))
																																																						{
																																																							// useMaxentDepGrammar = true;
																																																							i++;
																																																						}
																																																						else
																																																						{
																																																							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitNPTMP"))
																																																							{
																																																								splitNPTMP = true;
																																																								i++;
																																																							}
																																																							else
																																																							{
																																																								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitPPTMP"))
																																																								{
																																																									splitPPTMP = true;
																																																									i++;
																																																								}
																																																								else
																																																								{
																																																									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-splitXPTMP"))
																																																									{
																																																										splitXPTMP = true;
																																																										i++;
																																																									}
																																																									else
																																																									{
																																																										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-segmenter"))
																																																										{
																																																											segment = true;
																																																											segmentMarkov = false;
																																																											segmenterClass = args[i + 1];
																																																											i += 2;
																																																										}
																																																										else
																																																										{
																																																											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-headFinder") && (i + 1 < args.Length))
																																																											{
																																																												try
																																																												{
																																																													headFinder = (IHeadFinder)System.Activator.CreateInstance(Sharpen.Runtime.GetType(args[i + 1]));
																																																												}
																																																												catch (Exception e)
																																																												{
																																																													log.Info(e);
																																																													log.Info(this.GetType().FullName + ": Could not load head finder " + args[i + 1]);
																																																													throw new Exception(e);
																																																												}
																																																												i += 2;
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
											}
										}
									}
								}
							}
						}
					}
				}
			}
			return i;
		}

		private int depGramFeatureLevel = 0;

		public override IExtractor<IDependencyGrammar> DependencyGrammarExtractor(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			/* ----------
			if (useMaxentDepGrammar) {
			return new Extractor() {
			public Object extract(Collection<Tree> trees) {
			ChineseWordFeatureExtractor wfe = new ChineseWordFeatureExtractor(trees);
			ChineseWordFeatureExtractor wfe2 = new ChineseWordFeatureExtractor(trees);
			wfe.setFeatureLevel(2);
			wfe2.turnOffWordFeatures = true;
			wfe2.setFeatureLevel(depGramFeatureLevel);
			MaxentDependencyGrammar dg = new MaxentDependencyGrammar(op.tlpParams, wfe, wfe2, true, false, false);
			dg.train(trees);
			return dg;
			}
			
			public Object extract(Iterator<Tree> iterator, Function<Tree, Tree> f) {
			throw new UnsupportedOperationException();
			}
			};
			} else ------- */
			if (useSimilarWordMap)
			{
				return new _MLEDependencyGrammarExtractor_1192(this, op, wordIndex, tagIndex);
			}
			else
			{
				return new MLEDependencyGrammarExtractor(op, wordIndex, tagIndex);
			}
		}

		private sealed class _MLEDependencyGrammarExtractor_1192 : MLEDependencyGrammarExtractor
		{
			public _MLEDependencyGrammarExtractor_1192(ChineseTreebankParserParams _enclosing, Options baseArg1, IIndex<string> baseArg2, IIndex<string> baseArg3)
				: base(baseArg1, baseArg2, baseArg3)
			{
				this._enclosing = _enclosing;
			}

			public override IDependencyGrammar FormResult()
			{
				this.wordIndex.AddToIndex(LexiconConstants.UnknownWord);
				ChineseSimWordAvgDepGrammar dg = new ChineseSimWordAvgDepGrammar(this.tlpParams, this.directional, this.useDistance, this.useCoarseDistance, this._enclosing._enclosing.op.trainOptions.basicCategoryTagsInDependencyGrammar, this._enclosing._enclosing
					.op, this.wordIndex, this.tagIndex);
				if (this._enclosing.lex == null)
				{
					throw new Exception("Attempt to create ChineseSimWordAvgDepGrammar before Lexicon!!!");
				}
				else
				{
					dg.SetLex(this._enclosing.lex);
				}
				foreach (IntDependency dependency in this.dependencyCounter.KeySet())
				{
					dg.AddRule(dependency, this.dependencyCounter.GetCount(dependency));
				}
				return dg;
			}

			private readonly ChineseTreebankParserParams _enclosing;
		}

		/// <summary>Return a default sentence for the language (for testing)</summary>
		public override IList<IHasWord> DefaultTestSentence()
		{
			return SentenceUtils.ToUntaggedList("\u951f\u65a4\u62f7", "\u951f\u65a4\u62f7", "\u5b66\u6821", "\u951f\u65a4\u62f7", "\u5b66\u4e60", "\u951f\u65a4\u62f7");
		}

		private const long serialVersionUID = 2;

		public override IList<GrammaticalStructure> ReadGrammaticalStructureFromFile(string filename)
		{
			try
			{
				if (this.GenerateOriginalDependencies())
				{
					return ChineseGrammaticalStructure.ReadCoNLLXGrammaticalStructureCollection(filename);
				}
				else
				{
					return UniversalChineseGrammaticalStructure.ReadCoNLLXGrammaticalStructureCollection(filename);
				}
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
		}

		public override GrammaticalStructure GetGrammaticalStructure(Tree t, IPredicate<string> filter, IHeadFinder hf)
		{
			if (this.GenerateOriginalDependencies())
			{
				return new ChineseGrammaticalStructure(t, filter, hf);
			}
			else
			{
				return new UniversalChineseGrammaticalStructure(t, filter, hf);
			}
		}

		public override bool SupportsBasicDependencies()
		{
			return true;
		}

		public override bool GenerateOriginalDependencies()
		{
			return generateOriginalDependencies;
		}

		/// <summary>For testing: loads a treebank and prints the trees.</summary>
		public static void Main(string[] args)
		{
			ITreebankLangParserParams tlpp = new Edu.Stanford.Nlp.Parser.Lexparser.ChineseTreebankParserParams();
			System.Console.Out.WriteLine("Default encoding is: " + tlpp.DiskTreebank().Encoding());
			if (args.Length < 2)
			{
				PrintlnErr("Usage: edu.stanford.nlp.parser.lexparser.ChineseTreebankParserParams treesPath fileRange");
			}
			else
			{
				Treebank m = tlpp.DiskTreebank();
				m.LoadPath(args[0], new NumberRangesFileFilter(args[1], false));
				foreach (Tree t in m)
				{
					t.PennPrint(tlpp.Pw());
				}
				System.Console.Out.WriteLine("There were " + m.Count + " trees.");
			}
		}
	}
}

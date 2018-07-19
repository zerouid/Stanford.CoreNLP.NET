using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Negra;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>
	/// Parameter file for parsing the Penn Treebank format of the Negra
	/// Treebank (German).
	/// </summary>
	/// <remarks>
	/// Parameter file for parsing the Penn Treebank format of the Negra
	/// Treebank (German).  STILL UNDER CONSTRUCTION!
	/// </remarks>
	/// <author>Roger Levy</author>
	[System.Serializable]
	public class NegraPennTreebankParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.NegraPennTreebankParserParams));

		private const long serialVersionUID = 757812264219400466L;

		private const bool Debug = false;

		private bool markRC = false;

		private bool markZuVP = false;

		private bool markLP = false;

		private bool markColon = false;

		private bool markKonjParent = false;

		private bool markHDParent = false;

		private bool markContainsV = false;

		private const bool defaultLeaveGF = false;

		private const char defaultGFCharacter = '-';

		/// <summary>Node cleanup is how node names are normalized.</summary>
		/// <remarks>
		/// Node cleanup is how node names are normalized. The known values are:
		/// 0 = do nothing;
		/// 1 = keep category and function;
		/// 2 = keep only category
		/// </remarks>
		private int nodeCleanup = 2;

		private IHeadFinder headFinder;

		private bool treeNormalizerInsertNPinPP = false;

		private bool treeNormalizerLeaveGF = false;

		public NegraPennTreebankParserParams()
			: base(new NegraPennLanguagePack(defaultLeaveGF, defaultGFCharacter))
		{
			//Features
			//Grammatical function parameters
			//TODO: fix this so it really works
			//wsg2010: Commented out by Roger?
			//return new NegraHeadFinder();
			//return new LeftHeadFinder();
			headFinder = new NegraHeadFinder();
			// override output encoding: make it UTF-8
			SetOutputEncoding("UTF-8");
		}

		/// <summary>returns a NegraHeadFinder</summary>
		public override IHeadFinder HeadFinder()
		{
			return headFinder;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return HeadFinder();
		}

		/// <summary>returns an ordinary Lexicon (could be tuned for German!)</summary>
		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (op.lexOptions.uwModelTrainer == null)
			{
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.GermanUnknownWordModelTrainer";
			}
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		private NegraPennTreeReaderFactory treeReaderFactory;

		public override ITreeReaderFactory TreeReaderFactory()
		{
			if (treeReaderFactory == null)
			{
				treeReaderFactory = new NegraPennTreeReaderFactory(nodeCleanup, treeNormalizerInsertNPinPP, treeNormalizerLeaveGF, TreebankLanguagePack());
			}
			return treeReaderFactory;
		}

		/* Returns a MemoryTreebank with a NegraPennTokenizer and a
		* NegraPennTreeNormalizer */
		public override Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.MemoryTreebank(TreeReaderFactory(), inputEncoding);
		}

		/* Returns a DiskTreebank with a NegraPennTokenizer and a
		* NegraPennTreeNormalizer */
		public override Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.DiskTreebank(TreeReaderFactory(), inputEncoding);
		}

		/// <summary>returns a NegraPennCollinizer</summary>
		public override ITreeTransformer Collinizer()
		{
			return new NegraPennCollinizer(this);
		}

		/// <summary>returns a NegraPennCollinizer</summary>
		public override ITreeTransformer CollinizerEvalb()
		{
			return new NegraPennCollinizer(this, false);
		}

		/* parser tuning follows */
		public override string[] SisterSplitters()
		{
			return new string[0];
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
			//lang. specific options
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nodeCleanup"))
			{
				nodeCleanup = System.Convert.ToInt32(args[i + 1]);
				i += 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-leaveGF"))
				{
					((NegraPennLanguagePack)TreebankLanguagePack()).SetLeaveGF(true);
					treeNormalizerLeaveGF = true;
					i++;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-gfCharacter"))
					{
						string gfChar = args[i + 1];
						if (gfChar.Length > 1)
						{
							System.Console.Out.WriteLine("Warning! gfCharacter argument ignored; must specify a character, not a String");
						}
						TreebankLanguagePack().SetGfCharacter(gfChar[0]);
						i += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markZuVP"))
						{
							markZuVP = true;
							i++;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markRC"))
							{
								markRC = true;
								i++;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-insertNPinPP"))
								{
									treeNormalizerInsertNPinPP = true;
									i++;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markLP"))
									{
										markLP = true;
										i++;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markColon"))
										{
											markColon = true;
											i++;
										}
										else
										{
											if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markKonjParent"))
											{
												markKonjParent = true;
												i++;
											}
											else
											{
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markHDParent"))
												{
													markHDParent = true;
													i++;
												}
												else
												{
													if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markContainsV"))
													{
														markContainsV = true;
														i++;
													}
													else
													{
														if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-evalGF"))
														{
															this.SetEvalGF(bool.ParseBoolean(args[i + 1]));
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
			return i;
		}

		public override void Display()
		{
			log.Info("NegraPennTreebankParserParams");
			log.Info("  markZuVP=" + markZuVP);
			log.Info("  insertNPinPP=" + treeNormalizerInsertNPinPP);
			log.Info("  leaveGF=" + treeNormalizerLeaveGF);
			System.Console.Out.WriteLine("markLP=" + markLP);
			System.Console.Out.WriteLine("markColon=" + markColon);
		}

		private string BasicCat(string str)
		{
			return TreebankLanguagePack().BasicCategory(str);
		}

		/// <summary>
		/// transformTree does all language-specific tree
		/// transformations.
		/// </summary>
		/// <remarks>
		/// transformTree does all language-specific tree
		/// transformations. Any parameterizations should be inside the
		/// specific TreebankLangParserarams class.
		/// </remarks>
		public override Tree TransformTree(Tree t, Tree root)
		{
			if (t == null || t.IsLeaf())
			{
				return t;
			}
			IList<string> annotations = new List<string>();
			CoreLabel lab = (CoreLabel)t.Label();
			string word = lab.Word();
			string tag = lab.Tag();
			string cat = lab.Value();
			string baseCat = TreebankLanguagePack().BasicCategory(cat);
			//Tree parent = t.parent(root);
			// String mcat = "";
			// if (parent != null) {
			//   mcat = parent.label().value();
			// }
			//categories -- at present there is no tag annotation!!
			if (t.IsPhrasal())
			{
				IList<string> childBasicCats = ChildBasicCats(t);
				// mark vp's headed by "zu" verbs
				if (markZuVP && baseCat.Equals("VP") && (childBasicCats.Contains("VZ") || childBasicCats.Contains("VVIZU")))
				{
					annotations.Add("%ZU");
				}
				// mark relative clause S's
				if (markRC && (t.Label() is NegraLabel) && baseCat.Equals("S") && ((NegraLabel)t.Label()).GetEdge() != null && ((NegraLabel)t.Label()).GetEdge().Equals("RC"))
				{
					//throw new RuntimeException("damn, not a Negra Label");
					annotations.Add("%RC");
				}
				//      if(t.children().length == 1) {
				//        annotations.add("%U");
				//      }
				if (markContainsV && ContainsVP(t))
				{
					annotations.Add("%vp");
				}
				if (markLP && LeftPhrasal(t))
				{
					annotations.Add("%LP");
				}
				if (markKonjParent)
				{
					// this depends on functional tags being present
					foreach (string cCat in childBasicCats)
					{
						if (cCat.Contains("-KONJ"))
						{
							annotations.Add("%konjp");
							break;
						}
					}
				}
				if (markHDParent)
				{
					// this depends on functional tags being present
					foreach (string cCat in childBasicCats)
					{
						if (cCat.Contains("-HD"))
						{
							annotations.Add("%hdp");
							break;
						}
					}
				}
			}
			else
			{
				//t.isPreTerminal() case
				if (markColon && cat.Equals("$.") && (word.Equals(":") || word.Equals(";")))
				{
					annotations.Add("-%colon");
				}
			}
			//    if(t.isPreTerminal()) {
			//      if(parent != null) {
			//        String parentVal = parent.label().value();
			//        int cutOffPtD = parentVal.indexOf('-');
			//        int cutOffPtC = parentVal.indexOf('^');
			//        int curMin = parentVal.length();
			//        if(cutOffPtD != -1) {
			//          curMin = cutOffPtD;
			//        }
			//        if(cutOffPtC != -1) {
			//          curMin = Math.min(curMin, cutOffPtC);
			//        }
			//        parentVal = parentVal.substring(0, curMin);
			//        annotations.add("^" + parentVal);
			//      }
			//    }
			// put on all the annotations
			StringBuilder catSB = new StringBuilder(cat);
			foreach (string annotation in annotations)
			{
				catSB.Append(annotation);
			}
			t.SetLabel(new CategoryWordTag(catSB.ToString(), word, tag));
			return t;
		}

		private static bool LeftPhrasal(Tree t)
		{
			while (!t.IsLeaf())
			{
				t = t.LastChild();
				string str = t.Label().Value();
				if (str.StartsWith("NP") || str.StartsWith("PP") || str.StartsWith("VP") || str.StartsWith("S") || str.StartsWith("Q") || str.StartsWith("A"))
				{
					return true;
				}
			}
			return false;
		}

		private bool ContainsVP(Tree t)
		{
			string cat = tlp.BasicCategory(t.Label().Value());
			if (cat.StartsWith("V"))
			{
				return true;
			}
			else
			{
				Tree[] kids = t.Children();
				foreach (Tree kid in kids)
				{
					if (ContainsVP(kid))
					{
						return true;
					}
				}
				return false;
			}
		}

		private IList<string> ChildBasicCats(Tree t)
		{
			Tree[] kids = t.Children();
			IList<string> l = new List<string>();
			foreach (Tree kid in kids)
			{
				l.Add(BasicCat(kid.Label().Value()));
			}
			return l;
		}

		/// <summary>Return a default sentence for the language (for testing)</summary>
		public override IList<IHasWord> DefaultTestSentence()
		{
			string[] sent = new string[] { "Solch", "einen", "Zuspruch", "hat", "Angela", "Merkel", "lange", "nicht", "mehr", "erlebt", "." };
			return SentenceUtils.ToWordList(sent);
		}
	}
}

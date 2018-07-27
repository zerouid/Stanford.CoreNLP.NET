using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Tuebadz;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	/// <summary>TreebankLangParserParams for the German Tuebingen corpus.</summary>
	/// <remarks>
	/// TreebankLangParserParams for the German Tuebingen corpus.
	/// The TueBaDZTreeReaderFactory has been changed in order to use a
	/// TueBaDZPennTreeNormalizer.
	/// </remarks>
	/// <author>Roger Levy (rog@stanford.edu)</author>
	/// <author>Wolfgang Maier (wmaier@sfs.uni-tuebingen.de)</author>
	[System.Serializable]
	public class TueBaDZParserParams : AbstractTreebankParserParams
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.TueBaDZParserParams));

		private IHeadFinder hf = new TueBaDZHeadFinder();

		/// <summary>
		/// How to clean up node labels: 0 = do nothing, 1 = keep category and
		/// function, 2 = just category.
		/// </summary>
		private int nodeCleanup = 0;

		private bool markKonjParent = false;

		private bool markContainsV = true;

		private bool markZu = true;

		private bool markColons = false;

		private bool leftPhrasal = false;

		private bool markHDParent = false;

		private bool leaveGF = false;

		public TueBaDZParserParams()
			: base(new TueBaDZLanguagePack())
		{
		}

		/// <summary>Returns the first sentence of TueBaDZ.</summary>
		public override IList<IHasWord> DefaultTestSentence()
		{
			return SentenceUtils.ToWordList("Veruntreute", "die", "AWO", "Spendengeld", "?");
		}

		public override string[] SisterSplitters()
		{
			return new string[0];
		}

		public override ITreeTransformer Collinizer()
		{
			return new TreeCollinizer(TreebankLanguagePack());
		}

		public override ITreeTransformer CollinizerEvalb()
		{
			return new TreeCollinizer(TreebankLanguagePack());
		}

		public override Edu.Stanford.Nlp.Trees.MemoryTreebank MemoryTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.MemoryTreebank(TreeReaderFactory());
		}

		public override Edu.Stanford.Nlp.Trees.DiskTreebank DiskTreebank()
		{
			return new Edu.Stanford.Nlp.Trees.DiskTreebank(TreeReaderFactory());
		}

		public override ITreeReaderFactory TreeReaderFactory()
		{
			return new TueBaDZTreeReaderFactory(TreebankLanguagePack(), nodeCleanup);
		}

		public override ILexicon Lex(Options op, IIndex<string> wordIndex, IIndex<string> tagIndex)
		{
			if (op.lexOptions.uwModelTrainer == null)
			{
				op.lexOptions.uwModelTrainer = "edu.stanford.nlp.parser.lexparser.GermanUnknownWordModelTrainer";
			}
			return new BaseLexicon(op, wordIndex, tagIndex);
		}

		/// <summary>Set language-specific options according to flags.</summary>
		/// <remarks>
		/// Set language-specific options according to flags.
		/// This routine should process the option starting in args[i] (which
		/// might potentially be several arguments long if it takes arguments).
		/// It should return the index after the last index it consumed in
		/// processing.  In particular, if it cannot process the current option,
		/// the return value should be i.
		/// <p>
		/// In the TueBaDZ ParserParams, all flags take 1 argument (and so can all
		/// be turned on and off).
		/// </remarks>
		public override int SetOptionFlag(string[] args, int i)
		{
			// [CDM 2008: there are no generic options!] first, see if it's a generic option
			// int j = super.setOptionFlag(args, i);
			// if(i != j) return j;
			//lang. specific options
			if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-nodeCleanup"))
			{
				nodeCleanup = System.Convert.ToInt32(args[i + 1]);
				i += 2;
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markKonjParent"))
				{
					markKonjParent = bool.ParseBoolean(args[i + 1]);
					i += 2;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markContainsV"))
					{
						markContainsV = bool.ParseBoolean(args[i + 1]);
						i += 2;
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markZu"))
						{
							markZu = bool.ParseBoolean(args[i + 1]);
							i += 2;
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markColons"))
							{
								markColons = bool.ParseBoolean(args[i + 1]);
								i += 2;
							}
							else
							{
								if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-leftPhrasal"))
								{
									leftPhrasal = bool.ParseBoolean(args[i + 1]);
									i += 2;
								}
								else
								{
									if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-markHDParent"))
									{
										markHDParent = bool.ParseBoolean(args[i + 1]);
										i += 2;
									}
									else
									{
										if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-leaveGF"))
										{
											leaveGF = bool.ParseBoolean(args[i + 1]);
											((TueBaDZLanguagePack)TreebankLanguagePack()).SetLeaveGF(leaveGF);
											i += 2;
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
												if (Sharpen.Runtime.EqualsIgnoreCase(args[i], "-limitedGF"))
												{
													((TueBaDZLanguagePack)TreebankLanguagePack()).SetLimitedGF(bool.ParseBoolean(args[i + 1]));
													i += 2;
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
			log.Info("TueBaDZParserParams nodeCleanup=" + nodeCleanup + " mKonjParent=" + markKonjParent + " mContainsV=" + markContainsV + " mZu=" + markZu + " mColons=" + markColons);
		}

		/// <summary>
		/// returns a
		/// <see cref="Edu.Stanford.Nlp.Trees.International.Tuebadz.TueBaDZHeadFinder"/>
		/// .
		/// </summary>
		public override IHeadFinder HeadFinder()
		{
			return hf;
		}

		public override IHeadFinder TypedDependencyHeadFinder()
		{
			return HeadFinder();
		}

		/// <summary>Annotates a tree according to options.</summary>
		public override Tree TransformTree(Tree t, Tree root)
		{
			if (t == null || t.IsLeaf())
			{
				return t;
			}
			IList<string> annotations = new List<string>();
			ILabel lab = t.Label();
			string word = null;
			if (lab is IHasWord)
			{
				word = ((IHasWord)lab).Word();
			}
			string tag = null;
			if (lab is IHasTag)
			{
				tag = ((IHasTag)lab).Tag();
			}
			string cat = lab.Value();
			// Tree parent = t.parent(root);
			if (t.IsPhrasal())
			{
				IList<string> childBasicCats = ChildBasicCats(t);
				// cdm 2008: have form for with and without functional tags since this is a hash
				if (markZu && cat.StartsWith("V") && (childBasicCats.Contains("PTKZU") || childBasicCats.Contains("PTKZU-HD") || childBasicCats.Contains("VVIZU") || childBasicCats.Contains("VVIZU-HD")))
				{
					annotations.Add("%ZU");
				}
				if (markContainsV && ContainsV(t))
				{
					annotations.Add("%vp");
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
				// t.isPreTerminal() case
				//      if (word.equals("%")) {
				//        annotations.add("-%");
				//      }
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
				if (markColons && cat.Equals("$.") && word != null && (word.Equals(":") || word.Equals(";")))
				{
					annotations.Add("-%colon");
				}
				if (leftPhrasal && LeftPhrasal(t))
				{
					annotations.Add("%LP");
				}
			}
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

		private string BasicCat(string str)
		{
			return tlp.BasicCategory(str);
		}

		private static bool ContainsV(Tree t)
		{
			string cat = t.Label().Value();
			if (cat.StartsWith("V"))
			{
				return true;
			}
			else
			{
				Tree[] kids = t.Children();
				foreach (Tree kid in kids)
				{
					if (ContainsV(kid))
					{
						return true;
					}
				}
				return false;
			}
		}

		private const long serialVersionUID = 7303189408025355170L;
	}
}

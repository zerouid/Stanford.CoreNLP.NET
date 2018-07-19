using System;
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>
	/// Coordination transformer transforms a PennTreebank tree containing
	/// a coordination in a flat structure in order to get the dependencies
	/// right.
	/// </summary>
	/// <remarks>
	/// Coordination transformer transforms a PennTreebank tree containing
	/// a coordination in a flat structure in order to get the dependencies
	/// right.
	/// <br />
	/// The transformer goes through several steps:
	/// <ul>
	/// <li> Removes empty nodes and simplifies many tags (<code>DependencyTreeTransformer</code>)
	/// <li> Relabels UCP phrases to either ADVP or NP depending on their content
	/// <li> Turn flat CC structures into structures with an intervening node
	/// <li> Add extra structure to QP phrases - combine "well over", unflattened structures with CC (<code>QPTreeTransformer</code>)
	/// <li> Flatten SQ structures to get the verb as the head
	/// <li> Rearrange structures that appear to be dates
	/// <li> Flatten X over only X structures
	/// <li> Turn some fixed conjunction phrases into CONJP, such as "and yet", etc
	/// <li> Attach RB such as "not" to the next phrase to get the RB headed by the phrase it modifies
	/// <li> Turn SBAR to PP if parsed as SBAR in phrases such as "The day after the airline was planning ..."
	/// <li> Rearrange "now that" into an SBAR phrase if it was misparsed as ADVP
	/// <li> (Only for universal dependencies) Extracts multi-word expressions and attaches all nodes to a new MWE constituent
	/// </ul>
	/// </remarks>
	/// <author>Marie-Catherine de Marneffe</author>
	/// <author>John Bauer</author>
	/// <author>Sebastian Schuster</author>
	public class CoordinationTransformer : ITreeTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.CoordinationTransformer));

		private static readonly bool Verbose = Runtime.GetProperty("CoordinationTransformer", null) != null;

		private readonly ITreeTransformer tn = new DependencyTreeTransformer();

		private readonly ITreeTransformer dates = new DateTreeTransformer();

		private readonly ITreeTransformer qp;

		private readonly IHeadFinder headFinder;

		private readonly bool performMWETransformation;

		public CoordinationTransformer(IHeadFinder hf)
			: this(hf, false)
		{
		}

		/// <summary>Constructor</summary>
		/// <param name="hf">the headfinder</param>
		/// <param name="performMWETransformation">
		/// Parameter for backwards compatibility.
		/// If set to false, multi-word expressions won't be attached to a new "MWE" node
		/// </param>
		public CoordinationTransformer(IHeadFinder hf, bool performMWETransformation)
		{
			//to get rid of unwanted nodes and tag
			//to flatten date patterns
			//to restructure the QP constituents
			// default constructor
			this.headFinder = hf;
			this.performMWETransformation = performMWETransformation;
			qp = new QPTreeTransformer(performMWETransformation);
		}

		/// <summary>
		/// Transforms t if it contains a coordination in a flat structure (CCtransform)
		/// and transforms UCP (UCPtransform).
		/// </summary>
		/// <param name="t">a tree to be transformed</param>
		/// <returns>t transformed</returns>
		public virtual Tree TransformTree(Tree t)
		{
			if (Verbose)
			{
				log.Info("Input to CoordinationTransformer: " + t);
			}
			t = tn.TransformTree(t);
			if (Verbose)
			{
				log.Info("After DependencyTreeTransformer:  " + t);
			}
			if (t == null)
			{
				return t;
			}
			if (performMWETransformation)
			{
				t = MWETransform(t);
				if (Verbose)
				{
					log.Info("After MWETransform:               " + t);
				}
				t = PrepCCTransform(t);
				if (Verbose)
				{
					log.Info("After prepCCTransform:               " + t);
				}
			}
			t = UCPtransform(t);
			if (Verbose)
			{
				log.Info("After UCPTransformer:             " + t);
			}
			t = CCtransform(t);
			if (Verbose)
			{
				log.Info("After CCTransformer:              " + t);
			}
			t = qp.TransformTree(t);
			if (Verbose)
			{
				log.Info("After QPTreeTransformer:          " + t);
			}
			t = SQflatten(t);
			if (Verbose)
			{
				log.Info("After SQ flattening:              " + t);
			}
			t = dates.TransformTree(t);
			if (Verbose)
			{
				log.Info("After DateTreeTransformer:        " + t);
			}
			t = RemoveXOverX(t);
			if (Verbose)
			{
				log.Info("After removeXoverX:               " + t);
			}
			t = CombineConjp(t);
			if (Verbose)
			{
				log.Info("After combineConjp:               " + t);
			}
			t = MoveRB(t);
			if (Verbose)
			{
				log.Info("After moveRB:                     " + t);
			}
			t = ChangeSbarToPP(t);
			if (Verbose)
			{
				log.Info("After changeSbarToPP:             " + t);
			}
			t = RearrangeNowThat(t);
			if (Verbose)
			{
				log.Info("After rearrangeNowThat:           " + t);
			}
			return t;
		}

		private static TregexPattern rearrangeNowThatTregex = TregexPattern.Compile("ADVP=advp <1 (RB < /^(?i:now)$/) <2 (SBAR=sbar <1 (IN < /^(?i:that)$/))");

		private static TsurgeonPattern rearrangeNowThatTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel advp SBAR] [excise sbar sbar]");

		private static Tree RearrangeNowThat(Tree t)
		{
			if (t == null)
			{
				return t;
			}
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(rearrangeNowThatTregex, rearrangeNowThatTsurgeon, t);
		}

		private static TregexPattern changeSbarToPPTregex = TregexPattern.Compile("NP < (NP $++ (SBAR=sbar < (IN < /^(?i:after|before|until|since|during)$/ $++ S)))");

		private static TsurgeonPattern changeSbarToPPTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("relabel sbar PP");

		/// <summary>
		/// For certain phrases, we change the SBAR to a PP to get prep/pcomp
		/// dependencies.
		/// </summary>
		/// <remarks>
		/// For certain phrases, we change the SBAR to a PP to get prep/pcomp
		/// dependencies.  For example, in "The day after the airline was
		/// planning...", we want prep(day, after) and pcomp(after,
		/// planning).  If "after the airline was planning" was parsed as an
		/// SBAR, either by the parser or in the treebank, we fix that here.
		/// </remarks>
		private static Tree ChangeSbarToPP(Tree t)
		{
			if (t == null)
			{
				return null;
			}
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(changeSbarToPPTregex, changeSbarToPPTsurgeon, t);
		}

		private static TregexPattern findFlatConjpTregex = TregexPattern.Compile("/^(S|PP|VP)/ < (/^(S(?!YM)|PP|VP)/ $++ (CC=start $+ (RB|ADVP $+ /^(S(?!YM)|PP|VP)/) " + "[ (< and $+ (RB=end < yet)) | " + "  (< and $+ (RB=end < so)) | " + "  (< and $+ (ADVP=end < (RB|IN < so))) ] ))"
			);

		private static TsurgeonPattern addConjpTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("createSubtree CONJP start end");

		// TODO: add more patterns, perhaps ignore case
		// for example, what should we do with "and not"?  Is it right to
		// generally add the "not" to the following tree with moveRB, or
		// should we make "and not" a CONJP?
		// also, perhaps look at ADVP
		// TODO: what should be the head of "and yet"?
		// TODO: this structure needs a dependency
		private static Tree CombineConjp(Tree t)
		{
			if (t == null)
			{
				return null;
			}
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(findFlatConjpTregex, addConjpTsurgeon, t);
		}

		private static TregexPattern[] moveRBTregex = new TregexPattern[] { TregexPattern.Compile("/^S|PP|VP|NP/ < (/^(S|PP|VP|NP)/ $++ (/^(,|CC|CONJP)$/ [ $+ (RB=adv [ < not | < then ]) | $+ (ADVP=adv <: RB) ])) : (=adv $+ /^(S(?!YM)|PP|VP|NP)/=dest) "
			), TregexPattern.Compile("/^ADVP/ < (/^ADVP/ $++ (/^(,|CC|CONJP)$/ [$+ (RB=adv [ < not | < then ]) | $+ (ADVP=adv <: RB)])) : (=adv $+ /^NP-ADV|ADVP|PP/=dest)"), TregexPattern.Compile("/^FRAG/ < (ADVP|RB=adv $+ VP=dest)") };

		private static TsurgeonPattern moveRBTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("move adv >0 dest");

		internal static Tree MoveRB(Tree t)
		{
			if (t == null)
			{
				return null;
			}
			foreach (TregexPattern pattern in moveRBTregex)
			{
				t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(pattern, moveRBTsurgeon, t);
			}
			return t;
		}

		private static TregexPattern flattenSQTregex = TregexPattern.Compile("SBARQ < ((WHNP=what < WP) $+ (SQ=sq < (/^VB/=verb < " + EnglishPatterns.copularWordRegex + ") " + " !< (/^VB/ < !" + EnglishPatterns.copularWordRegex + ") " + " !< (/^V/ < /^VB/ < !"
			 + EnglishPatterns.copularWordRegex + ") " + " !< (PP $- =verb) " + " !<, (/^VB/ < " + EnglishPatterns.copularWordRegex + " $+ (NP < (EX < there)))" + " !< (ADJP < (PP <: IN|TO))))");

		private static TsurgeonPattern flattenSQTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("excise sq sq");

		// Matches to be questions if the question starts with WHNP, such as
		// Who, What, if there is an SQ after the WH question.
		//
		// TODO: maybe we want to catch more complicated tree structures
		// with something in between the WH and the actual question.
		// match against "is running" if the verb is under just a VBG
		// match against "is running" if the verb is under a VP - VBG
		// match against "What is on the test?"
		// match against "is there"
		// match against "good at"
		/// <summary>
		/// Removes the SQ structure under a WHNP question, such as "Who am I
		/// to judge?".
		/// </summary>
		/// <remarks>
		/// Removes the SQ structure under a WHNP question, such as "Who am I
		/// to judge?".  We do this so that it is easier to pick out the head
		/// and then easier to connect that head to all of the other words in
		/// the question in this situation.  In the specific case of making
		/// the copula head, we don't do this so that the existing headfinder
		/// code can easily find the "am" or other copula verb.
		/// </remarks>
		public virtual Tree SQflatten(Tree t)
		{
			if (headFinder != null && (headFinder is ICopulaHeadFinder))
			{
				if (((ICopulaHeadFinder)headFinder).MakesCopulaHead())
				{
					return t;
				}
			}
			if (t == null)
			{
				return null;
			}
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(flattenSQTregex, flattenSQTsurgeon, t);
		}

		private static TregexPattern removeXOverXTregex = TregexPattern.Compile("__=repeat <: (~repeat < __)");

		private static TsurgeonPattern removeXOverXTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("excise repeat repeat");

		public static Tree RemoveXOverX(Tree t)
		{
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(removeXOverXTregex, removeXOverXTsurgeon, t);
		}

		private static readonly TregexPattern ucpRenameTregex = TregexPattern.Compile("/^UCP/=ucp [ <, /^JJ|ADJP/=adjp | ( <1 DT <2 /^JJ|ADJP/=adjp ) |" + " <- (ADJP=adjp < (JJR < /^(?i:younger|older)$/)) |" + " <, /^N/=np | ( <1 DT <2 /^N/=np ) | "
			 + " <, /^ADVP/=advp ]");

		private static readonly TsurgeonPattern ucpRenameTsurgeon = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[if exists adjp relabel ucp /^UCP(.*)$/ADJP$1/] [if exists np relabel ucp /^UCP(.*)$/NP$1/] [if exists advp relabel ucp /^UCP(.*)$/ADVP/]"
			);

		// UCP (JJ ...) -> ADJP
		// UCP (DT JJ ...) -> ADJP
		// UCP (... (ADJP (JJR older|younger))) -> ADJP
		// UCP (N ...) -> NP
		// UCP ADVP -> ADVP
		// Might want to look for ways to include RB for flatter structures,
		// but then we have to watch out for (RB not) for example
		// Note that the order of OR expressions means the older|younger
		// pattern takes precedence
		// By searching for everything at once, then using one tsurgeon
		// which fixes everything at once, we can save quite a bit of time
		// TODO: this turns UCP-TMP into ADVP instead of ADVP-TMP.  What do we actually want?
		/// <summary>
		/// Transforms t if it contains an UCP, it will change the UCP tag
		/// into the phrasal tag of the first word of the UCP
		/// (UCP (JJ electronic) (, ,) (NN computer) (CC and) (NN building))
		/// will become
		/// (ADJP (JJ electronic) (, ,) (NN computer) (CC and) (NN building))
		/// </summary>
		/// <param name="t">a tree to be transformed</param>
		/// <returns>t transformed</returns>
		public static Tree UCPtransform(Tree t)
		{
			if (t == null)
			{
				return null;
			}
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(ucpRenameTregex, ucpRenameTsurgeon, t);
		}

		/// <summary>Transforms t if it contains a coordination in a flat structure</summary>
		/// <param name="t">a tree to be transformed</param>
		/// <returns>t transformed (give t not null, return will not be null)</returns>
		public static Tree CCtransform(Tree t)
		{
			bool notDone = true;
			while (notDone)
			{
				Tree cc = FindCCparent(t, t);
				if (cc != null)
				{
					t = cc;
				}
				else
				{
					notDone = false;
				}
			}
			return t;
		}

		private static string GetHeadTag(Tree t)
		{
			if (t.Value().StartsWith("NN"))
			{
				return "NP";
			}
			else
			{
				if (t.Value().StartsWith("JJ"))
				{
					return "ADJP";
				}
				else
				{
					return "NP";
				}
			}
		}

		/// <summary>
		/// If things match, this method destructively changes the children list
		/// of the tree t.
		/// </summary>
		/// <remarks>
		/// If things match, this method destructively changes the children list
		/// of the tree t.  When this method is called, t is an NP and there must
		/// be at least two children to the right of ccIndex.
		/// </remarks>
		/// <param name="t">The tree to transform a conjunction in</param>
		/// <param name="ccIndex">The index of the CC child</param>
		/// <returns>t</returns>
		private static Tree TransformCC(Tree t, int ccIndex)
		{
			if (Verbose)
			{
				log.Info("transformCC in:  " + t);
			}
			//System.out.println(ccIndex);
			// use the factories of t to create new nodes
			ITreeFactory tf = t.TreeFactory();
			ILabelFactory lf = t.Label().LabelFactory();
			Tree[] ccSiblings = t.Children();
			//check if other CC
			IList<int> ccPositions = new List<int>();
			for (int i = ccIndex + 1; i < ccSiblings.Length; i++)
			{
				if (ccSiblings[i].Value().StartsWith("CC") && i < ccSiblings.Length - 1)
				{
					// second conjunct to ensure that a CC we add isn't the last child
					ccPositions.Add(int.Parse(i));
				}
			}
			// a CC b c ... -> (a CC b) c ...  with b not a DT
			string beforeSibling = ccSiblings[ccIndex - 1].Value();
			if (ccIndex == 1 && (beforeSibling.Equals("DT") || beforeSibling.Equals("JJ") || beforeSibling.Equals("RB") || !(ccSiblings[ccIndex + 1].Value().Equals("DT"))) && !(beforeSibling.StartsWith("NP") || beforeSibling.Equals("ADJP") || beforeSibling
				.Equals("NNS")))
			{
				// && (ccSiblings.length == ccIndex + 3 || !ccPositions.isEmpty())) {  // something like "soya or maize oil"
				string leftHead = GetHeadTag(ccSiblings[ccIndex - 1]);
				//create a new tree to be inserted as first child of t
				Tree left = tf.NewTreeNode(lf.NewLabel(leftHead), null);
				for (int i_1 = 0; i_1 < ccIndex + 2; i_1++)
				{
					left.AddChild(ccSiblings[i_1]);
				}
				if (Verbose)
				{
					System.Console.Out.WriteLine("print left tree");
					left.PennPrint();
					System.Console.Out.WriteLine();
				}
				// remove all the children of t before ccIndex+2
				for (int i_2 = 0; i_2 < ccIndex + 2; i_2++)
				{
					t.RemoveChild(0);
				}
				if (Verbose)
				{
					if (t.NumChildren() == 0)
					{
						System.Console.Out.WriteLine("Youch! No t children");
					}
				}
				// if stuff after (like "soya or maize oil and vegetables")
				// we need to put the tree in another tree
				if (!ccPositions.IsEmpty())
				{
					bool comma = false;
					int index = ccPositions[0];
					if (Verbose)
					{
						log.Info("more CC index " + index);
					}
					if (ccSiblings[index - 1].Value().Equals(","))
					{
						//to handle the case of a comma ("soya and maize oil, and vegetables")
						index = index - 1;
						comma = true;
					}
					if (Verbose)
					{
						log.Info("more CC index " + index);
					}
					string head = GetHeadTag(ccSiblings[index - 1]);
					if (ccIndex + 2 < index)
					{
						Tree tree = tf.NewTreeNode(lf.NewLabel(head), null);
						tree.AddChild(0, left);
						int k = 1;
						for (int j = ccIndex + 2; j < index; j++)
						{
							if (Verbose)
							{
								ccSiblings[j].PennPrint();
							}
							t.RemoveChild(0);
							tree.AddChild(k, ccSiblings[j]);
							k++;
						}
						if (Verbose)
						{
							System.Console.Out.WriteLine("print t");
							t.PennPrint();
							System.Console.Out.WriteLine("print tree");
							tree.PennPrint();
							System.Console.Out.WriteLine();
						}
						t.AddChild(0, tree);
					}
					else
					{
						t.AddChild(0, left);
					}
					Tree rightTree = tf.NewTreeNode(lf.NewLabel("NP"), null);
					int start = 2;
					if (comma)
					{
						start++;
					}
					while (start < t.NumChildren())
					{
						Tree sib = t.GetChild(start);
						t.RemoveChild(start);
						rightTree.AddChild(sib);
					}
					t.AddChild(rightTree);
				}
				else
				{
					t.AddChild(0, left);
				}
			}
			else
			{
				// DT a CC b c -> DT (a CC b) c
				if (ccIndex == 2 && ccSiblings[0].Value().StartsWith("DT") && !ccSiblings[ccIndex - 1].Value().Equals("NNS") && (ccSiblings.Length == 5 || (!ccPositions.IsEmpty() && ccPositions[0] == 5)))
				{
					string head = GetHeadTag(ccSiblings[ccIndex - 1]);
					//create a new tree to be inserted as second child of t (after the determiner
					Tree child = tf.NewTreeNode(lf.NewLabel(head), null);
					for (int i_1 = 1; i_1 < ccIndex + 2; i_1++)
					{
						child.AddChild(ccSiblings[i_1]);
					}
					if (Verbose)
					{
						if (child.NumChildren() == 0)
						{
							System.Console.Out.WriteLine("Youch! No child children");
						}
					}
					// remove all the children of t between the determiner and ccIndex+2
					//System.out.println("print left tree");
					//child.pennPrint();
					for (int i_2 = 1; i_2 < ccIndex + 2; i_2++)
					{
						t.RemoveChild(1);
					}
					t.AddChild(1, child);
				}
				else
				{
					// ... a, b CC c ... -> ... (a, b CC c) ...
					if (ccIndex > 2 && ccSiblings[ccIndex - 2].Value().Equals(",") && !ccSiblings[ccIndex - 1].Value().Equals("NNS"))
					{
						string head = GetHeadTag(ccSiblings[ccIndex - 1]);
						Tree child = tf.NewTreeNode(lf.NewLabel(head), null);
						for (int i_1 = ccIndex - 3; i_1 < ccIndex + 2; i_1++)
						{
							child.AddChild(ccSiblings[i_1]);
						}
						if (Verbose)
						{
							if (child.NumChildren() == 0)
							{
								System.Console.Out.WriteLine("Youch! No child children");
							}
						}
						int i_2 = ccIndex - 4;
						while (i_2 > 0 && ccSiblings[i_2].Value().Equals(","))
						{
							child.AddChild(0, ccSiblings[i_2]);
							// add the comma
							child.AddChild(0, ccSiblings[i_2 - 1]);
							// add the word before the comma
							i_2 = i_2 - 2;
						}
						if (i_2 < 0)
						{
							i_2 = -1;
						}
						// remove the old children
						for (int j = i_2 + 1; j < ccIndex + 2; j++)
						{
							t.RemoveChild(i_2 + 1);
						}
						// put the new tree
						t.AddChild(i_2 + 1, child);
					}
					else
					{
						// something like "the new phone book and tour guide" -> multiple heads
						// we want (NP the new phone book) (CC and) (NP tour guide)
						bool commaLeft = false;
						bool commaRight = false;
						bool preconj = false;
						int indexBegin = 0;
						Tree conjT = tf.NewTreeNode(lf.NewLabel("CC"), null);
						// create the left tree
						string leftHead = GetHeadTag(ccSiblings[ccIndex - 1]);
						Tree left = tf.NewTreeNode(lf.NewLabel(leftHead), null);
						// handle the case of a preconjunct (either, both, neither)
						Tree first = ccSiblings[0];
						string leaf = first.FirstChild().Value().ToLower();
						if (leaf.Equals("either") || leaf.Equals("neither") || leaf.Equals("both"))
						{
							preconj = true;
							indexBegin = 1;
							conjT.AddChild(first.FirstChild());
						}
						for (int i_1 = indexBegin; i_1 < ccIndex - 1; i_1++)
						{
							left.AddChild(ccSiblings[i_1]);
						}
						// handle the case of a comma ("GM soya and maize, and food ingredients")
						if (ccSiblings[ccIndex - 1].Value().Equals(","))
						{
							commaLeft = true;
						}
						else
						{
							left.AddChild(ccSiblings[ccIndex - 1]);
						}
						// create the CC tree
						Tree cc = ccSiblings[ccIndex];
						// create the right tree
						int nextCC;
						if (ccPositions.IsEmpty())
						{
							nextCC = ccSiblings.Length;
						}
						else
						{
							nextCC = ccPositions[0];
						}
						string rightHead = GetHeadTag(ccSiblings[nextCC - 1]);
						Tree right = tf.NewTreeNode(lf.NewLabel(rightHead), null);
						for (int i_2 = ccIndex + 1; i_2 < nextCC - 1; i_2++)
						{
							right.AddChild(ccSiblings[i_2]);
						}
						// handle the case of a comma ("GM soya and maize, and food ingredients")
						if (ccSiblings[nextCC - 1].Value().Equals(","))
						{
							commaRight = true;
						}
						else
						{
							right.AddChild(ccSiblings[nextCC - 1]);
						}
						if (Verbose)
						{
							if (left.NumChildren() == 0)
							{
								System.Console.Out.WriteLine("Youch! No left children");
							}
							if (right.NumChildren() == 0)
							{
								System.Console.Out.WriteLine("Youch! No right children");
							}
						}
						// put trees together in old t, first we remove the old nodes
						for (int i_3 = 0; i_3 < nextCC; i_3++)
						{
							t.RemoveChild(0);
						}
						if (!ccPositions.IsEmpty())
						{
							// need an extra level
							Tree tree = tf.NewTreeNode(lf.NewLabel("NP"), null);
							if (preconj)
							{
								tree.AddChild(conjT);
							}
							if (left.NumChildren() > 0)
							{
								tree.AddChild(left);
							}
							if (commaLeft)
							{
								tree.AddChild(ccSiblings[ccIndex - 1]);
							}
							tree.AddChild(cc);
							if (right.NumChildren() > 0)
							{
								tree.AddChild(right);
							}
							if (commaRight)
							{
								t.AddChild(0, ccSiblings[nextCC - 1]);
							}
							t.AddChild(0, tree);
						}
						else
						{
							if (preconj)
							{
								t.AddChild(conjT);
							}
							if (left.NumChildren() > 0)
							{
								t.AddChild(left);
							}
							if (commaLeft)
							{
								t.AddChild(ccSiblings[ccIndex - 1]);
							}
							t.AddChild(cc);
							if (right.NumChildren() > 0)
							{
								t.AddChild(right);
							}
							if (commaRight)
							{
								t.AddChild(ccSiblings[nextCC - 1]);
							}
						}
					}
				}
			}
			if (Verbose)
			{
				log.Info("transformCC out: " + t);
			}
			return t;
		}

		private static bool NotNP(IList<Tree> children, int ccIndex)
		{
			for (int i = ccIndex; i < sz; i++)
			{
				if (children[i].Value().StartsWith("NP"))
				{
					return false;
				}
			}
			return true;
		}

		/*
		* Given a tree t, if this tree contains a CC inside a NP followed by 2 nodes
		* (i.e. we have a flat structure that will not work for the dependencies),
		* it will call transform CC on the NP containing the CC and the index of the
		* CC, and then return the root of the whole transformed tree.
		* If it finds no such tree, this method returns null.
		*/
		private static Tree FindCCparent(Tree t, Tree root)
		{
			if (t.IsPreTerminal())
			{
				if (t.Value().StartsWith("CC"))
				{
					Tree parent = t.Parent(root);
					if (parent != null && parent.Value().StartsWith("NP"))
					{
						IList<Tree> children = parent.GetChildrenAsList();
						//System.out.println(children);
						int ccIndex = children.IndexOf(t);
						if (children.Count > ccIndex + 2 && NotNP(children, ccIndex) && ccIndex != 0 && (ccIndex == children.Count - 1 || !children[ccIndex + 1].Value().StartsWith("CC")))
						{
							TransformCC(parent, ccIndex);
							if (Verbose)
							{
								log.Info("After transformCC:             " + root);
							}
							return root;
						}
					}
				}
			}
			else
			{
				foreach (Tree child in t.GetChildrenAsList())
				{
					Tree cur = FindCCparent(child, root);
					if (cur != null)
					{
						return cur;
					}
				}
			}
			return null;
		}

		/// <summary>Multi-word expression patterns</summary>
		private static TregexPattern[] MwePatterns = new TregexPattern[] { TregexPattern.Compile("@CONJP <1 (RB=node1 < /^(?i)as$/) <2 (RB=node2 < /^(?i)well$/) <- (IN=node3 < /^(?i)as$/)"), TregexPattern.Compile("@ADVP|CONJP <1 (RB=node1 < /^(?i)as$/) <- (IN|RB=node2 < /^(?i)well$/)"
			), TregexPattern.Compile("@PP < ((JJ=node1 < /^(?i)such$/) $+ (IN=node2 < /^(?i)as$/))"), TregexPattern.Compile("@PP < ((JJ|IN=node1 < /^(?i)due$/) $+ (IN|TO=node2 < /^(?i)to$/))"), TregexPattern.Compile("@PP|CONJP < ((IN|RB=node1 < /^(?i)(because|instead)$/) $+ (IN=node2 < of))"
			), TregexPattern.Compile("@ADVP|SBAR < ((IN|RB=node1 < /^(?i)in$/) $+ (NN=node2 < /^(?i)case$/))"), TregexPattern.Compile("@ADVP|PP < ((IN|RB=node1 < /^(?i)of$/) $+ (NN|RB=node2 < /^(?i)course$/))"), TregexPattern.Compile("@SBAR|PP < ((IN|RB=node1 < /^(?i)in$/) $+ (NN|NP|RB=node2 [< /^(?i)order$/ | <: (NN < /^(?i)order$/)]))"
			), TregexPattern.Compile("@PP|CONJP|SBAR < ((IN|RB=node1 < /^(?i)rather$/) $+ (IN=node2 < /^(?i)than$/))"), TregexPattern.Compile("@CONJP < ((IN|RB=node1 < /^(?i)not$/) $+ (TO=node2 < /^(?i)to$/ $+ (VB|RB=node3 < /^(?i)mention$/)))"), TregexPattern
			.Compile("@PP|SBAR < ((JJ|IN|RB=node1 < /^(?i)so$/) $+ (IN|TO=node2 < /^(?i)that$/))"), TregexPattern.Compile("@SBAR < ((IN|RB=node1 < /^(?i)as$/) $+ (IN=node2 < /^(?i)if$/))"), TregexPattern.Compile("@PP < ((JJ|RB=node1 < /^(?i)prior$/) $+ (TO|IN=node2 < /^(?i)to$/))"
			), TregexPattern.Compile("@PP < ((IN=node1 < /^(?i)as$/) $+ (TO|IN=node2 < /^(?i)to$/))"), TregexPattern.Compile("@ADVP < ((RB|NN=node1 < /^(?i)kind$/) $+ (IN|RB=node2 < /^(?i)of$/))"), TregexPattern.Compile("@SBAR < ((IN|RB=node1 < /^(?i)whether$/) $+ (CC=node2 < /^(?i)or$/ $+ (RB=node3 < /^(?i)not$/)))"
			), TregexPattern.Compile("@CONJP < ((IN=node1 < /^(?i)as$/) $+ (VBN=node2 < /^(?i)opposed$/ $+ (TO|IN=node3 < /^(?i)to$/)))"), TregexPattern.Compile("@ADVP|CONJP < ((VB|RB|VBD=node1 < /^(?i)let$/) $+ (RB|JJ=node2 < /^(?i)alone$/))"), TregexPattern
			.Compile("@ADVP|PP < ((IN|RB=node1 < /^(?i)in$/) $+ (IN|NP|PP|RB|ADVP=node2 [< /^(?i)between$/ | <: (IN|RB < /^(?i)between$/)]))"), TregexPattern.Compile("@ADVP|QP|ADJP < ((DT|RB=node1 < /^(?i)all$/) $+ (CC|RB|IN=node2 < /^(?i)but$/))"), TregexPattern
			.Compile("@ADVP|INTJ < ((NN|DT|RB=node1 < /^(?i)that$/) $+ (VBZ|RB=node2 < /^(?i)is$/))"), TregexPattern.Compile("@WHADVP < ((WRB=node1 < /^(?i:how)$/) $+ (VB=node2 < /^(?i)come$/))"), TregexPattern.Compile("@VP < ((VBD=node1 < had|'d) $+ (@PRT|ADVP=node2 <: (RBR < /^(?i)better$/)))"
			), TregexPattern.Compile("@QP|XS < ((JJR|RBR|IN=node1 < /^(?i)(more|less)$/) $+ (IN=node2 < /^(?i)than$/))"), TregexPattern.Compile("@QP < ((JJR|RBR|IN=node1 < /^(?i)up$/) $+ (IN|TO=node2 < /^(?i)to$/))"), TregexPattern.Compile("@S|SQ|VP|ADVP|PP < (@ADVP < ((IN|RB=node1 < /^(?i)at$/) $+ (JJS|RBS=node2 < /^(?i)least$/)) !$+ (RB < /(?i)(once|twice)/))"
			) };

		private static TsurgeonPattern MweOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree MWE node1 node2] [if exists node3 move node3 $- node2]");

		private static TregexPattern AccordingToPattern = TregexPattern.Compile("PP=pp1 < (VBG=node1 < /^(?i)according$/ $+ (PP=pp2 < (TO|IN=node2 < to)))");

		private static TsurgeonPattern AccordingToOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree MWE node1] [move node2 $- node1] [excise pp2 pp2]");

		private static TregexPattern ButAlsoPattern = TregexPattern.Compile("CONJP=conjp < (CC=cc < but) < (RB=rb < also) ?$+ (__=nextNode < (__ < __))");

		private static TsurgeonPattern ButAlsoOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[move cc $- conjp] [move rb $- cc] [if exists nextNode move rb >1 nextNode] [createSubtree ADVP rb] [delete conjp]");

		private static TregexPattern AtRbsPattern = TregexPattern.Compile("@ADVP|QP < ((IN|RB=node1 < /^(?i)at$/) $+ (JJS|RBS=node2))");

		private static TsurgeonPattern AtRbsOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel node1 IN] [createSubtree ADVP node1] [move node2 $- node1] [createSubtree NP node2]");

		private static TregexPattern AtAllPattern = TregexPattern.Compile("@ADVP=head < (RB|IN=node1 < /^(?i)at$/ $+ (RB|DT=node2 < /^(?i)all$/))");

		private static TsurgeonPattern AtAllOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[relabel head PP] [relabel node1 IN] [createSubtree NP node2]");

		//as well as
		//as well
		//such as
		//due to 
		//because of/instead of 
		//in case
		//of course
		//in order
		//rather than
		//not to mention
		//so that 
		//as if
		//prior to
		//as to
		//kind of
		//whether or not
		//as opposed to
		//let alone
		//TODO: "so as to"
		//in between
		//all but
		//that is
		//how come
		//had better
		//more/less than
		//up to
		//at least
		/* "but also" is not a MWE, so break up the CONJP. */
		/* at least / at most / at best / at worst / ... should be treated as if "at"
		was a preposition and the RBS was a noun. Assumes that the MWE "at least"
		has already been extracted. */
		/* at all should be treated like a PP. */
		/// <summary>Puts all multi-word expressions below a single constituent labeled "MWE".</summary>
		/// <remarks>
		/// Puts all multi-word expressions below a single constituent labeled "MWE".
		/// Patterns for multi-word expressions are defined in MWE_PATTERNS.
		/// </remarks>
		public static Tree MWETransform(Tree t)
		{
			foreach (TregexPattern p in MwePatterns)
			{
				Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(p, MweOperation, t);
			}
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(AccordingToPattern, AccordingToOperation, t);
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(ButAlsoPattern, ButAlsoOperation, t);
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(AtRbsPattern, AtRbsOperation, t);
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(AtAllPattern, AtAllOperation, t);
			return t;
		}

		private static TregexPattern FlatPrepCcPattern = TregexPattern.Compile("PP <, (/^(IN|TO)$/=p1 $+ (CC=cc $+ /^(IN|TO)$/=p2))");

		private static TsurgeonPattern FlatPrepCcOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("[createSubtree PCONJP p1 cc] [move p2 $- cc]");

		public static Tree PrepCCTransform(Tree t)
		{
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(FlatPrepCcPattern, FlatPrepCcOperation, t);
			return t;
		}

		public static void Main(string[] args)
		{
			Edu.Stanford.Nlp.Trees.CoordinationTransformer transformer = new Edu.Stanford.Nlp.Trees.CoordinationTransformer(null);
			Treebank tb = new MemoryTreebank();
			Properties props = StringUtils.ArgsToProperties(args);
			string treeFileName = props.GetProperty("treeFile");
			if (treeFileName != null)
			{
				try
				{
					ITreeReader tr = new PennTreeReader(new BufferedReader(new InputStreamReader(new FileInputStream(treeFileName))), new LabeledScoredTreeFactory());
					for (Tree t; (t = tr.ReadTree()) != null; )
					{
						tb.Add(t);
					}
				}
				catch (IOException e)
				{
					throw new Exception("File problem: " + e);
				}
			}
			foreach (Tree t_1 in tb)
			{
				System.Console.Out.WriteLine("Original tree");
				t_1.PennPrint();
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("Tree transformed");
				Tree tree = transformer.TransformTree(t_1);
				tree.PennPrint();
				System.Console.Out.WriteLine();
				System.Console.Out.WriteLine("----------------------------");
			}
		}
	}
}

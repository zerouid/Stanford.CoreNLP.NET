using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Trees
{
	public class EnglishPTBTreebankCorrector : ITreebankTransformer
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.EnglishPTBTreebankCorrector));

		private const bool Debug = false;

		private const string Be = "/^(?i:am|is|are|was|were|be|being|been|'s|'m|'re|s|ai)$/";

		private const string Do = "/^(?i:do|did|does|doing|done)$/";

		/// <summary>
		/// List of be, have, and get auxiliary forms: things that you would expect
		/// to have a VBN complement if anything verbal.
		/// </summary>
		/// <remarks>
		/// List of be, have, and get auxiliary forms: things that you would expect
		/// to have a VBN complement if anything verbal.
		/// A few times the apostrophe is missing on "'s", so we have just "s".
		/// Could also add become, feel, seem, remain for some PTB cases, but
		/// maybe should reparse them?
		/// </remarks>
		private const string BeHaveGet = "/^(?i:has|have|had|having|am|is|are|was|were|be|being|been|'s|'ve|'d|'m|'re|s|ai|get|gets|getting|got|gotten)$/";

		private const string ModalWord = "/^(?i:should|would|wo|could|may|might|ca|can|dare|will|'ll|must|shall|sha|'d)$/";

		private const string Modal = "[ < (__ < /^(?i:should|would|wo|could|may|might|ca|can|dare|will|'ll|must|shall|sha)$/) | < (MD < /^(?i:'d)$/) ]";

		/// <summary>Contexts that take VB complement.</summary>
		/// <remarks>
		/// Contexts that take VB complement.  This is a tregex subexpression
		/// that includes the tag.  Something to specify next to VP:
		/// "(@VP" + MODAL_DO_TO + ")".  na is gonna.  sha is shan't (though doesn't occur).
		/// Not modals that take TO: need, ought.  Or mighta.
		/// Note that "'d" can also be "had", so one needs to be careful on that!
		/// </remarks>
		private const string ModalDoTo = "[ < (__ < /^(?i:do|did|does|doing|done|to|na|should|would|wo|could|may|might|ca|can|dare|will|'ll|must|shall|sha)$/) | < (MD < /^(?i:'d)$/) ]";

		/// <summary>Verbs that take bare VB complements.</summary>
		/// <remarks>
		/// Verbs that take bare VB complements.
		/// 'say' is sort of special, these are semi-direct speech cases where there
		/// are no inverted quotes but imperatives follow.  Check for
		/// overgeneralization.
		/// </remarks>
		private const string BareVpVerb = " < (__ < /^(?i:help|helps|helping|helped|make|makes|making|made|see|sees|saw|seen|seeing|hear|heard|hears|hearing|let|lets|letting)$/)";

		private const string SayVerb = " < (__ < /^(?i:say|says|said|saying)$/)";

		private readonly IList<Pair<TregexPattern, TsurgeonPattern>> ops;

		public EnglishPTBTreebankCorrector()
		{
			// "ai" is from "ai n't"; occasionally there is a bare "s" from "is"
			// initialize the transformations to be done
			ops = new List<Pair<TregexPattern, TsurgeonPattern>>();
			ITreebankLanguagePack tlp = new PennTreebankLanguagePack();
			TregexPatternCompiler tpc = new TregexPatternCompiler(tlp.HeadFinder(), tlp.GetBasicCategoryFunction());
			Macros.AddAllMacros(tpc, GetBufferedReader(macroStr));
			try
			{
				BufferedReader br = GetBufferedReader(editStr);
				IList<TsurgeonPattern> tsp = new List<TsurgeonPattern>();
				for (string line; (line = br.ReadLine()) != null; )
				{
					TregexPattern matchPattern = tpc.Compile(line);
					tsp.Clear();
					while (Continuing(line = br.ReadLine()))
					{
						TsurgeonPattern p = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(line);
						tsp.Add(p);
					}
					if (!tsp.IsEmpty())
					{
						TsurgeonPattern tp = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.CollectOperations(tsp);
						ops.Add(new Pair<TregexPattern, TsurgeonPattern>(matchPattern, tp));
					}
				}
			}
			catch (IOException ioe)
			{
				// while not at end of file
				Sharpen.Runtime.PrintStackTrace(ioe);
			}
		}

		/// <summary>Fix all the English Penn Treebank errors, or at least some of them (!).</summary>
		public virtual MemoryTreebank TransformTrees(Treebank tb)
		{
			MemoryTreebank mtb = new MemoryTreebank(tb.TreeReaderFactory(), tb.Encoding());
			foreach (Tree t in tb)
			{
				mtb.Add(Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(ops, t));
			}
			return mtb;
		}

		private static bool Continuing(string str)
		{
			return str != null && !str.Matches("\\s*");
		}

		private static BufferedReader GetBufferedReader(string source)
		{
			return new BufferedReader(new StringReader(source));
		}

		private const string macroStr = "HAVE_AUX_WORD\t/^(?i:has|have|had|having|'s|'ve|'d)\\$/\n";

		private const string editStr = (("@VP=adj < (NP < (NP=ex < (NN < growth)) < CC=bad < (NP=bd < (VB < service)))\n" + "excise ex ex\n" + "delete bad\n" + "delete bd\n" + "adjoinF (VP VP@ (CC and) (VP (VB service) (NP (NN debt)))) adj\n" + '\n'
			) + ("@SBAR=home <1 /^-NONE-$/=emp <2 (@S < (@NP <1 (DT=bad < that|That) <-1 NNS))\n" + "delete emp\n" + "relabel bad IN\n" + "move bad >1 home\n" + '\n') + ("@NP < (@NP=gone < (NN < authority)) < (@PP=bad < (TO < to) < (NP=vp < (NN=newv < block) < (NNS=newnp < mergers)))\n"
			 + "excise gone gone\n" + "adjoin (S (NP-SBJ (-NONE- *)) VP@) bad\n" + "relabel vp VP\n" + "relabel newv VB\n" + "adjoin (NP NN@) newnp\n" + '\n') + ("@NP < (__=bad < well|Well|WELL $, (__=before < as|AS|As) $. (__=after < as|AS|As))\n" + "adjoinH (CONJP RB@) bad\n"
			 + "relabel bad CONJP\n" + "move before >1 bad\n" + "move after >-1 bad\n" + '\n')) + (("@NP < (/^``$/ < /^`$/) < (POS=bad < /^'$/)\n" + "relabel bad /''/\n" + '\n') + ("@NP < (IN|WDT=bad < /^(?:a|that|That)$/)\n" + "relabel bad DT\n" + '\n'
			) + ("@NP < (IN=bad < /^(?:so|about)$/)\n" + "relabel bad RB\n" + '\n') + ("@NP < (IN=bad < /^(?:fiscal|next)$/)\n" + "relabel bad JJ\n" + '\n') + ("@NP < (RB=bad < /^(?:a|that|Some)$/)\n" + "relabel bad DT\n" + '\n') + ("@NP < (RB=bad < most $. DT)\n"
			 + "relabel bad PDT\n" + '\n') + ("@NP < (RB=bad < /^(?:MORE)$/)\n" + "relabel bad JJR\n" + '\n') + ("@NP < (NN=bad < the)\n" + "relabel bad DT\n" + '\n') + ("@NP < (/^VB/=bad < won)\n" + "relabel bad NN\n" + '\n') + ("@NP < (/^RB/=bad < well|WELL|Well)\n"
			 + "relabel bad NN\n" + '\n') + ("@NP < (NN=bad < yen|won [ $- (CD !< one|One|1) | $- (@QP !< (CD < one|One|1)) ] )\n" + "relabel bad NNS\n" + '\n') + ("@NP < (NNP|NNS=bad < Democrats|Republicans)\n" + "relabel bad NNPS\n" + '\n') + ("@NP < (CD=bad < the)\n"
			 + "relabel bad DT\n" + '\n') + ("@NP < (JJ=bad < the)\n" + "relabel bad DT\n" + '\n') + ("@NP|NX < (NNP=bad < the)\n" + "relabel bad DT\n" + '\n') + ("@NP=bad < (NNP=badder < Technically|Historically)\n" + "relabel bad ADVP\n" + "relabel badder RB\n"
			 + '\n') + ("@NP < (RB=bad < /^(?:McNally)$/)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (RB=bad < /^(?:vice|night|multifamily|hand|fist)$/)\n" + "relabel bad NN\n" + '\n') + ("@NP < (RP=bad < /^(?:whole)$/)\n" + "relabel bad JJ\n" + '\n') 
			+ ("@NP < (RP=bad < Howard) < (NN=badder < /^A\\.$/)\n" + "relabel bad NNP\n" + "relabel badder NNP\n" + '\n') + ("@NP < (JJ=bad < (First , (__ !> /^``$/ !> /^-LRB-$/ !> /^PRP\\$$/)) $. NNP)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (JJ=bad < /^(?:U\\.S\\.|Sept\\.)$/)\n"
			 + "relabel bad NNP\n" + '\n') + ("@NP < (JJ=bad <1 Sharp) !<2 __\n" + "relabel bad NNP\n" + '\n') + ("@NP < (JJ=bad < /^(?:mine)$/)\n" + "relabel bad NN\n" + '\n') + ("@NP <-1 (JJ=bad < firm)\n" + "relabel bad NN\n" + '\n') + ("@NP < (JJ=bad < /^(?:ours)$/)\n"
			 + "relabel bad PRP\n" + '\n') + ("@NP < (JJ=bad < aluminum|plastic|textile)\n" + "relabel bad NN\n" + '\n') + ("@NP < (JJ=bad < gold|silver|bronze . stocks|bat|standard|reserves|prices|market|price|prices|fund|funds|consumers|use|commemorative|medal|bullion|exploration|producer|producers|trader|traders)\n"
			 + "relabel bad NN\n" + '\n') + ("@NP <: (JJ=bad < gold|silver|bronze)\n" + "relabel bad NN\n" + '\n') + ("@NP <2 (NN=bad < House $- (DT < /^[Tt]he$/))\n" + "relabel bad NNP\n" + '\n') + ("@NP < (NNP=bad < Democrats|Republicans)\n" + "relabel bad NNPS\n"
			 + '\n') + ("@NP < (NNS=bad < Democrats|Republicans , __)\n" + "relabel bad NNPS\n" + '\n') + ("@NP < (NN=bad < /^(?:Chapman|Ok|Oslo|Boeing|Jan\\.|Sept\\.|Oct\\.|Nov\\.|Dec\\.|Treasury|Esso)$/)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (NN=bad < /^(?:members|bureaus|days|outfits|institutes|innings|write-offs|wines|trade-offs|tie-ins|thrips|1980s|1920s|receivables|earnings)$/)\n"
			 + "relabel bad NNS\n" + '\n') + ("@NP < (NNP=bad < Dutch $. NN|NNS|JJ|VBG)\n" + "relabel bad JJ\n" + '\n') + ("@NP < (NN=bad < /^(?:this)$/)\n" + "relabel bad DT\n" + '\n') + ("@NP < (/^:/=bad < /^(?:')$/)\n" + "relabel bad /''/\n" + '\n')
			 + ("@NP < (NNS=bad < /^(?:start-up|ground-handling|word-processing|T-shirt|co-pilot|sell-off)$/)\n" + "relabel bad NN\n" + '\n') + ("@NP < (NNS=bad < /^(?:Sens\\.|Aichi|Asahi|Cincinnati|Hawaii|Pepsi)$/)\n" + "relabel bad NNP\n" + '\n') + (
			"@NP <1 (@NP=dest !< @NP . (__=wrong < /^\u0027s$/))\n" + "move wrong >-1 dest\n" + '\n') + ("@S < (@NP=bad < PRP < (VBZ=bottom < /^'s$/)) < (@VP=adj < VBN|VBG)\n" + "adjoin (VP (VBZ 's) VP@) adj\n" + "delete bottom\n" + '\n') + ("@S < (NP-SBJ < (PRP < I)) < (VP < (VB=bad < think) < SBAR)\n"
			 + "relabel bad VBP\n" + '\n') + ("@VP < (@NP=bad < (VBZ < kills|blames) < @NP)\n" + "excise bad bad\n" + '\n') + ("@NP < (VBZ=bad < /^'s$/) !< (NNP < Everything)\n" + "relabel bad POS\n" + '\n') + ("@NP|NX < (VBZ=bad !< /^(?:'s|kills)/)\n"
			 + "relabel bad NNS\n" + '\n') + ("@NP < (POS=bad < /^'s$/) > (@S > (@VP < (VB < let)))\n" + "relabel bad PRP\n" + '\n') + (("@S < (@VP < (VB=bad < /...ed$/ )) < (/^NP-SBJ/ !< /^-NONE-$/) > (__ !> __)\n" + "relabel bad VBD\n" + '\n') + ("@S < (@VP < (VB < let|cut|bid| )) < (/^NP-SBJ/ !< /^-NONE-$/) > (__ !> __)\n"
			 + "relabel bad VBD\n" + '\n') + ("@S < (@VP < (VB=bad !, Tait|MD|not )) < (/^NP-SBJ/ !< /^-NONE-$/) > (__ !> __)\n" + "relabel bad VBP\n" + '\n')) + ("@NP=bad < (VBP < are) > (@VP > (@S < NP-SBJ))\n" + "excise bad bad\n" + '\n') + ("@NP < (VBP=bad < charge)\n"
			 + "relabel bad NN\n" + '\n') + ("@NP < (VBP=bad < the)\n" + "relabel bad DT\n" + '\n') + ("@NP < (VBP=bad < we)\n" + "relabel bad PRP\n" + '\n') + ("@NP < (VBP=bad < /^[A-Z]/)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (VBN=bad < Applied !$ __)\n"
			 + "relabel bad NNP\n" + '\n') + ("@NP < (VBG=bad < preferred)\n" + "relabel bad VBN\n" + '\n') + ("@NP < (VB=bad < The)\n" + "relabel bad DT\n" + '\n') + ("@NP < (VB=bad < allowed)\n" + "relabel bad VBD\n" + '\n') + ("@NP <-1 (JJR=bad < cleaner)\n"
			 + "relabel bad NN\n" + '\n') + ("@NP < (VB=bad < /^(?:Nov\\.|Jan\\.|Dec\\.|Tandy|Release|Orkem|McDonald|Citicorp|Anne)$/)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (VB=bad < /^(?:short|key|many|last|further)$/)\n" + "relabel bad JJ\n" + '\n'
			) + ("@NP < (VB=bad < lower)\n" + "relabel bad JJR\n" + '\n') + ("@NP < (VB=bad < /^(?:spill|watch|review|risk|realestate|love|experience|control|Transport|mind|term|program|gender|audit|blame|stock|run|group|affect|rent|show|accord|change|finish|work|schedule|influence|school|freight|growth|travel|call|autograph|demand|abuse|return|defeat|pressure|bank|notice|tax|ooze|network|concern|pit|contract|cash|help|lunch|combat|pot|care|date|Streetspeak|face|effect|worry)$/)\n"
			 + "relabel bad NN\n" + '\n') + ("@NP <1 (NNP=bad < Officials|Cartoonists|Prices)\n" + "relabel bad NNS\n" + '\n') + ("@NP=badder < (NNP=bad < Currently)\n" + "relabel bad RB\n" + "relabel badder ADVP-TMP\n" + '\n') + ("@NP < (NN=bad < /^(?i:first|second|third|fourth)-quarter$/ $+ __)\n"
			 + "relabel bad JJ\n" + '\n') + ("@NP < (NNP < K $+ (NN=bad < mart))\n" + "relabel bad NNP\n" + '\n') + ("@NP < (PRP=bad < US & $. __)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (PRP=bad < her & $. __)\n" + "relabel bad /PRP$/\n" + '\n') + 
			("@NP <1 (PRP=bad < his) !<2 __\n" + "relabel bad /PRP$/\n" + '\n') + ("VBD=bad [ > @NP | > (@ADJP < CC|CONJP > @NP) ]\n" + "relabel bad VBN\n" + '\n') + ("@NP < (NN=bad < Time) < (NNP < Warner)\n" + "relabel bad NNP\n" + '\n') + ("@NP < (MD=bad < Can|May)\n"
			 + "relabel bad NNP\n" + '\n') + ("@NP <1 (DT=bad !< the $. (DT !< half))\n" + "relabel bad PDT\n" + '\n') + ("@NP=place < (@NP <1 DT !<2 __ $. (JJ=bad $. (NN=badder $. PP)))\n" + "move bad >-1 place\n" + "move badder >-1 place\n" + '\n') +
			 ("@NP=place < (@NP <1 DT|JJ !<2 __ $. (NN=bad $. PP))\n" + "move bad >-1 place\n" + '\n') + ("@NP < (NNPS=bad < NEWSPAPERS ! $ /^NN/)\n" + "relabel bad NNS\n" + '\n') + ("@NP < (@NP < (NNPS=bad < CERTIFICATES)) < (PP < (IN < OF) < (NP < (__ < DEPOSIT)))\n"
			 + "relabel bad NNS\n" + '\n') + ("@NP < (@NP < (__ < CERTIFICATES)) < (PP < (IN < OF) < (NP < (NNPS=bad < DEPOSIT)))\n" + "relabel bad NN\n" + '\n') + ("@NP < (NNP=bad < DISCOUNT) < (NNP=badder < RATE)\n" + "relabel bad NN\n" + "relabel bad NN\n"
			 + '\n') + ("@NP < (JJ=bad < DISCOUNT) < (NN < RATE)\n" + "relabel bad NN\n" + "relabel bad NN\n" + '\n') + ("@NP < (__ < chief $. (NN=bad < executive $. (NN < officer)))\n" + "relabel bad JJ\n" + '\n') + ("@NP < (NN=bad < chief $. (__ < executive $. (NN < officer)))\n"
			 + "relabel bad JJ\n" + '\n') + ("@NP <: (NNP=bad < /^'s$/)\n" + "relabel bad PRP\n" + '\n') + "NP-TMP=bad < (NN < Leisure)\n" + "relabel bad NP\n" + '\n' + "@NP < (RB=bad < well !$, (RB < as) $,, (DT < The|the))\n" + "relabel bad NN\n" + '\n'
			 + ("@NP < (NN < officer $- (NN=bad < executive $- (__ < chief)))\n" + "relabel bad JJ\n" + '\n') + ("@NP < (NN < officer $- (JJ < executive $- (NN=bad < chief)))\n" + "relabel bad JJ\n" + '\n') + ("@NP < (NNP=bad < Ad) < (/^NN/ < Notes)\n"
			 + "relabel bad NN\n" + '\n') + ("@NP < (/^NN/ < Ad) < (NNPS=bad < Notes)\n" + "relabel bad NNS\n" + '\n') + ("NP=bad < (/^NN/ < Ad) < (/^NN/ < Notes)\n" + "relabel bad NP-HLN\n" + '\n') + ("@NP < (NN=bad < nonperforming)\n" + "relabel bad JJ\n"
			 + '\n') + ("@NP < (IN=bad < next|Next) < NN|NNP\n" + "relabel bad JJ\n" + '\n') + ("NP-PRD=x < (JJ=y $+ (NN=z < simple))\n" + "relabel x ADJP-PRD\n" + "relabel y RB\n" + "relabel z JJ\n" + '\n') + ("NP-PRD=x <: (NN=z < simple)\n" + "relabel x ADJP-PRD\n"
			 + "relabel z JJ\n" + '\n') + ("NN=z < simple\n" + "relabel z JJ\n" + '\n') + string.Empty) + ("@PP < (IN < behind) < (@NP < PRP=bad < (RB=no < back))\n" + "relabel bad /PRP$/\n" + "relabel no NN\n" + '\n') + ("@SBAR=bad < (@WHNP < (WP < WHO)) < (S=badder < (VP < (@NP < (NN < NEWS))))\n"
			 + "relabel bad /SBARQ-HLN/\n" + "relabel badder SQ\n" + '\n') + ("@SBARQ < (@WHNP < (WP < WHO)) < (SQ < (VP < (@NP < (NNP=bad < NEWS))))\n" + "relabel bad NN\n" + '\n') + ("@SBARQ < (@WHNP < (WP < WHO)) < (S=bad < (VP < (@NP < (NN < NEWS))))\n"
			 + "relabel bad SQ\n" + '\n') + ("@WHNP < @WHNP=bad < @WHPP\n" + "relabel bad NP\n" + '\n') + ("/^WH/ < (WRB < /^(?i:how)$/) < (__=bad < (much !> JJ))\n" + "relabel bad JJ\n" + '\n') + ("@WHNP|WHADVP < (VBP|DT|IN=bad < /^(?i:that)$/) [ > (@SBAR > @NP|VP) | > (@SBAR > (@SBAR < /^(?:CC|CONJP|,)$/ > @NP|VP)) ]\n"
			 + "relabel bad WDT\n" + '\n') + ("@UCP < (RB=bad < multifamily)\n" + "relabel bad NN\n" + '\n') + ("@PRT < (RBR=bad < in)\n" + "relabel bad RP\n" + '\n') + ("@PRT < (NNP=bad < up)\n" + "relabel bad RP\n" + '\n') + ("@PP < (RP=bad < through) < @NP\n"
			 + "relabel bad IN\n" + '\n') + ("@PP < (RP|NN=bad < in) < @NP\n" + "relabel bad IN\n" + '\n') + ("@PP < (RB=bad < for|For|after|After|past|Past|under|Under)\n" + "relabel bad IN\n" + '\n') + ("@PP < (JJ=bad < if)\n" + "relabel bad IN\n" + 
			'\n') + ("@VP=bad < (IN < past) < @NP\n" + "relabel bad PP\n" + '\n') + ("@VP < (RB=bad < back $. (PRT < (RP < down))) > (@SINV|SQ|VP < MD)\n" + "relabel bad VB\n" + '\n') + ("@VP < (IN=bad < complicated) > @S\n" + "relabel bad VBD\n" + '\n'
			) + ("@VP < (IN=bad < near) > @VP\n" + "relabel bad VB\n" + '\n') + ("@VP < (IN=bad < like|post) > (@SQ|VP < /^(?:VB|MD)/)\n" + "relabel bad VB\n" + '\n') + ("@VP < (IN=bad < like|post) [ > @S | > (@VP < CC|CONJP > @S) ]\n" + "relabel bad VBP\n"
			 + '\n') + ("@VP < (/^VBD?$/=ins < take|sold) < (IN=bad < off)\n" + "delete bad\n" + "insert (PRT (RP off)) $- ins\n" + '\n') + ("@VP < NNS=bad\n" + "relabel bad VBZ\n" + '\n') + ("@VP < (NN=bad < /.{2}ing$/)\n" + "relabel bad VBG\n" + '\n'
			) + ("@VP < (NN=bad [ < set|beat|bid|redone|reset|hurt|underwritten|overrun | < /.{2}[^e]ed$/ ]) [ > (@VP < (/^VB/ < " + BeHaveGet + ")) | > (@NP < @NP)]\n" + "relabel bad VBN\n" + '\n') + ("@VP < (NN=bad < agreed|set|rebounded|fell) [ > @S | > (@VP < @CC|CONJP > @S) ]\n"
			 + "relabel bad VBD\n" + '\n') + ("@VP !< /^VB/ < (NN|NNP|JJ=bad !< /...(?i:ing)$/) [ > (@VP|SINV|SQ " + ModalDoTo + ") | > (@VP|SINV|UCP|SQ < CC|CONJP > (@VP|SINV|UCP|SQ " + ModalDoTo + ")) | > (@S > (@VP " + BareVpVerb + ")) | > (@VP " + 
			BareVpVerb + ") | > (@S < (/^NP-SBJ/ < /^-NONE-$/) > (@VP " + SayVerb + ")) ]\n" + "relabel bad VB\n" + '\n') + ("@VP !< /^VB/ < (NN=bad [ [ !< /s$/ & !< /e[dn]$/ & !< /ing$/ ] | < stress ] ) [ > @S | > (@VP < @CONJP|CC > @S) ]\n" + "relabel bad VBP\n"
			 + '\n') + ("@VP < (NN=bad < institutes) > @S\n" + "relabel bad VBZ\n" + '\n') + ("@VP < (VBP=bad !< /...(?i:ing)$/) [ > (@VP|SQ|SINV " + ModalDoTo + ") | > (@VP|UCP|SQ|SINV < CC|CONJP > (@VP|UCP|SQ|SINV " + ModalDoTo + ")) | > (@S > (@VP "
			 + BareVpVerb + ")) | > (@VP " + BareVpVerb + ") | > (@S < (/^NP-SBJ/ < /^-NONE-$/) > (@VP " + SayVerb + ")) ]\n" + "relabel bad VB\n" + '\n') + ("@VP < (VBN=bad !< /...(?i:ing)$/ !< /...(?i:ed)$/) [ > (@VP|SQ " + ModalDoTo + ") | > (@VP|SQ < CC|CONJP !< /^VB/ > (@VP|SQ "
			 + ModalDoTo + ")) | > (@SINV " + Modal + ") | > (@SINV < CC|CONJP !< /^VB/ > (@SINV " + Modal + ")) ]\n" + "relabel bad VB\n" + '\n') + ("@VP < (VBN=bad !< /...(?i:ing)$/) < (NP !< /^-NONE-$/) [ > (@S > (@VP " + BareVpVerb + ")) | > (@VP "
			 + BareVpVerb + ") | > (@S < (/^NP-SBJ/ < /^-NONE-$/) > (@VP " + SayVerb + ")) ]\n" + "relabel bad VB\n" + '\n') + ("@VP < (NN=bad < relocate) > (@VP < @CONJP > (@VP < MD))\n" + "relabel bad VB\n" + '\n') + ("@VP < (NN=bad < might|will)\n" 
			+ "relabel bad MD\n" + '\n') + ("@VP < (NNP=bad < /...(?i:ed)$/) [ > (SINV !> /TTL/) | > (@VP < @CONJP|CC > (SINV !> /TTL/)) ]\n" + "relabel bad VBD\n" + '\n') + ("@VP < (NNP=bad < /...(?i:ed)$/) [ > (S|S-HLN|S-ADV !> /TTL/) | > (@VP < @CONJP|CC > (S|S-HLN|S-ADV !> /TTL/)) | > (@VP < (__ < "
			 + BeHaveGet + ")) ]\n" + "relabel bad VBN\n" + '\n') + ("@VP < (NNP=bad < Got|Gotten) [ > (S|S-HLN|S-ADV !> /TTL/) | > (@VP < @CONJP|CC > (S|S-HLN|S-ADV !> /TTL/)) | > (@VP < (__ < " + BeHaveGet + ")) ]\n" + "relabel bad VBN\n" + '\n') + (
			"@VP < (NNP=bad < /..[^Ss](?i:s)$/) [ > (S|S-HLN|SINV !> /TTL/) | > (@VP < @CONJP|CC > (S|SINV|S-HLN !> /TTL/)) ]\n" + "relabel bad VBZ\n" + '\n') + ("@VP !< /^VB/ < (NNP=bad < /(?i:ing)$/)\n" + "relabel bad VBG\n" + '\n') + ("@VP < NNP=bad [ > (S|S-HLN !> /TTL/) | > (@VP < @CONJP|CC > (S|S-HLN !> /TTL/)) ]\n"
			 + "relabel bad VBP\n" + '\n') + ("@VP < (NNP=bad < are) [ > @S | > (@VP < @CONJP|CC > @S) ]\n" + "relabel bad VBP\n" + '\n') + ("@VP < (VBD < /^(?i:'d)$/) < (@VP < (VB=badder < seen))\n" + "relabel badder VBN\n" + '\n') + ("@VP < (VBP=bad < "
			 + ModalWord + ") < (@VP < VB)\n" + "relabel bad MD\n" + '\n') + ("@VP < (VBP=bad < /^(?i:'d)$/) < (@VP < VB)\n" + "relabel bad MD\n" + '\n') + ("@VP < POS=bad\n" + "relabel bad VBZ\n" + '\n') + ("@VP < (VBD=bad < heaves)\n" + "relabel bad VBZ\n"
			 + '\n') + ("@VP < (VB=bad < /.{2}[^e]ed$/) > @S\n" + "relabel bad VBD\n" + '\n') + ("@VP < (VB=bad < /^(?i:.{2,}[^e]ed|reset|run|become|hit|remade|gone|rid|put|hurt|become)$/) [ > (@VP < (/^VB/ < " + BeHaveGet + ")) | > (@NP < @NP) ]\n" + 
			"relabel bad VBN\n" + '\n') + ("@VP < (VBN=bad < has)\n" + "relabel bad VBZ\n" + '\n') + ("@VP < (VBN=bad < grew|fell|had) [ > @S | > (@VP < CONJP|CC > @S)]\n" + "relabel bad VBD\n" + '\n') + ("@VP < VBN=bad < (NP !< /^-NONE-$/) [ > (S < (@NP !< /^-NONE-$/)) | > (@VP < CONJP|CC > (S < (@NP !< /^-NONE-$/))) ]\n"
			 + "relabel bad VBD\n" + '\n') + ("@VP < VBN=bad [ > (S < (@NP !< /^-NONE-$/) > (__ !> __)) | > (@VP < CONJP|CC > (S < (@NP !< /^-NONE-$/) > (__ !> __))) ]\n" + "relabel bad VBD\n" + '\n') + ("@SBAR [ < (/^-NONE-$/ < /^0$/) | < (IN < that) ] < (@S < (NP-SBJ !< /^-NONE-$/) < (@VP < VBN=bad))\n"
			 + "relabel bad VBD\n" + '\n') + ("@VP < VB=bad [ > (S < (NP-SBJ !< /^-NONE-$/) > (__ !> __)) | > (@VP < CONJP|CC > (S < (NP-SBJ !< /^-NONE-$/) > (__ !> __))) ]\n" + "relabel bad VBP\n" + '\n') + ("@NP < @NP < (@SBAR < @WHNP < (@S < (VP < VB=bad)))\n"
			 + "relabel bad VBP\n" + '\n') + ("@VP < CONJP|CC <1 (VBP $.. VB=bad)\n" + "relabel bad VBP\n" + '\n') + ("@VP < (VBP=bad < has)\n" + "relabel bad VBZ\n" + '\n') + ("@VP < (JJ=bad < own|elaborate) [ > @S | > (@VP < CONJP|CC > @S)]\n" + "relabel bad VBP\n"
			 + '\n') + ("@VP < (JJ=bad < /..ing$/) < (@S < (@NP !< /^-NONE-$/) < (VP < TO))\n" + "relabel bad VBG\n" + '\n') + ("@VP < (JJ=bad < /...ed$/) [ > @S | > (@VP < CONJP|CC > @S)]\n" + "relabel bad VBD\n" + '\n') + ("@VP < (JJ=bad < pressured|known) [ > (@VP < (__ < "
			 + Be + ")) | > (@VP < CONJP|CC > (@VP < (__ < " + Be + "))) ]\n" + "relabel bad VBN\n" + '\n') + ("@VP < (JJ=bad < /(?i:..e[dn])$/) > (@VP < (__ < HAVE_AUX_WORD))\n" + "relabel bad VBN\n" + '\n') + ("@VP < (JJ=bad < /.{2}ing$/) [ > @S | > (@VP < CONJP|CC > @S)]\n"
			 + "relabel bad VBG\n" + '\n') + ("@VP < (JJ=bad < to)\n" + "relabel bad TO\n" + '\n') + ("@VP|S < (JJ=bad < all|ALL|All)\n" + "relabel bad RB\n" + '\n') + ("@VP < VBD=bad [ > (@VP < (/^VB/ < " + BeHaveGet + ")) | > (@VP < CONJP|CC > (@VP < (/^VB/ < "
			 + BeHaveGet + "))) | > (@NP < @NP) ]\n" + "relabel bad VBN\n" + '\n') + ("@VP < (VBD=bad < /[A-Z]/) > S-ADV\n" + "relabel bad VBN\n" + '\n') + ("@VP < (/^VB/ $. (IN=bad < up|off))\n" + "adjoin (PRT (RP@)) bad\n" + '\n') + ("@VP < /^VB/ < (/^(?:ADVP|PRT)\\|(?:ADVP|PRT)$/=bad < (RB|NN=badder < back))\n"
			 + "relabel bad PRT\n" + "relabel badder RP\n" + '\n') + ("@VP < (/^VB/ < win|wins|winning|won) < (@ADVP=bad < (RB=badder < back))\n" + "relabel bad PRT\n" + "relabel badder RP\n" + '\n') + ("@VP < (PDT=bad < all)\n" + "relabel bad RB\n" + 
			'\n') + ("@VP < (PRT < (VBP=bad < down))\n" + "relabel bad RP\n" + '\n') + ("@VP < (PRT=bad < (RBS < best))\n" + "relabel bad ADVP\n" + '\n') + ("@VP <1 (VB=bad < plea) <2 (NN=badder < bargain) > (@VP" + ModalDoTo + ")\n" + "relabel bad NN\n"
			 + "relabel badder VB\n" + '\n') + (("@ADJP < UH=bad\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (JJ < alive) < CC < (RB=bad < well)\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (JJ=bad < more)\n" + "relabel bad JJR\n" + '\n') + ("Korean > (NNP=x $- (NNP=y < South))\n"
			 + "relabel x JJ\n" + "relabel y JJ\n" + '\n') + ("@ADJP <1 (NNP=x < /^(?:New|San|Los|Des|St\\.|Washington|Hong)$/) <2 (__ < /^(?:York|Francisco|Angeles|London|Orleans|Zealand|Diego|Moines|Louis|D\\.C\\.|Kong)-based$/) !<3 __\n" + "relabel x JJ\n"
			 + '\n') + ("@ADJP <1 (__ < New|San|Los) <2 (NNP=y < /^(?:York|Francisco|Angeles)-based$/) !<3 __\n" + "relabel y JJ\n" + '\n') + ("@ADJP < (NN=bad < firm|due|permissible)\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (NNS=bad < due)\n" + "relabel bad JJ\n"
			 + '\n') + ("@ADJP < (NNP=bad < READY)\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (RB=bad < free|clear|tight|sure|particular|due)\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (RB=bad < likely) > @VP\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (VB=bad < /^(?i:stock|No\\.)$/)\n"
			 + "relabel bad NN\n" + '\n') + ("@ADJP < (VBP=bad < fit|close)\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < (VB=bad < secure|keen|quiet)\n" + "relabel bad JJ\n" + '\n') + ("@ADJP < JJ < (IN=bad < that)\n" + "relabel bad RB\n" + '\n') + string.Empty
			) + ("@QP < (IN|JJ|RBR|RP=bad < about)\n" + "relabel bad RB\n" + '\n') + ("@QP < (JJ=bad < as)\n" + "relabel bad RB\n" + '\n') + ("@QP < (JJ|JJS=bad < more|less)\n" + "relabel bad JJR\n" + '\n') + ("@QP < (RP=bad < up $. (TO <to))\n" + "relabel bad IN\n"
			 + '\n') + ("@ADVP < EX=bad\n" + "relabel bad RB\n" + '\n') + ("@ADVP < (NN=bad < that)\n" + "relabel bad DT\n" + '\n') + ("@ADVP < (NNP=bad [ < /.{2}ly$/ | < Overall | < Systemwide ])\n" + "relabel bad RB\n" + '\n') + ("@ADVP < (RP=bad < around|before)\n"
			 + "relabel bad RB\n" + '\n') + ("@ADVP=bad <1 PRT !<2 __\n" + "excise bad bad\n" + '\n') + ("@ADVP < (VBD=bad < let) < (RB=badder < alone)\n" + "relabel bad VB\n" + "relabel badder JJ\n" + '\n') + ("@ADVP < VBD=bad\n" + "relabel bad VBN\n"
			 + '\n') + ("@SBAR < (DT|WDT|NN|NNP|RB=bad < that|because|while|Though|Whether)\n" + "relabel bad IN\n" + '\n') + ("@SQ < VB=bad\n" + "relabel bad VBP\n" + '\n') + ("@SQ < (NNS=bad $. NP-SBJ)\n" + "relabel bad VBZ\n" + '\n') + ("@SQ < (NNP=bad < Does)\n"
			 + "relabel bad VBZ\n" + '\n') + ("@SQ < (NNP=bad < Should)\n" + "relabel bad MD\n" + '\n') + ("@X < (JJS=bad < more|less)\n" + "relabel bad JJR\n" + '\n') + ("@INTJ < (RB=bad < well|WELL|Well)\n" + "relabel bad UH\n" + '\n') + ("@INTJ < (NNP=bad < UH|HUH)\n"
			 + "relabel bad UH\n" + '\n') + ("JJ=bad < /^%$/\n" + "relabel bad NN\n" + '\n') + ("NN|NNP|JJ|IN=bad < and\n" + "relabel bad CC\n" + '\n') + ("VB=bad < even\n" + "relabel bad RB\n" + '\n') + ("/^,$/=bad < /^2$/\n" + "relabel bad CD\n" + '\n'
			) + ("/^,$/=bad < an\n" + "relabel bad DT\n" + '\n') + ("/^,$/=bad < Wa\n" + "relabel bad NNP\n" + '\n') + ("/^,$/=bad < section\n" + "relabel bad NN\n" + '\n') + ("/^,$/=bad < underwriters\n" + "relabel bad NNS\n" + '\n') + ("CD=bad < high-risk\n"
			 + "relabel bad JJ\n" + '\n') + ("RB|RP|NN=bad < for|at\n" + "relabel bad IN\n" + '\n') + ("NN=bad [ < /^.\\.$/ | < Lorillard ]\n" + "relabel bad NNP\n" + '\n') + ("JJS=bad < StatesWest\n" + "relabel bad NNP\n" + '\n') + ("JJR=bad < Richter|Gartner\n"
			 + "relabel bad NNP\n" + '\n') + ("IN|JJ|NN|NNP=bad < /^[Aa][Nn][Dd]$/\n" + "relabel bad CC\n" + '\n') + ("/^(?:ADVP|ADVP-TMP.*)$/=badder < (RB=bad < ago) < @NP\n" + "relabel bad IN\n" + "relabel badder PP-TMP\n" + '\n') + ("ADV-TMP=bad < (ADVP|NP=badder < (IN < ago) < @NP)\n"
			 + "relabel bad PP-TMP\n" + "relabel badder PP\n" + '\n') + ("/^(?:ADVP|NP)/=badder < (IN < ago) < @NP\n" + "relabel badder PP-TMP\n" + '\n') + ("@NP=badder < (RB=bad < ago) < @NP\n" + "relabel bad IN\n" + "relabel badder PP-TMP\n" + '\n') 
			+ ("/^ADVP-TMP/=badder < (RB < not|Not|so|So) < (RB=jj < long) < (RB=bad < ago)\n" + "adjoin (PP-TMP NP@ (IN ago)) badder\n" + "delete bad\n" + "relabel jj JJ\n" + '\n') + ("/^ADVP-TMP/=badder <1 (RB|JJ=jj < long) <2 (RB=bad < ago)\n" + "adjoin (PP-TMP NP@ (IN ago)) badder\n"
			 + "delete bad\n" + "relabel jj JJ\n" + '\n') + ("ADJP=badder <1 (NN=baddest < Year) <2 (RB=bad < ago)\n" + "adjoin (NP NN@) baddest\n" + "relabel badder PP-TMP\n" + "relabel bad IN\n" + '\n') + ("@NP=bad <1 NP <2 (ADVP-TMP=badder < (RB=baddest < ago))\n"
			 + "relabel bad PP-TMP\n" + "excise badder badder\n" + "relabel baddest IN\n" + '\n') + ("/^NP/=badder < DT|CD < NN|NNS < (RB|IN=bad < ago)\n" + "adjoin (PP-TMP NP@ (IN ago)) badder\n" + "delete bad\n" + '\n') + ("ADVP-TMP=badder < (IN < ago) < @ADVP\n"
			 + "relabel badder PP-TMP\n" + '\n') + ("RB=bad < newsweekly\n" + "relabel bad NN\n" + '\n') + ("RB=bad < stocks\n" + "relabel bad NNS\n" + '\n') + ("NN=bad < PaineWebber\n" + "relabel bad NNP\n" + '\n') + ("NNP=bad < Though\n" + "relabel bad IN\n"
			 + '\n') + ("@PP < (IN < /^(?i:at)$/ $. RBS=adj)\n" + "adjoin (NP RBS@) adj\n" + '\n') + ("@SBAR < (/^WP\\$$/=bad $. (@WHNP=dest < NN))\n" + "move bad >1 dest\n" + '\n') + ("@SBAR=sbar < (/^WP\\$$/=wrong $. (S=ins < NP-SBJ=fix < VP))\n" + "move wrong >1 fix\n"
			 + "relabel fix WHNP\n" + "move fix >1 sbar\n" + "insert (NP-SBJ (-NONE- *T*)) >1 ins\n" + '\n') + ("@SBAR <1 @WHNP <2 @S=loc <3 @VP=bad !<4 __\n" + "move bad >-1 loc\n" + '\n') + ("@WHNP < (@WHADVP=bad < (WRB < /^(?i:how)$/) < (JJ < many|much)) < NN|NNS|NNP|NNPS\n"
			 + "relabel bad WHADJP\n" + '\n') + ("@WHNP < (@NP=bad < /^(?:WP\\$|WDT|WRB)$/ $.. @PP|PRN|NP)\n" + "relabel bad WHNP\n" + '\n') + ("@WHNP < (@WHADVP=bad < (WRB < /^(?i:how)$/)) < (JJ < much)\n" + "excise bad bad\n" + '\n') + ("@WHNP < @NP < (@PP=bad < (@WHNP < WDT|WP))\n"
			 + "relabel bad WHPP\n" + '\n') + ("@WHNP < (WP=bad $.. NN|NNS !$.. DT)\n" + "relabel bad WDT\n" + '\n') + ("@WHNP < @NP < (@PP=bad < (@NP=badder < WDT))\n" + "relabel bad WHPP\n" + "relabel badder WHNP\n" + '\n') + ("@PP <1 (IN|TO $. (@NP=place < ADJP)) <-1 NN|NNS|NNP|NNPS=word\n"
			 + "move word >-1 place\n" + '\n') + ("@PP=head <1 IN|TO=prep <-1 NN|NNS|NNP|NNPS\n" + "adjoinH (PP NP@) head\n" + "move prep >1 head\n" + '\n') + ("@S < (@SBAR < (SBAR < SINV $. (CC $. (SBAR=adj < VBD < S))))\n" + "adjoin (SBAR SINV@) adj\n"
			 + '\n') + ("@SINV < (NP-SBJ=subj $. (@VP=base $.. (@VP=say < (/^VB/ < say|says|said))))\n" + "adjoinH (S VP@) base\n" + "move subj >1 base\n" + "relabel base S-1\n" + "insert (S (-NONE- *T*-1)) >-1 say\n" + '\n') + ("NP-SBJ=bad < NP-TMP !< NP < PP-TMP\n"
			 + "excise bad bad\n" + '\n') + ("@S < NP-SBJ < (NP=bad < (/^NN/ < Tuesday|yesterday|Yesterday))\n" + "relabel bad NP-TMP\n" + '\n') + ("@S=bad < (VBP $. (NP-SBJ $. VP))\n" + "relabel bad SINV\n" + '\n') + ("@S < (/^NP-SBJ/ $. (VBP|VBZ=aux $. @VP=adj))\n"
			 + "adjoinH (VP VP@) adj\n" + "move aux >1 adj\n" + '\n') + ("@S < (/^NP-SBJ/ $. (VBP|VBZ=aux $. @ADJP=adj))\n" + "adjoinH (VP ADJP@) adj\n" + "move aux >1 adj\n" + '\n') + ("@PP=adj < (IN=prep $. JJ)\n" + "adjoinH (PP NP@) adj\n" + "move prep >1 adj\n"
			 + '\n') + ("@SBARQ <1 @WHADVP <2 VBZ=bad <3 RB=badder <4 @SQ=loc\n" + "move badder >1 loc\n" + "move bad >1 loc\n" + '\n') + ("@SBARQ <2 @WHADVP <3 MD=bad <4 @SQ=loc\n" + "move bad >1 loc\n" + '\n') + ("@SBARQ <2 @WHNP <3 VBZ=bad <4 @SQ=loc\n"
			 + "move bad >1 loc\n" + '\n') + ("@VP=adj < (VBN=bad < been) < (JJ < unable) !< CC|CONJP\n" + "adjoin (VP (VBN been) ADJP@) adj\n" + "delete bad\n" + '\n') + ("@S < (@NP < (NNS=bad < runs)) < (VP=home !< /^VB/ < (IN=badder < up))\n" + "relabel bad VBZ\n"
			 + "relabel badder RP\n" + "move bad >1 home\n" + "adjoin (PRT RP@) badder\n" + '\n') + ("@VP=top < (MD=bottom < will $. /^VB/)\n" + "adjoin (VP (MD will) VP@) top\n" + "delete bottom\n" + '\n') + ("@VP < (MD=bad < /^'d$/)\n" + "relabel bad VBD\n"
			 + '\n') + ("@S < (TO=bottom < to $. (@VP=top < /^VB/))\n" + "adjoin (VP (TO to) VP@) top\n" + "delete bottom\n" + '\n') + ("@S < /^NP-SBJ/ < (VP < (VBD < " + Do + ") < (@NP=bad < (NN=badder < work)))\n" + "relabel bad VP\n" + "relabel badder VB\n"
			 + '\n') + ("@SBARQ !< @SQ !< /^-NONE-$/ !< @SBARQ < (@S|SINV=bad < VBP|VBZ|MD|VBD)\n" + "relabel bad SQ\n" + '\n') + ("@SBARQ < (@SINV=bad < (__ < would))\n" + "relabel bad SQ\n" + '\n') + ("@NP=top < @NP=bottom !<2 __\n" + "excise bottom bottom\n"
			 + '\n') + ("NP-SBJ=bad < (RB < Earlier)\n" + "relabel bad NP-TMP\n" + '\n') + ("@NP < (RB=bad < late|early $+ /^NN/)\n" + "relabel bad JJ\n" + '\n') + ("@NP <1 (DT < a|an) <2 (JJ=bad < Hungary) !<3 __\n" + "relabel bad NNP\n" + '\n') + ("@NP <1 (DT < a|an) <2 (JJ=bad < /^(?:official|deterrant|bible|academic|fine|buy-out|perk|installment)$/) !<3 __\n"
			 + "relabel bad NN\n" + '\n') + ("@VP < (/^VB/ < help|helps|helped|helping|start|started|starts|starting|begin|begins|began|beginning) < (@VP=site < VB)\n" + "adjoin (S (NP-SBJ (-NONE- *)) VP@) site\n" + '\n') + ("@NP < CD < (NNP < Drugs=bad)\n"
			 + "relabel bad NNS\n" + '\n') + ("well|Well|WELL [ , /^[:,]$/ | !, __ ] . /^[:,]$/ > (RB > ADVP)\n" + "relabel bad UH\n" + "relabel badder INTJ\n" + '\n') + (("NNP=bad < Securities|Manufacturers|Enterprises|Securities|Resources|Corporations|Sports|Merchants|Industries|Holdings|Brothers|Airlines|Systems|Motors|Industries|Parks|Communications|Facilities|Technologies|Sons|Publications|Products|Nations|Monopolies|Mergers|Machines|INDUSTRIES|Giants|Firearms|Associates|ASSOCIATES\n"
			 + "relabel bad NNPS\n" + '\n') + ("@NP < (NN=bad < chief $++ /^NN/)\n" + "relabel bad JJ\n" + '\n') + ("@NP < (VBG=bad < operating|Operating $++ /^NN/)\n" + "relabel bad NN\n" + '\n') + ("@NP <- (DT=bad < half)\n" + "relabel bad NN\n" + '\n'
			) + ("@ADVP < (RB=bad < easier|harder|earlier)\n" + "relabel bad RBR\n" + '\n') + ("@ADJP < (RB|JJ=bad < easier|harder|earlier)\n" + "relabel bad JJR\n" + '\n') + ("NN=bad < salespeople\n" + "relabel bad NNS\n" + '\n') + ("NN=bad < Chevrolet\n"
			 + "relabel bad NNP\n" + '\n') + ("NNS=bad < Caltrans\n" + "relabel bad NNP\n" + '\n') + ("NNS=bad < Dirks\n" + "relabel bad NNP\n" + '\n') + ("NN=bad < /^U\\.K\\.$/\n" + "relabel bad NNP\n" + '\n') + ("@PP < (JJR=bad < more|less)\n" + "relabel bad RBR\n"
			 + '\n') + ("@PP < (@ADVP < (JJR=bad < more|less))\n" + "relabel bad RBR\n" + '\n') + ("RB=bad < earlier\n" + "relabel bad RBR\n" + '\n') + ("@NP <- (NN < month|year) < (@QP < (RBR=bad < less|more) < (IN < than))\n" + "relabel bad JJR\n" + 
			'\n') + ("@NP < (NNP=bad < Afghan $+ /^NN/)\n" + "relabel bad JJ\n" + '\n') + ("NNS=bad < headquarters\n" + "relabel bad NN\n" + '\n') + ("@NP < (NN=bad < managing)\n" + "relabel bad VBG\n" + '\n') + ("@NP < (NNS=x < East) < (NNS=y < Germans)\n"
			 + "relabel x NNPS\n" + "relabel y NNPS\n" + '\n') + ("@NP < DT < (NNS=y < Germans)\n" + "relabel y NNPS\n" + '\n') + ("@NP < (NN=y < /^S&P$/)\n" + "relabel y NNP\n" + '\n') + ("JJ=y < benchmark\n" + "relabel y NN\n" + '\n') + ("JJ=y < Aeroflot\n"
			 + "relabel y NNP\n" + '\n') + ("NNPS=y < ADRs\n" + "relabel y NNS\n" + '\n') + ("NN=y < telecommunications\n" + "relabel y NNS\n" + '\n') + ("@NP < (NN=y < executive $+ (NN|NNS < officer|officers|vice))\n" + "relabel y JJ\n" + '\n') + ("@NP < (CD|NN=y < /^(?:'[0-9]0s|1[1-9][0-9]0s)$/)\n"
			 + "relabel y NNS\n" + '\n') + ("IN|CC|NN|JJ=bad < /^(vs\\.|versus)$/\n" + "relabel bad FW\n" + '\n') + ("NN=bad < /^U\\.S\\.A\\.$/\n" + "relabel bad NNP\n" + '\n') + string.Empty) + string.Empty;
		// Note that backslashes and dollar signs need to be escaped.
		// Maybe we should have an easy syntax for invoking Matcher.quoteReplacement?
		// 1. Bung tree fixing
		// NOTE: if you add more of these, make sure to group the string concatenations
		// into chunks using parentheses, or the compiler will choke with a
		// StackOverflowError (!)
		// Fix a bad parse in wsj_0415.mrg
		// sec 24
		// sec 22 bad parse!
		// Fix some cases of 'as well as' not made into a CONJP unit
		// There are a few other wierd cases that should also be reviewed with the tregex
		// well|Well|WELL , as|AS|As . as|AS|As !>(__ > @CONJP)
		// but note that there are also non-CONJP uses as adverbial form of 'as good as'
		// This bleeds retagging of 'well' inside NP below
		// 2. POS tag fixing
		// 2.a. Ones specific to a phrasal category
		// 2.a.i NP
		// "well". Other rules for under INTJ further below.
		// treat like "sheep": if it would be "dollars", make it plural
		// it's 69 NNPS, 17 NNS, 3 NNP for Democrats; 46, 17, 3 for Republicans; all political party mentions
		// noun usages (mining)
		// noun usages
		// don't regard aluminum as an adjective (color)
		// all uses of plastic are also for the noun, not adjectivally for things showing plasticity
		// gold and silver can validly be colours but are usually metals in WSJ.  Use noun to decide
		// gold and silver can validly be colours but are usually metals in WSJ.  Use noun to decide
		// make all uses of 'the House' (Congress) NNP, like most, and
		// like all uses of 'the Senate'
		// not clear why Sens isn't NNPS
		// VBZ under @NP.  Strong rule ordering!
		// move up misplaced possessives
		// fix bung syntax in wsj_0295 and wsj_1142
		// and an extra weird NP in wsj_0446 and wsj_1101
		// then turn all 's VBZ ino a POS
		// except for Everything's a Dollar Inc. !
		// and then turn all other VBZ that aren't the 's into NNS.
		// 100% precision now!
		// CHECK IF CAN DELETE kills HERE NOW!
		// but turn POS under NP to PRP if it is the 's of let's
		// fix VB at root level
		// these ones are VBD
		// these ones are also VBD in practice (though in principle ambiguous)
		// Then, except in a few weird cases (treebank err, missing letter, reduced "better not" they should be VBP
		// VBP under NP.  First fix one that should be a verb!  NP is wrong
		// VBN under NP
		// nth-quarter as a modifier: may as well treat it as JJ as manual says and majority of instances are
		// may as well take all of "K mart" as a proper noun
		// there are a number of cases of 'the the' -- errors in sources or
		// mistakes in Perl scripts?
		// and one case of a/DT half/DT
		// filling out NP with PP modifier.  This rule should precede
		// filling out NP with PP modifier.  This rule should follow
		// NEWSPAPERS
		// chief executive officer
		// chief executive officer
		// the infamous "Ad Notes"
		// the infamous "Ad Notes"
		// the infamous "Ad Notes"
		// this needs to be after the above two "simple" ones
		// equally infamous "Who's News:"
		// equally infamous "Who's News:"
		// equally infamous "Who's News:"
		// How much always has 'much' as a JJ
		// the VP is for extraposed relatives....
		// PP parent
		// VP parent
		// NNS under VP.  Easy.
		// ordered rules for NN under VP
		// first fix the ones ending in -ing
		// contexts where an untensed verb should occur
		// an imperative is VB!  The 'say' cases are direct speech imperatives
		// file should maybe be VB - subjunctive (look at higher verb? wsj_0979)
		// not just anything ending in s.  Definitely not if -ss, but also 'focus'
		// Don't allow SINV for VBN as get participial clauses
		// unless the SINV has a modal under it
		// Allow VBN in complement of SEE or HEAR (but not LET or MAKE)
		// for transitive, it can't be VBN under see/hear
		// ordered rules for NNP under VP
		// it's VBD for SINV
		// it's VBN not VBD because the one or two there are headline-speak
		// it's VBN not VBD because the one or two there are headline-speak
		// ordered rules for NNP under VP  (2 dots as want to get "Adds")
		// ordered rules for NNP under VP
		// ordered rules for NNP under VP
		// this one is in a title....
		/* ----------------------------
		tregex3 -f -w '@VP < NN' | & less
		
		// unresolved: some bid, ask cases
		
		// so far only done 1-1000 inclusive
		
		} else if (word.equals("reconfirm")) {
		cat = changeBaseCat(cat, "VB");
		} else if (word.equals("rebounded")) {
		cat = changeBaseCat(cat, "VBD");
		}
		// still need to do NNP > VP rules as well!
		--------------------- */
		/*
		} else if (baseCat.equals("NNP")) {
		if (word.equals("GRAB")) {
		cat = changeBaseCat(cat, "VBP");
		} else if (word.equals("mature")) {
		cat = changeBaseCat(cat, "VB");
		} else if (word.equals("Face")) {
		cat = changeBaseCat(cat, "VBP");
		} else if (word.equals("are")) {
		cat = changeBaseCat(cat, "VBP");
		} else if (word.equals("say")) {
		cat = changeBaseCat(cat, "VBP");
		} else if (word.equals("Added")) {
		cat = changeBaseCat(cat, "VBD");
		} else if (word.equals("Adds")) {
		cat = changeBaseCat(cat, "VBZ");
		} else if (word.equals("BRACED")) {
		cat = changeBaseCat(cat, "VBD");
		} else if (word.equals("REQUIRED")) {
		cat = changeBaseCat(cat, "VBN");
		} else if (word.equals("REVIEW")) {
		cat = changeBaseCat(cat, "VB");
		} else if (word.equals("code-named")) {
		cat = changeBaseCat(cat, "VBN");
		} else if (word.equals("Printed")) {
		cat = changeBaseCat(cat, "VBN");
		} else if (word.equals("Rated")) {
		cat = changeBaseCat(cat, "VBN");
		} else if (word.equals("FALTERS")) {
		cat = changeBaseCat(cat, "VBZ");
		} else if (word.equals("Got")) {
		cat = changeBaseCat(cat, "VBN");
		} else if (word.equals("Adds")) {
		cat = changeBaseCat(cat, "VBZ");
		}
		*/
		// Modal fixes: of 'd tagged as VBD and others tagged VBP
		// first fix one where the complement verb tag is wrong
		// this must be ordered before the have/be auxiliary complement fix!
		// Doing the VBN to VBD is too general because of auxiliary deletion in
		// contracted constructions, headlines, etc.
		// Nevertheless, there are quite a few cases....
		// Try ones with objects??
		// "@VP < VBN=bad [ > @S | > (@VP < CONJP|CC > @S) ]\n" +
		// "relabel bad VBN\n" +
		// "\n" +
		// Below are three patterns that doesn't overgeneralize and still gets a
		// lot of the cases: note that they don't use @ so as to not match NP-TMP
		// or S-HLN, and checks for non-NULL constituents
		// must be finite in clause with overt NPsubj and NPobj
		// root main clauses with overt subj must be finite
		// that clauses (or that-less that clauses) with overt subjects are finite
		// similar but more limited corrections of VB to VBP
		// have to beware getting VB in imperatives, subjunctives, etc.
		// root main clauses with overt subj must be finite
		// Should have a finite VBP not a VB in a finite relative clause
		// JJ under VP rewrites (see also above for BARE_VP_VERB generalizations)
		// VBN after have/be auxiliaries
		// Asked, Warned, etc. at start of sentence
		// remove the two (!) unsure phrasal categories: ADVP|PRT, PRT|ADVP
		// make other instances of 'win back' particle verbs
		// TODO: do other instances of put back.  Worth looking at Treebank II -PUT
		// ADJP rules
		// "alive and well"
		// consider also "hard" -- many but not all cases should be JJ
		// reconsider also cases not under VP, but some looked more complex....
		// look at 'as' examples, but don't want to maul 'as much as X' idiom
		// need to work out whether to relabel all the RBR ones similarly....
		// there's some IN/RB variability for this up ... what's correct/why?
		// special for let
		// in general must be non finite
		// Under INTJ
		// non-phrasally rooted POS tag corrections
		// bad comma tags section
		// This is for 'ago' as adverb
		// TODO: reconsider this -- I think Pullum argues for IN
		// "IN=bad < ago\n" +
		// "relabel bad RB\n" +
		// "\n" +
		// The next several are for 'ago' as postposition IN
		// NP is marked -ADV at least once... (strip it?)
		// TODO: for cases embedded in ADV-TMP: "a year ago, when X was snamed pres":
		// higher one PP-TMP.
		// TODO: Provisional rule for this tree; should be revised!
		//    (ADVP-TMP
		//     (ADVP
		//      (ADVP (RB As) (RB long))
		//      (PP (IN as)
		//       (NP (DT a) (NN decade))))
		//     (IN ago))
		// TODO: this suggests a postposing in the above sentence! Do as movement?
		//    (ADVP-TMP
		//     (ADVP (RB as) (JJ long) (RB ago))
		//     (PP (IN as)
		//      (NP (JJ early) (CD 1988))))
		// "RB=bad < ago\n" +
		// "relabel bad IN\n" +
		// "\n" +
		/*
		(ADVP-TMP
		(ADVP
		(NP (CD 25) (NNS years))
		(RB ago))
		(, ,)
		(SBAR
		(WHADVP-1 (WRB when))
		
		(NP-TMP
		(ADVP
		(NP (DT a) (NN year))
		(RB ago))
		(, ,)
		(SBAR
		(WHADVP-1 (WRB when))
		
		(ADVP
		(NP (DT a) (NN year))
		(RB ago))))
		
		(ADVP-TMP
		(ADVP
		(NP (DT a) (NN year))
		(RB ago))
		(, ,)
		(SBAR
		(WHADVP-1 (WRB when))
		(S
		
		(PP-DIR (IN from)
		(ADVP
		(NP (DT a) (NN year))
		(RB ago)))))))))
		
		(NP (DT the) (NN use))
		(PP (IN of)
		(NP (NNS incentives)))
		(UCP-TMP
		(NP (DT this) (NN year))
		(CC and)
		(RB not)
		(ADVP
		(NP (DT a) (NN year))
		(RB ago)))))
		
		(ADVP-TMP (RB so) (RB long) (RB ago))))
		
		(PP-DIR (IN from)
		(NP-TMP
		(NP (DT a) (NN year))
		(IN ago))))))
		
		(NP-TMP (DT a) (NN year) (IN ago)))))))
		
		(PP-TMP (IN from)
		(ADVP
		(NP (DT a) (NN year))
		(IN ago)))
		
		(ADVP-TMP
		(ADVP
		(NP (CD five) (NNS years))
		(IN ago))
		(, ,)
		(SBAR
		(WHPP-2 (IN at)
		(WHNP (WDT which) (NN time)))
		
		(ADVP
		(NP
		(QP (JJR more) (IN than) (CD 30))
		(NNS years))
		(IN ago))
		(, ,)
		(SBAR
		(WHADVP (WRB when))
		
		(NP-SBJ-1
		(ADJP (NN Year) (RB ago))
		(NN figure))
		
		(PP-TMP (IN until)
		(ADVP
		(NP (CD three) (NNS years))
		(RB ago))))))))
		
		(PP-TMP (IN from)
		(ADVP
		(NP (CD six) (NNS years))
		(RB ago)))))
		
		(PP (IN of)
		(NP
		(NP (RB about) (CD two) (NNS decades))
		(ADVP-TMP (RB ago)))))))
		
		(PP-DIR (IN from)
		(ADVP
		(NP (DT a) (NN year))
		(RB ago))))))))
		
		(NP-TMP
		(NP (DT a) (NN year))
		(RB ago))))
		
		(PP-TMP (IN from)
		(ADVP
		(NP (DT a) (NN year))
		(IN ago)))))))))
		
		(NP
		(ADVP
		(NP (CD six) (NNS months))
		(IN ago))
		(, ,)
		(SBAR
		(WHADVP-1 (WRB when))
		
		(PP-TMP (IN from)
		(ADVP
		(NP (DT a) (NN year))
		(IN ago)))
		
		(NP-TMP
		(ADVP
		(NP (DT a) (NN year))
		(IN ago))
		(, ,)
		(SBAR
		(WHADVP-2 (WRB when))
		
		(PP-TMP (IN until)
		(NP (DT a) (JJ few) (NNS years) (IN ago)))
		
		(ADVP-TMP (RB long) (RB ago))
		*/
		// phrase structure stuff / phrasal categories
		// at best/least
		// Wh-phrases
		// treee in wsj_1447.
		// tree in wsj_2155
		// tree in wsj_1457
		// Make a "What" before a noun a WDT (Santorini p.22)
		// Except not when there's a DT, since I'm unsure of the "What a good feeling" case....
		// ordered PP with noun directly in it - fix wrong size NP
		// ordered PP with noun directly in it - fix missing NP
		// these two rules show why an "insert above" operation would be nice....
		// tree in wsj_0755
		// fix a bad tree in wsj_1623
		// NP over NP with nothing else.
		// clear cases of NN(P) tagged as JJ after an indefinite article; unclear cases are left JJ
		// spelling of "deterrant" is how it appears in wrong tree!
		// most complements of help take an S complement (ECM analysis),
		// but some with null subjects don't, just a VP.  Fix them so they do.
		// This is in a NX-TTL, but just NNP is wrong on both counts so make NNS
		// "well" that should be interjection, but done as (ADVP (RB well))
		// last minute; check more carefully
		// not yet done
		// 94 NP < (RB < much) -- many are determinatives (JJ?)
		// lots of weird stuff in NP < RB !!
		// Lots of PP < RP that should be consistentized somehow!
		// Don Blaheta
		// Markus Dickinson (does he cite Blaheta??)
		// Ratnaparkhi tagger paper
		// Singer et al. POS tagging paper
		// Huddleston and Pullum analyze 'ago' as IN.  Check Treebank guidelines.
		// turn off markCC in definition of goodFactored.  It just doesn't help.
		// Naiwen Xue et al. for CTB discuss similar style of rules and tgrep error
		// detection and correction phase.
	}
}

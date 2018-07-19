using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.International.Spanish;
using Edu.Stanford.Nlp.International.Spanish.Process;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon;
using Edu.Stanford.Nlp.Util;
using Java.Lang;
using Java.Util;
using Java.Util.Function;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.International.Spanish
{
	/// <summary>Normalize trees read from the AnCora Spanish corpus.</summary>
	/// <author>Jon Gauthier</author>
	[System.Serializable]
	public class SpanishTreeNormalizer : BobChrisTreeNormalizer
	{
		/// <summary>
		/// Tag provided to words which are extracted from a multi-word token
		/// into their own independent nodes.
		/// </summary>
		public const string MwTag = "MW?";

		/// <summary>Tag provided to constituents which contain words from MW tokens</summary>
		public const string MwPhraseTag = "MW_PHRASE?";

		public const string EmptyLeafValue = "=NONE=";

		public const string LeftParenthesis = "=LRB=";

		public const string RightParenthesis = "=RRB=";

		private static readonly IDictionary<string, string> spellingFixes = new Dictionary<string, string>();

		static SpanishTreeNormalizer()
		{
			spellingFixes["embargp"] = "embargo";
			// 18381_20000322.tbf-4
			spellingFixes["jucio"] = "juicio";
			// 4800_2000406.tbf-5
			spellingFixes["méxico"] = "México";
			// 111_C-3.tbf-17
			spellingFixes["reirse"] = "reírse";
			// 140_20011102.tbf-13
			spellingFixes["tambien"] = "también";
			// 41_19991002.tbf-8
			spellingFixes["Intitute"] = "Institute";
			// 22863_20001129.tbf-16
			// Hack: these aren't exactly spelling mistakes, but we need to
			// run a search-and-replace across the entire corpus with them, so
			// they should be treated just like spelling mistakes for our
			// purposes
			spellingFixes["("] = LeftParenthesis;
			spellingFixes[")"] = RightParenthesis;
		}

		private const long serialVersionUID = 7810182997777764277L;

		/// <summary>
		/// A filter which rejects preterminal nodes that contain "empty" leaf
		/// nodes.
		/// </summary>
		private static readonly IPredicate<Tree> emptyFilter = null;

		private sealed class _ITreeTransformer_78 : ITreeTransformer
		{
			public _ITreeTransformer_78()
			{
			}

			public Tree TransformTree(Tree t)
			{
				if (t.IsLeaf())
				{
					return t;
				}
				string value = t.Value();
				if (value == null)
				{
					return t;
				}
				if (value.Equals("sa"))
				{
					t.SetValue("s.a");
				}
				return t;
			}
		}

		/// <summary>
		/// Resolves some inconsistencies in constituent naming:
		/// - "sa" and "s.a" are equivalent -- merge to "s.a"
		/// </summary>
		private static readonly ITreeTransformer constituentRenamer = new _ITreeTransformer_78();

		private static readonly Pair<string, string>[] cleanupStrs = new Pair[] { new Pair("sp < (sp=sp <: prep=prep)", "replace sp prep"), new Pair("fpa > __=grandparent $++ (__=ancestor <<` fpt=fpt >` =grandparent)", "move fpt $- ancestor"), new Pair
			("/^s\\.a$/ <: (/^grup\\.nom$/=gn <: /^a/)", "relabel gn /grup.a/"), new Pair("sadv !< /^grup\\.adv$/ <: /^(rg|neg)$/=adv", "adjoinF (grup.adv foot@) adv"), new Pair("z=z <: (__ !< __)", "relabel z z0"), new Pair("/^grup\\.c/=grup > conj <: sp=sp"
			, "replace grup sp"), new Pair("__=N <<` (fp|fs=fp <: (/^\\.$/ !. __)) > sentence=sentence", "move fp $- N"), new Pair("(pi000000 <: __ !$+ S >` (/^grup\\.nom/=gn >` sn=sn))" + ". ((que >: (__=queTag $- =sn)) . (__=vb !< __ >>: (__=vbContainer $- =queTag)))"
			, "[insert (S (relatiu (pr000000 que)) (infinitiu vmn0000=vbFoot)) >-1 gn]" + "[move vb >0 vbFoot]" + "[delete queTag]" + "[delete vbContainer]"), new Pair("sn=sn <: (/^grup\\.nom/=gn <<: Nada)" + "$+ (infinitiu=inf <<, que=que <<` (ver , =que) $+ sp=sp)"
			, "[delete inf] [insert (S (relatiu (pr000000 que)) (infinitiu (vmn0000 ver))) >-1 gn]" + "[move sp >-1 sn]"), new Pair("sentence <<, (sn=sn <, (/^grup\\.w$/ $+ fp))", "delete sn"), new Pair("conj=conj <: fp=fp", "replace conj fp"), new Pair
			("fit=fit <: ¿", "relabel fit fia") };

		private static readonly IList<Pair<TregexPattern, TsurgeonPattern>> cleanup = CompilePatterns(cleanupStrs);

		/// <summary>
		/// If one of the constituents in this set has a single child has a
		/// multi-word token, it should be replaced by a node heading the
		/// expanded word leaves rather than simply receive that node as a
		/// child.
		/// </summary>
		/// <remarks>
		/// If one of the constituents in this set has a single child has a
		/// multi-word token, it should be replaced by a node heading the
		/// expanded word leaves rather than simply receive that node as a
		/// child.
		/// Note that this is only the case for constituents with a *single
		/// child which is a multi-word token.
		/// </remarks>
		private static readonly ICollection<string> mergeWithConstituentWhenPossible = new HashSet<string>(Arrays.AsList("grup.adv", "grup.nom", "grup.nom.loc", "grup.nom.org", "grup.nom.otros", "grup.nom.pers", "grup.verb", "spec"));

		private bool simplifiedTagset;

		private bool aggressiveNormalization;

		private bool retainNER;

		public SpanishTreeNormalizer()
			: this(true, false, false)
		{
		}

		public SpanishTreeNormalizer(bool simplifiedTagset, bool aggressiveNormalization, bool retainNER)
			: base(new SpanishTreebankLanguagePack())
		{
			// Left and right parentheses should be at same depth
			// Nominal groups where adjectival groups belong
			// Adverbial phrases should always have adverb group children
			// -- we see about 50 exceptions in the corpus..
			// 'z' tag should be 'z0'
			// Conjunction groups aren't necessary if they head single
			// prepositional phrases (we already see a `conj < sp` pattern;
			// replicate that
			// "Lift up" sentence-final periods which have been nested within
			// constituents (convention in AnCora is to have sentence-final
			// periods as final right children of the `sentence` constituent)
			// AnCora has a few weird parses of "nada que ver" and related
			// phrases. Normalize them:
			//
			//     (grup.nom (pi000000 X) (S (relatiu (pr000000 que))
			//                               (infinitiu (vmn0000 Y))))
			// One more bizarre "nada que ver"
			// Remove date lead-ins
			// Shed "conj" parents of periods in the middle of trees so that
			// our splitter can identify sentence boundaries properly
			// Fix mis-tagging of inverted question mark
			// Customization
			if (retainNER && !simplifiedTagset)
			{
				throw new ArgumentException("retainNER argument only valid when " + "simplified tagset is used");
			}
			this.simplifiedTagset = simplifiedTagset;
			this.aggressiveNormalization = aggressiveNormalization;
			this.retainNER = retainNER;
		}

		public override Tree NormalizeWholeTree(Tree tree, ITreeFactory tf)
		{
			// Begin with some basic transformations
			tree = tree.Prune(emptyFilter).SpliceOut(aOverAFilter).Transform(constituentRenamer);
			// Now start some simple cleanup
			tree = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(cleanup, tree);
			// That might've produced some more A-over-As
			tree = tree.SpliceOut(aOverAFilter);
			// Find all named entities which are not multi-word tokens and nest
			// them within named entity NP groups
			if (retainNER)
			{
				MarkSimpleNamedEntities(tree);
			}
			foreach (Tree t in tree)
			{
				if (simplifiedTagset && t.IsPreTerminal())
				{
					// This is a part of speech tag. Remove extra morphological
					// information.
					CoreLabel label = (CoreLabel)t.Label();
					string pos = label.Value();
					pos = string.Intern(SimplifyPOSTag(pos));
					label.SetValue(pos);
					label.SetTag(pos);
				}
				else
				{
					if (aggressiveNormalization && IsMultiWordCandidate(t))
					{
						// Expand multi-word token if necessary
						NormalizeForMultiWord(t, tf);
					}
				}
			}
			// More tregex-powered fixes
			tree = ExpandElisions(tree);
			tree = ExpandConmigo(tree);
			tree = ExpandCliticPronouns(tree);
			// Make sure the tree has a top-level unary rewrite; the root
			// should have a proper root label
			string rootLabel = tlp.StartSymbol();
			if (!tree.Value().Equals(rootLabel))
			{
				tree = tf.NewTreeNode(rootLabel, Java.Util.Collections.SingletonList(tree));
			}
			return tree;
		}

		public override string NormalizeTerminal(string word)
		{
			if (spellingFixes.Contains(word))
			{
				return spellingFixes[word];
			}
			return word;
		}

		/// <summary>
		/// Return a "simplified" version of an original AnCora part-of-speech
		/// tag, with much morphological annotation information removed.
		/// </summary>
		private string SimplifyPOSTag(string pos)
		{
			if (pos.Length == 0)
			{
				return pos;
			}
			char type;
			switch (pos[0])
			{
				case 'd':
				{
					// determinant (d)
					//   retain category, type
					//   drop person, gender, number, possessor
					return Sharpen.Runtime.Substring(pos, 0, 2) + "0000";
				}

				case 's':
				{
					// preposition (s)
					//   retain category, type
					//     ignore rare exceptions in LDC
					//   drop form, gender, number
					return pos[0] + "p000";
				}

				case 'p':
				{
					// pronoun (p)
					//   retain category, type
					//   drop person, gender, number, case, possessor, politeness
					type = pos[1];
					return string.Format("p%s000000", type);
				}

				case 'a':
				{
					// adjective
					//   retain category, type, grade
					//   drop gender, number, function
					type = pos[1] == 'o' ? 'o' : 'q';
					return string.Format("a%s%s000", type, pos[2]);
				}

				case 'n':
				{
					// noun
					//   retain category, type, number, NER label
					//   drop type, gender, classification
					char number = pos[3];
					if (number == 'c')
					{
						// LDC inconsistency.
						return "w";
					}
					else
					{
						if (number == 'a')
						{
							// Only appears once in LDC?
							number = 's';
						}
					}
					char ner = retainNER && pos.Length == 7 ? pos[6] : '0';
					return Sharpen.Runtime.Substring(pos, 0, 2) + '0' + number + "00" + ner;
				}

				case 'v':
				{
					// verb
					//   retain category, type, mood, tense
					//   drop person, number, gender
					return Sharpen.Runtime.Substring(pos, 0, 4) + "000";
				}

				case 'i':
				{
					// interjection
					//   drop LDC extras
					return "i";
				}

				default:
				{
					// adverb
					//   retain all
					// punctuation
					//   retain all
					// numerals
					//   retain all
					// date and time
					//   retain all
					// conjunction
					//   retain all
					return pos;
				}
			}
		}

		/// <summary>
		/// Matches a verb with attached pronouns; used in several following
		/// Tregex expressions
		/// </summary>
		private const string VerbLeafWithPronounsTregex = "/(?:(?:[aeiáéí]r|[áé]ndo|[aeáé]n?|[aeiáéí](?:d(?!os)|(?=os)))" + "|^(?:d[ií]|h[aá]z|v[eé]|p[oó]n|s[aá]l|sé|t[eé]n|v[eé]n|(?:id(?=os$))))" + "(?:(?:(?:[mts]e|n?os|les?)(?:l[oa]s?)?)|l[oa]s?)$/=vb "
			 + "> (/^vm[gmn]0000$/";

		/// <summary>
		/// Matches verbs (infinitives, gerunds and imperatives) which have
		/// attached pronouns, and the clauses which contain them
		/// </summary>
		private static readonly TregexPattern verbWithCliticPronouns = TregexPattern.Compile(VerbLeafWithPronounsTregex + " !$ __)" + ">+(/^[^S]/) (/^(infinitiu|gerundi|grup\\.verb)$/=target " + "> /^(sentence|S|grup\\.verb|infinitiu|gerundi)$/=clause << =vb "
			 + "!<< (/^(infinitiu|gerundi|grup\\.verb)$/ << =vb))");

		/// <summary>
		/// Matches verbs (infinitives, gerunds and imperatives) which have
		/// attached pronouns and siblings within their containing verb
		/// phrases
		/// </summary>
		private static readonly TregexPattern verbWithCliticPronounsAndSiblings = TregexPattern.Compile(VerbLeafWithPronounsTregex + "=target $ __) " + ">+(/^[^S]/) (/^(infinitiu|gerundi|grup\\.verb)$/ " + "> /^(sentence|S|grup\\.verb|infinitiu|gerundi)$/=clause << =vb "
			 + "!<< (/^(infinitiu|gerundi|grup\\.verb)$/ << =vb))");

		/// <summary>
		/// Matches verbs which really should be in a clause, but were
		/// squeezed into an infinitive constituent (because the pronoun was
		/// attached to the verb, we could just pretend it wasn't a clause..
		/// </summary>
		/// <remarks>
		/// Matches verbs which really should be in a clause, but were
		/// squeezed into an infinitive constituent (because the pronoun was
		/// attached to the verb, we could just pretend it wasn't a clause..
		/// not anymore!)
		/// </remarks>
		private static readonly TregexPattern clauselessVerbWithCliticPronouns = TregexPattern.Compile(VerbLeafWithPronounsTregex + ") > (/^vmn/ > (/^infinitiu$/=target > /^sp$/))");

		private static readonly TsurgeonPattern clausifyVerbWithCliticPronouns = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation("adjoinF (S foot@) target");

		private static readonly SpanishVerbStripper verbStripper = SpanishVerbStripper.GetInstance();

		// Match a leaf that looks like it has a clitic pronoun
		// Match suffixes of regular forms which may carry attached
		// pronouns (imperative, gerund, infinitive)
		// Match irregular imperative stems
		// Match attached pronouns
		// It should actually be a verb (gerund, imperative or
		// infinitive)
		//
		// (Careful: other code that uses this pattern requires that this
		// node be at the end, with parens so that it can be named /
		// modified. See e.g. #verbWithCliticPronounAndSiblings)
		// Verb tag should not have siblings in verb
		// phrase
		// Locate the clause which contains it, and
		// the child just below that clause
		// Make sure we're not up too far in the tree:
		// there should be no infinitive / gerund /
		// verb phrase between the located ancestor
		// and the verb
		// Name the matched verb tag as the target for insertion;
		// require that it have siblings
		// Locate the clause which contains it, and
		// the child just below that clause
		// Make sure we're not up too far in the tree:
		// there should be no infinitive / gerund /
		// verb phrase between the located ancestor
		// and the verb
		/// <summary>Separate clitic pronouns into their own tokens in the given tree.</summary>
		/// <remarks>
		/// Separate clitic pronouns into their own tokens in the given tree.
		/// (The clitic pronouns are attached under new `grup.nom` constituents
		/// which follow the verbs to which they were formerly attached.)
		/// </remarks>
		private static Tree ExpandCliticPronouns(Tree t)
		{
			// Perform some cleanup first -- we want to match as many
			// clitic-attached verbs as possible..
			t = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPattern(clauselessVerbWithCliticPronouns, clausifyVerbWithCliticPronouns, t);
			// Run two separate stages: one for only-child VPs, then another
			// for VP children which have siblings
			t = ExpandCliticPronounsInner(t, verbWithCliticPronouns);
			t = ExpandCliticPronounsInner(t, verbWithCliticPronounsAndSiblings);
			return t;
		}

		/// <summary>Expand clitic pronouns on verbs matching the given pattern.</summary>
		private static Tree ExpandCliticPronounsInner(Tree t, TregexPattern pattern)
		{
			TregexMatcher matcher = pattern.Matcher(t);
			while (matcher.Find())
			{
				Tree verbNode = matcher.GetNode("vb");
				string verb = verbNode.Value();
				if (!SpanishVerbStripper.IsStrippable(verb))
				{
					continue;
				}
				SpanishVerbStripper.StrippedVerb split = verbStripper.SeparatePronouns(verb);
				if (split == null)
				{
					continue;
				}
				// Retrieve some context for the pronoun disambiguator: take the
				// matched clause and walk (at most) two constituents up
				StringBuilder clauseYieldBuilder = new StringBuilder();
				foreach (ILabel label in matcher.GetNode("clause").Yield())
				{
					clauseYieldBuilder.Append(label.Value()).Append(" ");
				}
				string clauseYield = clauseYieldBuilder.ToString();
				clauseYield = Sharpen.Runtime.Substring(clauseYield, 0, clauseYield.Length - 1);
				// Insert clitic pronouns as leaves of pronominal phrases which are
				// siblings of `target`. Iterate in reverse order since pronouns are
				// attached to immediate right of `target`
				IList<string> pronouns = split.GetPronouns();
				for (int i = pronouns.Count - 1; i >= 0; i--)
				{
					string pronoun = pronouns[i];
					string newTreeStr = null;
					if (AnCoraPronounDisambiguator.IsAmbiguous(pronoun))
					{
						AnCoraPronounDisambiguator.PersonalPronounType type = AnCoraPronounDisambiguator.DisambiguatePersonalPronoun(split, i, clauseYield);
						switch (type)
						{
							case AnCoraPronounDisambiguator.PersonalPronounType.Object:
							{
								newTreeStr = "(sn (grup.nom (pp000000 %s)))";
								break;
							}

							case AnCoraPronounDisambiguator.PersonalPronounType.Reflexive:
							{
								newTreeStr = "(morfema.pronominal (pp000000 %s))";
								break;
							}

							case AnCoraPronounDisambiguator.PersonalPronounType.Unknown:
							{
								// Mark for manual disambiguation
								newTreeStr = "(PRONOUN? (pp000000 %s))";
								break;
							}
						}
					}
					else
					{
						// Unambiguous clitic pronouns are all indirect / direct
						// object pronouns.. convenient!
						newTreeStr = "(sn (grup.nom (pp000000 %s)))";
					}
					string patternString = "[insert " + string.Format(newTreeStr, pronoun) + " $- target]";
					TsurgeonPattern insertPattern = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(patternString);
					t = insertPattern.Matcher().Evaluate(t, matcher);
				}
				TsurgeonPattern relabelOperation = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(string.Format("[relabel vb /%s/]", split.GetStem()));
				t = relabelOperation.Matcher().Evaluate(t, matcher);
			}
			return t;
		}

		private static readonly IList<Pair<TregexPattern, TsurgeonPattern>> markSimpleNEs;

		static SpanishTreeNormalizer()
		{
			// Generate some reusable patterns for four different NE groups
			Pair<string, string>[] patternTemplates = new Pair[] { new Pair("/^grup\\.nom$/=target <: (/np0000%c/ < __)", "[relabel target /grup.nom.%s/]"), new Pair("/^grup\\.nom$/ < ((/np0000%c/=target < __) $+ __)", "[adjoinF (grup.nom.%s foot@) target]"
				), new Pair("/^grup\\.nom$/ < ((/np0000%c/=target < __) $- __)", "[adjoinF (grup.nom.%s foot@) target]") };
			// NE as only child of a `grup.nom`
			// NE as child with a right sibling in a `grup.nom`
			// NE as child with a left sibling in a `grup.nom`
			// Pairs tagset annotation codes with the annotations used in our constituents
			Pair<char, string>[] namedEntityTypes = new Pair[] { new Pair('0', "otros"), new Pair('l', "lug"), new Pair('o', "org"), new Pair('p', "pers") };
			// other
			// location
			// organization
			// person
			markSimpleNEs = new List<Pair<TregexPattern, TsurgeonPattern>>(patternTemplates.Length * namedEntityTypes.Length);
			foreach (Pair<string, string> template in patternTemplates)
			{
				foreach (Pair<char, string> namedEntityType in namedEntityTypes)
				{
					string tregex = string.Format(template.First(), namedEntityType.First());
					string tsurgeon = string.Format(template.Second(), namedEntityType.Second());
					markSimpleNEs.Add(new Pair<TregexPattern, TsurgeonPattern>(TregexPattern.Compile(tregex), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(tsurgeon)));
				}
			}
		}

		/// <summary>
		/// Find all named entities which are not multi-word tokens and nest
		/// them in named entity NP groups (`grup.nom.{lug,org,pers,otros}`).
		/// </summary>
		/// <remarks>
		/// Find all named entities which are not multi-word tokens and nest
		/// them in named entity NP groups (`grup.nom.{lug,org,pers,otros}`).
		/// Do this only for "simple" NEs: the multi-word NEs have to be done
		/// at a later step in `MultiWordPreprocessor`.
		/// </remarks>
		private static void MarkSimpleNamedEntities(Tree t)
		{
			Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(markSimpleNEs, t);
		}

		/// <summary>
		/// Determine whether the given tree node is a multi-word token
		/// expansion candidate.
		/// </summary>
		/// <remarks>
		/// Determine whether the given tree node is a multi-word token
		/// expansion candidate. (True if the node has at least one grandchild
		/// which is a leaf node.)
		/// </remarks>
		private static bool IsMultiWordCandidate(Tree t)
		{
			foreach (Tree child in t.Children())
			{
				foreach (Tree grandchild in child.Children())
				{
					if (grandchild.IsLeaf())
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Normalize a pre-pre-terminal tree node by accounting for multi-word
		/// tokens.
		/// </summary>
		/// <remarks>
		/// Normalize a pre-pre-terminal tree node by accounting for multi-word
		/// tokens.
		/// Detects multi-word tokens in leaves below this pre-pre-terminal and
		/// expands their constituent words into separate leaves.
		/// </remarks>
		internal virtual void NormalizeForMultiWord(Tree t, ITreeFactory tf)
		{
			Tree[] preterminals = t.Children();
			for (int i = 0; i < preterminals.Length; i++)
			{
				// This particular child is not actually a preterminal --- skip
				if (!preterminals[i].IsPreTerminal())
				{
					continue;
				}
				Tree leaf = preterminals[i].FirstChild();
				string leafValue = ((CoreLabel)leaf.Label()).Value();
				string[] words = GetMultiWords(leafValue);
				if (words.Length == 1)
				{
					continue;
				}
				// Leaf is a multi-word token; build new nodes for each of its
				// constituent words
				IList<Tree> newNodes = new List<Tree>(words.Length);
				foreach (string word1 in words)
				{
					string word = NormalizeTerminal(word1);
					Tree newLeaf = tf.NewLeaf(word);
					if (newLeaf.Label() is IHasWord)
					{
						((IHasWord)newLeaf.Label()).SetWord(word);
					}
					Tree newNode = tf.NewTreeNode(MwTag, Arrays.AsList(newLeaf));
					if (newNode.Label() is IHasTag)
					{
						((IHasTag)newNode.Label()).SetTag(MwTag);
					}
					newNodes.Add(newNode);
				}
				// Value of the phrase which should head these preterminals. Mark
				// that this was created from a multiword token, and also retain
				// the original parts of speech.
				string phraseValue = MwPhraseTag + "_" + SimplifyPOSTag(preterminals[i].Value());
				// Should we insert these new nodes as children of the parent `t`
				// (i.e., "merge" the multi-word token phrase into its parent), or
				// head them with a new node and set that as a child of the
				// parent?
				bool shouldMerge = preterminals.Length == 1 && mergeWithConstituentWhenPossible.Contains(t.Value());
				if (shouldMerge)
				{
					t.SetChildren(newNodes);
					t.SetValue(phraseValue);
				}
				else
				{
					Tree newHead = tf.NewTreeNode(phraseValue, newNodes);
					t.SetChild(i, newHead);
				}
			}
		}

		private static readonly Pattern pQuoted = Pattern.Compile("\"(.+)\"");

		/// <summary>Strings of punctuation which should remain a single token.</summary>
		private static readonly Pattern pPunct = Pattern.Compile("[.,!?:/'=()-]+");

		/// <summary>Characters which may separate words in a single token.</summary>
		private const string WordSeparators = ",-_¡!¿?()/%";

		/// <summary>
		/// Word separators which should not be treated as separate "words" and
		/// dropped from a multi-word token.
		/// </summary>
		private const string WordSeparatorsDrop = "_";

		/// <summary>
		/// These bound morphemes should not be separated from the words with
		/// which they are joined by hyphen.
		/// </summary>
		private static readonly ICollection<string> hyphenBoundMorphemes = new HashSet<string>(Arrays.AsList("anti", "co", "ex", "meso", "neo", "pre", "pro", "quasi", "re", "semi", "sub"));

		// TODO how to handle clitics? chino-japonés
		// anti-Gil
		// co-promotora
		// ex-diputado
		// meso-americano
		// neo-proteccionismo
		// pre-presidencia
		// pro-indonesias
		// quasi-unidimensional
		// re-flotamiento
		// semi-negro
		// sub-18
		/// <summary>Prepare the given token for multi-word detection / extraction.</summary>
		/// <remarks>
		/// Prepare the given token for multi-word detection / extraction.
		/// This method makes up for some various oddities in corpus annotations.
		/// </remarks>
		private static string PrepareForMultiWordExtraction(string token)
		{
			return token.ReplaceAll("-fpa-", "(").ReplaceAll("-fpt-", ")");
		}

		/// <summary>
		/// Return the (single or multiple) words which make up the given
		/// token.
		/// </summary>
		/// <remarks>
		/// Return the (single or multiple) words which make up the given
		/// token.
		/// TODO can't SpanishTokenizer handle most of this?
		/// </remarks>
		private static string[] GetMultiWords(string token)
		{
			token = PrepareForMultiWordExtraction(token);
			Matcher punctMatcher = pPunct.Matcher(token);
			if (punctMatcher.Matches())
			{
				return new string[] { token };
			}
			Matcher quoteMatcher = pQuoted.Matcher(token);
			if (quoteMatcher.Matches())
			{
				string[] ret = new string[3];
				ret[0] = "\"";
				ret[1] = quoteMatcher.Group(1);
				ret[2] = "\"";
				return ret;
			}
			// Confusing: we are using a tokenizer to split a token into its
			// constituent words
			StringTokenizer splitter = new StringTokenizer(token, WordSeparators, true);
			int remainingTokens = splitter.CountTokens();
			IList<string> words = new List<string>();
			while (splitter.HasMoreTokens())
			{
				string word = splitter.NextToken();
				remainingTokens--;
				if (ShouldDropWord(word))
				{
					// This is a delimiter that we should drop
					continue;
				}
				if (remainingTokens >= 2 && hyphenBoundMorphemes.Contains(word))
				{
					string hyphen = splitter.NextToken();
					remainingTokens--;
					if (!hyphen.Equals("-"))
					{
						// Ouch. We expected a hyphen here. Clean things up and keep
						// moving.
						words.Add(word);
						if (!ShouldDropWord(hyphen))
						{
							words.Add(hyphen);
						}
						continue;
					}
					string freeMorpheme = splitter.NextToken();
					remainingTokens--;
					words.Add(word + hyphen + freeMorpheme);
					continue;
				}
				else
				{
					if (word.Equals(",") && remainingTokens >= 1 && words.Count > 0)
					{
						int prevIndex = words.Count - 1;
						string prevWord = words[prevIndex];
						if (StringUtils.IsNumeric(prevWord))
						{
							string nextWord = splitter.NextToken();
							remainingTokens--;
							if (StringUtils.IsNumeric(nextWord))
							{
								words.Set(prevIndex, prevWord + ',' + nextWord);
							}
							else
							{
								// Expected a number here.. clean up and move on
								words.Add(word);
								words.Add(nextWord);
							}
							continue;
						}
					}
				}
				// Otherwise..
				words.Add(word);
			}
			return Sharpen.Collections.ToArray(words, new string[words.Count]);
		}

		/// <summary>
		/// Determine if the given "word" which is part of a multiword token
		/// should be dropped.
		/// </summary>
		private static bool ShouldDropWord(string word)
		{
			return word.Length == 1 && WordSeparatorsDrop.IndexOf(word[0]) != -1;
		}

		/// <summary>
		/// Expand grandchild tokens which are elided forms of multi-word
		/// expressions ('al,' 'del').
		/// </summary>
		/// <remarks>
		/// Expand grandchild tokens which are elided forms of multi-word
		/// expressions ('al,' 'del').
		/// We perform this expansion separately from multi-word expansion
		/// because we follow special rules about where the expanded tokens
		/// should be placed in the case of elision.
		/// </remarks>
		/// <param name="t">Tree representing an entire sentence</param>
		private static Tree ExpandElisions(Tree t)
		{
			return Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ProcessPatternsOnTree(elisionExpansions, t);
		}

		private static readonly Pair<string, string>[] elisionExpansionStrs = new Pair[] { new Pair("/^(prep|sadv|conj)$/ <+(/^(prep|grup\\.(adv|cc|prep))$/) (sp000=sp < /(?i)^(del|al)$/=elided) <<` =sp " + "$+ (sn > (__ <+(sn) (sn=sn !< sn) << =sn) !$- sn)"
			, "[relabel elided /(?i)l//] [insert (spec (da0000 el)) >0 sn]"), new Pair("prep < (sp000 < /(?i)^(del|al)$/=elided) $+ /grup\\.nom/=target", "[relabel elided /(?i)l//] " + "[adjoinF (sn (spec (da0000 el)) foot@) target]"), new Pair("prep < (sp000 < /(?i)^(del|al)$/=elided) $+ /s\\.a/=target"
			, "[relabel elided /(?i)l//] " + "[adjoinF (sn (spec (da0000 el)) (grup.nom foot@)) target]"), new Pair("sp < (prep=prep < (sp000 < /(?i)^(a|de)l$/=elided) $+ " + "(S=S <<, relatiu))", "[relabel elided /(?i)l//] " + "[adjoinF (sn (spec (da0000 el)) (grup.nom foot@)) S]"
			), new Pair("prep < (sp000 < /(?i)^(al|del)$/=elided) $+ " + "(S=target <+(S) infinitiu=inf <<, =inf)", "[relabel elided /(?i)l//] " + "[adjoinF (sn (spec (da0000 el)) foot@) target]"), new Pair("prep < (sp000 < /(?i)^al$/=elided) $+ (S=target <, neg <2 infinitiu)"
			, "[relabel elided a] " + "[adjoinF (sn (spec (da0000 el)) foot@) target]"), new Pair("prep < (sp000 < /(?i)^al$/=elided) $+ relatiu=target", "[relabel elided a] " + "[adjoinF (sn (spec (da0000 el)) foot@) target]"), new Pair("prep < (sp000 < /(?i)^al$/=elided) $+ (sp=target <, prep)"
			, "[relabel elided a] " + "[adjoinF (sn (spec (da0000 el)) (grup.nom foot@)) target]"), new Pair("prep < (sp000 < /(?i)^(del|al)$/=elided) $+ " + "(/grup\\.nom/=target <, /s\\.a/ <2 /sn|nc0[sp]000/)", "[relabel elided /(?i)l//] " + "[adjoinF (sn (spec (da0000 el)) foot@) target]"
			), new Pair("prep < (sp000 < /(?i)^(al|del)$/=elided) $+ (S=target < participi)", "[relabel elided /(?i)l//] " + "[adjoinF (sn (spec (da0000 lo)) foot@) target]"), new Pair("spec < (sp000=target < /(?i)^del$/=elided) > sn $+ /grup\\.nom/", 
			"[relabel elided /(?i)l//] " + "[insert (da0000 el) $- target]"), new Pair("sp000=kill < /(?i)^(del|al)$/ $+ w=target", "[delete kill] " + "[adjoinF (sp (prep (sp000 de)) (sn (spec (da0000 el)) foot@)) target]"), new Pair("sp000 < /(?i)^(a|de)l$/=contraction >: (prep >` (/^grup\\.prep$/ "
			 + ">` (prep=prep > sp $+ (sn=sn <, /^grup\\.(nom|[wz])/))))", "[relabel contraction /(?i)l//] [insert (spec (da0000 el)) >0 sn]"), new Pair("sp000 < /(?i)^(a|de)l$/=contraction >: (prep >` (sp >: (conj $+ (sn=sn <, /^grup\\.(nom|[wz])/))))"
			, "[relabel contraction /(?i)l//] [insert (spec (da0000 el)) >0 sn]"), new Pair("sp000 < /(?i)^(a|de)l$/=contraction >: (prep >` (/^grup\\.prep$/ " + ">` (prep=prep > sp $+ (sn <, (sn=sn <, /^grup\\.(nom|[wz])/)))))", "[relabel contraction /(?i)l//] [insert (spec (da0000 el)) >0 sn]"
			), new Pair("sp000 < /(?i)^(a|de)l$/=contraction >: (prep >` (/^grup\\.prep$/ " + ">` (prep > sp $+ (sn=sn <, spec=spec))))", "[relabel contraction /(?i)l//] [insert (da0000 el) >0 spec]"), new Pair("sp000 < /(?i)^(a|de)l$/=contraction >: (prep >` (/^grup\\.prep$/ "
			 + ">` (prep > sp $+ /^grup\\.(nom|[wz])$/=ng)))", "[adjoinF (sn (spec (da0000 el)) foot@) ng] [relabel contraction /(?i)l//]"), new Pair("sp000 < /(?i)^(de|a)l$/=elided >` (/^grup\\.cc$/ >: (conj $+ /^grup\\.nom/=gn))", "[relabel elided /(?i)l//] [adjoinF (sn (spec (da0000 el)) foot@) gn]"
			), new Pair("sp000=sp < /(?i)^al$/=elided $+ /^vmp/", "[relabel elided /(?i)l//] [insert (da0000 el) $- sp]"), new Pair("prep < (sp000 < /(?i)^(al|del)$/=elided) $+ (S=S <+(S) (/^f/=punct $+ (S <+(S) (S <, infinitiu))))", "[relabel elided /(?i)l//] [adjoinF (sn (spec (da0000 el)) (grup.nom foot@)) S]"
			), new Pair("__=sp < del=contraction >, __=parent $+ (__ < todo >` =parent)", "[relabel contraction de] [insert (da0000 el) $- sp]") };

		private static readonly IList<Pair<TregexPattern, TsurgeonPattern>> elisionExpansions = CompilePatterns(elisionExpansionStrs);

		private static TregexPattern conmigoPattern = TregexPattern.Compile("/(?i)^con[mst]igo$/=conmigo > (/^pp/ > (/^grup\\.nom$/ > sn=sn))");

		// Elided forms with a `prep` ancestor which has an `sn` phrase as a
		// right sibling
		// Search for `sn` which is right sibling of closest `prep`
		// ancestor to the elided node; cascade down tree to lowest `sn`
		// Insert the 'el' specifier as a constituent in adjacent
		// noun phrase
		// Prepositional forms with a `prep` grandparent which has a
		// `grup.nom` phrase as a right sibling
		// Elided forms with a `prep` ancestor which has an adjectival
		// phrase as a right sibling ('al segundo', etc.)
		// Turn neighboring adjectival phrase into a noun phrase,
		// adjoining original adj phrase beneath a `grup.nom`
		// "del que golpea:" insert 'el' as specifier into adjacent relative
		// phrase
		// Build a noun phrase in the neighboring relative clause
		// containing the 'el' specifier
		// "al" + infinitive phrase
		// Looking for an infinitive directly to the right of the
		// "al" token, nested within one or more clause
		// constituents
		// "al no" + infinitive phrase
		// "al que quisimos tanto"
		// "al de" etc.
		// leading adjective in sibling: "al chileno Fernando"
		// "al" + phrase begun by participle -> "a lo <participle>"
		// e.g. "al conseguido" -> "a lo conseguido"
		// "del" used within specifier; e.g. "más del 30 por ciento"
		// "del," "al" in date phrases: "1 de enero del 2001"
		// "a favor del X," "en torno al Y": very common (and somewhat
		// complex) phrase structure that we can match
		// "en vez del X": same as above, except prepositional phrase
		// functions as conjunction (and is labeled as such)
		// "a favor del X," "en torno al Y" where X, Y are doubly nested
		// substantives
		// "a favor del X," "en torno al Y" where X, Y already have
		// leading specifiers
		// "a favor del X," "en torno al Y" where X, Y are nominal
		// groups (not substantives)
		// "al," "del" as part of coordinating conjunction: "frente al,"
		// "además del"
		//
		// (nearby noun phrase labeled as nominal group)
		// "al" + participle in adverbial phrase: "al contado," "al descubierto"
		// über-special case: 15021_20000218.tbf-5
		//
		// intentional: article should bind all of quoted phrase, even
		// though there are multiple clauses (kind of a crazy sentence)
		// special case: "del todo" -> "de el todo" (flat)
		/// <summary>¡Venga, expand conmigo!</summary>
		private static Tree ExpandConmigo(Tree t)
		{
			TregexMatcher matcher = conmigoPattern.Matcher(t);
			while (matcher.Find())
			{
				Tree conmigoNode = matcher.GetNode("conmigo");
				string word = conmigoNode.Value();
				string newPronoun = null;
				if (Sharpen.Runtime.EqualsIgnoreCase(word, "conmigo"))
				{
					newPronoun = "mí";
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(word, "contigo"))
					{
						newPronoun = "ti";
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(word, "consigo"))
						{
							newPronoun = "sí";
						}
					}
				}
				if (word[0] == 'C')
				{
					newPronoun = newPronoun.ToUpper();
				}
				string tsurgeon = string.Format("[relabel conmigo /%s/]" + "[adjoinF (sp (prep (sp000 con)) foot@) sn]", newPronoun);
				TsurgeonPattern pattern = Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(tsurgeon);
				t = pattern.Matcher().Evaluate(t, matcher);
			}
			return t;
		}

		private static IList<Pair<TregexPattern, TsurgeonPattern>> CompilePatterns(Pair<string, string>[] patterns)
		{
			IList<Pair<TregexPattern, TsurgeonPattern>> ret = new List<Pair<TregexPattern, TsurgeonPattern>>(patterns.Length);
			foreach (Pair<string, string> pattern in patterns)
			{
				ret.Add(new Pair<TregexPattern, TsurgeonPattern>(TregexPattern.Compile(pattern.First()), Edu.Stanford.Nlp.Trees.Tregex.Tsurgeon.Tsurgeon.ParseOperation(pattern.Second())));
			}
			return ret;
		}
	}
}

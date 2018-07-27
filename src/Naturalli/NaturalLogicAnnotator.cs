using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>An annotator marking operators with their scope.</summary>
	/// <remarks>
	/// An annotator marking operators with their scope.
	/// Look at
	/// <see cref="Patterns"/>
	/// for the full list of patterns, otherwise
	/// <see cref="DoOneSentence(Edu.Stanford.Nlp.Pipeline.Annotation, Edu.Stanford.Nlp.Util.ICoreMap)"/>
	/// is the main interface for this class.
	/// TODO(gabor) annotate generics as "most"
	/// </remarks>
	/// <author>Gabor Angeli</author>
	public class NaturalLogicAnnotator : SentenceAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator));

		/// <summary>A regex for arcs that act as determiners.</summary>
		private const string Det = "/det.*|a(dv)?mod|neg|nummod|compound|case/";

		/// <summary>A regex for arcs that we pretend are subject arcs.</summary>
		private const string GenSubj = "/[ni]subj(pass)?/";

		/// <summary>A regex for arcs that we pretend are object arcs.</summary>
		private const string GenObj = "/[di]obj|xcomp|advcl/";

		/// <summary>A regex for arcs that we pretend are copula.</summary>
		private const string GenCop = "/cop|aux(pass)?/";

		/// <summary>A regex for arcs which denote a sub-clause (e.g., "at Stanford" or "who are at Stanford")</summary>
		private const string GenClause = "/nmod|acl:relcl/";

		/// <summary>A regex for arcs which denote a preposition</summary>
		private const string GenPrep = "/nmod(:.{1,10})?|advcl|ccomp|advmod/";

		/// <summary>A Semgrex fragment for matching a quantifier.</summary>
		private static readonly string Quantifier;

		static NaturalLogicAnnotator()
		{
			ICollection<string> singleWordQuantifiers = new HashSet<string>();
			foreach (Operator q in Operator.Values())
			{
				string[] tokens = q.surfaceForm.Split("\\s+");
				if (!tokens[tokens.Length - 1].StartsWith("_"))
				{
					singleWordQuantifiers.Add("(" + tokens[tokens.Length - 1].ToLower() + ")");
				}
			}
			Quantifier = "[ {lemma:/" + StringUtils.Join(singleWordQuantifiers, "|") + "/}=quantifier | {pos:CD}=quantifier ]";
		}

		private sealed class _List_84 : List<SemgrexPattern>
		{
			public _List_84()
			{
				{
					// { All cats eat mice,
					//   All cats want milk }
					this.Add(SemgrexPattern.Compile("{}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " ({}=subject >>" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Det + " " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier
						 + ") >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenObj + " {}=object"));
					// { All cats are in boxes,
					//   All cats voted for Obama,
					//   All cats have voted for Obama }
					this.Add(SemgrexPattern.Compile("{pos:/V.*/}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " ({}=subject >>" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Det + " " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator
						.Quantifier + ") >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenPrep + " {}=object"));
					// { All cats are cute,
					//   All cats can purr }
					this.Add(SemgrexPattern.Compile("{}=object >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " ({}=subject >>" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Det + " " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier
						 + ") >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenCop + " {}=pivot"));
					// { Everyone at Stanford likes cats,
					//   Everyone who is at Stanford likes cats }
					this.Add(SemgrexPattern.Compile("{}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " ( " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier + " >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenClause
						 + " {}=subject ) >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenObj + " {}=object"));
					// { Everyone at Stanford voted for Colbert }
					this.Add(SemgrexPattern.Compile("{pos:/V.*/}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " ( " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier + " >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.
						GenClause + " {}=subject ) >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenPrep + " {}=object"));
					// { Felix likes cat food }
					this.Add(SemgrexPattern.Compile("{}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {pos:NNP}=Subject >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenObj + " {}=object"));
					// { Felix has spoken to Fido }
					//nmod used to be prep - problem?
					this.Add(SemgrexPattern.Compile("{pos:/V.*/}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {pos:NNP}=Subject >/nmod|ccomp|[di]obj/ {}=object"));
					// { Felix is a cat,
					//   Felix is cute }
					this.Add(SemgrexPattern.Compile("{}=object >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {pos:NNP}=Subject >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenCop + " {}=pivot"));
					// { Some cats do n't like dogs }
					this.Add(SemgrexPattern.Compile("{}=pivot >/neg/ " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier + " >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenObj + " {}=object"));
					// { Obama was not born in Dallas,
					//   Cats are not fluffy,
					//   Tuesday will not work }
					this.Add(SemgrexPattern.Compile("{}=pivot >/neg/ {}=quantifier >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenPrep + " {}=object >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {}=subject "));
					this.Add(SemgrexPattern.Compile("{pos:/J.*/}=object >/neg/ {}=quantifier >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {}=subject "));
					this.Add(SemgrexPattern.Compile("{pos:/V.*/}=object >/neg/ {}=quantifier >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {}=subject >aux {pos:MD}"));
					// { Anytime but next Tuesday,
					//   food but not water,
					//   not on Tuesday  }
					this.Add(SemgrexPattern.Compile("{}=pivot >>/cc/ " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier + " >/conj/ {}=object"));
					this.Add(SemgrexPattern.Compile("{lemma:/not|no|but|except/}=quantifier >/conj|nmod(:.*)?/ {}=object"));
					// as above, but handle a common parse error
					// { Anything except cabbage }
					this.Add(SemgrexPattern.Compile("{}=object >/case/ " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier));
					// { All of the cats hate dogs. }
					//nmod used to be prep - problem?
					this.Add(SemgrexPattern.Compile("{pos:/V.*/}=pivot >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " ( " + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.Quantifier + " >/nmod.*/ {}=subject ) >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator
						.GenObj + " {}=object"));
					//    add(SemgrexPattern.compile("{pos:/V.*/}=pivot > ( "+QUANTIFIER+" >/nmod.*/ {}=subject ) >"+GEN_SUBJ+" {}=object"));  // as above, but handle a common parse error
					// { Either cats or dogs have tails. }
					this.Add(SemgrexPattern.Compile("{pos:/V.*/}=pivot > {lemma:either}=quantifier >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {}=subject >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenObj + " {}=object"));
					// { There are cats }
					this.Add(SemgrexPattern.Compile("{}=quantifier >" + Edu.Stanford.Nlp.Naturalli.NaturalLogicAnnotator.GenSubj + " {}=pivot >>expl {}"));
				}
			}
		}

		/// <summary>The patterns to use for marking quantifier scopes.</summary>
		private static readonly IList<SemgrexPattern> Patterns = Java.Util.Collections.UnmodifiableList(new _List_84());

		/// <summary>
		/// A pattern for just trivial unary quantification, in case a quantifier doesn't match any of the patterns in
		/// <see cref="Patterns"/>
		/// .
		/// </summary>
		private static readonly SemgrexPattern UnaryPattern = SemgrexPattern.Compile("{pos:/N.*/}=subject >" + Det + " " + Quantifier);

		/// <summary>A list of words that suggest their complement has downward polarity.</summary>
		/// <remarks>
		/// A list of words that suggest their complement has downward polarity.
		/// For example, "doubt" ("I doubt that X")
		/// </remarks>
		private static readonly IList<string> DoubtWords = Arrays.AsList("doubt", "skeptical");

		/// <summary>
		/// A pattern for recognizing the words in
		/// <see cref="DoubtWords"/>
		/// .
		/// </summary>
		private static readonly TokenSequencePattern DoubtPattern = TokenSequencePattern.Compile("(?$doubt [{ lemma:/" + StringUtils.Join(DoubtWords, "|") + "/}]) (?$target [{lemma:/that|of/}] []+ )");

		// { Cats eat _some_ mice,
		//   Cats eat _most_ mice }
		/// <summary>
		/// A helper method for
		/// <see cref="GetModifierSubtreeSpan(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord)"/>
		/// and
		/// <see cref="GetSubtreeSpan(Edu.Stanford.Nlp.Semgraph.SemanticGraph, Edu.Stanford.Nlp.Ling.IndexedWord)"/>
		/// .
		/// </summary>
		private static Pair<int, int> GetGeneralizedSubtreeSpan(SemanticGraph tree, IndexedWord root, ICollection<string> validArcs)
		{
			int min = root.Index();
			int max = root.Index();
			IQueue<IndexedWord> fringe = new LinkedList<IndexedWord>();
			foreach (SemanticGraphEdge edge in tree.OutgoingEdgeIterable(root))
			{
				string edgeLabel = edge.GetRelation().GetShortName();
				if ((validArcs == null || validArcs.Contains(edgeLabel)) && !"punct".Equals(edgeLabel))
				{
					fringe.Add(edge.GetDependent());
				}
			}
			while (!fringe.IsEmpty())
			{
				IndexedWord node = fringe.Poll();
				min = Math.Min(node.Index(), min);
				max = Math.Max(node.Index(), max);
				// ignore punctuation
				Sharpen.Collections.AddAll(fringe, tree.GetOutEdgesSorted(node).Stream().Filter(null).Map(null).Collect(Collectors.ToList()));
			}
			return Pair.MakePair(min, max + 1);
		}

		private sealed class _HashSet_180 : HashSet<string>
		{
			public _HashSet_180()
			{
				{
					this.Add("aux");
					this.Add("nmod");
				}
			}
		}

		private static readonly ICollection<string> ModifierArcs = Java.Util.Collections.UnmodifiableSet(new _HashSet_180());

		private sealed class _HashSet_185 : HashSet<string>
		{
			public _HashSet_185()
			{
				{
					this.Add("compound");
				}
			}
		}

		private static readonly ICollection<string> NounComponentArcs = Java.Util.Collections.UnmodifiableSet(new _HashSet_185());

		private sealed class _HashSet_189 : HashSet<string>
		{
			public _HashSet_189()
			{
				{
					this.Add("advmod");
					this.Add("amod");
				}
			}
		}

		private static readonly ICollection<string> JjComponentArcs = Java.Util.Collections.UnmodifiableSet(new _HashSet_189());

		/// <summary>Returns the yield span for the word rooted at the given node, but only traversing a fixed set of relations.</summary>
		/// <param name="tree">The dependency graph to get the span from.</param>
		/// <param name="root">The root word of the span.</param>
		/// <returns>A one indexed span rooted at the given word.</returns>
		private static Pair<int, int> GetModifierSubtreeSpan(SemanticGraph tree, IndexedWord root)
		{
			if (tree.OutgoingEdgeList(root).Stream().AnyMatch(null))
			{
				return GetGeneralizedSubtreeSpan(tree, root, Java.Util.Collections.Singleton("nmod"));
			}
			else
			{
				return GetGeneralizedSubtreeSpan(tree, root, ModifierArcs);
			}
		}

		/// <summary>
		/// Returns the yield span for the word rooted at the given node, but only traversing relations indicative
		/// of staying in the same noun phrase.
		/// </summary>
		/// <param name="tree">The dependency graph to get the span from.</param>
		/// <param name="root">The root word of the span.</param>
		/// <returns>A one indexed span rooted at the given word.</returns>
		private static Pair<int, int> GetProperNounSubtreeSpan(SemanticGraph tree, IndexedWord root)
		{
			return GetGeneralizedSubtreeSpan(tree, root, NounComponentArcs);
		}

		/// <summary>Returns the yield span for the word rooted at the given node.</summary>
		/// <remarks>
		/// Returns the yield span for the word rooted at the given node. So, for example, all cats like dogs rooted at the word
		/// "cats" would yield a span (1, 3) -- "all cats".
		/// </remarks>
		/// <param name="tree">The dependency graph to get the span from.</param>
		/// <param name="root">The root word of the span.</param>
		/// <returns>A one indexed span rooted at the given word.</returns>
		private static Pair<int, int> GetSubtreeSpan(SemanticGraph tree, IndexedWord root)
		{
			return GetGeneralizedSubtreeSpan(tree, root, null);
		}

		/// <summary>Effectively, merge two spans</summary>
		private static Pair<int, int> IncludeInSpan(Pair<int, int> span, Pair<int, int> toInclude)
		{
			return Pair.MakePair(Math.Min(span.first, toInclude.first), Math.Max(span.second, toInclude.second));
		}

		/// <summary>Exclude the second span from the first, if the second is on the edge of the first.</summary>
		/// <remarks>
		/// Exclude the second span from the first, if the second is on the edge of the first. If the second is in the middle, it's
		/// unclear what this function should do, so it just returns the original span.
		/// </remarks>
		private static Pair<int, int> ExcludeFromSpan(Pair<int, int> span, Pair<int, int> toExclude)
		{
			if (toExclude.second <= span.first || toExclude.first >= span.second)
			{
				// Case: toExclude is outside of the span anyways
				return span;
			}
			else
			{
				if (toExclude.first <= span.first && toExclude.second > span.first)
				{
					// Case: overlap on the front
					return Pair.MakePair(toExclude.second, span.second);
				}
				else
				{
					if (toExclude.first < span.second && toExclude.second >= span.second)
					{
						// Case: overlap on the front
						return Pair.MakePair(span.first, toExclude.first);
					}
					else
					{
						if (toExclude.first > span.first && toExclude.second < span.second)
						{
							// Case: toExclude is within the span
							return span;
						}
						else
						{
							throw new InvalidOperationException("This case should be impossible");
						}
					}
				}
			}
		}

		/// <summary>Compute the span for a given matched pattern.</summary>
		/// <remarks>
		/// Compute the span for a given matched pattern.
		/// At a high level:
		/// <ul>
		/// <li>If both a subject and an object exist, we take the subject minus the quantifier, and the object plus the pivot. </li>
		/// <li>If only an object exists, we make the subject the object, and create a dummy object to signify a one-place quantifier. </li>
		/// <li>If neither the subject or object exist, the pivot is the subject and there is no object. </li>
		/// <li>If the subject is a proper noun, only mark the object itself with the subject span. </li>
		/// </ul>
		/// But:
		/// <ul>
		/// <li>If we have a two-place quantifier, the object is allowed to absorb various specific arcs from the pivot.</li>
		/// <li>If we have a one-place quantifier, the object is allowed to absorb only prepositions from the pivot.</li>
		/// </ul>
		/// </remarks>
		private static OperatorSpec ComputeScope(SemanticGraph tree, Operator @operator, IndexedWord pivot, Pair<int, int> quantifierSpan, IndexedWord subject, bool isProperNounSubject, IndexedWord @object, int sentenceLength)
		{
			Pair<int, int> subjSpan;
			Pair<int, int> objSpan;
			if (subject == null && @object == null)
			{
				subjSpan = GetSubtreeSpan(tree, pivot);
				if (Span.FromPair(subjSpan).Contains(Span.FromPair(quantifierSpan)))
				{
					// Don't consume the quantifier -- take only the part after the quantifier
					subjSpan = Pair.MakePair(Math.Max(subjSpan.first, quantifierSpan.second), subjSpan.second);
					if (subjSpan.second <= subjSpan.first)
					{
						subjSpan = Pair.MakePair(subjSpan.first, subjSpan.first + 1);
					}
				}
				else
				{
					// Exclude the quantifier from the span
					subjSpan = ExcludeFromSpan(subjSpan, quantifierSpan);
				}
				objSpan = Pair.MakePair(subjSpan.second, subjSpan.second);
			}
			else
			{
				if (subject == null)
				{
					subjSpan = IncludeInSpan(GetSubtreeSpan(tree, @object), GetGeneralizedSubtreeSpan(tree, pivot, Java.Util.Collections.Singleton("nmod")));
					objSpan = Pair.MakePair(subjSpan.second, subjSpan.second);
				}
				else
				{
					Pair<int, int> subjectSubtree;
					if (isProperNounSubject)
					{
						subjectSubtree = GetProperNounSubtreeSpan(tree, subject);
					}
					else
					{
						subjectSubtree = GetSubtreeSpan(tree, subject);
					}
					subjSpan = ExcludeFromSpan(subjectSubtree, quantifierSpan);
					Pair<int, int> vanillaObjectSpan = GetGeneralizedSubtreeSpan(tree, @object, @object == pivot ? JjComponentArcs : null);
					objSpan = ExcludeFromSpan(IncludeInSpan(vanillaObjectSpan, @object == pivot ? vanillaObjectSpan : GetModifierSubtreeSpan(tree, pivot)), subjectSubtree);
				}
			}
			// Return scopes
			if (subjSpan.first < quantifierSpan.second && subjSpan.second > quantifierSpan.second)
			{
				subjSpan = Pair.MakePair(quantifierSpan.second, subjSpan.second);
			}
			return new OperatorSpec(@operator, quantifierSpan.first - 1, quantifierSpan.second - 1, subjSpan.first - 1, subjSpan.second - 1, objSpan.first - 1, objSpan.second - 1, sentenceLength);
		}

		/// <summary>
		/// Try to find which quantifier we matched, given that we matched the head of a quantifier at the given IndexedWord, and that
		/// this whole deal is taking place in the given sentence.
		/// </summary>
		/// <param name="sentence">The sentence we are matching.</param>
		/// <param name="quantifier">The word at which we matched a quantifier.</param>
		/// <param name="isUnary">If true, this is a unary quantifier</param>
		/// <returns>An optional triple consisting of the particular quantifier we matched, as well as the span of that quantifier in the sentence.</returns>
		private static Optional<Triple<Operator, int, int>> ValidateQuantifierByHead(ICoreMap sentence, IndexedWord quantifier, bool isUnary)
		{
			// Some useful variables
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			IFunction<CoreLabel, string> glossFn = null;
			int quantIndex = quantifier.Index();
			// Look forward a bit too, if the head is a number.
			int[] positiveOffsetToCheck = "CD".Equals(tokens[quantIndex - 1].Tag()) ? new int[] { 2, 1, 0 } : new int[] { 0 };
			// Try searching backwards for the right quantifier
			foreach (int offsetEnd in positiveOffsetToCheck)
			{
				int end = quantIndex + offsetEnd;
				for (int start = Math.Max(0, quantIndex - 10); start < quantIndex; ++start)
				{
					string gloss = StringUtils.Join(tokens, " ", glossFn, start, end).ToLower();
					foreach (Operator q in Operator.valuesByLengthDesc)
					{
						if (q.surfaceForm.Equals(gloss) && (!q.IsUnary() || isUnary))
						{
							return Optional.Of(Triple.MakeTriple(q, start + 1, end + 1));
						}
					}
				}
			}
			return Optional.Empty();
		}

		/// <summary>
		/// Find the operators in this sentence, annotating the head word (only!) of each operator with the
		/// <see cref="OperatorAnnotation"/>
		/// .
		/// </summary>
		/// <param name="sentence">
		/// As in
		/// <see cref="DoOneSentence(Edu.Stanford.Nlp.Pipeline.Annotation, Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// </param>
		private void AnnotateOperators(ICoreMap sentence)
		{
			SemanticGraph tree = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (tree == null)
			{
				tree = sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			}
			foreach (SemgrexPattern pattern in Patterns)
			{
				SemgrexMatcher matcher = pattern.Matcher(tree);
				while (matcher.Find())
				{
					// Get terms
					IndexedWord properSubject = matcher.GetNode("Subject");
					IndexedWord quantifier;
					IndexedWord subject;
					bool namedEntityQuantifier = false;
					if (properSubject != null)
					{
						quantifier = subject = properSubject;
						namedEntityQuantifier = true;
					}
					else
					{
						quantifier = matcher.GetNode("quantifier");
						subject = matcher.GetNode("subject");
					}
					IndexedWord @object = matcher.GetNode("object");
					// Validate quantifier
					// At the end of this
					Optional<Triple<Operator, int, int>> quantifierInfo;
					if (namedEntityQuantifier)
					{
						// named entities have the "all" semantics by default.
						if (!neQuantifiers)
						{
							continue;
						}
						quantifierInfo = Optional.Of(Triple.MakeTriple(Operator.ImplicitNamedEntity, quantifier.Index(), quantifier.Index()));
					}
					else
					{
						// note: empty quantifier span given
						// find the quantifier, and return some info about it.
						quantifierInfo = ValidateQuantifierByHead(sentence, quantifier, @object == null || subject == null);
					}
					// Awful hacks to regularize the subject of things like "one of" and "there are"
					// (fix up 'there are')
					if ("be".Equals(subject == null ? null : subject.Lemma()))
					{
						bool hasExpl = false;
						IndexedWord newSubject = null;
						foreach (SemanticGraphEdge outgoingEdge in tree.OutgoingEdgeIterable(subject))
						{
							if ("nsubj".Equals(outgoingEdge.GetRelation().ToString()))
							{
								newSubject = outgoingEdge.GetDependent();
							}
							else
							{
								if ("expl".Equals(outgoingEdge.GetRelation().ToString()))
								{
									hasExpl = true;
								}
							}
						}
						if (hasExpl)
						{
							subject = newSubject;
						}
					}
					// (fix up '$n$ of')
					if ("CD".Equals(subject == null ? null : subject.Tag()))
					{
						foreach (SemanticGraphEdge outgoingEdge in tree.OutgoingEdgeIterable(subject))
						{
							string rel = outgoingEdge.GetRelation().ToString();
							if (rel.StartsWith("nmod"))
							{
								subject = outgoingEdge.GetDependent();
							}
						}
					}
					// Set tokens
					if (quantifierInfo.IsPresent())
					{
						// Compute span
						IndexedWord pivot = matcher.GetNode("pivot");
						if (pivot == null)
						{
							pivot = @object;
						}
						OperatorSpec scope = ComputeScope(tree, quantifierInfo.Get().first, pivot, Pair.MakePair(quantifierInfo.Get().second, quantifierInfo.Get().third), subject, namedEntityQuantifier, @object, tokens.Count);
						// Set annotation
						CoreLabel token = sentence.Get(typeof(CoreAnnotations.TokensAnnotation))[quantifier.Index() - 1];
						OperatorSpec oldScope = token.Get(typeof(NaturalLogicAnnotations.OperatorAnnotation));
						if (oldScope == null || oldScope.QuantifierLength() < scope.QuantifierLength() || oldScope.instance != scope.instance)
						{
							token.Set(typeof(NaturalLogicAnnotations.OperatorAnnotation), scope);
						}
						else
						{
							token.Set(typeof(NaturalLogicAnnotations.OperatorAnnotation), OperatorSpec.Merge(oldScope, scope));
						}
					}
				}
			}
			// Ensure we didn't select overlapping quantifiers. For example, "a" and "a few" can often overlap.
			// In these cases, take the longer quantifier match.
			IList<OperatorSpec> quantifiers = new List<OperatorSpec>();
			for (int i = 0; i < tokens.Count; ++i)
			{
				CoreLabel token = tokens[i];
				OperatorSpec @operator;
				if ((@operator = token.Get(typeof(NaturalLogicAnnotations.OperatorAnnotation))) != null)
				{
					if (i == 0 && @operator.instance == Operator.No && tokens.Count > 2 && "PRP".Equals(tokens[1].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation))))
					{
						// This is pragmatically not a negation -- ignore it
						// For example, "no I don't like candy" or "no you like cats"
						token.Remove(typeof(NaturalLogicAnnotations.OperatorAnnotation));
					}
					else
					{
						quantifiers.Add(@operator);
					}
				}
			}
			quantifiers.Sort(null);
			foreach (OperatorSpec quantifier_1 in quantifiers)
			{
				for (int i_1 = quantifier_1.quantifierBegin; i_1 < quantifier_1.quantifierEnd; ++i_1)
				{
					if (i_1 != quantifier_1.quantifierHead)
					{
						tokens[i_1].Remove(typeof(NaturalLogicAnnotations.OperatorAnnotation));
					}
				}
			}
		}

		/// <summary>
		/// Annotate any unary quantifiers that weren't found in the main
		/// <see cref="AnnotateOperators(Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// method.
		/// </summary>
		/// <param name="sentence">The sentence to annotate.</param>
		private static void AnnotateUnaries(ICoreMap sentence)
		{
			// Get tree and tokens
			SemanticGraph tree = sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			if (tree == null)
			{
				tree = sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			}
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			// Get operator exists mask
			bool[] isOperator = new bool[tokens.Count];
			for (int i = 0; i < isOperator.Length; ++i)
			{
				OperatorSpec spec = tokens[i].Get(typeof(NaturalLogicAnnotations.OperatorAnnotation));
				if (spec != null)
				{
					for (int k = spec.quantifierBegin; k < spec.quantifierEnd; ++k)
					{
						isOperator[k] = true;
					}
				}
			}
			// Match Semgrex
			SemgrexMatcher matcher = UnaryPattern.Matcher(tree);
			while (matcher.Find())
			{
				// Get relevant nodes
				IndexedWord quantifier = matcher.GetNode("quantifier");
				string word = quantifier.Word().ToLower();
				if (word.Equals("a") || word.Equals("an") || word.Equals("the") || "CD".Equals(quantifier.Tag()))
				{
					continue;
				}
				// These are absurdly common, and uninformative, and we're just going to shoot ourselves in the foot from parsing errors and idiomatic expressions.
				IndexedWord subject = matcher.GetNode("subject");
				// ... If there is not already an operator there
				if (!isOperator[quantifier.Index() - 1])
				{
					Optional<Triple<Operator, int, int>> quantifierInfo = ValidateQuantifierByHead(sentence, quantifier, true);
					// ... and if we found a quantifier span
					if (quantifierInfo.IsPresent())
					{
						// Then add the unary operator!
						OperatorSpec scope = ComputeScope(tree, quantifierInfo.Get().first, subject, Pair.MakePair(quantifierInfo.Get().second, quantifierInfo.Get().third), null, false, null, tokens.Count);
						CoreLabel token = tokens[quantifier.Index() - 1];
						token.Set(typeof(NaturalLogicAnnotations.OperatorAnnotation), scope);
					}
				}
			}
			// Match TokensRegex
			TokenSequenceMatcher tokenMatcher = DoubtPattern.Matcher(tokens);
			while (tokenMatcher.Find())
			{
				IList<CoreLabel> doubt = (IList<CoreLabel>)tokenMatcher.GroupNodes("$doubt");
				IList<CoreLabel> target = (IList<CoreLabel>)tokenMatcher.GroupNodes("$target");
				foreach (CoreLabel word in doubt)
				{
					OperatorSpec spec = new OperatorSpec(Operator.GeneralNegPolarity, word.Index() - 1, word.Index(), target[0].Index() - 1, target[target.Count - 1].Index(), 0, 0, tokens.Count);
					word.Set(typeof(NaturalLogicAnnotations.OperatorAnnotation), spec);
				}
			}
		}

		/// <summary>Annotate every token for its polarity, based on the operators found.</summary>
		/// <remarks>
		/// Annotate every token for its polarity, based on the operators found. This function will set the
		/// <see cref="PolarityAnnotation"/>
		/// for every token.
		/// </remarks>
		/// <param name="sentence">
		/// As in
		/// <see cref="DoOneSentence(Edu.Stanford.Nlp.Pipeline.Annotation, Edu.Stanford.Nlp.Util.ICoreMap)"/>
		/// </param>
		private static void AnnotatePolarity(ICoreMap sentence)
		{
			// Collect all the operators in this sentence
			IList<OperatorSpec> operators = new List<OperatorSpec>();
			IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
			foreach (CoreLabel token in tokens)
			{
				OperatorSpec specOrNull = token.Get(typeof(NaturalLogicAnnotations.OperatorAnnotation));
				if (specOrNull != null)
				{
					operators.Add(specOrNull);
				}
			}
			// Make sure every node of the dependency tree has a polarity.
			// This is separate from the code below in case the tokens in the dependency
			// tree don't correspond to the tokens in the sentence. This happens at least
			// when the constituency parser craps out on a long sentence, and the
			// dependency tree is put together haphazardly.
			if (sentence.ContainsKey(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)))
			{
				foreach (IndexedWord token_1 in sentence.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)).VertexSet())
				{
					token_1.Set(typeof(NaturalLogicAnnotations.PolarityAnnotation), Polarity.Default);
				}
			}
			if (sentence.ContainsKey(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)))
			{
				foreach (IndexedWord token_1 in sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)).VertexSet())
				{
					token_1.Set(typeof(NaturalLogicAnnotations.PolarityAnnotation), Polarity.Default);
				}
			}
			if (sentence.ContainsKey(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)))
			{
				foreach (IndexedWord token_1 in sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)).VertexSet())
				{
					token_1.Set(typeof(NaturalLogicAnnotations.PolarityAnnotation), Polarity.Default);
				}
			}
			// Set polarity for each token
			for (int i = 0; i < tokens.Count; ++i)
			{
				CoreLabel token_1 = tokens[i];
				// Get operators in scope
				IList<Triple<int, Monotonicity, MonotonicityType>> inScope = new List<Triple<int, Monotonicity, MonotonicityType>>(4);
				foreach (OperatorSpec @operator in operators)
				{
					if (i >= @operator.subjectBegin && i < @operator.subjectEnd)
					{
						inScope.Add(Triple.MakeTriple(@operator.subjectEnd - @operator.subjectBegin, @operator.instance.subjMono, @operator.instance.subjType));
					}
					else
					{
						if (i >= @operator.objectBegin && i < @operator.objectEnd)
						{
							inScope.Add(Triple.MakeTriple(@operator.objectEnd - @operator.objectBegin, @operator.instance.objMono, @operator.instance.objType));
						}
					}
				}
				// Sort the operators by their scope (approximated by the size of their argument span)
				inScope.Sort(null);
				// Create polarity
				IList<Pair<Monotonicity, MonotonicityType>> info = new List<Pair<Monotonicity, MonotonicityType>>(inScope.Count);
				foreach (Triple<int, Monotonicity, MonotonicityType> term in inScope)
				{
					info.Add(Pair.MakePair(term.second, term.third));
				}
				Polarity polarity = new Polarity(info);
				// Set polarity
				token_1.Set(typeof(NaturalLogicAnnotations.PolarityAnnotation), polarity);
			}
			// Set the PolarityDirectionAnnotation
			foreach (CoreLabel token_2 in tokens)
			{
				Polarity polarity = token_2.Get(typeof(NaturalLogicAnnotations.PolarityAnnotation));
				if (polarity != null)
				{
					if (polarity.IsUpwards())
					{
						token_2.Set(typeof(NaturalLogicAnnotations.PolarityDirectionAnnotation), "up");
					}
					else
					{
						if (polarity.IsDownwards())
						{
							token_2.Set(typeof(NaturalLogicAnnotations.PolarityDirectionAnnotation), "down");
						}
						else
						{
							token_2.Set(typeof(NaturalLogicAnnotations.PolarityDirectionAnnotation), "flat");
						}
					}
				}
			}
		}

		/// <summary>If false, don't annotate tokens for polarity but only find the operators and their scopes.</summary>
		private bool doPolarity = true;

		private bool neQuantifiers = false;

		/// <summary>Create a new annotator.</summary>
		/// <param name="annotatorName">The prefix for the properties for this annotator.</param>
		/// <param name="props">The properties to configure this annotator with.</param>
		public NaturalLogicAnnotator(string annotatorName, Properties props)
		{
			ArgumentParser.FillOptions(this, annotatorName, props);
		}

		/// <seealso cref="NaturalLogicAnnotator(string, Java.Util.Properties)"/>
		public NaturalLogicAnnotator(Properties props)
			: this(AnnotatorConstants.StanfordNatlog, props)
		{
		}

		/// <summary>The default constructor</summary>
		public NaturalLogicAnnotator()
			: this("__irrelevant__", new Properties())
		{
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override void DoOneSentence(Annotation annotation, ICoreMap sentence)
		{
			AnnotateOperators(sentence);
			AnnotateUnaries(sentence);
			if (doPolarity)
			{
				AnnotatePolarity(sentence);
			}
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override int NThreads()
		{
			return 1;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override long MaxTime()
		{
			return -1;
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		protected internal override void DoOneFailedSentence(Annotation annotation, ICoreMap sentence)
		{
			log.Info("Failed to annotate: " + sentence.Get(typeof(CoreAnnotations.TextAnnotation)));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(doPolarity ? typeof(NaturalLogicAnnotations.PolarityAnnotation) : null, typeof(NaturalLogicAnnotations.OperatorAnnotation))));
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		public override ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.SentencesAnnotation
				), typeof(CoreAnnotations.SentenceIndexAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.LemmaAnnotation), typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation
				), typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation))));
		}
	}
}

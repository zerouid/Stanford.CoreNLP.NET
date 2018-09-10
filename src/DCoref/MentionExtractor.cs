//
// StanfordCoreNLP -- a suite of NLP tools
// Copyright (c) 2009-2011 The Board of Trustees of
// The Leland Stanford Junior University. All Rights Reserved.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 1A
//    Stanford CA 94305-9010
//    USA
//
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Generic mention extractor from a corpus.</summary>
	/// <author>Jenny Finkel</author>
	/// <author>Mihai Surdeanu</author>
	/// <author>Karthik Raghunathan</author>
	/// <author>Heeyoung Lee</author>
	/// <author>Sudarshan Rangarajan</author>
	public class MentionExtractor
	{
		private readonly IHeadFinder headFinder;

		protected internal string currentDocumentID;

		protected internal readonly Dictionaries dictionaries;

		protected internal readonly Semantics semantics;

		public ICorefMentionFinder mentionFinder;

		protected internal StanfordCoreNLP stanfordProcessor;

		protected internal LogisticClassifier<string, string> singletonPredictor;

		/// <summary>The maximum mention ID: for preventing duplicated mention ID assignment</summary>
		protected internal int maxID = -1;

		public const bool Verbose = false;

		public MentionExtractor(Dictionaries dict, Semantics semantics)
		{
			this.headFinder = new SemanticHeadFinder();
			this.dictionaries = dict;
			this.semantics = semantics;
			this.mentionFinder = new RuleBasedCorefMentionFinder();
		}

		// Default
		public virtual void SetMentionFinder(ICorefMentionFinder mentionFinder)
		{
			this.mentionFinder = mentionFinder;
		}

		/// <summary>Extracts the info relevant for coref from the next document in the corpus</summary>
		/// <returns>List of mentions found in each sentence ordered according to the tree traversal.</returns>
		/// <exception cref="System.Exception"/>
		public virtual Document NextDoc()
		{
			return null;
		}

		/// <summary>Reset so that we start at the beginning of the document collection</summary>
		public virtual void ResetDocs()
		{
			maxID = -1;
			currentDocumentID = null;
		}

		/// <exception cref="System.Exception"/>
		public virtual Document Arrange(Annotation anno, IList<IList<CoreLabel>> words, IList<Tree> trees, IList<IList<Mention>> unorderedMentions)
		{
			return Arrange(anno, words, trees, unorderedMentions, null, false);
		}

		protected internal virtual int GetHeadIndex(Tree t)
		{
			// The trees passed in do not have the CoordinationTransformer
			// applied, but that just means the SemanticHeadFinder results are
			// slightly worse.
			Tree ht = t.HeadTerminal(headFinder);
			if (ht == null)
			{
				return -1;
			}
			// temporary: a key which is matched to nothing
			CoreLabel l = (CoreLabel)ht.Label();
			return l.Get(typeof(CoreAnnotations.IndexAnnotation));
		}

		private string TreeToKey(Tree t)
		{
			int idx = GetHeadIndex(t);
			string key = int.ToString(idx) + ':' + t.ToString();
			return key;
		}

		/// <exception cref="System.Exception"/>
		public virtual Document Arrange(Annotation anno, IList<IList<CoreLabel>> words, IList<Tree> trees, IList<IList<Mention>> unorderedMentions, IList<IList<Mention>> unorderedGoldMentions, bool doMergeLabels)
		{
			IList<IList<Mention>> predictedOrderedMentionsBySentence = Arrange(anno, words, trees, unorderedMentions, doMergeLabels);
			IList<IList<Mention>> goldOrderedMentionsBySentence = null;
			//    SieveCoreferenceSystem.debugPrintMentions(System.err, "UNORDERED GOLD MENTIONS:", unorderedGoldMentions);
			if (unorderedGoldMentions != null)
			{
				goldOrderedMentionsBySentence = Arrange(anno, words, trees, unorderedGoldMentions, doMergeLabels);
			}
			//    SieveCoreferenceSystem.debugPrintMentions(System.err, "ORDERED GOLD MENTIONS:", goldOrderedMentionsBySentence);
			return new Document(anno, predictedOrderedMentionsBySentence, goldOrderedMentionsBySentence, dictionaries);
		}

		/// <summary>Post-processes the extracted mentions.</summary>
		/// <remarks>Post-processes the extracted mentions. Here we set the Mention fields required for coref and order mentions by tree-traversal order.</remarks>
		/// <param name="words">List of words in each sentence, in textual order</param>
		/// <param name="trees">List of trees, one per sentence</param>
		/// <param name="unorderedMentions">
		/// List of unordered, unprocessed mentions
		/// Each mention MUST have startIndex and endIndex set!
		/// Optionally, if scoring is desired, mentions must have mentionID and originalRef set.
		/// All the other Mention fields are set here.
		/// </param>
		/// <returns>List of mentions ordered according to the tree traversal</returns>
		/// <exception cref="System.Exception"/>
		public virtual IList<IList<Mention>> Arrange(Annotation anno, IList<IList<CoreLabel>> words, IList<Tree> trees, IList<IList<Mention>> unorderedMentions, bool doMergeLabels)
		{
			IList<IList<Mention>> orderedMentionsBySentence = new List<IList<Mention>>();
			//
			// traverse all sentences and process each individual one
			//
			for (int sent = 0; sent < sz; sent++)
			{
				IList<CoreLabel> sentence = words[sent];
				Tree tree = trees[sent];
				IList<Mention> mentions = unorderedMentions[sent];
				IDictionary<string, IList<Mention>> mentionsToTrees = Generics.NewHashMap();
				// merge the parse tree of the entire sentence with the sentence words
				if (doMergeLabels)
				{
					MergeLabels(tree, sentence);
				}
				//
				// set the surface information and the syntactic info in each mention
				// startIndex and endIndex MUST be set before!
				//
				foreach (Mention mention in mentions)
				{
					mention.contextParseTree = tree;
					mention.sentenceWords = sentence;
					mention.originalSpan = new List<CoreLabel>(mention.sentenceWords.SubList(mention.startIndex, mention.endIndex));
					if (!((CoreLabel)tree.Label()).ContainsKey(typeof(CoreAnnotations.BeginIndexAnnotation)))
					{
						tree.IndexSpans(0);
					}
					if (mention.headWord == null)
					{
						Tree headTree = ((RuleBasedCorefMentionFinder)mentionFinder).FindSyntacticHead(mention, tree, sentence);
						mention.headWord = (CoreLabel)headTree.Label();
						mention.headIndex = mention.headWord.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					}
					if (mention.mentionSubTree == null)
					{
						// mentionSubTree = highest NP that has the same head
						Tree headTree = tree.GetLeaves()[mention.headIndex];
						if (headTree == null)
						{
							throw new Exception("Missing head tree for a mention!");
						}
						Tree t = headTree;
						while ((t = t.Parent(tree)) != null)
						{
							if (t.HeadTerminal(headFinder) == headTree && t.Value().Equals("NP"))
							{
								mention.mentionSubTree = t;
							}
							else
							{
								if (mention.mentionSubTree != null)
								{
									break;
								}
							}
						}
						if (mention.mentionSubTree == null)
						{
							mention.mentionSubTree = headTree;
						}
					}
					IList<Mention> mentionsForTree = mentionsToTrees[TreeToKey(mention.mentionSubTree)];
					if (mentionsForTree == null)
					{
						mentionsForTree = new List<Mention>();
						mentionsToTrees[TreeToKey(mention.mentionSubTree)] = mentionsForTree;
					}
					mentionsForTree.Add(mention);
					// generates all fields required for coref, such as gender, number, etc.
					mention.Process(dictionaries, semantics, this, singletonPredictor);
				}
				//
				// Order all mentions in tree-traversal order
				//
				IList<Mention> orderedMentions = new List<Mention>();
				orderedMentionsBySentence.Add(orderedMentions);
				// extract all mentions in tree traversal order (alternative: tree.postOrderNodeList())
				foreach (Tree t_1 in tree.PreOrderNodeList())
				{
					IList<Mention> lm = mentionsToTrees[TreeToKey(t_1)];
					if (lm != null)
					{
						foreach (Mention m in lm)
						{
							orderedMentions.Add(m);
						}
					}
				}
				//
				// find appositions, predicate nominatives, relative pronouns in this sentence
				//
				FindSyntacticRelations(tree, orderedMentions);
				System.Diagnostics.Debug.Assert((mentions.Count == orderedMentions.Count));
			}
			return orderedMentionsBySentence;
		}

		/// <summary>Sets the label of the leaf nodes of a Tree to be the CoreLabels in the given sentence.</summary>
		/// <remarks>
		/// Sets the label of the leaf nodes of a Tree to be the CoreLabels in the given sentence.
		/// The original value() of the Tree nodes is preserved, and otherwise the label of tree
		/// leaves becomes the label from the List.
		/// </remarks>
		public static void MergeLabels(Tree tree, IList<CoreLabel> sentence)
		{
			// todo [cdm 2015]: This clearly shouldn't be here! Maybe it's not needed at all now since parsing code does this?
			int idx = 0;
			foreach (Tree t in tree.GetLeaves())
			{
				CoreLabel cl = sentence[idx++];
				string value = t.Value();
				cl.Set(typeof(CoreAnnotations.ValueAnnotation), value);
				t.SetLabel(cl);
			}
			tree.IndexLeaves();
		}

		private static bool Inside(int i, Mention m)
		{
			return i >= m.startIndex && i < m.endIndex;
		}

		/// <summary>Find syntactic relations (e.g., appositives) in a sentence</summary>
		private void FindSyntacticRelations(Tree tree, IList<Mention> orderedMentions)
		{
			MarkListMemberRelation(orderedMentions);
			ICollection<Pair<int, int>> appos = Generics.NewHashSet();
			// TODO: This apposition finding doesn't seem to be very good - what about using "appos" from dependencies?
			FindAppositions(tree, appos);
			MarkMentionRelation(orderedMentions, appos, "APPOSITION");
			ICollection<Pair<int, int>> preNomi = Generics.NewHashSet();
			FindPredicateNominatives(tree, preNomi);
			MarkMentionRelation(orderedMentions, preNomi, "PREDICATE_NOMINATIVE");
			ICollection<Pair<int, int>> relativePronounPairs = Generics.NewHashSet();
			FindRelativePronouns(tree, relativePronounPairs);
			MarkMentionRelation(orderedMentions, relativePronounPairs, "RELATIVE_PRONOUN");
		}

		/// <summary>Find syntactic pattern in a sentence by tregex</summary>
		private void FindTreePattern(Tree tree, string tregex, ICollection<Pair<int, int>> foundPairs)
		{
			try
			{
				TregexPattern tgrepPattern = TregexPattern.Compile(tregex);
				FindTreePattern(tree, tgrepPattern, foundPairs);
			}
			catch (Exception e)
			{
				// shouldn't happen....
				throw new Exception(e);
			}
		}

		private void FindTreePattern(Tree tree, TregexPattern tgrepPattern, ICollection<Pair<int, int>> foundPairs)
		{
			try
			{
				TregexMatcher m = tgrepPattern.Matcher(tree);
				while (m.Find())
				{
					Tree t = m.GetMatch();
					Tree np1 = m.GetNode("m1");
					Tree np2 = m.GetNode("m2");
					Tree np3 = null;
					if (tgrepPattern.Pattern().Contains("m3"))
					{
						np3 = m.GetNode("m3");
					}
					AddFoundPair(np1, np2, t, foundPairs);
					if (np3 != null)
					{
						AddFoundPair(np2, np3, t, foundPairs);
					}
				}
			}
			catch (Exception e)
			{
				// shouldn't happen....
				throw new Exception(e);
			}
		}

		private void AddFoundPair(Tree np1, Tree np2, Tree t, ICollection<Pair<int, int>> foundPairs)
		{
			Tree head1 = np1.HeadTerminal(headFinder);
			Tree head2 = np2.HeadTerminal(headFinder);
			int h1 = ((ICoreMap)head1.Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
			int h2 = ((ICoreMap)head2.Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
			Pair<int, int> p = new Pair<int, int>(h1, h2);
			foundPairs.Add(p);
		}

		private static readonly TregexPattern appositionPattern = TregexPattern.Compile("NP=m1 < (NP=m2 $.. (/,/ $.. NP=m3))");

		private static readonly TregexPattern appositionPattern2 = TregexPattern.Compile("NP=m1 < (NP=m2 $.. (/,/ $.. (SBAR < (WHNP < WP|WDT=m3))))");

		private static readonly TregexPattern appositionPattern3 = TregexPattern.Compile("/^NP(?:-TMP|-ADV)?$/=m1 < (NP=m2 $- /^,$/ $-- NP=m3 !$ CC|CONJP)");

		private static readonly TregexPattern appositionPattern4 = TregexPattern.Compile("/^NP(?:-TMP|-ADV)?$/=m1 < (PRN=m2 < (NP < /^NNS?|CD$/ $-- /^-LRB-$/ $+ /^-RRB-$/))");

		private void FindAppositions(Tree tree, ICollection<Pair<int, int>> appos)
		{
			FindTreePattern(tree, appositionPattern, appos);
			FindTreePattern(tree, appositionPattern2, appos);
			FindTreePattern(tree, appositionPattern3, appos);
			FindTreePattern(tree, appositionPattern4, appos);
		}

		private static readonly TregexPattern predicateNominativePattern = TregexPattern.Compile("S < (NP=m1 $.. (VP < ((/VB/ < /^(am|are|is|was|were|'m|'re|'s|be)$/) $.. NP=m2)))");

		private static readonly TregexPattern predicateNominativePattern2 = TregexPattern.Compile("S < (NP=m1 $.. (VP < (VP < ((/VB/ < /^(be|been|being)$/) $.. NP=m2))))");

		private void FindPredicateNominatives(Tree tree, ICollection<Pair<int, int>> preNomi)
		{
			//    String predicateNominativePattern2 = "NP=m1 $.. (VP < ((/VB/ < /^(am|are|is|was|were|'m|'re|'s|be)$/) $.. NP=m2))";
			FindTreePattern(tree, predicateNominativePattern, preNomi);
			FindTreePattern(tree, predicateNominativePattern2, preNomi);
		}

		private static readonly TregexPattern relativePronounPattern = TregexPattern.Compile("NP < (NP=m1 $.. (SBAR < (WHNP < WP|WDT=m2)))");

		private void FindRelativePronouns(Tree tree, ICollection<Pair<int, int>> relativePronounPairs)
		{
			FindTreePattern(tree, relativePronounPattern, relativePronounPairs);
		}

		private static void MarkListMemberRelation(IList<Mention> orderedMentions)
		{
			foreach (Mention m1 in orderedMentions)
			{
				foreach (Mention m2 in orderedMentions)
				{
					// Mark if m2 and m1 are in list relationship
					if (m1.IsListMemberOf(m2))
					{
						m2.AddListMember(m1);
						m1.AddBelongsToList(m2);
					}
					else
					{
						if (m2.IsListMemberOf(m1))
						{
							m1.AddListMember(m2);
							m2.AddBelongsToList(m1);
						}
					}
				}
			}
		}

		private static void MarkMentionRelation(IList<Mention> orderedMentions, ICollection<Pair<int, int>> foundPairs, string flag)
		{
			foreach (Mention m1 in orderedMentions)
			{
				foreach (Mention m2 in orderedMentions)
				{
					// Ignore if m2 and m1 are in list relationship
					if (m1.IsListMemberOf(m2) || m2.IsListMemberOf(m1) || m1.IsMemberOfSameList(m2))
					{
						SieveCoreferenceSystem.logger.Finest("Not checking '" + m1 + "' and '" + m2 + "' for " + flag + ": in list relationship");
						continue;
					}
					foreach (Pair<int, int> foundPair in foundPairs)
					{
						if ((foundPair.first == m1.headIndex && foundPair.second == m2.headIndex))
						{
							switch (flag)
							{
								case "APPOSITION":
								{
									m2.AddApposition(m1);
									break;
								}

								case "PREDICATE_NOMINATIVE":
								{
									m2.AddPredicateNominatives(m1);
									break;
								}

								case "RELATIVE_PRONOUN":
								{
									m2.AddRelativePronoun(m1);
									break;
								}

								default:
								{
									throw new Exception("check flag in markMentionRelation (dcoref/MentionExtractor.java)");
								}
							}
						}
					}
				}
			}
		}

		/// <summary>Finds the tree the matches this span exactly</summary>
		/// <param name="tree">Leaves must be indexed!</param>
		/// <param name="first">First element in the span (first position has offset 1)</param>
		/// <param name="last">Last element included in the span (first position has offset 1)</param>
		public static Tree FindExactMatch(Tree tree, int first, int last)
		{
			IList<Tree> leaves = tree.GetLeaves();
			int thisFirst = ((ICoreMap)leaves[0].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
			int thisLast = ((ICoreMap)leaves[leaves.Count - 1].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
			if (thisFirst == first && thisLast == last)
			{
				return tree;
			}
			else
			{
				Tree[] kids = tree.Children();
				foreach (Tree k in kids)
				{
					Tree t = FindExactMatch(k, first, last);
					if (t != null)
					{
						return t;
					}
				}
			}
			return null;
		}

		/// <summary>Load Stanford Processor: skip unnecessary annotator</summary>
		protected internal static StanfordCoreNLP LoadStanfordProcessor(Properties props)
		{
			bool replicateCoNLL = bool.Parse(props.GetProperty(Constants.ReplicateconllProp, "false"));
			Properties pipelineProps = new Properties(props);
			StringBuilder annoSb = new StringBuilder(string.Empty);
			if (!Constants.UseGoldPos && !replicateCoNLL)
			{
				annoSb.Append("pos, lemma");
			}
			else
			{
				annoSb.Append("lemma");
			}
			if (!Constants.UseGoldNe && !replicateCoNLL)
			{
				annoSb.Append(", ner");
			}
			if (!Constants.UseGoldParses && !replicateCoNLL)
			{
				annoSb.Append(", parse");
			}
			string annoStr = annoSb.ToString();
			SieveCoreferenceSystem.logger.Info("MentionExtractor ignores specified annotators, using annotators=" + annoStr);
			pipelineProps.SetProperty("annotators", annoStr);
			return new StanfordCoreNLP(pipelineProps, false);
		}

		public static void InitializeUtterance(IList<CoreLabel> tokens)
		{
			foreach (CoreLabel l in tokens)
			{
				if (l.Get(typeof(CoreAnnotations.UtteranceAnnotation)) == null)
				{
					l.Set(typeof(CoreAnnotations.UtteranceAnnotation), 0);
				}
			}
		}
	}
}

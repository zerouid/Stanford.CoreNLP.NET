// Stanford Parser -- a probabilistic lexicalized NL CFG parser
// Copyright (c) 2002 - 2011 The Board of Trustees of
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
// along with this program.  If not, see http://www.gnu.org/licenses/ .
//
// For more information, bug reports, fixes, contact:
//    Christopher Manning
//    Dept of Computer Science, Gates 2A
//    Stanford CA 94305-9020
//    USA
//    parser-support@lists.stanford.edu
//    https://nlp.stanford.edu/software/lex-parser.html
using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Parser.Lexparser
{
	public class LexicalizedParserQuery : IParserQuery
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Parser.Lexparser.LexicalizedParserQuery));

		private readonly Options op;

		private readonly ITreeTransformer debinarizer;

		private readonly ITreeTransformer boundaryRemover;

		/// <summary>The PCFG parser.</summary>
		private readonly ExhaustivePCFGParser pparser;

		/// <summary>The dependency parser.</summary>
		private readonly ExhaustiveDependencyParser dparser;

		/// <summary>The factored parser that combines the dependency and PCFG parsers.</summary>
		private readonly IKBestViterbiParser bparser;

		private readonly bool fallbackToPCFG = true;

		private readonly ITreeTransformer subcategoryStripper;

		private bool parseSucceeded = false;

		private bool parseSkipped = false;

		private bool parseFallback = false;

		private bool parseNoMemory = false;

		private bool parseUnparsable = false;

		private string whatFailed = null;

		// Whether or not the most complicated model available successfully
		// parsed the input sentence.
		// parseSkipped means that not only did we not succeed at parsing,
		// but for some reason we didn't even try.  Most likely this happens
		// when the sentence is too long or is of length 0.
		// In some sense we succeeded, but only because we used a fallback grammar
		// Not enough memory to parse
		// Horrible error
		// If something ran out of memory, where the error occurred
		public virtual bool ParseSucceeded()
		{
			return parseSucceeded;
		}

		public virtual bool ParseSkipped()
		{
			return parseSkipped;
		}

		public virtual bool ParseFallback()
		{
			return parseFallback;
		}

		public virtual bool ParseNoMemory()
		{
			return parseNoMemory;
		}

		public virtual bool ParseUnparsable()
		{
			return parseUnparsable;
		}

		private IList<IHasWord> originalSentence;

		public virtual IList<IHasWord> OriginalSentence()
		{
			return originalSentence;
		}

		/// <summary>Keeps track of whether the sentence had punctuation added, which affects the expected length of the sentence</summary>
		private bool addedPunct = false;

		private bool saidMemMessage = false;

		public virtual bool SaidMemMessage()
		{
			return saidMemMessage;
		}

		internal LexicalizedParserQuery(LexicalizedParser parser)
		{
			this.op = parser.GetOp();
			BinaryGrammar bg = parser.bg;
			UnaryGrammar ug = parser.ug;
			ILexicon lex = parser.lex;
			IDependencyGrammar dg = parser.dg;
			IIndex<string> stateIndex = parser.stateIndex;
			IIndex<string> wordIndex = new DeltaIndex<string>(parser.wordIndex);
			IIndex<string> tagIndex = parser.tagIndex;
			this.debinarizer = new Debinarizer(op.forceCNF);
			this.boundaryRemover = new BoundaryRemover();
			if (op.doPCFG)
			{
				if (op.testOptions.iterativeCKY)
				{
					pparser = new IterativeCKYPCFGParser(bg, ug, lex, op, stateIndex, wordIndex, tagIndex);
				}
				else
				{
					pparser = new ExhaustivePCFGParser(bg, ug, lex, op, stateIndex, wordIndex, tagIndex);
				}
			}
			else
			{
				pparser = null;
			}
			if (op.doDep)
			{
				dg.SetLexicon(lex);
				if (!op.testOptions.useFastFactored)
				{
					dparser = new ExhaustiveDependencyParser(dg, lex, op, wordIndex, tagIndex);
				}
				else
				{
					dparser = null;
				}
			}
			else
			{
				dparser = null;
			}
			if (op.doDep && op.doPCFG)
			{
				if (op.testOptions.useFastFactored)
				{
					MLEDependencyGrammar mledg = (MLEDependencyGrammar)dg;
					int numToFind = 1;
					if (op.testOptions.printFactoredKGood > 0)
					{
						numToFind = op.testOptions.printFactoredKGood;
					}
					bparser = new FastFactoredParser(pparser, mledg, op, numToFind, wordIndex, tagIndex);
				}
				else
				{
					IScorer scorer = new TwinScorer(pparser, dparser);
					//Scorer scorer = parser;
					if (op.testOptions.useN5)
					{
						bparser = new BiLexPCFGParser.N5BiLexPCFGParser(scorer, pparser, dparser, bg, ug, dg, lex, op, stateIndex, wordIndex, tagIndex);
					}
					else
					{
						bparser = new BiLexPCFGParser(scorer, pparser, dparser, bg, ug, dg, lex, op, stateIndex, wordIndex, tagIndex);
					}
				}
			}
			else
			{
				bparser = null;
			}
			subcategoryStripper = op.tlpParams.SubcategoryStripper();
		}

		public virtual void SetConstraints(IList<ParserConstraint> constraints)
		{
			if (pparser != null)
			{
				pparser.SetConstraints(constraints);
			}
		}

		/// <summary>Parse a sentence represented as a List of tokens.</summary>
		/// <remarks>
		/// Parse a sentence represented as a List of tokens.
		/// The text must already have been tokenized and
		/// normalized into tokens that are appropriate to the treebank
		/// which was used to train the parser.  The tokens can be of
		/// multiple types, and the list items need not be homogeneous as to type
		/// (in particular, only some words might be given tags):
		/// <ul>
		/// <li>If a token implements HasWord, then the word to be parsed is
		/// given by its word() value.</li>
		/// <li>If a token implements HasTag and the tag() value is not
		/// null or the empty String, then the parser is strongly advised to assign
		/// a part of speech tag that <i>begins</i> with this String.</li>
		/// </ul>
		/// </remarks>
		/// <param name="sentence">The sentence to parse</param>
		/// <returns>true Iff the sentence was accepted by the grammar</returns>
		/// <exception cref="System.NotSupportedException">
		/// If the Sentence is too long or
		/// of zero length or the parse
		/// otherwise fails for resource reasons
		/// </exception>
		private bool ParseInternal<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			parseSucceeded = false;
			parseNoMemory = false;
			parseUnparsable = false;
			parseSkipped = false;
			parseFallback = false;
			whatFailed = null;
			addedPunct = false;
			originalSentence = sentence;
			int length = sentence.Count;
			if (length == 0)
			{
				parseSkipped = true;
				throw new NotSupportedException("Can't parse a zero-length sentence!");
			}
			IList<IHasWord> sentenceB;
			if (op.wordFunction != null)
			{
				sentenceB = Generics.NewArrayList();
				foreach (IHasWord word in originalSentence)
				{
					if (word is ILabel)
					{
						ILabel label = (ILabel)word;
						ILabel newLabel = label.LabelFactory().NewLabel(label);
						if (newLabel is IHasWord)
						{
							sentenceB.Add((IHasWord)newLabel);
						}
						else
						{
							throw new AssertionError("This should have been a HasWord");
						}
					}
					else
					{
						if (word is IHasTag)
						{
							TaggedWord tw = new TaggedWord(word.Word(), ((IHasTag)word).Tag());
							sentenceB.Add(tw);
						}
						else
						{
							sentenceB.Add(new Word(word.Word()));
						}
					}
				}
				foreach (IHasWord word_1 in sentenceB)
				{
					word_1.SetWord(op.wordFunction.Apply(word_1.Word()));
				}
			}
			else
			{
				sentenceB = new List<IHasWord>(sentence);
			}
			if (op.testOptions.addMissingFinalPunctuation)
			{
				addedPunct = AddSentenceFinalPunctIfNeeded(sentenceB, length);
			}
			if (length > op.testOptions.maxLength)
			{
				parseSkipped = true;
				throw new NotSupportedException("Sentence too long: length " + length);
			}
			TreePrint treePrint = GetTreePrint();
			PrintWriter pwOut = op.tlpParams.Pw();
			//Insert the boundary symbol
			if (sentence[0] is CoreLabel)
			{
				CoreLabel boundary = new CoreLabel();
				boundary.SetWord(LexiconConstants.Boundary);
				boundary.SetValue(LexiconConstants.Boundary);
				boundary.SetTag(LexiconConstants.BoundaryTag);
				boundary.SetIndex(sentence.Count + 1);
				//1-based indexing used in the parser
				sentenceB.Add(boundary);
			}
			else
			{
				sentenceB.Add(new TaggedWord(LexiconConstants.Boundary, LexiconConstants.BoundaryTag));
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			if (op.doPCFG)
			{
				if (!pparser.Parse(sentenceB))
				{
					return parseSucceeded;
				}
				if (op.testOptions.verbose)
				{
					pwOut.Println("PParser output");
					// getBestPCFGParse(false).pennPrint(pwOut); // with scores on nodes
					treePrint.PrintTree(GetBestPCFGParse(false), pwOut);
				}
			}
			// without scores on nodes
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			if (op.doDep && !op.testOptions.useFastFactored)
			{
				if (!dparser.Parse(sentenceB))
				{
					return parseSucceeded;
				}
				// cdm nov 2006: should move these printing bits to the main printing section,
				// so don't calculate the best parse twice!
				if (op.testOptions.verbose)
				{
					pwOut.Println("DParser output");
					treePrint.PrintTree(dparser.GetBestParse(), pwOut);
				}
			}
			if (Thread.Interrupted())
			{
				throw new RuntimeInterruptedException();
			}
			if (op.doPCFG && op.doDep)
			{
				if (!bparser.Parse(sentenceB))
				{
					return parseSucceeded;
				}
				else
				{
					parseSucceeded = true;
				}
			}
			return true;
		}

		public virtual void RestoreOriginalWords(Tree tree)
		{
			if (originalSentence == null || tree == null)
			{
				return;
			}
			IList<Tree> leaves = tree.GetLeaves();
			int expectedSize = addedPunct ? originalSentence.Count + 1 : originalSentence.Count;
			if (leaves.Count != expectedSize)
			{
				throw new InvalidOperationException("originalWords and sentence of different sizes: " + expectedSize + " vs. " + leaves.Count + "\n Orig: " + SentenceUtils.ListToString(originalSentence) + "\n Pars: " + SentenceUtils.ListToString(leaves));
			}
			IEnumerator<Tree> leafIterator = leaves.GetEnumerator();
			foreach (IHasWord word in originalSentence)
			{
				Tree leaf = leafIterator.Current;
				if (!(word is ILabel))
				{
					continue;
				}
				leaf.SetLabel((ILabel)word);
			}
		}

		/// <summary>Parse a (speech) lattice with the PCFG parser.</summary>
		/// <param name="lr">a lattice to parse</param>
		/// <returns>Whether the lattice could be parsed by the grammar</returns>
		internal virtual bool Parse(HTKLatticeReader lr)
		{
			TreePrint treePrint = GetTreePrint();
			PrintWriter pwOut = op.tlpParams.Pw();
			parseSucceeded = false;
			parseNoMemory = false;
			parseUnparsable = false;
			parseSkipped = false;
			parseFallback = false;
			whatFailed = null;
			originalSentence = null;
			if (lr.GetNumStates() > op.testOptions.maxLength + 1)
			{
				// + 1 for boundary symbol
				parseSkipped = true;
				throw new NotSupportedException("Lattice too big: " + lr.GetNumStates());
			}
			if (op.doPCFG)
			{
				if (!pparser.Parse(lr))
				{
					return parseSucceeded;
				}
				if (op.testOptions.verbose)
				{
					pwOut.Println("PParser output");
					treePrint.PrintTree(GetBestPCFGParse(false), pwOut);
				}
			}
			parseSucceeded = true;
			return true;
		}

		/// <summary>Return the best parse of the sentence most recently parsed.</summary>
		/// <remarks>
		/// Return the best parse of the sentence most recently parsed.
		/// This will be from the factored parser, if it was used and it succeeded
		/// else from the PCFG if it was used and succeed, else from the dependency
		/// parser.
		/// </remarks>
		/// <returns>The best tree</returns>
		/// <exception cref="Edu.Stanford.Nlp.Parser.Common.NoSuchParseException">
		/// If no previously successfully parsed
		/// sentence
		/// </exception>
		public virtual Tree GetBestParse()
		{
			return GetBestParse(true);
		}

		internal virtual Tree GetBestParse(bool stripSubcat)
		{
			if (parseSkipped)
			{
				return null;
			}
			if (bparser != null && parseSucceeded)
			{
				Tree binaryTree = bparser.GetBestParse();
				Tree tree = debinarizer.TransformTree(binaryTree);
				if (op.nodePrune)
				{
					NodePruner np = new NodePruner(pparser, debinarizer);
					tree = np.Prune(tree);
				}
				if (stripSubcat)
				{
					tree = subcategoryStripper.TransformTree(tree);
				}
				RestoreOriginalWords(tree);
				return tree;
			}
			else
			{
				if (pparser != null && pparser.HasParse() && fallbackToPCFG)
				{
					return GetBestPCFGParse();
				}
				else
				{
					if (dparser != null && dparser.HasParse())
					{
						// && fallbackToDG
						// Should we strip subcategories like this?  Traditionally haven't...
						// return subcategoryStripper.transformTree(getBestDependencyParse(true));
						return GetBestDependencyParse(true);
					}
					else
					{
						throw new NoSuchParseException();
					}
				}
			}
		}

		/// <summary>Return the k best parses of the sentence most recently parsed.</summary>
		/// <remarks>
		/// Return the k best parses of the sentence most recently parsed.
		/// NB: The dependency parser does not implement a k-best method
		/// and the factored parser's method seems to be broken and therefore
		/// this method always returns a list of size 1 if either of these
		/// two parsers was used.
		/// </remarks>
		/// <returns>A list of scored trees</returns>
		/// <exception cref="Edu.Stanford.Nlp.Parser.Common.NoSuchParseException">
		/// If no previously successfully parsed
		/// sentence
		/// </exception>
		public virtual IList<ScoredObject<Tree>> GetKBestParses(int k)
		{
			if (parseSkipped)
			{
				return null;
			}
			if (bparser != null && parseSucceeded)
			{
				//The getKGoodParses seems to be broken, so just return the best parse
				Tree binaryTree = bparser.GetBestParse();
				Tree tree = debinarizer.TransformTree(binaryTree);
				if (op.nodePrune)
				{
					NodePruner np = new NodePruner(pparser, debinarizer);
					tree = np.Prune(tree);
				}
				tree = subcategoryStripper.TransformTree(tree);
				RestoreOriginalWords(tree);
				double score = dparser.GetBestScore();
				ScoredObject<Tree> so = new ScoredObject<Tree>(tree, score);
				IList<ScoredObject<Tree>> trees = new List<ScoredObject<Tree>>(1);
				trees.Add(so);
				return trees;
			}
			else
			{
				if (pparser != null && pparser.HasParse() && fallbackToPCFG)
				{
					return this.GetKBestPCFGParses(k);
				}
				else
				{
					if (dparser != null && dparser.HasParse())
					{
						// && fallbackToDG
						// The dependency parser doesn't support k-best parse extraction, so just
						// return the best parse
						Tree tree = this.GetBestDependencyParse(true);
						double score = dparser.GetBestScore();
						ScoredObject<Tree> so = new ScoredObject<Tree>(tree, score);
						IList<ScoredObject<Tree>> trees = new List<ScoredObject<Tree>>(1);
						trees.Add(so);
						return trees;
					}
					else
					{
						throw new NoSuchParseException();
					}
				}
			}
		}

		/// <summary>
		/// Checks which parser (factored, PCFG, or dependency) was used and
		/// returns the score of the best parse from this parser.
		/// </summary>
		/// <remarks>
		/// Checks which parser (factored, PCFG, or dependency) was used and
		/// returns the score of the best parse from this parser.
		/// If no parse could be obtained, it returns Double.NEGATIVE_INFINITY.
		/// </remarks>
		/// <returns>the score of the best parse, or Double.NEGATIVE_INFINITY</returns>
		public virtual double GetBestScore()
		{
			if (parseSkipped)
			{
				return double.NegativeInfinity;
			}
			if (bparser != null && parseSucceeded)
			{
				return bparser.GetBestScore();
			}
			else
			{
				if (pparser != null && pparser.HasParse() && fallbackToPCFG)
				{
					return pparser.GetBestScore();
				}
				else
				{
					if (dparser != null && dparser.HasParse())
					{
						return dparser.GetBestScore();
					}
					else
					{
						return double.NegativeInfinity;
					}
				}
			}
		}

		public virtual IList<ScoredObject<Tree>> GetBestPCFGParses()
		{
			return pparser.GetBestParses();
		}

		public virtual bool HasFactoredParse()
		{
			if (bparser == null)
			{
				return false;
			}
			return !parseSkipped && parseSucceeded && bparser.HasParse();
		}

		public virtual Tree GetBestFactoredParse()
		{
			return bparser.GetBestParse();
		}

		public virtual IList<ScoredObject<Tree>> GetKGoodFactoredParses(int k)
		{
			if (bparser == null || parseSkipped)
			{
				return null;
			}
			IList<ScoredObject<Tree>> binaryTrees = bparser.GetKGoodParses(k);
			if (binaryTrees == null)
			{
				return null;
			}
			IList<ScoredObject<Tree>> trees = new List<ScoredObject<Tree>>(k);
			foreach (ScoredObject<Tree> tp in binaryTrees)
			{
				Tree t = debinarizer.TransformTree(tp.Object());
				if (op.nodePrune)
				{
					NodePruner np = new NodePruner(pparser, debinarizer);
					t = np.Prune(t);
				}
				t = subcategoryStripper.TransformTree(t);
				RestoreOriginalWords(t);
				trees.Add(new ScoredObject<Tree>(t, tp.Score()));
			}
			return trees;
		}

		/// <summary>
		/// Returns the trees (and scores) corresponding to the
		/// k-best derivations of the sentence.
		/// </summary>
		/// <remarks>
		/// Returns the trees (and scores) corresponding to the
		/// k-best derivations of the sentence.  This cannot be
		/// a Counter because frequently there will be multiple
		/// derivations which lead to the same parse tree.
		/// </remarks>
		/// <param name="k">The number of best parses to return</param>
		/// <returns>The list of trees with their scores (log prob).</returns>
		public virtual IList<ScoredObject<Tree>> GetKBestPCFGParses(int k)
		{
			if (pparser == null)
			{
				return null;
			}
			IList<ScoredObject<Tree>> binaryTrees = pparser.GetKBestParses(k);
			if (binaryTrees == null)
			{
				return null;
			}
			IList<ScoredObject<Tree>> trees = new List<ScoredObject<Tree>>(k);
			foreach (ScoredObject<Tree> p in binaryTrees)
			{
				Tree t = debinarizer.TransformTree(p.Object());
				t = subcategoryStripper.TransformTree(t);
				RestoreOriginalWords(t);
				trees.Add(new ScoredObject<Tree>(t, p.Score()));
			}
			return trees;
		}

		public virtual Tree GetBestPCFGParse()
		{
			return GetBestPCFGParse(true);
		}

		public virtual Tree GetBestPCFGParse(bool stripSubcategories)
		{
			if (pparser == null || parseSkipped || parseUnparsable)
			{
				return null;
			}
			Tree binaryTree = pparser.GetBestParse();
			if (binaryTree == null)
			{
				return null;
			}
			Tree t = debinarizer.TransformTree(binaryTree);
			if (stripSubcategories)
			{
				t = subcategoryStripper.TransformTree(t);
			}
			RestoreOriginalWords(t);
			return t;
		}

		public virtual double GetPCFGScore()
		{
			return pparser.GetBestScore();
		}

		internal virtual double GetPCFGScore(string goalStr)
		{
			return pparser.GetBestScore(goalStr);
		}

		internal virtual void ParsePCFG<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			parseSucceeded = false;
			parseNoMemory = false;
			parseUnparsable = false;
			parseSkipped = false;
			parseFallback = false;
			whatFailed = null;
			originalSentence = sentence;
			pparser.Parse(sentence);
		}

		public virtual Tree GetBestDependencyParse()
		{
			return GetBestDependencyParse(false);
		}

		public virtual Tree GetBestDependencyParse(bool debinarize)
		{
			if (dparser == null || parseSkipped || parseUnparsable)
			{
				return null;
			}
			Tree t = dparser.GetBestParse();
			if (t != null)
			{
				if (debinarize)
				{
					t = debinarizer.TransformTree(t);
				}
				t = boundaryRemover.TransformTree(t);
				// remove boundary .$$. which is otherwise still there from dparser.
				RestoreOriginalWords(t);
			}
			return t;
		}

		/// <summary>Parse a sentence represented as a List of tokens.</summary>
		/// <remarks>
		/// Parse a sentence represented as a List of tokens.
		/// The text must already have been tokenized and
		/// normalized into tokens that are appropriate to the treebank
		/// which was used to train the parser.  The tokens can be of
		/// multiple types, and the list items need not be homogeneous as to type
		/// (in particular, only some words might be given tags):
		/// <ul>
		/// <li>If a token implements HasWord, then the word to be parsed is
		/// given by its word() value.</li>
		/// <li>If a token implements HasTag and the tag() value is not
		/// null or the empty String, then the parser is strongly advised to assign
		/// a part of speech tag that <i>begins</i> with this String.</li>
		/// </ul>
		/// </remarks>
		/// <param name="sentence">The sentence to parse</param>
		/// <returns>
		/// true Iff the sentence was accepted by the grammar.  If
		/// the main grammar fails, but the PCFG succeeds, then
		/// this still returns true, but parseFallback() will
		/// also return true.  getBestParse() will have a valid
		/// result iff this returns true.
		/// </returns>
		public virtual bool Parse<_T0>(IList<_T0> sentence)
			where _T0 : IHasWord
		{
			try
			{
				if (!ParseInternal(sentence))
				{
					if (pparser != null && pparser.HasParse() && fallbackToPCFG)
					{
						parseFallback = true;
						return true;
					}
					else
					{
						parseUnparsable = true;
						return false;
					}
				}
				else
				{
					return true;
				}
			}
			catch (OutOfMemoryException e)
			{
				if (op.testOptions.maxLength != -unchecked((int)(0xDEADBEEF)))
				{
					// this means they explicitly asked for a length they cannot handle.
					// Throw exception.  Avoid string concatenation before throw it.
					log.Info("NOT ENOUGH MEMORY TO PARSE SENTENCES OF LENGTH ");
					log.Info(op.testOptions.maxLength);
					throw;
				}
				if (pparser.HasParse() && fallbackToPCFG)
				{
					try
					{
						whatFailed = "dependency";
						if (dparser.HasParse())
						{
							whatFailed = "factored";
						}
						parseFallback = true;
						return true;
					}
					catch (OutOfMemoryException oome)
					{
						Sharpen.Runtime.PrintStackTrace(oome);
						parseNoMemory = true;
						pparser.NudgeDownArraySize();
						return false;
					}
				}
				else
				{
					parseNoMemory = true;
					return false;
				}
			}
			catch (NotSupportedException)
			{
				parseSkipped = true;
				return false;
			}
		}

		/// <summary>
		/// Implements the same parsing with fallback that parse() does, but
		/// also outputs status messages for failed parses to pwErr.
		/// </summary>
		public virtual bool ParseAndReport<_T0>(IList<_T0> sentence, PrintWriter pwErr)
			where _T0 : IHasWord
		{
			bool result = Parse(sentence);
			if (result)
			{
				if (whatFailed != null)
				{
					// Something failed, probably because of memory problems.
					// However, we still got a PCFG parse, at least.
					if (!saidMemMessage)
					{
						ParserUtils.PrintOutOfMemory(pwErr);
						saidMemMessage = true;
					}
					pwErr.Println("Sentence too long for " + whatFailed + " parser.  Falling back to PCFG parse...");
				}
				else
				{
					if (parseFallback)
					{
						// We had to fall back for some other reason.
						pwErr.Println("Sentence couldn't be parsed by grammar.... falling back to PCFG parse.");
					}
				}
			}
			else
			{
				if (parseUnparsable)
				{
					// No parse at all, completely failed.
					pwErr.Println("Sentence couldn't be parsed by grammar.");
				}
				else
				{
					if (parseNoMemory)
					{
						// Ran out of memory, either with or without a possible PCFG parse.
						if (!saidMemMessage)
						{
							ParserUtils.PrintOutOfMemory(pwErr);
							saidMemMessage = true;
						}
						if (pparser.HasParse() && fallbackToPCFG)
						{
							pwErr.Println("No memory to gather PCFG parse. Skipping...");
						}
						else
						{
							pwErr.Println("Sentence has no parse using PCFG grammar (or no PCFG fallback).  Skipping...");
						}
					}
					else
					{
						if (parseSkipped)
						{
							pwErr.Println("Sentence too long (or zero words).");
						}
					}
				}
			}
			return result;
		}

		/// <summary>Return a TreePrint for formatting parsed output trees.</summary>
		/// <returns>A TreePrint for formatting parsed output trees.</returns>
		public virtual TreePrint GetTreePrint()
		{
			return op.testOptions.TreePrint(op.tlpParams);
		}

		public virtual IKBestViterbiParser GetPCFGParser()
		{
			return pparser;
		}

		public virtual IKBestViterbiParser GetDependencyParser()
		{
			return dparser;
		}

		public virtual IKBestViterbiParser GetFactoredParser()
		{
			return bparser;
		}

		/// <summary>Adds a sentence final punctuation mark to sentences that lack one.</summary>
		/// <remarks>
		/// Adds a sentence final punctuation mark to sentences that lack one.
		/// This method adds a period (the first sentence final punctuation word
		/// in a parser language pack) to sentences that don't have one within
		/// the last 3 words (to allow for close parentheses, etc.).  It checks
		/// tags for punctuation, if available, otherwise words.
		/// </remarks>
		/// <param name="sentence">The sentence to check</param>
		/// <param name="length">The length of the sentence (just to avoid recomputation)</param>
		private bool AddSentenceFinalPunctIfNeeded(IList<IHasWord> sentence, int length)
		{
			int start = length - 3;
			if (start < 0)
			{
				start = 0;
			}
			ITreebankLanguagePack tlp = op.tlpParams.TreebankLanguagePack();
			for (int i = length - 1; i >= start; i--)
			{
				IHasWord item = sentence[i];
				// An object (e.g., CoreLabel) can implement HasTag but not actually store
				// a tag so we need to check that there is something there for this case.
				// If there is, use only it, since word tokens can be ambiguous.
				string tag = null;
				if (item is IHasTag)
				{
					tag = ((IHasTag)item).Tag();
				}
				if (tag != null && !tag.IsEmpty())
				{
					if (tlp.IsSentenceFinalPunctuationTag(tag))
					{
						return false;
					}
				}
				else
				{
					string str = item.Word();
					if (tlp.IsPunctuationWord(str))
					{
						return false;
					}
				}
			}
			// none found so add one.
			if (op.testOptions.verbose)
			{
				log.Info("Adding missing final punctuation to sentence.");
			}
			string[] sfpWords = tlp.SentenceFinalPunctuationWords();
			if (sfpWords.Length > 0)
			{
				sentence.Add(new Word(sfpWords[0]));
			}
			return true;
		}
	}
}

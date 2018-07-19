using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Parser.Common;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Coref.MD
{
	/// <summary>Interface for finding coref mentions in a document.</summary>
	/// <author>Angel Chang</author>
	public abstract class CorefMentionFinder
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(CorefMentionFinder));

		protected internal Locale lang;

		protected internal IHeadFinder headFinder;

		protected internal IAnnotator parserProcessor;

		protected internal bool allowReparsing;

		protected internal static readonly TregexPattern npOrPrpMentionPattern = TregexPattern.Compile("/^(?:NP|PN|PRP)/");

		private const bool Verbose = false;

		/// <summary>Get all the predicted mentions for a document.</summary>
		/// <param name="doc">The syntactically annotated document</param>
		/// <param name="dict">Dictionaries for coref.</param>
		/// <returns>For each of the List of sentences in the document, a List of Mention objects</returns>
		public abstract IList<IList<Mention>> FindMentions(Annotation doc, Dictionaries dict, Properties props);

		protected internal static void ExtractPremarkedEntityMentions(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			SemanticGraph basicDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			SemanticGraph enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			if (enhancedDependency == null)
			{
				enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			}
			int beginIndex = -1;
			foreach (CoreLabel w in sent)
			{
				MultiTokenTag t = w.Get(typeof(CoreAnnotations.MentionTokenAnnotation));
				if (t != null)
				{
					// Part of a mention
					if (t.IsStart())
					{
						// Start of mention
						beginIndex = w.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					}
					if (t.IsEnd())
					{
						// end of mention
						int endIndex = w.Get(typeof(CoreAnnotations.IndexAnnotation));
						if (beginIndex >= 0)
						{
							IntPair mSpan = new IntPair(beginIndex, endIndex);
							int dummyMentionId = -1;
							Mention m = new Mention(dummyMentionId, beginIndex, endIndex, sent, basicDependency, enhancedDependency, new List<CoreLabel>(sent.SubList(beginIndex, endIndex)));
							mentions.Add(m);
							mentionSpanSet.Add(mSpan);
							beginIndex = -1;
						}
						else
						{
							Redwood.Log("Start of marked mention not found in sentence: " + t + " at tokenIndex=" + (w.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1) + " for " + s.Get(typeof(CoreAnnotations.TextAnnotation)));
						}
					}
				}
			}
		}

		/// <summary>Extract enumerations (A, B, and C)</summary>
		protected internal static readonly TregexPattern enumerationsMentionPattern = TregexPattern.Compile("NP < (/^(?:NP|NNP|NML)/=m1 $.. (/^CC|,/ $.. /^(?:NP|NNP|NML)/=m2))");

		protected internal static void ExtractEnumerations(ICoreMap s, IList<Mention> mentions, ICollection<IntPair> mentionSpanSet, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			SemanticGraph basicDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			SemanticGraph enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
			if (enhancedDependency == null)
			{
				enhancedDependency = s.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation));
			}
			TregexPattern tgrepPattern = enumerationsMentionPattern;
			TregexMatcher matcher = tgrepPattern.Matcher(tree);
			IDictionary<IntPair, Tree> spanToMentionSubTree = Generics.NewHashMap();
			while (matcher.Find())
			{
				matcher.GetMatch();
				Tree m1 = matcher.GetNode("m1");
				Tree m2 = matcher.GetNode("m2");
				IList<Tree> mLeaves = m1.GetLeaves();
				int beginIdx = ((CoreLabel)mLeaves[0].Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
				int endIdx = ((CoreLabel)mLeaves[mLeaves.Count - 1].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				spanToMentionSubTree[new IntPair(beginIdx, endIdx)] = m1;
				mLeaves = m2.GetLeaves();
				beginIdx = ((CoreLabel)mLeaves[0].Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
				endIdx = ((CoreLabel)mLeaves[mLeaves.Count - 1].Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				spanToMentionSubTree[new IntPair(beginIdx, endIdx)] = m2;
			}
			foreach (KeyValuePair<IntPair, Tree> spanMention in spanToMentionSubTree)
			{
				IntPair span = spanMention.Key;
				if (!mentionSpanSet.Contains(span) && !InsideNE(span, namedEntitySpanSet))
				{
					int dummyMentionId = -1;
					Mention m = new Mention(dummyMentionId, span.Get(0), span.Get(1), sent, basicDependency, enhancedDependency, new List<CoreLabel>(sent.SubList(span.Get(0), span.Get(1))), spanMention.Value);
					mentions.Add(m);
					mentionSpanSet.Add(span);
				}
			}
		}

		/// <summary>Check whether a mention is inside of a named entity</summary>
		protected internal static bool InsideNE(IntPair mSpan, ICollection<IntPair> namedEntitySpanSet)
		{
			foreach (IntPair span in namedEntitySpanSet)
			{
				if (span.Get(0) <= mSpan.Get(0) && mSpan.Get(1) <= span.Get(1))
				{
					return true;
				}
			}
			return false;
		}

		public static bool InStopList(Mention m)
		{
			string mentionSpan = m.SpanToString().ToLower(Locale.English);
			if (mentionSpan.Equals("u.s.") || mentionSpan.Equals("u.k.") || mentionSpan.Equals("u.s.s.r"))
			{
				return true;
			}
			if (mentionSpan.Equals("there") || mentionSpan.StartsWith("etc.") || mentionSpan.Equals("ltd."))
			{
				return true;
			}
			if (mentionSpan.StartsWith("'s "))
			{
				return true;
			}
			//    if (mentionSpan.endsWith("etc.")) return true;
			return false;
		}

		protected internal virtual void RemoveSpuriousMentions(Annotation doc, IList<IList<Mention>> predictedMentions, Dictionaries dict, bool removeNested, Locale lang)
		{
			if (lang == Locale.English)
			{
				RemoveSpuriousMentionsEn(doc, predictedMentions, dict);
			}
			else
			{
				if (lang == Locale.Chinese)
				{
					RemoveSpuriousMentionsZh(doc, predictedMentions, dict, removeNested);
				}
			}
		}

		protected internal virtual void RemoveSpuriousMentionsEn(Annotation doc, IList<IList<Mention>> predictedMentions, Dictionaries dict)
		{
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < predictedMentions.Count; i++)
			{
				ICoreMap s = sentences[i];
				IList<Mention> mentions = predictedMentions[i];
				IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
				ICollection<Mention> remove = Generics.NewHashSet();
				foreach (Mention m in mentions)
				{
					string headPOS = m.headWord.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
					// non word such as 'hmm'
					if (dict.nonWords.Contains(m.headString))
					{
						remove.Add(m);
					}
					// adjective form of nations
					// the [American] policy -> not mention
					// speak in [Japanese] -> mention
					// check if the mention is noun and the next word is not noun
					if (dict.IsAdjectivalDemonym(m.SpanToString()))
					{
						if (!headPOS.StartsWith("N") || (m.endIndex < sent.Count && sent[m.endIndex].Tag().StartsWith("N")))
						{
							remove.Add(m);
						}
					}
					// stop list (e.g., U.S., there)
					if (InStopList(m))
					{
						remove.Add(m);
					}
				}
				mentions.RemoveAll(remove);
			}
		}

		protected internal virtual void RemoveSpuriousMentionsZh(Annotation doc, IList<IList<Mention>> predictedMentions, Dictionaries dict, bool removeNested)
		{
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// this goes through each sentence -- predictedMentions has a list for each sentence
			for (int i = 0; i < sz; i++)
			{
				IList<Mention> mentions = predictedMentions[i];
				IList<CoreLabel> sent = sentences[i].Get(typeof(CoreAnnotations.TokensAnnotation));
				ICollection<Mention> remove = Generics.NewHashSet();
				foreach (Mention m in mentions)
				{
					if (m.headWord.Ner().Matches("PERCENT|MONEY|QUANTITY|CARDINAL"))
					{
						remove.Add(m);
					}
					else
					{
						if (m.originalSpan.Count == 1 && m.headWord.Tag().Equals("CD"))
						{
							remove.Add(m);
						}
						else
						{
							if (dict.removeWords.Contains(m.SpanToString()))
							{
								remove.Add(m);
							}
							else
							{
								if (MentionContainsRemoveChars(m, dict.removeChars))
								{
									remove.Add(m);
								}
								else
								{
									if (m.headWord.Tag().Equals("PU"))
									{
										// punctuation-only mentions
										remove.Add(m);
									}
									else
									{
										if (MentionIsDemonym(m, dict.countries))
										{
											// demonyms -- this seems to be a no-op on devset. Maybe not working?
											remove.Add(m);
										}
										else
										{
											if (m.SpanToString().Equals("问题") && m.startIndex > 0 && sent[m.startIndex - 1].Word().EndsWith("没"))
											{
												// 没 问题 - this is maybe okay but having 问题 on removeWords was dangerous
												remove.Add(m);
											}
											else
											{
												if (MentionIsRangren(m, sent))
												{
													remove.Add(m);
												}
												else
												{
													if (m.SpanToString().Equals("你") && m.startIndex < sent.Count - 1 && sent[m.startIndex + 1].Word().StartsWith("知道"))
													{
														// 你 知道
														remove.Add(m);
													}
													else
													{
														// The words that used to be in this case are now handled more generallyin removeCharsZh
														// } else if (m.spanToString().contains("什么") || m.spanToString().contains("多少")) {
														//   remove.add(m);
														//   if (VERBOSE) log.info("MENTION FILTERING Removed many/few mention ending: " + m.spanToString());
														if (m.SpanToString().EndsWith("的"))
														{
															remove.Add(m);
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
				// omit this case, it decreases performance. A few useful interrogative pronouns are now in the removeChars list
				// } else if (mentionIsInterrogativePronoun(m, dict.interrogativePronouns)) {
				//     remove.add(m);
				//     if (VERBOSE) log.info("MENTION FILTERING Removed interrogative pronoun: " + m.spanToString());
				// 的 handling
				//        if(m.startIndex>0 && sent.get(m.startIndex-1).word().equals("的")) {
				//          // remove.add(m);
				//          Tree t = sentences.get(i).get(TreeAnnotation.class);
				//          Tree mTree = m.mentionSubTree;
				//          if(mTree==null) continue;
				//          for(Tree p : t.pathNodeToNode(mTree, t)) {
				//            if(mTree==p) continue;
				//            if(p.value().equals("NP")) {
				//              remove.add(m);
				//            }
				//          }
				//        }
				// for each mention
				// nested mention with shared headword (except apposition, enumeration): pick larger one
				if (removeNested)
				{
					foreach (Mention m1 in mentions)
					{
						foreach (Mention m2 in mentions)
						{
							if (m1 == m2 || remove.Contains(m1) || remove.Contains(m2))
							{
								continue;
							}
							if (m1.sentNum == m2.sentNum && m1.headWord == m2.headWord && m2.InsideIn(m1))
							{
								if (m2.endIndex < sent.Count && (sent[m2.endIndex].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals(",") || sent[m2.endIndex].Get(typeof(CoreAnnotations.PartOfSpeechAnnotation)).Equals("CC")))
								{
									continue;
								}
								remove.Add(m2);
							}
						}
					}
				}
				mentions.RemoveAll(remove);
			}
		}

		// for each sentence
		private static bool MentionContainsRemoveChars(Mention m, ICollection<string> removeChars)
		{
			string spanString = m.SpanToString();
			foreach (string ch in removeChars)
			{
				if (spanString.Contains(ch))
				{
					return true;
				}
			}
			return false;
		}

		private static bool MentionIsDemonym(Mention m, ICollection<string> countries)
		{
			string lastWord = m.originalSpan[m.originalSpan.Count - 1].Word();
			return lastWord.Length > 0 && m.SpanToString().EndsWith("人") && countries.Contains(Sharpen.Runtime.Substring(lastWord, 0, lastWord.Length - 1));
		}

		private static bool MentionIsRangren(Mention m, IList<CoreLabel> sent)
		{
			if (m.SpanToString().Equals("人") && m.startIndex > 0)
			{
				string priorWord = sent[m.startIndex - 1].Word();
				// cdm [2016]: This test matches everything because of the 3rd clause! That can't be right!
				if (priorWord.EndsWith("让") || priorWord.EndsWith("令") || priorWord.EndsWith(string.Empty))
				{
					return true;
				}
			}
			return false;
		}

		private static bool MentionIsInterrogativePronoun(Mention m, ICollection<string> interrogatives)
		{
			// handling interrogative pronouns
			foreach (CoreLabel cl in m.originalSpan)
			{
				// if (dict.interrogativePronouns.contains(m.spanToString())) remove.add(m);
				if (interrogatives.Contains(cl.Word()))
				{
					return true;
				}
			}
			return false;
		}

		// extract mentions which have same string as another stand-alone mention
		protected internal static void ExtractNamedEntityModifiers(IList<ICoreMap> sentences, IList<ICollection<IntPair>> mentionSpanSetList, IList<IList<Mention>> predictedMentions, ICollection<string> neStrings)
		{
			for (int i = 0; i < sz; i++)
			{
				IList<Mention> mentions = predictedMentions[i];
				ICoreMap sent = sentences[i];
				IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
				ICollection<IntPair> mentionSpanSet = mentionSpanSetList[i];
				for (int j = 0; j < tSize; j++)
				{
					foreach (string ne in neStrings)
					{
						int len = ne.Split(" ").Length;
						if (j + len > tokens.Count)
						{
							continue;
						}
						StringBuilder sb = new StringBuilder();
						for (int k = 0; k < len; k++)
						{
							sb.Append(tokens[k + j].Word()).Append(" ");
						}
						string phrase = sb.ToString().Trim();
						int beginIndex = j;
						int endIndex = j + len;
						// include "'s" if it belongs to this named entity
						if (endIndex < tokens.Count && tokens[endIndex].Word().Equals("'s") && tokens[endIndex].Tag().Equals("POS"))
						{
							Tree tree = sent.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
							Tree sToken = tree.GetLeaves()[beginIndex];
							Tree eToken = tree.GetLeaves()[endIndex];
							Tree join = tree.JoinNode(sToken, eToken);
							Tree sJoin = join.GetLeaves()[0];
							Tree eJoin = join.GetLeaves()[join.GetLeaves().Count - 1];
							if (sToken == sJoin && eToken == eJoin)
							{
								endIndex++;
							}
						}
						// include DT if it belongs to this named entity
						if (beginIndex > 0 && tokens[beginIndex - 1].Tag().Equals("DT"))
						{
							Tree tree = sent.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
							Tree sToken = tree.GetLeaves()[beginIndex - 1];
							Tree eToken = tree.GetLeaves()[endIndex - 1];
							Tree join = tree.JoinNode(sToken, eToken);
							Tree sJoin = join.GetLeaves()[0];
							Tree eJoin = join.GetLeaves()[join.GetLeaves().Count - 1];
							if (sToken == sJoin && eToken == eJoin)
							{
								beginIndex--;
							}
						}
						IntPair span = new IntPair(beginIndex, endIndex);
						if (Sharpen.Runtime.EqualsIgnoreCase(phrase, ne) && !mentionSpanSet.Contains(span))
						{
							int dummyMentionId = -1;
							Mention m = new Mention(dummyMentionId, beginIndex, endIndex, tokens, sent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)), sent.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)) != null ? sent.Get
								(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)) : sent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)), new List<CoreLabel>(tokens.SubList(beginIndex, endIndex)));
							mentions.Add(m);
							mentionSpanSet.Add(span);
						}
					}
				}
			}
		}

		protected internal static void AddNamedEntityStrings(ICoreMap s, ICollection<string> neStrings, ICollection<IntPair> namedEntitySpanSet)
		{
			IList<CoreLabel> tokens = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			foreach (IntPair p in namedEntitySpanSet)
			{
				StringBuilder sb = new StringBuilder();
				for (int idx = p.Get(0); idx < p.Get(1); idx++)
				{
					sb.Append(tokens[idx].Word()).Append(" ");
				}
				string str = sb.ToString().Trim();
				if (str.EndsWith(" 's"))
				{
					str = Sharpen.Runtime.Substring(str, 0, str.Length - 3);
				}
				neStrings.Add(str);
			}
		}

		// temporary for debug
		protected internal static void AddGoldMentions(IList<ICoreMap> sentences, IList<ICollection<IntPair>> mentionSpanSetList, IList<IList<Mention>> predictedMentions, IList<IList<Mention>> allGoldMentions)
		{
			for (int i = 0; i < sz; i++)
			{
				IList<Mention> mentions = predictedMentions[i];
				ICoreMap sent = sentences[i];
				IList<CoreLabel> tokens = sent.Get(typeof(CoreAnnotations.TokensAnnotation));
				ICollection<IntPair> mentionSpanSet = mentionSpanSetList[i];
				IList<Mention> golds = allGoldMentions[i];
				foreach (Mention g in golds)
				{
					IntPair pair = new IntPair(g.startIndex, g.endIndex);
					if (!mentionSpanSet.Contains(pair))
					{
						int dummyMentionId = -1;
						Mention m = new Mention(dummyMentionId, g.startIndex, g.endIndex, tokens, sent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)), sent.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)) != null ? sent
							.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation)) : sent.Get(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation)), new List<CoreLabel>(tokens.SubList(g.startIndex, g.endIndex)));
						mentions.Add(m);
						mentionSpanSet.Add(pair);
					}
				}
			}
		}

		public virtual void FindHead(ICoreMap s, IList<Mention> mentions)
		{
			Tree tree = s.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
			IList<CoreLabel> sent = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			tree.IndexSpans(0);
			foreach (Mention m in mentions)
			{
				if (lang == Locale.Chinese)
				{
					FindHeadChinese(sent, m);
				}
				else
				{
					CoreLabel head = (CoreLabel)FindSyntacticHead(m, tree, sent).Label();
					m.headIndex = head.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					m.headWord = sent[m.headIndex];
					m.headString = m.headWord.Get(typeof(CoreAnnotations.TextAnnotation)).ToLower(Locale.English);
				}
				int start = m.headIndex - m.startIndex;
				if (start < 0 || start >= m.originalSpan.Count)
				{
					Redwood.Log("Invalid index for head " + start + "=" + m.headIndex + "-" + m.startIndex + ": originalSpan=[" + StringUtils.JoinWords(m.originalSpan, " ") + "], head=" + m.headWord);
					Redwood.Log("Setting head string to entire mention");
					m.headIndex = m.startIndex;
					m.headWord = m.originalSpan.Count > 0 ? m.originalSpan[0] : sent[m.startIndex];
					m.headString = m.originalSpan.ToString();
				}
			}
		}

		protected internal static void FindHeadChinese(IList<CoreLabel> sent, Mention m)
		{
			int headPos = m.endIndex - 1;
			// Skip trailing punctuations
			while (headPos > m.startIndex && sent[headPos].Tag().Equals("PU"))
			{
				headPos--;
			}
			// If we got right to the end without finding non punctuation, reset to end again
			if (headPos == m.startIndex && sent[headPos].Tag().Equals("PU"))
			{
				headPos = m.endIndex - 1;
			}
			if (sent[headPos].OriginalText().Equals("自己") && m.endIndex != m.startIndex && headPos > m.startIndex)
			{
				if (!sent[headPos - 1].Tag().Equals("PU"))
				{
					headPos--;
				}
			}
			m.headIndex = headPos;
			m.headWord = sent[headPos];
			m.headString = m.headWord.Get(typeof(CoreAnnotations.TextAnnotation));
		}

		public virtual Tree FindSyntacticHead(Mention m, Tree root, IList<CoreLabel> tokens)
		{
			// mention ends with 's
			int endIdx = m.endIndex;
			if (m.originalSpan.Count > 0)
			{
				string lastWord = m.originalSpan[m.originalSpan.Count - 1].Get(typeof(CoreAnnotations.TextAnnotation));
				if ((lastWord.Equals("'s") || lastWord.Equals("'")) && m.originalSpan.Count != 1)
				{
					endIdx--;
				}
			}
			Tree exactMatch = FindTreeWithSpan(root, m.startIndex, endIdx);
			//
			// found an exact match
			//
			if (exactMatch != null)
			{
				return SafeHead(exactMatch, endIdx);
			}
			// no exact match found
			// in this case, we parse the actual extent of the mention, embedded in a sentence
			// context, so as to make the parser work better :-)
			if (allowReparsing)
			{
				int approximateness = 0;
				IList<CoreLabel> extentTokens = new List<CoreLabel>();
				extentTokens.Add(InitCoreLabel("It", "PRP"));
				extentTokens.Add(InitCoreLabel("was", "VBD"));
				int AddedWords = 2;
				for (int i = m.startIndex; i < endIdx; i++)
				{
					// Add everything except separated dashes! The separated dashes mess with the parser too badly.
					CoreLabel label = tokens[i];
					if (!"-".Equals(label.Word()))
					{
						extentTokens.Add(tokens[i]);
					}
					else
					{
						approximateness++;
					}
				}
				extentTokens.Add(InitCoreLabel(".", "."));
				// constrain the parse to the part we're interested in.
				// Starting from ADDED_WORDS comes from skipping "It was".
				// -1 to exclude the period.
				// We now let it be any kind of nominal constituent, since there
				// are VP and S ones
				ParserConstraint constraint = new ParserConstraint(AddedWords, extentTokens.Count - 1, Pattern.Compile(".*"));
				IList<ParserConstraint> constraints = Java.Util.Collections.SingletonList(constraint);
				Tree tree = Parse(extentTokens, constraints);
				ConvertToCoreLabels(tree);
				// now unnecessary, as parser uses CoreLabels?
				tree.IndexSpans(m.startIndex - AddedWords);
				// remember it has ADDED_WORDS extra words at the beginning
				Tree subtree = FindPartialSpan(tree, m.startIndex);
				// There was a possible problem that with a crazy parse, extentHead could be one of the added words, not a real word!
				// Now we make sure in findPartialSpan that it can't be before the real start, and in safeHead, we disallow something
				// passed the right end (that is, just that final period).
				Tree extentHead = SafeHead(subtree, endIdx);
				System.Diagnostics.Debug.Assert((extentHead != null));
				// extentHead is a child in the local extent parse tree. we need to find the corresponding node in the main tree
				// Because we deleted dashes, it's index will be >= the index in the extent parse tree
				CoreLabel l = (CoreLabel)extentHead.Label();
				Tree realHead = FunkyFindLeafWithApproximateSpan(root, l.Value(), l.Get(typeof(CoreAnnotations.BeginIndexAnnotation)), approximateness);
				System.Diagnostics.Debug.Assert((realHead != null));
				return realHead;
			}
			// If reparsing wasn't allowed, try to find a span in the tree
			// which happens to have the head
			Tree wordMatch = FindTreeWithSmallestSpan(root, m.startIndex, endIdx);
			if (wordMatch != null)
			{
				Tree head = SafeHead(wordMatch, endIdx);
				if (head != null)
				{
					int index = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
					if (index >= m.startIndex && index < endIdx)
					{
						return head;
					}
				}
			}
			// If that didn't work, guess that it's the last word
			int lastNounIdx = endIdx - 1;
			for (int i_1 = m.startIndex; i_1 < m.endIndex; i_1++)
			{
				if (tokens[i_1].Tag().StartsWith("N"))
				{
					lastNounIdx = i_1;
				}
				else
				{
					if (tokens[i_1].Tag().StartsWith("W"))
					{
						break;
					}
				}
			}
			IList<Tree> leaves = root.GetLeaves();
			Tree endLeaf = leaves[lastNounIdx];
			return endLeaf;
		}

		/// <summary>Find the tree that covers the portion of interest.</summary>
		private static Tree FindPartialSpan(Tree root, int start)
		{
			CoreLabel label = (CoreLabel)root.Label();
			int startIndex = label.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
			if (startIndex == start)
			{
				return root;
			}
			foreach (Tree kid in root.Children())
			{
				CoreLabel kidLabel = (CoreLabel)kid.Label();
				int kidStart = kidLabel.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
				int kidEnd = kidLabel.Get(typeof(CoreAnnotations.EndIndexAnnotation));
				if (kidStart <= start && kidEnd > start)
				{
					return FindPartialSpan(kid, start);
				}
			}
			throw new Exception("Shouldn't happen: " + start + " " + root);
		}

		private static Tree FunkyFindLeafWithApproximateSpan(Tree root, string token, int index, int approximateness)
		{
			// log.info("Searching " + root + "\n  for " + token + " at position " + index + " (plus up to " + approximateness + ")");
			IList<Tree> leaves = root.GetLeaves();
			foreach (Tree leaf in leaves)
			{
				CoreLabel label = typeof(CoreLabel).Cast(leaf.Label());
				int indexInteger = label.Get(typeof(CoreAnnotations.IndexAnnotation));
				if (indexInteger == null)
				{
					continue;
				}
				int ind = indexInteger - 1;
				if (token.Equals(leaf.Value()) && ind >= index && ind <= index + approximateness)
				{
					return leaf;
				}
			}
			// this shouldn't happen
			//    throw new RuntimeException("RuleBasedCorefMentionFinder: ERROR: Failed to find head token");
			Redwood.Log("RuleBasedCorefMentionFinder: Failed to find head token:\n" + "Tree is: " + root + "\n" + "token = |" + token + "|" + index + "|, approx=" + approximateness);
			foreach (Tree leaf_1 in leaves)
			{
				if (token.Equals(leaf_1.Value()))
				{
					// log.info("Found it at position " + ind + "; returning " + leaf);
					return leaf_1;
				}
			}
			int fallback = Math.Max(0, leaves.Count - 2);
			Redwood.Log("RuleBasedCorefMentionFinder: Last resort: returning as head: " + leaves[fallback]);
			return leaves[fallback];
		}

		// last except for the added period.
		private static CoreLabel InitCoreLabel(string token, string posTag)
		{
			CoreLabel label = new CoreLabel();
			label.Set(typeof(CoreAnnotations.TextAnnotation), token);
			label.Set(typeof(CoreAnnotations.ValueAnnotation), token);
			label.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), posTag);
			return label;
		}

		private Tree Parse(IList<CoreLabel> tokens)
		{
			return Parse(tokens, null);
		}

		private Tree Parse(IList<CoreLabel> tokens, IList<ParserConstraint> constraints)
		{
			ICoreMap sent = new Annotation(string.Empty);
			sent.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			sent.Set(typeof(ParserAnnotations.ConstraintAnnotation), constraints);
			Annotation doc = new Annotation(string.Empty);
			IList<ICoreMap> sents = new List<ICoreMap>(1);
			sents.Add(sent);
			doc.Set(typeof(CoreAnnotations.SentencesAnnotation), sents);
			GetParser().Annotate(doc);
			sents = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sents[0].Get(typeof(TreeCoreAnnotations.TreeAnnotation));
		}

		private IAnnotator GetParser()
		{
			if (parserProcessor == null)
			{
				parserProcessor = StanfordCoreNLP.GetExistingAnnotator("parse");
				if (parserProcessor == null)
				{
					Properties emptyProperties = new Properties();
					parserProcessor = new ParserAnnotator("coref.parse.md", emptyProperties);
				}
				System.Diagnostics.Debug.Assert((parserProcessor != null));
			}
			return parserProcessor;
		}

		// This probably isn't needed now; everything is always a core label. But no-op.
		private static void ConvertToCoreLabels(Tree tree)
		{
			ILabel l = tree.Label();
			if (!(l is CoreLabel))
			{
				CoreLabel cl = new CoreLabel();
				cl.SetValue(l.Value());
				tree.SetLabel(cl);
			}
			foreach (Tree kid in tree.Children())
			{
				ConvertToCoreLabels(kid);
			}
		}

		private Tree SafeHead(Tree top, int endIndex)
		{
			// The trees passed in do not have the CoordinationTransformer
			// applied, but that just means the SemanticHeadFinder results are
			// slightly worse.
			Tree head = top.HeadTerminal(headFinder);
			// One obscure failure case is that the added period becomes the head. Disallow this.
			if (head != null)
			{
				int headIndexInteger = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				if (headIndexInteger != null)
				{
					int headIndex = headIndexInteger - 1;
					if (headIndex < endIndex)
					{
						return head;
					}
				}
			}
			// if no head found return the right-most leaf
			IList<Tree> leaves = top.GetLeaves();
			int candidate = leaves.Count - 1;
			while (candidate >= 0)
			{
				head = leaves[candidate];
				int headIndexInteger = ((CoreLabel)head.Label()).Get(typeof(CoreAnnotations.IndexAnnotation));
				if (headIndexInteger != null)
				{
					int headIndex = headIndexInteger - 1;
					if (headIndex < endIndex)
					{
						return head;
					}
				}
				candidate--;
			}
			// fallback: return top
			return top;
		}

		internal static Tree FindTreeWithSmallestSpan(Tree tree, int start, int end)
		{
			IList<Tree> leaves = tree.GetLeaves();
			Tree startLeaf = leaves[start];
			Tree endLeaf = leaves[end - 1];
			return Edu.Stanford.Nlp.Trees.Trees.GetLowestCommonAncestor(Arrays.AsList(startLeaf, endLeaf), tree);
		}

		private static Tree FindTreeWithSpan(Tree tree, int start, int end)
		{
			CoreLabel l = (CoreLabel)tree.Label();
			if (l != null && l.ContainsKey(typeof(CoreAnnotations.BeginIndexAnnotation)) && l.ContainsKey(typeof(CoreAnnotations.EndIndexAnnotation)))
			{
				int myStart = l.Get(typeof(CoreAnnotations.BeginIndexAnnotation));
				int myEnd = l.Get(typeof(CoreAnnotations.EndIndexAnnotation));
				if (start == myStart && end == myEnd)
				{
					// found perfect match
					return tree;
				}
				else
				{
					if (end < myStart)
					{
						return null;
					}
					else
					{
						if (start >= myEnd)
						{
							return null;
						}
					}
				}
			}
			// otherwise, check inside children - a match is possible
			foreach (Tree kid in tree.Children())
			{
				if (kid == null)
				{
					continue;
				}
				Tree ret = FindTreeWithSpan(kid, start, end);
				// found matching child
				if (ret != null)
				{
					return ret;
				}
			}
			// no match
			return null;
		}

		public static bool PartitiveRule(Mention m, IList<CoreLabel> sent, Dictionaries dict)
		{
			return m.startIndex >= 2 && Sharpen.Runtime.EqualsIgnoreCase(sent[m.startIndex - 1].Get(typeof(CoreAnnotations.TextAnnotation)), "of") && dict.parts.Contains(sent[m.startIndex - 2].Get(typeof(CoreAnnotations.TextAnnotation)).ToLower(Locale.English
				));
		}

		/// <summary>Check whether pleonastic 'it'.</summary>
		/// <remarks>Check whether pleonastic 'it'. E.g., It is possible that ...</remarks>
		private static readonly TregexPattern[] pleonasticPatterns = GetPleonasticPatterns();

		public static bool IsPleonastic(Mention m, Tree tree)
		{
			if (!Sharpen.Runtime.EqualsIgnoreCase(m.SpanToString(), "it"))
			{
				return false;
			}
			foreach (TregexPattern p in pleonasticPatterns)
			{
				if (CheckPleonastic(m, tree, p))
				{
					// SieveCoreferenceSystem.logger.fine("RuleBasedCorefMentionFinder: matched pleonastic pattern '" + p + "' for " + tree);
					return true;
				}
			}
			return false;
		}

		public static bool IsPleonasticDebug(Mention m, Tree tree, StringBuilder sbLog)
		{
			if (!Sharpen.Runtime.EqualsIgnoreCase(m.SpanToString(), "it"))
			{
				return false;
			}
			bool isPleonastic = false;
			int patternIdx = -1;
			int matchedPattern = -1;
			foreach (TregexPattern p in pleonasticPatterns)
			{
				patternIdx++;
				if (CheckPleonastic(m, tree, p))
				{
					// SieveCoreferenceSystem.logger.fine("RuleBasedCorefMentionFinder: matched pleonastic pattern '" + p + "' for " + tree);
					isPleonastic = true;
					matchedPattern = patternIdx;
				}
			}
			sbLog.Append("PLEONASTIC IT: mention ID: " + m.mentionID + "\thastwin: " + m.hasTwin + "\tpleonastic it? " + isPleonastic + "\tcorrect? " + (m.hasTwin != isPleonastic) + "\tmatched pattern: " + matchedPattern + "\n");
			sbLog.Append(m.contextParseTree.PennString()).Append("\n");
			sbLog.Append("PLEONASTIC IT END\n");
			return isPleonastic;
		}

		private static TregexPattern[] GetPleonasticPatterns()
		{
			string[] patterns = new string[] { "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@VP < (VBN $.. @S|SBAR))))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (ADJP $.. (/S|SBAR/))))"
				, "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (ADJP < (/S|SBAR/))))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (NP < /S|SBAR/)))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (NP $.. ADVP $.. /S|SBAR/)))"
				, "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (VP < (VBN $.. /S|SBAR/))))))", "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (ADJP $.. (/S|SBAR/))))))", "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (ADJP < (/S|SBAR/))))))"
				, "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (NP < /S|SBAR/)))))", "NP < (PRP=m1) $.. (VP < (MD $.. (VP < ((/^V.*/ < /^(?:be|become)/) $.. (NP $.. ADVP $.. /S|SBAR/)))))", "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:seems|appears|means|follows)/) $.. /S|SBAR/))"
				, "NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:turns|turned)/) $.. PRT $.. /S|SBAR/))" };
			// cdm 2013: I spent a while on these patterns. I fixed a syntax error in five patterns ($.. split with space), so it now shouldn't exception in checkPleonastic. This gave 0.02% on CoNLL11 dev
			// I tried some more precise patterns but they didn't help. Indeed, they tended to hurt vs. the higher recall patterns.
			//"NP < (PRP=m1) $.. (VP < ((/^V.*/ < /^(?:is|was|become|became)/) $.. (VP < (VBN $.. /S|SBAR/))))", // overmatches
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@VP < (VBN < expected|hoped $.. @SBAR))))",  // this one seems more accurate, but ...
			// in practice, go with this one (best results)
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@ADJP < (/^(?:JJ|VB)/ < /^(?i:(?:hard|tough|easi)(?:er|est)?|(?:im|un)?(?:possible|interesting|worthwhile|likely|surprising|certain)|disappointing|pointless|easy|fine|okay)$/) [ < @S|SBAR | $.. (@S|SBAR !< (IN !< for|For|FOR|that|That|THAT)) ] )))", // does worse than above 2 on CoNLL11 dev
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (/^V.*/ < /^(?i:is|was|be|becomes|become|became)$/ $.. (@NP $.. @ADVP $.. @SBAR)))", // cleft examples, generalized to not need ADVP; but gave worse CoNLL12 dev numbers....
			// these next 5 had buggy space in "$ ..", which I fixed
			// extraposed. OK 1/2 correct; need non-adverbial case
			// OK: 3/3 good matches on dev; but 3/4 wrong on WSJ
			// certain can be either but relatively likely pleonastic with it ... be
			// "@NP < (PRP=m1 < it|IT|It) $.. (@VP < (MD $.. (@VP < ((/^V.*/ < /^(?:be|become)/) $.. (@ADJP < (/^JJ/ < /^(?i:(?:hard|tough|easi)(?:er|est)?|(?:im|un)?(?:possible|interesting|worthwhile|likely|surprising|certain)|disappointing|pointless|easy|fine|okay))$/) [ < @S|SBAR | $.. (@S|SBAR !< (IN !< for|For|FOR|that|That|THAT)) ] )))))", // GOOD REPLACEMENT ; 2nd clause is for extraposed ones
			TregexPattern[] tgrepPatterns = new TregexPattern[patterns.Length];
			for (int i = 0; i < tgrepPatterns.Length; i++)
			{
				tgrepPatterns[i] = TregexPattern.Compile(patterns[i]);
			}
			return tgrepPatterns;
		}

		private static bool CheckPleonastic(Mention m, Tree tree, TregexPattern tgrepPattern)
		{
			try
			{
				TregexMatcher matcher = tgrepPattern.Matcher(tree);
				while (matcher.Find())
				{
					Tree np1 = matcher.GetNode("m1");
					if (((CoreLabel)np1.Label()).Get(typeof(CoreAnnotations.BeginIndexAnnotation)) + 1 == m.headWord.Get(typeof(CoreAnnotations.IndexAnnotation)))
					{
						return true;
					}
				}
			}
			catch (Exception e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
			}
			return false;
		}
	}
}

//
// StanfordCoreNLP -- a suite of NLP tools
// Copyright (c) 2009-2010 The Board of Trustees of
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
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>
	/// Extracts
	/// <literal><COREF></literal>
	/// mentions from a file annotated in MUC format.
	/// </summary>
	/// <author>Jenny Finkel</author>
	/// <author>Mihai Surdeanu</author>
	/// <author>Karthik Raghunathan</author>
	public class MUCMentionExtractor : MentionExtractor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Dcoref.MUCMentionExtractor));

		private readonly ITokenizerFactory<CoreLabel> tokenizerFactory;

		private readonly string fileContents;

		private int currentOffset;

		/// <exception cref="System.Exception"/>
		public MUCMentionExtractor(Dictionaries dict, Properties props, Semantics semantics)
			: base(dict, semantics)
		{
			string fileName = props.GetProperty(Constants.MucProp);
			fileContents = IOUtils.SlurpFile(fileName);
			currentOffset = 0;
			tokenizerFactory = PTBTokenizer.Factory(new CoreLabelTokenFactory(false), string.Empty);
			stanfordProcessor = LoadStanfordProcessor(props);
		}

		/// <exception cref="System.Exception"/>
		public MUCMentionExtractor(Dictionaries dict, Properties props, Semantics semantics, LogisticClassifier<string, string> singletonModel)
			: this(dict, props, semantics)
		{
			singletonPredictor = singletonModel;
		}

		public override void ResetDocs()
		{
			base.ResetDocs();
			currentOffset = 0;
		}

		/// <exception cref="System.Exception"/>
		public override Document NextDoc()
		{
			IList<IList<CoreLabel>> allWords = new List<IList<CoreLabel>>();
			IList<Tree> allTrees = new List<Tree>();
			IList<IList<Mention>> allGoldMentions = new List<IList<Mention>>();
			IList<IList<Mention>> allPredictedMentions;
			IList<ICoreMap> allSentences = new List<ICoreMap>();
			Annotation docAnno = new Annotation(string.Empty);
			Pattern docPattern = Pattern.Compile("<DOC>(.*?)</DOC>", Pattern.Dotall + Pattern.CaseInsensitive);
			Pattern sentencePattern = Pattern.Compile("(<s>|<hl>|<dd>|<DATELINE>)(.*?)(</s>|</hl>|</dd>|</DATELINE>)", Pattern.Dotall + Pattern.CaseInsensitive);
			Matcher docMatcher = docPattern.Matcher(fileContents);
			if (!docMatcher.Find(currentOffset))
			{
				return null;
			}
			currentOffset = docMatcher.End();
			string doc = docMatcher.Group(1);
			Matcher sentenceMatcher = sentencePattern.Matcher(doc);
			string ner = null;
			//Maintain current document ID.
			Pattern docIDPattern = Pattern.Compile("<DOCNO>(.*?)</DOCNO>", Pattern.Dotall + Pattern.CaseInsensitive);
			Matcher docIDMatcher = docIDPattern.Matcher(doc);
			if (docIDMatcher.Find())
			{
				currentDocumentID = docIDMatcher.Group(1);
			}
			else
			{
				currentDocumentID = "documentAfter " + currentDocumentID;
			}
			while (sentenceMatcher.Find())
			{
				string sentenceString = sentenceMatcher.Group(2);
				IList<CoreLabel> words = tokenizerFactory.GetTokenizer(new StringReader(sentenceString)).Tokenize();
				// FIXING TOKENIZATION PROBLEMS
				for (int i = 0; i < words.Count; i++)
				{
					CoreLabel w = words[i];
					if (i > 0 && w.Word().Equals("$"))
					{
						if (!words[i - 1].Word().EndsWith("PRP") && !words[i - 1].Word().EndsWith("WP"))
						{
							continue;
						}
						words[i - 1].Set(typeof(CoreAnnotations.TextAnnotation), words[i - 1].Word() + "$");
						words.Remove(i);
						i--;
					}
					else
					{
						if (w.Word().Equals("\\/"))
						{
							if (words[i - 1].Word().Equals("</COREF>"))
							{
								continue;
							}
							w.Set(typeof(CoreAnnotations.TextAnnotation), words[i - 1].Word() + "\\/" + words[i + 1].Word());
							words.Remove(i + 1);
							words.Remove(i - 1);
						}
					}
				}
				// END FIXING TOKENIZATION PROBLEMS
				IList<CoreLabel> sentence = new List<CoreLabel>();
				// MUC accepts embedded coref mentions, so we need to keep a stack for the mentions currently open
				Stack<Mention> stack = new Stack<Mention>();
				IList<Mention> mentions = new List<Mention>();
				allWords.Add(sentence);
				allGoldMentions.Add(mentions);
				foreach (CoreLabel word in words)
				{
					string w = word.Get(typeof(CoreAnnotations.TextAnnotation));
					// found regular token: WORD/POS
					if (!w.StartsWith("<") && w.Contains("\\/") && w.LastIndexOf("\\/") != w.Length - 2)
					{
						int i_1 = w.LastIndexOf("\\/");
						string w1 = Sharpen.Runtime.Substring(w, 0, i_1);
						// we do NOT set POS info here. We take the POS tags from the parser!
						word.Set(typeof(CoreAnnotations.TextAnnotation), w1);
						word.Remove(typeof(CoreAnnotations.OriginalTextAnnotation));
						sentence.Add(word);
					}
					else
					{
						// found the start SGML tag for a NE, e.g., "<ORGANIZATION>"
						if (w.StartsWith("<") && !w.StartsWith("<COREF") && !w.StartsWith("</"))
						{
							Pattern nerPattern = Pattern.Compile("<(.*?)>");
							Matcher m = nerPattern.Matcher(w);
							m.Find();
							ner = m.Group(1);
						}
						else
						{
							// found the end SGML tag for a NE, e.g., "</ORGANIZATION>"
							if (w.StartsWith("</") && !w.StartsWith("</COREF"))
							{
								Pattern nerPattern = Pattern.Compile("</(.*?)>");
								Matcher m = nerPattern.Matcher(w);
								m.Find();
								string ner1 = m.Group(1);
								if (ner != null && !ner.Equals(ner1))
								{
									throw new Exception("Unmatched NE labels in MUC file: " + ner + " v. " + ner1);
								}
								ner = null;
							}
							else
							{
								// found the start SGML tag for a coref mention
								if (w.StartsWith("<COREF"))
								{
									Mention mention = new Mention();
									// position of this mention in the sentence
									mention.startIndex = sentence.Count;
									// extract GOLD info about this coref chain. needed for eval
									Pattern idPattern = Pattern.Compile("ID=\"(.*?)\"");
									Pattern refPattern = Pattern.Compile("REF=\"(.*?)\"");
									Matcher m = idPattern.Matcher(w);
									m.Find();
									mention.mentionID = System.Convert.ToInt32(m.Group(1));
									m = refPattern.Matcher(w);
									if (m.Find())
									{
										mention.originalRef = System.Convert.ToInt32(m.Group(1));
									}
									// open mention. keep track of all open mentions using the stack
									stack.Push(mention);
								}
								else
								{
									// found the end SGML tag for a coref mention
									if (w.Equals("</COREF>"))
									{
										Mention mention = stack.Pop();
										mention.endIndex = sentence.Count;
										// this is a closed mention. add it to the final list of mentions
										// System.err.printf("Found MENTION: ID=%d, REF=%d\n", mention.mentionID, mention.originalRef);
										mentions.Add(mention);
									}
									else
									{
										word.Remove(typeof(CoreAnnotations.OriginalTextAnnotation));
										sentence.Add(word);
									}
								}
							}
						}
					}
				}
				StringBuilder textContent = new StringBuilder();
				for (int i_2 = 0; i_2 < sentence.Count; i_2++)
				{
					CoreLabel w = sentence[i_2];
					w.Set(typeof(CoreAnnotations.IndexAnnotation), i_2 + 1);
					w.Set(typeof(CoreAnnotations.UtteranceAnnotation), 0);
					if (i_2 > 0)
					{
						textContent.Append(" ");
					}
					textContent.Append(w.GetString<CoreAnnotations.TextAnnotation>());
				}
				ICoreMap sentCoreMap = new Annotation(textContent.ToString());
				allSentences.Add(sentCoreMap);
				sentCoreMap.Set(typeof(CoreAnnotations.TokensAnnotation), sentence);
			}
			// assign goldCorefClusterID
			IDictionary<int, Mention> idMention = Generics.NewHashMap();
			// temporary use
			foreach (IList<Mention> goldMentions in allGoldMentions)
			{
				foreach (Mention m in goldMentions)
				{
					idMention[m.mentionID] = m;
				}
			}
			foreach (IList<Mention> goldMentions_1 in allGoldMentions)
			{
				foreach (Mention m in goldMentions_1)
				{
					if (m.goldCorefClusterID == -1)
					{
						if (m.originalRef == -1)
						{
							m.goldCorefClusterID = m.mentionID;
						}
						else
						{
							int @ref = m.originalRef;
							while (true)
							{
								Mention m2 = idMention[@ref];
								if (m2.goldCorefClusterID != -1)
								{
									m.goldCorefClusterID = m2.goldCorefClusterID;
									break;
								}
								else
								{
									if (m2.originalRef == -1)
									{
										m2.goldCorefClusterID = m2.mentionID;
										m.goldCorefClusterID = m2.goldCorefClusterID;
										break;
									}
									else
									{
										@ref = m2.originalRef;
									}
								}
							}
						}
					}
				}
			}
			docAnno.Set(typeof(CoreAnnotations.SentencesAnnotation), allSentences);
			stanfordProcessor.Annotate(docAnno);
			if (allSentences.Count != allWords.Count)
			{
				throw new InvalidOperationException("allSentences != allWords");
			}
			for (int i_3 = 0; i_3 < allSentences.Count; i_3++)
			{
				IList<CoreLabel> annotatedSent = allSentences[i_3].Get(typeof(CoreAnnotations.TokensAnnotation));
				IList<CoreLabel> unannotatedSent = allWords[i_3];
				IList<Mention> mentionInSent = allGoldMentions[i_3];
				foreach (Mention m in mentionInSent)
				{
					m.dependency = allSentences[i_3].Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
				}
				if (annotatedSent.Count != unannotatedSent.Count)
				{
					throw new InvalidOperationException("annotatedSent != unannotatedSent");
				}
				for (int j = 0; j < sz; j++)
				{
					CoreLabel annotatedWord = annotatedSent[j];
					CoreLabel unannotatedWord = unannotatedSent[j];
					if (!annotatedWord.Get(typeof(CoreAnnotations.TextAnnotation)).Equals(unannotatedWord.Get(typeof(CoreAnnotations.TextAnnotation))))
					{
						throw new InvalidOperationException("annotatedWord != unannotatedWord");
					}
				}
				allWords.Set(i_3, annotatedSent);
				allTrees.Add(allSentences[i_3].Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
			}
			// extract predicted mentions
			allPredictedMentions = mentionFinder.ExtractPredictedMentions(docAnno, maxID, dictionaries);
			// add the relevant fields to mentions and order them for coref
			return Arrange(docAnno, allWords, allTrees, allPredictedMentions, allGoldMentions, true);
		}
	}
}

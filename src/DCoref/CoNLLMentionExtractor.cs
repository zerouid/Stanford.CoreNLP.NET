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
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>Extracts coref mentions from CoNLL2011 data files.</summary>
	/// <author>Angel Chang</author>
	public class CoNLLMentionExtractor : MentionExtractor
	{
		private readonly CoNLL2011DocumentReader reader;

		private readonly string corpusPath;

		private readonly bool replicateCoNLL;

		private static readonly Logger logger = SieveCoreferenceSystem.logger;

		/// <exception cref="System.Exception"/>
		public CoNLLMentionExtractor(Dictionaries dict, Properties props, Semantics semantics)
			: base(dict, semantics)
		{
			// Initialize reader for reading from CONLL2011 corpus
			corpusPath = props.GetProperty(Constants.Conll2011Prop);
			replicateCoNLL = bool.Parse(props.GetProperty(Constants.ReplicateconllProp, "false"));
			CoNLL2011DocumentReader.Options options = new CoNLL2011DocumentReader.Options();
			options.annotateTokenCoref = false;
			options.annotateTokenSpeaker = Constants.UseGoldSpeakerTags || replicateCoNLL;
			options.annotateTokenNer = Constants.UseGoldNe || replicateCoNLL;
			options.annotateTokenPos = Constants.UseGoldPos || replicateCoNLL;
			options.SetFilter(".*_auto_conll$");
			reader = new CoNLL2011DocumentReader(corpusPath, options);
			stanfordProcessor = LoadStanfordProcessor(props);
		}

		/// <exception cref="System.Exception"/>
		public CoNLLMentionExtractor(Dictionaries dict, Properties props, Semantics semantics, LogisticClassifier<string, string> singletonModel)
			: this(dict, props, semantics)
		{
			singletonPredictor = singletonModel;
		}

		private const bool Lemmatize = true;

		private const bool threadSafe = true;

		private static readonly TreeLemmatizer treeLemmatizer = new TreeLemmatizer();

		public override void ResetDocs()
		{
			base.ResetDocs();
			reader.Reset();
		}

		/// <exception cref="System.Exception"/>
		public override Document NextDoc()
		{
			IList<IList<CoreLabel>> allWords = new List<IList<CoreLabel>>();
			IList<Tree> allTrees = new List<Tree>();
			CoNLL2011DocumentReader.Document conllDoc = reader.GetNextDocument();
			if (conllDoc == null)
			{
				return null;
			}
			Annotation anno = conllDoc.GetAnnotation();
			IList<ICoreMap> sentences = anno.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap sentence in sentences)
			{
				if (!Constants.UseGoldParses && !replicateCoNLL)
				{
					// Remove tree from annotation and replace with parse using stanford parser
					sentence.Remove(typeof(TreeCoreAnnotations.TreeAnnotation));
				}
				else
				{
					Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					treeLemmatizer.TransformTree(tree);
					// generate the dependency graph
					try
					{
						SemanticGraph deps = SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Enhanced, GrammaticalStructure.Extras.None);
						SemanticGraph basicDeps = SemanticGraphFactory.MakeFromTree(tree, SemanticGraphFactory.Mode.Basic, GrammaticalStructure.Extras.None);
						sentence.Set(typeof(SemanticGraphCoreAnnotations.BasicDependenciesAnnotation), basicDeps);
						sentence.Set(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation), deps);
					}
					catch (Exception e)
					{
						logger.Log(Level.Warning, "Exception caught during extraction of Stanford dependencies. Will ignore and continue...", e);
					}
				}
			}
			string preSpeaker = null;
			int utterance = -1;
			foreach (CoreLabel token in anno.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				if (!token.ContainsKey(typeof(CoreAnnotations.SpeakerAnnotation)))
				{
					token.Set(typeof(CoreAnnotations.SpeakerAnnotation), string.Empty);
				}
				string curSpeaker = token.Get(typeof(CoreAnnotations.SpeakerAnnotation));
				if (!curSpeaker.Equals(preSpeaker))
				{
					utterance++;
					preSpeaker = curSpeaker;
				}
				token.Set(typeof(CoreAnnotations.UtteranceAnnotation), utterance);
			}
			// Run pipeline
			stanfordProcessor.Annotate(anno);
			foreach (ICoreMap sentence_1 in anno.Get(typeof(CoreAnnotations.SentencesAnnotation)))
			{
				allWords.Add(sentence_1.Get(typeof(CoreAnnotations.TokensAnnotation)));
				allTrees.Add(sentence_1.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
			}
			// Initialize gold mentions
			IList<IList<Mention>> allGoldMentions = ExtractGoldMentions(conllDoc);
			IList<IList<Mention>> allPredictedMentions;
			//allPredictedMentions = allGoldMentions;
			// Make copy of gold mentions since mentions may be later merged, mentionID's changed and stuff
			allPredictedMentions = mentionFinder.ExtractPredictedMentions(anno, maxID, dictionaries);
			try
			{
				RecallErrors(allGoldMentions, allPredictedMentions, anno);
			}
			catch (IOException e)
			{
				throw new Exception(e);
			}
			Document doc = Arrange(anno, allWords, allTrees, allPredictedMentions, allGoldMentions, true);
			doc.conllDoc = conllDoc;
			return doc;
		}

		private static IList<IList<Mention>> MakeCopy(IList<IList<Mention>> mentions)
		{
			IList<IList<Mention>> copy = new List<IList<Mention>>(mentions.Count);
			foreach (IList<Mention> sm in mentions)
			{
				IList<Mention> sm2 = new List<Mention>(sm.Count);
				foreach (Mention m in sm)
				{
					Mention m2 = new Mention();
					m2.goldCorefClusterID = m.goldCorefClusterID;
					m2.mentionID = m.mentionID;
					m2.startIndex = m.startIndex;
					m2.endIndex = m.endIndex;
					m2.originalSpan = m.originalSpan;
					m2.dependency = m.dependency;
					sm2.Add(m2);
				}
				copy.Add(sm2);
			}
			return copy;
		}

		/// <exception cref="System.IO.IOException"/>
		private static void RecallErrors(IList<IList<Mention>> goldMentions, IList<IList<Mention>> predictedMentions, Annotation doc)
		{
			IList<ICoreMap> coreMaps = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			int numSentences = goldMentions.Count;
			for (int i = 0; i < numSentences; i++)
			{
				ICoreMap coreMap = coreMaps[i];
				IList<CoreLabel> words = coreMap.Get(typeof(CoreAnnotations.TokensAnnotation));
				Tree tree = coreMap.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
				IList<Mention> goldMentionsSent = goldMentions[i];
				IList<Pair<int, int>> goldMentionsSpans = ExtractSpans(goldMentionsSent);
				foreach (Pair<int, int> mentionSpan in goldMentionsSpans)
				{
					logger.Finer("RECALL ERROR\n");
					logger.Finer(coreMap + "\n");
					for (int x = mentionSpan.first; x < mentionSpan.second; x++)
					{
						logger.Finer(words[x].Value() + " ");
					}
					logger.Finer("\n" + tree + "\n");
				}
			}
		}

		private static IList<Pair<int, int>> ExtractSpans(IList<Mention> listOfMentions)
		{
			IList<Pair<int, int>> mentionSpans = new List<Pair<int, int>>();
			foreach (Mention mention in listOfMentions)
			{
				Pair<int, int> mentionSpan = new Pair<int, int>(mention.startIndex, mention.endIndex);
				mentionSpans.Add(mentionSpan);
			}
			return mentionSpans;
		}

		public virtual IList<IList<Mention>> ExtractGoldMentions(CoNLL2011DocumentReader.Document conllDoc)
		{
			IList<ICoreMap> sentences = conllDoc.GetAnnotation().Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<IList<Mention>> allGoldMentions = new List<IList<Mention>>();
			CollectionValuedMap<string, ICoreMap> corefChainMap = conllDoc.GetCorefChainMap();
			for (int i = 0; i < sentences.Count; i++)
			{
				allGoldMentions.Add(new List<Mention>());
			}
			int maxCorefClusterId = -1;
			foreach (string corefIdStr in corefChainMap.Keys)
			{
				int id = System.Convert.ToInt32(corefIdStr);
				if (id > maxCorefClusterId)
				{
					maxCorefClusterId = id;
				}
			}
			int newMentionID = maxCorefClusterId + 1;
			foreach (KeyValuePair<string, ICollection<ICoreMap>> idChainEntry in corefChainMap)
			{
				int id = System.Convert.ToInt32(idChainEntry.Key);
				int clusterMentionCnt = 0;
				foreach (ICoreMap m in idChainEntry.Value)
				{
					clusterMentionCnt++;
					Mention mention = new Mention();
					mention.goldCorefClusterID = id;
					if (clusterMentionCnt == 1)
					{
						// First mention in cluster
						mention.mentionID = id;
						mention.originalRef = -1;
					}
					else
					{
						mention.mentionID = newMentionID;
						mention.originalRef = id;
						newMentionID++;
					}
					if (maxID < mention.mentionID)
					{
						maxID = mention.mentionID;
					}
					int sentIndex = m.Get(typeof(CoreAnnotations.SentenceIndexAnnotation));
					ICoreMap sent = sentences[sentIndex];
					mention.startIndex = m.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) - sent.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					mention.endIndex = m.Get(typeof(CoreAnnotations.TokenEndAnnotation)) - sent.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
					// will be set by arrange
					mention.originalSpan = m.Get(typeof(CoreAnnotations.TokensAnnotation));
					// Mention dependency graph is the enhanced dependency graph of the sentence
					mention.dependency = sentences[sentIndex].Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					allGoldMentions[sentIndex].Add(mention);
				}
			}
			return allGoldMentions;
		}
	}
}

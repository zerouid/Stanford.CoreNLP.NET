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
using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Util.Logging;
using Sharpen;

namespace Edu.Stanford.Nlp.Dcoref
{
	/// <summary>
	/// Extracts
	/// <c>&lt;COREF&gt;</c>
	/// mentions from a file annotated in ACE format (ACE2004, ACE2005).
	/// </summary>
	/// <author>Heeyoung Lee</author>
	public class ACEMentionExtractor : MentionExtractor
	{
		private AceReader aceReader;

		private string corpusPath;

		protected internal int fileIndex = 0;

		protected internal string[] files;

		private static readonly Logger logger = SieveCoreferenceSystem.logger;

		private class EntityComparator : IComparator<EntityMention>
		{
			public virtual int Compare(EntityMention m1, EntityMention m2)
			{
				if (m1.GetExtentTokenStart() > m2.GetExtentTokenStart())
				{
					return 1;
				}
				else
				{
					if (m1.GetExtentTokenStart() < m2.GetExtentTokenStart())
					{
						return -1;
					}
					else
					{
						if (m1.GetExtentTokenEnd() > m2.GetExtentTokenEnd())
						{
							return -1;
						}
						else
						{
							if (m1.GetExtentTokenEnd() < m2.GetExtentTokenEnd())
							{
								return 1;
							}
							else
							{
								return 0;
							}
						}
					}
				}
			}
		}

		/// <exception cref="System.Exception"/>
		public ACEMentionExtractor(Dictionaries dict, Properties props, Semantics semantics)
			: base(dict, semantics)
		{
			stanfordProcessor = LoadStanfordProcessor(props);
			if (props.Contains(Constants.Ace2004Prop))
			{
				corpusPath = props.GetProperty(Constants.Ace2004Prop);
				aceReader = new AceReader(stanfordProcessor, false, "ACE2004");
			}
			else
			{
				if (props.Contains(Constants.Ace2005Prop))
				{
					corpusPath = props.GetProperty(Constants.Ace2005Prop);
					aceReader = new AceReader(stanfordProcessor, false);
				}
			}
			aceReader.SetLoggerLevel(Level.Info);
			if (corpusPath[corpusPath.Length - 1] != File.separatorChar)
			{
				corpusPath += File.separatorChar;
			}
			files = new File(corpusPath).List();
		}

		/// <exception cref="System.Exception"/>
		public ACEMentionExtractor(Dictionaries dict, Properties props, Semantics semantics, LogisticClassifier<string, string> singletonModel)
			: this(dict, props, semantics)
		{
			singletonPredictor = singletonModel;
		}

		public override void ResetDocs()
		{
			base.ResetDocs();
			fileIndex = 0;
		}

		/// <exception cref="System.Exception"/>
		public override Document NextDoc()
		{
			IList<IList<CoreLabel>> allWords = new List<IList<CoreLabel>>();
			IList<IList<Mention>> allGoldMentions = new List<IList<Mention>>();
			IList<IList<Mention>> allPredictedMentions;
			IList<Tree> allTrees = new List<Tree>();
			Annotation anno;
			try
			{
				string filename = string.Empty;
				while (files.Length > fileIndex)
				{
					if (files[fileIndex].Contains("apf.xml"))
					{
						filename = files[fileIndex];
						fileIndex++;
						break;
					}
					else
					{
						fileIndex++;
						filename = string.Empty;
					}
				}
				if (files.Length <= fileIndex && filename.Equals(string.Empty))
				{
					return null;
				}
				anno = aceReader.Parse(corpusPath + filename);
				stanfordProcessor.Annotate(anno);
				IList<ICoreMap> sentences = anno.Get(typeof(CoreAnnotations.SentencesAnnotation));
				foreach (ICoreMap s in sentences)
				{
					int i = 1;
					foreach (CoreLabel w in s.Get(typeof(CoreAnnotations.TokensAnnotation)))
					{
						w.Set(typeof(CoreAnnotations.IndexAnnotation), i++);
						if (!w.ContainsKey(typeof(CoreAnnotations.UtteranceAnnotation)))
						{
							w.Set(typeof(CoreAnnotations.UtteranceAnnotation), 0);
						}
					}
					allTrees.Add(s.Get(typeof(TreeCoreAnnotations.TreeAnnotation)));
					allWords.Add(s.Get(typeof(CoreAnnotations.TokensAnnotation)));
					ACEMentionExtractor.EntityComparator comparator = new ACEMentionExtractor.EntityComparator();
					ExtractGoldMentions(s, allGoldMentions, comparator);
				}
				allPredictedMentions = mentionFinder.ExtractPredictedMentions(anno, maxID, dictionaries);
				PrintRawDoc(sentences, allGoldMentions, filename, true);
				PrintRawDoc(sentences, allPredictedMentions, filename, false);
			}
			catch (IOException e)
			{
				throw new RuntimeIOException(e);
			}
			return Arrange(anno, allWords, allTrees, allPredictedMentions, allGoldMentions, true);
		}

		private void ExtractGoldMentions(ICoreMap s, IList<IList<Mention>> allGoldMentions, ACEMentionExtractor.EntityComparator comparator)
		{
			IList<Mention> goldMentions = new List<Mention>();
			allGoldMentions.Add(goldMentions);
			IList<EntityMention> goldMentionList = s.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
			IList<CoreLabel> words = s.Get(typeof(CoreAnnotations.TokensAnnotation));
			TreeSet<EntityMention> treeForSortGoldMentions = new TreeSet<EntityMention>(comparator);
			if (goldMentionList != null)
			{
				Sharpen.Collections.AddAll(treeForSortGoldMentions, goldMentionList);
			}
			if (!treeForSortGoldMentions.IsEmpty())
			{
				foreach (EntityMention e in treeForSortGoldMentions)
				{
					Mention men = new Mention();
					men.dependency = s.Get(typeof(SemanticGraphCoreAnnotations.CollapsedDependenciesAnnotation));
					if (men.dependency == null)
					{
						men.dependency = s.Get(typeof(SemanticGraphCoreAnnotations.EnhancedDependenciesAnnotation));
					}
					men.startIndex = e.GetExtentTokenStart();
					men.endIndex = e.GetExtentTokenEnd();
					string[] parseID = e.GetObjectId().Split("-");
					men.mentionID = System.Convert.ToInt32(parseID[parseID.Length - 1]);
					string[] parseCorefID = e.GetCorefID().Split("-E");
					men.goldCorefClusterID = System.Convert.ToInt32(parseCorefID[parseCorefID.Length - 1]);
					men.originalRef = -1;
					for (int j = allGoldMentions.Count - 1; j >= 0; j--)
					{
						IList<Mention> l = allGoldMentions[j];
						for (int k = l.Count - 1; k >= 0; k--)
						{
							Mention m = l[k];
							if (men.goldCorefClusterID == m.goldCorefClusterID)
							{
								men.originalRef = m.mentionID;
							}
						}
					}
					goldMentions.Add(men);
					if (men.mentionID > maxID)
					{
						maxID = men.mentionID;
					}
					// set ner type
					for (int j_1 = e.GetExtentTokenStart(); j_1 < e.GetExtentTokenEnd(); j_1++)
					{
						CoreLabel word = words[j_1];
						string ner = e.GetType() + "-" + e.GetSubType();
					}
				}
			}
		}

		/// <exception cref="Java.IO.FileNotFoundException"/>
		private static void PrintRawDoc(IList<ICoreMap> sentences, IList<IList<Mention>> allMentions, string filename, bool gold)
		{
			StringBuilder doc = new StringBuilder();
			int previousOffset = 0;
			ICounter<int> mentionCount = new ClassicCounter<int>();
			foreach (IList<Mention> l in allMentions)
			{
				foreach (Mention m in l)
				{
					mentionCount.IncrementCount(m.goldCorefClusterID);
				}
			}
			for (int i = 0; i < sentences.Count; i++)
			{
				ICoreMap sentence = sentences[i];
				IList<Mention> mentions = allMentions[i];
				string[] tokens = sentence.Get(typeof(CoreAnnotations.TextAnnotation)).Split(" ");
				string sent = string.Empty;
				IList<CoreLabel> t = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
				if (previousOffset + 2 < t[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)))
				{
					sent += "\n";
				}
				previousOffset = t[t.Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				ICounter<int> startCounts = new ClassicCounter<int>();
				ICounter<int> endCounts = new ClassicCounter<int>();
				IDictionary<int, ICollection<int>> endID = Generics.NewHashMap();
				foreach (Mention m in mentions)
				{
					startCounts.IncrementCount(m.startIndex);
					endCounts.IncrementCount(m.endIndex);
					if (!endID.Contains(m.endIndex))
					{
						endID[m.endIndex] = Generics.NewHashSet<int>();
					}
					endID[m.endIndex].Add(m.goldCorefClusterID);
				}
				for (int j = 0; j < tokens.Length; j++)
				{
					if (endID.Contains(j))
					{
						foreach (int id in endID[j])
						{
							if (mentionCount.GetCount(id) != 1 && gold)
							{
								sent += "]_" + id;
							}
							else
							{
								sent += "]";
							}
						}
					}
					for (int k = 0; k < startCounts.GetCount(j); k++)
					{
						if (!sent.EndsWith("["))
						{
							sent += " ";
						}
						sent += "[";
					}
					sent += " ";
					sent = sent + tokens[j];
				}
				for (int k_1 = 0; k_1 < endCounts.GetCount(tokens.Length); k_1++)
				{
					sent += "]";
				}
				sent += "\n";
				doc.Append(sent);
			}
			if (gold)
			{
				logger.Fine("New DOC: (GOLD MENTIONS) ==================================================");
			}
			else
			{
				logger.Fine("New DOC: (Predicted Mentions) ==================================================");
			}
			logger.Fine(doc.ToString());
		}
	}
}

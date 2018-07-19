using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.Tregex;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Trees.UD
{
	/// <summary>
	/// Adds lemmata and features to an English CoNLL-U dependencies
	/// treebank.
	/// </summary>
	/// <author>Sebastian Schuster</author>
	public class UniversalDependenciesFeatureAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Trees.UD.UniversalDependenciesFeatureAnnotator));

		private const string FeatureMapFile = "edu/stanford/nlp/models/ud/feature_map.txt";

		private Dictionary<string, Dictionary<string, string>> posFeatureMap;

		private Dictionary<string, Dictionary<string, string>> wordPosFeatureMap;

		private readonly Morphology morphology = new Morphology();

		/// <exception cref="System.IO.IOException"/>
		public UniversalDependenciesFeatureAnnotator()
		{
			LoadFeatureMap();
		}

		/// <exception cref="System.IO.IOException"/>
		private void LoadFeatureMap()
		{
			using (Reader r = IOUtils.ReaderFromString(FeatureMapFile))
			{
				BufferedReader br = new BufferedReader(r);
				posFeatureMap = new Dictionary<string, Dictionary<string, string>>();
				wordPosFeatureMap = new Dictionary<string, Dictionary<string, string>>();
				string line;
				while ((line = br.ReadLine()) != null)
				{
					string[] parts = line.Split("\\s+");
					if (parts.Length < 3)
					{
						continue;
					}
					if (parts[0].Equals("*"))
					{
						posFeatureMap[parts[1]] = CoNLLUUtils.ParseFeatures(parts[2]);
					}
					else
					{
						wordPosFeatureMap[parts[0] + '_' + parts[1]] = CoNLLUUtils.ParseFeatures(parts[2]);
					}
				}
			}
		}

		private Dictionary<string, string> GetPOSFeatures(string word, string pos)
		{
			Dictionary<string, string> features = new Dictionary<string, string>();
			string wordPos = word.ToLower() + '_' + pos;
			if (wordPosFeatureMap.Contains(wordPos))
			{
				features.PutAll(wordPosFeatureMap[wordPos]);
			}
			else
			{
				if (posFeatureMap.Contains(pos))
				{
					features.PutAll(posFeatureMap[pos]);
				}
			}
			if (IsOrdinal(word, pos))
			{
				features["NumType"] = "Ord";
			}
			if (IsMultiplicative(word, pos))
			{
				features["NumType"] = "Mult";
			}
			return features;
		}

		private const string OrdinalExpression = "^(first|second|third|fourth|fifth|sixth|seventh|eigth|ninth|tenth|([0-9,.]+(th|st|nd|rd)))$";

		private const string MultiplicativeExpression = "^(once|twice)$";

		private static bool IsOrdinal(string word, string pos)
		{
			if (!pos.Equals("JJ"))
			{
				return false;
			}
			return word.ToLower().Matches(OrdinalExpression);
		}

		private static bool IsMultiplicative(string word, string pos)
		{
			if (!pos.Equals("RB"))
			{
				return false;
			}
			return word.ToLower().Matches(MultiplicativeExpression);
		}

		private static string SelfRegex = EnglishPatterns.selfRegex.Replace("/", string.Empty);

		private static Dictionary<string, string> GetGraphFeatures(SemanticGraph sg, IndexedWord word)
		{
			Dictionary<string, string> features = new Dictionary<string, string>();
			/* Determine the case of "you". */
			if (word.Tag().Equals("PRP") && (Sharpen.Runtime.EqualsIgnoreCase(word.Value(), "you") || Sharpen.Runtime.EqualsIgnoreCase(word.Value(), "it")))
			{
				features["Case"] = PronounCase(sg, word);
			}
			/* Determine the person of "was". */
			if (word.Tag().Equals("VBD") && Sharpen.Runtime.EqualsIgnoreCase(word.Value(), "was"))
			{
				string person = WasPerson(sg, word);
				if (person != null)
				{
					features["Person"] = person;
				}
			}
			/* Determine features of relative and interrogative pronouns. */
			features.PutAll(GetRelAndIntPronFeatures(sg, word));
			/* Determine features of gerunds and present participles. */
			if (word.Tag().Equals("VBG"))
			{
				if (HasBeAux(sg, word))
				{
					features["VerbForm"] = "Part";
					features["Tense"] = "Pres";
				}
				else
				{
					features["VerbForm"] = "Ger";
				}
			}
			/* Determine whether reflexive pronoun is reflexive or intensive. */
			if (word.Value().Matches(SelfRegex) && word.Tag().Equals("PRP"))
			{
				IndexedWord parent = sg.GetParent(word);
				if (parent != null)
				{
					SemanticGraphEdge edge = sg.GetEdge(parent, word);
					if (edge.GetRelation() != UniversalEnglishGrammaticalRelations.NpAdverbialModifier)
					{
						features["Case"] = "Acc";
						features["Reflex"] = "Yes";
					}
				}
			}
			/* Voice feature. */
			if (word.Tag().Equals("VBN"))
			{
				if (sg.HasChildWithReln(word, UniversalEnglishGrammaticalRelations.AuxPassiveModifier))
				{
					features["Voice"] = "Pass";
				}
			}
			return features;
		}

		/// <summary>Determine the case of the pronoun "you" or "it".</summary>
		private static string PronounCase(SemanticGraph sg, IndexedWord word)
		{
			word = sg.GetNodeByIndex(word.Index());
			IndexedWord parent = sg.GetParent(word);
			if (parent != null)
			{
				SemanticGraphEdge edge = sg.GetEdge(parent, word);
				if (edge != null)
				{
					if (UniversalEnglishGrammaticalRelations.Object.IsAncestor(edge.GetRelation()))
					{
						/* "you" is an object. */
						return "Acc";
					}
					else
					{
						if (UniversalEnglishGrammaticalRelations.NominalModifier.IsAncestor(edge.GetRelation()) || edge.GetRelation() == GrammaticalRelation.Root)
						{
							if (sg.HasChildWithReln(word, UniversalEnglishGrammaticalRelations.CaseMarker))
							{
								/* "you" is the head of a prepositional phrase. */
								return "Acc";
							}
						}
					}
				}
			}
			return "Nom";
		}

		/// <summary>Determine the person of "was".</summary>
		private static string WasPerson(SemanticGraph sg, IndexedWord word)
		{
			IndexedWord subj = sg.GetChildWithReln(word, UniversalEnglishGrammaticalRelations.NominalSubject);
			if (subj == null)
			{
				subj = sg.GetChildWithReln(word, UniversalEnglishGrammaticalRelations.NominalPassiveSubject);
			}
			if (subj != null)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(subj.Word(), "i"))
				{
					/* "I" is the subject of "was". */
					return "1";
				}
			}
			IndexedWord parent = sg.GetParent(word);
			if (parent == null)
			{
				return subj != null ? "3" : null;
			}
			SemanticGraphEdge edge = sg.GetEdge(parent, word);
			if (edge == null)
			{
				return subj != null ? "3" : null;
			}
			if (UniversalEnglishGrammaticalRelations.AuxModifier.Equals(edge.GetRelation()) || UniversalEnglishGrammaticalRelations.AuxPassiveModifier.Equals(edge.GetRelation()))
			{
				return WasPerson(sg, parent);
			}
			if (UniversalEnglishGrammaticalRelations.Conjunct.IsAncestor(edge.GetRelation()))
			{
				/* Check if the subject of the head of a conjunction is "I". */
				return WasPerson(sg, parent);
			}
			return "3";
		}

		/// <summary>Extracts features from relative and interrogative pronouns.</summary>
		private static Dictionary<string, string> GetRelAndIntPronFeatures(SemanticGraph sg, IndexedWord word)
		{
			Dictionary<string, string> features = new Dictionary<string, string>();
			if (word.Tag().StartsWith("W"))
			{
				bool isRel = false;
				IndexedWord parent = sg.GetParent(word);
				if (parent != null)
				{
					IndexedWord parentParent = sg.GetParent(parent);
					if (parentParent != null)
					{
						SemanticGraphEdge edge = sg.GetEdge(parentParent, parent);
						isRel = edge.GetRelation().Equals(UniversalEnglishGrammaticalRelations.RelativeClauseModifier);
					}
				}
				if (isRel)
				{
					features["PronType"] = "Rel";
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(word.Value(), "that"))
					{
						features["PronType"] = "Dem";
					}
					else
					{
						features["PronType"] = "Int";
					}
				}
			}
			return features;
		}

		private static IEnumerator<Tree> TreebankIterator(string path)
		{
			/* Remove empty nodes and strip indices from internal nodes but keep
			functional tags. */
			Treebank tb = new MemoryTreebank(new NPTmpRetainingTreeNormalizer(0, false, 1, false));
			tb.LoadPath(path);
			return tb.GetEnumerator();
		}

		private static readonly TregexPattern ImperativePattern = TregexPattern.Compile("__ > VB >+(/^[^S]/) S-IMP");

		/// <summary>
		/// Returns the indices of all imperative verbs in the
		/// tree t.
		/// </summary>
		private static ICollection<int> GetImperatives(Tree t)
		{
			ICollection<int> imps = new HashSet<int>();
			TregexMatcher matcher = ImperativePattern.Matcher(t);
			while (matcher.Find())
			{
				IList<ILabel> verbs = matcher.GetMatch().Yield();
				CoreLabel cl = (CoreLabel)verbs[0];
				imps.Add(cl.Index());
			}
			return imps;
		}

		/// <summary>
		/// Returns true if
		/// <paramref name="word"/>
		/// has an auxiliary verb attached to it.
		/// </summary>
		private static bool HasAux(SemanticGraph sg, IndexedWord word)
		{
			if (sg.HasChildWithReln(word, UniversalEnglishGrammaticalRelations.AuxModifier))
			{
				return true;
			}
			IndexedWord gov = sg.GetParent(word);
			if (gov != null)
			{
				SemanticGraphEdge edge = sg.GetEdge(gov, word);
				if (UniversalEnglishGrammaticalRelations.Conjunct.IsAncestor(edge.GetRelation()) || UniversalEnglishGrammaticalRelations.Copula.Equals(edge.GetRelation()))
				{
					return HasAux(sg, gov);
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if
		/// <paramref name="word"/>
		/// has an infinitival "to" attached to it.
		/// </summary>
		private static bool HasTo(SemanticGraph sg, IndexedWord word)
		{
			/* Check for infinitival to. */
			if (sg.HasChildWithReln(word, UniversalEnglishGrammaticalRelations.Marker))
			{
				foreach (IndexedWord marker in sg.GetChildrenWithReln(word, UniversalEnglishGrammaticalRelations.Marker))
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(marker.Value(), "to"))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static readonly string BeRegex = EnglishPatterns.beAuxiliaryRegex.Replace("/", string.Empty);

		/// <summary>
		/// Returns true if
		/// <paramref name="word"/>
		/// has an inflection of "be" as an auxiliary.
		/// </summary>
		private static bool HasBeAux(SemanticGraph sg, IndexedWord word)
		{
			foreach (IndexedWord aux in sg.GetChildrenWithReln(word, UniversalEnglishGrammaticalRelations.AuxModifier))
			{
				if (aux.Value().Matches(BeRegex))
				{
					return true;
				}
			}
			/* Check if head of conjunction has an auxiliary in case the word is part of a conjunction */
			IndexedWord gov = sg.GetParent(word);
			if (gov != null)
			{
				SemanticGraphEdge edge = sg.GetEdge(gov, word);
				if (UniversalEnglishGrammaticalRelations.Conjunct.IsAncestor(edge.GetRelation()))
				{
					return HasBeAux(sg, gov);
				}
			}
			return false;
		}

		public virtual void AddFeatures(SemanticGraph sg, Tree t, bool addLemma, bool addUPOS)
		{
			ICollection<int> imperatives = t != null ? GetImperatives(t) : new HashSet<int>();
			foreach (IndexedWord word in sg.VertexListSorted())
			{
				string posTag = word.Get(typeof(CoreAnnotations.PartOfSpeechAnnotation));
				string token = word.Get(typeof(CoreAnnotations.TextAnnotation));
				int index = word.Get(typeof(CoreAnnotations.IndexAnnotation));
				Dictionary<string, string> wordFeatures = word.Get(typeof(CoreAnnotations.CoNLLUFeats));
				if (wordFeatures == null)
				{
					wordFeatures = new Dictionary<string, string>();
					word.Set(typeof(CoreAnnotations.CoNLLUFeats), wordFeatures);
				}
				/* Features that only depend on the word and the PTB POS tag. */
				wordFeatures.PutAll(GetPOSFeatures(token, posTag));
				/* Semantic graph features. */
				wordFeatures.PutAll(GetGraphFeatures(sg, word));
				/* Handle VBs. */
				if (imperatives.Contains(index))
				{
					/* Imperative */
					wordFeatures["VerbForm"] = "Fin";
					wordFeatures["Mood"] = "Imp";
				}
				else
				{
					if (posTag.Equals("VB"))
					{
						/* Infinitive */
						wordFeatures["VerbForm"] = "Inf";
					}
				}
				/* Subjunctive detection too unreliable. */
				//} else {
				//  /* Present subjunctive */
				//  wordFeatures.put("VerbForm", "Fin");
				//  wordFeatures.put("Tense", "Pres");
				//  wordFeatures.put("Mood", "Subj");
				//}
				string lemma = word.Get(typeof(CoreAnnotations.LemmaAnnotation));
				if (addLemma && (lemma == null || lemma.Equals("_")))
				{
					word.Set(typeof(CoreAnnotations.LemmaAnnotation), morphology.Lemma(token, posTag));
				}
			}
			if (addUPOS && t != null)
			{
				t = UniversalPOSMapper.MapTree(t);
				IList<ILabel> uPOSTags = t.PreTerminalYield();
				IList<IndexedWord> yield = sg.VertexListSorted();
				// int len = yield.size();
				foreach (IndexedWord word_1 in yield)
				{
					ILabel uPOSTag = uPOSTags[word_1.Index() - 1];
					word_1.Set(typeof(CoreAnnotations.CoarseTagAnnotation), uPOSTag.Value());
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		public static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				log.Info("Usage: ");
				log.Info("java ");
				log.Info(typeof(Edu.Stanford.Nlp.Trees.UD.UniversalDependenciesFeatureAnnotator).GetCanonicalName());
				log.Info(" CoNLL-U_file tree_file [-addUPOS -escapeParenthesis]");
				return;
			}
			string coNLLUFile = args[0];
			string treeFile = args[1];
			bool addUPOS = false;
			bool escapeParens = false;
			for (int i = 2; i < args.Length; i++)
			{
				if (args[i].Equals("-addUPOS"))
				{
					addUPOS = true;
				}
				else
				{
					if (args[i].Equals("-escapeParenthesis"))
					{
						escapeParens = true;
					}
				}
			}
			Edu.Stanford.Nlp.Trees.UD.UniversalDependenciesFeatureAnnotator featureAnnotator = new Edu.Stanford.Nlp.Trees.UD.UniversalDependenciesFeatureAnnotator();
			Reader r = IOUtils.ReaderFromString(coNLLUFile);
			CoNLLUDocumentReader depReader = new CoNLLUDocumentReader();
			CoNLLUDocumentWriter depWriter = new CoNLLUDocumentWriter();
			IEnumerator<SemanticGraph> it = depReader.GetIterator(r);
			IEnumerator<Tree> treeIt = TreebankIterator(treeFile);
			while (it.MoveNext())
			{
				SemanticGraph sg = it.Current;
				Tree t = treeIt.Current;
				if (t == null || t.Yield().Count != sg.Size())
				{
					StringBuilder sentenceSb = new StringBuilder();
					foreach (IndexedWord word in sg.VertexListSorted())
					{
						sentenceSb.Append(word.Get(typeof(CoreAnnotations.TextAnnotation)));
						sentenceSb.Append(' ');
					}
					throw new Exception("CoNLL-U file and tree file are not aligned. \n" + "Sentence: " + sentenceSb + '\n' + "Tree: " + ((t == null) ? "null" : t.PennString()));
				}
				featureAnnotator.AddFeatures(sg, t, true, addUPOS);
				System.Console.Out.Write(depWriter.PrintSemanticGraph(sg, !escapeParens));
			}
		}
	}
}

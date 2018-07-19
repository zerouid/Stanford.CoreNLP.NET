using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <author>Grace Muzny</author>
	public class DependencyParseSieve : QMSieve
	{
		public DependencyParseSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
			: base(doc, characterMap, pronounCorefMap, animacySet, "Deterministic depparse")
		{
		}

		public override void DoQuoteToMention(Annotation doc)
		{
			// Trigram patterns
			// p/r 1/.304
			DependencyParses(doc);
			OneSpeakerSentence(doc);
		}

		private bool InRange(Pair<int, int> range, int val)
		{
			return range.first <= val && val <= range.second;
		}

		//using quote-removed depparses
		public virtual void DependencyParses(Annotation doc)
		{
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			foreach (ICoreMap quote in quotes)
			{
				if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
				{
					continue;
				}
				Pair<int, int> range = QuoteAttributionUtils.GetRemainderInSentence(doc, quote);
				if (range == null)
				{
					continue;
				}
				//search for mentions in the first run
				Pair<List<string>, List<Pair<int, int>>> namesAndNameIndices = ScanForNames(range);
				List<string> names = namesAndNameIndices.first;
				List<Pair<int, int>> nameIndices = namesAndNameIndices.second;
				SemanticGraph graph = quote.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation));
				SemgrexMatcher matcher = subjVerbPattern.Matcher(graph);
				IList<Pair<IndexedWord, IndexedWord>> subjVerbPairs = new List<Pair<IndexedWord, IndexedWord>>();
				//TODO: check and see if this is necessary
				while (matcher.Find())
				{
					IndexedWord subj = matcher.GetNode("SUBJ");
					IndexedWord verb = matcher.GetNode("VERB");
					subjVerbPairs.Add(new Pair<IndexedWord, IndexedWord>(subj, verb));
				}
				IList<IndexedWord> vbs = graph.GetAllNodesByPartOfSpeechPattern("VB.*");
				foreach (IndexedWord iw in vbs)
				{
					// does it have an nsubj child?
					ICollection<IndexedWord> children = graph.GetChildren(iw);
					IList<IndexedWord> deps = Generics.NewArrayList();
					IndexedWord nsubj = null;
					foreach (IndexedWord child in children)
					{
						SemanticGraphEdge sge = graph.GetEdge(iw, child);
						if (sge.GetRelation().GetShortName().Equals("dep") && child.Tag().StartsWith("VB"))
						{
							deps.Add(child);
						}
						else
						{
							if (sge.GetRelation().GetShortName().Equals("nsubj"))
							{
								nsubj = child;
							}
						}
					}
					if (nsubj != null)
					{
						foreach (IndexedWord dep in deps)
						{
							subjVerbPairs.Add(new Pair(nsubj, dep));
						}
					}
				}
				//look for a speech verb
				foreach (Pair<IndexedWord, IndexedWord> SVPair in subjVerbPairs)
				{
					IndexedWord verb = SVPair.second;
					IndexedWord subj = SVPair.first;
					//check if subj and verb outside of quote
					int verbTokPos = TokenToLocation(verb.BackingLabel());
					int subjTokPos = TokenToLocation(verb.BackingLabel());
					if (InRange(range, verbTokPos) && InRange(range, subjTokPos) && commonSpeechWords.Contains(verb.Lemma()))
					{
						if (subj.Tag().Equals("NNP"))
						{
							int startChar = subj.BeginPosition();
							for (int i = 0; i < names.Count; i++)
							{
								Pair<int, int> nameIndex = nameIndices[i];
								//avoid names that don't actually exist in
								if (RangeContainsCharIndex(nameIndex, startChar))
								{
									FillInMention(quote, TokenRangeToString(nameIndex), nameIndex.first, nameIndex.second, sieveName, Name);
									break;
								}
							}
						}
						else
						{
							if (subj.Tag().Equals("PRP"))
							{
								int loc = TokenToLocation(subj.BackingLabel());
								FillInMention(quote, subj.Word(), loc, loc, sieveName, Pronoun);
								break;
							}
							else
							{
								if (subj.Tag().Equals("NN") && animacySet.Contains(subj.Word()))
								{
									int loc = TokenToLocation(subj.BackingLabel());
									FillInMention(quote, subj.Word(), loc, loc, sieveName, AnimateNoun);
									break;
								}
							}
						}
					}
				}
			}
		}
	}
}

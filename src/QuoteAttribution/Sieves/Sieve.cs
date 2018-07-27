using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Quoteattribution.Sieves
{
	/// <summary>Created by mjfang on 7/8/16.</summary>
	public class Sieve
	{
		protected internal Annotation doc;

		protected internal IDictionary<string, IList<Person>> characterMap;

		protected internal IDictionary<int, string> pronounCorefMap;

		protected internal ICollection<string> animacySet;

		public const string Pronoun = "pronoun";

		public const string Name = "name";

		public const string AnimateNoun = "animate noun";

		protected internal Sieve.TokenNode rootNameNode;

		public Sieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet)
		{
			//mention types
			this.doc = doc;
			this.characterMap = characterMap;
			this.pronounCorefMap = pronounCorefMap;
			this.animacySet = animacySet;
			this.rootNameNode = CreateNameMatcher();
		}

		//resolves ambiguities if necessary (note: currently not actually being done)
		protected internal virtual Person ResolveAmbiguities(string name)
		{
			if (characterMap[name] == null)
			{
				return null;
			}
			if (characterMap[name].Count == 1)
			{
				return characterMap[name][0];
			}
			else
			{
				return null;
			}
		}

		protected internal virtual ICollection<Person> GetNamesInParagraph(ICoreMap quote)
		{
			//iterate forwards and backwards to look for quotes in the same paragraph, and add all the names present in them to the list.
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			IList<string> quoteNames = new List<string>();
			int quoteParagraph = QuoteAttributionUtils.GetQuoteParagraphIndex(doc, quote);
			int quoteIndex = quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation));
			for (int i = quoteIndex; i >= 0; i--)
			{
				ICoreMap currQuote = quotes[i];
				int currQuoteParagraph = QuoteAttributionUtils.GetQuoteParagraphIndex(doc, currQuote);
				if (currQuoteParagraph == quoteParagraph)
				{
					Sharpen.Collections.AddAll(quoteNames, ScanForNames(new Pair<int, int>(currQuote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), currQuote.Get(typeof(CoreAnnotations.TokenEndAnnotation)))).first);
				}
				else
				{
					break;
				}
			}
			for (int i_1 = quoteIndex + 1; i_1 < quotes.Count; i_1++)
			{
				ICoreMap currQuote = quotes[i_1];
				int currQuoteParagraph = QuoteAttributionUtils.GetQuoteParagraphIndex(doc, currQuote);
				if (currQuoteParagraph == quoteParagraph)
				{
					Sharpen.Collections.AddAll(quoteNames, ScanForNames(new Pair<int, int>(currQuote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), currQuote.Get(typeof(CoreAnnotations.TokenEndAnnotation)))).first);
				}
				else
				{
					break;
				}
			}
			ICollection<Person> namesInParagraph = new HashSet<Person>();
			foreach (string name in quoteNames)
			{
				foreach (Person p in characterMap[name])
				{
					namesInParagraph.Add(p);
				}
			}
			return namesInParagraph;
		}

		public virtual Person DoCoreference(int corefMapKey, ICoreMap quote)
		{
			if (pronounCorefMap == null)
			{
				return null;
			}
			ICollection<Person> quoteNames = new HashSet<Person>();
			if (quote != null)
			{
				quoteNames = GetNamesInParagraph(quote);
			}
			string referent = pronounCorefMap[corefMapKey];
			Person candidate = ResolveAmbiguities(referent);
			if (candidate != null && !quoteNames.Contains(candidate))
			{
				return candidate;
			}
			return null;
		}

		private class TokenNode
		{
			public IList<Person> personList;

			public Dictionary<string, Sieve.TokenNode> childNodes;

			public string token;

			public string fullName;

			internal int level;

			public TokenNode(Sieve _enclosing, string token, int level)
			{
				this._enclosing = _enclosing;
				this.token = token;
				this.level = level;
				this.childNodes = new Dictionary<string, Sieve.TokenNode>();
			}

			private readonly Sieve _enclosing;
		}

		protected internal virtual Sieve.TokenNode CreateNameMatcher()
		{
			Sieve.TokenNode rootNode = new Sieve.TokenNode(this, "$ROOT", -1);
			foreach (string key in characterMap.Keys)
			{
				string[] tokens = key.Split(" ");
				Sieve.TokenNode currNode = rootNode;
				for (int i = 0; i < tokens.Length; i++)
				{
					string tok = tokens[i];
					if (currNode.childNodes.Keys.Contains(tok))
					{
						currNode = currNode.childNodes[tok];
					}
					else
					{
						Sieve.TokenNode newNode = new Sieve.TokenNode(this, tok, i);
						currNode.childNodes[tok] = newNode;
						currNode = newNode;
					}
					if (i == tokens.Length - 1)
					{
						currNode.personList = characterMap[key];
						currNode.fullName = key;
					}
				}
			}
			return rootNode;
		}

		//Note: this doesn't necessarily find all possible candidates, but is kind of a greedy version.
		// E.g. "Elizabeth and Jane" will return only "Elizabeth and Jane", but not "Elizabeth", and "Jane" as well.
		public virtual Pair<List<string>, List<Pair<int, int>>> ScanForNamesNew(Pair<int, int> textRun)
		{
			List<string> potentialNames = new List<string>();
			List<Pair<int, int>> nameIndices = new List<Pair<int, int>>();
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			Sieve.TokenNode pointer = rootNameNode;
			for (int index = textRun.first; index <= textRun.second && index < tokens.Count; index++)
			{
				CoreLabel token = tokens[index];
				string tokenText = token.Word();
				//      System.out.println(token);
				if (pointer.childNodes.Keys.Contains(tokenText))
				{
					pointer = pointer.childNodes[tokenText];
				}
				else
				{
					if (!pointer.token.Equals("$ROOT"))
					{
						if (pointer.fullName != null)
						{
							potentialNames.Add(pointer.fullName);
							nameIndices.Add(new Pair<int, int>(index - 1 - pointer.level, index - 1));
						}
						pointer = rootNameNode;
					}
				}
			}
			int index_1 = textRun.second + 1;
			if (!pointer.token.Equals("$ROOT"))
			{
				//catch the end case
				if (pointer.fullName != null)
				{
					potentialNames.Add(pointer.fullName);
					nameIndices.Add(new Pair<int, int>(index_1 - 1 - pointer.level, index_1 - 1));
				}
				pointer = rootNameNode;
			}
			return new Pair<List<string>, List<Pair<int, int>>>(potentialNames, nameIndices);
		}

		//scan for all potential names based on names list, based on CoreMaps and returns their indices in doc.tokens as well.
		public virtual Pair<List<string>, List<Pair<int, int>>> ScanForNames(Pair<int, int> textRun)
		{
			List<string> potentialNames = new List<string>();
			List<Pair<int, int>> nameIndices = new List<Pair<int, int>>();
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			//split on non-alphanumeric
			ICollection<string> aliases = characterMap.Keys;
			string potentialName = string.Empty;
			Pair<int, int> potentialIndex = null;
			for (int index = textRun.first; index <= textRun.second; index++)
			{
				CoreLabel token = tokens[index];
				string tokenText = token.Word();
				if (char.IsUpperCase(tokenText[0]) || tokenText.Equals("de"))
				{
					//TODO: make this better (String matching)
					potentialName += " " + tokenText;
					if (potentialIndex == null)
					{
						potentialIndex = new Pair<int, int>(index, index);
					}
					else
					{
						potentialIndex.second = index;
					}
				}
				else
				{
					if (potentialName.Length != 0)
					{
						string actual = Sharpen.Runtime.Substring(potentialName, 1);
						if (aliases.Contains(actual))
						{
							potentialNames.Add(actual);
							nameIndices.Add(potentialIndex);
						}
						else
						{
							// in the event that the first word in a sentence is a non-name..
							string removeFirstWord = Sharpen.Runtime.Substring(actual, actual.IndexOf(" ") + 1);
							if (aliases.Contains(removeFirstWord))
							{
								potentialNames.Add(removeFirstWord);
								nameIndices.Add(new Pair<int, int>(potentialIndex.first + 1, potentialIndex.second));
							}
						}
						potentialName = string.Empty;
						potentialIndex = null;
					}
				}
			}
			if (potentialName.Length != 0)
			{
				if (aliases.Contains(Sharpen.Runtime.Substring(potentialName, 1)))
				{
					potentialNames.Add(Sharpen.Runtime.Substring(potentialName, 1));
					nameIndices.Add(potentialIndex);
				}
			}
			return new Pair<List<string>, List<Pair<int, int>>>(potentialNames, nameIndices);
		}

		protected internal virtual List<int> ScanForPronouns(Pair<int, int> nonQuoteRun)
		{
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			List<int> pronounList = new List<int>();
			for (int i = nonQuoteRun.first; i <= nonQuoteRun.second && i < tokens.Count; i++)
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(tokens[i].Word(), "he") || Sharpen.Runtime.EqualsIgnoreCase(tokens[i].Word(), "she"))
				{
					pronounList.Add(i);
				}
			}
			return pronounList;
		}

		protected internal virtual List<int> ScanForPronouns(List<Pair<int, int>> nonQuoteRuns)
		{
			List<int> pronounList = new List<int>();
			for (int run_index = 0; run_index < nonQuoteRuns.Count; run_index++)
			{
				Sharpen.Collections.AddAll(pronounList, ScanForPronouns(nonQuoteRuns[run_index]));
			}
			return pronounList;
		}

		// for filling in the text of a mention
		public virtual string TokenRangeToString(Pair<int, int> tokenRange)
		{
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			// see if the token range matches an entity mention
			IList<ICoreMap> entityMentionsInDoc = doc.Get(typeof(CoreAnnotations.MentionsAnnotation));
			int potentialMatchingEntityMentionIndex = tokens[tokenRange.first].Get(typeof(CoreAnnotations.EntityMentionIndexAnnotation));
			ICoreMap potentialMatchingEntityMention = null;
			if (entityMentionsInDoc != null && potentialMatchingEntityMentionIndex != null)
			{
				potentialMatchingEntityMention = entityMentionsInDoc[potentialMatchingEntityMentionIndex];
			}
			// if there is a matching entity mention, return it's text (which has been processed to remove
			// things like newlines and xml)...if there isn't return the full substring of the document text
			if (potentialMatchingEntityMention != null && potentialMatchingEntityMention.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) == tokens[tokenRange.first].BeginPosition() && potentialMatchingEntityMention.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				)) == tokens[tokenRange.second].EndPosition())
			{
				return potentialMatchingEntityMention.Get(typeof(CoreAnnotations.TextAnnotation));
			}
			else
			{
				return Sharpen.Runtime.Substring(doc.Get(typeof(CoreAnnotations.TextAnnotation)), tokens[tokenRange.first].BeginPosition(), tokens[tokenRange.second].EndPosition());
			}
		}

		public virtual string TokenRangeToString(int token_idx)
		{
			return doc.Get(typeof(CoreAnnotations.TokensAnnotation))[token_idx].Word();
		}

		public virtual Sieve.MentionData FindClosestMentionInSpanForward(Pair<int, int> span)
		{
			IList<int> pronounIndices = ScanForPronouns(span);
			IList<Pair<int, int>> nameIndices = ScanForNamesNew(span).second;
			IList<int> animacyIndices = ScanForAnimates(span);
			int closestPronounIndex = int.MaxValue;
			int closestAnimate = int.MaxValue;
			Pair<int, int> closestNameIndex = new Pair<int, int>(int.MaxValue, 0);
			if (pronounIndices.Count > 0)
			{
				closestPronounIndex = pronounIndices[0];
			}
			if (nameIndices.Count > 0)
			{
				closestNameIndex = nameIndices[0];
			}
			if (animacyIndices.Count > 0)
			{
				closestAnimate = animacyIndices[0];
			}
			Sieve.MentionData md = null;
			if (closestPronounIndex < closestNameIndex.first)
			{
				md = (closestAnimate < closestPronounIndex) ? new Sieve.MentionData(this, closestAnimate, closestAnimate, TokenRangeToString(closestAnimate), AnimateNoun) : new Sieve.MentionData(this, closestPronounIndex, closestPronounIndex, TokenRangeToString
					(closestPronounIndex), Pronoun);
			}
			else
			{
				if (closestPronounIndex > closestNameIndex.first)
				{
					md = (closestAnimate < closestNameIndex.first) ? new Sieve.MentionData(this, closestAnimate, closestAnimate, TokenRangeToString(closestAnimate), AnimateNoun) : new Sieve.MentionData(this, closestNameIndex.first, closestNameIndex.second, TokenRangeToString
						(closestNameIndex), Name);
				}
			}
			return md;
		}

		public virtual IList<Sieve.MentionData> FindClosestMentionsInSpanForward(Pair<int, int> span)
		{
			IList<Sieve.MentionData> mentions = new List<Sieve.MentionData>();
			Pair<int, int> currSpan = span;
			while (true)
			{
				Sieve.MentionData mention = FindClosestMentionInSpanForward(currSpan);
				if (mention != null)
				{
					mentions.Add(mention);
					currSpan.first = mention.end + 1;
				}
				else
				{
					return mentions;
				}
			}
		}

		public virtual IList<Sieve.MentionData> FindClosestMentionsInSpanBackward(Pair<int, int> span)
		{
			IList<Sieve.MentionData> mentions = new List<Sieve.MentionData>();
			Pair<int, int> currSpan = span;
			while (true)
			{
				Sieve.MentionData mentionData = FindClosestMentionInSpanBackward(currSpan);
				if (mentionData != null)
				{
					mentions.Add(mentionData);
					currSpan.second = mentionData.begin - 1;
				}
				else
				{
					return mentions;
				}
			}
		}

		public virtual IList<int> ScanForAnimates(Pair<int, int> span)
		{
			IList<int> animateIndices = new List<int>();
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			for (int i = span.first; i <= span.second && i < tokens.Count; i++)
			{
				CoreLabel token = tokens[i];
				if (animacySet.Contains(token.Word()))
				{
					animateIndices.Add(i);
				}
			}
			return animateIndices;
		}

		public class MentionData
		{
			public int begin;

			public int end;

			public string text;

			public string type;

			public MentionData(Sieve _enclosing, int begin, int end, string text, string type)
			{
				this._enclosing = _enclosing;
				this.begin = begin;
				this.end = end;
				this.text = text;
				this.type = type;
			}

			private readonly Sieve _enclosing;
		}

		public virtual Sieve.MentionData FindClosestMentionInSpanBackward(Pair<int, int> span)
		{
			IList<int> pronounIndices = ScanForPronouns(span);
			IList<Pair<int, int>> nameIndices = ScanForNamesNew(span).second;
			IList<int> animateIndices = ScanForAnimates(span);
			int closestPronounIndex = int.MinValue;
			int closestAnimate = int.MinValue;
			Pair<int, int> closestNameIndex = new Pair<int, int>(0, int.MinValue);
			if (pronounIndices.Count > 0)
			{
				closestPronounIndex = pronounIndices[pronounIndices.Count - 1];
			}
			if (nameIndices.Count > 0)
			{
				closestNameIndex = nameIndices[nameIndices.Count - 1];
			}
			if (animateIndices.Count > 0)
			{
				closestAnimate = animateIndices[animateIndices.Count - 1];
			}
			Sieve.MentionData md = null;
			if (closestPronounIndex > closestNameIndex.second)
			{
				md = (closestAnimate > closestPronounIndex) ? new Sieve.MentionData(this, closestAnimate, closestAnimate, TokenRangeToString(closestAnimate), AnimateNoun) : new Sieve.MentionData(this, closestPronounIndex, closestPronounIndex, TokenRangeToString
					(closestPronounIndex), Pronoun);
			}
			else
			{
				if (closestPronounIndex < closestNameIndex.second)
				{
					md = (closestAnimate > closestNameIndex.second) ? new Sieve.MentionData(this, closestAnimate, closestAnimate, TokenRangeToString(closestAnimate), AnimateNoun) : new Sieve.MentionData(this, closestNameIndex.first, closestNameIndex.second, TokenRangeToString
						(closestNameIndex), Name);
				}
			}
			return md;
		}

		private class Mention
		{
			public int begin;

			public int end;

			public string text;

			public string type;

			public Mention(Sieve _enclosing, int begin, int end, string text, string type)
			{
				this._enclosing = _enclosing;
				this.begin = begin;
				this.end = end;
				this.text = text;
				this.type = type;
			}

			private readonly Sieve _enclosing;
		}

		public virtual void OneSpeakerSentence(Annotation doc)
		{
			IList<CoreLabel> toks = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			IDictionary<int, IList<ICoreMap>> quotesBySentence = new Dictionary<int, IList<ICoreMap>>();
			for (int quoteIndex = 0; quoteIndex < quotes.Count; quoteIndex++)
			{
				ICoreMap quote = quotes[quoteIndex];
				// iterate through each quote in the chapter
				// group quotes by sentence
				int quoteBeginTok = quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				int sentenceBeginId = toks[quoteBeginTok].SentIndex();
				int quoteEndTok = quote.Get(typeof(CoreAnnotations.TokenEndAnnotation));
				int sentenceEndId = toks[quoteEndTok].SentIndex();
				quotesBySentence.PutIfAbsent(sentenceBeginId, new List<ICoreMap>());
				quotesBySentence.PutIfAbsent(sentenceEndId, new List<ICoreMap>());
				quotesBySentence[sentenceBeginId].Add(quote);
				quotesBySentence[sentenceEndId].Add(quote);
			}
			//
			foreach (int k in quotesBySentence.Keys)
			{
				IList<ICoreMap> quotesInSent = quotesBySentence[k];
				IList<Sieve.Mention> existantMentions = new List<Sieve.Mention>();
				foreach (ICoreMap quote in quotesInSent)
				{
					if (quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
					{
						Sieve.Mention m = new Sieve.Mention(this, quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation)), quote.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation)), quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)), 
							quote.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation)));
						existantMentions.Add(m);
					}
				}
				//remove cases in which there is more than one mention in a sentence.
				bool same = true;
				string text = null;
				foreach (Sieve.Mention m_1 in existantMentions)
				{
					if (text == null)
					{
						text = m_1.text;
					}
					if (!Sharpen.Runtime.EqualsIgnoreCase(m_1.text, text))
					{
						same = false;
					}
				}
				if (same && text != null && existantMentions.Count > 0)
				{
					foreach (ICoreMap quote_1 in quotesInSent)
					{
						if (quote_1.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) == null)
						{
							Sieve.Mention firstM = existantMentions[0];
							quote_1.Set(typeof(QuoteAttributionAnnotator.MentionAnnotation), firstM.text);
							quote_1.Set(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation), firstM.begin);
							quote_1.Set(typeof(QuoteAttributionAnnotator.MentionEndAnnotation), firstM.end);
							quote_1.Set(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation), "Deterministic one speaker sentence");
							quote_1.Set(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation), firstM.type);
						}
					}
				}
			}
		}

		//convert token range to char range, check if charIndex is in it.
		public virtual bool RangeContainsCharIndex(Pair<int, int> tokenRange, int charIndex)
		{
			IList<CoreLabel> tokens = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			CoreLabel startToken = tokens[tokenRange.First()];
			CoreLabel endToken = tokens[tokenRange.Second()];
			int startTokenCharBegin = startToken.BeginPosition();
			int endTokenCharEnd = endToken.EndPosition();
			return (startTokenCharBegin <= charIndex && charIndex <= endTokenCharEnd);
		}

		public virtual int TokenToLocation(CoreLabel token)
		{
			ICoreMap sentence = doc.Get(typeof(CoreAnnotations.SentencesAnnotation))[token.Get(typeof(CoreAnnotations.SentenceIndexAnnotation))];
			return sentence.Get(typeof(CoreAnnotations.TokenBeginAnnotation)) + token.Get(typeof(CoreAnnotations.IndexAnnotation)) - 1;
		}

		protected internal virtual int GetQuoteParagraph(ICoreMap quote)
		{
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			return sentences[quote.Get(typeof(CoreAnnotations.SentenceBeginAnnotation))].Get(typeof(CoreAnnotations.ParagraphIndexAnnotation));
		}
	}
}

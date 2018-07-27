using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;



namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves
{
	/// <author>Michael Fang</author>
	/// <author>Grace Muzny</author>
	public class BaselineTopSpeakerSieve : MSSieve
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Quoteattribution.Sieves.MSSieves.BaselineTopSpeakerSieve));

		private IDictionary<string, Person.Gender> genderList;

		private ICollection<string> familyRelations;

		public const int BackwardWindow = 2000;

		public const int BackwardWindowBig = 4000;

		public const int ForwardWindow = 500;

		public const int ForwardWindowBig = 2500;

		public const double ForwardWeight = 0.34;

		public const double BackwardWeight = 1.0;

		public BaselineTopSpeakerSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet, IDictionary<string, Person.Gender> genderList, ICollection<string> familyRelations
			)
			: base(doc, characterMap, pronounCorefMap, animacySet)
		{
			this.genderList = genderList;
			this.familyRelations = familyRelations;
		}

		public override void DoMentionToSpeaker(Annotation doc)
		{
			TopSpeakerInRange(doc);
		}

		public virtual Sieve.MentionData MakeMentionData(ICoreMap q)
		{
			if (q.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)) != null)
			{
				return new Sieve.MentionData(this, q.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation)), q.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation)), q.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation)), q.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation
					)));
			}
			return new Sieve.MentionData(this, -1, -1, null, null);
		}

		public virtual void TopSpeakerInRange(Annotation doc)
		{
			IList<CoreLabel> toks = doc.Get(typeof(CoreAnnotations.TokensAnnotation));
			IList<ICoreMap> quotes = doc.Get(typeof(CoreAnnotations.QuotationsAnnotation));
			for (int quote_idx = 0; quote_idx < quotes.Count; quote_idx++)
			{
				ICoreMap quote = quotes[quote_idx];
				if (quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) == null)
				{
					Pair<int, int> quoteRun = new Pair<int, int>(quote.Get(typeof(CoreAnnotations.TokenBeginAnnotation)), quote.Get(typeof(CoreAnnotations.TokenEndAnnotation)));
					IList<Sieve.MentionData> closestMentionsBackward = FindClosestMentionsInSpanBackward(new Pair<int, int>(Math.Max(0, quoteRun.first - BackwardWindow), quoteRun.first - 1));
					IList<Sieve.MentionData> closestMentions = FindClosestMentionsInSpanForward(new Pair<int, int>(quoteRun.second + 1, Math.Min(quoteRun.second + ForwardWindow, toks.Count - 1)));
					Sharpen.Collections.AddAll(closestMentions, closestMentionsBackward);
					Person.Gender gender = GetGender(MakeMentionData(quote));
					IList<string> topSpeakers = Counters.ToSortedList(GetTopSpeakers(closestMentions, closestMentionsBackward, gender, quote, false));
					//if none found, try again with bigger window
					if (topSpeakers.IsEmpty())
					{
						closestMentionsBackward = FindClosestMentionsInSpanBackward(new Pair<int, int>(Math.Max(0, quoteRun.first - BackwardWindowBig), quoteRun.first - 1));
						closestMentions = FindClosestMentionsInSpanForward(new Pair<int, int>(quoteRun.second + 1, Math.Min(quoteRun.second + ForwardWindowBig, toks.Count - 1)));
						topSpeakers = Counters.ToSortedList(GetTopSpeakers(closestMentions, closestMentionsBackward, gender, quote, true));
					}
					if (topSpeakers.IsEmpty())
					{
						log.Warn("Watch out, there's an empty top speakers list!");
						continue;
					}
					topSpeakers = RemoveQuoteNames(topSpeakers, quote);
					string topSpeaker = topSpeakers[0];
					Pair<string, string> nextPrediction = GetConversationalNextPrediction(quotes, quote_idx, gender);
					bool set = UpdatePredictions(quote, nextPrediction);
					if (set)
					{
						continue;
					}
					Pair<string, string> prevPrediction = GetConversationalPreviousPrediction(quotes, quote_idx, gender);
					set = UpdatePredictions(quote, prevPrediction);
					if (set)
					{
						continue;
					}
					Pair<string, string> famPrediction = GetFamilyAnimateVocative(quotes, quote_idx, gender, topSpeakers);
					set = UpdatePredictions(quote, famPrediction);
					if (set)
					{
						continue;
					}
					UpdatePredictions(quote, new Pair<string, string>(topSpeaker, string.Empty));
				}
			}
		}

		public virtual IList<string> RemoveQuoteNames(IList<string> topSpeakers, ICoreMap quote)
		{
			// if the top speakers name is in the quote,
			// move to the next option and remove it
			string topSpeaker = topSpeakers[0];
			ICollection<Person> namesInParagraphQuotes = GetNamesInParagraph(quote);
			if (namesInParagraphQuotes.Contains(characterMap[topSpeaker][0]) && topSpeakers.Count > 1)
			{
				topSpeakers.Remove(0);
			}
			return topSpeakers;
		}

		public virtual Person.Gender GetGender(Sieve.MentionData mention)
		{
			Person.Gender gender = Person.Gender.Unk;
			if (mention.type != null && mention.type.Equals("pronoun"))
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(mention.text, "he"))
				{
					gender = Person.Gender.Male;
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(mention.text, "she"))
					{
						gender = Person.Gender.Female;
					}
				}
			}
			else
			{
				if (mention.type != null && mention.type.Equals("animate noun"))
				{
					string mentionText = mention.text.ToLower();
					if (genderList[mentionText] != null)
					{
						gender = genderList[mentionText];
					}
				}
				else
				{
					if (mention.type != null && mention.type.Equals("name"))
					{
						gender = characterMap[mention.text][0].gender;
					}
				}
			}
			return gender;
		}

		public virtual ICounter<string> GetTopSpeakers(IList<Sieve.MentionData> closestMentions, IList<Sieve.MentionData> closestMentionsBackward, Person.Gender gender, ICoreMap quote, bool overrideGender)
		{
			ICounter<string> topSpeakerInRange = new ClassicCounter<string>();
			ICounter<string> topSpeakerInRangeIgnoreGender = new ClassicCounter<string>();
			ICollection<Sieve.MentionData> backwardsMentions = new HashSet<Sieve.MentionData>(closestMentionsBackward);
			foreach (Sieve.MentionData mention in closestMentions)
			{
				double weight = backwardsMentions.Contains(mention) ? BackwardWeight : ForwardWeight;
				if (mention.type.Equals(Name))
				{
					if (!characterMap.Keys.Contains(mention.text))
					{
						continue;
					}
					Person p = characterMap[mention.text][0];
					if ((gender == Person.Gender.Male && p.gender == Person.Gender.Male) || (gender == Person.Gender.Female && p.gender == Person.Gender.Female) || (gender == Person.Gender.Unk))
					{
						topSpeakerInRange.IncrementCount(p.name, weight);
					}
					topSpeakerInRangeIgnoreGender.IncrementCount(p.name, weight);
					if (closestMentions.Count == 128 && closestMentionsBackward.Count == 94)
					{
						System.Console.Out.WriteLine(p.name + " " + weight + " name");
					}
				}
				else
				{
					if (mention.type.Equals(Pronoun))
					{
						int charBeginKey = doc.Get(typeof(CoreAnnotations.TokensAnnotation))[mention.begin].BeginPosition();
						Person p = DoCoreference(charBeginKey, quote);
						if (p != null)
						{
							if ((gender == Person.Gender.Male && p.gender == Person.Gender.Male) || (gender == Person.Gender.Female && p.gender == Person.Gender.Female) || (gender == Person.Gender.Unk))
							{
								topSpeakerInRange.IncrementCount(p.name, weight);
							}
							topSpeakerInRangeIgnoreGender.IncrementCount(p.name, weight);
							if (closestMentions.Count == 128 && closestMentionsBackward.Count == 94)
							{
								System.Console.Out.WriteLine(p.name + " " + weight + " pronoun");
							}
						}
					}
				}
			}
			if (topSpeakerInRange.Size() > 0)
			{
				return topSpeakerInRange;
			}
			else
			{
				if (gender != Person.Gender.Unk && !overrideGender)
				{
					return topSpeakerInRange;
				}
			}
			return topSpeakerInRangeIgnoreGender;
		}

		public virtual bool UpdatePredictions(ICoreMap quote, Pair<string, string> speakerAndMethod)
		{
			if (speakerAndMethod.first != null && speakerAndMethod.second != null)
			{
				quote.Set(typeof(QuoteAttributionAnnotator.SpeakerAnnotation), characterMap[speakerAndMethod.first][0].name);
				quote.Set(typeof(QuoteAttributionAnnotator.SpeakerSieveAnnotation), "Baseline Top" + speakerAndMethod.second);
				return true;
			}
			return false;
		}

		public virtual Pair<string, string> GetFamilyAnimateVocative(IList<ICoreMap> quotes, int quote_index, Person.Gender gender, IList<string> topSpeakers)
		{
			Sieve.MentionData mention = MakeMentionData(quotes[quote_index]);
			if (mention.text != null)
			{
				if (mention.type.Equals("animate noun") && familyRelations.Contains(mention.text.ToLower()) && gender != Person.Gender.Unk)
				{
					int quoteContainingMention = GetQuoteContainingRange(quotes, new Pair<int, int>(mention.begin, mention.end));
					if (quoteContainingMention >= 0)
					{
						string relatedName = quotes[quoteContainingMention].Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation));
						if (relatedName != null)
						{
							foreach (string speaker in topSpeakers)
							{
								string[] speakerNames = speaker.Split("_");
								if (relatedName.EndsWith(speakerNames[speakerNames.Length - 1]))
								{
									return new Pair<string, string>(speaker, "family animate");
								}
							}
						}
					}
				}
			}
			return new Pair<string, string>(null, null);
		}

		public virtual Pair<string, string> GetConversationalPreviousPrediction(IList<ICoreMap> quotes, int quoteIndex, Person.Gender gender)
		{
			string topSpeaker = null;
			string modifier = null;
			// if the n - 2 paragraph quotes are labelled with a speaker and
			// that speakers gender does not disagree, label with that speaker
			IList<int> quotesInPrevPrev = new List<int>();
			ICoreMap quote = quotes[quoteIndex];
			int quoteParagraph = GetQuoteParagraph(quote);
			for (int j = quoteIndex - 1; j >= 0; j--)
			{
				if (GetQuoteParagraph(quotes[j]) == quoteParagraph - 2)
				{
					quotesInPrevPrev.Add(j);
				}
			}
			foreach (int prevPrev in quotesInPrevPrev)
			{
				ICoreMap prevprevQuote = quotes[prevPrev];
				string speakerName = prevprevQuote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation));
				if (speakerName != null && (gender == Person.Gender.Unk) || GetGender(MakeMentionData(prevprevQuote)) == gender)
				{
					topSpeaker = speakerName;
					modifier = " conversation - prev";
				}
			}
			return new Pair<string, string>(topSpeaker, modifier);
		}

		public virtual Pair<string, string> GetConversationalNextPrediction(IList<ICoreMap> quotes, int quoteIndex, Person.Gender gender)
		{
			string topSpeaker = null;
			string modifier = null;
			// if the n - 2 paragraph quotes are labelled with a speaker and
			// that speakers gender does not disagree, label with that speaker
			IList<int> quotesInNextNext = new List<int>();
			ICoreMap quote = quotes[quoteIndex];
			int quoteParagraph = GetQuoteParagraph(quote);
			for (int j = quoteIndex + 1; j < quotes.Count; j++)
			{
				if (GetQuoteParagraph(quotes[j]) == quoteParagraph + 2)
				{
					quotesInNextNext.Add(j);
				}
			}
			foreach (int nextNext in quotesInNextNext)
			{
				ICoreMap nextNextQuote = quotes[nextNext];
				string speakerName = nextNextQuote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation));
				Sieve.MentionData md = MakeMentionData(quotes[nextNext]);
				if (speakerName != null && (gender == Person.Gender.Unk) || GetGender(md) == gender)
				{
					topSpeaker = speakerName;
					modifier = " conversation - next";
				}
			}
			return new Pair<string, string>(topSpeaker, modifier);
		}

		public static int GetQuoteContainingRange(IList<ICoreMap> quotes, Pair<int, int> range)
		{
			for (int i = 0; i < quotes.Count; i++)
			{
				if (quotes[i].Get(typeof(CoreAnnotations.TokenBeginAnnotation)) <= range.first && quotes[i].Get(typeof(CoreAnnotations.TokenEndAnnotation)) >= range.second)
				{
					return i;
				}
			}
			return -1;
		}
	}
}

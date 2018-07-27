using System.Collections.Generic;
using Edu.Stanford.Nlp.Pipeline;
using Edu.Stanford.Nlp.Quoteattribution;
using Edu.Stanford.Nlp.Quoteattribution.Sieves;
using Edu.Stanford.Nlp.Semgraph.Semgrex;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Quoteattribution.Sieves.QMSieves
{
	/// <author>Michael Fang</author>
	public abstract class QMSieve : Sieve
	{
		protected internal static ICollection<string> beforeQuotePunctuation = new HashSet<string>(Arrays.AsList(new string[] { ",", ":" }));

		protected internal static readonly SemgrexPattern subjVerbPattern = SemgrexPattern.Compile("{pos:/VB.*/}=VERB >nsubj {}=SUBJ");

		protected internal static ICollection<string> commonSpeechWords = new HashSet<string>(Arrays.AsList(new string[] { "say", "cry", "reply", "add", "think", "observe", "call", "answer" }));

		protected internal string sieveName;

		public QMSieve(Annotation doc, IDictionary<string, IList<Person>> characterMap, IDictionary<int, string> pronounCorefMap, ICollection<string> animacySet, string sieveName)
			: base(doc, characterMap, pronounCorefMap, animacySet)
		{
			this.sieveName = sieveName;
		}

		//  public abstract void doQuoteToMention(List<XMLPredictions> predsList);
		public abstract void DoQuoteToMention(Annotation doc);

		public static void FillInMention(ICoreMap quote, string text, int begin, int end, string sieveName, string mentionType)
		{
			quote.Set(typeof(QuoteAttributionAnnotator.MentionAnnotation), text);
			quote.Set(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation), begin);
			quote.Set(typeof(QuoteAttributionAnnotator.MentionEndAnnotation), end);
			quote.Set(typeof(QuoteAttributionAnnotator.MentionSieveAnnotation), sieveName);
			quote.Set(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation), mentionType);
		}

		protected internal static void FillInMention(ICoreMap quote, Sieve.MentionData md, string sieveName)
		{
			FillInMention(quote, md.text, md.begin, md.end, sieveName, md.type);
		}

		protected internal virtual Sieve.MentionData GetMentionData(ICoreMap quote)
		{
			string text = quote.Get(typeof(QuoteAttributionAnnotator.MentionAnnotation));
			int begin = quote.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation));
			int end = quote.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation));
			string type = quote.Get(typeof(QuoteAttributionAnnotator.MentionTypeAnnotation));
			return new Sieve.MentionData(this, begin, end, text, type);
		}
	}
}

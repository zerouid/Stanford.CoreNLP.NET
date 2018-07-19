using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Wrapper around a CoreMap representing a quote.</summary>
	/// <remarks>Wrapper around a CoreMap representing a quote.  Adds some helpful methods.</remarks>
	public class CoreQuote
	{
		private ICoreMap quoteCoreMap;

		private CoreDocument document;

		private IList<CoreSentence> sentences;

		public bool hasSpeaker;

		public bool hasCanonicalSpeaker;

		private Optional<string> speaker;

		private Optional<string> canonicalSpeaker;

		private Optional<IList<CoreLabel>> speakerTokens;

		private Optional<IList<CoreLabel>> canonicalSpeakerTokens;

		private Optional<Pair<int, int>> speakerCharOffsets;

		private Optional<Pair<int, int>> canonicalSpeakerCharOffsets;

		private Optional<CoreEntityMention> speakerEntityMention;

		private Optional<CoreEntityMention> canonicalSpeakerEntityMention;

		public CoreQuote(CoreDocument myDocument, ICoreMap coreMapQuote)
		{
			// optional speaker info...note there may not be an entity mention corresponding to the speaker
			this.document = myDocument;
			this.quoteCoreMap = coreMapQuote;
			// attach sentences to the quote
			this.sentences = new List<CoreSentence>();
			int firstSentenceIndex = this.quoteCoreMap.Get(typeof(CoreAnnotations.SentenceBeginAnnotation));
			int lastSentenceIndex = this.quoteCoreMap.Get(typeof(CoreAnnotations.SentenceEndAnnotation));
			for (int currSentIndex = firstSentenceIndex; currSentIndex <= lastSentenceIndex; currSentIndex++)
			{
				this.sentences.Add(this.document.Sentences()[currSentIndex]);
			}
			// set up the speaker info
			this.speaker = this.quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) != null ? Optional.Of(this.quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation))) : Optional.Empty();
			this.canonicalSpeaker = this.quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation)) != null ? Optional.Of(this.quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation))) : Optional.Empty();
			// set up info for direct speaker mention (example: "He")
			int firstSpeakerTokenIndex = quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.MentionBeginAnnotation));
			int lastSpeakerTokenIndex = quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.MentionEndAnnotation));
			this.speakerTokens = Optional.Empty();
			this.speakerCharOffsets = Optional.Empty();
			this.speakerEntityMention = Optional.Empty();
			if (firstSpeakerTokenIndex != null && lastSpeakerTokenIndex != null)
			{
				this.speakerTokens = Optional.Of(new List<CoreLabel>());
				for (int speakerTokenIndex = firstSpeakerTokenIndex; speakerTokenIndex <= lastSpeakerTokenIndex; speakerTokenIndex++)
				{
					this.speakerTokens.Get().Add(this.document.Tokens()[speakerTokenIndex]);
				}
				int speakerCharOffsetBegin = this.speakerTokens.Get()[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int speakerCharOffsetEnd = this.speakerTokens.Get()[speakerTokens.Get().Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				this.speakerCharOffsets = Optional.Of(new Pair<int, int>(speakerCharOffsetBegin, speakerCharOffsetEnd));
				foreach (CoreEntityMention candidateEntityMention in this.document.EntityMentions())
				{
					Pair<int, int> entityMentionOffsets = candidateEntityMention.CharOffsets();
					if (entityMentionOffsets.Equals(this.speakerCharOffsets.Get()))
					{
						this.speakerEntityMention = Optional.Of(candidateEntityMention);
						break;
					}
				}
			}
			// set up info for canonical speaker mention (example: "Joe Smith")
			int firstCanonicalSpeakerTokenIndex = quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionBeginAnnotation));
			int lastCanonicalSpeakerTokenIndex = quoteCoreMap.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionEndAnnotation));
			this.canonicalSpeakerTokens = Optional.Empty();
			this.canonicalSpeakerCharOffsets = Optional.Empty();
			this.canonicalSpeakerEntityMention = Optional.Empty();
			if (firstCanonicalSpeakerTokenIndex != null && lastCanonicalSpeakerTokenIndex != null)
			{
				this.canonicalSpeakerTokens = Optional.Of(new List<CoreLabel>());
				for (int canonicalSpeakerTokenIndex = firstCanonicalSpeakerTokenIndex; canonicalSpeakerTokenIndex <= lastCanonicalSpeakerTokenIndex; canonicalSpeakerTokenIndex++)
				{
					this.canonicalSpeakerTokens.Get().Add(this.document.Tokens()[canonicalSpeakerTokenIndex]);
				}
				int canonicalSpeakerCharOffsetBegin = this.canonicalSpeakerTokens.Get()[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int canonicalSpeakerCharOffsetEnd = this.canonicalSpeakerTokens.Get()[canonicalSpeakerTokens.Get().Count - 1].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				this.canonicalSpeakerCharOffsets = Optional.Of(new Pair<int, int>(canonicalSpeakerCharOffsetBegin, canonicalSpeakerCharOffsetEnd));
				foreach (CoreEntityMention candidateEntityMention in this.document.EntityMentions())
				{
					Pair<int, int> entityMentionOffsets = candidateEntityMention.CharOffsets();
					if (entityMentionOffsets.Equals(this.canonicalSpeakerCharOffsets.Get()))
					{
						this.canonicalSpeakerEntityMention = Optional.Of(candidateEntityMention);
						break;
					}
				}
			}
			// record if there is speaker info
			this.hasSpeaker = this.speaker.IsPresent();
			this.hasCanonicalSpeaker = this.canonicalSpeaker.IsPresent();
		}

		/// <summary>get the underlying CoreMap if need be</summary>
		public virtual ICoreMap CoreMap()
		{
			return quoteCoreMap;
		}

		/// <summary>get this quote's document</summary>
		public virtual CoreDocument Document()
		{
			return document;
		}

		/// <summary>full text of the mention</summary>
		public virtual string Text()
		{
			return this.quoteCoreMap.Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary>retrieve the CoreSentence's attached to this quote</summary>
		public virtual IList<CoreSentence> Sentences()
		{
			return this.sentences;
		}

		/// <summary>retrieve the text of the speaker</summary>
		public virtual Optional<string> Speaker()
		{
			return this.speaker;
		}

		/// <summary>retrieve the text of the canonical speaker</summary>
		public virtual Optional<string> CanonicalSpeaker()
		{
			return this.canonicalSpeaker;
		}

		/// <summary>retrieve the tokens of the speaker</summary>
		public virtual Optional<IList<CoreLabel>> SpeakerTokens()
		{
			return this.speakerTokens;
		}

		/// <summary>retrieve the character offsets of the speaker</summary>
		public virtual Optional<Pair<int, int>> SpeakerCharOffsets()
		{
			return this.speakerCharOffsets;
		}

		/// <summary>retrieve the entity mention corresponding to the speaker if there is one</summary>
		public virtual Optional<CoreEntityMention> SpeakerEntityMention()
		{
			return this.speakerEntityMention;
		}

		/// <summary>retrieve the tokens of the canonical speaker</summary>
		public virtual Optional<IList<CoreLabel>> CanonicalSpeakerTokens()
		{
			return this.canonicalSpeakerTokens;
		}

		/// <summary>retrieve the character offsets of the canonical speaker</summary>
		public virtual Optional<Pair<int, int>> CanonicalSpeakerCharOffsets()
		{
			return this.canonicalSpeakerCharOffsets;
		}

		/// <summary>retrieve the entity mention corresponding to the canonical speaker if there is one</summary>
		public virtual Optional<CoreEntityMention> CanonicalSpeakerEntityMention()
		{
			return this.canonicalSpeakerEntityMention;
		}

		/// <summary>char offsets of quote</summary>
		public virtual Pair<int, int> QuoteCharOffsets()
		{
			int beginCharOffset = this.quoteCoreMap.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
			int endCharOffset = this.quoteCoreMap.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
			return new Pair<int, int>(beginCharOffset, endCharOffset);
		}

		public override string ToString()
		{
			return CoreMap().ToString();
		}
	}
}

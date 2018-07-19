using System.Collections.Generic;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.Ling;
using Java.Util.Stream;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Wrapper around an annotation representing a document.</summary>
	/// <remarks>Wrapper around an annotation representing a document.  Adds some helpful methods.</remarks>
	public class CoreDocument
	{
		protected internal Edu.Stanford.Nlp.Pipeline.Annotation annotationDocument;

		private IList<CoreEntityMention> entityMentions;

		private IList<CoreQuote> quotes;

		private IList<CoreSentence> sentences;

		public CoreDocument(string documentText)
		{
			this.annotationDocument = new Edu.Stanford.Nlp.Pipeline.Annotation(documentText);
		}

		public CoreDocument(Edu.Stanford.Nlp.Pipeline.Annotation annotation)
		{
			this.annotationDocument = annotation;
			WrapAnnotations();
		}

		/// <summary>complete the wrapping process post annotation by a pipeline</summary>
		public virtual void WrapAnnotations()
		{
			// wrap all of the sentences
			if (this.annotationDocument.Get(typeof(CoreAnnotations.SentencesAnnotation)) != null)
			{
				WrapSentences();
				// if there are entity mentions, build a document wide list
				if (!sentences.IsEmpty() && sentences[0].EntityMentions() != null)
				{
					BuildDocumentEntityMentionsList();
				}
				// if there are quotes, build a document wide list
				if (QuoteAnnotator.GatherQuotes(this.annotationDocument) != null)
				{
					BuildDocumentQuotesList();
				}
			}
		}

		/// <summary>create list of CoreSentence's based on the Annotation's sentences</summary>
		private void WrapSentences()
		{
			sentences = this.annotationDocument.Get(typeof(CoreAnnotations.SentencesAnnotation)).Stream().Map(null).Collect(Collectors.ToList());
			sentences.ForEach(null);
		}

		/// <summary>build a list of all entity mentions in the document from the sentences</summary>
		private void BuildDocumentEntityMentionsList()
		{
			entityMentions = sentences.Stream().FlatMap(null).Collect(Collectors.ToList());
		}

		private void BuildDocumentQuotesList()
		{
			this.quotes = QuoteAnnotator.GatherQuotes(this.annotationDocument).Stream().Map(null).Collect(Collectors.ToList());
		}

		/// <summary>provide access to the underlying annotation if needed</summary>
		public virtual Edu.Stanford.Nlp.Pipeline.Annotation Annotation()
		{
			return this.annotationDocument;
		}

		/// <summary>return the doc id of this doc</summary>
		public virtual string DocID()
		{
			return this.annotationDocument.Get(typeof(CoreAnnotations.DocIDAnnotation));
		}

		/// <summary>return the doc date of this doc</summary>
		public virtual string DocDate()
		{
			return this.annotationDocument.Get(typeof(CoreAnnotations.DocDateAnnotation));
		}

		/// <summary>return the full text of the doc</summary>
		public virtual string Text()
		{
			return this.annotationDocument.Get(typeof(CoreAnnotations.TextAnnotation));
		}

		/// <summary>return the full token list for this doc</summary>
		public virtual IList<CoreLabel> Tokens()
		{
			return this.annotationDocument.Get(typeof(CoreAnnotations.TokensAnnotation));
		}

		/// <summary>the list of sentences in this document</summary>
		public virtual IList<CoreSentence> Sentences()
		{
			return this.sentences;
		}

		/// <summary>the list of entity mentions in this document</summary>
		public virtual IList<CoreEntityMention> EntityMentions()
		{
			return this.entityMentions;
		}

		/// <summary>coref info</summary>
		public virtual IDictionary<int, CorefChain> CorefChains()
		{
			return this.annotationDocument.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
		}

		/// <summary>quotes</summary>
		public virtual IList<CoreQuote> Quotes()
		{
			return this.quotes;
		}

		public override string ToString()
		{
			return Annotation().ToString();
		}
	}
}

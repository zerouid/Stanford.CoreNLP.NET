using System.Collections.Generic;
using System.IO;
using Edu.Stanford.Nlp.Coref;
using Edu.Stanford.Nlp.Coref.Data;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.IE.Util;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Naturalli;
using Edu.Stanford.Nlp.Semgraph;
using Edu.Stanford.Nlp.Sentiment;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Pipeline
{
	/// <author>John Bauer</author>
	public class TextOutputter : AnnotationOutputter
	{
		public TextOutputter()
		{
		}

		/// <summary>
		/// <inheritDoc/>
		/// 
		/// </summary>
		/// <exception cref="System.IO.IOException"/>
		public override void Print(Annotation annotation, OutputStream stream, AnnotationOutputter.Options options)
		{
			PrintWriter os = new PrintWriter(IOUtils.EncodedOutputStreamWriter(stream, options.encoding));
			Print(annotation, os, options);
		}

		/// <summary>The meat of the outputter</summary>
		/// <exception cref="System.IO.IOException"/>
		private static void Print(Annotation annotation, PrintWriter pw, AnnotationOutputter.Options options)
		{
			double beam = options.beamPrintingOption;
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// Display docid if available
			string docId = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
			if (docId != null)
			{
				IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
				int nSentences = (sentences != null) ? sentences.Count : 0;
				int nTokens = (tokens != null) ? tokens.Count : 0;
				pw.Printf("Document: ID=%s (%d sentences, %d tokens)%n", docId, nSentences, nTokens);
			}
			// Display doctitle if available
			string docTitle = annotation.Get(typeof(CoreAnnotations.DocTitleAnnotation));
			if (docTitle != null)
			{
				pw.Printf("Document Title: %s%n", docTitle);
			}
			// Display docdate if available
			string docDate = annotation.Get(typeof(CoreAnnotations.DocDateAnnotation));
			if (docDate != null)
			{
				pw.Printf("Document Date: %s%n", docDate);
			}
			// Display doctype if available
			string docType = annotation.Get(typeof(CoreAnnotations.DocTypeAnnotation));
			if (docType != null)
			{
				pw.Printf("Document Type: %s%n", docType);
			}
			// Display docsourcetype if available
			string docSourceType = annotation.Get(typeof(CoreAnnotations.DocSourceTypeAnnotation));
			if (docSourceType != null)
			{
				pw.Printf("Document Source Type: %s%n", docSourceType);
			}
			// display each sentence in this annotation
			if (sentences != null)
			{
				for (int i = 0; i < sz; i++)
				{
					pw.Println();
					ICoreMap sentence = sentences[i];
					IList<CoreLabel> tokens = sentence.Get(typeof(CoreAnnotations.TokensAnnotation));
					string sentiment = sentence.Get(typeof(SentimentCoreAnnotations.SentimentClass));
					string piece;
					if (sentiment == null)
					{
						piece = string.Empty;
					}
					else
					{
						piece = ", sentiment: " + sentiment;
					}
					pw.Printf("Sentence #%d (%d tokens%s):%n", (i + 1), tokens.Count, piece);
					string text = sentence.Get(typeof(CoreAnnotations.TextAnnotation));
					pw.Println(text);
					// display the token-level annotations
					string[] tokenAnnotations = new string[] { "Text", "PartOfSpeech", "Lemma", "Answer", "NamedEntityTag", "CharacterOffsetBegin", "CharacterOffsetEnd", "NormalizedNamedEntityTag", "Timex", "TrueCase", "TrueCaseText", "SentimentClass", "WikipediaEntity"
						 };
					pw.Println();
					pw.Println("Tokens:");
					foreach (CoreLabel token in tokens)
					{
						pw.Print(token.ToShorterString(tokenAnnotations));
						pw.Println();
					}
					// display the parse tree for this sentence
					Tree tree = sentence.Get(typeof(TreeCoreAnnotations.TreeAnnotation));
					if (tree != null)
					{
						pw.Println();
						pw.Println("Constituency parse: ");
						options.constituentTreePrinter.PrintTree(tree, pw);
					}
					// display sentiment tree if they asked for sentiment
					if (!StringUtils.IsNullOrEmpty(sentiment))
					{
						pw.Println();
						pw.Println("Sentiment-annotated binary tree:");
						Tree sTree = sentence.Get(typeof(SentimentCoreAnnotations.SentimentAnnotatedTree));
						if (sTree != null)
						{
							sTree.PennPrint(pw, null);
							pw.Println();
						}
					}
					// It is possible to turn off the semantic graphs, in which
					// case we don't want to recreate them using the dependency
					// printer.  This might be relevant if using CoreNLP for a
					// language which doesn't have dependencies, for example.
					if (sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)) != null)
					{
						pw.Println();
						pw.Println("Dependency Parse (enhanced plus plus dependencies):");
						pw.Print(sentence.Get(typeof(SemanticGraphCoreAnnotations.EnhancedPlusPlusDependenciesAnnotation)).ToList());
					}
					// display the entity mentions
					IList<ICoreMap> entityMentions = sentence.Get(typeof(CoreAnnotations.MentionsAnnotation));
					if (entityMentions != null)
					{
						pw.Println();
						pw.Println("Extracted the following NER entity mentions:");
						foreach (ICoreMap entityMention in entityMentions)
						{
							if (entityMention.Get(typeof(CoreAnnotations.EntityTypeAnnotation)) != null)
							{
								pw.Println(entityMention.Get(typeof(CoreAnnotations.TextAnnotation)) + "\t" + entityMention.Get(typeof(CoreAnnotations.EntityTypeAnnotation)));
							}
						}
					}
					// display MachineReading entities and relations
					IList<EntityMention> entities = sentence.Get(typeof(MachineReadingAnnotations.EntityMentionsAnnotation));
					if (entities != null)
					{
						pw.Println();
						pw.Println("Extracted the following MachineReading entity mentions:");
						foreach (EntityMention e in entities)
						{
							pw.Print('\t');
							pw.Println(e);
						}
					}
					IList<RelationMention> relations = sentence.Get(typeof(MachineReadingAnnotations.RelationMentionsAnnotation));
					if (relations != null)
					{
						pw.Println();
						pw.Println("Extracted the following MachineReading relation mentions:");
						foreach (RelationMention r in relations)
						{
							if (r.PrintableObject(beam))
							{
								pw.Println(r);
							}
						}
					}
					// display OpenIE triples
					ICollection<RelationTriple> openieTriples = sentence.Get(typeof(NaturalLogicAnnotations.RelationTriplesAnnotation));
					if (openieTriples != null && !openieTriples.IsEmpty())
					{
						pw.Println();
						pw.Println("Extracted the following Open IE triples:");
						foreach (RelationTriple triple in openieTriples)
						{
							pw.Println(OpenIE.TripleToString(triple, docId, sentence));
						}
					}
					// display KBP triples
					ICollection<RelationTriple> kbpTriples = sentence.Get(typeof(CoreAnnotations.KBPTriplesAnnotation));
					if (kbpTriples != null && !kbpTriples.IsEmpty())
					{
						pw.Println();
						pw.Println("Extracted the following KBP triples:");
						foreach (RelationTriple triple in kbpTriples)
						{
							pw.Println(triple);
						}
					}
				}
			}
			else
			{
				IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
				pw.Println("Tokens:");
				pw.Println(annotation.Get(typeof(CoreAnnotations.TextAnnotation)));
				foreach (CoreLabel token in tokens)
				{
					int tokenCharBegin = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					int tokenCharEnd = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					pw.Println("[Text=" + token.Word() + " CharacterOffsetBegin=" + tokenCharBegin + " CharacterOffsetEnd=" + tokenCharEnd + ']');
				}
			}
			// display the old-style doc-level coref annotations
			// this is not supported anymore!
			//String corefAnno = annotation.get(CorefPLAnnotation.class);
			//if(corefAnno != null) os.println(corefAnno);
			// display the new-style coreference graph
			IDictionary<int, CorefChain> corefChains = annotation.Get(typeof(CorefCoreAnnotations.CorefChainAnnotation));
			if (corefChains != null && sentences != null)
			{
				foreach (CorefChain chain in corefChains.Values)
				{
					CorefChain.CorefMention representative = chain.GetRepresentativeMention();
					bool outputHeading = false;
					foreach (CorefChain.CorefMention mention in chain.GetMentionsInTextualOrder())
					{
						if (mention == representative)
						{
							continue;
						}
						if (!outputHeading)
						{
							outputHeading = true;
							pw.Println();
							pw.Println("Coreference set:");
						}
						// all offsets start at 1!
						pw.Printf("\t(%d,%d,[%d,%d]) -> (%d,%d,[%d,%d]), that is: \"%s\" -> \"%s\"%n", mention.sentNum, mention.headIndex, mention.startIndex, mention.endIndex, representative.sentNum, representative.headIndex, representative.startIndex, representative
							.endIndex, mention.mentionSpan, representative.mentionSpan);
					}
				}
			}
			// display quotes if available
			if (annotation.Get(typeof(CoreAnnotations.QuotationsAnnotation)) != null)
			{
				pw.Println();
				pw.Println("Extracted quotes: ");
				IList<ICoreMap> allQuotes = QuoteAnnotator.GatherQuotes(annotation);
				foreach (ICoreMap quote in allQuotes)
				{
					string speakerString;
					if (quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation)) != null)
					{
						speakerString = quote.Get(typeof(QuoteAttributionAnnotator.CanonicalMentionAnnotation));
					}
					else
					{
						if (quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation)) != null)
						{
							speakerString = quote.Get(typeof(QuoteAttributionAnnotator.SpeakerAnnotation));
						}
						else
						{
							speakerString = "Unknown";
						}
					}
					pw.Printf("[QuotationIndex=%d, CharacterOffsetBegin=%d, Text=%s, Speaker=%s]%n", quote.Get(typeof(CoreAnnotations.QuotationIndexAnnotation)), quote.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), quote.Get(typeof(CoreAnnotations.TextAnnotation
						)), speakerString);
				}
			}
			pw.Flush();
		}

		/// <summary>Static helper</summary>
		public static void PrettyPrint(Annotation annotation, OutputStream stream, StanfordCoreNLP pipeline)
		{
			PrettyPrint(annotation, new PrintWriter(stream), pipeline);
		}

		/// <summary>Static helper</summary>
		public static void PrettyPrint(Annotation annotation, PrintWriter pw, StanfordCoreNLP pipeline)
		{
			try
			{
				Edu.Stanford.Nlp.Pipeline.TextOutputter.Print(annotation, pw, GetOptions(pipeline));
			}
			catch (IOException e)
			{
				// already flushed
				// don't close, might not want to close underlying stream
				throw new RuntimeIOException(e);
			}
		}
	}
}

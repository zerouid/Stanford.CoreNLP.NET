using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// This class assumes that there is a
	/// <c>List&lt;CoreLabel&gt;</c>
	/// under the
	/// <c>TokensAnnotation</c>
	/// field,
	/// and runs it through
	/// <see cref="Edu.Stanford.Nlp.Process.WordToSentenceProcessor{IN}"/>
	/// and puts the new
	/// <c>List&lt;Annotation&gt;</c>
	/// under the
	/// <c>SentencesAnnotation</c>
	/// field.
	/// </summary>
	/// <author>Jenny Finkel</author>
	/// <author>Christopher Manning</author>
	public class WordsToSentencesAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator));

		private readonly WordToSentenceProcessor<CoreLabel> wts;

		private readonly bool Verbose;

		private readonly bool countLineNumbers;

		public WordsToSentencesAnnotator()
			: this(false)
		{
		}

		public WordsToSentencesAnnotator(Properties properties)
		{
			bool nlSplitting = bool.ValueOf(properties.GetProperty(StanfordCoreNLP.NewlineSplitterProperty, "false"));
			if (nlSplitting)
			{
				bool whitespaceTokenization = bool.ValueOf(properties.GetProperty("tokenize.whitespace", "false"));
				if (whitespaceTokenization)
				{
					if (Runtime.LineSeparator().Equals("\n"))
					{
						// this constructor will keep empty lines as empty sentences
						WordToSentenceProcessor<CoreLabel> wts1 = new WordToSentenceProcessor<CoreLabel>(ArrayUtils.AsImmutableSet(new string[] { "\n" }));
						this.countLineNumbers = true;
						this.wts = wts1;
					}
					else
					{
						// throw "\n" in just in case files use that instead of
						// the system separator
						// this constructor will keep empty lines as empty sentences
						WordToSentenceProcessor<CoreLabel> wts1 = new WordToSentenceProcessor<CoreLabel>(ArrayUtils.AsImmutableSet(new string[] { Runtime.LineSeparator(), "\n" }));
						this.countLineNumbers = true;
						this.wts = wts1;
					}
				}
				else
				{
					// this constructor will keep empty lines as empty sentences
					WordToSentenceProcessor<CoreLabel> wts1 = new WordToSentenceProcessor<CoreLabel>(ArrayUtils.AsImmutableSet(new string[] { PTBTokenizer.GetNewlineToken() }));
					this.countLineNumbers = true;
					this.wts = wts1;
				}
			}
			else
			{
				string isOneSentence = properties.GetProperty("ssplit.isOneSentence");
				if (bool.ParseBoolean(isOneSentence))
				{
					// this method treats null as false
					// Treat as one sentence: You get a no-op sentence splitter that always returns all tokens as one sentence.
					WordToSentenceProcessor<CoreLabel> wts1 = new WordToSentenceProcessor<CoreLabel>(true);
					this.countLineNumbers = false;
					this.wts = wts1;
				}
				else
				{
					// multi token sentence boundaries
					string boundaryMultiTokenRegex = properties.GetProperty("ssplit.boundaryMultiTokenRegex");
					// Discard these tokens without marking them as sentence boundaries
					string tokenPatternsToDiscardProp = properties.GetProperty("ssplit.tokenPatternsToDiscard");
					ICollection<string> tokenRegexesToDiscard = null;
					if (tokenPatternsToDiscardProp != null)
					{
						string[] toks = tokenPatternsToDiscardProp.Split(",");
						tokenRegexesToDiscard = Generics.NewHashSet(Arrays.AsList(toks));
					}
					// regular boundaries
					string boundaryTokenRegex = properties.GetProperty("ssplit.boundaryTokenRegex");
					string boundaryFollowersRegex = properties.GetProperty("ssplit.boundaryFollowersRegex");
					// newline boundaries which are discarded.
					ICollection<string> boundariesToDiscard = null;
					string bounds = properties.GetProperty("ssplit.boundariesToDiscard");
					if (bounds != null)
					{
						string[] toks = bounds.Split(",");
						boundariesToDiscard = Generics.NewHashSet(Arrays.AsList(toks));
					}
					ICollection<string> htmlElementsToDiscard = null;
					// HTML boundaries which are discarded
					bounds = properties.GetProperty("ssplit.htmlBoundariesToDiscard");
					if (bounds != null)
					{
						string[] elements = bounds.Split(",");
						htmlElementsToDiscard = Generics.NewHashSet(Arrays.AsList(elements));
					}
					string nlsb = properties.GetProperty(StanfordCoreNLP.NewlineIsSentenceBreakProperty, StanfordCoreNLP.DefaultNewlineIsSentenceBreak);
					this.countLineNumbers = false;
					this.wts = new WordToSentenceProcessor<CoreLabel>(boundaryTokenRegex, boundaryFollowersRegex, boundariesToDiscard, htmlElementsToDiscard, WordToSentenceProcessor.StringToNewlineIsSentenceBreak(nlsb), (boundaryMultiTokenRegex != null) ? TokenSequencePattern
						.Compile(boundaryMultiTokenRegex) : null, tokenRegexesToDiscard);
				}
			}
			Verbose = bool.ValueOf(properties.GetProperty("ssplit.verbose", "false"));
		}

		public WordsToSentencesAnnotator(bool verbose)
			: this(verbose, false, new WordToSentenceProcessor<CoreLabel>())
		{
		}

		public WordsToSentencesAnnotator(bool verbose, string boundaryTokenRegex, ICollection<string> boundaryToDiscard, ICollection<string> htmlElementsToDiscard, string newlineIsSentenceBreak, string boundaryMultiTokenRegex, ICollection<string> tokenRegexesToDiscard
			)
			: this(verbose, false, new WordToSentenceProcessor<CoreLabel>(boundaryTokenRegex, null, boundaryToDiscard, htmlElementsToDiscard, WordToSentenceProcessor.StringToNewlineIsSentenceBreak(newlineIsSentenceBreak), (boundaryMultiTokenRegex != null
				) ? TokenSequencePattern.Compile(boundaryMultiTokenRegex) : null, tokenRegexesToDiscard))
		{
		}

		private WordsToSentencesAnnotator(bool verbose, bool countLineNumbers, WordToSentenceProcessor<CoreLabel> wts)
		{
			Verbose = verbose;
			this.countLineNumbers = countLineNumbers;
			this.wts = wts;
		}

		/// <summary>Return a WordsToSentencesAnnotator that splits on newlines (only), which are then deleted.</summary>
		/// <remarks>
		/// Return a WordsToSentencesAnnotator that splits on newlines (only), which are then deleted.
		/// This constructor counts the lines by putting in empty token lists for empty lines.
		/// It tells the underlying splitter to return empty lists of tokens
		/// and then treats those empty lists as empty lines.  We don't
		/// actually include empty sentences in the annotation, though. But they
		/// are used in numbering the sentence. Only this constructor leads to
		/// empty sentences.
		/// </remarks>
		/// <param name="nlToken">
		/// Zero or more new line tokens, which might be a
		/// <literal>\n</literal>
		/// or the fake
		/// newline tokens returned from the tokenizer.
		/// </param>
		/// <returns>A WordsToSentenceAnnotator.</returns>
		public static Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator NewlineSplitter(params string[] nlToken)
		{
			// this constructor will keep empty lines as empty sentences
			WordToSentenceProcessor<CoreLabel> wts = new WordToSentenceProcessor<CoreLabel>(ArrayUtils.AsImmutableSet(nlToken));
			return new Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator(false, true, wts);
		}

		/// <summary>Return a WordsToSentencesAnnotator that never splits the token stream.</summary>
		/// <remarks>Return a WordsToSentencesAnnotator that never splits the token stream. You just get one sentence.</remarks>
		/// <returns>A WordsToSentenceAnnotator.</returns>
		public static Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator NonSplitter()
		{
			WordToSentenceProcessor<CoreLabel> wts = new WordToSentenceProcessor<CoreLabel>(true);
			return new Edu.Stanford.Nlp.Pipeline.WordsToSentencesAnnotator(false, false, wts);
		}

		/// <summary>
		/// If setCountLineNumbers is set to true, we count line numbers by
		/// telling the underlying splitter to return empty lists of tokens
		/// and then treating those empty lists as empty lines.
		/// </summary>
		/// <remarks>
		/// If setCountLineNumbers is set to true, we count line numbers by
		/// telling the underlying splitter to return empty lists of tokens
		/// and then treating those empty lists as empty lines.  We don't
		/// actually include empty sentences in the annotation, though.
		/// </remarks>
		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				log.Info("Sentence splitting ... " + annotation);
			}
			if (!annotation.ContainsKey(typeof(CoreAnnotations.TokensAnnotation)))
			{
				throw new ArgumentException("WordsToSentencesAnnotator: unable to find words/tokens in: " + annotation);
			}
			// get text and tokens from the document
			string text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
			if (Verbose)
			{
				log.Info("Tokens are: " + tokens);
			}
			string docID = annotation.Get(typeof(CoreAnnotations.DocIDAnnotation));
			// assemble the sentence annotations
			int lineNumber = 0;
			// section annotations to mark sentences with
			ICoreMap sectionAnnotations = null;
			IList<ICoreMap> sentences = new List<ICoreMap>();
			// keep track of current section to assign sentences to sections
			int currSectionIndex = 0;
			IList<ICoreMap> sections = annotation.Get(typeof(CoreAnnotations.SectionsAnnotation));
			foreach (IList<CoreLabel> sentenceTokens in wts.Process(tokens))
			{
				if (countLineNumbers)
				{
					++lineNumber;
				}
				if (sentenceTokens.IsEmpty())
				{
					if (!countLineNumbers)
					{
						throw new InvalidOperationException("unexpected empty sentence: " + sentenceTokens);
					}
					else
					{
						continue;
					}
				}
				// get the sentence text from the first and last character offsets
				int begin = sentenceTokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int last = sentenceTokens.Count - 1;
				int end = sentenceTokens[last].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				string sentenceText = Sharpen.Runtime.Substring(text, begin, end);
				// create a sentence annotation with text and token offsets
				Annotation sentence = new Annotation(sentenceText);
				sentence.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), begin);
				sentence.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), end);
				sentence.Set(typeof(CoreAnnotations.TokensAnnotation), sentenceTokens);
				sentence.Set(typeof(CoreAnnotations.SentenceIndexAnnotation), sentences.Count);
				if (countLineNumbers)
				{
					sentence.Set(typeof(CoreAnnotations.LineNumberAnnotation), lineNumber);
				}
				// Annotate sentence with section information.
				// Assume section start and end appear as first and last tokens of sentence
				CoreLabel sentenceStartToken = sentenceTokens[0];
				CoreLabel sentenceEndToken = sentenceTokens[sentenceTokens.Count - 1];
				ICoreMap sectionStart = sentenceStartToken.Get(typeof(CoreAnnotations.SectionStartAnnotation));
				if (sectionStart != null)
				{
					// Section is started
					sectionAnnotations = sectionStart;
				}
				if (sectionAnnotations != null)
				{
					// transfer annotations over to sentence
					ChunkAnnotationUtils.CopyUnsetAnnotations(sectionAnnotations, sentence);
				}
				string sectionEnd = sentenceEndToken.Get(typeof(CoreAnnotations.SectionEndAnnotation));
				if (sectionEnd != null)
				{
					sectionAnnotations = null;
				}
				// determine section index for this sentence if keeping track of sections
				if (sections != null)
				{
					// try to find a section that ends after this sentence ends, check if it encloses sentence
					// if it doesn't, that means this sentence is in two sections
					while (currSectionIndex < sections.Count)
					{
						int currSectionCharBegin = sections[currSectionIndex].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
						int currSectionCharEnd = sections[currSectionIndex].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
						if (currSectionCharEnd < end)
						{
							currSectionIndex++;
						}
						else
						{
							// if the sentence falls in this current section, link it to this section
							if (currSectionCharBegin <= begin)
							{
								// ... but first check if it's in one of this sections quotes!
								// if so mark it as quoted
								foreach (ICoreMap sectionQuote in sections[currSectionIndex].Get(typeof(CoreAnnotations.QuotesAnnotation)))
								{
									if (sectionQuote.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)) <= begin && end <= sectionQuote.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)))
									{
										sentence.Set(typeof(CoreAnnotations.QuotedAnnotation), true);
										// set the author to the quote author
										sentence.Set(typeof(CoreAnnotations.AuthorAnnotation), sectionQuote.Get(typeof(CoreAnnotations.AuthorAnnotation)));
									}
								}
								// add the sentence to the section's sentence list
								sections[currSectionIndex].Get(typeof(CoreAnnotations.SentencesAnnotation)).Add(sentence);
								// set sentence's section date
								string sectionDate = sections[currSectionIndex].Get(typeof(CoreAnnotations.SectionDateAnnotation));
								sentence.Set(typeof(CoreAnnotations.SectionDateAnnotation), sectionDate);
								// set sentence's section index
								sentence.Set(typeof(CoreAnnotations.SectionIndexAnnotation), currSectionIndex);
							}
							break;
						}
					}
				}
				if (docID != null)
				{
					sentence.Set(typeof(CoreAnnotations.DocIDAnnotation), docID);
				}
				int index = 1;
				foreach (CoreLabel token in sentenceTokens)
				{
					token.SetIndex(index++);
					token.SetSentIndex(sentences.Count);
					if (docID != null)
					{
						token.SetDocID(docID);
					}
				}
				// add the sentence to the list
				sentences.Add(sentence);
			}
			// after sentence splitting, remove newline tokens, set token and
			// sentence indexes, and update before and after text appropriately
			// at end of this annotator, it should be as though newline tokens
			// were never used
			// reset token indexes
			IList<CoreLabel> finalTokens = new List<CoreLabel>();
			int tokenIndex = 0;
			CoreLabel prevToken = null;
			foreach (CoreLabel currToken in annotation.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				if (!currToken.IsNewline())
				{
					finalTokens.Add(currToken);
					currToken.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenIndex);
					currToken.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenIndex + 1);
					tokenIndex++;
					// fix before text for this token
					if (prevToken != null && prevToken.IsNewline())
					{
						string currTokenBeforeText = currToken.Get(typeof(CoreAnnotations.BeforeAnnotation));
						string prevTokenText = prevToken.Get(typeof(CoreAnnotations.OriginalTextAnnotation));
						currToken.Set(typeof(CoreAnnotations.BeforeAnnotation), prevTokenText + currTokenBeforeText);
					}
				}
				else
				{
					string newlineText = currToken.Get(typeof(CoreAnnotations.OriginalTextAnnotation));
					// fix after text for last token
					if (prevToken != null)
					{
						string prevTokenAfterText = prevToken.Get(typeof(CoreAnnotations.AfterAnnotation));
						prevToken.Set(typeof(CoreAnnotations.AfterAnnotation), prevTokenAfterText + newlineText);
					}
				}
				prevToken = currToken;
			}
			annotation.Set(typeof(CoreAnnotations.TokensAnnotation), finalTokens);
			// set sentence token begin and token end values
			foreach (ICoreMap sentence_1 in sentences)
			{
				IList<CoreLabel> sentenceTokens_1 = sentence_1.Get(typeof(CoreAnnotations.TokensAnnotation));
				int sentenceTokenBegin = sentenceTokens_1[0].Get(typeof(CoreAnnotations.TokenBeginAnnotation));
				int sentenceTokenEnd = sentenceTokens_1[sentenceTokens_1.Count - 1].Get(typeof(CoreAnnotations.TokenEndAnnotation));
				sentence_1.Set(typeof(CoreAnnotations.TokenBeginAnnotation), sentenceTokenBegin);
				sentence_1.Set(typeof(CoreAnnotations.TokenEndAnnotation), sentenceTokenEnd);
			}
			// add the sentences annotations to the document
			annotation.Set(typeof(CoreAnnotations.SentencesAnnotation), sentences);
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.UnmodifiableSet(new ArraySet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation
				), typeof(CoreAnnotations.IsNewlineAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation))));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.SentencesAnnotation), typeof(CoreAnnotations.SentenceIndexAnnotation)));
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Ling.Tokensregex;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.Lang;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// An annotator which removes all XML tags (as identified by the
	/// tokenizer) and possibly selectively keeps the text between them.
	/// </summary>
	/// <remarks>
	/// An annotator which removes all XML tags (as identified by the
	/// tokenizer) and possibly selectively keeps the text between them.
	/// Can also add sentence-ending markers depending on the XML tag.
	/// Note that the removal of tags is done by a finite state tokenizer.
	/// Thus, this works for simple, typical XML, or equally for similar
	/// SGML or XML tags, but will not work on arbitrarily complicated XML.
	/// </remarks>
	/// <author>John Bauer</author>
	/// <author>Angel Chang</author>
	public class CleanXmlAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator));

		private const bool Debug = false;

		/// <summary>
		/// A regular expression telling us where to look for tokens
		/// we care about.
		/// </summary>
		/// <remarks>
		/// A regular expression telling us where to look for tokens
		/// we care about. If this Pattern only matches certain XML elements, then text in other
		/// elements will be discarded. Text outside any element will be kept iff the pattern matches "".
		/// </remarks>
		private readonly Pattern xmlTagMatcher;

		public const string DefaultXmlTags = ".*";

		/// <summary>This regular expression tells us which tags end a sentence.</summary>
		/// <remarks>
		/// This regular expression tells us which tags end a sentence.
		/// For example,
		/// <c>"p"</c>
		/// would be a great candidate.
		/// The pattern should match element names not tags (i.e., you don't include the angle brackets).
		/// </remarks>
		private readonly Pattern sentenceEndingTagMatcher;

		public const string DefaultSentenceEnders = string.Empty;

		/// <summary>This tells us what tags denote single sentences (tokens inside should not be sentence split on)</summary>
		private Pattern singleSentenceTagMatcher;

		public static readonly string DefaultSingleSentenceTags = null;

		/// <summary>This tells us which XML tags wrap document date.</summary>
		private readonly Pattern dateTagMatcher;

		public const string DefaultDateTags = "datetime|date";

		/// <summary>This tells us which XML tags wrap document id</summary>
		private Pattern docIdTagMatcher;

		public const string DefaultDocidTags = "docid";

		/// <summary>This tells us which XML tags wrap document type</summary>
		private Pattern docTypeTagMatcher;

		public const string DefaultDoctypeTags = "doctype";

		/// <summary>This tells us when an utterance turn starts (used in dcoref).</summary>
		private Pattern utteranceTurnTagMatcher;

		public const string DefaultUtteranceTurnTags = "turn";

		/// <summary>This tells us what the speaker tag is (used in dcoref).</summary>
		private Pattern speakerTagMatcher;

		public const string DefaultSpeakerTags = "speaker";

		/// <summary>
		/// A map of document level annotation keys (i.e., docid) along with a pattern
		/// indicating the tag to match, and the attribute to match.
		/// </summary>
		private readonly CollectionValuedMap<Type, Pair<Pattern, Pattern>> docAnnotationPatterns = new CollectionValuedMap<Type, Pair<Pattern, Pattern>>();

		public const string DefaultDocAnnotationsPatterns = "docID=doc[id],doctype=doc[type],docsourcetype=doctype[source]";

		/// <summary>
		/// A map of token level annotation keys (i.e., link, speaker) along with a pattern
		/// indicating the tag/attribute to match (tokens that belong to the text enclosed in the specified tag will be annotated).
		/// </summary>
		private readonly CollectionValuedMap<Type, Pair<Pattern, Pattern>> tokenAnnotationPatterns = new CollectionValuedMap<Type, Pair<Pattern, Pattern>>();

		public static readonly string DefaultTokenAnnotationsPatterns = null;

		/// <summary>This tells us what the section tag is.</summary>
		private Pattern sectionTagMatcher;

		public static readonly string DefaultSectionTags = null;

		/// <summary>This tells us what the quote tag is.</summary>
		private Pattern quoteTagMatcher;

		public static readonly string DefaultQuoteTags = null;

		/// <summary>This tells us the attribute names that indicates quote author</summary>
		private string[] quoteAuthorAttributeNames = StringUtils.EmptyStringArray;

		public const string DefaultQuoteAuthorAttributes = string.Empty;

		/// <summary>This tells us what tokens will be discarded by ssplit.</summary>
		private Pattern ssplitDiscardTokensMatcher;

		/// <summary>
		/// A map of section level annotation keys (i.e., docid) along with a pattern
		/// indicating the tag to match, and the attribute to match.
		/// </summary>
		private readonly CollectionValuedMap<Type, Pair<Pattern, Pattern>> sectionAnnotationPatterns = new CollectionValuedMap<Type, Pair<Pattern, Pattern>>();

		public static readonly string DefaultSectionAnnotationsPatterns = null;

		/// <summary>This setting allows handling of "flawed XML", which may be valid SGML.</summary>
		/// <remarks>
		/// This setting allows handling of "flawed XML", which may be valid SGML.  For example,
		/// a lot of the news articles we parse go: <br />
		/// &lt;text&gt; <br />
		/// &lt;turn&gt; <br />
		/// &lt;turn&gt; <br />
		/// &lt;turn&gt; <br />
		/// &lt;/text&gt; <br />
		/// ... i.e., no closing &lt;/turn&gt; tags.
		/// </remarks>
		private readonly bool allowFlawedXml;

		public const bool DefaultAllowFlaws = true;

		public CleanXmlAnnotator()
			: this(DefaultXmlTags, DefaultSentenceEnders, DefaultDateTags, DefaultAllowFlaws)
		{
		}

		public CleanXmlAnnotator(Properties properties)
		{
			// = null;
			// = null;
			// = null;
			// = null;
			// = null;
			string xmlElementsToProcess = properties.GetProperty("clean.xmltags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultXmlTags);
			string sentenceEndingTags = properties.GetProperty("clean.sentenceendingtags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultSentenceEnders);
			string singleSentenceTags = properties.GetProperty("clean.singlesentencetags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultSingleSentenceTags);
			string allowFlawedString = properties.GetProperty("clean.allowflawedxml");
			bool allowFlawed = Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultAllowFlaws;
			if (allowFlawedString != null)
			{
				allowFlawed = bool.ValueOf(allowFlawedString);
			}
			string dateTags = properties.GetProperty("clean.datetags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultDateTags);
			string docIdTags = properties.GetProperty("clean.docIdtags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultDocidTags);
			string docTypeTags = properties.GetProperty("clean.docTypetags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultDoctypeTags);
			string utteranceTurnTags = properties.GetProperty("clean.turntags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultUtteranceTurnTags);
			string speakerTags = properties.GetProperty("clean.speakertags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultSpeakerTags);
			string docAnnotations = properties.GetProperty("clean.docAnnotations", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultDocAnnotationsPatterns);
			string tokenAnnotations = properties.GetProperty("clean.tokenAnnotations", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultTokenAnnotationsPatterns);
			string sectionTags = properties.GetProperty("clean.sectiontags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultSectionTags);
			string sectionAnnotations = properties.GetProperty("clean.sectionAnnotations", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultSectionAnnotationsPatterns);
			string quoteTags = properties.GetProperty("clean.quotetags", Edu.Stanford.Nlp.Pipeline.CleanXmlAnnotator.DefaultQuoteTags);
			string ssplitDiscardTokens = properties.GetProperty("clean.ssplitDiscardTokens");
			string quoteAuthorAttributes = properties.GetProperty("clean.quoteauthorattributes", DefaultQuoteAuthorAttributes);
			if (xmlElementsToProcess != null)
			{
				xmlTagMatcher = ToCaseInsensitivePattern(xmlElementsToProcess);
				if (StringUtils.IsNullOrEmpty(sentenceEndingTags))
				{
					sentenceEndingTagMatcher = null;
				}
				else
				{
					sentenceEndingTagMatcher = ToCaseInsensitivePattern(sentenceEndingTags);
				}
			}
			else
			{
				xmlTagMatcher = null;
				sentenceEndingTagMatcher = null;
			}
			dateTagMatcher = ToCaseInsensitivePattern(dateTags);
			this.allowFlawedXml = allowFlawed;
			SetSingleSentenceTagMatcher(singleSentenceTags);
			SetDocIdTagMatcher(docIdTags);
			SetDocTypeTagMatcher(docTypeTags);
			SetDiscourseTags(utteranceTurnTags, speakerTags);
			SetDocAnnotationPatterns(docAnnotations);
			SetTokenAnnotationPatterns(tokenAnnotations);
			SetSectionTagMatcher(sectionTags);
			SetSectionAnnotationPatterns(sectionAnnotations);
			SetQuoteTagMatcher(quoteTags);
			SetSsplitDiscardTokensMatcher(ssplitDiscardTokens);
			// set up the labels that indicate quote author
			quoteAuthorAttributeNames = quoteAuthorAttributes.Split(",");
		}

		public CleanXmlAnnotator(string xmlTagsToRemove, string sentenceEndingTags, string dateTags, bool allowFlawedXml)
		{
			this.allowFlawedXml = allowFlawedXml;
			if (xmlTagsToRemove != null)
			{
				xmlTagMatcher = ToCaseInsensitivePattern(xmlTagsToRemove);
				if (StringUtils.IsNullOrEmpty(sentenceEndingTags))
				{
					sentenceEndingTagMatcher = null;
				}
				else
				{
					sentenceEndingTagMatcher = ToCaseInsensitivePattern(sentenceEndingTags);
				}
			}
			else
			{
				xmlTagMatcher = null;
				sentenceEndingTagMatcher = null;
			}
			dateTagMatcher = ToCaseInsensitivePattern(dateTags);
		}

		private static Pattern ToCaseInsensitivePattern(string tags)
		{
			if (tags != null)
			{
				return Pattern.Compile(tags, Pattern.CaseInsensitive | Pattern.UnicodeCase);
			}
			else
			{
				return null;
			}
		}

		public virtual void SetSsplitDiscardTokensMatcher(string tags)
		{
			ssplitDiscardTokensMatcher = ToCaseInsensitivePattern(tags);
		}

		public virtual void SetSingleSentenceTagMatcher(string tags)
		{
			singleSentenceTagMatcher = ToCaseInsensitivePattern(tags);
		}

		public virtual void SetDocIdTagMatcher(string docIdTags)
		{
			docIdTagMatcher = ToCaseInsensitivePattern(docIdTags);
		}

		public virtual void SetDocTypeTagMatcher(string docTypeTags)
		{
			docTypeTagMatcher = ToCaseInsensitivePattern(docTypeTags);
		}

		public virtual void SetSectionTagMatcher(string sectionTags)
		{
			sectionTagMatcher = ToCaseInsensitivePattern(sectionTags);
		}

		public virtual void SetQuoteTagMatcher(string quoteTags)
		{
			quoteTagMatcher = ToCaseInsensitivePattern(quoteTags);
		}

		public virtual void SetDiscourseTags(string utteranceTurnTags, string speakerTags)
		{
			utteranceTurnTagMatcher = ToCaseInsensitivePattern(utteranceTurnTags);
			speakerTagMatcher = ToCaseInsensitivePattern(speakerTags);
		}

		public virtual void SetDocAnnotationPatterns(string conf)
		{
			docAnnotationPatterns.Clear();
			// Patterns can only be tag attributes
			AddAnnotationPatterns(docAnnotationPatterns, conf, true);
		}

		public virtual void SetTokenAnnotationPatterns(string conf)
		{
			tokenAnnotationPatterns.Clear();
			// Patterns can only be tag attributes
			AddAnnotationPatterns(tokenAnnotationPatterns, conf, true);
		}

		public virtual void SetSectionAnnotationPatterns(string conf)
		{
			sectionAnnotationPatterns.Clear();
			AddAnnotationPatterns(sectionAnnotationPatterns, conf, false);
		}

		private static readonly Pattern TagAttrPattern = Pattern.Compile("(.*)\\[(.*)\\]");

		private static void AddAnnotationPatterns(CollectionValuedMap<Type, Pair<Pattern, Pattern>> annotationPatterns, string conf, bool attrOnly)
		{
			string[] annoPatternStrings = conf == null ? StringUtils.EmptyStringArray : conf.Trim().Split("\\s*,\\s*");
			foreach (string annoPatternString in annoPatternStrings)
			{
				string[] annoPattern = annoPatternString.Split("\\s*=\\s*", 2);
				if (annoPattern.Length != 2)
				{
					throw new ArgumentException("Invalid annotation to tag pattern: " + annoPatternString);
				}
				string annoKeyString = annoPattern[0];
				string pattern = annoPattern[1];
				Type annoKey = EnvLookup.LookupAnnotationKeyWithClassname(null, annoKeyString);
				if (annoKey == null)
				{
					throw new ArgumentException("Cannot resolve annotation key " + annoKeyString);
				}
				Matcher m = TagAttrPattern.Matcher(pattern);
				if (m.Matches())
				{
					Pattern tagPattern = ToCaseInsensitivePattern(m.Group(1));
					Pattern attrPattern = ToCaseInsensitivePattern(m.Group(2));
					annotationPatterns.Add(annoKey, Pair.MakePair(tagPattern, attrPattern));
				}
				else
				{
					if (attrOnly)
					{
						// attribute is require
						throw new ArgumentException("Invalid tag pattern: " + pattern + " for annotation key " + annoKeyString);
					}
					else
					{
						Pattern tagPattern = ToCaseInsensitivePattern(pattern);
						annotationPatterns.Add(annoKey, Pair.MakePair(tagPattern, null));
					}
				}
			}
		}

		/// <summary>Helper method to set the TokenBeginAnnotation and TokenEndAnnotation of every token.</summary>
		public virtual void SetTokenBeginTokenEnd(IList<CoreLabel> tokensList)
		{
			int tokenIndex = 0;
			foreach (CoreLabel token in tokensList)
			{
				token.Set(typeof(CoreAnnotations.TokenBeginAnnotation), tokenIndex);
				token.Set(typeof(CoreAnnotations.TokenEndAnnotation), tokenIndex + 1);
				tokenIndex++;
			}
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (annotation.ContainsKey(typeof(CoreAnnotations.TokensAnnotation)))
			{
				IList<CoreLabel> tokens = annotation.Get(typeof(CoreAnnotations.TokensAnnotation));
				IList<CoreLabel> newTokens = Process(annotation, tokens);
				// We assume that if someone is using this annotator, they don't
				// want the old tokens any more and get rid of them
				// redo the token indexes if xml tokens have been removed
				SetTokenBeginTokenEnd(newTokens);
				annotation.Set(typeof(CoreAnnotations.TokensAnnotation), newTokens);
			}
		}

		public virtual IList<CoreLabel> Process(IList<CoreLabel> tokens)
		{
			return Process(null, tokens);
		}

		private static string TokensToString(Annotation annotation, IList<CoreLabel> tokens)
		{
			if (tokens.IsEmpty())
			{
				return string.Empty;
			}
			// Try to get original text back?
			string annotationText = (annotation != null) ? annotation.Get(typeof(CoreAnnotations.TextAnnotation)) : null;
			if (annotationText != null)
			{
				CoreLabel firstToken = tokens[0];
				CoreLabel lastToken = tokens[tokens.Count - 1];
				int firstCharOffset = firstToken.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				int lastCharOffset = lastToken.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				return Sharpen.Runtime.Substring(annotationText, firstCharOffset, lastCharOffset);
			}
			else
			{
				return StringUtils.JoinWords(tokens, " ");
			}
		}

		// Annotates CoreMap with information from xml tag
		/// <summary>Updates a CoreMap with attributes (or text context) from a tag.</summary>
		/// <param name="annotation">- Main document level annotation (from which the original text can be extracted)</param>
		/// <param name="cm">- CoreMap to annotate</param>
		/// <param name="tag">- tag to process</param>
		/// <param name="annotationPatterns">- list of annotation patterns to match</param>
		/// <param name="savedTokens">- tokens for annotations that are text context of a tag</param>
		/// <param name="toAnnotate">- what keys to annotate</param>
		/// <returns>The set of annotations found</returns>
		private static ICollection<Type> AnnotateWithTag(Annotation annotation, ICoreMap cm, XMLUtils.XMLTag tag, CollectionValuedMap<Type, Pair<Pattern, Pattern>> annotationPatterns, IDictionary<Type, IList<CoreLabel>> savedTokens, ICollection<Type
			> toAnnotate, IDictionary<Type, Stack<Pair<string, string>>> savedTokenAnnotations)
		{
			ICollection<Type> foundAnnotations = new HashSet<Type>();
			if (annotationPatterns == null)
			{
				return foundAnnotations;
			}
			if (toAnnotate == null)
			{
				toAnnotate = annotationPatterns.Keys;
			}
			foreach (Type key in toAnnotate)
			{
				foreach (Pair<Pattern, Pattern> pattern in annotationPatterns[key])
				{
					Pattern tagPattern = pattern.first;
					Pattern attrPattern = pattern.second;
					if (tagPattern.Matcher(tag.name).Matches())
					{
						bool matched = false;
						if (attrPattern != null)
						{
							if (tag.attributes != null)
							{
								foreach (KeyValuePair<string, string> entry in tag.attributes)
								{
									if (attrPattern.Matcher(entry.Key).Matches())
									{
										if (savedTokenAnnotations != null)
										{
											Stack<Pair<string, string>> stack = savedTokenAnnotations.ComputeIfAbsent(key, null);
											stack.Push(Pair.MakePair(tag.name, entry.Value));
										}
										cm.Set(key, entry.Value);
										foundAnnotations.Add(key);
										matched = true;
										break;
									}
								}
							}
							if (savedTokenAnnotations != null)
							{
								if (tag.isEndTag)
								{
									// tag ended - clear this annotation
									Stack<Pair<string, string>> stack = savedTokenAnnotations[key];
									if (stack != null && !stack.IsEmpty())
									{
										Pair<string, string> p = stack.Peek();
										if (Sharpen.Runtime.EqualsIgnoreCase(p.first, tag.name))
										{
											stack.Pop();
											if (!stack.IsEmpty())
											{
												cm.Set(key, stack.Peek().second);
											}
											else
											{
												cm.Remove(key);
											}
										}
									}
								}
							}
						}
						else
						{
							if (savedTokens != null)
							{
								if (tag.isEndTag && !tag.isSingleTag)
								{
									// End tag - annotate using saved tokens
									IList<CoreLabel> saved = Sharpen.Collections.Remove(savedTokens, key);
									if (saved != null && !saved.IsEmpty())
									{
										cm.Set(key, TokensToString(annotation, saved));
										foundAnnotations.Add(key);
										matched = true;
									}
								}
								else
								{
									// Start tag
									savedTokens[key] = new List<CoreLabel>();
								}
							}
						}
						if (matched)
						{
							break;
						}
					}
				}
			}
			return foundAnnotations;
		}

		private IList<CoreLabel> Process(Annotation annotation, IList<CoreLabel> tokens)
		{
			// As we are processing, this stack keeps track of which tags we
			// are currently inside
			Stack<string> enclosingTags = new Stack<string>();
			// here we keep track of the current enclosingTags
			// this lets multiple tokens reuse the same tag stack
			IList<string> currentTagSet = null;
			// How many matching tags we've seen
			int matchDepth = 0;
			// stores the filtered tags as we go
			IList<CoreLabel> newTokens = new List<CoreLabel>();
			// we use this to store the before & after annotations if the
			// tokens were tokenized for "invertible"
			StringBuilder removedText = new StringBuilder();
			// we keep track of this so we can look at the last tag after
			// we're outside the loop
			// Keeps track of what we still need to doc level annotations
			// we still need to look for
			ICollection<Type> toAnnotate = new HashSet<Type>(docAnnotationPatterns.Keys);
			int utteranceIndex = 0;
			bool inUtterance = false;
			bool inSpeakerTag = false;
			string currentSpeaker = null;
			IList<CoreLabel> speakerTokens = new List<CoreLabel>();
			IList<CoreLabel> docDateTokens = new List<CoreLabel>();
			IList<CoreLabel> docTypeTokens = new List<CoreLabel>();
			IList<CoreLabel> docIdTokens = new List<CoreLabel>();
			// Local variables for additional per token annotations
			ICoreMap tokenAnnotations = (tokenAnnotationPatterns != null && !tokenAnnotationPatterns.IsEmpty()) ? new ArrayCoreMap() : null;
			IDictionary<Type, Stack<Pair<string, string>>> savedTokenAnnotations = new ArrayMap<Type, Stack<Pair<string, string>>>();
			// Local variables for annotating sections
			XMLUtils.XMLTag sectionStartTag = null;
			int sectionStartTagCharBegin = -1;
			CoreLabel sectionStartToken = null;
			CoreLabel sectionStartTagToken = null;
			ICoreMap sectionAnnotations = null;
			IDictionary<Type, IList<CoreLabel>> savedTokensForSection = new Dictionary<Type, IList<CoreLabel>>();
			// store section quotes
			IList<ICoreMap> sectionQuotes = null;
			// Local variables for annotating quotes
			// XMLUtils.XMLTag quoteStartTag = null;
			string quoteAuthor = null;
			int quoteStartCharOffset = -1;
			annotation.Set(typeof(CoreAnnotations.SectionsAnnotation), new List<ICoreMap>());
			bool markSingleSentence = false;
			foreach (CoreLabel token in tokens)
			{
				string word = token.Word().Trim();
				XMLUtils.XMLTag tag = XMLUtils.ParseTag(word);
				// If it's not a tag, we do manipulations such as unescaping
				if (tag == null)
				{
					// TODO: put this into the lexer instead of here
					token.SetWord(XMLUtils.UnescapeStringForXML(token.Word()));
					// TODO: was there another annotation that also represents the word?
					if (matchDepth > 0 || xmlTagMatcher == null || xmlTagMatcher.Matcher(string.Empty).Matches())
					{
						newTokens.Add(token);
						if (inUtterance)
						{
							token.Set(typeof(CoreAnnotations.UtteranceAnnotation), utteranceIndex);
							if (currentSpeaker != null)
							{
								token.Set(typeof(CoreAnnotations.SpeakerAnnotation), currentSpeaker);
							}
						}
						if (markSingleSentence)
						{
							token.Set(typeof(CoreAnnotations.ForcedSentenceUntilEndAnnotation), true);
							markSingleSentence = false;
						}
						if (tokenAnnotations != null)
						{
							ChunkAnnotationUtils.CopyUnsetAnnotations(tokenAnnotations, token);
						}
					}
					// if we removed any text, and the tokens are "invertible" and
					// therefore keep track of their before/after text, append
					// what we removed to the appropriate tokens
					if (removedText.Length > 0)
					{
						bool added = false;
						string before = token.Get(typeof(CoreAnnotations.BeforeAnnotation));
						if (before != null)
						{
							token.Set(typeof(CoreAnnotations.BeforeAnnotation), removedText + before);
							added = true;
						}
						if (added && newTokens.Count > 1)
						{
							CoreLabel previous = newTokens[newTokens.Count - 2];
							string after = previous.Get(typeof(CoreAnnotations.AfterAnnotation));
							if (after != null)
							{
								previous.Set(typeof(CoreAnnotations.AfterAnnotation), after + removedText);
							}
							else
							{
								previous.Set(typeof(CoreAnnotations.AfterAnnotation), removedText.ToString());
							}
						}
						removedText = new StringBuilder();
					}
					if (currentTagSet == null)
					{
						// We wrap the list in an unmodifiable list because we reuse
						// the same list object many times.  We don't want to
						// let someone modify one list and screw up all the others.
						currentTagSet = Java.Util.Collections.UnmodifiableList(new List<string>(enclosingTags));
					}
					token.Set(typeof(CoreAnnotations.XmlContextAnnotation), currentTagSet);
					// is this token part of the doc date sequence?
					if (dateTagMatcher != null && !currentTagSet.IsEmpty() && dateTagMatcher.Matcher(currentTagSet[currentTagSet.Count - 1]).Matches())
					{
						docDateTokens.Add(token);
					}
					if (docIdTagMatcher != null && !currentTagSet.IsEmpty() && docIdTagMatcher.Matcher(currentTagSet[currentTagSet.Count - 1]).Matches())
					{
						docIdTokens.Add(token);
					}
					if (docTypeTagMatcher != null && !currentTagSet.IsEmpty() && docTypeTagMatcher.Matcher(currentTagSet[currentTagSet.Count - 1]).Matches())
					{
						docTypeTokens.Add(token);
					}
					if (inSpeakerTag)
					{
						speakerTokens.Add(token);
					}
					if (sectionStartTag != null)
					{
						bool okay = true;
						if (ssplitDiscardTokensMatcher != null)
						{
							okay = !ssplitDiscardTokensMatcher.Matcher(token.Word()).Matches();
						}
						if (okay)
						{
							if (sectionStartToken == null)
							{
								sectionStartToken = token;
							}
							// Add tokens to saved section tokens
							foreach (IList<CoreLabel> saved in savedTokensForSection.Values)
							{
								saved.Add(token);
							}
						}
					}
					continue;
				}
				// At this point, we know we have a tag
				// we are removing a token and its associated text...
				// keep track of that
				string currentRemoval = token.Get(typeof(CoreAnnotations.BeforeAnnotation));
				if (currentRemoval != null)
				{
					removedText.Append(currentRemoval);
				}
				currentRemoval = token.Get(typeof(CoreAnnotations.OriginalTextAnnotation));
				if (currentRemoval != null)
				{
					removedText.Append(currentRemoval);
				}
				if (token == tokens[tokens.Count - 1])
				{
					currentRemoval = token.Get(typeof(CoreAnnotations.AfterAnnotation));
					if (currentRemoval != null)
					{
						removedText.Append(currentRemoval);
					}
				}
				// Process tag
				// Check if we want to annotate anything using the tags's attributes
				if (!toAnnotate.IsEmpty() && tag.attributes != null)
				{
					ICollection<Type> foundAnnotations = AnnotateWithTag(annotation, annotation, tag, docAnnotationPatterns, null, toAnnotate, null);
					toAnnotate.RemoveAll(foundAnnotations);
				}
				// Check if the tag matches a quote
				if (quoteTagMatcher != null && quoteTagMatcher.Matcher(tag.name).Matches())
				{
					if (tag.isEndTag)
					{
						// only store quote info if currently processing a section
						if (sectionQuotes != null)
						{
							ICoreMap currQuote = new ArrayCoreMap();
							// set the quote start
							currQuote.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), quoteStartCharOffset);
							int quoteEndCharOffset = token.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
							currQuote.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), quoteEndCharOffset);
							currQuote.Set(typeof(CoreAnnotations.AuthorAnnotation), quoteAuthor);
							// add this quote to the list of quotes in this section
							// a Quote has character offsets and an author
							sectionQuotes.Add(currQuote);
							// quoteStartTag = null;
							quoteStartCharOffset = -1;
							quoteAuthor = null;
						}
					}
					else
					{
						// set quote start offset
						quoteStartCharOffset = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
						// set quote author
						foreach (string quoteAuthorAttribute in quoteAuthorAttributeNames)
						{
							if (tag.attributes.Contains(quoteAuthorAttribute))
							{
								quoteAuthor = tag.attributes[quoteAuthorAttribute];
								break;
							}
						}
					}
				}
				// store quote start tag
				// quoteStartTag = tag;
				// Check if the tag matches a section
				if (sectionTagMatcher != null && sectionTagMatcher.Matcher(tag.name).Matches())
				{
					if (tag.isEndTag)
					{
						// sometimes there is malformed xml (post within post)
						// only store section info if sectionStartTag is not null
						// if sectionStartTag is null something has gone wrong, like posts within posts, etc...
						if (sectionStartTag != null)
						{
							AnnotateWithTag(annotation, sectionAnnotations, tag, sectionAnnotationPatterns, savedTokensForSection, null, null);
							// create a CoreMap to store info about this section
							ICoreMap currSectionCoreMap = new ArrayCoreMap();
							if (sectionStartToken != null)
							{
								sectionStartToken.Set(typeof(CoreAnnotations.SectionStartAnnotation), sectionAnnotations);
								// set character offset info for this section
								currSectionCoreMap.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), sectionStartToken.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)));
							}
							else
							{
								// handle case where section has 0 tokens (for instance post that just contains an img)
								currSectionCoreMap.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), -1);
							}
							// Mark previous token as forcing sentence and section end
							if (!newTokens.IsEmpty())
							{
								CoreLabel previous = newTokens[newTokens.Count - 1];
								previous.Set(typeof(CoreAnnotations.ForcedSentenceEndAnnotation), true);
								previous.Set(typeof(CoreAnnotations.SectionEndAnnotation), sectionStartTag.name);
								// set character offset info for this section
								currSectionCoreMap.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), previous.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)));
							}
							else
							{
								// handle case where section has 0 tokens (for instance post that just contains an img)
								currSectionCoreMap.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), -1);
							}
							// set author of this section
							string foundAuthor = sectionAnnotations.Get(typeof(CoreAnnotations.AuthorAnnotation));
							currSectionCoreMap.Set(typeof(CoreAnnotations.AuthorAnnotation), foundAuthor);
							// get author mention info
							if (foundAuthor != null)
							{
								Pattern p = Pattern.Compile(foundAuthor, Pattern.Literal);
								Matcher matcher = p.Matcher(sectionStartTagToken.Word());
								if (matcher.Find())
								{
									int authorMentionStart = matcher.Start() + sectionStartTagCharBegin;
									int authorMentionEnd = matcher.End() + sectionStartTagCharBegin;
									// set the author mention offsets
									currSectionCoreMap.Set(typeof(CoreAnnotations.SectionAuthorCharacterOffsetBeginAnnotation), authorMentionStart);
									currSectionCoreMap.Set(typeof(CoreAnnotations.SectionAuthorCharacterOffsetEndAnnotation), authorMentionEnd);
								}
							}
							// add the tag for the section
							currSectionCoreMap.Set(typeof(CoreAnnotations.SectionTagAnnotation), sectionStartTagToken);
							// set up empty sentences list
							currSectionCoreMap.Set(typeof(CoreAnnotations.SentencesAnnotation), new List<ICoreMap>());
							// set doc date for post
							string dateString = sectionAnnotations.Get(typeof(CoreAnnotations.SectionDateAnnotation));
							currSectionCoreMap.Set(typeof(CoreAnnotations.SectionDateAnnotation), dateString);
							// add the quotes list
							currSectionCoreMap.Set(typeof(CoreAnnotations.QuotesAnnotation), sectionQuotes);
							// add this to the list of sections
							annotation.Get(typeof(CoreAnnotations.SectionsAnnotation)).Add(currSectionCoreMap);
							// finish processing section
							savedTokensForSection.Clear();
							sectionStartTag = null;
							sectionStartTagCharBegin = -1;
							sectionStartToken = null;
							sectionStartTagToken = null;
							sectionAnnotations = null;
							sectionQuotes = null;
						}
					}
					else
					{
						if (!tag.isSingleTag)
						{
							// Prepare to mark first token with section information
							sectionStartTag = tag;
							sectionStartTagCharBegin = token.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
							sectionStartTagToken = token;
							sectionAnnotations = new ArrayCoreMap();
							sectionAnnotations.Set(typeof(CoreAnnotations.SectionAnnotation), sectionStartTag.name);
							sectionQuotes = new List<ICoreMap>();
						}
					}
				}
				if (sectionStartTag != null)
				{
					// store away annotations for section
					AnnotateWithTag(annotation, sectionAnnotations, tag, sectionAnnotationPatterns, savedTokensForSection, null, null);
				}
				if (tokenAnnotations != null)
				{
					AnnotateWithTag(annotation, tokenAnnotations, tag, tokenAnnotationPatterns, null, null, savedTokenAnnotations);
				}
				// If the tag matches the sentence ending tags, and we have some
				// existing words, mark that word as being somewhere we want
				// to end the sentence.
				if (sentenceEndingTagMatcher != null && sentenceEndingTagMatcher.Matcher(tag.name).Matches() && !newTokens.IsEmpty())
				{
					CoreLabel previous = newTokens[newTokens.Count - 1];
					previous.Set(typeof(CoreAnnotations.ForcedSentenceEndAnnotation), true);
				}
				if (utteranceTurnTagMatcher != null && utteranceTurnTagMatcher.Matcher(tag.name).Matches())
				{
					if (!newTokens.IsEmpty())
					{
						// Utterance turn is also sentence ending
						CoreLabel previous = newTokens[newTokens.Count - 1];
						previous.Set(typeof(CoreAnnotations.ForcedSentenceEndAnnotation), true);
					}
					inUtterance = !(tag.isEndTag || tag.isSingleTag);
					if (inUtterance)
					{
						utteranceIndex++;
					}
					if (!inUtterance)
					{
						currentSpeaker = null;
					}
				}
				if (speakerTagMatcher != null && speakerTagMatcher.Matcher(tag.name).Matches())
				{
					if (!newTokens.IsEmpty())
					{
						// Speaker is not really part of sentence
						CoreLabel previous = newTokens[newTokens.Count - 1];
						previous.Set(typeof(CoreAnnotations.ForcedSentenceEndAnnotation), true);
					}
					inSpeakerTag = !(tag.isEndTag || tag.isSingleTag);
					if (tag.isEndTag)
					{
						currentSpeaker = TokensToString(annotation, speakerTokens);
						MultiTokenTag.Tag mentionTag = new MultiTokenTag.Tag(currentSpeaker, "Speaker", speakerTokens.Count);
						int i = 0;
						foreach (CoreLabel t in speakerTokens)
						{
							t.Set(typeof(CoreAnnotations.SpeakerAnnotation), currentSpeaker);
							t.Set(typeof(CoreAnnotations.MentionTokenAnnotation), new MultiTokenTag(mentionTag, i));
							i++;
						}
					}
					else
					{
						currentSpeaker = null;
					}
					speakerTokens.Clear();
				}
				if (singleSentenceTagMatcher != null && singleSentenceTagMatcher.Matcher(tag.name).Matches())
				{
					if (tag.isEndTag)
					{
						// Mark previous token as forcing sentence end
						if (!newTokens.IsEmpty())
						{
							CoreLabel previous = newTokens[newTokens.Count - 1];
							previous.Set(typeof(CoreAnnotations.ForcedSentenceEndAnnotation), true);
						}
						markSingleSentence = false;
					}
					else
					{
						if (!tag.isSingleTag)
						{
							// Enforce rest of the tokens to be single token until ForceSentenceEnd is seen
							markSingleSentence = true;
						}
					}
				}
				if (xmlTagMatcher == null)
				{
					continue;
				}
				if (tag.isSingleTag)
				{
					continue;
				}
				// at this point, we can't reuse the "currentTagSet" vector
				// any more, since the current tag set has changed
				currentTagSet = null;
				if (tag.isEndTag)
				{
					while (true)
					{
						if (enclosingTags.IsEmpty())
						{
							string mesg = "Got a close tag </" + tag.name + "> which does not match any open tag";
							if (allowFlawedXml)
							{
								log.Warn(mesg);
								break;
							}
							else
							{
								throw new ArgumentException(mesg);
							}
						}
						string lastTag = enclosingTags.Pop();
						if (xmlTagMatcher.Matcher(lastTag).Matches())
						{
							matchDepth--;
						}
						if (lastTag.Equals(tag.name))
						{
							break;
						}
						string mesg_1 = "Mismatched tags: </" + tag.name + "> closed a <" + lastTag + "> tag.";
						if (!allowFlawedXml)
						{
							throw new ArgumentException(mesg_1);
						}
						else
						{
							log.Warn(mesg_1);
						}
					}
					if (matchDepth < 0)
					{
						// this should be impossible, since we already assert that
						// the tags match up correctly
						throw new AssertionError("Programming error?  We think there " + "have been more close tags than open tags");
					}
				}
				else
				{
					// open tag, since all other cases are exhausted
					enclosingTags.Push(tag.name);
					if (xmlTagMatcher.Matcher(tag.name).Matches())
					{
						matchDepth++;
					}
				}
			}
			// end for (CoreLabel token: tokens)
			if (!enclosingTags.IsEmpty())
			{
				string mesg = "Unclosed tags, starting with <" + enclosingTags.Pop() + '>';
				if (allowFlawedXml)
				{
					log.Warn(mesg);
				}
				else
				{
					throw new ArgumentException();
				}
			}
			// If we ended with a string of xml tokens, that text needs to be
			// appended to the "AfterAnnotation" of one of the tokens...
			// Note that we clear removedText when we see a real token, so
			// if removedText is not empty, that must be because we just
			// dropped an xml tag.  Therefore we ignore that old After
			// annotation, since that text was already absorbed in the Before
			// annotation of the xml tag we threw away
			if (!newTokens.IsEmpty() && removedText.Length > 0)
			{
				CoreLabel lastToken = newTokens[newTokens.Count - 1];
				// sometimes AfterAnnotation seems to be null even when we are
				// collecting before & after annotations, but OriginalTextAnnotation
				// is only non-null if we are invertible.  Hopefully.
				if (lastToken.Get(typeof(CoreAnnotations.OriginalTextAnnotation)) != null)
				{
					lastToken.Set(typeof(CoreAnnotations.AfterAnnotation), removedText.ToString());
				}
			}
			// Populate docid, docdate, doctype
			if (annotation != null)
			{
				if (!docIdTokens.IsEmpty())
				{
					string str = TokensToString(annotation, docIdTokens).Trim();
					annotation.Set(typeof(CoreAnnotations.DocIDAnnotation), str);
				}
				if (!docDateTokens.IsEmpty())
				{
					string str = TokensToString(annotation, docDateTokens).Trim();
					annotation.Set(typeof(CoreAnnotations.DocDateAnnotation), str);
				}
				if (!docTypeTokens.IsEmpty())
				{
					string str = TokensToString(annotation, docTypeTokens).Trim();
					annotation.Set(typeof(CoreAnnotations.DocTypeAnnotation), str);
				}
			}
			return newTokens;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.Singleton(typeof(CoreAnnotations.TokensAnnotation));
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return Java.Util.Collections.EmptySet();
		}
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IE;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;





namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>This class will add segmentation information to an Annotation.</summary>
	/// <remarks>
	/// This class will add segmentation information to an Annotation.
	/// It assumes that the original document is a List of sentences under the
	/// <c>SentencesAnnotation.class</c>
	/// key, and that each sentence has a
	/// <c>TextAnnotation.class key</c>
	/// . This Annotator adds corresponding
	/// information under a
	/// <c>CharactersAnnotation.class</c>
	/// key prior to segmentation,
	/// and a
	/// <c>TokensAnnotation.class</c>
	/// key with value of a List of CoreLabel
	/// after segmentation.
	/// </remarks>
	/// <author>Pi-Chuan Chang</author>
	public class ChineseSegmenterAnnotator : IAnnotator
	{
		/// <summary>A logger for this class</summary>
		private static readonly Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Pipeline.ChineseSegmenterAnnotator));

		private const string DefaultModelName = "segment";

		private const string DefaultSegLoc = "/u/nlp/data/chinese-segmenter/stanford-seg-2010/classifiers-2013/ctb7.chris6.lex.gz";

		private const string DefaultSerDictionary = "//u/nlp/data/chinese-segmenter/stanford-seg-2010/classifiers-2013/dict-chris6.ser.gz";

		private const string DefaultSighanCorporaDict = "/u/nlp/data/chinese-segmenter/stanford-seg-2010/releasedata/";

		private static readonly string separator = "(?:\r|\r?\n|" + Runtime.LineSeparator() + ')';

		private static readonly Pattern separatorPattern = Pattern.Compile(separator);

		private readonly AbstractSequenceClassifier<object> segmenter;

		private readonly bool Verbose;

		private readonly bool tokenizeNewline;

		private readonly bool sentenceSplitOnTwoNewlines;

		private readonly bool normalizeSpace;

		public ChineseSegmenterAnnotator()
			: this(DefaultSegLoc, false)
		{
		}

		public ChineseSegmenterAnnotator(string segLoc, bool verbose)
			: this(segLoc, verbose, DefaultSerDictionary, DefaultSighanCorporaDict)
		{
		}

		public ChineseSegmenterAnnotator(string segLoc, bool verbose, string serDictionary, string sighanCorporaDict)
			: this(DefaultModelName, PropertiesUtils.AsProperties(DefaultModelName + ".serDictionary", serDictionary, DefaultModelName + ".sighanCorporaDict", sighanCorporaDict, DefaultModelName + ".verbose", bool.ToString(verbose), DefaultModelName + ".model"
				, segLoc))
		{
		}

		public ChineseSegmenterAnnotator(string name, Properties props)
		{
			string model = null;
			// Keep only the properties that apply to this annotator
			Properties modelProps = new Properties();
			string desiredKey = name + '.';
			foreach (string key in props.StringPropertyNames())
			{
				if (key.StartsWith(desiredKey))
				{
					// skip past name and the subsequent "."
					string modelKey = Sharpen.Runtime.Substring(key, desiredKey.Length);
					if (modelKey.Equals("model"))
					{
						model = props.GetProperty(key);
					}
					else
					{
						modelProps.SetProperty(modelKey, props.GetProperty(key));
					}
				}
			}
			this.Verbose = PropertiesUtils.GetBool(props, name + ".verbose", false);
			this.normalizeSpace = PropertiesUtils.GetBool(props, name + ".normalizeSpace", false);
			if (model == null)
			{
				throw new Exception("Expected a property " + name + ".model");
			}
			// don't write very much, because the CRFClassifier already reports loading
			if (Verbose)
			{
				log.Info("Loading Segmentation Model ... ");
			}
			try
			{
				segmenter = CRFClassifier.GetClassifier(model, modelProps);
			}
			catch (Exception e)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new Exception(e);
			}
			// If newlines are treated as sentence split, we need to retain them in tokenization for ssplit to make use of them
			tokenizeNewline = (!props.GetProperty(StanfordCoreNLP.NewlineIsSentenceBreakProperty, "never").Equals("never")) || bool.ValueOf(props.GetProperty(StanfordCoreNLP.NewlineSplitterProperty, "false"));
			// record whether or not sentence splitting on two newlines ; if so, need to remove single newlines
			sentenceSplitOnTwoNewlines = props.GetProperty(StanfordCoreNLP.NewlineIsSentenceBreakProperty, "never").Equals("two");
		}

		public virtual void Annotate(Annotation annotation)
		{
			if (Verbose)
			{
				log.Info("Adding Segmentation annotation ... ");
			}
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			if (sentences != null)
			{
				foreach (ICoreMap sentence in sentences)
				{
					DoOneSentence(sentence);
				}
			}
			else
			{
				DoOneSentence(annotation);
			}
		}

		private void DoOneSentence(ICoreMap annotation)
		{
			SplitCharacters(annotation);
			RunSegmentation(annotation);
		}

		/// <summary>This is based on the "SGML2" pattern from PTBLexer.flex.</summary>
		private static readonly Pattern xmlPattern = Pattern.Compile("<([!?][A-Za-z-][^>\r\n]*|[A-Za-z][A-Za-z0-9_:.-]*([ ]+([A-Za-z][A-Za-z0-9_:.-]*|[A-Za-z][A-Za-z0-9_:.-]*[ ]*=[ ]*('[^'\r\n]*'|\"[^\"\r\n]*\"|[A-Za-z][A-Za-z0-9_:.-]*)))*[ ]*/?|/[A-Za-z][A-Za-z0-9_:.-]*)[ ]*>"
			);

		/// <summary>
		/// This gets the TextAnnotation and creates a CharactersAnnotation, where, roughly,
		/// the text has been separated into one character non-whitespace tokens with ChineseCharAnnotation, and with
		/// a ChineseSegAnnotation marking the ones after whitespace, so that there will definitely
		/// be word segmentation there.
		/// </summary>
		/// <remarks>
		/// This gets the TextAnnotation and creates a CharactersAnnotation, where, roughly,
		/// the text has been separated into one character non-whitespace tokens with ChineseCharAnnotation, and with
		/// a ChineseSegAnnotation marking the ones after whitespace, so that there will definitely
		/// be word segmentation there. In 2016, two improvements were added: Handling non-BMP characters
		/// correctly and not splitting on whitespace in the same types of XML places that are recognized by
		/// English PTBTokenizer.
		/// </remarks>
		/// <param name="annotation">
		/// The annotation to process. The result of processing is stored under the
		/// <c>SegmenterCoreAnnotations.CharactersAnnotation.class</c>
		/// key
		/// </param>
		private void SplitCharacters(ICoreMap annotation)
		{
			string origText = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			bool seg = true;
			// false only while inside an XML entity
			IList<CoreLabel> charTokens = new List<CoreLabel>();
			int length = origText.Length;
			int xmlStartOffset = int.MaxValue;
			int xmlEndOffset = -1;
			Matcher m = xmlPattern.Matcher(origText);
			if (m.Find())
			{
				xmlStartOffset = m.Start();
				xmlEndOffset = m.End();
			}
			// determine boundaries of leading and trailing newlines, carriage returns
			int firstNonNewlineOffset = -1;
			int lastNonNewlineOffset = length;
			for (int offset = 0; offset < length; offset += cpCharCount)
			{
				int cp = origText.CodePointAt(offset);
				cpCharCount = char.CharCount(cp);
				string charString = Sharpen.Runtime.Substring(origText, offset, offset + cpCharCount);
				if (firstNonNewlineOffset == -1 && !(cp == '\n' || cp == '\r' || Runtime.LineSeparator().Contains(charString)))
				{
					firstNonNewlineOffset = offset;
				}
				if (!(cp == '\n' || cp == '\r' || Runtime.LineSeparator().Contains(charString)))
				{
					lastNonNewlineOffset = offset;
				}
			}
			// keep track of previous offset while looping through characters
			LinkedList<bool> isNewlineQueue = new LinkedList<bool>();
			Sharpen.Collections.AddAll(isNewlineQueue, Arrays.AsList(false));
			// loop through characters
			for (int offset_1 = 0; offset_1 < length; offset_1 += cpCharCount)
			{
				int cp = origText.CodePointAt(offset_1);
				cpCharCount = char.CharCount(cp);
				CoreLabel wi = new CoreLabel();
				string charString = Sharpen.Runtime.Substring(origText, offset_1, offset_1 + cpCharCount);
				// new Java 8 substring, don't need to copy.
				if (offset_1 == xmlEndOffset)
				{
					// reset with another search
					m = xmlPattern.Matcher(origText);
					if (m.Find(offset_1))
					{
						xmlStartOffset = m.Start();
						xmlEndOffset = m.End();
					}
				}
				// need to add the first char into the newline queue
				if (offset_1 == 0)
				{
					isNewlineQueue.Add(cp == '\n');
				}
				// check next char, or add false if no next char
				int nextOffset = offset_1 + cpCharCount;
				if (nextOffset < origText.Length)
				{
					int nextCodePoint = origText.CodePointAt(nextOffset);
					isNewlineQueue.Add(nextCodePoint == '\n');
				}
				else
				{
					isNewlineQueue.Add(false);
				}
				bool skipCharacter = false;
				bool isXMLCharacter = false;
				// first two cases are for XML region
				if (offset_1 == xmlStartOffset)
				{
					seg = true;
					isXMLCharacter = true;
				}
				else
				{
					if (offset_1 > xmlStartOffset && offset_1 < xmlEndOffset)
					{
						seg = false;
						isXMLCharacter = true;
					}
					else
					{
						if (char.IsSpaceChar(cp) || char.IsISOControl(cp))
						{
							// if this word is a whitespace or a control character, set 'seg' to true for next character
							seg = true;
							// Don't skip newline characters if we're tokenizing them
							// We always count \n as newline to be consistent with the implementation of ssplit
							// check if this is a newline character
							bool prevIsNewline = isNewlineQueue[0];
							bool currIsNewline = isNewlineQueue[1];
							bool nextIsNewline = isNewlineQueue[2];
							// determine if this is a leading or trailing newline at beginning or end of document
							bool isLeadingOrTrailingNewline = (offset_1 < firstNonNewlineOffset || offset_1 > lastNonNewlineOffset);
							// determine if this is an isolated newline in the middle of the document
							bool isSingleNewlineInMiddle = (currIsNewline && (!prevIsNewline && !nextIsNewline));
							// don't skip if tokenizing newlines and this is a newline character
							skipCharacter = !(tokenizeNewline && currIsNewline);
							// ...unless leading or trailing newlines (always skip these)
							if (isLeadingOrTrailingNewline)
							{
								skipCharacter = true;
							}
							// ...skip single newlines in the middle of document if splitting on two newlines
							if (sentenceSplitOnTwoNewlines && isSingleNewlineInMiddle)
							{
								skipCharacter = true;
							}
						}
					}
				}
				if (!skipCharacter)
				{
					// if this character is a normal character, put it in as a CoreLabel and set seg to false for next word
					wi.Set(typeof(CoreAnnotations.ChineseCharAnnotation), charString);
					if (seg)
					{
						wi.Set(typeof(CoreAnnotations.ChineseSegAnnotation), "1");
					}
					else
					{
						wi.Set(typeof(CoreAnnotations.ChineseSegAnnotation), "0");
					}
					if (isXMLCharacter)
					{
						if (char.IsSpaceChar(cp) || char.IsISOControl(cp))
						{
							// We mark XML whitespace with a special tag because later they will be handled differently
							// than non-whitespace XML characters. This is because the segmenter eats whitespaces...
							wi.Set(typeof(SegmenterCoreAnnotations.XMLCharAnnotation), "whitespace");
						}
						else
						{
							if (offset_1 == xmlStartOffset)
							{
								wi.Set(typeof(SegmenterCoreAnnotations.XMLCharAnnotation), "beginning");
							}
							else
							{
								wi.Set(typeof(SegmenterCoreAnnotations.XMLCharAnnotation), "1");
							}
						}
					}
					else
					{
						wi.Set(typeof(SegmenterCoreAnnotations.XMLCharAnnotation), "0");
					}
					wi.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), offset_1);
					wi.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), (offset_1 + cpCharCount));
					charTokens.Add(wi);
					seg = false;
				}
				// drop oldest element from isNewline queue
				isNewlineQueue.Poll();
			}
			// for loop through charPoints
			annotation.Set(typeof(SegmenterCoreAnnotations.CharactersAnnotation), charTokens);
		}

		/// <summary>Move the pos pointer to point into sentChars after passing w.</summary>
		/// <remarks>
		/// Move the pos pointer to point into sentChars after passing w.
		/// This is a bit subtle, because there can be multi-char codepoints in sentChars elements.
		/// </remarks>
		/// <returns>The position of the next thing in sentChars to look at</returns>
		private static int AdvancePos(IList<CoreLabel> sentChars, int pos, string w)
		{
			StringBuilder sb = new StringBuilder();
			while (!w.Equals(sb.ToString()))
			{
				sb.Append(sentChars[pos].Get(typeof(CoreAnnotations.ChineseCharAnnotation)));
				pos++;
			}
			return pos;
		}

		private void RunSegmentation(ICoreMap annotation)
		{
			//0 2
			// A BC D E
			// 1 10 1 1
			// 0 12 3 4
			// 0, 0+1 ,
			string text = annotation.Get(typeof(CoreAnnotations.TextAnnotation));
			// the original text String
			IList<CoreLabel> sentChars = annotation.Get(typeof(SegmenterCoreAnnotations.CharactersAnnotation));
			// the way it was divided by splitCharacters
			if (Verbose)
			{
				log.Info("sentChars (length " + sentChars.Count + ") is " + SentenceUtils.ListToString(sentChars, StringUtils.EmptyStringArray));
			}
			IList<CoreLabel> tokens = new List<CoreLabel>();
			annotation.Set(typeof(CoreAnnotations.TokensAnnotation), tokens);
			// Run the segmenter! On the whole String. It knows not about the splitting into chars.
			// Can we change this to have it run directly on the already existing list of tokens. That would help, no?
			IList<string> words;
			if (!tokenizeNewline)
			{
				text = text.ReplaceAll("[\r\n]", string.Empty);
				words = segmenter.SegmentString(text);
			}
			else
			{
				// remove leading and trailing newlines
				text = text.ReplaceAll("^[\\r\\n]+", string.Empty);
				text = text.ReplaceAll("[\\r\\n]+$", string.Empty);
				// if using the sentence split on two newlines option, replace single newlines
				// single newlines should be ignored for segmenting
				if (sentenceSplitOnTwoNewlines)
				{
					text = text.ReplaceAll("([^\\n])\\r?\\n([^\\r\\n])", "$1$2");
					// do a second pass to handle corner case of consecutive isolated newlines
					// x \n x \n x
					text = text.ReplaceAll("([^\\n])\\r?\\n([^\\r\\n])", "$1$2");
				}
				// Run the segmenter on each line so that we don't get tokens that cross line boundaries
				// Neat trick to keep delimiters from: http://stackoverflow.com/a/2206432
				string[] lines = text.Split(string.Format("((?<=%1$s)|(?=%1$s))", separator));
				words = new List<string>();
				foreach (string line in lines)
				{
					if (separatorPattern.Matcher(line).Matches())
					{
						// Don't segment newline tokens, keep them as-is
						words.Add(line);
					}
					else
					{
						Sharpen.Collections.AddAll(words, segmenter.SegmentString(line));
					}
				}
			}
			if (Verbose)
			{
				log.Info(text + "\n--->\n" + words + " (length " + words.Count + ')');
			}
			// Go through everything again and make the final tokens list; for loop is over segmented words
			int pos = 0;
			// This is used to index sentChars, the output from splitCharacters
			StringBuilder xmlBuffer = new StringBuilder();
			int xmlBegin = -1;
			foreach (string w in words)
			{
				CoreLabel fl = sentChars[pos];
				string xmlCharAnnotation = fl.Get(typeof(SegmenterCoreAnnotations.XMLCharAnnotation));
				if (Verbose)
				{
					log.Info("Working on word " + w + ", sentChar " + fl.ToShorterString() + " (sentChars index " + pos + ')');
				}
				if ("0".Equals(xmlCharAnnotation) || "beginning".Equals(xmlCharAnnotation))
				{
					// Beginnings of plain text and other XML tags are good places to end an XML tag
					if (xmlBuffer.Length > 0)
					{
						// Form the XML token
						string xmlTag = xmlBuffer.ToString();
						CoreLabel fl1 = sentChars[pos - 1];
						int end = fl1.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
						tokens.Add(MakeXmlToken(xmlTag, true, xmlBegin, end));
						// Clean up and prepare for the next XML tag
						xmlBegin = -1;
						xmlBuffer = new StringBuilder();
					}
				}
				if (!"0".Equals(xmlCharAnnotation))
				{
					// found an XML character; fl changes inside this loop!
					while (fl.Get(typeof(SegmenterCoreAnnotations.XMLCharAnnotation)).Equals("whitespace"))
					{
						// Print whitespaces into the XML buffer and move on until the next non-whitespace character is found
						// and we're in sync with segmenter output again
						xmlBuffer.Append(' ');
						pos += 1;
						fl = sentChars[pos];
					}
					xmlBuffer.Append(w);
					pos = AdvancePos(sentChars, pos, w);
					if (xmlBegin < 0)
					{
						xmlBegin = fl.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
					}
					continue;
				}
				// remember that fl may be more than one char long (non-BMP chars like emoji), so use advancePos()
				fl.Set(typeof(CoreAnnotations.ChineseSegAnnotation), "1");
				if (w.IsEmpty())
				{
					if (Verbose)
					{
						log.Warn("Encountered an empty word. Shouldn't happen?");
					}
					continue;
				}
				// [cdm 2016:] surely this shouldn't happen!
				int begin = fl.Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation));
				pos = AdvancePos(sentChars, pos, w);
				if (pos - 1 >= sentChars.Count)
				{
					log.Error("Error: on word " + w + " at position " + (pos - w.Length) + " trying to get at position " + (pos - 1));
					log.Error("last element of sentChars is " + sentChars[sentChars.Count - 1]);
				}
				else
				{
					fl = sentChars[pos - 1];
					int end = fl.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
					tokens.Add(MakeXmlToken(w, false, begin, end));
				}
			}
			// end for (go through everything again)
			if (xmlBuffer.Length > 0)
			{
				// Form the last XML token, if any
				string xmlTag = xmlBuffer.ToString();
				CoreLabel fl1 = sentChars[pos - 1];
				int end = fl1.Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation));
				tokens.Add(MakeXmlToken(xmlTag, true, xmlBegin, end));
			}
			if (Verbose)
			{
				foreach (CoreLabel token in tokens)
				{
					log.Info(token.ToShorterString());
				}
			}
		}

		private CoreLabel MakeXmlToken(string tokenText, bool doNormalization, int charOffsetBegin, int charOffsetEnd)
		{
			CoreLabel token = new CoreLabel();
			token.SetOriginalText(tokenText);
			if (separatorPattern.Matcher(tokenText).Matches())
			{
				// Map to CoreNLP newline token
				tokenText = AbstractTokenizer.NewlineToken;
			}
			else
			{
				if (doNormalization && normalizeSpace)
				{
					tokenText = tokenText.Replace(' ', '\u00A0');
				}
			}
			// change space to non-breaking space
			token.SetWord(tokenText);
			token.SetValue(tokenText);
			token.Set(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), charOffsetBegin);
			token.Set(typeof(CoreAnnotations.CharacterOffsetEndAnnotation), charOffsetEnd);
			if (Verbose)
			{
				log.Info("Adding token " + token.ToShorterString());
			}
			return token;
		}

		public virtual ICollection<Type> Requires()
		{
			return Java.Util.Collections.EmptySet();
		}

		public virtual ICollection<Type> RequirementsSatisfied()
		{
			return new HashSet<Type>(Arrays.AsList(typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.TokensAnnotation), typeof(CoreAnnotations.CharacterOffsetBeginAnnotation), typeof(CoreAnnotations.CharacterOffsetEndAnnotation), typeof(CoreAnnotations.BeforeAnnotation
				), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.TokenBeginAnnotation), typeof(CoreAnnotations.TokenEndAnnotation), typeof(CoreAnnotations.PositionAnnotation), typeof(CoreAnnotations.IndexAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
				), typeof(CoreAnnotations.ValueAnnotation)));
		}
	}
}

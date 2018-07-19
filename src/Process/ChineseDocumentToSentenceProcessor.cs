using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Trees.International.Pennchinese;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Net;
using Java.Util;
using Java.Util.Regex;
using Javax.Swing.Text;
using Javax.Swing.Text.Html;
using Javax.Swing.Text.Html.Parser;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <summary>Convert a Chinese Document into a List of sentence Strings.</summary>
	/// <author>Pi-Chuan Chang</author>
	[System.Serializable]
	public class ChineseDocumentToSentenceProcessor
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.Process.ChineseDocumentToSentenceProcessor));

		private const long serialVersionUID = 4054964767812217460L;

		private static readonly ICollection<char> fullStopsSet = Generics.NewHashSet(Arrays.AsList(new char[] { '。', '！', '？', '!', '?' }));

		private static readonly ICollection<char> rightMarkSet = Generics.NewHashSet(Arrays.AsList(new char[] { '”', '’', '》', '』', '〉', '」', '＞', '＇', '）', '\'', '"', ')', ']', '>' }));

		private const string encoding = "UTF-8";

		private readonly IList<Pair<string, string>> normalizationTable;

		public ChineseDocumentToSentenceProcessor()
			: this(null)
		{
		}

		private static readonly Pattern PairPattern = Pattern.Compile("([^\\s]+)\\s+([^\\s]+)");

		/// <param name="normalizationTableFile">
		/// A file listing character pairs for
		/// normalization.  Currently the normalization table must be in UTF-8.
		/// If this parameter is
		/// <see langword="null"/>
		/// , the default normalization
		/// of the zero-argument constructor is used.
		/// </param>
		public ChineseDocumentToSentenceProcessor(string normalizationTableFile)
		{
			// todo: This class is a mess. We should try to get it out of core
			// not \uff0e . (too often separates English first/last name, etc.)
			// private final String normalizationTableFile;
			// this.normalizationTableFile = normalizationTableFile;
			if (normalizationTableFile != null)
			{
				normalizationTable = new List<Pair<string, string>>();
				foreach (string line in ObjectBank.GetLineIterator(new File(normalizationTableFile), encoding))
				{
					Matcher pairMatcher = PairPattern.Matcher(line);
					if (pairMatcher.Find())
					{
						normalizationTable.Add(new Pair<string, string>(pairMatcher.Group(1), pairMatcher.Group(2)));
					}
					else
					{
						log.Info("Didn't match: " + line);
					}
				}
			}
			else
			{
				normalizationTable = null;
			}
		}

		/*
		public ChineseDocumentToSentenceProcessor(String normalizationTableFile, String encoding) {
		log.info("WARNING: ChineseDocumentToSentenceProcessor ignores normalizationTableFile argument!");
		log.info("WARNING: ChineseDocumentToSentenceProcessor ignores encoding argument!");
		// encoding is never read locally
		this.encoding = encoding;
		}
		*/
		/// <summary>
		/// This should now become disused, and other people should call
		/// ChineseUtils directly!  CDM June 2006.
		/// </summary>
		public virtual string Normalization(string @in)
		{
			//log.info("BEFOR NORM: "+in);
			string norm = ChineseUtils.Normalize(@in);
			string @out = Normalize(norm);
			//log.info("AFTER NORM: "+out);
			return @out;
		}

		private static readonly Pattern WhiteplusPattern = Pattern.Compile(ChineseUtils.Whiteplus);

		private static readonly Pattern StartWhiteplusPattern = Pattern.Compile('^' + ChineseUtils.Whiteplus);

		private static readonly Pattern EndWhiteplusPattern = Pattern.Compile(ChineseUtils.Whiteplus + '$');

		private string Normalize(string inputString)
		{
			if (normalizationTable == null)
			{
				return inputString;
			}
			Pattern replacePattern = WhiteplusPattern;
			Matcher replaceMatcher = replacePattern.Matcher(inputString);
			inputString = replaceMatcher.ReplaceAll(" ");
			foreach (Pair<string, string> p in normalizationTable)
			{
				replacePattern = Pattern.Compile(p.First(), Pattern.Literal);
				replaceMatcher = replacePattern.Matcher(inputString);
				string escape = p.Second();
				if (escape.Equals("$"))
				{
					escape = "\\$";
				}
				inputString = replaceMatcher.ReplaceAll(escape);
			}
			return inputString;
		}

		/// <summary>
		/// usage: java ChineseDocumentToSentenceProcessor [-segmentIBM]
		/// -file filename [-encoding encoding]
		/// <p>
		/// The -segmentIBM option is for IBM GALE-specific splitting of an
		/// XML element into sentences.
		/// </summary>
		/// <exception cref="System.Exception"/>
		public static void Main(string[] args)
		{
			//String encoding = "GB18030";
			Properties props = StringUtils.ArgsToProperties(args);
			// log.info("Here are the properties:");
			// props.list(System.err);
			bool alwaysAddS = props.Contains("alwaysAddS");
			Edu.Stanford.Nlp.Process.ChineseDocumentToSentenceProcessor cp;
			if (!props.Contains("file"))
			{
				log.Info("usage: java ChineseDocumentToSentenceProcessor [-segmentIBM] -file filename [-encoding encoding]");
				return;
			}
			cp = new Edu.Stanford.Nlp.Process.ChineseDocumentToSentenceProcessor();
			if (props.Contains("encoding"))
			{
				log.Info("WARNING: for now the default encoding is " + cp.encoding + ". It's not changeable for now");
			}
			string input = IOUtils.SlurpFileNoExceptions(props.GetProperty("file"), cp.encoding);
			// String input = StringUtils.slurpGBURLNoExceptions(new URL(props.getProperty("file")));
			if (props.Contains("segmentIBM"))
			{
				ITokenizer<Word> tok = WhitespaceTokenizer.NewWordWhitespaceTokenizer(new StringReader(input), true);
				string parseInside = props.GetProperty("parseInside");
				if (parseInside == null)
				{
					parseInside = string.Empty;
				}
				Pattern p1;
				Pattern p2;
				Pattern p3;
				Pattern p4;
				PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Out, cp.encoding), true);
				StringBuilder buff = new StringBuilder();
				StringBuilder sgmlbuff = new StringBuilder();
				string lastSgml = string.Empty;
				p1 = Pattern.Compile("<.*>");
				p2 = Pattern.Compile("\uFEFF?<[\\p{Alpha}]+");
				p3 = Pattern.Compile("[A-Za-z0-9=\"]+>");
				p4 = Pattern.Compile("<(?:" + parseInside + ")[ >]");
				bool inSGML = false;
				int splitItems = 0;
				int numAdded = 0;
				while (tok.MoveNext())
				{
					string s = tok.Current.Word();
					// pw.println("The token is |" + s + "|");
					if (p2.Matcher(s).Matches())
					{
						inSGML = true;
						sgmlbuff.Append(s).Append(" ");
					}
					else
					{
						if (p1.Matcher(s).Matches() || inSGML && p3.Matcher(s).Matches() || "\n".Equals(s))
						{
							inSGML = false;
							if (buff.ToString().Trim().Length > 0)
							{
								// pw.println("Dumping sentences");
								// pw.println("Buff is " + buff);
								bool processIt = false;
								if (parseInside.Equals(string.Empty))
								{
									processIt = true;
								}
								else
								{
									if (p4.Matcher(lastSgml).Find())
									{
										processIt = true;
									}
								}
								if (processIt)
								{
									IList<string> sents = Edu.Stanford.Nlp.Process.ChineseDocumentToSentenceProcessor.FromPlainText(buff.ToString(), true);
									// pw.println("Sents is " + sents);
									// pw.println();
									if (alwaysAddS || sents.Count > 1)
									{
										int i = 1;
										foreach (string str in sents)
										{
											pw.Print("<s id=\"" + i + "\">");
											pw.Print(str);
											pw.Println("</s>");
											i++;
										}
										if (sents.Count > 1)
										{
											splitItems++;
											numAdded += sents.Count - 1;
										}
									}
									else
									{
										if (sents.Count == 1)
										{
											pw.Print(sents[0]);
										}
									}
								}
								else
								{
									pw.Print(buff);
								}
								buff = new StringBuilder();
							}
							sgmlbuff.Append(s);
							// pw.println("sgmlbuff is " + sgmlbuff);
							pw.Print(sgmlbuff);
							lastSgml = sgmlbuff.ToString();
							sgmlbuff = new StringBuilder();
						}
						else
						{
							if (inSGML)
							{
								sgmlbuff.Append(s).Append(" ");
							}
							else
							{
								buff.Append(s).Append(" ");
							}
						}
					}
				}
				// pw.println("Buff is now |" + buff + "|");
				// end while (tok.hasNext()) {
				// empty remaining buffers
				pw.Flush();
				pw.Close();
				log.Info("Split " + splitItems + " segments, adding " + numAdded + " sentences.");
			}
			else
			{
				IList<string> sent = Edu.Stanford.Nlp.Process.ChineseDocumentToSentenceProcessor.FromHTML(input);
				PrintWriter pw = new PrintWriter(new OutputStreamWriter(System.Console.Error, cp.encoding), true);
				foreach (string a in sent)
				{
					pw.Println(a);
				}
			}
		}

		/// <summary>Strip off HTML tags before processing.</summary>
		/// <remarks>
		/// Strip off HTML tags before processing.
		/// Only the simplest tag stripping is implemented.
		/// </remarks>
		/// <param name="inputString">Chinese document text which contains HTML tags</param>
		/// <returns>a List of sentence strings</returns>
		/// <exception cref="System.IO.IOException"/>
		public static IList<string> FromHTML(string inputString)
		{
			//HTMLParser parser = new HTMLParser();
			//return fromPlainText(parser.parse(inputString));
			IList<string> ans = new List<string>();
			ChineseDocumentToSentenceProcessor.MyHTMLParser parser = new ChineseDocumentToSentenceProcessor.MyHTMLParser();
			IList<string> sents = parser.Parse(inputString);
			foreach (string s in sents)
			{
				Sharpen.Collections.AddAll(ans, FromPlainText(s));
			}
			return ans;
		}

		/// <param name="contentString">Chinese document text</param>
		/// <returns>a List of sentence strings</returns>
		/// <exception cref="System.IO.IOException"/>
		public static IList<string> FromPlainText(string contentString)
		{
			return FromPlainText(contentString, false);
		}

		/// <exception cref="System.IO.IOException"/>
		public static IList<string> FromPlainText(string contentString, bool segmented)
		{
			if (segmented)
			{
				contentString = ChineseUtils.Normalize(contentString, ChineseUtils.Leave, ChineseUtils.Ascii);
			}
			else
			{
				contentString = ChineseUtils.Normalize(contentString, ChineseUtils.Fullwidth, ChineseUtils.Ascii);
			}
			string sentenceString = string.Empty;
			char[] content = contentString.ToCharArray();
			bool sentenceEnd = false;
			IList<string> sentenceList = new List<string>();
			int lastCh = -1;
			foreach (char c in content)
			{
				// EncodingPrintWriter.out.println("Char is |" + c + "|", "UTF-8");
				string newChar = c.ToString();
				if (!sentenceEnd)
				{
					if (segmented && fullStopsSet.Contains(c) && (lastCh == -1 || char.IsSpaceChar(lastCh)))
					{
						// require it to be a standalone punctuation mark -- cf. URLs
						sentenceString += newChar;
						sentenceEnd = true;
					}
					else
					{
						if (!segmented && fullStopsSet.Contains(c))
						{
							// EncodingPrintWriter.out.println("  End of sent char", "UTF-8");
							sentenceString += newChar;
							sentenceEnd = true;
						}
						else
						{
							sentenceString += newChar;
						}
					}
				}
				else
				{
					// sentenceEnd == true
					if (rightMarkSet.Contains(c))
					{
						sentenceString += newChar;
					}
					else
					{
						// EncodingPrintWriter.out.println("  Right mark char", "UTF-8");
						if (newChar.Matches("\\s"))
						{
							sentenceString += newChar;
						}
						else
						{
							if (fullStopsSet.Contains(c))
							{
								// EncodingPrintWriter.out.println("  End of sent char (2+)", "UTF-8");
								sentenceString += newChar;
							}
							else
							{
								// otherwise
								if (sentenceString.Length > 0)
								{
									sentenceEnd = false;
								}
								sentenceString = RemoveWhitespace(sentenceString, segmented);
								if (sentenceString.Length > 0)
								{
									//log.info("<<< "+sentenceString+" >>>");
									sentenceList.Add(sentenceString);
								}
								sentenceString = string.Empty;
								sentenceString += newChar;
							}
						}
					}
				}
				lastCh = c;
			}
			// end for (Character c : content)
			sentenceString = RemoveWhitespace(sentenceString, segmented);
			if (sentenceString.Length > 0)
			{
				//log.info("<<< "+sentenceString+" >>>");
				sentenceList.Add(sentenceString);
			}
			return sentenceList;
		}

		/// <summary>
		/// In non-segmented mode, all whitespace is removed,
		/// in segmented mode only leading and trailing whitespace goes away.
		/// </summary>
		private static string RemoveWhitespace(string str, bool segmented)
		{
			if (str.Length > 0)
			{
				//System.out.println("Add: "+sentenceString);
				Pattern replacePattern = StartWhiteplusPattern;
				Matcher replaceMatcher = replacePattern.Matcher(str);
				str = replaceMatcher.ReplaceAll(string.Empty);
				replacePattern = EndWhiteplusPattern;
				replaceMatcher = replacePattern.Matcher(str);
				str = replaceMatcher.ReplaceAll(string.Empty);
				if (!segmented)
				{
					replacePattern = WhiteplusPattern;
					replaceMatcher = replacePattern.Matcher(str);
					str = replaceMatcher.ReplaceAll(string.Empty);
				}
			}
			return str;
		}

		internal class MyHTMLParser : HTMLEditorKit.ParserCallback
		{
			protected internal StringBuilder textBuffer;

			protected internal IList<string> sentences;

			protected internal string title;

			protected internal bool isTitle;

			protected internal bool isBody;

			protected internal bool isScript;

			protected internal bool isBreak;

			public MyHTMLParser()
				: base()
			{
				title = string.Empty;
				isTitle = false;
				isBody = false;
				isScript = false;
				isBreak = false;
			}

			public override void HandleText(char[] data, int pos)
			{
				if (data.Length == 0)
				{
					return;
				}
				if (isTitle)
				{
					title = new string(data);
				}
				else
				{
					if (isBody && !isScript)
					{
					}
				}
				//textBuffer.append(data).append(" ");
				//if (isBreak) {
				if (true)
				{
					textBuffer.Append(data);
					string text = textBuffer.ToString();
					text = text.ReplaceAll("\u00a0", string.Empty);
					text = text.Trim();
					if (text.Length == 0)
					{
						return;
					}
					sentences.Add(text);
					textBuffer = new StringBuilder(500);
				}
			}

			/// <summary>Sets a flag if the start tag is the "TITLE" element start tag.</summary>
			public override void HandleStartTag(HTML.Tag tag, IMutableAttributeSet attrSet, int pos)
			{
				if (tag == HTML.Tag.Title)
				{
					isTitle = true;
				}
				else
				{
					if (tag == HTML.Tag.Body)
					{
						isBody = true;
					}
					else
					{
						if (tag == HTML.Tag.Script)
						{
							isScript = true;
						}
					}
				}
				isBreak = tag.BreaksFlow();
			}

			/// <summary>Sets a flag if the end tag is the "TITLE" element end tag</summary>
			public override void HandleEndTag(HTML.Tag tag, int pos)
			{
				if (tag == HTML.Tag.Title)
				{
					isTitle = false;
				}
				else
				{
					if (tag == HTML.Tag.Body)
					{
						isBody = false;
					}
					else
					{
						if (tag == HTML.Tag.Script)
						{
							isScript = false;
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual IList<string> Parse(URL url)
			{
				return (Parse(IOUtils.SlurpURL(url)));
			}

			/// <exception cref="System.IO.IOException"/>
			public virtual IList<string> Parse(Reader r)
			{
				return Parse(IOUtils.SlurpReader(r));
			}

			/// <summary>The parse method that actually does the work.</summary>
			/// <remarks>
			/// The parse method that actually does the work.
			/// Now it first gets rid of singleton tags before running.
			/// </remarks>
			/// <exception cref="System.IO.IOException"/>
			public virtual IList<string> Parse(string text)
			{
				text = text.ReplaceAll("/>", ">");
				text = text.ReplaceAll("<\\?", "<");
				StringReader r = new StringReader(text);
				textBuffer = new StringBuilder(200);
				sentences = new List<string>();
				new ParserDelegator().Parse(r, this, true);
				return sentences;
			}

			public virtual string Title()
			{
				return title;
			}
			/*
			public static void main(String[] args) throws IOException {
			MyHTMLParser parser = new MyHTMLParser();
			String input = StringUtils.slurpGBURLNoExceptions(new URL(args[0]));
			List<String> result = parser.parse(input);
			PrintWriter orig = new PrintWriter("file.orig");
			PrintWriter parsed = new PrintWriter("file.parsed");
			log.info("output to file.orig");
			orig.println(input);
			for (String s : result) {
			log.info("output to file.parsed");
			parsed.println(s);
			parsed.println("-----------------------------------------");
			}
			orig.close();
			parsed.close();
			}
			*/
		}
	}
}

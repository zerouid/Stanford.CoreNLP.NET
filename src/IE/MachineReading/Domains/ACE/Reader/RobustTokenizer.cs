/*
* RobustTokenizer.java
* Performs tokenization of natural language English text, following ACE data
* Use the method tokenize() for smart tokenization
* @author Mihai
*/
using System.Collections.Generic;
using System.Text;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Process;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Sharpen;

namespace Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader
{
	public class RobustTokenizer<T> : AbstractTokenizer<Word>
		where T : Word
	{
		/// <summary>A logger for this class</summary>
		private static Redwood.RedwoodChannels log = Redwood.Channels(typeof(Edu.Stanford.Nlp.IE.Machinereading.Domains.Ace.Reader.RobustTokenizer));

		/// <summary>Buffer to tokenize</summary>
		internal string buffer;

		/// <summary>The set of known abbreviations</summary>
		private RobustTokenizer.AbbreviationMap mAbbreviations;

		public const int MaxMultiWordSize = 20;

		public static readonly string Dot = Block("\\.");

		public static readonly string Dotdot = Block("\\:");

		public static readonly string Apostrophe = Block("\\'");

		public static readonly string Slash = Block("\\/");

		public static readonly string Underscore = Block("\\_");

		public static readonly string Minus = Block("\\-");

		public static readonly string Plus = Block("\\+");

		public static readonly string Comma = Block("\\,");

		public static readonly string Dotcomma = Block("\\;");

		public static readonly string Quotes = Block(Or("\\\"", "\\'\\'", "\\'", "\\`\\`", "\\`"));

		public static readonly string DoubleQuotes = Block(Or("\\\"", "\\'\\'"));

		public static readonly string Lrb = Block("\\(");

		public static readonly string Rrb = Block("\\)");

		public static readonly string Lcb = Block("\\{");

		public static readonly string Rcb = Block("\\}");

		public static readonly string Greater = Block("\\>");

		public static readonly string Lower = Block("\\<");

		public static readonly string Ampersand = Block("\\&");

		public static readonly string At = Block("\\@");

		public static readonly string Http = Block("[hH][tT][tT][pP]\\:\\/\\/");

		public static readonly string WhiteSpace = Block("\\s");

		public static readonly string Digit = Block("\\d");

		public static readonly string Letter = Block("[a-zA-Z]");

		public static readonly string Upper = Block("[A-Z]");

		public static readonly string Sign = Or(Minus, Plus);

		public static readonly string Fullnum = Block(ZeroOrOne(Sign) + OneOrMore(Digit) + ZeroOrMore(ZeroOrOne(Or(Dot, Comma, Slash)) + OneOrMore(Digit)));

		public static readonly string Decnum = Block(Dot + OneOrMore(Digit));

		public static readonly string Num = Or(Fullnum, Decnum);

		public static readonly string Date = Block(OneOrMore(Digit) + Slash + OneOrMore(Digit) + Slash + OneOrMore(Digit));

		public static readonly string Time = Block(OneOrMore(Digit) + OneOrMore(Block(Dotdot + OneOrMore(Digit))));

		public static readonly string Punc = Or(Quotes, Block(Minus + OneOrMore(Minus)), Block(Dot + OneOrMore(Dot)));

		public static readonly string Letters = OneOrMore(Letter);

		public static readonly string Block = Or(Num, Letters);

		public static readonly string Word = Block(ZeroOrOne(Apostrophe) + Block + ZeroOrMore(Block(ZeroOrOne(Or(Underscore, Minus, Apostrophe, Slash, Ampersand)) + Block)));

		public static readonly string Acronym = Block(OneOrMore(Letter + Dot));

		public static readonly string LooseAcronym = Block(OneOrMore((OneOrMore(Letter) + Dot)) + ZeroOrMore(Letter));

		public static readonly string Paren = Or(Lrb, Rrb, Lcb, Rcb);

		public const string Sgml = "<[^<>]+>";

		public static readonly string Htmlcode = Block(Ampersand + Upper + Dotcomma);

		public static readonly string Any = Block("\\S");

		public static readonly string Email = Block(Letter + ZeroOrMore(Or(Letter, Digit, Dot, Minus, Underscore)) + At + ZeroOrMore(Or(Letter, Digit, Dot, Minus, Underscore)) + Letter);

		public static readonly string DomainEmail = Block(Letter + ZeroOrMore(Or(Letter, Digit, Dot, Minus, Underscore)) + At + OneOrMore(Or(Letter, Digit, Dot, Minus, Underscore)) + ZeroOrMore(WhiteSpace) + Dot + ZeroOrMore(WhiteSpace) + Or("org", 
			"ORG", "com", "COM", "net", "NET", "ru", "us"));

		public static readonly string Url = Block(Http + OneOrMore(Or(Letter, Digit, Dot, Underscore, Slash, Ampersand, Minus, Plus)));

		public static readonly string SmallUrl = Block(OneOrMore(OneOrMore(Letter) + Dot) + ZeroOrMore(WhiteSpace) + Or("org", "ORG", "com", "COM", "net", "NET", "ru", "us"));

		public static readonly string Underscoreseq = OneOrMore("_");

		public static readonly string ListBullet = Block(Lrb + Letter + ZeroOrOne(Letter) + Rrb);

		public static readonly string PhonePart = Block(Lrb + OneOrMore(Digit) + Rrb);

		public static readonly string Digitseq = OneOrMore(Digit);

		public static readonly string RecognisedPattern = Block(Block(Time) + "|" + Block(DomainEmail) + "|" + Block(Email) + "|" + Block(Url) + "|" + Block(Acronym) + "|" + Block(Date) + "|" + Block(PhonePart) + "|" + Block(Word) + "|" + Block(Punc
			) + "|" + Block(ListBullet) + "|" + Block(Paren) + "|" + Block(Sgml) + "|" + Block(Htmlcode) + "|" + Block(Underscoreseq) + "|" + Block(Any));

		/// <summary>The overall token pattern</summary>
		private static readonly Pattern wordPattern;

		/// <summary>Pattern to recognize SGML tags</summary>
		private static readonly Pattern sgmlPattern;

		/// <summary>Pattern to recognize slash-separated dates</summary>
		private static readonly Pattern slashDatePattern;

		/// <summary>Pattern to recognize acronyms</summary>
		private static readonly Pattern acronymPattern;

		/// <summary>Pattern to recognize URLs</summary>
		private static readonly Pattern urlPattern;

		/// <summary>Pattern to recognize emails</summary>
		private static readonly Pattern emailPattern;

		/// <summary>Recognized sequences of digits</summary>
		private static readonly Pattern digitSeqPattern;

		static RobustTokenizer()
		{
			// basic tokens
			// basic sequences
			// numbers
			// date and time
			// punctuation marks
			// words
			// acronyms
			// + zeroOrOne(LETTER));
			// this matches acronyms AFTER abbreviation merging
			// other possible constructs
			// email addresses must start with a letter, contain @, and end with a letter
			// email addresses must start with a letter, contain @, and end with . com 
			// URLs must start with http:// or ftp://, followed by at least a letter
			//URLs without http, but ending in org, com, net
			// keep sequence of underscores as a single token
			// list bullet, e.g., "(a)"
			// part of a phone number, e.g., "(214)"
			// sequence of digits
			// the complete pattern
			// block(SMALL_URL) + "|" +
			// must be before WORD, otherwise it's broken into multiple tokens
			wordPattern = Pattern.Compile(RecognisedPattern);
			sgmlPattern = Pattern.Compile(Sgml);
			slashDatePattern = Pattern.Compile(Date);
			acronymPattern = Pattern.Compile(LooseAcronym);
			urlPattern = Pattern.Compile(Url);
			emailPattern = Pattern.Compile(Email);
			digitSeqPattern = Pattern.Compile(Digitseq);
		}

		public RobustTokenizer(string buffer)
		{
			mAbbreviations = new RobustTokenizer.AbbreviationMap(true);
			this.buffer = buffer;
			this.cachedTokens = null;
		}

		public RobustTokenizer(bool caseInsensitive, string buffer)
		{
			mAbbreviations = new RobustTokenizer.AbbreviationMap(caseInsensitive);
			this.buffer = buffer;
			this.cachedTokens = null;
		}

		/// <summary>any in the set</summary>
		public static string Range(string s)
		{
			return Block("[" + s + "]");
		}

		/// <summary>zero or one</summary>
		public static string ZeroOrOne(string s)
		{
			return Block(Block(s) + "?");
		}

		/// <summary>zero or more</summary>
		public static string ZeroOrMore(string s)
		{
			return Block(Block(s) + "*");
		}

		/// <summary>one or more</summary>
		public static string OneOrMore(string s)
		{
			return Block(Block(s) + "+");
		}

		/// <summary>parens</summary>
		public static string Block(string s)
		{
			return "(" + s + ")";
		}

		/// <summary>any of the two</summary>
		public static string Or(string s1, string s2)
		{
			return Block(Block(s1) + "|" + Block(s2));
		}

		/// <summary>any of the three</summary>
		public static string Or(string s1, string s2, string s3)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3));
		}

		/// <summary>any of the four</summary>
		public static string Or(string s1, string s2, string s3, string s4)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4));
		}

		/// <summary>any of the five</summary>
		public static string Or(string s1, string s2, string s3, string s4, string s5)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5));
		}

		/// <summary>any of the six</summary>
		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6));
		}

		/// <summary>any of the seven</summary>
		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6, string s7)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6) + "|" + Block(s7));
		}

		/// <summary>any of the eight</summary>
		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6) + "|" + Block(s7) + "|" + Block(s8));
		}

		/// <summary>any of the nine</summary>
		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6) + "|" + Block(s7) + "|" + Block(s8) + "|" + Block(s9));
		}

		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9, string s10)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6) + "|" + Block(s7) + "|" + Block(s8) + "|" + Block(s9) + "|" + Block(s10));
		}

		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9, string s10, string s11)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6) + "|" + Block(s7) + "|" + Block(s8) + "|" + Block(s9) + "|" + Block(s10) + "|" + Block(s11));
		}

		public static string Or(string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8, string s9, string s10, string s11, string s12)
		{
			return Block(Block(s1) + "|" + Block(s2) + "|" + Block(s3) + "|" + Block(s4) + "|" + Block(s5) + "|" + Block(s6) + "|" + Block(s7) + "|" + Block(s8) + "|" + Block(s9) + "|" + Block(s10) + "|" + Block(s11) + "|" + Block(s12));
		}

		/// <summary>not</summary>
		public static string RangeNot(string s)
		{
			return Range(Block("^" + s));
		}

		private static int HasApostropheBlock(string s)
		{
			for (int i = s.Length - 1; i > 0; i--)
			{
				if (s[i] == '\'' && i < s.Length - 1)
				{
					return i;
				}
				if (!char.IsLetter(s[i]))
				{
					return -1;
				}
			}
			return -1;
		}

		private static string Concatenate<T>(IList<T> tokens, int start, int end)
			where T : RobustTokenizer.WordToken
		{
			StringBuilder buffer = new StringBuilder();
			for (; start < end; start++)
			{
				buffer.Append(((RobustTokenizer.WordToken)tokens[start]).GetWord());
			}
			return buffer.ToString();
		}

		private static int CountNewLines<T>(IList<T> tokens, int start, int end)
			where T : RobustTokenizer.WordToken
		{
			int count = 0;
			for (int i = start + 1; i < end; i++)
			{
				count += tokens[i].GetNewLineCount();
			}
			return count;
		}

		public static bool IsUrl(string s)
		{
			Matcher match = urlPattern.Matcher(s);
			return match.Find(0);
		}

		public static bool IsEmail(string s)
		{
			Matcher match = emailPattern.Matcher(s);
			return match.Find(0);
		}

		public static bool IsSgml(string s)
		{
			Matcher match = sgmlPattern.Matcher(s);
			return match.Find(0);
		}

		public static bool IsSlashDate(string s)
		{
			Matcher match = slashDatePattern.Matcher(s);
			return match.Find(0);
		}

		public static bool IsAcronym(string s)
		{
			Matcher match = acronymPattern.Matcher(s);
			return match.Find(0);
		}

		public static bool IsDigitSeq(string s)
		{
			Matcher match = digitSeqPattern.Matcher(s);
			return match.Find(0);
		}

		public virtual int CountNewLines(string s, int start, int end)
		{
			int count = 0;
			for (int i = start; i < end; i++)
			{
				if (s[i] == '\n')
				{
					count++;
				}
			}
			return count;
		}

		/// <summary>
		/// Smart tokenization storing the output in an array of CoreLabel
		/// Sets the following fields:
		/// - TextAnnotation - the text of the token
		/// - TokenBeginAnnotation - the byte offset of the token (start)
		/// - TokenEndAnnotation - the byte offset of the token (end)
		/// </summary>
		public virtual Word[] TokenizeToWords()
		{
			IList<RobustTokenizer.WordToken> toks = TokenizeToWordTokens();
			Word[] labels = new Word[toks.Count];
			for (int i = 0; i < toks.Count; i++)
			{
				RobustTokenizer.WordToken tok = toks[i];
				Word l = new Word(tok.GetWord(), tok.GetStart(), tok.GetEnd());
				labels[i] = l;
			}
			return labels;
		}

		/// <summary>Tokenizes a natural language string</summary>
		/// <returns>List of WordTokens</returns>
		public virtual IList<RobustTokenizer.WordToken> TokenizeToWordTokens()
		{
			IList<RobustTokenizer.WordToken> result = new List<RobustTokenizer.WordToken>();
			//
			// replace illegal characters with SPACE
			//
			/*
			StringBuffer buffer = new StringBuffer();
			for(int i = 0; i < originalString.length(); i ++){
			int c = (int) originalString.charAt(i);
			//
			// regular character
			//
			if(c > 31 && c < 127) buffer.append((char) c);
			
			else{
			log.info("Control character at position " + i + ": " + c);
			
			//
			// DOS new line counts as two characters
			//
			if(c == 10) buffer.append(" ");
			
			//
			// other control character
			//
			else buffer.append(' ');
			}
			}
			*/
			Matcher match = wordPattern.Matcher(buffer);
			int previousEndMatch = 0;
			//
			// Straight tokenization, ignoring known abbreviations
			//
			while (match.Find())
			{
				string crtMatch = match.Group();
				int endMatch = match.End();
				int startMatch = endMatch - crtMatch.Length;
				int i;
				// found word ending in "n't"
				if (crtMatch.EndsWith("n't"))
				{
					if (crtMatch.Length > 3)
					{
						RobustTokenizer.WordToken token1 = new RobustTokenizer.WordToken(Sharpen.Runtime.Substring(crtMatch, 0, crtMatch.Length - 3), startMatch, endMatch - 3, CountNewLines(buffer, previousEndMatch, startMatch));
						result.Add(token1);
					}
					RobustTokenizer.WordToken token2 = new RobustTokenizer.WordToken(Sharpen.Runtime.Substring(crtMatch, crtMatch.Length - 3, crtMatch.Length), endMatch - 3, endMatch, 0);
					result.Add(token2);
				}
				else
				{
					// found word containing an appostrophe
					// XXX: is this too relaxed? e.g. "O'Hare"
					if ((i = HasApostropheBlock(crtMatch)) != -1)
					{
						RobustTokenizer.WordToken token1 = new RobustTokenizer.WordToken(Sharpen.Runtime.Substring(crtMatch, 0, i), startMatch, startMatch + i, CountNewLines(buffer, previousEndMatch, startMatch));
						RobustTokenizer.WordToken token2 = new RobustTokenizer.WordToken(Sharpen.Runtime.Substring(crtMatch, i, crtMatch.Length), startMatch + i, endMatch, 0);
						result.Add(token1);
						result.Add(token2);
					}
					else
					{
						// just a regular word
						RobustTokenizer.WordToken token = new RobustTokenizer.WordToken(crtMatch, startMatch, endMatch, CountNewLines(buffer, previousEndMatch, startMatch));
						result.Add(token);
					}
				}
				previousEndMatch = endMatch;
			}
			//
			// Merge known abreviations
			//
			IList<RobustTokenizer.WordToken> resultWithAbs = new List<RobustTokenizer.WordToken>();
			for (int i_1 = 0; i_1 < result.Count; i_1++)
			{
				// where the mw ends
				int end = result.Count;
				if (end > i_1 + MaxMultiWordSize)
				{
					end = i_1 + MaxMultiWordSize;
				}
				bool found = false;
				// must have at least two tokens per multiword
				for (; end > i_1 + 1; end--)
				{
					RobustTokenizer.WordToken startToken = result[i_1];
					RobustTokenizer.WordToken endToken = result[end - 1];
					if (CountNewLines(result, i_1, end) == 0)
					{
						// abbreviation tokens cannot appear on different lines
						string conc = Concatenate(result, i_1, end);
						found = false;
						// found a multiword
						if ((mAbbreviations.Contains(conc) == true))
						{
							found = true;
							RobustTokenizer.WordToken token = new RobustTokenizer.WordToken(conc, startToken.GetStart(), endToken.GetEnd(), startToken.GetNewLineCount());
							resultWithAbs.Add(token);
							i_1 = end - 1;
							break;
						}
					}
				}
				// no multiword starting at this position found
				if (!found)
				{
					resultWithAbs.Add(result[i_1]);
				}
			}
			resultWithAbs = Postprocess(resultWithAbs);
			return resultWithAbs;
		}

		/// <summary>Redefine this method to implement additional domain-specific tokenization rules</summary>
		/// <param name="tokens"/>
		protected internal virtual IList<RobustTokenizer.WordToken> Postprocess(IList<RobustTokenizer.WordToken> tokens)
		{
			return tokens;
		}

		/// <summary>Tokenizes and adds blank spaces were needed between each token</summary>
		/// <exception cref="System.IO.IOException"/>
		public virtual string TokenizeText()
		{
			IList<RobustTokenizer.WordToken> tokenList = TokenizeToWordTokens();
			StringBuilder strBuffer = new StringBuilder();
			IEnumerator<RobustTokenizer.WordToken> iter = tokenList.GetEnumerator();
			if (iter.MoveNext())
			{
				strBuffer.Append(iter.Current);
			}
			while (iter.MoveNext())
			{
				strBuffer.Append(" ");
				strBuffer.Append(iter.Current);
			}
			return strBuffer.ToString().ReplaceAll("\\s\\s+", " ");
		}

		public class AbbreviationMap
		{
			private ICollection<string> mAbbrevSet;

			private static IList<string> NormalizeCase(bool caseInsensitive, IList<string> words)
			{
				if (!caseInsensitive)
				{
					return words;
				}
				IList<string> normWords = new List<string>();
				foreach (string word in words)
				{
					normWords.Add(word.ToLower());
				}
				return normWords;
			}

			/// <summary>Creates a new instance of AbreviationMap with some know abbreviations</summary>
			public AbbreviationMap(bool caseInsensitive)
			{
				mAbbrevSet = Generics.NewHashSet(NormalizeCase(caseInsensitive, Arrays.AsList(new string[] { "1.", "10.", "11.", "12.", "13.", "14.", "15.", "16.", "17.", "18.", "19.", "2.", "20.", "21.", "22.", "23.", "24.", "25.", "26.", "27.", "28.", "29."
					, "3.", "30.", "31.", "32.", "33.", "34.", "35.", "36.", "37.", "38.", "39.", "4.", "40.", "41.", "42.", "43.", "44.", "45.", "46.", "47.", "48.", "49.", "5.", "50.", "6.", "7.", "8.", "9.", "A.", "A.C.", "A.D.", "A.D.L.", "A.F.", "A.G.", "A.H."
					, "A.J.C.", "A.L.", "A.M", "A.M.", "A.P.", "A.T.B.", "AUG.", "Act.", "Adm.", "Ala.", "Ariz.", "Ark.", "Assn.", "Ass'n.", "Ass'n", "Aug.", "B.", "B.A.T", "B.B.", "B.F.", "B.J.", "B.V.", "Bancorp.", "Bhd.", "Blvd.", "Br.", "Brig.", "Bros.", "C."
					, "C.B.", "C.D.s", "C.J.", "C.O.", "C.R.", "C.W.", "CEO.", "CO.", "CORP.", "COS.", "Cal.", "Calif.", "Capt.", "Cie.", "Cir.", "Cmdr.", "Co.", "Col.", "Colo.", "Comdr.", "Conn.", "Corp.", "Cos.", "D.", "D.B.", "D.C", "D.C.", "D.H.", "D.M.", 
					"D.N.", "D.S.", "D.T", "D.T.", "D.s", "Dec.", "Del.", "Dept.", "Dev.", "Dr.", "Ds.", "E.", "E.E.", "E.F.", "E.I.", "E.M.", "E.R.", "E.W.", "Etc.", "F.", "F.A.", "F.A.O.", "F.C", "F.E.", "F.J.", "F.S.B.", "F.W.", "FEB.", "FL.", "Feb.", "Fed."
					, "Fla.", "Fran.", "French.", "Freon.", "Ft.", "G.", "G.D.", "G.L.", "G.O.", "G.S.", "G.m.b", "G.m.b.H.", "GP.", "GPO.", "Ga.", "Gen.", "Gov.", "H.", "H.F.", "H.G.", "H.H.", "H.J.", "H.L.", "H.R.", "Hon.", "I.", "I.B.M.", "I.C.H.", "I.E.P."
					, "I.M.", "I.V.", "I.W.", "II.", "III.", "INC.", "Intl.", "Int'l", "IV.", "IX.", "Ill.", "Inc.", "Ind.", "J.", "J.C.", "J.D.", "J.E.", "J.F.", "J.F.K.", "J.H.", "J.L.", "J.M.", "JohnQ.Public", "J.P.", "J.R.", "J.V", "J.V.", "J.X.", "Jan.", 
					"Jansz.", "Je.", "Jos.", "Jr.", "K.", "K.C.", "Kan.", "Ky.", "L.", "L.A.", "L.H.", "L.J.", "L.L.", "L.M.", "L.P", "L.P.", "La.", "Lt.", "Ltd.", "M.", "M.A.", "M.B.A.", "M.D", "M.D.", "M.D.C.", "M.E.", "M.J.", "M.R.", "M.S.", "M.W.", "M8.7sp"
					, "Maj.", "Mar.", "Mass.", "Md.", "Med.", "Messrs.", "Mfg.", "Mich.", "Minn.", "Mir.", "Miss.", "Mo.", "Mr.", "Mrs.", "Ms.", "Mt.", "N.", "N.A.", "N.C", "N.C.", "N.D", "N.D.", "N.H", "N.H.", "N.J", "N.J.", "N.M", "N.M.", "N.V", "N.V.", "N.Y"
					, "N.Y.", "NOV.", "Neb.", "Nev.", "No.", "no.", "Nos.", "Nov.", "O.", "O.P.", "OK.", "Oct.", "Okla.", "Ore.", "P.", "P.J.", "P.M", "P.M.", "P.R.", "Pa.", "Penn.", "Pfc.", "Ph.", "Ph.D.", "pro-U.N.", "Prof.", "Prop.", "Pty.", "Q.", "R.", "R.D."
					, "Ret.", "R.H.", "R.I", "R.I.", "R.L.", "R.P.", "R.R.", "R.W.", "RLV.", "Rd.", "Rep.", "Reps.", "Rev.", "S.", "S.A", "S.A.", "S.C", "S.C.", "S.D.", "S.G.", "S.I.", "S.P.", "S.S.", "S.p", "S.p.A", "S.p.A.", "SKr1.5", "Sen.", "Sens.", "Sept."
					, "Sgt.", "Snr.", "Spc.", "Sr.", "St.", "Sys.", "T.", "T.D.", "T.F.", "T.T.", "T.V.", "TEL.", "Tech.", "Tenn.", "Tex.", "Tx.", "U.", "U.Cal-Davis", "U.K", "U.K.", "U.N.", "U.S.", "U.S.A", "U.S.A.", "U.S.C.", "U.S.C..", "U.S.S.R", "U.S.S.R."
					, "UK.", "US116.7", "V.", "V.H.", "VI.", "VII.", "VIII.", "VS.", "Va.", "Vs.", "Vt.", "W.", "W.A.", "W.G.", "W.I.", "W.J.", "W.R.", "W.T.", "W.Va", "W.Va.", "Wash.", "Wis.", "Wyo.", "X.", "Y.", "Y.J.", "Z.", "a.", "a.d.", "a.k.a", "a.m", "a.m."
					, "al.", "b.", "c.", "c.i.f", "cf.", "cnsl.", "cnsls.", "cont'd.", "d.", "deft.", "defts.", "e.", "et.", "etc.", "etseq.", "f.", "f.o.b", "ft.", "g.", "h.", "i.", "i.e.", "j.", "k.", "l.", "m.", "mots.", "n.", "o.", "p.", "p.m", "p.m.", "pltf."
					, "pltfs.", "prelim.", "r.", "s.", "seq.", "supp.", "sq.", "t.", "u.", "v.", "vs.", "x.", "y.", "z." })));
			}

			public virtual bool Contains(string s)
			{
				return mAbbrevSet.Contains(s.ToLower());
			}
		}

		public class WordToken
		{
			/// <summary>Start position</summary>
			protected internal int mStart;

			/// <summary>End position</summary>
			protected internal int mEnd;

			/// <summary>Counts how many new lines appear between this token and the previous one in the stream</summary>
			protected internal int mNewLineCount;

			/// <summary>The lexem</summary>
			protected internal string mWord;

			public WordToken(string w, int s, int e)
			{
				mWord = w;
				mStart = s;
				mEnd = e;
				mNewLineCount = 0;
			}

			public WordToken(string w, int s, int e, int nl)
			{
				mWord = w;
				mStart = s;
				mEnd = e;
				mNewLineCount = nl;
			}

			public override string ToString()
			{
				StringBuilder buffer = new StringBuilder();
				buffer.Append("[");
				buffer.Append(mWord);
				buffer.Append(", ");
				buffer.Append(mStart);
				buffer.Append(", ");
				buffer.Append(mEnd);
				buffer.Append("]");
				return buffer.ToString();
			}

			public virtual int GetStart()
			{
				return mStart;
			}

			public virtual void SetStart(int i)
			{
				mStart = i;
			}

			public virtual int GetEnd()
			{
				return mEnd;
			}

			public virtual void SetEnd(int i)
			{
				mEnd = i;
			}

			public virtual int GetNewLineCount()
			{
				return mNewLineCount;
			}

			public virtual void SetNewLineCount(int i)
			{
				mNewLineCount = i;
			}

			public virtual string GetWord()
			{
				return mWord;
			}

			public virtual void SetWord(string w)
			{
				mWord = w;
			}
		}

		/// <summary>Cached tokens for this buffer.</summary>
		/// <remarks>Cached tokens for this buffer. Used by getNext</remarks>
		internal Word[] cachedTokens;

		/// <summary>Current position in the cachedTokens list.</summary>
		/// <remarks>Current position in the cachedTokens list. Used by getNext</remarks>
		internal int cachedPosition;

		protected internal override Word GetNext()
		{
			if (cachedTokens == null)
			{
				cachedTokens = TokenizeToWords();
				cachedPosition = 0;
			}
			if (cachedPosition >= cachedTokens.Length)
			{
				return null;
			}
			Word token = cachedTokens[cachedPosition];
			cachedPosition++;
			return token;
		}

		/// <exception cref="System.Exception"/>
		public static void Main(string[] argv)
		{
			if (argv.Length != 1)
			{
				log.Info("Usage: java edu.stanford.nlp.ie.machinereading.common.RobustTokenizer <file to tokenize>");
				System.Environment.Exit(1);
			}
			// tokenize this file
			BufferedReader @is = new BufferedReader(new FileReader(argv[0]));
			// read the whole file in a buffer
			// XXX: for sure there are more efficient ways of reading a file...
			int ch;
			StringBuilder buffer = new StringBuilder();
			while ((ch = @is.Read()) != -1)
			{
				buffer.Append((char)ch);
			}
			// create the tokenizer object
			RobustTokenizer<Word> t = new RobustTokenizer<Word>(buffer.ToString());
			IList<Word> tokens = t.Tokenize();
			foreach (Word token in tokens)
			{
				System.Console.Out.WriteLine(token);
			}
		}
	}
}

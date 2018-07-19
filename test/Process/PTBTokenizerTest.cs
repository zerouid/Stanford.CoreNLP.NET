using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Trees;
using Edu.Stanford.Nlp.Trees.International.Negra;
using Java.IO;
using Java.Lang;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Process
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	public class PTBTokenizerTest
	{
		private readonly string[] ptbInputs = new string[] { "This is a sentence.", "U.S. insurance: Conseco acquires Kemper Corp. \n</HEADLINE>\n<P>\nU.S insurance", "Based in Eugene,Ore., PakTech needs a new distributor after Sydney-based Creative Pack Pty. Ltd. went into voluntary administration."
			, "The Iron Age (ca. 1300 – ca. 300 BC).", "Indo\u00ADnesian ship\u00ADping \u00AD", "Gimme a phone, I'm gonna call.", "\"John & Mary's dog,\" Jane thought (to herself).\n\"What a #$%!\na- ``I like AT&T''.\"", "I said at 4:45pm.", "I can't believe they wanna keep 40% of that.\"\n``Whatcha think?''\n\"I don't --- think so...,\""
			, "You `paid' US$170,000?!\nYou should've paid only$16.75.", "1. Buy a new Chevrolet (37%-owned in the U.S..) . 15%", "I like you ;-) but do you care :(. I'm happy ^_^ but shy (x.x)!", "Diamond (``Not even the chair'') lives near Udaipur (84km). {1. A potential Palmer trade:}"
			, "No. I like No. 24 and no.47.", "You can get a B.S. or a B. A. or a Ph.D (sometimes a Ph. D) from Stanford.", "@Harry_Styles didn`t like Mu`ammar al-Qaddafi", "Kenneth liked Windows 3.1, Windows 3.x, and Mesa A.B as I remember things.", "I like programming in F# more than C#."
			, "NBC Live will be available free through the Yahoo! Chat Web site. E! Entertainment said ``Jeopardy!'' is a game show.", "I lived in O\u2019Malley and read OK! Magazine.", "I lived in O\u0092Malley and read OK! Magazine.", "I like: \u2022wine, \u0095cheese, \u2023salami, & \u2043speck."
			, "I don't give a f**k about your sh*tty life.", "First sentence.... Second sentence.", "First sentence . . . . Second sentence.", "I wasn’t really ... well, what I mean...see . . . what I'm saying, the thing is . . . I didn’t mean it.", "This is a url test. Here is one: http://google.com."
			, "This is a url test. Here is one: htvp://google.com.", "Download from ftp://myname@host.dom/%2Fetc/motd", "Download from svn://user@location.edu/path/to/magic/unicorns", "Download from svn+ssh://user@location.edu/path/to/magic/unicorns", 
			"Independent Living can be reached at http://www.inlv.demon.nl/.", "We traveled from No. Korea to So. Calif. yesterday.", "I dunno.", "The o-kay was received by the anti-acquisition front on its foolishness-filled fish market.", "We ran the pre-tests through the post-scripted centrifuge."
			, "School-aged parents should be aware of the unique problems that they face.", "I dispute Art. 53 of the convention.", "I like Art. And I like History.", "Contact: sue@google.com, fred@stanford.edu; michael.inman@lab.rpi.cs.cmu.edu.", "Email: recruiters@marvelconsultants.com <mailto:recruiters@marvelconsultants.com>"
			, " Jeremy Meier <jermeier@earthlink.net>", "Ram Tackett,  (mailto:rtackett@abacustech.net)", "[Jgerma5@aol.com]. Danny_Jones%ENRON@eott.com", "https://fancy.startup.ai", "mid-2015", "UK-based", "2010-2015", "20-30%", "80,000-man march", "39-yard"
			, "60-90's", "Soft AC-styled", "3 p.m., eastern time", "Total Private\nOrders 779.5 -9.5%", "2-9.5%", "2- 9.5%", "From July 23-24. Radisson Miyako Hotel.", "23 percent-2 percent higher than today", "23 percent--2 percent higher than today", 
			"438798-438804", "He earned eligibility by virtue of a top-35 finish.", "Witt was 2-for-34 as a hitter", "An Atlanta-bound DC-9 crashed", "weigh 1,000-1,200 pounds, ", "Imus arrived to be host for the 5:30-to-10 a.m. show.", "The .38-Magnum bullet"
			, "a 1908 Model K Stanley with 1:01-minute time", "the 9-to-11:45 a.m. weekday shift", "Brighton Rd. Pacifica", "Walls keeping water out of the bowl-shaped city have been breached, and emergency teams are using helicopters to drop 1,350kg (3,000lb) sandbags and concrete barriers into the gaps."
			, "i got (89.2%) in my exams" };

		private readonly string[][] ptbGold = new string[][] { new string[] { "This", "is", "a", "sentence", "." }, new string[] { "U.S.", "insurance", ":", "Conseco", "acquires", "Kemper", "Corp.", ".", "</HEADLINE>", "<P>", "U.S", "insurance" }, new 
			string[] { "Based", "in", "Eugene", ",", "Ore.", ",", "PakTech", "needs", "a", "new", "distributor", "after", "Sydney-based", "Creative", "Pack", "Pty.", "Ltd.", "went", "into", "voluntary", "administration", "." }, new string[] { "The", "Iron"
			, "Age", "-LRB-", "ca.", "1300", "--", "ca.", "300", "BC", "-RRB-", "." }, new string[] { "Indonesian", "shipping", "-" }, new string[] { "Gim", "me", "a", "phone", ",", "I", "'m", "gon", "na", "call", "." }, new string[] { "``", "John", "&"
			, "Mary", "'s", "dog", ",", "''", "Jane", "thought", "-LRB-", "to", "herself", "-RRB-", ".", "``", "What", "a", "#", "$", "%", "!", "a", "-", "``", "I", "like", "AT&T", "''", ".", "''" }, new string[] { "I", "said", "at", "4:45", "pm", "." }
			, new string[] { "I", "ca", "n't", "believe", "they", "wan", "na", "keep", "40", "%", "of", "that", ".", "''", "``", "Whatcha", "think", "?", "''", "``", "I", "do", "n't", "--", "think", "so", "...", ",", "''" }, new string[] { "You", "`", 
			"paid", "'", "US$", "170,000", "?!", "You", "should", "'ve", "paid", "only", "$", "16.75", "." }, new string[] { "1", ".", "Buy", "a", "new", "Chevrolet", "-LRB-", "37", "%", "-", "owned", "in", "the", "U.S.", ".", "-RRB-", ".", "15", "%" }
			, new string[] { "I", "like", "you", ";--RRB-", "but", "do", "you", "care", ":-LRB-", ".", "I", "'m", "happy", "^_^", "but", "shy", "-LRB-x.x-RRB-", "!" }, new string[] { "Diamond", "-LRB-", "``", "Not", "even", "the", "chair", "''", "-RRB-"
			, "lives", "near", "Udaipur", "-LRB-", "84", "km", "-RRB-", ".", "-LCB-", "1", ".", "A", "potential", "Palmer", "trade", ":", "-RCB-" }, new string[] { "No", ".", "I", "like", "No.", "24", "and", "no.", "47", "." }, new string[] { "You", "can"
			, "get", "a", "B.S.", "or", "a", "B.", "A.", "or", "a", "Ph.D", "-LRB-", "sometimes", "a", "Ph.", "D", "-RRB-", "from", "Stanford", "." }, new string[] { "@Harry_Styles", "did", "n`t", "like", "Mu`ammar", "al-Qaddafi" }, new string[] { "Kenneth"
			, "liked", "Windows", "3.1", ",", "Windows", "3.x", ",", "and", "Mesa", "A.B", "as", "I", "remember", "things", "." }, new string[] { "I", "like", "programming", "in", "F#", "more", "than", "C#", "." }, new string[] { "NBC", "Live", "will", 
			"be", "available", "free", "through", "the", "Yahoo!", "Chat", "Web", "site", ".", "E!", "Entertainment", "said", "``", "Jeopardy!", "''", "is", "a", "game", "show", "." }, new string[] { "I", "lived", "in", "O'Malley", "and", "read", "OK!"
			, "Magazine", "." }, new string[] { "I", "lived", "in", "O'Malley", "and", "read", "OK!", "Magazine", "." }, new string[] { "I", "like", ":", "\u2022", "wine", ",", "\u2022", "cheese", ",", "\u2023", "salami", ",", "&", "\u2043", "speck", "."
			 }, new string[] { "I", "do", "n't", "give", "a", "f**k", "about", "your", "sh*tty", "life", "." }, new string[] { "First", "sentence", "...", ".", "Second", "sentence", "." }, new string[] { "First", "sentence", "...", ".", "Second", "sentence"
			, "." }, new string[] { "I", "was", "n't", "really", "...", "well", ",", "what", "I", "mean", "...", "see", "...", "what", "I", "'m", "saying", ",", "the", "thing", "is", "...", "I", "did", "n't", "mean", "it", "." }, new string[] { "This", 
			"is", "a", "url", "test", ".", "Here", "is", "one", ":", "http://google.com", "." }, new string[] { "This", "is", "a", "url", "test", ".", "Here", "is", "one", ":", "htvp", ":", "/", "/", "google.com", "." }, new string[] { "Download", "from"
			, "ftp://myname@host.dom/%2Fetc/motd" }, new string[] { "Download", "from", "svn://user@location.edu/path/to/magic/unicorns" }, new string[] { "Download", "from", "svn+ssh://user@location.edu/path/to/magic/unicorns" }, new string[] { "Independent"
			, "Living", "can", "be", "reached", "at", "http://www.inlv.demon.nl/", "." }, new string[] { "We", "traveled", "from", "No.", "Korea", "to", "So.", "Calif.", "yesterday", "." }, new string[] { "I", "du", "n", "no", "." }, new string[] { "The"
			, "o-kay", "was", "received", "by", "the", "anti-acquisition", "front", "on", "its", "foolishness-filled", "fish", "market", "." }, new string[] { "We", "ran", "the", "pre-tests", "through", "the", "post-scripted", "centrifuge", "." }, new 
			string[] { "School-aged", "parents", "should", "be", "aware", "of", "the", "unique", "problems", "that", "they", "face", "." }, new string[] { "I", "dispute", "Art.", "53", "of", "the", "convention", "." }, new string[] { "I", "like", "Art"
			, ".", "And", "I", "like", "History", "." }, new string[] { "Contact", ":", "sue@google.com", ",", "fred@stanford.edu", ";", "michael.inman@lab.rpi.cs.cmu.edu", "." }, new string[] { "Email", ":", "recruiters@marvelconsultants.com", "<mailto:recruiters@marvelconsultants.com>"
			 }, new string[] { "Jeremy", "Meier", "<jermeier@earthlink.net>" }, new string[] { "Ram", "Tackett", ",", "-LRB-", "mailto:rtackett@abacustech.net", "-RRB-" }, new string[] { "-LSB-", "Jgerma5@aol.com", "-RSB-", ".", "Danny_Jones%ENRON@eott.com"
			 }, new string[] { "https://fancy.startup.ai" }, new string[] { "mid-2015" }, new string[] { "UK-based" }, new string[] { "2010-2015" }, new string[] { "20-30", "%" }, new string[] { "80,000-man", "march" }, new string[] { "39-yard" }, new 
			string[] { "60-90", "'s" }, new string[] { "Soft", "AC-styled" }, new string[] { "3", "p.m.", ",", "eastern", "time" }, new string[] { "Total", "Private", "Orders", "779.5", "-9.5", "%" }, new string[] { "2-9.5", "%" }, new string[] { "2", 
			"-", "9.5", "%" }, new string[] { "From", "July", "23-24", ".", "Radisson", "Miyako", "Hotel", "." }, new string[] { "23", "percent-2", "percent", "higher", "than", "today" }, new string[] { "23", "percent", "--", "2", "percent", "higher", 
			"than", "today" }, new string[] { "438798-438804" }, new string[] { "He", "earned", "eligibility", "by", "virtue", "of", "a", "top-35", "finish", "." }, new string[] { "Witt", "was", "2-for-34", "as", "a", "hitter" }, new string[] { "An", "Atlanta-bound"
			, "DC-9", "crashed" }, new string[] { "weigh", "1,000-1,200", "pounds", "," }, new string[] { "Imus", "arrived", "to", "be", "host", "for", "the", "5:30-to-10", "a.m.", "show", "." }, new string[] { "The", ".38-Magnum", "bullet" }, new string
			[] { "a", "1908", "Model", "K", "Stanley", "with", "1:01-minute", "time" }, new string[] { "the", "9-to-11:45", "a.m.", "weekday", "shift" }, new string[] { "Brighton", "Rd.", "Pacifica" }, new string[] { "Walls", "keeping", "water", "out", 
			"of", "the", "bowl-shaped", "city", "have", "been", "breached", ",", "and", "emergency", "teams", "are", "using", "helicopters", "to", "drop", "1,350", "kg", "-LRB-", "3,000", "lb", "-RRB-", "sandbags", "and", "concrete", "barriers", "into"
			, "the", "gaps", "." }, new string[] { "i", "got", "-LRB-", "89.2", "%", "-RRB-", "in", "my", "exams" } };

		private readonly string[][] ptbGoldSplitHyphenated = new string[][] { new string[] { "This", "is", "a", "sentence", "." }, new string[] { "U.S.", "insurance", ":", "Conseco", "acquires", "Kemper", "Corp.", ".", "</HEADLINE>", "<P>", "U.S", "insurance"
			 }, new string[] { "Based", "in", "Eugene", ",", "Ore.", ",", "PakTech", "needs", "a", "new", "distributor", "after", "Sydney", "-", "based", "Creative", "Pack", "Pty.", "Ltd.", "went", "into", "voluntary", "administration", "." }, new string
			[] { "The", "Iron", "Age", "-LRB-", "ca.", "1300", "--", "ca.", "300", "BC", "-RRB-", "." }, new string[] { "Indonesian", "shipping", "-" }, new string[] { "Gim", "me", "a", "phone", ",", "I", "'m", "gon", "na", "call", "." }, new string[] 
			{ "``", "John", "&", "Mary", "'s", "dog", ",", "''", "Jane", "thought", "-LRB-", "to", "herself", "-RRB-", ".", "``", "What", "a", "#", "$", "%", "!", "a", "-", "``", "I", "like", "AT&T", "''", ".", "''" }, new string[] { "I", "said", "at", 
			"4:45", "pm", "." }, new string[] { "I", "ca", "n't", "believe", "they", "wan", "na", "keep", "40", "%", "of", "that", ".", "''", "``", "Whatcha", "think", "?", "''", "``", "I", "do", "n't", "--", "think", "so", "...", ",", "''" }, new string
			[] { "You", "`", "paid", "'", "US$", "170,000", "?!", "You", "should", "'ve", "paid", "only", "$", "16.75", "." }, new string[] { "1", ".", "Buy", "a", "new", "Chevrolet", "-LRB-", "37", "%", "-", "owned", "in", "the", "U.S.", ".", "-RRB-", 
			".", "15", "%" }, new string[] { "I", "like", "you", ";--RRB-", "but", "do", "you", "care", ":-LRB-", ".", "I", "'m", "happy", "^_^", "but", "shy", "-LRB-x.x-RRB-", "!" }, new string[] { "Diamond", "-LRB-", "``", "Not", "even", "the", "chair"
			, "''", "-RRB-", "lives", "near", "Udaipur", "-LRB-", "84", "km", "-RRB-", ".", "-LCB-", "1", ".", "A", "potential", "Palmer", "trade", ":", "-RCB-" }, new string[] { "No", ".", "I", "like", "No.", "24", "and", "no.", "47", "." }, new string
			[] { "You", "can", "get", "a", "B.S.", "or", "a", "B.", "A.", "or", "a", "Ph.D", "-LRB-", "sometimes", "a", "Ph.", "D", "-RRB-", "from", "Stanford", "." }, new string[] { "@Harry_Styles", "did", "n`t", "like", "Mu`ammar", "al", "-", "Qaddafi"
			 }, new string[] { "Kenneth", "liked", "Windows", "3.1", ",", "Windows", "3.x", ",", "and", "Mesa", "A.B", "as", "I", "remember", "things", "." }, new string[] { "I", "like", "programming", "in", "F#", "more", "than", "C#", "." }, new string
			[] { "NBC", "Live", "will", "be", "available", "free", "through", "the", "Yahoo!", "Chat", "Web", "site", ".", "E!", "Entertainment", "said", "``", "Jeopardy!", "''", "is", "a", "game", "show", "." }, new string[] { "I", "lived", "in", "O'Malley"
			, "and", "read", "OK!", "Magazine", "." }, new string[] { "I", "lived", "in", "O'Malley", "and", "read", "OK!", "Magazine", "." }, new string[] { "I", "like", ":", "\u2022", "wine", ",", "\u2022", "cheese", ",", "\u2023", "salami", ",", "&"
			, "\u2043", "speck", "." }, new string[] { "I", "do", "n't", "give", "a", "f**k", "about", "your", "sh*tty", "life", "." }, new string[] { "First", "sentence", "...", ".", "Second", "sentence", "." }, new string[] { "First", "sentence", "..."
			, ".", "Second", "sentence", "." }, new string[] { "I", "was", "n't", "really", "...", "well", ",", "what", "I", "mean", "...", "see", "...", "what", "I", "'m", "saying", ",", "the", "thing", "is", "...", "I", "did", "n't", "mean", "it", "."
			 }, new string[] { "This", "is", "a", "url", "test", ".", "Here", "is", "one", ":", "http://google.com", "." }, new string[] { "This", "is", "a", "url", "test", ".", "Here", "is", "one", ":", "htvp", ":", "/", "/", "google.com", "." }, new 
			string[] { "Download", "from", "ftp://myname@host.dom/%2Fetc/motd" }, new string[] { "Download", "from", "svn://user@location.edu/path/to/magic/unicorns" }, new string[] { "Download", "from", "svn+ssh://user@location.edu/path/to/magic/unicorns"
			 }, new string[] { "Independent", "Living", "can", "be", "reached", "at", "http://www.inlv.demon.nl/", "." }, new string[] { "We", "traveled", "from", "No.", "Korea", "to", "So.", "Calif.", "yesterday", "." }, new string[] { "I", "du", "n", 
			"no", "." }, new string[] { "The", "o-kay", "was", "received", "by", "the", "anti-acquisition", "front", "on", "its", "foolishness", "-", "filled", "fish", "market", "." }, new string[] { "We", "ran", "the", "pre-tests", "through", "the", "post-scripted"
			, "centrifuge", "." }, new string[] { "School", "-", "aged", "parents", "should", "be", "aware", "of", "the", "unique", "problems", "that", "they", "face", "." }, new string[] { "I", "dispute", "Art.", "53", "of", "the", "convention", "." }
			, new string[] { "I", "like", "Art", ".", "And", "I", "like", "History", "." }, new string[] { "Contact", ":", "sue@google.com", ",", "fred@stanford.edu", ";", "michael.inman@lab.rpi.cs.cmu.edu", "." }, new string[] { "Email", ":", "recruiters@marvelconsultants.com"
			, "<mailto:recruiters@marvelconsultants.com>" }, new string[] { "Jeremy", "Meier", "<jermeier@earthlink.net>" }, new string[] { "Ram", "Tackett", ",", "-LRB-", "mailto:rtackett@abacustech.net", "-RRB-" }, new string[] { "-LSB-", "Jgerma5@aol.com"
			, "-RSB-", ".", "Danny_Jones%ENRON@eott.com" }, new string[] { "https://fancy.startup.ai" }, new string[] { "mid", "-", "2015" }, new string[] { "UK", "-", "based" }, new string[] { "2010", "-", "2015" }, new string[] { "20", "-", "30", "%"
			 }, new string[] { "80,000", "-", "man", "march" }, new string[] { "39", "-", "yard" }, new string[] { "60", "-", "90", "'s" }, new string[] { "Soft", "AC", "-", "styled" }, new string[] { "3", "p.m.", ",", "eastern", "time" }, new string[]
			 { "Total", "Private", "Orders", "779.5", "-9.5", "%" }, new string[] { "2", "-", "9.5", "%" }, new string[] { "2", "-", "9.5", "%" }, new string[] { "From", "July", "23", "-", "24", ".", "Radisson", "Miyako", "Hotel", "." }, new string[] { 
			"23", "percent", "-2", "percent", "higher", "than", "today" }, new string[] { "23", "percent", "--", "2", "percent", "higher", "than", "today" }, new string[] { "438798", "-", "438804" }, new string[] { "He", "earned", "eligibility", "by", 
			"virtue", "of", "a", "top", "-35", "finish", "." }, new string[] { "Witt", "was", "2", "-", "for", "-34", "as", "a", "hitter" }, new string[] { "An", "Atlanta", "-", "bound", "DC", "-9", "crashed" }, new string[] { "weigh", "1,000-1,200", "pounds"
			, "," }, new string[] { "Imus", "arrived", "to", "be", "host", "for", "the", "5:30-to-10", "a.m.", "show", "." }, new string[] { "The", ".38-Magnum", "bullet" }, new string[] { "a", "1908", "Model", "K", "Stanley", "with", "1:01-minute", "time"
			 }, new string[] { "the", "9-to-11:45", "a.m.", "weekday", "shift" }, new string[] { "Brighton", "Rd.", "Pacifica" }, new string[] { "Walls", "keeping", "water", "out", "of", "the", "bowl", "-", "shaped", "city", "have", "been", "breached", 
			",", "and", "emergency", "teams", "are", "using", "helicopters", "to", "drop", "1,350", "kg", "-LRB-", "3,000", "lb", "-RRB-", "sandbags", "and", "concrete", "barriers", "into", "the", "gaps", "." }, new string[] { "i", "got", "-LRB-", "89.2"
			, "%", "-RRB-", "in", "my", "exams" } };

		/* invalid unicode codepoint, but inherit from cp1252 */
		// We don't yet split "Whatcha" but probably should following model of "Whaddya" --> What d ya. Maybe What cha
		// Unclear if 37%-owned is right or wrong under old PTB....  Maybe should be 37 %-owned even though sort of crazy
		// We don't yet split "Whatcha" but probably should following model of "Whaddya" --> What d ya. Maybe What cha
		// Unclear if 37%-owned is right or wrong under old PTB....  Maybe should be 37 %-owned even though sort of crazy
		// todo [gabor 2017]: This one probably isn't what you want either:
		//      { "23", "percent", "-", "2", "percent", "higher", "than", "today" },
		// todo [gabor 2017]: This one probably isn't what you want either:
		//      { "He", "earned", "eligibility", "by", "virtue", "of", "a", "top", "-", "35", "finish", "." },
		//      { "Witt", "was", "2", "-", "for", "-", "34", "as", "a", "hitter" },
		//      { "An", "Atlanta", "-", "bound", "DC", "-9", "crashed" },
		// todo [cdm 2017]: These next ones aren't yet right, but I'm putting off fixing them for now, since it might take a rewrite of hyphen handling
		// these are the correct answers:
		//      { "weigh", "1,000", "-", "1,200", "pounds", "," },
		//      { "Imus", "arrived", "to", "be", "host", "for", "the", "5:30", "-", "to", "-", "10", "a.m.", "show", "." },
		//      { "The", ".38", "-", "Magnum", "bullet" },
		//      { "a", "1908", "Model", "K", "Stanley", "with", "1:01", "-", "minute", "time" },
		//      { "the", "9", "-", "to", "-", "11:45", "a.m.", "weekday", "shift" },
		[Test]
		public virtual void TestPTBTokenizerWord()
		{
			ITokenizerFactory<Word> tokFactory = PTBTokenizer.Factory();
			RunOnTwoArrays(tokFactory, ptbInputs, ptbGold);
		}

		private readonly string[] moreInputs = new string[] { "Joseph Someone (fl. 2050–75) liked the noble gases, viz. helium, neon, argon, xenon, krypton and radon.", "Sambucus nigra subsp. canadensis and Canis spp. missing" };

		private readonly string[][] moreGold = new string[][] { new string[] { "Joseph", "Someone", "-LRB-", "fl.", "2050", "--", "75", "-RRB-", "liked", "the", "noble", "gases", ",", "viz.", "helium", ",", "neon", ",", "argon", ",", "xenon", ",", "krypton"
			, "and", "radon", "." }, new string[] { "Sambucus", "nigra", "subsp.", "canadensis", "and", "Canis", "spp.", "missing" } };

		[Test]
		public virtual void TestPTBTokenizerCoreLabel()
		{
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory();
			RunOnTwoArrays(tokFactory, moreInputs, moreGold);
		}

		private readonly string[] corpInputs = new string[] { "So, too, many analysts predict, will Exxon Corp., Chevron Corp. and Amoco Corp.", "So, too, many analysts predict, will Exxon Corp., Chevron Corp. and Amoco Corp.   " };

		private readonly string[][] corpGold = new string[][] { new string[] { "So", ",", "too", ",", "many", "analysts", "predict", ",", "will", "Exxon", "Corp.", ",", "Chevron", "Corp.", "and", "Amoco", "Corp", "." }, new string[] { "So", ",", "too"
			, ",", "many", "analysts", "predict", ",", "will", "Exxon", "Corp.", ",", "Chevron", "Corp.", "and", "Amoco", "Corp.", "." } };

		// strictTreebank3
		// regular
		[Test]
		public virtual void TestCorp()
		{
			// We test a 2x2 design: {strict, regular} x {no following context, following context}
			for (int sent = 0; sent < 4; sent++)
			{
				PTBTokenizer<CoreLabel> ptbTokenizer = new PTBTokenizer<CoreLabel>(new StringReader(corpInputs[sent / 2]), new CoreLabelTokenFactory(), (sent % 2 == 0) ? "strictTreebank3" : string.Empty);
				int i = 0;
				while (ptbTokenizer.MoveNext())
				{
					CoreLabel w = ptbTokenizer.Current;
					try
					{
						NUnit.Framework.Assert.AreEqual(corpGold[sent % 2][i], w.Word(), "PTBTokenizer problem");
					}
					catch (IndexOutOfRangeException)
					{
					}
					// the assertion below outside the loop will fail
					i++;
				}
				if (i != corpGold[sent % 2].Length)
				{
					System.Console.Out.Write("Gold: ");
					System.Console.Out.WriteLine(Arrays.ToString(corpGold[sent % 2]));
					IList<CoreLabel> tokens = new PTBTokenizer<CoreLabel>(new StringReader(corpInputs[sent / 2]), new CoreLabelTokenFactory(), (sent % 2 == 0) ? "strictTreebank3" : string.Empty).Tokenize();
					System.Console.Out.Write("Guess: ");
					System.Console.Out.WriteLine(SentenceUtils.ListToString(tokens));
					System.Console.Out.Flush();
				}
				NUnit.Framework.Assert.AreEqual(i, corpGold[sent % 2].Length, "PTBTokenizer num tokens problem");
			}
		}

		private static readonly string[] jeInputs = new string[] { "it's", " it's " };

		private static readonly IList[] jeOutputs = new IList[] { Arrays.AsList(new Word("it"), new Word("'s")), Arrays.AsList(new Word("it"), new Word("'s")) };

		// "open images/cat.png", // Dunno how to get this case without bad consequence. Can't detect eof in pattern....
		// Arrays.asList(new Word("open"), new Word("images/cat.png")),
		/// <summary>These case check things still work at end of file that would normally have following contexts.</summary>
		[Test]
		public virtual void TestJacobEisensteinApostropheCase()
		{
			NUnit.Framework.Assert.AreEqual(jeInputs.Length, jeOutputs.Length);
			for (int i = 0; i < jeInputs.Length; i++)
			{
				StringReader reader = new StringReader(jeInputs[i]);
				PTBTokenizer<Word> tokenizer = PTBTokenizer.NewPTBTokenizer(reader);
				IList<Word> tokens = tokenizer.Tokenize();
				NUnit.Framework.Assert.AreEqual(jeOutputs[i], tokens);
			}
		}

		private static readonly string[] untokInputs = new string[] { "London - AFP reported junk .", "Paris - Reuters reported news .", "Sydney - News said - something .", "HEADLINE - New Android phone !", "I did it 'cause I wanted to , and you 'n' me know that ."
			, "He said that `` Luxembourg needs surface - to - air missiles . ''" };

		private static readonly string[] untokOutputs = new string[] { "London - AFP reported junk.", "Paris - Reuters reported news.", "Sydney - News said - something.", "HEADLINE - New Android phone!", "I did it 'cause I wanted to, and you 'n' me know that."
			, "He said that \"Luxembourg needs surface-to-air missiles.\"" };

		[Test]
		public virtual void TestUntok()
		{
			System.Diagnostics.Debug.Assert((untokInputs.Length == untokOutputs.Length));
			for (int i = 0; i < untokInputs.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(untokOutputs[i], PTBTokenizer.Ptb2Text(untokInputs[i]), "untok gave the wrong result");
			}
		}

		[Test]
		public virtual void TestInvertible()
		{
			string text = "  This     is     a      colourful sentence.    ";
			PTBTokenizer<CoreLabel> tokenizer = PTBTokenizer.NewPTBTokenizer(new StringReader(text), false, true);
			IList<CoreLabel> tokens = tokenizer.Tokenize();
			NUnit.Framework.Assert.AreEqual(6, tokens.Count);
			NUnit.Framework.Assert.AreEqual("  ", tokens[0].Get(typeof(CoreAnnotations.BeforeAnnotation)));
			NUnit.Framework.Assert.AreEqual("     ", tokens[0].Get(typeof(CoreAnnotations.AfterAnnotation)));
			NUnit.Framework.Assert.AreEqual(2, (int)tokens[0].Get(typeof(CoreAnnotations.CharacterOffsetBeginAnnotation)), "Wrong begin char offset");
			NUnit.Framework.Assert.AreEqual(6, (int)tokens[0].Get(typeof(CoreAnnotations.CharacterOffsetEndAnnotation)), "Wrong end char offset");
			NUnit.Framework.Assert.AreEqual("This", tokens[0].Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
			// note: after(x) and before(x+1) are the same
			NUnit.Framework.Assert.AreEqual("     ", tokens[0].Get(typeof(CoreAnnotations.AfterAnnotation)));
			NUnit.Framework.Assert.AreEqual("     ", tokens[1].Get(typeof(CoreAnnotations.BeforeAnnotation)));
			// americanize is now off by default
			NUnit.Framework.Assert.AreEqual("colourful", tokens[3].Get(typeof(CoreAnnotations.TextAnnotation)));
			NUnit.Framework.Assert.AreEqual("colourful", tokens[3].Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
			NUnit.Framework.Assert.AreEqual(string.Empty, tokens[4].After());
			NUnit.Framework.Assert.AreEqual(string.Empty, tokens[5].Before());
			NUnit.Framework.Assert.AreEqual("    ", tokens[5].Get(typeof(CoreAnnotations.AfterAnnotation)));
			StringBuilder result = new StringBuilder();
			result.Append(tokens[0].Get(typeof(CoreAnnotations.BeforeAnnotation)));
			foreach (CoreLabel token in tokens)
			{
				result.Append(token.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
				string after = token.Get(typeof(CoreAnnotations.AfterAnnotation));
				if (after != null)
				{
					result.Append(after);
				}
			}
			NUnit.Framework.Assert.AreEqual(text, result.ToString());
			for (int i = 0; i < tokens.Count - 1; ++i)
			{
				NUnit.Framework.Assert.AreEqual(tokens[i].Get(typeof(CoreAnnotations.AfterAnnotation)), tokens[i + 1].Get(typeof(CoreAnnotations.BeforeAnnotation)));
			}
		}

		private readonly string[] sgmlInputs = new string[] { "Significant improvements in peak FEV1 were demonstrated with tiotropium/olodaterol 5/2 μg (p = 0.008), 5/5 μg (p = 0.012), and 5/10 μg (p < 0.0001) versus tiotropium monotherapy [51].", 
			"Panasonic brand products are produced by Samsung Electronics Co. Ltd. Sanyo products aren't.", "Oesophageal acid exposure (% time <pH 4) was similar in patients with or without complications (19.2% v 19.3% p>0.05).", "<!DOCTYPE html PUBLIC \"-//W3C//DTD HTML 4.01 Strict//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">"
			, "Hi! <foo bar=\"baz xy = foo !$*) 422\" > <?PITarget PIContent?> <?PITarget PIContent> Hi!", "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n<?xml-stylesheet type=\"text/xsl\" href=\"style.xsl\"?>\n<book xml:id=\"simple_book\" xmlns=\"http://docbook.org/ns/docbook\" version=\"5.0\">\n"
			, "<chapter xml:id=\"chapter_1\"><?php echo $a; ?>\n<!-- This is an SGML/XML comment \"Hi!\" -->\n<p> </p> <p-fix / >", "<a href=\"http:\\\\it's\\here\"> <quote orig_author='some \"dude'/> <not sgmltag", "<quote previouspost=\"\n" + "&gt; &gt; I really don't want to process this junk.\n"
			 + "&gt; No one said you did, runny.  What's got you so scared, anyway?-\n" + "\">", "&lt;b...@canada.com&gt; funky@thedismalscience.net <myemail@where.com>", "<DOC> <DOCID> nyt960102.0516 </DOCID><STORYID cat=w pri=u> A0264 </STORYID> <SLUG fv=ttj-z> "
			, "<!-- copy from here --> <a href=\"http://strategis.gc.ca/epic/internet/inabc-eac.nsf/en/home\"><img src=\"id-images/ad-220x80_01e.jpg\" alt=\"Aboriginal Business Canada:\n" + "Opening New Doors for Your Business\" width=\"220\" height=\"80\" border=\"0\"></a> <!-- copy to here --> Small ABC Graphic Instructions 1."
			, "We traveled from No.\nKorea to the U.S.A.\nWhy?" };

		private readonly string[][] sgmlGold = new string[][] { new string[] { "Significant", "improvements", "in", "peak", "FEV1", "were", "demonstrated", "with", "tiotropium/olodaterol", "5/2", "μg", "-LRB-", "p", "=", "0.008", "-RRB-", ",", "5/5"
			, "μg", "-LRB-", "p", "=", "0.012", "-RRB-", ",", "and", "5/10", "μg", "-LRB-", "p", "<", "0.0001", "-RRB-", "versus", "tiotropium", "monotherapy", "-LSB-", "51", "-RSB-", "." }, new string[] { "Panasonic", "brand", "products", "are", "produced"
			, "by", "Samsung", "Electronics", "Co.", "Ltd.", ".", "Sanyo", "products", "are", "n't", "." }, new string[] { "Oesophageal", "acid", "exposure", "-LRB-", "%", "time", "<", "pH", "4", "-RRB-", "was", "similar", "in", "patients", "with", "or"
			, "without", "complications", "-LRB-", "19.2", "%", "v", "19.3", "%", "p", ">", "0.05", "-RRB-", "." }, new string[] { "<!DOCTYPE\u00A0html\u00A0PUBLIC\u00A0\"-//W3C//DTD\u00A0HTML\u00A04.01\u00A0Strict//EN\"\u00A0\"http://www.w3.org/TR/html4/strict.dtd\">"
			 }, new string[] { "Hi", "!", "<foo\u00A0bar=\"baz\u00A0xy\u00A0=\u00A0foo\u00A0!$*)\u00A0422\"\u00A0>", "<?PITarget\u00A0PIContent?>", "<?PITarget\u00A0PIContent>", "Hi", "!" }, new string[] { "<?xml\u00A0version=\"1.0\"\u00A0encoding=\"UTF-8\"\u00A0?>"
			, "<?xml-stylesheet\u00A0type=\"text/xsl\"\u00A0href=\"style.xsl\"?>", "<book\u00A0xml:id=\"simple_book\"\u00A0xmlns=\"http://docbook.org/ns/docbook\"\u00A0version=\"5.0\">" }, new string[] { "<chapter\u00A0xml:id=\"chapter_1\">", "<?php\u00A0echo\u00A0$a;\u00A0?>"
			, "<!--\u00A0This\u00A0is\u00A0an\u00A0SGML/XML\u00A0comment\u00A0\"Hi!\"\u00A0-->", "<p>", "</p>", "<p-fix\u00A0/\u00A0>" }, new string[] { "<a href=\"http:\\\\it's\\here\">", "<quote orig_author='some \"dude'/>", "<", "not", "sgmltag" }, 
			new string[] { "<quote previouspost=\"\u00A0" + "&gt; &gt; I really don't want to process this junk.\u00A0" + "&gt; No one said you did, runny.  What's got you so scared, anyway?-\u00A0" + "\">" }, new string[] { "&lt;b...@canada.com&gt;", 
			"funky@thedismalscience.net", "<myemail@where.com>" }, new string[] { "<DOC>", "<DOCID>", "nyt960102", ".0516", "</DOCID>", "<STORYID\u00A0cat=w\u00A0pri=u>", "A0264", "</STORYID>", "<SLUG\u00A0fv=ttj-z>" }, new string[] { "<!--\u00A0copy\u00A0from\u00A0here\u00A0-->"
			, "<a\u00A0href=\"http://strategis.gc.ca/epic/internet/inabc-eac.nsf/en/home\">", "<img\u00A0src=\"id-images/ad-220x80_01e.jpg\"\u00A0alt=\"Aboriginal\u00A0Business\u00A0Canada:\u00A0" + "Opening\u00A0New\u00A0Doors\u00A0for\u00A0Your\u00A0Business\"\u00A0width=\"220\"\u00A0height=\"80\"\u00A0border=\"0\">"
			, "</a>", "<!--\u00A0copy\u00A0to\u00A0here\u00A0-->", "Small", "ABC", "Graphic", "Instructions", "1", "." }, new string[] { "We", "traveled", "from", "No.", "Korea", "to", "the", "U.S.A.", ".", "Why", "?" } };

		// this is a MUC7 document
		// In WMT 2015 from GigaWord (mis)processing. Do not always want to allow newline within SGML as messes too badly with CoNLL and one-sentence-per-line processing
		// spaces go to &nbsp; \u00A0
		[Test]
		public virtual void TestPTBTokenizerSGML()
		{
			// System.err.println("Starting SGML test");
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory("invertible");
			RunOnTwoArrays(tokFactory, sgmlInputs, sgmlGold);
			RunAgainstOrig(tokFactory, sgmlInputs);
		}

		private readonly string[][] sgmlPerLineGold = new string[][] { new string[] { "Significant", "improvements", "in", "peak", "FEV1", "were", "demonstrated", "with", "tiotropium/olodaterol", "5/2", "μg", "-LRB-", "p", "=", "0.008", "-RRB-", ","
			, "5/5", "μg", "-LRB-", "p", "=", "0.012", "-RRB-", ",", "and", "5/10", "μg", "-LRB-", "p", "<", "0.0001", "-RRB-", "versus", "tiotropium", "monotherapy", "-LSB-", "51", "-RSB-", "." }, new string[] { "Panasonic", "brand", "products", "are"
			, "produced", "by", "Samsung", "Electronics", "Co.", "Ltd.", ".", "Sanyo", "products", "are", "n't", "." }, new string[] { "Oesophageal", "acid", "exposure", "-LRB-", "%", "time", "<", "pH", "4", "-RRB-", "was", "similar", "in", "patients", 
			"with", "or", "without", "complications", "-LRB-", "19.2", "%", "v", "19.3", "%", "p", ">", "0.05", "-RRB-", "." }, new string[] { "<!DOCTYPE\u00A0html\u00A0PUBLIC\u00A0\"-//W3C//DTD\u00A0HTML\u00A04.01\u00A0Strict//EN\"\u00A0\"http://www.w3.org/TR/html4/strict.dtd\">"
			 }, new string[] { "Hi", "!", "<foo\u00A0bar=\"baz\u00A0xy\u00A0=\u00A0foo\u00A0!$*)\u00A0422\"\u00A0>", "<?PITarget\u00A0PIContent?>", "<?PITarget\u00A0PIContent>", "Hi", "!" }, new string[] { "<?xml\u00A0version=\"1.0\"\u00A0encoding=\"UTF-8\"\u00A0?>"
			, "<?xml-stylesheet\u00A0type=\"text/xsl\"\u00A0href=\"style.xsl\"?>", "<book\u00A0xml:id=\"simple_book\"\u00A0xmlns=\"http://docbook.org/ns/docbook\"\u00A0version=\"5.0\">" }, new string[] { "<chapter\u00A0xml:id=\"chapter_1\">", "<?php\u00A0echo\u00A0$a;\u00A0?>"
			, "<!--\u00A0This\u00A0is\u00A0an\u00A0SGML/XML\u00A0comment\u00A0\"Hi!\"\u00A0-->", "<p>", "</p>", "<p-fix\u00A0/\u00A0>" }, new string[] { "<a href=\"http:\\\\it's\\here\">", "<quote orig_author='some \"dude'/>", "<", "not", "sgmltag" }, 
			new string[] { "<", "quote", "previouspost", "=", "''", ">", ">", "I", "really", "do", "n't", "want", "to", "process", "this", "junk", ".", ">", "No", "one", "said", "you", "did", ",", "runny", ".", "What", "'s", "got", "you", "so", "scared"
			, ",", "anyway", "?", "-", "''", ">" }, new string[] { "&lt;b...@canada.com&gt;", "funky@thedismalscience.net", "<myemail@where.com>" }, new string[] { "<DOC>", "<DOCID>", "nyt960102", ".0516", "</DOCID>", "<STORYID\u00A0cat=w\u00A0pri=u>", 
			"A0264", "</STORYID>", "<SLUG\u00A0fv=ttj-z>" }, new string[] { "<!--\u00A0copy\u00A0from\u00A0here\u00A0-->", "<a\u00A0href=\"http://strategis.gc.ca/epic/internet/inabc-eac.nsf/en/home\">", "<", "img", "src", "=", "``", "id-images/ad-220x80_01e.jpg"
			, "''", "alt", "=", "``", "Aboriginal", "Business", "Canada", ":", "Opening", "New", "Doors", "for", "Your", "Business", "''", "width", "=", "``", "220", "''", "height", "=", "``", "80", "''", "border", "=", "``", "0", "''", ">", "</a>", "<!--\u00A0copy\u00A0to\u00A0here\u00A0-->"
			, "Small", "ABC", "Graphic", "Instructions", "1", "." }, new string[] { "We", "traveled", "from", "No", ".", "Korea", "to", "the", "U.S.A.", "Why", "?" } };

		// spaces go to &nbsp; \u00A0
		[Test]
		public virtual void TestPTBTokenizerTokenizePerLineSGML()
		{
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory("tokenizePerLine=true,invertible");
			RunOnTwoArrays(tokFactory, sgmlInputs, sgmlPerLineGold);
			RunAgainstOrig(tokFactory, sgmlInputs);
		}

		[Test]
		public virtual void TestPTBTokenizerTokenizeSplitHyphens()
		{
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory("splitHyphenated=true,invertible");
			RunOnTwoArrays(tokFactory, ptbInputs, ptbGoldSplitHyphenated);
			RunAgainstOrig(tokFactory, ptbInputs);
		}

		[Test]
		public virtual void TestFractions()
		{
			string[] sample = new string[] { "5-1/4 plus 2 3/16 = 7\u00A07/16 in the U.S.S.R. Why not?" };
			string[][] tokenizedNormal = new string[][] { new string[] { "5-1/4", "plus", "2\u00A03/16", "=", "7\u00A07/16", "in", "the", "U.S.S.R.", ".", "Why", "not", "?" } };
			string[][] tokenizedStrict = new string[][] { new string[] { "5-1/4", "plus", "2", "3/16", "=", "7", "7/16", "in", "the", "U.S.S.R", ".", "Why", "not", "?" } };
			ITokenizerFactory<CoreLabel> tokFactoryNormal = PTBTokenizer.CoreLabelFactory("invertible=true");
			ITokenizerFactory<CoreLabel> tokFactoryStrict = PTBTokenizer.CoreLabelFactory("strictTreebank3=true,invertible=true");
			RunOnTwoArrays(tokFactoryNormal, sample, tokenizedNormal);
			RunOnTwoArrays(tokFactoryStrict, sample, tokenizedStrict);
			RunAgainstOrig(tokFactoryNormal, sample);
			RunAgainstOrig(tokFactoryStrict, sample);
		}

		private static void RunOnTwoArrays<T>(ITokenizerFactory<T> tokFactory, string[] inputs, string[][] desired)
			where T : ILabel
		{
			NUnit.Framework.Assert.AreEqual(inputs.Length, desired.Length, "Test data arrays don't match in length");
			for (int sent = 0; sent < inputs.Length; sent++)
			{
				// System.err.println("Testing " + inputs[sent]);
				ITokenizer<T> tok = tokFactory.GetTokenizer(new StringReader(inputs[sent]));
				for (int i = 0; tok.MoveNext() || i < desired[sent].Length; i++)
				{
					if (!tok.MoveNext())
					{
						NUnit.Framework.Assert.Fail("PTBTokenizer generated too few tokens for sentence " + sent + "! Missing " + desired[sent][i]);
					}
					T w = tok.Current;
					if (i >= desired[sent].Length)
					{
						NUnit.Framework.Assert.Fail("PTBTokenizer generated too many tokens for sentence " + sent + "! Added " + w.Value());
					}
					else
					{
						NUnit.Framework.Assert.AreEqual(desired[sent][i], w.Value(), "PTBTokenizer got wrong token");
					}
				}
			}
		}

		private static void RunOnTwoArraysWithOffsets<T>(ITokenizerFactory<T> tokFactory, string[] inputs, string[][] desired)
			where T : CoreLabel
		{
			NUnit.Framework.Assert.AreEqual(inputs.Length, desired.Length, "Test data arrays don't match in length");
			for (int sent = 0; sent < inputs.Length; sent++)
			{
				// System.err.println("Testing " + inputs[sent]);
				ITokenizer<T> tok = tokFactory.GetTokenizer(new StringReader(inputs[sent]));
				for (int i = 0; tok.MoveNext() || i < desired[sent].Length; i++)
				{
					if (!tok.MoveNext())
					{
						NUnit.Framework.Assert.Fail("PTBTokenizer generated too few tokens for sentence " + sent + "! Missing " + desired[sent][i]);
					}
					T w = tok.Current;
					if (i >= desired[sent].Length)
					{
						NUnit.Framework.Assert.Fail("PTBTokenizer generated too many tokens for sentence " + sent + "! Added " + w.Value());
					}
					else
					{
						NUnit.Framework.Assert.AreEqual(desired[sent][i], w.Value(), "PTBTokenizer got wrong token");
						NUnit.Framework.Assert.AreEqual(desired[sent][i].Length, w.EndPosition() - w.BeginPosition(), "PTBTokenizer charOffsets wrong for " + desired[sent][i]);
					}
				}
			}
		}

		/// <summary>
		/// The appending has to run one behind so as to make sure that the after annotation has been filled in!
		/// Just placing the appendTextFrom() after reading tok.next() in the loop does not work.
		/// </summary>
		private static void RunAgainstOrig<T>(ITokenizerFactory<T> tokFactory, string[] inputs)
			where T : CoreLabel
		{
			foreach (string input in inputs)
			{
				// System.err.println("Running on line: |" + input + "|");
				StringBuilder origText = new StringBuilder();
				T last = null;
				for (ITokenizer<T> tok = tokFactory.GetTokenizer(new StringReader(input)); tok.MoveNext(); )
				{
					AppendTextFrom(origText, last);
					last = tok.Current;
				}
				AppendTextFrom(origText, last);
				NUnit.Framework.Assert.AreEqual(input, origText.ToString(), "PTBTokenizer has wrong originalText");
			}
		}

		private static void AppendTextFrom<T>(StringBuilder origText, T token)
			where T : CoreLabel
		{
			if (token != null)
			{
				// System.err.println("|Before|OrigText|After| = |" + token.get(CoreAnnotations.BeforeAnnotation.class) +
				//         "|" + token.get(CoreAnnotations.OriginalTextAnnotation.class) + "|" + token.get(CoreAnnotations.AfterAnnotation.class) + "|");
				if (origText.Length == 0)
				{
					origText.Append(token.Get(typeof(CoreAnnotations.BeforeAnnotation)));
				}
				origText.Append(token.Get(typeof(CoreAnnotations.OriginalTextAnnotation)));
				origText.Append(token.Get(typeof(CoreAnnotations.AfterAnnotation)));
			}
		}

		[Test]
		public virtual void TestPTBTokenizerGerman()
		{
			string[] sample = new string[] { "Das TV-Duell von Kanzlerin Merkel und SPD-Herausforderer Steinbrück war eher lahm - können es die Spitzenleute der kleinen Parteien besser? ", "Die erquickende Sicherheit und Festigkeit in der Bewegung, den Vorrat von Kraft, kann ja die Versammlung nicht fühlen, hören will sie sie nicht, also muß sie sie sehen; und die sehe man einmal in einem Paar spitzen Schultern, zylindrischen Schenkeln, oder leeren Ärmeln, oder lattenförmigen Beinen."
				 };
			string[][] tokenized = new string[][] { new string[] { "Das", "TV-Duell", "von", "Kanzlerin", "Merkel", "und", "SPD-Herausforderer", "Steinbrück", "war", "eher", "lahm", "-", "können", "es", "die", "Spitzenleute", "der", "kleinen", "Parteien"
				, "besser", "?" }, new string[] { "Die", "erquickende", "Sicherheit", "und", "Festigkeit", "in", "der", "Bewegung", ",", "den", "Vorrat", "von", "Kraft", ",", "kann", "ja", "die", "Versammlung", "nicht", "fühlen", ",", "hören", "will", "sie"
				, "sie", "nicht", ",", "also", "muß", "sie", "sie", "sehen", ";", "und", "die", "sehe", "man", "einmal", "in", "einem", "Paar", "spitzen", "Schultern", ",", "zylindrischen", "Schenkeln", ",", "oder", "leeren", "Ärmeln", ",", "oder", "lattenförmigen"
				, "Beinen", "." } };
			ITreebankLanguagePack tlp = new NegraPennLanguagePack();
			ITokenizerFactory tokFactory = tlp.GetTokenizerFactory();
			RunOnTwoArrays(tokFactory, sample, tokenized);
		}

		private readonly string[] mtInputs = new string[] { "Enter an option [?/Current]:{1}", "for example, {1}http://www.autodesk.com{2}, or a path", "enter {3}@{4} at the Of prompt.", "{1}block name={2}", "1202-03-04 5:32:56 2004-03-04T18:32:56", 
			"20°C is 68°F because 0℃ is 32℉", "a.jpg a-b.jpg a.b.jpg a-b.jpg a_b.jpg a-b-c.jpg 0-1-2.jpg a-b/c-d_e.jpg a-b/c-9a9_9a.jpg\n", "¯\\_(ツ)_/¯", "#hashtag #Azərbaycanca #mûǁae #Čeština #日本語ハッシュタグ #1 #23 #Trump2016 @3 @acl_2016", "Sect. 793 of the Penal Code"
			, "Pls. copy the text within this quote to the subject part of your email and explain wrt. the principles." };

		private readonly string[][] mtGold = new string[][] { new string[] { "Enter", "an", "option", "-LSB-", "?", "/", "Current", "-RSB-", ":", "-LCB-", "1", "-RCB-" }, new string[] { "for", "example", ",", "-LCB-", "1", "-RCB-", "http://www.autodesk.com"
			, "-LCB-", "2", "-RCB-", ",", "or", "a", "path" }, new string[] { "enter", "-LCB-", "3", "-RCB-", "@", "-LCB-", "4", "-RCB-", "at", "the", "Of", "prompt", "." }, new string[] { "-LCB-", "1", "-RCB-", "block", "name", "=", "-LCB-", "2", "-RCB-"
			 }, new string[] { "1202-03-04", "5:32:56", "2004-03-04T18:32:56" }, new string[] { "20", "°C", "is", "68", "°F", "because", "0", "℃", "is", "32", "℉" }, new string[] { "a.jpg", "a-b.jpg", "a.b.jpg", "a-b.jpg", "a_b.jpg", "a-b-c.jpg", "0-1-2.jpg"
			, "a-b/c-d_e.jpg", "a-b/c-9a9_9a.jpg" }, new string[] { "¯\\_-LRB-ツ-RRB-_/¯" }, new string[] { "#hashtag", "#Azərbaycanca", "#mûǁae", "#Čeština", "#日本語ハッシュタグ", "#", "1", "#", "23", "#Trump2016", "@", "3", "@acl_2016" }, new string[] { "Sect."
			, "793", "of", "the", "Penal", "Code" }, new string[] { "Pls.", "copy", "the", "text", "within", "this", "quote", "to", "the", "subject", "part", "of", "your", "email", "and", "explain", "wrt.", "the", "principles", "." } };

		[Test]
		public virtual void TestPTBTokenizerMT()
		{
			ITokenizerFactory<Word> tokFactory = PTBTokenizer.Factory();
			RunOnTwoArrays(tokFactory, mtInputs, mtGold);
		}

		private readonly string[] emojiInputs = new string[] { "\uD83D\uDE09\uD83D\uDE00\uD83D\uDE02\uD83D\uDE0D\uD83E\uDD21\uD83C\uDDE6\uD83C\uDDFA\uD83C\uDF7A", "\uD83D\uDC66\uD83C\uDFFB\uD83D\uDC67\uD83C\uDFFF", "\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67\uD83E\uDDC0"
			, "\u00AE\u203C\u2198\u231A\u2328\u23F0\u2620\u26BD\u2705\u2757", "⚠⚠️⚠︎❤️❤", "\uD83D\uDC69\u200D⚖\uD83D\uDC68\uD83C\uDFFF\u200D\uD83C\uDFA4" };

		private readonly string[][] emojiGold = new string[][] { new string[] { "\uD83D\uDE09", "\uD83D\uDE00", "\uD83D\uDE02", "\uD83D\uDE0D", "\uD83E\uDD21", "\uD83C\uDDE6\uD83C\uDDFA", "\uD83C\uDF7A" }, new string[] { "\uD83D\uDC66\uD83C\uDFFB", 
			"\uD83D\uDC67\uD83C\uDFFF" }, new string[] { "\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67", "\uD83E\uDDC0" }, new string[] { "\u00AE", "\u203C", "\u2198", "\u231A", "\u2328", "\u23F0", "\u2620", "\u26BD", "\u2705", "\u2757" }, new string
			[] { "⚠", "⚠️", "⚠︎", "❤️", "❤" }, new string[] { "\uD83D\uDC69\u200D⚖", "\uD83D\uDC68\uD83C\uDFFF\u200D\uD83C\uDFA4" } };

		// The non-BMP Emoji end up being surrogate pair encoded in Java! This list includes a flag.
		// People with skin tones
		// A family with cheese
		// Some BMP emoji
		// Choosing emoji vs. text presentation.
		[Test]
		public virtual void TestEmoji()
		{
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory("invertible");
			RunOnTwoArraysWithOffsets(tokFactory, emojiInputs, emojiGold);
			RunAgainstOrig(tokFactory, emojiInputs);
			NUnit.Framework.Assert.AreEqual(1, "\uD83D\uDCF7".CodePointCount(0, 2));
			NUnit.Framework.Assert.AreEqual(2, "❤️".CodePointCount(0, 2));
		}

		private readonly string[] tweetInputs = new string[] { "Happy #StarWars week! Ever wonder what was going on with Uncle Owen's dad? Check out .@WHMPodcast's rant on Ep2 https://t.co/9iJMMkAokT", "RT @BiIlionaires: #TheForceAwakens inspired vehicles are a big hit in LA."
			, "“@people: A woman built the perfect #StarWars costume for her dog https://t.co/VJRQwNZB0t https://t.co/nmNROB7diR”@guacomole123", "I would like to get a 13\" MB Air with an i7@1,7GHz", "So you have audio track 1 @145bpm and global project tempo is now 145bpm"
			, "I know that the inside of the mall opens @5am.", "I have ordered Bose Headfones worth 300USD. Not 156bpmt. FCPX MP4 playback choppy on 5k iMac", "RT @Suns: What happens when you combine @50cent, #StarWars and introductions at an @NBA game? This."
			, "RT @ShirleyHoman481: '#StarWars' Premiere Street Closures Are “Bigger Than the Oscars\": Four blocks of Hollywood Blvd. -- from Highland… ht…", "In 2009, Wiesel criticized the Vatican for lifting the excommunication of controversial bishop Richard Williamson, a member of the Society of Saint Pius X."
			, "RM460.35 million" };

		private readonly string[][] tweetGold = new string[][] { new string[] { "Happy", "#StarWars", "week", "!", "Ever", "wonder", "what", "was", "going", "on", "with", "Uncle", "Owen", "'s", "dad", "?", "Check", "out", ".@WHMPodcast", "'s", "rant"
			, "on", "Ep2", "https://t.co/9iJMMkAokT" }, new string[] { "RT", "@BiIlionaires", ":", "#TheForceAwakens", "inspired", "vehicles", "are", "a", "big", "hit", "in", "LA", "." }, new string[] { "``", "@people", ":", "A", "woman", "built", "the"
			, "perfect", "#StarWars", "costume", "for", "her", "dog", "https://t.co/VJRQwNZB0t", "https://t.co/nmNROB7diR", "''", "@guacomole123" }, new string[] { "I", "would", "like", "to", "get", "a", "13", "''", "MB", "Air", "with", "an", "i7", "@"
			, "1,7", "GHz" }, new string[] { "So", "you", "have", "audio", "track", "1", "@", "145", "bpm", "and", "global", "project", "tempo", "is", "now", "145", "bpm" }, new string[] { "I", "know", "that", "the", "inside", "of", "the", "mall", "opens"
			, "@", "5", "am", "." }, new string[] { "I", "have", "ordered", "Bose", "Headfones", "worth", "300", "USD", ".", "Not", "156bpmt", ".", "FCPX", "MP4", "playback", "choppy", "on", "5k", "iMac" }, new string[] { "RT", "@Suns", ":", "What", "happens"
			, "when", "you", "combine", "@50cent", ",", "#StarWars", "and", "introductions", "at", "an", "@NBA", "game", "?", "This", "." }, new string[] { "RT", "@ShirleyHoman481", ":", "'", "#StarWars", "'", "Premiere", "Street", "Closures", "Are", "``"
			, "Bigger", "Than", "the", "Oscars", "''", ":", "Four", "blocks", "of", "Hollywood", "Blvd.", "--", "from", "Highland", "...", "ht", "..." }, new string[] { "In", "2009", ",", "Wiesel", "criticized", "the", "Vatican", "for", "lifting", "the"
			, "excommunication", "of", "controversial", "bishop", "Richard", "Williamson", ",", "a", "member", "of", "the", "Society", "of", "Saint", "Pius", "X." }, new string[] { "RM", "460.35", "million" } };

		// Should really be "Saint Pius X ." but unclear how to achieve.
		[Test]
		public virtual void TestTweets()
		{
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory("invertible");
			RunOnTwoArrays(tokFactory, tweetInputs, tweetGold);
			RunAgainstOrig(tokFactory, tweetInputs);
		}

		private readonly string[] hyphenInputs = new string[] { "\uFEFFThis is hy\u00ADphen\u00ADated and non-breaking spaces: 3\u202F456\u202F473.89", "\u0093I need \u008080.\u0094 \u0082And \u0085 dollars.\u0092", "Charles Howard ''Charlie’' Bridges and Helen Hoyle Bridges"
			, "All energy markets close at 1 p.m. except Palo Verde electricity futures and options, closing at\n" + "12:55.; Palladium and copper markets close at 1 p.m.; Silver markets close at 1:05 p.m.", "BHP is `` making the right noises.''", "``There's a saying nowadays,'' he said. ```The more you owe, the longer you live.' It means the mafia "
			 + "won't come until we have money.''\n", "\"Whereas strategic considerations have to be based on 'real- politick' and harsh facts,\" Saleem said.", "F*ck, cr-p, I met Uchenna Nnobuko yesterday.", "I´m wrong and she\u00B4s right.", "Left Duxbury Ave. and read para. 13.8 and attached 3802.doc."
			, "Phone:86-0832-2115188" };

		private readonly string[][] hyphenGold = new string[][] { new string[] { "This", "is", "hyphenated", "and", "non-breaking", "spaces", ":", "3456473.89" }, new string[] { "``", "I", "need", "€", "80", ".", "''", "`", "And", "...", "dollars", 
			".", "'" }, new string[] { "Charles", "Howard", "``", "Charlie", "''", "Bridges", "and", "Helen", "Hoyle", "Bridges" }, new string[] { "All", "energy", "markets", "close", "at", "1", "p.m.", "except", "Palo", "Verde", "electricity", "futures"
			, "and", "options", ",", "closing", "at", "12:55", ".", ";", "Palladium", "and", "copper", "markets", "close", "at", "1", "p.m.", ";", "Silver", "markets", "close", "at", "1:05", "p.m." }, new string[] { "BHP", "is", "``", "making", "the", 
			"right", "noises", ".", "''" }, new string[] { "``", "There", "'s", "a", "saying", "nowadays", ",", "''", "he", "said", ".", "``", "`", "The", "more", "you", "owe", ",", "the", "longer", "you", "live", ".", "'", "It", "means", "the", "mafia"
			, "wo", "n't", "come", "until", "we", "have", "money", ".", "''" }, new string[] { "``", "Whereas", "strategic", "considerations", "have", "to", "be", "based", "on", "`", "real", "-", "politick", "'", "and", "harsh", "facts", ",", "''", "Saleem"
			, "said", "." }, new string[] { "F*ck", ",", "cr-p", ",", "I", "met", "Uchenna", "Nnobuko", "yesterday", "." }, new string[] { "I", "'m", "wrong", "and", "she", "'s", "right", "." }, new string[] { "Left", "Duxbury", "Ave.", "and", "read", 
			"para.", "13.8", "and", "attached", "3802.doc", "." }, new string[] { "Phone", ":", "86-0832-2115188" } };

		// Text starting with BOM (should be deleted), words with soft hyphens and non-breaking space.
		// Test that some cp1252 that shouldn't be in file is normalized okay
		// remnant of "dunno" should not match prefix
		// "bad?what opinion?kisses", // Not yet sure whether to break on this one (don't on periods)
		// not working: I´m
		// { "bad", "?", "what", "opinion", "?", "kisses" },
		[Test]
		public virtual void TestHyphensQuoteAndBOM()
		{
			ITokenizerFactory<CoreLabel> tokFactory = PTBTokenizer.CoreLabelFactory("normalizeCurrency=false,invertible");
			RunOnTwoArrays(tokFactory, hyphenInputs, hyphenGold);
			RunAgainstOrig(tokFactory, hyphenInputs);
		}
	}
}

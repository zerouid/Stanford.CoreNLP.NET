using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Tests for converting token sequences to sentences.</summary>
	/// <remarks>Tests for converting token sequences to sentences. Also effectively includes some CleanXML tests.</remarks>
	/// <author>Adam Vogel</author>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	public class WordsToSentencesAnnotatorTest
	{
		[Test]
		public virtual void TestAnnotator()
		{
			string text = "I saw Dr. Spock yesterday, he was speaking with Mr. McCoy.  They were walking down Mullholand Dr. talking about www.google.com.  Dr. Spock returns!";
			RunSentence(text, 3);
			// This would fail for "Yahoo! Research", since we don't yet know to chunk "Yahoo!"
			text = "I visited Google Research.  Dr. Spock, Ph.D., was working there and said it's an awful place!  What a waste of Ms. Pacman's last remaining life. Indeed";
			RunSentence(text, 4);
		}

		private static void RunSentence(string text, int num_sentences)
		{
			Annotation doc = new Annotation(text);
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize,ssplit", "tokenize.language", "en");
			//Annotator annotator = new TokenizerAnnotator("en");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			pipeline.Annotate(doc);
			// now check what's up...
			IList<ICoreMap> sentences = doc.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.IsNotNull(sentences);
			NUnit.Framework.Assert.AreEqual(num_sentences, sentences.Count);
		}

		/*
		for(CoreMap s : sentences) {
		String position = s.get(SentencePositionAnnotation.class); // what's wrong here?
		System.out.print("position: ");
		System.out.println(position);
		//throw new RuntimeException(position);
		}
		*/
		[Test]
		public virtual void TestSentenceSplitting()
		{
			string text = "Date :\n01/02/2012\nContent :\nSome words are here .\n";
			// System.out.println(text);
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit", "ssplit.eolonly", "true", "tokenize.whitespace", "true");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(text);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			// System.out.println("* Num of sentences in text = "+sentences.size());
			// System.out.println("Sentences is " + sentences);
			NUnit.Framework.Assert.AreEqual(4, sentences.Count);
		}

		[Test]
		public virtual void TestTokenizeNLsDoesntChangeSsplitResults()
		{
			string text = "This is one sentence\n\nThis is not another with default ssplit settings.";
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit", "tokenize.options", "tokenizeNLs");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(text);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.AreEqual(1, sentences.Count);
			// make sure that there are the correct # of tokens
			// (does NOT contain NL tokens)
			IList<CoreLabel> tokens = document1.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(13, tokens.Count);
		}

		[Test]
		public virtual void TestDefaultNewlineIsSentenceBreakSettings()
		{
			string text = "This is one sentence\n\nThis is not another with default ssplit settings.";
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(text);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.AreEqual(1, sentences.Count);
			// make sure that there are the correct # of tokens
			// (does NOT contain NL tokens)
			IList<CoreLabel> tokens = document1.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(13, tokens.Count);
		}

		[Test]
		public virtual void TestTwoNewlineIsSentenceBreakSettings()
		{
			string text = "This is \none sentence\n\nThis is not another.";
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit", "ssplit.newlineIsSentenceBreak", "two");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(text);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.AreEqual(2, sentences.Count);
			// make sure that there are the correct # of tokens (does contain NL tokens)
			IList<CoreLabel> tokens = document1.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(9, tokens.Count);
		}

		[Test]
		public virtual void TestTwoNewlineIsSentenceBreakTokenizeNLs()
		{
			string text = "This is \none sentence\n\nThis is not another.";
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit", "tokenize.language", "en", "tokenize.options", "tokenizeNLs,invertible,ptb3Escaping=true", "ssplit.newlineIsSentenceBreak", "two");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(text);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.AreEqual(2, sentences.Count);
			// make sure that there are the correct # of tokens (does contain NL tokens)
			IList<CoreLabel> tokens = document1.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(9, tokens.Count);
			IList<CoreLabel> sentenceTwoTokens = sentences[1].Get(typeof(CoreAnnotations.TokensAnnotation));
			string sentenceTwo = SentenceUtils.ListToString(sentenceTwoTokens);
			NUnit.Framework.Assert.AreEqual("This is not another .", sentenceTwo, "Bad tokens in sentence");
		}

		[Test]
		public virtual void TestAlwaysNewlineIsSentenceBreakSettings()
		{
			string text = "This is \none sentence\n\nThis is not another.";
			string[] sents = new string[] { "This is", "one sentence", "This is not another ." };
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, ssplit", "ssplit.newlineIsSentenceBreak", "always");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(text);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.AreEqual(3, sentences.Count);
			// make sure that there are the correct # of tokens (count does contain NL tokens)
			IList<CoreLabel> tokens = document1.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(9, tokens.Count);
			for (int i = 0; i < Math.Min(sents.Length, sentences.Count); i++)
			{
				ICoreMap sentence = sentences[i];
				string sentenceText = SentenceUtils.ListToString(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
				NUnit.Framework.Assert.AreEqual(sents[i], sentenceText, "Bad sentence #" + i);
			}
		}

		private static readonly string[] dateLineTexts = new string[] { "<P>\n" + "GAZA, Dec. 1 (Xinhua) -- Hamas will respect any Palestinian referendum on a\n" + "peaceful settlement with Israel even if the agreement was against its agenda,\n" + "deposed Prime Minister of the Hamas government Ismail Haneya said Wednesday.\n"
			 + "</P>\n", "\nLOS ANGELES, Dec. 31 (Xinhua) -- Body", "\nCARBONDALE, United States, Dec. 13 (Xinhua) -- Body", "<P>\nBRISBANE, Australia, Jan. 1(Xinhua) -- Body.</P>", "\nRIO DE JANEIRO, Dec. 31 (Xinhua) -- Body", "\nPORT-AU-PRINCE, Jan. 1 (Xinhua) -- Body"
			, "\nWASHINGTON, May 12 (AFP) -- Body", "\nPanama  City,  Sept. 8 (CNA) -- Body", "\nUNITED NATIONS, April 3 (Xinhua) -- The", "<P>\nSAN FRANCISCO - California\n</P>", "<P>\nRIO DE JANEIRO - Edward J. Snowden\n</P>", "<P>\nPARETS DEL VALLÈS, Spain - From\n</P>"
			 };

		private static readonly string[] dateLineTokens = new string[] { "GAZA , Dec. 1 -LRB- Xinhua -RRB- --", "LOS ANGELES , Dec. 31 -LRB- Xinhua -RRB- --", "CARBONDALE , United States , Dec. 13 -LRB- Xinhua -RRB- --", "BRISBANE , Australia , Jan. 1 -LRB- Xinhua -RRB- --"
			, "RIO DE JANEIRO , Dec. 31 -LRB- Xinhua -RRB- --", "PORT-AU-PRINCE , Jan. 1 -LRB- Xinhua -RRB- --", "WASHINGTON , May 12 -LRB- AFP -RRB- --", "Panama City , Sept. 8 -LRB- CNA -RRB- --", "UNITED NATIONS , April 3 -LRB- Xinhua -RRB- --", "SAN FRANCISCO -"
			, "RIO DE JANEIRO -", "PARETS DEL VALLÈS , Spain -" };

		/// <summary>Test whether you can separate off a dateline as a separate sentence using ssplit.boundaryMultiTokenRegex.</summary>
		[Test]
		public virtual void TestDatelineSeparation()
		{
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, cleanxml, ssplit", "tokenize.language", "en", "ssplit.newlineIsSentenceBreak", "two", "ssplit.boundaryMultiTokenRegex", "( /\\*NL\\*/ /\\p{Lu}[-\\p{L}]+/+ /,/ ( /[-\\p{L}]+/+ /,/ )? "
				 + "/\\p{Lu}\\p{Ll}{2,5}\\.?/ /[1-3]?[0-9]/ /-LRB-/ /\\p{Lu}\\p{L}+/ /-RRB-/ /--/ | " + "/\\*NL\\*/ /\\p{Lu}[-\\p{Lu}]+/+ ( /,/ /[-\\p{L}]+/+ )? /-/ )");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			NUnit.Framework.Assert.AreEqual(dateLineTexts.Length, dateLineTokens.Length, "Bad test data");
			for (int i = 0; i < dateLineTexts.Length; i++)
			{
				Annotation document1 = new Annotation(dateLineTexts[i]);
				pipeline.Annotate(document1);
				IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
				// for (CoreMap sentence : sentences) {
				//   String sentenceText = SentenceUtils.listToString(sentence.get(CoreAnnotations.TokensAnnotation.class));
				//   System.err.println(sentenceText);
				// }
				NUnit.Framework.Assert.AreEqual(2, sentences.Count, "For " + dateLineTexts[i] + " annotation is " + document1);
				IList<CoreLabel> sentenceOneTokens = sentences[0].Get(typeof(CoreAnnotations.TokensAnnotation));
				string sentenceOne = SentenceUtils.ListToString(sentenceOneTokens);
				NUnit.Framework.Assert.AreEqual(dateLineTokens[i], sentenceOne, "Bad tokens in dateline");
			}
		}

		private static readonly string[] dateLineSpanishTexts = new string[] { "<P>\n" + "\nEL CAIRO, 30 jun (Xinhua) -- Al menos una persona.\n", "\nMONTEVIDEO, 1 jul (Xinhua) -- Los diarios uruguayos", "\nRIO DE JANEIRO, 30 jun (Xinhua) -- La selección brasileña"
			, "\nSALVADOR DE BAHIA, Brasil, 30 jun (Xinhua) -- La selección italiana", "\nLA HAYA, 31 dic (Xinhua) -- Dos candidatos holandeses", "\nJERUSALEN, 1 ene (Xinhua) -- El presidente de Israel", "\nCANBERRA (Xinhua) -- El calentamiento oceánico"
			 };

		private static readonly string[] dateLineSpanishTokens = new string[] { "EL CAIRO , 30 jun =LRB= Xinhua =RRB= --", "MONTEVIDEO , 1 jul =LRB= Xinhua =RRB= --", "RIO DE JANEIRO , 30 jun =LRB= Xinhua =RRB= --", "SALVADOR DE BAHIA , Brasil , 30 jun =LRB= Xinhua =RRB= --"
			, "LA HAYA , 31 dic =LRB= Xinhua =RRB= --", "JERUSALEN , 1 ene =LRB= Xinhua =RRB= --", "CANBERRA =LRB= Xinhua =RRB= --" };

		/// <summary>Test whether you can separate off a dateline as a separate sentence using ssplit.boundaryMultiTokenRegex.</summary>
		[Test]
		public virtual void TestSpanishDatelineSeparation()
		{
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, cleanxml, ssplit", "tokenize.language", "es", "tokenize.options", "tokenizeNLs,ptb3Escaping=true", "ssplit.newlineIsSentenceBreak", "two", "ssplit.boundaryMultiTokenRegex"
				, "/\\*NL\\*/ /\\p{Lu}[-\\p{L}]+/+ ( /,/  /[-\\p{L}]+/+ )? " + "( /,/ /[1-3]?[0-9]/ /\\p{Ll}{3,3}/ )? /=LRB=/ /\\p{Lu}\\p{L}+/ /=RRB=/ /--/");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			NUnit.Framework.Assert.AreEqual(dateLineSpanishTexts.Length, dateLineSpanishTokens.Length, "Bad test data");
			for (int i = 0; i < dateLineSpanishTexts.Length; i++)
			{
				Annotation document1 = new Annotation(dateLineSpanishTexts[i]);
				pipeline.Annotate(document1);
				IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
				NUnit.Framework.Assert.AreEqual(2, sentences.Count, "For " + dateLineSpanishTexts[i] + " annotation is " + document1);
				IList<CoreLabel> sentenceOneTokens = sentences[0].Get(typeof(CoreAnnotations.TokensAnnotation));
				string sentenceOne = SentenceUtils.ListToString(sentenceOneTokens);
				NUnit.Framework.Assert.AreEqual(dateLineSpanishTokens[i], sentenceOne, "Bad tokens in dateline");
			}
		}

		private const string kbpDocument = "<DOC    id=\"ENG_NW_001278_20130413_F00012OVI\">\n" + "<DATE_TIME>2013-04-13T04:49:26</DATE_TIME>\n" + "<HEADLINE>\n" + "Urgent: powerful quake jolts western Japan\n" + "</HEADLINE>\n" + "<AUTHOR>马兴华</AUTHOR>\n"
			 + "<TEXT>\n" + "Urgent: powerful quake jolts western Japan\n" + "\n" + "Urgent: powerful quake jolts western Japan\n" + "\n" + "OSAKA, April 13 (Xinhua) -- A powerful earthquake stroke a wide area in Japan's Kinki region in western Japan early Saturday. The quake was strongly felt in Osaka. Enditem\n"
			 + "</TEXT>\n" + "</DOC>\n";

		private static readonly string[] kbpSentences = new string[] { "Urgent : powerful quake jolts western Japan", "Urgent : powerful quake jolts western Japan", "Urgent : powerful quake jolts western Japan", "OSAKA , April 13 -LRB- Xinhua -RRB- --"
			, string.Empty + "A powerful earthquake stroke a wide area in Japan 's Kinki region in western Japan early Saturday .", "The quake was strongly felt in Osaka .", "Enditem" };

		/// <summary>Test written in 2017 to debug why the KBP setup doesn't work once you introduce newlineIsSentenceBreak=two.</summary>
		/// <remarks>
		/// Test written in 2017 to debug why the KBP setup doesn't work once you introduce newlineIsSentenceBreak=two.
		/// Now fixed.
		/// </remarks>
		[Test]
		public virtual void TestKbpWorks()
		{
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, cleanxml, ssplit", "tokenize.language", "en", "tokenize.options", "tokenizeNLs,invertible,ptb3Escaping=true", "ssplit.newlineIsSentenceBreak", "two", "ssplit.tokenPatternsToDiscard"
				, "\\n,\\*NL\\*", "ssplit.boundaryMultiTokenRegex", "( /\\*NL\\*/ /\\p{Lu}[-\\p{L}]+/+ /,/ ( /[-\\p{L}]+/+ /,/ )? " + "/\\p{Lu}\\p{Ll}{2,5}\\.?/ /[1-3]?[0-9]/ /-LRB-/ /\\p{Lu}\\p{L}+/ /-RRB-/ /--/ | " + "/\\*NL\\*/ /\\p{Lu}[-\\p{Lu}]+/+ ( /,/ /[-\\p{L}]+/+ )? /-/ )"
				, "clean.xmltags", "headline|dateline|text|post", "clean.singlesentencetags", "HEADLINE|DATELINE|SPEAKER|POSTER|POSTDATE", "clean.sentenceendingtags", "P|POST|QUOTE", "clean.turntags", "TURN|POST|QUOTE", "clean.speakertags", "SPEAKER|POSTER"
				, "clean.docidtags", "DOCID", "clean.datetags", "DATETIME|DATE|DATELINE", "clean.doctypetags", "DOCTYPE", "clean.docAnnotations", "docID=doc[id],doctype=doc[type],docsourcetype=doctype[source]", "clean.sectiontags", "HEADLINE|DATELINE|POST"
				, "clean.sectionAnnotations", "sectionID=post[id],sectionDate=post[date|datetime],sectionDate=postdate,author=post[author],author=poster", "clean.quotetags", "quote", "clean.quoteauthorattributes", "orig_author", "clean.tokenAnnotations", "link=a[href],speaker=post[author],speaker=quote[orig_author]"
				);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(kbpDocument);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < Math.Min(kbpSentences.Length, sentences.Count); i++)
			{
				ICoreMap sentence = sentences[i];
				string sentenceText = SentenceUtils.ListToString(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
				NUnit.Framework.Assert.AreEqual(kbpSentences[i], sentenceText, "Bad sentence #" + i);
			}
			NUnit.Framework.Assert.AreEqual(kbpSentences.Length, sentences.Count, "Bad total number of sentences");
		}

		private const string kbpSpanishDocument = "<DOC    id=\"SPA_NW_001278_20130701_F00013T62\">\n" + "<DATE_TIME>2013-07-01T03:06:44</DATE_TIME>\n" + "<HEADLINE>\n" + "Muere una persona y 37 resultan heridas en manifestación contra presidente egipcio\n"
			 + "</HEADLINE>\n" + "<AUTHOR/>\n" + "<TEXT>\n" + "Muere una persona y 37 resultan heridas en manifestación contra presidente egipcio\n" + "\n" + "EL CAIRO, 30 jun (Xinhua) -- Al menos una persona murió y 37 resultaron heridas hoy en un ataque armado lanzado en una protesta contra el presidente de Egipto, Mohamed Morsi, en Beni Suef, al sur de la capital egipcia de El Cairo, informó la agencia estatal de noticias MENA. Fin\n"
			 + "</TEXT>\n" + "</DOC>\n";

		private static readonly string[] kbpSpanishSentences = new string[] { "Muere una persona y 37 resultan heridas en manifestación contra presidente egipcio", "Muere una persona y 37 resultan heridas en manifestación contra presidente egipcio", 
			"EL CAIRO , 30 jun =LRB= Xinhua =RRB= --", "Al menos una persona murió y 37 resultaron heridas hoy en un ataque armado lanzado en una protesta contra el presidente de Egipto , Mohamed Morsi , en Beni Suef , al sur de la capital egipcia de El Cairo , informó la agencia estatal de noticias MENA ."
			, "Fin" };

		/// <summary>Test written in 2017 to debug why the KBP setup doesn't work once you introduce newlineIsSentenceBreak=two.</summary>
		/// <remarks>
		/// Test written in 2017 to debug why the KBP setup doesn't work once you introduce newlineIsSentenceBreak=two.
		/// Somehow it fell apart with Angel's complex configuration option, stuck in forced wait.
		/// </remarks>
		[Test]
		public virtual void TestKbpSpanishWorks()
		{
			Properties props = PropertiesUtils.AsProperties("annotators", "tokenize, cleanxml, ssplit", "tokenize.language", "es", "tokenize.options", "tokenizeNLs,ptb3Escaping=true", "ssplit.newlineIsSentenceBreak", "two", "ssplit.tokenPatternsToDiscard"
				, "\\n,\\*NL\\*", "ssplit.boundaryMultiTokenRegex", "/\\*NL\\*/ /\\p{Lu}[-\\p{L}]+/+ /,/ ( /[-\\p{L}]+/+ /,/ )? " + "/[1-3]?[0-9]/ /\\p{Ll}{3,5}/ /=LRB=/ /\\p{Lu}\\p{L}+/ /=RRB=/ /--/", "clean.xmltags", "headline|text|post", "clean.singlesentencetags"
				, "HEADLINE|AUTHOR", "clean.sentenceendingtags", "TEXT|POST|QUOTE", "clean.turntags", "POST|QUOTE", "clean.speakertags", "AUTHOR", "clean.datetags", "DATE_TIME", "clean.doctypetags", "DOC", "clean.docAnnotations", "docID=doc[id]", "clean.sectiontags"
				, "HEADLINE|POST", "clean.sectionAnnotations", "sectionID=post[id],sectionDate=post[datetime],author=post[author]", "clean.quotetags", "quote", "clean.quoteauthorattributes", "orig_author", "clean.tokenAnnotations", "link=a[href],speaker=post[author],speaker=quote[orig_author]"
				);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			Annotation document1 = new Annotation(kbpSpanishDocument);
			pipeline.Annotate(document1);
			IList<ICoreMap> sentences = document1.Get(typeof(CoreAnnotations.SentencesAnnotation));
			for (int i = 0; i < Math.Min(kbpSpanishSentences.Length, sentences.Count); i++)
			{
				ICoreMap sentence = sentences[i];
				string sentenceText = SentenceUtils.ListToString(sentence.Get(typeof(CoreAnnotations.TokensAnnotation)));
				NUnit.Framework.Assert.AreEqual(kbpSpanishSentences[i], sentenceText, "Bad sentence #" + i);
			}
			NUnit.Framework.Assert.AreEqual(kbpSpanishSentences.Length, sentences.Count, "Bad total number of sentences");
		}
	}
}

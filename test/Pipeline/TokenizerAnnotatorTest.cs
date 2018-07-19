using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>See TokenizerAnnotatorITest for some tests that require model files.</summary>
	/// <remarks>
	/// See TokenizerAnnotatorITest for some tests that require model files.
	/// See PTBTokenizerTest, etc. for more detailed language-specific tests.
	/// </remarks>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TokenizerAnnotatorTest
	{
		private const string text = "She'll prove it ain't so.";

		private static IList<string> tokenWords = Arrays.AsList("She", "'ll", "prove", "it", "ai", "n't", "so", ".");

		[NUnit.Framework.Test]
		public virtual void TestNewVersion()
		{
			Annotation ann = new Annotation(text);
			IAnnotator annotator = new TokenizerAnnotator("en");
			annotator.Annotate(ann);
			IEnumerator<string> it = tokenWords.GetEnumerator();
			foreach (CoreLabel word in ann.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				NUnit.Framework.Assert.AreEqual("Bung token in new CoreLabel usage", it.Current, word.Word());
			}
			NUnit.Framework.Assert.IsFalse("Too few tokens in new CoreLabel usage", it.MoveNext());
			IEnumerator<string> it2 = tokenWords.GetEnumerator();
			foreach (CoreLabel word_1 in ann.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				NUnit.Framework.Assert.AreEqual("Bung token in new CoreLabel usage", it2.Current, word_1.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			NUnit.Framework.Assert.IsFalse("Too few tokens in new CoreLabel usage", it2.MoveNext());
		}

		[NUnit.Framework.Test]
		public virtual void TestBadLanguage()
		{
			Properties props = new Properties();
			props.SetProperty("annotators", "tokenize");
			props.SetProperty("tokenize.language", "notalanguage");
			try
			{
				new StanfordCoreNLP(props);
				throw new Exception("Should have failed");
			}
			catch (ArgumentException)
			{
			}
		}

		// yay, passed
		[NUnit.Framework.Test]
		public virtual void TestDefaultNoNLsPipeline()
		{
			string t = "Text with \n\n a new \nline.";
			IList<string> tWords = Arrays.AsList("Text", "with", "a", "new", "line", ".");
			Properties props = new Properties();
			props.SetProperty("annotators", "tokenize");
			Annotation ann = new Annotation(t);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			pipeline.Annotate(ann);
			IEnumerator<string> it = tWords.GetEnumerator();
			foreach (CoreLabel word in ann.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				NUnit.Framework.Assert.AreEqual("Bung token in new CoreLabel usage", it.Current, word.Word());
			}
			NUnit.Framework.Assert.IsFalse("Too few tokens in new CoreLabel usage", it.MoveNext());
			IEnumerator<string> it2 = tWords.GetEnumerator();
			foreach (CoreLabel word_1 in ann.Get(typeof(CoreAnnotations.TokensAnnotation)))
			{
				NUnit.Framework.Assert.AreEqual("Bung token in new CoreLabel usage", it2.Current, word_1.Get(typeof(CoreAnnotations.TextAnnotation)));
			}
			NUnit.Framework.Assert.IsFalse("Too few tokens in new CoreLabel usage", it2.MoveNext());
		}

		[NUnit.Framework.Test]
		public virtual void TestHyphens()
		{
			string test = "Hyphen-ated words should be split except when school-aged-children eat " + "anti-disestablishmentariansm for breakfast at the o-kay choral infront of some explor-o-toriums.";
			Properties props = new Properties();
			props.SetProperty("annotators", "tokenize");
			Annotation ann = new Annotation(test);
			StanfordCoreNLP pipeline = new StanfordCoreNLP(props);
			pipeline.Annotate(ann);
			IList<CoreLabel> toks = ann.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(21, toks.Count);
			Properties props2 = new Properties();
			props2.SetProperty("annotators", "tokenize");
			props2.SetProperty("tokenize.options", "splitHyphenated=true");
			Annotation ann2 = new Annotation(test);
			StanfordCoreNLP pipeline2 = new StanfordCoreNLP(props2);
			pipeline2.Annotate(ann2);
			IList<CoreLabel> toks2 = ann2.Get(typeof(CoreAnnotations.TokensAnnotation));
			NUnit.Framework.Assert.AreEqual(27, toks2.Count);
		}
	}
}

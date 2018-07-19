using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>Tests Chunk Annotation Utility functions</summary>
	/// <author>Angel Chang</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ChunkAnnotationUtilsTest
	{
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestMergeChunks()
		{
			// Create 4 sentences
			string text = "I have created sentence1.  And then sentence2.  Now sentence3. Finally sentence4.";
			IAnnotator tokenizer = new TokenizerAnnotator("en");
			IAnnotator ssplit = new WordsToSentencesAnnotator();
			Annotation annotation = new Annotation(text);
			tokenizer.Annotate(annotation);
			ssplit.Annotate(annotation);
			// Get sentences
			IList<ICoreMap> sentences = annotation.Get(typeof(CoreAnnotations.SentencesAnnotation));
			NUnit.Framework.Assert.AreEqual("4 sentence expected", 4, sentences.Count);
			// Merge last 3 into one
			ChunkAnnotationUtils.MergeChunks(sentences, text, 1, 4);
			NUnit.Framework.Assert.AreEqual("2 sentence expected", 2, sentences.Count);
		}
	}
}

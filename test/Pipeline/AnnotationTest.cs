using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Util;


namespace Edu.Stanford.Nlp.Pipeline
{
	/// <author>John Bauer</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class AnnotationTest
	{
		/// <summary>Test a bug a user reported where the text would wind up having the list toString used, adding extra []</summary>
		[NUnit.Framework.Test]
		public virtual void TestFromList()
		{
			IList<ICoreMap> sentences = Generics.NewArrayList();
			ICoreMap sentence = new ArrayCoreMap();
			IList<CoreLabel> words = SentenceUtils.ToCoreLabelList("This", "is", "a", "test", ".");
			sentence.Set(typeof(CoreAnnotations.TokensAnnotation), words);
			sentences.Add(sentence);
			Annotation annotation = new Annotation(sentences);
			NUnit.Framework.Assert.AreEqual("This is a test .", annotation.ToString());
			sentence.Set(typeof(CoreAnnotations.TextAnnotation), "This is a test.");
			annotation = new Annotation(sentences);
			NUnit.Framework.Assert.AreEqual("This is a test.", annotation.ToString());
		}
	}
}

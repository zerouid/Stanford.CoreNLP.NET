using Edu.Stanford.Nlp.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Pipeline
{
	/// <summary>
	/// A very basic test for
	/// <see cref="CoNLLOutputter"/>
	/// .
	/// </summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CoNLLOutputterTest
	{
		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestSimpleSentence()
		{
			Annotation ann = new Annotation("CoNLL is neat. Better than XML.");
			StanfordCoreNLP pipeline = new StanfordCoreNLP(PropertiesUtils.AsProperties("annotators", "tokenize, ssplit"));
			pipeline.Annotate(ann);
			string actual = new CoNLLOutputter().Print(ann);
			string expected = "1\tCoNLL\t_\t_\t_\t_\t_\n" + "2\tis\t_\t_\t_\t_\t_\n" + "3\tneat\t_\t_\t_\t_\t_\n" + "4\t.\t_\t_\t_\t_\t_\n" + '\n' + "1\tBetter\t_\t_\t_\t_\t_\n" + "2\tthan\t_\t_\t_\t_\t_\n" + "3\tXML\t_\t_\t_\t_\t_\n" + "4\t.\t_\t_\t_\t_\t_\n"
				 + '\n';
			NUnit.Framework.Assert.AreEqual(expected, actual);
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCustomSimpleSentence()
		{
			Annotation ann = new Annotation("CoNLL is neat. Better than XML.");
			string outputKeys = "word,pos";
			StanfordCoreNLP pipeline = new StanfordCoreNLP(PropertiesUtils.AsProperties("annotators", "tokenize, ssplit", "outputFormatOptions", outputKeys));
			pipeline.Annotate(ann);
			string actual = new CoNLLOutputter(outputKeys).Print(ann);
			string expected = "CoNLL\t_\n" + "is\t_\n" + "neat\t_\n" + ".\t_\n" + '\n' + "Better\t_\n" + "than\t_\n" + "XML\t_\n" + ".\t_\n" + '\n';
			NUnit.Framework.Assert.AreEqual(expected, actual);
		}
	}
}

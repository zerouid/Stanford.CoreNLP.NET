using System.Collections.Generic;
using Edu.Stanford.Nlp.IE.Machinereading.Structure;
using Edu.Stanford.Nlp.Ling;
using Java.Util;
using Java.Util.Stream;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Naturalli
{
	/// <summary>Some tests for the simpler methods in Util -- e.g., the ones manipulating NER spans.</summary>
	/// <author>Gabor Angeli</author>
	[NUnit.Framework.TestFixture]
	public class UtilTest
	{
		private CoreLabel MkLabel(string word, string ner)
		{
			CoreLabel label = new CoreLabel();
			label.SetWord(word);
			label.SetOriginalText(word);
			label.SetNER(ner);
			return label;
		}

		private IList<CoreLabel> MockLabels(string input)
		{
			return Arrays.Stream(input.Split(" ")).Map(null).Collect(Collectors.ToList());
		}

		[Test]
		public virtual void GuessNERSpan()
		{
			NUnit.Framework.Assert.AreEqual("O", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the black cat"), new Span(0, 3)));
			NUnit.Framework.Assert.AreEqual("PERSON", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the president Obama_PERSON"), new Span(0, 3)));
			NUnit.Framework.Assert.AreEqual("TITLE", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the President_TITLE Obama_PERSON"), new Span(0, 2)));
			NUnit.Framework.Assert.AreEqual("PERSON", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the President_TITLE Obama_PERSON"), new Span(2, 3)));
			NUnit.Framework.Assert.AreEqual("PERSON", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON"), new Span(0, 4)));
		}

		[Test]
		public virtual void GuessNER()
		{
			NUnit.Framework.Assert.AreEqual("O", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the black cat")));
			NUnit.Framework.Assert.AreEqual("PERSON", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the president Obama_PERSON")));
			NUnit.Framework.Assert.AreEqual("PERSON", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON")));
		}

		[Test]
		public virtual void ExtractNER()
		{
			NUnit.Framework.Assert.AreEqual("O", Edu.Stanford.Nlp.Naturalli.Util.GuessNER(MockLabels("the black cat")));
			NUnit.Framework.Assert.AreEqual(new Span(2, 3), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the president Obama_PERSON"), new Span(2, 3)));
			NUnit.Framework.Assert.AreEqual(new Span(2, 3), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the president Obama_PERSON"), new Span(1, 3)));
			NUnit.Framework.Assert.AreEqual(new Span(2, 3), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the president Obama_PERSON"), new Span(0, 3)));
			NUnit.Framework.Assert.AreEqual(new Span(2, 4), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON"), new Span(2, 4)));
			NUnit.Framework.Assert.AreEqual(new Span(2, 4), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON"), new Span(2, 3)));
			NUnit.Framework.Assert.AreEqual(new Span(1, 2), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON"), new Span(0, 2)));
			NUnit.Framework.Assert.AreEqual(new Span(1, 2), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(0, 2)));
			NUnit.Framework.Assert.AreEqual(new Span(2, 4), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(2, 5)));
			NUnit.Framework.Assert.AreEqual(new Span(2, 4), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(2, 6)));
			NUnit.Framework.Assert.AreEqual(new Span(5, 6), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(5, 6)));
			NUnit.Framework.Assert.AreEqual(new Span(5, 6), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(4, 6)));
		}

		[Test]
		public virtual void ExtractNERDifferingTypes()
		{
			NUnit.Framework.Assert.AreEqual(new Span(2, 4), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(0, 5)));
			NUnit.Framework.Assert.AreEqual(new Span(5, 10), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited The_LOCATION Peoples_LOCATION Republic_LOCATION of_LOCATION China_LOCATION"), new 
				Span(0, 10)));
		}

		[Test]
		public virtual void ExtractNERNoNER()
		{
			NUnit.Framework.Assert.AreEqual(new Span(0, 1), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the President_TITLE"), new Span(0, 1)));
			NUnit.Framework.Assert.AreEqual(new Span(0, 1), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the honorable President_TITLE"), new Span(0, 1)));
			NUnit.Framework.Assert.AreEqual(new Span(0, 2), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the honorable President_TITLE"), new Span(0, 2)));
			NUnit.Framework.Assert.AreEqual(new Span(0, 2), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the honorable Mr. President_TITLE"), new Span(0, 2)));
			NUnit.Framework.Assert.AreEqual(new Span(1, 2), Edu.Stanford.Nlp.Naturalli.Util.ExtractNER(MockLabels("the honorable Mr. President_TITLE"), new Span(1, 2)));
		}

		[Test]
		public virtual void NerOverlap()
		{
			NUnit.Framework.Assert.AreEqual(true, Edu.Stanford.Nlp.Naturalli.Util.NerOverlap(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(0, 1), new Span(0, 1)));
			NUnit.Framework.Assert.AreEqual(true, Edu.Stanford.Nlp.Naturalli.Util.NerOverlap(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(1, 2), new Span(1, 2)));
			NUnit.Framework.Assert.AreEqual(true, Edu.Stanford.Nlp.Naturalli.Util.NerOverlap(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(1, 2), new Span(0, 2)));
			NUnit.Framework.Assert.AreEqual(false, Edu.Stanford.Nlp.Naturalli.Util.NerOverlap(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(1, 2), new Span(2, 4)));
			NUnit.Framework.Assert.AreEqual(true, Edu.Stanford.Nlp.Naturalli.Util.NerOverlap(MockLabels("the President_TITLE Barack_PERSON Obama_PERSON visited China_LOCATION"), new Span(1, 4), new Span(2, 4)));
		}
	}
}

using Sharpen;

namespace Edu.Stanford.Nlp.IE
{
	/// <summary>
	/// Notes: This tests the old code that is independent of SUTime!
	/// The test that checks the integration of SUTime with QEN is NumberSequenceClassifierITest.
	/// </summary>
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class QuantifiableEntityNormalizerTest
	{
		private string[] dateStrings = new string[] { "February 5, 1923", "Mar 3", "18 July 2005", "18 Sep 05", "Jan. 13 , '90", "Jan. 13", "2009-07-19", "2007-06-16" };

		private string[] dateAnswers = new string[] { "19230205", "****0303", "20050718", "20050918", "19900113", "****0113", "20090719", "20070616" };

		private string[] percentStrings = new string[] { "one percent", "% 8", "8 %", "8.25 %", "48 percent", "%4.9" };

		private string[] percentAnswers = new string[] { "%1.0", "%8.0", "%8.0", "%8.25", "%48.0", "%4.9" };

		private string[] moneyStrings = new string[] { "24 cents", "18\u00A2", "250 won", "\u00A35.40", "10 thousand million pounds", "10 thousand million dollars", "million dollars", "four million dollars", "$1m", "50 million yuan", "50 cents", "# 1500"
			, "\u00A3 1500", "\u00A3 .50", "# .50", "$ 1500", "$1500", "$ 1,500", "$1,500", "$48.75", "$ 57 . 60", "2.30", "8 million", "$8 thousand", "$42,33" };

		private string[] moneyAnswers = new string[] { "$0.24", "$0.18", "\u20A9250.0", "\u00A35.4", "\u00A31.0E10", "$1.0E10", "$1000000.0", "$4000000.0", "$1000000.0", "\u51435.0E7", "$0.5", "\u00A31500.0", "\u00A31500.0", "\u00A30.5", "\u00A30.5"
			, "$1500.0", "$1500.0", "$1500.0", "$1500.0", "$48.75", "$57.6", "$2.3", "$8000000.0", "$8000.0", "$42.33" };

		private string[] numberStrings = new string[] { "twenty-five", "1.3 million", "10 thousand million", "3.625", "-15", "117-111", string.Empty, " ", "   " };

		private string[] numberAnswers = new string[] { "25.0", "1300000.0", "1.0E10", "3.625", "-15.0", "117.0 - 111.0", string.Empty, " ", "   " };

		private string[] ordinalStrings = new string[] { "twelfth", "twenty-second", "0th", "1,000th" };

		private string[] ordinalAnswers = new string[] { "12.0", "22.0", "0.0", "1000.0" };

		private string[] timeStrings = new string[] { "4:30", "11:00 pm", "2 am", "12:29 p.m.", "midnight", "22:26:48" };

		private string[] timeAnswers = new string[] { "4:30", "11:00pm", "2:00am", "12:29pm", "00:00am", "22:26:48" };

		[NUnit.Framework.Test]
		public virtual void TestDateNormalization()
		{
			NUnit.Framework.Assert.AreEqual(dateStrings.Length, dateAnswers.Length);
			for (int i = 0; i < dateStrings.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual("Testing " + dateStrings[i], dateAnswers[i], QuantifiableEntityNormalizer.NormalizedDateString(dateStrings[i], null));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestPercentNormalization()
		{
			NUnit.Framework.Assert.AreEqual(percentStrings.Length, percentAnswers.Length);
			for (int i = 0; i < percentStrings.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(percentAnswers[i], QuantifiableEntityNormalizer.NormalizedPercentString(percentStrings[i], null));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestMoneyNormalization()
		{
			NUnit.Framework.Assert.AreEqual(moneyStrings.Length, moneyAnswers.Length);
			for (int i = 0; i < moneyStrings.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(moneyAnswers[i], QuantifiableEntityNormalizer.NormalizedMoneyString(moneyStrings[i], null));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestNumberNormalization()
		{
			NUnit.Framework.Assert.AreEqual(numberStrings.Length, numberAnswers.Length);
			for (int i = 0; i < numberStrings.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(numberAnswers[i], QuantifiableEntityNormalizer.NormalizedNumberString(numberStrings[i], string.Empty, null));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestOrdinalNormalization()
		{
			NUnit.Framework.Assert.AreEqual(ordinalStrings.Length, ordinalAnswers.Length);
			for (int i = 0; i < ordinalStrings.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(ordinalAnswers[i], QuantifiableEntityNormalizer.NormalizedOrdinalString(ordinalStrings[i], null));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestTimeNormalization()
		{
			NUnit.Framework.Assert.AreEqual(timeStrings.Length, timeAnswers.Length);
			for (int i = 0; i < timeStrings.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual(timeAnswers[i], QuantifiableEntityNormalizer.NormalizedTimeString(timeStrings[i], null));
			}
		}
	}
}

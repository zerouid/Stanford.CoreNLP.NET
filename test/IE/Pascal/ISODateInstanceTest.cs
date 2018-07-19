using Sharpen;

namespace Edu.Stanford.Nlp.IE.Pascal
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ISODateInstanceTest
	{
		private string[] dateStrings = new string[] { "February 5, 1923", "Mar 3", "18 July 2005", "18 Sep 05", "Jan. 13 , '90", "Jan. 13", "01/03/07", "03-27-85", "1900-1946", "1900--1946", "June 8-10", "today, Saturday", "Saturday, June 10", "Dec. 27"
			, "1438143814381434" };

		private string[] dateAnswers = new string[] { "19230205", "****0303", "20050718", "20050918", "19900113", "****0113", "20070103", "19850327", "1900/1946", "1900/1946", "****0608/****0610", "saturday", "****0610", "****1227", "1438" };

		private string[] staticCompatibleStrings1 = new string[] { "20071203", "****1203", "200712", "****1112" };

		private string[] staticCompatibleStrings2 = new string[] { "20071203", "20071203", "200412", "******12" };

		private bool[] staticCompatibleAnswers = new bool[] { true, true, false, true };

		private string[] staticAfterStrings2 = new string[] { "20071203", "20071203", "200712", "200712", "200701", "****05", "200703", "200703", "****11", "******03" };

		private string[] staticAfterStrings1 = new string[] { "20071207", "2008", "2008", "2007", "200703", "****06", "2006", "200701", "******03", "****11" };

		private bool[] staticAfterAnswers = new bool[] { true, true, true, false, true, true, false, false, true, true };

		[NUnit.Framework.Test]
		public virtual void TestDateNormalization()
		{
			NUnit.Framework.Assert.AreEqual(dateStrings.Length, dateAnswers.Length);
			for (int i = 0; i < dateStrings.Length; i++)
			{
				ISODateInstance d = new ISODateInstance(dateStrings[i]);
				NUnit.Framework.Assert.AreEqual("Testing " + dateStrings[i], dateAnswers[i], d.ToString());
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestIsAfter()
		{
			for (int i = 0; i < staticAfterStrings1.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual("Testing " + staticAfterStrings1[i] + " and " + staticAfterStrings2[i], staticAfterAnswers[i], ISODateInstance.IsAfter(staticAfterStrings1[i], staticAfterStrings2[i]));
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestIsCompatible()
		{
			for (int i = 0; i < staticCompatibleStrings1.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual("Testing " + staticCompatibleStrings1[i] + " and " + staticCompatibleStrings2[i], staticCompatibleAnswers[i], ISODateInstance.IsCompatible(staticCompatibleStrings1[i], staticCompatibleStrings2[i]));
			}
		}

		private string[] originalDates = new string[] { "18 July 2005", "18 July 2005", "18 July 2005", "1 February 2008", "1 February 2008", "1 February", "1 February", "1 January 2008", "31 December 2007", "1 January", "31 December" };

		private string[] relativeArguments = new string[] { "today", "tomorrow", "yesterday", "tomorrow", "yesterday", "tomorrow", "yesterday", "yesterday", "tomorrow", "yesterday", "tomorrow" };

		private string[] relativeDateAnswers = new string[] { "20050718", "20050719", "20050717", "20080202", "20080131", "****0202", "****0131", "20071231", "20080101", "****1231", "****0101" };

		[NUnit.Framework.Test]
		public virtual void TestRelativeDateCreation()
		{
			for (int i = 0; i < originalDates.Length; i++)
			{
				NUnit.Framework.Assert.AreEqual("Testing " + relativeArguments[i] + " with respect to " + originalDates[i], relativeDateAnswers[i], (new ISODateInstance(new ISODateInstance(originalDates[i]), relativeArguments[i])).GetDateString());
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestContains()
		{
		}
		//TODO: implement!
	}
}

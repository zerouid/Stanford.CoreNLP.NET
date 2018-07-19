using NUnit.Framework;
using Org.Joda.Time;
using Sharpen;

namespace Edu.Stanford.Nlp.Time
{
	/// <author>Gabor Angeli (angeli at cs.stanford)</author>
	[NUnit.Framework.TestFixture]
	public class JodaTimeTests : TestSuite
	{
		[Test]
		public virtual void TimexDurationValue()
		{
			JodaTimeUtils.ConversionOptions opts = new JodaTimeUtils.ConversionOptions();
			//--2 Decades
			NUnit.Framework.Assert.AreEqual("P2E", JodaTimeUtils.TimexDurationValue(Period.Years(20), opts));
			opts.forceUnits = new string[] { "Y" };
			NUnit.Framework.Assert.AreEqual("P20Y", JodaTimeUtils.TimexDurationValue(Period.Years(20), opts));
			opts.forceUnits = new string[] { "L" };
			NUnit.Framework.Assert.AreEqual("P2E", JodaTimeUtils.TimexDurationValue(Period.Years(20), opts));
			opts.approximate = true;
			NUnit.Framework.Assert.AreEqual("PXE", JodaTimeUtils.TimexDurationValue(Period.Years(20), opts));
			opts.forceUnits = new string[] { "Y" };
			NUnit.Framework.Assert.AreEqual("PXY", JodaTimeUtils.TimexDurationValue(Period.Years(20), opts));
			opts = new JodaTimeUtils.ConversionOptions();
			//--Quarters
			NUnit.Framework.Assert.AreEqual("P2Q", JodaTimeUtils.TimexDurationValue(Period.Months(6), opts));
			opts.forceUnits = new string[] { "M" };
			NUnit.Framework.Assert.AreEqual("P6M", JodaTimeUtils.TimexDurationValue(Period.Months(6), opts));
			opts.approximate = true;
			NUnit.Framework.Assert.AreEqual("PXM", JodaTimeUtils.TimexDurationValue(Period.Months(6), opts));
			opts = new JodaTimeUtils.ConversionOptions();
		}
		//--Others go here...
	}
}

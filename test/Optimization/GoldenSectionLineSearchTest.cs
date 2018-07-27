
using NUnit.Framework;


namespace Edu.Stanford.Nlp.Optimization
{
	[NUnit.Framework.TestFixture]
	public class GoldenSectionLineSearchTest
	{
		[Test]
		public virtual void TestEasy()
		{
			GoldenSectionLineSearch min = new GoldenSectionLineSearch(false, 0.00001, 0.0, 1.0, false);
			IDoubleUnaryOperator f2 = null;
			// this function used to fail in Galen's version; min should be 0.2
			// return - x * (2 * x - 1) * (x - 0.8);
			// this function fails if you don't find an initial bracketing
			// return - Math.sin(x * Math.PI);
			// return -(3 + 6 * x - 4 * x * x);
			NUnit.Framework.Assert.AreEqual(min.Minimize(f2), 1E-4, 0.15);
		}
	}
}

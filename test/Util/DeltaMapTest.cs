using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	/// <author>Sebastian Riedel</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class DeltaMapTest
	{
		private IDictionary<int, int> originalMap;

		private IDictionary<int, int> originalCopyMap;

		private IDictionary<int, int> deltaCopyMap;

		private IDictionary<int, int> deltaMap;

		private const int Bound3 = 100;

		private const int Bound2 = 90;

		private const int Bound4 = 110;

		private const int Bound1 = 10;

		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			originalMap = new Dictionary<int, int>();
			Random r = new Random();
			for (int i = 0; i < Bound3; i++)
			{
				originalMap[i] = r.NextInt(Bound3);
			}
			originalCopyMap = new Dictionary<int, int>(originalMap);
			deltaCopyMap = new Dictionary<int, int>(originalMap);
			deltaMap = new DeltaMap(originalMap);
			// now make a lot of changes to deltaMap;
			// add and change some stuff
			for (int i_1 = Bound2; i_1 < Bound4; i_1++)
			{
				int rInt = r.NextInt(Bound3);
				//noinspection unchecked
				deltaMap[i_1] = rInt;
				deltaCopyMap[i_1] = rInt;
			}
			// remove some stuff
			for (int i_2 = 0; i_2 < Bound1; i_2++)
			{
				int rInt = r.NextInt(Bound4);
				Sharpen.Collections.Remove(deltaMap, rInt);
				Sharpen.Collections.Remove(deltaCopyMap, rInt);
			}
			// set some stuff to null
			for (int i_3 = 0; i_3 < Bound1; i_3++)
			{
				int rInt = r.NextInt(Bound4);
				deltaMap[rInt] = null;
				deltaCopyMap[rInt] = null;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestOriginalPreserved()
		{
			NUnit.Framework.Assert.AreEqual(originalCopyMap, originalMap);
		}

		[NUnit.Framework.Test]
		public virtual void TestDeltaAccurate()
		{
			NUnit.Framework.Assert.AreEqual(deltaCopyMap, deltaMap);
		}
	}
}

using System.Collections.Generic;



namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Tests for the CoreMaps utilities class.</summary>
	/// <author>dramage</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CoreMapsTest
	{
		/// <summary>Tests the map view and extraction</summary>
		[NUnit.Framework.Test]
		public virtual void TestMaps()
		{
			Random random = new Random();
			IList<ICoreMap> maps = new LinkedList<ICoreMap>();
			for (int i = 0; i < 25; i++)
			{
				ArrayCoreMap m = new ArrayCoreMap();
				m.Set(typeof(CoreMapTest.IntegerA), random.NextInt());
				maps.Add(m);
			}
			IDictionary<ICoreMap, int> view = CoreMaps.AsMap(maps, typeof(CoreMapTest.IntegerA));
			// test getting and setting
			foreach (ICoreMap map in maps)
			{
				NUnit.Framework.Assert.IsTrue(view.Contains(map));
				NUnit.Framework.Assert.AreEqual(view[map], map.Get(typeof(CoreMapTest.IntegerA)));
				int v = random.NextInt();
				map.Set(typeof(CoreMapTest.IntegerA), v);
				NUnit.Framework.Assert.AreEqual(view[map], v);
				NUnit.Framework.Assert.AreEqual(view[map], map.Get(typeof(CoreMapTest.IntegerA)));
			}
			// test iterating and set views
			NUnit.Framework.Assert.AreEqual(new LinkedList<ICoreMap>(view.Keys), maps);
			foreach (KeyValuePair<ICoreMap, int> entry in view)
			{
				NUnit.Framework.Assert.AreEqual(entry.Key.Get(typeof(CoreMapTest.IntegerA)), entry.Value);
				int v = random.NextInt();
				entry.SetValue(v);
				NUnit.Framework.Assert.AreEqual(entry.Value, v);
				NUnit.Framework.Assert.AreEqual(entry.Key.Get(typeof(CoreMapTest.IntegerA)), v);
			}
		}
	}
}

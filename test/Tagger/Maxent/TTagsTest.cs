using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Tagger.Maxent
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TTagsTest
	{
		private TTags tt;

		//import edu.stanford.nlp.tagger.maxent.TTags;
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			tt = new TTags();
		}

		[NUnit.Framework.Test]
		public virtual void TestUniqueness()
		{
			int a = tt.Add("one");
			int b = tt.Add("two");
			NUnit.Framework.Assert.IsTrue(a != b);
		}

		[NUnit.Framework.Test]
		public virtual void TestSameness()
		{
			int a = tt.Add("goat");
			int b = tt.Add("goat");
			NUnit.Framework.Assert.AreEqual(a, b);
		}

		[NUnit.Framework.Test]
		public virtual void TestPreservesString()
		{
			int a = tt.Add("monkey");
			string s = tt.GetTag(a);
			NUnit.Framework.Assert.AreEqual(s, "monkey");
		}

		[NUnit.Framework.Test]
		public virtual void TestPreservesIndex()
		{
			int a = tt.Add("spunky");
			int b = tt.GetIndex("spunky");
			NUnit.Framework.Assert.AreEqual(a, b);
		}

		[NUnit.Framework.Test]
		public virtual void TestCanCount()
		{
			int s = tt.GetSize();
			tt.Add("asdfdsaefasfdsaf");
			int s2 = tt.GetSize();
			NUnit.Framework.Assert.AreEqual(s + 1, s2);
		}

		[NUnit.Framework.Test]
		public virtual void TestHoldsLotsOfStuff()
		{
			try
			{
				for (int i = 0; i < 1000; i++)
				{
					tt.Add("fake" + int.ToString(i));
				}
			}
			catch (Exception e)
			{
				Fail("couldn't put lots of stuff in:" + e.Message);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestClosed()
		{
			tt.Add("java");
			NUnit.Framework.Assert.IsFalse(tt.IsClosed("java"));
			tt.MarkClosed("java");
			NUnit.Framework.Assert.IsTrue(tt.IsClosed("java"));
		}

		[NUnit.Framework.Test]
		public virtual void TestSerialization()
		{
			for (int i = 0; i < 100; i++)
			{
				tt.Add("fake" + int.ToString(i));
			}
			tt.MarkClosed("fake44");
			tt.Add("boat");
			tt.Save("testoutputfile", Generics.NewHashMap<string, ICollection<string>>());
			TTags t2 = new TTags();
			t2.Read("testoutputfile");
			NUnit.Framework.Assert.AreEqual(tt.GetSize(), t2.GetSize());
			NUnit.Framework.Assert.AreEqual(tt.GetIndex("boat"), t2.GetIndex("boat"));
			NUnit.Framework.Assert.AreEqual(t2.GetTag(tt.GetIndex("boat")), "boat");
			NUnit.Framework.Assert.IsFalse(t2.IsClosed("fake43"));
			NUnit.Framework.Assert.IsTrue(t2.IsClosed("fake44"));
			/* java=lame */
			(new File("testoutputfile")).Delete();
		}
	}
}

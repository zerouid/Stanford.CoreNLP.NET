using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;



namespace Edu.Stanford.Nlp.Trees
{
	/// <summary>ConstituentTest.java</summary>
	/// <author>Christopher Manning</author>
	/// <author>Sebastian Pado</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ConstituentTest
	{
		[NUnit.Framework.Test]
		public virtual void TestConstituents()
		{
			ICollection<Constituent> set = new HashSet<Constituent>();
			Constituent c1 = new LabeledScoredConstituent(9, 15, new StringLabel("S"), 0);
			Constituent c2 = new LabeledScoredConstituent(9, 15, new StringLabel("VP"), 0);
			//  System.err.println("c1 "+c1+" c2 "+c2+" equal? "+c1.equals(c2));
			NUnit.Framework.Assert.AreNotSame(c1, c2);
			set.Add(c1);
			//  System.err.println("Set has c1? "+set.contains(c1));
			// System.err.println("Set has c2? "+set.contains(c2));
			NUnit.Framework.Assert.IsTrue(set.Contains(c1));
			NUnit.Framework.Assert.IsFalse(set.Contains(c2));
			set.Add(c2);
			//  System.err.println("Set has c1? "+set.contains(c1));
			//  System.err.println("Set has c2? "+set.contains(c2));
			NUnit.Framework.Assert.IsTrue(set.Contains(c1));
			NUnit.Framework.Assert.IsTrue(set.Contains(c2));
			//   System.err.println("Set size is " + set.size());
			NUnit.Framework.Assert.IsTrue(set.Count == 2);
			foreach (Constituent c in set)
			{
				//   System.err.println(" "+c+" is c1? "+c.equals(c1)+" or "+c1.equals(c)+" is c2? "+c.equals(c2)+" or "+c2.equals(c));
				NUnit.Framework.Assert.IsTrue((c.Equals(c1) || c.Equals(c2)));
			}
		}
		// there used to be a parallel test for Constituents in TreeSets,
		// but given that Constituents do not implement Comparable(),
		// this test just always failed.
	}
}

using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Math;
using Java.Text;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Tests that the CoreMap TypesafeMap works as expected.</summary>
	/// <author>dramage</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class CoreMapTest
	{
		private class StringA : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		private class StringB : ICoreAnnotation<string>
		{
			public virtual Type GetType()
			{
				return typeof(string);
			}
		}

		/// <summary>This class is used in CoreMapsTest, so it can't be private.</summary>
		internal class IntegerA : ICoreAnnotation<int>
		{
			public virtual Type GetType()
			{
				return typeof(int);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestCoreMap()
		{
			ICoreMap @object = new ArrayCoreMap(0);
			NUnit.Framework.Assert.IsFalse(@object.ContainsKey(typeof(CoreMapTest.StringA)));
			@object.Set(typeof(CoreMapTest.StringA), "stem");
			NUnit.Framework.Assert.IsTrue(@object.ContainsKey(typeof(CoreMapTest.StringA)));
			NUnit.Framework.Assert.AreEqual("stem", @object.Get(typeof(CoreMapTest.StringA)));
			@object.Set(typeof(CoreMapTest.StringA), "hi");
			NUnit.Framework.Assert.AreEqual("hi", @object.Get(typeof(CoreMapTest.StringA)));
			NUnit.Framework.Assert.AreEqual(null, @object.Get(typeof(CoreMapTest.IntegerA)));
			@object.Set(typeof(CoreMapTest.IntegerA), 4);
			NUnit.Framework.Assert.AreEqual(int.Parse(4), @object.Get(typeof(CoreMapTest.IntegerA)));
			@object.Set(typeof(CoreMapTest.StringB), "Yes");
			NUnit.Framework.Assert.AreEqual("Wrong # objects", 3, @object.KeySet().Count);
			NUnit.Framework.Assert.AreEqual("Wrong keyset", new HashSet<Type>(Arrays.AsList(typeof(CoreMapTest.StringA), typeof(CoreMapTest.IntegerA), typeof(CoreMapTest.StringB))), @object.KeySet());
			NUnit.Framework.Assert.AreEqual("Wrong remove value", int.Parse(4), @object.Remove(typeof(CoreMapTest.IntegerA)));
			NUnit.Framework.Assert.AreEqual("Wrong # objects", 2, @object.KeySet().Count);
			NUnit.Framework.Assert.AreEqual("Wrong keyset", new HashSet<Type>(Arrays.AsList(typeof(CoreMapTest.StringA), typeof(CoreMapTest.StringB))), @object.KeySet());
			NUnit.Framework.Assert.AreEqual("Wrong value", "hi", @object.Get(typeof(CoreMapTest.StringA)));
			NUnit.Framework.Assert.AreEqual("Wrong value", "Yes", @object.Get(typeof(CoreMapTest.StringB)));
			NUnit.Framework.Assert.AreEqual(null, @object.Set(typeof(CoreMapTest.IntegerA), 7));
			NUnit.Framework.Assert.AreEqual(int.Parse(7), @object.Get(typeof(CoreMapTest.IntegerA)));
			NUnit.Framework.Assert.AreEqual(int.Parse(7), @object.Set(typeof(CoreMapTest.IntegerA), 3));
			NUnit.Framework.Assert.AreEqual(int.Parse(3), @object.Get(typeof(CoreMapTest.IntegerA)));
		}

		[NUnit.Framework.Test]
		public virtual void TestToShorterString()
		{
			ArrayCoreMap a = new ArrayCoreMap();
			a.Set(typeof(CoreAnnotations.TextAnnotation), "Australia");
			a.Set(typeof(CoreAnnotations.NamedEntityTagAnnotation), "LOCATION");
			a.Set(typeof(CoreAnnotations.BeforeAnnotation), "  ");
			a.Set(typeof(CoreAnnotations.PartOfSpeechAnnotation), "NNP");
			a.Set(typeof(CoreAnnotations.ShapeAnnotation), "Xx");
			NUnit.Framework.Assert.AreEqual("Incorrect toShorterString()", "[Text=Australia NamedEntityTag=LOCATION]", a.ToShorterString("Text", "NamedEntityTag"));
			NUnit.Framework.Assert.AreEqual("Incorrect toShorterString()", "[Text=Australia]", a.ToShorterString("Text"));
			NUnit.Framework.Assert.AreEqual("Incorrect toShorterString()", "[Text=Australia NamedEntityTag=LOCATION Before=   PartOfSpeech=NNP Shape=Xx]", a.ToShorterString());
		}

		[NUnit.Framework.Test]
		public virtual void TestEquality()
		{
			ICoreMap a = new ArrayCoreMap();
			ICoreMap b = new ArrayCoreMap();
			NUnit.Framework.Assert.IsTrue(a.Equals(b));
			NUnit.Framework.Assert.IsTrue(a.GetHashCode() == b.GetHashCode());
			a.Set(typeof(CoreMapTest.StringA), "hi");
			NUnit.Framework.Assert.IsFalse(a.Equals(b));
			NUnit.Framework.Assert.IsFalse(a.GetHashCode() == b.GetHashCode());
			b.Set(typeof(CoreMapTest.StringA), "hi");
			NUnit.Framework.Assert.IsTrue(a.Equals(b));
			NUnit.Framework.Assert.IsTrue(a.GetHashCode() == b.GetHashCode());
			a.Remove(typeof(CoreMapTest.StringA));
			NUnit.Framework.Assert.IsFalse(a.Equals(b));
			NUnit.Framework.Assert.IsFalse(a.GetHashCode() == b.GetHashCode());
		}

		/// <summary>
		/// This method is for comparing the speed of the ArrayCoreMap family and
		/// HashMap.
		/// </summary>
		/// <remarks>
		/// This method is for comparing the speed of the ArrayCoreMap family and
		/// HashMap. It tests random access speed for a fixed number of accesses, i,
		/// for both a CoreLabel (can be swapped out for an ArrayCoreMap) and a
		/// HashMap. Switching the order of testing (CoreLabel first or second) shows
		/// that there's a slight advantage to the second loop, especially noticeable
		/// for small i - this is due to some background java funky-ness, so we now
		/// run 50% each way.
		/// </remarks>
		public static void Main(string[] args)
		{
			Type[] allKeys = new Type[] { typeof(CoreAnnotations.TextAnnotation), typeof(CoreAnnotations.LemmaAnnotation), typeof(CoreAnnotations.PartOfSpeechAnnotation), typeof(CoreAnnotations.ShapeAnnotation), typeof(CoreAnnotations.NamedEntityTagAnnotation
				), typeof(CoreAnnotations.DocIDAnnotation), typeof(CoreAnnotations.ValueAnnotation), typeof(CoreAnnotations.CategoryAnnotation), typeof(CoreAnnotations.BeforeAnnotation), typeof(CoreAnnotations.AfterAnnotation), typeof(CoreAnnotations.OriginalTextAnnotation
				), typeof(CoreAnnotations.ArgumentAnnotation), typeof(CoreAnnotations.MarkingAnnotation) };
			// how many iterations
			int numBurnRounds = 10;
			int numGoodRounds = 60;
			int numIterations = 2000000;
			int maxNumKeys = 12;
			double gains = 0.0;
			for (int numKeys = 1; numKeys <= maxNumKeys; numKeys++)
			{
				// the HashMap instance
				Dictionary<string, string> hashmap = new Dictionary<string, string>(numKeys);
				// the CoreMap instance
				ICoreMap coremap = new ArrayCoreMap(numKeys);
				// the set of keys to use
				string[] hashKeys = new string[numKeys];
				Type[] coreKeys = new Type[numKeys];
				for (int key = 0; key < numKeys; key++)
				{
					hashKeys[key] = allKeys[key].GetSimpleName();
					coreKeys[key] = allKeys[key];
				}
				// initialize with default values
				for (int i = 0; i < numKeys; i++)
				{
					coremap.Set(coreKeys[i], i.ToString());
					hashmap[hashKeys[i]] = i.ToString();
				}
				System.Diagnostics.Debug.Assert(coremap.Size() == numKeys);
				System.Diagnostics.Debug.Assert(hashmap.Count == numKeys);
				// for storing results
				double[] hashTimings = new double[numGoodRounds];
				double[] coreTimings = new double[numGoodRounds];
				Random rand = new Random(0);
				bool foundEqual = false;
				for (int round = 0; round < numBurnRounds + numGoodRounds; round++)
				{
					System.Console.Error.Write(".");
					if (round % 2 == 0)
					{
						// test timings on hashmap first
						long hashStart = Runtime.NanoTime();
						int length = hashKeys.Length;
						string last = null;
						for (int i_1 = 0; i_1 < numIterations; i_1++)
						{
							int key_1 = rand.NextInt(length);
							string val = hashmap[hashKeys[key_1]];
							if (val == last)
							{
								foundEqual = true;
							}
							last = val;
						}
						if (round >= numBurnRounds)
						{
							hashTimings[round - numBurnRounds] = (Runtime.NanoTime() - hashStart) / 1000000000.0;
						}
					}
					{
						// test timings on coremap
						long coreStart = Runtime.NanoTime();
						int length = coreKeys.Length;
						string last = null;
						for (int i_1 = 0; i_1 < numIterations; i_1++)
						{
							int key_1 = rand.NextInt(length);
							string val = coremap.Get(coreKeys[key_1]);
							if (val == last)
							{
								foundEqual = true;
							}
							last = val;
						}
						if (round >= numBurnRounds)
						{
							coreTimings[round - numBurnRounds] = (Runtime.NanoTime() - coreStart) / 1000000000.0;
						}
					}
					if (round % 2 == 1)
					{
						// test timings on hashmap second
						long hashStart = Runtime.NanoTime();
						int length = hashKeys.Length;
						string last = null;
						for (int i_1 = 0; i_1 < numIterations; i_1++)
						{
							int key_1 = rand.NextInt(length);
							string val = hashmap[hashKeys[key_1]];
							if (val == last)
							{
								foundEqual = true;
							}
							last = val;
						}
						if (round >= numBurnRounds)
						{
							hashTimings[round - numBurnRounds] = (Runtime.NanoTime() - hashStart) / 1000000000.0;
						}
					}
				}
				if (foundEqual)
				{
					System.Console.Error.Write(" [found equal]");
				}
				System.Console.Error.WriteLine();
				double hashMean = ArrayMath.Mean(hashTimings);
				double coreMean = ArrayMath.Mean(coreTimings);
				double percentDiff = (hashMean - coreMean) / hashMean * 100.0;
				NumberFormat nf = new DecimalFormat("0.00");
				System.Console.Out.WriteLine("HashMap @ " + numKeys + " keys: " + hashMean + " secs/2million gets");
				System.Console.Out.WriteLine("CoreMap @ " + numKeys + " keys: " + coreMean + " secs/2million gets (" + nf.Format(System.Math.Abs(percentDiff)) + "% " + (percentDiff >= 0.0 ? "faster" : "slower") + ")");
				gains += percentDiff;
			}
			System.Console.Out.WriteLine();
			gains = gains / maxNumKeys;
			System.Console.Out.WriteLine("Average: " + System.Math.Abs(gains) + "% " + (gains >= 0.0 ? "faster" : "slower") + ".");
		}
	}
}

using System;
using Java.Lang;
using Java.Util;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	/// <summary>Test of MulticoreWrapper.</summary>
	/// <author>Spence Green</author>
	[NUnit.Framework.TestFixture]
	public class MulticoreWrapperTest
	{
		private MulticoreWrapper<int, int> wrapper;

		private int nThreads;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			// Automagically detect the number of cores
			nThreads = -1;
		}

		[Test]
		public virtual void TestSynchronization()
		{
			wrapper = new MulticoreWrapper<int, int>(nThreads, new MulticoreWrapperTest.DelayedIdentityFunction());
			int lastReturned = 0;
			int nItems = 1000;
			for (int i = 0; i < nItems; ++i)
			{
				wrapper.Put(i);
				while (wrapper.Peek())
				{
					int result = wrapper.Poll();
					System.Console.Error.Printf("Result: %d%n", result);
					NUnit.Framework.Assert.AreEqual(result, lastReturned++);
				}
			}
			wrapper.Join();
			while (wrapper.Peek())
			{
				int result = wrapper.Poll();
				System.Console.Error.Printf("Result2: %d%n", result);
				NUnit.Framework.Assert.AreEqual(result, lastReturned++);
			}
		}

		[Test]
		public virtual void TestUnsynchronized()
		{
			wrapper = new MulticoreWrapper<int, int>(nThreads, new MulticoreWrapperTest.DelayedIdentityFunction(), false);
			int nReturned = 0;
			int nItems = 1000;
			for (int i = 0; i < nItems; ++i)
			{
				wrapper.Put(i);
				while (wrapper.Peek())
				{
					int result = wrapper.Poll();
					System.Console.Error.Printf("Result: %d%n", result);
					nReturned++;
				}
			}
			wrapper.Join();
			while (wrapper.Peek())
			{
				int result = wrapper.Poll();
				System.Console.Error.Printf("Result2: %d%n", result);
				nReturned++;
			}
			NUnit.Framework.Assert.AreEqual(nItems, nReturned);
		}

		/// <summary>Sleeps for some random interval up to 3ms, then returns the input id.</summary>
		/// <author>Spence Green</author>
		private class DelayedIdentityFunction : IThreadsafeProcessor<int, int>
		{
			private readonly Random random = new Random();

			private const int MaxSleepTime = 3;

			// This class is not necessarily threadsafe
			//   http://docs.oracle.com/javase/1.4.2/docs/api/java/util/Random.html
			//   http://download.java.net/jdk7/archive/b123/docs/api/java/util/Random.html
			//
			// In Java 7, you can use ThreadLocalRandom:
			//   http://download.java.net/jdk7/archive/b123/docs/api/java/util/concurrent/ThreadLocalRandom.html
			//
			public virtual int Process(int input)
			{
				int sleepTime = NextSleepTime();
				try
				{
					Thread.Sleep(sleepTime);
				}
				catch (Exception)
				{
				}
				return input;
			}

			private int NextSleepTime()
			{
				lock (this)
				{
					return random.NextInt(MaxSleepTime);
				}
			}

			public virtual IThreadsafeProcessor<int, int> NewInstance()
			{
				return this;
			}
		}
	}
}

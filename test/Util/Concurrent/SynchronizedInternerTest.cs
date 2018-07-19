using System;
using Java.Lang;
using Sharpen;

namespace Edu.Stanford.Nlp.Util.Concurrent
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class SynchronizedInternerTest
	{
		private readonly object[] objects = new object[] { "salamander", "kitten", new string[] { "fred", "george", "sam" }, int.Parse(5), float.ValueOf(5f) };

		private readonly Thread[] threads = new Thread[100];

		[NUnit.Framework.Test]
		public virtual void TestGlobal()
		{
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i] = new Thread(new _IRunnable_18(this));
			}
			foreach (Thread thread in threads)
			{
				thread.Start();
			}
			foreach (Thread thread_1 in threads)
			{
				try
				{
					thread_1.Join();
				}
				catch (Exception e)
				{
					Sharpen.Runtime.PrintStackTrace(e);
				}
			}
		}

		private sealed class _IRunnable_18 : IRunnable
		{
			public _IRunnable_18(SynchronizedInternerTest _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void Run()
			{
				foreach (object @object in this._enclosing.objects)
				{
					object interned = SynchronizedInterner.GlobalIntern(@object);
					Thread.Yield();
					if (interned != @object)
					{
						throw new AssertionError("Interning failed for " + @object);
					}
				}
			}

			private readonly SynchronizedInternerTest _enclosing;
		}
	}
}

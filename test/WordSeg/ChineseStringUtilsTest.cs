using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Sequences;
using Java.Util.Concurrent;
using Sharpen;

namespace Edu.Stanford.Nlp.Wordseg
{
	/// <author>KellenSunderland (public domain contribution)</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ChineseStringUtilsTest
	{
		private static readonly int SegmentAttemptsPerThread = 100;

		private static readonly int Threads = 8;

		/// <summary>
		/// A small test with stubbed data that is meant to expose multithreading initialization errors
		/// in combineSegmentedSentence.
		/// </summary>
		/// <remarks>
		/// A small test with stubbed data that is meant to expose multithreading initialization errors
		/// in combineSegmentedSentence.
		/// In my testing this reliably reproduces the crash seen in the issue:
		/// https://github.com/stanfordnlp/CoreNLP/issues/263
		/// </remarks>
		/// <exception cref="System.Exception">
		/// Various exceptions including Interrupted, all of which should be handled by
		/// failing the test.
		/// </exception>
		[NUnit.Framework.Test]
		public virtual void TestMultithreadedCombineSegmentedSentence()
		{
			SeqClassifierFlags flags = CreateTestFlags();
			IList<CoreLabel> labels = CreateTestTokens();
			IList<IFuture<bool>> tasks = new List<IFuture<bool>>(Threads);
			IExecutorService executor = Executors.NewFixedThreadPool(Threads);
			for (int v = 0; v < Threads; v++)
			{
				IFuture<bool> f = executor.Submit(null);
				tasks.Add(f);
			}
			foreach (IFuture<bool> task in tasks)
			{
				// This assert will fail by throwing a propagated exception, if exceptions due to
				// multithreading issues (generally NPEs) were thrown during the test.
				System.Diagnostics.Debug.Assert((task.Get()));
			}
		}

		// Arbitrary test input.  We just need to segment something on multiple threads to reproduce
		// the issue
		private static IList<CoreLabel> CreateTestTokens()
		{
			CoreLabel token = new CoreLabel();
			token.SetWord("你好，世界");
			token.SetValue("你好，世界");
			token.Set(typeof(CoreAnnotations.ChineseSegAnnotation), "1");
			token.Set(typeof(CoreAnnotations.AnswerAnnotation), "0");
			IList<CoreLabel> labels = new List<CoreLabel>();
			labels.Add(token);
			return labels;
		}

		// Somewhat arbitrary flags.  We're just picking flags that will execute the problematic code
		// path.
		private static SeqClassifierFlags CreateTestFlags()
		{
			SeqClassifierFlags flags = new SeqClassifierFlags();
			flags.sighanPostProcessing = true;
			flags.usePk = true;
			flags.keepEnglishWhitespaces = false;
			flags.keepAllWhitespaces = false;
			return flags;
		}
	}
}

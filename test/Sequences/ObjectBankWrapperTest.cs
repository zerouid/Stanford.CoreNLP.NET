using System;
using System.Collections;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Objectbank;
using Edu.Stanford.Nlp.Util;


using NUnit.Framework;


namespace Edu.Stanford.Nlp.Sequences
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	public class ObjectBankWrapperTest
	{
		[Test]
		public virtual void TestUsingIterator()
		{
			string s = "\n\n@@123\nthis\nis\na\nsentence\n\n@@12\nThis\nis another\n.\n\n";
			string[] output = new string[] { "@@", "123", "this", "is", "a", "sentence", "@@", "12", "This", "is", "another", "." };
			string[] outWSs = new string[] { "@@", "ddd", "xxxx", "xx", "x", "xxxxx", "@@", "dd", "Xxxx", "xx", "xxxxx", "." };
			NUnit.Framework.Assert.AreEqual(output.Length, outWSs.Length, "Two output arrays should have same length");
			Properties props = PropertiesUtils.AsProperties("wordShape", "chris2");
			SeqClassifierFlags flags = new SeqClassifierFlags(props);
			PlainTextDocumentReaderAndWriter<CoreLabel> readerAndWriter = new PlainTextDocumentReaderAndWriter<CoreLabel>();
			readerAndWriter.Init(flags);
			ReaderIteratorFactory rif = new ReaderIteratorFactory(new StringReader(s));
			ObjectBank<IList<CoreLabel>> di = new ObjectBank<IList<CoreLabel>>(rif, readerAndWriter);
			ICollection<string> knownLCWords = new HashSet<string>();
			ObjectBankWrapper<CoreLabel> obw = new ObjectBankWrapper<CoreLabel>(flags, di, knownLCWords);
			try
			{
				int outIdx = 0;
				for (IEnumerator<IList<CoreLabel>> iter = obw.GetEnumerator(); iter.MoveNext(); )
				{
					IList<CoreLabel> sent = iter.Current;
					for (IEnumerator<CoreLabel> iter2 = sent.GetEnumerator(); iter2.MoveNext(); )
					{
						CoreLabel cl = iter2.Current;
						string tok = cl.Word();
						string shape = cl.Get(typeof(CoreAnnotations.ShapeAnnotation));
						NUnit.Framework.Assert.AreEqual(output[outIdx], tok);
						NUnit.Framework.Assert.AreEqual(outWSs[outIdx], shape);
						outIdx++;
					}
				}
				if (outIdx < output.Length)
				{
					NUnit.Framework.Assert.Fail("Too few things in iterator, lacking: " + output[outIdx]);
				}
			}
			catch (Exception e)
			{
				NUnit.Framework.Assert.Fail("Probably too many things in iterator: " + e);
			}
		}

		[Test]
		public virtual void TestUsingEnhancedFor()
		{
			string s = "\n\n@@123\nthis\nis\na\nsentence\n\n@@12\nThis\nis another\n.\n\n";
			string[] output = new string[] { "@@", "123", "this", "is", "a", "sentence", "@@", "12", "This", "is", "another", "." };
			string[] outWSs = new string[] { "@@", "ddd", "xxxx", "xx", "x", "xxxxx", "@@", "dd", "Xxxx", "xx", "xxxxx", "." };
			NUnit.Framework.Assert.AreEqual(output.Length, outWSs.Length, "Two output arrays should have same length");
			Properties props = PropertiesUtils.AsProperties("wordShape", "chris2");
			SeqClassifierFlags flags = new SeqClassifierFlags(props);
			PlainTextDocumentReaderAndWriter<CoreLabel> readerAndWriter = new PlainTextDocumentReaderAndWriter<CoreLabel>();
			readerAndWriter.Init(flags);
			ReaderIteratorFactory rif = new ReaderIteratorFactory(new StringReader(s));
			ObjectBank<IList<CoreLabel>> di = new ObjectBank<IList<CoreLabel>>(rif, readerAndWriter);
			ICollection<string> knownLCWords = new HashSet<string>();
			ObjectBankWrapper<CoreLabel> obw = new ObjectBankWrapper<CoreLabel>(flags, di, knownLCWords);
			try
			{
				int outIdx = 0;
				foreach (IList<CoreLabel> sent in obw)
				{
					foreach (CoreLabel cl in sent)
					{
						string tok = cl.Word();
						string shape = cl.Get(typeof(CoreAnnotations.ShapeAnnotation));
						NUnit.Framework.Assert.AreEqual(output[outIdx], tok);
						NUnit.Framework.Assert.AreEqual(outWSs[outIdx], shape);
						outIdx++;
					}
				}
				if (outIdx < output.Length)
				{
					NUnit.Framework.Assert.Fail("Too few things in iterator, lacking: " + output[outIdx]);
				}
			}
			catch (Exception e)
			{
				NUnit.Framework.Assert.Fail("Probably too many things in iterator." + e);
			}
		}

		[Test]
		public virtual void TestUsingToArray()
		{
			string s = "\n\n@@123\nthis\nis\na\nsentence\n\n@@12\nThis\nis another\n.\n\n";
			string[] output = new string[] { "@@", "123", "this", "is", "a", "sentence", "@@", "12", "This", "is", "another", "." };
			string[] outWSs = new string[] { "@@", "ddd", "xxxx", "xx", "x", "xxxxx", "@@", "dd", "Xxxx", "xx", "xxxxx", "." };
			NUnit.Framework.Assert.AreEqual(output.Length, outWSs.Length, "Two output arrays should have same length");
			Properties props = PropertiesUtils.AsProperties("wordShape", "chris2");
			SeqClassifierFlags flags = new SeqClassifierFlags(props);
			PlainTextDocumentReaderAndWriter<CoreLabel> readerAndWriter = new PlainTextDocumentReaderAndWriter<CoreLabel>();
			readerAndWriter.Init(flags);
			ReaderIteratorFactory rif = new ReaderIteratorFactory(new StringReader(s));
			ObjectBank<IList<CoreLabel>> di = new ObjectBank<IList<CoreLabel>>(rif, readerAndWriter);
			ICollection<string> knownLCWords = new HashSet<string>();
			ObjectBankWrapper<CoreLabel> obw = new ObjectBankWrapper<CoreLabel>(flags, di, knownLCWords);
			try
			{
				IList<CoreLabel>[] sents = Sharpen.Collections.ToArray(obw, new IList[0]);
				int outIdx = 0;
				foreach (IList<CoreLabel> sent in sents)
				{
					foreach (CoreLabel cl in sent)
					{
						string tok = cl.Word();
						string shape = cl.Get(typeof(CoreAnnotations.ShapeAnnotation));
						NUnit.Framework.Assert.AreEqual(output[outIdx], tok);
						NUnit.Framework.Assert.AreEqual(outWSs[outIdx], shape);
						outIdx++;
					}
				}
				if (outIdx < output.Length)
				{
					NUnit.Framework.Assert.Fail("Too few things in iterator, lacking: " + output[outIdx]);
				}
			}
			catch (Exception e)
			{
				NUnit.Framework.Assert.Fail("Probably too many things in iterator: " + e);
			}
		}
	}
}

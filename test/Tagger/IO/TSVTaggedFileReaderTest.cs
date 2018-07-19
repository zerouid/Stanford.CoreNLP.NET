using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;
using Java.IO;
using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Tagger.IO
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class TSVTaggedFileReaderTest
	{
		internal const string TestFile = "A\t1\nB\t2\nC\t3\n\nD\t4\nE\t5\n\n\n\nF\t6\n\n\n";

		/// <exception cref="System.IO.IOException"/>
		internal virtual File CreateFile(string data)
		{
			File file = File.CreateTempFile("TSVTaggedFileReaderTest", "txt");
			FileWriter fout = new FileWriter(file);
			fout.Write(data);
			fout.Close();
			return file;
		}

		/// <exception cref="System.IO.IOException"/>
		internal virtual File CreateTestFile()
		{
			return CreateFile(TestFile);
		}

		/// <exception cref="System.IO.IOException"/>
		internal virtual File CreateBrokenFile()
		{
			// no tags
			return CreateFile("A\nB\n\n");
		}

		internal virtual TaggedFileRecord CreateRecord(File file, string extraArgs)
		{
			string description = extraArgs + "format=TSV," + file;
			Properties props = new Properties();
			return TaggedFileRecord.CreateRecord(props, description);
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestReadNormal()
		{
			File file = CreateTestFile();
			TaggedFileRecord record = CreateRecord(file, string.Empty);
			IList<IList<TaggedWord>> sentences = new List<IList<TaggedWord>>();
			foreach (IList<TaggedWord> sentence in record.Reader())
			{
				sentences.Add(sentence);
			}
			NUnit.Framework.Assert.AreEqual(3, sentences.Count);
			NUnit.Framework.Assert.AreEqual(3, sentences[0].Count);
			NUnit.Framework.Assert.AreEqual("A", sentences[0][0].Word());
			NUnit.Framework.Assert.AreEqual("B", sentences[0][1].Word());
			NUnit.Framework.Assert.AreEqual("C", sentences[0][2].Word());
			NUnit.Framework.Assert.AreEqual("D", sentences[1][0].Word());
			NUnit.Framework.Assert.AreEqual("E", sentences[1][1].Word());
			NUnit.Framework.Assert.AreEqual("F", sentences[2][0].Word());
			NUnit.Framework.Assert.AreEqual("1", sentences[0][0].Tag());
			NUnit.Framework.Assert.AreEqual("2", sentences[0][1].Tag());
			NUnit.Framework.Assert.AreEqual("3", sentences[0][2].Tag());
			NUnit.Framework.Assert.AreEqual("4", sentences[1][0].Tag());
			NUnit.Framework.Assert.AreEqual("5", sentences[1][1].Tag());
			NUnit.Framework.Assert.AreEqual("6", sentences[2][0].Tag());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestReadBackwards()
		{
			File file = CreateTestFile();
			TaggedFileRecord record = CreateRecord(file, "tagColumn=0,wordColumn=1,");
			IList<IList<TaggedWord>> sentences = new List<IList<TaggedWord>>();
			foreach (IList<TaggedWord> sentence in record.Reader())
			{
				sentences.Add(sentence);
			}
			NUnit.Framework.Assert.AreEqual(3, sentences.Count);
			NUnit.Framework.Assert.AreEqual(3, sentences[0].Count);
			NUnit.Framework.Assert.AreEqual("A", sentences[0][0].Tag());
			NUnit.Framework.Assert.AreEqual("B", sentences[0][1].Tag());
			NUnit.Framework.Assert.AreEqual("C", sentences[0][2].Tag());
			NUnit.Framework.Assert.AreEqual("D", sentences[1][0].Tag());
			NUnit.Framework.Assert.AreEqual("E", sentences[1][1].Tag());
			NUnit.Framework.Assert.AreEqual("F", sentences[2][0].Tag());
			NUnit.Framework.Assert.AreEqual("1", sentences[0][0].Word());
			NUnit.Framework.Assert.AreEqual("2", sentences[0][1].Word());
			NUnit.Framework.Assert.AreEqual("3", sentences[0][2].Word());
			NUnit.Framework.Assert.AreEqual("4", sentences[1][0].Word());
			NUnit.Framework.Assert.AreEqual("5", sentences[1][1].Word());
			NUnit.Framework.Assert.AreEqual("6", sentences[2][0].Word());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestError()
		{
			File file = CreateBrokenFile();
			TaggedFileRecord record = CreateRecord(file, "tagColumn=0,wordColumn=1,");
			try
			{
				foreach (IList<TaggedWord> sentence in record.Reader())
				{
					throw new AssertionError("Should have thrown an error " + " reading a file with no tags");
				}
			}
			catch (ArgumentException)
			{
			}
		}
		// yay
	}
}

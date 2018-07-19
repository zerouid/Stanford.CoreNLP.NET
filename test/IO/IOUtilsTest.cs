using System.Collections.Generic;
using Edu.Stanford.Nlp.Util;
using Java.IO;
using Java.Util;
using Java.Util.Regex;
using Java.Util.Zip;
using NUnit.Framework;
using Sharpen;

namespace Edu.Stanford.Nlp.IO
{
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class IOUtilsTest
	{
		private string dirPath;

		private File dir;

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.SetUp]
		protected virtual void SetUp()
		{
			dir = File.CreateTempFile("IOUtilsTest", ".dir");
			NUnit.Framework.Assert.IsTrue(dir.Delete());
			NUnit.Framework.Assert.IsTrue(dir.Mkdir());
			dirPath = dir.GetAbsolutePath();
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.TearDown]
		protected virtual void TearDown()
		{
			this.Delete(this.dir);
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		[NUnit.Framework.Test]
		public virtual void TestReadWriteStreamFromString()
		{
			ObjectOutputStream oos = IOUtils.WriteStreamFromString(dirPath + "/objs.obj");
			oos.WriteObject(int.Parse(42));
			oos.WriteObject("forty two");
			oos.Close();
			ObjectInputStream ois = IOUtils.ReadStreamFromString(dirPath + "/objs.obj");
			object i = ois.ReadObject();
			object s = ois.ReadObject();
			NUnit.Framework.Assert.IsTrue(int.Parse(42).Equals(i));
			NUnit.Framework.Assert.IsTrue("forty two".Equals(s));
			ois.Close();
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void TestReadLines()
		{
			File file = new File(this.dir, "lines.txt");
			IEnumerable<string> iterable;
			Write("abc", file);
			iterable = IOUtils.ReadLines(file);
			NUnit.Framework.Assert.AreEqual("abc", StringUtils.Join(iterable, "!"));
			NUnit.Framework.Assert.AreEqual("abc", StringUtils.Join(iterable, "!"));
			Write("abc\ndef\n", file);
			iterable = IOUtils.ReadLines(file);
			NUnit.Framework.Assert.AreEqual("abc!def", StringUtils.Join(iterable, "!"));
			NUnit.Framework.Assert.AreEqual("abc!def", StringUtils.Join(iterable, "!"));
			Write("\na\nb\n", file);
			iterable = IOUtils.ReadLines(file.GetPath());
			NUnit.Framework.Assert.AreEqual("!a!b", StringUtils.Join(iterable, "!"));
			NUnit.Framework.Assert.AreEqual("!a!b", StringUtils.Join(iterable, "!"));
			Write(string.Empty, file);
			iterable = IOUtils.ReadLines(file);
			NUnit.Framework.Assert.IsFalse(iterable.GetEnumerator().MoveNext());
			Write("\n", file);
			iterable = IOUtils.ReadLines(file.GetPath());
			IEnumerator<string> iterator = iterable.GetEnumerator();
			NUnit.Framework.Assert.IsTrue(iterator.MoveNext());
			iterator.Current;
			BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(new GZIPOutputStream(new FileOutputStream(file))));
			writer.Write("\nzipped\ntext\n");
			writer.Close();
			iterable = IOUtils.ReadLines(file, typeof(GZIPInputStream));
			NUnit.Framework.Assert.AreEqual("!zipped!text", StringUtils.Join(iterable, "!"));
			NUnit.Framework.Assert.AreEqual("!zipped!text", StringUtils.Join(iterable, "!"));
		}

		/// <exception cref="System.IO.IOException"/>
		private static void CheckLineIterable(bool includeEol)
		{
			string[] expected = new string[] { "abcdefhij\r\n", "klnm\r\n", "opqrst\n", "uvwxyz\r", "I am a longer line than the rest\n", "12345" };
			string testString = StringUtils.Join(expected, string.Empty);
			Reader reader = new StringReader(testString);
			int i = 0;
			IEnumerable<string> iterable = IOUtils.GetLineIterable(reader, 10, includeEol);
			foreach (string line in iterable)
			{
				string expLine = expected[i];
				if (!includeEol)
				{
					expLine = expLine.ReplaceAll("\\r|\\n", string.Empty);
				}
				NUnit.Framework.Assert.AreEqual("Checking line " + i, expLine, line);
				i++;
			}
			NUnit.Framework.Assert.AreEqual("Check got all lines", expected.Length, i);
			IOUtils.CloseIgnoringExceptions(reader);
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestLineIterableWithEol()
		{
			CheckLineIterable(true);
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestLineIterableWithoutEol()
		{
			CheckLineIterable(false);
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestIterFilesRecursive()
		{
			File dir = new File(this.dir, "recursive");
			File a = new File(dir, "x/a");
			File b = new File(dir, "x/y/b.txt");
			File c = new File(dir, "c.txt");
			File d = new File(dir, "dtxt");
			Write("A", a);
			Write("B", b);
			Write("C", c);
			Write("D", d);
			ICollection<File> actual = ToSet(IOUtils.IterFilesRecursive(dir));
			NUnit.Framework.Assert.AreEqual(ToSet(Arrays.AsList(a, b, c, d)), actual);
			actual = ToSet(IOUtils.IterFilesRecursive(dir, ".txt"));
			NUnit.Framework.Assert.AreEqual(ToSet(Arrays.AsList(b, c)), actual);
			actual = ToSet(IOUtils.IterFilesRecursive(dir, Pattern.Compile(".txt")));
			NUnit.Framework.Assert.AreEqual(ToSet(Arrays.AsList(b, c, d)), actual);
		}

		protected internal virtual void Delete(File file)
		{
			if (file.IsDirectory())
			{
				File[] children = file.ListFiles();
				if (children != null)
				{
					foreach (File child in children)
					{
						this.Delete(child);
					}
				}
			}
			// Use an Assert here to make sure that all files were closed properly
			NUnit.Framework.Assert.IsTrue(file.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		protected internal static void Write(string text, File file)
		{
			if (!file.GetParentFile().Exists())
			{
				//noinspection ResultOfMethodCallIgnored
				file.GetParentFile().Mkdirs();
			}
			FileWriter writer = new FileWriter(file);
			writer.Write(text);
			writer.Close();
		}

		private static ICollection<E> ToSet<E>(IEnumerable<E> iter)
		{
			ICollection<E> set = new HashSet<E>();
			foreach (E item in iter)
			{
				set.Add(item);
			}
			return set;
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCpSourceFileTargetNotExists()
		{
			File source = File.CreateTempFile("foo", ".file");
			IOUtils.WriteStringToFile("foo", source.GetPath(), "utf-8");
			File dst = File.CreateTempFile("foo", ".file");
			NUnit.Framework.Assert.IsTrue(dst.Delete());
			IOUtils.Cp(source, dst);
			NUnit.Framework.Assert.AreEqual("foo", IOUtils.SlurpFile(dst));
			NUnit.Framework.Assert.IsTrue(source.Delete());
			NUnit.Framework.Assert.IsTrue(dst.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCpSourceFileTargetExists()
		{
			File source = File.CreateTempFile("foo", ".file");
			IOUtils.WriteStringToFile("foo", source.GetPath(), "utf-8");
			File dst = File.CreateTempFile("foo", ".file");
			IOUtils.Cp(source, dst);
			NUnit.Framework.Assert.AreEqual("foo", IOUtils.SlurpFile(dst));
			NUnit.Framework.Assert.IsTrue(source.Delete());
			NUnit.Framework.Assert.IsTrue(dst.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCpSourceFileTargetIsDir()
		{
			File source = File.CreateTempFile("foo", ".file");
			IOUtils.WriteStringToFile("foo", source.GetPath(), "utf-8");
			File dst = File.CreateTempFile("foo", ".file");
			NUnit.Framework.Assert.IsTrue(dst.Delete());
			NUnit.Framework.Assert.IsTrue(dst.Mkdir());
			IOUtils.Cp(source, dst);
			NUnit.Framework.Assert.AreEqual("foo", IOUtils.SlurpFile(dst.GetPath() + File.separator + source.GetName()));
			NUnit.Framework.Assert.IsTrue(source.Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst.GetPath() + File.separator + source.GetName()).Delete());
			NUnit.Framework.Assert.IsTrue(dst.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCpSourceDirTargetNotExists()
		{
			// create source
			File sourceDir = File.CreateTempFile("foo", ".file");
			NUnit.Framework.Assert.IsTrue(sourceDir.Delete());
			NUnit.Framework.Assert.IsTrue(sourceDir.Mkdir());
			File foo = new File(sourceDir + File.separator + "foo");
			IOUtils.WriteStringToFile("foo", foo.GetPath(), "utf-8");
			// create destination
			File dst = File.CreateTempFile("foo", ".file");
			NUnit.Framework.Assert.IsTrue(dst.Delete());
			// copy
			IOUtils.Cp(sourceDir, dst, true);
			NUnit.Framework.Assert.AreEqual("foo", IOUtils.SlurpFile(dst.GetPath() + File.separator + "foo"));
			// clean up
			NUnit.Framework.Assert.IsTrue(foo.Delete());
			NUnit.Framework.Assert.IsTrue(sourceDir.Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst.GetPath() + File.separator + "foo").Delete());
			NUnit.Framework.Assert.IsTrue(dst.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCpSourceDirTargetIsDir()
		{
			// create source
			File sourceDir = File.CreateTempFile("foo", ".file");
			NUnit.Framework.Assert.IsTrue(sourceDir.Delete());
			NUnit.Framework.Assert.IsTrue(sourceDir.Mkdir());
			File foo = new File(sourceDir + File.separator + "foo");
			IOUtils.WriteStringToFile("foo", foo.GetPath(), "utf-8");
			// create destination
			File dst = File.CreateTempFile("foo", ".file");
			NUnit.Framework.Assert.IsTrue(dst.Delete());
			NUnit.Framework.Assert.IsTrue(dst.Mkdir());
			// copy
			IOUtils.Cp(sourceDir, dst, true);
			NUnit.Framework.Assert.AreEqual("foo", IOUtils.SlurpFile(dst.GetPath() + File.separator + sourceDir.GetName() + File.separator + "foo"));
			// clean up
			NUnit.Framework.Assert.IsTrue(foo.Delete());
			NUnit.Framework.Assert.IsTrue(sourceDir.Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst.GetPath() + File.separator + sourceDir.GetName() + File.separator + "foo").Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst.GetPath() + File.separator + sourceDir.GetName()).Delete());
			NUnit.Framework.Assert.IsTrue(dst.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestCpRecursive()
		{
			// create source
			// d1/
			//   d2/
			//     foo
			//   bar
			File sourceDir = File.CreateTempFile("directory", ".file");
			NUnit.Framework.Assert.IsTrue(sourceDir.Delete());
			NUnit.Framework.Assert.IsTrue(sourceDir.Mkdir());
			File sourceSubDir = new File(sourceDir + File.separator + "d2");
			NUnit.Framework.Assert.IsTrue(sourceSubDir.Mkdir());
			File foo = new File(sourceSubDir + File.separator + "foo");
			IOUtils.WriteStringToFile("foo", foo.GetPath(), "utf-8");
			File bar = new File(sourceDir + File.separator + "bar");
			IOUtils.WriteStringToFile("bar", bar.GetPath(), "utf-8");
			// create destination
			File dst = File.CreateTempFile("dst", ".file");
			NUnit.Framework.Assert.IsTrue(dst.Delete());
			// copy
			IOUtils.Cp(sourceDir, dst, true);
			NUnit.Framework.Assert.AreEqual("foo", IOUtils.SlurpFile(dst + File.separator + "d2" + File.separator + "foo"));
			NUnit.Framework.Assert.AreEqual("bar", IOUtils.SlurpFile(dst + File.separator + "bar"));
			// clean up
			NUnit.Framework.Assert.IsTrue(foo.Delete());
			NUnit.Framework.Assert.IsTrue(bar.Delete());
			NUnit.Framework.Assert.IsTrue(sourceSubDir.Delete());
			NUnit.Framework.Assert.IsTrue(sourceDir.Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst + File.separator + "d2" + File.separator + "foo").Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst + File.separator + "d2").Delete());
			NUnit.Framework.Assert.IsTrue(new File(dst + File.separator + "bar").Delete());
			NUnit.Framework.Assert.IsTrue(dst.Delete());
		}

		/// <exception cref="System.IO.IOException"/>
		[NUnit.Framework.Test]
		public virtual void TestTail()
		{
			File f = File.CreateTempFile("totail", ".file");
			// Easy case
			IOUtils.WriteStringToFile("line 1\nline 2\nline 3\nline 4\nline 5\nline 6\nline 7", f.GetPath(), "utf-8");
			NUnit.Framework.Assert.AreEqual("line 7", IOUtils.Tail(f, 1)[0]);
			NUnit.Framework.Assert.AreEqual("line 6", IOUtils.Tail(f, 2)[0]);
			NUnit.Framework.Assert.AreEqual("line 7", IOUtils.Tail(f, 2)[1]);
			// Hard case
			IOUtils.WriteStringToFile("line 1\nline 2\n\nline 3\n", f.GetPath(), "utf-8");
			NUnit.Framework.Assert.AreEqual(string.Empty, IOUtils.Tail(f, 1)[0]);
			NUnit.Framework.Assert.AreEqual(string.Empty, IOUtils.Tail(f, 3)[0]);
			NUnit.Framework.Assert.AreEqual("line 3", IOUtils.Tail(f, 3)[1]);
			NUnit.Framework.Assert.AreEqual(string.Empty, IOUtils.Tail(f, 3)[2]);
			// Too few lines
			IOUtils.WriteStringToFile("line 1\nline 2", f.GetPath(), "utf-8");
			NUnit.Framework.Assert.AreEqual(0, IOUtils.Tail(f, 0).Length);
			NUnit.Framework.Assert.AreEqual(1, IOUtils.Tail(f, 1).Length);
			NUnit.Framework.Assert.AreEqual(2, IOUtils.Tail(f, 3).Length);
			NUnit.Framework.Assert.AreEqual(2, IOUtils.Tail(f, 2).Length);
			// UTF-reading
			IOUtils.WriteStringToFile("↹↝\n۝æ", f.GetPath(), "utf-8");
			NUnit.Framework.Assert.AreEqual("↹↝", IOUtils.Tail(f, 2)[0]);
			NUnit.Framework.Assert.AreEqual("۝æ", IOUtils.Tail(f, 2)[1]);
			// Clean up
			NUnit.Framework.Assert.IsTrue(f.Delete());
		}
	}
}

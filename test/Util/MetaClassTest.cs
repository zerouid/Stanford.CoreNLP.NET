using System.Collections;
using System.Collections.Generic;



using NUnit.Framework;


namespace Edu.Stanford.Nlp.Util
{
	[NUnit.Framework.TestFixture]
	public class MetaClassTest
	{
		private static readonly string Class = typeof(MetaClassTest).FullName;

		public interface IISomething
		{
			//Interface
			string Display();
		}

		public class IInstSomething : MetaClassTest.IISomething
		{
			public virtual string Display()
			{
				return "Isomething";
			}
		}

		public abstract class ASomething
		{
			//Abstract
			public abstract string Display();
		}

		public class AInstSomething : MetaClassTest.ASomething
		{
			public override string Display()
			{
				return "Asomething";
			}
		}

		public class SSomething
		{
			//Superclass
			public virtual string Display()
			{
				return "FAIL";
			}
		}

		public class SInstSomething : MetaClassTest.SSomething
		{
			public override string Display()
			{
				return "Ssomething";
			}
		}

		public class Something
		{
			//Simpleclass
			public virtual string Display()
			{
				return "something";
			}
		}

		public class SomethingWrapper
		{
			private MetaClassTest.IISomething isomething;

			private MetaClassTest.ASomething asomething;

			private MetaClassTest.SSomething ssomething;

			private MetaClassTest.Something something;

			public SomethingWrapper(MetaClassTest.IISomething something)
			{
				this.isomething = something;
			}

			public SomethingWrapper(MetaClassTest.ASomething something)
			{
				this.asomething = something;
			}

			public SomethingWrapper(MetaClassTest.SSomething something)
			{
				this.ssomething = something;
			}

			public SomethingWrapper(MetaClassTest.Something something)
			{
				this.something = something;
			}

			public virtual string Display()
			{
				return something.Display();
			}

			public virtual string DisplayI()
			{
				return isomething.Display();
			}

			public virtual string DisplayA()
			{
				return asomething.Display();
			}

			public virtual string DisplayS()
			{
				return ssomething.Display();
			}
		}

		public class SubSSomething : MetaClassTest.SSomething
		{
			public override string Display()
			{
				return "subssomething";
			}
		}

		public class ManyConstructors
		{
			private int constructorInvoked = -1;

			public ManyConstructors(object a)
			{
				constructorInvoked = 0;
			}

			public ManyConstructors(MetaClassTest.Something a)
			{
				constructorInvoked = 1;
			}

			public ManyConstructors(MetaClassTest.SSomething a)
			{
				constructorInvoked = 2;
			}

			public ManyConstructors(MetaClassTest.SubSSomething a)
			{
				constructorInvoked = 3;
			}

			public ManyConstructors(object a, object b)
			{
				this.constructorInvoked = 4;
			}

			public ManyConstructors(object a, MetaClassTest.Something b)
			{
				this.constructorInvoked = 5;
			}

			public ManyConstructors(object a, MetaClassTest.SSomething b)
			{
				this.constructorInvoked = 6;
			}

			public ManyConstructors(object a, MetaClassTest.SubSSomething b)
			{
				this.constructorInvoked = 7;
			}

			public ManyConstructors(MetaClassTest.Something a, object b)
			{
				this.constructorInvoked = 8;
			}

			public ManyConstructors(MetaClassTest.Something a, MetaClassTest.Something b)
			{
				this.constructorInvoked = 9;
			}

			public ManyConstructors(MetaClassTest.Something a, MetaClassTest.SSomething b)
			{
				this.constructorInvoked = 10;
			}

			public ManyConstructors(MetaClassTest.Something a, MetaClassTest.SubSSomething b)
			{
				this.constructorInvoked = 11;
			}

			public ManyConstructors(MetaClassTest.SSomething a, object b)
			{
				this.constructorInvoked = 12;
			}

			public ManyConstructors(MetaClassTest.SSomething a, MetaClassTest.Something b)
			{
				this.constructorInvoked = 13;
			}

			public ManyConstructors(MetaClassTest.SSomething a, MetaClassTest.SSomething b)
			{
				this.constructorInvoked = 14;
			}

			public ManyConstructors(MetaClassTest.SSomething a, MetaClassTest.SubSSomething b)
			{
				this.constructorInvoked = 15;
			}

			public ManyConstructors(MetaClassTest.SubSSomething a, object b)
			{
				this.constructorInvoked = 16;
			}

			public ManyConstructors(MetaClassTest.SubSSomething a, MetaClassTest.Something b)
			{
				this.constructorInvoked = 17;
			}

			public ManyConstructors(MetaClassTest.SubSSomething a, MetaClassTest.SSomething b)
			{
				this.constructorInvoked = 18;
			}

			public ManyConstructors(MetaClassTest.SubSSomething a, MetaClassTest.SubSSomething b)
			{
				this.constructorInvoked = 19;
			}

			public ManyConstructors(MetaClassTest.Something a, MetaClassTest.Something b, MetaClassTest.Something c)
			{
				this.constructorInvoked = 20;
			}

			public virtual int ConstructorInvoked()
			{
				return constructorInvoked;
			}

			public override bool Equals(object o)
			{
				if (o is MetaClassTest.ManyConstructors)
				{
					return this.constructorInvoked == ((MetaClassTest.ManyConstructors)o).constructorInvoked;
				}
				return false;
			}

			public override string ToString()
			{
				return string.Empty + constructorInvoked;
			}
		}

		public class Primitive
		{
			public Primitive(int i)
			{
			}

			public Primitive(double d)
			{
			}
		}

		public class VarArgs
		{
			public int[] a;

			public VarArgs(params int[] args)
			{
				a = args;
			}
		}

		[Test]
		public virtual void TestBasic()
		{
			//--Basics
			//(succeed)
			MetaClass.Create("java.lang.String");
			NUnit.Framework.Assert.AreEqual(MetaClass.Create("java.lang.String").CreateInstance("hello"), "hello");
			//(fail)
			try
			{
				MetaClass.Create(Class + "$SomethingWrapper").CreateInstance("hello");
			}
			catch (MetaClass.ClassCreationException)
			{
				NUnit.Framework.Assert.IsTrue(true, "Should not instantiate Super with String");
			}
			//--Argument Length
			MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.Something(), new MetaClassTest.Something(), new MetaClassTest.Something());
			NUnit.Framework.Assert.AreEqual(((MetaClassTest.ManyConstructors)MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.Something())).ConstructorInvoked(), new MetaClassTest.ManyConstructors(new MetaClassTest.Something
				()).ConstructorInvoked());
			NUnit.Framework.Assert.AreEqual(((MetaClassTest.ManyConstructors)MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.Something(), new MetaClassTest.Something())).ConstructorInvoked(), new MetaClassTest.ManyConstructors
				(new MetaClassTest.Something(), new MetaClassTest.Something()).ConstructorInvoked());
			NUnit.Framework.Assert.AreEqual(((MetaClassTest.ManyConstructors)MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.Something(), new MetaClassTest.Something(), new MetaClassTest.Something())).ConstructorInvoked(), 
				new MetaClassTest.ManyConstructors(new MetaClassTest.Something(), new MetaClassTest.Something(), new MetaClassTest.Something()).ConstructorInvoked());
			NUnit.Framework.Assert.AreEqual(new MetaClassTest.ManyConstructors(new string("hi")), MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new string("hi")));
			NUnit.Framework.Assert.AreEqual(new MetaClassTest.ManyConstructors(new MetaClassTest.Something()), MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.Something()));
			NUnit.Framework.Assert.AreEqual(new MetaClassTest.ManyConstructors(new MetaClassTest.SSomething()), MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.SSomething()));
			NUnit.Framework.Assert.AreEqual(new MetaClassTest.ManyConstructors(new MetaClassTest.SubSSomething()), MetaClass.Create(Class + "$ManyConstructors").CreateInstance(new MetaClassTest.SubSSomething()));
		}

		[Test]
		public virtual void TestInheritance()
		{
			//--Implementing Class
			try
			{
				object o = MetaClass.Create(Class + "$SomethingWrapper").CreateInstance(new MetaClassTest.Something());
				NUnit.Framework.Assert.IsTrue(o is MetaClassTest.SomethingWrapper, "Returned class should be a SomethingWrapper");
				NUnit.Framework.Assert.AreEqual(((MetaClassTest.SomethingWrapper)o).Display(), "something");
			}
			catch (MetaClass.ClassCreationException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				NUnit.Framework.Assert.IsFalse(true, "Should not exception on this call");
			}
			//--Implementing super class
			try
			{
				object o = MetaClass.Create(Class + "$SomethingWrapper").CreateInstance(new MetaClassTest.SInstSomething());
				NUnit.Framework.Assert.IsTrue(o is MetaClassTest.SomethingWrapper, "Returned class should be a SomethingWrapper");
				NUnit.Framework.Assert.AreEqual(((MetaClassTest.SomethingWrapper)o).DisplayS(), "Ssomething");
			}
			catch (MetaClass.ClassCreationException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				NUnit.Framework.Assert.IsFalse(true, "Should not exception on this call");
			}
			//--Implementing abstract classes
			try
			{
				object o = MetaClass.Create(Class + "$SomethingWrapper").CreateInstance(new MetaClassTest.AInstSomething());
				NUnit.Framework.Assert.IsTrue(o is MetaClassTest.SomethingWrapper, "Returned class should be a SomethingWrapper");
				NUnit.Framework.Assert.AreEqual(((MetaClassTest.SomethingWrapper)o).DisplayA(), "Asomething");
			}
			catch (MetaClass.ClassCreationException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				NUnit.Framework.Assert.IsFalse(true, "Should not exception on this call");
			}
			//--Implementing interfaces
			try
			{
				object o = MetaClass.Create(Class + "$SomethingWrapper").CreateInstance(new MetaClassTest.IInstSomething());
				NUnit.Framework.Assert.IsTrue(o is MetaClassTest.SomethingWrapper, "Returned class should be a SomethingWrapper");
				NUnit.Framework.Assert.AreEqual(((MetaClassTest.SomethingWrapper)o).DisplayI(), "Isomething");
			}
			catch (MetaClass.ClassCreationException e)
			{
				Sharpen.Runtime.PrintStackTrace(e);
				NUnit.Framework.Assert.IsFalse(true, "Should not exception on this call");
			}
		}

		private MetaClassTest.ManyConstructors MakeRef(int i, int j)
		{
			switch (i)
			{
				case 0:
				{
					switch (j)
					{
						case 0:
						{
							return new MetaClassTest.ManyConstructors(new string("hi"), new string("hi"));
						}

						case 1:
						{
							return new MetaClassTest.ManyConstructors(new string("hi"), new MetaClassTest.Something());
						}

						case 2:
						{
							return new MetaClassTest.ManyConstructors(new string("hi"), new MetaClassTest.SSomething());
						}

						case 3:
						{
							return new MetaClassTest.ManyConstructors(new string("hi"), new MetaClassTest.SubSSomething());
						}
					}
					return null;
				}

				case 1:
				{
					switch (j)
					{
						case 0:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.Something(), new string("hi"));
						}

						case 1:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.Something(), new MetaClassTest.Something());
						}

						case 2:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.Something(), new MetaClassTest.SSomething());
						}

						case 3:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.Something(), new MetaClassTest.SubSSomething());
						}
					}
					return null;
				}

				case 2:
				{
					switch (j)
					{
						case 0:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SSomething(), new string("hi"));
						}

						case 1:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SSomething(), new MetaClassTest.Something());
						}

						case 2:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SSomething(), new MetaClassTest.SSomething());
						}

						case 3:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SSomething(), new MetaClassTest.SubSSomething());
						}
					}
					return null;
				}

				case 3:
				{
					switch (j)
					{
						case 0:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SubSSomething(), new string("hi"));
						}

						case 1:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SubSSomething(), new MetaClassTest.Something());
						}

						case 2:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SubSSomething(), new MetaClassTest.SSomething());
						}

						case 3:
						{
							return new MetaClassTest.ManyConstructors(new MetaClassTest.SubSSomething(), new MetaClassTest.SubSSomething());
						}
					}
					return null;
				}
			}
			return null;
		}

		private static MetaClassTest.ManyConstructors MakeRef(int i)
		{
			switch (i)
			{
				case 0:
				{
					return new MetaClassTest.ManyConstructors(new string("hi"));
				}

				case 1:
				{
					return new MetaClassTest.ManyConstructors(new MetaClassTest.Something());
				}

				case 2:
				{
					return new MetaClassTest.ManyConstructors(new MetaClassTest.SSomething());
				}

				case 3:
				{
					return new MetaClassTest.ManyConstructors(new MetaClassTest.SubSSomething());
				}
			}
			return null;
		}

		[Test]
		public virtual void TestConsistencyWithJava()
		{
			object[] options = new object[] { new string("hi"), new MetaClassTest.Something(), new MetaClassTest.SSomething(), new MetaClassTest.SubSSomething() };
			/*
			* Single Term
			*/
			//--Cast everything as an object
			foreach (object option in options)
			{
				MetaClassTest.ManyConstructors @ref = new MetaClassTest.ManyConstructors(option);
				MetaClassTest.ManyConstructors test = (MetaClassTest.ManyConstructors)MetaClass.Create(Class + "$ManyConstructors").CreateFactory(typeof(object)).CreateInstance(option);
				NUnit.Framework.Assert.AreEqual(@ref.ConstructorInvoked(), test.ConstructorInvoked());
			}
			//--Use native types
			for (int i = 0; i < options.Length; i++)
			{
				MetaClassTest.ManyConstructors @ref = MakeRef(i);
				MetaClassTest.ManyConstructors test = (MetaClassTest.ManyConstructors)MetaClass.Create(Class + "$ManyConstructors").CreateInstance(options[i]);
				NUnit.Framework.Assert.AreEqual(@ref, test);
			}
			/*
			* Multi Term
			*/
			//--Use native types
			for (int i_1 = 0; i_1 < options.Length; i_1++)
			{
				for (int j = 0; j < options.Length; j++)
				{
					MetaClassTest.ManyConstructors @ref = MakeRef(i_1, j);
					MetaClassTest.ManyConstructors test = (MetaClassTest.ManyConstructors)MetaClass.Create(Class + "$ManyConstructors").CreateInstance(options[i_1], options[j]);
					NUnit.Framework.Assert.AreEqual(@ref, test);
				}
			}
		}

		[Test]
		public virtual void TestPrimitives()
		{
			// pass a value as a class
			MetaClass.Create(Class + "$Primitive").CreateInstance(7);
			MetaClass.Create(Class + "$Primitive").CreateInstance(System.Convert.ToDouble(7));
			// pass a value as a primitive
			MetaClass.Create(Class + "$Primitive").CreateInstance(7);
			MetaClass.Create(Class + "$Primitive").CreateInstance(2.8);
			//(fail)
			try
			{
				MetaClass.Create(Class + "$Primitive").CreateInstance(7L);
			}
			catch (MetaClass.ClassCreationException)
			{
				NUnit.Framework.Assert.IsTrue(true, "Should not be able to case Long int Primitive()");
			}
		}

		[Test]
		public virtual void TestCastSimple()
		{
			NUnit.Framework.Assert.AreEqual(1.0, MetaClass.Cast("1.0", typeof(double)));
			NUnit.Framework.Assert.AreEqual(1, MetaClass.Cast("1", typeof(int)));
			NUnit.Framework.Assert.AreEqual(1, MetaClass.Cast("1.0", typeof(int)));
			NUnit.Framework.Assert.AreEqual(1L, MetaClass.Cast("1.0", typeof(long)));
			NUnit.Framework.Assert.AreEqual((short)1, MetaClass.Cast("1.0", typeof(short)));
			NUnit.Framework.Assert.AreEqual(unchecked((byte)1), MetaClass.Cast("1.0", typeof(byte)));
			NUnit.Framework.Assert.AreEqual("Hello", MetaClass.Cast("Hello", typeof(string)));
			NUnit.Framework.Assert.AreEqual(true, MetaClass.Cast("true", typeof(bool)));
			NUnit.Framework.Assert.AreEqual(true, MetaClass.Cast("1", typeof(bool)));
			NUnit.Framework.Assert.AreEqual(false, MetaClass.Cast("False", typeof(bool)));
			NUnit.Framework.Assert.AreEqual(new File("/path/to/file"), MetaClass.Cast("/path/to/file", typeof(File)));
		}

		[Test]
		public virtual void TestCastArray()
		{
			int[] ints1 = MetaClass.Cast("[1,2,3]", typeof(int[]));
			Assert.AssertArrayEquals(new int[] { 1, 2, 3 }, ints1);
			int[] ints2 = MetaClass.Cast("(1,2,3)", typeof(int[]));
			Assert.AssertArrayEquals(new int[] { 1, 2, 3 }, ints2);
			int[] ints3 = MetaClass.Cast("1, 2, 3", typeof(int[]));
			Assert.AssertArrayEquals(new int[] { 1, 2, 3 }, ints3);
			int[] ints4 = MetaClass.Cast("1 2 3", typeof(int[]));
			Assert.AssertArrayEquals(new int[] { 1, 2, 3 }, ints4);
			int[] ints5 = MetaClass.Cast("1   2   3", typeof(int[]));
			Assert.AssertArrayEquals(new int[] { 1, 2, 3 }, ints5);
			int[] ints6 = MetaClass.Cast("\n1 \n\n  2   3", typeof(int[]));
			Assert.AssertArrayEquals(new int[] { 1, 2, 3 }, ints6);
			int[] intsEmpty = MetaClass.Cast(string.Empty, typeof(int[]));
			Assert.AssertArrayEquals(new int[] {  }, intsEmpty);
		}

		/// <exception cref="System.IO.IOException"/>
		[Test]
		public virtual void TestCastStringArray()
		{
			string[] strings1 = MetaClass.Cast("[a,b,c]", typeof(string[]));
			Assert.AssertArrayEquals(new string[] { "a", "b", "c" }, strings1);
			string string1 = Files.CreateTempFile("TestCastString", "tmp").ToString();
			string string2 = Files.CreateTempFile("TestCastString", "tmp").ToString();
			string[] strings2 = MetaClass.Cast("['" + string1 + "','" + string2 + "']", typeof(string[]));
			Assert.AssertArrayEquals(new string[] { string1, string2 }, strings2);
			string[] strings3 = MetaClass.Cast("['a','b','c']", typeof(string[]));
			Assert.AssertArrayEquals(new string[] { "a", "b", "c" }, strings3);
		}

		private enum Fruits
		{
			Apple,
			Orange,
			grape
		}

		[Test]
		public virtual void TestCastEnum()
		{
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.Apple, MetaClass.Cast("APPLE", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.Apple, MetaClass.Cast("apple", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.Apple, MetaClass.Cast("Apple", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.Apple, MetaClass.Cast("aPPlE", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.Orange, MetaClass.Cast("orange", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.grape, MetaClass.Cast("grape", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.grape, MetaClass.Cast("Grape", typeof(MetaClassTest.Fruits)));
			NUnit.Framework.Assert.AreEqual(MetaClassTest.Fruits.grape, MetaClass.Cast("GRAPE", typeof(MetaClassTest.Fruits)));
		}

		[Test]
		public virtual void TestCastCollection()
		{
			ICollection<string> set = new HashSet<string>();
			set.Add("apple");
			set.Add("banana");
			ICollection<string> castedSet = MetaClass.Cast("[apple, banana]", typeof(ISet));
			ICollection<string> castedSet2 = MetaClass.Cast("[apple ,    banana ]", typeof(ISet));
			ICollection<string> castedSet3 = MetaClass.Cast("{apple ,    banana }", typeof(ISet));
			NUnit.Framework.Assert.AreEqual(set, castedSet);
			NUnit.Framework.Assert.AreEqual(set, castedSet2);
			NUnit.Framework.Assert.AreEqual(set, castedSet3);
			IList<string> list = new LinkedList<string>();
			list.Add("apple");
			list.Add("banana");
			IList<string> castedList = MetaClass.Cast("[apple, banana]", typeof(IList));
			NUnit.Framework.Assert.AreEqual(list, castedList);
		}

		private class Pointer<E>
		{
			public E value;

			public Pointer(E value)
			{
				this.value = value;
			}

			public static MetaClassTest.Pointer<E> FromString<E>(string value)
			{
				// used via reflection
				E v = MetaClass.CastWithoutKnowingType(value);
				return new MetaClassTest.Pointer<E>(v);
			}
		}

		[Test]
		public virtual void TestCastMap()
		{
			IDictionary<string, string> a = MetaClass.Cast("{ a -> 1, b -> 2 }", typeof(IDictionary));
			NUnit.Framework.Assert.AreEqual(2, a.Count);
			NUnit.Framework.Assert.AreEqual("1", a["a"]);
			NUnit.Framework.Assert.AreEqual("2", a["b"]);
			IDictionary<string, string> b = MetaClass.Cast("a => 1, b -> 2", typeof(IDictionary));
			NUnit.Framework.Assert.AreEqual(2, b.Count);
			NUnit.Framework.Assert.AreEqual("1", b["a"]);
			NUnit.Framework.Assert.AreEqual("2", b["b"]);
			IDictionary<string, string> c = MetaClass.Cast("[a->1;b->2]", typeof(IDictionary));
			NUnit.Framework.Assert.AreEqual(2, c.Count);
			NUnit.Framework.Assert.AreEqual("1", c["a"]);
			NUnit.Framework.Assert.AreEqual("2", c["b"]);
			IDictionary<string, string> d = MetaClass.Cast("\n\na->\n1\n\n\nb->2", typeof(IDictionary));
			NUnit.Framework.Assert.AreEqual(2, d.Count);
			NUnit.Framework.Assert.AreEqual("1", d["a"]);
			NUnit.Framework.Assert.AreEqual("2", d["b"]);
		}

		[Test]
		public virtual void TestCastRegression()
		{
			// Generics ordering (integer should go relatively early)
			MetaClassTest.Pointer<int> x1 = MetaClass.Cast("1", typeof(MetaClassTest.Pointer));
			NUnit.Framework.Assert.AreEqual(1, x1.value);
		}

		private class FromStringable
		{
			public readonly string myContents;

			private FromStringable(string contents)
			{
				myContents = contents;
			}

			public static MetaClassTest.FromStringable FromString(string str)
			{
				return new MetaClassTest.FromStringable(str);
			}

			public override bool Equals(object o)
			{
				if (this == o)
				{
					return true;
				}
				if (!(o is MetaClassTest.FromStringable))
				{
					return false;
				}
				MetaClassTest.FromStringable that = (MetaClassTest.FromStringable)o;
				if (myContents != null ? !myContents.Equals(that.myContents) : that.myContents != null)
				{
					return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				return myContents != null ? myContents.GetHashCode() : 0;
			}
		}

		[Test]
		public virtual void TestCastFromString()
		{
			NUnit.Framework.Assert.AreEqual(new MetaClassTest.FromStringable("foo"), MetaClass.Cast("foo", typeof(MetaClassTest.FromStringable)));
			NUnit.Framework.Assert.AreEqual(new MetaClassTest.FromStringable("bar"), MetaClass.Cast("bar", typeof(MetaClassTest.FromStringable)));
		}

		[Test]
		public virtual void TestCastStream()
		{
			NUnit.Framework.Assert.AreEqual(System.Console.Out, MetaClass.Cast("stdout", typeof(OutputStream)));
			NUnit.Framework.Assert.AreEqual(System.Console.Out, MetaClass.Cast("out", typeof(OutputStream)));
			NUnit.Framework.Assert.AreEqual(System.Console.Error, MetaClass.Cast("stderr", typeof(OutputStream)));
			NUnit.Framework.Assert.AreEqual(System.Console.Error, MetaClass.Cast("err", typeof(OutputStream)));
			NUnit.Framework.Assert.AreEqual(typeof(ObjectOutputStream), MetaClass.Cast("err", typeof(ObjectOutputStream)).GetType());
		}
		//	TODO(gabor) this would be kind of cool to implement
		/*
		@Test
		@Ignore
		public void testVariableArgConstructor(){
		VarArgs a = MetaClass.create(CLASS+"$VarArgs").createInstance(1,2,3);
		assertEquals(3, a.a.length);
		assertTrue(a.a[0] == 1);
		assertTrue(a.a[1] == 2);
		assertTrue(a.a[2] == 3);
		}
		*/
	}
}

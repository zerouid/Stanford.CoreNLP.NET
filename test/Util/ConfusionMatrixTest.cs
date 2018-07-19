using Java.Lang;
using Java.Util;
using Sharpen;

namespace Edu.Stanford.Nlp.Util
{
	/// <summary>Tests that the output of the ConfusionMatrix is in the expected format.</summary>
	/// <author>Eric Yeh yeh1@cs.stanford.edu</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class ConfusionMatrixTest
	{
		internal bool echo;

		public ConfusionMatrixTest()
			: this(false)
		{
		}

		public ConfusionMatrixTest(bool echo)
		{
			this.echo = echo;
		}

		[NUnit.Framework.Test]
		public virtual void TestBasic()
		{
			string expected = "      Guess/Gold      C1      C2      C3    Marg. (Guess)\n" + "              C1       2       0       0       2\n" + "              C2       1       0       0       1\n" + "              C3       0       0       1       1\n"
				 + "    Marg. (Gold)       3       0       1\n\n" + "              C1 = a        prec=1, recall=0.66667, spec=1, f1=0.8\n" + "              C2 = b        prec=0, recall=n/a, spec=0.75, f1=n/a\n" + "              C3 = c        prec=1, recall=1, spec=1, f1=1\n";
			ConfusionMatrix<string> conf = new ConfusionMatrix<string>(Locale.Us);
			conf.Add("a", "a");
			conf.Add("a", "a");
			conf.Add("b", "a");
			conf.Add("c", "c");
			string result = conf.PrintTable();
			if (echo)
			{
				System.Console.Error.WriteLine(result);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(expected, result);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestRealLabels()
		{
			string expected = "      Guess/Gold       a       b       c    Marg. (Guess)\n" + "               a       2       0       0       2\n" + "               b       1       0       0       1\n" + "               c       0       0       1       1\n"
				 + "    Marg. (Gold)       3       0       1\n\n" + "               a        prec=1, recall=0.66667, spec=1, f1=0.8\n" + "               b        prec=0, recall=n/a, spec=0.75, f1=n/a\n" + "               c        prec=1, recall=1, spec=1, f1=1\n";
			ConfusionMatrix<string> conf = new ConfusionMatrix<string>(Locale.Us);
			conf.SetUseRealLabels(true);
			conf.Add("a", "a");
			conf.Add("a", "a");
			conf.Add("b", "a");
			conf.Add("c", "c");
			string result = conf.PrintTable();
			if (echo)
			{
				System.Console.Error.WriteLine(result);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(expected, result);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestBulkAdd()
		{
			string expected = "      Guess/Gold      C1      C2    Marg. (Guess)\n" + "              C1      10       5      15\n" + "              C2       2       3       5\n" + "    Marg. (Gold)      12       8\n\n" + "              C1 = 1        prec=0.66667, recall=0.83333, spec=0.375, f1=0.74074\n"
				 + "              C2 = 2        prec=0.6, recall=0.375, spec=0.83333, f1=0.46154\n";
			ConfusionMatrix<int> conf = new ConfusionMatrix<int>(Locale.Us);
			conf.Add(1, 1, 10);
			conf.Add(1, 2, 5);
			conf.Add(2, 1, 2);
			conf.Add(2, 2, 3);
			string result = conf.PrintTable();
			if (echo)
			{
				System.Console.Error.WriteLine(result);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(expected, result);
			}
		}

		private class BackwardsInteger : IComparable<ConfusionMatrixTest.BackwardsInteger>
		{
			private readonly int value;

			public BackwardsInteger(int value)
			{
				this.value = value;
			}

			public virtual int CompareTo(ConfusionMatrixTest.BackwardsInteger other)
			{
				return other.value - this.value;
			}

			// backwards
			public override int GetHashCode()
			{
				return value;
			}

			public override bool Equals(object o)
			{
				if (o == null || (!(o is ConfusionMatrixTest.BackwardsInteger)))
				{
					return false;
				}
				return (((ConfusionMatrixTest.BackwardsInteger)o).value == value);
			}

			public override string ToString()
			{
				return int.ToString(value);
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestValueSort()
		{
			string expected = "      Guess/Gold       2       1    Marg. (Guess)\n" + "               2       3       2       5\n" + "               1       5      10      15\n" + "    Marg. (Gold)       8      12\n\n" + "               2        prec=0.6, recall=0.375, spec=0.83333, f1=0.46154\n"
				 + "               1        prec=0.66667, recall=0.83333, spec=0.375, f1=0.74074\n";
			ConfusionMatrixTest.BackwardsInteger one = new ConfusionMatrixTest.BackwardsInteger(1);
			ConfusionMatrixTest.BackwardsInteger two = new ConfusionMatrixTest.BackwardsInteger(2);
			ConfusionMatrix<ConfusionMatrixTest.BackwardsInteger> conf = new ConfusionMatrix<ConfusionMatrixTest.BackwardsInteger>(Locale.Us);
			conf.SetUseRealLabels(true);
			conf.Add(one, one, 10);
			conf.Add(one, two, 5);
			conf.Add(two, one, 2);
			conf.Add(two, two, 3);
			string result = conf.PrintTable();
			if (echo)
			{
				System.Console.Error.WriteLine(result);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(expected, result);
			}
		}

		public static void Main(string[] args)
		{
			ConfusionMatrixTest tester = new ConfusionMatrixTest(true);
			System.Console.Out.WriteLine("Test 1");
			tester.TestBasic();
			System.Console.Out.WriteLine("\nTest 2");
			tester.TestRealLabels();
			System.Console.Out.WriteLine("\nTest 3");
			tester.TestBulkAdd();
			System.Console.Out.WriteLine("\nTest 4");
			tester.TestValueSort();
		}
	}
}

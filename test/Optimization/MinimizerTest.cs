


namespace Edu.Stanford.Nlp.Optimization
{
	/// <author>Christopher Manning</author>
	[NUnit.Framework.TestFixture]
	[NUnit.Framework.TestFixture]
	public class MinimizerTest
	{
		/// <summary>H.H.</summary>
		/// <remarks>
		/// H.H. Rosenbrock. 1960. An Automatic Method for Finding the Greatest or
		/// Least Value of a Function. Computer Journal 3, 175-184.
		/// </remarks>
		private class RosenbrockFunction : IDiffFunction
		{
			public virtual double[] DerivativeAt(double[] x)
			{
				double[] derivatives = new double[2];
				// df/dx = -400x(y-x^2) - 2(1-x)
				derivatives[0] = -400.0 * x[0] * (x[1] - x[0] * x[0]) - 2 * (1.0 - x[0]);
				// df/dy = 200(y-x^2)
				derivatives[1] = 200.0 * (x[1] - x[0] * x[0]);
				return derivatives;
			}

			/// <summary>f(x,y) = (1-x)^2 + 100(y-x^2)^2</summary>
			public virtual double ValueAt(double[] x)
			{
				double t1 = (1.0 - x[0]);
				double t2 = x[1] - x[0] * x[0];
				return t1 * t1 + 100.0 * t2 * t2;
			}

			public virtual int DomainDimension()
			{
				return 2;
			}
		}

		[NUnit.Framework.Test]
		public virtual void TestRosenbrock()
		{
			IDiffFunction rf = new MinimizerTest.RosenbrockFunction();
			DiffFunctionTest.GradientCheck(rf);
		}

		[NUnit.Framework.Test]
		public virtual void TestQNMinimizerRosenbrock()
		{
			double[] initial = new double[] { 0.0, 0.0 };
			IDiffFunction rf = new MinimizerTest.RosenbrockFunction();
			QNMinimizer qn = new QNMinimizer();
			double[] answer = qn.Minimize(rf, 1e-10, initial);
			System.Console.Error.WriteLine("Answer is: " + Arrays.ToString(answer));
			NUnit.Framework.Assert.AreEqual(1.0, answer[0], 1e-8);
			NUnit.Framework.Assert.AreEqual(1.0, answer[1], 1e-8);
		}
	}
}

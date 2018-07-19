using NUnit.Framework;
using Org.Ejml.Simple;
using Sharpen;

namespace Edu.Stanford.Nlp.Neural
{
	/// <author>Minh-Thang Luong <lmthang@stanford.edu>, created on Nov 15, 2013</author>
	[NUnit.Framework.TestFixture]
	public class NeuralUtilsTest
	{
		[Test]
		public virtual void TestCosine()
		{
			double[][] values = new double[][] { new double[5] };
			values[0] = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 };
			SimpleMatrix vector1 = new SimpleMatrix(values);
			values[0] = new double[] { 0.5, 0.4, 0.3, 0.2, 0.1 };
			SimpleMatrix vector2 = new SimpleMatrix(values);
			NUnit.Framework.Assert.AreEqual(NeuralUtils.Dot(vector1, vector2), 1e-5, 0.35000000000000003);
			NUnit.Framework.Assert.AreEqual(NeuralUtils.Cosine(vector1, vector2), 1e-5, 0.6363636363636364);
			vector1 = vector1.Transpose();
			vector2 = vector2.Transpose();
			NUnit.Framework.Assert.AreEqual(NeuralUtils.Dot(vector1, vector2), 1e-5, 0.35000000000000003);
			NUnit.Framework.Assert.AreEqual(NeuralUtils.Cosine(vector1, vector2), 1e-5, 0.6363636363636364);
		}

		public virtual void TestIsZero()
		{
			double[][] values = new double[][] { new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 }, new double[] { 0.0, 0.0, 0.0, 0.0, 0.0 } };
			SimpleMatrix vector1 = new SimpleMatrix(values);
			NUnit.Framework.Assert.IsFalse(NeuralUtils.IsZero(vector1));
			values = new double[][] { new double[] { 0.0, 0.0, 0.0, 0.0, 0.0 }, new double[] { 0.0, 0.0, 0.0, 0.0, 0.0 } };
			vector1 = new SimpleMatrix(values);
			NUnit.Framework.Assert.IsTrue(NeuralUtils.IsZero(vector1));
		}
	}
}

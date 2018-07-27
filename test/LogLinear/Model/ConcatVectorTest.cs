using System;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;



using NUnit.Framework;
using NUnit.Framework.Contrib.Theories;


namespace Edu.Stanford.Nlp.Loglinear.Model
{
	public class ConcatVectorTest
	{
		[Theory]
		public virtual void TestNewEmptyClone(ConcatVectorTest.DenseTestVector d1)
		{
			ConcatVector empty = new ConcatVector(d1.vector.GetNumberOfComponents());
			ConcatVector emptyClone = d1.vector.NewEmptyClone();
			NUnit.Framework.Assert.IsTrue(empty.ValueEquals(emptyClone, 1.0e-5));
		}

		[Theory]
		public virtual void TestResizeOnSetComponent(ConcatVectorTest.DenseTestVector d1)
		{
			d1.vector.SetSparseComponent(d1.values.Length, 1, 0.0);
			d1.vector.SetDenseComponent(d1.values.Length + 1, new double[] { 0.0 });
		}

		[Theory]
		public virtual void TestCopyOnWrite(ConcatVectorTest.DenseTestVector d1)
		{
			ConcatVector v2 = d1.vector.DeepClone();
			v2.AddVectorInPlace(v2, 1.0);
			for (int i = 0; i < d1.values.Length; i++)
			{
				for (int j = 0; j < d1.values[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(d1.vector.GetValueAt(i, j), 5.0e-4, d1.values[i][j]);
					NUnit.Framework.Assert.AreEqual(v2.GetValueAt(i, j), 5.0e-4, d1.values[i][j] * 2);
				}
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestAppendDenseComponent(double[] vector1, double[] vector2)
		{
			ConcatVector v1 = new ConcatVector(1);
			ConcatVector v2 = new ConcatVector(1);
			v1.SetDenseComponent(0, vector1);
			v2.SetDenseComponent(0, vector2);
			double sum = 0.0f;
			for (int i = 0; i < Math.Min(vector1.Length, vector2.Length); i++)
			{
				sum += vector1[i] * vector2[i];
			}
			NUnit.Framework.Assert.AreEqual(v1.DotProduct(v2), 5.0e-4, sum);
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestAppendSparseComponent(int sparse1, double sparse1Val, int sparse2, double sparse2Val)
		{
			ConcatVector v1 = new ConcatVector(1);
			ConcatVector v2 = new ConcatVector(1);
			v1.SetSparseComponent(0, (int)sparse1, sparse1Val);
			v2.SetSparseComponent(0, (int)sparse2, sparse2Val);
			if (sparse1 == sparse2)
			{
				NUnit.Framework.Assert.AreEqual(v1.DotProduct(v2), 5.0e-4, sparse1Val * sparse2Val);
			}
			else
			{
				NUnit.Framework.Assert.AreEqual(v1.DotProduct(v2), 5.0e-4, 0.0);
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestGetSparseIndex(int sparse1, double sparse1Val, int sparse2, double sparse2Val)
		{
			ConcatVector v1 = new ConcatVector(2);
			ConcatVector v2 = new ConcatVector(2);
			v1.SetSparseComponent(0, (int)sparse1, sparse1Val);
			v1.SetSparseComponent(1, (int)sparse2, sparse1Val);
			v2.SetSparseComponent(0, (int)sparse2, sparse2Val);
			v2.SetSparseComponent(1, (int)sparse1, sparse2Val);
			NUnit.Framework.Assert.AreEqual(sparse1, v1.GetSparseIndex(0));
			NUnit.Framework.Assert.AreEqual(sparse2, v1.GetSparseIndex(1));
			NUnit.Framework.Assert.AreEqual(sparse2, v2.GetSparseIndex(0));
			NUnit.Framework.Assert.AreEqual(sparse1, v2.GetSparseIndex(1));
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestInnerProduct(ConcatVectorTest.DenseTestVector d1, ConcatVectorTest.DenseTestVector d2)
		{
			NUnit.Framework.Assert.AreEqual(d1.vector.DotProduct(d2.vector) + d2.vector.DotProduct(d2.vector), 5.0e-4, d1.TrueInnerProduct((ConcatVectorTest.DenseTestVector)d2) + d2.TrueInnerProduct((ConcatVectorTest.DenseTestVector)d2));
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestDeepClone(ConcatVectorTest.DenseTestVector d1, ConcatVectorTest.DenseTestVector d2)
		{
			NUnit.Framework.Assert.AreEqual(d1.vector.DeepClone().DotProduct(d2.vector), 5.0e-4, d1.vector.DotProduct(d2.vector));
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestDeepCloneGetValueAt(ConcatVectorTest.DenseTestVector d1)
		{
			ConcatVector mv = d1.vector;
			ConcatVector clone = d1.vector.DeepClone();
			for (int i = 0; i < d1.values.Length; i++)
			{
				for (int j = 0; j < d1.values[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(clone.GetValueAt(i, j), 1.0e-10, mv.GetValueAt(i, j));
				}
			}
		}

		[Theory]
		public virtual void TestAddDenseToDense(double[] dense1, double[] dense2)
		{
			ConcatVector v1 = new ConcatVector(1);
			v1.SetDenseComponent(0, dense1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetDenseComponent(0, dense2);
			double expected = v1.DotProduct(v2) + 0.7f * (v2.DotProduct(v2));
			v1.AddVectorInPlace(v2, 0.7f);
			NUnit.Framework.Assert.AreEqual(v1.DotProduct(v2), 5.0e-3, expected);
		}

		[Theory]
		public virtual void TestAddSparseToDense(double[] dense1, int sparseIndex, double v)
		{
			ConcatVector v1 = new ConcatVector(1);
			v1.SetDenseComponent(0, dense1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetSparseComponent(0, (int)sparseIndex, v);
			double expected = v1.DotProduct(v2) + 0.7f * (v2.DotProduct(v2));
			v1.AddVectorInPlace(v2, 0.7f);
			NUnit.Framework.Assert.AreEqual(v1.DotProduct(v2), 5.0e-4, expected);
		}

		[Theory]
		public virtual void TestAddDenseToSparse(double[] dense1, int sparseIndex, double v)
		{
			Assume.AssumeTrue(sparseIndex >= 0);
			Assume.AssumeTrue(sparseIndex <= 100);
			ConcatVector v1 = new ConcatVector(1);
			v1.SetDenseComponent(0, dense1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetSparseComponent(0, (int)sparseIndex, v);
			double expected = v1.DotProduct(v2) + 0.7f * (v1.DotProduct(v1));
			v2.AddVectorInPlace(v1, 0.7f);
			NUnit.Framework.Assert.AreEqual(v2.DotProduct(v1), 5.0e-4, expected);
		}

		[Theory]
		public virtual void TestAddSparseToSparse(int sparseIndex1, double val1, int sparseIndex2, double val2)
		{
			ConcatVector v1 = new ConcatVector(1);
			v1.SetSparseComponent(0, (int)sparseIndex1, val1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetSparseComponent(0, (int)sparseIndex2, val2);
			double expected = v1.DotProduct(v2) + 0.7f * (v2.DotProduct(v2));
			v1.AddVectorInPlace(v2, 0.7f);
			NUnit.Framework.Assert.AreEqual(v1.DotProduct(v2), 5.0e-3, expected);
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestInnerProduct2(ConcatVectorTest.DenseTestVector d1, ConcatVectorTest.DenseTestVector d2, ConcatVectorTest.DenseTestVector d3)
		{
			// Test the invariant x^Tz + 0.7*y^Tz == (x+0.7*y)^Tz
			double d1d3 = d1.vector.DotProduct(d3.vector);
			NUnit.Framework.Assert.AreEqual(d1d3, 5.0e-4, d1.TrueInnerProduct((ConcatVectorTest.DenseTestVector)d3));
			double d2d3 = d2.vector.DotProduct(d3.vector);
			NUnit.Framework.Assert.AreEqual(d2d3, 5.0e-4, d2.TrueInnerProduct((ConcatVectorTest.DenseTestVector)d3));
			double expected = d1d3 + (0.7f * d2d3);
			NUnit.Framework.Assert.AreEqual(expected, 5.0e-4, d1.TrueInnerProduct((ConcatVectorTest.DenseTestVector)d3) + (0.7 * d2.TrueInnerProduct((ConcatVectorTest.DenseTestVector)d3)));
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestAddVector(ConcatVectorTest.DenseTestVector d1, ConcatVectorTest.DenseTestVector d2, ConcatVectorTest.DenseTestVector d3)
		{
			// Test the invariant x^Tz + 0.7*y^Tz == (x+0.7*y)^Tz
			double expected = d1.vector.DotProduct(d3.vector) + (0.7f * d2.vector.DotProduct(d3.vector));
			ConcatVector clone = d1.vector.DeepClone();
			clone.AddVectorInPlace(d2.vector, 0.7f);
			NUnit.Framework.Assert.AreEqual(clone.DotProduct(d3.vector), 5.0e-4, expected);
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestProtoVector(ConcatVectorTest.DenseTestVector d1, ConcatVectorTest.DenseTestVector d2)
		{
			double expected = d1.vector.DotProduct(d2.vector);
			ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
			System.Diagnostics.Debug.Assert((d1.vector.GetType() == typeof(ConcatVector)));
			d1.vector.WriteToStream(byteArrayOutputStream);
			byteArrayOutputStream.Close();
			byte[] bytes = byteArrayOutputStream.ToByteArray();
			ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(bytes);
			ConcatVector recovered = ConcatVector.ReadFromStream(byteArrayInputStream);
			NUnit.Framework.Assert.AreEqual(recovered.DotProduct(d2.vector), 5.0e-4, expected);
		}

		[Theory]
		public virtual void TestSizes(ConcatVectorTest.DenseTestVector d1)
		{
			int size = d1.vector.GetNumberOfComponents();
			NUnit.Framework.Assert.AreEqual(d1.values.Length, size);
		}

		[Theory]
		public virtual void TestIsSparse(ConcatVectorTest.DenseTestVector d1)
		{
			int size = d1.vector.GetNumberOfComponents();
			NUnit.Framework.Assert.AreEqual(d1.values.Length, size);
			for (int i = 0; i < d1.values.Length; i++)
			{
				if (d1.vector.IsComponentSparse(i))
				{
					for (int j = 0; j < d1.values[i].Length; j++)
					{
						if (d1.vector.GetSparseIndex(i) != j)
						{
							NUnit.Framework.Assert.AreEqual(d1.values[i][j], 1.0e-9, 0.0);
						}
					}
				}
			}
		}

		[Theory]
		public virtual void TestRetrieveDense(ConcatVectorTest.DenseTestVector d1)
		{
			int size = d1.vector.GetNumberOfComponents();
			NUnit.Framework.Assert.AreEqual(d1.values.Length, size);
			for (int i = 0; i < d1.values.Length; i++)
			{
				if (!d1.vector.IsComponentSparse(i))
				{
					Assert.AssertArrayEquals(d1.vector.GetDenseComponent(i), 1.0e-9, d1.values[i]);
				}
			}
		}

		[Theory]
		public virtual void TestGetValueAt(ConcatVectorTest.DenseTestVector d1)
		{
			for (int i = 0; i < d1.values.Length; i++)
			{
				for (int j = 0; j < d1.values[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(d1.vector.GetValueAt(i, j), 5.0e-4, d1.values[i][j]);
				}
			}
		}

		[Theory]
		public virtual void TestElementwiseProduct(ConcatVectorTest.DenseTestVector d1, ConcatVectorTest.DenseTestVector d2)
		{
			for (int i = 0; i < d1.values.Length; i++)
			{
				for (int j = 0; j < d1.values[i].Length; j++)
				{
					Assume.AssumeTrue(d1.values[i][j] == d1.vector.GetValueAt(i, j));
				}
			}
			for (int i_1 = 0; i_1 < d2.values.Length; i_1++)
			{
				for (int j = 0; j < d2.values[i_1].Length; j++)
				{
					Assume.AssumeTrue(d2.values[i_1][j] == d2.vector.GetValueAt(i_1, j));
				}
			}
			ConcatVector clone = d1.vector.DeepClone();
			clone.ElementwiseProductInPlace(d2.vector);
			for (int i_2 = 0; i_2 < d1.values.Length; i_2++)
			{
				for (int j = 0; j < d1.values[i_2].Length; j++)
				{
					double val = 0.0f;
					if (i_2 < d2.values.Length)
					{
						if (j < d2.values[i_2].Length)
						{
							val = d1.values[i_2][j] * d2.values[i_2][j];
						}
					}
					NUnit.Framework.Assert.AreEqual(clone.GetValueAt(i_2, j), 5.0e-4, val);
				}
			}
		}

		[Theory]
		public virtual void TestElementwiseDenseToDense(double[] dense1, double[] dense2)
		{
			ConcatVector v1 = new ConcatVector(1);
			v1.SetDenseComponent(0, dense1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetDenseComponent(0, dense2);
			v1.ElementwiseProductInPlace(v2);
			for (int i = 0; i < dense1.Length; i++)
			{
				double expected = 0.0f;
				if (i < dense2.Length)
				{
					expected = dense1[i] * dense2[i];
				}
				NUnit.Framework.Assert.AreEqual(v1.GetValueAt(0, i), 5.0e-4, expected);
			}
		}

		[Theory]
		public virtual void TestElementwiseSparseToDense(double[] dense1, int sparseIndex, double v)
		{
			ConcatVector v1 = new ConcatVector(1);
			v1.SetDenseComponent(0, dense1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetSparseComponent(0, (int)sparseIndex, v);
			v1.ElementwiseProductInPlace(v2);
			for (int i = 0; i < dense1.Length; i++)
			{
				double expected = 0.0f;
				if (i == sparseIndex)
				{
					expected = dense1[i] * v;
				}
				NUnit.Framework.Assert.AreEqual(v1.GetValueAt(0, i), 5.0e-4, expected);
			}
		}

		[Theory]
		public virtual void TestElementwiseDenseToSparse(double[] dense1, int sparseIndex, double v)
		{
			Assume.AssumeTrue(sparseIndex >= 0);
			Assume.AssumeTrue(sparseIndex <= 100);
			ConcatVector v1 = new ConcatVector(1);
			v1.SetDenseComponent(0, dense1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetSparseComponent(0, (int)sparseIndex, v);
			v2.ElementwiseProductInPlace(v1);
			for (int i = 0; i < dense1.Length; i++)
			{
				double expected = 0.0f;
				if (i == sparseIndex)
				{
					expected = dense1[i] * v;
				}
				NUnit.Framework.Assert.AreEqual(v2.GetValueAt(0, i), 5.0e-4, expected);
			}
		}

		[Theory]
		public virtual void TestElementwiseSparseToSparse(int sparseIndex1, double val1, int sparseIndex2, double val2)
		{
			ConcatVector v1 = new ConcatVector(1);
			v1.SetSparseComponent(0, (int)sparseIndex1, val1);
			ConcatVector v2 = new ConcatVector(1);
			v2.SetSparseComponent(0, (int)sparseIndex2, val2);
			v1.ElementwiseProductInPlace(v2);
			for (int i = 0; i < 10; i++)
			{
				double expected = 0.0f;
				if (i == sparseIndex1 && i == sparseIndex2)
				{
					expected = val1 * val2;
				}
				NUnit.Framework.Assert.AreEqual(v1.GetValueAt(0, i), 5.0e-4, expected);
			}
		}

		[Theory]
		public virtual void TestMap(ConcatVectorTest.DenseTestVector d1)
		{
			d1.vector.MapInPlace(null);
			d1.Map(null);
			for (int i = 0; i < d1.values.Length; i++)
			{
				for (int j = 0; j < d1.values[i].Length; j++)
				{
					NUnit.Framework.Assert.AreEqual(d1.vector.GetValueAt(i, j), 5.0e-4, d1.values[i][j]);
				}
			}
		}

		[Theory]
		public virtual void TestValueEquals(ConcatVectorTest.DenseTestVector d1)
		{
			ConcatVector clone = d1.vector.DeepClone();
			NUnit.Framework.Assert.IsTrue(clone.ValueEquals(d1.vector, 1.0e-5));
			NUnit.Framework.Assert.IsTrue(d1.vector.ValueEquals(clone, 1.0e-5));
			NUnit.Framework.Assert.IsTrue(d1.vector.ValueEquals(d1.vector, 1.0e-5));
			NUnit.Framework.Assert.IsTrue(clone.ValueEquals(clone, 1.0e-5));
			Random r = new Random();
			int size = clone.GetNumberOfComponents();
			if (size > 0)
			{
				clone.AddVectorInPlace(d1.vector, 1.0);
				// If the clone is a 0 vector
				bool isZero = true;
				foreach (double[] arr in d1.values)
				{
					foreach (double d in arr)
					{
						if (d != 0)
						{
							isZero = false;
						}
					}
				}
				if (isZero)
				{
					NUnit.Framework.Assert.IsTrue(clone.ValueEquals(d1.vector, 1.0e-5));
					NUnit.Framework.Assert.IsTrue(d1.vector.ValueEquals(clone, 1.0e-5));
				}
				else
				{
					NUnit.Framework.Assert.IsFalse(clone.ValueEquals(d1.vector, 1.0e-5));
					NUnit.Framework.Assert.IsFalse(d1.vector.ValueEquals(clone, 1.0e-5));
				}
				NUnit.Framework.Assert.IsTrue(d1.vector.ValueEquals(d1.vector, 1.0e-5));
				NUnit.Framework.Assert.IsTrue(clone.ValueEquals(clone, 1.0e-5));
				// refresh the clone
				clone = d1.vector.DeepClone();
				int tinker = r.NextInt(size);
				d1.vector.SetDenseComponent(tinker, new double[] { 0, 0, 1 });
				clone.SetSparseComponent(tinker, 2, 1);
				NUnit.Framework.Assert.IsTrue(d1.vector.ValueEquals(clone, 1.0e-5));
				NUnit.Framework.Assert.IsTrue(clone.ValueEquals(d1.vector, 1.0e-5));
			}
		}

		/// <summary>Created by keenon on 12/6/14.</summary>
		/// <remarks>
		/// Created by keenon on 12/6/14.
		/// <p>
		/// DenseVector with obviously correct semantics for checking the MultiVector against.
		/// </remarks>
		public class DenseTestVector
		{
			public double[][] values;

			public ConcatVector vector;

			public DenseTestVector(double[][] values, ConcatVector vector)
			{
				this.values = values;
				this.vector = vector;
			}

			public virtual double TrueInnerProduct(ConcatVectorTest.DenseTestVector testVector)
			{
				double sum = 0.0f;
				for (int i = 0; i < Math.Min(values.Length, testVector.values.Length); i++)
				{
					for (int j = 0; j < Math.Min(values[i].Length, testVector.values[i].Length); j++)
					{
						sum += values[i][j] * testVector.values[i][j];
					}
				}
				return sum;
			}

			public virtual void Map(IDoubleUnaryOperator fn)
			{
				for (int i = 0; i < values.Length; i++)
				{
					for (int j = 0; j < values[i].Length; j++)
					{
						values[i][j] = fn.ApplyAsDouble(values[i][j]);
					}
				}
			}

			public override string ToString()
			{
				return vector.ToString();
			}
		}

		/// <summary>Created by keenon on 12/6/14.</summary>
		/// <remarks>
		/// Created by keenon on 12/6/14.
		/// <p>
		/// Handles generating the inputs for Quickcheck against the MultiVector
		/// </remarks>
		public class DenseTestVectorGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<ConcatVectorTest.DenseTestVector>
		{
			public DenseTestVectorGenerator(Type type)
				: base(type)
			{
			}

			internal const int SparseVectorLength = 5;

			public DenseTestVectorGenerator()
				: base(typeof(ConcatVectorTest.DenseTestVector))
			{
			}

			public override ConcatVectorTest.DenseTestVector Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				int length = sourceOfRandomness.NextInt(10);
				double[][] trueValues = new double[length][];
				bool[] sparse = new bool[length];
				int[] sizes = new int[length];
				// Generate sizes in advance, so we can pass the clues on to the constructor for the multivector
				for (int i = 0; i < length; i++)
				{
					bool isSparse = sourceOfRandomness.NextBoolean();
					sparse[i] = isSparse;
					if (isSparse)
					{
						sizes[i] = -1;
					}
					else
					{
						int componentLength = sourceOfRandomness.NextInt(SparseVectorLength);
						sizes[i] = componentLength;
					}
				}
				ConcatVector mv = new ConcatVector(length);
				for (int i_1 = 0; i_1 < length; i_1++)
				{
					if (sparse[i_1])
					{
						trueValues[i_1] = new double[SparseVectorLength];
						int sparseIndex = sourceOfRandomness.NextInt(SparseVectorLength);
						double sparseValue = sourceOfRandomness.NextDouble();
						trueValues[i_1][sparseIndex] = sparseValue;
						mv.SetSparseComponent(i_1, sparseIndex, sparseValue);
					}
					else
					{
						trueValues[i_1] = new double[sizes[i_1]];
						// Ensure we have some null components in our generated vector
						if (sizes[i_1] > 0)
						{
							for (int j = 0; j < sizes[i_1]; j++)
							{
								trueValues[i_1][j] = sourceOfRandomness.NextDouble();
							}
							mv.SetDenseComponent(i_1, trueValues[i_1]);
						}
					}
				}
				return new ConcatVectorTest.DenseTestVector(trueValues, mv);
			}
		}
	}
}

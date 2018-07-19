using System;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;
using Java.IO;
using NUnit.Framework.Contrib.Theories;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 8/9/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This will attempt to quickcheck the ConcatVectorTable class. There isn't much functionality in there, so this is a short test
	/// suite.
	/// </author>
	public class ConcatVectorTableTest
	{
		public static ConcatVectorTable ConvertArrayToVectorTable(ConcatVector[][][] factor3)
		{
			int[] neighborSizes = new int[] { factor3.Length, factor3[0].Length, factor3[0][0].Length };
			ConcatVectorTable concatVectorTable = new ConcatVectorTable(neighborSizes);
			for (int i = 0; i < factor3.Length; i++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						int iF = i;
						int jF = j;
						int kF = k;
						concatVectorTable.SetAssignmentValue(new int[] { i, j, k }, null);
					}
				}
			}
			return concatVectorTable;
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestCache(ConcatVector[][][] factor3, int numUses)
		{
			int[] dimensions = new int[] { factor3.Length, factor3[0].Length, factor3[0][0].Length };
			int[][][] thunkHits = new int[][][] {  };
			ConcatVectorTable table = new ConcatVectorTable(dimensions);
			for (int i = 0; i < factor3.Length; i++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						int[] assignment = new int[] { i, j, k };
						table.SetAssignmentValue(assignment, null);
					}
				}
			}
			// Pre-cacheing
			for (int n = 0; n < numUses; n++)
			{
				for (int i_1 = 0; i_1 < factor3.Length; i_1++)
				{
					for (int j = 0; j < factor3[0].Length; j++)
					{
						for (int k = 0; k < factor3[0][0].Length; k++)
						{
							int[] assignment = new int[] { i_1, j, k };
							table.GetAssignmentValue(assignment).Get();
						}
					}
				}
			}
			for (int i_2 = 0; i_2 < factor3.Length; i_2++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						NUnit.Framework.Assert.AreEqual(numUses, thunkHits[i_2][j][k]);
					}
				}
			}
			table.CacheVectors();
			// Cached
			for (int n_1 = 0; n_1 < numUses; n_1++)
			{
				for (int i_1 = 0; i_1 < factor3.Length; i_1++)
				{
					for (int j = 0; j < factor3[0].Length; j++)
					{
						for (int k = 0; k < factor3[0][0].Length; k++)
						{
							int[] assignment = new int[] { i_1, j, k };
							table.GetAssignmentValue(assignment).Get();
						}
					}
				}
			}
			for (int i_3 = 0; i_3 < factor3.Length; i_3++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						NUnit.Framework.Assert.AreEqual(numUses + 1, thunkHits[i_3][j][k]);
					}
				}
			}
			table.ReleaseCache();
			// Post-cacheing
			for (int n_2 = 0; n_2 < numUses; n_2++)
			{
				for (int i_1 = 0; i_1 < factor3.Length; i_1++)
				{
					for (int j = 0; j < factor3[0].Length; j++)
					{
						for (int k = 0; k < factor3[0][0].Length; k++)
						{
							int[] assignment = new int[] { i_1, j, k };
							table.GetAssignmentValue(assignment).Get();
						}
					}
				}
			}
			for (int i_4 = 0; i_4 < factor3.Length; i_4++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						NUnit.Framework.Assert.AreEqual((2 * numUses) + 1, thunkHits[i_4][j][k]);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestProtoTable(ConcatVector[][][] factor3)
		{
			ConcatVectorTable concatVectorTable = ConvertArrayToVectorTable((ConcatVector[][][])factor3);
			ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
			concatVectorTable.WriteToStream(byteArrayOutputStream);
			byteArrayOutputStream.Close();
			byte[] bytes = byteArrayOutputStream.ToByteArray();
			ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(bytes);
			ConcatVectorTable recovered = ConcatVectorTable.ReadFromStream(byteArrayInputStream);
			for (int i = 0; i < factor3.Length; i++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						NUnit.Framework.Assert.IsTrue(factor3[i][j][k].ValueEquals(recovered.GetAssignmentValue(new int[] { i, j, k }).Get(), 1.0e-5));
					}
				}
			}
			NUnit.Framework.Assert.IsTrue(concatVectorTable.ValueEquals(recovered, 1.0e-5));
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestCloneTable(ConcatVector[][][] factor3)
		{
			ConcatVectorTable concatVectorTable = ConvertArrayToVectorTable((ConcatVector[][][])factor3);
			ConcatVectorTable cloned = concatVectorTable.CloneTable();
			for (int i = 0; i < factor3.Length; i++)
			{
				for (int j = 0; j < factor3[0].Length; j++)
				{
					for (int k = 0; k < factor3[0][0].Length; k++)
					{
						NUnit.Framework.Assert.IsTrue(factor3[i][j][k].ValueEquals(cloned.GetAssignmentValue(new int[] { i, j, k }).Get(), 1.0e-5));
					}
				}
			}
			NUnit.Framework.Assert.IsTrue(concatVectorTable.ValueEquals(cloned, 1.0e-5));
		}

		public class FeatureFactorGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<ConcatVector[][][]>
		{
			public FeatureFactorGenerator(Type type)
				: base(type)
			{
			}

			public override ConcatVector[][][] Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				int l = sourceOfRandomness.NextInt(10) + 1;
				int m = sourceOfRandomness.NextInt(10) + 1;
				int n = sourceOfRandomness.NextInt(10) + 1;
				ConcatVector[][][] factor3 = new ConcatVector[l][][];
				for (int i = 0; i < factor3.Length; i++)
				{
					for (int j = 0; j < factor3[0].Length; j++)
					{
						for (int k = 0; k < factor3[0][0].Length; k++)
						{
							int numComponents = sourceOfRandomness.NextInt(7);
							ConcatVector v = new ConcatVector(numComponents);
							for (int x = 0; x < numComponents; x++)
							{
								if (sourceOfRandomness.NextBoolean())
								{
									v.SetSparseComponent(x, sourceOfRandomness.NextInt(32), sourceOfRandomness.NextDouble());
								}
								else
								{
									double[] val = new double[sourceOfRandomness.NextInt(12)];
									for (int y = 0; y < val.Length; y++)
									{
										val[y] = sourceOfRandomness.NextDouble();
									}
									v.SetDenseComponent(x, val);
								}
							}
							factor3[i][j][k] = v;
						}
					}
				}
				return factor3;
			}
		}
	}
}

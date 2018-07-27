using System;
using System.Collections.Generic;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;

using NUnit.Framework.Contrib.Theories;


namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 8/11/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Quickchecks a couple of pieces of functionality, but mostly the serialization and deserialization (basically the only
	/// non-trivial section).
	/// </author>
	public class GraphicalModelTest
	{
		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestProtoModel(GraphicalModel graphicalModel)
		{
			ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
			graphicalModel.WriteToStream(byteArrayOutputStream);
			byteArrayOutputStream.Close();
			byte[] bytes = byteArrayOutputStream.ToByteArray();
			ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(bytes);
			GraphicalModel recovered = GraphicalModel.ReadFromStream(byteArrayInputStream);
			NUnit.Framework.Assert.IsTrue(graphicalModel.ValueEquals(recovered, 1.0e-5));
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestClone(GraphicalModel graphicalModel)
		{
			GraphicalModel clone = graphicalModel.CloneModel();
			NUnit.Framework.Assert.IsTrue(graphicalModel.ValueEquals(clone, 1.0e-5));
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestGetVariableSizes(GraphicalModel graphicalModel)
		{
			int[] sizes = graphicalModel.GetVariableSizes();
			foreach (GraphicalModel.Factor f in graphicalModel.factors)
			{
				for (int i = 0; i < f.neigborIndices.Length; i++)
				{
					NUnit.Framework.Assert.AreEqual(f.featuresTable.GetDimensions()[i], sizes[f.neigborIndices[i]]);
				}
			}
		}

		public class GraphicalModelGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<GraphicalModel>
		{
			public GraphicalModelGenerator(Type type)
				: base(type)
			{
			}

			private IDictionary<string, string> GenerateMetaData(SourceOfRandomness sourceOfRandomness, IDictionary<string, string> metaData)
			{
				int numPairs = sourceOfRandomness.NextInt(9);
				for (int i = 0; i < numPairs; i++)
				{
					int key = sourceOfRandomness.NextInt();
					int value = sourceOfRandomness.NextInt();
					metaData["key:" + key] = "value:" + value;
				}
				return metaData;
			}

			public override GraphicalModel Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				GraphicalModel model = new GraphicalModel();
				// Create the variables and factors
				int[] variableSizes = new int[20];
				for (int i = 0; i < 20; i++)
				{
					variableSizes[i] = sourceOfRandomness.NextInt(1, 5);
				}
				int numFactors = sourceOfRandomness.NextInt(12);
				for (int i_1 = 0; i_1 < numFactors; i_1++)
				{
					int[] neighbors = new int[sourceOfRandomness.NextInt(1, 3)];
					int[] neighborSizes = new int[neighbors.Length];
					for (int j = 0; j < neighbors.Length; j++)
					{
						neighbors[j] = sourceOfRandomness.NextInt(20);
						neighborSizes[j] = variableSizes[neighbors[j]];
					}
					ConcatVectorTable table = new ConcatVectorTable(neighborSizes);
					foreach (int[] assignment in table)
					{
						int numComponents = sourceOfRandomness.NextInt(7);
						// Generate a vector
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
						// set vec in table
						table.SetAssignmentValue(assignment, null);
					}
					model.AddFactor(table, neighbors);
				}
				// Add metadata to the variables, factors, and model
				GenerateMetaData(sourceOfRandomness, model.GetModelMetaDataByReference());
				for (int i_2 = 0; i_2 < 20; i_2++)
				{
					GenerateMetaData(sourceOfRandomness, model.GetVariableMetaDataByReference(i_2));
				}
				foreach (GraphicalModel.Factor factor in model.factors)
				{
					GenerateMetaData(sourceOfRandomness, factor.GetMetaDataByReference());
				}
				return model;
			}
		}
	}
}

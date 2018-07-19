using System;
using System.Collections.Generic;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;
using Edu.Stanford.Nlp.Loglinear.Model;
using Java.Util;
using Java.Util.Stream;
using NUnit.Framework;
using NUnit.Framework.Contrib.Theories;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Inference
{
	/// <summary>Created on 8/11/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This is a really tricky thing to test in the quickcheck way, since we basically don't know what we want out of random
	/// graphs unless we run the routines that we're trying to test. The trick here is to implement exhaustive factor
	/// multiplication, which is normally super intractable but easy to get right, as ground truth.
	/// </author>
	public class CliqueTreeTest
	{
		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestCalculateMarginals(GraphicalModel model, ConcatVector weights)
		{
			CliqueTree inference = new CliqueTree(model, weights);
			// This is the basic check that inference works when you first construct the model
			CheckMarginalsAgainstBruteForce((GraphicalModel)model, (ConcatVector)weights, inference);
			// Now we go through several random mutations to the model, and check that everything is still consistent
			Random r = new Random();
			for (int i = 0; i < 10; i++)
			{
				RandomlyMutateGraphicalModel((GraphicalModel)model, r);
				CheckMarginalsAgainstBruteForce((GraphicalModel)model, (ConcatVector)weights, inference);
			}
		}

		private void RandomlyMutateGraphicalModel(GraphicalModel model, Random r)
		{
			if (r.NextBoolean() && model.factors.Count > 1)
			{
				// Remove one factor at random
				model.factors.Remove(Sharpen.Collections.ToArray(model.factors, new GraphicalModel.Factor[model.factors.Count])[r.NextInt(model.factors.Count)]);
			}
			else
			{
				// Add a simple binary factor, attaching a variable we haven't touched yet, but do observe, to an
				// existing variable. This represents the human observation operation in LENSE
				int maxVar = 0;
				int attachVar = -1;
				int attachVarSize = 0;
				foreach (GraphicalModel.Factor f in model.factors)
				{
					for (int j = 0; j < f.neigborIndices.Length; j++)
					{
						int k = f.neigborIndices[j];
						if (k > maxVar)
						{
							maxVar = k;
						}
						if (r.NextDouble() > 0.3 || attachVar == -1)
						{
							attachVar = k;
							attachVarSize = f.featuresTable.GetDimensions()[j];
						}
					}
				}
				int newVar = maxVar + 1;
				int newVarSize = 1 + r.NextInt(2);
				if (maxVar >= 8)
				{
					bool[] seenVariables = new bool[maxVar + 1];
					foreach (GraphicalModel.Factor f_1 in model.factors)
					{
						foreach (int n in f_1.neigborIndices)
						{
							seenVariables[n] = true;
						}
					}
					for (int j = 0; j < seenVariables.Length; j++)
					{
						if (!seenVariables[j])
						{
							newVar = j;
							break;
						}
					}
					// This means the model is already too gigantic to be tractable, so we don't add anything here
					if (newVar == maxVar + 1)
					{
						return;
					}
				}
				if (model.GetVariableMetaDataByReference(newVar).Contains(CliqueTree.VariableObservedValue))
				{
					int assignment = System.Convert.ToInt32(model.GetVariableMetaDataByReference(newVar)[CliqueTree.VariableObservedValue]);
					if (assignment >= newVarSize)
					{
						newVarSize = assignment + 1;
					}
				}
				GraphicalModel.Factor binary = model.AddFactor(new int[] { newVar, attachVar }, new int[] { newVarSize, attachVarSize }, null);
				// "Cook" the randomly generated feature vector thunks, so they don't change as we run the system
				foreach (int[] assignment_1 in binary.featuresTable)
				{
					ConcatVector randomlyGenerated = binary.featuresTable.GetAssignmentValue(assignment_1).Get();
					binary.featuresTable.SetAssignmentValue(assignment_1, null);
				}
			}
		}

		private void CheckMarginalsAgainstBruteForce(GraphicalModel model, ConcatVector weights, CliqueTree inference)
		{
			CliqueTree.MarginalResult result = inference.CalculateMarginals();
			double[][] marginals = result.marginals;
			ICollection<TableFactor> tableFactors = model.factors.Stream().Map(null).Collect(Collectors.ToSet());
			System.Diagnostics.Debug.Assert((tableFactors.Count == model.factors.Count));
			// this is the super slow but obviously correct way to get global marginals
			TableFactor bruteForce = null;
			foreach (TableFactor factor in tableFactors)
			{
				if (bruteForce == null)
				{
					bruteForce = factor;
				}
				else
				{
					bruteForce = bruteForce.Multiply(factor);
				}
			}
			if (bruteForce != null)
			{
				// observe out all variables that have been registered
				TableFactor observed = bruteForce;
				for (int i = 0; i < bruteForce.neighborIndices.Length; i++)
				{
					int n = bruteForce.neighborIndices[i];
					if (model.GetVariableMetaDataByReference(n).Contains(CliqueTree.VariableObservedValue))
					{
						int value = System.Convert.ToInt32(model.GetVariableMetaDataByReference(n)[CliqueTree.VariableObservedValue]);
						// Check that the marginals reflect the observation
						for (int j = 0; j < marginals[n].Length; j++)
						{
							NUnit.Framework.Assert.AreEqual(marginals[n][j], 1.0e-9, j == value ? 1.0 : 0.0);
						}
						if (observed.neighborIndices.Length > 1)
						{
							observed = observed.Observe(n, value);
						}
						else
						{
							// If we've observed everything, then just quit
							return;
						}
					}
				}
				bruteForce = observed;
				// Spot check each of the marginals in the brute force calculation
				double[][] bruteMarginals = bruteForce.GetSummedMarginals();
				int index = 0;
				foreach (int i_1 in bruteForce.neighborIndices)
				{
					bool isEqual = true;
					double[] brute = bruteMarginals[index];
					index++;
					System.Diagnostics.Debug.Assert((brute != null));
					System.Diagnostics.Debug.Assert((marginals[i_1] != null));
					for (int j = 0; j < brute.Length; j++)
					{
						if (double.IsNaN(brute[j]))
						{
							isEqual = false;
							break;
						}
						if (Math.Abs(brute[j] - marginals[i_1][j]) > 3.0e-2)
						{
							isEqual = false;
							break;
						}
					}
					if (!isEqual)
					{
						System.Console.Error.WriteLine("Arrays not equal! Variable " + i_1);
						System.Console.Error.WriteLine("\tGold: " + Arrays.ToString(brute));
						System.Console.Error.WriteLine("\tResult: " + Arrays.ToString(marginals[i_1]));
					}
					Assert.AssertArrayEquals(marginals[i_1], 3.0e-2, brute);
				}
				// Spot check the partition function
				double goldPartitionFunction = bruteForce.ValueSum();
				// Correct to within 3%
				NUnit.Framework.Assert.AreEqual(result.partitionFunction, goldPartitionFunction * 3.0e-2, goldPartitionFunction);
				// Check the joint marginals
				foreach (GraphicalModel.Factor f in model.factors)
				{
					NUnit.Framework.Assert.IsTrue(result.jointMarginals.Contains(f));
					TableFactor bruteForceJointMarginal = bruteForce;
					foreach (int n in bruteForce.neighborIndices)
					{
						foreach (int i_2 in f.neigborIndices)
						{
							if (i_2 == n)
							{
								goto outer_continue;
							}
						}
						if (bruteForceJointMarginal.neighborIndices.Length > 1)
						{
							bruteForceJointMarginal = bruteForceJointMarginal.SumOut(n);
						}
						else
						{
							int[] fixedAssignment = new int[f.neigborIndices.Length];
							for (int i_3 = 0; i_3 < fixedAssignment.Length; i_3++)
							{
								fixedAssignment[i_3] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(f.neigborIndices[i_3])[CliqueTree.VariableObservedValue]);
							}
							foreach (int[] assn in result.jointMarginals[f])
							{
								if (Arrays.Equals(assn, fixedAssignment))
								{
									NUnit.Framework.Assert.AreEqual(result.jointMarginals[f].GetAssignmentValue(assn), 1.0e-7, 1.0);
								}
								else
								{
									if (result.jointMarginals[f].GetAssignmentValue(assn) != 0)
									{
										TableFactor j = result.jointMarginals[f];
										foreach (int[] assignment in j)
										{
											System.Console.Error.WriteLine(Arrays.ToString(assignment) + ": " + j.GetAssignmentValue(assignment));
										}
									}
									NUnit.Framework.Assert.AreEqual(result.jointMarginals[f].GetAssignmentValue(assn), 1.0e-7, 0.0);
								}
							}
							goto marginals_continue;
						}
					}
outer_break: ;
					// Find the correspondence between the brute force joint marginal, which may be missing variables
					// because they were observed out of the table, and the output joint marginals, which are always an exact
					// match for the original factor
					int[] backPointers = new int[f.neigborIndices.Length];
					int[] observedValue = new int[f.neigborIndices.Length];
					for (int i_4 = 0; i_4 < backPointers.Length; i_4++)
					{
						if (model.GetVariableMetaDataByReference(f.neigborIndices[i_4]).Contains(CliqueTree.VariableObservedValue))
						{
							observedValue[i_4] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(f.neigborIndices[i_4])[CliqueTree.VariableObservedValue]);
							backPointers[i_4] = -1;
						}
						else
						{
							observedValue[i_4] = -1;
							backPointers[i_4] = -1;
							for (int j = 0; j < bruteForceJointMarginal.neighborIndices.Length; j++)
							{
								if (bruteForceJointMarginal.neighborIndices[j] == f.neigborIndices[i_4])
								{
									backPointers[i_4] = j;
								}
							}
							System.Diagnostics.Debug.Assert((backPointers[i_4] != -1));
						}
					}
					double sum = bruteForceJointMarginal.ValueSum();
					if (sum == 0.0)
					{
						sum = 1;
					}
					foreach (int[] assignment_1 in result.jointMarginals[f])
					{
						int[] bruteForceMarginalAssignment = new int[bruteForceJointMarginal.neighborIndices.Length];
						for (int i_2 = 0; i_2 < assignment_1.Length; i_2++)
						{
							if (backPointers[i_2] != -1)
							{
								bruteForceMarginalAssignment[backPointers[i_2]] = assignment_1[i_2];
							}
							else
							{
								// Make sure all assignments that don't square with observations get 0 weight
								System.Diagnostics.Debug.Assert((observedValue[i_2] != -1));
								if (assignment_1[i_2] != observedValue[i_2])
								{
									if (result.jointMarginals[f].GetAssignmentValue(assignment_1) != 0)
									{
										System.Console.Error.WriteLine("Joint marginals: " + Arrays.ToString(result.jointMarginals[f].neighborIndices));
										System.Console.Error.WriteLine("Assignment: " + Arrays.ToString(assignment_1));
										System.Console.Error.WriteLine("Observed Value: " + Arrays.ToString(observedValue));
										foreach (int[] assn in result.jointMarginals[f])
										{
											System.Console.Error.WriteLine("\t" + Arrays.ToString(assn) + ":" + result.jointMarginals[f].GetAssignmentValue(assn));
										}
									}
									NUnit.Framework.Assert.AreEqual(result.jointMarginals[f].GetAssignmentValue(assignment_1), 1.0e-7, 0.0);
									goto outer_continue;
								}
							}
						}
						NUnit.Framework.Assert.AreEqual(result.jointMarginals[f].GetAssignmentValue(assignment_1), 1.0e-3, bruteForceJointMarginal.GetAssignmentValue(bruteForceMarginalAssignment) / sum);
					}
outer_break: ;
				}
marginals_break: ;
			}
			else
			{
				foreach (double[] marginal in marginals)
				{
					foreach (double d in marginal)
					{
						NUnit.Framework.Assert.AreEqual(d, 3.0e-2, 1.0 / marginal.Length);
					}
				}
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestCalculateMap(GraphicalModel model, ConcatVector weights)
		{
			if (model.factors.Count == 0)
			{
				return;
			}
			CliqueTree inference = new CliqueTree(model, weights);
			// This is the basic check that inference works when you first construct the model
			CheckMAPAgainstBruteForce((GraphicalModel)model, (ConcatVector)weights, inference);
			// Now we go through several random mutations to the model, and check that everything is still consistent
			Random r = new Random();
			for (int i = 0; i < 10; i++)
			{
				RandomlyMutateGraphicalModel((GraphicalModel)model, r);
				CheckMAPAgainstBruteForce((GraphicalModel)model, (ConcatVector)weights, inference);
			}
		}

		public virtual void CheckMAPAgainstBruteForce(GraphicalModel model, ConcatVector weights, CliqueTree inference)
		{
			int[] map = inference.CalculateMAP();
			ICollection<TableFactor> tableFactors = model.factors.Stream().Map(null).Collect(Collectors.ToSet());
			// this is the super slow but obviously correct way to get global marginals
			TableFactor bruteForce = null;
			foreach (TableFactor factor in tableFactors)
			{
				if (bruteForce == null)
				{
					bruteForce = factor;
				}
				else
				{
					bruteForce = bruteForce.Multiply(factor);
				}
			}
			System.Diagnostics.Debug.Assert((bruteForce != null));
			// observe out all variables that have been registered
			TableFactor observed = bruteForce;
			foreach (int n in bruteForce.neighborIndices)
			{
				if (model.GetVariableMetaDataByReference(n).Contains(CliqueTree.VariableObservedValue))
				{
					int value = System.Convert.ToInt32(model.GetVariableMetaDataByReference(n)[CliqueTree.VariableObservedValue]);
					if (observed.neighborIndices.Length > 1)
					{
						observed = observed.Observe(n, value);
					}
					else
					{
						// If we've observed everything, then just quit
						return;
					}
				}
			}
			bruteForce = observed;
			int largestVariableNum = 0;
			foreach (GraphicalModel.Factor f in model.factors)
			{
				foreach (int i in f.neigborIndices)
				{
					if (i > largestVariableNum)
					{
						largestVariableNum = i;
					}
				}
			}
			// this is presented in true order, where 0 corresponds to var 0
			int[] mapValueAssignment = new int[largestVariableNum + 1];
			// this is kept in the order that the factor presents to us
			int[] highestValueAssignment = new int[bruteForce.neighborIndices.Length];
			foreach (int[] assignment in bruteForce)
			{
				if (bruteForce.GetAssignmentValue(assignment) > bruteForce.GetAssignmentValue(highestValueAssignment))
				{
					highestValueAssignment = assignment;
					for (int i = 0; i < assignment.Length; i++)
					{
						mapValueAssignment[bruteForce.neighborIndices[i]] = assignment[i];
					}
				}
			}
			int[] forcedAssignments = new int[largestVariableNum + 1];
			for (int i_1 = 0; i_1 < mapValueAssignment.Length; i_1++)
			{
				if (model.GetVariableMetaDataByReference(i_1).Contains(CliqueTree.VariableObservedValue))
				{
					mapValueAssignment[i_1] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(i_1)[CliqueTree.VariableObservedValue]);
					forcedAssignments[i_1] = mapValueAssignment[i_1];
				}
			}
			if (!Arrays.Equals(mapValueAssignment, map))
			{
				System.Console.Error.WriteLine("---");
				System.Console.Error.WriteLine("Relevant variables: " + Arrays.ToString(bruteForce.neighborIndices));
				System.Console.Error.WriteLine("Var Sizes: " + Arrays.ToString(bruteForce.GetDimensions()));
				System.Console.Error.WriteLine("MAP: " + Arrays.ToString(map));
				System.Console.Error.WriteLine("Brute force map: " + Arrays.ToString(mapValueAssignment));
				System.Console.Error.WriteLine("Forced assignments: " + Arrays.ToString(forcedAssignments));
			}
			foreach (int i_2 in bruteForce.neighborIndices)
			{
				// Only check defined variables
				NUnit.Framework.Assert.AreEqual(mapValueAssignment[i_2], map[i_2]);
			}
		}

		public const int ConcatVecComponents = 2;

		public const int ConcatVecComponentLength = 3;

		public class WeightsGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<ConcatVector>
		{
			public WeightsGenerator(Type type)
				: base(type)
			{
			}

			/////////////////////////////////////////////////////////////////////////////
			//
			// A copy of these generators exists in GradientSourceTest in the learning module. If any bug fixes are made here,
			// remember to update that code as well by copy-paste.
			//
			/////////////////////////////////////////////////////////////////////////////
			public override ConcatVector Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				ConcatVector v = new ConcatVector(ConcatVecComponents);
				for (int x = 0; x < ConcatVecComponents; x++)
				{
					if (sourceOfRandomness.NextBoolean())
					{
						v.SetSparseComponent(x, sourceOfRandomness.NextInt(ConcatVecComponentLength), sourceOfRandomness.NextDouble());
					}
					else
					{
						double[] val = new double[sourceOfRandomness.NextInt(ConcatVecComponentLength)];
						for (int y = 0; y < val.Length; y++)
						{
							val[y] = sourceOfRandomness.NextDouble();
						}
						v.SetDenseComponent(x, val);
					}
				}
				return v;
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
				// Create the variables and factors. These are deliberately tiny so that the brute force approach is tractable
				int[] variableSizes = new int[8];
				for (int i = 0; i < variableSizes.Length; i++)
				{
					variableSizes[i] = sourceOfRandomness.NextInt(1, 3);
				}
				// Traverse in a randomized BFS to ensure the generated graph is a tree
				if (sourceOfRandomness.NextBoolean())
				{
					GenerateCliques(variableSizes, new List<int>(), new HashSet<int>(), model, sourceOfRandomness);
				}
				else
				{
					// Or generate a linear chain CRF, because our random BFS doesn't generate these very often, and they're very
					// common in practice, so worth testing densely
					for (int i_1 = 0; i_1 < variableSizes.Length; i_1++)
					{
						// Add unary factor
						GraphicalModel.Factor unary = model.AddFactor(new int[] { i_1 }, new int[] { variableSizes[i_1] }, null);
						// "Cook" the randomly generated feature vector thunks, so they don't change as we run the system
						foreach (int[] assignment in unary.featuresTable)
						{
							ConcatVector randomlyGenerated = unary.featuresTable.GetAssignmentValue(assignment).Get();
							unary.featuresTable.SetAssignmentValue(assignment, null);
						}
						// Add binary factor
						if (i_1 < variableSizes.Length - 1)
						{
							GraphicalModel.Factor binary = model.AddFactor(new int[] { i_1, i_1 + 1 }, new int[] { variableSizes[i_1], variableSizes[i_1 + 1] }, null);
							// "Cook" the randomly generated feature vector thunks, so they don't change as we run the system
							foreach (int[] assignment_1 in binary.featuresTable)
							{
								ConcatVector randomlyGenerated = binary.featuresTable.GetAssignmentValue(assignment_1).Get();
								binary.featuresTable.SetAssignmentValue(assignment_1, null);
							}
						}
					}
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
				// Observe a few of the variables
				foreach (GraphicalModel.Factor f in model.factors)
				{
					for (int i_1 = 0; i_1 < f.neigborIndices.Length; i_1++)
					{
						if (sourceOfRandomness.NextDouble() > 0.8)
						{
							int obs = sourceOfRandomness.NextInt(f.featuresTable.GetDimensions()[i_1]);
							model.GetVariableMetaDataByReference(f.neigborIndices[i_1])[CliqueTree.VariableObservedValue] = string.Empty + obs;
						}
					}
				}
				return model;
			}

			private void GenerateCliques(int[] variableSizes, IList<int> startSet, ICollection<int> alreadyRepresented, GraphicalModel model, SourceOfRandomness randomness)
			{
				if (alreadyRepresented.Count == variableSizes.Length)
				{
					return;
				}
				// Generate the clique variable set
				IList<int> cliqueContents = new List<int>();
				Sharpen.Collections.AddAll(cliqueContents, startSet);
				Sharpen.Collections.AddAll(alreadyRepresented, startSet);
				while (true)
				{
					if (alreadyRepresented.Count == variableSizes.Length)
					{
						break;
					}
					if (cliqueContents.Count == 0 || randomness.NextDouble(0, 1) < 0.7)
					{
						int gen;
						do
						{
							gen = randomness.NextInt(variableSizes.Length);
						}
						while (alreadyRepresented.Contains(gen));
						alreadyRepresented.Add(gen);
						cliqueContents.Add(gen);
					}
					else
					{
						break;
					}
				}
				// Create the actual table
				int[] neighbors = new int[cliqueContents.Count];
				int[] neighborSizes = new int[neighbors.Length];
				for (int j = 0; j < neighbors.Length; j++)
				{
					neighbors[j] = cliqueContents[j];
					neighborSizes[j] = variableSizes[neighbors[j]];
				}
				ConcatVectorTable table = new ConcatVectorTable(neighborSizes);
				foreach (int[] assignment in table)
				{
					// Generate a vector
					ConcatVector v = new ConcatVector(ConcatVecComponents);
					for (int x = 0; x < ConcatVecComponents; x++)
					{
						if (randomness.NextBoolean())
						{
							v.SetSparseComponent(x, randomness.NextInt(32), randomness.NextDouble());
						}
						else
						{
							double[] val = new double[randomness.NextInt(12)];
							for (int y = 0; y < val.Length; y++)
							{
								val[y] = randomness.NextDouble();
							}
							v.SetDenseComponent(x, val);
						}
					}
					// set vec in table
					table.SetAssignmentValue(assignment, null);
				}
				model.AddFactor(table, neighbors);
				// Pick the number of children
				IList<int> availableVariables = new List<int>();
				Sharpen.Collections.AddAll(availableVariables, cliqueContents);
				availableVariables.RemoveAll(startSet);
				int numChildren = randomness.NextInt(0, availableVariables.Count);
				if (numChildren == 0)
				{
					return;
				}
				IList<IList<int>> children = new List<IList<int>>();
				for (int i = 0; i < numChildren; i++)
				{
					children.Add(new List<int>());
				}
				// divide up the shared variables across the children
				int cursor = 0;
				while (true)
				{
					if (availableVariables.Count == 0)
					{
						break;
					}
					if (children[cursor].Count == 0 || randomness.NextBoolean())
					{
						int gen = randomness.NextInt(availableVariables.Count);
						children[cursor].Add(availableVariables[gen]);
						availableVariables.Remove(availableVariables[gen]);
					}
					else
					{
						break;
					}
					cursor = (cursor + 1) % numChildren;
				}
				foreach (IList<int> shared1 in children)
				{
					foreach (int i_1 in shared1)
					{
						foreach (IList<int> shared2 in children)
						{
							System.Diagnostics.Debug.Assert((shared1 == shared2 || !shared2.Contains(i_1)));
						}
					}
				}
				foreach (IList<int> shared in children)
				{
					if (shared.Count > 0)
					{
						GenerateCliques(variableSizes, shared, alreadyRepresented, model, randomness);
					}
				}
			}
		}
	}
}

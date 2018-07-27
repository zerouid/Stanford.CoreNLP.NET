using System;
using System.Collections.Generic;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;
using Edu.Stanford.Nlp.Loglinear.Inference;
using Edu.Stanford.Nlp.Loglinear.Model;


using NUnit.Framework.Contrib.Theories;


namespace Edu.Stanford.Nlp.Loglinear.Learning
{
	/// <summary>Created on 8/24/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Uses the definition of the derivative to verify that the calculated gradients are approximately correct.
	/// </author>
	public class LogLikelihoodFunctionTest
	{
		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestGetSummaryForInstance(GraphicalModel[] dataset, ConcatVector weights)
		{
			LogLikelihoodDifferentiableFunction fn = new LogLikelihoodDifferentiableFunction();
			foreach (GraphicalModel model in dataset)
			{
				double goldLogLikelihood = LogLikelihood(model, (ConcatVector)weights);
				ConcatVector goldGradient = DefinitionOfDerivative(model, (ConcatVector)weights);
				ConcatVector gradient = new ConcatVector(0);
				double logLikelihood = fn.GetSummaryForInstance(model, (ConcatVector)weights, gradient);
				NUnit.Framework.Assert.AreEqual(logLikelihood, Math.Max(1.0e-3, goldLogLikelihood * 1.0e-2), goldLogLikelihood);
				// Our check for gradient similarity involves distance between endpoints of vectors, instead of elementwise
				// similarity, b/c it can be controlled as a percentage
				ConcatVector difference = goldGradient.DeepClone();
				difference.AddVectorInPlace(gradient, -1);
				double distance = Math.Sqrt(difference.DotProduct(difference));
				// The tolerance here is pretty large, since the gold gradient is computed approximately
				// 5% still tells us whether everything is working or not though
				if (distance > 5.0e-2)
				{
					System.Console.Error.WriteLine("Definitional and calculated gradient differ!");
					System.Console.Error.WriteLine("Definition approx: " + goldGradient);
					System.Console.Error.WriteLine("Calculated: " + gradient);
				}
				NUnit.Framework.Assert.AreEqual(distance, 5.0e-2, 0.0);
			}
		}

		/// <summary>The slowest, but obviously correct way to get log likelihood.</summary>
		/// <remarks>
		/// The slowest, but obviously correct way to get log likelihood. We've already tested the partition function in
		/// the CliqueTreeTest, but in the interest of making things as different as possible to catch any lurking bugs or
		/// numerical issues, we use the brute force approach here.
		/// </remarks>
		/// <param name="model">the model to get the log-likelihood of, assumes labels for assignments</param>
		/// <param name="weights">the weights to get the log-likelihood at</param>
		/// <returns>the log-likelihood</returns>
		private double LogLikelihood(GraphicalModel model, ConcatVector weights)
		{
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
						return 0.0;
					}
				}
			}
			bruteForce = observed;
			// Now we can get a partition function
			double partitionFunction = bruteForce.ValueSum();
			// For now, we'll assume that all the variables are given for training. EM is another problem altogether
			int[] assignment = new int[bruteForce.neighborIndices.Length];
			for (int i = 0; i < assignment.Length; i++)
			{
				System.Diagnostics.Debug.Assert((!model.GetVariableMetaDataByReference(bruteForce.neighborIndices[i]).Contains(CliqueTree.VariableObservedValue)));
				assignment[i] = System.Convert.ToInt32(model.GetVariableMetaDataByReference(bruteForce.neighborIndices[i])[LogLikelihoodDifferentiableFunction.VariableTrainingValue]);
			}
			if (bruteForce.GetAssignmentValue(assignment) == 0 || partitionFunction == 0)
			{
				return double.NegativeInfinity;
			}
			return Math.Log(bruteForce.GetAssignmentValue(assignment)) - Math.Log(partitionFunction);
		}

		/// <summary>
		/// Slowest possible way to calculate a derivative for a model: exhaustive definitional calculation, using the super
		/// slow logLikelihood function from this test suite.
		/// </summary>
		/// <param name="model">the model the get the derivative for</param>
		/// <param name="weights">the weights to get the derivative at</param>
		/// <returns>the derivative of the log likelihood with respect to the weights</returns>
		private ConcatVector DefinitionOfDerivative(GraphicalModel model, ConcatVector weights)
		{
			double epsilon = 1.0e-7;
			ConcatVector goldGradient = new ConcatVector(ConcatVecComponents);
			for (int i = 0; i < ConcatVecComponents; i++)
			{
				double[] component = new double[ConcatVecComponentLength];
				for (int j = 0; j < ConcatVecComponentLength; j++)
				{
					// Create a unit vector pointing in the direction of this element of this component
					ConcatVector unitVectorIJ = new ConcatVector(ConcatVecComponents);
					unitVectorIJ.SetSparseComponent(i, j, 1.0);
					// Create a +eps weight vector
					ConcatVector weightsPlusEpsilon = weights.DeepClone();
					weightsPlusEpsilon.AddVectorInPlace(unitVectorIJ, epsilon);
					// Create a -eps weight vector
					ConcatVector weightsMinusEpsilon = weights.DeepClone();
					weightsMinusEpsilon.AddVectorInPlace(unitVectorIJ, -epsilon);
					// Use the definition (f(x+eps) - f(x-eps))/(2*eps)
					component[j] = (LogLikelihood(model, weightsPlusEpsilon) - LogLikelihood(model, weightsMinusEpsilon)) / (2 * epsilon);
					// If we encounter an impossible assignment, logLikelihood will return negative infinity, which will
					// screw with the definitional calculation
					if (double.IsNaN(component[j]))
					{
						component[j] = 0.0;
					}
				}
				goldGradient.SetDenseComponent(i, component);
			}
			return goldGradient;
		}

		public class GraphicalModelDatasetGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<GraphicalModel[]>
		{
			internal LogLikelihoodFunctionTest.GraphicalModelGenerator modelGenerator = new LogLikelihoodFunctionTest.GraphicalModelGenerator(typeof(GraphicalModel));

			public GraphicalModelDatasetGenerator(Type type)
				: base(type)
			{
			}

			public override GraphicalModel[] Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				GraphicalModel[] dataset = new GraphicalModel[sourceOfRandomness.NextInt(1, 10)];
				for (int i = 0; i < dataset.Length; i++)
				{
					dataset[i] = modelGenerator.Generate(sourceOfRandomness, generationStatus);
					foreach (GraphicalModel.Factor f in dataset[i].factors)
					{
						for (int j = 0; j < f.neigborIndices.Length; j++)
						{
							int n = f.neigborIndices[j];
							int dim = f.featuresTable.GetDimensions()[j];
							dataset[i].GetVariableMetaDataByReference(n)[LogLikelihoodDifferentiableFunction.VariableTrainingValue] = string.Empty + sourceOfRandomness.NextInt(dim);
						}
					}
				}
				return dataset;
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
			// These generators COPIED DIRECTLY FROM CliqueTreeTest in the inference module.
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
				GenerateCliques(variableSizes, new List<int>(), new HashSet<int>(), model, sourceOfRandomness);
				// Add metadata to the variables, factors, and model
				GenerateMetaData(sourceOfRandomness, model.GetModelMetaDataByReference());
				for (int i_1 = 0; i_1 < 20; i_1++)
				{
					GenerateMetaData(sourceOfRandomness, model.GetVariableMetaDataByReference(i_1));
				}
				foreach (GraphicalModel.Factor factor in model.factors)
				{
					GenerateMetaData(sourceOfRandomness, factor.GetMetaDataByReference());
				}
				// Observe a few of the variables
				foreach (GraphicalModel.Factor f in model.factors)
				{
					for (int i_2 = 0; i_2 < f.neigborIndices.Length; i_2++)
					{
						if (sourceOfRandomness.NextDouble() > 0.8)
						{
							int obs = sourceOfRandomness.NextInt(f.featuresTable.GetDimensions()[i_2]);
							model.GetVariableMetaDataByReference(f.neigborIndices[i_2])[CliqueTree.VariableObservedValue] = string.Empty + obs;
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
							v.SetSparseComponent(x, randomness.NextInt(ConcatVecComponentLength), randomness.NextDouble());
						}
						else
						{
							double[] val = new double[randomness.NextInt(ConcatVecComponentLength)];
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

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
	/// <summary>Created on 8/12/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Tries to quickcheck our factor functions, as well as unit test for documentation and simple verification.
	/// </author>
	public class TableFactorTest
	{
		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestConstructWithObservations(TableFactorTest.PartiallyObservedConstructorData data, ConcatVector weights)
		{
			int[] obsArray = new int[9];
			for (int i = 0; i < obsArray.Length; i++)
			{
				obsArray[i] = -1;
			}
			for (int i_1 = 0; i_1 < data.observations.Length; i_1++)
			{
				obsArray[data.factor.neigborIndices[i_1]] = data.observations[i_1];
			}
			TableFactor normalObservations = new TableFactor(weights, data.factor);
			for (int i_2 = 0; i_2 < obsArray.Length; i_2++)
			{
				if (obsArray[i_2] != -1)
				{
					normalObservations = normalObservations.Observe(i_2, obsArray[i_2]);
				}
			}
			TableFactor constructedObservations = new TableFactor(weights, data.factor, data.observations);
			Assert.AssertArrayEquals(normalObservations.neighborIndices, constructedObservations.neighborIndices);
			foreach (int[] assn in normalObservations)
			{
				NUnit.Framework.Assert.AreEqual(constructedObservations.GetAssignmentValue(assn), 1.0e-9, normalObservations.GetAssignmentValue(assn));
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestObserve(TableFactor factor, int observe, int value)
		{
			if (!Arrays.Stream(factor.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(observe))
			{
				return;
			}
			if (factor.neighborIndices.Length == 1)
			{
				return;
			}
			TableFactor observedOut = factor.Observe((int)observe, (int)value);
			int observeIndex = -1;
			for (int i = 0; i < factor.neighborIndices.Length; i++)
			{
				if (factor.neighborIndices[i] == observe)
				{
					observeIndex = i;
				}
			}
			foreach (int[] assignment in factor)
			{
				if (assignment[observeIndex] == value)
				{
					NUnit.Framework.Assert.AreEqual(observedOut.GetAssignmentValue(SubsetAssignment(assignment, (TableFactor)factor, observedOut)), 1.0e-7, factor.GetAssignmentValue(assignment));
				}
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestGetMaxedMarginals(TableFactor factor, int marginalizeTo)
		{
			if (!Arrays.Stream(factor.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(marginalizeTo))
			{
				return;
			}
			int indexOfVariable = -1;
			for (int i = 0; i < factor.neighborIndices.Length; i++)
			{
				if (factor.neighborIndices[i] == marginalizeTo)
				{
					indexOfVariable = i;
					break;
				}
			}
			Assume.AssumeTrue(indexOfVariable > -1);
			double[] gold = new double[factor.GetDimensions()[indexOfVariable]];
			for (int i_1 = 0; i_1 < gold.Length; i_1++)
			{
				gold[i_1] = double.NegativeInfinity;
			}
			foreach (int[] assignment in factor)
			{
				gold[assignment[indexOfVariable]] = Math.Max(gold[assignment[indexOfVariable]], factor.GetAssignmentValue(assignment));
			}
			Normalize(gold);
			Assert.AssertArrayEquals(factor.GetMaxedMarginals()[indexOfVariable], 1.0e-5, gold);
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestGetSummedMarginals(TableFactor factor, int marginalizeTo)
		{
			if (!Arrays.Stream(factor.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(marginalizeTo))
			{
				return;
			}
			int indexOfVariable = -1;
			for (int i = 0; i < factor.neighborIndices.Length; i++)
			{
				if (factor.neighborIndices[i] == marginalizeTo)
				{
					indexOfVariable = i;
					break;
				}
			}
			Assume.AssumeTrue(indexOfVariable > -1);
			double[] gold = new double[factor.GetDimensions()[indexOfVariable]];
			foreach (int[] assignment in factor)
			{
				gold[assignment[indexOfVariable]] = gold[assignment[indexOfVariable]] + factor.GetAssignmentValue(assignment);
			}
			Normalize(gold);
			Assert.AssertArrayEquals(factor.GetSummedMarginals()[indexOfVariable], 1.0e-5, gold);
		}

		private void Normalize(double[] arr)
		{
			double sum = 0;
			foreach (double d in arr)
			{
				sum += d;
			}
			if (sum == 0)
			{
				for (int i = 0; i < arr.Length; i++)
				{
					arr[i] = 1.0 / arr.Length;
				}
			}
			else
			{
				for (int i = 0; i < arr.Length; i++)
				{
					arr[i] = arr[i] / sum;
				}
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestValueSum(TableFactor factor)
		{
			double sum = 0.0;
			foreach (int[] assignment in factor)
			{
				sum += factor.GetAssignmentValue(assignment);
			}
			NUnit.Framework.Assert.AreEqual(factor.ValueSum(), 1.0e-5, sum);
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestMaxOut(TableFactor factor, int marginalize)
		{
			if (!Arrays.Stream(factor.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(marginalize))
			{
				return;
			}
			if (factor.neighborIndices.Length <= 1)
			{
				return;
			}
			TableFactor maxedOut = factor.MaxOut((int)marginalize);
			NUnit.Framework.Assert.AreEqual(factor.neighborIndices.Length - 1, maxedOut.neighborIndices.Length);
			NUnit.Framework.Assert.IsTrue(!Arrays.Stream(maxedOut.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(marginalize));
			foreach (int[] assignment in factor)
			{
				NUnit.Framework.Assert.IsTrue(factor.GetAssignmentValue(assignment) >= double.NegativeInfinity);
				NUnit.Framework.Assert.IsTrue(factor.GetAssignmentValue(assignment) <= maxedOut.GetAssignmentValue(SubsetAssignment(assignment, (TableFactor)factor, maxedOut)));
			}
			IDictionary<IList<int>, IList<int[]>> subsetToSuperset = SubsetToSupersetAssignments((TableFactor)factor, maxedOut);
			foreach (IList<int> subsetAssignmentList in subsetToSuperset.Keys)
			{
				double max = double.NegativeInfinity;
				foreach (int[] supersetAssignment in subsetToSuperset[subsetAssignmentList])
				{
					max = Math.Max(max, factor.GetAssignmentValue(supersetAssignment));
				}
				int[] subsetAssignment = new int[subsetAssignmentList.Count];
				for (int i = 0; i < subsetAssignment.Length; i++)
				{
					subsetAssignment[i] = subsetAssignmentList[i];
				}
				NUnit.Framework.Assert.AreEqual(maxedOut.GetAssignmentValue(subsetAssignment), 1.0e-5, max);
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestSumOut(TableFactor factor, int marginalize)
		{
			if (!Arrays.Stream(factor.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(marginalize))
			{
				return;
			}
			if (factor.neighborIndices.Length <= 1)
			{
				return;
			}
			TableFactor summedOut = factor.SumOut((int)marginalize);
			NUnit.Framework.Assert.AreEqual(factor.neighborIndices.Length - 1, summedOut.neighborIndices.Length);
			NUnit.Framework.Assert.IsTrue(!Arrays.Stream(summedOut.neighborIndices).Boxed().Collect(Collectors.ToSet()).Contains(marginalize));
			IDictionary<IList<int>, IList<int[]>> subsetToSuperset = SubsetToSupersetAssignments((TableFactor)factor, summedOut);
			foreach (IList<int> subsetAssignmentList in subsetToSuperset.Keys)
			{
				double sum = 0.0;
				foreach (int[] supersetAssignment in subsetToSuperset[subsetAssignmentList])
				{
					sum += factor.GetAssignmentValue(supersetAssignment);
				}
				int[] subsetAssignment = new int[subsetAssignmentList.Count];
				for (int i = 0; i < subsetAssignment.Length; i++)
				{
					subsetAssignment[i] = subsetAssignmentList[i];
				}
				NUnit.Framework.Assert.AreEqual(summedOut.GetAssignmentValue(subsetAssignment), 1.0e-5, sum);
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestMultiply(TableFactor factor1, TableFactor factor2)
		{
			TableFactor result = factor1.Multiply((TableFactor)factor2);
			foreach (int[] assignment in result)
			{
				double factor1Value = factor1.GetAssignmentValue(SubsetAssignment(assignment, result, (TableFactor)factor1));
				double factor2Value = factor2.GetAssignmentValue(SubsetAssignment(assignment, result, (TableFactor)factor2));
				NUnit.Framework.Assert.AreEqual(result.GetAssignmentValue(assignment), 1.0e-5, factor1Value * factor2Value);
			}
			// Check for no duplication
			for (int i = 0; i < result.neighborIndices.Length; i++)
			{
				for (int j = 0; j < result.neighborIndices.Length; j++)
				{
					if (i == j)
					{
						continue;
					}
					Assert.AssertNotEquals(result.neighborIndices[i], result.neighborIndices[j]);
				}
			}
		}

		public static int[] variableSizes = new int[] { 2, 4, 2, 3 };

		public class TableFactorGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<TableFactor>
		{
			public TableFactorGenerator(Type type)
				: base(type)
			{
			}

			public override TableFactor Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				int numNeighbors = sourceOfRandomness.NextInt(1, 3);
				int[] neighbors = new int[numNeighbors];
				int[] dimensions = new int[numNeighbors];
				ICollection<int> usedNeighbors = new HashSet<int>();
				for (int i = 0; i < neighbors.Length; i++)
				{
					while (true)
					{
						int neighbor = sourceOfRandomness.NextInt(0, 3);
						if (!usedNeighbors.Contains(neighbor))
						{
							usedNeighbors.Add(neighbor);
							neighbors[i] = neighbor;
							dimensions[i] = variableSizes[neighbor];
							break;
						}
					}
				}
				// Make sure we get some all-0 factor tables
				double multiple = sourceOfRandomness.NextDouble();
				TableFactor factor = new TableFactor(neighbors, dimensions);
				foreach (int[] assignment in factor)
				{
					factor.SetAssignmentValue(assignment, multiple * sourceOfRandomness.NextDouble());
				}
				return factor;
			}
		}

		public class ConcatVectorGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<ConcatVector>
		{
			public ConcatVectorGenerator(Type type)
				: base(type)
			{
			}

			public override ConcatVector Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				ConcatVector vec = new ConcatVector(1);
				double[] d = new double[20];
				for (int i = 0; i < d.Length; i++)
				{
					d[i] = sourceOfRandomness.NextDouble();
				}
				vec.SetDenseComponent(0, d);
				return vec;
			}
		}

		private class PartiallyObservedConstructorData
		{
			public GraphicalModel.Factor factor;

			public int[] observations;
		}

		public class PartiallyObservedConstructorDataGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<TableFactorTest.PartiallyObservedConstructorData>
		{
			public PartiallyObservedConstructorDataGenerator(Type type)
				: base(type)
			{
			}

			public override TableFactorTest.PartiallyObservedConstructorData Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				int len = sourceOfRandomness.NextInt(1, 4);
				ICollection<int> taken = new HashSet<int>();
				int[] neighborIndices = new int[len];
				int[] dimensions = new int[len];
				int[] observations = new int[len];
				int numObserved = 0;
				for (int i = 0; i < len; i++)
				{
					int j = sourceOfRandomness.NextInt(8);
					while (taken.Contains(j))
					{
						j = sourceOfRandomness.NextInt(8);
					}
					taken.Add(j);
					neighborIndices[i] = j;
					dimensions[i] = sourceOfRandomness.NextInt(1, 3);
					if (sourceOfRandomness.NextBoolean() && numObserved + 1 < dimensions.Length)
					{
						observations[i] = sourceOfRandomness.NextInt(dimensions[i]);
						numObserved++;
					}
					else
					{
						observations[i] = -1;
					}
				}
				ConcatVectorTable t = new ConcatVectorTable(dimensions);
				TableFactorTest.ConcatVectorGenerator gen = new TableFactorTest.ConcatVectorGenerator(typeof(ConcatVector));
				foreach (int[] assn in t)
				{
					ConcatVector vec = gen.Generate(sourceOfRandomness, generationStatus);
					t.SetAssignmentValue(assn, null);
				}
				TableFactorTest.PartiallyObservedConstructorData data = new TableFactorTest.PartiallyObservedConstructorData();
				data.factor = new GraphicalModel.Factor(t, neighborIndices);
				data.observations = observations;
				return data;
			}
		}

		/// <summary>Takes a full assignment from a superset factor, and figures out how to map it into a subset factor.</summary>
		/// <remarks>
		/// Takes a full assignment from a superset factor, and figures out how to map it into a subset factor. This is very
		/// useful for testing that functional properties are not violated across both product and marginalization steps.
		/// </remarks>
		/// <param name="supersetAssignment">the assignment in the superset factor</param>
		/// <param name="superset">the superset factor, containing the variables from the subset</param>
		/// <param name="subset">the subset factor, containing some of the variables found in the superset</param>
		/// <returns>an assignment into the subset factor</returns>
		private int[] SubsetAssignment(int[] supersetAssignment, TableFactor superset, TableFactor subset)
		{
			int[] subsetAssignment = new int[subset.neighborIndices.Length];
			for (int i = 0; i < subset.neighborIndices.Length; i++)
			{
				int var = subset.neighborIndices[i];
				subsetAssignment[i] = -1;
				for (int j = 0; j < superset.neighborIndices.Length; j++)
				{
					if (superset.neighborIndices[j] == var)
					{
						subsetAssignment[i] = supersetAssignment[j];
						break;
					}
				}
				System.Diagnostics.Debug.Assert((subsetAssignment[i] != -1));
			}
			return subsetAssignment;
		}

		/// <summary>Convenience function to construct a subset to superset assignment map.</summary>
		/// <remarks>
		/// Convenience function to construct a subset to superset assignment map. Each subset assignment will be mapping
		/// to a large number of superset assignments.
		/// </remarks>
		/// <param name="superset">the superset factor to map to</param>
		/// <param name="subset">the subset factor to map from</param>
		/// <returns>a map from subset assignment to list of superset assignment</returns>
		private IDictionary<IList<int>, IList<int[]>> SubsetToSupersetAssignments(TableFactor superset, TableFactor subset)
		{
			IDictionary<IList<int>, IList<int[]>> subsetToSupersets = new Dictionary<IList<int>, IList<int[]>>();
			foreach (int[] assignment in subset)
			{
				IList<int> subsetAssignmentList = Arrays.Stream(assignment).Boxed().Collect(Collectors.ToList());
				IList<int[]> supersetAssignments = new List<int[]>();
				foreach (int[] supersetAssignment in superset)
				{
					if (Arrays.Equals(assignment, SubsetAssignment(supersetAssignment, superset, subset)))
					{
						supersetAssignments.Add(supersetAssignment);
					}
				}
				subsetToSupersets[subsetAssignmentList] = supersetAssignments;
			}
			return subsetToSupersets;
		}
	}
}

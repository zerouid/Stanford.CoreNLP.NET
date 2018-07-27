using System;
using System.Collections.Generic;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;


using NUnit.Framework;
using NUnit.Framework.Contrib.Theories;


namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 8/12/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// Quickchecking NDArray means hammering on the two bits that are important: assignment iterator, and assignment itself.
	/// </author>
	public class NDArrayTest
	{
		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestAssignmentsIterator(NDArrayTest.NDArrayWithGold<double> testPair)
		{
			ICollection<IList<int>> assignmentSet = new HashSet<IList<int>>();
			foreach (int[] assignment in testPair.gold.Keys)
			{
				assignmentSet.Add(Arrays.Stream(assignment).Boxed().Collect(Collectors.ToList()));
			}
			foreach (int[] assignment_1 in testPair.array)
			{
				IList<int> l = new List<int>();
				foreach (int i in assignment_1)
				{
					l.Add(i);
				}
				NUnit.Framework.Assert.IsTrue(assignmentSet.Contains(l));
				assignmentSet.Remove(l);
			}
			NUnit.Framework.Assert.IsTrue(assignmentSet.IsEmpty());
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestReadWrite(NDArrayTest.NDArrayWithGold<double> testPair)
		{
			foreach (int[] assignment in testPair.gold.Keys)
			{
				NUnit.Framework.Assert.AreEqual(testPair.array.GetAssignmentValue(assignment), 1.0e-5, testPair.gold[assignment]);
			}
		}

		/// <exception cref="System.Exception"/>
		[Theory]
		public virtual void TestClone(NDArrayTest.NDArrayWithGold<double> testPair)
		{
			NDArray<double> clone = testPair.array.CloneArray();
			foreach (int[] assignment in testPair.gold.Keys)
			{
				NUnit.Framework.Assert.AreEqual(clone.GetAssignmentValue(assignment), 1.0e-5, testPair.gold[assignment]);
			}
		}

		public class NDArrayWithGold<T>
		{
			public NDArray<T> array;

			public IDictionary<int[], T> gold = new Dictionary<int[], T>();
		}

		public class NDArrayGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<NDArrayTest.NDArrayWithGold<double>>
		{
			public NDArrayGenerator(Type type)
				: base(type)
			{
			}

			public override NDArrayTest.NDArrayWithGold<double> Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				NDArrayTest.NDArrayWithGold<double> testPair = new NDArrayTest.NDArrayWithGold<double>();
				int numDimensions = sourceOfRandomness.NextInt(1, 5);
				int[] dimensions = new int[numDimensions];
				for (int i = 0; i < dimensions.Length; i++)
				{
					dimensions[i] = sourceOfRandomness.NextInt(1, 4);
				}
				testPair.array = new NDArray<double>(dimensions);
				RecursivelyFillArray(new List<int>(), testPair, sourceOfRandomness);
				return testPair;
			}

			private static void RecursivelyFillArray(IList<int> assignmentSoFar, NDArrayTest.NDArrayWithGold<double> testPair, SourceOfRandomness sourceOfRandomness)
			{
				if (assignmentSoFar.Count == testPair.array.GetDimensions().Length)
				{
					int[] arr = new int[assignmentSoFar.Count];
					for (int i = 0; i < arr.Length; i++)
					{
						arr[i] = assignmentSoFar[i];
					}
					double value = sourceOfRandomness.NextDouble();
					testPair.array.SetAssignmentValue(arr, value);
					testPair.gold[arr] = value;
				}
				else
				{
					for (int i = 0; i < testPair.array.GetDimensions()[assignmentSoFar.Count]; i++)
					{
						IList<int> newList = new List<int>();
						Sharpen.Collections.AddAll(newList, assignmentSoFar);
						newList.Add(i);
						RecursivelyFillArray(newList, testPair, sourceOfRandomness);
					}
				}
			}
		}
	}
}

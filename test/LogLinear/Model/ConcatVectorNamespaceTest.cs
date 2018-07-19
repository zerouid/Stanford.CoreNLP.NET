using System;
using System.Collections.Generic;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;
using NUnit.Framework.Contrib.Theories;
using Sharpen;

namespace Edu.Stanford.Nlp.Loglinear.Model
{
	/// <summary>Created on 10/20/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This checks the coherence of the ConcatVectorNamespace approach against the basic ConcatVector approach, using a cute
	/// trick where we map random ints as "feature names", and double check that the output is always the same.
	/// </author>
	public class ConcatVectorNamespaceTest
	{
		[Theory]
		public virtual void TestResizeOnSetComponent(IDictionary<int, int> featureMap1, IDictionary<int, int> featureMap2)
		{
			ConcatVectorNamespace @namespace = new ConcatVectorNamespace();
			ConcatVector namespace1 = ToNamespaceVector(@namespace, (IDictionary<int, int>)featureMap1);
			ConcatVector namespace2 = ToNamespaceVector(@namespace, (IDictionary<int, int>)featureMap2);
			ConcatVector regular1 = ToVector((IDictionary<int, int>)featureMap1);
			ConcatVector regular2 = ToVector((IDictionary<int, int>)featureMap2);
			NUnit.Framework.Assert.AreEqual(namespace1.DotProduct(namespace2), 1.0e-5, regular1.DotProduct(regular2));
			ConcatVector namespaceSum = namespace1.DeepClone();
			namespaceSum.AddVectorInPlace(namespace2, 1.0);
			ConcatVector regularSum = regular1.DeepClone();
			regularSum.AddVectorInPlace(regular2, 1.0);
			NUnit.Framework.Assert.AreEqual(namespace1.DotProduct(namespaceSum), 1.0e-5, regular1.DotProduct(regularSum));
			NUnit.Framework.Assert.AreEqual(namespaceSum.DotProduct(namespace2), 1.0e-5, regularSum.DotProduct(regular2));
		}

		public virtual ConcatVector ToNamespaceVector(ConcatVectorNamespace @namespace, IDictionary<int, int> featureMap)
		{
			ConcatVector newVector = @namespace.NewVector();
			foreach (int i in featureMap.Keys)
			{
				string feature = "feat" + i;
				string sparse = "index" + featureMap[i];
				@namespace.SetSparseFeature(newVector, feature, sparse, 1.0);
			}
			return newVector;
		}

		public virtual ConcatVector ToVector(IDictionary<int, int> featureMap)
		{
			ConcatVector vector = new ConcatVector(20);
			foreach (int i in featureMap.Keys)
			{
				vector.SetSparseComponent(i, featureMap[i], 1.0);
			}
			return vector;
		}

		public class MapGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<IDictionary<int, int>>
		{
			public MapGenerator(Type type)
				: base(type)
			{
			}

			public override IDictionary<int, int> Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				int numFeatures = sourceOfRandomness.NextInt(1, 15);
				IDictionary<int, int> featureMap = new Dictionary<int, int>();
				for (int i = 0; i < numFeatures; i++)
				{
					int featureValue = sourceOfRandomness.NextInt(20);
					while (featureMap.Contains(featureValue))
					{
						featureValue = sourceOfRandomness.NextInt(20);
					}
					featureMap[featureValue] = sourceOfRandomness.NextInt(2);
				}
				return featureMap;
			}
		}
	}
}

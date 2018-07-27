using System;
using Com.Pholser.Junit.Quickcheck.Generator;
using Com.Pholser.Junit.Quickcheck.Random;
using Edu.Stanford.Nlp.Loglinear.Model;

using NUnit.Framework.Contrib.Theories;


namespace Edu.Stanford.Nlp.Loglinear.Storage
{
	/// <summary>Created on 10/17/15.</summary>
	/// <author>
	/// keenon
	/// <p>
	/// This just double checks that we can write and read these model batches without loss.
	/// </author>
	public class ModelBatchTest
	{
		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestProtoBatch(ModelBatch batch)
		{
			ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
			batch.WriteToStream(byteArrayOutputStream);
			byteArrayOutputStream.Close();
			byte[] bytes = byteArrayOutputStream.ToByteArray();
			ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(bytes);
			ModelBatch recovered = new ModelBatch(byteArrayInputStream);
			byteArrayInputStream.Close();
			NUnit.Framework.Assert.AreEqual(batch.Count, recovered.Count);
			for (int i = 0; i < batch.Count; i++)
			{
				NUnit.Framework.Assert.IsTrue(batch[i].ValueEquals(recovered[i], 1.0e-5));
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestProtoBatchModifier(ModelBatch batch)
		{
			ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
			batch.WriteToStream(byteArrayOutputStream);
			byteArrayOutputStream.Close();
			byte[] bytes = byteArrayOutputStream.ToByteArray();
			ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(bytes);
			ModelBatch recovered = new ModelBatch(byteArrayInputStream, null);
			byteArrayInputStream.Close();
			NUnit.Framework.Assert.AreEqual(batch.Count, recovered.Count);
			for (int i = 0; i < batch.Count; i++)
			{
				NUnit.Framework.Assert.AreEqual("true", recovered[i].GetModelMetaDataByReference()["testing"]);
			}
		}

		/// <exception cref="System.IO.IOException"/>
		[Theory]
		public virtual void TestProtoBatchWithoutFactors(ModelBatch batch)
		{
			ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
			batch.WriteToStreamWithoutFactors(byteArrayOutputStream);
			byteArrayOutputStream.Close();
			byte[] bytes = byteArrayOutputStream.ToByteArray();
			ByteArrayInputStream byteArrayInputStream = new ByteArrayInputStream(bytes);
			ModelBatch recovered = new ModelBatch(byteArrayInputStream);
			byteArrayInputStream.Close();
			NUnit.Framework.Assert.AreEqual(batch.Count, recovered.Count);
			for (int i = 0; i < batch.Count; i++)
			{
				NUnit.Framework.Assert.AreEqual(0, recovered[i].factors.Count);
				NUnit.Framework.Assert.IsTrue(batch[i].GetModelMetaDataByReference().Equals(recovered[i].GetModelMetaDataByReference()));
				for (int j = 0; j < batch[i].GetVariableSizes().Length; j++)
				{
					NUnit.Framework.Assert.IsTrue(batch[i].GetVariableMetaDataByReference(j).Equals(recovered[i].GetVariableMetaDataByReference(j)));
				}
			}
		}

		public class BatchGenerator : Com.Pholser.Junit.Quickcheck.Generator.Generator<ModelBatch>
		{
			internal GraphicalModelTest.GraphicalModelGenerator modelGenerator = new GraphicalModelTest.GraphicalModelGenerator(typeof(GraphicalModel));

			public BatchGenerator(Type type)
				: base(type)
			{
			}

			public override ModelBatch Generate(SourceOfRandomness sourceOfRandomness, IGenerationStatus generationStatus)
			{
				int length = sourceOfRandomness.NextInt(0, 50);
				ModelBatch batch = new ModelBatch();
				for (int i = 0; i < length; i++)
				{
					batch.Add(modelGenerator.Generate(sourceOfRandomness, generationStatus));
				}
				return batch;
			}
		}
	}
}

using Edu.Stanford.Nlp.Loglinear.Model;




namespace Edu.Stanford.Nlp.Loglinear.Benchmarks
{
	public class ConcatVectorBenchmark
	{
		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		public static void Main(string[] args)
		{
			long randomSeed = 10101L;
			// Create the templates we'll use for our truly random dense vectors benchmarks
			ConcatVectorBenchmark.ConcatVectorConstructionRecord[] randomSizedRecords = new ConcatVectorBenchmark.ConcatVectorConstructionRecord[100000];
			Random r = new Random(randomSeed);
			for (int i = 0; i < randomSizedRecords.Length; i++)
			{
				randomSizedRecords[i] = new ConcatVectorBenchmark.ConcatVectorConstructionRecord(r);
			}
			// Create the templates for the more realistic same-sized records
			int[] sizes = ConcatVectorBenchmark.ConcatVectorConstructionRecord.GetRandomSizes(r);
			ConcatVectorBenchmark.ConcatVectorConstructionRecord[] sameSizedRecords = new ConcatVectorBenchmark.ConcatVectorConstructionRecord[100000];
			for (int i_1 = 0; i_1 < sameSizedRecords.Length; i_1++)
			{
				sameSizedRecords[i_1] = new ConcatVectorBenchmark.ConcatVectorConstructionRecord(r, sizes);
			}
			// Create template for clone action
			ConcatVectorBenchmark.ConcatVectorConstructionRecord toClone = new ConcatVectorBenchmark.ConcatVectorConstructionRecord(r);
			// Warmup the JIT compiler
			System.Console.Out.WriteLine("Warming up");
			for (int i_2 = 0; i_2 < 10; i_2++)
			{
				System.Console.Out.WriteLine(i_2);
				System.Console.Out.WriteLine("Serialize");
				ProtoSerializationBenchmark(randomSizedRecords);
			}
			for (int i_3 = 0; i_3 < 10; i_3++)
			{
				System.Console.Out.WriteLine(i_3);
				System.Console.Out.WriteLine("Clone");
				CloneBenchmark(toClone.Create());
			}
			for (int i_4 = 0; i_4 < 100; i_4++)
			{
				System.Console.Out.WriteLine(i_4);
				System.Console.Out.WriteLine("Construction");
				ConstructionBenchmark(randomSizedRecords);
			}
			for (int i_5 = 0; i_5 < 100; i_5++)
			{
				System.Console.Out.WriteLine(i_5);
				System.Console.Out.WriteLine("Inner Product");
				DotProductBenchmark(sameSizedRecords);
			}
			for (int i_6 = 0; i_6 < 100; i_6++)
			{
				System.Console.Out.WriteLine(i_6);
				System.Console.Out.WriteLine("Addition");
				AddBenchmark(sameSizedRecords);
			}
			System.Console.Out.WriteLine("Done warmup");
			// Actual benchmarking
			long cloneRuntime = 0;
			long constructionRuntime = 0;
			long dotProductRuntime = 0;
			long addRuntime = 0;
			long protoSerializeRuntime = 0;
			long protoSerializeSize = 0;
			for (int i_7 = 0; i_7 < 10; i_7++)
			{
				System.Console.Out.WriteLine(i_7);
				System.Console.Out.WriteLine("Serialize");
				ConcatVectorBenchmark.SerializationReport sr = ProtoSerializationBenchmark(randomSizedRecords);
				protoSerializeRuntime += sr.time;
				if (protoSerializeSize == 0)
				{
					protoSerializeSize = sr.size;
				}
			}
			for (int i_8 = 0; i_8 < 10; i_8++)
			{
				System.Console.Out.WriteLine(i_8);
				System.Console.Out.WriteLine("Clone");
				cloneRuntime += CloneBenchmark(toClone.Create());
			}
			for (int i_9 = 0; i_9 < 100; i_9++)
			{
				System.Console.Out.WriteLine(i_9);
				System.Console.Out.WriteLine("Construction");
				constructionRuntime += ConstructionBenchmark(randomSizedRecords);
			}
			for (int i_10 = 0; i_10 < 100; i_10++)
			{
				System.Console.Out.WriteLine(i_10);
				System.Console.Out.WriteLine("Inner Product");
				dotProductRuntime += DotProductBenchmark(sameSizedRecords);
			}
			for (int i_11 = 0; i_11 < 100; i_11++)
			{
				System.Console.Out.WriteLine(i_11);
				System.Console.Out.WriteLine("Addition");
				addRuntime += AddBenchmark(sameSizedRecords);
			}
			System.Console.Out.WriteLine("Clone Runtime: " + cloneRuntime);
			System.Console.Out.WriteLine("Construction Runtime: " + constructionRuntime);
			System.Console.Out.WriteLine("Dot Product Runtimes: " + dotProductRuntime);
			System.Console.Out.WriteLine("Add Runtimes: " + addRuntime);
			System.Console.Out.WriteLine("Proto Serialize Runtimes: " + protoSerializeRuntime);
			System.Console.Out.WriteLine("Proto Serialize Size: " + protoSerializeSize);
		}

		internal static long CloneBenchmark(ConcatVector vector)
		{
			long before = Runtime.CurrentTimeMillis();
			for (int i = 0; i < 10000000; i++)
			{
				vector.DeepClone();
			}
			return Runtime.CurrentTimeMillis() - before;
		}

		internal static ConcatVector[] MakeVectors(ConcatVectorBenchmark.ConcatVectorConstructionRecord[] records)
		{
			ConcatVector[] vectors = new ConcatVector[records.Length];
			for (int i = 0; i < records.Length; i++)
			{
				vectors[i] = records[i].Create();
			}
			return vectors;
		}

		internal static long AddBenchmark(ConcatVectorBenchmark.ConcatVectorConstructionRecord[] records)
		{
			ConcatVector[] vectors = MakeVectors(records);
			long before = Runtime.CurrentTimeMillis();
			for (int i = 1; i < vectors.Length; i++)
			{
				vectors[0].AddVectorInPlace(vectors[i], 1.0f);
			}
			return Runtime.CurrentTimeMillis() - before;
		}

		internal static long DotProductBenchmark(ConcatVectorBenchmark.ConcatVectorConstructionRecord[] records)
		{
			ConcatVector[] vectors = MakeVectors(records);
			long before = Runtime.CurrentTimeMillis();
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[0].DotProduct(vectors[i]);
			}
			return Runtime.CurrentTimeMillis() - before;
		}

		internal static long ConstructionBenchmark(ConcatVectorBenchmark.ConcatVectorConstructionRecord[] records)
		{
			// Then run the ConcatVector parts
			long before = Runtime.CurrentTimeMillis();
			for (int i = 0; i < records.Length; i++)
			{
				ConcatVector v = records[i].Create();
			}
			// Report the union
			return Runtime.CurrentTimeMillis() - before;
		}

		public class ConcatVectorConstructionRecord
		{
			internal int[] componentSizes;

			internal double[][] densePieces;

			internal int[] sparseOffsets;

			internal double[] sparseValues;

			public static int[] GetRandomSizes(Random r)
			{
				int length = r.NextInt(10);
				int[] sizes = new int[length];
				for (int i = 0; i < length; i++)
				{
					bool sparse = r.NextBoolean();
					if (sparse)
					{
						sizes[i] = -1;
					}
					else
					{
						sizes[i] = r.NextInt(100);
					}
				}
				return sizes;
			}

			public ConcatVectorConstructionRecord(Random r)
				: this(r, GetRandomSizes(r))
			{
			}

			public ConcatVectorConstructionRecord(Random r, int[] sizes)
			{
				// Generates a new multivector construction record
				int length = sizes.Length;
				componentSizes = sizes;
				densePieces = new double[length][];
				sparseOffsets = new int[length];
				sparseValues = new double[length];
				for (int i = 0; i < length; i++)
				{
					bool sparse = componentSizes[i] == -1;
					if (sparse)
					{
						sparseOffsets[i] = r.NextInt(100);
						sparseValues[i] = r.NextFloat();
					}
					else
					{
						densePieces[i] = new double[componentSizes[i]];
						for (int j = 0; j < densePieces[i].Length; j++)
						{
							densePieces[i][j] = r.NextFloat();
						}
					}
				}
			}

			// Creates the multivector
			public virtual ConcatVector Create()
			{
				ConcatVector mv = new ConcatVector(componentSizes.Length);
				for (int i = 0; i < componentSizes.Length; i++)
				{
					if (componentSizes[i] == -1)
					{
						mv.SetSparseComponent(i, sparseOffsets[i], sparseValues[i]);
					}
					else
					{
						mv.SetDenseComponent(i, densePieces[i]);
					}
				}
				return mv;
			}
		}

		internal class SerializationReport
		{
			public long time;

			public int size;
		}

		/// <exception cref="System.IO.IOException"/>
		/// <exception cref="System.TypeLoadException"/>
		internal static ConcatVectorBenchmark.SerializationReport ProtoSerializationBenchmark(ConcatVectorBenchmark.ConcatVectorConstructionRecord[] records)
		{
			ConcatVector[] vectors = MakeVectors(records);
			ByteArrayOutputStream bArr = new ByteArrayOutputStream();
			long before = Runtime.CurrentTimeMillis();
			for (int i = 0; i < vectors.Length; i++)
			{
				vectors[i].WriteToStream(bArr);
			}
			bArr.Close();
			byte[] bytes = bArr.ToByteArray();
			ByteArrayInputStream bArrIn = new ByteArrayInputStream(bytes);
			for (int i_1 = 0; i_1 < vectors.Length; i_1++)
			{
				ConcatVector.ReadFromStream(bArrIn);
			}
			ConcatVectorBenchmark.SerializationReport sr = new ConcatVectorBenchmark.SerializationReport();
			sr.time = Runtime.CurrentTimeMillis() - before;
			sr.size = bytes.Length;
			return sr;
		}
	}
}

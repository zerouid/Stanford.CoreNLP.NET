using System.Collections.Generic;
using Edu.Stanford.Nlp.Classify;
using Edu.Stanford.Nlp.IE.Crf;
using Edu.Stanford.Nlp.Ling;
using Edu.Stanford.Nlp.Optimization;
using Edu.Stanford.Nlp.Stats;
using Edu.Stanford.Nlp.Util;



namespace Edu.Stanford.Nlp.Benchmarks
{
	/// <summary>Created by keenon on 6/19/15.</summary>
	/// <remarks>
	/// Created by keenon on 6/19/15.
	/// Down and dirty (and not entirely representative) benchmarks to quickly judge improvement as we optimize stuff
	/// </remarks>
	public class Benchmarks
	{
		/// <summary>
		/// 67% of time spent in LogConditionalObjectiveFunction.rvfcalculate()
		/// 29% of time spent in dataset construction (11% in RVFDataset.addFeatures(), 7% rvf incrementCount(), 11% rest)
		/// Single threaded, 4700 ms
		/// Multi threaded, 700 ms
		/// With same data, seed 42, 245 ms
		/// With reordered accesses for cacheing, 195 ms
		/// Down to 80% of the time, not huge but a win nonetheless
		/// with 8 cpus, a 6.7x speedup -- almost, but not quite linear, pretty good
		/// </summary>
		public static void BenchmarkRVFLogisticRegression()
		{
			RVFDataset<string, string> data = new RVFDataset<string, string>();
			for (int i = 0; i < 10000; i++)
			{
				Random r = new Random(42);
				ICounter<string> features = new ClassicCounter<string>();
				bool cl = r.NextBoolean();
				for (int j = 0; j < 1000; j++)
				{
					double value;
					if (cl && i % 2 == 0)
					{
						value = (r.NextDouble() * 2.0) - 0.6;
					}
					else
					{
						value = (r.NextDouble() * 2.0) - 1.4;
					}
					features.IncrementCount("f" + j, value);
				}
				data.Add(new RVFDatum<string, string>(features, "target:" + cl));
			}
			LinearClassifierFactory<string, string> factory = new LinearClassifierFactory<string, string>();
			long msStart = Runtime.CurrentTimeMillis();
			factory.TrainClassifier(data);
			long delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Training took " + delay + " ms");
		}

		/// <summary>
		/// 57% of time spent in LogConditionalObjectiveFunction.calculateCLBatch()
		/// 22% spent in constructing datums (expensive)
		/// Single threaded, 4100 ms
		/// Multi threaded, 600 ms
		/// With same data, seed 42, 52 ms
		/// With reordered accesses for cacheing, 38 ms
		/// Down to 73% of the time
		/// with 8 cpus, a 6.8x speedup -- basically the same as with RVFDatum
		/// </summary>
		public static void BenchmarkLogisticRegression()
		{
			Dataset<string, string> data = new Dataset<string, string>();
			for (int i = 0; i < 10000; i++)
			{
				Random r = new Random(42);
				ICollection<string> features = new HashSet<string>();
				bool cl = r.NextBoolean();
				for (int j = 0; j < 1000; j++)
				{
					if (cl && i % 2 == 0)
					{
						if (r.NextDouble() > 0.3)
						{
							features.Add("f:" + j + ":true");
						}
						else
						{
							features.Add("f:" + j + ":false");
						}
					}
					else
					{
						if (r.NextDouble() > 0.3)
						{
							features.Add("f:" + j + ":false");
						}
						else
						{
							features.Add("f:" + j + ":false");
						}
					}
				}
				data.Add(new BasicDatum<string, string>(features, "target:" + cl));
			}
			LinearClassifierFactory<string, string> factory = new LinearClassifierFactory<string, string>();
			long msStart = Runtime.CurrentTimeMillis();
			factory.TrainClassifier(data);
			long delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Training took " + delay + " ms");
		}

		/// <summary>
		/// 29% in FactorTable.getValue()
		/// 28% in CRFCliqueTree.getCalibratedCliqueTree()
		/// 12.6% waiting for threads
		/// Single threaded: 15000 ms - 26000 ms
		/// Multi threaded: 4500 ms - 7000 ms
		/// with 8 cpus, 3.3x - 3.7x speedup, around 800% utilization
		/// </summary>
		public static void BenchmarkCRF()
		{
			Properties props = new Properties();
			props.SetProperty("macro", "true");
			// use a generic CRF configuration
			props.SetProperty("useIfInteger", "true");
			props.SetProperty("featureFactory", "edu.stanford.nlp.benchmarks.BenchmarkFeatureFactory");
			props.SetProperty("saveFeatureIndexToDisk", "false");
			CRFClassifier<CoreLabel> crf = new CRFClassifier<CoreLabel>(props);
			Random r = new Random(42);
			IList<IList<CoreLabel>> data = new List<IList<CoreLabel>>();
			for (int i = 0; i < 100; i++)
			{
				IList<CoreLabel> sentence = new List<CoreLabel>();
				for (int j = 0; j < 20; j++)
				{
					CoreLabel l = new CoreLabel();
					l.SetWord("j:" + j);
					bool tag = j % 2 == 0 ^ (r.NextDouble() > 0.7);
					l.Set(typeof(CoreAnnotations.AnswerAnnotation), "target:" + tag);
					sentence.Add(l);
				}
				data.Add(sentence);
			}
			long msStart = Runtime.CurrentTimeMillis();
			crf.Train(data);
			long delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Training took " + delay + " ms");
		}

		public static void BenchmarkSGD()
		{
			Dataset<string, string> data = new Dataset<string, string>();
			for (int i = 0; i < 10000; i++)
			{
				Random r = new Random(42);
				ICollection<string> features = new HashSet<string>();
				bool cl = r.NextBoolean();
				for (int j = 0; j < 1000; j++)
				{
					if (cl && i % 2 == 0)
					{
						if (r.NextDouble() > 0.3)
						{
							features.Add("f:" + j + ":true");
						}
						else
						{
							features.Add("f:" + j + ":false");
						}
					}
					else
					{
						if (r.NextDouble() > 0.3)
						{
							features.Add("f:" + j + ":false");
						}
						else
						{
							features.Add("f:" + j + ":false");
						}
					}
				}
				data.Add(new BasicDatum<string, string>(features, "target:" + cl));
			}
			LinearClassifierFactory<string, string> factory = new LinearClassifierFactory<string, string>();
			factory.SetMinimizerCreator(new _IFactory_192());
			long msStart = Runtime.CurrentTimeMillis();
			factory.TrainClassifier(data);
			long delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Training took " + delay + " ms");
		}

		private sealed class _IFactory_192 : IFactory<IMinimizer<IDiffFunction>>
		{
			public _IFactory_192()
			{
			}

			public IMinimizer<IDiffFunction> Create()
			{
				return new SGDMinimizer<IDiffFunction>(0.1, 100, 0, 1000);
			}
		}

		public static void BenchmarkDatum()
		{
			long msStart = Runtime.CurrentTimeMillis();
			Dataset<string, string> data = new Dataset<string, string>();
			for (int i = 0; i < 10000; i++)
			{
				Random r = new Random(42);
				ICollection<string> features = new HashSet<string>();
				bool cl = r.NextBoolean();
				for (int j = 0; j < 1000; j++)
				{
					if (cl && i % 2 == 0)
					{
						if (r.NextDouble() > 0.3)
						{
							features.Add("f:" + j + ":true");
						}
						else
						{
							features.Add("f:" + j + ":false");
						}
					}
					else
					{
						if (r.NextDouble() > 0.3)
						{
							features.Add("f:" + j + ":false");
						}
						else
						{
							features.Add("f:" + j + ":false");
						}
					}
				}
				data.Add(new BasicDatum<string, string>(features, "target:" + cl));
			}
			long delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Dataset construction took " + delay + " ms");
			msStart = Runtime.CurrentTimeMillis();
			for (int i_1 = 0; i_1 < 10000; i_1++)
			{
				Random r = new Random(42);
				ICollection<string> features = new HashSet<string>();
				bool cl = r.NextBoolean();
				for (int j = 0; j < 1000; j++)
				{
					if (cl && i_1 % 2 == 0)
					{
						if (r.NextDouble() > 0.3)
						{
						}
					}
					else
					{
						if (r.NextDouble() > 0.3)
						{
						}
					}
				}
			}
			delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("MultiVector took " + delay + " ms");
		}

		/// <summary>on my machine this results in a factor of two gain, roughly</summary>
		public static void TestAdjacency()
		{
			double[][] sqar = new double[][] { new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double
				[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000]
				, new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new double[1000], new 
				double[1000], new double[1000] };
			Random r = new Random();
			int k = 0;
			long msStart = Runtime.CurrentTimeMillis();
			for (int i = 0; i < 10000; i++)
			{
				int loc = r.NextInt(10000);
				for (int j = 0; j < 1000; j++)
				{
					k += sqar[loc][j];
				}
			}
			long delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Scanning with cache friendly lookups took " + delay + " ms");
			int[] randLocs = new int[10000];
			for (int i_1 = 0; i_1 < 10000; i_1++)
			{
				randLocs[i_1] = r.NextInt(10000);
			}
			k = 0;
			msStart = Runtime.CurrentTimeMillis();
			for (int j_1 = 0; j_1 < 1000; j_1++)
			{
				for (int i_2 = 0; i_2 < 10000; i_2++)
				{
					k += sqar[randLocs[i_2]][j_1];
				}
			}
			delay = Runtime.CurrentTimeMillis() - msStart;
			System.Console.Out.WriteLine("Scanning with cache UNfriendly lookups took " + delay + " ms");
		}

		public static void Main(string[] args)
		{
			for (int i = 0; i < 100; i++)
			{
				// benchmarkRVFLogisticRegression();
				// benchmarkLogisticRegression();
				BenchmarkSGD();
			}
		}
		// benchmarkCRF();
		// testAdjacency();
	}
}
